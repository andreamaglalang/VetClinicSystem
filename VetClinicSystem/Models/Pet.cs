using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Helpers;

namespace VetClinicSystem.Models;

[Index("OwnerId", Name = "IX_Pets_OwnerId")]
public partial class Pet : IValidatableObject
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    [Required(ErrorMessage = "Pet name is required.")]
    [StringLength(100, ErrorMessage = "Pet name must not exceed 100 characters.")]
    [RegularExpression(InputValidationHelper.PetNamePattern, ErrorMessage = InputValidationHelper.PetNameMessage)]
    public string PetName { get; set; } = null!;

    [Required(ErrorMessage = "Species is required.")]
    [StringLength(50, ErrorMessage = "Species must not exceed 50 characters.")]
    [RegularExpression(InputValidationHelper.SpeciesPattern, ErrorMessage = InputValidationHelper.SpeciesMessage)]
    public string Species { get; set; } = null!;

    [Required(ErrorMessage = "Breed is required.")]
    [StringLength(100, ErrorMessage = "Breed must not exceed 100 characters.")]
    [RegularExpression(InputValidationHelper.BreedPattern, ErrorMessage = InputValidationHelper.BreedMessage)]
    public string? Breed { get; set; }

    [RegularExpression(@"^(Male|Female)$", ErrorMessage = "Sex must be Male or Female.")]
    [StringLength(20)]
    public string? Sex { get; set; }

    public DateOnly? BirthDate { get; set; }

    [Range(0, 40, ErrorMessage = "Age must be between 0 and 40 years.")]
    public int? Age { get; set; }

    [StringLength(50)]
    [RegularExpression(InputValidationHelper.ColorPattern, ErrorMessage = InputValidationHelper.ColorMessage)]
    public string? Color { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    [Range(typeof(decimal), "0.10", "200.00", ErrorMessage = "Weight must be between 0.10 kg and 200.00 kg.")]
    public decimal? Weight { get; set; }

    [StringLength(255, ErrorMessage = "Medical history/notes must not exceed 255 characters.")]
    public string? Notes { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    public bool IsDeleted { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DeletedAt { get; set; }

    [InverseProperty("Pet")]
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    [InverseProperty("Pet")]
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    [ForeignKey("OwnerId")]
    [InverseProperty("Pets")]
    public virtual PetOwner Owner { get; set; } = null!;

    [InverseProperty("Pet")]
    public virtual ICollection<VaccinationRecord> VaccinationRecords { get; set; } = new List<VaccinationRecord>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PetValidationHelper.IsValidSpecies(Species))
        {
            yield return new ValidationResult(
                "Please enter a valid species.",
                new[] { nameof(Species) });
        }

        if (!PetValidationHelper.IsValidBreed(Species, Breed))
        {
            yield return new ValidationResult(
                "Please enter a valid breed for the chosen species.",
                new[] { nameof(Breed) });
        }
    }
}
