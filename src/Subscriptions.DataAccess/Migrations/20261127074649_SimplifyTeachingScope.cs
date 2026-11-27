using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subscriptions.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyTeachingScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Subscription_Currency",
                table: "Subscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_Currency",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Payments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Subscriptions",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Subscriptions",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "RUB");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Payments",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "RUB");

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    OccurredAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Subscription_Currency",
                table: "Subscriptions",
                sql: "Currency IN ('RUB','USD','EUR')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_Currency",
                table: "Payments",
                sql: "Currency IN ('RUB','USD','EUR')");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_NormalizedEmail_OccurredAtUtcTicks",
                table: "LoginAttempts",
                columns: new[] { "NormalizedEmail", "OccurredAtUtcTicks" });
        }
    }
}
