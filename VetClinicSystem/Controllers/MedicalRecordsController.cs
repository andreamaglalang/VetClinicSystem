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
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var records = _medicalRecordService.GetAll();

            if (roleId == 3)
            {
                var myPetIds = _petService.GetByUser(userId.Value).Select(p => p.Id).ToList();
                records = records.Where(r => myPetIds.Contains(r.PetId)).ToList();
            }

            return View(records);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (roleId == 3)
                ViewBag.Pets = new SelectList(_petService.GetByUser(userId.Value), "Id", "PetName");
            else
                ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MedicalRecord medicalRecord)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Pet");
            ModelState.Remove("CreatedByUser");
            NormalizeMedicalRecord(medicalRecord);

            if (roleId == 3)
            {
                var myPets = _petService.GetByUser(userId.Value);
                if (!myPets.Any(p => p.Id == medicalRecord.PetId))
                    return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                if (roleId == 3)
                    ViewBag.Pets = new SelectList(_petService.GetByUser(userId.Value), "Id", "PetName", medicalRecord.PetId);
                else
                    ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", medicalRecord.PetId);

                TempData["Error"] = "Please complete all required medical record fields.";
                return View(medicalRecord);
            }

            medicalRecord.CreatedByUserId = userId.Value;
            _medicalRecordService.Add(medicalRecord);

            TempData["Success"] = "Medical record added successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var record = _medicalRecordService.GetById(id);
            if (record == null) return NotFound();

            if (roleId == 3)
            {
                var myPetIds = _petService.GetByUser(userId.Value).Select(p => p.Id).ToList();
                if (!myPetIds.Contains(record.PetId))
                    return Unauthorized();

                ViewBag.Pets = new SelectList(_petService.GetByUser(userId.Value), "Id", "PetName", record.PetId);
            }
            else
            {
                ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", record.PetId);
            }

            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(MedicalRecord medicalRecord)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Pet");
            ModelState.Remove("CreatedByUser");
            NormalizeMedicalRecord(medicalRecord);

            if (roleId == 3)
            {
                var myPetIds = _petService.GetByUser(userId.Value).Select(p => p.Id).ToList();
                if (!myPetIds.Contains(medicalRecord.PetId))
                    return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                if (roleId == 3)
                    ViewBag.Pets = new SelectList(_petService.GetByUser(userId.Value), "Id", "PetName", medicalRecord.PetId);
                else
                    ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", medicalRecord.PetId);

                TempData["Error"] = "Please complete all required medical record fields.";
                return View(medicalRecord);
            }

            _medicalRecordService.Update(medicalRecord);
            TempData["Success"] = "Medical record updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var record = _medicalRecordService.GetById(id);
            if (record == null) return NotFound();

            if (roleId == 3)
            {
                var myPetIds = _petService.GetByUser(userId.Value).Select(p => p.Id).ToList();
                if (!myPetIds.Contains(record.PetId))
                    return Unauthorized();
            }

            return View(record);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var record = _medicalRecordService.GetById(id);
            if (record == null) return NotFound();

            if (roleId == 3)
            {
                var myPetIds = _petService.GetByUser(userId.Value).Select(p => p.Id).ToList();
                if (!myPetIds.Contains(record.PetId))
                    return Unauthorized();
            }

            return View(record);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var record = _medicalRecordService.GetById(id);
            if (record == null) return NotFound();

            if (roleId == 3)
            {
                var myPetIds = _petService.GetByUser(userId.Value).Select(p => p.Id).ToList();
                if (!myPetIds.Contains(record.PetId))
                    return Unauthorized();
            }

            _medicalRecordService.Delete(id);
            TempData["Success"] = "Medical record deleted successfully.";
            return RedirectToAction("Index");
        }

        private static void NormalizeMedicalRecord(MedicalRecord medicalRecord)
        {
            medicalRecord.Diagnosis = string.IsNullOrWhiteSpace(medicalRecord.Diagnosis) ? null : medicalRecord.Diagnosis.Trim();
            medicalRecord.Treatment = string.IsNullOrWhiteSpace(medicalRecord.Treatment) ? null : medicalRecord.Treatment.Trim();
            medicalRecord.Prescription = string.IsNullOrWhiteSpace(medicalRecord.Prescription) ? null : medicalRecord.Prescription.Trim();
            medicalRecord.Findings = string.IsNullOrWhiteSpace(medicalRecord.Findings) ? null : medicalRecord.Findings.Trim();
        }
    }
}
