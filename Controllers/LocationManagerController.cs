using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Cần thiết để truy cập Session
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookingToursWeb.Data;
using BookingToursWeb.Models;

public class LocationManagerController : Controller
{
    private readonly ApplicationDbContext _context;

    public LocationManagerController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Helper method để kiểm tra quyền LocationManager
    private bool IsCurrentUserLocationManager()
    {
        // Kiểm tra session để xác định quyền LocationManager
        return HttpContext.Session.GetString("IsLocationManager") == "True"; //
    }

    // Helper method để kiểm tra quyền Admin
    private bool IsCurrentUserAdmin()
    {
        // Kiểm tra session để xác định quyền Admin
        return HttpContext.Session.GetString("IsAdmin") == "True"; // Dựa trên thông tin Debugger (image_b15757.png)
    }

    // Helper method để lấy ManagedLocationId của LocationManager hiện tại
    private int? GetCurrentManagedLocationId()
    {
        // Sử dụng GetInt32 thay vì GetString và TryParse để an toàn hơn
        return HttpContext.Session.GetInt32("ManagedLocationId"); //
    }

    // GET: LocationManager/Index (Dashboard của Location Manager)
    // [Authorize(Roles = "LocationManager, Admin")] // Bạn có thể dùng thuộc tính này thay vì kiểm tra thủ công nếu dùng Identity
    public IActionResult Index()
    {
        // Kiểm tra quyền bằng helper method
        if (!IsCurrentUserLocationManager() && !IsCurrentUserAdmin()) //
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang quản lý địa điểm."; //
            return RedirectToAction("Login", "Account"); // Chuyển về trang đăng nhập nếu không có quyền
        }

        // Nếu là Admin, không cần ManagedLocationId cụ thể
        if (IsCurrentUserAdmin()) //
        {
            ViewData["Title"] = "Giao diện Quản lý Địa điểm";
            ViewBag.ManagedLocationName = "Admin"; // Hoặc một thông báo phù hợp cho Admin
            return View(); //
        }

        var managedLocationId = GetCurrentManagedLocationId(); //
        if (!managedLocationId.HasValue) //
        {
            TempData["ErrorMessage"] = "Tài khoản của bạn chưa được gán địa điểm quản lý. Vui lòng liên hệ quản trị viên."; //
            return RedirectToAction("Index", "Home"); // Chuyển về trang chủ
        }

