# Definición de Alcance — Impuestos

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

El sub-dominio de Impuestos se define como el sistema transversal del ERP responsable de tres funciones: (1) mantener la configuración fiscal vigente (catálogo de tributos, tarifas, reglas de dependencia y régimen tributario por país), (2) resolver el cálculo tributario para cualquier transacción de gasto o ingreso que lo requiera, y (3) registrar cada hecho tributario como verdad propia del dominio para sostener auditorías, generar reportes estatutarios y emitir certificados tributarios ante terceros y entes reguladores.

### Contexto actual

El ERP actual maneja los impuestos de forma descentralizada. Cada módulo que produce transacciones con impacto fiscal (compras administrativas, compras de construcción, facturación, etc.) ha construido su propia lógica de cálculo tributario mediante funciones que reciben parámetros de entrada y retornan el resultado del cálculo.

No existe un estándar uniforme en el manejo del desglose fiscal: algunos módulos mantienen el detalle de los tributos aplicados a cada transacción, mientras que otros calculan los tributos y los expresan directamente en términos contables, perdiendo la trazabilidad del hecho tributario original.

Las tarifas y reglas tributarias se configuran de forma independiente en cada módulo mediante CRUDs propios. Adicionalmente, el sistema contable también mantiene sus propios niveles de tarifas. Esto genera múltiples puntos de actualización cuando la normativa cambia.

Los reportes de exógena ante la DIAN (formatos 1001, 1003, entre otros) y los certificados de ingresos y retenciones a proveedores se generan desde el sistema contable, mediante procesos que cruzan la información contable contra las configuraciones vigentes al momento de la generación. Se producen reportes tanto a nivel nacional como municipal.

### Problema actual

1. **Lógica tributaria fragmentada:** Cada módulo implementa su propia lógica de cálculo. No existe un motor centralizado, lo que genera duplicación de código, comportamientos inconsistentes entre módulos y mayor costo de mantenimiento.

2. **Configuración dispersa y propensa a desincronización:** Las tarifas y reglas tributarias se mantienen en CRUDs independientes por módulo e incluso en el sistema contable. Cuando la normativa cambia, la actualización debe replicarse manualmente en cada punto, generando ventanas donde un módulo opera con tarifas actualizadas y otro no.

3. **Pérdida de trazabilidad del hecho tributario:** Los módulos que expresan los tributos directamente en términos contables pierden el detalle del cálculo original (qué base gravable se usó, qué tarifa aplicó, qué regla lo determinó). Esto dificulta la justificación ante auditorías tributarias.

4. **Reportes construidos desde la contabilidad, no desde el origen:** Los exógenos y certificados se generan reconstruyendo la información fiscal a partir de los movimientos contables y la configuración vigente al momento de generación — no al momento de la transacción. Si las tarifas cambiaron entre ambos momentos, los reportes pueden no reflejar fielmente lo que ocurrió.

5. **Riesgo ante auditorías:** La combinación de lógica fragmentada, configuración dispersa y reportes reconstruidos crea un escenario donde justificar un cálculo tributario específico ante la DIAN requiere cruzar manualmente múltiples fuentes de información.

### Implementación inicial

- **País:** Colombia.
- **Frentes:** Gastos (sub-dominio OXP como primer consumidor) e ingresos (sub-dominio CXC cuando se construya).
- **Reportes:** Exógenos a nivel nacional y municipal, certificados de retención a proveedores.
- **Diseño:** Extensible para incorporar otros países y módulos en el futuro.

---

## Sección 2: Glosario de términos

Los términos de esta sección son **globales** — aplican independiente de la localización (país). Los catálogos específicos por país (qué tributos existen, qué regímenes hay, qué reportes se generan) se definen en anexos de localización.

