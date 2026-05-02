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
            clinicInfo.LastUpdated = DateTime.Now;

            if (existing == null)
            {
                _clinicRepository.Add(clinicInfo);
            }
            else
            {
                existing.ClinicName = clinicInfo.ClinicName;
                existing.Address = clinicInfo.Address;
                existing.ContactNumber = clinicInfo.ContactNumber;
                existing.Email = clinicInfo.Email;
                existing.OperatingHours = clinicInfo.OperatingHours;
                existing.FacebookPage = clinicInfo.FacebookPage;
                existing.AboutText = clinicInfo.AboutText;
                existing.Mission = clinicInfo.Mission;
                existing.Vision = clinicInfo.Vision;
                existing.LastUpdated = clinicInfo.LastUpdated;
            }

            _clinicRepository.Save();
        }
    }
}
