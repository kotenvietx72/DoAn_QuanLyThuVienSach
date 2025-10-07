using DoAn_QuanLyThuVienSach.Data;
using DoAn_QuanLyThuVienSach.Models;
using DoAn_QuanLyThuVienSach.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class AdminController : Controller
{
    private readonly DataContext db;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminController(DataContext db, IWebHostEnvironment webHostEnvironment)
    {
        this.db = db;
        _webHostEnvironment = webHostEnvironment;
    }

    public IActionResult Index(string module = "book", string searchString = "")
    {
        ViewData["CurrentFilter"] = searchString;
        ViewData["CurrentModule"] = module;

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
        return View();
    }

    // GET: /Admin/UpsertBook/5 (Edit) hoặc /Admin/UpsertBook (Create)
    public async Task<IActionResult> UpsertBook(int? id)
    {
        BookViewModel vm = new BookViewModel
        {
            AllPublishers = new SelectList(db.Publishers.ToList(), "Name", "Name"),
            AllCategories = new SelectList(db.Categories.ToList(), "Name", "Name")
        };

        if (id != null) // Trường hợp Edit
        {
            vm.Book = await db.Books
                .Include(b => b.Publisher)
                .Include(b => b.BookCategories)
                .ThenInclude(bc => bc.Category)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (vm.Book == null) return NotFound();

            // Điền lại các giá trị đã chọn
            vm.PublisherName = vm.Book.Publisher?.Name;
            vm.CategoryNames = vm.Book.BookCategories.Select(bc => bc.Category.Name).ToList();
        }

        ViewData["Title"] = id == null ? "Thêm sách mới" : "Chỉnh sửa sách";
        return View(vm);
    }

    // POST: /Admin/UpsertBook
    [HttpPost]
    public async Task<IActionResult> UpsertBook(BookViewModel vm)
    {
        ModelState.Remove("Book.Publisher");
        ModelState.Remove("Book.BookCategories");

        ModelState.Remove("AllPublishers");
        ModelState.Remove("AllCategories");

        if (ModelState.IsValid)
        {
            // --- Xử lý Publisher (One-to-Many) ---
            if (!string.IsNullOrEmpty(vm.PublisherName))
            {
                var publisher = await db.Publishers.FirstOrDefaultAsync(p => p.Name == vm.PublisherName);
                if (publisher == null)
                {
                    publisher = new Publisher { Name = vm.PublisherName };
                    db.Publishers.Add(publisher);
                    await db.SaveChangesAsync();
                }
                vm.Book.PublisherId = publisher.PublisherId;
            }

            // --- Xử lý upload ảnh ---
            if (vm.CoverImageFile != null)
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
            }

            // --- Xử lý Thêm mới hoặc Cập nhật Sách ---
            if (vm.Book.BookId == 0) // Thêm mới
            {
                db.Books.Add(vm.Book);
                await db.SaveChangesAsync(); // Lưu sách để lấy BookId mới
            }
            else // Cập nhật
            {
                db.Books.Update(vm.Book);
                // Xóa các thể loại cũ để cập nhật lại
                var existingCategories = db.BookCategories.Where(bc => bc.BookId == vm.Book.BookId);
                db.BookCategories.RemoveRange(existingCategories);
            }

            // --- Xử lý Categories (Many-to-Many) ---
            if (vm.CategoryNames != null)
            {
                foreach (var catName in vm.CategoryNames)
                {
                    var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == catName);
                    if (category == null)
                    {
                        category = new Category { Name = catName };
                        db.Categories.Add(category);
                        await db.SaveChangesAsync();
                    }
                    db.BookCategories.Add(new BookCategory { BookId = vm.Book.BookId, CategoryId = category.CategoryId });
                }
            }

            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = vm.Book.BookId == 0 ? "Thêm sách thành công!" : "Cập nhật sách thành công!";
            return RedirectToAction("Index", new { module = "book" });
        }

        // Nếu vẫn lỗi, load lại và quay về form
        ViewData["Title"] = vm.Book.BookId == 0 ? "Thêm sách mới" : "Chỉnh sửa sách";
        vm.AllPublishers = new SelectList(db.Publishers.ToList(), "Name", "Name", vm.PublisherName);
        vm.AllCategories = new SelectList(db.Categories.ToList(), "Name", "Name", vm.CategoryNames);
        return View("UpsertBook", vm);
    }

    // POST: /Admin/DeleteBook/5
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

    // GET: /Admin/UpsertCategory/5 (Edit) hoặc /Admin/UpsertCategory (Create)
    public async Task<IActionResult> UpsertCategory(int? id)
    {
        Category category = new Category();
        if (id != null)
        {
            category = await db.Categories.FindAsync(id);
            if (category == null) return NotFound();
        }
        ViewData["Title"] = id == null ? "Thêm thể loại mới" : "Chỉnh sửa thể loại";
        return View(category);
    }

    // POST: /Admin/UpsertCategory
    [HttpPost]
    public async Task<IActionResult> UpsertCategory(Category category)
    {
        if (ModelState.IsValid)
        {
            if (category.CategoryId == 0) // Thêm mới
            {
                await db.Categories.AddAsync(category);
                TempData["SuccessMessage"] = "Thêm thể loại thành công!";
            }
            else // Cập nhật
            {
                db.Categories.Update(category);
                TempData["SuccessMessage"] = "Cập nhật thể loại thành công!";
            }
            await db.SaveChangesAsync();
            return RedirectToAction("Index", new { module = "category" });
        }
        return View(category);
    }

    // POST: /Admin/DeleteCategory/5
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

    public async Task<IActionResult> EditAuthor(int id)
    {
        var author = await db.Authors.FindAsync(id);
        if (author == null) return NotFound();
        ViewData["Title"] = "Chỉnh sửa tác giả";
        return View(author);
    }

    // POST: /Admin/EditAuthor/5
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

    // GET: /Admin/EditPublisher/5
    public async Task<IActionResult> EditPublisher(int id)
    {
        var publisher = await db.Publishers.FindAsync(id);
        if (publisher == null) return NotFound();
        ViewData["Title"] = "Chỉnh sửa nhà xuất bản";
        return View(publisher);
    }

    // POST: /Admin/EditPublisher/5
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

    // GET: /Admin/EditLoan/5
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

    // POST: /Admin/EditLoan/5
    [HttpPost]
    public async Task<IActionResult> EditLoan(Loan loan)
    {
        if (ModelState.IsValid)
        {
            db.Loans.Update(loan);
            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật lượt mượn thành công!";
            return RedirectToAction("Index", new { module = "loan" });
        }
        return View(loan);
    }

    // GET: /Admin/UpsertMember/5 (Edit) hoặc /Admin/UpsertMember (Create)
    public async Task<IActionResult> UpsertMember(int? id)
    {
        MemberViewModel vm = new MemberViewModel
        {
            Member = new Member(),
            RoleList = new SelectList(db.Roles.ToList(), "RoleId", "RoleName")
        };

        if (id == null) // Thêm mới
        {
            ViewData["Title"] = "Thêm thành viên mới";
            return View(vm);
        }
        else // Chỉnh sửa
        {
            vm.Member = await db.Members.FindAsync(id);
            if (vm.Member == null) return NotFound();
            ViewData["Title"] = "Chỉnh sửa thành viên";
            return View(vm);
        }
    }

    // POST: /Admin/UpsertMember
    [HttpPost]
    public async Task<IActionResult> UpsertMember(MemberViewModel vm)
    {
        if (ModelState.IsValid)
        {
            if (vm.Member.MemberId == 0) // Thêm mới
            {
                // Lưu ý: Cần xử lý mã hóa mật khẩu ở đây trong một dự án thực tế
                await db.Members.AddAsync(vm.Member);
                TempData["SuccessMessage"] = "Thêm thành viên mới thành công!";
            }
            else // Cập nhật
            {
                db.Members.Update(vm.Member);
                TempData["SuccessMessage"] = "Cập nhật thành viên thành công!";
            }
            await db.SaveChangesAsync();
            return RedirectToAction("Index", new { module = "member" });
        }

        vm.RoleList = new SelectList(db.Roles.ToList(), "RoleId", "RoleName");
        return View(vm);
    }

    // POST: /Admin/DeleteMember/5
    [HttpPost]
    public async Task<IActionResult> DeleteMember(int id)
    {
        var member = await db.Members.FindAsync(id);
        if (member != null)
        {
            // Cẩn thận: Nên có logic kiểm tra xem thành viên có đang mượn sách không trước khi xóa
            db.Members.Remove(member);
            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa thành viên '{member.Name}'!";
        }
        return RedirectToAction("Index", new { module = "member" });
    }

}