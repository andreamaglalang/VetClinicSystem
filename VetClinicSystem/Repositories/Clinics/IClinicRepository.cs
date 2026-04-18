using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Clinics
{
    public interface IClinicRepository
    {
        ClinicInfo? GetClinicInfo();
        void Update(ClinicInfo clinicInfo);
        void Save();
    }
}