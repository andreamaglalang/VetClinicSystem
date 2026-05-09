using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VetClinicSystem.Models;

[Index("Username", Name = "UQ__Users__536C85E46EB1C654", IsUnique = true)]
[Index("Email", Name = "UQ__Users__A9D105346FFA82C4", IsUnique = true)]
public partial class User
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [MinLength(4, ErrorMessage = "Username must be at least 4 characters.")]
    [RegularExpression(@"^\S+$", ErrorMessage = "Username must not contain spaces.")]
    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public bool IsGuest { get; set; }

    public bool IsDeleted { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    [StringLength(255)]
    public string? DeleteReason { get; set; }

    public bool MustChangePassword { get; set; }

    public DateTime? LastPasswordChange { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    [InverseProperty("User")]
    public virtual PetOwner? PetOwner { get; set; }

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role Role { get; set; } = null!;

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<VaccinationRecord> VaccinationRecords { get; set; } = new List<VaccinationRecord>();
}
