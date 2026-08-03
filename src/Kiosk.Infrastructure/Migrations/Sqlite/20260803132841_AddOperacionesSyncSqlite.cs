using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kiosk.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddOperacionesSyncSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operaciones_sync",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    comercio_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    resultado_json = table.Column<string>(type: "TEXT", nullable: true),
                    aplicada_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    confirmada_en = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operaciones_sync", x => x.id);
                    table.ForeignKey(
                        name: "FK_operaciones_sync_comercios_comercio_id",
                        column: x => x.comercio_id,
                        principalTable: "comercios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sync_comercio_id_aplicada_en",
                table: "operaciones_sync",
                columns: new[] { "comercio_id", "aplicada_en" });

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_sync_comercio_id_operation_id",
                table: "operaciones_sync",
                columns: new[] { "comercio_id", "operation_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operaciones_sync");
        }
    }
}
