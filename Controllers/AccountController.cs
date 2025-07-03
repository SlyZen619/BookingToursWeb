using Microsoft.AspNetCore.Mvc;
using BookingToursWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BookingToursWeb.Data;
using Microsoft.AspNetCore.Http; // Thêm namespace này để sử dụng Session
using System; // Thêm namespace này cho StringComparison

namespace BookingToursWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Account/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            ViewData["Title"] = "Đăng ký tài khoản";
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                    return View(model);
                }

                if (_context.Users.Any(u => u.Email == model.Email))
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
                    IsAdmin = false
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
        [AllowAnonymous]
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
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

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

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

            if (user.IsAdmin)
            {
                TempData["AdminLoginMessage"] = $"Chào mừng Admin {user.Username}!";
            }
            else
            {
                TempData["UserLoginMessage"] = $"Chào mừng {user.Username}!";
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Logout - Xử lý đăng xuất
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Login", "Account");
        }

        // GET: Account/ForgotPassword (Chỉ yêu cầu Email)
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            ViewData["Title"] = "Quên mật khẩu";
            return View();
        }

        // POST: Account/ForgotPassword (Bước 1: Xác nhận Email tồn tại)
        [HttpPost]
        [AllowAnonymous]
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
        // Trong AccountController, action ResetPassword (GET)
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
                NewPassword = string.Empty, // Khởi tạo với chuỗi rỗng để tránh lỗi "Required member must be set"
                ConfirmPassword = string.Empty // Khởi tạo với chuỗi rỗng
            };
            return View(model);
        }

        // POST: Account/ResetPassword (Bước 2: Cập nhật mật khẩu mới)
        [HttpPost]
        [AllowAnonymous]
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

                // --- LOGIC MỚI ĐỂ XỬ LÝ USERNAME ---
                // Chỉ kiểm tra và cập nhật Username nếu nó đã thay đổi
                if (!string.Equals(userToUpdate.Username, model.Username, StringComparison.OrdinalIgnoreCase))
                {
                    // Kiểm tra Username mới đã tồn tại cho người dùng khác chưa
                    if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.Id != model.Id))
                    {
                        ModelState.AddModelError("Username", "Tên tài khoản này đã được sử dụng bởi người khác.");
                        return View(model);
                    }
                    userToUpdate.Username = model.Username; // Cập nhật Username
                }
                // --- KẾT THÚC LOGIC USERNAME ---


                // Kiểm tra Email đã tồn tại cho người dùng khác chưa (logic cũ, vẫn giữ)
                if (!string.Equals(userToUpdate.Email, model.Email, StringComparison.OrdinalIgnoreCase) &&
                    await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != model.Id))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                    return View(model);
                }
                userToUpdate.Email = model.Email; // Cập nhật Email

                userToUpdate.PhoneNumber = model.PhoneNumber; // Cập nhật SĐT

                _context.Update(userToUpdate);
                await _context.SaveChangesAsync();

                // Cập nhật lại Session nếu Username hoặc Email thay đổi
                HttpContext.Session.SetString("Username", userToUpdate.Username); // Cập nhật Username trong Session
                HttpContext.Session.SetString("Email", userToUpdate.Email); // Cập nhật Email trong Session

                TempData["SuccessMessage"] = "Thông tin profile đã được cập nhật thành công.";
                return RedirectToAction("Profile", "Home");
            }

            return View(model);
        }
    }
}