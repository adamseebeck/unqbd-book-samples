using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseApi.Migrations;

/// <inheritdoc />
public partial class AddDeadLetterTracking : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "DeadLetterRetries",
            table: "Shipments",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeadLetteredAt",
            table: "Shipments",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastRetriedAt",
            table: "Shipments",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "NextRetryAt",
            table: "Shipments",
            type: "datetime2",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DeadLetterRetries",
            table: "Shipments");

        migrationBuilder.DropColumn(
            name: "DeadLetteredAt",
            table: "Shipments");

        migrationBuilder.DropColumn(
            name: "LastRetriedAt",
            table: "Shipments");

        migrationBuilder.DropColumn(
            name: "NextRetryAt",
            table: "Shipments");
    }
}
