# Modelo de Dominio — Terceros

> ℹ️ **v2.0 — En construcción (junio 2026).** Reescritura por el replanteamiento arquitectónico (issues #31, #33): Terceros pasa de autoridad de registro a **bodega consolidadora**. La v1.0 se conserva como referencia en [`modelo-dominio_bk.md`](modelo-dominio_bk.md) hasta el cierre del issue #33.

## Tabla de contenido

1. [Propósito y relación con otros documentos](#1-propósito-y-relación-con-otros-documentos)
2. [Convenciones del documento](#2-convenciones-del-documento)
3. [Bounded Context y Agregados](#3-bounded-context-y-agregados)
4. [Máquinas de estado](#4-máquinas-de-estado)
5. [Catálogo de eventos](#5-catálogo-de-eventos)
6. [Catálogos del dominio](#6-catálogos-del-dominio)
7. [Invariantes del dominio](#7-invariantes-del-dominio)
8. [Qué NO contiene este documento](#8-qué-no-contiene-este-documento)
9. [Decisiones de arquitectura y diseño](#9-decisiones-de-arquitectura-y-diseño)
10. [Premisas de negocio](#10-premisas-de-negocio)
11. [Pendientes por definir](#11-pendientes-por-definir)
12. [Catálogo de permisos atómicos del dominio](#12-catálogo-de-permisos-atómicos-del-dominio)

---

## 1. Propósito y relación con otros documentos

Este documento describe el comportamiento del sub-dominio de Terceros como **bodega consolidadora**, en términos de DDD / Event Sourcing / EDA: agregados, eventos, máquinas de estado, invariantes, contratos de integración, decisiones de diseño y permisos atómicos. No duplica el alcance funcional — lo complementa.

| Documento | Rol | Descripción |
|-----------|-----|-------------|
| `definicion-alcance.md` (v2.0) | QUÉ hace Terceros | Fuente de verdad para glosario (16 términos), actores, 6 flujos principales, 30 reglas de negocio, alcance dentro/fuera y fases de implementación. No se duplica aquí. |
| **Este documento** | CÓMO se comporta Terceros | Agregados, eventos, transiciones de estado, precondiciones, invariantes, contrato del evento de rol, decisiones de arquitectura y permisos atómicos. |
| `definicion-alcance_bk.md` / `modelo-dominio_bk.md` | Referencia histórica | La v1.0 (autoridad de registro), superada por el replanteamiento. Se conservan hasta el cierre del issue #33. |
| EventCatalog | Catalogación técnica | Fase 3 del proyecto. Consumirá este documento como especificación de entrada. |

Las reglas de negocio se referencian como `[R##]` y su texto completo vive en `definicion-alcance.md`, Sección 6.

---

## 2. Convenciones del documento

### 2.1. Nomenclatura

- **Eventos:** PascalCase en español, sin tildes ni caracteres especiales (compatibilidad con código fuente). Ejemplo: `RolIncorporado`, `TercerosFusionados`.
- **Referencias:** `[R##]` reglas de negocio, `[P##]` premisas, `[D##]` decisiones, `[I##]` invariantes, `[SI##]` sugerencias de implementación, `[PD#]` pendientes.
- **Agregados:** PascalCase; corresponden a los términos del glosario canónico (`definicion-alcance.md`, Sección 2).
- **Alcance del glosario canónico:** Los domain services, entidades internas y value objects son artefactos del modelo de dominio — no requieren entrada en el glosario canónico.
- **El Bounded Context da el contexto:** los nombres no repiten lo que la frontera ya dice. El agregado es `Tercero` (no "TerceroConsolidado": dentro de este BC no existe otro tercero); la señal global es `TerceroInactivado` (no "InactivadoGlobalmente": dentro de este BC no existe otra inactivación de tercero). **"Consolidar" es el verbo del dominio** — aparece como acción (`ServicioDeConsolidacion`), nunca como calificativo de los nombres.

### 2.2. Template de evento

Cada evento se documenta con esta estructura:

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Qué ocurrió en términos de negocio. |
| **Causalidad** | Tipo: directa, derivado por transición, derivado por configuración, efecto inter-agregado, compensatorio. |
| **Agregado** | Agregado que emite el evento. |
| **Estado previo** | Estado requerido del agregado antes del evento. |
| **Estado resultante** | Estado del agregado después del evento (o "sin cambio" si es evento de progreso). |
| **Precondiciones** | Condiciones que deben cumplirse. Referencias a `[R##]`. |
| **Información capturada** | Datos que el evento registra (payload). |
| **Efectos** | Consecuencias: entidades creadas, estado modificado, eventos derivados, avisos publicados a los dominios. |

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

### 2.5. Precisiones terminológicas

| Término | Desambiguación |
|---------|----------------|
| **Tercero** | En este BC, el agregado `Tercero` **es** el "tercero consolidado" del alcance — la entidad única que agrupa los roles informados por los dominios. El calificativo "consolidado" pertenece a la descripción conceptual del alcance; el modelo usa el nombre simple. |
| **Rol del tercero / rol del contacto** | Dos conceptos siempre calificados (alcance, Sección 2): el rol del tercero (proveedor, cliente, empleado — entidad interna `Rol` del agregado) y el rol del contacto (representante legal, tesorero — atributo del contacto). |
| **Conciliación** | Designa el proceso (término 10 del glosario) **y** el agregado `Conciliacion` (una instancia del proceso: un caso concreto con su evidencia, decisión y trazabilidad). El contexto gramatical desambigua: "la conciliación del NIT 900123456" = instancia. |
| **La bodega publica decisiones, no datos** | Los datos de los roles (direcciones, contactos) **entran** en los eventos de rol y se **consultan** en la ficha; nunca se re-publican como avisos. Lo único que la bodega publica son sus decisiones: señal global, fusiones, correcciones (ver `[D4]` en Sección 9). |

---

## 3. Bounded Context y Agregados

### 3.1. Terceros como Bounded Context

El sub-dominio de Terceros es la **bodega consolidadora** de las personas y empresas con las que la organización tiene relación. Su frontera cubre: la consolidación de los roles que los dominios operativos informan (agrupación por clave natural `[R01]`), la detección y conciliación de duplicados y divergencias (`[R09]`-`[R14]`), la señal global del tercero (`[R16]`-`[R20]`) y la vista consolidada de lectura. Todo lo demás — captura, validación en el origen, datos de negocio, autorización para operar — vive en los dominios operativos (`[R26]`-`[R29]`).

**Dos agregados raíz:**

- **`Tercero`** — la entidad consolidada: identidad compartida, sus roles (entidad interna `Rol`, uno por dominio y empresa) y la señal global. No se crea por comando de un usuario: nace cuando el `ServicioDeConsolidacion` procesa el primer evento de rol con una clave natural nueva.
- **`Conciliacion`** — una instancia del proceso de conciliación: un caso de duplicado o divergencia, con su evidencia, decisión humana y trazabilidad (`[R10]`).

**Un domain service:** `ServicioDeConsolidacion` — procesa cada evento de rol recibido: resuelve la clave natural, crea o actualiza el `Tercero`, y evalúa las señales de duplicado y divergencia (consultando la memoria de conciliación) para abrir una `Conciliacion` cuando corresponde.

**Diagrama:**

```
   ┌────────────────────────────────────────────────────────────┐
   │                 Entradas al sub-dominio                     │
   │                                                             │
   │  · Eventos de rol de los dominios operativos                │
   │    (OXP: Proveedor · CXC: Cliente · RRHH: Empleado)         │
   │  · Eventos de perfil tributario (Impuestos)                 │
   │  · Comandos del administrador de terceros                   │
   │    (resolver conciliación, señal global)                    │
   └───────────────────────────┬─────────────────────────────────┘
                               │ eventos / comandos
                               ▼
 ┌────────────────────────────────────────────────────────────────┐
 │                  Bounded Context: Terceros                      │
 │                                                                 │
 │  ┌──────────────────────────┐      ┌─────────────────────────┐ │
 │  │   Agregado: Tercero      │      │  Agregado: Conciliacion │ │
 │  │                          │      │                         │ │
 │  │  IdentificacionLegal(VO) │      │  Tipo: duplicado |      │ │
 │  │  RazonSocial             │◄─┐   │        divergencia      │ │
 │  │  TipoPersona             │  │   │  Candidatos/versiones   │ │
 │  │  Estado: Activo⇄Inactivo │  │   │  + evidencia            │ │
 │  │    (■Fusionado)          │  │   │  Decisión + motivo      │ │
 │  │  ┌─────────────────────┐ │  │   │  Estado: Abierta →      │ │
 │  │  │ Entidad: Rol (1..N) │ │  │   │   EnCorreccion→Cerrada■ │ │
 │  │  │ · dominio · empresa │ │  │   └────────────▲────────────┘ │
 │  │  │ · estado en origen  │ │  │                │              │
 │  │  │ · direcciones (VO)  │ │  └────┐           │ abre         │
 │  │  │ · contactos         │ │       │           │              │
 │  │  └─────────────────────┘ │   ┌───┴───────────┴───────────┐  │
 │  └──────────────────────────┘   │  ServicioDeConsolidacion  │  │
 │                                 │  · resuelve clave natural │  │
 │                                 │  · crea/actualiza Tercero │  │
 │                                 │  · evalúa señales (R09,   │  │
 │                                 │    R14) vs memoria        │  │
 │                                 └───────────────────────────┘  │
 └────────────────────────────┬───────────────────────────────────┘
                              │ publica DECISIONES (nunca datos):
                              │ señal global · fusiones · correcciones
        ┌─────────────┬───────┴─────┬──────────────┐
        ▼             ▼             ▼              ▼
   ┌─────────┐   ┌─────────┐   ┌─────────┐   ┌──────────────┐
   │   OXP   │   │   CXC   │   │  RRHH   │   │ Contabilidad │
   │(aplica  │   │(aplica  │   │(aplica  │   │(mapa canónico│
   │ en su   │   │ en su   │   │ en su   │   │ en reportes  │
   │ registro)│  │ registro)│  │ registro)│  │ por tercero) │
   └─────────┘   └─────────┘   └─────────┘   └──────────────┘

 Lecturas (no bloqueantes):
   ◄── asistencia de captura (formularios de los dominios)
   ◄── ficha consolidada (usuarios, Emisión Electrónica)
```

**Frontera con otros dominios:**

| Dominio | Relación | Datos que intercambia |
|---------|----------|-----------------------|
| OXP (hoy) · CXC, RRHH (futuros) | Fuente + consumidor | Entrada: eventos de rol con la información estándar (Sección 5). Salida: señal global, correcciones de conciliación — aplicadas automáticamente en su registro del tercero `[R27]`. |
| Impuestos | Fuente | Eventos del perfil tributario por identificación legal — enriquecen la vista consolidada. |
| Contabilidad | Consumidor | Señal global (copia local para sus reglas de datos maestros) + fusiones (mapa canónico en sus vistas y reportes por tercero). |
| Emisión Electrónica | Lector | Contactos consolidados (ej: representante legal) vía consulta de la ficha. |
| Validaciones empaquetadas (custodio: Datos de Referencia) | Dependencia de paquete, no de ejecución | La bodega verifica al consolidar con las mismas reglas con que los dominios capturan. |

**Lo que este BC ya no tiene (frente a la v1.0):** comandos de registro de terceros (`RegistrarTercero`, `AsegurarTerceroDesdeConsumidor`, `RegistrarTerceroForzado` desaparecen — la captura vive en los dominios), estados de registro (`EnRegistro`, `Abortado`), referencias a direcciones por identificador (las direcciones llegan embebidas en los eventos de rol) y dependencias de ejecución con Datos de Referencia y Direcciones.

### 3.2. Agregado: Tercero

**Descripción:** La entidad consolidada del tercero. Agrupa bajo una clave natural los roles que los dominios operativos informan, mantiene la identidad compartida (identificación legal, razón social, tipo de persona) y administra la señal global. **No se crea ni se edita por comando de usuario:** nace cuando el `ServicioDeConsolidacion` procesa el primer evento de rol con una clave natural nueva (`[R16]` — nace Activo), y sus datos solo cambian por consolidación de eventos de rol o por resolución de conciliación. Los únicos comandos que acepta son los del administrador sobre la señal global (`InactivarTercero`, `ReactivarTercero`, con motivo obligatorio `[R17]`).

**Identidad del agregado:** el `Tercero` tiene **identificador propio** (`terceroId`), con **índice único por clave natural** (tipo de documento + número + país). La clave natural **no puede ser** la identidad del agregado porque es corregible: el caso CC→NIT (una conciliación corrige el tipo de documento) cambiaría la clave, y una fusión hace que dos claves apunten al mismo tercero. Con identificador propio, el historial sobrevive a ambas. *(Ver `[D2]` en Sección 9 y el índice `[SI1]`.)*

**Composición:**

**Raíz — datos de identidad compartidos:**

| Atributo | Tipo | Notas |
|----------|------|-------|
| `terceroId` | Identificador | Identidad del agregado, estable ante correcciones y fusiones. |
| `identificacionLegal` | VO `IdentificacionLegal` *(paquete transversal)* | Tipo + número + país + DV. Define la clave natural (sin DV, `[R05]`). |
| `razonSocial` | Texto | Dato compartido — sujeto a divergencia (`[R14]`). |
| `tipoPersona` | `persona` \| `organizacion` | Dato compartido (`[R07]`). |
| `estado` | `Activo` \| `Inactivo` | Señal global. Nace Activo (`[R16]`). |
| `motivoEstado` | Texto | Obligatorio al inactivar/reactivar (`[R17]`). |

**Entidad interna — `Rol` (colección 1..N):**

| Atributo | Tipo | Notas |
|----------|------|-------|
| Identidad de la entidad | (`rol`, `dominio`, `empresa`) | Un tercero no puede tener dos veces el mismo rol del mismo dominio en la misma empresa. |
| `rol` | Del catálogo de roles (Sección 6) | proveedor, cliente, empleado, entidad financiera, otro. |
| `dominio` | Identificador del dominio fuente | OXP, CXC, RRHH… |
| `empresa` | Referencia | La empresa donde el rol opera. |
| `referenciaOrigen` | Identificador externo | El identificador del registro en el dominio dueño (ej: el `proveedorId` de OXP) — la correlación entre la bodega y el origen: permite que las resoluciones de conciliación lleguen al registro exacto y que la ficha enlace "navegar al dominio". |
| `estadoEnOrigen` | `activo` \| `inactivo` | Lo que el dominio informó (`[R20]`); la bodega no lo decide. |
| `direcciones` | Colección VO `DireccionFisica` + tipo de uso | Como llegaron en el evento de rol. |
| `contactos` | Colección `Contacto` | Estructura del paquete (issue #35): nombre, rol del contacto, correo, teléfono, marca de principal (`[R25]` — principal por rol). |

**Lo que NO es atributo (y dónde vive):**

| Concepto | Dónde vive |
|----------|-----------|
| Historial de identidad (`[R06]`) | Derivado del stream de eventos — no se persiste como atributo *(mismo criterio `[D3]` de la v1.0)*. |
| Marca "en conciliación" / marca de divergencia | Proyección que cruza el tercero con sus `Conciliacion` abiertas — el agregado no la duplica. |
| Mapa canónico | Proyección acumulada de los eventos `TercerosFusionados`. |
| Completitud / "listo para operar" | No existe en la bodega (`[R29]`). |

**Eventos que emite:** `TerceroCreado`, `RolIncorporado`, `RolActualizado`, `RolInactivado`, `IdentidadActualizada` (consolidación); `TerceroInactivado`, `TerceroReactivado` (señal global); `TerceroAbsorbido` (fusión: el absorbido pasa al estado terminal `Fusionado` y sus roles se incorporan al canónico — ver `[D7]` y Sección 5).

**Qué protege (anticipo de invariantes, Sección 7):** unicidad por clave natural (`[R02]`, eventual vía índice); nace Activo con al menos un rol; un solo rol por (`rol`, `dominio`, `empresa`); cambio de señal global solo por administrador con motivo; ningún dato de identidad se edita directamente en la bodega — solo consolidación o resolución (`[R13]`).

### 3.3. Agregado: Conciliacion

**Descripción:** Una instancia del proceso de conciliación — un caso concreto de duplicado o divergencia, detectado por el `ServicioDeConsolidacion`, con su evidencia, la decisión humana y la trazabilidad (`[R10]`, `[R11]`). **Nace con la detección** (el evento de detección es la apertura — no hay un "abrir caso" separado), vive `Abierta` mientras se decide, pasa a `EnCorreccion` cuando una divergencia fue resuelta y espera que los dominios converjan (`[R13]`, Flujo 4 paso 5), y termina `Cerrada` con su motivo de cierre.

**Identidad:** `conciliacionId` propio.

**Composición:**

**Datos comunes:**

| Atributo | Tipo | Notas |
|----------|------|-------|
| `conciliacionId` | Identificador | Identidad del agregado. |
| `tipo` | `duplicado` \| `divergencia` | Fijo desde la detección. |
| `estado` | `Abierta` \| `EnCorreccion` \| `Cerrada` | FSM en Sección 4. `EnCorreccion` solo aplica a divergencias. |
| `motivoCierre` | `fusion` \| `homonimia` \| `convergencia` \| `superada` | Con qué cerró: decisión de fusión, homonimia legítima, convergencia tras resolución, o superada sin decisión humana. |
| `decision` | Estructura de resolución | Tipo de decisión, decidida por quién, cuándo, **motivo obligatorio**. |
| `notas` | Texto (0..N) | Anotaciones del administrador durante la revisión. |

**Si es duplicado:**

| Atributo | Notas |
|----------|-------|
| `candidatos` (VO `Candidato`, colección 2..N) | Referencia a cada `terceroId` + **instantánea de la evidencia al detectar**: clave natural, razón social, roles y dominios de cada candidato en ese momento. |
| `criterioDeteccion` | Qué señal lo abrió (`[R09]`: número igual + razón social canónica equivalente; en F2, criterios ampliados). Es la llave de la **memoria de conciliación**: una homonimia marcada con este criterio no se reabre (`[R11]`). |
| Decisión posible | **Fusionar** (designa el canónico — debe ser uno de los candidatos) o **marcar homonimia legítima**. |

**Si es divergencia:**

| Atributo | Notas |
|----------|-------|
| `terceroId` | El tercero (uno solo) cuyas fuentes discrepan. |
| `datoEnDisputa` | Cuál dato de identidad compartido: razón social, tipo de persona o componente de la identificación (`[R14]`). |
| `versiones` (VO `VersionDeDato`, colección 2..N) | Cada valor informado + el dominio que lo informó + cuándo. |
| Decisión posible | **Determinar el dato correcto** — el valor elegido puede ser una de las versiones **o un valor distinto respaldado por evidencia** (ej: el certificado RUES trae la razón social vigente y ningún dominio la tenía bien, ver `[D12]`). La resolución publica la corrección a todos los dominios cuyo valor difiera del correcto. |

**Comandos (solo administrador de terceros, `[R10]`):** `FusionarTerceros`, `MarcarHomonimia`, `ResolverDivergencia`, `AgregarNota`. No existe comando de apertura (la abre el servicio) ni de cierre por convergencia (lo detecta el servicio al consolidar las correcciones).

**Eventos que emite:** `PosibleDuplicadoDetectado` y `DivergenciaDetectada` (apertura); `TercerosFusionados`, `HomonimiaMarcada`, `DivergenciaResuelta` (decisión); `ConvergenciaConfirmada` (cierre de divergencia resuelta cuando los dominios convergieron); `DivergenciaSuperada` (cierre sin decisión humana: convergieron solos); `NotaAgregada` (progreso).

**Qué protege (anticipo de invariantes):** un duplicado exige ≥2 candidatos y una divergencia exige ≥2 versiones del mismo tercero; el canónico de una fusión debe ser uno de los candidatos; toda resolución lleva decisor, fecha y motivo; una homonimia marcada no se reabre por el mismo criterio sobre los mismos terceros; `EnCorreccion` y los cierres por convergencia solo aplican a divergencias.

**Resolución por lotes (carga histórica):** el lote **no es un concepto del dominio** — es la aplicación repetida del mismo comando individual con una marca de lote compartida para trazabilidad. Cada conciliación conserva su decisión propia (`[SI8]` — la herramienta es de la aplicación, las reglas son las de siempre).

### 3.4. Value Objects

**Del paquete transversal** (validación local, sin consulta — sus especificaciones viven en `compartido/nuggets/`):

| VO | Uso en este BC | Notas |
|----|----------------|-------|
| `IdentificacionLegal` | Raíz del `Tercero` | Tipo + número + país + DV. La clave natural se deriva de él **sin el DV** (`[R05]`). La bodega verifica con sus mismas reglas al consolidar (`[R04]`). |
| `DireccionFisica` | Colección en la entidad `Rol` | El tipo de uso (fiscal, comercial, entrega…) es atributo de la relación en el `Rol`, no del VO — criterio transversal del catálogo de Nuggets. |
| `Telefono` | Dentro de `Contacto` | Formato internacional validado. |
| `CorreoElectronico` | Dentro de `Contacto` | Formato validado, normalizado a minúsculas. |
| `Contacto` | Colección en la entidad `Rol` | Estructura propuesta como pieza del paquete (issue #35): nombre opcional, rol del contacto (vocabulario compartido), correos y teléfonos (`[R23]`: al menos un medio). La marca de principal es atributo de la relación en el `Rol` (`[R25]`). |

**Propios del BC:**

| VO | Dónde | Composición |
|----|-------|-------------|
| `Candidato` | `Conciliacion` (duplicados) | `terceroId` + instantánea al detectar: clave natural, razón social, lista de (rol, dominio, empresa). |
| `VersionDeDato` | `Conciliacion` (divergencias) | Valor informado + dominio que lo informó + fecha del evento que lo trajo. |

### 3.5. Sugerencias de implementación

#### `[SI1]` Índice único por clave natural
Materializa `[I1]`/`[R02]`: índice único sobre (tipo de documento, número, país) de los terceros **no fusionados**. Las claves de terceros absorbidos se asocian al canónico (ver `[SI4]`). Análogo al `[SI1]` de la v1.0.

#### `[SI2]` Forma canónica de la razón social
Normalización para la comparación de `[R09]`: mayúsculas, tildes, puntuación y espacios. Se calcula al consolidar y se usa solo para detección — nunca se muestra ni reemplaza el valor informado. Análoga al `[SI9]` de la v1.0.

#### `[SI3]` Idempotencia y orden de los eventos de rol
El contrato de entrada trae (`referenciaOrigen`, `secuencia`) — ver Sección 5.1. La bodega aplica cada evento una sola vez y descarta secuencias anteriores a la última aplicada por esa referencia. El mecanismo técnico (deduplicación, reintentos) es de plataforma (`[D11]`).

#### `[SI4]` Proyección del mapa canónico
Acumulado de los eventos `TercerosFusionados`: correspondencia identificación → tercero canónico. Consultable por los interesados en reportes por tercero (Contabilidad) y usada por el `ServicioDeConsolidacion` para enrutar eventos de rol que lleguen con claves de terceros absorbidos.

#### `[SI5]` Proyección de la memoria de conciliación
Pares de terceros + criterio con homonimia marcada (`HomonimiaMarcada`). El `ServicioDeConsolidacion` la consulta antes de abrir un duplicado (`[I9]`).

#### `[SI6]` Proyección de la ficha consolidada
La vista de lectura del Flujo 6: identidad, estado global, roles con sus datos, contactos, perfil tributario (de los eventos de Impuestos — ver `[D9]`), marca "en conciliación" (cruce con `Conciliacion` abiertas o en corrección).

#### `[SI7]` Consulta de asistencia de captura
Búsqueda por número (exacta y similar) sobre la proyección consolidada, diseñada para responder dentro del tiempo de espera corto del Flujo 2. El presupuesto de espera y la degradación los gobierna el formulario del dominio, no la bodega.

#### `[SI8]` Resolución por lotes
Los comandos de resolución aceptan una marca de lote (`loteId`) compartida: la herramienta de la aplicación agrupa conciliaciones del mismo patrón (post-carga histórica) y aplica el mismo comando una a una. Cada conciliación conserva su decisión y trazabilidad propia.

#### `[SI9]` Colisión de claves por corrección
Si una corrección de clave natural (aplicada por un dominio y consolidada de regreso) hace que la clave coincida con la de otro tercero vigente: el `ServicioDeConsolidacion` lo trata como **señal de duplicado con evidencia máxima** (clave idéntica). Si la corrección provino de una resolución humana previa que ya designó canónico, la fusión es consecuencia de esa misma decisión y no exige una segunda.

### 3.6. Domain service: ServicioDeConsolidacion

**Responsabilidad:** procesar cada evento de rol recibido de los dominios (Flujo 1) y producir la consolidación y las señales. Es la única vía de creación y actualización de datos del agregado `Tercero` (junto con las resoluciones de `Conciliacion`).

**Pasos:**

| # | Paso | Agregado / proyección | Eventos |
|---|------|----------------------|---------|
| 1 | **Verificar** el evento con las reglas empaquetadas. Una anomalía no rechaza: se registra como evidencia para conciliación (`[R04]`). | — | — |
| 2 | **Resolver la clave natural** y buscar el tercero por el índice (`[SI1]`), pasando por el mapa canónico (`[SI4]`) si la clave pertenece a un absorbido. | `Tercero` | — |
| 3 | **Consolidar:** si no existe → crear (`TerceroCreado` + `RolIncorporado`, mismo append). Si existe → incorporar o actualizar el rol (`RolIncorporado` / `RolActualizado` / `RolInactivado`); si el evento trae identidad compartida distinta y es la única fuente, actualizarla (`IdentidadActualizada`). | `Tercero` | Consolidación |
| 4 | **Evaluar señales:** duplicado (`[R09]`, consultando la memoria `[SI5]`) y divergencia (`[R14]`, comparando los datos compartidos entre fuentes). Si hay señal → abrir `Conciliacion` (`PosibleDuplicadoDetectado` / `DivergenciaDetectada`). Caso especial de colisión de claves: `[SI9]`. | `Conciliacion` | Apertura |
| 5 | **Detectar convergencia:** si el tercero tiene divergencias `Abiertas` o `EnCorreccion` y los datos de las fuentes ya coinciden → cerrarlas (`DivergenciaSuperada` / `ConvergenciaConfirmada`). | `Conciliacion` | Cierre |

**Eventos de perfil tributario (Impuestos):** no tocan los agregados — alimentan directamente la proyección de la ficha (`[SI6]`, ver `[D9]`).

**Compensaciones:** no tiene. Todos los pasos son acumulativos e idempotentes (`[SI3]`); un fallo se resuelve con reintento de plataforma (`[D11]`), nunca con reversa — la consolidación no produce efectos en otros dominios.

### 3.7. Relaciones y referencias externas

| Referencia | Apunta a | Naturaleza |
|------------|----------|------------|
| `empresa` (en `Rol`) | Estructura Organizacional | Referencia por identificador; sin validación en caliente (`[P4]`). |
| `dominio` (en `Rol`) | El sub-dominio fuente | Identificador del dominio (OXP, CXC, RRHH). |
| `referenciaOrigen` (en `Rol`) | El registro del tercero en el dominio dueño | Correlación bodega ↔ origen; destino de las resoluciones. |
| Perfil tributario | Impuestos | **No es atributo del agregado** — proyección de la ficha (`[D9]`). |
| Validaciones empaquetadas | Paquete del producto | Dependencia de paquete, no de ejecución. |

---

## 4. Máquinas de estado

### 4.1. Tercero

Nace `Activo` (`[R16]`). La señal global alterna entre `Activo` e `Inactivo` (`[R17]`-`[R20]`); la absorción por fusión es terminal. **La consolidación nunca se detiene por el estado:** los eventos de rol se siguen aplicando en `Activo` y en `Inactivo` (la señal restringe operaciones nuevas en los dominios, no la información).

```
                    TerceroCreado
                         │
                         ▼
                  ┌─────────────┐   TerceroInactivado    ┌─────────────┐
                  │   ACTIVO    │ ─────────────────────► │  INACTIVO   │
                  │             │ ◄───────────────────── │             │
                  │ · RolIncorporado    TerceroReactivado│ · RolIncorporado
                  │ · RolActualizado │                   │ · RolActualizado
                  │ · RolInactivado  │                   │ · RolInactivado
                  │ · IdentidadActualizada               │ · IdentidadActualizada
                  └──────┬──────┘                        └──────┬──────┘
                         │        TerceroAbsorbido              │
                         └──────────────┬───────────────────────┘
                                        ▼
                                 ┌─────────────┐
                                 │ FUSIONADO ■ │  (terminal: sus claves
                                 └─────────────┘   enrutan al canónico)
```

### 4.2. Conciliacion

Nace `Abierta` con la detección. Los duplicados cierran con la decisión; las divergencias resueltas pasan a `EnCorreccion` hasta que los dominios convergen (Flujo 4, paso 5), o cierran directo si convergieron sin decisión humana.

```
   PosibleDuplicadoDetectado / DivergenciaDetectada
                         │
                         ▼
                  ┌─────────────┐  TercerosFusionados (duplicado)
                  │   ABIERTA   │  HomonimiaMarcada (duplicado)     ┌────────────┐
                  │             │ ────────────────────────────────► │ CERRADA ■  │
                  │ · NotaAgregada  DivergenciaSuperada             │ motivoCierre:│
                  └──────┬──────┘  (divergencia, sin decisión)      │ fusion │    │
                         │                                     ┌──► │ homonimia │ │
                         │ DivergenciaResuelta                 │    │ convergencia│
                         ▼ (divergencia)                       │    │ superada    │
                  ┌──────────────┐  ConvergenciaConfirmada     │    └────────────┘
                  │ ENCORRECCION │ ─────────────────────────────┘
                  │ · NotaAgregada│  (los dominios convergieron)
                  └──────────────┘
```

---

## 5. Catálogo de eventos

**16 eventos de dominio** (8 del `Tercero`, 8 de `Conciliacion`) + **1 evento de integración derivado** (`DatoDeIdentidadCorregido`) + el **contrato de entrada** (el evento de rol que publican los dominios).

### 5.1. Contrato de entrada — el evento de rol

Lo publican los dominios fuente (OXP: su Proveedor; CXC: su Cliente; RRHH: su Empleado) en cada creación, actualización o inactivación de su registro del tercero. Es la "información estándar del rol" del alcance (Secciones 3 y 5).

| Bloque | Campos |
|--------|--------|
| Identificación | `identificacionLegal` (tipo, número, país, DV), `razonSocial`, `tipoPersona` |
| El rol | `rol` (del catálogo, Sección 6), `dominio`, `empresa`, `referenciaOrigen`, `estadoEnOrigen`, `secuencia` |
| Direcciones | Colección de (`DireccionFisica`, tipo de uso) |
| Contactos | Colección de `Contacto` (nombre opcional, rol del contacto, correos, teléfonos) + marca de principal |
| Contexto | Fecha del hecho en el origen |

**Semántica del contrato (`[D5]`):**

- Cada evento lleva el **estado completo del rol** al momento (no un delta): la consolidación tolera mensajes perdidos o desordenados — el más reciente por `secuencia` siempre deja el rol correcto.
- `secuencia` es creciente por `referenciaOrigen`; la bodega aplica una sola vez y descarta secuencias viejas (`[SI3]`).
- La entrega es "al menos una vez" (`[P2]`); la idempotencia la garantiza la bodega, no el publicador.

### 5.2. Eventos del agregado Tercero

#### `TerceroCreado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La bodega conoció un tercero nuevo: llegó el primer evento de rol con una clave natural sin tercero vigente. |
| **Causalidad** | Directa (emitido por el `ServicioDeConsolidacion` al procesar el evento de rol). |
| **Agregado** | `Tercero`. |
| **Estado previo** | No existe. |
| **Estado resultante** | `Activo` (`[R16]`). |
| **Precondiciones** | Clave natural sin tercero vigente (`[R01]`, `[R02]`, `[SI1]`). |
| **Información capturada** | `terceroId`, `identificacionLegal`, `razonSocial`, `tipoPersona`, dominio y referencia del primer rol. |
| **Efectos** | `RolIncorporado` en el mismo append (derivado por transición) — el tercero nace con su primer rol (`[I2]`). |

#### `RolIncorporado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Un rol informado por un dominio quedó incorporado al tercero. |
| **Causalidad** | Derivado por transición (con `TerceroCreado`), directa (servicio, sobre tercero existente) o efecto inter-agregado (fusión: los roles del absorbido se incorporan al canónico). |
| **Agregado** | `Tercero`. |
| **Estado previo** | `Activo` o `Inactivo` (progreso — la consolidación no se detiene por la señal). |
| **Estado resultante** | Sin cambio. |
| **Precondiciones** | No existe rol con la misma combinación (`rol`, `dominio`, `empresa`) (`[I3]`). |
| **Información capturada** | El estado completo del rol según el contrato de entrada: rol, dominio, empresa, `referenciaOrigen`, `estadoEnOrigen`, direcciones, contactos con su principal, `secuencia`. |
| **Efectos** | El servicio evalúa señales de duplicado y divergencia (paso 4 del `ServicioDeConsolidacion`). |

#### `RolActualizado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El dominio informó cambios en un rol ya incorporado (datos, direcciones, contactos o su estado completo más reciente). |
| **Causalidad** | Directa (servicio). |
| **Agregado** | `Tercero`. |
| **Estado previo / resultante** | `Activo` o `Inactivo` / sin cambio (progreso). |
| **Precondiciones** | El rol existe; `secuencia` mayor a la última aplicada (`[SI3]`). |
| **Información capturada** | Estado completo del rol (contrato `[D5]`). |
| **Efectos** | Posible `DivergenciaDetectada` (si trae un dato compartido distinto) o cierre por convergencia (`DivergenciaSuperada` / `ConvergenciaConfirmada`). |

#### `RolInactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El dominio informó que su rol dejó de operar (ej: el proveedor se inactivó en OXP). La bodega lo refleja — no lo decide (`[R20]`). |
| **Causalidad** | Directa (servicio). |
| **Agregado** | `Tercero`. |
| **Estado previo / resultante** | `Activo` o `Inactivo` / sin cambio (progreso). |
| **Precondiciones** | El rol existe. |
| **Información capturada** | (`rol`, `dominio`, `empresa`), `referenciaOrigen`, fecha del hecho. |
| **Efectos** | La ficha muestra el rol inactivo en su origen. |

#### `IdentidadActualizada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Cambió un dato de identidad compartido del tercero consolidado: razón social, tipo de persona o un componente de la identificación. |
| **Causalidad** | Directa (servicio: el evento de rol más reciente trae identidad distinta sin generar divergencia — ej: fuente única) o efecto inter-agregado (la resolución de una divergencia fija el valor correcto, `DivergenciaResuelta`). |
| **Agregado** | `Tercero`. |
| **Estado previo / resultante** | `Activo` o `Inactivo` / sin cambio (progreso). |
| **Precondiciones** | Si cambia la clave natural: verificación de colisión (`[SI9]`). |
| **Información capturada** | Dato cambiado, valor anterior, valor nuevo, origen del cambio (consolidación o `conciliacionId`). |
| **Efectos** | El historial de identidad queda en el stream (`[R06]`); si cambió la clave natural, el índice `[SI1]` se actualiza. |

#### `TerceroInactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El administrador decretó el cese global de la relación con el tercero (`[R17]`). |
| **Causalidad** | Directa (comando `InactivarTercero`). |
| **Agregado** | `Tercero`. |
| **Estado previo** | `Activo`. |
| **Estado resultante** | `Inactivo`. |
| **Precondiciones** | Motivo obligatorio; solo administrador de terceros (`[I4]`). |
| **Información capturada** | Motivo, decisor, fecha. |
| **Efectos** | **Se publica como aviso de integración** — cada dominio lo aplica localmente (`[R18]`). El historial de los dominios queda intacto (`[R19]`). |

#### `TerceroReactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El administrador reactivó la relación global (`[R20]`). |
| **Causalidad** | Directa (comando `ReactivarTercero`). |
| **Agregado** | `Tercero`. |
| **Estado previo / resultante** | `Inactivo` / `Activo`. |
| **Precondiciones** | Motivo obligatorio; solo administrador. |
| **Información capturada** | Motivo, decisor, fecha. |
| **Efectos** | Se publica como aviso de integración; los dominios vuelven a permitir operaciones según su regla. |

#### `TerceroAbsorbido`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El tercero fue absorbido por el canónico en una fusión — deja de existir como entidad independiente. |
| **Causalidad** | Efecto inter-agregado (de `TercerosFusionados`). |
| **Agregado** | `Tercero` (el absorbido). |
| **Estado previo** | `Activo` o `Inactivo`. |
| **Estado resultante** | `Fusionado` ■ (terminal). |
| **Precondiciones** | Fusión decidida en una `Conciliacion` (`[I7]`). |
| **Información capturada** | `terceroCanonicoId`, `conciliacionId`. |
| **Efectos** | Sus roles se incorporan al canónico (`RolIncorporado`, efecto inter-agregado); su clave natural queda asociada al canónico en el mapa (`[SI4]`); eventos de rol futuros con esa clave se enrutan al canónico. |

### 5.3. Eventos del agregado Conciliacion

#### `PosibleDuplicadoDetectado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La bodega detectó que dos terceros con claves naturales distintas parecen ser la misma entidad (`[R09]`). Este evento **es** la apertura del caso. |
| **Causalidad** | Directa (servicio, paso 4). |
| **Agregado** | `Conciliacion`. |
| **Estado previo / resultante** | No existe / `Abierta`. |
| **Precondiciones** | Criterio de `[R09]` cumplido; sin homonimia previa por el mismo criterio sobre los mismos terceros (`[I9]`, `[SI5]`). |
| **Información capturada** | `candidatos` (instantáneas), `criterioDeteccion`. |
| **Efectos** | Los candidatos se marcan "en conciliación" en la ficha (`[SI6]`). Ningún dominio se entera; nada se bloquea. |

#### `DivergenciaDetectada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Dos fuentes del mismo tercero informan distinto un dato de identidad compartido (`[R14]`). Este evento **es** la apertura del caso. |
| **Causalidad** | Directa (servicio, paso 4). |
| **Agregado** | `Conciliacion`. |
| **Estado previo / resultante** | No existe / `Abierta`. |
| **Precondiciones** | Dato compartido con ≥2 valores vigentes distintos entre fuentes (`[I6]`). |
| **Información capturada** | `terceroId`, `datoEnDisputa`, `versiones` (valor + dominio + fecha). |
| **Efectos** | La ficha muestra el valor más reciente con marca de divergencia (Flujo 4, paso 2). |

#### `NotaAgregada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El administrador anotó la revisión del caso. |
| **Causalidad** | Directa (comando `AgregarNota`). |
| **Agregado** | `Conciliacion`. |
| **Estado previo / resultante** | `Abierta` o `EnCorreccion` / sin cambio (progreso). |
| **Información capturada** | Texto, autor, fecha. |
| **Efectos** | Trazabilidad de la revisión. |

#### `TercerosFusionados`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El administrador decidió que los candidatos son la misma entidad y designó el tercero canónico. |
| **Causalidad** | Directa (comando `FusionarTerceros`). |
| **Agregado** | `Conciliacion`. |
| **Estado previo** | `Abierta` (tipo `duplicado`). |
| **Estado resultante** | `Cerrada` ■ (`motivoCierre = fusion`). |
| **Precondiciones** | El canónico es uno de los candidatos (`[I7]`); decisor + motivo (`[I8]`). |
| **Información capturada** | `terceroCanonicoId`, terceros absorbidos, correspondencia de claves, corrección del dato si el duplicado nació de un error de captura, decisor, motivo. |
| **Efectos** | `TerceroAbsorbido` en cada absorbido y `RolIncorporado` en el canónico (efectos inter-agregado); mapa canónico actualizado (`[SI4]`); **se publica como aviso de integración** (Contabilidad lo aplica en sus reportes por tercero); si hay corrección de dato, se publica `DatoDeIdentidadCorregido`. |

#### `HomonimiaMarcada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El administrador determinó que los candidatos son entidades distintas (homonimia legítima). |
| **Causalidad** | Directa (comando `MarcarHomonimia`). |
| **Agregado** | `Conciliacion`. |
| **Estado previo / resultante** | `Abierta` (tipo `duplicado`) / `Cerrada` ■ (`motivoCierre = homonimia`). |
| **Precondiciones** | Decisor + motivo (`[I8]`). |
| **Información capturada** | Pares de terceros, criterio, decisor, motivo. |
| **Efectos** | Memoria de conciliación (`[SI5]`): la señal no se reabre por el mismo criterio (`[I9]`, `[R11]`). |

#### `DivergenciaResuelta`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El administrador determinó el dato correcto de una divergencia (`[R13]`). |
| **Causalidad** | Directa (comando `ResolverDivergencia`). |
| **Agregado** | `Conciliacion`. |
| **Estado previo** | `Abierta` (tipo `divergencia`). |
| **Estado resultante** | `EnCorreccion` — la decisión está tomada; falta que los dominios converjan. |
| **Precondiciones** | Valor correcto determinado — una de las versiones u otro valor con evidencia (`[D12]`); decisor + motivo (`[I8]`). |
| **Información capturada** | `datoEnDisputa`, valor correcto, evidencia, dominios a corregir, decisor, motivo. |
| **Efectos** | **Se publica `DatoDeIdentidadCorregido`** (integración) a los dominios cuyo valor difiere — lo aplican automáticamente (`[R27]`); `IdentidadActualizada` en el `Tercero` (efecto inter-agregado: la vista consolidada refleja el valor decidido sin esperar el regreso). |

#### `ConvergenciaConfirmada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Tras la resolución, todos los dominios corrigieron: las fuentes convergen en el valor correcto (Flujo 4, paso 5). |
| **Causalidad** | Derivada (el servicio la detecta al consolidar las correcciones que regresan, paso 5). |
| **Agregado** | `Conciliacion`. |
| **Estado previo / resultante** | `EnCorreccion` / `Cerrada` ■ (`motivoCierre = convergencia`). |
| **Información capturada** | Confirmación por dominio (qué evento de rol trajo cada corrección). |
| **Efectos** | Caso cerrado con trazabilidad completa. |

#### `DivergenciaSuperada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Los dominios convergieron **antes** de cualquier decisión humana (ej: alguien corrigió el dato en el origen por su cuenta). |
| **Causalidad** | Derivada (servicio, paso 5). |
| **Agregado** | `Conciliacion`. |
| **Estado previo / resultante** | `Abierta` (tipo `divergencia`) / `Cerrada` ■ (`motivoCierre = superada`). |
| **Información capturada** | Valor final y los eventos de rol que produjeron la convergencia. |
| **Efectos** | Caso cerrado sin intervención (`[I10]`). |

### 5.4. Integración de salida

La bodega publica **sus decisiones, nunca los datos de los roles** (`[D4]`):

| Aviso | Origen | Qué lleva | Quién lo aplica |
|-------|--------|-----------|-----------------|
| `TerceroInactivado` / `TerceroReactivado` | El mismo evento de dominio viaja | Clave natural, motivo | Todos los dominios con roles del tercero — bloquean/permiten nuevas operaciones según su regla (`[R18]`). |
| `TercerosFusionados` | El mismo evento de dominio viaja | Correspondencia identificación → tercero canónico | Contabilidad y demás interesados en reportes por tercero (`[R12]`). |
| `DatoDeIdentidadCorregido` | Derivado de `DivergenciaResuelta` (y de fusiones con corrección de dato) | Clave natural, dato en disputa, valor correcto | Los dominios cuyo valor difiere — corrigen su registro automáticamente (`[R13]`, `[R27]`). |

> Las direcciones y los contactos **no aparecen en esta tabla por diseño**: entran en los eventos de rol y se consultan en la ficha (`[D4]`).

---

## 6. Catálogos del dominio

### 6.1. Roles del tercero

Universal, no varía por país (`[R08]` del alcance v1.0, ratificado). Se extiende por versión del producto, no por configuración del cliente.

| Código | Rol | Dominio dueño |
|--------|-----|---------------|
| `proveedor` | Proveedor | OXP |
| `cliente` | Cliente | CXC *(futuro)* |
| `empleado` | Empleado | RRHH *(futuro)* |
| `entidad_financiera` | Entidad financiera | Tesorería *(futuro)* |
| `otro` | Otro | El dominio que lo informe |

### 6.2. Vocabulario de roles de contacto

Compartido con los dominios a través de la estructura empaquetada del contacto (issue #35) — todos capturan con el mismo vocabulario (`[R22]`).

| Código | Rol del contacto |
|--------|------------------|
| `representante_legal` | Representante legal |
| `tesorero` | Tesorero |
| `comercial` | Comercial |
| `tecnico` | Técnico |
| `facturacion` | Contacto de facturación |
| `notificaciones` | Contacto de notificaciones |
| `otro` | Otro |

### 6.3. Datos de identidad compartidos

Los únicos datos sujetos a divergencia (`[R14]`):

| Dato | Notas |
|------|-------|
| Identificación legal (tipo, número, país, DV) | Su corrección puede cambiar la clave natural (`[SI9]`). |
| Razón social | Comparada en forma canónica para duplicados (`[SI2]`). |
| Tipo de persona | persona / organización. |

### 6.4. Motivos de inactivación global

Propuesta inicial, extensible por versión del producto (ver `[PD3]`):

| Código | Motivo |
|--------|--------|
| `fraude` | Fraude comprobado o en investigación |
| `lista_restrictiva` | Coincidencia en listas restrictivas o de cumplimiento |
| `cierre_relacion` | Cierre definitivo de la relación comercial y laboral |
| `otro` | Otro (con descripción obligatoria) |

---

## 7. Invariantes del dominio

| ID | Invariante | Clase | Mecanismo |
|----|-----------|-------|-----------|
| `[I1]` | No pueden existir dos terceros vigentes (no fusionados) con la misma clave natural (`[R02]`). | Eventual | Índice único `[SI1]`; las colisiones por corrección se tratan vía `[SI9]`. |
| `[I2]` | Todo tercero nace `Activo` y con al menos un rol (`[R16]`). | Local | `TerceroCreado` + `RolIncorporado` en el mismo append. |
| `[I3]` | Un tercero no tiene dos roles con la misma combinación (`rol`, `dominio`, `empresa`). | Local | Precondición de `RolIncorporado`. |
| `[I4]` | La señal global solo cambia por comando del administrador, con motivo (`[R17]`). | Local | Precondición de `TerceroInactivado` / `TerceroReactivado` + permiso (Sección 12). |
| `[I5]` | La identidad compartida no se edita por comando: solo cambia por consolidación o por resolución de conciliación (`[R13]`). | Local | El agregado no expone comandos de edición de datos. |
| `[I6]` | Un duplicado exige ≥2 candidatos; una divergencia exige un tercero y ≥2 versiones. | Local | Precondición de los eventos de apertura. |
| `[I7]` | El canónico de una fusión debe ser uno de los candidatos del caso. | Local | Precondición de `TercerosFusionados`. |
| `[I8]` | Toda decisión de conciliación lleva decisor, fecha y motivo (`[R10]`, `[R11]`). | Local | Precondición de los tres comandos de resolución. |
| `[I9]` | Una homonimia marcada no se reabre por el mismo criterio sobre los mismos terceros (`[R11]`). | Eventual | El servicio consulta la memoria `[SI5]` antes de abrir. |
| `[I10]` | `EnCorreccion` y los cierres por convergencia solo aplican a divergencias. | Local | FSM (Sección 4.2). |
| `[I11]` | Ningún evento de rol recibido se descarta: termina aplicado a un tercero o representado en una conciliación (`[R04]`). | Eventual | Pasos 1-4 del `ServicioDeConsolidacion` + reintento de plataforma (`[D11]`). |
| `[I12]` | Un tercero `Fusionado` es terminal: no consolida más roles ni cambia de señal. | Local | FSM (Sección 4.1). |
| `[I13]` | Los eventos de rol con clave de un tercero absorbido se aplican al canónico. | Eventual | Enrutamiento por el mapa canónico (`[SI4]`, paso 2 del servicio). |

---

## 8. Qué NO contiene este documento

| Tema | Dónde vive |
|------|-----------|
| La captura de terceros, sus formularios y la experiencia de asistencia | Los dominios operativos (alcance, `[R26]`-`[R29]`). |
| Las reglas de validación de identificación, dirección, teléfono, correo y contacto | Las especificaciones del paquete transversal (`compartido/nuggets/`). |
| Los agregados de rol (Proveedor, Cliente, Empleado) | Los modelos de dominio de OXP, CXC y RRHH. |
| Mecanismos de plataforma: concurrencia, deduplicación técnica, reintentos, orden de entrega | `[D11]` — plataforma (Marten + Wolverine), alineado con OXP `[D20]`. |
| El diseño de las proyecciones y del BFF (ficha, asistencia) | Implementación; este documento solo fija su semántica (`[SI6]`, `[SI7]`). |
| Los contratos formales de eventos (esquemas) | EventCatalog (Fase 3 del proyecto). |
| La herramienta de resolución por lotes | Aplicación (`[SI8]` fija las reglas de dominio que la limitan). |
| La migración de datos de cada dominio | Cada dominio (la bodega solo consolida lo que llega — alcance, Sección 7). |

---

## 9. Decisiones de arquitectura y diseño

| ID | Decisión | Justificación |
|----|----------|---------------|
| `[D1]` | **La bodega no captura ni edita: consolida, concilia y señala.** No existen comandos de registro o edición de datos del tercero; las únicas escrituras de usuario son las resoluciones de conciliación y la señal global. | Es el corazón del replanteamiento (#31): la captura vive en los dominios con las validaciones empaquetadas; la bodega que edita se convierte en autoridad y renace el acoplamiento. |
| `[D2]` | **Identidad propia del agregado `Tercero` (`terceroId`) + índice único por clave natural.** | La clave natural es corregible (CC→NIT) y fusionable — no puede ser la identidad del stream. El historial sobrevive a correcciones y fusiones. |
| `[D3]` | **Dos agregados (`Tercero`, `Conciliacion`); el mapa canónico y la memoria de conciliación son proyecciones.** | El mapa y la memoria son acumulados derivados de eventos, sin reglas propias que proteger — agregarlos como agregados duplicaría estado. |
| `[D4]` | **La bodega publica decisiones, no datos.** Direcciones y contactos entran en los eventos de rol y se consultan en la ficha; nunca se re-publican. | Si la bodega re-emitiera datos, cada cambio de teléfono rebotaría por el ERP y la bodega se volvería un repetidor — acoplamiento informativo. |
| `[D5]` | **El contrato de entrada lleva el estado completo del rol (no delta), con `secuencia` por `referenciaOrigen`.** | La consolidación tolera pérdida y desorden de mensajes: el evento más reciente siempre deja el rol correcto. El criterio de "delta" del proyecto aplica a eventos de dominio dentro de un agregado, no a contratos de integración entre BCs. |
| `[D6]` | **La detección es del servicio; la decisión es humana; la apertura no tiene evento propio.** `PosibleDuplicadoDetectado` y `DivergenciaDetectada` son a la vez detección y apertura. | Un evento "CasoAbierto" separado no agrega información — la detección es el hecho de negocio (`[R10]`). |
| `[D7]` | **Fusión por absorción:** el canónico incorpora los roles del absorbido; el absorbido pasa a `Fusionado` (terminal) y sus claves enrutan al canónico. | Los streams de ambos terceros se preservan completos (nada se reescribe, `[R12]`); el enrutamiento garantiza que los dominios no necesiten enterarse de la fusión para seguir publicando. |
| `[D8]` | **Resolución ≠ cierre en divergencias:** la divergencia resuelta queda `EnCorreccion` hasta que los dominios convergen (`ConvergenciaConfirmada`). | Fidelidad al Flujo 4 del alcance: el caso supervisa que la corrección publicada efectivamente se aplicó — visibilidad operativa de lo pendiente. |
| `[D9]` | **El perfil tributario no es parte del agregado `Tercero`** — alimenta la proyección de la ficha directamente. | Impuestos es el dueño; la bodega solo lo muestra. Meterlo al agregado lo convertiría en dato compartido sujeto a divergencia, y no lo es. |
| `[D10]` | **Injerencia por mensajes (`[R27]`) como contrato:** la bodega publica `DatoDeIdentidadCorregido` y la señal global; cada dominio los aplica automáticamente en sus registros, de forma autónoma. | Decisión del usuario (alcance, Flujo 4): corrección automática sí, pero desacoplada y distribuida — nunca escritura remota ni dependencia en línea. |
| `[D11]` | **Concurrencia, idempotencia técnica y trazabilidad delegadas a la plataforma** (Marten + Wolverine). | Alineado con `[D11]` de la v1.0 y `[D20]` de OXP — los mecanismos no se especifican por evento. |
| `[D12]` | **El valor correcto de una divergencia puede ser externo a las versiones informadas**, con evidencia obligatoria. | La verdad puede estar fuera de los dominios (certificado RUES, documento del tercero); restringir a las versiones obligaría a resolver con un dato sabido errado. |

---

## 10. Premisas de negocio

| ID | Premisa |
|----|---------|
| `[P1]` | Los dominios capturan con las validaciones empaquetadas del producto — la calidad de formato y DV se garantiza en el origen (`[R03]`). La bodega verifica con las mismas reglas, pero las anomalías son casos de conciliación, no rechazos (`[R04]`). |
| `[P2]` | Los eventos de los dominios llegan al menos una vez y pueden llegar desordenados. El contrato `[D5]` + `[SI3]` lo absorben. |
| `[P3]` | Tras la carga histórica, el volumen de conciliaciones abiertas será alto — es la deuda de calidad de datos de SincoERP saliendo a la luz (alcance, Sección 7). La resolución por lotes (`[SI8]`) es parte del plan, no una contingencia. |
| `[P4]` | La empresa referenciada en cada rol existe en Estructura Organizacional. Es referencia por identificador, sin validación en caliente. |

---

## 11. Pendientes por definir

| ID | Pendiente | Owner | Criterio / momento de cierre |
|----|-----------|-------|------------------------------|
| `[PD1]` | Veredicto del custodio sobre la estructura empaquetada del contacto (issue #35). Si la estructura cambia, ajustar el contrato de entrada (Sección 5.1). | Custodio (Datos de Referencia) | Resolución del issue #35, antes del desarrollo F1 del contrato. |
| `[PD2]` | Criterios ampliados de detección de duplicados (F2): contactos o direcciones coincidentes entre consolidados. | Producto + consultores | Diseño de F2 — no bloquea F1. |
| `[PD3]` | Ratificar el catálogo de motivos de inactivación global (Sección 6.4) con el comité de producto. | Producto | Antes del desarrollo F1 de la señal global. |
| `[PD4]` | Issue cruzado de refinamiento en Contabilidad: actualizar R07 y las menciones "Terceros como fuente de verdad que valida" al modelo de copia local + reportes canonizados (alcance v2.0, Sección 3). | Este sub-dominio (origina el cambio) | Crear el issue al fusionar el PR del #33. |

---

## 12. Catálogo de permisos atómicos del dominio

| # | Permiso | Operación que habilita | Actor típico |
|---|---------|------------------------|--------------|
| 1 | `terceros.ficha.consultar` | Consultar la vista consolidada (Flujo 6). | Usuario operativo, administrador. |
| 2 | `terceros.conciliacion.consultar` | Ver casos de conciliación y su evidencia. | Administrador de terceros. |
| 3 | `terceros.conciliacion.fusionar` | `FusionarTerceros`. | Administrador de terceros. |
| 4 | `terceros.conciliacion.marcar-homonimia` | `MarcarHomonimia`. | Administrador de terceros. |
| 5 | `terceros.conciliacion.resolver-divergencia` | `ResolverDivergencia`. | Administrador de terceros. |
| 6 | `terceros.conciliacion.agregar-nota` | `AgregarNota`. | Administrador de terceros. |
| 7 | `terceros.conciliacion.resolver-por-lotes` | Usar la marca de lote en resoluciones (`[SI8]`) — permiso aparte por su alcance masivo. | Administrador de terceros (carga histórica). |
| 8 | `terceros.tercero.inactivar` | `InactivarTercero` (señal global). | Administrador de terceros. |
| 9 | `terceros.tercero.reactivar` | `ReactivarTercero`. | Administrador de terceros. |
| 10 | `terceros.asistencia.consultar` | La consulta de asistencia de captura (Flujo 2). | Sistema — formularios de los dominios (vía BFF). |
| 11 | `terceros.mapa-canonico.consultar` | Consultar la proyección del mapa canónico (`[SI4]`). | Sistema — Contabilidad y demás interesados. |

> Todos los permisos de resolución y señal global son de **operador humano** — no existen vías de sistema para decidir conciliaciones ni inactivar terceros (`[R10]`, `[R17]`).

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 2.0 | Junio 2026 | **Reescritura completa por el replanteamiento arquitectónico (#31, #33):** bodega consolidadora. 2 agregados raíz (`Tercero` con entidad interna `Rol` y estado terminal `Fusionado`; `Conciliacion` con seguimiento `EnCorreccion`), 1 domain service (`ServicioDeConsolidacion`, 5 pasos sin compensaciones), 7 VOs (5 del paquete transversal + `Candidato`/`VersionDeDato`), 16 eventos de dominio + 1 de integración derivado (`DatoDeIdentidadCorregido`) + contrato de entrada del evento de rol (estado completo + secuencia, `[D5]`), 2 FSM, 4 catálogos, 13 invariantes (9 Local + 4 Eventual), 12 decisiones, 4 premisas, 4 pendientes, 9 SIs, 11 permisos. Desaparecen frente a la v1.0: comandos de registro/edición, estados `EnRegistro`/`Abortado`, referencias a direcciones, dependencias de ejecución con Datos de Referencia y Direcciones. |
| 1.0 | Abril 2026 | Versión inicial (autoridad de registro). Conservada en `modelo-dominio_bk.md`. |
