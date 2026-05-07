using Microsoft.AspNetCore.Mvc;
using VetClinicSystem.Helpers;
using VetClinicSystem.Models;
using VetClinicSystem.Services.Clinics;

namespace VetClinicSystem.Controllers
{
    public class ClinicController : Controller
    {
        private readonly IClinicService _clinicService;

        public ClinicController(IClinicService clinicService)
        {
            _clinicService = clinicService;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var clinic = _clinicService.GetClinicInfo();
            return View(clinic);
        }

        [HttpGet]
        public IActionResult Edit()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("Index");

            var clinic = _clinicService.GetClinicInfo() ?? new ClinicInfo();
            return View(clinic);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ClinicInfo clinicInfo)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("Index");

            if (!ModelState.IsValid)
                return View(clinicInfo);

            clinicInfo.ContactNumber = PhoneNumberHelper.Normalize(clinicInfo.ContactNumber);

            _clinicService.SaveClinicInfo(clinicInfo);
            return RedirectToAction("Index");
        }
    }
}
