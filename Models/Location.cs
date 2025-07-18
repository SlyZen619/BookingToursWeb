using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Đảm bảo có dòng này

namespace BookingToursWeb.Models
{
    [Table("Locations")]
    public class Location
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên địa điểm không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên địa điểm không được vượt quá 200 ký tự.")]
        [Display(Name = "Tên Địa điểm")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả không được để trống.")]
        [StringLength(1000, ErrorMessage = "Mô tả ngắn gọn không được vượt quá 1000 ký tự.")]
        [Display(Name = "Mô tả ngắn gọn")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thông tin chi tiết không được để trống.")]
        [Display(Name = "Thông tin chi tiết")]
        [Column(TypeName = "nvarchar(MAX)")]
        public string Information { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, 9999999999999996.99, ErrorMessage = "Giá vé phải là số không âm.")]
        [Display(Name = "Giá vé")]
        public decimal? TicketPrice { get; set; }

        [Required(ErrorMessage = "Giờ mở cửa không được để trống.")]
        [StringLength(100, ErrorMessage = "Giờ mở cửa không được vượt quá 100 ký tự.")]
        [Display(Name = "Giờ mở cửa")]
        public string OpeningHours { get; set; } = string.Empty;

        [Required(ErrorMessage = "URL Hình ảnh không được để trống.")]
        [StringLength(500, ErrorMessage = "URL Hình ảnh không được vượt quá 500 ký tự.")]
        [Display(Name = "URL Hình ảnh")]
        public string ImageUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thông tin liên hệ không được để trống.")]
        [StringLength(200, ErrorMessage = "Thông tin liên hệ không được vượt quá 200 ký tự.")]
        [Display(Name = "Thông tin liên hệ")]
        public string ContactInfo { get; set; } = string.Empty;

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; } = true;

        [Column(TypeName = "decimal(9, 6)")]
        [Display(Name = "Vĩ độ")]
        [Range(-90.0, 90.0, ErrorMessage = "Vĩ độ phải nằm trong khoảng từ -90 đến 90.")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(9, 6)")]
        [Display(Name = "Kinh độ")]
        [Range(-180.0, 180.0, ErrorMessage = "Kinh độ phải nằm trong khoảng từ -180 đến 180.")]
        public decimal? Longitude { get; set; }

        // --- Các thuộc tính mới cho thông tin thanh toán VietQR ---
        [StringLength(100)]
        [Display(Name = "Tên Ngân hàng")]
        public string? BankName { get; set; } // Tên ngân hàng (ví dụ: "Ngân hàng TMCP Công Thương Việt Nam (VietinBank)")

        [StringLength(50)]
        [Display(Name = "Số tài khoản Ngân hàng")]
        public string? BankAccountNumber { get; set; } // Số tài khoản ngân hàng

        [StringLength(200)]
        [Display(Name = "Tên chủ tài khoản")]
        public string? BankAccountName { get; set; } // Tên chủ tài khoản (ví dụ: "NGUYEN VAN A")

        [Column(TypeName = "nvarchar(MAX)")] // Có thể chứa hướng dẫn dài
        [Display(Name = "Hướng dẫn thanh toán")]
        public string? PaymentInstructions { get; set; } // Hướng dẫn cụ thể cho người dùng khi thanh toán bằng QR

        // Thuộc tính điều hướng ngược lại từ User (nếu bạn muốn Location biết được Manager của nó)
        // Đây là Navigation Property để truy cập vào danh sách các User có ManagedLocationId trỏ đến Location này.
        // Mối quan hệ này là "một địa điểm có thể được quản lý bởi nhiều user (nếu ManagedLocationId không unique)",
        // nhưng trong thực tế, bạn sẽ chỉ gán một User là IsLocationManager cho một địa điểm.
        public ICollection<User>? LocationManagers { get; set; }


        // --- Thuộc tính điều hướng (Navigation Properties) ---
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<PanoramaPoint>? PanoramaPoints { get; set; }
    }
}