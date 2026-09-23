# Catálogo de Plantillas de Asiento — OXP

**Sub-dominio emisor:** Obligaciones por Pagar (OXP)
**Catálogo del modelo:** `PlantillaDeAsiento` (Sección 3.7 de `modelo-dominio.md`)
**Versión:** 1.12
**Fecha de actualización:** 2026-09-22
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
| Plantillas de OXP | 6 |
| Plantillas del inventario total (8 sub-dominios) | 42 |
| `tipoTransaccion` cubiertos | `causacion_gasto`, `anticipo_a_proveedor`, `nota_credito_gasto`, `reversa_anticipo`, `amortizacion_anticipo`, `reclasificacion_partida` |

**Alcance de este archivo:** solo las 6 plantillas de **OXP** (único sub-dominio transaccional modelado a la fecha). Las restantes del inventario (CXC, Tesorería, Inventarios, Activos Fijos, Nómina, Arrendamientos, GL) se precargarán cuando esos sub-dominios se modelen.

---

## 4. Plantillas

> **Convención:** cada rol declara su naturaleza y los componentes que lo alimentan. El `grupoPucEsperado` es una lista de prefijos del código PUC (longitud variable). El `llevaDescripcionConcepto` (✅/❌) indica si la partida resultante recibe la descripción de concepto que envía el consumidor (`[D13]`/`[R48]`) — ✅ en componentes de concepto de negocio, ❌ en impuestos/retenciones (la cuenta ya es autodescriptiva). La **contrapartida** viaja como línea del consumidor (`tipoComponente = contrapartida`, con tercero y clasificación, sin valor ni unidad organizacional — el motor balancea, `[D15]`/`[R54]`). La columna **espejo** indica los componentes con `resolucionPorEspejo`: su cuenta se **copia** de la partida del rol indicado en el borrador del hecho relacionado (`referenciaHechoRelacionado` de la línea) — no pasan por la cadena ni alimentan el aprendizaje (`[D15]`/`[R53]`). Los ítems marcados ⚠️ están **por validar** (ver Sección 5).

### 4.1. `causacion_gasto`

Causación de una obligación por pagar. Emitida por `OxpComercioCausada` y `ExtractoCausado`.

| Rol | Naturaleza | `tipoComponente` | `grupoPucEsperado` | `llevaDescripcionConcepto` | Espejo (`resolucionPorEspejo`) | |
|-----|-----------|------------------|--------------------|:--------------------------:|--------------------------------|---|
| GASTO | Débito | `gasto` | `["51","52","53"]` | ✅ | — | |
| IMPUESTO | Débito | `iva` | `["2408"]` | ❌ | — | |
| IMPUESTO | Débito | `inc` | — | ❌ | — | ⚠️ |
| RETENCION | Crédito | `retefuente` | `["2365"]` | ❌ | — | |
| RETENCION | Crédito | `retefuente_exterior` | `["2365"]` | ❌ | — | ⚠️ |
| RETENCION | Crédito | `reteiva` | `["2367"]` | ❌ | — | |
| RETENCION | Crédito | `reteica` | `["2368"]` | ❌ | — | ⚠️ |
| IMPUESTO_ASUMIDO | Débito | `retencion_asumida` | `["5315"]` | ❌ | — | ⚠️ |
| IMPUESTO_AUTOLIQUIDADO | Débito | `iva_importacion_servicios` | `["2408"]` | ❌ | — | ⚠️ |
| AUTOLIQUIDADO_POR_PAGAR | Crédito | `iva_importacion_servicios_por_pagar` | `["2367","2408"]` | ❌ | — | ⚠️ |
| CARGO_FINANCIERO | Débito | `cargo_financiero` | `["5305"]` | ❌ | — | ⚠️ |
| DIFERENCIA_EN_CAMBIO_PERDIDA | Débito | `diferencia_en_cambio_perdida` | `["5305"]` | ❌ | — | ⚠️ |
| DIFERENCIA_EN_CAMBIO_GANANCIA | Crédito | `diferencia_en_cambio_ganancia` | `["4210"]` | ❌ | — | ⚠️ |
| AMORTIZACION_ANTICIPO | Crédito | `amortizacion_anticipo` | `["1330"]` | ❌ | ANTICIPO del anticipo original | ⚠️ |
| AJUSTE_TOLERANCIA_PERDIDA | Débito | `ajuste_tolerancia_perdida` | `["5305"]` | ❌ | — | ⚠️ |
| AJUSTE_TOLERANCIA_GANANCIA | Crédito | `ajuste_tolerancia_ganancia` | `["4210"]` | ❌ | — | ⚠️ |
| CRUCE_OBLIGACION | Débito | `cruce_obligacion` | `["2205","2335"]` | ❌ | CONTRAPARTIDA de la causación cruzada | |
| PARTIDA_POR_ACLARAR | Débito | `partida_por_aclarar` | `["1360","1380"]` | ❌ | — | ⚠️ |
| PARTIDA_ACLARADA | Crédito | `partida_aclarada` | `["1360","1380"]` | ❌ | PARTIDA_POR_ACLARAR del extracto con la disputa | ⚠️ |
| CONTRAPARTIDA | Crédito | `contrapartida` (valor: motor) | `["2205","2335"]` | ❌ | — | |

