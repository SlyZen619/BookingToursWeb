// Models/EditProfileViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace BookingToursWeb.Models
{
    public class EditProfileViewModel
    {
        public int Id { get; set; } // Id của người dùng (dùng để xác định người dùng cần sửa)

        [Required(ErrorMessage = "Tên tài khoản không được để trống.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên tài khoản phải dài từ 3 đến 50 ký tự.")]
        [Display(Name = "Tên tài khoản")]
        public required string Username { get; set; } // Bỏ 'readonly' ở đây

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [Display(Name = "Email")]
        public required string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; } // Dùng string? nếu có thể null
        // Bạn có thể thêm các trường khác nếu User model của bạn có, ví dụ FullName, Address...

        // KHÔNG BAO GỒM PASSWORD Ở ĐÂY CHO MỤC ĐÍCH CHỈNH SỬA THÔNG TIN CƠ BẢN
        // Đổi mật khẩu sẽ có một ViewModel và Action riêng
    }
}