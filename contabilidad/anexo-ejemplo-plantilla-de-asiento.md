# Anexo — Plantillas de asiento y cadena de resolución

> **Fecha:** Marzo 2026
> **Propósito:** Ejemplificar cómo los sub-dominios transaccionales emiten líneas de traducción mediante `lineasParaTraduccion()` y cómo el motor de traducción de Contabilidad las transforma en asientos contables mediante la cadena de resolución. Este anexo respalda las definiciones de *plantilla de asiento*, *línea de traducción* y *cadena de resolución* del glosario del sub-dominio de Contabilidad.
> **Versión:** 1.0

---

## 1. Patrón universal de traducción contable

Todos los sub-dominios transaccionales del ERP siguen el mismo patrón para comunicar hechos económicos a Contabilidad:

### 1.1 Flujo completo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Sub-dominio emisor (OXP, CXC, Tesorería, Inventarios, etc.)              │
│                                                                             │
│  Agregado con componentes internos propios                                  │
│   ├─ Componentes de negocio (ConceptoDeGasto, ItemRecibido, etc.)          │
│   ├─ Componentes fiscales (DesgloseFiscal → Tributos)                      │
│   └─ Distribución (InstruccionDistribucion → DestinoDeNegocio)             │
│                                                                             │
│  lineasParaTraduccion()  ← comportamiento calculado del agregado            │
│   Aplana componentes × destinos en una lista de LineaTraduccion             │
│   con el valor ya distribuido.                                              │
└────────────────────────────────────┬────────────────────────────────────────┘
                                     │
                                     │  List<LineaTraduccion>
                                     │  (contrato estandarizado)
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  Contabilidad (motor de traducción)                                         │
│                                                                             │
│  1. Identifica el tipo de transacción contable                              │
│  2. Aplica la plantilla de asiento (estructura de roles — código del        │
│     producto, universal, no configurable)                                    │
│  3. Para cada rol, resuelve la cuenta auxiliar mediante la cadena:           │
│     Nivel A (regla manual) → Nivel C (aprendizaje) → Nivel B (inferencia)  │
│  4. Genera la contrapartida                                                 │
│  5. Valida balance (débitos = créditos)                                     │
│  6. Persiste el asiento contable                                            │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Contrato estandarizado: LineaTraduccion

Todos los sub-dominios emiten líneas con el mismo contrato. Contabilidad es agnóstica al origen — solo ve líneas.

| Campo | Descripción | Ejemplo (OXP) | Ejemplo (CXC futuro) |
|-------|-------------|---------------|----------------------|
| `tipoTransaccion` | Tipo de transacción contable. Define qué plantilla de asiento aplica. | `causacion_gasto` | `causacion_ingreso` |
| `tipoComponente` | Tipo del componente del hecho económico. Define el rol dentro del asiento. | `gasto`, `iva`, `retefuente` | `ingreso`, `iva_generado`, `retefuente_practicada` |
| `clasificacion` | Clasificación de negocio del componente. Clave para derivar la cuenta. | `honorarios`, `servicios` | `venta_inmueble`, `administracion` |
| `tercero` | Identificación del tercero (tipo, número, razón social). | `{ NIT, 900123456, "Auditoría SAS" }` | `{ CC, 1234567, "Juan Pérez" }` |
| `empresa` | Identificación de la empresa que produce el hecho económico. | `COSMOS-SAS` | `COSMOS-SAS` |
| `unidadOrganizacional` | Código del destino de negocio (Shared Kernel). | `VTA-001` | `PRY-042` |
| `valor` | Monto ya distribuido (componente × porcentaje). | `600.000` | `50.000.000` |
| `moneda` | Moneda de la operación. | `COP` | `COP` |
| `fecha` | Fecha del hecho económico. | `2026-03-15` | `2026-03-20` |
| `referenciaOrigen` | ID del agregado + evento que originó la línea. Referencia técnica para trazabilidad interna. | `oxp-comercio-{id}/OxpComercioCausada` | `factura-{id}/FacturaEmitida` |
| `documentoFuente` | Identificador del documento que origina el asiento. Es lo que el usuario ve en el auxiliar contable como columna de referencia. Cada consumidor envía lo que es relevante para su documento (número de factura, número de obligación, número de pago, etc.). Contabilidad no interpreta este campo — solo lo persiste y lo muestra. | `OXP-COM-5678` | `FV-001234` |
| `subDominioOrigen` | Sub-dominio que emite. | `OXP` | `CXC` |
| `referenciaHechoRelacionado` | (Opcional) Referencia al hecho económico original cuando la línea corresponde a una devolución, nota crédito u otro hecho derivado. Null para hechos originales. N1 conserva esta referencia en el borrador y la propaga al destino. Una OXP puede tener múltiples hechos relacionados (varias devoluciones), pero cada hecho relacionado referencia a un solo hecho original. | `null` | `factura-{id}/FacturaEmitida` |

