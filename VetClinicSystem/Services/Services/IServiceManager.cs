using VetClinicSystem.Models;

namespace VetClinicSystem.Services.Services
{
    public interface IServiceManager
    {
        List<Service> GetAll();
        Service? GetById(int id);
        void Add(Service service);
        void Update(Service service);
        void Delete(int id);
    }
}