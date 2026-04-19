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

        public List<StaffNotification> GetAll()
        {
            return _context.StaffNotifications.ToList();
        }

        public void Add(StaffNotification notification)
        {
            _context.StaffNotifications.Add(notification);
        }

        public void Update(StaffNotification notification)
        {
            _context.StaffNotifications.Update(notification);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}