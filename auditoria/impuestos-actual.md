# Audit Full — Reporte de Auditoría Completa

**Fecha:** 2026-05-13
**Modelo auditado:** `dominio/impuestos/modelo-dominio.md` (post Cambios 1-4 hacia v2.0)

---

## 1. Glosario y Lenguaje Ubicuo (audit-structure-glossary)

### Hallazgos

| # | Sev | Evidencia | Problema | Corrección mínima |
|---|-----|-----------|----------|-------------------|
| 1 | Alta | L1525 "**45 eventos** … 10 agregados" vs tabla L1553 "**Total** 56" | Contradicción interna del catálogo de eventos. | Reemplazar texto a "56 eventos en 12 agregados". |
| 2 | Media | L2057 control de versiones "10 agregados, 45 eventos" | Conteos desactualizados tras los Cambios 2 y 3. | Actualizar a "12 agregados [F1] + 4 [F2] = 16; 56 eventos". |
| 3 | Media | L376 "EntidadEvaluada (emisora/contraparte/sedeEmisora.jurisdiccion/…)" | "entidad" significa rol fiscal Y también jurisdicción — lenguaje ubicuo roto. | Renombrar a `objetoEvaluado` o aclarar nota explícita en 3.4. |
| 4 | Media | L444 ejemplo "atributoEvaluado: tipoTransaccion" | `tipoTransaccion` no es atributo de perfil ni de jurisdicción → viola `[I15]`. | Eliminar ejemplo o extender `[I15]` con tercera fuente formal. |
| 5 | Media | L376 + L444 | `[I15]` no contempla atributos del contexto transaccional. | Hacer `[I15]` exhaustiva con todas las fuentes válidas. |
| 6 | Media | L788 + L841 "municipioRef" usado para departamento `05` y provincia Colón | El nombre `municipioRef` es engañoso (apunta a cualquier nivel jurisdiccional). | Renombrar a `jurisdiccionRef`. |
| 7 | Baja | L1094 "homologacion: ref" vs L1153 `ReferenciaHomologacion` (VO) | Misma relación con dos nombres. | Uniformar a `ReferenciaHomologacion`. |
| 8 | Baja | L454 "alternativa: …" vs L376 "tarifaAlternativa" | Inconsistencia diagrama/texto. | Uniformar a `tarifaAlternativa`. |
| 9 | Baja | L168 "direccionFiscalAplicable (gasto/ingreso/ambas)" | `ambas` se documenta como dirección — confunde. | Aclarar como "comodín de aplicabilidad", no valor de dirección. |
| 10 | Baja | L884 "LineaDesgloseMotor … estructura idéntica" | Sinónimo no controlado. | Unificar con atributo `proposito: confirmado | referencia | descartada`. |

**Resumen:** Alta 1 | Media 5 | Baja 4 | Total 10

---

## 2. Composición de Agregados (audit-structure-composition)

### Hallazgos

