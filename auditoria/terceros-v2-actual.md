# Audit Full — Reporte de Auditoría Completa

**Fecha:** 2026-06-12
**Modelo auditado:** `dominio/terceros/modelo-dominio.md` v2.0 (783 líneas — auditado completo)
**Alcance de referencia:** `dominio/terceros/definicion-alcance.md` v2.0

---

### 1. Glosario y Lenguaje Ubicuo

#### Términos con Hallazgo

| Término canónico | Variantes encontradas | Secciones donde aparece | Tipo de problema |
|-----------------|----------------------|------------------------|-----------------|
| *(sin término)* | "vigente" | I1 (L~685), SI1 (L~279), TerceroCreado (L~428) | Ambigüedad — sin definición |
| aviso | "aviso", "mensaje", "evento de integración" | 2.5, 5.5, D10 | Sinónimos no controlados |
| fuente | "dominios fuente", "las fuentes discrepan", "única fuente" | 3.6, 5.4, 6.3 | Ambigüedad de granularidad |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Media | L~685 "terceros **vigentes** (no fusionados)"; L~428 "Clave natural sin tercero **vigente**" | "Vigente" se usa como condición de unicidad y de creación pero no está definido: ¿un tercero `Inactivo` es vigente? (debería serlo — conserva la clave). | Agregar fila en 2.5: "vigente = no fusionado (Activo o Inactivo)". |
| 2 | Baja | L~84 "nunca se re-publican como **avisos**" vs L~729 "Injerencia por **mensajes**" | "Aviso", "mensaje" y "evento de integración" nombran lo mismo sin declaración de sinonimia. | Nota en 2.5 fijando "aviso" como término y los otros como equivalentes. |
| 3 | Baja | L~315 "si el evento trae identidad compartida distinta y es la **única fuente**" | "Fuente" oscila entre dominio y registro informante; en divergencias la fuente real es el rol (un dominio puede tener dos registros). | Precisar en 2.5: "fuente = un rol informado (dominio + referenciaOrigen)". |

#### Resumen
- Alta: 0 | Media: 1 | Baja: 2 — Total: 3 hallazgos

---

### 2. Composición de Agregados

#### Inventario por Agregado

**Tercero** — Entidades internas: `Rol` (1..N). VOs: `IdentificacionLegal`, `DireccionFisica`, `Telefono`, `CorreoElectronico`, `Contacto` (paquete). Atributos raíz: `terceroId`, `identificacionLegal`, `razonSocial`, `tipoPersona`, `estado`, `motivoEstado`. Comportamientos calculados: ninguno declarado.

**Conciliacion** — Entidades internas: ninguna. VOs propios: `Candidato`, `VersionDeDato`. Atributos: `conciliacionId`, `tipo`, `estado`, `motivoCierre`, `decision`, `notas`.

#### Inconsistencias

