
using DemoMVC.Data;
using DemoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // READ: Danh sách sinh viên kèm thông tin khoa
        public async Task<IActionResult> Index()
        {
            var danhSach = await _context.SinhViens
                .AsNoTracking()
                .Include(s => s.Khoa)
                .OrderBy(s => s.MaSinhVien)
                .ToListAsync();

            return View(danhSach);
        }

        // Nạp danh sách khoa cho dropdown
        private async Task NapDanhSachKhoaAsync(int? khoaId = null)
        {
            ViewBag.KhoaId = new SelectList(
                await _context.Khoas
                    .AsNoTracking()
                    .OrderBy(k => k.TenKhoa)
                    .ToListAsync(),
                "Id",
                "TenKhoa",
                khoaId
            );
        }

        // TỰ ĐỘNG SINH MÃ SINH VIÊN
        // Ví dụ: 2821050199
        private async Task<string> TaoMaSinhVienAsync(
            int namNhapHoc,
            string maHeDaoTao,
            string maNganh)
        {
            string tienTo =
                $"{namNhapHoc % 100:D2}{maHeDaoTao}{maNganh}";

            var danhSachMa = await _context.SinhViens
                .AsNoTracking()
                .Where(s => s.MaSinhVien.StartsWith(tienTo))
                .Select(s => s.MaSinhVien)
                .ToListAsync();

            int soThuTuLonNhat = danhSachMa
                .Where(ma =>
                    !string.IsNullOrEmpty(ma) &&
                    ma.Length == 10 &&
                    ma.StartsWith(tienTo))
                .Select(ma =>
                    int.TryParse(ma.Substring(6, 4), out int so)
                        ? so
                        : 0)
                .DefaultIfEmpty(0)
                .Max();

            if (soThuTuLonNhat >= 9999)
            {
                throw new InvalidOperationException(
                    "Đã hết số thứ tự sinh viên cho tiền tố " + tienTo);
            }

            return tienTo + (soThuTuLonNhat + 1).ToString("D4");
        }

        // CREATE: Hiển thị form thêm
        public async Task<IActionResult> Create()
        {
            await NapDanhSachKhoaAsync();

            var sinhVien = new SinhVien
            {
                NamNhapHoc = 2028,
                MaHeDaoTao = "21",
                MaNganh = "05"
            };

            return View(sinhVien);
        }

        // CREATE: Lưu sinh viên mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SinhVien sinhVien)
        {
            // Không nhận mã sinh viên từ form
            ModelState.Remove(nameof(SinhVien.MaSinhVien));
            ModelState.Remove(nameof(SinhVien.Khoa));

            // Kiểm tra khóa ngoại
            bool khoaTonTai = await _context.Khoas
                .AnyAsync(k => k.Id == sinhVien.KhoaId);

            if (!khoaTonTai)
            {
                ModelState.AddModelError(
                    nameof(SinhVien.KhoaId),
                    "Vui lòng chọn khoa hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                await NapDanhSachKhoaAsync(sinhVien.KhoaId);
                return View(sinhVien);
            }

            sinhVien.MaSinhVien = await TaoMaSinhVienAsync(
                sinhVien.NamNhapHoc,
                sinhVien.MaHeDaoTao,
                sinhVien.MaNganh
            );

            _context.SinhViens.Add(sinhVien);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] =
                $"Thêm sinh viên thành công. Mã sinh viên: {sinhVien.MaSinhVien}";

            return RedirectToAction(nameof(Index));
        }

        // READ: Xem chi tiết sinh viên
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var sinhVien = await _context.SinhViens
                .AsNoTracking()
                .Include(s => s.Khoa)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sinhVien == null)
                return NotFound();

            return View(sinhVien);
        }

        // UPDATE: Hiển thị form sửa
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var sinhVien = await _context.SinhViens
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sinhVien == null)
                return NotFound();

            await NapDanhSachKhoaAsync(sinhVien.KhoaId);
            return View(sinhVien);
        }

        // UPDATE: Lưu thông tin đã sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, SinhVien sinhVien)
        {
            if (id != sinhVien.Id)
                return NotFound();

            // Không cho sửa mã sinh viên và đối tượng điều hướng
            ModelState.Remove(nameof(SinhVien.MaSinhVien));
            ModelState.Remove(nameof(SinhVien.Khoa));

            if (!ModelState.IsValid)
            {
                await NapDanhSachKhoaAsync(sinhVien.KhoaId);
                return View(sinhVien);
            }

            var sinhVienCu = await _context.SinhViens
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sinhVienCu == null)
                return NotFound();

            // Kiểm tra khoa mới
            bool khoaTonTai = await _context.Khoas
                .AnyAsync(k => k.Id == sinhVien.KhoaId);

            if (!khoaTonTai)
            {
                ModelState.AddModelError(
                    nameof(SinhVien.KhoaId),
                    "Vui lòng chọn khoa hợp lệ.");

                await NapDanhSachKhoaAsync(sinhVien.KhoaId);
                return View(sinhVien);
            }

            // Cập nhật thông tin, giữ nguyên mã sinh viên
            sinhVienCu.HoTen = sinhVien.HoTen;
            sinhVienCu.Email = sinhVien.Email;
            sinhVienCu.SoDienThoai = sinhVien.SoDienThoai;
            sinhVienCu.NamNhapHoc = sinhVien.NamNhapHoc;
            sinhVienCu.MaHeDaoTao = sinhVien.MaHeDaoTao;
            sinhVienCu.MaNganh = sinhVien.MaNganh;

            // Cập nhật khóa ngoại
            sinhVienCu.KhoaId = sinhVien.KhoaId;

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Cập nhật sinh viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE: Hiển thị thông tin trước khi xóa
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var sinhVien = await _context.SinhViens
                .AsNoTracking()
                .Include(s => s.Khoa)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sinhVien == null)
                return NotFound();

            return View(sinhVien);
        }

        // DELETE: Xóa sinh viên
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sinhVien = await _context.SinhViens
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sinhVien == null)
                return NotFound();

            _context.SinhViens.Remove(sinhVien);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Xóa sinh viên thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
