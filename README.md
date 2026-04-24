# ERP Definiciones

> Repositorio de **diseño de dominio conversacional con IA** para construir las especificaciones de un ERP multi-tenant. Cada sub-dominio se modela como un artefacto de DDD/Event Sourcing/EDA lo suficientemente refinado para que el equipo de desarrollo lo implemente sin zonas grises.

No hay código aquí. Hay **contratos de dominio**: lo que el negocio hace, cómo se comporta, qué eventos emite y qué reglas debe respetar.

---

## 🎯 Para quién es este repo

| Si eres... | Te sirve para... |
|------------|------------------|
| **Arquitecto o modelador** que quiere aprender este enfoque | Ver modelos DDD/ES/EDA reales, completos, con sus decisiones explícitas |
| **Equipo de desarrollo** que va a implementar un sub-dominio | Leer el alcance + modelo de ese sub-dominio como especificación |
| **Colaborador** de un sub-dominio existente | Extender el modelo siguiendo el flujo y convenciones establecidas |
| **Arquitecto** que arranca un nuevo sub-dominio | Copiar las plantillas y el flujo de 3 fases |

---

## 🧭 Enfoque en una imagen

```
                  ┌───────────────────────────────────┐
                  │   CONVERSACIÓN ITERATIVA CON IA   │
                  └───────────────────────────────────┘
                                   │
           ┌───────────────────────┼────────────────────────┐
           ▼                       ▼                        ▼
   ┌───────────────┐       ┌───────────────┐       ┌────────────────┐
   │   FASE 1      │       │   FASE 2      │       │   FASE 3       │
   │   Alcance     │ ───▶  │   Modelo de   │ ───▶  │  EventCatalog  │
   │  (el QUÉ)     │       │   dominio     │       │  (visual)      │
   │               │       │  (el CÓMO)    │       │                │
   └───────────────┘       └───────────────┘       └────────────────┘
   definicion-            modelo-dominio.md         (pendiente)
   alcance.md             + /audit (10 skills)
```

Cada sub-dominio atraviesa estas fases. Los artefactos quedan versionados en el repo como fuente de verdad.

---

## 📦 Qué hay hoy en el repo

### Sub-dominios de negocio (`dominio/`)

| Sub-dominio | Alcance | Modelo | Estado |
|-------------|---------|--------|--------|
| [Obligaciones por Pagar](dominio/obligaciones-por-pagar/) | ✅ v1 | ✅ v2.9 | 🟡 En refinamiento (Fase 2) |
| [Impuestos](dominio/impuestos/) | ✅ v1.1 | ✅ v1.3 | 🟢 Completo — listo para desarrollo |
| [Contabilidad](dominio/contabilidad/) | ✅ v1.0 | ✅ v1.0 | 🟢 Completo — listo para desarrollo (F1) |
| [Terceros](dominio/terceros/) | ✅ v1.0 | ✅ v1.0 | 🟢 Completo — listo para desarrollo |
| [Estructura Organizacional](dominio/estructura-organizacional/) | ⬜ | ⬜ | 🔴 Solo anexo de contexto |
| *Tesorería* | — | — | ⚪ No iniciado |
| *Emisión Electrónica* | — | — | ⚪ No iniciado |
| *Recepción Electrónica* | — | — | ⚪ No iniciado |

### Servicios compartidos (`compartido/`)

| Servicio | Estado |
|----------|--------|
| [Datos de Referencia](compartido/datos-referencia/) (países, monedas, tipos de documento, divisiones territoriales) | 🟢 Alcance v1.0 + especificación v1.0 |
| [Direcciones](compartido/direcciones/) | 🟢 Alcance v1.0 + especificación v1.0 |

> Los servicios compartidos viven en el *application plane* pero no son dominio de negocio. Se consumen desde los sub-dominios.

---

## 🗂️ Estructura del repositorio

