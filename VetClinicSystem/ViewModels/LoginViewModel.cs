using System.ComponentModel.DataAnnotations;
using VetClinicSystem.Helpers;

namespace VetClinicSystem.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(20, ErrorMessage = "Username must not exceed 20 characters.")]
        [RegularExpression(InputValidationHelper.UsernamePattern, ErrorMessage = "Username must be 4-20 characters and may only contain letters, numbers, or underscores.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(255, ErrorMessage = "Password is too long.")]
        public string Password { get; set; } = string.Empty;
    }
}
