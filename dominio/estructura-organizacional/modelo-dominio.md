# Modelo de Dominio — Estructura Organizacional

**Versión:** 2.2
**Fecha:** 2026-07-08

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
- **Agregados:** PascalCase; corresponden a los términos del glosario canónico (Sección 2 del alcance). Tres agregados raíz: `GrupoOrganizacional`, `UnidadOrganizacional` y `TipoUnidad`.
- **Referencias:**
  - `[R##]` reglas de negocio (alcance, Sección 6).
  - `[D##]` decisiones de este modelo (Sección 9).
  - `[DA##]` decisiones del anexo arquitectónico (`anexo-decisiones-arquitectonicas.md`).
  - `[I##]` invariantes del dominio (Sección 7).
  - `[SI##]` sugerencias de implementación (Sección 3.6).
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
| **motivoBaja** | Atributo del modelo de lectura (no estado FSM) que registra por qué una unidad quedó `Inactiva` o `Descartada`. Valores literales fijos del dominio: `operativa`, `fusion`, `division`, `cascada_grupo`. |

---

## 3. Bounded Context y Agregados

### 3.1. Estructura Organizacional como Bounded Context

El bounded context contiene tres agregados raíz (`GrupoOrganizacional`, `UnidadOrganizacional` y `TipoUnidad`) y dos domain services que coordinan procesos multi-agregado.

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
│  │                     │                  │  · motivoBaja        │   │
│  └─────────┬───────────┘                  └──────────┬───────────┘   │
│            │                                          │              │
│            │                                          │              │
│            ▼                                          ▼              │
│   ┌─────────────────────────────────────────────────────────┐        │
│   │  Domain Services                                        │        │
│   │   · CascadaInactivacionGrupo (orquesta F10)            │        │
│   │   · ReestructuracionUnidad (orquesta F12/F13)          │        │
│   └─────────────────────────────────────────────────────────┘        │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
                              │
                              │  Eventos del dominio
                              ▼
            Sub-dominios consumidores (OXP, Contabilidad)
