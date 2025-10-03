using System.ComponentModel.DataAnnotations;

namespace DoAn_QuanLyThuVienSach.ViewModel
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [Display(Name = "Họ và Tên")]
        public string Name { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; } // Email không cho phép sửa

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        // --- PHẦN MỚI ĐỂ ĐỔI MẬT KHẨU ---

        [Display(Name = "Mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; } // Dấu ? cho biết trường này không bắt buộc

        [Display(Name = "Mật khẩu mới")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
        public string? NewPassword { get; set; } // Dấu ? cho biết trường này không bắt buộc

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.")]
        public string? ConfirmNewPassword { get; set; } // Dấu ? cho biết trường này không bắt buộc

        public ProfileViewModel() {
            Name = string.Empty;
            Email = string.Empty;
            PhoneNumber = string.Empty;
            Address = string.Empty;
        }
    }
}
