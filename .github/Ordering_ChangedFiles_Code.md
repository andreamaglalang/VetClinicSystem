# Ordering and Sorting Changes

This file contains the complete updated code for every file changed while improving ordering and sorting behavior across the VetClinicSystem project.

## Changed Files
- `Repositories/Appointments/AppointmentRepository.cs`
- `Repositories/Pets/PetRepository.cs`
- `Repositories/Users/UserRepository.cs`
- `Repositories/MedicalRecords/MedicalRecordRepository.cs`
- `Repositories/Vaccinations/VaccinationRepository.cs`
- `Views/Appointments/Index.cshtml`

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Repositories\Appointments\AppointmentRepository.cs

`$lang
using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Appointments
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly VetClinicDbContext _context;

        public AppointmentRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public List<Appointment> GetAll()
        {
            var appointments = _context.Appointments
                .Include(x => x.Pet)
                .Include(x => x.Service)
                .Include(x => x.Status)
                .ToList();

            return OrderForManagement(appointments);
        }

        public List<Appointment> GetByOwnerId(int ownerId)
        {
            var appointments = _context.Appointments
                .Include(x => x.Pet)
                .Include(x => x.Service)
                .Include(x => x.Status)
                .Where(x => x.Pet.OwnerId == ownerId)
                .ToList();

            return OrderForClient(appointments);
        }

        public List<Appointment> Search(string? search)
        {
            return Filter(search, null, null);
        }

        public List<Appointment> SearchByOwnerId(int ownerId, string? search)
        {
            return FilterByOwnerId(ownerId, search, null, null);
        }

        public List<Appointment> Filter(string? search, int? statusId, DateOnly? appointmentDate)
        {
            var query = _context.Appointments
                .Include(x => x.Pet)
                .Include(x => x.Service)
                .Include(x => x.Status)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    (x.Pet != null && x.Pet.PetName.Contains(search)) ||
                    (x.Service != null && x.Service.ServiceName.Contains(search)) ||
                    (x.ReasonForVisit != null && x.ReasonForVisit.Contains(search)));
            }

            if (statusId.HasValue && statusId.Value > 0)
            {
                query = query.Where(x => x.StatusId == statusId.Value);
            }

            if (appointmentDate.HasValue)
            {
                query = query.Where(x => x.AppointmentDate == appointmentDate.Value);
            }

            return OrderForManagement(query.ToList());
        }

        public List<Appointment> FilterByOwnerId(int ownerId, string? search, int? statusId, DateOnly? appointmentDate)
        {
            var query = _context.Appointments
                .Include(x => x.Pet)
                .Include(x => x.Service)
                .Include(x => x.Status)
                .Where(x => x.Pet.OwnerId == ownerId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    (x.Pet != null && x.Pet.PetName.Contains(search)) ||
                    (x.Service != null && x.Service.ServiceName.Contains(search)) ||
                    (x.ReasonForVisit != null && x.ReasonForVisit.Contains(search)));
            }

            if (statusId.HasValue && statusId.Value > 0)
            {
                query = query.Where(x => x.StatusId == statusId.Value);
            }

            if (appointmentDate.HasValue)
            {
                query = query.Where(x => x.AppointmentDate == appointmentDate.Value);
            }

            return OrderForClient(query.ToList());
        }

        public Appointment? GetById(int id)
        {
            return _context.Appointments
                .Include(x => x.Pet)
                    .ThenInclude(x => x.Owner)
                .Include(x => x.Service)
                .Include(x => x.Status)
                .Include(x => x.CreatedByUser)
                .FirstOrDefault(x => x.Id == id);
        }

        public void Add(Appointment appointment)
        {
            _context.Appointments.Add(appointment);
        }

        public void Update(Appointment appointment)
        {
            _context.Appointments.Update(appointment);
        }

        public void Delete(Appointment appointment)
        {
            var notifications = _context.StaffNotifications
                .Where(x => x.AppointmentId == appointment.Id)
                .ToList();

            if (notifications.Any())
            {
                foreach (var notification in notifications)
                    notification.AppointmentId = null;
            }

            var reminderLogs = _context.ReminderLogs
                .Where(x => x.AppointmentId == appointment.Id)
                .ToList();

            if (reminderLogs.Any())
                _context.ReminderLogs.RemoveRange(reminderLogs);

            var medicalRecords = _context.MedicalRecords
                .Where(x => x.AppointmentId == appointment.Id)
                .ToList();

            foreach (var medicalRecord in medicalRecords)
                medicalRecord.AppointmentId = null;

            _context.Appointments.Remove(appointment);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        private static List<Appointment> OrderForManagement(IEnumerable<Appointment> appointments)
        {
            var now = DateTime.Now;

            return appointments
                .OrderBy(x => x.StatusId == 1 ? 0 : IsUpcomingOperational(x, now) ? 1 : 2)
                .ThenBy(x => (x.StatusId == 1 || IsUpcomingOperational(x, now)) && x.IsEmergency ? 0 : 1)
                .ThenBy(x => x.StatusId == 1 || IsUpcomingOperational(x, now) ? GetAppointmentDateTime(x) : DateTime.MaxValue)
                .ThenByDescending(x => x.StatusId == 1 || IsUpcomingOperational(x, now) ? DateTime.MinValue : GetAppointmentDateTime(x))
                .ThenByDescending(x => x.LastUpdated)
                .ToList();
        }

        private static List<Appointment> OrderForClient(IEnumerable<Appointment> appointments)
        {
            var now = DateTime.Now;

            return appointments
                .OrderBy(x => IsUpcomingForClient(x, now) ? 0 : 1)
                .ThenBy(x => IsUpcomingForClient(x, now) ? GetAppointmentDateTime(x) : DateTime.MaxValue)
                .ThenByDescending(x => IsUpcomingForClient(x, now) ? DateTime.MinValue : GetAppointmentDateTime(x))
                .ThenByDescending(x => x.LastUpdated)
                .ToList();
        }

        private static bool IsUpcomingOperational(Appointment appointment, DateTime now)
        {
            return !IsClosed(appointment) && GetAppointmentDateTime(appointment) >= now;
        }

        private static bool IsUpcomingForClient(Appointment appointment, DateTime now)
        {
            return appointment.StatusId == 1 || (!IsClosed(appointment) && GetAppointmentDateTime(appointment) >= now);
        }

        private static bool IsClosed(Appointment appointment)
        {
            return appointment.StatusId == 3 || appointment.StatusId == 4;
        }

        private static DateTime GetAppointmentDateTime(Appointment appointment)
        {
            return appointment.AppointmentDate.ToDateTime(appointment.AppointmentTime);
        }
    }
}

```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Repositories\Pets\PetRepository.cs