| Agregado | Componente | Declarado en composición | Referenciado en eventos | Tipo de inconsistencia |
|----------|-----------|-------------------------|------------------------|----------------------|
| Tercero | Identidad de `Rol` | (`rol`, `dominio`, `empresa`) | La fusión incorpora roles homólogos del absorbido | Identidad insuficiente |
| Tercero | `motivoEstado` | "Texto" | `motivo { codigo (catálogo 6.4), descripcion }` | Tipo desalineado |
| Tercero (`Rol`) | última `secuencia` aplicada | No declarada | Precondición de `RolActualizado` la exige | Dato referenciado sin hogar |
| Tercero (`Rol`) | `esPrincipal` | "atributo de la relación en el Rol, no del VO" (L~267) | Anidado dentro del elemento contacto en payloads | Representación ambigua |
| Conciliacion | `decision` | "Estructura de resolución" sin atributos | Eventos capturan decisor, fecha, motivo, tipo | Tipo sin detallar |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | **Alta** | L~191 "Identidad de la entidad \| (`rol`, `dominio`, `empresa`)" vs L~517 "Sus roles se incorporan al canónico" | El duplicado típico (la razón de ser de la fusión) tiene **el mismo rol del mismo dominio en la misma empresa** en ambos terceros: dos registros distintos de OXP (referenciaOrigen distinta). Al absorber, ambos deben persistir en el canónico pero colisionan en la identidad triple — `[I3]` lo prohíbe. La fusión más común es irrepresentable. | Identidad de `Rol` = (`dominio`, `referenciaOrigen`); `rol` y `empresa` pasan a atributos. Ajustar `[I3]` en consecuencia. |
| 2 | Media | L~185 "`motivoEstado` \| Texto" vs L~491 "`motivo` { codigo (catálogo 6.4), descripcion }" | La composición declara texto plano; los eventos capturan estructura con código del catálogo 6.4. | Tipar `motivoEstado` como { codigo, descripcion }. |
| 3 | Media | L~453 "`secuencia` mayor a la última aplicada (`[SI3]`)" | La precondición compara contra "la última aplicada" pero ningún componente del agregado la almacena. | Declarar `ultimaSecuencia` en la entidad `Rol` (o indicar explícitamente que vive en el estado reconstruido del stream). |
| 4 | Baja | L~267 "La marca de principal es atributo de la relación en el `Rol`" vs L~442 "contactos [ { …, esPrincipal } ]" | El criterio dice que `esPrincipal` no es del VO, pero el payload lo anida dentro del contacto. | Representar la colección como [ { contacto: Contacto, esPrincipal } ] o nota aclaratoria. |
| 5 | Baja | L~229 "`decision` \| Estructura de resolución" | Tipo sin atributos declarados, aunque los eventos los capturan. | Detallar: { tipoDecision, decididaPor, fecha, motivo }. |

#### Resumen
- Alta: 1 | Media: 2 | Baja: 2 — Total: 5 hallazgos

---

### 3. Máquinas de Estado (FSM)

#### FSM por Agregado

**FSM: Tercero** — Estados: Activo, Inactivo. Terminales: Fusionado ■. Transiciones: TerceroCreado (∅→Activo), TerceroInactivado (Activo→Inactivo), TerceroReactivado (Inactivo→Activo), TerceroAbsorbido (Activo|Inactivo→Fusionado). Progreso: RolIncorporado, RolActualizado, RolInactivado, IdentidadActualizada (en Activo e Inactivo). Sin estados huérfanos ni sumideros no intencionados; los 8 eventos del catálogo están cubiertos.

**FSM: Conciliacion** — Estados: Abierta, EnCorreccion. Terminales: Cerrada ■. Transiciones: PosibleDuplicadoDetectado/DivergenciaDetectada (∅→Abierta), TercerosFusionados/HomonimiaMarcada (Abierta→Cerrada), DivergenciaSuperada (Abierta→Cerrada), DivergenciaResuelta (Abierta→EnCorreccion), ConvergenciaConfirmada (EnCorreccion→Cerrada). Progreso: NotaAgregada (Abierta, EnCorreccion). Sin huérfanos; los 8 eventos cubiertos.

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | **Alta** | L~513 "Estado previo: `Activo` o `Inactivo`" (TerceroAbsorbido); L~566 payload de fusión sin tratamiento de señal | Fusionar candidatos con **señales globales distintas** no tiene regla: si el absorbido estaba `Inactivo` por fraude y el canónico `Activo`, el resultado opera sin restricción — el veto desaparece silenciosamente. | Regla en `TercerosFusionados`: el canónico hereda la señal más restrictiva de los candidatos (o precondición de igualar señales antes de fusionar), documentada en la ficha del evento y en la FSM 4.1. |

#### Resumen
- Alta: 1 | Media: 0 | Baja: 0 — Total: 1 hallazgo

---

### 4. Invariantes

#### Clasificación de Invariantes

