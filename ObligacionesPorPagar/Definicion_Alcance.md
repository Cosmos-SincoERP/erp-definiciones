# Sistema OXP - Definición de Alcance

## Tabla de contenido

1. [Contexto y problema a resolver](#sección-1-contexto-y-problema-a-resolver)
2. [Glosario de términos](#sección-2-glosario-de-términos)
3. [Actores del sistema](#sección-3-actores-del-sistema)
4. [Flujo principal](#sección-4-flujo-principal)
5. [Integraciones](#sección-5-integraciones)
6. [Reglas de negocio](#sección-6-reglas-de-negocio)
7. [Qué está dentro y fuera del alcance](#sección-7-qué-está-dentro-y-fuera-del-alcance)
8. [Beneficios esperados](#sección-8-beneficios-esperados)

---

## Sección 1: Contexto y problema a resolver

### Contexto

La compañía realiza compras a través de dos medios de pago corporativos:

- **Tarjetas de crédito corporativas:** Utilizadas para compras generales de la operación.
- **Tarjetas de débito prepago:** Asignadas a empleados para compras menores como insumos en obras (tornillos, materiales), suministros para estaciones de trabajo (agua, café), y gastos de representación (almuerzos de negocios, transporte para capacitaciones o reuniones con clientes).

Estas transacciones generan obligaciones por pagar (OXP) que deben ser radicadas con sus soportes documentales, conciliadas con los extractos bancarios y causadas contablemente.

Actualmente el proceso se gestiona de forma manual mediante archivos de Excel, donde los usuarios concilian las transacciones contra el extracto de la entidad bancaria y posteriormente registran la causación en el sistema contable.

### Problema actual

La gestión manual presenta los siguientes desafíos:

1. **Proceso de radicación no formalizado:** No existe un flujo estructurado para recibir y registrar los soportes de las compras, lo que genera demoras en la causación por documentación incompleta o faltante.

2. **Conciliación propensa a errores:** El cruce manual entre transacciones y extractos bancarios en Excel dificulta la identificación de diferencias y aumenta el riesgo de errores.

3. **Falta de visibilidad:** No se cuenta con una vista consolidada y en tiempo real del estado de las obligaciones por pagar, dificultando el seguimiento y control.

4. **Alta carga operativa:** El esfuerzo manual requerido para conciliar, causar y dar seguimiento a las OXP consume tiempo significativo del equipo. Esto incluye la gestión individual de cada recibo o documento soporte de las transacciones.

5. **Cumplimiento normativo con reprocesos:** La DIAN exige la emisión de documentos soporte para compras del exterior que no generan factura electrónica. Estos documentos deben formalizarse dentro de los 6 días hábiles posteriores a la fecha de la transacción, es decir, antes de recibir el extracto bancario. Actualmente se cumple con este requisito, pero el proceso manual genera reprocesos posteriores que incrementan la carga operativa y pueden derivar en hallazgos de auditoría.

6. **Restricción en el uso del medio de pago:** Debido a lo desgastante del proceso actual, la compañía limita los tipos de compras que se realizan con tarjeta de crédito, perdiendo oportunidades como acceder a mejores precios de otros proveedores o adquirir mejores productos disponibles únicamente a través de este medio de pago.

### Alcance inicial

El sistema se implementará inicialmente en una compañía piloto que maneja:
- 2 tarjetas de crédito corporativas
- Aproximadamente 100 transacciones mensuales (50 por tarjeta)

El diseño debe contemplar escalabilidad para compañías con mayor volumen y la incorporación de tarjetas de débito prepago.

---

## Sección 2: Glosario de términos

| Término | Definición |
|---------|------------|
| **OXP** | Obligación por Pagar. Registro que representa un compromiso de pago de la compañía. |
| **OXP de Comercio** | Obligación por pagar originada por una compra individual realizada con tarjeta de crédito corporativa o tarjeta de débito prepago en un comercio. |
| **OXP de Extracto** | Obligación por pagar consolidada que agrupa las OXP de comercio de un período, incluyendo las devoluciones y los cargos adicionales aplicables según el medio de pago. |
| **Compensada (OXP de Comercio)** | Estado operativo que indica que una OXP de comercio ha sido vinculada a una OXP de Extracto. Este estado no representa un pago financiero, pero sí constituye el insumo que el sistema OXP entrega al sistema contable (SincoA&F) para que se realicen los cruces contables efectivos. Es decir: la compensación es operativa en OXP, pero tiene impacto contable indirecto al habilitar el registro de la relación OXP-Extracto en el sistema contable. |
| **Pagada (OXP de Extracto)** | Estado que indica que el sistema contable externo (SincoA&F) ha confirmado la ejecución del pago financiero correspondiente al extracto. |
| **Radicación / Legalización** | Proceso de registro formal de una transacción en el sistema, incluyendo sus soportes documentales. Ambos términos son sinónimos. |
| **Anticipo** | Obligación por pagar que no cuenta con los soportes correspondientes pero se debe entregar el dinero al comercio/proveedor. Se registra en cuentas específicas y posteriormente se reclasifica a las cuentas definitivas cuando se completa la legalización. El sistema mantiene trazabilidad del anticipo desde su creación hasta su legalización, permitiendo el seguimiento de saldos pendientes. Se aplican políticas de plazo configurables para alertar sobre anticipos que excedan el tiempo permitido sin legalizar. Aplica para ambos medios de pago. |
| **Devolución** | Reintegro de dinero por una compra devuelta al comercio. Se refleja como valor negativo en el extracto y se representa contablemente como nota crédito. Debe legalizarse y queda relacionada en la OXP de extracto. La devolución puede aparecer en un período diferente al de la OXP de comercio original. |
| **Nota Crédito** | Documento contable que representa una devolución o ajuste a favor de la compañía. Se genera al legalizar una devolución. |
| **Documento Soporte** | Documento requerido por la DIAN para respaldar compras del exterior que no generan factura electrónica. Debe emitirse dentro de los 6 días hábiles posteriores a la fecha de la transacción, típicamente durante la etapa de radicación. Una vez emitido el documento soporte, si el comercio intenta enviar una factura electrónica para la misma transacción, esta será rechazada por el módulo SincoRE conforme a las reglas del ente regulador. |
| **Causación** | Registro contable de una obligación por pagar en el sistema contable de la compañía. |
| **Extracto Bancario** | Documento emitido por la entidad financiera que detalla las transacciones y cargos de la tarjeta en un período determinado. |
| **Conciliación** | Proceso de cruce y verificación entre las OXP de comercio registradas y las transacciones reportadas en el extracto bancario. |
| **Recarga** | Proceso de reposición de fondos a una tarjeta de débito prepago, originado a partir de las transacciones realizadas y legalizadas. |
| **4x1000** | Gravamen a los Movimientos Financieros (GMF) aplicado en Colombia sobre las transacciones financieras. Aplica para ambos medios de pago. |
| **Cuota de Manejo** | Cargo periódico cobrado por la entidad financiera por la administración de la tarjeta. Aplica para ambos medios de pago. |
| **Intereses** | Cargos financieros aplicados por la entidad bancaria sobre saldos diferidos o en mora. Aplica únicamente para tarjetas de crédito. |

---

## Sección 3: Actores del sistema

### Actores internos (usuarios del sistema)

| Actor | Descripción |
|-------|-------------|
| **Solicitante** | Empleado que realiza compras con tarjeta de crédito corporativa o tarjeta de débito prepago. Puede iniciar el proceso de radicación adjuntando el soporte disponible al momento de la compra. También puede cargar extractos bancarios. |
| **Radicador** | Usuario responsable de completar el registro de las transacciones en el sistema con sus respectivos soportes documentales. Puede ser el mismo solicitante u otra persona. También puede cargar extractos bancarios. |
| **Confirmador** | Usuario que revisa y confirma las OXP en estado pendiente para habilitar su integración con el sistema contable. Este rol es opcional según las preferencias configuradas por empresa; en algunos casos las OXP pueden generarse directamente en estado confirmado. |

Cualquier usuario puede acceder a vistas de monitoreo (OXP pendientes, confirmadas, contabilizadas) según los permisos asignados.

### Actores externos (sistemas integrados)

| Actor | Descripción |
|-------|-------------|
| **Entidad Bancaria** | Provee los extractos de las tarjetas de crédito y débito prepago en formato PDF o CSV, los cuales son cargados al sistema por un usuario. |
| **Sistema Contable** | Recibe automáticamente las causaciones de las OXP una vez estas son confirmadas. |
| **Sistema de Tesorería** | Gestiona los pagos de las OXP de extracto (tarjetas de crédito) y las recargas (tarjetas débito prepago). |

### Formatos de entrada soportados

| Formato | Descripción |
|---------|-------------|
| **PDF** | Extractos bancarios y soportes documentales escaneados o digitales. |
| **CSV** | Extractos bancarios en formato estructurado para facilitar la carga masiva. |
| **XML** | Facturas electrónicas que permiten extraer automáticamente información como: identificación del proveedor/comercio, fecha de factura, valores y número de factura. |
| **Imágenes** | Fotografías o escaneos de recibos y soportes documentales (JPG, PNG, etc.). |

---

## Sección 4: Flujo principal

### Flujos del proceso

El sistema OXP maneja dos flujos diferenciados que convergen en la conciliación:

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           FLUJO OXP DE COMERCIO                                  │
│                                                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │ 1. Radica-   │    │ 2. Confir-   │    │ 3. Causa-    │    │ 4. Compen-   │   │
│  │    ción      │───▶│    mación    │───▶│    ción      │───▶│    sada      │   │
│  └──────────────┘    └──────────────┘    └──────────────┘    └──────┬───────┘   │
│                                                                      │           │
└──────────────────────────────────────────────────────────────────────┼───────────┘
                                                                       │
                                                              (vinculación)
                                                                       │
┌──────────────────────────────────────────────────────────────────────▼─────────────────────────────┐
│                                    FLUJO OXP DE EXTRACTO                                           │
│                                                                                                    │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐ │
│  │ 1. Radica-   │    │ 2. Conci-    │    │ 3. Confir-   │    │ 4. Causa-    │    │ 5. Pago /    │ │
│  │    ción      │───▶│   liación    │───▶│   mación     │───▶│   ción       │───▶│   Recarga    │ │
│  └──────────────┘    └──────────────┘    └──────────────┘    └──────────────┘    └──────────────┘ │
│                                                                                                    │
└────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

**OXP de Comercio:** Se radica, confirma y causa de forma independiente. Posteriormente, cuando se carga el extracto y se completa la conciliación, la OXP de Comercio pasa a estado **compensada** al vincularse a la OXP de Extracto.

**OXP de Extracto:** Se radica, se concilia al 100% (vinculando las OXP de Comercio), se confirma, se causa contablemente, y finalmente se registra el pago cuando el sistema contable (SincoA&F) lo confirma.

---

### Relación entre etapas y estados

Las **etapas** son las actividades del flujo de trabajo (qué se hace). Los **estados** son las condiciones de cada OXP en un momento dado (cómo está). Al completar una etapa, la OXP cambia de estado.

| Etapa (actividad) | Estado resultante OXP Comercio | Estado resultante OXP Extracto |
|-------------------|-------------------------------|-------------------------------|
| Radicación | Pendiente | Pendiente |
| Confirmación | Confirmada | — |
| Causación | Contabilizada | — |
| Conciliación | Compensada | Conciliada (100%) |
| Confirmación (Extracto) | — | Confirmada |
| Causación (Extracto) | — | Contabilizada |
| Pago | — | Pagada |

---

### Etapa 1: Radicación

| Aspecto | Descripción |
|---------|-------------|
| **Disparador - OXP de Comercio** | Empleado realiza una compra con tarjeta de crédito o débito prepago. |
| **Disparador - OXP de Extracto** | La entidad bancaria emite el extracto del período. |
| **Entrada - OXP de Comercio** | Soporte documental (factura XML, PDF, imagen) con datos de la transacción. |
| **Entrada - OXP de Extracto** | Archivo del extracto en formato PDF o CSV con la relación de transacciones del período. |
| **Proceso - OXP de Comercio** | El solicitante o radicador registra la transacción en el sistema adjuntando el soporte. Si el soporte es XML, se extraen automáticamente los datos relevantes. |
| **Proceso - OXP de Extracto** | El solicitante o radicador carga el extracto en el sistema. Se extraen las transacciones y cargos adicionales (4x1000, cuota de manejo, intereses si aplica). |
| **Salida - OXP de Comercio** | OXP de Comercio creada en estado pendiente. |
| **Salida - OXP de Extracto** | OXP de Extracto creada con el detalle de transacciones y cargos del período. |
| **Variante - Anticipo** | Si no se cuenta con el soporte, se crea la OXP de Comercio como Anticipo para su posterior legalización. |
| **Variante - Devolución** | Si la transacción es una devolución, se registra y se asocia a la OXP de comercio original cuando esta se encuentre disponible. Si la OXP original no está disponible (por ejemplo, por aparecer en un período diferente), la devolución se registra y queda pendiente de asociación hasta que pueda relacionarse posteriormente. |

---

### Etapa 2: Conciliación

| Aspecto | Descripción |
|---------|-------------|
| **Disparador** | OXP de Extracto disponible para cruce. |
| **Entrada** | OXP de Comercio radicadas y OXP de Extracto. |
| **Proceso** | El sistema realiza un cruce automático entre las transacciones del extracto y las OXP de comercio registradas. Para las partidas que no logra conciliar automáticamente, el usuario puede completar el cruce manualmente. |
| **Criterios de cruce automático** | El sistema utiliza una combinación de criterios: (1) **Comercio:** detección asistida por un agente inteligente que sugiere asociaciones basadas en el contexto histórico de transacciones y las descripciones del extracto; el usuario puede también realizar la relación manualmente. Una vez establecida la asociación, el sistema la persiste para aplicarla automáticamente en futuras conciliaciones. (2) **Valor:** comparación del monto radicado considerando variaciones por impuestos. (3) **Fecha:** correspondencia con la fecha de la transacción. |
| **Estados de conciliación** | **Inicio:** proceso no iniciado. **Parcialmente conciliado:** algunas partidas cruzadas, otras pendientes. **Conciliación completada:** 100% de las partidas del extracto conciliadas. |
| **Regla de avance** | El extracto debe estar en estado "Conciliación completada" (100%) antes de pasar a la etapa de confirmación. |
| **Alerta de plazo** | El sistema alerta cuando la conciliación no está completada dentro del plazo configurado previo a la fecha de pago (por defecto 3 días). Este plazo es configurable en las preferencias de cada tarjeta. |
| **Salida - Conciliación exitosa** | OXP de Comercio vinculadas a la OXP de Extracto. |
| **Salida - Partidas sin conciliar** | El sistema notifica al usuario las transacciones del extracto sin OXP de comercio asociada. El usuario decide entre: (a) gestionar la solicitud de radicación de las OXP faltantes, o (b) generar Anticipos. Cuando la radicación no se completa oportunamente, la partida debe formalizarse como anticipo para permitir el cierre del extracto y cumplir la conciliación 100%. |

---

### Etapa 3: Confirmación y Causación

| Aspecto | Descripción |
|---------|-------------|
| **Disparador** | OXP conciliadas listas para confirmar. |
| **Entrada** | OXP de Comercio y OXP de Extracto pendientes de confirmación (extracto con conciliación 100% completada). |
| **Proceso** | El confirmador revisa y confirma las OXP (o se confirman automáticamente según configuración de la empresa). La confirmación habilita y dispara el proceso de causación automático que se integra con el sistema contable (SincoA&F). La causación se ejecuta automáticamente como consecuencia directa de la confirmación y no requiere una acción adicional del usuario. |
| **Causación** | Se genera una causación individual por cada OXP de comercio y una causación por la OXP de extracto. La causación de la OXP de Extracto tiene como propósito registrar el total del extracto contra la entidad bancaria o el medio de pago, incluyendo los cargos adicionales (4x1000, cuota de manejo, intereses). Esto no representa doble causación respecto a las OXP de Comercio, ya que cada una tiene naturaleza contable diferente. |
| **Salida** | OXP en estado confirmado. Causaciones registradas en el sistema contable externo. |
| **Estado Contabilizada** | Una OXP se considera **contabilizada** cuando el sistema contable externo (SincoA&F) confirma el registro exitoso de la causación enviada por OXP. |

---

### Etapa 4: Pago / Recarga

| Aspecto | Descripción |
|---------|-------------|
| **Disparador** | OXP de Extracto causada contablemente. |
| **Entrada** | OXP de Extracto confirmada y contabilizada. |
| **Proceso - OXP de Comercio** | Se considera **compensada** en el momento que se vincula dentro de una OXP de Extracto. Este estado es operativo; no representa un desembolso financiero, pero sí es el insumo que OXP entrega a SincoA&F para registrar los cruces contables correspondientes. |
| **Proceso - OXP de Extracto** | El sistema contable externo (SincoA&F) gestiona la ejecución del pago (tarjeta de crédito) o la recarga (tarjeta débito prepago). El sistema OXP **únicamente monitorea** y registra el estado de pago consultando a SincoA&F. |
| **Salida** | OXP de Extracto marcada como **pagada** una vez SincoA&F confirma la ejecución del pago financiero. Ciclo cerrado. |

---

## Sección 5: Integraciones

### Principio de responsabilidad

El sistema OXP actúa como orquestador del proceso de obligaciones por pagar. Sus responsabilidades son:

- **Registrar** transacciones con sus soportes documentales.
- **Orquestar** el flujo de radicación-conciliación-confirmación.
- **Traducir contablemente** los conceptos de obligaciones por pagar (comercio, extracto, anticipos, devoluciones, cargos) a cuentas, centros de costo, terceros y naturaleza contable. Esta traducción estructurada es el insumo que OXP entrega a SincoA&F para su registro.
- **Monitorear** el estado de las obligaciones consultando los sistemas externos.

La **ejecución** de pagos, desembolsos y movimientos financieros es responsabilidad exclusiva de los sistemas externos integrados (SincoA&F). El sistema OXP no ejecuta desembolsos ni modifica saldos bancarios directamente.

### Ecosistema de integración

El sistema OXP se integrará con **SincoERP** como plataforma central, interactuando con los siguientes módulos:

| Módulo | Nombre | Función |
|--------|--------|---------|
| **SincoA&F** | Administración y Finanzas | Sistema contable. Recibe causaciones, gestiona pagos y confirma estado de pago de las OXP. |
| **SincoRE** | Recepción Electrónica | Transforma facturas electrónicas colombianas en información estructurada y la expone mediante API. |
| **SincoADPRO** | Compras y Contratación | Ratifica la legalización de compras que requieren formalización en este módulo (no aplica para todas las compras). |

---

### Integraciones entrantes (hacia el sistema OXP)

| Origen | Datos recibidos | Formato | Método de integración | Observación |
|--------|-----------------|---------|----------------------|-------------|
| **SincoRE** | Datos estructurados de facturas electrónicas colombianas | JSON | Consumo de API | Solo facturas electrónicas colombianas |
| **SincoADPRO** | Ratificación de legalización de compras | JSON | Consumo de API | Solo para compras que requieren formalización en este módulo |
| **SincoA&F** | Confirmación de pago de OXP de Extracto | JSON | Consumo de API | |
| **Entidad Bancaria** | Extracto con transacciones y cargos del período | PDF, CSV | Carga manual de archivo por usuario | |
| **Soportes documentales** | Recibos, facturas no electrónicas, comprobantes | PDF, Imágenes (JPG, PNG) | Carga manual de archivo por usuario | Compras del exterior u otras sin factura electrónica |

---

### Integraciones salientes (desde el sistema OXP)

| Destino | Datos enviados | Formato | Método de integración |
|---------|----------------|---------|----------------------|
| **SincoA&F** | Causación de OXP de Comercio (individual por cada OXP) | JSON | Consumo de API |
| **SincoA&F** | Causación de OXP de Extracto | JSON | Consumo de API |
| **SincoA&F** | Nota Crédito por devoluciones | JSON | Consumo de API |
| **SincoA&F** | Reclasificación contable por legalización de anticipos | JSON | Consumo de API |

---

### Diagrama de integraciones

```
                                    ┌─────────────────┐
                                    │  Entidad        │
                                    │  Bancaria       │
                                    │  (PDF, CSV)     │
                                    └────────┬────────┘
                                             │ Carga manual
                                             ▼
┌─────────────────┐                ┌─────────────────┐                ┌─────────────────┐
│    SincoRE      │───────────────▶│                 │───────────────▶│    SincoA&F     │
│  (Facturas XML) │    API JSON    │   Sistema OXP   │    API JSON    │   (Contable)    │
└─────────────────┘                │                 │◀───────────────│                 │
                                   │                 │  Confirmación  └─────────────────┘
┌─────────────────┐                │                 │     pago
│   SincoADPRO    │───────────────▶│                 │
│   (Compras)     │    API JSON    └─────────────────┘
└─────────────────┘   (opcional)
```

---

### Notas de la primera fase

- No se tendrá integración directa con un sistema de tesorería independiente.
- El módulo SincoA&F se encarga del procesamiento de pago a partir de la causación.
- El sistema OXP consultará el servicio de SincoA&F para confirmar el estado de pago de las OXP.
- Los detalles técnicos de los API (endpoints, estructura de datos, autenticación) se documentarán posteriormente.
- **Formatos de extracto soportados:** En la primera fase, el sistema soporta formatos estructurados (CSV) y extractos PDF. Los extractos en formato PDF son interpretados por un agente inteligente que detecta patrones sobre la información relevante a extraer (transacciones, fechas, valores, cargos adicionales). Cuando un PDF no sea estructurable por el agente, el proceso se apoyará en CSV u otros mecanismos definidos por la operación.

### Visión a futuro

La arquitectura del sistema OXP está diseñada para servir como base para la modernización progresiva de los módulos de SincoERP. A medida que se construyan nuevos módulos con esta arquitectura, las integraciones y responsabilidades podrán evolucionar, permitiendo una transición gradual del ecosistema actual.

---

## Sección 6: Reglas de negocio

### Reglas de radicación

| ID | Regla | Configurable |
|----|-------|--------------|
| R01 | Las compras del exterior que no generan factura electrónica deben tener documento soporte emitido dentro de los 6 días hábiles posteriores a la fecha de la transacción (requisito DIAN). | No |
| R02 | Toda OXP de Comercio se crea inicialmente en estado pendiente. | Sí (por empresa) |
| R03 | Si no se cuenta con el soporte documental, se puede crear la OXP como Anticipo. | No |
| R04 | Las devoluciones deben legalizarse y asociarse a la OXP de comercio original cuando esté disponible. | No |
| R04b | Los anticipos deben legalizarse (amortizarse) dentro del plazo configurado. El plazo puede variar según acuerdos con el comercio/proveedor. El sistema genera alertas cuando un anticipo excede el tiempo permitido sin legalización. | Sí (por empresa, default 30 días) |
| R05 | Las transacciones que superen un monto máximo configurado generan una alerta informativa indicando que deben cumplir el flujo completo de legalización. | Sí (por empresa, default 30 millones COP) |

---

### Reglas de conciliación

| ID | Regla | Configurable |
|----|-------|--------------|
| R06 | El extracto debe estar 100% conciliado antes de pasar a la etapa de confirmación. *Nota: Las partidas del extracto que no cuenten con OXP de comercio se formalizan como anticipos, garantizando siempre el 100% de conciliación sin excepciones y manteniendo la trazabilidad y control contable completo. Los cargos adicionales del extracto (4x1000, cuota de manejo, intereses) se consideran conciliados como parte de la OXP de Extracto, no requieren OXP de Comercio y no generan anticipos. Se debe configurar por cada tarjeta cuáles cargos adicionales maneja, para detectarlos automáticamente en el extracto.* | No |
| R07 | El sistema alerta cuando la conciliación no está completada dentro del plazo configurado previo a la fecha de pago. | Sí (por tarjeta, default 3 días) |
| R08 | Las partidas del extracto sin OXP de comercio asociada pueden resolverse mediante: (a) solicitud de radicación de OXP faltante, o (b) generación de Anticipo. | No |
| R09 | El sistema persiste las asociaciones entre patrones de descripción del extracto bancario y comercios/terceros. Estas asociaciones se establecen durante la conciliación (manual o asistida por el agente inteligente) y se aplican automáticamente para sugerir cruces en conciliaciones futuras. *Ejemplo: Si el extracto muestra "AMZN\*1X2Y3Z SEATTLE" y el usuario lo asocia a "Amazon.com", el sistema almacena el patrón "AMZN\*" vinculado a ese tercero. En extractos futuros, cualquier transacción con descripción similar (ej: "AMZN\*ABC123") será sugerida automáticamente para cruce con OXP de Amazon.com.* | No |
| R10 | La conciliación automática aplica una tolerancia en la comparación de valores. Si la diferencia está dentro de la tolerancia, se acepta el cruce automáticamente. Si la diferencia supera la tolerancia, el sistema funciona en modo asistido sugiriendo al usuario las partidas que considera correspondientes. | Sí (por empresa, default +/- 1000 COP) |

---

### Reglas de confirmación y causación

| ID | Regla | Configurable |
|----|-------|--------------|
| R11 | Las OXP pueden confirmarse manualmente por un usuario o automáticamente según configuración de la empresa. La confirmación aplica a ambos tipos de OXP pero con estados previos diferentes: (1) **OXP de Comercio:** la confirmación valida la operación de radicación y dispara la causación; posteriormente pasa a estado compensada cuando se vincula a una OXP de Extracto. (2) **OXP de Extracto:** la confirmación se habilita únicamente cuando la conciliación está completada (100%), y posteriormente se monitorea el pago. | Sí (por empresa) |
| R12 | Al confirmar una OXP, se genera automáticamente la integración con el sistema contable. | No |
| R13 | Se genera una causación individual por cada OXP de comercio. | No |
| R14 | Se genera una causación por cada OXP de extracto. | No |
| R15 | Al legalizar un anticipo, se genera la reclasificación contable correspondiente. | No |
| R16 | Al legalizar una devolución, se genera una nota crédito en el sistema contable. | No |

---

### Reglas de pago

| ID | Regla | Configurable |
|----|-------|--------------|
| R17 | Una OXP de comercio se considera **compensada** (estado operativo) cuando se vincula dentro de una OXP de Extracto. Este estado indica que la obligación individual está cubierta por el extracto del período. No representa un pago financiero, pero la información de esta compensación (cuenta, tercero, naturaleza) es la base que OXP traduce y entrega a SincoA&F para su registro contable. | No |
| R18 | Una OXP de extracto se considera **pagada** únicamente cuando el sistema contable externo (SincoA&F) confirma la ejecución del pago financiero. El sistema OXP no ejecuta pagos; solo registra y monitorea su estado. | No |

---

### Reglas de cargos adicionales

| ID | Regla | Configurable |
|----|-------|--------------|
| R19 | Los cargos adicionales del extracto (4x1000, cuota de manejo, intereses) se incluyen dentro de la OXP de extracto para que el valor a pagar coincida exactamente con el extracto. | No |

---

### Reglas de integración con SincoADPRO

| ID | Regla | Configurable |
|----|-------|--------------|
| R20 | Solo las compras que requieren formalización en SincoADPRO deben ratificarse desde ese módulo. | No |

---

### Reglas de permisos

| ID | Regla | Configurable |
|----|-------|--------------|
| R21 | Las acciones del sistema pueden restringirse según perfiles de usuario configurados. | Sí (por empresa) |
| R22 | La generación de anticipos puede limitarse a perfiles específicos. | Sí (por empresa) |
| R23 | La confirmación de OXP puede limitarse a perfiles específicos. | Sí (por empresa) |

---

## Sección 7: Qué está dentro y fuera del alcance

### Dentro del alcance

| Área | Descripción |
|------|-------------|
| **Medios de pago** | Tarjetas de crédito corporativas y tarjetas de débito prepago. |
| **Radicación / Legalización** | Registro de transacciones con soportes documentales (XML, PDF, imágenes). Extracción automática de datos desde facturas electrónicas colombianas vía SincoRE. |
| **Gestión de anticipos** | Creación de OXP sin soporte documental y posterior reclasificación al legalizar. |
| **Gestión de devoluciones** | Registro de devoluciones, asociación con OXP original y generación de nota crédito. |
| **Carga de extractos** | Carga manual de extractos bancarios en formato PDF o CSV. Extracción de transacciones y cargos adicionales. |
| **Conciliación** | Cruce automático y asistido entre OXP de comercio y extractos bancarios. Aprendizaje de relaciones comercio-descripción. |
| **Confirmación** | Flujo de confirmación manual o automático según configuración por empresa. |
| **Causación** | Generación automática de causaciones individuales por OXP de comercio y OXP de extracto hacia SincoA&F. |
| **Monitoreo de pago** | Consulta del estado de pago de OXP de extracto desde SincoA&F. |
| **Integración con SincoADPRO** | Ratificación de compras que requieren formalización en este módulo. |
| **Alertas** | Notificaciones de plazos de conciliación, montos que exceden límites configurados, y anticipos pendientes de legalización que superen el plazo establecido. |
| **Vistas de monitoreo** | Consultas del estado de OXP (pendientes, confirmadas, contabilizadas) según permisos de usuario. |
| **Configuraciones por empresa** | Preferencias de confirmación automática, tolerancias de conciliación, límites de monto, plazos de alerta. |

---

### Fuera del alcance

| Área | Descripción | Observación |
|------|-------------|-------------|
| **Procesamiento de pagos** | El sistema OXP no ejecuta pagos ni recargas. | Esta responsabilidad es de SincoA&F y el módulo de tesorería existente. |
| **Integración directa con tesorería** | No se integrará con un sistema de tesorería independiente en la primera fase. | SincoA&F gestiona el procesamiento de pago a partir de la causación. |
| **Otros medios de pago** | Pagos en efectivo, transferencias bancarias, cheques u otros medios diferentes a tarjetas de crédito y débito prepago. | Podrían considerarse en fases futuras. |
| **Gestión de tarjetas** | Alta, baja o modificación de tarjetas corporativas. | Responsabilidad del módulo de Tesorería - Medios de pago. |
| **Gestión de comercios/proveedores** | Alta, baja o modificación de terceros. | Responsabilidad del servicio transversal de Gestión de Terceros. |
| **Gestión de usuarios** | Creación de usuarios y asignación de permisos. | Se asume integración con sistema de gestión de identidad existente. |

---

### Dependencias externas a construir

El sistema OXP será el primer módulo de la nueva arquitectura del ERP. Para su funcionamiento, requiere la construcción de los siguientes servicios transversales con responsabilidades claramente definidas:

| Dependencia | Descripción | Impacto en el sistema OXP |
|-------------|-------------|---------------------------|
| **Gestión de Terceros** | Servicio transversal que centraliza la información de comercios, proveedores y entidades externas. Incluye identificación tributaria, razón social, datos de contacto y clasificación. | El sistema OXP consumirá este servicio para identificar comercios/proveedores en las transacciones. |
| **Tesorería - Medios de pago** | Componente que gestiona los datos maestros de tarjetas de crédito y débito prepago (número, tipo, fecha de corte, fecha de pago, límites, entidad bancaria). | El sistema OXP consumirá este servicio para obtener la información de las tarjetas. |
| **Sistema de cálculo de impuestos** | Sistema transversal que determina los impuestos aplicables a las transacciones según la normativa colombiana. | El sistema OXP consumirá este servicio para obtener los valores de impuestos en las OXP. |
| **Reglas de traducción contable** | Lógica que convierte los conceptos de OXP (comercio, extracto, anticipos, devoluciones, cargos) en asientos contables para SincoA&F. | El sistema OXP aplicará estas reglas al generar las causaciones. |

Cada una de estas dependencias requiere su propia definición de alcance independiente.

---

### Visión arquitectónica

```
┌─────────────────────────────────────────────────────────────────┐
│                    Servicios Transversales                       │
├─────────────────┬─────────────────┬─────────────────────────────┤
│  Gestión de     │  Cálculo de     │  Reglas de Traducción       │
│  Terceros       │  Impuestos      │  Contable                   │
└────────┬────────┴────────┬────────┴──────────────┬──────────────┘
         │                 │                       │
         ▼                 ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Sistema OXP                                 │
└─────────────────────────────────────────────────────────────────┘
         ▲                                         │
         │                                         ▼
┌─────────────────┐                    ┌─────────────────┐
│  Tesorería      │                    │  SincoA&F       │
│  (Medios de     │                    │  (Contable)     │
│   pago)         │                    │                 │
└─────────────────┘                    └─────────────────┘
```

Esta arquitectura de servicios desacoplados con responsabilidades claras permite:
- Evitar la duplicación de datos y lógica entre módulos
- Facilitar el mantenimiento y evolución independiente de cada servicio
- Garantizar consistencia en la información transversal
- Servir como base para la modernización progresiva de los demás módulos de SincoERP

---

## Sección 8: Beneficios esperados

### Beneficios operativos

| Problema actual | Beneficio esperado |
|-----------------|-------------------|
| Proceso de radicación no formalizado | Flujo estructurado para registro de transacciones con soportes documentales, garantizando trazabilidad y completitud. |
| Conciliación propensa a errores | Conciliación automática y asistida que minimiza errores y reduce el esfuerzo manual. El sistema aprende de las conciliaciones previas para mejorar la precisión. |
| Falta de visibilidad | Vistas consolidadas del estado de las OXP en tiempo real, permitiendo seguimiento y control efectivo del proceso. |
| Alta carga operativa | Automatización de tareas repetitivas: extracción de datos de facturas XML, cruce de partidas, generación de causaciones. |
| Cumplimiento normativo con reprocesos | Alertas de plazos para documentos soporte (6 días hábiles) y conciliación (previo a fecha de pago), reduciendo reprocesos y riesgo de hallazgos de auditoría. |
| Restricción en el uso del medio de pago | Proceso simplificado que habilita el uso de tarjetas para más tipos de compras, accediendo a mejores precios y productos. |

---

### Beneficios de control

| Beneficio | Descripción |
|-----------|-------------|
| **Trazabilidad completa** | Cada transacción tiene un registro desde la radicación hasta el pago, con los soportes documentales asociados. |
| **Segregación de funciones** | Roles diferenciados (solicitante, radicador, confirmador) con permisos configurables por empresa. |
| **Auditoría facilitada** | Información centralizada y estructurada que simplifica los procesos de auditoría interna y externa. |
| **Gestión de anticipos** | Control sobre las OXP sin soporte documental, con seguimiento hasta su legalización y reclasificación contable. |

---

### Beneficios de integración

| Beneficio | Descripción |
|-----------|-------------|
| **Causación automática** | Eliminación de la doble digitación mediante integración directa con SincoA&F. |
| **Datos consistentes** | Consumo de servicios transversales (Terceros, Impuestos) que garantizan información unificada en todo el ecosistema. |
| **Monitoreo de pagos** | Visibilidad del estado de pago consultando directamente el sistema contable. |

---

### Beneficios estratégicos

| Beneficio | Descripción |
|-----------|-------------|
| **Innovación en procesos** | Transformación de procesos manuales y propensos a errores hacia procesos ágiles, automatizados y seguros, estableciendo un nuevo estándar operativo para la compañía. |
| **Base para modernización del ERP** | El sistema OXP establece la arquitectura de servicios desacoplados que servirá como modelo para la reconstrucción progresiva de los demás módulos de SincoERP. |
| **Escalabilidad** | Diseño preparado para incorporar más compañías, tarjetas y volumen de transacciones. |
| **Extensibilidad** | Arquitectura que permite agregar nuevos medios de pago y funcionalidades en fases futuras. |

---

### Indicadores de éxito

| Indicador | Línea base | Meta | Plazo |
|-----------|------------|------|-------|
| Tiempo promedio de radicación | *Por definir en fase de implementación* | Reducción del 50% | 6 meses post-implementación |
| Porcentaje de conciliación automática | 0% (todo manual) | ≥ 70% | 6 meses post-implementación |
| Errores de conciliación por período | *Por definir en fase de implementación* | Reducción del 80% | 6 meses post-implementación |
| Horas/hombre mensuales dedicadas al proceso | *Por definir en fase de implementación* | Reducción del 40% | 6 meses post-implementación |
| Documentos soporte emitidos fuera de plazo | *Por definir en fase de implementación* | 0 documentos fuera de plazo | 3 meses post-implementación |
| Hallazgos de auditoría relacionados al proceso | *Por definir en fase de implementación* | 0 hallazgos | Siguiente auditoría |
| Volumen de transacciones con tarjeta | *Por definir en fase de implementación* | Incremento del 20% | 12 meses post-implementación |

*Nota: Las líneas base se establecerán durante la fase de implementación mediante levantamiento de datos del proceso actual.*

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Enero 2026 | Versión inicial del documento de definición de alcance |
