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
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (roleId == 3)
                return View(_appointmentService.GetByUser(userId.Value));

            return View(_appointmentService.GetAll());
        }

        [HttpGet]
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (roleId == 3)
            {
                var pets = _petService.GetByUser(userId.Value);

                if (!pets.Any())
                {
                    TempData["Error"] = "You need to add a pet first before creating an appointment.";
                    return RedirectToAction("Index", "Pets");
                }

                ViewBag.Pets = new SelectList(pets, "Id", "PetName");
            }
            else
            {
                ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName");
            }

            ViewBag.Services = new SelectList(_serviceManager.GetAll(), "Id", "ServiceName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Appointment appointment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Pet");
            ModelState.Remove("Service");
            ModelState.Remove("CreatedByUser");
            ModelState.Remove("AppointmentStatus");
            ModelState.Remove("Status");
            ModelState.Remove("CreatedByUserId");
            ModelState.Remove("LastUpdated");

            if (!ModelState.IsValid)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId);
                TempData["Error"] = "Please complete all required appointment fields.";
                return View(appointment);
            }

            try
            {
                appointment.CreatedByUserId = userId.Value;
                appointment.LastUpdated = DateTime.Now;

                _appointmentService.Add(appointment);

                TempData["Success"] = "Appointment created successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId);
                TempData["Error"] = ex.Message;
                return View(appointment);
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var appointment = _appointmentService.GetById(id);
            if (appointment == null) return NotFound();

            LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId);

            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Appointment appointment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Pet");
            ModelState.Remove("Service");
            ModelState.Remove("CreatedByUser");
            ModelState.Remove("AppointmentStatus");
            ModelState.Remove("Status");
            ModelState.Remove("CreatedByUserId");
            ModelState.Remove("LastUpdated");

            if (!ModelState.IsValid)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId);
                TempData["Error"] = "Please complete all required appointment fields.";
                return View(appointment);
            }

            try
            {
                appointment.LastUpdated = DateTime.Now;
                _appointmentService.Update(appointment);

                TempData["Success"] = "Appointment updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId);
                TempData["Error"] = ex.Message;
                return View(appointment);
            }
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var appointment = _appointmentService.GetById(id);
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var appointment = _appointmentService.GetById(id);
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            try
            {
                _appointmentService.Delete(id);
                TempData["Success"] = "Appointment deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        private void LoadDropdowns(int userId, int? roleId, int? selectedPetId = null, int? selectedServiceId = null)
        {
            if (roleId == 3)
                ViewBag.Pets = new SelectList(_petService.GetByUser(userId), "Id", "PetName", selectedPetId);
            else
                ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", selectedPetId);

            ViewBag.Services = new SelectList(_serviceManager.GetAll(), "Id", "ServiceName", selectedServiceId);
        }
    }
}