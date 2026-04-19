using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Notifications
{
    public interface INotificationRepository
    {
        List<StaffNotification> GetUnread();
        List<StaffNotification> GetAll();
        void Add(StaffNotification notification);
        void Update(StaffNotification notification);
        void Save();
    }
}