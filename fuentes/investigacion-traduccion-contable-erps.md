# Investigación: Relación Módulos Transaccionales ↔ Contabilidad en ERPs

> **Fecha:** 2026-03-18
> **Propósito:** Análisis comparativo de cómo los ERPs líderes resuelven la traducción de transacciones operativas a asientos contables.
> **Versión:** 1.0

---

## Tabla de contenido

1. [Resumen ejecutivo](#1-resumen-ejecutivo)
2. [SAP S/4HANA](#2-sap-s4hana)
3. [Oracle Fusion Cloud](#3-oracle-fusion-cloud)
4. [Microsoft Dynamics 365 Finance](#4-microsoft-dynamics-365-finance)
5. [NetSuite](#5-netsuite)
6. [Odoo](#6-odoo)
7. [Workday](#7-workday)
8. [Matriz comparativa](#8-matriz-comparativa)
9. [Patrones arquitectónicos identificados](#9-patrones-arquitectónicos-identificados)
10. [Conclusión y recomendación](#10-conclusión-y-recomendación)
11. [Fuentes](#11-fuentes)

---

## 1. Resumen ejecutivo

Existen **tres patrones arquitectónicos fundamentales** para resolver la relación entre módulos transaccionales y contabilidad:

| Patrón | Descripción | Quién lo usa |
|--------|-------------|--------------|
| **A. Motor de contabilidad subledger centralizado** | Capa intermedia explícita con motor de reglas que traduce eventos → asientos | Oracle Fusion (SLA) |
| **B. Determinación automática de cuentas por configuración** | El módulo transaccional tiene tablas de mapeo que determinan cuentas contables al momento de postear | SAP, Dynamics 365, NetSuite, Odoo |
| **C. Reglas de posteo basadas en categorías semánticas** | Los usuarios no eligen cuentas contables; eligen categorías de negocio y un motor de reglas traduce | Workday |

La tendencia moderna (Oracle SLA, Workday APR) apunta a **separar explícitamente** la lógica contable del módulo transaccional. SAP S/4HANA tomó un camino diferente: en lugar de separar, **fusionó** todo en una tabla única (Universal Journal / ACDOCA).

---

## 2. SAP S/4HANA

### 2.1 Arquitectura: Determinación Automática de Cuentas + Universal Journal

SAP utiliza un patrón de **determinación automática de cuentas (Automatic Account Determination)** configurado por módulo:

- **MM-FI (Compras → Contabilidad):** Configurado con la transacción `OBYC`. Usa *transaction keys* (BSX, WRX, GBB, etc.) + *valuation class* + *account modifier* para determinar automáticamente las cuentas GL al momento de un goods receipt o invoice verification.
- **SD-FI (Ventas → Contabilidad):** Configurado con la transacción `VKOA`. Combina *chart of accounts* + *sales organization* + *account key* para derivar cuentas de ingreso, descuento, costo de ventas, etc.
- **FI-AP / FI-AR:** Las cuentas de control (reconciliation accounts) se asignan directamente en el maestro de proveedor/cliente.

### 2.2 ¿El módulo transaccional conoce las cuentas contables?

**Sí, directamente.** En SAP, el módulo transaccional (MM, SD) contiene la configuración de mapeo a cuentas GL. No hay una capa intermedia separada. El posteo financiero ocurre **sincrónicamente** al momento de registrar la transacción logística.

### 2.3 Evolución en S/4HANA: Universal Journal (ACDOCA)

SAP S/4HANA introdujo un cambio radical: **eliminó los subledgers separados** y los fusionó en una tabla única llamada ACDOCA (Universal Journal):

- Reemplaza tablas separadas de GL (GLT0/FAGLFLEX), Controlling (COEP), Asset Accounting (ANEP), Material Ledger (MLIT).
- Cada línea de ACDOCA contiene **más de 500 campos** que combinan datos de FI, CO, AA, ML.
- **Elimina la necesidad de reconciliación** entre subledgers y GL porque todo vive en una sola tabla.
- Aprovecha SAP HANA para hacer agregaciones on-the-fly sin tablas de totales.

### 2.4 Quién es dueño de las reglas de mapeo

**Cada módulo es dueño de su propia configuración de determinación de cuentas.** No hay un motor centralizado. Un consultor FI-MM configura OBYC; un consultor FI-SD configura VKOA. Las reglas están dispersas.

### 2.5 Pros y contras

| Pros | Contras |
|------|---------|
| Posteo síncrono → siempre consistente | Reglas de mapeo dispersas en múltiples transacciones de configuración |
| Universal Journal elimina reconciliación | Acoplamiento fuerte entre módulo transaccional y contabilidad |
| Rendimiento excepcional con HANA | Cambiar un mapeo contable requiere tocar la config del módulo transaccional |
| Madurez de 30+ años | Complejidad de configuración inicial alta |

---

## 3. Oracle Fusion Cloud

### 3.1 Arquitectura: Subledger Accounting (SLA) — Motor centralizado

Oracle Fusion implementa el patrón más sofisticado de separación: **Subledger Accounting (SLA)** es un motor de reglas centralizado que actúa como capa intermedia entre TODOS los módulos transaccionales y el General Ledger.

```
Módulos transaccionales (AP, AR, Inventory, Projects, Assets, Cost Mgmt)
        ↓ (emiten eventos contables)
   ┌─────────────────────────┐
   │  Subledger Accounting   │
   │  (SLA / XLA Engine)     │
   │                         │
   │  - Event Model          │
   │  - Account Derivation   │
   │  - Journal Line Rules   │
   │  - Accounting Methods   │
   └────────┬────────────────┘
            ↓ (journal entries)
      General Ledger (GL)
```

### 3.2 Componentes clave del SLA

1. **Event Model:** Cada módulo transaccional emite *accounting events* tipificados por *event class* y *event type* (ej: "Receiving → Receipt", "Payables → Invoice Validation").

2. **Accounting Methods Builder (AMB):** Herramienta centralizada donde se definen:
   - **Account Derivation Rules:** Determinan qué cuenta GL usar, segmento por segmento o como Accounting Flexfield completo. Pueden ser condicionales (ej: "si el item es categoría X, usar cuenta Y").
   - **Journal Line Rules (JLT):** Definen las líneas del asiento (débito/crédito) para cada tipo de evento.
   - **Description Rules:** Generan descripciones automáticas para las líneas del journal.
   - **Subledger Journal Entry Rule Sets:** Agrupan Account Rules + Journal Line Rules + Description Rules por evento.
   - **Accounting Methods:** Agrupan todos los rule sets para definir un tratamiento contable completo.

3. **Proceso "Create Accounting":** Proceso batch (o en línea) que toma las transacciones registradas, aplica las reglas del AMB y genera asientos en tablas SLA, que luego se transfieren al GL.

### 3.3 ¿El módulo transaccional conoce las cuentas contables?

**No.** El módulo transaccional solo emite eventos con datos del negocio (proveedor, item, monto, fecha). El SLA determina las cuentas contables aplicando sus reglas. Esta es la **separación más limpia** de todos los ERPs analizados.

### 3.4 Quién es dueño de las reglas de mapeo

**SLA centraliza todas las reglas.** Un equipo contable puede modificar el tratamiento contable de cualquier módulo desde una sola herramienta (AMB) sin tocar la configuración transaccional.

### 3.5 Pros y contras

| Pros | Contras |
|------|---------|
| Separación total entre transaccional y contable | Complejidad de configuración del AMB |
| Un solo lugar para todas las reglas contables | Curva de aprendizaje pronunciada |
| Permite múltiples representaciones contables del mismo evento (multi-GAAP) | Latencia: el proceso "Create Accounting" puede ser batch |
| Reprocesar contabilidad sin retocar transacciones | Overhead de un motor de reglas intermediario |
| Modelo de eventos extensible | Mayor número de componentes a mantener |
| Auditoría centralizada del trail contable | |

---

## 4. Microsoft Dynamics 365 Finance

### 4.1 Arquitectura: Posting Profiles + Subledger Journal Entries

Dynamics 365 usa un patrón intermedio: **Posting Profiles** configurados por módulo que determinan las cuentas GL, con un concepto explícito de **subledger journal entries** como paso intermedio.

```
Source Document (factura AP, invoice AR, etc.)
        ↓
   Accounting Distributions
   (usuario asigna dimensiones financieras)
        ↓
   Subledger Journal Account Entry
   (se aplica Posting Profile → determina Main Account)
        ↓
   General Ledger (Voucher)
```

### 4.2 Posting Profiles

Cada módulo tiene su Posting Profile:
- **Vendor Posting Profile (AP):** Define la cuenta de control de AP (summary account), cuentas de descuento, cuentas de prepago. Se puede configurar por proveedor individual, grupo de proveedores o todos.
- **Customer Posting Profile (AR):** Ídem para clientes.
- **Inventory Posting:** Mapea movimientos de inventario a cuentas GL.
- **Production Groups, Fixed Asset Posting Profiles, etc.**

### 4.3 ¿El módulo transaccional conoce las cuentas contables?

**Parcialmente.** El Posting Profile está configurado dentro del módulo (AP, AR, Inventory), pero el concepto de *subledger journal entry* proporciona una capa de abstracción. El usuario en el documento fuente trabaja con *accounting distributions* (dimensiones financieras), y el sistema usa el Posting Profile para completar la cuenta principal (main account). La transferencia al GL puede hacerse en detalle o resumen.

### 4.4 Quién es dueño de las reglas de mapeo

**Cada módulo es dueño de su Posting Profile**, pero el concepto es uniforme y consistente entre módulos. No hay un motor centralizado como Oracle SLA, pero el patrón es estandarizado.

### 4.5 Pros y contras

| Pros | Contras |
|------|---------|
| Patrón consistente y bien documentado | Reglas distribuidas por módulo (no centralizadas) |
| Subledger journal entries como concepto explícito | Menos flexible que Oracle SLA para multi-GAAP |
| Configuración más simple que Oracle o SAP | Posting Profiles pueden proliferar si no se gestionan |
| Accounting distributions dan control al usuario | Cambiar reglas contables requiere tocar configuración del módulo |
| Reconciliación subledger ↔ GL incorporada | |

---

## 5. NetSuite

### 5.1 Arquitectura: GL Impact automático con mapeo implícito

NetSuite usa un enfoque más **monolítico e implícito**: las transacciones generan automáticamente su GL Impact basándose en el tipo de transacción y la configuración del item/cuenta.

### 5.2 Mecanismo de mapeo

- Cada tipo de transacción tiene un **GL Impact predeterminado** (ej: una factura de proveedor debita la cuenta del gasto/item y acredita AP automáticamente).
- Las cuentas se configuran a nivel de:
  - **Item:** Cada item tiene una cuenta de ingreso, cuenta de activo (inventario), cuenta COGS.
  - **Subsidiaria:** Cuentas de control por subsidiaria.
  - **Tipo de transacción:** Comportamiento contable predefinido.
- **SuiteGL:** Permite personalización avanzada del GL Impact a nivel de línea, tipos de transacción personalizados y clasificaciones custom.
- **System Accounts:** Cuentas de control generadas automáticamente (AP, AR, Undeposited Funds, etc.) que actúan como puente subledger ↔ GL.

### 5.3 ¿El módulo transaccional conoce las cuentas contables?

**Sí, muy directamente.** En NetSuite, el usuario frecuentemente selecciona la cuenta GL directamente en la línea de la transacción (especialmente en vendor bills). Las cuentas están embebidas en los maestros de items. No hay separación entre transaccional y contable.

### 5.4 Quién es dueño de las reglas de mapeo

**Distribuido entre maestros de items, configuración de subsidiarias y tipo de transacción.** No hay un motor centralizado ni un concepto de Posting Profile unificado.

### 5.5 Pros y contras

| Pros | Contras |
|------|---------|
| Simplicidad: lo que ves es lo que se postea | Sin separación transaccional/contable |
| GL Impact visible en cada transacción | Difícil cambiar reglas contables globalmente |
| Dimensiones flexibles (Class, Dept, Location) sin predefinir combinaciones | Usuarios expuestos directamente a cuentas GL |
| SuiteGL para personalizaciones avanzadas | Poco soporte nativo para multi-GAAP |
| Rápido de implementar | Escalabilidad limitada para organizaciones complejas |

---

## 6. Odoo

### 6.1 Arquitectura: Generación automática con cuentas embebidas

Odoo adopta el enfoque más **monolítico y directo** de todos los ERPs analizados:

- Cada módulo (compras, ventas, inventario) genera automáticamente asientos contables (journal entries) al confirmar operaciones.
- Las cuentas GL se configuran en:
  - **Cuentas por defecto del producto** (cuenta de ingreso, cuenta de gasto).
  - **Categorías de producto** (heredan cuentas).
  - **Diarios contables** (Purchase Journal, Sales Journal, etc.) con cuentas por defecto.
  - **Configuración de la compañía** (cuenta AP, cuenta AR por defecto).
- **Reglas de asientos automáticos** permiten crear entries periódicas o condicionales.

### 6.2 ¿El módulo transaccional conoce las cuentas contables?

**Sí, completamente.** En Odoo, la contabilidad está profundamente integrada en cada módulo. Cuando se confirma una factura de proveedor, Odoo crea inmediatamente el asiento contable usando las cuentas configuradas en el producto/categoría/journal.

### 6.3 Quién es dueño de las reglas de mapeo

**Distribuido entre productos, categorías de producto y journals.** No hay motor centralizado.

### 6.4 Pros y contras

| Pros | Contras |
|------|---------|
| Máxima simplicidad | Acoplamiento total transaccional ↔ contable |
| Configuración intuitiva | Sin soporte real para multi-GAAP |
| Tiempo de implementación corto | Cambios contables requieren tocar muchos maestros |
| Open source y extensible | Sin concepto de subledger formal |
| Ideal para PYMES | No escala bien para multinacionales complejas |

---

## 7. Workday

### 7.1 Arquitectura: Account Posting Rules + Worktags (categorías semánticas)

Workday implementa un enfoque **único y moderno**: los usuarios nunca interactúan con cuentas contables. En su lugar, seleccionan **categorías semánticas de negocio** (Spend Categories, Revenue Categories), y un motor de reglas (Account Posting Rules) traduce a cuentas GL.

```
Transacción operativa
   Usuario selecciona: Spend Category = "Servicios Profesionales"
                       Cost Center = "Marketing"
        ↓
   ┌──────────────────────────────┐
   │  Account Posting Rules (APR) │
   │                              │
   │  Spend Category "Serv Prof"  │
   │    → Ledger Account 6210     │
   │    → "Professional Fees"     │
   └──────────┬───────────────────┘
              ↓
   Journal Entry automático:
     DR 6210 Professional Fees / Marketing
     CR 2100 Accounts Payable
```

### 7.2 Worktags como lenguaje del negocio

- **Worktags** son etiquetas multidimensionales (Cost Center, Fund, Program, Spend Category, Revenue Category, Supplier, Project, etc.) que el usuario asigna a transacciones.
- Los worktags reemplazan el concepto tradicional de "segmentos del chart of accounts". En lugar de codificar todo en un account string (ej: 1010-200-MKT-PR01), Workday usa dimensiones independientes.
- **Account Posting Rule Sets** mapean combinaciones de worktags a ledger accounts.

### 7.3 ¿El módulo transaccional conoce las cuentas contables?

**No.** El usuario operativo nunca ve ni selecciona una cuenta contable. Solo trabaja con categorías de negocio (Spend Category = "Suministros de Oficina") y dimensiones (Cost Center, Project). Las APR hacen la traducción transparentemente.

### 7.4 Quién es dueño de las reglas de mapeo

**Centralizado en los Account Posting Rule Sets.** Controlado por el equipo de contabilidad. Los módulos transaccionales no contienen lógica contable.

### 7.5 Pros y contras

| Pros | Contras |
|------|---------|
| Separación total entre usuario operativo y contabilidad | Modelo propietario, difícil de replicar |
| Reglas centralizadas y auditables | Menor control granular vs. Oracle SLA |
| Los usuarios hablan en lenguaje de negocio, no en cuentas GL | Requiere diseño cuidadoso de Spend/Revenue Categories |
| Flexible: cambiar mapeo contable no afecta operaciones | Menos maduro que SAP/Oracle para manufactura |
| Multi-GAAP nativo vía múltiples Account Sets | Ecosistema cerrado |

---

## 8. Matriz comparativa

| Dimensión | SAP S/4HANA | Oracle Fusion | Dynamics 365 | NetSuite | Odoo | Workday |
|-----------|-------------|---------------|--------------|----------|------|---------|
| **¿Módulo transaccional conoce cuentas GL?** | Sí | No | Parcialmente | Sí | Sí | No |
| **¿Capa intermedia explícita?** | No (fusionada en ACDOCA) | Sí (SLA/XLA) | Semi (Subledger Journal) | No | No | Sí (APR) |
| **¿Motor de reglas centralizado?** | No | Sí (AMB) | No | No | No | Sí (APR Sets) |
| **Dueño de las reglas de mapeo** | Cada módulo | SLA centralizado | Cada módulo (Posting Profiles) | Items/subsidiarias | Productos/journals | APR centralizado |
| **Multi-GAAP nativo** | Sí (vía ledgers paralelos) | Sí (vía métodos contables) | Limitado | No | No | Sí (vía Account Sets) |
| **Posteo síncrono/asíncrono** | Síncrono | Batch o en línea | Síncrono | Síncrono | Síncrono | Asíncrono (behind scenes) |
| **Complejidad de config** | Alta | Muy alta | Media | Baja | Baja | Media-Alta |
| **Flexibilidad contable** | Alta | Muy alta | Media | Baja | Baja | Alta |
| **Ideal para** | Grandes empresas manufactura | Multinacionales multi-GAAP | Mid-market | PYMES/Mid-market | PYMES | Servicios/mid-large |

---

## 9. Patrones arquitectónicos identificados

### Patrón A: Subledger Accounting Engine (Oracle SLA)

```
[Módulo Transaccional] → emite Evento Contable → [Motor SLA] → aplica Reglas → [GL]
```

**Características:**
- Separación completa de concerns (transaccional vs. contable).
- Motor de reglas con: Event Model, Account Derivation Rules, Journal Line Types, Accounting Methods.
- El módulo transaccional NO conoce cuentas GL; solo emite eventos con datos de negocio.
- Permite reprocesar contabilidad sin modificar transacciones.
- Soporta múltiples tratamientos contables del mismo evento (multi-GAAP).

**Cuándo usarlo:** Cuando se requiere máxima flexibilidad contable, multi-GAAP, o cuando los módulos transaccionales son desarrollados por equipos distintos al equipo contable.

### Patrón B: Account Determination Tables (SAP, Dynamics 365)

```
[Módulo Transaccional] → consulta Config de Mapeo interna → postea directo a [GL]
```

**Características:**
- El módulo transaccional contiene tablas de configuración que mapean atributos de negocio (valuation class, vendor group, etc.) a cuentas GL.
- El posteo es síncrono: al confirmar la transacción, se crea el asiento.
- Las reglas están distribuidas por módulo.
- Cada módulo "sabe" de contabilidad.

**Cuándo usarlo:** Cuando se quiere simplicidad operativa y consistencia inmediata. Funciona bien en monolitos donde los módulos comparten el mismo codebase.

### Patrón C: Semantic Category Translation (Workday)

```
[Usuario] → selecciona Categorías de Negocio → [APR Engine] → traduce a Cuentas GL → [GL]
```

**Características:**
- El usuario nunca interactúa con cuentas GL.
- Un vocabulario de negocio (Spend Categories, Revenue Categories) actúa como **lenguaje intermedio**.
- Las reglas de traducción son centralizadas y controladas por el equipo contable.
- Limpio, pero requiere un diseño muy cuidadoso del catálogo de categorías.

**Cuándo usarlo:** Cuando se quiere que los usuarios operativos no necesiten conocimiento contable, y se desea centralizar el control de las reglas contables.

### Patrón D: Direct Account Embedding (NetSuite, Odoo)

```
[Módulo Transaccional] → usa cuentas GL embebidas en maestros → postea directo a [GL]
```

**Características:**
- Las cuentas GL están configuradas directamente en los maestros (items, productos, journals).
- El usuario a veces selecciona la cuenta GL directamente.
- Máxima simplicidad, mínima separación.
- Ideal para PYMES con estructuras contables simples.

**Cuándo usarlo:** PYMES, implementaciones rápidas, cuando la estructura contable es simple y estable.

---

## 10. Conclusión y recomendación

### Cuál es el patrón más moderno y recomendado

Para un **ERP multi-módulo con ambiciones de escalabilidad**, los patrones A (Oracle SLA) y C (Workday APR) representan el estado del arte, porque:

1. **Desacoplan** la lógica contable de la lógica transaccional — principio de Single Responsibility.
2. **Centralizan** las reglas contables — un solo lugar para auditar, modificar y extender.
3. **Permiten** que los módulos transaccionales evolucionen independientemente de la contabilidad.
4. **Soportan** multi-GAAP, multi-normativa, y cambios regulatorios sin tocar los módulos operativos.
5. **Facilitan** el testing: las reglas contables se prueban independientemente de las transacciones.

### Recomendación para el ERP Cosmos

Dado el contexto del proyecto (DDD, Event Sourcing, EDA), el **Patrón A (Subledger Accounting Engine) adaptado a eventos** es el más natural:

```
[Módulo OXP/CXC/etc.]
   → emite Domain Events (ObligacionRegistrada, PagoAplicado, etc.)
      → [Traductor Contable / Accounting Engine]
         → consume eventos
         → aplica reglas de derivación de cuentas
         → produce AsientoContableSolicitado / MovimientoContableRegistrado
            → [Módulo Contabilidad / GL]
```

**Ventajas de este enfoque en arquitectura DDD/ES/EDA:**

| Aspecto | Beneficio |
|---------|-----------|
| **Bounded Contexts** | OXP no conoce cuentas GL; Contabilidad no conoce obligaciones. El traductor es el Anti-Corruption Layer. |
| **Event-driven** | Los Domain Events ya existen; el traductor los consume. No se necesita posteo síncrono. |
| **Reglas centralizadas** | Un solo servicio/agregado contiene todas las reglas de mapeo evento → asiento. |
| **Auditoría** | Cada traducción queda registrada como evento (traceabilidad completa). |
| **Multi-normativa** | Diferentes reglas de traducción para diferentes normativas sin tocar OXP. |
| **Reprocesamiento** | Se puede "replay" la traducción contable si cambian las reglas, sin modificar las transacciones originales. |

Este patrón es esencialmente lo que Oracle SLA hace, pero expresado en el lenguaje de DDD/ES/EDA en lugar de en el lenguaje de un ERP monolítico.

---

## 11. Fuentes

### SAP
- [SAP SD FI Integration and Account Determination](https://www.saplogisticsexpert.com/sap-sd-fi-integration-and-account-determination/)
- [A Step-by-Step Guide to Automatic Account Determination in SAP MM-FI Integration](https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-members/a-step-by-step-guide-to-automatic-account-determination-in-sap-mm-fi/ba-p/14146163)
- [SAP MM Account Determination](https://erpcorp.com/sap-controlling-blog/fundamentals-of-mm-fi-account-determination)
- [All you need to know about Universal Journal (ACDOCA)](https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-members/all-you-need-to-know-about-universal-journal-acdoca-sap-s-4-hana-2020/ba-p/13545279)
- [SAP Universal Journal (ACDOCA Table) - Detailed Guide](https://skillstek.com/universal-journal-in-sap/)
- [What Is SAP's Universal Journal?](https://blog.sap-press.com/what-is-saps-universal-journal)
- [New Data Architecture in SAP FI on S/4HANA Cloud](https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-sap/new-data-architecture-in-sap-fi-on-s-4hana-cloud-what-every-functional/ba-p/14129021)

### Oracle Fusion
- [Subledger Accounting (SLA) - Oracle Documentation](https://docs.oracle.com/cd/E18727_01/doc.121/e13635/T372621T464264.htm)
- [Mastering Oracle SLA: Essential Guide](https://www.suretysystems.com/insights/mastering-oracle-sla-essential-guide-for-accurate-subledger-accounting/)
- [SLA Customization in Oracle Fusion](https://www.linkedin.com/pulse/sla-customization-oracle-fusion-amit-bhatnagar)
- [Overview of Subledger Accounting in Oracle Fusion Applications](http://lifeofanoracleprodigy.blogspot.com/2019/05/overview-of-subledger-accounting-in.html)
- [Oracle Fusion Applications Financials Implementation Guide](https://docs.oracle.com/cd/E15586_01/fusionapps.1111/e20375/F569960AN52F30.htm)

### Microsoft Dynamics 365
- [Posting profiles overview - Dynamics 365](https://learn.microsoft.com/en-us/dynamics365/finance/general-ledger/pstg-prfles-ovrvw)
- [Ledger, subledger, and subledger journal accounting entries overview](https://learn.microsoft.com/en-us/dynamics365/finance/general-ledger/ledger-subledger)
- [Recommended practices for posting profiles](https://learn.microsoft.com/en-us/dynamics365/finance/general-ledger/recommended-practices-pstg-prfles)
- [Accounts payable postings](https://learn.microsoft.com/en-us/dynamics365/finance/general-ledger/accts-payble-posting)
- [Vendor posting profiles](https://learn.microsoft.com/en-us/dynamics365/finance/accounts-payable/vendor-posting-profiles)

### NetSuite
- [Chapter 8: GL Impact in NetSuite](https://www.ikigailabs.io/netsuite-year-end-closings/gl-impact-in-netsuite)
- [NetSuite GL Impact Page - Oracle Documentation](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_N1481077.html)
- [NetSuite General Ledger Software](https://www.netsuite.com/portal/products/erp/financial-management/finance-accounting/general-ledger-software.shtml)
- [Understand NetSuite System-Generated Accounts](https://blog.prolecto.com/2021/08/28/understand-netsuite-systems-accounts/)

### Odoo
- [Accounting and Invoicing — Odoo 18.0 documentation](https://www.odoo.com/documentation/18.0/applications/finance/accounting.html)
- [Odoo Accounting Features](https://www.odoo.com/app/accounting-features)

### Workday
- [Using Journal Entries and Expenses with Ledger Accounts in Workday](https://developers.apideck.com/guides/journal-entries-expenses-ledger-accounts-workday)
- [4 Things to Know when Deploying Workday Financials](https://commitconsulting.com/blog/4-things-to-know-when-deploying-workday-financials)
- [How To Build And Maintain A Sustainable Workday Foundational Data Model](https://tcblog.protiviti.com/2022/07/20/how-to-build-and-maintain-a-sustainable-workday-foundational-data-model/)

### Patrones generales
- [Subledger Posting: A Complete Guide](https://www.hubifi.com/blog/sub-ledger-financial-reporting)
- [Core Banking ERP Part 2 – Data Model, Ledger & Core Products](https://clefincode.com/blog/global-digital-vibes/en/core-banking-erp-part-2-data-model-ledger-core-products)
- [Managing Direct Posting in Business Central](https://erpsoftwareblog.com/2025/11/managing-direct-posting-in-business-central-enabling-control-without-disruption/)
