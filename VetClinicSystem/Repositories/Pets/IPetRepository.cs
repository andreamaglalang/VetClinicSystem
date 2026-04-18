using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Pets
{
    public interface IPetRepository
    {
        List<Pet> GetAll();
        Pet? GetById(int id);
        void Add(Pet pet);
        void Update(Pet pet);
        void Delete(Pet pet);
        void Save();
    }
}