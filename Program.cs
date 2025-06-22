using Microsoft.EntityFrameworkCore; // Thêm namespace cho Entity Framework Core
using Microsoft.AspNetCore.Builder; // Cần thiết cho WebApplicationBuilder và WebApplication
using Microsoft.Extensions.DependencyInjection; // Cần thiết cho AddSession và các dịch vụ khác
using Microsoft.AspNetCore.Authentication.Cookies; // Thêm dòng này để sử dụng Cookie Authentication
using System;
using BookingToursWeb.Data; // Cần cho TimeSpan

var builder = WebApplication.CreateBuilder(args);

// Thêm các dịch vụ vào container.
builder.Services.AddControllersWithViews();

// Thêm cấu hình DbContext để kết nối với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// THÊM CẤU HÌNH AUTHENTICATION VÀ AUTHORIZATION VÀO ĐÂY
// Cấu hình Cookie Authentication làm lược đồ mặc định
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Đường dẫn đến trang đăng nhập nếu chưa xác thực
        options.AccessDeniedPath = "/Account/AccessDenied"; // Đường dẫn nếu người dùng không có quyền truy cập
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Thời gian hết hạn của cookie xác thực
        options.SlidingExpiration = true; // Làm mới thời gian hết hạn nếu người dùng hoạt động
    });

// THÊM CẤU HÌNH AUTHORIZATION (rất quan trọng để [Authorize] hoạt động)
builder.Services.AddAuthorization();

// THÊM CẤU HÌNH SESSION VÀO ĐÂY (Bạn đã có, tôi giữ nguyên)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian session hết hạn (ví dụ: 30 phút)
    options.Cookie.HttpOnly = true; // Cookie chỉ có thể truy cập bằng HTTP, không phải JavaScript
    options.Cookie.IsEssential = true; // Đánh dấu cookie session là thiết yếu để tuân thủ GDPR
});

var app = builder.Build();

// Cấu hình HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Giá trị HSTS mặc định là 30 ngày. Bạn có thể muốn thay đổi điều này cho các tình huống production, xem https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection(); // Chuyển hướng HTTP sang HTTPS
app.UseStaticFiles();      // Cho phép phục vụ các file tĩnh (CSS, JS, hình ảnh)

app.UseRouting();          // Định tuyến các yêu cầu HTTP đến các endpoint phù hợp

// THỨ TỰ MIDDLEWARE QUAN TRỌNG:
// 1. UseSession() phải trước UseAuthentication() nếu bạn dùng session cho xác thực hoặc thông tin người dùng.
app.UseSession();

// 2. UseAuthentication() phải trước UseAuthorization()
app.UseAuthentication();   // Kích hoạt middleware xác thực (Đọc cookie xác thực, tạo User Identity)

// 3. UseAuthorization() phải sau UseAuthentication()
app.UseAuthorization();    // Kích hoạt middleware ủy quyền (Kiểm tra quyền truy cập dựa trên Authorize attribute)


// Cấu hình route mặc định
// Chúng ta muốn cả admin và người dùng thường đều được đưa đến trang chủ sau khi đăng nhập,
// và trang chủ sẽ có logic hiển thị nút Admin nếu là admin.
// Do đó, chúng ta giữ route mặc định là Account/Login để bắt đầu.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Điều chỉnh route mặc định đến trang Đăng nhập

app.Run();