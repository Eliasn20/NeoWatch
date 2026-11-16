using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoWatch.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261116041550_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Asteroids",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                NeoId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                AbsoluteMagnitude = table.Column<double>(type: "REAL", nullable: false),
                DiameterMinKm = table.Column<double>(type: "REAL", nullable: false),
                DiameterMaxKm = table.Column<double>(type: "REAL", nullable: false),
                IsPotentiallyHazardous = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Asteroids", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DailySummaries",
            columns: table => new
            {
                Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                ApproachCount = table.Column<int>(type: "INTEGER", nullable: false),
                HazardousCount = table.Column<int>(type: "INTEGER", nullable: false),
                ClosestMissKm = table.Column<double>(type: "REAL", nullable: false),
                ClosestAsteroidName = table.Column<string>(type: "TEXT", nullable: false),
                LargestDiameterKm = table.Column<double>(type: "REAL", nullable: false),
                LargestAsteroidName = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DailySummaries", x => x.Date);
            });

        migrationBuilder.CreateTable(
            name: "IngestionRuns",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                WindowStart = table.Column<DateOnly>(type: "TEXT", nullable: false),
                WindowEnd = table.Column<DateOnly>(type: "TEXT", nullable: false),
                StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                FinishedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                ApproachesSeen = table.Column<int>(type: "INTEGER", nullable: false),
                ApproachesInserted = table.Column<int>(type: "INTEGER", nullable: false),
                ApproachesUpdated = table.Column<int>(type: "INTEGER", nullable: false),
                Error = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IngestionRuns", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CloseApproaches",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AsteroidId = table.Column<int>(type: "INTEGER", nullable: false),
                Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                EpochMillis = table.Column<long>(type: "INTEGER", nullable: false),
                VelocityKmPerSec = table.Column<double>(type: "REAL", nullable: false),
                MissDistanceKm = table.Column<double>(type: "REAL", nullable: false),
                OrbitingBody = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CloseApproaches", x => x.Id);
                table.ForeignKey(
                    name: "FK_CloseApproaches_Asteroids_AsteroidId",
                    column: x => x.AsteroidId,
                    principalTable: "Asteroids",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Asteroids_NeoId",
            table: "Asteroids",
            column: "NeoId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CloseApproaches_AsteroidId_EpochMillis",
            table: "CloseApproaches",
            columns: new[] { "AsteroidId", "EpochMillis" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CloseApproaches_Date",
            table: "CloseApproaches",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_IngestionRuns_WindowStart_WindowEnd",
            table: "IngestionRuns",
            columns: new[] { "WindowStart", "WindowEnd" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CloseApproaches");

        migrationBuilder.DropTable(
            name: "DailySummaries");

        migrationBuilder.DropTable(
            name: "IngestionRuns");

        migrationBuilder.DropTable(
            name: "Asteroids");
    }
}
