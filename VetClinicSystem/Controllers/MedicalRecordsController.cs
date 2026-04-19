using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VetClinicSystem.Models;
using VetClinicSystem.Services.MedicalRecords;
using VetClinicSystem.Services.Pets;

namespace VetClinicSystem.Controllers
{
    public class MedicalRecordsController : Controller
    {
        private readonly IMedicalRecordService _medicalRecordService;
        private readonly IPetService _petService;

        public MedicalRecordsController(IMedicalRecordService medicalRecordService, IPetService petService)
        {
            _medicalRecordService = medicalRecordService;
            _petService = petService;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            return View(_medicalRecordService.GetAll());
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
        public IActionResult Create(MedicalRecord medicalRecord)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", medicalRecord.PetId);
                return View(medicalRecord);
            }

            medicalRecord.CreatedByUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            _medicalRecordService.Add(medicalRecord);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var record = _medicalRecordService.GetById(id);
            if (record == null) return NotFound();

            ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", record.PetId);
            return View(record);
        }

        [HttpPost]
        public IActionResult Edit(MedicalRecord medicalRecord)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", medicalRecord.PetId);
                return View(medicalRecord);
            }

            _medicalRecordService.Update(medicalRecord);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var record = _medicalRecordService.GetById(id);
            if (record == null) return NotFound();

            return View(record);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var record = _medicalRecordService.GetById(id);
            if (record == null) return NotFound();

            return View(record);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            _medicalRecordService.Delete(id);
            return RedirectToAction("Index");
        }
    }
}