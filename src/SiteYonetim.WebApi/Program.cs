using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SiteYonetim.Domain;
using SiteYonetim.Infrastructure;
using SiteYonetim.Infrastructure.Data;
using SiteYonetim.WebApi.Filters;

var builder = WebApplication.CreateBuilder(args);

// MVC + API
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AppAreaAuthorizationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Site Yönetim API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token. Örnek: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Çift kimlik: Web sitesi Cookie, API JWT
var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
builder.Services.Configure<JwtSettings>(jwtSection);
var jwtSecret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret appsettings'te tanımlanmalı.");
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Upload klasörleri: uploads/{modul}/{siteId}/ — Teklifler, Gider faturaları, Destek, Evraklar
var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
foreach (var folder in new[] { "uploads", "uploads/teklifler", "uploads/destek", "uploads/giderler", "uploads/evraklar" })
{
    try { Directory.CreateDirectory(Path.Combine(webRoot, folder)); } catch { /* yoksa devam */ }
}

// Geliştirme ortamında veritabanı yoksa oluştur (LocalDB / SQL Server)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connStr = config.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connStr))
        {
            try
            {
                var builderCs = new SqlConnectionStringBuilder(connStr);
                var databaseName = builderCs.InitialCatalog;
                if (!string.IsNullOrEmpty(databaseName))
                {
                    builderCs.InitialCatalog = "master";
                    using var conn = new SqlConnection(builderCs.ConnectionString);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'{databaseName.Replace("'", "''")}') CREATE DATABASE [{databaseName.Replace("]", "]]")}]";
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(ex, "Veritabanı oluşturma atlandı. SQL script'leri ile manuel oluşturabilirsiniz.");
            }
        }

        var db = scope.ServiceProvider.GetRequiredService<SiteYonetimDbContext>();
        try
        {
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "EnsureCreated atlandı.");
        }
    }
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// API (JWT) - /api/* 
app.MapControllers();

// Web sitesi (Cookie) - /App/ olmadan App area erişimi (örn: /Dashboard, /Sites)
app.MapControllerRoute(
    name: "app_default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "App" });
// Eski /App/ URL'leri için geriye dönük uyumluluk
app.MapControllerRoute(
    name: "areas",
    pattern: "App/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "App" });
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
