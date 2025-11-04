using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Models
{
    public class TaskObject : Base
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public CurrentStatus Status { get; set; } = CurrentStatus.Pending;

        [Required]
        public PriorityLevel Priority { get; set; } = PriorityLevel.Low;

        public DateTime DueDate { get; set; }

        [Required]
        public int UserId {  get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
