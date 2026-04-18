using System.Security.Cryptography;
using System.Text;

namespace VetClinicSystem.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            using SHA256 sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}