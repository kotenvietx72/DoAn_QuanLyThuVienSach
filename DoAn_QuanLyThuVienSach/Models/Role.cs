using System.ComponentModel.DataAnnotations;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        public string RoleName { get; set; }

        public virtual ICollection<Member> Members { get; set; } = new List<Member>();

        public Role() { }
    }
}
