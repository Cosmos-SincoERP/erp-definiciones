---
name: audit-structure-composition
description: "Audita la composición de agregados en modelos de dominio DDD/ES: cruza entidades, value objects y atributos documentados contra los payloads de eventos para detectar atributos huérfanos, VO duplicados vs reutilizados, entidades mal ubicadas y componentes referenciados en eventos pero ausentes en la composición. Úsalo cuando el usuario modifique la estructura de un agregado, cuando se agreguen o eliminen entidades o value objects, cuando se cambien payloads de eventos, cuando se mencione composición, atributos, value objects, entidades internas, o cuando se detecten inconsistencias entre lo documentado en la composición y lo referenciado en los eventos."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Audit Structure — Composición de Agregados (VO + Entidades)

Especialista en diseño estructural de agregados fundamentado en los patrones de arquitectura y diseño **DDD**, **POO** y **Event Sourcing**: coherencia entre composición documentada (entidades, VO, atributos), payloads de eventos y comportamientos calculados.

## Qué audita

Verifica que la estructura interna de cada agregado (entidades, value objects, atributos) sea coherente con lo que los eventos capturan, las precondiciones verifican y los comportamientos calculados consumen. Trabaja sobre el modelo de dominio presente en la ventana de contexto actual.

## Procedimiento

1. Identificar en el contexto todos los agregados con su composición documentada (entidades internas, value objects, diagramas de composición).
2. Identificar el catálogo de eventos con su "Información capturada" por evento.
3. Para cada agregado:

- [ ] **Inventariar composición:** listar entidades internas, value objects y atributos declarados en la sección de composición del agregado.
- [ ] **Inventariar uso en eventos:** para cada evento del agregado, extraer los datos referenciados en "Información capturada" y "Precondiciones".
- [ ] **Cruce composición → eventos:** cada entidad/VO/atributo documentado en la composición debe ser referenciado por al menos un evento (captura, crea, modifica, consulta). Reportar componentes declarados pero nunca referenciados (huérfanos).
- [ ] **Cruce eventos → composición:** cada dato referenciado en "Información capturada" de un evento debe existir en la composición del agregado (como entidad, VO o atributo). Reportar datos referenciados pero no documentados en la composición.
- [ ] **VO duplicados vs reutilizados:** si dos o más agregados usan el mismo VO (ej: `InformacionTercero`), verificar que esté documentado como VO compartido y no duplicado con nombres distintos. Si existen VO con nombres distintos pero estructura idéntica, señalar posible duplicación.
- [ ] **Entidades mal ubicadas:** verificar que cada entidad interna pertenezca al agregado correcto según las invariantes que protege. Si una entidad es modificada por eventos de otro agregado (vía domain service), señalar si debería ser entidad del otro agregado.
- [ ] **Comportamientos calculados:** verificar que los comportamientos calculados documentados (ej: `saldoPorPagar()`, `valorNeto()`) referencien solo componentes que existen en la composición del agregado.
- [ ] **Consistencia con decisiones de diseño:** si el modelo documenta decisiones sobre por qué un componente es entidad vs VO, verificar coherencia.

4. Producir tabla de inconsistencias + hallazgos.

## Formato de salida

### Inventario de composición (uno por agregado)

```
### Composición: <NombreAgregado>

**Entidades internas:** E1, E2, ...
**Value Objects:** VO1, VO2, ...
**VO compartidos:** VOs1, VOs2, ...
**Comportamientos calculados:** fn1(), fn2(), ...
```

### Tabla de inconsistencias

| Agregado | Componente | Declarado en composición | Referenciado en eventos | Tipo de inconsistencia |
|----------|-----------|-------------------------|------------------------|----------------------|

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
## Audit Structure — Composición — Reporte de Auditoría

**Fecha:** <fecha>

### Inventario por Agregado
(composición documentada)

### Inconsistencias

| Agregado | Componente | Declarado en composición | Referenciado en eventos | Tipo de inconsistencia |
|----------|-----------|-------------------------|------------------------|----------------------|

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

### Resumen
- Alta: N | Media: N | Baja: N
- Total: N hallazgos
```

### Regla de oro

> **NO reescribir el documento.** Solo diagnosticar y sugerir la corrección mínima necesaria. No proponer reestructuraciones, no agregar secciones nuevas, no cambiar convenciones existentes.