| # | Sev | Evidencia | Problema | Corrección mínima |
|---|-----|-----------|----------|-------------------|
| 1 | Alta | L444 "atributoEvaluado: tipoTransaccion" | Atributo referenciado pero no declarado en composición ni en `[I15]`. | Eliminar ejemplo o formalizar tercera fuente de atributos evaluables. |
| 2 | Alta | L1317 vs L1322-1336 diagrama dependencias | El diagrama omite `JurisdiccionFiscal` y `CatalogoDeRegimenesEspeciales` que el texto sí incluye. | Actualizar el diagrama ASCII con las 7 dependencias. |
| 3 | Media | L1090 "tipoEntregable" sin declaración | Atributo del agregado raíz `FormatoFiscal` no documentado en la composición. | Declarar formalmente `tipoEntregable` como atributo del raíz. |
| 4 | Media | L376 identidad de `Condicion` | No declarada — eventos `*Modificada`/`*Cerrada` referencian id no definido. | Documentar tupla de identidad o id sintético explícito. |
| 5 | Media | L676 `regimenesEspecialesVigentes()` consulta cross-aggregate | Comportamiento del agregado lee otro agregado sin documentar la dependencia. | Documentar dependencia de lectura a `CatalogoDeAtributosFiscales`. |
| 6 | Media | L884 `LineaDesgloseMotor` duplica `LineaDeDesglose` | Dos entidades con misma estructura distintas solo por propósito. | Unificar como una entidad con `proposito`. |
| 7 | Media | L884-885 vs L1668 `LineaDescartada` solo en gravámenes | Restricción condicional implícita. | Documentar la condición en la composición. |
| 8 | Baja | L1233 vs L1203 estado `Fallido` omitido en header | Inconsistencia presentación FSM. | Incluir `Fallido` en la cabecera del agregado. |
| 9 | Baja | L1094 campo `homologacion` solo en diagrama | Atributo del raíz no formalizado. | Documentar como VO `ReferenciaHomologacion`. |
| 10 | Baja | L985 factory `crear(...)` con `calculoDeReferencia` | Input del factory no se traza al payload del evento. | Aclarar que se descompone en `LineaDesgloseMotor[]` o se descarta. |

**Resumen:** Alta 2 | Media 5 | Baja 3 | Total 10

---

## 3. Máquinas de Estado FSM (audit-structure-state-machines)

### Hallazgos

| # | Sev | Evidencia | Problema | Corrección mínima |
|---|-----|-----------|----------|-------------------|
| 1 | Media | L1499-1509 FSM Certificado + L1517 | `Fallido` no es terminal pero las transiciones de salida no están todas documentadas. | Documentar todas las salidas (Reenviado, ¿Regenerado?). |
| 2 | Media | L1517 "corregir datos del destinatario antes de reintentar" | No hay evento que capture la corrección. | Documentar dónde se persiste la corrección. |
| 3 | Media | L1483 vs L1183 `puedeGenerarContenido()` | Comportamiento engañoso (no distingue Generar vs Regenerar). | Separar en `puedeGenerar()` / `puedeRegenerar()`. |
| 4 | Baja | L1499-1509 diagrama Certificado | No queda claro si Fallido → Regenerado existe. | Aclarar en el diagrama. |
| 5 | Baja | L1472 "10 agregados de configuración" | Conteo desactualizado. | Recontar (9 + RegistroTributario). |
| 6 | Baja | L1492 + L1518 regla "estado terminal → nuevo stream" | Regla no formalizada como invariante. | Reforzar `[I11a]`/`[I11b]` con la cláusula. |
| 7 | Baja | L1499 diagrama Certificado | Lector podría inferir regeneración desde Borrador. | Agregar nota "Borrador no admite Regenerado". |

**Resumen:** Alta 0 | Media 3 | Baja 4 | Total 7

---

## 4. Invariantes (audit-structure-invariants)

### Hallazgos

