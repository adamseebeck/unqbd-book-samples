using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Retail.Migrations;

/// <inheritdoc />
public partial class _20261031211743_AddExternalReference : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ExternalReference",
            table: "Products",
            type: "nvarchar(max)",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ExternalReference",
            table: "Products");
    }
}
