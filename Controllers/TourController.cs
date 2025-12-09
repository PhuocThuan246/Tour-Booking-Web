using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourBookingWeb.Data;
using NLog;
using X.PagedList;
using X.PagedList.EF;
using TourBookingWeb.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TourBookingWeb.Controllers
{
    public class TourController : Controller
    {
        private readonly TourDBContext _ctx;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public TourController(TourDBContext ctx)
        {
            _ctx = ctx;
        }
        public async Task<IActionResult> Index(
                    string? keyword, int? categoryId,
                    string? destination, DateTime? departureDate,
                    int? page)
        {
            int pageNumber = page ?? 1;
            int pageSize = 9;

            /* ---- truy vấn gốc ---- */
            var query = _ctx.Tours
                            .Include(t => t.Schedules)
                            .AsQueryable();

            /* ---- áp dụng filter ---- */
            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(t => t.Title.Contains(keyword));

            if (categoryId is > 0)
                query = query.Where(t => t.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(destination))
                query = query.Where(t => t.Destination.Contains(destination));

            if (departureDate.HasValue)
                query = query.Where(t => t.Schedules
                                    .Any(s => s.DepartureDate.Date ==
                                              departureDate.Value.Date));

            /* ---- phân trang ---- */
            var paged = await query
                            .OrderBy(t => t.Title)
                            .ToPagedListAsync(pageNumber, pageSize);

            /* ---- dữ liệu cho dropdown ---- */
            ViewBag.Categories = await _ctx.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    }).ToListAsync();

            ViewBag.Destinations = await _ctx.Tours
                    .Select(t => t.Destination)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

            /* để đổ ngược lại ô tìm kiếm */
            ViewBag.Keyword = keyword;
            ViewBag.CategoryId = categoryId ?? 0;
            ViewBag.Destination = destination;
            ViewBag.DepartureDate = departureDate?.ToString("yyyy-MM-dd");

            return View(paged);
        }

        // -----------  AJAX -------------
        [HttpGet]
        public async Task<IActionResult> SearchJson(
            string? keyword, int? categoryId,
            string? destination, DateTime? departureDate,
            int page = 1, int pageSize = 9)
        {
            var q = _ctx.Tours
                        .Include(t => t.Schedules)   // cần để lọc + lấy ngày
                        .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(t => t.Title.Contains(keyword));

            if (categoryId is > 0)
                q = q.Where(t => t.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(destination))
                q = q.Where(t => t.Destination.Contains(destination));

            if (departureDate.HasValue)
                q = q.Where(t => t.Schedules
                                .Any(s => s.DepartureDate.Date ==
                                          departureDate.Value.Date));

            int total = await q.CountAsync();

            var list = await q.OrderBy(t => t.Title)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .Select(t => new
                              {
                                  t.TourId,
                                  t.Title,
                                  t.Destination,
                                  t.Duration,
                                  t.Capacity,
                                  t.AdultPrice,
                                  /* 3 ngày khởi hành đầu tiên */
                                  Departures = t.Schedules
                                                .Where(s => !s.IsDeleted)
                                                .OrderBy(s => s.DepartureDate)
                                                .Take(3)
                                                .Select(s => s.DepartureDate.ToString("dd/MM/yyyy"))
                                                .ToList(),

                                  ImageUrl = _ctx.Images
                                          .Where(i => i.TourId == t.TourId &&
                                                      i.DayNumber == 0)
                                          .Select(i => i.Url)
                                          .FirstOrDefault() ??
                                          "assets/images/no-image.jpg"
                              })
                              .ToListAsync();

            return Json(new
            {
                data = list,
                currentPage = page,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }




        public async Task<IActionResult> Details(int id)
        {
            var tour = await _ctx.Tours
                .Include(t => t.Schedules.Where(s => !s.IsDeleted)) // chỉ lấy lịch trình chưa xóa
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (tour == null)
            {
                logger.Warn($"Không tìm thấy tour ID {id} khi xem chi tiết");
                return NotFound();
            }

            return View(tour);
        }


        [HttpPost]
        public IActionResult BookNow(int tourId, string departureDate)
        {
            if (DateTime.TryParseExact(departureDate, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                return RedirectToAction("Create", "Booking", new
                {
                    tourId = tourId,
                    departureDate = parsedDate.ToString("dd/MM/yyyy")
                });
            }

            TempData["Error"] = "Ngày khởi hành không hợp lệ.";
            return RedirectToAction("Details", new { id = tourId });
        }




    }
}
