// ViewModels/LocationPanoramaViewModel.cs
using BookingToursWeb.Models;
using System.Collections.Generic;

namespace BookingToursWeb.ViewModels
{
    public class LocationPanoramaViewModel
    {
        public Location Location { get; set; } = new Location(); // Thông tin về địa điểm hiện tại
        public List<PanoramaView> PanoramaViews { get; set; } = new List<PanoramaView>(); // Danh sách các PanoramaViews đã có cho địa điểm này
        public PanoramaView NewPanoramaView { get; set; } = new PanoramaView(); // Đối tượng để binding với form thêm mới
    }
}