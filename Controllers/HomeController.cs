using System.Diagnostics;
using BookingToursWeb.Models;
using Microsoft.AspNetCore.Mvc;
using BookingToursWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BookingToursWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Truy vấn TẤT CẢ các địa điểm từ database, bỏ qua điều kiện IsActive
            var allPlaces = await _context.Locations
                                        .OrderByDescending(l => l.Id) // Vẫn sắp xếp để địa điểm mới nhất lên đầu
                                        .ToListAsync();

            // ViewModel vẫn được sử dụng để truyền danh sách địa điểm sang View,
            // mặc dù hiện tại View sẽ không hiển thị chúng.
            var viewModel = new HomeViewModel
            {
                FamousPlaces = allPlaces // Đổi từ famousPlaces thành allPlaces
            };

            ViewData["Title"] = "Trang chủ";
            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            ViewData["Title"] = "Chính sách bảo mật";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}