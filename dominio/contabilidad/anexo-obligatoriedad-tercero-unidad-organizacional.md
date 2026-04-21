# Anexo — Obligatoriedad de tercero y unidad organizacional en partidas contables

> **Fecha:** Marzo 2026
> **Propósito:** Documentar el análisis y la decisión sobre cuándo el tercero y la unidad organizacional son obligatorios en las partidas de los borradores y asientos contables.
> **Versión:** 1.0

---

## 1. El problema

En la práctica contable, el tercero y la unidad organizacional no son obligatorios en todos los tipos de partida. Una cuenta de gasto necesita saber a quién se le pagó y a qué unidad de negocio se imputa, pero una cuenta de patrimonio no tiene tercero ni unidad organizacional por naturaleza. Si estas reglas no se establecen correctamente, el resultado son asientos contables con datos faltantes donde sí debían estar, o datos forzados donde no tienen sentido.

La consecuencia de no establecer estas reglas se refleja en:

- **Auxiliares incompletos:** Partidas de cartera (CxP/CxC) sin tercero — no se puede saber a quién se le debe o quién debe.
- **Distribución por unidad organizacional rota:** Partidas de gasto sin unidad organizacional — no se puede imputar el gasto a una unidad de negocio.
- **Datos forzados sin sentido:** Partidas de patrimonio o banco con tercero obligatorio — el usuario inventa un tercero genérico para cumplir la validación.

---

## 2. Cómo lo manejan los ERPs

### Tercero

Ningún ERP exige tercero en todas las partidas. El patrón universal es obligatorio solo en ciertos tipos de cuenta:

| ERP | Comportamiento | Mecanismo de configuración |
|-----|---------------|---------------------------|
| **SAP S/4HANA** | Obligatorio solo en cuentas de reconciliación (CxP/CxC). Las cuentas GL normales (gastos, activos) no lo requieren. | Field Status Group (trx. OBC4) a nivel de cuenta GL, y tipo de cuenta de reconciliación en el master del Business Partner. |
| **Oracle Fusion** | Obligatorio solo en Third Party Control Accounts. Si una cuenta se define como control account tipo Supplier o Customer, la partida debe incluir el tercero. Cuentas GL normales no lo requieren. | A nivel de cuenta GL al marcarla como Third Party Control Account. |
| **Dynamics 365** | El tercero no es una dimensión financiera estándar. Para cuentas tipo Customer, Vendor, Bank, se usa el subledger correspondiente. Para cuentas de ventas/gastos se puede exigir vía Advanced Rules. | Account Structures y Advanced Rules por rango de cuentas principales. |
| **NetSuite** | El campo Name (entity) es opcional para cuentas normales. Efectivamente obligatorio en cuentas tipo Accounts Receivable y Accounts Payable — sin entity, aparecen líneas huérfanas en reportes de aging. | No hay configuración formal de obligatoriedad; es mejor práctica operativa para AR/AP. |
| **Odoo** | Obligatorio solo en cuentas tipo Receivable y Payable. Además exige fecha de vencimiento en esas cuentas. Cuentas de gastos, activos no requieren partner. | Hardcoded por tipo de cuenta. No configurable. |

### Unidad organizacional

Los ERPs usan el término "centro de costo" (*cost center*) para un concepto similar a nuestra unidad organizacional. Ningún ERP lo exige en todas las partidas. La práctica estándar es exigirlo solo en cuentas de resultado (gastos/ingresos):

| ERP | Comportamiento | Mecanismo de configuración |
|-----|---------------|---------------------------|
| **SAP S/4HANA** | Obligatorio solo en cuentas de P&L que tengan un Cost Element asociado. No se puede hacer obligatorio en cuentas de balance. | A nivel de cuenta GL vía Field Status Group (trx. OBC4). En OKB9 se asignan centros de costo por defecto. |
| **Oracle Fusion** | El centro de costo es un segmento del Chart of Accounts. Típicamente obligatorio en la estructura de cuentas de P&L y opcional en balance. | A nivel de Chart of Accounts structure y Cross-Validation Rules. |
| **Dynamics 365** | Se configuran Account Structures separadas por rango de cuentas. Ejemplo: Balance (100000-399999) solo requiere Business Unit; P&L (400000-999999) requiere Business Unit + Department + Cost Center. | A nivel de Account Structure por rango de Main Account, por empresa/ledger. |
| **NetSuite** | Department, Class y Location son clasificaciones opcionales. Se pueden hacer obligatorias globalmente pero no se puede diferenciar por tipo de cuenta. | A nivel global (empresa) en Accounting Preferences. |
| **Odoo** | Usa Analytic Plans con configuración de Applicability: Mandatory, Optional o Unavailable. Se configura por prefijo de cuenta contable y por tipo de documento. | A nivel de Analytic Plan con filtro por prefijo de cuenta y tipo de documento. |

