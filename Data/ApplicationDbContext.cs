using Microsoft.EntityFrameworkCore;
using BookingToursWeb.Models;

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
        public DbSet<PanoramaView> PanoramaViews { get; set; } // THÊM DÒNG NÀY

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình UNIQUE index cho Username và Email của bảng Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // =====================================================================
            // Cấu hình các mối quan hệ tường minh (CẬP NHẬT LẠI ĐÂY)
            // =====================================================================

            // Mối quan hệ Booking - User (Many-to-One: Bookings to User)
            // Một Booking thuộc về một User. Một User có nhiều Bookings.
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
            // Một Review thuộc về một User. Một User có nhiều Reviews.
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
            // Một Post có một Author (là User). Một User có nhiều Posts.
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ cho PanoramaView: một Location có nhiều PanoramaViews
            modelBuilder.Entity<PanoramaView>()
                .HasOne(pv => pv.Location)           // Một PanoramaView có một Location
                .WithMany(l => l.PanoramaViews)      // Một Location có nhiều PanoramaViews (sử dụng thuộc tính PanoramaViews trong Location Model)
                .HasForeignKey(pv => pv.LocationId)  // Khóa ngoại trong PanoramaView là LocationId
                .OnDelete(DeleteBehavior.Cascade);   // Khi Location bị xóa, các PanoramaView liên quan cũng bị xóa

            base.OnModelCreating(modelBuilder);
        }
    }
}