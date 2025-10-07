using System.ComponentModel.DataAnnotations.Schema;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class BookCategory
    {
        [ForeignKey("Book")]
        public int BookId { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        public virtual Book Book { get; set; } = null!;

        public virtual Category Category { get; set; } = null!;

        public BookCategory() { }
    }
}
