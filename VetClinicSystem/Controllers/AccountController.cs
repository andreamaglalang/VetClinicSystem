using Microsoft.AspNetCore.Mvc;
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
            model.Username = InputValidationHelper.NormalizeTrimmed(model.Username);

            if (!ModelState.IsValid)
                return View(model);

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
            model.Username = InputValidationHelper.NormalizeTrimmed(model.Username);
            model.Email = InputValidationHelper.NormalizeEmail(model.Email);
            model.FirstName = InputValidationHelper.NormalizeTrimmed(model.FirstName);
            model.LastName = InputValidationHelper.NormalizeTrimmed(model.LastName);
            model.ContactNumber = model.ContactNumber?.Trim() ?? string.Empty;
            model.Address = InputValidationHelper.NormalizeTrimmed(model.Address);

            ValidateRegisterInput(model);

            if (!ModelState.IsValid)
                return View(model);

            var normalizedUsername = model.Username.ToLowerInvariant();
            var normalizedEmail = model.Email.ToLowerInvariant();

            if (_context.Users.Any(x => x.Username.ToLower() == normalizedUsername))
                ModelState.AddModelError(nameof(RegisterViewModel.Username), "Username is already taken.");

            if (_context.Users.Any(x => x.Email.ToLower() == normalizedEmail))
                ModelState.AddModelError(nameof(RegisterViewModel.Email), "Email is already registered.");

            if (!ModelState.IsValid)
                return View(model);

            var success = _userService.Register(model.Username, model.Email, model.Password, model.FirstName, model.LastName, model.ContactNumber, model.Address);

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
            var firstName = InputValidationHelper.NormalizeTrimmed(Request.Form["FirstName"].ToString());
            var lastName = InputValidationHelper.NormalizeTrimmed(Request.Form["LastName"].ToString());
            var contactNumber = Request.Form["ContactNumber"].ToString().Trim();
            var normalizedUsername = InputValidationHelper.NormalizeTrimmed(model.Username);
            var normalizedEmail = InputValidationHelper.NormalizeEmail(model.Email);
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
                if (!InputValidationHelper.IsValidPersonName(firstName))
                {
                    ViewBag.FirstNameError = "Name must contain letters only and cannot include numbers or symbols.";
                    hasValidationError = true;
                }

                if (!InputValidationHelper.IsValidPersonName(lastName))
                {
                    ViewBag.LastNameError = "Name must contain letters only and cannot include numbers or symbols.";
                    hasValidationError = true;
                }

                if (!InputValidationHelper.IsValidPhilippineMobile(contactNumber))
                {
                    ViewBag.ContactNumberError = InputValidationHelper.ContactNumberMessage;
                    hasValidationError = true;
                }
            }

            if (!InputValidationHelper.IsValidUsername(normalizedUsername))
            {
                ModelState.AddModelError("Username", InputValidationHelper.UsernameMessage);
                hasValidationError = true;
            }

            if (!string.IsNullOrWhiteSpace(normalizedUsername))
            {
                var usernameExists = _context.Users
                    .Any(u => u.Username.ToLower() == normalizedUsername.ToLower() && u.Id != user.Id);

                if (usernameExists)
                {
                    ModelState.AddModelError("Username", "Username is already taken. Please choose another username.");
                    hasValidationError = true;
                }
            }

            if (!InputValidationHelper.IsValidGmail(normalizedEmail))
            {
                ModelState.AddModelError("Email", InputValidationHelper.GmailMessage);
                hasValidationError = true;
            }
            else
            {
                var emailExists = _context.Users
                    .Any(u => u.Email.ToLower() == normalizedEmail.ToLower() && u.Id != user.Id);

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
                else if (!InputValidationHelper.IsValidPassword(newPassword))
                {
                    ViewBag.NewPasswordError = InputValidationHelper.PasswordMessage;
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

        private void ValidateRegisterInput(RegisterViewModel model)
        {
            if (!InputValidationHelper.IsValidUsername(model.Username))
            {
                AddModelErrorIfMissing(nameof(RegisterViewModel.Username), InputValidationHelper.UsernameMessage);
            }

            if (!InputValidationHelper.IsValidGmail(model.Email))
            {
                AddModelErrorIfMissing(nameof(RegisterViewModel.Email), InputValidationHelper.GmailMessage);
            }

            if (!InputValidationHelper.IsValidPersonName(model.FirstName))
            {
                AddModelErrorIfMissing(nameof(RegisterViewModel.FirstName), InputValidationHelper.PersonNameMessage);
            }

            if (!InputValidationHelper.IsValidPersonName(model.LastName))
            {
                AddModelErrorIfMissing(nameof(RegisterViewModel.LastName), InputValidationHelper.PersonNameMessage);
            }

            if (!InputValidationHelper.IsValidPhilippineMobile(model.ContactNumber))
            {
                AddModelErrorIfMissing(nameof(RegisterViewModel.ContactNumber), InputValidationHelper.ContactNumberMessage);
            }

            if (!InputValidationHelper.IsValidAddress(model.Address))
            {
                AddModelErrorIfMissing(nameof(RegisterViewModel.Address), InputValidationHelper.AddressMessage);
            }

            if (!InputValidationHelper.IsValidPassword(model.Password))
            {
                AddModelErrorIfMissing(nameof(RegisterViewModel.Password), InputValidationHelper.PasswordMessage);
            }

            if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
            {
                AddModelErrorIfMissing(nameof(RegisterViewModel.ConfirmPassword), "Confirm password must match.");
            }
        }

        private void AddModelErrorIfMissing(string key, string errorMessage)
        {
            if (!ModelState.TryGetValue(key, out var entry) || entry.Errors.Count == 0)
            {
                ModelState.AddModelError(key, errorMessage);
            }
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
