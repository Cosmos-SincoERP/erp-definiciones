# Modelo de Dominio — Terceros

**Versión:** 1.0
**Última actualización:** 2026-04-21
**Documento de alcance de referencia:** `definicion-alcance.md`

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

Este documento describe el comportamiento del sub-dominio de Terceros en términos de DDD / Event Sourcing / EDA: agregados, eventos, máquinas de estado, invariantes, catálogos del dominio, decisiones de diseño y permisos atómicos. No duplica el alcance funcional — lo complementa.

| Documento | Rol | Descripción |
|-----------|-----|-------------|
| `definicion-alcance.md` | QUÉ hace Terceros | Fuente de verdad para glosario (11 términos), actores, 6 flujos principales, 24 reglas de negocio, alcance dentro/fuera y fases de implementación. No se duplica aquí. |
| **Este documento** | CÓMO se comporta Terceros | Agregados, eventos, transiciones de estado, precondiciones, invariantes, catálogos del dominio, decisiones de arquitectura y permisos atómicos. |
| `anexo-decision-orquestacion-registro.md` | Contexto de integración | Documenta el patrón BFF / API Composition externo al sub-dominio para coordinar el registro completo de un tercero (identidad + dirección + perfil tributario + condiciones comerciales). |
| EventCatalog | Catalogación técnica | Fase 3 del proyecto. Consumirá este documento como especificación de entrada. |

Las reglas de negocio se referencian como `[R##]` y su texto completo vive en `definicion-alcance.md`, Sección 6.

---

## 2. Convenciones del documento

### 2.1. Nomenclatura

- **Eventos:** PascalCase en español, sin tildes ni caracteres especiales (compatibilidad con código fuente). Ejemplo: `TerceroRegistrado`, `ContactoActualizado`.
- **Referencias:** `[R##]` reglas de negocio, `[P##]` premisas, `[D##]` decisiones, `[I##]` invariantes, `[SI##]` sugerencias de implementación, `[PD#]` pendientes.
- **Agregados:** PascalCase; corresponden a los términos del glosario canónico (`definicion-alcance.md`, Sección 2).
- **Alcance del glosario canónico:** Los domain services, entidades internas y value objects son artefactos del modelo de dominio — no requieren entrada en el glosario canónico.

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
| **Efectos** | Consecuencias: entidades creadas, estado modificado, eventos derivados, notificaciones a dominios consumidores. |

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

### 3.1. Terceros como Bounded Context

El sub-dominio de Terceros es el registro centralizado de la identidad base de personas y empresas con las que la organización tiene relación. Su frontera cubre identidad (tipo de persona, identificación, razón social), roles asignados (como atributos, no como agregados), contactos del tercero, estado del ciclo de vida (`EnRegistro`, `Activo`, `Inactivo`, `Abortado`) y referencias a sus direcciones (un tercero puede tener varias: fiscal, comercial, correspondencia). Todo lo demás — contenido de direcciones, perfil tributario, cuentas bancarias, condiciones comerciales, datos laborales — vive en otros sub-dominios `[R20]`.

**Un solo agregado raíz:** `Tercero`. Contiene `Contacto` como componente interno (ver `[D2]` en Sección 9). Los roles se modelan como atributos del tercero (set de tags universales), no como agregados independientes — los agregados de rol viven en los dominios consumidores (ver `[D1]`).

**Diagrama:**

```
                  ┌─────────────────────────────────────────────┐
                  │            Entradas al sub-dominio           │
                  │                                              │
                  │  · Administrador de terceros                 │
                  │  · Usuario operativo desde OXP / CXC / RRHH  │
                  │    (solicita creación de un tercero)         │
                  │  · Procesos de recepción (orquestados        │
                  │    externamente; reparten datos a los        │
                  │    dominios dueños):                         │
                  │     ─ SincoRE (recepción electrónica         │
                  │       de factura)                            │
                  │     ─ Importación masiva desde archivos      │
                  │     ─ Lectura de documentos de soporte       │
                  │       (RUT y equivalentes por país)          │
                  └────────────────────┬─────────────────────────┘
                                       │ comandos
                                       ▼
 ┌──────────────────────────────────────────────────────────────────┐
 │                   Bounded Context: Terceros                       │
 │                                                                   │
 │   ┌────────────────────────────────────────────────────────┐    │
 │   │                  Agregado: Tercero                      │    │
 │   │                                                         │    │
 │   │   Identificacion (VO)       TipoPersona                │    │
 │   │   Roles (set, universales)                              │    │
 │   │   Estado: EnRegistro → Activo ↔ Inactivo / Abortado    │    │
 │   │   Direcciones (VO ReferenciaDireccion, colección N:    │    │
 │   │     fiscal, comercial, correspondencia, etc.)          │    │
 │   │                                                         │    │
 │   │   ┌────────────────────────────────────────────┐      │    │
 │   │   │  Componente: Contacto (colección N)         │      │    │
 │   │   │   · Nombre · RolContacto                    │      │    │
 │   │   │   · CorreoElectronico (VO, colección 0..N)  │      │    │
 │   │   │   · Telefono (VO, colección 0..N)           │      │    │
 │   │   │   · esPrincipal (exactamente 1 por tercero  │      │    │
 │   │   │     activo, con correo y teléfono)          │      │    │
 │   │   │   · Estado: Activo ↔ Inactivo               │      │    │
 │   │   └────────────────────────────────────────────┘      │    │
 │   └────────────────────────────────────────────────────────┘    │
 │                                                                   │
 └──────────────────────────────┬────────────────────────────────────┘
                                │ notifica cambios de identidad,
                                │ roles y contactos
        ┌──────────────┬────────┼───────┬─────────────┬─────────────┐
        ▼              ▼        ▼       ▼             ▼             ▼
  ┌──────────┐  ┌──────────┐ ┌───────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
  │Impuestos │  │   OXP    │ │  CXC  │ │  RRHH    │ │Tesorería │ │ Emisión  │
  │(perfiles │  │(proveedor│ │(cli-  │ │(empleado)│ │(ctas.    │ │Electrón. │
  │ tribut.) │  │  )       │ │ ente) │ │          │ │ bancar.) │ │          │
  └──────────┘  └──────────┘ └───────┘ └──────────┘ └──────────┘ └──────────┘

 Lecturas (consumo de referencia externa):
   ◄── Datos de Referencia (tipos de documento, países)
   ◄── Direcciones (contenido por identificador)
```

**Frontera con otros dominios:**

| Dominio | Relación | Datos que intercambia |
|---------|----------|-----------------------|
| Datos de Referencia | Consumo (lectura) | Catálogo de tipos de documento y países; reglas de formato y DV. |
| Direcciones | Consumo por referencia | Terceros guarda el identificador; el contenido lo gestiona Direcciones. |
| OXP, CXC, RRHH | Entrada + notificación saliente | Entrada: solicitud de creación desde el flujo operativo. Salida: eventos de rol asignado/removido, creación, actualización, inactivación. |
| Impuestos, Tesorería, Emisión Electrónica | Notificación saliente | Eventos de identidad y contactos; cada dominio mantiene su propia vista local del tercero. |
| Procesos de recepción (SincoRE, importación masiva, lectura de documentos de soporte) | Entrada orquestada externamente | Datos del tercero repartidos por el proceso de recepción a los dominios dueños. Terceros recibe únicamente identidad y roles. |

**Domain services:** Terceros **no tiene domain services**. Solo existe un agregado propio (por tanto, no hay coordinación entre agregados internos), y la orquestación multi-dominio del registro completo vive externamente (capa BFF / API Composition, ver `anexo-decision-orquestacion-registro.md`).

### 3.2. Agregado: Tercero

**Descripción:** Registro centralizado de la identidad base de una persona o empresa con la que la organización tiene relación. Es la fuente de verdad de los datos de identidad y el punto único de emisión de eventos que notifican cambios a los dominios consumidores (Impuestos, OXP, CXC, RRHH, Tesorería, Emisión Electrónica). Los agregados de rol en los dominios consumidores reaccionan a estos eventos para abrir y mantener sus propios registros del tercero en cada contexto.

**Raíz:** `Tercero`
**Identificador técnico:** `terceroId` (UUID interno, asignado al registrar).
**Clave natural:** `Identificacion` (tipo de documento + número + país) — única en todo el sistema `[R01]`. Ver detalle en la composición.

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| `Identificacion` | VO | Clave natural del tercero según el catálogo de Datos de Referencia `[R03]`. | `tipoDocumento`, `numero`, `pais` |
| `digitoVerificacion` | Atributo (opcional) | Carácter verificador del número de documento cuando aplica según el tipo (ej: NIT en Colombia). Se almacena como campo aparte del VO `Identificacion`; no participa en la clave de unicidad (ver `[D6]`). Su presencia y algoritmo de validación los publica el catálogo de tipos de documento `[R04]`. | string (opcional según `tipoDocumento`) |
| `tipoPersona` | Atributo | Clasificación base: persona (individuo) u organización (entidad constituida). Es dato de identidad, no tributario `[R05]`. | enum: `Persona`, `Organizacion` |
| `razonSocial` | Atributo | Nombre legal del tercero. Para personas: nombres y apellidos. Para organizaciones: nombre registrado. | string |
| `roles` | Atributo (set) | Conjunto de roles universales asignados al tercero `[R07]` `[R08]`. Los valores del enum están definidos en el **catálogo canónico § 6.1**; los agregados de rol viven en dominios consumidores (ver `[D1]`). | set de enum (ver § 6.1) |
| `Direcciones` | VO `ReferenciaDireccion` (colección N≥1) | Referencias a las direcciones del tercero por tipo de uso. Terceros guarda el identificador, el tipo de uso y la marca de preferida; el contenido (calle, ciudad, municipio, etc.) vive en el servicio de Direcciones `[R21]`. Ver `[D4]`. **Todo tercero activo debe tener al menos una referencia con `tipoUso = Fiscal` `[R25]`.** Como mucho una referencia con `esPreferida = true` por `tipoUso`. | colección de `ReferenciaDireccion` (`direccionId`, `tipoUso` enum: `Fiscal`, `Comercial`, `Correspondencia`, `Otro`, `esPreferida` booleano) |
| `Contacto` (colección N) | Componente interno | Personas de contacto del tercero con ciclo de vida propio dentro del agregado `[R11]` `[R12]`. Los medios de comunicación (correos, teléfonos) son exclusivos del contacto — no existen a nivel del tercero `[R14]`. Ver sub-tabla siguiente. | colección de `Contacto` |
| `estado` | Atributo | Estado del tercero: `EnRegistro` (identidad registrada, pendiente confirmación de dirección fiscal por el servicio de Direcciones — no operable), `Activo` (puede usarse en nuevas operaciones), `Inactivo` (solo referencias históricas), `Abortado` (registro nunca completó por fallo permanente; estado terminal). Estado inicial: `EnRegistro` `[R16]`. Ver `[D13]`. | enum: `EnRegistro`, `Activo`, `Inactivo`, `Abortado` |

**Componente interno `Contacto`:**

| Sub-componente | Tipo | Descripción | Atributos clave |
|----------------|------|-------------|-----------------|
| `contactoId` | Identificador | Identificador técnico del contacto dentro del agregado Tercero. | UUID interno |
| `nombre` | Atributo (opcional) | Nombre de la persona de contacto. Opcional al registro; se recomienda completarlo posteriormente para comunicaciones personalizadas `[R13]`. | string (opcional) |
| `rolContacto` | Atributo | Rol del contacto (representante legal, tesorero, comercial, técnico, contacto de facturación, contacto de notificaciones, otro). Ver catálogo en Sección 6. | enum del catálogo |
| `correos` | VO `CorreoElectronico` (colección 0..N) | Correos electrónicos del contacto. Como mucho uno marcado `preferido = true` dentro de la colección. | colección de `CorreoElectronico` (`valor`, `preferido`) |
| `telefonos` | VO `Telefono` (colección 0..N) | Teléfonos del contacto. Como mucho uno marcado `preferido = true` dentro de la colección. | colección de `Telefono` (`indicativoPais`, `numero`, `preferido`) |
| `esPrincipal` | Atributo | Marca técnica ortogonal al rol. Exactamente uno de los contactos activos del tercero debe tener `esPrincipal = true`. Si es principal, debe tener al menos un correo y al menos un teléfono registrados `[R15]`. | booleano |
| `estado` | Atributo | Estado del contacto: `Activo` o `Inactivo`. Ciclo de vida independiente del tercero y de los demás contactos `[R12]`. | enum: `Activo`, `Inactivo` |

