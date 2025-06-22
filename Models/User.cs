using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingToursWeb.Models
{
    [Table("Users")] // Map class User với bảng Users trong CSDL
    public class User
    {
        [Key] // Đánh dấu Id là khóa chính
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [StringLength(100)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [StringLength(255)]
        public string Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; } // Dùng ? cho phép null

        [Required(ErrorMessage = "Mật khẩu hash không được để trống.")]
        [StringLength(255)] // Độ dài đủ để lưu mật khẩu đã hash
        public string PasswordHash { get; set; }

        public bool IsAdmin { get; set; } = false; // Mặc định là false (người dùng thường)
    }
}