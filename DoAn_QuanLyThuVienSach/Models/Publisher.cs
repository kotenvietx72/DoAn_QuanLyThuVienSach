using System.ComponentModel.DataAnnotations;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class Publisher
    {
        [Key]
        public int PublisherId { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public virtual ICollection<Book> Books { get; set; }

        public Publisher()
        {
            PublisherId = 0;
            Name = string.Empty;
            Address = string.Empty;
            Books = new List<Book>();
        }
    }
}
