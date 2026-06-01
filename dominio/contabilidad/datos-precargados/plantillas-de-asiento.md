# Catálogo de Plantillas de Asiento — OXP

**Sub-dominio emisor:** Obligaciones por Pagar (OXP)
**Catálogo del modelo:** `PlantillaDeAsiento` (Sección 3.7 de `modelo-dominio.md`)
**Versión:** 1.0
**Fecha de actualización:** 2026-06-01
**Archivo de datos:** [`plantillas-de-asiento.json`](plantillas-de-asiento.json)

---

## 1. Propósito

Este catálogo precarga las **plantillas de asiento** del producto correspondientes a los hechos económicos que emite el sub-dominio **OXP**. Cada plantilla define la estructura universal de roles (débito/crédito) de un `tipoTransaccion`, qué `tipoComponente` alimenta cada rol y el `grupoPucEsperado` que acota la inferencia de cuentas (Nivel B de la cadena de resolución, ver `[D12]` / `[R47]`).

Las plantillas de asiento son **contenido del producto** (`origen: estándar`) — no las configura el usuario (premisa P6). El detalle conceptual con ejemplos está en [`../anexo-ejemplo-plantilla-de-asiento.md`](../anexo-ejemplo-plantilla-de-asiento.md); este catálogo es la especificación completa por plantilla, lista para precarga.

---

## 2. Fuente

- **Mapeo canónico de eventos OXP → `tipoTransaccion`:** `dominio/obligaciones-por-pagar/modelo-dominio.md`, sección "Integración con sub-dominio Contabilidad".
- **Estructura del agregado:** `dominio/contabilidad/modelo-dominio.md`, Sección 3.7 (`PlantillaDeAsiento`, `RolPartida`, `ComponenteDelRol`) y decisión `[D12]`.
- **Códigos PUC:** Decreto 2650 de 1993 (Colombia). El `grupoPucEsperado` orienta la inferencia; la cuenta exacta la resuelve la cadena A/C/B.

---

## 3. Cobertura

| Concepto | Cantidad |
|---|---|
| Plantillas de OXP | 4 |
| Plantillas del inventario total (8 sub-dominios) | 42 |
| `tipoTransaccion` cubiertos | `causacion_gasto`, `anticipo_a_proveedor`, `nota_credito_gasto`, `reversa_anticipo` |

**Alcance de este archivo:** solo las 4 plantillas de **OXP** (único sub-dominio transaccional modelado a la fecha). Las 38 restantes del inventario (CXC, Tesorería, Inventarios, Activos Fijos, Nómina, Arrendamientos, GL) se precargarán cuando esos sub-dominios se modelen.

---

## 4. Plantillas

> **Convención:** cada rol declara su naturaleza y los componentes que lo alimentan. El `grupoPucEsperado` es una lista de prefijos del código PUC (longitud variable). La **contrapartida** la genera el motor (sin `tipoComponente`) y declara su grupo a nivel de rol. Los ítems marcados ⚠️ están **por validar** (ver Sección 5).

### 4.1. `causacion_gasto`

Causación de una obligación por pagar. Emitida por `OxpComercioCausada` y `ExtractoCausado`.

| Rol | Naturaleza | `tipoComponente` | `grupoPucEsperado` | |
|-----|-----------|------------------|--------------------|---|
| GASTO | Débito | `gasto` | `["51","52","53"]` | |
| IMPUESTO | Débito | `iva` | `["2408"]` | |
| IMPUESTO | Débito | `inc` | — | ⚠️ |
| RETENCION | Crédito | `retefuente` | `["2365"]` | |
| RETENCION | Crédito | `reteiva` | `["2367"]` | |
| RETENCION | Crédito | `reteica` | `["2368"]` | ⚠️ |
| CARGO_FINANCIERO | Débito | `cargo_financiero` | `["5305"]` | ⚠️ |
| DIFERENCIA_EN_CAMBIO | Débito/Crédito | `diferencia_en_cambio` | `["5305","4215"]` | ⚠️ |
| CONTRAPARTIDA | Crédito | — (genera el motor) | `["2205","2335"]` | |

### 4.2. `anticipo_a_proveedor`