```

El tercer agregado raíz, **`TipoUnidad`** (ámbito del tenant, FSM de 2 estados — ver Sección 3.4), no aparece en el diagrama porque no participa de la jerarquía ni de los domain services: las unidades lo referencian por `tipoUnidadId` y el catálogo de tipos es la proyección de todos los `TipoUnidad` del tenant.

### 3.2. Agregado: `GrupoOrganizacional`

**Descripción:** Nodo agrupador de la jerarquía organizacional. No recibe imputaciones operativas. Su ciclo de vida es binario (`Activo` / `Inactivo`) y su inactivación dispara una cascada hacia todos sus descendientes (sub-grupos y unidades).

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| `grupoId` | Identidad | Identificador único del grupo. | uuid generado al crear |
| `codigo` | VO `Codigo` | Identificador alfanumérico plano, único por tenant entre grupos y unidades. Inmutable. | 4-12 caracteres (parametrizable por tenant `[R10]`) |
| `nombre` | VO `Nombre` | Denominación descriptiva del grupo. | texto |
| `padreId` | Referencia | Identificador del grupo padre. Null cuando el grupo es un **grupo de primer nivel** (sin padre) — un tenant puede tener varios; la estructura es un bosque (`[D16]`). Ver el comportamiento calculado `esDePrimerNivel()`. | uuid o null |
| `nivel` | Entero (calculado, no almacenado) | Profundidad del grupo en la jerarquía vigente. Se proyecta desde `[SI02]` (proyección de jerarquía vigente). No se appendea en eventos ni se persiste en el agregado. | 1 para grupos de primer nivel (la profundidad cuenta desde 1) |
| `estado` | Enum FSM | `Activo` o `Inactivo`. | |
| `versionAgregado` | Entero (atributo interno de plataforma) | Stamp de concurrencia optimista materializado por `[SI06]`. No es atributo de negocio — se usa solo en validación de `expectedVersion` al hacer append. No aparece en los payloads de los eventos. | |

**Comportamiento calculado (no almacenado):**

| Método | Fórmula | Usado por |
|--------|---------|-----------|
| `descendientesActivos()` | Recorrido recursivo de la proyección de jerarquía vigente (`[SI02]`), filtrando sub-grupos `Activo` y unidades `Activa` o `Suspendida`. | F10 (Inactivación de grupo con cascada). El domain service `CascadaInactivacionGrupo` invoca este método del agregado — no consulta la proyección directamente — para mantener la fuente única de verdad en el dominio. |
| `descendientesAfectablesPorCascada()` | Recorrido recursivo: sub-grupos `Activo` + unidades `Activa`/`Suspendida` + unidades `Borrador`. Devuelve lista clasificada por tipo. | F10, para mostrar al administrador el impacto previsto (`[R21]`) y para que la saga itere todos los nodos afectados. |
| `esDePrimerNivel()` | `padreId == null` | Nombre de negocio de la condición "grupo de primer nivel" (glosario del alcance, `[R31]`, `[D16]`). **Derivada, no almacenada:** un atributo almacenado permitiría el estado imposible "primer nivel con padre" — la derivación lo hace incontradecible por construcción. Las reglas y el código preguntan por este método, nunca por el null de `padreId` directamente; la proyección `[SI02]` lo expone para la UI y los reportes. |
| `puedeInactivarse()` | `estado == Activo` | Validación previa a F10. Si retorna false, el comando `InactivarGrupo` se rechaza. (La protección especial del raíz se retiró con `[D16]`: inactivar un grupo de primer nivel es un F10 normal — cascada con confirmación del impacto, `[R21]`.) |

**Eventos propios (4):**

- Ciclo de vida del grupo: `GrupoCreado`, `GrupoInactivado`, `GrupoReactivado`, `GrupoModificado`.

### 3.3. Agregado: `UnidadOrganizacional`

**Descripción:** Nodo hoja de la jerarquía donde se imputan las transacciones. Pertenece a exactamente un grupo padre y nunca tiene hijos (`[R01]`, `[R06]`). Su ciclo de vida tiene cinco estados con transiciones controladas (ver Sección 4.1): nace en `Borrador` o `Activa` según el flujo de creación, opera, puede pausarse (`Suspendida`), reactivarse, cerrarse (`Inactiva` reabrible), reabrirse o descartarse antes de operar (`Descartada` terminal estricto). Soporta los tres procesos de reestructuración (Fusión, División, Traslado) preservando identidad e historial.

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| `unidadId` | Identidad | Identificador único de la unidad. | uuid generado al crear |
| `codigo` | VO `Codigo` | Identificador alfanumérico plano, único por tenant, inmutable. | 4-12 caracteres |
| `nombre` | VO `Nombre` | Denominación descriptiva. | texto |
| `tipoUnidadId` | Referencia | Identificador del tipo de unidad (agregado `TipoUnidad` del tenant, Sección 3.4). Referencia **por identidad**: renombrar el tipo no afecta a las unidades. Los eventos de la unidad incluyen además `tipoUnidad` (nombre vigente del tipo) como dato informativo para consumidores. | uuid de un `TipoUnidad` vigente (`[I07]`) |
| `descripcion` | Texto opcional | Descripción libre. | |
| `grupoPadreId` | Referencia | Identificador del grupo padre. | uuid no nulo |
| `estado` | Enum FSM | `Borrador`, `Activa`, `Suspendida`, `Inactiva`, `Descartada`. | |
| `motivoBaja` | Enum opcional (proyectado en read model) | Causa de la baja cuando `estado in {Inactiva, Descartada}`. Valores: `operativa`, `fusion`, `division`, `cascada_grupo`. Ver `[D06]`. | null si está operando |
| `causalidadBaja` | Referencia opcional | Cuando `motivoBaja in {fusion, division}`, referencia a la unidad destino o lista de destinos. | uuid o lista de uuid |
| `fechaEstimadaReactivacion` | Fecha opcional (proyectada en read model) | Dato informativo para el administrador mientras la unidad está en `Suspendida`: expresa la transitoriedad esperada de la pausa y cuándo se espera reactivarla — ayuda a distinguir suspender (retorno esperado) de inactivar (sin expectativa de retorno). Ningún proceso la lee ni dispara reactivación automática (la reactivación F5 es siempre manual). Capturada en `UnidadSuspendida`. | null salvo en `Suspendida` |
| `fechaEfectiva` | `FechaEfectiva` opcional | Solo presente cuando la unidad fue parte de una reestructuración (`motivoBaja in {fusion, division}`). Capturada en `UnidadInactivada` y proyectada para reconstrucción histórica. Ver Sección 3.5 (VO `FechaEfectiva`). | null en operación normal |
| `versionAgregado` | Entero (atributo interno de plataforma) | Stamp de concurrencia optimista materializado por `[SI06]`. No es atributo de negocio — se usa solo en validación de `expectedVersion` al hacer append. No aparece en los payloads de los eventos. | |

**Datos transaccionales capturados en eventos pero no almacenados como atributos del agregado:**

- `motivo` (texto libre que aparece en `UnidadActivada`, `UnidadSuspendida`, `UnidadReactivada`, `UnidadReabierta`, `UnidadInactivada`, `UnidadDescartada`, `UnidadModificada`, `UnidadTrasladada`).

Este dato vive solo en el stream de eventos para auditoría narrativa; el agregado no lo proyecta en su estado actual porque no es una condición de negocio que afecte el comportamiento futuro.

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
| `validarCoherenciaBaja()` | Verifica `[I05]` cuando `estado in {Inactiva, Descartada}`: (a) `motivoBaja` está definido; (b) si `motivoBaja in {fusion, division}`, entonces `causalidadBaja` y `fechaEfectiva` están presentes; (c) si `motivoBaja in {operativa, cascada_grupo}`, entonces `causalidadBaja` y `fechaEfectiva` son null. | Guard interno que el agregado invoca al recibir `UnidadInactivada` o `UnidadDescartada`. Si la coherencia falla, el evento se rechaza antes del append. Refuerza la invariante `[I05]` y la propiedad write-once de los atributos de baja. |

`*` Consulta la proyección documentada en sugerencias de implementación (`[SI03]`). No es método local del agregado.

**Eventos propios (11):**

- Creación y activación: `UnidadCreada`, `UnidadActivada`.
- Pausa y reactivación: `UnidadSuspendida`, `UnidadReactivada`, `UnidadReabierta`.
- Baja: `UnidadInactivada`, `UnidadDescartada`.
- Modificación: `UnidadModificada`.
- Reestructuración: `UnidadFusionada`, `UnidadDividida`, `UnidadTrasladada`.

### 3.4. Agregado: `TipoUnidad`

**Descripción:** Clasificación de una unidad organizacional según su naturaleza (centro de costo, proyecto, sucursal, inmueble, departamento, y los que cada empresa agregue). Es un agregado raíz pequeño con **ámbito del tenant**: no pertenece a ningún nodo de la jerarquía (ver `[D10]`). El "catálogo de tipos" no es un agregado contenedor — es la **proyección de todos los `TipoUnidad` del tenant**. Las unidades lo referencian por identidad (`tipoUnidadId`), lo que permite renombrar un tipo sin romper referencias.

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| `tipoUnidadId` | Identidad | Identificador único del tipo. | uuid generado al crear |
| `nombre` | VO `Nombre` | Denominación del tipo. **Única por tenant** (`[SI13]`) y **modificable** — las unidades referencian por id, no por nombre. | texto |
| `descripcion` | Texto opcional | Descripción libre del propósito del tipo. | |
| `estado` | Enum FSM | `Activo` o `Inactivo` (ver Sección 4.3). Inactivar un tipo no afecta a las unidades existentes que lo usan; solo impide nuevas asignaciones. | |
| `versionAgregado` | Entero (atributo interno de plataforma) | Concurrencia optimista materializada por `[SI06]`, igual que en los demás agregados. | |

**Proyección de catálogo vigente — `tiposVigentes(tenant)`:** consulta de los `TipoUnidad` del tenant con `estado == Activo`. La usan las validaciones de creación, activación y cambio de tipo de unidades (`[I07]`) y la UI del administrador. Reemplaza la herencia dinámica desde el grupo raíz de la `[D14]` retirada: ya no se recorre la jerarquía — el catálogo es plano, del tenant.

**Eventos propios (3):** `TipoUnidadAgregado`, `TipoUnidadModificado`, `TipoUnidadInactivado` (Sección 5.4). Cada tipo vive en su propio stream.

**Tipos precargados:** al inicializar el tenant se crean como agregados `TipoUnidad` los tipos sugeridos de la Sección 6.1.

### 3.5. Value Objects compartidos

| VO | Usado por | Descripción |
|----|-----------|-------------|
| `Codigo` | `GrupoOrganizacional`, `UnidadOrganizacional` | Cadena alfanumérica plana de longitud parametrizable entre 4 y 12 caracteres (`[R10]`). **Codificación plana, sin estructura jerárquica embebida** — la jerarquía se modela como agregado separado y se proyecta vía `[SI02]` (ver `[DA1]`). Inmutable una vez asignado (`[R09]`). Único por tenant cruzando grupos y unidades (`[R08]`, `[I09]`). |
| `Nombre` | Ambos agregados | Cadena descriptiva no vacía para lectura humana. Sin restricción de unicidad. Modificable. |
| `MotivoBaja` | `UnidadOrganizacional` (atributo proyectado) | Enum cerrado del dominio: `operativa`, `fusion`, `division`, `cascada_grupo`. Los cuatro valores son literales fijos del modelo — no son catálogo configurable (ver `[D08]`). |
| `FechaEfectiva` | Eventos de reestructuración y de transición de jerarquía | Momento a partir del cual una versión de la jerarquía o una reestructuración rige. No puede ser anterior a la última versión vigente de jerarquía de las unidades involucradas (`[I08]`, validable localmente); su coherencia con la actividad transaccional la define y responde el administrador (`[R25]`). |
| `ReferenciaJerarquica` | Composición de ambos agregados | Combinación `padreId + nivel` que ubica un nodo en el árbol. El nivel es calculado, no almacenado en el código (`[DA1]`). |

### 3.6. Sugerencias de implementación

**Mapping rápido — Comando ↔ sugerencias de implementación aplicables:**

| Comando / proceso | SIs que el implementador debe aplicar |
|-------------------|---------------------------------------|
| `CrearGrupo` (F9) | `[SI01]`, `[SI02]`, `[SI03]`, `[SI06]` |
| `CrearUnidad` (F1 — admin) | `[SI01]`, `[SI03]`, `[SI04]`, `[SI06]` |
| `ActivarUnidad` (F3) | `[SI03]`, `[SI06]` |
| `SuspenderUnidad` (F4) / `ReactivarUnidad` (F5) | `[SI03]`, `[SI06]` |
| `ReabrirUnidad` (F6) | `[SI03]`, `[SI06]` |
| `InactivarUnidad` (F7) | `[SI06]` |
| `DescartarUnidad` (F8 manual) | `[SI04]`, `[SI06]` |
| `InactivarGrupo` (F10) | `[SI06]`, `[SI08]`, `[SI09]` |
| `ReactivarGrupo` (F11) | `[SI06]`, `[SI08]` (consulta correlación) |
| `FusionarUnidades` (F12) | `[SI02]`, `[SI06]`, `[SI09]` |
| `DividirUnidad` (F13) | `[SI02]`, `[SI06]`, `[SI09]` |
| `TrasladarUnidad` (F14) | `[SI02]`, `[SI03]`, `[SI06]` |
| `ModificarUnidad` / `ModificarGrupo` (F15) | `[SI06]` |
| `AgregarTipoUnidad` / `ModificarTipoUnidad` / `InactivarTipoUnidad` | `[SI06]`, `[SI13]` |

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

La proyección expone además los derivados de posición — `nivel` (contando desde 1: los grupos de primer nivel tienen nivel 1) y `esDePrimerNivel` — para la UI y los reportes; ningún consumidor de lectura interpreta directamente el null de `padreId`.

#### `[SI03]` Proyección de "padre Activo"

Materializa `[I10]`, `[R07]`, `[R16]`.

Mantener una proyección que indique el estado vigente del grupo padre de cada unidad y de cada sub-grupo. Las precondiciones de F1, F3, F6, F9, F14 consultan esta proyección antes de aceptar el comando.

**SLA y estrategia ante stale:**

- La proyección actualiza en <500 ms tras `GrupoInactivado` o `GrupoReactivado`.
- El agregado consulta la proyección como "última copia conocida" — no garantía transaccional.
- Si entre la consulta y el append del comando el padre cambia de estado (race condition), el evento se emite y, en post-procesamiento, se detecta la inconsistencia: el sub-dominio emite `UnidadInactivada` automática (con `motivoBaja: "operativa"` y un campo `causaSistema: "padre_inactivado_post_creacion"` en metadata) + alerta interna al administrador para revisión.
- Esto es una ventana tolerable de inconsistencia eventual documentada (<500 ms).

#### `[SI04]` Bandeja de Borradores pendientes

Soporta los flujos F3 (activación) y F8 (descarte manual).

Proyección que lista todas las unidades en `Borrador` —preparaciones del administrador— con su antigüedad (fecha de última actividad). Es la fuente para la UI del administrador (F3, F8). Contiene solo unidades reales en preparación del administrador (no demandas de consumidores).

La fecha de última actividad del borrador es un **dato informativo derivado de los eventos** (`UnidadCreada`, `UnidadModificada` — siempre del timestamp del evento mismo, nunca de un timestamp de BD externo): le permite al administrador identificar borradores antiguos y decidir si los activa (F3) o los descarta (F8). Ninguna política automática la lee — el descarte de un borrador es siempre una decisión del administrador o consecuencia de la cascada F10 (el descarte automático por inactividad fue retirado; ver control de versiones v1.9).

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

#### `[SI07]` Idempotencia de los comandos del administrador

Los comandos del administrador (F1, F3, F4, F5, F6, F7, F8, F9, F10, F11, F14, F15) pueden reintentarse (reenvío de la UI, reintento de red). La idempotencia no requiere una tabla de mensajes recibidos: se garantiza por la combinación de mecanismos que el modelo ya tiene:

- **Concurrencia optimista (`[SI06]`):** el comando lleva `expectedVersion`; un reintento sobre una versión ya aplicada se detecta como `version-conflict` y no duplica el efecto.
- **Validación de unicidad (`[SI01]`):** la unicidad de código por tenant rechaza una segunda creación con el mismo código.
- **Control en la UI:** deshabilitar el botón de envío tras el clic y confirmación explícita en operaciones críticas.

> **Nota — reorientación (issue #72).** Entre el #46 y el #72, `[SI07]` cubrió la idempotencia de la **señal de demanda** entrante. Al retirarse esa señal (ver `[SI11]`), `[SI07]` vuelve a su objeto original: la idempotencia de los comandos del administrador, que no necesita tabla propia porque la resuelven `[SI06]` y `[SI01]`.

#### `[SI08]` Saga `CascadaInactivacionGrupo` — política de completud

Materializa `[I12]`, `[I16]`, `[R19]`, `[R20]`.

El domain service `CascadaInactivacionGrupo` (Sección 3.7) usa un `correlationId` único para enlazar el `GrupoInactivado` raíz con todos los eventos derivados (`GrupoInactivado` de sub-grupos, `UnidadInactivada` y `UnidadDescartada` de unidades hijas). Cada evento derivado lleva el `correlationId`.

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

> **Nota — `[SI10]` retirada (issue #56).** Existió una "proyección local de última imputación por unidad" para validar la `fechaEfectiva` contra el historial transaccional. Con el **replanteamiento de `[R25]`/`[I08]`** —la coherencia de la fecha con la actividad transaccional la responde el administrador, y el sistema solo valida localmente contra la jerarquía vigente— esa proyección ya no es necesaria y se retira. Se conserva la numeración de `[SI11]`/`[SI12]` para no romper referencias. El detalle vive en el historial de git.

> **Nota — `[SI11]` retirada (issue #72).** Existió una "bandeja de sugerencias de creación" alimentada por una **señal de demanda** (`DemandaDeUnidadSenalada`) que un consumidor emitía cuando necesitaba una unidad inexistente, para que el administrador la creara. Una vez implementada la asignación/distribución de la unidad en los consumidores —la UI elige unidades de la fuente de verdad y las reglas de distribución se parametrizan contra ella—, una unidad referenciada **siempre existe** en Estructura Organizacional; el caso "unidad que el administrador aún no ha creado" deja de ocurrir en el camino operativo (`[P05]` reformulada), y este aparato quedó sin disparador → se retira junto con `[R30]` y la parte 4 de `[D15]`. La creación de unidades sigue su curso normal por planeación del administrador (F1). Se conserva la numeración de `[SI12]` para no romper referencias. El detalle vive en el historial de git.

#### `[SI13]` Índice único de nombre de `TipoUnidad` por tenant

Análoga a `[SI01]` (índice único del código — se referencia explícitamente como patrón fuente). Mantener un índice único por `(tenantId, nombre normalizado)` sobre los agregados `TipoUnidad`. Los comandos `AgregarTipoUnidad` y `ModificarTipoUnidad` (cuando el delta incluye `nombre`) validan contra el índice antes de aceptar; la concurrencia se resuelve con la misma política de `version-conflict` de `[SI01]`/`[SI06]`.

El índice cubre **todos** los tipos del tenant, activos e inactivos: el nombre de un tipo inactivado **no se libera** — dos tipos homónimos (uno inactivo y uno nuevo) harían ambigua la lectura de reportes históricos que denormalizan el nombre.

#### `[SI12]` Punto de resincronización de respaldo

Soporta `[R13]`, `[R29]` y la copia local de los consumidores.

Estructura Organizacional ofrece un punto de lectura de respaldo para que un consumidor **repare su copia local** de unidades cuando se desfasa (estuvo caído mucho tiempo, perdió un evento). Materializa la Capa 2 (reconciliación de respaldo) de la guía [`datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md).

