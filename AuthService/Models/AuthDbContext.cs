using Microsoft.EntityFrameworkCore;
using Shared.Models;
using System.Reflection;

namespace AuthService.Models
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
