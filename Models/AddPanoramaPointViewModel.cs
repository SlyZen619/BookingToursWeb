using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Cần thiết để sử dụng IFormFile cho việc upload file
using BookingToursWeb.Models; // Để tham chiếu đến model Location

namespace BookingToursWeb.Models
{
    public class AddPanoramaPointViewModel
    {
        public int LocationId { get; set; } // ID của địa điểm mà panorama này thuộc về

        [Display(Name = "Tên Điểm Nhìn Panorama")]
        [Required(ErrorMessage = "Tên điểm nhìn không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }

        [Display(Name = "Chọn Ảnh Panorama (Equirectangular)")]
        [Required(ErrorMessage = "Vui lòng chọn một ảnh panorama để tải lên.")]
        [DataType(DataType.Upload)] // Gợi ý rằng đây là input upload file
        public IFormFile? UploadedImageFile { get; set; } // Sử dụng IFormFile để nhận file từ form

        // Thuộc tính này để hiển thị tên địa điểm trên form cho Admin dễ theo dõi
        public Location? Location { get; set; }
    }
}