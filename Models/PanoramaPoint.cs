using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingToursWeb.Models
{
    [Table("PanoramaPoints")] // Tên bảng mới trong database
    public class PanoramaPoint
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Địa điểm liên kết không được để trống.")]
        [Display(Name = "ID Địa điểm")]
        public int LocationId { get; set; } // Khóa ngoại tới bảng Locations

        [Required(ErrorMessage = "Tên điểm nhìn không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Tên Điểm Nhìn")]
        public string Name { get; set; } = string.Empty; // Ví dụ: "Phòng khách", "Sân sau"

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Đường dẫn ảnh panorama không được để trống.")]
        [StringLength(500, ErrorMessage = "Đường dẫn ảnh không được vượt quá 500 ký tự.")]
        [Display(Name = "Đường dẫn ảnh Panorama")]
        public string ImageUrl { get; set; } = string.Empty; // Đường dẫn đến thư mục chứa các tile ảnh (vd: /images/panoramas/locationX/roomY/)

        // Navigation Property: Một PanoramaPoint thuộc về một Location
        [ForeignKey("LocationId")]
        public Location? Location { get; set; }
    }
}