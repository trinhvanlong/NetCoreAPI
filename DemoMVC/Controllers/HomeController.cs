using System.Diagnostics;
using DemoMVC.Data;
using Microsoft.AspNetCore.Mvc;
using DemoMVC.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ApplicationDbContext context,
        ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var dashboard = new DashboardViewModel(
                await _context.SinhViens.CountAsync(),
                await _context.NhanViens.CountAsync(),
                true,
                null);

            return View(dashboard);
        }
        catch (SqlException exception)
        {
            _logger.LogError(exception, "Unable to load dashboard metrics from SQL Server.");

            return View(new DashboardViewModel(
                null,
                null,
                false,
                "Không thể kết nối cơ sở dữ liệu. Vui lòng kiểm tra SQL Server."));
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
