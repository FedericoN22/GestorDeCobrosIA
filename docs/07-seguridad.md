# 07 - Seguridad

## Autenticación

| Superficie | Mecanismo |
|---|---|
| Panel web | Login username + contraseña; sesión JWT (corta duración) o cookie autenticada |
| POS desktop | Login username + contraseña; opcional PIN rápido para retomar turno |
| API | JWT Bearer con rol y permisos |
| WhatsApp | El "actor" es el número verificado contra la whitelist; no hay contraseña |

- Contraseñas con hash BCrypt, nunca en texto plano.
- En el POS, el token se revalida en cada sync; sin conexión el turno continúa local y la autenticación se revalida al reconectar.

## Autorización

Los permisos son explícitos (RF-023); cada caso de uso declara el permiso que requiere.

| Permiso | Admin | Cajero |
|---|---|---|
| `productos.consultar` | ✔ | ✔ |
| `ventas.registrar` | ✔ | ✔ |
| `cajas.abrir` / `cajas.cerrar` | ✔ | ✔ |
| `productos.gestionar` | ✔ | ✘ |
| `stock.gestionar` | ✔ | ✘ |
| `reportes.ver` / `ganancias.ver` | ✔ | ✘ |
| `usuarios.gestionar` | ✔ | ✘ |
| `config.gestionar` | ✔ | ✘ |
| `auditoria.ver` | ✔ | ✘ |
| `whatsapp.operar` | ✔ | ✘ |

Permisos v1 completos: `productos.gestionar`, `productos.consultar`, `stock.gestionar`, `stock.consultar`, `ventas.registrar`, `ventas.consultar`, `cajas.abrir`, `cajas.cerrar`, `cajas.consultar`, `reportes.ver`, `ganancias.ver`, `usuarios.gestionar`, `config.gestionar`, `auditoria.ver`, `whatsapp.operar`.

## Auditoría

- `AuditoriaEvento` (append-only, INV-005) registra cada operación sensible: quién (usuario o número WhatsApp), canal, tipo, detalle JSON, timestamp.
- Las intenciones de WhatsApp guardan mensaje original + comando + decisión + resultado.
- El Admin consulta la auditoría con filtros por canal, actor, tipo y rango (RF-038).

## Gestión de secretos

| Secreto | Dónde se guarda |
|---|---|
| Meta Cloud API token | Secret manager / env de infraestructura |
| API keys del proveedor IA | Secret manager / env |
| Connection string PostgreSQL | Secret manager / env |
| Contraseñas de usuarios | Solo hash BCrypt en BD |

**Nunca** en: repositorio, logs, respuestas del bot, mensajes de error.

## Seguridad del canal WhatsApp

El canal más expuesto. Mitigaciones:

1. **Whitelist estricta** validada **antes** de invocar la IA (no se gastan tokens ni se expone información). RF-024 / R-WA-001.
2. **Sin información sensible en respuestas** a números no autorizados.
3. **Confirmación en acciones destructivas** (`MODIFICAR_PRECIO`, `ELIMINAR_PRODUCTO`). R-WA-004.
4. **Rate limiting** por número (ej: 10/min). Ante abuso: silencio y notificación al Admin.
5. **Webhook verificado por firma** (`X-Hub-Signature-256`) y token de verificación de Meta.
6. **Confirmaciones atadas al número** que originó la intención: otro número autorizado no puede confirmar por error.
7. **Timeout** de confirmaciones para no dejar acciones destructivas pendientes indefinidamente.

## Protección del API

- Todas las rutas exigen JWT salvo: login, webhook de WhatsApp (firmado) y healthcheck.
- Autorización por permiso en cada endpoint.
- `comercio_id` se deriva del token, nunca del cliente (previene accesos cruzados; deja listo el multi-tenant).
- Validación de entrada en todos los endpoints (montos enteros positivos, rangos).
- Rate limiting global por IP; CORS restringido al origen del panel.

## Riesgos y mitigaciones

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Suplantación en WhatsApp | Alto | Whitelist + firma de webhook + confirmaciones atadas al número + rate limiting |
| Fuga de secretos | Alto | Secret manager, nunca en repo/logs; revisión en CI |
| Acceso cruzado entre comercios | Alto | `comercio_id` derivado del token, no del cliente |
| Comandos destructivos por error | Medio | Confirmación explícita con detalle y timeout |
| Auditoría manipulada | Medio | Registros append-only e inmutables |
| Token robado del POS | Medio | Tokens de corta duración, revalidación en cada sync |
| Abuso del webhook (spam) | Medio | Firma de Meta + rate limiting |

## Checklist de revisión

- [ ] Webhook verifica firma antes de procesar.
- [ ] Números no autorizados nunca llaman al LLM.
- [ ] Endpoints con autorización por permiso (no solo login).
- [ ] Montos enteros validados en entrada.
- [ ] Sin secretos en errores ni logs.
- [ ] Auditoría registra todas las escrituras sensibles.
- [ ] Confirmaciones asociadas al número de la intención original.
