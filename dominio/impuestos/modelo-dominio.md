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
| `definicion-alcance.md` | QUÉ hace el sistema | Fuente de verdad para glosario, actores, flujos y reglas de negocio vigentes (`[R##]`). No se duplica aquí. |
| **Este documento** | CÓMO se comporta el sub-dominio | Eventos, transiciones, precondiciones, invariantes, read models. |
| `guias-de-modelado/` | Criterios de modelado | Guías transversales de decisión: agregados, separación de responsabilidades, arquitectura EDA. Aplican a todos los sub-dominios. |
| EventCatalog (fase 3) | Catalogación técnica | Consumirá este documento como especificación de entrada durante la implementación. |

Las reglas de negocio se referencian como `[R##]` y su texto completo vive en `definicion-alcance.md`, Sección 6.

---

## 2. Convenciones del documento

### 2.1. Nomenclatura

- **Eventos:** PascalCase en español, sin tildes ni caracteres especiales (compatibilidad con código fuente). Ej: `RegistroTributarioCreado`, `PerfilTributarioActualizado`.
- **Referencias:** `[R##]` reglas de negocio, `[P##]` premisas, `[D##]` decisiones, `[I##]` invariantes, `[SI##]` sugerencias de implementación, `[PD##]` pendientes.
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

**Convención de payload en eventos `*Modificado`:** Los eventos de tipo `*Modificado` (ej: `TributoModificado`, `EntradaDeTarifaModificada`, `CondicionModificada`, `DefinicionAtributoModificada`, etc.) capturan **solo los campos que cambian** (delta), no el estado completo del recurso. Coherente con el patrón de Event Sourcing `[D10]`: el estado actual de un recurso se reconstruye reproduciendo el stream del agregado desde su `*Creado` y aplicando cada `*Modificado` posterior sobre el estado acumulado. En el documento, el payload de cada `*Modificado` se describe en dos partes: **(1) campos identificadores** — los que determinan a qué recurso interno se refiere el evento (ej: `codigo` en `TributoModificado`, `nombre` en `DefinicionAtributoModificada`); son inmutables dentro del ciclo de vida del recurso. **(2) campos modificables** — los que la operación cambió; reemplazan los valores previos al aplicar el evento. Los campos no listados conservan su valor previo. Las lecturas rápidas del estado actual se sirven desde **proyecciones / read models**, no desde snapshots dentro de los eventos.

**Patrón de eventos `*Definido` (upsert idempotente):** Algunos eventos del modelo (ej: `TratamientoDefinido`, `ReglaDeLocalizacionDefinida`) usan el verbo `*Definido` en lugar de `*Agregado` / `*Modificado`. Esto declara explícitamente que el evento es **idempotente**: emitirlo dos veces con la misma triple-clave produce el mismo resultado lógico. El verbo `Definido` cubre tanto la creación inicial como la actualización posterior — el agregado decide internamente si es un alta nueva o un reemplazo según la triple-clave del recurso. Se usa cuando la operación de configuración es naturalmente "establece este valor para esta combinación" sin distinguir entre crear y modificar.

**Identidad de entidades internas:** Cada entidad interna de un agregado (`Tributo`, `Condicion`, `Equivalencia`, `EntradaDeTarifa`, `DefinicionAtributo`, etc.) tiene una **identidad declarada explícitamente** que permite referenciarla en eventos de modificación o cierre. La identidad puede ser de dos formas: **(a) Combinación de atributos inmutables** — un conjunto de atributos que no pueden cambiar durante el ciclo de vida de la entidad. Ej: `Tributo` se identifica por `codigo`; `Condicion` por `(ambitoEvaluado + atributoEvaluado + tributoAfectado + direccionFiscalAplicable + origen + vigencia.fechaDesde)`. Modificar cualquier atributo de la combinación requiere cerrar la entrada y crear una nueva. **(b) Identificador único asignado al crear la entidad** — un código que vive durante todo el ciclo de vida de la entidad y se preserva en eventos posteriores. Ej: `entradaId` en `EntradaDeTarifa`, `actividadId` en `ActividadEconomicaRegistrada`. En cada agregado de la Sección 3 se documenta cuál mecanismo aplica a cada entidad interna; los eventos `*Modificado` y `*Cerrado` capturan la identidad explícitamente como primer campo de su payload.

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

### 2.5. Value Objects compartidos del Bounded Context

Algunos value objects se reutilizan en varios agregados del sub-dominio. Esta sección los lista una vez con su definición canónica; cada agregado que los usa los enumera en su tabla de VOs propios pero **comparte la misma definición estructural** — son el mismo concepto, no réplicas independientes. Esto preserva consistencia semántica entre agregados que conviven en el mismo bounded context.

| Value Object | Definición canónica | Agregados que lo usan |
|---|---|---|
| `Vigencia` | Rango temporal de validez de una entrada: `fechaDesde`, `fechaHasta` (opcional — abierto si no se cierra). Se usa para acotar temporalmente cualquier dato fiscal que pueda cambiar con vigencia futura (`[P5]`). | TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales, PerfilTributario, JurisdiccionFiscal, CatalogoDeRegimenesEspeciales, HomologacionFiscal, FormatoFiscal |
| `Origen` | Procedencia del contenido de configuración: `estándar` (provisto con el producto) o `personalizado` (configurado por el cliente). La precedencia es personalizado > estándar (`[P3]`). | CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales, JurisdiccionFiscal, CatalogoDeRegimenesEspeciales, HomologacionFiscal, FormatoFiscal |
| `AutoridadFiscal` | Autoridad destinataria de un entregable o equivalencia: nombre (DIAN, DGII, DGI, etc.), jurisdicción, país. | HomologacionFiscal, FormatoFiscal, EntregableFiscal, CertificadoTributario |
| `ReferenciaFormato` | Referencia al `FormatoFiscal` usado para la generación: ID, versión. | EntregableFiscal, CertificadoTributario |
| `ReferenciaHomologacion` | Referencia a la `HomologacionFiscal` usada para traducir valores internos a códigos de la autoridad: ID, versión. | FormatoFiscal (atributo raíz `referenciaHomologacion`), EntregableFiscal, CertificadoTributario |
| `Periodicidad` | Frecuencia de generación de un entregable: tipo (mensual / bimestral / trimestral / anual), meses aplicables. `[R29]` | FormatoFiscal |
| `FormatoDeSalida` | Formato técnico del archivo: tipo (XML / Excel / PDF), esquema o plantilla de referencia. Un formato puede tener múltiples salidas. `[R27]` | FormatoFiscal |

Las tablas de VOs en cada agregado de la Sección 3 listan estos VOs por completitud (no obligan a saltar a esta sección), pero la definición canónica vive aquí. Si un VO compartido evoluciona, se actualiza una sola vez aquí y se propaga a todos los agregados consumidores.

---

## 3. Bounded Context y Agregados

### Clasificación de capacidades

El bounded context de Impuestos agrupa capacidades con distinto nivel de centralidad. Esta clasificación no implica separación en bounded contexts distintos — todas conviven dentro del mismo BC — pero establece una jerarquía de dependencia: las capacidades derivadas consumen el núcleo, no lo redefinen. `[D7]`

| Nivel | Capacidades | Agregados / Servicios | Fase |
|---|---|---|---|
| **Núcleo** | Configuración tributaria, determinación/cálculo, perfil tributario, registro tributario, jurisdicciones fiscales y regímenes especiales empresariales, confirmación de transacciones | CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales, PerfilTributario, JurisdiccionFiscal, CatalogoDeRegimenesEspeciales, RegistroTributario, MotorDeCalculo, ConfirmacionTributaria | `[F1]` |
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
│  ┌────────┴──────────┐  ┌────────┴────────────────────────┴──────────┐ │
│  │ JurisdiccionFiscal│  │  CatalogoDeRegimenesEspeciales              │ │
│  │  (agregado) [D12] │  │   (agregado) [D13]                          │ │
│  └────────┬──────────┘  └────────┬────────────────────────────────────┘ │
│           │                      │                                      │
│           └──────────┬───────────┘────────────────────────┐             │
│                      ▼                                    ▼             │
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
│                          │PerfilTributario│◄── CatalogoDeRegimenesEsp.  │
│                          │  (agregado)    │    (validación referencial  │
│                          └────────────────┘     [I16])                  │
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
- Diseño dimensional documentado en `anexo-diseno-dimensional.md` (los anexos del sub-dominio se versionan de forma independiente; ver el _Control de versiones_ al final de cada anexo).
- El diagrama muestra solo las **dependencias directas** entre agregados. Las dependencias **transitivas** (ej: el motor lee `CatalogoDeAtributosFiscales` vía `CondicionDeAplicacion` y vía `PerfilTributario`) se documentan en la Sección 3.14 — ver "Agregados que lee directamente" y "Dependencias transitivas" del `MotorDeCalculo`.

### 3.2. Agregado: CatalogoTributario `[F1]`

- **Raíz:** Catálogo tributario de un país. Agrupa tributos, clasificaciones y la matriz de tratamiento que determina qué tributos aplican a qué clasificaciones. No contiene tarifas ni condiciones por perfil (dimensiones independientes).
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `catalogo-tributario-{id}`
- **Eventos propios:** 9 — ver Sección 5.2.1.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `Tributo` | Carga fiscal aplicable en la jurisdicción. Cada tributo declara su `factorDeTarifa` — el tipo de dato que el motor usa para buscar la tarifa en TarifaTributaria — y su `direccionFiscalAplicable` — las direcciones fiscales en las que el tributo existe normativamente. El valor `ambas` actúa como **comodín de aplicabilidad** (el tributo aplica tanto en `gasto` como en `ingreso`), no como una dirección fiscal por sí misma — la `direccionFiscal` real de la transacción solo puede ser `gasto` o `ingreso`. **Identidad:** `codigo` (código semántico legible asignado al crear el tributo, ej: `IVA`, `RETEFUENTE`, `ICA`). El `codigo` es **inmutable** durante todo el ciclo de vida del tributo porque es referenciado semánticamente desde los `RegistroTributario` históricos (cada `LineaDeDesglose` captura el código como snapshot inmutable `[D4]`). Cambiar el código rompería la trazabilidad histórica del catálogo. Cuando la normativa cambia o se necesita un tributo distinto, se sigue el patrón general `[P5]`: se desactiva el tributo actual y se agrega uno nuevo con código y vigencia propios. Los criterios de qué otros atributos son modificables vs. requieren el patrón "desactivar + nuevo" dependen de la política de corrección de errores — ver `[PD12]`. `[R03]` | Código, nombre, naturaleza (aditivo/sustractivo), caracterRetención (anticipado/definitivo), nivelJurisdiccional (nacional/municipal/estatal), factorDeTarifa, **direccionFiscalAplicable** (gasto/ingreso/ambas — default: ambas; `ambas` es comodín de aplicabilidad, no una dirección de transacción), tributoPadre, origen. |
| `ClasificacionTributaria` | Categoría que agrupa bienes/servicios según tratamiento tributario. **Identidad:** `codigo` (código semántico legible, ej: `GRAV_19`, `EXENTO`, `INC_04`). Inmutable por la misma razón que `Tributo.codigo` — los `RegistroTributario` y los conceptos enviados por los sub-dominios consumidores lo referencian semánticamente (`[I26]`). Los criterios de modificabilidad de los demás atributos dependen de la política de corrección — ver `[PD12]`. `[R01]` `[I26]` | Código, nombre, descripción, origen. |
| `Tratamiento` | Define si un tributo aplica o no a una clasificación. Tiene identidad porque puede ser sobrescrito por origen personalizado. | Tributo, clasificación, aplica (sí/no), origen. |
| `ReglaDeLocalizacion` | Define qué rol de ubicación determina la jurisdicción fiscal para un tributo en una clasificación dada. Contenido fiscal del producto. El motor la consulta para resolver cuál de las ubicaciones enviadas por el consumidor es la fiscalmente relevante. `[R34]` | Tributo, clasificación (o `*` para todas), rolQueManda (sedeEmisora / sedeContraparte / lugarEjecucion), rolFallback (opcional), origen. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `Origen` | Procedencia del contenido: `estándar` (contenido fiscal precargado, actualizable con el producto) o `personalizado` (excepción configurada por el usuario, tiene precedencia). |

**Direccionalidad inherente del tributo (`direccionFiscalAplicable`):**

Algunos tributos solo existen normativamente en una dirección fiscal. El atributo `direccionFiscalAplicable` declara esta invariante a nivel del agregado, permitiendo que el motor filtre el tributo antes de evaluar sus condiciones:

- **`ambas`** (default): el tributo aplica en gasto e ingreso. La mayoría de los tributos directos (IVA, RETEFUENTE, RIVA, ICA, RICA) tienen direccionalidad bidireccional — su comportamiento específico se modela vía condiciones.
- **`ingreso`**: el tributo solo existe cuando la emisora es facturadora. Aplica a autorretenciones que el sujeto pasivo practica sobre sus propios ingresos (AUTO_RETEFUENTE, AUTO_RICA, AUTO_RENTA).
- **`gasto`**: el tributo solo existe cuando la emisora es adquiriente. Aplica a tributos autoliquidados por reverseCharge (AUTO_RIVA en importación de servicios).

Esta declaración es una **invariante normativa del agregado**: AUTO_RETEFUENTE no existe en gasto, sin importar las condiciones que se configuren. El motor descarta el tributo entero antes de evaluar sus condiciones cuando la dirección fiscal de la transacción no coincide.

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
- **actividadEconomica:** La tarifa depende de la actividad económica (código CIIU/equivalente) del sujeto pasivo del tributo en una jurisdicción específica. El motor invoca `PerfilTributario.actividadEconomicaPara(jurisdiccion, clasificacion, fecha)` para resolver el CIIU aplicable entre las `ActividadEconomicaRegistrada` del sujeto pasivo, con precedencia descendente (jurisdicción + clasificación → jurisdicción → clasificación → catch-all principal). El CIIU resuelto se usa como factor de búsqueda dentro del stream de la jurisdicción correspondiente (`tarifa-CO-11001-ICA` ≠ `tarifa-CO-05001-ICA`). Dos ciudades pueden asignar tarifas distintas a la misma actividad, y una misma entidad puede tener actividades diferentes según ciudad o tipo de bien/servicio. `[D12]`
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

- **Raíz:** Tabla de tarifas de un tributo específico en una jurisdicción específica. Cada instancia es un stream independiente cuya clave se compone del código de la jurisdicción (referencia a `JurisdiccionFiscal.codigo`) y el código del tributo. Para tributos nacionales el stream usa solo el código de país (ej: `tarifa-CO-IVA`); para tributos subnacionales incluye el código de la jurisdicción municipal/estatal (ej: `tarifa-CO-11001-ICA` para Bogotá D.C.). En la documentación didáctica se usan abreviaturas como `BOG`, `MED` por legibilidad, pero la implementación usa los códigos del catálogo `JurisdiccionFiscal` (`[D12]`). Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar `[P3]`.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `tarifa-{id}`
- **Eventos propios:** 4 — ver Sección 5.2.2.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `EntradaDeTarifa` | Una fila de la tabla: factor → tarifa. El significado del factor lo define el `factorDeTarifa` del tributo correspondiente en CatalogoTributario — este agregado solo almacena y busca por coincidencia exacta. **Identidad:** `entradaId` (identificador único asignado al crear la entrada) — se preserva durante todo el ciclo de vida y permite referenciarla en `EntradaDeTarifaModificada` y `EntradaDeTarifaCerrada`. Los campos `factor` y `vigencia.fechaDesde` son inmutables — para cambiarlos, cerrar la entrada y agregar una nueva (ver `[I25]`). `[R06]` `[R07]` | entradaId, factor, tarifa, tipoTarifa (porcentaje/específica), cuantíaMínima (VO, opcional), vigencia (VO), origen. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `Vigencia` | Rango temporal de validez: fechaDesde, fechaHasta. Los cambios fiscales se configuran con fecha efectiva — el motor resuelve la tarifa aplicable a partir de `fechaTransaccion`, sin aplicación retroactiva `[P5]`. Protege `[R08]` (no solapamiento dentro del mismo factor y origen). |
| `CuantiaMínima` | Umbral por debajo del cual el tributo no aplica. Valor, unidadDeReferencia (UVT, UMA, COP, USD, etc.). Opcional — no todas las entradas tienen cuantía mínima. |
| `Origen` | Procedencia del contenido: `estándar` o `personalizado` (precedencia personalizado > estándar). |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  TarifaTributaria (Agregado)                                  │
│                                                              │
│  jurisdiccionFiscal.codigo · tributo · origen                 │
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

- **Raíz:** Conjunto de condiciones de un país que modifican la aplicación de tributos según perfiles tributarios o atributos de las jurisdicciones fiscales involucradas en la transacción. Cada condición evalúa un atributo de una entidad fiscal (emisora/contraparte — atributos del `PerfilTributario`) o de una jurisdicción (sedeEmisora/sedeContraparte/lugarEjecucion/jurisdicción resuelta del tributo — atributos de `JurisdiccionFiscal`) y produce un efecto sobre un tributo específico. Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar `[P3]`.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `condicion-{id}`
- **Eventos propios:** 4 — ver Sección 5.2.3.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `Condicion` | Regla que evalúa un atributo del perfil tributario o de una jurisdicción fiscal y produce un efecto sobre un tributo. Cuando `ambitoEvaluado` referencia un rol fiscal (`emisora`/`contraparte`), `atributoEvaluado` referencia una `DefinicionAtributo` del `CatalogoDeAtributosFiscales` (atributo del `PerfilTributario`). Cuando `ambitoEvaluado` referencia una jurisdicción (`sedeEmisora.jurisdiccion`/`sedeContraparte.jurisdiccion`/`lugarEjecucion.jurisdiccion`/`jurisdiccionResuelta`), `atributoEvaluado` referencia un atributo del agregado `JurisdiccionFiscal` (`codigo`, `nombre`, `nivelJurisdiccional`, `tipo`, `tipoRegimen`). Si existen condición estándar y condición personalizada para la misma combinación (ambitoEvaluado + atributoEvaluado + tributoAfectado), aplica la personalizada. **Identidad:** combinación inmutable de atributos `(ambitoEvaluado + atributoEvaluado + tributoAfectado + direccionFiscalAplicable + origen + vigencia.fechaDesde)`. Para modificar cualquiera de estos atributos identificadores, se cierra la condición existente (`CondicionCerrada`) y se agrega una nueva (`CondicionAgregada`) — no se permite `CondicionModificada` sobre ellos. Los campos modificables vía `CondicionModificada` son: `valorEsperado`, `efecto`, `tarifaAlternativa`, `vigencia.fechaHasta` (ver `[I24]`). `[R10]` `[R11]` `[R35]` `[I15]` | AmbitoEvaluado (emisora/contraparte/sedeEmisora.jurisdiccion/sedeContraparte.jurisdiccion/lugarEjecucion.jurisdiccion/jurisdiccionResuelta), atributoEvaluado (ref. a DefinicionAtributo si el rol es de perfil, o nombre de atributo de `JurisdiccionFiscal` si el rol es de jurisdicción), valorEsperado, tributoAfectado, efecto (VO), tarifaAlternativa (si efecto = cambiarTarifa), **direccionFiscalAplicable** (gasto/ingreso/ambas — default: ambas), vigencia (VO), origen. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `Vigencia` | Rango temporal de validez: fechaDesde, fechaHasta. |
| `Efecto` | Resultado de la evaluación: `noAplicar` (tributo se excluye del desglose), `cambiarTarifa` (se usa tarifaAlternativa), `reverseCharge` (el tributo original `T` se reemplaza por el tributo alternativo `T'` **siempre que `T'` sea aplicable en la dirección fiscal actual** — verificado contra `T'.direccionFiscalAplicable`; si `T'` no es aplicable, `T` continúa su evaluación normal). |
| `Origen` | Procedencia del contenido: `estándar` o `personalizado` (precedencia personalizado > estándar). |

**Convención de roles `emisora` / `contraparte` (lenguaje fiscal del dominio):**

Estos son **roles posicionales fiscales**, no comerciales. La convención del dominio (alineada con SAP Legal Entity, Oracle First/Third Party, Dynamics Counterparty):

- **`emisora`**: entidad operadora del ERP en la transacción. Típicamente la empresa cliente; en escenarios de facturación a nombre de terceros (inmobiliario, mandante por proyecto), es el tercero gestionado. Su rol comercial cambia según `direccionFiscal`: adquiriente en `gasto`, facturadora en `ingreso`.
- **`contraparte`**: la otra parte. Proveedor en `gasto`, cliente en `ingreso`.

Lo que cambia entre direcciones NO es quién juega cada rol fiscal, sino el rol **comercial** que la emisora desempeña.

**Direccionalidad de la condición (`direccionFiscalAplicable`):**

Algunas condiciones tienen perspectiva fiscal específica — su semántica normativa es válida solo en una dirección. Por ejemplo: "si el proveedor es exento de retefuente no le retengo" tiene sentido solo en `gasto` evaluando `contraparte`; en `ingreso` la regla equivalente evalúa `emisora`. El campo `direccionFiscalAplicable` permite declarar explícitamente cuándo se evalúa una condición:

- **`ambas`** (default): la condición se evalúa en cualquier dirección. Aplica a reglas bilaterales por naturaleza (ej: "régimen simple no retiene ni le retienen") o a condiciones default.
- **`gasto`**: la condición se evalúa solo cuando `direccionFiscal=gasto`. Aplica a reglas con perspectiva del lado adquiriente.
- **`ingreso`**: la condición se evalúa solo cuando `direccionFiscal=ingreso`. Aplica a reglas con perspectiva del lado facturador.

Cuando una regla normativa es bilateral pero se expresa con perspectiva fiscal específica en cada dirección, se modela como **dos condiciones separadas** (una para gasto evaluando `contraparte`, otra para ingreso evaluando `emisora`). Esta explicitud direccional preserva el lenguaje fiscal del dominio sin ambigüedad semántica.

**Evaluación de jurisdicciones fiscales (`[D12]`):**

Además de los roles `emisora`/`contraparte` (atributos del `PerfilTributario`), el campo `ambitoEvaluado` admite cuatro valores que referencian **jurisdicciones fiscales** del catálogo `JurisdiccionFiscal` (Sección 3.7). El motor resuelve estas jurisdicciones contra el catálogo al inicio del cálculo (paso 2.b del motor, Sección 3.14) y las pone disponibles para evaluación de condiciones:

| Rol de jurisdicción | Qué referencia | Cuándo usarlo |
|---|---|---|
| `sedeEmisora.jurisdiccion` | `JurisdiccionFiscal` resuelta desde `ubicaciones.sedeEmisora.subnacional` | Condiciones que dependen de dónde opera fiscalmente la emisora (ej: gran contribuyente de Bogotá depende de sede en Bogotá) |
| `sedeContraparte.jurisdiccion` | `JurisdiccionFiscal` resuelta desde `ubicaciones.sedeContraparte.subnacional` | Condiciones que dependen de dónde opera fiscalmente la contraparte |
| `lugarEjecucion.jurisdiccion` | `JurisdiccionFiscal` resuelta desde `ubicaciones.lugarEjecucion.subnacional` | Condiciones que dependen del lugar de prestación del servicio o entrega del bien (ej: régimen Puerto Libre San Andrés exime IVA si el lugar de ejecución es San Andrés) |
| `jurisdiccionResuelta` | `JurisdiccionFiscal` que ganó por `ReglaDeLocalizacion` para el tributo en evaluación | Condiciones que dependen de la jurisdicción específica que tributa (ej: regla que aplica solo cuando la jurisdicción del tributo tiene cierto `tipoRegimen`) |

**Atributos de `JurisdiccionFiscal` evaluables:** `codigo` (string), `nombre` (string), `nivelJurisdiccional` (nacional/estatal/provincial/departamental/municipal/distrital), `tipo` (territorial-administrativa/regimen-especial-territorial/distrito-fiscal-especial/soberania-tributaria), `tipoRegimen` (string opcional — clasificación categórica). Ver Sección 3.7.

**Ejemplo — Puerto Libre San Andrés (Colombia):** Una sola condición cubre cualquier transacción donde el lugar de ejecución pertenezca a una jurisdicción con régimen Puerto Libre — sin necesidad de declarar una condición por cada código municipal:

```
Condicion (estándar):
  tributoAfectado:           IVA
  ambitoEvaluado:           lugarEjecucion.jurisdiccion
  atributoEvaluado:          tipoRegimen
  valorEsperado:             "puerto-libre"
  efecto:                    noAplicar
  direccionFiscalAplicable:  ambas
```

Al modelar regímenes territoriales por `tipoRegimen` (atributo categórico de la jurisdicción) en lugar de por `codigo` específico, una condición agrupa todas las jurisdicciones de la misma categoría (los 3 municipios del archipiélago de San Andrés, los 43 municipios de Frontera Norte MX, etc.) y se evita la proliferación de reglas.

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  CondicionDeAplicacion (Agregado)                             │
│                                                              │
│  pais · origen                                               │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Condicion #1 (Entidad)                                 │  │
│  │  ambitoEvaluado: contraparte                          │  │
│  │  atributoEvaluado: esAutorretenedora                   │  │
│  │  valorEsperado: true · tributoAfectado: RETEFUENTE     │  │
│  │  ○ Efecto { noAplicar }                                │  │
│  │  ○ Vigencia { 2026-01-01 → ∞ }                        │  │
│  │  origen: estándar                                      │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Condicion #2 (Entidad)                                 │  │
│  │  ambitoEvaluado: emisora                              │  │
│  │  atributoEvaluado: esGranContribuyente                 │  │
│  │  valorEsperado: true · tributoAfectado: RIVA           │  │
│  │  ○ Efecto { cambiarTarifa, tarifaAlternativa: "Ag.dsg."}│  │
│  │  ○ Vigencia { 2026-01-01 → ∞ }                        │  │
│  │  origen: estándar                                      │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Condicion #3 (Entidad)                                 │  │
│  │  ambitoEvaluado: lugarEjecucion.jurisdiccion          │  │
│  │  atributoEvaluado: tipoRegimen                         │  │
│  │  valorEsperado: puerto-libre                           │  │
│  │  tributoAfectado: IVA                                  │  │
│  │  ○ Efecto { noAplicar }                                │  │
│  │  ○ Vigencia { 1991-01-01 → ∞ }                        │  │
│  │  origen: estándar                                      │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  condicionesVigentesA(fecha)                           │  │
│  │    → filtra por vigencia, precedencia personalizado > estándar  │  │
│  │  _(la evaluación contra el contexto de transacción      │  │
│  │   la realiza el MotorDeCalculo — ver Sección 3.14)_     │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `condicionesVigentesA(fecha)` | Filtra condiciones cuya vigencia contenga la fecha. Si existen condición estándar y condición personalizada para la misma combinación (atributo + tributo), retorna la del personalizado (precedencia). |
| _(`evaluar(...)` reclasificado como operación del `MotorDeCalculo` — ver Sección 3.14)_ | La **evaluación** de las condiciones contra el contexto de una transacción (perfiles + jurisdicciones + tributos candidatos) es responsabilidad del motor de cálculo, no del catálogo. El catálogo declara qué condiciones existen y permite consultarlas por vigencia; el motor las aplica al contexto de cada transacción para determinar el efecto sobre cada tributo. |

Decisiones de diseño aplicadas: `[D1]` Raíz por país. `[D2]` Diseño dimensional — tercera dimensión del pipeline.

### 3.5. Agregado: CatalogoDeAtributosFiscales `[F1]`

