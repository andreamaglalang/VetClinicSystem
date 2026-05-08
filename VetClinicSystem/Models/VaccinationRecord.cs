using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

[Index("PetId", Name = "IX_VaccinationRecords_PetId")]
public partial class VaccinationRecord : IValidatableObject
{
    [Key]
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid pet.")]
    public int PetId { get; set; }

    [Required(ErrorMessage = "Vaccine name is required.")]
    [StringLength(100)]
    public string VaccineName { get; set; } = null!;

    public DateOnly VaccinationDate { get; set; }

    public DateOnly? NextDueDate { get; set; }

    [StringLength(255, ErrorMessage = "Notes must not exceed 255 characters.")]
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NextDueDate.HasValue && NextDueDate.Value <= VaccinationDate)
        {
            yield return new ValidationResult(
                "Next due date must be after vaccination date.",
                new[] { nameof(NextDueDate) });
        }
    }
}
