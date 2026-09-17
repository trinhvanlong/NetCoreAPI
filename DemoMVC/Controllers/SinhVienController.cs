using DemoMVC.Data;
using DemoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers
{
    public class SinhVienController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SinhVienController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var danhSach = await _context.SinhViens
                .AsNoTracking()
                .ToListAsync();

            return View(danhSach);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SinhVien sinhVien)
        {
            if (ModelState.IsValid)
            {
                _context.SinhViens.Add(sinhVien);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(sinhVien);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sinhVien = await _context.SinhViens.FindAsync(id);

            if (sinhVien == null)
            {
                return NotFound();
            }

            return View(sinhVien);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SinhVien sinhVien)
        {
            if (id != sinhVien.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sinhVien);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SinhVienExists(sinhVien.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(sinhVien);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sinhVien = await _context.SinhViens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (sinhVien == null)
            {
                return NotFound();
            }

            return View(sinhVien);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sinhVien = await _context.SinhViens.FindAsync(id);

            if (sinhVien != null)
            {
                _context.SinhViens.Remove(sinhVien);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SinhVienExists(int id)
        {
            return _context.SinhViens.Any(x => x.Id == id);
        }
    }
}