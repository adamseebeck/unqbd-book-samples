using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Retail.Migrations;

/// <inheritdoc />
public partial class _20261031212032_AddSuppliers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Suppliers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                ContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Suppliers", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Suppliers_ContactEmail",
            table: "Suppliers",
            column: "ContactEmail",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Suppliers");
    }
}
