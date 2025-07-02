// Models/PanoramaView.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingToursWeb.Models
{
    [Table("PanoramaViews")] // Đặt tên bảng trong database là PanoramaViews
    public class PanoramaView
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Id tự động tăng
        public int Id { get; set; }

        [Required(ErrorMessage = "Địa điểm liên kết không được để trống.")]
        [Display(Name = "ID Địa điểm")]
        public int LocationId { get; set; } // Khóa ngoại tới bảng Locations

        [Required(ErrorMessage = "Tên chế độ xem panorama không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên không được vượt quá 200 ký tự.")]
        [Display(Name = "Tên Chế độ xem")]
        public string Name { get; set; } = string.Empty; // Ví dụ: "Phòng khách", "Sân thượng"

        [Required(ErrorMessage = "URL ảnh panorama không được để trống.")]
        [StringLength(500, ErrorMessage = "URL ảnh panorama không được vượt quá 500 ký tự.")]
        [Display(Name = "URL Ảnh Panorama")]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")] // SỬA LỖI TẠI ĐÂY
        [Display(Name = "Mô tả")]
        [Column(TypeName = "nvarchar(MAX)")] // Đảm bảo đủ không gian cho mô tả dài
        public string? Description { get; set; } // Cho phép null

        // Navigation Property: Một PanoramaView thuộc về một Location
        // [ForeignKey("LocationId")] // Không cần thiết nếu tên thuộc tính là "TênNavigationPropertyId" theo quy ước
        public Location? Location { get; set; } // Mark as nullable for clarity, though it will be loaded
    }
}