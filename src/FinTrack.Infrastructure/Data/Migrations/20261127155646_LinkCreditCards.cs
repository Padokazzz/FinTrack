using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkCreditCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Accounts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedBankAccountId",
                table: "Accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_LinkedBankAccountId",
                table: "Accounts",
                column: "LinkedBankAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Accounts_LinkedBankAccountId",
                table: "Accounts",
                column: "LinkedBankAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Accounts_LinkedBankAccountId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_LinkedBankAccountId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LinkedBankAccountId",
                table: "Accounts");
        }
    }
}
