
using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models
{
    public class Khoa
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Mã khoa")]
        public string MaKhoa { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Tên khoa")]
        public string TenKhoa { get; set; } = string.Empty;

        // Một khoa có nhiều sinh viên
        public ICollection<SinhVien> SinhViens { get; set; }
            = new List<SinhVien>();
    }
}
