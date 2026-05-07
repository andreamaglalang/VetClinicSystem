using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.MedicalRecords
{
    public class MedicalRecordRepository : IMedicalRecordRepository
    {
        private readonly VetClinicDbContext _context;

        public MedicalRecordRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public List<MedicalRecord> GetAll()
        {
            return _context.MedicalRecords
                .Include(x => x.Pet)
                .Include(x => x.Appointment)
                    .ThenInclude(x => x!.Service)
                .OrderByDescending(x => x.RecordDate)
                .ToList();
        }

        public MedicalRecord? GetById(int id)
        {
            return _context.MedicalRecords
                .Include(x => x.Pet)
                .Include(x => x.Appointment)
                    .ThenInclude(x => x!.Service)
                .FirstOrDefault(x => x.Id == id);
        }

        public void Add(MedicalRecord medicalRecord)
        {
            _context.MedicalRecords.Add(medicalRecord);
        }

        public void Update(MedicalRecord medicalRecord)
        {
            _context.MedicalRecords.Update(medicalRecord);
        }

        public void Delete(MedicalRecord medicalRecord)
        {
            _context.MedicalRecords.Remove(medicalRecord);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
