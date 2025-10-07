using DoAn_QuanLyThuVienSach.Models;

namespace DoAn_QuanLyThuVienSach.ViewModel
{
    public class ManageLoanViewModel
    {
        public List<Member> MembersWithLoans { get; set; } = new List<Member>();

        public List<Loan> SelectedMemberLoans { get; set; } = new List<Loan>();

        public Member? SelectedMember { get; set; }
    }
}
