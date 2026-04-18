using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Users
{
    public interface IUserRepository
    {
        User? GetByUsername(string username);
        User? GetByEmail(string email);
        User? GetById(int id);
        void Add(User user);
        void Save();
    }
}