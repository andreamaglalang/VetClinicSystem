# Soft Delete / Archive Changes

This file contains the complete updated code for every file changed while implementing account soft delete and archive support.

## Changed Files
- `Models/User.cs`
- `Services/Users/UserService.cs`
- `ViewModels/AdminUsersViewModel.cs`
- `Controllers/AccountController.cs`
- `Controllers/DashboardController.cs`
- `Views/Dashboard/Users.cshtml`
- `wwwroot/css/site.css`
- `Program.cs`

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Models\User.cs

`$lang
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

[Index("Username", Name = "UQ__Users__536C85E46EB1C654", IsUnique = true)]
[Index("Email", Name = "UQ__Users__A9D105346FFA82C4", IsUnique = true)]
public partial class User
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [MinLength(4, ErrorMessage = "Username must be at least 4 characters.")]
    [RegularExpression(@"^\S+$", ErrorMessage = "Username must not contain spaces.")]
    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public bool IsGuest { get; set; }

    public bool IsDeleted { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    [StringLength(255)]
    public string? DeleteReason { get; set; }

    public bool MustChangePassword { get; set; }

    public DateTime? LastPasswordChange { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    [InverseProperty("User")]
    public virtual PetOwner? PetOwner { get; set; }

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role Role { get; set; } = null!;

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<VaccinationRecord> VaccinationRecords { get; set; } = new List<VaccinationRecord>();
}

```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Services\Users\UserService.cs

`$lang
using VetClinicSystem.Helpers;
using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Users;

namespace VetClinicSystem.Services.Users
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly VetClinicDbContext _context;

        public UserService(IUserRepository userRepository, VetClinicDbContext context)
        {
            _userRepository = userRepository;
            _context = context;
        }

        public bool Register(string username, string email, string password, string firstName, string lastName, string contactNumber, string address)
        {
            if (_userRepository.GetByUsername(username) != null) return false;
            if (_userRepository.GetByEmail(email) != null) return false;

            var clientRole = _context.Roles.FirstOrDefault(x => x.RoleName == "Client");
            if (clientRole == null) return false;

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(password),
                RoleId = clientRole.Id,
                IsActive = true,
                IsDeleted = false,
                DateCreated = DateTime.Now
            };

            _userRepository.Add(user);
            _userRepository.Save();

            var petOwner = new PetOwner
            {
                UserId = user.Id,
                FirstName = firstName,
                LastName = lastName,
                ContactNumber = contactNumber,
                Address = address,
                DateCreated = DateTime.Now
            };

            _context.PetOwners.Add(petOwner);
            _context.SaveChanges();

            return true;
        }

        public User? Login(string username, string password)
        {
            var user = _userRepository.GetByUsername(username);
            if (user == null) return null;
            if (!user.IsActive || user.IsDeleted) return null;

            var hashed = PasswordHelper.HashPassword(password);
            if (user.PasswordHash != hashed) return null;

            return user;
        }

        public List<User> GetAll()
        {
            return _userRepository.GetAll();
        }

        public User? GetById(int id)
        {
            return _userRepository.GetById(id);
        }

        public PetOwner? GetPetOwnerByUserId(int userId)
        {
            return _userRepository.GetPetOwnerByUserId(userId);
        }
    }
}

```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\ViewModels\AdminUsersViewModel.cs

`$lang
using VetClinicSystem.Models;

namespace VetClinicSystem.ViewModels
{
    public class AdminUsersViewModel
    {
        public List<User> Users { get; set; } = new();

        public string Filter { get; set; } = "all";

        public int CurrentUserId { get; set; }

        public int ActiveCount { get; set; }

        public int InactiveCount { get; set; }

        public int ArchivedCount { get; set; }
    }
}

```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Controllers\AccountController.cs

`$lang
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using VetClinicSystem.Helpers;
using VetClinicSystem.Models;
using VetClinicSystem.Services.Users;
using VetClinicSystem.ViewModels;

namespace VetClinicSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly VetClinicDbContext _context;

        public AccountController(IUserService userService, VetClinicDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                var roleId = HttpContext.Session.GetInt32("RoleId");

                if (roleId == 1)
                    return RedirectToAction("Admin", "Dashboard");

                if (roleId == 2)
                    return RedirectToAction("Staff", "Dashboard");

                if (roleId == 3)
                    return RedirectToAction("Client", "Dashboard");

                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            model.Username = model.Username?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
                return View(model);

            var existingUser = _context.Users.FirstOrDefault(x => x.Username == model.Username);

            if (existingUser != null && existingUser.IsDeleted)
            {
                ViewBag.Error = "This account has been archived. Please contact the clinic.";
                return View(model);
            }

            if (existingUser != null && !existingUser.IsActive)
            {
                ViewBag.Error = "Your account has been deactivated. Please contact the clinic.";
                return View(model);
            }

            var user = _userService.Login(model.Username, model.Password);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("RoleId", user.RoleId);

            if (user.RoleId == 1)
                return RedirectToAction("Admin", "Dashboard");

            if (user.RoleId == 2)
                return RedirectToAction("Staff", "Dashboard");

            if (user.RoleId == 3)
                return RedirectToAction("Client", "Dashboard");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            model.Username = model.Username?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim() ?? string.Empty;
            model.FirstName = model.FirstName?.Trim() ?? string.Empty;
            model.LastName = model.LastName?.Trim() ?? string.Empty;
            model.ContactNumber = PhoneNumberHelper.Normalize(model.ContactNumber);
            model.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();

            if (!ModelState.IsValid)
                return View(model);

            if (_context.Users.Any(x => x.Username == model.Username))
                ModelState.AddModelError(nameof(RegisterViewModel.Username), "Username is already taken.");

            if (_context.Users.Any(x => x.Email == model.Email))
                ModelState.AddModelError(nameof(RegisterViewModel.Email), "Email is already registered.");

            if (!ModelState.IsValid)
                return View(model);

            var success = _userService.Register(model.Username, model.Email, model.Password, model.FirstName, model.LastName, model.ContactNumber, model.Address ?? string.Empty);

            if (!success)
            {
                ViewBag.Error = "Username or email already exists, or Client role is missing.";
                return View(model);
            }

            TempData["Success"] = "Registration successful. Please login.";
            return RedirectToAction("Login");
        }

        public IActionResult Settings()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId.Value);

            if (user == null)
                return RedirectToAction("Login");

            PrepareSettingsViewData(user.Id);

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(User model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId.Value);

            if (user == null)
                return RedirectToAction("Login");

            var currentPassword = Request.Form["CurrentPassword"].ToString();
            var newPassword = Request.Form["NewPassword"].ToString();
            var confirmPassword = Request.Form["ConfirmPassword"].ToString();
            var firstName = Request.Form["FirstName"].ToString().Trim();
            var lastName = Request.Form["LastName"].ToString().Trim();
            var contactNumber = Request.Form["ContactNumber"].ToString().Trim();
            var normalizedUsername = model.Username?.Trim() ?? string.Empty;
            var normalizedEmail = model.Email?.Trim() ?? string.Empty;
            var petOwner = _context.PetOwners.FirstOrDefault(x => x.UserId == user.Id);
            var hasValidationError = false;

            if (petOwner != null)
            {
                if (string.IsNullOrWhiteSpace(firstName))
                {
                    ViewBag.FirstNameError = "First name is required.";
                    hasValidationError = true;
                }

                if (string.IsNullOrWhiteSpace(lastName))
                {
                    ViewBag.LastNameError = "Last name is required.";
                    hasValidationError = true;
                }

                contactNumber = PhoneNumberHelper.Normalize(contactNumber);

                if (!PhoneNumberHelper.IsValidPhilippineMobileNumber(contactNumber))
                {
                    ViewBag.ContactNumberError = PhoneNumberHelper.ValidationMessage;
                    hasValidationError = true;
                }
            }

            if (string.IsNullOrWhiteSpace(model.Username))
            {
                ModelState.AddModelError("Username", "Username is required.");
                hasValidationError = true;
            }
            else if (normalizedUsername.Length < 4)
            {
                ModelState.AddModelError("Username", "Username must be at least 4 characters.");
                hasValidationError = true;
            }
            else if (model.Username.Any(char.IsWhiteSpace))
            {
                ModelState.AddModelError("Username", "Username cannot contain spaces.");
                hasValidationError = true;
            }

            if (!string.IsNullOrWhiteSpace(normalizedUsername))
            {
                var usernameExists = _context.Users
                    .Any(u => u.Username == normalizedUsername && u.Id != user.Id);

                if (usernameExists)
                {
                    ModelState.AddModelError("Username", "Username is already taken. Please choose another username.");
                    hasValidationError = true;
                }
            }

            if (string.IsNullOrWhiteSpace(model.Email) || !IsValidGmailAddress(model.Email))
            {
                ModelState.AddModelError("Email", "Please enter a valid Gmail address.");
                hasValidationError = true;
            }
            else
            {
                var emailExists = _context.Users
                    .Any(u => u.Email == normalizedEmail && u.Id != user.Id);

                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    hasValidationError = true;
                }
            }

            bool changingPassword =
                !string.IsNullOrWhiteSpace(currentPassword) ||
                !string.IsNullOrWhiteSpace(newPassword) ||
                !string.IsNullOrWhiteSpace(confirmPassword);

            if (changingPassword)
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    ViewBag.CurrentPasswordError = "Current password is required.";
                    hasValidationError = true;
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    ViewBag.NewPasswordError = "New password is required.";
                    hasValidationError = true;
                }
                else if (!IsStrongPassword(newPassword))
                {
                    ViewBag.NewPasswordError = "Password must contain at least 8 characters, including uppercase, lowercase, number, and special character.";
                    hasValidationError = true;
                }

                if (newPassword != confirmPassword)
                {
                    ViewBag.ConfirmPasswordError = "Passwords do not match.";
                    hasValidationError = true;
                }

                if (!string.IsNullOrWhiteSpace(currentPassword))
                {
                    var currentHash = PasswordHelper.HashPassword(currentPassword);

                    if (user.PasswordHash != currentHash)
                    {
                        ViewBag.CurrentPasswordError = "Current password is incorrect.";
                        hasValidationError = true;
                    }
                }
            }

            if (!ModelState.IsValid)
                hasValidationError = true;

            if (hasValidationError)
            {
                PrepareSettingsViewData(user.Id, firstName, lastName, contactNumber);
                return View(model);
            }

            user.Username = normalizedUsername;
            user.Email = normalizedEmail;

            if (petOwner != null)
            {
                petOwner.FirstName = firstName;
                petOwner.LastName = lastName;
                petOwner.ContactNumber = contactNumber;
            }

            if (changingPassword)
            {
                user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                user.LastPasswordChange = DateTime.Now;
                user.MustChangePassword = false;
            }

            _context.SaveChanges();

            HttpContext.Session.SetString("Username", user.Username);

            TempData["Success"] = "Account updated successfully.";

            return RedirectToAction("Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Deactivate()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId.Value);

            if (user == null)
                return RedirectToAction("Login");

            var deactivateUsername = Request.Form["DeactivateUsername"].ToString().Trim();
            var deactivatePassword = Request.Form["DeactivatePassword"].ToString();
            var deactivateConfirmation = Request.Form["DeactivateConfirmation"].ToString().Trim();
            var hasValidationError = false;

            if (!string.Equals(deactivateUsername, user.Username, StringComparison.Ordinal))
            {
                ViewBag.DeactivateUsernameError = "The username does not match your current account.";
                hasValidationError = true;
            }

            if (string.IsNullOrWhiteSpace(deactivatePassword))
            {
                ViewBag.DeactivatePasswordError = "Please enter your password.";
                hasValidationError = true;
            }
            else if (user.PasswordHash != PasswordHelper.HashPassword(deactivatePassword))
            {
                ViewBag.DeactivatePasswordError = "The password you entered is incorrect.";
                hasValidationError = true;
            }

            if (!string.Equals(deactivateConfirmation, "DEACTIVATE", StringComparison.Ordinal))
            {
                ViewBag.DeactivateConfirmationError = "Please type DEACTIVATE exactly to confirm.";
                hasValidationError = true;
            }

            if (hasValidationError)
            {
                ViewBag.DeactivateUsername = deactivateUsername;
                ViewBag.DeactivateConfirmation = deactivateConfirmation;
                PrepareSettingsViewData(user.Id);
                return View("Settings", user);
            }

            user.IsActive = false;
            _context.SaveChanges();

            HttpContext.Session.Clear();

            TempData["Success"] = "Your account has been deactivated.";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private static bool IsValidGmailAddress(string email)
        {
            return Regex.IsMatch(
                email.Trim(),
                @"^[A-Za-z0-9._%+-]+@gmail\.com$",
                RegexOptions.IgnoreCase);
        }

        private static bool IsStrongPassword(string password)
        {
            return password.Length >= 8 &&
                password.Any(char.IsUpper) &&
                password.Any(char.IsLower) &&
                password.Any(char.IsDigit) &&
                password.Any(ch => !char.IsLetterOrDigit(ch));
        }

        private void PrepareSettingsViewData(int userId, string? firstName = null, string? lastName = null, string? contactNumber = null)
        {
            var petOwner = _context.PetOwners.FirstOrDefault(x => x.UserId == userId);

            ViewBag.PetOwner = petOwner;
            ViewBag.FirstName = firstName ?? petOwner?.FirstName;
            ViewBag.LastName = lastName ?? petOwner?.LastName;
            ViewBag.ContactNumber = contactNumber ?? petOwner?.ContactNumber;
        }
    }
}

```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Controllers\DashboardController.cs

`$lang
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

```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Views\Dashboard\Users.cshtml

`$lang
@model VetClinicSystem.ViewModels.AdminUsersViewModel

@{
    ViewData["Title"] = "Users";
}

<section class="admin-users-panel">
    <div class="admin-users-panel__header">
        <div>
            <h4>Users</h4>
            <p>Manage active, inactive, and archived accounts without deleting related clinic records.</p>
        </div>
        <span>@Model.Users.Count shown</span>
    </div>

    <div class="admin-users-filters" role="tablist" aria-label="User status filters">
        <a asp-action="Users" asp-controller="Dashboard" asp-route-filter="all" class="admin-users-filter @(Model.Filter == "all" ? "is-active" : null)">
            All
        </a>
        <a asp-action="Users" asp-controller="Dashboard" asp-route-filter="active" class="admin-users-filter @(Model.Filter == "active" ? "is-active" : null)">
            Active
            <span>@Model.ActiveCount</span>
        </a>
        <a asp-action="Users" asp-controller="Dashboard" asp-route-filter="inactive" class="admin-users-filter @(Model.Filter == "inactive" ? "is-active" : null)">
            Inactive
            <span>@Model.InactiveCount</span>
        </a>
        <a asp-action="Users" asp-controller="Dashboard" asp-route-filter="archived" class="admin-users-filter @(Model.Filter == "archived" ? "is-active" : null)">
            Archived
            <span>@Model.ArchivedCount</span>
        </a>
    </div>

    @if (!Model.Users.Any())
    {
        <div class="alert alert-info">No users found for this filter.</div>
    }
    else
    {
        <div class="table-panel">
            <table class="table table-bordered table-striped admin-users-table">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Username</th>
                        <th>Email</th>
                        <th>Role</th>
                        <th>Status</th>
                        <th>Date Created</th>
                        <th>Archive Info</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                @foreach (var user in Model.Users)
                {
                    var displayName = user.PetOwner != null
                        ? $"{user.PetOwner.FirstName} {user.PetOwner.LastName}".Trim()
                        : user.Username;

                    <tr class="@(user.IsDeleted ? "admin-users-row--archived" : null)">
                        <td>@displayName</td>
                        <td>@user.Username</td>
                        <td>@user.Email</td>
                        <td>@(user.Role?.RoleName ?? "User")</td>
                        <td>
                            @if (user.IsDeleted)
                            {
                                <span class="status-pill status-pill--archived">Archived</span>
                            }
                            else if (user.IsActive)
                            {
                                <span class="status-pill status-pill--success">Active</span>
                            }
                            else
                            {
                                <span class="status-pill status-pill--muted">Inactive</span>
                            }
                        </td>
                        <td>@user.DateCreated.ToString("MMM d, yyyy")</td>
                        <td>
                            @if (user.IsDeleted)
                            {
                                <div class="admin-users-meta">
                                    <span>@(user.DeletedAt?.ToString("MMM d, yyyy h:mm tt") ?? "Archived")</span>
                                    @if (!string.IsNullOrWhiteSpace(user.DeleteReason))
                                    {
                                        <small>@user.DeleteReason</small>
                                    }
                                </div>
                            }
                            else
                            {
                                <span class="text-muted">Not archived</span>
                            }
                        </td>
                        <td>
                            @if (user.Id == Model.CurrentUserId)
                            {
                                <span class="text-muted">Current account</span>
                            }
                            else
                            {
                                <div class="admin-user-actions">
                                    @if (user.IsDeleted)
                                    {
                                        <form asp-action="RestoreArchivedUser" asp-controller="Dashboard" method="post" class="admin-user-action-form" data-admin-user-confirm="Are you sure you want to restore this archived user account?">
                                            @Html.AntiForgeryToken()
                                            <input type="hidden" name="id" value="@user.Id" />
                                            <button type="submit" class="btn btn-success btn-sm">Restore Archived</button>
                                        </form>
                                    }
                                    else if (user.IsActive)
                                    {
                                        <form asp-action="DeactivateUser" asp-controller="Dashboard" method="post" class="admin-user-action-form" data-admin-user-confirm="Are you sure you want to deactivate this user account?">
                                            @Html.AntiForgeryToken()
                                            <input type="hidden" name="id" value="@user.Id" />
                                            <button type="submit" class="btn btn-warning btn-sm">Deactivate</button>
                                        </form>

                                        <form asp-action="ArchiveUser" asp-controller="Dashboard" method="post" class="admin-user-action-form" data-admin-user-confirm="Are you sure you want to archive this user account?" data-admin-user-archive="true">
                                            @Html.AntiForgeryToken()
                                            <input type="hidden" name="id" value="@user.Id" />
                                            <input type="hidden" name="reason" value="" data-admin-archive-reason />
                                            <button type="submit" class="btn btn-danger btn-sm">Archive</button>
                                        </form>
                                    }
                                    else
                                    {
                                        <form asp-action="RestoreUser" asp-controller="Dashboard" method="post" class="admin-user-action-form" data-admin-user-confirm="Are you sure you want to restore this inactive user account?">
                                            @Html.AntiForgeryToken()
                                            <input type="hidden" name="id" value="@user.Id" />
                                            <button type="submit" class="btn btn-success btn-sm">Restore Inactive</button>
                                        </form>

                                        <form asp-action="ArchiveUser" asp-controller="Dashboard" method="post" class="admin-user-action-form" data-admin-user-confirm="Are you sure you want to archive this user account?" data-admin-user-archive="true">
                                            @Html.AntiForgeryToken()
                                            <input type="hidden" name="id" value="@user.Id" />
                                            <input type="hidden" name="reason" value="" data-admin-archive-reason />
                                            <button type="submit" class="btn btn-danger btn-sm">Archive</button>
                                        </form>
                                    }
                                </div>
                            }
                        </td>
                    </tr>
                }
                </tbody>
            </table>
        </div>
    }
</section>

<div class="admin-confirm-overlay" id="adminUserConfirmOverlay" hidden>
    <div class="admin-confirm-modal" role="dialog" aria-modal="true" aria-labelledby="adminUserConfirmTitle">
        <button type="button" class="admin-confirm-close" data-admin-confirm-cancel aria-label="Close confirmation">
            <i class="fa-solid fa-xmark"></i>
        </button>
        <span class="admin-confirm-icon"><i class="fa-solid fa-exclamation"></i></span>
        <h3 id="adminUserConfirmTitle">Confirm Action</h3>
        <p id="adminUserConfirmMessage"></p>
        <div class="admin-archive-reason" id="adminArchiveReasonWrap" hidden>
            <label for="adminArchiveReason" class="form-label">Archive reason (optional)</label>
            <textarea id="adminArchiveReason" class="form-control" rows="3" maxlength="255" placeholder="Add a short reason for archiving this account."></textarea>
        </div>
        <div class="admin-confirm-actions">
            <button type="button" class="btn btn-danger" data-admin-confirm-submit>Confirm</button>
            <button type="button" class="btn btn-outline-secondary" data-admin-confirm-cancel>Cancel</button>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        document.addEventListener("DOMContentLoaded", function () {
            const confirmOverlay = document.getElementById("adminUserConfirmOverlay");
            const confirmMessage = document.getElementById("adminUserConfirmMessage");
            const confirmSubmit = document.querySelector("[data-admin-confirm-submit]");
            const archiveReasonWrap = document.getElementById("adminArchiveReasonWrap");
            const archiveReason = document.getElementById("adminArchiveReason");
            let pendingUserActionForm = null;

            document.querySelectorAll("[data-admin-user-confirm]").forEach(function (form) {
                form.addEventListener("submit", function (event) {
                    if (form.dataset.confirmed === "true") {
                        return;
                    }

                    event.preventDefault();
                    pendingUserActionForm = form;
                    confirmMessage.textContent = form.dataset.adminUserConfirm;
                    const isArchive = form.dataset.adminUserArchive === "true";
                    archiveReasonWrap.hidden = !isArchive;

                    if (archiveReason) {
                        archiveReason.value = "";
                    }

                    confirmOverlay.hidden = false;
                    confirmSubmit.focus();
                });
            });

            function closeConfirm() {
                confirmOverlay.hidden = true;
                pendingUserActionForm = null;
                archiveReasonWrap.hidden = true;

                if (archiveReason) {
                    archiveReason.value = "";
                }
            }

            document.querySelectorAll("[data-admin-confirm-cancel]").forEach(function (button) {
                button.addEventListener("click", closeConfirm);
            });

            if (confirmOverlay) {
                confirmOverlay.addEventListener("click", function (event) {
                    if (event.target === confirmOverlay) {
                        closeConfirm();
                    }
                });
            }

            if (confirmSubmit) {
                confirmSubmit.addEventListener("click", function () {
                    if (!pendingUserActionForm) return;

                    const reasonField = pendingUserActionForm.querySelector("[data-admin-archive-reason]");
                    if (reasonField) {
                        reasonField.value = archiveReason ? archiveReason.value.trim() : "";
                    }

                    pendingUserActionForm.dataset.confirmed = "true";
                    pendingUserActionForm.requestSubmit();
                });
            }
        });
    </script>
}

```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\wwwroot\css\site.css

