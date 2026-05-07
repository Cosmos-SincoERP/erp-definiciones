---
name: audit-behavior-idempotency
description: "Audita idempotencia y concurrencia en modelos de dominio ES/EDA financieros: detecta operaciones sin identificador único, reglas anti-duplicado faltantes, enforcement insuficiente contra concurrencia en saldos, ausencia de optimistic concurrency y cruces mutables. Úsalo cuando el usuario pida revisar idempotencia, cuando se modifiquen comandos que afectan saldos, cuando se mencione concurrencia, duplicados, replay-safety, optimistic locking, at-least-once, o cuando se detecten riesgos de doble aplicación de operaciones financieras."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Audit Behavior — Idempotencia y Concurrencia

Especialista en análisis de idempotencia y control de concurrencia fundamentado en los patrones de arquitectura y diseño **Event Sourcing** y **EDA**: replay-safety, optimistic concurrency, prevención de duplicados en operaciones financieras y garantías at-least-once.

## Qué audita

Valida que las operaciones que modifican saldos y estados financieros sean idempotentes (safe para replay y retry), que existan mecanismos documentados contra duplicados y concurrencia, y que las entidades de cruce sean inmutables una vez creadas. Trabaja sobre el modelo de dominio presente en la ventana de contexto actual.

## Procedimiento

1. Identificar en el contexto todos los comandos que modifican saldos o crean cruces entre agregados, junto con las invariantes relacionadas con saldos (ej: I11, I13, I14 o equivalentes).
2. Para cada operación financiera relevante:

- [ ] **operationId / paymentId:** ¿el comando que inicia la operación documenta un identificador único (operationId, paymentId, idempotencyKey) para prevenir doble ejecución?
- [ ] **Reglas anti-duplicado:** ¿las precondiciones del comando verifican que la operación no haya sido aplicada previamente? Buscar guards como "no existe PagoAplicado con mismo paymentId".
- [ ] **Saldos no negativos:** ¿las invariantes que protegen saldos (ej: "saldo >= 0", "totalPagado <= totalObligación") documentan enforcement contra concurrencia? Un guard a nivel de agregado es insuficiente si dos comandos concurrentes leen el mismo saldo.
- [ ] **Optimistic concurrency:** ¿los comandos que modifican estado documentan expected version o mecanismo equivalente para detectar conflictos de escritura concurrente?
- [ ] **Ventanas de concurrencia:** ¿existen escenarios donde dos comandos simultáneos podrían violar una invariante? Ej: dos pagos simultáneos que exceden el saldo disponible. ¿Está documentada la estrategia de resolución?
- [ ] **Cruces inmutables:** ¿las entidades de cruce (PagoAplicado, CrucePago, CruceAnticipo u equivalentes) documentan inmutabilidad post-creación? Una vez creado un cruce, no debería modificarse — solo revertirse con un nuevo evento.
- [ ] **At-least-once safety:** ¿los domain services (conciliación, regularización, devolución) son safe para retry? Si se ejecutan dos veces con los mismos parámetros, ¿el resultado es idéntico?
- [ ] **Replay safety:** si se reproduce el stream de eventos completo, ¿los handlers producen el mismo estado? ¿Hay side-effects en los handlers que no son idempotentes?

3. Producir matriz de idempotencia + hallazgos.

## Formato de salida

### Matriz de idempotencia (una fila por operación financiera)

| Operación / Comando | Agregado | IdempotencyKey documentada | Guard anti-duplicado | Optimistic concurrency | Riesgo concurrencia |
|---------------------|----------|---------------------------|---------------------|----------------------|-------------------|
| _comando_ | _agregado_ | Sí/No | Sí/No (detalle) | Sí/No | Alto/Medio/Bajo |

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
## Audit Behavior — Idempotencia — Reporte de Auditoría

**Fecha:** <fecha>

### Matriz de Idempotencia

| Operación / Comando | Agregado | IdempotencyKey documentada | Guard anti-duplicado | Optimistic concurrency | Riesgo concurrencia |
|---------------------|----------|---------------------------|---------------------|----------------------|-------------------|

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

### Resumen
- Alta: N | Media: N | Baja: N
- Total: N hallazgos
```

### Regla de oro

> **NO reescribir el documento.** Solo diagnosticar y sugerir la corrección mínima necesaria. No proponer reestructuraciones, no agregar secciones nuevas, no cambiar convenciones existentes.
