using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Pets
{
    public interface IPetRepository
    {
        List<Pet> GetAll();
        List<Pet> GetByOwnerId(int ownerId);
        List<Pet> Search(string? search);
        List<Pet> SearchByOwnerId(int ownerId, string? search);
        Pet? GetById(int id);
        void Add(Pet pet);
        void Update(Pet pet);
        void Delete(Pet pet);
        void Save();
    }
}