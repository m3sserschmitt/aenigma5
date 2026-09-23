using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicesOnDestinationAndUuidForPendingMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Messages_Destination_Sent",
                table: "Messages",
                columns: new[] { "Destination", "Sent" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Uuid",
                table: "Messages",
                column: "Uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_Destination_Sent",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_Uuid",
                table: "Messages");
        }
    }
}
