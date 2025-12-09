using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TourBookingWeb.Models;
using Microsoft.AspNetCore.Identity;

namespace TourBookingWeb.Data
{
    public class TourDBContext : IdentityDbContext<IdentityUser>
    {
        public TourDBContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Tour> Tours { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<TourSchedule> TourSchedules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ràng buộc Booking → Tour (1 Tour có nhiều Booking)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Tour)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TourId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ràng buộc Booking → User (1 User có nhiều Booking)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ràng buộc Tour → Category (1 Category có nhiều Tour)
            modelBuilder.Entity<Category>()
                .HasMany<Tour>()
                .WithOne();

            // Ràng buộc Image → Tour (1 Tour có nhiều Image)
            modelBuilder.Entity<Image>()
                .HasOne(i => i.Tour)
                .WithMany(t => t.Images)
                .HasForeignKey(i => i.TourId)
                .OnDelete(DeleteBehavior.Cascade);

            // Thiết lập decimal cho Tour
            modelBuilder.Entity<Tour>()
                .Property(t => t.AdultPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Tour>()
                .Property(t => t.ChildPrice)
                .HasPrecision(18, 2);

            // Thiết lập decimal cho Booking
            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasPrecision(18, 2);

            // TourSchedule → Tour   (1 tour có nhiều lịch)
            modelBuilder.Entity<TourSchedule>()
                .HasOne(ts => ts.Tour)
                .WithMany(t => t.Schedules)
                .HasForeignKey(ts => ts.TourId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking → TourSchedule  (mỗi booking gắn 1 lịch)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.TourSchedule)
                .WithMany()           
                .HasForeignKey(b => b.TourScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
