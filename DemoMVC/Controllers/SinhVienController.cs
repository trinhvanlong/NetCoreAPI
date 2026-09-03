using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DemoMVC.Models;

namespace DemoMVC.Controllers;

    public class SinhVienController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.ThongBao = "Nhập Thông Tin Sinh Viên";
        ViewData["Khoa"] = "Công Nghệ Thông Tin";
        return View();
    }
    [HttpPost]
    public IActionResult Index(SinhVien sv)
    {
        ViewBag.ThongBao  = "Thông Tin Sinh Viên";
        ViewData["Khoa"] = "Công Nghệ Thông Tin";

        return View(sv);
    }
}