```
erp-definiciones/
│
├── dominio/                         ← Bounded contexts de negocio
│   ├── obligaciones-por-pagar/
│   │   ├── definicion-alcance.md    ← el QUÉ
│   │   ├── modelo-dominio.md        ← el CÓMO
│   │   └── anexo-*.md               ← decisiones, análisis, ejemplos (ver §Anexos)
│   ├── impuestos/
│   ├── contabilidad/
│   ├── terceros/
│   └── estructura-organizacional/
│
├── compartido/                      ← Servicios de plataforma no-negocio
│   ├── datos-referencia/
│   └── direcciones/
│
├── integraciones/
│   ├── entre-dominios/              ← Contratos entre sub-dominios propios
│   └── externas/                    ← Conectores con sistemas de terceros
│
├── plataforma-saas/                 ← Control plane (futuro: tenant, identity, billing)
│
├── plantillas/                      ← Punto de partida para nuevos sub-dominios
│   ├── definicion-alcance.md
│   ├── modelo-dominio.md
│   ├── definicion-alcance-servicio.md
│   └── especificacion-servicio.md
│
├── guias-de-modelado/               ← Criterios transversales
│   ├── arquitectura-eda.md
│   ├── modelar-agregados.md
│   └── separacion-responsabilidades.md
│
├── auditoria/                       ← Reportes de las skills de auditoría
├── fuentes/                         ← Referencias externas (PDFs, papers)
│
├── .claude/
│   ├── commands/                    ← /audit, /relacion-cambios
│   └── skills/                      ← 10 skills de auditoría + orquestador
│
├── CLAUDE.md                        ← Instrucciones operativas para la IA
└── README.md                        ← Este archivo
```

---

## 🧩 Conceptos base

- **Dominio** — el problema completo: un ERP.
- **Sub-dominio** — cada área especializada: OXP, Impuestos, Contabilidad, etc. Tiene vocabulario propio, reglas propias y ciclo de vida independiente, pero sirve al mismo negocio.
- **Bounded Context** — frontera de consistencia de un sub-dominio. Coincide con cada carpeta de `dominio/`.
- **Lenguaje ubicuo** — cada sub-dominio mantiene su glosario canónico en su `definicion-alcance.md`.

---

## 📑 Documentos anexos

Además del **alcance** y del **modelo de dominio**, cada sub-dominio puede tener **anexos** — documentos satélite que sostienen el modelo sin contaminarlo. El alcance y el modelo responden *qué* y *cómo*; los anexos responden *por qué*, *cómo lo hace la industria* y *cómo se ve en la práctica*.

Los anexos viven junto al sub-dominio al que pertenecen y se nombran con prefijo `anexo-<tipo>-*.md`.

### Tipos de anexo

