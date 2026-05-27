---
name: audit-full
description: "Orquestador de auditoría completa del modelo de dominio DDD/ES/EDA: ejecuta las 11 skills de auditoría en secuencia lógica (glossary → composition → state-machines → invariants → responsibilities → event-semantics → contract-vs-internals → idempotency → sagas → open-decisions → sanity-check) y consolida un reporte unificado priorizado. Úsalo cuando el usuario pida una auditoría completa, un review general del modelo, o cuando se quiera validar el estado global del documento después de sesiones largas de edición."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Audit Full — Orquestador de Auditoría Completa

Orquestador que ejecuta las 11 skills de auditoría especializadas en secuencia lógica, consolidando los hallazgos en un reporte unificado priorizado por severidad.

## Secuencia de ejecución

Ejecutar las skills en este orden preciso. Cada skill construye sobre el contexto de las anteriores:

### Capa 1 — Structure (establece vocabulario y estructura)

| Paso | Skill | Razón del orden |
|------|-------|----------------|
| 1 | `/audit-structure-glossary` | Primero: establece el vocabulario canónico |
| 2 | `/audit-structure-composition` | Estructura interna de agregados |
| 3 | `/audit-structure-state-machines` | Ciclos de vida (FSM) |
| 4 | `/audit-structure-invariants` | Reglas y enforcement |

### Capa 2 — Behavior (valida comportamiento sobre la estructura)

| Paso | Skill | Razón del orden |
|------|-------|----------------|
| 5 | `/audit-behavior-responsibilities` | Diseño comportamental de agregados |
| 6 | `/audit-behavior-event-semantics` | Semántica de eventos |
| 7 | `/audit-behavior-contract-vs-internals` | Coherencia entre contrato externo y flujo interno |
| 8 | `/audit-behavior-idempotency` | Concurrencia e idempotencia |

### Capa 3 — Process (valida coordinación multi-agregado)

| Paso | Skill | Razón del orden |
|------|-------|----------------|
| 9 | `/audit-process-sagas` | Procesos multi-agregado |

### Capa 4 — Quality (meta-auditoría sobre todo lo anterior)

| Paso | Skill | Razón del orden |
|------|-------|----------------|
| 10 | `/audit-quality-open-decisions` | Inventario de pendientes |
| 11 | `/audit-quality-sanity-check` | Último: coherencia global con todo lo anterior |

## Procedimiento

1. **Leer el modelo de dominio completo** al inicio. El modelo debe estar en la ventana de contexto antes de comenzar.
2. **Ejecutar cada skill en orden**, siguiendo el procedimiento definido en cada una.
3. **Para cada skill**, producir su reporte individual completo (formato de salida + hallazgos).
4. **Al finalizar las 10 skills**, consolidar todos los hallazgos en el reporte unificado.

## Formato de salida

### Reporte consolidado

```
## Audit Full — Reporte de Auditoría Completa

**Fecha:** <fecha>
**Modelo auditado:** <nombre del documento>

---

### 1. Glosario y Lenguaje Ubicuo
(reporte completo de audit-structure-glossary)

### 2. Composición de Agregados
(reporte completo de audit-structure-composition)

### 3. Máquinas de Estado (FSM)
(reporte completo de audit-structure-state-machines)

### 4. Invariantes
(reporte completo de audit-structure-invariants)

### 5. Responsabilidades de Agregados
(reporte completo de audit-behavior-responsibilities)

### 6. Semántica de Eventos
(reporte completo de audit-behavior-event-semantics)

### 7. Contrato vs Flujo Interno
(reporte completo de audit-behavior-contract-vs-internals)

### 8. Idempotencia y Concurrencia
(reporte completo de audit-behavior-idempotency)

### 9. Sagas y Procesos Multi-Agregado
(reporte completo de audit-process-sagas)

### 10. Decisiones Abiertas
(reporte completo de audit-quality-open-decisions)

### 11. Sanity Check (Coherencia Cruzada)
(reporte completo de audit-quality-sanity-check)

---

### Resumen Ejecutivo

| Skill | Alta | Media | Baja | Total |
|-------|------|-------|------|-------|
| Glosario | N | N | N | N |
| Composición | N | N | N | N |
| FSM | N | N | N | N |
| Invariantes | N | N | N | N |
| Responsabilidades | N | N | N | N |
| Semántica Eventos | N | N | N | N |
| Contrato vs Flujo | N | N | N | N |
| Idempotencia | N | N | N | N |
| Sagas | N | N | N | N |
| Decisiones Abiertas | N | N | N | N |
| Sanity Check | N | N | N | N |
| **TOTAL** | **N** | **N** | **N** | **N** |

### Top 5 Hallazgos Críticos

(los 5 hallazgos de mayor severidad e impacto de toda la auditoría, sin importar de qué skill provienen)

| # | Skill origen | Severidad | Problema | Corrección mínima |
|---|-------------|-----------|----------|-------------------|
```

## Protocolo de salida

### Severidad (misma escala en todas las skills)

| Nivel | Criterio |
|-------|----------|
| **Alta** | Rompe invariante, lógica contradictoria, estado inalcanzable, pérdida financiera potencial |
| **Media** | Ambigüedad que bloquea implementación, gap de especificación, riesgo no mitigado |
| **Baja** | Claridad, estilo, optimización menor |

### Reglas

- Cada skill individual mantiene su máximo de 10 hallazgos.
- El reporte consolidado incluye **todos** los hallazgos de todas las skills.
- El Top 5 prioriza los hallazgos más críticos de todo el reporte.
- Si el modelo de dominio no cabe en contexto, indicar qué secciones se auditaron y cuáles quedaron fuera.

### Regla de oro

> **NO reescribir el documento.** Solo diagnosticar y sugerir la corrección mínima necesaria. No proponer reestructuraciones, no agregar secciones nuevas, no cambiar convenciones existentes. Esto aplica a cada skill individual y al reporte consolidado.
