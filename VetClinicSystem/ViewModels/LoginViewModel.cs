using System.ComponentModel.DataAnnotations;

namespace VetClinicSystem.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(30, ErrorMessage = "Username must not exceed 30 characters.")]
        [RegularExpression(@"^\S+$", ErrorMessage = "Username must not contain spaces.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(255, ErrorMessage = "Password is too long.")]
        public string Password { get; set; } = string.Empty;
    }
}
