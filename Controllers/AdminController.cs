using BCrypt.Net; // Thêm cho BCrypt, đảm bảo bạn đã cài đặt package BCrypt.Net-Next
using BookingToursWeb.Data;
using BookingToursWeb.Models;
using BookingToursWeb.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // Thêm để sử dụng DateTime
using System.Collections.Generic; // Thêm để sử dụng List
using System.Linq;
using System.Threading.Tasks;

namespace BookingToursWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger) // THÊM ILogger<AdminController> logger
        {
            _context = context;
            _logger = logger; // GÁN GIÁ TRỊ CHO _logger
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
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa địa điểm.";
                return RedirectToAction("Index", "Home");
            }

            // Tải địa điểm CÙNG VỚI các lịch hẹn và đánh giá liên quan
            var location = await _context.Locations
                                         .Include(l => l.Bookings) // RẤT QUAN TRỌNG: Tải Bookings liên quan
                                         .Include(l => l.Reviews)  // RẤT QUAN TRỌNG: Tải Reviews liên quan
                                         .FirstOrDefaultAsync(m => m.Id == id);

            if (location == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy địa điểm để xóa.";
                // Thay vì NotFound(), chúng ta sẽ chuyển hướng về ManageLocations để hiển thị lỗi thân thiện
                return RedirectToAction(nameof(ManageLocations));
            }

            // -------------------------------------------------------------------
            // THÊM LOGIC KIỂM TRA SỐ LƯỢNG LỊCH HẸN VÀ ĐÁNH GIÁ TRƯỚC KHI XÓA
            // -------------------------------------------------------------------

            if (location.Bookings.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa địa điểm này vì có các lịch hẹn liên quan. Vui lòng xóa tất cả các lịch hẹn của địa điểm này trước khi xóa.";
                return RedirectToAction(nameof(ManageLocations)); // Chuyển hướng về trang quản lý để hiển thị thông báo
            }

            if (location.Reviews.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa địa điểm này vì có các đánh giá liên quan. Vui lòng xóa tất cả các đánh giá của địa điểm này trước khi xóa.";
                return RedirectToAction(nameof(ManageLocations)); // Chuyển hướng về trang quản lý để hiển thị thông báo
            }

            // -------------------------------------------------------------------
            // Nếu không có lịch hẹn hoặc đánh giá liên quan, tiến hành xóa
            // -------------------------------------------------------------------
            try
            {
                _context.Locations.Remove(location);
                await _context.SaveChangesAsync(); // Thao tác xóa thực sự trong database

                TempData["SuccessMessage"] = "Địa điểm đã được xóa thành công.";
                return RedirectToAction(nameof(ManageLocations));
            }
            catch (DbUpdateException ex)
            {
                // Mặc dù chúng ta đã kiểm tra, nhưng đây là lớp bảo vệ cuối cùng nếu có lỗi ràng buộc khác xảy ra.
                // Ví dụ: có thể có một ràng buộc khác mà chúng ta chưa kiểm tra bằng .Any()
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi hệ thống khi xóa địa điểm. Vui lòng thử lại. Lỗi chi tiết: {ex.Message}";
                // Bạn có thể log `ex` chi tiết hơn nếu cần để debug.
                return RedirectToAction(nameof(ManageLocations));
            }
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

        // GET: Admin/ManagePanoramaViews (Action danh sách địa điểm có phân trang thủ công)
        public async Task<IActionResult> ManagePanoramaViews(int page = 1)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = "Quản lý Panorama theo Địa điểm";

            int pageSize = 10;
            int pageNumber = page;

            int totalLocations = await _context.Locations.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalLocations / pageSize);

            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;
            if (totalPages == 0) pageNumber = 1;

            var locations = await _context.Locations
                                        .OrderBy(l => l.Name)
                                        .Skip((pageNumber - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = (pageNumber > 1);
            ViewBag.HasNextPage = (pageNumber < totalPages);
            ViewBag.TotalLocations = totalLocations;

            return View(locations);
        }

        // GET: Admin/ManagePanoramaForLocation/{locationId}
        public async Task<IActionResult> ManagePanoramaForLocation(int locationId)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }

            var location = await _context.Locations
                                        .Include(l => l.PanoramaViews) // Tải kèm các PanoramaViews liên quan
                                        .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null)
            {
                TempData["ErrorMessage"] = "Địa điểm không tồn tại.";
                return RedirectToAction(nameof(ManagePanoramaViews));
            }

            ViewData["Title"] = $"Quản lý Panorama cho {location.Name}";

            var model = new LocationPanoramaViewModel
            {
                Location = location,
                PanoramaViews = location.PanoramaViews.OrderBy(pv => pv.Name).ToList(),
                NewPanoramaView = new PanoramaView { LocationId = location.Id } // Khởi tạo LocationId cho form thêm mới
            };

            return View(model);
        }

        // POST: Admin/AddPanoramaView
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPanoramaView(LocationPanoramaViewModel model)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Home");
            }

            // Đảm bảo LocationId được đặt đúng
            if (model.NewPanoramaView.LocationId == 0 && model.Location?.Id > 0)
            {
                model.NewPanoramaView.LocationId = model.Location.Id;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(model.NewPanoramaView);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Ảnh panorama đã được thêm thành công!";
                    return RedirectToAction(nameof(ManagePanoramaForLocation), new { locationId = model.NewPanoramaView.LocationId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi thêm ảnh panorama mới."); // Dòng này giờ sẽ hoạt động
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi thêm ảnh panorama: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin bạn đã nhập.";
            }

            // Nếu có lỗi (ModelState không hợp lệ hoặc Exception), tải lại dữ liệu để hiển thị lại View
            var location = await _context.Locations
                                        .Include(l => l.PanoramaViews)
                                        .FirstOrDefaultAsync(l => l.Id == model.NewPanoramaView.LocationId);
            if (location != null)
            {
                model.Location = location;
                model.PanoramaViews = location.PanoramaViews.OrderBy(pv => pv.Name).ToList();
            }
            else
            {
                // Xử lý trường hợp không tìm thấy location (rất hiếm nếu LocationId đúng)
                return RedirectToAction(nameof(ManagePanoramaViews));
            }
            return View(nameof(ManagePanoramaForLocation), model);
        }

    }
}