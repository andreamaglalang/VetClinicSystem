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
            if (service.DateCreated == default)
                service.DateCreated = DateTime.Now;

            service.IsActive = true;

            _serviceRepository.Add(service);
            _serviceRepository.Save();
        }

        public void Update(Service service)
        {
            var existingService = _serviceRepository.GetById(service.Id);
            if (existingService == null)
                throw new Exception("Service not found.");

            existingService.ServiceName = service.ServiceName;
            existingService.Description = service.Description;
            existingService.IsWalkInOnly = service.IsWalkInOnly;
            existingService.RequiresAppointment = service.RequiresAppointment;

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
