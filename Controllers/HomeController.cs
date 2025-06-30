using BookingToursWeb.Data;
using BookingToursWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Cần thiết để sử dụng Session
using Newtonsoft.Json; // <-- Đảm bảo dòng này có mặt và gói NuGet đã cài đặt

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
            var allPlaces = await _context.Locations
                                         .OrderByDescending(l => l.Id)
                                         .ToListAsync();

            var viewModel = new HomeViewModel
            {
                FamousPlaces = allPlaces
            };

            ViewData["Title"] = "Trang chủ";
            return View(viewModel);
        }

        // GET: Home/Booking
        public async Task<IActionResult> Booking()
        {
            ViewData["Title"] = "Đặt lịch Tour";

            // Lấy TẤT CẢ các địa điểm với các thuộc tính cụ thể, bao gồm ImageUrl.
            // Newtonsoft.Json sẽ serialize các tên này giữ nguyên PascalCase (ví dụ: "Id", "Name").
            var locationsData = await _context.Locations
                                              .Select(l => new
                                              {
                                                  l.Id,
                                                  l.Name,
                                                  l.IsActive,
                                                  l.TicketPrice,
                                                  l.ImageUrl // <--- ĐÃ THÊM: Đảm bảo ImageUrl được select
                                              })
                                              .ToListAsync();

            // Chuyển danh sách đối tượng ẩn danh thành chuỗi JSON và truyền qua ViewBag
            ViewBag.AllLocationsData = JsonConvert.SerializeObject(locationsData);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                ViewBag.CurrentUserId = userId.Value;
            }
            else
            {
                ViewBag.CurrentUserId = null; // Đảm bảo không lỗi nếu người dùng chưa đăng nhập
            }

            return View(new Booking());
        }

        // POST: Home/CreateBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(Booking booking)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để đặt lịch.";
                return RedirectToAction("Login", "Account");
            }

            booking.UserId = userId.Value;
            booking.CreatedAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;
            booking.Status = "Pending";

            var location = await _context.Locations.FindAsync(booking.LocationId);

            if (location == null)
            {
                ModelState.AddModelError("LocationId", "Địa điểm không tồn tại.");
            }
            else if (!location.IsActive)
            {
                ModelState.AddModelError("LocationId", "Địa điểm này hiện không hoạt động. Vui lòng chọn địa điểm khác.");
            }
            else if (!location.TicketPrice.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Thông tin giá vé cho địa điểm đã chọn không hợp lệ.");
            }
            else
            {
                booking.TotalAmount = booking.NumberOfVisitors * location.TicketPrice.Value;
            }

            const int MAX_BOOKINGS_PER_DAY_PER_LOCATION = 3;
            var existingBookingsCount = await _context.Bookings
                .CountAsync(b => b.LocationId == booking.LocationId &&
                                 b.AppointmentDate.Date == booking.AppointmentDate.Date);

            if (existingBookingsCount >= MAX_BOOKINGS_PER_DAY_PER_LOCATION)
            {
                ModelState.AddModelError(string.Empty, "Địa điểm này đã đủ lịch đặt cho ngày đã chọn. Vui lòng chọn ngày hoặc địa điểm khác.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đặt lịch thành công! Vui lòng chờ xác nhận.";
                return RedirectToAction("BookingSuccess");
            }

            // Nếu ModelState không hợp lệ, tải lại dữ liệu địa điểm cho View
            // Đảm bảo rằng ImageUrl cũng được bao gồm khi tải lại dữ liệu để hiển thị lại các card
            var locationsDataForErrors = await _context.Locations
                                                      .Select(l => new
                                                      {
                                                          l.Id,
                                                          l.Name,
                                                          l.IsActive,
                                                          l.TicketPrice,
                                                          l.ImageUrl // <--- ĐÃ THÊM: Đảm bảo ImageUrl được select khi có lỗi
                                                      })
                                                      .ToListAsync();
            ViewBag.AllLocationsData = JsonConvert.SerializeObject(locationsDataForErrors);
            ViewBag.CurrentUserId = userId.Value;
            ViewData["Title"] = "Đặt lịch Tour";

            return View("Booking", booking);
        }

        // API để kiểm tra số lượng đặt lịch theo ngày và địa điểm
        [HttpGet]
        public async Task<IActionResult> GetBookingsCountByDateAndLocation(DateTime date, int locationId)
        {
            var count = await _context.Bookings
                .CountAsync(b => b.AppointmentDate.Date == date.Date && b.LocationId == locationId);

            return Json(new { count = count });
        }


        // Trang xác nhận đặt lịch thành công
        public IActionResult BookingSuccess()
        {
            ViewData["Title"] = "Đặt lịch thành công";
            return View();
        }

        public async Task<IActionResult> Profile()
        {
            ViewData["Title"] = "Profile của tôi"; // Tiêu đề trang là Profile của tôi

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem thông tin cá nhân và lịch đã đặt.";
                return RedirectToAction("Login", "Account"); // Chuyển hướng đến trang đăng nhập nếu chưa đăng nhập
            }

            // Lấy thông tin người dùng (có thể cần sau này)
            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Index"); // Hoặc trang lỗi phù hợp
            }

            // Lấy tất cả lịch đặt của người dùng hiện tại, bao gồm thông tin Location
            var userBookings = await _context.Bookings
                .Include(b => b.Location) // Nạp thông tin địa điểm liên quan
                .Where(b => b.UserId == userId.Value)
                .OrderByDescending(b => b.AppointmentDate) // Sắp xếp theo ngày đặt giảm dần
                .ToListAsync();

            // Tạo một ViewModel để chứa cả thông tin User và danh sách Bookings
            var profileViewModel = new UserProfileViewModel
            {
                User = user,
                Bookings = userBookings
            };

            return View(profileViewModel); // Truyền ViewModel sang View
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