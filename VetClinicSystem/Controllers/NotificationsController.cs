using Microsoft.AspNetCore.Mvc;
using VetClinicSystem.Services.Notifications;

namespace VetClinicSystem.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (roleId != 1 && roleId != 2)
                return Unauthorized();

            var notifications = _notificationService.GetAll();
            return View(notifications);
        }

        public IActionResult MarkAsRead(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (roleId != 1 && roleId != 2)
                return Unauthorized();

            _notificationService.MarkAsRead(id);
            TempData["Success"] = "Notification marked as read.";
            return RedirectToAction("Index");
        }
    }
}