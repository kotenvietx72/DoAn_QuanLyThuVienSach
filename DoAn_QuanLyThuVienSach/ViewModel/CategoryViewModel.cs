using DoAn_QuanLyThuVienSach.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DoAn_QuanLyThuVienSach.ViewModel
{
    public class CategoryViewModel
    {
        public Category Category { get; set; } = new Category();

        public SelectList? CategoryGroupList { get; set; }
    }
}
