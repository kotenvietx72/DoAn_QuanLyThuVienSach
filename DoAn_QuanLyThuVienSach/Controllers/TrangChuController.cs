using DoAn_QuanLyThuVienSach.Data;
using DoAn_QuanLyThuVienSach.Models;
using DoAn_QuanLyThuVienSach.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            ViewBag.CategoryGroups = db.CategoryGroups
                                 .Include(g => g.Categories.OrderBy(c => c.Name)) // Sắp xếp thể loại con
                                 .OrderBy(g => g.Name) // Sắp xếp nhóm cha
                                 .ToList();

            HttpContext.Session.SetString("Username", username!);

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

        public IActionResult Search(string keyword, int page = 1)
        {
            int pageSize = 12; // Số sách trên 1 trang

            // Khởi tạo truy vấn
            IQueryable<Book> query = db.Books;

            // Nếu có từ khóa, áp dụng bộ lọc tìm kiếm
            if (!string.IsNullOrEmpty(keyword))
            {
                // Loại bỏ khoảng trắng thừa và chuyển sang chữ thường để tìm kiếm không phân biệt hoa thường
                string normalizedKeyword = keyword.Trim().ToLower();

                // Tìm kiếm sách theo Tiêu đề (Title) HOẶC Tác giả (Author)
                query = query.Where(b =>
                    b.Title.ToLower().Contains(normalizedKeyword) ||
                    b.BookAuthors.Any(ba => ba.Author.Name.ToLower().Contains(normalizedKeyword))
                );

                // Lưu từ khóa vào ViewBag để giữ lại trên thanh tìm kiếm sau khi gửi form
                ViewBag.Keyword = keyword;
            }

            // Sắp xếp và phân trang kết quả
            var searchResults = query.OrderBy(b => b.BookId).ToPagedList(page, pageSize);

            // Lưu thông báo kết quả tìm kiếm (Tùy chọn)
            ViewBag.SearchCount = searchResults.TotalItemCount;

            // Trả về View Index (hoặc View Search riêng nếu bạn có) với kết quả phân trang
            return View("Index", searchResults);
        }

        // GET: /TrangChu/Detail/5
        public IActionResult Detail(int id)
        {
            // Lấy username để hiển thị Header
            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
                ViewBag.Username = username;

            // Sử dụng Eager Loading để tải thông tin Tác giả và Nhà xuất bản
            var book = db.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.Publisher)
                // 🌟 THÊM PHẦN TẢI THÔNG TIN THỂ LOẠI 🌟
                .Include(b => b.BookCategories) // Tải bảng trung gian BookCategory
                    .ThenInclude(bc => bc.Category) // Tải thông tin Category
                .FirstOrDefault(b => b.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            // TÍNH TOÁN SỐ LƯỢNG SÁCH CÓ THỂ MƯỢN (Giả định có thuộc tính AvailableCopies)
            // Nếu không có AvailableCopies, bạn có thể tự thêm nó hoặc dùng TotalCopies.
            ViewBag.IsAvailable = book.TotalCopies > 0;

            return View(book);
        }
    }
}
