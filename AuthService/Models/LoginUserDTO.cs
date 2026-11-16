using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuthService.Models
{
    public class LoginUserDTO
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }
}
