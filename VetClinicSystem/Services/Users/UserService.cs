using VetClinicSystem.Helpers;
using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Users;

namespace VetClinicSystem.Services.Users
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly VetClinicDbContext _context;

        public UserService(IUserRepository userRepository, VetClinicDbContext context)
        {
            _userRepository = userRepository;
            _context = context;
        }

        public bool Register(string username, string email, string password, string firstName, string lastName, string contactNumber, string address)
        {
            if (_userRepository.GetByUsername(username) != null) return false;
            if (_userRepository.GetByEmail(email) != null) return false;

            var clientRole = _context.Roles.FirstOrDefault(x => x.RoleName == "Client");
            if (clientRole == null) return false;

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(password),
                RoleId = clientRole.Id,
                IsActive = true,
                IsDeleted = false,
                DateCreated = DateTime.Now
            };

            _userRepository.Add(user);
            _userRepository.Save();

            var petOwner = new PetOwner
            {
                UserId = user.Id,
                FirstName = firstName,
                LastName = lastName,
                ContactNumber = contactNumber,
                Address = address,
                DateCreated = DateTime.Now
            };

            _context.PetOwners.Add(petOwner);
            _context.SaveChanges();

            return true;
        }

        public User? Login(string username, string password)
        {
            var user = _userRepository.GetByUsername(username);
            if (user == null) return null;
            if (!string.Equals(user.Username, username, StringComparison.Ordinal)) return null;
            if (!user.IsActive || user.IsDeleted) return null;

            var hashed = PasswordHelper.HashPassword(password);
            if (user.PasswordHash != hashed) return null;

            return user;
        }

        public List<User> GetAll()
        {
            return _userRepository.GetAll();
        }

        public User? GetById(int id)
        {
            return _userRepository.GetById(id);
        }

        public PetOwner? GetPetOwnerByUserId(int userId)
        {
            return _userRepository.GetPetOwnerByUserId(userId);
        }
    }
}
