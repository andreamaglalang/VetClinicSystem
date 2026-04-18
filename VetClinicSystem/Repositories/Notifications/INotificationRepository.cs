using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Notifications
{
    public interface INotificationRepository
    {
        List<StaffNotification> GetUnread();
        void Add(StaffNotification notification);
        void Save();
    }
}