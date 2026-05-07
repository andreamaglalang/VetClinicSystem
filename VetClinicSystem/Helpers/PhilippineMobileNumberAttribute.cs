using System.ComponentModel.DataAnnotations;

namespace VetClinicSystem.Helpers
{
    public class PhilippineMobileNumberAttribute : ValidationAttribute
    {
        public PhilippineMobileNumberAttribute()
        {
            ErrorMessage = PhoneNumberHelper.ValidationMessage;
        }

        public override bool IsValid(object? value)
        {
            return PhoneNumberHelper.IsValidPhilippineMobileNumber(value?.ToString());
        }
    }
}
