using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Retail.Migrations;

/// <inheritdoc />
public partial class _20261030220854_AddAddressPaymentsAndAudit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsDeleted",
            table: "Products",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastModifiedUtc",
            table: "Products",
            type: "datetime2",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<string>(
            name: "ShippingAddress_PostalCode",
            table: "Customers",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "ShippingAddress_Street",
            table: "Customers",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Kind = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                LastFour = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Payments");

        migrationBuilder.DropColumn(
            name: "IsDeleted",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "LastModifiedUtc",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "ShippingAddress_PostalCode",
            table: "Customers");

        migrationBuilder.DropColumn(
            name: "ShippingAddress_Street",
            table: "Customers");
    }
}
