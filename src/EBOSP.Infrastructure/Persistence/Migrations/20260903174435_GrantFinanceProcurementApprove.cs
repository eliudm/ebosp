using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EBOSP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GrantFinanceProcurementApprove : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111104"), new Guid("22222222-2222-2222-2222-222222222205") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("11111111-1111-1111-1111-111111111104"), new Guid("22222222-2222-2222-2222-222222222205") });
        }
    }
}
