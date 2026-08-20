using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DemoMVC.Models;

namespace DemoMVC.Controllers;

public class DemoController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
    public IActionResult Hello()
    {
        return View();
    }
    public IActionResult Privacy()
    {
        return View();
    }
}


