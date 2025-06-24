using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string Title { get; set; }

        [Required(ErrorMessage = "Nội dung không được để trống.")]
        public string Content { get; set; } // Content có thể rất dài, không giới hạn độ dài nếu dùng string

        [StringLength(100)]
        [Display(Name = "Danh Mục")]
        public string? Category { get; set; }

        [StringLength(500)]
        [Display(Name = "URL Hình ảnh")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Đã Xuất Bản")]
        public bool IsPublished { get; set; } = false;

        [Display(Name = "Ngày Xuất Bản")]
        public DateTime? PublishedAt { get; set; } // Nullable nếu chưa xuất bản

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Thuộc tính điều hướng ---
        [ForeignKey("AuthorId")]
        public User? Author { get; set; } // Tham chiếu đến tác giả (User)
    }
}