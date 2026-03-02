# Audit Full — Reporte de Auditoría Completa

**Fecha:** 2026-02-28
**Modelo auditado:** `obligaciones-por-pagar/modelo-dominio.md` (v2.5, 1967 líneas, 4 agregados, 47 eventos, 17 invariantes, 20 decisiones)

---

## 1. Glosario y Lenguaje Ubicuo

Cruce del vocabulario del modelo contra el glosario canónico de `definicion-alcance.md` (Sección 2, ~28 términos).

**Nota:** Los hallazgos G1 y G2 de la auditoría anterior (v2.4) —Compensada y signo de devolución contradiciendo el glosario— fueron **resueltos** en v2.5: `definicion-alcance.md` actualizado con D18 (Compensada eliminada) y D19 (valor positivo en devolución).

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima | Estado |
|---|-----------|-------------------------------|----------|-------------------|--------|
| G1 | Media | L42 `OxpComercio` vs Alcance L66 "OXP de Comercio" | El modelo usa nombres técnicos compactos (PascalCase) sin alias cruzado al término canónico del glosario. | Agregar alias en convenciones (Sección 2): `OxpComercio` = "OXP de Comercio". | Resuelto |
| G2 | Media | L42 `OxpExtracto` vs Alcance L67 "OXP de Extracto" | Mismo patrón que G1. Sin alias cruzado. | Agregar alias: `OxpExtracto` = "OXP de Extracto". | Resuelto |
| G3 | Media | L87 "Conciliación" + L968 "Parcialmente Conciliado" + L975 "Conciliado" + L699 "ServicioDeConciliacion" | Término polisémico: "conciliación" designa simultáneamente el proceso (domain service), un estado del extracto y la completitud de partidas. | Desambiguar en convenciones: "conciliación" = proceso; "Conciliado" = estado resultante del extracto. | Resuelto |
| G4 | Media | Alcance L70 "no cuenta con los soportes correspondientes" vs Modelo L462 `SoporteDocumental` (opcional) | El glosario canónico define Anticipo como obligación SIN soporte, pero el modelo permite `SoporteDocumental` opcional (ej: cuenta de cobro). | Actualizar glosario Alcance: "puede o no contar con soportes preliminares (ej: cuenta de cobro)". | Resuelto |
| G5 | Baja | L42 `Devolucion` (sin tilde) vs Alcance L73 "Devolución" | PascalCase sin tildes vs español formal. La diferencia es por convención de naming para compatibilidad con código fuente, pero no está documentada. | Documentar convención en Sección 2: "Los nombres de agregados y eventos usan PascalCase sin tildes para compatibilidad con código fuente." | Resuelto |
| G6 | Baja | Términos del modelo sin entrada en glosario: `ServicioDeRegularizacion`, `InstruccionDistribucion`, `DestinoDeNegocio` | Domain services y VOs técnicos del modelo no tienen entrada en glosario canónico. "Regularización" sí existe en glosario pero no los demás. | Informativo — son términos del modelo de dominio, no del glosario de negocio. | Resuelto |

### Resumen
- Alta: 0 | Media: 4 | Baja: 2
- Total: 6 hallazgos

---

## 2. Composición de Agregados

Cruce entre composición documentada (entidades, VOs, atributos) y payloads de eventos del catálogo.

**Nota:** Los hallazgos C1 (`lineasParaTraduccion()` en Anticipo) y C2 (`SoporteDocumental` en OxpExtracto) de la auditoría anterior (v2.4) fueron **resueltos** en v2.5.

### Inventario por Agregado

