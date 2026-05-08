using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
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

            var notifications = _notificationService.GetAllForStaff();
            return View(notifications);
        }

        public IActionResult Client()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (roleId != 3)
                return Unauthorized();

            var notifications = _notificationService.GetAllForUser(userId.Value);
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

            if (!_notificationService.MarkAsReadForStaff(id))
                return NotFound();

            if (IsAjaxRequest())
                return Json(new { success = true });

            TempData["Success"] = "Notification marked as read.";
            return RedirectToAction("Index");
        }

        public IActionResult MarkAsReadClient(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (roleId != 3)
                return Unauthorized();

            if (!_notificationService.MarkAsReadForUser(id, userId.Value))
                return NotFound();

            if (IsAjaxRequest())
                return Json(new { success = true });

            TempData["Success"] = "Notification marked as read.";
            return RedirectToAction("Client");
        }

        private bool IsAjaxRequest()
        {
            if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) &&
                string.Equals(requestedWith.ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                return true;

            if (Request.Headers.TryGetValue(HeaderNames.Accept, out var acceptHeader) &&
                acceptHeader.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
