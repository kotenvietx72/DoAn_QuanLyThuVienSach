namespace DoAn_QuanLyThuVienSach.ViewModel
{
    public class CategoryGroupMenuVM
    {
        public int CategoryGroupId { get; set; }

        public string Name { get; set; }

        public string IconClass { get; set; }

        public int BookCount { get; set; } 

        public List<DoAn_QuanLyThuVienSach.Models.Category> Categories { get; set; }

        public CategoryGroupMenuVM() {
            CategoryGroupId = 0;
            Name = string.Empty;
            IconClass = string.Empty;
            BookCount = 0;
            Categories = new List<Models.Category> { };
        }
    }
}
