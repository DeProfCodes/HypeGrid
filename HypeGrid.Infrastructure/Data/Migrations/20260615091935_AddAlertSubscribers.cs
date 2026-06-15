using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HypeGrid.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertSubscribers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertSubscribers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Interests = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FrequencyPreference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourcePage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ConsentGiven = table.Column<bool>(type: "bit", nullable: false),
                    ConsentTextVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConsentSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConsentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsentIpHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ConsentUserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ChannelJoinClicked = table.Column<bool>(type: "bit", nullable: false),
                    ChannelJoinClickedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DirectMessageOptIn = table.Column<bool>(type: "bit", nullable: false),
                    UnsubscribedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertSubscribers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertSubscribers_ChannelJoinClicked",
                table: "AlertSubscribers",
                column: "ChannelJoinClicked");

            migrationBuilder.CreateIndex(
                name: "IX_AlertSubscribers_CreatedDate",
                table: "AlertSubscribers",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_AlertSubscribers_Email",
                table: "AlertSubscribers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AlertSubscribers_PhoneNumber",
                table: "AlertSubscribers",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AlertSubscribers_Status",
                table: "AlertSubscribers",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertSubscribers");
        }
    }
}
