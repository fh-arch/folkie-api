using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Folkie.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddUserBlocking : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_blocked",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "blocked_reason",
            table: "users",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "blocked_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "is_blocked",       table: "users");
        migrationBuilder.DropColumn(name: "blocked_reason",   table: "users");
        migrationBuilder.DropColumn(name: "blocked_at",       table: "users");
    }
}
