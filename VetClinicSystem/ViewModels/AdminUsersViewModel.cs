using VetClinicSystem.Models;

namespace VetClinicSystem.ViewModels
{
    public class AdminUsersViewModel
    {
        public List<User> Users { get; set; } = new();

        public string Filter { get; set; } = "all";

        public int CurrentUserId { get; set; }

        public int ActiveCount { get; set; }

        public int InactiveCount { get; set; }

        public int ArchivedCount { get; set; }
    }
}
