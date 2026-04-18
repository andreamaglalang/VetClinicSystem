using VetClinicSystem.Models;
using VetClinicSystem.Repositories.MedicalRecords;

namespace VetClinicSystem.Services.MedicalRecords
{
    public class MedicalRecordService : IMedicalRecordService
    {
        private readonly IMedicalRecordRepository _medicalRecordRepository;

        public MedicalRecordService(IMedicalRecordRepository medicalRecordRepository)
        {
            _medicalRecordRepository = medicalRecordRepository;
        }

        public List<MedicalRecord> GetAll()
        {
            return _medicalRecordRepository.GetAll();
        }

        public MedicalRecord? GetById(int id)
        {
            return _medicalRecordRepository.GetById(id);
        }

        public void Add(MedicalRecord medicalRecord)
        {
            _medicalRecordRepository.Add(medicalRecord);
            _medicalRecordRepository.Save();
        }

        public void Update(MedicalRecord medicalRecord)
        {
            _medicalRecordRepository.Update(medicalRecord);
            _medicalRecordRepository.Save();
        }

        public void Delete(int id)
        {
            var record = _medicalRecordRepository.GetById(id);
            if (record != null)
            {
                _medicalRecordRepository.Delete(record);
                _medicalRecordRepository.Save();
            }
        }
    }
}