`$lang
using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Pets
{
    public class PetRepository : IPetRepository
    {
        private readonly VetClinicDbContext _context;

        public PetRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public List<Pet> GetAll()
        {
            return _context.Pets
                .OrderBy(x => x.PetName)
                .ThenBy(x => x.Id)
                .ToList();
        }

        public List<Pet> GetByOwnerId(int ownerId)
        {
            return _context.Pets
                .Where(x => x.OwnerId == ownerId)
                .OrderBy(x => x.PetName)
                .ThenBy(x => x.Id)
                .ToList();
        }

        public List<Pet> GetPaged(int page, int pageSize)
        {
            return _context.Pets
                .OrderBy(x => x.PetName)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public List<Pet> GetPagedByOwnerId(int ownerId, int page, int pageSize)
        {
            return _context.Pets
                .Where(x => x.OwnerId == ownerId)
                .OrderBy(x => x.PetName)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public int GetTotalCount()
        {
            return _context.Pets.Count();
        }

        public int GetTotalCountByOwnerId(int ownerId)
        {
            return _context.Pets.Count(x => x.OwnerId == ownerId);
        }

        public List<Pet> Search(string? search)
        {
            var query = _context.Pets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    (x.PetName != null && x.PetName.Contains(search)) ||
                    (x.Species != null && x.Species.Contains(search)) ||
                    (x.Breed != null && x.Breed.Contains(search)));
            }

            return query
                .OrderBy(x => x.PetName)
                .ThenBy(x => x.Id)
                .ToList();
        }

        public List<Pet> SearchByOwnerId(int ownerId, string? search)
        {
            var query = _context.Pets.Where(x => x.OwnerId == ownerId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    (x.PetName != null && x.PetName.Contains(search)) ||
                    (x.Species != null && x.Species.Contains(search)) ||
                    (x.Breed != null && x.Breed.Contains(search)));
            }

            return query
                .OrderBy(x => x.PetName)
                .ThenBy(x => x.Id)
                .ToList();
        }

        public Pet? GetById(int id)
        {
            return _context.Pets.FirstOrDefault(x => x.Id == id);
        }

        public void Add(Pet pet)
        {
            _context.Pets.Add(pet);
        }

        public void Update(Pet pet)
        {
            _context.Pets.Update(pet);
        }

        public void Delete(Pet pet)
        {
            var appointments = _context.Appointments
                .Where(x => x.PetId == pet.Id)
                .ToList();
            var appointmentIds = appointments.Select(x => x.Id).ToList();

            if (appointmentIds.Any())
            {
                var staffNotifications = _context.StaffNotifications
                    .Where(x => x.AppointmentId.HasValue && appointmentIds.Contains(x.AppointmentId.Value))
                    .ToList();

                if (staffNotifications.Any())
                {
                    foreach (var staffNotification in staffNotifications)
                        staffNotification.AppointmentId = null;
                }

                var reminderLogs = _context.ReminderLogs
                    .Where(x => appointmentIds.Contains(x.AppointmentId))
                    .ToList();

                if (reminderLogs.Any())
                    _context.ReminderLogs.RemoveRange(reminderLogs);
            }

            var medicalRecords = _context.MedicalRecords
                .Where(x => x.PetId == pet.Id)
                .ToList();

            if (medicalRecords.Any())
                _context.MedicalRecords.RemoveRange(medicalRecords);

            var vaccinationRecords = _context.VaccinationRecords
                .Where(x => x.PetId == pet.Id)
                .ToList();

            if (vaccinationRecords.Any())
                _context.VaccinationRecords.RemoveRange(vaccinationRecords);

            if (appointments.Any())
                _context.Appointments.RemoveRange(appointments);

            _context.Pets.Remove(pet);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}

```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Repositories\Users\UserRepository.cs

