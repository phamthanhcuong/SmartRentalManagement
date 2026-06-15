using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManagementSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyName = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    BankName = table.Column<string>(type: "TEXT", nullable: true),
                    BankAccount = table.Column<string>(type: "TEXT", nullable: true),
                    InvoiceFooterNote = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultElectricPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DefaultWaterPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LateFeePercent = table.Column<decimal>(type: "TEXT", precision: 9, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Address", "BankAccount", "BankName", "CompanyName", "CreatedAt", "DefaultElectricPrice", "DefaultWaterPrice", "InvoiceFooterNote", "IsDeleted", "LateFeePercent", "Phone", "UpdatedAt" },
                values: new object[] { 1, null, null, null, "NHÀ TRỌ", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3500m, 15000m, "Cảm ơn Quý khách!", false, 0m, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
