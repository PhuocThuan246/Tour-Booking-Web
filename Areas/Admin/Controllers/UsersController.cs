using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;
using TourBookingWeb.Data;
using TourBookingWeb.Models;

namespace TourBookingWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class UsersController : Controller
    {
        private readonly TourDBContext _ctx;

        private readonly UserManager<IdentityUser> _userManager;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public UsersController(TourDBContext ctx, UserManager<IdentityUser> userManager)
        {
            _ctx = ctx;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? keyword, int page = 1)
        {
            int pageSize = 10;

            // Bỏ admin
            var query = _ctx.Users.Where(u => u.Role != "admin");

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(keyword) ||
                    u.Email.ToLower().Contains(keyword) ||
                    u.Phone.Contains(keyword) ||
                    u.Address.ToLower().Contains(keyword));
            }

            var totalUsers = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Keyword = keyword;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_UserListPartial", users);

            return View(users);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _ctx.Users
                .Include(u => u.Bookings)
                .ThenInclude(b => b.Tour)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                logger.Warn("Không tìm thấy người dùng với ID: {Id}", id);
                return NotFound();
            }

            logger.Info("Admin xem chi tiết người dùng ID: {Id}", id);
            return View(user);
        }


        public async Task<IActionResult> Delete(int id)
        {
            var user = await _ctx.Users.FindAsync(id);
            if (user == null)
            {
                logger.Error("Không tìm thấy người dùng để xoá. ID = {Id}", id);
                return NotFound();
            }

            // Tìm trong bảng Identity (AspNetUsers) theo email
            var identityUser = await _userManager.FindByEmailAsync(user.Email);
            if (identityUser != null)
            {
                // Nếu có, tiến hành xóa khỏi hệ thống xác thực
                var identityResult = await _userManager.DeleteAsync(identityUser);
                if (!identityResult.Succeeded)
                {
                    logger.Error("Không thể xoá tài khoản xác thực của người dùng: {Email}", user.Email);
                    TempData["Error"] = "Không thể xoá tài khoản xác thực (Identity).";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Xoá khỏi bảng Users (dù là khách vãng lai hay không)
            _ctx.Users.Remove(user);
            await _ctx.SaveChangesAsync();
            logger.Warn("Admin xoá người dùng ID: {Id}, Email: {Email}", user.UserId, user.Email);
            TempData["Success"] = "Đã xoá người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

    }
}
