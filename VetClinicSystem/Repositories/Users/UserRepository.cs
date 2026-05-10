using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly VetClinicDbContext _context;

        public UserRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public User? GetByUsername(string username)
        {
            return _context.Users
                .Where(x => x.Username == username)
                .AsEnumerable()
                .FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.Ordinal));
        }

        public User? GetByEmail(string email)
        {
            return _context.Users.FirstOrDefault(x => x.Email == email);
        }

        public User? GetById(int id)
        {
            return _context.Users.FirstOrDefault(x => x.Id == id);
        }

        public PetOwner? GetPetOwnerByUserId(int userId)
        {
            return _context.PetOwners.FirstOrDefault(x => x.UserId == userId);
        }

        public List<User> GetAll()
        {
            return _context.Users
                .Include(x => x.Role)
                .Include(x => x.PetOwner)
                .OrderByDescending(x => x.DateCreated)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        public void Add(User user)
        {
            _context.Users.Add(user);
        }

        public void Update(User user)
        {
            _context.Users.Update(user);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