---

## 3. Normativa colombiana

- **NIIF (NIC 1, NIIF 15):** No prescriben el nivel de detalle interno de los asientos. No exigen tercero ni unidad organizacional en las partidas.
- **Unidad organizacional:** No hay norma colombiana que la exija. Es decisión de gestión interna.
- **Tercero:** No hay norma contable que lo exija en todas las partidas. La obligatoriedad práctica del tercero en cuentas de cartera (CxP/CxC) responde a la necesidad de identificar saldos por tercero, que es una práctica contable universal.

---

## 4. Decisión: producto preconfigurado con herencia

Se evaluaron dos filosofías:

| Filosofía | Descripción | Problema |
|-----------|-------------|----------|
| **Flexible** | El producto permite configurar todo. El cliente decide qué es obligatorio. | Los clientes configuran mal → datos inconsistentes → asientos con datos faltantes donde sí debían estar. Es el problema que se tiene hoy. |
| **Preconfigurado** | El producto viene con defaults estrictos por tipo de cuenta basados en las mejores prácticas contables. El cliente puede relajar una regla pero el default es estricto. | Menos flexibilidad, pero los datos son consistentes por defecto. |

**Decisión adoptada:** Producto preconfigurado. El sistema viene con defaults por tipo de cuenta que el cliente puede sobreescribir a nivel de cuenta auxiliar individual.

---

## 5. Defaults por tipo de cuenta

Los defaults se establecieron según las prácticas internacionales identificadas en la investigación comparativa de ERPs (sección 2) y la normativa contable (sección 3).

| Tipo de cuenta | Tercero (default) | Und. organizacional (default) | Justificación |
|---------------|:-----------------:|:----------------------------:|---------------|
| **Gasto** | Obligatorio | Obligatorio | ¿A quién se le pagó? ¿A qué unidad de negocio se imputa? |
| **Costo** | Obligatorio | Obligatorio | ¿Quién es el proveedor del costo? ¿A qué unidad de negocio se imputa? |
| **Ingreso** | Obligatorio | Obligatorio | ¿Quién pagó? ¿Qué unidad de negocio lo generó? |
| **CxP / CxC** (pasivo y activo de cartera) | Obligatorio | Obligatorio | ¿A quién le debemos / quién nos debe? ¿Qué unidad de negocio generó la obligación o el derecho de cobro? |
| **Activo** (fijo, inventario, otros) | Opcional | Opcional | Depende del activo. Un activo fijo puede necesitar saber a quién se le compró. Un inventario puede no requerirlo. |
| **Banco** | Opcional | Opcional | La cuenta de banco se identifica por sí misma. |
| **Patrimonio** | Opcional | Opcional | Capital social, reservas — no pertenecen a un tercero ni a una unidad organizacional por naturaleza. |

**Nota sobre CxP / CxC y unidad organizacional:** La práctica internacional estándar (SAP, Oracle, Dynamics) no exige unidad organizacional en cuentas de cartera — solo en cuentas de resultado (gastos/ingresos). En este producto se establece como obligatorio por defecto porque permite rastrear qué unidad de negocio generó la obligación o el derecho de cobro. Las empresas que no requieran esta granularidad pueden relajar la regla a nivel de cuenta auxiliar mediante el modelo de herencia (sección 6).

---

## 6. Modelo de herencia

La obligatoriedad se resuelve en dos niveles. El nivel más específico prevalece:

```
Nivel 1 — Tipo de cuenta (default del producto)
  Gasto → tercero: obligatorio, und. organizacional: obligatorio

Nivel 2 — Cuenta auxiliar (sobreescritura del cliente)
  Cuenta 5195-01-001 (Gastos diversos) → tercero: opcional
    (el cliente decide que para esta cuenta específica no necesita tercero)
```

**Regla de herencia:** Si la cuenta auxiliar no tiene configuración explícita, hereda del tipo de cuenta. Si tiene configuración explícita, la propia prevalece.

**Advertencia al relajar:** Cuando el cliente sobreescribe un campo obligatorio para hacerlo opcional, el sistema advierte que los reportes relacionados (auxiliar por tercero, distribución por unidad organizacional) pueden quedar incompletos.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: análisis de obligatoriedad, comparativa 5 ERPs, normativa colombiana, decisión de producto preconfigurado con herencia por tipo de cuenta. |
