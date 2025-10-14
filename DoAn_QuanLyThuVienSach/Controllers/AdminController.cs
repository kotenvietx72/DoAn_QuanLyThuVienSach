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

    #region Trang chính & Tìm kiếm
    public IActionResult Index(string module = "book", string searchString = "")
    {
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
                var loans = db.Loans.Include(l => l.Book).Include(l => l.Member).AsQueryable();
                if (!string.IsNullOrEmpty(searchString))
                    loans = loans.Where(l => l.Book.Title.Contains(searchString) || l.Member.Name.Contains(searchString));
                ViewData["ModuleData"] = loans.ToList();
                ViewData["Title"] = "Quản lý Mượn Sách";
                ViewData["PartialViewName"] = "_LoanList";
                break;
            case "member":
                var members = db.Members.Include(m => m.Role).AsQueryable();
                if (!string.IsNullOrEmpty(searchString))
                    members = members.Where(m => m.Name.Contains(searchString) || m.Email.Contains(searchString));
                ViewData["ModuleData"] = members.ToList();
                ViewData["Title"] = "Quản lý Tài khoản";
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
}
