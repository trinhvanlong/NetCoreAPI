
using DemoMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Khai báo các bảng trong database
        public DbSet<SinhVien> SinhViens { get; set; }
        public DbSet<Khoa> Khoas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================================
            // 1. CẤU HÌNH BẢNG KHOA
            // ================================

            // Mã khoa không được trùng nhau
            modelBuilder.Entity<Khoa>()
                .HasIndex(k => k.MaKhoa)
                .IsUnique();

            // Nhập dữ liệu các khoa HUMG
            modelBuilder.Entity<Khoa>().HasData(
                new Khoa
                {
                    Id = 1,
                    MaKhoa = "CNTT",
                    TenKhoa = "Công nghệ thông tin"
                },
                new Khoa
                {
                    Id = 2,
                    MaKhoa = "MO",
                    TenKhoa = "Mỏ"
                },
                new Khoa
                {
                    Id = 3,
                    MaKhoa = "CODIEN",
                    TenKhoa = "Cơ điện"
                },
                new Khoa
                {
                    Id = 4,
                    MaKhoa = "KINHTE",
                    TenKhoa = "Kinh tế và Quản trị kinh doanh"
                },
                new Khoa
                {
                    Id = 5,
                    MaKhoa = "MOITRUONG",
                    TenKhoa = "Môi trường"
                },
                new Khoa
                {
                    Id = 6,
                    MaKhoa = "XAYDUNG",
                    TenKhoa = "Xây dựng"
                },
                new Khoa
                {
                    Id = 7,
                    MaKhoa = "DAUKHI",
                    TenKhoa = "Dầu khí"
                },
                new Khoa
                {
                    Id = 8,
                    MaKhoa = "DIACHAT",
                    TenKhoa = "Khoa học và Kỹ thuật Địa chất"
                },
                new Khoa
                {
                    Id = 9,
                    MaKhoa = "TRACDIA",
                    TenKhoa = "Trắc địa - Bản đồ và Quản lý đất đai"
                },
                new Khoa
                {
                    Id = 10,
                    MaKhoa = "KHCB",
                    TenKhoa = "Khoa học cơ bản"
                }
            );

            // ================================
            // 2. CẤU HÌNH BẢNG SINH VIÊN
            // ================================

            // Mã sinh viên không được trùng nhau
            modelBuilder.Entity<SinhVien>()
                .HasIndex(s => s.MaSinhVien)
                .IsUnique();

            // Quan hệ: Một khoa có nhiều sinh viên
            modelBuilder.Entity<SinhVien>()
                .HasOne(s => s.Khoa)
                .WithMany(k => k.SinhViens)
                .HasForeignKey(s => s.KhoaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
