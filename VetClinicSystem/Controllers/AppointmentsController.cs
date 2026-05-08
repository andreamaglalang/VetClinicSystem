using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VetClinicSystem.Helpers;
using VetClinicSystem.Models;
using VetClinicSystem.Services.Appointments;
using VetClinicSystem.Services.Notifications;
using VetClinicSystem.Services.Pets;
using VetClinicSystem.Services.Services;

namespace VetClinicSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        private const int MaxDailySurgeryLoadPoints = 5;
        private static readonly Dictionary<string, int> SurgeryCategoryPoints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Minor"] = 1,
            ["Moderate"] = 2,
            ["Major"] = 3
        };

        private readonly IAppointmentService _appointmentService;
        private readonly INotificationService _notificationService;
        private readonly IPetService _petService;
        private readonly IServiceManager _serviceManager;
        private readonly VetClinicDbContext _context;

        public AppointmentsController(
            IAppointmentService appointmentService,
            INotificationService notificationService,
            IPetService petService,
            IServiceManager serviceManager,
            VetClinicDbContext context)
        {
            _appointmentService = appointmentService;
            _notificationService = notificationService;
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
            LoadSurgeryCategoryOptions();

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

            LoadGuestDropdowns(booking.ServiceId, booking.SurgeryCategory);
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
                LoadGuestDropdowns(booking.ServiceId, booking.SurgeryCategory);
                TempData["Error"] = "Please complete all required guest booking fields.";
                return View(booking);
            }

            if (!booking.AppointmentDate.HasValue || !booking.AppointmentTime.HasValue)
            {
                LoadGuestDropdowns(booking.ServiceId, booking.SurgeryCategory);
                TempData["Error"] = "Please choose an appointment date and time.";
                return View(booking);
            }

            if (!IsAppointmentService(booking.ServiceId))
            {
                LoadGuestDropdowns(booking.ServiceId, booking.SurgeryCategory);
                TempData["Error"] = "Only surgery can be booked by appointment. Other services are walk-in only.";
                return View(booking);
            }

            var appointmentDate = booking.AppointmentDate.Value;
            var unavailableDateReason = GetUnavailableDateReason(appointmentDate);
            if (unavailableDateReason != null)
            {
                LoadGuestDropdowns(booking.ServiceId, booking.SurgeryCategory);
                TempData["Error"] = unavailableDateReason;
                return View(booking);
            }

            ValidateSurgeryRequest(booking.ServiceId, appointmentDate, booking.SurgeryCategory, booking.IsEmergency);
            if (!ModelState.IsValid)
            {
                LoadGuestDropdowns(booking.ServiceId, booking.SurgeryCategory);
                return View(booking);
            }

            try
            {
                booking.ContactNumber = PhoneNumberHelper.Normalize(booking.ContactNumber);
                CreateGuestBooking(booking);
                TempData["Success"] = "Guest surgery booking submitted successfully. Confirmation is recorded and the clinic will contact you for final approval.";
                return RedirectToAction("GuestCreate");
            }
            catch (Exception ex)
            {
                LoadGuestDropdowns(booking.ServiceId, booking.SurgeryCategory);
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
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId, appointment.SurgeryCategory, appointment.StatusId);
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
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId, appointment.SurgeryCategory, appointment.StatusId);
                TempData["Error"] = "Only surgery can be booked by appointment. Other services are walk-in only.";
                return View(appointment);
            }

            var unavailableDateReason = GetUnavailableDateReason(appointment.AppointmentDate);
            if (unavailableDateReason != null)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId, appointment.SurgeryCategory, appointment.StatusId);
                TempData["Error"] = unavailableDateReason;
                return View(appointment);
            }

            ValidateSurgeryRequest(appointment.ServiceId, appointment.AppointmentDate, appointment.SurgeryCategory, appointment.IsEmergency);
            if (!ModelState.IsValid)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId, appointment.SurgeryCategory, appointment.StatusId);
                return View(appointment);
            }

            try
            {
                appointment.CreatedByUserId = userId.Value;
                appointment.LastUpdated = DateTime.Now;
                appointment.IsWalkIn = false;
                appointment.PreferredAppointmentDate = appointment.AppointmentDate;
                appointment.PreferredAppointmentTime = appointment.AppointmentTime;
                appointment.SurgeryCategory = NormalizeSurgeryCategory(appointment.SurgeryCategory);
                appointment.SurgeryLoadPoints = GetSurgeryLoadPoints(appointment.SurgeryCategory);
                appointment.IsScheduleFinalized = roleId != 3;

                _appointmentService.Add(appointment);

                TempData["Success"] = "Appointment created successfully. Confirmation has been recorded.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId, appointment.SurgeryCategory, appointment.StatusId);
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

            LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId, appointment.SurgeryCategory, appointment.StatusId);
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

            var existingAppointment = _appointmentService.GetById(appointment.Id);
            if (existingAppointment == null)
                return NotFound();

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
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId, appointment.SurgeryCategory, appointment.StatusId);
                TempData["Error"] = "Please complete all required appointment fields.";
                return View(appointment);
            }

            if (!IsAppointmentService(appointment.ServiceId))
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId, appointment.SurgeryCategory, appointment.StatusId);
                TempData["Error"] = "Only surgery can be booked by appointment. Other services are walk-in only.";
                return View(appointment);
            }

            var unavailableDateReason = GetUnavailableDateReason(appointment.AppointmentDate);
            if (unavailableDateReason != null)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId, appointment.SurgeryCategory, appointment.StatusId);
                TempData["Error"] = unavailableDateReason;
                return View(appointment);
            }

            ValidateSurgeryRequest(appointment.ServiceId, appointment.AppointmentDate, appointment.SurgeryCategory, appointment.IsEmergency, appointment.Id);
            if (!ModelState.IsValid)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId, appointment.SurgeryCategory, appointment.StatusId);
                return View(appointment);
            }

            try
            {
                appointment.LastUpdated = DateTime.Now;
                appointment.IsWalkIn = false;

                if (roleId == 3)
                {
                    appointment.StatusId = 1;
                    appointment.PreferredAppointmentDate = appointment.AppointmentDate;
                    appointment.PreferredAppointmentTime = appointment.AppointmentTime;
                    appointment.IsScheduleFinalized = false;
                    appointment.StaffNotes = existingAppointment.StaffNotes;
                }
                else
                {
                    appointment.PreferredAppointmentDate = existingAppointment.PreferredAppointmentDate ?? existingAppointment.AppointmentDate;
                    appointment.PreferredAppointmentTime = existingAppointment.PreferredAppointmentTime ?? existingAppointment.AppointmentTime;

                    if (appointment.StatusId == 2 || appointment.StatusId == 4)
                        appointment.IsScheduleFinalized = true;

                    if (appointment.StatusId == 3)
                        appointment.IsScheduleFinalized = false;
                }

                appointment.SurgeryCategory = NormalizeSurgeryCategory(appointment.SurgeryCategory);
                appointment.SurgeryLoadPoints = GetSurgeryLoadPoints(appointment.SurgeryCategory);
                _appointmentService.Update(appointment);

                var updatedAppointment = _appointmentService.GetById(appointment.Id);

                if (roleId == 3)
                {
                    if (updatedAppointment != null)
                        _notificationService.CreateStaffNotification(updatedAppointment, BuildStaffScheduleRequestNotificationMessage(updatedAppointment));
                }
                else
                {
                    var notificationMessage = BuildClientUpdateNotificationMessage(existingAppointment, updatedAppointment);
                    if (updatedAppointment != null && notificationMessage != null)
                        _notificationService.CreateClientNotification(updatedAppointment, notificationMessage);
                }

                TempData["Success"] = "Appointment updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoadDropdowns(userId.Value, roleId, appointment.PetId, appointment.ServiceId, appointment.SurgeryCategory, appointment.StatusId);
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
                var appointmentToDelete = _appointmentService.GetById(id);
                if (appointmentToDelete == null)
                    return NotFound();

                if (roleId == 1 || roleId == 2)
                    _notificationService.CreateClientNotification(appointmentToDelete, BuildClientCancellationNotificationMessage(appointmentToDelete));

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

            appointment.IsScheduleFinalized = true;
            _appointmentService.Update(appointment);
            var approvedAppointment = _appointmentService.GetById(id);
            if (approvedAppointment != null)
                _notificationService.CreateClientNotification(approvedAppointment, BuildClientStatusNotificationMessage(approvedAppointment, "confirmed"));
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

            appointment.IsScheduleFinalized = false;
            _appointmentService.Update(appointment);
            var rejectedAppointment = _appointmentService.GetById(id);
            if (rejectedAppointment != null)
                _notificationService.CreateClientNotification(rejectedAppointment, BuildClientStatusNotificationMessage(rejectedAppointment, "declined"));
            TempData["Success"] = "Appointment rejected.";
            return RedirectToAction("Index");
        }

        public IActionResult Complete(int id)
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

            appointment.StatusId = 4;
            appointment.IsScheduleFinalized = true;

            _appointmentService.Update(appointment);
            var completedAppointment = _appointmentService.GetById(id);
            if (completedAppointment != null)
                _notificationService.CreateClientNotification(completedAppointment, BuildClientStatusNotificationMessage(completedAppointment, "completed"));

            TempData["Success"] = "Appointment marked as completed.";
            return RedirectToAction("Index");
        }

        private void LoadDropdowns(int userId, int? roleId, int? selectedPetId = null, int? selectedServiceId = null, string? selectedSurgeryCategory = null, int? selectedStatusId = null)
        {
            if (roleId == 3)
                ViewBag.Pets = new SelectList(_petService.GetByUser(userId), "Id", "PetName", selectedPetId);
            else
                ViewBag.Pets = new SelectList(_petService.GetAll(), "Id", "PetName", selectedPetId);

            ViewBag.Services = new SelectList(GetAppointmentServices(), "Id", "ServiceName", selectedServiceId);
            LoadSurgeryCategoryOptions(selectedSurgeryCategory);

            if (roleId == 1 || roleId == 2)
            {
                ViewBag.EditStatuses = new SelectList(
                    _context.AppointmentStatuses.OrderBy(x => x.Id).ToList(),
                    "Id",
                    "StatusName",
                    selectedStatusId);
            }
        }

        private void LoadGuestDropdowns(int? selectedServiceId = null, string? selectedSurgeryCategory = null)
        {
            ViewBag.Services = new SelectList(GetAppointmentServices(), "Id", "ServiceName", selectedServiceId);
            ViewBag.SexOptions = new SelectList(new[] { "Male", "Female" });
            LoadSurgeryCategoryOptions(selectedSurgeryCategory);
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
                PreferredAppointmentDate = booking.AppointmentDate.Value,
                PreferredAppointmentTime = booking.AppointmentTime.Value,
                SurgeryCategory = NormalizeSurgeryCategory(booking.SurgeryCategory),
                SurgeryLoadPoints = GetSurgeryLoadPoints(booking.SurgeryCategory),
                IsEmergency = booking.IsEmergency,
                IsScheduleFinalized = false,
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

        private void LoadSurgeryCategoryOptions(string? selectedCategory = null)
        {
            ViewBag.SurgeryCategories = new SelectList(
                SurgeryCategoryPoints.Keys.Select(x => new { Value = x, Text = $"{x} ({SurgeryCategoryPoints[x]} point{(SurgeryCategoryPoints[x] > 1 ? "s" : string.Empty)})" }),
                "Value",
                "Text",
                NormalizeSurgeryCategory(selectedCategory));
        }

        private void ValidateSurgeryRequest(int serviceId, DateOnly appointmentDate, string? surgeryCategory, bool isEmergency, int? currentAppointmentId = null)
        {
            if (!IsAppointmentService(serviceId))
                return;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var maxAllowedDate = today.AddDays(6);
            if (appointmentDate < today)
                ModelState.AddModelError(nameof(Appointment.AppointmentDate), $"Surgery requests cannot be booked in the past. Choose a date from {today:MMMM d, yyyy} to {maxAllowedDate:MMMM d, yyyy}.");

            if (appointmentDate > maxAllowedDate)
                ModelState.AddModelError(nameof(Appointment.AppointmentDate), $"Surgery requests must be scheduled between {today:MMMM d, yyyy} and {maxAllowedDate:MMMM d, yyyy}.");

            var normalizedCategory = NormalizeSurgeryCategory(surgeryCategory);
            if (normalizedCategory == null)
            {
                ModelState.AddModelError(nameof(Appointment.SurgeryCategory), "Please select a surgery category.");
                return;
            }

            if (isEmergency)
                return;

            var selectedLoadPoints = GetSurgeryLoadPoints(normalizedCategory);
            var currentLoadPoints = _context.Appointments
                .Where(x =>
                    x.Id == currentAppointmentId &&
                    x.Service.ServiceName.Contains("Surgery"))
                .Select(x => x.SurgeryLoadPoints)
                .FirstOrDefault();

            var scheduledLoadPoints = _context.Appointments
                .Where(x =>
                    x.AppointmentDate == appointmentDate &&
                    x.Id != currentAppointmentId &&
                    x.Service.ServiceName.Contains("Surgery") &&
                    x.StatusId != 3 &&
                    !x.IsEmergency)
                .Sum(x => x.SurgeryLoadPoints > 0 ? x.SurgeryLoadPoints : 0);

            var totalLoadPoints = scheduledLoadPoints + selectedLoadPoints;

            if (currentAppointmentId.HasValue && currentLoadPoints > 0 && appointmentDate == _context.Appointments.Where(x => x.Id == currentAppointmentId.Value).Select(x => x.AppointmentDate).FirstOrDefault())
                totalLoadPoints = scheduledLoadPoints + selectedLoadPoints;

            if (totalLoadPoints > MaxDailySurgeryLoadPoints)
            {
                ModelState.AddModelError(
                    nameof(Appointment.AppointmentDate),
                    $"This surgery request would bring the day to {totalLoadPoints} load points. The clinic only allows {MaxDailySurgeryLoadPoints} surgery points per day for non-emergency cases.");
            }
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

        private string? BuildClientUpdateNotificationMessage(Appointment? previousAppointment, Appointment? updatedAppointment)
        {
            if (previousAppointment == null || updatedAppointment == null)
                return null;

            if (!previousAppointment.IsEmergency && updatedAppointment.IsEmergency)
                return BuildClientStatusNotificationMessage(updatedAppointment, "emergency");

            if (previousAppointment.AppointmentDate != updatedAppointment.AppointmentDate ||
                previousAppointment.AppointmentTime != updatedAppointment.AppointmentTime)
            {
                return BuildClientRescheduleNotificationMessage(previousAppointment, updatedAppointment);
            }

            if (previousAppointment.StatusId != updatedAppointment.StatusId)
            {
                return updatedAppointment.StatusId switch
                {
                    2 => BuildClientStatusNotificationMessage(updatedAppointment, "confirmed"),
                    3 => BuildClientStatusNotificationMessage(updatedAppointment, "declined"),
                    4 => BuildClientStatusNotificationMessage(updatedAppointment, "completed"),
                    _ => null
                };
            }

            return null;
        }

        private string BuildClientCancellationNotificationMessage(Appointment appointment)
        {
            return BuildClientStatusNotificationMessage(appointment, "cancelled");
        }

        private string BuildStaffScheduleRequestNotificationMessage(Appointment appointment)
        {
            var petName = string.IsNullOrWhiteSpace(appointment.Pet?.PetName) ? "the pet" : appointment.Pet.PetName.Trim();
            var ownerName = appointment.Pet?.Owner == null
                ? "A client"
                : $"{appointment.Pet.Owner.FirstName} {appointment.Pet.Owner.LastName}".Trim();
            var category = NormalizeSurgeryCategory(appointment.SurgeryCategory) ?? "Moderate";
            var emergencyText = appointment.IsEmergency ? " Emergency priority requested." : string.Empty;

            return $"{ownerName} updated the surgery request for {petName}. Preferred schedule: {FormatSchedule(appointment)}. Category: {category}.{emergencyText}";
        }

        private string BuildClientRescheduleNotificationMessage(Appointment previousAppointment, Appointment updatedAppointment)
        {
            var petName = string.IsNullOrWhiteSpace(updatedAppointment.Pet?.PetName) ? "your pet" : updatedAppointment.Pet.PetName.Trim();
            return $"Your surgery appointment for {petName} has been rescheduled from {FormatSchedule(previousAppointment)} to {FormatSchedule(updatedAppointment)}.";
        }

        private string BuildClientStatusNotificationMessage(Appointment appointment, string state)
        {
            var petName = string.IsNullOrWhiteSpace(appointment.Pet?.PetName) ? "your pet" : appointment.Pet.PetName.Trim();
            var schedule = FormatSchedule(appointment);
            var category = NormalizeSurgeryCategory(appointment.SurgeryCategory) ?? "Surgery";
            var emergencyPrefix = appointment.IsEmergency ? "emergency " : string.Empty;

            return state switch
            {
                "pending" => $"Your {emergencyPrefix}{category.ToLowerInvariant()} surgery request for {petName} on {schedule} has been submitted and is pending clinic approval.",
                "confirmed" when appointment.IsEmergency => $"Your emergency surgery appointment for {petName} has been prioritized and confirmed for {schedule}.",
                "confirmed" => $"Your surgery appointment for {petName} on {schedule} has been confirmed.",
                "declined" => $"Your surgery appointment for {petName} on {schedule} has been declined.",
                "cancelled" => $"Your surgery appointment for {petName} on {schedule} has been cancelled.",
                "rescheduled" => $"Your surgery appointment for {petName} on {schedule} has been rescheduled.",
                "completed" => $"Your surgery appointment for {petName} on {schedule} has been completed.",
                "emergency" => $"Your surgery appointment for {petName} on {schedule} has been tagged as an emergency priority.",
                _ => $"Your surgery appointment for {petName} on {schedule} has been updated."
            };
        }

        private string? NormalizeSurgeryCategory(string? surgeryCategory)
        {
            if (string.IsNullOrWhiteSpace(surgeryCategory))
                return null;

            return SurgeryCategoryPoints.Keys.FirstOrDefault(x => string.Equals(x, surgeryCategory.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private int GetSurgeryLoadPoints(string? surgeryCategory)
        {
            var normalizedCategory = NormalizeSurgeryCategory(surgeryCategory);
            if (normalizedCategory == null)
                return 0;

            return SurgeryCategoryPoints[normalizedCategory];
        }

        private string FormatSchedule(Appointment appointment)
        {
            var scheduleDateTime = appointment.AppointmentDate.ToDateTime(appointment.AppointmentTime);
            return scheduleDateTime.ToString("MMMM d, yyyy h:mm tt");
        }

        private void SetIntPropertyValue(object obj, string propertyName, int value)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite)
                prop.SetValue(obj, value);
        }
    }
}