**Características:**

- **Fuera del camino crítico:** el consumidor lo usa de fondo, no al imputar. Imputar siempre va contra su copia local (`[R13]`). Este punto solo repara la copia, no participa en cada operación.
- Puede materializarse como reproceso de los eventos de ciclo de vida desde un punto, o como una foto del estado vigente de las unidades del tenant.
- La fuente de verdad sigue siendo Estructura Organizacional; la copia del consumidor es derivada y reconstruible.

### 3.7. Domain services

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
   - Por cada unidad descendiente en estado `Borrador`, emite `UnidadDescartada` con `motivoBaja: "cascada_grupo"`, `esCascada: true` y `correlationId` (interpretación: el grupo padre se inactivó, los borradores quedan sin razón de ser).
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

### 3.8. Relaciones entre agregados

```
   ┌──────────────────────────┐
   │   GrupoOrganizacional    │
   │      (primer nivel)      │
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
- Grupos de primer nivel (`esDePrimerNivel()`): 1..N por tenant — la estructura es un **bosque**; cada uno encabeza su propio árbol y la frontera de consolidación es el tenant (`[D16]`).
- `UnidadOrganizacional` N:1 `TipoUnidad` (referencia por `tipoUnidadId`; el tipo es agregado del tenant y no participa de la jerarquía — no aparece en el diagrama).

### 3.9. Comportamiento de integración con consumidores (`[D15]`)

Las FSM de la Sección 4 describen el ciclo de vida **interno** de la unidad. Esta sub-sección describe el comportamiento **entre dominios** que introduce `[D15]`: cómo opera un consumidor (OXP, Contabilidad) contra su copia local y qué hace ante el desfase de consistencia eventual. El fundamento de patrones está en [`../../guias-de-modelado/datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md).

**Dos planos, dos lecturas del mismo dato.** El desacople es del backend; la UI es una capa de composición y lee al dueño en vivo:

- **UI / BFF — lee a Estructura Organizacional en vivo** (fuente de verdad): selección de unidades, nombres, jerarquía, parametrización de reglas. La UI compone de los servicios que necesita y, si Estructura Organizacional no está disponible, degrada apoyándose en la capacidad del dominio de operar y diferir. **La UI no consume la copia local del consumidor.**
- **Dominio del consumidor — valida contra su copia local** (`[R13]`); nunca consulta a Estructura Organizacional en el camino crítico. La copia es una proyección para **validación e integridad**, no una API de lectura para la pantalla.

**Diferir por consistencia eventual.** Como una unidad solo se referencia tras existir en Estructura Organizacional (la UI elige de la fuente de verdad; las reglas de distribución se parametrizan contra ella), una unidad referenciada **siempre existe en el dueño**. Lo único que puede pasar es que el evento de ciclo de vida (`UnidadCreada`/`UnidadActivada`) aún no haya llegado a la copia local del consumidor (desfase normal de propagación). En ese caso el consumidor **no se detiene**: registra lo que puede y difiere solo la parte que requiere la unidad, que se resuelve sola cuando el evento llega a su copia. Si una copia se desfasa por más tiempo (evento perdido, consumidor caído), se repara de fondo por el punto de resincronización (`[SI12]`), nunca en el camino crítico.

**La unidad nunca se aproxima.** No hay unidad de tránsito ni provisional: se difiere hasta que el evento llegue a la copia, porque la unidad debe coincidir exacto con la contabilidad.

> **Nota — qué cambió respecto al replanteamiento #46.** El #46 introdujo, además, una **señal de demanda** del consumidor hacia Estructura Organizacional y una **bandeja de sugerencias de creación** (`[SI11]`), pensadas para el caso "el consumidor necesita una unidad que el administrador aún no ha creado". Una vez implementada la asignación/distribución de la unidad en los consumidores (la UI elige de la fuente de verdad; las reglas resuelven sobre unidades existentes), ese caso deja de ocurrir en el camino operativo y ese aparato quedó **sin disparador** → se retiró (`[SI11]`, `[R30]`, la parte 4 de `[D15]`). Lo que permanece es la copia local para validar, el diferir por consistencia eventual y la resincronización de respaldo.

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
                                            │  F10 cascada)
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

- **`Borrador`** — Estado de **preparación del administrador**: la unidad que el administrador deja a medio definir antes de activarla. No transaccional y **no se origina desde sub-dominios consumidores** (la demanda de un consumidor no crea unidades — ver `[D15]`, `[R29]`). Admite eventos de progreso (`UnidadModificada`) sin cambio de estado. Transiciones de salida: a `Activa` mediante `UnidadActivada` (F3), o a `Descartada` mediante `UnidadDescartada` (F8 manual, o F10 cascada al inactivar el grupo padre).
- **`Activa`** — Estado operativo. Recibe imputaciones de los consumidores (`[R13]`). También es el estado inicial cuando el administrador elige "crear y activar directamente" en F1; en ese caso, el agregado emite `UnidadCreada` + `UnidadActivada` en el mismo append. Admite eventos de progreso (`UnidadModificada`). Transiciones de salida: a `Suspendida` (F4), a `Inactiva` (F7).
- **`Suspendida`** — Estado transitorio. No recibe nuevas imputaciones pero sigue consultable y reportable. Admite `UnidadModificada`. Transiciones de salida: a `Activa` (F5, `UnidadReactivada`) o a `Inactiva` (F7, `UnidadInactivada`).
- **`Inactiva`** — Estado de baja post-operación. Se conserva el historial. No admite imputaciones (`[R13]`) ni modificaciones (`[R17]`, `[I15]`). Admite reapertura mediante `UnidadReabierta` (F6), que la lleva a `Activa`. Lleva atributo `motivoBaja` proyectado (`operativa`, `fusion` o `division` — `cascada_grupo` no aplica aquí: un `Borrador` arrastrado por la cascada termina en `Descartada`, nunca en `Inactiva`).

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
   └──────────┘                      └─────┬───────┘
        ▲                                  │
        └──────────────────────────────────┘
              GrupoReactivado (F11)
              (sin cascada inversa)
