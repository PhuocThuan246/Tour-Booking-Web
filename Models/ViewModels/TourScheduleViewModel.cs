namespace TourBookingWeb.Models.ViewModels
{
    public class TourScheduleViewModel
    {
        public DateTime DepartureDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public int RemainingSlot { get; set; }
    }
}