| # | Término | Definición |
|---|---------|------------|
| 1 | **Tributo** | Carga fiscal obligatoria aplicable a una transacción. En el contexto de este sistema, se cubren dos categorías: impuestos y retenciones. Las retenciones tienen dos modalidades con comportamiento diferente: la retención (practicada por un agente retenedor sobre el pago a un tercero) y la autorretención (practicada por el mismo sujeto sobre sus propios ingresos). |
| 2 | **Impuesto** | Tributo de naturaleza aditiva que incrementa el valor total de la transacción. En la dirección fiscal de gastos, lo cobra el proveedor y lo paga la empresa. En la dirección fiscal de ingresos, lo cobra la empresa al cliente. Ejemplo: IVA, INC. |
| 3 | **Retención** | Tributo de naturaleza sustractiva mediante el cual un agente de retención descuenta un porcentaje del pago y lo consigna a la autoridad fiscal. En la dirección fiscal de gastos, la empresa retiene al proveedor. En la de ingresos, el cliente retiene a la empresa. Ejemplo: retención en la fuente, retención de IVA. |
| 4 | **Autorretención** | Mecanismo mediante el cual un tercero se practica a sí mismo la retención sobre sus propios ingresos. Aplica exclusivamente en la dirección fiscal de ingresos. En la dirección fiscal de gastos, el perfil tributario de autorretenedor del proveedor determina si la empresa debe o no practicarle retención. |
| 5 | **Dirección fiscal** | Sentido del hecho económico que determina el rol de la entidad fiscal emisora ante cada tributo. En la dirección de gastos (OXP/CXP) la entidad fiscal emisora es adquiriente y actúa como agente retenedor. En la dirección de ingresos (CXC) la entidad fiscal emisora es quien factura y puede ser sujeto de retención o responsable del impuesto. Equivale a "tax direction" (Dynamics 365) o "input/output tax" (SAP, Oracle). |
| 6 | **Base gravable** | Monto sobre el cual se aplica la tarifa de un tributo para determinar su valor. Según la jurisdicción y el tipo de tributo, la base gravable puede ser el valor de la transacción, una porción de este, o el valor calculado de otro tributo. También conocido como "base imponible" en otras legislaciones. |
| 7 | **Tarifa** | Porcentaje o valor fijo que se aplica sobre la base gravable para determinar el valor del tributo. Varía según el tipo de tributo, la clasificación tributaria y la jurisdicción. |
| 8 | **Cuantía mínima** | Umbral por debajo del cual un tributo no se aplica a la transacción. Si la base gravable no supera este valor, el cálculo se omite. Su existencia, valor y forma de expresión varían según la jurisdicción y el tipo de tributo. También conocido como "exemption threshold" o "minimum threshold" en contextos internacionales. |
| 9 | **Tributo padre (dependencia entre tributos)** | Tributo cuya existencia es prerequisito para calcular otro tributo derivado. El tributo hijo no puede existir sin el tributo padre. La base gravable del tributo hijo puede ser el valor calculado del tributo padre. Ejemplo: en Colombia la retención de IVA solo aplica si existe IVA; en República Dominicana la RITBIS solo aplica si existe ITBIS. |
| 10 | **Perfil tributario** | Conjunto de responsabilidades y atributos fiscales de una entidad (empresa o persona), independiente de su rol en la transacción. Tanto la entidad fiscal emisora como la entidad fiscal contraparte tienen su propio perfil tributario. Incluye atributos como: régimen tributario, responsabilidad de IVA, condición de autorretenedor, gran contribuyente, entre otros según la jurisdicción. Es uno de los parámetros de entrada del motor de cálculo tributario. Equivale a "Party Tax Profile" (Oracle Fusion Tax). |
| 11 | **Agente de retención** | Tercero designado u obligado por la autoridad fiscal a practicar retenciones sobre los pagos que realiza y consignarlas al Estado. Los criterios de designación varían según la jurisdicción. La condición de agente de retención es un atributo del perfil tributario. |
| 12 | **Registro tributario** | Registro inmutable que documenta un cálculo tributario realizado por el motor del sistema: qué tributos se aplicaron, sobre qué base gravable, con qué tarifa y a qué transacción origen. Es la fuente de verdad del sub-dominio de Impuestos y sustenta auditorías, reportes y certificados. En otros ERPs este concepto se representa como líneas de impuesto (tax lines) distribuidas dentro de cada documento transaccional; en este sistema se centraliza como registro propio del sub-dominio. |
| 13 | **Desglose fiscal** | Conjunto de tributos (impuestos y retenciones) calculados para un concepto de una transacción. Cada concepto dentro de un mismo documento puede tener un desglose fiscal diferente según su clasificación tributaria. Es el resultado que el motor de cálculo entrega al sub-dominio solicitante. Cada sub-dominio consumidor (OXP, CXC, etc.) almacena su propia copia para operar de forma autónoma. |
| 14 | **Régimen tributario** | Marco normativo bajo el cual un tercero cumple sus obligaciones tributarias en una jurisdicción. El régimen determina qué tributos aplican y bajo qué condiciones. Es un atributo del perfil tributario del tercero. Los regímenes disponibles varían según la localización. |
| 15 | **Jurisdicción** | Ámbito territorial donde una autoridad fiscal tiene potestad para gravar. Puede ser a nivel de país, región, estado, municipio o ciudad. Una misma transacción puede estar sujeta a tributos de múltiples jurisdicciones simultáneamente. Equivale a "Tax Jurisdiction" (SAP, Oracle, Dynamics) o "Nexus" (Avalara, Vertex). |
| 16 | **Certificado tributario** | Documento que certifica las retenciones practicadas a un tercero durante un período fiscal. Se emite como constancia al tercero retenido. Su formato, obligatoriedad y periodicidad varían según la jurisdicción. Equivale a "Withholding Tax Certificate" (SAP, Oracle) o "Form 1099" en EE.UU. |
| 17 | **Declaración tributaria** | Informe presentado ante una autoridad fiscal donde la empresa reporta sus propios impuestos y el valor a pagar o saldo a favor. Su estructura y periodicidad varían según la jurisdicción y el tipo de tributo. Equivale a "Tax Return" o "Tax Declaration". |
| 18 | **Reporte de información fiscal** | Informe presentado ante una autoridad fiscal que detalla las transacciones y retenciones realizadas a terceros. Su propósito es permitir a la autoridad el cruce de información para verificación y control. Puede generarse a nivel nacional o municipal. Equivale a "Information Return" (EE.UU.) o "SAF-T" (UE). |
| 19 | **Clasificación tributaria** | Categoría que agrupa bienes y servicios según el tratamiento tributario que reciben en una jurisdicción. El sub-dominio de Impuestos define y mantiene el catálogo de clasificaciones. Los sub-dominios consumidores (OXP, CXC, etc.) asignan a cada uno de sus conceptos la clasificación tributaria correspondiente. Las reglas de aplicación de tributos se configuran contra clasificaciones tributarias, no contra conceptos individuales. Equivale a "Tax Code" (SAP), "Tax Classification Code" (Oracle), "Item Sales Tax Group" (Dynamics 365) o "Product Tax Code" (Avalara, Vertex). |
| 20 | **Entidad fiscal emisora** | Parte de la transacción que origina el hecho económico a efectos tributarios. En el caso más común coincide con la empresa operadora, pero en escenarios como facturación a nombre de terceros (ej: inmobiliario, donde la emisora fiscal es el propietario) o mandante por proyecto (ej: construcción, donde la emisora fiscal es el dueño del proyecto), puede ser una entidad diferente. Es el sub-dominio consumidor quien determina qué entidad ocupa este rol según su contexto de negocio. Equivale a "First Party" (Oracle Fusion Tax) o "Legal Entity" (Dynamics 365). |
| 21 | **Entidad fiscal contraparte** | Parte opuesta de la transacción a efectos tributarios. En la dirección de gastos es el proveedor; en la de ingresos es el cliente. El motor de cálculo resuelve los perfiles tributarios de ambas entidades fiscales (emisora y contraparte) para determinar qué tributos aplican. Equivale a "Third Party" (Oracle Fusion Tax) o "Counterparty" (Dynamics 365). |
| 22 | **Contenido fiscal** | Conjunto de tributos, tarifas, bases mínimas, reglas de aplicación, dependencias y vigencias que el sistema provee como parte del producto para cada jurisdicción soportada. El contenido fiscal viene precargado para que el cliente inicie operación sin configurar el estándar fiscal del país — solo configura lo propio de su empresa (perfil tributario, excepciones). Cuando la normativa cambia, el contenido fiscal se actualiza sin intervención del cliente. Equivale a "Tax Content" (Avalara, Vertex, ONESOURCE). |

---

## Sección 3: Actores del sistema

### Actores humanos

| Actor | Descripción |
|-------|-------------|
| **Administrador fiscal** | Administra los perfiles tributarios de las entidades fiscales (emisoras y contrapartes) y configura excepciones o casos especiales no cubiertos por el contenido fiscal del producto. El contenido fiscal estándar (tributos, tarifas, reglas de aplicación y vigencias por jurisdicción) viene precargado y se actualiza como parte del producto, sin intervención de este actor. |
| **Analista tributario** | Consulta los registros tributarios para verificar y justificar cálculos ante revisiones internas o requerimientos de autoridades fiscales. |
| **Responsable de cumplimiento fiscal** | Solicita la generación de certificados tributarios, declaraciones y reportes de información fiscal por período. En organizaciones pequeñas, este rol y el de administrador fiscal pueden ser ejercidos por la misma persona (típicamente el contador). |

### Actores sistema

| Actor | Descripción |
|-------|-------------|
| **Sub-dominio consumidor** (OXP, CXC, etc.) | Solicita el cálculo tributario enviando el contexto de la transacción. Recibe el desglose fiscal y almacena su propia copia para operar de forma autónoma. |
| **Tercero** (proveedor, cliente) | Destinatario de certificados tributarios. No interactúa directamente con el sistema. |
| **Autoridad fiscal** (DIAN, DGII, DGI) | Destinataria de declaraciones y reportes de información fiscal. No interactúa directamente con el sistema — recibe los archivos generados. |