> **Regla de medios de comunicación:** Todo contacto debe tener al menos un medio (correo o teléfono) `[R13]`. El contacto marcado como principal debe tener al menos uno de cada tipo (un correo + un teléfono) `[R15]`.

**Ciclo de vida del agregado:**

- **Nacimiento (estado `EnRegistro`):** Un Tercero nace con el evento `TerceroRegistrado` en estado `EnRegistro` `[R16]` `[D13]`. Requiere como mínimo: identidad válida (tipo de documento, número, país, DV si aplica), tipo de persona, razón social, al menos un rol asignado y un contacto principal con al menos un correo y un teléfono `[R15]`. El nombre del contacto principal es opcional al registro y puede completarse posteriormente `[R13]`. En este estado el tercero **no es operable** por los dominios consumidores; la dirección fiscal aún no ha sido confirmada por Direcciones. Admite configurar roles y contactos mientras se espera la activación.
- **Activación (transición a `Activo`):** El tercero transiciona a `Activo` con el evento `TerceroActivado` cuando el servicio de Direcciones confirma asincrónicamente la creación de la dirección fiscal. Solo en este momento se notifica a los dominios consumidores del rol para que abran sus registros en cada contexto. Si Direcciones falla permanentemente tras los reintentos automáticos, el tercero transiciona a `Abortado` (terminal, no reactivable) con `TerceroRegistroAbortado`. Ver `[D13]`.
- **Operación (estado `Activo`):** El tercero acumula eventos de identidad, gestión de roles, gestión de referencias a direcciones y gestión de contactos. Los invariantes fuertes del tercero operable (dirección fiscal obligatoria `[I6]`, contacto principal `[I4]`) aplican en este estado.
- **Inactivación:** Transiciona a `Inactivo` con `TerceroInactivado`. Los registros históricos y las referencias en otros dominios se conservan intactos `[R17]`. La validación del estado para permitir una nueva transacción es responsabilidad de cada consumidor `[R18]`.
- **Reactivación:** Puede volver a `Activo` con `TerceroReactivado` si la relación comercial o laboral se retoma `[R19]`. La reactivación solo aplica al estado `Inactivo` — el estado `Abortado` es terminal y no se reactiva; un nuevo intento de registro del mismo tercero se resuelve con un nuevo `terceroId`.
- **Terminación:** No tiene estado terminal — el Tercero nunca se elimina.

**Ciclo de vida del componente Contacto:**

- **Nacimiento:** `ContactoRegistrado` en estado `Activo`. Un contacto puede designarse como principal en el mismo registro o posteriormente mediante `ContactoPrincipalDesignado`.
- **Operación:** Actualización de nombre, rol y medios de comunicación con `ContactoActualizado`. Reasignación de la marca de principal con `ContactoPrincipalDesignado` — un único evento que registra al nuevo principal y al anterior que deja de serlo (ver `[D7]`).
- **Inactivación:** `ContactoInactivado`. **Precondición:** si el contacto a inactivar es el principal, debe haberse designado previamente otro contacto activo como principal mediante `ContactoPrincipalDesignado`. No se permite inactivar al principal sin que exista un reemplazo activo y designado `[R15]`.
- **Reactivación:** `ContactoReactivado` — el contacto vuelve a estado `Activo` pero no recupera automáticamente la marca de principal. Para volver a ser principal, debe emitirse un nuevo `ContactoPrincipalDesignado`.
- Los contactos inactivos se conservan en el agregado; nunca se eliminan.

**Stream de eventos:**

- **Estrategia:** un stream por tercero, identificado como `tercero-{terceroId}`.
- Todos los eventos del agregado — tanto de la raíz `Tercero` como del componente interno `Contacto` — se appendan al mismo stream. Refleja la consistencia transaccional del agregado y permite reconstruir el estado completo (incluyendo todos los contactos) replicando el stream desde el inicio.
- Los eventos conservan el orden de aparición.

**Comportamiento calculado (no almacenado):**

| Método | Fórmula | Usado por |
|--------|---------|-----------|
| `contactoPrincipalActivo()` | Retorna el `Contacto` con `esPrincipal = true` y `estado = Activo`. Existe exactamente uno por tercero activo (`[I4]`). | Dominios consumidores (Emisión Electrónica, OXP, CXC) para identificar el canal oficial de comunicación con el tercero. |
| `estaActivo()` | Retorna `true` solo si `estado = Activo`. Los estados `EnRegistro`, `Inactivo` y `Abortado` retornan `false` — el tercero no es operable en ninguno de ellos. | Dominios consumidores para validar antes de iniciar una nueva transacción `[R18]` `[R23]`. |
| `tieneRol(rol)` | Retorna `true` si el rol está presente en el set `roles`. | Dominios consumidores: OXP valida "¿es proveedor?", CXC valida "¿es cliente?", RRHH valida "¿es empleado?", etc. `[R07]` |
| `direccionesPorTipoUso(tipoUso)` | Retorna todas las referencias con el `tipoUso` indicado. Puede retornar cero, una o varias. | Dominios consumidores que necesitan iterar sobre las direcciones de un tipo (ej: todas las sucursales comerciales). |
| `direccionPreferidaPorTipoUso(tipoUso)` | Retorna la única referencia con `tipoUso` y `esPreferida = true`, o `null` si no existe. Nota: para `tipoUso=Fiscal`, siempre existe mientras el tercero esté activo (`[I6]`). | Consumidores que necesitan "la" dirección fiscal/comercial/correspondencia (caso mayoritario: Impuestos para dirección fiscal, Emisión Electrónica para correspondencia, CXC para comercial). |
| `contactosActivos()` | Retorna la sub-colección de `Contactos` con `estado = Activo`. | Vistas de consulta y dominios consumidores que requieran iterar sobre los contactos vigentes. |
| `contactosPorRol(rolContacto)` | Retorna los contactos cuyo `rolContacto` coincide (representante legal, contacto de facturación, etc.). | Emisión Electrónica para obtener el representante legal; Tesorería para obtener el contacto de facturación. |
| `identificacionVigente()` | Retorna la `Identificacion` actual del tercero. | Dominios consumidores para comparar contra su vista local y detectar cambios tras recibir `TerceroIdentificacionActualizada`. |

**Eventos propios del agregado (18 eventos):**

Organizados por tema funcional. El detalle completo de cada evento (payload, precondiciones, efectos, causalidad) se documenta en la **Sección 5 — Catálogo de eventos**.

| Tema | # | Evento | Estado previo | Estado resultante |
|------|---|--------|---------------|-------------------|
| **Identidad** | 1 | `TerceroRegistrado` | — (no existe) | Tercero `EnRegistro` |
|  | 2 | `TerceroIdentificacionActualizada` | Tercero `Activo` | Tercero `Activo` (progreso) |
|  | 3 | `TerceroRazonSocialActualizada` | Tercero `Activo` | Tercero `Activo` (progreso) |
|  | 4 | `TerceroTipoPersonaActualizado` | Tercero `Activo` | Tercero `Activo` (progreso) |
| **Estado** | 5 | `TerceroActivado` | Tercero `EnRegistro` | Tercero `Activo` |
|  | 6 | `TerceroRegistroAbortado` | Tercero `EnRegistro` | Tercero `Abortado` (terminal) |
|  | 7 | `TerceroInactivado` | Tercero `Activo` | Tercero `Inactivo` |
|  | 8 | `TerceroReactivado` | Tercero `Inactivo` | Tercero `Activo` |
| **Roles** | 9 | `TerceroRolAsignado` | Tercero `EnRegistro` o `Activo` | sin cambio (progreso) |
|  | 10 | `TerceroRolRemovido` | Tercero `EnRegistro` o `Activo` | sin cambio (progreso) |
| **Direcciones** | 11 | `TerceroDireccionReferenciada` | Tercero `Activo` | Tercero `Activo` (progreso) |
|  | 12 | `TerceroDireccionDesreferenciada` | Tercero `Activo` | Tercero `Activo` (progreso) |
|  | 13 | `TerceroDireccionPreferidaDesignada` | Tercero `Activo` | Tercero `Activo` (progreso) |
| **Contactos** | 14 | `ContactoRegistrado` | Tercero `EnRegistro` o `Activo` | Contacto `Activo` |
|  | 15 | `ContactoActualizado` | Contacto `Activo` | Contacto `Activo` (progreso) |
|  | 16 | `ContactoInactivado` | Contacto `Activo` | Contacto `Inactivo` |
|  | 17 | `ContactoReactivado` | Contacto `Inactivo` | Contacto `Activo` |
|  | 18 | `ContactoPrincipalDesignado` | Contacto `Activo` (ambos) | Contacto `Activo` (progreso; cambia `esPrincipal` en el nuevo y en el anterior simultáneamente) |

**Notas sobre granularidad:**

- `TerceroIdentificacionActualizada` cubre cambios en `tipoDocumento`, `numero`, `pais` y `digitoVerificacion` (clave técnica y su verificador). Si varios atributos cambian en una misma operación, se emite un único evento con todos los valores nuevos. Si solo cambia el DV (corrección puntual), se emite el mismo evento con el DV actualizado y los demás campos iguales.
- `TerceroRazonSocialActualizada` es un evento separado porque el cambio no afecta la clave técnica pero sí los documentos emitidos a nombre del tercero.
- `TerceroTipoPersonaActualizado` es un evento separado por su impacto tributario (Impuestos revalida el perfil tributario).
- Si en una misma operación cambian atributos de temas distintos (ej: razón social + tipo persona), se emiten N eventos — uno por tema.

**Nota sobre `TerceroRegistrado` y origen del registro (ver `[D8]`):**

Un único evento cubre todos los orígenes posibles (manual, desde un consumidor, importación masiva, SincoRE, documento de soporte, otro). El evento lleva un atributo `origen` (enum) y un objeto `contextoOrigen` con la metadata específica del origen. Esto evita proliferar eventos por canal y mantiene extensibilidad para nuevos orígenes a futuro (ej: portal de autogestión, API pública). El detalle del payload y los contextos por origen se documenta en la **Sección 5 — Catálogo de eventos**.

### 3.3. Value Objects

Todos los VOs del dominio son **inmutables**: cualquier cambio implica crear una nueva instancia. Las validaciones indicadas aplican al construir la instancia.

**Nota sobre el término "medio de comunicación".** Las reglas del alcance (`[R13]`, `[R15]`) usan *"medio de comunicación"* como término colectivo informal para referirse indistintamente a un correo electrónico o un teléfono. En el modelo de dominio **no existe como VO, entidad ni agregado** — se implementa como dos VOs independientes: `CorreoElectronico` (3.3.3) y `Telefono` (3.3.4). El agrupamiento es lingüístico, no estructural.

#### 3.3.1. `Identificacion`

Clave natural del tercero según el catálogo de tipos de documento de Datos de Referencia.

| Atributo | Tipo | Descripción |
|----------|------|-------------|
| `tipoDocumento` | string (código del catálogo) | Tipo de documento según el catálogo de Datos de Referencia (ej: `NIT`, `CC`, `RNC`). |
| `numero` | string | Número del documento, sin separadores ni DV. |
| `pais` | string (ISO 3166-1 alpha-2) | País emisor del documento (ej: `CO`, `DO`, `PA`). |

**Validaciones al construir:**
- `tipoDocumento` debe existir en el catálogo para el `pais` indicado `[R03]`.
- `numero` debe cumplir el formato (longitud, caracteres permitidos) publicado por el catálogo `[R04]`.
- `pais` debe existir en el catálogo de países.

**Igualdad:** dos `Identificacion` son iguales si coinciden los tres atributos. El `digitoVerificacion` del tercero **no forma parte** de la comparación (ver `[D6]`).

---

#### 3.3.2. `ReferenciaDireccion`

Referencia a una dirección gestionada por el servicio externo de Direcciones. Terceros guarda el identificador, el tipo de uso y la marca de preferida; nunca el contenido (ver `[D4]`, `[R21]`).

| Atributo | Tipo | Descripción |
|----------|------|-------------|
| `direccionId` | UUID | Identificador de la dirección en el servicio de Direcciones. |
| `tipoUso` | enum | Uso de la dirección dentro del tercero: `Fiscal`, `Comercial`, `Correspondencia`, `Otro`. |
| `esPreferida` | booleano | Marca si esta referencia es la preferida dentro de su `tipoUso`. Default `false`. |

**Validaciones al construir:**
- `direccionId` debe ser un UUID válido, no nulo.
- `tipoUso` debe ser uno de los valores del enum.