- **Raíz:** Catálogo que define qué atributos fiscales existen para un país, con su tipo, valores válidos y vigencia de la definición misma. Es el esquema contra el cual PerfilTributario valida sus datos y CondicionDeAplicacion referencia sus evaluaciones. Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar `[P3]`.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `catalogo-atributos-{id}`
- **Eventos propios:** 4 — ver Sección 5.2.4.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `DefinicionAtributo` | Metadato de un atributo fiscal: qué es, qué tipo tiene, qué valores acepta y cuándo está vigente. Cuando un atributo deja de existir en la normativa, se cierra su `VigenciaDefinicion` — los perfiles existentes conservan el valor histórico pero el motor ya no lo evalúa. El dominio de valores se especifica de una de dos formas según la cardinalidad: `valoresValidos` (enum embebido, valores estables y de baja cardinalidad — ej: `regimenTributario: [Ordinario, Simple, Especial]`) o `catalogoReferencia` (nombre del agregado externo que mantiene los valores válidos cuando son numerosos, evolucionan independientemente o tienen vigencia propia — ej: `CatalogoDeRegimenesEspeciales` para los 121 códigos de zonas francas CO). Las dos formas son mutuamente exclusivas. `[D13]` | Nombre, tipo (boolean/enum/string/numerico), valoresValidos (lista, solo si tipo = enum y dominio embebido), catalogoReferencia (nombre del agregado de referencia, solo si tipo = enum y dominio externo), requerido (sí/no), vigenciaDefinicion (VO), origen. |

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
│  ┌────────────────────────────────────────────────────────┐  │
│  │ DefinicionAtributo #4 (Entidad)  ← CATÁLOGO EXTERNO   │  │
│  │  nombre: regimenZonaFranca · tipo: enum                │  │
│  │  catalogoReferencia: CatalogoDeRegimenesEspeciales     │  │
│  │  requerido: no · origen: estándar                      │  │
│  │  ○ VigenciaDefinicion { 2018-01-01 → ∞ }              │  │
│  │  (valor se valida contra el catálogo externo)          │  │
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
| `validarValor(nombre, valor, fecha)` | Este catálogo valida **solo lo que conoce**: (1) existe una `DefinicionAtributo` vigente con ese nombre, (2) el valor es del tipo correcto, (3) si es enum con dominio embebido, el valor está en `valoresValidos`. **Cuando el atributo tiene dominio en un catálogo externo** (`catalogoReferencia` poblado, ej: códigos de zona franca en `CatalogoDeRegimenesEspeciales`), el catálogo de atributos fiscales **no verifica** que el valor exista en ese catálogo externo — esa verificación es **responsabilidad del `PerfilTributario`** al guardar el atributo (cubierta por `[I16]`). El split mantiene cada catálogo dueño de su propia regla: el de atributos sabe de tipos y listas embebidas; el de regímenes sabe de códigos vigentes; el perfil tributario verifica la consistencia entre ambos al persistir. Usado por `PerfilTributario` como precondición de escritura. |
| `atributosRequeridos(fecha)` | Retorna las definiciones vigentes con `requerido = sí`. Permite identificar perfiles incompletos. |

Decisiones de diseño aplicadas: `[D1]` Raíz por país. `[D3]` Catálogo de atributos validado. `[D13]` Patrón `valoresValidos` vs `catalogoReferencia` para dominio embebido vs externo.

**Patrón `valoresValidos` vs `catalogoReferencia`:**

El catálogo de atributos fiscales soporta dos formas de declarar el dominio de valores válidos para atributos `tipo = enum`:

| Forma | Cuándo usar | Ejemplos |
|---|---|---|
| `valoresValidos` (lista embebida) | Dominio cerrado, baja cardinalidad (≤ ~10 valores), valores estables que cambian poco con el tiempo. La lista vive dentro del propio `CatalogoDeAtributosFiscales`. | `regimenTributario: [Ordinario, Simple, Especial]`, `tipoContribuyente: [Persona, Sociedad]`, `responsabilidadIVA: [Responsable, NoResponsable]`. |
| `catalogoReferencia` (agregado externo) | Dominio extenso (decenas o cientos de valores), valores que evolucionan independientemente del catálogo de atributos, valores con vigencia propia o metadatos adicionales (autoridad, ubicación, tipo). El catálogo externo es responsable de mantener la lista vigente. | `regimenZonaFranca` → `CatalogoDeRegimenesEspeciales` (121 ZFs CO), `inscripcionMonopolio` → `CatalogoDeRegimenesEspeciales`. |

Las dos formas son **mutuamente exclusivas** dentro de una misma `DefinicionAtributo`: si `catalogoReferencia` está poblado, `valoresValidos` debe estar vacío y viceversa. Validación referencial: cuando `catalogoReferencia` está poblado, la validación del valor contra el catálogo externo se realiza en `PerfilTributario` al escribir el `AtributoFiscal` correspondiente (ver `[I16]`).

### 3.6. Agregado: PerfilTributario `[F1]`

- **Raíz:** Perfil fiscal de una entidad (empresa o tercero) **en un país específico**. Cada combinación (entidad × país) genera un perfil independiente — un tercero que opera en Colombia y República Dominicana tiene dos perfiles con identificaciones fiscales diferentes (NIT vs RNC), atributos diferentes y catálogos de validación diferentes. Contiene los atributos que el MotorDeCalculo y las CondicionDeAplicacion evalúan para determinar qué tributos aplican y con qué tratamiento, y las actividades económicas registradas (con multiplicidad por jurisdicción y/o clasificación) que el motor usa para resolver tarifas de tributos cuyo `factorDeTarifa` es `actividadEconomica` (ICA, RICA, etc.). Cada atributo se valida contra el CatalogoDeAtributosFiscales del país correspondiente (tipo, valores válidos, vigencia de la definición). No es transaccional — es un atributo de la entidad que evoluciona cuando cambia su situación fiscal.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `perfil-tributario-{id}`
- **Eventos propios:** 7 — ver Sección 5.2.5.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `AtributoFiscal` | Dato fiscal individual de la entidad. `nombre` referencia una `DefinicionAtributo` del CatalogoDeAtributosFiscales — no es un string libre. `valor` se valida contra el tipo y valores permitidos de la definición. Cada atributo tiene vigencia temporal — permite registrar cambios históricos (ej: empresa que pasa de régimen simple a ordinario). | Nombre (ref. a DefinicionAtributo), valor, vigencia (VO), fuenteDeAutoridad (VO, opcional). |
| `ActividadEconomicaRegistrada` | Actividad económica que la entidad fiscal realiza, declarada con multiplicidad por jurisdicción y/o clasificación tributaria. Una entidad puede tener varias actividades registradas que reflejan la realidad fiscal: empresa con CIIU 6201 (software) en Bogotá y CIIU 6810 (arrendamiento de inmuebles) en Medellín; o empresa con CIIU principal y CIIUs específicos por tipo de bien/servicio. Cuando `jurisdiccion` y `clasificacionAplicable` son ambos `null`, la entrada representa la actividad principal de la entidad (catch-all). Cuando uno o ambos están poblados, la entrada es específica para esa combinación y tiene precedencia sobre el catch-all. El motor de cálculo invoca `actividadEconomicaPara(jurisdiccion, clasificacion, fecha)` para resolver qué CIIU usar según contexto. Cada entrada tiene vigencia temporal. **Identidad:** `actividadId` (identificador único asignado al crear la entrada) — se preserva durante todo el ciclo de vida y permite referenciarla en `ActividadEconomicaRegistradaModificada` y `ActividadEconomicaRegistradaCerrada`. Los campos `ciiu`, `jurisdiccion` y `clasificacionAplicable` son inmutables — para cambiarlos, cerrar la entrada y agregar una nueva. Los campos modificables vía `*Modificada` son: `vigencia.fechaHasta`, `fuenteDeAutoridad`. `[D12]` | actividadId, Ciiu (string — código CIIU/equivalente del país), jurisdiccion (opcional, ref a `JurisdiccionFiscal.codigo` del país), clasificacionAplicable (opcional, ref a `ClasificacionTributaria.codigo`), vigencia (VO), fuenteDeAutoridad (VO, opcional). |

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
│                                                              │
│  ── Actividades económicas registradas ───────────────────── │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ActividadEconomicaRegistrada #1 (Entidad)              │  │
│  │  ciiu: "4711"  (principal, catch-all)                  │  │
│  │  jurisdiccion: —  ·  clasificacionAplicable: —         │  │
│  │  ○ Vigencia { 2020-01-01 → ∞ }                        │  │
│  │  ○ FuenteDeAutoridad { RUT, 2020-01-15 }              │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ActividadEconomicaRegistrada #2 (Entidad)              │  │
│  │  ciiu: "6810"  (específica para arrendamientos)        │  │
│  │  jurisdiccion: 11001  ·  clasificacionAplicable: —     │  │
│  │  ○ Vigencia { 2022-06-01 → ∞ }                        │  │
│  │  ○ FuenteDeAutoridad { RIT-Bogotá, 2022-05-15 }       │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  atributoVigenteA(nombre, fecha)                       │  │
│  │    → último valor vigente del atributo a esa fecha     │  │
│  │  perfilCompletoA(fecha)                                │  │
│  │    → mapa { nombre → valor } de todos los atributos    │  │
│  │      vigentes a la fecha                               │  │
│  │  actividadEconomicaPara(jurisdiccion, clasificacion,   │  │
│  │                          fecha)                         │  │
│  │    → CIIU resuelto por precedencia: específica         │  │
│  │      (jurisd+clasif), por jurisd, por clasif, catch-all │  │
│  │  regimenesEspecialesVigentes(fecha)                    │  │
│  │    → lista de códigos de RegimenEspecial activos        │  │
│  │      (de AtributoFiscal con catalogoReferencia          │  │
│  │       = CatalogoDeRegimenesEspeciales)                  │  │
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
| `actividadEconomicaPara(jurisdiccion, clasificacion, fecha)` | Retorna el código CIIU aplicable según el contexto de la transacción, resolviendo entre las `ActividadEconomicaRegistrada` vigentes por precedencia descendente: (1) entrada que coincida con jurisdicción + clasificación, (2) entrada que coincida solo con jurisdicción (sin clasificación), (3) entrada que coincida solo con clasificación (sin jurisdicción), (4) entrada catch-all (sin jurisdicción ni clasificación — la actividad principal). Si ninguna coincide, retorna `null` y el motor descarta el tributo con `motivoExclusion: actividad_no_registrada`. Usado por el `MotorDeCalculo` en el paso 2.d cuando `Tributo.factorDeTarifa = actividadEconomica`. `[D12]` |
| `regimenesEspecialesVigentes(fecha)` | Retorna la lista de códigos de `RegimenEspecial` en los que la entidad fiscal está inscrita y vigentes a la fecha. Se construye recorriendo los `AtributoFiscal` cuya `DefinicionAtributo` asociada tiene `catalogoReferencia: "CatalogoDeRegimenesEspeciales"` y filtrando por vigencia del propio atributo. **Dependencia de lectura cross-aggregate:** este comportamiento consulta el `CatalogoDeAtributosFiscales` del país para identificar qué `AtributoFiscal` del perfil tienen `catalogoReferencia` poblado y por lo tanto representan inscripciones en regímenes especiales. Esto se documenta explícitamente porque rompe el principio general "un agregado se construye solo con sus eventos" — el agregado consume metadatos del catálogo de atributos para interpretar correctamente los valores que él mismo almacena. La consulta es estática (no escribe en el catálogo) y se permite porque el `CatalogoDeAtributosFiscales` es la fuente única de verdad sobre la semántica de los atributos del perfil. **Coherencia con cambios del catálogo:** la lista de atributos del perfil cuyo valor referencia el `CatalogoDeRegimenesEspeciales` se reconstruye dinámicamente en cada consulta, leyendo qué `DefinicionAtributo` del país tienen actualmente `catalogoReferencia: "CatalogoDeRegimenesEspeciales"`. Si el catálogo de atributos evoluciona (ej: el producto agrega una nueva definición de atributo asociada al catálogo de regímenes empresariales en una actualización), las consultas posteriores reflejan el cambio automáticamente — no requiere sincronización ni caché. Los valores ya almacenados en el perfil se conservan tal como están; solo se reinterpreta cuáles cuentan como inscripciones a regímenes según la última definición del catálogo de atributos. Usado por el `MotorDeCalculo` en el paso 2.c para evaluar condiciones que activan tratamiento diferenciado por inscripción empresarial (zona franca, monopolio, decreto individual). La validación referencial al momento de la escritura del atributo la realiza este agregado contra el `CatalogoDeRegimenesEspeciales` del país (ver `[I16]`). `[D13]` |

**Frontera con `AtributoFiscal.actividadEconomica` (atributo simple):**

En el modelo previo a `[D12]`, la actividad económica se modelaba como un único atributo simple (`AtributoFiscal { nombre: "actividadEconomica", valor: "4711" }`) sin distinción por jurisdicción ni clasificación. La nueva entidad `ActividadEconomicaRegistrada` reemplaza ese modelo simple — para cargas iniciales y migraciones, una entidad con una sola actividad principal (catch-all con `jurisdiccion: null, clasificacionAplicable: null`) es funcionalmente equivalente al atributo simple anterior. La cláusula `actividadEconomica` en `CatalogoDeAtributosFiscales` queda obsoleta y se retira de la precarga estándar (ver `[D14]` para la decisión de diseño y `[PD9]` para el plan de migración). El motor consulta `actividadEconomicaPara()` exclusivamente; no consulta el atributo simple.

Decisiones de diseño aplicadas: `[D3]` Catálogo de atributos validado. `[D12]` Catálogo de jurisdicciones fiscales independiente — habilita la resolución de actividad económica por jurisdicción. `[D13]` Catálogo de regímenes especiales empresariales — el perfil enlaza los regímenes en que la entidad está inscrita mediante atributos con `catalogoReferencia`.

### 3.7. Agregado: JurisdiccionFiscal `[F1]`

- **Raíz:** Catálogo de jurisdicciones fiscales de un país. Identifica los ámbitos territoriales y especiales donde aplica tributación, con sus atributos fiscales (tipo, régimen, vigencia). Es el catálogo que `ubicaciones.subnacional` del contrato del motor referencia, y que `TarifaTributaria` usa como dimensión de stream key. Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar `[P3]`. `[D12]`
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `jurisdiccion-fiscal-{pais}`
- **Eventos propios:** 4 — ver Sección 5.2.6.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `Jurisdiccion` | Ámbito fiscal del catálogo. Identifica una jurisdicción (nacional, estatal, departamental, municipal, distrital) con sus propiedades fiscales. Puede referenciar opcionalmente una división territorial administrativa del catálogo de Datos de Referencia (`divisionTerritorialRef`) cuando coincide con una división administrativa; en jurisdicciones puramente fiscales (distritos US, reservas indígenas) la referencia queda vacía. El atributo `tipoRegimen` clasifica jurisdicciones por su régimen fiscal especial, permitiendo que las condiciones evalúen categorías (`puerto-libre`, `frontera-iva-reducido`, etc.) en lugar de códigos individuales. `[D12]` | Codigo (string único por país), nombre, nivelJurisdiccional (nacional/estatal/provincial/departamental/municipal/distrital), divisionTerritorialRef (opcional — código del catálogo `divisiones-territoriales-{pais}` de Datos de Referencia), tipo (territorial-administrativa/regimen-especial-territorial/distrito-fiscal-especial/soberania-tributaria), tipoRegimen (opcional, string — clasificación categórica del régimen fiscal), vigencia (VO), origen. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `Vigencia` | Rango temporal de validez: fechaDesde, fechaHasta. |
| `Origen` | Procedencia del contenido: `estándar` o `personalizado` (precedencia personalizado > estándar). |

**Tipos de jurisdicción (`tipo`):**

| Tipo | Descripción | Cobertura | Ejemplo |
|---|---|---|---|
| `territorial-administrativa` | Jurisdicción que coincide con una división administrativa (departamento, municipio, estado, provincia). `divisionTerritorialRef` poblado. | LatAm clásico | Bogotá D.C. (CO, codigo `11001`, divisionTerritorialRef `11001`) |
| `regimen-especial-territorial` | Región territorial con régimen fiscal propio. Coincide con división administrativa pero tiene tributación diferenciada. `tipoRegimen` clasifica el régimen específico. | LatAm + casos puntuales | San Andrés (CO, codigo `88001`, tipoRegimen `puerto-libre`) |
| `distrito-fiscal-especial` | Jurisdicción fiscal sin equivalente administrativo (distritos especiales US: transit, fire, water, BIDs, TIFs). `divisionTerritorialRef` queda vacío. | US (F2) | LA Metro Transit District |
| `soberania-tributaria` | Territorio con soberanía fiscal autónoma (reservas indígenas US, First Nations CA). | US/CA (F2) | Navajo Nation |

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `jurisdiccionVigenteA(codigo, fecha)` | Retorna la jurisdicción con el código indicado vigente a la fecha. Si existen entrada estándar y personalizada para el mismo código, retorna la personalizada (precedencia). |
| `jurisdiccionesPorTipo(tipo, fecha)` | Retorna todas las jurisdicciones vigentes con el tipo indicado. Usado por el motor para enumerar regímenes territoriales aplicables. |
| `jurisdiccionesPorTipoRegimen(tipoRegimen, fecha)` | Retorna todas las jurisdicciones con el régimen categórico indicado. Permite agrupar jurisdicciones con misma regla fiscal (ej: todos los municipios de Frontera Norte MX). |
| `validarReferencia(codigo, pais, fecha)` | Verifica que el código referenciado exista como jurisdicción vigente del país. Precondición usada por el motor al validar `ubicaciones.subnacional` y `RegistroTributario.Jurisdiccion.subnacional`. `[I13]` |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  JurisdiccionFiscal (Agregado)                                │
│                                                              │
│  pais · origen                                               │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Jurisdiccion #1 (Entidad)                              │  │
│  │  codigo: 11001 · nombre: Bogotá D.C.                   │  │
│  │  nivelJurisdiccional: municipal                        │  │
│  │  divisionTerritorialRef: 11001  (→ DR)                 │  │
│  │  tipo: territorial-administrativa                      │  │
│  │  tipoRegimen: —                                        │  │
│  │  ○ Vigencia { 2017-01-01 → ∞ }                        │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Jurisdiccion #2 (Entidad)                              │  │
│  │  codigo: 88001 · nombre: San Andrés                    │  │
│  │  nivelJurisdiccional: municipal                        │  │
│  │  divisionTerritorialRef: 88001  (→ DR)                 │  │
│  │  tipo: regimen-especial-territorial                    │  │
│  │  tipoRegimen: puerto-libre                             │  │
│  │  ○ Vigencia { 1991-01-01 → ∞ }                        │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Jurisdiccion #3 (Entidad) — F2                         │  │
│  │  codigo: US-CA-LA-METRO · nombre: LA Metro Transit     │  │
│  │  nivelJurisdiccional: distrital                        │  │
│  │  divisionTerritorialRef: null                          │  │
│  │  tipo: distrito-fiscal-especial                        │  │
│  │  ○ Vigencia { 2010-01-01 → ∞ }                        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  jurisdiccionVigenteA(codigo, fecha)                   │  │
│  │  jurisdiccionesPorTipo(tipo, fecha)                    │  │
│  │  jurisdiccionesPorTipoRegimen(tipoRegimen, fecha)      │  │
│  │  validarReferencia(codigo, pais, fecha) → I13          │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Relación con el contrato del motor (`[D9]`):** el campo `ubicaciones.{rol}.subnacional` del contrato es un string que referencia `Jurisdiccion.codigo` del país correspondiente. El motor valida la referencia (invariante `[I13]`) y resuelve la jurisdicción completa antes de evaluar condiciones — esto permite que las condiciones (Sección 3.4) evalúen atributos de la jurisdicción (`tipo`, `tipoRegimen`, `codigo`) mediante los roles `sedeEmisora.jurisdiccion`, `sedeContraparte.jurisdiccion`, `lugarEjecucion.jurisdiccion` y `jurisdiccionResuelta` (introducidos por `[D12]` y formalizados por `[I15]`).

**Relación con `TarifaTributaria` (Sección 3.3):** el stream key de `TarifaTributaria` se compone con `Jurisdiccion.codigo` (por país) cuando el tributo es subnacional (`nivelJurisdiccional ≠ nacional`). Para tributos nacionales, el stream usa solo el código de país.

**Frontera con `CatalogoDeRegimenesEspeciales` (Sección 3.8):** Las zonas francas, regímenes de inscripción regional fronteriza, zonas económicas especiales y demás regímenes que aplican a **empresas específicas dentro de un territorio normal** NO se modelan en `JurisdiccionFiscal` — se modelan como atributos del `PerfilTributario` validados contra `CatalogoDeRegimenesEspeciales` (Sección 3.8). Solo cuando un régimen aplica a **una región territorial entera** (San Andrés Puerto Libre, Galápagos LOREG, Frontera Norte/Sur MX, ALCs Brasil) entra en `JurisdiccionFiscal` con `tipo: regimen-especial-territorial`.

**Tipos del enum declarados sin precarga en F1:** Los tipos `distrito-fiscal-especial` y `soberania-tributaria` están definidos en el enum de `Jurisdiccion.tipo` y son válidos estructuralmente, pero **no tienen entradas precargadas en F1** — su activación es responsabilidad de F2 (apertura US/CA). Cuando se aborde la implementación US/CA, la precarga se hace via el flujo normal de configuración (`JurisdiccionAgregada`) sin cambios estructurales del modelo. Ver `[PD11]` para el detalle de las cuatro líneas de trabajo de la apertura multi-país.

Decisiones de diseño aplicadas: `[D1]` Raíz por país. `[D12]` Catálogo de jurisdicciones fiscales independiente. `[D15]` Política de extensión de enums fiscales.

### 3.8. Agregado: CatalogoDeRegimenesEspeciales `[F1]`

- **Raíz:** Catálogo de regímenes especiales empresariales de un país. Identifica los regímenes en los que una entidad fiscal puede estar inscrita para acceder a tributación diferenciada (zonas francas, monopolios sectoriales, regímenes empresariales archipelágicos, zonas económicas especiales, decretos individuales). Es referenciado desde el `PerfilTributario` mediante `AtributoFiscal.valor` cuando la `DefinicionAtributo` correspondiente tiene `catalogoReferencia: "CatalogoDeRegimenesEspeciales"`. Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar `[P3]`. Documentación de origen, fuentes y política de extensión: ver `anexo-catalogo-regimenes-especiales.md`. `[D13]`
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `catalogo-regimenes-{pais}`
- **Eventos propios:** 4 — ver Sección 5.2.7.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `RegimenEspecial` | Régimen especial empresarial que una entidad fiscal puede tener registrado. Tiene un código único dentro del país (asignado por la autoridad fiscal correspondiente), un nombre legible, un `tipo` categórico (clasificación de la naturaleza fiscal), una `autoridad` que lo administra, y opcionalmente un `jurisdiccionRef` cuando el régimen está localizado en una jurisdicción específica. Cada régimen tiene vigencia temporal — al cerrarse, las empresas inscritas conservan el código histórico pero el motor ya no lo activa para nuevas transacciones. `[D13]` `[I16]` `[I17]` | Codigo (string único por país, formato definido por la autoridad), nombre, tipo (zona-franca/puerto-libre-empresa/monopolio-sectorial/zona-economica-especial/regimen-especial-decreto), autoridad (string — nombre de la autoridad emisora: DIAN, CNZFE, etc.), jurisdiccionRef (opcional, ref a `JurisdiccionFiscal.codigo` cuando el régimen está físicamente localizado), vigencia (VO), origen. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `Vigencia` | Rango temporal de validez: fechaDesde, fechaHasta. |
| `Origen` | Procedencia del contenido: `estándar` o `personalizado` (precedencia personalizado > estándar). |

**Tipos de régimen especial (`tipo`) — Enum F1:**

El enum del modelo F1 contiene cinco tipos certificados con respaldo normativo para los países del alcance (CO, DO, PA). Tipos candidatos para F2 (`polo-economico`, `inscripcion-region-fronteriza`, `area-libre-comercio`, `regimen-archipielago-empresa`, `status-indigena`) están documentados conceptualmente en `anexo-catalogo-regimenes-especiales.md` pero **no están en el enum del modelo F1**; se agregarán al enum cuando se aborde el país correspondiente.

| Tipo | Descripción | Países (F1) | Autoridad típica |
|---|---|---|---|
| `zona-franca` | Empresa autorizada para operar en zona franca con régimen tributario diferenciado | CO, DO | DIAN (CO), CNZFE (DO) |
| `puerto-libre-empresa` | Empresa inscrita en régimen empresarial archipelágico (cuando aplica condición empresarial específica además de la ubicación territorial) | CO | DIAN |
| `monopolio-sectorial` | Empresa con monopolio departamental de comercialización (licores, juegos de azar) | CO | Asambleas departamentales |
| `zona-economica-especial` | Empresa autorizada para operar en zona económica especial / centro internacional de negocios | PA | Autoridades específicas (ZLC, AEEPP, Ciudad del Saber) |
| `regimen-especial-decreto` | Régimen otorgado por decreto/resolución individual a una empresa específica. Catch-all para casos no cubiertos por los tipos anteriores | Genérico | Varía según jurisdicción |

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `regimenVigenteA(codigo, fecha)` | Retorna el régimen especial con el código indicado vigente a la fecha. Si coexisten entrada estándar y personalizada para el mismo código, retorna la personalizada (precedencia). |
| `regimenesPorTipo(tipo, fecha)` | Retorna todos los regímenes vigentes con el tipo indicado. Usado por el motor cuando una condición fiscal aplica a una categoría completa (ej: "cualquier zona franca → exención IVA en operaciones internas"). |
| `regimenesPorAutoridad(autoridad, fecha)` | Retorna todos los regímenes vigentes administrados por la autoridad indicada. Usado para reportes a la autoridad correspondiente. |
| `validarReferencia(codigo, pais, fecha)` | Verifica que el código referenciado exista como régimen vigente del país. Precondición usada al validar `AtributoFiscal.valor` que referencia este catálogo (invariante `[I16]`). |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  CatalogoDeRegimenesEspeciales (Agregado)                     │
│                                                              │
│  pais · origen                                               │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ RegimenEspecial #1 (Entidad)                           │  │
│  │  codigo: ZF-OCCIDENTE-001                              │  │
│  │  nombre: Zona Franca de Occidente                      │  │
│  │  tipo: zona-franca · autoridad: DIAN                   │  │
│  │  jurisdiccionRef: 25286 (Funza)                           │  │
│  │  ○ Vigencia { 1995-08-15 → ∞ }                        │  │
│  │  origen: estándar                                      │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ RegimenEspecial #2 (Entidad)                           │  │
│  │  codigo: MON-LICOR-ANTIOQUIA                           │  │
│  │  nombre: Monopolio de licores destilados — Antioquia   │  │
│  │  tipo: monopolio-sectorial                             │  │
│  │  autoridad: Asamblea Departamental Antioquia           │  │
│  │  jurisdiccionRef: 05 (departamento)                       │  │
│  │  ○ Vigencia { 2017-01-01 → ∞ }                        │  │
│  │  origen: estándar                                      │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ RegimenEspecial #3 (Entidad) — Panamá                  │  │
│  │  codigo: ZLC-OPERADOR-123                              │  │
│  │  nombre: Zona Libre de Colón — Operador 123            │  │
│  │  tipo: zona-economica-especial · autoridad: ZOLICOL    │  │
│  │  jurisdiccionRef: 0301 (Colón)                            │  │
│  │  ○ Vigencia { 2020-03-01 → ∞ }                        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  regimenVigenteA(codigo, fecha)                        │  │
│  │  regimenesPorTipo(tipo, fecha)                         │  │
│  │  regimenesPorAutoridad(autoridad, fecha)               │  │
│  │  validarReferencia(codigo, pais, fecha) → I16          │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Relación con `PerfilTributario` (Sección 3.6):** los regímenes empresariales se materializan como **atributos del perfil tributario** de la entidad fiscal. Una `DefinicionAtributo` (Sección 3.5) con `catalogoReferencia: "CatalogoDeRegimenesEspeciales"` define qué atributos del perfil referencian este catálogo (ej: `regimenZonaFranca`, `inscripcionEnMonopolio`). El `AtributoFiscal.valor` correspondiente contiene el código del `RegimenEspecial`, validado por invariante `[I16]`. El motor consulta `PerfilTributario.regimenesEspecialesVigentes(fecha)` para enumerar los regímenes activos en evaluación de condiciones.

