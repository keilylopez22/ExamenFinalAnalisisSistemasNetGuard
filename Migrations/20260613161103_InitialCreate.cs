using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NetGuardGT.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Technicians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Specialization = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    SiteLocation = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEscalated = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TechnicianId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IncidentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IncidentId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ToStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    TechnicianId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentHistories_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Technicians",
                columns: new[] { "Id", "IsActive", "Name", "Specialization" },
                values: new object[,]
                {
                    { 1, true, "Carlos Mendez", 0 },
                    { 2, true, "Ana Lopez", 0 },
                    { 3, true, "Luis Perez", 1 },
                    { 4, true, "Maria Garcia", 1 },
                    { 5, true, "Roberto Diaz", 2 },
                    { 6, true, "Sofia Castro", 2 },
                    { 7, true, "Diego Morales", 3 },
                    { 8, true, "Laura Torres", 3 },
                    { 9, true, "Pedro Jimenez", 4 },
                    { 10, true, "Elena Ruiz", 4 },
                    { 11, true, "Marco Salazar", 0 },
                    { 12, true, "Claudia Reyes", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentHistories_IncidentId",
                table: "IncidentHistories",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_TechnicianId",
                table: "Incidents",
                column: "TechnicianId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentHistories");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "Technicians");
        }
    }
}
