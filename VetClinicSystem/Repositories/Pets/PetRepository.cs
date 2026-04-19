using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Pets
{
    public class PetRepository : IPetRepository
    {
        private readonly VetClinicDbContext _context;

        public PetRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public List<Pet> GetAll()
        {
            return _context.Pets.ToList();
        }

        public List<Pet> GetByOwnerId(int ownerId)
        {
            return _context.Pets
                .Where(x => x.OwnerId == ownerId)
                .ToList();
        }

        public List<Pet> Search(string? search)
        {
            var query = _context.Pets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.PetName.Contains(search) ||
                    x.Species.Contains(search) ||
                    x.Breed.Contains(search));
            }

            return query.ToList();
        }

        public List<Pet> SearchByOwnerId(int ownerId, string? search)
        {
            var query = _context.Pets.Where(x => x.OwnerId == ownerId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.PetName.Contains(search) ||
                    x.Species.Contains(search) ||
                    x.Breed.Contains(search));
            }

            return query.ToList();
        }

        public Pet? GetById(int id)
        {
            return _context.Pets.FirstOrDefault(x => x.Id == id);
        }

        public void Add(Pet pet)
        {
            _context.Pets.Add(pet);
        }

        public void Update(Pet pet)
        {
            _context.Pets.Update(pet);
        }

        public void Delete(Pet pet)
        {
            _context.Pets.Remove(pet);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}