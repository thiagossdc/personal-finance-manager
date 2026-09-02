using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalFinance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "RecurringTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "FinancialGoals",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Budgets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "FinancialGoals");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Budgets");
        }
    }
}
