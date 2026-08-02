# 04 - Casos de uso

## Actores

| Actor | Descripción | Permisos clave |
|---|---|---|
| **Admin** | Dueño del comercio. Gestiona todo. También puede atender la caja. Opera por panel web y WhatsApp. | Todos (`07-seguridad.md`) |
| **Cajero** | Atiende la caja con el POS desktop. | Solo vender, cobrar, consultar, abrir/cerrar caja |
| **Sistema** | Procesa ventas, stock, cajas y comandos de WhatsApp. | — |

## Casos de uso por actor

### Admin (panel web)

| ID | Caso de uso | RF |
|---|---|---|
| `CU-ADM-001` | Iniciar sesión | RF-022 |
| `CU-ADM-002` | Gestionar categorías (alta, edición, baja lógica) | RF-001 |
| `CU-ADM-003` | Gestionar productos y presentaciones (alta, edición, precio, costo, código, baja lógica) | RF-002…007 |
| `CU-ADM-004` | Cargar stock manualmente con costo | RF-008 |
| `CU-ADM-005` | Registrar ajustes de stock con motivo | RF-009 |
| `CU-ADM-006` | Gestionar usuarios y roles | RF-021 |
| `CU-ADM-007` | Gestionar whitelist de WhatsApp | RF-040 |
| `CU-ADM-008` | Ver reportes (ventas, ganancias, stock, cierres, ranking, auditoría) | RF-035 |
| `CU-ADM-009` | Exportar reportes a CSV | RF-036 |
| `CU-ADM-010` | Configurar el comercio y el bot | RF-039 |
| `CU-ADM-011` | Consultar auditoría | RF-038 |

### Admin y Cajero (POS desktop)

| ID | Caso de uso | RF |
|---|---|---|
| `CU-CAJ-001` | Abrir caja con monto inicial | RF-013 |
| `CU-CAJ-002` | Buscar producto (nombre, código, escaneo) | RF-014 |
| `CU-CAJ-003` | Consultar precio | RF-015 |
| `CU-CAJ-004` | Registrar venta (líneas y cantidades) | RF-016 |
| `CU-CAJ-005` | Cobrar (efectivo con vuelto, tarjeta, QR, mixto) | RF-017 |
| `CU-CAJ-006` | Imprimir ticket no fiscal | RF-018 |
| `CU-CAJ-007` | Cerrar caja con arqueo | RF-019 |
| `CU-CAJ-008` | Ver el cierre del propio turno | RF-035 (parcial) |

### Admin (WhatsApp)

| ID | Caso de uso | RF |
|---|---|---|
| `CU-WA-001` | Consultar stock | RF-027 |
| `CU-WA-002` | Consultar precio | RF-027 |
| `CU-WA-003` | Agregar stock (con costo opcional) | RF-028 |
| `CU-WA-004` | Crear producto con presentación | RF-029 |
| `CU-WA-005` | Modificar precio (con confirmación) | RF-030 |
| `CU-WA-006` | Eliminar/desactivar producto (con confirmación) | RF-031 |
| `CU-WA-007` | Listar productos por categoría o texto | RF-027 |

## Flujo general del POS (venta)

```mermaid
flowchart TD
    A[Abrir caja con monto inicial] --> B[Buscar producto]
    B --> C[Agregar línea y cantidad]
    C --> D{¿Más productos?}
    D -- Sí --> B
    D -- No --> E[Seleccionar medio de pago]
    E --> F[Registrar pago y calcular vuelto]
    F --> G[Persistir venta + movimientos de stock]
    G --> H[Imprimir ticket]
```

## Flujo del cierre de caja

```mermaid
flowchart TD
    A[Pedir cierre de caja] --> B{¿Hay caja abierta?}
    B -- No --> Z[Rechazar: no hay caja abierta]
    B -- Sí --> C[Sistema calcula esperado por medio de pago]
    C --> D[Cajero declara el arqueo por medio]
    D --> E[Calcular diferencia esperado vs declarado]
    E --> F[Registrar cierre; caja pasa a CERRADA]
    F --> G[Reporte de cierre con diferencias]
```

