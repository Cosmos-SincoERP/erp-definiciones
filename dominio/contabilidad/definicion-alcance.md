# Definición de Alcance — Contabilidad

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

El sub-dominio de Contabilidad es el sistema centralizado del ERP responsable de expresar los hechos económicos producidos por los sub-dominios transaccionales (OXP, CXC, Tesorería, Nómina, Activos Fijos, Arrendamientos, entre otros) en lenguaje contable, y opcionalmente de gestionar el registro contable de la empresa.

El sub-dominio opera en dos niveles:

- **Nivel 1 (N1) — Motor de Traducción:** Componente obligatorio. Está compuesto por dos capacidades: (1) el motor de traducción, que recibe los hechos económicos de los sub-dominios consumidores mediante un contrato estandarizado y los traduce a borradores contables mediante plantillas y reglas de derivación de cuentas; y (2) el Servicio de Entrega, que toma los borradores resueltos y los entrega al sistema contable de destino configurado, gestionando la comunicación con el destino e informando el resultado a los consumidores. N1 puede operar de forma independiente — un cliente que ya tiene un sistema contable externo (SincoA&F, Siigo, Alegra u otro) puede usar N1 como adaptador sin necesidad de activar N2.

- **Nivel 2 (N2) — Sistema contable:** Componente opcional. Recibe los borradores resueltos del Servicio de Entrega, los persiste como asientos contables inmutables, gestiona libros contables, periodos, numeración y genera los reportes contables de la empresa (auxiliar contable, saldos, balances, estados financieros). N2 reemplaza al sistema contable externo cuando se activa. N2 no puede operar sin N1.

En el resto de este documento se referencian como **N1** y **N2**.

Ningún sub-dominio transaccional conoce cuentas contables, centros de costo contables ni naturalezas débito/crédito — solo emiten hechos económicos ricos en contexto de negocio. N1 los consume, aplica sus reglas de derivación y produce borradores contables.

Este diseño sigue el patrón de **Subledger Accounting Engine** implementado por Oracle Fusion (SLA) y Workday (Account Posting Rules), adaptado a una arquitectura donde el motor de traducción es independiente del sistema contable de destino. La investigación comparativa con ERPs líderes (SAP, Oracle, Dynamics 365, NetSuite, Odoo, Workday) está documentada en `fuentes/investigacion-traduccion-contable-erps.md`.

### Contexto actual

El ERP actual (SincoA&F) opera con un patrón donde cada módulo transaccional conoce el dominio contable completo — cuentas, centros de costo, terceros contables — y arma el asiento casi totalmente formado antes de enviarlo al sistema contable. SincoA&F recibe estos asientos pre-construidos, les asigna un consecutivo y los persiste.

Las reglas de mapeo de cuentas están dispersas en cada módulo transaccional, materializadas en catálogos de hasta 7 mil cuentas contables por cliente que combinan múltiples dimensiones: empresa, clasificación de gasto, tipo de tercero, régimen fiscal, unidad organizacional, entre otros. Cada contador personaliza estas combinaciones según la interpretación contable de su empresa.

### Problemas actuales

1. **Acoplamiento transaccional-contable.** Cada módulo transaccional (OXP, CXC, Compras) contiene lógica de derivación de cuentas contables. Un cambio en el plan de cuentas requiere modificar múltiples módulos.
2. **Reglas de mapeo dispersas.** Las reglas que determinan a qué cuenta contable va cada tipo de hecho económico están distribuidas en cada módulo, sin un punto único de gestión ni auditoría.
3. **Duplicación de catálogos.** Cada módulo mantiene su propia copia de la configuración de mapeo contable. Sincronizar cambios entre módulos es propenso a errores.
4. **Conocimiento contable en usuarios operativos.** Los usuarios de módulos transaccionales deben entender cuentas contables para operar, en lugar de trabajar exclusivamente con conceptos de negocio.
5. **Rigidez ante cambios normativos.** Adaptar el tratamiento contable de un tipo de transacción (ej: nueva NIIF, cambio de plan de cuentas) requiere intervenir cada módulo que lo produce.
6. **Imposibilidad de comercialización independiente.** Los módulos de negocio (OXP, ABR, CXC) no pueden venderse sin el sistema contable completo. Un cliente que ya tiene Siigo o Alegra no puede usar los módulos de negocio sin adoptar SincoA&F.

---

## Sección 2: Glosario de términos

| # | Término | Nivel | Definición |
|---|---------|:-----:|-----------|
| 1 | **Hecho económico** | N1 | Evento de negocio producido por un sub-dominio transaccional que tiene impacto contable. Ejemplos: causación de una obligación, aplicación de un pago, registro de un cargo financiero, causación de una devolución. |
| 2 | **Línea de traducción** | N1 | Unidad mínima de información que un sub-dominio transaccional emite para ser traducida a contabilidad. Contiene el tipo de componente, el contexto de negocio (clasificación, tercero, empresa, naturaleza fiscal) y el valor ya distribuido por unidad organizacional. Contrato estandarizado entre todos los sub-dominios y N1. |
| 3 | **Regla de derivación** | N1 | Configuración que determina qué cuenta contable corresponde a una combinación de dimensiones del hecho económico. Las reglas son propiedad exclusiva de N1. |
| 4 | **Motor de traducción** | N1 | Componente de N1 que recibe líneas de traducción de los sub-dominios transaccionales, aplica las reglas de derivación y produce borradores contables. |
| 5 | **Borrador contable** | N1 | Resultado de la traducción de un hecho económico. Tiene tres estados posibles: pendiente (cuentas sin resolver — el contador puede completarlo), resuelto (completo y balanceado — se entrega automáticamente al Servicio de Entrega) y descartado (solo aplica a borradores manuales creados por el contador). Un borrador en estado pendiente puede editarse para resolver las cuentas faltantes. |
| 6 | **Plantilla de asiento** | N1 | Estructura universal de roles (débitos/créditos) por tipo de transacción contable. Define qué líneas genera el borrador, qué naturaleza tienen y cómo genera la contrapartida. Es contenido incluido en el producto. El concepto se detalla con ejemplos en `anexo-ejemplo-plantilla-de-asiento.md`. |
| 7 | **Documento fuente** | N1 | Identificador del documento del sub-dominio consumidor que origina el borrador (número de factura, número de obligación, número de pago, etc.). Es lo que el usuario ve en los reportes contables como columna de referencia. |
| 8 | **Asiento contable** | N2 | Registro contable inmutable generado a partir de un borrador resuelto. Compuesto por encabezado (fecha, tipo, comprobante, libro, referencia al hecho económico de origen) y partidas débito/crédito. La suma de débitos debe ser igual a la suma de créditos. Solo existe cuando N2 está activo como destino. |
| 9 | **Partida contable** | N2 | Línea individual dentro de un asiento contable que registra un débito o crédito a una cuenta auxiliar específica, con su unidad organizacional, tercero y valor. |
| 10 | **Plan de cuentas** | N1 | Catálogo jerárquico de cuentas contables de una empresa. Las cuentas se clasifican en **maestras** (agrupadoras, no reciben movimientos) y **auxiliares** (de detalle, donde se registran las partidas). Solo las cuentas auxiliares son posteables. El PUC es necesario en N1 para que el motor pueda resolver cuentas durante la traducción. |
| 11 | **Libro contable** | N2 | Configuración que define un conjunto de registros contables. El producto provee dos libros predeterminados al onboardear la empresa: **Principal** (donde se registra toda la operación bajo el PUC NIIF) y **Fiscal** (donde se registran los ajustes específicos para reportes a la autoridad fiscal). El analista contable puede agregar libros adicionales (Gerencial, Consolidación, sectoriales u otros) según las necesidades de la empresa. Cada libro tiene un PUC asociado; en la operación estándar moderna, todos los libros apuntan al mismo PUC NIIF. La equivalencia entre libros que usen PUCs distintos se utiliza solo en casos excepcionales (transición de marcos, sectores regulados, consolidación). |
| 12 | **Unidad organizacional** | N1/N2 | Destino de negocio al que se imputan valores para efectos de control de gestión. Los sub-dominios transaccionales la envían como dato de la línea de traducción. Es gestionada por el sub-dominio de Estructura Organizacional. |
| 13 | **Contrapartida** | N1 | Partida del borrador que completa el balance para que la ecuación débito = crédito se cumpla. Los sub-dominios transaccionales no conocen ni emiten contrapartidas — el motor de traducción las genera según el tipo de transacción y la configuración. |
| 14 | **Dimensión de derivación** | N1 | Atributo del hecho económico que participa en la resolución de la cuenta contable. Ejemplos: tipo de componente, clasificación del gasto, régimen fiscal del tercero, empresa, unidad organizacional, tipo de transacción. |
| 15 | **Tipo de transacción contable** | N1 | Clasificación del hecho económico desde la perspectiva contable. Ejemplos: causación de gasto, anticipo a proveedor, nota crédito, cargo financiero. Determina qué plantilla de asiento aplica. |
| 16 | **Periodo contable** | N2 | Intervalo de tiempo (generalmente mensual) en el que se agrupan los asientos contables. Un periodo puede estar abierto (acepta nuevos asientos) o cerrado (no acepta). |
| 17 | **Numeración contable** | N2 | Secuencia numérica única asignada a cada asiento contable. Se segmenta por dimensiones configurables (empresa, tipo de comprobante, periodo, sucursal). Documentado en `anexo-analisis-numeracion-contable.md`. |
| 18 | **Comprobante contable** | N2 | Representación mediante la cual el usuario consulta, imprime o referencia un asiento contable. En Colombia, el Decreto 2649 (Art. 124) exige que sea numerado consecutivamente. |
| 19 | **Sub-dominio consumidor** | N1 | Cualquier sub-dominio transaccional del ERP que produce hechos económicos con impacto contable y los envía a N1 para traducción. |
| 20 | **Sistema contable de destino** | N1 | Sistema que recibe los borradores resueltos a través del Servicio de Entrega. Puede ser N2 (sistema contable propio), SincoA&F, Siigo, Alegra u otro sistema contable externo. El destino es configurable por empresa. |
| 21 | **Servicio de Entrega** | N1 | Componente de N1 que toma los borradores resueltos y los entrega al sistema contable de destino configurado. Gestiona la comunicación con el destino: si el destino acepta, informa el resultado con la referencia asignada por el destino; si rechaza, informa el motivo. Cada destino tiene su propio adaptador que conoce el formato y protocolo del sistema externo. |
| 22 | **Consola de contabilización** | N1 | Capacidad de N1 para operar y consultar el estado de los hechos económicos: pendientes de resolución, entregados, aceptados, rechazados o descartados. Permite ejecutar acciones operativas (resolver cuentas, reintentar entrega, navegar al origen o al destino) y consultar el historial de transiciones e intentos de entrega. La definición funcional completa está en la Sección 5. |
| 23 | **Cadena de resolución** | N1 | Proceso de tres niveles que determina la cuenta auxiliar para cada partida del borrador: Nivel A (regla manual del analista contable), Nivel C (aprendizaje acumulado del sistema) y Nivel B (inferencia analizando el plan de cuentas). Se evalúan en orden de precedencia: A primero, luego C, luego B. |
| 24 | **Aprendizaje del sistema** | N1 | Registro acumulado de las resoluciones de cuentas que el contador ha tomado al completar borradores pendientes. Cada resolución asocia una combinación de dimensiones (tipo de componente, clasificación, empresa) con la cuenta auxiliar elegida. Corresponde al Nivel C de la cadena de resolución. |
| 25 | **Marco contable** | N1 | Catálogo de marcos contables disponibles para una empresa. Cada marco identifica formalmente el esquema bajo el cual se diseña un plan de cuentas (NIIF, marcos locales, gerencial, consolidación, sectoriales, etc.). El producto precarga el marco NIIF como predeterminado al crear la empresa, junto con su PUC NIIF y los libros Principal y Fiscal. Marcos adicionales (consolidación, fiscal alterno, sectoriales) los crea un usuario con permiso especial cuando la empresa los requiere. Justificación detallada en `anexo-marco-contable-y-arquitectura-puc.md`. |
| 26 | **Asistente de onboarding del PUC** | N1 | Capacidad transversal del producto que guía al consultor especializado y al analista contable durante la carga inicial del PUC de una empresa. Compara el PUC del sistema anterior contra una estructura de referencia, aplica reglas heurísticas y aprendizaje acumulado, presenta sugerencias iterativas por grupo contable y persiste cada proceso como historial auditable. Vive en el servicio compartido `compartido/asistente-onboarding/`. El caso PUC es el primero del patrón; otros casos futuros (terceros, unidades organizacionales, saldos iniciales) seguirán el mismo modelo. |
| 27 | **Grupo del PUC esperado** | N1 | Lista de prefijos del código del plan de cuentas (clase, grupo o cuenta — longitud variable) declarada en cada componente del rol de una plantilla de asiento, y a nivel del rol para la contrapartida. Acota la inferencia automática (Nivel B de la cadena de resolución) a las cuentas cuyo código inicia por alguno de los prefijos. No determina la cuenta exacta — solo orienta la búsqueda. [R47] |
| 28 | **Descripción del borrador** | N1 | Texto general que narra el hecho económico, a nivel del encabezado del borrador. La envía el consumidor; es opcional — si no la envía, el borrador queda sin descripción general. Distinta del `documentoFuente`, que es un identificador, no una narración. [R48] |
| 29 | **Descripción de concepto** | N1 | Narración del movimiento de una partida individual (ej: "Honorarios auditoría externa"). La envía el consumidor y el motor la asigna solo a las partidas cuyo componente la lleva (definido en la plantilla de asiento mediante `llevaDescripcionConcepto`): los componentes de concepto de negocio sí, los de impuesto y retención no. [R48] |
| 30 | **Rol de la partida** | N1 | Código del conjunto cerrado (GASTO, IMPUESTO, RETENCION, CONTRAPARTIDA) que identifica la función de una partida dentro del asiento. Se define en la plantilla de asiento y la partida del borrador lo hereda; se entrega al sistema contable de destino para identificar las partidas tributarias. No es texto libre — es un código. [R49] |

