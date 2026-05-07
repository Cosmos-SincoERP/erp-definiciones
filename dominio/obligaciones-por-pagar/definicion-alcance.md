# Definición de Alcance — Obligaciones por Pagar

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



## Sección 1: Definición, Contexto actual y problema a resolver

### Definición

OXP se define como un sistema de dominio que gobierna el ciclo de vida operativo completo de las Obligaciones por Pagar, incorporando, estructurando y gestionando la información asociada a los hechos económicos de egreso que dan origen a dichas obligaciones (compras, gastos y costos), desde su radicación hasta su cierre operativo, y habilitando su correcta traducción hacia los procesos contables y financieros externos.

OXP gestiona tres tipos de obligaciones por ciclo de vida:

- **OXP de Comercio:** Obligación por pagar originada por un hecho económico de egreso (compra, gasto o costo). Independiente del medio de pago — puede pagarse vía extracto bancario (tarjeta de crédito/débito), pago directo desde Tesorería (crédito a proveedor, transferencia, contado) o pagos mixtos.
- **OXP de Extracto:** Obligación consolidada que agrupa OXP de Comercio de un período, ligada a un producto financiero (tarjeta de crédito o débito). Incluye devoluciones y cargos financieros aplicables.
- **OXP de Caja Menor** *(F2):* Obligación de menor cuantía operada bajo esquema de fondo fijo con rendición y reembolso.

### Contexto actual

La compañía genera obligaciones por pagar desde múltiples orígenes:

- **Compras con tarjeta de crédito corporativa:** Compras generales de la operación, pagadas mediante extracto bancario.
- **Compras con tarjeta de débito prepago:** Insumos en obras, suministros, gastos de representación, pagadas mediante extracto bancario.
- **Facturas de proveedores a crédito:** Compras de bienes y servicios con plazo de pago acordado, pagadas vía Tesorería.
- **Pagos de contado:** Servicios públicos, pagos inmediatos, pagados vía Tesorería.

Todas estas transacciones generan obligaciones por pagar (OXP) que deben ser radicadas con sus soportes documentales y causadas contablemente. Las que se asocian a productos financieros (tarjetas) adicionalmente requieren conciliación contra el extracto bancario.

Actualmente, el proceso de tarjetas corporativas — que es el más desgastante — se gestiona de forma manual mediante archivos de Excel, donde los usuarios concilian las transacciones contra el extracto de la entidad bancaria y posteriormente registran la causación en el sistema contable.

### Problema actual

La gestión manual presenta los siguientes desafíos:

1. **Proceso de radicación no formalizado:** No existe un flujo estructurado para recibir y registrar los soportes de las compras, lo que genera demoras en la causación por documentación incompleta o faltante.

2. **Conciliación propensa a errores:** El cruce manual entre transacciones y extractos bancarios en Excel dificulta la identificación de diferencias y aumenta el riesgo de errores.

3. **Falta de visibilidad:** No se cuenta con una vista consolidada y en tiempo real del estado de las obligaciones por pagar, dificultando el seguimiento y control.

4. **Alta carga operativa:** El esfuerzo manual requerido para conciliar, causar y dar seguimiento a las OXP consume tiempo significativo del equipo. Esto incluye la gestión individual de cada recibo o documento soporte de las transacciones.

5. **Cumplimiento normativo con reprocesos:** La DIAN exige la emisión de documentos soporte para compras del exterior que no generan factura electrónica. Estos documentos deben formalizarse dentro de los 6 días hábiles posteriores a la fecha de la transacción, es decir, antes de recibir el extracto bancario. Actualmente se cumple con este requisito, pero el proceso manual genera reprocesos posteriores que incrementan la carga operativa y pueden derivar en hallazgos de auditoría.

6. **Restricción en el uso del medio de pago:** Debido a lo desgastante del proceso actual, la compañía limita los tipos de compras que se realizan con tarjeta de crédito, perdiendo oportunidades como acceder a mejores precios de otros proveedores o adquirir mejores productos disponibles únicamente a través de este medio de pago.

### Implementación inicial

El sistema se implementará inicialmente en una compañía piloto que maneja:
- 2 tarjetas de crédito corporativas (~100 transacciones mensuales)
- ~1000 facturas de proveedores a crédito mensuales
- ~50 pagos de contado mensuales (arriendos, servicios públicos)

El diseño contempla escalabilidad para compañías con mayor volumen de transacciones y la incorporación progresiva de todos los tipos de OXP definidos en las fases de implementación (Sección 8).

**Nota:** El sistema se diseña inicialmente para Colombia, con arquitectura extensible para soportar otros países en el futuro.

---

## Sección 2: Glosario de términos

