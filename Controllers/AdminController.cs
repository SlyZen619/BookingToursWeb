using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Cần thiết cho các phương thức của EF Core như ToListAsync, FindAsync
using BookingToursWeb.Data;
using BookingToursWeb.Models;


namespace BookingToursWeb.Controllers
{
    // Lưu ý: Nếu bạn muốn tất cả các action trong AdminController yêu cầu đăng nhập
    // và muốn sử dụng hệ thống Authorize tích hợp của ASP.NET Core,
    // bạn sẽ cần thêm [Authorize] attribute ở đây và cấu hình Authentication trong Program.cs.
    // Tuy nhiên, dựa trên code hiện tại của bạn sử dụng HttpContext.Session.GetString("IsAdmin"),
    // thì việc kiểm tra thủ công trong mỗi action là phù hợp với cách bạn đang làm.
    // Nếu bạn thêm [Authorize] ở đây, bạn cần đảm bảo Program.cs đã được cập nhật với AddAuthentication/UseAuthentication.
    // [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context; // Khai báo biến DbContext

        // Constructor: Sử dụng Dependency Injection để tiêm ApplicationDbContext vào Controller
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Phương thức helper để kiểm tra quyền Admin
        private bool IsCurrentUserAdmin()
        {
            // Kiểm tra session để biết người dùng hiện tại có phải là Admin không.
            // Điều này cần được thiết lập trong quá trình đăng nhập của Admin.
            // Ví dụ: trong action Login POST, sau khi xác thực thành công admin, bạn set:
            // HttpContext.Session.SetString("IsAdmin", "True");
            return HttpContext.Session.GetString("IsAdmin") == "True";
        }

