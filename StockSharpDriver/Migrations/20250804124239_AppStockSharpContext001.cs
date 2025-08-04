using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSharpDriver.Migrations
{
    public partial class AppStockSharpContext001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashFlowType",
                table: "CashFlows");

            migrationBuilder.RenameColumn(
                name: "PaymentValue",
                table: "CashFlows",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "PaymentDate",
                table: "CashFlows",
                newName: "Notional");

            migrationBuilder.AddColumn<decimal>(
                name: "Coupon",
                table: "CashFlows",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CouponRate",
                table: "CashFlows",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "CashFlows",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Coupon",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "CouponRate",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "CashFlows");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "CashFlows",
                newName: "PaymentValue");

            migrationBuilder.RenameColumn(
                name: "Notional",
                table: "CashFlows",
                newName: "PaymentDate");

            migrationBuilder.AddColumn<int>(
                name: "CashFlowType",
                table: "CashFlows",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
