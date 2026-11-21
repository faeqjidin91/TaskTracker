using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Hand-written: the scaffolded version deleted the seeded rows unconditionally. Demo data now
    /// comes from the Development seeder, so this removes it only where nobody has used it.
    /// A project that was transferred, has members, or has extra tasks is kept.
    /// </summary>
    public partial class RemoveDemoSeedData : Migration
    {
        private const string SystemUserId = "3c0f6e1a-8d42-4b7e-9f55-6a2b1c7d9e01";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deleting a project cascades to its seeded tasks.
            migrationBuilder.Sql($"""
                DELETE p FROM [Projects] p
                WHERE p.[Id] IN (1, 2)
                  AND p.[OwnerUserId] = '{SystemUserId}'
                  AND NOT EXISTS (SELECT 1 FROM [ProjectMembers] m WHERE m.[ProjectId] = p.[Id])
                  AND NOT EXISTS (SELECT 1 FROM [Tasks] t WHERE t.[ProjectId] = p.[Id] AND t.[Id] NOT IN (1, 2, 3, 4, 5));
                """);

            migrationBuilder.Sql($"""
                IF NOT EXISTS (SELECT 1 FROM [Projects] WHERE [OwnerUserId] = '{SystemUserId}')
                   AND NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [UserId] = '{SystemUserId}')
                BEGIN
                    DELETE FROM [OrganizationMembers] WHERE [UserId] = '{SystemUserId}';
                    DELETE FROM [AspNetUsers] WHERE [Id] = '{SystemUserId}';
                END
                """);

            migrationBuilder.Sql("""
                DELETE o FROM [Organizations] o
                WHERE o.[Id] = 1
                  AND NOT EXISTS (SELECT 1 FROM [OrganizationMembers] m WHERE m.[OrganizationId] = o.[Id])
                  AND NOT EXISTS (SELECT 1 FROM [Projects] p WHERE p.[OrganizationId] = o.[Id]);
                """);

            // A fresh database starts its real data at id 1.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Tasks]) DBCC CHECKIDENT ('[Tasks]', RESEED, 0);
                IF NOT EXISTS (SELECT 1 FROM [Projects]) DBCC CHECKIDENT ('[Projects]', RESEED, 0);
                IF NOT EXISTS (SELECT 1 FROM [Organizations]) DBCC CHECKIDENT ('[Organizations]', RESEED, 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores only the rows that are missing, so existing data is never overwritten.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Organizations] WHERE [Id] = 1 OR [Name] = N'Default Organization')
                BEGIN
                    SET IDENTITY_INSERT [Organizations] ON;
                    INSERT INTO [Organizations] ([Id], [Name], [IsActive], [CreatedAtUtc])
                    VALUES (1, N'Default Organization', 1, '2026-01-05T09:00:00');
                    SET IDENTITY_INSERT [Organizations] OFF;
                END
                """);

            migrationBuilder.Sql($"""
                IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = '{SystemUserId}')
                INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed], [LockoutEnabled],
                    [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PhoneNumberConfirmed], [SecurityStamp], [TwoFactorEnabled], [UserName])
                VALUES ('{SystemUserId}', 0, '{SystemUserId}', N'system@tasktracker.local', 0, 1,
                    '9999-12-31T23:59:59.9999999+00:00', N'SYSTEM@TASKTRACKER.LOCAL', N'SYSTEM@TASKTRACKER.LOCAL', 0, '{SystemUserId}', 0, N'system@tasktracker.local');

                IF EXISTS (SELECT 1 FROM [Organizations] WHERE [Id] = 1)
                   AND NOT EXISTS (SELECT 1 FROM [OrganizationMembers] WHERE [UserId] = '{SystemUserId}')
                INSERT INTO [OrganizationMembers] ([OrganizationId], [UserId], [Role]) VALUES (1, '{SystemUserId}', 'Admin');
                """);

            migrationBuilder.Sql($"""
                IF EXISTS (SELECT 1 FROM [OrganizationMembers] WHERE [OrganizationId] = 1 AND [UserId] = '{SystemUserId}')
                BEGIN
                    SET IDENTITY_INSERT [Projects] ON;
                    INSERT INTO [Projects] ([Id], [OrganizationId], [OwnerUserId], [Name], [Description], [CreatedAtUtc])
                    SELECT v.[Id], 1, '{SystemUserId}', v.[Name], v.[Description], '2026-01-05T09:00:00'
                    FROM (VALUES
                        (1, N'Website Redesign', N'Refresh the marketing site with a new design system.'),
                        (2, N'Mobile App v2', N'Second major release of the mobile app.')) v ([Id], [Name], [Description])
                    WHERE NOT EXISTS (SELECT 1 FROM [Projects] p WHERE p.[Id] = v.[Id]);
                    SET IDENTITY_INSERT [Projects] OFF;

                    SET IDENTITY_INSERT [Tasks] ON;
                    INSERT INTO [Tasks] ([Id], [ProjectId], [Title], [Status], [Priority], [DueDateUtc], [CreatedAtUtc], [UpdatedAtUtc])
                    SELECT v.[Id], v.[ProjectId], v.[Title], v.[Status], v.[Priority], v.[DueDateUtc], '2026-01-05T09:00:00', '2026-01-05T09:00:00'
                    FROM (VALUES
                        (1, 1, N'Audit existing pages', 'Done', 'Medium', CAST(NULL AS datetime2)),
                        (2, 1, N'Build new landing page', 'InProgress', 'High', CAST('2026-12-15' AS datetime2)),
                        (3, 1, N'Migrate blog content', 'Todo', 'Low', CAST(NULL AS datetime2)),
                        (4, 2, N'Design onboarding flow', 'InProgress', 'Medium', CAST('2026-12-20' AS datetime2)),
                        (5, 2, N'Add push notifications', 'Todo', 'High', CAST(NULL AS datetime2)))
                        v ([Id], [ProjectId], [Title], [Status], [Priority], [DueDateUtc])
                    WHERE NOT EXISTS (SELECT 1 FROM [Tasks] t WHERE t.[Id] = v.[Id])
                      AND EXISTS (SELECT 1 FROM [Projects] p WHERE p.[Id] = v.[ProjectId] AND p.[OwnerUserId] = '{SystemUserId}');
                    SET IDENTITY_INSERT [Tasks] OFF;
                END
                """);
        }
    }
}