> **Nota — rol `CRUCE_OBLIGACION`:** lo alimenta solo `ExtractoCausado` (una línea por `Vinculacion` del extracto; `OxpComercioCausada` no emite este componente). Es un débito a la cuenta por pagar del proveedor de la compra cruzada — la reclasificación de la deuda hacia el banco/emisor (la contrapartida acredita la CxP del banco). Su unidad organizacional se rinde según la política de empresa **`[I33]`** (igual que la contrapartida): distribuida con la distribución de origen que envía OXP, consolidada en una unidad general, o sin unidad. Ver `[D29]` de OXP.

> **Nota — rol `IMPUESTO_ASUMIDO`:** lo alimenta `OxpComercioCausada` cuando el pago del documento ya salió completo (medio de pago tarjeta, `[D37]` de OXP). La línea de la retención viaja **idéntica** al caso normal (Cr, misma cuenta, tercero = proveedor — se declara y certifica igual); esta línea Db espejo (mismo valor, una por cada retención asumida) la registra como **gasto propio de la empresa**, y la contrapartida queda por el **total** del documento. Grupo tentativo `["5315"]` — ⚠️ por validar (ítem 13). **Segunda causa (v1.12, issue #143):** la **retención a beneficiarios del exterior** (`retefuente_exterior`, tributo `RETEFUENTE_EXTERIOR` de Impuestos — proveedor sin domicilio fiscal en el país, 20 % general o tarifa de convenio) nace **asumida por defecto** en OXP cualquiera que sea el medio de pago, porque el proveedor del exterior recibe el total; el documento puede desmarcarla cuando el contrato permite descontarla. Componente `retefuente_exterior` en el rol RETENCION (`["2365"]`, ítem 16); sustituye a `retefuente` para ese proveedor — nunca concurren. Asiento ilustrativo (consultoría del exterior de 100, 20 %, asumida): Db Gasto 100 · Cr Retefuente exterior 20 · Db Retención asumida 20 · Cr CxP proveedor 100. La nota crédito reversa ambas líneas por espejo (rol `IMPUESTO_ASUMIDO` agregado a `nota_credito_gasto` en v1.12 — antes la devolución de una compra con retención asumida no reversaba el Db).

> **Nota — roles `IMPUESTO_AUTOLIQUIDADO` / `AUTOLIQUIDADO_POR_PAGAR`:** los alimenta `OxpComercioCausada` cuando el desglose fiscal trae **tributos de naturaleza provision** (autoliquidados — `[D41]` de OXP): la empresa asume un impuesto que el proveedor no factura (IVA_IMPORTACION_SERVICIOS: servicios de proveedores sin domicilio fiscal en el país — el proveedor recibe el total). Cada tributo emite su **par de líneas** del mismo valor: el **reconocimiento** (Db) y la **provisión por pagar** (Cr) — se netean entre sí y la contrapartida no cambia. El tercero de ambas es el **proveedor** (la retención del 100% se declara y certifica a su nombre). Asiento ilustrativo (servicio del exterior de 100, tarifa 19%): Db Gasto 100 · Db Reconocimiento 19 · Cr Provisión por pagar 19 · Cr CxP proveedor 100. Son **roles genéricos**: un autoliquidado nuevo agrega su par de componentes con sus propios grupos del PUC — las cuentas se acotan por componente, no por rol (como RETENCION). Grupos tentativos ⚠️ por validar (ítems 14-15): el destino del reconocimiento (2408 descontable vs mayor valor del gasto) cruza con el refinamiento del IVA descontable (ítem 14).

> **Nota — roles `DIFERENCIA_EN_CAMBIO_PERDIDA` / `DIFERENCIA_EN_CAMBIO_GANANCIA` y `AJUSTE_TOLERANCIA_PERDIDA` / `AJUSTE_TOLERANCIA_GANANCIA`:** los alimenta solo `ExtractoCausado`. Ambos ajustes nacen al **cruzar la partida del extracto con la compra** en la conciliación (`[R10]`/`[R10b]` de OXP) y el **signo lo decide OXP** (`[D42]` de OXP): cada ajuste viaja en **una sola línea**, en el componente de su sentido — pérdida (Db: la TRM subió / el extracto cobra más que la compra) o ganancia (Cr: la TRM bajó / el extracto cobra menos) — nunca en ambos. La naturaleza contable **vive en el rol**, por eso cada signo tiene el suyo (molde del par de `[D41]`: dos partidas de naturaleza distinta exigen dos roles); Contabilidad no infiere el sentido del texto de la clasificación, solo resuelve la cuenta dentro del grupo del rol. El **tercero** de estas líneas es el **proveedor de la compra cruzada** — el ajuste es la diferencia sobre esa obligación — igual que en `cruce_obligacion`; el banco/emisor solo va en el cargo financiero y en la contrapartida. No hay compensación explícita: el cruce viaja a TRM de radicación y la contrapartida (CxP del banco) la balancea el motor por el total real del extracto. Asiento ilustrativo (compra de USD 100 causada a TRM 4.000; extracto a TRM 4.100): Db `cruce_obligacion` 400.000 · Db `diferencia_en_cambio_perdida` 10.000 · Cr CxP banco 410.000. Si la TRM baja a 3.900: Db `cruce_obligacion` 400.000 · Cr `diferencia_en_cambio_ganancia` 10.000 · Cr CxP banco 390.000. Grupos tentativos ⚠️ por validar (ítems 4 y 10): pérdidas en **5305 Financieros** (530525 Diferencia en cambio · 530505 Gastos bancarios) y ganancias en **4210 Financieros** (421020 Diferencia en cambio); para el aprovechamiento de la tolerancia el consultor puede preferir **4295 Diversos** (429581 Aprovechamientos).

> **Nota — roles `PARTIDA_POR_ACLARAR` / `PARTIDA_ACLARADA`:** los alimenta solo `ExtractoCausado`. `partida_por_aclarar` (Db) constituye la **cuenta transitoria de partidas por aclarar** — una **reclamación al banco/emisor** por una partida en disputa del extracto (fraude, cargo no reconocido) — para que la contrapartida (CxP del banco) refleje el **total real** que el banco cobra. `partida_aclarada` (Cr) la cancela cuando el **reverso bancario** llega en un extracto futuro (conciliación trans-mensual de OXP, una línea por reverso vinculado a una disputa). El tercero de ambas líneas es el **banco/emisor** (la reclamación es contra él — abre y cierra contra el mismo tercero). Grupo tentativo `["1360","1380"]` (1360 Reclamaciones, recomendada; 1380 Deudores varios) — ⚠️ por validar (ítem 11). Ver `[D36]` de OXP y el Ejemplo 5 del anexo.

### 4.2. `anticipo_a_proveedor`

Registro de un anticipo a proveedor. Emitida por `AnticipoCausado`. Sin desglose fiscal (`[P1]`).

| Rol | Naturaleza | `tipoComponente` | `grupoPucEsperado` | `llevaDescripcionConcepto` | Espejo (`resolucionPorEspejo`) | |
|-----|-----------|------------------|--------------------|:--------------------------:|--------------------------------|---|
| ANTICIPO | Débito | `anticipo` | `["1330"]` | ✅ | — | |
| CONTRAPARTIDA | Crédito | `contrapartida` (valor: motor) | `["2205","2335"]` | ❌ | — | |

### 4.3. `nota_credito_gasto`

Nota crédito de proveedor (devolución). Emitida por `DevolucionCausada` (tipo Comercio y tipo Extracto). Inverso de `causacion_gasto` — las naturalezas se invierten.

> **Nombre:** OXP emite hoy `nota_credito_proveedor`; el nombre canónico acordado es `nota_credito_gasto`. OXP debe alinear su mapeo (issue #10).

| Rol | Naturaleza | `tipoComponente` | `grupoPucEsperado` | `llevaDescripcionConcepto` | Espejo (`resolucionPorEspejo`) | |
|-----|-----------|------------------|--------------------|:--------------------------:|--------------------------------|---|
| GASTO | Crédito | `concepto_devuelto` | `["51","52","53"]` | ✅ | GASTO de la causación devuelta | |
| IMPUESTO | Crédito | `iva` | `["2408"]` | ❌ | IMPUESTO de la causación devuelta | ⚠️ |
| IMPUESTO | Crédito | `inc` | — | ❌ | IMPUESTO de la causación devuelta | ⚠️ |
| RETENCION | Débito | `retefuente` | `["2365"]` | ❌ | RETENCION de la causación devuelta | ⚠️ |
| RETENCION | Débito | `retefuente_exterior` | `["2365"]` | ❌ | RETENCION de la causación devuelta | ⚠️ |
| RETENCION | Débito | `reteiva` | `["2367"]` | ❌ | RETENCION de la causación devuelta | ⚠️ |
| RETENCION | Débito | `reteica` | `["2368"]` | ❌ | RETENCION de la causación devuelta | ⚠️ |
| IMPUESTO_ASUMIDO | Crédito | `retencion_asumida` | `["5315"]` | ❌ | IMPUESTO_ASUMIDO de la causación devuelta | ⚠️ |
| IMPUESTO_AUTOLIQUIDADO | Crédito | `iva_importacion_servicios` | `["2408"]` | ❌ | IMPUESTO_AUTOLIQUIDADO de la causación devuelta | ⚠️ |
| AUTOLIQUIDADO_POR_PAGAR | Débito | `iva_importacion_servicios_por_pagar` | `["2367","2408"]` | ❌ | AUTOLIQUIDADO_POR_PAGAR de la causación devuelta | ⚠️ |
| CARGO_FINANCIERO | Crédito | `cargo_financiero` | `["5305"]` | ❌ | CARGO_FINANCIERO de la causación devuelta | ⚠️ |
| CONTRAPARTIDA | Débito | `contrapartida` (valor: motor) | `["2205","2335"]` | ❌ | CONTRAPARTIDA de la causación devuelta | |

> **Toda la plantilla resuelve por espejo (`[R53]`):** la nota crédito debe reversar exactamente las mismas cuentas de la causación original — cada componente copia la cuenta del rol homólogo del borrador referenciado en `referenciaHechoRelacionado`. Si la causación original no existe en Contabilidad (ej. saldos migrados), el borrador nace pendiente.

### 4.4. `reversa_anticipo`

Reversa total de un anticipo sin cruces previos. Emitida por `DevolucionCausada` (tipo Anticipo). **Plantilla nueva** — no existía en el inventario de Contabilidad. Inverso de `anticipo_a_proveedor`.

| Rol | Naturaleza | `tipoComponente` | `grupoPucEsperado` | `llevaDescripcionConcepto` | Espejo (`resolucionPorEspejo`) | |
|-----|-----------|------------------|--------------------|:--------------------------:|--------------------------------|---|
| ANTICIPO | Crédito | `reversa_anticipo` | `["1330"]` | ✅ | ANTICIPO del anticipo original | |
| CONTRAPARTIDA | Débito | `contrapartida` (valor: motor) | `["2205","2335"]` | ❌ | CONTRAPARTIDA del anticipo original | |

### 4.5. `amortizacion_anticipo`

Amortización de un anticipo cuando el cruce con la OXP de Comercio ocurre **después** de causarla (Caso B de `[D26]` de OXP). **Plantilla nueva.** Emitida por `PagoOxpComercioViaAnticipoAplicado` cuando la OXP ya está en estado Causada. Salda la cuenta por pagar del proveedor contra la cuenta de anticipos — patrón SAP F-54 (Down Payment Clearing). Cuando el cruce es pre/durante la causación (Caso A), la amortización **no** usa esta plantilla: viaja como `tipoComponente` `amortizacion_anticipo` dentro de `causacion_gasto`.

| Rol | Naturaleza | `tipoComponente` | `grupoPucEsperado` | `llevaDescripcionConcepto` | Espejo (`resolucionPorEspejo`) | |
|-----|-----------|------------------|--------------------|:--------------------------:|--------------------------------|---|
| AMORTIZACION_ANTICIPO | Crédito | `amortizacion_anticipo` | `["1330"]` | ❌ | ANTICIPO del anticipo original | ⚠️ |
| CONTRAPARTIDA | Débito | `contrapartida` (valor: motor) | `["2205","2335"]` | ❌ | CONTRAPARTIDA de la causación de la OxpComercio cruzada | |

> Inverso de `anticipo_a_proveedor` (que hace Db Anticipos · Cr CxP). Cada línea lleva su propia `referenciaHechoRelacionado`: la amortización espeja la cuenta de anticipos del anticipo original; la contrapartida espeja la CxP donde nació la deuda de la OxpComercio que se salda (`[R53]` — garantiza el neteo por cuenta). La unidad organizacional de la contrapartida (CxP) sigue la política de empresa `[I33]`. Grupo del `amortizacion_anticipo` `porValidar` con consultor contable.

### 4.6. `reclasificacion_partida`

Traslado de una **partida por aclarar** a su destino real. Emitida por `PartidaEnDisputaReclasificada` (OXP) cuando se identifica el gasto detrás de una partida en disputa del extracto y se radica/causa la OxpComercio correspondiente (`R06b` de OXP). **Plantilla nueva.** Salda la cuenta por pagar del proveedor identificado (su compra ya fue pagada vía extracto) contra la transitoria de partidas por aclarar. **Sin rol `CONTRAPARTIDA`:** sus dos líneas viajan explícitas desde OXP; `terceroPrincipal` del hecho = banco/emisor (informativo).

| Rol | Naturaleza | `tipoComponente` | `grupoPucEsperado` | `llevaDescripcionConcepto` | Espejo (`resolucionPorEspejo`) | |
|-----|-----------|------------------|--------------------|:--------------------------:|--------------------------------|---|
| CRUCE_OBLIGACION | Débito | `cruce_obligacion` | `["2205","2335"]` | ❌ | CONTRAPARTIDA de la causación cruzada | |
| PARTIDA_ACLARADA | Crédito | `partida_aclarada` | `["1360","1380"]` | ❌ | PARTIDA_POR_ACLARAR del extracto con la disputa | ⚠️ |

> El débito lleva el tercero del **proveedor** de la nueva OxpComercio (salda su CxP); el crédito, el del **banco/emisor** (cierra la reclamación contra él). Análoga en su razón de ser al Caso B de la amortización (`[D26]` de OXP): cuando el hecho contable ocurre en un momento distinto de la causación, gana `tipoTransaccion` propio. Ver `[D36]` de OXP y el Ejemplo 5 del anexo. ⚠️ Plantilla completa por validar (ítem 12).

---

## 5. Revisión pendiente

Los siguientes puntos requieren confirmación de **consultor contable** y/o **canonización en OXP** (issue [#10](https://github.com/Cosmos-SincoERP/erp-definiciones/issues/10)) antes de considerar el catálogo definitivo:

| # | Ítem | Pregunta a resolver |
|---|------|---------------------|
| 1 | `inc` (rol IMPUESTO) | ¿El INC se reconoce como impuesto descontable (con grupo propio) o como mayor valor del gasto? Definir grupo o eliminarlo como componente. |
| 2 | `reteica` (rol RETENCION) | ¿OXP emite `reteica` como `tipoComponente`? Confirmar y validar grupo `2368`. |
| 3 | `cargo_financiero` | Nombre de `tipoComponente` aún descrito en prosa en OXP (entidad `CargoFinanciero`). Canonizar en OXP. Validar grupo `5305`. |
| 4 | `diferencia_en_cambio_perdida` / `diferencia_en_cambio_ganancia` | Componentes desdoblados por signo (el sentido lo decide OXP al conciliar, `[D42]` de OXP). Validar grupos: pérdida → `5305` (gastos financieros, 530525 Diferencia en cambio), ganancia → `4210` (ingresos financieros, 421020 Diferencia en cambio). El grupo anterior `4215` era Dividendos y participaciones — corregido. Confirmar cuenta/subcuenta con consultor contable. |
| 5 | Componentes devueltos (`nota_credito_gasto`) | Los impuestos y retenciones devueltos reutilizan los nombres de la causación (`iva`, `inc`, `retefuente`, `reteiva`, `reteica`) y `cargo_financiero` para la nota crédito de extracto. Confirmar los grupos del PUC con consultor contable (igual que sus equivalentes en `causacion_gasto`). |
| 6 | Nombre `nota_credito_gasto` vs `nota_credito_proveedor` | OXP debe alinear su mapeo al nombre canónico `nota_credito_gasto`. |
| 7 | `reversa_anticipo` (plantilla completa) | Plantilla definida desde cero a partir del efecto contable descrito en OXP (Db Anticipos / Cr CxP). Validar estructura y registrar formalmente en el inventario. |
| 8 | Amortización de anticipo | OXP indica que viaja como `tipoComponente` dentro de `causacion_gasto` (`[D26]`) sin nombre canónico. Definir si requiere un rol/componente propio en esta plantilla. |
| 9 | `amortizacion_anticipo` (grupo PUC) | Rol nuevo agregado por coordinación cruzada con OXP #10. Grupo `["1330"]` (Anticipos a proveedores, por `[D26]` de OXP). Confirmar con consultor. |
| 10 | `ajuste_tolerancia_perdida` / `ajuste_tolerancia_ganancia` (grupo PUC) | Componentes desdoblados por signo (extracto mayor → pérdida; extracto menor → ganancia; el sentido lo decide OXP al conciliar, `[R10]`/`[D42]` de OXP). Grupos tentativos: pérdida → `5305` (530505 Gastos bancarios), ganancia → `4210` (ingresos financieros); alternativa para la ganancia: `4295` Diversos (429581 Aprovechamientos), que es el nombre que usa la regla de OXP. Validar con consultor contable. |
| 11 | Cuenta transitoria de partidas por aclarar (`PARTIDA_POR_ACLARAR`/`PARTIDA_ACLARADA`) | Grupo tentativo `["1360","1380"]` — recomendada **1360 Reclamaciones** (la disputa es materialmente una reclamación al banco/emisor; subcuenta sugerida `136095`), alternativa 1380 Deudores varios. Definir cuenta/subcuenta con consultor contable (issue #90). |
| 12 | `reclasificacion_partida` (plantilla completa) | Plantilla definida desde el ciclo contable de la partida en disputa (`[D36]` de OXP): Db CxP proveedor · Cr transitoria, **sin** contrapartida del motor. Validar estructura y los asientos de los tres momentos (Ejemplo 5 del anexo) con consultor contable (issue #90). |
| 13 | `retencion_asumida` (rol `IMPUESTO_ASUMIDO`) | Retención asumida por pago con tarjeta (`[D37]` de OXP, issue #94; mecánica validada con consultoría fiscal jul-2026). Confirmar grupo `["5315"]` y su deducibilidad/tratamiento en renta con consultor contable. |
| 14 | `iva_importacion_servicios` (rol `IMPUESTO_AUTOLIQUIDADO`) | Reconocimiento del autoliquidado (`[D41]` de OXP, issue #128). Grupo tentativo `["2408"]` (descontable) — pero el destino real depende del derecho al descuento (responsable de IVA / naturaleza del gasto): **cruza con el ítem 1 y el issue #98** (IVA descontable vs mayor valor del gasto). Definir con consultor contable. |
| 15 | `iva_importacion_servicios_por_pagar` (rol `AUTOLIQUIDADO_POR_PAGAR`) | Provisión por pagar del autoliquidado. Grupo tentativo `["2367","2408"]` — la práctica del 437-2 num. 3 lo declara como reteIVA (2367); confirmar cuenta/subcuenta y su presentación en la declaración con consultor contable. |
| 16 | `retefuente_exterior` (rol `RETENCION`) | Retención a beneficiarios del exterior (`RETEFUENTE_EXTERIOR` de Impuestos, `[D17]`; issue #143). Grupo tentativo `["2365"]` — confirmar con consultor contable la subcuenta para retenciones a no residentes y, cuando la empresa la asume, si el gasto de la retención asumida (5315) es deducible y si la base debe reajustarse como mayor valor del pago (pendiente `[PD13]` de Impuestos). |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Junio 2026 | Versión inicial. 4 plantillas de OXP (`causacion_gasto`, `anticipo_a_proveedor`, `nota_credito_gasto`, `reversa_anticipo`) con roles, componentes y `grupoPucEsperado`. Derivado del issue #7 (grupo del PUC en la plantilla). 8 ítems en revisión pendiente para consultor contable y sincronización con OXP (issue #10). |
| 1.1 | Junio 2026 | Atributo `llevaDescripcionConcepto` por componente (issue #8). Marca qué componentes portan la descripción de concepto que envía el consumidor: ✅ en `gasto`, `anticipo`, `concepto_devuelto`, `reversa_anticipo`; ❌ en impuestos y retenciones. Nueva columna en las 4 plantillas. Alinea con `modelo-dominio.md` v1.4 [D13] y `definicion-alcance.md` v1.5 [R48]. |
| 1.2 | Junio 2026 | Atributo del rol renombrado de `nombre` a `rol` (issue #9), consistente con la herencia del rol a la partida del borrador y su propagación a la entrega. El `rol` es un código de conjunto cerrado (GASTO/IMPUESTO/RETENCION/CONTRAPARTIDA). Alinea con `modelo-dominio.md` v1.5 [D14] y `definicion-alcance.md` v1.6 [R49]. |
| 1.3 | Junio 2026 | Dos roles nuevos en `causacion_gasto` por **ajuste cruzado con OXP** (issue #10): `AMORTIZACION_ANTICIPO` (`amortizacion_anticipo` → `["1330"]`) y `AJUSTE_TOLERANCIA` (`ajuste_tolerancia` → tentativo `["5305","4210"]`). OXP los emite como `tipoComponente`; se agregan al catálogo para preservar la coincidencia 1:1. Ambos `porValidar` (ítems 9 y 10 de revisión pendiente). |
| 1.4 | Junio 2026 | Rol nuevo `CRUCE_OBLIGACION` (Débito, `cruce_obligacion` → `["2205","2335"]`) en `causacion_gasto` por **ajuste cruzado con OXP** (issue #18). Lo alimenta solo `ExtractoCausado` (una línea por `Vinculacion`): salda la cuenta por pagar del proveedor de la compra cruzada, reclasificando la deuda hacia el banco/emisor. Su unidad organizacional se rinde según `[I33]` (igual que la contrapartida). Alinea con `modelo-dominio.md` de OXP v3.5 [D29]. |
| 1.6 | Junio 2026 | Nueva plantilla `amortizacion_anticipo` por ajuste cruzado con OXP (issue #25). La emite `PagoOxpComercioViaAnticipoAplicado` cuando la OXP de Comercio ya está Causada (cruce post-causación, Caso B de `[D26]` de OXP): Db CxP proveedor · Cr Anticipos a proveedores (espejo de `anticipo_a_proveedor`, patrón SAP F-54). Cuando el cruce es pre/durante la causación, la amortización sigue viajando como `tipoComponente` dentro de `causacion_gasto` (Caso A). Cobertura OXP: 4 → 5 plantillas. Grupo del `amortizacion_anticipo` `porValidar`. |
| 1.5 | Junio 2026 | Plantilla `nota_credito_gasto` completada con los componentes que OXP emite y faltaban (issue #20). Se agregan: `inc` al rol IMPUESTO; `reteiva` y `reteica` al rol RETENCION; y un rol nuevo `CARGO_FINANCIERO` (Crédito, `cargo_financiero` → `["5305"]`) — inverso del de `causacion_gasto`, lo emite la nota crédito de un extracto (`CargoFinancieroDevuelto`). Antes la plantilla era un inverso simplificado (solo `concepto_devuelto`/`iva`/`retefuente`) y habría rechazado líneas válidas (`LINEA_SIN_ROL_EN_PLANTILLA`, I27). Los grupos del PUC quedan `porValidar`, igual que sus equivalentes en `causacion_gasto`. No requiere cambios en OXP — su catálogo ya declara estos componentes para `nota_credito_gasto`. |
| 1.7 | Julio 2026 | **Ciclo contable de la partida en disputa por ajuste cruzado con OXP (issue #90).** Dos roles nuevos en `causacion_gasto`: **`PARTIDA_POR_ACLARAR`** (Db, `partida_por_aclarar` → tentativo `["1360","1380"]`) constituye la transitoria de partidas por aclarar —reclamación al banco/emisor por una partida en disputa— para que la contrapartida refleje el total real del extracto; **`PARTIDA_ACLARADA`** (Cr, `partida_aclarada` → mismo grupo) la cancela cuando el reverso bancario llega en un extracto futuro. **Nueva plantilla `reclasificacion_partida`** (§4.6): traslado de la partida aclarada a su destino real cuando se identifica el gasto (Db `CRUCE_OBLIGACION` CxP del proveedor · Cr `PARTIDA_ACLARADA`, sin contrapartida del motor) — análoga en su razón de ser al Caso B de `[D26]`. Nombres generales de contabilidad (sin "disputa": ese es el motivo en OXP; "por aclarar" es el concepto contable). Terceros: la transitoria siempre lleva el banco/emisor; las CxP, su proveedor. Cobertura OXP: 5 → **6 plantillas**. Ítems 11 y 12 nuevos en revisión pendiente (cuenta 1360/1380 y validación de la plantilla). Ver `[D37]` de OXP y Ejemplo 5 del anexo. |
| 1.9 | Julio 2026 | **Clasificación semántica, contrapartida como componente y resolución por espejo (issue #104).** La contrapartida deja de "generarla el motor sin `tipoComponente`": viaja como línea del consumidor con `tipoComponente = contrapartida` (tercero y clasificación del consumidor; valor y unidad organizacional del motor, `[D15]`/`[R54]`) — su `grupoPucEsperado` pasa del nivel de rol al `ComponenteDelRol`, como cualquier componente. Nuevo atributo **`resolucionPorEspejo`** por componente (`[R53]`): la cuenta se copia de la partida del rol indicado en el borrador del hecho relacionado — marcados: `cruce_obligacion` → CONTRAPARTIDA de la causación cruzada (en `causacion_gasto` y `reclasificacion_partida`), `partida_aclarada` → PARTIDA_POR_ACLARAR del extracto con la disputa, `amortizacion_anticipo` → ANTICIPO del anticipo original (componente del Caso A y plantilla del Caso B), `reversa_anticipo` → ANTICIPO, y **toda** `nota_credito_gasto` (cada componente espeja el rol homólogo de la causación devuelta, contrapartida incluida); las contrapartidas de `reversa_anticipo` y `amortizacion_anticipo` espejan la CONTRAPARTIDA/CxP del hecho que saldan. Columna "Espejo" nueva en las 6 tablas. Alinea con el modelo v1.12 (`[D15]`), el alcance v1.11 (`[R52]`-`[R54]`), el anexo v1.5 y el modelo de OXP v4.8 (recetas de composición de la clasificación). |
| 1.8 | Julio 2026 | **Rol `IMPUESTO_ASUMIDO` en `causacion_gasto` por ajuste cruzado con OXP (issue #94).** Retención asumida cuando el pago del documento ya salió completo (medio de pago tarjeta, `[D38]` de OXP): la línea de la retención viaja idéntica al caso normal (Cr, misma cuenta, tercero proveedor, certificable) y la línea Db espejo `retencion_asumida` (→ tentativo `["5315"]`) la asume como gasto propio — la contrapartida queda por el total. Aplica a todas las retenciones sustractivas. Doctrina de concurrencia (conceptos DIAN 2023-2025) validada con consultoría fiscal: el motor de Impuestos no cambia. Ítem 13 nuevo en revisión pendiente. |
| 1.10 | Agosto 2026 | **Tributos de provisión en `causacion_gasto` — roles `IMPUESTO_AUTOLIQUIDADO` y `AUTOLIQUIDADO_POR_PAGAR` (issue #128, `[D41]` de OXP; espejo de la naturaleza `provision` de Impuestos v2.0.8).** El autoliquidado `IVA_IMPORTACION_SERVICIOS` entraba al desglose fiscal de la compra y ningún rol lo recibía → rechazo `LINEA_SIN_ROL_EN_PLANTILLA` (I27). Cada tributo de naturaleza provision emite desde OXP un **par de líneas** del mismo valor — reconocimiento (Db, `iva_importacion_servicios` → tentativo `["2408"]`) y provisión por pagar (Cr, `iva_importacion_servicios_por_pagar` → tentativo `["2367","2408"]`) — que se netean entre sí; la contrapartida no cambia; tercero de ambas = proveedor. **Roles genéricos** (un autoliquidado nuevo agrega su par de componentes con sus grupos — cuentas por componente, no por rol, como RETENCION). En `nota_credito_gasto` el par se declara con naturalezas invertidas y **resolución por espejo** del rol homólogo (patrón de toda la plantilla). Ítems 14-15 nuevos en revisión pendiente; el destino del reconocimiento (descontable vs mayor gasto) cruza con el ítem 1 y el issue #98. Ver `[D41]` de OXP (modelo v4.13). |
| 1.11 | Septiembre 2026 | **Diferencia en cambio y ajuste por tolerancia desdoblados por signo (issue #132, planteado por el equipo de desarrollo).** Los roles `DIFERENCIA_EN_CAMBIO` y `AJUSTE_TOLERANCIA` declaraban `naturaleza: debito_credito` — valor que no existe en `RolPartida` (conjunto cerrado `debito | credito`, Sección 3.7 del modelo) — y un `grupoPucEsperado` mixto de gasto e ingreso que dejaba el Nivel B sin acotar; el motor no podía materializar la partida ni decidir su naturaleza, y el sentido solo viajaba embebido en el texto de la clasificación. Cada concepto se desdobla en **dos roles y dos componentes** con naturaleza fija — `DIFERENCIA_EN_CAMBIO_PERDIDA` (Db, `diferencia_en_cambio_perdida` → `["5305"]`) / `DIFERENCIA_EN_CAMBIO_GANANCIA` (Cr, `diferencia_en_cambio_ganancia` → `["4210"]`) y `AJUSTE_TOLERANCIA_PERDIDA` (Db, `ajuste_tolerancia_perdida` → `["5305"]`) / `AJUSTE_TOLERANCIA_GANANCIA` (Cr, `ajuste_tolerancia_ganancia` → `["4210"]`) — molde del par de `[D41]`: la naturaleza vive en el rol. **El signo lo decide OXP** al conciliar con el dato que la entidad ya tiene (`clasificacion` / `direccion`) y emite una sola línea por ajuste; el tercero de la línea es el **proveedor de la compra cruzada**, no el banco/emisor (nueva `[D42]` de OXP). Corrección incluida: la ganancia de diferencia en cambio pasa de `4215` (Dividendos y participaciones) a `4210` Financieros (421020). `nota_credito_gasto` no lleva estos componentes y no se amplía; el modelo de Contabilidad no cambia. Ítems 4 y 10 de revisión pendiente reescritos (siguen `porValidar`; alternativa 4295 Aprovechamientos para la ganancia de tolerancia). Roles de `causacion_gasto`: 17 → **19**. Alinea con el modelo de OXP v4.15 y el alcance de OXP v1.22. |
| 1.12 | Septiembre 2026 | **Retención a beneficiarios del exterior en `causacion_gasto` y `nota_credito_gasto` (issue #143; `[D17]` de Impuestos v2.1.0, `[D37]` ampliada de OXP v4.16).** Componente **`retefuente_exterior`** en el rol RETENCION (Cr `["2365"]`, porValidar — ítem 16) y su inverso por espejo en la nota crédito. **Corrección de hueco previo:** `nota_credito_gasto` gana el rol **`IMPUESTO_ASUMIDO`** (Crédito, `retencion_asumida`, espejo) — la devolución de una compra con retención asumida no reversaba el Db 5315. `nota_credito_gasto`: 7 → **8** roles (+2 filas: `retefuente_exterior` en RETENCION y `retencion_asumida` en IMPUESTO_ASUMIDO); `causacion_gasto`: sin rol nuevo, 19 → **20** filas. Nota del rol `IMPUESTO_ASUMIDO` ampliada con la segunda causa de asunción (retención del exterior asumida por defecto, decidida por OXP). |
