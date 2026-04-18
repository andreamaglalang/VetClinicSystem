using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

public partial class ReminderLog
{
    [Key]
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    [StringLength(50)]
    public string ReminderType { get; set; } = null!;

    [StringLength(100)]
    public string Recipient { get; set; } = null!;

    [StringLength(255)]
    public string Message { get; set; } = null!;

    [StringLength(50)]
    public string SentStatus { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? SentDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [ForeignKey("AppointmentId")]
    [InverseProperty("ReminderLogs")]
    public virtual Appointment Appointment { get; set; } = null!;
}