---

## Sección 4: Flujo principal

### Flujo 1 — Configuración fiscal (Administrador fiscal)

1. El sistema provee el **catálogo de clasificaciones tributarias** como parte del contenido fiscal del producto — categorías que agrupan bienes y servicios según su tratamiento tributario. El administrador fiscal puede extenderlo con clasificaciones adicionales cuando el contenido estándar no cubra un caso específico. Este catálogo es consumido por los sub-dominios (OXP, CXC, etc.) para asignar a cada uno de sus conceptos la clasificación tributaria que le corresponde.
2. Las **reglas de aplicación** vienen precargadas como parte del contenido fiscal: para cada combinación de clasificación tributaria + dirección fiscal + jurisdicción, el sistema define qué tributos aplican, con qué tarifa y bajo qué condiciones (cuantía mínima, dependencia de tributo padre, perfiles tributarios requeridos). El administrador fiscal puede agregar o ajustar reglas para cubrir excepciones no contempladas en el contenido estándar.
3. El administrador fiscal mantiene los **perfiles tributarios** de las entidades fiscales (emisoras y contrapartes) como datos propios del sub-dominio de Impuestos (régimen tributario, condición de autorretenedor, gran contribuyente, agente de retención, entre otros según la jurisdicción). Este es el principal dato que el cliente configura al iniciar operación. El sistema ofrece **carga asistida del perfil tributario**: a partir del número de identificación de la entidad fiscal, consulta fuentes oficiales de la autoridad fiscal de la jurisdicción correspondiente y extrae automáticamente los atributos tributarios disponibles (responsabilidades fiscales, régimen, estado, actividad económica). El administrador fiscal valida y confirma los datos extraídos. Los sub-dominios consumidores referencian a la entidad fiscal por su identificación (tipo y número de identificación); el sistema de impuestos resuelve el perfil tributario internamente.
4. Cada regla de aplicación tiene una **vigencia** — fecha desde la cual entra en vigor. Cuando la normativa cambia, el contenido fiscal se actualiza con una nueva versión de la regla sin eliminar la anterior. Los cálculos futuros usan la configuración vigente al momento de la transacción.
5. El administrador fiscal mantiene el catálogo de **formatos de presentación** exigidos por cada autoridad fiscal para los reportes de información fiscal, así como los requeridos para la emisión de certificados tributarios: estructura, campos requeridos y periodicidad. Estos formatos se actualizan cuando la autoridad fiscal modifica sus requerimientos.

### Flujo 2 — Cálculo tributario (flujo principal)

1. El **sub-dominio consumidor** (OXP, CXC) envía una solicitud de cálculo con el contexto de la transacción:
   - Dirección fiscal (gasto o ingreso)
   - Identificación de la entidad fiscal emisora (tipo y número de identificación)
   - Identificación de la entidad fiscal contraparte (tipo y número de identificación)
   - Ubicaciones tipificadas por rol: sede emisora, sede contraparte y, cuando aplique, lugar de ejecución (donde se presta el servicio, se entrega el bien, se ubica el inmueble o se ejecuta el proyecto)
   - Fecha de la transacción, moneda y tipo de cambio de referencia (si aplica)
   - Lista de conceptos, cada uno con su monto y su clasificación tributaria
2. El **motor de cálculo** resuelve internamente los perfiles tributarios de la entidad fiscal emisora y de la entidad fiscal contraparte a partir de sus identificaciones, y resuelve la jurisdicción fiscal para cada tributo aplicando las reglas de localización sobre las ubicaciones recibidas. Luego, para cada concepto:
   a. Determina qué tributos aplican según: clasificación tributaria + dirección fiscal + perfiles tributarios + jurisdicción resuelta + configuración vigente.
   b. Resuelve el orden de cálculo respetando dependencias entre tributos (tributo padre antes que tributo hijo).
   c. Para cada tributo aplicable: verifica cuantía mínima, determina base gravable y aplica tarifa.
   d. Retorna el **desglose fiscal propuesto** al sub-dominio consumidor.
3. El usuario revisa el desglose propuesto dentro del sub-dominio consumidor. Si necesita ajustar manualmente el desglose (quitar un tributo, agregar otro, modificar una tarifa), puede hacerlo en una o más iteraciones mientras la transacción no esté en estado confirmado. La trazabilidad de estos ajustes (qué se modificó, quién lo hizo y cuándo) es responsabilidad del sub-dominio consumidor.
4. Si el usuario modifica datos que afectan el contexto del cálculo (monto de un concepto, tercero, clasificación tributaria, ubicaciones), el sub-dominio consumidor envía una nueva solicitud de cálculo al motor (vuelve al paso 1). El desglose propuesto anterior se descarta y se reemplaza por el nuevo. Los ajustes manuales realizados previamente sobre el desglose no se conservan.
5. Cuando la **transacción se confirma** en el sub-dominio consumidor, este notifica al sistema de impuestos con el desglose definitivo (original o ajustado).
6. El sistema de impuestos **crea el registro tributario** como hecho definitivo, conservando como mínimo: el cálculo original del motor y el desglose confirmado. Si hubo ajustes manuales, el registro tributario lo indica para distinguir un cálculo automático de uno intervenido por el usuario.
7. El sub-dominio consumidor **almacena su propia copia** del desglose confirmado para operar de forma autónoma.

### Flujo 3 — Cumplimiento fiscal (Responsable de cumplimiento fiscal)

1. El responsable de cumplimiento fiscal solicita la generación de un entregable (reporte de información fiscal o certificado tributario) para un período fiscal.
2. El sistema consulta sus propios registros tributarios para el período solicitado.
3. Genera el entregable aplicando el formato fiscal configurado para el tipo de entregable correspondiente. Los formatos de salida se adaptan a los requerimientos de cada autoridad fiscal (XML, JSON, u otro que la autoridad exija). Cuando la autoridad fiscal dispone de herramientas intermedias de validación (como el prevalidador de la DIAN, que acepta Excel y genera XML), el sistema también permite generar el archivo en el formato de entrada de dicha herramienta. Los certificados tributarios se generan adicionalmente en formato legible (PDF) para el tercero destinatario.
4. Para certificados tributarios, el sistema gestiona la **entrega controlada** al tercero destinatario mediante el canal configurado (correo electrónico, portal u otro mecanismo disponible), registrando la trazabilidad de la entrega (a quién se envió, cuándo y por qué canal).

---

## Sección 5: Integraciones

### Integraciones de entrada

| Origen | Dato | Propósito |
|--------|------|-----------|
| **Sub-dominio consumidor** (OXP, CXC) | Solicitud de cálculo: dirección fiscal, identificación de la entidad fiscal emisora, identificación de la entidad fiscal contraparte, ubicaciones tipificadas por rol (sede emisora, sede contraparte, lugar de ejecución), fecha, moneda, conceptos con clasificación tributaria, montos y concepto de pago cuando lo requiera el factor de tarifa del tributo | Alimentar el motor de cálculo |
| **Sub-dominio consumidor** (OXP, CXC) | Confirmación de transacción con desglose definitivo (original o ajustado) | Crear el registro tributario |

