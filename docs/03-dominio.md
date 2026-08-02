# 03 - Dominio

Este documento describe el dominio del negocio **sin pensar en tablas ni implementación** (eso llega en `05-modelo-de-datos.md`). Se definen glosario, entidades, relaciones, reglas de negocio, invariantes y eventos importantes.

## Glosario

| Término | Definición |
|---|---|
| **Comercio** | El negocio administrado. En v1 hay un solo comercio por instalación. |
| **Producto** | Artículo genérico que vende el comercio. Ej: "Coca Cola". |
| **Presentación** | Variante vendible de un producto, con precio y stock propios. Ej: "2.25L". |
| **Categoría** | Agrupación opcional de productos. Ej: "Bebidas", "Snacks". |
| **Movimiento de stock** | Evento inmutable que suma o resta stock de una presentación. |
| **Caja** | Turno de trabajo de un cajero. Va de una apertura (con monto inicial) a un cierre (con arqueo). |
| **Arqueo** | Conteo del dinero físico al cerrar la caja, comparado con lo esperado por el sistema. |
| **Venta** | Operación que registra una o más líneas y uno o más pagos. |
| **Intención** | Resultado del parseo de un mensaje de WhatsApp con IA: un comando estructurado con estado. |
| **StructuredCommand** | DTO que la IA produce. Contiene acción, entidad, parámetros, confianza y campos faltantes. |
| **Canal** | Origen de una operación: `POS`, `WEB`, `WHATSAPP`. |
| **Stock actual** | Suma de los movimientos de stock de una presentación. Nunca un contador editable. |

## Entidades

### Comercio
Razón de existencia del sistema: posee catálogo, cajas, usuarios y configuración.

### Categoría
Agrupación de productos. Puede desactivarse; no se elimina si tiene productos.

### Producto
- Nombre genérico (único por comercio).
- Categoría opcional.
- Uno o más presentaciones.
- Baja lógica (se desactiva, no se borra).

### Presentación
- Pertenece a un producto. Combinación `(producto, presentación)` única.
- `precio_venta > 0`; `precio_costo` opcional.
- Código de barras opcional, único entre activas.
- Stock derivado de sus movimientos; stock mínimo opcional.
- Baja lógica (se desactiva, no se borra).

### Movimiento de stock
- Evento inmutable: tipo, cantidad (positiva = entrada, negativa = salida), motivo, origen, referencia (venta).
- Fuente de verdad del stock.

### Usuario
- Nombre, credenciales, rol (`ADMIN`/`CAJERO`), activo.
- El Admin también puede atender la caja.

### Caja
- Turno de un cajero. Apertura con monto inicial, cierre con arqueo.
- Máximo una activa por comercio.
- Agrupa las ventas de su turno.

### Venta
- Compuesta por líneas y pagos. Guarda total.
- Cada línea guarda **snapshot** de precio (los cambios de precio no re-escriben el pasado).

### Línea de venta
- Presentación vendida, cantidad y precio unitario snapshot.

### Pago
- Medio (`EFECTIVO`, `TARJETA`, `TRANSFERENCIA_QR`) y monto. Una venta puede tener varios pagos.

### Intención
- Registro de cada mensaje de WhatsApp: texto original, comando estructurado, estado y decisión.

### Evento de auditoría
- Registro inmutable de operaciones sensibles, con canal, actor y detalle.

### Configuración
- Pares clave-valor del comercio (datos del bot, timeouts, mensajes).

## Relaciones

```
Categoría 1─∞ Producto 1─∞ Presentación 1─∞ MovimientoStock
                                 │
                                 └─∞ LineaVenta ∞─1 Venta 1─∞ Pago
Venta N─1 Caja N─1 Usuario
Intención ── Comercio
AuditoriaEvento ── Comercio
Configuración ── Comercio
```

## Reglas de negocio

### Productos y presentaciones

- `R-PROD-001` Un producto tiene un nombre único por comercio y una o más presentaciones.
- `R-PROD-002` La combinación `(producto, presentación)` es única.
- `R-PROD-003` Toda presentación tiene `precio_venta > 0`.
- `R-PROD-004` Si hay `precio_costo`, se permite `precio_venta >= precio_costo`; un precio de venta menor al costo genera alerta, no bloqueo.
- `R-PROD-005` Un producto con presentaciones con stock no se borra físicamente: se desactiva. Igual para presentaciones con `stock > 0`.
- `R-PROD-006` El código de barras es opcional y único entre presentaciones activas.
- `R-PROD-007` Solo el rol `ADMIN` puede crear, modificar, desactivar o eliminar productos, presentaciones, precios y categorías.

### Stock

