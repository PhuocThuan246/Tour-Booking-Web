using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourBookingWeb.Models
{
    public class TourSchedule
    {
        [Key]
        public int TourScheduleId { get; set; }

        [Required]
        public int TourId { get; set; }

        [ForeignKey("TourId")]
        public Tour Tour { get; set; }

        [Required]
        [Display(Name = "Ngày đi")]
        public DateTime DepartureDate { get; set; }

        [Required]
        [Display(Name = "Ngày về")]
        public DateTime ReturnDate { get; set; }

        [Required]
        [Range(0, 1000)]
        [Display(Name = "Chỗ còn")]
        public int RemainingSlot { get; set; }
        [Required]
        public bool IsDeleted { get; set; } = false; // Soft Delete
    }
}
