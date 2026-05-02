using VetClinicSystem.Models;

namespace VetClinicSystem.Services.Users
{
    public interface IUserService
    {
        bool Register(string username, string email, string password, string firstName, string lastName, string contactNumber, string address);
        User? Login(string username, string password);
        User? GetById(int id);
        PetOwner? GetPetOwnerByUserId(int userId);
        List<User> GetAll();
    }
}
