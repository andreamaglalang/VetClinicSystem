using Microsoft.AspNetCore.Mvc;
using VetClinicSystem.Models;
using VetClinicSystem.Services.Services;

namespace VetClinicSystem.Controllers
{
    public class ServicesController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public ServicesController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            return View(_serviceManager.GetAll());
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Service service)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            NormalizeService(service);

            if (!ModelState.IsValid)
                return View(service);

            _serviceManager.Add(service);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var service = _serviceManager.GetById(id);
            if (service == null) return NotFound();

            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Service service)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            NormalizeService(service);

            if (!ModelState.IsValid)
                return View(service);

            _serviceManager.Update(service);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var service = _serviceManager.GetById(id);
            if (service == null) return NotFound();

            return View(service);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var service = _serviceManager.GetById(id);
            if (service == null) return NotFound();

            return View(service);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            try
            {
                _serviceManager.Delete(id);
                TempData["Success"] = "Service deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase)
                    ? "This service is already used by existing appointments, so it cannot be deleted."
                    : ex.Message;
            }

            return RedirectToAction("Index");
        }

        private static void NormalizeService(Service service)
        {
            service.ServiceName = service.ServiceName?.Trim() ?? string.Empty;
            service.Description = string.IsNullOrWhiteSpace(service.Description) ? null : service.Description.Trim();
        }
    }
}
