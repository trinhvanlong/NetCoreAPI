using DemoMVC.Data;
using DemoMVC.Models;
using DemoMVC.Services.Email;
using DemoMVC.Services.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredUniqueChars = 1;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddMemoryCache();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly",
        policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("AdminManagement",
        policy => policy.RequireRole("SuperAdmin", "Admin"));
    options.AddPolicy("SalesManagement",
        policy => policy.RequireRole("SuperAdmin", "Admin", "Sales"));
    options.AddPolicy("WarehouseManagement",
        policy => policy.RequireRole("SuperAdmin", "Admin", "Warehouse"));
    options.AddPolicy("CustomerAccount",
        policy => policy.RequireAuthenticatedUser());
});

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IIdentitySeeder, IdentitySeeder>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup");
    var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        logger.LogError("ConnectionStrings:DefaultConnection is not configured.");
    }

    var emailSettings = app.Configuration
        .GetSection(EmailSettings.SectionName)
        .Get<EmailSettings>();
    if (emailSettings is null ||
        string.IsNullOrWhiteSpace(emailSettings.Host) ||
        string.IsNullOrWhiteSpace(emailSettings.Username) ||
        string.IsNullOrWhiteSpace(emailSettings.Password) ||
        string.IsNullOrWhiteSpace(emailSettings.SenderEmail))
    {
        logger.LogWarning("SMTP is not fully configured. Email-dependent features will remain unavailable.");
    }

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IIdentitySeeder>();
        try
        {
            await seeder.SeedAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Không thể khởi tạo dữ liệu Identity. Ứng dụng tiếp tục chạy.");
        }
    }
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception)
    {
        if (!string.Equals(
                context.Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase))
        {
            throw;
        }

        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("AjaxExceptionHandler");
        logger.LogError(exception, "An AJAX request failed.");

        var databaseUnavailable =
            exception is SqlException ||
            exception is DbUpdateException { InnerException: SqlException };
        context.Response.Clear();
        context.Response.StatusCode = databaseUnavailable
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = databaseUnavailable
                ? "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau."
                : "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại."
        });
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
