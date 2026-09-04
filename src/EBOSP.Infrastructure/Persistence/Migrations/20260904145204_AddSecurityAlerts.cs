using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EBOSP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "security_alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rule = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RelatedActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedAggregateType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RelatedAggregateId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_alerts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_TenantId_ActorId",
                table: "outbox_messages",
                columns: new[] { "TenantId", "ActorId" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_TenantId_OccurredAt",
                table: "outbox_messages",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_security_alerts_TenantId",
                table: "security_alerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_security_alerts_TenantId_Status",
                table: "security_alerts",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "security_alerts");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_TenantId_ActorId",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_TenantId_OccurredAt",
                table: "outbox_messages");
        }
    }
}