**Principios del contrato:**
- Cada sub-dominio tiene sus propios componentes internos, pero todos implementan `lineasParaTraduccion()` que produce `List<LineaTraduccion>` con este contrato.
- El valor llega **ya distribuido** — Contabilidad no distribuye.
- La clasificación usa **códigos de referencia** de los catálogos del sub-dominio emisor e Impuestos — no valores fiscales específicos de un país.
- Contabilidad no necesita saber qué agregado o qué dominio generó la línea para traducirla. Solo necesita las dimensiones del contrato.
- El `documentoFuente` es el campo visible para el usuario en auxiliares y reportes. Es responsabilidad del consumidor decidir qué identificador es relevante (número de factura, número de obligación, número de pago, etc.). La plantilla de asiento puede definir si este campo es obligatorio según el tipo de transacción.

### 1.3 Plantilla de asiento — código del producto

La estructura de un asiento (qué roles tiene, qué naturaleza débito/crédito) es **conocimiento universal del producto**. No la configura ningún usuario. Cada combinación de tipo de transacción + dirección tiene su propia plantilla.

Ejemplos de plantillas universales:

| Tipo de transacción | Roles | Conocimiento universal |
|---------------------|-------|----------------------|
| Causación de gasto | GASTO (Débito), IMPUESTO (Débito), RETENCION (Crédito), CONTRAPARTIDA-CXP (Crédito) | En cualquier país, causar un gasto debita la cuenta de gasto y acredita la cuenta por pagar |
| Anticipo a proveedor | ANTICIPO (Débito), CONTRAPARTIDA (Crédito) | Universal: un anticipo debita la cuenta de anticipos |
| Nota crédito gasto | GASTO (Crédito), IMPUESTO (Crédito), RETENCION (Débito), CONTRAPARTIDA-CXP (Débito) | Inverso de la causación de gasto |
| Causación de ingreso | CXC (Débito), INGRESO (Crédito), IMPUESTO (Crédito), RETENCION (Débito) | Inverso direccional de la causación de gasto |
| Nota crédito ingreso | INGRESO (Débito), IMPUESTO (Débito), RETENCION (Crédito), CXC (Crédito) | Inverso de la causación de ingreso |
| Cargo financiero | CARGO-FINANCIERO (Débito), CONTRAPARTIDA (Crédito) | Universal: un cargo bancario es un gasto financiero |
| Diferencia en cambio (pérdida) | GASTO-FINANCIERO (Débito), CONTRAPARTIDA-CXP (Crédito) | Pérdida cambiaria debita gasto financiero |
| Diferencia en cambio (ganancia) | CONTRAPARTIDA-CXP (Débito), INGRESO-FINANCIERO (Crédito) | Ganancia cambiaria acredita ingreso financiero |

Lo que **varía por empresa** es a qué cuenta auxiliar específica va cada rol. Eso lo resuelve la cadena de resolución.

### 1.4 Cadena de resolución de cuentas

Para cada rol del asiento, el motor resuelve la cuenta auxiliar con tres niveles en orden de precedencia:

```
¿Qué cuenta auxiliar corresponde a este rol?

1. Nivel A — Regla manual (excepción)
   │  ¿El analista contable creó una regla específica para esta
   │  combinación de dimensiones?
   │  Prevalece sobre todo lo demás.
   │
   │  No encontró
   ▼
2. Nivel C — Aprendizaje (predeterminado)
   │  ¿El sistema aprendió de un asiento anterior con las mismas
   │  dimensiones? (el usuario confirmó o corrigió previamente)
   │  Aplica lo aprendido.
   │
   │  No encontró
   ▼
3. Nivel B — Inferencia (predeterminado)
   │  El sistema analiza el plan de cuentas del cliente
   │  (nombre, código, jerarquía) y sugiere la cuenta más
   │  probable.
   │  → Presenta la sugerencia al usuario
   │  → El usuario confirma o corrige
   │  → Alimenta el nivel C para la próxima vez
   │
   │  No pudo inferir
   ▼
4. Sin resolución — Error de derivación
   El sistema no pudo resolver la cuenta.
   Pide al usuario que asigne manualmente.
   → La asignación alimenta el nivel C para la próxima vez.
```

