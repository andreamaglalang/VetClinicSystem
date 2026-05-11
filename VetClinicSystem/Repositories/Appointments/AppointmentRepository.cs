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
                    .ThenInclude(x => x.Owner)
                .Include(x => x.Service)
                .Include(x => x.Status)
                .ToList();

            return OrderForManagement(appointments);
        }

        public List<Appointment> GetByOwnerId(int ownerId)
        {
            var appointments = _context.Appointments
                .Include(x => x.Pet)
                    .ThenInclude(x => x.Owner)
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
                    .ThenInclude(x => x.Owner)
                .Include(x => x.Service)
                .Include(x => x.Status)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    (x.Pet != null && x.Pet.PetName.Contains(search)) ||
                    (x.Pet != null && x.Pet.Owner != null && (
                        (((x.Pet.Owner.FirstName ?? string.Empty) + " " + (x.Pet.Owner.LastName ?? string.Empty)).Trim().Contains(search)) ||
                        (x.Pet.Owner.FirstName != null && x.Pet.Owner.FirstName.Contains(search)) ||
                        (x.Pet.Owner.LastName != null && x.Pet.Owner.LastName.Contains(search))
                    )) ||
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
                    .ThenInclude(x => x.Owner)
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
                .OrderBy(x => GetStatusPriority(x))
                .ThenBy(x => IsPastActiveAppointment(x, now))
                .ThenBy(x => GetPrimaryScheduleSortKey(x, now))
                .ThenBy(x => GetSecondaryScheduleSortKey(x, now))
                .ThenByDescending(x => x.LastUpdated)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        private static List<Appointment> OrderForClient(IEnumerable<Appointment> appointments)
        {
            var now = DateTime.Now;

            return appointments
                .OrderBy(x => GetStatusPriority(x))
                .ThenBy(x => IsPastActiveAppointment(x, now))
                .ThenBy(x => GetPrimaryScheduleSortKey(x, now))
                .ThenBy(x => GetSecondaryScheduleSortKey(x, now))
                .ThenByDescending(x => x.LastUpdated)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        private static int GetStatusPriority(Appointment appointment)
        {
            return NormalizeStatusName(appointment.Status?.StatusName) switch
            {
                "pending" or "approved" or "confirmed" => 0,
                "completed" or "complete" or "done" or "rejected" or "declined" or "cancelled" or "canceled" => 1,
                _ => 2
            };
        }

        private static int IsPastActiveAppointment(Appointment appointment, DateTime now)
        {
            var statusName = NormalizeStatusName(appointment.Status?.StatusName);
            if (statusName is not ("pending" or "approved" or "confirmed"))
                return 0;

            return appointment.AppointmentDate.ToDateTime(appointment.AppointmentTime) < now ? 1 : 0;
        }

        private static string NormalizeStatusName(string? statusName)
        {
            return string.IsNullOrWhiteSpace(statusName)
                ? string.Empty
                : statusName.Trim().Replace(" ", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static long GetPrimaryScheduleSortKey(Appointment appointment, DateTime now)
        {
            var appointmentDateTime = appointment.AppointmentDate.ToDateTime(appointment.AppointmentTime);
            var ticks = appointmentDateTime.Ticks;

            return GetStatusPriority(appointment) switch
            {
                0 when appointmentDateTime < now => -ticks,
                1 => -ticks,
                _ => ticks
            };
        }

        private static DateTime GetSecondaryScheduleSortKey(Appointment appointment, DateTime now)
        {
            return appointment.AppointmentDate.ToDateTime(appointment.AppointmentTime);
        }
    }
}
