using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Models
{
    public class UserDbModel
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        public PublicUser ToPublicUser()
        {
            return new PublicUser
            {
                Id = Id,
                Email = Email,
                Name = Name
            };
        }
    }
}
