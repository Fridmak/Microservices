using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuthService.Models
{
    public class RegisterUserDTO
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(254)]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }
}
