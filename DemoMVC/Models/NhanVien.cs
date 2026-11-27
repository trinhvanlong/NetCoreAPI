using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models
{
    public class NhanVien
    {
        public int Id { get; set; }

        [Display(Name = "Mã nhân viên")]
        public string MaNhanVien { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập phòng ban.")]
        [StringLength(100, ErrorMessage = "Phòng ban không được vượt quá 100 ký tự.")]
        [Display(Name = "Phòng ban")]
        public string PhongBan { get; set; } = string.Empty;
    }
}