**Frontera con `JurisdiccionFiscal` (Sección 3.7):** Los regímenes territoriales (toda una región completa tributa diferente — San Andrés Puerto Libre, Galápagos LOREG, Frontera Norte MX, ALCs Brasil) NO se modelan aquí — se modelan en `JurisdiccionFiscal` con `tipo: regimen-especial-territorial` y `tipoRegimen` categórico. Algunos casos requieren ambos modelados simultáneamente: ejemplo, en México una transacción goza de IVA reducido si (a) `lugarEjecucion.jurisdiccion.tipoRegimen = frontera-iva-reducido` (territorial) Y (b) la empresa vendedora tiene atributo `inscripcionRegionFronteriza` apuntando a un `RegimenEspecial` vigente (empresarial). Ver `anexo-catalogo-regimenes-especiales.md` Sección 5 para la frontera completa.

Decisiones de diseño aplicadas: `[D1]` Raíz por país. `[D13]` Catálogo de regímenes especiales empresariales.

### 3.9. Agregado: RegistroTributario (ES) `[F1]`

- **Raíz:** Hecho fiscal inmutable que representa el resultado tributario de una transacción confirmada. Nace con un único evento de creación y contiene: el desglose confirmado por el consumidor, el contexto transaccional completo (entidades fiscales, jurisdicción, efecto fiscal) y, si hubo intervención manual, el cálculo de referencia (tributos propuestos y descartados con motivo) para auditoría. En gravámenes, la referencia es el resultado del motor. En desgravámenes, es el prorrateo del desglose confirmado del registro origen (resuelto internamente por `transaccionOrigenId`). `[R22]` `[R23]` `[R24]`
- **Ciclo de vida:** Sin FSM — nace como hecho fiscal confirmado con un único evento de creación.
- **Stream de eventos:** `registro-tributario-{guid}` (ES)
- **Eventos propios:** 1 — ver Sección 5.3.1.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `LineaDeDesglose` | Cálculo fiscal individual de un tributo aplicado a un concepto. Inmutable — es el resultado del desglose confirmado. Una por cada tributo que aplicó a cada concepto. Granularidad por concepto: se puede agregar para reportes pero no desagregar. La entidad sirve a tres propósitos distintos según el atributo `proposito`: **(a)** `confirmado` — la línea representa el desglose final confirmado por el consumidor (siempre presente); **(b)** `referencia` — la línea representa el cálculo de referencia del motor al momento de la confirmación (presente solo si hubo intervención manual; antes denominada `LineaDesgloseMotor`); **(c)** `descartada` — la línea representa un tributo que el motor evaluó pero excluyó del desglose con un motivo de exclusión (presente solo en gravámenes con intervención manual; antes denominada `LineaDescartada`). Esta unificación reemplaza tres entidades separadas que compartían la misma estructura. `[R16]` `[R19]` `[R24]` | proposito (confirmado/referencia/descartada), Tributo (código, nombre), naturaleza (aditivo/sustractivo), baseGravable, tarifa, tipoTarifa (porcentaje/específica), valor calculado, factorUtilizado, conceptoOrigen (ref. al concepto del sub-dominio consumidor), motivoExclusion (solo cuando `proposito = descartada`: cuantia_minima / perfil_no_aplica / clasificacion_excluida / jurisdiccion_no_aplica / dependencia_padre / actividad_no_registrada). |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `ContextoTransaccional` | Referencia a la transacción: sub-dominio (OXP/CXC), ID de transacción, dirección fiscal (gasto/ingreso), efecto fiscal (gravamen/desgravamen). Si es desgravamen: `transaccionOrigenId` (ID de la transacción del gravamen original en el consumidor — Impuestos resuelve internamente el RegistroTributario origen). Inmutable. |
| `EntidadFiscalEmisora` | Snapshot de la entidad que origina el hecho económico al momento del cálculo: identificación fiscal, perfil tributario vigente. Inmutable. **Por qué se modela como VO separado de `EntidadFiscalContraparte`:** ambos VOs tienen estructura idéntica, pero el rol fiscal (emisora vs. contraparte) es semánticamente significativo dentro del hecho fiscal — preservarlo como dos VOs distintos (no como un único VO con campo `rol`) evita que el rol pueda cambiarse por confusión de datos. La emisora siempre es la entidad operadora del ERP; la contraparte siempre es la otra parte. La separación protege el lenguaje fiscal del registro inmutable. |
| `EntidadFiscalContraparte` | Snapshot de la contraparte al momento del cálculo: identificación fiscal, perfil tributario vigente. Inmutable. Ver la nota en `EntidadFiscalEmisora` sobre por qué son dos VOs separados. |
| `Jurisdiccion` | Jurisdicción resuelta para el cálculo: país, `subnacional` (código que referencia `JurisdiccionFiscal.codigo`). Inmutable — snapshot del momento del cálculo. Resultado de aplicar la `ReglaDeLocalizacion` sobre las ubicaciones enviadas por el consumidor; cada ubicación referencia una `Jurisdiccion` vigente del catálogo `JurisdiccionFiscal` del país correspondiente (invariante `[I13]`). El registro conserva el código histórico aunque la jurisdicción se cierre posteriormente. `[D8]` `[D12]` |
| `IntervencionManual` | Indica si el desglose confirmado diverge del cálculo de referencia. En gravámenes, el cálculo de referencia es el resultado del motor. En desgravámenes, es el prorrateo del desglose confirmado del registro origen. `huboIntervencion` (boolean, derivable comparando ambos conjuntos). **Cuando es `true`**, el registro incluye `LineaDeDesglose` con `proposito: referencia` (en gravámenes y desgravámenes) y `LineaDeDesglose` con `proposito: descartada` (solo en gravámenes — en desgravámenes el motor no participa, por lo tanto no hay líneas descartadas). **Cuando es `false`**, solo existen las `LineaDeDesglose` con `proposito: confirmado`. |

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
│  │ LineaDeDesglose · proposito: referencia (Entidad)      │  │
│  │  Misma estructura que `proposito: confirmado` —        │  │
│  │  solo presente si huboIntervencion = true              │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ LineaDeDesglose · proposito: descartada #1 (Entidad)   │  │
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
| `calcularProrrateoPara(desgravamen)` | **Comportamiento del registro origen** invocado al confirmar un desgravamen que referencia este registro como gravamen origen. Aplica la regla fiscal de prorrateo: para cada concepto y tributo del desglose confirmado del registro, calcula la porción proporcional al monto del desgravamen → produce el `CalculoDeReferencia` (lista de `LineaDeDesglose` con `proposito: referencia`) que el flujo `ConfirmacionTributaria` usará para comparar contra el desglose confirmado del desgravamen. La regla fiscal vive en el agregado origen (que es quien conoce el desglose original), no en el flujo de aplicación. **Nota sobre el término `CalculoDeReferencia`:** no es una entidad ni un value object nuevo del modelo; es el **nombre del conjunto retornado** por esta operación — específicamente, una lista de `LineaDeDesglose` con `proposito: referencia` (Sección 3.9, tabla de entidades). Se usa como término de referencia para que el flujo y los eventos puedan nombrar el conjunto sin tener que repetir la descripción cada vez. **Comportamiento ante conceptos del desgravamen que no existen en el origen:** si el `desgloseConfirmado` del desgravamen incluye un concepto que no estaba presente en el desglose confirmado del registro origen, la operación rechaza el cálculo con motivo `concepto_no_existe_en_origen` — una nota crédito o devolución no puede introducir tributos sobre conceptos que no fueron originalmente gravados; el desgravamen debe ser estrictamente un subconjunto de los conceptos del gravamen origen. El flujo `ConfirmacionTributaria` propaga este rechazo como `ConfirmacionTributariaRechazada` con el mismo `motivoCodigo`. **Comportamiento ante reintentos:** el cálculo es **determinístico** — con el mismo registro origen y los mismos montos del desgravamen, el resultado siempre es el mismo `CalculoDeReferencia`. La operación **no escribe nada ni produce efectos colaterales** (no emite eventos, no modifica el registro origen). Si el flujo de confirmación falla después de invocar este cálculo y debe reintentar el comando, puede invocar la operación nuevamente y obtener el mismo resultado, sin riesgo de duplicar registros ni cambiar el desglose origen. |
| `localizarRegistroPorTransaccionOrigen(subDominio, transaccionId)` | Operación de consulta que busca el `RegistroTributario` originado por una transacción específica de un sub-dominio consumidor. Recibe el identificador del sub-dominio que originó el hecho fiscal (OXP, CXC) y el identificador de la transacción origen tal como lo conoce ese sub-dominio. Retorna el registro tributario correspondiente si existe y está confirmado como gravamen. Si no existe, retorna ausencia (no es error). Usado por el flujo `ConfirmacionTributaria` (Sección 3.15) al resolver el gravamen origen de un desgravamen. La búsqueda se hace por los **identificadores de negocio** (sub-dominio + transacción), no por el identificador interno del stream del registro. Documentar esta operación como parte del agregado preserva la regla de búsqueda como conocimiento de dominio, no como detalle del flujo de aplicación. La garantía de consistencia entre la escritura del registro y su disponibilidad en esta búsqueda es responsabilidad de la implementación — ver `[SI02]`. |
| `crear(contexto, desgloseConfirmado, calculoDeReferencia)` | Factory method. Crea el registro comparando `desgloseConfirmado` con `calculoDeReferencia`. El cálculo de referencia depende del `efectoFiscal`: para gravámenes proviene del motor (`resultadoMotor.aplicados`); para desgravámenes proviene del comportamiento `calcularProrrateoPara(...)` invocado sobre el registro origen. **Trazabilidad del input al evento:** el `calculoDeReferencia` recibido por el factory no se persiste literalmente como input separado en el evento `RegistroTributarioCreado`; se procesa así — si difiere del `desgloseConfirmado` (más allá del margen de redondeo de `[I10]`), el factory marca `huboIntervencion = true` y descompone el `calculoDeReferencia` en `LineaDeDesglose[]` con `proposito: referencia` (y, en gravámenes, también `proposito: descartada` para los tributos que el motor excluyó). Si no difiere, el `calculoDeReferencia` se descarta — no aparece en el evento, porque coincide con el desglose confirmado. Si divergen → persiste ambos conjuntos. Si coinciden → solo el desglose confirmado. Emite `RegistroTributarioCreado`. `[R22]` `[R24]` |

Decisiones de diseño aplicadas: `[D4]` Registro tributario como hecho inmutable.

Ejemplo completo de almacenamiento (stream de eventos, gravamen, desgravamen y efecto fiscal neto) en `anexo-ejemplo-registro-tributario.md`.

### 3.10. Agregado: HomologacionFiscal `[F2]`

- **Raíz:** Tabla de equivalencias entre los valores internos del sub-dominio (factorUtilizado, clasificación tributaria) y los códigos que exige una autoridad fiscal en sus reportes. Cada instancia cubre una autoridad específica. Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar `[P3]`.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `homologacion-fiscal-{id}`
- **Eventos propios:** 4 — ver Sección 5.2.8.

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

### 3.11. Agregado: FormatoFiscal `[F2]`

- **Raíz:** Definición de un formato de entregable fiscal exigido por una autoridad. Describe qué tipo de entregable es, con qué periodicidad, en qué formato(s) de salida y qué estructura de datos requiere. Referencia la `HomologacionFiscal` de su autoridad para traducir valores internos a códigos del reporte. Soporta contenido de dos orígenes (estándar/personalizado) con precedencia personalizado > estándar `[P3]`.
- **Atributos del agregado raíz:** `codigo` (identificador del formato — **inmutable**, referenciado por `EntregableFiscal` y `CertificadoTributario` vía `ReferenciaFormato`; cambiar el código rompería esos vínculos históricos), `nombre`, `tipoEntregable` (clasifica el formato: `reporte` para reportes de información fiscal exógena/municipal, `certificado` para certificados tributarios; el enum es extensible vía `[D15]` cuando se aborde declaraciones tributarias — ver `[PD4]`), `referenciaHomologacion` (VO `ReferenciaHomologacion` — apunta a la `HomologacionFiscal` de la autoridad), `origen`. La modificabilidad de los demás atributos sigue la política general de corrección de errores en configuración fiscal — ver `[PD12]`.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `formato-fiscal-{id}`
- **Eventos propios:** 5 — ver Sección 5.2.9.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `SeccionFormato` | Bloque lógico del entregable que agrupa campos relacionados. Un formato puede tener múltiples secciones. Cada sección define qué datos del `RegistroTributario` consume, cómo los agrupa y qué homologación aplica. **Identidad:** `seccionId` (identificador único asignado al crear la sección) — se preserva durante todo el ciclo de vida y permite referenciarla en `SeccionFormatoModificada` y `SeccionFormatoEliminada`. **Política de re-creación:** tras eliminar una sección, su `seccionId` queda cerrado en el stream. Si el administrador crea una nueva sección con el mismo `Nombre`, se asigna un `seccionId` nuevo y el stream conserva ambas instancias como entidades históricas separadas — esto preserva la inmutabilidad del histórico sin necesidad de duplicar atributos en el evento de eliminación. `[R26]` `[R27]` | seccionId, Nombre, descripción, criterioDeAgrupacion (por tercero, por tributo, por concepto, por jurisdicción), criterioDeSeleccion (qué registros tributarios incluir), orden. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `AutoridadFiscal` | (compartido — ver Sección 2.5) Autoridad destinataria: nombre, jurisdicción, país. |
| `Periodicidad` | (compartido — ver Sección 2.5) Frecuencia de generación: tipo (mensual/bimestral/trimestral/anual), meses aplicables. `[R29]` |
| `FormatoDeSalida` | (compartido — ver Sección 2.5) Formato técnico del archivo: tipo (XML/Excel/PDF), esquema o plantilla de referencia. Un formato puede tener múltiples salidas (ej: XML para DIAN + Excel para prevalidador). `[R27]` |
| `ReferenciaHomologacion` | (compartido — ver Sección 2.5) Referencia a la `HomologacionFiscal` de la autoridad para traducir valores internos a códigos del reporte: ID, versión. Usado por el atributo raíz `referenciaHomologacion` del agregado (Sección 3.11 — "Atributos del agregado raíz") y propagado a `EntregableFiscal` y `CertificadoTributario` al generar entregables. |
| `Vigencia` | (compartido — ver Sección 2.5) Rango temporal de validez: fechaDesde, fechaHasta. |
| `Origen` | (compartido — ver Sección 2.5) Procedencia del contenido: `estándar` o `personalizado` (precedencia personalizado > estándar). |

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

### 3.12. Agregado: EntregableFiscal (ES) `[F2]`

- **Raíz:** Instancia concreta de un reporte fiscal generado para un período, autoridad y tipo específicos. Representa la ejecución de un `FormatoFiscal` sobre los `RegistroTributario` confirmados del período, traducidos mediante `HomologacionFiscal`. Es un documento compuesto — un solo archivo que contiene datos de múltiples terceros y registros. Cada generación crea un nuevo stream. No incluye certificados (propio agregado `CertificadoTributario`, 3.13) ni declaraciones tributarias (ver `[PD4]`). `[R26]` `[R27]`
- **Ciclo de vida:** FSM transaccional — Borrador → Generado → Presentado. Permite regeneración (vuelve a Borrador).
- **Stream de eventos:** `entregable-fiscal-{guid}`
- **Eventos propios:** 4 — ver Sección 5.3.2.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `ContenidoGenerado` | Resultado de aplicar el formato al conjunto de registros tributarios del período. Contiene las filas de datos ya homologadas con códigos de la autoridad. Se reemplaza completamente en cada regeneración. La `fechaDeCorte` permite reconstruir exactamente cuáles registros conformaron este contenido (todos los `RegistroTributario` del período con `fechaTransaccion ≤ fechaDeCorte`) — una regeneración posterior puede tener un conjunto distinto si llegaron registros nuevos al período, pero el evento previo conserva la `fechaDeCorte` original para auditoría. | Filas (lista de datos estructurados según `SeccionFormato`), totalRegistrosIncluidos, fechaGeneracion, **fechaDeCorte** (fecha hasta la cual se incluyeron los `RegistroTributario` del período en esta generación — todos los registros con `fechaTransaccion ≤ fechaDeCorte` que estuvieran disponibles al momento de generar quedan incluidos). |

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
│  │  puedeGenerar()    → estado = Borrador                  │  │
│  │  puedeRegenerar()  → estado = Generado                  │  │
│  │  esPresentable() → estado = Generado                   │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento calculado del agregado:**

| Comportamiento | Descripción |
|---|---|
| `puedeGenerar()` | Indica si se puede generar el contenido por primera vez. Retorna `true` solo cuando el estado es Borrador. |
| `puedeRegenerar()` | Indica si se puede regenerar el contenido (descartando el contenido anterior). Retorna `true` solo cuando el estado es Generado. Si ya fue presentado, no se puede — se crea uno nuevo. |
| `esPresentable()` | Solo entregables en estado Generado pueden marcarse como presentados ante la autoridad. |

Decisiones de diseño aplicadas: `[D4]` Registros tributarios como fuente. `[D6]` Homologación fiscal como dimensión independiente.

### 3.13. Agregado: CertificadoTributario (ES) `[F2]`

- **Raíz:** Certificado tributario individual emitido para un tercero específico en un período fiscal. Cada certificado tiene su propio ciclo de vida: se genera, se envía y se entrega de forma independiente. La generación masiva ("todos los certificados del 2025") es un proceso de aplicación que crea N instancias de este agregado. **⚠️ Postura preliminar — sujeta a revisión en F2 vía `[PD7]`:** la agrupación por período se modela como información del proceso (read model) y no como entidad con identidad propia. Esta postura es una de dos alternativas que se evaluarán al modelar el proceso de generación masiva en F2 — ver `[PD7]` para los dos planteamientos abiertos y los criterios de decisión. `[R28]` `[R37]`
- **Ciclo de vida:** FSM transaccional — Borrador → Generado → Entregado (■ terminal). Estado intermedio recuperable: Fallido (cuando la infraestructura reporta fallo de envío; se resuelve vía Reenviado). Permite regeneración (Generado → Borrador). Detalle completo en FSM 4.2.
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
| `ResultadoEnvio` | Resultado del último intento de envío: canal (correo/portal), fecha, exitoso (boolean), detalle de fallo (si aplica), `referenciaEnvio` (referencia externa que la infraestructura asignó al envío — ej: identificador del correo enviado, ticket del portal, código del adaptador; permite correlacionar el reporte de resultado con el envío que lo originó). |

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
│  │  puedeGenerar()    → estado = Borrador                  │  │
│  │  puedeRegenerar()  → estado = Generado                  │  │
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
| `puedeGenerar()` | Indica si se puede generar el contenido por primera vez. Retorna `true` solo cuando el estado es Borrador. |
| `puedeRegenerar()` | Indica si se puede regenerar el contenido (descartando el contenido anterior). Retorna `true` solo cuando el estado es Generado. Si ya fue entregado o está en estado Fallido, no se puede regenerar — desde Fallido la única salida es `Reenviado` (ver Sección 4.2). |
| `esEnviable()` | Solo certificados en estado Generado pueden solicitar envío a la infraestructura. `[R28]` |
| `esReenviable()` | Solo certificados en estado Fallido pueden reintentarse. El reintento vuelve a Generado y solicita nuevo envío. |

Decisiones de diseño aplicadas: `[D4]` Registros tributarios como fuente. `[D6]` Homologación fiscal como dimensión independiente.

### 3.14. Servicio de dominio: MotorDeCalculo `[F1]`

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
2. **Para cada concepto** `[P1]`:
   - a. Lee `CatalogoTributario` → `tributosAplicablesA(clasificacion)`. **Filtra los tributos por `Tributo.direccionFiscalAplicable` contra `direccionFiscal` de la solicitud** — los tributos cuya `direccionFiscalAplicable` no incluya la dirección actual se descartan antes de evaluación posterior (invariante del agregado). Si clasificación no existe o no vigente → rechaza concepto `[R32]`
   - b. **Resuelve las jurisdicciones fiscales de las 3 ubicaciones contra `JurisdiccionFiscal`** (`jurisdiccionVigenteA(codigo, fecha)` para cada `ubicaciones.{rol}.subnacional`). Si algún código no referencia una `Jurisdiccion` vigente → rechaza `[R30]` `[I13]`. Para cada tributo candidato, aplica `ReglaDeLocalizacion` → `resolverJurisdiccion(ubicaciones)` `[D8]` `[D12]` → obtiene la **jurisdicción resuelta del tributo** (la ubicación que ganó según la regla de localización, con su entidad `Jurisdiccion` completa: tipo, tipoRegimen, codigo). Si ubicación obligatoria falta → rechaza `[R30]`. El motor mantiene durante el cálculo del concepto el contexto de jurisdicciones: `sedeEmisora.jurisdiccion`, `sedeContraparte.jurisdiccion`, `lugarEjecucion.jurisdiccion` (cuando aplique) y `jurisdiccionResuelta` (la del tributo en evaluación) — disponibles para evaluación de condiciones en el paso 2.c.
   - c. Evalúa `CondicionDeAplicacion` con perfiles de ambas entidades **y con las jurisdicciones resueltas** → determina efecto (aplica / excluye / modifica tarifa) `[R09]` `[R10]`. **Filtra las condiciones por `Condicion.direccionFiscalAplicable` contra `direccionFiscal` de la solicitud** — solo se evalúan las condiciones cuya dirección sea compatible con la dirección actual. Las condiciones cuyo `ambitoEvaluado` referencia jurisdicciones (`sedeEmisora.jurisdiccion`, `sedeContraparte.jurisdiccion`, `lugarEjecucion.jurisdiccion`, `jurisdiccionResuelta`) evalúan atributos de la entidad `Jurisdiccion` correspondiente (`tipo`, `tipoRegimen`, `codigo`) — habilitado por `[D12]` y `[I15]`. **Para condiciones cuyo `atributoEvaluado` referencia una `DefinicionAtributo` con `catalogoReferencia: "CatalogoDeRegimenesEspeciales"`** (regímenes empresariales: zona franca, monopolio, decreto individual), el motor consulta `PerfilTributario.regimenesEspecialesVigentes(fechaTransaccion)` del rol correspondiente (`emisora` o `contraparte` según `ambitoEvaluado`) y evalúa la pertenencia del `valorEsperado` (código del régimen) en la lista de regímenes activos. La validación referencial al escribir el atributo se realiza al persistir el `PerfilTributario` (`[I16]`); aquí solo se consulta. `[D13]`
   - d. Si aplica: lee `TarifaTributaria` → tarifa vigente a `fechaTransaccion`, usando el `factorDeTarifa` del tributo. Resolución por tipo: `clasificacion` desde la solicitud, `conceptoPago` desde la solicitud (condicional), `actividadEconomica` desde el `PerfilTributario` del sujeto pasivo del tributo (según el rol `emisora`/`contraparte` declarado en la condición que activó el tributo, en la dirección fiscal actual) invocando `PerfilTributario.actividadEconomicaPara(jurisdiccionResuelta, clasificacionDelConcepto, fechaTransaccion)` — esto resuelve por precedencia (jurisdicción + clasificación → jurisdicción → clasificación → catch-all) entre las `ActividadEconomicaRegistrada` del sujeto pasivo. Si ninguna actividad económica coincide, el tributo se descarta con `motivoExclusion: actividad_no_registrada` `[D12]` `[D14]`. `fija` sin factor externo, `porcentajeDePadre` desde el cálculo del tributo padre. Si el factor requerido no está disponible → rechaza el concepto indicando el dato faltante `[R30]` `[R07]`
   - e. Evalúa cuantía mínima — si baseGravable < umbral → descarta con motivo `cuantia_minima` `[R13]`
   - f. Evalúa dependencia de tributo padre — si padre no existe en el resultado → descarta con motivo `dependencia_padre` `[R14]`
   - g. Calcula: baseGravable × tarifa = valor `[R15]` `[R16]`

**Salida (`ResultadoCalculo`):**

| Campo | Descripción |
|---|---|
| `aplicados[]` | Tributos que el motor determinó que aplican. Cada uno con: tributo, naturaleza, **jurisdiccionResuelta** (la jurisdicción específica del tributo según la `ReglaDeLocalizacion` aplicada — un mismo cálculo puede generar líneas con jurisdicciones distintas, ej: IVA nacional + ICA municipal en la misma transacción), baseGravable, tarifa, tipoTarifa, valor, factorUtilizado, conceptoOrigen. `[D8]` |
| `descartados[]` | Tributos evaluados pero excluidos. Misma estructura + `motivoExclusion` (cuantia_minima / perfil_no_aplica / clasificacion_excluida / jurisdiccion_no_aplica / dependencia_padre / actividad_no_registrada). Cada descartado también lleva su `jurisdiccionResuelta` específica. `[R19]` |
| `perfilesUtilizados` | Snapshot de los perfiles de ambas entidades al momento del cálculo. |

> **Nota sobre el cambio de forma del resultado:** En versiones anteriores, `ResultadoCalculo` exponía un único campo `jurisdiccionResuelta` a nivel raíz. Esto era insuficiente cuando un mismo cálculo combinaba tributos nacionales (IVA, RETEFUENTE) con tributos subnacionales (ICA, RICA), porque cada tributo resuelve su propia jurisdicción según la `ReglaDeLocalizacion`. La jurisdicción es **propiedad de cada tributo**, no del cálculo global — por eso vive dentro de cada línea aplicada/descartada. El motor mantiene durante el cálculo el contexto de jurisdicciones de las 3 ubicaciones (sedeEmisora, sedeContraparte, lugarEjecucion) más la `jurisdiccionResuelta` por tributo, y este último se preserva en el output.

**Agregados que lee directamente:** CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, PerfilTributario, JurisdiccionFiscal, CatalogoDeRegimenesEspeciales.

**Dependencias transitivas:** `CatalogoDeAtributosFiscales` no es leído directamente por el motor — lo leen `CondicionDeAplicacion` (para validar que los atributos de las condiciones son definiciones vigentes) y `PerfilTributario` (para construir el perfil completo del momento del cálculo y validar los atributos guardados). El motor confía en que ambos agregados ya validaron contra el catálogo de atributos al persistir.

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

                            │ lee (Cambios 2 y 3)
                ┌───────────┴──────────────────┐
                ▼                              ▼
        JurisdiccionFiscal           CatalogoDeRegimenes-
        [D12]                        Especiales [D13]