| # | Sev | Evidencia | Problema | Corrección mínima |
|---|-----|-----------|----------|-------------------|
| 1 | Alta | L1820 `[I15]` + L444 ejemplo `tipoTransaccion` | Invariante no cubre el ejemplo del modelo. | Eliminar ejemplo o ampliar `[I15]`. |
| 2 | Alta | L1818 `[I13]` + L1301 | Sin compensación si jurisdicción se cierra entre simulación y confirmación. | Snapshot de jurisdicción en `ContextoTransaccional` o política explícita de rechazo. |
| 3 | Media | L1806 `[I2]` padre activo | Enforcement no especificado (cascade vs guard). | Documentar precondición de `TributoDesactivado` y/o cascade. |
| 4 | Media | L1807 `[I3]` unicidad de tratamiento | Enforcement no documentado más allá de la precedencia. | Documentar guard explícito al definir tratamiento. |
| 5 | Media | Implícita-3 identidad de `Condicion` | Sin invariante formal sobre la tupla de identidad. Bloquea implementación. | Formalizar tupla de identidad como invariante. |
| 6 | Media | Implícita-4 `tarifaAlternativa` válida | Sin invariante que garantice referencia vigente. | Agregar invariante eventual con motivo de rechazo. |
| 7 | Media | L1820 `[I15]` "deben revisarse" | Sin mecanismo de detección/compensación. | Documentar proyección o restricción de migración. |
| 8 | Media | Implícita-5 `tipoRegimen` consistente con `tipo` | Sin invariante (obligatorio si `tipo=regimen-especial-territorial`). | Formalizar como invariante local. |
| 9 | Baja | L1815/L1816 `[I11a]`/`[I11b]` | No documentan enforcement (guard de comando). | Agregar "Enforcement: guard en comando". |
| 10 | Baja | Implícita-6 `ActividadEconomicaRegistrada.jurisdiccion → JurisdiccionFiscal` | Validación implícita no formalizada como invariante. | Formalizar como `[I18]` análoga a `[I13]`. |

**Resumen:** Alta 2 | Media 6 | Baja 2 | Total 10

---

## 5. Responsabilidades de Agregados (audit-behavior-responsibilities)

### Hallazgos

| # | Sev | Evidencia | Problema | Corrección mínima |
|---|-----|-----------|----------|-------------------|
| F1 | Media | L487 `CondicionDeAplicacion.evaluar(perfilEmisora, perfilContraparte, tributos, fecha)` | Coordinación inter-agregado documentada como comportamiento del propio catálogo — fuga de lógica del motor. | Reclasificar `evaluar(...)` como operación del `MotorDeCalculo`. |
| F2 | Media | L1361-1364 prorrateo en `ConfirmacionTributaria` | Regla fiscal de prorrateo vive en flujo de aplicación. | Encapsular como `RegistroTributario.prorratearDesde(...)` o domain service. |
| F3 | Media | L1294 clasificación tributaria | Validación referencial no formalizada como invariante. | Agregar invariante análoga a `[I13]`. |
| F4 | Media | L567 `validarValor(...)` delega validación referencial | Romper cohesión: parte la cumple el catálogo, parte el consumidor. | Renombrar comportamiento y exponer validación referencial separada. |
| F5 | Media | L1310-1315 `jurisdiccionResuelta` único | Output ambiguo para multi-jurisdicción (ICA por ciudad). | Mover `jurisdiccionResuelta` dentro de cada línea aplicada. |
| F6 | Media | L1316 motor declara leer `CatalogoDeAtributosFiscales` | Pero no aparece en el flujo. Ambigüedad. | Eliminarlo de la lista o explicitar el paso. |
| F7 | Media | L1183/L1256 `puedeGenerarContenido()` | No distingue generar inicial vs regenerar. | Separar en dos métodos. |
| F8 | Media | L1764 `CertificadoTributarioEntregado/Fallido` | No hay guard sobre intento de envío — at-least-once de infraestructura puede invertir orden. | Agregar `intentoEnvioId` + guard "intento abierto". |
| F9 | Baja | L1126-1129 `FormatoFiscal` cerca de anémico | Sin invariantes formalizadas (PD5). | Formalizar mínimas como `[I##]` y cerrar PD5. |
| F10 | Baja | L1144 `ContenidoGenerado` se "reemplaza" | Mecanismo ES de reemplazo no documentado. | Aclarar que cada evento captura contenido completo. |

**Resumen:** Alta 0 | Media 8 | Baja 2 | Total 10

---

## 6. Semántica de Eventos (audit-behavior-event-semantics)

### Hallazgos

