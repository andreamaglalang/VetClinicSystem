using System.ComponentModel.DataAnnotations;
using VetClinicSystem.Helpers;

namespace VetClinicSystem.Models;

public class GuestAppointmentBooking : IValidatableObject
{
    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [PhilippineMobileNumber]
    [Display(Name = "Contact Number")]
    public string ContactNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(255, ErrorMessage = "Address must not exceed 255 characters.")]
    public string? Address { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Pet Name")]
    public string PetName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Species { get; set; } = string.Empty;

    [Required(ErrorMessage = "Breed is required.")]
    [StringLength(100)]
    public string? Breed { get; set; }

    [RegularExpression(@"^(Male|Female)$", ErrorMessage = "Sex must be Male or Female.")]
    [StringLength(20)]
    public string? Sex { get; set; }

    [Range(0, 40, ErrorMessage = "Age must be between 0 and 40 years.")]
    public int? Age { get; set; }

    [Range(typeof(decimal), "0.10", "200.00", ErrorMessage = "Weight must be between 0.10 kg and 200.00 kg.")]
    public decimal? Weight { get; set; }

    [StringLength(255, ErrorMessage = "Pet notes must not exceed 255 characters.")]
    [Display(Name = "Pet Notes")]
    public string? PetNotes { get; set; }

    [Required]
    [Display(Name = "Appointment Service")]
    public int ServiceId { get; set; }

    [Required]
    [Display(Name = "Appointment Date")]
    public DateOnly? AppointmentDate { get; set; }

    [Required]
    [Display(Name = "Appointment Time")]
    public TimeOnly? AppointmentTime { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Surgery Category")]
    public string SurgeryCategory { get; set; } = string.Empty;

    [Display(Name = "Emergency Case")]
    public bool IsEmergency { get; set; }

    [StringLength(255, ErrorMessage = "Reason for visit must not exceed 255 characters.")]
    [Display(Name = "Reason for Visit")]
    public string? ReasonForVisit { get; set; }

    [StringLength(255, ErrorMessage = "Client notes must not exceed 255 characters.")]
    [Display(Name = "Client Notes")]
    public string? ClientNotes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PetValidationHelper.IsValidSpecies(Species))
        {
            yield return new ValidationResult(
                "Please select a valid species.",
                new[] { nameof(Species) });
        }

        if (!PetValidationHelper.IsValidBreed(Species, Breed))
        {
            yield return new ValidationResult(
                "Please select a valid breed for the chosen species.",
                new[] { nameof(Breed) });
        }

        if (AppointmentDate.HasValue && AppointmentDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult(
                "Appointment date cannot be in the past.",
                new[] { nameof(AppointmentDate) });
        }
    }
}
