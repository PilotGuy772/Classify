using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classify.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnsembleSoloistYearToProposedMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnsembleName",
                table: "ProposedMatch",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecordingYear",
                table: "ProposedMatch",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoloistName",
                table: "ProposedMatch",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnsembleName",
                table: "ProposedMatch");

            migrationBuilder.DropColumn(
                name: "RecordingYear",
                table: "ProposedMatch");

            migrationBuilder.DropColumn(
                name: "SoloistName",
                table: "ProposedMatch");
        }
    }
}
