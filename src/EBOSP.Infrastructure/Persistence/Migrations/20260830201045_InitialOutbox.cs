using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EBOSP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    AggregateType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AggregateId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_PublishedAt",
                table: "outbox_messages",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_TenantId",
                table: "outbox_messages",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
