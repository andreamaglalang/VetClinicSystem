using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Clinics
{
    public interface IClinicRepository
    {
        ClinicInfo? GetClinicInfo();
        void Add(ClinicInfo clinicInfo);
        void Update(ClinicInfo clinicInfo);
        void Save();
    }
}