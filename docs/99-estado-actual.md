{
"$schema": "https://opencode.ai/config.json",

// ============================================================
// MODELO PRINCIPAL
// ============================================================

"model": "opencode/gpt-5.6-luna",

// ============================================================
// COMPACTACIÓN
// ============================================================

"compaction": {
"auto": true,
"prune": true,
"keep": {
"tokens": 8000
},
"buffer": 20000
},

// ============================================================
// WARMING
// ============================================================

"warming": false,

// ============================================================
// AGENTES
// ============================================================

"agent": {
"reviewer": {
"description": "Revisor de código de solo lectura para GestorDeCobrosIA",

      "mode": "subagent",

      "model": "opencode/minimax-m3",

      "prompt": "{file:./.opencode/agents/reviewer.md}",

      "tools": {
        "write": false,
        "edit": false
      }
    }

},

// ============================================================
// COMANDOS
// ============================================================

"command": {

    "analyze": {
      "description": "Analiza una funcionalidad sin modificar código",

      "template": "Analizá únicamente la funcionalidad indicada por el usuario. Consultá docs/99-estado-actual.md y solo los archivos directamente relacionados. NO modifiques archivos. Identificá dependencias, reglas de negocio, riesgos y plan de implementación. Mantené la respuesta concisa."
    },

    "review": {
      "description": "Revisa cambios sin modificar código",

      "agent": "reviewer",

      "template": "Revisá únicamente los cambios relevantes para la solicitud actual. No modifiques archivos. Priorizá bugs, reglas de dominio, seguridad, concurrencia, aislamiento por ComercioId y tests faltantes."
    },

    "audit": {
      "description": "Audita una regla crítica del sistema",

      "agent": "reviewer",

      "template": "Auditá exclusivamente la regla o flujo indicado. Seguí el dato desde su entrada hasta su persistencia/ejecución. No modifiques archivos. Buscá especialmente fugas de ComercioId, bypass de autorización, operaciones no idempotentes, stock negativo y ejecución sin confirmación."
    },

    "test": {
      "description": "Ejecuta y analiza los tests relevantes",

      "template": "Ejecutá únicamente los tests relacionados con la funcionalidad actual. Si fallan, identificá la causa raíz. No hagas cambios salvo que el usuario lo solicite explícitamente."
    },

    "explain": {
      "description": "Explica código existente sin modificarlo",

      "template": "Explicá exclusivamente el código indicado. No modifiques archivos. Describí flujo, responsabilidades, dependencias y decisiones importantes. Evitá explicar código no relacionado."
    }

}
}
