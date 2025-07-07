using BookingToursWeb.Data;
using BookingToursWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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

        // Phương thức hỗ trợ để tải danh mục vào ViewBag cho _Layout
        // Gọi phương thức này trong TẤT CẢ các action render ra view sử dụng _Layout.cshtml
        private async Task LoadCategoriesForLayout()
        {
            // Lấy danh mục và sắp xếp theo tên
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        }

        // Action mặc định - Trang chủ
        public async Task<IActionResult> Index()
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout

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
        public async Task<IActionResult> Booking(int? locationId)
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout

            ViewData["Title"] = "Đặt lịch Tour";

            var locationsData = await _context.Locations
                                              .Select(l => new
                                              {
                                                  l.Id,
                                                  l.Name,
                                                  l.IsActive,
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
                    ViewBag.PreselectedLocationId = preselectedLocation.Id;
                    ViewBag.PreselectedLocationName = preselectedLocation.Name;
                    ViewBag.PreselectedLocationTicketPrice = preselectedLocation.TicketPrice;

                    if (!preselectedLocation.IsActive)
                    {
                        TempData["ErrorMessage"] = "Địa điểm này hiện không hoạt động. Vui lòng chọn địa điểm khác.";
                    }
                }
                else
                {
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
            await LoadCategoriesForLayout(); // Tải danh mục cho layout (quan trọng nếu có lỗi validation và view được trả về)

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
                    ViewBag.HideBackButton = false;
                }
                else
                {
                    ViewBag.PreselectedLocationId = null;
                    ViewBag.HideBackButton = true;
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
                    ViewBag.HideBackButton = false;
                }
                else
                {
                    ViewBag.PreselectedLocationId = null;
                    ViewBag.HideBackButton = true;
                }

                ViewData["Title"] = "Đặt lịch Tour";
                return View("Booking", booking);
            }
        }

        // API để kiểm tra số lượng đặt lịch theo ngày và địa điểm (dùng cho client-side validation)
        [HttpGet]
        public async Task<IActionResult> GetBookingsCountByDateAndLocation(DateTime date, int locationId)
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout (Nếu bạn dùng Layout cho các API endpoint, thường thì không)

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
        public async Task<IActionResult> BookingSuccess() // Thêm async/await và LoadCategoriesForLayout
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout
            ViewData["Title"] = "Đặt lịch thành công";
            return View();
        }

        // Trang hồ sơ người dùng và lịch sử đặt lịch
        public async Task<IActionResult> Profile()
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout

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

        // Action hiển thị TẤT CẢ bài viết
        public async Task<IActionResult> Posts()
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout

            var allPosts = await _context.Posts
                                       .Include(p => p.Category)
                                       .Include(p => p.Author)
                                       .OrderByDescending(p => p.PublishedAt)
                                       .ToListAsync();

            ViewData["CurrentCategoryName"] = "Tất cả bài viết"; // Để hiển thị tiêu đề trên trang
            ViewData["Title"] = ViewData["CurrentCategoryName"]; // Đặt title cho trang
            return View(allPosts); // Trả về danh sách tất cả bài viết
        }

        // ACTION MỚI: Hiển thị bài viết theo danh mục
        public async Task<IActionResult> PostsByCategory(int? categoryId)
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout

            if (categoryId == null)
            {
                // Nếu không có categoryId, chuyển hướng về trang tất cả bài viết
                return RedirectToAction(nameof(Posts));
            }

            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Danh mục không tồn tại.";
                return RedirectToAction(nameof(Posts)); // Chuyển hướng về trang tất cả bài viết
            }

            var postsInCategory = await _context.Posts
                                                .Where(p => p.CategoryId == categoryId)
                                                .Include(p => p.Category)
                                                .Include(p => p.Author)
                                                .OrderByDescending(p => p.PublishedAt)
                                                .ToListAsync();

            ViewData["CurrentCategoryName"] = category.Name; // Dùng để hiển thị tên danh mục trên trang
            ViewData["Title"] = $"Bài viết trong danh mục: {category.Name}"; // Đặt title cho trang
            return View("Posts", postsInCategory); // Sử dụng lại View "Posts" để hiển thị, chỉ khác dữ liệu
        }

        // ACTION MỚI: Hiển thị chi tiết bài đăng
        public async Task<IActionResult> PostDetails(int? id)
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout

            if (id == null)
            {
                _logger.LogWarning("PostDetails: ID bài đăng không được cung cấp.");
                TempData["ErrorMessage"] = "Bài đăng không tồn tại hoặc không tìm thấy.";
                return RedirectToAction(nameof(Posts)); // Chuyển hướng về trang danh sách bài viết
            }

            try
            {
                var post = await _context.Posts
                                         .Include(p => p.Author) // Bao gồm thông tin tác giả
                                         .Include(p => p.Category) // Bao gồm thông tin danh mục
                                         .FirstOrDefaultAsync(p => p.Id == id); //

                if (post == null)
                {
                    _logger.LogWarning($"PostDetails: Không tìm thấy bài đăng với ID: {id}.");
                    TempData["ErrorMessage"] = "Bài đăng không tồn tại hoặc không tìm thấy.";
                    return RedirectToAction(nameof(Posts)); // Chuyển hướng về trang danh sách bài viết
                }

                ViewData["Title"] = post.Title; // Đặt tiêu đề cho trang dựa trên tiêu đề bài viết
                return View(post); // Truyền đối tượng bài đăng sang View
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải chi tiết bài đăng với ID: {id}.");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin bài đăng. Vui lòng thử lại sau.";
                return View("Error");
            }
        }

        public async Task<IActionResult> PlaceDetails(int? id)
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout

            if (id == null)
            {
                _logger.LogWarning("PlaceDetails: ID địa điểm không được cung cấp.");
                TempData["ErrorMessage"] = "Địa điểm không tồn tại hoặc không tìm thấy.";
                return RedirectToAction("Index");
            }

            try
            {
                var location = await _context.Locations
                                             .Include(l => l.Reviews)
                                                 .ThenInclude(r => r.User)
                                             .FirstOrDefaultAsync(l => l.Id == id);

                if (location == null)
                {
                    _logger.LogWarning($"PlaceDetails: Không tìm thấy địa điểm với ID: {id}.");
                    TempData["ErrorMessage"] = "Địa điểm không tồn tại hoặc không tìm thấy.";
                    return RedirectToAction("Index");
                }

                ViewData["Title"] = location.Name;
                ViewBag.LocationIsActive = location.IsActive;

                int? currentUserId = HttpContext.Session.GetInt32("UserId");
                ViewBag.CurrentUserId = currentUserId;

                bool hasUserReviewed = false;
                if (currentUserId.HasValue)
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

        // POST: Home/AddReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(Review review)
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout (quan trọng nếu có lỗi validation và view được trả về)

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để gửi đánh giá.";
                return RedirectToAction("Login", "Account");
            }

            review.UserId = userId.Value;
            review.CreatedAt = DateTime.UtcNow;
            review.UpdatedAt = DateTime.UtcNow;

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.LocationId == review.LocationId && r.UserId == userId.Value);

            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "Bạn đã gửi đánh giá cho địa điểm này rồi. Bạn có thể chỉnh sửa đánh giá hiện có.";
                return RedirectToAction("PlaceDetails", new { id = review.LocationId });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(review);
                    await _context.SaveChangesAsync();

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
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = "Đã có lỗi xảy ra trong dữ liệu đánh giá: " + string.Join("; ", errors);
            }

            // Nếu có lỗi, đảm bảo rằng tất cả ViewBag cần thiết để render lại PlaceDetails (nếu quay lại đó) vẫn được thiết lập.
            // Hoặc đơn giản là redirect về trang chi tiết địa điểm để người dùng thấy lỗi qua TempData
            return RedirectToAction("PlaceDetails", new { id = review.LocationId });
        }

        // --- Bổ sung Action PanoramaPointsForLocation ---
        [HttpGet]
        public async Task<IActionResult> PanoramaPointsForLocation(int locationId)
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout

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
            ViewData["Title"] = $"Các điểm Panorama của {location.Name}"; // Đặt title cho trang

            return View(panoramaPoints);
        }

        [HttpGet]
        public async Task<IActionResult> ViewPanorama(int id, string? returnUrl = null)
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout

            var panoramaPoint = await _context.PanoramaPoints
                                             .Include(p => p.Location)
                                             .FirstOrDefaultAsync(p => p.Id == id);

            if (panoramaPoint == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy điểm nhìn panorama này.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = $"Xem Panorama: {panoramaPoint.Name}";
            ViewData["LocationId"] = panoramaPoint.LocationId;
            ViewData["ReturnUrl"] = returnUrl;

            return View(panoramaPoint);
        }


        public async Task<IActionResult> Privacy() // Thêm async/await và LoadCategoriesForLayout
        {
            await LoadCategoriesForLayout(); // Tải danh mục cho layout
            ViewData["Title"] = "Chính sách bảo mật";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Error page doesn't necessarily need categories, but adding it just in case _Layout is used.
            // If you have a separate simplified error layout, you might not need this.
            // await LoadCategoriesForLayout(); // Optional for error page
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}