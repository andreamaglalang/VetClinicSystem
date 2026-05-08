using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

[Index("PetId", Name = "IX_Appointments_PetId")]
[Index("ServiceId", Name = "IX_Appointments_ServiceId")]
[Index("StatusId", Name = "IX_Appointments_StatusId")]
public partial class Appointment
{
    [Key]
    public int Id { get; set; }

    public int PetId { get; set; }

    public int ServiceId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public TimeOnly AppointmentTime { get; set; }

    public DateOnly? PreferredAppointmentDate { get; set; }

    public TimeOnly? PreferredAppointmentTime { get; set; }

    public int StatusId { get; set; }

    [StringLength(20)]
    public string? SurgeryCategory { get; set; }

    public int SurgeryLoadPoints { get; set; }

    public bool IsEmergency { get; set; }

    public bool IsScheduleFinalized { get; set; }

    [StringLength(255)]
    public string? ReasonForVisit { get; set; }

    [StringLength(255)]
    public string? ClientNotes { get; set; }

    [StringLength(255)]
    public string? StaffNotes { get; set; }

    public bool IsWalkIn { get; set; }

    public bool IsGuestBooking { get; set; }

    public int CreatedByUserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdated { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("Appointments")]
    public virtual User CreatedByUser { get; set; } = null!;

    [InverseProperty("Appointment")]
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    [ForeignKey("PetId")]
    [InverseProperty("Appointments")]
    public virtual Pet Pet { get; set; } = null!;

    [InverseProperty("Appointment")]
    public virtual ICollection<ReminderLog> ReminderLogs { get; set; } = new List<ReminderLog>();

    [ForeignKey("ServiceId")]
    [InverseProperty("Appointments")]
    public virtual Service Service { get; set; } = null!;

    [InverseProperty("Appointment")]
    public virtual ICollection<StaffNotification> StaffNotifications { get; set; } = new List<StaffNotification>();

    [ForeignKey("StatusId")]
    [InverseProperty("Appointments")]
    public virtual AppointmentStatus Status { get; set; } = null!;
}
