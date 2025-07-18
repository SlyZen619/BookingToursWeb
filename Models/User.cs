using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace BookingToursWeb.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Username { get; set; }

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public required string Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(255)]
        public required string PasswordHash { get; set; }

        [Required]
        [Column(TypeName = "bit")]
        [Display(Name = "Là Admin")]
        public bool IsAdmin { get; set; } = false; // Mặc định là người dùng thường

        // Cột mới để xác định quản lý địa điểm
        [Required]
        [Column(TypeName = "bit")]
        [Display(Name = "Là Quản lý Địa điểm")]
        public bool IsLocationManager { get; set; } = false; // Mặc định là người dùng thường

        // Khóa ngoại đến Location.Id cho người dùng có vai trò LocationManager
        // Cho phép null vì không phải tất cả người dùng đều là quản lý địa điểm
        [Display(Name = "ID Địa điểm quản lý")]
        public int? ManagedLocationId { get; set; }

        [ForeignKey("ManagedLocationId")]
        // Thuộc tính điều hướng để truy cập đối tượng Location mà người này quản lý
        public Location? ManagedLocation { get; set; }

        // --- Thuộc tính điều hướng (Navigation Properties) ---
        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<Post>? Posts { get; set; }
    }
}