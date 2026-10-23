using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewTracker.Migrations
{
    /// <inheritdoc />
    public partial class SyncIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "JobApplications",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "SyncInstallations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstallationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncInstallations", x => x.Id);
                });

            // Assign each existing row a permanent UUID before adding the unique index.
            // Keep legacy stage integers and all creation/modification clocks unchanged.
            const string newGuid = "hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)), 2) || '-8' || substr(hex(randomblob(2)), 2) || '-' || hex(randomblob(6))";
            migrationBuilder.Sql($"UPDATE JobApplications SET SyncId = {newGuid};");
            migrationBuilder.Sql("UPDATE JobApplications SET LastActivityDate = COALESCE(LastActivityDate, UpdatedAt, CreatedAt) WHERE LastActivityDate IS NULL;");
            migrationBuilder.Sql($"INSERT INTO SyncInstallations (Id, InstallationId) VALUES (1, {newGuid});");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_SyncId",
                table: "JobApplications",
                column: "SyncId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncInstallations");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_SyncId",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "JobApplications");
        }
    }
}
