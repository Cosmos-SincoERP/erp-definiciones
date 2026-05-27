# Asistente de Onboarding — Modelo de dominio

> **Versión:** 1.0
> **Fecha:** Mayo 2026
> **Alcance:** `definicion-alcance.md` v1.0 — caso PUC en v1.0

---

## 1. Introducción

Este documento describe el comportamiento del servicio compartido **Asistente de Onboarding**: agregados, eventos, transiciones, precondiciones, invariantes y decisiones. El **qué hace** el servicio vive en `definicion-alcance.md`. Este documento describe el **cómo se comporta** el modelo de dominio.

### Convenciones

- **Referencias:** `[R##]` reglas de negocio (viven en `definicion-alcance.md`), `[D##]` decisiones de diseño documentadas en este modelo, `[I##]` invariantes, `[P##]` premisas, `[SI##]` sugerencias de implementación, `[PD#]` pendientes.
- **Lenguaje:** funcional/de negocio. Los mecanismos técnicos (concurrencia, idempotencia, reintentos, almacenamiento) viven en la sección de Sugerencias de Implementación, no por evento.
- **Eventos `*Modificado`:** capturan delta (campos identificadores + campos efectivamente cambiados), no snapshot completo. El estado se reconstruye reproduciendo el stream.
- **Multi-país:** términos comprensibles independientemente de la jurisdicción (`jurisdiccion`, `sector`, `modeloNegocio`).
- **Inspiración:** el sistema de aprendizaje del asistente está inspirado en el agregado `Aprendizaje` del Motor de Traducción del sub-dominio Contabilidad (sección 3.3 de su `modelo-dominio.md`). La cadena de aplicación de heurísticas A→C→B sigue el patrón documentado en `[DD2]` de ese mismo sub-dominio.

---

## 2. Tabla de contenido

3. Bounded Context y agregados
4. Ciclos de vida (FSM)
5. Catálogo de eventos
6. Invariantes
7. Decisiones de arquitectura y diseño
8. Premisas
9. Pendientes
10. Sugerencias de implementación
11. Permisos atómicos
12. Control de versiones

---

## 3. Bounded Context y agregados

### 3.1. Asistente de Onboarding como Bounded Context

**Clasificación de elementos:**

| Elemento | Tipo | Fase |
|----------|------|:----:|
| ProcesoOnboardingPUC | Transaccional (ES, FSM) | F1 |
| PUCdeReferencia | Configuración | F1 |
| ReglaDeRevisionPUC | Configuración | F1 |
| AprendizajeOnboardingPUC | Transaccional (ES, sin FSM) | F1 |
| ServicioDeGeneracionPUC | Domain service | F1 |
| ServicioDeAnalisisAutomatico | Domain service | F1 |

**Diagrama del Bounded Context:**

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                Bounded Context: Asistente de Onboarding (caso PUC, v1.0)          │
│                                                                                   │
│  ┌─ Catálogos del producto (mantenidos por el equipo de producto) ────────────┐  │
│  │                                                                              │ │
│  │  ┌──────────────────────┐    ┌──────────────────────────┐                  │ │
│  │  │ PUCdeReferencia      │    │ ReglaDeRevisionPUC       │                  │ │
│  │  │ (configuración)      │    │ (configuración)          │                  │ │
│  │  └──────────┬───────────┘    └──────────┬───────────────┘                  │ │
│  │             │                            │                                  │ │
│  └─────────────┼────────────────────────────┼──────────────────────────────────┘ │
│                │                            │                                    │
│                │   consultados por          │                                    │
│                ▼                            ▼                                    │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                                                                              │ │
│  │  ┌────────────────────────────────┐                                         │ │
│  │  │  ProcesoOnboardingPUC          │   ────► alimenta ────►                  │ │
│  │  │  (transaccional, FSM)          │                                         │ │
│  │  │                                │                                         │ │
│  │  │  INICIADO → EN_ANALISIS →      │   ┌─────────────────────────────────┐  │ │
│  │  │  EN_REVISION → LISTO_PARA_     │   │ AprendizajeOnboardingPUC        │  │ │
│  │  │  GENERAR → GENERADO ■          │──▶│ (transaccional ES, receptor     │  │ │
│  │  │                                │   │  pasivo, por empresa)            │  │ │
│  │  │                                │   └─────────────────────────────────┘  │ │
│  │  │                  └──► ABANDONADO ■                                       │ │
│  │  │                                                                          │ │
│  │  └────────────────┬───────────────┘                                         │ │
│  │                   │                                                         │ │
│  │     [ServicioDeGeneracionPUC] al PUCFinalGenerado                           │ │
│  │                   ▼                                                         │ │
│  │     ┌─────────────────────────────────┐                                     │ │
│  │     │ Sub-dominio Contabilidad        │                                     │ │
│  │     │   MarcoContable (si custom)     │                                     │ │
│  │     │   PlanDeCuentas                 │                                     │ │
│  │     │   CuentaContable (N)            │                                     │ │
│  │     └─────────────────────────────────┘                                     │ │
│  │                                                                              │ │
│  └─────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                   │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

### 3.2. Agregado: PUCdeReferencia (configuración)

**Descripción:** catálogo de Planes Únicos de Cuentas base que sirven como referencia comparativa durante el análisis automático. Cada referencia se identifica por sector económico, modelo de negocio y jurisdicción. El producto provee referencias precargadas (construcción, inmobiliaria, concesiones viales, administrativa); el equipo de producto puede agregar, modificar o desactivar referencias con permisos especiales.

**Raíz:** PUCdeReferencia

**Atributos de la raíz:**

| Atributo | Descripción |
|----------|-------------|
| `codigo` | Identificador estable y único, inmutable tras creación [I8]. Texto descriptivo (ej: `CONSTRUCCION_CO`, `INMOBILIARIA_CO`, `ADMINISTRATIVA_GENERICA`). |
| `nombre` | Texto presentable al usuario, localizable. |
| `descripcion` | Texto largo que explica para qué tipo de empresa aplica la referencia. |
| `sector` | Sector económico al que aplica (servicios, comercio, manufactura, construcción, etc.). |
| `modeloNegocio` | Modelo de negocio específico (construcción de obra, arrendamiento de inmuebles, concesiones, comercio general, etc.). |
| `jurisdiccion` | Código ISO de jurisdicción donde aplica la referencia (CO, DO, PA, etc.). |
| `marcoContable` | Marco contable bajo el que se diseñó la referencia. Por defecto: `NIIF`. Referencia al código del agregado `MarcoContable` del sub-dominio Contabilidad. |
| `estado` | Activo / Inactivo. Inactivar previene seleccionarla en nuevos procesos; procesos en curso que ya la seleccionaron no se afectan. |

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| CuentaDeReferencia | Entidad | Cuenta individual dentro del PUC base | `codigo`, `nombre`, `tipo` (gasto/costo/ingreso/activo/pasivo/patrimonio/banco), `nivel` (maestra/auxiliar), `obligatoriedadTercero`, `obligatoriedadUnidadOrganizacional`, `descripcion` opcional con notas operativas |