**Ciclo de retroalimentación:** Cada interacción del usuario (confirmación, corrección, asignación manual) enriquece el nivel C. Con el tiempo, el sistema requiere menos intervención. El analista contable puede promover un aprendizaje del nivel C a una regla formal del nivel A cuando quiere que sea explícita e inmutable.

---

## 2. Ejemplo 1 — Causación de una OxpComercio

Escenario: la empresa Cosmos SAS causa una obligación por honorarios de auditoría por $1.000.000 + IVA, con distribución 60% Ventas / 40% Administración.

### 2.1 Agregado y sus componentes internos

```
OxpComercio (agregado)
│
│  InformacionTercero: { NIT, 900123456, "Auditoría Global SAS" }
│  Empresa: COSMOS-SAS
│  Fecha soporte: 2026-03-15
│
├─ ConceptoDeGasto #1 (Entidad)
│    codigo: "AUD-EXT-001"
│    descripcion: "Servicios de auditoría externa"
│    valor: $1.000.000
│    clasificacionTributaria: "SERV-PROF" (ref. Impuestos)
│    conceptoPago: "HONORARIOS" (ref. Impuestos)
│    referenciaOrigen: { subDominio: "OXP", tipo: "gasto_directo" }
│    │
│    └─ DesgloseFiscal (VO)
│         ├─ Tributo { tipo: IVA, base: 1.000.000, tarifa: 19%, valor: 190.000 }
│         └─ Tributo { tipo: RETEFUENTE, base: 1.000.000, tarifa: 11%, valor: 110.000 }
│
└─ InstruccionDistribucion (VO)
     ├─ DestinoDeNegocio { unidadOrganizacional: "VTA-001", porcentaje: 60% }
     └─ DestinoDeNegocio { unidadOrganizacional: "ADM-001", porcentaje: 40% }
```

### 2.2 lineasParaTraduccion() — función del agregado

La función aplana los componentes × destinos y produce líneas con el contrato estandarizado:

| # | tipoTransaccion | tipoComponente | clasificacion | tercero | empresa | unidadOrg | valor | moneda | fecha |
|---|-----------------|---------------|---------------|---------|---------|-----------|-------|--------|-------|
| 1 | causacion_gasto | gasto | HONORARIOS | 900123456 | COSMOS-SAS | VTA-001 | 600.000 | COP | 2026-03-15 |
| 2 | causacion_gasto | gasto | HONORARIOS | 900123456 | COSMOS-SAS | ADM-001 | 400.000 | COP | 2026-03-15 |
| 3 | causacion_gasto | iva | IVA-19 | 900123456 | COSMOS-SAS | VTA-001 | 114.000 | COP | 2026-03-15 |
| 4 | causacion_gasto | iva | IVA-19 | 900123456 | COSMOS-SAS | ADM-001 | 76.000 | COP | 2026-03-15 |
| 5 | causacion_gasto | retefuente | RTFT-HON-11 | 900123456 | COSMOS-SAS | VTA-001 | 66.000 | COP | 2026-03-15 |
| 6 | causacion_gasto | retefuente | RTFT-HON-11 | 900123456 | COSMOS-SAS | ADM-001 | 44.000 | COP | 2026-03-15 |

### 2.3 Plantilla de asiento (código del producto)

El motor identifica `tipoTransaccion = causacion_gasto` y aplica la plantilla universal:

| Rol | Naturaleza | Alimentado por | Descripción |
|-----|-----------|----------------|-------------|
| GASTO | Débito | Líneas con `tipoComponente = gasto` | Cuenta de gasto según clasificación |
| IMPUESTO | Débito | Líneas con `tipoComponente = iva, inc, ...` | Cuenta de impuesto según tipo de tributo |
| RETENCION | Crédito | Líneas con `tipoComponente = retefuente, reteiva, ...` | Cuenta de retención según tipo de tributo |
| CONTRAPARTIDA | Crédito | Generada por el motor | Cuenta por pagar. Valor = suma(débitos) - suma(créditos anteriores) |