### Integraciones de salida

| Destino | Dato | Propósito |
|---------|------|-----------|
| **Sub-dominio consumidor** (OXP, CXC) | Catálogo de clasificaciones tributarias | Para que cada sub-dominio clasifique sus conceptos |
| **Sub-dominio consumidor** (OXP, CXC) | Desglose fiscal propuesto | Resultado del cálculo tributario |
| **Tercero** (proveedor, cliente) | Certificado tributario | Entrega controlada con trazabilidad |
| **Autoridad fiscal** | Reportes de información fiscal | Cumplimiento fiscal |

### Datos propios del sub-dominio

El sistema de impuestos gestiona como datos propios los **perfiles tributarios** de las entidades fiscales — tanto emisoras como contrapartes — (régimen tributario, condición de autorretenedor, gran contribuyente, agente de retención, entre otros según la jurisdicción). Los sub-dominios consumidores no envían estos datos — envían la identificación (tipo y número) y el motor de cálculo resuelve los perfiles internamente.

### Nota sobre la traducción contable

La representación contable de los tributos (cuentas de IVA, retención, etc.) no es responsabilidad del sub-dominio de Impuestos. Cada sub-dominio consumidor posee su propia copia del desglose fiscal y es desde allí que se produce la traducción contable. El sub-dominio de Impuestos no tiene conocimiento de cuentas contables.

---

## Sección 6: Reglas de negocio

Las reglas se organizan en seis frentes funcionales del producto.

| Frente | Alcance | Reglas |
|--------|---------|:------:|
| 6.1 Clasificación tributaria | Catálogo de clasificaciones, reglas de aplicación por combinación, dependencias entre tributos, contenido fiscal | 5 |
| 6.2 Perfil tributario | Atributos fiscales de empresas y terceros, matriz de aplicabilidad por perfil | 3 |
| 6.3 Cálculo de tributos | Vigencia, determinación, fórmulas de cálculo, ajuste manual | 16 |
| 6.4 Registro tributario (huella fiscal) | Creación, inmutabilidad, contenido mínimo, vinculación con transacción origen | 5 |
| 6.5 Cumplimiento fiscal | Reportes de información fiscal, certificados tributarios, declaraciones, entrega controlada | 5 |
| 6.6 Integridad y validación (transversal) | Validaciones de entrada, requisitos de perfil, clasificación válida, frontera contable | 4 |

### 6.1 Clasificación tributaria

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R01** | **Clasificación tributaria obligatoria:** Todo tributo se configura contra clasificaciones tributarias, no contra conceptos individuales de los sub-dominios consumidores. Los sub-dominios consumidores asignan a cada uno de sus conceptos la clasificación tributaria que le corresponde del catálogo publicado por Impuestos. | No |
| **R02** | **Reglas de aplicación por combinación:** Las reglas de aplicación de tributos se definen para cada combinación de: clasificación tributaria + dirección fiscal + jurisdicción. Una misma clasificación tributaria puede tener reglas diferentes según la dirección fiscal (gasto vs. ingreso) y la jurisdicción. | No |
| **R03** | **Dependencia entre tributos:** Un tributo hijo solo puede configurarse si su tributo padre está definido en la misma combinación de reglas de aplicación. El sistema impide crear un tributo hijo sin su padre. Ejemplo: la retención de IVA solo puede existir si el IVA está configurado para esa combinación. | No |
| **R34** | **Jurisdicción multinivel:** Una clasificación tributaria puede tener tributos de diferentes niveles jurisdiccionales simultáneamente (nacional y municipal). Los tributos municipales (ej: ICA, RICA en Colombia) requieren tarifa a nivel de ciudad. El sistema soporta esta granularidad sin duplicar la clasificación tributaria. | No |
| **R38** | **Contenido fiscal como parte del producto:** El sistema provee precargados los tributos, tarifas, bases mínimas, reglas de aplicación, dependencias y vigencias para cada jurisdicción soportada. El cliente inicia operación sin configurar el estándar fiscal del país. El administrador fiscal solo configura los perfiles tributarios de sus entidades fiscales y las excepciones no cubiertas por el contenido estándar. | No |

### 6.2 Perfil tributario

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R04** | **Perfil tributario como dato propio:** Los perfiles tributarios de las entidades fiscales (emisoras y contrapartes) son datos propios del sub-dominio de Impuestos. Los sub-dominios consumidores no envían ni mantienen estos datos — envían la identificación (tipo y número) y el motor resuelve los perfiles internamente. | No |
| **R05** | **Perfil tributario a nivel de entidad:** El perfil tributario es un atributo de la entidad (empresa o persona), no de la transacción. La determinación de qué tributos aplican la resuelve el motor de cálculo automáticamente — no se configura manualmente por tercero. | No |
| **R35** | **Matriz de aplicabilidad por perfil:** La combinación del perfil tributario de la entidad fiscal emisora y el perfil tributario de la entidad fiscal contraparte determina qué tributos aplican en una transacción. Los atributos relevantes incluyen: régimen tributario, condición de autorretenedor, gran contribuyente, agente de retención, entre otros según la jurisdicción. El sistema evalúa esta combinación automáticamente — no se configura transacción por transacción. | No |

### 6.3 Cálculo de tributos

Este frente se subdivide en cuatro aspectos del proceso de cálculo: la vigencia de las reglas que lo alimentan, la determinación de qué tributos aplican, el cálculo numérico propiamente dicho y el ajuste manual posterior.

**Vigencia**

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R06** | **Vigencia temporal de reglas:** Cada regla de aplicación tiene una fecha de vigencia (desde cuándo entra en vigor). Cuando la normativa cambia, el contenido fiscal se actualiza con una nueva versión de la regla sin eliminar la anterior. El sistema conserva el histórico completo de versiones. | No |
| **R07** | **Fecha de la transacción rige el cálculo:** El motor de cálculo aplica la configuración vigente según la fecha de la transacción informada por el sub-dominio consumidor, no según la fecha en que se ejecuta el cálculo. Si una tarifa cambió entre ambos momentos, aplica la vigente al momento de la transacción. | No |
| **R08** | **No solapamiento de vigencias:** Para una misma combinación de clasificación tributaria + dirección fiscal + jurisdicción + tributo, no puede haber dos versiones de regla con rangos de vigencia que se solapen. | No |