Registro de un anticipo a proveedor. Emitida por `AnticipoCausado`. Sin desglose fiscal (`[P1]`).

| Rol | Naturaleza | `tipoComponente` | `grupoPucEsperado` | |
|-----|-----------|------------------|--------------------|---|
| ANTICIPO | Débito | `anticipo` | `["1330"]` | |
| CONTRAPARTIDA | Crédito | — (genera el motor) | `["2205","2335"]` | |

### 4.3. `nota_credito_gasto`

Nota crédito de proveedor (devolución). Emitida por `DevolucionCausada` (tipo Comercio y tipo Extracto). Inverso de `causacion_gasto` — las naturalezas se invierten.

> **Nombre:** OXP emite hoy `nota_credito_proveedor`; el nombre canónico acordado es `nota_credito_gasto`. OXP debe alinear su mapeo (issue #10).

| Rol | Naturaleza | `tipoComponente` | `grupoPucEsperado` | |
|-----|-----------|------------------|--------------------|---|
| GASTO | Crédito | `concepto_devuelto` | `["51","52","53"]` | |
| IMPUESTO | Crédito | `iva` | `["2408"]` | ⚠️ |
| RETENCION | Débito | `retefuente` | `["2365"]` | ⚠️ |
| CONTRAPARTIDA | Débito | — (genera el motor) | `["2205","2335"]` | |

### 4.4. `reversa_anticipo`

Reversa total de un anticipo sin cruces previos. Emitida por `DevolucionCausada` (tipo Anticipo). **Plantilla nueva** — no existía en el inventario de Contabilidad. Inverso de `anticipo_a_proveedor`.

| Rol | Naturaleza | `tipoComponente` | `grupoPucEsperado` | |
|-----|-----------|------------------|--------------------|---|
| ANTICIPO | Crédito | `reversa_anticipo` | `["1330"]` | |
| CONTRAPARTIDA | Débito | — (genera el motor) | `["2205","2335"]` | |

---

## 5. Revisión pendiente

Los siguientes puntos requieren confirmación de **consultor contable** y/o **canonización en OXP** (issue [#10](https://github.com/Cosmos-SincoERP/erp-definiciones/issues/10)) antes de considerar el catálogo definitivo:

| # | Ítem | Pregunta a resolver |
|---|------|---------------------|
| 1 | `inc` (rol IMPUESTO) | ¿El INC se reconoce como impuesto descontable (con grupo propio) o como mayor valor del gasto? Definir grupo o eliminarlo como componente. |
| 2 | `reteica` (rol RETENCION) | ¿OXP emite `reteica` como `tipoComponente`? Confirmar y validar grupo `2368`. |
| 3 | `cargo_financiero` | Nombre de `tipoComponente` aún descrito en prosa en OXP (entidad `CargoFinanciero`). Canonizar en OXP. Validar grupo `5305`. |
| 4 | `diferencia_en_cambio` | Nombre sin canonizar en OXP. Validar grupos: pérdida → `5305` (gasto financiero), ganancia → `4215` (ingreso financiero). |
| 5 | Componentes devueltos (`nota_credito_gasto`) | El impuesto y la retención devueltos no tienen nombre canónico en OXP — se asumen `iva` y `retefuente`. Canonizar en OXP. |
| 6 | Nombre `nota_credito_gasto` vs `nota_credito_proveedor` | OXP debe alinear su mapeo al nombre canónico `nota_credito_gasto`. |
| 7 | `reversa_anticipo` (plantilla completa) | Plantilla definida desde cero a partir del efecto contable descrito en OXP (Db Anticipos / Cr CxP). Validar estructura y registrar formalmente en el inventario. |
| 8 | Amortización de anticipo | OXP indica que viaja como `tipoComponente` dentro de `causacion_gasto` (`[D26]`) sin nombre canónico. Definir si requiere un rol/componente propio en esta plantilla. |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Junio 2026 | Versión inicial. 4 plantillas de OXP (`causacion_gasto`, `anticipo_a_proveedor`, `nota_credito_gasto`, `reversa_anticipo`) con roles, componentes y `grupoPucEsperado`. Derivado del issue #7 (grupo del PUC en la plantilla). 8 ítems en revisión pendiente para consultor contable y sincronización con OXP (issue #10). |
