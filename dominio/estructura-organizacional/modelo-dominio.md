# Modelo de Dominio — Estructura Organizacional

**Versión:** 1.5
**Fecha:** 2026-06-19

---

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

Este documento formaliza el modelo de dominio DDD/ES/EDA del sub-dominio de Estructura Organizacional: agregados, value objects, eventos, invariantes, máquinas de estado, decisiones y sugerencias de implementación. Materializa lo que el alcance dejó descrito en lenguaje funcional y lo traduce a las piezas que el equipo de desarrollo va a implementar.

| Documento | Rol | Descripción |
|-----------|-----|-------------|
| `definicion-alcance.md` v1.3 | QUÉ hace el sistema | Fuente de verdad para glosario, actores, flujos y reglas (`[R##]`, 30 reglas). No se duplica aquí. |
| **Este documento** | CÓMO se comporta el dominio | Agregados, eventos, transiciones, precondiciones, invariantes, sugerencias de implementación. |
| `anexo-decisiones-arquitectonicas.md` v1.2 | Decisiones estructurales | Cuatro decisiones que enmarcan el modelo (codificación plana + jerarquía aparte, estructura del árbol y ciclo de vida con cinco estados de unidad reabribles, reestructuración como eventos de dominio, multi-dimensionalidad desde el diseño). |
| [`../../guias-de-modelado/datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md) | **Fundamento del trato de la unidad como dato entre dominios** | Por qué el consumidor mantiene **copia local** y no consulta al dueño en caliente, las dos capas de sincronización, las tres estrategias para un dato que aún no existe (solicitar / valor de respaldo / **diferir**) y los anti-patrones. `[D15]` aplica esta guía a la unidad organizacional; aquí vive el razonamiento de *por qué* es el mejor patrón. |
| EventCatalog (Fase 3) | Catalogación técnica | Consumirá este documento como especificación de entrada para los contratos versionados de eventos. |

Las reglas de negocio se referencian como `[R##]` y su texto completo vive en `definicion-alcance.md`, Sección 6. Las decisiones estructurales se referencian como `[DA##]` para distinguirlas (Decisión del Anexo arquitectónico) de las decisiones del propio modelo (`[D##]`).

---

## 2. Convenciones del documento

### 2.1. Nomenclatura

- **Eventos:** PascalCase en español, sin tildes ni caracteres especiales (compatibilidad con código fuente). Ejemplo: `UnidadActivada`.
- **Comandos:** PascalCase en español, infinitivo + objeto. Ejemplo: `ActivarUnidad`.
- **Agregados:** PascalCase; corresponden a los términos del glosario canónico (Sección 2 del alcance). Dos agregados raíz: `GrupoOrganizacional` y `UnidadOrganizacional`.
- **Referencias:**
  - `[R##]` reglas de negocio (alcance, Sección 6).
  - `[D##]` decisiones de este modelo (Sección 9).
  - `[DA##]` decisiones del anexo arquitectónico (`anexo-decisiones-arquitectonicas.md`).
  - `[I##]` invariantes del dominio (Sección 7).
  - `[SI##]` sugerencias de implementación (Sección 3.5).
  - `[P##]` premisas de negocio (Sección 10).
  - `[PD##]` pendientes por definir (Sección 11).
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
| **Precondiciones** | Condiciones que deben cumplirse. Referencias a `[R##]` e `[I##]`. |
| **Información capturada** | Datos que el evento registra (payload). |
| **Efectos** | Consecuencias: entidades creadas, estado modificado, eventos derivados. |

### 2.3. Diagramas

- FSM en ASCII. Estados terminales marcados con `■`.
- Eventos de progreso (sin cambio de estado) se listan dentro del recuadro del estado.
- Eventos de transición se muestran en las flechas entre estados.

### 2.3.1. Formato del delta en eventos `*Modificado`

Los eventos cuyo nombre termina en `Modificado` (`UnidadModificada`, `GrupoModificado`, `TipoUnidadModificado`) capturan **solo los campos que efectivamente cambiaron**, no un snapshot completo del agregado (`[D07]`). El payload usa el formato canónico:

```
{
  ...identificadores estables (unidadId, grupoId, etc.),
  changes: {
    nombreDelCampo: nuevoValor,
    ...
  },
  motivo: "...",
  usuarioId: "...",
  timestamp: "..."
}
```

- El mapa `changes` contiene una clave por cada campo modificado con su nuevo valor.
- Si un campo no cambió, no aparece en `changes`.
- El estado completo se reconstruye reproduciendo la secuencia de eventos sobre el agregado.

### 2.4. Causalidad entre eventos

| Tipo | Descripción | Consistencia |
|------|-------------|-------------|
| Directa | Resultado inmediato de un comando del usuario u otro consumidor. | Transaccional |
| Derivado por transición | Mismo agregado, mismo append atómico. | Transaccional |
| Efecto inter-agregado | Domain service coordina entre agregados (saga). | Eventual |
| Reactiva | Disparado por la recepción de otro evento (proyección que actúa). | Eventual |

### 2.5. Precisiones terminológicas

| Término | Significado en este modelo |
|---------|---------------------------|
| **Inactiva** (unidad) | Estado de `UnidadOrganizacional` reabrible. La unidad operó previamente y dejó de hacerlo, pero su historial se conserva y puede volver a operar mediante `UnidadReabierta`. |
| **Inactivo** (grupo) | Estado de `GrupoOrganizacional`. El grupo no agrupa nuevas operaciones; puede reactivarse mediante `GrupoReactivado`. |
| **Inactivación** | Proceso operativo del dominio (Flujos F7 y F10 del alcance) que lleva una unidad o grupo a su estado no operativo. No es terminal: la unidad puede reabrirse; el grupo puede reactivarse. |
| **Descarte** | Proceso operativo (Flujo F8) que lleva una unidad en `Borrador` al estado terminal estricto `Descartada`. Se diferencia de la inactivación porque la unidad nunca operó. |
| **Reestructuración** | Familia de procesos (F12 Fusión, F13 División, F14 Traslado) que cambian la estructura preservando trazabilidad histórica para comparabilidad IFRS 8. |
| **motivoBaja** | Atributo del modelo de lectura (no estado FSM) que registra por qué una unidad quedó `Inactiva` o `Descartada`. Valores literales fijos del dominio: `operativa`, `fusion`, `division`, `abandono_por_inactividad`. |

---

## 3. Bounded Context y Agregados

### 3.1. Estructura Organizacional como Bounded Context

El bounded context contiene dos agregados raíz (`GrupoOrganizacional` y `UnidadOrganizacional`) y tres domain services que coordinan procesos multi-agregado.

```
┌──────────────────────────────────────────────────────────────────────┐
│                Bounded Context: Estructura Organizacional             │
│                                                                       │
│  ┌─────────────────────┐                  ┌──────────────────────┐   │
│  │ GrupoOrganizacional │◄───────[ref]─────│ UnidadOrganizacional │   │
│  │  (FSM 2 estados)    │                  │   (FSM 5 estados)    │   │
│  │                     │                  │                      │   │
│  │  · Jerarquía        │                  │  · Estado operativo  │   │
│  │  · Cascada          │                  │  · Reestructuración  │   │
│  │  · Tipos de unidad  │                  │  · motivoBaja        │   │
│  └─────────┬───────────┘                  └──────────┬───────────┘   │
│            │                                          │              │
│            │                                          │              │
│            ▼                                          ▼              │
│   ┌─────────────────────────────────────────────────────────┐        │
│   │  Domain Services                                        │        │
│   │   · CascadaInactivacionGrupo (orquesta F10)            │        │
│   │   · ReestructuracionUnidad (orquesta F12/F13)          │        │
│   │   · DescarteAutomaticoBorradores (proceso programado)  │        │
│   └─────────────────────────────────────────────────────────┘        │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
                              │
                              │  Eventos del dominio
                              ▼
            Sub-dominios consumidores (OXP, Contabilidad)
```

### 3.2. Agregado: `GrupoOrganizacional`

**Descripción:** Nodo agrupador de la jerarquía organizacional. No recibe imputaciones operativas. Su ciclo de vida es binario (`Activo` / `Inactivo`) y su inactivación dispara una cascada hacia todos sus descendientes (sub-grupos y unidades). Contiene además el catálogo de tipos de unidad disponibles para la empresa, como configuración estructural del propio agregado (ver `[D10]`).

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| `grupoId` | Identidad | Identificador único del grupo. | uuid generado al crear |
| `codigo` | VO `Codigo` | Identificador alfanumérico plano, único por tenant entre grupos y unidades. Inmutable. | 4-12 caracteres (parametrizable por tenant `[R10]`) |
| `nombre` | VO `Nombre` | Denominación descriptiva del grupo. | texto |
| `padreId` | Referencia | Identificador del grupo padre. Null solo para el grupo raíz. | uuid o null |
| `esRaiz` | Boolean | Marca el grupo raíz único del tenant (creado automáticamente). | true / false |
| `nivel` | Entero (calculado, no almacenado) | Profundidad del grupo en la jerarquía vigente. Se proyecta desde `[SI02]` (proyección de jerarquía vigente). No se appendea en eventos ni se persiste en el agregado. | 0 para raíz |
| `estado` | Enum FSM | `Activo` o `Inactivo`. | |
| `tiposUnidad` | Entidades internas (lista) | Catálogo de tipos de unidad. **Vive únicamente en el grupo raíz**; los sub-grupos lo heredan dinámicamente (ver `tiposVigentes()` y `[D13]`). La lista del raíz se reconstruye reproduciendo los eventos `TipoUnidadAgregado`, `TipoUnidadModificado` y `TipoUnidadInactivado` sobre el agregado raíz; una proyección de catálogo vigente la materializa para consulta rápida desde la UI y para validar al crear unidades hijas. | lista de `TipoUnidad { nombre, activo }` (solo poblada en el grupo raíz; vacía en sub-grupos) |
| `versionAgregado` | Entero (atributo interno de plataforma) | Stamp de concurrencia optimista materializado por `[SI06]`. No es atributo de negocio — se usa solo en validación de `expectedVersion` al hacer append. No aparece en los payloads de los eventos. | |

**Comportamiento calculado (no almacenado):**

| Método | Fórmula | Usado por |
|--------|---------|-----------|
| `descendientesActivos()` | Recorrido recursivo de la proyección de jerarquía vigente (`[SI02]`), filtrando sub-grupos `Activo` y unidades `Activa` o `Suspendida`. | F10 (Inactivación de grupo con cascada). El domain service `CascadaInactivacionGrupo` invoca este método del agregado — no consulta la proyección directamente — para mantener la fuente única de verdad en el dominio. |
| `descendientesAfectablesPorCascada()` | Recorrido recursivo: sub-grupos `Activo` + unidades `Activa`/`Suspendida` + unidades `Borrador`. Devuelve lista clasificada por tipo. | F10, para mostrar al administrador el impacto previsto (`[R21]`) y para que la saga itere todos los nodos afectados. |
| `tiposVigentes()` | **Herencia dinámica desde el grupo raíz** (ver `[D14]`): recorre la jerarquía vía proyección `[SI02]` hasta el grupo raíz y devuelve `tiposUnidad` con `activo = true` del raíz. Sin replicación: los sub-grupos no almacenan tipos propios — siempre ven los del raíz vigente. El catálogo se administra en un único lugar (el raíz); cualquier cambio se refleja inmediatamente en todos los sub-grupos sin migración. | Validación al crear unidades hijas en cualquier punto de la jerarquía. |
| `puedeInactivarse()` | `estado == Activo` y `(esRaiz == false OR sinContenido())` | Validación previa a F10. Si retorna false, el comando `InactivarGrupo` se rechaza. |

**Eventos propios (7):**

- Ciclo de vida del grupo: `GrupoCreado`, `GrupoInactivado`, `GrupoReactivado`, `GrupoModificado`.
- Configuración del catálogo de tipos: `TipoUnidadAgregado`, `TipoUnidadModificado`, `TipoUnidadInactivado`.

### 3.3. Agregado: `UnidadOrganizacional`

**Descripción:** Nodo hoja de la jerarquía donde se imputan las transacciones. Pertenece a exactamente un grupo padre y nunca tiene hijos (`[R01]`, `[R06]`). Su ciclo de vida tiene cinco estados con transiciones controladas (ver Sección 4.1): nace en `Borrador` o `Activa` según el flujo de creación, opera, puede pausarse (`Suspendida`), reactivarse, cerrarse (`Inactiva` reabrible), reabrirse o descartarse antes de operar (`Descartada` terminal estricto). Soporta los tres procesos de reestructuración (Fusión, División, Traslado) preservando identidad e historial.

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| `unidadId` | Identidad | Identificador único de la unidad. | uuid generado al crear |
| `codigo` | VO `Codigo` | Identificador alfanumérico plano, único por tenant, inmutable. | 4-12 caracteres |
| `nombre` | VO `Nombre` | Denominación descriptiva. | texto |
| `tipoUnidad` | Referencia | Nombre del tipo de unidad (del catálogo del grupo padre o de un ancestro). | texto que existe en `tiposUnidad` vigentes |
| `descripcion` | Texto opcional | Descripción libre. | |
| `grupoPadreId` | Referencia | Identificador del grupo padre. | uuid no nulo |
| `estado` | Enum FSM | `Borrador`, `Activa`, `Suspendida`, `Inactiva`, `Descartada`. | |
| `motivoBaja` | Enum opcional (proyectado en read model) | Causa de la baja cuando `estado in {Inactiva, Descartada}`. Valores: `operativa`, `fusion`, `division`, `abandono_por_inactividad`. Ver `[D06]`. | null si está operando |
| `causalidadBaja` | Referencia opcional | Cuando `motivoBaja in {fusion, division}`, referencia a la unidad destino o lista de destinos. | uuid o lista de uuid |
| `fechaUltimaActividadBorrador` | Timestamp | Última modificación mientras la unidad estuvo en `Borrador`. Habilita la política de descarte automático por inactividad. Ver `[D09]` y `[SI05]`. | null cuando deja de estar en Borrador |
| `fechaEfectiva` | `FechaEfectiva` opcional | Solo presente cuando la unidad fue parte de una reestructuración (`motivoBaja in {fusion, division}`). Capturada en `UnidadInactivada` y proyectada para reconstrucción histórica. Ver Sección 3.4 (VO `FechaEfectiva`). | null en operación normal |
| `versionAgregado` | Entero (atributo interno de plataforma) | Stamp de concurrencia optimista materializado por `[SI06]`. No es atributo de negocio — se usa solo en validación de `expectedVersion` al hacer append. No aparece en los payloads de los eventos. | |

**Datos transaccionales capturados en eventos pero no almacenados como atributos del agregado:**

- `motivo` (texto libre que aparece en `UnidadActivada`, `UnidadSuspendida`, `UnidadReactivada`, `UnidadReabierta`, `UnidadInactivada`, `UnidadDescartada`, `UnidadModificada`, `UnidadTrasladada`).
- `fechaEstimadaReactivacion` (opcional en `UnidadSuspendida`).

Estos datos viven solo en el stream de eventos para auditoría narrativa; el agregado no los proyecta en su estado actual porque no son condiciones de negocio que afecten el comportamiento futuro.

**Atributos de baja son write-once:** `motivoBaja`, `causalidadBaja` y `fechaEfectiva` se asignan una sola vez (al evento que origina la baja) y no se modifican posteriormente. Esto se garantiza con el método interno `validarCoherenciaBaja()` (ver comportamientos calculados).

**Comportamiento calculado (no almacenado):**

| Método | Fórmula | Usado por |
|--------|---------|-----------|
| `puedeRecibirImputaciones()` | `estado == Activa` | Validación de consumidores antes de imputar. |
| `puedeReabrirse()` | `estado == Inactiva` y `padreEstaActivo()*` | Validación previa a F6 (`[I06]`). |
| `puedeReestructurarse()` | `estado in {Activa, Suspendida}` | Validación previa a F12/F13/F14 (`[I14]`). |
| `puedeFusionarse()` | `estado in {Activa, Suspendida}` | Validación previa a F12 — el agregado valida su propio estado; la saga `ReestructuracionUnidad` valida las precondiciones cruzadas. |
| `puedeDividirse()` | `estado in {Activa, Suspendida}` | Validación previa a F13 — análogo a `puedeFusionarse()`. |
| `puedeTrasladarse()` | `estado in {Activa, Suspendida}` y `padreNuevoActivo()*` | Validación previa a F14 (`[I10]`). |
| `puedeModificarse()` | `estado in {Borrador, Activa, Suspendida}` | Validación previa a F15 (`[I15]`). |
| `puedeDescartarseAutomáticamente(umbral)` | `estado == Borrador` y `fechaUltimaActividadBorrador + umbral < ahora()` | Validación que el agregado aplica al recibir `UnidadDescartada` con `motivoBaja: "abandono_por_inactividad"` desde el proceso `[SI05]`. Si la condición no se cumple, el evento se rechaza. |
| `validarCoherenciaBaja()` | Verifica `[I05]` cuando `estado in {Inactiva, Descartada}`: (a) `motivoBaja` está definido; (b) si `motivoBaja in {fusion, division}`, entonces `causalidadBaja` y `fechaEfectiva` están presentes; (c) si `motivoBaja in {operativa, abandono_por_inactividad}`, entonces `causalidadBaja` y `fechaEfectiva` son null. | Guard interno que el agregado invoca al recibir `UnidadInactivada` o `UnidadDescartada`. Si la coherencia falla, el evento se rechaza antes del append. Refuerza la invariante `[I05]` y la propiedad write-once de los atributos de baja. |

`*` Consulta la proyección documentada en sugerencias de implementación (`[SI03]`). No es método local del agregado.

**Eventos propios (11):**

- Creación y activación: `UnidadCreada`, `UnidadActivada`.
- Pausa y reactivación: `UnidadSuspendida`, `UnidadReactivada`, `UnidadReabierta`.
- Baja: `UnidadInactivada`, `UnidadDescartada`.
- Modificación: `UnidadModificada`.
- Reestructuración: `UnidadFusionada`, `UnidadDividida`, `UnidadTrasladada`.

### 3.4. Value Objects compartidos

| VO | Usado por | Descripción |
|----|-----------|-------------|
| `Codigo` | `GrupoOrganizacional`, `UnidadOrganizacional` | Cadena alfanumérica plana de longitud parametrizable entre 4 y 12 caracteres (`[R10]`). **Codificación plana, sin estructura jerárquica embebida** — la jerarquía se modela como agregado separado y se proyecta vía `[SI02]` (ver `[DA1]`). Inmutable una vez asignado (`[R09]`). Único por tenant cruzando grupos y unidades (`[R08]`, `[I09]`). |
| `Nombre` | Ambos agregados | Cadena descriptiva no vacía para lectura humana. Sin restricción de unicidad. Modificable. |
| `MotivoBaja` | `UnidadOrganizacional` (atributo proyectado) | Enum cerrado del dominio: `operativa`, `fusion`, `division`, `abandono_por_inactividad`. Los cuatro valores son literales fijos del modelo — no son catálogo configurable (ver `[D08]`, `[D09]`). |
| `FechaEfectiva` | Eventos de reestructuración y de transición de jerarquía | Momento a partir del cual una versión de la jerarquía o una reestructuración rige. No puede ser anterior a la última transacción registrada en las unidades involucradas (`[R25]`, `[I08]`). |
| `ReferenciaJerarquica` | Composición de ambos agregados | Combinación `padreId + nivel` que ubica un nodo en el árbol. El nivel es calculado, no almacenado en el código (`[DA1]`). |

### 3.5. Sugerencias de implementación

**Mapping rápido — Comando ↔ sugerencias de implementación aplicables:**

| Comando / proceso | SIs que el implementador debe aplicar |
|-------------------|---------------------------------------|
| `CrearGrupo` (F9) | `[SI01]`, `[SI02]`, `[SI03]`, `[SI06]` |
| `CrearUnidad` (F1 — admin, directa o desde la bandeja de sugerencias) | `[SI01]`, `[SI03]`, `[SI04]`, `[SI06]` |
| `ActivarUnidad` (F3) | `[SI03]`, `[SI06]` |
| `SuspenderUnidad` (F4) / `ReactivarUnidad` (F5) | `[SI03]`, `[SI06]` |
| `ReabrirUnidad` (F6) | `[SI03]`, `[SI06]` |
| `InactivarUnidad` (F7) | `[SI06]` |
| `DescartarUnidad` (F8 manual) | `[SI04]`, `[SI06]` |
| `InactivarGrupo` (F10) | `[SI06]`, `[SI08]`, `[SI09]` |
| `ReactivarGrupo` (F11) | `[SI06]`, `[SI08]` (consulta correlación) |
| `FusionarUnidades` (F12) | `[SI02]`, `[SI06]`, `[SI09]`, `[SI10]` |
| `DividirUnidad` (F13) | `[SI02]`, `[SI06]`, `[SI09]`, `[SI10]` |
| `TrasladarUnidad` (F14) | `[SI02]`, `[SI03]`, `[SI06]`, `[SI10]` |
| `ModificarUnidad` / `ModificarGrupo` (F15) | `[SI06]` |
| `AgregarTipoUnidad` / `ModificarTipoUnidad` / `InactivarTipoUnidad` | `[SI06]` |
| Proceso `DescarteAutomaticoBorradores` (`[SI05]`) | `[SI04]` (lectura) |
| Recepción de la señal de demanda de unidad (desde consumidores) | `[SI07]` (idempotencia), `[SI11]` (bandeja de sugerencias) |
| Proyección de última imputación (F12/F13/F14) | `[SI10]` (eventos de imputación entrantes) |

---

#### `[SI01]` Índice único de `Codigo` por tenant

Materializa `[I09]`, `[R08]`, `[R11]`.

Mantener un índice único por `(tenantId, codigo)` que cubre simultáneamente grupos y unidades organizacionales (espacio de nombres único — un grupo y una unidad no pueden compartir código en el mismo tenant). El índice **excluye unidades en `Descartada`** para liberar la identificación según `[R11]`.

**Orden transaccional para validación de unicidad:**

1. El comando (`CrearGrupo`, `CrearUnidad`) consulta el índice antes de aceptar la creación.
2. Si el código está disponible, el comando emite el evento `GrupoCreado` o `UnidadCreada` y, en el mismo append, registra el código en el índice como parte de la proyección del evento.
3. Si dos comandos concurrentes superan la validación pero solo uno logra el append (el otro recibe `version-conflict` por `[SI06]`), el segundo se rechaza con error específico `version-conflict` y la UI re-lee el estado y solicita confirmación al usuario.
4. Tras `UnidadDescartada`, el código de la unidad se remueve del índice. SLA esperado: la remoción es efectiva en <500 ms tras el append. Si un nuevo `CrearUnidad` con el código liberado llega dentro de esa ventana y choca con el índice, el comando se rechaza con error `codigo-no-disponible` y la UI reintenta tras breve espera (o el usuario elige otro código).

#### `[SI02]` Proyección de jerarquía vigente con detección de ciclos

Materializa `[I11]`, `[R04]`.

Mantener una proyección actualizada de la jerarquía con la versión vigente por fecha efectiva. La proyección valida en tiempo de comando que cualquier traslado o creación no introduzca un ciclo mediante recorrido de ancestros. Esta proyección debe mantenerse **en sincronía** con `[SI03]` (padre activo); si divergen, los comandos podrían validar estados inconsistentes. SLA de convergencia: <500 ms tras cualquier evento que modifique la estructura.

#### `[SI03]` Proyección de "padre Activo"

Materializa `[I10]`, `[R07]`, `[R16]`.

Mantener una proyección que indique el estado vigente del grupo padre de cada unidad y de cada sub-grupo. Las precondiciones de F1, F3, F6, F9, F14 consultan esta proyección antes de aceptar el comando.

**SLA y estrategia ante stale:**

- La proyección actualiza en <500 ms tras `GrupoInactivado` o `GrupoReactivado`.
- El agregado consulta la proyección como "última copia conocida" — no garantía transaccional.
- Si entre la consulta y el append del comando el padre cambia de estado (race condition), el evento se emite y, en post-procesamiento, se detecta la inconsistencia: el sub-dominio emite `UnidadInactivada` automática (con `motivoBaja: "operativa"` y un campo `causaSistema: "padre_inactivado_post_creacion"` en metadata) + alerta interna al administrador para revisión.
- Esto es una ventana tolerable de inconsistencia eventual documentada (<500 ms).

#### `[SI04]` Bandeja de Borradores pendientes

Soporta los flujos F3 (activación) y F8 (descarte manual) y el proceso `[SI05]`.

Proyección que lista todas las unidades en `Borrador` —preparaciones del administrador— con su `fechaUltimaActividadBorrador` y antigüedad. Es la fuente para la UI del administrador (F3, F8) y para el proceso programado de descarte automático (`[SI05]`). No contiene demandas de consumidores: esas viven en la bandeja de sugerencias (`[SI11]`), que es una proyección distinta y previa a que exista cualquier unidad.

El campo `fechaUltimaActividadBorrador` se actualiza **atómicamente** con cada evento que modifique la unidad en estado `Borrador` (`UnidadCreada`, `UnidadModificada`). Nunca se calcula desde un timestamp de BD externo — siempre proviene del evento mismo.

#### `[SI05]` Proceso programado de descarte automático de Borradores

Materializa `[D09]`.

**Definición:** job recurrente del propio sub-dominio que recorre la bandeja de Borradores (`[SI04]`), identifica unidades con antigüedad mayor al umbral configurado por tenant (default sugerido: 30 días) y emite `UnidadDescartada` con `motivoBaja: "abandono_por_inactividad"`.

**Trigger:** cron configurable por tenant; default 02:00 UTC diario. Garantía de ejecución única vía lock distribuido por `(tenantId, fecha)`: si dos nodos intentan iniciar el job el mismo día, solo uno lo ejecuta.

**Estado persistido del job:**

- `JobDescarteEjecucion(jobExecutionId, tenantId, fechaInicio, fechaFin, estado: Running | Completed | Failed)` — una entrada por ejecución.
- `JobDescarteUnidadProcesada(jobExecutionId, unidadId, fechaProcesamiento, resultadoEmision: Emitido | Rechazado | NotificacionPendiente)` — una entrada por unidad procesada.

**Flujo:**

1. Crear `JobDescarteEjecucion` con estado `Running`.
2. Consultar candidatos en `[SI04]` con `fechaUltimaActividadBorrador + umbral < ahora()`.
3. Por cada candidato: validar que **no haya sido procesado** en la ejecución actual (consultar `JobDescarteUnidadProcesada`). Si ya fue procesado, saltar.
4. Emitir `UnidadDescartada` con `usuarioId: "sistema:descarte-automatico"`, `motivoBaja: "abandono_por_inactividad"`, `correlationId: jobExecutionId`. El agregado valida `puedeDescartarseAutomáticamente(umbral)` y rechaza si la condición ya no se cumple (carrera aceptable: el evento simplemente no se emite).
5. Registrar `JobDescarteUnidadProcesada` con `resultadoEmision: Emitido`.
6. Al terminar el recorrido: marcar `JobDescarteEjecucion` como `Completed` (o `Failed` si hubo errores no recuperables).

Los borradores son preparaciones del administrador y no fueron referenciados por la operación de ningún consumidor, así que el descarte no notifica ni compensa a otros sub-dominios (`[D15]`).

**Idempotencia:** garantizada por `(unidadId, jobExecutionId)`. Si el job crashea y se reinicia con el mismo `jobExecutionId`, los registros ya procesados se saltan.

#### `[SI06]` Concurrencia optimista por agregado

Soporta `[R12]`, `[R18]` y todas las transiciones FSM.

Cada agregado mantiene un `versionAgregado` (entero) que se incrementa con cada evento aplicado. La versión actual se devuelve en la **metadata** de cada evento emitido (no en el payload de negocio).

**Comandos que requieren `expectedVersion`:**

- **Obligatorio** en comandos críticos: `InactivarUnidad` (F7), `InactivarGrupo` (F10), `FusionarUnidades` (F12), `DividirUnidad` (F13), `TrasladarUnidad` (F14). Estos cambios afectan trazabilidad histórica y no admiten pérdida silenciosa por concurrencia.
- **Recomendado** en `ModificarUnidad`, `ModificarGrupo`, `SuspenderUnidad`, `ReactivarUnidad`, `ReabrirUnidad`, `ActivarUnidad`.
- **No aplica** en comandos de creación (`CrearGrupo`, `CrearUnidad`): no existe versión previa.

**Comportamiento ante conflicto:**

- Si `expectedVersion` difiere de la versión actual del agregado, el comando se rechaza con error específico `version-conflict`.
- La UI captura el error, re-lee el estado actual del agregado y notifica al usuario: "El estado de la unidad cambió desde su última consulta; revise y confirme nuevamente".
- Optimistic concurrency previene Silent Writes (dos comandos concurrentes sin que el segundo se entere), pero no reemplaza la lógica de negocio: dos administradores activando la misma unidad simultáneamente verán el conflicto y deben confirmar de nuevo.

#### `[SI07]` Idempotencia de la señal de demanda desde consumidores

Materializa `[R30]`.

La **señal de demanda** que un consumidor emite cuando necesita una unidad inexistente (ver `[SI11]`) es un mensaje entrante que puede llegar repetido (reenvío del bus, reintento del consumidor). Para que un reenvío no genere dos sugerencias idénticas en la bandeja del administrador, cada señal lleva un identificador único (`senalId`) que Estructura Organizacional persiste; si la misma señal llega dos veces, la segunda se ignora.

> Este patrón **protege la visibilidad, no la corrección**: la señal solo alimenta la bandeja de sugerencias (`[SI11]`); no crea unidades ni dispara comandos. La consistencia de la operación del consumidor depende del mecanismo de diferir (`[D15]`), no de esta señal. Por eso la señal puede tratarse como aviso seguro de repetir sin garantías fuertes — si se perdiera, el sistema seguiría siendo correcto (ver `[D15]`).

**Mensajes cubiertos por este patrón:**

- La señal de demanda de unidad inexistente (desde OXP, Contabilidad, futuros consumidores).

**Mecanismo de persistencia:**

- Tabla `SenalRecibida(senalId, tenantId, resultadoProyectado, fechaRecepcion)` con TTL de 7 días (configurable). Tras el TTL, los registros se purgan — una señal posterior con el mismo identificador se trataría como nueva.
- La tabla es por tenant para evitar colisiones entre clientes distintos.

**No cubiertos por este patrón:**

- Comandos originados por el administrador desde la UI propia del módulo (F1, F3, F4, F5, F6, F7, F8, F9, F10, F11, F14, F15): la idempotencia se gestiona en la UI (deshabilitar el botón de envío tras el clic, validación de formulario, confirmación explícita en operaciones críticas). La unicidad de código la valida `[SI01]` al recibir el comando.
- Procesos internos del propio sub-dominio (`[SI05]` `DescarteAutomaticoBorradores`): la idempotencia se gestiona con `jobExecutionId` (ver `[SI05]`).
- Eventos de imputación entrantes para `[SI10]`: la idempotencia de esa proyección es la estándar de proyecciones (`[D11]`).

#### `[SI08]` Saga `CascadaInactivacionGrupo` — política de completud

Materializa `[I12]`, `[I16]`, `[R19]`, `[R20]`.

El domain service `CascadaInactivacionGrupo` (Sección 3.6) usa un `correlationId` único para enlazar el `GrupoInactivado` raíz con todos los eventos derivados (`GrupoInactivado` de sub-grupos, `UnidadInactivada` y `UnidadDescartada` de unidades hijas). Cada evento derivado lleva el `correlationId`.

**Política de completud — "at-least-once con convergencia":**

- La saga persiste su estado en una tabla relacional dedicada del propio sub-dominio: `SagaCascadaEstado(correlationId, grupoOrigenId, descendientesPendientes, descendientesCompletados, estado, fechaInicio, fechaUltimaActividad)`. No vive en el event store — es estado de proceso, no eventos de dominio.
- Reintenta cada descendiente pendiente hasta éxito eventual (max 5 reintentos por descendiente; backoff exponencial 100 ms / 500 ms / 2 s / 10 s / 30 s).
- Tras agotar reintentos, el descendiente entra en estado `dead-letter` y se registra en un log de sagas incompletas (tabla `SagaCascadaDeadLetter`).
- **Idempotencia por `(agregadoId, correlationId)`** donde `agregadoId` es el `grupoId` para sub-grupos derivados y el `unidadId` para unidades derivadas. El agregado mantiene además su propio guard de idempotencia: si recibe un evento con un `correlationId` que ya aplicó, lo rechaza silenciosamente.

**Alerta operacional:** un proceso de health-check del propio sub-dominio recorre `SagaCascadaEstado` con `estado: Running` y `fechaUltimaActividad` antigua. Si la saga lleva más de N minutos sin completar todos los descendientes (configurable por tenant, default 15 min), se emite una alerta interna (no es evento de dominio) para revisión humana. La saga sigue intentando converger en segundo plano.

**Sin compensación inversa:** no se implementa rollback automático. Si la saga queda irrecuperablemente incompleta, el operador debe ejecutar `ReactivarGrupo` (F11) sobre el grupo origen y reabrir los hijos manualmente. La invariante `[I16]` garantiza que toda saga converge en estado coherente.

**Reactivación con sugerencia:** cuando el administrador ejecuta `ReactivarGrupo` (F11) sobre un grupo previamente inactivado por esta saga, el sistema inteligente consulta los `correlationId` asociados al grupo y sugiere reabrir los hijos uno a uno (Flujo F6 para unidades, F11 para sub-grupos).

#### `[SI09]` Tabla de deduplicación de eventos de saga

Materializa el principio at-least-once de `[SI08]` y `[SI06]`.

Las sagas (`CascadaInactivacionGrupo`, `ReestructuracionUnidad`) pueden reintentar la emisión de un evento derivado si el primer intento falló. Para evitar que un reintento provoque dos appends del mismo evento en el stream del agregado destino, el dispatcher de saga mantiene una tabla de deduplicación:

```
SagaEventEmitted(
  sagaId,
  step,
  aggregateId,
  correlationId,
  eventType,
  fechaEmision
)
```

**Comportamiento:**

- Antes de emitir un evento derivado, el dispatcher consulta la tabla con clave `(aggregateId, correlationId, eventType)`.
- Si la combinación ya existe, el evento ya fue emitido exitosamente — se salta y se marca el paso como completado.
- Si no existe, se emite y se registra atómicamente con el append (en la misma transacción del event store o con compensación).
- El agregado mantiene además su propia idempotencia por `(agregadoId, correlationId)`: si por cualquier razón el evento llega dos veces al agregado, el segundo se rechaza silenciosamente.

**Por qué dos niveles de dedup:** la tabla `SagaEventEmitted` evita el doble append; la idempotencia del agregado es la red de seguridad ante fallos del dispatcher. Ambos se complementan.

#### `[SI10]` Proyección local de última imputación por unidad

Materializa `[I08]` y `[R25]`.

Mantener, **dentro de Estructura Organizacional**, una proyección local de la última imputación registrada por unidad organizacional, para validar la `fechaEfectiva` en procesos de reestructuración y traslado (`FusionarUnidades` F12, `DividirUnidad` F13, `TrasladarUnidad` F14).

**Cómo se alimenta — eventos de imputación entrantes.** Los consumidores transaccionales (OXP, Contabilidad, futuros) publican un evento cuando imputan a una unidad; Estructura Organizacional se suscribe y proyecta la fecha más reciente por unidad en su propia copia. Es el mismo patrón de copia local por eventos de la guía [`datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md), aplicado en sentido inverso: aquí Estructura Organizacional es el consumidor de un dato (la actividad transaccional) gobernado por los sub-dominios operativos.

Contrato mínimo del evento entrante:

```
ImputacionRegistrada { unidadId, tenantId, fechaImputacion, ...origen }
```

**Comportamiento:**

- El servicio `ReestructuracionUnidad` consulta la **proyección local** antes de aceptar `FusionarUnidades`, `DividirUnidad` o `TrasladarUnidad`. Si la fecha efectiva propuesta es anterior a la última imputación de cualquier unidad involucrada, el comando se rechaza con error `fecha-efectiva-anterior-a-historial`.
- La validación **nunca consulta a los consumidores en caliente**: opera siempre contra la proyección local. No hay política de "rechazar si el consumidor está inaccesible" — Estructura Organizacional no depende de la disponibilidad de los consumidores para reestructurar.
- La proyección es consistente eventual: si un evento de imputación aún no llegó, la última imputación conocida podría quedar momentáneamente atrás. El riesgo se acota con la reconciliación de respaldo (`[SI12]`) y con la fecha efectiva, que en la práctica es posterior a la actividad ya registrada. La idempotencia y el orden de la proyección son los estándar de proyecciones (`[D11]`).

**Nota sobre la dependencia funcional:** este flujo establece una segunda relación de Estructura Organizacional con los sub-dominios consumidores —los eventos de imputación entrantes—, adicional a la principal de emisión de eventos del ciclo de vida. Es una suscripción por eventos, no una consulta en caliente. Ver `definicion-alcance.md` Sección 7 — Dependencias externas.

#### `[SI11]` Bandeja de sugerencias de creación (señal de demanda)

Materializa `[R30]`.

Proyección que lista las **demandas de unidades inexistentes** que los consumidores hicieron visibles, como sugerencias para que el administrador cree la unidad si corresponde. Es la fuente de la UI de "sugerencias de creación" del administrador. Es distinta de la bandeja de Borradores (`[SI04]`): una sugerencia **no es una unidad** —es un aviso previo a que exista cualquier unidad—; el administrador la atiende creando la unidad por F1 (`CrearUnidad`) o la descarta de la bandeja.

**Cómo se alimenta — señal de demanda entrante.** Cuando un consumidor necesita una unidad que no existe en su copia local, además de diferir su operación (`[D15]`), emite una señal informativa que Estructura Organizacional proyecta en esta bandeja:

```
DemandaDeUnidadSenalada { senalId, tenantId, subDominioOrigen, datosSugeridos?, fechaSenal }
```

**Comportamiento:**

- La señal es **informativa y no bloqueante**: no crea unidades ni dispara comandos, solo agrega o actualiza una entrada en la bandeja. La creación sigue siendo acto deliberado del administrador (`[D15]`, `[R30]`).
- La idempotencia de la señal la garantiza `[SI07]` (`senalId`): un reenvío no genera dos sugerencias.
- La señal **no es condición de la operación del consumidor**: si se perdiera, la operación del consumidor sigue correcta porque su integridad la sostiene el mecanismo de diferir, no la señal. La bandeja es una mejora de la gestión, no parte del camino crítico de nadie.
- En F2+ una señal con contexto inequívoco puede habilitar la creación automática (`[PD01]`).

#### `[SI12]` Punto de resincronización de respaldo

Soporta `[R13]`, `[R29]` y la copia local de los consumidores.

Estructura Organizacional ofrece un punto de lectura de respaldo para que un consumidor **repare su copia local** de unidades cuando se desfasa (estuvo caído mucho tiempo, perdió un evento). Materializa la Capa 2 (reconciliación de respaldo) de la guía [`datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md).

**Características:**

- **Fuera del camino crítico:** el consumidor lo usa de fondo, no al imputar. Imputar siempre va contra su copia local (`[R13]`). Este punto solo repara la copia, no participa en cada operación.
- Puede materializarse como reproceso de los eventos de ciclo de vida desde un punto, o como una foto del estado vigente de las unidades del tenant.
- La fuente de verdad sigue siendo Estructura Organizacional; la copia del consumidor es derivada y reconstruible.
- Simétricamente, la proyección local de imputación (`[SI10]`) de Estructura Organizacional se repara por el mismo principio contra los consumidores.

### 3.6. Domain services

#### Servicio: `CascadaInactivacionGrupo`

**Trigger:** Comando `InactivarGrupo` validado por el agregado `GrupoOrganizacional` (que invoca `puedeInactivarse()` — el agregado decide; el servicio orquesta). Resuelve el Flujo F10 del alcance.

**Flujo principal:**

1. El servicio invoca `descendientesAfectablesPorCascada()` del agregado raíz para enumerar todos los descendientes (sub-grupos `Activo`, unidades `Activa`/`Suspendida`, unidades `Borrador`). El agregado es la fuente única de verdad; el servicio no consulta la proyección directamente.
2. Asigna un `correlationId` único al proceso y crea el registro `SagaCascadaEstado(correlationId, grupoOrigenId, descendientesPendientes, descendientesCompletados, estado: Running)`.
3. Aplica al agregado raíz `GrupoOrganizacional` el evento `GrupoInactivado` con `esCascada: false`, `correlationId` y `grupoIdOrigen: null` (es el propio raíz).
4. **Espera el write-ack del broker** para el `GrupoInactivado` raíz — solo la confirmación de persistencia en el event store, no de procesamiento downstream. Este es el "punto de no retorno" de la saga: una vez confirmada la persistencia del raíz, la saga está comprometida a propagar a todos los descendientes (`[I16]`). Si el broker no confirma (timeout, error), la saga aborta limpiamente sin emitir derivados.
5. Tras el write-ack, emite los eventos derivados en paralelo (no espera write-ack individual de cada uno; cada uno se valida vía `[SI09]` para evitar duplicados):
   - Por cada sub-grupo descendiente, emite `GrupoInactivado` con `esCascada: true`, `correlationId` y `grupoIdOrigen: <grupoOrigenId>`.
   - Por cada unidad descendiente en estado `Activa` o `Suspendida`, emite `UnidadInactivada` con `motivoBaja: "operativa"`, `esCascada: true` y `correlationId`.
   - Por cada unidad descendiente en estado `Borrador`, emite `UnidadDescartada` con `motivoBaja: "abandono_por_inactividad"`, `esCascada: true` y `correlationId` (interpretación: el grupo padre se inactivó, los borradores quedan sin razón de ser).
6. Cada evento exitoso actualiza `SagaCascadaEstado` (mueve el descendiente de `pendientes` a `completados`).
7. Al completar todos los descendientes: marcar la saga como `Completed`.

**Sobre el orden de entrega a consumidores:** los eventos derivados se emiten en paralelo tras el write-ack del raíz. **El sub-dominio no garantiza el orden de entrega** a los consumidores externos (OXP, Contabilidad, etc.) — esa garantía depende de la infraestructura de transporte y se considera responsabilidad de cada consumidor. Los consumidores reconcilian el orden lógico usando `correlationId`, la causalidad documentada en cada evento (`esCascada`, `grupoIdOrigen`) y los timestamps. Esto es coherente con el patrón EDA asincrónico: at-least-once + out-of-order tolerable.

**Política de compensación — "at-least-once con convergencia eventual":**

El paso 3 (`GrupoInactivado` raíz) es **punto de no retorno**: una vez emitido, la saga es responsable de propagar a TODOS los descendientes. No hay rollback del raíz.

| Paso | Evento | Stream | Estrategia ante fallo |
|------|--------|--------|-----------------------|
| 3 | `GrupoInactivado` raíz | Stream del grupo origen | Max 3 reintentos con backoff exponencial (100 ms / 500 ms / 2 s). Si persiste tras los 3, el comando se rechaza y la saga **no inicia** (raíz no se emite). |
| 4-6 | Eventos derivados | Streams de cada descendiente | Max 5 reintentos por descendiente, backoff exponencial 100 ms / 500 ms / 2 s / 10 s / 30 s. Idempotencia por `(agregadoId, correlationId)` — si el agregado ya recibió el evento, lo rechaza silenciosamente. Tras agotar reintentos, el descendiente entra en `dead-letter` (registrado en log de sagas incompletas) y la saga continúa con los demás. |

**Alerta operacional:** si la saga lleva más de 15 min (configurable por tenant) sin completar, se emite alerta interna para revisión humana. La saga sigue intentando converger en segundo plano. La invariante `[I16]` garantiza que toda saga converge a estado coherente (completa o `dead-letter`).

**Sin compensación inversa:** si una saga queda irrecuperablemente incompleta, el operador resuelve manualmente ejecutando `ReactivarGrupo` (F11) sobre el grupo origen y reabriendo los hijos uno a uno.

**Si el operador necesita visibilidad durante la ventana de propagación:** la proyección de lectura muestra el grupo origen como `Inactivo` desde el paso 3; los descendientes muestran su estado de origen hasta que reciban su evento derivado. Esto es ventana de inconsistencia eventual documentada (típicamente <5 s para árboles de hasta 2.000 nodos).

**Protocolo de proceso:** `correlationId` único; estado de la saga persistido en `SagaCascadaEstado` para retomar en caso de caída del proceso; cada evento derivado lleva el `correlationId` y `esCascada: true` para auditoría y para soportar la reactivación correlacionada de F11 (ver `[SI08]`).

#### Servicio: `ReestructuracionUnidad`

**Trigger:** Comandos `FusionarUnidades` (F12) o `DividirUnidad` (F13). El comando `TrasladarUnidad` (F14) no requiere servicio — afecta a una sola unidad y se ejecuta directamente sobre el agregado.

**Flujo principal (Fusión):**

1. Validar precondiciones cruzadas (`[I14]`, `[R22]`, `[R23]`, `[R24]`, `[R25]`). Las precondiciones por unidad las valida cada agregado vía `puedeFusionarse()`; las cruzadas (separación origen/destino, fecha efectiva coherente, destino en `Activa`) las valida el servicio.
2. Asignar `correlationId` único al proceso.
3. Emitir `UnidadFusionada` como evento de proceso (no transicional) en stream propio del proceso de reestructuración. Incluye `correlationId`, `codigosOrigen` (lista), `codigoDestino`, `fechaEfectiva`, `motivo`, `usuarioId`, `timestamp`.
4. **Esperar el write-ack del broker** para `UnidadFusionada` — punto de no retorno del proceso. Si falla, la saga aborta sin emitir bajas.
5. Tras el write-ack, por cada unidad origen, emitir `UnidadInactivada` con `motivoBaja: "fusion"`, `causalidadBaja: destinoId`, `fechaEfectiva`, `esCascada: true` y `correlationId`. Los eventos a las unidades origen se emiten en paralelo, deduplicados por `[SI09]`. Los consumidores reconcilian el orden lógico vía `correlationId`.

**Flujo principal (División):**

Análogo a Fusión pero con un origen y N destinos. `UnidadDividida` incluye `codigoOrigen` y `codigosDestino` (lista). El `UnidadInactivada` del origen lleva `motivoBaja: "division"` y `causalidadBaja: lista de destinos`.

**Tabla de compensación:**

| Paso | Evento | Stream | Estrategia ante fallo |
|------|--------|--------|-----------------------|
| 3 | `UnidadFusionada` / `UnidadDividida` | Stream del proceso | Max 3 reintentos con backoff exponencial. Si persiste, retornar error y no emitir bajas. El evento de proceso es punto de no retorno: si se emitió, los pasos 4 deben converger. **Idempotencia por `(processStreamId, correlationId, eventType)`** donde `processStreamId` identifica el stream del proceso de reestructuración. Garantiza que un reintento del paso 3 no append duplique el evento de proceso. |
| 4 | `UnidadInactivada` por origen | Stream de cada unidad origen | Max 5 reintentos por unidad. **Idempotencia por `(unidadId, correlationId)`** vía `[SI09]`. Si tras 5 reintentos no converge, entra en `dead-letter`; alerta operacional tras 15 min sin completar. |

**Protocolo de proceso:** mismo patrón que `CascadaInactivacionGrupo` — estado persistido, `correlationId` único, política de convergencia eventual sin compensación inversa.

#### Servicio: `DescarteAutomaticoBorradores`

**Trigger:** Proceso programado del propio sub-dominio (cron diario por tenant con lock distribuido). Ver `[SI05]` para el detalle completo del trigger, el estado persistido (`JobDescarteEjecucion`, `JobDescarteUnidadProcesada`) y la idempotencia por `jobExecutionId`. Esta sección documenta solo el flujo y la compensación; el resto de detalles operativos vive en `[SI05]` para evitar duplicación.

**Flujo principal (resumido):**

1. Iniciar la ejecución registrando estado (ver `[SI05]`).
2. Consultar la bandeja de Borradores pendientes (`[SI04]`) filtrando por antigüedad.
3. Por cada unidad candidata, emitir `UnidadDescartada` con `usuarioId: "sistema:descarte-automatico"`, `motivoBaja: "abandono_por_inactividad"`, `correlationId: jobExecutionId`. El agregado valida `puedeDescartarseAutomáticamente(umbral)`.
4. Cerrar la ejecución con estado final.

Los borradores son preparaciones del administrador; ningún consumidor los referencia en su operación, por lo que el descarte no notifica ni compensa a otros sub-dominios (`[D15]`).

**Tabla de compensación:**

| Paso | Evento / Acción | Estrategia ante fallo |
|------|-----------------|-----------------------|
| 3 | `UnidadDescartada` | Reintento con backoff (3 intentos: 1 s / 5 s / 30 s). Idempotencia por `(unidadId, jobExecutionId)`. Si persiste, marca `resultadoEmision: NoEmitido` y continúa con la siguiente unidad; alerta operacional al cierre del job si hubo > N fallos. |

**Idempotencia:** garantizada por `(unidadId, jobExecutionId)`. Si el job se cae y se reinicia con el mismo `jobExecutionId`, los registros ya marcados en `JobDescarteUnidadProcesada` se saltan.

**Coexistencia con `CascadaInactivacionGrupo`:** un `Borrador` puede ser candidato simultáneamente del job y de una saga de cascada activa cuyo grupo padre se está inactivando. **No se implementa exclusión previa entre los dos procesos** — se acepta como race tolerable resuelto por dedup natural del agregado:

- Ambos procesos pueden emitir `UnidadDescartada` con sus respectivos `correlationId` (`jobExecutionId` en el caso del job; `correlationId` de la saga en F10).
- El agregado `UnidadOrganizacional` aplica `puedeDescartarseAutomáticamente(umbral)` al recibir el evento. El primero que llegue cambia el estado de `Borrador` a `Descartada`; el segundo es **rechazado silenciosamente** por el guard porque el `estado != Borrador`.
- El `motivoBaja` registrado en el evento persistido es siempre `abandono_por_inactividad` (idéntico en ambos casos), por lo que no hay ambigüedad semántica para los consumidores.
- El `correlationId` del evento persistido apunta a quien llegó primero — útil para auditoría pero no compromete la integridad de la transición.

Esta política evita introducir una dependencia entre el job y una proyección de "sagas activas", manteniendo cada proceso independiente y resolviendo la coordinación con mecanismos del propio dominio.

### 3.7. Relaciones entre agregados

```
   ┌──────────────────────────┐
   │   GrupoOrganizacional    │
   │   (esRaiz=true, único)   │
   └────────────┬─────────────┘
                │ 1
                │
                │ N (sub-grupos)
       ┌────────┴────────┐
       │                 │
       ▼                 ▼
┌─────────────┐   ┌─────────────┐
│Grupo Org. A │   │Grupo Org. B │
└──┬────────┬─┘   └──┬──────────┘
   │ 1      │ 1     │ 1
   │        │       │
   │ N      │ N     │ N
   ▼        ▼       ▼
┌─────┐  ┌─────┐  ┌─────┐
│Unid.│  │Unid.│  │Unid.│  (UnidadOrganizacional, siempre hoja)
└─────┘  └─────┘  └─────┘
```

**Cardinalidad:**

- `GrupoOrganizacional` 1:N `GrupoOrganizacional` (sub-grupos como hijos).
- `GrupoOrganizacional` 1:N `UnidadOrganizacional` (unidades como hijos).
- `UnidadOrganizacional` N:1 `GrupoOrganizacional` (padre obligatorio; `[R01]`).
- Mezcla libre de hijos en grupos (`[R05]`).
- `GrupoOrganizacional` `esRaiz` 1:1 por tenant (`[R02]`, `[I13]`).

### 3.8. Comportamiento de integración con consumidores (`[D15]`)

Las FSM de la Sección 4 describen el ciclo de vida **interno** de la unidad. Esta sub-sección describe el comportamiento **entre dominios** que introduce `[D15]`: qué pasa cuando un consumidor (OXP, Contabilidad) necesita una unidad que aún no existe. Es el flujo nuevo del replanteamiento #46 y el más sutil, porque combina tres mecanismos independientes (copia local, diferir, señal). El fundamento de patrones está en [`../../guias-de-modelado/datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md).

**Escenario:** a OXP le llega un documento imputado a una unidad que todavía no existe en su copia local.

```
   ┌──────────────────────────────────────────────┐
   │ [OXP] imputa contra su copia local;          │
   │ la unidad aún no existe                      │
   └──────────────────────────────────────────────┘
                          │
                          ▼
   ┌──────────────────────────────────────────────┐
   │ CORRECCIÓN — el sistema queda correcto:      │
   │ [OXP] DIFIERE la parte que requiere la       │
   │ unidad; no se detiene y espera el evento     │
   └──────────────────────────────────────────────┘
                          │
                          ▼
   ┌──────────────────────────────────────────────┐
   │ VISIBILIDAD — solo avisa, no condiciona:     │
   │ [OXP] emite la señal de demanda              │
   │ (no bloqueante · segura de repetir · [SI07]) │
   └──────────────────────────────────────────────┘
                          │
                          ▼
   ┌──────────────────────────────────────────────┐
   │ [EO] proyecta la señal en la bandeja de      │
   │ sugerencias ([SI11])                         │
   └──────────────────────────────────────────────┘
                          │
                          ▼
   ┌──────────────────────────────────────────────┐
   │ [ADMIN EO] ve la sugerencia y decide         │
   │ crear la unidad — acto deliberado (F1)       │
   └──────────────────────────────────────────────┘
                          │
                          ▼
   ┌──────────────────────────────────────────────┐
   │ [EO] emite UnidadActivada a los              │
   │ consumidores                                 │
   └──────────────────────────────────────────────┘
                          │
                          ▼
   ┌──────────────────────────────────────────────┐
   │ [OXP] recibe UnidadActivada: su copia        │
   │ local se actualiza y el diferido se          │
   │ resuelve solo (consistencia eventual)        │
   └──────────────────────────────────────────────┘
                          │
                          ▼
   ┌──────────────────────────────────────────────┐
   │ causación completa contra la unidad          │
   │ real (coincide exacto con Contabilidad)      │
   └──────────────────────────────────────────────┘
```

**Lo que el diagrama hace explícito** (la CORRECCIÓN y la VISIBILIDAD son dos caminos independientes que arrancan a la vez tras el primer paso; el diagrama los presenta en secuencia por claridad de lectura):

- **Camino de la *corrección* vs camino de la *visibilidad*.** El primero (diferir y esperar el evento `UnidadActivada`) es el que deja el sistema correcto; el segundo (la señal a la bandeja) solo acelera que el administrador se entere.
- **La señal no es condición de nada.** Si se perdiera, el diferido sigue en pie y la unidad puede crearse igual por la planeación del administrador; cuando exista, la resolución ocurre de todos modos. Por eso la señal puede ser fire-and-forget.
- **Nadie consulta a EO en caliente.** OXP siempre lee su copia local (`[R13]`); EO valida la última imputación contra su propia proyección local (`[SI10]`). Si una copia se desfasa, se repara de fondo por el punto de resincronización (`[SI12]`), nunca en el camino crítico.
- **La unidad nunca se aproxima.** No hay unidad de tránsito ni provisional: se difiere hasta que exista la real, porque debe coincidir exacto con la contabilidad.

---

## 4. Máquinas de estado

### 4.1. FSM de `UnidadOrganizacional`

Cinco estados, siete transiciones permitidas. `Descartada` es el único terminal estricto; `Inactiva` admite reapertura.

```
                                    ┌─────────────────┐
                                    │   Descartada  ■ │
                                    └─────────────────┘
                                            ▲
                                            │ UnidadDescartada
                                            │ (F8 manual o
                                            │  F10 cascada o
                                            │  SI05 automático)
                                            │
┌──────────────────┐  UnidadActivada   ┌────┴─────────────┐
│    Borrador      │──────(F3)────────►│      Activa      │◄────┐
│                  │                   │                  │     │
│  Eventos de      │                   │ Eventos de       │     │ UnidadReabierta
│  progreso:       │                   │ progreso:        │     │ (F6)
│   · UnidadCreada │                   │  · UnidadCreada  │     │
│   · UnidadModif. │                   │    (si F1 Activa │     │
│                  │                   │     directa)     │     │
└──────────────────┘                   │  · UnidadModif.  │     │
                                       └──┬───────────▲───┘     │
                                          │           │         │
                                          │           │         │
                              UnidadSuspendida  UnidadReactivada│
                                       (F4)          (F5)       │
                                          │           │         │
                                          ▼           │         │
                                       ┌──────────────┴───┐     │
                                       │   Suspendida     │     │
                                       │                  │     │
                                       │ Eventos de       │     │
                                       │ progreso:        │     │
                                       │  · UnidadModif.  │     │
                                       └──┬───────────────┘     │
                                          │                     │
                                          │ UnidadInactivada    │
                                          │ (F7 desde           │
                                          │  Activa o           │
                                          │  Suspendida)        │
                                          ▼                     │
                                       ┌──────────────────┐     │
                                       │    Inactiva      │─────┘
                                       │                  │
                                       │   (sin eventos   │
                                       │    de progreso)  │
                                       └──────────────────┘
```

**Notas estado por estado:**

- **`Borrador`** — Estado de **preparación del administrador**: la unidad que el administrador deja a medio definir antes de activarla. No transaccional y **no se origina desde sub-dominios consumidores** (la demanda de un consumidor no crea unidades — ver `[D15]`, `[R29]`). Admite eventos de progreso (`UnidadModificada`) sin cambio de estado. Transiciones de salida: a `Activa` mediante `UnidadActivada` (F3), o a `Descartada` mediante `UnidadDescartada` (F8 manual, F10 cascada al inactivar grupo padre, o `[SI05]` proceso automático tras umbral de inactividad).
- **`Activa`** — Estado operativo. Recibe imputaciones de los consumidores (`[R13]`). También es el estado inicial cuando el administrador elige "crear y activar directamente" en F1; en ese caso, el agregado emite `UnidadCreada` + `UnidadActivada` en el mismo append. Admite eventos de progreso (`UnidadModificada`). Transiciones de salida: a `Suspendida` (F4), a `Inactiva` (F7).
- **`Suspendida`** — Estado transitorio. No recibe nuevas imputaciones pero sigue consultable y reportable. Admite `UnidadModificada`. Transiciones de salida: a `Activa` (F5, `UnidadReactivada`) o a `Inactiva` (F7, `UnidadInactivada`).
- **`Inactiva`** — Estado de baja post-operación. Se conserva el historial. No admite imputaciones (`[R13]`) ni modificaciones (`[R17]`, `[I15]`). Admite reapertura mediante `UnidadReabierta` (F6), que la lleva a `Activa`. Lleva atributo `motivoBaja` proyectado (`operativa`, `fusion`, `division` o `abandono_por_inactividad` solo cuando viene desde Borrador en cascada).

  > **Nota sobre asimetría con grupos:** A diferencia de los grupos (que sí admiten `GrupoModificado` en estado `Inactivo`), las unidades en `Inactiva` **no admiten modificaciones**. La razón es que las unidades participan en historial transaccional — corregir su nombre o tipo tras la baja alteraría la lectura del histórico. Si se requiere corregir un dato erróneo, el flujo es F6 Reabrir → F15 Modificar → F7 Inactivar de nuevo.

- **`Descartada`** ■ — Terminal estricto (`[R14]`). La unidad nunca operó (siempre vino desde `Borrador`). Su `codigo` queda libre para una nueva solicitud (`[R11]`).

  > **Visibilidad en reportes:** Se filtra de **reportes operativos y de movimientos históricos** (la unidad nunca imputó, no aporta valor a la lectura transaccional). Sí permanece visible en: (a) **auditoría** — el evento `UnidadDescartada` queda en el stream y se puede consultar para trazabilidad; (b) **módulo administrativo de gestión de solicitudes** — bandeja de revisión del administrador; (c) **reportes de gestión de proyectos cancelados o abandonados** — cuando el usuario los active explícitamente desde la UI de Estructura Organizacional.

### 4.2. FSM de `GrupoOrganizacional`

Dos estados con transición bidireccional. Sin estados terminales.

```
                  GrupoInactivado (F10)
                    + cascada SI08
        ┌──────────────────────────────────┐
        │                                  ▼
   ┌────┴─────┐                      ┌─────────────┐
   │  Activo  │                      │  Inactivo   │
   │          │                      │             │
   │ Eventos  │                      │  Eventos    │
   │  prog.:  │                      │   prog.:    │
   │  · GrupoModif.                  │  · GrupoModif. (no en cascada)
   │  · TipoUnidadAgregado           │             │
   │  · TipoUnidadModificado         │             │
   │  · TipoUnidadInactivado         │             │
   └──────────┘                      └─────┬───────┘
        ▲                                  │
        └──────────────────────────────────┘
              GrupoReactivado (F11)
              (sin cascada inversa)
```

**Notas estado por estado:**

- **`Activo`** — Estado inicial al crear (F9, `GrupoCreado` directamente en `Activo`; no hay `Borrador` para grupos). Admite gestión del catálogo de tipos de unidad como eventos de progreso (`TipoUnidadAgregado`, `TipoUnidadModificado`, `TipoUnidadInactivado`) y modificación general (`GrupoModificado`). Transición de salida: a `Inactivo` mediante `GrupoInactivado` (F10) que dispara la saga `CascadaInactivacionGrupo`.
- **`Inactivo`** — El grupo no organiza nuevos descendientes operativos. Admite `GrupoModificado` (puede corregirse el nombre, por ejemplo, sin reactivar). **No admite** los eventos de configuración de catálogo de tipos (`TipoUnidadAgregado`, `TipoUnidadModificado`, `TipoUnidadInactivado`) — el catálogo queda **congelado de solo lectura** durante el período `Inactivo`. Las unidades existentes que usan tipos del catálogo no se ven afectadas. Transición de salida: a `Activo` mediante `GrupoReactivado` (F11) — sin cascada inversa a los hijos; el administrador reabre los hijos uno a uno con apoyo del sistema inteligente.

  > **Nota sobre asimetría con unidades:** A diferencia de las unidades en `Inactiva` (que NO admiten modificaciones), los grupos en `Inactivo` SÍ admiten `GrupoModificado`. La razón es que los grupos **no participan en historial transaccional** — son nodos agrupadores. Corregir el nombre o la descripción de un grupo inactivo no afecta la semántica de operaciones pasadas. La asimetría es intencional y refleja la diferencia funcional entre los dos tipos de nodo.

**Caso especial — Grupo raíz:** El grupo raíz (`esRaiz = true`) nunca puede inactivarse mientras tenga contenido (`[R03]`, `[I13]`). La validación se hace en el comando `InactivarGrupo` antes de iniciar la saga.

### 4.3. FSM de `TipoUnidad` (entidad interna)

Dos estados. La inactivación de un tipo no afecta unidades existentes que lo usan; solo impide nuevas asignaciones.

```
   ┌──────────┐  TipoUnidadInactivado   ┌─────────────┐
   │  Activo  │─────────────────────────►│  Inactivo   │
   │          │                          │             │
   │ Eventos: │                          └─────────────┘
   │  · TipoUnidadModificado             (terminal en F1; sin reactivación)
   └──────────┘
```

**Nota:** En F1 la reactivación de tipos de unidad no se modela como flujo (no apareció en el alcance). Si el negocio lo requiere en el futuro, se agrega un evento `TipoUnidadReactivado` análogo a la reactivación de grupo.

---

## 5. Catálogo de eventos

**Total: 18 eventos.** `GrupoOrganizacional` 7 + `UnidadOrganizacional` 11. Organizados por tema funcional, no por agregado.

### 5.1. Eventos del ciclo de vida de unidades

#### `UnidadCreada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una unidad organizacional fue registrada en el sistema. Siempre nace por **acto deliberado del administrador** (F1): directamente operativa (`Activa`) o en preparación (`Borrador`). La creación nunca se origina en un consumidor — la demanda de un consumidor solo se hace visible como sugerencia en la bandeja (`[SI11]`); el administrador decide si la atiende creando la unidad por F1. |
| **Causalidad** | Directa (comando `CrearUnidad`). |
| **Agregado** | `UnidadOrganizacional` |
| **Estado previo** | — (creación) |
| **Estado resultante** | `Borrador` (default) o `Activa` (cuando el administrador elige activar directamente — en ese caso se emite `UnidadActivada` en el mismo append). |
| **Precondiciones** | `[R08]` (código único), `[R07]` (grupo padre Activo), tipo válido en el catálogo vigente del grupo padre o ancestros (`[R05]` jerarquía), formato del código válido (`[R10]`), `[I09]` (unicidad cruzada en tenant). |
| **Información capturada** | `unidadId`, `codigo`, `nombre`, `tipoUnidad`, `descripcion`, `grupoPadreId`, `estadoInicial` (`Borrador` o `Activa` — **es un parámetro del comando que se captura para auditoría; el estado actual de la unidad se reconstruye reproduciendo la secuencia de eventos, no se lee de este campo**), `usuarioId`, `timestamp`. |
| **Efectos** | Se crea el agregado en el estado inicial elegido. Se inicializa `fechaUltimaActividadBorrador` si nace en `Borrador`. Si nace en `Activa`, se emite también `UnidadActivada`. Los consumidores reciben la notificación y actualizan su copia local; un diferido pendiente por esta unidad (`[R29]`, `[D15]`) se resuelve solo al llegar este evento. |

#### `UnidadActivada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una unidad pasa a estado `Activa`. Aplica al activar una unidad en `Borrador` (F3) o como evento colateral cuando una unidad nace directamente en `Activa` en F1 con `estadoInicial: Activa`. F5 (reactivación desde `Suspendida`) emite `UnidadReactivada`; F6 (reapertura desde `Inactiva`) emite `UnidadReabierta`. Este evento es el que destraba, en los consumidores, las operaciones que estaban diferidas a la espera de esta unidad (`[D15]`). |
| **Causalidad** | Directa (comando `ActivarUnidad`) en F3; derivada por transición (mismo append que `UnidadCreada`) en F1 con `estadoInicial: Activa`. |
| **Agregado** | `UnidadOrganizacional` |
| **Estado previo** | `Borrador` (F3) o ninguno si se emite junto con `UnidadCreada` (F1). |
| **Estado resultante** | `Activa` |
| **Precondiciones** | Datos mínimos completos (`[I07]`), grupo padre en estado `Activo` (`[I10]`, `[R07]`), no existe otra unidad `Activa` con el mismo `codigo` (`[I09]`). |
| **Información capturada** | `unidadId`, `codigo`, `tipoUnidad`, `grupoPadreId`, `estadoAnterior` (`Borrador` o `null` si vino con `UnidadCreada` en F1), `motivo` (opcional, texto libre), `usuarioId`, `timestamp`. |
| **Efectos** | La unidad acepta imputaciones (`[R13]`). Los consumidores actualizan su copia local; las operaciones que estaban diferidas a la espera de esta unidad se resuelven solas (`[R29]`, `[D15]`). El payload self-contained permite a los consumidores actualizar referencias locales sin consultas adicionales. |

#### `UnidadSuspendida`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una unidad activa entra en pausa transitoria. No recibe nuevas imputaciones pero sigue consultable y reportable (F4). |
| **Causalidad** | Directa (comando `SuspenderUnidad`). |
| **Agregado** | `UnidadOrganizacional` |
| **Estado previo** | `Activa` |
| **Estado resultante** | `Suspendida` |
| **Precondiciones** | Unidad en `Activa`. |
| **Información capturada** | `unidadId`, `motivo` (opcional, texto libre), `fechaEstimadaReactivacion` (opcional), `usuarioId`, `timestamp`. |
| **Efectos** | Los consumidores bloquean nuevas imputaciones. El historial se conserva intacto. |

#### `UnidadReactivada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una unidad suspendida vuelve a operar (F5). Diferenciado de `UnidadReabierta` (que aplica solo desde `Inactiva`) para auditoría. |
| **Causalidad** | Directa (comando `ReactivarUnidad`). |
| **Agregado** | `UnidadOrganizacional` |
| **Estado previo** | `Suspendida` |
| **Estado resultante** | `Activa` |
| **Precondiciones** | Unidad en `Suspendida`; grupo padre en `Activo` (`[I10]`). |
| **Información capturada** | `unidadId`, `motivo` (opcional), `usuarioId`, `timestamp`. |
| **Efectos** | Los consumidores vuelven a permitir imputaciones. |

#### `UnidadReabierta`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una unidad inactivada vuelve a operar (F6). Evento diferenciado de `UnidadReactivada` para que la auditoría distinga "reactivación de pausa transitoria" de "reapertura tras cierre". Habilita métricas como "tasa de reaperturas" sin inspeccionar payload. |
| **Causalidad** | Directa (comando `ReabrirUnidad`). |
| **Agregado** | `UnidadOrganizacional` |
| **Estado previo** | `Inactiva` |
| **Estado resultante** | `Activa` |
| **Precondiciones** | Unidad en `Inactiva` (`[R15]`); grupo padre en `Activo` (`[R16]`, `[I06]`). |
| **Información capturada** | `unidadId`, `motivo` (opcional, recomendado para trazabilidad), `usuarioId`, `timestamp`. |
| **Efectos** | El historial previo a la inactivación se mantiene visible; las nuevas imputaciones se enlazan en continuidad. El atributo `motivoBaja` proyectado se limpia. |

#### `UnidadInactivada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una unidad que operó deja de hacerlo. Aplica al cierre operativo manual (F7), a las bajas por reestructuración (F12 fusión, F13 división) o a las cascadas de inactivación de grupo (F10). |
| **Causalidad** | Directa en F7 (comando `InactivarUnidad`); efecto inter-agregado en F10 (`CascadaInactivacionGrupo`); derivada por transición en F12/F13 (`ReestructuracionUnidad`). |
| **Agregado** | `UnidadOrganizacional` |
| **Estado previo** | `Activa` o `Suspendida` |
| **Estado resultante** | `Inactiva` (reabrible) |
| **Precondiciones** | Unidad en `Activa` o `Suspendida` (`[R17]`); en F12/F13 además se aplican `[R22]`-`[R25]`. |
| **Información capturada** | `unidadId`, `codigo`, `tipoUnidad`, `grupoPadreId`, `motivoBaja` (`operativa` \| `fusion` \| `division`), `causalidadBaja` (opcional, referencia rápida: `unidadId` destino para fusión, lista de `unidadId` destinos para división; null cuando `motivoBaja == "operativa"`), `fechaEfectiva` (nullable; presente cuando `motivoBaja in {fusion, division}`; null cuando `motivoBaja == "operativa"` — la baja rige desde el `timestamp`), `esCascada` (boolean; `true` cuando es derivado de F10/F12/F13), `correlationId` (presente cuando `esCascada == true` o cuando proviene de saga), `motivo` (texto libre opcional), `usuarioId`, `timestamp`. |
| **Efectos** | Los consumidores bloquean nuevas imputaciones. El atributo `motivoBaja` se proyecta para reportería y bandejas. Los registros históricos se conservan. La unidad puede reabrirse posteriormente con F6 (excepto cuando `motivoBaja in {fusion, division}` — en esos casos reabrir es semánticamente raro pero técnicamente posible; el sistema inteligente debe advertir). |

> **Nota sobre `causalidadBaja`:** es una referencia rápida (uuid o lista de uuids). La información completa del proceso de reestructuración (todos los participantes, fecha efectiva, motivo) se consulta en el evento de proceso correlacionado (`UnidadFusionada` o `UnidadDividida`) usando el `correlationId`. Esta distribución entre dos streams (proceso + agregado) es por diseño: el agregado mantiene una referencia mínima; el stream de proceso mantiene el detalle completo (`[D04]`).

#### `UnidadDescartada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una unidad que nunca operó se descarta (F8). Aplica al rechazo manual del administrador sobre un borrador suyo, al abandono por inactividad (proceso automático `[SI05]`) o a la cascada cuando el grupo padre se inactiva con borradores colgando (F10). |
| **Causalidad** | Directa en F8 (comando `DescartarUnidad`); reactiva en `[SI05]`; efecto inter-agregado en F10. |
| **Agregado** | `UnidadOrganizacional` |
| **Estado previo** | `Borrador` |
| **Estado resultante** | `Descartada` ■ (terminal estricto, `[R14]`) |
| **Precondiciones** | Unidad en `Borrador`. Si proviene de `[SI05]`: además, `fechaUltimaActividadBorrador + umbral < ahora()` validado por el agregado vía `puedeDescartarseAutomáticamente(umbral)`. |
| **Información capturada** | `unidadId`, `codigo`, `tipoUnidad`, `grupoPadreId`, `motivoBaja` (`operativa` cuando es rechazo manual del admin; `abandono_por_inactividad` cuando es automático o por cascada F10 sobre Borrador), `esCascada` (boolean; `true` cuando es derivado de F10 o `[SI05]`), `correlationId` (presente cuando `esCascada == true`: `jobExecutionId` en `[SI05]`; `correlationId` de la saga en F10), `motivo` (texto libre opcional), `usuarioId` (`sistema:descarte-automatico` cuando proviene de `[SI05]`; identificador del administrador en F8), `timestamp`. |
| **Efectos** | El `codigo` de la unidad queda libre para una nueva creación (`[R11]`). La unidad se filtra de reportes históricos. Descartar un borrador del administrador no afecta a ningún consumidor — un borrador nunca fue referenciado por la operación de otro sub-dominio (ningún consumidor opera ni difiere contra borradores; difiere a la espera de una unidad `Activa`, `[D15]`). |

#### `UnidadModificada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Cambian uno o más campos descriptivos o clasificatorios de una unidad: nombre, tipo, descripción. El código y el grupo padre no se modifican por este evento (el código es inmutable, `[R09]`; el cambio de padre se hace por F14 con `UnidadTrasladada`). |
| **Causalidad** | Directa (comando `ModificarUnidad`). |
| **Agregado** | `UnidadOrganizacional` |
| **Estado previo** | `Borrador`, `Activa` o `Suspendida` |
| **Estado resultante** | Sin cambio de estado |
| **Precondiciones** | Unidad en estado modificable (`[R17]`, `[I15]`); si cambia el tipo, el nuevo tipo está vigente en el catálogo (`[I07]`). |
| **Información capturada** | `unidadId`, `changes` (map de `{ fieldName: nuevoValor }` solo con los campos efectivamente modificados; claves posibles: `nombre`, `tipoUnidad`, `descripcion`), `motivo` (opcional), `usuarioId`, `timestamp`. Formato delta canónico según Sección 2.3.1. |
| **Efectos** | El estado proyectado refleja los nuevos valores. Si se modificó en `Borrador`, se actualiza `fechaUltimaActividadBorrador` para reiniciar el conteo de antigüedad de `[SI05]`. Los consumidores con interés en los campos modificados (ej: Contabilidad al cambio de tipo) actualizan su vista local. |

### 5.2. Eventos del ciclo de vida de grupos

#### `GrupoCreado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Un grupo organizacional fue registrado en el sistema, directamente en estado `Activo` (los grupos no tienen `Borrador`; F9). |
| **Causalidad** | Directa (comando `CrearGrupo`). |
| **Agregado** | `GrupoOrganizacional` |
| **Estado previo** | — (creación) |
| **Estado resultante** | `Activo` |
| **Precondiciones** | Código único en el tenant (`[I09]`); grupo padre existente y en `Activo` (`[R07]`); posición válida en la jerarquía sin ciclos (`[I11]`); formato del código (`[R10]`). |
| **Información capturada** | `grupoId`, `codigo`, `nombre`, `padreId` (null si se está creando el grupo raíz por la inicialización del tenant), `esRaiz` (boolean), `usuarioId` (o `sistema` si es la inicialización automática del raíz), `timestamp`. |
| **Efectos** | El grupo queda operativo. Los consumidores con interés en la estructura jerárquica actualizan su vista local. |

#### `GrupoInactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Un grupo organizacional pasa a estado no operativo. Puede ser el grupo origen de una cascada (comando `InactivarGrupo`) o un sub-grupo afectado por la cascada de un ancestro. |
| **Causalidad** | Directa en el grupo origen (comando `InactivarGrupo`); efecto inter-agregado en sub-grupos descendientes (saga `CascadaInactivacionGrupo`). |
| **Agregado** | `GrupoOrganizacional` |
| **Estado previo** | `Activo` |
| **Estado resultante** | `Inactivo` |
| **Precondiciones** | Grupo en `Activo`; el grupo no es el raíz con contenido (`[R03]`, `[I13]`); el administrador confirmó el impacto previsto (`[R21]`, validación previa al comando). |
| **Información capturada** | `grupoId`, `codigo`, `nombre`, `padreId`, `motivo` (opcional), `esCascada` (boolean: `true` si fue derivado, `false` si es el origen), `grupoIdOrigen` (nullable; presente cuando `esCascada == true`: identifica el grupo raíz que disparó la cascada), `correlationId` (presente cuando `esCascada == true`), `usuarioId`, `timestamp`. |
| **Efectos** | El grupo no admite nuevos hijos directos en `Activo`. Si es el origen, dispara la saga `CascadaInactivacionGrupo`; si es derivado, contribuye a la propagación. Los consumidores actualizan su vista jerárquica. El payload self-contained (incluye `codigo`, `padreId`) permite a los consumidores actualizar referencias locales sin queries adicionales. |

#### `GrupoReactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Un grupo inactivo vuelve a estar disponible (F11). No propaga en cascada a sus hijos previamente afectados; el administrador los reabre o reactiva uno a uno. |
| **Causalidad** | Directa (comando `ReactivarGrupo`). |
| **Agregado** | `GrupoOrganizacional` |
| **Estado previo** | `Inactivo` |
| **Estado resultante** | `Activo` |
| **Precondiciones** | Grupo en `Inactivo`; grupo padre en `Activo` (si fue inactivado en cascada, primero se reactiva el padre). |
| **Información capturada** | `grupoId`, `nombre`, `motivo` (opcional), `usuarioId`, `timestamp`. Payload mínimo intencional: la lista de hijos afectados por la cascada original NO se incluye en el evento. |
| **Efectos** | El grupo vuelve a admitir hijos y operaciones de configuración. La lista de hijos afectados por la cascada original vive en la **proyección interna** definida en `[SI08]` (correlación por `correlationId`); la UI del administrador de Estructura Organizacional la consume para sugerir qué reabrir/reactivar. Los consumidores externos no requieren esa lista en el payload — solo necesitan saber que el grupo se reactivó para actualizar su vista local. |

> **Nota sobre payload mínimo:** se aplica el principio de que los eventos de integración deben llevar lo que **los consumidores externos** necesitan para reaccionar. La lista de hijos afectados es información de UI interna del propio sub-dominio — no es responsabilidad de OXP, Contabilidad u otros consumidores. Mantenerla como proyección consultable evita payloads impracticables en empresas con grupos amplios (hasta 2.000 unidades, `[P04]`).

#### `GrupoModificado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Cambian uno o más campos descriptivos del grupo (nombre, descripción). El código es inmutable. |
| **Causalidad** | Directa (comando `ModificarGrupo`). |
| **Agregado** | `GrupoOrganizacional` |
| **Estado previo** | `Activo` o `Inactivo` |
| **Estado resultante** | Sin cambio de estado |
| **Precondiciones** | Grupo existente. |
| **Información capturada** | `grupoId`, `changes` (map de `{ fieldName: nuevoValor }` solo con campos efectivamente modificados; claves posibles: `nombre`, `descripcion`), `motivo` (opcional), `usuarioId`, `timestamp`. Formato delta canónico según Sección 2.3.1. |
| **Efectos** | El estado proyectado refleja los nuevos valores. |

### 5.3. Eventos de reestructuración

#### `UnidadFusionada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Evento de proceso que registra la fusión de N unidades origen en una unidad destino (F12). Es el primer evento del proceso de reestructuración por fusión; le siguen los `UnidadInactivada` por cada unidad origen (con `motivoBaja: "fusion"`). |
| **Causalidad** | Directa (comando `FusionarUnidades`). |
| **Agregado** | Stream propio del proceso de reestructuración (no se appendea al stream de las unidades origen ni del destino). El servicio `ReestructuracionUnidad` es el emisor. |
| **Estado previo** | N/A (evento de proceso) |
| **Estado resultante** | N/A (las unidades origen pasarán a `Inactiva` por los `UnidadInactivada` derivados) |
| **Precondiciones** | Todas las unidades origen en `Activa` o `Suspendida` (`[R23]`, `[I14]`); destino en `Activa` y distinto del conjunto origen (`[R22]`, `[R24]`); fecha efectiva no anterior a la última transacción en origen ni destino (`[R25]`, `[I08]`). |
| **Información capturada** | `correlationId`, `unidadesOrigen` (lista de `unidadId`), `codigosOrigen` (lista de `codigo` paralela a `unidadesOrigen`), `unidadDestino` (`unidadId`), `codigoDestino`, `fechaEfectiva`, `motivo`, `usuarioId`, `timestamp`. |
| **Efectos** | Marca el inicio del proceso. Le siguen N `UnidadInactivada` (una por origen, con `motivoBaja: "fusion"` y el mismo `correlationId`). Los consumidores que reciben el evento reasignan sus referencias del conjunto origen al destino con fecha efectiva. El payload self-contained (incluye códigos) permite reconstruir el cambio histórico sin queries adicionales. Habilita reportes con "vista actual" (todo al destino desde fecha efectiva) o "vista histórica" (cada periodo con su estructura). |

> **Nota sobre boundaries:** Este evento vive en un **stream propio del proceso de reestructuración** (un stream por proceso, identificado por `correlationId`); no se appendea al stream de las unidades involucradas. **No existe un agregado backend `ReestructuracionUnidad`** — el evento es generado directamente por el **domain service** del mismo nombre (Sección 3.6) que coordina la saga. Los streams de las unidades reciben sus propios `UnidadInactivada` correlacionados por `correlationId`.

#### `UnidadDividida`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Evento de proceso que registra la división de una unidad origen en N unidades destino (F13). Le sigue el `UnidadInactivada` de la unidad origen (con `motivoBaja: "division"`). |
| **Causalidad** | Directa (comando `DividirUnidad`). |
| **Agregado** | Stream propio del proceso de reestructuración. |
| **Estado previo** | N/A (evento de proceso) |
| **Estado resultante** | N/A (la unidad origen pasará a `Inactiva` por el `UnidadInactivada` derivado) |
| **Precondiciones** | Unidad origen en `Activa` o `Suspendida` (`[R23]`); al menos dos destinos, todos en `Activa`, distintos del origen (`[R22]`, `[R24]`); fecha efectiva no anterior a la última transacción en origen (`[R25]`, `[I08]`). |
| **Información capturada** | `correlationId`, `unidadOrigen` (`unidadId`), `codigoOrigen`, `unidadesDestino` (lista de `unidadId`), `codigosDestino` (lista de `codigo` paralela a `unidadesDestino`), `fechaEfectiva`, `motivo`, `usuarioId`, `timestamp`. |
| **Efectos** | Marca el inicio del proceso. Le sigue 1 `UnidadInactivada` para la origen (con `motivoBaja: "division"` y el mismo `correlationId`). Los consumidores reasignan sus referencias futuras según corresponda; el historial previo a la fecha efectiva queda referenciado al origen (`[R27]`). El payload self-contained (incluye códigos) permite a los consumidores reconstruir la división histórica sin queries adicionales. |

> **Nota sobre boundaries:** análogo a `UnidadFusionada` — vive en stream propio del proceso de reestructuración (un stream por `correlationId`), no se appendea al stream de las unidades involucradas, y no existe agregado backend. El servicio `ReestructuracionUnidad` (Sección 3.6) es el emisor.

#### `UnidadTrasladada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una unidad cambia de grupo padre en la jerarquía (F14). Conserva identidad, código, estado e historial transaccional (`[R28]`). Solo cambia su posición en el árbol y la versión vigente de jerarquía. |
| **Causalidad** | Directa (comando `TrasladarUnidad`). |
| **Agregado** | `UnidadOrganizacional` (el cambio impacta también la proyección de jerarquía, pero el evento se appendea al stream de la unidad). |
| **Estado previo** | `Activa` o `Suspendida` |
| **Estado resultante** | Sin cambio de estado (la posición en el árbol cambia, no el ciclo de vida) |
| **Precondiciones** | Unidad en `Activa` o `Suspendida` (`[R23]`); nuevo grupo padre existente, en `Activo` y distinto del padre actual (`[R07]`, `[I10]`); fecha efectiva coherente con la versión vigente (`[R25]`, `[I08]`); el nuevo padre admite el tipo de la unidad (catálogo vigente). |
| **Información capturada** | `unidadId`, `codigo`, `tipoUnidad`, `grupoPadreAnterior`, `grupoPadreNuevo`, `fechaEfectiva`, `motivo`, `usuarioId`, `timestamp`. |
| **Efectos** | La jerarquía registra una nueva versión vigente a partir de la fecha efectiva. La unidad sigue operando con el mismo código. Los consumidores con interés jerárquico (reportería) actualizan su vista. El payload self-contained (incluye `codigo` y `tipoUnidad`) permite a los consumidores que mantienen mapeos `código → padre` actualizar deterministicamente sin queries adicionales (`[P03]`). |

### 5.4. Eventos de configuración del catálogo de tipos

> **Nota terminológica:** Estos tres eventos (`TipoUnidadAgregado`, `TipoUnidadModificado`, `TipoUnidadInactivado`) son **eventos de configuración del catálogo** dentro del agregado `GrupoOrganizacional`. Modifican el estado de la entidad interna `TipoUnidad`, no el del propio grupo — por eso el grupo aparece como "estado resultante: sin cambio de estado del grupo". Lo que sí cambia es el catálogo (`TipoUnidadAgregado` añade, `TipoUnidadModificado` actualiza, `TipoUnidadInactivado` desactiva un tipo). Esta distinción importa para los consumidores que mantengan proyecciones del catálogo: deben reaccionar al evento aunque la FSM del grupo no cambie.

#### `TipoUnidadAgregado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se agrega un nuevo tipo de unidad al catálogo del grupo. En F1 se administra al nivel del grupo raíz y se hereda hacia abajo. |
| **Causalidad** | Directa (comando `AgregarTipoUnidad`). |
| **Agregado** | `GrupoOrganizacional` |
| **Estado previo** | `Activo` |
| **Estado resultante** | Sin cambio de estado del grupo (evento de progreso). |
| **Precondiciones** | Grupo en `Activo` (si está `Inactivo`, el catálogo queda congelado y el comando se rechaza). El nombre del tipo no está duplicado en el catálogo del grupo. |
| **Información capturada** | `grupoId`, `nombreTipoUnidad`, `descripcion` (opcional), `usuarioId`, `timestamp`. |
| **Efectos** | El catálogo del grupo (y heredado por sub-grupos) incluye el nuevo tipo, disponible para asignar a unidades nuevas. |

#### `TipoUnidadModificado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Cambian datos descriptivos de un tipo de unidad existente. |
| **Causalidad** | Directa (comando `ModificarTipoUnidad`). |
| **Agregado** | `GrupoOrganizacional` |
| **Estado previo** | `Activo` |
| **Estado resultante** | Sin cambio de estado del grupo. |
| **Precondiciones** | Tipo existente y vigente. Grupo en `Activo` (si está `Inactivo`, el catálogo queda congelado y el comando se rechaza). |
| **Información capturada** | `grupoId`, `nombreTipoUnidad`, `changes` (map de `{ fieldName: nuevoValor }` solo con campos efectivamente modificados; clave principal: `descripcion`), `motivo` (opcional), `usuarioId`, `timestamp`. Formato delta canónico según Sección 2.3.1. |
| **Efectos** | El catálogo refleja los nuevos valores. |

#### `TipoUnidadInactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Un tipo de unidad se inactiva — no podrá asignarse a unidades nuevas. Las unidades existentes que ya lo usan no se ven afectadas. |
| **Causalidad** | Directa (comando `InactivarTipoUnidad`). |
| **Agregado** | `GrupoOrganizacional` |
| **Estado previo** | `Activo` (del tipo) |
| **Estado resultante** | `Inactivo` (del tipo) |
| **Precondiciones** | Tipo existente y `Activo`. Grupo en `Activo` (si está `Inactivo`, el catálogo queda congelado y el comando se rechaza). |
| **Información capturada** | `grupoId`, `nombreTipoUnidad`, `motivo` (opcional), `usuarioId`, `timestamp`. |
| **Efectos** | El tipo deja de aparecer en las opciones de creación de unidades. Las unidades existentes que lo usan no requieren acción. |

---

## 6. Catálogos del dominio

### 6.1. Catálogo de tipos de unidad

Catálogo **interno** al sub-dominio (no proviene de Datos de Referencia — los tipos son conceptos del modelo organizacional, no datos de referencia universales). Se administra al nivel del grupo raíz y se hereda hacia los sub-grupos. Es extensible: cada empresa puede agregar tipos personalizados según su modelo de negocio.

**Tipos pre-cargados sugeridos al inicializar un tenant:**

| Nombre | Descripción |
|--------|-------------|
| `centro_de_costo` | Unidad contable de imputación de costos. |
| `proyecto` | Unidad temporal con alcance, fechas y entregables definidos. |
| `sucursal` | Punto de operación físico geográficamente distinguible. |
| `inmueble` | Bien físico administrable (uso típico: ABR). |
| `departamento` | Unidad funcional de la organización. |

Cada tenant puede agregar, modificar o inactivar tipos vía los eventos `TipoUnidadAgregado`, `TipoUnidadModificado`, `TipoUnidadInactivado`.

### 6.2. Catálogo de motivos de baja

Catálogo de **literales fijos del dominio**. No es configurable; agregar un nuevo motivo requiere diseñar un nuevo flujo de inactivación y modificar el modelo.

| Valor | Significado | Flujo origen |
|-------|-------------|--------------|
| `operativa` | Baja por decisión operativa normal (cierre de sucursal, fin de proyecto, etc.) o rechazo manual de un Borrador. | F7 (unidades operativas); F8 manual (Borradores) |
| `fusion` | Baja como parte de una fusión: la unidad origen quedó integrada en una unidad destino. | F12 |
| `division` | Baja como parte de una división: la unidad origen quedó separada en N unidades destino. | F13 |
| `abandono_por_inactividad` | Baja automática de un Borrador que excedió el umbral de inactividad configurado. | `[SI05]` (proceso programado) |

El atributo `motivoBaja` se proyecta en el modelo de lectura para reportería, bandejas y auditoría (`[D06]`).

---

## 7. Invariantes del dominio

16 invariantes. Clasificación: **local** (un solo agregado, transaccional) o **eventual** (cruza fronteras, enforcement por proyección).

| # | Invariante | Tipo | Agregado | Referencia |
|---|-----------|------|----------|------------|
| `I01` | **Transiciones FSM válidas de unidad.** Solo se permiten las 7 transiciones documentadas en Sección 4.1 (`Borrador → Activa`, `Borrador → Descartada`, `Activa → Suspendida`, `Suspendida → Activa`, `Activa → Inactiva`, `Suspendida → Inactiva`, `Inactiva → Activa`). Cualquier otra es rechazada por el agregado antes del append. | Local | `UnidadOrganizacional` | `[R12]` |
| `I02` | **Transiciones FSM válidas de grupo.** Solo se permiten `Activo → Inactivo` y `Inactivo → Activo`. | Local | `GrupoOrganizacional` | `[R18]` |
| `I03` | **Formato del código.** El código es alfanumérico de longitud entre 4 y 12 caracteres. La longitud específica admitida por tenant es parametrizable dentro de ese rango. | Local | Ambos | `[R10]` |
| `I04` | **Inmutabilidad del código.** Una vez asignado, el código de una unidad o grupo no se modifica en ningún comando posterior. | Local | Ambos | `[R09]` |
| `I05` | **Coherencia `motivoBaja` ↔ flujo.** Cuando `estado in {Inactiva, Descartada}`, el atributo `motivoBaja` está definido y su valor corresponde al flujo que disparó la baja (`operativa` desde F7 o F8 manual; `fusion` desde F12; `division` desde F13; `abandono_por_inactividad` desde `[SI05]` o F10 cascada para Borradores). | Local | `UnidadOrganizacional` | `[R26]` |
| `I06` | **Reapertura requiere padre activo.** Un comando `ReabrirUnidad` solo se acepta si el `grupoPadreId` está en `Activo` al momento del comando. | Eventual | `UnidadOrganizacional` | `[R16]` |
| `I07` | **Datos mínimos para activación.** Una unidad solo transiciona a `Activa` si `codigo`, `nombre`, `tipoUnidad` y `grupoPadreId` están definidos y el tipo está vigente en el catálogo del padre o ancestros. | Local | `UnidadOrganizacional` | Flujo 3 (activación), alcance |
| `I08` | **Fecha efectiva no anterior al historial.** En F12, F13 y F14, la `fechaEfectiva` no puede ser anterior a la última transacción registrada en las unidades involucradas ni a la última versión vigente de jerarquía. La validación se hace contra la **proyección local de última imputación** (`[SI10]`), que Estructura Organizacional mantiene por suscripción a los eventos de imputación de los consumidores — nunca por consulta en caliente. | Eventual / Cross-domain | `UnidadOrganizacional` + consumidores | `[R25]`, `[SI10]` |
| `I09` | **Unicidad de código por tenant.** El `codigo` es único dentro del tenant cruzando grupos y unidades. Las unidades en `Descartada` se excluyen del índice para liberar la identificación (`[R11]`). Enforcement por proyección (`[SI01]`) porque cruza dos agregados. | Eventual | Ambos | `[R08]`, `[R11]` |
| `I10` | **Padre activo al crear/trasladar/reabrir.** Al crear (F1, F9), trasladar (F14) o reabrir (F6) un nodo, el grupo padre destino debe estar en `Activo`. Enforcement por proyección (`[SI03]`) porque cruza dos agregados. | Eventual | Ambos | `[R07]`, `[R16]` |
| `I11` | **No ciclos en la jerarquía.** Un grupo no puede ser su propio ancestro directo ni indirecto. La validación se hace al recibir comandos que cambian la jerarquía (`CrearGrupo`, `TrasladarUnidad`) consultando la proyección (`[SI02]`). | Eventual | `GrupoOrganizacional` | `[R04]` |
| `I12` | **Integridad de la cascada de inactivación.** Cuando `GrupoInactivado` se emite por la saga `CascadaInactivacionGrupo`, todos los descendientes vivos al momento de la captura reciben su propio evento (`GrupoInactivado` para sub-grupos, `UnidadInactivada` o `UnidadDescartada` para unidades) correlacionado por `correlationId`. No quedan nodos descendientes en estado operativo bajo un grupo inactivado. | Eventual | Ambos (vía saga) | `[R19]` |
| `I13` | **Grupo raíz único y protegido.** Cada tenant tiene exactamente un grupo con `esRaiz = true`. Ese grupo no puede inactivarse mientras existan otros nodos colgando de él. | Eventual | `GrupoOrganizacional` | `[R02]`, `[R03]` |
| `I14` | **Reestructuración: estados y separación origen/destino.** En F12 y F13, las unidades origen están en `Activa` o `Suspendida`, todas las destino están en `Activa`, y no hay intersección entre el conjunto origen y el conjunto destino. | Local | `UnidadOrganizacional` (validado en saga `ReestructuracionUnidad`) | `[R22]`, `[R23]`, `[R24]` |
| `I15` | **Modificación bloqueada en estados terminales.** Las unidades en `Inactiva` o `Descartada` no aceptan `UnidadModificada`. La unidad debe ser reabierta primero (F6 → F15 → F7) si el cambio es necesario. | Local | `UnidadOrganizacional` | `[R17]` |
| `I16` | **Convergencia de la cascada de inactivación.** Toda saga `CascadaInactivacionGrupo` que emite el evento `GrupoInactivado` raíz **converge en estado coherente**: todos los descendientes capturados al inicio terminan en estado no operativo (`Inactivo`, `Inactiva` o `Descartada`) o registrados como `dead-letter` para revisión humana. No quedan descendientes en limbo indefinido. Enforcement por la persistencia de la saga (`[SI08]`) + alerta operacional tras N minutos sin completar. | Eventual (Saga) | Ambos (vía `CascadaInactivacionGrupo`) | `[R19]`, `[R20]` |

---

## 8. Qué NO contiene este documento

| Concepto | Razón | Donde sí vive | Evento de integración esperado (cuando exista) |
|----------|-------|---------------|------------------------------------------------|
| Direcciones físicas de las unidades | Estructura Organizacional no atribuye direcciones a unidades. | Servicio compartido de Direcciones; cada consumidor que requiera la dirección la gestiona en su contexto (ej: ABR para inmuebles). | — (no requiere integración con Estructura Organizacional) |
| Reglas tributarias asociadas al tipo de unidad | Las consecuencias fiscales de un tipo de unidad pertenecen al sub-dominio fiscal. | Sub-dominio de Impuestos. | — (Impuestos consume `UnidadActivada` para enriquecer el contexto fiscal por unidad, sin requerir nuevos eventos) |
| Reglas contables de derivación (plantillas de asiento, mapeo de cuentas) | El motor contable consume la unidad como dimensión de imputación y aplica sus reglas. | Sub-dominio de Contabilidad. | — (Contabilidad consume los 18 eventos existentes; no requiere nuevos) |
| Presupuesto y planeación por unidad | No se almacenan montos planeados ni ejecuciones en este sub-dominio. | Sub-dominio futuro de Presupuesto / Planeación. | `PresupuestoAprobado` → habilita `[PD01]` (activación automática por contexto inequívoco) |
| Gestión de empleados asociados a unidades | La pertenencia de un empleado a una unidad vive donde se gestionan los empleados. | Sub-dominio futuro de Nómina / RRHH. | `EmpleadoAsignadoAUnidad` / `EmpleadoDesasignadoDeUnidad` (consumidos por Estructura para reportería de cobertura) |
| Reportería consolidada y dashboards | Este sub-dominio expone consultas básicas; la reportería avanzada (agregaciones multi-dimensión, comparativos históricos, exportes regulatorios) es transversal. | Capa de reportería del ERP / herramientas BI. | — (consume eventos del modelo; no genera entrantes) |
| Dimensiones de imputación distintas a "Unidad Organizacional" en F1 | En F1 solo se expone esa dimensión. | Sub-dominios futuros (Proyecto, Sucursal como entidad separada, Línea de Negocio, etc.), cada uno owner de su dimensión. Ver `[DA4]`. | `DimensionAgregadaAlContrato` (de plataforma) → habilita `[PD03]` (multi-dimensionalidad) |
| Capa BFF (Backend for Frontend) | Es infraestructura de aplicación que orquesta la experiencia de usuario del propio módulo. No es parte del dominio. La orquestación BFF de creación desde consumidores quedó superada por `[D15]` (la demanda es una señal informativa, no un comando vía BFF). | Infraestructura de aplicación del módulo. | — (infraestructura; sin eventos de dominio) |
| Sistema inteligente (sugerencias, pre-llenado, advertencias de impacto) | Es transversal del producto, no del dominio. Los flujos del alcance mencionan sus intervenciones; el modelo de dominio no las codifica. | Infraestructura transversal del producto. | — (consume eventos para entrenamiento; no genera entrantes) |
| Auditoría externa (cumplimiento SOX, logs regulatorios externos) | El modelo emite eventos auditables; la persistencia auditable de largo plazo es transversal. | Infraestructura de auditoría del ERP. | — (consume todos los eventos del modelo) |

---

## 9. Decisiones de arquitectura y diseño

| # | Decisión | Justificación | Referencia |
|---|----------|---------------|------------|
| `D01` | **Dos agregados raíz** (`GrupoOrganizacional` + `UnidadOrganizacional`) en lugar de un único agregado monolítico. | El agregado único produciría streams masivos y concurrencia mala en empresas con miles de unidades. Dos agregados mantienen el ciclo de vida claro de cada uno y permiten concurrencia por nodo. Las invariantes que cruzan fronteras (unicidad de código por tenant, padre activo, no ciclos) se materializan como eventuales con enforcement por proyección (`[SI01]`, `[SI02]`, `[SI03]`). | Plan, sección "Decisiones cerradas" |
| `D02` | **Codificación plana + jerarquía versionada aparte** (referencia al anexo arquitectónico, decisión 1). | Sin techo combinatorio del posicional; reestructuración limpia; comparabilidad IFRS 8. | `[DA1]` |
| `D03` | **FSM de 5 estados para unidad con `Inactiva` reabrible y `Descartada` único terminal estricto** (referencia al anexo arquitectónico, decisión 2). | Modela momentos transitorios reales; permite recuperar errores de cierre operativo conservando historial; impide reabrir lo que nunca operó. | `[DA2]` |
| `D04` | **Reestructuración como eventos de dominio de primera clase** (referencia al anexo arquitectónico, decisión 3). | Trazabilidad histórica + cumplimiento IFRS 8 §29-30 sin reconstrucción manual. | `[DA3]` |
| `D05` | **Modelo multi-dimensional desde el diseño** (referencia al anexo arquitectónico, decisión 4). En F1 se expone solo `Unidad Organizacional`. El contrato de líneas de traducción con Contabilidad acepta solo esa dimensión. | El costo de prever extensibilidad en F1 es bajo; el costo de migrar de mono-jerárquico a multi-dimensional en producción es altísimo. | `[DA4]` |
| `D06` | **Causa de baja (`motivoBaja`) proyectada como atributo del modelo de lectura, no como estado FSM.** Patrón canónico DDD/ES/CQRS: la FSM modela comportamiento permitido; los atributos enriquecidos modelan información contextual. | Mantiene la FSM minimal y extensible. Agregar un nuevo motivo no infla la FSM; solo agrega un valor al enum del catálogo de motivos. | Sección 4 (Familia 3) del alcance |
| `D07` | **Eventos `*Modificado` capturan delta**, no snapshot completo. El estado se reconstruye reproduciendo el stream. | Decisión transversal del proyecto. Reduce el tamaño del stream y permite identificar qué cambió específicamente. | Decisión del usuario (MEMORY.md) |
| `D08` | **Un solo evento `UnidadDescartada` cubre rechazo del admin y abandono por inactividad.** El distinguidor del motivo va en el atributo `motivoBaja` (`operativa` vs `abandono_por_inactividad`). **Alcance específico de esta decisión:** no existe un evento explícito separado de rechazo (`UnidadRechazada`) — el evento `UnidadDescartada` se unifica con diferenciación por atributo. La política operativa de descarte automático en sí (cuándo y cómo se ejecuta, umbrales, proceso programado) se resuelve por separado en `[D09]` + `[SI05]`. Los dos cierres son complementarios y cubren aspectos distintos de la misma área. | Evita inflar el catálogo de eventos. La auditoría diferencia los casos vía el atributo proyectado, no vía evento separado. | Plan, decisión cerrada con el usuario. |
| `D09` | **Descarte automático de Borradores por inactividad** mediante proceso programado del propio sub-dominio (`[SI05]`). Política: borrador con `fechaUltimaActividadBorrador` > umbral configurado por tenant (default sugerido 30 días) → emite `UnidadDescartada` con `motivoBaja: "abandono_por_inactividad"`. | Reduce ruido operativo de Borradores abandonados. El umbral es parametrizable para que cada tenant ajuste según su realidad operativa. | Plan, decisión cerrada con el usuario. |
| `D10` | **Catálogo de tipos de unidad como entidad interna del agregado `GrupoOrganizacional`**. Se administra al nivel del grupo raíz y se hereda hacia sub-grupos. | El catálogo es configuración estructural del árbol y calza con el rol de `GrupoOrganizacional` como nodo agrupador. Evita un tercer agregado (`CatalogoTiposUnidad`) y mantiene la cardinalidad del bounded context controlada. La opción "agregado separado" se evaluó y descartó por tres razones: (1) **alta cohesión funcional** — los tipos se consultan junto con la jerarquía en cada creación de unidad; (2) **sin ciclo de vida independiente** — los tipos no tienen estados ni transiciones complejas (solo `Activo`/`Inactivo` en la entidad interna `TipoUnidad`); (3) **simplificación operacional** — un solo punto de administración (el grupo raíz) en lugar de coordinar dos agregados. Esta decisión reconoce el trade-off: el agregado `GrupoOrganizacional` carga dos responsabilidades (estructura jerárquica + catálogo de tipos). Si en el futuro el catálogo crece en complejidad (tipos con sub-tipos, reglas de aplicabilidad por país, etc.), se evaluará separarlo en F2+. | Plan, decisión cerrada con el usuario; reforzada tras auditoría con análisis de alternativas. |
| `D11` | **Mecanismos de plataforma (concurrencia optimista, idempotencia técnica, retry) viven como `[SI##]`**, no se especifican por evento ni como invariantes del dominio. | Decisión transversal del proyecto. Las invariantes y reglas pertenecen al dominio; los mecanismos de plataforma son sugerencias de implementación que materializan invariantes (especialmente las eventuales). | Decisión del usuario (MEMORY.md) |
| `D12` | **Cascada de inactivación de grupos modelada como saga** (`CascadaInactivacionGrupo`). Emite un evento por nodo afectado (no un evento agregado tipo `GrupoInactivadoEnCascada`) para que los consumidores puedan reaccionar granularmente sin parsear listas. Sin cascada inversa al reactivar (`[R20]`); el sistema inteligente identifica candidatos a reabrir vía `correlationId` correlacionado (`[SI08]`). | Granularidad de eventos = granularidad de reacción. La asimetría reactivación-sin-cascada es coherente con que `Inactiva` no es terminal en F1 — el admin puede reabrir hijos que vea pertinentes sin que el sistema lo presuma. | Plan, decisión cerrada con el usuario en familia 2 grupos |
| `D13` | **Reactivación de tipos de unidad no modelada en F1.** La FSM de `TipoUnidad` tiene transición `Activo → Inactivo` (vía `TipoUnidadInactivado`) pero no la inversa. El alcance v1.0 no la requiere — no hay un caso de negocio identificado que justifique reactivar un tipo previamente inactivado. | Si en F2+ el negocio lo solicita, se evalúa modelarlo como evento `TipoUnidadReactivado` análogo a `GrupoReactivado`, con transición `Inactivo → Activo` en la FSM de `TipoUnidad`. No se mantiene como pendiente formal (`[PD##]`) porque no hay demanda actual ni horizonte definido — es una extensión natural si surge. La asunción de F1 es que `TipoUnidadInactivado` es terminal. | Auditoría Bloque Media, M9. |
| `D14` | **Herencia dinámica del catálogo de tipos desde el grupo raíz** (sin replicación). El catálogo `tiposUnidad` vive solo en el grupo raíz; los sub-grupos lo heredan dinámicamente vía el comportamiento `tiposVigentes()` que recorre la jerarquía hasta el raíz por la proyección `[SI02]`. | Fuente única — modificar el catálogo del raíz se refleja inmediatamente en todos los sub-grupos sin migración explícita. Evita problemas de desincronización entre raíz y sub-grupos. El costo de lectura es despreciable porque la jerarquía es poco profunda (raramente >5 niveles en empresas de hasta 2.000 unidades, `[P04]`) y la proyección de jerarquía ya está materializada. Alternativa descartada: copia estática al crear sub-grupo (requiere migración manual si el raíz cambia y produce versiones divergentes del catálogo por sub-grupo). | Auditoría Bloque Baja, B3. |
| `D15` | **La unidad organizacional es un dato gobernado con dueño único (Estructura Organizacional); los consumidores operan contra copia local, difieren cuando falta y nunca crean ni bloquean.** Cuatro consecuencias de modelo: **(1) Dueño único** — solo Estructura Organizacional crea, modifica y da de baja unidades; ningún consumidor las origina. **(2) Copia local por eventos** — OXP, Contabilidad y futuros consumidores mantienen su copia de unidades por suscripción a los eventos de ciclo de vida y operan contra ella; nunca consultan a Estructura Organizacional en el camino crítico (`[R13]`, `[SI12]` repara la copia de fondo). **(3) Diferir, no bloquear** — cuando un consumidor necesita una unidad que aún no existe, registra lo que puede y difiere solo la parte que la requiere; esa parte se resuelve sola cuando llega `UnidadActivada` a su copia local (consistencia eventual). La unidad es parte de la integridad del hecho del consumidor y debe coincidir exacto con la contabilidad, así que **no se aproxima con un valor de tránsito o provisional** —aproximarla desconciliaría operación y contabilidad— (estrategia "diferir" de la guía `datos-entre-dominios.md`). **(4) Demanda por señal informativa** — la necesidad de una unidad inexistente se hace visible como señal no bloqueante (`[R30]`, `[SI11]`) que solo alimenta la bandeja de sugerencias; **no es un comando, no crea nada y la corrección del sistema no depende de ella**. La última imputación para reestructurar se valida contra una proyección local (`[SI10]`), no por consulta en caliente. | Elimina los acoplamientos de ejecución y proceso entre Estructura Organizacional y los consumidores (issue #45/#46): el consumidor nunca queda detenido por la disponibilidad ni por el ciclo de creación de Estructura Organizacional, y Estructura Organizacional no queda atada a la disponibilidad de los consumidores para reestructurar. Reemplaza el patrón anterior (creación desde consumidor vía BFF → unidad en `Borrador` → activación → cancelación en cascada al descartar), que acoplaba la operación del consumidor al gesto humano del administrador. Fundamento completo en [`../../guias-de-modelado/datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md). | Replanteamiento #46; guía `datos-entre-dominios.md`. |

---

## 10. Premisas de negocio

| # | Premisa | Impacto en el modelo |
|---|---------|---------------------|
| `P01` | **Una unidad puede atravesar pausa/reapertura múltiples veces durante su vida operativa.** Sucursales estacionales, proyectos por fases, áreas que se reorganizan cíclicamente. | Justifica que `Inactiva` no sea terminal estricto y que existan dos eventos diferenciados (`UnidadReactivada` desde pausa transitoria, `UnidadReabierta` tras cierre) para auditoría. |
| `P02` | **La estructura jerárquica de las empresas evoluciona constantemente.** Fusiones, divisiones, traslados; las empresas reorganizan sus áreas con frecuencia. | Justifica la jerarquía versionada (`[DA1]`) y los tres procesos de reestructuración como eventos de dominio de primera clase. |
| `P03` | **La identificación de una unidad debe ser estable a lo largo del tiempo.** Auditorías retrospectivas pueden ocurrir años después; la comparabilidad histórica IFRS 8 exige que cada periodo se reporte con la estructura de entonces. | Justifica la inmutabilidad del código (`I04`), la conservación del historial al inactivar (`Inactiva` no borra; `[R27]` historial referenciado al origen en F12/F13) y la fecha efectiva como eje de la jerarquía versionada. |
| `P04` | **Las empresas operan con estructuras de hasta ~2.000 unidades organizacionales por empresa con jerarquías de múltiples niveles.** Límite documentado en el alcance. | Acota el dimensionamiento de las proyecciones (`[SI01]`-`[SI04]`) y los costos esperados de la cascada (`CascadaInactivacionGrupo` debe manejar miles de descendientes en grupos amplios). |
| `P05` | **La demanda de unidades inexistentes desde la operación es habitual**, no excepcional. OXP y Contabilidad encuentran con frecuencia documentos asociados a unidades aún no registradas. La diferencia con F1 es que esa demanda **no crea** la unidad ni detiene la operación del consumidor. | Justifica que la demanda tenga un canal de primera clase sin acoplar: el consumidor difiere su operación (`[D15]`, `[R29]`), hace visible la necesidad como señal no bloqueante (`[R30]`) y Estructura Organizacional la proyecta en la bandeja de sugerencias (`[SI11]`, idempotente por `[SI07]`). La creación sigue siendo acto deliberado del administrador. |

---

## 11. Pendientes por definir

| # | Pendiente | Tipo | Contexto | Trigger de activación |
|---|-----------|------|----------|----------------------|
| `PD01` | **Política de activación automática por sistema inteligente.** En F1 la activación es siempre humana. La activación automática (cuándo el sistema inteligente puede asumir el gesto sin intervención del administrador, p.ej. proyectos aprobados en un sistema de presupuesto con todos los datos) se evaluará en fases posteriores. | Futuro (F2+) | El alcance documentó que en F1 la activación es siempre humana. El sub-dominio queda preparado: el comando `ActivarUnidad` ya acepta `usuarioId: "sistema"`, pero las reglas de cuándo el sistema actúa no se definen aquí. | Cuando se integre el primer sub-dominio de presupuesto/aprobación que pueda servir como fuente inequívoca de contexto. Owner sugerido: equipo de producto + sub-dominio de Presupuesto. |
| `PD02` | **Contratos formales de eventos hacia consumidores.** Los 18 eventos están especificados aquí con su payload. La versión contractual externa (schemas versionados, compatibilidad hacia atrás, registry) se formaliza en EventCatalog (Fase 3 del proyecto). | Fase 3 (EventCatalog) | El proyecto tiene Fase 3 reservada para EventCatalog. Este pendiente conecta el modelo con esa fase. | Cuando se inicie la Fase 3 del proyecto. Owner: equipo de plataforma de eventos. |
| `PD03` | **Extensibilidad de la dimensión "Unidad Organizacional" a otras dimensiones ortogonales** (Proyecto, Sucursal como entidad separada, Línea de Negocio). En F1 solo se expone "Unidad Organizacional"; el modelo está preparado para extender el contrato de líneas de traducción con campos opcionales adicionales sin rediseño estructural (`[DA4]`). | Futuro (F2+) | El alcance lo deja explícito en "Fases futuras" sin compromiso de tiempo. La extensión requerirá construir el sub-dominio owner de cada dimensión nueva. | Cuando aparezca demanda real de una segunda dimensión por parte de un consumidor (típicamente desde Contabilidad u OXP cuando una empresa requiera cruzar costos por proyecto + sucursal + unidad). Owner: equipo de arquitectura. |

---

## 12. Catálogo de permisos atómicos del dominio

El bounded context de Estructura Organizacional protege dos recursos principales: `GrupoOrganizacional` y `UnidadOrganizacional`. Adicionalmente, el catálogo interno de tipos de unidad se protege como sub-recurso del grupo.

**Convención:** `accion_recurso` en snake_case. Compatible con OAuth scopes, policy engines (OPA, Cedar) y motores ReBAC.

**Restricción de contexto:** todos los permisos se aplican dentro del scope `tenant` (cada tenant tiene su propia estructura organizacional independiente).

| Recurso | Acción | Identificador |
|---------|--------|---------------|
| Grupo organizacional | Crear | `crear_grupo` |
| Grupo organizacional | Modificar | `modificar_grupo` |
| Grupo organizacional | Inactivar (con cascada) | `inactivar_grupo` |
| Grupo organizacional | Reactivar | `reactivar_grupo` |
| Grupo organizacional | Consultar | `consultar_grupo` |
| Grupo organizacional | Consultar jerarquía completa | `consultar_jerarquia` |
| Tipo de unidad (sub-recurso del grupo) | Agregar | `agregar_tipo_unidad` |
| Tipo de unidad | Modificar | `modificar_tipo_unidad` |
| Tipo de unidad | Inactivar | `inactivar_tipo_unidad` |
| Unidad organizacional | Crear (F1 — directa o desde la bandeja de sugerencias) | `crear_unidad` |
| Unidad organizacional | Activar | `activar_unidad` |
| Unidad organizacional | Suspender | `suspender_unidad` |
| Unidad organizacional | Reactivar (desde Suspendida) | `reactivar_unidad` |
| Unidad organizacional | Reabrir (desde Inactiva) | `reabrir_unidad` |
| Unidad organizacional | Inactivar | `inactivar_unidad` |
| Unidad organizacional | Descartar (Borrador) | `descartar_unidad` |
| Unidad organizacional | Modificar | `modificar_unidad` |
| Unidad organizacional | Fusionar | `fusionar_unidades` |
| Unidad organizacional | Dividir | `dividir_unidad` |
| Unidad organizacional | Trasladar (cambiar grupo padre) | `trasladar_unidad` |
| Unidad organizacional | Consultar | `consultar_unidad` |
| Configuración del sub-dominio | Configurar umbral de expiración de Borradores | `configurar_umbral_expiracion_borrador` |

Total: **22 permisos atómicos**.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-05-27 | Versión inicial del modelo de dominio. Cubre las 12 secciones canónicas alineadas con el patrón de Terceros. **2 agregados raíz** (`GrupoOrganizacional` con FSM 2 estados + entidad interna `TipoUnidad`; `UnidadOrganizacional` con FSM 5 estados), **5 value objects compartidos**, **18 eventos** (11 de unidad + 4 de grupo + 3 de configuración de tipos), **15 invariantes** (locales y eventuales), **12 decisiones de modelo** + 4 heredadas del anexo arquitectónico v1.2, **5 premisas**, **3 pendientes** (PD01 activación automática, PD02 contratos formales EventCatalog, PD03 multi-dimensionalidad), **23 permisos atómicos**, **8 sugerencias de implementación** y **3 domain services** (cascada de inactivación, reestructuración, descarte automático de Borradores). Cierra `[PD2]` y `[PD3]` del `anexo-orquestacion-creacion.md` v1.0 con `[D09]` y `[D08]` respectivamente; mantiene abierto `[PD1]` como `[PD01]` de este modelo. Listo para auditoría completa (11 skills) en sesión dedicada. |
| 1.1 | 2026-05-27 | **Aplicado bloque Alta de la auditoría (10 temas, 25 hallazgos resueltos).** **A1 — Self-contained events:** payloads ampliados con códigos, tipos, padres y fecha efectiva en `UnidadActivada`, `UnidadInactivada`, `UnidadDescartada`, `UnidadFusionada`, `UnidadDividida`, `UnidadTrasladada`, `GrupoInactivado`. **A2 — Concurrencia optimista (`[SI06]`):** documentado cómo se devuelve `versionAgregado` en metadata, qué comandos requieren `expectedVersion` obligatorio/recomendado, comportamiento ante `version-conflict`. **A3 — Idempotencia de comandos (`[SI07]`):** lista de comandos cubiertos, tabla `ComandoRecibido` con TTL, distinción admin/consumidor/proceso interno. **A4 — `DescarteAutomaticoBorradores` (`[SI05]` + Sección 3.6):** estado persistido (`JobDescarteEjecucion`, `JobDescarteUnidadProcesada`), idempotencia por `jobExecutionId`, tabla de compensación, trigger con lock distribuido. **A5 — Sagas con convergencia (`[SI08]` + Sección 3.6 + nueva invariante `[I16]`):** política at-least-once con convergencia eventual, max retries y backoff exponencial, alerta operacional, sin compensación inversa. **A6 — God Aggregate y lógica fugada:** `[D10]` reforzada con análisis de alternativas; `CascadaInactivacionGrupo` invoca `descendientesAfectablesPorCascada()` del agregado; nuevo método `puedeDescartarseAutomáticamente(umbral)` en `UnidadOrganizacional`. **A7 — Asimetría FSM grupo/unidad:** notas explícitas en Sección 4.1 y 4.2 documentando la asimetría como intencional. **A8 — `causalidadBaja` distribuida:** nota en `UnidadInactivada` aclarando la distribución entre stream del proceso y stream del agregado. **A9 — Invariantes eventuales:** `[SI01]` con orden transaccional y SLA de remoción tras `UnidadDescartada`; `[SI03]` con SLA de actualización y estrategia ante stale. **A10 — Semántica de eventos:** Sección 2.3.1 nueva con formato canónico de delta (`changes` map); `esCascada` y `correlationId` estandarizados en `UnidadInactivada`, `UnidadDescartada` y `GrupoInactivado`; `[R15]` verificado en alcance. Nuevos comportamientos calculados: `puedeFusionarse`, `puedeDividirse`, `puedeTrasladarse`, `puedeDescartarseAutomáticamente` en `UnidadOrganizacional`; `descendientesAfectablesPorCascada` en `GrupoOrganizacional`. Conteos actualizados: **16 invariantes** (+1: `[I16]`). |
| **1.2** | **2026-05-27** | **Aplicado bloque Media de la auditoría (11 temas, 48 hallazgos resueltos).** **M1 — Composición:** `FechaEfectiva` agregada a composición de `UnidadOrganizacional`; aclarado `tiposUnidad` (reconstruye desde eventos + proyección); nota de datos transaccionales no almacenados; `origenSolicitud` documentado como atributo almacenado. **M2 — Coherencia de baja:** método `validarCoherenciaBaja()` y atributos write-once formalizados en `UnidadOrganizacional`. **M3 — Saga deduplication:** nueva `[SI09]` con tabla `SagaEventEmitted` para dedup en re-emisión de eventos derivados. **M4 — Ordenamiento y race (decisiones C+B):** `CascadaInactivacionGrupo` y `ReestructuracionUnidad` esperan write-ack del broker del raíz/evento de proceso antes de emitir derivados; los consumidores reconcilian orden lógico (no se asume orden de entrega); coexistencia con `[SI05]` resuelta por dedup natural del guard del agregado, sin proyección adicional. **M5 — FSM Unidad:** aclarado qué reportes filtran `Descartada` y cuáles la incluyen. **M6 — FSM Grupo:** documentado que el catálogo de tipos queda congelado durante `Inactivo`; precondiciones de los 3 eventos `TipoUnidad*` actualizadas. **M7 — Eventos de proceso:** notas en `UnidadFusionada` y `UnidadDividida` aclarando que viven en stream propio sin agregado backend formal. **M8 — `GrupoReactivado` (decisión B):** payload mínimo mantenido; lista de hijos afectados como proyección consultable interna del sub-dominio (no en payload externo). **M9 — Decisiones formales:** nueva `[D13]` (reactivación de tipos no modelada en F1); `[D08]` ampliada con alcance específico vs `[D09]`. **M10 — Naming y mapping:** estandarizado `[PD01]/[PD02]/[PD03]` en modelo y anexo de orquestación; agregada nota de mapping de pendientes cerrados al inicio de Sección 11. **M11 — Contrato BFF (decisión C):** endpoint `verificarDisponibilidadCodigo(codigo)` del servicio expuesto y consumido por el BFF en el Flujo F2 (best-effort; el servicio sigue siendo la autoridad única de unicidad). Conteos actualizados: **9 sugerencias de implementación** (+1: `[SI09]`), **13 decisiones** (+1: `[D13]`). Anexo de orquestación bumpeado a v1.1. |
| **1.3** | **2026-05-28** | **Aplicado bloque Baja de la auditoría (7 temas, 28 hallazgos resueltos).** **B1 — Cleanups en composición y VOs:** `versionAgregado` aclarado como atributo interno de plataforma (no aparece en payloads); `nivel` aclarado como proyectado vía `[SI02]`; `estadoInicial` en `UnidadCreada` aclarado como parámetro de comando (no atributo persistente); VO `Codigo` explicita "sin estructura jerárquica embebida". **B2 — FSM y eventos `TipoUnidad*`:** diagrama FSM de Unidad redibujado para mostrar `Inactiva → Activa` simétricamente; nota terminológica en Sección 5.4 aclarando que los eventos `TipoUnidad*` son "eventos de configuración del catálogo" (cambian la entidad interna `TipoUnidad`, no el estado del grupo). **B3 — Herencia dinámica del catálogo de tipos (decisión A):** nueva `[D14]` formaliza que el catálogo `tiposUnidad` vive solo en el grupo raíz y los sub-grupos lo heredan dinámicamente vía `tiposVigentes()` (sin replicación, fuente única, sin migración). **B4 — Saga: persistencia y claves de idempotencia:** `[SI08]` ampliada con descripción del estado persistido (`SagaCascadaEstado` en tabla relacional) y del proceso de health-check que dispara alertas; clave de idempotencia explícita `(agregadoId, correlationId)` donde `agregadoId` es `grupoId` o `unidadId` según el caso; `ReestructuracionUnidad` con clave de idempotencia explícita en paso 3 (`(processStreamId, correlationId, eventType)`). **B5 — Vinculación SIs ↔ comandos:** nueva tabla al inicio de Sección 3.5 mapeando cada comando a las sugerencias de implementación aplicables. **B6 — Sección 8 ampliada** con columna "Evento de integración esperado" para sub-dominios futuros (`PresupuestoAprobado`, `EmpleadoAsignadoAUnidad`, `DimensionAgregadaAlContrato`). **B7 — Limpiezas finales:** consolidada la descripción de `DescarteAutomaticoBorradores` en Sección 3.6 (detalles operativos viven en `[SI05]` para evitar duplicación); glosario del alcance (`definicion-alcance.md` v1.1) actualizado en término 22 ("Unidad de imputación"). Conteos actualizados: **14 decisiones** (+1: `[D14]`). |
| **1.4** | **2026-05-28** | **Aplicados 4 ajustes del comité de producto (D1-D4) sobre el modelo.** **D1 — Eliminada la duplicidad de los servicios** `ReestructuracionUnidad` y `DescarteAutomaticoBorradores` en Sección 3.6 (versiones obsoletas que habían quedado por error tras el Bloque Alta). Conservadas únicamente las versiones completas con write-ack, punto de no retorno, tabla de compensación detallada, idempotencia explícita y protocolo de proceso. **D2 — Conteo de invariantes** corregido en el encabezado de Sección 7 (15 → 16) para alinear con la realidad de la tabla (`[I01]`-`[I16]`). **D3 — Reclasificación de `[I08]`** de Local a Eventual / Cross-domain: la validación de "fecha efectiva no anterior al historial" requiere consultar información transaccional que vive en los sub-dominios consumidores, no en `UnidadOrganizacional`; agregado en agregado afectado `UnidadOrganizacional + consumidores` y referencia adicional a `[SI10]`. **D4 — Nueva sugerencia de implementación `[SI10]` Proyección de última imputación por unidad** que materializa `[I08]` y `[R25]`; contrato mínimo `obtenerUltimaImputacion(unidadId, tenantId) → fecha | null`; tres opciones de implementación (proyección transversal, consulta federada, proyección materializada del motor contable); política de rechazo por defecto si el contrato no está disponible; nota cruzada con Sección 7 del alcance. Tabla de mapeo SIs ↔ comandos actualizada para incluir `[SI10]` en F12, F13 y F14. **Conteos actualizados: 10 sugerencias de implementación** (+1: `[SI10]`); las **16 invariantes** ahora reflejan correctamente que `[I08]` es Eventual / Cross-domain. Alcance bumpeado a v1.2 con los 11 ajustes A1-A11 + consecuencia de D4 en Sección 7. |
| **1.5** | **2026-06-19** | **Replanteamiento — eliminación de acoplamientos de ejecución y proceso con los consumidores (issue #45/#46), Hito 2 (modelo).** Acompaña al alcance v1.3. **Nueva decisión `[D15]`** que fija el modelo: unidad = dato gobernado con dueño único; consumidores con copia local por eventos; **diferir** (no bloquear ni aproximar) cuando falta la unidad; **demanda por señal informativa** (no comando, no crea nada). **Eliminado el patrón viejo de creación desde consumidor:** se retiran el comando `SolicitarCreacionDeUnidad`, el atributo almacenado `origenSolicitud`, el VO `OrigenSolicitud` y la cancelación en cascada al descartar; `UnidadCreada`/`UnidadActivada`/`UnidadDescartada` limpiados de F2/BFF/`origenSolicitud`; `Borrador` acotado a preparación del administrador (FSM, notas); `I10` sin F2. **`[SI07]` reorientada** de idempotencia del comando a idempotencia de la **señal de demanda** (`SenalRecibida`, materializa `[R30]`); se retira el endpoint BFF `verificarDisponibilidadCodigo`. **`[SI05]` y el servicio `DescarteAutomaticoBorradores` simplificados** (sin notificación/compensación a consumidores: un borrador no es referenciado por nadie). **`[SI04]`** acotada a borradores del administrador. **`[SI10]` cerrada** a una proyección local de última imputación alimentada por eventos de imputación entrantes (se retiran la consulta federada y la política de "rechazar si el consumidor está inaccesible"); `I08` ajustada. **Nuevas `[SI11]`** (bandeja de sugerencias de creación, proyección de la señal entrante) y **`[SI12]`** (punto de resincronización de respaldo, Capa 2 de la guía). **`I07`** deja de referenciar `[R29]` (cambió de significado) → Flujo 3 de activación. **`P05`** reescrita (demanda habitual sin crear ni bloquear). **Permiso `solicitar_creacion_unidad` eliminado.** Fundamento en `guias-de-modelado/datos-entre-dominios.md`. Conteos actualizados: **12 sugerencias de implementación** (+2: `[SI11]`, `[SI12]`), **15 decisiones** (+1: `[D15]`), **22 permisos atómicos** (−1). Catálogo de 18 eventos propios sin cambios (la señal de demanda y los eventos de imputación son entrantes, de otros dominios, documentados en `[SI11]`/`[SI10]`). **Eliminado el `anexo-orquestacion-creacion.md`** (superado por `[D15]`; su contenido histórico vive en git) y limpiadas sus referencias vivas en `[D08]`, `[D09]`, `[SI05]`, `UnidadDescartada`, `PD01`, la Sección 11 y el documento consolidado del ERP. **Tras revisión del PR #50:** la guía `datos-entre-dominios.md` se sube a la tabla de documentos relacionados (Sección 1) para que el *por qué* de la copia local sea visible; nueva sub-sección **3.8 — Comportamiento de integración con consumidores** con diagrama (ASCII) del flujo copia local + diferir + señal, mostrando los dos caminos independientes (corrección / visibilidad). |