| Término | Definición |
|---------|------------|
| **OXP** | Obligación por Pagar. Registro que representa un compromiso de pago de la compañía. |
| **OXP de Comercio** | Obligación por pagar originada por un hecho económico de egreso (compra, gasto o costo). Independiente del medio de pago: puede originarse en compra con tarjeta (crédito o débito), factura de proveedor a crédito, pago de contado u otro medio. La forma de pago se resuelve automáticamente durante la radicación (por los datos extraídos del soporte, por el contexto del tercero/concepto, o por asignación del usuario) y determina el flujo de resolución financiera dentro del sistema (vía extracto bancario o pago directo desde Tesorería). |
| **OXP de Extracto** | Obligación por pagar consolidada que agrupa las OXP de comercio de un período, incluyendo las devoluciones y los cargos adicionales aplicables según el medio de pago. |
| **Pagada (OXP de Extracto)** | Estado que indica que el sistema contable externo (SincoA&F) ha confirmado la ejecución del pago financiero correspondiente al extracto. |
| **Radicación** | Fase operativa inicial que comprende tres momentos: (1) **extracción** — obtención de datos estructurados desde el soporte documental, delegada a servicios de infraestructura transversal, (2) **clasificación** — determinación inteligente del origen de la obligación (directa o de sub-dominio de gestión) y resolución de las referencias fiscales (clasificacionTributaria, conceptoPago), (3) **registro** — incorporación al dominio OXP como obligación por pagar en estado pendiente. La extracción es un servicio de infraestructura transversal; la clasificación y el registro son responsabilidad del dominio OXP. La radicación puede iniciar con información parcial y contempla la completitud progresiva de datos y soportes requeridos. |
| **Anticipo** | Obligación por pagar que puede o no contar con soportes preliminares (ej: cuenta de cobro) y requiere la entrega de dinero al comercio/proveedor. Tiene ciclo de vida independiente con dos dimensiones de resolución: (1) **pago** — cómo se cubrió el desembolso (vinculación a extracto, pago directo o devolución), y (2) **regularización** — contra qué OXP de Comercio se justifica el gasto. Estados: Vigente (ambos saldos pendientes) → Pagado (desembolso cubierto) / Regularizado (justificación completa) → Cerrado (ambos resueltos). También puede cancelarse completamente mediante reversa total si aún no tiene pagos ni regularizaciones aplicadas (estado Reversado). Un anticipo también puede nacer automáticamente por excedente de devolución, en cuyo caso inicia directamente en estado Pagado. Se aplican políticas de plazo configurables para alertar sobre anticipos que excedan el tiempo permitido sin regularizar. Aplica para ambos medios de pago. |
| **Regularización de Anticipos** | Proceso coordinado mediante el cual un anticipo se cruza contra una o más OXP de Comercio confirmadas del mismo tercero. Cada regularización reduce el saldo por regularizar del anticipo y aplica un pago sobre la OXP de Comercio destino. La regularización solo puede realizarse contra OXP de Comercio en estado Confirmada o posterior, donde el valor neto ya es estable. Un anticipo puede regularizarse contra múltiples OXP de Comercio en operaciones independientes (1:N). La regularización genera la información estructurada necesaria para la amortización contable. |
| **Amortización del Anticipo** | Efecto contable mediante el cual el saldo del anticipo se reclasifica total o parcialmente a las cuentas de gasto o costo definitivas. La amortización es ejecutada por el sistema contable externo (SincoA&F) a partir de la información estructurada entregada por OXP. Equivale al concepto internacional de *Down Payment Clearing* (SAP). |
| **Devolución** | Reintegro de dinero del comercio hacia la compañía. Se registra con valor positivo representando la magnitud del crédito; la naturaleza contable (nota crédito) la determina el tipo, no el signo. Tiene ciclo de vida independiente (Pendiente → Confirmada → Causada) y se clasifica en 3 tipos según el OXP origen: (1) **Comercio** — devuelve conceptos de gasto de una OXP de Comercio; el crédito se aplica contra el saldo pendiente de pago, y si lo excede, genera un anticipo por el excedente. (2) **Extracto** — devuelve cargos financieros cobrados en un extracto anterior. (3) **Anticipo** — reversa total de un anticipo sin pagos ni regularizaciones. La devolución puede aparecer en un período diferente al del OXP original. |
| **Reversa de Anticipo** | Cancelación total de un anticipo que aún no tiene pagos ni regularizaciones aplicadas. Se formaliza mediante una devolución tipo Anticipo. Solo aplica cuando el anticipo está en estado Vigente sin cruces previos. El anticipo pasa a estado terminal Reversado. |
| **Excedente de Devolución** | Cuando el valor de una devolución excede el saldo pendiente de pago de la OXP de Comercio origen, el sistema genera automáticamente un anticipo por la diferencia. Este anticipo nace en estado Pagado (ya cubierto por la devolución) y pendiente de regularización. |
| **Pago Directo** | Pago de una OXP de Comercio confirmado por el sistema contable externo (SincoA&F) para formas de pago diferentes a tarjeta de crédito. Aplica únicamente sobre OXP en estado Causada. Complementa el flujo estándar de pago vía extracto bancario. |
| **Nota Crédito** | Documento contable que representa una devolución o ajuste a favor de la compañía. Se genera al registrar una devolución. |
| **Documento Soporte Electrónico** | Documento requerido por el ente regulador para respaldar compras que no generan factura electrónica, como por ejemplo compras en el exterior o proveedores informales. |
| **TRM (Tasa Representativa del Mercado)** | Tasa de cambio oficial publicada por el Banco de la República de Colombia. Utilizada para valorar transacciones en moneda extranjera. |
| **Moneda Funcional** | Moneda base de operación del sistema en el país donde opera la empresa (COP para Colombia). Todas las obligaciones por pagar se expresan y liquidan en moneda funcional. Cuando una transacción se origina en moneda extranjera, el sistema almacena el valor original y lo convierte a moneda funcional. Concepto alineado con NIIF/NIC 21. |
| **Diferencia en Cambio** | Variación en el valor de una transacción en moneda extranjera ocasionada por fluctuaciones en la TRM. En el contexto de OXP se presentan dos momentos: (1) **En conciliación:** diferencia entre la TRM de radicación de la OXP de Comercio y la TRM del extracto aplicada a la partida. El sistema genera automáticamente un **concepto de ajuste por diferencia en cambio** sobre la OXP de Extracto por cada OXP de Comercio en moneda extranjera: si la TRM subió, se clasifica como gasto financiero; si la TRM bajó, se clasifica como ingreso financiero. Estos conceptos permiten el cruce exacto sin crear una nueva OXP. (2) **Al desembolso:** diferencia entre la TRM del extracto (radicación) y la TRM al momento del pago efectivo. Esta diferencia es responsabilidad del dominio de Tesorería — OXP no ejecuta pagos, solo registra y monitorea su estado. |
| **Partida en Disputa** | Marca especial para partidas del extracto bancario que no pueden conciliarse debido a errores bancarios, fraudes potenciales o transacciones no reconocidas. Permite alcanzar el 100% de conciliación operativa sin generar anticipos. Su resolución posterior puede ser: (1) **Descartada:** la partida se marca como descartada cuando el banco reversa la transacción, compensándola contra la línea de "Reverso Bancario" en un extracto futuro; (2) **Reclasificación Contable:** cuando se identifica el gasto real y se radica la OXP de Comercio correspondiente, el sistema vincula la partida en disputa con la nueva OXP mediante reclasificación contable, sin generar documentos duplicados. |
| **Distribución de Costos (Split)** | Funcionalidad que permite que una sola OXP de Comercio afecte múltiples destinos contables. Una OXP puede distribuirse porcentualmente o por valor entre diferentes centros de costo o cuentas contables, generando N registros de causación a partir de 1 OXP. |
| **Concepto** | Componente estructurado de una OXP que representa un elemento con significado de negocio y destino contable. Una OXP puede contener múltiples conceptos de diferentes tipos. El dominio OXP es responsable de gestionar los conceptos y realizar su traducción contable mediante el servicio de Traducción Contable (cuenta, centro de costo, tercero, naturaleza). Equivale al término *Distribution* en Oracle ERP o *Line Item* en SAP. |
| **Tipo de Concepto** | Clasificación que determina la naturaleza de un concepto dentro de una OXP. Tipos soportados: Gasto/Costo (compra principal), Impuesto, Retención, Diferencia en Cambio, Ajuste por Tolerancia, Cargo Financiero (4x1000, cuota de manejo, intereses). |
| **Causación** | Registro contable de una obligación por pagar en el sistema contable de la compañía. |
| **Fecha de Causación** | Fecha utilizada para el registro contable del gasto. Corresponde a la **fecha del soporte/factura**, respetando el principio de devengo (NIIF). Esto garantiza el reconocimiento del gasto en el período contable correcto, independientemente de cuándo se refleje en el extracto bancario. |
| **Fecha de Compensación** | Fecha del extracto bancario utilizada exclusivamente para el asiento de pago (cruzar la cuenta por pagar contra la cuenta de banco). No afecta el período de reconocimiento del gasto. |
| **Ventana de Validación de Duplicidad** | Período de 24 meses calendario contados hacia atrás desde la fecha de radicación, dentro del cual no se permite la repetición de un número de factura para un mismo proveedor. |
| **Extracto Bancario** | Documento emitido por la entidad financiera que detalla las transacciones y cargos de la tarjeta en un período determinado. Puede contener transacciones en múltiples monedas (ej: tarjetas con facturación segmentada por moneda). Cuando un extracto contiene partidas en monedas mixtas, las partidas en moneda extranjera se convierten a moneda funcional al momento de la radicación `[R05d]`. Si todas las partidas están en una sola moneda (sea COP, USD u otra), el extracto opera en esa moneda directamente. |
| **Conciliación** | Proceso de cruce y verificación entre las OXP de comercio registradas y las transacciones reportadas en el extracto bancario. |
| **Recarga** | Proceso de reposición de fondos a una tarjeta de débito prepago, originado a partir de las transacciones radicadas y causadas. |
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
| **Autorizador** | Usuario responsable de autorizar ajustes que superan la tolerancia definida en R10, resolver Partidas en Disputa y manejar las excepciones que se presentan durante el proceso. Tiene la facultad adicional de gestionar el tablero de Partidas en Disputa y autorizar cierres de OXP que presenten discrepancias superiores a la tolerancia definida, dejando siempre un registro de auditoría. También es responsable de auditar y liberar los registros bloqueados por la regla R26, determinando si una coincidencia de NIT y número corresponde a una numeración reciclada legalmente (fuera de la ventana de 24 meses) o a un intento de doble cobro. |

Cualquier usuario puede acceder a vistas de monitoreo (OXP pendientes, confirmadas, causadas) según los permisos asignados.

### Actores externos (sistemas integrados)

| Actor | Descripción |
|-------|-------------|
| **Entidad Bancaria** | Provee los extractos de las tarjetas de crédito y débito prepago en formato PDF o CSV, los cuales son cargados al sistema por un usuario. |
| **SincoRE (Recepción Electrónica)** | Transforma facturas electrónicas en información estructurada y la expone mediante API para consumo del sistema OXP. |
| **Sistema Contable** | Recibe automáticamente las causaciones de las OXP una vez estas son confirmadas. |
| **Sistema de Tesorería** | Gestiona los pagos de las OXP de extracto (tarjetas de crédito) y las recargas (tarjetas débito prepago). |
| **Servicio de extracción de datos** | Servicio transversal de infraestructura que interpreta soportes documentales no electrónicos (PDF, imágenes) para extraer datos estructurados (tercero, valor, conceptos, tributos). Análogo a SincoRE para documentos que no son factura electrónica. Otros sub-dominios (CXC, etc.) podrán consumir el mismo servicio. |

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

