# GestorDeCobrosIA

Sistema de gestión para kioscos y pequeños comercios, actualmente en desarrollo, orientado a reducir tareas administrativas mediante automatización, integración con **WhatsApp** e **Inteligencia Artificial**.

El objetivo no es simplemente agregar IA a un sistema de gestión, sino diseñar una arquitectura donde la IA pueda interpretar lenguaje natural sin tener autoridad sobre la lógica de negocio.

---

## 🚧 Project Status

**Status:** Active development

El proyecto se encuentra en desarrollo activo. Las primeras fases se enfocaron en definir el dominio, establecer los límites arquitectónicos y construir las bases de persistencia, autenticación, stock e integración con WhatsApp.

| Área                          | Estado           |
| ----------------------------- | ---------------- |
| Arquitectura                  | ✅ Implementado  |
| Modelo de dominio             | ✅ Implementado  |
| Autenticación                 | ✅ Implementado  |
| Catálogo                      | ✅ Implementado  |
| Gestión de stock              | ✅ Implementado  |
| WhatsApp Webhook              | ✅ Implementado  |
| Idempotencia de Webhooks      | ✅ Implementado  |
| Aislamiento por comercio      | ✅ Implementado  |
| Procesamiento de intenciones  | ✅ Implementado  |
| Pipeline IA                   | 🟡 En desarrollo |
| POS Offline-first             | 🟡 En desarrollo |
| Sincronización offline/online | 🟡 En desarrollo |
| Reportes avanzados            | 🔴 Pendiente     |
| Hardening y pruebas reales    | 🔴 Pendiente     |

El roadmap completo se encuentra en [`docs/11-hoja-de-ruta.md`](docs/11-hoja-de-ruta.md).

---

## 🎯 Visión y Propósito

### Problema

Los kioscos y pequeños comercios suelen dedicar una cantidad considerable de tiempo a tareas administrativas repetitivas:

- actualización de productos y precios;
- gestión de stock;
- operaciones de caja;
- consultas de información;
- registro de movimientos.

El objetivo del proyecto es reducir esa carga sin introducir un sistema excesivamente complejo o costoso para el tamaño del negocio.

### Diferencial

La principal característica del sistema es la integración con **WhatsApp** como canal operativo.

El comerciante puede utilizar lenguaje natural, mediante texto o audio, para expresar una intención:

```text
"Agregá 10 Coca Cola de un litro y medio"
```

El sistema transforma esa entrada en un comando estructurado:

```text
Natural Language
       ↓
   STT / LLM
       ↓
StructuredCommand
       ↓
Application
       ↓
Domain
       ↓
Business Operation
```

La IA **interpreta**, pero no ejecuta directamente la lógica de negocio.

---

## 🧠 AI as a Parser, Not an Executor

Una de las decisiones arquitectónicas centrales del proyecto es mantener la Inteligencia Artificial fuera de la lógica de negocio.

La IA puede producir una interpretación incorrecta. Por eso su salida se considera **input no confiable**.

El flujo conceptual es:

```text
WhatsApp message
       ↓
   Whitelist
       ↓
   STT (audio)
       ↓
   LLM / Parser
       ↓
StructuredCommand
       ↓
Validation
       ↓
Authorization
       ↓
Domain Rules
       ↓
Execution
       ↓
Audit
```

La IA no puede:

- modificar directamente la base de datos;
- decidir reglas de negocio;
- ejecutar operaciones sin autorización;
- saltarse validaciones;
- modificar stock directamente.

Las reglas de negocio permanecen dentro del dominio y la aplicación.

---

## ✨ Principales características

- Gestión de productos y catálogo.
- Gestión de stock.
- Autenticación y autorización.
- Separación entre usuarios administrativos y operaciones de caja.
- Integración con WhatsApp Cloud API.
- Procesamiento de mensajes mediante webhook.
- Interpretación de texto y audio mediante IA.
- Conversión de lenguaje natural a comandos estructurados.
- Confirmación explícita de operaciones destructivas.
- Idempotencia de mensajes recibidos mediante webhook.
- Aislamiento de operaciones por `ComercioId`.
- Auditoría de operaciones críticas.
- Operaciones de stock atómicas.
- Diseño orientado a funcionamiento offline.
- Persistencia compatible con PostgreSQL y SQLite.
- Tests automatizados.
- CI.

---

## 🏗️ Arquitectura

El proyecto utiliza **Clean Architecture**, manteniendo las reglas de negocio independientes de frameworks e integraciones externas.

```text
                    ┌──────────────────────┐
                    │       Kiosk.Web      │
                    │   Admin Dashboard    │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │       Kiosk.Api      │
                    │ REST API / Webhooks  │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │   Kiosk.Application  │
                    │      Use Cases       │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │     Kiosk.Domain     │
                    │ Business Rules       │
                    └──────────────────────┘
                               ▲
                               │
                    ┌──────────┴───────────┐
                    │  Kiosk.Infrastructure│
                    │ DB / WhatsApp / APIs │
                    └──────────────────────┘

             ┌───────────────────────────────┐
             │          Kiosk.Ia             │
             │      STT + LLM + Parser       │
             └───────────────────────────────┘

             ┌───────────────────────────────┐
             │          Kiosk.Pos            │
             │       WPF / Offline POS       │
             └───────────────────────────────┘
```

