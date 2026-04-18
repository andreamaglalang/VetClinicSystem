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
            _vaccinationRepository.Add(vaccinationRecord);
            _vaccinationRepository.Save();
        }

        public void Update(VaccinationRecord vaccinationRecord)
        {
            _vaccinationRepository.Update(vaccinationRecord);
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