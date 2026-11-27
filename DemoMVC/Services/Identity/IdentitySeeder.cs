using DemoMVC.Models;
using Microsoft.AspNetCore.Identity;

namespace DemoMVC.Services.Identity;

public sealed class IdentitySeeder : IIdentitySeeder
{
    private static readonly string[] DefaultRoles =
    [
        "SuperAdmin",
        "Admin",
        "Sales",
        "Warehouse",
        "Customer"
    ];

    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<IdentitySeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in DefaultRoles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (createRoleResult.Succeeded)
                {
                    _logger.LogInformation("Đã tạo role {RoleName}.", roleName);
                }
                else
                {
                    _logger.LogError("Không thể tạo role {RoleName}: {Errors}",
                        roleName, string.Join("; ", createRoleResult.Errors.Select(error => error.Description)));
                }
            }
        }

        var adminPassword = _configuration["ADMIN_DEFAULT_PASSWORD"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            _logger.LogWarning("ADMIN_DEFAULT_PASSWORD chưa được cấu hình; bỏ qua khởi tạo tài khoản quản trị.");
            return;
        }

        const string adminUsername = "admin";
        const string adminEmail = "admin@yourdomain.com";
        var admin = await _userManager.FindByNameAsync(adminUsername);
        if (admin is not null)
        {
            _logger.LogInformation("Tài khoản quản trị mặc định đã tồn tại; không thay đổi mật khẩu.");
            return;
        }

        admin = new ApplicationUser
        {
            UserName = adminUsername,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "Quản trị viên"
        };

        var createResult = await _userManager.CreateAsync(admin, adminPassword);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Không thể tạo tài khoản quản trị: {Errors}",
                string.Join("; ", createResult.Errors.Select(error => error.Description)));
            return;
        }

        var roleResult = await _userManager.AddToRoleAsync(admin, "SuperAdmin");
        if (!roleResult.Succeeded)
        {
            _logger.LogError("Tài khoản admin đã được tạo nhưng không gán được role SuperAdmin: {Errors}",
                string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            return;
        }

        _logger.LogInformation("Đã khởi tạo tài khoản quản trị mặc định.");
    }
}
