using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventorySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Branches_BranchId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Branches",
                table: "Branches");

            migrationBuilder.RenameTable(
                name: "Branches",
                newName: "Branch");

            migrationBuilder.RenameIndex(
                name: "IX_Branches_Code",
                table: "Branch",
                newName: "IX_Branch_Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Branch",
                table: "Branch",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Branch",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 6, 26, 44, 109, DateTimeKind.Utc).AddTicks(1434));

            migrationBuilder.UpdateData(
                table: "Branch",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 6, 26, 44, 109, DateTimeKind.Utc).AddTicks(3193));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 6, 26, 44, 107, DateTimeKind.Utc).AddTicks(6689));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 6, 26, 44, 107, DateTimeKind.Utc).AddTicks(7928));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 6, 26, 44, 108, DateTimeKind.Utc).AddTicks(8984));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 6, 26, 44, 109, DateTimeKind.Utc).AddTicks(561));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 6, 26, 44, 108, DateTimeKind.Utc).AddTicks(6846));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 6, 26, 44, 108, DateTimeKind.Utc).AddTicks(7709));

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Branch_BranchId",
                table: "Products",
                column: "BranchId",
                principalTable: "Branch",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Branch_BranchId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Branch",
                table: "Branch");

            migrationBuilder.RenameTable(
                name: "Branch",
                newName: "Branches");

            migrationBuilder.RenameIndex(
                name: "IX_Branch_Code",
                table: "Branches",
                newName: "IX_Branches_Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Branches",
                table: "Branches",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 3, 50, 31, 992, DateTimeKind.Utc).AddTicks(2383));

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 3, 50, 31, 992, DateTimeKind.Utc).AddTicks(3944));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 3, 50, 31, 990, DateTimeKind.Utc).AddTicks(6604));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 3, 50, 31, 990, DateTimeKind.Utc).AddTicks(7913));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 3, 50, 31, 991, DateTimeKind.Utc).AddTicks(9760));

            migrationBuilder.UpdateData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 3, 50, 31, 992, DateTimeKind.Utc).AddTicks(1339));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 3, 50, 31, 991, DateTimeKind.Utc).AddTicks(7373));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 24, 3, 50, 31, 991, DateTimeKind.Utc).AddTicks(8583));

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Branches_BranchId",
                table: "Products",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }
    }
}
