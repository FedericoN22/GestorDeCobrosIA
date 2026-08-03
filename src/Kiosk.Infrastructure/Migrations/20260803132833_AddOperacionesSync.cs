using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kiosk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOperacionesSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operaciones_sync",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    comercio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resultado_json = table.Column<string>(type: "text", nullable: true),
                    aplicada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