        // GET: Admin/Index (Trang Dashboard chính của Admin)
        public IActionResult Index()
        {
            // Kiểm tra quyền Admin
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang Admin Dashboard.";
                return RedirectToAction("Index", "Home"); // Chuyển hướng về trang chủ
            }
            ViewData["Title"] = "Admin Dashboard";
            return View();
        }

        // GET: Admin/ManageUsers (Trang danh sách người dùng cho Admin)
        // Đây sẽ là trang mà nút "Thêm người dùng mới" sẽ nằm.
        public async Task<IActionResult> ManageUsers()
        {
            // Kiểm tra quyền Admin
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang quản lý người dùng.";
                return RedirectToAction("Index", "Home"); // Chuyển hướng nếu không có quyền
            }

            ViewData["Title"] = "Quản lý Tài khoản người dùng";
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: Admin/AddUser
        // Hiển thị form để Admin thêm người dùng mới
        [HttpGet]
        public IActionResult AddUser()
        {
            // Kiểm tra quyền Admin
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thêm người dùng mới.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Thêm người dùng mới";
            return View();
        }

        // POST: Admin/AddUser
        // Xử lý việc thêm người dùng mới bởi Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(AdminAddUserViewModel model)
        {
            // Kiểm tra quyền Admin
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                // 1. Kiểm tra tên đăng nhập đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                    ViewData["Title"] = "Thêm người dùng mới"; // Giữ lại title
                    return View(model);
                }

                // 2. Kiểm tra email đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một tài khoản khác.");
                    ViewData["Title"] = "Thêm người dùng mới"; // Giữ lại title
                    return View(model);
                }

                // 3. Hash mật khẩu trước khi lưu vào database
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // 4. Tạo đối tượng User mới từ ViewModel
                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PasswordHash = hashedPassword,
                    IsAdmin = model.IsAdmin // Đặt quyền Admin dựa trên input từ form
                };

                // 5. Thêm người dùng vào DbContext và lưu vào cơ sở dữ liệu
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm người dùng mới thành công!";
                // Chuyển hướng về trang danh sách người dùng Admin
                return RedirectToAction(nameof(ManageUsers));
            }

            // Nếu ModelState không hợp lệ, hiển thị lại form với các lỗi
            ViewData["Title"] = "Thêm người dùng mới"; // Giữ lại title
            return View(model);
        }

        // GET: Admin/ManageAppointments (Quản lý Lịch hẹn)
        public IActionResult ManageAppointments()
        {
            // Kiểm tra quyền Admin
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Quản lý Lịch hẹn";
            return View();
        }

        // GET: Admin/ManageMenus (Quản lý Menu)
        public IActionResult ManageMenus()
        {
            // Kiểm tra quyền Admin
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Quản lý Menu";
            return View();
        }

        // GET: Admin/ManagePosts (Quản lý Bài đăng)
        public IActionResult ManagePosts()
        {
            // Kiểm tra quyền Admin
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "Quản lý Bài đăng";
            return View();
        }

        // ----------------------------------------------------
        // CHỨC NĂNG SỬA THÔNG TIN NGƯỜI DÙNG

        // GET: Admin/EditUser/{id} - Hiển thị form sửa thông tin người dùng
        // Phương thức này được gọi khi người dùng nhấp vào nút "Sửa" trên bảng danh sách người dùng
        [HttpGet] // Khuyến nghị rõ ràng là GET
        public async Task<IActionResult> EditUser(int? id) // 'int?' cho phép id có thể null
        {
            // Kiểm tra quyền Admin
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa người dùng.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = "Sửa thông tin người dùng";

            if (id == null) // Nếu không có ID được cung cấp, trả về lỗi
            {
                return NotFound(); // Trả về HTTP 404 Not Found
            }

            // Tìm người dùng trong database bằng ID
            var user = await _context.Users.FindAsync(id);

            if (user == null) // Nếu không tìm thấy người dùng, trả về lỗi
            {
                return NotFound(); // Trả về HTTP 404 Not Found
            }

            // Truyền đối tượng người dùng tìm được vào View để điền sẵn dữ liệu vào form
            return View(user);
        }

        // POST: Admin/EditUser/{id} - Xử lý lưu thông tin người dùng đã sửa
        // Phương thức này được gọi khi người dùng submit form chỉnh sửa
        [HttpPost]
        [ValidateAntiForgeryToken] // Bảo vệ khỏi tấn công CSRF
        // [Bind(...)] rất quan trọng để chỉ cho phép binding các thuộc tính được phép sửa, tránh over-posting
        public async Task<IActionResult> EditUser(int id, [Bind("Id,Username,Email,PhoneNumber,IsAdmin")] User user)
        {
            // Kiểm tra quyền Admin
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa người dùng.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Title"] = "Sửa thông tin người dùng";

            // THÊM DÒNG NÀY: Loại bỏ PasswordHash khỏi quá trình validation của ModelState
            // Vì trường PasswordHash không được gửi từ form EditUser và ta không muốn validate nó trong trường hợp này.
            ModelState.Remove("PasswordHash");

            // Kiểm tra xem ID trong URL có khớp với ID của đối tượng user gửi lên từ form không
            if (id != user.Id)
            {
                return NotFound();
            }

            // Kiểm tra tính hợp lệ của dữ liệu Model (bây giờ không còn kiểm tra PasswordHash)
            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy đối tượng người dùng hiện tại từ database trước khi cập nhật.
                    // Điều này đảm bảo các trường không được gửi qua form (như PasswordHash)
                    // không bị ghi đè thành null hoặc giá trị mặc định.
                    var userToUpdate = await _context.Users.FindAsync(id);
                    if (userToUpdate == null)
                    {
                        return NotFound(); // Người dùng có thể đã bị xóa bởi người khác
                    }

                    // Cập nhật các thuộc tính của đối tượng người dùng từ database
                    // bằng các giá trị mới từ form (đối tượng 'user' được binding)
                    userToUpdate.Username = user.Username;
                    userToUpdate.Email = user.Email;
                    userToUpdate.PhoneNumber = user.PhoneNumber;
                    userToUpdate.IsAdmin = user.IsAdmin;

                    // Kiểm tra trùng lặp Username và Email với các người dùng KHÁC
                    // Đảm bảo người dùng đang sửa không bị coi là trùng với chính mình
                    if (_context.Users.Any(u => u.Username == userToUpdate.Username && u.Id != userToUpdate.Id))
                    {
                        ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                        return View(user); // Hiển thị lại form với lỗi
                    }

                    if (_context.Users.Any(u => u.Email == userToUpdate.Email && u.Id != userToUpdate.Id))
                    {
                        ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                        return View(user); // Hiển thị lại form với lỗi
                    }

                    // Đánh dấu đối tượng userToUpdate là cần được cập nhật trong DbContext
                    _context.Update(userToUpdate);
                    // Lưu các thay đổi vào cơ sở dữ liệu một cách bất đồng bộ
                    await _context.SaveChangesAsync();

                    // Sử dụng TempData để truyền thông báo thành công sau khi chuyển hướng
                    TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Xử lý trường hợp lỗi đồng thời (ví dụ: hai người cùng sửa một lúc)
                    if (!_context.Users.Any(e => e.Id == user.Id))
                    {
                        return NotFound(); // Người dùng không còn tồn tại
                    }
                    else
                    {
                        throw; // Ném lỗi để hệ thống xử lý
                    }
                }
                // Sau khi cập nhật thành công, chuyển hướng về trang danh sách người dùng
                return RedirectToAction(nameof(ManageUsers));
            }

            // Nếu ModelState không hợp lệ (có lỗi validation khác ngoài PasswordHash), hiển thị lại form với các lỗi
            return View(user);
        }

        // ----------------------------------------------------
        // CHỨC NĂNG XÓA NGƯỜI DÙNG

        // POST: Admin/DeleteUser/{id} - Xử lý xóa người dùng
        // ActionName("DeleteUser") ánh xạ với asp-action="DeleteUser" trong form POST của nút Xóa
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Kiểm tra quyền Admin
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa người dùng.";
                return RedirectToAction("Index", "Home");
            }

            // Tìm người dùng cần xóa
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng để xóa.";
                return NotFound();
            }

            // Ngăn không cho Admin tự xóa tài khoản của chính mình (dùng Session)
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId.HasValue && currentUserId.Value == user.Id)
            {
                TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Xóa người dùng khỏi DbContext
            _context.Users.Remove(user);
            // Lưu thay đổi vào database
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Người dùng đã được xóa thành công.";
            // Chuyển hướng về trang quản lý người dùng
            return RedirectToAction(nameof(ManageUsers));
        }
    }
}