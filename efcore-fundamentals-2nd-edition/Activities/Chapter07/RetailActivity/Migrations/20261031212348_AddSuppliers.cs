using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Retail.Migrations;

/// <inheritdoc />
public partial class _20261031212348_AddSuppliers : Migration
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
            name: "Suppliers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Suppliers", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Products_SupplierId",
            table: "Products",
            column: "SupplierId");

        migrationBuilder.AddForeignKey(
            name: "FK_Products_Suppliers_SupplierId",
            table: "Products",
            column: "SupplierId",
            principalTable: "Suppliers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Products_Suppliers_SupplierId",
            table: "Products");

        migrationBuilder.DropTable(
            name: "Suppliers");

        migrationBuilder.DropIndex(
            name: "IX_Products_SupplierId",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "SupplierId",
            table: "Products");
    }
}
