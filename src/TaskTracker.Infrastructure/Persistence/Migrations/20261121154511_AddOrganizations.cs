using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Hand-written: the scaffolded version deleted and re-inserted the seeded projects, which
    /// would have cascaded into their tasks. This one keeps all existing data, moves it into the
    /// Default Organization, and gives every current user equivalent access.
    /// </summary>
    public partial class AddOrganizations : Migration
    {
        private const string SystemUserId = "3c0f6e1a-8d42-4b7e-9f55-6a2b1c7d9e01";
        private const string SuperAdminRoleId = "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a04";
        private const string OldViewerRoleId = "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a01";
        private const string OldMemberRoleId = "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a02";
        private const string OldAdminRoleId = "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a03";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMembers",
                columns: table => new
                {
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMembers", x => new { x.OrganizationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { SuperAdminRoleId, SuperAdminRoleId, "SuperAdmin", "SUPERADMIN" });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { SystemUserId, 0, SystemUserId, "system@tasktracker.local", false, true, new DateTimeOffset(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), new TimeSpan(0, 0, 0, 0, 0)), "SYSTEM@TASKTRACKER.LOCAL", "SYSTEM@TASKTRACKER.LOCAL", null, null, false, SystemUserId, false, "system@tasktracker.local" });

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Name" },
                values: new object[] { 1, new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc), true, "Default Organization" });

            migrationBuilder.InsertData(
                table: "OrganizationMembers",
                columns: new[] { "OrganizationId", "UserId", "Role" },
                values: new object[] { 1, SystemUserId, "Admin" });

            // Existing Admins stay Admins. Everyone else becomes an organization Member.
            migrationBuilder.Sql($"""
                INSERT INTO [OrganizationMembers] ([OrganizationId], [UserId], [Role])
                SELECT 1, u.[Id],
                       CASE WHEN EXISTS (SELECT 1 FROM [AspNetUserRoles] ur
                                         WHERE ur.[UserId] = u.[Id] AND ur.[RoleId] = '{OldAdminRoleId}')
                            THEN 'Admin' ELSE 'Member' END
                FROM [AspNetUsers] u
                WHERE u.[Id] <> '{SystemUserId}';
                """);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "Projects",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "NotificationLog",
                type: "int",
                nullable: true);

            // Existing projects go to an active Admin, or to the system user if there is none.
            migrationBuilder.Sql($"""
                DECLARE @owner nvarchar(450) = (
                    SELECT TOP 1 u.[Id]
                    FROM [AspNetUsers] u
                    JOIN [AspNetUserRoles] ur ON ur.[UserId] = u.[Id] AND ur.[RoleId] = '{OldAdminRoleId}'
                    WHERE u.[LockoutEnd] IS NULL OR u.[LockoutEnd] <= SYSDATETIMEOFFSET()
                    ORDER BY CASE WHEN u.[NormalizedEmail] = 'ADMIN@TASKTRACKER.LOCAL' THEN 0 ELSE 1 END, u.[Id]);

                UPDATE [Projects] SET [OrganizationId] = 1, [OwnerUserId] = COALESCE(@owner, '{SystemUserId}');
                UPDATE [NotificationLog] SET [OrganizationId] = 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "Projects",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerUserId",
                table: "Projects",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "NotificationLog",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Projects_OrganizationId_Id",
                table: "Projects",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ProjectMembers_OrganizationMembers_OrganizationId_UserId",
                        columns: x => new { x.OrganizationId, x.UserId },
                        principalTable: "OrganizationMembers",
                        principalColumns: new[] { "OrganizationId", "UserId" });
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_OrganizationId_ProjectId",
                        columns: x => new { x.OrganizationId, x.ProjectId },
                        principalTable: "Projects",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            // Previous global access becomes project access, so nobody loses what they could do.
            migrationBuilder.Sql($"""
                INSERT INTO [ProjectMembers] ([ProjectId], [UserId], [OrganizationId], [Role], [AddedAtUtc])
                SELECT p.[Id], u.[Id], p.[OrganizationId],
                       CASE WHEN EXISTS (SELECT 1 FROM [AspNetUserRoles] ur
                                         WHERE ur.[UserId] = u.[Id]
                                           AND ur.[RoleId] IN ('{OldAdminRoleId}', '{OldMemberRoleId}'))
                            THEN 'Contributor' ELSE 'Viewer' END,
                       SYSUTCDATETIME()
                FROM [Projects] p
                CROSS JOIN [AspNetUsers] u
                WHERE u.[Id] <> '{SystemUserId}' AND u.[Id] <> p.[OwnerUserId];
                """);

            // Existing tokens carry the old role model, so every user must sign in again.
            migrationBuilder.Sql($"""
                UPDATE [AspNetUsers] SET [SecurityStamp] = CONVERT(nvarchar(36), NEWID())
                WHERE [Id] <> '{SystemUserId}';
                """);

            migrationBuilder.DeleteData(table: "AspNetRoles", keyColumn: "Id", keyValue: OldViewerRoleId);
            migrationBuilder.DeleteData(table: "AspNetRoles", keyColumn: "Id", keyValue: OldMemberRoleId);
            migrationBuilder.DeleteData(table: "AspNetRoles", keyColumn: "Id", keyValue: OldAdminRoleId);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganizationId_OwnerUserId",
                table: "Projects",
                columns: new[] { "OrganizationId", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_UserId",
                table: "OrganizationMembers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_OrganizationId_ProjectId",
                table: "ProjectMembers",
                columns: new[] { "OrganizationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_OrganizationId_UserId",
                table: "ProjectMembers",
                columns: new[] { "OrganizationId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_OrganizationMembers_OrganizationId_OwnerUserId",
                table: "Projects",
                columns: new[] { "OrganizationId", "OwnerUserId" },
                principalTable: "OrganizationMembers",
                principalColumns: new[] { "OrganizationId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Organizations_OrganizationId",
                table: "Projects",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { OldViewerRoleId, OldViewerRoleId, "Viewer", "VIEWER" },
                    { OldMemberRoleId, OldMemberRoleId, "Member", "MEMBER" },
                    { OldAdminRoleId, OldAdminRoleId, "Admin", "ADMIN" }
                });

            // Maps access back to one global role. Owners count as Members at minimum.
            migrationBuilder.Sql($"""
                INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
                SELECT om.[UserId],
                       CASE WHEN om.[Role] = 'Admin' THEN '{OldAdminRoleId}'
                            WHEN EXISTS (SELECT 1 FROM [ProjectMembers] pm
                                         WHERE pm.[UserId] = om.[UserId] AND pm.[Role] = 'Contributor')
                              OR EXISTS (SELECT 1 FROM [Projects] p WHERE p.[OwnerUserId] = om.[UserId])
                            THEN '{OldMemberRoleId}'
                            ELSE '{OldViewerRoleId}' END
                FROM [OrganizationMembers] om
                WHERE om.[UserId] <> '{SystemUserId}';

                UPDATE [AspNetUsers] SET [SecurityStamp] = CONVERT(nvarchar(36), NEWID())
                WHERE [Id] <> '{SystemUserId}';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_OrganizationMembers_OrganizationId_OwnerUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Organizations_OrganizationId",
                table: "Projects");

            migrationBuilder.DropTable(name: "ProjectMembers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Projects_OrganizationId_Id",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OrganizationId_OwnerUserId",
                table: "Projects");

            migrationBuilder.DropColumn(name: "OrganizationId", table: "Projects");
            migrationBuilder.DropColumn(name: "OwnerUserId", table: "Projects");
            migrationBuilder.DropColumn(name: "OrganizationId", table: "NotificationLog");

            migrationBuilder.DropTable(name: "OrganizationMembers");
            migrationBuilder.DropTable(name: "Organizations");

            migrationBuilder.DeleteData(table: "AspNetUsers", keyColumn: "Id", keyValue: SystemUserId);
            migrationBuilder.DeleteData(table: "AspNetRoles", keyColumn: "Id", keyValue: SuperAdminRoleId);
        }
    }
}
