// Models/Location.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Display(Name = "Tên Địa điểm")] // Thêm Display Name
        public string Name { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
        [Display(Name = "Mô tả")] // Thêm Display Name
        public string? Description { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        [Display(Name = "Địa chỉ")] // Thêm Display Name
        public string Address { get; set; }

        [Column(TypeName = "decimal(18, 2)")] // Kiểu tiền tệ trong SQL
        // Không dùng [Required] vì TicketPrice là nullable
        [Range(0, 9999999999999999.99, ErrorMessage = "Giá vé phải là số không âm.")] // Điều chỉnh Range cho decimal
        [Display(Name = "Giá vé")]
        public decimal? TicketPrice { get; set; } // Nullable nếu có thể miễn phí

        [StringLength(100, ErrorMessage = "Giờ mở cửa không được vượt quá 100 ký tự.")]
        [Display(Name = "Giờ mở cửa")]
        public string? OpeningHours { get; set; }

        [StringLength(500, ErrorMessage = "URL Hình ảnh không được vượt quá 500 ký tự.")]
        [Display(Name = "URL Hình ảnh")]
        public string? ImageUrl { get; set; }

        [StringLength(200, ErrorMessage = "Thông tin liên hệ không được vượt quá 200 ký tự.")]
        [Display(Name = "Thông tin liên hệ")]
        public string? ContactInfo { get; set; }

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; } = true;

        // --- Thuộc tính điều hướng (Navigation Properties) ---
        // Một Location có thể có nhiều Bookings
        public ICollection<Booking>? Bookings { get; set; }

        // Một Location có thể có nhiều Reviews
        public ICollection<Review>? Reviews { get; set; }
    }
}