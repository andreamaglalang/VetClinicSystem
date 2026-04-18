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

    public int PetId { get; set; }

    public int? AppointmentId { get; set; }

    [StringLength(255)]
    public string? Diagnosis { get; set; }

    [StringLength(255)]
    public string? Treatment { get; set; }

    [StringLength(255)]
    public string? Prescription { get; set; }

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