### 2.4 Cadena de resolución — cuenta por cuenta

**Línea #1 — Rol GASTO (gasto, HONORARIOS, VTA-001):**

```
Nivel A: ¿Regla manual para empresa=COSMOS-SAS + clasificacion=HONORARIOS?
  → No existe.

Nivel C: ¿Aprendizaje previo para gasto + HONORARIOS en COSMOS-SAS?
  → Sí. En febrero el usuario confirmó: HONORARIOS → cuenta 5110-05-002.
  → Resuelto: 5110-05-002 ✓
```

**Línea #3 — Rol IMPUESTO (iva, IVA-19, VTA-001):**

```
Nivel A: ¿Regla manual?
  → No existe.

Nivel C: ¿Aprendizaje previo para iva + IVA-19 en COSMOS-SAS?
  → No. Primera vez.

Nivel B: El sistema busca en el plan de cuentas de COSMOS-SAS
  cuentas cuyo nombre/código sugiera "IVA" en el grupo 2408.
  → Encuentra: 2408-01-001 "IVA descontable"
  → Sugiere al usuario → usuario confirma
  → Resuelto: 2408-01-001 ✓
  → Almacena en nivel C: {iva, IVA-19, COSMOS-SAS} → 2408-01-001
```

**Línea #5 — Rol RETENCION (retefuente, RTFT-HON-11, VTA-001):**

```
Nivel A: ¿Regla manual?
  → No existe.

Nivel C: ¿Aprendizaje previo para retefuente + RTFT-HON-11?
  → No. Primera vez.

Nivel B: El sistema busca cuentas cuyo nombre sugiera
  "retención" + "honorarios" en el grupo 2365.
  → Encuentra: 2365-05-001 "Retención en la fuente por honorarios"
  → Sugiere al usuario → usuario confirma
  → Resuelto: 2365-05-001 ✓
  → Almacena en nivel C
```

**Contrapartida — Rol CONTRAPARTIDA:**

```
Nivel C: ¿Aprendizaje previo para contrapartida de causacion_gasto
  con tercero tipo NIT nacional en COSMOS-SAS?
  → Sí. Siempre ha sido 2205-01-001 "CxP proveedores nacionales".
  → Resuelto: 2205-01-001 ✓
  Valor = 600.000 + 400.000 + 114.000 + 76.000 - 66.000 - 44.000 = 1.080.000
```

### 2.5 Asiento contable resultante

| Partida | Cuenta | Descripción | Unidad Org | Tercero | Débito | Crédito |
|---------|--------|-------------|------------|---------|--------|---------|
| 1 | 5110-05-002 | Honorarios | VTA-001 | 900123456 | 600.000 | |
| 2 | 5110-05-002 | Honorarios | ADM-001 | 900123456 | 400.000 | |
| 3 | 2408-01-001 | IVA descontable | VTA-001 | 900123456 | 114.000 | |
| 4 | 2408-01-001 | IVA descontable | ADM-001 | 900123456 | 76.000 | |
| 5 | 2365-05-001 | ReteFuente honorarios | VTA-001 | 900123456 | | 66.000 |
| 6 | 2365-05-001 | ReteFuente honorarios | ADM-001 | 900123456 | | 44.000 |
| 7 | 2205-01-001 | CxP proveedores nacionales | — | 900123456 | | 1.080.000 |
| | | | | **Totales** | **1.190.000** | **1.190.000** |

---

## 3. Ejemplo 2 — Registro de un anticipo a proveedor

Escenario: la empresa Cosmos SAS registra un anticipo de $3.000.000 al proveedor 900123456, con destino 100% a Ventas.

### 3.1 Agregado y sus componentes internos

```
Anticipo (agregado)
│
│  InformacionTercero: { NIT, 900123456, "Auditoría Global SAS" }
│  Empresa: COSMOS-SAS
│  valorAnticipo: $3.000.000
│  Fecha: 2026-03-10
│
└─ InstruccionDistribucion (VO)
     └─ DestinoDeNegocio { unidadOrganizacional: "VTA-001", porcentaje: 100% }
```

