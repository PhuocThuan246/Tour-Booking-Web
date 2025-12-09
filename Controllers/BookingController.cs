using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourBookingWeb.Data;
using TourBookingWeb.Models.ViewModels;
using NLog;
using TourBookingWeb.Models;

namespace TourBookingWeb.Controllers
{
    public class BookingController : Controller
    {
        private readonly TourDBContext _ctx;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public BookingController(TourDBContext ctx)
        {
            _ctx = ctx;
        }

        [HttpGet]
        public IActionResult Create(int tourId, string departureDate)
        {
            if (!DateTime.TryParseExact(departureDate, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                TempData["Error"] = "Không thể đọc ngày khởi hành.";
                return RedirectToAction("Details", "Tour", new { id = tourId });
            }

            var tour = _ctx.Tours
                .Include(t => t.Images)
                .FirstOrDefault(t => t.TourId == tourId);

            if (tour == null)
            {
                logger.Warn("Không tìm thấy tour với ID = {TourId}", tourId);
                return NotFound();
            }

            logger.Info("Người dùng mở trang đặt tour: {Title} – Ngày đi: {Date}", tour.Title, parsedDate.ToString("dd/MM/yyyy"));

            ViewBag.AdultPrice = tour.AdultPrice;
            ViewBag.ChildPrice = tour.ChildPrice;

            var schedules = _ctx.TourSchedules
                .Where(s => s.TourId == tourId && s.RemainingSlot > 0)
                .OrderBy(s => s.DepartureDate)
                .ToList();

            ViewBag.Schedules = schedules;

            var vm = new BookingViewModel
            {
                TourId = tourId,
                DepartureDate = parsedDate.ToString("dd/MM/yyyy"),
                AdultPrice = tour.AdultPrice,
                ChildPrice = tour.ChildPrice,
                Tour = tour
            };

            return View("Booking", vm); // đúng kiểu ViewModel

        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BookingViewModel model)
        {
            if (!DateTime.TryParseExact(model.DepartureDate, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime parsedDepartureDate))
            {
                ModelState.AddModelError("DepartureDate", "Ngày khởi hành không hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                var tour = _ctx.Tours.Include(t => t.Images).FirstOrDefault(t => t.TourId == model.TourId);
                model.Tour = tour;
                model.AdultPrice = tour?.AdultPrice ?? 0;
                model.ChildPrice = tour?.ChildPrice ?? 0;

                ViewBag.Schedules = _ctx.TourSchedules
                    .Where(s => s.TourId == model.TourId && s.RemainingSlot > 0)
                    .OrderBy(s => s.DepartureDate)
                    .ToList();

                return View("Booking", model);
            }

            var tourData = _ctx.Tours.FirstOrDefault(t => t.TourId == model.TourId);
            if (tourData == null)
            {
                logger.Error("Không tìm thấy tour khi đặt: ID = {TourId}", model.TourId);
                return NotFound();
            }

            var schedule = _ctx.TourSchedules.FirstOrDefault(s =>
                s.TourId == model.TourId && s.DepartureDate.Date == parsedDepartureDate.Date);

            if (schedule == null)
            {
                logger.Warn("Không tìm thấy lịch khởi hành {Date} cho TourID = {TourId}", parsedDepartureDate.ToString("dd/MM/yyyy"), model.TourId);
                TempData["Error"] = "Không tìm thấy lịch khởi hành.";
                return RedirectToAction("Index", "Tour");
            }

            int totalBooked = model.AdultQuantity.Value + model.ChildQuantity;
            if (schedule.RemainingSlot < totalBooked)
            {
                logger.Warn("Không đủ chỗ: Đặt {Total}, còn lại {Available} – TourID = {TourId}", totalBooked, schedule.RemainingSlot, model.TourId);
                TempData["Error"] = "Không đủ chỗ còn lại.";
                return RedirectToAction("Create", new { tourId = model.TourId, departureDate = parsedDepartureDate.ToString("dd/MM/yyyy") });
            }

            var user = _ctx.Users.FirstOrDefault(u => u.Email == model.Email);
            int userId;
            if (user != null)
            {
                userId = user.UserId;
            }
            else
            {
                var newUser = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone,
                    Address = model.Address,
                    Role = "Guest",
                    Password = ""
                };
                _ctx.Users.Add(newUser);
                _ctx.SaveChanges();
                userId = newUser.UserId;
            }

            decimal total = (model.AdultQuantity.Value * tourData.AdultPrice) + (model.ChildQuantity * tourData.ChildPrice);

            var booking = new Booking
            {
                BookingDate = DateTime.Now,
                TourId = model.TourId,
                TourScheduleId = schedule.TourScheduleId,
                DepartureDate = parsedDepartureDate,
                AdultQuantity = model.AdultQuantity.Value,
                ChildQuantity = model.ChildQuantity,
                TotalPrice = total,
                SpecialRequest = model.SpecialRequest,
                UserId = userId,
                PaymentMethod = model.PaymentMethod
            };

            _ctx.Bookings.Add(booking);
            schedule.RemainingSlot -= totalBooked;
            _ctx.SaveChanges();

            logger.Info("Đặt tour thành công – TourID: {TourId}, Tên tour: {TourTitle}, Ngày đi: {DepartureDate}, Người đặt: {Name}, Email: {Email}, Số điện thoại: {Phone}, Địa chỉ: {Address}, Người lớn: {Adults}, Trẻ em: {Children}, Tổng tiền: {Total} đ, Thanh toán: {Payment}",
                model.TourId, tourData.Title, parsedDepartureDate.ToString("dd/MM/yyyy"),
                model.FullName, model.Email, model.Phone, model.Address,
                model.AdultQuantity, model.ChildQuantity, total.ToString("n0"), model.PaymentMethod);

            TempData["Success"] = "Đặt tour thành công!";
            return RedirectToAction("Index", "Tour");
        }


    }
}
