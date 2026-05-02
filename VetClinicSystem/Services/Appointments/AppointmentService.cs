using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Appointments;
using VetClinicSystem.Repositories.Notifications;
using VetClinicSystem.Repositories.Users;

namespace VetClinicSystem.Services.Appointments
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            INotificationRepository notificationRepository,
            IUserRepository userRepository)
        {
            _appointmentRepository = appointmentRepository;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
        }

        public List<Appointment> GetAll()
        {
            return _appointmentRepository.GetAll();
        }

        public List<Appointment> GetByUser(int userId)
        {
            var petOwner = _userRepository.GetPetOwnerByUserId(userId);

            if (petOwner == null)
                return new List<Appointment>();

            return _appointmentRepository.GetByOwnerId(petOwner.Id);
        }

        public List<Appointment> Search(string? search)
        {
            return _appointmentRepository.Search(search);
        }

        public List<Appointment> SearchByUser(int userId, string? search)
        {
            var petOwner = _userRepository.GetPetOwnerByUserId(userId);

            if (petOwner == null)
                return new List<Appointment>();

            return _appointmentRepository.SearchByOwnerId(petOwner.Id, search);
        }

        public List<Appointment> Filter(string? search, int? statusId, DateOnly? appointmentDate)
        {
            return _appointmentRepository.Filter(search, statusId, appointmentDate);
        }

        public List<Appointment> FilterByUser(int userId, string? search, int? statusId, DateOnly? appointmentDate)
        {
            var petOwner = _userRepository.GetPetOwnerByUserId(userId);

            if (petOwner == null)
                return new List<Appointment>();

            return _appointmentRepository.FilterByOwnerId(petOwner.Id, search, statusId, appointmentDate);
        }

        public Appointment? GetById(int id)
        {
            return _appointmentRepository.GetById(id);
        }

        public void Add(Appointment appointment)
        {
            try
            {
                if (appointment.LastUpdated == default)
                    appointment.LastUpdated = DateTime.Now;

                if (HasProperty(appointment, "StatusId"))
                {
                    var currentValue = GetIntPropertyValue(appointment, "StatusId");
                    if (currentValue == 0)
                        SetIntPropertyValue(appointment, "StatusId", 1);
                }

                if (HasProperty(appointment, "AppointmentStatusId"))
                {
                    var currentValue = GetIntPropertyValue(appointment, "AppointmentStatusId");
                    if (currentValue == 0)
                        SetIntPropertyValue(appointment, "AppointmentStatusId", 1);
                }

                if (HasProperty(appointment, "DateCreated"))
                {
                    var currentDate = GetDateTimePropertyValue(appointment, "DateCreated");
                    if (currentDate == default)
                        SetDateTimePropertyValue(appointment, "DateCreated", DateTime.Now);
                }

                _appointmentRepository.Add(appointment);
                _appointmentRepository.Save();

                var notification = new StaffNotification
                {
                    AppointmentId = appointment.Id,
                    Message = "New appointment has been created.",
                    IsRead = false,
                    DateCreated = DateTime.Now
                };

                _notificationRepository.Add(notification);
                _notificationRepository.Save();
            }
            catch (DbUpdateException ex)
            {
                var realMessage = ex.InnerException?.Message ?? ex.Message;
                throw new Exception(realMessage);
            }
        }

        public void Update(Appointment appointment)
        {
            try
            {
                var existingAppointment = _appointmentRepository.GetById(appointment.Id);
                if (existingAppointment == null)
                    throw new Exception("Appointment not found.");

                existingAppointment.PetId = appointment.PetId;
                existingAppointment.ServiceId = appointment.ServiceId;
                existingAppointment.AppointmentDate = appointment.AppointmentDate;
                existingAppointment.AppointmentTime = appointment.AppointmentTime;
                existingAppointment.ReasonForVisit = appointment.ReasonForVisit;
                existingAppointment.ClientNotes = appointment.ClientNotes;
                existingAppointment.StaffNotes = appointment.StaffNotes;
                existingAppointment.IsWalkIn = appointment.IsWalkIn;

                if (appointment.StatusId > 0)
                    existingAppointment.StatusId = appointment.StatusId;

                existingAppointment.LastUpdated = DateTime.Now;
                _appointmentRepository.Save();
            }
            catch (DbUpdateException ex)
            {
                var realMessage = ex.InnerException?.Message ?? ex.Message;
                throw new Exception(realMessage);
            }
        }

        public void Delete(int id)
        {
            try
            {
                var appointment = _appointmentRepository.GetById(id);
                if (appointment != null)
                {
                    _appointmentRepository.Delete(appointment);
                    _appointmentRepository.Save();
                }
            }
            catch (DbUpdateException ex)
            {
                var realMessage = ex.InnerException?.Message ?? ex.Message;
                throw new Exception(realMessage);
            }
        }

        private bool HasProperty(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName) != null;
        }

        private int GetIntPropertyValue(object obj, string propertyName)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null) return 0;
            var value = prop.GetValue(obj);
            return value == null ? 0 : (int)value;
        }

        private void SetIntPropertyValue(object obj, string propertyName, int value)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite)
                prop.SetValue(obj, value);
        }

        private DateTime GetDateTimePropertyValue(object obj, string propertyName)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null) return default;
            var value = prop.GetValue(obj);
            return value == null ? default : (DateTime)value;
        }

        private void SetDateTimePropertyValue(object obj, string propertyName, DateTime value)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite)
                prop.SetValue(obj, value);
        }
    }
}