Sin desglose fiscal (P1 — el anticipo no tiene tributos).

### 3.2 lineasParaTraduccion() — función del agregado

| # | tipoTransaccion | tipoComponente | clasificacion | tercero | empresa | unidadOrg | valor | moneda | fecha |
|---|-----------------|---------------|---------------|---------|---------|-----------|-------|--------|-------|
| 1 | anticipo_proveedor | anticipo | — | 900123456 | COSMOS-SAS | VTA-001 | 3.000.000 | COP | 2026-03-10 |

Línea única. Sin clasificación de gasto. Sin distribución compleja.

### 3.3 Plantilla de asiento (código del producto)

El motor identifica `tipoTransaccion = anticipo_proveedor` y aplica:

| Rol | Naturaleza | Alimentado por | Descripción |
|-----|-----------|----------------|-------------|
| ANTICIPO | Débito | Línea con `tipoComponente = anticipo` | Cuenta de anticipos a proveedores |
| CONTRAPARTIDA | Crédito | Generada por el motor | Cuenta por pagar. Mismo valor. |

### 3.4 Cadena de resolución

**Línea #1 — Rol ANTICIPO:**

```
Nivel C: ¿Aprendizaje previo para anticipo en COSMOS-SAS?
  → Sí. Siempre ha sido 1330-05-001 "Anticipos a proveedores".
  → Resuelto: 1330-05-001 ✓
```

**Contrapartida:**

```
Nivel C: ¿Aprendizaje previo para contrapartida de anticipo_proveedor?
  → Sí. 2205-01-001 "CxP proveedores nacionales".
  → Resuelto: 2205-01-001 ✓
```

### 3.5 Asiento contable resultante

| Partida | Cuenta | Descripción | Unidad Org | Tercero | Débito | Crédito |
|---------|--------|-------------|------------|---------|--------|---------|
| 1 | 1330-05-001 | Anticipos a proveedores | VTA-001 | 900123456 | 3.000.000 | |
| 2 | 2205-01-001 | CxP proveedores nacionales | — | 900123456 | | 3.000.000 |
| | | | | **Totales** | **3.000.000** | **3.000.000** |

---

## 4. Ejemplo 3 — Causación de una devolución tipo Comercio (nota crédito)

Escenario: la empresa Cosmos SAS causa una nota crédito por devolución parcial de $300.000 + IVA sobre la OxpComercio de honorarios del Ejemplo 1. 100% a Ventas.

### 4.1 Agregado y sus componentes internos

```
Devolucion (agregado) — tipo Comercio
│
│  Ref. a OXP origen: OxpComercio-{id}
│  InformacionTercero: { NIT, 900123456, "Auditoría Global SAS" }
│  Empresa: COSMOS-SAS
│  Fecha: 2026-03-18
│
├─ ConceptoDevuelto #1 (Entidad)
│    descripcion: "Servicios de auditoría externa"
│    valor: $300.000 (positivo — magnitud del crédito, D19)
│    codigo: "AUD-EXT-001"
│    │
│    └─ DesgloseFiscal (VO) — prorrateado del gravamen original
│         ├─ Tributo { tipo: IVA, base: 300.000, tarifa: 19%, valor: 57.000 }
│         └─ Tributo { tipo: RETEFUENTE, base: 300.000, tarifa: 11%, valor: 33.000 }
│
└─ InstruccionDistribucion (VO)
     └─ DestinoDeNegocio { unidadOrganizacional: "VTA-001", porcentaje: 100% }
```

### 4.2 lineasParaTraduccion() — función del agregado

| # | tipoTransaccion | tipoComponente | clasificacion | tercero | empresa | unidadOrg | valor | moneda | fecha |
|---|-----------------|---------------|---------------|---------|---------|-----------|-------|--------|-------|
| 1 | nota_credito_gasto | concepto_devuelto | HONORARIOS | 900123456 | COSMOS-SAS | VTA-001 | 300.000 | COP | 2026-03-18 |
| 2 | nota_credito_gasto | iva | IVA-19 | 900123456 | COSMOS-SAS | VTA-001 | 57.000 | COP | 2026-03-18 |
| 3 | nota_credito_gasto | retefuente | RTFT-HON-11 | 900123456 | COSMOS-SAS | VTA-001 | 33.000 | COP | 2026-03-18 |

