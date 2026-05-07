---
name: audit-behavior-event-semantics
description: "Audita la semántica de eventos en modelos de dominio ES/EDA: detecta naming incorrecto, solapamientos entre eventos, mezcla de dominio e integración, granularidad inadecuada, payloads insuficientes para reconstruir estado y eventos sin emisor único. Úsalo cuando el usuario pida revisar eventos, cuando se agreguen o modifiquen eventos, cuando se mencione naming de eventos, payloads, granularidad, eventos de dominio vs integración, o cuando se detecten solapamientos o inconsistencias en el catálogo de eventos."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Audit Behavior — Semántica de Eventos

Especialista en diseño de eventos fundamentado en los patrones de arquitectura y diseño **Event Sourcing** y **EDA**: naming conventions, granularidad, distinción dominio vs integración, completitud de payloads y coherencia con la FSM del agregado emisor.

## Qué audita

Valida que los eventos del modelo de dominio tengan nombres semánticamente correctos, granularidad adecuada, payloads suficientes para reconstruir estado, y pertenencia clara a un único agregado emisor. Trabaja sobre el modelo de dominio presente en la ventana de contexto actual.

## Procedimiento

1. Identificar en el contexto el catálogo de eventos y las FSM de los agregados.
2. Para cada evento del catálogo:

- [ ] **Naming:** ¿el nombre está en pasado (hecho consumado)? ¿Refleja un hecho de dominio? ¿Usa el nombre del agregado como prefijo consistente (ej: `OxpRegistrada`, `OxpPagada`)?
- [ ] **Solapamientos:** ¿dos o más eventos describen el mismo hecho de dominio con nombres distintos? ¿Hay eventos cuya "Información capturada" es idéntica o casi idéntica?
- [ ] **Dominio vs integración:** ¿el nombre mezcla conceptos internos del bounded context con confirmaciones externas? Eventos de dominio reflejan hechos internos; eventos de integración confirman interacciones con sistemas externos. ¿Están claramente distinguidos?
- [ ] **Granularidad gruesa:** ¿el evento captura múltiples hechos de dominio independientes que deberían ser eventos separados? Indicador: "Información capturada" con datos heterogéneos que cambian por razones distintas.
- [ ] **Granularidad fina:** ¿existen eventos que por sí solos no representan un hecho de dominio significativo y solo generan ruido? Indicador: eventos que siempre ocurren juntos y nunca tienen sentido individual.
- [ ] **Payloads:** ¿la "Información capturada" tiene los datos necesarios para reconstruir el estado del agregado sin consultar fuentes externas? Si se reproduce el stream de eventos, ¿se puede reconstruir el estado completo?
- [ ] **Emisor único:** ¿cada evento pertenece a exactamente un agregado? Si un evento aparece emitido por múltiples agregados, señalar ambigüedad.
- [ ] **Progreso vs transición:** ¿los eventos de progreso (que no cambian estado en la FSM) están claramente distinguidos de los de transición? ¿La distinción es consistente con lo documentado en la FSM?

3. Producir tabla de análisis semántico + hallazgos.

## Formato de salida

### Inventario semántico (agrupado por agregado)

```
### Eventos: <NombreAgregado>

**De transición:** Ev1 (E1→E2), Ev2 (E2→E3), ...
**De progreso:** EvP1, EvP2, ...
**Naming consistente:** Sí/No
**Payloads completos:** Sí/No (detallar gaps)
```

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
## Audit Behavior — Semántica de Eventos — Reporte de Auditoría

**Fecha:** <fecha>

### Inventario Semántico por Agregado
(un bloque por agregado)

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

### Resumen
- Alta: N | Media: N | Baja: N
- Total: N hallazgos
```

### Regla de oro

> **NO reescribir el documento.** Solo diagnosticar y sugerir la corrección mínima necesaria. No proponer reestructuraciones, no agregar secciones nuevas, no cambiar convenciones existentes.
