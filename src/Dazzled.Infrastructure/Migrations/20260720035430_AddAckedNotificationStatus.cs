using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dazzled.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAckedNotificationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "NotificationStatuses",
                columns: new[] { "Id", "Name" },
                values: new object[] { 4, "Acked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NotificationStatuses",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
