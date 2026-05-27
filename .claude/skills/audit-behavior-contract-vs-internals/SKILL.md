---
name: audit-behavior-contract-vs-internals
description: "Audita la coherencia entre el contrato externo de un servicio/agregado/proceso y la forma del flujo interno que lo implementa. Detecta colapso de cardinalidad (flujo itera por N dimensiones pero el contrato expone 1), dimensiones perdidas (flujo calcula por (X,Y) pero el contrato solo expone X), campos del contrato sin computación interna, computación interna no expuesta, y discrepancias de tipo entre contrato y valor calculado. Úsalo cuando el usuario pida revisar coherencia de contratos, cuando se mencione discrepancia entre input/output y flujo, cuando el cliente del contrato no pueda reconstruir la decisión interna, o cuando se detecten dimensiones ocultas."
disable-model-invocation: false
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Audit Behavior — Contrato vs Flujo Interno

Especialista en coherencia entre la **forma del contrato externo** (input/output de un domain service, método calculado expuesto de un agregado, contrato de orquestación de una saga, evento de integración) y la **forma del flujo interno** que lo implementa. Detecta gaps conceptuales donde el contrato externo no representa fielmente lo que el flujo interno computa.

## Qué audita

Valida que la **dimensionalidad y cardinalidad del contrato** coincidan con las del flujo interno. Una discrepancia típica: el flujo itera por N elementos (por tributo, por línea, por jurisdicción) y produce N resultados, pero el contrato externo expone un único valor consolidado — el cliente no puede reconstruir las decisiones por elemento. Trabaja sobre el modelo de dominio presente en la ventana de contexto actual.

Aplicabilidad:
- **Domain Services** con contrato input/output documentado.
- **Métodos calculados de agregados** expuestos como API pública.
- **Sagas y flujos orquestados** con contrato de orquestación.
- **Eventos de integración** que actúan como contrato hacia consumidores externos.

## Procedimiento

1. Identificar en el contexto los **contratos públicos**: input/output de domain services, métodos calculados de agregados, contratos de orquestación, eventos de integración con consumidores. Para cada uno, anotar línea aproximada.
2. Para cada contrato:

- [ ] **Mapeo del flujo interno:** identificar las dimensiones que el flujo recorre (iteraciones por agregado, por entidad interna, por colección; agrupaciones; decisiones por elemento). Anotar la cardinalidad esperada (1, N, N×M).
- [ ] **Mapeo del contrato externo:** identificar la forma del input/output (escalar, lista, matriz, objeto compuesto). Anotar cardinalidad y dimensiones expuestas.
- [ ] **Colapso de cardinalidad:** ¿el flujo interno itera por N elementos y produce N resultados, pero el contrato externo expone solo 1 valor? Si el colapso es deliberado, ¿está documentado el criterio de selección/agregación?
- [ ] **Dimensión perdida:** ¿el flujo interno computa por dos o más dimensiones (ej: por `(tributo, jurisdicción)`) pero el contrato solo expone una (ej: solo `jurisdicción`)? El cliente pierde la asociación.
- [ ] **Campo del contrato sin computación interna:** ¿hay campos declarados en input/output que no están conectados a ningún paso del flujo interno? Síntoma: el contrato menciona un dato que el motor nunca calcula ni recibe.
- [ ] **Computación interna no expuesta:** ¿el flujo interno toma decisiones o calcula información relevante para el cliente que no se expone en el contrato? Síntoma: el cliente debe re-deducir o consultar para reconstruir lo que el motor ya sabe.
- [ ] **Coherencia de tipos:** ¿el tipo declarado en el contrato (string, número, objeto) refleja la naturaleza real del valor computado (ej: lista, matriz, objeto compuesto)? Síntoma: campo declarado como string singular cuando internamente representa múltiples valores.
- [ ] **Pérdida de contexto de decisión:** ¿el consumidor del contrato puede reconstruir las decisiones tomadas internamente con la información que recibe? Si el motor aplicó N reglas distintas y el output solo dice "resultado: X", el consumidor no puede auditar por qué.
- [ ] **Naming del campo:** ¿el nombre del campo en el contrato refleja correctamente su contenido y cardinalidad? Síntoma: `jurisdiccionResuelta` (singular) cuando hay varias jurisdicciones resueltas (una por tributo).

3. Producir matriz contrato ↔ flujo + hallazgos.

## Formato de salida

### Matriz Contrato ↔ Flujo (uno por contrato auditado)

```
### Contrato: <NombreContrato> (L~N)

**Tipo de contrato:** domain-service-io / metodo-calculado / contrato-saga / evento-integracion
**Agregado/servicio dueño:** <Nombre>

**Dimensiones del flujo interno:**
- Itera por: <dimensión 1>, <dimensión 2>, ...
- Cardinalidad esperada del resultado: 1 / N / N×M / ...
- Decisiones tomadas por elemento: <lista>

**Forma del contrato externo:**
- Input: <forma>
- Output: <forma>
- Cardinalidad expuesta: 1 / N / N×M / ...

**Diagnóstico:** Coherente / Colapso de cardinalidad / Dimensión perdida / Campo huérfano / Computación oculta / Tipo inconsistente
```

### Tabla de hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

## Protocolo de salida

### Severidad

| Nivel | Criterio |
|-------|----------|
| **Alta** | El consumidor del contrato no puede reconstruir la decisión interna; el contrato declara campos no computados; pérdida de información financiera/auditable |
| **Media** | Ambigüedad en la forma del contrato; cardinalidad ambigua; computación interna relevante no expuesta sin pérdida directa |
| **Baja** | Naming inconsistente, claridad, sugerencias de tipado más expresivo |

### Reglas

- **Evidencia concreta:** siempre citar línea aproximada (`L~N`) y fragmento textual entre comillas tanto del contrato como del flujo interno.
- **Un hallazgo = un problema atómico.** Si una discrepancia genera varias inconsistencias (ej: una dimensión perdida afecta input y output), separar por hallazgo.
- **Corrección mínima:** la intervención más pequeña que resuelve el problema sin reestructurar el contrato. Opciones típicas: cambiar cardinalidad del campo (escalar→lista), agregar dimensión faltante al objeto del contrato, eliminar campo huérfano del contrato, documentar criterio de agregación cuando el colapso es deliberado.
- **Orden:** Alta → Media → Baja.
- **Máximo 10 hallazgos** priorizados por severidad. Si hay más de 10 de severidad Alta, mencionar cuántos quedan sin reportar.

### Estructura del reporte

```
## Audit Behavior — Contrato vs Flujo Interno — Reporte de Auditoría

**Fecha:** <fecha>

### Matriz Contrato ↔ Flujo
(un bloque por contrato auditado)

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|

### Resumen
- Alta: N | Media: N | Baja: N
- Total: N hallazgos
```

### Regla de oro

> **NO reescribir el documento.** Solo diagnosticar y sugerir la corrección mínima necesaria. No proponer reestructuraciones del contrato ni del flujo interno; identificar el desalineamiento y la intervención puntual que lo resuelve.
