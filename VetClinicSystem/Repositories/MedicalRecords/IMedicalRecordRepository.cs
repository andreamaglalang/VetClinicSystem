using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.MedicalRecords
{
    public interface IMedicalRecordRepository
    {
        List<MedicalRecord> GetAll();
        MedicalRecord? GetById(int id);
        void Add(MedicalRecord medicalRecord);
        void Update(MedicalRecord medicalRecord);
        void Delete(MedicalRecord medicalRecord);
        void Save();
    }
}