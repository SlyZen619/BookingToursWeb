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

        public DbSet<User> Users { get; set; } // DbSet đại diện cho bảng Users

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình UNIQUE index cho Username và Email thông qua Fluent API
            // (Đảm bảo tính duy nhất ở cấp độ CSDL)
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}