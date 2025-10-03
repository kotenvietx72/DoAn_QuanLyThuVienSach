using Microsoft.AspNetCore.Mvc;

namespace DoAn_QuanLyThuVienSach.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");

            if (!string.IsNullOrEmpty(username))
                ViewBag.Username = username;

            return View();
        }

        [HttpPost]
        public IActionResult Send(string name, string email, string subject, string message)
        {
            // Logic xử lý và gửi email
            ViewBag.Message = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.";

            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
                ViewBag.Username = username;

            return View("Index"); // Trở lại trang liên hệ
        }
    }
}
