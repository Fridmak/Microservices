using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Models
{
    public class UserDbModel
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        [MaxLength(20)]
        public string Name { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        [Required]
        public string Email { get; set; }

        public PublicUser ToPublicUser()
        {
            var user = new PublicUser {
                Id = Id,
                Email = Email,
                Name = Name,
            };

            return user;
        }
    }
}
