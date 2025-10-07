using System.ComponentModel.DataAnnotations;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class Publisher
    {
        [Key]
        public int PublisherId { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public virtual ICollection<Book> Books { get; set; } = null!;

        public Publisher() { }
    }
}
