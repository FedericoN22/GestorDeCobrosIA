# 01 - Visión del producto

## Problema que resuelve

Un comerciante de un kiosco o comercio chico dedica horas por semana a tareas administrativas repetitivas: actualizar precios, cargar stock, revisar existencias, sacar cuentas y cuadrar caja. Estas tareas lo obligan a sentarse frente a la PC o a usar papel, mientras su caja sigue funcionando pero sin información confiable.

Los sistemas de gestión existentes suelen ser pesados, caros y pensados para cadenas, no para un kiosco con una PC y un WhatsApp. El resultado: el comerciante administra "a ojo".

## Objetivos

- **Reducir el tiempo administrativo** del comerciante mediante automatización e IA.
- Permitir **operar el negocio desde WhatsApp** (texto o audio) para las tareas más frecuentes: stock, precios y productos.
- Proveer un **POS rápido y confiable** que nunca detiene la venta, incluso sin internet.
- Dar **información real** (ventas, ganancias, stock, caja) sin que el comerciante tenga que llevarla en la cabeza.
- Priorizar la **confiabilidad**: el sistema prefiere preguntar antes que equivocarse (P8).

## Público objetivo

**Administrador** (dueño/a del kiosco o comercio chico):
- Gestiona productos, categorías, stock, usuarios, cajas, configuración y reportes.
- Ve estadísticas y ganancias.
- Opera el negocio por WhatsApp.
- También puede atender la caja.

**Cajero** (empleado que atiende):
- Solo busca productos, consulta precios, registra ventas, cobra y abre/cierra su caja.
- No modifica información administrativa.

## Diferencial

La integración con **WhatsApp + IA**: el administrador envía mensajes como

> "Agregar Coca Cola 2.25L, cantidad 12, precio 4200."

y el sistema interpreta el mensaje, lo convierte en un comando estructurado, lo valida en el backend y lo ejecuta con confirmación cuando corresponde.

La IA es **solamente un parser** (P1): nunca ejecuta reglas. El backend decide, valida, persiste y audita.

## Alcance

### Incluido (v1)

- Gestión de productos, presentaciones y categorías (panel web + WhatsApp).
- Control de stock por presentación, con costo, basado en movimientos.
- POS desktop: venta, cobro en efectivo/tarjeta/QR (mixto), ticket no fiscal, apertura/cierre de caja con arqueo.
- Offline-first: el POS vende sin internet y sincroniza.
- Usuarios con roles Admin/Cajero y permisos explícitos.
- Reportes: ventas, ganancias, stock, cierres de caja, ranking, auditoría.
- Bot de WhatsApp (API oficial) con texto y audio; respuestas en texto.
- Auditoría completa de todas las operaciones sensibles.
- Un solo comercio por instalación, una caja activa a la vez.

### Excluido (fases posteriores)

- Facturación fiscal (AFIP).
- Módulo de compras a proveedores con detalle.
- Descuentos y precios especiales.
- Ventas fraccionadas / por peso.
- Crédito a cuenta (fiado) y clientes.
- Varias cajas simultáneas, multi-sucursal, multi-tenant.
- Respuestas de voz (TTS).

## MVP

El MVP es una versión que **un kiosco real puede usar como sistema único**:

1. **POS offline-first**: vende, cobra, imprime ticket y cuadra caja, con y sin internet.
2. **Panel web**: catálogo, stock, usuarios y reportes esenciales.
3. **Bot de WhatsApp**: consultar y modificar stock/precios, crear y eliminar productos, con confirmaciones.

El detalle de fases y entregables está en `11-hoja-de-ruta.md`.

## Métricas de éxito

| Métrica | Objetivo |
|---|---|
| Tiempo administrativo | Reducción medible de minutos/día del comerciante en tareas administrativas |
| Adopción WhatsApp | ≥ 50% de las operaciones de stock y precios se hacen por WhatsApp |
| Confiabilidad de la IA | < 5% de comandos malinterpretados sin aviso (toda ambigüedad se resuelve preguntando) |
| Cero corrupción de datos | Ninguna operación produce stock negativo ni ventas inconsistentes, incluso offline |
| Cuadre de caja | Los cierres de caja coinciden con el arqueo declarado (diferencias explicadas) |
| Disponibilidad | El negocio nunca deja de vender por falta de conexión |
