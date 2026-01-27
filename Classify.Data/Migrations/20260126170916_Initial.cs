using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classify.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Composers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Composers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Works",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComposerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CatalogNumber = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Works", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Works_Composers_ComposerId",
                        column: x => x.ComposerId,
                        principalTable: "Composers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Movements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movements_Works_WorkId",
                        column: x => x.WorkId,
                        principalTable: "Works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recordings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkId = table.Column<int>(type: "INTEGER", nullable: false),
                    Conductor = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recordings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recordings_Works_WorkId",
                        column: x => x.WorkId,
                        principalTable: "Works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AudioFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    RecordingId = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudioFiles_Movements_MovementId",
                        column: x => x.MovementId,
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AudioFiles_Recordings_RecordingId",
                        column: x => x.RecordingId,
                        principalTable: "Recordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovementRecording",
                columns: table => new
                {
                    MovementId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordingId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovementRecording", x => new { x.MovementId, x.RecordingId });
                    table.ForeignKey(
                        name: "FK_MovementRecording_Movements_MovementId",
                        column: x => x.MovementId,
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovementRecording_Recordings_RecordingId",
                        column: x => x.RecordingId,
                        principalTable: "Recordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerformedMovement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecordingId = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformedMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformedMovement_Movements_MovementId",
                        column: x => x.MovementId,
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerformedMovement_Recordings_RecordingId",
                        column: x => x.RecordingId,
                        principalTable: "Recordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProposedMatch",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AudioFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComposerName = table.Column<string>(type: "TEXT", nullable: true),
                    WorkTitle = table.Column<string>(type: "TEXT", nullable: true),
                    CatalogNumber = table.Column<string>(type: "TEXT", nullable: true),
                    ConductorName = table.Column<string>(type: "TEXT", nullable: true),
                    MovementNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    MovementTitle = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: true),
                    ConfidenceScore = table.Column<float>(type: "REAL", nullable: false),
                    MatchReasoning = table.Column<string>(type: "TEXT", nullable: true),
                    Confirmed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposedMatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposedMatch_AudioFiles_AudioFileId",
                        column: x => x.AudioFileId,
                        principalTable: "AudioFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_MovementId",
                table: "AudioFiles",
                column: "MovementId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_RecordingId",
                table: "AudioFiles",
                column: "RecordingId");

            migrationBuilder.CreateIndex(
                name: "IX_MovementRecording_RecordingId",
                table: "MovementRecording",
                column: "RecordingId");

            migrationBuilder.CreateIndex(
                name: "IX_Movements_WorkId",
                table: "Movements",
                column: "WorkId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformedMovement_MovementId",
                table: "PerformedMovement",
                column: "MovementId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformedMovement_RecordingId",
                table: "PerformedMovement",
                column: "RecordingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedMatch_AudioFileId",
                table: "ProposedMatch",
                column: "AudioFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_WorkId",
                table: "Recordings",
                column: "WorkId");

            migrationBuilder.CreateIndex(
                name: "IX_Works_ComposerId",
                table: "Works",
                column: "ComposerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovementRecording");

            migrationBuilder.DropTable(
                name: "PerformedMovement");

            migrationBuilder.DropTable(
                name: "ProposedMatch");

            migrationBuilder.DropTable(
                name: "AudioFiles");

            migrationBuilder.DropTable(
                name: "Movements");

            migrationBuilder.DropTable(
                name: "Recordings");

            migrationBuilder.DropTable(
                name: "Works");

            migrationBuilder.DropTable(
                name: "Composers");
        }
    }
}
