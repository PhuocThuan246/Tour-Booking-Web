using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourBookingWeb.Data;
using TourBookingWeb.Models;

namespace TourBookingWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class ReportController : Controller
    {
        private readonly TourDBContext _ctx;

        public ReportController(TourDBContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<IActionResult> Index(int? month, int? year)
        {
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;

            bool hasYear = year.HasValue;
            bool hasMonth = month.HasValue;

            IQueryable<Booking> bookings = _ctx.Bookings;

            if (hasYear)
            {
                var filteredBookings = bookings.Where(b => b.DepartureDate.Year == year.Value);
                if (hasMonth)
                    filteredBookings = filteredBookings.Where(b => b.DepartureDate.Month == month.Value);

                ViewBag.TotalBookings = await filteredBookings.CountAsync();
                ViewBag.TotalRevenue = await filteredBookings.SumAsync(b => (decimal?)b.TotalPrice) ?? 0;

                // THAY navigation bằng join để lấy thống kê theo loại tour (category)
                var regionStats = await (
                    from b in filteredBookings
                    join t in _ctx.Tours on b.TourId equals t.TourId
                    join c in _ctx.Categories on t.CategoryId equals c.CategoryId
                    group b by c.CategoryName into g
                    select new { Region = g.Key, Count = g.Count() }
                ).ToListAsync();

                var paymentStats = await filteredBookings
                    .GroupBy(b => b.PaymentMethod)
                    .Select(g => new { Method = g.Key, Count = g.Count() })
                    .ToListAsync();

                var revenueByMonth = await bookings
                    .Where(b => b.DepartureDate.Year == year.Value)
                    .GroupBy(b => b.DepartureDate.Month)
                    .Select(g => new { Month = g.Key, Revenue = g.Sum(b => b.TotalPrice) })
                    .ToListAsync();

                decimal[] monthlyRevenue = new decimal[12];
                foreach (var item in revenueByMonth)
                {
                    monthlyRevenue[item.Month - 1] = item.Revenue;
                }

                ViewBag.RegionChart = regionStats;
                ViewBag.PaymentChart = paymentStats;
                ViewBag.MonthlyRevenue = monthlyRevenue;
            }
            else
            {
                ViewBag.TotalBookings = 0;
                ViewBag.TotalRevenue = 0;
                ViewBag.RegionChart = new List<object>();
                ViewBag.PaymentChart = new List<object>();
                ViewBag.MonthlyRevenue = new decimal[12];
            }

            ViewBag.TotalTours = await _ctx.Tours.CountAsync();
            return View();
        }
    }
}
