---
name: audit-process-sagas
description: "Audita procesos multi-agregado (sagas) en modelos de dominio EDA/DDD: detecta domain services sin trigger, pasos sin compensación, correlationId faltante, idempotencyKey ausente, dependencias de ordenamiento no documentadas y ventanas de inconsistencia eventual sin documentar. Úsalo cuando el usuario pida revisar procesos multi-agregado, cuando se modifiquen domain services, cuando se mencione sagas, compensación, consistencia eventual, correlación, o cuando se detecten procesos incompletos."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Audit Process — Sagas y Procesos Multi-Agregado

Especialista en análisis de procesos multi-agregado fundamentado en los patrones de arquitectura y diseño **EDA** y **DDD**: completitud de sagas, compensación, correlación, idempotencia por paso, ordenamiento y persistencia de estado del proceso.

## Qué audita

Valida que cada proceso que coordina múltiples agregados (domain services, sagas, process managers) documente de forma completa su trigger, pasos, eventos, compensación y estrategia ante fallos. Trabaja sobre el modelo de dominio presente en la ventana de contexto actual.

## Procedimiento

1. Identificar en el contexto todos los domain services, sagas o procesos que coordinen más de un agregado.
2. Para cada proceso encontrado:

- [ ] **Completitud:** ¿documenta trigger (evento o comando que lo inicia), pasos (secuencia de comandos a agregados), eventos emitidos por cada paso y mecanismo de compensación si un paso falla?
- [ ] **CorrelationId:** ¿los eventos de un mismo proceso comparten un identificador de correlación documentado? ¿Se puede trazar un proceso completo por su correlationId?
- [ ] **IdempotencyKey por paso:** ¿cada paso del proceso tiene clave de idempotencia para prevenir doble ejecución ante retry?
- [ ] **Ordenamiento:** ¿el proceso depende del orden de llegada de eventos? Si es así, ¿está documentado qué pasa si los eventos llegan en orden distinto? ¿Hay estrategia de reordenamiento o es order-independent?
- [ ] **Timeouts / retries:** ¿qué pasa si un paso falla o no responde? ¿Hay timeout documentado? ¿Política de retry? ¿Dead letter?
- [ ] **Consistencia eventual:** ¿las ventanas de inconsistencia entre el primer y último paso están documentadas? ¿Qué estado ve un usuario durante la ventana?
- [ ] **Compensación:** si un paso falla después de que pasos anteriores ya emitieron eventos, ¿hay mecanismo de rollback documentado (eventos compensatorios)? ¿La compensación es idempotente?
- [ ] **Persistencia del estado:** ¿el estado del proceso se persiste en un stream propio? ¿O es efímero (en memoria)? Si es efímero, ¿qué pasa ante un crash mid-process?

3. Producir mapa de procesos + hallazgos.

## Formato de salida

### Mapa de procesos (uno por domain service / saga)

```
### Proceso: <NombreDomainService>

**Trigger:** <evento o comando>
**Agregados involucrados:** Agg1, Agg2, ...
**Pasos:**
  1. Comando → Agg1 → Evento1
  2. Comando → Agg2 → Evento2
  ...
**Compensación:** Documentada / Parcial / Ausente
**CorrelationId:** Sí/No
**IdempotencyKey por paso:** Sí/No
**Persistencia del estado:** Stream propio / Efímero / No documentado
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
## Audit Process — Sagas — Reporte de Auditoría

**Fecha:** <fecha>

### Mapa de Procesos
(un bloque por domain service / saga)

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

### Resumen
- Alta: N | Media: N | Baja: N
- Total: N hallazgos
```

### Regla de oro

> **NO reescribir el documento.** Solo diagnosticar y sugerir la corrección mínima necesaria. No proponer reestructuraciones, no agregar secciones nuevas, no cambiar convenciones existentes.
