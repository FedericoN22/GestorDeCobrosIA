---
description: Revisa código sin modificar archivos
agent: reviewer
---

Revisá únicamente los cambios relacionados con:

$ARGUMENTS

Priorizá:

- Bugs
- Reglas de dominio
- Seguridad
- Concurrencia
- Atomicidad
- Idempotencia
- Aislamiento por ComercioId
- Tests faltantes

No modifiques archivos.

Para cada problema encontrado indicá:

- Severidad
- Archivo
- Método
- Problema
- Consecuencia
- Solución recomendada

Si no encontrás problemas críticos, indicá:

"Sin problemas críticos encontrados."
