using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Users
{
    public interface IUserRepository
    {
        User? GetByUsername(string username);
        User? GetByEmail(string email);
        User? GetById(int id);
        PetOwner? GetPetOwnerByUserId(int userId);
        List<User> GetAll();
        void Add(User user);
        void Update(User user);
        void Save();
    }
}