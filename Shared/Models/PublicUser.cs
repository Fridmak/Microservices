using System.ComponentModel.DataAnnotations;

namespace Shared.Models
{
    public class PublicUser
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
    }
}
