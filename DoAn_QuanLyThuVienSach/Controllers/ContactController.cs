using DoAn_QuanLyThuVienSach.Data;
using DoAn_QuanLyThuVienSach.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAn_QuanLyThuVienSach.Controllers
{
    public class ContactController : Controller
    {
        private readonly DataContext db;

        public ContactController() => db = new DataContext();

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
            TempData["SuccessMessage"] = "Chúng tôi sẽ phản hồi lại trong thời gian sớm nhất!";

            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
                ViewBag.Username = username;

            return View("Index"); // Trở lại trang liên hệ
        }
    }
}
