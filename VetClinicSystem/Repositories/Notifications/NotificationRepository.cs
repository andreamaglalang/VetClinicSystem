using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Notifications
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly VetClinicDbContext _context;

        public NotificationRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public List<StaffNotification> GetUnread()
        {
            return _context.StaffNotifications.Where(x => !x.IsRead).ToList();
        }

        public void Add(StaffNotification notification)
        {
            _context.StaffNotifications.Add(notification);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}