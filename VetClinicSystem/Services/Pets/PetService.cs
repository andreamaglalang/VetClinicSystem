using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Pets;

namespace VetClinicSystem.Services.Pets
{
    public class PetService : IPetService
    {
        private readonly IPetRepository _petRepository;

        public PetService(IPetRepository petRepository)
        {
            _petRepository = petRepository;
        }

        public List<Pet> GetAll()
        {
            return _petRepository.GetAll();
        }

        public Pet? GetById(int id)
        {
            return _petRepository.GetById(id);
        }

        public void Add(Pet pet)
        {
            _petRepository.Add(pet);
            _petRepository.Save();
        }

        public void Update(Pet pet)
        {
            _petRepository.Update(pet);
            _petRepository.Save();
        }

        public void Delete(int id)
        {
            var pet = _petRepository.GetById(id);
            if (pet != null)
            {
                _petRepository.Delete(pet);
                _petRepository.Save();
            }
        }
    }
}