using Microsoft.AspNetCore.Mvc;
using BookingToursWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BookingToursWeb.Data;

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
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]// Quan trọng để ngăn chặn CSRF attacks
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra tên đăng nhập và email đã tồn tại chưa
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

                // 2. Hash mật khẩu trước khi lưu
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // 3. Tạo đối tượng User từ RegisterViewModel
                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PasswordHash = hashedPassword,
                    IsAdmin = false // Mặc định là người dùng thường khi đăng ký
                };

                // 4. Thêm User vào DbContext và lưu vào CSDL
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync(); // Lưu thay đổi bất đồng bộ

                // 5. Chuyển hướng người dùng về trang đăng nhập sau khi đăng ký thành công
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            // Nếu ModelState không hợp lệ (lỗi validation từ RegisterViewModel), hiển thị lại View với các lỗi
            return View(model);
        }

        // GET: Account/Login
        [AllowAnonymous]
        public IActionResult Login()
        {
            // Hiển thị thông báo thành công nếu có từ TempData
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
        public IActionResult Login(string username, string password)
        {
            // 1. Tìm người dùng theo tên đăng nhập (hoặc email)
            var user = _context.Users.FirstOrDefault(u => u.Username == username || u.Email == username);

            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View();
            }

            // 2. Xác minh mật khẩu
            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isPasswordCorrect)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View();
            }

            // 3. Đăng nhập thành công!
            // Thiết lập phiên đăng nhập (Sử dụng Session cho mục đích đơn giản hóa,
            // trong thực tế nên dùng ASP.NET Core Identity hoặc Claims-based Authentication)

            // Lưu User Id và Username vào Session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);

            // Lưu trạng thái Admin vào Session dưới dạng chuỗi "True" hoặc "False"
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());


            // 4. Chuyển hướng người dùng:
            // Cả admin và người dùng thường đều được chuyển hướng về trang chủ ("Home/Index")
            // Nút điều hướng đến Admin Panel sẽ được hiển thị trên trang chủ
            // nếu người dùng là Admin (được kiểm tra trong View bằng Session).

            if (user.IsAdmin)
            {
                TempData["AdminLoginMessage"] = $"Chào mừng Admin {user.Username}!";
            }
            else
            {
                TempData["UserLoginMessage"] = $"Chào mừng {user.Username}!";
            }

            // Chuyển hướng về trang chủ (Home/Index)
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Logout - Xử lý đăng xuất
        public IActionResult Logout()
        {
            // Xóa tất cả các biến Session
            HttpContext.Session.Clear();

            // Hiển thị thông báo đăng xuất thành công
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";

            // Chuyển hướng về trang đăng nhập
            return RedirectToAction("Login", "Account");
        }

        // GET: Account/ForgotPassword (Chỉ yêu cầu Email)
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
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
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                // Vẫn giữ thông báo chung chung để không tiết lộ email tồn tại hay không
                ModelState.AddModelError("", "Email không tồn tại trong hệ thống.");
                return View(model);
            }

            // Nếu email tồn tại, chuyển hướng đến trang ResetPassword
            // Chúng ta truyền email qua TempData để dùng ở trang ResetPassword
            TempData["UserEmailForReset"] = model.Email;
            return RedirectToAction(nameof(ResetPassword));
        }

        // GET: Account/ResetPassword (Nhận email từ TempData, hiển thị form đổi mật khẩu)
        [AllowAnonymous]
        public IActionResult ResetPassword()
        {
            // Lấy email từ TempData (do ForgotPassword POST chuyển hướng tới)
            string? userEmail = TempData["UserEmailForReset"] as string;

            if (string.IsNullOrEmpty(userEmail))
            {
                // Nếu không có email (người dùng truy cập trực tiếp), quay lại trang ForgotPassword
                TempData["ErrorMessage"] = "Vui lòng nhập email của bạn trước.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Tạo ViewModel với email đã nhận được
            var model = new ResetPasswordViewModel { Email = userEmail };
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
                // Nếu ModelState không hợp lệ, giữ nguyên email đã truyền
                // Để form hiển thị lại đúng email
                TempData["UserEmailForReset"] = model.Email; // Lưu lại để dùng nếu View bị hiển thị lại
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            // Kiểm tra lại người dùng có tồn tại không (phòng trường hợp người dùng chỉnh sửa form)
            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                return View(model);
            }

            // Hash mật khẩu mới và cập nhật vào DB
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mật khẩu của bạn đã được đặt lại thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
    }
}