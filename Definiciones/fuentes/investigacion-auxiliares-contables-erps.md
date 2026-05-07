# Investigacion: Auxiliares Contables (Subsidiary Ledger Detail) en ERPs Lideres

> **Fecha:** 2026-03-25
> **Proposito:** Documentar como los ERPs lideres presentan los auxiliares contables (libro mayor por cuenta con detalle transaccional), con enfasis en: columnas/campos estandar, presencia del numero de documento fiscal, y campos de referencia al documento de origen.
> **Version:** 1.0

---

## Tabla de contenido

1. [Resumen ejecutivo](#1-resumen-ejecutivo)
2. [SAP S/4HANA](#2-sap-s4hana)
3. [Oracle Fusion Cloud](#3-oracle-fusion-cloud)
4. [Microsoft Dynamics 365 Finance](#4-microsoft-dynamics-365-finance)
5. [NetSuite](#5-netsuite)
6. [Odoo](#6-odoo)
7. [Workday](#7-workday)
8. [Matriz comparativa de campos](#8-matriz-comparativa-de-campos)
9. [Respuestas a las preguntas clave](#9-respuestas-a-las-preguntas-clave)
10. [Conclusiones para el ERP Cosmos](#10-conclusiones-para-el-erp-cosmos)
11. [Fuentes](#11-fuentes)

---

## 1. Resumen ejecutivo

| Hallazgo | Detalle |
|----------|---------|
| **El numero de documento fiscal NO es columna estandar del auxiliar contable** | En ninguno de los 6 ERPs investigados el numero del documento fiscal (factura electronica, NCF, CFDI) aparece como columna nativa del reporte de auxiliar contable o libro mayor por cuenta. |
| **Los ERPs usan campos genericos de "referencia"** | Todos los ERPs tienen un campo de referencia (SAP: `Reference`/XBLNR, Oracle: `Transaction Number`, Dynamics: `Document Number`, NetSuite: `Num`, Odoo: `Reference`, Workday: `Memo`/Worktags) que **puede** contener el numero de la factura del proveedor o del documento comercial, pero no es especifico para documentos fiscales regulados. |
| **El drill-down es el mecanismo universal** | En todos los ERPs, la trazabilidad completa al documento fiscal se logra mediante navegacion (drill-down) desde la linea del auxiliar al documento de origen (factura, nota credito, pago), donde se encuentran todos los campos fiscales. |
| **La personalizacion es posible en todos** | Todos los ERPs permiten agregar columnas adicionales al reporte mediante configuracion de layout (SAP), Report Builder (NetSuite, Oracle OTBI), personalizacion de formularios (Dynamics, Odoo) o campos calculados (Workday). |

---

## 2. SAP S/4HANA

### 2.1 Reportes de auxiliar contable

SAP ofrece varias transacciones para consultar auxiliares contables:

| Transaccion | Nombre | Proposito |
|-------------|--------|-----------|
| **FBL3N** / **FAGLL03** | G/L Account Line Items | Lineas de item por cuenta de mayor |
| **FAGLL03H** | G/L Line Item Browser | Navegador de items en S/4HANA (reemplaza FBL3N) |
| **FBL1N** | Vendor Line Items | Lineas de item por proveedor (subledger AP) |
| **FBL5N** | Customer Line Items | Lineas de item por cliente (subledger AR) |
| **S_ALR_87012301** | G/L Account Balances (Line Items) | Saldos con detalle de items |

### 2.2 Columnas estandar del auxiliar (FBL3N / FAGLL03H)

Las columnas por defecto en el layout estandar de FBL3N/FAGLL03H son:

| # | Campo | Campo tecnico (BSEG/BKPF/ACDOCA) | Descripcion |
|---|-------|-----------------------------------|-------------|
| 1 | **Status** | BSTAT | Icono de estado del item (abierto, compensado, etc.) |
| 2 | **Document Number** | BELNR | Numero del documento contable (asiento FI) |
| 3 | **Document Type** | BLART | Tipo de documento (KR=factura proveedor, SA=asiento manual, etc.) |
| 4 | **Posting Date** | BUDAT | Fecha de contabilizacion |
| 5 | **Document Date** | BLDAT | Fecha del documento |
| 6 | **Reference** | XBLNR | Numero de referencia del documento externo |
| 7 | **Assignment** | ZUONR | Campo de asignacion (configurable: puede contener numero de PO, numero de factura, etc.) |
| 8 | **Amount in Local Currency** | DMBTR/WRBTR | Monto en moneda local |
| 9 | **Currency** | WAERS | Moneda de la transaccion |
| 10 | **Text** | SGTXT | Texto de la linea del asiento |
| 11 | **Clearing Document** | AUGBL | Documento de compensacion (si fue compensado) |
| 12 | **Clearing Date** | AUGDT | Fecha de compensacion |
| 13 | **G/L Account** | HKONT | Cuenta de mayor |
| 14 | **Debit/Credit Indicator** | SHKZG | Indicador debito/credito (S/H) |

### 2.3 Campos de referencia al documento de origen

| Campo | Campo tecnico | Contenido tipico |
|-------|---------------|------------------|
| **Reference** | XBLNR | Numero del documento externo del proveedor (ej: numero de factura del proveedor). Se copia del campo "Reference" del encabezado del documento contable. |
| **Assignment** | ZUONR | Campo libre configurable. Tipicamente contiene el numero de orden de compra, numero de factura interna, o un campo derivado por regla de clasificacion (sorting rule). |
| **Invoice Reference** | REBZG | Referencia al documento de factura vinculado (numero del documento FI de la factura). Solo visible si se agrega al layout. |
| **Purchasing Document** | EBELN | Numero de orden de compra. Solo visible si se agrega como Special Field al layout. |
| **Original Reference Key** | AWKEY | Clave de referencia al documento de origen (ej: clave del documento MM o SD que genero el asiento). |

### 2.4 Campos especiales (Special Fields)

SAP permite agregar campos adicionales al layout de FBL3N/FBL1N mediante la configuracion de **Special Fields** (IMG > Financial Accounting > G/L Accounting > Line Items > Define Special Fields for Line Item Display). Esto permite exponer campos de la tabla BSEG o ACDOCA que no estan en el layout por defecto, incluyendo campos de referencia a documentos de origen.

### 2.5 ACDOCA (Universal Journal) — Campos de referencia

La tabla ACDOCA en S/4HANA contiene mas de 500 campos. Los campos relevantes para referencia a documentos de origen son:

| Campo | Descripcion |
|-------|-------------|
| **AWREF** | Reference Document Number (numero del documento de origen) |
| **AWTYP** | Reference Transaction (tipo de transaccion de origen: RMRP=factura MM, VBRK=billing SD, etc.) |
| **AWITEM** | Reference Document Line Item |
| **SRC_AWREF** | Source Document Number |
| **SRC_AWTYP** | Source Document Type |
| **PREC_AWREF** | Preceding Document Reference Number |
| **PREC_BELNR** | Preceding Journal Entry Document Number |
| **XBLNR** | Reference Document Number (campo libre — tipicamente numero de factura del proveedor) |

### 2.6 Numero de documento fiscal en el auxiliar

**No aparece como columna estandar.** El campo `Reference` (XBLNR) contiene el numero del documento externo del proveedor (que puede ser el numero de factura), pero NO es el numero fiscal regulado (CUFE, NCF, CFDI). El numero fiscal regulado vive en el modulo de facturacion electronica o en las localizaciones por pais y se accede mediante drill-down al documento de origen.

En SAP, la navegacion al documento fiscal se hace asi:
1. En FBL3N, doble clic en el numero de documento (BELNR) → abre el documento contable FI.
2. Desde el documento FI, navegar al documento de origen (Environment > Document Environment > Display Document).
3. En el documento de origen (factura MM/SD) se encuentra el numero fiscal completo.

---

## 3. Oracle Fusion Cloud

### 3.1 Reportes de auxiliar contable

| Reporte | Proposito |
|---------|-----------|
| **Account Analysis** | Movimientos por cuenta con drill-down a subledger |
| **Configurable Account Analysis** | Version personalizable del Account Analysis |
| **Subledger Detail Journal Report** | Detalle de journals del subledger con referencia a transacciones |
| **Daily Journals Report** | Journals diarios con detalle de lineas |
| **Journal Ledger Report** | Detalle de journals con referencia interna |

### 3.2 Columnas del Account Analysis Report

| # | Campo | Descripcion |
|---|-------|-------------|
| 1 | **Period** | Periodo contable |
| 2 | **Account** | Cuenta contable (con segmentos del Accounting Flexfield) |
| 3 | **Beginning Balance** | Saldo inicial del periodo |
| 4 | **Journal Batch Name** | Nombre del lote de journals |
| 5 | **Journal Header** | Nombre del journal |
| 6 | **Journal Sequence Number** | Numero de secuencia del journal |
| 7 | **Journal Line** | Numero de linea |
| 8 | **Category** | Categoria del journal (Purchase Invoices, Payments, etc.) |
| 9 | **Accounting Date** | Fecha contable |
| 10 | **Description** | Descripcion de la linea |
| 11 | **Debit** | Monto debito |
| 12 | **Credit** | Monto credito |
| 13 | **Ending Balance** | Saldo final |

### 3.3 Columnas del Subledger Detail Journal Report

| # | Campo | Descripcion |
|---|-------|-------------|
| 1 | **Journal Line** | Numero de linea del journal |
| 2 | **Subledger Line** | Numero de linea del subledger |
| 3 | **Account** | Cuenta contable |
| 4 | **Account Description** | Descripcion de la cuenta |
| 5 | **Tax Code** | Codigo de impuesto |
| 6 | **Transaction Number** | **Numero de la transaccion de origen** (ej: numero de factura AP) |
| 7 | **Transaction Date** | Fecha de la transaccion de origen |
| 8 | **Currency** | Moneda |
| 9 | **Conversion Rate** | Tasa de cambio |
| 10 | **Entered Amount** | Monto en moneda ingresada |
| 11 | **Accounted Amount** | Monto contabilizado |

### 3.4 Columnas del Daily Journals Report

| # | Campo | Descripcion |
|---|-------|-------------|
| 1 | **Journal Line** | Numero de linea |
| 2 | **Account** | Cuenta contable |
| 3 | **Account Description** | Descripcion de la cuenta |
| 4 | **Line Description** | Descripcion de la linea |
| 5 | **Transaction Date or Number** | Fecha o numero de la transaccion de origen |
| 6 | **Tax Code** | Codigo de impuesto |
| 7 | **Third-Party Name** | Nombre del tercero (proveedor/cliente) |
| 8 | **Third-Party Number** | Numero del tercero |
| 9 | **Entered Currency** | Moneda ingresada |
| 10 | **Entered Amount** | Monto en moneda ingresada |
| 11 | **Accounted Amount** | Monto contabilizado |

### 3.5 Numero de documento fiscal en el auxiliar

**No aparece como columna estandar del Account Analysis.** El campo `Transaction Number` en los reportes de subledger muestra el numero de la transaccion de AP/AR (que es la numeracion interna de Oracle, no el numero fiscal). El numero fiscal regulado se almacena en el documento de origen (AP Invoice o AR Transaction) y se accede mediante drill-down.

La navegacion en Oracle Fusion sigue el patron de 3 niveles:
1. **Account Analysis (GL)** → muestra journals con saldo por cuenta.
2. Drill-down al **Subledger Journal (SLA)** → muestra Transaction Number del subledger.
3. Drill-down a la **Transaction (AP/AR)** → muestra todos los campos del documento, incluyendo numeros fiscales.

Oracle permite personalizar el Account Analysis con OTBI (Oracle Transactional Business Intelligence) para agregar columnas adicionales, pero los campos fiscales no estan en los reportes GL/SLA estandar.

---

## 4. Microsoft Dynamics 365 Finance

### 4.1 Reportes de auxiliar contable

| Reporte/Pagina | Proposito |
|----------------|-----------|
| **Voucher Transactions** | Pagina de consulta de transacciones por voucher/cuenta |
| **Trial Balance with Transactional Detail** | Balance de prueba con detalle de cada transaccion |
| **Ledger Transaction List** | Lista de transacciones por cuenta |
| **Transaction List by Date** | Transacciones ordenadas por fecha |
| **Dimension Statement** | Transacciones por dimension y periodo |
| **Account Source Explorer** | Explorador de origen de transacciones por cuenta |

### 4.2 Columnas del Voucher Transactions

Las columnas por defecto de la pagina Voucher Transactions son:

| # | Campo | Descripcion |
|---|-------|-------------|
| 1 | **Date** | Fecha de la transaccion |
| 2 | **Voucher** | Numero del voucher contable |
| 3 | **Transaction Type** | Tipo de transaccion (Vendor invoice, Payment, etc.) |
| 4 | **Main Account** | Cuenta principal |
| 5 | **Main Account Name** | Nombre de la cuenta |
| 6 | **Financial Dimensions** | Dimensiones financieras (Department, Cost Center, etc.) |
| 7 | **Debit** | Monto debito |
| 8 | **Credit** | Monto credito |
| 9 | **Amount in Transaction Currency** | Monto en moneda de transaccion |
| 10 | **Currency** | Moneda |
| 11 | **Amount in Reporting Currency** | Monto en moneda de reporte |

### 4.3 Columnas del Trial Balance with Transactional Detail

| # | Campo | Descripcion |
|---|-------|-------------|
| 1 | **Main Account** | Cuenta principal |
| 2 | **Financial Dimensions** | Dimensiones financieras |
| 3 | **Opening Balance** | Saldo de apertura |
| 4 | **Transaction Date** | Fecha de la transaccion |
| 5 | **Voucher Number** | Numero del voucher |
| 6 | **Document Number** | Numero del documento de origen |
| 7 | **Transaction Description** | Descripcion de la transaccion |
| 8 | **Debit** | Monto debito |
| 9 | **Credit** | Monto credito |
| 10 | **Running Balance YTD** | Saldo acumulado del anio |

### 4.4 Campos de referencia al documento de origen

| Campo | Descripcion |
|-------|-------------|
| **Voucher** | Numero del registro contable (generado por Number Sequence del journal). |
| **Document Number** | Numero del documento de origen (ej: numero de factura). Aparece en el Trial Balance with Transactional Detail. |
| **Invoice** | Numero de factura. **No es columna estandar** del Voucher Transactions; se accede via drill-down o mediante personalizacion. |
| **Vendor ID / Vendor Name** | Disponibles desde release 2020 wave 1 como columnas agregables al Voucher Transactions. |

### 4.5 Numero de documento fiscal en el auxiliar

**No aparece como columna estandar.** El campo `Document Number` en el Trial Balance with Transactional Detail puede mostrar el numero de factura interna, pero el numero fiscal regulado (ej: CFDI, NCF) **no es una columna nativa** del reporte de GL.

Segun la comunidad de Dynamics 365, para ver el numero de factura desde el auxiliar contable:
- La pagina **Account Source Explorer** muestra el `Document Number` que corresponde al numero de factura para transacciones de AP/AR.
- En la pagina **Voucher Transactions**, el numero de factura **no esta disponible out-of-the-box** y requiere personalizacion.
- Una practica comun es configurar **Default Descriptions** para incluir el numero de factura en la descripcion de la transaccion contable.
- Dynamics 365 soporta **Financial Tags** (hasta 20 campos personalizables) que pueden usarse para almacenar numeros fiscales adicionales.

---

## 5. NetSuite

### 5.1 Reportes de auxiliar contable

| Reporte | Proposito |
|---------|-----------|
| **GL Detail Report** | Detalle de movimientos por cuenta de mayor |
| **Account Detail Report** | Detalle de movimientos por cuenta individual |
| **Transaction Detail Report** | Detalle de transacciones con multiples filtros |
| **General Ledger Report** | Resumen del mayor con totales por cuenta |

### 5.2 Columnas del GL Detail Report / Account Detail Report

| # | Campo | Descripcion |
|---|-------|-------------|
| 1 | **Account** | Nombre y numero de la cuenta |
| 2 | **Type** | Tipo de transaccion (Invoice, Bill, Journal Entry, Payment, etc.) |
| 3 | **Date** | Fecha de la transaccion |
| 4 | **Num** | **Numero del documento/transaccion** (ej: numero de factura, numero de bill) |
| 5 | **Name** | Nombre de la entidad asociada (proveedor, cliente, empleado) |
| 6 | **Memo** | Descripcion o nota de la transaccion |
| 7 | **Debit** | Monto debito |
| 8 | **Credit** | Monto credito |
| 9 | **Running Balance** | Saldo acumulado despues de cada transaccion |
| 10 | **Split** | Cuenta de contrapartida (o "-Split-" si hay multiples) |

### 5.3 Campos de referencia al documento de origen

| Campo | Descripcion |
|-------|-------------|
| **Num** | Numero del documento de la transaccion. Para facturas de proveedor (Bills) muestra el numero del Bill; para facturas de venta (Invoices) muestra el numero del Invoice. Este es el **Document Number** de NetSuite, no el numero fiscal. |
| **Transaction ID** | Identificador interno unico. Disponible como columna adicional. |
| **GL Audit Number** | Numero de auditoria GL (secuencia gapless independiente). |
| **Name** | Nombre del proveedor/cliente. |
| **Memo** | Campo libre donde se puede incluir referencia al documento fiscal. |

### 5.4 Campos adicionales via Report Builder

NetSuite permite agregar columnas al reporte mediante el Report Builder:
- Transaction ID
- Created By / Date Created
- Custom Segments (Class, Department, Location)
- Subsidiary (en NetSuite OneWorld)
- System Notes

### 5.5 Numero de documento fiscal en el auxiliar

**No aparece como columna estandar.** El campo `Num` muestra el numero de documento interno de NetSuite (Document Number o Auto-Generated Number), no el numero fiscal regulado. Para documentos fiscales de paises especificos (CFDI en Mexico, NCF en RD), estos numeros se almacenan en campos de localizacion dentro de la transaccion y se acceden mediante drill-down al documento.

En NetSuite, el drill-down es directo: clic en la linea del GL Detail → abre la transaccion de origen donde estan todos los campos fiscales.

---

## 6. Odoo

### 6.1 Reportes de auxiliar contable

| Reporte | Ruta | Proposito |
|---------|------|-----------|
| **General Ledger** | Accounting > Reporting > General Ledger | Detalle de movimientos por cuenta |
| **Partner Ledger** | Accounting > Reporting > Partner Ledger | Detalle de movimientos por tercero |
| **Aged Payable / Receivable** | Accounting > Reporting | Cartera por vencimiento |
| **Journal Audit** | Accounting > Reporting > Audit Reports | Detalle por diario |

### 6.2 Columnas del General Ledger

| # | Campo | Descripcion |
|---|-------|-------------|
| 1 | **Date** | Fecha del asiento |
| 2 | **Journal** | Diario contable (ej: INV, BILL, MISC) |
| 3 | **Partner** | Nombre del tercero (proveedor/cliente) |
| 4 | **Reference** | Referencia del asiento (`account.move` name, ej: `BILL/2026/0001`) |
| 5 | **Label** | Etiqueta/descripcion de la linea |
| 6 | **Debit** | Monto debito |
| 7 | **Credit** | Monto credito |
| 8 | **Balance** | Saldo acumulado |

### 6.3 Columnas del Partner Ledger

| # | Campo | Descripcion |
|---|-------|-------------|
| 1 | **Date** | Fecha del asiento |
| 2 | **Journal** | Diario contable |
| 3 | **Account** | Cuenta contable |
| 4 | **Reference** | Referencia del asiento |
| 5 | **Label** | Descripcion |
| 6 | **Due Date** | Fecha de vencimiento |
| 7 | **Matching Number** | Numero de conciliacion |
| 8 | **Initial Balance** | Saldo inicial |
| 9 | **Debit** | Monto debito |
| 10 | **Credit** | Monto credito |
| 11 | **Amount Currency** | Monto en moneda original |
| 12 | **Balance** | Saldo acumulado |

### 6.4 Particularidad de Odoo: Factura = Asiento

En Odoo (desde v13+), la factura y el asiento contable son el **mismo registro** (`account.move`). Esto significa que:

- El campo `Reference` en el General Ledger muestra el nombre del `account.move` (ej: `BILL/2026/0001` para facturas de proveedor, `INV/2026/0001` para facturas de venta).
- Este nombre **es** el numero de la factura interna de Odoo, pero **no necesariamente** el numero fiscal regulado.
- Para localizaciones con numeracion fiscal (Colombia, Mexico, RD), Odoo agrega campos adicionales como `l10n_latam_document_number` que almacenan el numero fiscal real. Este campo **no aparece** en el General Ledger estandar.

### 6.5 Numero de documento fiscal en el auxiliar

**No aparece como columna estandar del General Ledger.** El campo `Reference` muestra el nombre del `account.move`, que funciona como numero de factura interna de Odoo. Para paises con numeracion fiscal regulada, el numero fiscal se almacena en un campo separado de localizacion que no es visible en el reporte estandar.

Odoo permite agregar columnas personalizadas al General Ledger mediante desarrollo (heredar el metodo `_get_columns_name` del modelo de reporte), pero no es configurable por el usuario sin codigo.

---

## 7. Workday

### 7.1 Reportes de auxiliar contable

| Reporte | Proposito |
|---------|-----------|
| **Journal Lines** | Detalle de lineas de journals por cuenta/periodo |
| **Journal Lines by Org** | Detalle de lineas agrupadas por organizacion |
| **Ledger Account Summary** | Resumen de movimientos por ledger account |
| **Trial Balance** | Balance de prueba con drill-down a transacciones |

### 7.2 Columnas del reporte Journal Lines

| # | Campo | Descripcion |
|---|-------|-------------|
| 1 | **Journal Number** | Numero del journal |
| 2 | **Accounting Date** | Fecha contable |
| 3 | **Ledger Account** | Cuenta del ledger |
| 4 | **Debit Amount** | Monto debito |
| 5 | **Credit Amount** | Monto credito |
| 6 | **Currency** | Moneda |
| 7 | **Currency Rate** | Tasa de cambio |
| 8 | **Ledger Debit Amount** | Monto debito en moneda del ledger |
| 9 | **Ledger Credit Amount** | Monto credito en moneda del ledger |
| 10 | **Line Memo** | Descripcion de la linea |
| 11 | **Worktags** | Etiquetas multidimensionales (Cost Center, Fund, Supplier, Project, etc.) |
| 12 | **Inter Company Affiliate** | Empresa intercompania (si aplica) |
| 13 | **Journal Source** | Origen del journal (Supplier Invoice, Expense Report, etc.) |

### 7.3 Campos de referencia al documento de origen

| Campo | Descripcion |
|-------|-------------|
| **Journal Source** | Identifica el tipo de documento de origen (Supplier Invoice, Expense Report, Customer Invoice, etc.). |
| **Worktags** | Los worktags de tipo Supplier, Customer, Project, etc., vinculan la linea del journal con las entidades de origen. |
| **Line Memo** | Campo libre donde se puede incluir referencia al documento de origen. |
| **Business Document Reference** | En el detalle del journal, Workday muestra el enlace al documento de negocio que genero el asiento. |

### 7.4 Numero de documento fiscal en el auxiliar

**No aparece como columna estandar.** Workday no muestra numeros de documentos fiscales en los reportes de journal lines. La referencia al documento de origen se hace a traves del `Journal Source` y los worktags. El numero de factura o documento fiscal se encuentra en el documento de origen (Supplier Invoice, Customer Invoice) al cual se navega desde el journal.

Workday permite crear reportes personalizados con campos calculados (Calculated Fields) que pueden extraer informacion del documento de origen y mostrarla como columna adicional.

---

## 8. Matriz comparativa de campos

### 8.1 Campos estandar del auxiliar contable

| Campo | SAP S/4HANA | Oracle Fusion | Dynamics 365 | NetSuite | Odoo | Workday |
|-------|:-----------:|:-------------:|:------------:|:--------:|:----:|:-------:|
| **Fecha contabilizacion** | Si | Si | Si | Si | Si | Si |
| **Numero de asiento/voucher** | Si (BELNR) | Si (Journal #) | Si (Voucher) | N/A (inline) | Si (Reference) | Si (Journal #) |
| **Tipo de documento/transaccion** | Si (BLART) | Si (Category) | Si (Txn Type) | Si (Type) | Si (Journal) | Si (Journal Source) |
| **Cuenta contable** | Si | Si | Si | Si | Implicito (agrupado) | Si |
| **Descripcion de linea** | Si (Text) | Si (Description) | Si (Description) | Si (Memo) | Si (Label) | Si (Line Memo) |
| **Debito** | Si | Si | Si | Si | Si | Si |
| **Credito** | Si | Si | Si | Si | Si | Si |
| **Saldo acumulado** | No (estandar) | Si (Balance) | Si (Running YTD) | Si | Si | No (estandar) |
| **Moneda** | Si | Si | Si | Configurable | Configurable | Si |
| **Tercero (proveedor/cliente)** | No (estandar) | Si (Third-Party) | No (estandar) | Si (Name) | Si (Partner) | Via Worktags |

### 8.2 Campos de referencia al documento de origen

| Campo de referencia | SAP S/4HANA | Oracle Fusion | Dynamics 365 | NetSuite | Odoo | Workday |
|---------------------|:-----------:|:-------------:|:------------:|:--------:|:----:|:-------:|
| **Referencia al documento externo** | Si (`Reference`/XBLNR) | No (estandar) | No (estandar) | Si (`Num`) | Si (`Reference`) | No (estandar) |
| **Numero de transaccion de origen** | Via drill-down | Si (`Transaction Number` en SLA) | Si (`Document Number` en TB Detail) | Si (`Num`) | Si (`Reference` = move name) | Via drill-down |
| **Numero de factura del proveedor** | En `Reference` (si se captura) | Via drill-down a AP | Via drill-down a AP | Via drill-down a Bill | Es el `Reference` (move name) | Via drill-down |
| **Numero de orden de compra** | En `Assignment` o Special Field | Via drill-down | Via drill-down | Via drill-down | Via drill-down | Via Worktags |
| **Numero de documento fiscal regulado** | Via drill-down (3 niveles) | Via drill-down (3 niveles) | Via drill-down (2 niveles) | Via drill-down | Via drill-down | Via drill-down |
| **Cuenta de contrapartida** | No (estandar) | No (estandar) | No (estandar) | Si (`Split`) | No (estandar) | No (estandar) |

### 8.3 Documento fiscal como columna estandar vs. personalizable

| ERP | Columna estandar | Personalizable | Mecanismo de personalizacion |
|-----|:-----------------:|:--------------:|------------------------------|
| **SAP S/4HANA** | No | Si | Special Fields (IMG config) + Layout Variants |
| **Oracle Fusion** | No | Si | OTBI (custom reports) + Configurable Account Analysis |
| **Dynamics 365** | No | Si | Financial Tags + Default Descriptions + Customization |
| **NetSuite** | No | Si | Report Builder + Saved Searches + Custom Fields |
| **Odoo** | No | Si (requiere codigo) | Herencia de `_get_columns_name` en modelo de reporte |
| **Workday** | No | Si | Calculated Fields + Custom Reports |

---

## 9. Respuestas a las preguntas clave

### Pregunta 1: Que columnas/campos muestran los auxiliares contables en cada ERP?

**Respuesta:** Ver secciones 2-7 para el detalle por ERP. El nucleo comun a todos es:

1. **Fecha** de contabilizacion o transaccion
2. **Identificador del asiento** (Document Number, Voucher, Journal Number, Reference)
3. **Tipo** de documento o transaccion
4. **Cuenta contable**
5. **Descripcion** o texto de la linea
6. **Debito y Credito** (montos)
7. **Un campo de referencia** al documento de origen (con nombre y granularidad variable)

Los campos que varian significativamente entre ERPs son: saldo acumulado (no todos lo muestran por defecto), nombre del tercero (solo algunos), moneda de origen (solo algunos), y cuenta de contrapartida (solo NetSuite).

### Pregunta 2: Aparece el numero del documento fiscal directamente en el auxiliar contable?

**Respuesta: No, en ningun ERP investigado.** El numero del documento fiscal regulado (CUFE colombiano, NCF dominicano, CFDI mexicano, UUID) **nunca** aparece como columna estandar del auxiliar contable. En todos los ERPs, el acceso al numero fiscal requiere navegacion (drill-down) al documento de origen.

Lo que si aparece como campo de referencia es:
- **SAP:** `Reference` (XBLNR) — que tipicamente contiene el numero de factura del proveedor (no el numero fiscal regulado).
- **Oracle:** `Transaction Number` en los reportes de subledger — que es el numero interno de la transaccion AP/AR.
- **Dynamics 365:** `Document Number` en el Trial Balance Detail — que es el numero de factura interna.
- **NetSuite:** `Num` — que es el Document Number interno.
- **Odoo:** `Reference` — que es el nombre del `account.move` (numero de factura interna).
- **Workday:** No hay campo de referencia directa; se usa `Journal Source` + worktags.

### Pregunta 3: Que campos de referencia al documento de origen muestra el auxiliar?

**Respuesta:** Ver seccion 8.2 para la matriz completa. Resumen:

| ERP | Campo principal de referencia | Contenido tipico |
|-----|-------------------------------|------------------|
| SAP | `Reference` (XBLNR) + `Assignment` (ZUONR) | Numero de factura del proveedor + PO o campo configurable |
| Oracle | `Transaction Number` (en SLA Detail) | Numero interno de transaccion AP/AR |
| Dynamics 365 | `Document Number` (en TB Detail) + `Voucher` | Numero de factura interna + voucher contable |
| NetSuite | `Num` + `Name` | Document Number + nombre del tercero |
| Odoo | `Reference` + `Partner` | Nombre del account.move + nombre del tercero |
| Workday | `Journal Source` + Worktags | Tipo de origen + dimensiones del negocio |

### Pregunta 4: Los ERPs muestran la referencia fiscal como columna estandar o es un campo personalizable?

**Respuesta: Es siempre personalizable, nunca estandar.** Ningun ERP muestra el numero de documento fiscal regulado (NCF, CFDI, CUFE) como columna estandar del auxiliar contable. Todos permiten agregar campos personalizados al reporte, pero con distintos niveles de facilidad:

| Facilidad de personalizacion | ERPs |
|------------------------------|------|
| **Configurable por usuario (sin codigo)** | SAP (Special Fields + Layout), NetSuite (Report Builder), Dynamics 365 (Financial Tags) |
| **Configurable por admin con herramientas** | Oracle (OTBI), Workday (Custom Reports + Calculated Fields) |
| **Requiere desarrollo** | Odoo (codigo Python para modificar reporte) |

---

## 10. Conclusiones para el ERP Cosmos

### 10.1 Patron universal

Los ERPs lideres siguen un **patron consistente** para el auxiliar contable:

1. **El auxiliar contable es un reporte centrado en la cuenta contable**, no en el documento fiscal. Su proposito es mostrar los movimientos debito/credito que afectaron una cuenta en un periodo.

2. **El vinculo al documento de origen se hace mediante un campo de referencia generico**, no especifico para documentos fiscales. Este campo almacena una clave o numero que permite navegar al documento fuente.

3. **El drill-down es el mecanismo de trazabilidad**, no la inclusion de todos los campos del documento de origen en el reporte. Esto mantiene el reporte limpio y enfocado.

4. **La personalizacion de columnas es universal**: todos los ERPs permiten agregar campos adicionales para casos donde la organizacion necesita ver cierta informacion directamente en el auxiliar.

### 10.2 Implicaciones para el diseno de Contabilidad en Cosmos

1. **El asiento contable debe almacenar una referencia al hecho economico de origen** (`referenciaOrigen` ya esta definida en el contrato de `LineaTraduccion`). Esta referencia permite el drill-down al documento fuente.

2. **No es necesario replicar campos fiscales en el asiento contable.** El numero fiscal (NCF, CFDI, CUFE) pertenece al sub-dominio de Emision/Recepcion Electronica o al documento del sub-dominio transaccional. El auxiliar contable los muestra via navegacion.

3. **El auxiliar contable debe tener un campo de "Referencia" o "Descripcion" configurable** donde se puedan incluir datos del documento de origen (numero de factura del proveedor, numero de orden de compra, referencia libre). Este es el equivalente al campo `Reference` (XBLNR) de SAP o `Num` de NetSuite.

4. **Considerar un campo "Tipo de transaccion" o "Origen"** visible en el auxiliar, equivalente al `Document Type` de SAP o `Journal Source` de Workday, para que el usuario identifique rapidamente el tipo de operacion sin navegar al detalle.

5. **El drill-down desde el auxiliar al documento de origen es un requisito funcional critico.** Debe ser bidireccional: desde el auxiliar al documento fuente, y desde el documento fuente al asiento contable (ya contemplado en el Flujo 2, paso 7 de `definicion-alcance.md`).

---

## 11. Fuentes

### SAP S/4HANA
- [FBL3N GL Line Item Display - Sapsharks](https://sapsharks.com/fbl3n-gl-line-item-display/)
- [FBL3N - G/L Account Line Items Step-by-Step - ERPLingo](https://www.erplingo.com/sap-transaction-code/en/fbl3n)
- [Display Line Items in General Ledger - SAP Help Portal](https://help.sap.com/docs/SAP_S4HANA_CLOUD/0fa84c9d9c634132b7c4abb9ffdd8f06/bd874158706b9144e10000000a4450e5.html)
- [Reference Document Number XBLNR in FBL3N - SAPStack](https://sapstack.com/tables/reference-document-number-xblnr-in-fbl3n-table-in-sap)
- [ACDOCA Universal Journal Entry Line Items - ERPExplorer](https://www.erpexplorer.com/sap/s4/table/ACDOCA)
- [A Case for the FI Line Item Browsers under S/4 HANA Finance - SAP Community](https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-members/a-case-for-the-fi-line-item-browsers-under-s-4-hana-finance-part-2/ba-p/13387765)
- [Line Item Display Special Fields S4HANA - SAP Community](https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-sap/line-item-display-special-fields-sap-s4hana-conversion-migration/ba-p/13406751)
- [Enhancing SAP FBL*N/FAGLL03 Reports - LinkedIn](https://www.linkedin.com/pulse/enhancing-sap-fblnfagll03-reports-sidharth-jyothi)
- [Invoice Reference in Accounts Payable Documents - SAP Community](https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-members/invoice-reference-in-accounts-payable-documents/ba-p/13438293)

### Oracle Fusion Cloud
- [Oracle Fusion Subledger Accounting Reports R20B](https://docs.oracle.com/en/cloud/saas/financials/20b/ocuar/oracle-fusion-subledger-accounting-reports.html)
- [Journal Reports - Oracle Fusion Using Subledger Accounting 25C](https://docs.oracle.com/en/cloud/saas/financials/25c/fausl/journal-reports.html)
- [Configurable Account Analysis - Oracle Analytics](https://docs.oracle.com/en/cloud/saas/analytics/25r4/faiae/configurable-account-analysis.html)
- [Account Analysis Report with Subledger Detail - Oracle GL Users Guide](https://docs.oracle.com/cd/A60725_05/html/comnls/us/gl/anlysr02.htm)
- [Oracle Fusion Subledger Accounting Predefined Reports](https://docs.oracle.com/cd/E51367_01/financialsop_gs/OCUAR/F1559317AN1677A.htm)

### Microsoft Dynamics 365 Finance
- [View Journal Entries and Transactions - Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/finance/general-ledger/view-journal-entries-transactions)
- [Trial Balance with Transactional Detail Report - Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/finance/public-sector/general-ledger-public-sector-trial-balance)
- [Generate Trial Balance with Transactional Detail - Dynamics Community](https://exploredynamics365.home.blog/2020/11/20/generate-the-trial-balance-with-transactional-detail-report-new-feature-in-microsoft-dynamics-365-finance-and-operations/)
- [Invoice Number in Voucher Transactions - Dynamics Community Forum](https://community.dynamics.com/forums/thread/details/?threadid=1490eec5-6a87-413f-bf9c-4b5d2a922840)
- [Customer and Vendor Information on Voucher Transactions - Dynamics Blog](https://community.dynamics.com/blogs/post/?postid=fc910159-ae9e-4cf7-a908-c4173297cb7e)

### NetSuite
- [NetSuite GL Detail Report - NuageCG](https://nuagecg.com/blog/netsuite-gl-detail-report/)
- [Account Detail Report - NetSuite Documentation](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_N1522428.html)
- [General Ledger Report - NetSuite Documentation](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_N1520771.html)
- [General Ledger, Trial Balance and Transaction Detail Reports - ERP Professor](https://www.erpprofessor.com/lesson/general-ledger-trial-balance-and-transaction-detail-reports/)

### Odoo
- [Partner Reports in Odoo 15 Accounting - Cybrosys](https://www.cybrosys.com/odoo/odoo-books/odoo-book-v15/accounting/partner-reports/)
- [How to Add Custom Column in Odoo 18 General Ledger - Netilligence](https://www.netilligence.io/blog/how-can-you-add-a-custom-column-in-odoo-18-general-ledger/)
- [How to Show Field on General Ledger Report - Odoo Forum](https://www.odoo.com/forum/help-1/how-to-show-field-on-general-ledger-report-159745)
- [Add Column to General Ledger Odoo 16 - Odoo Forum](https://www.odoo.com/forum/help-1/add-column-to-general-ledger-odoo16-228578)

### Workday
- [Journal Entries and Expenses with Ledger Accounts in Workday - Apideck](https://developers.apideck.com/guides/journal-entries-expenses-ledger-accounts-workday)
- [Workday Financial Reports - Montgomery College](https://info.montgomerycollege.edu/_documents/offices/procurement/workday/common-financial-reports-employees.pdf)
- [Workday Financial Reports Training - SLU](https://www.slu.edu/business-finance/departments-and-offices/financial-services/-pdf/workday-financial-report-training-5-19.pdf)
