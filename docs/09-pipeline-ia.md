# 09 - Pipeline de IA y WhatsApp

## Objetivo

Transformar lenguaje natural (texto o audio) en un **comando estructurado confiable**. La IA es un *parser* (P1); el backend es el ejecutor. Si no puede traducir con confianza, pide aclaración (P8).

## Flujo de WhatsApp

```
┌──────────┐   ┌────────┐   ┌──────────────┐   ┌──────────────────┐   ┌─────────┐
│ WhatsApp │──▶│  STT   │──▶│ LLM + schema │──▶│ Backend decide   │──▶│Respuesta│
│ (Meta)   │   │(audio) │   │ function     │   │ ejecutar/confirm │   │ (texto) │
└──────────┘   └────────┘   │ calling      │   │ aclarar/rechazar │   └─────────┘
                           └──────────────┘   └──────────────────┘
```

Pasos:

1. **Ingreso:** webhook de Meta Cloud API (texto o audio).
2. **Seguridad:** whitelist + verificación de firma antes de todo (R-WA-001).
3. **Preprocesamiento:** si es audio → STT. Normalizar texto.
4. **Parsing:** LLM con *function calling* y schema estricto → `StructuredCommand` JSON.
5. **Normalización en backend:** resolver producto/presentación contra el catálogo (acentos, mayúsculas, sinónimos) — **no** en el LLM.
6. **Decisión:** el backend aplica reglas de negocio: ejecutar, confirmar, aclarar o rechazar.
7. **Persistencia:** guardar la `Intención` (mensaje, comando, decisión, resultado).
8. **Respuesta:** texto por Meta Cloud API.

## Speech to Text (STT)

- Se transcribe con un servicio STT (ej: Whisper API).
- Si la transcripción es de baja calidad, el bot pide repetir o escribir.
- El resultado se procesa idéntico al texto (R-WA-006).

## StructuredCommand

```jsonc
{
  "version": 1,
  "accion": "AGREGAR_STOCK",
  "entidad": "PRESENTACION",
  "parametros": {
    "producto": "Coca Cola",
    "presentacion": "2.25L",
    "cantidad": 12,
    "precio": 4200,
    "tipo_precio": "VENTA",        // VENTA | COSTO | NO_INDICADO
    "categoria": null
  },
  "confianza": 0.92,
  "campos_faltantes": [],
  "campos_ambiguos": [],
  "texto_original": "Agregar Coca Cola 2.25L, cantidad 12, precio 4200."
}
```

> `intent_id` lo asigna el backend, no la IA. El LLM devuelve solo el contenido de este schema.

### Acciones v1

| `accion` | Parámetros | Destructivo | Confirmación |
|---|---|---|---|
| `CONSULTAR_STOCK` | producto, presentacion? | No | No |
| `CONSULTAR_PRECIO` | producto, presentacion? | No | No |
| `LISTAR_PRODUCTOS` | categoria?, texto? | No | No |
| `AGREGAR_STOCK` | producto, presentacion, cantidad, costo? | No | No |
| `CREAR_PRODUCTO` | producto, presentacion?, precio, costo? | No | No |
| `MODIFICAR_PRECIO` | producto, presentacion, precio | Sí | Sí |
| `ELIMINAR_PRODUCTO` | producto, presentacion? | Sí | Sí |

### Semántica de campos

- `producto`: nombre genérico, resuelto por normalización contra el catálogo.
- `presentacion`: tamaño/variante. Opcional si el producto tiene una sola presentación.
- `cantidad`: entero positivo.
- `precio`: entero en pesos. `tipo_precio` distingue venta/costo; si no se aclara → `NO_INDICADO` y el bot pregunta.
- `confianza`: 0-1, estimación del LLM.
- `campos_faltantes`: obligatorios ausentes.
- `campos_ambiguos`: campos con más de una interpretación (ej: dos presentaciones "2.25L" vs "2.25 L").

## Validaciones

La tabla de decisión del backend:

| Confianza | Campos faltantes | Resultado |
|---|---|---|
| `< 0.7` | — | Pedir aclaración |
| `>= 0.7` | Sí | Pedir aclaración específica |
| `>= 0.7` | No | Ejecutar, o confirmar si es destructivo |

**Regla de oro:** el backend nunca ejecuta un comando con confianza baja ni completa campos por su cuenta (RNF-002).

## Confirmaciones

- Timeout de confirmación: **2 minutos** (configurable).
- Confirmar: `SI`, `CONFIRMO`, `OK`, `DALE`. Cancelar: `NO`, `CANCELAR`, `CANCELO`.
- La confirmación se asocia al mismo número que originó la intención (R-WA-007).
- Las confirmaciones destructivas muestran el **detalle completo**, incluido el estado actual ("Stock actual: 12. Queda: 0").
- Mientras una intención está en `ESPERANDO_CONFIRMACION`, un nuevo mensaje del mismo número se interpreta como respuesta (confirmación/cancelación) si coincide; si no, reemplaza la pendiente.

## Estados de conversación

| Estado | Significado |
|---|---|
| `RECIBIDA` | Llegó y se procesa |
| `PARSEADA` | Se obtuvo el `StructuredCommand` |
| `ACLARACION` | Falta información; el bot preguntó |
| `ESPERANDO_CONFIRMACION` | Acción destructiva; se pidió confirmación |
| `EJECUTADA` | Se aplicó |
| `CANCELADA` | Canceló o expiró timeout |
| `RECHAZADA` | No interpretable / inválida |
| `ERROR` | Falló la ejecución |

## Estrategia de prompting

- **Function calling / tool use** del proveedor con schema estricto (no texto libre).
- **Few-shot** con mensajes reales del dominio ("agregá 12 coca 2.25", "cuánto sale la quilmes").
- El catálogo resumido se inyecta cuando el comando es ambiguo, o se resuelve en backend por similitud.
- La normalización (acentos, sinónimos) se hace **en backend** contra el catálogo para acotar el error.

## Mensajes de ayuda

```
Hola 👋 Soy tu asistente del kiosco. Puedo ayudarte con:
• "¿Cuánto stock hay de [producto]?"
• "¿Cuánto sale [producto] [presentación]?"
• "Agregar [producto] [presentación], cantidad N, precio N"
• "Cambiar precio de [producto] [presentación] a N"
• "Crear producto [nombre], presentación [X], precio N"
• "Eliminar [producto] [presentación]"
```

## Auditoría de intenciones

Toda intención persiste: texto original (y transcripción si fue audio), comando estructurado, decisión y motivo, resultado, timestamp y número. Sirve para auditar, debuggear y mejorar prompts con datos reales.

## Límites y guardas

- **Un comando por mensaje** (R-WA-007); dos intenciones → pedir separar.
- **Rate limiting** por número.
- Whitelist validada **antes** de llamar al LLM.
- El bot nunca revela datos de otros comercios ni configuración interna.

## Selección de proveedor

| Criterio | Peso |
|---|---|
| Function calling con JSON estricto | Alta |
| STT de calidad en español rioplatense | Alta |
| Latencia y costo por mensaje | Media |
| Facilidad de cambio (adapter, P6) | Alta |