El sistema OXP maneja cuatro flujos con ciclos de vida independientes:

**Nota conceptual:** Los diagramas siguientes representan las **fases del ciclo de vida** de cada tipo de obligación dentro del dominio OXP, no una secuencia de pasos operativos ni un workflow de tareas de usuario. Cada fase describe una etapa de evolución de la obligación que habilita capacidades del sistema y transiciones de estado. El sistema OXP gobierna la obligación desde su incorporación (radicación) hasta su cierre, actuando como sistema de dominio integral y no como un subproceso aislado.

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           FLUJO OXP DE COMERCIO                                  │
│                                                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │ 1. Radica-   │    │ 2. Confir-   │    │ 3. Causa-    │    │ 4. Pagada    │   │
│  │    ción      │───▶│    mación    │───▶│    ción      │───▶│              │   │
│  └──────────────┘    └──────────────┘    └──────────────┘    └──────┬───────┘   │
│                                                                      │           │
└──────────────────────────────────────────────────────────────────────┼───────────┘
                                                                       │
                                                              (vinculación)
                                                                       ↕
┌──────────────────────────────────────────────────────────────────────┼─────────────────────────────┐
│                                    FLUJO OXP DE EXTRACTO                                           │
│                                                                                                    │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐ │
│  │ 1. Radica-   │    │ 2. Conci-    │    │ 3. Confir-   │    │ 4. Causa-    │    │ 5. Pago /    │ │
│  │    ción      │───▶│   liación    │───▶│   mación     │───▶│   ción       │───▶│   Recarga    │ │
│  └──────────────┘    └──────────────┘    └──────────────┘    └──────────────┘    └──────────────┘ │
│                                                                                                    │
└────────────────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────┐
│                           FLUJO ANTICIPO                                         │
│                                                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │ 1. Registro  │    │ 2. Pago      │    │ 3. Regulari- │    │ 4. Cerrado   │   │
│  │   (Vigente)  │───▶│              │───▶│   zación     │───▶│              │   │
│  └──────┬───────┘    └──────────────┘    └──────────────┘    └──────────────┘   │
│         │                                                                        │
│         └──── Reversa total ───▶ Reversado                                       │
└─────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────┐
│                           FLUJO DEVOLUCIÓN                                       │
│                                                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐                       │
│  │ 1. Radica-   │    │ 2. Confir-   │    │ 3. Causa-    │                       │
│  │    ción      │───▶│    mación    │───▶│    ción      │                       │
│  └──────────────┘    └──────────────┘    └──────────────┘                       │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

**OXP de Comercio:** Se radica, confirma y causa de forma independiente. El pago se registra cuando se vincula a una OXP de Extracto (conciliación), se regulariza un anticipo contra ella, se confirma un pago directo (SincoA&F), o se aplica una devolución. La OXP pasa a Pagada cuando su saldo pendiente de pago llega a cero.

**OXP de Extracto:** Se radica, se concilia al 100% (vinculando OXP de Comercio, cubriendo con anticipos/devoluciones, o marcando partidas en disputa), se confirma, se causa contablemente, y finalmente se registra el pago cuando SincoA&F lo confirma.

**Anticipo:** Se registra en estado Vigente con dos dimensiones de resolución independientes: pago (vinculación a extracto o pago directo) y regularización (cruce contra OXP de Comercio). Cuando ambas dimensiones se resuelven, pasa a Cerrado. Puede cancelarse completamente mediante reversa total si aún no tiene cruces.

**Devolución:** Se radica referenciando un OXP origen (Comercio, Extracto o Anticipo), se confirma (aplicando el crédito sobre el origen), y se causa contablemente.

---

### Relación entre etapas y estados

Las **etapas** son las fases del ciclo de vida de la obligación; cada una agrupa las actividades que se realizan en ella. Los **estados** son las condiciones de cada OXP en un momento dado (cómo está). Al completar una etapa, la OXP transiciona de estado.

| Etapa (fase) | OXP Comercio | OXP Extracto | Anticipo | Devolución |
|-------------------|---|---|---|---|
| Radicación / Registro | Pendiente | Pendiente | Vigente | Pendiente |
| Confirmación | Confirmada | — | — | Confirmada |
| Rechazo por Confirmador | Devuelta | — | — | — |
| Causación | Causada | — | — | Causada |
| Conciliación | Pagos aplicados vía extracto | Conciliada (100%) | — | — |
| Confirmación (Extracto) | — | Confirmada | — | — |
| Causación (Extracto) | — | Causada | — | — |
| Pago / Vinculación | Pagada (saldo = 0) | Pagada | Pagado | — |
| Regularización | Pago aplicado vía anticipo | — | Regularizado | — |
| Cierre | — | — | Cerrado (ambos saldos = 0) | — |
| Reversa | — | — | Reversado | — |
| Pago directo (SincoA&F) | Pagada (saldo = 0) | — | — | — |

**Nota de configuración:** Según la configuración de la empresa, la causación puede ejecutarse de dos formas: (1) automáticamente como consecuencia directa de la confirmación, o (2) como una acción independiente realizada por un usuario sobre las OXP ya confirmadas.

---

### Etapa 1: Radicación

| Aspecto | Descripción |
|---------|-------------|
| **Canales de entrada** | Los soportes documentales llegan por múltiples canales agnósticos al origen de la obligación: (1) factura electrónica XML vía SincoRE, (2) PDF e imágenes interpretados por servicio de extracción de datos, (3) CSV estructurado (extractos bancarios), (4) carga manual por usuario. Todos los canales entregan datos extraídos sin determinar si la obligación es directa o de sub-dominio de gestión — esa clasificación es responsabilidad de OXP. |
| **Disparador - OXP de Comercio** | Empleado realiza una compra con tarjeta de crédito o débito prepago. |
| **Disparador - OXP de Extracto** | La entidad bancaria emite el extracto del período. |
| **Entrada - OXP de Comercio** | Soporte documental (factura XML, PDF, imagen) con datos de la transacción. |
| **Entrada - OXP de Extracto** | Archivo del extracto en formato PDF o CSV con la relación de transacciones del período. |
| **Proceso - OXP de Comercio** | El solicitante o radicador registra la transacción en el sistema adjuntando el soporte. Si el soporte es XML, se extraen automáticamente los datos relevantes. |
| **Proceso - OXP de Extracto** | El solicitante o radicador carga el extracto en el sistema. Se extraen las transacciones y cargos adicionales (4x1000, cuota de manejo, intereses si aplica). |
| **Salida - OXP de Comercio** | OXP de Comercio creada en estado pendiente. |
| **Salida - OXP de Extracto** | OXP de Extracto creada con el detalle de transacciones y cargos del período. |
| **Variante - Anticipo** | Si no se cuenta con el soporte, se crea un Anticipo en estado Vigente con dos saldos independientes: saldo por pagar (desembolso) y saldo por regularizar (justificación). También puede nacer automáticamente por excedente de devolución [R32], en cuyo caso inicia en estado Pagado. |
| **Variante - Devolución** | Si la transacción es una devolución, se radica como Devolución en estado Pendiente, referenciando el OXP origen. Según el tipo: (1) Comercio — referencia OXP de Comercio y registra conceptos devueltos. (2) Extracto — referencia OXP de Extracto anterior y registra cargos financieros devueltos. (3) Anticipo — referencia un anticipo para reversa total [R34]. La devolución puede aparecer en un período diferente al del OXP original. |
| **Variante - Moneda Extranjera (OXP de Comercio)** | Para compras del exterior, la OXP de Comercio se radica con la TRM del día de la transacción. El sistema siempre almacena el valor original de la compra en la moneda de origen y el valor convertido a la moneda funcional del país donde opera el sistema. |
| **Variante - Moneda Extranjera (OXP de Extracto)** | Un extracto bancario puede contener partidas en una o varias monedas. Si todas las partidas están en la misma moneda, el extracto opera en esa moneda (aplicando `ValorMonetario` con TRM y equivalente en moneda funcional si es moneda extranjera, igual que OxpComercio). Cuando el extracto contiene partidas en **monedas mixtas** (ej: tarjetas con facturación segmentada por moneda), el sistema convierte las partidas en moneda extranjera a moneda funcional usando la TRM informada por la entidad bancaria en el extracto. Cada partida conserva su valor original, moneda de origen y TRM para trazabilidad. En este caso, el OxpExtracto opera en moneda funcional desde la radicación `[R05d]`. |
| **Variante - Distribución de Costos (Split)** | Durante la radicación (o al regularizar un anticipo), el usuario puede realizar una distribución porcentual o por valor del costo entre diferentes centros de costo o cuentas contables. Una OXP de Comercio puede generar N registros de causación de costo. El sistema valida que la suma de las distribuciones sea igual al 100% del valor de la OXP. |
| **Variante - Clasificación inteligente de origen** | Al radicar una OXP de Comercio, el sistema determina si la obligación es directa (gasto sin sub-dominio de gestión) o pertenece a un sub-dominio de gestión (Compras, Arrendamiento, etc.). La clasificación es automática y asistida — el sistema sugiere el origen más probable y el usuario siempre puede corregir la sugerencia. Si es directa, las referencias fiscales se resuelven desde el catálogo de gasto directo de OXP. Si es de gestión, el sub-dominio origen aporta las referencias fiscales ya resueltas desde su catálogo. |
| **Variante - Tributos declarados por el proveedor** | Cuando el soporte documental incluye tributos declarados por el proveedor (desglose fiscal en factura electrónica o en soporte extraído), estos se registran como referencia del proveedor. El sub-dominio de Impuestos calcula los tributos según las reglas fiscales propias de la empresa. Si hay discrepancia entre lo declarado por el proveedor y lo calculado por Impuestos, el sistema genera una alerta para revisión del usuario. |

