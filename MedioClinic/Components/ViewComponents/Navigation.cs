﻿#define no_suffix

using System;
using Microsoft.AspNetCore.Mvc;

using Business.Repositories;

namespace MedioClinic.ViewComponents
{
    public class Navigation : ViewComponent
    {
        protected readonly INavigationRepository _navigationRepository;

        public Navigation(INavigationRepository navigationRepository)
        {
            _navigationRepository = navigationRepository ?? throw new ArgumentNullException(nameof(navigationRepository));
        }

        public IViewComponentResult Invoke(string placement, string? nodeAliasPath = default)
        {
            var navigation = _navigationRepository.GetNavigation();

            return View(placement, navigation);
        }
    }
}