# Investigacion: Numeracion Fiscal vs. Numeracion Contable

> **Fecha:** 2026-03-25
> **Proposito:** Documentar como los ERPs lideres y las normativas nacionales/internacionales manejan la relacion entre la numeracion fiscal de documentos (facturas, notas credito) y la numeracion contable interna (comprobantes/asientos).
> **Version:** 1.0

---

## Tabla de contenido

1. [Resumen ejecutivo](#1-resumen-ejecutivo)
2. [Pregunta 1 — Numeracion fiscal vs. contable en ERPs](#2-pregunta-1--numeracion-fiscal-vs-contable-en-erps)
3. [Pregunta 2 — Normativa por pais](#3-pregunta-2--normativa-por-pais)
4. [Pregunta 3 — Herencia o referencia cruzada](#4-pregunta-3--herencia-o-referencia-cruzada)
5. [Pregunta 4 — Quien es dueno de la numeracion fiscal en los ERPs](#5-pregunta-4--quien-es-dueno-de-la-numeracion-fiscal-en-los-erps)
6. [Pregunta 5 — Caso especifico Republica Dominicana (NCF)](#6-pregunta-5--caso-especifico-republica-dominicana-ncf)
7. [Matriz consolidada](#7-matriz-consolidada)
8. [Conclusiones para el ERP Cosmos](#8-conclusiones-para-el-erp-cosmos)
9. [Fuentes](#9-fuentes)

---

## 1. Resumen ejecutivo

La numeracion fiscal (autorizada por el ente regulador) y la numeracion contable interna (comprobantes/asientos del Libro Mayor) son **dos sistemas de numeracion independientes** en todos los ERPs lideres y en todas las normativas fiscales investigadas. No existe ningun ERP ni normativa donde el asiento contable use la misma numeracion que la factura fiscal. La unica excepcion parcial es **Odoo**, donde la factura y el asiento contable son el mismo registro (`account.move`) y comparten numero — pero esto es una decision arquitectonica monolitica, no un requisito fiscal.

**Hallazgos clave:**

| Hallazgo | Detalle |
|----------|---------|
| **Dos numeraciones siempre separadas** | En SAP, Oracle, Dynamics 365, NetSuite y Workday, la factura (documento fiscal/comercial) y el asiento contable tienen numeraciones independientes con referencia cruzada. |
| **La numeracion fiscal pertenece al documento comercial** | En todas las normativas investigadas (Colombia, RD, Mexico, Chile, Panama, Espana), la numeracion fiscal se asigna al documento comercial (factura, nota credito), NO al asiento contable. |
| **El modulo de facturacion/ventas administra la numeracion fiscal** | No el modulo contable. En SAP es SD (Sales & Distribution), en Oracle es AR (Accounts Receivable), en Dynamics es AR. El modulo contable solo recibe una referencia. |
| **Referencia cruzada, no herencia** | Los ERPs vinculan factura y asiento mediante una referencia (numero de factura como campo del asiento), pero el asiento tiene su propia numeracion independiente. |
| **Odoo es la excepcion arquitectonica** | Desde v13+, factura = asiento (`account.move`), mismo registro, mismo numero. Pero incluso en Odoo, los paises con numeracion fiscal regulada usan secuencias separadas o campos adicionales para el numero fiscal. |

---

## 2. Pregunta 1 — Numeracion fiscal vs. contable en ERPs

### SAP S/4HANA — Dos documentos, dos numeraciones

En SAP, cuando se genera una factura de venta (billing document en SD), el sistema crea **dos documentos separados**:

1. **Billing Document (SD):** Tiene su propio numero generado por el rango de numeracion configurado en transaccion VBN1 (ej: 90023457). Este es el documento comercial/fiscal que se envia al cliente.
2. **Accounting Document (FI):** Tiene su propio numero generado por el rango de numeracion configurado en transaccion FBN1 (ej: 100000029). Este es el asiento contable que se registra en el Libro Mayor.

**Son dos registros independientes** con numeraciones independientes. El Accounting Document contiene una referencia al Billing Document para trazabilidad.

**Configuracion para igualar numeros (opcional):** SAP permite que el FI document use el mismo numero que el SD billing document configurando el rango FI como **externo** (external number assignment). En este modo, SD "pasa" su numero a FI. Esto se configura mediante:
- Transaccion VOFA: mapear billing document type a FI document type.
- Transaccion FBN1: definir el rango FI como externo con el mismo intervalo que el rango SD.

Esta configuracion es una **conveniencia operativa** (para que ambos equipos usen el mismo numero), NO un requisito fiscal ni contable. La practica estandar es tener numeros separados.

### Oracle Fusion — Tres niveles de numeracion

Oracle Fusion tiene la separacion mas clara de todos los ERPs:

1. **AR Transaction Number:** Numero de la factura en Accounts Receivable. Generado por Document Sequences configuradas a nivel de Ledger/Business Unit.
2. **SLA Subledger Journal Number:** Numero del asiento en el Subledger Accounting (SLA), generado cuando se ejecuta el proceso "Create Accounting".
3. **GL Journal Number:** Numero del journal en el General Ledger, asignado al transferir del SLA al GL.

Multiples facturas de AR pueden agruparse en un solo journal de GL (batch posting). La trazabilidad se mantiene mediante drill-down: desde GL se navega a SLA, desde SLA a AR.

Oracle soporta **Document Sequencing** con modo Gapless (sin gaps) para cumplir requisitos legales de numeracion consecutiva en facturas, independiente de la secuencia de journals.

### Microsoft Dynamics 365 — Invoice Number vs. Voucher Number

Dynamics 365 distingue explicitamente:

1. **Invoice Number:** Identifica el documento comercial/fiscal (la factura enviada al cliente o recibida del proveedor). Generado por una Number Sequence.
2. **Voucher Number:** Identifica el registro contable en el General Ledger. Generado por una Number Sequence diferente, asociada al Journal Name.

**Son dos numeraciones independientes por defecto.** Dynamics 365 permite configurar que el Voucher Number reutilice el Invoice Number (misma Number Sequence), pero es una opcion de configuracion, no el comportamiento por defecto.

Dynamics 365 soporta **numeracion cronologica** (requisito legal en Francia y otros paises europeos): garantiza que un voucher posterior siempre tenga numero mayor que uno anterior.

### NetSuite — Triple sistema de numeracion

NetSuite opera con tres niveles de numeracion:

1. **Document Number / Invoice Number:** Numero de la factura (ej: INV0001). Configurable con Advanced Numbering por Transaction Type + Subsidiary.
2. **Transaction Number:** Consecutivo interno automatico asignado por el sistema. No configurable, siempre secuencial.
3. **GL Audit Number:** Secuencia separada, gapless, generada para cumplir requisitos de auditoria contable. Independiente de las otras dos numeraciones. Se configura en Setup > Company > GL Audit Numbering.

El GL Audit Number es especificamente independiente del Auto-Generated Numbers de las transacciones. Un invoice INV0001 puede tener GL Audit Number 00045 sin relacion numerica.

### Odoo — Excepcion: factura = asiento (mismo registro)

Odoo es el **unico ERP** de los investigados donde la factura y el asiento contable son el **mismo registro**:

- Desde Odoo v13+, `account.invoice` fue fusionado con `account.move` (PR #33797 del repositorio oficial). Toda factura, nota credito o asiento manual es un registro `account.move`.
- El numero de la factura ES el numero del asiento contable. Comparten la secuencia del Journal asignado (ej: `INV/2026/0001` para ventas, `BILL/2026/0001` para compras).
- No existe un "asiento contable separado" de la factura — son la misma entidad.

**Limitacion para localizaciones fiscales:** En paises con numeracion fiscal regulada (ej: Colombia, Mexico, RD), los modulos de localizacion de Odoo agregan campos adicionales (ej: `l10n_latam_document_number`) para almacenar la numeracion fiscal, que puede ser diferente del nombre del `account.move`. Es decir, incluso en Odoo, la numeracion fiscal puede diferir de la numeracion del registro contable cuando la ley lo exige.

### Workday — Separacion total

Workday mantiene separacion completa:

1. **Customer Invoice ID / Document Number:** Identificador del documento comercial en Revenue Management. Tiene su propia secuencia.
2. **Journal Sequence Number:** Numero del journal en Accounting, asignado por Company + Ledger Type + Journal Source.

Los ajustes a una factura de un periodo anterior generan journals de reversa y re-registro con **nuevos numeros de journal**, mientras el Invoice ID original permanece intacto. Esto confirma la independencia de las dos numeraciones.

---

## 3. Pregunta 2 — Normativa por pais

### Colombia — Resolucion de numeracion DIAN

**Que regula la DIAN:**
- La DIAN autoriza **rangos de numeracion** para documentos del **sistema de facturacion**: facturas electronicas de venta, facturas de contingencia, notas credito, notas debito, y documentos soporte de adquisiciones a no obligados a facturar.
- La autorizacion define: prefijo, rango de numeracion (desde-hasta), periodo de vigencia, modalidad (electronica, contingencia).
- Normativa vigente: Resolucion Unica DIAN 000227 de 2025 (que compila la Resolucion 000165 de 2023).

**Que NO regula la DIAN en numeracion:**
- La DIAN **no regula** la numeracion de comprobantes contables internos (asientos del Libro Mayor). La numeracion de comprobantes contables se rige por el Decreto 2649 de 1993 (Art. 124), que exige consecutivos por clase de comprobante, pero no requiere autorizacion de la DIAN.
- La resolucion de numeracion aplica al **documento comercial/fiscal** (factura), no al asiento contable.

**Implicacion:** En Colombia, el modulo de facturacion/ventas (o el modulo de emision electronica) administra la resolucion de numeracion DIAN. El modulo contable recibe una referencia al numero de factura pero tiene su propia numeracion independiente para los comprobantes contables.

### Republica Dominicana — NCF (Numero de Comprobante Fiscal)

**Que es el NCF:**
- El Numero de Comprobante Fiscal (NCF) es un identificador unico asignado por la DGII (Direccion General de Impuestos Internos) que identifica cada transaccion comercial.
- Estructura (desde 2025): 11 caracteres — una letra de serie (B) + 2 digitos del tipo de comprobante + 8 digitos de secuencia.
- Para e-CF (comprobantes electronicos): 13 caracteres — una letra (E) + 2 digitos del tipo + 10 digitos de secuencia.

**Tipos principales de NCF:**
| Codigo | Tipo | Uso |
|--------|------|-----|
| B01 | Factura de Credito Fiscal | Reporta gastos e ingresos. Requiere RNC/Cedula del comprador. Permite deduccion. |
| B02 | Factura de Consumo | Reporta ingresos (no gastos). Consumidor final. No requiere documento. |
| B03 | Nota de Debito | Ajuste que incrementa el valor de una transaccion. |
| B04 | Nota de Credito | Ajuste que reduce el valor de una transaccion. |
| B11 | Comprobante de Compras | Para compras a proveedores informales. |
| B13 | Comprobante de Gastos Menores | Gastos menores sin soporte formal. |
| B14 | Comprobante de Regimenes Especiales | Para operaciones con zonas francas. |
| B15 | Comprobante Gubernamental | Para ventas al sector gubernamental. |

**NCF vs. numeracion contable:**
- El NCF se asigna al **documento comercial** (factura de venta, nota credito), no al asiento contable.
- Los recibos de caja y otros documentos internos tienen su propia numeracion simple sin NCF.
- Los ERPs que operan en RD (ej: Alegra, Odoo-RD) mantienen el NCF como campo del documento de facturacion, separado de la numeracion contable interna.
- La trazabilidad se mantiene porque el asiento contable referencia el NCF como dato del documento origen.

**Relacion con formatos DGII:**
- Formato 606 (compras), 607 (ventas), 608 (NCF anulados) reportan transacciones por su NCF. Estos reportes se alimentan de los documentos comerciales, no de los asientos contables.

### Mexico — CFDI y folio fiscal (UUID)

**Que es el CFDI:**
- Comprobante Fiscal Digital por Internet. Formato XML obligatorio con firma digital del emisor y sello del PAC (Proveedor Autorizado de Certificacion).
- Cada CFDI recibe un **folio fiscal (UUID)** — codigo unico de 36 caracteres asignado por el PAC y validado por el SAT.

**UUID vs. numeracion contable:**
- El UUID es el identificador del documento fiscal. Es completamente independiente de la numeracion contable interna.
- Sin embargo, la contabilidad electronica mexicana (Anexo 24 de la Resolucion Miscelanea Fiscal) **exige** que las polizas contables incluyan el UUID del CFDI que soporta la operacion. Es decir, el asiento contable debe referenciar el UUID, pero tiene su propia numeracion de poliza.
- Las polizas contables siguen su propia secuencia numerica (ej: poliza de diario 001, poliza de egresos 001).

**Implicacion:** Mexico es el pais que mas explicitamente exige la **referencia cruzada** entre numeracion fiscal y contable, pero NO la igualdad de numeros.

### Chile — DTE (Documento Tributario Electronico)

**Que es el DTE:**
- Documentos tributarios emitidos en formato XML con firma digital y timbre electronico, validados por el SII (Servicio de Impuestos Internos).
- Cada tipo de DTE tiene un codigo oficial (ej: 33 = Factura Electronica, 34 = Factura No Afecta o Exenta, 61 = Nota de Credito).

**Folios:**
- El SII autoriza **rangos de folios** electronicos por tipo de DTE. La empresa solicita folios al SII (ej: del 50 al 110 para facturas tipo 33) mediante certificado digital.
- El folio es la numeracion fiscal del documento. Es independiente de la numeracion contable.

**DTE vs. numeracion contable:**
- Los DTE se numeran con los folios autorizados por el SII. Los asientos contables tienen su propia numeracion en el libro diario.
- Desde 2018, todos los contribuyentes estan obligados a emitir DTE electronicos.

### Panama — Facturacion electronica (SFEP)

**Marco regulatorio:**
- Ley 256 de 2021 y Decreto Ejecutivo 766 de 2020 establecen el sistema de facturacion electronica.
- La DGI (Direccion General de Ingresos) administra el Sistema de Facturacion Electronica de Panama (SFEP).

**Numeracion:**
- Cada factura electronica recibe un **CUFE** (Codigo Unico de Factura Electronica) — identificador unico generado por el sistema.
- La DGI emite autorizaciones para emision de documentos fiscales.
- La numeracion fiscal es independiente de la numeracion contable interna.

**Estado:** Panama esta en proceso de implementacion progresiva de la facturacion electronica. La numeracion SI esta regulada por la DGI.

### Espana — SII (Suministro Inmediato de Informacion)

**Que es el SII:**
- Sistema de la Agencia Tributaria (AEAT) que exige el suministro electronico de los registros de facturacion en un plazo de 4 dias desde la emision/recepcion.
- NO es facturacion electronica — es la obligacion de reportar los datos de las facturas emitidas y recibidas.

**Numeracion de factura vs. asiento contable:**
- La factura tiene su propia numeracion secuencial. Un numero de factura no puede repetirse para el mismo expedidor y fecha.
- El asiento contable en el libro diario tiene su propia numeracion independiente.
- El SII permite **opcionalmente** incluir el numero de asiento contable como referencia en el registro del SII, pero son dos numeraciones independientes. Cita textual de la AEAT: *"El objetivo de este campo es que aquellos sujetos pasivos que asi lo estimen oportuno, puedan utilizarlo con la finalidad que pudiera tener en sus anteriores Libros registro, por ejemplo: puede informarse del numero de asiento contable."*

---

## 4. Pregunta 3 — Herencia o referencia cruzada

### Practica estandar: referencia cruzada, NO herencia

En todos los ERPs investigados (excepto Odoo), el asiento contable contiene un **campo de referencia** al documento de origen (factura, nota credito), pero tiene su **propia numeracion independiente**.

| ERP | Campo de referencia en el asiento | Numeracion del asiento |
|-----|-----------------------------------|------------------------|
| **SAP** | Reference field (XBLNR) contiene el numero de billing document | Numeracion propia por Document Type + Company Code + Fiscal Year |
| **Oracle** | Drill-down desde GL Journal → SLA → AR Transaction | Numeracion propia por Document Sequence del GL |
| **Dynamics 365** | Invoice Number como campo del voucher | Voucher Number propio por Number Sequence del Journal Name |
| **NetSuite** | Memo/Reference fields | Document Number propio + GL Audit Number separado |
| **Workday** | Reference fields en journal lines | Journal Sequence Number propio por Company + Ledger Type + Source |
| **Odoo** | N/A — son el mismo registro | Mismo numero (excepcion) |

### SAP permite igualar numeros (configuracion opcional)

SAP es el unico ERP que soporta nativamente que el Accounting Document use el mismo numero que el Billing Document, mediante numeracion externa (external number assignment). Esto requiere:
1. El rango de FI se configura como externo (FBN1).
2. SD pasa su numero al crear el FI document.
3. Los rangos deben coincidir y no solaparse con otros.

Esta es una **conveniencia operativa** utilizada por algunas empresas para simplificar la referencia, pero NO es la practica por defecto ni un requisito de ninguna normativa.

### Dynamics 365 permite reutilizar la secuencia

Dynamics 365 permite configurar que el Voucher Number use la misma Number Sequence que el Invoice Number. Al igual que SAP, es una opcion de configuracion, no el comportamiento por defecto.

### Conclusion: nunca es obligatorio

Ninguna normativa fiscal en ningun pais investigado exige que el asiento contable tenga el mismo numero que la factura. Lo que algunas normativas exigen (especialmente Mexico) es que el asiento **referencie** el documento fiscal, pero con su propia numeracion.

---

## 5. Pregunta 4 — Quien es dueno de la numeracion fiscal en los ERPs

### Respuesta universal: el modulo de facturacion/ventas

| ERP | Modulo dueno de la numeracion fiscal | Modulo dueno de la numeracion contable |
|-----|-------------------------------------|---------------------------------------|
| **SAP** | SD (Sales & Distribution) via VBN1. Para compras: MM via MIRO/MR8M. | FI (Financial Accounting) via FBN1. |
| **Oracle** | AR (Accounts Receivable) para ventas. AP (Accounts Payable) para compras. Document Sequences configuradas por modulo. | GL (General Ledger) tiene sus propias Document Sequences. SLA intermedia. |
| **Dynamics 365** | AR/AP — cada modulo con su Number Sequence para invoices. | GL — Number Sequences por Journal Name. |
| **NetSuite** | Transaction-level numbering por tipo (Invoice, Credit Memo, etc.) con Advanced Numbering. | GL Audit Numbering separado, configurado a nivel global. |
| **Odoo** | El Journal de ventas/compras maneja la secuencia (fusionada con contable). Para localizaciones con numeracion fiscal, modulo de localizacion. | Mismo Journal (fusionado). |
| **Workday** | Revenue Management para customer invoices. Procurement para supplier invoices. | Account Posting Rules + Journal Sequences. |

### No existe un "modulo fiscal independiente" para numeracion

En ninguno de los ERPs investigados hay un modulo fiscal o tributario que administre la numeracion de facturas. La numeracion fiscal siempre la administra el modulo que **emite** el documento:
- Facturas de venta → Modulo de ventas/facturacion (SD, AR, Revenue Management).
- Facturas de compra → El numero lo asigna el proveedor; el ERP solo lo registra.
- Notas credito/debito → El modulo que las emite (generalmente el mismo de ventas).

El modulo de impuestos (cuando existe como modulo separado, como en Avalara o Vertex) **calcula tributos** pero no administra la numeracion fiscal.

### Caso especial: localizaciones

En paises con requisitos especificos (Colombia-DIAN, RD-DGII, Mexico-SAT, Chile-SII), los ERPs implementan **modulos de localizacion** o **integraciones con proveedores de facturacion electronica** que:
1. Administran las resoluciones/autorizaciones de numeracion del ente regulador.
2. Asignan el numero fiscal autorizado al documento comercial.
3. Generan el documento electronico (XML) para transmision al ente regulador.

Estos modulos de localizacion son extensiones del modulo de facturacion, no del modulo contable.

---

## 6. Pregunta 5 — Caso especifico Republica Dominicana (NCF)

### Que es el NCF

El **Numero de Comprobante Fiscal (NCF)** es el identificador legal que la DGII asigna a cada documento fiscal en Republica Dominicana. Es el equivalente dominicano a la resolucion de numeracion colombiana, pero con una estructura mas granular por tipo de transaccion.

### Quien lo emite

La **DGII** autoriza secuencias de NCF al contribuyente. El contribuyente solicita la autorizacion (alta de NCF) en el portal de la DGII, indicando el tipo de comprobante y la cantidad de numeros requeridos. La DGII aprueba un rango de secuencia.

### Se asigna al documento comercial, NO al registro contable

- El NCF se imprime/asocia a la **factura de venta**, nota de credito, nota de debito u otro documento comercial.
- El asiento contable en el Libro Diario tiene su propia numeracion contable interna.
- La trazabilidad se mantiene porque el asiento referencia el NCF del documento origen.

### Como lo manejan los ERPs en RD

Los ERPs que operan en Republica Dominicana (Alegra, Odoo con localizacion dominicana, Softland, etc.):

1. **Mantienen un catalogo de secuencias NCF** configurado por tipo de comprobante (B01, B02, B04, etc.), con rango autorizado por la DGII, fecha de vencimiento y secuencia actual.
2. **Asignan el NCF al crear el documento comercial** (factura, nota credito). El modulo de facturacion consume la secuencia NCF.
3. **El modulo contable genera su asiento con numeracion propia** — la secuencia del comprobante contable (ej: CD-001, CE-001) es independiente del NCF.
4. **El asiento referencia el NCF** como dato del documento origen para trazabilidad y para generar los formatos DGII (606, 607, 608).

### Vinculacion NCF y asiento contable

```
Factura de venta (NCF: B0100000045)
    ↓ genera
Asiento contable (Comprobante: CD-2026-0312)
    → campo referencia: "B0100000045"
    → campo tipo_documento_fiscal: "B01"
```

El asiento contable NO tiene NCF. El NCF pertenece exclusivamente al documento comercial. El asiento contable referencia el NCF para:
- Trazabilidad (drill-down desde contabilidad al documento fiscal).
- Generacion de formatos DGII (el formato 607 de ventas requiere NCF + monto; esta informacion viene del documento comercial, no del asiento contable).

---

## 7. Matriz consolidada

### Numeracion fiscal vs. contable por ERP

| Dimension | SAP | Oracle | D365 | NetSuite | Odoo | Workday |
|-----------|-----|--------|------|----------|------|---------|
| **Factura tiene su propio numero** | Si (SD Billing Document) | Si (AR Transaction Number) | Si (Invoice Number) | Si (Document Number) | Si (mismo que asiento*) | Si (Customer Invoice ID) |
| **Asiento tiene su propio numero** | Si (FI Accounting Document) | Si (GL Journal Number) | Si (Voucher Number) | Si (GL Audit Number) | Si (mismo que factura*) | Si (Journal Sequence Number) |
| **Son dos numeraciones independientes** | Si (por defecto) | Si (siempre) | Si (por defecto) | Si (siempre) | No* | Si (siempre) |
| **Permite igualar numeros** | Si (external numbering) | No | Si (shared sequence) | No | N/A | No |
| **Referencia cruzada** | XBLNR field | SLA drill-down | Invoice field en voucher | Reference/memo | N/A | Reference fields |

*En Odoo, factura y asiento son el mismo registro (`account.move`). En paises con numeracion fiscal regulada, el modulo de localizacion agrega un campo separado para el numero fiscal.

### Numeracion fiscal por pais

| Pais | Ente regulador | Identificador fiscal | Se asigna a | Modulo dueno | Independiente del asiento contable |
|------|---------------|---------------------|-------------|-------------|-----------------------------------|
| **Colombia** | DIAN | Resolucion de numeracion (prefijo + rango) | Factura electronica de venta, notas, doc. soporte | Facturacion / Emision electronica | Si |
| **Rep. Dominicana** | DGII | NCF (B01, B02, etc.) / e-NCF | Factura, notas credito/debito | Facturacion | Si |
| **Mexico** | SAT | UUID (folio fiscal via PAC) | CFDI (factura, complemento de pago) | Facturacion + PAC externo | Si (pero asiento debe referenciar UUID) |
| **Chile** | SII | Folio DTE (por tipo de documento) | DTE (factura, nota credito, guia) | Facturacion + SII | Si |
| **Panama** | DGI | CUFE (Codigo Unico de Factura Electronica) | Factura electronica | Facturacion + SFEP | Si |
| **Espana** | AEAT | Numero de factura (secuencial propio) | Factura emitida/recibida | Facturacion | Si (SII permite incluir num. asiento como referencia opcional) |

---

## 8. Conclusiones para el ERP Cosmos

### Principios confirmados por la investigacion

1. **La numeracion fiscal y la numeracion contable son dominios diferentes.** La numeracion fiscal pertenece al documento comercial (factura, nota credito) y es regulada por el ente tributario. La numeracion contable pertenece al comprobante del Libro Mayor y se rige por normas contables internas (en Colombia, Decreto 2649).

2. **El modulo de facturacion/emision electronica es el dueno de la numeracion fiscal.** No el modulo contable ni el modulo de impuestos. El modulo de impuestos calcula tributos; el modulo de facturacion administra resoluciones y asigna numeros fiscales.

3. **El asiento contable referencia el documento fiscal, no al reves.** El asiento contable contiene una referencia al numero del documento fiscal que lo origino. El documento fiscal no contiene el numero del asiento contable (salvo en Espana donde es opcional via SII).

4. **No existe ningun requisito normativo para que el asiento contable tenga el mismo numero que la factura.** Esto confirma que el sub-dominio de Contabilidad puede operar con su propia numeracion independiente, como ya esta disenado en el modelo actual.

5. **Mexico es el pais mas exigente en trazabilidad fiscal-contable.** Exige que las polizas contables incluyan el UUID del CFDI. Esto refuerza la necesidad de que el asiento contable tenga un campo de referencia al documento fiscal de origen.

### Implicaciones para el diseno del ERP Cosmos

| Implicacion | Detalle |
|-------------|---------|
| **Contabilidad no administra resoluciones de numeracion fiscal** | El sub-dominio de Contabilidad genera comprobantes con su propia numeracion. Las resoluciones DIAN/DGII/SAT son responsabilidad del sub-dominio de Facturacion (o Emision Electronica). |
| **El asiento contable debe tener un campo de referencia al documento fiscal** | Para trazabilidad bidireccional y para cumplir requisitos como el Anexo 24 de Mexico. Este campo ya existe conceptualmente en la "referencia al hecho economico de origen" del modelo actual. |
| **Impuestos calcula, Facturacion numera** | El sub-dominio de Impuestos es responsable del calculo tributario. La asignacion de NCF/resolucion DIAN/UUID es responsabilidad del sub-dominio que emite el documento fiscal. |
| **La linea de traduccion contable debe incluir la referencia fiscal** | Cuando un sub-dominio emite lineas de traduccion a Contabilidad, debe incluir el identificador fiscal del documento (NCF, UUID, numero de resolucion) como parte del contexto del hecho economico. |

---

## 9. Fuentes

### SAP
- [Align R/3 FI Accounting and SD Billing Document Numbers](https://sapinsider.org/align-r-3-fi-accounting-and-sd-billing-document-numbers-to-keep-everyone-on-the-same-page/)
- [Invoice & Accounting Document Number — SAP Community](https://community.sap.com/t5/enterprise-resource-planning-q-a/invoice-accounting-document-number/qaq-p/7899233)
- [Accounting Document Number same as Billing Document Number — SAP Note 2826591](https://userapps.support.sap.com/sap/support/knowledge/en/2826591)
- [How to Make the Accounting Document Number Same as the Billing Number — SAP Note 3600066](https://userapps.support.sap.com/sap/support/knowledge/en/3600066)
- [SAP FI Document Number Ranges — TutorialsPoint](https://www.tutorialspoint.com/sap_fico/sap_fi_document_number_ranges.htm)
- [Understanding the Interface Between SD and FI — SAP Learning](https://learning.sap.com/learning-journeys/configuring-billing-in-sap-s-4hana-sales/understanding-the-interface-between-sales-and-distribution-and-financial-accounting)
- [Tax Invoice Numbering — SAP Help](https://help.sap.com/docs/SAP_ERP/a70c5fce76eb44adb0c86a9d3059e4dd/4183d0531d8b4208e10000000a174cb4.html)

### Oracle Fusion
- [AR Invoice and GL Journal Query — Oracle Community](https://community.oracle.com/mosc/discussion/4483269/ar-invoice-and-gl-journal-query-in-oracle)
- [Document Sequencing in Fusion Receivables — Oracle Support 1678427](https://support.oracle.com/knowledge/Oracle%20Cloud/1678427_1.html)
- [Query to get AR Invoice SLA and GL Details — Tech7](https://www.tech7.in/blog/query-to-get-ar-invoice-sla-and-gl-details)

### Microsoft Dynamics 365
- [Chronological Invoice and Voucher Numbers — Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/finance/localizations/france/emea-fra-chronological-invoices-vouchers)
- [Numbering Documents and Vouchers Chronologically — Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/finance/accounts-receivable/chrono-numbers)
- [One Voucher — Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/finance/general-ledger/one-voucher)

### NetSuite
- [GL Audit Numbering — Oracle NetSuite Documentation](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_3735573963.html)
- [Mastering GL Audit Numbering in NetSuite — Tvarana](https://www.tvarana.com/blog/mastering-gl-audit-numbering-in-netsuite)
- [GL Audit Numbering in NetSuite — SuiteRep](https://suiterep.com/2023/09/19/gl-audit-numbering-in-netsuite/)

### Odoo
- [Invoice Sequence — Odoo 18.0 Documentation](https://www.odoo.com/documentation/18.0/applications/finance/accounting/customer_invoices/sequence.html)
- [Merge account.invoice & account.move — Odoo GitHub PR #33797](https://github.com/odoo/odoo/pull/33797)
- [The account.move Model — Dasolo](https://www.dasolo.ai/blog/odoo-data-api-5/odoo-account-move-model-guide-157)

### Workday
- [Creating Customer Invoices in Workday — Simmons University](https://internal.simmons.edu/wp-content/uploads/2023/12/Creating-Customer-Invoices-in-Workday.pdf)
- [Customer Invoices in Workday — Washington University](https://workday.wustl.edu/items/customer-invoices-in-workday-reference-guide/)

### Colombia
- [DIAN — Sistema de Facturacion Electronica](https://micrositios.dian.gov.co/sistema-de-facturacion-electronica/)
- [DIAN — Preguntas frecuentes de numeracion de facturacion](https://micrositios.dian.gov.co/sistema-de-facturacion-electronica/numeracion-de-facturacion-preguntas-frecuentes/)
- [DIAN — ABC Factura Electronica](https://www.dian.gov.co/impuestos/factura-electronica/Documents/Abece-FE-Facturador.pdf)
- [Resolucion 000165 de 2023 — Normograma DIAN](https://normograma.dian.gov.co/dian/compilacion/docs/resolucion_dian_0165_2023.htm)

### Republica Dominicana
- [DGII — Estructura y Tipos de Comprobantes](https://dgii.gov.do/cicloContribuyente/facturacion/comprobantesFiscales/Paginas/tiposComprobantes.aspx)
- [DGII — Autorizacion para emitir NCF](https://dgii.gov.do/cicloContribuyente/facturacion/comprobantesFiscales/Paginas/autorizacionNCF.aspx)
- [DGII — Guia Informativa sobre Comprobantes Fiscales](https://dgii.gov.do/publicacionesOficiales/bibliotecaVirtual/contribuyentes/facturacion/Documents/Comprobantes%20Fiscales/2-Guia-Informativa-NCF.pdf)
- [Comprobantes Fiscales en Republica Dominicana — Anchi Advisors](https://www.anchiadvisors.com/post/comprobantes-fiscales-en-rep%C3%BAblica-dominicana)
- [NCF en Republica Dominicana — PortalDom](https://portaldom.do/noticias/ncf-republica-dominicana/)
- [Cambios en Comprobantes Fiscales DGII 2025 — Alegra](https://blog.alegra.com/republica-dominicana/cuales-los-cambios-comprobantes-fiscales-la-dgii-republica-dominicana/)

### Mexico
- [Registro del UUID del CFDI en el asiento contable — Estela](https://blog.estela.com/mexico/registro-del-uuid)
- [Registro del UUID del CFDI con Complemento de Pagos — SOLTUM](https://soltum.com.mx/registro-del-uuid-del-cfdi-con-complemento-de-pagos-en-el-asiento-contable/)
- [Importancia del folio fiscal UUID — FiscalCloud](https://fiscalcloud.mx/2025/08/12/la-importancia-del-folio-fiscal-uuid-en-la-validacion-de-tus-cfdi/)
- [Que es el UUID — Facturama](https://facturama.mx/blog/que-significa/uuid/)

### Chile
- [SII — Formato DTE](http://www.sii.cl/factura_electronica/formato_dte.pdf)
- [SII — Instructivo Tecnico Factura Electronica](https://www.sii.cl/factura_electronica/instructivo_emision.pdf)
- [ChileAtiende — Solicitud de folios electronicos](https://www.chileatiende.gob.cl/fichas/3217-solicitud-de-folios-electronicos-y-timbraje-de-documentos)
- [Tipos de DTE en Chile — DTEPDF](https://dtepdf.cl/blog/tipos-dte-chile-guia-completa)

### Panama
- [DGI — Factura Electronica Panama](https://dgi-fep.mef.gob.pa/)
- [DGI — Preguntas frecuentes factura electronica](https://dgi.mef.gob.pa/_7FacturaElectronica/fpreguntas)
- [Factura electronica en Panama — EDICOM](https://edicomgroup.com/blog/state-electronic-invoicing-panama)

### Espana
- [AEAT — Preguntas frecuentes SII v1.1](https://sede.agenciatributaria.gob.es/static_files/Sede/Procedimiento_ayuda/G417/FicherosSuministros/V_1_1/FaqGral/FAQs_10_02_2025.pdf)
- [AEAT — Libro registro de facturas expedidas](https://sede.agenciatributaria.gob.es/Sede/iva/facturacion-registro/preguntas-frecuentes/libro-registro-facturas-expedidas-iva-irpf.html)
- [SII Suministro Inmediato de Informacion — Wolters Kluwer](https://www.wolterskluwer.com/es-es/solutions/a3/novedades-legales/suministro-inmediato-informacion)

---

## Control de versiones

| Version | Fecha | Descripcion |
|---------|-------|-------------|
| 1.0 | 2026-03-25 | Version inicial: investigacion de 6 ERPs + 6 normativas nacionales. |
