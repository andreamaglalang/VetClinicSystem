using VetClinicSystem.Models;

namespace VetClinicSystem.Services.Notifications
{
    public interface INotificationService
    {
        List<StaffNotification> GetUnreadForStaff();
        List<StaffNotification> GetAllForStaff();
        List<StaffNotification> GetUnreadForUser(int userId);
        List<StaffNotification> GetAllForUser(int userId);
        void Add(StaffNotification notification);
        void CreateClientNotification(Appointment appointment, string message);
        void CreateStaffNotification(Appointment appointment, string message);
        bool MarkAsReadForStaff(int id);
        bool MarkAsReadForUser(int id, int userId);
    }
}
