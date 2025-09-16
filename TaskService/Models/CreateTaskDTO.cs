using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace TaskService.Models
{
    public class CreateTaskDTO
    {
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public DateTime DeadLine { get; set; }

        [Required]
        public Priority Priority { get; set; }
    }
}
