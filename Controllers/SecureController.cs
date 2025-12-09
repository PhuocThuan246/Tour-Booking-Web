using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TourBookingWeb.Controllers
{
    //Secure/Index
    public class SecureController : Controller
    {
        [Authorize] // Bắt buộc phải có JWT hợp lệ
        public IActionResult Index()
        {
            //var email = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //var message = $"Bạn đã xác thực thành công. User ID: {email}";
            //return Content(message);
            var claims = User.Claims.Select(c => $"{c.Type}: {c.Value}");
            return Content(string.Join("\n", claims));
        }
    }
}
