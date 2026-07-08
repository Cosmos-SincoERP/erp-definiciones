# Anexo — Decisiones arquitectónicas del sub-dominio de Estructura Organizacional

> **Fecha:** 2026-07-08
> **Versión:** 1.4
> **Propósito:** Documentar las decisiones de diseño estructurales del sub-dominio de Estructura Organizacional, sus alternativas evaluadas y la justificación de cada una. Este anexo acompaña a `definicion-alcance.md` y sirve como referencia para el modelo de dominio.

---

## 1. Propósito

Durante la construcción del alcance de Estructura Organizacional se identificaron cuatro decisiones estructurales que rompen con el patrón actual de SincoA&F y que tienen impacto directo en el modelo de dominio, la FSM del agregado, los eventos emitidos y el contrato con los sub-dominios consumidores (OXP, Contabilidad, CXC, etc.). Este anexo deja registro de cada decisión, las alternativas consideradas y la justificación para que el equipo de desarrollo y futuros mantenedores puedan comprender por qué se eligió ese camino y qué estándares o referencias de industria lo respaldan.

Las decisiones documentadas son:

1. Codificación plana + jerarquía versionada como agregado aparte.
2. Ciclo de vida con 4 estados (`Borrador`, `Activa`, `Suspendida`, `Inactiva`) en lugar de la dupla activo/inactivo.
3. Fusión, División y Traslado modelados como eventos de dominio de primera clase.
4. Modelo multi-dimensional desde el diseño, aunque en F1 solo se exponga una dimensión.

---

## 2. Metodología

Las decisiones se tomaron con base en:

- **Investigación comparativa de ERPs líderes:** SAP S/4HANA, Oracle Fusion, Microsoft Dynamics 365 Finance, NetSuite, Workday, Odoo, Sage. Se analizó cómo cada uno modela la codificación de centros de costo, el ciclo de vida, los procesos de reestructuración y las dimensiones financieras.
- **Revisión de estándares contables internacionales:** IFRS (especialmente IFRS 8 — Operating Segments, IAS 1, IAS 8), COSO ERM, AICPA. El objetivo fue identificar si alguno prescribe formato o estructura de las unidades de imputación.
- **Experiencia del ERP actual (SincoA&F):** los dolores observados en producción — techo de combinaciones del código posicional, transacciones contra unidades inactivas, imposibilidad de rastrear fusiones/divisiones, imposibilidad de cruzar dimensiones ortogonales — orientaron qué problemas específicos se debían resolver.

---

## 3. Decisión 1 — Codificación plana + jerarquía versionada aparte

### Decisión tomada

Cada unidad y cada grupo organizacional tiene un **código alfanumérico plano** (longitud sugerida entre 4 y 12 caracteres, único por tenant). La jerarquía padre-hijo **no se embebe en el código**: se modela en un agregado separado, versionado por fecha efectiva.

### Alternativas evaluadas

| Enfoque | Ventajas | Desventajas |
|---------|----------|-------------|
| **Posicional embebido** — `0101` padre, `010101` hijo (patrón actual de SincoA&F) | Lectura humana directa; orden natural en reportes sin join; sin tabla adicional. | Techo duro de combinaciones; reestructurar (re-parenting) exige renumerar todo el sub-árbol; imposible versionar la jerarquía; bloquea fusiones, divisiones y traslados limpios. |
| **Código plano + jerarquía aparte** *(elegida)* | Jerarquía versionable y eventualmente múltiple (financiera, gerencial, etc.); re-parenting trivial; sin techo combinatorio; permite historia estructural. | Requiere proyección al componer reportes legibles; el orden visual debe calcularse. |
| **Atributo derivado `codigoPresentacion`** (compuesto desde la jerarquía) | Mantiene la lectura visual tipo `0101.010101`. | **Descartada.** Con códigos planos alfanuméricos, componer `CC-001.CC-002.CC-042` no aporta la legibilidad del posicional numérico. El valor solo existía si se mantenía numeración posicional dentro del código plano, lo que contradice el espíritu de la separación. |

### Justificación

