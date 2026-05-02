using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using VetClinicSystem.Services.Appointments;
using VetClinicSystem.Services.Notifications;
using VetClinicSystem.Services.Pets;
using VetClinicSystem.Services.Users;

namespace VetClinicSystem.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IPetService _petService;
        private readonly IAppointmentService _appointmentService;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        public DashboardController(
            IPetService petService,
            IAppointmentService appointmentService,
            INotificationService notificationService,
            IUserService userService)
        {
            _petService = petService;
            _appointmentService = appointmentService;
            _notificationService = notificationService;
            _userService = userService;
        }

        public IActionResult Admin()
        {
            if (HttpContext.Session.GetInt32("UserId") == null || HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("Login", "Account");

            var appointments = _appointmentService.GetAll();

            ViewBag.TotalPets = _petService.GetAll().Count;
            ViewBag.TotalAppointments = appointments.Count;
            ViewBag.TotalUsers = _userService.GetAll().Count;
            ViewBag.UnreadNotifications = _notificationService.GetUnread().Count;

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
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null || roleId != 3)
                return RedirectToAction("Login", "Account");

            var appointments = _appointmentService.GetByUser(userId.Value);
            var user = _userService.GetById(userId.Value);
            var petOwner = _userService.GetPetOwnerByUserId(userId.Value);

            ViewBag.TotalPets = _petService.GetByUser(userId.Value).Count;
            ViewBag.TotalAppointments = appointments.Count;
            ViewBag.CurrentUser = user;
            ViewBag.PetOwner = petOwner;
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
    }
}
