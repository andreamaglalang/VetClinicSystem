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

            NormalizeClinicInfo(clinicInfo);

            if (!ModelState.IsValid)
                return View(clinicInfo);

            clinicInfo.ContactNumber = PhoneNumberHelper.Normalize(clinicInfo.ContactNumber);

            _clinicService.SaveClinicInfo(clinicInfo);
            return RedirectToAction("Index");
        }

        private static void NormalizeClinicInfo(ClinicInfo clinicInfo)
        {
            clinicInfo.ClinicName = clinicInfo.ClinicName?.Trim() ?? string.Empty;
            clinicInfo.Address = string.IsNullOrWhiteSpace(clinicInfo.Address) ? null : clinicInfo.Address.Trim();
            clinicInfo.ContactNumber = PhoneNumberHelper.Normalize(clinicInfo.ContactNumber);
            clinicInfo.Email = string.IsNullOrWhiteSpace(clinicInfo.Email) ? null : clinicInfo.Email.Trim();
            clinicInfo.OperatingHours = string.IsNullOrWhiteSpace(clinicInfo.OperatingHours) ? null : clinicInfo.OperatingHours.Trim();
            clinicInfo.FacebookPage = string.IsNullOrWhiteSpace(clinicInfo.FacebookPage) ? null : clinicInfo.FacebookPage.Trim();
            clinicInfo.AboutText = string.IsNullOrWhiteSpace(clinicInfo.AboutText) ? null : clinicInfo.AboutText.Trim();
            clinicInfo.Mission = string.IsNullOrWhiteSpace(clinicInfo.Mission) ? null : clinicInfo.Mission.Trim();
            clinicInfo.Vision = string.IsNullOrWhiteSpace(clinicInfo.Vision) ? null : clinicInfo.Vision.Trim();
        }
    }
}
