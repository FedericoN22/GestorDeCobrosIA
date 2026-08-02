# 11 - Hoja de ruta del MVP

## Objetivo de la fase 0

Tener el sistema funcionando de punta a punta para **un solo comercio real**, con lo mínimo viable: POS que vende online/offline, panel web de admin y bot de WhatsApp que opera stock y precios de forma confiable.

## Fases

### Fase 1 — Fundaciones (semanas 1-3)

**Entregables:**
- Esqueleto .NET con Clean Architecture (módulos core de `06-arquitectura.md`).
- Schema inicial con migraciones (PostgreSQL + SQLite).
- Autenticación JWT y permisos (RF-021…023).
- Seed de comercio y usuario admin.
- CI básica (build + tests del dominio).

**Criterios de salida:** el API responde, login funciona, migraciones aplican en ambos proveedores, tests del dominio pasan.

### Fase 2 — Catálogo y stock (semanas 3-5)

**Entregables:**
- CRUD de categorías, productos y presentaciones (RF-001…007).
- Stock como movimientos: entrada manual, ajuste (RF-008…012).
- Cálculo de stock actual por snapshot cacheado.
- Panel web con pantallas de catálogo y stock.

**Criterios de salida:** el Admin crea productos con presentaciones, carga stock y ve existencias en el panel.

### Fase 3 — POS offline-first (semanas 5-8)

**Entregables:**
- POS WPF: login, búsqueda (nombre/código/escaneo), venta, cobro (efectivo con vuelto, tarjeta, QR, mixto), ticket no fiscal (RF-013…020).
- Apertura/cierre de caja con arqueo.
- SQLite local + cola `PendingOps` + sync engine + endpoints `/sync/*` (`08-sync-offline.md`).
- Almacén local del catálogo.

**Criterios de salida:** una caja vende todo el día sin internet, sincroniza al reconectar, no duplica ni pierde ventas.

### Fase 4 — Reportes (semanas 8-10)

**Entregables:**
- R1 Ventas, R5 Cierres, R4 Stock, R2/R3 Ganancias, R6 Ranking (RF-035).
- Exportación CSV (RF-036).
- Pantalla de auditoría (RF-038).

**Criterios de salida:** el Admin responde "cuánto vendí, cuánto gané, qué diferencia hubo en caja" con el panel.

### Fase 5 — Bot de WhatsApp (semanas 10-13)

**Entregables:**
- Webhook de Meta Cloud API firmado + verificación.
- Whitelist y registro del admin (RF-024, RF-040).
- Pipeline `STT → LLM (function calling) → StructuredCommand → decisión → respuesta` (`09-pipeline-ia.md`).
- Ciclo de vida de `Intención` con estados, confirmaciones y timeout.
- Acciones v1: consultar stock/precio, listar, agregar stock, crear producto, modificar precio, eliminar.

**Criterios de salida:** el Admin agrega stock y consulta precios por WhatsApp (texto y audio) sin adivinanzas; las acciones destructivas piden confirmación.

### Fase 6 — Endurecimiento y pruebas reales (semanas 13-15)

**Entregables:**
- Prueba en un kiosco real (o simulado) durante al menos una semana.
- Casos límite: stock negativo, arqueo con diferencia, caída de internet a mitad de turno, doble envío de mensajes de WhatsApp.
- Ajuste de prompts con datos reales de auditoría.
- Backup, monitoreo y logging.

**Criterios de salida:** corrida real sin pérdida de datos ni ventas; confiabilidad de la IA dentro del objetivo (< 5% de malinterpretaciones silenciosas).

## Estimación de esfuerzo

| Fase | Esfuerzo relativo |
|---|---|
| 1 Fundaciones | 10% |
| 2 Catálogo y stock | 15% |
| 3 POS offline-first | 30% (la más compleja: sync) |
| 4 Reportes | 10% |
| 5 Bot WhatsApp | 25% |
| 6 Endurecimiento | 10% |

## Riesgos y mitigaciones

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Complejidad del sync offline | Alto | Una caja activa en v1; catálogo solo en cloud; idempotencia por `operationId`; stock por movimientos conmutativos |
| Bloqueo/costo de API de WhatsApp | Medio | API oficial desde el inicio; presupuestar costo; mensajes de ayuda bien armados |
| Calidad del parseo con IA | Alto | Schema estricto con function calling; pocos ejemplos bien elegidos; auditoría para mejorar prompts; regla "nunca adivinar" |
| Falta de internet del comercio | Bajo | Offline-first del POS (WhatsApp depende del cloud, pero el comercio igual vende) |
| Pérdida de la PC del POS | Bajo | Sync frecuente (15-30 s); backup del SQLite; contingencia documentada |

## Backlog post-MVP

- Módulo de compras a proveedores (costo por lote → precisión de R2/R3).
- Facturación fiscal (AFIP).
- Descuentos y precios especiales.
- Varias cajas simultáneas (reservas de stock) y multi-sucursal.
- Ventas fraccionadas / por peso.
- Crédito a cuenta (fiado) y clientes.
- Respuestas de voz (TTS).
- Multi-tenant (el modelo ya lo contempla: `comercio_id` en todas las tablas).
- App móvil para el cajero.

## Orden sugerido de implementación en código

1. `Kiosk.Domain` (entidades, reglas, movimientos de stock) con tests unitarios (RNF-015).
2. `Kiosk.Application` (casos de uso, puertos).
3. `Kiosk.Infrastructure` (EF Core, ambos proveedores).
4. `Kiosk.Api` (endpoints, auth).
5. `Kiosk.Web` (panel admin).
6. `Kiosk.Pos` (POS WPF + sync).
7. `Kiosk.Ia` (pipeline; al final, porque depende del API estable y del contrato del `StructuredCommand`).

## Notas de consistencia

Este set de documentos (00–11) reemplaza la versión anterior. No cambió ninguna decisión de negocio; se reorganizó el orden, se agregaron `00-principios.md` y `02-requisitos.md`, y se enriquecieron `03-dominio.md` (invariantes y eventos) y `01-vision.md` (nueva estructura). Todas las reglas y requisitos están numerados y referenciados entre documentos para rastreabilidad.
