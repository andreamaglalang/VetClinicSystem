using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

[Index("OwnerId", Name = "IX_Pets_OwnerId")]
public partial class Pet
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    [StringLength(100)]
    public string PetName { get; set; } = null!;

    [StringLength(50)]
    public string Species { get; set; } = null!;

    [StringLength(100)]
    public string? Breed { get; set; }

    [StringLength(20)]
    public string? Sex { get; set; }

    public DateOnly? BirthDate { get; set; }

    public int? Age { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? Weight { get; set; }

    [StringLength(255)]
    public string? Notes { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [InverseProperty("Pet")]
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    [InverseProperty("Pet")]
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    [ForeignKey("OwnerId")]
    [InverseProperty("Pets")]
    public virtual PetOwner Owner { get; set; } = null!;

    [InverseProperty("Pet")]
    public virtual ICollection<VaccinationRecord> VaccinationRecords { get; set; } = new List<VaccinationRecord>();
}