```

**Notas estado por estado:**

- **`Activo`** — Estado inicial al crear (F9, `GrupoCreado` directamente en `Activo`; no hay `Borrador` para grupos). Admite modificación general (`GrupoModificado`) como evento de progreso. Transición de salida: a `Inactivo` mediante `GrupoInactivado` (F10) que dispara la saga `CascadaInactivacionGrupo`.
- **`Inactivo`** — El grupo no organiza nuevos descendientes operativos. Admite `GrupoModificado` (puede corregirse el nombre, por ejemplo, sin reactivar). Transición de salida: a `Activo` mediante `GrupoReactivado` (F11) — sin cascada inversa a los hijos; el administrador reabre los hijos uno a uno con apoyo del sistema inteligente. *(El catálogo de tipos ya no guarda relación con el estado del grupo: desde el #86 los tipos son agregados del tenant — la regla del "catálogo congelado" desapareció con la reubicación.)*

  > **Nota sobre asimetría con unidades:** A diferencia de las unidades en `Inactiva` (que NO admiten modificaciones), los grupos en `Inactivo` SÍ admiten `GrupoModificado`. La razón es que los grupos **no participan en historial transaccional** — son nodos agrupadores. Corregir el nombre o la descripción de un grupo inactivo no afecta la semántica de operaciones pasadas. La asimetría es intencional y refleja la diferencia funcional entre los dos tipos de nodo.

### 4.3. FSM de `TipoUnidad` (agregado)

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
| **Descripción** | Una unidad organizacional fue registrada en el sistema. Siempre nace por **acto deliberado del administrador** (F1): directamente operativa (`Activa`) o en preparación (`Borrador`). La creación nunca se origina en un consumidor. |
| **Causalidad** | Directa (comando `CrearUnidad`). |
| **Agregado** | `UnidadOrganizacional` |
| **Estado previo** | — (creación) |
| **Estado resultante** | `Borrador` (default) o `Activa` (cuando el administrador elige activar directamente — en ese caso se emite `UnidadActivada` en el mismo append). |
| **Precondiciones** | `[R08]` (código único), `[R07]` (grupo padre Activo), tipo vigente en el catálogo de tipos del tenant (`[I07]`, Sección 3.4), formato del código válido (`[R10]`), `[I09]` (unicidad cruzada en tenant). |
| **Información capturada** | `unidadId`, `codigo`, `nombre`, `tipoUnidadId`, `tipoUnidad` (nombre vigente, informativo), `descripcion`, `grupoPadreId`, `estadoInicial` (`Borrador` o `Activa` — **es un parámetro del comando que se captura para auditoría; el estado actual de la unidad se reconstruye reproduciendo la secuencia de eventos, no se lee de este campo**), `usuarioId`, `timestamp`. |
| **Efectos** | Se crea el agregado en el estado inicial elegido. Si nace en `Activa`, se emite también `UnidadActivada`. Los consumidores reciben la notificación y actualizan su copia local; un diferido pendiente por esta unidad (`[R29]`, `[D15]`) se resuelve solo al llegar este evento. |

#### `UnidadActivada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una unidad pasa a estado `Activa`. Aplica al activar una unidad en `Borrador` (F3) o como evento colateral cuando una unidad nace directamente en `Activa` en F1 con `estadoInicial: Activa`. F5 (reactivación desde `Suspendida`) emite `UnidadReactivada`; F6 (reapertura desde `Inactiva`) emite `UnidadReabierta`. Este evento es el que destraba, en los consumidores, las operaciones que estaban diferidas a la espera de esta unidad (`[D15]`). |
| **Causalidad** | Directa (comando `ActivarUnidad`) en F3; derivada por transición (mismo append que `UnidadCreada`) en F1 con `estadoInicial: Activa`. |
| **Agregado** | `UnidadOrganizacional` |
| **Estado previo** | `Borrador` (F3) o ninguno si se emite junto con `UnidadCreada` (F1). |
| **Estado resultante** | `Activa` |
| **Precondiciones** | Datos mínimos completos (`[I07]`), grupo padre en estado `Activo` (`[I10]`, `[R07]`), no existe otra unidad `Activa` con el mismo `codigo` (`[I09]`). |
| **Información capturada** | `unidadId`, `codigo`, `tipoUnidadId`, `tipoUnidad` (nombre vigente, informativo), `grupoPadreId`, `estadoAnterior` (`Borrador` o `null` si vino con `UnidadCreada` en F1), `motivo` (opcional, texto libre), `usuarioId`, `timestamp`. |
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
| **Información capturada** | `unidadId`, `motivo` (opcional, texto libre), `fechaEstimadaReactivacion` (opcional — dato informativo, ver composición en 3.3), `usuarioId`, `timestamp`. |
| **Efectos** | Los consumidores bloquean nuevas imputaciones. El historial se conserva intacto. La fecha estimada de reactivación queda consultable mientras la unidad esté suspendida; es informativa para el administrador — no dispara reactivación automática (la reactivación F5 es siempre un gesto manual). |

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
| **Información capturada** | `unidadId`, `codigo`, `tipoUnidadId`, `tipoUnidad` (nombre vigente, informativo), `grupoPadreId`, `motivoBaja` (`operativa` \| `fusion` \| `division`), `causalidadBaja` (opcional, referencia rápida: `unidadId` destino para fusión, lista de `unidadId` destinos para división; null cuando `motivoBaja == "operativa"`), `fechaEfectiva` (nullable; presente cuando `motivoBaja in {fusion, division}`; null cuando `motivoBaja == "operativa"` — la baja rige desde el `timestamp`), `esCascada` (boolean; `true` cuando es derivado de F10/F12/F13), `correlationId` (presente cuando `esCascada == true` o cuando proviene de saga), `motivo` (texto libre opcional), `usuarioId`, `timestamp`. |
| **Efectos** | Los consumidores bloquean nuevas imputaciones. El atributo `motivoBaja` se proyecta para reportería y bandejas. Los registros históricos se conservan. La unidad puede reabrirse posteriormente con F6 (excepto cuando `motivoBaja in {fusion, division}` — en esos casos reabrir es semánticamente raro pero técnicamente posible; el sistema inteligente debe advertir). |

> **Nota sobre `causalidadBaja`:** es una referencia rápida (uuid o lista de uuids). La información completa del proceso de reestructuración (todos los participantes, fecha efectiva, motivo) se consulta en el evento de proceso correlacionado (`UnidadFusionada` o `UnidadDividida`) usando el `correlationId`. Esta distribución entre dos streams (proceso + agregado) es por diseño: el agregado mantiene una referencia mínima; el stream de proceso mantiene el detalle completo (`[D04]`).

#### `UnidadDescartada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una unidad que nunca operó se descarta. Aplica al rechazo manual del administrador sobre un borrador suyo (F8) o a la cascada cuando el grupo padre se inactiva con borradores colgando (F10). |
| **Causalidad** | Directa en F8 (comando `DescartarUnidad`); efecto inter-agregado en F10. |
| **Agregado** | `UnidadOrganizacional` |
| **Estado previo** | `Borrador` |
| **Estado resultante** | `Descartada` ■ (terminal estricto, `[R14]`) |
| **Precondiciones** | Unidad en `Borrador`. |
| **Información capturada** | `unidadId`, `codigo`, `tipoUnidadId`, `tipoUnidad` (nombre vigente, informativo), `grupoPadreId`, `motivoBaja` (`operativa` cuando es rechazo manual del admin en F8; `cascada_grupo` cuando es descarte por cascada F10), `esCascada` (boolean; `true` cuando es derivado de F10), `correlationId` (presente cuando `esCascada == true`: el `correlationId` de la saga F10), `motivo` (texto libre opcional), `usuarioId` (el administrador que descarta en F8, o el que inactivó el grupo origen en F10), `timestamp`. |
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
| **Información capturada** | `unidadId`, `changes` (map de `{ fieldName: nuevoValor }` solo con los campos efectivamente modificados; claves posibles: `nombre`, `tipoUnidadId`, `descripcion`; cuando cambia `tipoUnidadId`, el payload incluye además `tipoUnidad` (nombre vigente, informativo)), `motivo` (opcional), `usuarioId`, `timestamp`. Formato delta canónico según Sección 2.3.1. |
| **Efectos** | El estado proyectado refleja los nuevos valores (si se modificó en `Borrador`, la bandeja `[SI04]` refresca la fecha de última actividad del borrador). Los consumidores con interés en los campos modificados (ej: Contabilidad al cambio de tipo) actualizan su vista local. |

### 5.2. Eventos del ciclo de vida de grupos

#### `GrupoCreado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Un grupo organizacional fue registrado en el sistema, directamente en estado `Activo` (los grupos no tienen `Borrador`; F9). |
| **Causalidad** | Directa (comando `CrearGrupo`). |
| **Agregado** | `GrupoOrganizacional` |
| **Estado previo** | — (creación) |
| **Estado resultante** | `Activo` |
| **Precondiciones** | Código único en el tenant (`[I09]`); si se indica `padreId`, el grupo padre existe y está en `Activo` (`[R07]`) y la posición no introduce ciclos (`[I11]`); sin `padreId`, el grupo nace como **grupo de primer nivel** (`[D16]`); formato del código (`[R10]`). |
| **Información capturada** | `grupoId`, `codigo`, `nombre`, `padreId` (null cuando el grupo se crea como grupo de primer nivel, sin padre), `usuarioId`, `timestamp`. |
| **Efectos** | El grupo queda operativo. Los consumidores con interés en la estructura jerárquica actualizan su vista local. |

