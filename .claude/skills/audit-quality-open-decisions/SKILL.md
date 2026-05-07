---
name: audit-quality-open-decisions
description: "Audita deuda técnica documental en modelos de dominio DDD/ES/EDA: detecta pendientes sin resolver, decisiones implícitas no formalizadas, TODOs sin ownership, frases como 'por definir' o 'se evaluará', y versiones del changelog con pendientes no cerrados. Úsalo cuando el usuario pida revisar pendientes, cuando se mencione deuda técnica documental, decisiones abiertas, TODOs, o cuando se quiera hacer inventario de lo que falta por definir en el modelo."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Audit Quality — Decisiones Abiertas y Deuda Documental

Especialista en gestión de deuda técnica documental en modelos de dominio **DDD**, **Event Sourcing** y **EDA**: inventario de pendientes, decisiones implícitas, TODOs sin ownership y compromisos diferidos sin plan de cierre.

## Qué audita

Identifica todo lo que el modelo deja sin definir o asume implícitamente: frases que indican pendientes, decisiones tomadas sin formalizarse, TODOs sin dueño ni plazo, y versiones del changelog que prometieron resolver algo y no lo hicieron. Trabaja sobre el modelo de dominio presente en la ventana de contexto actual.

## Procedimiento

1. Identificar en el contexto el modelo de dominio completo, incluyendo changelog si existe.
2. Ejecutar los siguientes chequeos:

- [ ] **Frases indicadoras de pendiente:** buscar en todo el documento variantes de: "pendiente", "por definir", "se evaluará", "se definirá", "futuro", "diferido", "a determinar", "por determinar", "TBD", "TODO", "⚠️", "nota:", "revisar", "provisional", "temporal".
- [ ] **Decisiones implícitas:** detectar lugares donde el texto asume algo sin declararlo formalmente como decisión (D##). Ej: "los anticipos no llevan desglose fiscal" aparece en una nota pero no está formalizado como D##.
- [ ] **Para cada pendiente encontrado, documentar:**
  - Contexto: ¿en qué sección aparece?
  - Decisión temporal: ¿se tomó alguna decisión provisional mientras tanto?
  - Alternativas: ¿el texto menciona opciones?
  - Riesgo: ¿qué pasa si se implementa sin resolver este pendiente?
  - Criterio de cierre: ¿qué información o decisión se necesita para cerrarlo?
- [ ] **TODOs sin ownership:** cualquier pendiente que no tenga asignado un responsable o un plazo.
- [ ] **Changelog con pendientes no cerrados:** si el changelog menciona "pendiente" o "diferido" en una versión anterior, verificar si versiones posteriores lo resolvieron. Señalar los que siguen abiertos.

3. Producir inventario de pendientes + hallazgos.

## Formato de salida

### Inventario de pendientes

| # | Ubicación (L~N) | Texto literal | Tipo | Decisión temporal | Riesgo | Criterio de cierre |
|---|-----------------|--------------|------|-------------------|--------|-------------------|
| _n_ | _sección, línea_ | _"cita"_ | Pendiente / Implícita / TODO / Diferido | _si existe_ | Alto/Medio/Bajo | _qué se necesita_ |

### Tabla de hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

## Protocolo de salida

### Severidad

| Nivel | Criterio |
|-------|----------|
| **Alta** | Rompe invariante, lógica contradictoria, estado inalcanzable, pérdida financiera potencial |
| **Media** | Ambigüedad que bloquea implementación, gap de especificación, riesgo no mitigado |
| **Baja** | Claridad, estilo, optimización menor |

### Reglas

- **Evidencia concreta:** siempre citar línea aproximada (`L~N`) y fragmento textual entre comillas.
- **Un hallazgo = un problema atómico.**
- **Corrección mínima:** la intervención más pequeña que resuelve el problema.
- **Orden:** Alta → Media → Baja.
- **Máximo 10 hallazgos** priorizados por severidad. Si hay más de 10 de severidad Alta, mencionar cuántos quedan sin reportar.

### Estructura del reporte

```
## Audit Quality — Decisiones Abiertas — Reporte de Auditoría

**Fecha:** <fecha>

### Inventario de Pendientes

| # | Ubicación (L~N) | Texto literal | Tipo | Decisión temporal | Riesgo | Criterio de cierre |
|---|-----------------|--------------|------|-------------------|--------|-------------------|

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

### Resumen
- Pendientes: N | Decisiones implícitas: N | TODOs: N | Diferidos: N
- Alta: N | Media: N | Baja: N
- Total: N hallazgos
```

### Regla de oro

> **NO reescribir el documento.** Solo diagnosticar y sugerir la corrección mínima necesaria. No proponer reestructuraciones, no agregar secciones nuevas, no cambiar convenciones existentes.
