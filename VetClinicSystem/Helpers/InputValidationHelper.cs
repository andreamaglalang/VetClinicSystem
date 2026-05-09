using System.Text.RegularExpressions;

namespace VetClinicSystem.Helpers
{
    public static class InputValidationHelper
    {
        public const string UsernamePattern = @"^(?=.{4,30}$)(?=.*[A-Za-z])[A-Za-z0-9._]+$";
        public const string UsernameMessage = "Username must be 4-30 characters and may only contain letters, numbers, dots, or underscores.";

        public const string GmailPattern = @"^(?=.{1,100}$)[A-Za-z0-9](?:[A-Za-z0-9._%+-]{0,62}[A-Za-z0-9])?@gmail\.com$";
        public const string GmailMessage = "Email must be a valid Gmail address.";

        public const string PersonNamePattern = @"^(?=.{2,50}$)[A-Za-z]+(?:[ '-][A-Za-z]+)*$";
        public const string PersonNameMessage = "Name must contain letters only and cannot include numbers or symbols.";

        public const string ContactNumberPattern = @"^09\d{9}$";
        public const string ContactNumberMessage = "Contact number must be 11 digits and start with 09.";

        public const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$";
        public const string PasswordMessage = "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.";

        public const string AddressPattern = @"^(?=.{5,150}$)(?=.*[A-Za-z])[A-Za-z0-9.,/\- ]+$";
        public const string AddressMessage = "Address must be complete and cannot be numbers only.";

        public static string NormalizeTrimmed(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return Regex.Replace(value.Trim(), @"\s+", " ");
        }

        public static string NormalizeEmail(string? email)
        {
            return NormalizeTrimmed(email).ToLowerInvariant();
        }

        public static bool IsValidUsername(string? username)
        {
            return !string.IsNullOrWhiteSpace(username) &&
                   Regex.IsMatch(username.Trim(), UsernamePattern);
        }

        public static bool IsValidGmail(string? email)
        {
            return !string.IsNullOrWhiteSpace(email) &&
                   Regex.IsMatch(email.Trim(), GmailPattern, RegexOptions.IgnoreCase);
        }

        public static bool IsValidPersonName(string? name)
        {
            return !string.IsNullOrWhiteSpace(name) &&
                   Regex.IsMatch(NormalizeTrimmed(name), PersonNamePattern);
        }

        public static bool IsValidPhilippineMobile(string? contactNumber)
        {
            return !string.IsNullOrWhiteSpace(contactNumber) &&
                   Regex.IsMatch(contactNumber.Trim(), ContactNumberPattern);
        }

        public static bool IsValidPassword(string? password)
        {
            return !string.IsNullOrWhiteSpace(password) &&
                   Regex.IsMatch(password, PasswordPattern);
        }

        public static bool IsValidAddress(string? address)
        {
            return !string.IsNullOrWhiteSpace(address) &&
                   Regex.IsMatch(NormalizeTrimmed(address), AddressPattern);
        }
    }
}