#### `GrupoInactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Un grupo organizacional pasa a estado no operativo. Puede ser el grupo origen de una cascada (comando `InactivarGrupo`) o un sub-grupo afectado por la cascada de un ancestro. |
| **Causalidad** | Directa en el grupo origen (comando `InactivarGrupo`); efecto inter-agregado en sub-grupos descendientes (saga `CascadaInactivacionGrupo`). |
| **Agregado** | `GrupoOrganizacional` |
| **Estado previo** | `Activo` |
| **Estado resultante** | `Inactivo` |
| **Precondiciones** | Grupo en `Activo`; el administrador confirmó el impacto previsto (`[R21]`, validación previa al comando). |
| **Información capturada** | `grupoId`, `codigo`, `nombre`, `padreId`, `motivo` (opcional), `esCascada` (boolean: `true` si fue derivado, `false` si es el origen), `grupoIdOrigen` (nullable; presente cuando `esCascada == true`: identifica el grupo origen que disparó la cascada), `correlationId` (presente cuando `esCascada == true`), `usuarioId`, `timestamp`. |
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
| **Precondiciones** | Todas las unidades origen en `Activa` o `Suspendida` (`[R23]`, `[I14]`); destino en `Activa` y distinto del conjunto origen (`[R22]`, `[R24]`); fecha efectiva no anterior a la última versión vigente de jerarquía (`[I08]`, validable localmente). La coherencia con la actividad transaccional la responde el administrador (`[R25]`). |
| **Información capturada** | `correlationId`, `unidadesOrigen` (lista de `unidadId`), `codigosOrigen` (lista de `codigo` paralela a `unidadesOrigen`), `unidadDestino` (`unidadId`), `codigoDestino`, `fechaEfectiva`, `motivo`, `usuarioId`, `timestamp`. |
| **Efectos** | Marca el inicio del proceso. Le siguen N `UnidadInactivada` (una por origen, con `motivoBaja: "fusion"` y el mismo `correlationId`). Los consumidores que reciben el evento reasignan sus referencias del conjunto origen al destino con fecha efectiva. El payload self-contained (incluye códigos) permite reconstruir el cambio histórico sin queries adicionales. Habilita reportes con "vista actual" (todo al destino desde fecha efectiva) o "vista histórica" (cada periodo con su estructura). |

> **Nota sobre boundaries:** Este evento vive en un **stream propio del proceso de reestructuración** (un stream por proceso, identificado por `correlationId`); no se appendea al stream de las unidades involucradas. **No existe un agregado backend `ReestructuracionUnidad`** — el evento es generado directamente por el **domain service** del mismo nombre (Sección 3.7) que coordina la saga. Los streams de las unidades reciben sus propios `UnidadInactivada` correlacionados por `correlationId`.

#### `UnidadDividida`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Evento de proceso que registra la división de una unidad origen en N unidades destino (F13). Le sigue el `UnidadInactivada` de la unidad origen (con `motivoBaja: "division"`). |
| **Causalidad** | Directa (comando `DividirUnidad`). |
| **Agregado** | Stream propio del proceso de reestructuración. |
| **Estado previo** | N/A (evento de proceso) |
| **Estado resultante** | N/A (la unidad origen pasará a `Inactiva` por el `UnidadInactivada` derivado) |
| **Precondiciones** | Unidad origen en `Activa` o `Suspendida` (`[R23]`); al menos dos destinos, todos en `Activa`, distintos del origen (`[R22]`, `[R24]`); fecha efectiva no anterior a la última versión vigente de jerarquía (`[I08]`, validable localmente). La coherencia con la actividad transaccional la responde el administrador (`[R25]`). |
| **Información capturada** | `correlationId`, `unidadOrigen` (`unidadId`), `codigoOrigen`, `unidadesDestino` (lista de `unidadId`), `codigosDestino` (lista de `codigo` paralela a `unidadesDestino`), `fechaEfectiva`, `motivo`, `usuarioId`, `timestamp`. |
| **Efectos** | Marca el inicio del proceso. Le sigue 1 `UnidadInactivada` para la origen (con `motivoBaja: "division"` y el mismo `correlationId`). Los consumidores reasignan sus referencias futuras según corresponda; el historial previo a la fecha efectiva queda referenciado al origen (`[R27]`). El payload self-contained (incluye códigos) permite a los consumidores reconstruir la división histórica sin queries adicionales. |

> **Nota sobre boundaries:** análogo a `UnidadFusionada` — vive en stream propio del proceso de reestructuración (un stream por `correlationId`), no se appendea al stream de las unidades involucradas, y no existe agregado backend. El servicio `ReestructuracionUnidad` (Sección 3.7) es el emisor.

#### `UnidadTrasladada`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una unidad cambia de grupo padre en la jerarquía (F14). Conserva identidad, código, estado e historial transaccional (`[R28]`). Solo cambia su posición en el árbol y la versión vigente de jerarquía. |
| **Causalidad** | Directa (comando `TrasladarUnidad`). |
| **Agregado** | `UnidadOrganizacional` (el cambio impacta también la proyección de jerarquía, pero el evento se appendea al stream de la unidad). |
| **Estado previo** | `Activa` o `Suspendida` |
| **Estado resultante** | Sin cambio de estado (la posición en el árbol cambia, no el ciclo de vida) |
| **Precondiciones** | Unidad en `Activa` o `Suspendida` (`[R23]`); nuevo grupo padre existente, en `Activo` y distinto del padre actual (`[R07]`, `[I10]`); fecha efectiva coherente con la versión vigente (`[R25]`, `[I08]`). |
| **Información capturada** | `unidadId`, `codigo`, `tipoUnidadId`, `tipoUnidad` (nombre vigente, informativo), `grupoPadreAnterior`, `grupoPadreNuevo`, `fechaEfectiva`, `motivo`, `usuarioId`, `timestamp`. |
| **Efectos** | La jerarquía registra una nueva versión vigente a partir de la fecha efectiva. La unidad sigue operando con el mismo código. Los consumidores con interés jerárquico (reportería) actualizan su vista. El payload self-contained (incluye `codigo`, `tipoUnidadId` y el nombre vigente del tipo) permite a los consumidores que mantienen mapeos `código → padre` actualizar deterministicamente sin queries adicionales (`[P03]`). |

### 5.4. Eventos del agregado `TipoUnidad`

> **Nota:** Estos tres eventos son de **configuración del catálogo del tenant**: cambian un agregado `TipoUnidad` (Sección 3.4), no la jerarquía ni el estado de ningún grupo o unidad. Cada evento se appendea al **stream propio del tipo**. Los consumidores que mantengan proyecciones del catálogo reaccionan a estos eventos.

#### `TipoUnidadAgregado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se agrega un nuevo tipo de unidad al catálogo del tenant. |
| **Causalidad** | Directa (comando `AgregarTipoUnidad`). |
| **Agregado** | `TipoUnidad` |
| **Estado previo** | — (creación) |
| **Estado resultante** | `Activo` |
| **Precondiciones** | El `nombre` no duplica el de otro tipo del tenant (`[SI13]`, cubre activos e inactivos). |
| **Información capturada** | `tipoUnidadId`, `nombre`, `descripcion` (opcional), `usuarioId`, `timestamp`. |
| **Efectos** | El catálogo del tenant incluye el nuevo tipo, disponible para asignar a unidades nuevas en cualquier punto de la jerarquía. |

#### `TipoUnidadModificado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Cambian el nombre o la descripción de un tipo existente. Renombrar es seguro: las unidades referencian el tipo por `tipoUnidadId`, no por nombre. |
| **Causalidad** | Directa (comando `ModificarTipoUnidad`). |
| **Agregado** | `TipoUnidad` |
| **Estado previo** | `Activo` |
| **Estado resultante** | Sin cambio de estado. |
| **Precondiciones** | Tipo existente y `Activo`. Si el delta incluye `nombre`, el nuevo nombre no duplica el de otro tipo del tenant (`[SI13]`). |
| **Información capturada** | `tipoUnidadId`, `changes` (map de `{ fieldName: nuevoValor }` solo con campos efectivamente modificados; claves posibles: `nombre`, `descripcion`), `motivo` (opcional), `usuarioId`, `timestamp`. Formato delta canónico según Sección 2.3.1. |
| **Efectos** | El catálogo refleja los nuevos valores. Las unidades que referencian el tipo no requieren acción (referencia por id); las proyecciones y consumidores que denormalizan el nombre lo refrescan con este evento. |

#### `TipoUnidadInactivado`

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Un tipo de unidad se inactiva — no podrá asignarse a unidades nuevas. Las unidades existentes que ya lo usan no se ven afectadas. |
| **Causalidad** | Directa (comando `InactivarTipoUnidad`). |
| **Agregado** | `TipoUnidad` |
| **Estado previo** | `Activo` |
| **Estado resultante** | `Inactivo` ■ (terminal en F1, `[D13]`) |
| **Precondiciones** | Tipo existente y `Activo`. |
| **Información capturada** | `tipoUnidadId`, `motivo` (opcional), `usuarioId`, `timestamp`. |
| **Efectos** | El tipo deja de aparecer en las opciones de creación de unidades. Las unidades existentes que lo usan no requieren acción. Su `nombre` no se libera (`[SI13]`). |

