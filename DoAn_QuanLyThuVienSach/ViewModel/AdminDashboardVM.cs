namespace DoAn_QuanLyThuVienSach.ViewModel
{
    public class AdminDashboardVM
    {
        public int TotalBooks { get; set; }

        public int TotalMembers { get; set; }

        public int TotalLoans { get; set; }

        public string CurrentModule { get; set; } = "book";
    }
}
