using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Folkie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTiktokOAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tiktok_access_token",
                table: "influencer_profiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tiktok_avatar_url",
                table: "influencer_profiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tiktok_likes_count",
                table: "influencer_profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tiktok_refresh_token",
                table: "influencer_profiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "tiktok_token_expires_at",
                table: "influencer_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tiktok_video_count",
                table: "influencer_profiles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tiktok_access_token",
                table: "influencer_profiles");

            migrationBuilder.DropColumn(
                name: "tiktok_avatar_url",
                table: "influencer_profiles");

            migrationBuilder.DropColumn(
                name: "tiktok_likes_count",
                table: "influencer_profiles");

            migrationBuilder.DropColumn(
                name: "tiktok_refresh_token",
                table: "influencer_profiles");

            migrationBuilder.DropColumn(
                name: "tiktok_token_expires_at",
                table: "influencer_profiles");

            migrationBuilder.DropColumn(
                name: "tiktok_video_count",
                table: "influencer_profiles");
        }
    }
}
