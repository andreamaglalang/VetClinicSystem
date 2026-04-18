using VetClinicSystem.Models;

namespace VetClinicSystem.Services.MedicalRecords
{
    public interface IMedicalRecordService
    {
        List<MedicalRecord> GetAll();
        MedicalRecord? GetById(int id);
        void Add(MedicalRecord medicalRecord);
        void Update(MedicalRecord medicalRecord);
        void Delete(int id);
    }
}