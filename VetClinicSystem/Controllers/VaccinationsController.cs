using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VetClinicSystem.Models;
using VetClinicSystem.Services.Pets;
using VetClinicSystem.Services.Vaccinations;

namespace VetClinicSystem.Controllers
{
    public class VaccinationsController : Controller
    {
        private readonly IVaccinationService _vaccinationService;
        private readonly IPetService _petService;

        public VaccinationsController(IVaccinationService vaccinationService, IPetService petService)
        {
            _vaccinationService = vaccinationService;
            _petService = petService;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            return View(_vaccinationService.GetAll());
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName");
            return View();
        }

        [HttpPost]
        public IActionResult Create(VaccinationRecord vaccinationRecord)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", vaccinationRecord.PetId);
                return View(vaccinationRecord);
            }

            vaccinationRecord.CreatedByUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            _vaccinationService.Add(vaccinationRecord);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var vaccination = _vaccinationService.GetById(id);
            if (vaccination == null) return NotFound();

            ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", vaccination.PetId);
            return View(vaccination);
        }

        [HttpPost]
        public IActionResult Edit(VaccinationRecord vaccinationRecord)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", vaccinationRecord.PetId);
                return View(vaccinationRecord);
            }

            _vaccinationService.Update(vaccinationRecord);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var vaccination = _vaccinationService.GetById(id);
            if (vaccination == null) return NotFound();

            return View(vaccination);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var vaccination = _vaccinationService.GetById(id);
            if (vaccination == null) return NotFound();

            return View(vaccination);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            _vaccinationService.Delete(id);
            return RedirectToAction("Index");
        }
    }
}