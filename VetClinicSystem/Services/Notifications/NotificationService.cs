using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Notifications;

namespace VetClinicSystem.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public List<StaffNotification> GetUnreadForStaff()
        {
            return _notificationRepository.GetUnreadForStaff();
        }

        public List<StaffNotification> GetAllForStaff()
        {
            return _notificationRepository.GetAllForStaff();
        }

        public List<StaffNotification> GetUnreadForUser(int userId)
        {
            return _notificationRepository.GetUnreadForUser(userId);
        }

        public List<StaffNotification> GetAllForUser(int userId)
        {
            return _notificationRepository.GetAllForUser(userId);
        }

        public void Add(StaffNotification notification)
        {
            _notificationRepository.Add(notification);
            _notificationRepository.Save();
        }

        public void CreateClientNotification(Appointment appointment, string message)
        {
            if (appointment.CreatedByUserId <= 0)
                return;

            Add(new StaffNotification
            {
                AppointmentId = appointment.Id,
                UserId = appointment.CreatedByUserId,
                RecipientRole = "Client",
                Message = TrimMessage(message),
                IsRead = false,
                DateCreated = DateTime.Now
            });
        }

        public void CreateStaffNotification(Appointment appointment, string message)
        {
            Add(new StaffNotification
            {
                AppointmentId = appointment.Id,
                UserId = null,
                RecipientRole = "Staff",
                Message = TrimMessage(message),
                IsRead = false,
                DateCreated = DateTime.Now
            });
        }

        public bool MarkAsReadForStaff(int id)
        {
            var notification = _notificationRepository.GetById(id);
            if (notification == null)
                return false;

            if (!string.IsNullOrEmpty(notification.RecipientRole) && notification.RecipientRole != "Staff")
                return false;

            notification.IsRead = true;
            _notificationRepository.Update(notification);
            _notificationRepository.Save();
            return true;
        }

        public bool MarkAsReadForUser(int id, int userId)
        {
            var notification = _notificationRepository.GetById(id);
            if (notification == null)
                return false;

            if (notification.RecipientRole != "Client" || notification.UserId != userId)
                return false;

            notification.IsRead = true;
            _notificationRepository.Update(notification);
            _notificationRepository.Save();
            return true;
        }

        private static string TrimMessage(string message)
        {
            return message.Length <= 255 ? message : message[..255];
        }
    }
}
