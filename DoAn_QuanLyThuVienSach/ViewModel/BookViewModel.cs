using DoAn_QuanLyThuVienSach.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DoAn_QuanLyThuVienSach.ViewModel
{
    public class BookViewModel
    {
        public Book Book { get; set; } = new Book();

        public SelectList? PublisherList { get; set; }

        public IFormFile? CoverImageFile { get; set; }

        public SelectList? AllPublishers { get; set; }

        public SelectList? AllCategories { get; set; }

        // Nhận về TÊN của nhà xuất bản (có thể là tên cũ hoặc tên mới)
        public string? PublisherName { get; set; }

        // Nhận về MỘT DANH SÁCH TÊN các thể loại
        public List<string> CategoryNames { get; set; } = new List<string>();
    }
}
