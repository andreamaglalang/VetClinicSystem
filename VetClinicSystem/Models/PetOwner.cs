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

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be 2-50 characters long.")]
    [RegularExpression(InputValidationHelper.PersonNamePattern, ErrorMessage = InputValidationHelper.PersonNameMessage)]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be 2-50 characters long.")]
    [RegularExpression(InputValidationHelper.PersonNamePattern, ErrorMessage = InputValidationHelper.PersonNameMessage)]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "Contact number is required.")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = InputValidationHelper.ContactNumberMessage)]
    [RegularExpression(InputValidationHelper.ContactNumberPattern, ErrorMessage = InputValidationHelper.ContactNumberMessage)]
    public string ContactNumber { get; set; } = null!;

    [StringLength(150, MinimumLength = 5, ErrorMessage = "Address must be 5-150 characters long.")]
    [RegularExpression(InputValidationHelper.AddressPattern, ErrorMessage = InputValidationHelper.AddressMessage)]
    public string? Address { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [InverseProperty("Owner")]
    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();

    [ForeignKey("UserId")]
    [InverseProperty("PetOwner")]
    public virtual User User { get; set; } = null!;
}
