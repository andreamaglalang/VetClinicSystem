using System.ComponentModel.DataAnnotations;
using VetClinicSystem.Helpers;

namespace VetClinicSystem.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "Username must be 4-20 characters and may only contain letters, numbers, or underscores.")]
        [RegularExpression(InputValidationHelper.UsernamePattern, ErrorMessage = InputValidationHelper.UsernameMessage)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
        [RegularExpression(InputValidationHelper.GmailPattern, ErrorMessage = InputValidationHelper.GmailMessage)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [RegularExpression(InputValidationHelper.PasswordPattern, ErrorMessage = InputValidationHelper.PasswordMessage)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Confirm password must match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be 2-50 characters long.")]
        [RegularExpression(InputValidationHelper.PersonNamePattern, ErrorMessage = InputValidationHelper.PersonNameMessage)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be 2-50 characters long.")]
        [RegularExpression(InputValidationHelper.PersonNamePattern, ErrorMessage = InputValidationHelper.PersonNameMessage)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = InputValidationHelper.ContactNumberMessage)]
        [RegularExpression(InputValidationHelper.ContactNumberPattern, ErrorMessage = InputValidationHelper.ContactNumberMessage)]
        public string ContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "Address must be 5-150 characters long.")]
        [RegularExpression(InputValidationHelper.AddressPattern, ErrorMessage = InputValidationHelper.AddressMessage)]
        public string Address { get; set; } = string.Empty;
    }
}