| Prefijo | Tipo | Propósito | Ejemplo |
|---------|------|-----------|---------|
| `anexo-decision-*` | **Decisión de diseño** | Documenta el *porqué* de una elección arquitectónica: problema, alternativas evaluadas con trade-offs, decisión tomada y justificación. Preserva el contexto de decisiones que de otra forma se perderían. | [Modelo de direcciones](compartido/direcciones/anexo-decision-modelo-direcciones.md) · [i18n / l10n transversal](compartido/anexo-decision-i18n-l10n.md) · [Orquestación del registro de terceros](dominio/terceros/anexo-decision-orquestacion-registro.md) |
| `anexo-analisis-*` | **Análisis de industria / técnico** | Investigación de cómo resuelven el mismo problema ERPs líderes (SAP, Oracle, Dynamics, NetSuite, Odoo) o análisis técnico profundo de alternativas. Respalda las decisiones con evidencia externa. | [Numeración contable](dominio/contabilidad/anexo-analisis-numeracion-contable.md) · [Obligatoriedad de tercero / unidad organizacional](dominio/contabilidad/anexo-obligatoriedad-tercero-unidad-organizacional.md) · [ES en agregados de configuración](dominio/impuestos/anexo-analisis-es-configuracion.md) |
| `anexo-ejemplo-*` | **Ejemplo ilustrativo** | Aterriza un concepto abstracto del modelo con un caso concreto, con datos y flujo paso a paso. Reduce ambigüedades al momento de implementar. | [Plantilla de asiento](dominio/contabilidad/anexo-ejemplo-plantilla-de-asiento.md) · [Stream de Registro Tributario](dominio/impuestos/anexo-ejemplo-registro-tributario.md) |
| `anexo-configuracion-estandar-<país>` | **Configuración estándar por país** | Contenido precargado (seeds) por jurisdicción: catálogos, tarifas, tributos, formatos fiscales. Se carga como streams de eventos al iniciar operación. | [Colombia](dominio/impuestos/anexo-configuracion-estandar-co.md) · [Rep. Dominicana](dominio/impuestos/anexo-configuracion-estandar-do.md) · [Panamá](dominio/impuestos/anexo-configuracion-estandar-pa.md) |
| `anexo-estrategia-*` · `anexo-proyecciones-*` · `anexo-diseno-*` | **Estrategia / complemento técnico** | Desarrollo técnico profundo que respalda un concepto del modelo principal (proyecciones, estrategias de sincronización, modelos dimensionales, etc.). Evita que el modelo crezca con detalle de implementación. | [Estrategia de datos de referencia](compartido/datos-referencia/anexo-estrategia-datos-referencia.md) · [Proyecciones contables](dominio/contabilidad/anexo-proyecciones-contables.md) · [Diseño dimensional — Impuestos](dominio/impuestos/anexo-diseno-dimensional.md) |
| `anexo-definicion-contexto-inicial.md` | **Contexto inicial** | Notas previas a la definición formal del alcance. Puente conceptual cuando un sub-dominio aún no tiene `definicion-alcance.md` completo. | [Estructura Organizacional](dominio/estructura-organizacional/anexo-definicion-contexto-inicial.md) |

### Cuándo crear un anexo

```
¿La información es…?
 │
 ├─ Parte del QUÉ del negocio  ──────▶ va en definicion-alcance.md
 │
 ├─ Parte del CÓMO del modelo  ──────▶ va en modelo-dominio.md
 │
 ├─ El PORQUÉ de una decisión    ────▶ anexo-decision-*
 │  (alternativas, trade-offs)
 │
 ├─ Investigación de la industria ───▶ anexo-analisis-*
 │  o análisis técnico profundo
 │
 ├─ Un EJEMPLO concreto que       ───▶ anexo-ejemplo-*
 │  ilustra un concepto
 │
 ├─ Contenido PRECARGADO por país ───▶ anexo-configuracion-estandar-<país>
 │
 └─ Desarrollo técnico que         ──▶ anexo-estrategia-* / anexo-proyecciones-* / anexo-diseno-*
    respalda el modelo sin
    inflarlo
```

> **Regla de oro:** si una decisión tiene trade-offs serios, o si la solución imita/difiere de la industria, **documéntalo en un anexo**. El modelo queda limpio; el contexto de la decisión queda preservado.

---

## 🛠️ Cómo usar este repo

### A) Para leer un sub-dominio como especificación

1. Abre `dominio/<sub-dominio>/definicion-alcance.md` → entiendes el QUÉ.
2. Abre `dominio/<sub-dominio>/modelo-dominio.md` → entiendes el CÓMO (agregados, eventos, invariantes, FSM, sagas).
3. Revisa los anexos en la misma carpeta para decisiones de diseño puntuales.

### B) Para contribuir a un sub-dominio existente

```
┌─────────────────────────────────────────────────────────────┐
│  1. Lee el alcance y el modelo actuales                      │
│  2. Plantea el cambio en conversación con IA                 │
│  3. La IA propone el cambio → tú apruebas antes de aplicar   │
│  4. Al cerrar el hito, ejecuta /audit <archivo>              │
│  5. Aplica hallazgos de auditoría por severidad (Alta→Baja)  │
│  6. Commit con el formato de changelog del repo              │
└─────────────────────────────────────────────────────────────┘
```

