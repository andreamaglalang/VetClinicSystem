using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Clinics;

namespace VetClinicSystem.Services.Clinics
{
    public class ClinicService : IClinicService
    {
        private readonly IClinicRepository _clinicRepository;

        public ClinicService(IClinicRepository clinicRepository)
        {
            _clinicRepository = clinicRepository;
        }

        public ClinicInfo? GetClinicInfo()
        {
            return _clinicRepository.GetClinicInfo();
        }

        public void UpdateClinicInfo(ClinicInfo clinicInfo)
        {
            _clinicRepository.Update(clinicInfo);
            _clinicRepository.Save();
        }
    }
}