using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LAPTOP-MHRETQLO;Database=VetClinicDB;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Appointm__3214EC0763B075C5");

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Appointments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Appointme__Creat__6D0D32F4");

            entity.HasOne(d => d.Pet).WithMany(p => p.Appointments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Appointme__PetId__6A30C649");

            entity.HasOne(d => d.Service).WithMany(p => p.Appointments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Appointme__Servi__6B24EA82");

            entity.HasOne(d => d.Status).WithMany(p => p.Appointments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Appointme__Statu__6C190EBB");
        });

        modelBuilder.Entity<AppointmentStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Appointm__3214EC077B484E49");
        });

        modelBuilder.Entity<ClinicInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ClinicIn__3214EC07B5090437");

            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MedicalR__3214EC07FF022C8E");

            entity.Property(e => e.RecordDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Appointment).WithMany(p => p.MedicalRecords).HasConstraintName("FK__MedicalRe__Appoi__71D1E811");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.MedicalRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MedicalRe__Creat__72C60C4A");

            entity.HasOne(d => d.Pet).WithMany(p => p.MedicalRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MedicalRe__PetId__70DDC3D8");
        });

        modelBuilder.Entity<Pet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Pets__3214EC0788C3CE6F");

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Owner).WithMany(p => p.Pets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Pets__OwnerId__628FA481");
        });

        modelBuilder.Entity<PetOwner>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PetOwner__3214EC07AC70E1B8");

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithOne(p => p.PetOwner)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PetOwners__UserI__5629CD9C");
        });

        modelBuilder.Entity<ReminderLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reminder__3214EC0703CD73C0");

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Appointment).WithMany(p => p.ReminderLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReminderL__Appoi__00200768");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC0718A42E53");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Services__3214EC07482C8767");

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsWalkInOnly).HasDefaultValue(true);
        });

        modelBuilder.Entity<StaffNotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffNot__3214EC07DF536CE4");

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Appointment).WithMany(p => p.StaffNotifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StaffNoti__Appoi__7C4F7684");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC077862B6CF");

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__5165187F");
        });

        modelBuilder.Entity<VaccinationRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Vaccinat__3214EC073C0B76F1");

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.VaccinationRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Vaccinati__Creat__778AC167");

            entity.HasOne(d => d.Pet).WithMany(p => p.VaccinationRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Vaccinati__PetId__76969D2E");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