| # | Sev | Evidencia | Problema | Corrección mínima |
|---|-----|-----------|----------|-------------------|
| E1 | Alta | L1525 "45 eventos … 10 agregados" vs tabla L1553 "56" | Contradicción del catálogo total. | Actualizar L1525 a "56 eventos en 12 agregados". |
| E2 | Media | L1567 `TributoModificado` "campos modificados" | Ambiguo: delta vs snapshot completo. | Documentar convención (recomendado: snapshot post-modificación). |
| E3 | Media | L1581 `EntradaDeTarifaModificada` identifica por "Factor + vigencia" | Identidad circular si cambia la propia vigencia. | Introducir `entradaId` estable. |
| E4 | Media | L1590 `CondicionModificada` "Identificador de condición" | Identidad no documentada en la entidad. | Documentar tupla o `id` sintético. |
| E5 | Media | L1611 `ActividadEconomicaRegistradaModificada` identifica por triple | Se rompe si una modificación toca un campo identificador. | Introducir `actividadId` o declarar campos inmutables. |
| E6 | Media | L1649 `SeccionFormatoEliminada` captura solo "Nombre" | Replay-unsafe si se recrea con mismo nombre. | Capturar snapshot completo + política de reutilización. |
| E7 | Media | L1718 `EntregableFiscalPresentado` sin referencia al contenido | PD6 reconoce el gap — sin hash ni versión del archivo. | Agregar `referenciaContenido` (cierra PD6). |
| E8 | Media | L1794 `CertificadoTributarioReenviado` sin `intentoEnvioId` | No correlaciona con `Entregado`/`Fallido` posteriores. | Agregar `intentoEnvioId`. |
| E9 | Baja | L1572/L1573 `TratamientoDefinido`/`ReglaDeLocalizacionDefinida` | Patrón upsert idempotente sin convención formalizada. | Documentar en Sección 2.1. |
| E10 | Baja | L1551 `EntregableFiscalCreado` vs `EntregableFiscalGenerado` | Nombres semánticamente próximos. | Considerar renombrar (opcional). |

**Resumen:** Alta 1 | Media 7 | Baja 2 | Total 10

---

## 7. Idempotencia y Concurrencia (audit-behavior-idempotency)

### Hallazgos

| # | Sev | Evidencia | Problema | Corrección mínima |
|---|-----|-----------|----------|-------------------|
| C1 | Alta | L1813 `[I9]` + L1948 `transaccionId` | Sin invariante de unicidad `(subDominio, transaccionId, efectoFiscal)` para `RegistroTributario` — at-least-once duplica el hecho fiscal. | Agregar invariante `[I##]` de unicidad con enforcement por validación al crear. |
| C2 | Alta | L1363 búsqueda registro origen para desgravamen | Sin invariante "suma de desgravámenes ≤ gravamen origen". | Agregar invariante de saldo de prorrateo o registrar como premisa del consumidor. |
| C3 | Alta | L1764 `CertificadoTributarioEntregado/Fallido` | Sin `intentoEnvioId` — at-least-once + orden invertido puede marcar como Fallido un certificado entregado. | Agregar `intentoEnvioId` correlacionado + guard "último intento abierto". |
| C4 | Media | L1607-1609 `AtributoFiscal*` | Sin invariante de no-solapamiento de vigencias por `(nombre, perfil)`. | Agregar invariante análoga a `[I1]`. |
| C5 | Media | L1610-1612 `ActividadEconomicaRegistrada*` | Sin invariante de no-solapamiento por tupla + perfil. | Agregar invariante análoga. |
| C6 | Media | L1814 `[I10]` consistencia intervención | `crear()` asume comparabilidad estructural; redondeos en desgravámenes pueden marcar `huboIntervencion` falsamente. | Definir tolerancia y regla de comparación. |
| C7 | Media | L1991 `[D11]` invariantes eventuales | Mecanismo de compensación no documentado para violaciones tardías. | Documentar detección + reacción (proyección, evento, guía operativa). |
| C8 | Media | L879 stream key `registro-tributario-{guid}` | `transaccionId` no aparece en el stream key — depende solo del inbox de plataforma. | Adoptar stream key compuesto o documentar mapeo inbox. |
| C9 | Media | L1144 + L1693 `ContenidoGenerado` lee `RegistroTributario` del período | Sin snapshot/cursor que congele el conjunto incluido entre generaciones. | Capturar cursor temporal o lista de IDs en `EntregableFiscalGenerado`. |
| C10 | Baja | L1639 `EquivalenciaCerrada` | `[I4]` no cubre no-solapamiento de vigencias. | Extender `[I4]`. |

