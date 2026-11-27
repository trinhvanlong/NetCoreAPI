namespace DemoMVC.Services.Identity;

public interface IIdentitySeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
