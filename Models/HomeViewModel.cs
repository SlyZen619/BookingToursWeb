// Models/HomeViewModel.cs
using System.Collections.Generic;

namespace BookingToursWeb.Models
{
    public class HomeViewModel
    {
        public List<Location> FamousPlaces { get; set; } = new List<Location>();
        // Có thể thêm các thuộc tính khác ở đây nếu trang chủ cần hiển thị nhiều loại dữ liệu
        // public List<Post> FeaturedPosts { get; set; }
        // public List<Review> LatestReviews { get; set; }
    }
}