| Agregado | Entidades internas | Value Objects | Eventos | Comportamiento calculado |
|----------|-------------------|---------------|---------|-------------------------|
| OxpComercio | ConceptoDeGasto, PagoAplicado | InformacionTercero, MedioDePago, ValorMonetario, SoporteDocumental, DesgloseFiscal, Tributo, InstruccionDistribucion, DestinoDeNegocio | 13 | valorBruto(), totalImpuestos(), totalRetenciones(), valorNeto(), saldoPorPagar(), lineasParaTraduccion() |
| OxpExtracto | PartidaExtracto, CargoFinanciero, AjustePorDiferenciaCambio, AjustePorTolerancia, Vinculacion, CoberturaAnticipo, CoberturaDevolucion, CrucePagoExtractoAplicado | InformacionTercero, MedioDePago, ValorMonetario, SoporteDocumental, InstruccionDistribucion, DestinoDeNegocio | 20 | valorTotalExtracto(), saldoPorPagar(), lineasParaTraduccion() |
| Anticipo | CrucePagoAplicado, CruceRegularizacionAplicada | InformacionTercero, MedioDePago, ValorMonetario, SoporteDocumental, InstruccionDistribucion, DestinoDeNegocio | 10 | saldoPorPagar(), saldoPorRegularizar(), lineasParaTraduccion() |
| Devolucion | ConceptoDevuelto / CargoFinancieroDevuelto / ReversaTotal (polimórfico) | InformacionTercero, ValorMonetario, SoporteDocumental, DesgloseFiscal, Tributo, InstruccionDistribucion, DestinoDeNegocio (tipo Comercio) | 4 | valorBruto(), totalImpuestos(), totalRetenciones(), valorNeto(), lineasParaTraduccion() |

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima | Estado |
|---|-----------|-------------------------------|----------|-------------------|--------|
| C1 | Media | L298 `InstruccionDistribucion` en OxpExtracto, L453 en Anticipo, L584 en Devolucion — sin evento de configuración | OxpComercio tiene `DistribucionDeCostosConfigurada` (L1158) para configurar distribución explícitamente. OxpExtracto, Anticipo y Devolucion tipo Comercio también tienen `InstruccionDistribucion` pero no tienen evento equivalente. ¿Se configura durante radicación implícitamente? | Documentar cómo y cuándo se establece la distribución en cada agregado. Si es durante radicación, indicarlo en la "Información capturada" de cada evento de radicación. | Resuelto |
| C2 | Media | L1342-1366 DiferenciaEnCambioDetectada + ConceptoAjusteDiferenciaEnCambioGenerado | Cadena de 2 eventos para crear una sola entidad (`AjustePorDiferenciaCambio`). La detección y la generación del concepto ocurren en la misma operación (derivado por transición). Granularidad potencialmente excesiva. | Evaluar fusión en un solo evento o documentar explícitamente por qué la cadena de 2 eventos es necesaria (ej: la detección es un hecho observable y la generación es el efecto). | Resuelto |
| C3 | Media | L286 `AjustePorTolerancia` como entidad | `AjustePorTolerancia` es inmutable una vez creado, sin ciclo de vida propio — se crea durante vinculación y no cambia. Candidato a VO. | Evaluar si debe ser VO en lugar de entidad. Si se mantiene como entidad, documentar criterio de identidad. | Resuelto |
| C4 | Media | L87-93 mapeo de tipo `revertido` — sin mapeo equivalente para tipo `reversa` | La sección "Tipos de cruce: reversa vs revertido" incluye tabla de mapeo solo para tipo `revertido` (evento → entidad). El tipo `reversa` se explica en texto narrativo pero no tiene tabla equivalente. | Agregar tabla de mapeo para tipo `reversa`: qué evento crea cada cruce tipo reversa. | Resuelto |
| C5 | Baja | L282 `PartidaExtracto` "Descripción, valor, fecha, estado" | Criterio de identidad de `PartidaExtracto` no documentado. ¿Dos partidas con misma descripción, valor y fecha son distintas? ¿Identidad posicional o por ID asignado? | Documentar criterio de identidad (ej: índice posicional en el archivo del extracto o ID generado al importar). | Resuelto |
| C6 | Baja | L570-573 entidades polimórficas Devolucion | Las tres entidades comparten contrato común pero la restricción de cardinalidad varía: ConceptoDevuelto (1..N), CargoFinancieroDevuelto (1..N), ReversaTotal (exactamente 1). La restricción "exactamente 1" para ReversaTotal no tiene invariante formal (I##). | Considerar agregar I## para "Devolucion tipo Anticipo contiene exactamente 1 ReversaTotal". | Descartado |
| C7 | Baja | L116-133 diagrama de bounded context | El diagrama ASCII del BC muestra relaciones entre agregados pero `ServicioDeRegularizacion` aparece como texto suelto sin flechas claras de conexión. Los otros servicios tampoco están claramente conectados. | Mejorar legibilidad del diagrama ASCII para representar los flujos de los 3 domain services. | Resuelto |

### Resumen
- Alta: 0 | Media: 4 | Baja: 3
- Total: 7 hallazgos

---

## 3. Máquinas de Estado (FSM)

Verificación de estados, transiciones, estados terminales, cobertura de eventos y asimetrías entre diagramas.

### Conteo de estados

| Agregado | Estados documentados (L42-46) | Estados en FSM | Match |
|----------|-------------------------------|----------------|-------|
| OxpComercio | 5 | 5 (L917-955) | ✓ |
| OxpExtracto | 6 | 6 (L965-996) | ✓ |
| PartidaExtracto | 6 | 6 (L1005-1048) | ✓ |
| Anticipo | 5 | 5 (L1051-1097) | ✓ |
| Devolucion | 3 | 3 (L1098-1113) | ✓ |

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima | Estado |
|---|-----------|-------------------------------|----------|-------------------|--------|
| F1 | Media | L968-996 FSM OxpExtracto — estado "Parcialmente Conciliado" sin eventos de progreso | La FSM de OxpComercio (L928-947) muestra explícitamente los eventos de progreso dentro de cada estado (recuadros). La FSM de OxpExtracto no muestra los ~8 eventos de progreso que ocurren en "Parcialmente Conciliado" (VinculacionRealizada, PartidaCubiertaPorAnticipo, PartidaCubiertaPorDevolucion, PartidaEnDisputaMarcada, etc.). | Agregar recuadro de eventos de progreso en "Parcialmente Conciliado" para consistencia con el estilo de OxpComercio. | Descartado |
| F2 | Media | L988-990 FSM OxpExtracto — estado "Causado" sin eventos de progreso | Estado "Causado" no lista eventos de progreso en el diagrama. Las notas (L1001-1002) los mencionan en prosa pero el diagrama no los muestra. Debería listar: `PagoExtractoAplicado`, `PagoExtractoViaDevolucionAplicado`. | Agregar recuadro en "Causado" listando eventos de progreso (como se hace en OxpComercio Causada, L938-947). | Resuelto |
| F3 | Media | L979-985 FSM OxpExtracto — estado "Confirmada" | Confirmada lista `PagoExtractoViaDevolucionAplicado` como evento de progreso pero no muestra su evento compensatorio (`PagoExtractoViaDevolucionRevertido`). OxpComercio tampoco muestra compensatorios, pero la asimetría con los protocolos de proceso (que sí los documentan) puede confundir. | Agregar nota o referencia: "Eventos compensatorios: ver Sección 3, tablas de compensación." | Descartado |
| F4 | Media | L960 "pago directo (futuro)" | La nota de FSM OxpComercio dice "pago directo (futuro)" pero el evento `PagoOxpComercioDirectoAplicado` está completamente definido en el catálogo (L1590-1601) con descripción, precondiciones, payload y efectos. No es futuro. | Eliminar "(futuro)" de L960. | Resuelto |
| F5 | Baja | L1064 "(Regulariz.)" en diagrama FSM Anticipo | Abreviatura informal. Otros diagramas usan nombres completos o al menos descriptivos. | Expandir a "(RegularizacionDeAnticipoCompletada)" o "(Regularización completada)". | Resuelto |
| F6 | Baja | L998-999 vs L928-947 — asimetría de estilo | Las notas de FSM OxpExtracto describen eventos en prosa narrativa. Las notas de FSM OxpComercio los listan directamente dentro de recuadros en el diagrama ASCII. Asimetría de estilo entre diagramas. | Informativo — considerar unificar estilo. | **Resuelto** — Notas OxpExtracto reestructuradas estado-por-estado para simetría con OxpComercio. |

### Resumen
- Alta: 0 | Media: 4 | Baja: 2
- Total: 6 hallazgos

---

## 4. Invariantes

Clasificación (local/eventual/inter-agregado), enforcement documentado y coherencia con el modelo.

### Inventario

| I# | Tipo | Enforcement documentado | Alineación con modelo |
|----|------|------------------------|----------------------|
| I1 | Eventual | Sí ([SI4] — proyección con constraint) | ✓ |
| I2 | Local | Sí (validación en agregado) | ✓ |
| I3 | Local | Sí (conteo de partidas) | ✓ |
| I4a-d | Local | Sí (FSM por agregado) | ✓ |
| I5 | Local | Sí (agregado OxpComercio) | ✓ |
| I6 | Local (condicional) | Sí (configurable por empresa) | ⚠️ condicional |
| I7 | Inter-agregado | Parcial — sin mecanismo explícito | ⚠️ |
| I8 | Local | Sí (precondiciones en Anticipo) | ✓ |
| I9 | Local | Sí (confirmación externa SincoA&F) | ✓ |
| I10 | Local | Sí (cadena de resolución D7) | ✓ |
| I11 | Local | Sí (validación en agregado) | ✓ |
| I12 | Local | Sí (estados + saldos) | ✓ |
| I13 | Local | Sí (validación en agregado) | ✓ |
| I14 | Local | Sí (validación en agregado) | ✓ |
| I15 | Local | Sí (estados + saldos) | ✓ |
| I16 | Local | Sí (FSM + precondiciones) | ✓ |
| I17 | Inter-agregado | Parcial — acumulado sin mecanismo | ⚠️ |

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima | Estado |
|---|-----------|-------------------------------|----------|-------------------|--------|
| IV1 | Media | L1861 I3 "partidas vinculadas [...] clasificadas como cargos adicionales" | I3 mezcla `PartidaExtracto` con `CargoFinanciero` en la condición de completitud. Los `CargoFinanciero` no son partidas — son entidades separadas que se consideran conciliadas automáticamente. | Clarificar I3: "100% de `PartidaExtracto` resueltas. Los `CargoFinanciero` se consideran conciliados automáticamente y no participan en el conteo de completitud." | Resuelto |
| IV2 | Media | L1868 I7 marcado "Inter-agregado" sin mecanismo de enforcement | I7 dice que una OxpComercio solo puede vincularse a un único OxpExtracto. Cruza fronteras de agregado pero no indica cómo se enforcea (¿proyección como [SI4]? ¿validación en ServicioDeConciliacion?). | Agregar mecanismo: "Enforcement: validación en `ServicioDeConciliacion` (precondición de vinculación) + proyección eventual para detección tardía." | Resuelto |
| IV3 | Media | L1877 I16 "Con un futuro sistema de Tesorería independiente..." | I16 contiene una nota prospectiva sobre un sistema futuro que cambiará la invariante. Esto es un pendiente disfrazado de nota dentro de una invariante que debería ser "absoluta" (L1855). | Mover nota sobre Tesorería a Sección 11 como PD3: "Redefinición de I16 con sistema de Tesorería independiente." Mantener I16 como regla actual sin condiciones futuras. | Resuelto |
| IV4 | Media | L1878 I17 acumulado sin mecanismo de enforcement | I17 dice "la suma de todas las devoluciones sobre una misma OxpComercio no puede superar el `valorNeto()` original". Requiere consultar todas las devoluciones previas — no validable por un solo agregado. No indica mecanismo. | Agregar mecanismo: "Enforcement: validación en `ServicioDeAplicacionDevolucion` (precondición con lectura de acumulado) + proyección eventual de suma de devoluciones por OxpComercio." | Resuelto |
| IV5 | Baja | L1855 "las invariantes son absolutas" vs L1867 I6 "aplica como invariante solo cuando está habilitada" | Invariante condicional contradice la definición de "absolutas". I6 es configurable por empresa. | Reclasificar I6 como regla de negocio configurable, o agregar excepción explícita en la definición: "excepto I6, que es configurable." | **Resuelto** — Excepción explícita agregada en definición de invariantes. |
| IV6 | Baja | L1873 I12 — extensión | I12 documenta 5 estados del Anticipo con condiciones de saldo de forma exhaustiva. Correcta pero verbosa. | Informativo — considerar reformular como tabla (estado × saldoPorPagar × saldoPorRegularizar). | **Resuelto** — I12 reformulada como tabla estado × saldos. |

### Resumen
- Alta: 0 | Media: 4 | Baja: 2
- Total: 6 hallazgos

---

## 5. Responsabilidades de Agregados

Evaluación del diseño comportamental: agregados anémicos, inflados, lógica fugada, SRP, fronteras.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima | Estado |
|---|-----------|-------------------------------|----------|-------------------|--------|
| RS1 | Media | OxpExtracto: 21 eventos, 8 entidades internas, sub-FSM de PartidaExtracto | OxpExtracto es el agregado más complejo: concentra conciliación, disputas, ajustes, pagos, coberturas. Todos los componentes están relacionados con el ciclo del extracto pero la complejidad es alta. | Informativo con monitoreo — documentar umbral: si supera 25 eventos, evaluar extracción de sub-dominio (ej: disputas como sub-agregado o módulo). | Descartado |
| RS2 | Media | L699-731 ServicioDeConciliacion con flujo principal + flujo de partidas de retorno | El servicio coordina vinculación + pago + ajustes (tolerancia, diferencia cambio) en el flujo principal, Y además tiene un flujo separado de partidas de retorno (L711-719). Son responsabilidades distintas dentro del mismo servicio. | Evaluar si el flujo de partidas de retorno debería ser un servicio separado (ej: `ServicioDeCobertura`), o documentar por qué debe permanecer en ServicioDeConciliacion. | Resuelto |
| RS3 | Media | L770-842 ServicioDeAplicacionDevolucion — 3 ramas + compensación | El servicio tiene 3 ramas diferenciadas por tipo de OXP, cada una con su propia tabla de compensación. [SI2] ya sugiere Strategy pattern. | [SI2] cubre la sugerencia. Complementar: cada Strategy debería encapsular su propia tabla de compensación. | Resuelto |
| RS4 | Baja | L1212-1222 AnticipoAmortizado — evento sin cambio de estado | El evento registra confirmación externa de SincoA&F sin comportamiento interno del agregado. Es un dato de trazabilidad. | Informativo — correcto para trazabilidad contable. | **Descartado** — Informativo, diseño confirmado como válido. |
| RS5 | Baja | L1288-1298 AlertaPlazoAnticipoVencido, L1456-1466 AlertaConciliacionPlazoVencido | Alertas como eventos del agregado sin consumidores documentados. ¿Quién recibe y procesa las notificaciones? | Documentar consumidores esperados (ej: read model de alertas, notificaciones push, email). | **Resuelto** — Documentados como eventos de dominio con consumidor (read model de alertas/panel de trabajo) y resolución implícita. |
| RS6 | Baja | L510-516 Devolucion: 4 eventos, FSM lineal de 3 estados | Devolucion es el agregado más liviano. Podría argumentarse que no justifica un agregado independiente. | Informativo — D12 provee la justificación (comportamiento financiero fundamentalmente distinto). | **Descartado** — Informativo, D12 justifica el diseño. |

### Resumen
- Alta: 0 | Media: 3 | Baja: 3
- Total: 6 hallazgos

---

## 6. Semántica de Eventos

Validación de naming, granularidad, payloads, emisor único y clasificación.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima | Estado |
|---|-----------|-------------------------------|----------|-------------------|--------|
| ES1 | Media | L1342-1366 DiferenciaEnCambioDetectada + ConceptoAjusteDiferenciaEnCambioGenerado | Cadena de 2 eventos derivados por transición para un solo hecho atómico: la diferencia se detecta Y se genera el ajuste en la misma operación. ¿Son realmente 2 hechos de dominio distintos? | Evaluar fusión en un solo evento (ej: `AjustePorDiferenciaEnCambioRegistrado`). Si se mantienen separados, documentar por qué la detección y la generación son hechos de negocio independientes. | Resuelto |
| ES2 | Media | L1224-1235 `AnticipoVinculadoAPartida` | El nombre sugiere vinculación (relación) pero el efecto principal es financiero: crea `CrucePagoAplicado` tipo extracto y reduce `saldoPorPagar()`. Inconsistente con el patrón de naming `PagoXxxViaYyyAplicado` usado para operaciones financieras equivalentes en OxpComercio. | Considerar renombrar a `PagoAnticipoViaExtractoAplicado` para consistencia con el patrón. O documentar por qué el nombre actual es preferible (ej: enfatiza la vinculación con la partida, no el efecto financiero). | Descartado |
| ES3 | Media | L1304-1314 ConciliacionIniciada — `correlationId` ausente en payload | D20 establece correlationId como garantía transversal. El protocolo de proceso (L730-731) dice que correlationId va incluido en VinculacionRealizada y PagoOxpComercioViaExtractoAplicado. Pero ConciliacionIniciada no lo menciona en "Información capturada". | Clarificar: si ConciliacionIniciada precede al proceso de conciliación y no requiere correlationId (el proceso empieza con la primera vinculación), documentarlo. Si sí debe tenerlo, agregarlo al payload. | Descartado |
| ES4 | Media | L1653-1664 PagoOxpComercioViaDevolucionAplicado — payload | "Información capturada" dice "Referencia a Devolucion, monto cubierto por devolución, fecha" pero no lista `devolucionId` como campo explícito. Otros eventos similares (ej: PagoOxpComercioViaExtractoAplicado L1561) listan "Referencia a OxpExtracto, partida del extracto vinculada, tipo de vinculación, valor cubierto" — más detallados. | Detallar el payload: listar explícitamente `devolucionId`, `monto cubierto`, `fecha` como campos separados. | Descartado |
| ES5 | Baja | L38 convención naming: "PascalCase en español" sin mención de tildes | El modelo usa consistentemente PascalCase sin tildes (OxpComercioRadicada, DevolucionConfirmada, etc.) pero la convención no lo explicita. | Agregar en L38: "PascalCase en español, sin tildes ni caracteres especiales (compatibilidad con código fuente)." | Resuelto |
| ES6 | Baja | L1420 `PartidaEnDisputaMarcada` vs L1432 `PartidaDisputaDescartada` y L1444 `PartidaDisputaReclasificada` | Inconsistencia de naming: el primer evento usa "En" (`PartidaEnDisputaMarcada`) pero los siguientes no (`PartidaDisputaDescartada`, `PartidaDisputaReclasificada`). | Unificar: `PartidaDisputaMarcada` (sin "En") o `PartidaEnDisputaDescartada` / `PartidaEnDisputaReclasificada`. | **Resuelto** — Unificado con "En": `PartidaEnDisputaDescartada`, `PartidaEnDisputaReclasificada`. |
| ES7 | Baja | L1288-1298 AlertaPlazoAnticipoVencido, L1456-1466 AlertaConciliacionPlazoVencido | Las alertas son eventos informativos sin cambio de estado. ¿Son eventos de dominio (pertenecen al event stream del agregado) o señales de integración (se emiten vía outbox pero no se persisten en el stream)? | Documentar clasificación explícitamente: evento de dominio (replay-safe, se persiste en stream) o señal de integración (fire-and-forget). | **Resuelto** — Clasificados explícitamente como eventos de dominio con resolución implícita. |

### Resumen
- Alta: 0 | Media: 4 | Baja: 3
- Total: 7 hallazgos

---

## 7. Idempotencia y Concurrencia

Evaluación de mecanismos de deduplicación, concurrencia optimista y replay safety.

**Nota:** Los hallazgos ID1-ID6 de la auditoría anterior (v2.4) — expectedVersion y idempotencyKey faltantes en los 4 agregados y 3 services — fueron **resueltos** en v2.5 con D20 (delegación a Marten + Wolverine).

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima | Estado |
|---|-----------|-------------------------------|----------|-------------------|--------|
| ID1 | Media | L1922 D20 "garantías transversales de la plataforma de persistencia y mensajería" | D20 delega expectedVersion, idempotencyKey y correlationId completamente a la plataforma (Marten + Wolverine). El modelo de dominio queda acoplado a la plataforma — si se migra, estas garantías deben revalidarse. | Agregar nota a D20: "Principio: estas garantías deben existir independiente de la plataforma. Si la plataforma cambia, revalidar que el nuevo stack las provea." | Resuelto |
| ID2 | Media | L755 "control de concurrencia `[D20]`" en escenario 1:N de ServicioDeRegularizacion | El escenario 1:N (múltiples OxpComercio regularizan el mismo Anticipo simultáneamente) depende de expectedVersion para evitar overbooking. Pero no especifica que la colisión ocurre en el stream del Anticipo (no del OxpComercio). | Documentar: "La concurrencia optimista se valida contra el stream del Anticipo (`expectedVersion` del Anticipo). La segunda ejecución concurrente falla con conflicto de versión y reintenta con el saldo actualizado." | Resuelto |
| ID3 | Media | L726-727 ServicioDeConciliacion — fallo de compensación | La tabla de compensación documenta qué pasa si paso 5 falla (compensar paso 4). Pero no documenta qué pasa si la compensación misma falla (VinculacionRevertida falla al escribirse). | Documentar política de fallo de compensación: ¿reintentar compensación? ¿dead letter? ¿intervención manual? Aplicable a los 3 domain services. | Resuelto |
| ID4 | Baja | L1150-1156 CargosAdicionalesExtraidos "co-emisión atómica con ExtractoRadicado (mismo append)" | Replay safe — ambos eventos se persisten en el mismo append atómico. | Informativo — diseño correcto. | **Descartado** — Informativo, diseño confirmado. |
| ID5 | Baja | 6 eventos compensatorios nuevos en v2.5 | Los 6 eventos compensatorios referencian `correlationId` del proceso fallido para identificar exactamente qué cruce revertir. Diseño correcto: la compensación revierte un hecho específico, no "el último". | Informativo — diseño correcto de compensación selectiva. | **Descartado** — Informativo, diseño confirmado. |

### Resumen
- Alta: 0 | Media: 3 | Baja: 2
- Total: 5 hallazgos

---

## 8. Sagas y Procesos Multi-Agregado

Validación de domain services: triggers, compensación, correlación, persistencia, ventanas de inconsistencia.

**Nota:** Los hallazgos S1-S3 de la auditoría anterior (v2.4) — compensación, correlationId e idempotencyKey faltantes en los 3 services — fueron **resueltos** en v2.5 con protocolos de proceso documentados y D20.

### Inventario de procesos

| Domain Service | Agregados | Pasos | Compensación | correlationId | Persistencia |
|---------------|-----------|-------|-------------|---------------|-------------|
| ServicioDeConciliacion | OxpExtracto, OxpComercio | 2 | Sí (L722-727) | Sí (L730) | Sí (L731) |
| ServicioDeRegularizacion | Anticipo, OxpComercio | 2 | Sí (L757-762) | Sí (L765) | Sí (L766) |
| ServicioDeAplicacionDevolucion | Devolucion + 1-2 agregados según rama | 2-3 por rama | Sí (L827-838) | Sí (L841) | Sí (L842) |

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima | Estado |
|---|-----------|-------------------------------|----------|-------------------|--------|
| SG1 | Alta | L835-836 Rama Comercio-C, paso 8cc "Fallo permanente improbable (stream nuevo)" | Rama C tiene 2 pasos de efecto: 7cc (`PagoOxpComercioViaDevolucionAplicado`) y 8cc (crear Anticipo por excedente). Si 8cc falla después de 7cc exitoso, la tabla dice compensar 7cc + DevolucionRevertida. Pero no documenta qué pasa con el stream del Anticipo si se creó parcialmente (stream huérfano). La asunción "improbable" no elimina la necesidad de documentar la política. | Documentar compensación completa: (1) si Anticipo no se creó → PagoOxpComercioViaDevolucionRevertido + DevolucionRevertida. (2) Si stream se creó parcialmente → política de limpieza (ej: marcar como fallido, no procesar). | Resuelto |
| SG2 | Media | L737 ServicioDeRegularizacion trigger | El trigger dice "El usuario selecciona..." pero no indica el rol del usuario ni la interfaz. ¿Es el Radicador? ¿El Confirmador? | Documentar rol del usuario que puede ejecutar la regularización (ej: Radicador o rol específico). | Descartado |
| SG3 | Media | L731 "Stream propio `conciliacion-{correlationId}`" | Los streams propios de proceso (conciliacion-{id}, regularizacion-{id}, aplicacion-devolucion-{id}) se documentan en protocolo de proceso pero no se especifica qué eventos se escriben en ellos. ¿Metadata del proceso? ¿Eventos de dominio duplicados? | Documentar contenido del stream de proceso: metadata (pasos completados, timestamps, estado del proceso, referencias a streams afectados). Aclarar que no duplica eventos de dominio. | Resuelto |
| SG4 | Media | L711-719 ServicioDeConciliacion flujo de partidas de retorno — sin tabla de compensación | El flujo principal (L703-709) tiene tabla de compensación (L722-727). El flujo de partidas de retorno (L711-719) emite `PartidaCubiertaPorDevolucion` en OxpExtracto pero no tiene tabla de compensación. ¿Qué pasa si falla? | Documentar compensación para flujo de retorno, o justificar: "Operación de un solo paso sobre un solo agregado — no requiere compensación inter-agregado." | Resuelto |
| SG5 | Media | L845-846 "Pendientes: Reembolso / Anticipo A2" dentro de ServicioDeAplicacionDevolucion | Los pendientes están documentados inline en la sección del servicio Y en PD1 de Sección 11. Duplicación sin referencia cruzada. | Reemplazar las líneas ⚠️ con "Ver `[PD1]` en Sección 11." | Resuelto |
| SG6 | Baja | L859-867 [SI3] Wolverine Saga | La sugerencia de implementación mapea correctamente servicios a sagas de Wolverine con tabla de correspondencia. | Informativo — bien documentado. | **Descartado** — Informativo, diseño confirmado. |
| SG7 | Baja | L834 Rama Comercio-B "Fallo permanente improbable (stream nuevo, sin conflicto de precondiciones)" | Misma asunción de improbabilidad que en SG1 pero para un caso más simple (solo crear Anticipo, sin paso previo de pago). Aunque improbable, la política debería documentarse. | Agregar nota: "En caso improbable de fallo permanente → DevolucionRevertida → stream Devolucion." | Resuelto |

### Resumen
- Alta: 1 | Media: 4 | Baja: 2
- Total: 7 hallazgos

---

## 9. Decisiones Abiertas

Inventario de pendientes sin resolver, decisiones implícitas, TODOs sin ownership y pendientes de changelog no cerrados.

### Inventario de Pendientes

| # | Ubicación (L~N) | Texto literal | Tipo | Decisión temporal | Riesgo | Criterio de cierre |
|---|-----------------|--------------|------|-------------------|--------|-------------------|
| 1 | L1942 PD1 | "Reembolso de anticipo — integración con CXC" | Pendiente formal | No | Medio | Implementar BC CXC |
| 2 | L1943 PD2 | "Cruce tipo `reversa` (negocio) para OxpComercio y OxpExtracto" | Pendiente formal | No | Bajo | Escenario real identificado por negocio |
| 3 | L549 ⚠️ | "Pendiente: reembolso cuando no existe OxpComercio futura" | Pendiente inline | No | Medio | = PD1 (duplicado) |
| 4 | L565 A2 | "Diferido — requiere dominio CXC" | Diferido | No | Medio | = PD1 (duplicado) |
| 5 | L845-846 ⚠️ | "Pendientes: Reembolso / Anticipo A2" | Pendiente inline | No | Medio | = PD1 (duplicado) |
| 6 | L960 | "pago directo (futuro)" | Texto futuro | Evento ya definido | Bajo | Eliminar "(futuro)" |
| 7 | L1877 I16 | "Con un futuro sistema de Tesorería independiente..." | Decisión implícita | I16 actual | Medio | Implementar Tesorería |

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima | Estado |
|---|-----------|-------------------------------|----------|-------------------|--------|
| OD1 | Media | L549, L565, L845-846 duplican PD1 (L1942) | El mismo pendiente (reembolso anticipo / CXC) aparece en 4 ubicaciones sin referencia cruzada. Deuda documental que dificulta el tracking. | Consolidar: dejar PD1 como fuente de verdad. En L549, L565 y L845-846 reemplazar ⚠️ con "Ver `[PD1]`." | Resuelto |
| OD2 | Media | L1877 I16 nota "Con un futuro sistema de Tesorería..." | Pendiente implícito dentro de una invariante. I16 es regla actual pero contiene una nota prospectiva que debería ser un pendiente formal. | Crear PD3: "Redefinición de I16 cuando se implemente sistema de Tesorería independiente — pagos externos podrían desacoplarse de la causación contable." Referenciar [PD3] en I16. | Resuelto |
| OD3 | Media | L960 "pago directo (futuro)" vs L1590-1601 PagoOxpComercioDirectoAplicado completamente definido | El texto "(futuro)" contradice el catálogo donde el evento está plenamente especificado con descripción, precondiciones, payload y efectos. | Eliminar "(futuro)" de L960. | Resuelto |
| OD4 | Baja | L1943 PD2 sin prioridad ni ownership | PD2 es un pendiente formal pero no indica quién lo resolvería ni cuándo evaluarlo. | Agregar: "Prioridad: baja. Ownership: equipo de negocio. Se activa cuando surja un escenario real." | **Descartado** — Trigger ya documentado, asimetría con PD1/PD3. |
| OD5 | Baja | Changelog v2.3 (L1964) pendiente #1 "momento de regularización" y pendiente #3 "lineasParaTraduccion() Devolucion" | Ambos pendientes fueron resueltos (v2.5 y v2.4 respectivamente) pero el changelog de v2.5 no menciona explícitamente su cierre con referencia a la versión donde se originaron. | Agregar en changelog v2.5: "Resueltos: pendiente v2.3 #1 (momento de regularización, ahora Confirmada o posterior) y pendiente v2.3 #3 (lineasParaTraduccion Devolucion, resuelto en v2.4)." | **Descartado** — Se resolverá al crear el changelog completo. |

### Resumen
- Pendientes: 2 formales (PD1, PD2) + 3 duplicados + 1 texto contradictorio + 1 implícito
- Alta: 0 | Media: 3 | Baja: 2
- Total: 5 hallazgos

---

## 10. Sanity Check (Coherencia Cruzada)

Meta-auditoría de coherencia entre secciones: contradicciones, referencias rotas, conteos inconsistentes, decisiones desalineadas.

### Coherencia Cruzada

**Referencias verificadas:**
- `[R##]`: todas las referencias a reglas de negocio apuntan a `definicion-alcance.md`, Sección 6. 0 rotas.
- `[P##]`: P1 definida en Sección 10, referenciada en Anticipo y I2. 0 rotas.
- `[I##]`: I1-I17 definidas en Sección 7, referenciadas consistentemente. 0 rotas.
- `[D##]`: D1-D20 definidas en Sección 9, referenciadas consistentemente. 0 rotas.
- `[SI##]`: SI1-SI4 definidas en Sección 3, referenciadas en catálogo y convenciones. 0 rotas.
- `[PD##]`: PD1-PD2 definidas en Sección 11. 0 rotas (pero duplicadas inline — ver OD1).

**Conteos verificados:**

| Conteo declarado | Valor | Conteo real | Match |
|-----------------|-------|-------------|-------|
| OxpComercio eventos (L142) | 13 | 13 | ✓ |
| OxpExtracto eventos (L276) | 20 | 20 | ✓ |
| Anticipo eventos (L410) | 10 | 10 | ✓ |
| Devolucion eventos (L516) | 4 | 4 | ✓ |
| Total eventos | 47 | 47 | ✓ |
| Invariantes | 17 | 17 (I1-I17) | ✓ |
| Decisiones | 20 | 20 (D1-D20) | ✓ |
| Premisas | 1 | 1 (P1) | ✓ |
| Pendientes | 2 | 2 (PD1-PD2) | ✓ |

**Decisiones vigentes:** 20 decisiones verificadas — todas alineadas con el modelo actual.

**Premisas operacionalizadas:** P1 (anticipo sin desglose fiscal) reflejada en composición del Anticipo y en I2.

### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima | Estado |
|---|-----------|-------------------------------|----------|-------------------|--------|
| SC1 | Media | L45 convenciones "Confirmada" (femenino) vs L989 FSM "Causado" (masculino) vs L994 FSM "Pagado" (masculino) | OxpExtracto mezcla géneros en sus estados: Confirmada (femenino, unificado en v2.5 changelog) pero Causado y Pagado siguen en masculino. Los demás agregados usan femenino consistentemente (Confirmada, Causada, Pagada). | Completar la unificación de género: cambiar "Causado" → "Causada" y "Pagado" → "Pagada" en OxpExtracto. O documentar que OxpExtracto usa masculino porque "extracto" es masculino. | Resuelto |
| SC2 | Media | L549, L565, L845-846, L1942 — misma información en 4 ubicaciones | Deuda documental: pendiente PD1 duplicado en 4 ubicaciones sin referencias cruzadas. Ya reportado en OD1 pero afecta coherencia cruzada. | Consolidar en PD1 con "Ver `[PD1]`" en las demás ubicaciones. | Resuelto |
| SC3 | Media | L960 "(futuro)" vs L1590-1601 catálogo completo | Contradicción entre FSM (marca como futuro) y catálogo (completamente especificado). Ya reportado en F4/OD3 pero es una contradicción cruzada entre secciones. | Eliminar "(futuro)". | Resuelto |
| SC4 | Baja | L1064 "(Regulariz.)" vs estilo de otros diagramas | Inconsistencia de formato entre diagramas FSM. | Expandir abreviatura. | Resuelto |
| SC5 | Baja | Changelog v2.5 (L1966) vs pendientes de v2.3 (L1964) | El changelog documenta exhaustivamente los cambios pero no cierra explícitamente los pendientes originados en versiones anteriores. | Agregar cierres explícitos de pendientes resueltos referenciando la versión de origen. | **Descartado** — Se resolverá al crear el changelog completo. |

### Resumen
- **Referencias verificadas:** 0 rotas
- **Conteos verificados:** 9/9 correctos
- **Decisiones vigentes:** 20 alineadas
- **Premisas operacionalizadas:** 1/1 reflejada
- Alta: 0 | Media: 3 | Baja: 2
- Total: 5 hallazgos

---

## Resumen Ejecutivo

| Skill | Alta | Media | Baja | Total |
|-------|------|-------|------|-------|
| Glosario | 0 | 4 | 2 | 6 |
| Composición | 0 | 4 | 3 | 7 |
| FSM | 0 | 4 | 2 | 6 |
| Invariantes | 0 | 4 | 2 | 6 |
| Responsabilidades | 0 | 3 | 3 | 6 |
| Semántica Eventos | 0 | 4 | 3 | 7 |
| Idempotencia | 0 | 3 | 2 | 5 |
| Sagas | 1 | 4 | 2 | 7 |
| Decisiones Abiertas | 0 | 3 | 2 | 5 |
| Sanity Check | 0 | 3 | 2 | 5 |
| **TOTAL** | **1** | **36** | **23** | **60** |

### Estado de resolución

| Skill | Resuelto | Descartado | Total |
|-------|----------|------------|-------|
| Glosario | 6 | 0 | 6 |
| Composición | 6 | 1 | 7 |
| FSM | 4 | 2 | 6 |
| Invariantes | 6 | 0 | 6 |
| Responsabilidades | 3 | 3 | 6 |
| Semántica Eventos | 4 | 3 | 7 |
| Idempotencia | 3 | 2 | 5 |
| Sagas | 5 | 2 | 7 |
| Decisiones Abiertas | 3 | 2 | 5 |
| Sanity Check | 4 | 1 | 5 |
| **TOTAL** | **44** | **16** | **60** |

### Comparación con auditoría anterior (v2.4)

| Métrica | v2.4 | v2.5 | Cambio |
|---------|------|------|--------|
| Total hallazgos | 88 | 60 | -32% |
| Severidad Alta | 18 | 1 | -94% |
| Severidad Media | 42 | 36 | -14% |
| Severidad Baja | 28 | 23 | -18% |

La Fase 1 de correcciones (v2.5) resolvió los 18 hallazgos de severidad Alta de la auditoría anterior. Las Fases 2 y 3 resolvieron los 60 hallazgos de la auditoría v2.5: 44 resueltos con correcciones aplicadas al modelo, 16 descartados (informativos confirmados o diferidos al changelog).

### Top 5 Hallazgos Críticos

| # | Skill origen | Severidad | Problema | Corrección mínima | Estado |
|---|-------------|-----------|----------|-------------------|--------|
| 1 | Sagas (SG1) | **Alta** | ServicioDeAplicacionDevolucion Rama Comercio-C: si crear Anticipo por excedente (paso 8cc) falla después de PagoOxpComercioViaDevolucionAplicado (paso 7cc), la compensación no documenta qué pasa con un stream de Anticipo creado parcialmente (stream huérfano). | Documentar compensación completa para Rama C incluyendo política de streams huérfanos. | Resuelto |
| 2 | FSM (F4) + Decisiones (OD3) + Sanity (SC3) | Media | "(futuro)" en FSM OxpComercio (L960) para pago directo contradice el catálogo donde el evento `PagoOxpComercioDirectoAplicado` está completamente especificado. Aparece en 3 skills como hallazgo. | Eliminar "(futuro)" de L960. Corrección mínima, impacto cruzado en 3 skills. | Resuelto |
| 3 | Invariantes (IV2, IV4) + Composición (C1) | Media | Enforcement no documentado: I7 (vinculación única OxpComercio→OxpExtracto) e I17 (acumulado de devoluciones) son inter-agregado sin mecanismo explícito. InstruccionDistribucion en 3 agregados sin evento de configuración. | Documentar mecanismos de enforcement y eventos de configuración de distribución. | Resuelto |
| 4 | Sanity (SC1-SC2) + Decisiones (OD1) | Media | Género mixto en estados OxpExtracto (Confirmada vs Causado/Pagado) y pendiente PD1 duplicado en 4 ubicaciones sin cross-reference. Dos problemas de coherencia documental. | Unificar género de estados OxpExtracto. Consolidar PD1 con "Ver [PD1]" en duplicados. | Resuelto |
| 5 | Idempotencia (ID3) + Sagas (SG4) | Media | Fallo de compensación no documentado: ¿qué pasa si la compensación misma falla? Y flujo de partidas de retorno en ServicioDeConciliacion sin tabla de compensación. | Documentar política de fallo de compensación (dead letter, intervención manual) y compensación para flujo de retorno. | Resuelto |
