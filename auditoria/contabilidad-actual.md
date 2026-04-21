# Reporte de Auditoría Consolidado — Contabilidad

**Fecha de consolidación:** 2026-04-08
**Modelo auditado:** `dominio/contabilidad/modelo-dominio.md` (v1.0 entregada)
**Alcance de referencia:** `dominio/contabilidad/definicion-alcance.md` (v1.0 entregada)

---

## Resumen de auditorías ejecutadas

| Auditoría | Fecha | Modelo | Hallazgos | Aplicados | Descartados |
|-----------|-------|--------|:---------:|:---------:|:-----------:|
| V1 | Abril 2026 | v1.1 (pre-ajustes PO) | 36 | 36 | 0 |
| V2 | Abril 2026 | v1.2 (post V1) | 31 | 31 | 0 |
| V3 | Abril 2026 | v1.5 (post ajustes PO) | 84 | 49 | 35 |
| **Total** | | | **151** | **116** | **35** |

---

## Auditoría V1 — 36 hallazgos (todos aplicados)

Ejecutada sobre el modelo v1.1 previo a los ajustes del comité de PO. Los 36 hallazgos fueron resueltos en su totalidad durante la construcción del modelo.

**Reporte original:** `auditoria/contabilidad-v1.md` (archivado)

---

## Auditoría V2 — 31 hallazgos (todos aplicados)

Ejecutada como auditoría estricta sobre el modelo v1.2 post V1. Los 31 hallazgos fueron resueltos en su totalidad.

**Reporte original:** `auditoria/contabilidad-v2.md` (archivado)

---

## Auditoría V3 — 84 hallazgos (49 aplicados, 35 descartados)

Ejecutada sobre el modelo v1.5 post ajustes del comité de PO (3 rondas). Auditoría completa de 10 skills.

### Resumen por skill

| Skill | Alta | Media | Baja | Total | Aplicados | Descartados |
|-------|:----:|:-----:|:----:|:-----:|:---------:|:-----------:|
| 1. Glosario | 0 | 3 | 7 | 10 | 3 | 7 |
| 2. Composición | 0 | 4 | 4 | 8 | 5 | 3 |
| 3. FSM | 0 | 4 | 4 | 8 | 3 | 5 |
| 4. Invariantes | 1 | 5 | 3 | 9 | 8 | 1 |
| 5. Responsabilidades | 0 | 3 | 4 | 7 | 3 | 4 |
| 6. Semántica Eventos | 0 | 4 | 4 | 8 | 2 | 6 |
| 7. Idempotencia | 2 | 4 | 2 | 8 | 4 | 4 |
| 8. Sagas | 0 | 5 | 4 | 9 | 5 | 4 |
| 9. Decisiones Abiertas | 0 | 3 | 4 | 7 | 1 | 6 |
| 10. Sanity Check | 4 | 4 | 2 | 10 | 10 | 0 |
| **TOTAL** | **7** | **39** | **38** | **84** | **49** | **35** |

### Resolución por severidad

| Severidad | Aplicados | Descartados | Total |
|-----------|:---------:|:-----------:|:-----:|
| Alta | 7 | 0 | 7 |
| Media | 29 | 10 | 39 |
| Baja | 13 | 25 | 38 |

### Top 5 hallazgos críticos (todos aplicados)

