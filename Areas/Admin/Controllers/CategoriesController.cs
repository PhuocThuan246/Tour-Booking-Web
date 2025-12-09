// Areas/Admin/Controllers/CategoriesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourBookingWeb.Data;
using TourBookingWeb.Models;
using Microsoft.Extensions.Logging;
using NLog;
using Microsoft.AspNetCore.Authorization;

namespace TourBookingWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class CategoriesController : Controller
    {
        private readonly TourDBContext _context;
        private static Logger logger = LogManager.GetCurrentClassLogger(); // Thêm logger
        public CategoriesController(TourDBContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SearchJson(string? keyword, int page = 1, int pageSize = 10)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(c => c.CategoryName.Contains(keyword));

            int total = query.Count();
            var data = query
                .OrderBy(c => c.CategoryId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Json(new
            {
                data = data.Select(c => new {
                    c.CategoryId,
                    c.CategoryName
                }),
                currentPage = page,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                _context.SaveChanges();
                logger.Info($"Đã thêm loại tour mới: {category.CategoryName}");
                TempData["Success"] = "Thêm loại tour thành công.";
                return RedirectToAction(nameof(Index));
            }
            logger.Warn("Dữ liệu loại tour không hợp lệ khi thêm mới");
            return View(category);
        }

        // GET: Admin/Categories/Edit/5
        public IActionResult Edit(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
            {
                logger.Warn($"Không tìm thấy loại tour ID {id} để chỉnh sửa");
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Update(category);
                _context.SaveChanges();
                logger.Info($"Đã cập nhật loại tour ID {category.CategoryId}");
                TempData["Success"] = "Cập nhật loại tour thành công.";
                return RedirectToAction(nameof(Index));
            }
            logger.Warn("Dữ liệu loại tour không hợp lệ khi chỉnh sửa");
            return View(category);
        }

        // GET: Admin/Categories/Delete/5
        public IActionResult Delete(int id)
        {
            bool isUsed = _context.Tours.Any(t => t.CategoryId == id);

            if (isUsed)
            {
                logger.Warn($"Không thể xóa loại tour ID {id} vì đang được sử dụng");
                TempData["Error"] = "Không thể xóa vì đã có tour sử dụng loại này.";
                return RedirectToAction("Index");
            }

            var category = _context.Categories.Find(id);
            if (category == null)
            {
                logger.Warn($"Không tìm thấy loại tour ID {id} để xóa");
                return NotFound();
            }

            _context.Categories.Remove(category);
            _context.SaveChanges();

            logger.Info($"Đã xóa loại tour ID {id}");
            TempData["Success"] = "Xóa loại tour thành công.";
            return RedirectToAction("Index");
        }




    }
}
    
