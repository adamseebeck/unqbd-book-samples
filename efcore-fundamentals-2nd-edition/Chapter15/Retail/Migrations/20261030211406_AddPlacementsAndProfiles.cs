using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Retail.Migrations;

/// <inheritdoc />
public partial class _20261030211406_AddPlacementsAndProfiles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CustomerProfile",
            columns: table => new
            {
                CustomerId = table.Column<int>(type: "int", nullable: false),
                LoyaltyTier = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerProfile", x => x.CustomerId);
                table.ForeignKey(
                    name: "FK_CustomerProfile_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProductCategories",
            columns: table => new
            {
                ProductId = table.Column<int>(type: "int", nullable: false),
                CategoryId = table.Column<int>(type: "int", nullable: false),
                StockThreshold = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductCategories", x => new { x.ProductId, x.CategoryId });
                table.ForeignKey(
                    name: "FK_ProductCategories_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ProductCategories_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProductCategories_CategoryId",
            table: "ProductCategories",
            column: "CategoryId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CustomerProfile");

        migrationBuilder.DropTable(
            name: "ProductCategories");
    }
}