**Invariantes de colección (viven en el agregado Tercero, no en el VO):**
- Como mucho una `ReferenciaDireccion` con `esPreferida = true` por cada `tipoUso`.
- No puede haber dos `ReferenciaDireccion` con el mismo `direccionId` dentro del mismo tercero.

---

#### 3.3.3. `CorreoElectronico`

Medio de comunicación tipo correo asociado a un `Contacto`.

| Atributo | Tipo | Descripción |
|----------|------|-------------|
| `valor` | string | Dirección de correo. |
| `preferido` | booleano | Marca si es el correo preferido del contacto. Default `false`. |

**Validaciones al construir:**
- `valor` debe cumplir el formato RFC 5322 (regex robusta).
- Longitud máxima de `valor`: 254 caracteres (estándar).
- `valor` no puede contener espacios en blanco.
- El dominio debe contener al menos un punto (ej: `usuario@dominio.com`).

**Invariante de colección:** como mucho un `CorreoElectronico` con `preferido = true` dentro de los correos del mismo contacto.

**Validación opcional documentada en `[SI##]`:** verificación de registro MX del dominio (no se hace al construir el VO porque implica I/O).

---

#### 3.3.4. `Telefono`

Medio de comunicación tipo teléfono asociado a un `Contacto`.

| Atributo | Tipo | Descripción |
|----------|------|-------------|
| `indicativoPais` | string | Código internacional de marcación en formato E.164 (ej: `+57`, `+1`, `+507`). |
| `numero` | string | Número telefónico, solo dígitos, sin separadores. |
| `preferido` | booleano | Marca si es el teléfono preferido del contacto. Default `false`. |

**Validaciones al construir:**
- `indicativoPais` debe iniciar con `+` seguido de 1 a 3 dígitos.
- `numero` debe ser solo dígitos; no puede estar vacío.
- Longitud total (dígitos del indicativo sin `+` más dígitos del número) entre 8 y 15 (estándar E.164).

**Validación por país delegada al catálogo de Datos de Referencia:** cuando el catálogo publique las longitudes válidas por país, se valida la coherencia entre `indicativoPais` y `numero`. Mientras no exista, aplica la validación E.164 genérica anterior. Este pendiente queda al catálogo de Datos de Referencia — sin ownership en Terceros.

**Invariante de colección:** como mucho un `Telefono` con `preferido = true` dentro de los teléfonos del mismo contacto.

### 3.4. Sugerencias de implementación

Recomendaciones técnicas que complementan las definiciones de dominio. No son reglas del dominio — son estrategias de implementación que el equipo de desarrollo debe considerar.

#### `[SI1]` Índice de unicidad sobre `Identificacion`

El sistema mantiene una **proyección (read model) con constraint de unicidad compuesto** sobre la tupla `(tipoDocumento, numero, pais)` para garantizar `[R01]` / `[I1]`. La proyección vive fuera del stream de eventos y se alimenta por `TerceroRegistrado` y `TerceroIdentificacionActualizada`. El constraint compuesto del read model es el mecanismo de enforcement — rechaza el duplicado antes de que un segundo append proceda. Clasificado como eventual por formalidad (cruza streams), con **ventana de inconsistencia mínima**.

Adicionalmente, la proyección soporta lookup **solo por `numero`** (sin `tipoDocumento` ni `pais`) para la validación de segundo nivel `[I11]`: al intentar registrar un tercero, si la clave primaria no coincide pero el `numero` sí coincide con otro registro, el sistema compara la razón social canónica (`[SI9]`) y aplica el rechazo/aceptación según `[I11]`.

Los comandos `RegistrarTercero`, `RegistrarTerceroForzado` y `AsegurarTerceroDesdeConsumidor` (`[SI10]`) consultan esta proyección antes de emitir cualquier evento de registro. Los casos extremos de inconsistencia eventual (migraciones sucias, fallos del read model) se resuelven operativamente fuera del modelo de dominio, siguiendo el mismo patrón de OXP `[I1]` / `[SI4]`.

La proyección **excluye a los terceros en estado `Abortado`**. Una identificación cuyo tercero quedó en Abortado (por fallo permanente durante el registro en dos fases — ver `[D13]`) queda disponible para un nuevo intento con otro `terceroId`. El tercero Abortado se conserva como evidencia histórica del intento fallido, pero no bloquea nuevos registros con la misma identificación.

#### `[SI2]` Servicio de validación y cálculo del DV

Un servicio o utility de dominio consume el algoritmo de cálculo del DV publicado por el catálogo de Datos de Referencia (según `tipoDocumento` y `pais`) y lo aplica tanto al validar una entrada manual como al calcular automáticamente el DV cuando no se provee. Soporta ambos escenarios: DV calculado por el sistema y DV capturado manualmente (datos legados con DV histórico).

#### `[SI3]` Búsqueda de terceros por identificación

El repositorio de Terceros expone un método de lookup:

```
buscarPorIdentificacion(tipoDocumento, numero, pais?) : Tercero | null
```

- Si `pais` se provee, retorna el tercero que coincida exactamente o `null`.
- Si `pais` se omite y solo hay un tercero con esa combinación `tipoDocumento + numero`, retorna ese tercero.
- Si `pais` se omite y hay varios candidatos (mismo tipo y número en distintos países), retorna un error explícito de ambigüedad para que el consumidor especifique el país.

Útil para consumidores que tienen los datos de identidad y necesitan resolver el `terceroId`.

#### `[SI4]` Proyección CQRS del historial de identidad

Una proyección consume desde el stream los eventos `TerceroIdentificacionActualizada`, `TerceroRazonSocialActualizada` y `TerceroTipoPersonaActualizado`, y materializa una vista de lectura con atributos: `{ terceroId, tipoCambio, valorAnterior, valorNuevo, fechaCambio, usuarioId }`. Atiende casos de auditoría, reportes regulatorios retroactivos (ej: exógena DIAN de años anteriores usa la identificación vigente en ese período) y reconciliación de registros históricos tras un cambio de identificación `[R06]`. Ver `[D3]`.

#### `[SI5]` Vistas de lectura compuestas fuera del agregado

Toda vista que combine datos de Terceros con otros dominios (Direcciones, Impuestos, OXP, CXC, Tesorería) se resuelve por la capa externa de composición (BFF / API Composition), siguiendo el patrón del `anexo-decision-orquestacion-registro.md`. Esto incluye:

- La **vista consolidada de completitud** (Flujo 6 del alcance) que integra identidad + dirección + perfil tributario + cuentas bancarias + condiciones comerciales para mostrar el checklist de "tercero listo para operar" por contexto.
- La **vista operativa Tercero + contenido de direcciones** (ciudad, municipio, departamento) para consumidores que frecuentemente necesitan ambos datos en una misma transacción.
- Cualquier otra combinación consumida por UI o por flujos transaccionales.

Terceros no compone estas vistas — expone sus datos propios y la capa externa resuelve la composición. Ver `[D5]`.

#### `[SI6]` Proyección de contacto principal por tercero

Proyección que materializa el contacto principal activo de cada tercero como vista de lectura: `{ terceroId, contactoPrincipalId, nombre, correoPreferido, telefonoPreferido }`. Se actualiza por los eventos `ContactoRegistrado` (si nace como principal), `ContactoPrincipalDesignado` y `ContactoInactivado`. Permite consultas rápidas desde consumidores sin replicar el stream completo del tercero.

#### `[SI7]` Verificación opcional de MX record para correos

Al capturar un `CorreoElectronico`, opcionalmente el sistema puede verificar que el dominio tenga un registro MX válido. La verificación no se hace al construir el VO (porque implica I/O y no debe bloquear el alta), sino como paso asíncrono posterior. Si falla, el correo se marca como "pendiente de verificación" — no se rechaza. Es una **feature opcional habilitable por configuración de tenant** — no forma parte del comportamiento obligatorio del modelo de dominio.

#### `[SI8]` Advertencia UX para contactos sin nombre

Cuando un contacto se registra desde un proceso de recepción automatizada (SincoRE, importación masiva, documento de soporte) sin nombre, la UI debe presentar advertencia visual en la ficha del contacto e incentivar al administrador a completarlo. Detalle de UX fuera del modelo de dominio; esta sugerencia queda como contrato entre el dominio y la capa de presentación.

#### `[SI9]` Forma canónica de la razón social para detección de duplicados

Al procesar comandos que registran o actualizan la identificación o razón social de un tercero, el sistema deriva una **forma canónica** de la `razonSocial` aplicando en orden:

1. Conversión a minúsculas (`"ACME" → "acme"`).
2. Eliminación de tildes y diacríticos (`"José Pérez" → "jose perez"`).
3. Eliminación de signos de puntuación (`"acme s.a.s." → "acme sas"`).
4. Colapso de espacios múltiples a un solo espacio.
5. Recorte de espacios al inicio y al final.

La forma canónica **no se persiste** — es un derivado que el agregado calcula al vuelo para aplicar `[I11]`. El stream y las proyecciones conservan la razón social original tal como la capturó el operador o consumidor.

La normalización no pretende ser igualdad semántica — es un filtro contra errores operativos comunes (mayúsculas, tildes, puntuación, espacios). No resuelve abreviaturas (`"S.A."` vs `"Sociedad Anónima"`) ni omisión de apellidos — esos quedan como falsos negativos aceptables, cubiertos por la válvula `[SI11]`.

#### `[SI10]` Comando idempotente `AsegurarTerceroDesdeConsumidor`

Comando emitido por dominios consumidores automáticos (OXP, CXC, RRHH, Tesorería, importaciones masivas) y por integraciones externas (SincoRE, documento de soporte). Semántica: *"garantiza que el tercero con esta identificación existe con al menos esta forma"*. Ver `[D9]`.

**Ubicación de la lógica del comando.** El flujo de decisión descrito abajo (lookup, detección de posible duplicado, enriquecer vs crear) reside en el **application service** que carga el agregado Tercero — no es un domain service del BC, que no tiene ninguno (Sección 3.5). El application service consulta la proyección de unicidad (`[SI1]`), delega los guards al agregado y apendea los eventos resultantes al stream en un único commit atómico.

**Flujo de validación:**

1. **Lookup por clave primaria `(tipoDocumento, numero, pais)`:**
   - Coincide → tomar como canónico, saltar al paso 3.
   - No coincide → paso 2.
2. **Lookup por número:** buscar terceros con el mismo `numero` y distinta combinación `(tipoDocumento, pais)`:
   - Candidato con razón social canónica coincidente (`[SI9]`) → **rechazar** con `{ causa: PosibleDuplicadoDetectado, terceroIdCandidato }`. El consumidor reintenta con la identificación del candidato o escala a humano. No se crea nada.
   - Sin candidato coincidente → paso 4 (crear).
3. **Enriquecer tercero existente (idempotente):**
   - Identidad (`tipoPersona`, `razonSocial`, `DV`): existente es **autoritativo**; divergencias se ignoran silenciosamente. Los cambios de identidad solo se procesan por comandos explícitos (`ActualizarRazonSocial`, `ActualizarTipoPersona`, `ActualizarIdentificacion`).
   - **Rol:** si el valor del enum ∉ roles activos → emitir `TerceroRolAsignado`. Si ya está activo → no-op.
   - **Contacto:** si el `rolContacto` ∉ contactos activos → emitir `ContactoRegistrado`. Si ya existe un contacto activo con ese `rolContacto` → ignorar el contacto completo (medios incluidos). Complementar medios del contacto existente es responsabilidad del flujo manual (`ContactoActualizado`).
   - **Dirección:** si el `tipoUso` ∉ direcciones activas → emitir `TerceroDireccionReferenciada`. Si ya existe una activa con ese `tipoUso` → no-op.
4. **Crear tercero nuevo:** el payload debe traer los mínimos que exigen los invariantes al nacimiento del agregado: identificación + razón social + `tipoPersona` + al menos un rol + contacto principal con correo y teléfono (`[I4]`). Si falta alguno → rechazar con `{ causa: MinimosParaCreacionIncompletos, falta: [...] }`. Si están → emitir `TerceroRegistrado` con `origen=DesdeConsumidor` y `contextoOrigen={consumidor, referenciaExterna?}` — el tercero queda en `EnRegistro`. La `ReferenciaDireccion` con `tipoUso=Fiscal` **no es parte del payload** ni condición de nacimiento del agregado: la dirección la crea el servicio de Direcciones en paralelo y el tercero transiciona a `Activo` vía `TerceroActivado` cuando Direcciones confirma (ver `[D13]`).

**Claves de deduplicación (paso 3):**

