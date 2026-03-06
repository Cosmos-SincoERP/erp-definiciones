# Investigación: Moneda única por factura — Estándar internacional

**Fecha:** 2026-03-05
**Contexto:** Validación de que una factura/OXP de comercio siempre liquida todos sus conceptos en una sola moneda, sin mezclar monedas entre líneas de un mismo documento.

---

## Conclusión

Es un estándar universal en la industria de ERPs y facturación electrónica: **una factura = una moneda de documento**. Ningún ERP relevante ni estándar de facturación electrónica permite mezclar monedas en las líneas de una misma factura.

"Multi-moneda" significa que el sistema puede manejar facturas en USD, EUR, COP, etc., pero cada factura individual opera en una sola moneda. Si un proveedor vende artículos en USD y en EUR, se generan facturas separadas por moneda.

---

## Hallazgos por sistema/estándar

### SAP S/4HANA (Global)

Un documento de factura solo puede tener una moneda. Si existen líneas en monedas distintas, SAP divide automáticamente en facturas separadas por moneda. SAP S/4HANA soporta hasta 10 monedas paralelas (documento, local, grupo y 7 adicionales), pero todas son representaciones del mismo monto convertido, no monedas diferentes por línea.

**Fuentes:**
- [One invoice document in two currencies — SAP Community](https://answers.sap.com/questions/7986777/one-invoice-document-in-two-currencies.html)
- [Billing Document Currency — SAP Community](https://community.sap.com/t5/enterprise-resource-planning-q-a/billing-document-currency/qaq-p/11033937)
- [Multiple Currency in Purchase Order — SAP Community](https://community.sap.com/t5/enterprise-resource-planning-q-a/multiple-currency-in-purchase-order/qaq-p/9382121)
- [Maintaining Multiple Currencies for Company Code in SAP S/4HANA](https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-members/maintaining-multiple-currencies-for-company-code-in-sap-s-4hana/ba-p/13524106)

---

### Microsoft Dynamics 365 Finance (Global)

La moneda se deriva del encabezado del documento o de la orden de compra asociada. No permite mezclar monedas en líneas de una misma factura. Soporta "dual currency" (moneda de transacción + moneda contable), pero ambas aplican al documento completo.

**Fuentes:**
- [Vendor invoices overview — Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/finance/accounts-payable/vendor-invoices-overview)
- [Dual currency — Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/finance/general-ledger/dual-currency)
- [Dynamics 365 F&O and Multi-Currency Management](https://community.dynamics.com/blogs/post/?postid=db46cc03-ce12-ef11-989a-6045bdbedf76)
- [Specify the cross rate — Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/finance/accounts-payable/specify-cross-rate)

---

### Oracle NetSuite (USA / Global)

Cada transacción (factura, orden de compra, etc.) tiene una sola moneda a nivel de documento. No se pueden mezclar monedas en las líneas. Al cambiar la moneda antes de guardar, NetSuite reconvierte todos los montos de las líneas a la nueva moneda.

**Fuentes:**
- [Currency on Vendor Transactions — Oracle NetSuite](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/bridgehead_N1400935.html)
- [Currency on Customer Transactions — Oracle NetSuite](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/bridgehead_N1398658.html)
- [Multiple Currencies — Oracle NetSuite](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_N1395463.html)
- [How to Set Up NetSuite Multi Currency — Tipalti](https://tipalti.com/netsuite-hub/netsuite-multi-currency/)

---

### Odoo (Europa / Global)

La moneda se define a nivel de documento completo, no por línea. Cada factura opera en una sola moneda. Las diferencias de cambio se registran automáticamente al momento del pago si la tasa de cambio varía respecto a la fecha de emisión.

**Fuentes:**
- [Multi-currency system — Odoo 19.0](https://www.odoo.com/documentation/19.0/applications/finance/accounting/get_started/multi_currency.html)
- [Manage invoices and payment in multiple currencies — Odoo 14.0](https://www.odoo.com/documentation/14.0/applications/finance/accounting/others/multicurrencies/invoices_payments.html)

---

### Peppol BIS Billing 3.0 (Unión Europea)

El estándar europeo de facturación electrónica exige explícitamente una sola moneda por factura a través del campo `DocumentCurrencyCode`. La especificación dice textualmente: *"Only one currency shall be used in the Invoice"*. Las únicas excepciones son:

1. `TaxCurrencyCode` (BT-6): moneda para reportar el IVA cuando difiere de la moneda del documento.
2. `TaxAmount` en moneda contable (BT-111): monto de IVA expresado en la moneda contable nacional.

Estas excepciones no permiten líneas en monedas distintas — solo permiten expresar el IVA total en una segunda moneda.

**Fuentes:**
- [DocumentCurrencyCode — Peppol BIS Billing 3.0](https://docs.peppol.eu/poacc/billing/3.0/syntax/ubl-invoice/cbc-DocumentCurrencyCode/)
- [UBL Invoice Syntax — Peppol BIS Billing 3.0](https://docs.peppol.eu/poacc/billing/3.0/syntax/ubl-invoice/)
- [UBL Invoice Tree — Peppol BIS Billing 3.0](https://docs.peppol.eu/poacc/billing/3.0/syntax/ubl-invoice/tree/)
- [Peppol BIS Billing 3.0 — Overview](https://docs.peppol.eu/poacc/billing/3.0/bis/)

---

### Nota Fiscal Electrónica — NF-e (Brasil)

La nota fiscal electrónica siempre se emite en BRL (reales brasileños). Es una moneda por documento fiscal. Para transacciones internacionales, la factura comercial puede estar en otra moneda, pero el documento fiscal (NF-e) debe estar en moneda local.

**Fuentes:**
- [Electronic invoicing in Brazil (NF-e, NFS-e, NFCom, CT-e) — EDICOM](https://edicomgroup.com/blog/electronic-invoicing-brazil)
- [Brazil E-invoice Requirements: NF-e & NFS-e — Storecove](https://www.storecove.com/blog/en/what-are-the-e-invoice-requirements-in-brazil/)
- [Managing Nota Fiscal Eletrônica in Brazil — Vertex](https://www.vertexinc.com/resources/resource-library/managing-ap-e-invoices-or-nota-fiscal-eletronica-brazil)

---

### Stripe Invoicing (Global — Pagos digitales)

Stripe permite crear facturas en diferentes monedas por cliente, pero cada factura individual opera en una sola moneda. No permite mezclar monedas en las líneas de una misma factura.

**Fuentes:**
- [Multi-currency customers — Stripe Documentation](https://docs.stripe.com/invoicing/multi-currency-customers)

---

## Mejores prácticas de facturación internacional

- La moneda debe indicarse claramente con código ISO y símbolo junto a cada monto, especialmente el total.
- Los términos de pago deben especificar en qué moneda se realiza el pago y qué tasa de conversión aplica.
- Vendedor y comprador deben acordar la moneda de la transacción antes de emitir la factura.

**Fuentes:**
- [Currency on Invoices: Multi-Currency Billing Guide](https://www.quickbillmaker.com/blog/currency-on-invoices)
- [Currency on the Invoice — inv24](https://www.inv24.com/en/blog/invoice_currency/)
- [International Accounts Payable and Overseas Payment Processing — MineralTree](https://www.mineraltree.com/blog/the-ins-and-outs-of-international-invoice-processing-and-cross-currency-bill-payment/)

---

## Resumen ejecutivo

| Sistema | Región | Moneda única por factura | Mezcla de monedas en líneas |
|---|---|---|---|
| SAP S/4HANA | Global | Si | No (divide en facturas separadas) |
| Dynamics 365 | Global | Si | No |
| Oracle NetSuite | USA/Global | Si | No |
| Odoo | Europa/Global | Si | No |
| Peppol BIS 3.0 | Unión Europea | Si (obligatorio por especificación) | No |
| NF-e | Brasil | Si (siempre BRL) | No |
| Stripe | Global | Si | No |

**Resultado:** 7/7 sistemas y estándares evaluados confirman moneda única por documento.

---

## Países evaluados

- **Colombia, Panamá, República Dominicana, México:** Evaluados por la consultora del proyecto — ninguno requiere multi-moneda por línea.
- **Estados Unidos:** NetSuite, Stripe — moneda única por factura.
- **Europa (UE):** Peppol BIS 3.0 lo prohíbe explícitamente en la especificación técnica.
- **Brasil:** NF-e siempre en BRL, una moneda por documento.
- **Global:** SAP, Dynamics 365, Odoo — todos aplican moneda única por documento.
