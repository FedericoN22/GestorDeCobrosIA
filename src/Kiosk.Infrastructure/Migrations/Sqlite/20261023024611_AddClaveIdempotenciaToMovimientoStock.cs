using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kiosk.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddClaveIdempotenciaToMovimientoStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "clave_idempotencia",
                table: "movimientos_stock",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_stock_clave_idempotencia",
                table: "movimientos_stock",
                column: "clave_idempotencia",
                unique: true,
                filter: "\"clave_idempotencia\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_movimientos_stock_clave_idempotencia",
                table: "movimientos_stock");

            migrationBuilder.DropColumn(
                name: "clave_idempotencia",
                table: "movimientos_stock");
        }
    }
}
