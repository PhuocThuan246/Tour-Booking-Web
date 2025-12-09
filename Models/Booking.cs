using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourBookingWeb.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập ngày đặt tour")]
        [Display(Name = "Ngày đặt")]
        public DateTime BookingDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng người lớn")]
        [Range(1, 100, ErrorMessage = "Số lượng người lớn không hợp lệ")]
        [Display(Name = "Người lớn")]
        public int AdultQuantity { get; set; }

        [Display(Name = "Trẻ em")]
        public int ChildQuantity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tổng giá")]
        [Range(0, 100000000, ErrorMessage = "Tổng giá không hợp lệ")]
        [Display(Name = "Tổng giá")]
        public decimal TotalPrice { get; set; }

        [MaxLength(200)]
        [Display(Name = "Yêu cầu đặc biệt")]
        public string? SpecialRequest { get; set; } // Nên cho phép null

        [Required(ErrorMessage = "Thiếu thông tin tour")]
        [Display(Name = "Tour")]
        public int TourId { get; set; }
        public Tour Tour { get; set; }

        [Required(ErrorMessage = "Thiếu thông tin người đặt")]
        [Display(Name = "Người đặt")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày đi")]
        [Display(Name = "Ngày đi")]
        [DataType(DataType.Date)]
        public DateTime DepartureDate { get; set; }

        public int? TourScheduleId { get; set; }

        [ForeignKey("TourScheduleId")]
        public TourSchedule? TourSchedule { get; set; }

        public string? PaymentMethod { get; set; }

    }
}
