using System.ComponentModel.DataAnnotations;

namespace Shared.Models
{
    public class PublicUser
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;
    }
}
