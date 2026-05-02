using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Vaccinations;

namespace VetClinicSystem.Services.Vaccinations
{
    public class VaccinationService : IVaccinationService
    {
        private readonly IVaccinationRepository _vaccinationRepository;

        public VaccinationService(IVaccinationRepository vaccinationRepository)
        {
            _vaccinationRepository = vaccinationRepository;
        }

        public List<VaccinationRecord> GetAll()
        {
            return _vaccinationRepository.GetAll();
        }

        public VaccinationRecord? GetById(int id)
        {
            return _vaccinationRepository.GetById(id);
        }

        public void Add(VaccinationRecord vaccinationRecord)
        {
            if (vaccinationRecord.DateCreated == default)
                vaccinationRecord.DateCreated = DateTime.Now;

            _vaccinationRepository.Add(vaccinationRecord);
            _vaccinationRepository.Save();
        }

        public void Update(VaccinationRecord vaccinationRecord)
        {
            var existingVaccination = _vaccinationRepository.GetById(vaccinationRecord.Id);
            if (existingVaccination == null)
                throw new Exception("Vaccination record not found.");

            existingVaccination.PetId = vaccinationRecord.PetId;
            existingVaccination.VaccineName = vaccinationRecord.VaccineName;
            existingVaccination.VaccinationDate = vaccinationRecord.VaccinationDate;
            existingVaccination.NextDueDate = vaccinationRecord.NextDueDate;
            existingVaccination.Notes = vaccinationRecord.Notes;

            _vaccinationRepository.Save();
        }

        public void Delete(int id)
        {
            var vaccination = _vaccinationRepository.GetById(id);
            if (vaccination != null)
            {
                _vaccinationRepository.Delete(vaccination);
                _vaccinationRepository.Save();
            }
        }
    }
}