**Determinación**

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R09** | **Determinación automática:** El motor de cálculo determina automáticamente qué tributos aplican evaluando los factores de la transacción: clasificación tributaria del concepto, dirección fiscal, perfiles tributarios de la entidad fiscal emisora y de la entidad fiscal contraparte, y jurisdicción. No requiere selección manual de tributos por parte del usuario. | No |
| **R10** | **Resolución por perfil tributario:** El perfil tributario de la entidad fiscal contraparte afecta la determinación: régimen tributario, condición de autorretenedor, gran contribuyente, agente de retención y demás atributos según la jurisdicción. Si la contraparte es autorretenedora, la entidad fiscal emisora no le practica retención (la retención la asume la contraparte sobre sus propios ingresos). | No |
| **R11** | **Dirección fiscal determina el rol:** En la dirección de gastos (OXP/CXP), la empresa es adquiriente y actúa como agente retenedor. En la dirección de ingresos (CXC), la empresa es emisora y puede ser sujeto de retención o responsable del impuesto. Los mismos tributos cambian de comportamiento según la dirección. | No |
| **R12** | **Exención y exclusión:** Si la combinación de factores de la transacción resulta en que un tributo no aplica (por régimen del tercero, por clasificación tributaria exenta, o por regla específica de la jurisdicción), el motor lo excluye del desglose. No se genera línea de tributo con tarifa 0% — simplemente no aparece en el resultado. | No |

**Cálculo**

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R13** | **Cálculo a nivel de concepto:** El desglose fiscal se calcula por cada concepto de la transacción, no a nivel de documento. Cada concepto puede tener una clasificación tributaria diferente y, por tanto, un desglose fiscal diferente. | No |
| **R14** | **Orden de cálculo por dependencia:** El motor resuelve los tributos respetando el orden de dependencia: primero los tributos padre, luego los tributos hijo. La base gravable de un tributo hijo puede ser el valor calculado del tributo padre. | No |
| **R15** | **Cuantía mínima:** Cuando un tributo tiene cuantía mínima configurada, el motor verifica que la base gravable la supere antes de aplicar el cálculo. Si la base gravable no supera la cuantía mínima, el tributo no se aplica a ese concepto. | No |
| **R16** | **Fórmula estándar de cálculo:** El valor de cada tributo se calcula como: Valor = Base gravable × Tarifa. La base gravable puede ser el monto del concepto, una porción de este, o el valor de otro tributo (tributo padre), según la configuración de la jurisdicción. | No |
| **R17** | **Naturaleza aditiva vs. sustractiva:** Los impuestos (IVA, INC, ITBIS) son aditivos — incrementan el valor total de la transacción. Las retenciones (RETEFUENTE, RIVA, RICA) son sustractivas — se descuentan del valor a pagar al tercero. La naturaleza del tributo determina su efecto sobre el valor neto. | No |
| **R18** | **Retención como recaudo anticipado o impuesto definitivo:** Según la jurisdicción, una retención puede operar como recaudo anticipado de un impuesto que el tercero declarará y descontará en su declaración (Colombia), o como impuesto definitivo que constituye el pago total de la obligación tributaria (algunas retenciones de República Dominicana). El sistema registra esta distinción en el registro tributario porque afecta el tratamiento del certificado tributario y la información reportada al tercero. | No |

**Ajuste manual**

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R19** | **Desglose propuesto con tributos evaluados:** El resultado del motor de cálculo incluye dos conjuntos: los tributos que aplican (desglose propuesto) y los tributos descartados que fueron evaluados pero excluidos, indicando el motivo estructurado de exclusión (cuantía mínima no superada, perfil tributario no aplica, clasificación excluida, jurisdicción no aplica, dependencia de tributo padre no cumplida). El usuario puede ajustar el desglose dentro del sub-dominio consumidor mientras la transacción no esté confirmada: excluir un tributo propuesto o incluir un tributo descartado. Al confirmar, Impuestos detecta la divergencia entre el cálculo del motor y el desglose confirmado, y la registra como intervención manual en el registro tributario (R24). | No |
| **R20** | **Recálculo descarta ajustes:** Si el usuario modifica datos que afectan el contexto del cálculo (monto, tercero, clasificación tributaria, jurisdicción), el sub-dominio consumidor solicita un nuevo cálculo. El desglose anterior se descarta y los ajustes manuales no se conservan. | No |
| **R21** | **Trazabilidad de ajustes en el consumidor:** La trazabilidad de los ajustes manuales al desglose (qué se modificó, quién, cuándo) es responsabilidad del sub-dominio consumidor, no del sistema de Impuestos. | No |

### 6.4 Registro tributario (huella fiscal)

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R22** | **Registro tributario al confirmar:** El registro tributario se crea cuando el sub-dominio consumidor confirma la transacción y notifica el desglose definitivo. Durante la fase de edición, el consumidor utiliza el motor de cálculo en modo simulación (stateless) cuantas veces necesite sin generar registros. Al recibir la confirmación, el sistema re-ejecuta el motor con el contexto recibido para obtener el cálculo original, compara con el desglose confirmado por el consumidor para detectar intervención manual, y crea el registro tributario como hecho fiscal único e inmutable. | No |
| **R23** | **Registro tributario inmutable:** Una vez creado, el registro tributario no puede modificarse ni eliminarse. Es la fuente de verdad del sub-dominio de Impuestos. Cualquier corrección genera un nuevo registro tributario (de ajuste o anulación), nunca una modificación del original. | No |
| **R24** | **Contenido mínimo del registro:** El registro tributario conserva como mínimo: el desglose confirmado, la identificación de la transacción origen, la fecha de la transacción, la jurisdicción, los perfiles tributarios usados y la configuración vigente aplicada. Si hubo intervención manual (tributos excluidos o incluidos por el usuario), el registro conserva adicionalmente el cálculo original del motor — tributos aplicados y descartados con motivo de exclusión — para permitir auditar la divergencia entre lo que el motor determinó y lo que el usuario confirmó. | No |
| **R25** | **Copia autónoma en el consumidor:** El sub-dominio consumidor almacena su propia copia del desglose confirmado para operar de forma autónoma. El registro tributario centralizado y la copia del consumidor coexisten sin dependencia operativa. | No |
| **R36** | **Registro vinculado a transacción origen:** Cada registro tributario referencia la transacción origen mediante el `transaccionId` proporcionado por el sub-dominio consumidor. Los desgravámenes (devoluciones, notas crédito) son transacciones independientes del consumidor que generan su propio registro tributario a través del mismo flujo de confirmación — la relación entre transacciones es responsabilidad del consumidor. Las anulaciones de transacciones no confirmadas no generan registro tributario. | No |

