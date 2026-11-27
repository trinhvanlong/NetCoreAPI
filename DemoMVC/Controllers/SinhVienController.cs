
using DemoMVC.Data;
using DemoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DemoMVC.Controllers
{
    [Authorize(Policy = "AdminManagement")]
    public class SinhVienController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SinhVienController> _logger;

        public SinhVienController(
            ApplicationDbContext context,
            ILogger<SinhVienController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // READ: Danh sách sinh viên kèm thông tin khoa
        public async Task<IActionResult> Index(
            string? search,
            int khoaId = 0,
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize is < 5 or > 50) pageSize = 10;

            var query = _context.SinhViens
                .AsNoTracking()
                .Include(s => s.Khoa)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var tuKhoa = search.Trim();
                query = query.Where(s =>
                    s.MaSinhVien.Contains(tuKhoa) ||
                    s.HoTen.Contains(tuKhoa) ||
                    s.Email.Contains(tuKhoa) ||
                    s.SoDienThoai.Contains(tuKhoa));
            }

            if (khoaId > 0)
            {
                query = query.Where(s => s.KhoaId == khoaId);
            }

            var tongSo = await query.CountAsync();
            var tongSoTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)pageSize));
            if (page > tongSoTrang) page = tongSoTrang;

            var danhSach = await query
                .OrderBy(s => s.MaSinhVien)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await NapDanhSachKhoaAsync(khoaId > 0 ? khoaId : null);
            ViewBag.Search = search?.Trim();
            ViewBag.KhoaIdFilter = khoaId;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = tongSo;
            ViewBag.TotalPages = tongSoTrang;

            if (IsAjaxRequest())
            {
                return PartialView("_ListPartial", danhSach);
            }

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

            if (IsAjaxRequest())
            {
                ViewData["FormAction"] = nameof(Create);
                ViewData["FormTitle"] = "Thêm sinh viên";
                return PartialView("_FormModal", sinhVien);
            }

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

            if (!ModelState.IsValid)
            {
                await NapDanhSachKhoaAsync(sinhVien.KhoaId);
                return IsAjaxRequest() ? ValidationError() : View(sinhVien);
            }

            // Kiểm tra khóa ngoại sau khi xác thực định dạng đầu vào.
            bool khoaTonTai = await _context.Khoas
                .AnyAsync(k => k.Id == sinhVien.KhoaId);
            if (!khoaTonTai)
            {
                ModelState.AddModelError(
                    nameof(SinhVien.KhoaId),
                    "Vui lòng chọn khoa hợp lệ.");
                await NapDanhSachKhoaAsync(sinhVien.KhoaId);
                return IsAjaxRequest() ? ValidationError() : View(sinhVien);
            }

            sinhVien.MaSinhVien = await TaoMaSinhVienAsync(
                sinhVien.NamNhapHoc,
                sinhVien.MaHeDaoTao,
                sinhVien.MaNganh
            );

            _context.SinhViens.Add(sinhVien);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                if (IsAjaxRequest())
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "Mã sinh viên vừa được cấp cho hồ sơ khác. Vui lòng thử lại."
                    });
                }

                _logger.LogWarning(exception, "A duplicate student code was generated.");
                ModelState.AddModelError(string.Empty, "Không thể tạo mã sinh viên duy nhất. Vui lòng thử lại.");
                await NapDanhSachKhoaAsync(sinhVien.KhoaId);
                return View(sinhVien);
            }

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = $"Thêm sinh viên thành công. Mã sinh viên: {sinhVien.MaSinhVien}"
                });
            }

            TempData["ThongBao"] =
                $"Thêm sinh viên thành công. Mã sinh viên: {sinhVien.MaSinhVien}";

            return RedirectToAction(nameof(Index));
        }

        // READ: Xem chi tiết sinh viên
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return RecordNotFound();

            var sinhVien = await _context.SinhViens
                .AsNoTracking()
                .Include(s => s.Khoa)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sinhVien == null)
                return RecordNotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DetailsModal", sinhVien);
            }

            return View(sinhVien);
        }

        // UPDATE: Hiển thị form sửa
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return RecordNotFound();

            var sinhVien = await _context.SinhViens
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sinhVien == null)
                return RecordNotFound();

            await NapDanhSachKhoaAsync(sinhVien.KhoaId);
            if (IsAjaxRequest())
            {
                ViewData["FormAction"] = nameof(Edit);
                ViewData["FormTitle"] = "Chỉnh sửa sinh viên";
                return PartialView("_FormModal", sinhVien);
            }

            return View(sinhVien);
        }

        // UPDATE: Lưu thông tin đã sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, SinhVien sinhVien)
        {
            if (id != sinhVien.Id)
                return RecordNotFound();

            // Không cho sửa mã sinh viên và đối tượng điều hướng
            ModelState.Remove(nameof(SinhVien.MaSinhVien));
            ModelState.Remove(nameof(SinhVien.Khoa));
            ModelState.Remove(nameof(SinhVien.NamNhapHoc));
            ModelState.Remove(nameof(SinhVien.MaHeDaoTao));
            ModelState.Remove(nameof(SinhVien.MaNganh));

            if (!ModelState.IsValid)
            {
                await NapDanhSachKhoaAsync(sinhVien.KhoaId);
                return IsAjaxRequest() ? ValidationError() : View(sinhVien);
            }

            var sinhVienCu = await _context.SinhViens
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sinhVienCu == null)
                return RecordNotFound();

            // Kiểm tra khoa mới
            bool khoaTonTai = await _context.Khoas
                .AnyAsync(k => k.Id == sinhVien.KhoaId);

            if (!khoaTonTai)
            {
                ModelState.AddModelError(
                    nameof(SinhVien.KhoaId),
                    "Vui lòng chọn khoa hợp lệ.");

                await NapDanhSachKhoaAsync(sinhVien.KhoaId);
                return IsAjaxRequest() ? ValidationError() : View(sinhVien);
            }

            // Cập nhật thông tin, giữ nguyên mã sinh viên
            sinhVienCu.HoTen = sinhVien.HoTen;
            sinhVienCu.Email = sinhVien.Email;
            sinhVienCu.SoDienThoai = sinhVien.SoDienThoai;

            // Cập nhật khóa ngoại
            sinhVienCu.KhoaId = sinhVien.KhoaId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                _logger.LogWarning(exception, "A duplicate student code was encountered while updating a student.");
                if (IsAjaxRequest())
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "Mã sinh viên đã tồn tại. Vui lòng kiểm tra lại."
                    });
                }

                ModelState.AddModelError(string.Empty, "Mã sinh viên đã tồn tại. Vui lòng kiểm tra lại.");
                await NapDanhSachKhoaAsync(sinhVien.KhoaId);
                return View(sinhVien);
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true, message = "Cập nhật sinh viên thành công." });
            }

            TempData["ThongBao"] = "Cập nhật sinh viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE: Hiển thị thông tin trước khi xóa
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return RecordNotFound();

            var sinhVien = await _context.SinhViens
                .AsNoTracking()
                .Include(s => s.Khoa)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sinhVien == null)
                return RecordNotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DeleteModal", sinhVien);
            }

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
                return RecordNotFound();

            _context.SinhViens.Remove(sinhVien);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is SqlException sqlException &&
                sqlException.Number == 547)
            {
                _logger.LogError(exception, "Unable to delete student {StudentId}.", id);
                if (IsAjaxRequest())
                {
                    return StatusCode(StatusCodes.Status409Conflict, new
                    {
                        success = false,
                        message = "Không thể xóa sinh viên này vì dữ liệu có thể đang được sử dụng."
                    });
                }

                throw;
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true, message = "Xóa sinh viên thành công." });
            }

            TempData["ThongBao"] = "Xóa sinh viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        private bool IsAjaxRequest() =>
            string.Equals(
                Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);

        private IActionResult ValidationError()
        {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error => new
                {
                    field = entry.Key,
                    message = GetValidationMessage(entry.Key, error.ErrorMessage)
                }))
                .ToArray();

            return BadRequest(new
            {
                success = false,
                message = "Vui lòng kiểm tra lại thông tin đã nhập.",
                errors
            });
        }

        private IActionResult RecordNotFound() =>
            IsAjaxRequest()
                ? NotFound(new { success = false, message = "Không tìm thấy sinh viên." })
                : NotFound();

        private static string GetValidationMessage(string field, string? message)
        {
            if (field == nameof(SinhVien.KhoaId))
            {
                return "Vui lòng chọn khoa hợp lệ.";
            }

            if (field == nameof(SinhVien.NamNhapHoc) &&
                (string.IsNullOrWhiteSpace(message) || message.StartsWith("The value '", StringComparison.Ordinal)))
            {
                return "Vui lòng nhập năm nhập học hợp lệ.";
            }

            return string.IsNullOrWhiteSpace(message) ? "Giá trị không hợp lệ." : message;
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
            exception.InnerException is SqlException sqlException &&
            sqlException.Number is 2601 or 2627;
    }
}
