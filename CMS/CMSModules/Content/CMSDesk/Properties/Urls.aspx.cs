﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.UI.WebControls;
using CMS.Base.Web.UI;
using CMS.Base.Web.UI.ActionsConfig;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.DocumentEngine.Internal;
using CMS.DocumentEngine.Routing;
using CMS.DocumentEngine.Routing.Internal;
using CMS.DocumentEngine.Web.UI;
using CMS.Helpers;
using CMS.Membership;
using CMS.UIControls;

public partial class CMSModules_Content_CMSDesk_Properties_Urls : CMSPropertiesPage
{
    private const string RESOURCE_NAME = "CMS.Content";
    private const string PERMISSION_NAME = "ManageAlternativeURLs";

    private bool? mCanManageAlternativeURLs;
    private string mSitePresentationUrl;
    private readonly Dictionary<int, CMSGridActionButton> mOpenUrlActions = new Dictionary<int, CMSGridActionButton>();


    protected bool CanManageAlternativeURLs
    {
        get
        {
            if (!mCanManageAlternativeURLs.HasValue)
            {
                mCanManageAlternativeURLs = MembershipContext.AuthenticatedUser.IsAuthorizedPerResource(RESOURCE_NAME, PERMISSION_NAME);
            }

            return mCanManageAlternativeURLs.Value;
        }
    }


    private string SitePresentationUrl
    {
        get
        {
            return mSitePresentationUrl ?? (mSitePresentationUrl = DocumentURLProvider.GetPresentationUrl(Node.NodeSiteID, Node.DocumentCulture) + "/");
        }
    }


    protected override void OnInit(EventArgs e)
    {
        var showSlugsList = string.Equals(QueryHelper.GetString("subtab", null), PageRoutingUIHelper.SLUGS_LIST_SUBTAB_NAME, StringComparison.InvariantCultureIgnoreCase);
        if (showSlugsList)
        {
            URLHelper.Redirect(URLHelper.ResolveUrl(PageRoutingUIHelper.GetSlugsListingPath(Node.NodeID, Node.DocumentCulture)));
        }

        DocumentManager.RegisterEvents = false;
        DocumentManager.UseDocumentHelper = false;
        DocumentManager.CheckPermissions = false;

        base.OnInit(e);

        // Enable split mode
        EnableSplitMode = true;

        // Register the scripts
        ScriptHelper.RegisterLoader(Page);

        if (!MembershipContext.AuthenticatedUser.IsAuthorizedPerUIElement("CMS.Content", "Properties.URLs"))
        {
            RedirectToUIElementAccessDenied("CMS.Content", "Properties.URLs");
        }

        pnlAlternativeUrls.Visible = AlternativeUrlHelper.IsAlternativeUrlUIEnabled(Node.NodeSiteID);
        pnlUrl.Visible = pnlContainer.Visible = PageRoutingHelper.GetRoutingMode(Node.NodeSiteID) == PageRoutingModeEnum.BasedOnContentTree;

        // Hide option to view all slugs when only one culture is assigned to the site
        btnDisplaySlugs.Visible = Node.Site.HasMultipleCultures;

        ScriptHelper.RegisterModule(this, "CMS/Clamp", new
        {
            elementWithTextSelector = ".static-textpanel"
        });
    }


    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (pnlUrl.Visible)
        {
            var enabled = MembershipContext.AuthenticatedUser.IsAuthorizedPerDocument(Node, NodePermissionsEnum.Modify) == AuthorizationResultEnum.Allowed;
            menu.Enabled = pnlUrl.Enabled = enabled;

            lblSlug.AssociatedControlClientID = txtSlug.TextBox.ClientID;

            menu.ActionsList.Add(new SaveAction
            {
                Tooltip = !enabled ? String.Format(ResHelper.GetString("cmsdesk.notauthorizedtoeditdocument"), HTMLHelper.HTMLEncode(DocumentManager.Node.GetDocumentName())) : null
            });

            if (enabled)
            {
                // Register for action
                ComponentEvents.RequestEvents.RegisterForEvent(ComponentEvents.SAVE, btnSave_Click);
            }
        }

        if (pnlAlternativeUrls.Visible)
        {
            var url = ScriptHelper.ResolveUrl($"~/CMSModules/Content/CMSDesk/Properties/Urls_AlternativeUrlEdit.aspx?nodeid={NodeID}");
            btnAddAlternativeUrl.OnClientClick = GetClickScript(url);
            gridUrls.WhereCondition = $"AlternativeUrlSiteID = {Node.NodeSiteID} AND AlternativeUrlDocumentID = {Node.DocumentID}";

            // Ensure script which allows redirecting to conflicting page
            ScriptHelper.RegisterEditScript(Page, false);

            CheckAlternativeUrlsPermissions();
        }