### 6.5 Cumplimiento fiscal

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R26** | **Generación desde registros propios:** Los reportes de información fiscal y los certificados tributarios se generan exclusivamente desde los registros tributarios del sub-dominio de Impuestos, no desde la contabilidad ni desde los sub-dominios consumidores. | No |
| **R27** | **Formatos de presentación configurables:** Los formatos de los entregables se configuran según los requerimientos de cada autoridad fiscal. El sistema soporta como mínimo: formato final exigido por la autoridad (XML u otro) y, cuando la autoridad dispone de herramientas intermedias de validación (como el prevalidador DIAN), el formato de entrada de dicha herramienta (ej: Excel). | Sí |
| **R28** | **Certificados tributarios con entrega controlada:** Los certificados tributarios se generan en formato legible (PDF) y se entregan al tercero destinatario mediante el canal configurado (correo electrónico, portal u otro), registrando la trazabilidad de la entrega: a quién, cuándo y por qué canal. | Sí (canal) |
| **R29** | **Periodicidad de entregables:** Cada tipo de entregable tiene su propia periodicidad según la jurisdicción y la autoridad fiscal (mensual, bimestral, anual). El sistema permite configurar la periodicidad por tipo de entregable y jurisdicción. | Sí |
| **R37** | **Entrega masiva de certificados:** El sistema soporta la generación y entrega masiva de certificados tributarios para un período fiscal. La entrega masiva registra la misma trazabilidad que la individual (destinatario, fecha, canal) y permite programar el envío. | Sí (programación) |

### 6.6 Integridad y validación (transversal)

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R30** | **Solicitud de cálculo completa:** El motor de cálculo rechaza solicitudes incompletas. El contexto mínimo requerido es: dirección fiscal, identificación de la entidad fiscal emisora, identificación de la entidad fiscal contraparte, ubicaciones tipificadas por rol (sede emisora, sede contraparte y, cuando aplique, lugar de ejecución del hecho económico), fecha de la transacción, moneda, y al menos un concepto con su clasificación tributaria y monto. El motor resuelve internamente la jurisdicción fiscal a partir de las ubicaciones y las reglas de localización configuradas por tributo. | No |
| **R31** | **Entidades fiscales con perfil tributario:** El motor de cálculo requiere que tanto la entidad fiscal emisora como la entidad fiscal contraparte tengan perfil tributario registrado en el sub-dominio de Impuestos. Si no existe perfil para la identificación recibida, el motor rechaza la solicitud indicando el dato faltante. | No |
| **R32** | **Clasificación tributaria válida:** Cada concepto de la solicitud debe referenciar una clasificación tributaria que exista en el catálogo vigente del sub-dominio de Impuestos. Si la clasificación no existe o no está vigente, el motor rechaza el concepto. | No |
| **R33** | **Sin conocimiento contable:** El sub-dominio de Impuestos no tiene conocimiento de cuentas contables. La traducción contable de los tributos es responsabilidad de cada sub-dominio consumidor a partir de su copia del desglose fiscal. | No |

---

## Sección 7: Qué está dentro y fuera del alcance

El sub-dominio de Impuestos agrupa capacidades con distinto nivel de centralidad, que conviven dentro del mismo bounded context pero cumplen roles diferenciados. El **núcleo** del sub-dominio lo conforman la configuración tributaria (catálogo de tributos, tarifas, reglas de aplicación), la determinación y cálculo de tributos, el perfil tributario de las entidades fiscales y el registro tributario como fuente de verdad del hecho fiscal. Estas capacidades definen la identidad del sub-dominio y son las que protegen las invariantes centrales del dominio.

Las **capacidades de soporte** facilitan la operación del núcleo sin ser parte de él: la carga asistida del perfil tributario desde fuentes oficiales y los catálogos jurisdiccionales de consulta.

Las **capacidades derivadas** se construyen a partir del núcleo y consumen sus datos sin redefinirlo: reportes de información fiscal, certificados tributarios y los entregables regulatorios en general. Las declaraciones tributarias (Tax Returns) se contemplan como fase futura del producto — ver *Fuera del alcance*. Su ciclo de vida es distinto al del cálculo y el registro — operan por período, formato, versión y autoridad — y deben modelarse reconociendo esa diferencia.

Las tablas a continuación detallan qué está dentro y fuera del alcance del sub-dominio, abarcando los tres niveles de capacidad.

### Dentro del alcance

> Las fases F1 y F2 se definen en la Sección 8. La pertenencia al BC no cambia — la columna indica el objetivo de implementación.

| Área | Descripción | Fase |
|------|-------------|:----:|
| **Contenido fiscal** | El sistema provee precargados los tributos, tarifas, bases mínimas, reglas de aplicación, dependencias y vigencias para cada jurisdicción soportada. El cliente inicia operación sin configurar el estándar fiscal del país. Cuando la normativa cambia, el contenido fiscal se actualiza como parte del producto. | F1 |
| **Clasificación tributaria** | Catálogo de clasificaciones tributarias que agrupan bienes y servicios según su tratamiento tributario. Provisto como parte del contenido fiscal, extensible por el administrador fiscal para casos no cubiertos. | F1 |
| **Perfil tributario** | Gestión de los perfiles tributarios de las entidades fiscales (emisoras y contrapartes): régimen tributario, condición de autorretenedor, gran contribuyente, agente de retención, entre otros según la jurisdicción. | F1 |
| **Carga asistida del perfil tributario** | A partir del número de identificación de la entidad fiscal, el sistema consulta fuentes oficiales de la autoridad fiscal y extrae automáticamente los atributos tributarios disponibles. El administrador fiscal valida y confirma los datos extraídos. | F1 |
| **Referencia documental del perfil tributario** | Referencia al documento que respalda atributos del perfil tributario de las entidades fiscales (ej: RUT en Colombia, RNC en República Dominicana): tipo de documento, número y fecha de emisión. Permite registrar la procedencia del atributo fiscal para trazabilidad. | F1 |
| **Reglas de aplicación** | Configuración de qué tributos aplican para cada combinación de clasificación tributaria + dirección fiscal + jurisdicción, con soporte de dependencias entre tributos, cuantía mínima y vigencia temporal. | F1 |
| **Motor de cálculo** | Determinación automática y cálculo de tributos a partir del contexto de la transacción: entidades fiscales, clasificación tributaria, dirección fiscal y jurisdicción. Retorna un desglose fiscal propuesto al sub-dominio consumidor. | F1 |
| **Resolución de jurisdicción** | El consumidor envía ubicaciones tipificadas por rol semántico: sede emisora, sede contraparte y, cuando aplique, lugar de ejecución del hecho económico (donde se presta el servicio, se entrega el bien, se ubica el inmueble o se ejecuta el proyecto). El motor resuelve internamente cuál ubicación es la fiscalmente relevante para cada tributo mediante reglas de localización configuradas por tributo y clasificación tributaria (contenido estándar del producto). A partir de la ubicación seleccionada, el motor identifica los niveles jurisdiccionales que aplican según el país (nacional, municipal) y resuelve los tributos correspondientes a cada nivel. El contenido fiscal incluye el catálogo de jurisdicciones por país con su jerarquía y códigos estándar. | F1 |
| **Tributos autoliquidados (reverse charge)** | El motor soporta la determinación de tributos autoliquidados: transacciones donde la entidad fiscal emisora debe autoliquidar un impuesto que la contraparte no cobra (ej: importación de servicios, compras a no residentes). | F1 |
| **IVA descontable** | El registro tributario preserva los datos necesarios (naturaleza del tributo, dirección fiscal, base gravable, valor) para que se pueda clasificar el IVA como generado o soportado. La determinación de si el IVA soportado es descontable (crédito fiscal) — que depende del régimen de la empresa y la proporción de ingresos gravados del período — y su presentación en declaraciones (ej: F-1005 DIAN) es capacidad de Fase 2. | F1 |
| **Conversión por cuantía mínima** | Cuando la moneda de la transacción difiere de la moneda de la jurisdicción, el motor recibe un tipo de cambio de referencia como parte de la solicitud de cálculo y lo utiliza para convertir a la moneda de la jurisdicción al evaluar cuantías mínimas. El resultado del cálculo se retorna en la moneda de la transacción; la conversión para efectos contables es responsabilidad del traductor contable. | F1 |
| **Simulación de cálculo** | El motor soporta cálculos de simulación (ej: cotizaciones, presupuestos) que retornan un desglose fiscal propuesto sin generar registro tributario. Solo las transacciones confirmadas por el sub-dominio consumidor generan registro tributario. | F1 |
| **Registro tributario** | Registro inmutable de cada hecho tributario confirmado. Conserva el cálculo original, el desglose confirmado, los perfiles tributarios usados y la configuración vigente aplicada. Fuente de verdad del sub-dominio. | F1 |
| **Reportes de información fiscal** | Generación de reportes exigidos por autoridades fiscales (exógena DIAN, formatos DGII, reportes municipales) desde los registros tributarios propios, en los formatos requeridos (XML, Excel para prevalidador). | F2 |
| **Certificados tributarios** | Generación de certificados de retención en formato legible (PDF), con entrega controlada individual y masiva al tercero destinatario, registrando trazabilidad de la entrega. | F2 |
| **Vistas de conciliación fiscal** | El sistema expone vistas y reportes de sus registros tributarios organizados por tipo de tributo, período, jurisdicción y entidad fiscal, para facilitar la conciliación contra el libro mayor. El sistema no realiza la conciliación (no conoce cuentas contables), pero provee los datos como fuente de verdad fiscal. | F2 |
| **Multi-jurisdicción** | Diseño extensible para múltiples jurisdicciones y múltiples países. Implementación inicial: Colombia. | F1 |

