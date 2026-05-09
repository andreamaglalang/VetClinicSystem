using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Vaccinations
{
    public class VaccinationRepository : IVaccinationRepository
    {
        private readonly VetClinicDbContext _context;

        public VaccinationRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public List<VaccinationRecord> GetAll()
        {
            return _context.VaccinationRecords
                .Include(x => x.Pet)
                    .ThenInclude(x => x.Owner)
                .OrderByDescending(x => x.VaccinationDate)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        public VaccinationRecord? GetById(int id)
        {
            return _context.VaccinationRecords
                .Include(x => x.Pet)
                .FirstOrDefault(x => x.Id == id);
        }

        public void Add(VaccinationRecord vaccinationRecord)
        {
            _context.VaccinationRecords.Add(vaccinationRecord);
        }

        public void Update(VaccinationRecord vaccinationRecord)
        {
            _context.VaccinationRecords.Update(vaccinationRecord);
        }

        public void Delete(VaccinationRecord vaccinationRecord)
        {
            _context.VaccinationRecords.Remove(vaccinationRecord);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