**Resumen:** Alta 3 | Media 6 | Baja 1 | Total 10

---

## 8. Sagas y Procesos Multi-Agregado (audit-process-sagas)

### Hallazgos

| # | Sev | Evidencia | Problema | Corrección mínima |
|---|-----|-----------|----------|-------------------|
| S1 | Alta | L1363 desgravamen busca origen por `transaccionId` | Sin invariante de unicidad → resolución no determinista, prorrateo sobre registro equivocado. | Agregar invariante de unicidad o regla explícita de selección. |
| S2 | Alta | L1364-1365 `crear` después de motor | Sin compensación ni política de retry entre paso 3 y 4 del flujo. | Documentar manejo de fallos con clave de idempotencia natural. |
| S3 | Alta | L2045 (PD7) generación masiva de certificados | Sin saga formal: sin tracking, fallo parcial, correlationId de lote. | Documentar saga (trigger, estrategia ante fallo, correlationId). |
| S4 | Media | L1364 + L1991 `[D11]` | Clave natural de idempotencia del comando de confirmación no declarada. | Declarar `(subDominio, transaccionId, efectoFiscal)` como clave. |
| S5 | Media | L1991 correlationId | Identificador de proceso que conecta consumidor → registro → entregables no especificado. | Documentar identificador y verificar presencia en `RegistroTributarioCreado`. |
| S6 | Media | L1411 `CargaAsistida` pasos 5-6 humano-en-loop | Sin documentación de la ventana entre `ResultadoCarga` y persistencia. | Documentar pasos 5-6 (re-validación, política ante cambio del catálogo). |
| S7 | Media | L1342-1346 `ConfirmacionTributaria` | Ambigüedad sobre frontera dominio/aplicación (prorrateo). | Aclarar qué paso es de dominio y qué de aplicación. |
| S8 | Media | L1346 sin timeouts/retry para `MotorDeCalculo` | Sin política declarada. | Documentar timeout, retry y comportamiento ante timeout. |
| S9 | Baja | L1367 "Agregados involucrados" | Omite los transitivamente leídos por el motor. | Agregar nota "(transitivamente vía motor: …)". |
| S10 | Baja | L1369 "proyecciones interpretan efectoFiscal" | Sin documentación de ventana de consistencia eventual. | Documentar garantías de plataforma. |

**Resumen:** Alta 3 | Media 5 | Baja 2 | Total 10

---

## 9. Decisiones Abiertas (audit-quality-open-decisions)

### Hallazgos

| # | Sev | Evidencia | Problema | Corrección mínima |
|---|-----|-----------|----------|-------------------|
| 1 | Alta | L2042 (PD4) "se descarte como parte del producto" | Decisión de producto crítica sin owner ni criterio de cierre. | Agregar owner + criterio de decisión. |
| 2 | Alta | L680 retiro `actividadEconomica` | Decisión ya tomada sin formalización como `[D##]`. | Formalizar como decisión con plan de migración. |
| 3 | Alta | L2046 (PD8) "~30-50 ciudades CO" | Sin lista canónica → `[I13]` rechazará transacciones con códigos no precargados. | Lista canónica + procedimiento de expansión + fallback. |
| 4 | Alta | L2057 changelog "10 agregados, 45 eventos" | Changelog refleja v1.0; documento ya tiene Cambios 1-4. | Agregar fila v2.0 con conteos correctos. |
| 5 | Media | L1302/L1915 "Sub-cambio 2.3", L1969 "Cambio 5" | Referencias a artefactos externos no resolubles. | Reemplazar por `[D##]`/`[I##]` o mover al changelog. |
| 6 | Media | L2046-2049 (PD8-11) | Pendientes atados a "Cambio 5"/"Cambio 6" externos. | Sustituir por condición de activación operativa. |
| 7 | Media | L2043 (PD5) `FormatoFiscal` sin invariantes | TODO abierto desde v1.0 sin owner. | Convertir en hallazgo con owner al iniciar PD1. |
| 8 | Media | L2049 (PD11) proveedor fiscal externo | Sin criterios ni momento de cierre. | Agregar criterios + RFC programado. |
| 9 | Media | L799/L2011 tipos candidatos en anexo | Política de extensión del enum no formalizada como `[D##]`. | Formalizar política de extensión del enum. |
| 10 | Media | L1991 `[D11]` "revalidar si cambia plataforma" | Compromiso implícito sin owner ni proceso. | Convertir en PD## explícito. |

