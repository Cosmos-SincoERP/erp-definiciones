---
name: audit-quality-sanity-check
description: "Meta-auditor de coherencia cruzada en modelos de dominio DDD/ES/EDA: detecta contradicciones entre secciones, referencias rotas (R##, P##, I##, D##), conteos inconsistentes, decisiones que contradicen el modelo actual, premisas no reflejadas en invariantes y conceptos eliminados que aún se referencian. Úsalo cuando el usuario pida un sanity check, cuando se hagan cambios grandes al modelo, cuando se mencione coherencia, contradicciones, referencias rotas, o como auditoría final después de las demás skills."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Audit Quality — Sanity Check (Coherencia Cruzada)

Meta-auditor de coherencia documental fundamentado en los patrones de arquitectura y diseño **DDD**, **Event Sourcing** y **EDA**: detección de contradicciones entre secciones, referencias rotas, conteos inconsistentes y desalineaciones post-edición. No repite el análisis de skills individuales — solo verifica coherencia cruzada.

## Qué audita

Verifica que el modelo de dominio sea internamente consistente: que lo que dice una sección no contradiga otra, que todas las referencias estén resueltas, que los conteos coincidan y que no queden rastros de conceptos eliminados. Trabaja sobre el modelo de dominio presente en la ventana de contexto actual.

## Procedimiento

1. Identificar en el contexto el modelo de dominio completo con todas sus secciones.
2. Ejecutar los siguientes chequeos cruzados:

- [ ] **Contradicciones entre secciones:** un evento dice X en el catálogo pero la FSM dice Y. Un estado tiene nombre diferente en la FSM vs en la composición. Un invariante referencia un componente con un nombre y otro sección usa otro nombre.
- [ ] **Referencias rotas — definidas pero no usadas:** identificadores `[R##]`, `[P##]`, `I##`, `D##` definidos en su sección canónica pero nunca referenciados en el resto del modelo.
- [ ] **Referencias rotas — usadas pero no definidas:** identificadores `[R##]`, `[P##]`, `I##`, `D##` mencionados en el texto pero sin definición en la sección correspondiente.
- [ ] **Conteos inconsistentes:** si el modelo dice "12 eventos propios", contar los eventos reales en el catálogo. Si dice "5 estados", contar en la FSM. Reportar discrepancias.
- [ ] **Decisiones vs modelo actual:** si el modelo documenta decisiones de diseño (D##), verificar que el modelo actual las refleje. Una decisión dice "X es entidad" pero la composición lo muestra como VO (o viceversa).
- [ ] **Premisas no reflejadas:** si el modelo documenta premisas de negocio (P##), verificar que estén operacionalizadas en invariantes, guards o composición. Una premisa sin reflejo en el modelo es dead weight.
- [ ] **Reglas cross-aggregate imposibles:** invariantes o reglas que pretenden garantizarse sincrónicamente pero involucran más de un agregado. Un solo agregado no puede garantizar una invariante que depende de otro.
- [ ] **Relaciones bidireccionales consistentes:** si AgregadoA dice que se relaciona con AgregadoB de cierta forma, verificar que AgregadoB documente la relación inversa de forma consistente.
- [ ] **Conceptos eliminados o renombrados:** secciones que todavía referencian conceptos, estados, eventos o entidades que fueron eliminados o renombrados en versiones recientes del changelog.

3. Producir tabla de inconsistencias + hallazgos.

## Formato de salida

### Resumen de coherencia

```
### Coherencia Cruzada

**Referencias verificadas:** N definidas, N usadas, N rotas
**Conteos verificados:** N correctos, N inconsistentes
**Decisiones vigentes:** N alineadas, N desalineadas
**Premisas operacionalizadas:** N reflejadas, N sin reflejo
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
## Audit Quality — Sanity Check — Reporte de Auditoría

**Fecha:** <fecha>

### Coherencia Cruzada
(resumen de verificaciones)

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

### Resumen
- Alta: N | Media: N | Baja: N
- Total: N hallazgos
```

### Regla de oro

> **NO reescribir el documento.** Solo diagnosticar y sugerir la corrección mínima necesaria. No proponer reestructuraciones, no agregar secciones nuevas, no cambiar convenciones existentes.
