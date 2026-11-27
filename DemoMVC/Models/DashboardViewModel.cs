namespace DemoMVC.Models;

public sealed record DashboardViewModel(
    int? TotalStudents,
    int? TotalEmployees,
    bool IsDatabaseAvailable,
    string? DataSourceMessage);
