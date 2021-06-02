﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using CMS.DocumentEngine;
using CMS.Membership;

using XperienceAdapter.Extensions;
using XperienceAdapter.Repositories;
using XperienceAdapter.Services;
using Business.Extensions;
using Business.Models;

namespace Business.Repositories
{
    public class DoctorRepository : BasePageRepository<Doctor, CMS.DocumentEngine.Types.MedioClinic.Doctor>
    {
        private readonly IUserInfoProvider _userInfoProvider;

        /* Conventional routing: Begin */
        //private readonly INavigationRepository _navigationRepository;

        //public DoctorRepository(IRepositoryServices repositoryServices, IUserInfoProvider userInfoProvider, INavigationRepository navigationRepository) : base(repositoryServices)
        //{
        //    _userInfoProvider = userInfoProvider ?? throw new ArgumentNullException(nameof(userInfoProvider));
        //    _navigationRepository = navigationRepository ?? throw new ArgumentNullException(nameof(navigationRepository));
        //}
        /* Conventional routing: End */

        /* CTB routring: Begin */
        public DoctorRepository(IRepositoryServices repositoryServices, IUserInfoProvider userInfoProvider) : base(repositoryServices)
        {
            _userInfoProvider = userInfoProvider ?? throw new ArgumentNullException(nameof(userInfoProvider));
        }
        /* CTB routring: End */

        public override void MapDtoProperties(CMS.DocumentEngine.Types.MedioClinic.Doctor page, Doctor dto)
        {
            dto.UrlSlug = page.UrlSlug;
            dto.UserId = page.UserAccount;
            dto.UserName = _userInfoProvider.Get(page.UserAccount)?.UserName;
            dto.EmergencyShift = GetShiftDayOfWeek(page.Fields.EmergencyShift);
            dto.EmergencyShiftString = page.Fields.EmergencyShift.FirstOrDefault()?.DocumentName;
            dto.Degree = page.Degree;
            dto.Biography = page.Fields.Biography;
            dto.Specialty = page.Specialty;
 
            if (page.Fields.BackdropPicture != null)
            {
                dto.BackdropPictureUrl = _repositoryServices.PageAttachmentUrlRetriever.Retrieve(page.Fields.BackdropPicture); 
            }
            
            var culture = Thread.CurrentThread.CurrentUICulture.ToSiteCulture();

            if (culture != null)
            {
                /* CTB routring: Begin */
                dto.DoctorDetailUrl = _repositoryServices.PageUrlRetriever.Retrieve(page, culture.IsoCode).RelativePath;
                /* CTB routing: End */

                /* Conventional routing: Begin */
                //dto.DoctorDetailUrl = _navigationRepository.GetUrlByNodeId(page.NodeID, culture);
                /* Conventional routing: End */
            }
        }

        private static DayOfWeek? GetShiftDayOfWeek(IEnumerable<TreeNode> dayOfWeekPage) =>
            dayOfWeekPage.ToDaysOfWeek().FirstOrDefault();
    }
}
