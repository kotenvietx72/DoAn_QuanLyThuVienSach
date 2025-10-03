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

        public ICollection<BookCategory> BookCategories { get; set; }

        public CategoryGroup CategoryGroup { get; set; }

        public Category() {
            CategoryId = 0;
            CategoryGroupId = 0;
            Name = string.Empty;
            Description = string.Empty;
            BookCategories = new List<BookCategory>();
            CategoryGroup = new CategoryGroup();
        }
    }
}
