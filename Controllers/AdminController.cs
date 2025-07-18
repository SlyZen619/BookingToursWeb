using BCrypt.Net; // Thêm cho BCrypt, đảm bảo bạn đã cài đặt package BCrypt.Net-Next
using BookingToursWeb.Data;
using BookingToursWeb.Models;
using Microsoft.AspNetCore.Hosting; // Thêm dòng này để sử dụng IWebHostEnvironment
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System; // Thêm để sử dụng DateTime
using System.Collections.Generic; // Thêm để sử dụng List
using System.IO;
using System.Linq;
using System.Threading.Tasks;


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
        // Helper method MỚI để kiểm tra quyền LocationManager
        private bool IsCurrentUserLocationManager()
        {
            // Kiểm tra session để xác định quyền LocationManager
            return HttpContext.Session.GetString("IsLocationManager") == "True";
        }

        // Helper method MỚI để lấy ManagedLocationId của LocationManager hiện tại
        private int? GetCurrentManagedLocationId()
        {
            // Lấy ManagedLocationId từ Session
            var managedLocationIdString = HttpContext.Session.GetString("ManagedLocationId");
            if (int.TryParse(managedLocationIdString, out int locationId))
            {
                return locationId;
            }
            return null; // Trả về null nếu không tìm thấy hoặc không parse được
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
            // Chỉ Admin tổng mới được xem và quản lý tất cả người dùng
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang quản lý người dùng.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Quản lý Tài khoản người dùng";
            // Eager load ManagedLocation để hiển thị tên địa điểm nếu cần
            var users = await _context.Users.Include(u => u.ManagedLocation).ToListAsync();
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
            // Không cần truyền danh sách Locations ở đây vì ManagedLocationId được gán khi Edit
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

                // Kiểm tra trường hợp không hợp lệ: IsAdmin = true VÀ IsLocationManager = true
                if (model.IsAdmin && model.IsLocationManager)
                {
                    ModelState.AddModelError(string.Empty, "Không thể vừa là Admin vừa là Quản lý Địa điểm.");
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
                    IsAdmin = model.IsAdmin,
                    IsLocationManager = model.IsLocationManager, // GÁN GIÁ TRỊ MỚI
                    ManagedLocationId = null // Mặc định là null khi tạo mới, sẽ gán sau khi edit
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

            // Lấy User và Include ManagedLocation để hiển thị thông tin hiện tại
            var user = await _context.Users.Include(u => u.ManagedLocation).FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Lấy danh sách Locations để đổ vào DropDownList cho ManagedLocationId
            // Chỉ lấy những locations chưa có manager hoặc đang được quản lý bởi user này
            var availableLocations = await _context.Locations
                                        .Select(l => new SelectListItem
                                        {
                                            Value = l.Id.ToString(),
                                            Text = l.Name
                                        })
                                        .ToListAsync();

            // Thêm một mục "Không quản lý địa điểm nào"
            availableLocations.Insert(0, new SelectListItem { Value = "", Text = "-- Không quản lý địa điểm nào --" });

            ViewBag.AvailableLocations = availableLocations;

            // Chuyển User trực tiếp sang View để edit, View sẽ ánh xạ các thuộc tính
            return View(user);
        }

        // POST: Admin/EditUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, [Bind("Id,Username,Email,PhoneNumber,IsAdmin,IsLocationManager,ManagedLocationId")] User user)
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

            // Tải lại danh sách Locations cho DropDownList trong trường hợp ModelState không hợp lệ
            var availableLocations = await _context.Locations
                                    .Select(l => new SelectListItem
                                    {
                                        Value = l.Id.ToString(),
                                        Text = l.Name
                                    })
                                    .ToListAsync();
            availableLocations.Insert(0, new SelectListItem { Value = "", Text = "-- Không quản lý địa điểm nào --" });
            ViewBag.AvailableLocations = availableLocations;


            if (ModelState.IsValid)
            {
                // Kiểm tra trường hợp không hợp lệ: IsAdmin = true VÀ IsLocationManager = true
                if (user.IsAdmin && user.IsLocationManager)
                {
                    ModelState.AddModelError(string.Empty, "Không thể vừa là Admin vừa là Quản lý Địa điểm.");
                    return View(user);
                }

                try
                {
                    var userToUpdate = await _context.Users.FindAsync(id);
                    if (userToUpdate == null)
                    {
                        return NotFound();
                    }

                    // Kiểm tra xung đột ManagedLocationId (chỉ khi userToUpdate là LocationManager)
                    if (user.IsLocationManager && user.ManagedLocationId.HasValue)
                    {
                        // Tìm xem có User khác đang quản lý Location này không
                        var existingManager = await _context.Users
                                                    .Where(u => u.ManagedLocationId == user.ManagedLocationId && u.IsLocationManager && u.Id != user.Id)
                                                    .FirstOrDefaultAsync();
                        if (existingManager != null)
                        {
                            ModelState.AddModelError("ManagedLocationId", $"Địa điểm này đã được quản lý bởi người dùng: {existingManager.Username}.");
                            return View(user);
                        }
                    }
                    // Nếu user.IsLocationManager là false, ManagedLocationId phải là null
                    if (!user.IsLocationManager)
                    {
                        user.ManagedLocationId = null;
                    }

                    userToUpdate.Username = user.Username;
                    userToUpdate.Email = user.Email;
                    userToUpdate.PhoneNumber = user.PhoneNumber;
                    userToUpdate.IsAdmin = user.IsAdmin;
                    userToUpdate.IsLocationManager = user.IsLocationManager; // CẬP NHẬT TRƯỜNG MỚI
                    userToUpdate.ManagedLocationId = user.ManagedLocationId; // CẬP NHẬT TRƯỜNG MỚI

                    // Kiểm tra trùng lặp Username và Email
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

            // Logic để hủy gán ManagedLocationId nếu người dùng này đang quản lý một địa điểm
            // Vì ManagedLocationId là nullable và OnDelete(DeleteBehavior.Restrict) cho phép ManagedLocationId là null
            // nên việc xóa User sẽ không ảnh hưởng đến Location.
            // Nếu có dữ liệu liên quan khác (như Bookings, Reviews, Posts)
            // thì cần cân nhắc hành vi xóa cascade hoặc xóa thủ công các bản ghi liên quan
            // Hiện tại, cấu hình FK của bạn là Restrict, nên bạn phải đảm bảo không còn Booking/Review/Post nào của user này
            // trước khi xóa user. Hoặc thay đổi DeleteBehavior trong DbContext nếu muốn xóa cascade.
            // Ví dụ:
            // if (user.Bookings.Any()) { _context.Bookings.RemoveRange(user.Bookings); }
            // if (user.Reviews.Any()) { _context.Reviews.RemoveRange(user.Reviews); }
            // if (user.Posts.Any()) { _context.Posts.RemoveRange(user.Posts); }
            // -> Cần Include các Navigation Property này khi FindAsync(id) nếu bạn muốn xóa cascade.

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
            // Cả Admin và LocationManager đều có thể xem danh sách địa điểm (nhưng LocationManager chỉ xem của mình)
            if (!IsCurrentUserAdmin() && !IsCurrentUserLocationManager())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang quản lý địa điểm.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = "Quản lý Địa điểm";
            IQueryable<Location> locationsQuery = _context.Locations;

            // Nếu là LocationManager, chỉ cho phép xem địa điểm mà họ quản lý
            if (IsCurrentUserLocationManager())
            {
                var managedLocationId = GetCurrentManagedLocationId();
                if (managedLocationId.HasValue)
                {
                    locationsQuery = locationsQuery.Where(l => l.Id == managedLocationId.Value);
                }
                else
                {
                    TempData["ErrorMessage"] = "Bạn là quản lý địa điểm nhưng chưa được gán địa điểm nào.";
                    return RedirectToAction("Index", "Home"); // Hoặc một trang lỗi khác
                }
            }
            // Eager load LocationManagers để hiển thị manager của địa điểm (nếu có)
            var locations = await locationsQuery.Include(l => l.LocationManagers).ToListAsync();
            return View(locations);
        }

        // GET: Admin/AddLocation (Hiển thị form thêm địa điểm mới)
        [HttpGet]
        public IActionResult AddLocation()
        {
            // Chỉ Admin tổng mới được thêm địa điểm
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thêm địa điểm mới.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Thêm Địa điểm mới";

            // Truyền danh sách người dùng có thể làm quản lý địa điểm
            // Chỉ những người chưa là Admin và chưa là LocationManager của địa điểm khác
            var potentialManagers = _context.Users
                                        .Where(u => !u.IsAdmin && !(u.IsLocationManager && u.ManagedLocationId.HasValue))
                                        .Select(u => new SelectListItem
                                        {
                                            Value = u.Id.ToString(),
                                            Text = u.Username
                                        }).ToList();
            potentialManagers.Insert(0, new SelectListItem { Value = "", Text = "-- Chọn người quản lý (tùy chọn) --" });
            ViewBag.PotentialManagers = potentialManagers;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // CẬP NHẬT [Bind] ĐỂ BAO GỒM CÁC CỘT THANH TOÁN VÀ MANAGERUSERID
        public async Task<IActionResult> AddLocation([Bind("Name,Description,Information,Address,TicketPrice,OpeningHours,ImageUrl,ContactInfo,IsActive,Latitude,Longitude,BankName,BankAccountNumber,BankAccountName,PaymentInstructions")] Location location, int? ManagerUserId)
        {
            // Chỉ Admin tổng mới được thực hiện hành động này
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = "Thêm Địa điểm mới";

            // Tải lại danh sách potentialManagers trong trường hợp ModelState không hợp lệ
            var potentialManagers = _context.Users
                                        .Where(u => !u.IsAdmin && !(u.IsLocationManager && u.ManagedLocationId.HasValue))
                                        .Select(u => new SelectListItem
                                        {
                                            Value = u.Id.ToString(),
                                            Text = u.Username
                                        }).ToList();
            potentialManagers.Insert(0, new SelectListItem { Value = "", Text = "-- Chọn người quản lý (tùy chọn) --" });
            ViewBag.PotentialManagers = potentialManagers;

            if (ModelState.IsValid)
            {
                if (await _context.Locations.AnyAsync(l => l.Name == location.Name))
                {
                    ModelState.AddModelError("Name", "Tên địa điểm này đã tồn tại.");
                    return View(location);
                }

                _context.Add(location);
                await _context.SaveChangesAsync(); // Lưu Location trước để có Id

                // Nếu có ManagerUserId được chọn, cập nhật User đó
                if (ManagerUserId.HasValue)
                {
                    var managerUser = await _context.Users.FindAsync(ManagerUserId.Value);
                    if (managerUser != null)
                    {
                        // Kiểm tra xem user đã là quản lý của địa điểm khác chưa (hoặc là Admin)
                        if (managerUser.IsAdmin || (managerUser.IsLocationManager && managerUser.ManagedLocationId.HasValue))
                        {
                            ModelState.AddModelError("ManagerUserId", $"Người dùng '{managerUser.Username}' đã là Admin hoặc là quản lý của một địa điểm khác. Vui lòng chọn người dùng khác.");
                            // Cần xóa location vừa thêm để tránh dữ liệu rác
                            _context.Locations.Remove(location);
                            await _context.SaveChangesAsync();
                            return View(location);
                        }

                        managerUser.IsLocationManager = true;
                        managerUser.ManagedLocationId = location.Id; // Gán ID của địa điểm mới tạo
                        _context.Users.Update(managerUser);
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Địa điểm đã được thêm thành công!";
                return RedirectToAction(nameof(ManageLocations));
            }
            return View(location);
        }

        // GET: Admin/EditLocation/{id} (Hiển thị form sửa địa điểm)
        [HttpGet]
        public async Task<IActionResult> EditLocation(int? id)
        {
            // Cả Admin và LocationManager đều có thể chỉnh sửa địa điểm của mình
            if (!IsCurrentUserAdmin() && !IsCurrentUserLocationManager())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa địa điểm.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = "Sửa thông tin Địa điểm";

            if (id == null)
            {
                return NotFound();
            }

            IQueryable<Location> locationQuery = _context.Locations;

            // Nếu là LocationManager, chỉ cho phép chỉnh sửa địa điểm mà họ quản lý
            if (IsCurrentUserLocationManager())
            {
                var managedLocationId = GetCurrentManagedLocationId();
                if (!managedLocationId.HasValue || managedLocationId.Value != id.Value)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa địa điểm này.";
                    return RedirectToAction("ManageLocations"); // Chuyển hướng về trang quản lý địa điểm của họ
                }
            }

            // Include LocationManagers để biết ai đang quản lý địa điểm này
            var location = await locationQuery.Include(l => l.LocationManagers).FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
            {
                return NotFound();
            }

            // Lấy danh sách người dùng tiềm năng làm quản lý
            // Bao gồm:
            // 1. Những người chưa là Admin và chưa quản lý địa điểm nào
            // 2. Người hiện tại đang là quản lý của địa điểm này (để vẫn hiển thị trong dropdown)
            var currentManager = location.LocationManagers?.FirstOrDefault(u => u.IsLocationManager);
            var currentManagerId = currentManager?.Id;


            var potentialManagers = await _context.Users
                                        .Where(u => (!u.IsAdmin && !(u.IsLocationManager && u.ManagedLocationId.HasValue)) || (u.Id == currentManagerId))
                                        .Select(u => new SelectListItem
                                        {
                                            Value = u.Id.ToString(),
                                            Text = u.Username
                                        })
                                        .ToListAsync();
            potentialManagers.Insert(0, new SelectListItem { Value = "", Text = "-- Không gán quản lý --" });
            ViewBag.PotentialManagers = potentialManagers;

            // Gán ID của người quản lý hiện tại để chọn đúng trong dropdown
            ViewBag.SelectedManagerId = currentManagerId;

            return View(location);
        }

        // POST: Admin/EditLocation/{id} (Xử lý sửa địa điểm)
        [HttpPost]
        [ValidateAntiForgeryToken]
        // CẬP NHẬT [Bind] ĐỂ BAO GỒM CÁC CỘT THANH TOÁN VÀ MANAGERUSERID
        public async Task<IActionResult> EditLocation(int id, [Bind("Id,Name,Description,Information,Address,TicketPrice,OpeningHours,ImageUrl,ContactInfo,IsActive,Latitude,Longitude,BankName,BankAccountNumber,BankAccountName,PaymentInstructions")] Location location, int? ManagerUserId)
        {
            // Cả Admin và LocationManager đều có thể chỉnh sửa địa điểm của mình
            if (!IsCurrentUserAdmin() && !IsCurrentUserLocationManager())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa địa điểm.";
                return RedirectToAction("Index", "Home");
            }

            // Nếu là LocationManager, chỉ cho phép chỉnh sửa địa điểm mà họ quản lý
            if (IsCurrentUserLocationManager())
            {
                var managedLocationId = GetCurrentManagedLocationId();
                if (!managedLocationId.HasValue || managedLocationId.Value != id)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa địa điểm này.";
                    return RedirectToAction("ManageLocations");
                }
            }

            ViewData["Title"] = "Sửa thông tin Địa điểm";

            if (id != location.Id)
            {
                return NotFound();
            }

            // Lấy thông tin quản lý hiện tại của địa điểm từ DB để tái tạo DropDownList nếu ModelState không hợp lệ
            var locationCurrentInDb = await _context.Locations.Include(l => l.LocationManagers).AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
            var currentManagerOnPost = locationCurrentInDb?.LocationManagers?.FirstOrDefault(u => u.IsLocationManager);
            var currentManagerIdOnPost = currentManagerOnPost?.Id;

            var potentialManagers = await _context.Users
                                        .Where(u => (!u.IsAdmin && !(u.IsLocationManager && u.ManagedLocationId.HasValue)) || (u.Id == currentManagerIdOnPost))
                                        .Select(u => new SelectListItem
                                        {
                                            Value = u.Id.ToString(),
                                            Text = u.Username
                                        })
                                        .ToListAsync();
            potentialManagers.Insert(0, new SelectListItem { Value = "", Text = "-- Không gán quản lý --" });
            ViewBag.PotentialManagers = potentialManagers;
            ViewBag.SelectedManagerId = ManagerUserId; // Giữ lại giá trị người dùng đã chọn


            if (ModelState.IsValid)
            {
                try
                {
                    if (_context.Locations.Any(l => l.Name == location.Name && l.Id != location.Id))
                    {
                        ModelState.AddModelError("Name", "Tên địa điểm này đã tồn tại.");
                        return View(location);
                    }

                    // Lấy đối tượng từ DB để đảm bảo chỉ cập nhật các thuộc tính được phép bởi [Bind]
                    var locationToUpdate = await _context.Locations.Include(l => l.LocationManagers).FirstOrDefaultAsync(l => l.Id == id);
                    if (locationToUpdate == null)
                    {
                        return NotFound();
                    }

                    // Ánh xạ các giá trị từ đối tượng được bind vào đối tượng từ DB
                    locationToUpdate.Name = location.Name;
                    locationToUpdate.Description = location.Description;
                    locationToUpdate.Information = location.Information;
                    locationToUpdate.Address = location.Address;
                    locationToUpdate.TicketPrice = location.TicketPrice;
                    locationToUpdate.OpeningHours = location.OpeningHours;
                    locationToUpdate.ImageUrl = location.ImageUrl;
                    locationToUpdate.ContactInfo = location.ContactInfo;
                    locationToUpdate.IsActive = location.IsActive;
                    locationToUpdate.Latitude = location.Latitude;
                    locationToUpdate.Longitude = location.Longitude;
                    // CẬP NHẬT CÁC CỘT THANH TOÁN MỚI
                    locationToUpdate.BankName = location.BankName;
                    locationToUpdate.BankAccountNumber = location.BankAccountNumber;
                    locationToUpdate.BankAccountName = location.BankAccountName;
                    locationToUpdate.PaymentInstructions = location.PaymentInstructions;

                    // Xử lý việc gán/hủy gán quản lý địa điểm
                    var oldManager = locationToUpdate.LocationManagers?.FirstOrDefault(u => u.IsLocationManager && u.ManagedLocationId == id); // Đảm bảo đúng địa điểm

                    // Case 1: Gán quản lý mới (hoặc thay đổi quản lý)
                    if (ManagerUserId.HasValue)
                    {
                        var newManager = await _context.Users.FindAsync(ManagerUserId.Value);
                        if (newManager != null)
                        {
                            // Kiểm tra nếu người dùng mới đã là admin hoặc đang quản lý địa điểm khác
                            if (newManager.IsAdmin || (newManager.IsLocationManager && newManager.ManagedLocationId.HasValue && newManager.ManagedLocationId.Value != id))
                            {
                                ModelState.AddModelError("ManagerUserId", $"Người dùng '{newManager.Username}' không thể được gán làm quản lý. Người dùng đó đã là Admin hoặc đang quản lý địa điểm khác.");
                                // Để Model State hợp lệ cho View, cần detach locationToUpdate
                                _context.Entry(locationToUpdate).State = EntityState.Detached; // Detach để tránh lỗi theo dõi
                                return View(location);
                            }

                            // Nếu có quản lý cũ khác với quản lý mới, hủy gán quản lý cũ
                            if (oldManager != null && oldManager.Id != newManager.Id)
                            {
                                oldManager.IsLocationManager = false;
                                oldManager.ManagedLocationId = null;
                                _context.Users.Update(oldManager);
                            }

                            // Gán quản lý mới
                            newManager.IsLocationManager = true;
                            newManager.ManagedLocationId = id;
                            _context.Users.Update(newManager);
                        }
                    }
                    // Case 2: Hủy gán quản lý (ManagerUserId is null)
                    else
                    {
                        if (oldManager != null)
                        {
                            oldManager.IsLocationManager = false;
                            oldManager.ManagedLocationId = null;
                            _context.Users.Update(oldManager);
                        }
                    }

                    _context.Update(locationToUpdate); // Cập nhật Location
                    await _context.SaveChangesAsync(); // Lưu tất cả thay đổi (Location và User Managers)

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
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi cập nhật địa điểm ID: {id}");
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật địa điểm. Vui lòng thử lại.");
                    return View(location);
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
            if (!IsCurrentUserAdmin()) // Chỉ Admin tổng mới được xóa địa điểm
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa địa điểm.";
                return RedirectToAction("Index", "Home");
            }

            // Eager load tất cả các mối quan hệ liên quan để xử lý trước khi xóa Location
            var location = await _context.Locations
                                        .Include(l => l.Bookings)
                                        .Include(l => l.Reviews)
                                        .Include(l => l.PanoramaPoints)
                                        .Include(l => l.LocationManagers) // Thêm Include cho LocationManagers
                                        .FirstOrDefaultAsync(m => m.Id == id);

            if (location == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy địa điểm cần xóa.";
                return NotFound();
            }

            // Hủy gán người quản lý địa điểm này nếu có
            if (location.LocationManagers != null && location.LocationManagers.Any())
            {
                foreach (var manager in location.LocationManagers)
                {
                    // Chỉ hủy gán nếu họ thực sự đang quản lý địa điểm này
                    if (manager.ManagedLocationId == id && manager.IsLocationManager)
                    {
                        manager.IsLocationManager = false;
                        manager.ManagedLocationId = null;
                        _context.Users.Update(manager);
                    }
                }
            }


            // Bước 1: Xóa các thư mục ảnh panorama con liên quan đến từng PanoramaPoint
            var panoramaPoints = location.PanoramaPoints;

            if (panoramaPoints != null && panoramaPoints.Any())
            {
                foreach (var panoramaPoint in panoramaPoints)
                {
                    var relativeImageUrl = panoramaPoint.ImageUrl;

                    if (!string.IsNullOrEmpty(relativeImageUrl))
                    {
                        string fullPhysicalPathFromDb = Path.Combine(_webHostEnvironment.WebRootPath, relativeImageUrl.TrimStart('/'));
                        string? panoramaPointFolderPath = Path.GetDirectoryName(fullPhysicalPathFromDb);

                        if (!string.IsNullOrEmpty(panoramaPointFolderPath) && Directory.Exists(panoramaPointFolderPath))
                        {
                            try
                            {
                                Directory.Delete(panoramaPointFolderPath, true); // Xóa toàn bộ thư mục và nội dung bên trong
                                _logger.LogInformation($"Đã xóa thư mục panorama: {panoramaPointFolderPath}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Lỗi khi xóa thư mục panorama {panoramaPointFolderPath} cho điểm {panoramaPoint.Id}.");
                                // Không ném lỗi để tiếp tục xóa các thành phần khác
                            }
                        }
                    }
                }
            }

            // Bước 2: Xóa thư mục gốc của địa điểm nếu nó tồn tại và trống
            string locationRootFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "panoramas", id.ToString()); // Dùng id trực tiếp
            if (Directory.Exists(locationRootFolder))
            {
                try
                {
                    // Kiểm tra xem nó có rỗng không trước khi xóa, để tránh xóa nhầm dữ liệu không liên quan
                    if (!Directory.EnumerateFileSystemEntries(locationRootFolder).Any())
                    {
                        Directory.Delete(locationRootFolder, false); // false vì đã kiểm tra rỗng
                        _logger.LogInformation($"Đã xóa thư mục địa điểm gốc rỗng: {locationRootFolder}");
                    }
                    else
                    {
                        _logger.LogWarning($"Thư mục địa điểm gốc '{locationRootFolder}' không rỗng, không xóa tự động (cần kiểm tra thủ công).");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi xóa thư mục gốc địa điểm {locationRootFolder}.");
                }
            }

            // Bước 3: Xóa Location và các Bookings, Reviews, PanoramaPoints khỏi database
            // Vì các mối quan hệ được cấu hình là DeleteBehavior.Restrict, bạn phải xóa các bản ghi con trước.
            // Đoạn code dưới đây đã làm điều này bằng cách RemoveRange các collections.
            if (location.Bookings != null && location.Bookings.Any())
            {
                _context.Bookings.RemoveRange(location.Bookings);
            }

            if (location.Reviews != null && location.Reviews.Any())
            {
                _context.Reviews.RemoveRange(location.Reviews);
            }

            if (location.PanoramaPoints != null && location.PanoramaPoints.Any())
            {
                _context.PanoramaPoints.RemoveRange(location.PanoramaPoints);
            }

            _context.Locations.Remove(location);

            // Lưu tất cả các thay đổi vào database
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

        // GET: /Admin/ManageReviews
        // Action này sẽ hiển thị danh sách các địa điểm để người dùng chọn xem đánh giá của địa điểm nào
        public async Task<IActionResult> ManageReviews()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home"); // Chuyển hướng về trang chủ nếu không có quyền
            }

            ViewData["Title"] = "Quản lý Đánh giá theo Địa điểm"; // Tiêu đề của trang quản lý đánh giá tổng quát
            var locations = await _context.Locations.ToListAsync(); // Lấy tất cả địa điểm từ database

            // Truyền danh sách địa điểm sang View
            return View(locations);
        }

        // GET: /Admin/ListReviewsForLocation/{locationId}
        // Action này sẽ hiển thị danh sách đánh giá cho một địa điểm cụ thể
        public async Task<IActionResult> ListReviewsForLocation(int locationId)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home"); // Chuyển hướng về trang chủ nếu không có quyền
            }

            // Tìm địa điểm và include các đánh giá cùng với thông tin người dùng đánh giá
            var location = await _context.Locations
                                         .Include(l => l.Reviews) // Tải các đánh giá liên quan
                                             .ThenInclude(r => r.User) // Tải thông tin User cho mỗi đánh giá
                                         .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null)
            {
                TempData["ErrorMessage"] = "Địa điểm không tồn tại hoặc không tìm thấy.";
                return RedirectToAction("ManageReviews"); // Quay lại trang quản lý đánh giá tổng quát nếu không tìm thấy địa điểm
            }

            ViewBag.LocationName = location.Name; // Dùng để hiển thị tên địa điểm trên View
            ViewData["Title"] = $"Đánh giá cho: {location.Name}"; // Đặt tiêu đề cho View

            // Truyền danh sách đánh giá của địa điểm đó sang View
            return View(location.Reviews.ToList());
        }

        // POST: /Admin/DeleteReview/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Home"); // Chuyển hướng về trang chủ nếu không có quyền
            }

            var review = await _context.Reviews.FindAsync(id);

            if (review == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá để xóa.";
                return RedirectToAction("ManageReviews", "Admin");
            }

            var locationId = review.LocationId; // Lấy LocationId để chuyển hướng đúng

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đánh giá đã được xóa thành công.";
            return RedirectToAction("ListReviewsForLocation", "Admin", new { locationId = locationId });
        }

        [HttpGet]
        public async Task<IActionResult> ManagePosts()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Admin");
            }

            var allPosts = await _context.Posts
                                      .Include(p => p.Category)
                                      .Include(p => p.Author)
                                      .OrderByDescending(p => p.PublishedAt)
                                      .ToListAsync();

            var allCategories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            var categoriesWithPosts = new List<CategoryWithPosts>();

            foreach (var category in allCategories)
            {
                var postsInThisCategory = allPosts.Where(p => p.CategoryId == category.Id).ToList();
                categoriesWithPosts.Add(new CategoryWithPosts
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    Posts = postsInThisCategory
                });
            }

            var viewModel = new PostManagementViewModel // <--- Giờ đây nó sẽ được tìm thấy trong BookingToursWeb.Models
            {
                CategoriesWithPosts = categoriesWithPosts
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> AddPost()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Admin");
            }

            // Lấy danh sách các danh mục để đổ vào DropdownList
            ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPost([Bind("Title,Content,CategoryId,ImageUrl")] Post post, IFormFile? imageFile)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Admin");
            }

            // Lấy AuthorId từ session (người dùng hiện tại)
            // THAY ĐỔI TỪ GetString SANG GetInt32
            var currentUserIdInt = HttpContext.Session.GetInt32("UserId");
            if (currentUserIdInt == null) // Kiểm tra nếu giá trị là null (không có trong session hoặc lỗi)
            {
                ModelState.AddModelError("", "Không thể xác định tác giả bài viết. Vui lòng đăng nhập lại.");
                ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", post.CategoryId);
                _logger.LogWarning("Không thể lấy UserId (int) từ session. UserIdInt is NULL.");
                return View(post);
            }
            post.AuthorId = currentUserIdInt.Value; // Gán AuthorId từ session (sử dụng .Value để lấy giá trị int)

            // Đặt PublishedAt và UpdatedAt thủ công
            post.PublishedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;

            // Xử lý upload hình ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "post_images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                post.ImageUrl = "/uploads/post_images/" + uniqueFileName;
            }

            if (ModelState.IsValid)
            {
                _context.Add(post);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Bài đăng đã được thêm thành công.";
                return RedirectToAction(nameof(ManagePosts));
            }

            ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", post.CategoryId);
            return View(post);
        }

        [HttpGet]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Admin");
            }

            if (id == null)
            {
                return NotFound();
            }

            // Lấy bài đăng theo ID, bao gồm thông tin Category và Author
            var post = await _context.Posts
                                     .Include(p => p.Category)
                                     .Include(p => p.Author)
                                     .FirstOrDefaultAsync(m => m.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            // Lấy danh sách các danh mục để đổ vào DropdownList
            ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", post.CategoryId);
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id, [Bind("Id,Title,Content,CategoryId,ImageUrl,AuthorId,PublishedAt")] Post post, IFormFile? newImageFile)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Admin");
            }

            if (id != post.Id)
            {
                return NotFound();
            }

            // Lấy bài đăng gốc từ database để giữ lại các giá trị không được bind từ form (ví dụ: AuthorId, PublishedAt nếu không có trong Bind)
            var postToUpdate = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id); // Dùng AsNoTracking để tránh lỗi theo dõi nếu update

            if (postToUpdate == null)
            {
                return NotFound();
            }

            // Re-populate CategoryList if ModelState is invalid or for initial view
            ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", post.CategoryId);

            if (ModelState.IsValid)
            {
                try
                {
                    // Giữ lại AuthorId và PublishedAt gốc, chỉ cập nhật các trường được phép
                    // Hoặc bạn có thể thêm AuthorId và PublishedAt vào [Bind] nếu muốn hiển thị chúng ở form nhưng chỉ đọc
                    // Nếu AuthorId không nằm trong bind, thì post.AuthorId sẽ là 0, cần lấy lại từ postToUpdate
                    if (post.AuthorId == 0) // Điều này xảy ra nếu AuthorId không được bind từ form
                    {
                        post.AuthorId = postToUpdate.AuthorId;
                    }
                    if (post.PublishedAt == default(DateTime)) // Điều này xảy ra nếu PublishedAt không được bind từ form
                    {
                        post.PublishedAt = postToUpdate.PublishedAt;
                    }
                    post.UpdatedAt = DateTime.UtcNow; // Cập nhật thời gian chỉnh sửa

                    // Xử lý hình ảnh mới
                    if (newImageFile != null && newImageFile.Length > 0)
                    {
                        // Xóa hình ảnh cũ nếu có và không phải là hình ảnh mặc định (nếu bạn có)
                        if (!string.IsNullOrEmpty(postToUpdate.ImageUrl) && postToUpdate.ImageUrl != "/placeholder.jpg") // Ví dụ: không xóa placeholder
                        {
                            var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, postToUpdate.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "post_images");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + newImageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await newImageFile.CopyToAsync(fileStream);
                        }
                        post.ImageUrl = "/uploads/post_images/" + uniqueFileName; // Cập nhật URL hình ảnh mới
                    }
                    else // Nếu không có file mới, giữ lại hình ảnh cũ
                    {
                        post.ImageUrl = postToUpdate.ImageUrl;
                    }

                    _context.Update(post);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Bài đăng đã được cập nhật thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManagePosts));
            }
            return View(post);
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.Id == id);
        }

        
        // POST: Admin/DeletePost/5
        [HttpPost, ActionName("DeletePost")] // Đặt tên Action là DeletePost, nhưng ActionName là DeletePost
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePostConfirmed(int id)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Admin");
            }

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                TempData["ErrorMessage"] = "Bài đăng không tồn tại.";
                return RedirectToAction(nameof(ManagePosts));
            }

            // Xóa hình ảnh liên quan nếu có
            if (!string.IsNullOrEmpty(post.ImageUrl))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, post.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    try
                    {
                        System.IO.File.Delete(imagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi xóa file hình ảnh cho bài đăng ID {PostId}: {ImagePath}", post.Id, imagePath);
                        // Bạn có thể chọn không hiển thị lỗi này cho người dùng cuối
                        // hoặc ghi log và tiếp tục xóa bài đăng nếu việc xóa file không quá quan trọng.
                    }
                }
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Bài đăng đã được xóa thành công.";
            return RedirectToAction(nameof(ManagePosts));
        }

        [HttpGet]
        public async Task<IActionResult> ManageCategories()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Admin"); // Chuyển hướng về Admin Dashboard hoặc trang chính
            }

            // Lấy tất cả các danh mục từ database
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult AddCategory()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Admin");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory([Bind("Name")] Category category)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Admin");
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên danh mục (không phân biệt chữ hoa/thường để tránh trùng lặp logic)
                if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == category.Name.ToLower()))
                {
                    ModelState.AddModelError("Name", "Tên danh mục đã tồn tại.");
                    return View(category);
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Danh mục đã được thêm thành công.";
                return RedirectToAction(nameof(ManageCategories));
            }
            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int? id)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Admin");
            }

            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, [Bind("Id,Name")] Category category)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Admin");
            }

            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra trùng tên danh mục (trừ chính danh mục đang sửa, không phân biệt chữ hoa/thường)
                    if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id))
                    {
                        ModelState.AddModelError("Name", "Tên danh mục đã tồn tại.");
                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Danh mục đã được cập nhật thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageCategories));
            }
            return View(category);
        }

        [HttpPost, ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryConfirmed(int id)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Admin");
            }

            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                try
                {
                    _context.Categories.Remove(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Danh mục đã được xóa thành công.";
                }
                catch (DbUpdateException ex) // Bắt lỗi khi có khóa ngoại
                {
                    // Log lỗi để debug
                    _logger.LogError(ex, "Lỗi khi xóa danh mục {CategoryId}", id);

                    // Kiểm tra nếu lỗi là do vi phạm ràng buộc khóa ngoại (posts vẫn liên kết)
                    if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 547) // Lỗi 547 là lỗi ràng buộc khóa ngoại
                    {
                        TempData["ErrorMessage"] = "Không thể xóa danh mục này vì có bài đăng đang liên kết đến nó. Vui lòng xóa hoặc chuyển các bài đăng đó sang danh mục khác trước.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Đã xảy ra lỗi không xác định khi xóa danh mục.";
                    }
                    return RedirectToAction(nameof(ManageCategories));
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục để xóa.";
            }
            return RedirectToAction(nameof(ManageCategories));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}