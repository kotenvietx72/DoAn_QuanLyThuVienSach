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

        public virtual Publisher Publisher { get; set; }

        public ICollection<BookAuthor> BookAuthors { get; set; }

        public ICollection<BookCategory> BookCategories { get; set; }

        public virtual ICollection<Loan> Loans { get; set; }

        public Book() {
            BookId = 0;
            Title = string.Empty;
            PublisherId = 0;
            PublicationYear = 0;
            Description = string.Empty;
            CoverImage = string.Empty;
            TotalCopies = 0;
            Publisher = new Publisher();
            BookAuthors = new List<BookAuthor>();
            BookCategories = new List<BookCategory>();
            Loans = new List<Loan>();
        }
    }
}
