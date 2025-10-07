using DoAn_QuanLyThuVienSach.Data;
using DoAn_QuanLyThuVienSach.Models;
using DoAn_QuanLyThuVienSach.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DoAn_QuanLyThuVienSach.Controllers
{
    public class LoanController : Controller
    {
        private readonly DataContext db;

        public LoanController(DataContext db) => this.db = db;

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(int id)
        {
            var memberIdStr = HttpContext.Session.GetString("MemberId");
            var username = HttpContext.Session.GetString("Username");

            if (!string.IsNullOrEmpty(username))
                ViewBag.Username = username;

            if (string.IsNullOrEmpty(memberIdStr))
            {
                // Nếu chưa đăng nhập, chuyển về trang đăng nhập
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để mượn sách!";
                return RedirectToAction("Login", "Account");
            }

            var book = await db.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            // TÍNH TOÁN SỐ LƯỢNG SÁCH CÓ SẴN
            int currentlyLoanedCount = await db.Loans
                .Where(l => l.BookId == id && l.DateReturned == null)
                .SumAsync(l => l.Quantity);

            int availableCopies = book.TotalCopies - currentlyLoanedCount;

            if (availableCopies <= 0)
            {
                TempData["ErrorMessage"] = "Sách này đã hết, vui lòng quay lại sau.";
                return RedirectToAction("Details", "Book", new { id = id });
            }

            // Truyền số lượng có sẵn qua ViewBag để hiển thị hoặc kiểm tra
            ViewBag.AvailableCopies = availableCopies;

            var viewModel = new LoanViewModel
            {
                BookId = book.BookId,
                BookTitle = book.Title,
                CoverImage = book.CoverImage,
                // Các thông tin khác nếu cần
            };

            return View(viewModel);
        }

        // POST: /Loan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoanViewModel model)
        {
            var memberIdStr = HttpContext.Session.GetString("MemberId");
            if (string.IsNullOrEmpty(memberIdStr))
            {
                return RedirectToAction("Login", "Account");
            }

            var book = await db.Books.FindAsync(model.BookId);

            if (ModelState.IsValid && book != null)
            {
                int currentlyLoanedCount = await db.Loans
                    .Where(l => l.BookId == model.BookId && l.DateReturned == null)
                    .SumAsync(l => l.Quantity);

                int availableCopies = book.TotalCopies - currentlyLoanedCount;

                if (model.Quantity > availableCopies)
                {
                    ModelState.AddModelError("Quantity", $"Số lượng sách có sẵn không đủ (chỉ còn {availableCopies} cuốn).");
                    model.BookTitle = book.Title;
                    model.CoverImage = book.CoverImage;
                    return View(model);
                }

                var loan = new Loan
                {
                    BookId = model.BookId,
                    MemberId = int.Parse(memberIdStr),
                    DateBorrowed = DateTime.Now,
                    DueDate = model.DueDate,
                    Quantity = model.Quantity,
                    DateReturned = null // Chưa trả
                };

                db.Loans.Add(loan);
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Mượn sách thành công!";
                return RedirectToAction("Index", "TrangChu");
            }

            // Nếu có lỗi, load lại thông tin sách để hiển thị
            if (book != null)
            {
                model.BookTitle = book.Title;
                model.CoverImage = book.CoverImage;
            }

            return View(model);
        }

        public async Task<IActionResult> History()
        {
            var username = HttpContext.Session.GetString("Username");

            if (!string.IsNullOrEmpty(username))
                ViewBag.Username = username;

            var memberIdStr = HttpContext.Session.GetString("MemberId");
            if (string.IsNullOrEmpty(memberIdStr))
            {
                // Nếu chưa đăng nhập, chuyển về trang đăng nhập
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem lịch sử mượn sách.";
                return RedirectToAction("Login", "Account");
            }

            var currentMemberId = int.Parse(memberIdStr);

            var userLoans = await db.Loans
                .Include(l => l.Book)
                .Where(l => l.MemberId == currentMemberId) 
                .OrderBy(l => l.DateBorrowed)
                .ToListAsync();

            return View(userLoans);
        }

        public async Task<IActionResult> Manage()
        {
            var username = HttpContext.Session.GetString("Username");

            if (!string.IsNullOrEmpty(username))
                ViewBag.Username = username;

            var memberIdStr = HttpContext.Session.GetString("MemberId");
            if (string.IsNullOrEmpty(memberIdStr))
            {
                // Nếu chưa đăng nhập, yêu cầu đăng nhập
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để quản lý sách mượn.";
                return RedirectToAction("Login", "Account");
            }

            var currentMemberId = int.Parse(memberIdStr);

            // Lấy danh sách sách đang mượn (chưa trả) của chính thành viên này
            var activeLoans = await db.Loans
                .Include(l => l.Book)
                .Where(l => l.MemberId == currentMemberId && l.DateReturned == null)
                .OrderBy(l => l.DueDate)
                .ToListAsync();

            return View(activeLoans);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(int id)
        {
            var loan = await db.Loans.Include(l => l.Book).FirstOrDefaultAsync(l => l.LoanId == id);

            if (loan == null)
            {
                return NotFound();
            }

            loan.DateReturned = DateTime.Now;

            var book = loan.Book;
            if (book != null)
                book.TotalCopies += loan.Quantity;

            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xác nhận trả sách '{loan.Book.Title}' thành công!";

            return RedirectToAction(nameof(Manage));
        }
    }
}
