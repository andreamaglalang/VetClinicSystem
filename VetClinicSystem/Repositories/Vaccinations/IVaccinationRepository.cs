using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Vaccinations
{
    public interface IVaccinationRepository
    {
        List<VaccinationRecord> GetAll();
        VaccinationRecord? GetById(int id);
        void Add(VaccinationRecord vaccinationRecord);
        void Update(VaccinationRecord vaccinationRecord);
        void Delete(VaccinationRecord vaccinationRecord);
        void Save();
    }
}