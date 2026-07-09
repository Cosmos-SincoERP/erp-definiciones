# Definición de Alcance — Estructura Organizacional

## Tabla de contenido

1. [Definición, Contexto actual y problema a resolver](#sección-1-definición-contexto-actual-y-problema-a-resolver)
2. [Glosario de términos](#sección-2-glosario-de-términos)
3. [Actores del sistema](#sección-3-actores-del-sistema)
4. [Flujo principal](#sección-4-flujo-principal)
5. [Integraciones](#sección-5-integraciones)
6. [Reglas de negocio](#sección-6-reglas-de-negocio)
7. [Qué está dentro y fuera del alcance](#sección-7-qué-está-dentro-y-fuera-del-alcance)
8. [Estrategia de implementación por fases](#sección-8-estrategia-de-implementación-por-fases)
9. [Beneficios esperados](#sección-9-beneficios-esperados)

---

## Sección 1: Definición, Contexto actual y problema a resolver

### Definición

Estructura Organizacional es el registro centralizado de la estructura de unidades de la empresa a las que se asignan transacciones para efectos de control de gestión. Es la fuente de verdad de la pregunta "¿a qué unidad de la organización pertenece esta transacción?".

El sub-dominio gobierna una estructura jerárquica compuesta por dos tipos de nodo:

- **Grupo organizacional** — agrupador para organización y presentación en reportes. No recibe transacciones.
- **Unidad organizacional** — nivel de detalle donde se imputan las transacciones.

La jerarquía puede tener múltiples niveles mediante grupos anidados. Las unidades organizacionales siempre son nodos hoja y no pueden tener hijos.

La jerarquía soporta navegación por niveles y es la base para los reportes de gestión agrupados por área, proyecto, sucursal, inmueble o cualquier otro tipo de unidad que la organización quiera controlar. Cada unidad tiene un **tipo** (centro de costo, proyecto, sucursal, inmueble, departamento, entre otros) que permite a cada sub-dominio consumidor interpretarla según su contexto.

### Contexto actual

En **SincoA&F** (el ERP actual) no existe un sub-dominio de Estructura Organizacional con responsabilidades propias. Lo que existe hoy es un **CRUD simple de centros de costo** que vive dentro del sistema contable. Los demás módulos del ERP actual que producen hechos económicos (cuentas por pagar, cuentas por cobrar, activos fijos, arrendamientos, nómina, entre otros) se limitan a **mapear** ese CRUD desde sus propios flujos para poder entregar sus hechos económicos como representaciones contables. No hay ciclo de vida formal, no hay eventos hacia los consumidores, la jerarquía se expresa únicamente a través del código alfanumérico del centro de costo, y no hay un concepto de reestructuración — fusiones, divisiones o traslados se resuelven con renombres manuales o reasignaciones directas en cada módulo.

### Problema actual

1. **Sin comunicación controlada hacia los consumidores:** No hay canal centralizado por el cual los módulos se enteren de cambios en las unidades (inactivación, cambio de datos, reestructuración). En la práctica, los módulos transaccionales terminan registrando operaciones contra unidades inactivas, y el error se descubre tarde (al contabilizar, al reportar, al consolidar).
2. **Jerarquía limitada por codificación alfanumérica:** La estructura de árbol se expresa únicamente a través del código del centro de costo (ej: `01.02.03`), no hay un modelo formal de padres e hijos. Empresas con estructuras más robustas (múltiples sucursales, proyectos con sub-proyectos anidados, matrices con varios ejes) no caben en las combinaciones que permite el código — se topan con un techo de modelado.
3. **Nomenclatura limitante:** "Centro de costo" es un término contable que genera confusión en módulos no contables (ABR, Nómina) donde la unidad representa un inmueble o un departamento.
4. **Sin tipos de unidad:** No hay clasificación formal que permita distinguir un centro de costo de un proyecto, una sucursal o un inmueble. Cada módulo asume un tipo por convención.
5. **Creación dispersa:** Cada módulo crea unidades desde sus propios flujos sin pasar por un proceso centralizado que valide unicidad de código y posición en la jerarquía.
6. **Sin concepto de reestructuración:** Fusiones, divisiones y traslados no son procesos formales. Se resuelven con renombres o reasignaciones manuales, lo que rompe la trazabilidad histórica de las transacciones.

### Implementación inicial

El ERP opera como sistema multi-país y multi-moneda desde el diseño. El sub-dominio de Estructura Organizacional está dimensionado para soportar empresas con estructuras de hasta ~2.000 unidades organizacionales cada una, distribuidas en una jerarquía de múltiples niveles.

Consumidores en F1: **OXP** y **Contabilidad** (los dos sub-dominios transaccionales del ERP Cosmos que dependen de unidades de imputación para liberarse a desarrollo en F1). En fases posteriores se incorporan los demás sub-dominios del ERP que imputen transacciones a unidades organizacionales.

### Nomenclatura del sub-dominio

La decisión de nombre está resuelta en [`anexo-definicion-contexto-inicial.md`](anexo-definicion-contexto-inicial.md). Resumen:

| Término | Evaluación |
|---------|-----------|
| **Centro de costos** | Término contable/financiero. Limitante — una unidad puede ser un proyecto, una sucursal o un inmueble. Genera confusión en módulos no contables. |
| **Destino de negocio** | Usado en OXP como Shared Kernel. Descriptivo pero abstracto, no comunica jerarquía ni ciclo de vida. |
| **Estructura Organizacional** | Describe la responsabilidad completa: estructura con jerarquía, tipos y reestructuración. Usado por SAP (Enterprise Structure) y Oracle (Organization Structure). |

**Decisión:** el sub-dominio se nombra **Estructura Organizacional**. Los dos niveles jerárquicos se llaman **Grupo organizacional** y **Unidad organizacional** (patrón Odoo adaptado). Todos los sub-dominios consumidores adoptan "unidad organizacional" en sus documentos de dominio.

---

## Sección 2: Glosario de términos

| # | Término | Definición |
|---|---------|-----------|
| 1 | **Estructura organizacional** | Registro centralizado de la estructura de unidades de la empresa. Conjunto de grupos y unidades organizacionales enlazados en una jerarquía versionada por fecha efectiva. Es la fuente de verdad de la pregunta "¿a qué unidad de la organización pertenece esta transacción?". |
| 2 | **Grupo organizacional** | Nodo agrupador de la jerarquía. Puede contener cualquier combinación de sub-grupos y unidades organizacionales como hijos. No recibe transacciones ni admite imputaciones. Tiene ciclo de vida propio con dos estados (`Activo`, `Inactivo`); inactivar un grupo propaga la inactivación en cascada a todos sus hijos. |
| 3 | **Unidad organizacional** | Nodo hoja de la jerarquía donde se imputan las transacciones. Representa el destino concreto de un hecho económico para efectos de control de gestión. Pertenece a exactamente un grupo padre y nunca tiene hijos — si un caso de negocio requiere estructura adicional bajo una unidad, se modela mediante un grupo intermedio. Su ciclo de vida está definido por los cinco estados descritos en el término 10. |
| 4 | **Tipo de unidad** | Clasificación de una unidad organizacional según su naturaleza: centro de costo, proyecto, sucursal, inmueble, departamento, entre otros. Permite a cada sub-dominio consumidor interpretar la unidad según su contexto. |
| 5 | **Código** | Identificador plano de texto libre de una unidad o grupo organizacional, único por tenant (sin distinguir mayúsculas de minúsculas). No embebe información jerárquica — la jerarquía se modela como estructura separada; los separadores (guion, punto, barra) son cosméticos. Su longitud cumple el rango configurado por el tenant (por defecto 2 a 12 caracteres) dentro de la envolvente del dominio (R10). |
| 6 | **Nombre** | Denominación descriptiva de la unidad o grupo organizacional, destinada a la lectura humana en UI y reportes. |
| 7 | **Jerarquía** | Estructura de árbol que enlaza grupos y unidades organizacionales mediante relaciones padre-hijo con vigencia por fecha efectiva. Se modela como agregado separado del código de cada unidad o grupo. |
| 8 | **Grupo de primer nivel** | Grupo organizacional sin padre: encabeza su propio árbol. Un tenant puede tener **varios** grupos de primer nivel — la estructura es un bosque, no un árbol único. Ningún grupo se crea automáticamente: todos son actos deliberados del administrador. La consolidación "total compañía" no depende de un grupo de primer nivel: la da el tenant (todas las unidades del tenant). En el modelo es una condición **derivada** de no tener padre (comportamiento `esDePrimerNivel()`), no un atributo almacenado. *(Reemplaza al término "Grupo raíz" — único, automático y protegido — retirado en el issue #85.)* |
| 9 | **Nivel** | Profundidad de un nodo (grupo o unidad) dentro de la jerarquía. Se calcula a partir de la estructura, no se almacena en el código. |
| 10 | **Estado de la unidad** | Condición del ciclo de vida de una unidad organizacional. Cinco estados: **Borrador** (en preparación, no transaccional), **Activa** (recibe imputaciones), **Suspendida** (transitoriamente no recibe imputaciones, pero consultable), **Inactiva** (dada de baja después de haber operado; el historial se conserva; reabrible si la unidad retoma operación) y **Descartada** (terminal estricto, nunca llegó a operar; se filtra de reportes históricos). |
| 11 | **Borrador** | Estado de **preparación del administrador**: una unidad que aún no es transaccional, mientras el administrador termina de definirla antes de activarla (Flujo 1). **No se origina desde sub-dominios consumidores** (ver R29). Permite su referencia en flujos de planeación pero bloquea la imputación hasta que se active. |
| 12 | **Activa** | Estado operativo de una unidad organizacional. Recibe imputaciones en nuevas transacciones. |
| 13 | **Suspendida** | Estado transitorio en el que una unidad organizacional no recibe nuevas imputaciones pero sigue siendo consultable y reportable. Aplica a situaciones como cierre temporal, disputa en curso o congelamiento gerencial. |
| 14 | **Inactiva** | Estado en el que una unidad organizacional deja de recibir imputaciones después de haber operado. Los registros históricos que la referencian se conservan intactos y siguen apareciendo en reportes históricos. **Puede reabrirse** si la unidad retoma operación (ej: sucursal que reabre, proyecto que se reanuda); la reapertura emite un evento de auditoría dedicado (`UnidadReabierta`) distinto del de reactivación desde `Suspendida`. |
| 15 | **Descartada** | Estado terminal de una unidad organizacional que nunca llegó a operar. Aplica cuando una unidad creada en `Borrador` se rechaza o se abandona antes de activarse. A diferencia de `Inactiva`, la unidad no tiene historial transaccional y se filtra de los reportes históricos. La identificación queda libre para futuras solicitudes. |
| 16 | **Reestructuración** | Proceso formal que modifica la estructura organizacional. Agrupa los tres tipos de cambio de primera clase: fusión, división y traslado. Cada uno emite su propio evento de dominio con fecha efectiva, aprobador y razón. |
| 17 | **Fusión** | Proceso por el cual dos o más unidades organizacionales se integran en una sola unidad destino. El historial transaccional de las unidades origen queda enlazado al destino. |
| 18 | **División** | Proceso por el cual una unidad organizacional se separa en varias unidades destino. El historial transaccional previo a la fecha efectiva permanece referenciado a la unidad origen; las unidades destino reciben únicamente nuevas imputaciones a partir de la fecha efectiva. Los reportes pueden ofrecer vistas comparativas que permitan analizar la estructura antes y después de la división sin redistribuir el historial transaccional original. |
| 19 | **Traslado** | Proceso por el cual una unidad organizacional cambia de padre en la jerarquía conservando su identidad, su código y su historial transaccional. |
| 20 | **Fecha efectiva** | Momento en el que una versión de la jerarquía o una reestructuración comienza a regir. Habilita la historia estructural versionada y la comparabilidad de reportes entre periodos. |
| 21 | **Dimensión de imputación** | Eje independiente al que se puede imputar una transacción. En F1 solo se expone `Unidad Organizacional` como dimensión operativa; en fases posteriores se incorporan otras (Proyecto, Sucursal, Línea de Negocio, entre otras) sin rediseño estructural. Ver `anexo-decisiones-arquitectonicas.md`, Decisión 4. |
| 22 | **Unidad de imputación** | Sinónimo operativo de "unidad organizacional" usado por Contabilidad y otros sub-dominios consumidores cuando se refieren al destino de imputación de una línea de traducción. En los documentos de dominio de Estructura Organizacional se prefiere y se usa exclusivamente el término **unidad organizacional**; este glosario registra el sinónimo solo para referencia cruzada con Contabilidad y otros sub-dominios consumidores que ya lo hayan adoptado. |
| 23 | **Consumidor** | Sub-dominio del ERP que lee unidades organizacionales y reacciona a sus eventos. En F1: OXP y Contabilidad. En fases posteriores: los demás sub-dominios que se incorporen y consuman unidades organizacionales. |

---

## Sección 3: Actores del sistema

### Actores internos (usuarios del sistema)

| Actor | Descripción | Responsabilidades |
|-------|-------------|-------------------|
| **Administrador de estructura organizacional** | Usuario encargado de gobernar la estructura de grupos y unidades organizacionales. Perfil típico: controller, gerente de planeación o administrador del ERP. | Gestionar el ciclo de vida de grupos y unidades (crear, activar, suspender, inactivar). Gestionar la jerarquía. Ejecutar fusiones, divisiones y traslados con su fecha efectiva y razón. Crear las unidades que la planeación y la operación de la empresa requieran. |
| **Usuario consumidor** | Usuario de cualquier sub-dominio del ERP que interactúa con unidades organizacionales en su flujo. No se subdivide en roles específicos — el sistema es inteligente y guía al usuario según el contexto de la tarea. | Consultar unidades (contra la fuente de verdad, vía la UI). Referenciarlas en sus transacciones. |

### Actores externos (sistemas integrados)

| Sistema | Descripción | Relación con el dominio |
|---------|-------------|------------------------|
| **OXP** | Gestión de obligaciones por pagar. | Consume unidades organizacionales para imputar obligaciones. Escucha los eventos del ciclo de vida (creación, activación, suspensión, inactivación) y de reestructuración para mantener su **copia local** consistente, contra la que valida y opera. |
| **Contabilidad** | Motor de traducción contable. | Consume unidades como dimensión de imputación en las líneas de traducción, contra su **copia local**. Reacciona a eventos de reestructuración para reclasificación contable. |
| **Sub-dominios consumidores futuros** | Cualquier sub-dominio que se incorpore al ERP y requiera imputar transacciones a unidades organizacionales. | Consume unidades y reacciona a sus eventos siguiendo el mismo esquema de copia local mantenida por eventos. |

### Formatos de entrada soportados

No aplica. Estructura Organizacional no recibe documentos externos — las unidades y grupos se crean desde la UI propia del sub-dominio o mediante solicitudes originadas desde sub-dominios consumidores.

### Nota sobre la operación de los consumidores

Estructura Organizacional es el **dueño único** de las unidades; los consumidores **operan contra su copia local** (mantenida por los eventos de ciclo de vida) y **nunca consultan a Estructura Organizacional en el camino crítico** de sus operaciones. La unidad se elige siempre de la fuente de verdad (la UI lee Estructura Organizacional en vivo; las reglas de distribución del consumidor se parametrizan contra ella), así que una unidad referenciada siempre existe en el dueño; si su evento aún no llegó a la copia local, **la operación del consumidor no se detiene**: registra lo que puede y difiere solo la parte que requiere la unidad, que se resuelve cuando el evento llega (consistencia eventual). La creación de unidades es siempre **acto deliberado de Estructura Organizacional** (administrador o integración) — ningún flujo consumidor crea unidades ni las origina en un estado bloqueante. El fundamento de patrones (réplica local, estrategias para datos faltantes, anti-patrones, y el principio de capas UI/dominio) está en [`../../guias-de-modelado/datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md).

---

## Sección 4: Flujo principal

> **Nota sobre el sistema inteligente:** las intervenciones del sistema inteligente descritas en los flujos —prellenar datos, sugerir códigos, advertir impactos, detectar unidades similares o presentar candidatos— son capacidades de asistencia de producto. No reemplazan las validaciones del dominio ni constituyen reglas de negocio por sí mismas, salvo cuando el flujo indique expresamente que se requiere confirmación explícita del administrador.

Los flujos operativos del sub-dominio se agrupan en cuatro familias: **Creación**, **Gestión del ciclo de vida**, **Reestructuración** y **Actualización de datos**. Cada flujo dentro de una familia se mapea 1:1 con un comando del modelo de dominio.

### Creación

#### Flujo 1 — Creación directa por el administrador

Escenario en el que el administrador de estructura organizacional crea un grupo o una unidad desde la UI del propio módulo. Es el camino canónico cuando la empresa decide estructuralmente abrir una sucursal, lanzar un proyecto o crear un centro de costo.

```
 Administrador en el módulo de Estructura Organizacional
       │
       │  Selecciona "Crear unidad" / "Crear grupo"
       ▼
 ┌────────────────────────────────────────────┐
 │ Sistema inteligente pre-llena:             │
 │   · tipo inferido del contexto             │
 │   · padre probable en la jerarquía         │
 │   · código siguiente disponible            │
 │   · nombre canónico sugerido               │
 └──────────────────────┬─────────────────────┘
                        │
                        │  Admin ajusta datos y
                        │  elige estado inicial
                        │  (Borrador o Activa)
                        ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · código único por tenant                │
 │   · tipo válido                            │
 │   · padre existente y coherente            │
 │   · posición válida en la jerarquía        │
 └──────────────────────┬─────────────────────┘
            ┌───────────┴───────────┐
            ▼                       ▼
      Validación OK          Validación falla
            │                       │
            ▼                       ▼
   Crea y emite:             Rechaza con error
     UnidadCreada             Admin corrige y
    (+ UnidadActivada          reintenta
     si el estado inicial
     fue Activa)
            │
            ▼
 Consumidores (OXP, Contabilidad) reciben
 los eventos y replican la referencia local.
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** el tipo seleccionado está dentro del catálogo vigente; si se indica padre, éste existe y está en estado `Activa`.
- **Eventos emitidos:** `UnidadCreada` (siempre) o `GrupoCreado` según aplique; `UnidadActivada` si se eligió estado inicial `Activa`.
- **Estado resultante:** la unidad queda en `Borrador` o `Activa` según la decisión del administrador.

---

#### Flujo 2 — Operación de un consumidor contra su copia local

Escenario en el que un sub-dominio consumidor (OXP, Contabilidad, etc.) imputa contra una unidad organizacional. El consumidor **opera contra su copia local** de unidades —mantenida por los eventos de Estructura Organizacional— y **nunca consulta a Estructura Organizacional en el momento de operar** (ver [`../../guias-de-modelado/datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md)). Como la unidad se elige siempre de la fuente de verdad (la UI lee Estructura Organizacional en vivo; las reglas de distribución del consumidor se parametrizan contra ella), una unidad referenciada **siempre existe**; lo único que puede faltar es que su evento de ciclo de vida aún no haya llegado a la copia local (desfase de propagación).

```
 Sub-dominio consumidor (OXP, Contabilidad, ...) imputa a una unidad
       │
       ▼
 ¿el evento de esa unidad ya llegó a su COPIA LOCAL? ── SÍ ──► imputa contra la copia
       │                                                       (no consulta a Estructura Org.)
       NO (desfase de propagación; la unidad sí existe en el dueño)
       │
       └──► el consumidor NO se detiene: registra lo que puede y difiere
            solo la parte que requiere la unidad (el cómo vive en el consumidor)
                     │
                     ▼ (cuando llega) evento UnidadCreada / UnidadActivada
            La copia local del consumidor se actualiza →
            la parte diferida se resuelve sola (consistencia eventual).
```

- **Actor principal:** el sub-dominio consumidor opera de forma autónoma contra su copia local.
- **Pre-condiciones:** el consumidor mantiene su copia local de unidades al día (eventos de Estructura Organizacional); ante un desfase prolongado, repara la copia de fondo por el punto de resincronización, nunca en el camino crítico.
- **Comportamiento clave:** la operación del consumidor **no se detiene** por un evento de unidad aún no propagado (difiere solo la parte que la requiere — el detalle vive en el consumidor); la unidad **no se aproxima** con un valor provisional (debe coincidir exacto con la contabilidad). Ningún flujo consumidor crea unidades.
- **Estado resultante:** la parte diferida del consumidor se resuelve sola cuando el evento de la unidad llega a su copia local.

---

### Gestión del ciclo de vida (unidades)

#### Flujo 3 — Activación de una unidad en `Borrador`

Escenario en el que el administrador activa una unidad que fue creada en estado `Borrador` — una que dejó provisionalmente en preparación (Flujo 1) mientras completaba sus datos.

```
 Administrador revisa bandeja de unidades en Borrador
       │
       │  Selecciona una unidad pendiente
       ▼
 ┌────────────────────────────────────────────┐
 │ Sistema inteligente muestra:               │
 │   · unidades similares existentes          │
 │     (por si conviene reutilizar)           │
 │   · datos faltantes sugeridos              │
 └──────────────────────┬─────────────────────┘
                        │
                        │  Admin completa datos
                        │  y ejecuta "Activar"
                        │  (opcionalmente con motivo)
                        ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · datos mínimos completos                │
 │   · grupo padre en estado Activo           │
 │   · no existe unidad Activa con el         │
 │     mismo código                           │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
               Emite: UnidadActivada
                        │
                        ▼
 Consumidores (OXP, Contabilidad, ...) reciben
 el evento, actualizan su copia local y las
 operaciones diferidas a la espera de esta
 unidad se resuelven solas.
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** la unidad está en estado `Borrador`; sus datos mínimos están completos; el grupo padre está en `Activo`.
- **Eventos emitidos:** `UnidadActivada`.
- **Estado resultante:** `Activa`.

---

#### Flujo 4 — Suspensión de una unidad activa

Escenario en el que una unidad activa entra en un período transitorio durante el cual no debe recibir nuevas imputaciones pero sigue siendo consultable y reportable (sucursal cerrada por remodelación, proyecto en hold por disputa, centro de costo congelado por decisión gerencial).

La fecha estimada de reactivación es un dato informativo para el administrador, consistente con la naturaleza transitoria de la suspensión: quien suspende espera volver a operar la unidad, y esta fecha expresa cuándo. Si no existe expectativa de retorno, el camino correcto es la inactivación (Flujo 7). Ningún proceso reacciona a esta fecha — la reactivación (Flujo 5) es siempre un gesto manual del administrador.

```
 Administrador selecciona una unidad Activa
       │
       │  Ejecuta "Suspender"
       │  (opcionalmente registra motivo y/o
       │   fecha estimada de reactivación)
       ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · la unidad está en estado Activa        │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
               Emite: UnidadSuspendida
                        │
                        ▼
 Consumidores reciben el evento y bloquean
 nuevas imputaciones contra la unidad.
 La unidad sigue siendo consultable y
 reportable en datos históricos.
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** la unidad está en estado `Activa`.
- **Eventos emitidos:** `UnidadSuspendida`.
- **Estado resultante:** `Suspendida`.

---

#### Flujo 5 — Reactivación de una unidad suspendida

Escenario en el que la situación transitoria que motivó la suspensión finaliza y la unidad vuelve a operar normalmente.

```
 Administrador selecciona una unidad Suspendida
       │
       │  Ejecuta "Reactivar"
       │  (opcionalmente registra motivo)
       ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · la unidad está en estado Suspendida    │
 │   · el grupo padre está en Activo          │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
               Emite: UnidadReactivada
                        │
                        ▼
 Consumidores reciben el evento y vuelven a
 permitir imputaciones contra la unidad.
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** la unidad está en estado `Suspendida`; el grupo padre está en `Activo`.
- **Eventos emitidos:** `UnidadReactivada`.
- **Estado resultante:** `Activa`.

---

#### Flujo 6 — Reapertura de una unidad inactiva

Escenario en el que una unidad previamente inactivada vuelve a operar. Aplica cuando la decisión de baja se revierte por un cambio de negocio (sucursal cerrada que reabre, proyecto cancelado que se reanuda, centro de costo discontinuado que vuelve a usarse) o cuando la inactivación fue un error operativo del administrador. La unidad conserva su identidad, código e historial transaccional — la reapertura los enlaza con la nueva operación. Para auditoría, la reapertura emite un evento dedicado (`UnidadReabierta`) distinto del de reactivación desde `Suspendida` (`UnidadReactivada`).

```
 Administrador selecciona una unidad Inactiva
       │
       │  Ejecuta "Reabrir"
       │  (opcionalmente registra motivo —
       │   recomendado para trazabilidad)
       ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · la unidad está en estado Inactiva      │
 │   · el grupo padre está en Activo          │
 │     (si fue inactivado en cascada, se      │
 │      debe reactivar el grupo primero)      │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
               Emite: UnidadReabierta
                        │
                        ▼
 Consumidores reciben el evento y vuelven a
 permitir imputaciones contra la unidad. El
 historial previo a la inactivación se
 mantiene visible en reportes; las nuevas
 imputaciones se enlazan en continuidad.
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** la unidad está en estado `Inactiva`; el grupo padre está en `Activo`.
- **Eventos emitidos:** `UnidadReabierta`.
- **Estado resultante:** `Activa`.

---

#### Flujo 7 — Inactivación de una unidad

Escenario en el que una unidad **que operó** deja de hacerlo (cierre de sucursal, proyecto finalizado, centro de costo discontinuado). Los registros históricos que la referencian se conservan intactos; las consultas y reportes históricos siguen siendo posibles, y no puede recibir nuevas imputaciones. Si más adelante la unidad necesita volver a operar, el administrador la reabre mediante el Flujo 6.

```
 Administrador selecciona una unidad
 (Activa o Suspendida)
       │
       │  Ejecuta "Inactivar"
       │  (opcionalmente registra motivo)
       ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · la unidad está en Activa o Suspendida  │
 │     (las unidades en Borrador van por      │
 │      el flujo de Descarte — F8)            │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
               Emite: UnidadInactivada
                        │
                        ▼
 Consumidores reciben el evento y bloquean
 nuevas imputaciones contra la unidad.
 Los registros históricos se conservan y
 siguen apareciendo en reportes históricos.
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** la unidad está en estado `Activa` o `Suspendida`.
- **Eventos emitidos:** `UnidadInactivada`.
- **Estado resultante:** `Inactiva` (reabrible mediante el Flujo 6).

---

#### Flujo 8 — Descarte de una unidad en `Borrador`

Escenario terminal en el que una unidad **que nunca operó** se descarta antes de activarse. Aplica cuando el administrador abandona un borrador propio porque la decisión de negocio que motivó la creación no se concretó (proyecto cancelado, sucursal no autorizada). La unidad no aparece en reportes históricos y la identificación queda libre.

```
 Administrador selecciona una unidad
 en estado Borrador
       │
       │  Ejecuta "Descartar"
       │  (opcionalmente registra motivo)
       ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · la unidad está en estado Borrador      │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
               Emite: UnidadDescartada
                        │
                        ▼
 Consumidores reciben el evento y actualizan
 su copia local. Un borrador nunca es
 referenciado por la operación de otro
 sub-dominio, así que no hay nada que
 cancelar. La identificación queda libre.
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** la unidad está en estado `Borrador`.
- **Eventos emitidos:** `UnidadDescartada`.
- **Estado resultante:** `Descartada` (terminal estricto, no reabrible; identificación liberada).

---

### Gestión del ciclo de vida (grupos)

#### Flujo 9 — Creación y activación de un grupo

Los grupos solo los crea el administrador (no se solicitan desde sub-dominios consumidores — los consumidores referencian unidades, no grupos). La creación es lineal: se crea y queda directamente en estado `Activo`. No hay estado `Borrador` para grupos porque no reciben imputaciones y no requieren revisión previa para operar.

```
 Administrador en el módulo de Estructura Organizacional
       │
       │  Selecciona "Crear grupo"
       ▼
 ┌────────────────────────────────────────────┐
 │ Sistema inteligente pre-llena:             │
 │   · grupo padre probable                   │
 │   · código siguiente disponible            │
 │   · nombre canónico sugerido               │
 └──────────────────────┬─────────────────────┘
                        │
                        │  Admin ajusta datos
                        ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · código único por tenant                │
 │   · si se indica padre: existe y está      │
 │     en Activo, y la posición no            │
 │     introduce ciclos                       │
 │   · sin padre: el grupo nace como grupo    │
 │     de primer nivel (puede haber varios)   │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
        Emite: GrupoCreado (estado Activo)
                        │
                        ▼
 Consumidores con interés en la estructura
 jerárquica (reportería, sistemas de
 reportes consolidados) reciben el evento y
 actualizan su vista local.
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** el código propuesto no está en uso; si se indica grupo padre, existe y está en estado `Activo` (sin padre, nace como grupo de primer nivel — R31).
- **Eventos emitidos:** `GrupoCreado` (estado `Activo`).
- **Estado resultante:** `Activo`.

---

#### Flujo 10 — Inactivación de un grupo (con cascada a hijos)

Escenario en el que se inactiva un grupo. Como el grupo agrupa sub-grupos y/o unidades, la inactivación se **propaga en cascada** a todos sus descendientes. El sistema inteligente muestra el impacto antes de ejecutar para que el administrador entienda exactamente qué se va a inactivar (cuántos sub-grupos, cuántas unidades activas, cuántas en borrador, etc.). Si la inactivación se hizo por error o el negocio cambia, el grupo puede reactivarse posteriormente mediante el Flujo 11 (y las unidades afectadas pueden reabrirse una a una mediante el Flujo 6).

```
 Administrador selecciona un grupo Activo
       │
       │  Ejecuta "Inactivar"
       │  (opcionalmente registra motivo)
       ▼
 ┌────────────────────────────────────────────┐
 │ Sistema inteligente                        │
 │  Muestra impacto previsto:                 │
 │   · N sub-grupos que se inactivarán        │
 │   · N unidades Activas → Inactiva          │
 │   · N unidades Suspendidas → Inactiva      │
 │   · N unidades en Borrador → Descartada    │
 │  Exige confirmación explícita.             │
 └──────────────────────┬─────────────────────┘
                        │
                        │  Admin confirma
                        ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · el grupo está en estado Activo         │
 │ Aplica cascada recursiva:                  │
 │   · sub-grupos → Inactivo                  │
 │   · unidades en Activa/Suspendida →        │
 │     Inactiva                               │
 │   · unidades en Borrador → Descartada      │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
 Emite un evento por cada nodo afectado:
   GrupoInactivado (el grupo + sub-grupos)
   UnidadInactivada (por cada unidad operativa)
   UnidadDescartada (por cada unidad en
   Borrador colgando del grupo)
                        │
                        ▼
 Consumidores reciben los eventos, actualizan
 su copia local y bloquean nuevas imputaciones
 contra las unidades inactivadas (un borrador
 descartado nunca fue referenciado — no hay
 nada que cancelar).
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** el grupo está en estado `Activo`; el administrador confirmó el impacto previsto.
- **Eventos emitidos:** `GrupoInactivado` (por el grupo y cada sub-grupo afectado), `UnidadInactivada` (por cada unidad operativa afectada), `UnidadDescartada` (por cada unidad en `Borrador` afectada).
- **Estado resultante:** el grupo y todos sus descendientes pasan a estado no operativo (`Inactivo` para grupos y unidades operativas; `Descartada` para unidades en borrador). El grupo y sus sub-grupos pueden reactivarse posteriormente; las unidades inactivadas pueden reabrirse una a una.

---

#### Flujo 11 — Reactivación de un grupo

Escenario en el que un grupo inactivado vuelve a estar disponible para operar. La reactivación del grupo es directa y **no propaga en cascada a los hijos** — los sub-grupos y unidades que fueron inactivados/descartados al inactivar el grupo siguen como quedaron. El sistema inteligente identifica esos hijos afectados como candidatos y los presenta al administrador para que decida cuáles reabrir o reactivar uno a uno (mediante el Flujo 6 para unidades inactivadas, o un nuevo flujo de reactivación de sub-grupo). Las unidades que fueron descartadas no se recuperan: la `Descartada` es terminal estricto, y para volver a operar bajo ese código se crea una unidad nueva.

```
 Administrador selecciona un grupo Inactivo
       │
       │  Ejecuta "Reactivar"
       │  (opcionalmente registra motivo)
       ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · el grupo está en estado Inactivo       │
 │   · el grupo padre está en Activo          │
 │     (si fue inactivado en cascada, se      │
 │      debe reactivar el grupo padre antes)  │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
              Emite: GrupoReactivado
                        │
                        ▼
 ┌────────────────────────────────────────────┐
 │ Sistema inteligente                        │
 │  Identifica los hijos afectados por la     │
 │  cascada original:                         │
 │   · sub-grupos Inactivos (correlacionados) │
 │   · unidades Inactivas (correlacionadas)   │
 │   · unidades Descartadas (no recuperables) │
 │  Presenta una bandeja al administrador     │
 │  para que decida cuáles reabrir o          │
 │  reactivar uno a uno.                      │
 └────────────────────────────────────────────┘
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** el grupo está en estado `Inactivo`; el grupo padre está en `Activo`.
- **Eventos emitidos:** `GrupoReactivado`. Las reaperturas posteriores de unidades emiten `UnidadReabierta` (Flujo 6); las reactivaciones posteriores de sub-grupos emiten `GrupoReactivado` (mismo flujo, por cada uno).
- **Estado resultante:** el grupo queda en `Activo`. Los hijos previamente afectados por la cascada permanecen como estaban hasta que el administrador decida reabrirlos o reactivarlos uno a uno.

---

### Reestructuración

Los flujos de esta familia (Fusión, División, Traslado) son los que dan a Estructura Organizacional su característica distintiva frente a un CRUD tradicional de centros de costo: permiten que la estructura evolucione preservando trazabilidad histórica y comparabilidad IFRS 8 (ver `anexo-decisiones-arquitectonicas.md`, Decisión 3). Aplican únicamente a unidades organizacionales.

**Patrón común a los tres flujos:** las unidades que quedan dadas de baja por reestructuración pasan a estado `Inactiva` con la causa registrada en el evento (`fusion` o `division`) y proyectada como atributo `motivoBaja` en el modelo de lectura. La FSM no introduce estados nuevos por motivo de baja — la UI y los reportes diferencian visualmente los motivos a partir del atributo proyectado.

#### Flujo 12 — Fusión de unidades

Escenario en el que dos o más unidades organizacionales se integran en una sola unidad destino. Aplica cuando la empresa consolida operaciones (dos sucursales pequeñas se unen en una sola; dos centros de costo de áreas reorganizadas se funden). El historial transaccional de las unidades origen queda referenciado a las origen (que pasan a `Inactiva` con motivo `fusion`); las nuevas imputaciones se hacen contra la unidad destino. Los reportes históricos navegables muestran "vista actual" (todo en el destino a partir de la fecha efectiva) o "vista histórica" (cada periodo con la estructura de entonces).

```
 Administrador selecciona N unidades origen
 (Activas o Suspendidas) y una unidad destino
 (Activa) ya existente
       │
       │  Ejecuta "Fusionar"
       │  Captura: fecha efectiva, motivo
       ▼
 ┌────────────────────────────────────────────┐
 │ Sistema inteligente                        │
 │  Muestra impacto previsto:                 │
 │   · operaciones pendientes en consumidores │
 │     que se reasignarán al destino          │
 │   · efecto en reportes comparativos        │
 │  Exige confirmación explícita.             │
 └──────────────────────┬─────────────────────┘
                        │
                        │  Admin confirma
                        ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · todas las origen en Activa o           │
 │     Suspendida                             │
 │   · destino en Activa y distinta del       │
 │     conjunto origen                        │
 │   · fecha efectiva no anterior a la        │
 │     versión vigente de jerarquía de        │
 │     las unidades origen                    │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
 Emite, en este orden:
   1. UnidadFusionada (una vez, por la
      operación completa, con: lista de
      origen, destino, fecha efectiva,
      motivo)
   2. UnidadInactivada (por cada unidad
      origen, con motivoBaja: "fusion" y
      causalidad: "fusionada con <destinoId>")
                        │
                        ▼
 Consumidores reciben los eventos y reasignan
 sus referencias locales del conjunto origen al
 destino. Los reportes históricos se navegan
 con "vista actual" o "vista histórica" según
 la fecha de consulta.
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** todas las unidades origen están en `Activa` o `Suspendida`; el destino existe en `Activa` y no está en el conjunto origen; la fecha efectiva no es anterior a la versión vigente de jerarquía (validable localmente); su coherencia con la actividad transaccional la define y responde el administrador (R25).
- **Eventos emitidos:** `UnidadFusionada` (una vez, evento de proceso) + `UnidadInactivada` por cada unidad origen (con `motivoBaja: "fusion"` y referencia al destino).
- **Estado resultante:** las unidades origen quedan en `Inactiva` con `motivoBaja: "fusion"`; la unidad destino mantiene su estado `Activa` y absorbe las nuevas imputaciones desde la fecha efectiva.

---

#### Flujo 13 — División de una unidad

Escenario en el que una unidad organizacional se separa en varias unidades destino. Aplica cuando una operación crece y se desagrega (un centro de costo "Administración" se divide en "Administración Financiera" + "Administración de TI"; un proyecto matriz se descompone en sub-proyectos independientes). El historial transaccional de la unidad origen queda referenciado a la unidad origen (que pasa a `Inactiva` con motivo `division`); las unidades destino arrancan limpias y solo reciben las nuevas imputaciones que se hagan a partir de la fecha efectiva.

```
 Administrador selecciona una unidad origen
 (Activa o Suspendida) y N unidades destino
 (Activas) ya existentes
       │
       │  Ejecuta "Dividir"
       │  Captura: fecha efectiva, motivo
       ▼
 ┌────────────────────────────────────────────┐
 │ Sistema inteligente                        │
 │  Muestra impacto previsto:                 │
 │   · operaciones pendientes en              │
 │     consumidores y a qué destino se        │
 │     reasignarán por defecto                │
 │   · efecto en reportes comparativos        │
 │  Exige confirmación explícita.             │
 └──────────────────────┬─────────────────────┘
                        │
                        │  Admin confirma
                        ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · origen en Activa o Suspendida          │
 │   · al menos dos destinos, todos en        │
 │     Activa y distintos del origen          │
 │   · fecha efectiva no anterior a la        │
 │     versión vigente de jerarquía de        │
 │     la unidad origen                       │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
 Emite, en este orden:
   1. UnidadDividida (una vez, por la
      operación completa, con: origen, lista
      de destinos, fecha efectiva, motivo)
   2. UnidadInactivada (por la unidad origen,
      con motivoBaja: "division" y causalidad:
      "dividida en <listaDeDestinos>")
                        │
                        ▼
 Consumidores reciben los eventos y reasignan
 sus referencias según corresponda. Los
 reportes históricos se navegan con "vista
 actual" (operaciones nuevas en los destinos)
 o "vista histórica" (consolidado al origen
 hasta la fecha efectiva).
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** la unidad origen está en `Activa` o `Suspendida`; existen al menos dos unidades destino, todas en `Activa`, distintas del origen; la fecha efectiva no es anterior a la versión vigente de jerarquía (validable localmente); su coherencia con la actividad transaccional la define y responde el administrador (R25).
- **Eventos emitidos:** `UnidadDividida` (una vez, evento de proceso) + `UnidadInactivada` por la unidad origen (con `motivoBaja: "division"` y referencia a los destinos).
- **Estado resultante:** la unidad origen queda en `Inactiva` con `motivoBaja: "division"`; las unidades destino mantienen su estado `Activa` y reciben las nuevas imputaciones desde la fecha efectiva. El historial previo a la fecha efectiva permanece referenciado al origen.

---

#### Flujo 14 — Traslado de una unidad (cambio de grupo padre)

Escenario en el que una unidad organizacional cambia de grupo padre en la jerarquía. Aplica cuando la estructura se reorganiza sin que la unidad pierda identidad operativa (una sucursal se mueve de la región "Norte" a la región "Centro" porque se redibujan las zonas comerciales; un proyecto pasa del programa "Innovación" al programa "Operaciones" porque cambia su naturaleza). La unidad conserva su identidad, su código, su estado y su historial transaccional — solo cambia su posición en el árbol y la versión vigente de la jerarquía.

```
 Administrador selecciona una unidad
 (Activa o Suspendida) y un nuevo grupo padre
       │
       │  Ejecuta "Trasladar"
       │  Captura: fecha efectiva, motivo
       ▼
 ┌────────────────────────────────────────────┐
 │ Sistema inteligente                        │
 │  Muestra impacto previsto:                 │
 │   · cómo cambia la posición de la unidad   │
 │     en los reportes a partir de la         │
 │     fecha efectiva                         │
 │   · comparabilidad histórica con la        │
 │     posición anterior                      │
 │  Exige confirmación explícita.             │
 └──────────────────────┬─────────────────────┘
                        │
                        │  Admin confirma
                        ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · unidad en Activa o Suspendida          │
 │   · nuevo grupo padre existente y en       │
 │     Activo, distinto del padre actual      │
 │   · fecha efectiva no anterior a la        │
 │     última versión de jerarquía vigente    │
 │     para la unidad                         │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
              Emite: UnidadTrasladada
              (con: unidad, padre anterior,
               nuevo padre, fecha efectiva,
               motivo)
                        │
                        ▼
 La jerarquía registra una nueva versión con
 la fecha efectiva. Consumidores con interés
 jerárquico (reportería) actualizan su vista.
 La unidad sigue operando normalmente con su
 mismo código.
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** la unidad está en `Activa` o `Suspendida`; el nuevo grupo padre existe, está en `Activo` y es distinto del padre actual; la fecha efectiva es coherente con la versión vigente de jerarquía.
- **Eventos emitidos:** `UnidadTrasladada`.
- **Estado resultante:** la unidad conserva su estado, identidad, código e historial. La jerarquía registra una nueva versión vigente a partir de la fecha efectiva.

---

### Actualización de datos

#### Flujo 15 — Actualización de datos de una unidad o grupo

Escenario en el que el administrador modifica datos descriptivos o clasificatorios de una unidad o grupo sin cambiar su identidad ni su posición jerárquica. Aplica a cambios como corregir el nombre, ajustar el tipo de una unidad (de "centro de costo" a "proyecto" porque la naturaleza real es otra), o modificar la descripción. El código y el grupo padre **no** se modifican por este flujo — el código es inmutable por diseño (es el identificador único en el tenant) y el cambio de grupo padre se hace por F14 Traslado.

Siguiendo el patrón establecido del proyecto en Event Sourcing, los eventos `*Modificado` capturan un **delta**: identificadores estables + los campos efectivamente modificados (no snapshot completo del recurso). El estado actual se reconstruye reproduciendo el stream.

```
 Administrador selecciona una unidad o grupo
       │
       │  Edita uno o más campos modificables:
       │   · nombre
       │   · tipo (solo unidades)
       │   · descripción (opcional)
       │  Captura: motivo (opcional)
       ▼
 ┌────────────────────────────────────────────┐
 │ Sistema inteligente                        │
 │  Para cambios sensibles (ej: tipo de       │
 │  unidad) advierte el impacto previsto:     │
 │   · sub-dominios consumidores que          │
 │     reaccionan al tipo (Contabilidad       │
 │     puede tener reglas asociadas)          │
 │   · operaciones futuras que cambiarán      │
 │     de tratamiento                         │
 │  Para cambios livianos (nombre)            │
 │  procede sin advertencia.                  │
 └──────────────────────┬─────────────────────┘
                        │
                        │  Admin confirma
                        ▼
 ┌────────────────────────────────────────────┐
 │ Estructura Organizacional                  │
 │ Valida:                                    │
 │   · el nodo está en un estado modificable  │
 │     (no Inactiva, no Descartada — los      │
 │      datos del histórico no se modifican)  │
 │   · si cambia el tipo: el nuevo tipo está  │
 │     dentro del catálogo vigente            │
 └──────────────────────┬─────────────────────┘
                        │
                        ▼
 Emite (con delta — solo los campos
 efectivamente modificados):
   UnidadModificada  (si es una unidad)
   GrupoModificado   (si es un grupo)
                        │
                        ▼
 Consumidores con interés en los campos
 modificados (ej: Contabilidad al cambio de
 tipo) reciben el evento y actualizan su
 vista local o aplican sus propias reglas
 (ej: revalidación de plantillas contables).
```

- **Actor principal:** Administrador de estructura organizacional.
- **Pre-condiciones:** el nodo está en estado modificable según su tipo:
  - Para unidades organizacionales: `Borrador`, `Activa` o `Suspendida`. Las unidades en `Inactiva` o `Descartada` no admiten modificación.
  - Para grupos organizacionales: `Activo` o `Inactivo`. Los grupos inactivos sí admiten modificación de campos descriptivos, porque no reciben imputaciones y no alteran historial transaccional.

  Si cambia el tipo (solo unidades), el nuevo tipo está en el catálogo vigente.
- **Eventos emitidos:** `UnidadModificada` o `GrupoModificado` con delta (identificadores estables + campos modificados).
- **Estado resultante:** el nodo conserva su identidad, código, estado y posición jerárquica. Los campos descriptivos o clasificatorios reflejan los nuevos valores a partir del momento del evento.

---

## Sección 5: Integraciones

### Principio de responsabilidad

Estructura Organizacional es el **propietario** de la estructura jerárquica de grupos y unidades. Su responsabilidad termina en la gestión del ciclo de vida y la emisión de eventos. Los sub-dominios consumidores son **propietarios de su vista local** de unidades organizacionales — el sub-dominio no orquesta a los consumidores ni les impone reglas; solo notifica cambios mediante eventos.

En procesos de reestructuración, Estructura Organizacional emite los eventos que describen la fusión, división o traslado, pero no modifica directamente las transacciones ni las vistas internas de los consumidores. Cada sub-dominio consumidor es responsable de interpretar los eventos recibidos y aplicar, según sus propias reglas, la reasignación, bloqueo, reclasificación, advertencia o reconstrucción de sus vistas locales.

Los consumidores **operan contra su copia local** de unidades y **nunca consultan a Estructura Organizacional en el camino crítico** de sus operaciones (ver [`../../guias-de-modelado/datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md)). Estructura Organizacional no responde consultas en el flujo transaccional de los consumidores: publica eventos y ofrece un punto de resincronización de respaldo. La unidad se elige siempre de la fuente de verdad (la UI la lee en vivo), así que el consumidor solo difiere por desfase de propagación, sin señalar nada de vuelta (R29).

### Integraciones de entrada

Estructura Organizacional **no recibe integraciones de entrada por eventos**: es dueño y publicador de la estructura. Lo que ofrece es lectura — para administración, reportería (la UI compone de la fuente de verdad) y resincronización de las copias locales —, **nunca en el flujo transaccional de los consumidores**, que operan contra su copia local.

**Consultas mínimas esperadas** (para administración, reportería y resincronización — **no** para el flujo transaccional de los consumidores, que usan su copia local):

- Resolver unidad organizacional por código.
- Resolver grupo organizacional por código.
- Consultar estado, tipo, nombre y grupo padre de una unidad.
- Consultar jerarquía vigente.
- Consultar jerarquía a una fecha efectiva.
- Consultar descendientes de un grupo.
- Consultar impacto previsto de inactivación de grupo.
- Consultar unidades en `Borrador` pendientes de revisión.
- Consultar unidades por estado.
- Consultar historial de reestructuración de una unidad mediante el identificador del proceso correspondiente.

> **Catálogo de tipos de unidad.** Es **interno** al sub-dominio — no se consume desde Datos de Referencia. Razón: los tipos (centro de costo, proyecto, sucursal, inmueble, departamento, etc.) son conceptos del modelo organizacional, no datos de referencia universales como países o monedas. Cada empresa puede extender su catálogo de tipos según su modelo de negocio.
>
> **Motivos de reestructuración** (`operativa`, `fusion`, `division`). Son **literales del dominio** — no se gestionan como catálogo configurable. Cada motivo está atado a un flujo específico del modelo (F7, F12, F13) y agregar uno nuevo requiere diseñar un nuevo flujo de inactivación.

### Integraciones de salida

| Destino | Dato | Propósito |
|---------|------|-----------|
| **Sub-dominios consumidores** (OXP, Contabilidad, y futuros) | Eventos del ciclo de vida de unidades: `UnidadCreada`, `UnidadActivada`, `UnidadSuspendida`, `UnidadReactivada`, `UnidadReabierta`, `UnidadInactivada`, `UnidadDescartada`, `UnidadModificada` | Que cada consumidor mantenga su vista local consistente y reaccione según corresponda: bloquear nuevas imputaciones cuando una unidad se suspende o inactiva, destrabar operaciones pendientes cuando una unidad se activa, cancelar operaciones cuando una unidad se descarta, etc. |
| **Sub-dominios consumidores** | Eventos del ciclo de vida de grupos: `GrupoCreado`, `GrupoInactivado`, `GrupoReactivado`, `GrupoModificado` | Que los consumidores con interés en la estructura jerárquica (reportería consolidada, vistas agregadas) actualicen su vista local. |
| **Sub-dominios consumidores** | Eventos de reestructuración: `UnidadFusionada`, `UnidadDividida`, `UnidadTrasladada` | Que los consumidores reasignen sus referencias locales según corresponda, apliquen re-expresión comparativa en reportes (IFRS 8) y notifiquen a sus propios usuarios del cambio estructural. |
| **Sub-dominios consumidores** | Punto de resincronización (foto del estado actual / reproceso de eventos desde un punto) | Que un consumidor **repare su copia local** si se desfasó (reconciliación de respaldo, de fondo — ver [`../../guias-de-modelado/datos-entre-dominios.md`](../../guias-de-modelado/datos-entre-dominios.md)). No se usa en el flujo transaccional. |

> **Nota sobre el conteo de eventos:** el modelo de dominio define 18 eventos en total. De estos, 15 corresponden a eventos de ciclo de vida, reestructuración y jerarquía relevantes para los sub-dominios consumidores. Los 3 eventos restantes corresponden a la configuración interna del catálogo de tipos de unidad (`TipoUnidadAgregado`, `TipoUnidadModificado`, `TipoUnidadInactivado`) y su publicación externa dependerá de la definición contractual de EventCatalog en la fase correspondiente.

### Diagrama de integraciones

```
                    ┌──────────────────────────────────┐
                    │  Estructura Organizacional       │
                    │  (dueño de la estructura         │
                    │   jerárquica y su ciclo de vida) │
                    │                                  │
                    │  · Catálogo de tipos: interno    │
                    │  · Motivos de baja: literales    │
                    └────────────────┬─────────────────┘
                                     │
            ┌────────────────────────┴────────────────────────┐
            │                                                  │
      Eventos del ciclo de vida                         Punto de
      (creación, cambio, reestructuración)              resincronización
      ─────────►                                        ◄───────── (de fondo)
            │                                                  │
            ▼                                                  ▼
       ┌──────────────────────────────────────────────────────────┐
       │   Sub-dominios consumidores                              │
       │                                                          │
       │   F1: OXP, Contabilidad                                  │
       │   F2+: otros sub-dominios que se incorporen              │
       │                                                          │
       │   Cada uno:                                              │
       │    · OPERA y VALIDA contra su copia local de unidades    │
       │    · reacciona a eventos según su contexto               │
       │    · difiere por desfase de propagación (no aproxima)    │
       │    · NO consulta a Estructura Org. en el camino          │
       │      crítico de operar (la UI sí lee la fuente en vivo)  │
       └──────────────────────────────────────────────────────────┘
```

### Notas de la primera fase

- En F1 los consumidores son **OXP** y **Contabilidad** únicamente. Ambos operan contra su copia local de unidades; ante un desfase de propagación (el evento de una unidad aún no llegó a la copia), difieren la parte que la requiere sin detener su operación (R29).
- La creación de unidades es siempre **acto deliberado del administrador** en F1 (o por integración con sistemas de origen, ej: presupuesto). En fases posteriores el sistema inteligente puede crear unidades automáticamente cuando el contexto sea inequívoco.
- En F1 solo se expone la dimensión **Unidad Organizacional** como dimensión de imputación. El contrato de líneas de traducción con Contabilidad acepta solo esa dimensión.
- Las **direcciones** de las unidades (por ejemplo de sucursales o inmuebles) **no son responsabilidad** de Estructura Organizacional en F1. Si un sub-dominio consumidor requiere asociar una dirección a una unidad, lo gestiona internamente con el servicio de Direcciones que corresponda.

### Visión a futuro

- **F2+ — Multi-dimensionalidad.** Cuando se incorporen otras dimensiones (Proyecto, Sucursal, Línea de Negocio, etc.), el contrato de líneas de traducción se extiende con campos opcionales para cada dimensión adicional. Estructura Organizacional sigue siendo el owner de la dimensión "Unidad Organizacional"; las demás dimensiones tendrán sus propios sub-dominios owners. Ver `anexo-decisiones-arquitectonicas.md`, Decisión 4.
- **F2+ — Creación automática de unidades.** Cuando el contexto es inequívoco (ej: proyecto aprobado en el sistema de presupuesto con todos los datos), el sistema inteligente puede crear la unidad sin intervención humana, a partir de la integración con el sistema de origen.
- **F2+ — Incorporación de nuevos consumidores.** Cualquier sub-dominio que se incorpore al ERP y necesite imputar transacciones a unidades organizacionales se conecta al mismo esquema de eventos. No requiere cambios en Estructura Organizacional.

---

## Sección 6: Reglas de negocio

Las reglas se numeran `[R##]` y se organizan por tema operativo.

### Reglas de estructura jerárquica

| # | Regla | Descripción | Configurable |
|---|-------|-------------|:------------:|
| R1 | **Pertenencia obligatoria a grupo padre** | Toda unidad organizacional pertenece a exactamente un grupo padre. No existen unidades huérfanas. | No |
| R4 | **No ciclos en la jerarquía** | Un grupo no puede ser su propio ancestro directo ni indirecto. El árbol mantiene su naturaleza acíclica. | No |
| R5 | **Mezcla libre de hijos en grupos** | Un grupo puede contener cualquier combinación de sub-grupos y unidades organizacionales como hijos. No se restringe la mezcla. | No |
| R6 | **Unidad siempre hoja** | Una unidad organizacional nunca tiene hijos. Si un caso de negocio requiere estructura adicional bajo lo que parece una unidad, se modela con un grupo intermedio. | No |
| R7 | **Padre debe estar activo** | Al crear o trasladar una unidad o sub-grupo, el grupo padre debe estar en estado `Activo`. No se admiten hijos colgando de padres inactivos. | No |

### Reglas de códigos e identidad

| # | Regla | Descripción | Configurable |
|---|-------|-------------|:------------:|
| R8 | **Código único por tenant** | El código de una unidad o grupo es único dentro del tenant, considerando unidades y grupos en el mismo espacio de nombres. | No |
| R9 | **Código inmutable** | Una vez asignado, el código de una unidad o grupo no se modifica en ningún flujo. Es el ancla estable de toda la trazabilidad histórica. | No |
| R10 | **Formato y longitud del código** | El código es **texto libre con salvaguardas**: no vacío, sin espacios al inicio/fin ni espacios internos, solo caracteres imprimibles (sin saltos de línea ni caracteres de control); separadores como guion, punto o barra permitidos pero cosméticos (no embeben jerarquía). La unicidad no distingue mayúsculas de minúsculas (`cc-001` = `CC-001`). La longitud es un rango `min`/`max` **configurable por tenant** dentro de la envolvente del dominio (`1 ≤ min ≤ max ≤ 50`); el rango por defecto recomendado es **2 a 12**; longitud fija = `min == max`. Esta configurabilidad es la que permite recibir estructuras legadas con su codificación intacta, sin violar la inmutabilidad del código (R9, P3). | Sí (por tenant) |
| R11 | **Identificación libre tras `Descartada`** | Cuando una unidad pasa a `Descartada`, su código queda disponible para una nueva solicitud. Una unidad en `Inactiva` o `Suspendida` mantiene su código reservado. | No |

### Reglas del ciclo de vida de unidades

| # | Regla | Descripción | Configurable |
|---|-------|-------------|:------------:|
| R12 | **Transiciones permitidas** | Las transiciones válidas de la FSM de unidad son: `Borrador → Activa`, `Borrador → Descartada`, `Activa → Suspendida`, `Suspendida → Activa`, `Activa → Inactiva`, `Suspendida → Inactiva`, `Inactiva → Activa` (reapertura). Cualquier otra transición es rechazada por el dominio. | No |
| R13 | **Imputaciones solo contra `Activa`** | Las nuevas transacciones de los consumidores solo se aceptan contra unidades en estado `Activa`. Una unidad en `Borrador`, `Suspendida`, `Inactiva` o `Descartada` no admite nuevas imputaciones. **El consumidor valida esta condición contra su copia local de unidades** (mantenida por los eventos de Estructura Organizacional), no consultando a Estructura Organizacional en el momento de imputar. | No |
| R14 | **`Descartada` terminal estricto** | Una unidad en `Descartada` no puede transicionar a ningún otro estado. Para volver a operar, se crea una unidad nueva con datos limpios. | No |
| R15 | **`Inactiva` reabrible** | Una unidad en `Inactiva` puede reabrirse mediante F6 emitiendo `UnidadReabierta`. La unidad conserva su identidad, código e historial; las nuevas imputaciones se enlazan con la operación previa. | No |
| R16 | **Reapertura requiere padre activo** | Reabrir una unidad inactiva requiere que su grupo padre esté en estado `Activo`. Si el padre fue inactivado por cascada, primero se reactiva el grupo padre. | No |
| R17 | **Modificación bloqueada en unidades dadas de baja** | Las unidades en `Inactiva` o `Descartada` no admiten modificaciones de datos (F15). Si se requiere corregir datos de una unidad inactiva, el flujo natural es reabrir (F6) → modificar (F15) → inactivar (F7). Esta restricción aplica solo a unidades; los grupos en estado `Inactivo` sí admiten modificación de campos descriptivos porque no participan en historial transaccional. | No |

### Reglas del ciclo de vida de grupos

| # | Regla | Descripción | Configurable |
|---|-------|-------------|:------------:|
| R18 | **Grupos con dos estados** | La FSM de grupo tiene únicamente dos estados (`Activo`, `Inactivo`). Los grupos no requieren `Borrador` ni `Suspendido` porque no reciben imputaciones. | No |
| R19 | **Inactivación de grupo en cascada** | Inactivar un grupo propaga la inactivación recursivamente a todos sus descendientes: sub-grupos pasan a `Inactivo`, unidades `Activa`/`Suspendida` pasan a `Inactiva`, unidades `Borrador` pasan a `Descartada`. Cada nodo afectado emite su propio evento de dominio. | No |
| R20 | **Reactivación de grupo sin cascada inversa** | Reactivar un grupo solo cambia el estado del grupo. Los hijos previamente afectados por la cascada deben reabrirse o reactivarse uno a uno, con apoyo del sistema inteligente que identifica los candidatos. | No |
| R21 | **Confirmación de impacto obligatoria** | La inactivación de un grupo requiere confirmación explícita del administrador después de que el sistema inteligente muestre el impacto previsto (cantidad de sub-grupos, unidades activas, suspendidas y en borrador que se verán afectadas). | No |

### Reglas de reestructuración

| # | Regla | Descripción | Configurable |
|---|-------|-------------|:------------:|
| R22 | **Destino debe existir y estar `Activa`** | En F12 (Fusión) y F13 (División), las unidades destino deben existir y estar en estado `Activa` antes de iniciar el proceso. La creación de la unidad destino y la reestructuración son flujos separados. | No |
| R23 | **Origen en `Activa` o `Suspendida`** | En F12 y F13, las unidades origen deben estar en `Activa` o `Suspendida`. Las unidades en `Borrador`, `Inactiva` o `Descartada` no se reestructuran. | No |
| R24 | **Destino distinto del conjunto origen** | En F12, la unidad destino no puede estar dentro del conjunto de unidades origen. En F13, la unidad origen no puede estar dentro del conjunto de unidades destino. | No |
| R25 | **Fecha efectiva de la reestructuración** | La fecha efectiva de una fusión, división o traslado (F12, F13, F14) se gobierna en dos planos: **(a) validación del sistema** — no puede ser anterior a la versión vigente de jerarquía de las unidades involucradas; Estructura Organizacional lo valida localmente por ser dueña de la jerarquía. **(b) responsabilidad del administrador** — la coherencia de la fecha con la actividad transaccional de las unidades (no fijarla sobre periodos que ya tienen movimientos) la **define y la responde el administrador** que ejecuta la reestructuración, como acto deliberado de gestión. El sistema no la verifica contra el historial transaccional porque las transacciones/asientos son inmutables y los reportes ofrecen vista actual e histórica. | No |
| R26 | **Motivo de baja proyectado** | Las unidades que pasan a `Inactiva` o `Descartada` llevan en el modelo de lectura un atributo `motivoBaja` que permite identificar la causa de la baja. Para unidades `Inactiva`, los valores son `operativa`, `fusion` o `division`, según el flujo que originó la inactivación. Para unidades `Descartada`, los valores son `operativa` cuando corresponde a rechazo manual del administrador (Flujo 8), o `cascada_grupo` cuando corresponde al descarte por cascada de inactivación del grupo padre (Flujo 10). Los valores son literales fijos del dominio. | No |
| R27 | **Historial referenciado al origen** | En F12 y F13, el historial transaccional previo a la fecha efectiva permanece referenciado a las unidades origen. Las unidades destino arrancan limpias y solo reciben las nuevas imputaciones desde la fecha efectiva. | No |
| R28 | **Traslado preserva identidad** | El traslado de una unidad (F14) conserva su identidad, código, estado e historial transaccional. Solo cambia su posición en el árbol y la versión vigente de jerarquía. | No |

### Reglas de operación del consumidor

| # | Regla | Descripción | Configurable |
|---|-------|-------------|:------------:|
| R29 | **Operación del consumidor sin bloqueo** | El consumidor opera contra su copia local de unidades. Una unidad solo se referencia tras existir en Estructura Organizacional (la UI la elige de la fuente de verdad; las reglas de distribución del consumidor se parametrizan contra ella), de modo que la única demora posible es de propagación: si el evento de ciclo de vida aún no llegó a su copia local, su operación **no se detiene** — registra lo que puede y difiere solo la parte que requiere la unidad, que se resuelve cuando el evento llega (consistencia eventual). La creación de una unidad es siempre **acto deliberado de Estructura Organizacional** (administrador o integración) — ningún flujo consumidor crea unidades ni las origina en un estado bloqueante. | No |
| R31 | **Estructura en bosque y consolidación por tenant** | La estructura organizacional es un bosque: los grupos sin padre son los **grupos de primer nivel** y un tenant puede tener varios (estructura financiera, gerencial, u otras que el negocio necesite como árboles separados). Ningún grupo se crea automáticamente al inicializar el tenant. La consolidación "total compañía" (balances, estados de resultados, estados financieros) es responsabilidad de la **frontera del tenant** — totaliza todas las unidades del tenant — y no requiere un nodo único que las contenga; cada grupo de primer nivel consolida su propio sub-árbol, como los centros de costo maestros del ERP actual consolidan sus auxiliares. | No |

---

## Sección 7: Qué está dentro y fuera del alcance

### Dentro del alcance

Estructura Organizacional asume las siguientes responsabilidades funcionales:

- **Gestión del ciclo de vida de grupos organizacionales** (2 estados: `Activo`, `Inactivo`), incluyendo la inactivación en cascada a sus descendientes y la reactivación sin cascada inversa.
- **Gestión del ciclo de vida de unidades organizacionales** (5 estados: `Borrador`, `Activa`, `Suspendida`, `Inactiva`, `Descartada`), con todas sus transiciones permitidas.
- **Estructura jerárquica versionada por fecha efectiva** — el árbol de grupos y unidades, con relaciones padre-hijo, mantiene historia estructural para reportería comparativa.
- **Codificación de unidades y grupos** — códigos planos de texto libre, únicos por tenant, inmutables.
- **Catálogo interno de tipos de unidad** — clasificación de la unidad según su naturaleza (centro de costo, proyecto, sucursal, inmueble, departamento, etc.), extensible por cada empresa según su modelo de negocio.
- **Procesos de reestructuración**: fusión, división y traslado, con fecha efectiva, motivo y proyección del motivo de baja (`motivoBaja`) en el modelo de lectura.
- **Emisión de eventos** — todo cambio de ciclo de vida, jerarquía o reestructuración emite un evento de dominio que los consumidores reciben para mantener su vista local consistente.
- **Consultas de unidades y jerarquía** — los consumidores pueden resolver una unidad por código o navegar la jerarquía para validar antes de imputar transacciones o construir reportes.
- **Control de autorización funcional** — las operaciones principales del sub-dominio se protegen mediante permisos atómicos por acción y recurso, definidos en el modelo de dominio. Esto permite diferenciar permisos para crear, modificar, inactivar, reactivar, trasladar, fusionar, dividir, consultar y administrar tipos de unidad.

### Fuera del alcance

Estructura Organizacional **no** asume las siguientes responsabilidades — pertenecen a otros sub-dominios o servicios transversales del ERP:

| Responsabilidad | A quién pertenece |
|-----------------|-------------------|
| **Direcciones físicas de las unidades** (dirección de una sucursal, dirección de un inmueble) | Cada sub-dominio consumidor las gestiona si las requiere (ej: ABR para inmuebles). El servicio compartido de Direcciones las persiste. Estructura Organizacional no tiene atributo `dirección` en la unidad. |
| **Reglas tributarias asociadas al tipo de unidad** | Sub-dominio de Impuestos. Si un tipo de unidad implica un régimen tributario particular, la regla vive en Impuestos, no en Estructura Organizacional. |
| **Reglas contables de derivación** (plantillas de asiento, mapeo de cuentas por unidad) | Sub-dominio de Contabilidad. La unidad organizacional llega como dimensión de imputación; Contabilidad aplica sus reglas. |
| **Presupuesto y planeación por unidad** | Sub-dominio futuro de Presupuesto / Planeación. Estructura Organizacional no almacena montos planeados ni ejecuciones. |
| **Gestión de empleados asociados a unidades** | Sub-dominio futuro de Nómina o Recursos Humanos. La pertenencia de un empleado a una unidad vive en el sub-dominio que gestiona empleados, no acá. |
| **Reportería consolidada y dashboards** | Capa de reportería transversal del ERP o herramientas BI externas. Estructura Organizacional expone consultas básicas; la reportería avanzada (agregaciones multi-dimensión, comparativos históricos, exportes regulatorios) no es responsabilidad suya. |
| **Dimensiones de imputación distintas a "Unidad Organizacional"** | En F1 solo se expone esa dimensión. Otras dimensiones (Proyecto, Sucursal como entidad separada, Línea de Negocio, Tipo de Obra, etc.) se incorporan en fases posteriores con sus propios sub-dominios owners. |
| **Auditoría externa de cambios** (cumplimiento SOX, logs regulatorios externos) | Infraestructura de auditoría transversal del ERP. Estructura Organizacional emite los eventos; la persistencia auditable de largo plazo no es de su responsabilidad. |

### Dependencias externas

| Dependencia | Descripción | Impacto en Estructura Organizacional |
|-------------|-------------|--------------------------------------|
| **Sub-dominios consumidores** (OXP, Contabilidad en F1; otros que se incorporen en fases posteriores) | Sub-dominios que consumen unidades organizacionales para imputar transacciones y reaccionan a los eventos del ciclo de vida y de reestructuración. | Operan y validan contra su copia local de unidades; son notificados de los cambios para mantener consistencia. La existencia operativa de Estructura Organizacional cobra valor solo cuando hay al menos un consumidor (OXP o Contabilidad en F1). |

---

## Sección 8: Estrategia de implementación por fases

### Fase 1 — Sub-dominio completo

Estructura Organizacional se entrega como un sub-dominio funcional completo en una sola fase. Cubre todas las capacidades descritas en este documento.

> **Nota de implementación:** aunque Fase 1 define el alcance funcional completo del sub-dominio, la entrega técnica puede organizarse en **hitos internos** de construcción, validación y estabilización que **no constituyen sub-fases del producto ni alteran el alcance comprometido**. Esta organización permite gestionar riesgos de implementación asociados a sagas, proyecciones, eventos, consultas, permisos e integración con consumidores.

| # | Capacidad | Descripción |
|---|-----------|-------------|
| 1 | Gestión del ciclo de vida de grupos | Creación, inactivación con cascada a descendientes y reactivación. FSM de 2 estados (`Activo`, `Inactivo`). |
| 2 | Gestión del ciclo de vida de unidades | Creación (acto deliberado del administrador), activación, suspensión, reactivación, reapertura, inactivación y descarte. FSM de 5 estados (`Borrador`, `Activa`, `Suspendida`, `Inactiva`, `Descartada`). |
| 3 | Estructura jerárquica versionada | Árbol de grupos y unidades con relaciones padre-hijo y vigencia por fecha efectiva. Habilita la comparabilidad histórica de reportes (IFRS 8). |
| 4 | Codificación de unidades y grupos | Códigos alfanuméricos planos, únicos por tenant, inmutables. Longitud parametrizable entre 4 y 12 caracteres. |
| 5 | Catálogo interno de tipos de unidad | Lista de clasificaciones (centro de costo, proyecto, sucursal, inmueble, departamento, etc.) extensible por cada empresa según su modelo de negocio. |
| 6 | Procesos de reestructuración | Fusión (F12), División (F13) y Traslado (F14) aplicables a unidades, con fecha efectiva, motivo y motivo de baja proyectado en el modelo de lectura. |
| 7 | Notificación de cambios a los consumidores | Cada cambio de ciclo de vida, jerarquía o reestructuración se notifica mediante un evento que los consumidores reciben para mantener su vista al día. El modelo completo define 18 eventos; 15 son relevantes para los consumidores en F1 y 3 corresponden a la configuración interna del catálogo de tipos de unidad. |
| 8 | Consultas de unidades y jerarquía | Resolución por código y navegación de la jerarquía para validación antes de imputar transacciones o construir reportes. |
| 9 | Dimensión "Unidad Organizacional" como dimensión de imputación | Única dimensión expuesta en el contrato de líneas de traducción con Contabilidad. El modelo queda preparado para extender el contrato con dimensiones adicionales sin rediseño. |

### Consumidores en F1

| Consumidor | Rol |
|------------|-----|
| **OXP** | Imputa obligaciones a unidades organizacionales (valida y opera contra su copia local); ante un desfase de propagación (el evento de una unidad aún no llegó a la copia), no se detiene y difiere la parte que la requiere. |
| **Contabilidad** | Consume la unidad organizacional como dimensión de imputación en las líneas de traducción (contra su copia local). Reacciona a eventos de reestructuración para reclasificación contable. |

### Fases futuras

Las capacidades adicionales que el sub-dominio pueda soportar en el futuro (activación automática con sistema inteligente, multi-dimensionalidad operativa con otras dimensiones ortogonales, reestructuración a nivel de sub-grupo, etc.) se evaluarán según las necesidades del negocio en su momento. El modelo de F1 está diseñado preparado para extenderse sin rediseño estructural (ver `anexo-decisiones-arquitectonicas.md`, Decisión 4).

---

## Sección 9: Beneficios esperados

| # | Beneficio | Problema que resuelve |
|---|-----------|----------------------|
| 1 | **Comunicación controlada con los consumidores:** Cada cambio relevante en una unidad o grupo se notifica a los sub-dominios consumidores mediante un evento de dominio. Los consumidores actualizan su vista local automáticamente y dejan de imputar transacciones contra unidades inactivas o suspendidas. | Sin comunicación controlada (Problema 1) |
| 2 | **Estructura sin techo combinatorio:** La codificación plana + jerarquía versionada permite modelar empresas con estructuras robustas (múltiples sucursales en varios países, proyectos con sub-proyectos anidados, matrices con varios ejes) sin las restricciones del código posicional tradicional. | Jerarquía limitada por código alfanumérico (Problema 2) |
| 3 | **Nomenclatura unificada en todo el ERP:** "Estructura Organizacional" describe la responsabilidad completa del sub-dominio (estructura, jerarquía, tipos, reestructuración). Los demás sub-dominios consumidores adoptan "unidad organizacional" como término único, eliminando la confusión que generaba "centro de costo" en módulos no contables. | Nomenclatura limitante (Problema 3) |
| 4 | **Tipos de unidad explícitos:** El catálogo interno de tipos (centro de costo, proyecto, sucursal, inmueble, departamento, etc.) permite a cada sub-dominio consumidor interpretar la unidad según su contexto. Cada empresa puede extender el catálogo con sus propios tipos. | Sin tipos de unidad (Problema 4) |
| 5 | **Operación sin bloqueo con gobierno preservado:** los consumidores nunca se detienen por una unidad faltante (operan contra su copia local y difieren lo que falta); la creación de unidades sigue siendo acto deliberado de Estructura Organizacional, con su validación de unicidad y jerarquía — sin proliferación dispersa de unidades ad-hoc. | Creación dispersa (Problema 5) |
| 6 | **Reestructuración como procesos formales:** Fusión, división y traslado se ejecutan como eventos de dominio con fecha efectiva, motivo y trazabilidad histórica. Reemplazan los renombres y reasignaciones manuales que rompían la comparabilidad año-contra-año. | Sin concepto de reestructuración (Problema 6) |
| 7 | **Estados que reflejan la realidad operativa:** Los cinco estados de unidad (`Borrador`, `Activa`, `Suspendida`, `Inactiva`, `Descartada`) modelan explícitamente los momentos transitorios del negocio (preparación, operación, pausa, cierre, descarte) que hoy se enmascaran detrás de "activo/inactivo" y generan transacciones incorrectas. | *Modelado fiel a la realidad operativa* |
| 8 | **Cumplimiento IFRS 8 sin reconstrucción manual:** La historia estructural versionada y los eventos formales de reestructuración permiten cumplir con la re-expresión comparativa de periodos anteriores (IFRS 8 §29-30) sin que el equipo financiero deba reconstruir manualmente la estructura previa en cada auditoría. | *Comparabilidad histórica* |
| 9 | **Reapertura sin perder continuidad:** Una unidad inactivada por error o reactivada por un cambio de decisión del negocio (sucursal que reabre, proyecto que se reanuda) puede reabrirse conservando su identidad, código e historial. El evento `UnidadReabierta` queda como traza auditable diferenciada de la reactivación de pausas transitorias. | *Flexibilidad operativa sin sacrificar auditoría* |
| 10 | **Base preparada para crecer:** El modelo está diseñado para soportar nuevas dimensiones de imputación (Proyecto, Sucursal, Línea de Negocio) en fases posteriores sin rediseño estructural. Cualquier nuevo sub-dominio del ERP que requiera imputar a unidades organizacionales se incorpora al mismo esquema de eventos sin tocar Estructura Organizacional. | *Extensibilidad estratégica* |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.1 | 2026-04-24 | Borrador inicial — Sección 1 (Definición, Contexto actual, Problema, Implementación inicial, Nomenclatura). Resto de secciones pendientes. |
| 0.2 | 2026-04-24 | Sección 2 (Glosario de términos) cerrada con 21 términos. Cubre la estructura (grupo, unidad, tipo, código, nombre, jerarquía, nivel), el ciclo de vida (4 estados), la reestructuración (fusión, división, traslado, fecha efectiva), la multi-dimensionalidad (dimensión de imputación, unidad de imputación) y los consumidores. Alineado con `anexo-decisiones-arquitectonicas.md` v1.0. |
| 0.3 | 2026-04-24 | Sección 3 (Actores del sistema) cerrada. Diseño minimalista con 2 actores internos (administrador de estructura organizacional, usuario consumidor) — se evita la sobre-especificación de roles porque el sistema es inteligente y guía al usuario según el contexto. Actores externos: OXP y Contabilidad en F1, sub-dominios consumidores futuros en fases posteriores. Patrón de creación desde consumidores documentado en `anexo-orquestacion-creacion.md` v1.0 (BFF + estado `Borrador` como vehículo de consistencia eventual). |
| 0.4 | 2026-05-26 | Glosario actualizado a 23 términos (alineado con `anexo-decisiones-arquitectonicas.md` v1.1). Cambios: (1) Grupo organizacional reescrito — admite mezcla de sub-grupos y unidades como hijos, ciclo de vida con 2 estados, cascada de inactivación; (2) Unidad organizacional reescrita — siempre hoja, pertenece a exactamente un grupo padre; (3) nuevo término **Grupo raíz** (único por tenant); (4) Estado de la unidad ampliado a 5 estados; (5) nuevo término **Descartada** (terminal, nunca operó). Sección 4 (Flujo principal) sigue con solo la Familia 1 escrita; Familia 2 pendiente de redacción con la nueva separación grupo/unidad. |
| 0.5 | 2026-05-27 | Sección 4 — Familia 2 "Gestión del ciclo de vida" cerrada con 9 flujos. Sub-sección **unidades**: F3 Activación, F4 Suspensión, F5 Reactivación (desde `Suspendida`), F6 Reapertura (desde `Inactiva` — nueva), F7 Inactivación, F8 Descarte. Sub-sección **grupos**: F9 Creación, F10 Inactivación con cascada (1 evento por nodo afectado), F11 Reactivación sin cascada inversa (el sistema inteligente identifica candidatos a reabrir uno a uno). Cambio semántico de fondo: **`Inactiva` deja de ser terminal estricto** para unidades — admite reapertura mediante `UnidadReabierta` (evento diferenciado de `UnidadReactivada` para auditoría); `Descartada` permanece como único terminal estricto. Glosario actualizado (términos 10 y 14). Alineado con `anexo-decisiones-arquitectonicas.md` v1.2. |
| 0.6 | 2026-05-27 | Sección 4 — Familia 3 "Reestructuración" cerrada con 3 flujos (aplican solo a unidades): F12 Fusión (N origen → 1 destino), F13 División (1 origen → N destinos), F14 Traslado (cambio de grupo padre). Premisas comunes: la unidad destino debe existir y estar `Activa` antes de iniciar (creación y reestructuración separadas); las unidades origen quedan en `Inactiva` con la causa registrada en el evento y proyectada como atributo `motivoBaja` en el modelo de lectura (sin inflar la FSM con estados nuevos por motivo de baja — patrón canónico DDD/ES/CQRS); el historial transaccional previo a la fecha efectiva permanece referenciado a las unidades origen (sin distribución — opción más fiel a IFRS 8). Aplica solo a unidades; los sub-grupos no se reestructuran en F1. |
| 0.7 | 2026-05-27 | **Sección 4 — Flujo principal completamente cerrada con 15 flujos en 4 familias.** Familia 4 "Actualización de datos" agregada con F15 (Actualización de datos de una unidad o grupo). Premisas: flujo unificado para cualquier cambio de campo modificable (nombre, tipo en unidades, descripción) alineado con el patrón Event Sourcing de delta; eventos diferenciados `UnidadModificada` / `GrupoModificado`; el código es inmutable; el cambio de padre se hace por F14; las unidades `Inactiva` y `Descartada` no se modifican (el histórico se conserva intacto). Próximas secciones pendientes: 5 (Integraciones), 6 (Reglas de negocio), 7 (Dentro/fuera del alcance), 8 (Fases), 9 (Beneficios). |
| 0.8 | 2026-05-27 | **Sección 5 — Integraciones cerrada.** Principio de responsabilidad (Estructura Organizacional es owner del dato; los consumidores son owners de su vista local — patrón EDA puro). Integraciones de entrada: solicitudes de creación desde consumidores (vía BFF) y consultas. Integraciones de salida: 15 eventos del modelo distribuidos a consumidores. Diagrama incluido. Premisas confirmadas: (a) catálogo de tipos de unidad es **interno** al sub-dominio (no en Datos de Referencia); (b) motivos de reestructuración (`operativa`, `fusion`, `division`) son **literales del dominio** (no catálogo configurable); (c) las direcciones de unidades no son responsabilidad de Estructura Organizacional en F1. Visión a futuro: multi-dimensionalidad (F2+), activación automática por sistema inteligente (F2+), incorporación transparente de nuevos consumidores. |
| 0.9 | 2026-05-27 | **Sección 6 — Reglas de negocio cerrada con 30 reglas en 6 temas.** Estructura jerárquica (R1-R7), códigos e identidad (R8-R11), ciclo de vida de unidades (R12-R17), ciclo de vida de grupos (R18-R21), reestructuración (R22-R28), solicitudes desde consumidores (R29-R30). Solo R10 (formato del código) es configurable por tenant dentro del rango 4-12 caracteres alfanuméricos; las demás reglas son fijas del dominio. Los comportamientos del sistema inteligente no se documentan como reglas duras (son comportamientos de producto); las responsabilidades de notificación a consumidores no se duplican aquí (viven en Sección 5). |
| 0.10 | 2026-05-27 | **Sección 7 — Qué está dentro y fuera del alcance cerrada.** Dentro: 9 responsabilidades funcionales (ciclo de vida de grupos y unidades, jerarquía versionada, codificación, catálogo interno de tipos, reestructuración, solicitudes desde consumidores, emisión EDA, consultas). Fuera: 8 responsabilidades que pertenecen a otros sub-dominios o servicios transversales (direcciones, reglas tributarias, reglas contables, presupuesto, empleados, reportería consolidada, dimensiones adicionales, auditoría externa). Dependencias externas: única dependencia funcional son los sub-dominios consumidores (OXP, Contabilidad en F1). Se omite "Visión arquitectónica" — alineado con Terceros, Contabilidad e Impuestos; el contenido equivalente ya queda cubierto por el diagrama de la Sección 5. BFF y sistema inteligente quedan documentados solo en los anexos (infraestructura transversal, no dependencias funcionales). |
| 0.11 | 2026-05-27 | **Sección 8 — Estrategia de implementación por fases cerrada.** Una sola fase F1 plana con 10 capacidades del sub-dominio completo (ciclo de vida grupos+unidades, jerarquía versionada, codificación, catálogo de tipos, reestructuración, solicitudes desde consumidores, emisión EDA, consultas, dimensión Unidad Organizacional). Consumidores F1: OXP y Contabilidad. Sin sub-división núcleo/habilitadores porque la única dependencia funcional son los consumidores. Capacidades adicionales (activación automática, multi-dimensionalidad operativa, reestructuración de sub-grupos) quedan como nota corta "fases futuras" sin compromiso de tiempo — se evaluarán según necesidades del negocio. |
| **1.0** | **2026-05-27** | **Sección 9 — Beneficios esperados cerrada. Alcance completo, listo para Fase 2 (modelo de dominio).** 10 beneficios en formato tabla `# / Beneficio / Problema que resuelve` alineado con Terceros (los primeros 6 enlazan 1:1 con los 6 problemas originales de Sección 1; los 4 últimos son beneficios transversales derivados de las decisiones arquitectónicas: estados explícitos, IFRS 8, reapertura, multi-dimensionalidad preparatoria). Sin indicadores de éxito / KPIs — alineado con todos los demás sub-dominios del proyecto (no aplican naturalmente a sub-dominios de definición/configuración). |
| 1.1 | 2026-05-28 | Ajuste editorial menor por auditoría Bloque Baja (B7) del modelo de dominio: glosario término 22 "Unidad de imputación" — agregada nota explícita de que en los documentos de dominio de Estructura Organizacional se prefiere el término "unidad organizacional" y el sinónimo solo se registra para referencia cruzada con sub-dominios consumidores. |
| 1.2 | 2026-05-28 | **Aplicados 11 ajustes del comité de producto (A1-A11) + consecuencia de D4.** **A1:** Sección 1 — sustituido "estructura jerárquica de dos niveles" por "dos tipos de nodo" con aclaración de jerarquía multinivel mediante grupos anidados. **A2:** Sección 5 (nota de conteo) y Sección 8 capacidad 8 — distinguido entre 18 eventos totales y 15 relevantes para consumidores transaccionales en F1. **A3:** Flujo 15 — precondiciones diferenciadas por tipo de nodo (grupos `Inactivo` sí admiten modificación); R17 mantenida solo para unidades. **A4:** R26 ampliada para cubrir unidades `Descartada` y motivo `abandono_por_inactividad`. **A5:** R3 "grupo raíz protegido" + Flujo 10 alineado ("raíz con contenido"). **A6:** glosario término División — eliminada la idea de distribución del historial. **A7:** Sección 5 — agregada precisión sobre responsabilidad de consumidores en reestructuración (reasignación/reclasificación es suya, no de Estructura Organizacional). **A8:** Sección 5 — lista de 10 consultas mínimas esperadas (incluye consultar historial de reestructuración mediante el identificador del proceso correspondiente — lenguaje funcional). **A9:** Sección 7 dentro del alcance — agregada responsabilidad "Control de autorización funcional" (permisos atómicos). **A10:** Sección 4 — nota sobre el rol del sistema inteligente como asistencia de producto, no como reglas duras del dominio. **A11:** Sección 8 — nota de implementación con hitos internos (que **no constituyen sub-fases del producto ni alteran el alcance comprometido**). **Consecuencia de D4:** Sección 7 Dependencias externas — agregada segunda relación con sub-dominios consumidores transaccionales como fuente consultable de la última imputación por unidad, requerida para validar la `fechaEfectiva` en reestructuraciones (R25/I08). |
| 1.3 | 2026-06-19 | **Replanteamiento — eliminación de acoplamientos de ejecución y proceso con los consumidores (issue #45/#46).** La creación de unidades desde consumidores deja de ser un flujo bloqueante: el consumidor opera contra su **copia local** y nunca consulta a Estructura Organizacional en el camino crítico; cuando necesita una unidad inexistente, su operación **no se detiene** (difiere lo que la requiere) y la necesidad se hace visible como **sugerencia no bloqueante**. Flujo 2 reescrito; R29 (era 'solicitudes entran en Borrador') y R30 (era 'descarte cancela en cascada') reescritas como 'operación sin bloqueo' y 'demanda no bloqueante'; R13 aclarada (validación contra copia local); glosario de `Borrador` acotado a preparación del administrador; Sección 5 (principio, integraciones, diagrama) alineada — entra el punto de resincronización y los eventos de imputación, salen la solicitud bloqueante y la consulta en caliente del flujo transaccional; Sección 7 (H2) — la última imputación pasa de consulta federada a **proyección local por eventos**. Fundamento en `guias-de-modelado/datos-entre-dominios.md`. Acompaña al modelo de dominio (Hito 2). |
| 1.4 | 2026-06-19 | **Consistencia del modelo de comunicación — replanteamiento de R25 (issue #56).** `R25` **se replantea (no se quita)**: la fecha efectiva se gobierna en dos planos — EO valida localmente que no sea anterior a la versión vigente de jerarquía, y la coherencia con la actividad transaccional es **responsabilidad del administrador** (acto deliberado; transacciones inmutables + vista actual/histórica). Se **retira la integración entrante de eventos de imputación** y la dependencia de la Sección 7 correspondiente (EO ya no mantiene proyección de última imputación, `[SI10]` retirada en el modelo); el **diagrama de integraciones pasa de 4 a 3 flujos**; precondiciones de F12/F13 ajustadas al replanteamiento. Acompaña al modelo v1.6 (`[SI10]` retirada, `[I08]` reformulada). |
| 1.5 | 2026-06-23 | **Retiro del aparato de señal/bandeja — la copia local es para validación, no para la UI (issue #72/#73).** Una vez la asignación/distribución de la unidad en los consumidores se hace contra la fuente de verdad (la UI lee EO en vivo; las reglas se parametrizan contra ella), referenciar una unidad inexistente no ocurre en el camino operativo. En consecuencia: **`R30` (demanda de unidad no bloqueante) retirada** → 30 → 29 reglas; **`R29` reformulada** (operación sin bloqueo + diferir por **consistencia eventual**: la unidad existe en el dueño, solo puede faltar la propagación del evento); **Flujo 2 reescrito** (sin el camino de señal/visibilidad); **diagrama de integraciones pasa de 3 a 2 flujos** (eventos salientes + resincronización); responsabilidades de actores, principio de Sección 5, capacidades (se retira "Atención de la demanda…") y notas de fase limpiadas de la señal/sugerencia. La creación de unidades sigue su curso normal por planeación del administrador. Acompaña al modelo v1.7 (retiro de `[SI11]`, parte 4 de `[D15]`, reorientación de `[SI07]`, §3.8 con el principio de capas) y a la guía `datos-entre-dominios.md`. |
| 1.6 | 2026-07-08 | **Flujo 4 — propósito de la fecha estimada de reactivación (issue #88).** Se documenta el uso del dato que el flujo ya capturaba sin destino declarado: la fecha estimada de reactivación es **informativa para el administrador** y consistente con la naturaleza transitoria de la suspensión (quien suspende espera volver a operar la unidad; sin expectativa de retorno, el camino correcto es la inactivación — Flujo 7). Ningún proceso reacciona a la fecha: la reactivación (Flujo 5) sigue siendo siempre un gesto manual del administrador. Acompaña al modelo v1.8, que proyecta el dato en el read model de la unidad para su consulta. |
| 1.7 | 2026-07-08 | **Retiro del descarte automático de Borradores + limpieza de residuos del #46 (issue #87).** **R26:** el motivo de `Descartada` por cascada pasa de `abandono_por_inactividad` a **`cascada_grupo`** y desaparece la mención al descarte automático — descartar un borrador es siempre decisión del administrador (Flujo 8) o consecuencia de la cascada (Flujo 10). **Flujo 8 reescrito:** se retiran la advertencia del sistema inteligente sobre operaciones dependientes y la cancelación en cascada en consumidores — residuos del patrón anterior al #46: un borrador nunca es referenciado por la operación de otro sub-dominio, no hay nada que cancelar. **Flujo 3 limpiado:** el borrador ya no se describe como "solicitado por un consumidor (Flujo 2)" ni el sistema inteligente muestra "origen de la solicitud"; los consumidores actualizan su copia local y las operaciones diferidas a la espera de la unidad se resuelven solas (`[D15]`). Acompaña al modelo v1.9. |
| 1.8 | 2026-07-08 | **Replanteamiento — bosque en lugar de raíz única obligatoria (issue #85).** Glosario término 8: "Grupo raíz" (único, automático, protegido) se reemplaza por **"Grupo tope"** (grupo sin padre; un tenant puede tener varios; ninguno se crea automáticamente). **Se retiran `R2` y `R3`** (huecos — la numeración se conserva) y entra **`R31`**: estructura en bosque + consolidación "total compañía" por la frontera del tenant, con la homologación del ERP actual (centros de costo maestros consolidan reportes de sus auxiliares; hay varios maestros por empresa, nunca un "maestro único"). **Flujo 9**: crear grupo sin padre = nace como tope. **Flujo 10**: desaparece la validación "no es el grupo raíz con contenido"; de paso se limpian residuos del #46 en el impacto previsto y los efectos (ya no hay "operaciones pendientes en consumidores que se cancelarán" — un borrador descartado nunca fue referenciado). Acompaña al modelo v2.1 (`[D16]`, `[I13]` retirada) y al anexo de decisiones v1.3 (Decisión 2 actualizada). |
| 1.9 | 2026-07-08 | **Término definitivo del #85: "Grupo de primer nivel" (glosario término 8).** Reemplaza a "Grupo tope" (v1.8): definido **en plural** desde la primera frase para que nadie lo lea como un raíz único, y explícito en que es una condición **derivada** de no tener padre (comportamiento `esDePrimerNivel()` del modelo), no un atributo almacenado. Se descartaron "raíz" (colisiona con el concepto retirado) y el atributo almacenado (permitiría estados imposibles). `R31` y Flujo 9 alineados al término. Acompaña al modelo v2.2. |
| 1.10 | 2026-07-08 | **Formato y longitud del código — texto libre con salvaguardas + rango configurable por tenant (issue #89).** **R10 reescrita:** el código deja de ser "alfanumérico" (contradecía los ejemplos con guion) y pasa a **texto libre con salvaguardas** (no vacío, sin espacios al inicio/fin ni internos, solo imprimibles; separadores cosméticos); unicidad **sin distinguir mayúsculas** (`cc-001` = `CC-001`); longitud como rango `min`/`max` **configurable por tenant** dentro de la envolvente del dominio (`1 ≤ min ≤ max ≤ 50`), con **2–12 por defecto** (ya no tope duro) — lo que permite recibir estructuras legadas con su codificación intacta sin violar la inmutabilidad (R9/P3). **Glosario término 5 reconciliado** con R10 (desaparece la longitud "sugerida"). Acompaña al modelo v2.3. |