---

### Etapa 2: Conciliación

| Aspecto | Descripción |
|---------|-------------|
| **Disparador** | OXP de Extracto disponible para vinculación. |
| **Entrada** | OXP de Comercio radicadas y OXP de Extracto. |
| **Proceso** | El sistema realiza una conciliación automática entre las transacciones del extracto y las OXP de comercio registradas. Para las partidas que no logra conciliar automáticamente, el usuario puede completar la conciliación manualmente. |
| **Criterios de conciliación automática** | El sistema utiliza una combinación de criterios: (1) **Comercio:** detección asistida por un agente inteligente que sugiere asociaciones basadas en el contexto histórico de transacciones y las descripciones del extracto; el usuario puede también realizar la relación manualmente. Una vez establecida la asociación, el sistema la persiste para aplicarla automáticamente en futuras conciliaciones. (2) **Valor:** comparación del monto radicado considerando variaciones por impuestos. (3) **Fecha:** correspondencia con la fecha de la transacción. |
| **Tipos de vinculación soportados** | El sistema soporta dos tipos de vinculación: (1) **1:1 (Simple):** una OXP de Comercio se vincula con una partida del extracto; (2) **N:1 (Agrupación):** múltiples OXP de Comercio se vinculan contra una sola partida del extracto, para casos donde una compra tiene varios registros o documentos de soporte pero el banco la procesa como un solo cobro. La suma de las OXP debe coincidir con la partida del extracto dentro de la tolerancia definida en R10. *Las relaciones 1:N y N:M no aplican porque no representan escenarios reales de negocio.* |
| **Cobertura por Anticipo** | Una partida del extracto puede cubrirse con un anticipo existente del mismo tercero, en lugar de vincularse a una OXP de Comercio. La partida queda resuelta y el anticipo registra el pago correspondiente (reduce su saldo por pagar). |
| **Cobertura por Devolución** | Una partida del extracto que corresponde a un crédito bancario puede cubrirse con una devolución registrada en el sistema. La partida queda resuelta. |
| **Estados de conciliación** | **Inicio:** proceso no iniciado. **Parcialmente conciliada:** algunas partidas resueltas, otras pendientes. **Conciliada:** 100% de las partidas del extracto resueltas. Una partida se considera resuelta cuando está vinculada a OXP de Comercio, cubierta por anticipo, cubierta por devolución, marcada en disputa, o descartada. |
| **Regla de avance** | El extracto debe estar en estado "Conciliado" (100%) antes de pasar a la etapa de confirmación. |
| **Manejo de Diferencia en Cambio** | Para cada OXP de Comercio en moneda extranjera, si existe diferencia entre el valor radicado (TRM del día de la transacción) y el valor en el extracto (TRM del día de corte), el sistema genera automáticamente un **concepto de ajuste por diferencia en cambio** sobre la OXP de Extracto. Se genera un concepto de ajuste por cada OXP de Comercio en moneda extranjera. Esto permite el cruce exacto sin crear nuevas OXP. |
| **Alerta de plazo** | El sistema alerta cuando la conciliación no está completada dentro del plazo configurado previo a la fecha de pago (por defecto 3 días). Este plazo es configurable en las preferencias de cada tarjeta. |
| **Salida - Conciliación exitosa** | OXP de Comercio con pagos aplicados vía extracto (la vinculación reduce el saldo pendiente de pago de la OXP de Comercio), OXP de Extracto en estado **Conciliada**. Para cada OXP de Comercio en moneda extranjera con diferencia de cambio, se incluyen los conceptos de ajuste correspondientes sobre la OXP de Extracto. |
| **Salida - Partidas sin conciliar** | El sistema notifica al usuario las transacciones del extracto sin OXP de comercio asociada. El usuario decide entre: (a) gestionar la solicitud de radicación de las OXP faltantes, (b) generar Anticipos, (c) cubrir con anticipo existente, (d) cubrir con devolución registrada, o (e) marcar como **Partida en Disputa** (para errores bancarios, fraudes potenciales o transacciones no reconocidas). |
| **Salida - Partida en Disputa** | La marca de "Partida en Disputa" permite alcanzar el 100% de conciliación operativa sin generar anticipos. Posteriormente, la partida en disputa puede resolverse de dos formas: (1) **Descartada:** cuando el banco realiza el reverso, la partida se compensa contra la línea de "Reverso Bancario" en un extracto futuro y cierra su ciclo; (2) **Reclasificación Contable:** cuando se identifica el gasto real y se radica la OXP de Comercio correspondiente, el sistema vincula la partida en disputa del extracto original con la nueva OXP mediante reclasificación contable, sin generar documentos duplicados ni nueva deuda. |

---

### Etapa 3: Confirmación y Causación

| Aspecto | Descripción |
|---------|-------------|
| **Disparador** | OXP conciliadas listas para confirmar. |
| **Entrada** | OXP de Comercio y OXP de Extracto pendientes de confirmación (extracto con conciliación 100% completada). |
| **Proceso** | El confirmador revisa y confirma las OXP (o se confirman automáticamente según configuración de la empresa). La confirmación es la condición que habilita la transición hacia la causación e integra el proceso con el sistema contable (SincoA&F). Según la configuración de la empresa, la causación puede ejecutarse (1) automáticamente como consecuencia directa de la confirmación o (2) como una acción independiente realizada por un usuario sobre las OXP ya confirmadas. |
| **Proceso - Rechazo** | Si el confirmador no aprueba una OXP, esta pasa a estado **"Devuelta"** y retorna a la bandeja del radicador. El confirmador debe registrar obligatoriamente un **"Motivo de Rechazo"** que explique la razón de la devolución. El radicador puede entonces corregir la OXP y reenviarla a confirmación, o descartarla según corresponda. |
| **Causación** | Se genera una causación individual por cada OXP de comercio y una causación por la OXP de extracto. La causación de la OXP de Extracto tiene como propósito registrar el total del extracto contra la entidad bancaria o el medio de pago, incluyendo los cargos adicionales (4x1000, cuota de manejo, intereses). Esto no representa doble causación respecto a las OXP de Comercio, ya que cada una tiene naturaleza contable diferente. |
| **Salida** | OXP en estado confirmado. Causaciones registradas en el sistema contable externo. |
| **Estado Causada** | Una OXP se considera **causada** cuando el sistema contable externo (SincoA&F) confirma el registro exitoso de la causación enviada por OXP. |

---

### Etapa 4: Pago / Recarga

