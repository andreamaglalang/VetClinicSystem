using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VetClinicSystem.Models;
using VetClinicSystem.Services.Appointments;
using VetClinicSystem.Services.Pets;
using VetClinicSystem.Services.Services;

namespace VetClinicSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IPetService _petService;
        private readonly IServiceManager _serviceManager;

        public AppointmentsController(
            IAppointmentService appointmentService,
            IPetService petService,
            IServiceManager serviceManager)
        {
            _appointmentService = appointmentService;
            _petService = petService;
            _serviceManager = serviceManager;
        }

        public IActionResult Index()
        {
            return View(_appointmentService.GetAll());
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName");
            ViewBag.Services = new SelectList(_serviceManager.GetAll(), "Id", "ServiceName");
            return View();
        }

        [HttpPost]
        public IActionResult Create(Appointment appointment)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", appointment.PetId);
                ViewBag.Services = new SelectList(_serviceManager.GetAll(), "Id", "ServiceName", appointment.ServiceId);
                return View(appointment);
            }

            appointment.CreatedByUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            appointment.LastUpdated = DateTime.Now;

            _appointmentService.Add(appointment);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var appointment = _appointmentService.GetById(id);
            if (appointment == null) return NotFound();

            ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", appointment.PetId);
            ViewBag.Services = new SelectList(_serviceManager.GetAll(), "Id", "ServiceName", appointment.ServiceId);
            return View(appointment);
        }

        [HttpPost]
        public IActionResult Edit(Appointment appointment)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", appointment.PetId);
                ViewBag.Services = new SelectList(_serviceManager.GetAll(), "Id", "ServiceName", appointment.ServiceId);
                return View(appointment);
            }

            appointment.LastUpdated = DateTime.Now;
            _appointmentService.Update(appointment);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var appointment = _appointmentService.GetById(id);
            if (appointment == null) return NotFound();
            return View(appointment);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var appointment = _appointmentService.GetById(id);
            if (appointment == null) return NotFound();
            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _appointmentService.Delete(id);
            return RedirectToAction("Index");
        }
    }
}