using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classify.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAudioFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ulong>(
                name: "Hash",
                table: "AudioFiles",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Hash",
                table: "AudioFiles",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER");
        }
    }
}
