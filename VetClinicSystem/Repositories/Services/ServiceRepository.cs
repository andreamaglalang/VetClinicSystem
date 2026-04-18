using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Services
{
    public class ServiceRepository : IServiceRepository
    {
        private readonly VetClinicDbContext _context;

        public ServiceRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public List<Service> GetAll()
        {
            return _context.Services.ToList();
        }

        public Service? GetById(int id)
        {
            return _context.Services.FirstOrDefault(x => x.Id == id);
        }

        public void Add(Service service)
        {
            _context.Services.Add(service);
        }

        public void Update(Service service)
        {
            _context.Services.Update(service);
        }

        public void Delete(Service service)
        {
            _context.Services.Remove(service);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}