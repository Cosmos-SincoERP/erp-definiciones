# Anexo — Análisis de la numeración contable

> **Fecha:** Marzo 2026
> **Propósito:** Documentar por qué la numeración de comprobantes contables se segmenta por dimensiones, cómo lo resuelven los ERPs líderes y cómo lo maneja SincoA&F. Este análisis respalda la definición del término *numeración contable* en el glosario del sub-dominio de Contabilidad.
> **Versión:** 1.0

---

## 1. ¿Por qué la numeración no es un consecutivo global?

Un consecutivo global único (1, 2, 3, ...) para todos los comprobantes contables de una organización no satisface los requisitos reales por varias razones:

1. **Trazabilidad por tipo de operación.** Los usuarios y auditores necesitan identificar rápidamente la naturaleza de un comprobante contable por su numeración. Un comprobante de egreso (CE-0001) se distingue visualmente de un comprobante de diario (CD-0001). La segmentación por tipo permite series independientes que comunican el propósito del comprobante.

2. **Requisitos legales.** En Colombia, el Decreto 2649 de 1993 (Art. 124) exige que los comprobantes de contabilidad sean "numerados consecutivamente" — la práctica estándar es que cada **clase de comprobante** (ingreso, egreso, diario, nómina) lleve su propia serie consecutiva. La DIAN no regula la numeración de comprobantes contables internos (a diferencia de las facturas), pero la costumbre legal y de auditoría exige consecutivos por clase.

3. **Operación multi-sucursal.** Cuando una empresa opera con múltiples sucursales que registran comprobantes contables simultáneamente, un consecutivo global genera contención (bloqueos por concurrencia). Segmentar por sucursal permite que cada punto de operación numere de forma independiente sin conflictos.

4. **Control fiscal por periodo.** Reiniciar la numeración por periodo (mensual o anual) facilita el cierre y la auditoría — los rangos numéricos quedan acotados y se puede verificar completitud (ej: "del CE-0001 al CE-1234 de marzo 2026").

5. **Segregación multi-empresa.** En ERPs que operan múltiples empresas/entidades legales en la misma instancia, cada empresa tiene su propio plan de cuentas y su propia contabilidad. La numeración debe ser independiente por empresa.

---

## 2. Dimensiones de segmentación

### 2.1 Dimensiones comunes en la industria

| Dimensión | ¿Por qué? | Ejemplo |
|-----------|-----------|---------|
| **Empresa / Entidad legal** | Cada empresa es una entidad contable independiente con su propio Libro Mayor. | Empresa A: CP-0001, Empresa B: CP-0001 (series independientes). |
| **Tipo de comprobante** | Permite identificar la naturaleza de la operación y cumplir con la costumbre legal de consecutivos por clase de comprobante. | CP (Cuenta por Pagar), CE (Comprobante de Egreso), CD (Comprobante de Diario), CN (Comprobante de Nómina). |
| **Periodo (año o mes)** | Facilita cierre por periodo, auditoría y reinicio de series. La granularidad (anual o mensual) varía según la política de cada empresa o tipo de comprobante. | Anual: CP-2026-0001. Mensual: CP-202603-0001. |
| **Sucursal / Punto de operación** | Evita contención en operación distribuida. Cada sucursal numera independientemente. | Sucursal 91: CP-0001, Sucursal 92: CP-0001. |

### 2.2 Dimensiones menos comunes

| Dimensión | ¿Quién la usa? | Observación |
|-----------|----------------|-------------|
| **Libro contable** | Oracle Fusion (Ledger) | Relevante en multi-normativa (NIIF + fiscal local) donde cada libro tiene su propia numeración. |
| **Fuente del comprobante** | Oracle (Journal Source), Workday (Journal Source) | Distingue si el comprobante fue generado por un sub-dominio (automático) o por el usuario (manual). |

---

## 3. Comparativa de ERPs

### SAP S/4HANA — Number Range

SAP maneja **rangos de numeración** (*Nummernkreis*) configurados por **Company Code + Document Type + Fiscal Year**.

- Un Document Type (ej: KR = factura de proveedor, SA = asiento manual) se asigna a un rango de números.
- Varios Document Types pueden compartir el mismo rango.
- Soporta numeración **interna** (automática) y **externa** (el usuario ingresa el número, ej: cuando viene de un sistema fuente).
- El rango define el inicio y fin de la serie (ej: 1000000000 a 1999999999 para el tipo KR del año 2026).
- Configuración: transacciones FBN1 (definir rangos) y OBA7 (asignar rango a tipo de comprobante).

### Oracle Fusion — Document Sequence

Oracle implementa **Document Sequences** asignables por **Ledger/Legal Entity + Journal Category + Journal Source**.

- Tres modos: **Automatic** (puede tener gaps por rollback), **Gapless** (sin gaps, con bloqueo secuencial) y **Manual** (el usuario asigna).
- Permite que diferentes categorías de diario (ej: Payables, Receivables, Manual) tengan secuencias independientes.
- La segmentación por Ledger habilita numeración independiente para multi-normativa.