Los valores son positivos (D19). La plantilla de asiento invierte las naturalezas.

### 4.3 Plantilla de asiento (código del producto)

El motor identifica `tipoTransaccion = nota_credito_gasto` y aplica la plantilla inversa a la causación:

| Rol | Naturaleza | Alimentado por | Descripción |
|-----|-----------|----------------|-------------|
| GASTO | **Crédito** | Líneas con `tipoComponente = concepto_devuelto` | Inverso: acredita la cuenta de gasto |
| IMPUESTO | **Crédito** | Líneas con `tipoComponente = iva` | Inverso: acredita la cuenta de impuesto |
| RETENCION | **Débito** | Líneas con `tipoComponente = retefuente` | Inverso: debita la cuenta de retención |
| CONTRAPARTIDA | **Débito** | Generada por el motor | Reduce CxP |

### 4.4 Cadena de resolución

Las cuentas son las mismas que en la causación original — el nivel C ya las aprendió:

```
concepto_devuelto + HONORARIOS → 5110-05-002 (nivel C)
iva + IVA-19 → 2408-01-001 (nivel C)
retefuente + RTFT-HON-11 → 2365-05-001 (nivel C)
contrapartida de nota_credito_gasto → 2205-01-001 (nivel C)
```

Sin intervención del usuario. El sistema ya sabe.

### 4.5 Asiento contable resultante

| Partida | Cuenta | Descripción | Unidad Org | Tercero | Débito | Crédito |
|---------|--------|-------------|------------|---------|--------|---------|
| 1 | 2205-01-001 | CxP proveedores nacionales | — | 900123456 | 324.000 | |
| 2 | 2365-05-001 | ReteFuente honorarios | VTA-001 | 900123456 | 33.000 | |
| 3 | 5110-05-002 | Honorarios | VTA-001 | 900123456 | | 300.000 |
| 4 | 2408-01-001 | IVA descontable | VTA-001 | 900123456 | | 57.000 |
| | | | | **Totales** | **357.000** | **357.000** |

---

## 5. Inventario de plantillas por sub-dominio emisor

Las plantillas de asiento son código del producto — no las configura el usuario. Cada sub-dominio emisor tiene un conjunto finito de tipos de transacción contable que generan asientos con estructura conocida.

### 5.1 Relación entre dominios de gestión, transaccionales y Contabilidad

```
Dominios de gestión                Dominios transaccionales           Contabilidad
(originan la necesidad)            (materializan el hecho económico)  (traduce)

Compras ─────────────┐
Arrendamiento ───────┤
Servicios Públicos ──┼────────────▶ OXP ──────────────────────┐
Tarjetas Corporativas┤              (toda obligación           │
Gasto directo ───────┘               por pagar)               │
                                                               │
Facturación ─────────┐                                         │
Administración ──────┼────────────▶ CXC ───────────────────────┤
                     │              (todo derecho               │
                     └              de cobro)                  ├───▶ Contabilidad
                                                               │     (motor de
Tesorería ────────────────────────▶ Tesorería ─────────────────┤      traducción)
                                    (movimientos de             │
                                     caja y bancos)            │
                                                               │
Inventarios ──────────────────────▶ Inventarios ───────────────┤
                                    (movimientos físicos        │
                                     de mercancía)             │
                                                               │
Activos Fijos ────────────────────▶ Activos Fijos ─────────────┤
                                    (depreciación, bajas,       │
                                     revaluación)              │
                                                               │
Nómina ───────────────────────────▶ Nómina ────────────────────┤
                                    (causación de nómina,       │
                                     aportes, prestaciones)    │
                                                               │
                                   Arrendamientos (NIIF 16) ───┤
                                    (reconocimiento ROU,        │
                                     depreciación, interés)    │
                                                               │
                                   Contabilidad (GL directo) ──┘
                                    (manuales, cierre,
                                     apertura)
```

**Nota:** Los dominios de gestión (Compras, Arrendamiento, etc.) no emiten líneas de traducción. Sus hechos económicos se materializan a través de OXP o CXC. Los dominios que emiten directamente (Tesorería, Inventarios, Activos Fijos, Nómina, Arrendamientos NIIF 16) tienen hechos económicos que no son obligaciones por pagar ni derechos de cobro.

### 5.2 Plantillas por sub-dominio emisor

