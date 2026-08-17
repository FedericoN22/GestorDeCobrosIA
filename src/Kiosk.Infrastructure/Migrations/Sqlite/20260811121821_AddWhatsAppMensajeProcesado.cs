using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kiosk.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddWhatsAppMensajeProcesado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mensajes_whats_app_procesados",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    comercio_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    message_id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    procesado_en = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mensajes_whats_app_procesados", x => x.id);
                    table.ForeignKey(
                        name: "FK_mensajes_whats_app_procesados_comercios_comercio_id",
                        column: x => x.comercio_id,
                        principalTable: "comercios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_whats_app_procesados_comercio_id_message_id",
                table: "mensajes_whats_app_procesados",
                columns: new[] { "comercio_id", "message_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mensajes_whats_app_procesados");
        }
    }
}
