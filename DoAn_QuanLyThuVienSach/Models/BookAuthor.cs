using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class BookAuthor
    {
        [ForeignKey("Book")]
        public int BookId { get; set; }

        [ForeignKey("Author")]
        public int AuthorId { get; set; }

        public virtual Book Book { get; set; } = null!;

        public virtual Author Author { get; set; } = null!;

        public BookAuthor() { }
    }
}