| Aspecto | Descripción |
|---------|-------------|
| **Disparador** | OXP de Extracto causada. |
| **Entrada** | OXP de Extracto confirmada y causada. |
| **Proceso - OXP de Comercio** | La OXP de Comercio recibe pagos que reducen su saldo pendiente: vía vinculación con extracto, regularización de anticipo, aplicación de devolución o pago directo (SincoA&F para formas de pago diferentes a tarjeta de crédito). Cuando el saldo llega a cero, la OXP pasa a **Pagada**. |
| **Proceso - OXP de Extracto** | El sistema contable externo (SincoA&F) gestiona la ejecución del pago (tarjeta de crédito) o la recarga (tarjeta débito prepago). El sistema OXP **únicamente monitorea** y registra el estado de pago consultando a SincoA&F. Las devoluciones de cargos financieros también reducen el saldo del extracto. |
| **Salida** | OXP de Extracto marcada como **Pagada** una vez su saldo pendiente de pago llega a cero. Ciclo cerrado. |

---

### Etapa 5: Regularización de Anticipos

| Aspecto | Descripción |
|---------|-------------|
| **Disparador** | Anticipo con saldo por regularizar mayor a cero y OXP de Comercio confirmada del mismo tercero disponible. |
| **Entrada** | Anticipo + OXP de Comercio destino (en estado Confirmada o posterior). |
| **Proceso** | El usuario selecciona un anticipo y una OXP de Comercio confirmada o posterior del mismo tercero. El sistema cruza ambos: reduce el saldo por regularizar del anticipo y aplica un pago sobre la OXP de Comercio. La OXP de Comercio destino debe estar en estado Confirmada o posterior, donde el valor neto ya es estable [R30]. |
| **Variante - 1:N** | Un anticipo puede regularizarse contra múltiples OXP de Comercio en operaciones independientes (no necesariamente en una sola operación) [R29]. |
| **Variante - Amortización** | Al completarse la regularización, el sistema genera la información estructurada necesaria para que SincoA&F ejecute la amortización contable (reclasificación de cuentas de anticipo a cuentas definitivas de gasto/costo). |
| **Salida** | Anticipo con saldo por regularizar reducido. Si llega a cero: estado Regularizado (o Cerrado si el saldo por pagar también es cero). OXP de Comercio con pago aplicado por el valor regularizado. |

---

### Etapa 6: Devoluciones

| Aspecto | Descripción |
|---------|-------------|
| **Disparador** | Comercio realiza un reintegro de dinero a la compañía. |
| **Entrada** | Referencia al OXP origen (Comercio, Extracto o Anticipo) y datos de la devolución. |
| **Proceso - Tipo Comercio** | Se registran los conceptos devueltos (espejo de los conceptos de gasto originales). Al confirmarse, el sistema aplica el crédito contra el saldo pendiente de pago de la OXP de Comercio origen. Si el crédito excede el saldo pendiente, el sistema genera automáticamente un anticipo por la diferencia (excedente de devolución) [R32]. El valor acumulado de todas las devoluciones no puede exceder el valor neto de la OXP origen [R31]. |
| **Proceso - Tipo Extracto** | Se registran cargos financieros devueltos (ej: reversa de cobro indebido de 4x1000). Al confirmarse, el sistema reduce el saldo pendiente de pago de la OXP de Extracto origen [R33]. |
| **Proceso - Tipo Anticipo** | Se registra una reversa total del anticipo. Solo aplica cuando el anticipo está en estado Vigente sin cruces previos. El valor de la devolución debe ser exactamente igual al valor total del anticipo [R34]. Al confirmarse, el anticipo pasa a estado Reversado (terminal). |
| **Causación** | Al causarse la devolución, se genera una nota crédito en el sistema contable [R16]. |
| **Salida** | Devolución en estado Causada. Crédito aplicado sobre el OXP origen (o anticipo generado por excedente). |

---

## Sección 5: Integraciones

### Principio de responsabilidad

El sistema OXP actúa como orquestador del proceso operativo de formalización de las compras, gestionando la recepción, validación, conciliación y confirmación de los soportes documentales asociados, desde su radicación hasta su entrega estructurada al sistema contable, sin ejecutar directamente procesos de pago ni registros contables finales. Sus responsabilidades son:

- **Registrar** transacciones con sus soportes documentales.
- **Orquestar** el flujo de radicación-conciliación-confirmación.
- **Traducir contablemente** los conceptos de obligaciones por pagar (comercio, extracto, anticipos, devoluciones, cargos) a cuentas, centros de costo, terceros y naturaleza contable. Esta traducción estructurada es el insumo que OXP entrega a SincoA&F para su registro.
- **Monitorear** el estado de las obligaciones consultando los sistemas externos.

La **ejecución** de pagos, desembolsos y movimientos financieros es responsabilidad exclusiva de los sistemas externos integrados (SincoA&F). El sistema OXP no ejecuta desembolsos ni modifica saldos bancarios directamente.

### Ecosistema de integración

El sistema OXP se integrará con **SincoERP** como plataforma central, interactuando con los siguientes módulos:

| Módulo | Nombre | Función |
|--------|--------|---------|
| **SincoA&F** | Administrativo y Financiero | Sistema contable. Recibe causaciones, gestiona pagos y confirma estado de pago de las OXP. |
| **SincoRE** | Recepción Electrónica | Transforma facturas electrónicas colombianas en información estructurada y la expone mediante API. |
| **SincoADPRO** | Compras y Contratación | Ratifica la formalización de compras que requieren validación en este módulo (no aplica para todas las compras). |

---

### Integraciones entrantes (hacia el sistema OXP)

| Origen | Datos recibidos | Formato | Método de integración | Observación |
|--------|-----------------|---------|----------------------|-------------|
| **SincoRE** | Datos estructurados de facturas electrónicas colombianas | JSON | Consumo de API | Solo facturas electrónicas colombianas |
| **SincoADPRO** | Ratificación de formalización de compras | JSON | Consumo de API | Solo para compras que requieren formalización en este módulo |
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
| **SincoA&F** | Reclasificación contable por amortización de anticipos | JSON | Consumo de API |

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
| R01 | Las compras del exterior que no generan factura electrónica deben tener documento soporte emitido dentro de los 6 días hábiles posteriores a la fecha de la transacción (requisito DIAN). **Consecuencia:** El sistema OXP debe alertar sobre OXP de compras del exterior que se acerquen al vencimiento del plazo. La emisión del Documento Soporte Electrónico es responsabilidad del sistema contable (una vez causada la OXP) en conjunto con el sistema de emisión electrónica, el cual informa al sistema OXP la regularización del documento. Esta integración es complementaria y su comportamiento puede variar según el ecosistema de sistemas con el que se integre. | Sí |
| R02 | Toda OXP de Comercio se crea inicialmente en estado pendiente. Según parametrización por empresa, la OXP puede crearse en estado pendiente (requiere confirmación posterior) o crearse y confirmarse automáticamente. | Sí (por empresa) |
| R03 | Si no se cuenta con el soporte documental, se puede crear la OXP como Anticipo. | No |
| R04 | Las devoluciones deben registrarse y asociarse a la OXP de comercio original cuando esté disponible. | No |
| R04b | Los anticipos deben regularizarse dentro del plazo configurado. El plazo puede variar según acuerdos con el comercio/proveedor. El sistema genera alertas cuando un anticipo excede el tiempo permitido sin regularización. | Sí (por empresa, default 30 días) |
| R05 | Las OXP que superen un monto máximo configurado generan una alerta informativa indicando que deben cumplir el flujo completo de aprobación. | Sí (por empresa, default 30 millones COP) |
| R05b | **Valoración en Moneda Extranjera:** Las OXP de Comercio originadas en compras del exterior se radican con la TRM del día de la transacción. El sistema siempre almacena el valor original de la compra en la moneda de origen y el valor convertido a la moneda funcional del país donde opera el sistema. | No |
| R05c | **Distribución de Costos (Split):** Durante la radicación (o al regularizar un anticipo), el usuario puede distribuir el costo de una OXP de Comercio entre múltiples centros de costo o cuentas contables, ya sea porcentualmente o por valor. La suma de las distribuciones debe ser igual al 100% del valor de la OXP. Una OXP de Comercio puede generar N registros de causación de costo. | No |
| R05d | **Moneda operativa del extracto:** Un OxpExtracto opera en una sola moneda. Si todas las partidas del extracto están en la misma moneda (COP, USD u otra), el extracto opera en esa moneda — aplicando `ValorMonetario` (con TRM y equivalente en moneda funcional) si es moneda extranjera. Cuando el extracto contiene partidas en **monedas mixtas**, las partidas en moneda extranjera se convierten a moneda funcional usando la TRM informada por la entidad bancaria, y el extracto opera en moneda funcional. En ambos casos, cada partida conserva su valor original, moneda de origen y TRM para trazabilidad. Los cálculos derivados (`valorTotalExtracto()`, `saldoPorPagar()`) operan en la moneda del extracto. La ejecución del pago y cualquier diferencia de cambio al momento del desembolso son responsabilidad del dominio de Tesorería. | No |
| R36 | **Clasificación inteligente de origen:** Al radicar una OXP de Comercio, el sistema determina el origen de la obligación (directa o sub-dominio de gestión) y resuelve las referencias fiscales (clasificacionTributaria, conceptoPago). La clasificación es automática y asistida — el usuario siempre puede corregir la sugerencia. | Sí (mecanismo de clasificación configurable) |
| R37 | **Validación de tributos del proveedor:** Cuando el soporte documental incluye tributos declarados por el proveedor, estos se registran como referencia y se validan contra el cálculo del sub-dominio de Impuestos. Si hay discrepancia, el sistema genera alerta para revisión. | No |

