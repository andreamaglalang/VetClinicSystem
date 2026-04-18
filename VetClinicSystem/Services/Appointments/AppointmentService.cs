using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Appointments;
using VetClinicSystem.Repositories.Notifications;

namespace VetClinicSystem.Services.Appointments
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly INotificationRepository _notificationRepository;

        public AppointmentService(IAppointmentRepository appointmentRepository, INotificationRepository notificationRepository)
        {
            _appointmentRepository = appointmentRepository;
            _notificationRepository = notificationRepository;
        }

        public List<Appointment> GetAll()
        {
            return _appointmentRepository.GetAll();
        }

        public Appointment? GetById(int id)
        {
            return _appointmentRepository.GetById(id);
        }

        public void Add(Appointment appointment)
        {
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

        public void Update(Appointment appointment)
        {
            _appointmentRepository.Update(appointment);
            _appointmentRepository.Save();
        }

        public void Delete(int id)
        {
            var appointment = _appointmentRepository.GetById(id);
            if (appointment != null)
            {
                _appointmentRepository.Delete(appointment);
                _appointmentRepository.Save();
            }
        }
    }
}