using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kiosk.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InitialCreateSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comercios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comercios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auditoria_eventos",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    comercio_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    canal = table.Column<int>(type: "INTEGER", nullable: false),
                    actor = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    tipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    detalle_json = table.Column<string>(type: "TEXT", nullable: true),
                    intencion_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditoria_eventos", x => x.id);
                    table.ForeignKey(
                        name: "FK_auditoria_eventos_comercios_comercio_id",
                        column: x => x.comercio_id,
                        principalTable: "comercios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    comercio_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    activa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categorias", x => x.id);
                    table.ForeignKey(
                        name: "FK_categorias_comercios_comercio_id",
                        column: x => x.comercio_id,
                        principalTable: "comercios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "configuraciones",
                columns: table => new
                {
                    comercio_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    clave = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    valor = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuraciones", x => new { x.comercio_id, x.clave });
                    table.ForeignKey(
                        name: "FK_configuraciones_comercios_comercio_id",
                        column: x => x.comercio_id,
                        principalTable: "comercios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "intenciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    comercio_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    whatsapp_numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    texto_original = table.Column<string>(type: "TEXT", nullable: false),
                    fue_audio = table.Column<bool>(type: "INTEGER", nullable: false),
                    structured_command_json = table.Column<string>(type: "TEXT", nullable: true),
                    estado = table.Column<int>(type: "INTEGER", nullable: false),
                    decision = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    resultado_json = table.Column<string>(type: "TEXT", nullable: true),
                    expira_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intenciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_intenciones_comercios_comercio_id",
                        column: x => x.comercio_id,
                        principalTable: "comercios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    comercio_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    username = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    rol = table.Column<int>(type: "INTEGER", nullable: false),
                    activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_comercios_comercio_id",
                        column: x => x.comercio_id,
                        principalTable: "comercios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_whitelist",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    comercio_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    whatsapp_numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whatsapp_whitelist", x => x.id);
                    table.ForeignKey(
                        name: "FK_whatsapp_whitelist_comercios_comercio_id",
                        column: x => x.comercio_id,
                        principalTable: "comercios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "productos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    comercio_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    categoria_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_productos", x => x.id);
                    table.ForeignKey(
                        name: "FK_productos_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_productos_comercios_comercio_id",
                        column: x => x.comercio_id,
                        principalTable: "comercios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cajas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    comercio_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    usuario_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    fecha_apertura = table.Column<DateTime>(type: "TEXT", nullable: false),
                    monto_inicial_centavos = table.Column<int>(type: "INTEGER", nullable: false),
                    fecha_cierre = table.Column<DateTime>(type: "TEXT", nullable: true),
                    monto_esperado_centavos = table.Column<int>(type: "INTEGER", nullable: true),
                    monto_declarado_centavos = table.Column<int>(type: "INTEGER", nullable: true),
                    diferencia_centavos = table.Column<int>(type: "INTEGER", nullable: true),
                    estado = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cajas", x => x.id);
                    table.ForeignKey(
                        name: "FK_cajas_comercios_comercio_id",
                        column: x => x.comercio_id,
                        principalTable: "comercios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cajas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "presentacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    producto_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    codigo_barras = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    precio_venta_centavos = table.Column<int>(type: "INTEGER", nullable: false),
                    precio_costo_centavos = table.Column<int>(type: "INTEGER", nullable: true),
                    activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    stock_actual = table.Column<int>(type: "INTEGER", nullable: false),
                    stock_minimo = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_presentacion", x => x.id);
                    table.ForeignKey(
                        name: "FK_presentacion_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    comercio_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    caja_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    numero = table.Column<int>(type: "INTEGER", nullable: false),
                    total_centavos = table.Column<int>(type: "INTEGER", nullable: false),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    client_generated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ventas", x => x.id);
                    table.ForeignKey(
                        name: "FK_ventas_cajas_caja_id",
                        column: x => x.caja_id,
                        principalTable: "cajas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ventas_comercios_comercio_id",
                        column: x => x.comercio_id,
                        principalTable: "comercios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "linea_venta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    venta_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    presentacion_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    producto_nombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    presentacion_nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    precio_unitario_centavos = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_linea_venta", x => x.id);
                    table.ForeignKey(
                        name: "FK_linea_venta_presentacion_presentacion_id",
                        column: x => x.presentacion_id,
                        principalTable: "presentacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_linea_venta_ventas_venta_id",
                        column: x => x.venta_id,
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    presentacion_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    motivo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    venta_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    usuario_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    origen = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimientos_stock", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimientos_stock_presentacion_presentacion_id",
                        column: x => x.presentacion_id,
                        principalTable: "presentacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_movimientos_stock_ventas_venta_id",
                        column: x => x.venta_id,
                        principalTable: "ventas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pago",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    venta_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    medio = table.Column<int>(type: "INTEGER", nullable: false),
                    monto_centavos = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pago", x => x.id);
                    table.ForeignKey(
                        name: "FK_pago_ventas_venta_id",
                        column: x => x.venta_id,
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_eventos_comercio_id",
                table: "auditoria_eventos",
                column: "comercio_id");

            migrationBuilder.CreateIndex(
                name: "IX_cajas_comercio_id",
                table: "cajas",
                column: "comercio_id",
                unique: true,
                filter: "\"estado\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_cajas_usuario_id",
                table: "cajas",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_comercio_id_nombre",
                table: "categorias",
                columns: new[] { "comercio_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_intenciones_comercio_id",
                table: "intenciones",
                column: "comercio_id");

            migrationBuilder.CreateIndex(
                name: "IX_intenciones_whatsapp_numero_estado",
                table: "intenciones",
                columns: new[] { "whatsapp_numero", "estado" });

            migrationBuilder.CreateIndex(
                name: "IX_linea_venta_presentacion_id",
                table: "linea_venta",
                column: "presentacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_linea_venta_venta_id",
                table: "linea_venta",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_stock_presentacion_id_created_at",
                table: "movimientos_stock",
                columns: new[] { "presentacion_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_stock_venta_id",
                table: "movimientos_stock",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_pago_venta_id",
                table: "pago",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_presentacion_codigo_barras",
                table: "presentacion",
                column: "codigo_barras");

            migrationBuilder.CreateIndex(
                name: "IX_presentacion_producto_id",
                table: "presentacion",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_productos_categoria_id",
                table: "productos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_productos_comercio_id_nombre_normalizado",
                table: "productos",
                columns: new[] { "comercio_id", "nombre_normalizado" });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_comercio_id",
                table: "usuarios",
                column: "comercio_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_username",
                table: "usuarios",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ventas_caja_id",
                table: "ventas",
                column: "caja_id");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_comercio_id_fecha",
                table: "ventas",
                columns: new[] { "comercio_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_whitelist_comercio_id_whatsapp_numero",
                table: "whatsapp_whitelist",
                columns: new[] { "comercio_id", "whatsapp_numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria_eventos");

            migrationBuilder.DropTable(
                name: "configuraciones");

            migrationBuilder.DropTable(
                name: "intenciones");

            migrationBuilder.DropTable(
                name: "linea_venta");

            migrationBuilder.DropTable(
                name: "movimientos_stock");

            migrationBuilder.DropTable(
                name: "pago");

            migrationBuilder.DropTable(
                name: "whatsapp_whitelist");

            migrationBuilder.DropTable(
                name: "presentacion");

            migrationBuilder.DropTable(
                name: "ventas");

            migrationBuilder.DropTable(
                name: "productos");

            migrationBuilder.DropTable(
                name: "cajas");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "comercios");
        }
    }
}
