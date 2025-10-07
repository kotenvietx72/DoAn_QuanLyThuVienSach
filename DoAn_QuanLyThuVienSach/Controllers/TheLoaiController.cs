using DoAn_QuanLyThuVienSach.Data;
using DoAn_QuanLyThuVienSach.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace DoAn_QuanLyThuVienSach.Controllers
{
    public class TheLoaiController : Controller
    {
        private readonly DataContext db;

        public TheLoaiController() => db = new DataContext();

        public IActionResult Index()
        {
            return View();
        }

        // GET: /TheLoai/GroupDetails/VanHoc&NgheThuat
        public IActionResult GroupDetail(int groupId, int page = 1)
        {
            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
                ViewBag.Username = username;

            if (groupId <= 0)
            {
                return RedirectToAction("Index", "TrangChu");
            }
            int pageSize = 10;

            var menuData = db.CategoryGroups
                .Include(g => g.Categories.OrderBy(c => c.Name))
                .OrderBy(g => g.Name)
                .Select(g => new CategoryGroupMenuVM // SỬ DỤNG VIEW MODEL CÓ BookCount
                {
                    CategoryGroupId = g.CategoryGroupId,
                    Name = g.Name,
                    IconClass = g.IconClass,
                    Categories = g.Categories.ToList(),
                    BookCount = db.BookCategories
                        .Where(bc => bc.Category.CategoryGroupId == g.CategoryGroupId)
                        .Select(bc => bc.BookId)
                        .Distinct()
                        .Count()
                })
            .ToList();

            ViewBag.CategoryGroups = menuData;

            var books = db.Books
                .Where(b => b.BookCategories.Any(bc =>
                    bc.Category != null &&
                    bc.Category.CategoryGroupId == groupId))
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .OrderByDescending(b => b.PublicationYear)
                .ToPagedList(page, pageSize);


            // Lấy tên nhóm để hiển thị trên tiêu đề View
            var groupName = db.CategoryGroups.FirstOrDefault(g => g.CategoryGroupId == groupId)?.Name ?? "Không xác định";

            // Chuẩn bị dữ liệu cho View
            ViewBag.GroupId = groupId; // Giữ lại ID để phân trang
            ViewBag.GroupName = groupName;
            ViewBag.TotalBooks = books.TotalItemCount;

            return View(books);
        }
    }
}
