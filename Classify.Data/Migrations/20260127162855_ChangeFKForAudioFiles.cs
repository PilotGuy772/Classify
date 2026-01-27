using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classify.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFKForAudioFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AudioFiles_Movements_MovementId",
                table: "AudioFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_AudioFiles_Recordings_RecordingId",
                table: "AudioFiles");

            migrationBuilder.DropIndex(
                name: "IX_AudioFiles_MovementId",
                table: "AudioFiles");

            migrationBuilder.DropIndex(
                name: "IX_AudioFiles_RecordingId",
                table: "AudioFiles");

            migrationBuilder.DropColumn(
                name: "MovementId",
                table: "AudioFiles");

            migrationBuilder.DropColumn(
                name: "RecordingId",
                table: "AudioFiles");

            migrationBuilder.AddColumn<int>(
                name: "AudioFileId",
                table: "PerformedMovement",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PerformedMovement_AudioFileId",
                table: "PerformedMovement",
                column: "AudioFileId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformedMovement_AudioFiles_AudioFileId",
                table: "PerformedMovement",
                column: "AudioFileId",
                principalTable: "AudioFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerformedMovement_AudioFiles_AudioFileId",
                table: "PerformedMovement");

            migrationBuilder.DropIndex(
                name: "IX_PerformedMovement_AudioFileId",
                table: "PerformedMovement");

            migrationBuilder.DropColumn(
                name: "AudioFileId",
                table: "PerformedMovement");

            migrationBuilder.AddColumn<int>(
                name: "MovementId",
                table: "AudioFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecordingId",
                table: "AudioFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_MovementId",
                table: "AudioFiles",
                column: "MovementId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_RecordingId",
                table: "AudioFiles",
                column: "RecordingId");

            migrationBuilder.AddForeignKey(
                name: "FK_AudioFiles_Movements_MovementId",
                table: "AudioFiles",
                column: "MovementId",
                principalTable: "Movements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AudioFiles_Recordings_RecordingId",
                table: "AudioFiles",
                column: "RecordingId",
                principalTable: "Recordings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
