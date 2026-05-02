using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Pets;
using VetClinicSystem.Repositories.Users;

namespace VetClinicSystem.Services.Pets
{
    public class PetService : IPetService
    {
        private readonly IPetRepository _petRepository;
        private readonly IUserRepository _userRepository;

        public PetService(IPetRepository petRepository, IUserRepository userRepository)
        {
            _petRepository = petRepository;
            _userRepository = userRepository;
        }

        public List<Pet> GetAll()
        {
            return _petRepository.GetAll();
        }

        public List<Pet> GetByUser(int userId)
        {
            var petOwner = _userRepository.GetPetOwnerByUserId(userId);

            if (petOwner == null)
                return new List<Pet>();

            return _petRepository.GetByOwnerId(petOwner.Id);
        }

        public List<Pet> Search(string? search)
        {
            return _petRepository.Search(search);
        }

        public List<Pet> SearchByUser(int userId, string? search)
        {
            var petOwner = _userRepository.GetPetOwnerByUserId(userId);

            if (petOwner == null)
                return new List<Pet>();

            return _petRepository.SearchByOwnerId(petOwner.Id, search);
        }

        public Pet? GetById(int id)
        {
            return _petRepository.GetById(id);
        }

        public void Add(Pet pet, int userId)
        {
            var petOwner = _userRepository.GetPetOwnerByUserId(userId);

            if (petOwner == null)
                throw new Exception("Pet owner record not found for this user.");

            pet.OwnerId = petOwner.Id;

            if (pet.DateCreated == default)
                pet.DateCreated = DateTime.Now;

            _petRepository.Add(pet);
            _petRepository.Save();
        }

        public void Update(Pet pet)
        {
            var existingPet = _petRepository.GetById(pet.Id);
            if (existingPet == null)
                throw new Exception("Pet not found.");

            existingPet.PetName = pet.PetName;
            existingPet.Species = pet.Species;
            existingPet.Breed = pet.Breed;
            existingPet.Sex = pet.Sex;
            existingPet.BirthDate = pet.BirthDate;
            existingPet.Age = pet.Age;
            existingPet.Color = pet.Color;
            existingPet.Weight = pet.Weight;
            existingPet.Notes = pet.Notes;

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
