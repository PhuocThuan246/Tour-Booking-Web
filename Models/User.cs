using System.ComponentModel.DataAnnotations;

namespace TourBookingWeb.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        [MaxLength(200)]
        public string Email { get; set; }
        [MaxLength(200)]
        public string FullName { get; set; }
        [MaxLength(200)]
        public string Phone { get; set; }
        [MaxLength(200)]
        public string Address { get; set; }
        [MaxLength(200)]
        public string Password { get; set; }     
        [MaxLength(200)]
        public string Role { get; set; }
        public ICollection<Booking> Bookings { get; set; }

    }
}
