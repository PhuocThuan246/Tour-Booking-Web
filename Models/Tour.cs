using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourBookingWeb.Models
{
    public class Tour
    {
        [Key]
        public int TourId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề tour")]
        [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        [Display(Name = "Tiêu đề tour")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng khách")]
        [Range(1, 1000, ErrorMessage = "Sức chứa phải từ 1 đến 1000")]
        [Display(Name = "Số lượng khách tối đa")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả")]
        [MaxLength(20000, ErrorMessage = "Mô tả không được vượt quá 200 ký tự")]
        [Display(Name = "Mô tả tour")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Giá người lớn là bắt buộc")]
        [Range(0, 100000000, ErrorMessage = "Giá người lớn không hợp lệ")]
        [Display(Name = "Giá người lớn")]
        public decimal AdultPrice { get; set; }

        [Required(ErrorMessage = "Giá trẻ em là bắt buộc")]
        [Range(0, 100000000, ErrorMessage = "Giá trẻ em không hợp lệ")]
        [Display(Name = "Giá trẻ em")]
        public decimal ChildPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập phương tiện di chuyển")]
        [MaxLength(200)]
        [Display(Name = "Phương tiện")]
        public string Transport { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập điểm đến")]
        [MaxLength(200)]
        [Display(Name = "Điểm đến")]
        public string Destination { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời gian tour (VD: 3 ngày 2 đêm)")]
        [MaxLength(200)]
        [Display(Name = "Thời gian tour (VD: 3 ngày 2 đêm)")]
        public string Duration { get; set; }

        [Required(ErrorMessage = "Vui lòng thêm lịch trình")]
        [MaxLength(2000)]
        [Display(Name = "Lịch trình(Nhập theo mẫu)")]
        public string Itinerary { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại tour")]
        [Display(Name = "Loại tour")]
        public int CategoryId { get; set; }
        // Bổ sung các navigation cần thiết
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Image> Images { get; set; } = new List<Image>();
        public ICollection<TourSchedule> Schedules { get; set; } = new List<TourSchedule>();

    }
}
