using DoAn_QuanLyThuVienSach.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DoAn_QuanLyThuVienSach.ViewModel
{
    public class MemberViewModel
    {
        public Member Member { get; set; } = new Member();

        public SelectList? RoleList { get; set; }
    }
}
