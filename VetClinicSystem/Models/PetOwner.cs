using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Helpers;

namespace VetClinicSystem.Models;

[Index("UserId", Name = "UQ__PetOwner__1788CC4DCD01F597", IsUnique = true)]
public partial class PetOwner
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [StringLength(100)]
    public string FirstName { get; set; } = null!;

    [StringLength(100)]
    public string LastName { get; set; } = null!;

    [StringLength(20)]
    [PhilippineMobileNumber]
    public string ContactNumber { get; set; } = null!;

    [StringLength(255)]
    public string? Address { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [InverseProperty("Owner")]
    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();

    [ForeignKey("UserId")]
    [InverseProperty("PetOwner")]
    public virtual User User { get; set; } = null!;
}
