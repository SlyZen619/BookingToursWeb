using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// Bỏ using BookingToursWeb.Models; nếu nó không còn dùng cho Category
// Nếu bạn muốn giữ lại các models khác trong BookingToursWeb.Models thì giữ nguyên using

namespace BookingToursWeb.Models
{
    [Table("Posts")]
    public class Post
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Mã Tác Giả")]
        public int AuthorId { get; set; } // Khóa ngoại cho User (Admin)

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [StringLength(500)]
        [Display(Name = "Tiêu Đề")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Nội dung không được để trống.")]
        [Column(TypeName = "nvarchar(MAX)")] // Đảm bảo Content có thể lưu trữ nội dung dài
        public required string Content { get; set; }

        // === THAY ĐỔI Ở ĐÂY: Loại bỏ string Category và thêm CategoryId + Navigation Property ===
        [Required(ErrorMessage = "Vui lòng chọn danh mục.")] // Đặt required nếu muốn bắt buộc chọn danh mục
        [Display(Name = "Danh Mục")]
        public int CategoryId { get; set; } // Khóa ngoại tới bảng Categories
        [ForeignKey("CategoryId")] // Chỉ định CategoryId là khóa ngoại
        public Category? Category { get; set; } // Thuộc tính điều hướng để truy cập đối tượng Category liên quan
        // === HẾT THAY ĐỔI ===

        [StringLength(500)]
        [Display(Name = "URL Hình ảnh")]
        public string? ImageUrl { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Ngày Xuất Bản")]
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Thuộc tính điều hướng ---
        [ForeignKey("AuthorId")]
        public User? Author { get; set; } // Tham chiếu đến tác giả (User)
    }
}