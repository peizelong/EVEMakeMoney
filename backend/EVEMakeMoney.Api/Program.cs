using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using EVEMakeMoney.Api.Data;
using EVEMakeMoney.Api.Middleware;
using EVEMakeMoney.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<EVEMakeMoneyDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=EVEMakeMoney.db"));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "EVEMakeMoney_SecretKey_ForDevelopment_2024";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "EVEMakeMoney",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EVEMakeMoney",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddMemoryCache();
builder.Services.AddInMemoryRateLimiting();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Limit = 100,
            Period = "1m",
            QuotaExceededResponse = new QuotaExceededResponse
            {
                StatusCode = 429,
                Content = "{{ \"message\": \"请求过于频繁，请稍后再试\" }}"
            }
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/login",
            Limit = 10,
            Period = "1m"
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/register",
            Limit = 5,
            Period = "1h"
        }
    };
});
builder.Services.Configure<IpRateLimitPolicies>(options =>
{
    options.IpRules = new List<IpRateLimitPolicy>();
});
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddSingleton<TypeNameService>();
builder.Services.AddScoped<BlueprintService>();
builder.Services.AddScoped<CalculationCacheService>();
builder.Services.AddScoped<CostCalculationService>();
builder.Services.AddScoped<CostBreakdownService>();
builder.Services.AddScoped<MarketPopularityService>();
builder.Services.AddScoped<MarketDataService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EVESsoService>();
builder.Services.AddScoped<AssetService>();

builder.Services.AddScoped<BlueprintCacheService>();

builder.Services.Configure<MarketDataSyncOptions>(builder.Configuration.GetSection("MarketDataSync"));
builder.Services.AddHostedService<MarketDataSyncBackgroundService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:3000", "http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EVEMakeMoneyDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseIpRateLimiting();

app.UseExceptionHandling();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
