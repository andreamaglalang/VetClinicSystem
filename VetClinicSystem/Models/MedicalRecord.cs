using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

[Index("PetId", Name = "IX_MedicalRecords_PetId")]
public partial class MedicalRecord
{
    [Key]
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid pet.")]
    public int PetId { get; set; }

    public int? AppointmentId { get; set; }

    [Required(ErrorMessage = "Diagnosis is required.")]
    [StringLength(255, ErrorMessage = "Diagnosis must not exceed 255 characters.")]
    public string? Diagnosis { get; set; }

    [Required(ErrorMessage = "Treatment is required.")]
    [StringLength(255, ErrorMessage = "Treatment must not exceed 255 characters.")]
    public string? Treatment { get; set; }

    [StringLength(255, ErrorMessage = "Prescription must not exceed 255 characters.")]
    public string? Prescription { get; set; }

    [StringLength(1000, ErrorMessage = "Notes/findings must not exceed 1000 characters.")]
    public string? Findings { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime RecordDate { get; set; }

    public int CreatedByUserId { get; set; }

    [ForeignKey("AppointmentId")]
    [InverseProperty("MedicalRecords")]
    public virtual Appointment? Appointment { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("MedicalRecords")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("PetId")]
    [InverseProperty("MedicalRecords")]
    public virtual Pet Pet { get; set; } = null!;
}
