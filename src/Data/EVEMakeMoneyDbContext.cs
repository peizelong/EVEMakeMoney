using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace EVEMakeMoney.Data
{
    public class EVEMakeMoneyDbContext : DbContext
    {
        public DbSet<MarketOrderEntity> MarketOrders { get; set; }

        public DbSet<MarketHistoryEntity> MarketHistory { get; set; }

        public DbSet<WeeklySalesEntity> WeeklySales { get; set; }

        public DbSet<BlueprintEntity> Blueprints { get; set; }

        private static string GetDbPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = new DirectoryInfo(baseDir);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "EVEMakeMoney.csproj")))
                {
                    return Path.Combine(dir.FullName, "EVEMakeMoney.db");
                }
                dir = dir.Parent;
            }
            return Path.Combine(baseDir, "EVEMakeMoney.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = GetDbPath();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MarketHistoryEntity>()
                .HasIndex(x => new { x.TypeId, x.RegionId, x.Date })
                .IsUnique();

            modelBuilder.Entity<WeeklySalesEntity>()
                .HasIndex(x => new { x.TypeId, x.RegionId, x.Year, x.WeekNumber })
                .IsUnique();
        }
    }
}
