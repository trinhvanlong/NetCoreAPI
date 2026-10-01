using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models
{
    public class NhanVien
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập phòng ban")]
        [StringLength(100)]
        [Display(Name = "Phòng ban")]
        public string PhongBan { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập chức vụ")]
        [StringLength(100)]
        [Display(Name = "Chức vụ")]
        public string ChucVu { get; set; } = "";

        [Range(0, 1000000000, ErrorMessage = "Lương phải từ 0 đến 1.000.000.000")]
        [Display(Name = "Lương")]
        public decimal Luong { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày vào làm")]
        public DateTime NgayVaoLam { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "Đang làm việc";
    }
}
