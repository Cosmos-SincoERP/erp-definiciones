# Modelo de Dominio — Impuestos

## Tabla de contenido

1. [Propósito y relación con otros documentos](#1-propósito-y-relación-con-otros-documentos)
2. [Convenciones del documento](#2-convenciones-del-documento)
3. [Bounded Context y Agregados](#3-bounded-context-y-agregados)
4. [Máquinas de estado](#4-máquinas-de-estado)
5. [Catálogo de eventos](#5-catálogo-de-eventos)
6. [Invariantes del dominio](#6-invariantes-del-dominio)
7. [Qué NO contiene este documento](#7-qué-no-contiene-este-documento)
8. [Decisiones de arquitectura y diseño](#8-decisiones-de-arquitectura-y-diseño)
9. [Premisas de negocio](#9-premisas-de-negocio)
10. [Pendientes por definir](#10-pendientes-por-definir)

---

## 1. Propósito y relación con otros documentos

Este documento especifica el comportamiento interno del sub-dominio de Impuestos mediante eventos, transiciones de estado, precondiciones, invariantes y read models. Su objetivo es servir como puente entre la definición funcional y la implementación técnica.

| Documento | Alcance | Relación |
|-----------|---------|----------|
| `definicion-alcance.md` | QUÉ hace el sistema | Fuente de verdad para glosario, actores, flujos y reglas (R01–R38). No se duplica aquí. |
| **Este documento** | CÓMO se comporta el sub-dominio | Eventos, transiciones, precondiciones, invariantes, read models. |
| `guias-de-modelado/` | Criterios de modelado | Guías transversales de decisión: agregados, separación de responsabilidades, arquitectura EDA. Aplican a todos los sub-dominios. |
| EventCatalog (fase 3) | Catalogación técnica | Consumirá este documento como especificación de entrada durante la implementación. |

Las reglas de negocio se referencian como `[R##]` y su texto completo vive en `definicion-alcance.md`, Sección 6.

---

## 2. Convenciones del documento

### 2.1. Nomenclatura

- **Eventos:** PascalCase en español, sin tildes ni caracteres especiales (compatibilidad con código fuente). Ej: `RegistroTributarioCreado`, `PerfilTributarioActualizado`.
- **Referencias:** `[R##]` reglas de negocio, `[P##]` premisas, `[D##]` decisiones, `[I##]` invariantes, `[SI##]` sugerencias de implementación, `[PD#]` pendientes.
- **Fase de implementación:** `[F1]` Núcleo + Soporte (implementación inmediata). `[F2]` Derivadas (fase futura). Definido en `[D7]`.
- **Agregados:** Nombres en PascalCase; corresponden a los términos del glosario canónico (`definicion-alcance.md`, Sección 2).
- **Alcance del glosario canónico:** Los domain services, entidades internas y value objects son artefactos del modelo de dominio — no requieren entrada en el glosario canónico.

### 2.2. Template de evento

Cada evento del catálogo (Sección 5) se documenta con esta estructura:

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Qué ocurrió en términos de negocio. |
| **Causalidad** | _(Solo si no es directa.)_ Derivado por transición / Derivado por configuración / Efecto inter-agregado / Evento compensatorio. Ver Causalidad entre eventos. |
| **Agregado** | Agregado que emite el evento. |
| **Estado previo** | Estado(s) desde los que puede emitirse. |
| **Estado resultante** | Estado del agregado después del evento (o "sin cambio" si es evento de progreso). |
| **Precondiciones** | Condiciones requeridas. Ref. a reglas: `[R##]`. |
| **Información capturada** | Datos de negocio que el evento registra (no campos de BD). |
| **Efectos** | Consecuencias: entidades creadas, saldos modificados, eventos derivados. |

### 2.3. Diagramas

- FSM en ASCII. Estados terminales marcados con `■`.
- Eventos de progreso (sin cambio de estado) se listan dentro del recuadro del estado.
- Eventos de transición se muestran en las flechas entre estados.

### 2.4. Causalidad entre eventos

| Tipo | Descripción | Consistencia |
|------|-------------|-------------|
| Derivado por transición | Mismo agregado, mismo append atómico. | Transaccional |
| Derivado por configuración | Mismo agregado, condicional a configuración. | Transaccional |
| Efecto inter-agregado | Domain service coordina entre agregados. | Eventual |
| Compensatorio | Revierte un efecto previo por fallo de saga. | Eventual |

---

## 3. Bounded Context y Agregados

### Clasificación de capacidades

El bounded context de Impuestos agrupa capacidades con distinto nivel de centralidad. Esta clasificación no implica separación en bounded contexts distintos — todas conviven dentro del mismo BC — pero establece una jerarquía de dependencia: las capacidades derivadas consumen el núcleo, no lo redefinen. `[D7]`

| Nivel | Capacidades | Agregados / Servicios | Fase |
|---|---|---|---|
| **Núcleo** | Configuración tributaria, determinación/cálculo, perfil tributario, registro tributario | CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales, PerfilTributario, RegistroTributario, MotorDeCalculo | `[F1]` |
| **Soporte** | Carga asistida, catálogos jurisdiccionales | CargaAsistida, CatalogoJurisdiccional | `[F1]` |
| **Derivadas** | Reportes, certificados, entregables regulatorios | HomologacionFiscal, FormatoFiscal, EntregableFiscal, CertificadoTributario | `[F2]` |

### 3.1. Impuestos como Bounded Context

```
┌──────────────────────────────────────────────────────────────────────────┐
│                     Bounded Context: Impuestos                          │
│                                                                         │
│  ── Núcleo ────────────────────────────────────────────────────────     │
│                                                                         │
│  ┌───────────────────┐  ┌───────────────────┐  ┌────────────────────┐  │
│  │ CatalogoTributario│  │ TarifaTributaria  │  │CondicionDeAplicac. │  │
│  │  (agregado)       │  │  (agregado)       │  │  (agregado)        │  │
│  └────────┬──────────┘  └────────┬──────────┘  └─────────┬──────────┘  │
│           │                      │                        │             │
│           └──────────┬───────────┘────────────────────────┘             │
│                      ▼                                                  │
│  ┌────────────────────────┐                                             │
│  │  MotorDeCalculo        │◄─── PerfilTributario                       │
│  │  (domain service)      │◄─── CatalogoJurisdiccional                 │
│  └───────────┬────────────┘                                             │
│              │                                                          │
│              ▼                                                          │
│  ┌───────────────────┐  ┌──────────────────────────┐                    │
│  │RegistroTributario │  │CatalogoDeAtributosFiscal.│                    │
│  │  (agregado, ES)   │  │  (agregado)              │                    │
│  └───────────────────┘  └────────────┬─────────────┘                    │
│                                      ▼                                  │
│                          ┌────────────────┐                             │
│                          │PerfilTributario│                             │
│                          │  (agregado)    │                             │
│                          └────────────────┘                             │
│                                                                         │
│  ── Soporte ───────────────────────────────────────────────────────     │
│                                                                         │
│  ┌ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┐    ┌ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┐               │
│    CargaAsistida           CatalogoJurisdiccional                      │
│  │ (domain service) │    │ (read model)               │               │
│  └ ─ ─ ─ ─ ─ ─ ─ ─ ┘    └ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┘               │
│                                                                         │
│  ── Derivadas (consumen el núcleo, no lo redefinen) ───────────────    │
│                                                                         │
│  ┌───────────────────┐  ┌──────────────────┐  ┌───────────────────────┐│
│  │HomologacionFiscal │  │  FormatoFiscal   │  │EntregableFiscal       ││
│  │  (agregado)       │  │  (agregado)      │  │ (agregado, ES)        ││
│  └────────┬──────────┘  └────────┬─────────┘  └───────────▲───────────┘│
│           │                      │                         │           │
│           │              ┌───────┴─────────────────────────┤           │
│           │              │                                 │           │
│           │              │  ┌──────────────────────────┐   │           │
│           │              │  │CertificadoTributario     │   │           │
│           │              │  │ (agregado, ES)           │   │           │
│           │              │  └──────────────▲───────────┘   │           │
│           └──────────────┴─────────────────┘───────────────┘           │
│                                                                         │
│  ─ ─ ─ Integraciones ─ ─ ─                                             │
│  [entrada]  Solicitud de cálculo ← sub-dominio consumidor [síncrono]   │
│  [entrada]  Confirmación de transacción ← sub-dominio consumidor [async]│
│  [entrada]  Datos de autoridad fiscal ← DIAN, DGII [CargaAsistida]    │
│  [salida]   Desglose fiscal propuesto → sub-dominio consumidor [síncrono]│
│  [salida]   Catálogo de clasificaciones → sub-dominios consumidores [query]│
│  [salida]   Certificados/reportes → terceros/autoridades [entregables] │
└──────────────────────────────────────────────────────────────────────────┘
```

**Leyenda:**
- Recuadros sólidos (`┌──┐`): agregados
- Recuadros punteados (`┌ ─ ┐`): read models y domain services sin estado
- Flechas (`▼ ◄`): dependencia de lectura o escritura
- `(ES)`: agregados con Event Sourcing
- Diseño dimensional documentado en `anexo-diseno-dimensional.md`

### 3.2. Agregado: CatalogoTributario `[F1]`

- **Raíz:** Catálogo tributario de un país. Agrupa tributos, clasificaciones y la matriz de tratamiento que determina qué tributos aplican a qué clasificaciones. No contiene tarifas ni condiciones por perfil (dimensiones independientes).
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `catalogo-tributario-{id}`
- **Eventos propios:** 9 — ver Sección 5.2.1.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `Tributo` | Carga fiscal aplicable en la jurisdicción. Cada tributo declara su `factorDeTarifa` — el tipo de dato que el motor usa para buscar la tarifa en TarifaTributaria. `[R03]` | Código, nombre, naturaleza (aditivo/sustractivo), caracterRetención (anticipado/definitivo), nivelJurisdiccional (nacional/municipal/estatal), factorDeTarifa, tributoPadre, origen. |
| `ClasificacionTributaria` | Categoría que agrupa bienes/servicios según tratamiento tributario. `[R01]` | Código, nombre, descripción, origen. |
| `Tratamiento` | Define si un tributo aplica o no a una clasificación. Tiene identidad porque puede ser sobrescrito por origen personalizado. | Tributo, clasificación, aplica (sí/no), origen. |
| `ReglaDeLocalizacion` | Define qué rol de ubicación determina la jurisdicción fiscal para un tributo en una clasificación dada. Contenido fiscal del producto. El motor la consulta para resolver cuál de las ubicaciones enviadas por el consumidor es la fiscalmente relevante. `[R34]` | Tributo, clasificación (o `*` para todas), rolQueManda (sedeEmisora / sedeContraparte / lugarEjecucion), rolFallback (opcional), origen. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `Origen` | Procedencia del contenido: `estándar` (contenido fiscal precargado, actualizable con el producto) o `personalizado` (excepción configurada por el usuario, tiene precedencia). |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  CatalogoTributario (Agregado)                               │
│                                                              │
│  pais · origen                                               │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Tributo #1 (Entidad)                                   │  │
│  │  codigo: IVA · nombre: Imp. sobre las Ventas           │  │
│  │  naturaleza: aditivo · nivelJurisdiccional: nacional   │  │
│  │  factorDeTarifa: clasificacion · origen: estándar      │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Tributo #2 (Entidad)                                   │  │
│  │  codigo: RETEFUENTE · nombre: Retención en la fuente   │  │
│  │  naturaleza: sustractivo · nivelJurisd.: nacional      │  │
│  │  factorDeTarifa: conceptoPago · origen: estándar       │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Tributo #3 (Entidad)                                   │  │
│  │  codigo: ICA · nombre: Ind. y Comercio                 │  │
│  │  naturaleza: sustractivo · nivelJurisd.: municipal     │  │
│  │  factorDeTarifa: actividadEconomica · origen: estándar │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ClasificacionTributaria #1 (Entidad)                   │  │
│  │  codigo: GRAV_19 · nombre: Gravados 19%                │  │
│  │  origen: estándar                                      │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Tratamiento (Entidad) — matriz clasificación × tributo │  │
│  │  GRAV_19 + IVA → aplica: sí · origen: estándar        │  │
│  │  GRAV_19 + RETEFUENTE → aplica: sí · origen: estándar │  │
│  │  GRAV_19 + ICA → aplica: sí · origen: estándar        │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ReglaDeLocalizacion (Entidad)                          │  │
│  │  ICA + GRAV_19 → rol: lugarEjecucion                  │  │
│  │                   fallback: sedeEmisora                │  │
│  │  IVA + * → rol: sedeEmisora (siempre país)            │  │
│  │  RETEFUENTE + * → rol: sedeEmisora (siempre país)     │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  tributosAplicablesA(clasificacion)                     │  │
│  │    → filtra tratamientos, precedencia personalizado > estándar  │  │
│  │    → ordena por dependencias de tributoPadre            │  │
│  │  clasificacionesVigentes()                              │  │
│  │    → todas las clasificaciones (estándar + personalizado)     │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `tributosAplicablesA(clasificacion)` | Filtra tratamientos donde `aplica = sí` para la clasificación dada. Si existen tratamiento estándar y tratamiento personalizado para la misma combinación, retorna el del personalizado (precedencia). Ordena respetando dependencias de tributo padre. `[R03]` `[R09]` `[R14]` |
| `clasificacionesVigentes()` | Retorna todas las clasificaciones (estándar + personalizado). Usado por sub-dominios consumidores para asignar clasificación a sus conceptos. |
| `resolverJurisdiccion(tributo, clasificacion, ubicaciones)` | Busca la `ReglaDeLocalizacion` para la combinación tributo × clasificación. Retorna la ubicación del rol indicado. Si el rol no está presente en las ubicaciones y hay fallback, usa el fallback. Si no hay fallback, rechaza indicando qué ubicación falta. `[R34]` |

**Tipos de `factorDeTarifa`:**

El atributo `factorDeTarifa` del `Tributo` determina qué dato del contexto transaccional usa el `MotorDeCalculo` para buscar la tarifa en `TarifaTributaria`. Los cinco tipos son mutuamente excluyentes — cada tributo declara exactamente uno. **Los tipos disponibles son los mismos para todos los países, pero cada catálogo nacional define qué tipo usa cada tributo** — un tributo en Colombia puede usar `conceptoPago` mientras que un tributo equivalente en otro país usa `clasificacion`.

- **clasificacion:** La tarifa depende de la clasificación tributaria asignada al bien/servicio. El motor usa el código de `ClasificacionTributaria` como factor de búsqueda. Es el tipo más común.
- **conceptoPago:** La tarifa depende del concepto de pago de la transacción (honorarios, servicios, compras, arrendamientos, etc.). El motor usa el código del concepto como factor de búsqueda.
- **actividadEconomica:** La tarifa depende de la actividad económica (código CIIU) del tercero **en una jurisdicción específica**. El motor usa el código CIIU como factor de búsqueda dentro del stream de la jurisdicción correspondiente (`tarifa-CO-BOG-ICA` ≠ `tarifa-CO-MDE-ICA`). Dos ciudades pueden asignar tarifas distintas a la misma actividad.
- **fija:** La tarifa no depende de ningún factor externo — existe una única entrada en `TarifaTributaria` por jurisdicción. El motor busca con factor vacío.
- **porcentajeDePadre:** La tarifa se calcula como un porcentaje del valor del tributo padre. El motor primero calcula el tributo padre, luego aplica la tarifa de este tributo sobre el resultado. Requiere `tributoPadre`.

**Resolución por tipo — pipeline del MotorDeCalculo:**

| Tipo | Dato de entrada (contexto) | Factor en TarifaTributaria | Ejemplo tributo | Ejemplo stream → factor → tarifa |
|---|---|---|---|---|
| `clasificacion` | Clasificación del concepto | Código de clasificación | IVA (CO), ITBIS (DO), SALES_TAX (US) | `tarifa-CO-IVA` → `GRAV_19` → 19% |
| `conceptoPago` | Concepto de pago | Código del concepto | ReteFuente (CO), RET_ISR (DO) | `tarifa-CO-RETEFUENTE` → `honorarios` → 11% |
| `actividadEconomica` | CIIU del tercero + jurisdicción | Código CIIU | ICA (CO) | `tarifa-CO-BOG-ICA` → `4711` → 11.04‰ |
| `fija` | — (ninguno) | — (entrada única) | TIMBRE (CO), CDT (DO), PROPINA (DO) | `tarifa-CO-TIMBRE` → — → 1% |
| `porcentajeDePadre` | Valor calculado del padre | — (% sobre padre) | RIVA (CO), RITBIS (DO), RICA (CO) | `tarifa-CO-BOG-RICA` → — → 15% del ICA |

**Restricciones por tipo:**

| Restricción | clasificacion | conceptoPago | actividadEconomica | fija | porcentajeDePadre |
|---|:---:|:---:|:---:|:---:|:---:|
| Requiere `tributoPadre` | — | — | — | — | Sí |
| Factor de búsqueda en TarifaTributaria | Código clasificación | Código concepto | Código CIIU | N/A (entrada única) | N/A (% sobre padre) |
| Depende de clasificación del bien/servicio | Sí | — | — | — | — |
| Depende de datos del tercero | — | — | Sí (CIIU) | — | — |
| Depende de datos de la transacción | — | Sí (concepto) | — | — | — |
| Resolución varía por jurisdicción subnacional | — | — | Sí (ciudad) | — | Sí (hereda del padre) |
| Múltiples entradas por stream | Sí (N clasificaciones) | Sí (N conceptos) | Sí (N actividades) | No (1 entrada) | No (1 entrada) |

Decisiones de diseño aplicadas: `[D1]` Raíz por país — los tributos municipales (ICA, RICA) se definen aquí con `nivelJurisdiccional: municipal`; sus tarifas por municipio viven en TarifaTributaria. `[D2]` Diseño dimensional.

### 3.3. Agregado: TarifaTributaria `[F1]`

- **Raíz:** Tabla de tarifas de un tributo específico en una jurisdicción específica. Cada instancia es un stream independiente (ej: `tarifa-CO-IVA`, `tarifa-CO-BOG-ICA`). Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `tarifa-{id}`
- **Eventos propios:** 4 — ver Sección 5.2.2.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `EntradaDeTarifa` | Una fila de la tabla: factor → tarifa. El significado del factor lo define el `factorDeTarifa` del tributo correspondiente en CatalogoTributario — este agregado solo almacena y busca por coincidencia exacta. `[R06]` `[R07]` | Factor, tarifa, tipoTarifa (porcentaje/específica), cuantíaMínima (VO, opcional), vigencia (VO), origen. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `Vigencia` | Rango temporal de validez: fechaDesde, fechaHasta. Protege `[R08]` (no solapamiento dentro del mismo factor y origen). |
| `CuantiaMínima` | Umbral por debajo del cual el tributo no aplica. Valor, unidadDeReferencia (UVT, UMA, COP, USD, etc.). Opcional — no todas las entradas tienen cuantía mínima. |
| `Origen` | Procedencia del contenido: `estándar` o `personalizado` (precedencia personalizado > estándar). |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  TarifaTributaria (Agregado)                                  │
│                                                              │
│  jurisdiccion · tributo · origen                              │
│                                                              │
│  Invariante: no solapamiento de vigencias por factor [R08]   │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ EntradaDeTarifa #1 (Entidad)                           │  │
│  │  factor: "GRAV_19" · tarifa: 19%                       │  │
│  │  tipoTarifa: porcentaje · origen: estándar             │  │
│  │  ○ Vigencia { 2017-01-01 → ∞ }                        │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ EntradaDeTarifa #2 (Entidad)                           │  │
│  │  factor: "Compras generales" · tarifa: 2.5%            │  │
│  │  tipoTarifa: porcentaje · origen: estándar             │  │
│  │  ○ Vigencia { 2026-01-01 → ∞ }                        │  │
│  │  ○ CuantiaMínima { valor: 27, unidad: "UVT" }         │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ EntradaDeTarifa #3 (Entidad)                           │  │
│  │  factor: "Compras generales" · tarifa: 3%              │  │
│  │  tipoTarifa: porcentaje · origen: personalizado              │  │
│  │  ○ Vigencia { 2027-01-01 → 2027-12-31 }               │  │
│  │  ○ CuantiaMínima { valor: 27, unidad: "UVT" }         │  │
│  │  (precedencia sobre #2 mientras vigente)               │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  tarifaVigenteA(factor, fecha)                         │  │
│  │    → busca por factor + vigencia, precedencia personalizado  │  │
│  │  validarNoSolapamiento(factor, vigencia, origen)       │  │
│  │    → precondición interna de escritura [R08]           │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `tarifaVigenteA(factor, fecha)` | Busca entradas que coincidan con el factor y cuya vigencia contenga la fecha. Si existen entrada estándar y entrada personalizada para la misma combinación, retorna la del personalizado (precedencia). `[R06]` `[R07]` |
| `validarNoSolapamiento(factor, vigencia, origen)` | Verifica que no exista otra entrada con el mismo factor y origen cuya vigencia se solape. Precondición interna de escritura. `[R08]` |

Decisiones de diseño aplicadas: `[D1]` Raíz por jurisdicción + tributo (stream key por combinación). `[D2]` Diseño dimensional.

### 3.4. Agregado: CondicionDeAplicacion `[F1]`

- **Raíz:** Conjunto de condiciones de un país que modifican la aplicación de tributos según perfiles tributarios. Cada condición evalúa atributos de la entidad fiscal emisora o contraparte y produce un efecto sobre un tributo específico. Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `condicion-{id}`
- **Eventos propios:** 4 — ver Sección 5.2.3.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `Condicion` | Regla que evalúa un atributo del perfil tributario y produce un efecto sobre un tributo. `atributoEvaluado` referencia una `DefinicionAtributo` del CatalogoDeAtributosFiscales — no es un string libre. Si existen condición estándar y condición personalizada para la misma combinación (atributo + tributo), aplica la personalizada. `[R10]` `[R11]` `[R35]` | EntidadEvaluada (emisora/contraparte), atributoEvaluado (ref. a DefinicionAtributo), valorEsperado, tributoAfectado, efecto (VO), tarifaAlternativa (si efecto = cambiarTarifa), vigencia (VO), origen. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `Vigencia` | Rango temporal de validez: fechaDesde, fechaHasta. |
| `Efecto` | Resultado de la evaluación: `noAplicar` (tributo se excluye del desglose), `cambiarTarifa` (se usa tarifaAlternativa), `reverseCharge` (la emisora autoliquida el tributo que la contraparte no cobra). |
| `Origen` | Procedencia del contenido: `estándar` o `personalizado` (precedencia personalizado > estándar). |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  CondicionDeAplicacion (Agregado)                             │
│                                                              │
│  pais · origen                                               │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Condicion #1 (Entidad)                                 │  │
│  │  entidadEvaluada: contraparte                          │  │
│  │  atributoEvaluado: esAutorretenedora                   │  │
│  │  valorEsperado: true · tributoAfectado: RETEFUENTE     │  │
│  │  ○ Efecto { noAplicar }                                │  │
│  │  ○ Vigencia { 2026-01-01 → ∞ }                        │  │
│  │  origen: estándar                                      │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Condicion #2 (Entidad)                                 │  │
│  │  entidadEvaluada: emisora                              │  │
│  │  atributoEvaluado: esGranContribuyente                 │  │
│  │  valorEsperado: true · tributoAfectado: RIVA           │  │
│  │  ○ Efecto { cambiarTarifa, alternativa: "Ag. design."} │  │
│  │  ○ Vigencia { 2026-01-01 → ∞ }                        │  │
│  │  origen: estándar                                      │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Condicion #3 (Entidad)                                 │  │
│  │  entidadEvaluada: contraparte                          │  │
│  │  atributoEvaluado: tipoTransaccion                     │  │
│  │  valorEsperado: importacionServicios                   │  │
│  │  tributoAfectado: IVA                                  │  │
│  │  ○ Efecto { reverseCharge }                            │  │
│  │  ○ Vigencia { 2026-01-01 → ∞ }                        │  │
│  │  origen: estándar                                      │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  condicionesVigentesA(fecha)                           │  │
│  │    → filtra por vigencia, precedencia personalizado > estándar  │  │
│  │  evaluar(perfilEmisora, perfilContraparte,              │  │
│  │         tributosAplicables, fecha)                      │  │
│  │    → aplica efectos sobre lista de tributos            │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `condicionesVigentesA(fecha)` | Filtra condiciones cuya vigencia contenga la fecha. Si existen condición estándar y condición personalizada para la misma combinación (atributo + tributo), retorna la del personalizado (precedencia). |
| `evaluar(perfilEmisora, perfilContraparte, tributosAplicables, fecha)` | Para cada tributo aplicable, evalúa si alguna condición vigente modifica su aplicación. Retorna la lista de tributos con los efectos aplicados. Paso 3 del pipeline del motor. `[R10]` `[R11]` `[R35]` |

Decisiones de diseño aplicadas: `[D1]` Raíz por país. `[D2]` Diseño dimensional — tercera dimensión del pipeline.

### 3.5. Agregado: CatalogoDeAtributosFiscales `[F1]`

- **Raíz:** Catálogo que define qué atributos fiscales existen para un país, con su tipo, valores válidos y vigencia de la definición misma. Es el esquema contra el cual PerfilTributario valida sus datos y CondicionDeAplicacion referencia sus evaluaciones. Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `catalogo-atributos-{id}`
- **Eventos propios:** 4 — ver Sección 5.2.4.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `DefinicionAtributo` | Metadato de un atributo fiscal: qué es, qué tipo tiene, qué valores acepta y cuándo está vigente. Cuando un atributo deja de existir en la normativa, se cierra su `VigenciaDefinicion` — los perfiles existentes conservan el valor histórico pero el motor ya no lo evalúa. | Nombre, tipo (boolean/enum/string/numerico), valoresValidos (lista, solo si tipo = enum), requerido (sí/no), vigenciaDefinicion (VO), origen. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `VigenciaDefinicion` | Rango temporal en que la definición del atributo es relevante: fechaDesde, fechaHasta. Diferente de la vigencia del *valor* en el perfil — esta es la vigencia del *atributo mismo*. Ej: "régimen simplificado" vigente 2017–2022, después dejó de existir. |
| `Origen` | Procedencia del contenido: `estándar` o `personalizado` (precedencia personalizado > estándar). |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  CatalogoDeAtributosFiscales (Agregado)                       │
│                                                              │
│  pais · origen                                               │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ DefinicionAtributo #1 (Entidad)                        │  │
│  │  nombre: regimenTributario · tipo: enum                │  │
│  │  valoresValidos: [Ordinario, Simple, Especial]         │  │
│  │  requerido: sí · origen: estándar                      │  │
│  │  ○ VigenciaDefinicion { 2023-01-01 → ∞ }              │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ DefinicionAtributo #2 (Entidad)                        │  │
│  │  nombre: esGranContribuyente · tipo: boolean           │  │
│  │  requerido: sí · origen: estándar                      │  │
│  │  ○ VigenciaDefinicion { 2020-01-01 → ∞ }              │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ DefinicionAtributo #3 (Entidad)  ← HISTÓRICO          │  │
│  │  nombre: regimenSimplificado · tipo: boolean           │  │
│  │  requerido: no · origen: estándar                      │  │
│  │  ○ VigenciaDefinicion { 2017-01-01 → 2022-12-31 }     │  │
│  │  (cerrado — ya no aplica en la normativa)              │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  definicionesVigentesA(fecha)                          │  │
│  │    → atributos cuya definición contiene la fecha       │  │
│  │  validarValor(nombre, valor, fecha)                    │  │
│  │    → tipo correcto + valor permitido + def. vigente    │  │
│  │  atributosRequeridos(fecha)                            │  │
│  │    → definiciones vigentes con requerido = sí          │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `definicionesVigentesA(fecha)` | Retorna las definiciones cuya `VigenciaDefinicion` contiene la fecha. Atributos con definición cerrada no se incluyen. |
| `validarValor(nombre, valor, fecha)` | Valida que: (1) existe una `DefinicionAtributo` vigente con ese nombre, (2) el valor es del tipo correcto, (3) si es enum, el valor está en `valoresValidos`. Usado por PerfilTributario como precondición de escritura. |
| `atributosRequeridos(fecha)` | Retorna las definiciones vigentes con `requerido = sí`. Permite identificar perfiles incompletos. |

Decisiones de diseño aplicadas: `[D1]` Raíz por país. `[D3]` Catálogo de atributos validado.

### 3.6. Agregado: PerfilTributario `[F1]`

- **Raíz:** Perfil fiscal de una entidad (empresa o tercero) **en un país específico**. Cada combinación (entidad × país) genera un perfil independiente — un tercero que opera en Colombia y República Dominicana tiene dos perfiles con identificaciones fiscales diferentes (NIT vs RNC), atributos diferentes y catálogos de validación diferentes. Contiene los atributos que el MotorDeCalculo y las CondicionDeAplicacion evalúan para determinar qué tributos aplican y con qué tratamiento. Cada atributo se valida contra el CatalogoDeAtributosFiscales del país correspondiente (tipo, valores válidos, vigencia de la definición). No es transaccional — es un atributo de la entidad que evoluciona cuando cambia su situación fiscal.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `perfil-tributario-{id}`
- **Eventos propios:** 4 — ver Sección 5.2.5.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `AtributoFiscal` | Dato fiscal individual de la entidad. `nombre` referencia una `DefinicionAtributo` del CatalogoDeAtributosFiscales — no es un string libre. `valor` se valida contra el tipo y valores permitidos de la definición. Cada atributo tiene vigencia temporal — permite registrar cambios históricos (ej: empresa que pasa de régimen simple a ordinario). | Nombre (ref. a DefinicionAtributo), valor, vigencia (VO), fuenteDeAutoridad (VO, opcional). |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `Vigencia` | Rango temporal de validez: fechaDesde, fechaHasta. |
| `FuenteDeAutoridad` | Referencia al documento que respalda el atributo: tipo (RUT, resolución DIAN, certificado DGII), número, fechaEmisión. Opcional — algunos atributos no requieren soporte documental. |
| `IdentificacionFiscal` | NIT/RNC/EIN de la entidad, tipo de documento, país de emisión. Inmutable — identifica a la entidad ante la autoridad fiscal. |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  PerfilTributario (Agregado)                                  │
│                                                              │
│  ○ IdentificacionFiscal { NIT: 900.123.456-7, CO }          │
│  tipoEntidad: empresa | tercero                              │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ AtributoFiscal #1 (Entidad)                            │  │
│  │  nombre: regimenTributario · valor: "Ordinario"        │  │
│  │  ○ Vigencia { 2020-01-01 → ∞ }                        │  │
│  │  ○ FuenteDeAutoridad { RUT, 2020-01-15 }              │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ AtributoFiscal #2 (Entidad)                            │  │
│  │  nombre: esGranContribuyente · valor: true             │  │
│  │  ○ Vigencia { 2022-03-01 → ∞ }                        │  │
│  │  ○ FuenteDeAutoridad { Resolución DIAN 001, 2022-02 } │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ AtributoFiscal #3 (Entidad)                            │  │
│  │  nombre: esAutorretenedora · valor: false              │  │
│  │  ○ Vigencia { 2020-01-01 → ∞ }                        │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ AtributoFiscal #4 (Entidad)                            │  │
│  │  nombre: actividadEconomica · valor: "4711"            │  │
│  │  ○ Vigencia { 2020-01-01 → ∞ }                        │  │
│  │  ○ FuenteDeAutoridad { RUT, 2020-01-15 }              │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  atributoVigenteA(nombre, fecha)                       │  │
│  │    → último valor vigente del atributo a esa fecha     │  │
│  │  perfilCompletoA(fecha)                                │  │
│  │    → mapa { nombre → valor } de todos los atributos    │  │
│  │      vigentes a la fecha                               │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `atributoVigenteA(nombre, fecha)` | Retorna el valor vigente del atributo a la fecha indicada. Si el atributo tiene múltiples vigencias, retorna la que contiene la fecha. |
| `perfilCompletoA(fecha)` | Retorna un mapa `{ nombre → valor }` de todos los atributos vigentes a la fecha. Usado por el MotorDeCalculo y por CondicionDeAplicacion para evaluar condiciones. `[R10]` `[R11]` |

Decisiones de diseño aplicadas: `[D3]` Catálogo de atributos validado.

### 3.7. Agregado: RegistroTributario (ES) `[F1]`

- **Raíz:** Hecho fiscal inmutable que representa el resultado tributario de una transacción confirmada. Nace con un único evento de creación y contiene: el desglose confirmado por el consumidor, el contexto transaccional completo (entidades fiscales, jurisdicción, efecto fiscal) y, si hubo intervención manual, el cálculo de referencia (tributos propuestos y descartados con motivo) para auditoría. En gravámenes, la referencia es el resultado del motor. En desgravámenes, es el prorrateo del desglose confirmado del registro origen (resuelto internamente por `transaccionOrigenId`). `[R22]` `[R23]` `[R24]`
- **Ciclo de vida:** Sin FSM — nace como hecho fiscal confirmado con un único evento de creación.
- **Stream de eventos:** `registro-tributario-{guid}` (ES)
- **Eventos propios:** 1 — ver Sección 5.3.1.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `LineaDeDesglose` | Cálculo fiscal individual de un tributo aplicado a un concepto. Inmutable — es el resultado del desglose confirmado. Una por cada tributo que aplicó a cada concepto. Granularidad por concepto: se puede agregar para reportes pero no desagregar. `[R16]` | Tributo (código, nombre), naturaleza (aditivo/sustractivo), baseGravable, tarifa, tipoTarifa (porcentaje/específica), valor calculado, factorUtilizado, conceptoOrigen (ref. al concepto del sub-dominio consumidor). |
| `LineaDesgloseMotor` | Cálculo de referencia al momento de la confirmación. En gravámenes: resultado del motor. En desgravámenes: prorrateo del desglose confirmado del registro origen. Estructura idéntica a `LineaDeDesglose`. Solo presente si hubo intervención manual — cuando no hay intervención, el desglose confirmado coincide con la referencia y no se duplica. `[R24]` | Mismos atributos que LineaDeDesglose. |
| `LineaDescartada` | Tributo evaluado por el motor pero excluido del desglose. Solo presente en el cálculo original del motor. Permite auditar por qué un tributo no aplicó y detectar si el usuario lo incluyó manualmente. `[R19]` | Tributo (código, nombre), naturaleza (aditivo/sustractivo), baseGravable, tarifa, tipoTarifa, valor calculado, factorUtilizado, conceptoOrigen, motivoExclusion (cuantia_minima / perfil_no_aplica / clasificacion_excluida / jurisdiccion_no_aplica / dependencia_padre). |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `ContextoTransaccional` | Referencia a la transacción: sub-dominio (OXP/CXC), ID de transacción, dirección fiscal (gasto/ingreso), efecto fiscal (gravamen/desgravamen). Si es desgravamen: `transaccionOrigenId` (ID de la transacción del gravamen original en el consumidor — Impuestos resuelve internamente el RegistroTributario origen). Inmutable. |
| `EntidadFiscalEmisora` | Snapshot de la entidad que origina el hecho económico al momento del cálculo: identificación fiscal, perfil tributario vigente. Inmutable. |
| `EntidadFiscalContraparte` | Snapshot de la contraparte al momento del cálculo: identificación fiscal, perfil tributario vigente. Inmutable. |
| `Jurisdiccion` | Jurisdicción resuelta para el cálculo: país, jurisdicción subnacional (si aplica). Inmutable. Resultado de aplicar la `ReglaDeLocalizacion` sobre las ubicaciones enviadas por el consumidor. `[D8]` |
| `IntervencionManual` | Indica si el desglose confirmado diverge del cálculo de referencia. En gravámenes, el cálculo de referencia es el resultado del motor. En desgravámenes, es el prorrateo del desglose confirmado del registro origen. `huboIntervencion` (boolean, derivable comparando ambos conjuntos). Cuando es true, el registro incluye `LineaDesgloseMotor` y `LineaDescartada` para auditoría. |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  RegistroTributario (Agregado, ES)                            │
│                                                              │
│  (sin FSM — nace como hecho fiscal confirmado)               │
│                                                              │
│  ○ ContextoTransaccional { OXP, Comercio, oxp-123, gasto }  │
│  ○ EntidadFiscalEmisora { NIT 900.123, perfil snapshot }     │
│  ○ EntidadFiscalContraparte { NIT 800.456, perfil snapshot } │
│  ○ Jurisdiccion { CO, BOG }                                  │
│  ○ IntervencionManual { no }                                 │
│                                                              │
│  ── Desglose confirmado ─────────────────────────────────── │
│                                                              │
│  ── Concepto GASTO-001 (Servicios de consultoría, $1.000k) ─│
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ LineaDeDesglose #1 (Entidad)                           │  │
│  │  tributo: IVA · naturaleza: aditivo                    │  │
│  │  baseGravable: $1.000k · tarifa: 19% · valor: $190k    │  │
│  │  factorUtilizado: "GRAV_19" · concepto: GASTO-001     │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ LineaDeDesglose #2 (Entidad)                           │  │
│  │  tributo: RETEFUENTE · naturaleza: sustractivo         │  │
│  │  baseGravable: $1.000k · tarifa: 6% · valor: $60k      │  │
│  │  factorUtilizado: "Consultoría" · concepto: GASTO-001 │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ LineaDeDesglose #3 (Entidad)                           │  │
│  │  tributo: ICA · naturaleza: sustractivo                │  │
│  │  baseGravable: $1.000k · tarifa: 11.04‰ · valor: $11k  │  │
│  │  factorUtilizado: "4711" · concepto: GASTO-001         │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ── Concepto GASTO-002 (Papelería, $400k) ──────────────── │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ LineaDeDesglose #4 (Entidad)                           │  │
│  │  tributo: IVA · naturaleza: aditivo                    │  │
│  │  baseGravable: $400k · tarifa: 19% · valor: $76k       │  │
│  │  factorUtilizado: "GRAV_19" · concepto: GASTO-002     │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ LineaDeDesglose #5 (Entidad)                           │  │
│  │  tributo: RETEFUENTE · naturaleza: sustractivo         │  │
│  │  baseGravable: $400k · tarifa: 2.5% · valor: $10k      │  │
│  │  factorUtilizado: "Compras generales" · concepto: #2   │  │
│  └────────────────────────────────────────────────────────┘  │
│  (ICA no aplica — clasificación EXCLUIDO para ICA)          │
│                                                              │
│  ── Cálculo de referencia (si hubo intervención) ────────── │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ LineaDesgloseMotor (misma estructura que DeDesglose,   │  │
│  │ solo presente si huboIntervencion = true)              │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ LineaDescartada #1 (Entidad)                           │  │
│  │  tributo: ICA · naturaleza: sustractivo                │  │
│  │  baseGravable: $1.000k · tarifa: 11.04‰ · valor: $11k  │  │
│  │  motivoExclusion: cuantia_minima                       │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  totalImpuestos()                                      │  │
│  │    → sum(líneas aditivas) = $190k + $76k = $266k      │  │
│  │  totalRetenciones()                                    │  │
│  │    → sum(líneas sustractivas) = $60k+$11k+$10k = $81k │  │
│  │  valorNeto(baseTransaccion: $1.400k)                   │  │
│  │    → $1.400k + $266k - $81k = $1.585k                 │  │
│  │  fueIntervenido() → IntervencionManual.huboIntervencion│  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `totalImpuestos()` | Suma del valor de todas las `LineaDeDesglose` con naturaleza aditiva. |
| `totalRetenciones()` | Suma del valor de todas las `LineaDeDesglose` con naturaleza sustractiva. |
| `valorNeto(baseTransaccion)` | `baseTransaccion` + `totalImpuestos()` - `totalRetenciones()`. La base la provee el contexto transaccional. |
| `fueIntervenido()` | Indica si el desglose confirmado diverge del cálculo de referencia (motor en gravámenes, prorrateo del origen en desgravámenes). Derivado de `IntervencionManual`. |
| `crear(contexto, desgloseConfirmado, calculoDeReferencia)` | Factory method. Crea el registro comparando `desgloseConfirmado` con `calculoDeReferencia`. El cálculo de referencia depende del `efectoFiscal`: para gravámenes es `resultadoMotor.aplicados`; para desgravámenes es el prorrateo del desglose confirmado del registro origen. Si divergen → `huboIntervencion = true` y persiste ambos conjuntos. Si coinciden → solo el desglose confirmado. Emite `RegistroTributarioCreado`. `[R22]` `[R24]` |

Decisiones de diseño aplicadas: `[D4]` Registro tributario como hecho inmutable.

Ejemplo completo de almacenamiento (stream de eventos, gravamen, desgravamen y efecto fiscal neto) en `anexo-ejemplo-registro-tributario.md`.

### 3.8. Agregado: HomologacionFiscal `[F2]`

- **Raíz:** Tabla de equivalencias entre los valores internos del sub-dominio (factorUtilizado, clasificación tributaria) y los códigos que exige una autoridad fiscal en sus reportes. Cada instancia cubre una autoridad específica. Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `homologacion-fiscal-{id}`
- **Eventos propios:** 4 — ver Sección 5.2.6.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `Equivalencia` | Mapeo individual: un valor interno del sub-dominio se traduce a un código de la autoridad. La combinación (valorInterno + tributo) es única dentro de la homologación. | ValorInterno (factorUtilizado o código de clasificación), tributo, codigoAutoridad, nombreAutoridad (descripción legible), vigencia (VO), origen. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `AutoridadFiscal` | Autoridad destinataria: nombre (DIAN, DGII, Secretaría de Hacienda), jurisdicción, país. |
| `Vigencia` | Rango temporal de validez: fechaDesde, fechaHasta. Los códigos cambian cuando la autoridad modifica sus formatos. |
| `Origen` | Procedencia del contenido: `estándar` o `personalizado` (precedencia personalizado > estándar). |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  HomologacionFiscal (Agregado)                                │
│                                                              │
│  ○ AutoridadFiscal { DIAN, CO }                              │
│  origen: estándar                                            │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Equivalencia #1 (Entidad)                              │  │
│  │  valorInterno: "Consultoría" · tributo: RETEFUENTE     │  │
│  │  codigoAutoridad: "5002" · nombre: "Servicios"         │  │
│  │  ○ Vigencia { 2026-01-01 → ∞ } · origen: estándar     │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Equivalencia #2 (Entidad)                              │  │
│  │  valorInterno: "Compras generales" · tributo: RETEFUENTE│  │
│  │  codigoAutoridad: "5001" · nombre: "Compras"           │  │
│  │  ○ Vigencia { 2026-01-01 → ∞ } · origen: estándar     │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Equivalencia #3 (Entidad)                              │  │
│  │  valorInterno: "GRAV_19" · tributo: IVA                │  │
│  │  codigoAutoridad: "01" · nombre: "Gravado 19%"         │  │
│  │  ○ Vigencia { 2026-01-01 → ∞ } · origen: estándar     │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  homologar(valorInterno, tributo, fecha)                │  │
│  │    → busca equivalencia vigente, retorna código         │  │
│  │  equivalenciasVigentesA(fecha)                         │  │
│  │    → todas las equivalencias vigentes a la fecha        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `homologar(valorInterno, tributo, fecha)` | Busca la equivalencia vigente para la combinación (valorInterno + tributo) a la fecha dada. Retorna el código de la autoridad. Si existen equivalencia estándar y personalizada, retorna la personalizada. |
| `equivalenciasVigentesA(fecha)` | Retorna todas las equivalencias cuya vigencia contiene la fecha. Usado por EntregableFiscal durante la generación. |

Decisiones de diseño aplicadas: `[D1]` Raíz por autoridad fiscal. `[D6]` Homologación fiscal como dimensión independiente.

### 3.9. Agregado: FormatoFiscal `[F2]`

- **Raíz:** Definición de un formato de entregable fiscal exigido por una autoridad. Describe qué tipo de entregable es, con qué periodicidad, en qué formato(s) de salida y qué estructura de datos requiere. Referencia la `HomologacionFiscal` de su autoridad para traducir valores internos a códigos del reporte. Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `formato-fiscal-{id}`
- **Eventos propios:** 5 — ver Sección 5.2.7.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `SeccionFormato` | Bloque lógico del entregable que agrupa campos relacionados. Un formato puede tener múltiples secciones. Cada sección define qué datos del `RegistroTributario` consume, cómo los agrupa y qué homologación aplica. `[R26]` `[R27]` | Nombre, descripción, criterioDeAgrupacion (por tercero, por tributo, por concepto, por jurisdicción), criterioDeSeleccion (qué registros tributarios incluir), orden. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `AutoridadFiscal` | Autoridad destinataria: nombre, jurisdicción, país. |
| `Periodicidad` | Frecuencia de generación: tipo (mensual/bimestral/trimestral/anual), meses aplicables. `[R29]` |
| `FormatoDeSalida` | Formato técnico del archivo: tipo (XML/Excel/PDF), esquema o plantilla de referencia. Un formato puede tener múltiples salidas (ej: XML para DIAN + Excel para prevalidador). `[R27]` |
| `Vigencia` | Rango temporal de validez: fechaDesde, fechaHasta. |
| `Origen` | Procedencia del contenido: `estándar` o `personalizado` (precedencia personalizado > estándar). |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  FormatoFiscal (Agregado)                                     │
│                                                              │
│  tipoEntregable: reporte | certificado                       │
│  homologacion: ref. HomologacionFiscal { DIAN, CO }          │
│  ○ AutoridadFiscal { DIAN, CO }                              │
│  ○ Periodicidad { anual, [1-12] }                            │
│  ○ FormatoDeSalida { XML, esquema: "formato-1001-v3" }       │
│  ○ FormatoDeSalida { Excel, plantilla: "prevalidador-1001" } │
│  ○ Vigencia { 2026-01-01 → ∞ }                              │
│  origen: estándar                                            │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ SeccionFormato #1 (Entidad)                            │  │
│  │  nombre: "Pagos o abonos en cuenta"                    │  │
│  │  criterioDeAgrupacion: por tercero                     │  │
│  │  criterioDeSeleccion: registros con RETEFUENTE         │  │
│  │  orden: 1                                              │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ SeccionFormato #2 (Entidad)                            │  │
│  │  nombre: "Retenciones practicadas"                     │  │
│  │  criterioDeAgrupacion: por tercero × tributo           │  │
│  │  criterioDeSeleccion: registros con retenciones        │  │
│  │  orden: 2                                              │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  esVigenteA(fecha) → vigencia contiene la fecha        │  │
│  │  formatosDeSalida() → lista de formatos disponibles    │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `esVigenteA(fecha)` | Verifica que la vigencia del formato contenga la fecha. Formatos vencidos no se usan para nuevas generaciones pero se conservan para consulta histórica. |
| `formatosDeSalida()` | Retorna la lista de formatos de salida disponibles. Un entregable puede generarse en múltiples formatos simultáneamente. `[R27]` |

Decisiones de diseño aplicadas: `[D6]` Homologación fiscal como dimensión independiente.

### 3.10. Agregado: EntregableFiscal (ES) `[F2]`

- **Raíz:** Instancia concreta de un reporte fiscal generado para un período, autoridad y tipo específicos. Representa la ejecución de un `FormatoFiscal` sobre los `RegistroTributario` confirmados del período, traducidos mediante `HomologacionFiscal`. Es un documento compuesto — un solo archivo que contiene datos de múltiples terceros y registros. Cada generación crea un nuevo stream. No incluye certificados (propio agregado `CertificadoTributario`, 3.11) ni declaraciones tributarias (ver `[PD4]`). `[R26]` `[R27]`
- **Ciclo de vida:** FSM transaccional — Borrador → Generado → Presentado. Permite regeneración (vuelve a Borrador).
- **Stream de eventos:** `entregable-fiscal-{guid}`
- **Eventos propios:** 4 — ver Sección 5.3.2.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `ContenidoGenerado` | Resultado de aplicar el formato al conjunto de registros tributarios del período. Contiene las filas de datos ya homologadas con códigos de la autoridad. Se reemplaza completamente en cada regeneración. | Filas (lista de datos estructurados según `SeccionFormato`), totalRegistrosIncluidos, fechaGeneracion. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `AutoridadFiscal` | Autoridad destinataria del entregable: nombre, jurisdicción, país. |
| `PeriodoFiscal` | Período que cubre el entregable: año, mes (o rango según periodicidad). `[R29]` |
| `ReferenciaFormato` | Referencia al `FormatoFiscal` usado para la generación: ID, versión. |
| `ReferenciaHomologacion` | Referencia a la `HomologacionFiscal` usada para traducir valores internos a códigos de la autoridad. |
| `ArchivoGenerado` | Archivo de salida producido: tipo (XML/Excel/PDF), referencia de almacenamiento, hash de integridad. Un entregable puede tener múltiples archivos (ej: XML + Excel prevalidador). `[R27]` |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  EntregableFiscal (Agregado, ES)                              │
│                                                              │
│  estado: Borrador | Generado | Presentado                    │
│  tipoEntregable: reporte                                     │
│  ○ AutoridadFiscal { DIAN, CO }                              │
│                                                              │
│  ○ PeriodoFiscal { 2026, enero-diciembre }                   │
│  ○ ReferenciaFormato { formato-1001-v3 }                     │
│  ○ ReferenciaHomologacion { homologacion-DIAN }              │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ContenidoGenerado (Entidad)                            │  │
│  │  filas: [ { tercero: 800.456, concepto: "5002",        │  │
│  │            baseGravable: $1.000k, retencion: $60k } ]  │  │
│  │  totalRegistrosIncluidos: 342                          │  │
│  │  fechaGeneracion: 2027-03-15T10:00:00                  │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ ArchivoGenerado { XML, ref: "s3://...", hash: "a1b2" }   │
│  ○ ArchivoGenerado { Excel, ref: "s3://...", hash: "c3d4" } │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  puedeGenerarContenido() → estado ∈ {Borrador, Generado}       │  │
│  │  esPresentable() → estado = Generado                   │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `puedeGenerarContenido()` | Indica si se puede generar (primera vez desde Borrador) o regenerar (desde Generado) el contenido del entregable. Si ya fue presentado, no se puede — se crea uno nuevo. |
| `esPresentable()` | Solo entregables en estado Generado pueden marcarse como presentados ante la autoridad. |

Decisiones de diseño aplicadas: `[D4]` Registros tributarios como fuente. `[D6]` Homologación fiscal como dimensión independiente.

### 3.11. Agregado: CertificadoTributario (ES) `[F2]`

- **Raíz:** Certificado tributario individual emitido para un tercero específico en un período fiscal. Cada certificado tiene su propio ciclo de vida: se genera, se envía y se entrega de forma independiente. La generación masiva ("todos los certificados del 2025") es un proceso de aplicación que crea N instancias de este agregado — la agrupación por período es informativa (read model), no un agregado. `[R28]` `[R37]`
- **Ciclo de vida:** FSM transaccional — Borrador → Generado → Entregado. Soporta fallo de envío con reintento. Permite regeneración (vuelve a Borrador).
- **Stream de eventos:** `certificado-tributario-{guid}`
- **Eventos propios:** 6 — ver Sección 5.3.3.

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `Destinatario` | Tercero al que se emite el certificado: identificación fiscal (tipo y número), razón social, datos de contacto para entrega (correo, etc.). |
| `PeriodoFiscal` | Período que cubre el certificado: año, mes (o rango según periodicidad). `[R29]` |
| `ReferenciaFormato` | Referencia al `FormatoFiscal` usado para la generación: ID, versión. |
| `ReferenciaHomologacion` | Referencia a la `HomologacionFiscal` usada para traducir valores internos a códigos de la autoridad. |
| `ArchivoGenerado` | Archivo de salida producido: tipo (PDF), referencia de almacenamiento, hash de integridad. |
| `AutoridadFiscal` | Autoridad fiscal bajo cuya normativa se emite el certificado. |
| `ResultadoEnvio` | Resultado del último intento de envío: canal (correo/portal), fecha, exitoso (boolean), detalle de fallo (si aplica). |

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `ContenidoCertificado` | Contenido del certificado para el tercero: resumen de retenciones practicadas por tributo, bases gravables, tarifas aplicadas y valores retenidos en el período. Se genera a partir de los `RegistroTributario` del período para ese tercero, traducido mediante `HomologacionFiscal`. Se reemplaza completamente en cada regeneración. | Líneas de detalle (tributo, baseGravable, tarifa, valor retenido), totales por tributo, fechaGeneracion. |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  CertificadoTributario (Agregado, ES)                        │
│                                                              │
│  estado: Borrador | Generado | Entregado | Fallido           │
│  ○ AutoridadFiscal { DIAN, CO }                              │
│                                                              │
│  ○ Destinatario { NIT 800.456, "Empresa ABC",               │
│                    correo: "fiscal@abc.com" }                │
│  ○ PeriodoFiscal { 2026, enero-diciembre }                   │
│  ○ ReferenciaFormato { formato-cert-retefuente-v1 }          │
│  ○ ReferenciaHomologacion { homologacion-DIAN }              │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ContenidoCertificado (Entidad)                         │  │
│  │  líneas: [ { tributo: RETEFUENTE, concepto: "5002",    │  │
│  │             baseGravable: $12.000k, tarifa: 6%,          │  │
│  │             valor: $720k },                              │  │
│  │           { tributo: RIVA, baseGravable: $2.280k,       │  │
│  │             tarifa: 15%, valor: $342k } ]               │  │
│  │  totales: { RETEFUENTE: $720k, RIVA: $342k }           │  │
│  │  fechaGeneracion: 2027-03-15T10:00:00                  │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ ArchivoGenerado { PDF, ref: "s3://...", hash: "e5f6" }   │
│  ○ ResultadoEnvio { correo, 2027-03-16, exitoso: true }     │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  puedeGenerarContenido() → estado ∈ {Borrador, Generado}       │  │
│  │  esEnviable()    → estado = Generado                   │  │
│  │  esReenviable()  → estado = Fallido                    │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `puedeGenerarContenido()` | Indica si se puede generar (primera vez desde Borrador) o regenerar (desde Generado) el contenido del certificado. Si ya fue entregado, no se puede — se crea uno nuevo. |
| `esEnviable()` | Solo certificados en estado Generado pueden solicitar envío a la infraestructura. `[R28]` |
| `esReenviable()` | Solo certificados en estado Fallido pueden reintentarse. El reintento vuelve a Generado y solicita nuevo envío. |

Decisiones de diseño aplicadas: `[D4]` Registros tributarios como fuente. `[D6]` Homologación fiscal como dimensión independiente.

### 3.12. Servicio de dominio: MotorDeCalculo `[F1]`

**Naturaleza:** Domain service stateless de nivel Núcleo `[D7]`. Responsabilidad única: resolver el desglose tributario a partir del contexto de una transacción. No persiste, no crea registros, no distingue quién lo invoca ni para qué. La misma función se usa tanto para simulación como para confirmación. `[D5]`

**Operación:** `calcular(contexto) → ResultadoCalculo`

**Entrada (contrato `[D9]`):**

| Campo | Tipo | Obligatorio |
|---|---|---|
| `direccionFiscal` | gasto \| ingreso | Sí |
| `entidadFiscalEmisora` | { tipoId, numeroId, pais } | Sí |
| `entidadFiscalContraparte` | { tipoId, numeroId, pais } | Sí |
| `ubicaciones` | { sedeEmisora, sedeContraparte, lugarEjecucion? } | Sí (según `ReglaDeLocalizacion`) |
| `fechaTransaccion` | date | Sí |
| `moneda` | código ISO | Sí |
| `tipoCambioReferencia` | decimal | Solo si moneda ≠ moneda jurisdicción |
| `conceptos[]` | { id, clasificacionTributaria, monto, conceptoPago? } | Sí (mínimo 1) |

> **Responsabilidad del consumidor sobre datos fiscales del concepto:** El sub-dominio consumidor es responsable de asignar a cada uno de sus bienes o servicios la `clasificacionTributaria` usando el catálogo provisto por Impuestos — es siempre obligatoria. El campo `conceptoPago` es opcional en el contrato: el consumidor lo envía cuando el bien/servicio participa en tributos que lo requieren. La obligatoriedad real la dicta la configuración fiscal del país — si un tributo candidato usa `factorDeTarifa: conceptoPago` (ej: ReteFuente en Colombia) y el dato no viene en la solicitud, el motor rechaza el concepto indicando el dato faltante `[R30]`. Los demás factores de tarifa se resuelven internamente: `clasificacion` desde la propia solicitud, `actividadEconomica` desde el PerfilTributario del tercero, `fija` sin dato externo, `porcentajeDePadre` desde el cálculo del tributo padre. Patrón convergente: SAP (WHT Code opt-in por proveedor), Oracle Fusion (clasificación fiscal condicional), Dynamics 365 (Item WHT Group opcional), Vertex (categoría requerida solo para ítems en scope).

**Flujo interno:**

1. Resuelve perfiles tributarios: lee `PerfilTributario` de emisora y contraparte → `perfilCompletoA(fechaTransaccion)`. Si no existe perfil → rechaza `[R31]`
2. Para cada concepto:
   - a. Lee `CatalogoTributario` → `tributosAplicablesA(clasificacion)`. Si clasificación no existe o no vigente → rechaza concepto `[R32]`
   - b. Para cada tributo candidato, aplica `ReglaDeLocalizacion` → `resolverJurisdiccion(ubicaciones)` `[D8]`. Si ubicación obligatoria falta → rechaza `[R30]`
   - c. Evalúa `CondicionDeAplicacion` con perfiles de ambas entidades → determina efecto (aplica / excluye / modifica tarifa) `[R09]` `[R10]`
   - d. Si aplica: lee `TarifaTributaria` → tarifa vigente a `fechaTransaccion`, usando el `factorDeTarifa` del tributo. Resolución por tipo: `clasificacion` desde la solicitud, `conceptoPago` desde la solicitud (condicional), `actividadEconomica` desde el PerfilTributario del tercero + jurisdicción resuelta, `fija` sin factor externo, `porcentajeDePadre` desde el cálculo del tributo padre. Si el factor requerido no está disponible → rechaza el concepto indicando el dato faltante `[R30]` `[R07]`
   - e. Evalúa cuantía mínima — si baseGravable < umbral → descarta con motivo `cuantia_minima` `[R13]`
   - f. Evalúa dependencia de tributo padre — si padre no existe en el resultado → descarta con motivo `dependencia_padre` `[R14]`
   - g. Calcula: baseGravable × tarifa = valor `[R15]` `[R16]`

**Salida (`ResultadoCalculo`):**

| Campo | Descripción |
|---|---|
| `aplicados[]` | Tributos que el motor determinó que aplican. Cada uno con: tributo, naturaleza, baseGravable, tarifa, tipoTarifa, valor, factorUtilizado, conceptoOrigen. |
| `descartados[]` | Tributos evaluados pero excluidos. Misma estructura + `motivoExclusion` (cuantia_minima / perfil_no_aplica / clasificacion_excluida / jurisdiccion_no_aplica / dependencia_padre). `[R19]` |
| `jurisdiccionResuelta` | Jurisdicción determinada por las reglas de localización. `[D8]` |
| `perfilesUtilizados` | Snapshot de los perfiles de ambas entidades al momento del cálculo. |

**Agregados que lee:** CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales, PerfilTributario.

**Agregados que escribe:** Ninguno.

**Diagrama de dependencias:**

```
                     ┌───────────────────┐
                     │  MotorDeCalculo    │
                     │  (domain service)  │
                     │                   │
                     │  calcular(ctx)    │
                     │    → Resultado    │
                     └──────┬────────────┘
                            │ lee
        ┌───────────┬───────┼──────────┬──────────────┐
        ▼           ▼       ▼          ▼              ▼
  Catalogo    Tarifa    Condicion   Catalogo    Perfil
  Tributario  Tributaria DeAplic.   Atributos   Tributario
```

Decisiones de diseño aplicadas: `[D5]` Motor stateless con evaluación completa. `[D8]` Resolución de jurisdicción. `[D9]` Contrato semántico mínimo.

### 3.13. Flujo orquestado: ConfirmacionTributaria `[F1]`

**Naturaleza:** Flujo de aplicación que orquesta la creación de un `RegistroTributario` cuando un consumidor confirma una transacción. Coordina el `MotorDeCalculo` con el agregado `RegistroTributario`. `[R22]`

> **Nota de implementación:** Este flujo pertenece a la capa de aplicación — no es un domain service. Se documenta aquí para que el equipo tenga la visión completa del proceso de confirmación en un solo lugar.

**Trigger:** Comando asíncrono de confirmación desde un sub-dominio consumidor.

**Precondiciones:**

| # | Precondición | Capa |
|---|---|---|
| 1 | El sub-dominio que envía el comando está autorizado para crear hechos fiscales (ej: OXP, CXC). Los sub-dominios que solo simulan (cotizaciones, presupuestos) no tienen acceso a este comando. | Frontera del BC (aplicación) |
| 2 | El comando cumple el contrato de confirmación `[D9]`: contexto completo + desglose confirmado. | Frontera del BC (validación) |

**Flujo:**

1. Valida que el consumidor esté autorizado para confirmar (precondición 1)
2. Valida estructura del comando (precondición 2)
3. Determina el cálculo de referencia según el `efectoFiscal`:
   - **Gravamen:** Invoca `MotorDeCalculo.calcular(contextoDelComando)` → obtiene `ResultadoCalculo` como referencia.
   - **Desgravamen:** Resuelve el RegistroTributario origen buscando por `transaccionId = transaccionOrigenId` con `efectoFiscal = gravamen`. Si no existe → rechaza la confirmación indicando que el registro origen no fue encontrado. Toma el desglose confirmado del origen y lo prorratea proporcionalmente a los montos del desgravamen → obtiene `ProrrateoOrigen` como referencia. El motor no participa.
4. Invoca `RegistroTributario.crear(contexto, desgloseConfirmado, calculoDeReferencia)` → el agregado compara, determina intervención y emite `RegistroTributarioCreado`
5. El evento se persiste en stream `registro-tributario-{guid}`

**Agregados involucrados:** MotorDeCalculo (lectura, solo gravámenes), RegistroTributario (lectura del origen en desgravámenes + escritura).

> **Nota:** El flujo de confirmación se bifurca internamente según el `efectoFiscal`. Para **gravámenes**, el motor calcula y se compara con el desglose confirmado. Para **desgravámenes**, el sistema carga el RegistroTributario del gravamen original (resuelto por `transaccionOrigenId`), prorratea su desglose confirmado a los montos del desgravamen, y usa ese prorrateo como referencia — el motor no participa. En ambos casos el usuario puede intervenir sobre la propuesta y los montos son siempre positivos. Las proyecciones interpretan el `efectoFiscal` para determinar el signo al sumar.

### 3.14. Servicio de dominio: CargaAsistida `[F1]`

**Naturaleza:** Domain service de nivel Soporte `[D7]`. Facilita la construcción del perfil tributario de una entidad fiscal validando datos provenientes de diversas fuentes contra el catálogo de atributos fiscales del país. `[R34]`

El servicio no conoce el origen de los datos — recibe atributos normalizados. La diversidad de fuentes es un problema de infraestructura (adaptadores), no de dominio:

| Canal | Ejemplo | Quién lo activa |
|---|---|---|
| **API de autoridad fiscal** | DIAN (CO), DGII (DO) | Sistema — consulta automática por identificación fiscal |
| **Registro manual** | Formulario donde el usuario digita los atributos | Administrador fiscal — cuando no hay fuente automatizada |
| **Documento (OCR)** | RUT en PDF, certificado de inscripción | Sistema — extrae datos del documento y los normaliza |

> **Nota de implementación:** Cada canal es un adaptador de infraestructura (anti-corruption layer) que normaliza su entrada al formato que el servicio espera. El dominio no conoce la fuente — recibe atributos normalizados y los valida contra el catálogo.

**Operación:** `validarYPrepararCarga(atributosNormalizados, pais, fuenteOrigen) → ResultadoCarga`

**Flujo:**

1. Recibe atributos normalizados desde cualquier canal (API, formulario, OCR) + país + identificación de la fuente
2. Lee `CatalogoDeAtributosFiscales` del país → valida que cada atributo corresponda a una definición vigente del catálogo (tipo, valores válidos, vigencia)
3. Clasifica: atributos válidos vs. atributos descartados (sin definición vigente o valor fuera de rango)
4. Retorna `ResultadoCarga` para aprobación del administrador fiscal

**Salida (`ResultadoCarga`):**

| Campo | Descripción |
|---|---|
| `atributosValidos[]` | Atributos que pasaron validación contra el catálogo: nombre, valor, definición de referencia. |
| `atributosDescartados[]` | Atributos que no tienen definición vigente o cuyo valor no es válido, con motivo. |
| `fuenteOrigen` | Canal utilizado (api / manual / documento) + identificación (ej: "DIAN", "RUT-2026.pdf"). |
| `fechaCarga` | Timestamp de la carga. |

**Flujo posterior (aplicación):**

5. El administrador fiscal revisa el `ResultadoCarga` y decide qué atributos aplicar
6. Se actualiza `PerfilTributario` con los atributos aprobados

> **Nota de implementación:** Los pasos 5-6 son orquestación de aplicación (UI + comando). Los adaptadores por canal (API DIAN, parser OCR, formulario) son infraestructura. El servicio de dominio solo conoce atributos normalizados.

**Agregados que lee:** CatalogoDeAtributosFiscales.
**Agregados que escribe:** PerfilTributario (indirectamente — tras aprobación del administrador).
**Dependencia externa:** Adaptadores por canal (anti-corruption layer).

### 3.15. Read Model: CatalogoJurisdiccional `[F1]`

**Naturaleza:** Proyección de nivel Soporte `[D7]`. Consolida información jurisdiccional dispersa en múltiples agregados de configuración en una vista optimizada para consulta.

**Fuentes (eventos que alimentan la proyección):**

| Agregado origen | Datos proyectados |
|---|---|
| `CatalogoTributario` | Tributos por jurisdicción, clasificaciones, matriz de tratamiento. |
| `TarifaTributaria` | Tarifas vigentes por tributo × jurisdicción. |
| `CondicionDeAplicacion` | Condiciones activas por jurisdicción. |

**Estructura proyectada:**

| Vista | Contenido | Consumidor principal |
|---|---|---|
| Por jurisdicción | Tributos vigentes + tarifas + condiciones activas en esa jurisdicción | Administrador fiscal (UI de consulta) |
| Por tributo | Jurisdicciones donde aplica + tarifas por jurisdicción + condiciones asociadas | Administrador fiscal, MotorDeCalculo (cache de lectura) |

**Reconstrucción:** Al ser una proyección de agregados ES, se reconstruye desde cero reproduciendo los streams de origen `[D10]`. Si la proyección se corrompe o tiene un bug, el rebuild es la corrección — no requiere migración de datos.

### 3.16. Resumen de contenido fiscal por país

El detalle completo de la configuración estándar por país se encuentra en los anexos: `anexo-configuracion-estandar-co.md`, `anexo-configuracion-estandar-do.md`, `anexo-configuracion-estandar-pa.md`.

#### Tributos por país

| País | Impuestos | Retenciones | Autoretenciones | Total |
|------|:---------:|:-----------:|:---------------:|:-----:|
| Colombia | 3 (IVA, INC, ICA) | 4 (RETEFUENTE, RIVA, RICA, SOBRETASA_BOMBERIL) | 4 (AUTO_RENTA, AUTO_RETEFUENTE, AUTO_RIVA, AUTO_RICA) | **11** |
| Rep. Dominicana | 4 (ITBIS, ISC, CDT, PROPINA) | 1 (RITBIS) | — | **5** |
| Panamá | 2 (ITBMS, ISC) | 2 (RITBMS, ISR) | — | **4** |
| **Total** | **9** | **7** | **4** | **20** |

#### Complejidad de condiciones por país

| País | Condiciones con evaluación de perfil | Condiciones simples | Tributos con cuantía mínima | Tributos municipales |
|------|:------------------------------------:|:-------------------:|:---------------------------:|:--------------------:|
| Colombia | 8 (RETEFUENTE) + 3 (RIVA) + 3 (RICA) + 4 (autos) | 3 (IVA, INC, ICA) | 4 (RETEFUENTE, ICA, RICA, AUTO_RETEFUENTE) | 4 (ICA, RICA, SOBRETASA_BOMBERIL, AUTO_RICA) |
| Rep. Dominicana | — | 5 (todos) | — | — |
| Panamá | — | 4 (todos) | — | — |

#### Streams estimados por país (contenido fiscal)

| Agregado | Colombia | Rep. Dominicana | Panamá |
|----------|:--------:|:---------------:|:------:|
| CatalogoTributario | 1 | 1 | 1 |
| TarifaTributaria | ~25 (11 nacionales + ~14 municipales principales) | 5 | 4 |
| CondicionDeAplicacion | 1 | 1 | 1 |
| CatalogoDeAtributosFiscales | 1 | 1 | 1 |
| FormatoFiscal | ~10 (7 exógena + reportes municipales + certificados) | 4 | Por definir |
| HomologacionFiscal | 1 (DIAN) | 1 (DGII) | 1 (DGI) |
| **Total streams config** | **~39** | **~13** | **~8** |

---

## 4. Máquinas de estado

Dos agregados del bounded context tienen FSM transaccional: `EntregableFiscal` (reportes) y `CertificadoTributario` (certificados individuales). Los 7 agregados de configuración tienen ciclo de vida CRUD (crear, actualizar, desactivar) sin transiciones de estado. `RegistroTributario` nace como hecho confirmado sin transiciones `[D4]`.

### 4.1. EntregableFiscal (reportes)

```
┌──────────┐  EntregableFiscalGenerado  ┌──────────┐  EntregableFiscalPresentado  ┌────────────┐
│ Borrador │───────────────────────────►│ Generado │─────────────────────────────►│ Presentado │ ■
└──────────┘                            └─────┬────┘                              └────────────┘
     ▲                                        │
     │  EntregableFiscalRegenerado            │
     │  (vuelve a Borrador,                   │
     │   descarta contenido anterior)         │
     └────────────────────────────────────────┘
```

**Notas:**

- `Borrador` es el estado inicial. Se crea cuando se inicia la generación de un reporte para un período, autoridad y tipo específicos.
- `Generado` se alcanza cuando el `ContenidoGenerado` se completa exitosamente — las filas de datos homologadas y los archivos de salida están disponibles. Desde este estado se puede presentar o regenerar (vuelve a Borrador).
- `Presentado` es terminal. Se alcanza cuando el administrador fiscal confirma que el entregable fue presentado ante la autoridad fiscal.
- La **regeneración** permite corregir un entregable antes de presentarlo: si se detecta un error en el contenido o si se agregaron registros tributarios al período, se regenera desde Borrador. La regeneración desde `Generado` descarta el `ContenidoGenerado` anterior y los archivos asociados.
- La regeneración **no es posible** desde `Presentado` — si se necesita corregir un entregable ya presentado, se crea uno nuevo (nuevo stream).

### 4.2. CertificadoTributario

```
┌──────────┐  CertificadoTributarioGenerado  ┌──────────┐  CertificadoTributarioEntregado  ┌───────────┐
│ Borrador │───────────────────────────────►│ Generado │──────────────────────────────────►│ Entregado │ ■
└──────────┘                                └─────┬────┘                                   └───────────┘
     ▲                                            │  ▲
     │  CertificadoTributarioRegenerado           │  │  CertificadoTributarioReenviado
     │  (vuelve a Borrador)                       │  │  (vuelve a Generado)
     └────────────────────────────────────────────┤  │
                                                  │  │
                                                  ▼  │
                                             ┌──────────┐
                                             │ Fallido  │
                                             └──────────┘
```

**Notas:**

- `Borrador` es el estado inicial. Se crea una instancia por cada tercero al que se debe emitir certificado para el período.
- `Generado` se alcanza cuando el contenido del certificado se genera exitosamente a partir de los `RegistroTributario` del período para ese tercero. Desde este estado se puede enviar, regenerar (vuelve a Borrador) o el envío puede fallar.
- `Entregado` es terminal. Se alcanza cuando la infraestructura confirma que el envío fue exitoso (el certificado salió del sistema por el canal configurado). Es un **derivado por transición** — la infraestructura reporta éxito y el dominio registra el hecho. `[R28]` `[R37]`
- `Fallido` se alcanza cuando la infraestructura reporta fallo en el envío. Desde este estado el administrador puede reintentar (vuelve a Generado para nuevo envío) o corregir datos del destinatario antes de reintentar. `Fallido` no es terminal — siempre se puede resolver.
- La **regeneración** permite corregir el contenido del certificado antes de enviarlo. No es posible desde `Entregado` — si se necesita corregir un certificado ya entregado, se crea uno nuevo.
- La **generación masiva** ("generar todos los certificados del 2025") es un proceso de aplicación que crea N instancias de `CertificadoTributario`. Cada una sigue su propio ciclo de vida de forma independiente. La agrupación por período es informativa (read model / proyección).

---

## 5. Catálogo de eventos

El bounded context de Impuestos emite **45 eventos** distribuidos en 10 agregados. Los 7 agregados de configuración siguen un patrón uniforme (crear agregado, agregar/modificar/cerrar o desactivar entidades internas) y se documentan en formato compacto. Los 3 agregados transaccionales usan el template completo (Sección 2.2) porque tienen FSM, causalidad derivada y precondiciones complejas.

**Convención de verbos para fin de aplicabilidad:**

| Verbo | Mecanismo | Significa |
|---|---|---|
| **Cerrado/Cerrada** | La entidad tiene `Vigencia` VO | Se acotó el rango temporal. El dato sigue siendo válido dentro de su rango para consultas históricas. |
| **Desactivado/Desactivada** | La entidad no tiene vigencia temporal | Dejó de ser relevante. Es una definición estructural que se retira. Se conserva por trazabilidad. |
| **Eliminado/Eliminada** | Remoción del agregado | Se quitó de la definición. No caducó ni se desactivó — ya no forma parte de la estructura. |

### 5.1. Resumen

| Agregado | Tipo | Eventos | Total |
|---|---|---|:---:|
| CatalogoTributario | Configuración | CatalogoTributarioCreado, TributoAgregado, TributoModificado, TributoDesactivado, ClasificacionTributariaAgregada, ClasificacionTributariaModificada, ClasificacionTributariaDesactivada, TratamientoDefinido, ReglaDeLocalizacionDefinida | 9 |
| TarifaTributaria | Configuración | TarifaTributariaCreada, EntradaDeTarifaAgregada, EntradaDeTarifaModificada, EntradaDeTarifaCerrada | 4 |
| CondicionDeAplicacion | Configuración | CondicionDeAplicacionCreada, CondicionAgregada, CondicionModificada, CondicionCerrada | 4 |
| CatalogoDeAtributosFiscales | Configuración | CatalogoDeAtributosFiscalesCreado, DefinicionAtributoAgregada, DefinicionAtributoModificada, DefinicionAtributoCerrada | 4 |
| PerfilTributario | Configuración | PerfilTributarioCreado, AtributoFiscalAgregado, AtributoFiscalModificado, AtributoFiscalCerrado | 4 |
| HomologacionFiscal | Configuración | HomologacionFiscalCreada, EquivalenciaAgregada, EquivalenciaModificada, EquivalenciaCerrada | 4 |
| FormatoFiscal | Configuración | FormatoFiscalCreado, FormatoFiscalModificado, SeccionFormatoAgregada, SeccionFormatoModificada, SeccionFormatoEliminada | 5 |
| RegistroTributario | Transaccional (ES) | RegistroTributarioCreado | 1 |
| EntregableFiscal | Transaccional (ES) | EntregableFiscalCreado, EntregableFiscalGenerado, EntregableFiscalRegenerado, EntregableFiscalPresentado | 4 |
| CertificadoTributario | Transaccional (ES) | CertificadoTributarioCreado, CertificadoTributarioGenerado, CertificadoTributarioRegenerado, CertificadoTributarioEntregado, CertificadoTributarioFallido, CertificadoTributarioReenviado | 6 |
| **Total** | | | **45** |

---

### 5.2. Eventos de configuración

Los eventos de configuración siguen un patrón uniforme: el agregado se crea una vez y las entidades internas se agregan, modifican o cierran/desactivan según su mecanismo de control. No hay FSM transaccional — todos los eventos aplican desde cualquier punto del ciclo de vida del agregado. Las precondiciones son validaciones internas del agregado (tipos, unicidad, vigencias).

#### 5.2.1. CatalogoTributario — 9 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `CatalogoTributarioCreado` | Se creó el catálogo tributario para un país. | País, origen. | — |
| 2 | `TributoAgregado` | Se registró un nuevo tributo en el catálogo. | Código, nombre, naturaleza (aditivo/sustractivo), caracterRetención, nivelJurisdiccional, factorDeTarifa, tributoPadre (si aplica), origen. | `[R03]` |
| 3 | `TributoModificado` | Se actualizaron atributos de un tributo existente. | Código (identifica), nombre, naturaleza, caracterRetención, nivelJurisdiccional, factorDeTarifa, tributoPadre, origen (campos modificados). | `[R03]` |
| 4 | `TributoDesactivado` | Un tributo dejó de ser relevante en la jurisdicción. Se conserva para trazabilidad histórica. El motor no lo evalúa. | Código, motivo. | — |
| 5 | `ClasificacionTributariaAgregada` | Se registró una nueva clasificación tributaria. | Código, nombre, descripción, origen. | `[R01]` |
| 6 | `ClasificacionTributariaModificada` | Se actualizaron atributos de una clasificación existente. | Código (identifica), nombre, descripción, origen (campos modificados). | `[R01]` |
| 7 | `ClasificacionTributariaDesactivada` | Una clasificación dejó de ser relevante. Se conserva para trazabilidad histórica. | Código, motivo. | — |
| 8 | `TratamientoDefinido` | Se estableció si un tributo aplica o no a una clasificación. Cubre creación y modificación — es una operación idempotente sobre la combinación (tributo × clasificación). | Tributo, clasificación, aplica (sí/no), origen. | `[R03]` `[R09]` |
| 9 | `ReglaDeLocalizacionDefinida` | Se estableció qué rol de ubicación determina la jurisdicción fiscal para un tributo en una clasificación. Cubre creación y modificación. | Tributo, clasificación (o `*`), rolQueManda, rolFallback (opcional), origen. | `[R34]` |

#### 5.2.2. TarifaTributaria — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `TarifaTributariaCreada` | Se creó la tabla de tarifas para un tributo en una jurisdicción. | Jurisdicción, tributo, origen. | — |
| 2 | `EntradaDeTarifaAgregada` | Se registró una nueva entrada de tarifa. | Factor, tarifa, tipoTarifa (porcentaje/específica), cuantíaMínima (opcional), vigencia (desde/hasta), origen. | `[R06]` `[R07]` `[R08]` |
| 3 | `EntradaDeTarifaModificada` | Se actualizaron atributos de una entrada existente (tarifa, cuantíaMínima, tipoTarifa). | Factor + vigencia (identifican), tarifa, tipoTarifa, cuantíaMínima (campos modificados). | `[R06]` `[R08]` |
| 4 | `EntradaDeTarifaCerrada` | Se cerró la vigencia de una entrada. La entrada sigue siendo válida para consultas dentro de su rango temporal. | Factor, vigenciaHasta (fecha de cierre). | `[R08]` |

#### 5.2.3. CondicionDeAplicacion — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `CondicionDeAplicacionCreada` | Se creó el conjunto de condiciones para un país. | País, origen. | — |
| 2 | `CondicionAgregada` | Se registró una nueva condición de aplicación. | EntidadEvaluada (emisora/contraparte), atributoEvaluado (ref. DefinicionAtributo), valorEsperado, tributoAfectado, efecto (noAplicar/cambiarTarifa/reverseCharge), tarifaAlternativa (si aplica), vigencia, origen. | `[R10]` `[R11]` `[R35]` |
| 3 | `CondicionModificada` | Se actualizaron atributos de una condición existente. | Identificador de condición, valorEsperado, efecto, tarifaAlternativa, vigencia (campos modificados). | `[R10]` `[R11]` |
| 4 | `CondicionCerrada` | Se cerró la vigencia de una condición. La condición sigue siendo válida para evaluaciones dentro de su rango temporal. | Identificador de condición, vigenciaHasta. | — |

#### 5.2.4. CatalogoDeAtributosFiscales — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `CatalogoDeAtributosFiscalesCreado` | Se creó el catálogo de atributos fiscales para un país. | País, origen. | — |
| 2 | `DefinicionAtributoAgregada` | Se registró un nuevo atributo fiscal en el catálogo. | Nombre, tipo (boolean/enum/string/numerico), valoresValidos (si enum), requerido, vigenciaDefinicion, origen. | `[D3]` |
| 3 | `DefinicionAtributoModificada` | Se actualizaron propiedades de una definición (valoresValidos, requerido). El tipo no cambia — si cambia, se cierra la definición y se crea una nueva. | Nombre (identifica), valoresValidos, requerido (campos modificados). | `[D3]` |
| 4 | `DefinicionAtributoCerrada` | Se cerró la vigencia de la definición. El atributo dejó de existir en la normativa. Los perfiles conservan valores históricos pero el motor no los evalúa. | Nombre, vigenciaHasta. | `[D3]` |

#### 5.2.5. PerfilTributario — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `PerfilTributarioCreado` | Se creó el perfil fiscal de una entidad. | IdentificacionFiscal, tipoEntidad (empresa/tercero), país. | — |
| 2 | `AtributoFiscalAgregado` | Se registró un nuevo atributo fiscal en el perfil. Valor validado contra CatalogoDeAtributosFiscales. | Nombre (ref. DefinicionAtributo), valor, vigencia, fuenteDeAutoridad (opcional). | `[D3]` `[R10]` |
| 3 | `AtributoFiscalModificado` | Se actualizó el valor de un atributo fiscal existente. Nueva vigencia para el valor actualizado. | Nombre (identifica), valor nuevo, vigencia nueva, fuenteDeAutoridad (opcional). | `[D3]` |
| 4 | `AtributoFiscalCerrado` | Se cerró la vigencia de un atributo fiscal. El valor sigue siendo válido para consultas dentro de su rango temporal. | Nombre, vigenciaHasta. | — |

#### 5.2.6. HomologacionFiscal — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `HomologacionFiscalCreada` | Se creó la tabla de homologación para una autoridad fiscal. | AutoridadFiscal (nombre, jurisdicción, país), origen. | — |
| 2 | `EquivalenciaAgregada` | Se registró un nuevo mapeo entre valor interno y código de la autoridad. | ValorInterno, tributo, codigoAutoridad, nombreAutoridad, vigencia, origen. | `[D6]` |
| 3 | `EquivalenciaModificada` | Se actualizó un mapeo existente (cambio de código de la autoridad). | ValorInterno + tributo (identifican), codigoAutoridad, nombreAutoridad, vigencia (campos modificados). | `[D6]` |
| 4 | `EquivalenciaCerrada` | Se cerró la vigencia de una equivalencia. La traducción sigue disponible para consultas dentro de su rango temporal. | ValorInterno, tributo, vigenciaHasta. | — |

#### 5.2.7. FormatoFiscal — 5 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `FormatoFiscalCreado` | Se creó la definición de un formato de entregable fiscal. | TipoEntregable (reporte/certificado), AutoridadFiscal, periodicidad, formatosDeSalida, homologación (ref.), vigencia, origen. | `[R26]` `[R27]` |
| 2 | `FormatoFiscalModificado` | Se actualizaron atributos del formato (periodicidad, formatos de salida, vigencia). | ID (identifica), periodicidad, formatosDeSalida, vigencia (campos modificados). | `[R27]` |
| 3 | `SeccionFormatoAgregada` | Se agregó una nueva sección al formato. | Nombre, descripción, criterioDeAgrupacion, criterioDeSeleccion, orden. | `[R26]` |
| 4 | `SeccionFormatoModificada` | Se actualizaron atributos de una sección existente. | Nombre (identifica), descripción, criterioDeAgrupacion, criterioDeSeleccion, orden (campos modificados). | `[R26]` |
| 5 | `SeccionFormatoEliminada` | Se quitó una sección del formato. Las futuras generaciones no la incluyen. | Nombre de sección eliminada. | — |

---

### 5.3. Eventos transaccionales

Los agregados transaccionales usan el template completo (Sección 2.2). Cada evento incluye: descripción, causalidad (si no es directa), agregado, estado previo/resultante, precondiciones, información capturada y efectos.

#### 5.3.1. RegistroTributario — 1 evento

##### RegistroTributarioCreado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se registró un hecho fiscal inmutable: el resultado tributario de una transacción confirmada. El `efectoFiscal` (gravamen o desgravamen) clasifica el hecho — los montos siempre son positivos y las proyecciones interpretan el efecto para determinar el signo al sumar. Para desgravámenes, el desglose se deriva del prorrateo del registro origen (no del motor). |
| **Agregado** | RegistroTributario |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Sin FSM — nace como hecho fiscal confirmado. No tiene transiciones posteriores. |
| **Precondiciones** | Confirmación recibida del sub-dominio consumidor autorizado (OXP, CXC). Contexto transaccional completo: entidades fiscales, ubicaciones, conceptos con clasificación, desglose confirmado. Cálculo de referencia obtenido por `ConfirmacionTributaria` (3.13): motor (gravámenes) o prorrateo del registro origen (desgravámenes). `[R22]` |
| **Información capturada** | ContextoTransaccional (sub-dominio, ID transacción, dirección fiscal, efectoFiscal; si desgravamen: transaccionOrigenId), EntidadFiscalEmisora (snapshot), EntidadFiscalContraparte (snapshot), Jurisdiccion resuelta, desglose confirmado (LineaDeDesglose[]: tributo, naturaleza, baseGravable, tarifa, tipoTarifa, valor, factorUtilizado, conceptoOrigen), IntervencionManual. Si huboIntervencion: cálculo de referencia (LineaDesgloseMotor[]; en gravámenes también LineaDescartada[] con motivoExclusion — en desgravámenes LineaDescartada no aplica). `[R23]` `[R24]` |
| **Efectos** | Registro disponible para: proyecciones de reportes y certificados (EntregableFiscal, CertificadoTributario), consulta del motor en confirmaciones futuras. Las proyecciones interpretan `efectoFiscal` para determinar el signo al sumar y obtener el neto correcto del período. |

#### 5.3.2. EntregableFiscal — 4 eventos

##### EntregableFiscalCreado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se inició la generación de un reporte fiscal para un período, autoridad y tipo específicos. |
| **Agregado** | EntregableFiscal |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Borrador. |
| **Precondiciones** | FormatoFiscal vigente para la autoridad y tipo. Período fiscal definido. |
| **Información capturada** | TipoEntregable (reporte), AutoridadFiscal, PeriodoFiscal, ReferenciaFormato, ReferenciaHomologacion. |
| **Efectos** | Entregable disponible para generación de contenido. |

##### EntregableFiscalGenerado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contenido del entregable se generó exitosamente. Las filas de datos fueron homologadas con códigos de la autoridad y los archivos de salida están disponibles. |
| **Agregado** | EntregableFiscal |
| **Estado previo** | Borrador. |
| **Estado resultante** | Generado. |
| **Precondiciones** | Entregable en estado Borrador. RegistroTributario del período disponibles. HomologacionFiscal vigente para traducir valores internos. `[R26]` `[R27]` |
| **Información capturada** | ContenidoGenerado (filas homologadas, totalRegistrosIncluidos, fechaGeneracion), ArchivoGenerado[] (tipo, referencia, hash). |
| **Efectos** | Entregable disponible para presentación o regeneración. |

##### EntregableFiscalRegenerado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se regeneró el contenido del entregable. El contenido anterior y los archivos asociados fueron descartados. |
| **Agregado** | EntregableFiscal |
| **Estado previo** | Generado. |
| **Estado resultante** | Borrador. |
| **Precondiciones** | Entregable en estado Generado. `puedeGenerarContenido() = true`. |
| **Información capturada** | Motivo de regeneración (opcional). |
| **Efectos** | ContenidoGenerado anterior descartado. Archivos anteriores invalidados. Entregable requiere nueva generación. |

##### EntregableFiscalPresentado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El administrador fiscal confirmó que el entregable fue presentado ante la autoridad fiscal. |
| **Agregado** | EntregableFiscal |
| **Estado previo** | Generado. |
| **Estado resultante** | Presentado ■ (terminal). |
| **Precondiciones** | Entregable en estado Generado. `esPresentable() = true`. |
| **Información capturada** | FechaPresentacion, responsable (usuario que confirma). |
| **Efectos** | Entregable sellado — no se puede regenerar ni modificar. Si se necesita corregir, se crea un nuevo entregable (nuevo stream). |

#### 5.3.3. CertificadoTributario — 6 eventos

##### CertificadoTributarioCreado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se inició la creación de un certificado tributario individual para un tercero en un período fiscal. |
| **Agregado** | CertificadoTributario |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Borrador. |
| **Precondiciones** | FormatoFiscal de tipo certificado vigente. Tercero con RegistroTributario en el período. `[R28]` |
| **Información capturada** | Destinatario (identificación fiscal, razón social, datos de contacto), AutoridadFiscal, PeriodoFiscal, ReferenciaFormato, ReferenciaHomologacion. |
| **Efectos** | Certificado disponible para generación de contenido. En generación masiva, un proceso de aplicación crea N instancias — cada una es independiente. |

##### CertificadoTributarioGenerado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contenido del certificado se generó exitosamente a partir de los registros tributarios del período para el tercero. |
| **Agregado** | CertificadoTributario |
| **Estado previo** | Borrador. |
| **Estado resultante** | Generado. |
| **Precondiciones** | Certificado en estado Borrador. RegistroTributario del período para el tercero disponibles. HomologacionFiscal vigente. `[R28]` |
| **Información capturada** | ContenidoCertificado (líneas de detalle por tributo: baseGravable, tarifa, valor retenido; totales por tributo; fechaGeneracion), ArchivoGenerado (PDF, referencia, hash). |
| **Efectos** | Certificado disponible para envío, regeneración o consulta. |

##### CertificadoTributarioRegenerado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se regeneró el contenido del certificado. El contenido anterior y el archivo fueron descartados. |
| **Agregado** | CertificadoTributario |
| **Estado previo** | Generado. |
| **Estado resultante** | Borrador. |
| **Precondiciones** | Certificado en estado Generado. `puedeGenerarContenido() = true`. |
| **Información capturada** | Motivo de regeneración (opcional). |
| **Efectos** | ContenidoCertificado anterior descartado. Archivo anterior invalidado. Certificado requiere nueva generación. |

##### CertificadoTributarioEntregado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La infraestructura confirmó que el envío del certificado fue exitoso. El certificado salió del sistema por el canal configurado (correo, portal). |
| **Causalidad** | Derivado por transición — la infraestructura reporta éxito y el dominio registra el hecho. |
| **Agregado** | CertificadoTributario |
| **Estado previo** | Generado. |
| **Estado resultante** | Entregado ■ (terminal). |
| **Precondiciones** | Certificado en estado Generado. Infraestructura reporta envío exitoso. |
| **Información capturada** | ResultadoEnvio (canal, fecha, exitoso: true). `[R28]` `[R37]` |
| **Efectos** | Certificado sellado — no se puede regenerar ni reenviar. Si se necesita corregir, se crea un nuevo certificado (nuevo stream). |

##### CertificadoTributarioFallido

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La infraestructura reportó fallo en el envío del certificado. |
| **Causalidad** | Derivado por transición — la infraestructura reporta fallo y el dominio registra el hecho. |
| **Agregado** | CertificadoTributario |
| **Estado previo** | Generado. |
| **Estado resultante** | Fallido. |
| **Precondiciones** | Certificado en estado Generado. Infraestructura reporta fallo de envío. |
| **Información capturada** | ResultadoEnvio (canal, fecha, exitoso: false, detalleFallo). |
| **Efectos** | Certificado disponible para reintento (`esReenviable() = true`). Administrador puede corregir datos del destinatario antes de reintentar. |

##### CertificadoTributarioReenviado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se solicitó reintento de envío para un certificado cuyo envío anterior falló. |
| **Agregado** | CertificadoTributario |
| **Estado previo** | Fallido. |
| **Estado resultante** | Generado. |
| **Precondiciones** | Certificado en estado Fallido. `esReenviable() = true`. |
| **Información capturada** | Destinatario (estado actual al momento del reenvío — siempre incluido para replay safety), responsable (usuario que autoriza reintento). |
| **Efectos** | Certificado vuelve a Generado para nuevo intento de envío por infraestructura. Si el nuevo envío tiene éxito → CertificadoTributarioEntregado. Si falla → CertificadoTributarioFallido nuevamente. |

---

## 6. Invariantes del dominio

Las invariantes son restricciones estructurales que deben ser verdaderas en todo momento del ciclo de vida del dominio. A diferencia de las reglas de negocio (R01–R38), que pueden ser configurables y tener excepciones, las invariantes son absolutas. Clasificación: **local** (enforceada por un solo agregado, transaccional) o **eventual** (cruza fronteras de agregado, enforceada por validación en escritura + proyección eventual para detección tardía).

| # | Invariante | Agregado | Referencia |
|---|-----------|----------|------------|
| I1 | **No solapamiento de vigencias:** Dos `EntradaDeTarifa` con el mismo factor y origen no pueden tener vigencias que se solapen. Enforcement: `validarNoSolapamiento()` como precondición de escritura. | TarifaTributaria | `[R08]` |
| I2 | **Dependencia de tributo padre:** Si un `Tributo` declara `tributoPadre`, el padre debe existir y estar activo en el mismo `CatalogoTributario`. Tributos hijos no pueden existir sin su padre. | CatalogoTributario | `[R03]` |
| I3 | **Unicidad de tratamiento:** Para una combinación (tributo × clasificación × origen), solo puede existir un `Tratamiento`. Si coexisten tratamiento estándar y personalizado para la misma combinación (tributo × clasificación), ambos se almacenan pero `tributosAplicablesA()` retorna el personalizado (precedencia). | CatalogoTributario | `[R09]` `[R14]` |
| I4 | **Unicidad de equivalencia:** En `HomologacionFiscal`, la combinación (valorInterno + tributo + origen) es única. Si coexisten estándar y personalizada, `homologar()` retorna la personalizada. | HomologacionFiscal | `[D6]` |
| I5 | **Validación de atributo fiscal (eventual):** Todo `AtributoFiscal` en `PerfilTributario` debe tener nombre, tipo y valor consistentes con una `DefinicionAtributo` vigente en `CatalogoDeAtributosFiscales` del mismo país. Enforcement: `PerfilTributario` valida contra el catálogo al momento de la escritura. Si la definición se cierra después, los valores históricos se conservan pero el motor no los evalúa. | PerfilTributario, CatalogoDeAtributosFiscales | `[D3]` |
| I6 | **Condición referencia atributo existente (eventual):** Todo `Condicion.atributoEvaluado` debe referenciar una `DefinicionAtributo` que exista en `CatalogoDeAtributosFiscales` del mismo país. Enforcement: `CondicionDeAplicacion` valida al momento de agregar o modificar una condición. Si la definición del atributo se cierra después de crear la condición, la condición deja de evaluarse para transacciones cuya fecha exceda la vigencia de la definición. | CondicionDeAplicacion, CatalogoDeAtributosFiscales | `[D3]` `[R35]` |
| I7 | **Unicidad de catálogo por país (eventual):** Solo puede existir un `CatalogoTributario`, un `CatalogoDeAtributosFiscales` y un `CondicionDeAplicacion` por país. Enforcement: validación al crear + proyección eventual para detección tardía. | CatalogoTributario, CatalogoDeAtributosFiscales, CondicionDeAplicacion | `[D1]` |
| I8 | **Homologación completa para generación (eventual):** Al generar un `EntregableFiscal` o `CertificadoTributario`, cada `factorUtilizado` de los `RegistroTributario` incluidos debe tener una `Equivalencia` vigente en `HomologacionFiscal` de la autoridad correspondiente. Enforcement: precondición de generación — si falta una equivalencia, la generación falla e indica cuáles valores no tienen traducción. | EntregableFiscal, CertificadoTributario, HomologacionFiscal | `[D6]` `[R26]` |
| I9 | **Inmutabilidad del registro tributario:** `RegistroTributario` tiene un único evento (`RegistroTributarioCreado`). No admite eventos posteriores que modifiquen su contenido. Cada registro es un hecho fiscal independiente (gravamen o desgravamen). Las proyecciones interpretan el `efectoFiscal` para determinar el signo al sumar y obtener el neto correcto. | RegistroTributario | `[D4]` |
| I10 | **Consistencia de intervención manual:** Si `IntervencionManual.huboIntervencion = true`, el registro debe contener `LineaDesgloseMotor[]` (cálculo de referencia). En gravámenes, también `LineaDescartada[]` (tributos excluidos por el motor con `motivoExclusion`). En desgravámenes, `LineaDescartada` no aplica — el cálculo de referencia es el prorrateo del desglose confirmado del registro origen. Si `huboIntervencion = false`, estos conjuntos no existen. Derivable del factory method `crear()` que compara `desgloseConfirmado` con `calculoDeReferencia`. | RegistroTributario | `[R24]` |
| I11a | **Progresión de estados — EntregableFiscal:** Solo las transiciones definidas en FSM 4.1. Borrador → Generado → Presentado ■. Regeneración: Generado → Borrador. No hay retroceso desde Presentado (terminal). Si se necesita corregir un entregable presentado, se crea un nuevo stream. | EntregableFiscal | — |
| I11b | **Progresión de estados — CertificadoTributario:** Solo las transiciones definidas en FSM 4.2. Borrador → Generado → Entregado ■. Fallo: Generado → Fallido. Reintento: Fallido → Generado. Regeneración: Generado → Borrador. No hay retroceso desde Entregado (terminal). Si se necesita corregir un certificado entregado, se crea un nuevo stream. | CertificadoTributario | — |
| I12 | **Unicidad de perfil por entidad y país (eventual):** Solo puede existir un `PerfilTributario` por combinación (entidad × país). Enforcement: validación al crear + proyección eventual para detección tardía. | PerfilTributario | — |

---

## 7. Qué NO contiene este documento

| Excluido | Razón | Dónde vive |
|----------|-------|------------|
| Glosario de términos | Ya definido | `definicion-alcance.md`, Sección 2 |
| Actores y permisos | Ya definidos | `definicion-alcance.md`, Sección 3 |
| Reglas de negocio completas | Ya definidas (R01–R38) | `definicion-alcance.md`, Sección 6 |
| Localizaciones por país (tributos, tarifas, reportes) | Contenido de datos, no modelo | Configuración estándar por país (carga inicial) |
| Modelo de datos / esquema de BD | Pertenece a implementación | Documentación técnica |
| Endpoints de API / contratos de integración | Pertenece a implementación | Documentación técnica |
| Mecánica de envío de certificados (correo, portal) | Infraestructura — el dominio solo registra éxito/fallo | Adaptadores de infraestructura |
| Diseño de interfaz de usuario | Pertenece a UX | Especificaciones de UX |
| Configuración de EventCatalog | Herramienta de fase 3 | Se derivará de este documento |
| Justificación de decisiones de modelado | Documento separado | `guias-de-modelado/modelar-agregados.md` |

---

## 8. Decisiones de arquitectura y diseño

### [D1] Raíz por país en agregados de configuración

**Contexto:** Los tributos, clasificaciones, tratamientos y condiciones por perfil son definiciones nacionales — la normativa fiscal se define a nivel de país. Los tributos subnacionales (ICA, RICA en Colombia) también se definen en el catálogo nacional con `nivelJurisdiccional: municipal`; lo que varía por municipio son las tarifas, que viven en TarifaTributaria con stream key por jurisdicción.

**Decisión:** CatalogoTributario y CondicionDeAplicacion usan el país como raíz del agregado (un stream por país). TarifaTributaria usa jurisdicción + tributo como raíz (un stream por combinación).

**Justificación:** Las invariantes de tratamiento (R03: dependencia tributo-clasificación) son nacionales — no cambian por municipio. Agrupar por país mantiene la invariante local al agregado.

**Aplica a:** CatalogoTributario (3.2), CondicionDeAplicacion (3.4).

### [D2] Diseño dimensional del catálogo tributario

**Contexto:** Un modelo combinatorio (clasificación × dirección × jurisdicción × tributo × actividad) genera ~286.000 reglas para Colombia, difíciles de administrar y auditar.

**Decisión:** Separar la configuración fiscal en tres dimensiones independientes que el motor cruza en tiempo de cálculo: qué tributos aplican (CatalogoTributario), cuánto se cobra (TarifaTributaria), quién tiene tratamiento especial (CondicionDeAplicacion).

**Justificación:** Reduce complejidad administrativa, permite actualizar una dimensión sin tocar las demás, y la misma estructura sirve para cualquier país. Detalle completo en `anexo-diseno-dimensional.md`.

**Descomposición de R02 en el diseño dimensional:** R02 del alcance define las reglas de aplicación por combinación de "clasificación tributaria + dirección fiscal + jurisdicción". El diseño dimensional descompone esta combinación así:

- **Clasificación tributaria →** `Tratamiento` en CatalogoTributario determina qué tributos aplican a cada clasificación, independiente de la dirección fiscal.
- **Dirección fiscal →** `CondicionDeAplicacion` ajusta el tratamiento evaluando los roles de las entidades fiscales (emisora/contraparte). Los roles codifican implícitamente la dirección: en gastos, la emisora es adquiriente/retenedora; en ingresos, es facturadora/sujeto de retención. La dirección no es una dimensión del catálogo sino un contexto que modifica la aplicación vía condiciones.
- **Jurisdicción →** `ReglaDeLocalizacion` en CatalogoTributario determina la jurisdicción fiscal relevante; `TarifaTributaria` provee tarifas por jurisdicción.

**Aplica a:** CatalogoTributario (3.2), TarifaTributaria (3.3), CondicionDeAplicacion (3.4), MotorDeCalculo.

### [D3] Catálogo de atributos fiscales validado

**Contexto:** Los atributos fiscales de una entidad (gran contribuyente, autorretenedor, régimen tributario, actividad económica, etc.) varían por país y evolucionan con la normativa — atributos aparecen, desaparecen, se transforman (ej: "régimen simplificado" dejó de existir en Colombia en 2019). Un diseño con atributos genéricos (pares nombre-valor sin esquema) no ofrece validación de tipo, permite acoples invisibles por convención de nombres, y no detecta cuándo un atributo referenciado por una condición ya no existe.

**Decisión:** Se crea un agregado `CatalogoDeAtributosFiscales` (por país) que define qué atributos fiscales existen, su tipo (boolean/enum/string/numerico), valores válidos, obligatoriedad y vigencia de la definición misma. `PerfilTributario` valida cada escritura contra este catálogo. `CondicionDeAplicacion` referencia `DefinicionAtributo` del catálogo, no strings libres.

**Justificación:** Sigue el mismo patrón dimensional: así como `CatalogoTributario` define qué tributos existen y `TarifaTributaria` los consulta, `CatalogoDeAtributosFiscales` define qué atributos fiscales existen y `PerfilTributario` los almacena validados. Cuando un atributo cambia en la normativa, se cierra su vigencia en el catálogo — los perfiles conservan valores históricos pero el motor deja de evaluarlos. La carga asistida (`CargaAsistida`) puede migrar perfiles cuando cambian las definiciones.

**Aplica a:** CatalogoDeAtributosFiscales (3.5), PerfilTributario (3.6), CondicionDeAplicacion (3.4), CargaAsistida.

### [D4] Registro tributario como hecho inmutable

**Contexto:** El sub-dominio de Impuestos necesita una fuente de verdad propia de cada cálculo fiscal. Los sub-dominios consumidores (OXP, CXC) guardan su propia copia del desglose para operar autónomamente, pero Impuestos necesita el registro completo para cumplimiento fiscal (reportes, certificados, conciliación).

**Decisión:** El `RegistroTributario` se crea como hecho fiscal confirmado cuando el sub-dominio consumidor confirma la transacción. Al recibir la confirmación, Impuestos re-ejecuta el motor con el contexto recibido para obtener el cálculo original, lo compara con el desglose confirmado por el consumidor, y persiste ambos. El registro captura snapshots inmutables de las entidades fiscales al momento de la confirmación. Cada registro es un hecho fiscal independiente clasificado por su `efectoFiscal` (gravamen o desgravamen). Para gravámenes, Impuestos re-ejecuta el motor como referencia. Para desgravámenes, Impuestos resuelve el RegistroTributario del gravamen original (vía `transaccionOrigenId`) y prorratea su desglose como referencia — el motor no participa. El consumidor siempre envía montos positivos — las proyecciones interpretan el efecto fiscal para determinar el signo al sumar.

**Justificación:** Trazabilidad fiscal requiere que cada hecho fiscal exista como registro independiente. Un registro puede haber sido incluido en certificados o reportes fiscales ya emitidos — modificarlo rompería la integridad de esos entregables. El registro nace confirmado (sin estados intermedios) porque durante la fase de edición el consumidor usa el motor en modo simulación — persistir propuestas intermedias genera registros basura sin valor fiscal. Los desgravámenes (devoluciones, notas crédito) son transacciones independientes del consumidor. El consumidor envía `transaccionOrigenId` para que Impuestos resuelva el RegistroTributario del gravamen original y prorratea su desglose — garantizando coherencia tarifaria y respetando las intervenciones del usuario en el gravamen. Este es el patrón convergente de la industria (Oracle Applied Credit Memo, Dynamics 365, SAP Pricing Type D, Avalara TaxOverride.TaxDate).

**Aplica a:** RegistroTributario (3.7), MotorDeCalculo, EntregableFiscal.

### [D5] Motor de cálculo stateless con evaluación completa

**Contexto:** Algunos sub-dominios consumidores necesitan calcular impuestos sin que el cálculo sea un hecho fiscal (cotizaciones, simulaciones, vista previa). Además, durante la fase de edición de una transacción, el consumidor puede solicitar múltiples cálculos a medida que el usuario ajusta conceptos, montos o clasificaciones — persistir cada uno generaría registros basura.

**Decisión:** El MotorDeCalculo siempre opera stateless: calcula y retorna la evaluación completa sin crear ningún registro. La respuesta incluye dos conjuntos: tributos aplicados (desglose propuesto) y tributos descartados con motivo estructurado de exclusión (`cuantia_minima`, `perfil_no_aplica`, `clasificacion_excluida`, `jurisdiccion_no_aplica`, `dependencia_padre`). El consumidor presenta ambos conjuntos al usuario, quien puede excluir tributos propuestos o incluir tributos descartados. La creación del `RegistroTributario` ocurre únicamente cuando el consumidor envía el comando de confirmación — en ese momento Impuestos re-ejecuta el motor internamente para obtener el cálculo original y lo contrasta con el desglose confirmado.

**Justificación:** Separa completamente el cálculo (consulta) de la persistencia (hecho fiscal). El consumidor puede solicitar N simulaciones durante la edición sin costo de almacenamiento. Los tributos descartados con motivo dan transparencia al usuario sobre el razonamiento del motor, reducen la fricción operativa ("¿por qué no me calculó ICA?") y permiten overrides informados.

**Aplica a:** MotorDeCalculo, RegistroTributario (3.7).

### [D6] Homologación fiscal como dimensión independiente

**Contexto:** Los reportes fiscales exigen clasificar las transacciones con códigos específicos de cada autoridad (ej: DIAN código "5002" para servicios, DGII código "02" para honorarios). En el sistema actual, la cuenta contable actúa como puente entre la transacción y el código del reporte. En el nuevo diseño, Impuestos no conoce cuentas contables `[R33]`.

**Decisión:** Se crea un agregado `HomologacionFiscal` (por autoridad fiscal) que mapea los valores internos del sub-dominio (`factorUtilizado`, código de clasificación) a los códigos que exige la autoridad. `FormatoFiscal` referencia la homologación de su autoridad. `EntregableFiscal` consulta la homologación durante la generación para traducir cada `LineaDeDesglose` al código correspondiente del reporte.

**Justificación:** Es el patrón convergente de Oracle Fusion Tax (`Tax Reporting Type/Code`) y Dynamics 365 (`Sales Tax Reporting Code` + `Report Layout`). Separar la homologación del tributo y del formato permite: (1) un mismo tributo se mapea a códigos diferentes según la autoridad, (2) la homologación se actualiza independientemente cuando la autoridad cambia sus códigos, (3) es contenido fiscal que viene con el producto.

**Aplica a:** HomologacionFiscal (3.8), FormatoFiscal (3.9), EntregableFiscal.

### [D8] Resolución de jurisdicción por regla de localización

**Contexto:** La jurisdicción fiscal no siempre es obvia. Para tributos nacionales (IVA, RETEFUENTE) siempre es el país. Para tributos municipales (ICA, RICA en Colombia) la jurisdicción depende de dónde ocurre la actividad económica: lugar de prestación del servicio, punto de entrega del bien, ubicación del inmueble o del proyecto. Si el consumidor envía una jurisdicción "resuelta", se filtra lógica fiscal al consumidor — porque determinar cuál ubicación manda es una regla fiscal.

**Decisión:** El consumidor envía un conjunto de ubicaciones tipificadas por rol semántico (`sedeEmisora`, `sedeContraparte`, `lugarEjecucion`) sin resolver cuál es la fiscalmente relevante. El CatalogoTributario contiene `ReglaDeLocalizacion` (por tributo × clasificación) que define qué rol de ubicación usar, con fallback opcional. El motor aplica la regla para resolver la jurisdicción fiscal de cada tributo de cada concepto.

**Justificación:** Es el patrón convergente de Oracle Fusion Tax ("Determine Place of Supply"), SAP ("Tax Jurisdiction Code determination"), Avalara/Vertex ("sourcing rules") y Dynamics 365 ("Tax Jurisdiction applicability"). En todos, la transacción provee múltiples ubicaciones candidatas y el motor selecciona cuál manda según reglas configurables. Esto mantiene la lógica fiscal centralizada en Impuestos.

**Aplica a:** CatalogoTributario (3.2), MotorDeCalculo, contrato semántico del consumidor.

### [D9] Contrato semántico mínimo del consumidor

**Contexto:** El sub-dominio de Impuestos depende del consumidor para recibir el contexto correcto de la transacción. Si cada consumidor envía información distinta o incompleta, parte de la lógica tributaria queda escondida en los consumidores y el cálculo depende de interpretaciones implícitas. `[R30]`

**Decisión:** Se definen dos contratos semánticos mínimos que todo consumidor debe cumplir:

**Solicitud de cálculo (simulación):**

| Campo | Tipo | Obligatorio | Referencia |
|---|---|---|---|
| `direccionFiscal` | gasto \| ingreso | Sí | `[R11]` |
| `entidadFiscalEmisora` | { tipoId, numeroId, pais } | Sí | `[R04]` |
| `entidadFiscalContraparte` | { tipoId, numeroId, pais } | Sí | `[R04]` |
| `ubicaciones.sedeEmisora` | { pais, subnacional } | Sí | `[D8]` |
| `ubicaciones.sedeContraparte` | { pais, subnacional } | Sí | `[D8]` |
| `ubicaciones.lugarEjecucion` | { pais, subnacional } | Según ReglaDeLocalizacion | `[D8]` |
| `fechaTransaccion` | date | Sí | `[R07]` |
| `moneda` | código ISO | Sí | — |
| `tipoCambioReferencia` | decimal | Solo si moneda ≠ jurisdicción | conversión cuantía mínima |
| `conceptos[]` | { id, clasificacionTributaria, monto } | Sí (mín. 1) | `[R01]` `[R13]` `[R32]` |

**Nota:** Los perfiles tributarios NO viajan en la solicitud — Impuestos los resuelve internamente a partir de las identificaciones. `[R04]` `[R05]`

**Comando de confirmación:**

| Campo | Tipo | Obligatorio | Referencia |
|---|---|---|---|
| `transaccionId` | string | Sí | `[R36]` |
| `efectoFiscal` | gravamen \| desgravamen | Sí | `[D4]` |
| `transaccionOrigenId` | string | Solo si `efectoFiscal = desgravamen` | `[D4]` |
| `subDominio` | OXP \| CXC \| ... | Sí | — |
| `contextoCompleto` | (misma estructura que solicitud de cálculo) | Sí | `[R22]` |
| `desgloseConfirmado[]` | { tributo, naturaleza, baseGravable, tarifa, tipoTarifa, valor, factorUtilizado, conceptoOrigen } | Sí | `[R24]` |

**Nota:** Cada confirmación de desgravamen referencia exactamente una transacción origen (`transaccionOrigenId` es un string, no un array). Si una operación del consumidor afecta múltiples orígenes, el consumidor descompone en N confirmaciones independientes.

**Justificación:** El contrato explícito protege la frontera del bounded context. Impuestos no necesita interpretar el contexto de negocio del consumidor — recibe datos estructurados y resuelve internamente. Si un campo falta, el motor rechaza la solicitud indicando el dato faltante `[R30]` `[R31]`.

**Aplica a:** MotorDeCalculo, RegistroTributario (3.7), todos los sub-dominios consumidores.

### [D7] Capacidades con distinto nivel de centralidad

**Contexto:** El bounded context de Impuestos absorbe capacidades de naturaleza distinta: configuración fiscal, perfiles tributarios, cálculo, registro tributario, reportes fiscales, certificados, carga asistida y catálogos de consulta. Todas pueden convivir dentro del mismo BC, pero si se tratan como igualmente centrales, los agregados de reportes pueden terminar empujando decisiones sobre el diseño del núcleo (cálculo y registro).

**Decisión:** Se declaran tres niveles de centralidad dentro del BC: **núcleo** (configuración, cálculo, perfil, registro tributario), **soporte** (carga asistida, catálogos jurisdiccionales), y **derivadas** (reportes, certificados, declaraciones, entregables). Regla de diseño: las capacidades derivadas consumen el núcleo pero no lo redefinen. Si un requerimiento de reportes necesita un dato que no existe en el RegistroTributario, la pregunta correcta es si ese dato debería capturarse al momento del cálculo (núcleo) — no moldear el registro para el reporte.

**Justificación:** Protege la tesis central del sub-dominio (el registro tributario y el cálculo son el centro) y evita que necesidades propias de formatos, cierres, entregas y regeneraciones contaminen el diseño del núcleo. No fragmenta prematuramente el BC, pero deja explícito que la separación es posible en el futuro si el ciclo de vida lo justifica.

**Fases de implementación:** La clasificación de capacidades determina el orden de implementación. **Fase 1** `[F1]` implementa las capacidades de Núcleo y Soporte: CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales, PerfilTributario, RegistroTributario, MotorDeCalculo, ConfirmacionTributaria, CargaAsistida, CatalogoJurisdiccional. **Fase 2** `[F2]` implementa las capacidades Derivadas: HomologacionFiscal, FormatoFiscal, EntregableFiscal, CertificadoTributario. Los decoradores `[F1]` y `[F2]` en los títulos de Sección 3 reflejan esta asignación.

**Restricción de fase:** Durante la implementación de Fase 1, el diseño del núcleo no debe incorporar ajustes motivados por necesidades de formatos regulatorios, reportes o certificados que aún no se implementan. Las capacidades derivadas consumirán el registro tributario tal como lo produce el núcleo. Salvo evidencia crítica de que un dato requerido por las derivadas no puede obtenerse después, las necesidades de Fase 2 deberán adaptarse al registro — no al revés.

**Aplica a:** Todos los agregados del bounded context. Clasificación visible en Sección 3.

### [D10] Event Sourcing como patrón de persistencia para todos los agregados

**Contexto:** El bounded context tiene 2 agregados transaccionales (RegistroTributario, EntregableFiscal) y 7 de configuración (CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales, PerfilTributario, HomologacionFiscal, FormatoFiscal). Los transaccionales usan ES. La pregunta es si los de configuración deben usar ES o CRUD con eventos de auditoría.

**Decisión:** Todos los agregados del bounded context usan Event Sourcing como patrón de persistencia. Los agregados de configuración persisten sus cambios como eventos inmutables en streams propios, con read models (proyecciones) para consulta.

**Justificación resumida:** (1) Inmutabilidad nativa para reconstrucción temporal regulatoria — demostrar ante la DIAN qué configuración aplicaba en una fecha específica. (2) Un solo modelo mental de persistencia para todo el bounded context — evita la carga cognitiva de mantener dos patrones. (3) Resiliencia operativa — proyecciones reconstruibles desde los streams. El costo de aplicar ES a agregados simples de configuración es bajo (streams cortos, eventos sencillos, sin sagas) y la alternativa (auditoría paralela en tablas) tiene problemas operativos conocidos.

**Análisis completo:** Ver `anexo-analisis-es-configuracion.md` — evaluación de 7 criterios (rendimiento, evolución de esquema, reconstrucción regulatoria, escalabilidad, operaciones masivas, mantenimiento, resiliencia) con matriz comparativa.

**Aplica a:** Todos los agregados del bounded context (Sección 3).

### [D11] Control de concurrencia, idempotencia y trazabilidad delegados a la plataforma

**Contexto:** El modelo usa Event Sourcing para todos los agregados `[D10]`. Los mecanismos de concurrencia, deduplicación y trazabilidad son concerns transversales de infraestructura.

**Decisión:** `expectedVersion` (control de concurrencia optimista): garantizada por el event store a nivel de stream. `idempotencyKey` (deduplicación de mensajes): garantizada por la plataforma de mensajería vía inbox/outbox pattern. `correlationId` (trazabilidad de procesos): propagado automáticamente por la plataforma en la cadena de mensajes. Este documento no especifica estos mecanismos por evento ni por comando — son garantías transversales de la plataforma de persistencia y mensajería. Si la plataforma cambia, revalidar que el nuevo stack provea estas tres garantías.

**Justificación:** Estos mecanismos son patrones de infraestructura (optimistic concurrency control, idempotent consumer, correlation identifier), no comportamiento de dominio. Especificarlos por evento duplicaría lo que la plataforma ya resuelve y contaminaría el modelo con concerns de infraestructura.

**Aplica a:** Todos los agregados del bounded context (Sección 3).

---

## 9. Premisas de negocio

Premisas que provienen del negocio, la regulación o la fiscalidad y que condicionan el diseño del modelo. No son decisiones arquitectónicas (D##) ni invariantes estructurales (I##) — son verdades externas al modelo que se toman como base.

| # | Premisa | Justificación | Aplica a |
|---|---|---|---|
| P1 | **El desglose fiscal se calcula a nivel de concepto, no de documento.** Cada concepto dentro de una transacción puede tener tratamiento tributario diferente según su clasificación tributaria. El motor evalúa cada concepto de forma independiente. | Normativa fiscal — un mismo documento puede contener bienes gravados al 19%, servicios gravados al 5% y bienes excluidos. El desglose varía por concepto. | MotorDeCalculo, RegistroTributario `[R16]` |
| P2 | **La dirección fiscal (gasto/ingreso) determina el comportamiento del tributo.** En gastos (OXP/CXP), la empresa es adquiriente y agente retenedor. En ingresos (CXC), la empresa es emisora, sujeto de retención o responsable del impuesto. Un mismo tributo (ej: IVA) cambia de comportamiento según la dirección (IVA generado en ingresos vs IVA soportado en gastos, retener vs ser retenido). La cualidad de descontable del IVA soportado es una determinación de declaración (Fase 2), no del cálculo individual. | Normativa fiscal — la dirección de la transacción define los roles tributarios de las partes. | MotorDeCalculo, CondicionDeAplicacion |
| P3 | **La configuración fiscal tiene dos orígenes: estándar y personalizado.** El contenido fiscal viene con el producto y evoluciona con actualizaciones. El contenido personalizado son excepciones configuradas por el cliente — tiene precedencia sobre el estándar y se preserva entre actualizaciones del producto. | Requisito de producto — el sistema debe cubrir la normativa general pero permitir excepciones por empresa. | CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales, HomologacionFiscal, FormatoFiscal |
| P4 | **Las autoridades fiscales pueden auditar configuración y cálculos tributarios años después de realizados.** El sistema debe poder reconstruir qué configuración estaba vigente y qué cálculos se produjeron en cualquier momento histórico. | Normativa fiscal — la DIAN, DGII y otras autoridades auditan períodos anteriores (hasta 5 años). Fundamenta `[D10]`. | Todos los agregados |
| P5 | **Los cambios de configuración fiscal operan con vigencia futura.** Reformas tributarias y ajustes de tarifa se configuran con fecha efectiva (vigenciaDesde). No aplican retroactivamente — el motor usa la fecha de la transacción para determinar qué configuración estaba activa. | Normativa fiscal — los cambios tributarios se anuncian con anticipación y tienen fecha de entrada en vigencia. | TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales |

---

## 10. Pendientes por definir

Aspectos del modelo que requieren definición futura. Esta sección consolida los pendientes de alcance general.

| # | Pendiente | Contexto | Condición de activación |
|---|-----------|----------|------------------------|
| PD1 | **Validación final de composición y diseño — agregados de cumplimiento fiscal.** Los agregados del frente de cumplimiento (HomologacionFiscal 3.8, FormatoFiscal 3.9, EntregableFiscal 3.10, CertificadoTributario 3.11) requieren validación con datos reales para confirmar que las secciones de formato cubren todos los campos exigidos por cada autoridad y que el flujo completo FormatoFiscal → HomologacionFiscal → EntregableFiscal/CertificadoTributario no tiene gaps. Relacionado con PD5, PD6 y PD7. | — | Cuando se inicie la implementación del frente de cumplimiento. |
| PD2 | **Localizaciones por país — contenido fiscal.** La configuración base está documentada en anexos separados por país (`anexo-configuracion-estandar-co.md`, `anexo-configuracion-estandar-do.md`, `anexo-configuracion-estandar-pa.md`) y resumida en Sección 3.16. Pendiente: (1) catálogo completo de conceptos de pago para RETEFUENTE (~50 conceptos DIAN), (2) tarifas de ICA/RICA para municipios principales más allá de Bogotá, (3) tablas de homologación por autoridad fiscal (DIAN, DGII, DGI) para el agregado HomologacionFiscal, (4) formatos fiscales de Panamá (DGI), (5) localizaciones por país como nivel 2 del glosario en el alcance. | Anexos v1.0 creados (CO: 11 tributos, DO: 5, PA: 4). Los ítems pendientes son datos operativos que se completan con fuentes normativas de cada jurisdicción. | Cuando se inicie la implementación de la carga de contenido fiscal. |
| PD3 | **Eventos de integración con otros bounded contexts.** Los eventos consumidos desde y publicados hacia otros bounded contexts (OXP, CXC, Contabilidad) no están especificados como contratos formales. El contrato semántico mínimo `[D9]` define la estructura de solicitud/confirmación, pero los eventos concretos de integración se definirán en la fase de EventCatalog. | Fase 3 del flujo de trabajo del proyecto. | Cuando se inicie la construcción del EventCatalog. |
| PD4 | **Declaraciones tributarias.** Las declaraciones tributarias (IVA, retención en la fuente, ICA, ITBIS, etc.) están diferidas a una fase futura tanto en el alcance (`definicion-alcance.md` v1.1, Sección 7 — "Fuera del alcance") como en este modelo. A diferencia de los reportes de información (exógena, municipales) que son consolidaciones de datos, las declaraciones tienen lógica propia significativa: renglones calculados, saldos a favor de períodos anteriores, compensaciones, sanciones, liquidación privada. Esta complejidad requiere un modelado dedicado que puede resultar en un agregado propio o en una extensión de `EntregableFiscal`. También es posible que se descarte como parte del producto si el análisis costo-beneficio no lo justifica. | Diferidas en alcance y modelo. `FormatoFiscal` ya soporta `tipoEntregable` extensible para incorporarlas sin romper el modelo actual. | Decisión de producto sobre si se incluyen o no. Si se incluyen, modelar antes de implementar el frente de cumplimiento. |
| PD5 | **Invariantes formales de FormatoFiscal.** FormatoFiscal es el único agregado de configuración sin invariantes formalizadas en Sección 6. Si existen restricciones implícitas (ej: al menos una sección por formato vigente, al menos un formato de salida, unicidad por autoridad + tipo), deberían formalizarse como I##. | Relacionado con PD1. | Cuando se diseñe el proceso completo del frente de cumplimiento. |
| PD6 | **Payload de `EntregableFiscalPresentado` — referencia al contenido.** El evento `EntregableFiscalPresentado` no captura referencia al contenido presentado (hash de archivos, versión del ContenidoGenerado). La trazabilidad de QUÉ contenido se presentó depende de reconstruir el stream desde el evento de generación previo. Evaluar si agregar `referenciaContenido` al payload. | Relacionado con PD1 y PD5. | Cuando se diseñe el proceso completo del frente de cumplimiento. |
| PD7 | **Documentación del proceso de generación masiva de certificados.** La generación masiva (todos los certificados de un período) se menciona en 3.11 como proceso de aplicación pero no está documentada como proceso orquestado: sin tracking de progreso, sin estrategia ante fallo parcial, sin correlationId de lote. Incluye la decisión implícita de que no tiene stream propio — evaluar si se formaliza como D##. | Relacionado con PD1 y PD6. | Cuando se inicie la implementación de CertificadoTributario. |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: 10 agregados, 45 eventos, 2 máquinas de estado, 12 invariantes, 11 decisiones de diseño, 7 pendientes. |
