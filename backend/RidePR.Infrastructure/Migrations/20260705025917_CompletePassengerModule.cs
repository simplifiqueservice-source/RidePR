using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RidePR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompletePassengerModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Passengers");

            migrationBuilder.RenameColumn(
                name: "CPF",
                table: "Passengers",
                newName: "Cpf");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Passengers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Cpf",
                table: "Passengers",
                type: "character varying(14)",
                maxLength: 14,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Passengers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Passengers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Passengers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyPhone",
                table: "Passengers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Passengers",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Passengers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Passengers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Passengers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_Cpf",
                table: "Passengers",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_UserId",
                table: "Passengers",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Passengers_Users_UserId",
                table: "Passengers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Passengers_Users_UserId",
                table: "Passengers");

            migrationBuilder.DropIndex(
                name: "IX_Passengers_Cpf",
                table: "Passengers");

            migrationBuilder.DropIndex(
                name: "IX_Passengers_UserId",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "EmergencyPhone",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Passengers");

            migrationBuilder.RenameColumn(
                name: "Cpf",
                table: "Passengers",
                newName: "CPF");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Passengers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "CPF",
                table: "Passengers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(14)",
                oldMaxLength: 14);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Passengers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Passengers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