**Resumen:** Alta 4 | Media 6 | Baja 0 | Total 10

---

## 10. Sanity Check Coherencia Cruzada (audit-quality-sanity-check)

### Hallazgos

| # | Sev | Evidencia | Problema | Corrección mínima |
|---|-----|-----------|----------|-------------------|
| 1 | Alta | L1525 "45 eventos / 10 agregados" vs L1553 tabla "56" | Contradicción interna. | Actualizar L1525. |
| 2 | Alta | L2057 changelog "10 agregados, 45 eventos, 12 invariantes, 11 decisiones, 7 pendientes" | Desactualizado vs estado actual (16 / 56 / 17 / 13 / 11). | Agregar fila v2.0 al changelog. |
| 3 | Alta | L89-148 diagrama BC en 3.1 | No incluye `JurisdiccionFiscal` ni `CatalogoDeRegimenesEspeciales`. | Actualizar diagrama ASCII. |
| 4 | Alta | L38 `[P##]` y `[SI##]` declarados | `[SI##]` sin definición ni uso; `[P##]` sin citas. | Eliminar `[SI##]` y agregar citas `[P##]` donde se operacionalizan. |
| 5 | Media | L1815-1816 `[I11a]/[I11b]` vs changelog "12 invariantes" | División no documentada. | Documentar división y actualizar conteo a 17. |
| 6 | Media | L1302/L1915/L2046-2049 referencias "Cambio N" | Apuntan a plan externo. | Reemplazar por `[D##]`/`[I##]` o mover al changelog. |
| 7 | Media | L83 tabla clasificación de capacidades | No lista `JurisdiccionFiscal` ni `CatalogoDeRegimenesEspeciales`. | Actualizar tabla como Núcleo. |
| 8 | Media | L38 vs L1135 notación `[PD#]` vs `PD9` | Inconsistencia notacional. | Estandarizar a `[PD##]`. |
| 9 | Media | L2049 enum `Jurisdiccion.tipo` incluye tipos F2 sin precarga | El diagrama de 3.7 solo muestra tipos F1. | Agregar nota "tipos US/CA declarados sin precarga — PD11". |
| 10 | Baja | L155 referencias a anexos | Sin versión ni sello temporal. | Agregar versión a citas o nota en Sección 1. |

**Resumen:** Alta 4 | Media 5 | Baja 1 | Total 10

---

## Resumen Ejecutivo

| Skill | Alta | Media | Baja | Total |
|-------|:----:|:-----:|:----:|:-----:|
| Glosario | 1 | 5 | 4 | 10 |
| Composición | 2 | 5 | 3 | 10 |
| FSM | 0 | 3 | 4 | 7 |
| Invariantes | 2 | 6 | 2 | 10 |
| Responsabilidades | 0 | 8 | 2 | 10 |
| Semántica Eventos | 1 | 7 | 2 | 10 |
| Idempotencia | 3 | 6 | 1 | 10 |
| Sagas | 3 | 5 | 2 | 10 |
| Decisiones Abiertas | 4 | 6 | 0 | 10 |
| Sanity Check | 4 | 5 | 1 | 10 |
| **TOTAL** | **20** | **56** | **21** | **97** |

