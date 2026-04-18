using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Services
{
    public interface IServiceRepository
    {
        List<Service> GetAll();
        Service? GetById(int id);
        void Add(Service service);
        void Update(Service service);
        void Delete(Service service);
        void Save();
    }
}