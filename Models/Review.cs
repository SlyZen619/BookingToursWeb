using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingToursWeb.Models
{
    [Table("Reviews")]
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Mã Người Dùng")]
        public int UserId { get; set; } // Khóa ngoại cho User

        [Required]
        [Display(Name = "Mã Địa Điểm")]
        public int LocationId { get; set; } // Khóa ngoại cho Location

        [Required(ErrorMessage = "Điểm đánh giá phải từ 1 đến 5.")]
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5.")]
        [Display(Name = "Điểm Đánh Giá")]
        public int Rating { get; set; } // Ví dụ: từ 1 đến 5 sao

        [StringLength(2000)] // Nhận xét dài hơn
        [Display(Name = "Nhận Xét")]
        public string? Comment { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Thuộc tính điều hướng ---
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("LocationId")]
        public Location? Location { get; set; }
    }
}