**Ciclo de vida:** Configuración — sin FSM transaccional. La referencia se crea, se le agregan cuentas, se modifican, se inactivan o se reactivan a lo largo del tiempo.

**Stream de eventos:** `puc-de-referencia-{codigo}`

**Eventos:** patrón uniforme de configuración (ver Sección 5.3.1).

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  PUCdeReferencia (Agregado)                                          │
│                                                                      │
│  codigo: CONSTRUCCION_CO                                             │
│  nombre: PUC de referencia — Construcción Colombia                  │
│  sector: Construcción · modeloNegocio: Construcción de obra         │
│  jurisdiccion: CO · marcoContable: NIIF · estado: activo            │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ CuentaDeReferencia                                              │  │
│  │  codigo: 6135 · nombre: Costo de obra · tipo: costo             │  │
│  │  nivel: maestra · obligatoriedadTercero: opcional                │  │
│  │  obligatoriedadUndOrg: obligatoria                               │  │
│  │  descripcion: Cuenta agrupadora para costos de proyectos        │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ CuentaDeReferencia                                              │  │
│  │  codigo: 1440 · nombre: Anticipos a proveedores · tipo: activo  │  │
│  │  nivel: maestra · obligatoriedadTercero: obligatoria             │  │
│  │  obligatoriedadUndOrg: opcional                                  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ...                                                                 │
└──────────────────────────────────────────────────────────────────────┘
```

---

### 3.3. Agregado: ReglaDeRevisionPUC (configuración)

**Descripción:** catálogo de reglas heurísticas formales que el motor de análisis aplica para detectar patrones en el PUC del sistema anterior y proponer transformaciones. El producto provee reglas precargadas (mínimo las 12 identificadas en el caso PUC, ver `casos/onboarding-puc.md`). El equipo de producto agrega, modifica o desactiva reglas con permisos especiales. La consultora en campo no crea reglas — las aplica o las solicita.

**Raíz:** ReglaDeRevisionPUC

**Atributos de la raíz:**

| Atributo | Descripción |
|----------|-------------|
| `codigo` | Identificador estable y único, inmutable tras creación [I7]. Texto descriptivo (ej: `CONSOLIDAR_CUENTAS_POR_TERCERO`, `REUBICAR_ATRIBUTOS_FISCALES`). |
| `nombre` | Texto presentable al usuario. |
| `descripcion` | Explicación detallada del patrón que detecta y de la transformación que propone. |
| `categoria` | Una de las cinco categorías de tratamiento: Consolidar, Conservar, Reubicar, Foco, Validar. |
| `patron` | Descripción funcional del patrón de detección (cuándo aplica la regla). Lenguaje de negocio, no jerga técnica. |
| `sugerencia` | Descripción funcional de qué propone hacer la regla (consolidar X en Y, descartar atributo Z, etc.) y de las consecuencias de aplicarla. |
| `severidad` | Crítica (requiere atención obligatoria del consultor), Recomendada (mejora pero no bloquea), Informativa (señala sin proponer cambio). |
| `estado` | Activa / Inactiva. Inactivar previene aplicarla en procesos nuevos; procesos en curso que ya la consideraron no se afectan. |

**Composición:** sin entidades internas. La raíz contiene todos los atributos.

**Ciclo de vida:** Configuración — sin FSM transaccional.

**Stream de eventos:** `regla-de-revision-puc-{codigo}`

**Eventos:** patrón uniforme de configuración (ver Sección 5.3.2).

**Origen de las reglas:**

| Origen | Cómo se crea |
|--------|--------------|
| **Estándar del producto** | Reglas precargadas con el producto. Mantenidas por el equipo de producto. |
| **Promovida desde aprendizaje** | El equipo de producto detecta un patrón repetido en el aprendizaje de varias empresas (o de una con alto volumen) y decide promoverlo a regla formal. Es una acción explícita. |

---

### 3.4. Agregado: AprendizajeOnboardingPUC (transaccional ES — receptor pasivo)

**Descripción:** registro acumulado de decisiones del consultor para una empresa específica. Cuando el consultor acepta una sugerencia en un proceso de onboarding, el patrón decidido queda registrado aquí. En procesos futuros de la misma empresa, el motor consulta este aprendizaje como Nivel C de la cadena de aplicación de heurísticas, antes de la comparación contra la referencia.

Es un **receptor pasivo** — sus eventos son efectos inter-agregado de acciones en `ProcesoOnboardingPUC`. No tiene comandos de creación propios; el stream se crea implícitamente al primer evento. Inspirado en el agregado `Aprendizaje` del Motor de Traducción del sub-dominio Contabilidad (sección 3.3 de su `modelo-dominio.md`).

**Raíz:** AprendizajeOnboardingPUC

**Atributos de la raíz:**

| Atributo | Descripción |
|----------|-------------|
| `empresa` | Identificador de la empresa propietaria del aprendizaje. |

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| DecisionAprendida | Entidad | Una decisión acumulada para un patrón específico | `patronDetectado` (descripción del patrón), `decision` (qué se decidió hacer), `frecuencia` (cuántas veces se ha confirmado), `fechaUltimaAplicacion`, `consultorResponsable` (último consultor que confirmó), `estado` (vigente / invalidada) |

**Ciclo de vida:** Transaccional (ES) — sin FSM. Eventos de registro y promoción. **Nota:** el `AprendizajeOnboardingPUC` es un receptor pasivo — no tiene evento de creación propio. Su stream se crea implícitamente con el primer `DecisionAprendida` [SI04]. Patrón análogo al `Aprendizaje` del sub-dominio Contabilidad.

**Stream de eventos:** `aprendizaje-onboarding-puc-{empresa}`

**Eventos:**

| Evento | Origen |
|--------|--------|
| `DecisionAprendida` | Efecto inter-agregado de `SugerenciaAceptada` (o `SugerenciaModificada` con el ajuste registrado) en `ProcesoOnboardingPUC`. |
| `AprendizajePromovidoAReglaDeRevision` | Acción explícita del equipo de producto al detectar un patrón consistente. Genera `ReglaDeRevisionPUCCreada` en el agregado de reglas como efecto inter-agregado. |
| `AprendizajeInvalidado` | Acción del equipo de producto cuando identifica que una decisión aprendida no debería propagarse a futuros procesos. |

**Gobernanza:** el aprendizaje se alimenta automáticamente de las decisiones del consultor en cada proceso. El equipo de producto supervisa periódicamente los aprendizajes acumulados de cada empresa: puede invalidar aprendizajes erróneos o promover patrones consistentes a reglas formales del catálogo del producto [R14] [R15].

---

### 3.5. Agregado: ProcesoOnboardingPUC (transaccional ES — corazón del modelo)

**Descripción:** representa un proceso de onboarding del PUC para una empresa específica. Tiene ciclo de vida con FSM de cinco estados y registra el camino completo desde la carga del PUC del sistema anterior hasta la generación del PUC operativo en el sub-dominio Contabilidad. Una empresa puede tener varios procesos a lo largo del tiempo (multi-intento), todos persistidos para auditoría; solo uno termina como `GENERADO` (definitivo) [R05] [R06].

**Raíz:** ProcesoOnboardingPUC

**Atributos de la raíz:**

| Atributo | Descripción |
|----------|-------------|
| `id` | Identificador único del proceso. |
| `empresa` | Empresa propietaria del proceso. |
| `contexto` | Información del contexto seleccionado en la fase 2: `sector`, `modeloNegocio`, `jurisdiccion`. |
| `pucDeReferenciaUsado` | Referencia al `PUCdeReferencia` seleccionado para la comparación. |
| `marcoContableDestino` | Marco contable bajo el cual se generará el PUC final (por defecto: `NIIF`). |
| `intentoNumero` | Número de intento secuencial para la empresa (1, 2, 3...). Útil para reportes y trazabilidad. |
| `consultorResponsable` | Consultor que inició el proceso. |
| `fechaInicio` | Marca de tiempo del inicio. |
| `fechaFin` | Marca de tiempo del cierre (solo en estados terminales). |
| `estado` | Derivado del stream — no es atributo persistido. Posibles valores: INICIADO, EN_ANALISIS, EN_REVISION, LISTO_PARA_GENERAR, GENERADO, ABANDONADO. |

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| PUCLegacyImportado | VO | Información del archivo cargado | `origen` (Excel / CSV), `nombreArchivo`, `fechaCarga`, `totalCuentas`, `huellaContenido` (identificador del contenido del archivo para detectar duplicados) |
| ResultadoAnalisis | VO | Resumen del resultado del análisis automático | `cuentasAConsolidar`, `cuentasAReubicar`, `cuentasAValidar`, `cuentasAConservar`, `cuentasEnFoco` |
| SugerenciaGenerada | Entidad | Una sugerencia producida por el análisis | `id`, `reglaAplicada` (código de la regla), `categoria`, `registrosAfectados`, `transformacionPropuesta`, `consecuenciasAceptar`, `consecuenciasRechazar`, `estadoSugerencia` (pendiente / aceptada / modificada / rechazada / aplazada) |
| DecisionDeRevision | Entidad | La decisión del consultor sobre una sugerencia | `sugerenciaId`, `accion` (aceptar / modificar / rechazar / aplazar), `ajusteAplicado` (si la acción es Modificar), `justificacion`, `fechaDecision`, `consultorResponsable` |
| PUCResultante | VO | Solo poblado en estado GENERADO | Referencia al `PlanDeCuentas` creado en Contabilidad, total de cuentas finales, referencia al reporte de migración descargable |

**Ciclo de vida:** Transaccional (ES) con FSM de cinco estados (ver Sección 4.1).

**Stream de eventos:** `proceso-onboarding-puc-{id}`

**Eventos:** detallados en Sección 5.2.

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  ProcesoOnboardingPUC (Agregado)                                     │
│                                                                      │
│  id: PRC-2026-0042 · empresa: COSMOS-SAS · intento: 2               │
│  contexto: { sector: Construcción, modeloNegocio: Obra, juris: CO }  │
│  pucDeReferenciaUsado: CONSTRUCCION_CO                               │
│  marcoContableDestino: NIIF                                          │
│  estado: EN_REVISION (derivado del stream)                           │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ PUCLegacyImportado (VO)                                         │  │
│  │  origen: Excel · nombreArchivo: PUC-COSMOS-v2.xlsx              │  │
│  │  fechaCarga: 2026-05-26 · totalCuentas: 1847                    │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ ResultadoAnalisis (VO)                                          │  │
│  │  cuentasAConsolidar: 312 · cuentasAReubicar: 47                 │  │
│  │  cuentasAValidar: 8 · cuentasAConservar: 1480 · enFoco: 220     │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ SugerenciaGenerada (#1 de 359)                                  │  │
│  │  reglaAplicada: CONSOLIDAR_CUENTAS_POR_TERCERO                  │  │
│  │  categoria: Consolidar · registrosAfectados: 47 cuentas          │  │
│  │  estadoSugerencia: aceptada                                      │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ DecisionDeRevision (sobre Sugerencia #1)                        │  │
│  │  accion: aceptar · fechaDecision: 2026-05-26                    │  │
│  │  consultorResponsable: cons-007                                  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ...                                                                 │
└──────────────────────────────────────────────────────────────────────┘
```

