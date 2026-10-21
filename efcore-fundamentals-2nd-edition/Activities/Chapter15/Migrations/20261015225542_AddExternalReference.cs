using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfCoreBook.Migrations;

/// <inheritdoc />
public partial class _20261015225542_AddExternalReference : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ExternalReference",
            table: "Products",
            type: "nvarchar(500)",
            maxLength: 500,
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