        ViewData["Title"] = "Giao diện Quản lý Địa điểm"; //
        ViewBag.ManagedLocationName = HttpContext.Session.GetString("ManagedLocationName"); //
        return View(); // Trả về Views/LocationManager/Index.cshtml
    }

    // GET: LocationManager/ManageLocations
    public async Task<IActionResult> ManageLocations()
    {
        // Kiểm tra quyền LocationManager hoặc Admin
        if (!IsCurrentUserLocationManager() && !IsCurrentUserAdmin()) //
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này."; //
            return RedirectToAction("Login", "Account"); //
        }

        ViewData["Title"] = "Quản lý Địa điểm của tôi";
        IQueryable<Location> locationsQuery = _context.Locations;

        // Nếu là LocationManager và không phải Admin, chỉ cho phép xem địa điểm mà họ quản lý
        if (IsCurrentUserLocationManager() && !IsCurrentUserAdmin()) //
        {
            var managedLocationId = GetCurrentManagedLocationId(); //
            if (managedLocationId.HasValue) //
            {
                locationsQuery = locationsQuery.Where(l => l.Id == managedLocationId.Value); //
            }
            else
            {
                TempData["ErrorMessage"] = "Bạn là quản lý địa điểm nhưng chưa được gán địa điểm nào."; //
                return RedirectToAction("Index", "Home"); // Hoặc một trang lỗi khác
            }
        }
        // Admin sẽ thấy tất cả địa điểm, Location Manager chỉ thấy địa điểm của mình.
        var locations = await locationsQuery.ToListAsync(); //

        return View(locations); // Truyền danh sách địa điểm (có thể là 1 hoặc nhiều) vào View
    }

    // GET: LocationManager/EditLocation/5
    public async Task<IActionResult> EditLocation(int? id)
    {
        if (!IsCurrentUserLocationManager() && !IsCurrentUserAdmin())
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
            return RedirectToAction("Login", "Account");
        }

        if (id == null)
        {
            return NotFound();
        }

        var location = await _context.Locations.FindAsync(id);
        if (location == null)
        {
            return NotFound();
        }

        // Kiểm tra xem LocationManager có quyền chỉnh sửa địa điểm này không
        if (IsCurrentUserLocationManager() && !IsCurrentUserAdmin())
        {
            var managedLocationId = GetCurrentManagedLocationId();
            if (!managedLocationId.HasValue || location.Id != managedLocationId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa địa điểm này.";
                return RedirectToAction(nameof(ManageLocations));
            }
        }

        ViewData["Title"] = "Chỉnh sửa Địa điểm";
        return View(location);
    }

    // POST: LocationManager/EditLocation/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLocation(int id, [Bind("Id,Name,Description,Information,Address,TicketPrice,OpeningHours,ImageUrl,ContactInfo,Latitude,Longitude,BankAccountName,BankName,PaymentInstructions,IsActive")] Location location)
    {
        if (!IsCurrentUserLocationManager() && !IsCurrentUserAdmin())
        {
            TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này.";
            return RedirectToAction("Login", "Account");
        }

        if (id != location.Id)
        {
            return NotFound();
        }

        // Kiểm tra quyền chỉnh sửa tương tự như GET
        if (IsCurrentUserLocationManager() && !IsCurrentUserAdmin())
        {
            var managedLocationId = GetCurrentManagedLocationId();
            if (!managedLocationId.HasValue || location.Id != managedLocationId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa địa điểm này.";
                return RedirectToAction(nameof(ManageLocations));
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(location);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật địa điểm thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LocationExists(location.Id))
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
        ViewData["Title"] = "Chỉnh sửa Địa điểm";
        return View(location);
    }

    private bool LocationExists(int id)
    {
        return _context.Locations.Any(e => e.Id == id);
    }


    // GET: LocationManager/ManagePanoramas
    public async Task<IActionResult> ManagePanoramas()
    {
        if (!IsCurrentUserLocationManager() && !IsCurrentUserAdmin()) //
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này."; //
            return RedirectToAction("Login", "Account"); //
        }

        IQueryable<PanoramaPoint> panoramaQuery = _context.PanoramaPoints;

        if (IsCurrentUserLocationManager() && !IsCurrentUserAdmin()) //
        {
            var managedLocationId = GetCurrentManagedLocationId(); //
            if (!managedLocationId.HasValue) //
            {
                TempData["ErrorMessage"] = "Bạn chưa được gán địa điểm quản lý."; //
                return RedirectToAction("Index", "Home"); //
            }
            panoramaQuery = panoramaQuery.Where(p => p.LocationId == managedLocationId.Value); //
        }

        var panoramas = await panoramaQuery.ToListAsync(); //
        ViewData["Title"] = "Quản lý Panorama của tôi"; //
        return View(panoramas); //
    }

    // GET: LocationManager/ManageAppointments
    public async Task<IActionResult> ManageAppointments()
    {
        if (!IsCurrentUserLocationManager() && !IsCurrentUserAdmin()) //
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này."; //
            return RedirectToAction("Login", "Account"); //
        }

        IQueryable<Booking> bookingQuery = _context.Bookings
                                                  .Include(b => b.User)
                                                  .Include(b => b.Location);

        if (IsCurrentUserLocationManager() && !IsCurrentUserAdmin()) //
        {
            var managedLocationId = GetCurrentManagedLocationId(); //
            if (!managedLocationId.HasValue) //
            {
                TempData["ErrorMessage"] = "Bạn chưa được gán địa điểm quản lý."; //
                return RedirectToAction("Index", "Home"); //
            }
            bookingQuery = bookingQuery.Where(b => b.LocationId == managedLocationId.Value); //
        }

        var bookings = await bookingQuery.ToListAsync(); //
        ViewData["Title"] = "Quản lý Lịch hẹn của tôi"; //
        return View(bookings); //
    }

    // GET: LocationManager/ManageReviews
    public async Task<IActionResult> ManageReviews()
    {
        if (!IsCurrentUserLocationManager() && !IsCurrentUserAdmin()) //
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này."; //
            return RedirectToAction("Login", "Account"); //
        }

        IQueryable<Review> reviewQuery = _context.Reviews
                                                .Include(r => r.User)
                                                .Include(r => r.Location);

        if (IsCurrentUserLocationManager() && !IsCurrentUserAdmin()) //
        {
            var managedLocationId = GetCurrentManagedLocationId(); //
            if (!managedLocationId.HasValue) //
            {
                TempData["ErrorMessage"] = "Bạn chưa được gán địa điểm quản lý."; //
                return RedirectToAction("Index", "Home"); //
            }
            reviewQuery = reviewQuery.Where(r => r.LocationId == managedLocationId.Value); //
        }

        var reviews = await reviewQuery.ToListAsync(); //
        ViewData["Title"] = "Quản lý Đánh giá của tôi"; //
        return View(reviews); //
    }
}