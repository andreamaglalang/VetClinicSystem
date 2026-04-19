using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models
{
    public partial class VetClinicDbContext : DbContext
    {
        public VetClinicDbContext()
        {
        }

        public VetClinicDbContext(DbContextOptions<VetClinicDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Appointment> Appointments { get; set; }
        public virtual DbSet<AppointmentStatus> AppointmentStatuses { get; set; }
        public virtual DbSet<ClinicInfo> ClinicInfos { get; set; }
        public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }
        public virtual DbSet<Pet> Pets { get; set; }
        public virtual DbSet<PetOwner> PetOwners { get; set; }
        public virtual DbSet<ReminderLog> ReminderLogs { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<Service> Services { get; set; }
        public virtual DbSet<StaffNotification> StaffNotifications { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<VaccinationRecord> VaccinationRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}