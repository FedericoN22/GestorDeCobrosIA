using System.Globalization;
using System.Text;
using Kiosk.Domain.Ventas;
using Kiosk.Pos.Models;
using Microsoft.Data.Sqlite;

namespace Kiosk.Pos.Services;

public sealed class AlmacenLocal
{
    private readonly string _connectionString;
    private readonly object _lock = new();

    public AlmacenLocal(string rutaBaseDatos)
    {
        _connectionString = $"Data Source={rutaBaseDatos}";
    }

    public void Inicializar()
    {
        lock (_lock)
        {
            using var conexion = Abrir();
            using var cmd = conexion.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS sesion (
                    id INTEGER PRIMARY KEY CHECK (id = 1),
                    token TEXT NOT NULL,
                    comercio_id TEXT NOT NULL,
                    usuario_id TEXT NOT NULL,
                    username TEXT NOT NULL,
                    nombre TEXT NOT NULL,
                    rol TEXT NOT NULL,
                    login_en TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS categorias (
                    id TEXT PRIMARY KEY,
                    comercio_id TEXT NOT NULL,
                    nombre TEXT NOT NULL,
                    activa INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS productos (
                    id TEXT PRIMARY KEY,
                    comercio_id TEXT NOT NULL,
                    categoria_id TEXT,
                    nombre TEXT NOT NULL,
                    nombre_normalizado TEXT NOT NULL,
                    activo INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS presentaciones (
                    id TEXT PRIMARY KEY,
                    producto_id TEXT NOT NULL,
                    nombre TEXT NOT NULL,
                    codigo_barras TEXT,
                    precio_venta_centavos INTEGER NOT NULL,
                    precio_costo_centavos INTEGER,
                    activa INTEGER NOT NULL,
                    stock_actual INTEGER NOT NULL,
                    stock_minimo INTEGER
                );

                CREATE TABLE IF NOT EXISTS movimientos_stock (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    presentacion_id TEXT NOT NULL,
                    tipo INTEGER NOT NULL,
                    cantidad INTEGER NOT NULL,
                    venta_id TEXT,
                    creada_en TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS pending_ops (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    operation_id TEXT NOT NULL UNIQUE,
                    tipo TEXT NOT NULL,
                    payload TEXT NOT NULL,
                    estado TEXT NOT NULL,
                    error TEXT,
                    creada_en TEXT NOT NULL,
                    confirmada_en TEXT
                );

                CREATE TABLE IF NOT EXISTS cajas_local (
                    id TEXT PRIMARY KEY,
                    comercio_id TEXT NOT NULL,
                    usuario_id TEXT NOT NULL,
                    fecha_apertura TEXT NOT NULL,
                    monto_inicial_centavos INTEGER NOT NULL,
                    estado INTEGER NOT NULL,
                    fecha_cierre TEXT,
                    monto_esperado_centavos INTEGER,
                    monto_declarado_centavos INTEGER,
                    diferencia_centavos INTEGER
                );

                CREATE TABLE IF NOT EXISTS ventas_local (
                    id TEXT PRIMARY KEY,
                    numero INTEGER NOT NULL,
                    caja_id TEXT NOT NULL,
                    total_centavos INTEGER NOT NULL,
                    fecha TEXT NOT NULL,
                    client_generated INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS venta_lineas (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    venta_id TEXT NOT NULL,
                    presentacion_id TEXT NOT NULL,
                    producto_nombre TEXT NOT NULL,
                    presentacion_nombre TEXT NOT NULL,
                    cantidad INTEGER NOT NULL,
                    precio_unitario_centavos INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS venta_pagos (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    venta_id TEXT NOT NULL,
                    medio INTEGER NOT NULL,
                    monto_centavos INTEGER NOT NULL
                );

                CREATE INDEX IF NOT EXISTS ix_presentaciones_producto ON presentaciones(producto_id);
                CREATE INDEX IF NOT EXISTS ix_presentaciones_codigo ON presentaciones(codigo_barras);
                CREATE INDEX IF NOT EXISTS ix_productos_normalizado ON productos(nombre_normalizado);
                CREATE INDEX IF NOT EXISTS ix_pending_ops_estado ON pending_ops(estado);
                CREATE INDEX IF NOT EXISTS ix_ventas_local_caja ON ventas_local(caja_id);
                """;
            cmd.ExecuteNonQuery();
        }
    }

    private SqliteConnection Abrir()
    {
        var conexion = new SqliteConnection(_connectionString);
        conexion.Open();
        return conexion;
    }

    // ---------------- Sesión ----------------

    public void GuardarSesion(Sesion sesion)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                INSERT INTO sesion (id, token, comercio_id, usuario_id, username, nombre, rol, login_en)
                VALUES (1, $token, $comercio, $usuario, $username, $nombre, $rol, $login)
                ON CONFLICT(id) DO UPDATE SET
                    token = excluded.token,
                    comercio_id = excluded.comercio_id,
                    usuario_id = excluded.usuario_id,
                    username = excluded.username,
                    nombre = excluded.nombre,
                    rol = excluded.rol,
                    login_en = excluded.login_en
                """;
            cmd.Parameters.AddWithValue("$token", sesion.Token);
            cmd.Parameters.AddWithValue("$comercio", sesion.ComercioId.ToString());
            cmd.Parameters.AddWithValue("$usuario", sesion.UsuarioId.ToString());
            cmd.Parameters.AddWithValue("$username", sesion.Username);
            cmd.Parameters.AddWithValue("$nombre", sesion.Nombre);
            cmd.Parameters.AddWithValue("$rol", sesion.Rol);
            cmd.Parameters.AddWithValue("$login", sesion.LoginEn.ToString("O"));
            cmd.ExecuteNonQuery();
        }
    }

    public Sesion? ObtenerSesion()
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT token, comercio_id, usuario_id, username, nombre, rol, login_en FROM sesion WHERE id = 1";
            using var r = cmd.ExecuteReader();
            if (!r.Read())
            {
                return null;
            }

            return new Sesion
            {
                Token = r.GetString(0),
                ComercioId = Guid.Parse(r.GetString(1)),
                UsuarioId = Guid.Parse(r.GetString(2)),
                Username = r.GetString(3),
                Nombre = r.GetString(4),
                Rol = r.GetString(5),
                LoginEn = DateTime.Parse(r.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            };
        }
    }

    public void LimpiarSesion()
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "DELETE FROM sesion WHERE id = 1";
            cmd.ExecuteNonQuery();
        }
    }

    // ---------------- Caja ----------------

    public void AbrirCajaLocal(CajaLocal caja)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                INSERT INTO cajas_local (id, comercio_id, usuario_id, fecha_apertura, monto_inicial_centavos, estado)
                VALUES ($id, $comercio, $usuario, $fecha, $monto, 1)
                """;
            cmd.Parameters.AddWithValue("$id", caja.Id.ToString());
            cmd.Parameters.AddWithValue("$comercio", caja.ComercioId.ToString());
            cmd.Parameters.AddWithValue("$usuario", caja.UsuarioId.ToString());
            cmd.Parameters.AddWithValue("$fecha", caja.FechaApertura.ToString("O"));
            cmd.Parameters.AddWithValue("$monto", caja.MontoInicialCentavos);
            cmd.ExecuteNonQuery();
        }
    }

    public CajaLocal? ObtenerCajaActiva()
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                SELECT id, comercio_id, usuario_id, fecha_apertura, monto_inicial_centavos, estado,
                       fecha_cierre, monto_esperado_centavos, monto_declarado_centavos, diferencia_centavos
                FROM cajas_local WHERE estado = 1 LIMIT 1
                """;
            using var r = cmd.ExecuteReader();
            return r.Read() ? LeerCaja(r) : null;
        }
    }

    public void CerrarCajaLocal(CajaLocal caja)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                UPDATE cajas_local SET
                    estado = 2,
                    fecha_cierre = $fecha,
                    monto_esperado_centavos = $esperado,
                    monto_declarado_centavos = $declarado,
                    diferencia_centavos = $diferencia
                WHERE id = $id
                """;
            cmd.Parameters.AddWithValue("$fecha", caja.FechaCierre!.Value.ToString("O"));
            cmd.Parameters.AddWithValue("$esperado", caja.MontoEsperadoCentavos!.Value);
            cmd.Parameters.AddWithValue("$declarado", caja.MontoDeclaradoCentavos!.Value);
            cmd.Parameters.AddWithValue("$diferencia", caja.DiferenciaCentavos!.Value);
            cmd.Parameters.AddWithValue("$id", caja.Id.ToString());
            cmd.ExecuteNonQuery();
        }
    }

    public void ForzarCierreCajaLocal(Guid cajaId)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE cajas_local SET estado = 2, fecha_cierre = $fecha WHERE id = $id AND estado = 1";
            cmd.Parameters.AddWithValue("$fecha", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("$id", cajaId.ToString());
            cmd.ExecuteNonQuery();
        }
    }

    private static CajaLocal LeerCaja(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(0)),
        ComercioId = Guid.Parse(r.GetString(1)),
        UsuarioId = Guid.Parse(r.GetString(2)),
        FechaApertura = DateTime.Parse(r.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        MontoInicialCentavos = r.GetInt32(4),
        Estado = (EstadoCaja)r.GetInt32(5),
        FechaCierre = r.IsDBNull(6) ? null : DateTime.Parse(r.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        MontoEsperadoCentavos = r.IsDBNull(7) ? null : r.GetInt32(7),
        MontoDeclaradoCentavos = r.IsDBNull(8) ? null : r.GetInt32(8),
        DiferenciaCentavos = r.IsDBNull(9) ? null : r.GetInt32(9)
    };

    // ---------------- Catálogo ----------------

    public void ReemplazarCatalogo(IReadOnlyList<CategoriaDto> categorias, IReadOnlyList<ProductoDto> productos)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var tx = c.BeginTransaction();

            using (var borrar = c.CreateCommand())
            {
                borrar.Transaction = tx;
                borrar.CommandText = "DELETE FROM categorias; DELETE FROM presentaciones; DELETE FROM productos;";
                borrar.ExecuteNonQuery();
            }

            foreach (var cat in categorias)
            {
                using var cmd = c.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO categorias (id, comercio_id, nombre, activa) VALUES ($id, $comercio, $nombre, $activa)";
                cmd.Parameters.AddWithValue("$id", cat.Id.ToString());
                cmd.Parameters.AddWithValue("$comercio", cat.ComercioId.ToString());
                cmd.Parameters.AddWithValue("$nombre", cat.Nombre);
                cmd.Parameters.AddWithValue("$activa", cat.Activa ? 1 : 0);
                cmd.ExecuteNonQuery();
            }

            foreach (var prod in productos)
            {
                using var cmd = c.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO productos (id, comercio_id, categoria_id, nombre, nombre_normalizado, activo) VALUES ($id, $comercio, $categoria, $nombre, $norm, $activo)";
                cmd.Parameters.AddWithValue("$id", prod.Id.ToString());
                cmd.Parameters.AddWithValue("$comercio", prod.ComercioId.ToString());
                cmd.Parameters.AddWithValue("$categoria", (object?)prod.CategoriaId?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$nombre", prod.Nombre);
                cmd.Parameters.AddWithValue("$norm", prod.NombreNormalizado);
                cmd.Parameters.AddWithValue("$activo", prod.Activo ? 1 : 0);
                cmd.ExecuteNonQuery();

                foreach (var pres in prod.Presentaciones)
                {
                    using var c2 = c.CreateCommand();
                    c2.Transaction = tx;
                    c2.CommandText = """
                        INSERT INTO presentaciones (id, producto_id, nombre, codigo_barras, precio_venta_centavos,
                                                    precio_costo_centavos, activa, stock_actual, stock_minimo)
                        VALUES ($id, $producto, $nombre, $codigo, $precioVenta, $precioCosto, $activa, $stock, $minimo)
                        """;
                    c2.Parameters.AddWithValue("$id", pres.Id.ToString());
                    c2.Parameters.AddWithValue("$producto", pres.ProductoId.ToString());
                    c2.Parameters.AddWithValue("$nombre", pres.Nombre);
                    c2.Parameters.AddWithValue("$codigo", (object?)pres.CodigoBarras ?? DBNull.Value);
                    c2.Parameters.AddWithValue("$precioVenta", pres.PrecioVentaCentavos);
                    c2.Parameters.AddWithValue("$precioCosto", (object?)pres.PrecioCostoCentavos ?? DBNull.Value);
                    c2.Parameters.AddWithValue("$activa", pres.Activa ? 1 : 0);
                    c2.Parameters.AddWithValue("$stock", pres.StockActual);
                    c2.Parameters.AddWithValue("$minimo", (object?)pres.StockMinimo ?? DBNull.Value);
                    c2.ExecuteNonQuery();
                }
            }

            tx.Commit();
        }
    }

    public IReadOnlyList<ResultadoBusqueda> BuscarProductos(string termino)
    {
        termino = termino.Trim();
        var resultados = new List<ResultadoBusqueda>();
        if (termino.Length == 0)
        {
            return resultados;
        }

        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                SELECT p.id, p.producto_id, p.nombre, p.codigo_barras, p.precio_venta_centavos,
                       p.stock_actual, pr.nombre
                FROM presentaciones p
                JOIN productos pr ON pr.id = p.producto_id
                WHERE p.activa = 1 AND pr.activo = 1 AND (pr.nombre_normalizado LIKE $like OR p.nombre LIKE $like OR p.codigo_barras = $codigo)
                ORDER BY pr.nombre, p.nombre
                LIMIT 30
                """;
            cmd.Parameters.AddWithValue("$like", $"%{NormalizarParaBusqueda(termino)}%");
            cmd.Parameters.AddWithValue("$codigo", termino);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                resultados.Add(new ResultadoBusqueda(
                    Guid.Parse(r.GetString(0)),
                    Guid.Parse(r.GetString(1)),
                    r.GetString(6),
                    r.GetString(2),
                    r.IsDBNull(3) ? null : r.GetString(3),
                    r.GetInt32(4),
                    r.GetInt32(5)));
            }
        }

        return resultados;
    }

    public int ObtenerStockLocal(Guid presentacionId)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT stock_actual FROM presentaciones WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", presentacionId.ToString());
            var valor = cmd.ExecuteScalar();
            return valor is null or DBNull ? 0 : Convert.ToInt32(valor);
        }
    }

    public void DecrementarStock(Guid presentacionId, int cantidad, Guid ventaId)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var tx = c.BeginTransaction();

            using (var upd = c.CreateCommand())
            {
                upd.Transaction = tx;
                upd.CommandText = "UPDATE presentaciones SET stock_actual = stock_actual - $cantidad WHERE id = $id";
                upd.Parameters.AddWithValue("$cantidad", cantidad);
                upd.Parameters.AddWithValue("$id", presentacionId.ToString());
                upd.ExecuteNonQuery();
            }

            using (var mov = c.CreateCommand())
            {
                mov.Transaction = tx;
                mov.CommandText = "INSERT INTO movimientos_stock (presentacion_id, tipo, cantidad, venta_id, creada_en) VALUES ($id, 3, $cantidad, $venta, $fecha)";
                mov.Parameters.AddWithValue("$id", presentacionId.ToString());
                mov.Parameters.AddWithValue("$cantidad", -cantidad);
                mov.Parameters.AddWithValue("$venta", ventaId.ToString());
                mov.Parameters.AddWithValue("$fecha", DateTime.UtcNow.ToString("O"));
                mov.ExecuteNonQuery();
            }

            tx.Commit();
        }
    }

    // ---------------- Ventas locales ----------------

    public int SiguienteNumeroVenta()
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(MAX(numero), 0) FROM ventas_local";
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0) + 1;
        }
    }

    public void GuardarVenta(VentaLocal venta)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var tx = c.BeginTransaction();

            using (var v = c.CreateCommand())
            {
                v.Transaction = tx;
                v.CommandText = """
                    INSERT INTO ventas_local (id, numero, caja_id, total_centavos, fecha, client_generated)
                    VALUES ($id, $numero, $caja, $total, $fecha, $cg)
                    """;
                v.Parameters.AddWithValue("$id", venta.Id.ToString());
                v.Parameters.AddWithValue("$numero", venta.Numero);
                v.Parameters.AddWithValue("$caja", venta.CajaId.ToString());
                v.Parameters.AddWithValue("$total", venta.TotalCentavos);
                v.Parameters.AddWithValue("$fecha", venta.Fecha.ToString("O"));
                v.Parameters.AddWithValue("$cg", venta.ClientGenerated ? 1 : 0);
                v.ExecuteNonQuery();
            }

            foreach (var linea in venta.Lineas)
            {
                using var l = c.CreateCommand();
                l.Transaction = tx;
                l.CommandText = """
                    INSERT INTO venta_lineas (venta_id, presentacion_id, producto_nombre, presentacion_nombre, cantidad, precio_unitario_centavos)
                    VALUES ($venta, $pres, $prod, $presN, $cant, $precio)
                    """;
                l.Parameters.AddWithValue("$venta", venta.Id.ToString());
                l.Parameters.AddWithValue("$pres", linea.PresentacionId.ToString());
                l.Parameters.AddWithValue("$prod", linea.ProductoNombre);
                l.Parameters.AddWithValue("$presN", linea.PresentacionNombre);
                l.Parameters.AddWithValue("$cant", linea.Cantidad);
                l.Parameters.AddWithValue("$precio", linea.PrecioUnitarioCentavos);
                l.ExecuteNonQuery();
            }

            foreach (var pago in venta.Pagos)
            {
                using var p = c.CreateCommand();
                p.Transaction = tx;
                p.CommandText = "INSERT INTO venta_pagos (venta_id, medio, monto_centavos) VALUES ($venta, $medio, $monto)";
                p.Parameters.AddWithValue("$venta", venta.Id.ToString());
                p.Parameters.AddWithValue("$medio", (int)pago.Medio);
                p.Parameters.AddWithValue("$monto", pago.MontoCentavos);
                p.ExecuteNonQuery();
            }

            tx.Commit();
        }
    }

    public VentaLocal? ObtenerVenta(Guid id)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT id, numero, caja_id, total_centavos, fecha, client_generated FROM ventas_local WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            using var r = cmd.ExecuteReader();
            if (!r.Read())
            {
                return null;
            }

            var venta = new VentaLocal
            {
                Id = Guid.Parse(r.GetString(0)),
                Numero = r.GetInt32(1),
                CajaId = Guid.Parse(r.GetString(2)),
                TotalCentavos = r.GetInt32(3),
                Fecha = DateTime.Parse(r.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                ClientGenerated = r.GetInt32(5) == 1
            };
            r.Close();

            using var lc = c.CreateCommand();
            lc.CommandText = "SELECT presentacion_id, producto_nombre, presentacion_nombre, cantidad, precio_unitario_centavos FROM venta_lineas WHERE venta_id = $id";
            lc.Parameters.AddWithValue("$id", id.ToString());
            using var lr = lc.ExecuteReader();
            while (lr.Read())
            {
                venta.Lineas.Add(new LineaLocal
                {
                    PresentacionId = Guid.Parse(lr.GetString(0)),
                    ProductoNombre = lr.GetString(1),
                    PresentacionNombre = lr.GetString(2),
                    Cantidad = lr.GetInt32(3),
                    PrecioUnitarioCentavos = lr.GetInt32(4)
                });
            }
            lr.Close();

            using var pc = c.CreateCommand();
            pc.CommandText = "SELECT medio, monto_centavos FROM venta_pagos WHERE venta_id = $id";
            pc.Parameters.AddWithValue("$id", id.ToString());
            using var pr = pc.ExecuteReader();
            while (pr.Read())
            {
                venta.Pagos.Add(new PagoLocal
                {
                    Medio = (MedioPago)pr.GetInt32(0),
                    MontoCentavos = pr.GetInt32(1)
                });
            }

            return venta;
        }
    }

    public int SumarPagosDeCaja(Guid cajaId, MedioPago medio)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                SELECT COALESCE(SUM(vp.monto_centavos), 0)
                FROM venta_pagos vp
                JOIN ventas_local v ON v.id = vp.venta_id
                WHERE v.caja_id = $caja AND vp.medio = $medio
                """;
            cmd.Parameters.AddWithValue("$caja", cajaId.ToString());
            cmd.Parameters.AddWithValue("$medio", (int)medio);
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
    }

    // ---------------- Cola de operaciones ----------------

    public PendingOp EncolarOperacion(string tipo, object payload)
    {
        var op = new PendingOp
        {
            OperationId = Guid.NewGuid(),
            Tipo = tipo,
            Payload = System.Text.Json.JsonSerializer.Serialize(payload),
            Estado = "PENDIENTE",
            CreadaEn = DateTime.UtcNow
        };

        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                INSERT INTO pending_ops (operation_id, tipo, payload, estado, creada_en)
                VALUES ($op, $tipo, $payload, 'PENDIENTE', $fecha)
                """;
            cmd.Parameters.AddWithValue("$op", op.OperationId.ToString());
            cmd.Parameters.AddWithValue("$tipo", op.Tipo);
            cmd.Parameters.AddWithValue("$payload", op.Payload);
            cmd.Parameters.AddWithValue("$fecha", op.CreadaEn.ToString("O"));
            cmd.ExecuteNonQuery();
        }

        return op;
    }

    public IReadOnlyList<PendingOp> ObtenerPendientes()
    {
        var lista = new List<PendingOp>();
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT id, operation_id, tipo, payload, estado, error, creada_en, confirmada_en FROM pending_ops WHERE estado = 'PENDIENTE' ORDER BY id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                lista.Add(new PendingOp
                {
                    Id = r.GetInt64(0),
                    OperationId = Guid.Parse(r.GetString(1)),
                    Tipo = r.GetString(2),
                    Payload = r.GetString(3),
                    Estado = r.GetString(4),
                    Error = r.IsDBNull(5) ? null : r.GetString(5),
                    CreadaEn = DateTime.Parse(r.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    ConfirmadaEn = r.IsDBNull(7) ? null : DateTime.Parse(r.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                });
            }
        }

        return lista;
    }

    public IReadOnlyList<PendingOp> ObtenerConErrores()
    {
        var lista = new List<PendingOp>();
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT id, operation_id, tipo, payload, estado, error, creada_en, confirmada_en FROM pending_ops WHERE estado = 'ERROR' ORDER BY id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                lista.Add(new PendingOp
                {
                    Id = r.GetInt64(0),
                    OperationId = Guid.Parse(r.GetString(1)),
                    Tipo = r.GetString(2),
                    Payload = r.GetString(3),
                    Estado = r.GetString(4),
                    Error = r.IsDBNull(5) ? null : r.GetString(5),
                    CreadaEn = DateTime.Parse(r.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    ConfirmadaEn = r.IsDBNull(7) ? null : DateTime.Parse(r.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                });
            }
        }

        return lista;
    }

    public void MarcarOperacionOk(PendingOp op, DateTime confirmadaEn)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE pending_ops SET estado = 'OK', error = NULL, confirmada_en = $fecha WHERE id = $id";
            cmd.Parameters.AddWithValue("$fecha", confirmadaEn.ToString("O"));
            cmd.Parameters.AddWithValue("$id", op.Id);
            cmd.ExecuteNonQuery();
        }
    }

    public void MarcarOperacionError(PendingOp op, string error)
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE pending_ops SET estado = 'ERROR', error = $error WHERE id = $id";
            cmd.Parameters.AddWithValue("$error", error);
            cmd.Parameters.AddWithValue("$id", op.Id);
            cmd.ExecuteNonQuery();
        }
    }

    public int ContarPendientes()
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM pending_ops WHERE estado = 'PENDIENTE'";
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
    }

    public int ContarConErrores()
    {
        lock (_lock)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM pending_ops WHERE estado = 'ERROR'";
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
    }

    private static string NormalizarParaBusqueda(string texto)
    {
        var sb = new StringBuilder(texto.Length);
        foreach (var ch in texto.Normalize(NormalizationForm.FormD))
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(char.ToUpperInvariant(ch));
            }
        }

        return sb.ToString();
    }
}