#### OXP — Obligaciones por Pagar

| # | Tipo de transacción | Roles (Débito / Crédito) |
|---|---------------------|--------------------------|
| 1 | Causación de obligación | Débito: Gasto/Activo, Impuesto / Crédito: Retención, CxP |
| 2 | Nota crédito de proveedor (devolución) | Débito: CxP, Retención / Crédito: Gasto/Activo, Impuesto |
| 3 | Nota débito a proveedor | Débito: Gasto/Activo / Crédito: CxP |
| 4 | Anticipo a proveedor | Débito: Anticipo a proveedores / Crédito: CxP |
| 5 | Aplicación de anticipo a factura | Débito: CxP / Crédito: Anticipo a proveedores |
| 6 | Diferencia en cambio (pérdida/ganancia) | Débito/Crédito: Gasto o Ingreso financiero / Crédito/Débito: CxP |

#### CXC — Cuentas por Cobrar

| # | Tipo de transacción | Roles (Débito / Crédito) |
|---|---------------------|--------------------------|
| 7 | Causación de ingreso (factura de venta) | Débito: CxC / Crédito: Ingreso, Impuesto generado, Débito: Retención practicada |
| 8 | Nota crédito a cliente (devolución) | Débito: Ingreso, Impuesto / Crédito: CxC, Retención |
| 9 | Nota débito a cliente (intereses, cargos) | Débito: CxC / Crédito: Ingreso |
| 10 | Anticipo de cliente recibido | Débito: CxC / Crédito: Anticipo de clientes |
| 11 | Aplicación de anticipo de cliente | Débito: Anticipo de clientes / Crédito: CxC |
| 12 | Diferencia en cambio (pérdida/ganancia) | Débito/Crédito: Gasto o Ingreso financiero / Crédito/Débito: CxC |
| 13 | Provisión de cartera de dudoso recaudo | Débito: Gasto provisión / Crédito: Provisión CxC |

#### Tesorería

| # | Tipo de transacción | Roles (Débito / Crédito) |
|---|---------------------|--------------------------|
| 14 | Pago a proveedor | Débito: CxP / Crédito: Banco |
| 15 | Cobro de cliente | Débito: Banco / Crédito: CxC |
| 16 | Transferencia entre cuentas bancarias | Débito: Banco destino / Crédito: Banco origen |
| 17 | Consignación / depósito | Débito: Banco / Crédito: Caja o Fondos en tránsito |
| 18 | Cargo bancario (comisiones, intereses) | Débito: Gasto financiero / Crédito: Banco |
| 19 | Abono bancario (rendimientos) | Débito: Banco / Crédito: Ingreso financiero |
| 20 | Conciliación bancaria (ajustes) | Débito/Crédito según naturaleza del ajuste |
| 21 | Reembolso de caja menor | Débito: Gastos varios / Crédito: Banco |

#### Inventarios

| # | Tipo de transacción | Roles (Débito / Crédito) |
|---|---------------------|--------------------------|
| 22 | Entrada de mercancía | Débito: Inventario / Crédito: Cuenta puente (GR/IR) |
| 23 | Salida por venta (CMV) | Débito: Costo de mercancía vendida / Crédito: Inventario |
| 24 | Salida por consumo interno | Débito: Gasto consumo / Crédito: Inventario |
| 25 | Transferencia entre bodegas | Débito: Inventario destino / Crédito: Inventario origen |
| 26 | Ajuste de inventario (sobrante/faltante) | Débito/Crédito: Inventario / Crédito/Débito: Ajuste de inventario |
| 27 | Variación de costo (revaluación) | Débito/Crédito: Inventario / Crédito/Débito: Variación de precio |

#### Activos Fijos

| # | Tipo de transacción | Roles (Débito / Crédito) |
|---|---------------------|--------------------------|
| 28 | Depreciación periódica | Débito: Gasto depreciación / Crédito: Depreciación acumulada |
| 29 | Baja / retiro de activo | Débito: Depreciación acumulada, Pérdida (si aplica) / Crédito: Activo fijo, Ganancia (si aplica) |
| 30 | Revaluación de activo | Débito: Activo fijo / Crédito: Superávit por valorización |
| 31 | Deterioro (impairment) | Débito: Pérdida por deterioro / Crédito: Deterioro acumulado |
| 32 | Capitalización (CIP a activo definitivo) | Débito: Activo fijo definitivo / Crédito: Activo en construcción |
| 33 | Transferencia entre activos | Débito: Activo destino / Crédito: Activo origen |

