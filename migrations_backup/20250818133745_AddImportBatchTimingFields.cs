using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventorySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportBatchTimingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ImportBatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInProgress",
                table: "ImportBatches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "ProcessingTimeSeconds",
                table: "ImportBatches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "ImportBatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 18, 13, 37, 44, 556, DateTimeKind.Utc).AddTicks(1394));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 18, 13, 37, 44, 556, DateTimeKind.Utc).AddTicks(2642));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 18, 13, 37, 44, 557, DateTimeKind.Utc).AddTicks(3962));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 18, 13, 37, 44, 557, DateTimeKind.Utc).AddTicks(5549));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 18, 13, 37, 44, 557, DateTimeKind.Utc).AddTicks(1888));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 18, 13, 37, 44, 557, DateTimeKind.Utc).AddTicks(2774));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "IsInProgress",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "ProcessingTimeSeconds",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "ImportBatches");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 16, 6, 19, 22, 79, DateTimeKind.Utc).AddTicks(9390));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 16, 6, 19, 22, 80, DateTimeKind.Utc).AddTicks(628));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 16, 6, 19, 22, 81, DateTimeKind.Utc).AddTicks(2103));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 16, 6, 19, 22, 81, DateTimeKind.Utc).AddTicks(3681));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 16, 6, 19, 22, 81, DateTimeKind.Utc).AddTicks(67));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 16, 6, 19, 22, 81, DateTimeKind.Utc).AddTicks(945));
        }
    }
}