        var displaySlugUrl = ScriptHelper.ResolveUrl(PageRoutingUIHelper.GetSlugsListingPath(Node.NodeID, Node.DocumentCulture));
        btnDisplaySlugs.OnClientClick = GetClickScript(displaySlugUrl);
    }


    protected override void OnPreRender(EventArgs e)
    {
        base.OnPreRender(e);

        if (!IsPostBack && pnlUrl.Visible)
        {
            SetUrlTextbox();
        }
    }


    private void SetUrlTextbox(bool refreshSlug = true)
    {
        var urlPath = Node.GetPageUrlPath();
        txtSlug.MaxLength = PageUrlPath.SLUG_LENGTH;
        var parentPathInCorrectCase = PageRoutingHelper.EnsurePathFormat(urlPath.ParentPath, Node.NodeSiteID);
        txtSlug.PlaceholderText = TextHelper.LimitLength(SitePresentationUrl + parentPathInCorrectCase, 45, CutTextEnum.Start);
        if (refreshSlug)
        {
            txtSlug.Value = urlPath.Slug;
        }
    }


    protected void btnSave_Click(object sender, EventArgs e)
    {
        // Check modify permissions
        if (MembershipContext.AuthenticatedUser.IsAuthorizedPerDocument(Node, NodePermissionsEnum.Modify) == AuthorizationResultEnum.Denied)
        {
            return;
        }

        if (String.IsNullOrWhiteSpace(txtSlug.TextBox.Text))
        {
            ShowError(GetString("general.requiresvalue"));
            SetUrlTextbox(false);
            return;
        }

        if (new PageUrlPathSlugUpdater(Node).TryUpdate(txtSlug.TextBox.Text, out var collisions))
        {
            DocumentManager.SaveDocument();
            SetUrlTextbox(true);
        }
        else
        {
            ShowCollisionErrorMessage(collisions);
            SetUrlTextbox(false);
        }
    }


    private void ShowCollisionErrorMessage(IEnumerable<CollisionData> collisions)
    {
        foreach (var message in PageRoutingUIHelper.GetCollisionErrorMessages(collisions, Node.NodeSiteID))
        {
            AddError(message);
        }
    }


    private void CheckAlternativeUrlsPermissions()
    {
        if (CanManageAlternativeURLs)
        {
            return;
        }

        btnAddAlternativeUrl.Enabled = false;
        btnAddAlternativeUrl.ToolTip = string.Format(ResHelper.GetString("content.ui.properties.notauthorizedtomanagealternativeurls"), HTMLHelper.HTMLEncode(DocumentManager.Node.GetDocumentName()));

        // Remove extra grid action buttons
        var actionsToRemove = gridUrls.GridActions.Actions
            .Where(t => !t.Name.Equals("openurl", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var action in actionsToRemove)
        {
            gridUrls.GridActions.Actions.Remove(action);
        }
    }


    protected void gridUrls_OnAction(string actionName, object actionArgument)
    {
        if (!CanManageAlternativeURLs)
        {
            RedirectToAccessDenied(RESOURCE_NAME, PERMISSION_NAME);
        }

        int urlId = ValidationHelper.GetInteger(actionArgument, 0);

        switch (actionName.ToLowerInvariant())
        {
            case "delete":
                AlternativeUrlInfoProvider.DeleteAlternativeUrlInfo(urlId);
                DocumentManager.SaveDocument();
                break;
        }
    }


    protected object gridUrls_OnExternalDataBound(object sender, string sourceName, object parameter)
    {
        switch (sourceName.ToLowerInvariant())
        {
            case "openurl":
                {
                    var row = (DataRowView)((GridViewRow)parameter).DataItem;
                    var altUrlId = DataHelper.GetIntValue(row.Row, "AlternativeUrlID");

                    mOpenUrlActions[altUrlId] = (CMSGridActionButton)sender;
                }
                break;

            case "url":
                {
                    var row = (DataRowView)parameter;
                    var altUrl = DataHelper.GetStringValue(row.Row, "AlternativeUrlUrl");
                    altUrl = PageRoutingHelper.EnsurePathFormat(altUrl, Node.NodeSiteID);

                    var altUrlId = DataHelper.GetIntValue(row.Row, "AlternativeUrlID");

                    var completeUrl = HTMLHelper.HTMLEncode($"{SitePresentationUrl}{altUrl}");

                    SetOpenUrlAction(altUrlId, completeUrl);

                    return GetLabelControl(completeUrl);
                }

            case "edit":
                {
                    var btn = (CMSGridActionButton)sender;
                    var row = (DataRowView)((GridViewRow)parameter).DataItem;
                    var objectId = DataHelper.GetIntValue(row.Row, "AlternativeUrlID");
                    var siteId = DataHelper.GetIntValue(row.Row, "AlternativeUrlSiteID");
                    var url = ScriptHelper.ResolveUrl($"~/CMSModules/Content/CMSDesk/Properties/Urls_AlternativeUrlEdit.aspx?nodeid={NodeID}&objectid={objectId}&siteid={siteId}");
                    btn.OnClientClick = GetClickScript(url);
                }
                break;
        }

        return parameter;
    }


    private string GetClickScript(string url)
    {
        return $@"
            if (CheckChanges && !CheckChanges()) {{ 
                return false; 
            }} else {{ 
                window.open('{url}','_self');
                return false; 
            }}";
    }


    /// <summary>
    /// Sets 'onclick' JS action on grid action button to open new window
    /// </summary>
    /// <param name="altUrlId">Alternative Url ID</param>
    /// <param name="url">Un-Escaped plain-text full Url</param>
    private void SetOpenUrlAction(int altUrlId, string url)
    {
        if (mOpenUrlActions.TryGetValue(altUrlId, out CMSGridActionButton button))
        {
            var escapedUrl = Uri.EscapeUriString(url);
            button.OnClientClick = $"window.open(\"{escapedUrl}\"); return false;";
        }
    }


    private static object GetLabelControl(string completeUrl)
    {
        var label = new Label
        {
            Text = completeUrl,
            CssClass = "static-textpanel"
        };
        var container = new Panel
        {
            ToolTip = completeUrl,
            CssClass = "inline-editing-row-container",
        };
        container.Controls.Add(label);

        return container;
    }
}