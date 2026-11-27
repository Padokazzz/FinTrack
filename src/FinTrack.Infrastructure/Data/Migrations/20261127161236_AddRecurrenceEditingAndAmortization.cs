using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurrenceEditingAndAmortization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OriginalAccountId",
                table: "RecurringOccurrences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "RecurringOccurrences",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalCategoryId",
                table: "RecurringOccurrences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalDescription",
                table: "RecurringOccurrences",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalPaymentMethod",
                table: "RecurringOccurrences",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalType",
                table: "RecurringOccurrences",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecurrenceAmortizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Installments = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrenceAmortizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurrenceAmortizations_RecurringRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "RecurringRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringOccurrences_OriginalAccountId",
                table: "RecurringOccurrences",
                column: "OriginalAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringOccurrences_OriginalCategoryId",
                table: "RecurringOccurrences",
                column: "OriginalCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurrenceAmortizations_RuleId",
                table: "RecurrenceAmortizations",
                column: "RuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringOccurrences_Accounts_OriginalAccountId",
                table: "RecurringOccurrences",
                column: "OriginalAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringOccurrences_Categories_OriginalCategoryId",
                table: "RecurringOccurrences",
                column: "OriginalCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringOccurrences_Accounts_OriginalAccountId",
                table: "RecurringOccurrences");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringOccurrences_Categories_OriginalCategoryId",
                table: "RecurringOccurrences");

            migrationBuilder.DropTable(
                name: "RecurrenceAmortizations");

            migrationBuilder.DropIndex(
                name: "IX_RecurringOccurrences_OriginalAccountId",
                table: "RecurringOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_RecurringOccurrences_OriginalCategoryId",
                table: "RecurringOccurrences");

            migrationBuilder.DropColumn(
                name: "OriginalAccountId",
                table: "RecurringOccurrences");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "RecurringOccurrences");

            migrationBuilder.DropColumn(
                name: "OriginalCategoryId",
                table: "RecurringOccurrences");

            migrationBuilder.DropColumn(
                name: "OriginalDescription",
                table: "RecurringOccurrences");

            migrationBuilder.DropColumn(
                name: "OriginalPaymentMethod",
                table: "RecurringOccurrences");

            migrationBuilder.DropColumn(
                name: "OriginalType",
                table: "RecurringOccurrences");
        }
    }
}
