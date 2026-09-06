using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kiosk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCanalAndCanalUsuarioId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "whatsapp_numero",
                table: "intenciones",
                newName: "canal_usuario_id");

            migrationBuilder.RenameIndex(
                name: "IX_intenciones_whatsapp_numero_estado",
                table: "intenciones",
                newName: "IX_intenciones_canal_usuario_id_estado");

            migrationBuilder.AddColumn<int>(
                name: "canal",
                table: "intenciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE intenciones SET canal = 3 WHERE canal = 0;");

            migrationBuilder.AlterColumn<int>(
                name: "canal",
                table: "intenciones",
                type: "integer",
                nullable: false,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "canal",
                table: "intenciones",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 3);

            migrationBuilder.DropColumn(
                name: "canal",
                table: "intenciones");

            migrationBuilder.RenameColumn(
                name: "canal_usuario_id",
                table: "intenciones",
                newName: "whatsapp_numero");

            migrationBuilder.RenameIndex(
                name: "IX_intenciones_canal_usuario_id_estado",
                table: "intenciones",
                newName: "IX_intenciones_whatsapp_numero_estado");
        }
    }
}