---

### Fuera del alcance del sub-dominio de Impuestos

| Área | Descripción | Observación |
|------|-------------|-------------|
| **Determinación de la entidad fiscal** | El sub-dominio de Impuestos no determina quién es la entidad fiscal emisora ni la entidad fiscal contraparte de una transacción. | Es responsabilidad del sub-dominio consumidor, que conoce el contexto de negocio. Escenarios como facturación a nombre de terceros (inmobiliario) o mandante por proyecto (construcción) requieren que el consumidor identifique las entidades fiscales correctas antes de solicitar el cálculo. |
| **Traducción contable** | El sub-dominio de Impuestos no tiene conocimiento de cuentas contables ni genera asientos. | Cada sub-dominio consumidor traduce su copia del desglose fiscal a términos contables. |
| **Trazabilidad de ajustes manuales** | El sub-dominio de Impuestos no registra qué ajustes manuales realizó el usuario sobre el desglose propuesto. | La trazabilidad de ajustes (qué se modificó, quién, cuándo) es responsabilidad del sub-dominio consumidor. El registro tributario solo indica si hubo intervención manual. |
| **Tasas y contribuciones parafiscales** | No se cubren tasas (cobros por servicios estatales) ni contribuciones parafiscales (SENA, ICBF, cajas de compensación). | El alcance se limita a impuestos y retenciones, según la definición #1 del glosario. |
| **Procesamiento de pagos** | El sub-dominio de Impuestos no ejecuta pagos ni gestiona la consignación de retenciones a la autoridad fiscal. | Es responsabilidad de Tesorería. |
| **Facturación electrónica** | No se cubre la emisión ni recepción de facturas electrónicas (CFDI, e-CF, factura DIAN). | Es un sub-dominio independiente (Emisión y Recepción Electrónica). |
| **Transmisión a autoridades fiscales** | El sistema genera los archivos en el formato exigido, pero no los transmite directamente a las plataformas de las autoridades fiscales. | La transmisión (carga en portal DIAN, envío a DGII, etc.) se realiza fuera del sistema. |
| **Certificados de exención de clientes** | No se gestiona la recolección ni validación de certificados de exención presentados por clientes para no ser gravados. | Diferente de los certificados tributarios de retención (que la empresa emite al tercero). La condición de exención se refleja en el perfil tributario de la entidad fiscal. |
| **Impuesto de renta corporativo** | El cálculo de la provisión del impuesto de renta corporativo (renta líquida, deducciones, tarifa corporativa, saldo a pagar o a favor) no es responsabilidad del motor de impuestos transaccionales. | Los registros tributarios de retenciones practicadas a la empresa (en dirección de ingresos) son insumo para dicho cálculo. |
| **Nexus tracking** | La determinación de si una empresa tiene obligación de registrarse fiscalmente en una jurisdicción por umbral de ventas no es responsabilidad de este sub-dominio. | Relevante para jurisdicciones como Estados Unidos (economic nexus). A evaluar en expansión internacional. |
| **Conciliación fiscal-contable** | El sub-dominio no realiza la conciliación entre registros tributarios y el libro mayor. | Provee vistas de conciliación fiscal como fuente de verdad; la conciliación propiamente dicha es responsabilidad del sub-dominio contable. |
| **Gestión documental del perfil tributario** | Repositorio documental completo, alertamiento automático de vencimiento, bloqueo por ausencia o expiración de documentos y flujo documental avanzado. | Función evaluable a futuro según necesidad operativa. El perfil tributario registra la referencia al documento (tipo, número, fecha) como metadato de trazabilidad. |
| **Declaraciones tributarias** | La generación de declaraciones tributarias (IVA, retención en la fuente, ICA, ITBIS, etc.) no está dentro del alcance de la primera versión. A diferencia de los reportes de información (que consolidan datos), las declaraciones tienen lógica propia significativa: renglones calculados, saldos a favor de períodos anteriores, compensaciones, sanciones y liquidación privada. | Fase futura del producto. El modelo está preparado para incorporarlas sin cambios estructurales (`FormatoFiscal.tipoEntregable` es extensible). También es posible que se descarte como funcionalidad si el análisis costo-beneficio no lo justifica. |

---

### Dependencias externas

