using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using BookingToursWeb.Data; // Đảm bảo namespace này đúng

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Thêm cấu hình DbContext để kết nối với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// THÊM CẤU HÌNH SESSION VÀO ĐÂY
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian session hết hạn (ví dụ: 30 phút)
    options.Cookie.HttpOnly = true; // Cookie chỉ có thể truy cập bằng HTTP, không phải JavaScript
    options.Cookie.IsEssential = true; // Đánh dấu cookie session là thiết yếu
    options.Cookie.SameSite = SameSiteMode.Lax; // Có thể cần SameSiteMode.Lax hoặc None cho một số kịch bản cross-site
});

// Thêm dịch vụ cho HttpContextAccessor để có thể truy cập HttpContext từ các dịch vụ khác nếu cần
builder.Services.AddHttpContextAccessor(); // Rất hữu ích cho các helper methods

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// THỨ TỰ MIDDLEWARE QUAN TRỌNG NHẤT:
// Session phải được đặt sau UseRouting() để có thể đọc route data
// và trước MapControllerRoute để các controller có thể truy cập session.
app.UseSession();

// Không sử dụng UseAuthentication() và UseAuthorization()
// vì bạn đang quản lý xác thực/ủy quyền thủ công bằng Session.

// Cấu hình route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Điều chỉnh route mặc định đến trang Đăng nhập

app.Run();