**Integración con Contabilidad:** al emitir `PUCFinalGenerado`, el `ServicioDeGeneracionPUC` desencadena en el sub-dominio Contabilidad:

1. Si el `marcoContableDestino` no existe como `MarcoContable` activo para la empresa, se emite `MarcoContableCreado` en el agregado correspondiente.
2. Se emite `PlanDeCuentasCreado` con la referencia al marco.
3. Por cada cuenta del PUC resultante, se emite `CuentaAgregada` en el `PlanDeCuentas`.

Es un efecto inter-agregado eventual coordinado por el domain service (ver Sección 3.7).

---

### 3.6. Domain service: ServicioDeAnalisisAutomatico

**Trigger:** evento `PUCdeReferenciaSeleccionado` en un `ProcesoOnboardingPUC`.

**Flujo:**

| Paso | Acción | Evento emitido |
|------|--------|---------------|
| 1 | Aplicar reglas formales activas del catálogo `ReglaDeRevisionPUC` sobre las cuentas del PUC legacy. Cada coincidencia genera una sugerencia tentativa con su categoría y severidad. | — |
| 2 | Consultar `AprendizajeOnboardingPUC` de la empresa. Para cada patrón aprendido vigente que coincida con cuentas del PUC legacy, generar (o reforzar) la sugerencia correspondiente. Las sugerencias provenientes del aprendizaje quedan marcadas con su origen. | — |
| 3 | Comparar las cuentas del PUC legacy contra las `CuentaDeReferencia` del `PUCdeReferencia` seleccionado. Las diferencias (cuentas faltantes, cuentas adicionales, niveles inconsistentes, longitudes de código atípicas) generan sugerencias de categoría Validar o Foco. | — |
| 4 | Consolidar todas las sugerencias evitando duplicados (si dos niveles producen la misma sugerencia, prevalece el de mayor precedencia: A > C > B). | — |
| 5 | Emitir `AnalisisAutomaticoEjecutado` con el `ResultadoAnalisis` agregado y las sugerencias generadas. El proceso transita de `EN_ANALISIS` a `EN_REVISION`. | `AnalisisAutomaticoEjecutado` |

