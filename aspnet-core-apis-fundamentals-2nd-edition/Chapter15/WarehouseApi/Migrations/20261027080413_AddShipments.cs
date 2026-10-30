using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseApi.Migrations;

/// <inheritdoc />
public partial class AddShipments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Shipments",
            columns: table => new
            {
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ShipmentId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                TrackingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Attempts = table.Column<int>(type: "int", nullable: false),
                LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Shipments", x => x.OrderId);
                table.ForeignKey(
                    name: "FK_Shipments_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Shipments_Status",
            table: "Shipments",
            column: "Status");

        // Orders allocated before this migration get a pending shipment, so the processor sends them as well
        migrationBuilder.Sql("""
            INSERT INTO Shipments (OrderId, Status, Attempts, CorrelationId, RequestedAt)
            SELECT Id, 'Pending', 0, REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''), SYSUTCDATETIME()
            FROM Orders
            WHERE Status = 'Allocated'
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Shipments");
    }
}
