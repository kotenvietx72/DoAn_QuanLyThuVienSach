using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAn_QuanLyThuVienSach.Models
{
    public class Member
    {
        [Key]
        public int MemberId { get; set; }

        [ForeignKey("Role")]
        public int RoleId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public DateTime DateCreated { get; set; }

        public virtual Role Role { get; set; } = null!;

        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();

        public Member() { }
    }
}
