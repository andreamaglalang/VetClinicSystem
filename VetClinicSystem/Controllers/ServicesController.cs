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
            return View(_serviceManager.GetAll());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Service service)
        {
            if (!ModelState.IsValid)
                return View(service);

            _serviceManager.Add(service);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var service = _serviceManager.GetById(id);
            if (service == null) return NotFound();
            return View(service);
        }

        [HttpPost]
        public IActionResult Edit(Service service)
        {
            if (!ModelState.IsValid)
                return View(service);

            _serviceManager.Update(service);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var service = _serviceManager.GetById(id);
            if (service == null) return NotFound();
            return View(service);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var service = _serviceManager.GetById(id);
            if (service == null) return NotFound();
            return View(service);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _serviceManager.Delete(id);
            return RedirectToAction("Index");
        }
    }
}