using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TourBookingWeb.Data;
using TourBookingWeb.Services;
using TourBookingWeb.Models.ViewModels;
using TourBookingWeb.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using NLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace TourBookingWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly TourDBContext _ctx;
        private readonly IConfiguration _config;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, TourDBContext ctx, IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _ctx = ctx;
            _config = config;
        }


        // Đăng ký
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra email đã có trong Identity chưa
            var identityExists = await _userManager.FindByEmailAsync(model.Email);
            if (identityExists != null)
            {
                logger.Warn("Email đã tồn tại trong hệ thống Identity: {0}", model.Email);
                ModelState.AddModelError("Email", "Email đã được đăng ký tài khoản trước đó.");
                return View(model);
            }

            // Kiểm tra email đã tồn tại ở bảng Users → nếu có thì cập nhật role
            var existingUser = _ctx.Users.FirstOrDefault(u => u.Email == model.Email);
            if (existingUser != null && existingUser.Role == "Guest")
            {
                logger.Info("Cập nhật vai trò từ Guest sang user cho {0}", model.Email);
                existingUser.Role = "user";
                existingUser.FullName = model.FullName;
                existingUser.Phone = model.Phone;
                existingUser.Address = model.Address;
            }
            else if (existingUser == null)
            {
                _ctx.Users.Add(new User
                {
                    Email = model.Email,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Address = model.Address,
                    Role = "user",
                    Password = model.Password // (chỉ tạm nếu bạn đang dùng bảng Users riêng)
                });
            }

            // Thêm vào Identity (AspNetUsers)
            var identityUser = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(identityUser, model.Password);

            if (result.Succeeded)
            {
                // Tạo mã OTP 6 số
                var otp = new Random().Next(100000, 999999).ToString();

                // Lưu vào session để xác minh sau
                HttpContext.Session.SetString("otp", otp);
                HttpContext.Session.SetString("emailRegister", model.Email);
                HttpContext.Session.SetString("otp_time", DateTime.UtcNow.ToString());

                // Gửi mã OTP qua email
                EmailService.SendTokenEmail(model.Email, otp);

                // Lưu vào DB bảng Users
                await _ctx.SaveChangesAsync();
                logger.Info("Đăng ký thành công cho {0}, đã gửi mã OTP", model.Email);
                TempData["Success"] = "Đăng ký thành công! Mã xác thực đã được gửi qua email.";
                return RedirectToAction("VerifyEmailCode");
            }

            // Nếu có lỗi khi tạo tài khoản
            foreach (var error in result.Errors)
            {
                logger.Warn("Lỗi đăng ký: {0}", error.Description);
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult VerifyEmailCode()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmailCode(string otpInput)
        {
            var sessionOtp = HttpContext.Session.GetString("otp");
            var email = HttpContext.Session.GetString("emailRegister");

            if (otpInput == sessionOtp)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }

                TempData["Success"] = "Xác thực thành công. Bạn có thể đăng nhập.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Mã xác thực không chính xác");
            return View();
        }


        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailConfirmation(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Vui lòng nhập email");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản");
                return View();
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError("", "Tài khoản đã được xác nhận.");
                return View();
            }

            // Sinh mã OTP mới
            var otp = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("otp", otp);
            HttpContext.Session.SetString("emailRegister", email);

            // Gửi lại email chứa mã OTP
            EmailService.SendTokenEmail(email, otp);

            TempData["Success"] = "Mã xác thực đã được gửi lại qua email.";
            return RedirectToAction("VerifyEmailCode");
        }



        // Đăng nhập
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _signInManager.UserManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại");
                return View(model);
            }

            // Kiểm tra tài khoản đã xác thực email chưa (môi trường học tập không làm gắt)
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError("", "Tài khoản chưa được xác thực qua email.");
                return View(model);
            }

            // Nếu đã xác thực email, tiến hành đăng nhập
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
            {

                // 1. Đăng nhập bằng Identity Cookie để vào được khu vực Admin
                await _signInManager.SignInAsync(user, isPersistent: false);

                // 2. Tạo JWT token như cũ
                var token = await GenerateJwtToken(user);

                Response.Cookies.Append("jwt", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddHours(1)
                });

                TempData["Success"] = "Đăng nhập thành công!";
                // 3. Chuyển hướng theo role
                var roles = await _userManager.GetRolesAsync(user);
                logger.Warn(string.Join(",", roles));
                if (roles.Contains("admin"))
                    return RedirectToAction("Index", "Home", new { area = "Admin" });


                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Sai mật khẩu hoặc tài khoản không hợp lệ");
            return View(model);
        }




        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            // Lấy các role của user
            var userRoles = await _userManager.GetRolesAsync(user);

            // Tạo các claim cho role
            var roleClaims = userRoles.Select(role => new Claim(ClaimTypes.Role, role));

            // Các claim cơ bản
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Email, user.Email), // thêm dòng này
                new Claim(ClaimTypes.Email, user.Email),              // thêm dòng này
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            // Thêm claims cho Role
            claims.AddRange(roleClaims);

            // Tạo khóa và credentials
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Tạo token
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            // Trả chuỗi token
            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Xóa xác thực của Identity
            await _signInManager.SignOutAsync();

            // Xóa cookie JWT
            Response.Cookies.Delete("jwt");

            TempData["Success"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }


        // Quên mật khẩu
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Vui lòng nhập email");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                logger.Warn("Email không tồn tại trong hệ thống khi đặt lại mật khẩu: {0}", email);
                ModelState.AddModelError("", "Không tìm thấy tài khoản");
                return View();
            }

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("otp_reset", otp);
            HttpContext.Session.SetString("email_reset", email);
            HttpContext.Session.SetString("otp_reset_time", DateTime.UtcNow.ToString());

            EmailService.SendTokenEmail(email, otp);
            logger.Info("Đã gửi OTP đặt lại mật khẩu cho {0}", email);

            TempData["Success"] = "Mã xác thực đặt lại mật khẩu đã gửi qua email.";
            return RedirectToAction("VerifyResetCode");
        }


        [HttpGet]
        public IActionResult VerifyResetCode()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyResetCode(string otpInput)
        {
            var sessionOtp = HttpContext.Session.GetString("otp_reset");
            var timeStr = HttpContext.Session.GetString("otp_reset_time");

            if (sessionOtp != otpInput)
            {
                ModelState.AddModelError("", "Mã không đúng");
                return View();
            }

            if (DateTime.TryParse(timeStr, out var created) && (DateTime.UtcNow - created).TotalMinutes > 30)
            {
                ModelState.AddModelError("", "Mã đã hết hạn");
                return View();
            }

            TempData["ResetReady"] = true;
            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["ResetReady"] == null)
                return RedirectToAction("ForgotPassword");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string newPassword)
        {
            var email = HttpContext.Session.GetString("email_reset");
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                logger.Error("Không tìm thấy người dùng khi đặt lại mật khẩu: {0}", email);
                ModelState.AddModelError("", "Tài khoản không tồn tại");
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                logger.Info("Đặt lại mật khẩu thành công cho {0}", email);
                TempData["Success"] = "Đặt lại mật khẩu thành công!";
                HttpContext.Session.Remove("otp_reset");
                HttpContext.Session.Remove("email_reset");
                HttpContext.Session.Remove("otp_reset_time");
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                logger.Error("Lỗi đặt lại mật khẩu cho {0}: {1}", email, error.Description);
                ModelState.AddModelError("", error.Description);
            }

            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var user = _ctx.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                logger.Error("Không tìm thấy người dùng khi truy cập trang hồ sơ. Email: {0}", email);
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(User model)
        {
            var user = _ctx.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
            {
                logger.Error("Không tìm thấy người dùng để cập nhật hồ sơ. Email: {0}", model.Email);
                return NotFound();
            }

            user.FullName = model.FullName;
            user.Phone = model.Phone;
            user.Address = model.Address;

            _ctx.SaveChanges();
            logger.Info("Người dùng cập nhật hồ sơ thành công: {0}", model.Email);
            TempData["Success"] = "Đã cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

        // ---------- ĐƠN HÀNG CỦA TÔI ----------
        [HttpGet]
        [Authorize]
        public IActionResult MyOrders()
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (email == null) return RedirectToAction("Login");

            var user = _ctx.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return RedirectToAction("Login");

            var orders = _ctx.Bookings
                            .Include(b => b.Tour)
                            .ThenInclude(t => t.Images) //  thêm dòng này
                            .Include(b => b.TourSchedule)
                            .Where(b => b.UserId == user.UserId)
                            .OrderByDescending(b => b.BookingDate)
                            .ToList();


            ViewBag.UserEmail = user.Email; // truyền vào ViewBag

            return View("MyOrders", orders);
        }



        // ---------- HUỶ ĐƠN HÀNG ----------
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = _ctx.Users.FirstOrDefault(u => u.Email == email);

            var booking = _ctx.Bookings
                              .Include(b => b.TourSchedule)
                              .FirstOrDefault(b => b.BookingId == id && b.UserId == user.UserId);

            if (booking == null)
            {
                logger.Warn("Không tìm thấy booking để huỷ. Email: {0}, BookingId: {1}", email, id);
                TempData["Error"] = "Không tìm thấy đơn hàng để huỷ.";
                return RedirectToAction("MyOrders");
            }

            // Trả slot
            if (booking.TourSchedule != null)
            {
                booking.TourSchedule.RemainingSlot += booking.AdultQuantity + booking.ChildQuantity;
            }

            // Xoá booking (hoặc có thể đánh dấu trạng thái 'Cancelled' tuỳ DB)
            _ctx.Bookings.Remove(booking);
            _ctx.SaveChanges();
            logger.Info("Người dùng huỷ đơn hàng thành công. Email: {0}, BookingId: {1}", email, id);
            TempData["Success"] = "Đã huỷ tour thành công.";
            return RedirectToAction("MyOrders");
        }
    }
}
