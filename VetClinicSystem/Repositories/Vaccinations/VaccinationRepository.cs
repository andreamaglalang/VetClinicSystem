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
            var records = _context.VaccinationRecords
                .Include(x => x.Pet)
                    .ThenInclude(x => x.Owner)
                .ToList();

            return OrderForManagement(records);
        }

        public VaccinationRecord? GetById(int id)
        {
            return _context.VaccinationRecords
                .Include(x => x.Pet)
                    .ThenInclude(x => x.Owner)
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

        private static List<VaccinationRecord> OrderForManagement(IEnumerable<VaccinationRecord> records)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var dueSoonCutoff = today.AddDays(7);

            return records
                .OrderBy(x => GetStatusPriority(x, today, dueSoonCutoff))
                .ThenBy(x => x.NextDueDate ?? DateOnly.MaxValue)
                .ThenByDescending(x => x.VaccinationDate)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        private static int GetStatusPriority(VaccinationRecord record, DateOnly today, DateOnly dueSoonCutoff)
        {
            if (record.NextDueDate.HasValue && record.NextDueDate.Value < today)
                return 0;

            if (record.NextDueDate.HasValue && record.NextDueDate.Value <= dueSoonCutoff)
                return 1;

            return 2;
        }
    }
}
