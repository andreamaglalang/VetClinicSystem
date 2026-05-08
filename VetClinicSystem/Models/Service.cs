using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

public partial class Service
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Service name is required.")]
    [StringLength(100)]
    public string ServiceName { get; set; } = null!;

    [StringLength(255, ErrorMessage = "Description must not exceed 255 characters.")]
    public string? Description { get; set; }

    public bool IsWalkInOnly { get; set; }

    public bool RequiresAppointment { get; set; }

    public bool IsActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [InverseProperty("Service")]
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