### Microsoft Dynamics 365 — Number Sequence

Dynamics usa **Number Sequences** configurables con **scope** (Shared, Legal Entity, Operating Unit, Company) y asignadas al **Journal Name** (tipo de diario).

- Soporta **numeración cronológica** (requisito legal en Francia y otros países europeos): un comprobante posterior siempre tiene número mayor.
- El Voucher Number se genera según la Number Sequence del Journal Name.
- Permite prefijos y formatos configurables (ej: `GJ-####-2026`).

### NetSuite — Transaction Number + Document Number

NetSuite maneja un sistema dual:

- **Transaction Number:** consecutivo interno automático, siempre secuencial, asignado por el sistema. No configurable.
- **Document Number:** configurable con **Advanced Numbering** — secuencias por Transaction Type + Subsidiary + (opcionalmente) Fiscal Year.
- Adicionalmente, **GL Audit Numbering** genera una secuencia gapless separada para cumplir requisitos de auditoría.

### Odoo — Sequence (ir.sequence)

Odoo asigna la secuencia a nivel de **Journal** (diario contable):

- Cada diario (Ventas, Compras, Banco, Varios) tiene su propia secuencia.
- Formato típico: `INV/2026/0001`, `BILL/2026/0001`, `MISC/2026/0001`.
- La secuencia se reinicia por año fiscal.
- Desde Odoo 14+, el usuario puede editar el nombre manualmente (flexibilidad).

### Workday — Journal Number

Workday asigna un **Journal Sequence Number** automático por **Company + Ledger Type + Journal Source**.

- Distingue entre Accounting Journals (manuales) y Operational Journals (automáticos).
- Menos configurable que SAP u Oracle en cuanto a rangos personalizados.

### SincoA&F — Numeración configurable por tipo de comprobante

SincoA&F no tiene una política de numeración global. Cada tipo de comprobante (CP, CE, CD, etc.) tiene su propia **estructura de numeración configurable** que define independientemente:

- **Periodo de reinicio:** mensual o anual, según la configuración del tipo de comprobante.
- **Sucursal:** opcional — algunos tipos incluyen sucursal en la serie, otros no.

Cada tipo de comprobante define qué dimensiones participan en su consecutivo. La empresa opera en modo mono-empresa por base de datos, por lo que la dimensión de empresa está implícita en la instancia.

El consecutivo lo asigna SincoA&F al recibir el comprobante contable vía API. El sistema retorna el número asignado en la respuesta (ej: `{ "consecutivo": 2022010265 }`).

---

## 4. Matriz consolidada

| Dimensión | SAP | Oracle | D365 | NetSuite | Odoo | Workday | SincoA&F |
|-----------|-----|--------|------|----------|------|---------|----------|
| **Empresa** | Company Code | Legal Entity | Legal Entity | Subsidiary | Implícito | Company | Implícito (mono-empresa por BD) |
| **Tipo de comprobante** | Document Type | Journal Category | Journal Name | Transaction Type | Journal | Journal Source | Tipo (CP, CE, etc.) |
| **Periodo** | Año fiscal | Opcional | Opcional | Opcional (Advanced) | Año (formato) | Implícito | Configurable por tipo (mes o año) |
| **Sucursal** | No | No | Opcional (scope) | Opcional (Location) | No | No | Configurable por tipo |
| **Libro contable** | No | Ledger | No | No | No | Ledger Type | No |
| **Configurable por tipo** | Parcial (rangos compartibles) | Sí (por categoría) | Sí (por Journal Name) | Sí (Advanced Numbering) | Sí (por Journal) | Parcial | Sí (cada tipo define su estructura) |
| **Gapless (sin gaps)** | No nativo | Sí (modo Gapless) | No nativo | Sí (GL Audit Numbering) | No | No | Sí (por diseño) |

---

## 5. Referencia normativa — Colombia

**Decreto 2649 de 1993:**

- **Art. 123 (Soportes):** Deben adherirse al orden cronológico.
- **Art. 124 (Comprobantes de contabilidad):** "Las partidas asentadas en los libros de resumen y en aquel donde se asienten en orden cronológico las operaciones, deben estar respaldadas en **comprobantes de contabilidad** elaborados previamente." Los comprobantes deben ser **numerados consecutivamente**, indicando el día de preparación y las personas que los elaboraron y autorizaron.
- **Art. 125 (Libros):** Deben llevar numeración sucesiva y continua.

**Implicaciones para el diseño:**
1. La numeración debe ser **consecutiva** (sin gaps o al menos sucesiva y continua).
2. Cada **clase de comprobante** lleva su propia serie — alineado con la dimensión "tipo de comprobante".
3. Debe ser **cronológica** — un comprobante posterior no puede tener número menor que uno anterior dentro de la misma serie.
4. La DIAN no regula los consecutivos de comprobantes contables internos (sí regula facturas electrónicas).

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: análisis comparativo de 7 ERPs + normativa colombiana. |
