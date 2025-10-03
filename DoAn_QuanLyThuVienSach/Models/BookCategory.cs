using System.ComponentModel.DataAnnotations.Schema;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class BookCategory
    {
        [ForeignKey("Book")]
        public int BookId { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        public virtual Book Book { get; set; }

        public virtual Category Category { get; set; }

        public BookCategory()
        {
            BookId = 0;
            CategoryId = 0;
            Book = new Book();
            Category = new Category();
        }
    }
}
