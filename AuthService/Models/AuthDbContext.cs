using Microsoft.EntityFrameworkCore;

namespace AuthService.Models
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<UserDbModel> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserDbModel>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(user => user.Id);
                entity.Property(user => user.Name).HasMaxLength(50).IsRequired();
                entity.Property(user => user.Email).HasMaxLength(254).IsRequired();
                entity.Property(user => user.PasswordHash).IsRequired();
                entity.HasIndex(user => user.Email).IsUnique();
            });
        }
    }
}
