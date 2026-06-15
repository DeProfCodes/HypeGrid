using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HypeGrid.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_Group",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_Key",
                table: "SiteSettings");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_Group_Key",
                table: "SiteSettings",
                columns: new[] { "Group", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_Group_Key",
                table: "SiteSettings");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_Group",
                table: "SiteSettings",
                column: "Group");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_Key",
                table: "SiteSettings",
                column: "Key",
                unique: true);
        }
    }
}
