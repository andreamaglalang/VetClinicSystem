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
            var clinic = _clinicService.GetClinicInfo();
            return View(clinic);
        }

        [HttpGet]
        public IActionResult Edit()
        {
            var clinic = _clinicService.GetClinicInfo();
            return View(clinic);
        }

        [HttpPost]
        public IActionResult Edit(ClinicInfo clinicInfo)
        {
            if (!ModelState.IsValid)
                return View(clinicInfo);

            _clinicService.UpdateClinicInfo(clinicInfo);
            return RedirectToAction("Index");
        }
    }
}