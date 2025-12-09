using Microsoft.AspNetCore.Mvc;

namespace TourBookingWeb.Areas.Admin.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

}
