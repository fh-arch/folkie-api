using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Folkie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brand_favorites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    influencer_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brand_favorites", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_brand_favorites_brand_profile_id_influencer_profile_id",
                table: "brand_favorites",
                columns: new[] { "brand_profile_id", "influencer_profile_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brand_favorites");
        }
    }
}
