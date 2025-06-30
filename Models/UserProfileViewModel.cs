// Models/UserProfileViewModel.cs
using System.Collections.Generic;

namespace BookingToursWeb.Models
{
    public class UserProfileViewModel
    {
        public required User User { get; set; } // Giả định bạn đã có model User trong dự án
        public List<Booking> Bookings { get; set; }
        public UserProfileViewModel()
        {
            Bookings = new List<Booking>(); // Khởi tạo để tránh null reference
        }
    }
}