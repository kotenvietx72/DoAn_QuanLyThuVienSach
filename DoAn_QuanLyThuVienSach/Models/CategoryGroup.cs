using System.ComponentModel.DataAnnotations;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class CategoryGroup
    {
        [Key]
        public int CategoryGroupId { get; set; }

        public string Name { get; set; }

        public string IconClass { get; set; }

        public ICollection<Category> Categories { get; set; } = null!;

        public CategoryGroup() { }
    }
}
