using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseApi.Migrations;

/// <inheritdoc />
public partial class AddReservationAudit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CorrelationId",
            table: "StockAdjustments",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReferenceNumber",
            table: "StockAdjustments",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ReservedChange",
            table: "StockAdjustments",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CorrelationId",
            table: "StockAdjustments");

        migrationBuilder.DropColumn(
            name: "ReferenceNumber",
            table: "StockAdjustments");

        migrationBuilder.DropColumn(
            name: "ReservedChange",
            table: "StockAdjustments");
    }
}
