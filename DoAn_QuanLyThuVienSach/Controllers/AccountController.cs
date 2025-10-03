using DoAn_QuanLyThuVienSach.Data;
using DoAn_QuanLyThuVienSach.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAn_QuanLyThuVienSach.Controllers
{
    public class AccountController : Controller
    {
        private readonly DataContext db;

        public AccountController() => db = new DataContext();

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string Username, string Password)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            // Tìm user theo email hoặc số điện thoại
            var user = db.Members
                .FirstOrDefault(m => m.Email == Username || m.PhoneNumber == Username);

            if (user == null)
            {
                ViewBag.Error = "Tài khoản không tồn tại.";
                return View();
            }

            if (user.Password != Password)
            {
                ViewBag.Error = "Mật khẩu không chính xác.";
                return View();
            }


            HttpContext.Session.SetString("Username", user.Name);
            return RedirectToAction("Index", "TrangChu"); // Chuyển về trang chủ
        }

        [HttpPost]
        public ActionResult Register(string Email, string PhoneNumber, string Password, string ConfirmPassword)
        {
            // Hàm kiểm tra lỗi
            Action<string> setSignUpError = (errorMessage) => {
                ViewBag.Error = errorMessage;
                ViewBag.IsSignUpError = true; // Cờ để JS biết và mở lại tab đăng ký
                                              // Lưu lại giá trị đã nhập để không phải nhập lại
                ViewBag.EmailValue = Email;
                ViewBag.PhoneValue = PhoneNumber;
            };

            if (Password != ConfirmPassword)
            {
                setSignUpError("Mật khẩu nhập lại không khớp.");
                return View("Login");
            }

            if (db.Members.Any(m => m.Email == Email))
            {
                setSignUpError("Email đã tồn tại.");
                return View("Login");
            }

            // Nếu không có lỗi, tiến hành tạo tài khoản mới
            var member = new Member
            {
                Name = string.IsNullOrEmpty(Email) ? "User" : Email.Split('@')[0],
                Email = Email,
                PhoneNumber = PhoneNumber,
                Password = Password, // Lưu ý: Nên mã hóa mật khẩu trước khi lưu
                Address = "",
                DateCreated = DateTime.Now,
                RoleId = 2 // mặc định là Member
            };

            db.Members.Add(member);
            db.SaveChanges();

            // Đăng ký thành công, gửi thông báo và hiển thị tab đăng nhập
            ViewBag.Success = "Đăng ký thành công, bạn có thể đăng nhập.";
            return View("Login");
        }
    }
}
