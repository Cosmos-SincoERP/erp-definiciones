---
name: audit-structure-invariants
description: "Audita invariantes en modelos de dominio DDD/ES: clasifica cada invariante como local, eventual o de integración, detecta invariantes presentadas como locales que cruzan fronteras de agregado, señala mecanismos de enforcement faltantes y verifica coherencia con las decisiones de diseño documentadas. Úsalo cuando el usuario pida validar invariantes, cuando se agreguen o modifiquen reglas de negocio, cuando se mencione enforcement, consistencia eventual, fronteras de agregado, o cuando se detecten invariantes que podrían estar mal clasificadas."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Audit Structure — Invariantes: local vs eventual vs integración

Especialista en análisis de fronteras transaccionales fundamentado en los patrones de arquitectura y diseño **DDD**, **Event Sourcing** y **EDA**: clasificación de invariantes por alcance (local, eventual, integración), mecanismos de enforcement y estrategias de compensación.

## Qué audita

Valida que cada invariante del modelo de dominio esté correctamente clasificada según su alcance transaccional, tenga un mecanismo de enforcement documentado y no contradiga las fronteras de agregado ni las decisiones de diseño. Trabaja sobre el modelo de dominio presente en la ventana de contexto actual.

## Procedimiento

1. Identificar en el contexto todas las invariantes explícitas (ej: I1-I17 o la numeración que exista) y las reglas implícitas en el texto (precondiciones de eventos, guards en transiciones, restricciones mencionadas en notas).
2. Para cada invariante encontrada:

- [ ] **Clasificar** según alcance transaccional:

| Tipo | Criterio | Enforcement esperado |
|------|----------|---------------------|
| **Local** | Se evalúa dentro de un solo agregado en una transacción | Guard en comando del agregado |
| **Eventual** | Cruza agregados, se garantiza eventualmente | Saga / Policy / Process Manager |
| **Integración** | Depende de sistema externo o bounded context externo | Anti-corruption layer, compensación |

- [ ] **Verificar enforcement documentado:** ¿el modelo documenta cómo se garantiza esta invariante? ¿Guard, saga, policy, servicio de dominio?
- [ ] **Detectar clasificación incorrecta:** invariantes presentadas como locales (guard en un agregado) pero que referencian datos de otro agregado. Esto sugiere que la invariante es eventual, no local.
- [ ] **Detectar invariantes eventuales sin compensación:** si una invariante cruza agregados y se garantiza eventualmente, ¿qué pasa si falla? ¿Hay compensación documentada?
- [ ] **Cruzar con decisiones de diseño:** si el modelo documenta decisiones sobre fronteras de agregado, verificar que las invariantes no contradigan esas decisiones.
- [ ] **Detectar invariantes implícitas no formalizadas:** precondiciones de eventos que aplican la misma restricción repetidamente sin estar catalogadas como invariante formal.

3. Producir tabla de clasificación + hallazgos.

## Formato de salida

### Tabla de clasificación (una fila por invariante)

| ID | Invariante (resumen) | Tipo | Agregado(s) involucrado(s) | Enforcement documentado | Gap |
|----|---------------------|------|---------------------------|------------------------|-----|

Donde:
- **ID**: identificador de la invariante (I1, I2... o "implícita-N" si no está formalizada)
- **Tipo**: Local / Eventual / Integración
- **Enforcement documentado**: mecanismo que el modelo describe para garantizarla
- **Gap**: qué falta (vacío si está completa)

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
## Audit Structure — Invariantes — Reporte de Auditoría

**Fecha:** <fecha>

### Clasificación de Invariantes

| ID | Invariante (resumen) | Tipo | Agregado(s) involucrado(s) | Enforcement documentado | Gap |
|----|---------------------|------|---------------------------|------------------------|-----|

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

### Resumen
- Alta: N | Media: N | Baja: N
- Total: N hallazgos
```

### Regla de oro

> **NO reescribir el documento.** Solo diagnosticar y sugerir la corrección mínima necesaria. No proponer reestructuraciones, no agregar secciones nuevas, no cambiar convenciones existentes.
