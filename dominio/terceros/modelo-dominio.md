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
| **La bodega publica decisiones, no datos** | Los datos de los roles (direcciones, contactos) **entran** en los eventos de rol y se **consultan** en la ficha; nunca se re-publican como avisos. Lo único que la bodega publica son sus decisiones: señal global, fusiones, correcciones (ver `[D##]` en Sección 9). |

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
 │  │                          │  │   │  Decisión + motivo      │ │
 │  │  ┌─────────────────────┐ │  │   │  Estado: Abierta →      │ │
 │  │  │ Entidad: Rol (0..N) │ │  │   │    Resuelta | Superada  │ │
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

**Identidad del agregado:** el `Tercero` tiene **identificador propio** (`terceroId`), con **índice único por clave natural** (tipo de documento + número + país). La clave natural **no puede ser** la identidad del agregado porque es corregible: el caso CC→NIT (una conciliación corrige el tipo de documento) cambiaría la clave, y una fusión hace que dos claves apunten al mismo tercero. Con identificador propio, el historial sobrevive a ambas. *(Ver `[D##]` en Sección 9 y su `[SI##]` de índice.)*

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

**Eventos que emite:** `TerceroCreado`, `RolIncorporado`, `RolActualizado`, `RolInactivado`, `IdentidadActualizada` (consolidación); `TerceroInactivado`, `TerceroReactivado` (señal global); y participa en la fusión (`TercerosFusionados` — la mecánica exacta entre `Tercero` y `Conciliacion` se define en la Sección 5).

**Qué protege (anticipo de invariantes, Sección 7):** unicidad por clave natural (`[R02]`, eventual vía índice); nace Activo con al menos un rol; un solo rol por (`rol`, `dominio`, `empresa`); cambio de señal global solo por administrador con motivo; ningún dato de identidad se edita directamente en la bodega — solo consolidación o resolución (`[R13]`).

### 3.3. Agregado: Conciliacion

*(En construcción)*

### 3.4. Value Objects

*(En construcción)*

### 3.5. Sugerencias de implementación

*(En construcción)*

### 3.6. Domain service: ServicioDeConsolidacion

*(En construcción)*

### 3.7. Relaciones y referencias externas

*(En construcción)*

---

## 4. Máquinas de estado

*(En construcción)*

---

## 5. Catálogo de eventos

*(En construcción — incluirá el contrato del evento de rol como sección de integración de entrada)*

---

## 6. Catálogos del dominio

*(En construcción)*

---

## 7. Invariantes del dominio

*(En construcción)*

---

## 8. Qué NO contiene este documento

*(En construcción)*

---

## 9. Decisiones de arquitectura y diseño

*(En construcción)*

---

## 10. Premisas de negocio

*(En construcción)*

---

## 11. Pendientes por definir

*(En construcción)*

---

## 12. Catálogo de permisos atómicos del dominio

*(En construcción)*

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 2.0 | Junio 2026 | **Reescritura por el replanteamiento arquitectónico (#31, #33):** bodega consolidadora — 2 agregados (`Tercero`, `Conciliacion`), 1 domain service (`ServicioDeConsolidacion`). En construcción. |
| 1.0 | Abril 2026 | Versión inicial (autoridad de registro). Conservada en `modelo-dominio_bk.md`. |
