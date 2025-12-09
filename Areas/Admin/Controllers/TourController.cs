using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TourBookingWeb.Data;
using TourBookingWeb.Models;
using NLog;
using X.PagedList;
using X.PagedList.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
namespace TourBookingWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class TourController : Controller
    {
        private readonly TourDBContext _ctx;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public TourController(TourDBContext ctx) => _ctx = ctx;

        public async Task<IActionResult> Index()
        {
            // Lấy ảnh đại diện
            var coverImages = _ctx.Images
                .Where(i => i.DayNumber == 0)
                .ToList()
                .GroupBy(i => i.TourId)
                .ToDictionary(g => g.Key, g => g.First().Url);
            ViewBag.CoverImages = coverImages;

            // Lấy danh sách loại tour
            ViewBag.Categories = await _ctx.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                }).ToListAsync();

            // Lấy danh sách điểm đến
            ViewBag.Destinations = await _ctx.Tours
                .Select(t => t.Destination)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            // Không cần truyền model — dữ liệu sẽ được fetch qua AJAX
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> SearchJson(string? keyword, int? categoryId, DateTime? departureDate, string? destination, string? sortOrder, int page = 1, int pageSize = 10)
        {
            var query = _ctx.Tours
                            .Include(t => t.Schedules)
                            .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(t => t.Title.Contains(keyword));

            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(t => t.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(destination))
                query = query.Where(t => t.Destination.Contains(destination));

            if (departureDate.HasValue)
                query = query.Where(t => t.Schedules.Any(s => s.DepartureDate.Date == departureDate.Value.Date));

            // Sắp xếp
            query = sortOrder switch
            {
                "id_desc" => query.OrderByDescending(t => t.TourId),
                "title" => query.OrderBy(t => t.Title),
                "title_desc" => query.OrderByDescending(t => t.Title),
                "price" => query.OrderBy(t => t.AdultPrice),
                "price_desc" => query.OrderByDescending(t => t.AdultPrice),
                _ => query.OrderBy(t => t.TourId)
            };

            int total = await query.CountAsync();
            var pageData = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var coverImages = _ctx.Images
                .Where(i => i.DayNumber == 0)
                .ToList()
                .GroupBy(i => i.TourId)
                .ToDictionary(g => g.Key, g => g.First().Url);

            var result = pageData.Select(t => {
                var firstSchedule = t.Schedules
                    .Where(s => !s.IsDeleted) // chỉ lấy lịch trình còn hiệu lực
                    .OrderBy(s => s.DepartureDate)
                    .FirstOrDefault();

                return new
                {
                    t.TourId,
                    t.Title,
                    t.Destination,
                    t.Duration,
                    t.Capacity,
                    t.Transport,
                    t.AdultPrice,
                    t.ChildPrice,
                    DepartureDate = firstSchedule?.DepartureDate.ToString("dd/MM/yyyy") ?? "Chưa có",
                    RemainingSlot = firstSchedule?.RemainingSlot ?? 0,
                    ImageUrl = coverImages.ContainsKey(t.TourId) ? coverImages[t.TourId] : "assets/images/no-image.jpg"
                };
            });

            return Json(new
            {
                data = result,
                currentPage = page,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }


        // Hiển thị form
        public IActionResult Create()
        {
            ViewBag.CategoryList = new SelectList(_ctx.Categories.ToList(), "CategoryId", "CategoryName");
            return View();
        }

        // Xử lý POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            Tour model,
            IFormFile CoverImage,
            int DayCount,
            List<IFormFile> DayImages,
            List<DateTime> departDate,
            List<DateTime> returnDate,
            List<int> seats)
        {
            ViewBag.CategoryList = new SelectList(_ctx.Categories.ToList(), "CategoryId", "CategoryName");

            if (CoverImage == null)
            {
                logger.Warn("Không chọn ảnh đại diện khi thêm tour");
                ModelState.AddModelError("CoverImage", "Vui lòng chọn ảnh đại diện.");
            }

            if (!ModelState.IsValid)
            {
                logger.Warn("Dữ liệu tour không hợp lệ khi thêm mới");
                foreach (var kv in ModelState)
                {
                    foreach (var err in kv.Value.Errors)
                        logger.Warn($"Field {kv.Key}: {err.ErrorMessage}");
                }
                return View(model);
            }


            _ctx.Tours.Add(model);
            _ctx.SaveChanges();

            int tourId = model.TourId;
            logger.Info($"Đã thêm tour mới ID {tourId}: {model.Title}");

            // Ảnh đại diện
            string coverUrl = SaveImage(CoverImage, tourId);
            _ctx.Images.Add(new Image
            {
                TourId = tourId,
                Url = coverUrl,
                Description = "Ảnh đại diện",
                DayNumber = 0
            });

            // Ảnh từng ngày
            for (int i = 0; i < DayCount; i++)
            {
                if (i < DayImages.Count && DayImages[i] != null)
                {
                    string url = SaveImage(DayImages[i], tourId);
                    _ctx.Images.Add(new Image
                    {
                        TourId = tourId,
                        Url = url,
                        Description = $"Ảnh minh họa ngày {i + 1}",
                        DayNumber = i + 1
                    });
                }
            }

            // Lưu lịch khởi hành sang TourSchedule
            for (int i = 0; i < departDate.Count; i++)
            {
                if (departDate[i] != default && returnDate[i] != default && seats[i] > 0)
                {
                    _ctx.TourSchedules.Add(new TourSchedule
                    {
                        TourId = tourId,
                        DepartureDate = departDate[i],
                        ReturnDate = returnDate[i],
                        RemainingSlot = seats[i]
                    });
                }
            }

            _ctx.SaveChanges();
            TempData["Success"] = "Đã thêm tour mới thành công.";
            return RedirectToAction(nameof(Index));
        }

        private string SaveImage(IFormFile file, int tourId)
        {
            if (file == null || file.Length == 0)
                return null;

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "tours", tourId.ToString());
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return Path.Combine("img", "tours", tourId.ToString(), fileName).Replace("\\", "/");
        }

        // Hiển thị form sửa
        public IActionResult Edit(int id)
        {
            var tour = _ctx.Tours
                .Include(t => t.Schedules)
                .FirstOrDefault(t => t.TourId == id);

            if (tour == null)
            {
                TempData["Error"] = "Không tìm thấy tour cần sửa.";
                return RedirectToAction(nameof(Index));
            }

            // Convert lịch trình về dạng đơn giản ViewModel để tránh vòng lặp
            ViewBag.TourSchedules = tour.Schedules.Select(s => new {
                scheduleId = s.TourScheduleId,
                departureDate = s.DepartureDate.ToString("yyyy-MM-dd"),
                returnDate = s.ReturnDate.ToString("yyyy-MM-dd"),
                remainingSlot = s.RemainingSlot,
                isDeleted = s.IsDeleted
            }).ToList();



            ViewBag.CategoryList = new SelectList(_ctx.Categories.ToList(), "CategoryId", "CategoryName", tour.CategoryId);
            return View(tour);
        }


        // Xử lý POST Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
           Tour model,
           IFormFile? CoverImage,
           int DayCount,
           List<IFormFile>? DayImages,
           List<DateTime> departDate,
           List<DateTime> returnDate,
           List<int> seats)
        {
            ViewBag.CategoryList = new SelectList(_ctx.Categories.ToList(), "CategoryId", "CategoryName", model.CategoryId);

            if (!ModelState.IsValid)
            {
                logger.Warn("Dữ liệu tour không hợp lệ khi cập nhật.");
                return View(model);
            }

            var existing = _ctx.Tours.Include(t => t.Schedules).FirstOrDefault(t => t.TourId == model.TourId);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy tour để cập nhật.";
                return RedirectToAction(nameof(Index));
            }

            // Cập nhật thông tin tour
            existing.Title = model.Title;
            existing.Description = model.Description;
            existing.Itinerary = model.Itinerary;
            existing.AdultPrice = model.AdultPrice;
            existing.ChildPrice = model.ChildPrice;
            existing.Capacity = model.Capacity;
            existing.Duration = model.Duration;
            existing.Transport = model.Transport;
            existing.Destination = model.Destination;
            existing.CategoryId = model.CategoryId;

            // Cập nhật ảnh đại diện (nếu có)
            if (CoverImage != null)
            {
                var oldCover = _ctx.Images.FirstOrDefault(i => i.TourId == model.TourId && i.DayNumber == 0);
                if (oldCover != null)
                {
                    DeleteImageFile(oldCover.Url);
                    _ctx.Images.Remove(oldCover);
                }

                var newCoverUrl = SaveImage(CoverImage, model.TourId);
                _ctx.Images.Add(new Image
                {
                    TourId = model.TourId,
                    Url = newCoverUrl,
                    Description = "Ảnh đại diện",
                    DayNumber = 0
                });
            }

            // Cập nhật ảnh từng ngày (nếu có)
            if (DayImages != null && DayImages.Count > 0 && DayImages.Any(f => f.Length > 0))
            {
                var oldDayImgs = _ctx.Images.Where(i => i.TourId == model.TourId && i.DayNumber > 0).ToList();
                foreach (var img in oldDayImgs)
                {
                    DeleteImageFile(img.Url);
                }
                _ctx.Images.RemoveRange(oldDayImgs);

                for (int i = 0; i < DayCount; i++)
                {
                    if (i < DayImages.Count && DayImages[i] != null)
                    {
                        var url = SaveImage(DayImages[i], model.TourId);
                        _ctx.Images.Add(new Image
                        {
                            TourId = model.TourId,
                            Url = url,
                            Description = $"Ảnh minh họa ngày {i + 1}",
                            DayNumber = i + 1
                        });
                    }
                }
            }

            // Thêm mới các lịch trình (không xóa toàn bộ)
            for (int i = 0; i < departDate.Count; i++)
            {
                if (departDate[i] != default && returnDate[i] != default && seats[i] > 0)
                {
                    int scheduleId = int.TryParse(Request.Form["scheduleId"][i], out var sid) ? sid : 0;
                    bool isDeleted = Request.Form["isDeleted"][i] == "true";

                    if (scheduleId > 0)
                    {
                        var existingSchedule = _ctx.TourSchedules.FirstOrDefault(s => s.TourScheduleId == scheduleId);
                        if (existingSchedule != null)
                        {
                            if (isDeleted)
                            {
                                existingSchedule.IsDeleted = true;
                            }
                            else
                            {
                                existingSchedule.DepartureDate = departDate[i];
                                existingSchedule.ReturnDate = returnDate[i];
                                existingSchedule.RemainingSlot = seats[i];
                                existingSchedule.IsDeleted = false;
                            }
                        }
                    }
                    else if (!isDeleted)
                    {
                        _ctx.TourSchedules.Add(new TourSchedule
                        {
                            TourId = model.TourId,
                            DepartureDate = departDate[i],
                            ReturnDate = returnDate[i],
                            RemainingSlot = seats[i],
                            IsDeleted = false
                        });
                    }
                }
            }


            _ctx.SaveChanges();
            logger.Info($"Cập nhật tour ID {model.TourId}: {model.Title}");
            TempData["Success"] = "Đã cập nhật tour thành công.";
            return RedirectToAction(nameof(Index));
        }



        private void DeleteImageFile(string relativePath)
        {
            try
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.TrimStart('~', '/').Replace("/", "\\"));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Không thể xóa file ảnh");
            }
        }

        // Xử lý XÓA
        public IActionResult Delete(int id)
        {
            var tour = _ctx.Tours.Find(id);
            if (tour == null)
            {
                TempData["Error"] = "Không tìm thấy tour để xóa.";
                return RedirectToAction(nameof(Index));
            }
            // Xóa các booking liên quan đến các TourSchedule
            var scheduleIds = _ctx.TourSchedules
                                  .Where(s => s.TourId == id)
                                  .Select(s => s.TourScheduleId)
                                  .ToList();

            var bookingsToDelete = _ctx.Bookings
                .Where(b => b.TourScheduleId != null && scheduleIds.Contains(b.TourScheduleId.Value))
                .ToList();

            _ctx.Bookings.RemoveRange(bookingsToDelete);


            // Xóa các lịch khởi hành (TourSchedules)
            var schedules = _ctx.TourSchedules.Where(s => s.TourId == id).ToList();
            _ctx.TourSchedules.RemoveRange(schedules);

            // Xóa các ảnh trong database
            var images = _ctx.Images.Where(i => i.TourId == id).ToList();
            _ctx.Images.RemoveRange(images);

            // Xóa tour
            _ctx.Tours.Remove(tour);

            // Xóa thư mục ảnh vật lý
            DeleteTourFolder(id);

            _ctx.SaveChanges();

            logger.Info($"Đã xóa tour ID {id}: {tour.Title}, ảnh và toàn bộ lịch khởi hành");
            TempData["Success"] = "Đã xóa tour thành công.";
            return RedirectToAction(nameof(Index));
        }


        private void DeleteTourFolder(int tourId)
        {
            try
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "tours", tourId.ToString());
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, recursive: true); // recursive: true để xóa luôn toàn bộ file con
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Không thể xóa thư mục ảnh cho tour {tourId}");
            }
        }
        [HttpGet]
        public IActionResult Booking(int id)
        {
            var tour = _ctx.Tours.FirstOrDefault(t => t.TourId == id);
            if (tour == null)
            {
                return NotFound();
            }

            return View(tour); 
        }

  

    }
}
