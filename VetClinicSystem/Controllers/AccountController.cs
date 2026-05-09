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

            ModelState.Remove("PasswordHash");
            ModelState.Remove("Role");
            ModelState.Remove("PetOwner");
            ModelState.Remove("Appointments");
            ModelState.Remove("MedicalRecords");
            ModelState.Remove("VaccinationRecords");

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
