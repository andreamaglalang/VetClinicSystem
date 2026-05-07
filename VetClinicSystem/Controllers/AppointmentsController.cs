using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VetClinicSystem.Helpers;
using VetClinicSystem.Models;
using VetClinicSystem.Services.Appointments;
using VetClinicSystem.Services.Pets;
using VetClinicSystem.Services.Services;

namespace VetClinicSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        private const int MaxSurgeryAppointmentsPerDay = 2;

        private readonly IAppointmentService _appointmentService;
        private readonly IPetService _petService;
        private readonly IServiceManager _serviceManager;
        private readonly VetClinicDbContext _context;

        public AppointmentsController(
            IAppointmentService appointmentService,
            IPetService petService,
            IServiceManager serviceManager,
            VetClinicDbContext context)
        {
            _appointmentService = appointmentService;
            _petService = petService;
            _serviceManager = serviceManager;
            _context = context;
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

            ViewBag.Services = new SelectList(GetAppointmentServices(), "Id", "ServiceName");

            return View();
        }

        [HttpGet]
        public IActionResult GuestCreate(string? ownerName, string? contactNumber, string? serviceName, string? clientNotes)
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Create");

            var booking = new GuestAppointmentBooking
            {
                ContactNumber = contactNumber?.Trim() ?? string.Empty,
                ClientNotes = clientNotes?.Trim()
            };

            if (!string.IsNullOrWhiteSpace(ownerName))
            {
                var nameParts = ownerName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                booking.FirstName = nameParts[0];
                booking.LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(serviceName))
            {
                var requestedServiceName = serviceName.Trim();
                var service = GetAppointmentServices()
                    .FirstOrDefault(x =>
                        x.ServiceName.Equals(requestedServiceName, StringComparison.OrdinalIgnoreCase) ||
                        x.ServiceName.Contains(requestedServiceName, StringComparison.OrdinalIgnoreCase) ||
                        requestedServiceName.Contains(x.ServiceName, StringComparison.OrdinalIgnoreCase));

                if (service != null)
                    booking.ServiceId = service.Id;
            }

            LoadGuestDropdowns(booking.ServiceId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuestCreate(GuestAppointmentBooking booking)
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Create");

            if (!ModelState.IsValid)
            {
                LoadGuestDropdowns(booking.ServiceId);
                TempData["Error"] = "Please complete all required guest booking fields.";
                return View(booking);
            }

            if (!booking.AppointmentDate.HasValue || !booking.AppointmentTime.HasValue)
            {
                LoadGuestDropdowns(booking.ServiceId);
                TempData["Error"] = "Please choose an appointment date and time.";
                return View(booking);
            }

            if (!IsAppointmentService(booking.ServiceId))
            {
                LoadGuestDropdowns(booking.ServiceId);
                TempData["Error"] = "Only surgery can be booked by appointment. Other services are walk-in only.";
                return View(booking);
            }

            var appointmentDate = booking.AppointmentDate.Value;
            var unavailableDateReason = GetUnavailableDateReason(appointmentDate);
            if (unavailableDateReason != null)
            {
                LoadGuestDropdowns(booking.ServiceId);
                TempData["Error"] = unavailableDateReason;
                return View(booking);
            }

            var surgeryRuleError = GetSurgeryBookingRuleError(booking.ServiceId, appointmentDate);
            if (surgeryRuleError != null)
            {
                LoadGuestDropdowns(booking.ServiceId);
                TempData["Error"] = surgeryRuleError;
                return View(booking);
            }

            try
            {
                CreateGuestBooking(booking);
                TempData["Success"] = "Guest surgery booking submitted successfully. Confirmation is recorded and the clinic will contact you for final approval.";
                return RedirectToAction("GuestCreate");
            }
            catch (Exception ex)
            {
                LoadGuestDropdowns(booking.ServiceId);
                TempData["Error"] = ex.Message;
                return View(booking);
            }
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

            if (!IsAppointmentService(appointment.ServiceId))
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId);
                TempData["Error"] = "Only surgery can be booked by appointment. Other services are walk-in only.";
                return View(appointment);
            }

            var unavailableDateReason = GetUnavailableDateReason(appointment.AppointmentDate);
            if (unavailableDateReason != null)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId);
                TempData["Error"] = unavailableDateReason;
                return View(appointment);
            }

            var surgeryRuleError = GetSurgeryBookingRuleError(appointment.ServiceId, appointment.AppointmentDate);
            if (surgeryRuleError != null)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId);
                TempData["Error"] = surgeryRuleError;
                return View(appointment);
            }

            try
            {
                appointment.CreatedByUserId = userId.Value;
                appointment.LastUpdated = DateTime.Now;
                appointment.IsWalkIn = false;

                _appointmentService.Add(appointment);

                TempData["Success"] = "Appointment created successfully. Confirmation has been recorded.";
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

                if (!CanClientChangeAppointment(appointment))
                {
                    TempData["Error"] = "Appointments can only be rescheduled at least 1 day before the scheduled visit.";
                    return RedirectToAction("Index");
                }
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
                var currentAppointment = myAppointments.FirstOrDefault(a => a.Id == appointment.Id);
                if (currentAppointment == null)
                    return Unauthorized();

                if (!CanClientChangeAppointment(currentAppointment))
                {
                    TempData["Error"] = "Appointments can only be rescheduled at least 1 day before the scheduled visit.";
                    return RedirectToAction("Index");
                }

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

            if (!IsAppointmentService(appointment.ServiceId))
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId);
                TempData["Error"] = "Only surgery can be booked by appointment. Other services are walk-in only.";
                return View(appointment);
            }

            var unavailableDateReason = GetUnavailableDateReason(appointment.AppointmentDate);
            if (unavailableDateReason != null)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId);
                TempData["Error"] = unavailableDateReason;
                return View(appointment);
            }

            var surgeryRuleError = GetSurgeryBookingRuleError(appointment.ServiceId, appointment.AppointmentDate, appointment.Id);
            if (surgeryRuleError != null)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId);
                TempData["Error"] = surgeryRuleError;
                return View(appointment);
            }

            try
            {
                appointment.LastUpdated = DateTime.Now;
                appointment.IsWalkIn = false;
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
                var clientAppointment = myAppointments.FirstOrDefault(a => a.Id == id);
                if (clientAppointment == null)
                    return Unauthorized();

                if (!CanClientChangeAppointment(clientAppointment))
                {
                    TempData["Error"] = "Appointments can only be cancelled at least 1 day before the scheduled visit.";
                    return RedirectToAction("Index");
                }
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
                var appointment = myAppointments.FirstOrDefault(a => a.Id == id);
                if (appointment == null)
                    return Unauthorized();

                if (!CanClientChangeAppointment(appointment))
                {
                    TempData["Error"] = "Appointments can only be cancelled at least 1 day before the scheduled visit.";
                    return RedirectToAction("Index");
                }
            }

            try
            {
                _appointmentService.Delete(id);
                TempData["Success"] = "Appointment cancelled successfully.";
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

            ViewBag.Services = new SelectList(GetAppointmentServices(), "Id", "ServiceName", selectedServiceId);
        }

        private void LoadGuestDropdowns(int? selectedServiceId = null)
        {
            ViewBag.Services = new SelectList(GetAppointmentServices(), "Id", "ServiceName", selectedServiceId);
            ViewBag.SexOptions = new SelectList(new[] { "Male", "Female" });
        }

        private void CreateGuestBooking(GuestAppointmentBooking booking)
        {
            if (!booking.AppointmentDate.HasValue || !booking.AppointmentTime.HasValue)
                throw new Exception("Please choose an appointment date and time.");

            var email = booking.Email.Trim();
            var existingUser = _context.Users.FirstOrDefault(x => x.Email == email);

            if (existingUser != null && !existingUser.IsGuest)
                throw new Exception("This email is already registered. Please log in to book surgery with this account.");

            using var transaction = _context.Database.BeginTransaction();

            var clientRole = _context.Roles.FirstOrDefault(x => x.RoleName == "Client");
            if (clientRole == null)
                throw new Exception("Client role is missing. Please contact the clinic.");

            var guestUser = existingUser ?? new User
            {
                Username = BuildGuestUsername(),
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString("N")),
                RoleId = clientRole.Id,
                IsActive = true,
                IsGuest = true,
                DateCreated = DateTime.Now
            };

            if (existingUser == null)
            {
                _context.Users.Add(guestUser);
                _context.SaveChanges();
            }

            var petOwner = _context.PetOwners.FirstOrDefault(x => x.UserId == guestUser.Id);
            if (petOwner == null)
            {
                petOwner = new PetOwner
                {
                    UserId = guestUser.Id,
                    FirstName = booking.FirstName.Trim(),
                    LastName = booking.LastName.Trim(),
                    ContactNumber = booking.ContactNumber.Trim(),
                    Address = booking.Address,
                    DateCreated = DateTime.Now
                };

                _context.PetOwners.Add(petOwner);
                _context.SaveChanges();
            }
            else
            {
                petOwner.FirstName = booking.FirstName.Trim();
                petOwner.LastName = booking.LastName.Trim();
                petOwner.ContactNumber = booking.ContactNumber.Trim();
                petOwner.Address = booking.Address;
                _context.SaveChanges();
            }

            var pet = new Pet
            {
                OwnerId = petOwner.Id,
                PetName = booking.PetName.Trim(),
                Species = booking.Species.Trim(),
                Breed = booking.Breed,
                Sex = booking.Sex,
                Age = booking.Age,
                Notes = booking.PetNotes,
                DateCreated = DateTime.Now
            };

            _context.Pets.Add(pet);
            _context.SaveChanges();

            var appointment = new Appointment
            {
                PetId = pet.Id,
                ServiceId = booking.ServiceId,
                AppointmentDate = booking.AppointmentDate.Value,
                AppointmentTime = booking.AppointmentTime.Value,
                ReasonForVisit = booking.ReasonForVisit,
                ClientNotes = booking.ClientNotes,
                CreatedByUserId = guestUser.Id,
                IsWalkIn = false,
                IsGuestBooking = true,
                LastUpdated = DateTime.Now
            };

            _appointmentService.Add(appointment);
            transaction.Commit();
        }

        private string BuildGuestUsername()
        {
            string username;

            do
            {
                username = $"guest_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..32];
            }
            while (_context.Users.Any(x => x.Username == username));

            return username;
        }

        private List<Service> GetAppointmentServices()
        {
            return _serviceManager.GetAll()
                .Where(IsAppointmentService)
                .ToList();
        }

        private bool IsAppointmentService(int serviceId)
        {
            var service = _serviceManager.GetById(serviceId);
            return service != null && IsAppointmentService(service);
        }

        private bool IsAppointmentService(Service service)
        {
            return service.ServiceName.Contains("Surgery", StringComparison.OrdinalIgnoreCase);
        }

        private string? GetSurgeryBookingRuleError(int serviceId, DateOnly appointmentDate, int? currentAppointmentId = null)
        {
            if (!IsAppointmentService(serviceId))
                return null;

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (appointmentDate < today)
                return "Surgery appointments cannot be booked in the past.";

            if (appointmentDate > today.AddDays(6))
                return "Surgery appointments must be scheduled within the same week.";

            var surgeryAppointmentsOnDate = _context.Appointments
                .Count(x =>
                    x.AppointmentDate == appointmentDate &&
                    x.Id != currentAppointmentId &&
                    x.Service.ServiceName.Contains("Surgery") &&
                    x.StatusId != 3);

            if (surgeryAppointmentsOnDate >= MaxSurgeryAppointmentsPerDay)
                return $"Only {MaxSurgeryAppointmentsPerDay} surgery appointments can be scheduled per day. Please choose another date.";

            return null;
        }

        private bool CanClientChangeAppointment(Appointment appointment)
        {
            var appointmentDateTime = appointment.AppointmentDate.ToDateTime(appointment.AppointmentTime);
            return appointmentDateTime >= DateTime.Now.AddDays(1);
        }

        private string? GetUnavailableDateReason(DateOnly appointmentDate)
        {
            if (appointmentDate.DayOfWeek == DayOfWeek.Tuesday)
                return "The clinic is closed every Tuesday. Please choose another appointment date.";

            var holidayName = GetClinicHolidayName(appointmentDate);
            if (holidayName != null)
                return $"The clinic is unavailable on {holidayName}. Please choose another appointment date.";

            return null;
        }

        private string? GetClinicHolidayName(DateOnly date)
        {
            if (date.Month == 1 && date.Day == 1)
                return "New Year's Day";

            if (date.Month == 11 && date.Day == 1)
                return "All Saints' Day";

            if (date.Month == 12 && date.Day == 24)
                return "Christmas Eve";

            if (date.Month == 12 && date.Day == 25)
                return "Christmas Day";

            if (date.Month == 12 && date.Day == 31)
                return "New Year's Eve";

            var easterSunday = GetEasterSunday(date.Year);
            if (date == easterSunday.AddDays(-3))
                return "Maundy Thursday";

            if (date == easterSunday.AddDays(-2))
                return "Good Friday";

            if (date == easterSunday.AddDays(-1))
                return "Black Saturday";

            return null;
        }

        private DateOnly GetEasterSunday(int year)
        {
            var a = year % 19;
            var b = year / 100;
            var c = year % 100;
            var d = b / 4;
            var e = b % 4;
            var f = (b + 8) / 25;
            var g = (b - f + 1) / 3;
            var h = (19 * a + b - d - g + 15) % 30;
            var i = c / 4;
            var k = c % 4;
            var l = (32 + 2 * e + 2 * i - h - k) % 7;
            var m = (a + 11 * h + 22 * l) / 451;
            var month = (h + l - 7 * m + 114) / 31;
            var day = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateOnly(year, month, day);
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