#### Nómina (emite directo a Contabilidad — no pasa por OXP)

| # | Tipo de transacción | Roles (Débito / Crédito) |
|---|---------------------|--------------------------|
| 34 | Causación de nómina | Débito: Gasto salarios, prestaciones / Crédito: Nómina por pagar, Retenciones por pagar |
| 35 | Aportes patronales (seguridad social + parafiscales) | Débito: Gasto aportes / Crédito: Aportes por pagar |
| 36 | Provisión de prestaciones sociales | Débito: Gasto provisión / Crédito: Provisión cesantías, prima, vacaciones |

#### Arrendamientos (NIIF 16)

| # | Tipo de transacción | Roles (Débito / Crédito) |
|---|---------------------|--------------------------|
| 37 | Reconocimiento inicial del derecho de uso | Débito: Activo por derecho de uso (ROU) / Crédito: Pasivo por arrendamiento |
| 38 | Depreciación del derecho de uso | Débito: Gasto depreciación ROU / Crédito: Depreciación acumulada ROU |
| 39 | Interés sobre pasivo por arrendamiento | Débito: Gasto financiero / Crédito: Pasivo por arrendamiento |

#### Contabilidad (GL directo)

| # | Tipo de transacción | Roles (Débito / Crédito) |
|---|---------------------|--------------------------|
| 40 | Asiento manual / ajuste contable | Débito/Crédito según definición del contador |
| 41 | Cierre de periodo (reclasificaciones) | Débito: Ingresos/Gastos / Crédito: Resultado del ejercicio |
| 42 | Apertura de periodo | Débito: Activos / Crédito: Pasivos + Patrimonio |

### 5.3 Resumen consolidado

| Sub-dominio emisor | Plantillas | Principales |
|---------------------|:----------:|-------------|
| **OXP** | 6 | Causación de obligación, nota crédito proveedor, nota débito proveedor, anticipo a proveedor, aplicación de anticipo, diferencia en cambio |
| **CXC** | 7 | Causación de ingreso, nota crédito/débito cliente, anticipo de cliente, aplicación de anticipo, diferencia en cambio, provisión cartera |
| **Tesorería** | 8 | Pago a proveedor, cobro de cliente, transferencia, consignación, cargo bancario, abono bancario, conciliación, reembolso caja menor |
| **Inventarios** | 6 | Entrada de mercancía, salida por venta (CMV), salida por consumo, transferencia entre bodegas, ajuste de inventario, variación de costo |
| **Activos Fijos** | 6 | Depreciación, baja/retiro, revaluación, deterioro, capitalización CIP, transferencia |
| **Nómina** | 3 | Causación de nómina, aportes patronales, provisión prestaciones |
| **Arrendamientos (NIIF 16)** | 3 | Reconocimiento inicial ROU, depreciación ROU, interés sobre pasivo |
| **Contabilidad (GL)** | 3 | Asiento manual, cierre de periodo, apertura |
| **Total estimado** | **42** | El inventario cubre las plantillas más comunes. Podría crecer 2-3 si se agrega Proyectos como sub-dominio emisor. |

---

## 6. Resumen del patrón

| Aspecto | ¿Quién lo define? | ¿Configurable? |
|---------|-------------------|----------------|
| Estructura del asiento (roles, naturalezas) | El producto (código) | No — es conocimiento contable universal |
| Contrato de `LineaTraduccion` | El producto (código) | No — es el contrato estandarizado entre sub-dominios |
| `lineasParaTraduccion()` | Cada sub-dominio emisor | No — cada agregado implementa la función según sus componentes |
| Resolución de la cuenta auxiliar por rol | La cadena A → C → B | Sí — se enriquece con cada interacción del usuario |
| Plan de cuentas | El cliente (importado) | Sí — cada empresa tiene el suyo |
| Reglas manuales (nivel A) | El analista contable | Sí — excepciones explícitas |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: patrón universal, contrato LineaTraduccion, 3 ejemplos OXP (causación, anticipo, nota crédito), cadena de resolución A → C → B, inventario de plantillas por sub-dominio emisor (8 sub-dominios, 42 plantillas). |
