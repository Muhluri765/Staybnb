using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Staybnb.Models;

namespace Staybnb.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<HostProperty> HostProperties => Set<HostProperty>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Amenity> Amenities => Set<Amenity>();
        public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<HostApplication> HostApplications => Set<HostApplication>();
        public DbSet<CheckInProcess> CheckInProcesses => Set<CheckInProcess>();
        public DbSet<GuestCheckIn> GuestCheckIns => Set<GuestCheckIn>();
        public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // HostProperty Configuration
            builder.Entity<HostProperty>(entity =>
            {
                entity.Property(p => p.PricePerNight).HasColumnType("decimal(18,2)");
                entity.HasOne(p => p.Host)
                      .WithMany(u => u.HostProperties)
                      .HasForeignKey(p => p.HostId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(p => p.Amenities)
                      .WithMany(a => a.Properties)
                      .UsingEntity(j => j.ToTable("PropertyAmenities"));
            });

            // Booking Configuration
            builder.Entity<Booking>(entity =>
            {
                entity.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(b => b.Property)
                      .WithMany(p => p.Bookings)
                      .HasForeignKey(b => b.PropertyId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Guest)
                      .WithMany(u => u.Bookings)
                      .HasForeignKey(b => b.GuestId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Payment Configuration (1:1 with Booking)
            builder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                entity.HasOne(p => p.Booking)
                      .WithOne(b => b.Payment)
                      .HasForeignKey<Payment>(p => p.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Review Configuration
            builder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.Property)
                      .WithMany(p => p.Reviews)
                      .HasForeignKey(r => r.PropertyId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Reviewer)
                      .WithMany(u => u.Reviews)
                      .HasForeignKey(r => r.ReviewerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // WishlistItem Configuration
            builder.Entity<WishlistItem>(entity =>
            {
                entity.HasOne(w => w.User)
                      .WithMany(u => u.WishlistItems)
                      .HasForeignKey(w => w.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(w => w.Property)
                      .WithMany(p => p.WishlistItems)
                      .HasForeignKey(w => w.PropertyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Message Configuration (Sender and Receiver both ApplicationUser)
            builder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Sender)
                      .WithMany(u => u.SentMessages)
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Receiver)
                      .WithMany(u => u.ReceivedMessages)
                      .HasForeignKey(m => m.ReceiverId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ActivityLog Configuration
            builder.Entity<ActivityLog>(entity =>
            {
                entity.HasOne(a => a.User)
                      .WithMany(u => u.ActivityLogs)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Notification Configuration
            builder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // HostApplication Configuration
            builder.Entity<HostApplication>(entity =>
            {
                entity.HasOne(h => h.ApplicationUser)
                      .WithMany(u => u.HostApplications)
                      .HasForeignKey(h => h.ApplicationUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.Property)
                      .WithMany(p => p.HostApplications)
                      .HasForeignKey(h => h.PropertyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // CheckInProcess Configuration (1:1 with HostProperty)
            builder.Entity<CheckInProcess>(entity =>
            {
                entity.HasOne(c => c.Property)
                      .WithOne(p => p.CheckInProcess)
                      .HasForeignKey<CheckInProcess>(c => c.PropertyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // GuestCheckIn Configuration (1:1 with Booking)
            builder.Entity<GuestCheckIn>(entity =>
            {
                entity.HasOne(g => g.Booking)
                      .WithOne(b => b.GuestCheckIn)
                      .HasForeignKey<GuestCheckIn>(g => g.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(g => g.CheckInProcess)
                      .WithMany(cp => cp.GuestCheckIns)
                      .HasForeignKey(g => g.CheckInProcessId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // GuestDocument Configuration
            builder.Entity<GuestDocument>(entity =>
            {
                entity.HasOne(d => d.GuestCheckIn)
                      .WithMany(g => g.GuestDocuments)
                      .HasForeignKey(d => d.GuestCheckInId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