```

Decisiones de diseño aplicadas: `[D5]` Motor stateless con evaluación completa. `[D8]` Resolución de jurisdicción. `[D9]` Contrato semántico mínimo.

**Ejemplo aplicado:** ver `anexo-ejemplo-direccion-fiscal.md` para un caso completo del comportamiento del motor según la `direccionFiscal` (gasto vs ingreso), evidenciando cómo las mismas `CondicionDeAplicacion` se evalúan en ambas direcciones vía los roles `emisora/contraparte` (`[D2]`).

### 3.15. Flujo orquestado: ConfirmacionTributaria `[F1]`

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
   - **Desgravamen:** Resuelve el `RegistroTributario` origen invocando `localizarRegistroPorTransaccionOrigen(subDominio, transaccionOrigenId)` (Sección 3.9) — operación del agregado que encapsula la regla de búsqueda por identificadores de negocio. La lectura requiere garantía read-your-writes para evitar rechazos espurios cuando el desgravamen llega inmediatamente después del gravamen origen — ver `[SI02]` para sugerencias de implementación. **Orden de causalidad esperado:** un gravamen siempre se confirma antes que cualquier desgravamen que lo referencia (esto lo garantiza el sub-dominio consumidor, que no emite la nota crédito antes de la factura origen). Por lo tanto, un desgravamen recibido cuando el gravamen origen no es visible se rechaza legítimamente con motivo `origen_no_encontrado` — no hay riesgo de "carrera" entre dos confirmaciones porque la unicidad del hecho fiscal por origen transaccional (`[I18]`) impide que dos comandos creen el mismo gravamen simultáneamente. Si el origen no existe → rechaza la confirmación con motivo `origen_no_encontrado` (ver Resultado del flujo). Invoca **`registroOrigen.calcularProrrateoPara(desgravamen)`** — la regla fiscal de prorrateo vive en el agregado origen (Sección 3.9), no en este flujo de aplicación. Obtiene el `CalculoDeReferencia` y lo usa para crear el nuevo `RegistroTributario`. El motor no participa.
4. Verifica las invariantes fiscales transversales: unicidad del hecho fiscal por origen transaccional (`[I18]`) y saldo de desgravámenes acotado por el gravamen origen (`[I19]` — solo desgravámenes). **Esta verificación previa rechaza tempranamente comandos que claramente violan las invariantes, pero la garantía atómica final ante confirmaciones concurrentes con la misma combinación origen depende del mecanismo de unicidad documentado en `[SI01]`** — sin ese mecanismo, dos confirmaciones simultáneas podrían pasar la verificación del paso 4 y crear ambas un `RegistroTributario`. La implementación debe materializar `[SI01]` para que la verificación del flujo y la garantía atómica trabajen juntas.
5. Invoca `RegistroTributario.crear(contexto, desgloseConfirmado, calculoDeReferencia)` → el agregado compara, determina intervención y emite `RegistroTributarioCreado`. El evento se persiste en stream `registro-tributario-{guid}`.

**Resultado del flujo:**

El flujo siempre termina emitiendo exactamente uno de dos eventos correlacionados con el comando original:

| Evento | Cuándo se emite |
|---|---|
| `RegistroTributarioCreado` | La confirmación cumple todas las precondiciones e invariantes; el `RegistroTributario` se crea. (Documentado en Sección 5.3.1.) |
| `ConfirmacionTributariaRechazada` | La confirmación se rechaza en cualquier paso por incumplimiento de contrato, falta de datos, referencias inválidas o violación de invariantes fiscales. Permite que los sub-dominios consumidores reaccionen al rechazo (reintentar con corrección, escalar, anular el lado de la transacción en el consumidor). El evento captura el motivo del rechazo de forma estructurada. (Documentado en Sección 5.3.4.) |

Esta dualidad garantiza que **toda** confirmación produce una respuesta observable por el consumidor — coherente con la naturaleza asíncrona del flujo y con la arquitectura EDA del bounded context.

**Agregados involucrados:** MotorDeCalculo (lectura, solo gravámenes), RegistroTributario (lectura del origen en desgravámenes + escritura). **Lecturas transitivas vía MotorDeCalculo en gravámenes:** CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, JurisdiccionFiscal, CatalogoDeRegimenesEspeciales, PerfilTributario (ver Sección 3.14 — "Agregados que lee directamente" del MotorDeCalculo).

> **Nota:** El flujo de confirmación se bifurca internamente según el `efectoFiscal`. Para **gravámenes**, el motor calcula y se compara con el desglose confirmado. Para **desgravámenes**, el sistema carga el RegistroTributario del gravamen original (resuelto por `transaccionOrigenId`), prorratea su desglose confirmado a los montos del desgravamen, y usa ese prorrateo como referencia — el motor no participa. En ambos casos el usuario puede intervenir sobre la propuesta y los montos son siempre positivos. Las proyecciones interpretan el `efectoFiscal` para determinar el signo al sumar.

> **Ventana de consistencia eventual entre confirmación y proyecciones:** El evento `RegistroTributarioCreado` se persiste de forma atómica, pero las proyecciones que lo consumen (read models de reportes, certificados, vistas de conciliación fiscal) se actualizan **de forma eventual** — la plataforma de mensajería propaga el evento a los consumidores con un retraso típico de milisegundos a segundos. Durante esa ventana, una consulta inmediata al read model puede no reflejar todavía el registro recién confirmado. Los consumidores que necesiten leer su propia confirmación deben confiar en su copia local del desglose (que ya tienen) y no en el read model fiscal. Para casos de auditoría que requieran consistencia fuerte, consultar directamente el stream del agregado (`registro-tributario-{guid}`). Las garantías concretas de consistencia (read-your-writes, monotonic reads) dependen de la plataforma — ver `[D11]`.

### 3.16. Servicio de dominio: CargaAsistida `[F1]`

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
| `idProcesoCarga` | Identificador único de la propuesta de carga, asignado al generar el `ResultadoCarga`. Se propaga al comando de aprobación del administrador y, desde ahí, a los eventos `AtributoFiscalAgregado` / `AtributoFiscalModificado` que se persistan como consecuencia. Permite (a) trazar qué propuesta originó cada atributo del perfil, (b) detectar aprobaciones duplicadas sobre la misma propuesta, (c) auditar el flujo end-to-end entre la validación inicial y el cambio en el perfil. |
| `atributosValidos[]` | Atributos que pasaron validación contra el catálogo: nombre, valor, definición de referencia. |
| `atributosDescartados[]` | Atributos que no tienen definición vigente o cuyo valor no es válido, con motivo. |
| `fuenteOrigen` | Canal utilizado (api / manual / documento) + identificación (ej: "DIAN", "RUT-2026.pdf"). |
| `fechaCarga` | Timestamp de la carga. |

**Flujo posterior (aplicación) — revisión y aprobación humana:**

5. El administrador fiscal revisa el `ResultadoCarga` (información del proceso, no se persiste como agregado propio) y decide qué atributos aplicar.
6. El administrador emite un comando de aprobación que lleva el `idProcesoCarga` y la lista de atributos a persistir.
7. `PerfilTributario` recibe el comando y, antes de emitir los eventos correspondientes, **re-valida cada atributo contra el catálogo vigente** en ese momento (`CatalogoDeAtributosFiscales`). Si todos los atributos son válidos, emite `AtributoFiscalAgregado` / `AtributoFiscalModificado` (cada uno lleva el `idProcesoCarga` como referencia al proceso que lo originó). Si la re-validación falla, ver "Contrato de rechazo" abajo.

**Comportamiento entre la propuesta y la aprobación:**

El `ResultadoCarga` propuesto es **información efímera** del proceso — no tiene identidad fiscal ni stream propio, pero sí tiene un `idProcesoCarga` que permite correlacionarlo con los eventos posteriores. Tiempo entre la generación del `ResultadoCarga` y la aprobación humana puede variar (minutos, horas, días según operativa del cliente). Durante ese tiempo, el catálogo `CatalogoDeAtributosFiscales` puede haber cambiado (ej: una `DefinicionAtributo` que estaba vigente al generar el `ResultadoCarga` puede cerrarse antes de que el administrador apruebe). Por eso, al persistir los atributos aprobados en el `PerfilTributario`, este re-valida cada atributo contra el catálogo vigente en ese momento.

**Contrato de rechazo de la aprobación:**

Si al ejecutar el paso 7 una definición de atributo ya no está vigente, el comando de aprobación se rechaza **de forma síncrona** con un resultado estructurado que incluye: (a) el `idProcesoCarga` del intento, (b) la lista de atributos afectados por la invalidación con su motivo (ej: `definicion_no_vigente`, `valor_fuera_de_rango`), (c) sugerencia operativa (regenerar el `ResultadoCarga` para obtener una propuesta válida contra el catálogo actual). El rechazo **no emite evento de dominio** — el `PerfilTributario` no cambió, por lo tanto no hay hecho fiscal que registrar. El administrador decide si regenera la propuesta (paso 1 con los mismos atributos normalizados) o descarta el intento. Esta mecánica previene que cambios del catálogo entre propuesta y aprobación introduzcan datos inválidos en el perfil, y garantiza que la decisión de qué hacer ante la invalidación quede en el operador humano (no en automatización ciega).

> **Nota de implementación:** Los pasos 5-6 son orquestación de aplicación (UI + comando). Los adaptadores por canal (API DIAN, parser OCR, formulario) son infraestructura. El servicio de dominio solo conoce atributos normalizados.

**Agregados que lee:** CatalogoDeAtributosFiscales.
**Agregados que escribe:** PerfilTributario (indirectamente — tras aprobación del administrador).
**Dependencia externa:** Adaptadores por canal (anti-corruption layer).

### 3.17. Read Model: CatalogoJurisdiccional `[F1]`

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

### 3.18. Resumen de contenido fiscal por país

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
| FormatoFiscal | ~10 (7 exógena + reportes municipales + certificados) | 4 | Por definir (ver `[PD2]` ítem 4 — formatos DGI Panamá) |
| HomologacionFiscal | 1 (DIAN) | 1 (DGII) | 1 (DGI) |
| **Total streams config** | **~39** | **~13** | **~8** |

---

## 4. Máquinas de estado

Dos agregados del bounded context tienen FSM transaccional: `EntregableFiscal` (reportes) y `CertificadoTributario` (certificados individuales). Los **9 agregados de configuración** tienen ciclo de vida sin FSM (crear, agregar/modificar/cerrar o desactivar entidades internas) — no usan transiciones de estado del agregado raíz. `RegistroTributario` nace como hecho confirmado sin transiciones `[D4]`.

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
- `Fallido` se alcanza cuando la infraestructura reporta fallo en el envío. **La única salida documentada desde `Fallido` es `CertificadoTributarioReenviado`** (vuelve a Generado para nuevo intento de envío). No hay transición `Regenerado` desde `Fallido` — para corregir el contenido del certificado en lugar de los datos del destinatario, primero se debe alcanzar nuevamente el estado Generado vía Reenviado y luego aplicar Regenerado desde allí. La corrección de datos del destinatario antes del reintento se modela como parte del comando que dispara `CertificadoTributarioReenviado` (el evento captura el `Destinatario` actualizado). `Fallido` no es terminal — siempre se puede resolver vía Reenviado.
- La **regeneración** permite corregir el contenido del certificado antes de enviarlo. Solo es posible desde `Generado`; **no es posible desde `Borrador`** (el certificado en Borrador aún no tiene contenido que regenerar — primero se debe generar) ni desde `Entregado` (terminal — si se necesita corregir un certificado ya entregado, se crea uno nuevo en un nuevo stream). Desde `Fallido` la única salida es `Reenviado` (no `Regenerado`).
- La **generación masiva** ("generar todos los certificados del 2025") es un proceso de aplicación que crea N instancias de `CertificadoTributario`. Cada una sigue su propio ciclo de vida de forma independiente. La agrupación por período es informativa (read model / proyección).

---

## 5. Catálogo de eventos

El bounded context de Impuestos emite **57 eventos** distribuidos en **12 agregados con eventos + 1 flujo orquestado** (los 2 servicios de dominio `MotorDeCalculo` y `CargaAsistida` y el read model `CatalogoJurisdiccional` no emiten eventos propios). El flujo orquestado `ConfirmacionTributaria` emite un evento de rechazo (`ConfirmacionTributariaRechazada`) cuando la confirmación no llega a crear el `RegistroTributario`; el evento de éxito (`RegistroTributarioCreado`) lo emite el agregado `RegistroTributario`. Los **9 agregados de configuración** siguen un patrón uniforme (crear agregado, agregar/modificar/cerrar o desactivar entidades internas) y se documentan en formato compacto. Los **3 agregados transaccionales** (RegistroTributario, EntregableFiscal, CertificadoTributario) usan el template completo (Sección 2.2) porque tienen FSM (los dos últimos), causalidad derivada y precondiciones complejas.

**Convención de verbos para fin de aplicabilidad:**

| Verbo | Mecanismo | Significa |
|---|---|---|
| **Cerrado/Cerrada** | La entidad tiene `Vigencia` VO | Se acotó el rango temporal. El dato sigue siendo válido dentro de su rango para consultas históricas. |
| **Desactivado/Desactivada** | La entidad no tiene vigencia temporal | Dejó de ser relevante. Es una definición estructural que se retira. Se conserva por trazabilidad. |
| **Eliminado/Eliminada** | Remoción del agregado | Se quitó de la definición. No caducó ni se desactivó — ya no forma parte de la estructura. |

> **Mutua exclusividad:** Una entidad usa **uno u otro** mecanismo, nunca ambos. Combinar `Vigencia` con un flag `activo/inactivo` sería redundante: dos fuentes de verdad temporal exigirían una invariante adicional para mantenerlas sincronizadas y abrirían la puerta a inconsistencias (ej: vigencia abierta + flag inactivo, o vigencia vencida + flag activo). La pregunta "¿esta entidad aplica a la fecha X?" se responde **únicamente** con la vigencia (entidades del primer grupo, ej: `AtributoFiscal`, `EntradaDeTarifa`, `Condicion`, `DefinicionAtributo`, `Equivalencia`) o con la presencia del evento `*Desactivado` (entidades del segundo, ej: `Tributo`, `ClasificacionTributaria`).

### 5.1. Resumen

| Agregado | Tipo | Eventos | Total |
|---|---|---|:---:|
| CatalogoTributario | Configuración | CatalogoTributarioCreado, TributoAgregado, TributoModificado, TributoDesactivado, ClasificacionTributariaAgregada, ClasificacionTributariaModificada, ClasificacionTributariaDesactivada, TratamientoDefinido, ReglaDeLocalizacionDefinida | 9 |
| TarifaTributaria | Configuración | TarifaTributariaCreada, EntradaDeTarifaAgregada, EntradaDeTarifaModificada, EntradaDeTarifaCerrada | 4 |
| CondicionDeAplicacion | Configuración | CondicionDeAplicacionCreada, CondicionAgregada, CondicionModificada, CondicionCerrada | 4 |
| CatalogoDeAtributosFiscales | Configuración | CatalogoDeAtributosFiscalesCreado, DefinicionAtributoAgregada, DefinicionAtributoModificada, DefinicionAtributoCerrada | 4 |
| PerfilTributario | Configuración | PerfilTributarioCreado, AtributoFiscalAgregado, AtributoFiscalModificado, AtributoFiscalCerrado, ActividadEconomicaRegistradaAgregada, ActividadEconomicaRegistradaModificada, ActividadEconomicaRegistradaCerrada | 7 |
| JurisdiccionFiscal | Configuración | JurisdiccionFiscalCreada, JurisdiccionAgregada, JurisdiccionModificada, JurisdiccionCerrada | 4 |
| CatalogoDeRegimenesEspeciales | Configuración | CatalogoDeRegimenesEspecialesCreado, RegimenEspecialAgregado, RegimenEspecialModificado, RegimenEspecialCerrado | 4 |
| HomologacionFiscal | Configuración | HomologacionFiscalCreada, EquivalenciaAgregada, EquivalenciaModificada, EquivalenciaCerrada | 4 |
| FormatoFiscal | Configuración | FormatoFiscalCreado, FormatoFiscalModificado, SeccionFormatoAgregada, SeccionFormatoModificada, SeccionFormatoEliminada | 5 |
| RegistroTributario | Transaccional (ES) | RegistroTributarioCreado | 1 |
| EntregableFiscal | Transaccional (ES) | EntregableFiscalCreado, EntregableFiscalGenerado, EntregableFiscalRegenerado, EntregableFiscalPresentado | 4 |
| CertificadoTributario | Transaccional (ES) | CertificadoTributarioCreado, CertificadoTributarioGenerado, CertificadoTributarioRegenerado, CertificadoTributarioEntregado, CertificadoTributarioFallido, CertificadoTributarioReenviado | 6 |
| ConfirmacionTributaria | Flujo orquestado | ConfirmacionTributariaRechazada | 1 |
| **Total** | | | **57** |

---

### 5.2. Eventos de configuración

Los eventos de configuración siguen un patrón uniforme: el agregado se crea una vez y las entidades internas se agregan, modifican o cierran/desactivan según su mecanismo de control. No hay FSM transaccional — todos los eventos aplican desde cualquier punto del ciclo de vida del agregado. Las precondiciones son validaciones internas del agregado (tipos, unicidad, vigencias).

#### 5.2.1. CatalogoTributario — 9 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `CatalogoTributarioCreado` | Se creó el catálogo tributario para un país. | País, origen. | — |
| 2 | `TributoAgregado` | Se registró un nuevo tributo en el catálogo. | Código, nombre, naturaleza (aditivo/sustractivo), caracterRetención, nivelJurisdiccional, factorDeTarifa, direccionFiscalAplicable, tributoPadre (si aplica), origen. | `[R03]` |
| 3 | `TributoModificado` | Se actualizaron atributos de un tributo existente. | **Código (identifica — inmutable)**, nombre, naturaleza, caracterRetención, nivelJurisdiccional, factorDeTarifa, direccionFiscalAplicable, tributoPadre, origen (campos potencialmente modificables; qué atributos pueden modificarse vs. requieren desactivación + nuevo dependen de la política de corrección — ver `[PD12]`). | `[R03]` `[PD12]` |
| 4 | `TributoDesactivado` | Un tributo dejó de ser relevante en la jurisdicción. Se conserva para trazabilidad histórica. El motor no lo evalúa. | Código, motivo. | — |
| 5 | `ClasificacionTributariaAgregada` | Se registró una nueva clasificación tributaria. | Código, nombre, descripción, origen. | `[R01]` |
| 6 | `ClasificacionTributariaModificada` | Se actualizaron atributos de una clasificación existente. | **Código (identifica — inmutable)**, nombre, descripción, origen (campos potencialmente modificables; ver `[PD12]` para la política de corrección por tipo de atributo). | `[R01]` `[PD12]` |
| 7 | `ClasificacionTributariaDesactivada` | Una clasificación dejó de ser relevante. Se conserva para trazabilidad histórica. | Código, motivo. | — |
| 8 | `TratamientoDefinido` | Se estableció si un tributo aplica o no a una clasificación. Cubre creación y modificación — es una operación idempotente sobre la combinación (tributo × clasificación). | Tributo, clasificación, aplica (sí/no), origen. | `[R03]` `[R09]` |
| 9 | `ReglaDeLocalizacionDefinida` | Se estableció qué rol de ubicación determina la jurisdicción fiscal para un tributo en una clasificación. Cubre creación y modificación. | Tributo, clasificación (o `*`), rolQueManda, rolFallback (opcional), origen. | `[R34]` |

#### 5.2.2. TarifaTributaria — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `TarifaTributariaCreada` | Se creó la tabla de tarifas para un tributo en una jurisdicción. | Jurisdicción, tributo, origen. | — |
| 2 | `EntradaDeTarifaAgregada` | Se registró una nueva entrada de tarifa. | **entradaId** (identificador único asignado al crear la entrada — preservado durante todo su ciclo de vida), Factor, tarifa, tipoTarifa (porcentaje/específica), cuantíaMínima (opcional), vigencia (desde/hasta), origen. | `[R06]` `[R07]` `[R08]` `[I25]` |
| 3 | `EntradaDeTarifaModificada` | Se actualizaron atributos modificables de una entrada existente. | **entradaId** (identifica la entrada a modificar), tarifa, vigencia.fechaHasta, cuantíaMínima (campos modificables). El `Factor`, la `vigencia.fechaDesde` y el `tipoTarifa` son inmutables — para cambiarlos, se cierra la entrada y se agrega una nueva. | `[R06]` `[R08]` `[I25]` |
| 4 | `EntradaDeTarifaCerrada` | Se cerró la vigencia de una entrada. La entrada sigue siendo válida para consultas dentro de su rango temporal. | **entradaId** (identifica la entrada a cerrar), vigenciaHasta (fecha de cierre). | `[R08]` `[I25]` |

#### 5.2.3. CondicionDeAplicacion — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `CondicionDeAplicacionCreada` | Se creó el conjunto de condiciones para un país. | País, origen. | — |
| 2 | `CondicionAgregada` | Se registró una nueva condición de aplicación. | **Atributos que identifican la condición (inmutables durante su ciclo de vida):** ambitoEvaluado (emisora/contraparte/sedeEmisora.jurisdiccion/sedeContraparte.jurisdiccion/lugarEjecucion.jurisdiccion/jurisdiccionResuelta), atributoEvaluado (ref. DefinicionAtributo si rol es de perfil, o atributo de `JurisdiccionFiscal` si rol es de jurisdicción), tributoAfectado, direccionFiscalAplicable, origen, vigencia.fechaDesde. **Atributos adicionales:** valorEsperado, efecto (noAplicar/cambiarTarifa/reverseCharge), tarifaAlternativa (si aplica), vigencia.fechaHasta. | `[R10]` `[R11]` `[R35]` `[D12]` `[I15]` `[I24]` |
| 3 | `CondicionModificada` | Se actualizaron atributos modificables de una condición existente. | **Atributos que identifican la condición a modificar:** ambitoEvaluado + atributoEvaluado + tributoAfectado + direccionFiscalAplicable + origen + vigencia.fechaDesde. **Campos modificables:** valorEsperado, efecto, tarifaAlternativa, vigencia.fechaHasta. Para cambiar cualquier atributo de identificación, se cierra la condición existente y se agrega una nueva — no se modifican vía este evento. | `[R10]` `[R11]` `[I24]` |
| 4 | `CondicionCerrada` | Se cerró la vigencia de una condición. La condición sigue siendo válida para evaluaciones dentro de su rango temporal. | **Atributos que identifican la condición a cerrar:** ambitoEvaluado + atributoEvaluado + tributoAfectado + direccionFiscalAplicable + origen + vigencia.fechaDesde. vigenciaHasta (fecha de cierre). | `[I24]` |

#### 5.2.4. CatalogoDeAtributosFiscales — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `CatalogoDeAtributosFiscalesCreado` | Se creó el catálogo de atributos fiscales para un país. | País, origen. | — |
| 2 | `DefinicionAtributoAgregada` | Se registró un nuevo atributo fiscal en el catálogo. | Nombre, tipo (boolean/enum/string/numerico), valoresValidos (si enum con dominio embebido), catalogoReferencia (si enum con dominio externo), requerido, vigenciaDefinicion, origen. | `[D3]` `[D13]` |
| 3 | `DefinicionAtributoModificada` | Se actualizaron propiedades de una definición (valoresValidos, catalogoReferencia, requerido). El tipo no cambia — si cambia, se cierra la definición y se crea una nueva. El cambio entre `valoresValidos` y `catalogoReferencia` (migración de dominio embebido a externo o viceversa) es una modificación válida pero requiere migración de los valores ya registrados en perfiles. | Nombre (identifica), valoresValidos, catalogoReferencia, requerido (campos modificados). | `[D3]` `[D13]` |
| 4 | `DefinicionAtributoCerrada` | Se cerró la vigencia de la definición. El atributo dejó de existir en la normativa. Los perfiles conservan valores históricos pero el motor no los evalúa. | Nombre, vigenciaHasta. | `[D3]` |

#### 5.2.5. PerfilTributario — 7 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `PerfilTributarioCreado` | Se creó el perfil fiscal de una entidad. | IdentificacionFiscal, tipoEntidad (empresa/tercero), país. | — |
| 2 | `AtributoFiscalAgregado` | Se registró un nuevo atributo fiscal en el perfil. Valor validado contra CatalogoDeAtributosFiscales. | Nombre (ref. DefinicionAtributo), valor, vigencia, fuenteDeAutoridad (opcional). | `[D3]` `[R10]` |
| 3 | `AtributoFiscalModificado` | Se actualizó el valor de un atributo fiscal existente. Nueva vigencia para el valor actualizado. | Nombre (identifica), valor nuevo, vigencia nueva, fuenteDeAutoridad (opcional). | `[D3]` |
| 4 | `AtributoFiscalCerrado` | Se cerró la vigencia de un atributo fiscal. El valor sigue siendo válido para consultas dentro de su rango temporal. | Nombre, vigenciaHasta. | — |
| 5 | `ActividadEconomicaRegistradaAgregada` | Se registró una nueva actividad económica en el perfil, con o sin especificación de jurisdicción y/o clasificación tributaria. Valida que el ciiu sea un código válido para el país y que la jurisdicción (si está poblada) referencie una `JurisdiccionFiscal` vigente. | **actividadId** (identificador único asignado al crear la entrada — preservado durante todo su ciclo de vida), Ciiu, jurisdiccion (opcional, ref a JurisdiccionFiscal.codigo), clasificacionAplicable (opcional, ref a ClasificacionTributaria.codigo), vigencia, fuenteDeAutoridad (opcional). | `[D12]` `[D14]` `[I21]` `[I27]` |
| 6 | `ActividadEconomicaRegistradaModificada` | Se actualizaron atributos modificables de una actividad económica registrada existente. | **actividadId** (identifica la actividad a modificar). **Campos modificables:** vigencia.fechaHasta, fuenteDeAutoridad. Los atributos `ciiu`, `jurisdiccion` y `clasificacionAplicable` son inmutables — para cambiarlos, se cierra la actividad y se agrega una nueva. | `[D12]` `[D14]` |
| 7 | `ActividadEconomicaRegistradaCerrada` | Se cerró la vigencia de una actividad económica registrada. La actividad sigue siendo válida para consultas dentro de su rango temporal — el motor la resuelve para transacciones con `fechaTransaccion` dentro del rango. | **actividadId** (identifica la actividad a cerrar), vigenciaHasta (fecha de cierre). | `[D14]` |

#### 5.2.6. JurisdiccionFiscal — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `JurisdiccionFiscalCreada` | Se creó el catálogo de jurisdicciones fiscales para un país. | País, origen. | `[D12]` |
| 2 | `JurisdiccionAgregada` | Se registró una nueva jurisdicción fiscal en el catálogo. | Codigo, nombre, nivelJurisdiccional, divisionTerritorialRef (opcional), tipo (territorial-administrativa/regimen-especial-territorial/distrito-fiscal-especial/soberania-tributaria), tipoRegimen (opcional), vigencia, origen. | `[D12]` `[I13]` `[I14]` |
| 3 | `JurisdiccionModificada` | Se actualizaron atributos de una jurisdicción existente. | Codigo (identifica), nombre, nivelJurisdiccional, divisionTerritorialRef, tipo, tipoRegimen, vigencia, origen (campos modificados). | `[D12]` |
| 4 | `JurisdiccionCerrada` | Se cerró la vigencia de una jurisdicción. La jurisdicción sigue siendo válida para consultas dentro de su rango temporal — `RegistroTributario` históricos conservan el código. | Codigo, vigenciaHasta. | — |

#### 5.2.7. CatalogoDeRegimenesEspeciales — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `CatalogoDeRegimenesEspecialesCreado` | Se creó el catálogo de regímenes especiales para un país. | País, origen. | `[D13]` |
| 2 | `RegimenEspecialAgregado` | Se registró un nuevo régimen especial empresarial en el catálogo. Valida que el código sea único en el país y que `jurisdiccionRef` (si está poblado) referencie una `JurisdiccionFiscal` vigente. | Codigo, nombre, tipo (zona-franca/puerto-libre-empresa/monopolio-sectorial/zona-economica-especial/regimen-especial-decreto), autoridad, jurisdiccionRef (opcional), vigencia, origen. | `[D13]` `[I16]` `[I17]` |
| 3 | `RegimenEspecialModificado` | Se actualizaron atributos de un régimen especial existente. | Codigo (identifica), nombre, tipo, autoridad, jurisdiccionRef, vigencia, origen (campos modificados). | `[D13]` |
| 4 | `RegimenEspecialCerrado` | Se cerró la vigencia de un régimen especial. Las empresas inscritas conservan el código histórico para consultas de transacciones dentro del rango temporal. | Codigo, vigenciaHasta. | — |

#### 5.2.8. HomologacionFiscal — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `HomologacionFiscalCreada` | Se creó la tabla de homologación para una autoridad fiscal. | AutoridadFiscal (nombre, jurisdicción, país), origen. | — |
| 2 | `EquivalenciaAgregada` | Se registró un nuevo mapeo entre valor interno y código de la autoridad. | ValorInterno, tributo, codigoAutoridad, nombreAutoridad, vigencia, origen. | `[D6]` |
| 3 | `EquivalenciaModificada` | Se actualizó un mapeo existente (cambio de código de la autoridad). | ValorInterno + tributo (identifican), codigoAutoridad, nombreAutoridad, vigencia (campos modificados). | `[D6]` |
| 4 | `EquivalenciaCerrada` | Se cerró la vigencia de una equivalencia. La traducción sigue disponible para consultas dentro de su rango temporal. | ValorInterno, tributo, vigenciaHasta. | — |

#### 5.2.9. FormatoFiscal — 5 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `FormatoFiscalCreado` | Se creó la definición de un formato de entregable fiscal. | TipoEntregable (reporte/certificado), AutoridadFiscal, periodicidad, formatosDeSalida, homologación (ref.), vigencia, origen. | `[R26]` `[R27]` |
| 2 | `FormatoFiscalModificado` | Se actualizaron atributos modificables del formato. | **codigo (identifica — inmutable)**, nombre, periodicidad, formatosDeSalida, vigencia (campos potencialmente modificables; los criterios de qué atributos pueden modificarse vs. requieren desactivación + nuevo dependen de la política de corrección — ver `[PD12]`). | `[R27]` `[PD12]` |
| 3 | `SeccionFormatoAgregada` | Se agregó una nueva sección al formato. | Nombre, descripción, criterioDeAgrupacion, criterioDeSeleccion, orden. | `[R26]` |
| 4 | `SeccionFormatoModificada` | Se actualizaron atributos de una sección existente. | Nombre (identifica), descripción, criterioDeAgrupacion, criterioDeSeleccion, orden (campos modificados). | `[R26]` |
| 5 | `SeccionFormatoEliminada` | Se quitó una sección del formato. Las futuras generaciones no la incluyen. | `seccionId` de la sección eliminada (identifica inequívocamente cuál sección del stream se elimina; los atributos completos de la sección viven en los eventos previos `SeccionFormatoAgregada` y `SeccionFormatoModificada` y se reconstruyen reproduciendo el stream). | — |

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
| **Precondiciones** | Confirmación recibida del sub-dominio consumidor autorizado (OXP, CXC). Contexto transaccional completo: entidades fiscales, ubicaciones, conceptos con clasificación, desglose confirmado. Cálculo de referencia obtenido por `ConfirmacionTributaria` (3.15): motor (gravámenes) o prorrateo del registro origen (desgravámenes). `[R22]` |
| **Información capturada** | ContextoTransaccional (sub-dominio, ID transacción, dirección fiscal, efectoFiscal; si desgravamen: transaccionOrigenId), EntidadFiscalEmisora (snapshot), EntidadFiscalContraparte (snapshot), Jurisdiccion resuelta, desglose confirmado (`LineaDeDesglose[]` con `proposito: confirmado` — siempre presente: tributo, naturaleza, baseGravable, tarifa, tipoTarifa, valor, factorUtilizado, conceptoOrigen), IntervencionManual. Si `huboIntervencion = true`: cálculo de referencia (`LineaDeDesglose[]` con `proposito: referencia`); adicionalmente en gravámenes: tributos descartados por el motor (`LineaDeDesglose[]` con `proposito: descartada` y `motivoExclusion`). En desgravámenes con intervención no se incluyen líneas descartadas — el motor no participa, la referencia proviene del prorrateo del registro origen. `[R23]` `[R24]` |
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
| **Información capturada** | ContenidoGenerado (filas homologadas, totalRegistrosIncluidos, fechaGeneracion, **fechaDeCorte** — fecha hasta la cual se incluyeron los `RegistroTributario` del período en esta generación; todos los registros con `fechaTransaccion ≤ fechaDeCorte` que estuvieran disponibles al momento de generar quedan incluidos). La `fechaDeCorte` deja inmutable cuáles fueron los registros considerados en esta generación: una regeneración posterior puede incluir un conjunto distinto si llegaron registros nuevos al período, pero el evento previo conserva la fecha de corte original para auditoría. ArchivoGenerado[] (tipo, referencia, hash). |
| **Efectos** | Entregable disponible para presentación o regeneración. El cursor o lista de IDs incluidos permite reconstruir qué registros conformaron este intento específico de generación. |

##### EntregableFiscalRegenerado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se regeneró el contenido del entregable. El contenido anterior y los archivos asociados fueron descartados. |
| **Agregado** | EntregableFiscal |
| **Estado previo** | Generado. |
| **Estado resultante** | Borrador. |
| **Precondiciones** | Entregable en estado Generado. `puedeRegenerar() = true`. |
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
| **Información capturada** | FechaPresentacion, responsable (usuario que confirma), **referenciaContenido** (hash del `ContenidoGenerado` presentado + hash de los archivos generados + referencia al evento `EntregableFiscalGenerado` que produjo el contenido; preserva la trazabilidad exacta de qué versión del contenido se presentó ante la autoridad). |
| **Efectos** | Entregable sellado — no se puede regenerar ni modificar. La `referenciaContenido` capturada permite verificar a posteriori que el contenido presentado coincide con el archivo conservado, y vincular el evento de presentación con la generación específica que lo produjo. Si se necesita corregir, se crea un nuevo entregable (nuevo stream). |

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
| **Información capturada** | ContenidoCertificado (líneas de detalle por tributo: baseGravable, tarifa, valor retenido; totales por tributo; fechaGeneracion), ArchivoGenerado (PDF, referencia, hash), `referenciaEnvio` (referencia externa asignada por la infraestructura al envío que se realizará tras esta generación). |
| **Efectos** | Certificado disponible para envío, regeneración o consulta. La `referenciaEnvio` capturada es la única referencia válida para reportes de resultado (Entregado/Fallido) hasta que se emita un nuevo evento de generación o reenvío que la sustituya. |

##### CertificadoTributarioRegenerado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se regeneró el contenido del certificado. El contenido anterior y el archivo fueron descartados. |
| **Agregado** | CertificadoTributario |
| **Estado previo** | Generado. |
| **Estado resultante** | Borrador. |
| **Precondiciones** | Certificado en estado Generado. `puedeRegenerar() = true`. |
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
| **Precondiciones** | Certificado en estado Generado. Infraestructura reporta envío exitoso. **La `referenciaEnvio` del reporte debe coincidir con la `referenciaEnvio` capturada en el último evento de generación o reenvío del certificado** — reportes que correspondan a un envío que ya no es el más reciente (ej: confirmación tardía de un intento anterior tras un reenvío) se descartan sin cambiar el estado del agregado. |
| **Información capturada** | ResultadoEnvio (canal, fecha, exitoso: true, `referenciaEnvio` del envío reportado). `[R28]` `[R37]` |
| **Efectos** | Certificado sellado — no se puede regenerar ni reenviar. Si se necesita corregir, se crea un nuevo certificado (nuevo stream). |

##### CertificadoTributarioFallido

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La infraestructura reportó fallo en el envío del certificado. |
| **Causalidad** | Derivado por transición — la infraestructura reporta fallo y el dominio registra el hecho. |
| **Agregado** | CertificadoTributario |
| **Estado previo** | Generado. |
| **Estado resultante** | Fallido. |
| **Precondiciones** | Certificado en estado Generado. Infraestructura reporta fallo de envío. **La `referenciaEnvio` del reporte debe coincidir con la `referenciaEnvio` capturada en el último evento de generación o reenvío del certificado** — reportes que correspondan a un envío que ya no es el más reciente se descartan sin cambiar el estado del agregado. |
| **Información capturada** | ResultadoEnvio (canal, fecha, exitoso: false, detalleFallo, `referenciaEnvio` del envío reportado). |
| **Efectos** | Certificado disponible para reintento (`esReenviable() = true`). Administrador puede corregir datos del destinatario antes de reintentar. |

##### CertificadoTributarioReenviado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se solicitó reintento de envío para un certificado cuyo envío anterior falló. |
| **Agregado** | CertificadoTributario |
| **Estado previo** | Fallido. |
| **Estado resultante** | Generado. |
| **Precondiciones** | Certificado en estado Fallido. `esReenviable() = true`. |
| **Información capturada** | Destinatario (estado actual al momento del reenvío — incluye la **corrección de datos del destinatario** que el administrador haya realizado antes del reintento, ej: corrección del correo electrónico si el envío anterior falló por dirección inválida; es responsabilidad del comando de reenvío incorporar los datos actualizados del destinatario), responsable (usuario que autoriza reintento), motivoCorreccion (opcional — texto explicativo de qué se corrigió respecto al envío anterior), `referenciaEnvio` (nueva referencia externa asignada por la infraestructura al nuevo intento — sustituye a la referencia del envío anterior fallido). |
| **Efectos** | Certificado vuelve a Generado para nuevo intento de envío por infraestructura, con el `Destinatario` actualizado si hubo corrección. La nueva `referenciaEnvio` capturada es la única referencia válida para los próximos reportes de resultado — reportes tardíos del envío anterior se descartan. Si el nuevo envío tiene éxito → CertificadoTributarioEntregado. Si falla → CertificadoTributarioFallido nuevamente. |

#### 5.3.4. ConfirmacionTributaria — 1 evento

El flujo orquestado `ConfirmacionTributaria` (Sección 3.15) emite este evento cuando la confirmación de una transacción no llega a crear el `RegistroTributario`. El evento de éxito del flujo es `RegistroTributarioCreado` (Sección 5.3.1), emitido por el agregado `RegistroTributario`.

##### ConfirmacionTributariaRechazada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El flujo de confirmación rechazó el comando del sub-dominio consumidor antes de crear el `RegistroTributario`. Cubre tanto fallas de contrato/datos como violaciones de invariantes fiscales. Permite a los consumidores reaccionar de forma observable: reintentar con corrección, escalar al usuario, o anular el lado de la transacción en el consumidor. Es la contrapartida asíncrona de `RegistroTributarioCreado` — toda confirmación produce uno u otro. |
| **Agregado** | ConfirmacionTributaria (flujo orquestado, no agregado de dominio). El evento se emite desde el flujo de aplicación porque el rechazo ocurre antes de que exista el `RegistroTributario`. |
| **Estado previo** | N/A — el flujo es efímero. |
| **Estado resultante** | N/A — el flujo termina. |
| **Precondiciones** | Comando de confirmación recibido pero alguna validación falla en pasos 1-4 del flujo `ConfirmacionTributaria` (Sección 3.15). |
| **Información capturada** | Referencia al comando original (correlación), sub-dominio consumidor, `transaccionId`, `efectoFiscal`, `motivoCodigo` (uno de): **`comando_invalido`** (el comando no cumple el contrato de confirmación), **`consumidor_no_autorizado`** (el sub-dominio que envía no tiene permiso de confirmar), **`datos_faltantes`** (falta información obligatoria del contexto — incluye perfil tributario inexistente), **`clasificacion_no_vigente`** (la clasificación tributaria del concepto no existe o está fuera de vigencia a la fecha de la transacción), **`jurisdiccion_no_encontrada`** (código de jurisdicción no precargado o no vigente), **`transaccion_ya_confirmada`** (ya existe un `RegistroTributario` para la misma combinación origen), **`desgravamen_excede_saldo`** (el desgravamen supera el saldo del gravamen origen), **`origen_no_encontrado`** (el registro tributario origen del desgravamen no existe), **`concepto_sin_tributos_aplicables`** (el motor descartó todos los tributos del concepto), **`intervencion_excede_margen`** (el desglose confirmado diverge del cálculo de referencia más allá del margen de redondeo), **`concepto_no_existe_en_origen`** (un desgravamen incluye un concepto que no estaba presente en el desglose confirmado del registro origen — la nota crédito no puede introducir tributos sobre conceptos no gravados originalmente). `motivoDetalle` (texto explicativo legible para usuario y operadores; incluye, cuando aplique, la referencia interna al `[R##]` o `[I##]` que originó el rechazo para trazabilidad operativa). Fecha del rechazo, responsable (sistema o adaptador que disparó el rechazo). |
| **Efectos** | El consumidor (OXP/CXC) recibe el evento de forma asíncrona y aplica su política de reacción: registrar el rechazo en su propio dominio, abrir un caso para revisión del usuario, retirar la transacción de su estado "confirmada", o reintentar con la corrección apropiada según el `motivoCodigo`. Impuestos no persiste un `RegistroTributario` — el hecho fiscal no existe. |

---

## 6. Invariantes del dominio

Las invariantes son restricciones estructurales que deben ser verdaderas en todo momento del ciclo de vida del dominio. A diferencia de las reglas de negocio vigentes (`[R##]`), que pueden ser configurables y tener excepciones, las invariantes son absolutas. Clasificación: **local** (enforceada por un solo agregado, transaccional) o **eventual** (cruza fronteras de agregado, enforceada por validación en escritura + proyección eventual para detección tardía).

| # | Invariante | Agregado | Referencia |
|---|-----------|----------|------------|
| I1 | **No solapamiento de vigencias:** Dos `EntradaDeTarifa` con el mismo factor y origen no pueden tener vigencias que se solapen. Verificación: `validarNoSolapamiento()` como precondición de escritura. | TarifaTributaria | `[R08]` |
| I2 | **Dependencia de tributo padre:** Si un `Tributo` declara `tributoPadre`, el padre debe existir y estar activo en el mismo `CatalogoTributario`. Tributos hijos no pueden existir sin su padre. **Verificación:** Al emitir `TributoDesactivado`, el agregado `CatalogoTributario` valida que el tributo no tenga hijos activos (otros tributos cuyo `tributoPadre` lo referencien). Si los tiene, la desactivación se rechaza con motivo `tributo_padre_tiene_hijos_activos` — el administrador debe desactivar primero los hijos. Si se quiere desactivar el padre y sus hijos en una sola operación, debe emitirse `TributoDesactivado` para cada hijo antes que para el padre. | CatalogoTributario | `[R03]` |
| I3 | **Unicidad de tratamiento:** Para una combinación (tributo × clasificación × origen), solo puede existir un `Tratamiento`. Si coexisten tratamiento estándar y personalizado para la misma combinación (tributo × clasificación), ambos se almacenan pero `tributosAplicablesA()` retorna el personalizado (precedencia). **Verificación:** Al emitir `TratamientoDefinido` (operación idempotente upsert), el agregado `CatalogoTributario` verifica que no exista otro tratamiento con la misma triple-clave `(tributo, clasificacion, origen)` activo. Si existe con el mismo origen, el nuevo evento reemplaza al anterior (upsert idempotente — misma triple-clave produce el mismo resultado). Si existe con origen distinto, ambos coexisten y `tributosAplicablesA()` aplica la precedencia personalizado > estándar al resolver. | CatalogoTributario | `[R09]` `[R14]` |
| I4 | **Unicidad de equivalencia:** En `HomologacionFiscal`, la combinación (valorInterno + tributo + origen) es única. Si coexisten estándar y personalizada, `homologar()` retorna la personalizada. | HomologacionFiscal | `[D6]` |
| I5 | **Validación de atributo fiscal (eventual):** Todo `AtributoFiscal` en `PerfilTributario` debe tener nombre, tipo y valor consistentes con una `DefinicionAtributo` vigente en `CatalogoDeAtributosFiscales` del mismo país. Verificación: `PerfilTributario` valida contra el catálogo al momento de la escritura. Si la definición se cierra después, los valores históricos se conservan pero el motor no los evalúa. | PerfilTributario, CatalogoDeAtributosFiscales | `[D3]` |
| I6 | **Condición referencia atributo existente (eventual):** Todo `Condicion.atributoEvaluado` debe referenciar una `DefinicionAtributo` que exista en `CatalogoDeAtributosFiscales` del mismo país. Verificación: `CondicionDeAplicacion` valida al momento de agregar o modificar una condición. Si la definición del atributo se cierra después de crear la condición, la condición deja de evaluarse para transacciones cuya fecha exceda la vigencia de la definición. | CondicionDeAplicacion, CatalogoDeAtributosFiscales | `[D3]` `[R35]` |
| I7 | **Unicidad de catálogo por país (eventual):** Solo puede existir un `CatalogoTributario`, un `CatalogoDeAtributosFiscales` y un `CondicionDeAplicacion` por país. Verificación: validación al crear + proyección eventual para detección tardía. | CatalogoTributario, CatalogoDeAtributosFiscales, CondicionDeAplicacion | `[D1]` |
| I8 | **Homologación completa para generación (eventual):** Al generar un `EntregableFiscal` o `CertificadoTributario`, cada `factorUtilizado` de los `RegistroTributario` incluidos debe tener una `Equivalencia` vigente en `HomologacionFiscal` de la autoridad correspondiente. Verificación: precondición de generación — si falta una equivalencia, la generación falla e indica cuáles valores no tienen traducción. | EntregableFiscal, CertificadoTributario, HomologacionFiscal | `[D6]` `[R26]` |
| I9 | **Inmutabilidad del registro tributario:** `RegistroTributario` tiene un único evento (`RegistroTributarioCreado`). No admite eventos posteriores que modifiquen su contenido. Cada registro es un hecho fiscal independiente (gravamen o desgravamen). Las proyecciones interpretan el `efectoFiscal` para determinar el signo al sumar y obtener el neto correcto. | RegistroTributario | `[D4]` |
| I10 | **Consistencia de intervención manual:** Si `IntervencionManual.huboIntervencion = true`, el registro debe contener `LineaDeDesglose[]` con `proposito: referencia` (cálculo de referencia). En gravámenes, también `LineaDeDesglose[]` con `proposito: descartada` (tributos excluidos por el motor con `motivoExclusion`). En desgravámenes, líneas descartadas no aplican — el cálculo de referencia es el prorrateo del desglose confirmado del registro origen. Si `huboIntervencion = false`, estos conjuntos no existen. Derivable del factory method `crear()` que compara `desgloseConfirmado` con `calculoDeReferencia`. **Margen de diferencia aceptado por redondeo:** la comparación entre ambos conjuntos aplica un margen de redondeo (por defecto 0,01 unidades monetarias por línea, parametrizable por país según las reglas normativas de redondeo fiscal). Diferencias dentro del margen NO se consideran intervención manual — son producto del redondeo de cálculo, no de una decisión del usuario. Diferencias fuera del margen sí marcan `huboIntervencion = true`. Esto evita falsos positivos en desgravámenes con prorrateo (donde los redondeos por concepto pueden producir diferencias menores) y en gravámenes con tarifas que generan decimales largos. | RegistroTributario | `[R24]` |
| I11a | **Progresión de estados — EntregableFiscal:** Solo las transiciones definidas en FSM 4.1. Borrador → Generado → Presentado ■. Regeneración: Generado → Borrador. No hay retroceso desde Presentado (terminal). **Si se necesita corregir un entregable en estado terminal (Presentado), la única vía es crear un nuevo agregado en un nuevo stream** — el stream original conserva el hecho histórico inmutable. **Verificación:** guard en cada comando que dispara una transición — el agregado valida el estado previo antes de emitir el evento; si la transición no está permitida, el comando se rechaza. | EntregableFiscal | — |
| I11b | **Progresión de estados — CertificadoTributario:** Solo las transiciones definidas en FSM 4.2. Borrador → Generado → Entregado ■. Fallo: Generado → Fallido. Reintento: Fallido → Generado (única salida desde Fallido). Regeneración: Generado → Borrador (única transición de regeneración — Borrador y Fallido no admiten Regenerado). No hay retroceso desde Entregado (terminal). **Si se necesita corregir un certificado en estado terminal (Entregado), la única vía es crear un nuevo agregado en un nuevo stream** — el stream original conserva el hecho histórico inmutable. **Verificación:** guard en cada comando que dispara una transición — el agregado valida el estado previo antes de emitir el evento; si la transición no está permitida, el comando se rechaza. | CertificadoTributario | — |
| I12 | **Unicidad de perfil por entidad y país (eventual):** Solo puede existir un `PerfilTributario` por combinación (entidad × país). Verificación: validación al crear + proyección eventual para detección tardía. | PerfilTributario | — |
| I13 | **Integridad referencial `subnacional` → `JurisdiccionFiscal` (eventual):** Todo `ubicaciones.{rol}.subnacional` en la solicitud al motor y todo `RegistroTributario.Jurisdiccion.subnacional` deben referenciar una `Jurisdiccion` vigente del catálogo `JurisdiccionFiscal` del país correspondiente. Verificación: el motor valida la referencia (`JurisdiccionFiscal.validarReferencia()`) como precondición de cálculo. Si el código no existe o no está vigente a la fecha de transacción, el motor rechaza el concepto con `[R30]`. Para registros históricos, el código se conserva como snapshot inmutable. **Resolución temporal:** la vigencia se evalúa contra `fechaTransaccion`, no contra la fecha actual del sistema (`[P5]`). El cierre posterior de una jurisdicción (con `fechaHasta` futura respecto a una transacción ya ocurrida) NO invalida transacciones cuya `fechaTransaccion` sea anterior al cierre — la confirmación posterior usa la misma `fechaTransaccion` que la simulación, por lo que su validación temporal es idéntica. Este patrón aplica uniformemente a todas las invariantes de integridad referencial sobre catálogos con vigencia (ver `[I1]`, `[I5]`, `[I6]`, `[I15]`, `[I16]`). | JurisdiccionFiscal, MotorDeCalculo, RegistroTributario | `[D12]` `[P5]` |
| I14 | **Coherencia `divisionTerritorialRef` → Datos de Referencia (eventual, cross-bounded-context):** Cuando `Jurisdiccion.divisionTerritorialRef` está poblado, debe referenciar un código activo del catálogo `divisiones-territoriales-{pais}` del sub-dominio Datos de Referencia. Verificación: validación al agregar/modificar la jurisdicción contra el catálogo de DR. Si la división territorial se desactiva en DR posteriormente, la `Jurisdiccion` conserva el código histórico (preserva inmutabilidad del registro fiscal) y queda **señalizada para revisión** mediante el siguiente mecanismo: una proyección de auditoría cross-BC, ejecutada por el equipo de plataforma con frecuencia periódica, verifica que cada `Jurisdiccion.divisionTerritorialRef` no nulo siga apuntando a un código activo en DR; las inconsistencias se reportan a la consola operativa del equipo fiscal para evaluar si requieren acción (ej: actualizar el código histórico, cerrar la jurisdicción si dejó de ser fiscalmente relevante). La referencia es opcional — jurisdicciones sin equivalente administrativo (distritos US, reservas indígenas) tienen `divisionTerritorialRef: null`. | JurisdiccionFiscal, Datos de Referencia | `[D12]` |
| I15 | **Coherencia `atributoEvaluado` con `ambitoEvaluado` (eventual):** Toda `Condicion` debe tener un `atributoEvaluado` coherente con el rol declarado en `ambitoEvaluado`. Si `ambitoEvaluado ∈ {emisora, contraparte}`, `atributoEvaluado` debe referenciar una `DefinicionAtributo` vigente del `CatalogoDeAtributosFiscales` del mismo país (precondición existente — extiende `[I5]` `[I6]`). Si `ambitoEvaluado ∈ {sedeEmisora.jurisdiccion, sedeContraparte.jurisdiccion, lugarEjecucion.jurisdiccion, jurisdiccionResuelta}`, `atributoEvaluado` debe ser uno de los atributos válidos del agregado `JurisdiccionFiscal`: `codigo`, `nombre`, `nivelJurisdiccional`, `tipo`, `tipoRegimen`. Verificación: `CondicionDeAplicacion` valida al agregar o modificar una condición. **Mecanismo de detección ante evolución de `JurisdiccionFiscal`:** Si en el futuro se elimina un atributo del modelo de `Jurisdiccion` que era evaluado por condiciones existentes (ej: se decide retirar `tipoRegimen` por una decisión arquitectónica), una proyección de auditoría recorre todas las condiciones con `ambitoEvaluado` de jurisdicción y reporta las que referencian el atributo eliminado. Esas condiciones quedan inválidas — el equipo fiscal las debe revisar y migrar antes de aplicar el cambio estructural. Mientras tanto, las condiciones permanecen en el catálogo pero el motor las descarta al detectar referencias a atributos no existentes (evita fallar el cálculo completo por un atributo huérfano). | CondicionDeAplicacion, CatalogoDeAtributosFiscales, JurisdiccionFiscal | `[D12]` `[R35]` |
| I16 | **Integridad referencial `AtributoFiscal.valor` → `RegimenEspecial` (eventual):** Cuando una `DefinicionAtributo` del `CatalogoDeAtributosFiscales` tiene `catalogoReferencia: "CatalogoDeRegimenesEspeciales"`, el `valor` del `AtributoFiscal` correspondiente en cualquier `PerfilTributario` debe referenciar un `RegimenEspecial` vigente del catálogo del mismo país. Verificación: `PerfilTributario` valida la referencia al momento de la escritura. **Resolución temporal:** la vigencia del régimen se evalúa contra la fecha del evento del perfil (cuando el atributo se guarda), no contra la vigencia actual del catálogo de regímenes. Esto se alinea con `[P5]` y con las invariantes equivalentes `[I13]`, `[I26]`, `[I27]`: el cierre posterior de un régimen empresarial en el catálogo no invalida atributos del perfil que fueron escritos cuando ese régimen estaba vigente — preserva la inmutabilidad histórica del perfil tributario. Si el motor consulta el atributo después y el régimen ya está cerrado, el motor lo descarta para nuevas transacciones (no genera tratamiento diferenciado), pero el atributo histórico sigue documentado para auditoría. | PerfilTributario, CatalogoDeRegimenesEspeciales, CatalogoDeAtributosFiscales | `[D13]` `[P5]` |
| I17 | **Unicidad de `RegimenEspecial.codigo` por país (local):** Dos `RegimenEspecial` con el mismo `codigo` y `origen` no pueden coexistir vigentes simultáneamente en el catálogo del país. Si coexisten entrada estándar y personalizada para el mismo código, `regimenVigenteA()` retorna la personalizada (precedencia). Esta invariante opera dentro de un único agregado raíz (`CatalogoDeRegimenesEspeciales` por país) — su naturaleza es local, no eventual, y se enforce transaccionalmente como precondición de `RegimenEspecialAgregado` y `RegimenEspecialModificado`. | CatalogoDeRegimenesEspeciales | `[D13]` |
| I18 | **Unicidad del hecho fiscal por origen transaccional (eventual):** Un mismo evento económico, identificado por la combinación de sub-dominio consumidor, transacción origen y efecto fiscal, genera exactamente **un** `RegistroTributario`. No pueden coexistir dos `RegistroTributario` confirmados con la misma combinación `(subDominio, transaccionId, efectoFiscal)`. Esta unicidad garantiza que cada hecho fiscal aparezca una sola vez en reportes, certificados y declaraciones, y que la resolución del registro origen en desgravámenes sea determinística (la búsqueda por `transaccionId` con `efectoFiscal: gravamen` retorna a lo sumo un resultado). Es el mecanismo natural de idempotencia para la confirmación tributaria — la regla de unicidad pertenece al dominio, no a la plataforma. Verificación: validación al confirmar — si ya existe un registro con la misma combinación, la confirmación se rechaza con motivo `transaccion_ya_confirmada`. **Nota sobre garantía bajo concurrencia:** la verificación previa a la escritura (check-then-write) **no es atómica** por sí sola — dos confirmaciones concurrentes de la misma transacción podrían crear dos `RegistroTributario` distintos antes de que la verificación detecte la duplicación. La garantía atómica requiere un mecanismo adicional al `expectedVersion` de plataforma (que opera por stream, no por business key). Ver `[SI01]` para sugerencias de implementación del mecanismo de serialización por business key. | RegistroTributario, ConfirmacionTributaria | `[D11]` `[SI01]` |
| I19 | **Saldo de desgravámenes acotado por el gravamen origen (eventual):** La suma de los montos desgravados sobre un mismo gravamen origen no puede exceder los montos del propio gravamen, evaluada por concepto y por tributo. Para cada (concepto × tributo) del gravamen, la suma de los valores desgravados acumulados en los `RegistroTributario` de tipo desgravamen que referencian ese gravamen debe ser menor o igual al valor del gravamen origen. La regla refleja el principio fiscal de que no se puede revertir más impuesto del que se causó originalmente. Es responsabilidad primaria del sub-dominio consumidor garantizar esta regla en su propio flujo de notas crédito y devoluciones (`[P6]`); Impuestos la verifica como red de seguridad. Verificación: validación al confirmar un desgravamen — el motor consulta los desgravámenes previos sobre el mismo gravamen origen, suma los valores acumulados por concepto y tributo, y rechaza la confirmación con motivo `desgravamen_excede_saldo` si la suma propuesta excedería el saldo disponible. **Nota sobre garantía bajo concurrencia:** la lectura del saldo previo + la verificación + la escritura del nuevo desgravamen **no es atómica** por sí sola — dos desgravámenes concurrentes sobre el mismo gravamen origen podrían cada uno leer el saldo en el mismo estado y aprobar montos que en conjunto excedan el origen. La garantía atómica requiere serialización por `transaccionOrigenId`. Ver `[SI01]` para sugerencias de implementación. | RegistroTributario, ConfirmacionTributaria | `[P6]` `[SI01]` |
| I20 | **No-solapamiento de vigencias en atributos del perfil tributario (local):** Dentro de un `PerfilTributario`, no pueden coexistir dos `AtributoFiscal` con el mismo `nombre` cuyos rangos de `Vigencia` se solapen. Si el atributo cambia de valor en el tiempo (ej: la entidad pasa de régimen simplificado a ordinario), la entrada anterior debe cerrarse (con `fechaHasta` definida) antes o al momento de agregar la nueva. Verificación: el agregado `PerfilTributario` valida no-solapamiento como precondición de `AtributoFiscalAgregado` y `AtributoFiscalModificado`. Garantiza que `atributoVigenteA(nombre, fecha)` retorne exactamente un resultado para cualquier fecha consultada. | PerfilTributario | `[P5]` |
| I21 | **No-solapamiento de vigencias en actividades económicas registradas (local):** Dentro de un `PerfilTributario`, no pueden coexistir dos `ActividadEconomicaRegistrada` con la misma combinación `(ciiu, jurisdiccion, clasificacionAplicable)` cuyos rangos de `Vigencia` se solapen. La unicidad opera por los tres atributos juntos — la misma entidad puede tener simultáneamente actividades distintas para diferentes jurisdicciones o clasificaciones, pero no dos actividades para la misma combinación. Verificación: el agregado `PerfilTributario` valida no-solapamiento como precondición de `ActividadEconomicaRegistradaAgregada` y `ActividadEconomicaRegistradaModificada`. Garantiza que `actividadEconomicaPara(jurisdiccion, clasificacion, fecha)` resuelva de forma determinística por el árbol de precedencia descrito en `[D14]`. | PerfilTributario | `[P5]` `[D14]` |
| I22 | **No-solapamiento de vigencias en equivalencias de homologación (local):** Dentro de un `HomologacionFiscal`, no pueden coexistir dos `Equivalencia` con la misma combinación de `(valorInterno, tributo, origen)` cuyos rangos de `Vigencia` se solapen. Cuando la autoridad cambia el código del reporte para un valor interno (ej: la DIAN reasigna el código de un concepto), la equivalencia anterior debe cerrarse antes o al momento de agregar la nueva. Verificación: el agregado `HomologacionFiscal` valida no-solapamiento como precondición de `EquivalenciaAgregada` y `EquivalenciaModificada`. Garantiza que `homologar(valorInterno, tributo, fecha)` retorne exactamente una equivalencia vigente para cualquier fecha consultada. Extiende `[I4]` (que cubre unicidad lógica) con la dimensión temporal. | HomologacionFiscal | `[P5]` (extiende `[I4]`) |
| I23 | **Coherencia `tipoRegimen` con `tipo` en `Jurisdiccion` (local):** El atributo `tipoRegimen` es **obligatorio** cuando `Jurisdiccion.tipo = regimen-especial-territorial` (es el discriminador del régimen, ej: `puerto-libre`, `frontera-iva-reducido`). Es **nulo** cuando `tipo = territorial-administrativa` (las jurisdicciones administrativas no tienen régimen categórico). Para los tipos `distrito-fiscal-especial` y `soberania-tributaria` (F2), `tipoRegimen` es opcional — depende del caso US/CA específico al que se aborde el país correspondiente. Verificación: `JurisdiccionFiscal` valida la coherencia como precondición de `JurisdiccionAgregada` y `JurisdiccionModificada`. Si la regla se viola al persistir, el evento se rechaza con motivo `tipo_regimen_incoherente_con_tipo`. | JurisdiccionFiscal | `[D12]` |
| I24 | **Identidad estable de `Condicion` (local):** La identidad de una `Condicion` está dada por la combinación inmutable de atributos `(ambitoEvaluado, atributoEvaluado, tributoAfectado, direccionFiscalAplicable, origen, vigencia.fechaDesde)`. Esta combinación es inmutable durante el ciclo de vida de la condición — los eventos `CondicionModificada` y `CondicionCerrada` referencian la entidad por estos atributos. Una modificación que afecte cualquier atributo de la combinación requiere `CondicionCerrada` seguido de `CondicionAgregada` (no se permite `CondicionModificada` sobre los atributos identificadores). Los campos modificables vía `CondicionModificada` son: `valorEsperado`, `efecto`, `tarifaAlternativa`, `vigencia.fechaHasta`. Verificación: `CondicionDeAplicacion` valida al modificar — si la combinación no coincide con una condición existente, el evento se rechaza. **Garantía ante reintentos del comando `CondicionAgregada`:** si el sistema recibe dos veces el mismo comando (ej: por reintento de la plataforma de mensajería), el agregado rechaza el segundo intento porque ya existe una condición vigente con la misma combinación identificadora. No se requiere una clave adicional del cliente — la propia identidad de la condición actúa como mecanismo de no-duplicación. | CondicionDeAplicacion | `[D2]` |
| I25 | **Identidad estable de `EntradaDeTarifa` (local):** Cada `EntradaDeTarifa` tiene un `entradaId` (identificador único asignado al crear la entrada en `EntradaDeTarifaAgregada`) que se preserva durante todo su ciclo de vida. Los eventos `EntradaDeTarifaModificada` y `EntradaDeTarifaCerrada` referencian la entrada por `entradaId`. Los campos modificables vía `EntradaDeTarifaModificada` son: `tarifa`, `vigencia.fechaHasta`, `cuantiaMinima`. El `factor` y la `vigencia.fechaDesde` son inmutables — para cambiarlos, se cierra la entrada y se agrega una nueva. Verificación: `TarifaTributaria` valida al modificar — si el `entradaId` no existe, el evento se rechaza. | TarifaTributaria | `[I1]` |
| I26 | **Integridad referencial `conceptos[].clasificacionTributaria` → `CatalogoTributario.ClasificacionTributaria` (eventual):** Cada concepto enviado por el sub-dominio consumidor en la solicitud al motor debe referenciar una `ClasificacionTributaria` vigente del `CatalogoTributario` del país correspondiente, evaluada a `fechaTransaccion` (`[P5]` — vigencia histórica, no actual). Verificación: el motor valida la referencia como precondición de cálculo en el paso 2.a; si la clasificación no existe o no está vigente a la fecha de la transacción, el motor rechaza el concepto con motivo `clasificacion_no_vigente` (`[R32]`). Para registros históricos, el código de clasificación se conserva como snapshot inmutable. **Resolución temporal:** análoga a `[I13]` y `[I16]` — la vigencia se evalúa contra `fechaTransaccion`, no contra la fecha actual; el cierre posterior de una clasificación no invalida transacciones anteriores cuya `fechaTransaccion` sea previa al cierre. | CatalogoTributario, MotorDeCalculo, RegistroTributario | `[R32]` `[P5]` |
| I27 | **Integridad referencial `ActividadEconomicaRegistrada.jurisdiccion` → `JurisdiccionFiscal` (eventual):** Cuando una `ActividadEconomicaRegistrada` del `PerfilTributario` declara una `jurisdiccion` (campo opcional), el código debe referenciar una `Jurisdiccion` vigente del catálogo `JurisdiccionFiscal` del mismo país. Verificación: el agregado `PerfilTributario` valida la referencia como precondición de `ActividadEconomicaRegistradaAgregada` y `ActividadEconomicaRegistradaModificada`. Si la jurisdicción no existe o no está vigente a la fecha del evento, el evento se rechaza con motivo `jurisdiccion_invalida_para_actividad`. **Resolución temporal:** análoga a `[I13]` — la vigencia se evalúa contra la fecha del evento; el cierre posterior de una jurisdicción no invalida las actividades registradas previamente que la referenciaban (preserva inmutabilidad histórica del perfil). | PerfilTributario, JurisdiccionFiscal | `[D12]` `[D14]` `[P5]` |

---

## 7. Qué NO contiene este documento

| Excluido | Razón | Dónde vive |
|----------|-------|------------|
| Glosario de términos | Ya definido | `definicion-alcance.md`, Sección 2 |
| Actores y permisos | Ya definidos | `definicion-alcance.md`, Sección 3 |
| Reglas de negocio completas | Ya definidas (`[R##]` vigentes) | `definicion-alcance.md`, Sección 6 |
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
- **Dirección fiscal** `[P2]` **→** se modela de forma **explícita** mediante dos mecanismos complementarios: (1) `Tributo.direccionFiscalAplicable` declara las direcciones donde el tributo existe normativamente (invariante del agregado — autorretenciones solo aplican en `ingreso`, AUTO_RIVA por reverseCharge solo en `gasto`); (2) `Condicion.direccionFiscalAplicable` declara las direcciones donde una condición particular se evalúa, permitiendo modelar reglas con perspectiva fiscal específica (ej: "si el proveedor es exento de retefuente no le retengo" solo aplica en `gasto`). Los roles `emisora`/`contraparte` se mantienen como posicionales fiscales (lenguaje fiscal del dominio, sin proyección por dirección): `emisora` = entidad operadora del ERP (cuyo rol comercial cambia según dirección: adquiriente en gasto, facturadora en ingreso); `contraparte` = la otra parte. La dirección no es una dimensión del catálogo, pero sí una propiedad declarada explícitamente en tributos y condiciones.
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

**Aplica a:** RegistroTributario (3.9), MotorDeCalculo, EntregableFiscal.

### [D5] Motor de cálculo stateless con evaluación completa

**Contexto:** Algunos sub-dominios consumidores necesitan calcular impuestos sin que el cálculo sea un hecho fiscal (cotizaciones, simulaciones, vista previa). Además, durante la fase de edición de una transacción, el consumidor puede solicitar múltiples cálculos a medida que el usuario ajusta conceptos, montos o clasificaciones — persistir cada uno generaría registros basura.

**Decisión:** El MotorDeCalculo siempre opera stateless: calcula y retorna la evaluación completa sin crear ningún registro. La respuesta incluye dos conjuntos: tributos aplicados (desglose propuesto) y tributos descartados con motivo estructurado de exclusión (`cuantia_minima`, `perfil_no_aplica`, `clasificacion_excluida`, `jurisdiccion_no_aplica`, `dependencia_padre`). El consumidor presenta ambos conjuntos al usuario, quien puede excluir tributos propuestos o incluir tributos descartados. La creación del `RegistroTributario` ocurre únicamente cuando el consumidor envía el comando de confirmación — en ese momento Impuestos re-ejecuta el motor internamente para obtener el cálculo original y lo contrasta con el desglose confirmado.

**Justificación:** Separa completamente el cálculo (consulta) de la persistencia (hecho fiscal). El consumidor puede solicitar N simulaciones durante la edición sin costo de almacenamiento. Los tributos descartados con motivo dan transparencia al usuario sobre el razonamiento del motor, reducen la fricción operativa ("¿por qué no me calculó ICA?") y permiten overrides informados.

**Aplica a:** MotorDeCalculo, RegistroTributario (3.9).

### [D6] Homologación fiscal como dimensión independiente

**Contexto:** Los reportes fiscales exigen clasificar las transacciones con códigos específicos de cada autoridad (ej: DIAN código "5002" para servicios, DGII código "02" para honorarios). En el sistema actual, la cuenta contable actúa como puente entre la transacción y el código del reporte. En el nuevo diseño, Impuestos no conoce cuentas contables `[R33]`.

**Decisión:** Se crea un agregado `HomologacionFiscal` (por autoridad fiscal) que mapea los valores internos del sub-dominio (`factorUtilizado`, código de clasificación) a los códigos que exige la autoridad. `FormatoFiscal` referencia la homologación de su autoridad. `EntregableFiscal` consulta la homologación durante la generación para traducir cada `LineaDeDesglose` al código correspondiente del reporte.

**Justificación:** Es el patrón convergente de Oracle Fusion Tax (`Tax Reporting Type/Code`) y Dynamics 365 (`Sales Tax Reporting Code` + `Report Layout`). Separar la homologación del tributo y del formato permite: (1) un mismo tributo se mapea a códigos diferentes según la autoridad, (2) la homologación se actualiza independientemente cuando la autoridad cambia sus códigos, (3) es contenido fiscal que viene con el producto.

**Aplica a:** HomologacionFiscal (3.10), FormatoFiscal (3.11), EntregableFiscal.

### [D7] Capacidades con distinto nivel de centralidad

**Contexto:** El bounded context de Impuestos absorbe capacidades de naturaleza distinta: configuración fiscal, perfiles tributarios, cálculo, registro tributario, reportes fiscales, certificados, carga asistida y catálogos de consulta. Todas pueden convivir dentro del mismo BC, pero si se tratan como igualmente centrales, los agregados de reportes pueden terminar empujando decisiones sobre el diseño del núcleo (cálculo y registro).

**Decisión:** Se declaran tres niveles de centralidad dentro del BC: **núcleo** (configuración, cálculo, perfil, registro tributario), **soporte** (carga asistida, catálogos jurisdiccionales), y **derivadas** (reportes, certificados, declaraciones, entregables). Regla de diseño: las capacidades derivadas consumen el núcleo pero no lo redefinen. Si un requerimiento de reportes necesita un dato que no existe en el RegistroTributario, la pregunta correcta es si ese dato debería capturarse al momento del cálculo (núcleo) — no moldear el registro para el reporte.

**Justificación:** Protege la tesis central del sub-dominio (el registro tributario y el cálculo son el centro) y evita que necesidades propias de formatos, cierres, entregas y regeneraciones contaminen el diseño del núcleo. No fragmenta prematuramente el BC, pero deja explícito que la separación es posible en el futuro si el ciclo de vida lo justifica.

**Fases de implementación:** La clasificación de capacidades determina el orden de implementación. **Fase 1** `[F1]` implementa las capacidades de Núcleo y Soporte con **cobertura multi-país LatAm** (Colombia, República Dominicana, Panamá): CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales, PerfilTributario, **JurisdiccionFiscal** (incluye regímenes territoriales LatAm como Puerto Libre San Andrés), **CatalogoDeRegimenesEspeciales** (zonas francas CO/DO, monopolios departamentales CO, zonas económicas especiales PA — ZLC, AEEPP, Ciudad del Saber), RegistroTributario, MotorDeCalculo, ConfirmacionTributaria, CargaAsistida, CatalogoJurisdiccional. **Fase 2** `[F2]` implementa las capacidades Derivadas: HomologacionFiscal, FormatoFiscal, EntregableFiscal, CertificadoTributario; **y la apertura multi-país a Estados Unidos y Canadá** — activación de los tipos `distrito-fiscal-especial` y `soberania-tributaria` en `JurisdiccionFiscal` (enum ya definido en F1, sin precarga), servicio de resolución de jurisdicción por dirección (rooftop/geocoding) y evaluación arquitectónica de proveedor fiscal externo (Avalara/Vertex/Sovos) versus catálogo propio para distritos US. Los decoradores `[F1]` y `[F2]` en los títulos de Sección 3 reflejan esta asignación: **12 agregados `[F1]`** (los 10 históricos + `JurisdiccionFiscal` formalizado por `[D12]` + `CatalogoDeRegimenesEspeciales` formalizado por `[D13]`) y **4 agregados `[F2]`** (HomologacionFiscal, FormatoFiscal, EntregableFiscal, CertificadoTributario). La apertura a US/CA es objeto del pendiente `[PD11]`.

La cobertura multi-país de F1 define la estructura de diseño y los agregados necesarios para Colombia, República Dominicana y Panamá; sin embargo, la habilitación productiva de cada país depende de su precarga certificada de contenido fiscal y puede ocurrir de forma incremental. La primera salida productiva corresponde a Colombia con OXP como consumidor inicial en dirección fiscal de gastos, según lo definido en `definicion-alcance.md`.

**Restricción de fase:** Durante la implementación de Fase 1, el diseño del núcleo no debe incorporar ajustes motivados por necesidades de formatos regulatorios, reportes o certificados que aún no se implementan. Las capacidades derivadas consumirán el registro tributario tal como lo produce el núcleo. Salvo evidencia crítica de que un dato requerido por las derivadas no puede obtenerse después, las necesidades de Fase 2 deberán adaptarse al registro — no al revés.

**Aplica a:** Todos los agregados del bounded context. Clasificación visible en Sección 3.

### [D8] Resolución de jurisdicción por regla de localización

**Contexto:** La jurisdicción fiscal no siempre es obvia. Para tributos nacionales (IVA, RETEFUENTE) siempre es el país. Para tributos municipales (ICA, RICA en Colombia) la jurisdicción depende de dónde ocurre la actividad económica: lugar de prestación del servicio, punto de entrega del bien, ubicación del inmueble o del proyecto. Si el consumidor envía una jurisdicción "resuelta", se filtra lógica fiscal al consumidor — porque determinar cuál ubicación manda es una regla fiscal.

**Decisión:** El consumidor envía un conjunto de ubicaciones tipificadas por rol semántico (`sedeEmisora`, `sedeContraparte`, `lugarEjecucion`) sin resolver cuál es la fiscalmente relevante. Cada ubicación referencia (vía `subnacional`) un código del catálogo `JurisdiccionFiscal` (`[D12]`) del país correspondiente — el motor valida la integridad referencial (`[I13]`) y resuelve la entidad `Jurisdiccion` completa para cada ubicación al inicio del cálculo. El `CatalogoTributario` contiene `ReglaDeLocalizacion` (por tributo × clasificación) que define qué rol de ubicación usar, con fallback opcional. El motor aplica la regla para resolver la jurisdicción fiscal de cada tributo de cada concepto (`jurisdiccionResuelta`). Las jurisdicciones de las 3 ubicaciones (no solo la del tributo) quedan disponibles para evaluación de condiciones — esto permite modelar regímenes territoriales especiales (Puerto Libre, frontera fiscal) que dependen del lugar y no solo del tributo (ver `[D12]` y `[I15]`).

**Justificación:** Es el patrón convergente de Oracle Fusion Tax ("Determine Place of Supply"), SAP ("Tax Jurisdiction Code determination"), Avalara/Vertex ("sourcing rules") y Dynamics 365 ("Tax Jurisdiction applicability"). En todos, la transacción provee múltiples ubicaciones candidatas y el motor selecciona cuál manda según reglas configurables. Esto mantiene la lógica fiscal centralizada en Impuestos.

**Aplica a:** CatalogoTributario (3.2), JurisdiccionFiscal (3.7), MotorDeCalculo (3.14), contrato semántico del consumidor (`[D9]`).

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

**Aplica a:** MotorDeCalculo, RegistroTributario (3.9), todos los sub-dominios consumidores.

### [D10] Event Sourcing como patrón de persistencia para todos los agregados

**Contexto:** El bounded context tiene 2 agregados transaccionales (RegistroTributario, EntregableFiscal) y 7 de configuración (CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales, PerfilTributario, HomologacionFiscal, FormatoFiscal). Los transaccionales usan ES. La pregunta es si los de configuración deben usar ES o CRUD con eventos de auditoría.

**Decisión:** Todos los agregados del bounded context usan Event Sourcing como patrón de persistencia. Los agregados de configuración persisten sus cambios como eventos inmutables en streams propios, con read models (proyecciones) para consulta.

**Justificación resumida:** (1) Inmutabilidad nativa para reconstrucción temporal regulatoria `[P4]` — demostrar ante la DIAN qué configuración aplicaba en una fecha específica. (2) Un solo modelo mental de persistencia para todo el bounded context — evita la carga cognitiva de mantener dos patrones. (3) Resiliencia operativa — proyecciones reconstruibles desde los streams. El costo de aplicar ES a agregados simples de configuración es bajo (streams cortos, eventos sencillos, sin sagas) y la alternativa (auditoría paralela en tablas) tiene problemas operativos conocidos.

**Análisis completo:** Ver `anexo-analisis-es-configuracion.md` — evaluación de 7 criterios (rendimiento, evolución de esquema, reconstrucción regulatoria, escalabilidad, operaciones masivas, mantenimiento, resiliencia) con matriz comparativa.

**Aplica a:** Todos los agregados del bounded context (Sección 3).

### [D11] Control de concurrencia, idempotencia y trazabilidad delegados a la plataforma

**Contexto:** El modelo usa Event Sourcing para todos los agregados `[D10]`. Los mecanismos de concurrencia, deduplicación y trazabilidad son concerns transversales de infraestructura.

**Decisión:** `expectedVersion` (control de concurrencia optimista): garantizada por el event store a nivel de stream. `idempotencyKey` (deduplicación de mensajes): garantizada por la plataforma de mensajería vía inbox/outbox pattern. `correlationId` (trazabilidad de procesos): propagado automáticamente por la plataforma en la cadena de mensajes. Este documento no especifica estos mecanismos por evento ni por comando — son garantías transversales de la plataforma de persistencia y mensajería.

**Compromiso operativo:** Si la plataforma de persistencia o mensajería cambia (ej: migración a un stack distinto en el futuro), el equipo de arquitectura del producto **debe revalidar que el nuevo stack provea las tres garantías declaradas arriba** antes de habilitarlo en producción. La revalidación cubre: (a) control de concurrencia optimista equivalente a `expectedVersion`; (b) deduplicación de mensajes equivalente a inbox/outbox; (c) propagación automática de `correlationId`. Sin estas tres garantías, el modelo de dominio pierde supuestos críticos sobre los que descansan invariantes como `[I18]` (unicidad del hecho fiscal) y comportamientos como la idempotencia del `RegistroTributario.crear()`. **Owner:** equipo de arquitectura del producto, en cada migración o evaluación de cambio de plataforma.

**Justificación:** Estos mecanismos son patrones de infraestructura (optimistic concurrency control, idempotent consumer, correlation identifier), no comportamiento de dominio. Especificarlos por evento duplicaría lo que la plataforma ya resuelve y contaminaría el modelo con concerns de infraestructura.

**Nota sobre la confirmación tributaria:** La unicidad del hecho fiscal por origen transaccional (`[I18]`) es una **invariante de dominio**, no una garantía delegada a la plataforma. El motor valida la combinación `(subDominio, transaccionId, efectoFiscal)` como precondición de creación del `RegistroTributario`, independientemente de los mecanismos de deduplicación de la plataforma. Esto permite detección de duplicados a nivel de dominio incluso si el inbox de la plataforma cambia o falla. Patrón análogo a `[D20]` de OXP — la referencia de origen del hecho de negocio es siempre un mecanismo de dominio, no solo una clave de plataforma. **Limitación del `expectedVersion`:** el control de concurrencia optimista declarado arriba opera **a nivel de stream individual**, no a nivel de business key. Para invariantes que cruzan agregados y dependen de la business key del hecho fiscal (como `[I18]` y `[I19]`), `expectedVersion` por sí solo no garantiza la atomicidad — se requiere un mecanismo adicional de serialización por business key. Esta separación es deliberada: las garantías generales viven en la plataforma (declaradas arriba); los mecanismos específicos para invariantes financieras críticas se documentan como sugerencias de implementación en `[SI01]` para que el equipo de implementación elija la solución concreta apropiada.

**Nota sobre el envío de certificados tributarios:** El envío de un `CertificadoTributario` por una infraestructura externa (correo electrónico, portal de autoridad) es un proceso que produce reportes de resultado asíncronos (eventos `CertificadoTributarioEntregado` y `CertificadoTributarioFallido`). El dominio captura la **referencia externa del envío** (`referenciaEnvio`) que la infraestructura asigna a cada intento, y exige que los reportes de resultado vengan correlacionados con ella. Esto permite que el agregado descarte reportes tardíos correspondientes a envíos anteriores (ej: confirmación tardía de un intento que ya fue marcado como fallido y reenviado). La deduplicación técnica de mensajes sigue siendo responsabilidad de la plataforma; la correlación por referencia externa es del dominio porque protege la coherencia del estado del certificado. Patrón análogo a la referencia de origen de pagos externos en `[D20]` de OXP.

**Nota sobre identificador del proceso de confirmación:** El `correlationId` que la plataforma propaga (jerga técnica de mensajería) se materializa para el dominio como el **identificador del proceso de confirmación**: un código único que acompaña al comando de confirmación enviado por el sub-dominio consumidor, se propaga al `RegistroTributarioCreado` resultante y, en F2, a los `EntregableFiscal` y `CertificadoTributario` que consuman ese registro tributario. Permite reconstruir la cadena completa del hecho fiscal — desde la confirmación inicial del consumidor hasta cualquier entregable o certificado derivado — sin necesidad de cruzar manualmente streams. La trazabilidad por identificador de proceso es información del dominio (vinculación de hechos fiscales); la generación técnica del código es de plataforma.

**Nota sobre detección eventual de violaciones de invariantes:** Las invariantes **eventuales** del modelo (las marcadas como tales en Sección 6: `[I5]`, `[I6]`, `[I7]`, `[I8]`, `[I12]`, `[I13]`, `[I14]`, `[I15]`, `[I16]`, `[I18]`, `[I19]`, `[I26]`, `[I27]`) cruzan fronteras de agregado o de bounded context, y por ello requieren un mecanismo complementario de detección. Se enforce primariamente en escritura (precondición del agregado o del flujo que persiste el evento). Adicionalmente, una **proyección de auditoría** recorre los streams periódicamente y reporta a la consola operativa los casos donde una invariante eventual aparece violada — sea por una condición de carrera no prevista, una migración mal ejecutada, o una limpieza incompleta. La proyección no corrige automáticamente; reporta para resolución operativa por el equipo fiscal o de soporte (la corrección puede requerir emitir eventos compensatorios manuales o reabrir el caso del hecho fiscal afectado). Las invariantes **locales** (las que operan dentro de un único agregado, como `[I1]`, `[I9]`, `[I10]`, `[I11a]`, `[I11b]`, `[I17]`, `[I20]`, `[I21]`, `[I22]`, `[I23]`, `[I24]`, `[I25]`) no requieren proyección de auditoría — su enforcement transaccional en el propio agregado las protege completamente.

**Aplica a:** Todos los agregados del bounded context (Sección 3).

### [D12] Catálogo de jurisdicciones fiscales independiente

**Contexto:** Las jurisdicciones fiscales no siempre coinciden con divisiones administrativas. En LatAm clásico (Colombia, Rep. Dominicana, Panamá), las jurisdicciones territoriales coinciden con las divisiones administrativas oficiales (departamentos, municipios, provincias). Pero existen regímenes territoriales especiales con tributación diferenciada (San Andrés Puerto Libre, Galápagos LOREG, Frontera Norte/Sur MX, ALCs Brasil) que coinciden geográficamente con divisiones administrativas pero tienen normativa fiscal propia. Y en US/CA hay jurisdicciones fiscales sin equivalente administrativo (transit districts, fire districts, BIDs, reservas indígenas con soberanía tributaria) cuyas fronteras pueden ser arbitrarias. Si el sub-dominio de Impuestos referenciara directamente el catálogo de divisiones territoriales de Datos de Referencia, no podría representar estos casos.

**Decisión:** Se introduce el agregado `JurisdiccionFiscal` (por país) como catálogo propio del sub-dominio de Impuestos. Cada `Jurisdiccion` tiene `tipo` que clasifica su naturaleza (territorial-administrativa/regimen-especial-territorial/distrito-fiscal-especial/soberania-tributaria) y `tipoRegimen` opcional para categorizar regímenes específicos (puerto-libre, frontera-iva-reducido, etc.). El atributo `divisionTerritorialRef` referencia opcionalmente el catálogo `divisiones-territoriales-{pais}` de Datos de Referencia cuando la jurisdicción coincide con una división administrativa — esto preserva la interoperabilidad con DR sin acoplar Impuestos a su esquema. El campo `ubicaciones.{rol}.subnacional` del contrato del motor (`[D9]`) referencia `Jurisdiccion.codigo`, validado por `[I13]`.

**Justificación:** Patrón convergente de Avalara (PCode), Vertex (GeoCode), SAP (Tax Jurisdiction Code), Oracle Fusion Tax (Tax Regime + Tax Jurisdiction): todos los motores fiscales mantienen un catálogo propio de jurisdicciones fiscales independiente del catálogo de geografía administrativa. Esto permite (1) modelar regímenes fiscales territoriales sin contaminar Datos de Referencia con conceptos fiscales, (2) soportar jurisdicciones puramente fiscales (distritos US) sin equivalente administrativo, (3) evolucionar el catálogo fiscal con periodicidad propia (cambios normativos no requieren actualización de DR), y (4) permitir que las condiciones de aplicación (`CondicionDeAplicacion`) evalúen atributos de la jurisdicción (tipo, tipoRegimen) para modelar excepciones territoriales sin proliferar reglas por código. La referencia opcional a DR (`divisionTerritorialRef`) preserva trazabilidad cuando coinciden y facilita la carga inicial (semilla del catálogo desde DIVIPOLA/equivalente).

**Aplica a:** JurisdiccionFiscal (3.7), TarifaTributaria (3.3), CondicionDeAplicacion (3.4), RegistroTributario (3.9), MotorDeCalculo (3.14), contrato semántico (`[D9]`).

### [D13] Catálogo de regímenes especiales empresariales

**Contexto:** Existen regímenes fiscales que aplican a **empresas específicas inscritas en un registro oficial** y NO a regiones territoriales completas: zonas francas (CO 121, DO 75, otros países), zonas económicas especiales (Panamá ZLC, AEEPP, Ciudad del Saber), monopolios departamentales de comercialización (CO licores, juegos de azar), regímenes empresariales archipelágicos (caso empresarial del régimen Puerto Libre, cuando aplica condición empresarial específica además de la ubicación territorial), regímenes otorgados por decretos individuales. Estos regímenes se distinguen de los regímenes territoriales (modelados en `JurisdiccionFiscal` con `tipo: regimen-especial-territorial`) porque la empresa debe estar **inscrita** en el registro de la autoridad correspondiente para acceder a los beneficios — no basta con ubicarse geográficamente. Algunos casos requieren ambos modelados simultáneamente (ej: MX frontera requiere inscripción en padrón SAT además de ubicación en municipio fronterizo).

**Decisión:** Se introduce el agregado `CatalogoDeRegimenesEspeciales` (por país) como catálogo propio del sub-dominio. La entidad `RegimenEspecial` tiene `codigo` (asignado por autoridad), `nombre`, `tipo` categórico, `autoridad` que lo administra, y opcionalmente `jurisdiccionRef` cuando el régimen está físicamente localizado. El catálogo se referencia desde el `PerfilTributario` mediante atributos cuyo `DefinicionAtributo` tiene `catalogoReferencia: "CatalogoDeRegimenesEspeciales"` (extiende `[D3]`) — el `AtributoFiscal.valor` del perfil contiene el código del régimen al que la entidad está inscrita, validado por `[I16]`. El enum del modelo F1 contiene cinco tipos certificados (`zona-franca`, `puerto-libre-empresa`, `monopolio-sectorial`, `zona-economica-especial`, `regimen-especial-decreto`); tipos candidatos para F2 (`polo-economico`, `inscripcion-region-fronteriza`, `area-libre-comercio`, `regimen-archipielago-empresa`, `status-indigena`) están documentados conceptualmente en `anexo-catalogo-regimenes-especiales.md` y se agregarán al enum al abordar el país correspondiente.

**Justificación:** El patrón sigue el modelado convergente de proveedores fiscales internacionales (Avalara, Vertex, SAP, Oracle) donde los regímenes empresariales se mantienen como catálogos propios del módulo fiscal, no como atributos genéricos del perfil. La distinción con `JurisdiccionFiscal` (regímenes territoriales) preserva semántica fiscal clara: territorialidad versus inscripción empresarial. La taxonomía de tipos es propia del modelo, derivada de investigación de casos reales en LatAm + casos relevantes para evolución multi-país (US/CA/MX/BR); ver `anexo-catalogo-regimenes-especiales.md` para origen, fuentes normativas y política de extensión.

**Aplica a:** CatalogoDeRegimenesEspeciales (3.8), PerfilTributario (3.6), CatalogoDeAtributosFiscales (3.5), JurisdiccionFiscal (3.7) — frontera entre regímenes empresariales y territoriales.

### [D14] Reemplazo del atributo `actividadEconomica` por la entidad `ActividadEconomicaRegistrada`

**Contexto:** En versiones iniciales del modelo, la actividad económica de una entidad fiscal se representaba como un único atributo simple (`AtributoFiscal { nombre: "actividadEconomica", valor: "codigoCIIU" }`) dentro del `PerfilTributario`. Este modelo era insuficiente para casos reales: una entidad puede ejercer actividades diferentes según jurisdicción (CIIU diferente en Bogotá vs. Medellín) o según clasificación tributaria (CIIU principal vs. CIIU por línea de negocio), y los tributos cuyo factor de tarifa es la actividad económica (ICA, RICA, autorretención de renta) requieren resolver el CIIU correcto según el contexto de cada concepto.

**Decisión:** La actividad económica se modela como entidad propia `ActividadEconomicaRegistrada` dentro del agregado `PerfilTributario`, con multiplicidad por jurisdicción y/o clasificación. El motor consulta `actividadEconomicaPara(jurisdiccion, clasificacion, fecha)` que resuelve por precedencia (combinación específica → solo jurisdicción → solo clasificación → catch-all principal). El atributo simple `actividadEconomica` del `CatalogoDeAtributosFiscales` se retira de la precarga estándar; el motor no lo consulta más.

**Justificación:** Mantener un único atributo simple obligaría a inflar las tarifas o las condiciones para reflejar diferencias por jurisdicción, contaminando el catálogo con casos del perfil. Modelar la actividad como entidad con multiplicidad mantiene la responsabilidad en el perfil (que es quien conoce las actividades reales de la entidad) y permite que el motor consulte el dato correcto según el contexto sin lógica adicional en condiciones. La transición desde el modelo anterior (atributo simple) hacia el nuevo (entidad con multiplicidad) requiere migración de los perfiles existentes — ver `[PD9]`.

**Aplica a:** `PerfilTributario` (3.6 — entidad `ActividadEconomicaRegistrada` y comportamiento `actividadEconomicaPara()`), `CatalogoDeAtributosFiscales` (3.5 — retiro de la cláusula obsoleta de la precarga estándar), `MotorDeCalculo` (3.14 — paso 2.d consulta `actividadEconomicaPara()` exclusivamente), `[PD9]` (plan de migración de perfiles).

### [D15] Política de extensión de enums fiscales en el modelo (categóricos con catálogo en anexo)

**Contexto:** El modelo declara enums categóricos en agregados que clasifican entidades fiscales por tipo (`Jurisdiccion.tipo`, `Jurisdiccion.tipoRegimen`, `RegimenEspecial.tipo`). Estos enums no enumeran códigos de entradas individuales (que viven en el catálogo, con multiplicidad por país); enumeran las **categorías estructurales** que el motor entiende para evaluar condiciones y resolver reglas fiscales. Cada categoría requiere precarga de datos certificados y a veces lógica de condición específica.

**Decisión:** El enum del modelo F1 contiene **solo las categorías que están certificadas, precargadas y probadas en los países cubiertos por F1** (CO, DO, PA). Las categorías candidatas para fases futuras (US, CA, MX, BR) se documentan conceptualmente en anexos dedicados (`anexo-catalogo-regimenes-especiales.md`) pero **no se agregan al enum del modelo hasta que se aborde el país correspondiente**. La extensión del enum es una operación de bajo costo (no introduce eventos nuevos ni invariantes nuevas — solo amplía los valores válidos), por lo que diferir su inclusión no genera deuda estructural.

**Justificación:** Mantener el enum acotado a lo certificado evita falsos positivos (lector asume cobertura que no existe), reduce la superficie de pruebas y evita que el catálogo precargado quede "vacío" para categorías declaradas. Los anexos dedicados preservan la investigación realizada para que sea reutilizable cuando llegue el momento de la extensión.

**Aplica a:** `JurisdiccionFiscal` (3.7), `CatalogoDeRegimenesEspeciales` (3.8), y cualquier futuro agregado con enums categóricos fiscales.

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
| P6 | **El sub-dominio consumidor (OXP/CXC) controla el ciclo de notas crédito y desgravámenes que emite, garantizando que la suma de desgravámenes parciales no exceda los montos del gravamen origen.** El consumidor tiene la visibilidad completa de su propio flujo de notas crédito sobre una transacción y es la primera línea de defensa contra desgravámenes excesivos. Impuestos verifica esta regla como red de seguridad fiscal (ver `[I19]`), pero el caso esperado es que el consumidor nunca envíe un desgravamen que la viole. | Normativa fiscal — no se puede revertir más impuesto del que se causó originalmente; principio de coordinación entre dominios — el dueño de un flujo es quien lo controla, otros lo verifican. | ConfirmacionTributaria, RegistroTributario, sub-dominios consumidores `[I19]` |

---

## 10. Pendientes por definir

Aspectos del modelo que requieren definición futura. Esta sección consolida los pendientes de alcance general.

| # | Pendiente | Contexto | Condición de activación |
|---|-----------|----------|------------------------|
| PD1 | **Validación final de composición y diseño — agregados de cumplimiento fiscal.** Los agregados del frente de cumplimiento (HomologacionFiscal 3.10, FormatoFiscal 3.11, EntregableFiscal 3.12, CertificadoTributario 3.13) requieren validación con datos reales para confirmar que las secciones de formato cubren todos los campos exigidos por cada autoridad y que el flujo completo FormatoFiscal → HomologacionFiscal → EntregableFiscal/CertificadoTributario no tiene gaps. **Criterios de cierre:** (a) catálogos certificados de cada autoridad cargados (DIAN para CO, DGII para DO, DGI para PA); (b) ejecución exitosa del flujo end-to-end FormatoFiscal → EntregableFiscal con datos reales de al menos un período fiscal; (c) revisión por equipo fiscal del producto que confirme que no existen campos exigidos por las autoridades que el modelo no cubra. Relacionado con `[PD5]`, `[PD6]` y `[PD7]`. | **Owner:** equipo de implementación del frente de cumplimiento (F2), en coordinación con el equipo fiscal del producto. | F2 — antes de codificar el primer agregado de cumplimiento. |
| PD2 | **Localizaciones por país — contenido fiscal.** La configuración base está documentada en anexos separados por país (`anexo-configuracion-estandar-co.md`, `anexo-configuracion-estandar-do.md`, `anexo-configuracion-estandar-pa.md`) y resumida en Sección 3.18. Pendiente: (1) catálogo completo de conceptos de pago para RETEFUENTE (~50 conceptos DIAN), (2) tarifas de ICA/RICA para municipios principales más allá de Bogotá, (3) tablas de homologación por autoridad fiscal (DIAN, DGII, DGI) para el agregado HomologacionFiscal, (4) formatos fiscales de Panamá (DGI), (5) localizaciones por país como nivel 2 del glosario en el alcance. **Criterios de cierre:** (a) catálogo completo de conceptos DIAN validado por fuente normativa; (b) tarifas municipales con respaldo en gacetas departamentales/municipales o equivalente; (c) tablas DGII / DGI con respaldo normativo vigente; (d) glosario L2 publicado en el alcance. **Anexos v1.0 creados** (CO: 11 tributos, DO: 5 tributos, PA: 4 tributos) — los ítems pendientes son datos operativos que se completan con fuentes normativas de cada jurisdicción. | **Owner:** equipo fiscal del producto + equipos de implementación por país (CO/DO/PA). | Antes del go-live productivo de cada país. |
| PD3 | **Contratos de integración con otros bounded contexts y consolidación en EventCatalog.** Los contratos entre Impuestos y los BC consumidores (OXP, CXC, Contabilidad, etc.) tienen dos momentos diferenciados que conviene no confundir: **(1) Definición progresiva de contratos al implementar cada par.** Cada vez que se construye el flujo entre Impuestos y un BC consumidor, se especifican los contratos mínimos del par **antes de iniciar el desarrollo productivo de ese flujo**: estructura de payload, semántica de campos (obligatorio / opcional / condicional), referencia a la `[R##]` o `[I##]` que los motiva, y política de versionado inicial. La base semántica ya existe en `[D9]` (contrato mínimo del consumidor) y en la Sección 3.15 (flujo `ConfirmacionTributaria`); falta materializarla como contratos navegables por par. **El primer par a especificar es OXP ↔ Impuestos, bloqueante para iniciar el desarrollo productivo de F1.** **(2) Consolidación en EventCatalog (Fase 3).** Una vez los pares estén definidos progresivamente, el EventCatalog cataloga, versiona, conecta y publica los contratos ya producidos. **El EventCatalog es la herramienta de consolidación, no el momento donde se inventan los contratos** — los contratos pueden y deben existir antes. **Criterios de cierre:** (a) contratos del par OXP ↔ Impuestos definidos antes del desarrollo productivo F1; (b) contratos de cada par BC consumidor ↔ Impuestos definidos antes del desarrollo de ese par; (c) EventCatalog navegable que conecte todos los pares ya producidos, con versionado y compatibilidad documentada. | Base semántica disponible en `[D9]` y Sección 3.15. La materialización por par es progresiva, no requiere sesión global previa. | **Owner:** (1) Equipos de implementación de cada par BC ↔ Impuestos, en coordinación con el equipo de arquitectura — definición progresiva. (2) Equipo del EventCatalog (Fase 3) — consolidación. **Momento de cierre:** (a) antes del desarrollo F1 OXP ↔ Impuestos (bloqueante); (b) antes del desarrollo de cada par siguiente; (c) al consolidar el EventCatalog en Fase 3. |
| PD4 | **Declaraciones tributarias — decisión de producto pendiente.** Las declaraciones tributarias (IVA, retención en la fuente, ICA, ITBIS, etc.) están diferidas tanto en el alcance (`definicion-alcance.md` v1.1, Sección 7 — "Fuera del alcance") como en este modelo. A diferencia de los reportes de información (exógena, municipales) que son consolidaciones de datos, las declaraciones tienen lógica propia significativa: renglones calculados, saldos a favor de períodos anteriores, compensaciones, sanciones, liquidación privada. Esta complejidad requiere un modelado dedicado que puede resultar en un agregado propio o en una extensión de `EntregableFiscal`. **La decisión de incluirlas o descartarlas del producto la toma el equipo de producto del ERP**, en coordinación con el equipo fiscal, a partir del análisis del valor que aportan al cliente versus el costo de mantenimiento normativo. **Criterios principales:** **(a)** tamaño del mercado interesado en automatizar las declaraciones desde el sistema (versus mantenerlas en hoja de cálculo o consultor externo); **(b)** costo recurrente de mantener actualizada la lógica de cada declaración por país conforme cambia la normativa; **(c)** sinergia con los reportes ya cubiertos (los reportes alimentan la declaración, pero la declaración tiene lógica propia); **(d)** disponibilidad de proveedores de servicios fiscales que cubran este frente como alternativa. **Momento de cierre:** antes de iniciar el desarrollo del frente de cumplimiento (F2) — la decisión condiciona si se modela el agregado propio o se extiende `EntregableFiscal`. Si se descarta, retirar el ítem del modelo y del alcance; si se incluye, abrir un sub-pendiente con el modelado detallado. | Diferidas en alcance y modelo. `FormatoFiscal` ya soporta `tipoEntregable` extensible para incorporarlas sin romper el modelo actual. | Equipo de producto del ERP, en coordinación con el equipo fiscal — antes de iniciar F2. |
| PD5 | **Invariantes formales de FormatoFiscal.** FormatoFiscal es el único agregado de configuración sin invariantes formalizadas en Sección 6. Restricciones implícitas identificadas a formalizar como `I##`: (a) al menos una `SeccionFormato` por formato vigente, (b) al menos un `FormatoDeSalida` por formato vigente, (c) unicidad por `(autoridad, tipoEntregable, codigo)`. **Owner:** equipo de implementación del frente de cumplimiento (F2), en coordinación con el equipo fiscal. **Momento de cierre:** al iniciar el diseño detallado del frente de cumplimiento — antes de codificar el agregado, las invariantes deben estar formalizadas en Sección 6 y enforzadas como precondición de los eventos `FormatoFiscal*` y `SeccionFormato*`. | Relacionado con `[PD1]`. | F2 — al iniciar el diseño del frente de cumplimiento. |
| PD6 | **CERRADO — Payload de `EntregableFiscalPresentado` con referencia al contenido.** El evento `EntregableFiscalPresentado` (Sección 5.3.2) captura ahora explícitamente `referenciaContenido` (hash del `ContenidoGenerado` + hash de archivos + referencia al evento `EntregableFiscalGenerado` que produjo el contenido). Esto cierra el gap original — la trazabilidad de QUÉ contenido se presentó es ahora explícita y verificable, sin depender de reconstrucción del stream. | Cerrado. | — (cerrado en revisión de bloque B7 de auditoría). |
| PD7 | **Generación masiva de certificados tributarios — diferida a F2.** La operación de generar todos los certificados de retención de un período fiscal (puede ser miles de certificados por ciclo anual) se menciona en 3.13 como "proceso que crea N certificados a la vez". Su modelado detallado se difiere hasta el inicio de la implementación de F2 — momento en el que las condiciones reales de operación (volúmenes esperados, tiempos de respuesta, capacidades disponibles) estarán definidas y guiarán las decisiones de diseño. **Cuando se modele, el diseño debe responder explícitamente:** (1) **Qué inicia el proceso** — qué actor o evento dispara la generación masiva (administrador fiscal, calendario fiscal, evento externo) y con qué información (período, autoridad fiscal, alcance del lote). (2) **Seguimiento del avance** — cómo se conoce en cualquier momento cuántos certificados se han generado, cuántos están pendientes y cuántos fallaron, sin tener que consultar uno a uno. (3) **Comportamiento ante fallos parciales** — qué hacer cuando una parte del lote falla mientras otra se procesó correctamente (continuar con el resto, reintentar solo los fallidos, abortar el lote, mantener registro de lo procesado). (4) **Identificador del lote** — un código común que permita agrupar todos los certificados generados juntos para revisión y auditoría. (5) **Continuidad del proceso** — qué pasa si el sistema se interrumpe a la mitad del lote y debe retomarse. (6) **Si el lote es una entidad propia o solo información del proceso** — el documento contempla **dos planteamientos preliminares** que se evaluarán al modelar F2: **(a)** el lote es **información del proceso** (read model) — los certificados solo comparten un identificador común y la agrupación se reconstruye por consulta; ventaja: menos estructura, los certificados conservan su autonomía; **(b)** el lote es una **entidad propia** con identidad, ciclo de vida y estado (creado, en progreso, completado, fallido); ventaja: seguimiento operativo de primera clase, recuperación natural ante caídas. La línea 3.13 del modelo actualmente sugiere (a) como postura preliminar, pero la decisión final se tomará al modelar F2 con visibilidad de las restricciones reales de operación, y se formalizará entonces como una decisión `[D##]`. Como referencia, el proceso del extracto bancario en OXP (`OxpExtracto`) maneja una situación análoga de proceso multi-paso con seguimiento. La decisión `[D11]` actual delega ciertos aspectos a la plataforma — eso seguirá aplicando, pero la generación masiva tiene comportamiento propio que requiere documentación explícita. | Relacionado con `[PD1]` y `[PD6]`. | F2 — al iniciar la implementación del frente de cumplimiento. Coordinada con `PD1`. |
| PD8 | **Precarga inicial del catálogo `JurisdiccionFiscal` por país.** El agregado `JurisdiccionFiscal` cubre cuatro tipos de jurisdicción (`territorial-administrativa`, `regimen-especial-territorial`, `distrito-fiscal-especial`, `soberania-tributaria`). F1 precarga las jurisdicciones aplicables a LatAm (CO/DO/PA) en los dos primeros tipos; F2 abordará los dos tipos restantes (US/CA) — ver `[PD11]`. **Cobertura inicial F1 — criterios de selección de la lista canónica del go-live** (los datos específicos se definen en el proceso operativo de carga inicial de catálogos certificados por país): **(a)** Jurisdicciones nacionales de cada país (CO, DO, PA). **(b)** Divisiones territoriales administrativas relevantes para la operación: departamentos/provincias completos, y municipios donde el cliente ERP opera fiscalmente o donde aplican tributos subnacionales (ICA y RICA en Colombia: ciudades capitales + municipios identificados como fiscalmente relevantes para el cliente). La carga inicial usa el catálogo de Datos de Referencia como semilla cuando aplique (`divisionTerritorialRef` poblado desde DIVIPOLA/equivalente). **(c)** Regímenes territoriales especiales conocidos: Puerto Libre San Andrés (CO — municipios 88001, 88564, con `tipoRegimen: puerto-libre`), y otros que el equipo fiscal certifique. **Comportamiento ante una jurisdicción no precargada:** El motor rechaza el cálculo con `motivoCodigo: jurisdiccion_no_encontrada` y el sistema emite `ConfirmacionTributariaRechazada` (ver Sección 5.3.4) con el detalle de la jurisdicción no encontrada. El consumidor recibe el evento y aplica su política de reacción (registrar el caso, escalar al equipo fiscal, retirar la transacción de su flujo). El sistema **no usa fallback a una jurisdicción genérica** — la regla de integridad referencial (`[I13]`) es estricta para preservar la coherencia fiscal del cálculo. **Procedimiento de expansión bajo demanda:** Cuando se detectan rechazos `jurisdiccion_no_encontrada` recurrentes para una jurisdicción no precargada, el equipo fiscal del cliente (o del producto si es estándar) evalúa el caso y agrega la jurisdicción al catálogo via el flujo de configuración (`JurisdiccionAgregada`). La expansión es deliberada — no automática — para preservar la curaduría del catálogo. Una vez agregada, las transacciones subsecuentes con ese código se procesarán normalmente; las transacciones rechazadas previamente requieren ser reintentadas por el consumidor. **Riesgo operativo si la lista canónica es insuficiente al go-live:** Volumen alto de rechazos `jurisdiccion_no_encontrada` que el consumidor debe procesar. El equipo fiscal debe estimar la cobertura esperada antes del lanzamiento productivo y trabajar con el equipo de implementación del cliente para identificar las jurisdicciones operativas. | Modelo del agregado `JurisdiccionFiscal` (3.7) y comportamiento de rechazo asíncrono (`ConfirmacionTributariaRechazada` con `motivoCodigo: jurisdiccion_no_encontrada`) ya definidos. La precarga real de datos por país se aborda en la fase operativa previa al go-live productivo (carga inicial de catálogos certificados). | Antes del lanzamiento productivo del motor en cada país. Coordinada con la carga inicial de catálogos certificados por país (proceso operativo previo al go-live productivo). |
| PD9 | **Catálogo certificado de actividades económicas (CIIU) por país y migración de `actividadEconomica`.** Decisión de diseño que la motiva: `[D14]`. La entidad `ActividadEconomicaRegistrada` reemplaza el atributo simple `actividadEconomica` que vivía en `AtributoFiscal`. Esto requiere: (1) precarga del catálogo certificado de códigos CIIU (Colombia DANE), su equivalente dominicano (CNAE-Rev. 4 DGII) y panameño (clasificación industrial DGI), (2) retiro de la `DefinicionAtributo` `actividadEconomica` de la precarga estándar de `CatalogoDeAtributosFiscales`, (3) migración de perfiles existentes con el atributo simple a `ActividadEconomicaRegistrada` (catch-all con el ciiu del atributo simple, sin jurisdicción ni clasificación), (4) validación de que el motor consulta exclusivamente `actividadEconomicaPara()` y no el atributo simple. **Riesgo operativo si no se ejecuta antes del go-live:** los perfiles con el atributo simple no aportarán actividad económica al motor — los tributos con factor de tarifa `actividadEconomica` (ICA, RICA, autorretención de renta) se descartarán con `motivoExclusion: actividad_no_registrada` y no se calcularán. La migración (paso 3) es por lo tanto **bloqueante** para el lanzamiento productivo del motor en países que ya tengan perfiles con el atributo simple. | `ActividadEconomicaRegistrada` (3.6) está modelada. Decisión `[D14]` formalizada. La precarga de catálogos certificados se aborda en la fase operativa previa al go-live productivo. | Antes del lanzamiento productivo del motor — bloqueante. Coordinada con la carga inicial de catálogos certificados por país (proceso operativo previo al go-live productivo). |
| PD10 | **Precarga inicial del catálogo `CatalogoDeRegimenesEspeciales` por país.** F1 carga los regímenes empresariales certificados para CO, DO, PA (ver `anexo-catalogo-regimenes-especiales.md` Sección 7): (1) Colombia — 121 zonas francas (códigos DIAN), monopolios departamentales (~33 entradas para licores), regímenes empresariales archipelágicos si aplica el caso empresarial, (2) República Dominicana — 75 parques de zona franca (códigos CNZFE), (3) Panamá — Zona Libre de Colón, AEEPP, Ciudad del Saber. Los tipos candidatos para F2 (`polo-economico`, `inscripcion-region-fronteriza`, `area-libre-comercio`, `regimen-archipielago-empresa`, `status-indigena`) están documentados pero NO incluidos en el enum F1 — se agregan al modelo cuando se aborde el país correspondiente. | `CatalogoDeRegimenesEspeciales` (3.8) está modelado. La precarga real se gestiona en la fase operativa previa al go-live productivo con certificación del equipo de negocio. | Antes del lanzamiento productivo del motor. Coordinada con la carga inicial de catálogos certificados por país (proceso operativo previo al go-live productivo). |
| PD11 | **Apertura multi-país a Estados Unidos y Canadá (F2).** F1 cubre LatAm (CO/DO/PA). La extensión a US/CA requiere cuatro líneas de trabajo: (1) **Precarga inicial** de jurisdicciones con `tipo: distrito-fiscal-especial` (transit districts, fire districts, water districts, BIDs, TIFs) y `tipo: soberania-tributaria` (reservas indígenas con potestad fiscal propia, First Nations CA) — los tipos ya están definidos en el enum de `JurisdiccionFiscal` (Sección 3.7) y no requieren cambio estructural del modelo, solo carga de datos; (2) **Servicio de resolución de jurisdicción por dirección** (rooftop/geocoding) — hoy el motor recibe el código de jurisdicción del consumidor; los flujos US requieren resolver el código a partir de una dirección postal porque la pertenencia a distritos especiales no se deduce del código postal ni del condado; (3) **Decisión arquitectónica de proveedor fiscal externo** (Avalara/Vertex/Sovos) versus mantener catálogo propio — depende del costo de mantenimiento de las >100.000 jurisdicciones US y de la frecuencia de cambio normativo a nivel estatal/local; (4) **Activación de tipos candidatos** del enum de `CatalogoDeRegimenesEspeciales` (`polo-economico`, `inscripcion-region-fronteriza`, `area-libre-comercio`, `regimen-archipielago-empresa`, `status-indigena`) — documentados en `anexo-catalogo-regimenes-especiales.md` Sección 4 pero fuera del enum F1; la extensión del enum es un cambio de bajo costo (no introduce eventos nuevos, solo amplía valores válidos). **Criterios de decisión para proveedor fiscal externo vs catálogo propio (línea 3):** **(a)** costo de mantenimiento estimado de las jurisdicciones US (alto: >100.000 jurisdicciones, cambios estatales/locales frecuentes); **(b)** frecuencia normativa de cambios fiscales a nivel estatal/local en los estados objetivo; **(c)** SLA esperado para incorporar cambios fiscales después de su publicación normativa; **(d)** disponibilidad y costo de los proveedores fiscales (Avalara, Vertex, Sovos); **(e)** experiencia y capacidad del equipo fiscal interno para mantener un catálogo propio en US. **Owner:** equipo de arquitectura del producto + equipo de producto, en coordinación con el equipo fiscal. **Momento de cierre:** antes del primer cliente productivo US/CA — la decisión condiciona la arquitectura del motor y del contrato con consumidores. | Los tipos `distrito-fiscal-especial` y `soberania-tributaria` ya están en el enum de `JurisdiccionFiscal` (decisión `[D12]`, Sección 3.7). Los tipos candidatos para `CatalogoDeRegimenesEspeciales` están documentados en `anexo-catalogo-regimenes-especiales.md` (decisión `[D13]` + política `[D15]`). La precarga real, el servicio de geocoding y la decisión proveedor externo son trabajo de F2. | Equipo de arquitectura + equipo de producto, antes del primer cliente productivo US/CA. |
| PD12 | **Política de corrección de errores en la configuración del catálogo fiscal.** El modelo permite modificar atributos de `Tributo` y `ClasificacionTributaria` vía eventos `*Modificado`, pero **no documenta cuándo es apropiado modificar vs. cuándo es apropiado desactivar y crear uno nuevo**. La distinción es relevante porque conviven dos políticas: **(1) Evolución normativa** — cuando la autoridad fiscal cambia un tributo (reforma tributaria, ajuste de factor, etc.), el patrón estándar `[P5]` aplica: el tributo actual se desactiva con vigencia futura y se agrega uno nuevo con código distinto y la nueva configuración. Esto preserva trazabilidad histórica (la autoridad fiscal auditará períodos pasados con la configuración vigente entonces). **(2) Corrección de error de carga** — cuando el equipo fiscal cargó incorrectamente un atributo (ej: registró un tributo como `sustractivo` cuando debe ser `aditivo`), el camino correcto puede ser modificación directa (si no afecta registros históricos correctos) o desactivación + nuevo (si se debe preservar la versión "errónea" para auditoría interna). **Lo que el equipo fiscal del producto debe definir como política, por tipo de atributo:** **(a)** cuáles atributos del `Tributo` son **inmutables salvo corrección de error** y bajo qué procedimiento (candidatos: `naturaleza`, `factorDeTarifa`, `direccionFiscalAplicable`); **(b)** cuáles son **libremente modificables** porque no afectan registros históricos (candidatos: `nombre` descriptivo, posiblemente `caracterRetención`); **(c)** ventana de tiempo dentro de la cual una modificación se considera "corrección de carga" vs. "evolución normativa"; **(d)** si los cambios de atributos sensibles requieren aprobación adicional (rol de auditor, doble validación, registro de motivo). | El modelo no impone restricción hoy más allá de la inmutabilidad del `codigo`. Los registros tributarios históricos están protegidos por el snapshot de `LineaDeDesglose` independientemente de modificaciones futuras del catálogo. Relacionado con `[PD2]` (localización por país — la política puede variar por jurisdicción). | **Owner:** equipo fiscal del producto, en coordinación con el equipo de implementación. Antes del go-live productivo, junto con `[PD2]`. |

---

## 11. Sugerencias de implementación

Las sugerencias de implementación (`[SI##]`) son recomendaciones que **no son parte del modelo de dominio** — son guías para el equipo de implementación sobre cómo materializar invariantes o procesos que el dominio declara pero cuya forma concreta depende de la plataforma y de las garantías que ésta provea. Las sugerencias son no vinculantes: la implementación puede elegir el mecanismo apropiado según el contexto tecnológico, pero **debe documentar cuál mecanismo eligió** como parte del runbook de despliegue para que el equipo fiscal pueda verificar que la invariante queda protegida.

### [SI01] Serialización por business key para invariantes financieras críticas

**Invariantes que cubre:** `[I18]` (unicidad del hecho fiscal por origen transaccional) y `[I19]` (saldo de desgravámenes acotado por gravamen origen).

**Contexto:** Ambas invariantes protegen propiedades financieras críticas y dependen de una **business key** (combinación de identificadores del hecho de negocio, no del stream). El `expectedVersion` declarado por `[D11]` opera **por stream individual** — dos confirmaciones concurrentes que crean dos streams nuevos no se detectan entre sí. La verificación previa a la escritura (check-then-write) no es atómica por sí sola: dos comandos concurrentes pueden cada uno verificar el estado actual, pasar la verificación, y crear ambos un `RegistroTributario` con la misma combinación o exceder el saldo del gravamen origen.

**Sugerencias concretas (la implementación elige una o combina varias):**

**(a) Índice único sobre la business key del hecho fiscal.** Mantener un índice/constraint a nivel de plataforma sobre la combinación `(subDominio, transaccionId, efectoFiscal)` para `RegistroTributario`. Una violación de unicidad del índice rechaza el segundo intento de creación de forma atómica, incluso si llegan en paralelo. Esto cubre `[I18]` sin necesidad de un agregado-ledger adicional.

**(b) Agregado-ledger por transacción origen.** Para `[I19]`, modelar un agregado de saldo por `transaccionOrigenId` (un stream único por cada gravamen que recibe desgravámenes) que serialice las confirmaciones de desgravamen sobre ese origen. El `expectedVersion` del ledger garantiza que dos desgravámenes concurrentes no puedan leer el mismo saldo y aprobar montos que en conjunto excedan el origen.

**(c) Serialización por business key vía mensajería.** Algunas plataformas de mensajería permiten configurar el procesamiento secuencial de mensajes por una clave de partición (ej: `transaccionId` o `transaccionOrigenId`). Si la plataforma garantiza orden por partición, dos comandos con la misma business key se procesan secuencialmente, lo que es equivalente a serialización a nivel de aplicación.

**Decisión esperada en el runbook de despliegue:**

El equipo de implementación debe documentar:
1. Cuál mecanismo se eligió (a, b, c o combinación).
2. Cómo se verifica operativamente que el mecanismo está activo (ej: pruebas de carga concurrente, métricas de violación detectada).
3. Qué hacer si el mecanismo falla (ej: alerta, conciliación manual, cierre temporal del servicio).

**Patrón análogo en otros sub-dominios:** Esta sugerencia se inspira en cómo OXP maneja la unicidad de pagos externos por referencia de origen (ver `[D20]` de OXP — "Nota sobre pagos externos"). El patrón general es **"la invariante de dominio declara la regla; la implementación elige el mecanismo de plataforma que la materializa"**.

### [SI02] Lectura del registro origen en desgravámenes — garantía read-your-writes

**Contexto:** El paso 3.b del flujo `ConfirmacionTributaria` (Sección 3.15) resuelve el `RegistroTributario` origen del desgravamen buscándolo por `transaccionId = transaccionOrigenId`. Como cada `RegistroTributario` vive en su propio stream y la búsqueda típicamente se realiza contra una proyección/read model indexada por `transaccionId`, existe una ventana de consistencia eventual entre el evento `RegistroTributarioCreado` del gravamen y su disponibilidad en la proyección de lookup. Un desgravamen recibido pocos milisegundos después del gravamen origen podría ser rechazado erróneamente con motivo `origen_no_encontrado` si la proyección aún no indexó el gravamen.

**Sugerencias concretas (la implementación elige una o combina varias):**

**(a) Lectura directa del event store.** El flujo de desgravamen consulta directamente el stream `registro-tributario-{guid}` (lectura fuerte del event store) en lugar de pasar por una proyección. Garantiza visibilidad inmediata del gravamen recién persistido. Costo: requiere índice secundario o búsqueda por business key en el event store, lo cual no siempre es eficiente en todas las plataformas.

**(b) Proyección con garantía read-your-writes.** Usar una proyección/read model que la plataforma garantice consistente con las escrituras recientes del mismo proceso (read-your-writes consistency). El flujo escribe el gravamen y posteriormente el desgravamen contra la misma proyección — la plataforma asegura que la segunda lectura ve la primera escritura. Coherente con `[D11]` (delegar garantías a plataforma).

**(c) Política de espera con reintento.** Si la plataforma no garantiza read-your-writes nativamente, el flujo aplica una política de espera bounded con reintentos (ej: hasta N reintentos espaciados M ms) antes de rechazar con `origen_no_encontrado`. Útil cuando la proyección converge en tiempos predecibles. Costo: latencia adicional en el rechazo legítimo (origen realmente inexistente).

**Patrón análogo en otros sub-dominios:** Esta sugerencia se inspira en cómo Contabilidad maneja la consistencia eventual entre la creación de borradores y su consulta posterior (ver `[SI1]` y `[SI2]` de Contabilidad sobre optimistic concurrency en streams contables), y en cómo OXP coordina escrituras multi-agregado vía domain services con consistencia eventual coordinada (ver `[D3]` de OXP y `[SI3]` sobre Wolverine Saga).

**Decisión esperada en el runbook de despliegue:**

El equipo de implementación debe documentar:
1. Cuál mecanismo se eligió (a, b, c o combinación) para garantizar la visibilidad inmediata.
2. Métricas operativas: tasa de rechazos `origen_no_encontrado` para detectar si la ventana de consistencia está afectando casos legítimos.
3. Política de tolerancia si el rechazo persiste tras reintentos.

### [SI03] Vista consistente de la configuración fiscal para el `MotorDeCalculo`

**Aspectos que cubre:** `[D5]` (motor stateless con evaluación completa), `[R06]` (vigencia temporal de reglas), `[R07]` (fecha de la transacción rige el cálculo).

**Contexto:** Una invocación del `MotorDeCalculo` consulta múltiples agregados de configuración para resolver un mismo cálculo a `fechaTransaccion`: `CatalogoTributario` (tributos, clasificaciones, reglas de localización), `TarifaTributaria` (tarifas vigentes), `CondicionDeAplicacion` (condiciones por perfil/jurisdicción), `CatalogoDeAtributosFiscales`, `PerfilTributario` (entidad emisora y contraparte), `JurisdiccionFiscal`, `CatalogoDeRegimenesEspeciales`. Cada uno vive en su propio stream y, según `[D11]`, el `expectedVersion` garantiza concurrencia optimista **por stream individual** — no entre catálogos. Si las lecturas que alimentan un mismo cálculo se sirven desde proyecciones eventualmente consistentes que convergen en momentos distintos, dos invocaciones del motor sobre el mismo contexto y la misma `fechaTransaccion` podrían producir resultados diferentes mientras una de las proyecciones está rezagada respecto a otra.

**Propiedad de dominio que se debe preservar:** Para un mismo contexto transaccional (entidades fiscales, conceptos, ubicaciones, moneda) y la misma `fechaTransaccion`, el resultado del motor (desglose propuesto + tributos descartados con motivo) debe ser **idéntico** independientemente del momento de invocación. Esta propiedad sustenta la auditabilidad fiscal: en la re-ejecución del motor durante la confirmación (`[R22]`, paso 1 del flujo `ConfirmacionTributaria` en Sección 3.15) y en cualquier reproducción posterior del cálculo, el resultado debe coincidir con el original mientras la configuración a `fechaTransaccion` no haya cambiado.

**Sugerencias concretas (la implementación elige una como estrategia principal; puede complementarla con caché o fallback, pero un mismo cálculo se sirve desde una sola fuente):**

**(a) Reconstrucción directa desde streams.** Cada invocación del motor reconstruye el estado de los agregados de configuración a partir de sus streams (lectura fuerte). Garantiza visibilidad inmediata de cualquier evento `*Modificado`, `*Definido` o `*Desactivado` ya persistido. Costo: latencia de reconstrucción por invocación; se puede mitigar con caché en memoria invalidado por suscripción a eventos.

**(b) Snapshots certificados de configuración fiscal por país y fecha.** La plataforma materializa snapshots inmutables de la configuración vigente por país y por fecha de corte (ej: snapshot por cada publicación de cambio normativo). El motor toma como entrada la `fechaTransaccion` y resuelve el snapshot aplicable. Garantiza coherencia entre catálogos consultados en una misma ejecución, al costo de un proceso de generación y versionado de snapshots.

**(c) Read models con garantía de coherencia por marca de versión.** Cada agregado de configuración publica eventos con un identificador de versión que la proyección consume y persiste. El motor lee el conjunto de read models exigiendo que la marca de versión de cada uno sea coherente para `fechaTransaccion`. Si alguna proyección está rezagada, el motor espera (bounded) o rechaza con error operativo. Coherente con `[D11]` (delegar garantías a plataforma).

**Restricción:** Las proyecciones eventualmente consistentes pueden usarse libremente para **consulta, administración o navegación** del catálogo. La restricción aplica solo al motor productivo cuando produce el cálculo que alimentará la `ConfirmacionTributaria` (o cuando se re-ejecuta como referencia durante la confirmación) — ahí la coherencia entre catálogos es propiedad de dominio.

**Decisión esperada en el runbook de despliegue:**

El equipo de implementación debe documentar:
1. Cuál estrategia principal se eligió (a, b o c) y, si aplica, qué capa complementaria la acompaña (caché invalidado por eventos, fallback a reconstrucción cuando no hay snapshot, etc.).
2. Cómo se verifica la propiedad de reproducibilidad — ej: pruebas que invocan el motor dos veces con el mismo contexto y `fechaTransaccion` y comparan resultados.
3. Política operativa cuando se detecta rezago en una proyección (espera bounded, rechazo, alerta).

**Patrón análogo:** Sigue el mismo patrón de `[SI01]` y `[SI02]` — la propiedad fiscal se declara en el dominio (`[D5]`, `[R06]`, `[R07]`); la materialización se delega a la implementación con sugerencias no vinculantes y obligación de documentación en el runbook.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: 10 agregados, 45 eventos, 2 máquinas de estado, 12 invariantes, 11 decisiones de diseño, 7 pendientes. |
| 2.0 | Mayo 2026 | Evolución estructural multi-país: **(Cambio 1)** Resolución del gap de actividad económica → nueva entidad `ActividadEconomicaRegistrada` en `PerfilTributario` con multiplicidad por jurisdicción + clasificación, comportamiento `actividadEconomicaPara()`, paso 2.d del motor reescrito para usar el rol del sujeto pasivo declarado en la condición activadora + dirección fiscal, reorganización de condiciones RICA/RETEFUENTE en anexo CO, `direccionFiscalAplicable` aplicado a `Condicion` (no a `Tributo`), retiro de `actividadEconomica` del `CatalogoDeAtributosFiscales` (ver `[PD9]`). **(Cambio 2)** Nuevo agregado `JurisdiccionFiscal` con regímenes territoriales (Puerto Libre San Andrés), decisión `[D12]`, invariantes `[I13]`-`[I15]`, 4 eventos, 4 tipos de jurisdicción (`territorial-administrativa`, `regimen-especial-territorial` con precarga F1; `distrito-fiscal-especial`, `soberania-tributaria` declarados sin precarga para F2), extensión de `Condicion.ambitoEvaluado` con roles de jurisdicción, atributo categórico `tipoRegimen`, `[I13]` y `[I14]` para integridad referencial, `[PD8]` para precarga inicial. **(Cambio 3)** Nuevo agregado `CatalogoDeRegimenesEspeciales` con regímenes empresariales (zonas francas DIAN/CNZFE, monopolios departamentales CO Ley 1816/2016, ZEEs panameñas ZLC/AEEPP/Ciudad del Saber, Puerto Libre empresarial), decisión `[D13]`, invariantes `[I16]`-`[I17]`, 4 eventos, enum F1 con 5 tipos certificados, `DefinicionAtributo` extendida con campo opcional `catalogoReferencia` para enums extensos, `PerfilTributario.regimenesEspecialesVigentes()`, paso 2.c del motor extendido para evaluar regímenes empresariales, anexo nuevo `anexo-catalogo-regimenes-especiales.md` con fuentes normativas, `[PD10]` para precarga inicial. **(Cambio 4)** Reordenamiento F1/F2 multi-país — F1 cubre CO/DO/PA (LatAm completo con regímenes territoriales y empresariales precargados); F2 cubre apertura a US/CA (activación de tipos `distrito-fiscal-especial` y `soberania-tributaria`, resolución de jurisdicción por dirección/geocoding, decisión arquitectónica proveedor fiscal externo vs catálogo propio, tipos candidatos de `CatalogoDeRegimenesEspeciales`), `[D7]` actualizada, `[PD11]` consolida la apertura US/CA. |
| 2.0.1 | Mayo 2026 | **Auditoría completa aplicada (97 hallazgos tratados, 2 descartados conscientes).** Hallazgos Alta resueltos (20): sincronización del documento con v2.0 (conteos, changelog, diagrama BC), unicidad del hecho fiscal por origen transaccional (`[I18]` + nota en `[D11]`), saldo de desgravámenes acotado por gravamen origen (`[I19]` + `[P6]`), evento de rechazo del flujo asíncrono (`ConfirmacionTributariaRechazada` con motivo estructurado, Sección 5.3.4), referencia externa del envío de certificados (`referenciaEnvio` en `ResultadoEnvio`), formalización de la decisión `[D14]` (reemplazo de `actividadEconomica` por entidad), `[PD4]`/`[PD7]`/`[PD8]` refinados con owner/criterios/momento de cierre. Hallazgos Media resueltos (47 + 8 ya cubiertos en Alta, 1 descartado): identidad de entidades internas (tuplas inmutables / IDs sintéticos + `[I24]` `[I25]`), no-solapamiento de vigencias (`[I20]` `[I21]` `[I22]`), enforcement explícito de invariantes (`[I2]` `[I3]` `[I15]`), coherencia `tipoRegimen` ↔ `tipo` (`[I23]`), composición unificada de `LineaDeDesglose` con `proposito`, `tipoEntregable` formalizado en `FormatoFiscal`, política de extensión de enums (`[D15]`), reformulación de PD7 (generación masiva diferida a F2 con dos planteamientos preliminares), payload de `EntregableFiscalPresentado` con `referenciaContenido` (`PD6` cerrado), `EntregableFiscalGenerado` con cursor de registros incluidos, `SeccionFormato` con `seccionId` estable, `puedeGenerar()` + `puedeRegenerar()` separados, identificador del proceso de confirmación + detección eventual de violaciones en `[D11]`, flujo de revisión humana en `CargaAsistida`, margen de redondeo en `[I10]`, renombre `entidadEvaluada → ambitoEvaluado` y `municipioRef → jurisdiccionRef` (lenguaje ubicuo multi-país), responsabilidades del motor reclasificadas (evaluación de condiciones, prorrateo en agregado origen), `jurisdiccionResuelta` movida a cada línea aplicada del resultado, `[I26]` para integridad referencial de clasificación tributaria, `[PD5]`/`[PD11]` refinados con owner/criterios. Hallazgos Baja resueltos (13 + 7 ya cubiertos, 1 descartado): consistencia notacional (`tarifaAlternativa`, `ambas` como comodín), enforcement guard en `[I11a]`/`[I11b]`, `[I27]` (`ActividadEconomicaRegistrada.jurisdiccion` → `JurisdiccionFiscal`), convención de eventos `*Definido` (upsert idempotente), traza del factory `crear()`, ventana de consistencia eventual de proyecciones, versionado independiente de anexos. **Descartados conscientes:** S8 (timeouts/retry — implementación, no dominio), Eventos-10 (renombre `EntregableFiscalCreado` — costo de propagación alto, mejora estética marginal). **Conteos finales v2.0:** 16 elementos del BC, **57 eventos**, 2 FSM, **28 invariantes** — `I1`-`I10` (10) + `I11a`/`I11b` (2) + `I12`-`I27` (16), **15 decisiones de diseño** (`D1`-`D15`), **6 premisas** (`P1`-`P6`), **12 pendientes** (`PD1`-`PD12` — `PD6` cerrado; `PD12` abierto al refinar el diseño de `Tributo` y `ClasificacionTributaria` durante esta auditoría). Reporte completo de la auditoría en `auditoria/impuestos-actual.md`. |
| 2.0.2 | Mayo 2026 | **Segunda auditoría completa aplicada (53 hallazgos tratados, 5 descartados conscientes).** Hallazgos Alta resueltos (7 en 3 clusters): Cluster 1 — diagrama de `RegistroTributario` sincronizado con la unificación de `LineaDeDesglose` con `proposito`; Cluster 2 — eventos `EntradaDeTarifa*`, `Condicion*` y `ActividadEconomicaRegistrada*` alineados con las identidades declaradas en `[I24]` y `[I25]` y con `actividadId`; Cluster 3 — `[I18]` y `[I19]` con notas explícitas sobre garantía bajo concurrencia + nueva **Sección 11 (Sugerencias de implementación)** inaugurada con `[SI01]` (serialización por business key para invariantes financieras). Hallazgos Media resueltos (22 + 8 ya cubiertos en Alta, 2 descartados): VOs compartidos del BC formalizados en nueva **Sección 2.5** (Vigencia, Origen, AutoridadFiscal, ReferenciaFormato, ReferenciaHomologacion, Periodicidad, FormatoDeSalida); `[I17]` reclasificada de eventual a local; `[I14]` con mecanismo de señalización cross-BC; `[I16]` con resolución temporal análoga a `[I13]`/`[I26]`/`[I27]`; `motivoCodigo` de `ConfirmacionTributariaRechazada` reorganizado con 10 códigos planos en snake_case (causa de negocio, no referencia interna); `idProcesoCarga` en `ResultadoCarga` para trazabilidad de la aprobación humana en `CargaAsistida` + contrato de rechazo síncrono cuando la re-validación falla; `[SI02]` para read-your-writes en lookup del registro origen; nuevo comportamiento `localizarRegistroPorTransaccionOrigen(subDominio, transaccionId)` en `RegistroTributario`; refinamiento de la convención `*Modificado` (delta, no snapshot); refinamientos de PD1/PD2/PD3 con owner/criterios; conteo "28 invariantes" en lugar de "27"; tabla L83 con `ConfirmacionTributaria` agregado al Núcleo; nuevo motivo de rechazo `concepto_no_existe_en_origen`. Hallazgos Baja resueltos (13 + 3 ya cubiertos, 3 descartados): `[D7]` reordenado a su posición numérica correcta; `CalculoDeReferencia` documentado como alias del conjunto retornado; postura preliminar de Sección 3.13 marcada como sujeta a revisión vía `[PD7]`; `FormatoFiscal.codigo` declarado inmutable + vínculo a `[PD12]`; eliminado `registrosIncluidos` redundante de `ContenidoGenerado` (la `fechaDeCorte` es suficiente); paso 4 del flujo `ConfirmacionTributaria` aclara verificación temprana + materialización atómica vía `[SI01]`; lista transitiva de agregados involucrados; nueva `fechaDeCorte` en `EntregableFiscalGenerado` (renombre desde `cursorTemporal`); `Tributo.codigo` y `ClasificacionTributaria.codigo` declarados inmutables (referenciados semánticamente desde snapshots históricos); nuevo `[PD12]` (política de corrección de errores en configuración fiscal) con owner equipo fiscal del producto. **Limpieza editorial:** reemplazo global `Enforcement:` → `Verificación:` (24 ocurrencias); jerga técnica eliminada y reemplazada por lenguaje funcional (`tupla` → `combinación de atributos inmutables`, `ID sintético estable` → `identificador único asignado al crear`, `función pura/safe-retry` → `operación determinística sin efectos colaterales`, `cursorTemporal` → `fechaDeCorte`). **Descartados conscientes:** BM3.2 (unicidad de nombre `SeccionFormato` sin caso de uso real), R2 (lógica de pertenencia en motor como estrategia formal — sobreingeniería para F1), BB3.1 (`[I2]` con nota de concurrencia — redundante con `[D11]`), BB3.2 (visibilidad operativa de reportes tardíos — implementación, no dominio), BB2.3 (`condicionId` sintético — contradice `[I24]`). **Conteos finales v2.0.2:** 16 elementos del BC, **57 eventos**, 2 FSM, **28 invariantes** (`I1`-`I27` con `I11` dividida en `I11a`/`I11b`), **15 decisiones de diseño** (`D1`-`D15`), **6 premisas** (`P1`-`P6`), **12 pendientes** (`PD1`-`PD12`, `PD6` cerrado), **2 sugerencias de implementación** (`SI01` serialización por business key, `SI02` read-your-writes en lookup del origen). Reporte completo de la auditoría final en `auditoria/impuestos-final-v2.0.1.md`. |
| 2.0.3 | Mayo 2026 | Refinamiento (3 aplicados de 6 propuestos, 3 descartados conscientes). **D1 — Coherencia de referencias:** referencias globales `R01–R38` reemplazadas por reglas vigentes `[R##]` en 3 ubicaciones (Sección 1 tabla de relación documental; encabezado de Sección 6 invariantes; Sección 7 tabla "Qué NO contiene"). Las reglas reales llegan a R41; la referencia desactualizada quedaba en versiones previas. **D2 — Consistencia de lectura del motor:** nueva sugerencia de implementación `[SI03]` (Vista consistente de la configuración fiscal para el `MotorDeCalculo`) que formaliza la propiedad de dominio "para un mismo contexto y `fechaTransaccion`, el resultado del motor debe ser idéntico" + tres estrategias de materialización no vinculantes (reconstrucción directa desde streams, snapshots certificados, read models con marca de versión coherente) + requisito de documentación en runbook. Reformulación de la propuesta original (que pedía `[D##]` prescriptivo): mantenida como `[SI##]` siguiendo el patrón de `[SI01]` y `[SI02]` validado en la auditoría v2.0.2 — el dominio declara la propiedad, la implementación elige el mecanismo. **D3 — Reformulación de `[PD3]`:** separa los dos momentos que estaban fusionados: (1) definición progresiva de contratos al implementar cada par BC consumidor ↔ Impuestos (el primero es OXP, bloqueante para iniciar el desarrollo productivo F1) y (2) consolidación en EventCatalog (Fase 3, herramienta que cataloga y publica los contratos ya producidos — no donde se inventan). Owner y momento de cierre se reparten por nivel. **Descartados conscientes:** D4 (BC ≠ pieza de implementación por agregado — ya cubierto por `guias-de-modelado/modelar-agregados.md` transversal y por `[D7]` del propio modelo), D5 (guía operativa de Event Sourcing — pertenece a `guias-de-modelado/` transversal, no al modelo de Impuestos), D6 (criterios de aceptación del modelo — meta-documental, función cubierta por las 11 skills de auditoría). **Conteos finales v2.0.3:** 16 elementos del BC, **57 eventos**, 2 FSM, **28 invariantes** (`I1`-`I27`), **15 decisiones de diseño** (`D1`-`D15`), **6 premisas** (`P1`-`P6`), **12 pendientes** (`PD1`-`PD12`, `PD6` cerrado — `PD3` reformulado en v2.0.3), **3 sugerencias de implementación** (`SI01`, `SI02`, `SI03`). |
| 2.0.4 | Mayo 2026 | Refinamiento (1 aplicado de 1 propuesto, ninguno descartado). **D1 — `[D7]` aclarada con habilitación productiva incremental por país:** se inserta un párrafo después de "Fases de implementación" y antes de "Restricción de fase" que distingue cobertura de diseño F1 (CO/DO/PA) de habilitación productiva (depende de la precarga certificada por país y puede ocurrir de forma incremental). Identifica explícitamente que la primera salida productiva corresponde a Colombia con OXP en dirección fiscal de gastos, alineado con `definicion-alcance.md`. No mueve capacidades entre F1 y F2, no renumera decisiones, no toca otros agregados. Propaga al modelo la aclaración ya presente en el alcance v1.3. **Conteos finales v2.0.4:** sin cambios respecto a v2.0.3 — 16 elementos del BC, 57 eventos, 2 FSM, 28 invariantes, 15 decisiones (D1-D15), 6 premisas (P1-P6), 12 pendientes (PD1-PD12, PD6 cerrado), 3 sugerencias de implementación (SI01, SI02, SI03). |