---

## Top 5 Hallazgos Críticos

| # | Skill origen | Severidad | Problema | Corrección mínima |
|---|--------------|-----------|----------|-------------------|
| 1 | Idempotencia (C1+C8) + Sagas (S1+S2) | Alta | **Duplicación potencial de `RegistroTributario`** — no existe invariante de unicidad por `(subDominio, transaccionId, efectoFiscal)`. El stream usa GUID interno, no business key, por lo que un retry at-least-once del bus puede crear dos registros tributarios distintos para la misma transacción. Impacto financiero directo en reportes y certificados. | Agregar invariante `[I##]` de unicidad; adoptar stream key compuesto con business key; declarar clave natural de idempotencia en `ConfirmacionTributaria`. |
| 2 | Sanity (1+2+3) + Glosario (1) + Eventos (E1) + Decisiones (4) | Alta | **Conteos y diagramas inconsistentes con la v2.0 alcanzada**: introducción de Sección 5 dice "45 eventos / 10 agregados" vs tabla "56 / 12"; changelog refleja v1.0; diagrama del BC en 3.1 no incluye los dos agregados nuevos. Cualquier auditoría derivada o EventCatalog generado quedará incorrecto. | Actualizar L1525, L2057 (fila v2.0 al changelog), L83 (tabla clasificación) y L89-148 (diagrama ASCII del BC). |
| 3 | Sagas (S3) + Decisiones (7) | Alta | **Generación masiva de `CertificadoTributario` sin saga formal** — declarada como "proceso de aplicación" en 3.13 pero sin trigger, fallo parcial, correlationId de lote ni reanudación. PD7 reconoce el gap pero el riesgo operativo es real: un lote con fallos parciales deja certificados faltantes sin mecanismo de recuperación. | Documentar saga completa (trigger, estrategia de fallo parcial, correlationId, reanudación) y cerrar PD7. |
| 4 | Idempotencia (C3) + Responsabilidades (F8) + Eventos (E8) | Alta | **Idempotencia del envío de certificados rota** — `CertificadoTributarioEntregado`/`Fallido` no llevan `intentoEnvioId`. Una infraestructura at-least-once puede reportar Entregado y Fallido del mismo intento en orden invertido, y el agregado aplica el último → puede marcar Fallido un certificado realmente entregado (o viceversa). | Agregar `intentoEnvioId` correlacionado a `Reenviado`/`Entregado`/`Fallido` + guard "intento abierto". |
| 5 | Decisiones (2+3) + Invariantes (2) | Alta | **Riesgo de rechazo masivo en go-live productivo** por dos pendientes acoplados: PD8 admite cobertura limitada (~30-50 ciudades CO) pero `[I13]` exige integridad referencial estricta; PD9 deja la migración de `actividadEconomica` sin formalizar como `[D##]`. Si la migración no se ejecuta antes del corte productivo, los perfiles con el atributo simple harán que el motor descarte el tributo con `motivoExclusion: actividad_no_registrada`. | Cerrar PD8 con lista canónica + procedimiento de expansión + fallback; formalizar la migración de `actividadEconomica` como decisión `[D##]` con plan de migración explícito. |

---

## Notas

- Auditoría ejecutada en paralelo por 4 agentes (uno por capa). Cada skill produjo hasta 10 hallazgos según su SKILL.md.
- Severidad: Alta (rompe invariante, lógica contradictoria, estado inalcanzable, riesgo financiero) · Media (ambigüedad que bloquea implementación, gap de especificación, riesgo no mitigado) · Baja (claridad, estilo, optimización menor).
- Regla de oro: NO se reescribió el documento. Solo diagnóstico y corrección mínima sugerida.
