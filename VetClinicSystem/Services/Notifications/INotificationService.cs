using VetClinicSystem.Models;

namespace VetClinicSystem.Services.Notifications
{
    public interface INotificationService
    {
        List<StaffNotification> GetUnread();
    }
}