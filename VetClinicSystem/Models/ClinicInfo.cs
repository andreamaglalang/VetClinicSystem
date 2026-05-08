using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Helpers;

namespace VetClinicSystem.Models;

[Table("ClinicInfo")]
public partial class ClinicInfo
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Clinic name is required.")]
    [StringLength(150)]
    public string ClinicName { get; set; } = null!;

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(255)]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Contact number is required.")]
    [StringLength(20)]
    [PhilippineMobileNumber]
    public string? ContactNumber { get; set; }

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Operating hours is required.")]
    [StringLength(255)]
    public string? OperatingHours { get; set; }

    [StringLength(255)]
    public string? FacebookPage { get; set; }

    public string? AboutText { get; set; }

    public string? Mission { get; set; }

    public string? Vision { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime LastUpdated { get; set; }
}
