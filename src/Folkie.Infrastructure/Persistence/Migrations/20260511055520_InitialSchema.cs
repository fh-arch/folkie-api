using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Folkie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brand_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "bank_transfer"),
                    transfer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receipt_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    admin_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    confirmed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brand_payments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "brand_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tax_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    website = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    contact_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    billing_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brand_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campaign_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    influencer_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign_applications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    product_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    product_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    brief = table.Column<string>(type: "text", nullable: false),
                    required_hashtags = table.Column<List<string>>(type: "text[]", nullable: false),
                    content_types = table.Column<int[]>(type: "integer[]", nullable: false),
                    tone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    target_categories = table.Column<List<string>>(type: "text[]", nullable: false),
                    target_cities = table.Column<List<string>>(type: "text[]", nullable: false),
                    content_language = table.Column<List<string>>(type: "text[]", nullable: false),
                    min_followers = table.Column<int>(type: "integer", nullable: false),
                    max_followers = table.Column<int>(type: "integer", nullable: false),
                    influencer_count = table.Column<int>(type: "integer", nullable: false),
                    budget_per_influencer = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    platform_fee_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 15.0m),
                    product_delivery = table.Column<int>(type: "integer", nullable: false),
                    approval_mode = table.Column<int>(type: "integer", nullable: false),
                    application_deadline = table.Column<DateOnly>(type: "date", nullable: false),
                    publish_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    publish_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_flash_campaign = table.Column<bool>(type: "boolean", nullable: false),
                    flash_publish_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaigns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    video_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    external_video_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    script = table.Column<string>(type: "text", nullable: true),
                    hashtags = table.Column<List<string>>(type: "text[]", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    revision_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_submissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "influencer_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tiktok_handle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    tiktok_user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    follower_count = table.Column<int>(type: "integer", nullable: false),
                    engagement_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "TR"),
                    content_language = table.Column<List<string>>(type: "text[]", nullable: false),
                    categories = table.Column<List<string>>(type: "text[]", nullable: false),
                    subcategories = table.Column<List<string>>(type: "text[]", nullable: false),
                    bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    iban = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    iban_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    fake_follower_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    last_tiktok_sync = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_influencer_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    data_json = table.Column<string>(type: "jsonb", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    influencer_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    payment_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    iban = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    iban_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    admin_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    transfer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    approved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    transferred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_role = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    clerk_user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_brand_payments_brand_profile_id",
                table: "brand_payments",
                column: "brand_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_brand_payments_campaign_id",
                table: "brand_payments",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_brand_payments_status",
                table: "brand_payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_brand_profiles_user_id",
                table: "brand_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_campaign_applications_campaign_id_influencer_profile_id",
                table: "campaign_applications",
                columns: new[] { "campaign_id", "influencer_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_campaign_applications_status",
                table: "campaign_applications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_brand_profile_id",
                table: "campaigns",
                column: "brand_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_status",
                table: "campaigns",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_content_submissions_application_id",
                table: "content_submissions",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_submissions_status",
                table: "content_submissions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_influencer_profiles_tiktok_handle",
                table: "influencer_profiles",
                column: "tiktok_handle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_influencer_profiles_tiktok_user_id",
                table: "influencer_profiles",
                column: "tiktok_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_influencer_profiles_user_id",
                table: "influencer_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_is_read",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_campaign_id",
                table: "payments",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_influencer_profile_id",
                table: "payments",
                column: "influencer_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_status",
                table: "payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_campaign_id_reviewer_id",
                table: "reviews",
                columns: new[] { "campaign_id", "reviewer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_clerk_user_id",
                table: "users",
                column: "clerk_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brand_payments");

            migrationBuilder.DropTable(
                name: "brand_profiles");

            migrationBuilder.DropTable(
                name: "campaign_applications");

            migrationBuilder.DropTable(
                name: "campaigns");

            migrationBuilder.DropTable(
                name: "content_submissions");

            migrationBuilder.DropTable(
                name: "influencer_profiles");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
