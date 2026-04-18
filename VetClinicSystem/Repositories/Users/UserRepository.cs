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
            return _context.Users.FirstOrDefault(x => x.Username == username);
        }

        public User? GetByEmail(string email)
        {
            return _context.Users.FirstOrDefault(x => x.Email == email);
        }

        public User? GetById(int id)
        {
            return _context.Users.FirstOrDefault(x => x.Id == id);
        }

        public void Add(User user)
        {
            _context.Users.Add(user);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}