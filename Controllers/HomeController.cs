using BookingToursWeb.Data;
using BookingToursWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies; // Đảm bảo đã có dòng này

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

        // Action mặc định - Trang chủ
        public async Task<IActionResult> Index()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang Index.");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu. Vui lòng thử lại sau.";
                return View("Error");
            }
        }

        // GET: Home/Booking
        // Thêm tham số optional 'locationId' để xử lý yêu cầu từ trang chi tiết địa điểm.
        public async Task<IActionResult> Booking(int? locationId)
        {
            ViewData["Title"] = "Đặt lịch Tour";

            var locationsData = await _context.Locations
                                              .Select(l => new
                                              {
                                                  l.Id,
                                                  l.Name,
                                                  l.IsActive, // Giữ IsActive trong data
                                                  l.TicketPrice,
                                                  l.ImageUrl
                                              })
                                              .ToListAsync();
            ViewBag.AllLocationsData = JsonConvert.SerializeObject(locationsData);

            var userId = HttpContext.Session.GetInt32("UserId");
            ViewBag.CurrentUserId = userId;

            ViewBag.HideBackButton = !locationId.HasValue;

            if (locationId.HasValue)
            {
                var preselectedLocation = locationsData.FirstOrDefault(l => l.Id == locationId.Value);

                if (preselectedLocation != null)
                {
                    // KHÔNG ĐẶT TempData["ErrorMessage"] Ở ĐÂY NỮA
                    // Nếu địa điểm không hoạt động, PlaceDetails đã xử lý và hiển thị thông báo.
                    // Nếu người dùng chọn lại địa điểm không hoạt động từ trang Booking,
                    // validation client-side hoặc ModelState sẽ xử lý.
                    ViewBag.PreselectedLocationId = preselectedLocation.Id;
                    ViewBag.PreselectedLocationName = preselectedLocation.Name;
                    ViewBag.PreselectedLocationTicketPrice = preselectedLocation.TicketPrice;

                    // Thông báo lỗi nếu địa điểm không hoạt động (cho trường hợp người dùng direct link hoặc thay đổi IsActive sau khi vào trang)
                    if (!preselectedLocation.IsActive)
                    {
                        TempData["ErrorMessage"] = "Địa điểm này hiện không hoạt động. Vui lòng chọn địa điểm khác.";
                    }
                }
                else
                {
                    // Nếu locationId không tồn tại
                    ViewBag.PreselectedLocationId = null;
                    TempData["ErrorMessage"] = "Địa điểm được chọn không tồn tại.";
                }
            }
            else
            {
                ViewBag.PreselectedLocationId = null;
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
            booking.CreatedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;
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
            if (booking.AppointmentDate != default(DateTime))
            {
                var existingBookingsCount = await _context.Bookings
                   .CountAsync(b => b.LocationId == booking.LocationId &&
                                    b.AppointmentDate.Date == booking.AppointmentDate.Date);

                if (existingBookingsCount >= MAX_BOOKINGS_PER_DAY_PER_LOCATION)
                {
                    ModelState.AddModelError(string.Empty, "Địa điểm này đã đủ lịch đặt cho ngày đã chọn. Vui lòng chọn ngày hoặc địa điểm khác.");
                }
            }
            else
            {
                ModelState.AddModelError("AppointmentDate", "Vui lòng chọn ngày và giờ đặt lịch.");
            }

            if (booking.AppointmentDate < DateTime.UtcNow.AddMinutes(-5))
            {
                ModelState.AddModelError("AppointmentDate", "Không thể đặt lịch vào thời gian trong quá khứ.");
            }


            if (!ModelState.IsValid)
            {
                var locationsDataForErrors = await _context.Locations
                                                           .Select(l => new
                                                           {
                                                               l.Id,
                                                               l.Name,
                                                               l.IsActive,
                                                               l.TicketPrice,
                                                               l.ImageUrl
                                                           })
                                                           .ToListAsync();
                ViewBag.AllLocationsData = JsonConvert.SerializeObject(locationsDataForErrors);
                ViewBag.CurrentUserId = userId;

                var selectedLoc = locationsDataForErrors.FirstOrDefault(l => l.Id == booking.LocationId);
                if (selectedLoc != null)
                {
                    ViewBag.PreselectedLocationId = selectedLoc.Id;
                    ViewBag.PreselectedLocationName = selectedLoc.Name;
                    ViewBag.PreselectedLocationTicketPrice = selectedLoc.TicketPrice;
                    ViewBag.HideBackButton = false; // Luôn hiển thị nút khi đã chọn địa điểm (dù có lỗi validation)
                }
                else
                {
                    ViewBag.PreselectedLocationId = null;
                    ViewBag.HideBackButton = true; // Ẩn nút nếu không có địa điểm nào được chọn
                }

                ViewData["Title"] = "Đặt lịch Tour";
                return View("Booking", booking);
            }

            try
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đặt lịch thành công! Vui lòng chờ xác nhận.";
                return RedirectToAction("BookingSuccess");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu đặt lịch vào cơ sở dữ liệu.");
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi xử lý đặt lịch của bạn. Vui lòng thử lại.");

                var locationsDataForErrors = await _context.Locations
                                                           .Select(l => new
                                                           {
                                                               l.Id,
                                                               l.Name,
                                                               l.IsActive,
                                                               l.TicketPrice,
                                                               l.ImageUrl
                                                           })
                                                           .ToListAsync();
                ViewBag.AllLocationsData = JsonConvert.SerializeObject(locationsDataForErrors);
                ViewBag.CurrentUserId = userId;

                var selectedLoc = locationsDataForErrors.FirstOrDefault(l => l.Id == booking.LocationId);
                if (selectedLoc != null)
                {
                    ViewBag.PreselectedLocationId = selectedLoc.Id;
                    ViewBag.PreselectedLocationName = selectedLoc.Name;
                    ViewBag.PreselectedLocationTicketPrice = selectedLoc.TicketPrice;
                    ViewBag.HideBackButton = false; // Luôn hiển thị nút khi đã chọn địa điểm (dù có lỗi db)
                }
                else
                {
                    ViewBag.PreselectedLocationId = null;
                    ViewBag.HideBackButton = true; // Ẩn nút nếu không có địa điểm nào được chọn
                }

                ViewData["Title"] = "Đặt lịch Tour";
                return View("Booking", booking);
            }
        }

        // API để kiểm tra số lượng đặt lịch theo ngày và địa điểm (dùng cho client-side validation)
        [HttpGet]
        public async Task<IActionResult> GetBookingsCountByDateAndLocation(DateTime date, int locationId)
        {
            try
            {
                var count = await _context.Bookings
                    .CountAsync(b => b.AppointmentDate.Date == date.Date && b.LocationId == locationId);

                return Json(new { count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy số lượng đặt lịch cho ngày {date.ToShortDateString()} và địa điểm {locationId}.");
                return StatusCode(500, "Lỗi khi kiểm tra số lượng đặt lịch.");
            }
        }

        // Trang xác nhận đặt lịch thành công
        public IActionResult BookingSuccess()
        {
            ViewData["Title"] = "Đặt lịch thành công";
            return View();
        }

        // Trang hồ sơ người dùng và lịch sử đặt lịch
        public async Task<IActionResult> Profile()
        {
            ViewData["Title"] = "Hồ sơ của tôi";

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem thông tin cá nhân và lịch đã đặt.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.";
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }

                var userBookings = await _context.Bookings
                    .Include(b => b.Location)
                    .Where(b => b.UserId == userId.Value)
                    .OrderByDescending(b => b.AppointmentDate)
                    .ToListAsync();

                var profileViewModel = new UserProfileViewModel
                {
                    User = user,
                    Bookings = userBookings
                };

                return View(profileViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải hồ sơ người dùng cho UserId: {userId}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải hồ sơ của bạn. Vui lòng thử lại sau.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> PlaceDetails(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("PlaceDetails: ID địa điểm không được cung cấp.");
                TempData["ErrorMessage"] = "Địa điểm không tồn tại hoặc không tìm thấy.";
                return RedirectToAction("Index"); // Chuyển hướng về trang chủ
            }

            try
            {
                // Bao gồm Reviews và Users để hiển thị thông tin đánh giá
                var location = await _context.Locations
                                             .Include(l => l.Reviews)
                                                .ThenInclude(r => r.User) // Bao gồm User cho mỗi đánh giá
                                             .FirstOrDefaultAsync(l => l.Id == id);

                if (location == null)
                {
                    _logger.LogWarning($"PlaceDetails: Không tìm thấy địa điểm với ID: {id}.");
                    TempData["ErrorMessage"] = "Địa điểm không tồn tại hoặc không tìm thấy.";
                    return RedirectToAction("Index"); // Chuyển hướng về trang chủ
                }

                ViewData["Title"] = location.Name;
                ViewBag.LocationIsActive = location.IsActive;

                // Lấy User ID từ Session và ép kiểu tường minh để tránh lỗi dynamic operation
                int? currentUserId = HttpContext.Session.GetInt32("UserId");
                ViewBag.CurrentUserId = currentUserId; // Gán vào ViewBag để dùng trong View

                // Kiểm tra xem người dùng hiện tại đã đánh giá địa điểm này chưa
                bool hasUserReviewed = false;
                if (currentUserId.HasValue) // Chỉ kiểm tra nếu userId có giá trị
                {
                    var existingReview = await _context.Reviews
                        .FirstOrDefaultAsync(r => r.LocationId == id && r.UserId == currentUserId.Value);
                    hasUserReviewed = (existingReview != null);
                }
                ViewBag.HasUserReviewed = hasUserReviewed;

                return View(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải chi tiết địa điểm với ID: {id}.");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin địa điểm. Vui lòng thử lại sau.";
                return View("Error");
            }
        }

        // POST: Home/AddReview (Đã đổi tên từ SubmitReview)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(Review review) // Đổi tên action và tham số
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để gửi đánh giá.";
                return RedirectToAction("Login", "Account");
            }

            // Gán UserId từ session và thời gian tạo/cập nhật
            review.UserId = userId.Value;
            review.CreatedAt = DateTime.UtcNow;
            review.UpdatedAt = DateTime.UtcNow;

            // Kiểm tra xem người dùng đã đánh giá địa điểm này chưa
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.LocationId == review.LocationId && r.UserId == userId.Value);

            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "Bạn đã gửi đánh giá cho địa điểm này rồi. Bạn có thể chỉnh sửa đánh giá hiện có.";
                return RedirectToAction("PlaceDetails", new { id = review.LocationId });
            }

            // KHÔNG còn gán review.Status vì Review model không có thuộc tính Status

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(review);
                    await _context.SaveChangesAsync();

                    // KHÔNG CẬP NHẬT AverageRating và TotalReviews của Location
                    // Vì các thuộc tính này đã bị loại bỏ khỏi model Location theo yêu cầu.

                    TempData["SuccessMessage"] = "Đánh giá của bạn đã được gửi thành công!";
                    return RedirectToAction("PlaceDetails", new { id = review.LocationId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi gửi đánh giá cho địa điểm ID: {review.LocationId}, UserId: {review.UserId}");
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi đánh giá của bạn. Vui lòng thử lại.";
                }
            }
            else
            {
                // Thu thập và hiển thị lỗi ModelState nếu có
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = "Đã có lỗi xảy ra trong dữ liệu đánh giá: " + string.Join("; ", errors);
            }

            return RedirectToAction("PlaceDetails", new { id = review.LocationId });
        }

        // --- Bổ sung Action PanoramaPointsForLocation ---
        [HttpGet]
        public async Task<IActionResult> PanoramaPointsForLocation(int locationId)
        {
            var location = await _context.Locations
                                         .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null)
            {
                TempData["ErrorMessage"] = "Địa điểm không tồn tại hoặc không tìm thấy.";
                return RedirectToAction("Index", "Home");
            }

            var panoramaPoints = await _context.PanoramaPoints
                                               .Where(p => p.LocationId == locationId)
                                               .OrderBy(p => p.Name)
                                               .ToListAsync();

            ViewData["LocationName"] = location.Name;
            ViewData["LocationId"] = locationId;

            return View(panoramaPoints);
        }

        [HttpGet]
        // THAY ĐỔI: Thêm tham số string? returnUrl = null
        public async Task<IActionResult> ViewPanorama(int id, string? returnUrl = null)
        {
            var panoramaPoint = await _context.PanoramaPoints
                                              .Include(p => p.Location) // Bao gồm Location để có tên địa điểm
                                              .FirstOrDefaultAsync(p => p.Id == id);

            if (panoramaPoint == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy điểm nhìn panorama này.";
                // Mặc định quay về trang chủ nếu không tìm thấy panorama
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = $"Xem Panorama: {panoramaPoint.Name}";
            ViewData["LocationId"] = panoramaPoint.LocationId; // Để nút quay lại trang chi tiết địa điểm
            ViewData["ReturnUrl"] = returnUrl; // LƯU returnUrl vào ViewData

            return View(panoramaPoint);
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