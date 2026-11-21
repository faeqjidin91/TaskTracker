using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a01", "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a01", "Viewer", "VIEWER" },
                    { "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a02", "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a02", "Member", "MEMBER" },
                    { "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a03", "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a03", "Admin", "ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a01");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a02");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a03");
        }
    }
}
