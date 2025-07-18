using Microsoft.EntityFrameworkCore;
using BookingToursWeb.Models; // Đảm bảo namespace này đã được import để sử dụng các Models

namespace BookingToursWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets cho TẤT CẢ các bảng trong database của bạn
        public DbSet<User> Users { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PanoramaPoint> PanoramaPoints { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình UNIQUE index cho Username và Email của bảng Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Mối quan hệ Booking - User (Many-to-One: Bookings to User)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mối quan hệ Booking - Location (Many-to-One: Bookings to Location)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Location)
                .WithMany(l => l.Bookings)
                .HasForeignKey(b => b.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mối quan hệ Review - User (Many-to-One: Reviews to User)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mối quan hệ Review - Location (Many-to-One: Reviews to Location)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Location)
                .WithMany(l => l.Reviews)
                .HasForeignKey(r => r.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mối quan hệ Post - User (Author) (Many-to-One: Posts to User)
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ Post - Category
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // === CẤU HÌNH MỐI QUAN HỆ USER - LOCATION (CHO QUẢN LÝ ĐỊA ĐIỂM) ===
            // Một User có thể quản lý một Location (thông qua ManagedLocationId)
            modelBuilder.Entity<User>()
                .HasOne(u => u.ManagedLocation)
                .WithMany(l => l.LocationManagers) // Thêm thuộc tính điều hướng ngược lại trong Location
                .HasForeignKey(u => u.ManagedLocationId)
                .IsRequired(false) // ManagedLocationId có thể NULL
                .OnDelete(DeleteBehavior.Restrict); // Tránh xóa cascade

            base.OnModelCreating(modelBuilder);
        }
    }
}