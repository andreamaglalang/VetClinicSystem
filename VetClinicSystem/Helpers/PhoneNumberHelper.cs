using System.Text.RegularExpressions;

namespace VetClinicSystem.Helpers
{
    public static class PhoneNumberHelper
    {
        public const string ValidationMessage = "Please enter a valid 11-digit mobile number starting with 09.";

        public static string Normalize(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            return Regex.Replace(phoneNumber.Trim(), @"[\s-]", string.Empty);
        }

        public static bool IsValidPhilippineMobileNumber(string? phoneNumber)
        {
            return Regex.IsMatch(Normalize(phoneNumber), @"^09\d{9}$");
        }
    }
}
