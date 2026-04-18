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

    [StringLength(100)]
    public string ServiceName { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }

    public bool IsWalkInOnly { get; set; }

    public bool RequiresAppointment { get; set; }

    public bool IsActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [InverseProperty("Service")]
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