**CorrelationId:** `procesoId` del `ProcesoOnboardingPUC`.

**Nota sobre la cadena A→C→B:** el orden de precedencia (reglas formales primero, luego aprendizaje, luego comparación con referencia) sigue el patrón documentado en `[DD2]` del sub-dominio Contabilidad para el Motor de Traducción [D4].

---

### 3.7. Domain service: ServicioDeGeneracionPUC

**Trigger:** evento `ProcesoListoParaGenerar` cuando el consultor confirma la generación.

**Flujo:**

| Paso | Acción | Evento emitido | Stream destino |
|------|--------|---------------|----------------|
| 1 | Validar precondiciones: estado actual es `LISTO_PARA_GENERAR`, no hay sugerencias en estado `APLAZADA` [I3]. | — | — |
| 2 | Determinar si el `marcoContableDestino` ya existe como `MarcoContable` activo para la empresa. Si no existe, se debe crear. | — | — |
| 3 | Si aplica, emitir `MarcoContableCreado` en el sub-dominio Contabilidad. | `MarcoContableCreado` | `marco-contable-{empresa}-{codigo}` |
| 4 | Emitir `PlanDeCuentasCreado` en Contabilidad con la referencia al marco. | `PlanDeCuentasCreado` | `plan-de-cuentas-{id}` |
| 5 | Por cada cuenta del PUC resultante (aplicando las decisiones aceptadas y modificadas), emitir `CuentaAgregada` en el `PlanDeCuentas`. | `CuentaAgregada` (N veces) | `plan-de-cuentas-{id}` |
| 6 | Generar reporte de migración descargable con detalle de todas las decisiones [R21]. | — | — |
| 7 | Emitir `PUCFinalGenerado` en el `ProcesoOnboardingPUC`. El proceso transita a `GENERADO`. | `PUCFinalGenerado` | `proceso-onboarding-puc-{id}` |

**Tabla de compensación:**

| Paso | Si falla | Estrategia |
|------|----------|------------|
| 3 | Fallo al crear `MarcoContable` | Reintento. Operación idempotente por código del marco. |
| 4 | Fallo al crear `PlanDeCuentas` después de crear el marco | Reintento. Si persiste, el marco existe sin PUC asociado — situación recuperable manualmente o por reintento. |
| 5 | Fallo al agregar alguna cuenta | Reintento por cada cuenta. Idempotente por código de cuenta dentro del PUC. |
| 7 | Fallo al emitir `PUCFinalGenerado` después de crear todo en Contabilidad | Reintento. Mientras no se emita, el proceso permanece en `LISTO_PARA_GENERAR` y el consultor puede reintentar la generación; el sistema detecta que el PUC ya existe en Contabilidad y solo emite el evento de cierre. |

**CorrelationId:** `procesoId` del `ProcesoOnboardingPUC`.

**IdempotencyKey:** un proceso solo puede generar PUC final una vez [I5]. La emisión repetida de `PUCFinalGenerado` para el mismo proceso se ignora [SI02].

---

## 4. Ciclos de vida (FSM)

### 4.1. ProcesoOnboardingPUC — FSM

```
                          ┌──────────────┐
                          │   INICIADO   │   carga del archivo
                          └──────┬───────┘   + selección de contexto
                                 │
                                 │  PUCdeReferenciaSeleccionado
                                 ▼
                          ┌──────────────┐
                          │ EN_ANALISIS  │   motor analiza
                          └──────┬───────┘
                                 │
                                 │  AnalisisAutomaticoEjecutado
                                 ▼
                          ┌──────────────┐
                          │ EN_REVISION  │   consultor revisa
                          │ (pausable y  │   sugerencias por grupo
                          │  reanudable) │
                          └──────┬───────┘
                                 │
                                 │  ProcesoListoParaGenerar
                                 │  (todas las sugerencias resueltas)
                                 ▼
                ┌─────────────────────────────────┐
                │     LISTO_PARA_GENERAR          │
                └────────┬──────────────┬─────────┘
                         │              │
                  PUCFinalGenerado      │ ProcesoAbandonado
                         │              │ (también disponible desde
                         ▼              │  cualquier estado activo)
                  ┌────────────┐        ▼
                  │ GENERADO ■ │    ┌──────────────┐
                  └────────────┘    │ ABANDONADO ■ │
                                    └──────────────┘
```

**Notas sobre el ciclo de vida:**

- **INICIADO** es transitorio — al recibir el PUC legacy y el contexto, el consultor selecciona o confirma el `PUCdeReferencia` y avanza inmediatamente a `EN_ANALISIS`.
- **EN_ANALISIS** es transitorio y operado por el sistema. Sin participación del consultor mientras se ejecutan las reglas, el aprendizaje y la comparación.
- **EN_REVISION** es el estado donde el consultor interactúa. Pausable y reanudable [R03]. El consultor puede salir y volver en otra sesión — el sistema reconstruye el estado exacto del stream.
- **LISTO_PARA_GENERAR** señala que el consultor terminó de decidir todas las sugerencias [I3]. Aún puede regresar a `EN_REVISION` para modificar decisiones o pasar a `GENERADO` confirmando la generación.
- **GENERADO** es terminal e inmutable. El PUC final ya existe en Contabilidad. Las decisiones quedan persistidas como historial auditable [R02].
- **ABANDONADO** es terminal. Se alcanza por decisión explícita del consultor (cancelar) o automáticamente cuando se inicia un nuevo proceso para la misma empresa estando uno activo [R01] [R04].

**Transición a ABANDONADO:** disponible desde `INICIADO`, `EN_ANALISIS`, `EN_REVISION` y `LISTO_PARA_GENERAR`. No disponible desde estados terminales.

---

## 5. Catálogo de eventos

### 5.1. Resumen por agregado

| Agregado | Tipo | Eventos |
|----------|:----:|:-------:|
| ProcesoOnboardingPUC | Transaccional | 13 |
| AprendizajeOnboardingPUC | Transaccional | 3 |
| PUCdeReferencia | Configuración | 6 |
| ReglaDeRevisionPUC | Configuración | 4 |
| | **Total** | **26** |

### 5.2. Eventos transaccionales

#### ProcesoOnboardingPUCIniciado

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | El consultor inicia un nuevo proceso de onboarding del PUC para una empresa. |
| **Causalidad** | Directa (acción del consultor). |
| **Precondiciones** | No existen procesos activos previos para la misma empresa [R01]. Si existen, deben abandonarse antes. |
| **Información capturada** | empresa, intentoNumero, consultorResponsable, fechaInicio. |
| **Efectos** | Proceso transita a INICIADO. |

