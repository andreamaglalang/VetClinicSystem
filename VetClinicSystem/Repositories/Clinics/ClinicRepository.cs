using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Clinics
{
    public class ClinicRepository : IClinicRepository
    {
        private readonly VetClinicDbContext _context;

        public ClinicRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public ClinicInfo? GetClinicInfo()
        {
            return _context.ClinicInfos.FirstOrDefault();
        }

        public void Update(ClinicInfo clinicInfo)
        {
            _context.ClinicInfos.Update(clinicInfo);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}