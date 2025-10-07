using DoAn_QuanLyThuVienSach.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAn_QuanLyThuVienSach.ViewModel;

namespace DoAn_QuanLyThuVienSach.Controllers
{
    public class AdminController : Controller
    {
        private readonly DataContext db;

        public AdminController(DataContext db) => this.db = db;

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role != null && role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        public IActionResult Index(string module = "book", string searchString = "")
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "TrangChu");

            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
                ViewBag.Username = username;

            var vm = new AdminDashboardVM {CurrentModule = module};

            ViewData["CurrentFilter"] = searchString;

            switch (module.ToLower())
            {
                case "author":
                    var authors = db.Authors.AsQueryable();
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        authors = authors.Where(a => a.Name.Contains(searchString));
                    }
                    ViewData["ModuleData"] = authors.ToList();
                    ViewData["Title"] = "Quản lý Tác giả";
                    ViewData["PartialViewName"] = "_AuthorList";
                    break;

                case "publisher":
                    var publishers = db.Publishers.AsQueryable();
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        publishers = publishers.Where(p => p.Name.Contains(searchString));
                    }
                    ViewData["ModuleData"] = publishers.ToList();
                    ViewData["Title"] = "Quản lý Nhà Xuất Bản";
                    ViewData["PartialViewName"] = "_PublisherList";
                    break;

                case "category":
                    var categories = db.Categories.Include(c => c.CategoryGroup).AsQueryable();
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        categories = categories.Where(c => c.Name.Contains(searchString));
                    }
                    ViewData["ModuleData"] = categories.ToList();
                    ViewData["Title"] = "Quản lý Thể loại";
                    ViewData["PartialViewName"] = "_CategoryList";
                    break;

                case "loan":
                    var loans = db.Loans.Include(l => l.Book).Include(l => l.Member).AsQueryable();
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        // Tìm theo tên sách hoặc tên người mượn
                        loans = loans.Where(l => l.Book.Title.Contains(searchString) || l.Member.Name.Contains(searchString));
                    }
                    ViewData["ModuleData"] = loans.ToList();
                    ViewData["Title"] = "Quản lý Mượn Sách";
                    ViewData["PartialViewName"] = "_LoanList";
                    break;

                case "member":
                    var members = db.Members.Include(m => m.Role).AsQueryable();
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        // Tìm theo tên hoặc email
                        members = members.Where(m => m.Name.Contains(searchString) || m.Email.Contains(searchString));
                    }
                    ViewData["ModuleData"] = members.ToList();
                    ViewData["Title"] = "Quản lý Tài khoản";
                    ViewData["PartialViewName"] = "_MemberList";
                    break;

                case "book":
                default:
                    var books = db.Books.Include(b => b.Publisher).AsQueryable();
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        books = books.Where(b => b.Title.Contains(searchString));
                    }
                    ViewData["ModuleData"] = books.ToList();
                    ViewData["Title"] = "Quản lý Sách";
                    ViewData["PartialViewName"] = "_BookList";
                    break;
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await db.Books.FindAsync(id);
            if (book == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sách!";
                return RedirectToAction("Index", new { module = "book" });
            }
            db.Books.Remove(book);
            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa sách '{book.Title}' thành công!";
            return RedirectToAction("Index", new { module = "book" });
        }
    }
}
