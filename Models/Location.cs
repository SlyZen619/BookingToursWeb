// Models/Location.cs
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
        public string Name { get; set; } = string.Empty; // Thêm = string.Empty;

        [Required(ErrorMessage = "Mô tả không được để trống.")] // Bạn vừa thêm Required
        [StringLength(1000, ErrorMessage = "Mô tả ngắn gọn không được vượt quá 1000 ký tự.")]
        [Display(Name = "Mô tả ngắn gọn")]
        public string Description { get; set; } = string.Empty; // THÊM = string.Empty; để bỏ cảnh báo

        [Required(ErrorMessage = "Thông tin chi tiết không được để trống.")]
        [Display(Name = "Thông tin chi tiết")]
        [Column(TypeName = "nvarchar(MAX)")]
        public string Information { get; set; } = string.Empty; // Thêm = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty; // Thêm = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, 9999999999999996.99, ErrorMessage = "Giá vé phải là số không âm.")]
        [Display(Name = "Giá vé")]
        public decimal? TicketPrice { get; set; } // Giữ decimal? vì bạn đã tick "Allow Nulls" trong DB

        [Required(ErrorMessage = "Giờ mở cửa không được để trống.")]
        [StringLength(100, ErrorMessage = "Giờ mở cửa không được vượt quá 100 ký tự.")]
        [Display(Name = "Giờ mở cửa")]
        public string OpeningHours { get; set; } = string.Empty; // Thêm = string.Empty;

        [Required(ErrorMessage = "URL Hình ảnh không được để trống.")]
        [StringLength(500, ErrorMessage = "URL Hình ảnh không được vượt quá 500 ký tự.")]
        [Display(Name = "URL Hình ảnh")]
        public string ImageUrl { get; set; } = string.Empty; // Thêm = string.Empty;

        [Required(ErrorMessage = "Thông tin liên hệ không được để trống.")]
        [StringLength(200, ErrorMessage = "Thông tin liên hệ không được vượt quá 200 ký tự.")]
        [Display(Name = "Thông tin liên hệ")]
        public string ContactInfo { get; set; } = string.Empty; // Thêm = string.Empty;

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; } = true;

        // --- Thuộc tính điều hướng (Navigation Properties) ---
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>(); // Khởi tạo tại khai báo
        public ICollection<Review> Reviews { get; set; } = new List<Review>(); // Khởi tạo tại khai báo
        public ICollection<PanoramaView> PanoramaViews { get; set; } = new List<PanoramaView>(); // THÊM DÒNG NÀY
        // Có thể bỏ constructor rỗng nếu đã khởi tạo tất cả các thuộc tính trên khai báo
        // hoặc giữ nếu bạn có logic khởi tạo khác phức tạp hơn
        // public Location()
        // {
        //     Bookings = new List<Booking>();
        //     Reviews = new List<Review>();
        // }
    }
}