using BCrypt.Net; // Thêm cho BCrypt, đảm bảo bạn đã cài đặt package BCrypt.Net-Next
using BookingToursWeb.Data;
using BookingToursWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System; // Thêm để sử dụng DateTime
using System.Collections.Generic; // Thêm để sử dụng List
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting; // Thêm dòng này để sử dụng IWebHostEnvironment
using System.IO;


namespace BookingToursWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger, IWebHostEnvironment webHostEnvironment) // THÊM ILogger<AdminController> logger
        {
            _context = context;
            _logger = logger; // GÁN GIÁ TRỊ CHO _logger
            _webHostEnvironment = webHostEnvironment;
        }

        private bool IsCurrentUserAdmin()
        {
            // Kiểm tra session để xác định quyền Admin
            return HttpContext.Session.GetString("IsAdmin") == "True";
        }

        // GET: Admin/Index
        public IActionResult Index()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang Admin Dashboard.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Admin Dashboard";
            return View();
        }

        // ====================================================================
        // CÁC ACTIONS CHO QUẢN LÝ NGƯỜI DÙNG
        // ====================================================================

        // GET: Admin/ManageUsers
        public async Task<IActionResult> ManageUsers()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang quản lý người dùng.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Quản lý Tài khoản người dùng";
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: Admin/AddUser
        [HttpGet]
        public IActionResult AddUser()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thêm người dùng mới.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Thêm người dùng mới";
            return View();
        }

        // POST: Admin/AddUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(AdminAddUserViewModel model)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                    ViewData["Title"] = "Thêm người dùng mới";
                    return View(model);
                }

                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một tài khoản khác.");
                    ViewData["Title"] = "Thêm người dùng mới";
                    return View(model);
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PasswordHash = hashedPassword,
                    IsAdmin = model.IsAdmin
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm người dùng mới thành công!";
                return RedirectToAction(nameof(ManageUsers));
            }

            ViewData["Title"] = "Thêm người dùng mới";
            return View(model);
        }

        // GET: Admin/EditUser/{id}
        [HttpGet]
        public async Task<IActionResult> EditUser(int? id)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa người dùng.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = "Sửa thông tin người dùng";

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/EditUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, [Bind("Id,Username,Email,PhoneNumber,IsAdmin")] User user)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa người dùng.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = "Sửa thông tin người dùng";
            ModelState.Remove("PasswordHash"); // Loại bỏ PasswordHash khỏi validation khi chỉnh sửa

            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userToUpdate = await _context.Users.FindAsync(id);
                    if (userToUpdate == null)
                    {
                        return NotFound();
                    }

                    userToUpdate.Username = user.Username;
                    userToUpdate.Email = user.Email;
                    userToUpdate.PhoneNumber = user.PhoneNumber;
                    userToUpdate.IsAdmin = user.IsAdmin;

                    if (_context.Users.Any(u => u.Username == userToUpdate.Username && u.Id != userToUpdate.Id))
                    {
                        ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                        return View(user);
                    }

                    if (_context.Users.Any(u => u.Email == userToUpdate.Email && u.Id != userToUpdate.Id))
                    {
                        ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                        return View(user);
                    }

                    _context.Update(userToUpdate);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageUsers));
            }
            return View(user);
        }

        // POST: Admin/DeleteUser/{id}
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa người dùng.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng để xóa.";
                return NotFound();
            }

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId.HasValue && currentUserId.Value == user.Id)
            {
                TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(ManageUsers));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Người dùng đã được xóa thành công.";
            return RedirectToAction(nameof(ManageUsers));
        }

        // ====================================================================
        // CÁC ACTIONS CHO QUẢN LÝ ĐỊA ĐIỂM
        // ====================================================================

        // GET: Admin/ManageLocations (Hiển thị danh sách địa điểm)
        public async Task<IActionResult> ManageLocations()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang quản lý địa điểm.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Quản lý Địa điểm";
            var locations = await _context.Locations.ToListAsync();
            return View(locations);
        }

        // GET: Admin/AddLocation (Hiển thị form thêm địa điểm mới)
        [HttpGet]
        public IActionResult AddLocation()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thêm địa điểm mới.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Thêm Địa điểm mới";
            return View();
        }

        // POST: Admin/AddLocation (Xử lý thêm địa điểm mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLocation([Bind("Name,Description,Information,Address,TicketPrice,OpeningHours,ImageUrl,ContactInfo,IsActive")] Location location)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                if (await _context.Locations.AnyAsync(l => l.Name == location.Name))
                {
                    ModelState.AddModelError("Name", "Tên địa điểm này đã tồn tại.");
                    ViewData["Title"] = "Thêm Địa điểm mới";
                    return View(location);
                }

                _context.Add(location);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Địa điểm đã được thêm thành công!";
                return RedirectToAction(nameof(ManageLocations));
            }
            ViewData["Title"] = "Thêm Địa điểm mới";
            return View(location);
        }

        // GET: Admin/EditLocation/{id} (Hiển thị form sửa địa điểm)
        [HttpGet]
        public async Task<IActionResult> EditLocation(int? id)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa địa điểm.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = "Sửa thông tin Địa điểm";

            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                return NotFound();
            }
            return View(location);
        }

        // POST: Admin/EditLocation/{id} (Xử lý sửa địa điểm)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(int id, [Bind("Id,Name,Description,Information,Address,TicketPrice,OpeningHours,ImageUrl,ContactInfo,IsActive")] Location location)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa địa điểm.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = "Sửa thông tin Địa điểm";

            if (id != location.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (_context.Locations.Any(l => l.Name == location.Name && l.Id != location.Id))
                    {
                        ModelState.AddModelError("Name", "Tên địa điểm này đã tồn tại.");
                        return View(location);
                    }

                    _context.Update(location);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin địa điểm thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Locations.Any(e => e.Id == location.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageLocations));
            }
            return View(location);
        }

        // POST: Admin/DeleteLocation/{id} (Xử lý xóa địa điểm)
        [HttpPost, ActionName("DeleteLocation")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocationConfirmed(int id)
        {
            // ... (Phần kiểm tra quyền admin, Include Locations, Bookings, Reviews) ...

            var location = await _context.Locations
                                        .Include(l => l.Bookings)
                                        .Include(l => l.Reviews)
                                        .Include(l => l.PanoramaPoints) // Vẫn cần include PanoramaPoints
                                        .FirstOrDefaultAsync(m => m.Id == id);

            // Kiểm tra nếu location null
            if (location == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy địa điểm cần xóa.";
                return RedirectToAction(nameof(ManageLocations));
            }

            // Bước 1: Xóa các thư mục ảnh panorama con liên quan đến từng PanoramaPoint
            var panoramaPoints = location.PanoramaPoints; // Gán vào biến cục bộ

            if (panoramaPoints != null && panoramaPoints.Any()) // Kiểm tra null và rỗng trên biến cục bộ
            {
                foreach (var panoramaPoint in panoramaPoints) // Trình biên dịch giờ biết 'panoramaPoints' không null
                {
                    var relativeImageUrl = panoramaPoint.ImageUrl; // panoramaPoint.ImageUrl có thể là string?

                    if (!string.IsNullOrEmpty(relativeImageUrl))
                    {
                        // fullPhysicalPathFromDb sẽ không null vì relativeImageUrl không null
                        string fullPhysicalPathFromDb = Path.Combine(_webHostEnvironment.WebRootPath, relativeImageUrl.TrimStart('/'));

                        // panoramaPointFolderPath có thể là null nếu fullPhysicalPathFromDb là gốc hoặc không hợp lệ
                        string? panoramaPointFolderPath = Path.GetDirectoryName(fullPhysicalPathFromDb); // Đã sửa cảnh báo Conversion of null to non-nullable

                        if (!string.IsNullOrEmpty(panoramaPointFolderPath) && Directory.Exists(panoramaPointFolderPath))
                        {
                            Directory.Delete(panoramaPointFolderPath, true); // Xóa toàn bộ thư mục và nội dung bên trong
                            Console.WriteLine($"Đã xóa thư mục panorama: {panoramaPointFolderPath}");
                        }
                    }
                }
            }

            // Bước 2: Xóa thư mục gốc của địa điểm nếu nó tồn tại và trống
            // Thư mục này là E:\BookingToursWeb\wwwroot\images\panoramas\{LocationId}
            string locationRootFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "panoramas", location.Id.ToString());
            if (Directory.Exists(locationRootFolder))
            {
                // Kiểm tra xem nó có rỗng không trước khi xóa, để tránh xóa nhầm dữ liệu không liên quan
                if (!Directory.EnumerateFileSystemEntries(locationRootFolder).Any())
                {
                    Directory.Delete(locationRootFolder, false); // false vì đã kiểm tra rỗng
                    Console.WriteLine($"Đã xóa thư mục địa điểm gốc rỗng: {locationRootFolder}");
                }
                else
                {
                    Console.WriteLine($"Thư mục địa điểm gốc '{locationRootFolder}' không rỗng, không xóa tự động.");
                }
            }

            // Bước 3: Xóa Location và các Bookings, Reviews, PanoramaPoints khỏi database
            if (location.Bookings != null && location.Bookings.Any())
            {
                _context.Bookings.RemoveRange(location.Bookings);
            }

            if (location.Reviews != null && location.Reviews.Any())
            {
                _context.Reviews.RemoveRange(location.Reviews);
            }

            // Dòng này cũng được hưởng lợi từ việc kiểm tra null rõ ràng
            if (location.PanoramaPoints != null && location.PanoramaPoints.Any())
            {
                _context.PanoramaPoints.RemoveRange(location.PanoramaPoints);
            }

            _context.Locations.Remove(location);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Địa điểm và tất cả dữ liệu liên quan đã được xóa thành công.";
            return RedirectToAction(nameof(ManageLocations));
        }


        // ====================================================================
        // CÁC ACTIONS CHO QUẢN LÝ LỊCH HẸN
        // ====================================================================

        // GET: Admin/ManageAppointments
        public async Task<IActionResult> ManageAppointments()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Quản lý Lịch hẹn";

            var appointments = await _context.Bookings
                                             .Include(b => b.User)
                                             .Include(b => b.Location)
                                             // Sắp xếp theo LocationId trước trong database (luôn non-null)
                                             // Nếu muốn, có thể thêm một OrderBy nữa cho trường hợp Location.Name null
                                             .OrderBy(b => b.LocationId) // Sắp xếp theo LocationId
                                             .ThenBy(b => b.AppointmentDate) // Sau đó sắp xếp theo ngày hẹn
                                             .ToListAsync(); // Thực thi truy vấn và kéo dữ liệu về bộ nhớ

            // Sau khi dữ liệu đã ở trong bộ nhớ, bạn có thể sắp xếp lại theo tên địa điểm
            // Điều này sẽ không gây lỗi "null propagating operator"
            appointments = appointments
                .OrderBy(b => b.Location?.Name ?? "") // Sắp xếp theo tên địa điểm (an toàn với null)
                .ThenBy(b => b.AppointmentDate) // Sắp xếp theo ngày hẹn
                .ToList(); // Chuyển lại về List để đảm bảo kiểu dữ liệu

            return View(appointments);
        }

        // GET: Admin/GetAppointmentsForCalendar (API Endpoint cho FullCalendar)
        [HttpGet]
        public async Task<IActionResult> GetAppointmentsForCalendar()
        {
            if (!IsCurrentUserAdmin())
            {
                return Unauthorized(); // Trả về 401 Unauthorized nếu không phải admin
            }

            var bookings = await _context.Bookings
                .Include(b => b.User)       // Tải thông tin người dùng liên quan
                .Include(b => b.Location) // Tải thông tin địa điểm liên quan
                .ToListAsync();

            var appointments = bookings.Select(b => new
            {
                id = b.Id,
                // Sử dụng toán tử ?? để xử lý null an toàn cho Username và LocationName
                title = $"{b.User?.Username ?? "Người dùng ẩn"} - {b.Location?.Name ?? "Địa điểm ẩn"}",
                // ĐÃ SỬA: Chuyển đổi AppointmentDate sang múi giờ địa phương (GMT+7)
                start = b.AppointmentDate.ToUniversalTime().ToString("o"), 

                // Thêm các thuộc tính mở rộng cho tooltip hoặc modal chi tiết (extendedProps)
                userName = b.User?.Username ?? "Người dùng ẩn",
                locationName = b.Location?.Name ?? "Địa điểm ẩn",
                userId = b.UserId,
                locationId = b.LocationId,
                numberOfVisitors = b.NumberOfVisitors,
                totalAmount = b.TotalAmount.ToString("N0") + "đ", // Định dạng tiền tệ cho tooltip
                status = b.Status,
                specialNotes = b.SpecialNotes, // SpecialNotes là string? nên có thể null, không cần ?? string rỗng nếu muốn hiển thị null
                // ĐÃ SỬA: Chuyển đổi CreatedAt và UpdatedAt sang múi giờ địa phương cho tooltip
                createdAt = b.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                updatedAt = b.UpdatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            })
            .ToList(); // Thêm ToList() để thực thi truy vấn trước khi trả về Json

            return Json(appointments);
        }

        // GET: Admin/AddAppointment (Form thêm lịch hẹn mới)
        [HttpGet]
        public async Task<IActionResult> AddAppointment(string? date)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thêm lịch hẹn.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Thêm Lịch hẹn mới";

            // Truyền danh sách User và Location để tạo dropdown list
            ViewBag.Users = await _context.Users.ToListAsync();
            ViewBag.Locations = await _context.Locations.ToListAsync();

            var model = new Booking();
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime parsedDate))
            {
                // ĐẢM BẢO THỜI GIAN ĐƯỢC CHUYỂN ĐỔI CHÍNH XÁC KHI ĐƯỢC TRUYỀN TỪ FULLCALENDAR
                // FullCalendar truyền startStr là ISO 8601, C# Parse sẽ tự động xem nó là Local hoặc UTC tùy chuỗi
                // Nếu bạn muốn chắc chắn nó là múi giờ địa phương, bạn có thể thiết lập:
                model.AppointmentDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Local);
            }
            else
            {
                model.AppointmentDate = DateTime.Now; // Mặc định là thời gian hiện tại của máy chủ
            }
            model.Status = "Pending";

            return View(model);
        }

        // POST: Admin/AddAppointment (Xử lý thêm lịch hẹn mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAppointment([Bind("UserId,LocationId,AppointmentDate,NumberOfVisitors,TotalAmount,Status,SpecialNotes")] Booking booking)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                // === Bắt đầu logic giới hạn lịch hẹn ===
                // Lấy ngày của lịch hẹn (chỉ phần ngày, bỏ qua giờ, phút, giây)
                var appointmentDateOnly = booking.AppointmentDate.Date;

                // Đếm số lượng lịch hẹn đã có cho địa điểm này vào ngày này
                var existingAppointmentsCount = await _context.Bookings
                    .Where(b => b.LocationId == booking.LocationId && b.AppointmentDate.Date == appointmentDateOnly)
                    .CountAsync();

                // Giới hạn 3 lượt mỗi ngày cho mỗi địa điểm
                const int MaxAppointmentsPerDayPerLocation = 3;

                if (existingAppointmentsCount >= MaxAppointmentsPerDayPerLocation)
                {
                    // Nếu đã đạt giới hạn, thêm lỗi vào ModelState
                    ModelState.AddModelError(string.Empty, $"Địa điểm này đã đạt giới hạn {MaxAppointmentsPerDayPerLocation} lượt đặt trong ngày {appointmentDateOnly.ToShortDateString()}. Vui lòng chọn ngày hoặc địa điểm khác.");
                    // Cần tải lại ViewBag.Users và ViewBag.Locations để dropdown không bị rỗng khi quay lại View
                    ViewBag.Users = await _context.Users.ToListAsync();
                    ViewBag.Locations = await _context.Locations.ToListAsync();
                    ViewData["Title"] = "Thêm Lịch hẹn mới";
                    return View(booking); // Trả về View với lỗi
                }
                // === Kết thúc logic giới hạn lịch hẹn ===

                // Khi lưu vào DB, nên lưu dưới dạng UTC để nhất quán (sau đó chuyển đổi khi hiển thị)
                booking.CreatedAt = DateTime.UtcNow;
                booking.UpdatedAt = DateTime.UtcNow;

                var location = await _context.Locations.FindAsync(booking.LocationId);
                if (location != null && location.TicketPrice.HasValue)
                {
                    booking.TotalAmount = location.TicketPrice.Value * booking.NumberOfVisitors;
                }
                else
                {
                    booking.TotalAmount = 0;
                }

                _context.Add(booking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm lịch hẹn mới thành công!";
                return RedirectToAction(nameof(ManageAppointments));
            }

            // Nếu ModelState không hợp lệ ban đầu, cũng cần tải lại ViewBag
            ViewBag.Users = await _context.Users.ToListAsync();
            ViewBag.Locations = await _context.Locations.ToListAsync();
            ViewData["Title"] = "Thêm Lịch hẹn mới";
            return View(booking);
        }

        // GET: Admin/EditAppointment
        [HttpGet]
        public async Task<IActionResult> EditAppointment(int? id)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền sửa lịch hẹn.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            ViewBag.Users = await _context.Users.ToListAsync();
            ViewBag.Locations = await _context.Locations.ToListAsync();
            ViewData["Title"] = "Sửa thông tin Lịch hẹn";
            return View(booking);
        }

        // POST: Admin/EditAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAppointment(int id, [Bind("Id,UserId,LocationId,AppointmentDate,NumberOfVisitors,TotalAmount,Status,SpecialNotes,CreatedAt")] Booking booking)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return Unauthorized();
            }

            if (id != booking.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBooking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    if (existingBooking == null)
                    {
                        return NotFound();
                    }

                    // === Bắt đầu logic giới hạn lịch hẹn cho Edit ===
                    // Lấy ngày của lịch hẹn mới (chỉ phần ngày)
                    var newAppointmentDateOnly = booking.AppointmentDate.Date;

                    // Đếm số lượng lịch hẹn đã có cho địa điểm và ngày mới
                    // Loại trừ lịch hẹn hiện tại đang được chỉnh sửa khỏi phép đếm
                    var existingAppointmentsCount = await _context.Bookings
                        .Where(b => b.LocationId == booking.LocationId &&
                                     b.AppointmentDate.Date == newAppointmentDateOnly &&
                                     b.Id != booking.Id) // LOẠI TRỪ LỊCH HẸN HIỆN TẠI
                        .CountAsync();

                    const int MaxAppointmentsPerDayPerLocation = 3;

                    if (existingAppointmentsCount >= MaxAppointmentsPerDayPerLocation)
                    {
                        // Nếu đã đạt giới hạn, thêm lỗi vào ModelState
                        ModelState.AddModelError(string.Empty, $"Địa điểm này đã đạt giới hạn {MaxAppointmentsPerDayPerLocation} lượt đặt trong ngày {newAppointmentDateOnly.ToShortDateString()} (ngoại trừ lịch hẹn này). Vui lòng chọn ngày hoặc địa điểm khác.");
                        // Cần tải lại ViewBag.Users và ViewBag.Locations để dropdown không bị rỗng khi quay lại View
                        ViewBag.Users = await _context.Users.ToListAsync();
                        ViewBag.Locations = await _context.Locations.ToListAsync();
                        ViewData["Title"] = "Sửa thông tin Lịch hẹn";
                        return View(booking); // Trả về View với lỗi
                    }
                    // === Kết thúc logic giới hạn lịch hẹn cho Edit ===

                    // Gán CreatedAt từ existingBooking để đảm bảo nó không bị mất
                    booking.CreatedAt = existingBooking.CreatedAt;
                    // Cập nhật UpdatedAt về thời gian UTC hiện tại
                    booking.UpdatedAt = DateTime.UtcNow;

                    var location = await _context.Locations.FindAsync(booking.LocationId);
                    if (location != null && location.TicketPrice.HasValue)
                    {
                        booking.TotalAmount = location.TicketPrice.Value * booking.NumberOfVisitors;
                    }
                    else
                    {
                        booking.TotalAmount = 0;
                    }

                    _context.Update(booking);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Lịch hẹn đã được cập nhật thành công!";
                    return RedirectToAction(nameof(ManageAppointments));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Nếu ModelState không hợp lệ, tải lại ViewBag và trả về View
            ViewBag.Users = await _context.Users.ToListAsync();
            ViewBag.Locations = await _context.Locations.ToListAsync();
            ViewData["Title"] = "Sửa thông tin Lịch hẹn";
            return View(booking);
        }

        // POST: Admin/DeleteAppointment
        [HttpPost, ActionName("DeleteAppointment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAppointmentConfirmed(int id)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa lịch hẹn.";
                return RedirectToAction("Index", "Home");
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa lịch hẹn thành công!";
            return RedirectToAction(nameof(ManageAppointments));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }

        // ====================================================================
        // API Endpoint cho AJAX để lấy thông tin Location (cho tính toán TotalAmount)
        // ====================================================================
        [HttpGet("api/locations/{id}")] // Định nghĩa route cho API
        public async Task<IActionResult> GetLocationById(int id)
        {
            var location = await _context.Locations
                .Select(l => new { l.Id, l.Name, l.TicketPrice }) // Chỉ lấy các thông tin cần thiết
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
            {
                return NotFound(); // Trả về 404 nếu không tìm thấy
            }

            return Json(location); // Trả về dữ liệu dưới dạng JSON
        }


        // Các action quản lý Review và Post
        // Bạn đã có các placeholder cho chúng, tôi sẽ giữ nguyên
        // GET: Admin/ManageReviews
        public IActionResult ManageReviews()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Quản lý Đánh giá";
            return View();
        }

        // GET: Admin/ManagePosts
        public IActionResult ManagePosts()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Quản lý Bài đăng";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ManagePanoramas()
        {
            var locations = await _context.Locations.OrderBy(l => l.Name).ToListAsync();
            ViewData["Title"] = "Quản lý Panorama theo Địa điểm";
            return View(locations);
        }

        [HttpGet]
        public async Task<IActionResult> ListPanoramaPoints(int locationId)
        {
            var location = await _context.Locations
                                         .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null)
            {
                TempData["ErrorMessage"] = "Địa điểm không tồn tại.";
                return RedirectToAction(nameof(ManagePanoramas)); // Quay lại trang quản lý tổng quan
            }

            ViewData["Title"] = $"Danh sách Panorama cho: {location.Name}";
            ViewData["CurrentLocationId"] = locationId;
            ViewData["LocationName"] = location.Name;

            // Lấy các PanoramaPoint thuộc về địa điểm này
            var panoramaPoints = await _context.PanoramaPoints
                                               .Where(p => p.LocationId == locationId)
                                               .OrderBy(p => p.Name)
                                               .ToListAsync();

            return View(panoramaPoints);
        }

        [HttpGet]
        public async Task<IActionResult> AddPanoramaPoint(int locationId)
        {
            var location = await _context.Locations.FindAsync(locationId);
            if (location == null)
            {
                TempData["ErrorMessage"] = "Địa điểm không tồn tại.";
                return RedirectToAction(nameof(ManagePanoramas));
            }

            var viewModel = new AddPanoramaPointViewModel // Sử dụng ViewModel từ namespace BookingToursWeb.Models
            {
                LocationId = locationId,
                Location = location
            };
            ViewData["Title"] = $"Thêm Điểm Nhìn Panorama cho {location.Name}";
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPanoramaPoint(AddPanoramaPointViewModel model) // Sử dụng ViewModel từ namespace BookingToursWeb.Models
        {
            var location = await _context.Locations.FindAsync(model.LocationId);
            if (location == null)
            {
                TempData["ErrorMessage"] = "Địa điểm không tồn tại.";
                return RedirectToAction(nameof(ManagePanoramas));
            }
            model.Location = location;

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = $"Thêm Điểm Nhìn Panorama cho {model.Location.Name}";
                return View(model);
            }

            if (model.UploadedImageFile == null || model.UploadedImageFile.Length == 0)
            {
                ModelState.AddModelError("UploadedImageFile", "Vui lòng chọn một ảnh panorama để tải lên.");
                ViewData["Title"] = $"Thêm Điểm Nhìn Panorama cho {model.Location.Name}";
                return View(model);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(model.UploadedImageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("UploadedImageFile", "Chỉ cho phép file ảnh JPG hoặc PNG.");
                ViewData["Title"] = $"Thêm Điểm Nhìn Panorama cho {model.Location.Name}";
                return View(model);
            }

            if (model.UploadedImageFile.Length > 50 * 1024 * 1024) // 50 MB
            {
                ModelState.AddModelError("UploadedImageFile", "Kích thước ảnh không được vượt quá 50MB.");
                ViewData["Title"] = $"Thêm Điểm Nhìn Panorama cho {model.Location.Name}";
                return View(model);
            }

            string imageUrlForDb = string.Empty;
            try
            {
                string uploadsRootFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "panoramas");
                string locationSpecificFolder = Path.Combine(uploadsRootFolder, model.LocationId.ToString());

                string uniqueFolderName = Guid.NewGuid().ToString();
                string panoramaPointFolderPath = Path.Combine(locationSpecificFolder, uniqueFolderName);

                Directory.CreateDirectory(panoramaPointFolderPath);

                // Lưu file gốc với tên ban đầu vào thư mục riêng của panorama point
                string originalFileName = Path.GetFileName(model.UploadedImageFile.FileName);
                string filePath = Path.Combine(panoramaPointFolderPath, originalFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.UploadedImageFile.CopyToAsync(fileStream);
                }

                // --- ĐIỂM QUAN TRỌNG ĐÃ ĐƯỢC SỬA ĐỔI ---
                // Gán đường dẫn lưu vào DB. Đây sẽ là đường dẫn đến file ảnh gốc.
                // Ví dụ: /images/panoramas/{LocationId}/{uniqueGuid}/ten_file_goc.jpg
                imageUrlForDb = Path.Combine("/images", "panoramas", model.LocationId.ToString(), uniqueFolderName, originalFileName).Replace("\\", "/");

                var panoramaPoint = new PanoramaPoint
                {
                    LocationId = model.LocationId,
                    Name = model.Name,
                    Description = model.Description,
                    ImageUrl = imageUrlForDb // Gán đường dẫn đã tạo vào Model PanoramaPoint
                };

                _context.Add(panoramaPoint);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Điểm nhìn panorama đã được thêm thành công!";
                // Bỏ thông báo về Marzipano CLI vì không còn cần nữa
                return RedirectToAction(nameof(ListPanoramaPoints), new { locationId = model.LocationId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi thêm điểm nhìn panorama: {ex.Message}");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi thêm điểm nhìn panorama: " + ex.Message);
                ViewData["Title"] = $"Thêm Điểm Nhìn Panorama cho {model.Location.Name}";
                return View(model);
            }
        }

    }
}