---

## Sección 3: Actores del sistema

### Actores internos (usuarios del sistema)

| Actor | Descripción | Responsabilidades |
|-------|-------------|-------------------|
| **Analista contable** | Profesional contable responsable de la configuración. Conoce el plan de cuentas, las normas contables y las políticas de la empresa. | N1: configurar y mantener las reglas de derivación, plantillas de asiento y plan de cuentas. Supervisar aprendizajes del sistema: promover a regla formal o invalidar aprendizajes erróneos. N2: configurar libros contables, equivalencia entre PUCs, periodos contables y numeración. |
| **Contador** | Profesional que opera y supervisa la contabilidad de la empresa. Puede gestionar una o varias empresas. | N1: resolver borradores pendientes (genera aprendizaje al resolver cuentas), gestionar borradores rechazados por el destino, consultar la consola de contabilización. N2: gestionar periodos contables (apertura/cierre), registrar asientos manuales, anular asientos, supervisar reportes contables. |
| **Administrador del sistema** | Responsable de la configuración técnica y operativa de la plataforma. No es profesional contable. | Configurar el sistema contable de destino por empresa (N2, SincoA&F, Siigo, Alegra). No opera la contabilidad ni configura reglas contables. |

### Actores externos (sistemas integrados)

| Sistema | Descripción | Relación con el dominio |
|---------|-------------|------------------------|
| **Sub-dominios transaccionales** (OXP, CXC, Tesorería, Nómina, ABR, etc.) | Módulos del ERP que producen hechos económicos. | Emiten líneas de traducción que N1 consume para generar borradores. No conocen cuentas contables. Se enteran del resultado de la contabilización para actualizar su referencia al asiento. |
| **Sistema contable externo** (SincoA&F, Siigo, Alegra, otros) | Sistema contable del cliente cuando N2 no está activo. | Recibe borradores resueltos del Servicio de Entrega, asigna su propia numeración y persiste. Retorna su referencia (consecutivo, comprobante) o rechaza con motivo. |
| **Sub-dominio de Impuestos** | Motor de cálculo tributario centralizado. | No interactúa directamente con Contabilidad. Los tributos llegan como parte de las líneas de traducción emitidas por cada sub-dominio consumidor desde su copia del desglose fiscal. |
| **Sub-dominio de Terceros** | Fuente de verdad de la identificación y estado de terceros. | N1 valida que el tercero esté activo al crear borradores. |
| **Sub-dominio de Estructura Organizacional** | Fuente de verdad de las unidades organizacionales. | N1 recibe la unidad organizacional en las líneas de traducción y la valida como activa. |

---

## Sección 4: Flujo principal

El sub-dominio de Contabilidad opera en seis flujos:

### Flujo 1 — Configuración (Analista contable)

**Configuración de N1:**

1. El sistema provee las **plantillas de asiento** como contenido incluido en el producto — la estructura universal de roles (débitos/créditos) por cada tipo de transacción contable. El analista contable no necesita configurarlas.

2. Para cada empresa, el analista contable vincula un **plan de cuentas (PUC)**: la jerarquía de cuentas maestras y auxiliares. N1 necesita el PUC para resolver cuentas durante la traducción. El cargue inicial del PUC se realiza mediante el **Asistente de onboarding del PUC**, capacidad transversal del producto que vive en `compartido/asistente-onboarding/` y guía al consultor especializado a través de un flujo iterativo con sugerencias automáticas, aprendizaje acumulado e historial auditable.

3. El sistema provee las **reglas de derivación** como contenido incluido en el producto. El analista contable puede agregar reglas adicionales cuando considere necesario para cubrir excepciones no contempladas.

**Configuración de N2 (solo si el destino es el sistema contable propio):**

5. El sistema provee dos **libros contables** predeterminados al onboardear la empresa: **Principal** y **Fiscal**, ambos asociados al PUC NIIF predeterminado (que pertenece al marco contable NIIF cargado automáticamente). El analista contable puede configurar libros adicionales (Gerencial, Consolidación u otros tipos custom) según las necesidades de la empresa. La **equivalencia entre libros** se utiliza solo en casos excepcionales (transición de marcos, sectores regulados con PUC sectorial, grupos empresariales con consolidación) — en la operación moderna estándar, todos los libros comparten el mismo PUC NIIF y las diferencias entre tratamientos contables se modelan como asientos específicos del libro fiscal.

6. El sistema provee la **numeración contable** con una estructura por defecto. El analista contable puede modificar las secuencias si necesita una estructura diferente. Las dimensiones de segmentación (empresa, tipo de comprobante, periodo, sucursal) están documentadas en `anexo-analisis-numeracion-contable.md`.

7. El analista contable confirma la **fecha de inicio de operación** contable. El sistema crea automáticamente los periodos contables restantes del año en curso: el periodo corriente queda abierto y los futuros cerrados. Para los años siguientes, cuando los periodos disponibles se agoten, el sistema informa al analista que se requieren nuevos periodos. Al confirmar, nacen cerrados y el analista los abre mes a mes.

### Flujo 2 — Traducción contable (N1)

