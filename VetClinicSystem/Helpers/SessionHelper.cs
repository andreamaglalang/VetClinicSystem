using Microsoft.AspNetCore.Http;

namespace VetClinicSystem.Helpers
{
    public static class SessionHelper
    {
        public static bool IsLoggedIn(HttpContext context)
        {
            return context.Session.GetInt32("UserId") != null;
        }

        public static int? GetUserId(HttpContext context)
        {
            return context.Session.GetInt32("UserId");
        }

        public static string? GetUsername(HttpContext context)
        {
            return context.Session.GetString("Username");
        }

        public static int? GetRoleId(HttpContext context)
        {
            return context.Session.GetInt32("RoleId");
        }
    }
}