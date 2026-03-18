# Audit Full — Reporte de Auditoría Completa

**Fecha:** 2026-03-18
**Modelo auditado:** `obligaciones-por-pagar/modelo-dominio.md` (v2.8, ~2186 líneas, 5 agregados, 51 eventos, 17 invariantes, 24 decisiones)

---

## 1. Glosario y Lenguaje Ubicuo

Cruce del vocabulario del modelo contra el glosario canónico de `definicion-alcance.md` (Sección 2).

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| G1 | Media | L25 `R01–R35`, L2097 `R01–R35` | El rango de reglas referenciado en Sección 1 y Sección 8 dice R01–R35, pero `definicion-alcance.md` tiene reglas hasta R37 (R36: clasificación inteligente, R37: validación tributos proveedor). | Actualizar ambas ocurrencias a `R01–R37`. |
| G2 | Baja | L1574 `PartidaCubiertaPorAnticipo` Causalidad: "contraparte de AnticipoVinculadoAPartida (Anticipo)". L1430 `AnticipoVinculadoAPartida` Causalidad: "contraparte de PartidaCubiertaPorAnticipo (OxpExtracto)". | Ambos eventos referencian su contraparte pero no nombran el domain service que los coordina. Todos los demás pares de entidades espejo (Vinculacion↔PagoAplicado, CoberturaDevolucion) nombran explícitamente su coordinador. | Agregar `ServicioDeConciliacion` como coordinador en la causalidad de ambos eventos. |

### Resumen
- Alta: 0 | Media: 1 | Baja: 1
- Total: 2 hallazgos

---

## 2. Composición de Agregados

Cruce de entidades, value objects y atributos documentados en la composición contra los payloads de los eventos del catálogo.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| C1 | Media | L1104 tabla de entidades espejo: `CoberturaAnticipo ↔ CrucePagoAplicado tipo extracto (Anticipo) — ServicioDeConciliacion`. ServicioDeConciliacion (L886-923) solo documenta 2 flujos: vinculación de compras y partidas de retorno. | La tabla de entidades espejo afirma que `ServicioDeConciliacion` coordina la cobertura de anticipo (`PartidaCubiertaPorAnticipo` ↔ `AnticipoVinculadoAPartida`), pero el servicio no documenta un flujo de cobertura de anticipo. Falta el tercer flujo con su tabla de compensación. | Documentar el tercer flujo de cobertura de anticipo en ServicioDeConciliacion con pasos, tabla de compensación y protocolo de proceso. |
| C2 | Baja | L835-839 `CatalogoGastoDirecto`: stream `catalogo-gasto-directo-{id}`. | No se documenta si el catálogo es singleton por empresa o instanciable múltiples veces. El `{id}` del stream es ambiguo — ¿es un UUID arbitrario o un empresaId? Sin esta definición, no se puede determinar el scope de la unicidad de código de `ConceptoGastoDirecto`. | Documentar scope (singleton por empresa) y actualizar stream a `catalogo-gasto-directo-{empresaId}`. |

### Resumen
- Alta: 0 | Media: 1 | Baja: 1
- Total: 2 hallazgos

---

## 3. Máquinas de Estado

Validación de FSM: estados huérfanos, transiciones imposibles, estados sumidero no intencionados, terminales inconsistentes con saldos.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| FSM1 | Media | L1256-1284 FSM Anticipo: único estado inicial es Vigente. L586 y L984-997 Ramas B/C de ServicioDeAplicacionDevolucion: "Anticipo nace con CrucePagoAplicado tipo devolucion... saldoPorPagar() = 0 → estado Pagado". | La FSM de Anticipo solo muestra Vigente como estado inicial. Sin embargo, un anticipo nacido de devolución (Ramas B/C) nace directamente en estado Pagado (`saldoPorPagar()` = 0 desde el momento de creación). La FSM no refleja esta entrada directa a Pagado. | Agregar entrada directa a Pagado en el diagrama FSM y en las notas para anticipos nacidos de devolución. |

### Resumen
- Alta: 0 | Media: 1 | Baja: 0
- Total: 1 hallazgo

---

## 4. Invariantes