1. Un **sub-dominio consumidor** (OXP, CXC, Tesorería, etc.) confirma una transacción que tiene impacto contable. Como parte de la confirmación, emite las **líneas de traducción** del hecho económico mediante el contrato estandarizado. Cada línea contiene el tipo de transacción, el tipo de componente, la clasificación de negocio, el tercero, la empresa, la unidad organizacional, el valor ya distribuido, la moneda, la fecha, la referencia de origen y el documento fuente. El contrato se detalla con ejemplos en `anexo-ejemplo-plantilla-de-asiento.md`.

2. N1 valida que la **referencia de origen sea única** — si ya existe un borrador con esa misma referencia, ignora la solicitud. Esto protege contra duplicados por entregas repetidas.

3. El **motor de traducción** recibe las líneas y ejecuta la traducción:
   a. Identifica el tipo de transacción contable y aplica la **plantilla de asiento** correspondiente.
   b. Para cada rol de la plantilla, resuelve la **cuenta auxiliar** del plan de cuentas mediante la cadena de resolución: primero busca una regla de derivación configurada por el analista contable; si no la encuentra, busca en el aprendizaje acumulado de resoluciones previas; si tampoco, infiere la cuenta más probable analizando el plan de cuentas.
   c. Genera la **contrapartida** que completa el balance (suma de débitos = suma de créditos).

4. El resultado es un **borrador contable**:
   - Si la cadena resolvió todas las cuentas y el borrador balancea → nace **resuelto** y se entrega al Servicio de Entrega (ver Flujo 3).
   - Si quedaron cuentas sin resolver → nace **pendiente** y espera intervención del contador (ver Flujo 4).

### Flujo 3 — Entrega al destino (N1, Servicio de Entrega)

1. El **Servicio de Entrega** toma el borrador resuelto y lo envía al sistema contable de destino configurado para la empresa (N2 propio, SincoA&F, Siigo, Alegra u otro).

2. El destino procesa el borrador y responde:
   - **Si acepta:** retorna su propia referencia (consecutivo, comprobante u otro identificador según el sistema). El Servicio de Entrega informa el resultado. Los consumidores interesados se enteran de que su hecho económico fue contabilizado y actualizan su referencia al asiento.
   - **Si rechaza:** retorna el motivo (periodo cerrado, cuenta que no existe en el destino u otra razón). El Servicio de Entrega informa el motivo del rechazo.

3. Cuando un borrador es rechazado por el destino, queda visible en la **consola de contabilización** para que el contador decida la acción correctiva: corregir la causa en el destino (ej: abrir el periodo), modificar el borrador o reintentar el envío. La definición funcional completa está en la Sección 5.

4. Un borrador rechazado vuelve a **pendiente**. El contador decide la acción: corregir cuentas si el motivo lo requiere, reintentar sin cambios (si la causa se corrigió en el destino), o descartar si es un borrador manual.

### Flujo 4 — Resolución de borradores pendientes (N1)

1. El contador consulta los **borradores pendientes** — aquellos donde la cadena de resolución no pudo determinar todas las cuentas auxiliares. El sistema presenta cada borrador con la estructura completa: las partidas ya resueltas y las que faltan, junto con las sugerencias de cuentas cuando el sistema pudo inferir candidatas.

2. El contador **asigna o corrige** las cuentas faltantes seleccionando del plan de cuentas. Puede confirmar una sugerencia del sistema o elegir una cuenta diferente.

3. Cada decisión del contador **alimenta el aprendizaje** del sistema: la próxima vez que se presente la misma combinación de dimensiones, el sistema resolverá automáticamente. El analista contable puede convertir un aprendizaje en una regla formal de derivación cuando quiere que sea explícita e inmutable.

4. Cuando todas las cuentas están resueltas y el borrador balancea, pasa a resuelto y se entrega al Servicio de Entrega (Flujo 3).

5. Los borradores generados desde un sub-dominio consumidor **no se pueden descartar** — el hecho económico ya ocurrió y debe contabilizarse. Los borradores de asientos manuales (creados por el contador) **sí se pueden descartar**.

### Flujo 5 — Contabilización y registro (N2)

Este flujo solo aplica cuando el sistema contable de destino es el sistema contable propio (N2).

1. N2 recibe el borrador resuelto del Servicio de Entrega. Valida que el **periodo esté abierto** para el tipo de comprobante correspondiente. Si el periodo está cerrado, rechaza el borrador con el motivo — el Servicio de Entrega lo gestiona según el Flujo 3, de la misma forma que lo haría con cualquier otro destino (SincoA&F, Siigo, etc.).

2. N2 asigna el **consecutivo** según la configuración de numeración y genera el **asiento contable** — registro inmutable con su comprobante, periodo, partidas, libro y referencia al hecho económico de origen.

3. N2 retorna al Servicio de Entrega la referencia del asiento contable y el comprobante asignado.

4. Los **reportes contables** se actualizan automáticamente. Para cada libro que deba reflejar el asiento (el libro de registro y los libros cuyos PUCs tengan equivalencia configurada), se registran las entradas con la cuenta equivalente del PUC correspondiente. La equivalencia se congela al momento de registrar — cambios futuros en la configuración de equivalencia no afectan las entradas ya registradas.

#### Variante: asiento específico de un libro

Cuando un asiento aplica solo a un libro (ej: ajuste bajo NIIF que no tiene equivalente fiscal), el contador lo registra directamente en ese libro. Los reportes de otros libros no lo reflejan — no se genera equivalencia.

### Flujo 6 — Operación contable (N2)

Este flujo solo aplica cuando N2 está activo.

1. El contador puede registrar **asientos manuales** (ajustes contables, reclasificaciones) directamente en un libro específico. Estos asientos pasan por N1 (se crea un borrador que se resuelve y se entrega al Servicio de Entrega) siguiendo el mismo proceso.

2. Para **anular** un asiento contable, el contador genera un nuevo asiento con las partidas invertidas, referenciando al asiento original. El asiento original permanece intacto — los asientos contables nunca se modifican ni se eliminan.

3. El contador gestiona el **cierre de periodo**: al cerrar un periodo, el sistema no acepta nuevos asientos para ese periodo en los tipos de comprobante cerrados. El cierre puede incluir asientos de reclasificación (traslado de cuentas de resultado al resultado del ejercicio) según la configuración de la empresa.

4. El contador consulta la **trazabilidad** de cualquier asiento: desde el asiento puede navegar al hecho económico de origen, y desde cualquier transacción del consumidor puede ver el asiento contable que produjo, gracias a la consola de contabilización.

---

## Sección 5: Integraciones

### Entrada a N1

| Origen | Dato | Propósito |
|--------|------|-----------|
| **Sub-dominio consumidor** (OXP, CXC, Tesorería, Nómina, Activos Fijos, Arrendamientos) | Líneas de traducción: tipo de transacción, tipo de componente, clasificación, tercero, empresa, unidad organizacional, valor distribuido, moneda, fecha, referencia de origen, documento fuente y sub-dominio de origen | Alimentar el motor de traducción para generar borradores contables |

### Salida de N1 al Servicio de Entrega

| Destino | Dato | Propósito |
|---------|------|-----------|
| **Servicio de Entrega** (componente de N1) | Borrador resuelto: partidas con cuenta, tercero, unidad organizacional, débito/crédito, rol (GASTO/IMPUESTO/RETENCION/CONTRAPARTIDA), descripción de concepto, documento fuente, descripción general y referencia de origen | Entregar al sistema contable de destino configurado |

### Servicio de Entrega

| Dirección | Dato | Propósito |
|-----------|------|-----------|
| Salida hacia sistema contable de destino | Borrador resuelto en el formato que el destino espera | Contabilización en el sistema contable del cliente |
| Entrada desde sistema contable de destino | Aceptación con referencia (consecutivo, comprobante) o rechazo con motivo | Informar el resultado de la entrega |
| Salida hacia consumidores | Resultado de la contabilización: referencia de origen y referencia del asiento en el destino | Los consumidores interesados actualizan su referencia al asiento. Solo se informa el resultado final — no hay estados intermedios. |
| Salida hacia N1 | Resultado de la entrega (aceptación o rechazo) | N1 actualiza la consola de contabilización |

### Integraciones de N2 (solo si está activo como destino)

| Dirección | Dato | Propósito |
|-----------|------|-----------|
| Entrada (desde Servicio de Entrega) | Borrador resuelto | Generar asiento contable, asignar numeración, validar periodo |
| Salida (hacia Servicio de Entrega) | Referencia del asiento contable y comprobante, o rechazo con motivo | El Servicio de Entrega informa el resultado |
| Salida (reportes) | Auxiliar contable, saldos contables, balance de prueba, estados financieros | Consumo por usuarios, auditores y sistemas externos |

### Consola de contabilización

Capacidad de N1 para operar y consultar el estado de los hechos económicos. Sirve al contador (gestión operativa), soporte y consumidores que necesiten conocer el estado de sus transacciones.

**Unidad de consulta:** por hecho económico (referencia de origen).

**La consola presenta dos niveles de información:**

**Estado del borrador (información principal):**

| Estado | Descripción |
|--------|-------------|
| Pendiente | Cuentas sin resolver, o corrigiendo tras rechazo del destino, o reemplazado por re-emisión del consumidor. La causa específica se determina por el último evento del stream. |
| Resuelto | Todas las cuentas resueltas y balancea. Incluye borradores en tránsito hacia el destino y borradores ya aceptados por el destino. La diferencia se lee en el resultado de entrega (Enviado o Aceptado). |
| Descartado | Borrador manual descartado. |

**Resultado del último intento de entrega (contexto complementario):**

| Resultado | Descripción |
|-----------|-------------|
| Sin entrega | El borrador aún no se ha enviado al destino. |
| Enviado | El Servicio de Entrega envió el borrador. Esperando respuesta. |
| Aceptado | El destino aceptó la entrega. Referencia visible. |
| Rechazado | El destino rechazó la entrega. Motivo visible. |

