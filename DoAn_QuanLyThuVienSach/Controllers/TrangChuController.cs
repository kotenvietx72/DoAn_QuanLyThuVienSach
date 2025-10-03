using DoAn_QuanLyThuVienSach.Data;
using DoAn_QuanLyThuVienSach.ViewModel;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace DoAn_QuanLyThuVienSach.Controllers
{
    public class TrangChuController : Controller
    {
        private readonly DataContext db;

        public TrangChuController() => db = new DataContext();

        public IActionResult Index(int page = 1)
        {
            int pageSize = 12; // số sách trên 1 trang
            var books = db.Books.OrderBy(b => b.BookId).ToPagedList(page, pageSize);

            var username = HttpContext.Session.GetString("Username");

            if (!string.IsNullOrEmpty(username))
                ViewBag.Username = username;

            return View(books);
        }

        public ActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // GET: /TrangChu/Profile
        [HttpGet]
        public IActionResult Profile()
        {
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");
            else
                ViewBag.Username = username;

            var member = db.Members.FirstOrDefault(m => m.Name == username);
            if (member == null)
                return NotFound();

            var viewModel = new ProfileViewModel
            {
                Name = member.Name,
                Email = member.Email,
                PhoneNumber = member.PhoneNumber,
                Address = member.Address
            };

            return View(viewModel);
        }

        // POST: /TrangChu/Profile
        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var username = HttpContext.Session.GetString("Username");

            // Chỉ validate các trường mật khẩu nếu người dùng thực sự nhập mật khẩu mới
            if (string.IsNullOrEmpty(model.NewPassword))
            {
                ModelState.Remove("NewPassword");
                ModelState.Remove("ConfirmNewPassword");
                ModelState.Remove("CurrentPassword");
            }

            if (ModelState.IsValid)
            {
                var memberToUpdate = db.Members.FirstOrDefault(m => m.Name == username);
                if (memberToUpdate != null)
                {
                    // Cập nhật thông tin cá nhân
                    memberToUpdate.Name = model.Name;
                    memberToUpdate.PhoneNumber = model.PhoneNumber;
                    memberToUpdate.Address = model.Address;

                    // Xử lý đổi mật khẩu NẾU người dùng có nhập mật khẩu mới
                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        if (memberToUpdate.Password != model.CurrentPassword)
                        {
                            ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác.");
                            return View(model);
                        }
                        memberToUpdate.Password = model.NewPassword;
                        TempData["SuccessMessage"] = "Cập nhật thông tin và mật khẩu thành công!";
                    }
                    else
                        TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";

                    await db.SaveChangesAsync();
                    return RedirectToAction("Profile");
                }
            }
            return View(model);
        }
    }
}