| ID | Invariante (resumen) | Tipo | Agregado(s) | Enforcement documentado | Gap |
|----|---------------------|------|-------------|------------------------|-----|
| I1 | Unicidad de clave natural entre vigentes | Eventual | Tercero | Índice `[SI1]` + `[SI9]` | Colisión de creación concurrente sin estrategia (ver Idempotencia #1) |
| I2 | Nace Activo con ≥1 rol | Local | Tercero | Mismo append | — |
| I3 | Un rol por (rol, dominio, empresa) | Local | Tercero | Precondición | Identidad insuficiente (ver Composición #1) |
| I4 | Señal solo por administrador con motivo | Local | Tercero | Precondición + permiso | — |
| I5 | Identidad sin comandos de edición | Local | Tercero | Ausencia de comandos | — |
| I6 | ≥2 candidatos / ≥2 versiones | Local | Conciliacion | Precondición de apertura | — |
| I7 | Canónico ∈ candidatos | Local | Conciliacion | Precondición | — |
| I8 | Decisión con decisor+fecha+motivo | Local | Conciliacion | Precondición de comandos | — |
| I9 | Homonimia no se reabre | Eventual | Conciliacion + memoria | Consulta `[SI5]` | — |
| I10 | EnCorreccion/convergencia solo divergencias | Local | Conciliacion | FSM | — |
| I11 | Ningún evento de rol se descarta | Eventual | Servicio | Pasos 1-4 + `[D11]` | — |
| I12 | Fusionado es terminal | Local | Tercero | FSM | — |
| I13 | Claves absorbidas enrutan al canónico | Eventual | Servicio + mapa | `[SI4]` | — |
| implícita-1 | Una sola Conciliacion abierta por señal | — | Conciliacion | **No formalizada** | Ver hallazgo 1 |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Media | L~529 precondiciones de `PosibleDuplicadoDetectado` (solo criterio + memoria); L~541 `DivergenciaDetectada` | No existe invariante que impida **dos Conciliaciones abiertas por la misma señal**: cada evento de rol que repita la condición (R09/R14) re-dispara la detección — tras la carga histórica, el mismo par de candidatos generaría un caso por cada evento consolidado. | Nueva invariante: "una sola Conciliacion abierta por (par de candidatos + criterio) y por (terceroId + datoEnDisputa)"; precondición en los dos eventos de apertura consultando casos abiertos. |

#### Resumen
- Alta: 0 | Media: 1 | Baja: 0 — Total: 1 hallazgo

---

### 5. Responsabilidades de Agregados

#### Mapa de responsabilidades

**Tercero** — Razón de cambio dominante: consolidación de la identidad compartida y señal global. Comandos: 2. Eventos: 8. Invariantes: I1-I5, I12. Domain services: ServicioDeConsolidacion. **Diagnóstico: Saludable, con una fuga puntual** (hallazgo 1).

**Conciliacion** — Razón de cambio dominante: el ciclo de decisión humana sobre señales de calidad. Comandos: 4. Eventos: 8. Invariantes: I6-I10. **Diagnóstico: Saludable.**

**ServicioDeConsolidacion** — Coordina sin absorber, salvo el criterio del hallazgo 1.

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Media | L~315 "si el evento trae identidad compartida distinta **y es la única fuente**, actualizarla" | La decisión "actualizar identidad vs abrir divergencia" depende solo del estado interno del `Tercero` (cuántas fuentes tiene) — es lógica del agregado que vive en el paso 3 del servicio. | Declararla como comportamiento del agregado (el servicio entrega el dato; el agregado decide `IdentidadActualizada` o señal de divergencia) — basta una frase en 3.2 y ajustar el paso 3. |
| 2 | Baja | L~315 "es la única fuente" | "Única fuente" sin definición operativa (¿un solo dominio? ¿un solo rol?). | Precisar: "todos los roles del tercero provienen del mismo dominio y registro" o el criterio que se decida. |

#### Resumen
- Alta: 0 | Media: 1 | Baja: 1 — Total: 2 hallazgos

---

### 6. Semántica de Eventos

#### Inventario semántico

**Tercero** — De transición: TerceroCreado, TerceroInactivado, TerceroReactivado, TerceroAbsorbido. De progreso: RolIncorporado, RolActualizado, RolInactivado, IdentidadActualizada. Naming consistente: Sí (pasado, PascalCase, sin calificadores redundantes). Payloads completos: Sí.

**Conciliacion** — De transición: PosibleDuplicadoDetectado, DivergenciaDetectada, TercerosFusionados, HomonimiaMarcada, DivergenciaResuelta, ConvergenciaConfirmada, DivergenciaSuperada. De progreso: NotaAgregada. Naming consistente: Sí. Payloads completos: Sí (con la salvedad de Contrato vs Flujo #2).

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Media | L~449 `RolActualizado` "(datos… o su estado completo más reciente)" vs L~461 `RolInactivado` | Solapamiento: el contrato `[D5]` trae estado completo, así que una inactivación llega como estado con `estadoEnOrigen=inactivo` — expresable por ambos eventos. No hay regla de cuál emite el servicio. | Regla de emisión en 3.6/5.3: `RolInactivado` cuando la transición observada de `estadoEnOrigen` sea activo→inactivo; cualquier otro cambio → `RolActualizado`. |
| 2 | Baja | L~437-439 causalidad triple de `RolIncorporado` con "Estado previo: `Activo` o `Inactivo`" | En el nacimiento (mismo append con `TerceroCreado`) el estado previo es "no existe" — la ficha no cubre ese caso. | Estado previo: "— (nacimiento) \| Activo \| Inactivo". |

#### Resumen
- Alta: 0 | Media: 1 | Baja: 1 — Total: 2 hallazgos

---

### 7. Contrato vs Flujo Interno

#### Matriz Contrato ↔ Flujo (resumen de los 4 contratos con diagnóstico)

**`DatoDeIdentidadCorregido` (L~624)** — evento-integración. Flujo interno: la `Conciliacion` conoce las versiones por fuente y los registros exactos; el contrato expone (clave natural, dato, valor). **Diagnóstico: Dimensión perdida + ambigüedad de destino.**

**`versiones` de la divergencia (L~542)** — payload/contrato del caso. Flujo: el servicio compara valores por rol (dominio + referenciaOrigen + empresa); el contrato expone solo `dominio`. **Diagnóstico: Dimensión perdida.**

**`TercerosFusionados` como aviso (L~623)** — evento-integración. Flujo: fusión por `terceroId`; contrato expone `claveNatural → terceroCanonicoId` (UUID interno). **Diagnóstico: Tipo inconsistente con el consumidor.**

**Contrato de entrada (L~399)** — Forma completa y bien dimensionada. **Diagnóstico: Coherente, con un gap de materialización** (hallazgo 4).

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | **Alta** | L~624 "Clave natural, dato en disputa, valor correcto \| Los dominios cuyo valor difiere — corrigen su registro automáticamente" | El aviso no identifica con precisión el registro destino. Peor: si el dato corregido **es la identificación**, no se especifica si la clave del aviso es la errada (la que el dominio tiene) o la corregida — una aplicación automática con la clave equivocada corrige el registro equivocado. | El payload incluye las `referenciaOrigen` destino (la bodega las conoce por los roles) y se declara que la clave viaja en su valor **previo** a la corrección. |
| 2 | Media | L~542 "`versiones` [ { valor, dominio, fechaDelEvento } ]" | El flujo conoce qué rol exacto informó cada valor; el contrato colapsa a dominio: con dos registros del mismo dominio (post-fusión es seguro), el administrador no puede distinguir cuál informó qué, y `dominiosACorregir` opera a granularidad gruesa. | Agregar `referenciaOrigen` (y `empresa`) a `VersionDeDato` y a `dominiosACorregir` → `registrosACorregir`. |
| 3 | Media | L~566 "`correspondencias` [ { claveNatural → terceroCanonicoId } ]" | `terceroCanonicoId` es el UUID interno de la bodega; Contabilidad identifica terceros por identificación legal embebida en sus asientos — no posee ni necesita el UUID. | Incluir la identificación legal canónica en cada correspondencia (clave → clave canónica + terceroCanonicoId). |
| 4 | Media | L~401 "Lo publican los dominios fuente (OXP: su Proveedor…)" | No se define quién materializa el contrato: ¿OXP emite directamente el evento estándar, o emite `ProveedorCreado` y algo lo adapta? Afecta a los tres equipos consumidores del contrato. | Declarar en 5.2: cada dominio fuente emite el contrato como su propio evento de integración (sin adaptador intermedio), o la alternativa que se decida. |

#### Resumen
- Alta: 1 | Media: 3 | Baja: 0 — Total: 4 hallazgos

---

### 8. Idempotencia y Concurrencia

#### Matriz de idempotencia

| Operación / Comando | Agregado | IdempotencyKey | Guard anti-duplicado | Optimistic concurrency | Riesgo |
|---------------------|----------|----------------|---------------------|----------------------|--------|
| Consolidación de evento de rol | Tercero | (referenciaOrigen, secuencia) `[SI3]` | Sí — descarta secuencias viejas | `[D11]` plataforma | Bajo |
| Creación (`TerceroCreado`) | Tercero | — | Índice `[SI1]` (eventual) | `[D11]` | **Alto** (ver hallazgo 1) |
| `FusionarTerceros` / `MarcarHomonimia` / `ResolverDivergencia` | Conciliacion | Estado `Abierta` requerido | Sí — FSM rechaza re-ejecución | `[D11]` | Bajo |
| `InactivarTercero` / `ReactivarTercero` | Tercero | Estado previo requerido | Sí — FSM | `[D11]` | Bajo |
| `AgregarNota` | Conciliacion | — | No | `[D11]` | Bajo (ruido) |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | **Alta** | L~685 "[I1] … Eventual \| Índice único `[SI1]`"; L~428 "Clave natural sin tercero vigente" | **Creación concurrente**: dos fuentes publican la misma clave nueva a la vez (escenario garantizado en la carga histórica — OXP y CXC migrando al mismo tercero). Ambos pasan la precondición, el índice rechaza al segundo… y no está documentado qué hace el servicio con el perdedor. Riesgo de evento de rol perdido (violaría `[I11]`). | En `[SI1]`/paso 3: ante colisión del índice, reintentar resolviendo de nuevo la clave y consolidar sobre el tercero ya creado. |
| 2 | Baja | L~553 `NotaAgregada` "{ texto; usuarioId; fecha }" | Reintento del comando duplica la nota (sin clave de idempotencia). Impacto solo de ruido. | Cubierto por `[D11]` — basta mencionarlo en la ficha. |

#### Resumen
- Alta: 1 | Media: 0 | Baja: 1 — Total: 2 hallazgos

---

### 9. Sagas y Procesos Multi-Agregado

#### Mapa de procesos

**Proceso: ServicioDeConsolidacion** — Trigger: evento de rol. Agregados: Tercero, Conciliacion. Pasos: 5 documentados. Compensación: declarada innecesaria con justificación (acumulativo e idempotente). CorrelationId: no declarado. IdempotencyKey por paso: `[SI3]`. Persistencia del estado: sin estado propio (cada paso es derivación de streams). **Diagnóstico: completo salvo correlación.**

**Proceso: Fusión** (TercerosFusionados → TerceroAbsorbido×N → RolIncorporado×M en canónico → mapa `[SI4]`) — Trigger: comando FusionarTerceros. Agregados: Conciliacion + 2..N Terceros. Pasos: **no documentados como proceso** (solo como "Efectos"). Compensación: no aplica declarado… implícitamente. CorrelationId: `conciliacionId` viaja en TerceroAbsorbido pero no se declara como correlación. **Diagnóstico: incompleto.**

**Proceso: Corrección de divergencia** (DivergenciaResuelta → aviso → dominios aplican → regreso por F1 → ConvergenciaConfirmada) — Ventana documentada (estado `EnCorreccion` ✓). Sin política si no converge.

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | **Alta** | L~567 "Efectos \| `TerceroAbsorbido` en cada absorbido y `RolIncorporado` en el canónico (efectos inter-agregado); mapa canónico actualizado" | La fusión toca N agregados en cadena y no documenta: orden de los pasos, idempotencia por paso ante reintento, ni qué ve la ficha si falla a mitad (absorbido ya `Fusionado`, roles aún no incorporados → roles invisibles). Es el proceso más delicado del BC. | Documentar la cadena en 3.6 o 5.4: orden (absorbidos → canónico → mapa), correlación e idempotencia por `conciliacionId`, reintento hasta completar (sin compensación), y la ventana visible. |
| 2 | Media | L~589 "`EnCorreccion` — la decisión está tomada; falta que los dominios converjan" | Sin política si un dominio nunca aplica la corrección (ej: fuente que aún no implementa la aplicación automática): el caso queda `EnCorreccion` indefinidamente, sin seguimiento ni escalamiento documentado. | Nota en 4.2 + nuevo `[PD]` con owner (producto/equipo técnico) para la política de seguimiento. |
| 3 | Media | L~309-317 (pasos del servicio, sin correlación declarada) | No se declara cómo trazar la cadena evento de rol → consolidación → señales (correlationId). | Declarar (referenciaOrigen, secuencia) como correlación de la cadena en 3.6 — alineado con `[D11]`. |

#### Resumen
- Alta: 1 | Media: 2 | Baja: 0 — Total: 3 hallazgos

---

### 10. Decisiones Abiertas

#### Inventario de pendientes

| # | Ubicación (L~N) | Texto literal | Tipo | Decisión temporal | Riesgo | Criterio de cierre |
|---|-----------------|--------------|------|-------------------|--------|-------------------|
| 1 | L~750 `[PD1]` | "Veredicto del custodio… issue #35" | Pendiente formal | Estructura propuesta en el issue | Medio | Resolución #35 antes de F1 |
| 2 | L~751 `[PD2]` | "Criterios ampliados… (F2)" | Diferido formal | Criterio R09 en F1 | Bajo | Diseño F2 |
| 3 | L~752 `[PD3]` | "Ratificar el catálogo de motivos…" | Pendiente formal | Propuesta inicial 6.4 | Bajo | Comité antes de F1 |
| 4 | L~753 `[PD4]` | "Issue cruzado… Contabilidad" | Pendiente formal | — | Medio | Al fusionar el PR #33 |
| 5 | L~739 `[P1]` | "con las validaciones empaquetadas del producto" | **Implícita** | — | Medio | Ver hallazgo 1 |
| 6 | L~3 | "v2.0 — En construcción" | Banner temporal | — | Bajo | Cierre de auditoría |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Media | L~739 "La bodega verifica con las mismas reglas" | Decisión implícita sin formalizar: ¿qué pasa cuando dominio y bodega tienen **versiones distintas del paquete** de validaciones? (un dominio rezagado captura con reglas viejas). `[R04]` lo absorbe como anomalía→conciliación, pero nadie lo declara. | Ampliar `[P1]` ("las diferencias de versión del paquete se manifiestan como anomalías → conciliación, nunca rechazo") o nuevo `[PD]` con owner custodio. |
| 2 | Baja | L~3 "v2.0 — En construcción (junio 2026)" | El banner quedará obsoleto al cerrar la auditoría y fusionar el PR. | Actualizar el banner al cierre del #33. |

#### Resumen
- Alta: 0 | Media: 1 | Baja: 1 — Total: 2 hallazgos

---

### 11. Sanity Check (Coherencia Cruzada)

#### Resumen de coherencia

```
Referencias verificadas: D1-D12, I1-I13, P1-P4, PD1-PD4, SI1-SI9 — todas definidas; 0 usadas-sin-definir
Conteos verificados: 11 correctos (eventos 16=8+8, invariantes 13=9L+4E, VOs 7, SIs 9,
                     decisiones 12, premisas 4, pendientes 4, permisos 11, FSM 2, catálogos 4, pasos 5)
                     0 inconsistentes
Decisiones vigentes: 12 alineadas, 0 desalineadas
Premisas operacionalizadas: 4 reflejadas (P1→R04/paso1, P2→D5/SI3, P3→SI8, P4→3.7), 0 sin reflejo
Conceptos eliminados: sin rastros ("figura", "EnRegistro", "Abortado" solo en contraste declarado con v1.0)
```

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Media | L~184 "`estado` \| `Activo` \| `Inactivo`" vs L~357 "FUSIONADO ■" | La composición de la raíz no incluye el estado terminal `Fusionado` que la FSM 4.1 sí tiene. | Tipar `estado` como `Activo \| Inactivo \| Fusionado`. |
| 2 | Baja | L~96 "entidad interna `Rol`, **uno por dominio y empresa**" vs L~191 identidad (`rol`, `dominio`, `empresa`) | La prosa de 3.1 omite la dimensión `rol` de la identidad triple. | "uno por rol, dominio y empresa" (o la identidad que resulte del hallazgo Composición #1). |
| 3 | Baja | L~689, L~695-697 (I5, I11, I12, I13) | Cuatro invariantes definidas sin referencia cruzada desde las fichas de eventos o FSM donde se hacen cumplir. | Citarlas en las fichas correspondientes (TerceroAbsorbido→I12/I13; paso 1→I11; 3.2→I5). |

#### Resumen
- Alta: 0 | Media: 1 | Baja: 2 — Total: 3 hallazgos

---

## Resumen Ejecutivo

| Skill | Alta | Media | Baja | Total |
|-------|------|-------|------|-------|
| Glosario | 0 | 1 | 2 | 3 |
| Composición | 1 | 2 | 2 | 5 |
| FSM | 1 | 0 | 0 | 1 |
| Invariantes | 0 | 1 | 0 | 1 |
| Responsabilidades | 0 | 1 | 1 | 2 |
| Semántica Eventos | 0 | 1 | 1 | 2 |
| Contrato vs Flujo | 1 | 3 | 0 | 4 |
| Idempotencia | 1 | 0 | 1 | 2 |
| Sagas | 1 | 2 | 0 | 3 |
| Decisiones Abiertas | 0 | 1 | 1 | 2 |
| Sanity Check | 0 | 1 | 2 | 3 |
| **TOTAL** | **5** | **13** | **10** | **28** |

## Top 5 Hallazgos Críticos

| # | Skill origen | Severidad | Problema | Corrección mínima |
|---|-------------|-----------|----------|-------------------|
| 1 | Composición | Alta | La identidad de la entidad `Rol` (`rol`, `dominio`, `empresa`) hace **irrepresentable la fusión típica**: dos registros del mismo dominio/rol/empresa (el duplicado real) colisionan al absorber. | Identidad = (`dominio`, `referenciaOrigen`); `rol` y `empresa` como atributos; ajustar `[I3]`. |
| 2 | Contrato vs Flujo | Alta | `DatoDeIdentidadCorregido` no identifica el registro destino y es ambiguo cuando el dato corregido es la propia clave — la **corrección automática puede aplicarse al registro equivocado**. | Incluir `referenciaOrigen` destino y declarar que la clave viaja en su valor previo. |
| 3 | FSM | Alta | Fusionar candidatos con **señales globales distintas** no tiene regla: el veto por fraude del absorbido desaparece silenciosamente. | El canónico hereda la señal más restrictiva (o precondición de igualar señales). |
| 4 | Idempotencia | Alta | **Creación concurrente** de la misma clave (garantizada en carga histórica) sin estrategia documentada para el perdedor del índice — riesgo de evento de rol perdido (violaría `[I11]`). | Ante colisión de `[SI1]`: reintentar y consolidar sobre el tercero existente. |
| 5 | Sagas | Alta | La **fusión no está documentada como proceso**: sin orden de pasos, idempotencia por paso ni ventana visible definida — fallo parcial deja roles invisibles. | Documentar la cadena (orden, correlación por `conciliacionId`, reintento, ventana). |