El modelo posicional del ERP actual ya demostró su techo: empresas con estructuras más robustas (múltiples sucursales en varios países, proyectos con sub-proyectos anidados, matrices con varios ejes) no caben en las combinaciones que permite el código. Además, cualquier reestructuración rompe la numeración y obliga a renumerar manualmente el sub-árbol, lo que imposibilita la trazabilidad histórica.

Separar código y jerarquía es el **consenso de la industria moderna**. La jerarquía versionada habilita además las decisiones 2, 3 y 4 de este anexo.

### Cómo lo hacen los ERPs líderes

| ERP | Código | Jerarquía |
|-----|--------|-----------|
| **SAP S/4HANA** (Cost Center, tabla `CSKS`) | Campo `KOSTL`, 10 caracteres alfanuméricos | Standard Hierarchy + Alternative Hierarchies (tablas `SETNODE`/`SETLEAF`), vinculadas a Controlling Area |
| **Oracle Fusion** | Segmento del Chart of Accounts (flexfield), longitud configurable (típico 4-6), alfanumérico | Tree / Tree Version (Oracle Trees), totalmente separada del código |
| **Microsoft Dynamics 365 Finance** | Financial Dimension Value, hasta 20 caracteres alfanuméricos | Dimension Hierarchies independientes del código (múltiples jerarquías sobre la misma dimensión) |
| **NetSuite** | `internalId` plano por nodo; nombre con composición visual `Padre : Hijo : Nieto` | Parent-child nativo, sin codificación posicional |
| **Odoo** | Campo `code` libre (`account.analytic.account`) | `parent_id` para jerarquía |
| **Workday** | Code alfanumérico | Organization Hierarchies con versionado por fecha efectiva — el caso más fuerte de separación código/jerarquía/vigencia |
| **Sage Intacct / Sage X3** | Códigos alfanuméricos cortos | Jerarquía aparte |

### Implicaciones para el modelo

- La unidad y el grupo tienen cada uno un atributo `codigo` plano.
- La relación padre-hijo se modela como una entidad separada con vigencia (fecha efectiva desde/hasta).
- Los reportes que requieran orden visual jerárquico se resuelven por proyección, no por ordenamiento alfabético del código.
- La reestructuración (Decisión 3) opera sobre la jerarquía versionada sin tocar los códigos.

---

## 4. Decisión 2 — Estructura del árbol y ciclo de vida

### Estructura del árbol

La estructura organizacional se modela como un árbol con dos tipos de nodo:

| Tipo de nodo | Puede tener hijos | Tipos de hijos permitidos | Recibe imputaciones |
|--------------|-------------------|---------------------------|---------------------|
| **Grupo organizacional** | Sí | Cualquier combinación de sub-grupos y unidades organizacionales | No |
| **Unidad organizacional** | No (siempre hoja) | — | Sí |

Reglas estructurales:

