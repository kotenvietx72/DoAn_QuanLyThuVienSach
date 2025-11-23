using DoAn_QuanLyThuVienSach.Data;
using DoAn_QuanLyThuVienSach.Models;
using DoAn_QuanLyThuVienSach.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class AdminController : Controller
{
    private readonly DataContext db;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminController(DataContext db, IWebHostEnvironment webHostEnvironment)
    {
        this.db = db;
        _webHostEnvironment = webHostEnvironment;
    }

    #region Trang chính & Tìm kiếm
    public IActionResult Index(string module, string searchString = "", int memberId = 0, string status = "")
    {
        if (string.IsNullOrEmpty(module))
        {
            return RedirectToAction(nameof(Dashboard));
        }

        ViewData["CurrentFilter"] = searchString;
        ViewData["CurrentModule"] = module;

        switch (module.ToLower())
        {
            case "author":
                var authors = db.Authors.AsQueryable();
                if (!string.IsNullOrEmpty(searchString))
                    authors = authors.Where(a => a.Name.Contains(searchString));
                ViewData["ModuleData"] = authors.ToList();
                ViewData["Title"] = "Quản lý Tác giả";
                ViewData["PartialViewName"] = "_AuthorList";
                break;
            case "publisher":
                var publishers = db.Publishers.AsQueryable();
                if (!string.IsNullOrEmpty(searchString))
                    publishers = publishers.Where(p => p.Name.Contains(searchString));
                ViewData["ModuleData"] = publishers.ToList();
                ViewData["Title"] = "Quản lý Nhà Xuất Bản";
                ViewData["PartialViewName"] = "_PublisherList";
                break;
            case "category":
                var categories = db.Categories.Include(c => c.CategoryGroup).AsQueryable();
                if (!string.IsNullOrEmpty(searchString))
                    categories = categories.Where(c => c.Name.Contains(searchString));
                ViewData["ModuleData"] = categories.ToList();
                ViewData["Title"] = "Quản lý Thể loại";
                ViewData["PartialViewName"] = "_CategoryList";
                break;
            case "loan":
                // Gọi hàm chung
                LoadLoanFilters(memberId, status);

                var loans = db.Loans
                    .Include(l => l.Book)
                    .Include(l => l.Member)
                    .AsQueryable();

                // Search
                if (!string.IsNullOrEmpty(searchString))
                    loans = loans.Where(l => l.Book.Title.Contains(searchString)
                                       || l.Member.Name.Contains(searchString));

                // Lọc theo member
                if (memberId > 0)
                    loans = loans.Where(l => l.MemberId == memberId);

                // Lọc theo tình trạng
                switch (status)
                {
                    case "returned":
                        loans = loans.Where(l => l.DateReturned != null);
                        break;

                    case "overdue":
                        loans = loans.Where(l => l.DateReturned == null
                                               && l.DueDate < DateTime.Now);
                        break;

                    case "borrowing":
                        loans = loans.Where(l => l.DateReturned == null
                                               && l.DueDate >= DateTime.Now);
                        break;
                }

                ViewData["ModuleData"] = loans.ToList();
                ViewData["PartialViewName"] = "_LoanList";
                ViewData["Title"] = "Quản lý Mượn Sách";

                break;
            case "member":
                var members = db.Members.Include(m => m.Role).AsQueryable();
                if (!string.IsNullOrEmpty(searchString))
                    members = members.Where(m => m.Name.Contains(searchString) || m.Email.Contains(searchString));
                ViewData["ModuleData"] = members.ToList();
                ViewData["Title"] = "Quản lý Thành Viên";
                ViewData["PartialViewName"] = "_MemberList";
                break;
            case "categorygroup":
                var categoryGroups = db.CategoryGroups.AsQueryable();
                if (!string.IsNullOrEmpty(searchString))
                    categoryGroups = categoryGroups.Where(cg => cg.Name.Contains(searchString));
                ViewData["ModuleData"] = categoryGroups.ToList();
                ViewData["Title"] = "Quản lý Nhóm Thể loại";
                ViewData["PartialViewName"] = "_CategoryGroupList";
                break;
            case "book":
            default:
                var books = db.Books.Include(b => b.Publisher).AsQueryable();
                if (!string.IsNullOrEmpty(searchString))
                    books = books.Where(b => b.Title.Contains(searchString));
                ViewData["ModuleData"] = books.ToList();
                ViewData["Title"] = "Quản lý Sách";
                ViewData["PartialViewName"] = "_BookList";
                break;
        }
        return View();
    }
    #endregion

    #region Dashboard
    public IActionResult Dashboard()
    {
        DateTime today = DateTime.Now.Date;
        DateTime startDate = today.AddDays(-6);

        var loans = db.Loans
            .Include(l => l.Book)
            .ThenInclude(b => b.BookCategories)
            .ThenInclude(bc => bc.Category)
            .ToList();

        DashboardVM vm = new DashboardVM();

        // ----------------------
        // 1) KPI tổng quan
        // ----------------------

        int totalCopies = db.Books.Sum(b => b.TotalCopies);

        int borrowedCopies = loans
            .Where(l => l.DateReturned == null)
            .Sum(l => l.Quantity);

        vm.TotalBooks = totalCopies - borrowedCopies;

        vm.TotalMembers = db.Members.Count();

        vm.BorrowingCount = loans
            .Where(l => l.DateReturned == null && l.DueDate >= today)
            .Sum(l => l.Quantity);

        vm.OverdueCount = loans
            .Where(l => l.DateReturned == null && l.DueDate < today)
            .Sum(l => l.Quantity);


        // ----------------------
        // 2) Tình trạng tổng hợp
        // ----------------------

        vm.StatusReturned = loans
            .Where(l => l.DateReturned != null)
            .Sum(l => l.Quantity);

        vm.StatusOverdue = loans
            .Where(l => l.DateReturned == null && l.DueDate < today)
            .Sum(l => l.Quantity);

        vm.StatusBorrowing = loans
            .Where(l => l.DateReturned == null && l.DueDate >= today)
            .Sum(l => l.Quantity);


        // ----------------------
        // 3) Mượn / trả 7 ngày gần nhất
        // ----------------------

        var last7days = Enumerable.Range(0, 14).Select(i => startDate.AddDays(i)).ToList();

        foreach (var day in last7days)
        {
            vm.Last14DaysLabels.Add(day.ToString("dd/MM"));

            vm.BorrowCounts.Add(
                loans.Where(l => l.DateBorrowed.Date == day.Date)
                     .Sum(l => l.Quantity)
            );

            vm.ReturnCounts.Add(
                loans.Where(l => l.DateReturned.HasValue &&
                                 l.DateReturned.Value.Date == day.Date)
                     .Sum(l => l.Quantity)
            );
        }


        // ----------------------
        // 4) Top 10 sách được mượn nhiều nhất
        // ----------------------

        var topBooks = loans
            .GroupBy(l => l.Book.Title)
            .Select(g => new { Title = g.Key, Count = g.Sum(l => l.Quantity) })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        vm.TopBookLabels = topBooks.Select(x => x.Title).ToList();
        vm.TopBookCounts = topBooks.Select(x => x.Count).ToList();


        // ----------------------
        // 5) Top 10 thể loại được mượn nhiều nhất
        // ----------------------

        var topCategories = loans
            .Where(l => l.Book.BookCategories.Any())
            .SelectMany(l => l.Book.BookCategories.Select(bc => new
            {
                Category = bc.Category.Name,
                Qty = l.Quantity
            }))
            .GroupBy(x => x.Category)
            .Select(g => new { Category = g.Key, Count = g.Sum(x => x.Qty) })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        vm.TopCategoryLabels = topCategories.Select(x => x.Category).ToList();
        vm.TopCategoryCounts = topCategories.Select(x => x.Count).ToList();


        ViewData["Title"] = "Tổng quan";
        return View(vm);
    }

    #endregion

    #region Book CRUD
    // GET: /Admin/UpsertBook/5 (Edit) hoặc /Admin/UpsertBook (Create)
    public async Task<IActionResult> UpsertBook(int? id)
    {
        BookViewModel vm = new BookViewModel
        {
            AllPublishers = new SelectList(db.Publishers.ToList(), "Name", "Name"),
            AllCategories = new SelectList(db.Categories.ToList(), "Name", "Name")
        };

        if (id != null) // Edit
        {
            vm.Book = await db.Books
                .Include(b => b.Publisher)
                .Include(b => b.BookCategories).ThenInclude(bc => bc.Category)
                .FirstOrDefaultAsync(b => b.BookId == id);
            if (vm.Book == null) return NotFound();
            vm.PublisherName = vm.Book.Publisher?.Name;
            vm.CategoryNames = vm.Book.BookCategories.Select(bc => bc.Category.Name).ToList();
        }

        ViewData["Title"] = id == null ? "Thêm sách mới" : "Chỉnh sửa sách";
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> UpsertBook(BookViewModel vm)
    {
        // Bỏ qua validation cho các thuộc tính liên kết và SelectLists
        ModelState.Remove("Book.Publisher");
        ModelState.Remove("Book.BookCategories");
        ModelState.Remove("Book.BookAuthors");
        ModelState.Remove("Book.Loans");
        ModelState.Remove("AllPublishers");
        ModelState.Remove("AllCategories");
        ModelState.Remove("Book.CoverImage");

        // Kiểm tra xem người dùng có upload file khi tạo mới không
        if (vm.Book.BookId == 0 && vm.CoverImageFile == null)
        {
            ModelState.AddModelError("CoverImageFile", "Vui lòng chọn ảnh bìa khi tạo sách mới.");
        }

        if (ModelState.IsValid)
        {
            // --- Xử lý Publisher ---
            if (!string.IsNullOrEmpty(vm.PublisherName))
            {
                var publisher = await db.Publishers.FirstOrDefaultAsync(p => p.Name == vm.PublisherName);
                if (publisher == null)
                {
                    publisher = new Publisher { Name = vm.PublisherName, Address = "" };
                    db.Publishers.Add(publisher);
                }
                vm.Book.Publisher = publisher;
            }

            // --- Xử lý Thêm mới hoặc Cập nhật Sách ---
            if (vm.Book.BookId == 0) // Trường hợp THÊM MỚI
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.CoverImageFile.FileName);
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "books");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                var filePath = Path.Combine(uploadPath, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.CoverImageFile.CopyToAsync(fileStream);
                }
                vm.Book.CoverImage = "/uploads/books/" + fileName;

                db.Books.Add(vm.Book);
            }
            else // Trường hợp CẬP NHẬT
            {
                var bookFromDb = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.BookId == vm.Book.BookId);
                if (vm.CoverImageFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.CoverImageFile.FileName);
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "books");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                        await vm.CoverImageFile.CopyToAsync(fileStream);
                    vm.Book.CoverImage = "/uploads/books/" + fileName;
                }
                else
                {
                    vm.Book.CoverImage = bookFromDb.CoverImage;
                }
                db.Books.Update(vm.Book);
                var existingCategories = db.BookCategories.Where(bc => bc.BookId == vm.Book.BookId);
                db.BookCategories.RemoveRange(existingCategories);
            }

            // --- Xử lý Categories ---
            if (vm.CategoryNames != null)
            {
                foreach (var catName in vm.CategoryNames)
                {
                    var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == catName);
                    if (category == null)
                    {
                        category = new Category { Name = catName, Description = "", CategoryGroupId = 1 };
                        db.Categories.Add(category);
                    }
                    vm.Book.BookCategories.Add(new BookCategory { Category = category });
                }
            }

            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = vm.Book.BookId == 0 ? "Thêm sách thành công!" : "Cập nhật sách thành công!";
            return RedirectToAction("Index", new { module = "book" });
        }

        // Nếu lỗi, quay về form
        var errors = ModelState.Where(x => x.Value.Errors.Count > 0).Select(x => $"[{x.Key}]: {x.Value.Errors.First().ErrorMessage}").ToList();
        TempData["ErrorMessage"] = "Lỗi validation: " + string.Join(" | ", errors);
        vm.AllPublishers = new SelectList(db.Publishers.ToList(), "Name", "Name", vm.PublisherName);
        vm.AllCategories = new SelectList(db.Categories.ToList(), "Name", "Name", vm.CategoryNames);
        return View("UpsertBook", vm);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var book = await db.Books.FindAsync(id);
        if (book != null)
        {
            db.Books.Remove(book);
            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa sách '{book.Title}' thành công!";
        }
        return RedirectToAction("Index", new { module = "book" });
    }

    public IActionResult ExportBookExcel(string searchString = "")
    {
        var books = db.Books
            .Include(b => b.Publisher)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
            books = books.Where(b => b.Title.Contains(searchString));

        var data = books.ToList();

        using var package = new OfficeOpenXml.ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Books");

        // Header
        ws.Cells[1, 1].Value = "ID";
        ws.Cells[1, 2].Value = "Tên sách";
        ws.Cells[1, 3].Value = "NXB";
        ws.Cells[1, 4].Value = "Năm xuất bản";
        ws.Cells[1, 5].Value = "Số lượng";

        int row = 2;
        foreach (var b in data)
        {
            ws.Cells[row, 1].Value = b.BookId;
            ws.Cells[row, 2].Value = b.Title;
            ws.Cells[row, 3].Value = b.Publisher?.Name;
            ws.Cells[row, 4].Value = b.PublicationYear;
            ws.Cells[row, 5].Value = b.TotalCopies;
            row++;
        }

        ws.Cells.AutoFitColumns();

        var stream = new MemoryStream(package.GetAsByteArray());
        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "BookList.xlsx");
    }

    [HttpGet]
    public IActionResult ExportBookPdf(string searchString = "")
    {
        var books = db.Books
            .Include(b => b.Publisher)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
            books = books.Where(b => b.Title.Contains(searchString));

        var list = books.ToList();

        return File(GenerateBookPdf(list), "application/pdf", "DanhSachSach.pdf");
    }

    private byte[] GenerateBookPdf(List<Book> books)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);

                page.Header()
                    .AlignCenter()
                    .Text("DANH SÁCH SÁCH")
                    .Bold().FontSize(22).FontColor(Colors.Blue.Medium);

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(40); // ID
                        c.RelativeColumn(3);  // Tên sách
                        c.RelativeColumn(2);  // NXB
                        c.RelativeColumn(1);  // Năm
                        c.RelativeColumn(1);  // Số lượng
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background("#F0F0F0").Padding(5).Text("ID").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Tên sách").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("NXB").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Năm").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Số lượng").Bold();
                    });

                    // Rows
                    foreach (var b in books)
                    {
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(b.BookId.ToString());
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(b.Title);
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(b.Publisher?.Name ?? "");
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(b.PublicationYear.ToString());
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(b.TotalCopies.ToString());
                    }
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("Generated: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(10);
                });
            });
        });

        return doc.GeneratePdf();
    }

    #endregion

    #region Category CRUD
    // GET: /Admin/UpsertCategory/5 (Edit) hoặc /Admin/UpsertCategory (Create)
    public async Task<IActionResult> UpsertCategory(int? id)
    {
        CategoryViewModel vm = new CategoryViewModel
        {
            Category = new Category(),
            // Lấy danh sách Nhóm thể loại từ DB để đưa vào dropdown
            CategoryGroupList = new SelectList(db.CategoryGroups.ToList(), "CategoryGroupId", "Name")
        };

        if (id == null) // Thêm mới
        {
            ViewData["Title"] = "Thêm thể loại mới";
            return View(vm);
        }
        else // Chỉnh sửa
        {
            vm.Category = await db.Categories.FindAsync(id);
            if (vm.Category == null) return NotFound();

            ViewData["Title"] = "Chỉnh sửa thể loại";
            return View(vm);
        }
    }

    // POST: /Admin/UpsertCategory
    [HttpPost]
    public async Task<IActionResult> UpsertCategory(CategoryViewModel vm)
    {
        ModelState.Remove("Category.CategoryGroup");

        if (ModelState.IsValid)
        {
            if (vm.Category.CategoryId == 0) // Thêm mới
            {
                await db.Categories.AddAsync(vm.Category);
                TempData["SuccessMessage"] = "Thêm thể loại thành công!";
            }
            else // Cập nhật
            {
                db.Categories.Update(vm.Category);
                TempData["SuccessMessage"] = "Cập nhật thể loại thành công!";
            }
            await db.SaveChangesAsync();
            return RedirectToAction("Index", new { module = "category" });
        }

        // Nếu lỗi, nạp lại danh sách và quay về form
        vm.CategoryGroupList = new SelectList(db.CategoryGroups.ToList(), "CategoryGroupId", "Name", vm.Category.CategoryGroupId);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category != null)
        {
            db.Categories.Remove(category);
            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa thể loại thành công!";
        }
        return RedirectToAction("Index", new { module = "category" });
    }

    public IActionResult ExportCategoryExcel(string searchString = "")
    {
        var categories = db.Categories
            .Include(c => c.CategoryGroup)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
            categories = categories.Where(c => c.Name.Contains(searchString));

        var data = categories.ToList();

        using var package = new OfficeOpenXml.ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Categories");

        ws.Cells[1, 1].Value = "ID";
        ws.Cells[1, 2].Value = "Tên thể loại";
        ws.Cells[1, 3].Value = "Nhóm";
        ws.Cells[1, 4].Value = "Mô tả";

        int row = 2;
        foreach (var c in data)
        {
            ws.Cells[row, 1].Value = c.CategoryId;
            ws.Cells[row, 2].Value = c.Name;
            ws.Cells[row, 3].Value = c.CategoryGroup?.Name;
            ws.Cells[row, 4].Value = c.Description;
            row++;
        }

        ws.Cells.AutoFitColumns();

        var stream = new MemoryStream(package.GetAsByteArray());
        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "CategoryList.xlsx");
    }

    [HttpGet]
    public IActionResult ExportCategoryPdf(string searchString = "")
    {
        var list = db.Categories
            .Include(c => c.CategoryGroup)
            .Where(c => string.IsNullOrEmpty(searchString) || c.Name.Contains(searchString))
            .ToList();

        return File(GenerateCategoryPdf(list), "application/pdf", "DanhSachTheLoai.pdf");
    }

    private byte[] GenerateCategoryPdf(List<Category> categories)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .AlignCenter()
                    .Text("DANH SÁCH THỂ LOẠI")
                    .Bold().FontSize(22).FontColor(Colors.Blue.Medium);

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(40); // ID
                        c.RelativeColumn(3);  // Tên thể loại
                        c.RelativeColumn(2);  // Nhóm
                        c.RelativeColumn(3);  // Mô tả
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#F0F0F0").Padding(5).Text("ID").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Tên thể loại").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Nhóm").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Mô tả").Bold();
                    });

                    foreach (var c in categories)
                    {
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(c.CategoryId.ToString());
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(c.Name);
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(c.CategoryGroup?.Name ?? "");
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(c.Description ?? "");
                    }
                });
            });
        });

        return doc.GeneratePdf();
    }

    #endregion

    #region Author Edit
    public async Task<IActionResult> EditAuthor(int id)
    {
        var author = await db.Authors.FindAsync(id);
        if (author == null) return NotFound();
        ViewData["Title"] = "Chỉnh sửa tác giả";
        return View(author);
    }

    [HttpPost]
    public async Task<IActionResult> EditAuthor(Author author)
    {
        if (ModelState.IsValid)
        {
            db.Authors.Update(author);
            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật tác giả thành công!";
            return RedirectToAction("Index", new { module = "author" });
        }
        return View(author);
    }

    public IActionResult ExportAuthorExcel(string searchString = "")
    {
        var authors = db.Authors.AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
            authors = authors.Where(a => a.Name.Contains(searchString));

        var data = authors.ToList();

        using var package = new OfficeOpenXml.ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Authors");

        ws.Cells[1, 1].Value = "ID";
        ws.Cells[1, 2].Value = "Tên tác giả";
        ws.Cells[1, 3].Value = "Mô tả";

        int row = 2;
        foreach (var a in data)
        {
            ws.Cells[row, 1].Value = a.AuthorId;
            ws.Cells[row, 2].Value = a.Name;
            ws.Cells[row, 3].Value = a.Bio;
            row++;
        }

        ws.Cells.AutoFitColumns();

        var stream = new MemoryStream(package.GetAsByteArray());
        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "AuthorList.xlsx");
    }

    [HttpGet]
    public IActionResult ExportAuthorPdf(string searchString = "")
    {
        var list = db.Authors
            .Where(a => string.IsNullOrEmpty(searchString) || a.Name.Contains(searchString))
            .ToList();

        return File(GenerateAuthorPdf(list), "application/pdf", "DanhSachTacGia.pdf");
    }

    private byte[] GenerateAuthorPdf(List<Author> authors)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .AlignCenter()
                    .Text("DANH SÁCH TÁC GIẢ")
                    .Bold().FontSize(22).FontColor(Colors.Blue.Medium);

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(40); // ID
                        c.RelativeColumn(4);  // Tên
                        c.RelativeColumn(4);  // Mô tả
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#F0F0F0").Padding(5).Text("ID").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Tên tác giả").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Mô tả").Bold();
                    });

                    foreach (var a in authors)
                    {
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(a.AuthorId.ToString());
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(a.Name);
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(a.Bio ?? "");
                    }
                });
            });
        });

        return doc.GeneratePdf();
    }

    #endregion

    #region Publisher Edit
    public async Task<IActionResult> EditPublisher(int id)
    {
        var publisher = await db.Publishers.FindAsync(id);
        if (publisher == null) return NotFound();
        ViewData["Title"] = "Chỉnh sửa nhà xuất bản";
        return View(publisher);
    }

    [HttpPost]
    public async Task<IActionResult> EditPublisher(Publisher publisher)
    {
        if (ModelState.IsValid)
        {
            db.Publishers.Update(publisher);
            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật NXB thành công!";
            return RedirectToAction("Index", new { module = "publisher" });
        }
        return View(publisher);
    }

    public IActionResult ExportPublisherExcel(string searchString = "")
    {
        // Lấy dữ liệu NXB
        var publishers = db.Publishers.AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
            publishers = publishers.Where(p => p.Name.Contains(searchString));

        var list = publishers.ToList();

        // Tạo Excel
        using var package = new OfficeOpenXml.ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("DanhSachNXB");

        // Header
        ws.Cells["A1"].Value = "ID";
        ws.Cells["B1"].Value = "Tên NXB";
        ws.Cells["C1"].Value = "Địa chỉ";

        using (var range = ws.Cells["A1:C1"])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }

        // Ghi dữ liệu
        int row = 2;
        foreach (var p in list)
        {
            ws.Cells[row, 1].Value = p.PublisherId;
            ws.Cells[row, 2].Value = p.Name;
            ws.Cells[row, 3].Value = p.Address;
            row++;
        }

        ws.Cells.AutoFitColumns();

        // Trả về file
        var bytes = package.GetAsByteArray();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "DanhSachNXB.xlsx");
    }

    [HttpGet]
    public IActionResult ExportPublisherPdf(string searchString = "")
    {
        var list = db.Publishers
            .Where(p => string.IsNullOrEmpty(searchString) || p.Name.Contains(searchString))
            .ToList();

        return File(GeneratePublisherPdf(list), "application/pdf", "DanhSachNXB.pdf");
    }

    private byte[] GeneratePublisherPdf(List<Publisher> publishers)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .AlignCenter()
                    .Text("DANH SÁCH NHÀ XUẤT BẢN")
                    .Bold().FontSize(22).FontColor(Colors.Blue.Medium);

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(40); // ID
                        c.RelativeColumn(3);  // Tên
                        c.RelativeColumn(4);  // Địa chỉ
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#F0F0F0").Padding(5).Text("ID").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Tên NXB").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Địa chỉ").Bold();
                    });

                    foreach (var p in publishers)
                    {
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(p.PublisherId.ToString());
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(p.Name);
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(p.Address ?? "");
                    }
                });
            });
        });

        return doc.GeneratePdf();
    }

    #endregion

    #region Loan Edit
    public async Task<IActionResult> EditLoan(int id)
    {
        var loan = await db.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .FirstOrDefaultAsync(l => l.LoanId == id);
        if (loan == null) return NotFound();
        ViewData["Title"] = "Chỉnh sửa lượt mượn";
        return View(loan);
    }

    [HttpPost]
    public async Task<IActionResult> EditLoan(Loan loan)
    {
        ModelState.Remove("Book");
        ModelState.Remove("Member");

        if (ModelState.IsValid)
        {
            db.Loans.Update(loan);
            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật lượt mượn thành công!";
            return RedirectToAction("Index", new { module = "loan" });
        }
        return View(loan);
    }

    private void LoadLoanFilters(int selectedMemberId = 0, string selectedStatus = "")
    {
        // Danh sách người mượn
        ViewBag.AllMembers = db.Members
            .OrderBy(m => m.Name)
            .ToList();

        // Lưu member đang chọn
        ViewBag.SelectedMemberId = selectedMemberId;

        // Danh sách tình trạng
        ViewBag.StatusList = new List<SelectListItem>
    {
        new SelectListItem { Value = "", Text = "Tất cả" },
        new SelectListItem { Value = "returned", Text = "Đã trả" },
        new SelectListItem { Value = "overdue", Text = "Quá hạn" },
        new SelectListItem { Value = "borrowing", Text = "Đang mượn" }
    };

        // Lưu status đang chọn
        ViewBag.SelectedStatus = selectedStatus;
    }

    public IActionResult ExportLoanExcel(string searchString = "", int memberId = 0, string status = "")
    {
        var loans = db.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .AsQueryable();

        // Search
        if (!string.IsNullOrEmpty(searchString))
            loans = loans.Where(l => l.Book.Title.Contains(searchString)
                                   || l.Member.Name.Contains(searchString));

        // Lọc theo member
        if (memberId > 0)
            loans = loans.Where(l => l.MemberId == memberId);

        // Lọc theo status
        switch (status)
        {
            case "returned":
                loans = loans.Where(l => l.DateReturned != null);
                break;
            case "overdue":
                loans = loans.Where(l => l.DateReturned == null && l.DueDate < DateTime.Now);
                break;
            case "borrowing":
                loans = loans.Where(l => l.DateReturned == null && l.DueDate >= DateTime.Now);
                break;
        }

        var list = loans.ToList();

        using var package = new OfficeOpenXml.ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Loans");

        // HEADER
        string[] headers = { "ID", "Tên Sách", "Người Mượn", "Ngày Mượn", "Ngày Trả", "Số Lượng", "Tình Trạng" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cells[1, i + 1].Value = headers[i];

        using (var r = ws.Cells["A1:G1"])
        {
            r.Style.Font.Bold = true;
            r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            r.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }

        // DATA
        int row = 2;
        foreach (var l in list)
        {
            ws.Cells[row, 1].Value = l.LoanId;
            ws.Cells[row, 2].Value = l.Book.Title;
            ws.Cells[row, 3].Value = l.Member.Name;
            ws.Cells[row, 4].Value = l.DateBorrowed.ToString("dd/MM/yyyy");
            ws.Cells[row, 5].Value = l.DateReturned?.ToString("dd/MM/yyyy") ?? "Chưa trả";
            ws.Cells[row, 6].Value = l.Quantity;

            string statusLabel =
                l.DateReturned != null ? "Đã trả" :
                l.DueDate < DateTime.Now ? "Quá hạn" :
                "Đang mượn";

            ws.Cells[row, 7].Value = statusLabel;
            row++;
        }

        ws.Cells.AutoFitColumns();

        return File(
            package.GetAsByteArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "DanhSachMuonSach.xlsx"
        );
    }

    public IActionResult ExportLoanPdf(string searchString = "", int memberId = 0, string status = "")
    {
        var loans = GetFilteredLoans(searchString, memberId, status).ToList();

        var bytes = GenerateLoanPdf(loans);
        return File(bytes, "application/pdf", "DanhSachMuonTra.pdf");
    }

    private byte[] GenerateLoanPdf(List<Loan> loans)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());

                // Tiêu đề
                page.Header().Text("BÁO CÁO DANH SÁCH MƯỢN TRẢ")
                    .Bold().FontSize(20).FontColor("#4f46e5");

                // Nội dung bảng
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1); // ID
                        c.RelativeColumn(2); // Sách
                        c.RelativeColumn(2); // Người mượn
                        c.RelativeColumn(2); // Ngày mượn
                        c.RelativeColumn(2); // Hạn trả
                        c.RelativeColumn(2); // Ngày trả
                        c.RelativeColumn(1); // SL
                    });

                    // Header
                    table.Header(h =>
                    {
                        h.Cell().Element(CellStyle).Text("ID");
                        h.Cell().Element(CellStyle).Text("Sách");
                        h.Cell().Element(CellStyle).Text("Người mượn");
                        h.Cell().Element(CellStyle).Text("Ngày mượn");
                        h.Cell().Element(CellStyle).Text("Hạn trả");
                        h.Cell().Element(CellStyle).Text("Ngày trả");
                        h.Cell().Element(CellStyle).Text("SL");
                    });

                    // Rows
                    foreach (var l in loans)
                    {
                        table.Cell().Element(CellStyle).Text(l.LoanId.ToString());
                        table.Cell().Element(CellStyle).Text(l.Book.Title);
                        table.Cell().Element(CellStyle).Text(l.Member.Name);
                        table.Cell().Element(CellStyle).Text(l.DateBorrowed.ToString("dd/MM/yyyy"));
                        table.Cell().Element(CellStyle).Text(l.DueDate.ToString("dd/MM/yyyy"));
                        table.Cell().Element(CellStyle).Text(l.DateReturned?.ToString("dd/MM/yyyy") ?? "—");
                        table.Cell().Element(CellStyle).Text(l.Quantity.ToString());
                    }
                });

                // Footer
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Trang ").FontSize(10);
                    t.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }



    private IEnumerable<Loan> GetFilteredLoans(string searchString, int memberId, string status)
    {
        var loans = db.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .AsQueryable();

        // Tìm kiếm
        if (!string.IsNullOrEmpty(searchString))
        {
            loans = loans.Where(l =>
                l.Book.Title.Contains(searchString) ||
                l.Member.Name.Contains(searchString)
            );
        }

        // Lọc theo người mượn
        if (memberId > 0)
            loans = loans.Where(l => l.MemberId == memberId);

        // Lọc theo tình trạng
        switch (status)
        {
            case "returned":
                loans = loans.Where(l => l.DateReturned != null);
                break;

            case "overdue":
                loans = loans.Where(l => l.DateReturned == null && l.DueDate < DateTime.Now);
                break;

            case "borrowing":
                loans = loans.Where(l => l.DateReturned == null && l.DueDate >= DateTime.Now);
                break;
        }

        return loans.ToList();
    }

    #endregion

    #region Member CRUD
    public async Task<IActionResult> UpsertMember(int? id)
    {
        MemberViewModel vm = new MemberViewModel
        {
            Member = new Member(),
            RoleList = new SelectList(db.Roles.ToList(), "RoleId", "RoleName")
        };
        if (id != null)
        {
            vm.Member = await db.Members.FindAsync(id);
            if (vm.Member == null) return NotFound();
        }
        ViewData["Title"] = id == null ? "Thêm thành viên mới" : "Chỉnh sửa thành viên";
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> UpsertMember(MemberViewModel vm)
    {
        ModelState.Remove("Member.Role");
        // CẢI TIẾN: Kiểm tra email trùng lặp khi tạo mới
        if (vm.Member.MemberId == 0 && await db.Members.AnyAsync(m => m.Email == vm.Member.Email))
        {
            ModelState.AddModelError("Member.Email", "Email này đã được sử dụng.");
        }

        // Bỏ qua validation cho password khi edit mà không thay đổi
        if (vm.Member.MemberId != 0 && string.IsNullOrEmpty(vm.Member.Password))
        {
            ModelState.Remove("Member.Password");
        }

        if (ModelState.IsValid)
        {
            if (vm.Member.MemberId == 0) // Thêm mới
            {
                // LƯU Ý BẢO MẬT: Cần mã hóa mật khẩu trước khi lưu vào DB
                // vm.Member.Password = HashPassword(vm.Member.Password);
                vm.Member.DateCreated = DateTime.Now;
                await db.Members.AddAsync(vm.Member);
                TempData["SuccessMessage"] = "Thêm thành viên mới thành công!";
            }
            else // Cập nhật
            {
                var memberFromDb = await db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.MemberId == vm.Member.MemberId);
                if (string.IsNullOrEmpty(vm.Member.Password))
                {
                    // Giữ lại mật khẩu cũ nếu không nhập mật khẩu mới
                    vm.Member.Password = memberFromDb.Password;
                }
                else
                {
                    // LƯU Ý BẢO MẬT: Cần mã hóa mật khẩu mới
                    // vm.Member.Password = HashPassword(vm.Member.Password);
                }
                vm.Member.DateCreated = memberFromDb.DateCreated; // Giữ lại ngày tạo ban đầu
                db.Members.Update(vm.Member);
                TempData["SuccessMessage"] = "Cập nhật thành viên thành công!";
            }
            await db.SaveChangesAsync();
            return RedirectToAction("Index", new { module = "member" });
        }

        vm.RoleList = new SelectList(db.Roles.ToList(), "RoleId", "RoleName");
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMember(int id)
    {
        var member = await db.Members.FindAsync(id);
        if (member != null)
        {
            db.Members.Remove(member);
            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa thành viên '{member.Name}'!";
        }
        return RedirectToAction("Index", new { module = "member" });
    }

    public IActionResult ExportMemberExcel(string searchString = "")
    {
        var members = db.Members
            .Include(m => m.Role)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
            members = members.Where(m =>
                m.Name.Contains(searchString) ||
                m.Email.Contains(searchString)
            );

        var data = members.ToList();

        using var package = new OfficeOpenXml.ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Members");

        // Header
        ws.Cells[1, 1].Value = "ID";
        ws.Cells[1, 2].Value = "Tên";
        ws.Cells[1, 3].Value = "Email";
        ws.Cells[1, 4].Value = "Số điện thoại";
        ws.Cells[1, 5].Value = "Quyền";
        ws.Cells[1, 6].Value = "Ngày tạo";

        int row = 2;
        foreach (var m in data)
        {
            ws.Cells[row, 1].Value = m.MemberId;
            ws.Cells[row, 2].Value = m.Name;
            ws.Cells[row, 3].Value = m.Email;
            ws.Cells[row, 4].Value = m.PhoneNumber;
            ws.Cells[row, 5].Value = m.Role?.RoleName;
            ws.Cells[row, 6].Value = m.DateCreated.ToString("dd/MM/yyyy");
            row++;
        }

        ws.Cells.AutoFitColumns();

        var stream = new MemoryStream(package.GetAsByteArray());

        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MemberList.xlsx");
    }

    [HttpGet]
    public IActionResult ExportMemberPdf(string searchString = "")
    {
        var list = db.Members
            .Include(m => m.Role)
            .Where(m => string.IsNullOrEmpty(searchString) || m.Name.Contains(searchString))
            .ToList();

        return File(GenerateMemberPdf(list), "application/pdf", "DanhSachThanhVien.pdf");
    }

    private byte[] GenerateMemberPdf(List<Member> members)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .AlignCenter()
                    .Text("DANH SÁCH THÀNH VIÊN")
                    .Bold().FontSize(22).FontColor(Colors.Blue.Medium);

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(40); // ID
                        c.RelativeColumn(3);  // Tên
                        c.RelativeColumn(3);  // Email
                        c.RelativeColumn(2);  // SĐT
                        c.RelativeColumn(2);  // Vai trò
                        c.RelativeColumn(2);  // Ngày tạo
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#F0F0F0").Padding(5).Text("ID").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Tên").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Email").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("SĐT").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Vai trò").Bold();
                        header.Cell().Background("#F0F0F0").Padding(5).Text("Ngày tạo").Bold();
                    });

                    foreach (var m in members)
                    {
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(m.MemberId.ToString());
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(m.Name);
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(m.Email);
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(m.PhoneNumber);
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(m.Role?.RoleName ?? "");
                        table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(m.DateCreated.ToString("dd/MM/yyyy"));
                    }
                });
            });
        });

        return doc.GeneratePdf();
    }

    #endregion

    #region CategoryGroup CRUD
    // GET: /Admin/UpsertCategoryGroup/5 (Edit) hoặc /Admin/UpsertCategoryGroup (Create)
    public async Task<IActionResult> UpsertCategoryGroup(int? id)
    {
        CategoryGroup group = new CategoryGroup();
        if (id != null)
        {
            group = await db.CategoryGroups.FindAsync(id);
            if (group == null) return NotFound();
        }
        ViewData["Title"] = id == null ? "Thêm Nhóm Thể loại mới" : "Chỉnh sửa Nhóm Thể loại";
        return View(group);
    }

    // POST: /Admin/UpsertCategoryGroup
    [HttpPost]
    public async Task<IActionResult> UpsertCategoryGroup(CategoryGroup group)
    {
        ModelState.Remove("Categories");
        if (ModelState.IsValid)
        {
            if (group.CategoryGroupId == 0)
            {
                await db.CategoryGroups.AddAsync(group);
                TempData["SuccessMessage"] = "Thêm Nhóm Thể loại thành công!";
            }
            else
            {
                db.CategoryGroups.Update(group);
                TempData["SuccessMessage"] = "Cập nhật Nhóm Thể loại thành công!";
            }
            await db.SaveChangesAsync();
            return RedirectToAction("Index", new { module = "categorygroup" });
        }
        return View(group);
    }

    // GET: /Admin/DetailsCategoryGroup/5
    public async Task<IActionResult> DetailsCategoryGroup(int id)
    {
        var groupWithCategories = await db.CategoryGroups
            .Include(cg => cg.Categories) // Quan trọng: Lấy kèm danh sách các thể loại con
            .FirstOrDefaultAsync(cg => cg.CategoryGroupId == id);

        if (groupWithCategories == null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Chi tiết Nhóm: {groupWithCategories.Name}";
        return View(groupWithCategories);
    }

    // POST: /Admin/DeleteCategoryGroup/5
    [HttpPost]
    public async Task<IActionResult> DeleteCategoryGroup(int id)
    {
        var group = await db.CategoryGroups.FindAsync(id);
        if (group != null)
        {
            // Cảnh báo: Nếu có thể loại đang thuộc nhóm này, việc xóa sẽ gây lỗi.
            // Cần có logic xử lý nâng cao hơn trong dự án thực tế.
            db.CategoryGroups.Remove(group);
            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa Nhóm Thể loại thành công!";
        }
        return RedirectToAction("Index", new { module = "categorygroup" });
    }
    #endregion

    #region exportExcel/PDF
    public IActionResult ExportExcel(string module, string searchString = "", int memberId = 0, string status = "")
    {
        switch (module.ToLower())
        {
            case "loan":
                return ExportLoanExcel(searchString, memberId, status);
            case "book":
                return ExportBookExcel(searchString);
            case "member":
                return ExportMemberExcel(searchString);
            case "category":
                return ExportCategoryExcel(searchString);
            case "author":
                return ExportAuthorExcel(searchString);
            case "publisher":
                return ExportPublisherExcel(searchString);
            default:
                return BadRequest("Module không hợp lệ.");
        }
    }

    [HttpGet]
    public IActionResult ExportPdf(string module, string searchString = "", int memberId = 0, string status = "")
    {
        module = (module ?? "").ToLower();

        switch (module)
        {
            case "book":
                {
                    var books = db.Books
                        .Include(b => b.Publisher)
                        .AsQueryable();

                    if (!string.IsNullOrEmpty(searchString))
                        books = books.Where(b => b.Title.Contains(searchString));

                    var list = books.ToList();
                    var bytes = GenerateBookPdf(list);
                    return File(bytes, "application/pdf", "DanhSachSach.pdf");
                }

            case "category":
                {
                    var categories = db.Categories
                        .Include(c => c.CategoryGroup)
                        .AsQueryable();

                    if (!string.IsNullOrEmpty(searchString))
                        categories = categories.Where(c => c.Name.Contains(searchString));

                    var list = categories.ToList();
                    var bytes = GenerateCategoryPdf(list);
                    return File(bytes, "application/pdf", "DanhSachTheLoai.pdf");
                }

            case "author":
                {
                    var authors = db.Authors.AsQueryable();
                    if (!string.IsNullOrEmpty(searchString))
                        authors = authors.Where(a => a.Name.Contains(searchString));

                    var list = authors.ToList();
                    var bytes = GenerateAuthorPdf(list);
                    return File(bytes, "application/pdf", "DanhSachTacGia.pdf");
                }

            case "publisher":
                {
                    var publishers = db.Publishers.AsQueryable();
                    if (!string.IsNullOrEmpty(searchString))
                        publishers = publishers.Where(p => p.Name.Contains(searchString));

                    var list = publishers.ToList();
                    var bytes = GeneratePublisherPdf(list);
                    return File(bytes, "application/pdf", "DanhSachNXB.pdf");
                }

            case "member":
                {
                    var members = db.Members
                        .Include(m => m.Role)
                        .AsQueryable();

                    if (!string.IsNullOrEmpty(searchString))
                        members = members.Where(m =>
                            m.Name.Contains(searchString) ||
                            m.Email.Contains(searchString));

                    var list = members.ToList();
                    var bytes = GenerateMemberPdf(list);
                    return File(bytes, "application/pdf", "DanhSachThanhVien.pdf");
                }

            case "loan":
                {
                    var loans = GetFilteredLoans(searchString, memberId, status).ToList();
                    var bytes = GenerateLoanPdf(loans);
                    return File(bytes, "application/pdf", "DanhSachMuonTra.pdf");
                }

            default:
                return BadRequest("Module không hợp lệ.");
        }
    }
    #endregion

    private static IContainer CellStyle(IContainer container)
    {
        return container
            .Padding(6)
            .BorderBottom(1)
            .BorderColor("#E0E0E0")
            .DefaultTextStyle(x => x.FontSize(10));
    }
}
