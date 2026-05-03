using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classify.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeUserInputtedMatchIntoProposedMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComposerId",
                table: "ProposedMatch",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MovementId",
                table: "ProposedMatch",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PerformanceOrder",
                table: "ProposedMatch",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecordingId",
                table: "ProposedMatch",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkId",
                table: "ProposedMatch",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProposedMatch_ComposerId",
                table: "ProposedMatch",
                column: "ComposerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedMatch_MovementId",
                table: "ProposedMatch",
                column: "MovementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedMatch_RecordingId",
                table: "ProposedMatch",
                column: "RecordingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedMatch_WorkId",
                table: "ProposedMatch",
                column: "WorkId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProposedMatch_Composers_ComposerId",
                table: "ProposedMatch",
                column: "ComposerId",
                principalTable: "Composers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProposedMatch_Movements_MovementId",
                table: "ProposedMatch",
                column: "MovementId",
                principalTable: "Movements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProposedMatch_Recordings_RecordingId",
                table: "ProposedMatch",
                column: "RecordingId",
                principalTable: "Recordings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProposedMatch_Works_WorkId",
                table: "ProposedMatch",
                column: "WorkId",
                principalTable: "Works",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProposedMatch_Composers_ComposerId",
                table: "ProposedMatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ProposedMatch_Movements_MovementId",
                table: "ProposedMatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ProposedMatch_Recordings_RecordingId",
                table: "ProposedMatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ProposedMatch_Works_WorkId",
                table: "ProposedMatch");

            migrationBuilder.DropIndex(
                name: "IX_ProposedMatch_ComposerId",
                table: "ProposedMatch");

            migrationBuilder.DropIndex(
                name: "IX_ProposedMatch_MovementId",
                table: "ProposedMatch");

            migrationBuilder.DropIndex(
                name: "IX_ProposedMatch_RecordingId",
                table: "ProposedMatch");

            migrationBuilder.DropIndex(
                name: "IX_ProposedMatch_WorkId",
                table: "ProposedMatch");

            migrationBuilder.DropColumn(
                name: "ComposerId",
                table: "ProposedMatch");

            migrationBuilder.DropColumn(
                name: "MovementId",
                table: "ProposedMatch");

            migrationBuilder.DropColumn(
                name: "PerformanceOrder",
                table: "ProposedMatch");

            migrationBuilder.DropColumn(
                name: "RecordingId",
                table: "ProposedMatch");

            migrationBuilder.DropColumn(
                name: "WorkId",
                table: "ProposedMatch");
        }
    }
}
