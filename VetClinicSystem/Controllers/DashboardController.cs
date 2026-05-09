using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using VetClinicSystem.Models;
using VetClinicSystem.Services.Appointments;
using VetClinicSystem.Services.MedicalRecords;
using VetClinicSystem.Services.Notifications;
using VetClinicSystem.Services.Pets;
using VetClinicSystem.Services.Users;
using VetClinicSystem.Services.Vaccinations;
using VetClinicSystem.ViewModels;

namespace VetClinicSystem.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IPetService _petService;
        private readonly IAppointmentService _appointmentService;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly IVaccinationService _vaccinationService;
        private readonly IMedicalRecordService _medicalRecordService;
        private readonly VetClinicDbContext _context;

        public DashboardController(
            IPetService petService,
            IAppointmentService appointmentService,
            INotificationService notificationService,
            IUserService userService,
            IVaccinationService vaccinationService,
            IMedicalRecordService medicalRecordService,
            VetClinicDbContext context)
        {
            _petService = petService;
            _appointmentService = appointmentService;
            _notificationService = notificationService;
            _userService = userService;
            _vaccinationService = vaccinationService;
            _medicalRecordService = medicalRecordService;
            _context = context;
        }

        public IActionResult Admin()
        {
            if (HttpContext.Session.GetInt32("UserId") == null || HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("Login", "Account");

            var appointments = _appointmentService.GetAll();
            var users = _userService.GetAll();

            ViewBag.TotalPets = _petService.GetAll().Count;
            ViewBag.TotalAppointments = appointments.Count;
            ViewBag.TotalUsers = users.Count;
            ViewBag.UnreadNotifications = _notificationService.GetUnreadForStaff().Count;

            var grouped = appointments
                .GroupBy(a => a.AppointmentDate)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key.ToString("MM/dd"),
                    Count = g.Count()
                })
                .ToList();

            ViewBag.ChartLabels = JsonSerializer.Serialize(grouped.Select(x => x.Date));
            ViewBag.ChartData = JsonSerializer.Serialize(grouped.Select(x => x.Count));

            return View();
        }

        public IActionResult Users(string? filter)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (currentUserId == null || roleId != 1)
                return RedirectToAction("Login", "Account");

            var normalizedFilter = NormalizeUserFilter(filter);
            var allUsers = _userService.GetAll();
            var filteredUsers = allUsers
                .Where(user => MatchesUserFilter(user, normalizedFilter))
                .ToList();

            var model = new AdminUsersViewModel
            {
                Users = filteredUsers,
                Filter = normalizedFilter,
                CurrentUserId = currentUserId.Value,
                ActiveCount = allUsers.Count(user => !user.IsDeleted && user.IsActive),
                InactiveCount = allUsers.Count(user => !user.IsDeleted && !user.IsActive),
                ArchivedCount = allUsers.Count(user => user.IsDeleted)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeactivateUser(int id)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (currentUserId == null || roleId != 1)
                return RedirectToAction("Login", "Account");

            if (id == currentUserId.Value)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction("Users");
            }

            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            if (user == null)
            {
                TempData["Error"] = "User account was not found.";
                return RedirectToAction("Users");
            }

            if (user.IsDeleted)
            {
                TempData["Error"] = "Archived accounts cannot be deactivated. Restore the account first if needed.";
                return RedirectToAction("Users", new { filter = "archived" });
            }

            user.IsActive = false;
            _context.SaveChanges();

            TempData["Success"] = "User account deactivated successfully.";
            return RedirectToAction("Users", new { filter = "inactive" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RestoreUser(int id)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (currentUserId == null || roleId != 1)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            if (user == null)
            {
                TempData["Error"] = "User account was not found.";
                return RedirectToAction("Users");
            }

            user.IsDeleted = false;
            user.IsActive = true;
            user.DeletedAt = null;
            user.DeletedByUserId = null;
            user.DeleteReason = null;
            _context.SaveChanges();

            TempData["Success"] = "User account restored successfully.";
            return RedirectToAction("Users", new { filter = "active" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ArchiveUser(int id, string? reason)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (currentUserId == null || roleId != 1)
                return RedirectToAction("Login", "Account");

            if (id == currentUserId.Value)
            {
                TempData["Error"] = "You cannot archive your own account.";
                return RedirectToAction("Users");
            }

            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            if (user == null)
            {
                TempData["Error"] = "User account was not found.";
                return RedirectToAction("Users");
            }

            user.IsDeleted = true;
            user.IsActive = false;
            user.DeletedAt = DateTime.Now;
            user.DeletedByUserId = currentUserId.Value;
            user.DeleteReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            _context.SaveChanges();

            TempData["Success"] = "User account archived successfully.";
            return RedirectToAction("Users", new { filter = "archived" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RestoreArchivedUser(int id)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (currentUserId == null || roleId != 1)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            if (user == null)
            {
                TempData["Error"] = "User account was not found.";
                return RedirectToAction("Users");
            }

            user.IsDeleted = false;
            user.IsActive = true;
            user.DeletedAt = null;
            user.DeletedByUserId = null;
            user.DeleteReason = null;
            _context.SaveChanges();

            TempData["Success"] = "Archived user account restored successfully.";
            return RedirectToAction("Users", new { filter = "active" });
        }

        public IActionResult Staff()
        {
            if (HttpContext.Session.GetInt32("UserId") == null || HttpContext.Session.GetInt32("RoleId") != 2)
                return RedirectToAction("Login", "Account");

            ViewBag.TotalAppointments = _appointmentService.GetAll().Count;
            ViewBag.UnreadNotifications = _notificationService.GetUnreadForStaff().Count;

            return View();
        }

        public IActionResult Client()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null || roleId != 3)
                return RedirectToAction("Login", "Account");

            var appointments = _appointmentService.GetByUser(userId.Value);
            var user = _userService.GetById(userId.Value);
            var petOwner = _userService.GetPetOwnerByUserId(userId.Value);
            var myPetIds = _petService.GetByUser(userId.Value).Select(p => p.Id).ToList();
            var todayDate = DateOnly.FromDateTime(DateTime.Today);

            ViewBag.TotalPets = _petService.GetByUser(userId.Value).Count;
            ViewBag.TotalAppointments = appointments.Count;
            ViewBag.CurrentUser = user;
            ViewBag.PetOwner = petOwner;
            ViewBag.UnreadNotifications = _notificationService.GetUnreadForUser(userId.Value).Count;
            ViewBag.UpcomingReminders = appointments
                .Where(x => x.AppointmentDate >= todayDate && x.AppointmentDate <= todayDate.AddDays(2))
                .OrderBy(x => x.AppointmentDate)
                .ThenBy(x => x.AppointmentTime)
                .Take(3)
                .ToList();
            ViewBag.FollowUpAppointments = appointments
                .Where(x => x.Status?.StatusName == "Completed")
                .OrderByDescending(x => x.AppointmentDate)
                .Take(2)
                .ToList();
            ViewBag.OverdueVaccinations = _vaccinationService.GetAll()
                .Where(x => myPetIds.Contains(x.PetId) && x.NextDueDate.HasValue && x.NextDueDate.Value < todayDate)
                .OrderBy(x => x.NextDueDate)
                .ToList();
            ViewBag.RecentTreatmentHistory = _medicalRecordService.GetAll()
                .Where(x => myPetIds.Contains(x.PetId))
                .OrderByDescending(x => x.RecordDate)
                .Take(3)
                .ToList();
            ViewBag.ClientAppointments = appointments
                .OrderBy(x => x.AppointmentDate)
                .ThenBy(x => x.AppointmentTime)
                .Select(x => new
                {
                    date = x.AppointmentDate.ToString("yyyy-MM-dd"),
                    day = x.AppointmentDate.Day,
                    title = x.Service?.ServiceName ?? "Appointment",
                    pet = x.Pet?.PetName ?? "Pet",
                    time = x.AppointmentTime.ToString("HH:mm"),
                    status = x.Status?.StatusName ?? "Scheduled",
                    reason = x.ReasonForVisit ?? "Clinic appointment"
                })
                .ToList();

            return View();
        }

        private static string NormalizeUserFilter(string? filter)
        {
            return filter?.Trim().ToLowerInvariant() switch
            {
                "active" => "active",
                "inactive" => "inactive",
                "archived" => "archived",
                _ => "all"
            };
        }

        private static bool MatchesUserFilter(User user, string filter)
        {
            return filter switch
            {
                "active" => !user.IsDeleted && user.IsActive,
                "inactive" => !user.IsDeleted && !user.IsActive,
                "archived" => user.IsDeleted,
                _ => true
            };
        }
    }
}
