using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classify.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAudioFilePerformedMovementStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PerformedMovement_AudioFileId",
                table: "PerformedMovement");

            migrationBuilder.CreateIndex(
                name: "IX_PerformedMovement_AudioFileId",
                table: "PerformedMovement",
                column: "AudioFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PerformedMovement_AudioFileId",
                table: "PerformedMovement");

            migrationBuilder.CreateIndex(
                name: "IX_PerformedMovement_AudioFileId",
                table: "PerformedMovement",
                column: "AudioFileId",
                unique: true);
        }
    }
}
