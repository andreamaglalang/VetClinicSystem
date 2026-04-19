using VetClinicSystem.Models;

namespace VetClinicSystem.Services.Clinics
{
    public interface IClinicService
    {
        ClinicInfo? GetClinicInfo();
        void SaveClinicInfo(ClinicInfo clinicInfo);
    }
}