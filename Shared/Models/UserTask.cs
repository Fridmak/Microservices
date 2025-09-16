using System.ComponentModel.DataAnnotations;

namespace Shared.Models
{
    public class UserTask
    {
        [Required]
        public Guid Id { get; init; }
        [Required]
        public Guid AssignedToUserId { get; set; }
        [Required]
        public DateTime CreationTime { get; set; }
        [Required]
        public DateTime DeadLine {  get; set; }
        [Required]
        public Priority Priority { get; set; }
        public List<string> Comment { get; set; } = new();
    }

    public enum Priority
    {
        Normal,
        High,
        Low,
        Nessesary
    }
}
