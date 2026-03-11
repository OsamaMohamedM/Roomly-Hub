using Domain.Entities;
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
                        .Property("IsDeleted")
                        .HasDefaultValue(false);
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("CreatedAt")
                        .HasDefaultValueSql("NOW()");
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("UpdatedAt")
                        .HasDefaultValueSql("NOW()");
                    modelBuilder.Entity(entityType.ClrType).HasKey("Id");
                }
            }
        }

        public DbSet<User> Users { get; set; }
        public DbSet<KycSubmission> kycSubmissions { get; set; }

        public DbSet<Otp> Otps { get; set; }
    }
}