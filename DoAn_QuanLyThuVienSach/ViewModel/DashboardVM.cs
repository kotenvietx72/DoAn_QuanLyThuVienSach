namespace DoAn_QuanLyThuVienSach.ViewModel
{
    public class DashboardVM
    {
        // KPI
        public int TotalBooks { get; set; }
        public int BorrowingCount { get; set; }
        public int OverdueCount { get; set; }
        public int TotalMembers { get; set; }

        // Biểu đồ mượn / trả 14 ngày gần nhất
        public List<string> Last14DaysLabels { get; set; } = new();
        public List<int> BorrowCounts { get; set; } = new();
        public List<int> ReturnCounts { get; set; } = new();

        // Biểu đồ tình trạng
        public int StatusReturned { get; set; }
        public int StatusOverdue { get; set; }
        public int StatusBorrowing { get; set; }

        // Top sách
        public List<string> TopBookLabels { get; set; } = new();
        public List<int> TopBookCounts { get; set; } = new();

        // Top thể loại
        public List<string> TopCategoryLabels { get; set; } = new();
        public List<int> TopCategoryCounts { get; set; } = new();
    }
}
