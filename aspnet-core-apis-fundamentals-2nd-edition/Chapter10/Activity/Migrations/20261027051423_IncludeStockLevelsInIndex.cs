using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseApi.Migrations;

/// <inheritdoc />
public partial class IncludeStockLevelsInIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_StockItems_WarehouseCode_ProductSku",
            table: "StockItems");

        migrationBuilder.CreateIndex(
            name: "IX_StockItems_WarehouseCode_ProductSku",
            table: "StockItems",
            columns: new[] { "WarehouseCode", "ProductSku" },
            unique: true)
            .Annotation("SqlServer:Include", new[] { "OnHand", "Reserved", "ReorderThreshold" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_StockItems_WarehouseCode_ProductSku",
            table: "StockItems");

        migrationBuilder.CreateIndex(
            name: "IX_StockItems_WarehouseCode_ProductSku",
            table: "StockItems",
            columns: new[] { "WarehouseCode", "ProductSku" },
            unique: true);
    }
}
