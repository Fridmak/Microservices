using System.ComponentModel.DataAnnotations;

namespace TaskService.Models
{
    public class AssignTaskDTO
    {
        [Required]
        public string UserId { get; set; }
    }
}
