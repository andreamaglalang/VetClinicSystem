using Microsoft.AspNetCore.Mvc;
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

            var clinic = _clinicService.GetClinicInfo() ?? new ClinicInfo();
            return View(clinic);
        }

        [HttpPost]
        public IActionResult Edit(ClinicInfo clinicInfo)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(clinicInfo);

            _clinicService.SaveClinicInfo(clinicInfo);
            return RedirectToAction("Index");
        }
    }
}