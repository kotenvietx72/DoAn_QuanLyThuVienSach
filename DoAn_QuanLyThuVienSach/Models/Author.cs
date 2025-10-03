using System.ComponentModel.DataAnnotations;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class Author
    {
        [Key]
        public int AuthorId { get; set; }

        public string Name { get; set; }

        public string Bio { get; set; }

        public ICollection<BookAuthor> BookAuthors { get; set; }

        public Author() {
            AuthorId = 0;
            Name = string.Empty;
            Bio = string.Empty;
            BookAuthors = new List<BookAuthor>();
        }
    }
}
