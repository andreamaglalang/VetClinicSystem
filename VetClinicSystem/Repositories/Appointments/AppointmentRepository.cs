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
            return _context.Appointments
                .Include(x => x.Pet)
                .Include(x => x.Service)
                .Include(x => x.Status)
                .ToList();
        }

        public List<Appointment> GetByOwnerId(int ownerId)
        {
            return _context.Appointments
                .Include(x => x.Pet)
                .Include(x => x.Service)
                .Include(x => x.Status)
                .Where(x => x.Pet.OwnerId == ownerId)
                .ToList();
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

            return query.ToList();
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

            return query.ToList();
        }

        public Appointment? GetById(int id)
        {
            return _context.Appointments
                .Include(x => x.Pet)
                    .ThenInclude(x => x.Owner)
                .Include(x => x.Service)
                .Include(x => x.Status)
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
                _context.StaffNotifications.RemoveRange(notifications);

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
    }
}
