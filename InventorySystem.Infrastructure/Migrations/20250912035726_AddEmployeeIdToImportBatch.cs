using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventorySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeIdToImportBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "ImportBatches",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 732, DateTimeKind.Utc).AddTicks(4238));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 732, DateTimeKind.Utc).AddTicks(5840));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 733, DateTimeKind.Utc).AddTicks(6719));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 733, DateTimeKind.Utc).AddTicks(8407));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 733, DateTimeKind.Utc).AddTicks(4637));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 12, 3, 57, 25, 733, DateTimeKind.Utc).AddTicks(5516));

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_EmployeeId",
                table: "ImportBatches",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportBatches_Employees_EmployeeId",
                table: "ImportBatches",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportBatches_Employees_EmployeeId",
                table: "ImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_ImportBatches_EmployeeId",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "ImportBatches");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 3, 54, 29, 654, DateTimeKind.Utc).AddTicks(9589));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 3, 54, 29, 655, DateTimeKind.Utc).AddTicks(1011));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 3, 54, 29, 656, DateTimeKind.Utc).AddTicks(9903));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 3, 54, 29, 657, DateTimeKind.Utc).AddTicks(2322));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 3, 54, 29, 656, DateTimeKind.Utc).AddTicks(6801));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 3, 54, 29, 656, DateTimeKind.Utc).AddTicks(7977));
        }
    }
}
