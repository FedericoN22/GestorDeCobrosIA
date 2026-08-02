# 00 - Principios del proyecto

## Filosofía

Este proyecto no es otro sistema de gestión tradicional. Su propósito es **reducir el tiempo que el comerciante dedica a tareas administrativas** mediante automatización e Inteligencia Artificial.

El diferencial es la operación por **WhatsApp**: el administrador envía texto o audio y el sistema ejecuta operaciones sobre el negocio (stock, precios, productos, consultas).

La relación con la IA es la siguiente: la IA **no ejecuta ninguna regla de negocio**. Es únicamente un *parser* que transforma lenguaje natural en un comando estructurado. Toda la lógica, validación, permiso, persistencia y auditoría vive en el backend y el dominio.

## Principios arquitectónicos

Estos principios son vinculantes. Toda decisión de diseño o implementación debe respetarlos. En los documentos siguientes se referencia su número (P1…P8).

### P1 — La lógica de negocio nunca depende de la IA

**Qué implica:** el dominio no conoce a ningún proveedor de IA. La IA produce un `StructuredCommand` (DTO) y nada más. Cambiar de proveedor, eliminar la IA o agregar un canal nuevo no altera las reglas del negocio.

**Cómo se garantiza:** arquitectura en capas con dependencias hacia el centro; la IA es un adapter detrás de una interfaz (`IIaParser`). El backend valida y ejecuta todo.

### P2 — Toda operación importante debe quedar auditada

**Qué implica:** cada operación sensible registra canal, actor, timestamp y detalle. Las operaciones de WhatsApp guardan además el mensaje original, el comando estructurado, la decisión y el resultado.

**Cómo se garantiza:** registro de auditoría *append-only* (no se edita ni borra). Los movimientos de stock también son inmutables.

### P3 — Nunca se debe perder información

**Qué implica:** no hay borrados físicos destructivos de información de negocio. Se desactiva, no se elimina. Las ventas y los movimientos de stock son inmutables. Nada se pierde por falta de conexión.

**Cómo se garantiza:** baja lógica, `operationId` idempotente para el sync offline, cola persistida localmente, backups y contingencias documentadas.

### P4 — La experiencia del cajero tiene prioridad sobre la complejidad administrativa

**Qué implica:** la atención al cliente es el corazón del negocio. Vender, cobrar e imprimir el ticket debe ser rápido y funcionar siempre. La complejidad administrativa (reportes, configuración) vive en el panel del admin, no en la pantalla del cajero.

**Cómo se garantiza:** el POS desktop prioriza velocidad (búsqueda, venta, cobro, ticket) y funciona offline.

### P5 — El sistema debe seguir funcionando para vender incluso sin conexión a Internet

**Qué implica:** la falta de internet nunca detiene la venta. El POS registra localmente y sincroniza cuando hay conectividad.

**Cómo se garantiza:** offline-first con base local (SQLite), cola de operaciones y sync idempotente. El stock se modela como movimientos (suma conmutativa) para evitar conflictos de sync.

### P6 — Toda integración externa debe poder reemplazarse sin modificar el dominio

**Qué implica:** proveedor de IA, WhatsApp (Meta Cloud API), impresión y persistencia son intercambiables.

**Cómo se garantiza:** el dominio depende de interfaces definidas en la capa de aplicación (`IIaParser`, `IStt`, `IWhatsAppSender`, `ITicketPrinter`, repositorios). La infraestructura provee implementaciones.

### P7 — Las reglas del negocio viven únicamente en el dominio

**Qué implica:** ninguna validación de negocio se repite ni se decide en la UI, en la IA, en el API ni en la base de datos.

**Cómo se garantiza:** entidades y servicios de dominio contienen las reglas e invariantes. La UI y los canales solo invocan casos de uso.

### P8 — La confiabilidad tiene prioridad sobre la flexibilidad

**Qué implica:** si la IA tiene dudas, pide confirmación o aclaración. **Nunca adivina ni completa datos por su cuenta.** Es preferible una operación que pregunta antes que una que asume mal.

**Cómo se garantiza:** umbral de confianza mínimo, schema estricto de comandos, `campos_faltantes`/`campos_ambiguos`, confirmación obligatoria para acciones destructivas y auditoría que permite medir y mejorar el parseo.

## Regla de oro

> La IA traduce. El backend decide. La IA nunca ejecuta. El backend nunca adivina.