---

### Reglas de conciliación

| ID | Regla | Configurable |
|----|-------|--------------|
| R06 | El extracto debe estar 100% conciliado antes de pasar a la etapa de confirmación. *Nota: Las partidas del extracto que no cuenten con OXP de comercio pueden resolverse mediante: (a) anticipos, o (b) marca de **Partida en Disputa** (para errores bancarios, fraudes potenciales o transacciones no reconocidas). Los cargos adicionales del extracto (4x1000, cuota de manejo, intereses) se consideran conciliados como parte de la OXP de Extracto, no requieren OXP de Comercio y no generan anticipos. Se debe configurar por cada tarjeta cuáles cargos adicionales maneja, para detectarlos automáticamente en el extracto.* | No |
| R06b | Las **Partidas en Disputa** permiten alcanzar el 100% de conciliación sin generar anticipos. Su resolución posterior puede ser: (1) **Descartada:** la partida específica se marca como descartada cuando el banco reversa la transacción, compensándola contra la línea de "Reverso Bancario" en un extracto futuro; (2) **Reclasificación Contable:** cuando se identifica el gasto real y se radica la OXP de Comercio correspondiente, el sistema vincula la partida en disputa del extracto original con la nueva OXP de Comercio mediante una reclasificación contable, sin generar un nuevo documento de cobro ni duplicar la deuda. | No |
| R07 | El sistema alerta cuando la conciliación no está completada dentro del plazo configurado previo a la fecha de pago. | Sí (por tarjeta, default 3 días) |
| R08 | Las partidas del extracto sin OXP de comercio asociada pueden resolverse mediante: (a) solicitud de radicación de OXP faltante (opción inicial y temporal), o (b) generación de Anticipo. Si la radicación no se completa dentro del plazo definido por la operación, la partida debe formalizarse como Anticipo para permitir el cierre del extracto y cumplir la conciliación 100%. | No |
| R09 | El sistema persiste las asociaciones entre patrones de descripción del extracto bancario y comercios/terceros. Estas asociaciones se establecen durante la conciliación (manual o asistida por el agente inteligente) y se aplican automáticamente para sugerir cruces en conciliaciones futuras. *Ejemplo: Si el extracto muestra "AMZN\*1X2Y3Z SEATTLE" y el usuario lo asocia a "Amazon.com", el sistema almacena el patrón "AMZN\*" vinculado a ese tercero. En extractos futuros, cualquier transacción con descripción similar (ej: "AMZN\*ABC123") será sugerida automáticamente para cruce con OXP de Amazon.com.* | No |
| R10 | La conciliación automática aplica una tolerancia en la comparación de valores. Si la diferencia está dentro de la tolerancia, se acepta el cruce automáticamente. Si la diferencia supera la tolerancia, el sistema funciona en modo asistido sugiriendo al usuario las partidas que considera correspondientes. **Importante:** La diferencia dentro de tolerancia no desaparece; el sistema genera automáticamente un movimiento de ajuste contra la cuenta de "Gastos Bancarios" (si el extracto es mayor) o "Aprovechamientos Bancarios" (si el extracto es menor), según la configuración definida para cada tarjeta. | Sí (por empresa, default +/- 1000 COP) |
| R10b | **Ajuste por Diferencia en Cambio:** Para cada OXP de Comercio en moneda extranjera, si existe diferencia entre el valor radicado (TRM del día de la transacción) y el valor reflejado en el extracto (TRM del día de corte), el sistema genera automáticamente un **concepto de ajuste por diferencia en cambio** sobre la OXP de Extracto: clasificado como gasto financiero si la TRM subió, o ingreso financiero si la TRM bajó. Se genera un concepto de ajuste por cada OXP de Comercio en moneda extranjera. Estos conceptos permiten el cruce exacto sin crear nuevas OXP. Nota: las partidas del extracto en moneda extranjera ya fueron convertidas a moneda funcional durante la radicación `[R05d]`; la diferencia de cambio se calcula comparando la TRM de radicación de la OxpComercio contra la TRM del extracto aplicada a la partida. | No |
| R10c | **Conciliación Trans-mensual:** El sistema debe permitir al usuario buscar y vincular partidas marcadas como **"En Disputa"** de períodos anteriores con transacciones de tipo "Reverso" o "Ajuste" del extracto del período actual. Esto permite resolver disputas que cruzan múltiples períodos contables (ej: un fraude en enero y su devolución en marzo). | No |

---

### Reglas de confirmación y causación

| ID | Regla | Configurable |
|----|-------|--------------|
| R11 | Las OXP pueden confirmarse manualmente por un usuario o automáticamente según configuración de la empresa. La confirmación aplica a ambos tipos de OXP pero con estados previos diferentes: (1) **OXP de Comercio:** la confirmación valida la operación de radicación y habilita la causación; los pagos (vía extracto, anticipo, devolución o pago directo) reducen progresivamente el saldo pendiente. (2) **OXP de Extracto:** la confirmación se habilita únicamente cuando la conciliación está completada (100%), y posteriormente se monitorea el pago. | Sí (por empresa) |
| R11b | **Rechazo por Confirmador:** Si el confirmador no aprueba una OXP de Comercio, esta pasa a estado **"Devuelta"** y retorna a la bandeja del radicador. El confirmador debe registrar obligatoriamente un **"Motivo de Rechazo"**. El radicador puede corregir la OXP y reenviarla a confirmación, o descartarla según corresponda. | No |
| R12 | Al confirmar una OXP, se genera automáticamente la integración con el sistema contable. La forma en que se ejecuta la causación (automática al confirmar o mediante acción de un usuario sobre OXP confirmadas) es configurable por empresa, tal como se describe en la Etapa de Confirmación y Causación. | Sí (por empresa) |
| R13 | Se genera una causación individual por cada OXP de comercio. | No |
| R14 | Se genera una causación por cada OXP de extracto. | No |
| R15 | Al regularizar un anticipo, se genera la información para la amortización contable correspondiente. | No |
| R16 | Al causar una devolución, se genera una nota crédito en el sistema contable. | No |

---

### Reglas de pago

| ID | Regla | Configurable |
|----|-------|--------------|
| R17 | Una OXP de Comercio recibe pagos de múltiples orígenes que reducen su saldo pendiente: vinculación con extracto (conciliación), regularización de anticipo, aplicación de devolución, o pago directo (SincoA&F). Cuando el saldo llega a cero, la OXP pasa a estado **Pagada**. La información de cada pago (cuenta, tercero, naturaleza) es la base que OXP traduce y entrega a SincoA&F para su registro contable. | No |
| R18 | Una OXP de Extracto se considera **Pagada** cuando su saldo pendiente de pago llega a cero. El saldo se reduce por confirmación de pago de SincoA&F y por devoluciones de cargos financieros. El sistema OXP no ejecuta pagos; solo registra y monitorea su estado. | No |

