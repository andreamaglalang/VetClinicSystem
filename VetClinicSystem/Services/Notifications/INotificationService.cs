using VetClinicSystem.Models;

namespace VetClinicSystem.Services.Notifications
{
    public interface INotificationService
    {
        List<StaffNotification> GetUnread();
        List<StaffNotification> GetAll();
        void MarkAsRead(int id);
    }
}