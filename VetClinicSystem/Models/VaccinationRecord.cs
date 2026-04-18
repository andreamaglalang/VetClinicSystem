using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

[Index("PetId", Name = "IX_VaccinationRecords_PetId")]
public partial class VaccinationRecord
{
    [Key]
    public int Id { get; set; }

    public int PetId { get; set; }

    [StringLength(100)]
    public string VaccineName { get; set; } = null!;

    public DateOnly VaccinationDate { get; set; }

    public DateOnly? NextDueDate { get; set; }

    [StringLength(255)]
    public string? Notes { get; set; }

    public int CreatedByUserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("VaccinationRecords")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("PetId")]
    [InverseProperty("VaccinationRecords")]
    public virtual Pet Pet { get; set; } = null!;
}