### Proyectos principales

#### `Kiosk.Domain`

Contiene:

- entidades;
- value objects;
- invariantes;
- reglas de negocio;
- conceptos centrales del dominio.

No depende de infraestructura ni de servicios externos.

#### `Kiosk.Application`

Contiene:

- casos de uso;
- interfaces/puertos;
- orquestación;
- validaciones;
- coordinación entre dominio e infraestructura.

#### `Kiosk.Infrastructure`

Contiene las implementaciones concretas:

- Entity Framework Core;
- PostgreSQL;
- SQLite;
- WhatsApp Cloud API;
- servicios externos;
- persistencia;
- integraciones.

#### `Kiosk.Api`

Expone:

- API HTTP;
- endpoints;
- autenticación;
- webhook de WhatsApp.

#### `Kiosk.Pos`

Aplicación de punto de venta basada en WPF, diseñada bajo un enfoque **offline-first**.

#### `Kiosk.Web`

Panel web para administración del sistema.

#### `Kiosk.Ia`

Pipeline relacionado con Inteligencia Artificial:

- Speech-to-Text;
- LLM;
- parsing;
- transformación a comandos estructurados.

---

## 🔐 Engineering Decisions

### 1. La IA no ejecuta lógica de negocio

La salida del LLM se trata como input no confiable.

Esto permite mantener:

```text
AI ≠ Business Logic
```

La aplicación y el dominio siguen siendo responsables de validar y ejecutar las operaciones.

---

### 2. Idempotencia de Webhooks

Los webhooks pueden ser enviados más de una vez.

Para evitar que un mismo mensaje produzca dos operaciones, el sistema registra los mensajes procesados y utiliza una restricción única basada en el comercio y el identificador del mensaje.

```text
WhatsApp Webhook
       ↓
MessageId
       ↓
Already processed?
    ↙       ↘
  YES        NO
   ↓          ↓
 Ignore     Process
              ↓
           Persist
```

Esto evita duplicaciones causadas por reintentos del proveedor.

---

### 3. Aislamiento por comercio

Las operaciones relacionadas con el negocio están delimitadas mediante `ComercioId`.

El identificador del comercio no se obtiene del contenido enviado por el usuario.

El flujo establece el contexto de comercio antes de procesar la operación:

```text
WhatsApp
   ↓
Whitelist
   ↓
ComercioId
   ↓
Application
   ↓
Domain
```

Esto evita que un mensaje pueda seleccionar arbitrariamente otro comercio.

---

### 4. Stock atómico

Las operaciones de stock validan el estado proyectado antes de persistir cambios.

Una operación nunca debe permitir que el stock quede por debajo de cero.

```text
Current Stock
      +
Requested Change
      ↓
Projected Stock
      ↓
Valid?
   ↙     ↘
 YES      NO
  ↓        ↓
Commit    Reject
```

---

### 5. Confirmación de operaciones destructivas

Las operaciones sensibles no se ejecutan automáticamente a partir de una interpretación de IA.

Cuando corresponde, el sistema genera una intención pendiente y requiere confirmación explícita antes de ejecutar la operación.

```text
Message
   ↓
AI Parser
   ↓
Intent
   ↓
Destructive?
  ↙      ↘
 NO       YES
 ↓         ↓
Execute   Confirm
             ↓
          Execute
```

---

### 6. Auditoría

Las operaciones críticas deben poder ser rastreadas posteriormente.

La auditoría busca proporcionar:

- trazabilidad;
- contexto de la operación;
- identificación del actor;
- registro de acciones sensibles.

El objetivo es que una operación importante no desaparezca sin dejar evidencia.

---

## 📱 WhatsApp + AI Pipeline

El pipeline de procesamiento está diseñado para separar claramente comunicación, interpretación y ejecución.

```text
User
 │
 │ Text / Audio
 ▼
WhatsApp Cloud API
 │
 ▼
Webhook
 │
 ├── Idempotency
 │
 ├── Authorization / Whitelist
 │
 ▼
STT
 │
 ▼
LLM
 │
 ▼
StructuredCommand
 │
 ▼
Application
 │
 ▼
Domain
 │
 ▼
Persistence
 │
 ▼
Audit
```

Este diseño permite cambiar el proveedor de IA sin convertir la IA en una dependencia del dominio.

---

## 📴 Offline-first

Una de las premisas del proyecto es:

> **El comercio no debería dejar de vender porque Internet dejó de funcionar.**

El POS está diseñado para poder operar localmente y posteriormente sincronizar información cuando la conectividad esté disponible.

La implementación completa del mecanismo de sincronización offline/online continúa en desarrollo.

---

## 🧪 Tests y calidad

El proyecto utiliza **xUnit** para pruebas automatizadas.

Actualmente existen suites orientadas a validar componentes críticos de:

