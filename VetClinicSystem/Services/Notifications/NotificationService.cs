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

        public List<StaffNotification> GetUnread()
        {
            return _notificationRepository.GetUnread();
        }

        public List<StaffNotification> GetAll()
        {
            return _notificationRepository.GetAll();
        }

        public void MarkAsRead(int id)
        {
            var notification = _notificationRepository.GetAll().FirstOrDefault(x => x.Id == id);

            if (notification != null)
            {
                notification.IsRead = true;
                _notificationRepository.Update(notification);
                _notificationRepository.Save();
            }
        }
    }
}