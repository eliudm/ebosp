using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EBOSP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_stock_ledger_entries_TenantId_OccurredAt",
                table: "stock_ledger_entries",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_TenantId_CreatedAt",
                table: "sales_orders",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_TenantId_CreatedAt",
                table: "purchase_orders",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_TenantId_Status_ConfirmedAt",
                table: "payments",
                columns: new[] { "TenantId", "Status", "ConfirmedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_TenantId_CreatedAt",
                table: "invoices",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_TenantId_Status",
                table: "invoices",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_ledger_entries_TenantId_OccurredAt",
                table: "stock_ledger_entries");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_TenantId_CreatedAt",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_TenantId_CreatedAt",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_payments_TenantId_Status_ConfirmedAt",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_invoices_TenantId_CreatedAt",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_TenantId_Status",
                table: "invoices");
        }
    }
}
