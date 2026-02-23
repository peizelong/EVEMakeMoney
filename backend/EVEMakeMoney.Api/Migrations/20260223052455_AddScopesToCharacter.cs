using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVEMakeMoney.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScopesToCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Blueprints",
                columns: table => new
                {
                    BlueprintTypeId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ME = table.Column<int>(type: "INTEGER", nullable: false),
                    TE = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blueprints", x => x.BlueprintTypeId);
                });

            migrationBuilder.CreateTable(
                name: "CalculationHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    BlueprintTypeId = table.Column<long>(type: "INTEGER", nullable: false),
                    ME = table.Column<int>(type: "INTEGER", nullable: false),
                    TE = table.Column<int>(type: "INTEGER", nullable: false),
                    StructureBonus = table.Column<decimal>(type: "TEXT", nullable: false),
                    RigBonus = table.Column<decimal>(type: "TEXT", nullable: false),
                    IndustryLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    AdvancedIndustryLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ReactionStructureBonus = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReactionRigBonus = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReactionLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ManufacturingCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    ManufacturingTime = table.Column<decimal>(type: "TEXT", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeId = table.Column<long>(type: "INTEGER", nullable: false),
                    RegionId = table.Column<long>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Average = table.Column<double>(type: "REAL", nullable: false),
                    Highest = table.Column<double>(type: "REAL", nullable: false),
                    Lowest = table.Column<double>(type: "REAL", nullable: false),
                    OrderCount = table.Column<long>(type: "INTEGER", nullable: false),
                    Volume = table.Column<long>(type: "INTEGER", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketOrders",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeId = table.Column<long>(type: "INTEGER", nullable: false),
                    RegionId = table.Column<long>(type: "INTEGER", nullable: false),
                    SystemId = table.Column<long>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<long>(type: "INTEGER", nullable: false),
                    IsBuyOrder = table.Column<bool>(type: "INTEGER", nullable: false),
                    Price = table.Column<double>(type: "REAL", nullable: false),
                    VolumeRemain = table.Column<long>(type: "INTEGER", nullable: false),
                    VolumeTotal = table.Column<long>(type: "INTEGER", nullable: false),
                    Duration = table.Column<long>(type: "INTEGER", nullable: false),
                    MinVolume = table.Column<long>(type: "INTEGER", nullable: false),
                    Range = table.Column<string>(type: "TEXT", nullable: true),
                    Issued = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketOrders", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "UserBlueprintSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    BlueprintTypeId = table.Column<long>(type: "INTEGER", nullable: false),
                    ME = table.Column<int>(type: "INTEGER", nullable: false),
                    TE = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlueprintSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklySales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeId = table.Column<long>(type: "INTEGER", nullable: false),
                    RegionId = table.Column<long>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    WeekNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalVolume = table.Column<long>(type: "INTEGER", nullable: false),
                    AveragePrice = table.Column<double>(type: "REAL", nullable: false),
                    HighestPrice = table.Column<double>(type: "REAL", nullable: false),
                    LowestPrice = table.Column<double>(type: "REAL", nullable: false),
                    OrderCount = table.Column<long>(type: "INTEGER", nullable: false),
                    WeekStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WeekEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklySales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<long>(type: "INTEGER", nullable: false),
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorporationName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CorporationId = table.Column<long>(type: "INTEGER", nullable: true),
                    AllianceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AllianceId = table.Column<long>(type: "INTEGER", nullable: true),
                    AccessToken = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RefreshToken = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CharacterOwnerHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Scopes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CharacterId",
                table: "Characters",
                column: "CharacterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_CharacterId",
                table: "Characters",
                columns: new[] { "UserId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketHistory_TypeId_RegionId_Date",
                table: "MarketHistory",
                columns: new[] { "TypeId", "RegionId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBlueprintSettings_UserId_BlueprintTypeId",
                table: "UserBlueprintSettings",
                columns: new[] { "UserId", "BlueprintTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklySales_TypeId_RegionId_Year_WeekNumber",
                table: "WeeklySales",
                columns: new[] { "TypeId", "RegionId", "Year", "WeekNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Blueprints");

            migrationBuilder.DropTable(
                name: "CalculationHistory");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "MarketHistory");

            migrationBuilder.DropTable(
                name: "MarketOrders");

            migrationBuilder.DropTable(
                name: "UserBlueprintSettings");

            migrationBuilder.DropTable(
                name: "WeeklySales");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
