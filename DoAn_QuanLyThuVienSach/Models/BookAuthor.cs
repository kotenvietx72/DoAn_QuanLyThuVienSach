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

        public virtual Book Book { get; set; }

        public virtual Author Author { get; set; }

        public BookAuthor()
        {
            BookId = 0;
            AuthorId = 0;
            Book = new Book();
            Author = new Author();
        }
    }
}