> **Regla clave:** ningún cambio se aplica sin confirmación explícita. La IA propone, tú decides.

### C) Para crear un sub-dominio nuevo

```
┌─ Paso 1 ─────────────────────────────────────────────────┐
│  Crea dominio/<nuevo-sub-dominio>/                       │
│  Copia plantillas/definicion-alcance.md como base        │
└──────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─ Paso 2 ─ FASE 1 ────────────────────────────────────────┐
│  Conversa con IA para construir definicion-alcance.md:   │
│   · Necesidades del negocio                              │
│   · Glosario canónico                                    │
│   · Reglas y premisas                                    │
│   · Fases funcionales (F1, F2…)                          │
└──────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─ Paso 3 ─ FASE 2 ────────────────────────────────────────┐
│  Copia plantillas/modelo-dominio.md                      │
│  Conversa con IA para construirlo:                       │
│   · Agregados, entidades, VOs                            │
│   · Eventos de dominio e integración                     │
│   · Invariantes locales / eventuales / integración       │
│   · FSMs por agregado                                    │
│   · Domain services y sagas                              │
│   · Decisiones de diseño [D1]…[Dn]                       │
└──────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─ Paso 4 ─ AUDITORÍA ─────────────────────────────────────┐
│  /audit dominio/<nuevo-sub-dominio>/modelo-dominio.md    │
│  → Ejecuta 10 skills de auditoría                        │
│  → Resuelve hallazgos por severidad                      │
└──────────────────────────────────────────────────────────┘
```

Usa las plantillas de `plantillas/` como punto de partida y los ejemplos ya cerrados (Impuestos, Contabilidad, Terceros) como referencia de qué nivel de detalle esperar.

---

## ✍️ Estilo y lenguaje de los documentos

Tanto **`definicion-alcance.md`** como **`modelo-dominio.md`** son documentos **de dominio**, no de implementación. Su valor depende de mantener el vocabulario, el tono y el nivel de abstracción correctos. Si el documento impone cómo se construye el software, le quita espacio al equipo de implementación y acopla la especificación a una tecnología que puede cambiar.

### Principios

1. **Describe intención y comportamiento, no mecánica.** Di *qué* debe pasar y bajo *qué* condiciones. Cómo se persiste, transmite o expone es decisión del equipo de implementación.
2. **El alcance debe ser legible por un experto del negocio** sin conocimiento técnico. Si un contador no entiende el alcance de Contabilidad, el alcance está mal escrito.
3. **El modelo de dominio usa vocabulario DDD/ES/EDA** (agregado, evento, invariante, saga, FSM) pero **sigue siendo conceptual** — no prescribe base de datos, framework, protocolo ni lenguaje de programación.
4. **Si una decisión técnica es inevitable para entender el modelo**, documéntala en un **anexo de decisión o análisis** — no contamines el documento principal.

### Qué evitar y qué preferir

| Evita (impone implementación) | Prefiere (describe dominio) |
|-------------------------------|------------------------------|
| "Tabla `obligaciones`" / "registro en BD" | "Agregado `Obligación`" |
| "Endpoint `POST /obligaciones`" | "Comando `RegistrarObligación`" |
| "Guarda en PostgreSQL / MongoDB" | "Persiste" / "se registra en el stream" |
| "Publica en Kafka / RabbitMQ / SNS" | "Emite el evento `ObligaciónConfirmada`" |
| "Microservicio X" | "Sub-dominio X" / "Bounded context X" |
| "JSON / Schema / DTO" | "Payload del evento" / "estructura" |
| "ID autoincremental" / "UUID v4" | "Identificador del agregado" |
| "SQL `SELECT ... FROM`" | "Consulta sobre la proyección" |
| "Frontend / Backend" | "Experiencia del usuario" / "dominio" |
| "Clase / Interfaz / Controlador / Repositorio" | "Agregado" / "Servicio de dominio" / "Comando" |
| "Framework / Librería específica" | (omitir — no es relevante al dominio) |
| "Síncrono HTTP 200 / Código de error 409" | "El comando se rechaza cuando…" / "La operación es idempotente" |

