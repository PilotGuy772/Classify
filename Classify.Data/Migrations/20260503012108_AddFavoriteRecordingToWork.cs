using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classify.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteRecordingToWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FavoriteRecordingId",
                table: "Works",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Works_FavoriteRecordingId",
                table: "Works",
                column: "FavoriteRecordingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Works_Recordings_FavoriteRecordingId",
                table: "Works",
                column: "FavoriteRecordingId",
                principalTable: "Recordings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Works_Recordings_FavoriteRecordingId",
                table: "Works");

            migrationBuilder.DropIndex(
                name: "IX_Works_FavoriteRecordingId",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "FavoriteRecordingId",
                table: "Works");
        }
    }
}
