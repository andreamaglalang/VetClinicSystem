using Microsoft.AspNetCore.Mvc;
using VetClinicSystem.Services.Users;

namespace VetClinicSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _userService.Login(username, password);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("RoleId", user.RoleId);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string email, string password, string firstName, string lastName, string contactNumber, string address)
        {
            var success = _userService.Register(username, email, password, firstName, lastName, contactNumber, address);

            if (!success)
            {
                ViewBag.Error = "Username or email already exists, or Client role is missing.";
                return View();
            }

            TempData["Success"] = "Registration successful. Please login.";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}