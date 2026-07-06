using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RidePR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchesAdminsAndBranchFares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdminType",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Trips",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "EstimatedDurationMinutes",
                table: "Trips",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "EstimatedDistanceKm",
                table: "Trips",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualDistanceKm",
                table: "Trips",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Trips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Passengers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "WaitingMinutePrice",
                table: "FareSettings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerMinute",
                table: "FareSettings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerKm",
                table: "FareSettings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PlatformCommission",
                table: "FareSettings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "FareSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumFare",
                table: "FareSettings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "IncludedDistanceKm",
                table: "FareSettings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "DynamicMultiplier",
                table: "FareSettings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "CancellationFee",
                table: "FareSettings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseFare",
                table: "FareSettings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "FareSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Drivers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId",
                table: "Users",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_BranchId",
                table: "Trips",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DriverId",
                table: "Trips",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_PassengerId",
                table: "Trips",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Status",
                table: "Trips",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_BranchId",
                table: "Passengers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_FareSettings_BranchId_Active",
                table: "FareSettings",
                columns: new[] { "BranchId", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_BranchId",
                table: "Drivers",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Branches_BranchId",
                table: "Drivers",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FareSettings_Branches_BranchId",
                table: "FareSettings",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Passengers_Branches_BranchId",
                table: "Passengers",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Branches_BranchId",
                table: "Trips",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Branches_BranchId",
                table: "Users",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Branches_BranchId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_FareSettings_Branches_BranchId",
                table: "FareSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Passengers_Branches_BranchId",
                table: "Passengers");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Branches_BranchId",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Branches_BranchId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Users_BranchId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Trips_BranchId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_DriverId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_PassengerId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_Status",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Passengers_BranchId",
                table: "Passengers");

            migrationBuilder.DropIndex(
                name: "IX_FareSettings_BranchId_Active",
                table: "FareSettings");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_BranchId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "AdminType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "BaseFare",
                table: "FareSettings");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "FareSettings");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Drivers");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Trips",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "EstimatedDurationMinutes",
                table: "Trips",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "EstimatedDistanceKm",
                table: "Trips",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualDistanceKm",
                table: "Trips",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "WaitingMinutePrice",
                table: "FareSettings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerMinute",
                table: "FareSettings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerKm",
                table: "FareSettings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PlatformCommission",
                table: "FareSettings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "FareSettings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumFare",
                table: "FareSettings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "IncludedDistanceKm",
                table: "FareSettings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "DynamicMultiplier",
                table: "FareSettings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "CancellationFee",
                table: "FareSettings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}
