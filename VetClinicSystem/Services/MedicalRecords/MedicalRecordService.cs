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
            if (medicalRecord.RecordDate == default)
                medicalRecord.RecordDate = DateTime.Now;

            _medicalRecordRepository.Add(medicalRecord);
            _medicalRecordRepository.Save();
        }

        public void Update(MedicalRecord medicalRecord)
        {
            var existingRecord = _medicalRecordRepository.GetById(medicalRecord.Id);
            if (existingRecord == null)
                throw new Exception("Medical record not found.");

            existingRecord.PetId = medicalRecord.PetId;
            existingRecord.AppointmentId = medicalRecord.AppointmentId;
            existingRecord.Diagnosis = medicalRecord.Diagnosis;
            existingRecord.Treatment = medicalRecord.Treatment;
            existingRecord.Prescription = medicalRecord.Prescription;
            existingRecord.Findings = medicalRecord.Findings;

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
