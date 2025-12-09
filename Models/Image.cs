using System.ComponentModel.DataAnnotations;

namespace TourBookingWeb.Models
{
    public class Image
    {
        [Key]
        public int ImageId { get; set; }

        [MaxLength(200)]
        public string Url { get; set; }             // Đường dẫn ảnh

        [MaxLength(200)]
        public string Description { get; set; }     // Mô tả (chú thích ảnh)

        public int TourId { get; set; }             // FK tới Tour
        public int? DayNumber { get; set; }         // Ngày trong lịch trình (nếu có)
        public Tour Tour { get; set; }

    }
}
