using System.ComponentModel.DataAnnotations;

namespace BookingToursWeb.Models
{
    public class AdminAddUserViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "{0} phải dài ít nhất {2} và tối đa {1} ký tự.")]
        [Display(Name = "Tên đăng nhập")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100, ErrorMessage = "{0} không được vượt quá {1} ký tự.")]
        [Display(Name = "Email")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
        public required string ConfirmPassword { get; set; }

        // Đã thay đổi PhoneNumber từ required string sang string? (nullable) để đồng bộ với User model
        // Nếu bạn muốn nó Required, hãy giữ nguyên Required attribute và bỏ '?'
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")] // Giữ nguyên Required nếu bạn muốn Phone Number là bắt buộc
        [StringLength(20, ErrorMessage = "{0} không được vượt quá {1} ký tự.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; } // Đã sửa thành nullable để phù hợp với User model và tính linh hoạt

        [Display(Name = "Là Quản trị viên")]
        public bool IsAdmin { get; set; } = false; // Mặc định không phải Admin, Admin có thể tích chọn

        // === THÊM THUỘC TÍNH MỚI CHO LOCATION MANAGER ===
        [Display(Name = "Là Quản lý Địa điểm")]
        public bool IsLocationManager { get; set; } = false; // Mặc định không phải Quản lý Địa điểm
        // === HẾT THÊM ===
    }
}