
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models
{
    public class SinhVien
    {
        // Khóa chính
        public int Id { get; set; }

        // Mã sinh viên được SinhVienController tự động tạo
        [Required(ErrorMessage = "Mã sinh viên không được để trống.")]
        [StringLength(10, MinimumLength = 10,
            ErrorMessage = "Mã sinh viên phải gồm đúng 10 chữ số.")]
        [RegularExpression(@"^\d{10}$",
            ErrorMessage = "Mã sinh viên chỉ được chứa 10 chữ số.")]
        [Display(Name = "Mã sinh viên")]
        public string MaSinhVien { get; set; } = string.Empty;

        // Họ và tên sinh viên
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100,
            ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        // Email
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // Số điện thoại
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        // Năm nhập học
        [Range(2000, 2099,
            ErrorMessage = "Năm nhập học phải từ 2000 đến 2099.")]
        [Display(Name = "Năm nhập học")]
        public int NamNhapHoc { get; set; } = 2028;

        // Mã hệ đào tạo: 21 = đại học chính quy theo quy ước bài tập
        [Required(ErrorMessage = "Vui lòng chọn mã hệ đào tạo.")]
        [RegularExpression(@"^\d{2}$",
            ErrorMessage = "Mã hệ đào tạo phải gồm 2 chữ số.")]
        [Display(Name = "Mã hệ đào tạo")]
        public string MaHeDaoTao { get; set; } = "21";

        // Mã ngành: 05 = ngành theo quy ước bài tập
        [Required(ErrorMessage = "Vui lòng chọn mã ngành.")]
        [RegularExpression(@"^\d{2}$",
            ErrorMessage = "Mã ngành phải gồm 2 chữ số.")]
        [Display(Name = "Mã ngành")]
        public string MaNganh { get; set; } = "05";

        // KHÓA NGOẠI: tham chiếu đến Khoa.Id
        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn khoa.")]
        [Display(Name = "Khoa")]
        public int KhoaId { get; set; }

        // Thuộc tính điều hướng đến bảng Khoa
        [ForeignKey(nameof(KhoaId))]
        public Khoa? Khoa { get; set; }
    }
}
