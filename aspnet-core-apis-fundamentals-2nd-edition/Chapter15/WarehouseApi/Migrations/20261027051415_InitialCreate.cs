using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseApi.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IdempotencyRecords",
            columns: table => new
            {
                Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_BIN2"),
                RequestHash = table.Column<string>(type: "nvarchar(44)", maxLength: 44, nullable: false),
                StatusCode = table.Column<int>(type: "int", nullable: false),
                ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdempotencyRecords", x => x.Key);
            });

        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Sku = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Sku);
            });

        migrationBuilder.CreateTable(
            name: "Warehouses",
            columns: table => new
            {
                Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                Id = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                IsOperational = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Warehouses", x => x.Code);
            });

        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                FulfillmentWarehouseCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
                table.ForeignKey(
                    name: "FK_Orders_Warehouses_FulfillmentWarehouseCode",
                    column: x => x.FulfillmentWarehouseCode,
                    principalTable: "Warehouses",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "StockItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductSku = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                WarehouseCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                OnHand = table.Column<int>(type: "int", nullable: false),
                Reserved = table.Column<int>(type: "int", nullable: false),
                ReorderThreshold = table.Column<int>(type: "int", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_StockItems_Products_ProductSku",
                    column: x => x.ProductSku,
                    principalTable: "Products",
                    principalColumn: "Sku",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StockItems_Warehouses_WarehouseCode",
                    column: x => x.WarehouseCode,
                    principalTable: "Warehouses",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "OrderLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Sku = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                RequestedQuantity = table.Column<int>(type: "int", nullable: false),
                ReservedQuantity = table.Column<int>(type: "int", nullable: false),
                ShippedQuantity = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrderLines_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_OrderLines_Products_Sku",
                    column: x => x.Sku,
                    principalTable: "Products",
                    principalColumn: "Sku",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "StockAdjustments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StockItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReasonCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                QuantityChange = table.Column<int>(type: "int", nullable: false),
                AdjustedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                table.ForeignKey(
                    name: "FK_StockAdjustments_StockItems_StockItemId",
                    column: x => x.StockItemId,
                    principalTable: "StockItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OrderLines_OrderId",
            table: "OrderLines",
            column: "OrderId");

        migrationBuilder.CreateIndex(
            name: "IX_OrderLines_Sku",
            table: "OrderLines",
            column: "Sku");

        migrationBuilder.CreateIndex(
            name: "IX_Orders_FulfillmentWarehouseCode",
            table: "Orders",
            column: "FulfillmentWarehouseCode");

        migrationBuilder.CreateIndex(
            name: "IX_Products_Name",
            table: "Products",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_StockAdjustments_StockItemId",
            table: "StockAdjustments",
            column: "StockItemId");

        migrationBuilder.CreateIndex(
            name: "IX_StockItems_ProductSku",
            table: "StockItems",
            column: "ProductSku");

        migrationBuilder.CreateIndex(
            name: "IX_StockItems_WarehouseCode_ProductSku",
            table: "StockItems",
            columns: new[] { "WarehouseCode", "ProductSku" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Warehouses_Id",
            table: "Warehouses",
            column: "Id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IdempotencyRecords");

        migrationBuilder.DropTable(
            name: "OrderLines");

        migrationBuilder.DropTable(
            name: "StockAdjustments");

        migrationBuilder.DropTable(
            name: "Orders");

        migrationBuilder.DropTable(
            name: "StockItems");

        migrationBuilder.DropTable(
            name: "Products");

        migrationBuilder.DropTable(
            name: "Warehouses");
    }
}
