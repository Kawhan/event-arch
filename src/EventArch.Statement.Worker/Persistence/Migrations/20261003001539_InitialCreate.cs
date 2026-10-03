using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventArch.Statement.Worker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "statement");

            migrationBuilder.CreateTable(
                name: "statement_entries",
                schema: "statement",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statement_entries", x => x.event_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_statement_entries_account_occurred",
                schema: "statement",
                table: "statement_entries",
                columns: new[] { "account_id", "occurred_on_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "statement_entries",
                schema: "statement");
        }
    }
}
