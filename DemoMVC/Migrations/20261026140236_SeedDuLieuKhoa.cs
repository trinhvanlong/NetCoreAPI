using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DemoMVC.Migrations
{
    /// <inheritdoc />
    public partial class SeedDuLieuKhoa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SoDienThoai",
                table: "SinhViens",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "HoTen",
                table: "SinhViens",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "SinhViens",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "KhoaId",
                table: "SinhViens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MaHeDaoTao",
                table: "SinhViens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaNganh",
                table: "SinhViens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaSinhVien",
                table: "SinhViens",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NamNhapHoc",
                table: "SinhViens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Khoas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKhoa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenKhoa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Khoas", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Khoas",
                columns: new[] { "Id", "MaKhoa", "TenKhoa" },
                values: new object[,]
                {
                    { 1, "CNTT", "Công nghệ thông tin" },
                    { 2, "MO", "Mỏ" },
                    { 3, "CODIEN", "Cơ điện" },
                    { 4, "KINHTE", "Kinh tế và Quản trị kinh doanh" },
                    { 5, "MOITRUONG", "Môi trường" },
                    { 6, "XAYDUNG", "Xây dựng" },
                    { 7, "DAUKHI", "Dầu khí" },
                    { 8, "DIACHAT", "Khoa học và Kỹ thuật Địa chất" },
                    { 9, "TRACDIA", "Trắc địa - Bản đồ và Quản lý đất đai" },
                    { 10, "KHCB", "Khoa học cơ bản" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SinhViens_KhoaId",
                table: "SinhViens",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_SinhViens_MaSinhVien",
                table: "SinhViens",
                column: "MaSinhVien",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Khoas_MaKhoa",
                table: "Khoas",
                column: "MaKhoa",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SinhViens_Khoas_KhoaId",
                table: "SinhViens",
                column: "KhoaId",
                principalTable: "Khoas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SinhViens_Khoas_KhoaId",
                table: "SinhViens");

            migrationBuilder.DropTable(
                name: "Khoas");

            migrationBuilder.DropIndex(
                name: "IX_SinhViens_KhoaId",
                table: "SinhViens");

            migrationBuilder.DropIndex(
                name: "IX_SinhViens_MaSinhVien",
                table: "SinhViens");

            migrationBuilder.DropColumn(
                name: "KhoaId",
                table: "SinhViens");

            migrationBuilder.DropColumn(
                name: "MaHeDaoTao",
                table: "SinhViens");

            migrationBuilder.DropColumn(
                name: "MaNganh",
                table: "SinhViens");

            migrationBuilder.DropColumn(
                name: "MaSinhVien",
                table: "SinhViens");

            migrationBuilder.DropColumn(
                name: "NamNhapHoc",
                table: "SinhViens");

            migrationBuilder.AlterColumn<string>(
                name: "SoDienThoai",
                table: "SinhViens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15);

            migrationBuilder.AlterColumn<string>(
                name: "HoTen",
                table: "SinhViens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "SinhViens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