`$lang
:root {
    --brand: #087f8c;
    --brand-dark: #075866;
    --brand-soft: #e6f6f7;
    --accent: #f3b63f;
    --ink: #18323a;
    --muted: #647982;
    --line: #d9e7ea;
    --surface: #ffffff;
    --page: #f5f9fa;
    --danger: #d9534f;
    --success: #2f9e69;
    --shadow: 0 18px 45px rgba(24, 50, 58, 0.10);
    --shadow-sm: 0 8px 24px rgba(24, 50, 58, 0.08);
    --radius: 14px;
}

* {
    box-sizing: border-box;
}

html {
    min-height: 100%;
    font-size: 16px;
    scroll-behavior: smooth;
}

body {
    min-height: 100vh;
    margin: 0;
    background: var(--page);
    color: var(--ink);
    font-family: 'Poppins', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    font-weight: 400;
    letter-spacing: 0;
}

.toast-stack {
    position: fixed;
    inset: 0;
    z-index: 2000;
    display: grid;
    place-items: center;
    padding: 24px;
    background: rgba(24, 50, 58, 0.26);
    backdrop-filter: blur(2px);
    pointer-events: auto;
}

.vet-toast {
    position: relative;
    display: grid;
    justify-items: center;
    gap: 9px;
    width: min(330px, calc(100vw - 48px));
    min-height: 246px;
    padding: 32px 26px 24px;
    border: 1px solid rgba(126, 217, 217, 0.42);
    border-radius: 12px;
    background: #ffffff;
    box-shadow: 0 22px 58px rgba(24, 50, 58, 0.18);
    color: var(--ink);
    text-align: center;
    font-size: 14px;
    font-weight: 800;
    line-height: 1.45;
    animation: toast-pop-in 0.22s ease both;
}

.vet-toast.is-hiding {
    animation: toast-pop-out 0.22s ease both;
}

.vet-toast > i {
    width: 62px;
    height: 62px;
    border: 1.5px solid currentColor;
    border-radius: 50%;
    background: var(--toast-icon-bg, #f4fbfb);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 24px;
    box-shadow: none;
}

.vet-toast strong {
    color: #111111;
    margin-top: 4px;
    font-size: 17px;
    font-weight: 900;
    line-height: 1.2;
}

.vet-toast span {
    max-width: 270px;
    color: #111111;
    font-size: 12px;
    font-weight: 600;
    line-height: 1.55;
}

.vet-toast-action {
    width: 100%;
    min-height: 34px;
    margin-top: 14px;
    padding: 0 22px;
    border: 1px solid #b9c6c9;
    border-radius: 6px;
    background: #ffffff;
    color: #111111 !important;
    font-weight: 800;
    display: inline-flex;
    align-items: center;
    justify-content: center;
}

.vet-toast::after {
    content: "";
    position: absolute;
    left: 26px;
    right: 26px;
    bottom: 69px;
    height: 1px;
    background: #dfe8e9;
}

.vet-toast-action i {
    display: none;
}

.vet-toast-close {
    position: absolute;
    top: 12px;
    right: 12px;
    width: 34px;
    height: 34px;
    padding: 0;
    border: 0;
    border-radius: 50%;
    background: transparent;
    color: #9db2b2;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    transition: background 0.18s ease, color 0.18s ease;
}

.vet-toast-close:hover {
    background: transparent;
    color: var(--brand-dark);
}

.vet-toast--success {
    border-color: #d7efdf;
    background: #ffffff;
    color: #069978;
    --toast-icon-bg: #e9fbf0;
}

.vet-toast--error {
    border-color: #f4cccc;
    background: #ffffff;
    color: #ff4a42;
    --toast-icon-bg: #fff0f0;
}

.vet-toast--warning {
    border-color: #f3d890;
    background: #ffffff;
    color: #f0aa13;
    --toast-icon-bg: #fff8e6;
}

.vet-toast--info {
    border-color: #bfe3ed;
    background: #ffffff;
    color: #0d7fec;
    --toast-icon-bg: #edf6ff;
}

@keyframes toast-pop-in {
    from {
        opacity: 0;
        transform: translateY(10px) scale(0.96);
    }
    to {
        opacity: 1;
        transform: translateY(0) scale(1);
    }
}

@keyframes toast-pop-out {
    from {
        opacity: 1;
        transform: translateY(0) scale(1);
    }
    to {
        opacity: 0;
        transform: translateY(10px) scale(0.96);
    }
}

a {
    color: var(--brand);
}

a:hover {
    color: var(--brand-dark);
}

h1, h2, h3, h4, h5, h6 {
    color: var(--ink);
    font-weight: 700;
    letter-spacing: 0;
}

p {
    color: var(--muted);
    line-height: 1.7;
}

.main-header {
    position: sticky;
    top: 0;
    z-index: 1000;
    background: rgba(232, 250, 252, 0.96);
    backdrop-filter: blur(12px);
    padding: 14px 70px 10px;
    box-shadow: 0 10px 28px rgba(3, 139, 166, 0.08);
}

.top-clinic-bar {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    align-items: center;
    gap: 24px;
    margin-bottom: 16px;
}

.top-logo {
    display: flex;
    align-items: center;
    gap: 10px;
    color: #4c4949;
}

.brand-icon {
    width: 46px;
    height: 46px;
    border-radius: 50%;
    background: #ffffff;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    overflow: hidden;
}

.brand-icon::before {
    content: none;
}

.brand-icon img {
    display: block;
    width: 100%;
    height: 100%;
    object-fit: contain;
}

.top-logo strong {
    display: block;
    font-size: 18px;
    font-weight: 900;
    line-height: 1.05;
}

.top-logo small {
    display: block;
    color: #4c4949;
    font-size: 14px;
    font-weight: 800;
    line-height: 1.15;
}

.top-header-actions {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    align-items: center;
}

.top-header-actions .btn {
    min-height: 40px;
    padding: 9px 18px;
    border-radius: 999px !important;
}

.top-header-actions--empty {
    min-height: 40px;
    min-width: 1px;
}

.main-landing-nav {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 42px;
    flex-wrap: wrap;
    margin-top: 10px;
}

.main-landing-nav a {
    font-size: 14px;
    font-weight: 700;
    color: #4c4949;
    text-decoration: none;
    transition: 0.2s ease;
}

.main-landing-nav a:hover {
    color: #038ba6;
}

.main-header--public {
    padding-top: 16px;
    padding-bottom: 14px;
}

.main-header--account {
    padding-top: 16px;
    padding-bottom: 14px;
}

.main-header--public .top-clinic-bar {
    grid-template-columns: minmax(260px, auto) 1fr auto;
    gap: 28px;
    max-width: 1280px;
    margin: 0 auto 12px;
}

.main-header--account .top-clinic-bar {
    grid-template-columns: minmax(260px, auto) 1fr auto;
    gap: 28px;
    max-width: 1480px;
    margin: 0 auto 12px;
}

.main-header--public .top-logo {
    gap: 14px;
}

.main-header--account .top-logo {
    gap: 14px;
}

.main-header--public .brand-icon {
    width: 52px;
    height: 52px;
    flex: 0 0 52px;
    box-shadow: 0 8px 18px rgba(3, 139, 166, 0.08);
}

.main-header--account .brand-icon {
    width: 52px;
    height: 52px;
    flex: 0 0 52px;
    box-shadow: 0 8px 18px rgba(3, 139, 166, 0.08);
}

.main-header--public .top-logo strong {
    font-size: 19px;
}

.main-header--account .top-logo strong {
    font-size: 19px;
}

.main-header--public .top-logo small {
    font-size: 13px;
    font-weight: 700;
}

.main-header--account .top-logo small {
    font-size: 13px;
    font-weight: 700;
}

.main-landing-nav--public {
    max-width: 1280px;
    margin: 0 auto;
    padding-top: 12px;
    border-top: 1px solid rgba(126, 217, 217, 0.38);
    justify-content: center;
    gap: 14px 28px;
}

.main-landing-nav--account {
    max-width: 1480px;
    margin: 0 auto;
    padding-top: 12px;
    border-top: 1px solid rgba(126, 217, 217, 0.38);
    justify-content: center;
    gap: 14px 24px;
}

.main-landing-nav--public a {
    padding: 8px 14px;
    border-radius: 999px;
    font-size: 13px;
    font-weight: 800;
}

.main-landing-nav--account a {
    padding: 8px 14px;
    border-radius: 999px;
    font-size: 13px;
    font-weight: 800;
}

.page-shell {
    min-height: calc(100vh - 176px);
    padding: 0;
}

.app-container {
    width: min(1320px, calc(100% - 64px));
    margin: 0 auto;
    padding: 36px 0 56px;
}

.app-container--client-dashboard {
    width: min(1600px, calc(100% - 48px));
}

.app-container--standard > h1,
.app-container--standard > h2,
.app-container--standard > h3 {
    margin-bottom: 22px;
}

.app-container--standard > h2 {
    color: var(--teal);
    font-size: clamp(30px, 3vw, 42px);
    font-weight: 900;
}

.app-container--standard > form[method="post"] {
    max-width: 780px;
    margin: 0 auto;
    padding: 30px;
    border: 1px solid rgba(126, 217, 217, 0.35);
    border-radius: 16px;
    background: #ffffff;
    box-shadow: var(--shadow-sm);
}

.app-container--standard:has(> form[method="post"]) > h1,
.app-container--standard:has(> form[method="post"]) > h2,
.app-container--standard:has(> form[method="post"]) > h3,
.app-container--standard:has(> form[method="post"]) > .alert,
.app-container--standard:has(> form[method="post"]) > .text-danger {
    max-width: 780px;
    margin-left: auto;
    margin-right: auto;
}

.app-container--standard > form[method="post"] .btn {
    margin-top: 4px;
}

.account-settings-shell {
    width: min(100%, 820px);
    margin: 0 auto;
}

.account-settings-header {
    margin-bottom: 22px;
}

.account-settings-header h1 {
    margin: 0;
    color: var(--teal);
    font-size: clamp(34px, 4vw, 52px);
    font-weight: 900;
}

.account-settings-card {
    padding: 28px 30px 30px;
    border: 1px solid rgba(126, 217, 217, 0.38);
    border-radius: 18px;
    background: transparent;
    box-shadow: 0 18px 42px rgba(3, 139, 166, 0.10);
}

.account-settings-card form {
    margin: 0;
}

.account-settings-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 16px;
}

.account-settings-card hr {
    margin: 28px 0 24px;
    border-color: rgba(126, 217, 217, 0.42);
    opacity: 1;
}

.account-settings-card h4 {
    margin: 0 0 8px;
    color: var(--teal);
    font-size: 22px;
    font-weight: 900;
}

.account-settings-card .text-muted {
    margin-bottom: 20px;
    color: var(--muted) !important;
    font-size: 13px;
}

.account-settings-card .mb-3 {
    margin-bottom: 18px !important;
}

.account-settings-card .form-control {
    min-height: 44px;
}

.account-settings-card .btn {
    margin-top: 4px;
}

.account-settings-card .btn + .btn {
    margin-left: 10px;
}

.account-settings-section-actions {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-top: 6px;
    margin-bottom: 4px;
}

.account-settings-actions {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-top: 10px;
    padding-bottom: 6px;
}

.account-section-divider {
    margin: 36px 0 30px !important;
}

.account-danger-zone {
    margin-top: 0;
    padding: 0;
    border: 0;
    border-radius: 0;
    background: transparent;
}

.account-danger-zone h4 {
    margin: 0 0 8px;
    color: #9b3d3d;
    font-size: 22px;
    font-weight: 900;
}

.account-danger-zone p {
    margin: 0 0 20px;
    color: var(--muted);
    font-size: 13px;
    line-height: 1.6;
    max-width: 720px;
}

.account-danger-zone form {
    margin: 0;
    max-width: 520px;
}

.account-danger-zone .form-label {
    color: var(--teal);
}

.account-danger-zone .text-danger {
    display: block;
    margin-top: 6px;
    font-size: 12px;
    font-weight: 700;
}

.account-danger-zone .btn-danger {
    margin-top: 6px;
}

.admin-user-action-form {
    margin: 0;
}

.admin-users-table td:last-child {
    white-space: nowrap;
}

.admin-confirm-overlay {
    position: fixed;
    inset: 0;
    z-index: 2050;
    display: grid;
    place-items: center;
    padding: 24px;
    background: rgba(24, 50, 58, 0.26);
    backdrop-filter: blur(2px);
}

.admin-confirm-overlay[hidden] {
    display: none;
}

.admin-confirm-modal {
    position: relative;
    display: grid;
    justify-items: center;
    gap: 9px;
    width: min(340px, calc(100vw - 48px));
    padding: 32px 26px 24px;
    border: 1px solid rgba(126, 217, 217, 0.42);
    border-radius: 12px;
    background: #ffffff;
    text-align: center;
    box-shadow: 0 22px 58px rgba(24, 50, 58, 0.18);
}

.admin-confirm-close {
    position: absolute;
    top: 12px;
    right: 12px;
    width: 34px;
    height: 34px;
    padding: 0;
    border: 0;
    border-radius: 50%;
    background: transparent;
    color: #9db2b2;
    display: inline-flex;
    align-items: center;
    justify-content: center;
}

.admin-confirm-close:hover {
    background: transparent;
    color: #735515;
}

.admin-confirm-icon {
    width: 62px;
    height: 62px;
    border: 1.5px solid var(--accent);
    border-radius: 50%;
    background: #fff8e6;
    color: #b88914;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 24px;
    box-shadow: none;
}

.admin-confirm-modal h3 {
    margin: 0;
    color: #111111;
    margin-top: 4px;
    font-size: 17px;
    font-weight: 900;
    line-height: 1.2;
}

.admin-confirm-modal p {
    max-width: 290px;
    margin: 0;
    color: #111111;
    font-size: 12px;
    font-weight: 600;
    line-height: 1.55;
}

.admin-confirm-actions {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 10px;
    width: 100%;
    margin-top: 22px;
    padding-top: 10px;
    border-top: 1px solid #dfe8e9;
}

.admin-confirm-actions .btn {
    flex: 1 1 120px;
    min-width: 0;
    min-height: 34px;
    border-radius: 6px !important;
    color: #111111 !important;
}

.app-container--standard > form[method="get"],
.app-container--standard > .alert,
.app-container--standard > p,
.app-container--standard > .row,
.app-container--standard > table,
.app-container--standard > h4,
.app-container--standard > canvas {
    max-width: 1280px;
}

.app-container--standard > form[method="get"] {
    padding: 18px;
    border: 1px solid rgba(126, 217, 217, 0.28);
    border-radius: 16px;
    background: #ffffff;
    box-shadow: var(--shadow-sm);
}

.table-panel {
    width: 100%;
    max-width: 1280px;
    margin: 0 auto 28px;
    overflow: hidden;
    padding: 18px;
    border: 1px solid rgba(126, 217, 217, 0.30);
    border-radius: 16px;
    background: #ffffff;
    box-shadow: var(--shadow-sm);
}

.table-panel > form[method="get"] {
    margin: 0 !important;
    padding: 0 0 16px;
    border: 0;
    border-radius: 0;
    background: #ffffff;
    box-shadow: none;
}

.table-panel > p {
    margin: 0;
    padding: 0 0 16px;
}

.table-panel > p:empty {
    display: none;
}

.table-panel > .table {
    width: 100%;
    min-width: 0;
    margin: 0;
    border-width: 1px;
    border-radius: 8px;
    box-shadow: none;
}

.table-panel > .table:first-child {
    border-top-width: 1px;
}

.table-panel > .table:last-child {
    border-bottom-width: 1px;
}

.app-container--standard .table {
    margin-bottom: 14px;
    border-color: #dff4f6;
}

.app-container--standard .table-panel > .table {
    width: 100%;
    min-width: 0;
    margin: 0;
    border-width: 1px;
    border-radius: 8px;
    box-shadow: none;
}

.app-container--standard .table > tbody > tr:nth-of-type(even) > * {
    background-color: #ffffff !important;
}

.app-container--standard .table > tbody > tr:nth-of-type(odd) > * {
    background-color: #ffffff !important;
}

.app-container--standard .table > tbody > tr:hover > * {
    background-color: #f8fbff !important;
}

.table-pagination {
    max-width: 1280px;
    margin: 0 0 28px;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 14px;
    flex-wrap: wrap;
}

.table-pagination__summary {
    color: #7f949b;
    font-size: 13px;
    font-weight: 700;
}

.table-pagination__buttons {
    display: flex;
    align-items: center;
    gap: 0;
    overflow: hidden;
    border: 1px solid rgba(3, 139, 166, 0.16);
    border-radius: 16px;
    background: #ffffff;
    box-shadow: 0 7px 18px rgba(30, 126, 146, 0.14);
}

.table-pagination button {
    width: 40px;
    height: 36px;
    padding: 0;
    border: 0;
    border-radius: 0;
    background: #ffffff;
    color: var(--teal);
    font-size: 26px;
    font-weight: 800;
    transition: background 0.18s ease, border-color 0.18s ease, color 0.18s ease, transform 0.18s ease;
}

.table-pagination button:hover:not(:disabled) {
    background: #eefdff;
    color: var(--teal-strong);
}

.table-pagination button:disabled {
    cursor: not-allowed;
    color: #9fd7df;
    opacity: 0.65;
}

.table-pagination__page-status {
    min-width: 82px;
    height: 36px;
    border-right: 1px solid #edf1f7;
    border-left: 1px solid #edf1f7;
    color: #3f4f55;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-weight: 900;
    letter-spacing: 0;
    white-space: nowrap;
}

.vet-footer {
    border-top: 1px solid var(--line);
    background: #fff;
    padding: 20px 24px;
    color: var(--muted);
    font-size: 14px;
}

.vet-footer p {
    margin: 0;
}

.btn {
    border-radius: 10px !important;
    border: 1px solid transparent !important;
    font-weight: 700;
    padding: 10px 16px;
    min-height: 48px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    box-shadow: none !important;
    transition: background 0.18s ease, border-color 0.18s ease, color 0.18s ease, transform 0.18s ease;
}

.btn:hover {
    transform: translateY(-1px);
}

.btn-sm {
    min-height: 36px;
    padding: 7px 10px;
    font-size: 12px;
}

.btn-primary,
.btn-success,
.btn-info,
.btn-vet-primary {
    background: var(--brand) !important;
    border-color: var(--brand) !important;
    color: #fff !important;
}

.btn-primary:hover,
.btn-success:hover,
.btn-info:hover,
.btn-vet-primary:hover {
    background: var(--brand-dark) !important;
    border-color: var(--brand-dark) !important;
}

.btn-warning {
    background: var(--accent) !important;
    border-color: var(--accent) !important;
    color: #2e260f !important;
}

.btn-danger {
    background: var(--danger) !important;
    border-color: var(--danger) !important;
    color: #fff !important;
}

.btn-secondary,
.btn-outline-secondary,
.btn-vet-outline {
    background: #fff !important;
    border-color: var(--line) !important;
    color: var(--brand-dark) !important;
}

.btn-secondary:hover,
.btn-outline-secondary:hover,
.btn-vet-outline:hover {
    background: var(--brand-soft) !important;
    border-color: #b8dfe4 !important;
    color: var(--brand-dark) !important;
}

.btn-vet-secondary-accent {
    background: #fecd44 !important;
    border-color: #fecd44 !important;
    color: #4c4949 !important;
}

.btn-vet-secondary-accent:hover {
    background: #efba2f !important;
    border-color: #efba2f !important;
    color: #4c4949 !important;
}

.card,
.content-card,
.review-card,
.contact-card,
.price-table-card,
.gallery-card,
.specialist-name,
.specialist-main-photo,
.specialist-bio,
.auth-shell,
.auth-logo-card {
    border-radius: var(--radius) !important;
    border: 1px solid var(--line) !important;
    background: var(--surface);
    box-shadow: var(--shadow-sm);
}

.card {
    overflow: hidden;
}

.card-body {
    padding: 24px;
}

.bg-primary,
.bg-success,
.bg-danger,
.bg-warning {
    background: var(--surface) !important;
    color: var(--ink) !important;
}

.card.text-white h2,
.card.text-white h5,
.card.text-dark h2,
.card.text-dark h5 {
    color: var(--ink) !important;
}

.card.text-white h2,
.card.text-dark h2 {
    color: var(--brand-dark) !important;
    margin: 8px 0 0;
}

.table {
    width: 100%;
    min-width: 0;
    table-layout: auto;
    overflow: hidden;
    border-collapse: separate;
    border-spacing: 0;
    background: #fff;
    border: 1px solid #dff4f6;
    border-radius: var(--radius);
    box-shadow: var(--shadow-sm);
    --bs-table-striped-bg: #ffffff;
    --bs-table-striped-color: var(--ink);
    --bs-table-hover-bg: #f8fbff;
    --bs-table-hover-color: var(--ink);
}

.table-bordered > :not(caption) > * {
    border-width: 0;
}

.table thead th {
    background: #b6f2f2;
    color: #038ba6;
    border-bottom: 1px solid #a7eeee !important;
    font-size: 13px;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0;
    white-space: nowrap;
}

.table th:nth-child(1),
.table td:nth-child(1) {
    width: 13%;
    min-width: 100px;
}

.table th:nth-child(2),
.table td:nth-child(2) {
    width: 12%;
    min-width: 110px;
}

.table th:nth-child(3),
.table td:nth-child(3) {
    width: 20%;
    min-width: 150px;
}

.table th:nth-last-child(1),
.table td:nth-last-child(1) {
    width: 140px;
    min-width: 136px;
}

.table td,
.table th {
    vertical-align: middle;
    padding: 14px 18px;
    border-color: #edf1f7 !important;
    border-bottom: 1px solid #edf1f7 !important;
}

.table tbody td,
.table tbody th {
    color: #111111 !important;
}

.table tbody tr:last-child > * {
    border-bottom: 0 !important;
}

.table > tbody > tr:nth-of-type(odd) > * {
    background-color: #ffffff !important;
}

.table > tbody > tr:nth-of-type(even) > * {
    background-color: #ffffff !important;
}

.table > tbody > tr:hover > * {
    background-color: #f8fbff !important;
}

.table-actions {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: nowrap;
    min-width: max-content;
}

.table-action-note {
    margin-top: 8px;
    max-width: 210px;
    color: #6e8389;
    font-size: 11px;
    line-height: 1.45;
}

.table-appointments th:nth-child(1),
.table-appointments td:nth-child(1) {
    width: 10%;
    min-width: 90px;
}

.table-appointments th:nth-child(2),
.table-appointments td:nth-child(2) {
    width: 15%;
    min-width: 130px;
}

.table-appointments th:nth-child(3),
.table-appointments td:nth-child(3) {
    width: 14%;
    min-width: 132px;
}

.table-appointments th:nth-child(4),
.table-appointments td:nth-child(4) {
    width: 13%;
    min-width: 120px;
}

.table-appointments th:nth-child(5),
.table-appointments td:nth-child(5) {
    width: 23%;
    min-width: 220px;
}

.table-appointments th:nth-child(6),
.table-appointments td:nth-child(6) {
    width: 9%;
    min-width: 104px;
}

.table-appointments th:nth-child(7),
.table-appointments td:nth-child(7) {
    width: 9%;
    min-width: 104px;
}

.table-appointments th:nth-child(8),
.table-appointments td:nth-child(8) {
    width: 7%;
    min-width: 120px;
}

.table-appointments-time {
    white-space: normal;
}

.table-appointments-time__value {
    font-weight: 400;
    line-height: 1.35;
    white-space: nowrap;
}

.table-appointments .table-action-note {
    max-width: 180px;
}

.table-appointments td:nth-child(2),
.table-appointments td:nth-child(4),
.table-appointments td:nth-child(5) {
    vertical-align: top;
}

.table-medical-records th:nth-child(1),
.table-medical-records td:nth-child(1) {
    width: 13%;
}

.table-medical-records th:nth-child(2),
.table-medical-records td:nth-child(2),
.table-medical-records th:nth-child(3),
.table-medical-records td:nth-child(3) {
    width: 25%;
    min-width: 220px;
    white-space: normal;
}

.table-medical-records th:nth-child(4),
.table-medical-records td:nth-child(4) {
    width: 22%;
    min-width: 190px;
}

.table-medical-records th:nth-child(5),
.table-medical-records td:nth-child(5) {
    width: 15%;
    min-width: 136px;
}

.table-vaccinations th:nth-child(1),
.table-vaccinations td:nth-child(1) {
    width: 16%;
}

.table-vaccinations th:nth-child(2),
.table-vaccinations td:nth-child(2) {
    width: 28%;
    min-width: 220px;
}

.table-vaccinations th:nth-child(3),
.table-vaccinations td:nth-child(3),
.table-vaccinations th:nth-child(4),
.table-vaccinations td:nth-child(4) {
    width: 20%;
    min-width: 180px;
}

.table-vaccinations th:nth-child(5),
.table-vaccinations td:nth-child(5) {
    width: 16%;
    min-width: 136px;
}

.table-services th:nth-child(1),
.table-services td:nth-child(1) {
    width: 18%;
    min-width: 160px;
}

.table-services th:nth-child(2),
.table-services td:nth-child(2) {
    width: 38%;
    min-width: 300px;
    white-space: normal;
}

.table-services th:nth-child(3),
.table-services td:nth-child(3),
.table-services th:nth-child(4),
.table-services td:nth-child(4) {
    width: 15%;
    min-width: 150px;
}

.table-services th:nth-child(5),
.table-services td:nth-child(5) {
    width: 14%;
    min-width: 136px;
}

.table-notifications th:nth-child(1),
.table-notifications td:nth-child(1) {
    width: 46%;
    min-width: 360px;
    white-space: normal;
}

.table-notifications th:nth-child(2),
.table-notifications td:nth-child(2) {
    width: 22%;
    min-width: 190px;
}

.table-notifications th:nth-child(3),
.table-notifications td:nth-child(3) {
    width: 18%;
    min-width: 150px;
}

.table-notifications th:nth-child(4),
.table-notifications td:nth-child(4) {
    width: 14%;
    min-width: 120px;
}

.table-notifications tbody tr.notification-row td {
    transition: background-color 0.28s ease, color 0.28s ease, opacity 0.28s ease;
}

.table-notifications tbody tr.notification-unread td {
    background-color: #ffffff;
}

.table-notifications tbody tr.notification-read td {
    background-color: #eeeeee;
    color: #7a8087;
}

.table-notifications tbody tr.notification-read td:first-child {
    color: #666d75;
}

.table-notifications tbody tr.notification-read td:not(:nth-child(3)) {
    opacity: 0.7;
}

.table-notifications tbody tr.notification-row--updating td {
    opacity: 0.86;
}

.table-notifications tbody tr.notification-read .status-pill,
.table-notifications tbody tr.notification-unread .status-pill {
    opacity: 1;
}

.table-notifications tbody tr.notification-row--emergency td {
    position: relative;
}

.table-notifications tbody tr.notification-row--emergency.notification-unread td {
    background-color: #fff8f2;
}

.table-notifications tbody tr.notification-row--emergency.notification-unread:hover > td,
.table-notifications tbody tr.notification-row--emergency:hover > td {
    background-color: #fff2e7 !important;
}

.table-notifications tbody tr.notification-row--emergency.notification-read td {
    background-color: #f8f1eb;
}

.notification-priority-badge {
    display: inline-flex;
    align-items: center;
    margin: 0 0 10px;
    padding: 5px 10px;
    border-radius: 999px;
    background: #ffe6db;
    color: #b44c28;
    font-size: 11px;
    font-weight: 800;
    line-height: 1;
    text-transform: uppercase;
    letter-spacing: 0;
}

.table-panel > .table-pagination {
    max-width: none;
    margin: 0;
    padding: 16px 0 0;
    border: 0;
    border-top: 1px solid #edf1f7;
    border-radius: 0;
    background: transparent;
    box-shadow: none;
}

.table-action-icon {
    width: 30px;
    height: 30px;
    border-radius: 7px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    text-decoration: none;
    border: 1px solid transparent;
    transition: background 0.18s ease, border-color 0.18s ease, color 0.18s ease, transform 0.18s ease;
}

.table-action-icon:hover {
    transform: translateY(-1px);
}

.table-action-icon--disabled,
.table-action-icon--disabled:hover {
    cursor: not-allowed;
    opacity: 0.45;
    filter: grayscale(0.55);
    pointer-events: none;
    transform: none;
}

.table-action-icon--details {
    background: #eaf6ff;
    border-color: #cfe9ff;
    color: #2580c3;
}

.table-action-icon--details:hover {
    background: #d9efff;
    color: #176ba8;
}

.table-action-icon--edit {
    background: #e8f8ef;
    border-color: #ccefdc;
    color: #37a66c;
}

.table-action-icon--edit:hover {
    background: #d8f2e4;
    color: #248b55;
}

.table-action-icon--delete {
    background: #fff0ed;
    border-color: #ffd8d0;
    color: #ef7b62;
}

.table-action-icon--delete:hover {
    background: #ffe4de;
    color: #d95f46;
}

.table-action-icon--approve {
    background: #e8f8ef;
    border-color: #ccefdc;
    color: #37a66c;
}

.table-action-icon--reject {
    background: #fff3dc;
    border-color: #ffe3a9;
    color: #c48914;
}

.status-pill {
    min-width: 78px;
    padding: 6px 12px;
    border-radius: 999px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 11px;
    font-weight: 900;
    line-height: 1;
    letter-spacing: 0;
    text-transform: uppercase;
    border: 1px solid transparent;
    box-shadow: 0 8px 16px rgba(3, 139, 166, 0.08);
}

.status-pill--success {
    background: #dff8e6;
    border-color: #bfeccf;
    color: #24764a;
}

.status-pill--danger {
    background: #fde8e8;
    border-color: #f5c9c9;
    color: #9b3d3d;
}


.status-pill--warning {
    background: #fff4ce;
    border-color: #f2d889;
    color: #79570d;
}

.status-pill--info {
    background: #def8fb;
    border-color: #b9ebf0;
    color: #038ba6;
}

.status-pill--muted {
    background: #eef4f6;
    border-color: #d7e5e9;
    color: #56696f;
}

.status-pill--archived {
    background: #f2ecf7;
    border-color: #ddd0eb;
    color: #6a4b88;
}

.action-modal .modal-backdrop,
.modal-backdrop.show {
    opacity: 0.34;
}

.action-modal-dialog {
    width: min(100% - 32px, 760px);
    max-width: 760px;
}

.action-modal-content {
    border: 1px solid rgba(126, 217, 217, 0.42);
    border-radius: 18px;
    background: #ffffff;
    box-shadow: 0 28px 70px rgba(3, 139, 166, 0.22);
    overflow: hidden;
}

.action-modal-header {
    align-items: center;
    padding: 22px 26px 18px;
    border-bottom: 1px solid rgba(126, 217, 217, 0.26);
    background: linear-gradient(180deg, #f7feff 0%, #ffffff 100%);
}

.action-modal-header .modal-title {
    margin: 0;
    color: var(--teal);
    font-size: 24px;
    font-weight: 900;
}

.action-modal-body {
    max-height: min(72vh, 720px);
    overflow-y: auto;
    padding: 24px 26px 28px;
}

.action-modal-body > .alert {
    margin-bottom: 16px;
}

.action-modal-body > form,
.action-modal-body form[method="post"] {
    max-width: none;
    margin: 0;
    padding: 0;
    border: 0;
    border-radius: 0;
    background: transparent;
    box-shadow: none;
}

.action-modal-body table {
    width: 100%;
    margin-bottom: 18px;
}

.action-modal-body .table {
    box-shadow: none;
}

.action-modal-body .btn {
    margin-top: 4px;
}

.action-modal-body .btn + .btn,
.action-modal-body form .btn + .btn {
    margin-left: 8px;
}

.action-modal-loading {
    display: grid;
    place-items: center;
    min-height: 140px;
    color: var(--teal);
    font-weight: 800;
}

.action-modal-body .appointment-create-notes {
    max-width: none;
    margin-bottom: 18px;
}

@media (max-width: 900px) {
    .account-settings-grid {
        grid-template-columns: 1fr;
        gap: 0;
    }

    .account-settings-card {
        padding: 22px;
    }

    .account-danger-zone {
        grid-template-columns: 1fr;
    }

    .table-panel {
        width: 100%;
        min-width: 0;
        overflow-x: auto;
    }

    .table-panel > form[method="get"],
    .table-panel > p,
    .table-panel > .table,
    .app-container--standard .table-panel > .table,
    .table-panel > .table-pagination {
        min-width: 760px;
    }
}

@media (max-width: 760px) {
    .app-container {
        width: min(100% - 28px, 1320px);
    }
}

.form-control,
.form-select,
select,
textarea,
input {
    border-radius: 10px !important;
    border: 1px solid #cfdfe3 !important;
    color: var(--ink);
    min-height: 44px;
}

.form-control:focus,
.form-select:focus,
select:focus,
textarea:focus,
input:focus {
    border-color: var(--brand) !important;
    box-shadow: 0 0 0 0.2rem rgba(8, 127, 140, 0.14) !important;
}

label,
.form-label {
    color: var(--ink);
    font-weight: 700;
    margin-bottom: 7px;
}

.alert {
    border: 1px solid transparent;
    border-radius: var(--radius);
    font-weight: 600;
}

.alert-success {
    background: #e8f7ef;
    border-color: #c9ebd8;
    color: #236a49;
}

.alert-danger {
    background: #fdecec;
    border-color: #f5caca;
    color: #963b38;
}

.alert-warning {
    background: #fff7df;
    border-color: #f4dda1;
    color: #735515;
}

.alert-info {
    background: var(--brand-soft);
    border-color: #c7e7eb;
    color: var(--brand-dark);
}

.vet-hero {
    min-height: 560px;
    display: grid;
    align-items: center;
    padding: 72px 7vw 86px;
    background: linear-gradient(rgba(8, 55, 64, 0.66), rgba(8, 55, 64, 0.44)), url('/images/site/vet-checkup-dachshund.jpg') center 42%/cover no-repeat;
}

.vet-hero-content {
    width: min(700px, 100%);
}

.vet-kicker {
    margin-bottom: 14px;
    color: #d8fbff;
    font-size: 13px;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0;
}

.vet-hero h1 {
    color: #fff;
    font-size: clamp(38px, 5vw, 64px);
    line-height: 1.08;
    margin-bottom: 18px;
}

.vet-hero p {
    max-width: 560px;
    color: rgba(255, 255, 255, 0.88);
    font-size: 18px;
}

.vet-hero-buttons {
    display: flex;
    flex-wrap: wrap;
    gap: 12px;
    margin-top: 28px;
}

.vet-section {
    width: min(1220px, calc(100% - 48px));
    margin: 0 auto;
    padding: 72px 0;
}

.section-heading {
    margin-bottom: 30px;
}

.section-heading.text-center p {
    margin-left: auto;
    margin-right: auto;
}

.section-heading h2 {
    margin-bottom: 10px;
    font-size: clamp(28px, 3vw, 38px);
}

.section-heading p {
    max-width: 720px;
    margin-bottom: 0;
}

.vet-carousel,
.gallery-carousel {
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 14px;
}

.carousel-window,
.gallery-frame {
    width: min(100%, 1060px);
    overflow: hidden;
    padding: 12px 4px 28px;
}

.carousel-track,
.gallery-track {
    display: flex;
    align-items: stretch;
    gap: 22px;
    transition: transform 0.35s ease;
}

.gallery-card {
    min-width: 250px;
    padding: 24px;
    transition: transform 0.2s ease, border-color 0.2s ease;
}

.gallery-card:hover,
.gallery-card.active {
    border-color: #abd8de !important;
    transform: translateY(-3px);
}

.gallery-img {
    width: 54px;
    height: 54px;
    margin-bottom: 18px;
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--brand-soft);
    color: var(--brand-dark);
    font-size: 24px;
}

.gallery-card h4,
.review-card h4 {
    margin-bottom: 10px;
    font-size: 20px;
}

.price-layout,
.review-grid,
.contact-area {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 24px;
}

.price-photo {
    min-height: 320px;
    border-radius: var(--radius);
    background: linear-gradient(rgba(8, 55, 64, 0.14), rgba(8, 55, 64, 0.14)), url('/images/site/pet-vaccination.jpg') center 48%/cover no-repeat;
    box-shadow: var(--shadow-sm);
}

.price-table-card,
.review-card,
.contact-card {
    padding: 28px;
}

.specialist-layout-clean {
    display: grid;
    grid-template-columns: 310px minmax(0, 1fr);
    gap: 28px;
    align-items: start;
}

.specialist-list {
    display: grid;
    gap: 14px;
}

.specialist-name {
    width: 100%;
    min-height: 82px;
    padding: 16px;
    display: flex;
    align-items: center;
    gap: 14px;
    text-align: left;
    color: var(--ink);
    cursor: pointer;
}

.specialist-name span {
    width: 42px;
    height: 42px;
    border-radius: 12px;
    background: var(--brand-soft);
    color: var(--brand-dark);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex: 0 0 auto;
}

.specialist-name strong,
.specialist-name small {
    display: block;
}

.specialist-name small {
    margin-top: 4px;
    color: var(--muted);
    font-size: 12px;
}

.specialist-name.active {
    border-color: var(--brand) !important;
    background: var(--brand-soft);
}

.specialist-content-area {
    position: relative;
    display: grid;
    grid-template-columns: 0.9fr 1.1fr;
    gap: 24px;
}

.specialist-main-photo {
    min-height: 360px;
    background: linear-gradient(rgba(8, 55, 64, 0.08), rgba(8, 55, 64, 0.08)), url('/images/site/cat-exam.jpg') center 54%/cover no-repeat;
}

.specialist-bio {
    min-height: 360px;
    padding: 32px;
}

.specialist-bio h3 {
    margin-bottom: 4px;
}

.specialist-bio h5 {
    color: var(--brand);
    font-size: 16px;
    margin-bottom: 18px;
}

.specialist-bio h6 {
    margin-top: 24px;
    color: var(--ink);
}

.carousel-arrow,
.gallery-arrow,
.specialist-side-arrow {
    width: 42px;
    height: 42px;
    border: 1px solid var(--line) !important;
    border-radius: 10px;
    background: #fff;
    color: var(--brand-dark);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 22px;
    line-height: 1;
    box-shadow: var(--shadow-sm);
    cursor: pointer;
}

.specialist-side-arrow {
    position: absolute;
    top: 50%;
    transform: translateY(-50%);
    z-index: 2;
}

.specialist-left {
    left: -18px;
}

.specialist-right {
    right: -18px;
}

.carousel-dots {
    display: flex;
    justify-content: center;
    gap: 8px;
    margin-top: 14px;
}

.carousel-dots .dot {
    width: 9px;
    height: 9px;
    padding: 0;
    border: none !important;
    border-radius: 999px;
    background: #b8d2d7;
}

.carousel-dots .dot.active {
    width: 24px;
    background: var(--brand);
}

.gallery-frame {
    width: min(100%, 1060px);
}

.photo-card {
    min-width: 320px;
    height: 220px;
    border-radius: var(--radius);
    overflow: hidden;
    background: #fff;
    border: 1px solid var(--line);
    box-shadow: var(--shadow-sm);
    opacity: 0.58;
    transform: scale(0.92);
    transition: opacity 0.25s ease, transform 0.25s ease;
}

.photo-card.active {
    opacity: 1;
    transform: scale(1);
}

.photo-card img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
}

.motion-card {
    position: relative;
}

.motion-card::before {
    content: "\f04b";
    position: absolute;
    top: 14px;
    right: 14px;
    z-index: 2;
    width: 34px;
    height: 34px;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.9);
    color: var(--brand-dark);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-family: "Font Awesome 6 Free";
    font-size: 12px;
    font-weight: 900;
}

.motion-card span {
    position: absolute;
    left: 14px;
    bottom: 14px;
    z-index: 2;
    padding: 6px 10px;
    border-radius: 999px;
    background: rgba(8, 55, 64, 0.78);
    color: #fff;
    font-size: 12px;
    font-weight: 700;
}

.map-box,
.map-placeholder {
    min-height: 220px;
    margin-top: 18px;
    border-radius: var(--radius);
    background: linear-gradient(rgba(8, 127, 140, 0.16), rgba(8, 127, 140, 0.16)), url('/images/site/diagnostic-care.jpg') center 52%/cover no-repeat;
    color: #fff;
    display: flex;
    align-items: flex-end;
    padding: 18px;
    font-weight: 800;
}

.auth-page {
    min-height: calc(100vh - 178px);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 56px 24px;
}

.auth-shell {
    width: min(1060px, 100%);
    display: grid;
    grid-template-columns: 0.95fr 1.05fr;
    overflow: hidden;
}

.auth-form-panel {
    padding: 52px;
    background: #fff;
}

.auth-kicker {
    margin-bottom: 8px;
    color: var(--brand);
    font-size: 13px;
    font-weight: 800;
    text-transform: uppercase;
}

.auth-form-panel h2 {
    font-size: clamp(32px, 4vw, 44px);
    margin-bottom: 8px;
}

.auth-subtitle {
    margin-bottom: 28px;
}

.auth-main-btn {
    width: 100%;
    min-height: 48px;
    margin-top: 8px;
}

.auth-bottom-text {
    margin: 20px 0 0;
    text-align: center;
    font-weight: 600;
}

.auth-bottom-text a {
    font-weight: 800;
    text-decoration: none;
}

.auth-visual-panel {
    position: relative;
    min-height: 520px;
    display: flex;
    align-items: center;
    justify-content: center;
    overflow: hidden;
    background: #b6f2f2;
}

.auth-visual-panel::before {
    display: none;
}

.auth-blob {
    display: none;
}

.auth-logo-card {
    position: relative;
    z-index: 2;
    width: min(340px, calc(100% - 48px));
    padding: 30px;
    text-align: center;
    background: rgba(255, 255, 255, 0.88);
    backdrop-filter: blur(10px);
}

.auth-logo-card i {
    color: var(--brand);
    font-size: 56px;
    margin-bottom: 16px;
}

.auth-floating-icon {
    position: absolute;
    width: 54px;
    height: 54px;
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(255, 255, 255, 0.90);
    color: var(--brand);
    box-shadow: var(--shadow);
}

.icon-one { top: 56px; left: 64px; }
.icon-two { bottom: 68px; left: 86px; }
.icon-three { top: 96px; right: 70px; }

.register-shell {
    width: min(1160px, 100%);
}

.auth-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0 16px;
}

hr {
    border-color: var(--line);
    opacity: 1;
    margin: 32px 0;
}

canvas {
    display: block;
    width: 100% !important;
    max-height: 360px;
}

.chart-card {
    width: 100%;
    max-width: 1280px;
    min-height: 330px;
    padding: 24px;
    background: #fff;
    border: 1px solid var(--line);
    border-radius: 16px;
    box-shadow: var(--shadow-sm);
}

.chart-card canvas {
    height: 280px !important;
    max-height: none;
}

.admin-users-panel {
    max-width: 1280px;
    margin: 0 0 34px;
    padding: 24px;
    border: 1px solid rgba(126, 217, 217, 0.36);
    border-radius: 16px;
    background: #ffffff;
    box-shadow: var(--shadow-sm);
}

.admin-users-panel__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    margin-bottom: 16px;
}

.admin-users-panel__header h4 {
    margin: 0;
    color: var(--teal);
    font-size: 24px;
    font-weight: 900;
}

.admin-users-panel__header p {
    margin: 4px 0 0;
    color: var(--muted);
    font-size: 13px;
}

.admin-users-panel__header > span {
    display: inline-flex;
    align-items: center;
    min-height: 34px;
    padding: 8px 14px;
    border-radius: 999px;
    background: #eefdff;
    color: var(--teal);
    font-size: 12px;
    font-weight: 900;
    white-space: nowrap;
}

.admin-users-filters {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    margin-bottom: 18px;
}

.admin-users-filter {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    min-height: 38px;
    padding: 8px 14px;
    border: 1px solid rgba(126, 217, 217, 0.36);
    border-radius: 999px;
    background: #ffffff;
    color: #557076;
    font-size: 12px;
    font-weight: 800;
    text-decoration: none;
    transition: background-color 0.2s ease, border-color 0.2s ease, color 0.2s ease, box-shadow 0.2s ease;
}

.admin-users-filter span {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 24px;
    min-height: 24px;
    padding: 0 7px;
    border-radius: 999px;
    background: #eef8fa;
    color: var(--teal);
    font-size: 11px;
    font-weight: 900;
}

.admin-users-filter:hover,
.admin-users-filter:focus {
    color: var(--teal);
    border-color: rgba(60, 199, 208, 0.42);
    box-shadow: 0 8px 18px rgba(3, 139, 166, 0.08);
    text-decoration: none;
}

.admin-users-filter.is-active {
    background: #eefdff;
    border-color: rgba(60, 199, 208, 0.42);
    color: var(--teal);
}

.admin-users-filter.is-active span {
    background: #39c9d2;
    color: #ffffff;
}

.admin-users-table {
    margin-bottom: 0 !important;
}

.admin-users-table td,
.admin-users-table th {
    vertical-align: middle;
}

.admin-users-row--archived > * {
    background: #faf8fc !important;
}

.admin-users-meta {
    display: grid;
    gap: 4px;
    color: #56696f;
    font-size: 12px;
    line-height: 1.45;
}

.admin-users-meta small {
    color: #7a667f;
    font-size: 11px;
}

.admin-user-actions {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
}

.admin-user-actions .admin-user-action-form {
    margin: 0;
}

.admin-archive-reason {
    margin: 12px 0 4px;
    text-align: left;
}

.admin-archive-reason .form-label {
    display: inline-block;
    margin-bottom: 8px;
    color: var(--dark);
    font-weight: 700;
}

.badge {
    border-radius: 999px;
    padding: 7px 10px;
}

.service-toggle-group {
    display: grid;
    gap: 12px;
    margin: 18px 0 22px;
}

.service-toggle {
    display: flex;
    align-items: center;
    gap: 12px;
    min-height: 54px;
    padding: 12px 14px;
    border: 1px solid rgba(126, 217, 217, 0.38);
    border-radius: 14px;
    background: #f8feff;
    color: var(--teal);
    cursor: pointer;
    transition: border-color 0.18s ease, background 0.18s ease, box-shadow 0.18s ease;
}

.service-toggle:hover {
    border-color: rgba(3, 139, 166, 0.36);
    box-shadow: 0 10px 22px rgba(3, 139, 166, 0.08);
}

.service-toggle input {
    position: absolute;
    opacity: 0;
    pointer-events: none;
}

.service-toggle span {
    position: relative;
    flex: 0 0 auto;
    width: 46px;
    height: 26px;
    border-radius: 999px;
    background: #d8edf0;
    transition: background 0.18s ease;
}

.service-toggle span::after {
    content: "";
    position: absolute;
    top: 4px;
    left: 4px;
    width: 18px;
    height: 18px;
    border-radius: 50%;
    background: #ffffff;
    box-shadow: 0 3px 8px rgba(52, 71, 76, 0.18);
    transition: transform 0.18s ease;
}

.service-toggle input:checked + span {
    background: var(--aqua);
}

.service-toggle input:checked + span::after {
    transform: translateX(20px);
}

.service-toggle input:focus-visible + span {
    outline: 3px solid rgba(40, 213, 213, 0.26);
    outline-offset: 2px;
}

.service-toggle strong {
    color: var(--teal);
    font-size: 13px;
    font-weight: 900;
}

.med-dashboard {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 300px;
    gap: 24px;
    min-height: 760px;
    color: #17243f;
}

.med-side-rail {
    min-height: 720px;
    padding: 20px 0;
    border-radius: 28px;
    background: linear-gradient(180deg, #62c2c9, #49b5c2);
    box-shadow: var(--shadow-sm);
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 28px;
}

.med-side-rail a,
.rail-brand {
    width: 46px;
    height: 46px;
    border-radius: 14px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    color: rgba(255, 255, 255, 0.78);
    text-decoration: none;
    font-size: 18px;
}

.rail-brand {
    margin-bottom: 18px;
    color: #fff;
    background: rgba(255, 255, 255, 0.18);
}

.med-side-rail a.active,
.med-side-rail a:hover {
    color: #fff;
    background: rgba(255, 255, 255, 0.18);
}

.med-side-rail a:last-child {
    margin-top: auto;
}

.med-main-panel {
    min-width: 0;
}

.med-topbar {
    display: grid;
    grid-template-columns: minmax(220px, 1fr) minmax(260px, 390px);
    gap: 22px;
    align-items: center;
    margin-bottom: 28px;
}

.med-topbar h1 {
    margin: 0;
    color: #17243f;
    font-size: clamp(30px, 4vw, 38px);
    line-height: 1.12;
}

.med-topbar span {
    display: block;
    margin-top: 8px;
    color: #93a5aa;
    font-weight: 600;
}

.med-search {
    height: 58px;
    padding: 0 18px;
    border-radius: 12px;
    background: #fff;
    box-shadow: 0 12px 32px rgba(24, 50, 58, 0.08);
    display: flex;
    align-items: center;
    gap: 12px;
    color: #93c4c8;
}

.med-search input {
    min-height: 0;
    width: 100%;
    border: 0 !important;
    outline: none;
    box-shadow: none !important;
    color: #17243f;
    font-weight: 600;
}

.med-hero-grid {
    display: grid;
    grid-template-columns: minmax(0, 1.1fr) minmax(280px, 0.9fr);
    gap: 28px;
}

.med-welcome-card,
.med-results-card,
.med-calendar-card,
.med-action-card,
.med-profile-panel {
    border: 0;
    border-radius: 28px;
    box-shadow: 0 20px 46px rgba(24, 50, 58, 0.09);
}

.med-welcome-card {
    min-height: 214px;
    padding: 30px;
    background: #dff8f8;
    display: flex;
    align-items: center;
    overflow: hidden;
}

.med-welcome-card p,
.med-welcome-card h2 {
    color: var(--ink);
}

.med-welcome-card p {
    margin: 0 0 8px;
    font-size: 24px;
    font-weight: 800;
    line-height: 1.2;
}

.med-welcome-card h2 {
    margin: 0 0 24px;
    max-width: 270px;
    font-size: 15px;
    font-weight: 500;
    line-height: 1.6;
}

.med-icon-link {
    width: 48px;
    height: 48px;
    border: 2px solid rgba(3, 139, 166, 0.24);
    border-radius: 14px;
    color: var(--teal);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    text-decoration: none;
}

.med-results-card {
    min-height: 214px;
    padding: 30px;
    background: #f1fbfc;
}

.med-results-card span,
.med-results-card h2,
.med-results-card strong {
    color: var(--ink);
}

.med-results-card > span {
    display: block;
    margin-bottom: 8px;
    font-size: 24px;
    font-weight: 800;
}

.med-results-card h2 {
    margin: 0 0 22px;
    font-size: 14px;
    font-weight: 500;
}

.result-row {
    display: grid;
    grid-template-columns: 72px minmax(70px, 1fr) 58px;
    align-items: center;
    gap: 12px;
    margin-top: 12px;
    font-size: 12px;
}

.result-row div {
    height: 5px;
    border-radius: 999px;
    background: rgba(3, 139, 166, 0.12);
    overflow: hidden;
}

.result-row b {
    display: block;
    height: 100%;
    border-radius: inherit;
    background: #1ddada;
}

.result-row:nth-of-type(2) b {
    background: #58c98b;
}

.result-row:nth-of-type(3) b {
    background: #5aa9e6;
}

.med-action-row {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 18px;
    margin: 28px 0 34px;
}

.med-action-card {
    min-height: 104px;
    padding: 18px;
    background: #fff;
    color: #17243f;
    text-decoration: none;
    display: grid;
    grid-template-columns: 56px minmax(0, 1fr) 20px;
    gap: 14px;
    align-items: center;
}

.med-action-card > span {
    width: 54px;
    height: 54px;
    border-radius: 14px;
    background: #e0f5f4;
    color: #2d9daf;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 24px;
}

.med-action-card:nth-child(2) > span {
    background: #eaf1ff;
    color: #6f9df0;
}

.med-action-card:nth-child(3) > span {
    background: #fff6da;
    color: #e0a31d;
}

.med-action-card strong,
.med-action-card small {
    display: block;
}

.med-action-card small {
    margin-top: 4px;
    color: #9aabb0;
    font-size: 12px;
}

.med-action-card > i {
    color: #9fcbd0;
}

.med-calendar-card {
    position: relative;
    min-height: 330px;
    padding: 28px 34px 34px;
    background: #fff;
    overflow: visible;
}

.calendar-head {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    margin-bottom: 28px;
}

.calendar-head > div {
    min-width: 180px;
    text-align: center;
}

.calendar-nav-button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 34px;
    height: 34px;
    border: 1px solid rgba(126, 217, 217, 0.45);
    border-radius: 50%;
    background: #eefdff;
    color: #038ba6;
    transition: background 0.18s ease, color 0.18s ease, transform 0.18s ease;
}

.calendar-nav-button:hover {
    background: #28d5d5;
    color: #ffffff;
    transform: translateY(-1px);
}

.calendar-head h2 {
    margin: 0;
    color: #17243f;
    font-size: 28px;
}

.calendar-head span {
    color: #a6b4b8;
    font-weight: 700;
}

.calendar-grid {
    display: grid;
    grid-template-columns: repeat(7, minmax(34px, 1fr));
    gap: 12px 16px;
    text-align: center;
}

.calendar-weekdays {
    margin-bottom: 18px;
    color: #b5c2c6;
    font-size: 13px;
    font-weight: 800;
    text-transform: none;
}

.calendar-days span {
    min-height: 34px;
    border-radius: 10px;
    color: #24314e;
    font-weight: 800;
    display: flex;
    align-items: center;
    justify-content: center;
}

.calendar-days .today {
    background: #b7e5e3;
    color: #fff;
}

.calendar-days .has-appointment {
    position: relative;
}

.calendar-days .has-appointment::after {
    content: "";
    position: absolute;
    top: 5px;
    right: 8px;
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #ffc542;
}

.appointment-popover {
    position: absolute;
    right: -74px;
    bottom: 52px;
    width: 210px;
    min-height: 138px;
    padding: 22px;
    border-radius: 24px;
    background: #fff;
    box-shadow: 0 18px 44px rgba(24, 50, 58, 0.14);
}

.appointment-popover > span {
    position: absolute;
    top: -20px;
    right: -10px;
    width: 44px;
    height: 44px;
    border-radius: 50%;
    background: #ffc542;
    color: #fff;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 8px 18px rgba(255, 197, 66, 0.36);
}

.appointment-popover h3 {
    margin: 0 0 8px;
    color: #17243f;
    font-size: 18px;
}

.appointment-popover p,
.appointment-popover small {
    margin: 0;
    color: #a0adb2;
}

.appointment-popover strong {
    display: block;
    margin-top: 8px;
    color: #17243f;
}

.appointment-create-notes {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 14px;
    width: 100%;
    max-width: none;
    margin: 0 0 26px;
    padding-bottom: 20px;
    border-bottom: 1px solid rgba(200, 227, 234, 0.85);
    align-items: stretch;
}

.appointment-create-notes section {
    display: grid;
    grid-template-columns: 34px minmax(0, 1fr);
    gap: 11px;
    align-items: start;
    min-height: 0;
    padding: 16px 18px;
    border: 1px solid rgba(126, 217, 217, 0.38);
    border-radius: 16px;
    background: rgba(248, 254, 255, 0.94);
    box-shadow: 0 10px 22px rgba(3, 139, 166, 0.05);
}

.appointment-create-notes section:nth-child(2) {
    border-color: rgba(243, 182, 63, 0.42);
    background: rgba(255, 250, 240, 0.94);
}

.appointment-create-notes i {
    display: grid;
    place-items: center;
    width: 34px;
    height: 34px;
    border-radius: 12px;
    background: var(--brand-soft);
    color: var(--brand);
    font-size: 14px;
}

.appointment-create-notes section:nth-child(2) i {
    background: #fff1bd;
    color: #8a6100;
}

.appointment-create-notes strong {
    display: block;
    margin: 1px 0 5px;
    color: var(--brand-dark);
    font-size: 13px;
    font-weight: 900;
    line-height: 1.25;
}

.appointment-create-notes section:nth-child(2) strong {
    color: #735515;
}

.appointment-create-notes p {
    margin: 0;
    color: #4f6269;
    font-size: 12px;
    font-weight: 600;
    line-height: 1.55;
}

.appointment-request-form {
    max-width: 1080px;
    margin: 0 auto;
    padding: 34px 38px !important;
    border-radius: 22px !important;
}

.app-container--standard > form.appointment-request-form {
    max-width: 1080px;
}

.app-container--standard:has(> form.appointment-request-form) > h1,
.app-container--standard:has(> form.appointment-request-form) > h2,
.app-container--standard:has(> form.appointment-request-form) > h3,
.app-container--standard:has(> form.appointment-request-form) > .alert,
.app-container--standard:has(> form.appointment-request-form) > .text-danger {
    max-width: 1080px;
}

.appointment-request-form .row.g-3 {
    --bs-gutter-y: 1.15rem;
    --bs-gutter-x: 1.15rem;
}

.appointment-emergency-toggle {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
    padding: 10px 16px;
    border: 1px solid rgba(126, 217, 217, 0.36);
    border-radius: 14px;
    background: linear-gradient(180deg, rgba(255, 255, 255, 0.98) 0%, rgba(245, 252, 253, 0.98) 100%);
    box-shadow: 0 10px 24px rgba(3, 139, 166, 0.06);
}

.appointment-emergency-toggle__copy {
    min-width: 0;
    padding: 2px 0;
}

.appointment-emergency-toggle__switch {
    flex: 0 0 auto;
    margin: auto 0;
    padding: 0 !important;
    width: auto;
    min-width: 0;
    min-height: 30px;
    display: flex;
    align-items: center;
    justify-content: center;
    line-height: 0;
    align-self: center;
}

.appointment-emergency-toggle .form-check-input {
    width: 68px;
    min-width: 68px;
    max-width: 68px;
    height: 36px;
    margin: 0;
    padding: 0 !important;
    border-radius: 9999px !important;
    border: 0;
    background-color: #edf3f5;
    box-shadow: none;
    cursor: pointer;
    background-image: none !important;
    position: relative;
    display: block;
    overflow: hidden;
    background-clip: padding-box;
    vertical-align: middle;
    flex: 0 0 auto;
    appearance: none;
    -webkit-appearance: none;
    transition: background-color 0.22s ease, border-color 0.22s ease, box-shadow 0.22s ease, transform 0.22s ease;
    box-shadow: inset 0 1px 2px rgba(17, 145, 164, 0.05), 0 1px 4px rgba(17, 145, 164, 0.06);
}

.appointment-emergency-toggle .form-check-input::before {
    content: "";
    position: absolute;
    top: 3px;
    left: 3px;
    width: 30px;
    height: 30px;
    border-radius: 9999px;
    background: #ffffff;
    box-shadow: 0 3px 8px rgba(34, 57, 66, 0.18);
    transition: transform 0.22s ease, box-shadow 0.22s ease;
}

.appointment-emergency-toggle .form-check-input:checked {
    background-color: #39c9d2;
    box-shadow: inset 0 1px 2px rgba(10, 123, 138, 0.10), 0 1px 4px rgba(60, 199, 208, 0.10);
}

.appointment-emergency-toggle .form-check-input:checked::before {
    transform: translateX(32px);
    box-shadow: 0 3px 8px rgba(10, 123, 138, 0.2);
}

.appointment-emergency-toggle .form-check-label {
    margin: 0;
    color: var(--teal);
    font-size: 13px;
    font-weight: 800;
    line-height: 1.35;
}

.appointment-emergency-toggle__hint {
    margin: 5px 0 0;
    color: #5f7279;
    font-size: 12px;
    line-height: 1.5;
}

.appointment-emergency-toggle .form-check-input:focus {
    box-shadow: 0 0 0 3px rgba(60, 199, 208, 0.12), inset 0 1px 2px rgba(17, 145, 164, 0.05);
}

.appointment-emergency-toggle .form-check-input:hover {
    transform: translateY(-1px);
}

.appointment-emergency-toggle__switch .form-check-input {
    float: none;
    margin-left: 0 !important;
    margin-bottom: 0 !important;
}

.appointment-emergency-toggle--guest {
    margin-top: 4px;
}

.med-profile-panel {
    padding: 28px;
    background: #fff;
    text-align: center;
}

.profile-avatar {
    width: 82px;
    height: 82px;
    margin: 0 auto 16px;
    border-radius: 50%;
    background: #eaf1ff;
    color: #9c6b35;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 34px;
}

.med-profile-panel h2 {
    margin: 0;
    color: #17243f;
    font-size: 20px;
}

.med-profile-panel > p {
    margin: 8px 0 22px;
    color: #b0b9bd;
    font-size: 12px;
}

.profile-stats {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 8px;
    margin-bottom: 28px;
}

.profile-stats strong {
    color: #17243f;
    font-size: 17px;
}

.profile-stats small {
    display: block;
    margin-top: 4px;
    color: #a0adb2;
    font-size: 11px;
    font-weight: 600;
}

.profile-note {
    text-align: left;
    margin-bottom: 30px;
}

.profile-note span,
.profile-note p,
.reminder-list small {
    color: #a0adb2;
    font-size: 12px;
}

.profile-note h3,
.reminder-list h3 {
    margin: 8px 0 12px;
    color: #17243f;
    font-size: 15px;
}

.profile-note p {
    margin: 0;
    line-height: 1.7;
}

.reminder-list {
    display: grid;
    gap: 12px;
    text-align: left;
}

.reminder-list a {
    min-height: 56px;
    padding: 10px 12px;
    border-radius: 14px;
    background: #fbfdfd;
    color: #17243f;
    text-decoration: none;
    display: grid;
    grid-template-columns: 30px minmax(0, 1fr);
    gap: 10px;
    align-items: center;
    box-shadow: 0 10px 26px rgba(24, 50, 58, 0.05);
}

.reminder-list a > i {
    color: #b4c6cb;
}

.reminder-list span,
.reminder-list small {
    display: block;
}

.reminder-list span {
    font-size: 12px;
    font-weight: 800;
}

.profile-pager {
    width: 154px;
    min-height: 48px;
    margin: 28px auto 0;
    border-radius: 18px;
    background: #58bdc7;
    color: #fff;
    display: grid;
    grid-template-columns: 44px 1fr 44px;
    align-items: center;
    box-shadow: 0 14px 28px rgba(73, 181, 194, 0.24);
}

.profile-pager button {
    border: 0;
    background: transparent;
    color: #fff;
    font-size: 18px;
    font-weight: 800;
}

.profile-pager span {
    font-weight: 800;
}

@media (max-width: 1100px) {
    .main-header {
        padding: 14px 24px 12px;
    }

    .med-dashboard {
        grid-template-columns: 1fr;
    }

    .med-profile-panel {
        grid-column: auto;
    }

    .appointment-popover {
        right: 24px;
    }

    .specialist-layout-clean,
    .specialist-content-area,
    .price-layout,
    .review-grid,
    .contact-area,
    .auth-shell {
        grid-template-columns: 1fr;
    }

    .auth-visual-panel {
        min-height: 320px;
    }
}

@media (max-width: 768px) {
    .top-clinic-bar {
        grid-template-columns: 1fr;
        gap: 12px;
    }

    .top-socials {
        justify-content: flex-start;
    }

    .main-landing-nav {
        justify-content: flex-start;
        overflow-x: auto;
        flex-wrap: nowrap;
        padding-bottom: 4px;
    }

    .main-landing-nav a {
        white-space: nowrap;
    }

    .app-container,
    .vet-section {
        width: min(100% - 32px, 1220px);
    }

    .med-dashboard {
        grid-template-columns: 1fr;
    }

    .med-side-rail {
        min-height: 0;
        border-radius: 20px;
        flex-direction: row;
        justify-content: center;
        padding: 12px;
        gap: 10px;
    }

    .rail-brand {
        margin: 0;
    }

    .med-side-rail a:last-child {
        margin-top: 0;
    }

    .med-topbar,
    .med-hero-grid,
    .med-action-row {
        grid-template-columns: 1fr;
    }

    .med-profile-panel {
        grid-column: auto;
    }

    .med-welcome-card {
        grid-template-columns: 1fr;
    }

    .med-vet-illustration {
        justify-self: start;
    }

    .appointment-popover {
        position: static;
        width: 100%;
        margin-top: 22px;
    }

    .vet-hero {
        min-height: 500px;
        padding: 56px 24px 68px;
    }

    .table {
        display: block;
        overflow-x: auto;
        white-space: nowrap;
    }

    .auth-form-panel {
        padding: 36px 24px;
    }

    .auth-grid {
        grid-template-columns: 1fr;
    }

    .photo-card {
        min-width: 260px;
        height: 180px;
    }

    .carousel-arrow,
    .gallery-arrow {
        display: none;
    }

    .specialist-side-arrow {
        top: 180px;
    }
}

@media (max-width: 520px) {
    .main-header {
        padding: 12px 16px;
    }

    .top-logo strong {
        font-size: 16px;
    }

    .top-logo small {
        font-size: 12px;
    }

    .vet-hero-buttons,
    .contact-card .btn {
        display: grid;
        width: 100%;
    }

    .contact-card .btn + .btn {
        margin-top: 10px;
    }
}

/* Thrive clinic theme */
:root {
    --mint: #7ed9d9;
    --aqua: #1ddada;
    --yellow: #fecd44;
    --light-mint: #b6f2f2;
    --teal: #038ba6;
    --dark: #4c4949;
    --brand: #1ddada;
    --brand-dark: #038ba6;
    --brand-soft: #b6f2f2;
    --accent: #fecd44;
    --ink: #4c4949;
    --muted: #4c4949;
    --line: rgba(126, 217, 217, 0.45);
    --surface: #ffffff;
    --page: #eefdff;
    --success: #038ba6;
    --shadow: 0 18px 45px rgba(3, 139, 166, 0.14);
    --shadow-sm: 0 8px 24px rgba(3, 139, 166, 0.10);
}

body {
    background: var(--page);
    color: var(--ink);
    font-family: Arial, 'Poppins', sans-serif;
}

a,
.auth-bottom-text a {
    color: var(--teal);
}

a:hover,
.auth-bottom-text a:hover {
    color: var(--aqua);
}

h1,
h2,
h3,
h4,
h5,
h6,
.section-heading h2 {
    color: var(--ink);
}

p,
small,
.top-info,
.auth-subtitle {
    color: var(--muted);
}

.main-header {
    background: rgba(232, 250, 252, 0.96) !important;
    border-bottom: 1px solid rgba(126, 217, 217, 0.45);
    box-shadow: 0 12px 30px rgba(3, 139, 166, 0.12);
}

.top-logo,
.main-landing-nav a,
.top-header-actions .btn {
    color: var(--dark) !important;
}

.top-logo strong {
    color: #4c4949 !important;
}

.top-logo small {
    color: #4c4949 !important;
}

.brand-icon {
    background: #ffffff !important;
}

.main-landing-nav a:hover {
    background: var(--aqua) !important;
    color: #ffffff !important;
}

.top-header-actions .btn-outline-secondary {
    border-color: rgba(17, 145, 164, 0.35) !important;
    background: #ffffff !important;
    color: var(--teal) !important;
    box-shadow: 0 8px 18px rgba(3, 139, 166, 0.06);
}

.top-header-actions .btn-outline-secondary:hover {
    background: var(--brand-soft) !important;
    color: var(--teal) !important;
}

.top-header-actions .btn-primary {
    padding-left: 20px;
    padding-right: 20px;
    box-shadow: 0 10px 22px rgba(17, 145, 164, 0.18);
}

.vet-footer {
    background: #ffffff !important;
    border-top-color: rgba(126, 217, 217, 0.45);
    color: var(--teal);
}

.vet-footer p {
    color: var(--teal);
}

.btn-primary,
.btn-success,
.btn-info,
.btn-vet-primary,
.auth-main-btn {
    background: var(--aqua) !important;
    border-color: var(--aqua) !important;
    color: #ffffff !important;
}

.btn-primary:hover,
.btn-success:hover,
.btn-info:hover,
.btn-vet-primary:hover,
.auth-main-btn:hover {
    background: var(--teal) !important;
    border-color: var(--teal) !important;
    color: #ffffff !important;
}

.btn-secondary,
.btn-vet-outline {
    background: #ffffff !important;
    border-color: rgba(3, 139, 166, 0.25) !important;
    color: var(--teal) !important;
}

.btn-secondary:hover,
.btn-vet-outline:hover {
    background: var(--light-mint) !important;
    border-color: var(--mint) !important;
    color: var(--teal) !important;
}

.btn-warning,
.badge.bg-warning {
    background: var(--yellow) !important;
    border-color: var(--yellow) !important;
    color: var(--dark) !important;
}

.card,
.content-card,
.review-card,
.contact-card,
.price-table-card,
.gallery-card,
.specialist-name,
.specialist-main-photo,
.specialist-bio,
.auth-shell,
.auth-logo-card,
.med-results-card,
.med-calendar-card,
.med-action-card,
.med-profile-panel,
.med-welcome-card {
    border-color: var(--line) !important;
    box-shadow: var(--shadow-sm) !important;
}

.table {
    border-color: #dff4f6;
}

.table thead th {
    background: #b6f2f2 !important;
    color: #038ba6 !important;
}

.table tbody tr:hover {
    background: #f8fbff !important;
}

.form-control,
.form-select,
select,
textarea,
input {
    border-color: rgba(3, 139, 166, 0.22) !important;
}

.form-control:focus,
.form-select:focus,
select:focus,
textarea:focus,
input:focus {
    border-color: var(--aqua) !important;
    box-shadow: 0 0 0 0.2rem rgba(29, 218, 218, 0.18) !important;
}

label,
.form-label,
.auth-kicker,
.specialist-bio h5,
.top-info i {
    color: var(--teal) !important;
}

.alert-info {
    background: var(--brand-soft);
    border-color: var(--mint);
    color: var(--teal);
}

.vet-hero {
    background: linear-gradient(rgba(3, 139, 166, 0.66), rgba(29, 218, 218, 0.36)), url('/images/gallery/gallery-1.png') center/cover no-repeat !important;
}

.vet-kicker {
    color: var(--yellow) !important;
}

.vet-hero h1,
.vet-hero p {
    color: #ffffff;
}

.gallery-img,
.specialist-name span,
.med-action-card > span,
.calendar-days .today {
    background: var(--light-mint) !important;
    color: var(--teal) !important;
}

.gallery-card:hover,
.gallery-card.active,
.specialist-name.active {
    border-color: var(--aqua) !important;
    background: rgba(182, 242, 242, 0.45) !important;
}

.carousel-dots .dot {
    background: rgba(3, 139, 166, 0.25) !important;
}

.carousel-dots .dot.active,
.calendar-days .has-appointment::after,
.appointment-popover > span,
.med-vet-illustration {
    background: var(--yellow) !important;
    color: var(--dark) !important;
}

.carousel-arrow,
.gallery-arrow,
.specialist-side-arrow {
    color: var(--teal) !important;
}

.price-photo,
.specialist-main-photo,
.map-box,
.map-placeholder,
.auth-visual-panel {
    background-blend-mode: multiply;
}

.med-dashboard,
.med-topbar h1,
.med-search input,
.med-action-card,
.calendar-head h2,
.calendar-days span,
.appointment-popover h3,
.appointment-popover strong,
.med-profile-panel h2,
.profile-stats strong,
.profile-note h3,
.reminder-list h3,
.reminder-list a {
    color: var(--ink) !important;
}

.med-side-rail,
.profile-pager {
    background: linear-gradient(180deg, var(--mint), var(--teal)) !important;
}

.med-welcome-card {
    background: #038ba6 !important;
}

.med-results-card {
    background: #075866 !important;
}

.med-welcome-card p,
.med-welcome-card h2,
.med-results-card span,
.med-results-card h2,
.med-results-card strong {
    color: #ffffff !important;
}

.med-icon-link {
    border-color: rgba(255, 255, 255, 0.50);
    color: #ffffff;
}

.result-row div {
    background: rgba(255, 255, 255, 0.24);
}

.med-search,
.med-profile-panel,
.med-calendar-card,
.med-action-card,
.appointment-popover,
.reminder-list a {
    background: #ffffff !important;
}

.med-search,
.med-search i,
.med-action-card > i,
.reminder-list a > i {
    color: var(--teal) !important;
}

.profile-avatar {
    background: var(--light-mint) !important;
    color: var(--teal) !important;
}

/* Client dashboard refinements */
.app-container--client-dashboard {
    width: min(1600px, calc(100% - 48px));
}

.med-dashboard {
    grid-template-columns: minmax(0, 1fr) minmax(300px, 360px);
    gap: 24px;
    align-items: start;
    overflow: hidden;
}

.med-side-rail {
    width: 86px;
    min-width: 86px;
    max-width: 86px;
    justify-self: start;
}

.med-main-panel,
.med-profile-panel {
    min-width: 0;
}

.med-topbar {
    grid-template-columns: minmax(0, 1fr) minmax(280px, 490px);
}

.med-welcome-card {
    background: #dff8f8 !important;
}

.med-results-card {
    background: #f1fbfc !important;
}

.med-results-card span,
.med-results-card h2,
.med-results-card strong {
    color: var(--ink) !important;
}

.med-action-card {
    transition: transform 0.18s ease, box-shadow 0.18s ease, opacity 0.18s ease;
}

.med-welcome-card {
    background: #dff8f8 !important;
}

.med-results-card {
    background: #f1fbfc !important;
}

.result-row b {
    background: #1ddada !important;
}

.result-row:nth-of-type(2) b {
    background: #f3c64d !important;
}

.result-row:nth-of-type(3) b {
    background: #5aa9e6 !important;
}

.med-action-card:hover,
.reminder-list a:hover {
    transform: translateY(-2px);
    box-shadow: 0 18px 36px rgba(3, 139, 166, 0.12);
}

.is-filtered-out {
    opacity: 0.28;
    transform: scale(0.98);
}

.med-calendar-card {
    min-height: 420px;
    padding: 34px 42px 42px;
    overflow: visible;
}

.calendar-days span,
.calendar-days button {
    min-height: 42px;
    border: 0;
    border-radius: 12px;
    background: transparent;
    color: #4c4949;
    font: inherit;
    font-weight: 800;
    display: flex;
    align-items: center;
    justify-content: center;
}

.calendar-days button {
    cursor: pointer;
}

.calendar-days button:hover {
    background: #eefdff;
    color: #038ba6;
}

.calendar-days .today {
    background: #b6f2f2 !important;
    color: #038ba6 !important;
}

.calendar-days .has-appointment {
    position: relative;
    background: #fff8db;
}

.calendar-days .has-appointment::after {
    top: 7px;
    right: 9px;
    background: #fecd44;
}

.appointment-popover[hidden] {
    display: none !important;
}

.appointment-popover {
    right: 38px;
    bottom: 38px;
    width: 260px;
    border: 1px solid rgba(126, 217, 217, 0.45);
    box-shadow: 0 22px 46px rgba(3, 139, 166, 0.16);
    z-index: 5;
}

.med-calendar-card .appointment-popover {
    position: absolute;
    left: 0;
    top: 0;
    right: auto;
    bottom: auto;
    transition: left 0.16s ease, top 0.16s ease, opacity 0.16s ease;
}

.calendar-days button.is-selected {
    background: #eefdff;
    color: #038ba6;
    box-shadow: inset 0 0 0 2px rgba(126, 217, 217, 0.45);
}

.appointment-close {
    position: absolute;
    top: 12px;
    right: 12px;
    width: 28px;
    height: 28px;
    border: 0;
    border-radius: 50%;
    background: #eefdff;
    color: #038ba6;
    display: inline-flex;
    align-items: center;
    justify-content: center;
}

.appointment-popover > span {
    background: #fecd44 !important;
    color: #4c4949 !important;
}

.med-profile-panel {
    padding: 34px;
    border: 1px solid rgba(126, 217, 217, 0.32) !important;
}

.profile-avatar {
    width: 100px;
    height: 100px;
    color: #038ba6 !important;
    background: linear-gradient(135deg, #b6f2f2, #eefdff) !important;
    font-size: 40px;
}

.med-profile-panel h2 {
    color: #4c4949 !important;
    font-size: 24px;
}

.med-profile-panel > p {
    color: #7f949b;
    overflow-wrap: anywhere;
}

.profile-stats strong {
    color: #038ba6 !important;
}

.reminder-list a {
    transition: transform 0.18s ease, box-shadow 0.18s ease, opacity 0.18s ease;
}

.profile-pager {
    background: linear-gradient(135deg, #1ddada, #038ba6) !important;
}

@media (max-width: 1280px) {
    .app-container--client-dashboard {
        width: min(1180px, calc(100% - 36px));
    }

    .med-dashboard {
        grid-template-columns: minmax(0, 1fr);
    }

    .med-profile-panel {
        grid-column: auto;
    }
}

@media (max-width: 980px) {
    .med-topbar,
    .med-hero-grid,
    .med-action-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 768px) {
    .app-container {
        width: min(100% - 24px, 640px);
    }

    .app-container--client-dashboard {
        width: min(100% - 24px, 640px);
    }

    .med-dashboard {
        grid-template-columns: 1fr;
    }

    .med-side-rail {
        width: 100%;
        max-width: none;
        min-width: 0;
    }

    .med-profile-panel {
        grid-column: auto;
    }
}

/* Reference-style public landing page */
.medivet-container {
    width: min(1240px, calc(100% - 48px));
    margin: 0 auto;
    position: relative;
    z-index: 3;
}

.main-header,
.vet-footer {
    position: relative;
    z-index: 3;
}

.app-container {
    position: relative;
    z-index: 3;
}

.medivet-side-circles {
    position: fixed;
    inset: 0;
    z-index: 2;
    pointer-events: none;
    overflow: hidden;
}

.medivet-side-circle {
    position: absolute;
    display: block;
    border-radius: 50%;
}

.medivet-side-circle-blue {
    width: 430px;
    height: 430px;
    top: 50%;
    left: -250px;
    background: rgba(29, 218, 218, 0.13);
}

.medivet-side-circle-yellow {
    width: 360px;
    height: 360px;
    top: 18%;
    right: -210px;
    background: rgba(254, 205, 68, 0.15);
}

.medivet-hero {
    position: relative;
    min-height: 620px;
    padding: 74px 0 120px;
    overflow: hidden;
    background:
        radial-gradient(circle at 78% 42%, rgba(126, 217, 217, 0.30), transparent 34%),
        linear-gradient(180deg, #ffffff 0%, #f5fdff 74%, #eefdff 100%);
}

.medivet-hero::after,
.medivet-wave-top::before,
.medivet-band::before {
    content: "";
    position: absolute;
    left: -8%;
    right: -8%;
    height: 150px;
    pointer-events: none;
    background: #ffffff;
    border-radius: 0 0 50% 50%;
}

.medivet-hero::after {
    bottom: -72px;
    transform: rotate(-2deg);
}

.medivet-hero-grid {
    position: relative;
    z-index: 3;
    display: grid;
    grid-template-columns: minmax(460px, 0.92fr) minmax(420px, 0.88fr);
    gap: 84px;
    align-items: center;
    justify-content: center;
}

.medivet-hero-copy h1 {
    max-width: 600px;
    margin: 0 0 18px;
    color: var(--teal);
    font-size: clamp(34px, 4.6vw, 48px);
    line-height: 1.12;
    font-weight: 900;
}

.medivet-hero-copy p {
    max-width: 500px;
    margin: 0;
    color: rgba(76, 73, 73, 0.72);
    font-size: 16px;
    line-height: 1.65;
}

.medivet-actions {
    display: flex;
    flex-wrap: wrap;
    gap: 18px;
    margin-top: 30px;
}

.medivet-actions .btn,
.medivet-section .btn-vet-primary,
.medivet-section .btn-vet-outline,
.medivet-tabs button,
.medivet-photo-card a,
.medivet-article-grid .featured a {
    min-width: 128px;
    border-radius: 999px !important;
    padding: 11px 22px;
    font-size: 12px;
    text-transform: uppercase;
}



.medivet-hero-visual {
    position: relative;
    min-height: 430px;
    display: flex;
    align-items: center;
    justify-content: center;
    justify-self: center;
    width: min(520px, 100%);
}

.medivet-hero-visual::after {
    content: "";
    position: absolute;
    right: -26px;
    top: 68px;
    width: 170px;
    height: 190px;
    background-image: radial-gradient(var(--mint) 2px, transparent 2px);
    background-size: 18px 18px;
    opacity: 0.8;
}

.medivet-hero-visual img {
    position: relative;
    z-index: 2;
    width: min(500px, 100%);
    max-height: 470px;
    object-fit: contain;
    object-position: center;
    border-radius: 0;
    filter: drop-shadow(0 30px 34px rgba(76, 73, 73, 0.18));
}

.medivet-section,
.medivet-band {
    position: relative;
    padding: 58px 0;
    overflow: hidden;
}

.medivet-wave-top::before {
    top: -88px;
    transform: rotate(2deg);
}

.medivet-section {
    background: #ffffff;
}

.medivet-band {
    background: linear-gradient(180deg, #eefdff 0%, #ffffff 100%);
}

.medivet-band::before {
    top: -96px;
    border-radius: 0 0 50% 50%;
}

.medivet-section h2,
.medivet-band h2 {
    margin: 0 0 24px;
    color: var(--teal);
    font-size: clamp(25px, 3vw, 34px);
    font-weight: 900;
}

.medivet-slider-row,
.medivet-gallery-strip {
    display: grid;
    grid-template-columns: 34px minmax(0, 1fr) 34px;
    gap: 20px;
    align-items: center;
}

.medivet-arrow {
    width: 34px;
    height: 34px;
    border: 0;
    background: transparent;
    color: var(--teal);
    font-size: 18px;
}

.medivet-service-window {
    width: 100%;
    overflow: hidden;
    padding: 38px 4px 42px;
    perspective: 1100px;
}

.medivet-service-grid {
    position: relative;
    height: 306px;
    transform-style: preserve-3d;
}

.medivet-photo-card,
.medivet-contact-card,
.medivet-request-card,
.medivet-review-grid article,
.medivet-article-grid article {
    border: 1px solid rgba(126, 217, 217, 0.36);
    border-radius: 12px;
    background: #ffffff;
    box-shadow: 0 18px 44px rgba(3, 139, 166, 0.10);
}

.medivet-photo-card {
    position: relative;
    flex: 0 0 calc((100% - 60px) / 4);
    min-height: 190px;
    overflow: hidden;
    padding: 18px;
}

.medivet-service-carousel .medivet-photo-card {
    position: absolute;
    top: 18px;
    left: 50%;
    width: min(360px, 42vw);
    height: 260px;
    min-height: 260px;
    flex: none;
    opacity: 0;
    pointer-events: none;
    transform: translateX(-50%) translateZ(-180px) scale(0.58);
    transition: transform 0.58s ease, opacity 0.58s ease, filter 0.58s ease;
    will-change: transform, opacity;
}

.medivet-service-carousel .medivet-card-active {
    z-index: 5;
    opacity: 1;
    pointer-events: auto;
    transform: translateX(-50%) translateZ(120px) scale(1);
    filter: drop-shadow(0 22px 30px rgba(3, 139, 166, 0.18));
}

.medivet-service-carousel .medivet-card-left,
.medivet-service-carousel .medivet-card-right {
    z-index: 4;
    opacity: 0.82;
    pointer-events: auto;
}

.medivet-service-carousel .medivet-card-left {
    transform: translateX(calc(-50% - 280px)) rotateY(52deg) translateZ(-44px) scale(0.78);
}

.medivet-service-carousel .medivet-card-right {
    transform: translateX(calc(-50% + 280px)) rotateY(-52deg) translateZ(-44px) scale(0.78);
}

.medivet-service-carousel .medivet-card-far-left,
.medivet-service-carousel .medivet-card-far-right {
    z-index: 3;
    opacity: 0.5;
}

.medivet-service-carousel .medivet-card-far-left {
    transform: translateX(calc(-50% - 470px)) rotateY(64deg) translateZ(-120px) scale(0.62);
}

.medivet-service-carousel .medivet-card-far-right {
    transform: translateX(calc(-50% + 470px)) rotateY(-64deg) translateZ(-120px) scale(0.62);
}

.medivet-photo-card img {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    object-fit: cover;
    opacity: 0.78;
}

.medivet-photo-card::after {
    content: "";
    position: absolute;
    inset: 0;
    background: linear-gradient(180deg, rgba(255,255,255,0.68), rgba(255,255,255,0.06));
}

.medivet-photo-card h3,
.medivet-photo-card p,
.medivet-photo-card a {
    position: relative;
    z-index: 1;
}

.medivet-photo-card h3 {
    margin: 0;
    color: var(--teal);
    font-size: 16px;
    font-weight: 900;
}

.medivet-photo-card p {
    width: 82%;
    margin: 46px 0 16px;
    color: #ffffff;
    font-size: 12px;
    line-height: 1.55;
}

.medivet-photo-card a,
.medivet-article-grid .featured a {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border: 1px solid rgba(255,255,255,0.7);
    color: #ffffff;
    text-decoration: none;
}

.medivet-card-active {
    background: linear-gradient(135deg, var(--aqua), var(--teal));
}

.medivet-card-active img {
    opacity: 0.18;
    mix-blend-mode: multiply;
}

.medivet-card-active h3,
.medivet-card-active p {
    color: #ffffff;
}

.medivet-dots {
    display: flex;
    justify-content: center;
    gap: 7px;
    margin-top: 18px;
}

.medivet-dots span,
.medivet-dots button {
    width: 7px;
    height: 7px;
    padding: 0;
    border: 0;
    border-radius: 999px;
    background: rgba(3, 139, 166, 0.24);
}

.medivet-dots span.active,
.medivet-dots button.active {
    width: 18px;
    background: var(--aqua);
}

.medivet-tabs {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
    gap: 16px;
    margin: 0 0 30px;
}

.medivet-tab-row {
    display: grid;
    grid-template-columns: 34px minmax(0, 1fr) 34px;
    gap: 10px;
    align-items: center;
    margin: 0 0 30px;
}

.medivet-tab-row .medivet-tabs {
    margin: 0;
}

.medivet-tab-arrow {
    width: 34px;
    height: 42px;
    border: 0;
    border-radius: 999px;
    background: transparent;
    color: var(--teal);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 24px;
    transition: color 0.18s ease, transform 0.18s ease, background 0.18s ease;
}

.medivet-tab-arrow:hover {
    background: rgba(182, 242, 242, 0.55);
    color: #006f88;
    transform: translateY(-1px);
}

.medivet-tabs button {
    border: 1px solid rgba(3, 139, 166, 0.18);
    background: #ffffff;
    color: var(--dark);
    box-shadow: none;
}

.medivet-tabs button.active,
.medivet-tabs button[aria-pressed="true"] {
    border-color: var(--teal);
    background: linear-gradient(135deg, var(--aqua), var(--teal));
    color: #ffffff;
    box-shadow: 0 12px 24px rgba(3, 139, 166, 0.18);
}

.medivet-tabs button:focus-visible,
.medivet-dots button:focus-visible {
    outline: 3px solid rgba(40, 213, 213, 0.28);
    outline-offset: 3px;
}

.medivet-section .btn.btn-vet-secondary-accent,
.medivet-hero .btn.btn-vet-secondary-accent,
.medivet-contact-links .btn.btn-vet-secondary-accent {
    background: #fecd44 !important;
    border-color: #fecd44 !important;
    color: #4c4949 !important;
}

.medivet-section .btn.btn-vet-secondary-accent:hover,
.medivet-hero .btn.btn-vet-secondary-accent:hover,
.medivet-contact-links .btn.btn-vet-secondary-accent:hover {
    background: #efba2f !important;
    border-color: #efba2f !important;
    color: #4c4949 !important;
}

.medivet-price-layout {
    display: grid;
    grid-template-columns: 0.92fr 1.08fr;
    gap: 58px;
    align-items: center;
}

.medivet-price-photo,
.medivet-specialist-photo {
    width: 100%;
    min-height: 330px;
    object-fit: cover;
    border-radius: 12px;
    box-shadow: 0 22px 45px rgba(3, 139, 166, 0.16);
}

.medivet-price-panel {
    position: relative;
}

.medivet-price-panel::after {
    content: "\f0f1";
    position: absolute;
    right: -58px;
    bottom: 34px;
    color: var(--teal);
    font-family: "Font Awesome 6 Free";
    font-size: 112px;
    font-weight: 900;
    opacity: 0.30;
    transform: rotate(-12deg);
}

.medivet-price-panel table {
    width: 100%;
    overflow: hidden;
    border-collapse: collapse;
    border-radius: 12px;
    background: #ffffff;
    border: 1px solid #a7eeee;
    box-shadow: 0 14px 34px rgba(3, 139, 166, 0.08);
}

.medivet-price-panel th,
.medivet-price-panel td {
    padding: 13px 16px;
    border: 1px solid #a7eeee;
    color: var(--dark);
    font-size: 13px;
}

.medivet-price-panel th {
    background: #b6f2f2;
    color: #038ba6;
    font-weight: 900;
    text-transform: uppercase;
}

.medivet-price-panel tbody tr:nth-of-type(odd) td {
    background: #eefdff;
}

.medivet-price-panel tbody tr:nth-of-type(even) td {
    background: #ffffff;
}

.medivet-price-disclaimer {
    margin: 22px 0 0;
    line-height: 1.65;
}

.medivet-specialist-layout {
    display: grid;
    grid-template-columns: 250px minmax(260px, 330px) minmax(0, 1fr);
    gap: 34px;
    align-items: center;
}

.medivet-doctor-list {
    display: grid;
    gap: 14px;
}

.medivet-doctor-list button {
    min-height: 78px;
    border: 1px solid rgba(126, 217, 217, 0.36);
    border-radius: 12px;
    background: #ffffff;
    display: grid;
    grid-template-columns: 56px minmax(0, 1fr);
    gap: 12px;
    align-items: center;
    padding: 10px;
    text-align: left;
    box-shadow: 0 14px 34px rgba(3, 139, 166, 0.08);
}

.medivet-doctor-list button.active {
    background: linear-gradient(135deg, var(--aqua), var(--teal));
}

.medivet-doctor-list img {
    width: 56px;
    height: 56px;
    border-radius: 8px;
    object-fit: cover;
}

.medivet-doctor-list strong,
.medivet-doctor-list small {
    display: block;
}

.medivet-doctor-list strong {
    color: var(--dark);
    font-size: 13px;
}

.medivet-doctor-list small {
    margin-top: 4px;
    color: rgba(76, 73, 73, 0.68);
    font-size: 11px;
}

.medivet-doctor-list button.active strong,
.medivet-doctor-list button.active small {
    color: #ffffff;
}

.medivet-specialist-copy h3 {
    margin: 0 0 8px;
    color: var(--teal);
    font-size: 22px;
    font-weight: 900;
}

.medivet-specialist-copy h4 {
    margin: 22px 0 8px;
    color: var(--teal);
    font-size: 15px;
    font-weight: 900;
}

.medivet-specialist-copy p,
.medivet-specialist-copy li {
    color: rgba(76, 73, 73, 0.72);
    font-size: 14px;
    line-height: 1.7;
}

.medivet-specialist-copy ul {
    margin: 0 0 24px;
    padding-left: 18px;
}

.medivet-benefits {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 32px;
}

.medivet-benefits article i {
    color: var(--teal);
    font-size: 26px;
}

.medivet-benefits h3 {
    margin: 12px 0 8px;
    color: var(--teal);
    font-size: 16px;
    font-weight: 900;
}

.medivet-benefits p {
    margin: 0;
    color: rgba(76, 73, 73, 0.70);
    font-size: 13px;
}

.medivet-gallery-strip {
    grid-template-columns: 34px repeat(5, minmax(180px, 258px)) 34px;
    justify-content: center;
}

.medivet-gallery-strip img {
    width: 100%;
    height: 292px;
    object-fit: contain;
    object-position: center;
    padding: 6px;
    border: 1px solid rgba(126, 217, 217, 0.28);
    border-radius: 12px;
    box-shadow: 0 14px 30px rgba(3, 139, 166, 0.10);
    background: linear-gradient(180deg, #f8ffff 0%, #eefdff 100%);
}

.medivet-gallery-strip img:nth-of-type(3) {
    height: 322px;
    transform: translateY(-12px);
}

.medivet-gallery-strip img.active {
    border-color: rgba(3, 139, 166, 0.36);
    box-shadow: 0 16px 34px rgba(3, 139, 166, 0.12);
}

.medivet-review-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 28px;
}

.medivet-review-grid article {
    padding: 26px;
}

.medivet-review-grid article div {
    display: grid;
    grid-template-columns: 26px minmax(0, 1fr) auto;
    gap: 10px;
    align-items: center;
}

.medivet-review-grid i,
.medivet-contact-card i {
    color: var(--teal);
}

.medivet-review-grid strong {
    color: var(--teal);
    font-size: 14px;
}

.medivet-review-grid span {
    color: rgba(76, 73, 73, 0.60);
    font-size: 12px;
}

.medivet-review-grid p {
    margin: 16px 0 0;
    color: var(--dark);
    font-size: 14px;
}

.medivet-article-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 24px;
}

.medivet-article-grid article {
    position: relative;
    min-height: 180px;
    overflow: hidden;
    padding: 22px;
    display: flex;
    flex-direction: column;
    justify-content: space-between;
    align-items: flex-start;
    background: #eefdff;
}

.medivet-article-grid img {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.medivet-article-grid article::after {
    content: "";
    position: absolute;
    inset: 0;
    border-radius: inherit;
    background: linear-gradient(90deg, rgba(255,255,255,0.92), rgba(255,255,255,0.10));
}

.medivet-article-grid h3,
.medivet-article-grid p,
.medivet-article-grid a {
    position: relative;
    z-index: 1;
}

.medivet-article-grid h3 {
    max-width: 220px;
    margin: 0;
    color: var(--teal);
    font-size: 17px;
    font-weight: 900;
}

.medivet-article-grid article > a {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 126px;
    margin-top: 18px;
    padding: 10px 18px;
    border: 1px solid rgba(17, 145, 164, 0.22);
    border-radius: 999px;
    background: rgba(255, 255, 255, 0.9);
    color: var(--teal);
    font-size: 12px;
    font-weight: 800;
    text-decoration: none;
    text-transform: uppercase;
}

.medivet-article-grid .featured {
    display: block;
    background: linear-gradient(135deg, var(--aqua), var(--teal));
}

.medivet-article-grid .featured::after {
    display: none;
}

.medivet-article-grid .featured h3,
.medivet-article-grid .featured p {
    color: #ffffff;
}

.medivet-article-grid .featured > a {
    border-color: rgba(255, 255, 255, 0.78);
    background: #ffffff;
    color: var(--teal);
}

.medivet-contact-grid {
    display: grid;
    grid-template-columns: 0.92fr 1.08fr;
    gap: 28px;
    align-items: stretch;
}

.medivet-contact-card,
.medivet-request-card {
    padding: 44px 46px;
}

.medivet-contact-card p {
    display: flex;
    gap: 10px;
    align-items: center;
    margin: 0 0 12px;
    color: var(--dark);
    font-size: 14px;
}

.medivet-map {
    min-height: 210px;
    margin-top: 18px;
    border-radius: 10px;
    background: #ffffff;
    border: 1px solid rgba(126, 217, 217, 0.36);
    overflow: hidden;
}

.medivet-map img {
    display: block;
    width: 100%;
    min-height: 210px;
    object-fit: cover;
    object-position: center;
}

.medivet-contact-links {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
    margin-top: 18px;
}

.medivet-contact-links .btn {
    min-width: 170px;
    border-radius: 999px !important;
    padding: 11px 20px;
}

@media (max-width: 520px) {
    .medivet-contact-links .btn {
        width: 100%;
    }
}

.medivet-request-card {
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(210px, 280px);
    gap: 26px;
    align-items: start;
}

.medivet-request-card h2 {
    margin-top: 0;
}

.medivet-request-card > img {
    width: min(100%, 270px);
    height: 320px;
    object-fit: contain;
    object-position: center;
    filter: drop-shadow(0 18px 18px rgba(76, 73, 73, 0.16));
    justify-self: center;
    align-self: start;
    margin-top: 42px;
}

.medivet-request-card form {
    display: grid;
    gap: 14px;
    max-width: 520px;
}

.medivet-request-feedback {
    margin: 0;
    color: var(--dark);
    font-size: 13px;
    line-height: 1.5;
}

.medivet-request-feedback.is-success {
    color: #038ba6;
}

.medivet-request-feedback.is-error {
    color: #b42318;
}

.medivet-form-row {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 14px;
}

.medivet-request-card .btn {
    justify-self: start;
}

@media (max-width: 980px) {
    .medivet-hero-grid,
    .medivet-price-layout,
    .medivet-specialist-layout,
    .medivet-contact-grid,
    .medivet-request-card {
        grid-template-columns: 1fr;
    }

    .medivet-hero-grid {
        gap: 38px;
        text-align: center;
    }

    .medivet-hero-copy h1,
    .medivet-hero-copy p {
        margin-left: auto;
        margin-right: auto;
    }

    .medivet-actions {
        justify-content: center;
    }

    .medivet-benefits,
    .medivet-article-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }

    .medivet-photo-card {
        flex-basis: calc((100% - 20px) / 2);
    }

    .medivet-service-carousel .medivet-photo-card {
        width: min(330px, 52vw);
    }

    .medivet-service-carousel .medivet-card-left {
        transform: translateX(calc(-50% - 215px)) rotateY(50deg) translateZ(-48px) scale(0.74);
    }

    .medivet-service-carousel .medivet-card-right {
        transform: translateX(calc(-50% + 215px)) rotateY(-50deg) translateZ(-48px) scale(0.74);
    }

    .medivet-service-carousel .medivet-card-far-left {
        transform: translateX(calc(-50% - 360px)) rotateY(62deg) translateZ(-128px) scale(0.56);
    }

    .medivet-service-carousel .medivet-card-far-right {
        transform: translateX(calc(-50% + 360px)) rotateY(-62deg) translateZ(-128px) scale(0.56);
    }

    .medivet-tabs {
        grid-template-columns: repeat(3, minmax(96px, 1fr));
    }

    .medivet-gallery-strip {
        grid-template-columns: 34px repeat(3, minmax(150px, 224px)) 34px;
    }

    .medivet-gallery-strip img:nth-of-type(n+4) {
        display: none;
    }

    .medivet-request-card > img {
        justify-self: center;
        width: min(100%, 220px);
        height: 280px;
        margin-top: 0;
    }
}

@media (max-width: 640px) {
    .medivet-side-circle-blue {
        width: 250px;
        height: 250px;
        left: -170px;
    }

    .medivet-side-circle-yellow {
        width: 230px;
        height: 230px;
        right: -160px;
    }

    .medivet-container {
        width: min(100% - 28px, 1240px);
    }

    .medivet-hero {
        padding-top: 42px;
        min-height: 0;
    }

    .medivet-hero-visual {
        min-height: 280px;
    }

    .medivet-benefits,
    .medivet-review-grid,
    .medivet-article-grid,
    .medivet-form-row {
        grid-template-columns: 1fr;
    }

    .medivet-request-card > img {
        width: min(100%, 190px);
        height: 240px;
    }

    .medivet-photo-card {
        flex-basis: 100%;
    }

    .medivet-service-grid {
        height: 286px;
    }

    .medivet-service-carousel .medivet-photo-card {
        width: min(100%, 310px);
        height: 248px;
        min-height: 248px;
    }

    .medivet-service-carousel .medivet-card-left {
        opacity: 0.5;
        transform: translateX(calc(-50% - 155px)) rotateY(54deg) translateZ(-90px) scale(0.68);
    }

    .medivet-service-carousel .medivet-card-right {
        opacity: 0.5;
        transform: translateX(calc(-50% + 155px)) rotateY(-54deg) translateZ(-90px) scale(0.68);
    }

    .medivet-service-carousel .medivet-card-far-left,
    .medivet-service-carousel .medivet-card-far-right {
        opacity: 0;
        pointer-events: none;
    }

    .medivet-tabs {
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 10px;
    }

    .medivet-slider-row,
    .medivet-gallery-strip {
        grid-template-columns: 1fr;
    }

    .medivet-arrow {
        display: none;
    }

    .medivet-gallery-strip img,
    .medivet-gallery-strip img:nth-of-type(3) {
        display: block;
        height: 270px;
        transform: none;
    }

    .medivet-gallery-strip img:nth-of-type(n+4) {
        display: none;
    }
}

table > tbody > tr:nth-of-type(odd) > th,
table > tbody > tr:nth-of-type(odd) > td {
    background-color: #ffffff !important;
}

table > tbody > tr:nth-of-type(even) > th,
table > tbody > tr:nth-of-type(even) > td {
    background-color: #ffffff !important;
}

table > tbody > tr:hover > th,
table > tbody > tr:hover > td {
    background-color: #f8fbff !important;
}

.medivet-price-panel tbody tr:nth-of-type(odd) td {
    background: #eefdff !important;
}

.medivet-price-panel tbody tr:nth-of-type(even) td {
    background: #ffffff !important;
}

.medivet-price-panel tbody tr:nth-of-type(odd):hover td {
    background: #eefdff !important;
}

.medivet-price-panel tbody tr:nth-of-type(even):hover td {
    background: #ffffff !important;
}

.app-container--client-dashboard .med-welcome-card {
    background: #ffffff !important;
    border: 1px solid var(--line) !important;
}

.app-container--client-dashboard .med-results-card {
    background: #ffffff !important;
    border: 1px solid var(--line) !important;
}

.app-container--client-dashboard .med-welcome-card p,
.app-container--client-dashboard .med-welcome-card h2,
.app-container--client-dashboard .med-results-card span,
.app-container--client-dashboard .med-results-card h2,
.app-container--client-dashboard .med-results-card strong {
    color: #34474c !important;
}

.app-container--client-dashboard .med-icon-link {
    border-color: rgba(3, 139, 166, 0.36) !important;
    color: #038ba6 !important;
}

.app-container--client-dashboard .result-row:nth-of-type(2) b {
    background: #f3c64d !important;
}

.guest-booking-shell {
    padding: 44px 0 64px;
    background: linear-gradient(180deg, #ffffff 0%, #f5fdff 100%);
}

.guest-booking-container {
    width: min(100% - 48px, 980px);
    margin: 0 auto;
}

.guest-booking-heading {
    max-width: 720px;
    margin: 0 auto 22px;
    text-align: center;
}

.guest-booking-heading span {
    color: var(--brand);
    font-size: 12px;
    font-weight: 900;
    text-transform: uppercase;
}

.guest-booking-heading h2 {
    margin: 6px 0 8px;
    color: var(--brand);
    font-size: clamp(30px, 4vw, 42px);
    font-weight: 900;
}

.guest-booking-heading p {
    max-width: 620px;
    margin: 0;
    margin-left: auto;
    margin-right: auto;
    color: var(--muted);
    font-size: 15px;
}

.guest-booking-card {
    padding: 30px 36px 34px;
    border: 1px solid rgba(126, 217, 217, 0.35);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.92);
    box-shadow: 0 22px 52px rgba(3, 139, 166, 0.10);
}

.guest-booking-notes {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 12px;
    margin-bottom: 24px;
}

.guest-booking-notes > div {
    display: grid;
    grid-template-columns: 34px minmax(0, 1fr);
    gap: 11px;
    align-items: start;
    min-height: 0;
    padding: 14px;
    border: 1px solid rgba(126, 217, 217, 0.38);
    border-radius: 14px;
    background: rgba(248, 254, 255, 0.92);
    box-shadow: 0 10px 22px rgba(3, 139, 166, 0.05);
}

.guest-booking-notes > div:nth-child(2) {
    border-color: rgba(243, 182, 63, 0.42);
    background: rgba(255, 250, 240, 0.94);
}

.guest-booking-notes > div > span {
    display: grid;
    place-items: center;
    width: 34px;
    height: 34px;
    border-radius: 12px;
    background: var(--brand-soft);
    color: var(--brand);
    font-size: 14px;
}

.guest-booking-notes > div:nth-child(2) > span {
    background: #fff1bd;
    color: #8a6100;
}

.guest-booking-notes strong {
    display: block;
    margin: 1px 0 5px;
    color: var(--brand-dark);
    font-size: 13px;
    line-height: 1.25;
}

.guest-booking-notes p {
    margin: 0;
    color: #4f6269;
    font-size: 12px;
    line-height: 1.55;
}

.guest-form-section {
    padding: 20px 0 22px;
    border-top: 1px solid #e8f3f5;
}

.guest-form-section:first-of-type {
    border-top: 0;
    padding-top: 0;
}

.guest-form-section h3 {
    margin: 0 0 14px;
    color: #3f4f55;
    font-size: 18px;
    font-weight: 900;
}

.guest-booking-card label {
    color: var(--brand);
    font-size: 12px;
}

.guest-booking-card .form-control {
    min-height: 40px;
    border-color: #bfe5ea !important;
    background: #ffffff;
}

.guest-booking-card textarea.form-control {
    min-height: 78px;
    resize: vertical;
}

.guest-booking-card .text-danger {
    display: block;
    margin-top: 6px;
    font-size: 12px;
    font-weight: 700;
}

.guest-booking-actions {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
    padding-top: 8px;
    border-top: 1px solid #e8f3f5;
}

.guest-booking-actions .btn {
    border-radius: 999px !important;
    padding: 11px 22px;
}

@media (max-width: 900px) {
    .appointment-create-notes {
        grid-template-columns: 1fr;
    }

    .appointment-request-form {
        padding: 24px !important;
    }

    .appointment-emergency-toggle {
        align-items: stretch;
        flex-direction: column;
    }

    .appointment-emergency-toggle__switch {
        align-self: flex-end;
        min-height: 30px;
    }

    .guest-booking-notes {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 640px) {
    .guest-booking-shell {
        padding: 36px 0 52px;
    }

    .guest-booking-container {
        width: min(100% - 28px, 980px);
    }

    .guest-booking-card {
        padding: 20px;
    }

    .guest-booking-actions .btn {
        width: 100%;
    }
}

/* Responsive safety pass */
html,
body {
    max-width: 100%;
    overflow-x: hidden;
}

*,
*::before,
*::after {
    box-sizing: border-box;
}

img,
svg,
video,
canvas,
iframe {
    max-width: 100%;
}

img,
video {
    height: auto;
}

.mobile-nav-toggle {
    display: none;
    align-items: center;
    justify-content: center;
    gap: 9px;
    width: 100%;
    min-height: 42px;
    border: 1px solid rgba(3, 139, 166, 0.22);
    border-radius: 14px;
    background: #ffffff;
    color: var(--teal);
    font-weight: 900;
    box-shadow: 0 10px 22px rgba(3, 139, 166, 0.08);
}

.top-logo,
.top-logo > div,
.main-landing-nav,
.app-container,
.app-container--standard,
.app-container--client-dashboard,
.table-panel,
.med-dashboard,
.med-main,
.med-side-rail,
.med-profile-panel,
.guest-booking-card,
.guest-booking-container,
.action-modal-content,
.admin-confirm-modal,
.vet-toast {
    min-width: 0;
}

.app-container--standard > table.table,
.table-panel table.table {
    width: 100%;
}

.table-panel {
    max-width: 100%;
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
}

.table-panel > form[method="get"],
.table-panel > p,
.table-panel > .table-pagination {
    min-width: 0;
}

.app-container--standard .table,
.table-panel .table {
    vertical-align: middle;
}

.form-control,
.form-select,
textarea,
select,
input {
    max-width: 100%;
}

.action-modal-dialog {
    width: min(920px, calc(100vw - 32px));
    max-width: calc(100vw - 32px);
    margin-left: auto;
    margin-right: auto;
}

.action-modal-body {
    max-width: 100%;
    overflow-x: hidden;
}

.action-modal-body table,
.admin-confirm-modal table {
    display: block;
    width: 100%;
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
}

.appointment-calendar-popover,
.calendar-popover,
.schedule-popover {
    max-width: min(320px, calc(100vw - 28px));
}

@media (max-width: 1180px) {
    .main-header {
        padding-left: 24px;
        padding-right: 24px;
    }

    .top-clinic-bar {
        grid-template-columns: minmax(0, 1fr) auto;
        gap: 16px;
    }

    .main-landing-nav {
        gap: 14px 22px;
    }

    .main-landing-nav a {
        padding: 8px 10px !important;
        border-radius: 999px !important;
    }

    .app-container,
    .app-container--client-dashboard {
        width: min(100% - 36px, 1180px);
    }

    .medivet-container,
    .vet-section,
    .guest-booking-container {
        width: min(100% - 36px, 1120px);
    }

    .med-dashboard,
    .admin-dashboard-grid,
    .dashboard-summary-grid,
    .staff-dashboard-grid {
        grid-template-columns: 1fr !important;
    }

    .med-profile-panel,
    .med-side-rail {
        position: static !important;
        width: 100%;
    }

    .med-action-row,
    .stats-grid,
    .dashboard-cards,
    .admin-stat-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr)) !important;
    }
}

@media (max-width: 900px) {
    .medivet-hero-grid,
    .medivet-contact-grid,
    .medivet-price-layout,
    .medivet-specialist-layout,
    .medivet-request-grid,
    .guest-booking-notes,
    .appointment-create-notes,
    .account-settings-grid,
    .login-shell,
    .auth-shell {
        grid-template-columns: 1fr !important;
    }

    .medivet-tabs,
    .medivet-specialist-tabs,
    .medivet-article-tabs {
        overflow-x: auto;
        justify-content: flex-start;
        scroll-snap-type: x proximity;
        -webkit-overflow-scrolling: touch;
    }

    .medivet-tabs > *,
    .medivet-specialist-tabs > *,
    .medivet-article-tabs > * {
        flex: 0 0 auto;
        scroll-snap-align: start;
    }

    .med-action-row,
    .stats-grid,
    .dashboard-cards,
    .admin-stat-grid {
        grid-template-columns: 1fr !important;
    }

    .chart-card,
    .chart-card--appointments,
    .appointments-overview-card {
        min-height: 260px;
        overflow: hidden;
    }

    .chart-card canvas,
    .chart-card--appointments canvas,
    .appointments-overview-card canvas {
        width: 100% !important;
        height: auto !important;
        min-height: 220px;
    }
}

@media (max-width: 760px) {
    .main-header {
        padding: 10px 14px;
    }

    .main-header--public {
        padding-top: 12px;
    }

    .main-header--account {
        padding-top: 12px;
    }

    .main-header--public .top-clinic-bar {
        grid-template-columns: 1fr;
        justify-items: center;
        gap: 12px;
        margin-bottom: 10px;
    }

    .main-header--account .top-clinic-bar {
        grid-template-columns: 1fr;
        justify-items: center;
        gap: 12px;
        margin-bottom: 10px;
    }

    .main-header--public .top-logo {
        justify-content: center;
        text-align: center;
    }

    .main-header--account .top-logo {
        justify-content: center;
        text-align: center;
    }

    .main-header--public .top-header-actions {
        justify-content: center;
        width: 100%;
        flex-wrap: wrap;
    }

    .main-header--account .top-header-actions {
        justify-content: center;
        width: 100%;
        flex-wrap: wrap;
    }

    .top-clinic-bar {
        grid-template-columns: minmax(0, 1fr) auto;
        align-items: center;
        margin-bottom: 10px;
    }

    .top-logo {
        gap: 10px;
    }

    .brand-icon {
        width: 42px;
        height: 42px;
        flex: 0 0 42px;
    }

    .top-logo strong {
        font-size: 18px;
        line-height: 1.05;
    }

    .top-logo small {
        font-size: 11px;
        line-height: 1.2;
    }

    .top-header-actions {
        gap: 8px;
    }

    .top-header-actions .btn {
        min-height: 36px;
        padding: 8px 14px;
    }

    .mobile-nav-toggle {
        display: inline-flex;
    }

    .main-landing-nav {
        display: none;
        grid-template-columns: 1fr;
        gap: 8px;
        margin-top: 10px;
        padding: 10px;
        border: 1px solid rgba(126, 217, 217, 0.38);
        border-radius: 18px;
        background: rgba(255, 255, 255, 0.96);
        box-shadow: 0 16px 34px rgba(3, 139, 166, 0.12);
        overflow: visible;
    }

    .main-landing-nav--public {
        margin-top: 10px;
        padding-top: 0;
        border-top: 0;
    }

    .main-landing-nav--account {
        margin-top: 10px;
        padding-top: 0;
        border-top: 0;
    }

    .main-landing-nav.is-open {
        display: grid;
    }

    .main-landing-nav a {
        display: block;
        width: 100%;
        padding: 11px 12px !important;
        border-radius: 12px !important;
        text-align: center;
        white-space: normal;
    }

    .app-container,
    .app-container--client-dashboard,
    .medivet-container,
    .vet-section,
    .guest-booking-container {
        width: min(100% - 24px, 640px);
    }

    .app-container {
        padding-top: 26px;
        padding-bottom: 42px;
    }

    .app-container--standard > h1,
    .app-container--standard > h2,
    .app-container--standard > h3,
    .med-topbar h1,
    .guest-booking-heading h2 {
        font-size: clamp(28px, 9vw, 40px) !important;
        line-height: 1.08;
    }

    .table-panel {
        padding: 14px;
        border-radius: 16px;
    }

    .table-panel table.table,
    .app-container--standard > table.table {
        min-width: 680px;
    }

    .table-pagination {
        align-items: stretch;
        flex-direction: column;
        gap: 12px;
    }

    .table-pagination__buttons {
        justify-content: space-between;
        width: 100%;
    }

    .row {
        --bs-gutter-x: 1rem;
    }

    .row > [class*="col-"] {
        margin-bottom: 12px;
    }

    .guest-booking-card,
    .account-settings-card,
    .app-container--standard > form[method="post"],
    .app-container--standard > .card {
        width: 100%;
        max-width: 100%;
        padding: 20px !important;
        border-radius: 16px;
    }

    .guest-booking-actions,
    .form-actions,
    .account-actions,
    .modal-actions {
        align-items: stretch;
        flex-direction: column;
    }

    .guest-booking-actions .btn,
    .form-actions .btn,
    .account-actions .btn,
    .app-container--standard > form[method="post"] > .btn,
    .app-container--standard > form[method="post"] > a.btn {
        width: 100%;
        margin-left: 0 !important;
    }

    .calendar-grid,
    .calendar-days {
        gap: 6px !important;
    }

    .calendar-grid > *,
    .calendar-days > * {
        min-width: 0;
    }

    .calendar-day,
    .calendar-days button,
    .calendar-days span {
        min-height: 46px;
        padding: 6px;
        font-size: 13px !important;
    }

    .medivet-gallery-strip {
        overflow-x: auto;
        justify-content: flex-start;
        scroll-snap-type: x proximity;
        -webkit-overflow-scrolling: touch;
    }

    .medivet-gallery-strip img {
        flex: 0 0 min(76vw, 280px);
        scroll-snap-align: center;
        object-fit: cover;
    }

    .action-modal-dialog {
        width: calc(100vw - 20px);
        max-width: calc(100vw - 20px);
    }

    .action-modal-header,
    .action-modal-body {
        padding-left: 18px !important;
        padding-right: 18px !important;
    }

    .admin-confirm-modal,
    .vet-toast {
        width: min(100%, calc(100vw - 28px));
    }

    .admin-confirm-actions {
        flex-direction: column;
        gap: 10px;
    }

    .admin-confirm-actions .btn,
    .vet-toast-action {
        width: 100%;
    }
}

@media (max-width: 520px) {
    .top-clinic-bar {
        grid-template-columns: 1fr;
        justify-items: center;
    }

    .top-logo {
        justify-content: center;
        text-align: center;
    }

    .top-header-actions {
        justify-content: center;
        width: 100%;
        flex-wrap: wrap;
    }

    .medivet-side-circle-yellow {
        right: -180px;
    }

    .medivet-side-circle-blue {
        left: -150px;
    }

    .table-panel table.table,
    .app-container--standard > table.table {
        min-width: 620px;
    }

    .guest-booking-notes > div,
    .appointment-create-notes > div {
        grid-template-columns: 1fr;
        text-align: left;
    }

    .btn-group,
    .table-actions {
        flex-wrap: wrap;
    }

    .action-modal-body .btn,
    .action-modal-body a.btn,
    .action-modal-body button.btn {
        width: 100%;
        margin: 4px 0 !important;
    }

    .action-modal-body .table-actions .btn,
    .action-modal-body .table-action-icon {
        width: auto;
    }
}

/* Focused UI fixes requested after responsive pass */
:root {
    --page: #f1f8f9;
    --brand-soft: #e9f7f8;
}

.top-logo {
    text-decoration: none !important;
}

.top-logo:hover,
.top-logo:focus {
    text-decoration: none !important;
}

.btn-danger,
.btn-danger:hover,
.btn-danger:focus,
.btn-danger:active,
.admin-confirm-actions .btn-danger,
.admin-confirm-actions .btn-danger:hover,
.action-modal-body .btn-danger,
.action-modal-body .btn-danger:hover,
.vet-toast .btn-danger,
.vet-toast .btn-danger:hover {
    color: #ffffff !important;
}

.medivet-service-carousel .medivet-photo-card {
    background: #f9feff !important;
    transition: transform 0.22s ease, box-shadow 0.22s ease, border-color 0.22s ease, filter 0.22s ease;
}

.medivet-service-carousel .medivet-photo-card img {
    opacity: 0.24;
    mix-blend-mode: normal;
}

.medivet-service-carousel .medivet-photo-card::after {
    background: transparent;
}

.medivet-service-carousel .medivet-photo-card h3 {
    color: var(--teal) !important;
}

.medivet-service-carousel .medivet-photo-card p {
    color: #34474c !important;
    font-weight: 700;
    text-shadow: none;
}

.medivet-service-carousel .medivet-card-active {
    background: linear-gradient(135deg, rgba(40, 213, 213, 0.88), rgba(3, 139, 166, 0.88)) !important;
}

.medivet-service-carousel .medivet-card-active h3,
.medivet-service-carousel .medivet-card-active p {
    color: #ffffff !important;
}

.medivet-service-carousel .medivet-card-active img {
    opacity: 0.20;
}

.medivet-photo-card a,
.medivet-article-grid .featured a,
.medivet-tabs button,
.medivet-tab-arrow {
    transition: transform 0.22s ease, box-shadow 0.22s ease, background-color 0.22s ease, border-color 0.22s ease, color 0.22s ease;
}

.medivet-photo-card a:hover,
.medivet-article-grid .featured a:hover,
.medivet-tabs button:hover,
.medivet-tab-arrow:hover {
    transform: translateY(-2px);
    box-shadow: 0 10px 22px rgba(3, 139, 166, 0.14);
}

.medivet-article-grid article:hover,
.medivet-benefits article:hover,
.medivet-review-grid article:hover,
.card:hover,
.med-action-card:hover {
    transform: translateY(-3px);
    box-shadow: 0 18px 38px rgba(24, 50, 58, 0.12);
}


.auth-shell,
.auth-logo-card,
.card,
.card-body,
.content-card,
.review-card,
.contact-card,
.price-table-card,
.table-panel,
.admin-users-panel,
.chart-card,
.chart-card--appointments,
.appointments-overview-card,
.med-welcome-card,
.med-results-card,
.med-action-card,
.med-profile-panel,
.med-calendar-card,
.medivet-contact-card,
.medivet-request-card,
.medivet-price-panel,
.medivet-photo-card,
.medivet-benefits article,
.medivet-review-grid article,
.medivet-article-grid article,
.guest-booking-card,
.guest-booking-container,
.guest-booking-notes > div,
.appointment-create-notes > div,
.account-settings-shell,
.account-settings-card,
.app-container--standard > form[method="post"],
.action-modal-content,
.modal-content,
.admin-confirm-modal,
.vet-toast {
    background-color: #ffffff !important;
    backdrop-filter: none !important;
    opacity: 1 !important;
}

.app-container--client-dashboard .med-main-panel {
    background: transparent !important;
    box-shadow: none !important;
    border: 0 !important;
    backdrop-filter: none !important;
}

.page-shell,
.page-shell > .container-fluid,
.app-container,
.medivet-section,
.medivet-container,
.guest-booking-shell,
.guest-booking-container,
.auth-page,
.account-settings-shell {
    position: relative;
    z-index: 3;
}

.account-settings-shell {
    background-color: transparent !important;
}

.account-settings-card {
    background-color: #ffffff !important;
}

.medivet-side-circles {
    z-index: 1;
}

@media (max-width: 760px) {
    .top-logo {
        width: 100%;
        justify-content: center;
    }

    .main-landing-nav a {
        margin: 0;
    }
}

/* Keep login/register cards solid above the decorative background circles. */
.auth-page {
    position: relative;
    z-index: 3;
    isolation: isolate;
}

.auth-shell,
.auth-form-panel,
.auth-visual-panel,
.auth-logo-card {
    opacity: 1 !important;
    backdrop-filter: none !important;
}

.auth-shell,
.auth-form-panel,
.auth-logo-card {
    background-color: #ffffff !important;
}

.auth-visual-panel {
    background-color: #b6f2f2 !important;
}

/* Gentle color calibration for phone and tablet screens */
:root {
    --aqua: #30cfd1;
    --teal: #1191a4;
    --brand-soft: #dceff1;
    --page: #e4f1f3;
}

.main-header {
    background: rgba(216, 238, 241, 0.98);
}

.top-socials a,
.brand-icon {
    background: #aee7e8;
}

.medivet-side-circle-blue {
    background: rgba(32, 197, 200, 0.10);
}

.medivet-side-circle-yellow {
    background: rgba(238, 183, 68, 0.13);
}

.medivet-tabs button.active,
.medivet-tabs button[aria-pressed="true"],
.medivet-doctor-list button.active,
.medivet-article-grid .featured,
.medivet-article-grid .featured::before {
    background: linear-gradient(135deg, rgba(48, 207, 209, 0.86), rgba(17, 145, 164, 0.86));
}

.medivet-service-carousel .medivet-card-active {
    background: linear-gradient(135deg, rgba(48, 207, 209, 0.82), rgba(17, 145, 164, 0.84)) !important;
}

.medivet-price-panel th {
    background: #ace7e8;
}

.medivet-price-panel tbody tr:nth-of-type(odd) td,
.medivet-price-panel tbody tr:nth-of-type(odd):hover td,
.table-pagination button:hover:not(:disabled),
.medivet-article-grid {
    background: #edf8f9 !important;
}

.guest-booking-shell {
    background: linear-gradient(180deg, #f7fcfc 0%, #e6f2f4 100%);
}

@media (max-width: 1024px) {
    body {
        background-color: #e4f1f3;
    }

    .main-header {
        background: rgba(216, 238, 241, 0.99) !important;
    }

    .mobile-nav-toggle,
    .main-landing-nav {
        background: #f6fbfc;
    }
}

@media (min-width: 761px) and (max-width: 1180px) {
    .top-clinic-bar {
        grid-template-columns: minmax(0, 1fr) auto;
        gap: 12px 18px;
    }

    .top-header-actions {
        justify-content: flex-end;
    }
}

#services,
#prices,
#articles,
#contact {
    scroll-margin-top: 150px;
}

@media (max-width: 760px) {
    html,
    body {
        max-width: 100%;
        overflow-x: hidden;
    }

    .medivet-tab-row {
        grid-template-columns: 38px minmax(0, 1fr) 38px;
        gap: 8px;
    }

    .medivet-tab-row .medivet-tabs {
        display: flex;
        flex-wrap: nowrap;
        max-width: 100%;
        min-width: 0;
        overflow-x: auto;
        overflow-y: hidden;
        padding: 2px 2px 8px;
        scroll-snap-type: x proximity;
        -webkit-overflow-scrolling: touch;
    }

    .medivet-tab-row .medivet-tabs button {
        flex: 0 0 min(168px, 72vw);
        scroll-snap-align: center;
    }

    .medivet-tab-row .medivet-tab-arrow {
        width: 36px;
        min-width: 36px;
        height: 40px;
        padding: 0;
        align-self: center;
    }

    .medivet-price-layout,
    .medivet-price-panel {
        min-width: 0;
        max-width: 100%;
    }

    .medivet-price-panel {
        overflow-x: auto;
        overflow-y: visible;
        padding-bottom: 6px;
        -webkit-overflow-scrolling: touch;
    }

    .medivet-price-panel table {
        min-width: 560px;
    }

    .medivet-price-panel::after {
        display: none;
    }

    .medivet-service-carousel.medivet-slider-row {
        grid-template-columns: 38px minmax(0, 1fr) 38px;
        align-items: center;
    }

    .medivet-service-carousel .medivet-arrow {
        display: inline-flex;
        width: 36px;
        height: 36px;
        min-width: 36px;
        align-self: center;
    }

    .medivet-service-window {
        min-width: 0;
    }

    #services,
    #prices,
    #articles,
    #contact {
        scroll-margin-top: 210px;
    }
}




```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Program.cs