#### PUCLegacyImportado

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | Se cargó el archivo del PUC del sistema anterior y se validó su formato. |
| **Causalidad** | Directa (consultor carga archivo). |
| **Precondiciones** | Proceso en estado INICIADO. Formato del archivo válido (Excel o CSV con columnas mínimas requeridas). |
| **Información capturada** | origen, nombreArchivo, fechaCarga, totalCuentas, huellaContenido. |
| **Efectos** | El PUC legacy queda disponible para el análisis. |

#### PUCdeReferenciaSeleccionado

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | El consultor seleccionó (o confirmó la sugerencia del sistema sobre) la estructura de referencia para la comparación. |
| **Causalidad** | Directa (consultor selecciona). |
| **Precondiciones** | Proceso en estado INICIADO con `PUCLegacyImportado` ya registrado. El `PUCdeReferencia` seleccionado debe estar activo [I4]. |
| **Información capturada** | pucDeReferenciaUsado, contexto (sector, modeloNegocio, jurisdiccion), marcoContableDestino. |
| **Efectos** | Proceso transita a EN_ANALISIS. Dispara al `ServicioDeAnalisisAutomatico`. |

#### AnalisisAutomaticoEjecutado

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | El motor de análisis terminó de aplicar reglas, aprendizaje y comparación; las sugerencias quedan generadas y listas para revisión del consultor. |
| **Causalidad** | Efecto del `ServicioDeAnalisisAutomatico`. |
| **Precondiciones** | Proceso en estado EN_ANALISIS. |
| **Información capturada** | resultadoAnalisis (cuentasAConsolidar, cuentasAReubicar, cuentasAValidar, cuentasAConservar, cuentasEnFoco), sugerenciasGeneradas (listado con regla, categoría, registros afectados, transformación). |
| **Efectos** | Proceso transita a EN_REVISION. |

#### SugerenciaAceptada

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | El consultor aceptó una sugerencia tal cual fue propuesta. |
| **Causalidad** | Directa (acción del consultor). |
| **Precondiciones** | Proceso en estado EN_REVISION. La sugerencia existe y está en estado pendiente o aplazada. |
| **Información capturada** | sugerenciaId, fechaDecision, consultorResponsable. |
| **Efectos** | La sugerencia pasa a estado aceptada. Genera `DecisionAprendida` como efecto inter-agregado eventual en `AprendizajeOnboardingPUC` [R12]. |

#### SugerenciaModificada

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | El consultor aceptó la sugerencia pero ajustó algún detalle de la transformación propuesta. |
| **Causalidad** | Directa (acción del consultor). |
| **Precondiciones** | Proceso en estado EN_REVISION. La sugerencia existe y está en estado pendiente o aplazada. La justificación de la modificación es obligatoria [R09]. |
| **Información capturada** | sugerenciaId, ajusteAplicado (campos cambiados respecto a la propuesta original — patrón delta), justificacion, fechaDecision, consultorResponsable. |
| **Efectos** | La sugerencia pasa a estado modificada. Genera `DecisionAprendida` con el ajuste registrado [R12]. |

#### SugerenciaRechazada

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | El consultor rechazó la sugerencia. La transformación propuesta no se aplica. |
| **Causalidad** | Directa (acción del consultor). |
| **Precondiciones** | Proceso en estado EN_REVISION. La sugerencia existe y está en estado pendiente o aplazada. La justificación es obligatoria [R09]. |
| **Información capturada** | sugerenciaId, justificacion, fechaDecision, consultorResponsable. |
| **Efectos** | La sugerencia pasa a estado rechazada. No alimenta el aprendizaje [R12]. |

#### SugerenciaAplazada

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | El consultor pospone la decisión sobre una sugerencia. La sugerencia volverá a presentarse antes del cierre del proceso [R10]. |
| **Causalidad** | Directa (acción del consultor). |
| **Precondiciones** | Proceso en estado EN_REVISION. La sugerencia existe y está en estado pendiente. |
| **Información capturada** | sugerenciaId, fechaDecision, consultorResponsable. |
| **Efectos** | La sugerencia pasa a estado aplazada. Bloquea la transición a LISTO_PARA_GENERAR mientras esté en ese estado [I3]. |

#### RevisionDeGrupoCompletada

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | El consultor terminó de revisar todas las sugerencias de un grupo (en el caso PUC: un grupo contable). Marca el avance del proceso y permite que el consultor pause sin perder contexto. |
| **Causalidad** | Directa (avance del consultor). |
| **Precondiciones** | Proceso en estado EN_REVISION. Todas las sugerencias del grupo están decididas (aceptadas, modificadas, rechazadas o aplazadas). |
| **Información capturada** | grupoCompletado, totalSugerenciasGrupo, decisionesPorAccion (resumen). |
| **Efectos** | Avance del progreso del proceso. El consultor puede pausar aquí. |

#### ProcesoListoParaGenerar

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | Todas las sugerencias del proceso fueron resueltas (ninguna en estado aplazada). El proceso queda listo para generar el PUC final. |
| **Causalidad** | Derivado (cuando la última sugerencia aplazada se resuelve). |
| **Precondiciones** | Proceso en estado EN_REVISION. Ninguna sugerencia en estado aplazada [I3]. |
| **Información capturada** | totalSugerencias, decisionesPorAccion (resumen final). |
| **Efectos** | Proceso transita a LISTO_PARA_GENERAR. El consultor puede confirmar la generación o regresar a modificar decisiones. |

#### PUCFinalGenerado

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | El proceso terminó. El PUC final fue creado en el sub-dominio Contabilidad. Estado terminal. |
| **Causalidad** | Efecto del `ServicioDeGeneracionPUC` después de coordinar la creación en Contabilidad. |
| **Precondiciones** | Proceso en estado LISTO_PARA_GENERAR. Generación nunca antes ejecutada para este proceso [I5]. |
| **Información capturada** | pucResultante (referencia al PlanDeCuentas creado), referenciaReporteMigracion, fechaFin. |
| **Efectos** | Proceso transita a GENERADO ■. Datos quedan inmutables [R02]. |

#### ProcesoAbandonado

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | ProcesoOnboardingPUC |
| **Stream** | `proceso-onboarding-puc-{id}` |
| **Descripción** | El proceso se abandonó. Puede ser por decisión explícita del consultor o automáticamente al iniciarse uno nuevo para la misma empresa [R04]. Estado terminal. |
| **Causalidad** | Directa (consultor cancela) o efecto inter-agregado (nuevo proceso iniciado). |
| **Precondiciones** | Proceso en estado no terminal (INICIADO, EN_ANALISIS, EN_REVISION o LISTO_PARA_GENERAR). |
| **Información capturada** | motivo (cancelación voluntaria / reemplazado por proceso nuevo), fechaFin. |
| **Efectos** | Proceso transita a ABANDONADO ■. Historial conservado [R05]. |

