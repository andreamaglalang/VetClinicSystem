using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

[Table("AppointmentStatus")]
[Index("StatusName", Name = "UQ__Appointm__05E7698AF63D2AAD", IsUnique = true)]
public partial class AppointmentStatus
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string StatusName { get; set; } = null!;

    [InverseProperty("Status")]
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
