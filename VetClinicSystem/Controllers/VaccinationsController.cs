using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VetClinicSystem.Models;
using VetClinicSystem.Services.Pets;
using VetClinicSystem.Services.Vaccinations;

namespace VetClinicSystem.Controllers
{
    public class VaccinationsController : Controller
    {
        private const int AdminRoleId = 1;
        private const int StaffRoleId = 2;
        private const int ClientRoleId = 3;

        private const string NextDueDateValidationMessage =
            "Next due date must be later than the vaccination date.";

        private const string VaccinationTuesdayValidationMessage =
            "Vaccination date cannot be scheduled on Tuesday because the clinic is closed every Tuesday.";

        private const string NextDueTuesdayValidationMessage =
            "Next due date cannot fall on Tuesday because the clinic is closed every Tuesday.";

        private readonly IVaccinationService _vaccinationService;
        private readonly IPetService _petService;

        public VaccinationsController(
            IVaccinationService vaccinationService,
            IPetService petService)
        {
            _vaccinationService = vaccinationService;
            _petService = petService;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var records = _vaccinationService.GetAll();

            if (roleId == ClientRoleId)
            {
                var myPetIds = _petService.GetByUser(userId.Value).Select(p => p.Id).ToList();
                records = records.Where(v => myPetIds.Contains(v.PetId)).ToList();
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

            if (!IsAdminOrStaff(roleId))
            {
                TempData["Error"] = "Only admin and staff can add, edit, or delete vaccination records.";
                return RedirectToAction("Index");
            }

            ViewBag.Pets = BuildPetSelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(VaccinationRecord vaccinationRecord)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Pet");
            ModelState.Remove("CreatedByUser");

            NormalizeVaccinationRecord(vaccinationRecord);
            ValidateVaccinationDates(vaccinationRecord);

            if (!IsAdminOrStaff(roleId))
            {
                TempData["Error"] = "Only admin and staff can add, edit, or delete vaccination records.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Pets = BuildPetSelectList(vaccinationRecord.PetId);
                TempData["Error"] = "Please correct the vaccination dates before saving.";
                return View(vaccinationRecord);
            }

            vaccinationRecord.CreatedByUserId = userId.Value;
            _vaccinationService.Add(vaccinationRecord);

            TempData["Success"] = "Vaccination record added successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (!IsAdminOrStaff(roleId))
            {
                TempData["Error"] = "Only admin and staff can add, edit, or delete vaccination records.";
                return RedirectToAction("Index");
            }

            var vaccination = _vaccinationService.GetById(id);
            if (vaccination == null)
                return NotFound();

            ViewBag.Pets = BuildPetSelectList(vaccination.PetId);
            return View(vaccination);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(VaccinationRecord vaccinationRecord)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Pet");
            ModelState.Remove("CreatedByUser");

            NormalizeVaccinationRecord(vaccinationRecord);
            ValidateVaccinationDates(vaccinationRecord);

            if (!IsAdminOrStaff(roleId))
            {
                TempData["Error"] = "Only admin and staff can add, edit, or delete vaccination records.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Pets = BuildPetSelectList(vaccinationRecord.PetId);
                TempData["Error"] = "Please correct the vaccination dates before saving.";
                return View(vaccinationRecord);
            }

            _vaccinationService.Update(vaccinationRecord);

            TempData["Success"] = "Vaccination record updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var vaccination = _vaccinationService.GetById(id);
            if (vaccination == null)
                return NotFound();

            if (roleId == ClientRoleId)
            {
                var myPetIds = _petService.GetByUser(userId.Value).Select(p => p.Id).ToList();
                if (!myPetIds.Contains(vaccination.PetId))
                    return Unauthorized();
            }

            return View(vaccination);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (!IsAdminOrStaff(roleId))
            {
                TempData["Error"] = "Only admin and staff can add, edit, or delete vaccination records.";
                return RedirectToAction("Index");
            }

            var vaccination = _vaccinationService.GetById(id);
            if (vaccination == null)
                return NotFound();

            return View(vaccination);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (!IsAdminOrStaff(roleId))
            {
                TempData["Error"] = "Only admin and staff can add, edit, or delete vaccination records.";
                return RedirectToAction("Index");
            }

            var vaccination = _vaccinationService.GetById(id);
            if (vaccination == null)
                return NotFound();

            _vaccinationService.Delete(id);
            TempData["Success"] = "Vaccination record deleted successfully.";
            return RedirectToAction("Index");
        }

        private static bool IsAdminOrStaff(int? roleId)
        {
            return roleId == AdminRoleId || roleId == StaffRoleId;
        }

        private static void NormalizeVaccinationRecord(VaccinationRecord vaccinationRecord)
        {
            vaccinationRecord.VaccineName = vaccinationRecord.VaccineName?.Trim() ?? string.Empty;
            vaccinationRecord.Notes = string.IsNullOrWhiteSpace(vaccinationRecord.Notes)
                ? null
                : vaccinationRecord.Notes.Trim();
        }

        private void ValidateVaccinationDates(VaccinationRecord vaccinationRecord)
        {
            if (vaccinationRecord.VaccinationDate.DayOfWeek == DayOfWeek.Tuesday)
            {
                ModelState.AddModelError(
                    nameof(VaccinationRecord.VaccinationDate),
                    VaccinationTuesdayValidationMessage);
            }

            if (vaccinationRecord.NextDueDate.HasValue &&
                vaccinationRecord.NextDueDate.Value.DayOfWeek == DayOfWeek.Tuesday)
            {
                ModelState.AddModelError(
                    nameof(VaccinationRecord.NextDueDate),
                    NextDueTuesdayValidationMessage);
            }

            if (vaccinationRecord.NextDueDate.HasValue &&
                vaccinationRecord.NextDueDate.Value <= vaccinationRecord.VaccinationDate)
            {
                ModelState.AddModelError(
                    nameof(VaccinationRecord.NextDueDate),
                    NextDueDateValidationMessage);
            }
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