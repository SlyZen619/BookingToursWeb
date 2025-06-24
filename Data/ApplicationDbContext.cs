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
                .HasOne(b => b.User)              // Booking có một User
                .WithMany(u => u.Bookings)        // User có nhiều Bookings (SỬ DỤNG Navigation Property mới thêm vào User Model)
                .HasForeignKey(b => b.UserId)     // Khóa ngoại trong Booking là UserId
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
                .HasOne(r => r.User)              // Review có một User
                .WithMany(u => u.Reviews)         // User có nhiều Reviews (SỬ DỤNG Navigation Property mới thêm vào User Model)
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
                .HasOne(p => p.Author)            // Post có một Author (là User)
                .WithMany(u => u.Posts)           // User có nhiều Posts (SỬ DỤNG Navigation Property mới thêm vào User Model)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}