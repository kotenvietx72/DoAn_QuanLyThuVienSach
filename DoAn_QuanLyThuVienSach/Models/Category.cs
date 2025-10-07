using System.ComponentModel.DataAnnotations;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int CategoryGroupId { get; set; }

        public ICollection<BookCategory> BookCategories { get; set; } = null!;

        public CategoryGroup CategoryGroup { get; set; } = null!;

        public Category() { }
    }
}
