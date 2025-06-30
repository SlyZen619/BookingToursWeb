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
        [Display(Name = "Tên Địa điểm")]
        public string Name { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả ngắn gọn không được vượt quá 1000 ký tự.")]
        [Display(Name = "Mô tả ngắn gọn")]
        public string? Description { get; set; }

        // Thêm [Required] nếu bạn muốn Information là bắt buộc
        [Required(ErrorMessage = "Thông tin chi tiết không được để trống.")]
        [Display(Name = "Thông tin chi tiết")]
        [Column(TypeName = "nvarchar(MAX)")]
        public string? Information { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Gía vé không được để trống.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, 9999999999999999.99, ErrorMessage = "Giá vé phải là số không âm.")]
        [Display(Name = "Giá vé")]
        public decimal? TicketPrice { get; set; } // Nullable, nên không Required

        // Thêm [Required] nếu bạn muốn Giờ mở cửa là bắt buộc
        [Required(ErrorMessage = "Giờ mở cửa không được để trống.")]
        [StringLength(100, ErrorMessage = "Giờ mở cửa không được vượt quá 100 ký tự.")]
        [Display(Name = "Giờ mở cửa")]
        public string? OpeningHours { get; set; }

        // Thêm [Required] nếu bạn muốn URL Hình ảnh là bắt buộc
        [Required(ErrorMessage = "URL Hình ảnh không được để trống.")]
        [StringLength(500, ErrorMessage = "URL Hình ảnh không được vượt quá 500 ký tự.")]
        [Display(Name = "URL Hình ảnh")]
        public string? ImageUrl { get; set; }

        // Thêm [Required] nếu bạn muốn Thông tin liên hệ là bắt buộc
        [Required(ErrorMessage = "Thông tin liên hệ không được để trống.")]
        [StringLength(200, ErrorMessage = "Thông tin liên hệ không được vượt quá 200 ký tự.")]
        [Display(Name = "Thông tin liên hệ")]
        public string? ContactInfo { get; set; }

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; } = true;

        // --- Thuộc tính điều hướng (Navigation Properties) ---
        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<Review>? Reviews { get; set; }
    }
}