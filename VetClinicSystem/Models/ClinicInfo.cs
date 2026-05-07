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

    [StringLength(150)]
    public string ClinicName { get; set; } = null!;

    [StringLength(255)]
    public string? Address { get; set; }

    [StringLength(20)]
    [PhilippineMobileNumber]
    public string? ContactNumber { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

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