### Test rápido antes de commitear

Pregúntate:

- ¿Un **experto del negocio** (contador, tesorero, analista fiscal) entendería el alcance sin traducción?
- ¿El modelo es **independiente de la tecnología** con la que se va a implementar?
- ¿La decisión técnica que agregué **pertenece realmente al dominio** o debería vivir en un anexo?

### Ejemplo

**❌ Mal (impone implementación):**
> Al confirmar una OXP, se hace `POST /oxp/{id}/confirm` que actualiza `estado='confirmada'` en la tabla `obligaciones_por_pagar` y envía un mensaje JSON a un tópico Kafka `oxp.events`.

**✅ Bien (describe dominio):**
> Al confirmar una OXP, el agregado transita al estado `Confirmada` y emite el evento `OXPConfirmada`. Los sub-dominios interesados reaccionan a ese evento.

La segunda versión es implementable en cualquier stack — REST + SQL + Kafka, gRPC + EventStoreDB, o cualquier otra combinación — y el equipo de desarrollo conserva la libertad de tomar esas decisiones técnicas.

---

## 🔍 Auditoría del modelo

El comando `/audit <archivo>` ejecuta 10 skills especializadas que validan distintas dimensiones:

| Dimensión | Skills |
|-----------|--------|
| **Estructura** | glossary · composition · state-machines · invariants |
| **Comportamiento** | responsibilities · event-semantics · idempotency |
| **Procesos** | sagas |
| **Calidad** | open-decisions · sanity-check |

Cada skill produce hallazgos clasificados por severidad (Alta / Media / Baja). El flujo recomendado:

1. Cerrar el hito del modelo (no auditar entre ediciones intermedias).
2. Ejecutar `/audit` completo.
3. Revisar hallazgos **uno a uno** con confirmación del usuario.
4. Aplicar o descartar — nunca en bloque.

> Para auditar una dimensión aislada: `/audit <archivo> <skill>`.

---

## 📐 Convenciones

| Aspecto | Regla |
|---------|-------|
| **Idioma** | Español Colombia (documentos y conversación) |
| **Naming** | `kebab-case` para carpetas y archivos |
| **Estructura de carpeta** | Una carpeta por sub-dominio en `dominio/` con sus artefactos dentro |
| **Plantillas** | Siempre partir de `plantillas/` al crear algo nuevo |
| **Edición** | Nunca aplicar cambios sin confirmación: proponer → aprobar → aplicar |
| **Auditoría** | Al cierre del modelo, no entre hitos |
| **Commits** | Título descriptivo + relación de cambios por sección afectada |

---

## 🧠 Stack conceptual

- **DDD (Domain-Driven Design)** — agregados, bounded contexts, lenguaje ubicuo.
- **Event Sourcing** — el estado se deriva de eventos; los eventos son la fuente de verdad.
- **EDA (Event-Driven Architecture)** — comunicación entre sub-dominios vía eventos de integración.
- **Sagas / Process Managers** — orquestación de procesos multi-agregado con compensación.

Los criterios transversales de modelado viven en [`guias-de-modelado/`](guias-de-modelado/).

---

## 🗺️ Próximos pasos del proyecto

Ver [`plan-trabajo-abril.md`](plan-trabajo-abril.md) para el plan activo:

- **Bloque A** — Sub-dominios base bloqueantes
- **Bloque B** — Refinamientos de OXP e Impuestos
- **Bloque C** — Visión transversal del ERP

---

## 📄 Para la IA

Las instrucciones operativas para trabajar con este repo vía Claude Code están en [`CLAUDE.md`](CLAUDE.md): flujo de modelado, reglas de edición, uso de skills y commands.
