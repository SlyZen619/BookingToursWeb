using System.ComponentModel.DataAnnotations;

namespace BookingToursWeb.Models
{
    public class ResetPasswordViewModel
    {
        // Chúng ta sẽ truyền Email qua đây để biết người dùng nào cần đổi mật khẩu
        [Required]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public required string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.")]
        public required string ConfirmPassword { get; set; }
    }
}