using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingToursWeb.Models
{
    public class User
    {
        [Key] // Đánh dấu Id là khóa chính
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Tự động tăng
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Username { get; set; }

        [Required]
        [StringLength(255)]
        [EmailAddress] // Thêm validation cho định dạng email
        public required string Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; } // Dấu '?' cho phép null

        [Required]
        [StringLength(255)]
        public required string PasswordHash { get; set; }

        [Required]
        [Column(TypeName = "bit")] // Khai báo kiểu dữ liệu SQL là bit
        public bool IsAdmin { get; set; } = false; // Mặc định là người dùng thường

        // --- Thuộc tính điều hướng (Navigation Properties) cho mối quan hệ ---
        // Một User có thể có nhiều Bookings
        public ICollection<Booking>? Bookings { get; set; }

        // Một User có thể có nhiều Reviews
        public ICollection<Review>? Reviews { get; set; }

        // Một User (Admin) có thể có nhiều Posts
        public ICollection<Post>? Posts { get; set; }
    }
}