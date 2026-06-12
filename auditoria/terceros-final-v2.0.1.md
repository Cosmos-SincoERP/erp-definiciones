# Audit Full — Reporte de Auditoría Completa (segunda ronda)

**Fecha:** 2026-06-12
**Modelo auditado:** `dominio/terceros/modelo-dominio.md` v2.0.1 (830 líneas — auditado completo)
**Ronda anterior:** `auditoria/terceros-v2-actual.md` (28 hallazgos — todos aplicados)
**Naturaleza de esta ronda:** verificación post-aplicación — confirma las correcciones y detecta residuos e inconsistencias introducidas por ellas.

---

### 1. Glosario y Lenguaje Ubicuo

Verificado: "vigente", "aviso" y "fuente" definidos en 2.5 (G1, G2, G3 de la primera ronda — resueltos); "registro"/"registro informante" cubierto por la fila "Fuente"; nomenclatura de eventos consistente; sin sinónimos nuevos.

**Hallazgos:** ninguno.

#### Resumen
- Alta: 0 | Media: 0 | Baja: 0 — Total: 0

---

### 2. Composición de Agregados

Verificado: identidad de `Rol` = (`dominio`, `referenciaOrigen`) aplicada coherentemente en 3.1, 3.2, `[I3]` y precondición de `RolIncorporado`; `ultimaSecuencia` y `motivoEstado` tipados; `decision` detallada; `estado` con `Fusionado`.

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Baja | L~202 "`contactos` \| Colección `Contacto` \| Estructura del paquete (issue #35): nombre, rol del contacto, correo, teléfono, marca de principal" | La fila de composición del `Rol` no refleja la representación { contacto, esPrincipal } que sí quedó en los payloads y el contrato (corrección C3 de la primera ronda — aplicada de forma incompleta). | Tipar la fila como "Colección { contacto: `Contacto`, esPrincipal }". |
| 2 | Baja | L~242 "instantánea de la evidencia al detectar: clave natural, razón social, roles y dominios" | La fila `candidatos` de la composición no incluye el **estado global** que sí ganaron el VO `Candidato` (L~279) y el payload de `PosibleDuplicadoDetectado` (L~561) con la corrección `[I14]`. | Agregar "estado global" a la instantánea de la fila. |

#### Resumen
- Alta: 0 | Media: 0 | Baja: 2 — Total: 2

---

### 3. Máquinas de Estado (FSM)

Verificado: `VersionAgregada` como progreso en `Abierta` y `EnCorreccion` (coincide con su ficha); `Fusionado` en composición y FSM; herencia de señal compatible con las transiciones (el canónico `Activo` → `Inactivo` por el derivado de fusión; si ya estaba `Inactivo`, no hay derivación — la señal más restrictiva ya rige).

**Hallazgos:** ninguno.

#### Resumen
- Alta: 0 | Media: 0 | Baja: 0 — Total: 0

---

### 4. Invariantes

Verificado: I1-I15 clasificadas; I14 Local con mecanismo; I15 Eventual con doble enforcement (paso 4 + precondición).

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Baja | L~742 "La señal repetida **enriquece** el caso existente (`VersionAgregada`), no abre otro." | La redacción de `[I15]` aplica el enriquecimiento a ambos tipos, pero `VersionAgregada` es solo de divergencias (L~584): en un duplicado, la señal repetida simplemente no produce evento — el caso abierto ya la representa. | Precisar en `[I15]`: "…enriquece el caso si es divergencia (`VersionAgregada`); en duplicados la señal repetida no produce evento". |

#### Resumen
- Alta: 0 | Media: 0 | Baja: 1 — Total: 1

---

### 5. Responsabilidades de Agregados

Verificado: el comportamiento "consolidar la identidad compartida" quedó en el agregado (R1 — resuelto); el servicio entrega y el agregado decide; "única fuente" definida a granularidad de registro (R2 — resuelto). Ambos agregados saludables.

**Hallazgos:** ninguno.

#### Resumen
- Alta: 0 | Media: 0 | Baja: 0 — Total: 0

---

### 6. Semántica de Eventos

Verificado: regla de emisión `RolInactivado`/`RolActualizado` (E1 — resuelto); estado previo de `RolIncorporado` cubre el nacimiento (E2 — resuelto); `VersionAgregada` bien delimitado (hecho propio: tercera fuente opina sobre un dato en disputa).

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Media | L~497 "Información capturada \| Identidad del rol: { rol, dominio, empresa }; `referenciaOrigen`; …" (`RolInactivado`) | El payload etiqueta { rol, dominio, empresa } como "Identidad del rol", pero la identidad de la entidad cambió a (`dominio`, `referenciaOrigen`) en la primera ronda — contradicción residual que confunde al implementador sobre cómo ubicar el rol a inactivar. | "Identidad del rol: { dominio, referenciaOrigen }; contexto: rol, empresa; `secuencia`; `fechaDelHecho`." |
| 2 | Baja | L~635 "Se publica `DatoDeIdentidadCorregido` (integración) a **los dominios** cuyo valor difiere" (`DivergenciaResuelta`, Efectos) | Residuo de la granularidad anterior: el payload ya dice `registrosACorregir`, pero los Efectos siguen hablando de dominios. | "…a los **registros señalados** (`registrosACorregir`)". |

#### Resumen
- Alta: 0 | Media: 1 | Baja: 1 — Total: 2

---

### 7. Contrato vs Flujo Interno

