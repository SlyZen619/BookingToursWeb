using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Cần thiết cho ICollection

namespace BookingToursWeb.Models
{
    [Table("Categories")] // Đặt tên bảng trong database là Categories
    public class Category
    {
        [Key] // Đặt Id làm khóa chính
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Tự động tăng
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
        [Display(Name = "Tên Danh Mục")]
        public required string Name { get; set; }

        // Thuộc tính điều hướng: Một Category có nhiều Posts
        public ICollection<Post>? Posts { get; set; }
    }
}