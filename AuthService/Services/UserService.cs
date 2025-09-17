using AuthService.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace AuthService.Services
{
    public interface IUserService
    {
        Task<PublicUser> RegisterAsync(string name, string email, string password);
        Task<PublicUser?> AuthenticateAsync(string email, string password);
        Task<bool> UserExistsAsync(string email);
        Task<PublicUser?> GetByIdAsync(Guid id);
    }
    public class UserService : IUserService
    {
        private AuthDbContext _context;
        private PasswordService _passwordService;

        public UserService(AuthDbContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<PublicUser> RegisterAsync(string name, string email, string password)
        {

            if (await UserExistsAsync(email))
                throw new InvalidOperationException("User with this email already exists");

            var user = new UserDbModel
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email
            };

            user.PasswordHash = _passwordService.HashPassword(password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user.ToPublicUser();
        }

        public async Task<PublicUser?> AuthenticateAsync(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return null;

            if (!_passwordService.VerifyPassword(password, user.PasswordHash))
                return null;

            return user.ToPublicUser();
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email);
        }

        public async Task<PublicUser?> GetByIdAsync(Guid id)
        {
            var userDto = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new PublicUser
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email
                })
                .FirstOrDefaultAsync();

            return userDto;
        }
    }
}
