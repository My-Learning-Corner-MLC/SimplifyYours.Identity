using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreUserPermissionsAsArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Permissions",
                table: "AspNetUsers",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.Sql(
                """
                UPDATE "AspNetUsers" AS u
                SET "Permissions" = sub.perms
                FROM (
                    SELECT "UserId", array_agg("Permission") AS perms
                    FROM "SimplifyYoursUserPermissions"
                    GROUP BY "UserId"
                ) AS sub
                WHERE u."Id" = sub."UserId";
                """);

            migrationBuilder.DropTable(
                name: "SimplifyYoursUserPermissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SimplifyYoursUserPermissions",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimplifyYoursUserPermissions", x => new { x.UserId, x.Permission });
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimplifyYoursUserPermissions_UserId",
                table: "SimplifyYoursUserPermissions",
                column: "UserId");

            migrationBuilder.Sql(
                """
                INSERT INTO "SimplifyYoursUserPermissions" ("UserId", "Permission")
                SELECT "Id", unnest("Permissions")
                FROM "AspNetUsers"
                WHERE cardinality("Permissions") > 0;
                """);

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "AspNetUsers");
        }
    }
}
