using VetClinicSystem.Models;

namespace VetClinicSystem.Services.Pets
{
    public interface IPetService
    {
        List<Pet> GetAll();
        List<Pet> GetByUser(int userId);
        List<Pet> Search(string? search);
        List<Pet> SearchByUser(int userId, string? search);
        Pet? GetById(int id);
        void Add(Pet pet, int userId);
        void Update(Pet pet);
        void Delete(int id);
    }
}