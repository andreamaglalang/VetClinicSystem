using VetClinicSystem.Models;

namespace VetClinicSystem.Services.Pets
{
    public interface IPetService
    {
        List<Pet> GetAll();
        Pet? GetById(int id);
        void Add(Pet pet);
        void Update(Pet pet);
        void Delete(int id);
    }
}