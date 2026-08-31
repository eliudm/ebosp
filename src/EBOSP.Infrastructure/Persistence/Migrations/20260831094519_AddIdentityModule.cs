using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EBOSP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByIp = table.Column<string>(type: "text", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.RoleId, x.PermissionId });
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PrimaryBranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FailedLoginCount = table.Column<int>(type: "integer", nullable: false),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "inventory.read", "Read inventory balances and ledger." },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "inventory.adjust", "Adjust stock balances." },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "procurement.create", "Create purchase requests." },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "procurement.approve", "Approve purchase requests." },
                    { new Guid("11111111-1111-1111-1111-111111111105"), "payment.create", "Record payments." },
                    { new Guid("11111111-1111-1111-1111-111111111106"), "security.alert.manage", "Manage security alerts." },
                    { new Guid("11111111-1111-1111-1111-111111111107"), "audit.read", "Read audit events." },
                    { new Guid("11111111-1111-1111-1111-111111111108"), "user.manage", "Manage users, roles and tenant administration." }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), new Guid("22222222-2222-2222-2222-222222222201") },
                    { new Guid("11111111-1111-1111-1111-111111111102"), new Guid("22222222-2222-2222-2222-222222222201") },
                    { new Guid("11111111-1111-1111-1111-111111111103"), new Guid("22222222-2222-2222-2222-222222222201") },
                    { new Guid("11111111-1111-1111-1111-111111111104"), new Guid("22222222-2222-2222-2222-222222222201") },
                    { new Guid("11111111-1111-1111-1111-111111111105"), new Guid("22222222-2222-2222-2222-222222222201") },
                    { new Guid("11111111-1111-1111-1111-111111111106"), new Guid("22222222-2222-2222-2222-222222222201") },
                    { new Guid("11111111-1111-1111-1111-111111111107"), new Guid("22222222-2222-2222-2222-222222222201") },
                    { new Guid("11111111-1111-1111-1111-111111111108"), new Guid("22222222-2222-2222-2222-222222222201") },
                    { new Guid("11111111-1111-1111-1111-111111111101"), new Guid("22222222-2222-2222-2222-222222222202") },
                    { new Guid("11111111-1111-1111-1111-111111111102"), new Guid("22222222-2222-2222-2222-222222222202") },
                    { new Guid("11111111-1111-1111-1111-111111111103"), new Guid("22222222-2222-2222-2222-222222222203") },
                    { new Guid("11111111-1111-1111-1111-111111111104"), new Guid("22222222-2222-2222-2222-222222222204") },
                    { new Guid("11111111-1111-1111-1111-111111111105"), new Guid("22222222-2222-2222-2222-222222222205") },
                    { new Guid("11111111-1111-1111-1111-111111111106"), new Guid("22222222-2222-2222-2222-222222222206") },
                    { new Guid("11111111-1111-1111-1111-111111111107"), new Guid("22222222-2222-2222-2222-222222222207") }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222201"), "tenant-admin", "Tenant Admin" },
                    { new Guid("22222222-2222-2222-2222-222222222202"), "warehouse-manager", "Warehouse Manager" },
                    { new Guid("22222222-2222-2222-2222-222222222203"), "procurement", "Procurement" },
                    { new Guid("22222222-2222-2222-2222-222222222204"), "manager", "Manager" },
                    { new Guid("22222222-2222-2222-2222-222222222205"), "finance", "Finance" },
                    { new Guid("22222222-2222-2222-2222-222222222206"), "security-officer", "Security Officer" },
                    { new Guid("22222222-2222-2222-2222-222222222207"), "auditor", "Auditor" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_branches_TenantId",
                table: "branches",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Code",
                table: "permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TenantId",
                table: "refresh_tokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_Code",
                table: "roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_TenantId_UserId",
                table: "user_roles",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_UserId_RoleId_BranchId",
                table: "user_roles",
                columns: new[] { "UserId", "RoleId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_TenantId",
                table: "users",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
