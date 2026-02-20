using Microsoft.EntityFrameworkCore;

namespace EVEMakeMoney.Api.Data
{
    public class EVEMakeMoneyDbContext : DbContext
    {
        public DbSet<MarketOrderEntity> MarketOrders { get; set; }
        public DbSet<MarketHistoryEntity> MarketHistory { get; set; }
        public DbSet<WeeklySalesEntity> WeeklySales { get; set; }
        public DbSet<BlueprintEntity> Blueprints { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<UserBlueprintSettingEntity> UserBlueprintSettings { get; set; }
        public DbSet<CalculationHistoryEntity> CalculationHistory { get; set; }

        public EVEMakeMoneyDbContext(DbContextOptions<EVEMakeMoneyDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MarketHistoryEntity>()
                .HasIndex(x => new { x.TypeId, x.RegionId, x.Date })
                .IsUnique();

            modelBuilder.Entity<WeeklySalesEntity>()
                .HasIndex(x => new { x.TypeId, x.RegionId, x.Year, x.WeekNumber })
                .IsUnique();

            modelBuilder.Entity<UserEntity>()
                .HasIndex(x => x.Username)
                .IsUnique();

            modelBuilder.Entity<UserEntity>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<UserBlueprintSettingEntity>()
                .HasIndex(x => new { x.UserId, x.BlueprintTypeId })
                .IsUnique();
        }
    }
}