#### DecisionRegistradaEnAprendizaje

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | AprendizajeOnboardingPUC |
| **Stream** | `aprendizaje-onboarding-puc-{empresa}` |
| **Descripción** | Una decisión aceptada (o modificada con su ajuste) del consultor se registra en el aprendizaje de la empresa. |
| **Causalidad** | Efecto inter-agregado eventual de `SugerenciaAceptada` o `SugerenciaModificada` en el `ProcesoOnboardingPUC` [R12]. |
| **Precondiciones** | La decisión está aceptada o modificada (no rechazada ni aplazada). |
| **Información capturada** | patronDetectado, decision, ajusteAplicado (si modificada), consultorResponsable, fechaAprendizaje. |
| **Efectos** | Si ya existe `DecisionAprendida` para el mismo patrón, se incrementa la frecuencia y se actualiza la última fecha. Si no existe, se crea. Disponible para procesos futuros de la misma empresa. |

#### AprendizajePromovidoAReglaDeRevision

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | AprendizajeOnboardingPUC |
| **Stream** | `aprendizaje-onboarding-puc-{empresa}` |
| **Descripción** | El equipo de producto promueve una decisión aprendida a regla formal del catálogo del producto. |
| **Causalidad** | Directa (acción del equipo de producto). |
| **Precondiciones** | La `DecisionAprendida` existe y tiene frecuencia suficiente para considerarse patrón consistente (criterio cualitativo del equipo de producto). |
| **Información capturada** | patronDetectado, decisionPromovida, codigoNuevaRegla. |
| **Efectos** | Genera `ReglaDeRevisionPUCCreada` en `ReglaDeRevisionPUC` como efecto inter-agregado eventual. La decisión aprendida queda marcada como promovida. |

#### AprendizajeInvalidado

| Aspecto | Detalle |
|---------|---------|
| **Agregado** | AprendizajeOnboardingPUC |
| **Stream** | `aprendizaje-onboarding-puc-{empresa}` |
| **Descripción** | El equipo de producto invalida una decisión aprendida específica. Procesos futuros no la aplicarán [R15]. |
| **Causalidad** | Directa (acción del equipo de producto). |
| **Precondiciones** | La `DecisionAprendida` existe y está vigente. |
| **Información capturada** | patronDetectado, motivoInvalidacion, responsable. |
| **Efectos** | La decisión queda marcada como invalidada. Procesos ya generados no se afectan. |

---

### 5.3. Eventos de configuración

Los eventos de configuración siguen un patrón uniforme: el agregado se crea una vez y los atributos se modifican (delta), inactivan o reactivan a lo largo del tiempo.

#### 5.3.1. PUCdeReferencia — 6 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `PUCdeReferenciaCreado` | Se creó una nueva estructura de referencia. Nace en estado activo. | codigo, nombre, descripcion, sector, modeloNegocio, jurisdiccion, marcoContable. | [R17] [R18] |
| 2 | `CuentaDeReferenciaAgregada` | Se agregó una cuenta a la estructura. | codigo, nombre, tipo, nivel, obligatoriedadTercero, obligatoriedadUnidadOrganizacional. | — |
| 3 | `CuentaDeReferenciaModificada` | Se modificaron atributos de una cuenta de referencia (delta — campos identificadores + campos cambiados). | codigo (identifica), campos modificados. | — |
| 4 | `CuentaDeReferenciaInactivada` | Una cuenta de la referencia dejó de aplicarse. | codigo, motivo. | — |
| 5 | `PUCdeReferenciaModificado` | Se modificaron atributos de la raíz (nombre, descripción, sector, modeloNegocio). El código no cambia. | campos modificados. | [R18] |
| 6 | `PUCdeReferenciaInactivado` | La estructura de referencia completa dejó de ofrecerse para nuevos procesos. | codigo, motivo. | — |

#### 5.3.2. ReglaDeRevisionPUC — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `ReglaDeRevisionPUCCreada` | Se registró una nueva regla en el catálogo. Nace activa. Puede ser creada directamente por el equipo de producto o como efecto de `AprendizajePromovidoAReglaDeRevision`. | codigo, nombre, descripcion, categoria, patron, sugerencia, severidad. | [R16] [R18] |
| 2 | `ReglaDeRevisionPUCModificada` | Se actualizaron atributos de la regla (delta). El código no cambia. | codigo (identifica), campos modificados. | [R18] |
| 3 | `ReglaDeRevisionPUCInactivada` | La regla dejó de aplicarse a nuevos procesos. | codigo, motivo. | — |
| 4 | `ReglaDeRevisionPUCReactivada` | La regla previamente inactivada volvió a aplicarse. | codigo. | — |

---

## 6. Invariantes

| ID | Invariante | Agregado(s) | Tipo | Reglas |
|----|------------|-------------|------|--------|
| I1 | Una empresa puede tener varios ProcesoOnboardingPUC, pero a lo sumo uno en estado no terminal (INICIADO, EN_ANALISIS, EN_REVISION o LISTO_PARA_GENERAR). Se valida al emitir ProcesoOnboardingPUCIniciado. | ProcesoOnboardingPUC | Eventual | [R01] |
| I2 | Un ProcesoOnboardingPUC en estado ABANDONADO o GENERADO no puede recibir nuevos eventos. Los datos quedan inmutables. | ProcesoOnboardingPUC | Local | [R02] |
| I3 | La transición a LISTO_PARA_GENERAR requiere que ninguna sugerencia esté en estado APLAZADA. Se valida al evaluar si se debe emitir ProcesoListoParaGenerar. | ProcesoOnboardingPUC | Local | [R10] [R19] |
| I4 | El PUCdeReferencia referenciado en PUCdeReferenciaSeleccionado debe estar en estado activo al momento del evento. Cambios posteriores en el estado del PUCdeReferencia no afectan al proceso que ya lo seleccionó. | ProcesoOnboardingPUC, PUCdeReferencia | Eventual | — |
| I5 | PUCFinalGenerado solo puede emitirse una vez por proceso. Si ya existe en el stream, una nueva solicitud de generación se ignora (idempotencia). | ProcesoOnboardingPUC | Local | [R20] |
| I6 | Solo SugerenciaAceptada y SugerenciaModificada generan DecisionRegistradaEnAprendizaje. SugerenciaRechazada y SugerenciaAplazada no alimentan el aprendizaje. Se garantiza en el ServicioDeAnalisisAutomatico que produce las sugerencias y en la coordinación inter-agregado. | ProcesoOnboardingPUC, AprendizajeOnboardingPUC | Eventual | [R12] |
| I7 | El código de una ReglaDeRevisionPUC es único globalmente e inmutable tras creación. Solo nombre, descripción, patrón, sugerencia y severidad admiten modificación. | ReglaDeRevisionPUC | Local | [R18] |
| I8 | El código de un PUCdeReferencia es único globalmente e inmutable tras creación. Solo nombre, descripción, sector y modeloNegocio admiten modificación. | PUCdeReferencia | Local | [R18] |

