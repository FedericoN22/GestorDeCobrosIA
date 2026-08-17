---
description: Implementa una funcionalidad concreta sin expandir su alcance
---

Implementá únicamente la funcionalidad indicada por el usuario.

Consultá primero:

@docs/99-estado-actual.md

Después revisá únicamente los archivos directamente relacionados con la solicitud.

No explores el repositorio completo salvo que exista una dependencia directa necesaria.

Antes de modificar código:

1. Identificá los archivos que serán modificados.
2. Verificá las reglas de negocio involucradas.
3. Verificá las dependencias necesarias.

Durante la implementación:

- No agregues funcionalidades no solicitadas.
- No hagas refactors no relacionados.
- Mantené los cambios mínimos necesarios.
- Respetá las reglas de dominio.
- Respetá el aislamiento por ComercioId.
- Respetá autorización e idempotencia.
- Mantené atomicidad en operaciones críticas.
- La IA nunca ejecuta reglas de negocio.
- Mantené o agregá únicamente los tests directamente relacionados.

Al finalizar:

1. Ejecutá únicamente los tests relevantes.
2. Informá los archivos modificados.
3. Informá los tests ejecutados.
4. Informá cualquier problema pendiente.

No continúes con otra funcionalidad.

Solicitud:

$ARGUMENTS
