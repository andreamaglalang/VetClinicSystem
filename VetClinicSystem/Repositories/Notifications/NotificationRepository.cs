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
                    !x.IsArchived &&
                    !x.IsRead &&
                    (string.IsNullOrEmpty(x.RecipientRole) || x.RecipientRole == "Staff"))
                .OrderByDescending(x => x.DateCreated)
                .ToList();
        }

        public List<StaffNotification> GetAllForStaff()
        {
            return CreateNotificationQuery()
                .Where(x =>
                    !x.IsArchived &&
                    (string.IsNullOrEmpty(x.RecipientRole) || x.RecipientRole == "Staff"))
                .OrderByDescending(x => x.DateCreated)
                .ToList();
        }

        public List<StaffNotification> GetArchivedForAdmin()
        {
            return CreateNotificationQuery(includeArchived: true)
                .Where(x => x.IsArchived)
                .OrderByDescending(x => x.ArchivedAt)
                .ThenByDescending(x => x.DateCreated)
                .ToList();
        }

        public List<StaffNotification> GetUnreadForUser(int userId)
        {
            return CreateNotificationQuery()
                .Where(x =>
                    !x.IsArchived &&
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
                    !x.IsArchived &&
                    x.UserId == userId &&
                    x.RecipientRole == "Client")
                .OrderByDescending(x => x.DateCreated)
                .ToList();
        }

        public StaffNotification? GetById(int id)
        {
            return CreateNotificationQuery()
                .Where(x => !x.IsArchived)
                .FirstOrDefault(x => x.Id == id);
        }

        public StaffNotification? GetArchivedById(int id)
        {
            return CreateNotificationQuery(includeArchived: true)
                .Where(x => x.IsArchived)
                .FirstOrDefault(x => x.Id == id);
        }

        public List<StaffNotification> GetReadUnarchivedForStaff()
        {
            return CreateNotificationQuery()
                .Where(x =>
                    !x.IsArchived &&
                    x.IsRead &&
                    (string.IsNullOrEmpty(x.RecipientRole) || x.RecipientRole == "Staff"))
                .ToList();
        }

        public List<StaffNotification> GetReadUnarchivedForUser(int userId)
        {
            return CreateNotificationQuery()
                .Where(x =>
                    !x.IsArchived &&
                    x.IsRead &&
                    x.UserId == userId &&
                    x.RecipientRole == "Client")
                .ToList();
        }

        private IQueryable<StaffNotification> CreateNotificationQuery(bool includeArchived = false)
        {
            var query = _context.StaffNotifications
                .Include(x => x.User)
                .Include(x => x.Appointment!)
                    .ThenInclude(x => x.Pet)
                        .ThenInclude(x => x.Owner)
                .Include(x => x.Appointment!)
                    .ThenInclude(x => x.Service)
                .Include(x => x.Appointment!)
                    .ThenInclude(x => x.Status)
                .AsQueryable();

            if (!includeArchived)
                query = query.Where(x => !x.IsArchived);

            return query;
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
