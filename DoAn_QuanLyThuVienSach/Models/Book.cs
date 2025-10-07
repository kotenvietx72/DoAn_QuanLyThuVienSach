using System.ComponentModel.DataAnnotations;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class Book
    {
        [Key]
        public int BookId { get; set; }

        public string Title { get; set; }

        public int PublisherId { get; set; }

        public int PublicationYear { get; set; }

        public string Description { get; set; }

        public string CoverImage { get; set; }

        public int TotalCopies { get; set; }

        public virtual Publisher Publisher { get; set; } = null!;

        public ICollection<BookAuthor> BookAuthors { get; set; } = null!;

        public ICollection<BookCategory> BookCategories { get; set; } = null!;

        public virtual ICollection<Loan> Loans { get; set; } = null!;

        public Book() { }
    }
}