Estas claves aplican **únicamente al flujo automático de este comando**. No son restricciones estructurales del agregado: el modelo permite múltiples contactos activos con el mismo `rolContacto` y múltiples direcciones con el mismo `tipoUso` (ver catálogo § 6.2 y composición § 3.3.2). La deduplicación del comando es una heurística para evitar crear duplicados desde integraciones automáticas; los flujos manuales (`RegistrarContacto`, `ReferenciarDireccion`) no la aplican y pueden crear entidades adicionales cuando el negocio lo justifique.

| Componente | Clave | Scope |
|------------|-------|-------|
| Rol | Valor del enum | Solo roles activos. Un rol removido puede re-asignarse. |
| Contacto | `rolContacto` | Solo contactos `Activo`. Un contacto inactivo con ese rol no bloquea agregar uno nuevo. |
| Dirección | `tipoUso` | Solo direcciones referenciadas activamente. |

**Respuesta síncrona:** `{ terceroId, piezasAgregadas[], piezasYaExistentes[] }`. El comando es determinista e idempotente: la deduplicación técnica de mensajes la garantiza la plataforma vía `idempotencyKey` (`[D11]`), y los guards de deduplicación lógica (rol/contacto/dirección) garantizan que reintentos con el mismo payload producen 0 eventos nuevos incluso sin la capa de infraestructura. Todos los eventos emitidos se appendean en un único commit atómico al stream del tercero; si el append falla, ningún evento persiste. No se emite un evento de confirmación adicional; los eventos atómicos emitidos (o su ausencia) son la fuente de verdad. Los reintentos son seguros por construcción — si el primer intento llegó al stream, el segundo observa el estado y produce no-op; si no llegó, el segundo actúa.

#### `[SI11]` Válvula de registro forzado `RegistrarTerceroForzado`

Comando alternativo a `RegistrarTercero` para el caso en que `[I11]` detecta un posible duplicado pero el operador afirma que es un registro legítimo. Caso canónico: persona con doble nacionalidad que tiene documentos legítimos con el mismo número en países distintos.

**Diferencias con `RegistrarTercero`:**

- Solo valida `[I1]` (clave primaria exacta sigue siendo estricta).
- No aplica la validación de `[I11]`.
- Exige `motivoRegistroForzado` (texto libre obligatorio) que queda capturado en el payload del evento `TerceroRegistrado` como atributo opcional.
- Requiere permiso específico del operador (declarado en Sección 12).

Este comando **nunca** lo invocan consumidores automáticos ni `AsegurarTerceroDesdeConsumidor` — siempre es ejecución manual por un operador humano autorizado. El consumidor automático, ante un posible duplicado, siempre rechaza y escala.

### 3.5. Relaciones y referencias externas

El sub-dominio de Terceros tiene **un único agregado raíz** (`Tercero`). No hay:

- Relaciones entre agregados internos del BC (solo existe un agregado).
- Entidades espejo con otros agregados.
- Domain services (la coordinación multi-dominio vive externamente; ver `[D1]` y el `anexo-decision-orquestacion-registro.md`).

Las referencias que el agregado mantiene hacia elementos externos al sub-dominio se resumen a continuación:

| Elemento referenciado | Origen | Propósito |
|-----------------------|--------|-----------|
| `Identificacion.tipoDocumento` | Catálogo de tipos de documento (Datos de Referencia — `compartido/datos-referencia/catalogos/tipos-documento-identidad.json`) | Validar la identificación del tercero `[R03]` y obtener las reglas de formato y algoritmo del DV `[R04]`. |
| `Identificacion.pais` | Catálogo de países (Datos de Referencia — `compartido/datos-referencia/catalogos/paises.json`) | Validar el país emisor de la identificación. |
| `ReferenciaDireccion.direccionId` | Servicio de Direcciones | Referenciar una dirección sin almacenar su contenido `[R21]` (ver `[D4]`). |
| `Contacto.rolContacto` | Catálogo de tipos de contacto (interno del dominio, ver Sección 6) | Clasificar el rol del contacto dentro del tercero. |
| `Tercero.roles` (set) | Catálogo de roles (interno del dominio, ver Sección 6) | Clasificar los roles universales que cumple el tercero en el ERP. |

**Consumidores externos** del agregado Tercero (Impuestos, OXP, CXC, RRHH, Tesorería, Emisión Electrónica) no son relaciones del modelo de dominio — son dominios que se suscriben a los eventos de Terceros y mantienen su propia vista local. La frontera con esos dominios está documentada en la Sección 3.1.

---

## 4. Máquinas de estado

### 4.1. Tercero — FSM

```
                 TerceroRegistrado
                        │
                        ▼
    ┌─────────────────────────────────────────────────┐
    │                  EnRegistro                      │
    │                                                  │
    │   Identidad registrada.                          │
    │   Pendiente confirmación de dirección fiscal     │
    │   por el servicio de Direcciones.                │
    │   No operable por dominios consumidores.         │
    │                                                  │
    │   Eventos de progreso (sin cambio de estado):    │
    │    · TerceroRolAsignado / TerceroRolRemovido     │
    │    · ContactoRegistrado / ContactoActualizado    │
    │    · ContactoPrincipalDesignado                  │
    │      (roles y contactos se pueden ajustar        │
    │       mientras se espera la activación)          │
    └─────┬────────────────────────────┬───────────────┘
          │                            │
   TerceroActivado           TerceroRegistroAbortado
   (Direcciones confirmó     (retries agotados;
    la dirección fiscal)      fallo permanente)
          │                            │
          ▼                            ▼
    ┌──────────────────────────┐   ┌──────────────┐
    │         Activo           │   │   Abortado   │ (terminal)
    │                          │   │              │
    │ Eventos de progreso      │   │ Estado       │
    │ (sin cambio de estado):  │   │ terminal.    │
    │  · TerceroIdentificacion-│   │ Identificación│
    │    Actualizada           │   │ queda        │
    │  · TerceroRazonSocial-   │   │ disponible   │
    │    Actualizada           │   │ para nuevo   │
    │  · TerceroTipoPersona-   │   │ intento      │
    │    Actualizado           │   │ (ver [SI1]). │
    │  · TerceroRolAsignado    │   │              │
    │  · TerceroRolRemovido    │   └──────────────┘
    │  · TerceroDireccion-     │
    │    Referenciada          │
    │  · TerceroDireccion-     │
    │    Desreferenciada       │
    │  · TerceroDireccion-     │
    │    PreferidaDesignada    │
    │  · (Componente Contacto  │
    │    — ver FSM 4.2)        │
    └────┬────────────────▲────┘
         │                │
  TerceroInactivado    TerceroReactivado
         │                │
         ▼                │
    ┌─────────────────────────┐
    │       Inactivo          │
    │                         │
    │  Sin eventos de         │
    │  contenido. Solo        │
    │  reactivación.          │
    └─────────────────────────┘
```

**Notas:**

- `EnRegistro` es el estado inicial tras `TerceroRegistrado` `[R16]`. En este estado el tercero aún **no es operable** — los dominios consumidores no deben actuar sobre terceros en EnRegistro.
- `EnRegistro` admite configurar roles y contactos (para preparar la identidad completa mientras se espera la confirmación de la dirección fiscal). No admite actualizaciones de identidad (razón social, tipo persona, identificación) — esas aplican solo en `Activo`.
- `TerceroActivado` se emite al consumir la confirmación asíncrona del servicio de Direcciones de que la dirección fiscal fue creada. Ver `[D13]`.
- `TerceroRegistroAbortado` se emite cuando la política de retries de la plataforma (`[D11]`) se agota sin confirmación. La identificación queda disponible para un nuevo intento con otro `terceroId` (ver `[SI1]`).
- `Activo` es el estado operable del tercero. Los eventos del componente interno `Contacto` se documentan en la FSM 4.2.
- `Inactivo` **no acepta eventos de contenido**. Se conserva el estado histórico intacto `[R17]` y solo puede transicionar de vuelta a `Activo` vía `TerceroReactivado`.
- `Abortado` es **estado terminal**. No es reactivable. Si el operador quiere reintentar el registro, crea un nuevo tercero (nuevo `terceroId`, nuevo stream).
- La validación del estado para operaciones nuevas es responsabilidad del consumidor `[R18]`; Terceros no la aplica.

### 4.2. Contacto — FSM

Máquina de estados del componente interno `Contacto`, independiente de la del agregado Tercero pero embebida en su stream.

```
                ContactoRegistrado
                        │
                        ▼
    ┌─────────────────────────────────────────────────┐
    │                    Activo                        │
    │                                                  │
    │  Eventos de progreso (sin cambio de estado):     │
    │   · ContactoActualizado                          │
    │   · ContactoPrincipalDesignado                   │
    │     (afecta a dos contactos: designa al nuevo    │
    │      y desmarca al anterior — ver [D7])          │
    └─────┬─────────────────────────────────────▲──────┘
          │                                     │
ContactoInactivado                    ContactoReactivado
 (si principal:                                  │
  requiere reemplazo                             │
  previo — ver [R15])                            │
          │                                     │
          ▼                                     │
    ┌─────────────────────────────────────────────────┐
    │                  Inactivo                        │
    │                                                  │
    │  Sin eventos de contenido. Solo reactivación.    │
    └──────────────────────────────────────────────────┘
```

**Notas:**

- `Activo` es el estado inicial tras `ContactoRegistrado`.
- En `Activo` el contacto acepta actualizaciones (`ContactoActualizado`) y designación como principal (`ContactoPrincipalDesignado`). Este último afecta dos contactos en un mismo evento: el nuevo principal y el anterior (ver `[D7]`).
- **Precondición para `ContactoInactivado`:** si el contacto es el principal, debe haberse designado previamente otro contacto activo como principal `[R15]`. No se permite inactivar al principal sin reemplazo. Cuando se inactiva al principal, `ContactoPrincipalDesignado` (para el reemplazo) y `ContactoInactivado` (para el saliente) se appendean al stream en un único commit atómico para que `[I4]` nunca se observe violada.
- `Inactivo` solo transiciona a `Activo` vía `ContactoReactivado`. El contacto reactivado **no recupera automáticamente** la marca de principal — para volver a serlo requiere un nuevo `ContactoPrincipalDesignado`.
- **Ningún estado es terminal.** Los contactos nunca se eliminan — la inactivación es reversible.
- La FSM del Contacto vive dentro del agregado Tercero; sus transiciones se ejecutan vía comandos sobre el Tercero (que enruta al contacto correspondiente).

---

## 5. Catálogo de eventos

18 eventos totales, organizados por tema funcional: Identidad (4), Estado (4), Roles (2), Direcciones (3), Contactos (5).

> **Nota sobre timestamps:** El timestamp de ocurrencia de cada evento vive en la metadata del stream de Event Sourcing, no en el payload. Los payloads descritos a continuación solo incluyen los datos de negocio.

### 5.1. Eventos de identidad

#### `TerceroRegistrado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se registró un nuevo tercero en el sistema con su identidad base, roles iniciales y contacto principal. El tercero queda en estado `EnRegistro` — identidad registrada pero aún no operable. La activación (transición a `Activo`) ocurre tras la confirmación asíncrona de la dirección fiscal por el servicio de Direcciones, vía `TerceroActivado` (ver `[D13]`). El contacto principal se crea como parte de este evento — no se emite un `ContactoRegistrado` separado. Evento de nacimiento del agregado. |
| **Causalidad** | Directa (comando `RegistrarTercero`). |
| **Agregado** | Tercero |
| **Estado previo** | — (no existe) |
| **Estado resultante** | Tercero `EnRegistro` |
| **Precondiciones** | `tipoDocumento + numero + pais` únicos en el sistema `[R01]` `[I1]`; razón social canónica distinta si el `numero` coincide con otro tercero con distinta combinación `(tipoDocumento, pais)` `[I11]`; `tipoDocumento` existe en el catálogo para el `pais` `[R03]`; `numero` cumple el formato `[R04]`; al menos un rol asignado `[R07]`; contacto principal con correo y teléfono `[R15]`; DV calculado o validado vía `[SI2]`. Si el comando es `RegistrarTerceroForzado`, no se aplica la validación de `[I11]` y se exige `motivoRegistroForzado` no vacío (`[SI11]`). La exigencia de dirección fiscal `[R25]` `[I6]` se verifica al transicionar a `Activo` vía `TerceroActivado`, no en este evento. |
| **Información capturada** | `terceroId` (UUID); `Identificacion` { tipoDocumento, numero, pais }; `digitoVerificacion` (opcional); `tipoPersona` (Persona / Organizacion); `razonSocial`; `roles` (set); `contactoPrincipalInicial` { contactoId, nombre?, rolContacto, correos, telefonos, esPrincipal=true, estado=Activo }; `origen` (enum: Manual, DesdeConsumidor, ImportacionMasiva, SincoRE, DocumentoDeSoporte, Otro); `contextoOrigen` (objeto opcional según `origen`); `motivoRegistroForzado` (opcional; obligatorio solo si el comando fue `RegistrarTerceroForzado`, ver `[SI11]`); `usuarioId` (opcional). |
| **Efectos** | Crea el agregado en estado `EnRegistro`; materializa el contacto principal inicial; actualiza índice de unicidad `[SI1]` y proyección de contacto principal `[SI6]`. **No notifica aún a dominios consumidores** — la notificación ocurre en `TerceroActivado` para garantizar que solo se abren agregados de rol para terceros operables. |

