using System.ComponentModel.DataAnnotations;

namespace VetClinicSystem.Models;

public class GuestAppointmentBooking
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
    [Display(Name = "Contact Number")]
    public string ContactNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Address { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Pet Name")]
    public string PetName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Species { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Breed { get; set; }

    [StringLength(20)]
    public string? Sex { get; set; }

    public int? Age { get; set; }

    [StringLength(255)]
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

    [StringLength(255)]
    [Display(Name = "Reason for Visit")]
    public string? ReasonForVisit { get; set; }

    [StringLength(255)]
    [Display(Name = "Client Notes")]
    public string? ClientNotes { get; set; }
}
