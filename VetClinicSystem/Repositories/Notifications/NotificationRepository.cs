using Microsoft.EntityFrameworkCore;
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
            return _context.StaffNotifications
                .Include(x => x.Appointment)
                    .ThenInclude(x => x.Pet)
                        .ThenInclude(x => x.Owner)
                .Include(x => x.Appointment)
                    .ThenInclude(x => x.Service)
                .Include(x => x.Appointment)
                    .ThenInclude(x => x.Status)
                .Where(x => !x.IsRead)
                .OrderByDescending(x => x.DateCreated)
                .ToList();
        }

        public List<StaffNotification> GetAll()
        {
            return _context.StaffNotifications
                .Include(x => x.Appointment)
                    .ThenInclude(x => x.Pet)
                        .ThenInclude(x => x.Owner)
                .Include(x => x.Appointment)
                    .ThenInclude(x => x.Service)
                .Include(x => x.Appointment)
                    .ThenInclude(x => x.Status)
                .OrderByDescending(x => x.DateCreated)
                .ToList();
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