El estado del borrador y el resultado de entrega son niveles diferentes. Un borrador en estado **Pendiente** con resultado de entrega **Rechazado** indica que fue rechazado por el destino y está siendo corregido. El resultado de entrega complementa al estado del borrador — no lo reemplaza.

**Relación entre Resuelto y Enviado:** Resuelto describe la condición contable del borrador — está completo y apto para ser entregado. Enviado describe la situación operativa de su entrega — ya existe un intento en curso hacia el destino. Un borrador puede estar Resuelto y al mismo tiempo tener como resultado de entrega Enviado. No son estados que compitan — uno es condición del borrador, el otro es progreso de la entrega.

**Campos mínimos por fila:** referenciaOrigen, subDominioOrigen, documentoFuente, fecha, estado del borrador, resultado de última entrega (si aplica), causa/motivo (si aplica), destino, referenciaDestino (si aceptado), fechaÚltimoIntento.

**Historial:** La consola muestra el estado actual y el historial de transiciones e intentos de entrega. Un mismo hecho económico puede tener múltiples entregas (primera rechazada, segunda aceptada).

**Regla de lectura operativa:**
- La fila principal de la consola representa siempre la **situación vigente** del hecho económico: estado actual del borrador + resultado del último intento de entrega como contexto.
- Un rechazo pasado no significa que el hecho económico siga rechazado si luego fue corregido y aceptado. El estado actual siempre refleja la situación vigente.
- El **historial** complementa la lectura principal — no la redefine. Sirve para responder: ¿cuántos intentos de entrega ha tenido? ¿Fue rechazado antes? ¿Qué evento produjo el estado actual? ¿Cuándo cambió por última vez?

**Acciones operativas desde la consola:**

| Acción | Disponible cuando |
|--------|-------------------|
| Resolver cuentas | Borrador pendiente (cuentas sin resolver) |
| Modificar campos del borrador | Borrador pendiente (cualquier causa) |
| Reintentar entrega | Borrador pendiente con resultado de entrega rechazado — después de corregir |
| Consultar motivo de rechazo | Borrador pendiente con resultado de entrega rechazado |
| Navegar al hecho económico origen | Siempre (enlace a la transacción del consumidor) |
| Navegar a referencia destino | Borrador resuelto con resultado de entrega aceptado (enlace al asiento en el destino) |
| Descartar | Borrador pendiente, solo manuales [R09][R10] |

### Trazabilidad bidireccional

| Dirección | Referencia | Propósito |
|-----------|-----------|-----------|
| Consumidor → N1 | Referencia de origen (única en N1) | N1 sabe qué hecho económico originó cada borrador. La unicidad protege contra duplicados. |
| Servicio de Entrega → Consumidor | Referencia al asiento contable en el destino (consecutivo, comprobante u otro identificador según el sistema configurado) | El consumidor sabe cuál es el asiento contable de su transacción en el destino. |

Este contrato es transversal — aplica por igual a todos los sub-dominios consumidores. El detalle del contrato de líneas de traducción está documentado en `anexo-ejemplo-plantilla-de-asiento.md`.

### Datos propios del sub-dominio

**N1:**
- **Plan de cuentas (PUC)** — jerarquía de cuentas maestras y auxiliares. Necesario para la traducción.
- **Reglas de derivación** — configuración que determina qué cuenta corresponde a cada combinación de dimensiones.
- **Aprendizaje del sistema** — resoluciones de cuentas acumuladas a partir de las decisiones del contador.
- **Consola de contabilización** — estado consolidado de cada hecho económico.

**N2 (solo si está activo):**
- **Libros contables** — configuración de libros predeterminados (Principal y Fiscal) sobre el PUC NIIF de la empresa. Libros adicionales (Gerencial, Consolidación, sectoriales) bajo demanda. La equivalencia entre PUCs solo aplica en casos excepcionales.
- **Marcos contables** — catálogo por empresa con el marco NIIF predeterminado. Marcos custom creados por usuario con permiso especial para casos como consolidación o sectores regulados.
- **Periodos contables** — apertura y cierre por empresa, con granularidad por tipo de comprobante.
- **Numeración contable** — secuencias por tipo de comprobante según las dimensiones de segmentación configuradas.

### Nota sobre la integración con Impuestos

El sub-dominio de Impuestos no interactúa directamente con Contabilidad. Los tributos (IVA, retenciones, etc.) llegan como parte de las líneas de traducción emitidas por cada sub-dominio consumidor desde su propia copia del desglose fiscal. N1 no conoce el dominio tributario — solo ve tipos de componente (iva, retefuente, reteiva) con sus valores.

---

## Sección 6: Reglas de negocio

Las reglas de negocio se organizan en 8 frentes (44 reglas):

| Frente | Alcance | Reglas |
|--------|---------|--------|
| 1. Integridad del borrador | Reglas que gobiernan la estructura, validez, concurrencia, autoridad de edición y advertencias del sistema (N1) | R01–R10, R40–R44 |
| 2. Traducción y resolución | Comportamiento de la traducción, resolución, re-emisión, gobernanza del aprendizaje y entrega (N1) | R11–R15, R35–R36 |
| 3. Trazabilidad | Unicidad de referencias, información de la contabilización, referencia a hecho relacionado y vista de estado (N1) | R16–R20 |
| 4. Rechazo del destino | Comportamiento cuando el sistema contable de destino rechaza un borrador (N1) | R21–R22 |
| 5. Integridad del asiento contable | Reglas que gobiernan el asiento contable (N2) | R23–R25 |
| 6. Periodos contables | Apertura, cierre, validación de periodo y estados (N2) | R26–R31 |
| 7. Multi-libro y equivalencia | Libros contables, planes de cuentas y equivalencia (N2) | R32–R34 |
| 8. Vigencia de configuración | Política de aplicación de cambios en configuración sobre borradores nuevos y existentes (N1/N2) | R37–R39 |

### 1. Integridad del borrador (N1)

Las reglas de integridad se validan sobre el borrador como condición para enviarlo al sistema contable de destino.

| ID | Regla | Configurable |
|----|-------|-------------|
| R01 | **Balance obligatorio:** Un borrador solo puede enviarse al sistema contable de destino cuando la suma de débitos es igual a la suma de créditos. | No |
| R02 | **Solo cuentas auxiliares:** Las partidas del borrador solo pueden asignarse a cuentas auxiliares (posteables). Las cuentas maestras (agrupadoras) no reciben movimientos — existen exclusivamente para presentación en informes. | No |
| R03 | **Moneda única:** Todas las partidas de un borrador se registran en una única moneda. Si el hecho económico de origen involucra moneda extranjera, el consumidor envía los valores ya convertidos a la moneda de operación de la empresa. | No |
| R04 | **Tercero y unidad organizacional con herencia:** La obligatoriedad del tercero y de la unidad organizacional en las partidas del borrador se define por tipo de cuenta (gasto, costo, ingreso, CxP/CxC, activo, banco, patrimonio) como configuración incluida en el producto. Cada cuenta auxiliar puede sobreescribir la configuración de su tipo cuando necesita un comportamiento diferente. El análisis completo y las configuraciones por tipo de cuenta están documentados en `anexo-obligatoriedad-tercero-unidad-organizacional.md`. | Sí (por tipo de cuenta, sobreescribible por cuenta auxiliar) |
| R05 | **Mínimo dos partidas:** Un borrador debe tener al menos dos partidas — un débito y un crédito. | No |
| R06 | **Valor mayor a cero:** Toda partida del borrador debe tener un valor mayor a cero en débito o crédito. No se permiten partidas en cero. | No |
| R07 | **Datos maestros activos:** Las cuentas auxiliares, los terceros y las unidades organizacionales tienen un estado activo o inactivo. Un borrador solo puede usar datos maestros en estado activo. Los asientos contables ya generados con datos que posteriormente pasaron a inactivo se conservan intactos — la inactivación no afecta el histórico, solo impide su uso en nuevos registros. | No |
| R08 | **Documento fuente según tipo de transacción:** La configuración de cada tipo de transacción contable define si el documento fuente es obligatorio en el borrador. Por ejemplo, las causaciones de ingreso por factura de venta requieren documento fuente; los ajustes manuales del contador no lo requieren. | No |
| R09 | **Borradores de consumidores no se descartan:** Cuando un borrador fue generado a partir de un hecho económico de un sub-dominio consumidor, no puede descartarse. El hecho económico ya ocurrió y debe contabilizarse. El borrador permanece pendiente hasta que el contador lo complete. | No |
| R10 | **Borradores manuales sí se descartan:** Los borradores creados directamente por el contador (ajustes, reclasificaciones) pueden descartarse mientras estén en estado pendiente. No hay un hecho económico externo que los respalde. | No |
| R40 | **Concurrencia en el borrador:** Un borrador solo puede ser intervenido por un usuario a la vez. Si dos usuarios intentan modificar el mismo borrador simultáneamente, el segundo intento se rechaza. | No |
| R41 | **Edición y entrega son mutuamente excluyentes:** No puede coexistir una edición abierta del borrador con un intento de entrega al destino. El borrador se entrega solo cuando está resuelto y sin edición en curso. | No |
| R42 | **Cambio de sistema contable destino requiere consistencia total:** El sistema contable de destino no puede cambiarse mientras existan entregas en curso (ENVIADO) o borradores pendientes que hayan sido rechazados por el destino actual. Todas las entregas deben estar finalizadas (aceptadas o rechazadas) y todos los borradores rechazados deben resolverse y entregarse al destino actual antes de permitir el cambio. | No |
| R43 | **Autoridad de N1 sobre el borrador y categorización de campos:** N1 tiene autoridad plena sobre el borrador una vez que lo recibe. El contador puede modificar cualquier campo en estado pendiente sin bloqueo. Los campos se categorizan según su impacto: (a) *Corrección contable natural:* cuenta contable, tercero, unidad organizacional — son ajustes propios de la operación contable que no alteran el hecho económico de origen. (b) *Campos que afectan el hecho económico:* valor, moneda, documento fuente, agregar/eliminar partidas — modificar estos campos puede hacer que el borrador deje de reflejar fielmente el hecho económico emitido por el consumidor. | No |
| R44 | **Advertencia del sistema al modificar campos que afectan el hecho económico:** Cuando el contador modifica un campo que afecta el hecho económico (valor, moneda, documento fuente, agregar/eliminar partidas), el sistema advierte que la mejor práctica es solicitar al consumidor la re-emisión del hecho [R14] o la generación de un nuevo hecho económico (devolución parcial o total) si aplica. La advertencia no bloquea la operación — el contador decide si continúa con la edición o solicita la corrección al consumidor. La advertencia no aplica a borradores manuales. | No |

