
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourBookingWeb.Data;
using TourBookingWeb.Models;
using Microsoft.Extensions.Logging;
using X.PagedList;
using X.PagedList.EF;

namespace TourBookingWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class BookingsController : Controller
    {
        private readonly TourDBContext _ctx;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(TourDBContext ctx, ILogger<BookingsController> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string name, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var query = _ctx.Bookings
                .Include(b => b.User)
                .Include(b => b.Tour)
                .Include(b => b.TourSchedule)
                .AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(b => b.User.FullName.Contains(name));
                _logger.LogInformation("Admin lọc booking theo tên người đặt: {Name}", name);
            }

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ToPagedListAsync(pageNumber, pageSize);

            ViewBag.Name = name;

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var booking = _ctx.Bookings
                .Include(b => b.TourSchedule)
                .Include(b => b.User)
                .FirstOrDefault(b => b.BookingId == id);

            if (booking == null)
            {
                _logger.LogError("Không tìm thấy booking để xoá. ID = {Id}", id);
                return NotFound();
            }

            if (booking.TourSchedule != null)
            {
                int totalGuests = booking.AdultQuantity + booking.ChildQuantity;
                booking.TourSchedule.RemainingSlot += totalGuests;
            }

            _ctx.Bookings.Remove(booking);
            _ctx.SaveChanges();

            _logger.LogWarning("Admin xoá booking #{BookingId} – Người đặt: {Name}", booking.BookingId, booking.User?.FullName);

            TempData["Success"] = "Xóa booking và cập nhật chỗ còn thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var booking = _ctx.Bookings
                .Include(b => b.User)
                .Include(b => b.Tour)
                .FirstOrDefault(b => b.BookingId == id);

            if (booking == null)
            {
                _logger.LogError("Không tìm thấy booking để chỉnh sửa. ID = {Id}", id);
                return NotFound();
            }

            ViewBag.Users = _ctx.Users.ToList();
            ViewBag.Tours = _ctx.Tours.ToList();
            ViewBag.Schedules = _ctx.TourSchedules
                .Where(s => s.TourId == booking.TourId)
                .OrderBy(s => s.DepartureDate)
                .ToList();

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Booking booking, string UserFullName, string UserEmail)
        {
            if (id != booking.BookingId) return NotFound();

            var existing = _ctx.Bookings
                .Include(b => b.User)
                .Include(b => b.TourSchedule)
                .FirstOrDefault(b => b.BookingId == id);
            if (existing == null)
            {
                _logger.LogError("Booking cần chỉnh sửa không tồn tại. ID = {Id}", id);
                return NotFound();
            }

            int oldTotalGuests = existing.AdultQuantity + existing.ChildQuantity;
            int newTotalGuests = booking.AdultQuantity + booking.ChildQuantity;

            if (existing.TourScheduleId != booking.TourScheduleId)
            {
                var oldSchedule = _ctx.TourSchedules.FirstOrDefault(s => s.TourScheduleId == existing.TourScheduleId);
                if (oldSchedule != null)
                    oldSchedule.RemainingSlot += oldTotalGuests;

                var newSchedule = _ctx.TourSchedules.FirstOrDefault(s => s.TourScheduleId == booking.TourScheduleId);
                if (newSchedule != null)
                {
                    newSchedule.RemainingSlot -= newTotalGuests;
                    existing.DepartureDate = newSchedule.DepartureDate;
                }

                existing.TourScheduleId = booking.TourScheduleId;
            }
            else
            {
                int diff = newTotalGuests - oldTotalGuests;
                var schedule = _ctx.TourSchedules.FirstOrDefault(s => s.TourScheduleId == existing.TourScheduleId);
                if (schedule != null)
                {
                    schedule.RemainingSlot -= diff;
                    existing.DepartureDate = schedule.DepartureDate;
                }
            }

            existing.AdultQuantity = booking.AdultQuantity;
            existing.ChildQuantity = booking.ChildQuantity;
            existing.SpecialRequest = booking.SpecialRequest;
            existing.BookingDate = booking.BookingDate;
            existing.PaymentMethod = booking.PaymentMethod;

            if (existing.User != null)
            {
                existing.User.FullName = UserFullName;
                existing.User.Email = UserEmail;
            }

            var tour = _ctx.Tours.FirstOrDefault(t => t.TourId == existing.TourId);
            if (tour != null)
            {
                existing.TotalPrice = (booking.AdultQuantity * tour.AdultPrice) +
                                      (booking.ChildQuantity * tour.ChildPrice);
            }

            _ctx.SaveChanges();

            _logger.LogInformation("Admin đã chỉnh sửa booking #{BookingId} – Người đặt: {Name}", existing.BookingId, existing.User?.FullName);

            TempData["Success"] = "Cập nhật booking & chỗ còn thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> SearchJson(string? name, string? email, string? tour, int page = 1, int pageSize = 8, string? sortOrder = "id_desc")
        {
            var query = _ctx.Bookings
                .Include(b => b.User)
                .Include(b => b.Tour)
                .Include(b => b.TourSchedule)
                .AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(b => b.User.FullName.Contains(name));
            if (!string.IsNullOrEmpty(email))
                query = query.Where(b => b.User.Email.Contains(email));
            if (!string.IsNullOrEmpty(tour))
                query = query.Where(b => b.Tour.Title.Contains(tour));

            // Sắp xếp theo sortOrder
            query = sortOrder switch
            {
                "id" => query.OrderBy(b => b.BookingId),
                "id_desc" => query.OrderByDescending(b => b.BookingId),
                _ => query.OrderByDescending(b => b.BookingId)
            };

            int total = await query.CountAsync();
            var bookings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = bookings.Select(b => new
            {
                bookingId = b.BookingId,
                fullName = b.User?.FullName,
                email = b.User?.Email,
                tourTitle = b.Tour?.Title,
                departureDate = b.TourSchedule != null ? b.TourSchedule.DepartureDate.ToString("dd/MM/yyyy") : "",
                returnDate = b.TourSchedule != null ? b.TourSchedule.ReturnDate.ToString("dd/MM/yyyy") : "",
                bookingDate = b.BookingDate.ToString("dd/MM/yyyy"),
                adultQuantity = b.AdultQuantity,
                childQuantity = b.ChildQuantity,
                totalPrice = string.Format("{0:N0} đ", b.TotalPrice),
                paymentMethod = b.PaymentMethod
            });

            return Json(new
            {
                data,
                currentPage = page,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }


    }

}
