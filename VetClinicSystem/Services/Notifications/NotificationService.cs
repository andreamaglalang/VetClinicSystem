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
    }
}