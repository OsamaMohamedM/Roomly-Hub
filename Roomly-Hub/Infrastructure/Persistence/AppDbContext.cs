using Domain.Entities;
using Domain.Entities.Booking;
using Domain.Entities.Rooms;
using Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("CreatedAt")
                        .HasDefaultValueSql("NOW()");

                    modelBuilder.Entity(entityType.ClrType).HasKey("Id");
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("Id")
                        .ValueGeneratedNever();
                }
            }
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Otp> Otps { get; set; }
        public DbSet<KycSubmission> KycSubmissions { get; set; }
        public DbSet<UserExternalLogin> UserExternalLogins { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomPhoto> RoomPhotos { get; set; }
        public DbSet<RoomAvailability> RoomAvailabilities { get; set; }
        public DbSet<RoomReview> RoomReviews { get; set; }
        public DbSet<AmenityType> Amenities { get; set; }
    }
}