- Toda unidad organizacional pertenece a **exactamente un grupo padre**.
- Un grupo puede contener cualquier combinación de sub-grupos y unidades organizacionales como hijos. No se restringe la mezcla — un grupo "Operaciones" puede tener simultáneamente un sub-grupo "Sucursales Norte" (con varias sucursales como unidades) y una unidad directa "Oficina Central".
- La estructura es un **bosque**: los grupos sin padre son los **grupos de primer nivel** y un tenant puede tener varios. Ningún grupo se crea automáticamente al inicializar el tenant. La consolidación "total compañía" la da la frontera del tenant, no un nodo único. *(Hasta la v1.2 esta regla imponía un "grupo raíz" único, automático y protegido — se retiró en el issue #85: no tenía justificación registrada, ningún mecanismo del modelo lo necesitaba, y contradecía tanto la visión multi-jerarquía de la Decisión 1 como la homologación con el ERP actual, donde los centros de costo maestros —contenedores de auxiliares para consolidar reportes— son varios por empresa, sin un "maestro único".)*
- Una unidad organizacional **nunca tiene hijos** — si en un caso de negocio se necesita estructura adicional bajo lo que parece una unidad, se modela con un grupo intermedio que la contenga junto con sus pares de detalle.

### Decisión sobre el ciclo de vida

| Tipo de nodo | Estados | Razón |
|--------------|---------|-------|
| **Grupo organizacional** | `Activo`, `Inactivo` | Los grupos no operan transaccionalmente — solo agrupan. No requieren `Borrador` (no se solicitan desde sub-dominios consumidores) ni `Suspendido` (no hay imputaciones que bloquear). |
| **Unidad organizacional** | `Borrador`, `Activa`, `Suspendida`, `Inactiva`, `Descartada` | Cubre los momentos transitorios reales de una unidad (apertura, operación, pausa, cierre, descarte de solicitud). |

Semántica de cada estado de unidad:

| Estado | Semántica |
|--------|-----------|
| `Borrador` | Unidad creada pero no transaccional. Editable. No recibe imputaciones. Representa una unidad en preparación (sucursal en apertura, proyecto aprobado presupuestalmente, centro de costo pendiente de aprobación). |
| `Activa` | Unidad operativa. Recibe imputaciones. |
| `Suspendida` | Unidad temporalmente fuera de operación. No recibe nuevas imputaciones pero sigue consultable y reportable. Aplica a cierres temporales, disputas en curso, congelamientos gerenciales. |
| `Inactiva` | Unidad dada de baja **después de haber operado**. No recibe imputaciones. Los registros históricos que la referencian se conservan intactos y siguen apareciendo en reportes históricos. **Reabrible**: puede volver a `Activa` si la unidad retoma operación (ej: sucursal que reabre, proyecto que se reanuda); la reapertura es un evento auditable distinto del de reactivación desde `Suspendida`. |
| `Descartada` | Unidad solicitada o creada en `Borrador` que **nunca llegó a operar** (rechazada por el administrador o abandonada). No tiene historial transaccional. Se filtra de reportes históricos. **Terminal estricto**: si se necesita volver a registrar una unidad similar, se crea una nueva con datos limpios y la identificación queda libre. |

### Cascada de inactivación

| Acción | Comportamiento |
|--------|----------------|
| Inactivar una **unidad** | No hay propagación — la unidad es hoja por definición. |
| Inactivar un **grupo** | Propaga la inactivación recursivamente a todos sus hijos (sub-grupos y unidades). El sistema inteligente muestra al administrador el impacto previsto antes de ejecutar (cantidad de sub-grupos, unidades activas, suspendidas y en borrador que se verán afectadas) y exige confirmación explícita. |
| Suspender un grupo | No aplica — los grupos no tienen estado `Suspendido`. Para pausar operativamente toda una rama se suspende a nivel de cada unidad hoja. |

### Alternativa evaluada (estados de unidad)

| Enfoque | Problema observado |
|---------|--------------------|
| **Solo Activa / Inactiva** (patrón actual de SincoA&F) | En la operación real, las unidades se dejan `Activa` durante períodos transitorios (apertura, suspensión) porque inactivarlas implicaría cerrarlas definitivamente. El resultado observado es que se registran transacciones incorrectas contra unidades que no deberían estar recibiendo imputaciones. |
| **4 estados sin `Descartada`** (reusando `Inactiva` para unidades que nunca operaron) | Mezcla dos semánticas distintas — "operó y se cerró" vs "nunca operó" — en el mismo estado. Genera ruido en reportes históricos (unidades sin historial aparecen como inactivas) y obliga a deducir el motivo del cierre revisando el log de eventos. |

### Justificación

La dupla activo/inactivo fuerza al usuario a una decisión binaria que no refleja la realidad del negocio. Los estados intermedios (`Borrador`, `Suspendida`) modelan explícitamente los momentos transitorios y bloquean las imputaciones cuando corresponde, cerrando la fuente de las transacciones incorrectas que se observan hoy. La separación de `Inactiva` y `Descartada` mejora la claridad semántica, la reportería y la auditoría sin agregar costo significativo a la FSM.

### Casos de negocio concretos

- **Borrador.** Una constructora aprueba presupuestalmente la apertura de una sucursal en Medellín para el próximo trimestre. El administrador crea la unidad en estado `Borrador` para que pueda ser referenciada en reportes de planeación, pero ningún módulo puede imputarle transacciones hasta que las licencias de operación se reciban y se active.
- **Suspendida.** Un proyecto de obra se detiene por tres meses por una disputa legal con el cliente. Durante ese tiempo el proyecto no debe recibir nuevas causaciones de costo, pero tampoco debe cerrarse definitivamente — cuando la disputa se resuelve, se reactiva.
- **Descartada.** Un usuario operativo en OXP solicita la creación de una unidad asociada a un proyecto que termina cancelándose antes de iniciar. La unidad queda en `Borrador` hasta que el administrador, al revisar la solicitud, la marca como `Descartada` con la razón documentada. La unidad no aparece en reportes históricos y la identificación queda libre para futuras solicitudes.
- **Inactiva.** Una sucursal opera durante 5 años, recibe miles de transacciones, y la empresa decide cerrarla. El administrador la inactiva. La sucursal deja de recibir nuevas imputaciones pero sigue apareciendo en reportes históricos de los 5 años de operación.

### Referencias

| ERP | Estados que maneja |
|-----|--------------------|
| **Workday** | `Proposed` / `Active` / `Inactive` + transiciones de cancelación de propuestas (`Proposed → Cancelled`) que separan el descarte del cierre operativo. Es el referente más cercano al modelo propuesto. |
| **Microsoft Dynamics 365 Finance** | Activo + `Suspended` + vigencia por fechas. Modela explícitamente el estado suspendido. |
| **SAP S/4HANA** | Activo + indicador de `Lock` con 4 granularidades (imputaciones reales, plan, compromisos, ingresos) + vigencia por fechas. |
| **Oracle Fusion** | Effective dating (`start_date` / `end_date`) + flag `enabled_flag`. |
| **NetSuite, Odoo** | Solo `isInactive` / `active`. |

### Implicaciones en la FSM del agregado

- La FSM de **unidad** tiene 5 estados. `Descartada` es el único terminal estricto. Transiciones permitidas (a alto nivel):
  - `Borrador → Activa` (activación tras revisión).
  - `Borrador → Descartada` (rechazo de solicitud o abandono).
  - `Activa → Suspendida` (pausa transitoria).
  - `Suspendida → Activa` (reactivación desde pausa).
  - `Activa → Inactiva` (cierre operativo).
  - `Suspendida → Inactiva` (cierre desde pausa).
  - `Inactiva → Activa` (**reapertura tras cierre** — la unidad retoma operación; el historial se conserva y se enlaza con la nueva operación).
  - `Borrador → Inactiva` no es transición válida directa — el descarte de un borrador siempre va a `Descartada`.
- La FSM de **grupo** tiene 2 estados: `Activo ↔ Inactivo`. Inactivar un grupo dispara la cascada descrita arriba. Reactivar un grupo solo cambia el estado del grupo; los hijos previamente inactivados o descartados por la cascada no se reactivan automáticamente — el sistema inteligente los identifica como candidatos y el administrador decide cuáles reactivar uno a uno.
- Cada transición emite un evento de dominio propio. `UnidadReactivada` (desde `Suspendida`) y `UnidadReabierta` (desde `Inactiva`) son **eventos diferenciados** para que la auditoría distinga "reactivación de pausa transitoria" de "reapertura tras cierre" sin inspeccionar payload — habilita métricas como "tasa de reaperturas" directamente sobre el catálogo de eventos.
- El detalle completo de la FSM, transiciones y eventos por transición se define en el modelo de dominio.

---

## 5. Decisión 3 — Reestructuración como eventos de dominio

### Decisión tomada

**Fusión**, **División** y **Traslado** se modelan como **eventos de dominio de primera clase** con fecha efectiva, unidades origen/destino, approver y razón.

| Proceso | Qué modela |
|---------|-----------|
| **Fusión** | Dos o más unidades se integran en una sola unidad destino. El historial transaccional de las unidades origen queda enlazado al destino. |
| **División** | Una unidad se separa en varias unidades destino. Se define la regla de distribución del historial (cuando aplique). |
| **Traslado** | Una unidad cambia de padre en la jerarquía. La unidad conserva su identidad y su historial; solo se modifica su posición en el árbol. |

### Alternativa descartada

| Enfoque | Por qué se descartó |
|---------|---------------------|
| **"Renombrar + crear nuevo"** (patrón habitual en SAP, Oracle, Dynamics fuera de Workday) | Pierde la relación origen-destino. Obliga a reconstrucción manual en cada reporte comparativo. No deja registro auditable del approver ni la razón del cambio. Cuando una auditoría exige re-expresión comparativa, el proceso se vuelve manual y propenso a error. |

### Justificación normativa

El argumento más fuerte es normativo:

- **IFRS 8 (Operating Segments), párrafos 29-30:** si cambia la estructura interna de la organización y eso altera la composición de los segmentos reportados, la entidad **debe re-expresar la información comparativa de periodos anteriores**, salvo impracticabilidad.
- **IAS 1 (Presentation of Financial Statements):** principio de consistencia en la presentación y comparabilidad entre periodos.
- **IAS 8 (Accounting Policies, Changes in Accounting Estimates and Errors):** principios de comparabilidad y tratamiento de cambios.

Sin historia estructural versionada y eventos explícitos de reestructuración, cumplir con estas normas exige reconstrucción manual en cada cierre que implique cambios organizacionales — lo que en auditoría real es un riesgo alto.

### Referencia principal

**Workday Reorganizations** es el ERP referente: modela los cambios estructurales como eventos formales con fecha efectiva y versión, incluyendo `Move`, `Inactivate`, `Create Subordinate`. SAP, Oracle, Dynamics y NetSuite no lo tienen como transacción de primera clase — se resuelve con bloquear el origen, crear el destino y usar reclasificaciones.

### Beneficios concretos

1. **Trazabilidad histórica.** Un evento `UnidadFusionada(origenes=[A,B], destino=C, fechaEfectiva)` permite reconstruir la estructura en cualquier fecha pasada.
2. **Comparabilidad IFRS 8 sin reconstrucción manual.** Los reportes comparativos se re-expresan de forma automática.
3. **Auditoría completa.** El approver, la razón y el impacto quedan en el mismo agregado, no dispersos en memos y correos.
4. **Reportes año-contra-año.** El usuario puede elegir entre "vista actual" (todo consolidado al destino) o "vista histórica" (cada período con su estructura de entonces).
5. **Consumidores reactivos.** OXP, Contabilidad y demás consumidores reciben el evento y aplican su propia reacción (reclasificación contable, actualización de unidad en obligaciones vigentes, reasignación de inmuebles, etc.).

### Implicaciones para el modelo

- Cada proceso es un comando con pre-condiciones y post-condiciones propias, que emite uno o más eventos de dominio.
- La jerarquía versionada (Decisión 1) es la estructura de soporte: cada reestructuración registra una nueva versión con fecha efectiva.
- El detalle de los comandos, payloads e invariantes se define en el modelo de dominio.

---

## 6. Decisión 4 — Modelo multi-dimensional desde el diseño

### Decisión tomada

El modelo se diseña desde F1 para soportar **múltiples dimensiones ortogonales** de imputación, aunque en F1 solo se expone en la UI la dimensión `Unidad Organizacional`. En fases posteriores se incorporan nuevas dimensiones (Proyecto, Sucursal, Línea de Negocio, Tipo de Obra, Cliente, etc.) **sin rediseño estructural**.

### Aclaración importante sobre complejidad

No hay **claves dinámicas** en el sentido de metadatos mutables en runtime. Cada dimensión es un **atributo estático, tipado, opcional** en el contrato de línea de traducción. La dimensionalidad se amplía por fase agregando campos al contrato, no por manipulación de metadatos:

```
F1:
  LineaTraduccion { valor, unidadOrganizacional }

F2 (agrega Proyecto):
  LineaTraduccion { valor, unidadOrganizacional, proyecto? }

F3 (agrega Línea de Negocio):
  LineaTraduccion { valor, unidadOrganizacional, proyecto?, lineaDeNegocio? }
```

La complejidad real, y que debe cuidarse en el diseño, está en **tres puntos acotados**:

1. **Reportería.** El motor de reportes debe soportar `GROUP BY` por N dimensiones. Se resuelve con cualquier motor OLAP o SQL agrupado, no es exótico.
2. **Reglas de derivación contable.** Una regla puede depender de una o varias dimensiones. Esto lo resuelve el sub-dominio de Contabilidad con sus plantillas y reglas de derivación — no es responsabilidad de Estructura Organizacional.
3. **Contrato con consumidores (OXP, CXC, etc.).** Al agregar una dimensión nueva en F2, se extiende el contrato de línea de traducción con un campo opcional. No rompe compatibilidad con consumidores existentes.

Lo que se diseña en F1 es **el espacio** para agregar dimensiones, no las dimensiones en sí.

### Ejemplo concreto — constructora

Una empresa constructora quiere analizar costos por (a) proyecto, (b) sucursal, (c) tipo de obra, (d) cliente simultáneamente.

| Enfoque | Nodos requeridos | Viabilidad |
|---------|------------------|-----------|
| **Mono-jerárquico** (todo colgado del árbol de unidades) | Nodos tipo `Sucursal-Bogotá/Edificación/Cliente-X/Torre-A`. Con 10 sucursales × 5 tipos × 50 clientes × 200 proyectos = **500.000 nodos teóricos**, la mayoría vacíos. | Inviable — explosión combinatoria, mantenimiento inviable. |
| **Multi-dimensional** *(elegida)* | 4 catálogos independientes: Proyectos (200) + Sucursales (10) + Tipos (5) + Clientes (50) = **265 valores maestros**. Cada factura lleva las 4 claves. Cruces arbitrarios con `GROUP BY`. | Estándar de industria. |

### Referencias

| ERP | Modelo multi-dimensional |
|-----|--------------------------|
| **Microsoft Dynamics 365 Finance** | "Financial Dimensions" — conjunto configurable de ejes (Department, Cost Center, Project, Customer, Worker, Item, custom). Cada transacción se imputa a una combinación de valores. Soporta `Account Structures` y `Advanced Rules` para validar combinaciones por cuenta contable. |
| **NetSuite** | "Classifications" — tres ejes nativos (Department, Class, Location) + Custom Segments ilimitados. |
| **Odoo 16+** | "Analytic Accounts" + "Analytic Plans" + "Analytic Tags" — una transacción puede imputarse a varios planes analíticos ortogonales simultáneamente. |
| **Oracle Fusion** | "Segments" del Chart of Accounts — hasta 30 segmentos configurables. |
| **SAP S/4HANA** | Combina Profit Center, Cost Center, Segment, Business Area, Functional Area, WBS Element, Internal Order — todas dimensiones ortogonales imputables en una misma línea. |

### Tendencia

Consenso claro: todos los ERPs modernos ofrecen dimensiones ortogonales. La jerarquía pura sobrevive solo en ERPs legacy o locales.

### Justificación del diseño desde F1

El costo de prever extensibilidad multi-dimensional en F1 es bajo: se modela `Unidad Organizacional` como una dimensión identificable en el contrato. El costo de migrar de mono-jerárquico a multi-dimensional en producción es altísimo — toca Contabilidad, OXP, Impuestos y cualquier otro sub-dominio que produzca líneas de traducción. Invertir la simplicidad aparente de F1 a cambio de una extensibilidad genuina en F2+ es una decisión net-positive.

### Plan por fases

- **F1:** se expone en la UI y en el contrato solo la dimensión `Unidad Organizacional`. El modelo y los contratos quedan preparados para agregar dimensiones adicionales.
- **F2+:** se incorporan nuevas dimensiones según demanda (Proyecto, Sucursal, Línea de Negocio, etc.) extendiendo el contrato de línea de traducción con campos opcionales. Los sub-dominios que no consuman esas dimensiones no requieren cambio.

### Implicaciones para el modelo

- `Unidad Organizacional` no se modela como si fuera la única dimensión posible, sino como una entre N.
- El contrato de línea de traducción hacia Contabilidad se diseña para aceptar campos opcionales de dimensiones adicionales sin rediseño.
- Las reglas de derivación contable (responsabilidad de Contabilidad) deben poder evaluarse contra N dimensiones.

---

## 7. Sobre estándares internacionales

### Qué NO prescriben

No existe ningún estándar **IFRS, COSO, AICPA, AccountAbility** ni equivalente que prescriba el formato de codificación de unidades de imputación ni la estructura jerárquica. Son decisiones puramente arquitectónicas y de UX.

- **IFRS 8 (Operating Segments)** regula cómo se **reportan** los segmentos, no cómo se **codifican** internamente.
- **COSO ERM** habla de estructura de control, no de keys ni codificación.
- **AICPA y AccountAbility** no opinan sobre el formato.

### Dónde SÍ aplican

- **IFRS 8, párrafos 29-30:** obliga a re-expresar la información comparativa de periodos anteriores cuando cambia la estructura interna de la organización. Esto refuerza la **Decisión 3** (reestructuración como eventos de dominio).
- **IAS 1:** principio de consistencia en la presentación. Refuerza la **Decisión 3**.
- **IAS 8:** principios de comparabilidad y tratamiento de cambios. Refuerza la **Decisión 3**.

### Conclusión

La codificación y la jerarquía son decisiones arquitectónicas libres. Donde el marco normativo sí aprieta es en la **comparabilidad histórica**, lo que sostiene la decisión de modelar la reestructuración como eventos de dominio con historia estructural versionada.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-24 | Versión inicial. Documenta las 4 decisiones arquitectónicas del sub-dominio (codificación plana + jerarquía aparte, 4 estados del ciclo de vida, reestructuración como eventos de dominio, modelo multi-dimensional desde el diseño) y la postura sobre estándares internacionales. |
| 1.1 | 2026-05-26 | Decisión 2 ampliada y renombrada a "Estructura del árbol y ciclo de vida". Se formaliza la estructura del árbol (grupo agrupa cualquier combinación de sub-grupos y unidades; unidad es siempre hoja; raíz única por tenant). Se separan los estados de grupo (`Activo`/`Inactivo`) y unidad (5 estados, agregando `Descartada` para distinguir descarte de solicitud vs cierre operativo). Se documenta la cascada de inactivación de grupos. Casos de negocio y referencias actualizados. FSM detallada con las 6 transiciones permitidas de unidad. |
| 1.2 | 2026-05-27 | FSM de unidad ampliada: `Inactiva` deja de ser terminal estricto. Se agrega la transición `Inactiva → Activa` (reapertura tras cierre — sucursal que reabre, proyecto que se reanuda) con evento dedicado `UnidadReabierta`, diferenciado de `UnidadReactivada` (que aplica solo desde `Suspendida`). Solo `Descartada` permanece como terminal estricto. Justificación: la pureza semántica de "terminal" obligaba al usuario a recrear unidades y perder continuidad histórica ante errores de inactivación o cambios de decisión de negocio — los ERPs líderes (SAP, Oracle, Dynamics, Workday) permiten reabrir. Reactivación de grupo sin cascada inversa: los hijos previamente afectados por la cascada se reabren uno a uno con apoyo del sistema inteligente. |
| 1.3 | 2026-07-08 | **Decisión 2 — se retira el grupo raíz único obligatorio (issue #85).** La regla estructural "raíz única por tenant, creada automáticamente" (introducida en la v1.1 sin justificación registrada — única regla estructural del anexo sin porqué escrito) se reemplaza por la **estructura en bosque**: los grupos sin padre son topes, un tenant puede tener varios, y la frontera de consolidación es el tenant. Fundamento: ningún mecanismo del modelo necesitaba el ancestro único (ciclos, nivel y cascada operan por sub-árbol); el catálogo de tipos dejó de vivir en el raíz (#86); la visión multi-jerarquía de la Decisión 1 es incompatible con un raíz único; y la homologación con el ERP actual (centros de costo **maestros** = contenedores para consolidar reportes de los **auxiliares** transaccionales) muestra que el negocio real opera con varios maestros, sin un "maestro único". Acompaña al alcance v1.8 (término "Grupo tope", R2/R3 retiradas, R31 nueva) y al modelo v2.1 (`[D16]`, `[I13]` retirada, `esRaiz` eliminado). |
| 1.4 | 2026-07-08 | **Término definitivo para los grupos sin padre: "grupos de primer nivel"** (reemplaza "topes" de la v1.3). Decidido con el usuario tras evaluar alternativas: "raíz" descartado por colisión con el concepto retirado en el #85; atributo almacenado descartado (estado imposible); la condición es derivada de `padreId == null` y se expone con nombre de negocio en glosario, comportamiento calculado y proyección. |
