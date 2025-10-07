using System.ComponentModel.DataAnnotations;

namespace DoAn_QuanLyThuVienSach.ViewModel
{
    public class LoanViewModel
    {
        // Dữ liệu ẩn, dùng để xử lý
        public int BookId { get; set; }

        // Dữ liệu hiển thị cho người dùng
        public string BookTitle { get; set; }
        public string CoverImage { get; set; }

        [Display(Name = "Ngày Mượn")]
        [DataType(DataType.Date)]
        public DateTime DateBorrowed { get; set; }

        // Dữ liệu người dùng nhập
        [Required(ErrorMessage = "Vui lòng chọn ngày hẹn trả.")]
        [Display(Name = "Ngày Hẹn Trả")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
        [Display(Name = "Số Lượng")]
        [Range(1, 5, ErrorMessage = "Số lượng mượn phải từ 1 đến 5.")]
        public int Quantity { get; set; }

        public LoanViewModel()
        {
            BookId = 1;
            BookTitle = string.Empty;
            CoverImage = string.Empty;
            DateBorrowed = DateTime.Now;
            DueDate = DateTime.Now.AddDays(14);
            Quantity = 1;
        }
    }
}
