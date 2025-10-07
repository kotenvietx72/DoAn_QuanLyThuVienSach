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

            var user = db.Members
               .Include(m => m.Role)
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
            HttpContext.Session.SetString("MemberId", user.MemberId.ToString());

            string userRole = user.Role?.RoleName ?? "Member";
            HttpContext.Session.SetString("Role", userRole);

            if (userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Chuyển hướng đến trang quản trị
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                // Chuyển hướng đến trang khách hàng
                return RedirectToAction("Index", "TrangChu");
            }
        }

        [HttpPost]
        public ActionResult Register(string Email, string PhoneNumber, string Password, string ConfirmPassword)
        {
            // Hàm kiểm tra lỗi
            Action<string> setSignUpError = (errorMessage) => {
                ViewBag.Error = errorMessage;
                ViewBag.IsSignUpError = true;
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

            var member = new Member
            {
                Name = string.IsNullOrEmpty(Email) ? "User" : Email.Split('@')[0],
                Email = Email,
                PhoneNumber = PhoneNumber,
                Password = Password,
                Address = "",
                DateCreated = DateTime.Now,
                RoleId = 2
            };

            db.Members.Add(member);
            db.SaveChanges();

            ViewBag.Success = "Đăng ký thành công, bạn có thể đăng nhập.";
            return View("Login");
        }
    }
}