#### `TerceroIdentificacionActualizada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Cambió al menos uno de los atributos de la clave técnica de identidad: tipo de documento, número, país o DV. |
| **Causalidad** | Directa (comando `ActualizarIdentificacion`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `Activo` |
| **Estado resultante** | Tercero `Activo` (progreso) |
| **Precondiciones** | La nueva combinación `tipoDocumento + numero + pais` es única `[R01]`; nuevo `tipoDocumento` existe en el catálogo `[R03]`; `numero` cumple formato `[R04]`; DV validado o calculado si aplica. |
| **Información capturada** | `terceroId`; `Identificacion` { tipoDocumento, numero, pais } con valores nuevos; `digitoVerificacion` nuevo (opcional); `motivo` (texto libre, opcional); `usuarioId` (opcional). |
| **Efectos** | Actualiza la identidad del agregado; notifica a dominios consumidores `[R24]`; actualiza índice de unicidad `[SI1]` y proyección de historial `[SI4]`. |

#### `TerceroRazonSocialActualizada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Cambió la razón social del tercero (nombre legal para organizaciones, nombre para personas naturales). No afecta la clave técnica. |
| **Causalidad** | Directa (comando `ActualizarRazonSocial`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `Activo` |
| **Estado resultante** | Tercero `Activo` (progreso) |
| **Precondiciones** | Tercero está Activo; `razonSocial` no vacía; nueva razón social ≠ actual. |
| **Información capturada** | `terceroId`; `razonSocial` (nuevo valor); `motivo` (texto libre, opcional); `usuarioId` (opcional). |
| **Efectos** | Actualiza la razón social del agregado; notifica a dominios consumidores `[R24]`; proyección de historial `[SI4]`. |

#### `TerceroTipoPersonaActualizado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Cambió la clasificación base del tercero entre persona (individuo) y organización (entidad constituida). Tiene impacto tributario — Impuestos revalida el perfil tributario. |
| **Causalidad** | Directa (comando `ActualizarTipoPersona`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `Activo` |
| **Estado resultante** | Tercero `Activo` (progreso) |
| **Precondiciones** | Tercero está Activo; `tipoPersona` nuevo ≠ actual. |
| **Información capturada** | `terceroId`; `tipoPersona` (nuevo valor); `motivo` (texto libre, opcional); `usuarioId` (opcional). |
| **Efectos** | Actualiza el tipo de persona del agregado; notifica a Impuestos para revalidación del perfil tributario `[R24]`; notifica a otros consumidores; proyección de historial `[SI4]`. |

### 5.2. Eventos de estado

#### `TerceroActivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El tercero completó su registro: el servicio de Direcciones confirmó asincrónicamente la creación de la dirección fiscal. El tercero pasa de `EnRegistro` a `Activo` y queda disponible para operaciones en los dominios consumidores. |
| **Causalidad** | Reactiva — emitida al consumir la confirmación del servicio de Direcciones (evento de Direcciones que informa la creación de la dirección fiscal vinculada al `terceroId`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `EnRegistro` |
| **Estado resultante** | Tercero `Activo` |
| **Precondiciones** | Tercero está en estado `EnRegistro`; existe una `ReferenciaDireccion` con `tipoUso = Fiscal` vinculada al tercero en el servicio de Direcciones `[R25]` `[I6]`. |
| **Información capturada** | `terceroId`; `direccionIdFiscal` (referencia a la dirección creada en Direcciones); `fechaActivacion`. |
| **Efectos** | Transiciona a estado `Activo`. Añade la `ReferenciaDireccion` fiscal a la colección de direcciones del agregado. **Notifica a dominios consumidores** según los roles asignados (Proveedor→OXP, Cliente→CXC, Empleado→RRHH, etc.) para que abran su registro del tercero en ese contexto `[R09]` `[R24]`. Ver `[D13]`. |

#### `TerceroRegistroAbortado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El registro del tercero no pudo completarse por fallo permanente en el servicio de Direcciones tras agotar los reintentos automáticos. El tercero queda en estado terminal `Abortado` — nunca operó en el ERP. |
| **Causalidad** | Política de plataforma (Marten + Wolverine vía `[D11]`) tras agotar retries de la creación de la dirección fiscal. |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `EnRegistro` |
| **Estado resultante** | Tercero `Abortado` (terminal) |
| **Precondiciones** | Tercero está en estado `EnRegistro`; la política de reintentos de la plataforma se agotó sin confirmación de la dirección fiscal. |
| **Información capturada** | `terceroId`; `causa` (enum: `DireccionesRechazo`, `DireccionesTimeout`, `DireccionesNoDisponible`, `Otro`); `servicioFallido` (default: `Direcciones`); `ultimoError` (texto libre); `intentos` (entero). |
| **Efectos** | Transiciona a `Abortado` (terminal — no reactivable). **La identificación del tercero queda disponible** para un nuevo intento de registro con otro `terceroId` (el índice de unicidad excluye Abortados, ver `[SI1]`). **No notifica a dominios consumidores** — el tercero nunca operó. El stream se conserva como evidencia histórica del intento fallido. Ver `[D13]`. |

#### `TerceroInactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El tercero ha sido inactivado; deja de poder usarse en nuevas operaciones, pero sus registros históricos se conservan intactos. |
| **Causalidad** | Directa (comando `InactivarTercero`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `Activo` |
| **Estado resultante** | Tercero `Inactivo` |
| **Precondiciones** | Tercero está en estado `Activo`. |
| **Información capturada** | `terceroId`; `motivo` (texto libre, opcional — ej: "cierre de relación comercial", "solicitud del tercero"); `usuarioId` (opcional). |
| **Efectos** | Transiciona a estado Inactivo `[R17]`; notifica a dominios consumidores `[R24]` para que impidan nuevas operaciones `[R18]`. Los registros históricos y las referencias en otros dominios se conservan intactos. |

#### `TerceroReactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El tercero ha sido reactivado tras una inactivación previa. Vuelve a estar disponible para nuevas operaciones. |
| **Causalidad** | Directa (comando `ReactivarTercero`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `Inactivo` |
| **Estado resultante** | Tercero `Activo` |
| **Precondiciones** | Tercero está en estado `Inactivo` `[R19]`; conserva al menos una `ReferenciaDireccion` con `tipoUso = Fiscal` para cumplir `[I6]` al volver a `Activo` (se cumple automáticamente porque la inactivación no desreferencia direcciones). |
| **Información capturada** | `terceroId`; `motivo` (texto libre, opcional — ej: "retoma de relación comercial"); `usuarioId` (opcional). |
| **Efectos** | Transiciona a estado Activo; notifica a dominios consumidores `[R24]` para habilitar nuevamente su uso en operaciones. |

### 5.3. Eventos de roles

#### `TerceroRolAsignado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se asignó un nuevo rol al tercero. Si el tercero está en `Activo`, el dominio consumidor correspondiente (OXP para Proveedor, CXC para Cliente, RRHH para Empleado, etc.) reacciona abriendo su propio registro del tercero en ese contexto de inmediato. Si el tercero está en `EnRegistro`, el rol queda asignado en el agregado y la apertura del registro del consumidor se difiere hasta la activación (`TerceroActivado`). |
| **Causalidad** | Directa (comando `AsignarRolTercero`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `EnRegistro` o `Activo` |
| **Estado resultante** | sin cambio de estado (progreso) |
| **Precondiciones** | Tercero está en `EnRegistro` o `Activo`; `rol` es un valor válido del enum (Proveedor, Cliente, Empleado, EntidadFinanciera, Otro); `rol` no está ya presente en la colección de roles del tercero. |
| **Información capturada** | `terceroId`; `rol` (valor del enum); `motivo` (texto libre, opcional); `usuarioId` (opcional). |
| **Efectos** | Agrega el rol al set `roles` del agregado. La notificación al dominio consumidor del rol para que abra su registro `[R09]` `[R24]` ocurre de inmediato si el tercero está `Activo`, o se difiere hasta la activación si el tercero está en `EnRegistro`. |

#### `TerceroRolRemovido`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se removió un rol del tercero. Los registros históricos asociados al rol en el dominio consumidor se conservan intactos; solo se impide la creación de nuevas operaciones bajo ese rol `[R10]`. |
| **Causalidad** | Directa (comando `RemoverRolTercero`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `EnRegistro` o `Activo` |
| **Estado resultante** | sin cambio de estado (progreso) |
| **Precondiciones** | Tercero está en `EnRegistro` o `Activo`; `rol` está actualmente asignado al tercero; **`rol` no es el único rol del tercero** — la remoción del último rol se rechaza; primero debe asignarse otro rol `[R07]`. |
| **Información capturada** | `terceroId`; `rol` (valor del enum); `motivo` (texto libre, opcional); `usuarioId` (opcional). |
| **Efectos** | Remueve el rol del set `roles` del agregado. Si el tercero está `Activo`, notifica al dominio consumidor `[R24]`: el consumidor aplica su política — si hay registros históricos asociados, **cierra** el agregado del tercero en ese rol (no elimina, para preservar trazabilidad `[R10]` y auditoría fiscal); si el agregado no tiene registros asociados, puede decidir eliminarlo internamente según su propia regla. Si el tercero está en `EnRegistro`, la remoción afecta solo la configuración interna del agregado — no hay consumidor notificado porque el registro del rol nunca se abrió. |

### 5.4. Eventos de direcciones

#### `TerceroDireccionReferenciada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se agregó una nueva referencia a dirección al tercero (con un `tipoUso` específico). La dirección debe existir en el servicio de Direcciones; Terceros solo guarda el identificador. |
| **Causalidad** | Directa (comando `ReferenciarDireccionTercero`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `Activo` |
| **Estado resultante** | Tercero `Activo` (progreso) |
| **Precondiciones** | Tercero está `Activo`; `direccionId` no está ya referenciado por este tercero; `tipoUso` es válido del enum; la dirección existe en el servicio de Direcciones (validación externa). |
| **Información capturada** | `terceroId`; `direccionId`; `tipoUso` (enum: Fiscal, Comercial, Correspondencia, Otro); `motivo` (texto libre, opcional); `usuarioId` (opcional). |
| **Efectos** | Agrega la `ReferenciaDireccion` al agregado. Si es la primera referencia del `tipoUso` en el tercero, se marca automáticamente como `esPreferida = true`; si ya existe otra referencia del mismo `tipoUso`, la nueva nace con `esPreferida = false` (para cambiar la preferida se usa `TerceroDireccionPreferidaDesignada`). Notifica a dominios consumidores `[R24]`. |

#### `TerceroDireccionDesreferenciada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se removió una referencia a dirección del tercero. La dirección en sí (en el servicio de Direcciones) no se afecta — solo se elimina el vínculo con este tercero. |
| **Causalidad** | Directa (comando `DesreferenciarDireccionTercero`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `Activo` |
| **Estado resultante** | Tercero `Activo` (progreso) |
| **Precondiciones** | Tercero está `Activo`; la referencia existe en el agregado; **si la referencia es la única con `tipoUso = Fiscal` en el tercero, se rechaza** `[R25]` — debe referenciarse otra fiscal primero; si es `esPreferida = true` y hay otras referencias del mismo `tipoUso`, debe haberse designado previamente otra como preferida mediante `TerceroDireccionPreferidaDesignada`. |
| **Información capturada** | `terceroId`; `direccionId`; `tipoUso`; `motivo` (texto libre, opcional); `usuarioId` (opcional). |
| **Efectos** | Remueve la `ReferenciaDireccion` del agregado. Si era la única del `tipoUso` no fiscal, el tercero queda sin referencia para ese tipo (permitido; solo la fiscal es obligatoria). Notifica a dominios consumidores `[R24]`. |

#### `TerceroDireccionPreferidaDesignada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se designó una nueva referencia como preferida dentro de un `tipoUso`, desmarcando simultáneamente la preferida anterior (si existía). Evento análogo a `ContactoPrincipalDesignado` para contactos. |
| **Causalidad** | Directa (comando `DesignarDireccionPreferida`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `Activo` |
| **Estado resultante** | Tercero `Activo` (progreso) |
| **Precondiciones** | Tercero está `Activo`; ambas referencias (`direccionIdNueva` y, si existe, `direccionIdAnterior`) pertenecen al agregado y son del mismo `tipoUso`; `direccionIdNueva` ≠ `direccionIdAnterior`; la referencia nueva no es ya la preferida. |
| **Información capturada** | `terceroId`; `direccionIdNueva` (pasa a `esPreferida = true`); `direccionIdAnterior` (opcional; pasa a `esPreferida = false`). Es `null` únicamente cuando, al momento de la designación, no existía ninguna dirección con `esPreferida = true` para ese `tipoUso`. Escenario posible solo en `tipoUso` no-fiscales y únicamente si la anterior preferida fue desreferenciada sin designar otra (`[I7]` no obliga a mantener una preferida, solo prohíbe que haya dos simultáneas); `tipoUso`; `motivo` (texto libre, opcional); `usuarioId` (opcional). |
| **Efectos** | Actualiza la marca `esPreferida` en ambas referencias simultáneamente. Mantiene el invariante del agregado: como mucho una preferida por `tipoUso`. Notifica a dominios consumidores `[R24]`. |

### 5.5. Eventos de contactos

#### `ContactoRegistrado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se registró un nuevo contacto asociado al tercero, en estado `Activo`. El contacto nace con `esPrincipal = false` — nunca viola `[I4]` por construcción. Para designarlo como principal se emite posteriormente `ContactoPrincipalDesignado` (pueden appendarse ambos eventos en el mismo commit atómico si el comando así lo orquesta). |
| **Causalidad** | Directa (comando `RegistrarContacto`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `EnRegistro` o `Activo` |
| **Estado resultante** | Contacto `Activo` (agregado al Tercero) |
| **Precondiciones** | Tercero está en `EnRegistro` o `Activo`; `rolContacto` es válido del catálogo (Sección 6); al menos un medio de comunicación (correo o teléfono) `[R13]`; en las colecciones recibidas, como mucho un `CorreoElectronico` con `preferido = true` y como mucho un `Telefono` con `preferido = true` `[I9]`. |
| **Información capturada** | `terceroId`; `contactoId` (UUID); `nombre` (opcional); `rolContacto`; `correos` (colección 0..N de `CorreoElectronico`); `telefonos` (colección 0..N de `Telefono`); `usuarioId` (opcional). |
| **Efectos** | Agrega el Contacto al agregado Tercero con `esPrincipal = false` y `estado = Activo`. Notifica a dominios consumidores que utilizan contactos (ej: Emisión Electrónica si es representante legal, Tesorería si es contacto de facturación) `[R24]`. |

#### `ContactoActualizado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se actualizó uno o más atributos del contacto: nombre, rol, correos o teléfonos. No cambia la marca `esPrincipal` ni el estado del contacto. |
| **Causalidad** | Directa (comando `ActualizarContacto`). |
| **Agregado** | Tercero |
| **Estado previo** | Contacto `Activo` |
| **Estado resultante** | Contacto `Activo` (progreso) |
| **Precondiciones** | Tercero está `Activo`; contacto existe en el agregado y está `Activo`; al menos un medio de comunicación después de la actualización `[R13]`; si el contacto es principal, debe tener al menos un correo y un teléfono tras la actualización `[R15]`; en las colecciones recibidas, como mucho un `CorreoElectronico` con `preferido = true` y como mucho un `Telefono` con `preferido = true` `[I9]`. |
| **Información capturada** | `terceroId`; `contactoId`; `nombre` (nuevo, opcional); `rolContacto` (nuevo, opcional); `correos` (nueva colección, opcional); `telefonos` (nueva colección, opcional); `usuarioId` (opcional). |
| **Efectos** | Actualiza los atributos modificados del contacto en el agregado. Notifica a dominios consumidores `[R24]`. Si es el contacto principal, actualiza la proyección `[SI6]`. |

#### `ContactoInactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se inactivó un contacto del tercero. Deja de usarse en nuevas comunicaciones; se conserva en el agregado para trazabilidad. |
| **Causalidad** | Directa (comando `InactivarContacto`). |
| **Agregado** | Tercero |
| **Estado previo** | Contacto `Activo` |
| **Estado resultante** | Contacto `Inactivo` |
| **Precondiciones** | Tercero está `Activo`; contacto existe en el agregado y está `Activo`; **si el contacto es el principal, debe haberse designado previamente otro contacto activo como principal** mediante `ContactoPrincipalDesignado` `[R15]`. Cuando se inactiva al contacto principal, `ContactoPrincipalDesignado` (para el reemplazo) y `ContactoInactivado` (para el saliente) se appendean al stream en un **único commit atómico** para que `[I4]` nunca se observe violada entre ambos eventos. |
| **Información capturada** | `terceroId`; `contactoId`; `motivo` (texto libre, opcional); `usuarioId` (opcional). |
| **Efectos** | Transiciona el contacto a estado `Inactivo`. El contacto se conserva en el agregado; nunca se elimina. Notifica a dominios consumidores `[R24]`. |

#### `ContactoReactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se reactivó un contacto previamente inactivado. Vuelve a estar disponible para comunicaciones. |
| **Causalidad** | Directa (comando `ReactivarContacto`). |
| **Agregado** | Tercero |
| **Estado previo** | Contacto `Inactivo` |
| **Estado resultante** | Contacto `Activo` |
| **Precondiciones** | Tercero está `Activo`; contacto existe en el agregado y está `Inactivo`. |
| **Información capturada** | `terceroId`; `contactoId`; `motivo` (texto libre, opcional); `usuarioId` (opcional). |
| **Efectos** | Transiciona el contacto a estado `Activo`. **No recupera automáticamente la marca de principal** — para volver a ser principal requiere un `ContactoPrincipalDesignado` posterior. Notifica a dominios consumidores `[R24]`. |

#### `ContactoPrincipalDesignado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se designó un nuevo contacto como principal del tercero, desmarcando simultáneamente al contacto principal anterior. Evento único con ambas referencias (ver `[D7]`). |
| **Causalidad** | Directa (comando `DesignarContactoPrincipal`). |
| **Agregado** | Tercero |
| **Estado previo** | Tercero `Activo`; ambos contactos `Activos` |
| **Estado resultante** | Tercero `Activo`; ambos contactos `Activos` (progreso; cambia `esPrincipal` en dos contactos simultáneamente) |
| **Precondiciones** | Tercero está `Activo`; `contactoIdNuevo` existe en el agregado y está `Activo`; `contactoIdNuevo` tiene al menos un correo y un teléfono `[R15]`; `contactoIdNuevo` ≠ `contactoIdAnterior`; `contactoIdNuevo` aún no es el principal. |
| **Información capturada** | `terceroId`; `contactoIdNuevo` (pasa a `esPrincipal = true`); `contactoIdAnterior` (pasa a `esPrincipal = false`). En operación normal **siempre tiene valor** (`[I4]` garantiza que hay un principal activo). Solo es `null` durante reparación de datos legados o migrados que llegan en estado inconsistente — escenario operativo, no flujo normal; `motivo` (texto libre, opcional); `usuarioId` (opcional). |
| **Efectos** | Actualiza la marca `esPrincipal` en ambos contactos simultáneamente en un solo evento. Mantiene el invariante del agregado: exactamente un contacto activo con `esPrincipal = true`. Notifica a dominios consumidores `[R24]`. Actualiza proyección de contacto principal `[SI6]`. |

---

## 6. Catálogos del dominio

Catálogos propios del sub-dominio que tipifican elementos del modelo. Excluye catálogos externos (tipos de documento, países — gestionados por Datos de Referencia) y enums estructurales menores (`tipoPersona`, `tipoUso` de dirección, estados `Activo`/`Inactivo`, `origen` del registro) que se documentan inline en la composición y en los eventos.

### 6.1. Catálogo de roles

Roles universales que puede cumplir un tercero. Se aplican por igual a todos los países — no varían por jurisdicción `[R08]`. Se usan en el atributo `roles` del agregado Tercero (colección; un tercero puede tener varios simultáneamente `[R07]`).

| Valor | Descripción | Dominio consumidor dueño |
|-------|-------------|--------------------------|
| `Proveedor` | El tercero es proveedor de bienes o servicios. | OXP |
| `Cliente` | El tercero es cliente al que se le emiten facturas. | CXC |
| `Empleado` | El tercero es empleado de la organización (con vínculo laboral). | RRHH |
| `EntidadFinanciera` | El tercero es una entidad financiera (banco, cooperativa). | Tesorería |
| `Otro` | Rol adicional no tipificado explícitamente (ej: socio, miembro de junta directiva). El detalle operativo queda en el dominio consumidor que lo utilice. | Según caso |

**Reglas de uso:**

- Al asignar un rol (evento `TerceroRolAsignado`), el dominio consumidor abre su propio registro del tercero en ese contexto cuando el tercero está `Activo`; si se asignó mientras estaba en `EnRegistro`, la apertura se difiere hasta la activación `[R09]`.
- No se pueden eliminar valores del catálogo — es universal y estable. Nuevos valores se agregan solo por decisión arquitectónica.

### 6.2. Catálogo de tipos de contacto

Roles que pueden tener los contactos de un tercero. Universales — no varían por país. Se usan en el atributo `rolContacto` del componente interno `Contacto`.

| Valor | Descripción | Usado por |
|-------|-------------|-----------|
| `RepresentanteLegal` | Persona con facultad legal para firmar a nombre del tercero. | Emisión Electrónica (firma de documentos), legal. |
| `Tesorero` | Persona responsable del área de tesorería/finanzas del tercero. | Tesorería, OXP, CXC. |
| `Comercial` | Persona comercial o de ventas del tercero. | CXC, OXP, área comercial. |
| `Tecnico` | Persona técnica/operativa del tercero. | Áreas operativas. |
| `ContactoDeFacturacion` | Persona que recibe las facturas del tercero. | OXP, CXC (facturación). |
| `ContactoDeNotificaciones` | Persona que recibe notificaciones oficiales (legales, corporativas). | Legal, operaciones. |
| `Otro` | Rol no tipificado explícitamente. El detalle queda en el dominio consumidor. | Según caso |

**Reglas de uso:**

- Un contacto tiene exactamente un `rolContacto`. Un tercero puede tener varios contactos con el mismo o con distinto rol.
- La marca `esPrincipal` es **ortogonal** al `rolContacto` — un contacto con rol `RepresentanteLegal` puede ser o no ser el principal `[R15]`.
- No se pueden eliminar valores del catálogo. Nuevos valores se agregan solo por decisión arquitectónica.

---

## 7. Invariantes del dominio

Restricciones estructurales que deben ser verdaderas en todo momento. Clasificación:

- **Local:** transaccional, se garantiza dentro del agregado Tercero al procesar un comando.
- **Eventual:** cruza fronteras (otros terceros, otros dominios, proyecciones). Se garantiza vía proyección o índice externo; la consistencia es eventual, no inmediata.

| # | Invariante | Tipo | Referencia |
|---|-----------|:----:|------------|
| **I1** | **Unicidad de la identificación:** la combinación `(tipoDocumento, numero, pais)` es única en todo el sistema. No pueden existir dos terceros con la misma identificación. | Eventual | `[R01]` `[SI1]` |
| **I2** | **Validación por catálogo de tipos de documento:** el `tipoDocumento` debe existir en el catálogo de Datos de Referencia para el `pais` informado; el `numero` cumple el formato publicado; el `digitoVerificacion` (si aplica) es válido según el algoritmo del catálogo. | Local | `[R03]` `[R04]` `[SI2]` |
| **I3** | **Al menos un rol asignado:** todo tercero activo tiene al menos un rol en la colección `roles`. Se garantiza al registrar (`TerceroRegistrado` exige al menos uno) y al remover (`TerceroRolRemovido` rechaza la remoción del último rol). | Local | `[R07]` |
| **I4** | **Contacto principal único y obligatorio:** todo tercero activo tiene exactamente un contacto con `esPrincipal = true` y `estado = Activo`, y ese contacto tiene al menos un `CorreoElectronico` y un `Telefono`. | Local | `[R15]` |
| **I5** | **Medios de comunicación mínimos por contacto:** todo contacto (principal o no) tiene al menos un `CorreoElectronico` o un `Telefono` en sus colecciones. | Local | `[R13]` |
| **I6** | **Dirección fiscal obligatoria en tercero Activo:** todo tercero en estado `Activo` tiene al menos una `ReferenciaDireccion` con `tipoUso = Fiscal` en su colección de direcciones. En estado `EnRegistro` la dirección fiscal aún no ha sido confirmada por Direcciones y esta invariante no aplica — su verificación se da al transicionar a `Activo` vía `TerceroActivado` (ver `[D13]`). | Local (al activarse) | `[R25]` `[D13]` |
| **I7** | **Unicidad de la preferida por tipo de uso:** dentro de la colección `Direcciones` del agregado, como mucho una `ReferenciaDireccion` tiene `esPreferida = true` por cada valor de `tipoUso`. | Local | Sección 3.3.2 |
| **I8** | **No duplicación de referencias a direcciones:** dentro de un mismo tercero, no pueden existir dos `ReferenciaDireccion` con el mismo `direccionId`. | Local | Sección 3.3.2 |
| **I9** | **Unicidad de medios preferidos por contacto:** dentro de un mismo contacto, como mucho un `CorreoElectronico` y como mucho un `Telefono` tienen `preferido = true`. | Local | Sección 3.3.3 / 3.3.4 |
| **I10** | **Conservación de historial:** ni los contactos inactivados, ni las direcciones desreferenciadas, ni los roles removidos, ni los terceros inactivados se eliminan del stream. El historial se conserva íntegramente. | Local | `[R10]` `[R17]` |
| **I11** | **Unicidad reforzada por número y razón social canónica:** cuando el `numero` coincide con el de otro tercero registrado con distinta combinación `(tipoDocumento, pais)`, la razón social en forma canónica (ver `[SI9]`) debe ser distinta. La única excepción es un registro ejecutado vía `RegistrarTerceroForzado` con motivo justificado (`[SI11]`). **Enforcement:** guard del comando al radicar (consulta a la proyección `[SI1]`) + proyección con constraint compuesto para detección de inconsistencias tardías. | Eventual | `[R01]` `[D10]` |

---

## 8. Qué NO contiene este documento

Lista explícita de lo que está fuera del alcance del modelo de dominio de Terceros. Cada exclusión se mapea al dominio o capa responsable.

| Concepto | Razón | Responsable / Referencia |
|----------|-------|--------------------------|
| **Contenido de direcciones** (calle, ciudad, municipio, departamento, código postal, coordenadas) | Terceros guarda solo la referencia por identificador (`ReferenciaDireccion`). El contenido vive en el servicio de Direcciones. | Servicio de Direcciones `[R21]` `[D4]` |
| **Perfil tributario** (régimen, condición de autorretenedor, gran contribuyente, clasificación tributaria detallada por país) | No es responsabilidad de Terceros. La identidad base (`tipoPersona`) se mantiene aquí; el perfil tributario completo vive en Impuestos. | Impuestos |
| **Cuentas bancarias del tercero** | No es responsabilidad de Terceros. | Tesorería (sub-dominio pendiente) |
| **Condiciones comerciales** (plazos de pago, límite de crédito, moneda, descuentos) | No es responsabilidad de Terceros. | OXP (proveedor), CXC (cliente) |
| **Datos laborales** (salario, cargo, fecha de ingreso, historial laboral, afiliaciones) | No es responsabilidad de Terceros. | RRHH |
| **Estado de completitud persistido del tercero** | Terceros no guarda un estado global de "listo para operar". La vista consolidada se compone en tiempo real por la capa BFF. Cada dominio consumidor define qué considera "tercero completo" para su contexto. | `[D5]` `[R22]` `[SI5]`, anexo de orquestación |
| **Autorización para operar** en una transacción específica | Cada dominio consumidor valida según sus propias reglas al iniciar la transacción. Terceros no autoriza ni bloquea operaciones externas. | `[R23]` (cada consumidor) |
| **Orquestación del registro completo** (identidad + direcciones + perfil tributario + condiciones comerciales) | La coordinación multi-dominio vive externamente al sub-dominio. | Capa BFF / API Composition, `anexo-decision-orquestacion-registro.md` |
| **Agregados de rol** (Proveedor, Cliente, Empleado, EntidadFinanciera) con su lógica de negocio | Los agregados de rol viven en los dominios consumidores, no en Terceros. Terceros solo mantiene el rol como tag sobre el tercero (atributo `roles`). | `[D1]`, OXP / CXC / RRHH / Tesorería |
| **Historial de identidad como dato del agregado** | El historial de cambios de identidad (razón social, tipo o número de documento, tipo de persona) no se almacena como un dato dentro del agregado. Cada cambio queda registrado en su propio evento, y el historial se obtiene consultando una vista de lectura que se alimenta de esos eventos. | `[D3]` `[SI4]` |
| **Lógica de cálculo/validación del DV** | El algoritmo es publicado por el catálogo de Datos de Referencia; Terceros solo invoca el servicio/utility que aplica el algoritmo. | `[SI2]`, Datos de Referencia |
| **Verificación asíncrona de MX de correos** | Verificación externa opcional post-captura. No se ejecuta al construir el VO. | `[SI7]` |
| **Domain services / sagas internas** | Terceros tiene un solo agregado — no hay coordinación entre agregados internos. La coordinación multi-dominio es externa. | Sección 3.1 / Sección 3.5 |
| **Actualización de identidad desde consumidores automáticos** | Los comandos `ActualizarIdentificacion`, `ActualizarRazonSocial` y `ActualizarTipoPersona` son exclusivos de operadores humanos autorizados. Los consumidores automáticos no pueden modificar la identidad de un tercero existente — solo enriquecerlo (agregar roles, contactos o direcciones faltantes). | `[SI10]`, Sección 12 |
| **Compensación ante fallos en la orquestación multi-dominio** | Cuando el registro del tercero es exitoso pero falla un paso posterior en otro dominio (ej: creación del perfil tributario en Impuestos, apertura del registro de proveedor en OXP), la compensación vive fuera de Terceros. La capa BFF / API Composition coordina el rollback o la estrategia de reintento entre dominios. | Capa BFF / API Composition, `anexo-decision-orquestacion-registro.md` |

---

## 9. Decisiones de arquitectura y diseño

| # | Decisión | Justificación | Referencia |
|---|----------|---------------|------------|
| **D1** | **Rol como atributo del tercero, no como agregado.** Los roles (Proveedor, Cliente, Empleado, EntidadFinanciera, Otro) son tags universales sobre el agregado Tercero. Los agregados de rol (el "proveedor en OXP", el "cliente en CXC", etc.) viven en los dominios consumidores, no en Terceros. | Respeta la autonomía de cada dominio consumidor — cada uno define qué significa "ser proveedor/cliente/empleado" en su propio contexto. Evita acoplar Terceros a la lógica de cada sub-dominio consumidor. Alinea con el modelo C + mecanismo B aprobado para la completitud del tercero. | Sección 3.2, `[R09]`, `anexo-decision-orquestacion-registro.md` |
| **D2** | **Contacto como componente interno** del agregado Tercero (entidad interna), no como agregado independiente. | El invariante cruzado `[R15]` (exactamente un contacto principal activo con correo y teléfono por tercero) requiere consistencia transaccional entre Tercero y sus contactos. Además, el volumen es bajo (2-3 contactos promedio por tercero) y no hay otros invariantes que justifiquen separar. | Sección 3.2, `[R15]`, `[I4]` |
| **D3** | **Historial de identidad derivado del stream de eventos**, no persistido como atributo del agregado. | El stream de eventos ya es fuente de verdad inmutable del historial. Duplicar esa información como atributo del agregado sería redundante. Las consultas se resuelven por una vista de lectura que se alimenta de los eventos. | Sección 3.2, `[SI4]`, `[R06]` |
| **D4** | **Referencias a direcciones por identificador, sin contenido.** Terceros guarda `direccionId + tipoUso + esPreferida`; el contenido (calle, ciudad, etc.) vive en el servicio de Direcciones. | Evita corromper la entidad Tercero con datos que pertenecen a otro dominio. Permite que Direcciones evolucione sin impactar Terceros (y viceversa). | Sección 3.3.2, `[R21]` |
| **D5** | **Terceros NO persiste un estado de completitud global del tercero.** No se guarda ninguna marca que indique "este tercero está completo para operar como X". | Cada dominio consumidor decide qué significa "completo" para su contexto. La vista consolidada se compone en tiempo real por la capa BFF consultando a cada dominio dueño. Persistir completitud duplicaría información y acoplaría Terceros a reglas ajenas. | Sección 3.2, `[R22]`, `[SI5]`, `anexo-decision-orquestacion-registro.md` |
| **D6** | **DV como atributo separado del VO `Identificacion`**, fuera de la clave de unicidad. El VO contiene `{ tipoDocumento, numero, pais }`; el `digitoVerificacion` vive como atributo aparte del Tercero. | El DV es un valor derivado del número (por algoritmo del catálogo), no parte natural de la identidad. La unicidad del tercero se define por `tipoDocumento + numero + pais`, no por el DV. Alinear el VO con la clave de unicidad evita ambigüedades y soporta DVs capturados manualmente (datos legados). | Sección 3.3.1, `[R01]`, `[R04]`, `[SI2]` |
| **D7** | **`ContactoPrincipalDesignado` como evento único con ambas referencias** (nuevo y anterior), en lugar de dos eventos separados. | Simplifica la semántica transaccional y garantiza en un solo append atómico el invariante `[I4]` (exactamente un contacto principal). Análogo a `TerceroDireccionPreferidaDesignada` por consistencia de patrón. | Sección 5.5, `[I4]` |
| **D8** | **`TerceroRegistrado` como evento único con atributo `origen` + `contextoOrigen`**, en lugar de eventos separados por canal. | Los registros desde distintos orígenes (manual, desde consumidor, importación masiva, SincoRE, documento de soporte) son variantes del mismo hecho de negocio. Un evento único evita proliferar eventos por canal y mantiene extensibilidad para orígenes futuros (portal de autogestión, API pública) sin romper compatibilidad. | Sección 5.1 |
| **D9** | **Comando idempotente `AsegurarTerceroDesdeConsumidor` como única vía de registro automático desde dominios consumidores.** Los dominios consumidores (OXP, CXC, RRHH, Tesorería, importaciones masivas, SincoRE) no registran terceros con `RegistrarTercero` — invocan `AsegurarTerceroDesdeConsumidor`. El comando crea el tercero si la identificación no existe, o enriquece el existente agregando solo lo faltante (roles por valor del enum, contactos por `rolContacto`, direcciones por `tipoUso`). | Elimina los duplicados que aparecen por reintentos del mismo consumidor o por datos ya aportados por otro consumidor, sin necesidad de fusiones ni marcados retroactivos. Deja al stream limpio por construcción. Resuelve el caso de uso donde un tercero ya existe como `Cliente` y otro consumidor lo aporta como `Proveedor` con su propio contacto. | `[SI10]`, `[R01]`, `[I4]`, `[I6]` |
| **D10** | **Unicidad reforzada con comparación de razón social canónica.** La clave primaria de unicidad `[I1]` sigue siendo `(tipoDocumento, numero, pais)` exacta. Un segundo nivel `[I11]` detecta duplicados no exactos: cuando el `numero` coincide con el de otro tercero ya registrado con distinta combinación `(tipoDocumento, pais)`, el sistema compara la razón social en su forma canónica (ver `[SI9]`) y rechaza el registro si coincide. | Cubre el caso frecuente de un mismo tercero registrado por error con distinto tipo de documento (CC vs NIT en Colombia es el caso canónico) o con país de emisión erróneo. Evita que la clave compuesta exacta —que es lo correcto para la llave primaria— deje pasar duplicados reales del negocio. Casos de homonimia legítima por número en países distintos se resuelven con `RegistrarTerceroForzado` (`[SI11]`). | `[I11]`, `[SI9]`, `[SI11]` |
| **D11** | **Control de concurrencia, idempotencia y trazabilidad delegados a la plataforma (Marten + Wolverine).** `expectedVersion` (control de concurrencia): garantizado por Marten a nivel del event store. `idempotencyKey` (deduplicación de mensajes): garantizado por Wolverine vía inbox/outbox pattern. `correlationId` (trazabilidad de procesos): propagado automáticamente por Wolverine en la cadena de mensajes. Este documento no especifica estos mecanismos por evento ni por comando — son garantías transversales de la plataforma de persistencia y mensajería. Si la plataforma cambia, revalidar que el nuevo stack provea estas tres garantías. | Estos mecanismos son patrones de infraestructura (optimistic concurrency control, idempotent consumer, correlation identifier), no comportamiento de dominio. Especificarlos por evento duplicaría lo que la plataforma ya resuelve y contaminaría el modelo con concerns de infraestructura. Alineado con OXP `[D20]`. | `[SI10]`, OXP `[D20]` |
| **D12** | **`AsegurarTerceroDesdeConsumidor` no puede omitir la validación de `[I11]`.** Los consumidores automáticos (OXP, CXC, RRHH, Tesorería, importaciones masivas, SincoRE) no pueden forzar un registro cuando se detecta un posible duplicado. Ante la detección, el comando siempre rechaza con `{ causa: PosibleDuplicadoDetectado, terceroIdCandidato }` y el consumidor debe mapear al existente o escalar a un operador humano. La única excepción a la validación de `[I11]` es el comando `RegistrarTerceroForzado`, ejecutado exclusivamente por operadores humanos autorizados. | Los consumidores automáticos no tienen el contexto de negocio para juzgar si un caso de homonimia es legítimo — solo un operador humano con visión completa del caso puede. Permitir que un consumidor fuerce el registro rompería la garantía de `[I11]` en el canal más frecuente de registro. | `[SI10]`, `[SI11]`, `[I11]` |
| **D13** | **Registro en dos fases con activación asíncrona tras confirmación de dirección fiscal.** El tercero nace en estado `EnRegistro` con su identidad base (vía `TerceroRegistrado`). Pasa a `Activo` solo cuando el servicio de Direcciones confirma asincrónicamente la creación de la dirección fiscal (vía `TerceroActivado`). Si Direcciones falla permanentemente tras los reintentos de la plataforma (`[D11]`), el tercero queda en estado terminal `Abortado` (vía `TerceroRegistroAbortado`) — no reactivable. Los dominios consumidores (OXP, CXC, RRHH, etc.) se suscriben a `TerceroActivado`, no a `TerceroRegistrado`, para garantizar que solo abren sus agregados de rol para terceros operables. La identificación de un tercero `Abortado` queda disponible para un nuevo intento de registro con otro `terceroId` (ver `[SI1]`). | Resuelve la tensión entre (a) `[R25]` / `[I6]` como invariante fuerte del tercero `Activo`, (b) Direcciones como servicio único dueño de todas las direcciones del ERP, y (c) arquitectura event-driven asíncrona. El estado intermedio `EnRegistro` reconoce explícitamente la ventana de consistencia eventual inherente al registro multi-servicio, sin romper invariantes ni introducir sincronización entre servicios. Patrón estándar de la industria (Stripe `PaymentIntents`, KYC bancario `pending_verification`, Auth0 users, SAP `BlockedForPosting`). | `[SI1]`, `[R16]`, `[R25]`, `[I6]`, `[D11]`, `anexo-decision-orquestacion-registro.md` |

---

## 10. Premisas de negocio

Verdades del negocio, la regulación o el contexto multi-país que condicionan el diseño del modelo. No son invariantes estructurales (I##) ni decisiones arquitectónicas (D##) — son hechos externos al modelo que se toman como base.

| # | Premisa | Justificación | Aplica a |
|---|---------|---------------|----------|
| **P1** | **Un tercero puede cambiar razón social, tipo/número de documento o tipo de persona sin perder su identidad en el sistema.** El identificador técnico (`terceroId`) se mantiene; los valores de la identificación evolucionan con el tiempo. | Hecho del negocio — las empresas cambian razón social por fusiones o reestructuraciones; los números de documento se corrigen por errores históricos o cambios legales; un mismo tercero debe poder mantenerse como entidad única ante esos cambios. | `TerceroIdentificacionActualizada`, `TerceroRazonSocialActualizada`, `TerceroTipoPersonaActualizado`, `[R06]`, `[D3]`, `[SI4]` |
| **P2** | **El dígito de verificación (DV) es un valor derivado del número de documento por un algoritmo público publicado por la autoridad fiscal de cada país.** El sistema consume el algoritmo del catálogo; no lo define. | Hecho normativo — en Colombia la DIAN publica el algoritmo de DV del NIT (módulo 11); otros países publican algoritmos análogos para sus verificadores. El sistema confía en esas publicaciones. | `[R04]`, `[D6]`, `[SI2]` |
| **P3** | **Un mismo tercero puede cumplir múltiples roles simultáneos en el mismo ERP.** Ejemplos: una empresa puede ser proveedor (OXP) y cliente (CXC) a la vez; un empleado puede también ser proveedor de servicios puntuales. | Hecho del negocio — el modelo de empresa colombiana permite que un mismo tercero participe en distintos contextos de negocio sin que el sistema lo duplique. | `[R02]`, `[R07]`, `[D1]` |
| **P4** | **Las autoridades fiscales y regulatorias auditan transacciones hasta 5 años después de ocurridas.** Las auditorías exigen identificar al tercero con la razón social y documento **vigentes al momento de la transacción**, no los actuales. | Hecho regulatorio — DIAN (Colombia), DGII (República Dominicana), DGI (Panamá) y equivalentes establecen plazos legales de auditoría que exigen trazabilidad retrospectiva del tercero. | `[R06]`, `[I10]`, `[D3]`, `[SI4]` |
| **P5** | **El mismo tipo de documento puede existir en varios países con algoritmos y formatos diferentes.** Ej: la cédula de identidad en Colombia y en Panamá tienen longitudes y reglas de validación distintas. | Hecho del contexto multi-país — la validación depende tanto del tipo de documento como del país emisor, no solo del tipo. | `[R03]`, `[R04]`, `[I2]` |
| **P6** | **Los cambios relevantes en la identidad de un tercero típicamente se respaldan con documentos legales externos al sistema** (acta notarial, escritura pública, RUT actualizado). | Práctica del negocio y de la regulación en los países soportados — los cambios de razón social o tipo de persona tienen soporte legal documental. Justifica que los eventos de identidad capturen un `motivo` libre para referenciar el respaldo. | Eventos de Sección 5.1 |

---

## 11. Pendientes por definir

Aspectos que requieren definición posterior al cierre de la Fase 2 del modelo. Ninguno es deuda pendiente del modelo de dominio en sí — todos corresponden a fases posteriores (EventCatalog, implementación o decisiones de producto). La columna **Tipo** explicita el origen del pendiente.

| # | Pendiente | Tipo | Contexto | Condición de activación |
|---|-----------|------|----------|-------------------------|
| **PD1** | **Contratos formales de eventos de integración con dominios consumidores.** Los eventos del dominio son consumidos por OXP, CXC, RRHH, Tesorería, Impuestos y por la capa BFF que compone completitud (`[D5]`). Los contratos formales (schema versionado, política de compatibilidad, broker, retención, política de reentrega, ordering guarantees, `correlationId`, política de retry y DLQ) no se especifican en este modelo. | Fase 3 — EventCatalog | Consistente con el patrón de Impuestos y Contabilidad: los contratos de integración se difieren al EventCatalog del proyecto, donde se especifican de forma transversal a todos los sub-dominios. | Inicio de la construcción del EventCatalog. |
| **PD2** | **Estrategia técnica del índice de unicidad para `[I1]` y `[I11]`.** `[SI1]` describe el índice como proyección/tabla de lookup consistente eventualmente. El mecanismo concreto (store, latencia objetivo, estrategia de reconciliación ante inconsistencias detectadas, respuesta ante fallos de la proyección, estrategia de rebuild) queda fuera del modelo por ser decisión de implementación. | Implementación | Relacionado con `[SI9]` (forma canónica) y con el comportamiento del comando `AsegurarTerceroDesdeConsumidor` (`[SI10]`) en el paso 2 del flujo — el índice debe soportar lookup por `numero` y por clave primaria completa. | Inicio de la implementación del agregado Tercero. |
| **PD3** | **Canales externos adicionales de captura de terceros.** `[D8]` establece `TerceroRegistrado` con atributos `origen` + `contextoOrigen` extensibles. Los canales automáticos/externos más allá de los ya previstos (portal de autogestión, API pública, nuevas fuentes externas) no tienen contrato ni flujo definido. Cada uno puede traer particularidades: autoenrolamiento con verificación posterior, reconciliación masiva, políticas anti-spam, precedencia entre canales. | Futuro — Producto | El modelo ya es extensible por diseño (`[D8]`). No hay bloqueo técnico — es decisión de producto habilitar cada canal según roadmap. | Priorización del canal en el roadmap de producto. |

---

## 12. Catálogo de permisos atómicos del dominio

Cada bounded context declara los recursos que protege y las acciones que expone como permisos atómicos. La plataforma de seguridad del ERP consume este catálogo para integrarlo a su modelo de autorización (roles, políticas, relaciones).

**Lo que define este catálogo:**
- **Recursos protegidos** — componentes del dominio que requieren control de acceso.
- **Acciones por recurso** — operaciones de negocio que se pueden proteger.
- **Nivel** — capacidad del dominio a la que pertenece la acción (convención ERP: N1 fundamental / N2 complementario).
- **Restricciones de contexto** — dimensiones que limitan el acceso más allá de la acción.

**Lo que NO define este catálogo:**
- Roles (responsabilidad de la plataforma de seguridad).
- Asignación de usuarios a permisos.
- Mecanismo de autenticación o enforcement.

**Convención de naming:** `accion_recurso` en snake_case. Compatible con OAuth scopes, policy engines (OPA, Cedar) y motores ReBAC (SpiceDB, OpenFGA).

**Nivel del dominio:** Terceros es un dominio de una sola capacidad — no tiene split de microservicios. Todos los permisos pertenecen a **N1**.

| Recurso | Acción | Nivel | Identificador |
|---------|--------|:-----:|---------------|
| Tercero | Registrar | N1 | `registrar_tercero` |
| Tercero | Registrar con omisión de la validación de duplicado | N1 | `registrar_tercero_forzado` |
| Tercero | Asegurar desde consumidor (servicio-a-servicio) | N1 | `asegurar_tercero_desde_consumidor` |
| Tercero | Consultar | N1 | `consultar_tercero` |
| Tercero | Actualizar identificación | N1 | `actualizar_identificacion_tercero` |
| Tercero | Actualizar razón social | N1 | `actualizar_razon_social_tercero` |
| Tercero | Actualizar tipo de persona | N1 | `actualizar_tipo_persona_tercero` |
| Tercero | Inactivar | N1 | `inactivar_tercero` |
| Tercero | Reactivar | N1 | `reactivar_tercero` |
| Rol del tercero | Asignar | N1 | `asignar_rol_tercero` |
| Rol del tercero | Remover | N1 | `remover_rol_tercero` |
| Dirección del tercero | Referenciar | N1 | `referenciar_direccion_tercero` |
| Dirección del tercero | Desreferenciar | N1 | `desreferenciar_direccion_tercero` |
| Dirección del tercero | Designar preferida | N1 | `designar_direccion_preferida_tercero` |
| Contacto del tercero | Registrar | N1 | `registrar_contacto_tercero` |
| Contacto del tercero | Actualizar | N1 | `actualizar_contacto_tercero` |
| Contacto del tercero | Inactivar | N1 | `inactivar_contacto_tercero` |
| Contacto del tercero | Reactivar | N1 | `reactivar_contacto_tercero` |
| Contacto del tercero | Designar principal | N1 | `designar_contacto_principal_tercero` |

**Total:** 19 permisos atómicos, todos N1.

**Restricción de contexto:** el acceso a todos los recursos se restringe por **empresa/tenant**. La plataforma de seguridad evalúa el tenant del usuario o servicio contra el tenant del recurso. Para el permiso `asegurar_tercero_desde_consumidor`, el servicio consumidor (OXP, CXC, etc.) opera con identidad de servicio dentro del tenant cliente.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-21 | 12 secciones. 1 agregado raíz (Tercero) con 1 entidad interna (Contacto). 4 Value Objects (Identificacion, ReferenciaDireccion, CorreoElectronico, Telefono). 18 eventos (4 identidad, 4 estado, 2 roles, 3 direcciones, 5 contactos). 2 FSM (Tercero con 4 estados, Contacto con 2 estados). 2 catálogos del dominio (roles, tipos de contacto). 11 invariantes (9 Local + 2 Eventual). 13 decisiones (D1-D13). 6 premisas (P1-P6). 3 pendientes (PD1-PD3). 11 sugerencias de implementación (SI1-SI11). 19 permisos atómicos (todos N1). Sin domain services. |
