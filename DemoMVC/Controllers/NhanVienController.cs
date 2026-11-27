using DemoMVC.Data;
using DemoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DemoMVC.Controllers
{
    [Authorize(Policy = "AdminManagement")]
    public class NhanVienController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NhanVienController> _logger;

        public NhanVienController(
            ApplicationDbContext context,
            ILogger<NhanVienController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize is < 5 or > 50) pageSize = 10;

            var query = _context.NhanViens.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(n =>
                    n.MaNhanVien.Contains(keyword) ||
                    n.HoTen.Contains(keyword) ||
                    n.Email.Contains(keyword) ||
                    n.SoDienThoai.Contains(keyword) ||
                    n.PhongBan.Contains(keyword));
            }

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var employees = await query
                .OrderBy(n => n.MaNhanVien)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search?.Trim();
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            if (IsAjaxRequest())
            {
                return PartialView("_ListPartial", employees);
            }

            return View(employees);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return RecordNotFound();
            var employee = await _context.NhanViens.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);
            if (employee == null) return RecordNotFound();
            if (IsAjaxRequest())
            {
                return PartialView("_DetailsModal", employee);
            }

            return View(employee);
        }

        public IActionResult Create()
        {
            if (IsAjaxRequest())
            {
                ViewData["FormAction"] = nameof(Create);
                ViewData["FormTitle"] = "Thêm nhân viên";
                return PartialView("_FormModal", new NhanVien());
            }

            return View(new NhanVien());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien employee)
        {
            ModelState.Remove(nameof(NhanVien.MaNhanVien));

            if (!ModelState.IsValid)
            {
                return IsAjaxRequest() ? ValidationError() : View(employee);
            }

            employee.MaNhanVien = await TaoMaNhanVienAsync();
            _context.NhanViens.Add(employee);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                _logger.LogWarning(exception, "A duplicate employee code was generated.");
                if (IsAjaxRequest())
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "Mã nhân viên vừa được cấp cho hồ sơ khác. Vui lòng thử lại."
                    });
                }

                ModelState.AddModelError(string.Empty, "Không thể tạo mã nhân viên duy nhất. Vui lòng thử lại.");
                return View(employee);
            }

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = $"Thêm nhân viên thành công. Mã nhân viên: {employee.MaNhanVien}"
                });
            }

            TempData["ThongBao"] =
                $"Thêm nhân viên thành công. Mã nhân viên: {employee.MaNhanVien}";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return RecordNotFound();
            var employee = await _context.NhanViens.FindAsync(id.Value);
            if (employee == null) return RecordNotFound();
            if (IsAjaxRequest())
            {
                ViewData["FormAction"] = nameof(Edit);
                ViewData["FormTitle"] = "Chỉnh sửa nhân viên";
                return PartialView("_FormModal", employee);
            }

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhanVien employee)
        {
            if (id != employee.Id) return RecordNotFound();
            ModelState.Remove(nameof(NhanVien.MaNhanVien));

            if (!ModelState.IsValid)
            {
                return IsAjaxRequest() ? ValidationError() : View(employee);
            }

            var existing = await _context.NhanViens.FindAsync(id);
            if (existing == null) return RecordNotFound();

            existing.HoTen = employee.HoTen;
            existing.Email = employee.Email;
            existing.SoDienThoai = employee.SoDienThoai;
            existing.PhongBan = employee.PhongBan;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                _logger.LogWarning(exception, "A duplicate employee code was encountered while updating employee {EmployeeId}.", id);
                if (IsAjaxRequest())
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "Mã nhân viên đã tồn tại. Vui lòng kiểm tra lại."
                    });
                }

                ModelState.AddModelError(string.Empty, "Mã nhân viên đã tồn tại. Vui lòng kiểm tra lại.");
                return View(employee);
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true, message = "Cập nhật nhân viên thành công." });
            }
            TempData["ThongBao"] = "Cập nhật nhân viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return RecordNotFound();
            var employee = await _context.NhanViens.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);
            if (employee == null) return RecordNotFound();
            if (IsAjaxRequest())
            {
                return PartialView("_DeleteModal", employee);
            }

            return View(employee);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.NhanViens.FindAsync(id);
            if (employee == null) return RecordNotFound();

            _context.NhanViens.Remove(employee);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is SqlException sqlException &&
                sqlException.Number == 547)
            {
                _logger.LogError(exception, "Unable to delete employee {EmployeeId}.", id);
                if (IsAjaxRequest())
                {
                    return StatusCode(StatusCodes.Status409Conflict, new
                    {
                        success = false,
                        message = "Không thể xóa nhân viên này vì dữ liệu có thể đang được sử dụng."
                    });
                }

                throw;
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true, message = "Xóa nhân viên thành công." });
            }
            TempData["ThongBao"] = "Xóa nhân viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> TaoMaNhanVienAsync()
        {
            var codes = await _context.NhanViens
                .AsNoTracking()
                .Select(n => n.MaNhanVien)
                .ToListAsync();

            var next = codes
                .Select(code =>
                    int.TryParse(code.StartsWith("NV") ? code[2..] : code, out var number)
                        ? number
                        : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            if (next > 999999999) throw new InvalidOperationException("Đã hết mã nhân viên hợp lệ.");
            return $"NV{next:D8}";
        }

        private bool IsAjaxRequest() =>
            string.Equals(
                Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);

        private IActionResult RecordNotFound() =>
            IsAjaxRequest()
                ? NotFound(new { success = false, message = "Không tìm thấy nhân viên." })
                : NotFound();

        private IActionResult ValidationError()
        {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error => new
                {
                    field = entry.Key,
                    message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Giá trị không hợp lệ."
                        : error.ErrorMessage
                }))
                .ToArray();

            return BadRequest(new
            {
                success = false,
                message = "Vui lòng kiểm tra lại thông tin đã nhập.",
                errors
            });
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
            exception.InnerException is SqlException sqlException &&
            sqlException.Number is 2601 or 2627;
    }
}
