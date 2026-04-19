using Microsoft.AspNetCore.Mvc;
using VetClinicSystem.Services.Appointments;
using VetClinicSystem.Services.Notifications;
using VetClinicSystem.Services.Pets;

namespace VetClinicSystem.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IPetService _petService;
        private readonly IAppointmentService _appointmentService;
        private readonly INotificationService _notificationService;

        public DashboardController(
            IPetService petService,
            IAppointmentService appointmentService,
            INotificationService notificationService)
        {
            _petService = petService;
            _appointmentService = appointmentService;
            _notificationService = notificationService;
        }

        public IActionResult Admin()
        {
            if (HttpContext.Session.GetInt32("UserId") == null || HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("Login", "Account");

            ViewBag.TotalPets = _petService.GetAll().Count;
            ViewBag.TotalAppointments = _appointmentService.GetAll().Count;
            ViewBag.UnreadNotifications = _notificationService.GetUnread().Count;

            return View();
        }

        public IActionResult Staff()
        {
            if (HttpContext.Session.GetInt32("UserId") == null || HttpContext.Session.GetInt32("RoleId") != 2)
                return RedirectToAction("Login", "Account");

            ViewBag.TotalAppointments = _appointmentService.GetAll().Count;
            ViewBag.UnreadNotifications = _notificationService.GetUnread().Count;

            return View();
        }

        public IActionResult Client()
        {
            if (HttpContext.Session.GetInt32("UserId") == null || HttpContext.Session.GetInt32("RoleId") != 3)
                return RedirectToAction("Login", "Account");

            ViewBag.TotalPets = _petService.GetAll().Count;
            ViewBag.TotalAppointments = _appointmentService.GetAll().Count;

            return View();
        }
    }
}