# 10 - Reportes del administrador

## Consideraciones generales

- Montos en ARS formateados con 2 decimales (internamente centavos).
- Rango de fechas configurable; por defecto "hoy".
- Solo los ve el Admin (`reportes.ver`, `ganancias.ver`).
- Se leen del cloud; pueden tener segundos de atraso si el POS no sincronizó (RNF-010).
- Usan **snapshots** de `LineaVenta`, no precios actuales: las ventas históricas no se re-escriben.

## Reportes v1

### R1 — Ventas por período

| Métrica | Fórmula |
|---|---|
| Total vendido | SUM(total) de ventas en el rango |
| Cantidad de ventas | COUNT(ventas) |
| Ticket promedio | Total / Cantidad |
| Por medio de pago | SUM(pago.monto) GROUP BY medio |
| Por cajero | SUM(total) GROUP BY caja.usuario |
| Por día | SUM(total) GROUP BY fecha (gráfico) |

### R2 — Ganancias (margen bruto)

| Métrica | Fórmula |
|---|---|
| Ingresos | SUM(linea.subtotal) |
| Costo de lo vendido | SUM(cantidad × costo de la presentación al momento de la venta) |
| Ganancia bruta | Ingresos − Costo |
| Margen % | Ganancia / Ingresos × 100 |

> En v1 el costo se toma como el último `precio_costo` conocido de la presentación. Con el módulo de compras futuro, el costo se fijará por lote (ver `11-hoja-de-ruta.md`).

### R3 — Ganancias por producto

- Por producto/presentación: unidades, ingresos, costo, ganancia, margen %.
- Ordenado por ganancia desc.
- Incluye productos con costo cargado y cero ventas en el rango (detecta mercadería quieta).

### R4 — Movimientos de stock

- Lista de `MovimientoStock` con filtros (presentación, tipo, rango, origen, usuario).
- Incluye stock actual por presentación (suma de movimientos) y alerta de mínimo configurado.

### R5 — Cierres de caja

| Campo | Definición |
|---|---|
| Cajero | Usuario que abrió la caja |
| Apertura / cierre | Fechas y horas |
| Monto inicial | Al abrir |
| Esperado | Inicial + ventas registradas |
| Declarado | Arqueo físico |
| Diferencia | Declarado − Esperado |

Cajas con diferencia ≠ 0 resaltadas. Filtros: cajero, rango, solo diferencias.

### R6 — Ranking de productos

- Top N por unidades vendidas y por ingresos, con % del total.
- N configurable (por defecto 10).

### R7 — Auditoría

- Eventos con canal, actor, tipo, timestamp y detalle.
- En WhatsApp: enlace a la intención (mensaje original + comando + decisión).
- Filtros: canal, actor, tipo, rango.

## Formato de salida

- En pantalla (panel web) con exportación a **CSV** en v1 (RF-036).
- El ticket del POS es independiente de los reportes (no fiscal).

## Prioridad de implementación

1. R1 Ventas por período
2. R5 Cierres de caja
3. R4 Movimientos de stock
4. R2/R3 Ganancias
5. R6 Ranking
6. R7 Auditoría
