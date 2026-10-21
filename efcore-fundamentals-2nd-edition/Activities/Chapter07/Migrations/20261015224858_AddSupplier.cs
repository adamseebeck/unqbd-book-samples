using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfCoreBook.Migrations;

/// <inheritdoc />
public partial class _20261015224858_AddSupplier : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SupplierId",
            table: "Products",
            type: "int",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Supplier",
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
                table.PrimaryKey("PK_Supplier", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Products_SupplierId",
            table: "Products",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_Supplier_CompanyName",
            table: "Supplier",
            column: "CompanyName")
            .Annotation("SqlServer:Clustered", false);

        migrationBuilder.CreateIndex(
            name: "IX_Suppliers_ContactEmail",
            table: "Supplier",
            column: "ContactEmail",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Products_Supplier_SupplierId",
            table: "Products",
            column: "SupplierId",
            principalTable: "Supplier",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Products_Supplier_SupplierId",
            table: "Products");

        migrationBuilder.DropTable(
            name: "Supplier");

        migrationBuilder.DropIndex(
            name: "IX_Products_SupplierId",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "SupplierId",
            table: "Products");
    }
}
