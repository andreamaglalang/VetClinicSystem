using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VetClinicSystem.Models;
using VetClinicSystem.Services.MedicalRecords;
using VetClinicSystem.Services.Pets;

namespace VetClinicSystem.Controllers
{
    public class MedicalRecordsController : Controller
    {
        private const int AdminRoleId = 1;
        private const int StaffRoleId = 2;
        private const int ClientRoleId = 3;

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

            if (roleId == ClientRoleId)
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

            if (!CanManageMedicalRecords(roleId))
                return RedirectRestrictedClient();

            ViewBag.Pets = BuildPetSelectList();
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

            if (!CanManageMedicalRecords(roleId))
                return RedirectRestrictedClient();

            ModelState.Remove("Pet");
            ModelState.Remove("CreatedByUser");
            NormalizeMedicalRecord(medicalRecord);

            if (!ModelState.IsValid)
            {
                ViewBag.Pets = BuildPetSelectList(medicalRecord.PetId);
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

            if (!CanManageMedicalRecords(roleId))
                return RedirectRestrictedClient();

            var record = _medicalRecordService.GetById(id);
            if (record == null)
                return NotFound();

            ViewBag.Pets = BuildPetSelectList(record.PetId);
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

            if (!CanManageMedicalRecords(roleId))
                return RedirectRestrictedClient();

            ModelState.Remove("Pet");
            ModelState.Remove("CreatedByUser");
            NormalizeMedicalRecord(medicalRecord);

            if (!ModelState.IsValid)
            {
                ViewBag.Pets = BuildPetSelectList(medicalRecord.PetId);
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
            if (record == null)
                return NotFound();

            if (roleId == ClientRoleId)
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

            if (!CanManageMedicalRecords(roleId))
                return RedirectRestrictedClient();

            var record = _medicalRecordService.GetById(id);
            if (record == null)
                return NotFound();

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

            if (!CanManageMedicalRecords(roleId))
                return RedirectRestrictedClient();

            var record = _medicalRecordService.GetById(id);
            if (record == null)
                return NotFound();

            _medicalRecordService.Delete(id);
            TempData["Success"] = "Medical record deleted successfully.";
            return RedirectToAction("Index");
        }

        private IActionResult RedirectRestrictedClient()
        {
            TempData["Error"] = "Only admin and staff can add, edit, or delete medical records.";
            return RedirectToAction("Index");
        }

        private static bool CanManageMedicalRecords(int? roleId)
        {
            return roleId == AdminRoleId || roleId == StaffRoleId;
        }

        private static void NormalizeMedicalRecord(MedicalRecord medicalRecord)
        {
            medicalRecord.Diagnosis = string.IsNullOrWhiteSpace(medicalRecord.Diagnosis) ? null : medicalRecord.Diagnosis.Trim();
            medicalRecord.Treatment = string.IsNullOrWhiteSpace(medicalRecord.Treatment) ? null : medicalRecord.Treatment.Trim();
            medicalRecord.Prescription = string.IsNullOrWhiteSpace(medicalRecord.Prescription) ? null : medicalRecord.Prescription.Trim();
            medicalRecord.Findings = string.IsNullOrWhiteSpace(medicalRecord.Findings) ? null : medicalRecord.Findings.Trim();
        }

        private SelectList BuildPetSelectList(int? selectedPetId = null)
        {
            var pets = _petService.GetAll()
                .Select(p => new
                {
                    p.Id,
                    DisplayName = string.IsNullOrWhiteSpace($"{p.Owner?.FirstName} {p.Owner?.LastName}".Trim())
                        ? $"{p.PetName} - N/A"
                        : $"{p.PetName} - {p.Owner!.FirstName} {p.Owner.LastName}".Trim()
                })
                .ToList();

            return new SelectList(pets, "Id", "DisplayName", selectedPetId);
        }
    }
}
