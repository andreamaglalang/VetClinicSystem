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

        public IActionResult Index(string? search, int? statusId, DateOnly? appointmentDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Search = search;
            ViewBag.StatusId = statusId;
            ViewBag.AppointmentDate = appointmentDate;

            ViewBag.Statuses = new SelectList(new[]
            {
        new { Id = 0, Name = "All Statuses" },
        new { Id = 1, Name = "Pending" },
        new { Id = 2, Name = "Approved" },
        new { Id = 3, Name = "Rejected" },
        new { Id = 4, Name = "Completed" }
    }, "Id", "Name", statusId ?? 0);

            if (roleId == 3)
                return View(_appointmentService.FilterByUser(userId.Value, search, statusId, appointmentDate));

            return View(_appointmentService.Filter(search, statusId, appointmentDate));
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

            if (roleId == 3)
            {
                var myPets = _petService.GetByUser(userId.Value);
                if (!myPets.Any(p => p.Id == appointment.PetId))
                    return Unauthorized();
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
            if (appointment == null)
                return NotFound();

            if (roleId == 3)
            {
                var myAppointments = _appointmentService.GetByUser(userId.Value);
                if (!myAppointments.Any(a => a.Id == id))
                    return Unauthorized();
            }

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

            if (roleId == 3)
            {
                var myAppointments = _appointmentService.GetByUser(userId.Value);
                if (!myAppointments.Any(a => a.Id == appointment.Id))
                    return Unauthorized();

                var myPets = _petService.GetByUser(userId.Value);
                if (!myPets.Any(p => p.Id == appointment.PetId))
                    return Unauthorized();
            }

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
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var appointment = _appointmentService.GetById(id);
            if (appointment == null)
                return NotFound();

            if (roleId == 3)
            {
                var myAppointments = _appointmentService.GetByUser(userId.Value);
                if (!myAppointments.Any(a => a.Id == id))
                    return Unauthorized();
            }

            return View(appointment);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var appointment = _appointmentService.GetById(id);
            if (appointment == null)
                return NotFound();

            if (roleId == 3)
            {
                var myAppointments = _appointmentService.GetByUser(userId.Value);
                if (!myAppointments.Any(a => a.Id == id))
                    return Unauthorized();
            }

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (roleId == 3)
            {
                var myAppointments = _appointmentService.GetByUser(userId.Value);
                if (!myAppointments.Any(a => a.Id == id))
                    return Unauthorized();
            }

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

        public IActionResult Approve(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (roleId != 1 && roleId != 2)
                return Unauthorized();

            var appointment = _appointmentService.GetById(id);
            if (appointment == null)
                return NotFound();

            if (HasProperty(appointment, "StatusId"))
                SetIntPropertyValue(appointment, "StatusId", 2);

            if (HasProperty(appointment, "AppointmentStatusId"))
                SetIntPropertyValue(appointment, "AppointmentStatusId", 2);

            _appointmentService.Update(appointment);
            TempData["Success"] = "Appointment approved.";
            return RedirectToAction("Index");
        }

        public IActionResult Reject(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (roleId != 1 && roleId != 2)
                return Unauthorized();

            var appointment = _appointmentService.GetById(id);
            if (appointment == null)
                return NotFound();

            if (HasProperty(appointment, "StatusId"))
                SetIntPropertyValue(appointment, "StatusId", 3);

            if (HasProperty(appointment, "AppointmentStatusId"))
                SetIntPropertyValue(appointment, "AppointmentStatusId", 3);

            _appointmentService.Update(appointment);
            TempData["Success"] = "Appointment rejected.";
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

        private bool HasProperty(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName) != null;
        }

        private void SetIntPropertyValue(object obj, string propertyName, int value)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite)
                prop.SetValue(obj, value);
        }
    }
}