### 2. Traducción y resolución (N1)

| ID | Regla | Configurable |
|----|-------|-------------|
| R11 | **Toda traducción produce un borrador:** El resultado de traducir un hecho económico es siempre un borrador. Si el sistema logra resolver todas las cuentas y el borrador balancea, se entrega automáticamente al Servicio de Entrega. Si quedan cuentas sin resolver, el borrador queda pendiente para que el contador lo complete. | No |
| R12 | **Aprendizaje progresivo:** Cada vez que el contador resuelve una cuenta en un borrador pendiente, el sistema aprende esa decisión. En el futuro, cuando se presente la misma combinación (mismo tipo de componente, misma clasificación, misma empresa), el sistema resolverá automáticamente sin intervención. El analista contable puede convertir un aprendizaje en una regla formal de derivación cuando quiere que sea explícita e inmutable. | No |
| R13 | **Sistema contable de destino configurable:** El Servicio de Entrega envía los borradores resueltos al sistema contable de destino configurado para la empresa. El destino puede ser N2 (sistema contable propio), SincoA&F, Siigo, Alegra u otro sistema contable externo. El contrato de líneas de traducción es el mismo independientemente del destino. | Sí (por empresa) |
| R14 | **Re-emisión controlada por el consumidor:** El consumidor emite un hecho económico con una referencia de origen única. Un hecho económico genera un solo borrador. Si el consumidor re-emite con la misma referencia y el borrador está en estado pendiente, N1 reemplaza toda la información del borrador con los nuevos datos (partidas, fecha, moneda, tipo de transacción, documento fuente y toda la información de las líneas de traducción). Si el borrador ya no está en estado pendiente (fue resuelto, entregado o descartado), la re-emisión se rechaza. | No |
| R15 | **Consecuencias del reemplazo:** Cuando un borrador se reemplaza por re-emisión del consumidor, toda la información anterior se sustituye por la nueva. Las resoluciones de cuentas realizadas por el contador sobre el borrador anterior se pierden. Los datos anteriores quedan registrados para trazabilidad. | No |
| R35 | **Invalidación de aprendizaje:** El analista contable puede invalidar un aprendizaje erróneo. Un aprendizaje invalidado no se aplica a futuros borradores. La invalidación no afecta borradores ya resueltos con ese aprendizaje. | No |
| R36 | **Trazabilidad del nivel de resolución:** Cada resolución de cuenta en un borrador debe ser trazable a su nivel: regla formal (Nivel A), aprendizaje del sistema (Nivel C), inferencia inteligente confirmada (Nivel B) o intervención manual puntual. El nivel queda registrado por partida. | No |
| R45 | **Validación contractual del motor de traducción:** El motor valida que cada línea de traducción tenga un rol disponible en la plantilla del tipo de transacción recibido. Si una o más líneas no encajan, el motor rechaza el hecho económico completo y no crea borrador. Estos rechazos también ocurren cuando el tipo de transacción no tiene plantilla configurada o cuando se recibe una referencia de origen duplicada que ya no es reemplazable. En todos estos casos, el motor notifica al consumidor con un motivo estructurado para que decida la acción correctiva (re-emitir corregido, escalar al equipo de producto si el motivo refleja un defecto de configuración del producto, o reconocer un duplicado en caso de idempotencia). Estos rechazos no son hechos contables, no aparecen en la consola de contabilización y no requieren intervención del contador. La durabilidad del hecho económico hasta que sea procesado correctamente es responsabilidad del sub-dominio consumidor emisor. | No |
| R47 | **Grupo del PUC esperado por componente del rol:** Cada componente que alimenta un rol de la plantilla de asiento declara los grupos del PUC (prefijos de código de cuenta, de longitud variable — clase, grupo o cuenta) a los que debe pertenecer la cuenta resuelta. La inferencia automática (Nivel B) solo sugiere cuentas dentro de esos grupos; los Niveles A (regla) y C (aprendizaje) resuelven la cuenta explícita o aprendida sin acotar. El grupo vive en el componente porque un rol agrupa varios tipos de componente que caen en grupos distintos (ej: el rol RETENCION cubre `retefuente`→`2365` y `reteiva`→`2367`); la contrapartida, que carece de tipo de componente, declara su grupo a nivel del rol. El detalle de la cuenta exacta lo determina la cadena de resolución. | No |
| R48 | **Narración del borrador y de las partidas:** El borrador admite una descripción general del hecho económico (a nivel de encabezado) y una descripción de concepto por partida. Ambas las envía el consumidor; si no envía la descripción general, el borrador queda sin ella. La descripción de concepto solo se asigna a las partidas cuyo componente está marcado para llevarla en la plantilla de asiento (los componentes de concepto de negocio — gasto, devolución de concepto, anticipo — la llevan; los de impuesto y retención no, porque la cuenta ya es autodescriptiva). El detalle de la cuenta no reemplaza esta narración: una identifica la cuenta, la otra explica el movimiento. | No |
| R49 | **Herencia del rol en la partida:** Cada partida del borrador conserva el rol que tenía en la plantilla de asiento (gasto, impuesto, retención, contrapartida). Como la cuenta contable se resuelve dinámicamente, el rol es la marca confiable del tipo de partida. El rol se entrega al sistema contable de destino para que pueda identificar las partidas tributarias (impuestos y retenciones) y darles el tratamiento fiscal correspondiente — necesario, por ejemplo, para la entrega a SincoA&F. El requisito específico de cada destino se concreta al implementar su adaptador. | No |

### 3. Trazabilidad (N1)

| ID | Regla | Configurable |
|----|-------|-------------|
| R16 | **Unicidad del hecho económico:** Cada hecho económico que un consumidor envía se identifica con una referencia de origen única. Si N1 recibe un hecho económico con una referencia que ya existe y el borrador está en estado pendiente, se aplica R14 (reemplazo). Si el borrador ya no está en estado pendiente, se ignora la solicitud. Esto protege contra duplicados en borradores ya procesados. | No |
| R17 | **Información de la contabilización:** Cuando el sistema contable de destino acepta un borrador, el Servicio de Entrega informa el resultado con la referencia asignada por el destino (consecutivo, comprobante u otro identificador). Los consumidores interesados actualizan su referencia al asiento. No se informan estados intermedios del borrador — solo el resultado final de la contabilización. | No |
| R18 | **Trazabilidad bidireccional:** Todo sub-dominio que envíe hechos económicos conserva la referencia al asiento contable resultante en el destino. N1 conserva la referencia al hecho económico de origen. Ambas referencias permiten la trazabilidad completa entre la transacción de negocio y su registro contable. | No |
| R19 | **Referencia a hecho económico relacionado:** Las líneas de traducción pueden incluir opcionalmente una referencia al hecho económico relacionado (ej: una devolución referencia a la OXP original). N1 conserva esta referencia en el borrador y la propaga al destino. Un hecho original puede tener múltiples hechos relacionados (varias devoluciones), pero cada hecho relacionado referencia a un solo hecho original. | No |
| R20 | **Consola de contabilización:** N1 ofrece una consola que permite consultar el estado de cualquier hecho económico, su historial de transiciones e intentos de entrega, y ejecutar acciones operativas. La definición funcional completa (estados visibles, campos, acciones) está en la Sección 5. | No |

### 4. Rechazo del destino (N1)

| ID | Regla | Configurable |
|----|-------|-------------|
| R21 | **Rechazo del sistema contable de destino:** Si el destino rechaza un borrador resuelto (periodo cerrado, cuenta que no existe u otra razón), el borrador queda visible en la consola de contabilización con el motivo del rechazo. El contador decide la acción correctiva: corregir la causa en el destino, modificar el borrador o reintentar el envío. | No |
| R22 | **Corrección y reenvío:** Un borrador rechazado vuelve a pendiente. El contador puede corregir las cuentas si el motivo lo requiere, o resolverlo nuevamente sin cambios para que se reenvíe al destino. | No |

### 5. Integridad del asiento contable (N2)

| ID | Regla | Configurable |
|----|-------|-------------|
| R23 | **Inmutabilidad:** Un asiento contable nunca se modifica ni se elimina. Cualquier corrección se realiza mediante un nuevo asiento contable con las partidas invertidas que referencia al original. Requisito legal (Decreto 2649, Art. 124). | No |
| R24 | **Numeración única:** El consecutivo asignado a cada asiento contable no puede repetirse dentro de la misma combinación de dimensiones de segmentación (empresa, tipo de comprobante, periodo, sucursal). | No |
| R25 | **Marca de ajuste:** Los asientos contables de ajuste de cierre se identifican con una marca explícita. Esto permite generar reportes del periodo con y sin ajustes de cierre por separado, sin necesidad de periodos de ajuste adicionales. | No |

### 6. Periodos contables (N2)

