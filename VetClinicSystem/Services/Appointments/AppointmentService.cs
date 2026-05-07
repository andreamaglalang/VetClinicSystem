using Microsoft.EntityFrameworkCore;
using System.Globalization;
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

                var savedAppointment = _appointmentRepository.GetById(appointment.Id) ?? appointment;

                var notification = new StaffNotification
                {
                    AppointmentId = appointment.Id,
                    Message = BuildAppointmentNotificationMessage(savedAppointment),
                    IsRead = false,
                    DateCreated = DateTime.Now
                };

                _notificationRepository.Add(notification);
                _notificationRepository.Save();

                AddReminderLogs(savedAppointment);
                _appointmentRepository.Save();
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

        private string BuildAppointmentNotificationMessage(Appointment appointment)
        {
            var ownerName = FormatOwnerName(appointment.Pet?.Owner);
            var petName = string.IsNullOrWhiteSpace(appointment.Pet?.PetName)
                ? "the pet"
                : appointment.Pet.PetName.Trim();
            var serviceName = string.IsNullOrWhiteSpace(appointment.Service?.ServiceName)
                ? "service"
                : appointment.Service.ServiceName.Trim();
            var appointmentDate = appointment.AppointmentDate.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
            var statusName = string.IsNullOrWhiteSpace(appointment.Status?.StatusName)
                ? "Pending"
                : appointment.Status.StatusName.Trim();

            var message = $"{ownerName} requested a {serviceName} appointment for {petName} on {appointmentDate}. Status: {statusName}.";

            return message.Length <= 255 ? message : message[..255];
        }

        private void AddReminderLogs(Appointment appointment)
        {
            var recipient = appointment.CreatedByUser?.Email ?? appointment.Pet?.Owner?.ContactNumber ?? "Client";
            var appointmentDate = appointment.AppointmentDate.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
            var petName = string.IsNullOrWhiteSpace(appointment.Pet?.PetName) ? "your pet" : appointment.Pet.PetName.Trim();
            var serviceName = string.IsNullOrWhiteSpace(appointment.Service?.ServiceName) ? "appointment" : appointment.Service.ServiceName.Trim();

            appointment.ReminderLogs.Add(CreateReminderLog(
                appointment.Id,
                "Confirmation",
                recipient,
                $"{serviceName} appointment for {petName} on {appointmentDate} has been received."));

            appointment.ReminderLogs.Add(CreateReminderLog(
                appointment.Id,
                "Reminder 2 Days Before",
                recipient,
                $"Reminder: {petName} has a {serviceName} appointment in 2 days."));

            appointment.ReminderLogs.Add(CreateReminderLog(
                appointment.Id,
                "Reminder 1 Day Before",
                recipient,
                $"Reminder: {petName} has a {serviceName} appointment tomorrow."));

            appointment.ReminderLogs.Add(CreateReminderLog(
                appointment.Id,
                "Follow-up After Visit",
                recipient,
                $"Follow-up: please monitor {petName} after the visit and contact the clinic for concerns."));
        }

        private ReminderLog CreateReminderLog(int appointmentId, string reminderType, string recipient, string message)
        {
            return new ReminderLog
            {
                AppointmentId = appointmentId,
                ReminderType = reminderType,
                Recipient = recipient,
                Message = message.Length <= 255 ? message : message[..255],
                SentStatus = "Pending",
                DateCreated = DateTime.Now
            };
        }

        private string FormatOwnerName(PetOwner? owner)
        {
            if (owner == null)
                return "A pet owner";

            var fullName = $"{owner.FirstName} {owner.LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? "A pet owner" : fullName;
        }
    }
}
