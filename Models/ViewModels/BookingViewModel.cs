using System;
using System.ComponentModel.DataAnnotations;

namespace TourBookingWeb.Models.ViewModels
{
    public class BookingViewModel
    {
        public int TourId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số người lớn")]
        [Range(1, 100, ErrorMessage = "Số người lớn phải từ 1 đến 100")]
        [Display(Name = "Số người lớn")]
        public int? AdultQuantity { get; set; }



        public int ChildQuantity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string Address { get; set; }

        public string? SpecialRequest { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày khởi hành.")]
        public string DepartureDate { get; set; } 

        public decimal AdultPrice { get; set; }
        public decimal ChildPrice { get; set; }
        public Tour? Tour { get; set; }
    }

}
