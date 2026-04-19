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

        public void SaveClinicInfo(ClinicInfo clinicInfo)
        {
            var existing = _clinicRepository.GetClinicInfo();

            if (existing == null)
                _clinicRepository.Add(clinicInfo);
            else
                _clinicRepository.Update(clinicInfo);

            _clinicRepository.Save();
        }
    }
}