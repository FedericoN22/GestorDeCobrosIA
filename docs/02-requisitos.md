# 02 - Requisitos

Cada requisito es claro, verificable y numerado. Los requisitos funcionales (RF) describen comportamiento. Los no funcionales (RNF) describen atributos del sistema y referencian los principios de `00-principios.md` cuando aplican.

## Requisitos funcionales

### Catálogo

- **RF-001** — El Admin puede crear, editar y desactivar categorías.
  *Verificación:* desde el panel web se puede crear/editar/desactivar y el cambio persiste y se lista.
- **RF-002** — El Admin puede crear productos con nombre único por comercio y categoría opcional.
- **RF-003** — El Admin puede crear una o más presentaciones por producto. La combinación `(producto, presentación)` es única.
- **RF-004** — El Admin puede asignar a cada presentación un precio de venta (`> 0`) y un precio de costo (opcional). Si `costo > venta` se muestra una alerta (no bloquea).
- **RF-005** — El Admin puede asignar un código de barras opcional, único entre presentaciones activas.
- **RF-006** — Productos y presentaciones con stock no se eliminan físicamente: se desactivan. El sistema lo impide o lo convierte en baja lógica.
- **RF-007** — El sistema mantiene un nombre normalizado (sin acentos, en mayúsculas) para búsqueda y para la resolución de la IA.

### Stock

- **RF-008** — El Admin puede cargar stock manualmente con `cantidad > 0` y costo opcional (que actualiza el costo de la presentación).
- **RF-009** — El Admin puede registrar ajustes de stock (positivos o negativos) con motivo obligatorio.
- **RF-010** — El stock actual de una presentación es la suma de sus movimientos; no es un contador editable.
- **RF-011** — El sistema nunca deja stock negativo.
- **RF-012** — El Admin puede configurar un stock mínimo por presentación y el sistema alerta cuando se alcanza.

### POS

- **RF-013** — El cajero abre una caja indicando el monto inicial. Existe como máximo una caja activa por comercio.
- **RF-014** — El cajero busca productos por nombre, código de barras o escaneo.
- **RF-015** — El cajero consulta el precio de un producto.
- **RF-016** — El cajero registra ventas con una o más líneas y cantidades. Sin caja abierta no se puede vender.
- **RF-017** — El cajero cobra en efectivo (con cálculo de vuelto), tarjeta, transferencia/QR o pago mixto.
- **RF-018** — El cajero imprime un ticket no fiscal.
- **RF-019** — El cajero cierra la caja declarando el arqueo por medio de pago; el sistema calcula la diferencia contra lo esperado y la registra.
- **RF-020** — El cajero no puede modificar precios ni aplicar descuentos.

### Usuarios y permisos

- **RF-021** — El Admin crea y modifica usuarios asignando rol `ADMIN` o `CAJERO`, y puede desactivarlos.
- **RF-022** — La autenticación valida credenciales y la autorización aplica permisos por rol en cada operación.
- **RF-023** — Los permisos son explícitos (no solo roles): cada caso de uso declara el permiso que requiere.

### WhatsApp e IA

- **RF-024** — El sistema procesa mensajes de WhatsApp solo de números incluidos en la whitelist.
- **RF-025** — El sistema acepta mensajes de texto y audios; los audios se transcriben y se procesan igual que el texto.
- **RF-026** — El sistema transforma el mensaje en un `StructuredCommand` y decide según la confianza: ejecutar, pedir aclaración, pedir confirmación o rechazar.
- **RF-027** — El Admin puede consultar stock y precio de un producto/presentación.
- **RF-028** — El Admin puede agregar stock indicando producto, presentación, cantidad y costo opcional.
- **RF-029** — El Admin puede crear un producto con su presentación y precio.
- **RF-030** — El Admin puede modificar el precio de una presentación. Requiere confirmación explícita.
- **RF-031** — El Admin puede eliminar/desactivar un producto o presentación. Requiere confirmación y advierte el stock existente.
- **RF-032** — Las confirmaciones tienen timeout (2 minutos, configurable) y reconocen palabras clave de confirmación/cancelación.
- **RF-033** — Un mensaje con más de un comando se rechaza pidiendo separar las instrucciones.
- **RF-034** — El bot responde en texto e incluye un mensaje de ayuda con ejemplos.

### Reportes

- **RF-035** — El Admin ve reportes de ventas por período, ganancias, stock, cierres de caja, ranking de productos y auditoría (definidos en `10-reportes.md`).
- **RF-036** — Los reportes se pueden exportar a CSV.

### Auditoría y configuración

- **RF-037** — Toda operación sensible genera un evento de auditoría *append-only* con canal, actor, tipo y detalle.
- **RF-038** — El Admin consulta la auditoría con filtros por canal, actor, tipo y rango de fechas.
- **RF-039** — El Admin configura los datos del comercio y el comportamiento del bot (nombre, bienvenida, timeout).
- **RF-040** — El Admin gestiona la whitelist de WhatsApp.

### Sync offline

- **RF-041** — El POS opera offline (vende, cobra, imprime) y sincroniza con el cloud cuando hay conexión, sin duplicar ni perder operaciones.
- **RF-042** — El catálogo se edita en el cloud y se distribuye al POS mediante un mecanismo de cursor (último estado visto).

## Requisitos no funcionales

- **RNF-001 (P1)** — La IA nunca ejecuta reglas de negocio; toda validación se hace en el backend.
  *Verificación:* el pipeline de IA solo produce un DTO; no existe ruta donde un comando de IA modifique datos sin pasar por el caso de uso del backend.
- **RNF-002 (P8)** — Si el parseo tiene confianza inferior al umbral o faltan campos, el bot pide aclaración. Nunca completa datos por su cuenta.
  *Verificación:* con los mismos mensajes de prueba, el bot nunca ejecuta una acción incompleta o ambigua.
- **RNF-003 (P5)** — El POS vende sin conexión a internet.
  *Verificación:* con la red cortada, se puede abrir caja, vender, cobrar e imprimir; al reconectar, el cloud queda consistente.
- **RNF-004 (P3)** — No se pierde información: operaciones idempotentes, registros inmutables y sin borrados físicos destructivos.
  *Verificación:* reintentar un sync no duplica ventas; los movimientos de stock y auditoría no se pueden editar ni borrar.
- **RNF-005 (P2)** — Toda operación sensible queda auditada y la auditoría es consultable.
- **RNF-006 (P6)** — Proveedor de IA, WhatsApp, impresión y persistencia son reemplazables por interfaces sin tocar el dominio.
- **RNF-007 (P7)** — Las reglas de negocio viven solo en el dominio.
- **RNF-008** — Seguridad: webhook de WhatsApp verificado por firma, whitelist validada antes de invocar IA, secretos fuera del repositorio.
- **RNF-009** — Los montos se manejan como enteros en centavos (moneda ARS), sin punto flotante.
- **RNF-010** — El cloud tiene consistencia eventual (atraso de segundos cuando el POS no sincroniza); el POS local es consistente en su turno.
- **RNF-011** — Máximo una caja activa por comercio, garantizada por el sistema.
- **RNF-012** — El API es stateless y escalable horizontalmente.
- **RNF-013** — Operaciones locales del POS en menos de 200 ms sin red (búsqueda, venta, cobro).
- **RNF-014** — Cumplimiento de las reglas de la API oficial de WhatsApp Business (plantillas, número verificado, TOS).
- **RNF-015** — El dominio es testeable sin infraestructura (tests unitarios sin base de datos ni servicios externos).
- **RNF-016** — El sistema sobrevive reinicios de la PC del POS sin perder operaciones pendientes (cola persistida).