---

### Reglas de cargos adicionales

| ID | Regla | Configurable |
|----|-------|--------------|
| R19 | Los cargos adicionales del extracto (4x1000, cuota de manejo, intereses) se incluyen dentro de la OXP de extracto para que el valor a pagar coincida exactamente con el extracto. | Sí |

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
| R24 | La causación de OXP puede limitarse a perfiles específicos. | Sí (por empresa) |
| R25 | **Segregación de Funciones:** El sistema debe impedir que un usuario con rol de Confirmador apruebe OXP en las que él mismo haya actuado como Radicador. | Sí (por empresa) |
| R26 | **Validación de Unicidad y Antigüedad (Anti-Fraude):** Para prevenir la duplicidad de pagos y el fraude por reutilización de soportes, el sistema validará la combinación de **NIT del Tercero + Número de Soporte**. (1) **Bloqueo por Duplicidad:** Si existe un registro previo con el mismo NIT y Número de Soporte cuya fecha de emisión tenga una diferencia menor a 24 meses respecto a la actual, el sistema bloqueará la radicación automáticamente. (2) **Alerta por Reuso:** Si la combinación existe pero la fecha es mayor a 24 meses, el sistema emitirá una alerta de advertencia, permitiendo la radicación pero marcándola para revisión prioritaria por el Autorizador. (3) **Validación de Valor:** Independientemente de la fecha, si el Valor también coincide, el bloqueo será mandatorio y solo podrá ser levantado por el Autorizador tras verificar la autenticidad del soporte. | No |
| R27 | **Gestión de Saldos Pendientes al Cierre:** Las OXP causadas cuya fecha de factura pertenezca al período actual, pero que no aparezcan en el extracto bancario del mismo período, permanecerán en estado **Confirmada** (Pasivo) y se arrastrarán automáticamente al siguiente período para su conciliación. El sistema debe permitir generar un reporte de "OXP Pendientes de Conciliar" para el cierre de mes. | No |

---

### Reglas de anticipos

| ID | Regla | Configurable |
|----|-------|--------------|
| R28 | **Reversa de Anticipo:** Un anticipo solo puede reversarse cuando está en estado Vigente y no tiene pagos ni regularizaciones aplicadas. La reversa es total — no existen reversas parciales. Al reversarse, el anticipo pasa a estado terminal Reversado. | No |
| R29 | **Regularización 1:N:** Un anticipo puede regularizarse contra múltiples OXP de Comercio del mismo tercero en operaciones independientes. Cada regularización reduce el saldo por regularizar del anticipo y aplica un pago sobre la OXP de Comercio destino. | No |
| R30 | **Estado mínimo para regularización:** La regularización solo puede realizarse contra OXP de Comercio en estado Confirmada o posterior, donde el valor neto ya es estable y no puede ser modificado. | No |

---

### Reglas de devoluciones

| ID | Regla | Configurable |
|----|-------|--------------|
| R31 | **Límite de devolución:** El valor acumulado de todas las devoluciones sobre una OXP de Comercio no puede exceder el valor neto de la OXP. | No |
| R32 | **Excedente de devolución:** Cuando una devolución sobre OXP de Comercio excede el saldo pendiente de pago (pero no el valor neto acumulado), el sistema genera automáticamente un anticipo por la diferencia. Este anticipo nace en estado Pagado y pendiente de regularización. | No |
| R33 | **Devolución tipo Extracto:** Una devolución de cargo financiero solo puede registrarse cuando la OXP de Extracto origen tiene saldo pendiente de pago mayor a cero. | No |
| R34 | **Devolución tipo Anticipo:** Solo aplica como reversa total — el valor de la devolución debe ser exactamente igual al valor total del anticipo, y el anticipo debe cumplir las condiciones de R28. | No |

---

### Reglas de pago directo

| ID | Regla | Configurable |
|----|-------|--------------|
| R35 | **Pago directo:** Un pago confirmado por SincoA&F para formas de pago diferentes a tarjeta de crédito solo puede aplicarse sobre OXP de Comercio en estado Causada. Reduce el saldo pendiente de pago de la OXP. | No |

---

## Sección 7: Qué está dentro y fuera del alcance

### Dentro del alcance

> Las fases F1 y F2 se definen en la Sección 8. La pertenencia al dominio no cambia — la columna indica el objetivo de implementación.

| Área | Descripción | Fase |
|------|-------------|:----:|
| **Medios de pago** | Gestión de las características y propiedades de los medios de pago: tarjetas de crédito corporativas, tarjetas de débito prepago, crédito a proveedor (plazos, condiciones de pago) y contado. Modelado como componente independiente dentro del BC de OXP — en el futuro se extraerá a un sub-dominio de configuraciones transversales cuando otros sub-dominios lo requieran. | F1 |
| **OXP de Comercio** | Ciclo de vida completo de obligaciones por hecho económico de egreso, independiente del medio de pago. Ciclo: radicación → confirmación → causación → pago. | F1 |
| **OXP de Extracto** | Ciclo de vida completo de obligaciones consolidadas por producto financiero (tarjetas). Ciclo: radicación → conciliación → confirmación → causación → pago. | F1 |
| **OXP de Caja Menor** | Obligaciones de menor cuantía con esquema de fondo fijo, rendición y reembolso. | F2 |
| **Radicación** | Registro de obligaciones al sistema con sus soportes documentales (XML, PDF, imágenes, CSV). Extracción automática de datos: vía SincoRE para facturas electrónicas XML, vía servicio de extracción para PDF e imágenes. Clasificación inteligente del origen de la obligación (directa o de sub-dominio de gestión) y resolución de referencias fiscales. Validación de tributos declarados en el soporte contra cálculo propio de Impuestos. | F1 |
| **Ciclo de vida de anticipos** | Creación, pago (vinculación a extracto o pago directo), regularización contra OXP de Comercio, cierre y reversa total. Trazabilidad completa con dos dimensiones de saldo independientes. Generación de información para amortización contable. | F1 |
| **Ciclo de vida de devoluciones** | Radicación, confirmación y causación de devoluciones de 3 tipos: comercio (conceptos de gasto), extracto (cargos financieros) y anticipo (reversa total). Aplicación automática de crédito sobre el OXP origen. | F1 |
| **Excedente de devolución** | Generación automática de anticipo cuando la devolución excede el saldo pendiente de pago de una OXP de Comercio. | F1 |
| **Pago directo** | Registro de pagos confirmados por SincoA&F para formas de pago diferentes a tarjeta de crédito. | F1 |
| **Regularización de anticipos** | Cruce coordinado de anticipos contra OXP de Comercio confirmadas del mismo tercero, con soporte 1:N. | F1 |
| **Carga de extractos** | Carga manual de extractos bancarios en formato PDF o CSV. Extracción de transacciones y cargos adicionales. | F1 |
| **Conciliación** | Cruce automático y asistido entre OXP de comercio y extractos bancarios. Aprendizaje de relaciones comercio-descripción. | F1 |
| **Confirmación** | Flujo de confirmación manual o automático según configuración por empresa. | F1 |
| **Causación** | Generación automática de causaciones individuales por OXP de comercio y OXP de extracto hacia SincoA&F. | F1 |
| **Monitoreo de pago** | Consulta del estado de pago de OXP de extracto desde SincoA&F. | F1 |
| **Integración con SincoADPRO** | Ratificación de compras que requieren formalización en este módulo. | F1 |
| **Alertas** | Notificaciones de plazos de conciliación, montos que exceden límites configurados, y anticipos pendientes de regularización que superen el plazo establecido. | F1 |
| **Vistas de monitoreo** | Consultas del estado de OXP (pendientes, confirmadas, causadas) según permisos de usuario. | F1 |
| **Configuraciones por empresa** | Preferencias de confirmación automática, tolerancias de conciliación, límites de monto, plazos de alerta. | F1 |

---

### Fuera del dominio de obligaciones por pagar