**Clasificación:**
- **Local:** se valida dentro de un solo agregado, en la misma transacción.
- **Eventual:** cruza fronteras de agregado o depende de consultas externas. Se garantiza mediante consultas o procesos asíncronos.

---

## 7. Decisiones de arquitectura y diseño

| # | Decisión | Justificación | Referencia |
|---|----------|---------------|------------|
| D1 | El asistente vive en `compartido/asistente-onboarding/` como servicio transversal, no dentro del sub-dominio Contabilidad. | El patrón aplicará a otros casos futuros (terceros, unidades organizacionales, saldos iniciales). Ubicarlo en `compartido/` evita inflar el modelo de Contabilidad con algo conceptualmente transversal. | [P1] |
| D2 | Aprendizaje por empresa en v1.0 (evolucionable a global con anonimización si el volumen lo justifica más adelante). | Más simple y suficiente para validar el patrón. Cada empresa desarrolla su propio aprendizaje. Coherente con el patrón `Aprendizaje` del N1 del sub-dominio Contabilidad. | [R13] |
| D3 | Cuatro agregados separados (no uno solo) por responsabilidades distintas: catálogos del producto (PUCdeReferencia, ReglaDeRevisionPUC), aprendizaje (acumulación pasiva por empresa) y proceso (transacción con FSM). | Cada agregado tiene ciclo de vida e invariantes propias. Mezclarlos haría imposible mantener la gobernanza separada (catálogos del producto vs decisiones operativas vs proceso de un cliente específico). | — |
| D4 | La cadena de aplicación de heurísticas sigue el patrón A→C→B del Motor de Traducción del sub-dominio Contabilidad: Nivel A (reglas formales de ReglaDeRevisionPUC), Nivel C (AprendizajeOnboardingPUC de la empresa), Nivel B (comparación con PUCdeReferencia + validaciones estructurales). | Reusa un patrón ya validado en el proyecto. Mantiene consistencia en el lenguaje del modelo. | Inspirado en [DD2] del sub-dominio Contabilidad. |
| D5 | El ProcesoOnboardingPUC es el coordinador del flujo. Los catálogos son consultados y el aprendizaje es alimentado. No hay coordinación cruzada compleja. El ServicioDeGeneracionPUC y el ServicioDeAnalisisAutomatico son los únicos domain services. | Modelo simple de lectura. La complejidad vive en el agregado central; los demás son periféricos. | — |
| D6 | Multi-intento sin replicación de datos: cada intento es un proceso independiente con su propio stream. Los procesos abandonados se conservan inmutables para auditoría. | Coherente con Event Sourcing: cada proceso es un evento histórico. Permite auditar todo el camino del cliente. | [R05] [R06] |
| D7 | Concurrencia y idempotencia técnica (cargas duplicadas del archivo, doble clic en aceptar sugerencia, reintentos de generación) se manejan como sugerencias de implementación, no como invariantes de dominio. | Las invariantes y reglas pertenecen al dominio; los mecanismos de plataforma viven en sugerencias de implementación. Coherente con el patrón establecido en OXP y Contabilidad. | Ver [SI01]-[SI04]. |
| D8 | La selección del PUCdeReferencia es por el consultor con sugerencia del sistema (no automática estricta). El sistema propone basado en el contexto pero el consultor confirma o selecciona otra. | Casos límite (empresas con líneas de negocio mixtas) requieren juicio humano. La autonomía completa del sistema introduciría error en ese tipo de casos. | — |
| D9 | El PUCResultante del proceso queda como VO solo en estado GENERADO. Antes de la generación, las sugerencias pendientes/aplazadas/decididas viven como entidades dentro del proceso, no como un PUC tentativo. | El PUC final se construye al cerrar el proceso aplicando las decisiones; no se mantiene un PUC tentativo en construcción. Reduce complejidad del modelo. | — |

---

## 8. Premisas

| # | Premisa | Implicación |
|---|---------|-------------|
| P1 | El caso PUC es el primer caso modelado del patrón. Cuando llegue el segundo caso (terceros, unidades organizacionales o saldos iniciales), se evaluará si extraer un patrón genérico reutilizable (un agregado `ProcesoOnboarding` parametrizado por tipo de caso) o mantener cada caso autónomo. | Mientras tanto, los agregados son específicos del caso PUC (`ProcesoOnboardingPUC`, `PUCdeReferencia`, etc.). El nombre con sufijo `PUC` facilita la decisión futura. |
| P2 | Las reglas heurísticas aplicadas por el motor en v1.0 son específicas del caso PUC (las 12 documentadas en `casos/onboarding-puc.md`). La estructura del agregado `ReglaDeRevisionPUC` es reutilizable; las reglas concretas son específicas. | Si se extrae el patrón genérico en el futuro, la estructura del agregado de reglas se generalizará a `ReglaDeRevision` con un campo `tipoCaso`. |
| P3 | El PUC legacy se carga desde Excel o CSV en v1.0. Conexión directa a SincoA&F u otros ERPs son evolución posterior. | El asistente parseará archivos en formato común (xlsx, csv) con un esquema mínimo de columnas requeridas. Validación de formato en la fase 1 del flujo. |
| P4 | El aprendizaje del asistente no usa modelos de aprendizaje automático (ML) en v1.0. Es un sistema de patrones repetidos con coincidencia explícita: si el consultor toma N veces la misma decisión sobre el mismo patrón, el sistema lo sugiere automáticamente en futuros procesos de la misma empresa. | El umbral de coincidencia y el detalle del patrón son refinables. ML se considerará si el volumen lo justifica (proceso continuo del equipo de producto). |
| P5 | Las reglas de revisión que se promueven desde el aprendizaje requieren acción explícita del equipo de producto, no son automáticas. | Evita ruido en el catálogo de reglas formales. El equipo de producto cura qué se vuelve regla del producto. |

---

## 9. Pendientes por definir

| ID | Pendiente | Momento de cierre |
|----|-----------|-------------------|
| PD1 | Definir el umbral mínimo de frecuencia (¿2 confirmaciones?, ¿5 confirmaciones?) que dispara la sugerencia automática de un aprendizaje como Nivel C en el motor. | Al iniciar la construcción de F1 — decisión operativa del equipo de producto basada en testing con consultores reales. |
| PD2 | Definir el esquema mínimo de columnas requeridas en el archivo de carga (Excel/CSV) del PUC legacy. | Antes de codificar el parser de la fase 1. |
| PD3 | Decidir si el reporte de migración descargable es PDF, Excel o ambos. | Decisión de UX/producto durante la construcción de F1. |
| PD4 | Definir la política de anonimización de aprendizajes si se evoluciona a aprendizaje global del producto (P4). | Cuando se evalúe la evolución, no en v1.0. |
| PD5 | Definir la lista inicial de reglas precargadas con códigos finales (en este modelo se describen las 12 funcionalmente; los códigos formales se fijan al construir el catálogo). | Al construir el seed inicial de `ReglaDeRevisionPUC` en F1. |
| PD6 | Definir la lista inicial de PUCdeReferencia precargadas: ¿solo las cuatro líneas de negocio principales (construcción, inmobiliaria, concesiones, administrativa) o más variantes por sub-sector? | Al construir el seed inicial — depende del análisis de la consultora sobre qué clientes se atenderán primero. |

