using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingToursWeb.Data;
using BookingToursWeb.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;

namespace BookingToursWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsCurrentUserAdmin()
        {
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

        // GET: Admin/ManageAppointments
        public IActionResult ManageAppointments()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Quản lý Lịch hẹn";
            return View();
        }

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

        // GET: Admin/AddLocation (Hiển thị form thêm địa điểm mới) - Giữ nguyên
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
        // CẬP NHẬT DANH SÁCH THUỘC TÍNH TRONG [Bind] để thêm "Information"
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

        // GET: Admin/EditLocation/{id} (Hiển thị form sửa địa điểm) - Giữ nguyên
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
        // CẬP NHẬT DANH SÁCH THUỘC TÍNH TRONG [Bind] để thêm "Information"
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

        // POST: Admin/DeleteLocation/{id} (Xử lý xóa địa điểm) - Giữ nguyên
        [HttpPost, ActionName("DeleteLocation")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocationConfirmed(int id)
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa địa điểm.";
                return RedirectToAction("Index", "Home");
            }

            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy địa điểm để xóa.";
                return NotFound();
            }

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Địa điểm đã được xóa thành công.";
            return RedirectToAction(nameof(ManageLocations));
        }

        // ----------------------------------------------------
        // CÁC CHỨC NĂNG SỬA/XÓA NGƯỜI DÙNG ĐÃ CÓ TRƯỚC ĐÓ

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
    }
}