## Flujo de WhatsApp (texto o audio)

```mermaid
sequenceDiagram
    participant U as Admin (WhatsApp)
    participant W as Meta Cloud API
    participant B as Backend
    participant LLM as Proveedor IA
    participant DB as Base de datos

    U->>W: "Agregar Coca Cola 2.25L, cantidad 12, precio 4200"
    W->>B: Webhook con mensaje
    B->>B: ¿Número en whitelist? Si no → responde y termina
    B->>B: ¿Es audio? → transcribir (STT)
    B->>LLM: Prompt + mensaje (function calling con schema)
    LLM-->>B: StructuredCommand JSON
    B->>B: Normalizar entidades (buscar producto/presentación)
    B->>DB: Persistir Intención
    alt Ambiguo o faltan campos
        B-->>U: "¿Qué presentación? ¿La de 600ml o la de 2.25L?"
    else Destructivo (modificar precio / eliminar)
        B-->>U: "¿Confirmás modificar precio de Coca Cola 2.25L a $4200? Respondé SI o CANCELAR"
        U->>B: "SI"
        B->>DB: Ejecutar comando
        B-->>U: "Listo. Precio de Coca Cola 2.25L: $4200"
    else Consulta directa (stock/precio)
        B->>DB: Consultar
        B-->>U: "Stock de Coca Cola 2.25L: 12 unidades. Precio: $4200"
    else Inválido
        B-->>U: "No se pudo interpretar. Probá: 'Agregar [producto] [presentación], cantidad N, precio N'"
    end
```

## Máquina de estados de una intención

```mermaid
stateDiagram-v2
    [*] --> PARSEADA
    PARSEADA --> ESPERANDO_CONFIRMACION: destructivo y confianza suficiente
    PARSEADA --> ACLARACION: ambigüedad o faltan campos
    ESPERANDO_CONFIRMACION --> EJECUTADA: respuesta SI
    ESPERANDO_CONFIRMACION --> CANCELADA: respuesta NO / CANCELAR
    ESPERANDO_CONFIRMACION --> CANCELADA: timeout 2 min
    ACLARACION --> PARSEADA: nuevo mensaje del usuario
    EJECUTADA --> [*]
    CANCELADA --> [*]
```

## Casos de uso de WhatsApp detallados

### CU-WA-003 — Agregar stock

**Mensaje de ejemplo:** `"Agregar Coca Cola 2.25L, cantidad 12, precio 4200."`

1. Validar whitelist.
2. Parsear a `StructuredCommand` (`ACCION=AGREGAR_STOCK`).
3. Resolver la presentación. Si hay varios candidatos → preguntar (nunca elegir al azar).
4. Validar `cantidad > 0`.
5. Si el campo numérico "precio" no especifica si es venta o costo → el bot pregunta (P8).
6. Ejecutar: crear `MovimientoStock` `ENTRADA_MANUAL`.
7. Responder con confirmación y stock resultante.

### CU-WA-005 — Modificar precio

**Mensaje de ejemplo:** `"Cambiar precio de Coca Cola 2.25L a 4300."`

1. Parsear y resolver presentación.
2. Acción destructiva → `ESPERANDO_CONFIRMACION`.
3. El usuario confirma → actualizar `precio_venta`.
4. Registrar auditoría con mensaje original y comando.

### CU-WA-006 — Eliminar/desactivar

1. Parsear y resolver producto/presentación.
2. Si tiene stock > 0 → proponer desactivación (no eliminación física) y advertir el stock.
3. Requerir confirmación explícita.
4. Confirmado → desactivar y registrar auditoría.

## Reglas de interacción del bot

- Un comando por mensaje (v1). Dos intenciones → pedir separarlas.
- Palabras de confirmación: `SI`, `CONFIRMO`, `OK`, `DALE`. Cancelación: `NO`, `CANCELAR`, `CANCELO`.
- Mensaje irreconocible → respuesta de ayuda con ejemplos.
- Confirmaciones asociadas al mismo número de WhatsApp que originó la intención.