`$lang
using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly VetClinicDbContext _context;

        public UserRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public User? GetByUsername(string username)
        {
            return _context.Users.FirstOrDefault(x => x.Username == username);
        }

        public User? GetByEmail(string email)
        {
            return _context.Users.FirstOrDefault(x => x.Email == email);
        }

        public User? GetById(int id)
        {
            return _context.Users.FirstOrDefault(x => x.Id == id);
        }

        public PetOwner? GetPetOwnerByUserId(int userId)
        {
            return _context.PetOwners.FirstOrDefault(x => x.UserId == userId);
        }

        public List<User> GetAll()
        {
            return _context.Users
                .Include(x => x.Role)
                .Include(x => x.PetOwner)
                .OrderByDescending(x => x.DateCreated)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        public void Add(User user)
        {
            _context.Users.Add(user);
        }

        public void Update(User user)
        {
            _context.Users.Update(user);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}

```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Repositories\MedicalRecords\MedicalRecordRepository.cs

`$lang
using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.MedicalRecords
{
    public class MedicalRecordRepository : IMedicalRecordRepository
    {
        private readonly VetClinicDbContext _context;

        public MedicalRecordRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public List<MedicalRecord> GetAll()
        {
            return _context.MedicalRecords
                .Include(x => x.Pet)
                .Include(x => x.Appointment)
                    .ThenInclude(x => x!.Service)
                .OrderByDescending(x => x.RecordDate)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        public MedicalRecord? GetById(int id)
        {
            return _context.MedicalRecords
                .Include(x => x.Pet)
                .Include(x => x.Appointment)
                    .ThenInclude(x => x!.Service)
                .FirstOrDefault(x => x.Id == id);
        }

        public void Add(MedicalRecord medicalRecord)
        {
            _context.MedicalRecords.Add(medicalRecord);
        }

        public void Update(MedicalRecord medicalRecord)
        {
            _context.MedicalRecords.Update(medicalRecord);
        }

        public void Delete(MedicalRecord medicalRecord)
        {
            _context.MedicalRecords.Remove(medicalRecord);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}

```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Repositories\Vaccinations\VaccinationRepository.cs

