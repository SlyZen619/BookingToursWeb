using System.ComponentModel.DataAnnotations;

namespace BookingToursWeb.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ.")]
        [Display(Name = "Email")]
        public required string Email { get; set; }
    }
}