Verificado: `DatoDeIdentidadCorregido` con registros exactos y clave en valor previo (CV1 — resuelto); `VersionDeDato` con granularidad de registro (CV2 — resuelto); correspondencias con identificación canónica (CV3 — resuelto); materialización del contrato declarada (CV4 — resuelto).

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Media | L~667 "`DatoDeIdentidadCorregido` \| Derivado de `DivergenciaResuelta` (y de fusiones con corrección de dato)" vs L~586 "la corrección se publica también al registro recién divergente" | La tabla 5.5 no registra la **tercera derivación** del aviso, introducida con `VersionAgregada`: cuando una versión nueva llega a un caso `EnCorreccion`, la corrección ya decidida se publica al registro recién divergente. El contrato queda incompleto frente al flujo. | En la columna Origen: "Derivado de `DivergenciaResuelta`, de fusiones con corrección de dato, y de `VersionAgregada` sobre casos `EnCorreccion`". |

#### Resumen
- Alta: 0 | Media: 1 | Baja: 0 — Total: 1

---

### 8. Idempotencia y Concurrencia

Verificado: creación concurrente con estrategia (ID1 — resuelto); `VersionAgregada` con guard anti-duplicado ("la versión no estaba registrada"); `NotaAgregada` remite a `[D11]` (ID3 — resuelto); comandos de resolución protegidos por estado.

**Hallazgos:** ninguno.

#### Resumen
- Alta: 0 | Media: 0 | Baja: 0 — Total: 0

---

### 9. Sagas y Procesos Multi-Agregado

Verificado: proceso de fusión documentado con orden, compensación justificada, correlación, idempotencia por paso y persistencia (SA1 — resuelto); correlación de la consolidación declarada (SA3 — resuelto); `[PD5]` para `EnCorreccion` sin convergencia (SA2 — resuelto). El paso 2 de la fusión no puede violar `[I3]`: un registro pertenece a un solo tercero a la vez, y el reintento queda cubierto por la idempotencia por paso.

**Hallazgos:** ninguno.

#### Resumen
- Alta: 0 | Media: 0 | Baja: 0 — Total: 0

---

### 10. Decisiones Abiertas

Verificado: `[PD1]`-`[PD5]` con owner y criterio de cierre; banner actualizado a v2.0.1 (OD2 — resuelto); `[P1]` cubre el desfase de versiones del paquete (OD1 — resuelto). Sin pendientes implícitos nuevos.

**Hallazgos:** ninguno.

#### Resumen
- Alta: 0 | Media: 0 | Baja: 0 — Total: 0

---

### 11. Sanity Check (Coherencia Cruzada)

```
Referencias verificadas: D1-D12, I1-I15, P1-P4, PD1-PD5, SI1-SI9 — todas definidas y resueltas
Conteos verificados: 17 eventos (8+9) = resumen 5.1 = fichas reales; 15 invariantes (10 Local + 5 Eventual)
                     = tabla Sección 7; 5 pendientes; 11 permisos; 12 decisiones — todos correctos
Decisiones vigentes: 12 alineadas (D7 con coexistencia y herencia; D8 con EnCorreccion)
Premisas operacionalizadas: 4 reflejadas
Conceptos eliminados: sin rastros de la identidad triple salvo el residuo reportado en Semántica #1
```

**Hallazgos:** ninguno adicional (la contradicción del payload de `RolInactivado` quedó reportada en Semántica de Eventos #1; las dos filas de composición desactualizadas, en Composición #1 y #2).

#### Resumen
- Alta: 0 | Media: 0 | Baja: 0 — Total: 0

---

## Resumen Ejecutivo

| Skill | Alta | Media | Baja | Total |
|-------|------|-------|------|-------|
| Glosario | 0 | 0 | 0 | 0 |
| Composición | 0 | 0 | 2 | 2 |
| FSM | 0 | 0 | 0 | 0 |
| Invariantes | 0 | 0 | 1 | 1 |
| Responsabilidades | 0 | 0 | 0 | 0 |
| Semántica Eventos | 0 | 1 | 1 | 2 |
| Contrato vs Flujo | 0 | 1 | 0 | 1 |
| Idempotencia | 0 | 0 | 0 | 0 |
| Sagas | 0 | 0 | 0 | 0 |
| Decisiones Abiertas | 0 | 0 | 0 | 0 |
| Sanity Check | 0 | 0 | 0 | 0 |
| **TOTAL** | **0** | **2** | **4** | **6** |

**Comparación entre rondas:** 28 hallazgos (5 Alta) → **6 hallazgos (0 Alta)**. Los 28 de la primera ronda quedaron resueltos; los 6 de esta ronda son residuos de la propia aplicación (filas de composición que no acompañaron a los payloads corregidos, una etiqueta de identidad desactualizada, una derivación nueva sin registrar en la tabla de integración y dos precisiones de redacción).

## Top hallazgos (los 2 Media)

| # | Skill origen | Severidad | Problema | Corrección mínima |
|---|-------------|-----------|----------|-------------------|
| 1 | Semántica Eventos | Media | El payload de `RolInactivado` etiqueta { rol, dominio, empresa } como "Identidad del rol" — contradice la identidad (`dominio`, `referenciaOrigen`) aplicada en la primera ronda. | Reetiquetar: identidad = { dominio, referenciaOrigen }; rol y empresa como contexto. |
| 2 | Contrato vs Flujo | Media | La tabla 5.5 omite la tercera derivación de `DatoDeIdentidadCorregido` (`VersionAgregada` sobre casos `EnCorreccion`). | Completar la columna Origen. |
