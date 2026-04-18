using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Services;

namespace VetClinicSystem.Services.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly IServiceRepository _serviceRepository;

        public ServiceManager(IServiceRepository serviceRepository)
        {
            _serviceRepository = serviceRepository;
        }

        public List<Service> GetAll()
        {
            return _serviceRepository.GetAll();
        }

        public Service? GetById(int id)
        {
            return _serviceRepository.GetById(id);
        }

        public void Add(Service service)
        {
            _serviceRepository.Add(service);
            _serviceRepository.Save();
        }

        public void Update(Service service)
        {
            _serviceRepository.Update(service);
            _serviceRepository.Save();
        }

        public void Delete(int id)
        {
            var service = _serviceRepository.GetById(id);
            if (service != null)
            {
                _serviceRepository.Delete(service);
                _serviceRepository.Save();
            }
        }
    }
}