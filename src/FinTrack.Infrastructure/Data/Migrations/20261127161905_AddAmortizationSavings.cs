using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAmortizationSavings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ComparedPayment",
                table: "RecurrenceAmortizations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAfter",
                table: "RecurrenceAmortizations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingBefore",
                table: "RecurrenceAmortizations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Savings",
                table: "RecurrenceAmortizations",
                type: "numeric(19,2)",
                precision: 19,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComparedPayment",
                table: "RecurrenceAmortizations");

            migrationBuilder.DropColumn(
                name: "RemainingAfter",
                table: "RecurrenceAmortizations");

            migrationBuilder.DropColumn(
                name: "RemainingBefore",
                table: "RecurrenceAmortizations");

            migrationBuilder.DropColumn(
                name: "Savings",
                table: "RecurrenceAmortizations");
        }
    }
}
