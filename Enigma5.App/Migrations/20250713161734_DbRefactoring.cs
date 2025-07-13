using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class DbRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateReceived",
                table: "Messages",
                newName: "DateCreated");

            migrationBuilder.AddColumn<long>(
                name: "Timestamp",
                table: "SharedData",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SentTimestamp",
                table: "Messages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Timestamp",
                table: "Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateCreated",
                table: "AuthorizedServices",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "Timestamp",
                table: "AuthorizedServices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "SharedData");

            migrationBuilder.DropColumn(
                name: "SentTimestamp",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "AuthorizedServices");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "AuthorizedServices");

            migrationBuilder.RenameColumn(
                name: "DateCreated",
                table: "Messages",
                newName: "DateReceived");
        }
    }
}