| ID | Regla | Configurable |
|----|-------|-------------|
| R26 | **Creación automática de periodos:** Al confirmar la fecha de inicio de operación contable, el sistema crea automáticamente los periodos restantes del año en curso. El periodo corriente queda abierto y los futuros cerrados. Para los años siguientes, el sistema informa al analista contable cuando los periodos disponibles se agotan; al confirmar la creación, los nuevos periodos nacen cerrados. | No |
| R27 | **Apertura mes a mes:** El analista contable abre los periodos según la operación de la empresa. Los periodos no se abren automáticamente. | No |
| R28 | **Cierre por niveles:** El periodo tiene un estado general (abierto o cerrado) y puede tener excepciones por tipo de comprobante. El estado a nivel de tipo de comprobante prevalece sobre el estado general. Esto permite, por ejemplo, cerrar el periodo para la recepción de causaciones pero mantenerlo abierto para ajustes manuales. | No |
| R29 | **Advertencia al cierre:** Al cerrar un periodo, si existen borradores pendientes para ese periodo, el sistema advierte indicando la cantidad. El cierre no se bloquea — el contador confirma explícitamente que desea cerrar sabiendo que hay hechos económicos sin contabilizar. | No |
| R30 | **Validación de periodo al recibir un borrador:** Cuando N2 recibe un borrador resuelto cuya fecha corresponde a un periodo cerrado para el tipo de comprobante correspondiente, aplica una de dos opciones según la configuración de la empresa: (a) **Rechazo** (por defecto) — rechaza el borrador con el motivo "periodo cerrado" y el Servicio de Entrega lo gestiona según el Flujo 3; (b) **Redirección al mes siguiente** — el borrador se registra con la fecha del mes inmediatamente siguiente; si este también está cerrado, rechaza. La redirección solo avanza un mes. La fecha original del hecho económico se conserva en la referencia de origen para trazabilidad. | Sí (por empresa) |
| R31 | **Cierre definitivo** *(fase futura):* Un periodo en estado de cierre definitivo no puede reabrirse bajo ninguna circunstancia. Diseñado como control para periodos ya auditados y aprobados. | No |

### 7. Multi-libro y equivalencia (N2)

| ID | Regla | Configurable |
|----|-------|-------------|
| R32 | **Un plan de cuentas por libro:** Cada libro contable tiene exactamente un plan de cuentas (PUC) asociado. Un mismo PUC puede ser compartido por varios libros. La equivalencia entre libros que usan PUCs diferentes se configura mediante un mapeo cuenta a cuenta entre los planes de cuentas. | No |
| R33 | **Equivalencia congelada:** La equivalencia entre planes de cuentas se resuelve y congela en el momento en que se registran las entradas en los reportes contables. Los cambios posteriores en la configuración de equivalencia no afectan las entradas ya registradas — preservando la correctitud del histórico contable. | No |
| R34 | **Asientos específicos de un libro:** Cuando un asiento contable aplica solo a un libro (ej: ajuste bajo NIIF que no tiene equivalente fiscal), se registra directamente en ese libro. Los reportes contables de otros libros no lo reflejan — no se genera equivalencia. | No |
| R46 | **Arquitectura predeterminada moderna:** Una empresa típica al onboardear opera con un único plan de cuentas estructurado bajo el marco contable NIIF, sobre el cual se configuran dos libros contables predeterminados (Principal y Fiscal). Los libros comparten el mismo PUC; las diferencias entre tratamientos contables (NIIF vs ajustes fiscales) se modelan como asientos específicos del libro fiscal [R34], no como planes de cuentas paralelos. La empresa puede activar marcos contables adicionales y crear PUCs alternos solo cuando la operación lo requiera (transición a NIIF, sectores regulados con PUC sectorial obligatorio, grupos empresariales con consolidación, PUC fiscal alterno). La creación de marcos custom requiere usuario con permiso especial. Justificación detallada en `anexo-marco-contable-y-arquitectura-puc.md`. | No |

### 8. Vigencia de configuración (N1/N2)

| ID | Regla | Configurable |
|----|-------|-------------|
| R37 | **Configuración aplica a borradores nuevos:** Los cambios en configuración (reglas de derivación, plan de cuentas, plantillas de asiento) aplican inmediatamente para los borradores nuevos. Los borradores existentes (pendientes, resueltos, rechazados) conservan la resolución con la que fueron traducidos. | No |
| R38 | **Trazabilidad de configuración aplicada:** Debe poder conocerse con qué configuración fue resuelto cada borrador (qué regla, qué aprendizaje o qué inferencia determinó cada cuenta). El mecanismo específico se define al implementar. | No |
| R39 | **Cuenta inactiva en aprendizaje o regla:** Si una cuenta utilizada por un aprendizaje o regla de derivación se inactiva en el plan de cuentas, el aprendizaje o regla permanece registrado pero no se aplica a nuevos borradores. Los borradores ya resueltos con esa cuenta no se afectan. | No |

---

## Sección 7: Qué está dentro y fuera del alcance

### Dentro del alcance del sub-dominio de Contabilidad

Las fases F1 y F2 se definen en la Sección 8. La pertenencia al dominio no cambia — la columna indica el objetivo de implementación. F1 corresponde a N1 (Motor de Traducción) con SincoA&F como sistema contable de destino. F2 corresponde a N2 (Sistema contable propio).

| Área | Descripción | Fase |
|------|-------------|:----:|
| Motor de traducción | Recepción de líneas de traducción de los sub-dominios consumidores y generación de borradores contables mediante plantillas de asiento y cadena de resolución de cuentas. | F1 |
| Borrador contable | Creación, resolución de cuentas pendientes y entrega al Servicio de Entrega. | F1 |
| Plantillas de asiento | Estructura universal de roles (débitos/créditos) por tipo de transacción contable. Incluidas en el producto. | F1 |
| Cadena de resolución de cuentas | Tres niveles: reglas manuales del analista, aprendizaje del sistema e inferencia inteligente. | F1 |
| Reglas de derivación | Configuración que determina qué cuenta corresponde a cada combinación de dimensiones del hecho económico. Incluidas en el producto con posibilidad de extensión. | F1 |
| Plan de cuentas (PUC) | Gestión de la jerarquía de cuentas maestras y auxiliares, asociado a un marco contable. Necesario para que N1 resuelva cuentas durante la traducción. | F1 |
| Marco contable | Catálogo por empresa que identifica formalmente el esquema bajo el cual se diseña cada PUC. Marco NIIF predeterminado al onboardear; marcos custom (consolidación, sectoriales, fiscal alterno) por usuario con permiso especial. | F1 |
| Asistente de onboarding del PUC | Capacidad transversal del producto (vive en `compartido/asistente-onboarding/`) que guía la carga inicial del PUC con sugerencias automáticas, aprendizaje progresivo e historial auditable. El caso PUC es el primero del patrón transversal. | F1 |
| Servicio de Entrega | Componente de N1 que toma los borradores resueltos y los entrega al sistema contable de destino configurado. Gestiona la comunicación con el destino e informa el resultado. | F1 |
| Adaptador SincoA&F | Adaptador del Servicio de Entrega para SincoA&F. Entrega borradores resueltos en el formato que este sistema espera. | F1 |
| Consola de contabilización | Vista consolidada que muestra el estado de cada hecho económico: pendiente, resuelto, aceptado por el destino, rechazado con motivo o descartado (solo manuales). | F1 |
| Trazabilidad bidireccional | Referencia de origen única en N1. Información de la contabilización para que los consumidores actualicen su referencia al asiento. | F1 |
| Asiento contable | Registro inmutable con comprobante, periodo, partidas, libro y referencia al hecho económico de origen. | F2 |
| Libros contables | Configuración de libros predeterminados (Principal y Fiscal) sobre el PUC NIIF de la empresa. Libros adicionales (Gerencial, Consolidación, sectoriales) creados bajo demanda según las necesidades. | F2 |
| Equivalencia entre PUCs | Mapeo cuenta a cuenta entre planes de cuentas de libros diferentes. Congelamiento de la equivalencia al momento de registrar. | F2 |
| Periodos contables | Apertura, cierre y reapertura de periodos. Cierre por niveles (general + por tipo de comprobante). Creación automática al inicio de operación. Validación de periodo con opción de redirección al mes siguiente. | F2 |
| Numeración contable | Secuencias por tipo de comprobante con dimensiones de segmentación configurables (empresa, tipo, periodo, sucursal). Incluidas en el producto. | F2 |
| Auxiliar contable | Detalle de cada partida por libro de presentación, con equivalencia congelada, documento fuente y referencia de origen. Base para reportes de detalle. | F2 |
| Saldos contables | Totales agrupados por libro de presentación, cuenta, tercero, unidad organizacional y periodo. Base para reportes de saldos. | F2 |
| Asientos manuales | Creación de asientos directamente por el contador (ajustes, reclasificaciones) en un libro específico. | F2 |
| Anulación de asientos | Generación de asiento inverso referenciando al original. | F2 |
| Multi-libro y reportes | Reportes contables por libro con equivalencia congelada: auxiliar por cuenta, auxiliar por tercero, libro diario, balance de prueba, estados financieros. | F2 |
| Cierre definitivo de periodos | Estado de periodo irreversible para periodos ya auditados y aprobados. | F2+ |

### Fuera del alcance del sub-dominio de Contabilidad

