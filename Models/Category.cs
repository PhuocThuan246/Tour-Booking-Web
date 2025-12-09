using System.ComponentModel.DataAnnotations;

namespace TourBookingWeb.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        [Required(ErrorMessage = "Tên loại tour không được để trống")]
        [StringLength(200, ErrorMessage = "Tên loại tour không được vượt quá 200 ký tự")]
        [Display(Name = "Tên loại tour")]
        public string CategoryName { get; set; }
    }
}
