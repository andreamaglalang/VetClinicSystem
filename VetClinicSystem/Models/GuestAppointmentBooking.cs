using System.ComponentModel.DataAnnotations;
using VetClinicSystem.Helpers;

namespace VetClinicSystem.Models;

public class GuestAppointmentBooking : IValidatableObject
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be 2-50 characters long.")]
    [RegularExpression(InputValidationHelper.PersonNamePattern, ErrorMessage = InputValidationHelper.PersonNameMessage)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be 2-50 characters long.")]
    [RegularExpression(InputValidationHelper.PersonNamePattern, ErrorMessage = InputValidationHelper.PersonNameMessage)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "Contact number must be 11 digits and start with 09.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Contact number must be 11 digits and start with 09.")]
    [PhilippineMobileNumber]
    [Display(Name = "Contact Number")]
    public string ContactNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(100)]
    [RegularExpression(InputValidationHelper.GmailPattern, ErrorMessage = InputValidationHelper.GmailMessage)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(150, MinimumLength = 5, ErrorMessage = "Address must be 5-150 characters long.")]
    [RegularExpression(InputValidationHelper.AddressPattern, ErrorMessage = InputValidationHelper.AddressMessage)]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Pet name is required.")]
    [StringLength(100)]
    [RegularExpression(InputValidationHelper.PetNamePattern, ErrorMessage = InputValidationHelper.PetNameMessage)]
    [Display(Name = "Pet Name")]
    public string PetName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Species is required.")]
    [StringLength(50)]
    [RegularExpression(InputValidationHelper.SpeciesPattern, ErrorMessage = InputValidationHelper.SpeciesMessage)]
    public string Species { get; set; } = string.Empty;

    [Required(ErrorMessage = "Breed is required.")]
    [StringLength(100)]
    [RegularExpression(InputValidationHelper.BreedPattern, ErrorMessage = InputValidationHelper.BreedMessage)]
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
