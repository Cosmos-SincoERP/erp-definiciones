---
name: audit-behavior-responsibilities
description: "Audita el diseño comportamental de agregados DDD/POO: detecta agregados anémicos, agregados inflados (God Aggregate), lógica de negocio fugada a servicios, violaciones de SRP y fronteras de agregado desalineadas con las invariantes que protegen. Úsalo cuando el usuario pida revisar responsabilidades de agregados, cuando se mencione anemia, inflación, SRP, fronteras de agregado, domain services, o cuando se detecte comportamiento fuera del agregado."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Audit Behavior — Responsabilidades de Agregados

Especialista en diseño comportamental de agregados fundamentado en los patrones de arquitectura y diseño **DDD** y **POO**: principio de responsabilidad única, encapsulación de lógica de negocio, detección de anemia e inflación, y alineación entre fronteras transaccionales e invariantes protegidas.

## Qué audita

Valida que cada agregado tenga responsabilidades bien delimitadas: ni anémico (solo datos) ni inflado (demasiadas responsabilidades). Verifica que la lógica de negocio resida dentro del agregado, que los domain services coordinen sin absorber lógica, y que las fronteras de agregado estén alineadas con las invariantes que protegen. Trabaja sobre el modelo de dominio presente en la ventana de contexto actual.

## Procedimiento

1. Identificar en el contexto todos los agregados con sus comandos, eventos, composición, invariantes y domain services.
2. Para cada agregado:

- [ ] **Detección de anemia:** ¿el agregado tiene comandos que solo setean datos sin validar invariantes? ¿Los eventos solo capturan datos sin transformación de estado significativa? Si el agregado tiene composición rica pero comportamiento trivial, señalar anemia.
- [ ] **Detección de inflación (God Aggregate):** ¿el agregado tiene más de una razón de cambio dominante? ¿Gestiona responsabilidades que podrían pertenecer a agregados independientes? Indicadores: demasiados estados en la FSM, demasiados eventos propios, invariantes que cubren conceptos heterogéneos.
- [ ] **Encapsulación de lógica:** ¿toda la lógica de negocio relevante está dentro del agregado? Buscar precondiciones o validaciones mencionadas en domain services que deberían ser guards del agregado.
- [ ] **Domain services:** ¿coordinan entre agregados sin absorber lógica que pertenece a un agregado específico? Un domain service no debe tomar decisiones de negocio que dependan solo del estado interno de un agregado.
- [ ] **Lógica fugada:** ¿existen precondiciones implícitas en las descripciones de domain services o en notas que no están formalizadas como guards del agregado?
- [ ] **SRP:** ¿cada agregado tiene una sola razón de cambio dominante? Si un cambio en una regla de negocio afecta a múltiples secciones del agregado por razones distintas, señalar posible violación de SRP.
- [ ] **Alineación frontera-invariantes:** ¿la frontera transaccional del agregado coincide con las invariantes que debe proteger? Si una invariante local involucra datos de otro agregado, la frontera está desalineada.
- [ ] **Comportamientos calculados:** ¿los métodos calculados (ej: `saldoPorPagar()`) referencian solo componentes propios del agregado?

3. Producir tabla de responsabilidades + hallazgos.

## Formato de salida

### Mapa de responsabilidades (uno por agregado)

```
### Responsabilidades: <NombreAgregado>

**Razón de cambio dominante:** <descripción>
**Comandos:** N
**Eventos propios:** N
**Invariantes protegidas:** I1, I5, ...
**Domain services que lo coordinan:** DS1, DS2, ...
**Diagnóstico:** Saludable / Anémico / Inflado / Fuga de lógica
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
## Audit Behavior — Responsabilidades — Reporte de Auditoría

**Fecha:** <fecha>

### Mapa de Responsabilidades por Agregado
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