Clasificación de invariantes (local vs eventual), detección de enforcement faltante, coherencia con decisiones.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| INV1 | Baja | L846 "Invariante: unicidad de código dentro del catálogo" (tabla de entidades). L1941 precondición "Código único dentro del catálogo". | La unicidad de código de `ConceptoGastoDirecto` está mencionada en la composición y en precondiciones, pero no tiene un identificador formal `I##` en la Sección 7 de invariantes. Todas las demás restricciones estructurales están formalizadas como I1–I17. | Agregar I18: unicidad de código en CatalogoGastoDirecto. |

### Resumen
- Alta: 0 | Media: 0 | Baja: 1
- Total: 1 hallazgo

---

## 5. Responsabilidades de Agregados

Detección de agregados anémicos, inflados, lógica fugada a servicios, violaciones de SRP.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| RS1 | Media | L846 `ConceptoGastoDirecto`: `clasificacionTributaria (ref. catálogo Impuestos), conceptoPago (ref. catálogo Impuestos)`. L1941-1943 eventos de CatalogoGastoDirecto: precondiciones no mencionan validación contra Impuestos. | `CatalogoGastoDirecto` almacena referencias fiscales (`clasificacionTributaria`, `conceptoPago`) que apuntan al catálogo de Impuestos, pero no hay mención de validación de estas referencias al agregar o modificar conceptos. Si las referencias son inválidas, las OxpComercio directas fallarían al solicitar cálculo a Impuestos. | Agregar precondición en `ConceptoGastoDirectoAgregado` y `ConceptoGastoDirectoModificado`: clasificacionTributaria y conceptoPago deben ser referencias válidas al catálogo de Impuestos `[D22]`. |

### Resumen
- Alta: 0 | Media: 1 | Baja: 0
- Total: 1 hallazgo

---

## 6. Semántica de Eventos

Validación de naming, payloads, granularidad, emisor único, solapamientos.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| ES1 | **Alta** | L604 `CrucePagoAplicado` tipo `devolucion`: "ref. a Devolucion que originó el anticipo; creado por ServicioDeAplicacionDevolucion al crear el anticipo por excedente". Catálogo Anticipo (Sección 5.5): no existe evento que documente la creación del anticipo con su CrucePagoAplicado tipo devolucion. | **Violación fundamental de event sourcing.** El anticipo nacido de devolución (Ramas B/C de ServicioDeAplicacionDevolucion) nace con un `CrucePagoAplicado` tipo `devolucion` que cubre 100% del `valorTotal` (estado Pagado desde nacimiento). Pero no existe ningún evento en el catálogo del Anticipo que registre este hecho de creación. Sin evento → sin replay → el estado se pierde al reconstruir el agregado. Los demás tipos de cruce tienen sus eventos creadores documentados: `AnticipoVinculadoAPartida` (extracto), `PagoAnticipoAplicado` (pago_directo), `AnticipoReversado` (reversa). Solo `devolucion` carece de evento creador. | Agregar evento `AnticipoRegistrado` en Sección 5.5 que documente la creación del anticipo incluyendo, cuando el origen es devolución, el CrucePagoAplicado tipo devolucion como parte de la información capturada. El anticipo nace en estado Pagado. |
| ES2 | Baja | L1940 `CatalogoGastoDirectoCreado`: Información capturada = "—" (vacía). | El evento de creación del catálogo no captura ningún dato. En event sourcing, cada evento debe registrar suficiente información para reconstruir el estado. Como mínimo debería capturar la referencia a la empresa (empresaId). | Agregar información capturada: `empresaId, fecha de creación`. |
| ES3 | Baja | L1890 `DevolucionRadicada` precondiciones: lista detallada por tipo (Comercio, Extracto, Anticipo) pero sin referencias a reglas de negocio de devoluciones. | `DevolucionRadicada` no referencia las reglas de negocio que rigen la radicación de devoluciones (R28: nota crédito, R31-R34: reglas de soporte documental). Los demás eventos de radicación (`OxpComercioRadicada`, `ExtractoRadicado`) sí referencian sus reglas correspondientes. | Agregar referencias a `[R28]` y reglas de soporte documental aplicables en las precondiciones de `DevolucionRadicada`. |

### Resumen
- Alta: 1 | Media: 0 | Baja: 2
- Total: 3 hallazgos

---

