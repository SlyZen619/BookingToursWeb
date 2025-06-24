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
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(1000)] // Độ dài lớn hơn cho mô tả
        public string? Description { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [StringLength(500)]
        public string Address { get; set; }

        [Column(TypeName = "decimal(18, 2)")] // Kiểu tiền tệ trong SQL
        [Display(Name = "Giá vé")]
        public decimal? TicketPrice { get; set; } // Nullable nếu có thể miễn phí

        [StringLength(100)]
        [Display(Name = "Giờ mở cửa")]
        public string? OpeningHours { get; set; }

        [StringLength(500)] // URL hình ảnh
        [Display(Name = "URL Hình ảnh")]
        public string? ImageUrl { get; set; }

        [StringLength(200)]
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