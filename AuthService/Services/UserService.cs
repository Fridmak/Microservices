using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Models;

namespace AuthService.Services
{
    public interface IUserService
    {
        Task<PublicUser> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default);
        Task<PublicUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
        Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken = default);
        Task<PublicUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<PagedResult<UserSummary>> GetUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    }

    public class UserService : IUserService
    {
        private readonly AuthDbContext _context;
        private readonly PasswordService _passwordService;

        public UserService(AuthDbContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<PublicUser> RegisterAsync(
            string name,
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            var normalizedName = name.Trim();
            var normalizedEmail = NormalizeEmail(email);

            if (normalizedName.Length is < 1 or > 50)
            {
                throw new BadRequestException("Имя должно содержать от 1 до 50 символов.");
            }

            if (System.Text.Encoding.UTF8.GetByteCount(password) > 72)
            {
                throw new BadRequestException("Пароль не должен превышать 72 байта (ограничение BCrypt).");
            }

            if (await UserExistsAsync(normalizedEmail, cancellationToken))
            {
                throw new ConflictException("Пользователь с таким email уже существует.");
            }

            var user = new UserDbModel
            {
                Id = Guid.NewGuid(),
                Name = normalizedName,
                Email = normalizedEmail,
                PasswordHash = _passwordService.HashPassword(password)
            };

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                throw new ConflictException("Пользователь с таким email уже существует.");
            }

            return user.ToPublicUser();
        }

        public async Task<PublicUser?> AuthenticateAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            // BCrypt ignores bytes after the first 72; reject longer input instead of silently truncating it.
            if (System.Text.Encoding.UTF8.GetByteCount(password) > 72)
            {
                return null;
            }

            var normalizedEmail = NormalizeEmail(email);
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (user is null || !_passwordService.VerifyPassword(password, user.PasswordHash))
            {
                return null;
            }

            return user.ToPublicUser();
        }

        public Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = NormalizeEmail(email);
            return _context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        }

        public async Task<PublicUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == id)
                .Select(user => new PublicUser
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PagedResult<UserSummary>> GetUsersAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{EscapeLikePattern(search.Trim())}%";
                query = query.Where(user => EF.Functions.ILike(user.Name, pattern, "\\"));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(user => user.Name)
                .ThenBy(user => user.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(user => new UserSummary(user.Id, user.Name))
                .ToListAsync(cancellationToken);

            return new PagedResult<UserSummary>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static string EscapeLikePattern(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }
    }
}
