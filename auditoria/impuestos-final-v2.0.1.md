# Audit Full — Reporte de Auditoría Final (v2.0.1)

**Fecha:** 2026-05-20
**Modelo auditado:** `dominio/impuestos/modelo-dominio.md` v2.0.1 (post-auditoría completa)

---

## Resumen Ejecutivo

| Skill | Alta | Media | Baja | Total |
|-------|:----:|:-----:|:----:|:-----:|
| Glosario | 1 | 3 | 1 | 5 |
| Composición | 3 | 3 | 1 | 7 |
| FSM | 0 | 0 | 0 | 0 |
| Invariantes | 0 | 3 | 1 | 4 |
| Responsabilidades | 0 | 4 | 2 | 6 |
| Semántica Eventos | 0 | 4 | 2 | 6 |
| Idempotencia | 2 | 4 | 1 | 7 |
| Sagas | 0 | 4 | 3 | 7 |
| Decisiones Abiertas | 0 | 3 | 2 | 5 |
| Sanity Check | 1 | 2 | 3 | 6 |
| **TOTAL** | **7** | **30** | **16** | **53** |

---

## Hallazgos Alta — Clusters consolidados

### Cluster 1 — Diagrama de `RegistroTributario` desincronizado tras unificación

Hallazgos: Glosario-1, Composición-4, Sanity-1.

- **L961-970** — Diagrama ASCII de la composición de `RegistroTributario` sigue mostrando `LineaDesgloseMotor` y `LineaDescartada` como entidades separadas, cuando la composición textual (L895) ya las unificó como `LineaDeDesglose` con atributo `proposito: confirmado | referencia | descartada`.
- **Corrección mínima:** Reemplazar los recuadros del diagrama por `LineaDeDesglose · proposito: referencia` y `LineaDeDesglose · proposito: descartada`.

### Cluster 2 — Eventos `*Modificada` desalineados con identidades sintéticas

Hallazgos: Composición-1, Composición-2, Composición-3.

- **L1624** `EntradaDeTarifaModificada` identifica por `Factor + vigencia` y lista `tipoTarifa` como modificable. Pero `[I25]` declara `entradaId` como identidad y solo `tarifa, vigencia.fechaHasta, cuantiaMinima` como modificables.
- **L1633** `CondicionModificada` lista `direccionFiscalAplicable` y `vigencia` completa como modificables. Pero `[I24]` declara la tupla inmutable (incluyendo esos dos campos).
- **L1653-1655** Eventos de `ActividadEconomicaRegistrada` ignoran `actividadId` declarado por la composición y la convención global; identifican por tupla.
- **Corrección mínima:** Sincronizar los payloads de los eventos con las identidades declaradas en las invariantes y composiciones.

### Cluster 3 — Idempotencia financiera con invariantes eventuales

Hallazgos: Idempotencia-I1 (`[I18]`), Idempotencia-I2 (`[I19]`).

- `[I18]` (unicidad del hecho fiscal por terna `subDominio + transaccionId + efectoFiscal`) clasificada como **eventual** pero protege propiedad financiera crítica — la guarda check-then-write del flujo no garantiza atomicidad bajo concurrencia.
- `[I19]` (saldo de desgravámenes acotado por gravamen origen) clasificada como **eventual** y enforce por suma agregada — dos desgravámenes concurrentes pueden cada uno leer el saldo en N y aprobar montos que sumen > origen.
- **Corrección mínima:** Reclasificar a invariantes **locales fuertes** vía mecanismo concreto (índice único en sumario de confirmaciones, o ledger de unicidad por `transaccionId`) o domain service que serialice por `transaccionOrigenId`. La idempotencia financiera no puede depender de proyecciones eventuales.

---

## Hallazgos Media (30) y Baja (16)

(Reporte completo de hallazgos disponible en las salidas individuales de los 4 agentes auditores.)

---

## Estado del modelo

**Hallazgos clave residuales:**
- **Inconsistencias residuales del refactor de identidades** (Cluster 2): el bloque B1 de la auditoría anterior introdujo identidades sintéticas (`entradaId`, `actividadId`) y tupla inmutable de `Condicion` con `[I24]`, pero los payloads de los eventos `*Modificada` no se actualizaron en consecuencia.
- **Diagrama desactualizado** (Cluster 1): el bloque B9 unificó `LineaDeDesglose` con `proposito`, pero el diagrama ASCII de composición no se sincronizó.
- **Cluster 3 es un gap conceptual real** que requiere decisión arquitectónica sobre la naturaleza de `[I18]` y `[I19]`.

**FSM completamente limpia** (0 hallazgos en Capa 1 / FSM) — bien construida con guards explícitos.

**Catálogo de invariantes saludable** salvo los tres clusters identificados.