| # | Skill | Problema | Corrección |
|---|-------|----------|------------|
| 1 | Sanity Check | Desplazamiento sistemático de referencias [R##] en bloque N2 | 21+ referencias corregidas incluyendo I19, I20, I21 |
| 2 | Idempotencia | AsientoContabilizado sin guard anti-duplicado | I26 nueva — unicidad de referenciaOrigen en N2 |
| 3 | Idempotencia | ConsecutivoAsignado sin idempotency key | SI1 fortalecida con idempotencyKey (entregaId) y versión esperada |
| 4 | Invariantes | I13 clasificada como Local pero cruza dos agregados | Reclasificada como Eventual con mecanismo documentado |
| 5 | Sagas | ServicioDeContabilizacion sin correlationId ni idempotencyKey | correlationId (entregaId) + idempotencyKey formalizados |

### Principales correcciones aplicadas

**Bloque 1 — Referencias rotas (mecánico):**
- 21+ referencias [R##] corregidas en eventos, invariantes y premisas de N2

**Bloque 2 — Hallazgos Alta:**
- I26 nueva: guard anti-duplicado en AsientoContable (referenciaOrigen única en N2)
- I13 reclasificada Local→Eventual con mecanismo de consulta documentado
- SI1 fortalecida: idempotencyKey + versión esperada del stream

**Bloque 3 — Invariantes reclasificadas:**
- I4: Local→Eventual (consulta a PlanDeCuentas)
- I15: Local→Eventual (ServicioDeContabilizacion cruza PeriodoContable/AsientoContable)
- I16: Local→Eventual (ServicioDeContabilizacion cruza NumeracionContable/AsientoContable)
- I23: Local→Eventual (principio transversal, 3 agregados)

**Bloque 4 — Payloads completados:**
- esContrapartida y nivelResolucion en BorradorCreado y BorradorReemplazado
- referenciaHechoRelacionado en EntregaIniciada, EncabezadoAsiento y AsientoContabilizado
- MotivoRechazo VO alineado con evento (entregaId, destino)

**Bloque 5 — Sagas formalizadas:**
- correlationId en 4 domain services (referenciaOrigen, borradorId, asientoOriginalId, entregaId)
- idempotencyKey en ServicioDeAnulacion y ServicioDeContabilizacion

**Bloque 6 — FSM y mecánicas:**
- BorradorContable FSM: bifurcación de creación (PENDIENTE o RESUELTO directo)
- PeriodoContable FSM: flecha de creación→CERRADO (nace cerrado)
- BorradorRechazadoPorDestino: mecánica implícita RESUELTO→PENDIENTE documentada
- PartidaAgregada: emite CuentaResuelta como derivado [D2]
- Convención de estado transitorio en Sección 2.3
- combinacionDimensiones definida en Sección 2.5

**Bloque 7 — Nuevos artefactos:**
- D10: madurez N2 (especificación suficiente para integración, refinamiento en F2)
- SI3: payload mínimo en BorradorResuelto
- SI4: creación implícita del stream de Aprendizaje
- SI5: manejo de fallos persistentes en procesos multi-agregado
- SI6: optimistic concurrency en EntregaContable

### Hallazgos descartados — Criterios principales

| Criterio | Cantidad | Ejemplos |
|----------|:--------:|---------|
| Estilo/naming sin impacto funcional | 10 | SE2 (prefijo agregado), SE8 (naming Comprobante), F5 (condición vs estado) |
| Ya cubierto por otro mecanismo | 8 | ID7 (I9 cubre replay), SG6-SG8 (SI5 cubre principio general), R4 (EntregaRechazada propaga) |
| Changelog no se modifica (decisión del usuario) | 3 | OD1, OD7 |
| Diferido a F2 | 3 | OD4 (config I15), C4 parcial (N2 evaluará) |
| Patrón transversal existente | 2 | C5 (empresa implícita), SE4 (causalidad ya distingue) |
| Información suficiente en contexto | 4 | C7, C8 (parcial — se aplicó nota), IN6 (redacción original funcional), ID8 |
| Redundante con otro hallazgo | 1 | SE3 = F4 |

---

## Estado final del modelo

| Artefacto | Cantidad |
|-----------|:--------:|
| Agregados | 12 (7 N1 + 5 N2) |
| Eventos | 55 (27 transaccionales + 28 configuración) |
| Invariantes | 26 (18 Local + 8 Eventual) |
| Decisiones | 10 (D1-D10) |
| Premisas | 7 (P1-P7) |
| Pendientes | 3 (PD1-PD3) |
| Sugerencias de implementación | 6 (SI1-SI6) |
| Permisos atómicos | 17 |
| Domain services | 3 |
| Reglas de negocio (alcance) | 44 (8 frentes) |

**El modelo está listo para entregar al equipo de desarrollo (F1).**
