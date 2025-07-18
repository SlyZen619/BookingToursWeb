using Microsoft.AspNetCore.Mvc;
using BookingToursWeb.Models;
// using Microsoft.AspNetCore.Authorization; // Bỏ đi nếu không dùng [AllowAnonymous]
using Microsoft.EntityFrameworkCore;
using BookingToursWeb.Data;
using Microsoft.AspNetCore.Http; // Cần thiết để truy cập Session
using System; // Thêm namespace này cho StringComparison
using System.Linq; // Thêm để sử dụng .Any() và các LINQ methods khác
using System.Threading.Tasks; // Thêm để sử dụng Task

namespace BookingToursWeb.Controllers
{
    // Bỏ [Authorize] và [AllowAnonymous] nếu bạn không dùng hệ thống Auth chuẩn
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Account/Register
        // [AllowAnonymous] // Không cần nếu không dùng Auth chuẩn
        public IActionResult Register()
        {
            ViewData["Title"] = "Đăng ký tài khoản";
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        // [AllowAnonymous] // Không cần nếu không dùng Auth chuẩn
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                    return View(model);
                }

                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một tài khoản khác.");
                    return View(model);
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PasswordHash = hashedPassword,
                    IsAdmin = false,
                    IsLocationManager = false,
                    ManagedLocationId = null
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Đăng ký tài khoản";
            return View(model);
        }

        // GET: Account/Login
        // [AllowAnonymous] // Không cần nếu không dùng Auth chuẩn
        public IActionResult Login()
        {
            ViewData["Title"] = "Đăng nhập";
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }
            return View();
        }

        // POST: Account/Login - Xử lý đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        // [AllowAnonymous] // Không cần nếu không dùng Auth chuẩn
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users
                                     .Include(u => u.ManagedLocation)
                                     .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                ViewData["Title"] = "Đăng nhập";
                return View();
            }

            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isPasswordCorrect)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                ViewData["Title"] = "Đăng nhập";
                return View();
            }

            // --- BẮT ĐẦU: CHỈ LƯU THÔNG TIN NGƯỜI DÙNG VÀO SESSION ---
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

            HttpContext.Session.SetString("IsLocationManager", user.IsLocationManager.ToString());
            if (user.IsLocationManager && user.ManagedLocationId.HasValue)
            {
                HttpContext.Session.SetInt32("ManagedLocationId", user.ManagedLocationId.Value);
                HttpContext.Session.SetString("ManagedLocationName", user.ManagedLocation?.Name ?? "Địa điểm không xác định");
            }
            else
            {
                HttpContext.Session.Remove("ManagedLocationId");
                HttpContext.Session.Remove("ManagedLocationName");
            }
            // --- KẾT THÚC: CHỈ LƯU THÔNG TIN NGƯỜI DÙNG VÀO SESSION ---

            if (user.IsAdmin)
            {
                TempData["AdminLoginMessage"] = $"Chào mừng Admin {user.Username}!";
                return RedirectToAction("Index", "Admin");
            }
            else if (user.IsLocationManager)
            {
                string managedLocationName = user.ManagedLocation?.Name ?? "Địa điểm của bạn";
                TempData["LocationManagerLoginMessage"] = $"Chào mừng quản lý địa điểm {managedLocationName} ({user.Username})!";
                return RedirectToAction("Index", "LocationManager");
            }
            else
            {
                TempData["UserLoginMessage"] = $"Chào mừng {user.Username}!";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Account/Logout - Xử lý đăng xuất
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Xóa toàn bộ Session
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Login", "Account");
        }

        // GET: Account/ForgotPassword (Chỉ yêu cầu Email)
        // [AllowAnonymous] // Không cần nếu không dùng Auth chuẩn
        public IActionResult ForgotPassword()
        {
            ViewData["Title"] = "Quên mật khẩu";
            return View();
        }

        // POST: Account/ForgotPassword (Bước 1: Xác nhận Email tồn tại)
        [HttpPost]
        // [AllowAnonymous] // Không cần nếu không dùng Auth chuẩn
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Quên mật khẩu";
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Email không tồn tại trong hệ thống.");
                ViewData["Title"] = "Quên mật khẩu";
                return View(model);
            }

            TempData["UserEmailForReset"] = model.Email;
            return RedirectToAction(nameof(ResetPassword));
        }

        // GET: Account/ResetPassword (Nhận email từ TempData, hiển thị form đổi mật khẩu)
        public IActionResult ResetPassword()
        {
            ViewData["Title"] = "Đặt lại mật khẩu";
            string? userEmail = TempData["UserEmailForReset"] as string;

            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập email của bạn trước.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel
            {
                Email = userEmail,
                NewPassword = string.Empty,
                ConfirmPassword = string.Empty
            };
            return View(model);
        }

        // POST: Account/ResetPassword (Bước 2: Cập nhật mật khẩu mới)
        [HttpPost]
        // [AllowAnonymous] // Không cần nếu không dùng Auth chuẩn
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["UserEmailForReset"] = model.Email;
                ViewData["Title"] = "Đặt lại mật khẩu";
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                ViewData["Title"] = "Đặt lại mật khẩu";
                return View(model);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mật khẩu của bạn đã được đặt lại thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        // GET: Account/EditProfile
        public async Task<IActionResult> EditProfile()
        {
            ViewData["Title"] = "Chỉnh sửa Profile";

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để chỉnh sửa thông tin cá nhân.";
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Index", "Home");
            }

            var model = new EditProfileViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        // POST: Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            ViewData["Title"] = "Chỉnh sửa Profile";

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId.Value != model.Id)
            {
                TempData["ErrorMessage"] = "Phiên làm việc không hợp lệ hoặc bạn không có quyền chỉnh sửa.";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                var userToUpdate = await _context.Users.FindAsync(model.Id);
                if (userToUpdate == null)
                {
                    TempData["ErrorMessage"] = "Người dùng không tồn tại.";
                    return RedirectToAction("Index", "Home");
                }

                if (!string.Equals(userToUpdate.Username, model.Username, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.Id != model.Id))
                    {
                        ModelState.AddModelError("Username", "Tên tài khoản này đã được sử dụng bởi người khác.");
                        return View(model);
                    }
                    userToUpdate.Username = model.Username;
                }

                if (!string.Equals(userToUpdate.Email, model.Email, StringComparison.OrdinalIgnoreCase) &&
                    await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != model.Id))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                    return View(model);
                }
                userToUpdate.Email = model.Email;

                userToUpdate.PhoneNumber = model.PhoneNumber;

                _context.Update(userToUpdate);
                await _context.SaveChangesAsync();

                // Cập nhật lại Session nếu Username hoặc Email thay đổi
                HttpContext.Session.SetString("Username", userToUpdate.Username);
                HttpContext.Session.SetString("Email", userToUpdate.Email);

                TempData["SuccessMessage"] = "Thông tin profile đã được cập nhật thành công.";
                return RedirectToAction("Profile", "Home");
            }

            return View(model);
        }
    }
}