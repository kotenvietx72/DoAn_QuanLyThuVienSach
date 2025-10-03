using System.ComponentModel.DataAnnotations;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        public string RoleName { get; set; }

        public virtual ICollection<Member> Members { get; set; }

        public Role()
        {
            RoleId = 0;
            RoleName = string.Empty;
            Members = new List<Member>();
        }
    }
}