---

## 6. Catálogos del dominio

### 6.1. Catálogo de tipos de unidad

Catálogo **interno** al sub-dominio (no proviene de Datos de Referencia — los tipos son conceptos del modelo organizacional, no datos de referencia universales). **Ámbito del tenant:** cada tipo es un agregado `TipoUnidad` propio (Sección 3.4) y el catálogo es la proyección de los tipos del tenant. Es extensible: cada empresa puede agregar tipos personalizados según su modelo de negocio.

**Tipos pre-cargados sugeridos al inicializar un tenant (se crean como agregados `TipoUnidad`):**

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
| `cascada_grupo` | Descarte de un Borrador arrastrado por la cascada de inactivación de su grupo padre. | F10 (cascada) |

El atributo `motivoBaja` se proyecta en el modelo de lectura para reportería, bandejas y auditoría (`[D06]`).

---

## 7. Invariantes del dominio

15 invariantes (hueco en `I13`, retirada con `[D16]` — se conserva la numeración). Clasificación: **local** (un solo agregado, transaccional) o **eventual** (cruza fronteras, enforcement por proyección).

| # | Invariante | Tipo | Agregado | Referencia |
|---|-----------|------|----------|------------|
| `I01` | **Transiciones FSM válidas de unidad.** Solo se permiten las 7 transiciones documentadas en Sección 4.1 (`Borrador → Activa`, `Borrador → Descartada`, `Activa → Suspendida`, `Suspendida → Activa`, `Activa → Inactiva`, `Suspendida → Inactiva`, `Inactiva → Activa`). Cualquier otra es rechazada por el agregado antes del append. | Local | `UnidadOrganizacional` | `[R12]` |
| `I02` | **Transiciones FSM válidas de grupo.** Solo se permiten `Activo → Inactivo` y `Inactivo → Activo`. | Local | `GrupoOrganizacional` | `[R18]` |
| `I03` | **Formato del código.** El código es alfanumérico de longitud entre 4 y 12 caracteres. La longitud específica admitida por tenant es parametrizable dentro de ese rango. | Local | Ambos | `[R10]` |
| `I04` | **Inmutabilidad del código.** Una vez asignado, el código de una unidad o grupo no se modifica en ningún comando posterior. | Local | Ambos | `[R09]` |
| `I05` | **Coherencia `motivoBaja` ↔ flujo.** Cuando `estado in {Inactiva, Descartada}`, el atributo `motivoBaja` está definido y su valor corresponde al flujo que disparó la baja (`operativa` desde F7 o F8 manual; `fusion` desde F12; `division` desde F13; `cascada_grupo` desde F10 cascada para Borradores). | Local | `UnidadOrganizacional` | `[R26]` |
| `I06` | **Reapertura requiere padre activo.** Un comando `ReabrirUnidad` solo se acepta si el `grupoPadreId` está en `Activo` al momento del comando. | Eventual | `UnidadOrganizacional` | `[R16]` |
| `I07` | **Datos mínimos para activación.** Una unidad solo transiciona a `Activa` si `codigo`, `nombre`, `tipoUnidadId` y `grupoPadreId` están definidos y el tipo referenciado está vigente en el catálogo de tipos del tenant (Sección 3.4). | Local | `UnidadOrganizacional` | Flujo 3 (activación), alcance |
| `I08` | **Fecha efectiva de la reestructuración.** En F12, F13 y F14 la `fechaEfectiva` se gobierna en dos planos: **(a)** no puede ser anterior a la última versión vigente de jerarquía de las unidades involucradas — Estructura Organizacional lo valida **localmente** (es dueña de la jerarquía); **(b)** su coherencia con la actividad transaccional (no fijarla sobre periodos que ya tienen movimientos) la **define y responde el administrador** que ejecuta la reestructuración, como acto deliberado de gestión — el sistema no la valida contra el historial transaccional porque las transacciones/asientos son inmutables y los reportes ofrecen vista actual e histórica. | Local | `UnidadOrganizacional` | `[R25]` |
| `I09` | **Unicidad de código por tenant.** El `codigo` es único dentro del tenant cruzando grupos y unidades. Las unidades en `Descartada` se excluyen del índice para liberar la identificación (`[R11]`). Enforcement por proyección (`[SI01]`) porque cruza dos agregados. | Eventual | Ambos | `[R08]`, `[R11]` |
| `I10` | **Padre activo al crear/trasladar/reabrir.** Al crear (F1, F9), trasladar (F14) o reabrir (F6) un nodo, el grupo padre destino debe estar en `Activo`. Enforcement por proyección (`[SI03]`) porque cruza dos agregados. | Eventual | Ambos | `[R07]`, `[R16]` |
| `I11` | **No ciclos en la jerarquía.** Un grupo no puede ser su propio ancestro directo ni indirecto. La validación se hace al recibir comandos que cambian la jerarquía (`CrearGrupo`, `TrasladarUnidad`) consultando la proyección (`[SI02]`). | Eventual | `GrupoOrganizacional` | `[R04]` |
| `I12` | **Integridad de la cascada de inactivación.** Cuando `GrupoInactivado` se emite por la saga `CascadaInactivacionGrupo`, todos los descendientes vivos al momento de la captura reciben su propio evento (`GrupoInactivado` para sub-grupos, `UnidadInactivada` o `UnidadDescartada` para unidades) correlacionado por `correlationId`. No quedan nodos descendientes en estado operativo bajo un grupo inactivado. | Eventual | Ambos (vía saga) | `[R19]` |
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
| `D08` | **Un solo evento `UnidadDescartada` cubre el rechazo manual del administrador y el descarte por cascada.** El distinguidor del motivo va en el atributo `motivoBaja` (`operativa` vs `cascada_grupo`). **Alcance específico de esta decisión:** no existe un evento explícito separado de rechazo (`UnidadRechazada`) — el evento `UnidadDescartada` se unifica con diferenciación por atributo. *(El descarte automático por inactividad que originalmente acompañaba esta decisión —`[D09]`/`[SI05]`— fue retirado en el issue #87: su motor original era la creación desde consumidores, eliminada en #46, y el caso residual —borradores que el propio administrador creó y abandonó— no justifica un proceso programado. El descarte de borradores es decisión del administrador: F8 manual o cascada F10.)* | Evita inflar el catálogo de eventos. La auditoría diferencia los casos vía el atributo proyectado, no vía evento separado. | Plan, decisión cerrada con el usuario; ajustada en #87. |
| `D10` | **`TipoUnidad` como agregado raíz propio, con ámbito del tenant** (issue #86 — replantea la decisión original de modelarlo como entidad interna del grupo raíz). El "catálogo de tipos" no es un agregado contenedor: es la **proyección** de todos los `TipoUnidad` del tenant. Las unidades referencian el tipo **por identidad** (`tipoUnidadId`), no por nombre. | La frontera de agregado se define por invariantes, identidad y ciclo de vida — no por conteo de estados. `TipoUnidad` cumple los criterios: ciclo de vida propio (comandos, eventos y FSM propios), referenciado por identidad desde otro agregado (`UnidadOrganizacional` — señal fuerte de agregado), y **ninguna invariante cruza dos tipos** que exija un contenedor común — la unicidad del nombre se materializa por índice único (`[SI13]`, patrón `[SI01]`). La decisión original se apoyaba en "pocos estados ⇒ no agregado" (criterio equivocado) y en una cohesión que la herencia dinámica ya había vuelto cross-agregado; producía además dos síntomas: el atributo `tiposUnidad` vacío en todos los grupos salvo el raíz, y el nombre del tipo inmutable de facto por ser la llave de referencia. Con id estable, el nombre es renombrable sin romper referencias. | Issue #86; reemplaza la D10 original (su texto vive en el historial de git). |
| `D11` | **Mecanismos de plataforma (concurrencia optimista, idempotencia técnica, retry) viven como `[SI##]`**, no se especifican por evento ni como invariantes del dominio. | Decisión transversal del proyecto. Las invariantes y reglas pertenecen al dominio; los mecanismos de plataforma son sugerencias de implementación que materializan invariantes (especialmente las eventuales). | Decisión del usuario (MEMORY.md) |
| `D12` | **Cascada de inactivación de grupos modelada como saga** (`CascadaInactivacionGrupo`). Emite un evento por nodo afectado (no un evento agregado tipo `GrupoInactivadoEnCascada`) para que los consumidores puedan reaccionar granularmente sin parsear listas. Sin cascada inversa al reactivar (`[R20]`); el sistema inteligente identifica candidatos a reabrir vía `correlationId` correlacionado (`[SI08]`). | Granularidad de eventos = granularidad de reacción. La asimetría reactivación-sin-cascada es coherente con que `Inactiva` no es terminal en F1 — el admin puede reabrir hijos que vea pertinentes sin que el sistema lo presuma. | Plan, decisión cerrada con el usuario en familia 2 grupos |
| `D13` | **Reactivación de tipos de unidad no modelada en F1.** La FSM de `TipoUnidad` tiene transición `Activo → Inactivo` (vía `TipoUnidadInactivado`) pero no la inversa. El alcance v1.0 no la requiere — no hay un caso de negocio identificado que justifique reactivar un tipo previamente inactivado. | Si en F2+ el negocio lo solicita, se evalúa modelarlo como evento `TipoUnidadReactivado` análogo a `GrupoReactivado`, con transición `Inactivo → Activo` en la FSM de `TipoUnidad`. No se mantiene como pendiente formal (`[PD##]`) porque no hay demanda actual ni horizonte definido — es una extensión natural si surge. La asunción de F1 es que `TipoUnidadInactivado` es terminal. | Auditoría Bloque Media, M9. |
| `D15` | **La unidad organizacional es un dato gobernado con dueño único (Estructura Organizacional); los consumidores operan contra copia local, difieren ante el desfase y nunca crean ni bloquean.** Tres consecuencias de modelo: **(1) Dueño único** — solo Estructura Organizacional crea, modifica y da de baja unidades; ningún consumidor las origina. **(2) Copia local por eventos, para validación** — OXP, Contabilidad y futuros consumidores mantienen su copia de unidades por suscripción a los eventos de ciclo de vida y **validan contra ella** en su dominio; nunca consultan a Estructura Organizacional en el camino crítico (`[R13]`, `[SI12]` repara la copia de fondo). La copia es una proyección para validación e integridad — **no una API de lectura para la UI**: la UI lee a Estructura Organizacional en vivo (ver §3.9 y `datos-entre-dominios.md`). **(3) Diferir por consistencia eventual, no bloquear** — como una unidad solo se referencia tras existir en Estructura Organizacional (la UI elige de la fuente de verdad; las reglas se parametrizan contra ella), una unidad referenciada siempre existe en el dueño; si su evento de ciclo de vida aún no llegó a la copia local, el consumidor registra lo que puede y difiere solo la parte que la requiere, que se resuelve sola cuando el evento llega (consistencia eventual). La unidad debe coincidir exacto con la contabilidad, así que **no se aproxima con un valor de tránsito o provisional** (estrategia "diferir" de la guía `datos-entre-dominios.md`). | Elimina los acoplamientos de ejecución y proceso entre Estructura Organizacional y los consumidores (issue #45/#46): el consumidor nunca queda detenido por la disponibilidad ni por el ciclo de creación de Estructura Organizacional, y Estructura Organizacional no queda atada a la disponibilidad de los consumidores para reestructurar. Reemplaza el patrón anterior (creación desde consumidor vía BFF → unidad en `Borrador` → activación → cancelación en cascada al descartar), que acoplaba la operación del consumidor al gesto humano del administrador. **Nota (issue #72):** la parte 4 original —"demanda por señal informativa" (`[R30]`/`[SI11]`)— se retiró: una vez la asignación de la unidad se hace contra la fuente de verdad, el caso "unidad que el administrador aún no creó" deja de ocurrir en operación y la señal queda sin disparador. Fundamento completo en [`../../guias-de-modelado/datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md). | Replanteamiento #46; ajuste #72; guía `datos-entre-dominios.md`. |
| `D16` | **La estructura organizacional es un bosque: se deja de imponer el grupo raíz único obligatorio** (issue #85). "Ser de primer nivel" es la propiedad derivada `padreId == null` (comportamiento calculado `esDePrimerNivel()`); un tenant puede tener varios grupos de primer nivel; ningún grupo se crea automáticamente al inicializar el tenant; la **frontera de consolidación es el tenant**, no un nodo ("total compañía" = todas las unidades del tenant). Se retiran `[R2]`/`[R3]`/`[I13]`, el atributo almacenado `esRaiz` y el auto-aprovisionamiento; inactivar un grupo de primer nivel es un F10 normal (cascada + confirmación de impacto `[R21]`). | El raíz único nació sin justificación registrada (anexo v1.1, Decisión 2 — única regla estructural sin porqué escrito) y sus tres propósitos no resisten análisis: (1) el ancestro único no lo necesita ningún mecanismo — ciclos (`[I11]`), `nivel` y cascada (`[SI08]`) operan por sub-árbol; (2) el catálogo de tipos dejó de vivir en el raíz (#86); (3) la consolidación "total compañía" la da el tenant, que ya delimita unicidad de código, permisos y catálogos. Arrastraba además dos huecos: `codigo`/`nombre` de inicialización nunca definidos y unicidad `[I13]` sin mecanismo que la garantizara. Es coherente con la visión multi-jerarquía de `[DA1]` (incompatible con un raíz único) y con la homologación del ERP actual: los **centros de costo maestros** (varios por empresa, contenedores para consolidar reportes — balances y estados financieros) ↔ grupos; los **auxiliares** (los que se mapean en la transacción) ↔ unidades — el negocio real nunca tuvo un "maestro único" obligatorio. La multi-jerarquía plena (un nodo bajo varias estructuras) sigue en F2+. | Issue #85; anexo `anexo-decisiones-arquitectonicas.md` v1.3 (Decisión 2 actualizada). |

---

## 10. Premisas de negocio

| # | Premisa | Impacto en el modelo |
|---|---------|---------------------|
| `P01` | **Una unidad puede atravesar pausa/reapertura múltiples veces durante su vida operativa.** Sucursales estacionales, proyectos por fases, áreas que se reorganizan cíclicamente. | Justifica que `Inactiva` no sea terminal estricto y que existan dos eventos diferenciados (`UnidadReactivada` desde pausa transitoria, `UnidadReabierta` tras cierre) para auditoría. |
| `P02` | **La estructura jerárquica de las empresas evoluciona constantemente.** Fusiones, divisiones, traslados; las empresas reorganizan sus áreas con frecuencia. | Justifica la jerarquía versionada (`[DA1]`) y los tres procesos de reestructuración como eventos de dominio de primera clase. |
| `P03` | **La identificación de una unidad debe ser estable a lo largo del tiempo.** Auditorías retrospectivas pueden ocurrir años después; la comparabilidad histórica IFRS 8 exige que cada periodo se reporte con la estructura de entonces. | Justifica la inmutabilidad del código (`I04`), la conservación del historial al inactivar (`Inactiva` no borra; `[R27]` historial referenciado al origen en F12/F13) y la fecha efectiva como eje de la jerarquía versionada. |
| `P04` | **Las empresas operan con estructuras de hasta ~2.000 unidades organizacionales por empresa con jerarquías de múltiples niveles.** Límite documentado en el alcance. | Acota el dimensionamiento de las proyecciones (`[SI01]`-`[SI04]`) y los costos esperados de la cascada (`CascadaInactivacionGrupo` debe manejar miles de descendientes en grupos amplios). |
| `P05` | **El consumidor asigna la unidad contra la fuente de verdad, así que referenciar una unidad inexistente no ocurre en el camino operativo.** La UI elige unidades de Estructura Organizacional en vivo y las reglas de distribución del consumidor se parametrizan contra ella; una unidad referenciada siempre existe en el dueño. Lo único que puede pasar es el **desfase de propagación**: el evento de ciclo de vida aún no llegó a la copia local del consumidor. | Justifica que el consumidor opere contra su copia local y **difiera por consistencia eventual** (`[D15]`, `[R29]`) la parte que requiere una unidad cuyo evento aún no llegó, sin bloquear ni aproximar. Ya no se requiere un canal de demanda hacia Estructura Organizacional (la señal/bandeja se retiraron, issue #72): la creación de unidades sigue su curso normal por planeación del administrador. |

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
| Tipo de unidad | Agregar | `agregar_tipo_unidad` |
| Tipo de unidad | Modificar | `modificar_tipo_unidad` |
| Tipo de unidad | Inactivar | `inactivar_tipo_unidad` |
| Unidad organizacional | Crear (F1) | `crear_unidad` |
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
| **1.6** | **2026-06-19** | **Consistencia del modelo de comunicación — replanteamiento de la validación de fecha efectiva (issue #56).** Se **retira `[SI10]`** (proyección local de última imputación) y con ella el acoplamiento que obligaba a EO a importar las imputaciones de todos los consumidores. **`[R25]`/`[I08]` replanteadas** (la regla no se quita, se replantea repartiendo responsabilidad): la fecha efectiva se gobierna en dos planos — validación local contra la jerarquía vigente (sistema) + coherencia con la actividad transaccional (responsabilidad del administrador, acto deliberado; transacciones inmutables + vista actual/histórica). Limpiadas las referencias a `[SI10]`/imputación (tabla comando↔SI, `[SI07]`, `[D15]`, sub-sección 3.8, VO `FechaEfectiva`, precondiciones F12/F13). Se conserva la numeración de `[SI11]`/`[SI12]` (hueco en SI10). Conteos: **11 sugerencias de implementación** (−1: `[SI10]` retirada); 16 invariantes (`[I08]` reformulada, no eliminada); 15 decisiones; 22 permisos — sin cambios. Acompaña al alcance v1.4 y a OXP v4.1 (retira el aviso de imputación) / Contabilidad v1.10. |
| **1.7** | **2026-06-23** | **Retiro del aparato de señal/bandeja — la copia local es para validación, no para la UI (issue #72/#73).** Una vez la asignación/distribución de la unidad en los consumidores se hace contra la fuente de verdad (la UI elige de Estructura Organizacional en vivo; las reglas se parametrizan contra ella), el caso "el consumidor necesita una unidad que el administrador aún no creó" deja de ocurrir en operación y `[P05]` se reformula. En consecuencia: **se retira `[SI11]`** (bandeja de sugerencias) y la **señal de demanda** entrante (`DemandaDeUnidadSenalada`) → SIs **11 → 10** (hueco en SI11, conserva numeración); **`[D15]` pasa de 4 partes a 3** (se retira "demanda por señal informativa"); **`[R30]` retirada** del alcance; **§3.8 reescrita** (se elimina el camino de visibilidad y el diagrama de señal/bandeja; se reencuadra el diferir a **consistencia eventual** y se agrega el **principio de capas**: la UI lee al dueño en vivo, la copia local es para validación, no API de lectura de la UI); **`[SI07]` reorientada** de idempotencia de la señal a idempotencia de los comandos del administrador (resuelta por `[SI06]`/`[SI01]`, sin tabla propia). Limpiadas las referencias a la bandeja/señal (tabla comando↔SI, `[SI04]`, evento `UnidadCreada`, permiso `crear_unidad`). Sin cambios en invariantes (16), decisiones (15, `[D15]` reformulada) ni permisos (22). Acompaña al alcance v1.5, a OXP (retira la emisión de la señal; aclara que la copia es para validación) y a la guía `datos-entre-dominios.md` (principio de capas). |
| **1.8** | **2026-07-08** | **Propósito documentado de `fechaEstimadaReactivacion` — dato informativo consistente con la suspensión (issue #88).** El campo deja de ser un dato capturado sin destino: se incorpora a la composición de `UnidadOrganizacional` (Sección 3.3) como dato **proyectado en read model** (molde de `motivoBaja`), presente solo mientras la unidad está en `Suspendida` y null al salir de ese estado. Su propósito queda explícito: expresa la **transitoriedad esperada de la suspensión** — quien suspende espera volver a operar la unidad; sin expectativa de retorno el camino es la inactivación — y es información de consulta para el administrador. **Ningún proceso la lee ni dispara reactivación automática**: la reactivación (F5) sigue siendo un gesto manual. Evento `UnidadSuspendida` (Sección 5.1) anotado en información capturada y efectos. Sale de la lista de "datos capturados no almacenados" (queda solo `motivo`). Acompaña al alcance v1.6. |
| **1.9** | **2026-07-08** | **Retiro del descarte automático de Borradores — perdió su driver original (issue #87).** El mecanismo nació para limpiar solicitudes de consumidores no atendidas; al eliminarse la creación desde consumidores (#46), solo cubría borradores que el propio administrador creó y abandonó — caso inocuo que no justifica un proceso programado por tenant con estado persistido y lock distribuido. Se retiran: **`[D09]`** (hueco en la numeración), **`[SI05]`** (hueco), el servicio **`DescarteAutomaticoBorradores`** de la Sección 3.6, el atributo **`fechaUltimaActividadBorrador`** y el guard **`puedeDescartarseAutomáticamente(umbral)`**. **`[SI04]` reorientada:** la antigüedad del borrador pasa a ser dato informativo derivado de los eventos, para decisión del administrador (mismo criterio del #88); ninguna política automática la lee. El descarte queda con dos vías con dueño claro: **F8 manual** (decisión del administrador) y **cascada F10** (se conserva intacta). **Literal `abandono_por_inactividad` renombrado a `cascada_grupo`** — su único uso restante es la cascada F10 y el nombre viejo ya no correspondía a ninguna inactividad real (2.5, composición, `validarCoherenciaBaja()`, VO `MotivoBaja`, saga F10, FSM, `UnidadDescartada`, catálogo 6.2, `[I05]`, `[D08]`). Nota de `Inactiva` en la FSM corregida: el motivo de cascada no aplica a `Inactiva` (un Borrador arrastrado por la cascada termina en `Descartada`). Conteos: **9 sugerencias de implementación** (−1), **14 decisiones** (−1), **2 domain services** (−1); invariantes (16) y permisos (22) sin cambios. Acompaña al alcance v1.7. |
| **2.0** | **2026-07-08** | **Replanteamiento — `TipoUnidad` pasa de entidad interna del grupo raíz a agregado raíz propio con ámbito del tenant (issue #86).** La decisión original se apoyaba en "pocos estados ⇒ no agregado" — un criterio equivocado: la frontera de agregado se define por invariantes, identidad y ciclo de vida. `TipoUnidad` cumple los criterios de agregado (ciclo de vida propio, referenciado por identidad desde `UnidadOrganizacional`, sin invariantes cross-tipo) y el diseño anterior producía síntomas visibles: `tiposUnidad` vacío en todos los grupos salvo el raíz, nombre del tipo inmutable de facto (era la llave de referencia), lectura siempre cross-agregado vía la herencia dinámica, y la regla del "catálogo congelado" en grupo `Inactivo` aplicable solo a un raíz sin contenido (letra muerta frente a `[R03]`). **Cambios:** nueva **Sección 3.4** (agregado `TipoUnidad`: id estable, nombre único por tenant y **modificable**, ámbito tenant); `GrupoOrganizacional` pierde `tiposUnidad`, `tiposVigentes()` y los 3 eventos de configuración (eventos propios 7 → 4); `UnidadOrganizacional` referencia por **`tipoUnidadId`** y sus eventos incluyen además el nombre vigente como dato informativo para consumidores; Sección 5.4 reescrita (eventos en stream propio del tipo, `TipoUnidadModificado` admite renombrar); **`[D10]` reescrita** con la decisión y el criterio correctos; **`[D14]` retirada** (hueco — la herencia dinámica desaparece; el catálogo es plano del tenant); nueva **`[SI13]`** (índice único de nombre por tenant, patrón `[SI01]`; el nombre de un tipo inactivado no se libera); `[I07]` valida contra el catálogo del tenant; FSM del grupo sin eventos `TipoUnidad*` y sin regla de congelamiento; precondición muerta de `UnidadTrasladada` ("el nuevo padre admite el tipo") eliminada; Sección 6.1 (precargados = agregados creados al inicializar el tenant), relaciones (3.8) y permisos ajustados; secciones 3.4-3.8 renumeradas a 3.5-3.9 con sus referencias vivas. Conteos: **3 agregados raíz** (+1), **0 entidades internas**, 18 eventos (sin cambio: 11 unidad + 4 grupo + 3 tipo), **13 decisiones** (−1), **10 sugerencias de implementación** (+1), 16 invariantes y 22 permisos sin cambios. Alcance sin cambios (ya describía el catálogo como extensible por empresa; "catálogo vigente" sigue siendo válido). Prepara el terreno del #85: el grupo raíz pierde el propósito de alojar el catálogo. |
| **2.1** | **2026-07-08** | **Replanteamiento — la estructura pasa de árbol con raíz única obligatoria a bosque (issue #85).** Nueva decisión **`[D16]`**: "ser tope" = propiedad derivada `padreId == null`; un tenant puede tener varios topes; ningún grupo se crea automáticamente al inicializar el tenant; la frontera de consolidación es el **tenant**, no un nodo. La arqueología del raíz único mostró que nació en el anexo v1.1 (Decisión 2) como regla estructural **sin justificación registrada**, y sus propósitos no resistieron análisis (el ancestro único no lo necesita ningún mecanismo; el catálogo salió del raíz en el #86; el "total compañía" lo da el tenant); arrastraba dos huecos (código/nombre de inicialización indefinidos; unicidad sin mecanismo). Coherente con la visión multi-jerarquía de `[DA1]` y con la homologación del ERP actual (centros de costo **maestros** —varios por empresa, consolidan reportes— ↔ grupos; **auxiliares** transaccionales ↔ unidades). **Cambios:** se retiran **`[I13]`** (hueco, invariantes 16 → **15**), el atributo almacenado **`esRaiz`** y el caso especial del raíz en la FSM 4.2; `padreId` admite null para cualquier tope; `puedeInactivarse()` queda en `estado == Activo` (inactivar un tope = F10 normal con confirmación de impacto); `GrupoCreado` sin `esRaiz` y con `padreId` null para topes; `grupoIdOrigen` reetiquetado "grupo origen"; relaciones (3.8) con cardinalidad de bosque; corrección de paso en convenciones (2.4 decía "dos agregados raíz" pese al #86). Conteos: **15 invariantes** (−1), **14 decisiones** (+1: `[D16]`); lo demás sin cambios. Acompaña al alcance v1.8 y al anexo v1.3. |
| **2.2** | **2026-07-08** | **Término definitivo del #85: "grupo de primer nivel" + comportamiento calculado `esDePrimerNivel()` + `nivel` cuenta desde 1.** Cierra la discusión de nomenclatura del bosque con el usuario: **(a)** "tope" se descartó por poco claro; **(b)** "raíz" se descartó porque colisiona con el concepto retirado ("grupo raíz" = único/automático/protegido — reusarlo invitaría a la confusión que el #85 eliminó); **(c)** un **atributo almacenado** se descartó porque permitiría el estado imposible "primer nivel con padre" — la condición es **derivada** (`padreId == null`) y por construcción no puede contradecirse. Para que el null no cargue semántica implícita (buena práctica señalada por el usuario), el concepto queda **nombrado en cada capa**: glosario ("Grupo de primer nivel", en plural desde la definición), comportamiento calculado **`esDePrimerNivel()`** en `GrupoOrganizacional` (las reglas y el código preguntan por el método, nunca por el null), y proyección `[SI02]` (expone `esDePrimerNivel` y `nivel` derivados para UI y reportes). **`nivel` pasa a contar desde 1** (los grupos de primer nivel tienen nivel 1) — la numeración desde 0 contradecía el lenguaje de negocio. Reemplazo de "tope" en composición, relaciones (3.8), `GrupoCreado` y `[D16]`. Acompaña al alcance v1.9 y al anexo v1.4. |