## 7. Idempotencia y Concurrencia

Detección de operaciones sin identificador único, reglas anti-duplicado faltantes, enforcement insuficiente.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| ID1 | Media | L2131 D20: "idempotencyKey (deduplicación de mensajes): garantizada por Wolverine vía inbox/outbox pattern". Pagos externos: `PagoOxpComercioDirectoAplicado`, `PagoAnticipoAplicado`, `CrucePagoExtractoAplicado` tipo pago_sincoa — ninguno documenta identificador de negocio del pago. | D20 delega idempotencia a la plataforma (Wolverine), lo cual es correcto para mensajes internos. Pero los pagos confirmados por SincoA&F son eventos de integración — la deduplicación técnica no reemplaza la necesidad de un identificador de negocio del pago externo (ej: número de transacción SincoA&F) que permita detección de duplicados a nivel de dominio. | Agregar nota en D20 sobre pagos externos: deben incluir referencia de origen del pago (número de transacción SincoA&F) para trazabilidad y detección de duplicados a nivel de dominio. |

### Resumen
- Alta: 0 | Media: 1 | Baja: 0
- Total: 1 hallazgo

---

## 8. Procesos Multi-Agregado (Sagas)

Detección de domain services sin trigger, pasos sin compensación, correlationId faltante.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| SG1 | Media | L1104 tabla entidades espejo: `CoberturaAnticipo ↔ CrucePagoAplicado (Anticipo) — ServicioDeConciliacion`. ServicioDeConciliacion (L886-923): no tiene flujo de cobertura de anticipo ni tabla de compensación. | La cobertura de anticipo opera sobre 2 streams (OxpExtracto + Anticipo), lo que la hace un proceso multi-agregado que requiere tabla de compensación. Sin embargo, `ServicioDeConciliacion` solo documenta compensación para el flujo de vinculación de compras. El flujo de cobertura de anticipo carece de: (1) flujo documentado con pasos, (2) tabla de compensación bilateral, (3) protocolo de proceso con correlationId. | Documentar flujo de cobertura de anticipo como tercer flujo de ServicioDeConciliacion con tabla de compensación bilateral y protocolo de proceso. Relacionado con C1 (composición). |

### Resumen
- Alta: 0 | Media: 1 | Baja: 0
- Total: 1 hallazgo

---

## 9. Decisiones Abiertas

Detección de pendientes sin resolver, decisiones implícitas, TODOs sin ownership.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| OD1 | Media | L2133 D22: "Para devoluciones tipo Comercio: efectoFiscal = desgravamen + transaccionOrigenId = OxpComercio original — Impuestos prorratea del gravamen, no invoca al motor." | El prorrateo de desgravamen está definido para el caso general (devolución total = desgravar todo), pero no está definido qué ocurre con devoluciones parciales: si se devuelven 2 de 5 conceptos, ¿el prorrateo es exacto por concepto o proporcional? El mecanismo es responsabilidad de Impuestos, pero OXP debería documentar qué información envía para que Impuestos pueda resolver. | Agregar pendiente PD4: prorrateo de desgravamen para devoluciones parciales — OXP envía ConceptoDevuelto con valores y transaccionOrigenId, Impuestos define el mecanismo. |
| OD2 | Media | L2134 D23: "Cuando el soporte trae tributos del proveedor, se validan contra el cálculo de Impuestos [R37]". L1659-1672 `OxpComercioRadicada`, `OxpComercioConfirmada`: sin referencia a [R37]. | R37 (validación de tributos del proveedor contra cálculo de Impuestos) está mencionada en D23 pero nunca se operacionaliza en los eventos donde debería ejecutarse (`OxpComercioRadicada` al solicitar cálculo, `OxpComercioConfirmada` al confirmar desglose definitivo). | Agregar referencia `[R37]` en efectos de `OxpComercioRadicada` (validación al solicitar cálculo) y en `OxpComercioConfirmada` (confirmación del desglose definitivo). |

### Resumen
- Alta: 0 | Media: 2 | Baja: 0
- Total: 2 hallazgos

---

## 10. Sanity Check (Coherencia Cruzada)

