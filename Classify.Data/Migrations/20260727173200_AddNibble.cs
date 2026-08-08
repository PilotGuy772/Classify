using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classify.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNibble : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nibbles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordingId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nibbles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nibbles_Recordings_RecordingId",
                        column: x => x.RecordingId,
                        principalTable: "Recordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nibbles_Works_WorkId",
                        column: x => x.WorkId,
                        principalTable: "Works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NibbleMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NibbleId = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NibbleMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NibbleMovements_Movements_MovementId",
                        column: x => x.MovementId,
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NibbleMovements_Nibbles_NibbleId",
                        column: x => x.NibbleId,
                        principalTable: "Nibbles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NibbleMovements_MovementId",
                table: "NibbleMovements",
                column: "MovementId");

            migrationBuilder.CreateIndex(
                name: "IX_NibbleMovements_NibbleId_Order",
                table: "NibbleMovements",
                columns: new[] { "NibbleId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Nibbles_RecordingId",
                table: "Nibbles",
                column: "RecordingId");

            migrationBuilder.CreateIndex(
                name: "IX_Nibbles_WorkId",
                table: "Nibbles",
                column: "WorkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NibbleMovements");

            migrationBuilder.DropTable(
                name: "Nibbles");
        }
    }
}