- `R-STOCK-001` El stock actual es la suma de los movimientos. Nunca es un contador editable.
- `R-STOCK-002` Entrada = cantidad positiva; salida = cantidad negativa.
- `R-STOCK-003` Ninguna operación deja stock negativo.
- `R-STOCK-004` Tipos: `ENTRADA_MANUAL`, `AJUSTE`, `VENTA`, `DEVOLUCION`.
- `R-STOCK-005` Los movimientos son inmutables. Para corregir un error se aplica un ajuste con motivo.
- `R-STOCK-006` La entrada manual la hace solo el Admin, con `cantidad > 0` y costo opcional (actualiza el costo de la presentación).
- `R-STOCK-007` Toda venta genera un movimiento negativo por cada línea.

### Ventas y caja

- `R-VENTA-001` No se registra una venta sin caja abierta.
- `R-VENTA-002` Máximo una caja activa a la vez por comercio.
- `R-VENTA-003` Solo el cajero que abrió la caja puede registrar ventas y cerrarla. (El Admin puede operar como cajero si abre su propia caja.)
- `R-VENTA-004` Cada línea guarda el precio de venta como snapshot.
- `R-VENTA-005` La suma de pagos debe ser `>= total`; el excedente en efectivo se devuelve como vuelto.
- `R-VENTA-006` El cajero no puede modificar precios ni aplicar descuentos (v1).
- `R-VENTA-007` El cierre de caja declara el arqueo por medio; el sistema calcula y registra la diferencia.
- `R-VENTA-008` Medios de pago v1: `EFECTIVO`, `TARJETA`, `TRANSFERENCIA_QR`.

### Usuarios y permisos

- `R-USR-001` Roles: `ADMIN` y `CAJERO`.
- `R-USR-002` El Admin tiene todos los permisos del Cajero más la gestión completa.
- `R-USR-003` Solo el Admin crea y modifica usuarios.

### WhatsApp e IA

- `R-WA-001` Solo los números de la whitelist pueden operar por WhatsApp.
- `R-WA-002` La IA nunca ejecuta lógica: solo produce un `StructuredCommand`.
- `R-WA-003` Confianza baja o campos faltantes → pedir aclaración. Nunca completar datos por cuenta propia.
- `R-WA-004` Acciones destructivas (`MODIFICAR_PRECIO`, `ELIMINAR_PRODUCTO`) requieren confirmación explícita dentro del timeout.
- `R-WA-005` Toda intención se persiste con mensaje original, comando, decisión y resultado.
- `R-WA-006` El bot responde solo en texto (v1); los audios se transcriben y procesan como texto.
- `R-WA-007` Un mensaje con más de un comando se rechaza pidiendo separar instrucciones.
- `R-WA-008` La whitelist la configura el Admin en el panel.

### Auditoría

- `R-AUD-001` Toda operación de escritura sensible genera un evento de auditoría con canal, actor, timestamp, tipo y detalle.
- `R-AUD-002` Las operaciones vía WhatsApp registran el mensaje original y el `StructuredCommand`.
- `R-AUD-003` Los eventos de auditoría no se editan ni se borran.

## Invariantes

Los invariantes son propiedades que deben cumplirse siempre, en cualquier estado del sistema:

- **INV-001** El stock actual de una presentación nunca es negativo.
- **INV-002** Existe como máximo una caja activa por comercio.
- **INV-003** La combinación `(producto, presentación)` es única.
- **INV-004** Todos los montos son enteros positivos (centavos); `precio_venta > 0`.
- **INV-005** Los movimientos de stock y los eventos de auditoría son inmutables (solo alta).
- **INV-006** No existe venta sin caja abierta.
- **INV-007** El precio de una línea de venta es un snapshot inmutable del momento de la venta.
- **INV-008** Ningún mensaje de WhatsApp ejecuta una acción sin pasar por el caso de uso del backend (P1).

## Eventos importantes

Eventos de dominio que materializan cambios relevantes. En v1 se persisten como auditoría y como movimientos de stock; no se usa un bus de eventos externo.

| Evento | Significado |
|---|---|
| `PresentacionCreada` | Se creó una presentación nueva. |
| `PrecioModificado` | Cambió el precio de venta de una presentación. |
| `StockEntradaManual` | El Admin cargó stock con costo. |
| `StockAjustado` | Se aplicó un ajuste con motivo. |
| `StockMinimoAlcanzado` | El stock actual cayó por debajo del mínimo configurado. |
| `CajaAbierta` | Un cajero abrió su turno con monto inicial. |
| `VentaRegistrada` | Se registró una venta (con líneas, pagos y movimientos de stock). |
| `CajaCerrada` | Se cerró la caja con arqueo y diferencia. |
| `IntencionParseada` | Un mensaje de WhatsApp se convirtió en comando estructurado. |
| `IntencionEjecutada` / `IntencionRechazada` | El backend decidió y ejecutó o rechazó la intención. |
| `OperacionSincronizada` | Una operación offline del POS se aplicó en el cloud. |
