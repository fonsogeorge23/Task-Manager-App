using System.ComponentModel.DataAnnotations;

namespace TaskManagementAPI.Models
{
    public abstract class Base
    {
        [Required]
        public DateTime CreatedDate { get; set; } 

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTime? UpdatedDate { get; set; }

        [Required]
        public int? UpdatedBy { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }
}
