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

        public List<StaffNotification> GetUnreadForStaff()
        {
            return CreateNotificationQuery()
                .Where(x =>
                    !x.IsRead &&
                    (string.IsNullOrEmpty(x.RecipientRole) || x.RecipientRole == "Staff"))
                .OrderByDescending(x => x.DateCreated)
                .ToList();
        }

        public List<StaffNotification> GetAllForStaff()
        {
            return CreateNotificationQuery()
                .Where(x => string.IsNullOrEmpty(x.RecipientRole) || x.RecipientRole == "Staff")
                .OrderByDescending(x => x.DateCreated)
                .ToList();
        }

        public List<StaffNotification> GetUnreadForUser(int userId)
        {
            return CreateNotificationQuery()
                .Where(x =>
                    !x.IsRead &&
                    x.UserId == userId &&
                    x.RecipientRole == "Client")
                .OrderByDescending(x => x.DateCreated)
                .ToList();
        }

        public List<StaffNotification> GetAllForUser(int userId)
        {
            return CreateNotificationQuery()
                .Where(x =>
                    x.UserId == userId &&
                    x.RecipientRole == "Client")
                .OrderByDescending(x => x.DateCreated)
                .ToList();
        }

        public StaffNotification? GetById(int id)
        {
            return CreateNotificationQuery()
                .FirstOrDefault(x => x.Id == id);
        }

        private IQueryable<StaffNotification> CreateNotificationQuery()
        {
            return _context.StaffNotifications
                .Include(x => x.User)
                .Include(x => x.Appointment!)
                    .ThenInclude(x => x.Pet)
                        .ThenInclude(x => x.Owner)
                .Include(x => x.Appointment!)
                    .ThenInclude(x => x.Service)
                .Include(x => x.Appointment!)
                    .ThenInclude(x => x.Status)
                .AsQueryable();
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
