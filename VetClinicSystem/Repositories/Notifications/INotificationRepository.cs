using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Notifications
{
    public interface INotificationRepository
    {
        List<StaffNotification> GetUnreadForStaff();
        List<StaffNotification> GetAllForStaff();
        List<StaffNotification> GetUnreadForUser(int userId);
        List<StaffNotification> GetAllForUser(int userId);
        StaffNotification? GetById(int id);
        void Add(StaffNotification notification);
        void Update(StaffNotification notification);
        void Save();
    }
}