| Área | Descripción | Observación |
|------|-------------|-------------|
| **Procesamiento de pagos** | El sistema OXP no ejecuta pagos ni recargas. | Esta responsabilidad es de SincoA&F y el módulo de tesorería existente. |
| **Integración directa con tesorería** | No se integrará con un sistema de tesorería independiente en la primera fase. | SincoA&F gestiona el procesamiento de pago a partir de la causación. |
| **Gestión de comercios/proveedores** | Alta, baja o modificación de terceros. | Responsabilidad del servicio transversal de Gestión de Terceros. |
| **Gestión de usuarios** | Creación de usuarios y asignación de permisos. | Se asume integración con sistema de gestión de identidad existente. |

---

### Dependencias externas a construir

El sistema OXP será el primer módulo de la nueva arquitectura del ERP. Para su funcionamiento, requiere la construcción de los siguientes servicios transversales con responsabilidades claramente definidas:

| Dependencia | Descripción | Impacto en el sistema OXP |
|-------------|-------------|---------------------------|
| **Gestión de Terceros** | Servicio transversal que centraliza la información de comercios, proveedores y entidades externas. Incluye identificación tributaria, razón social, datos de contacto y clasificación. | El sistema OXP consumirá este servicio para identificar comercios/proveedores en las transacciones. |
| **Tesorería - Medios de pago** | Componente que gestiona los datos maestros de tarjetas de crédito y débito prepago (número, tipo, fecha de corte, fecha de pago, límites, entidad bancaria). | Por ahora OXP gestiona el catálogo de medios de pago internamente como componente independiente dentro de su BC. Cuando se construya el sub-dominio de configuraciones transversales, este catálogo se extraerá para consumo de otros sub-dominios. |
| **Sistema de cálculo de impuestos** | Sistema transversal que determina los impuestos aplicables a las transacciones según la normativa colombiana. | El sistema OXP consumirá este servicio para obtener los valores de impuestos en las OXP. |
| **Reglas de traducción contable** | Lógica que convierte los conceptos de OXP (comercio, extracto, anticipos, devoluciones, cargos) en asientos contables para SincoA&F. | El sistema OXP aplicará estas reglas al generar las causaciones. |
| **Servicio de extracción de datos** | Servicio transversal de infraestructura que interpreta documentos no electrónicos (PDF, imágenes) para extraer datos estructurados. Complementa a SincoRE que cubre facturas electrónicas XML. | El sistema OXP consumirá este servicio para obtener datos de soportes no electrónicos. Otros sub-dominios (CXC, etc.) podrán consumir el mismo servicio. |

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

## Sección 8: Estrategia de implementación por fases

El dominio OXP conserva una visión integral que abarca todos los tipos de obligaciones por pagar descritos en este documento. Su implementación se organiza por fases alineadas con la complejidad del ciclo de vida:

### Fase 1 (F1) — OXP de Comercio y Extracto

Cubre el ciclo de vida completo de las obligaciones individuales y consolidadas:

| Capacidad | Descripción |
|-----------|-------------|
| OXP de Comercio | Obligaciones por hecho económico de egreso, independiente del medio de pago. Ciclo: radicación → confirmación → causación → pago. |
| OXP de Extracto | Obligaciones consolidadas por producto financiero (tarjetas). Ciclo: radicación → conciliación → confirmación → causación → pago. |
| Anticipos | Ciclo de vida completo con dos dimensiones de resolución (pago + regularización). |
| Devoluciones | Tres tipos: comercio, extracto, anticipo. Ciclo de vida independiente. |
| Clasificación inteligente de origen | Determinación automática del origen (directa o sub-dominio de gestión) y resolución de referencias fiscales. |
| Integración con Impuestos | Solicitud de cálculo (síncrona) y confirmación (asíncrona). |
| Catálogo de gasto directo | Configuración de conceptos para obligaciones directas de OXP. |
| Medios de pago | Gestión de características y propiedades de los medios de pago (tarjetas, crédito, contado). Componente independiente dentro del BC. |
| Conciliación | Cruce automático y asistido entre OXP de Comercio y extractos bancarios. |
| Causación y traducción contable | Generación de información estructurada para el sistema contable. |

### Fase 2 (F2) — Ampliación de tipos y orígenes

Capacidades que extienden el dominio sin redefinir el núcleo de la Fase 1:

| Capacidad | Descripción |
|-----------|-------------|
| OXP de Caja Menor | Obligaciones de menor cuantía con esquema de fondo fijo, rendición y reembolso. Nuevo agregado con ciclo de vida propio. |
| Viáticos / Gastos de viaje | *Evaluación pendiente:* determinar si es un sub-dominio de gestión que envía obligaciones a OXP (como Compras o Arrendamiento) o si requiere un tipo de OXP propio. |
| Obligaciones recurrentes | *Evaluación pendiente:* generación automática periódica desde sub-dominios de gestión (Arrendamiento, suscripciones). Probablemente fluyan como OXP de Comercio con subDominioOrigen. |

### Criterio de éxito de la Fase 1

La Fase 1 se considera operativa cuando:

1. Una OXP de Comercio puede radicarse desde cualquier canal de entrada (XML, PDF, carga manual) con clasificación inteligente del origen.
2. El sistema resuelve referencias fiscales y solicita cálculo tributario al sub-dominio de Impuestos.
3. Las OXP de Comercio asociadas a tarjeta se concilian contra el extracto bancario con asistencia automática.
4. Las OXP de Comercio de otros medios de pago (crédito, contado) se resuelven vía pago directo.
5. Los anticipos se gestionan con ciclo de vida completo (pago + regularización + cierre).
6. Las devoluciones de los tres tipos se procesan con aplicación automática de crédito.
7. Se generan las causaciones individuales hacia el sistema contable.
8. Existe trazabilidad completa desde la radicación hasta el cierre.

---

## Sección 9: Beneficios esperados

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
| **Gestión de anticipos** | Control sobre las OXP sin soporte documental, con seguimiento hasta su regularización y amortización contable. |

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
| 1.1 | Febrero 2026 | Alineación con Modelo de Dominio v2.4. **Compensada** eliminada del glosario y referencias (absorbida por Pagada, ver D18). **Devolución** corregida: valor positivo representando magnitud del crédito (ver D19). Tabla de estados y salida de conciliación actualizadas. |
| 1.2 | Marzo 2026 | Alineación con Modelo de Dominio v2.5. Glosario ampliado: Anticipo con ciclo de vida completo, Devolución con 3 tipos, nuevos términos (Reversa de Anticipo, Pago Directo, Excedente de Devolución). Flujo principal extendido con Etapa 5 (Regularización) y Etapa 6 (Devoluciones). Diagrama actualizado con 4 flujos. 8 nuevas reglas de negocio (R28–R35): anticipos, devoluciones y pago directo. Alcance actualizado con capacidades de ciclo de vida independiente de anticipos y devoluciones. |
| 1.3 | Marzo 2026 | **Ampliación de alcance, integración fiscal y fases de implementación.** OXP de Comercio redefinida como independiente del medio de pago (no solo tarjetas). Contexto actual ampliado con facturas de proveedores a crédito (~1000/mes) y pagos de contado (~50/mes). Tres tipos de OXP por ciclo de vida documentados (Comercio, Extracto, Caja Menor). Medios de pago ampliado: gestión de características y propiedades de tarjetas, crédito y contado — componente independiente dentro del BC, extraíble a futuro. "Gestión de tarjetas" y "Otros medios de pago" eliminados de fuera del alcance. Dependencia "Tesorería - Medios de pago" actualizada con nota de gestión interna temporal. Nueva Sección 8: Estrategia de implementación por fases (F1: Comercio + Extracto, F2: Caja Menor + evaluaciones pendientes). Criterios de éxito de F1. Tabla de alcance con columna Fase (F1/F2). Sección de beneficios renumerada a Sección 9. **Integración fiscal:** Glosario de Radicación ampliado (extracción + clasificación + registro). Nuevo actor externo: Servicio de extracción de datos. Canales de entrada agnósticos en Etapa 1. Nuevas variantes: clasificación inteligente de origen y tributos declarados por el proveedor. Nuevas reglas R36 (clasificación inteligente de origen) y R37 (validación de tributos del proveedor). Radicación ampliada en tabla de alcance (extracción, clasificación, validación de tributos). Nueva dependencia: Servicio de extracción de datos. |