| Área | Descripción | Observación |
|------|-------------|-------------|
| Numeración fiscal por resolución | Numeración autorizada por entes fiscales (resolución DIAN, NCF de DGII, CFDI del SAT) para documentos como facturas de venta. | Responsabilidad del sub-dominio emisor del documento (CXC, Facturación). N1 recibe el documento fuente como referencia pero no gestiona la numeración fiscal. |
| Gestión de terceros | Creación, actualización e inactivación de terceros (proveedores, clientes, empleados). | Responsabilidad del sub-dominio de Terceros. N1 recibe la referencia del tercero en las líneas de traducción. |
| Gestión de unidades organizacionales | Creación, jerarquía, tipos, codificación y reestructuración de las unidades organizacionales de la empresa. | Responsabilidad del sub-dominio de Estructura Organizacional. N1 recibe la unidad organizacional en las líneas de traducción. |
| Cálculo tributario | Determinación de tributos, tarifas y desglose fiscal de las transacciones. | Responsabilidad del sub-dominio de Impuestos. Los tributos llegan como componentes dentro de las líneas de traducción. |
| Distribución por unidad organizacional | Distribución de valores de un hecho económico entre unidades organizacionales. | Responsabilidad del sub-dominio consumidor. N1 recibe los valores ya distribuidos en las líneas de traducción. |
| Procesamiento de pagos | Ejecución de desembolsos, movimientos bancarios y conciliación de pagos. | Responsabilidad de Tesorería / SincoA&F. |
| Reportes de información fiscal | Generación de exógena DIAN, formatos DGII, certificados tributarios. | Responsabilidad del sub-dominio de Impuestos. |
| Datos base del ERP | Catálogos de países, ciudades, monedas, tipos de documento de identidad, tasas de cambio. | Responsabilidad de la plataforma de datos base del ERP. |
| Corrección post-entrega | Modificación de un borrador que ya fue entregado al destino o de un asiento ya contabilizado. | Si el borrador ya fue entregado, la corrección se resuelve con un nuevo hecho económico del consumidor (devolución, nota crédito). N1 no soporta reemplazo de borradores ya entregados. |
| Adaptadores para otros sistemas contables | Adaptadores para Siigo, Alegra u otros sistemas contables externos diferentes a SincoA&F. | F1 solo incluye el adaptador para SincoA&F. Los adaptadores adicionales se construyen según demanda comercial. |

### Dependencias externas

| Dependencia | Descripción | Impacto en Contabilidad |
|-------------|-------------|------------------------|
| **Sub-dominio de Terceros** | Fuente de verdad de la identificación y estado activo/inactivo de terceros. | N1 valida que el tercero esté activo al crear borradores (R07). |
| **Sub-dominio de Estructura Organizacional** | Fuente de verdad de las unidades organizacionales, su jerarquía, tipos y estado activo/inactivo. | N1 recibe la unidad organizacional en las líneas de traducción y la valida como activa (R07). |
| **Sub-dominio de Impuestos** | Motor de cálculo tributario. Perfil tributario de entidades fiscales. | No hay dependencia directa. Los tributos llegan como componentes de las líneas de traducción desde los consumidores. |
| **SincoA&F** | Sistema contable externo de destino en F1. | El Servicio de Entrega envía borradores resueltos a SincoA&F. SincoA&F asigna numeración, gestiona periodos y persiste los asientos. |
| **Plataforma: Datos base** | Catálogos de países, monedas, tipos de documento. | N1 consume moneda (para el borrador) y tipos de documento (para la identificación de terceros en las partidas). |

---

## Sección 8: Estrategia de implementación por fases

### Fase 1 (F1) — Motor de Traducción con SincoA&F como destino

F1 entrega N1 completo: motor de traducción, Servicio de Entrega con adaptador SincoA&F y consola de contabilización. Los sub-dominios consumidores (OXP, CXC) emiten líneas de traducción, N1 las traduce a borradores y el Servicio de Entrega los entrega a SincoA&F. SincoA&F sigue siendo el sistema contable — gestiona periodos, numeración, libros y reportes.

| Capacidad | Descripción |
|-----------|-------------|
| Motor de traducción | Recepción de líneas de traducción, aplicación de plantillas de asiento y cadena de resolución de cuentas. |
| Borrador contable | Creación con estados pendiente, resuelto y descartado (solo manuales). Resolución de cuentas por el contador. |
| Plantillas de asiento | Estructura universal de roles por tipo de transacción contable. Incluidas en el producto. |
| Cadena de resolución de cuentas | Tres niveles: reglas manuales, aprendizaje del sistema e inferencia inteligente. |
| Reglas de derivación | Incluidas en el producto con posibilidad de extensión por el analista contable. |
| Plan de cuentas (PUC) | Gestión de la jerarquía de cuentas maestras y auxiliares para la traducción. |
| Servicio de Entrega | Entrega de borradores resueltos al sistema contable de destino configurado. Gestión de la comunicación con el destino e información del resultado. |
| Adaptador SincoA&F | Adaptador que entrega borradores resueltos en el formato que SincoA&F espera y recibe la referencia (consecutivo) que SincoA&F retorna. |
| Consola de contabilización | Capacidad operativa central de N1. Permite operar borradores pendientes y rechazados, consultar el estado y resultado de entrega de cada hecho económico, navegar al origen y al destino, y ejecutar acciones correctivas. Sirve al contador, soporte y consumidores. Definición funcional completa en Sección 5. |
| Trazabilidad bidireccional | Referencia de origen única. Información del resultado de la contabilización a los consumidores. |

**Nota sobre la configuración del destino:** La elección del sistema contable de destino (SincoA&F, Siigo, Alegra, N2) es una decisión de administración del sistema, no de operación contable. En F1, el destino es SincoA&F.

#### Criterio de éxito de la Fase 1

1. Un sub-dominio consumidor (OXP) puede emitir líneas de traducción y N1 genera un borrador contable correctamente.
2. Un borrador con cuentas sin resolver queda pendiente y el contador puede completarlo desde la consola de contabilización.
3. Un borrador resuelto se entrega automáticamente a SincoA&F y SincoA&F retorna el consecutivo asignado.
4. Si SincoA&F rechaza un borrador (periodo cerrado, cuenta que no existe), el motivo queda visible en la consola y el contador puede corregir y reintentar.
5. El consumidor (OXP) se entera del resultado de la contabilización y actualiza su referencia al asiento en SincoA&F.
6. El aprendizaje del sistema funciona: la segunda vez que se presenta la misma combinación de dimensiones, el sistema resuelve automáticamente sin intervención del contador.
7. La consola muestra el estado actual de cada hecho económico con visibilidad operativa completa (estado del borrador + resultado de entrega).
8. Desde la consola se puede navegar al hecho económico origen y a la referencia del destino.
9. El historial de intentos de entrega (rechazos previos, reenvíos) es consultable desde la consola.
10. Las acciones operativas (resolver cuentas, reintentar entrega, descartar manual) se ejecutan desde la consola.

### Fase 2 (F2) — Sistema contable propio

**Nivel de madurez:**
- **F1:** Definición operativa suficiente para diseño y desarrollo.
- **F2:** Definición arquitectónica y funcional en consolidación progresiva. Las capacidades se identificaron para delimitar qué NO pertenece a F1, pero su especificación de detalle se completa cuando se inicie su construcción. Varios puntos de operación avanzada (procesos automáticos de cierre, reclasificación, cierre definitivo) permanecen abiertos intencionalmente.

Las capacidades listadas a continuación representan una parte de la visión del sistema contable propio (N2). Las fases necesarias para habilitar N2 en su totalidad son más amplias que lo aquí descrito y requieren su propia planificación detallada.

| Capacidad | Descripción |
|-----------|-------------|
| Asiento contable | Registro inmutable con comprobante, periodo, partidas, libro y referencia al hecho económico de origen. |
| Libros contables | Configuración de libros predeterminados (Principal y Fiscal) sobre el PUC NIIF de la empresa. Libros adicionales (Gerencial, Consolidación, sectoriales) bajo demanda. |
| Equivalencia entre PUCs | Mapeo cuenta a cuenta. Congelamiento de la equivalencia al momento de registrar. |
| Periodos contables | Apertura, cierre por niveles (general + por tipo de comprobante), creación automática al inicio de operación. Validación de periodo con opción de redirección al mes siguiente. |
| Numeración contable | Secuencias por tipo de comprobante con dimensiones de segmentación configurables. Incluidas en el producto. |
| Auxiliar contable | Detalle de cada partida por libro de presentación. Base para reportes de detalle. |
| Saldos contables | Totales agrupados por libro de presentación, cuenta, tercero, unidad organizacional y periodo. Base para reportes de saldos. |
| Asientos manuales | Ajustes contables y reclasificaciones directamente en un libro específico. |
| Anulación de asientos | Asiento inverso referenciando al original. |
| Multi-libro y reportes | Reportes por libro: auxiliar por cuenta, auxiliar por tercero, libro diario, balance de prueba, estados financieros. |

### Fase 2+ — Mejoras futuras

| Capacidad | Descripción |
|-----------|-------------|
| Cierre definitivo de periodos | Estado de periodo irreversible para periodos ya auditados y aprobados. |
| Adaptadores adicionales | Adaptadores del Servicio de Entrega para Siigo, Alegra u otros sistemas contables externos, según demanda comercial. |

### Estrategia de transición

La transición de SincoA&F a N2 se realiza cambiando la configuración del destino en el Servicio de Entrega:

1. **F1 (operación inicial):** N1 opera con SincoA&F como destino. SincoA&F gestiona todo lo contable.
2. **F2 (activación de N2):** Se activa N2 como destino. El Servicio de Entrega redirige los borradores a N2 en vez de SincoA&F. Los consumidores no se enteran del cambio — siguen emitiendo las mismas líneas de traducción.

---

## Sección 9: Beneficios esperados