`$lang
using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Appointments;
using VetClinicSystem.Repositories.Clinics; 
using VetClinicSystem.Repositories.MedicalRecords;
using VetClinicSystem.Repositories.Notifications;
using VetClinicSystem.Repositories.Pets;
using VetClinicSystem.Repositories.Services;
using VetClinicSystem.Repositories.Users;
using VetClinicSystem.Repositories.Vaccinations;
using VetClinicSystem.Services.Appointments;
using VetClinicSystem.Services.Clinics;
using VetClinicSystem.Services.MedicalRecords;
using VetClinicSystem.Services.Notifications;
using VetClinicSystem.Services.Pets;
using VetClinicSystem.Services.Services;
using VetClinicSystem.Services.Users;
using VetClinicSystem.Services.Vaccinations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<VetClinicDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPetRepository, PetRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IVaccinationRepository, VaccinationRepository>();
builder.Services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
builder.Services.AddScoped<IClinicRepository, ClinicRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IServiceManager, ServiceManager>();
builder.Services.AddScoped<IVaccinationService, VaccinationService>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();
builder.Services.AddScoped<IClinicService, ClinicService>();

var app = builder.Build();

EnsureApplicationSchema(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.Use(async (context, next) =>
{
    var notificationService = context.RequestServices.GetService<INotificationService>();
    var roleId = context.Session.GetInt32("RoleId");
    var userId = context.Session.GetInt32("UserId");

    if (notificationService != null && (roleId == 1 || roleId == 2))
    {
        context.Items["NotificationCount"] = notificationService.GetUnreadForStaff().Count;
    }
    else if (notificationService != null && roleId == 3 && userId.HasValue)
    {
        context.Items["NotificationCount"] = notificationService.GetUnreadForUser(userId.Value).Count;
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void EnsureApplicationSchema(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();

    context.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('Users', 'MustChangePassword') IS NULL
BEGIN
    ALTER TABLE Users
    ADD MustChangePassword BIT NOT NULL DEFAULT 0;
END

IF COL_LENGTH('Users', 'LastPasswordChange') IS NULL
BEGIN
    ALTER TABLE Users
    ADD LastPasswordChange DATETIME NULL;
END

IF COL_LENGTH('Users', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE Users
    ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT 0;
END

IF COL_LENGTH('Users', 'DeletedAt') IS NULL
BEGIN
    ALTER TABLE Users
    ADD DeletedAt DATETIME NULL;
END

IF COL_LENGTH('Users', 'DeletedByUserId') IS NULL
BEGIN
    ALTER TABLE Users
    ADD DeletedByUserId INT NULL;
END

IF COL_LENGTH('Users', 'DeleteReason') IS NULL
BEGIN
    ALTER TABLE Users
    ADD DeleteReason NVARCHAR(255) NULL;
END

IF COL_LENGTH('Appointments', 'PreferredAppointmentDate') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD PreferredAppointmentDate DATE NULL;
END

IF COL_LENGTH('Appointments', 'PreferredAppointmentTime') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD PreferredAppointmentTime TIME NULL;
END

IF COL_LENGTH('Appointments', 'SurgeryCategory') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD SurgeryCategory NVARCHAR(20) NULL;
END

IF COL_LENGTH('Appointments', 'SurgeryLoadPoints') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD SurgeryLoadPoints INT NOT NULL CONSTRAINT DF_Appointments_SurgeryLoadPoints DEFAULT 0;
END

IF COL_LENGTH('Appointments', 'IsEmergency') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD IsEmergency BIT NOT NULL CONSTRAINT DF_Appointments_IsEmergency DEFAULT 0;
END

IF COL_LENGTH('Appointments', 'IsScheduleFinalized') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD IsScheduleFinalized BIT NOT NULL CONSTRAINT DF_Appointments_IsScheduleFinalized DEFAULT 0;
END

IF COL_LENGTH('StaffNotifications', 'UserId') IS NULL
BEGIN
    ALTER TABLE StaffNotifications
    ADD UserId INT NULL;
END

IF COL_LENGTH('StaffNotifications', 'RecipientRole') IS NULL
BEGIN
    ALTER TABLE StaffNotifications
    ADD RecipientRole NVARCHAR(20) NOT NULL CONSTRAINT DF_StaffNotifications_RecipientRole DEFAULT 'Staff';
END

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('StaffNotifications')
      AND name = 'AppointmentId'
      AND is_nullable = 0)
BEGIN
    DECLARE @dropAppointmentFkSql NVARCHAR(MAX) = N'';
    SELECT @dropAppointmentFkSql = @dropAppointmentFkSql +
        N'ALTER TABLE StaffNotifications DROP CONSTRAINT [' + fk.name + N'];'
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('StaffNotifications')
      AND c.name = 'AppointmentId';

    IF (@dropAppointmentFkSql <> N'')
        EXEC sp_executesql @dropAppointmentFkSql;

    ALTER TABLE StaffNotifications
    ALTER COLUMN AppointmentId INT NULL;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('Users')
      AND c.name = 'DeletedByUserId')
BEGIN
    ALTER TABLE Users
    ADD CONSTRAINT FK_Users_Users_DeletedByUserId FOREIGN KEY (DeletedByUserId) REFERENCES Users(Id);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('StaffNotifications')
      AND c.name = 'AppointmentId')
BEGIN
    ALTER TABLE StaffNotifications
    ADD CONSTRAINT FK_StaffNotifications_Appointments_AppointmentId FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('StaffNotifications')
      AND c.name = 'UserId')
BEGIN
    ALTER TABLE StaffNotifications
    ADD CONSTRAINT FK_StaffNotifications_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id);
END

IF COL_LENGTH('StaffNotifications', 'RecipientRole') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE StaffNotifications
        SET RecipientRole = ''Staff''
        WHERE RecipientRole IS NULL OR LTRIM(RTRIM(RecipientRole)) = '''';
    ');
END

IF COL_LENGTH('Appointments', 'PreferredAppointmentDate') IS NOT NULL
   AND COL_LENGTH('Appointments', 'PreferredAppointmentTime') IS NOT NULL
   AND COL_LENGTH('Appointments', 'SurgeryCategory') IS NOT NULL
   AND COL_LENGTH('Appointments', 'SurgeryLoadPoints') IS NOT NULL
   AND COL_LENGTH('Appointments', 'IsEmergency') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE a
        SET
            PreferredAppointmentDate = ISNULL(a.PreferredAppointmentDate, a.AppointmentDate),
            PreferredAppointmentTime = ISNULL(a.PreferredAppointmentTime, a.AppointmentTime),
            SurgeryCategory = CASE
                WHEN a.SurgeryCategory IS NULL OR LTRIM(RTRIM(a.SurgeryCategory)) = '''' THEN ''Moderate''
                ELSE a.SurgeryCategory
            END,
            SurgeryLoadPoints = CASE
                WHEN a.IsEmergency = 1 THEN 0
                WHEN a.SurgeryCategory = ''Minor'' THEN 1
                WHEN a.SurgeryCategory = ''Major'' THEN 3
                WHEN a.SurgeryCategory = ''Moderate'' THEN 2
                WHEN a.SurgeryLoadPoints IS NULL OR a.SurgeryLoadPoints = 0 THEN 2
                ELSE a.SurgeryLoadPoints
            END
        FROM Appointments a
        INNER JOIN Services s ON s.Id = a.ServiceId
        WHERE s.ServiceName LIKE ''%Surgery%''
    ');
END
");
}

```
