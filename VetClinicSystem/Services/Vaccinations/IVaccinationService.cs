using VetClinicSystem.Models;

namespace VetClinicSystem.Services.Vaccinations
{
    public interface IVaccinationService
    {
        List<VaccinationRecord> GetAll();
        VaccinationRecord? GetById(int id);
        void Add(VaccinationRecord vaccinationRecord);
        void Update(VaccinationRecord vaccinationRecord);
        void Delete(int id);
    }
}