`$lang
using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Vaccinations
{
    public class VaccinationRepository : IVaccinationRepository
    {
        private readonly VetClinicDbContext _context;

        public VaccinationRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public List<VaccinationRecord> GetAll()
        {
            return _context.VaccinationRecords
                .Include(x => x.Pet)
                    .ThenInclude(x => x.Owner)
                .OrderByDescending(x => x.VaccinationDate)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        public VaccinationRecord? GetById(int id)
        {
            return _context.VaccinationRecords
                .Include(x => x.Pet)
                .FirstOrDefault(x => x.Id == id);
        }

        public void Add(VaccinationRecord vaccinationRecord)
        {
            _context.VaccinationRecords.Add(vaccinationRecord);
        }

        public void Update(VaccinationRecord vaccinationRecord)
        {
            _context.VaccinationRecords.Update(vaccinationRecord);
        }

        public void Delete(VaccinationRecord vaccinationRecord)
        {
            _context.VaccinationRecords.Remove(vaccinationRecord);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}

```

## C:\Users\andrea\Desktop\VetClinicSystem\VetClinicSystem\VetClinicSystem\Views\Appointments\Index.cshtml

`$lang
@model IEnumerable<VetClinicSystem.Models.Appointment>

@{
    ViewData["Title"] = "Appointments";
    var roleId = Context.Session.GetInt32("RoleId");
    var isClient = roleId == 3;
}

<h2>Appointments</h2>
<p class="page-subtitle">Pending and upcoming appointments are prioritized first, with recent completed or rejected records shown after active schedules.</p>

<form method="get" asp-action="Index" class="mb-3">
    <div class="row g-2">
        <div class="col-md-4">
            <input type="text" name="search" value="@ViewBag.Search" class="form-control" placeholder="Search by pet, service, or reason" />
        </div>

        <div class="col-md-3">
            <select name="statusId" asp-items="ViewBag.Statuses" class="form-control"></select>
        </div>

        <div class="col-md-3">
            <input type="date" name="appointmentDate" value="@ViewBag.AppointmentDate" class="form-control" />
        </div>

        <div class="col-md-2">
            <button type="submit" class="btn btn-primary">Filter</button>
            <a asp-action="Index" class="btn btn-secondary">Clear</a>
        </div>
    </div>
</form>

<p>
    <a asp-action="Create" class="btn btn-primary">Create Appointment</a>
</p>

<table class="table table-bordered table-appointments">
    <thead>
        <tr>
            <th>Pet</th>
            <th>Service</th>
            <th>Date</th>
            <th>Time</th>
            <th>Reason</th>
            <th>Booking Type</th>
            <th>Status</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model)
        {
            var statusName = item.Status?.StatusName ?? "Unknown";
            var appointmentDateTime = item.AppointmentDate.ToDateTime(item.AppointmentTime);
            var canChangeAppointment = !isClient || appointmentDateTime >= DateTime.Now.AddDays(1);
            var statusClass = statusName.ToLowerInvariant() switch
            {
                "approved" or "completed" or "confirmed" => "status-pill--success",
                "rejected" or "cancelled" or "canceled" => "status-pill--danger",
                "pending" or "scheduled" => "status-pill--warning",
                _ => "status-pill--info"
            };

            <tr>
                <td>@item.Pet?.PetName</td>
                <td>
                    <div>@item.Service?.ServiceName</div>
                    @if (!string.IsNullOrWhiteSpace(item.SurgeryCategory))
                    {
                        <div class="table-action-note">
                            @item.SurgeryCategory surgery@(item.IsEmergency ? " · Emergency" : string.Empty)
                        </div>
                    }
                </td>
                <td>
                    <div>@item.AppointmentDate.ToString("MMM d, yyyy")</div>
                    @if (item.PreferredAppointmentDate.HasValue && item.PreferredAppointmentDate.Value != item.AppointmentDate)
                    {
                        <div class="table-action-note">Client requested @item.PreferredAppointmentDate.Value.ToString("MMM d, yyyy")</div>
                    }
                </td>
                <td class="table-appointments-time">
                    <div class="table-appointments-time__value">@item.AppointmentTime.ToString("h:mm tt")</div>
                    @if (item.PreferredAppointmentTime.HasValue && item.PreferredAppointmentTime.Value != item.AppointmentTime)
                    {
                        <div class="table-action-note">Requested @item.PreferredAppointmentTime.Value.ToString("h:mm tt")</div>
                    }
                    else if (!item.IsScheduleFinalized)
                    {
                        <div class="table-action-note">Awaiting clinic final schedule</div>
                    }
                </td>
                <td>@item.ReasonForVisit</td>
                <td>@(item.IsGuestBooking ? "Guest" : "Client")</td>
                <td><span class="status-pill @statusClass">@statusName</span></td>
                <td>
                    <div class="table-actions">
                        <a asp-action="Details" asp-route-id="@item.Id" class="table-action-icon table-action-icon--details" title="Details" aria-label="View appointment details">
                            <i class="fa-solid fa-eye"></i>
                        </a>
                        @if (canChangeAppointment)
                        {
                            <a asp-action="Edit" asp-route-id="@item.Id" class="table-action-icon table-action-icon--edit" title="Edit" aria-label="Edit appointment">
                                <i class="fa-solid fa-pen-to-square"></i>
                            </a>
                            <a asp-action="Delete" asp-route-id="@item.Id" class="table-action-icon table-action-icon--delete" title="Cancel appointment" aria-label="Cancel appointment">
                                <i class="fa-solid fa-calendar-xmark"></i>
                            </a>
                        }
                        else
                        {
                            <span class="table-action-icon table-action-icon--edit table-action-icon--disabled" title="Appointments cannot be edited less than 1 day before the schedule." aria-label="Edit unavailable">
                                <i class="fa-solid fa-pen-to-square"></i>
                            </span>
                            <span class="table-action-icon table-action-icon--delete table-action-icon--disabled" title="Appointments cannot be cancelled less than 1 day before the schedule." aria-label="Cancel unavailable">
                                <i class="fa-solid fa-calendar-xmark"></i>
                            </span>
                        }

                        @if (roleId == 1 || roleId == 2)
                        {
                                <a asp-action="Approve" asp-route-id="@item.Id" class="table-action-icon table-action-icon--approve" title="Approve" aria-label="Approve appointment">
                                    <i class="fa-solid fa-check"></i>
                                </a>
                                <a asp-action="Reject" asp-route-id="@item.Id" class="table-action-icon table-action-icon--reject" title="Reject" aria-label="Reject appointment">
                                    <i class="fa-solid fa-ban"></i>
                                </a>
                                @if (item.StatusId != 3 && item.StatusId != 4)
                                {
                                    <a asp-action="Complete" asp-route-id="@item.Id" class="table-action-icon table-action-icon--approve" title="Complete" aria-label="Mark appointment completed">
                                        <i class="fa-solid fa-check-double"></i>
                                    </a>
                                }
                        }
                    </div>
                    @if (!canChangeAppointment && isClient)
                    {
                        <div class="table-action-note">
                            Appointments cannot be edited or cancelled less than 1 day before the schedule.
                        </div>
                    }
                </td>
            </tr>
        }
    </tbody>
</table>

```
