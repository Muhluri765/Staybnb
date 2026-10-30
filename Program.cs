using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Staybnb.Data;
using Staybnb.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Database Connection (SQL Server on Windows / production, SQLite on macOS / cross-platform development)
var configuredProvider = builder.Configuration.GetValue<string>("DatabaseProvider");
bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
string selectedProvider = !string.IsNullOrEmpty(configuredProvider) 
    ? configuredProvider 
    : (isWindows ? "SqlServer" : "Sqlite");

if (selectedProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    var sqlConn = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=(localdb)\\mssqllocaldb;Database=StaybnbDb;Trusted_Connection=True;MultipleActiveResultSets=true";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(sqlConn));
}
else
{
    var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection") 
        ?? "Data Source=staybnb.db";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(sqliteConn));
}

// 2. Configure ASP.NET Core Identity with ApplicationUser & IdentityRole
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Authentication Cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// 3. Add MVC Controllers and Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Ensure Local Uploads Directories Exist
var webHostEnv = app.Services.GetRequiredService<IWebHostEnvironment>();
var propertiesUploadDir = Path.Combine(webHostEnv.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "properties");
var documentsUploadDir = Path.Combine(webHostEnv.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "documents");
Directory.CreateDirectory(propertiesUploadDir);
Directory.CreateDirectory(documentsUploadDir);

// 5. Seed Roles, SuperAdmin, Amenities, and Sample Listings on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await DbInitializer.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database on startup.");
    }
}

// 6. Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