- dominio;
- aplicación;
- lógica de stock;
- pipeline de IA;
- integración.

El objetivo es complementar estas pruebas con escenarios de integración y pruebas sobre el funcionamiento real del sistema.

Para ejecutar los tests:

```bash
dotnet test
```

El repositorio también cuenta con CI básica para automatizar verificaciones durante el desarrollo.

---

## 📋 Roadmap

### Fase 1 — Fundaciones

- Arquitectura.
- Dominio.
- Persistencia.
- Configuración inicial.
- Autenticación.

### Fase 2 — Catálogo y Stock

- Productos.
- Presentaciones.
- Precios.
- Stock.
- Reglas de inventario.

### Fase 3 — POS Offline-first

- Punto de venta.
- Operaciones locales.
- Persistencia offline.
- Sincronización.

### Fase 4 — Reportes y Auditoría

- Auditoría.
- Arqueos.
- Reportes.
- Trazabilidad.

### Fase 5 — WhatsApp + IA

- WhatsApp Cloud API.
- Webhooks.
- Speech-to-Text.
- LLM.
- Structured Commands.
- Confirmaciones.
- Integración con operaciones del dominio.

### Fase 6 — Hardening

- Pruebas de integración.
- Pruebas en escenarios reales.
- Manejo de errores.
- Observabilidad.
- Optimización.
- Seguridad.
- Pulido final.

El roadmap detallado se encuentra en [`docs/11-hoja-de-ruta.md`](docs/11-hoja-de-ruta.md).

---

## 📦 Instalación

> ⚠️ El proyecto se encuentra en desarrollo y todavía no dispone de una build estable para producción.

### Requisitos

- **.NET 10 SDK**
- PostgreSQL o SQLite
- Variables de entorno necesarias
- Credenciales/API keys de los servicios externos utilizados

### Configuración

Las credenciales y secretos deben proporcionarse mediante variables de entorno o mecanismos seguros de configuración.

**No se deben almacenar API keys ni credenciales reales dentro del repositorio.**

La configuración específica de cada módulo se encuentra documentada en los archivos correspondientes.

### Ejecutar

```bash
git clone https://github.com/FedericoN22/GestorDeCobrosIA.git
cd GestorDeCobrosIA
dotnet restore
dotnet build
dotnet test
```

La estructura de configuración y los requisitos específicos de cada módulo pueden variar mientras el proyecto continúa en desarrollo.

---

## 📚 Documentación

La carpeta [`docs/`](docs/) contiene la documentación de diseño del sistema.

Incluye:

- Principios de arquitectura.
- Visión del producto.
- Reglas del dominio.
- Requisitos funcionales y no funcionales.
- Pipeline de IA.
- Modelo de datos.
- Decisiones arquitectónicas.
- ADRs.
- Roadmap.

Documentos principales:

- [`docs/00-principios.md`](docs/00-principios.md)
- [`docs/01-vision.md`](docs/01-vision.md)
- [`docs/02-dominio.md`](docs/02-dominio.md)
- [`docs/09-pipeline-ia.md`](docs/09-pipeline-ia.md)
- [`docs/11-hoja-de-ruta.md`](docs/11-hoja-de-ruta.md)

---

## 🛡️ Filosofía de desarrollo

El proyecto se construye alrededor de algunas reglas fundamentales:

### IA ≠ lógica de negocio

La IA interpreta información. Las reglas de negocio pertenecen al dominio.

### Seguridad antes que flexibilidad

Las operaciones sensibles requieren autorización y, cuando corresponde, confirmación explícita.

### El comercio no debe dejar de vender

La conectividad no debería convertirse en un requisito absoluto para las operaciones básicas del negocio.

### Integraciones desacopladas

WhatsApp, IA, bases de datos y otros servicios externos deben permanecer detrás de abstracciones que eviten contaminar el dominio.

### Trazabilidad

Las acciones críticas deben poder ser auditadas posteriormente.

### Reglas en un único lugar

Las reglas de negocio pertenecen al dominio y no deben distribuirse arbitrariamente entre API, infraestructura o servicios externos.

---

## 🤝 Cómo contribuir

El proyecto se encuentra principalmente orientado al desarrollo y aprendizaje del autor.

Para cambios importantes de dominio o arquitectura, las decisiones deben ser discutidas y documentadas antes de su implementación.

Las propuestas deben considerar:

- impacto sobre el dominio;
- límites arquitectónicos;
- seguridad;
- consistencia;
- testabilidad;
- compatibilidad con el enfoque offline-first.

---

## 📬 Issues

Las sugerencias, errores y propuestas pueden registrarse mediante Issues del repositorio.

Para cambios relacionados con lógica central o arquitectura, se recomienda consultar primero la documentación existente en `docs/`.

---

## 👨‍💻 Author

**Federico Nunez**

Backend Developer — .NET / C#

Este proyecto forma parte de mi evolución práctica en desarrollo backend, pasando de APIs y persistencia básica hacia sistemas con arquitectura limpia, integraciones externas, procesamiento de eventos, IA y problemas de consistencia y confiabilidad.
