---
name: audit-structure-glossary
description: "Audita el lenguaje ubicuo en modelos de dominio DDD: detecta sinónimos no controlados, términos ambiguos usados con significados distintos entre secciones, variantes de un mismo concepto y términos del glosario canónico ausentes en el modelo. Úsalo cuando el usuario pida revisar la terminología, cuando se agreguen nuevos conceptos o agregados, cuando se mencione glosario, lenguaje ubicuo, ambigüedad de términos, sinónimos, o cuando se detecten nombres inconsistentes entre secciones del modelo."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Audit Structure — Glosario y Lenguaje Ubicuo

Especialista en análisis de lenguaje ubicuo fundamentado en el patrón de arquitectura y diseño **DDD**: consistencia terminológica dentro del bounded context, detección de sinónimos no controlados, ambigüedades semánticas entre secciones y alineación con el glosario canónico.

## Qué audita

Valida que el modelo de dominio use una terminología consistente, sin sinónimos ocultos ni términos ambiguos. Cruza los términos del modelo contra el glosario canónico (si existe en documentos de referencia) para detectar desalineaciones. Trabaja sobre el modelo de dominio presente en la ventana de contexto actual.

## Procedimiento

1. Identificar en el contexto el modelo de dominio y, si existe, el glosario canónico (ej: sección de glosario en un documento de alcance).
2. Extraer todos los términos clave del modelo:

- [ ] **Agregados:** nombres, aliases, abreviaciones usadas.
- [ ] **Entidades internas:** nombres declarados en composición vs nombres usados en eventos y notas.
- [ ] **Value Objects:** nombres, ¿se usan igual en todos los agregados?
- [ ] **Eventos:** ¿los nombres reflejan hechos de dominio en pasado? ¿Hay variantes en las descripciones?
- [ ] **Estados:** ¿se nombran igual en la FSM, el catálogo de eventos y las notas?
- [ ] **Comandos/operaciones:** ¿se mencionan con nombres consistentes?
- [ ] **Servicios de dominio:** ¿se referencian igual en todas las secciones?
- [ ] **Conceptos de negocio:** términos funcionales (ej: "conciliación", "regularización", "causación") — ¿se usan siempre con el mismo significado?

3. Para cada término encontrado:

- [ ] ¿Tiene definición en el glosario canónico? ¿Coincide con el uso en el modelo?
- [ ] ¿Se usa con el mismo significado en **todas** las secciones del modelo?
- [ ] ¿Existen sinónimos o variantes no controladas (ej: "compensación" vs "pago vía extracto")?
- [ ] ¿Se usa un término técnico (DDD/ES) y uno de negocio para lo mismo sin aclaración?

4. Detectar términos del glosario canónico que deberían estar en el modelo pero están ausentes.
5. Producir tabla de términos + hallazgos.

## Formato de salida

### Tabla de términos (solo los que tienen hallazgo o variantes)

| Término canónico | Variantes encontradas | Secciones donde aparece | Tipo de problema |
|-----------------|----------------------|------------------------|-----------------|
| _término_ | _sinónimo1, sinónimo2_ | _composición, eventos, notas_ | Sinónimo / Ambigüedad / Ausente / Inconsistencia |

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
## Audit Structure — Glosario — Reporte de Auditoría

**Fecha:** <fecha>

### Términos con Hallazgo

| Término canónico | Variantes encontradas | Secciones donde aparece | Tipo de problema |
|-----------------|----------------------|------------------------|-----------------|

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

### Resumen
- Alta: N | Media: N | Baja: N
- Total: N hallazgos
```

### Regla de oro

> **NO reescribir el documento.** Solo diagnosticar y sugerir la corrección mínima necesaria. No proponer reestructuraciones, no agregar secciones nuevas, no cambiar convenciones existentes.
