using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace RidePR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnablePostGIS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Longitude",
                table: "DriverLocations",
                newName: "Speed");

            migrationBuilder.RenameColumn(
                name: "Latitude",
                table: "DriverLocations",
                newName: "Heading");

            migrationBuilder.RenameColumn(
                name: "LastUpdate",
                table: "DriverLocations",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "IsOnline",
                table: "DriverLocations",
                newName: "Online");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<Point>(
                name: "Position",
                table: "DriverLocations",
                type: "geometry(Point,4326)",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_DriverLocations_DriverId",
                table: "DriverLocations",
                column: "DriverId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DriverLocations_DriverId",
                table: "DriverLocations");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "DriverLocations");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "DriverLocations",
                newName: "LastUpdate");

            migrationBuilder.RenameColumn(
                name: "Speed",
                table: "DriverLocations",
                newName: "Longitude");

            migrationBuilder.RenameColumn(
                name: "Online",
                table: "DriverLocations",
                newName: "IsOnline");

            migrationBuilder.RenameColumn(
                name: "Heading",
                table: "DriverLocations",
                newName: "Latitude");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