| # | Beneficio | Descripción |
|---|-----------|-------------|
| 1 | **Desacoplamiento de los módulos de negocio** | Los sub-dominios transaccionales (OXP, CXC, Tesorería, Nómina, ABR) dejan de conocer cuentas contables, centros de costo y naturalezas débito/crédito. Solo emiten hechos económicos en lenguaje de negocio. Un cambio en el plan de cuentas no requiere modificar ningún módulo transaccional. |
| 2 | **Punto único de reglas contables** | Las reglas de derivación, plantillas de asiento y configuración del PUC se gestionan en un solo lugar (N1). Se eliminan las reglas dispersas en cada módulo y la duplicación de catálogos de mapeo contable. |
| 3 | **Comercialización independiente de los módulos de negocio** | Los módulos de negocio (OXP, ABR, CXC) pueden venderse a clientes que ya tienen un sistema contable externo (Siigo, Alegra u otro). N1 actúa como adaptador entre los módulos y el sistema contable del cliente sin necesidad de adoptar un sistema contable propio. |
| 4 | **Aprendizaje progresivo del sistema** | Cada resolución de cuenta por parte del contador alimenta el aprendizaje del sistema. Con el tiempo, la intervención humana se reduce progresivamente — el sistema resuelve automáticamente la mayoría de los borradores sin intervención del contador. |
| 5 | **Usuarios operativos sin conocimiento contable** | Los usuarios de los módulos transaccionales trabajan exclusivamente con conceptos de negocio (clasificación del gasto, tipo de servicio, destino de negocio). No necesitan entender cuentas contables para operar. |
| 6 | **Adaptabilidad ante cambios normativos** | Adaptar el tratamiento contable de un tipo de transacción (nueva NIIF, cambio de plan de cuentas, nueva normativa tributaria) se resuelve ajustando las reglas de derivación y plantillas en N1. Los módulos transaccionales no se modifican. |
| 7 | **Trazabilidad completa** | Desde cualquier transacción de negocio se puede navegar hasta el asiento contable en el destino y viceversa, independientemente de cuál sea el sistema contable de destino. La consola de contabilización ofrece un panorama consolidado de todo el proceso. |
| 8 | **Transición progresiva** | La migración del sistema contable actual (SincoA&F) al sistema contable propio (N2) se realiza cambiando la configuración del destino en el Servicio de Entrega. Los consumidores no se ven afectados por el cambio — siguen emitiendo las mismas líneas de traducción. |
| 9 | **Multi-libro sin duplicación** *(N2)* | Un solo asiento contable puede reflejarse en múltiples libros (Principal, Fiscal, Gerencial u otros) sobre el mismo PUC, sin generar asientos duplicados ni controles de cruces entre libros. Las diferencias entre tratamientos contables se modelan como asientos específicos del libro correspondiente. La equivalencia entre PUCs se utiliza solo en casos excepcionales (transición de marcos, sectores regulados, consolidación). |
| 10 | **Reportes contables optimizados** *(N2)* | Dos fuentes de reportes especializadas: auxiliar contable para reportes de detalle y saldos contables para reportes agrupados. Cada una optimizada para su propósito, con la equivalencia de PUC congelada al momento de registrar para preservar la correctitud del histórico. |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Abril 2026 | Versión inicial. 9 secciones, 24 términos en glosario, 3 actores internos, 5 actores externos, 6 flujos, 44 reglas de negocio (8 frentes), 22 áreas dentro del alcance, 10 áreas fuera del alcance, 5 dependencias externas, 10 beneficios, 10 criterios de aceptación F1, estrategia de implementación por fases (F1 N1+SincoA&F, F2 N2). Resultado de: construcción iterativa y 3 rondas de revisión. |
| 1.1 | Mayo 2026 | Validación contractual del motor de traducción. Nueva regla R45 en el frente "Traducción y resolución (N1)" que formaliza el rechazo del motor cuando una línea de traducción no encaja en ningún rol de la plantilla, cuando el tipo de transacción no tiene plantilla configurada o cuando se recibe una referencia de origen duplicada no reemplazable. Total ahora 45 reglas. Estos rechazos son contractuales/técnicos, no contables, y no aparecen en la consola de contabilización; la durabilidad del hecho económico hasta procesamiento correcto es responsabilidad del sub-dominio consumidor emisor. Acompaña actualización de `modelo-dominio.md` v1.1 (nueva invariante I27, ampliación de pasos 1 y 2 del ServicioDeTraduccion, tabla de motivos estructurados, nueva sugerencia de implementación SI7). |
| 1.2 | Mayo 2026 | Arquitectura PUC + Libro + Marco contable y replanteamiento de libros predeterminados. Cambios: (1) Nuevo término "Marco contable" en el glosario (entrada 25) — total ahora 25 términos. (2) Glosario "Libro contable" (entrada 11) reformulado: el producto provee Principal y Fiscal predeterminados sobre el PUC NIIF; libros adicionales bajo demanda. (3) Flujo 1 paso 5 actualizado con la arquitectura predeterminada moderna. (4) Nueva regla R46 en el frente "Multi-libro y equivalencia (N2)" — arquitectura predeterminada moderna (un PUC NIIF, libros Principal y Fiscal sobre el mismo PUC, diferencias modeladas como asientos específicos del libro). Total ahora 46 reglas. (5) Capacidades F1 ampliadas con "Marco contable" y descripción del PUC actualizada. (6) Capacidad F2 "Libros contables" reformulada con predeterminados Principal/Fiscal. (7) Datos propios del sub-dominio actualizados con "Marcos contables" y nueva descripción de "Libros contables". Acompaña actualización de `modelo-dominio.md` v1.2 (nuevo agregado `MarcoContable` en sección 3.5, atributo `marcoContable` en `PlanDeCuentas`, atributo `tipo` del `LibroContable` como texto libre, invariantes I28-I32, decisión D11, premisa P5 actualizada, renumeración de secciones 3.6-3.19). Nuevo anexo: `anexo-marco-contable-y-arquitectura-puc.md`. |
| 1.3 | Mayo 2026 | Referencia al nuevo servicio compartido **Asistente de onboarding del PUC**. Cambios: (1) Nuevo término "Asistente de onboarding del PUC" en el glosario (entrada 26) — total ahora 26 términos. La capacidad vive en `compartido/asistente-onboarding/` y guía la carga inicial del PUC con sugerencias automáticas, aprendizaje progresivo e historial auditable. (2) Flujo 1 paso 2 actualizado: el cargue del PUC se realiza mediante el Asistente de onboarding. (3) Capacidades F1 ampliadas con "Asistente de onboarding del PUC" referenciando al servicio compartido. Este cambio NO modifica el modelo de Contabilidad — solo agrega referencias en el alcance. El Asistente de onboarding está documentado completamente en `compartido/asistente-onboarding/definicion-alcance.md` v1.0, `compartido/asistente-onboarding/modelo-dominio.md` v1.0 (4 agregados: PUCdeReferencia, ReglaDeRevisionPUC, AprendizajeOnboardingPUC, ProcesoOnboardingPUC con FSM) y `compartido/asistente-onboarding/casos/onboarding-puc.md` v1.0. |
| 1.4 | Mayo 2026 | Grupo del PUC esperado en la plantilla de asiento (issue #7). Cambios: (1) Nueva regla R47 en el frente "Traducción y resolución (N1)" — cada componente del rol declara los grupos del PUC (prefijos de longitud variable) que acotan la inferencia (Nivel B); total ahora 47 reglas. (2) Nuevo término "Grupo del PUC esperado" en el glosario (entrada 27) — total ahora 27 términos. Acompaña actualización de `modelo-dominio.md` v1.3 (nuevo VO `ComponenteDelRol` que reemplaza el atributo plano `tipoComponenteAsociado`, atributo `grupoPucEsperado`, decisión D12, ampliación de los pasos 3 y 4 del ServicioDeTraduccion) y de `anexo-ejemplo-plantilla-de-asiento.md` v1.1. Llenado del inventario completo de 42 plantillas y confirmación del grupo del `inc` pendientes de revisión por consultor contable. |
| 1.5 | Junio 2026 | Narración del borrador y de las partidas (issue #8). Cambios: (1) Nueva regla R48 en el frente "Traducción y resolución (N1)" — descripción general del borrador (encabezado) y descripción de concepto por partida, ambas enviadas por el consumidor; la de concepto solo se asigna a partidas cuyo componente la lleva. Total ahora 48 reglas. (2) Dos términos nuevos en el glosario: "Descripción del borrador" (entrada 28) y "Descripción de concepto" (entrada 29) — total ahora 29 términos. Acompaña actualización de `modelo-dominio.md` v1.4 (atributos `descripcion` en BorradorContable, `descripcionConcepto` en PartidaBorrador, `llevaDescripcionConcepto` en ComponenteDelRol, paso 4b del ServicioDeTraduccion, decisión D13) y del catálogo `datos-precargados/plantillas-de-asiento.*`. Depende de que OXP envíe estos textos en `LineaTraduccion` (issue #10). |
| 1.6 | Junio 2026 | Herencia del rol en la partida (issue #9). Cambios: (1) Nueva regla R49 en el frente "Traducción y resolución (N1)" — la partida conserva el rol de la plantilla y se entrega al destino para identificar partidas tributarias (caso SincoA&F). Total ahora 49 reglas. (2) Nuevo término "Rol de la partida" en el glosario (entrada 30) — total ahora 30 términos. (3) Salida al Servicio de Entrega ampliada con el rol por partida. Acompaña actualización de `modelo-dominio.md` v1.5 (atributo del rol renombrado de `nombre` a `rol` en `RolPartida`, nuevo atributo `rol` en `PartidaBorrador`, paso 5 del ServicioDeTraduccion, paso 3 de EntregaContable, decisión D14) y del catálogo `datos-precargados/plantillas-de-asiento.*`. Requisito específico de SincoA&F sobre impuestos se confirma al implementar el adaptador [PD1]. |
