using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingToursWeb.Models
{
    [Table("Bookings")]
    public class Booking
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

        [Required(ErrorMessage = "Ngày hẹn không được để trống.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Ngày Hẹn")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Số lượng khách không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng khách phải lớn hơn 0.")]
        [Display(Name = "Số Lượng Khách")]
        public int NumberOfVisitors { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Tổng Tiền")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)] // Ví dụ: "Pending", "Confirmed", "Cancelled", "Completed"
        [Display(Name = "Trạng Thái")]
        public string Status { get; set; } = "Pending"; // Mặc định là Pending

        [StringLength(1000)]
        [Display(Name = "Ghi Chú Đặc Biệt")]
        public string? SpecialNotes { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Thuộc tính điều hướng ---
        // Mối quan hệ Many-to-One với User
        [ForeignKey("UserId")]
        public User? User { get; set; } // Tham chiếu đến đối tượng User

        // Mối quan hệ Many-to-One với Location
        [ForeignKey("LocationId")]
        public Location? Location { get; set; } // Tham chiếu đến đối tượng Location
    }
}