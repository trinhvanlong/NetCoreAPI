using DemoMVC.Data;
using DemoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers
{
    public class NhanVienController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NhanVienController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchString)
        {
            var query = _context.NhanViens.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(n => n.HoTen.Contains(searchString) ||
                                         n.Email.Contains(searchString) ||
                                         n.PhongBan.Contains(searchString) ||
                                         n.ChucVu.Contains(searchString));
            }
            ViewData["SearchString"] = searchString;
            return View(await query.OrderBy(n => n.Id).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(m => m.Id == id);
            if (nhanVien == null) return NotFound();
            return View(nhanVien);
        }

        public IActionResult Create() => View(new NhanVien { NgayVaoLam = DateTime.Today });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien nhanVien)
        {
            if (ModelState.IsValid)
            {
                _context.Add(nhanVien);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm nhân viên thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(nhanVien);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null) return NotFound();
            return View(nhanVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhanVien nhanVien)
        {
            if (id != nhanVien.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(nhanVien);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật nhân viên thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NhanVienExists(nhanVien.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(nhanVien);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var nhanVien = await _context.NhanViens.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (nhanVien == null) return NotFound();
            return View(nhanVien);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien != null)
            {
                _context.NhanViens.Remove(nhanVien);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa nhân viên thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool NhanVienExists(int id) => _context.NhanViens.Any(e => e.Id == id);
    }
}
