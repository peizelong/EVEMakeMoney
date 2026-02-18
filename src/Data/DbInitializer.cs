using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EVEMakeMoney.Data
{
    public static class DbInitializer
    {
        public static void Initialize()
        {
            using var db = new EVEMakeMoneyDbContext();
            db.Database.EnsureCreated();

            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Blueprints (
                    BlueprintTypeId INTEGER PRIMARY KEY,
                    ME INTEGER NOT NULL DEFAULT 0,
                    TE INTEGER NOT NULL DEFAULT 0
                )
            ");
        }

        public static async Task InitializeAsync()
        {
            using var db = new EVEMakeMoneyDbContext();
            await db.Database.EnsureCreatedAsync();

            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS Blueprints (
                    BlueprintTypeId INTEGER PRIMARY KEY,
                    ME INTEGER NOT NULL DEFAULT 0,
                    TE INTEGER NOT NULL DEFAULT 0
                )
            ");
        }
    }
}