Detección de contradicciones entre secciones, referencias rotas, conteos inconsistentes.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| SC1 | Media | L25 `R01–R35`, L2097 `R01–R35`. `definicion-alcance.md` Sección 6 tiene reglas hasta R37. | Misma evidencia que G1 (glosario). Las dos secciones que referencian el rango de reglas (Sección 1: propósito, Sección 8: exclusiones) están desactualizadas — no reflejan R36 (clasificación inteligente) ni R37 (validación tributos proveedor) que se agregaron después de v2.6. | Actualizar ambas ocurrencias a `R01–R37`. |
| SC2 | Baja | L1671 `[D9-Imp]`, L1958 `[D9-Imp]`. Sección 2 (convenciones): no hay convención para notación de referencia cruzada a otros sub-dominios. | La notación `[D9-Imp]` (referencia a decisión D9 del modelo de Impuestos) se usa en dos ubicaciones pero no está documentada en las convenciones. El lector podría confundirla con una decisión local mal numerada. | Agregar convención en Sección 2: `[D##-Xxx]` refiere a decisión del sub-dominio indicado. |
| SC3 | Baja | L2134 D23: "clasificación inteligente" sin referencia formal `[R36]`. L148 diagrama BC: "Clasificación inteligente [D23]" sin `[R36]`. | R36 (clasificación inteligente de origen) está operacionalizada en D23, pero la referencia formal `[R36]` no aparece en el modelo. Patrón: las demás reglas operacionalizadas se referencian explícitamente (ej: R05c, R08, R12, R26). | Agregar `[R36]` junto a las menciones de clasificación inteligente en D23 y en el diagrama BC. |

### Resumen
- Alta: 0 | Media: 1 | Baja: 2
- Total: 3 hallazgos

---

## Resumen Ejecutivo

| Skill | Alta | Media | Baja | Total |
|-------|------|-------|------|-------|
| Glosario | 0 | 1 | 1 | 2 |
| Composición | 0 | 1 | 1 | 2 |
| FSM | 0 | 1 | 0 | 1 |
| Invariantes | 0 | 0 | 1 | 1 |
| Responsabilidades | 0 | 1 | 0 | 1 |
| Semántica Eventos | 1 | 0 | 2 | 3 |
| Idempotencia | 0 | 1 | 0 | 1 |
| Sagas | 0 | 1 | 0 | 1 |
| Decisiones Abiertas | 0 | 2 | 0 | 2 |
| Sanity Check | 0 | 1 | 2 | 3 |
| **TOTAL** | **1** | **9** | **7** | **17** |

### Hallazgos duplicados cruzados

- **G1 = SC1:** Rango R01–R35 → R01–R37 (detectado por Glosario y Sanity Check)
- **C1 ≈ SG1:** ServicioDeConciliacion sin flujo de cobertura de anticipo (detectado por Composición y Sagas)

**Hallazgos únicos:** 15

### Top 5 hallazgos críticos

1. **ES1 (Alta)** — `CrucePagoAplicado` tipo `devolucion` sin evento creador en Anticipo. Violación fundamental de event sourcing: sin evento no hay replay, el estado se pierde.
2. **C1/SG1 (Media)** — ServicioDeConciliacion sin flujo de cobertura de anticipo. Proceso multi-agregado sin documentación de pasos, compensación ni protocolo.
3. **FSM1 (Media)** — FSM Anticipo no refleja entrada directa a Pagado para anticipos nacidos de devolución.
4. **OD2 (Media)** — R37 (validación tributos proveedor) mencionada en D23 pero no operacionalizada en eventos.
5. **RS1 (Media)** — CatalogoGastoDirecto almacena referencias fiscales sin validar contra catálogo de Impuestos.

### Comparación con auditoría anterior (v2.5)

| Métrica | v2.5 | v2.8 |
|---------|------|------|
| Hallazgos totales | 60 | 17 |
| Alta | 1 | 1 |
| Media | 36 | 9 |
| Baja | 23 | 7 |

La reducción de 60 → 17 hallazgos refleja la madurez del modelo tras las correcciones de v2.6 (44 hallazgos Media/Baja resueltos). Los hallazgos restantes son principalmente gaps introducidos por las features nuevas de v2.8 (CatalogoGastoDirecto, integración con Impuestos, fases F1/F2) y un gap fundamental (ES1) que existía desde v2.2 pero no fue detectado.
