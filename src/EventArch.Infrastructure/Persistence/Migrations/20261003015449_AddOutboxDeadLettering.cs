using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventArch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxDeadLettering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_pending",
                table: "outbox_messages");

            migrationBuilder.AddColumn<int>(
                name: "attempts",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "dead_lettered_on_utc",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                table: "outbox_messages",
                column: "occurred_on_utc",
                filter: "processed_on_utc IS NULL AND dead_lettered_on_utc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_pending",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "attempts",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "dead_lettered_on_utc",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                table: "outbox_messages",
                column: "occurred_on_utc",
                filter: "processed_on_utc IS NULL");
        }
    }
}
