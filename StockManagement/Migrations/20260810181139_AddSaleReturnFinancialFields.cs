using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManagement.Migrations
{
    public partial class AddSaleReturnFinancialFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                table: "Sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetProfit",
                table: "Sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundDueAmount",
                table: "Sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedAmount",
                table: "Sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedProfitAmount",
                table: "Sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE "Sales"
                SET
                    "ReturnedAmount" = 0,
                    "NetAmount" = "TotalAmount",
                    "RefundDueAmount" = 0,
                    "ReturnedProfitAmount" = 0,
                    "NetProfit" = "TotalProfit";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "NetProfit",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "RefundDueAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ReturnedAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ReturnedProfitAmount",
                table: "Sales");
        }
    }
}