---

## 10. Sugerencias de implementación

#### [SI01] Concurrencia en la carga del archivo del PUC legacy

La carga del archivo (PUCLegacyImportado) y la selección del PUCdeReferencia (PUCdeReferenciaSeleccionado) son operaciones secuenciales en el flujo del consultor, pero pueden recibir comandos duplicados por reintentos de red o doble clic. Se sugiere optimistic concurrency sobre la versión del stream del `ProcesoOnboardingPUC`: cada comando incluye la versión esperada; si cambió, el comando se rechaza con error de concurrencia. Adicionalmente, se sugiere idempotency key basada en `huellaContenido` del archivo: si ya existe `PUCLegacyImportado` con la misma huella para el proceso, se ignora la nueva carga. Patrón análogo a [SI1] del sub-dominio Contabilidad.

#### [SI02] Idempotencia de PUCFinalGenerado

La generación final coordina escritura en dos bounded contexts (asistente y Contabilidad). Si el ServicioDeGeneracionPUC falla después de crear el PlanDeCuentas en Contabilidad pero antes de emitir PUCFinalGenerado, el proceso queda en LISTO_PARA_GENERAR mientras el PUC ya existe en Contabilidad. Se sugiere que el ServicioDeGeneracionPUC, al reintentar, detecte la existencia del PlanDeCuentas por la referencia del proceso (idempotencia natural por referenciaOrigen del PUC) y solo emita el evento de cierre PUCFinalGenerado. Esta lógica garantiza I5.

#### [SI03] Optimistic concurrency en las decisiones de revisión

Múltiples consultores no deberían intervenir simultáneamente el mismo proceso, pero un consultor puede tener varias pestañas abiertas o reintentar comandos. Se sugiere optimistic concurrency sobre la versión del stream `proceso-onboarding-puc-{id}` para SugerenciaAceptada, SugerenciaModificada, SugerenciaRechazada, SugerenciaAplazada y RevisionDeGrupoCompletada. Si la versión esperada cambió, el comando se rechaza y el usuario debe refrescar.

#### [SI04] Creación implícita del stream de AprendizajeOnboardingPUC

El stream `aprendizaje-onboarding-puc-{empresa}` se crea implícitamente con el primer DecisionRegistradaEnAprendizaje. No hay evento `AprendizajeCreado`. Se sugiere evaluar si esta convención es suficiente o si conviene un evento explícito de creación por consistencia con el patrón del resto de agregados. Análogo a [SI4] del sub-dominio Contabilidad para el agregado Aprendizaje.

#### [SI05] Cadena A→C→B en el ServicioDeAnalisisAutomatico

El motor de análisis aplica las tres fuentes de heurísticas con orden de precedencia (A > C > B). Cuando dos fuentes producen la misma sugerencia (mismo patrón, mismos registros afectados, misma transformación propuesta), se consolida en una sola sugerencia marcada con el origen de mayor precedencia. Cuando producen sugerencias diferentes para el mismo patrón, se priman las de Nivel A y se descartan las de Nivel C/B en conflicto. Patrón análogo a [DD2] del sub-dominio Contabilidad para la cadena de resolución de cuentas.

#### [SI06] Reconstrucción de estado al reanudar la revisión

Cuando el consultor pausa y reanuda un proceso en estado EN_REVISION, el sistema reconstruye el estado completo desde el stream: cuáles sugerencias están pendientes, cuáles aceptadas/modificadas/rechazadas/aplazadas, en qué grupo iba el consultor (último RevisionDeGrupoCompletada). Se sugiere una proyección de lectura optimizada para esta consulta para evitar el costo de reproducir el stream completo en cada reanudación.

---

## 11. Permisos atómicos

| Agregado | Acción | Quién | Permiso |
|----------|--------|-------|---------|
| ProcesoOnboardingPUC | Iniciar proceso | Consultor especializado | `iniciar_onboarding_puc` |
| ProcesoOnboardingPUC | Cargar archivo legacy | Consultor especializado | `cargar_puc_legacy` |
| ProcesoOnboardingPUC | Seleccionar referencia | Consultor especializado | `seleccionar_puc_referencia` |
| ProcesoOnboardingPUC | Decidir sugerencias (aceptar/modificar/rechazar/aplazar) | Consultor especializado | `revisar_sugerencias_onboarding_puc` |
| ProcesoOnboardingPUC | Generar PUC final | Consultor especializado + aprobación del analista contable | `generar_puc_final` |
| ProcesoOnboardingPUC | Abandonar proceso | Consultor especializado | `abandonar_onboarding_puc` |
| ProcesoOnboardingPUC | Consultar historial | Consultor, analista contable, auditor | `consultar_onboarding_puc` |
| PUCdeReferencia | Crear, modificar, inactivar | Equipo de producto | `gestionar_puc_de_referencia` |
| ReglaDeRevisionPUC | Crear, modificar, inactivar | Equipo de producto | `gestionar_regla_revision_puc` |
| AprendizajeOnboardingPUC | Consultar | Consultor, analista contable, equipo de producto | `consultar_aprendizaje_onboarding` |
| AprendizajeOnboardingPUC | Promover a regla formal | Equipo de producto | `promover_aprendizaje_a_regla` |
| AprendizajeOnboardingPUC | Invalidar | Equipo de producto | `invalidar_aprendizaje` |

**Restricción de contexto:** el acceso a procesos y a aprendizajes se restringe por empresa. Un consultor solo puede ver y operar procesos de las empresas para las que está autorizado.

---

## 12. Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Mayo 2026 | Versión inicial. 12 secciones. 4 agregados (1 transaccional con FSM, 1 transaccional sin FSM como receptor pasivo, 2 de configuración), 26 eventos (13 transaccionales del proceso + 3 del aprendizaje + 10 de configuración), 8 invariantes (6 Local + 2 Eventual), 9 decisiones (D1-D9), 5 premisas (P1-P5), 6 pendientes (PD1-PD6), 6 sugerencias de implementación (SI01-SI06), 12 permisos atómicos, 2 domain services. Caso modelado: PUC en v1.0 — primer caso del patrón transversal del Asistente de Onboarding. Inspirado en el agregado `Aprendizaje` y la cadena de resolución A→C→B del sub-dominio Contabilidad (Motor de Traducción del N1). Acompañado por `definicion-alcance.md` v1.0 y `casos/onboarding-puc.md` v1.0. |