| Dependencia | Descripción | Impacto en el sub-dominio de Impuestos |
|-------------|-------------|----------------------------------------|
| **Gestión de Terceros** | Servicio transversal que centraliza la información de personas y empresas: identificación, razón social, datos de contacto. | El sub-dominio de Impuestos gestiona los perfiles tributarios como datos propios, pero referencia la identidad del tercero (tipo y número de identificación) desde este servicio. |
| **Sub-dominios consumidores** (OXP, CXC) | Módulos que producen transacciones con impacto fiscal y solicitan cálculo tributario. | Son la fuente de las solicitudes de cálculo y las confirmaciones que generan registros tributarios. Determinan quién ocupa el rol de entidad fiscal emisora y contraparte según su contexto de negocio. |
| **Fuentes oficiales de autoridades fiscales** | Servicios de consulta de las autoridades fiscales (DIAN, DGII) para obtener datos tributarios de entidades fiscales por número de identificación. | Alimentan la carga asistida de perfiles tributarios. La disponibilidad y alcance de datos varía según la jurisdicción. |

---

## Sección 8: Estrategia de implementación por fases

El sub-dominio de Impuestos conserva una visión integral de largo plazo que abarca todas las capacidades descritas en este documento. Su implementación se organiza por fases alineadas con la clasificación de capacidades `[D7]`:

### Fase 1 — Núcleo + Soporte

Capacidades que habilitan el ciclo operativo básico del sub-dominio:

| Capacidad | Nivel | Descripción |
|-----------|-------|-------------|
| Contenido fiscal (configuración tributaria base) | Núcleo | Catálogo de tributos, clasificaciones tributarias, reglas de aplicación, tarifas con vigencia temporal. |
| Perfil tributario | Núcleo | Gestión de perfiles tributarios de entidades fiscales: régimen, condiciones, atributos por jurisdicción. Referencia documental como metadato de trazabilidad. |
| Motor de cálculo | Núcleo | Determinación automática y cálculo de tributos a partir del contexto transaccional. |
| Registro tributario | Núcleo | Registro inmutable del hecho fiscal confirmado. Fuente de verdad del sub-dominio. |
| Carga asistida del perfil tributario | Soporte | Construcción del perfil desde fuentes oficiales con validación del administrador fiscal. |
| Catálogos jurisdiccionales | Soporte | Proyección consolidada de información jurisdiccional para consulta. |
| Integración con consumidores | Núcleo | Contrato de solicitud de cálculo y confirmación tributaria con sub-dominios consumidores (OXP, CXC). |

### Fase 2 — Derivadas

Capacidades que consumen el núcleo y el registro tributario sin redefinirlos:

| Capacidad | Descripción |
|-----------|-------------|
| Reportes de información fiscal | Exógena DIAN, formatos DGII, reportes municipales. |
| Certificados tributarios | Generación, entrega controlada y trazabilidad. |
| Homologación fiscal | Traducción de valores internos a códigos de autoridades fiscales. |
| Formatos fiscales | Plantillas de estructura y contenido por autoridad y tipo de entregable. |
| Entregables regulatorios | Ciclo de vida de generación y presentación ante autoridades. |
| IVA descontable | Determinación de crédito fiscal del IVA soportado para declaraciones (F-1005 DIAN, equivalentes por país). |
| Vistas de conciliación fiscal | Reportes organizados por tributo, período, jurisdicción. |

### Criterio de éxito de la Fase 1

La Fase 1 se considera operativa cuando:

1. Un sub-dominio consumidor puede solicitar el cálculo tributario enviando el contexto completo de la transacción.
2. Impuestos resuelve perfiles tributarios, jurisdicción y reglas de aplicación.
3. Impuestos devuelve el desglose fiscal (tributos aplicados y descartados con motivo).
4. El consumidor puede confirmar la transacción con el desglose aceptado (con o sin intervención manual).
5. Se genera el registro tributario inmutable como fuente de verdad del hecho fiscal.
6. Existe trazabilidad suficiente para auditoría y reconstrucción funcional del cálculo.

> **Nota:** Esta sección es una decisión de alcance funcional y de implementación, no un cronograma ni plan de proyecto. Las fases reflejan la dependencia natural entre capacidades: las derivadas requieren que el núcleo esté operativo.

---

## Sección 9: Beneficios esperados

| # | Beneficio | Problema que resuelve |
|---|-----------|----------------------|
| 1 | **Motor de cálculo centralizado:** Un único punto de cálculo tributario para todos los sub-dominios consumidores, eliminando la duplicación de lógica entre módulos. | Lógica tributaria fragmentada (Problema 1) |
| 2 | **Contenido fiscal como parte del producto:** El cliente inicia operación sin configurar el estándar fiscal del país. Cuando la normativa cambia, el producto se actualiza — no el cliente. | Configuración dispersa y propensa a desincronización (Problema 2) |
| 3 | **Registro tributario como fuente de verdad:** Cada cálculo tributario confirmado queda registrado de forma inmutable con el detalle completo: base gravable, tarifa, regla aplicada, perfiles usados y configuración vigente. | Pérdida de trazabilidad del hecho tributario (Problema 3) |
| 4 | **Reportes generados desde el origen:** Los exógenos y certificados se generan desde los registros tributarios propios del sub-dominio, no desde la reconstrucción contable. | Reportes construidos desde la contabilidad (Problema 4) |
| 5 | **Auditoría justificable:** La combinación de registro inmutable, configuración versionada y trazabilidad completa permite justificar cualquier cálculo tributario ante la autoridad fiscal sin cruzar manualmente múltiples fuentes. | Riesgo ante auditorías (Problema 5) |
| 6 | **Carga asistida de perfiles tributarios:** La consulta automática a fuentes oficiales de la autoridad fiscal reduce el esfuerzo de configuración inicial y minimiza errores de digitación en los perfiles tributarios. | *(Nuevo — reducción de barrera de entrada)* |
| 7 | **Resolución automática de jurisdicción:** El motor resuelve los niveles jurisdiccionales a partir del dato de ubicación, eliminando la carga de que el usuario o el consumidor determinen manualmente qué tributos municipales aplican. | *(Nuevo — simplificación operativa)* |
| 8 | **Determinación automática por perfil:** La combinación de perfiles tributarios de ambas entidades fiscales determina automáticamente qué tributos aplican, sin configuración manual por tercero ni matrices de aplicabilidad mantenidas por el usuario. | *(Nuevo — escalabilidad a 70k+ terceros)* |
| 9 | **Vistas de conciliación fiscal:** Los registros tributarios organizados por tipo de tributo, período y jurisdicción facilitan la conciliación contra el libro mayor sin depender de reconstrucciones contables. | *(Nuevo — eficiencia operativa)* |
| 10 | **Cálculo consistente desde la simulación hasta el hecho económico:** Los sub-dominios consumidores pueden usar el motor de cálculo en etapas previas al hecho económico (compras, cotizaciones, prefacturas, presupuestos) obteniendo el mismo resultado que se aplicará cuando la transacción se confirme. Un único mecanismo centralizado garantiza consistencia entre la simulación y el cálculo definitivo. | *(Nuevo — consistencia y confianza en proyecciones)* |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: 9 secciones, 22 términos en glosario, 6 actores, 3 flujos, 38 reglas de negocio, 17 áreas dentro del alcance, 13 áreas fuera del alcance, estrategia de implementación por fases (F1 Núcleo+Soporte, F2 Derivadas). |