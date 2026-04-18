using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

public partial class StaffNotification
{
    [Key]
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    [StringLength(255)]
    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [ForeignKey("AppointmentId")]
    [InverseProperty("StaffNotifications")]
    public virtual Appointment Appointment { get; set; } = null!;
}
