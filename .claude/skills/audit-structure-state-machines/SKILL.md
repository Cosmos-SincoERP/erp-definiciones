---
name: audit-structure-state-machines
description: "Audita máquinas de estado (FSM) en modelos de dominio DDD/ES: detecta estados huérfanos, transiciones imposibles, estados sumidero no intencionados, terminales inconsistentes con saldos y eventos sin cobertura en la FSM. Úsalo cuando el usuario pida validar transiciones de estado, cuando se modifiquen estados o eventos de un agregado, cuando se mencione revisar FSM, auditar estados, validar transiciones, o cuando se hable de estados inalcanzables o transiciones faltantes."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# S2 — FSM Audit (Estados y Transiciones)

Especialista en análisis de máquinas de estado finito fundamentado en los patrones de arquitectura y diseño **DDD** y **Event Sourcing**: ciclos de vida de agregados, eventos de progreso vs transición, alcanzabilidad de estados y coherencia con saldos derivados.

## Qué audita

Valida que las máquinas de estado documentadas en un modelo de dominio sean completas, coherentes y libres de contradicciones. Trabaja sobre el modelo de dominio presente en la ventana de contexto actual.

## Procedimiento

1. Identificar en el contexto todos los agregados que declaren estados y transiciones.
2. Para cada agregado encontrado:

- [ ] Listar todos los **estados** declarados, distinguiendo terminales (■) de intermedios.
- [ ] Listar todos los **eventos** que provocan transiciones, mapeando: evento → estado origen → estado destino.
- [ ] Detectar **estados huérfanos**: estados declarados que no tienen ninguna transición de entrada (ningún evento los alcanza).
- [ ] Detectar **estados sumidero no intencionados**: estados sin transición de salida que NO están declarados como terminales.
- [ ] Detectar **transiciones imposibles**: transiciones documentadas cuya precondición contradice las propiedades del estado origen.
- [ ] Si el modelo define una función de saldo (ej: `saldoPorPagar()`), cruzar cada estado terminal con ella: un estado terminal debe implicar saldo resuelto (típicamente = 0), o debe existir justificación explícita de por qué no.
- [ ] Si el modelo incluye un catálogo de eventos, verificar que todo evento que afecte al agregado aparezca representado en su FSM. Reportar eventos huérfanos (en catálogo pero ausentes de la FSM).
- [ ] Verificar que los eventos de progreso (que no cambian estado) estén documentados como tales y no contradigan la FSM.

3. Producir el reporte con resumen estructural + hallazgos.

## Formato de salida

### Resumen estructural (uno por agregado)

```
### FSM: <NombreAgregado>

**Estados:** E1, E2, E3, ...
**Terminales:** ET1 ■, ET2 ■
**Transiciones:**
  Ev1: E1 → E2
  Ev2: E2 → E3
  ...
**Eventos de progreso (sin cambio de estado):** EvP1, EvP2, ...
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
- **Un hallazgo = un problema atómico.** No agrupar múltiples issues.
- **Corrección mínima:** la intervención más pequeña que resuelve el problema.
- **Orden:** Alta → Media → Baja.
- **Máximo 10 hallazgos** priorizados por severidad. Si hay más de 10 de severidad Alta, mencionar cuántos quedan sin reportar.

### Estructura del reporte

```
## S2 — FSM Audit — Reporte de Auditoría

**Fecha:** <fecha>

### FSM por Agregado
(resumen estructural por cada agregado)

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

### Resumen
- Alta: N | Media: N | Baja: N
- Total: N hallazgos
```

### Regla de oro

> **NO reescribir el documento.** Solo diagnosticar y sugerir la corrección mínima necesaria. No proponer reestructuraciones, no agregar secciones nuevas, no cambiar convenciones existentes.
