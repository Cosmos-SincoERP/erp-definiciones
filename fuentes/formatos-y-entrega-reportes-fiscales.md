# Formatos y Mecanismos de Entrega — Reportes Fiscales Internacionales

Investigación realizada el 2026-03-06 como insumo para el sub-dominio de Impuestos.

---

## Formatos de archivo por jurisdicción

| Jurisdicción | Formato principal | Mecanismo de entrega | Fuente |
|-------------|-------------------|---------------------|--------|
| **DIAN — Colombia** | XML (formato final que acepta la plataforma DIAN). El prevalidador DIAN es una herramienta intermedia que acepta Excel como entrada y genera XML como salida. | Portal web DIAN + prevalidador | [DIAN - Información Exógena](https://www.dian.gov.co/impuestos/sociedades/ExogenaTributaria/Paginas/default.aspx) |
| **DGII — Rep. Dominicana** | XML (e-CF) | Transmisión automática por sistema a plataforma DGII | [DGII - Formatos de envío](https://dgii.gov.do/cicloContribuyente/obligacionesTributarias/remisionInformacion/Paginas/formatoEnvioDatos.aspx) |
| **SAT — México** | XML (CFDI 4.0 con firma digital) | PACs (Proveedores Autorizados de Certificación) + API | [SAT - Esquema de retenciones](https://wwwmat.sat.gob.mx/consultas/64451/conoce-el-esquema-de-retenciones-e-informacion-de-pagos) |
| **IRS — Estados Unidos** | XML (Modernized e-File / MeF) | e-File providers autorizados + APIs | [IRS - MeF Schemas](https://www.irs.gov/e-file-providers/modernized-e-file-mef-schemas-and-business-rules) |
| **HMRC — Reino Unido** | JSON (Making Tax Digital) | APIs RESTful con OAuth 2.0 | [HMRC - MTD Service Guide](https://developer.service.hmrc.gov.uk/guides/income-tax-mtd-end-to-end-service-guide/) |
| **UE — Estándar SAF-T** | XML (OECD SAF-T v2.0) | Portal específico por país | [SAF-T - Wikipedia](https://en.wikipedia.org/wiki/SAF-T) |

**Hallazgo clave:** XML es el estándar dominante como formato final de presentación. HMRC (UK) es la excepción con JSON vía API. En Colombia, la plataforma DIAN acepta XML; el prevalidador DIAN es una herramienta intermedia que convierte Excel → XML.

---

## Detalle por jurisdicción

### DIAN — Colombia

- **Declaraciones:** Formularios electrónicos en portal DIAN.
- **Exógena:** La plataforma DIAN acepta XML como formato final. El prevalidador DIAN es una herramienta intermedia construida sobre Excel: acepta Excel como entrada, valida los datos y genera XML como salida. El ERP puede generar Excel (para flujo con prevalidador) o XML directo (para carga directa). Resolución 000233 de 2026 introduce nuevos formatos (F-2856 activos digitales, F-2854 ingresos del exterior) y cambios en formatos 1001 y 1647.
- **Certificados de retención:** Formulario 220 (PDF). Plazo de entrega: antes del 31 de marzo del año siguiente.
- **Calendario 2026:** Grandes contribuyentes: 27 abril – 6 mayo. Personas naturales/jurídicas: 14 mayo – 12 junio.

Fuentes:
- [DIAN - Información Exógena](https://www.dian.gov.co/impuestos/sociedades/ExogenaTributaria/Paginas/default.aspx)
- [Resolución 000233 - Cambios 2026](https://crconsultorescolombia.com/cambios-en-exogena-activos-digitales-y-nuevos-obligados-dian-resolucion-000233.php)

### DGII — República Dominicana

- **Formato 606:** Compras y gastos de proveedores.
- **Formato 607:** Ventas e ingresos operacionales.
- **Formato 608:** Comprobantes cancelados (NCF).
- **Formato 609:** Pagos al exterior sin NCF.
- Si el 100% de las facturas son electrónicas (e-CF), no es obligatorio enviar formatos 607 y 608 — solo 606 y 609.
- Transmisión automática en XML al portal DGII.

Fuentes:
- [DGII - Formatos de envío](https://dgii.gov.do/cicloContribuyente/obligacionesTributarias/remisionInformacion/Paginas/formatoEnvioDatas.aspx)
- [Alegra - Reportes RD](https://blog.alegra.com/republica-dominicana/reportes-contables-606-607-608/)

### SAT — México

- **CFDI 4.0:** Formato XML obligatorio con firma digital del emisor y sello del PAC.
- **Retenciones:** Comprobante de Retención Electrónico v2.0 (desde abril 2023), documento XML separado del CFDI regular.
- **Validación:** Esquemas XSD específicos por año fiscal.
- **Retención documental:** 5 años.

Fuente:
- [SAT - Esquema de retenciones](https://wwwmat.sat.gob.mx/consultas/64451/conoce-el-esquema-de-retenciones-e-informacion-de-pagos)

### IRS — Estados Unidos

- **Formato:** XML mediante Modernized e-File (MeF). Cada línea del formulario recibe una etiqueta XML.
- **Certificados (W-2, 1042-S):** Transmisión electrónica + copia en papel al beneficiario.
- **APIs disponibles:** TIN Matching API (REST), Transcript Delivery System API (OAuth 2.0).

Fuentes:
- [IRS - MeF Schemas](https://www.irs.gov/e-file-providers/modernized-e-file-mef-schemas-and-business-rules)
- [IRS - API Documentation](https://irs.gov/tax-professionals/application-program-interface-api)

### HMRC — Reino Unido (Making Tax Digital)

- **Formato:** JSON (excepción al estándar XML).
- **Mecanismo:** APIs RESTful con OAuth 2.0. Sin portal de carga manual.
- **Requisito:** Fraud prevention headers obligatorios (requisito legal).

Fuente:
- [HMRC - MTD Service Guide](https://developer.service.hmrc.gov.uk/guides/income-tax-mtd-end-to-end-service-guide/)
- [HMRC - API Documentation](https://developer.service.hmrc.gov.uk/api-documentation/docs/api)

### UE — SAF-T (Standard Audit File for Tax)

- **Estándar OECD:** XML con estructura universal (Customer, Supplier, Invoice, TaxTable).
- **Adoptado por:** Portugal (2008), Polonia (2016), Noruega (2020), Ucrania, Dinamarca, Bulgaria, Bélgica.
- **Personalización:** Cada país define campos obligatorios sobre la base del esquema OECD v2.0.

Fuente:
- [SAF-T - Wikipedia](https://en.wikipedia.org/wiki/SAF-T)
- [OECD SAF-T Best Practices](https://reforms-investments.ec.europa.eu/document/download/b98a358e-809c-4bd5-b6b8-f35960e9a994_en?filename=22BG10_SAF-T_Best+Practices+Analysis_EN.pdf)

---

## Certificados tributarios — Formatos y entrega

| Jurisdicción | Formato | Entrega | Firma digital |
|-------------|---------|---------|---------------|
| Colombia | PDF (Formulario 220) | Empleador al beneficiario | No requerida |
| Rep. Dominicana | Generado automáticamente en plataforma DGII | Sistema DGII | Vía e-CF |
| México | XML (Retenciones v2.0) + firma digital | Electrónico vía PAC | Obligatoria |
| Estados Unidos | Papel + electrónico (W-2, 1042-S) | Correo + Transcript API | No requerida |
| Reino Unido | Digital vía portal/API | Portal HMRC + API | Vía OAuth |

---

## Cómo lo manejan los ERPs principales

### SAP (Document & Reporting Compliance)

Framework automatizado que genera reportes en formatos específicos por país a partir de datos financieros. Cubre e-invoicing (Italia, Polonia, Francia), SAF-T, exógena, VAT/GST. Entrega según país: portal, API o batch.

Fuente: [SAP DRC Cloud Edition](https://community.sap.com/t5/technology-blog-posts-by-sap/document-and-reporting-compliance-cloud-edition-a-new-generation-of-global/ba-p/14280589)

### Oracle Cloud ERP

Tax Reporting & Compliance Services (TRCS) como plataforma centralizada. Avalara y Vertex embebidos en Oracle Fusion para cálculo y filing automatizado multi-jurisdicción.

Fuentes:
- [Oracle TRCS](https://www.smarterp.com/materials/oracle-trcs-your-secret-weapon-for-tax-efficiency)
- [Avalara + Oracle Partnership](https://newsroom.avalara.com/2024-09-11-Avalara-and-Oracle-Expand-Embedded-Partnership-to-Support-Global-Tax-Compliance)

### Avalara

SaaS con API que se integra en SAP, Oracle, NetSuite. Genera múltiples formatos (CFDI, e-invoices EU, SAF-T, exógena CO). Actualización automática de legislación.

Fuente: [Avalara API Documentation](https://developer.avalara.com/documentation/)

### Vertex

Cloud + on-premise con integración profunda en SAP, Oracle, NetSuite, Salesforce. Certificate Center para gestión de exemption certificates con digital delivery.

Fuente: [Vertex Integrations](https://www.vertexinc.com/partners/oracle)
