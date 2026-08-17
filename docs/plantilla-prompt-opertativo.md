Trabajá sobre GestorDeCobrosIA.

Antes de comenzar:

1. Leé únicamente:
   - docs/99-estado-actual.md
   - el documento de la fase actual
   - los archivos directamente relacionados con la tarea.

2. No recorras todo el repositorio.

3. No leas documentación histórica salvo que sea necesaria para la tarea.

4. No modifiques archivos todavía.

Primero:

- identificá qué hay que hacer;
- indicá los archivos que probablemente estarán involucrados;
- señalá riesgos o inconsistencias.

Después de mi confirmación, implementá únicamente el objetivo indicado.

Restricciones:

- No agregar funcionalidades no solicitadas.
- No modificar arquitectura sin justificarlo.
- Mantener las reglas de negocio en el dominio.
- Mantener aislamiento por ComercioId.
- Mantener idempotencia donde corresponda.
- Mantener atomicidad de operaciones críticas.
- La IA nunca ejecuta reglas de negocio.
- Las operaciones sensibles requieren confirmación.
- No modificar archivos no relacionados.

Al finalizar:

- ejecutá únicamente los tests relevantes;
- informá qué cambió;
- informá qué tests se ejecutaron;
- informá problemas pendientes;
- actualizá docs/99-estado-actual.md si el estado del proyecto cambió.
