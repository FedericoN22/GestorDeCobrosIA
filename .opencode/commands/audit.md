---
description: Audita una regla crítica del sistema
agent: reviewer
---

Auditá exclusivamente:

$ARGUMENTS

Seguí el flujo completo:

entrada
→ contexto
→ autorización
→ servicio
→ dominio
→ persistencia
→ efecto

Buscá especialmente:

- Fugas de ComercioId
- Acceso cruzado entre comercios
- Bypass de autorización
- Ejecución sin confirmación
- Stock negativo
- Falta de atomicidad
- Falta de idempotencia
- Auditoría incompleta

NO modifiques archivos.

Informá únicamente problemas reales o riesgos concretos.
