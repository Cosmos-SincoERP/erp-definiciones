# Anexo — Plantillas de asiento y cadena de resolución

> **Fecha:** Julio 2026
> **Propósito:** Ejemplificar cómo los sub-dominios transaccionales emiten líneas de traducción mediante `lineasParaTraduccion()` y cómo el motor de traducción de Contabilidad las transforma en asientos contables mediante la cadena de resolución. Este anexo respalda las definiciones de *plantilla de asiento*, *línea de traducción* y *cadena de resolución* del glosario del sub-dominio de Contabilidad.
> **Versión:** 1.5

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
| `clasificacion` | Texto semántico de emparejamiento, compuesto mecánicamente por el consumidor según el tipo de componente a partir de datos de sus catálogos. Obligatorio en toda línea [R52]. Insumo de la resolución de cuenta en las tres capas (similitud en A/C, comparación contra descripciones del PUC en B). | `"Servicios de auditoría externa · honorarios · servicios profesionales"` | `"venta apartamento proyecto Alameda · vivienda nueva"` |
| `tercero` | Identificación del tercero (tipo, número, razón social). | `{ NIT, 900123456, "Auditoría SAS" }` | `{ CC, 1234567, "Juan Pérez" }` |
| `empresa` | Identificación de la empresa que produce el hecho económico. | `COSMOS-SAS` | `COSMOS-SAS` |
| `unidadOrganizacional` | Código del destino de negocio (Shared Kernel). | `VTA-001` | `PRY-042` |
| `valor` | Monto ya distribuido (componente × porcentaje). | `600.000` | `50.000.000` |
| `moneda` | Moneda de la operación. | `COP` | `COP` |
| `fecha` | Fecha del hecho económico. | `2026-03-15` | `2026-03-20` |
| `referenciaOrigen` | ID del agregado + evento que originó la línea. Referencia técnica para trazabilidad interna. | `oxp-comercio-{id}/OxpComercioCausada` | `factura-{id}/FacturaEmitida` |
| `documentoFuente` | Identificador del documento que origina el asiento. Es lo que el usuario ve en el auxiliar contable como columna de referencia. Cada consumidor envía lo que es relevante para su documento (número de factura, número de obligación, número de pago, etc.). Contabilidad no interpreta este campo — solo lo persiste y lo muestra. | `OXP-COM-5678` | `FV-001234` |
| `subDominioOrigen` | Sub-dominio que emite. | `OXP` | `CXC` |
| `referenciaHechoRelacionado` | (Opcional) Referencia al hecho económico original cuando la línea corresponde a una devolución, nota crédito u otro hecho derivado. Null para hechos originales. N1 conserva esta referencia en el borrador y la propaga al destino. Una OXP puede tener múltiples hechos relacionados (varias devoluciones), pero cada hecho relacionado referencia a un solo hecho original. **Además habilita la resolución por espejo [R53]:** cuando el componente está marcado con `resolucionPorEspejo` en la plantilla (cruces, aclaraciones, amortizaciones/reversas de anticipo, nota crédito), el motor copia la cuenta de la partida del rol espejado en el borrador de este hecho relacionado. | `null` | `factura-{id}/FacturaEmitida` |

**Principios del contrato:**
- Cada sub-dominio tiene sus propios componentes internos, pero todos implementan `lineasParaTraduccion()` que produce `List<LineaTraduccion>` con este contrato.
- El valor llega **ya distribuido** — Contabilidad no distribuye.
- La clasificación es **texto semántico compuesto mecánicamente por el emisor** a partir de datos de sus catálogos y de Impuestos (ej. OXP para el gasto: descripción del concepto + concepto de pago + clasificación tributaria) — no la digita un usuario, no es un código ni una llave, y es **obligatoria en toda línea** [R52]. Las recetas de composición por componente viven en el modelo de cada emisor.
- La **contrapartida también viaja como línea** (`tipoComponente = contrapartida`), con su tercero y su clasificación (ej. medio de pago + observación general) pero **sin valor ni unidad organizacional**: el valor lo calcula el motor para balancear y la unidad se rinde según la preferencia de la empresa [R54] [I33]. Así Contabilidad no conoce los campos internos de cada consumidor. El `terceroPrincipal` del hecho económico se conserva como informativo.
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

> **Grupo del PUC esperado (`grupoPucEsperado`):** Cada componente que alimenta un rol declara los grupos del PUC (prefijos de código de cuenta, de longitud variable) a los que debe pertenecer su cuenta. Esto **acota la inferencia (Nivel B)** a las cuentas cuyo código inicia por alguno de esos prefijos — formaliza lo que en los ejemplos de abajo se describe a mano ("busca cuentas… en el grupo 2408"). El grupo vive en el componente porque un rol agrupa varios `tipoComponente` que caen en grupos distintos (RETENCION: `retefuente`→`2365`, `reteiva`→`2367`); desde [D15] la contrapartida también lo declara en su componente (`contrapartida`). No reemplaza la cadena de resolución — solo orienta el Nivel B. Ver `modelo-dominio.md` [D12] y `definicion-alcance.md` [R47]. Los grupos mostrados en este anexo son ilustrativos para los ejemplos de OXP; el grupo del `inc` y el llenado del inventario completo (Sección 5) quedan pendientes de revisión por consultor contable.

### 1.4 Cadena de resolución de cuentas

Para cada rol del asiento, el motor resuelve la cuenta auxiliar. Los componentes que representan la contraparte de un hecho anterior se resuelven **por espejo** [R53]; los demás, por la cadena de tres niveles en orden de precedencia. En los tres niveles el insumo es el mismo: la **clasificación** de la línea [R52].

```
¿Qué cuenta auxiliar corresponde a este rol?

0. Espejo del hecho relacionado (solo componentes marcados)
   │  ¿La plantilla declara resolucionPorEspejo para este
   │  componente? (cruces, aclaraciones, amortizaciones y
   │  reversas de anticipo, nota crédito)
   │  → Copia la cuenta de la partida del rol espejado en el
   │     borrador del hecho relacionado (referenciaHechoRelacionado).
   │  → Si el hecho relacionado no está (ej. saldos migrados):
   │     borrador PENDIENTE — nunca se adivina por similitud.
   │
   │  No es componente de espejo
   ▼
1. Nivel A — Regla manual (excepción)
   │  Dentro de la partición exacta (tipoTransaccion, tipoComponente,
   │  empresa), ¿alguna regla del analista contable tiene un texto
   │  ancla suficientemente similar a la clasificación de la línea?
   │  Prevalece sobre todo lo demás.
   │
   │  No encontró
   ▼
2. Nivel C — Aprendizaje (predeterminado)
   │  Dentro de la misma partición, ¿alguna resolución aprendida
   │  tiene texto igual o suficientemente similar? (el usuario
   │  confirmó o corrigió previamente)
   │  Aplica la de mayor similitud. El texto idéntico (compra
   │  repetida) siempre resuelve.
   │
   │  No encontró
   ▼
3. Nivel B — Inferencia (predeterminado)
   │  El sistema compara la clasificación de la línea contra el
   │  plan de cuentas del cliente (nombre, código, jerarquía),
   │  acotado al grupo del PUC esperado, y sugiere la cuenta
   │  más probable.
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

**Ciclo de retroalimentación:** Cada interacción del usuario (confirmación, corrección, asignación manual) enriquece el nivel C. Con el tiempo, el sistema requiere menos intervención. El analista contable puede promover un aprendizaje del nivel C a una regla formal del nivel A cuando quiere que sea explícita e inmutable — el texto aprendido se copia como texto ancla de la regla. Las partidas resueltas por espejo no alimentan el aprendizaje: su cuenta es determinística, no aprendida.

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
| 1 | causacion_gasto | gasto | "Servicios de auditoría externa · honorarios · servicios profesionales" | 900123456 | COSMOS-SAS | VTA-001 | 600.000 | COP | 2026-03-15 |
| 2 | causacion_gasto | gasto | "Servicios de auditoría externa · honorarios · servicios profesionales" | 900123456 | COSMOS-SAS | ADM-001 | 400.000 | COP | 2026-03-15 |
| 3 | causacion_gasto | iva | "honorarios · iva 19%" | 900123456 | COSMOS-SAS | VTA-001 | 114.000 | COP | 2026-03-15 |
| 4 | causacion_gasto | iva | "honorarios · iva 19%" | 900123456 | COSMOS-SAS | ADM-001 | 76.000 | COP | 2026-03-15 |
| 5 | causacion_gasto | retefuente | "honorarios · retención en la fuente 11%" | 900123456 | COSMOS-SAS | VTA-001 | 66.000 | COP | 2026-03-15 |
| 6 | causacion_gasto | retefuente | "honorarios · retención en la fuente 11%" | 900123456 | COSMOS-SAS | ADM-001 | 44.000 | COP | 2026-03-15 |
| 7 | causacion_gasto | contrapartida | "crédito con el proveedor · honorarios de auditoría externa" | 900123456 | COSMOS-SAS | — | — (motor) | COP | 2026-03-15 |

La clasificación de cada línea la compone OXP según su receta por componente (`gasto`: descripción del concepto + concepto de pago + clasificación tributaria; tributos: concepto de pago + nombre y % del tributo; `contrapartida`: medio de pago + observación general). La línea 7 viaja **sin valor ni unidad organizacional** — el motor balancea y aplica la preferencia de la empresa [R54].

### 2.3 Plantilla de asiento (código del producto)

El motor identifica `tipoTransaccion = causacion_gasto` y aplica la plantilla universal:

| Rol | Naturaleza | Alimentado por | Grupo PUC esperado | Descripción |
|-----|-----------|----------------|--------------------|-------------|
| GASTO | Débito | Líneas con `tipoComponente = gasto` | `["51","52","53"]` | Cuenta de gasto según clasificación |
| IMPUESTO | Débito | Líneas con `tipoComponente = iva, inc, ...` | `iva → ["2408"]` · `inc → [...]` (a validar) | Cuenta de impuesto según tipo de tributo |
| RETENCION | Crédito | Líneas con `tipoComponente = retefuente, reteiva, ...` | `retefuente → ["2365"]` · `reteiva → ["2367"]` | Cuenta de retención según tipo de tributo |
| CONTRAPARTIDA | Crédito | Línea con `tipoComponente = contrapartida` (valor: motor) | `["2205","2335"]` | Cuenta por pagar. Valor = suma(débitos) - suma(créditos anteriores) |

### 2.4 Cadena de resolución — cuenta por cuenta

**Línea #1 — Rol GASTO (gasto, "Servicios de auditoría externa · honorarios · servicios profesionales", VTA-001):**

```
Partición: (causacion_gasto, gasto, COSMOS-SAS)

Nivel A: ¿Alguna regla manual de la partición con texto ancla
  suficientemente similar a la clasificación de la línea?
  → No existe.

Nivel C: ¿Alguna resolución aprendida de la partición con texto
  igual o suficientemente similar?
  → Sí. En febrero el usuario resolvió una línea con clasificación
    "Honorarios revisoría fiscal · honorarios · servicios
    profesionales" → cuenta 5110-05-002. Similitud alta (supera
    el umbral); el otro aprendizaje de la partición ("Aseo y
    cafetería · servicios · servicios generales" → 5195-10-001)
    queda lejos.
  → Resuelto: 5110-05-002 ✓
```

**Línea #3 — Rol IMPUESTO (iva, "honorarios · iva 19%", VTA-001):**

```
Partición: (causacion_gasto, iva, COSMOS-SAS)

Nivel A: ¿Regla manual con texto similar?
  → No existe.

Nivel C: ¿Resolución aprendida con texto similar?
  → No. Primera vez.

Nivel B: El sistema compara "honorarios · iva 19%" contra las
  cuentas del plan de COSMOS-SAS, acotado al grupo 2408.
  → Encuentra: 2408-01-001 "IVA descontable"
  → Sugiere al usuario → usuario confirma
  → Resuelto: 2408-01-001 ✓
  → Almacena en nivel C: (causacion_gasto, iva, COSMOS-SAS) +
    "honorarios · iva 19%" → 2408-01-001
```

**Línea #5 — Rol RETENCION (retefuente, "honorarios · retención en la fuente 11%", VTA-001):**

```
Partición: (causacion_gasto, retefuente, COSMOS-SAS)

Nivel A: ¿Regla manual con texto similar?
  → No existe.

Nivel C: ¿Resolución aprendida con texto similar?
  → No. Primera vez.

Nivel B: El sistema compara "honorarios · retención en la
  fuente 11%" contra el plan, acotado al grupo 2365.
  → Encuentra: 2365-05-001 "Retención en la fuente por honorarios"
  → Sugiere al usuario → usuario confirma
  → Resuelto: 2365-05-001 ✓
  → Almacena en nivel C
```

**Línea #7 — Rol CONTRAPARTIDA (contrapartida, "crédito con el proveedor · honorarios de auditoría externa"):**

```
Partición: (causacion_gasto, contrapartida, COSMOS-SAS)

Nivel C: ¿Resolución aprendida con texto similar?
  → Sí. Las compras a crédito con proveedor han resuelto siempre
    a 2205-01-001 "CxP proveedores nacionales".
  → Resuelto: 2205-01-001 ✓
  → El tercero de la partida es el de la línea (900123456);
    la unidad organizacional se rinde según [I33].
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
| 1 | anticipo_proveedor | anticipo | "anticipo para servicios de auditoría" | 900123456 | COSMOS-SAS | VTA-001 | 3.000.000 | COP | 2026-03-10 |
| 2 | anticipo_proveedor | contrapartida | "crédito con el proveedor · anticipo para servicios de auditoría" | 900123456 | COSMOS-SAS | — | — (motor) | COP | 2026-03-10 |

Una línea de negocio (la clasificación del anticipo es su descripción) más la línea de contrapartida [R54]. Sin desglose fiscal ni distribución compleja.

### 3.3 Plantilla de asiento (código del producto)

El motor identifica `tipoTransaccion = anticipo_proveedor` y aplica:

| Rol | Naturaleza | Alimentado por | Grupo PUC esperado | Descripción |
|-----|-----------|----------------|--------------------|-------------|
| ANTICIPO | Débito | Línea con `tipoComponente = anticipo` | `anticipo → ["1330"]` | Cuenta de anticipos a proveedores |
| CONTRAPARTIDA | Crédito | Línea con `tipoComponente = contrapartida` (valor: motor) | `["2205","2335"]` | Cuenta por pagar. Mismo valor. |

### 3.4 Cadena de resolución

**Línea #1 — Rol ANTICIPO:**

```
Partición: (anticipo_proveedor, anticipo, COSMOS-SAS)

Nivel C: ¿Resolución aprendida con texto similar a
  "anticipo para servicios de auditoría"?
  → Sí. Los anticipos han resuelto siempre a
    1330-05-001 "Anticipos a proveedores".
  → Resuelto: 1330-05-001 ✓
```

**Línea #2 — Rol CONTRAPARTIDA:**

```
Partición: (anticipo_proveedor, contrapartida, COSMOS-SAS)

Nivel C: ¿Resolución aprendida con texto similar?
  → Sí. 2205-01-001 "CxP proveedores nacionales".
  → Resuelto: 2205-01-001 ✓ (tercero: el de la línea;
    valor: lo calcula el motor)
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
| 1 | nota_credito_gasto | concepto_devuelto | "Servicios de auditoría externa · honorarios · servicios profesionales" | 900123456 | COSMOS-SAS | VTA-001 | 300.000 | COP | 2026-03-18 |
| 2 | nota_credito_gasto | iva | "honorarios · iva 19%" | 900123456 | COSMOS-SAS | VTA-001 | 57.000 | COP | 2026-03-18 |
| 3 | nota_credito_gasto | retefuente | "honorarios · retención en la fuente 11%" | 900123456 | COSMOS-SAS | VTA-001 | 33.000 | COP | 2026-03-18 |
| 4 | nota_credito_gasto | contrapartida | "crédito con el proveedor · devolución parcial de honorarios" | 900123456 | COSMOS-SAS | — | — (motor) | COP | 2026-03-18 |

Los valores son positivos (D19). La plantilla de asiento invierte las naturalezas. Las clasificaciones espejan las de la causación original. **Todas las líneas viajan con `referenciaHechoRelacionado` = la causación del Ejemplo 1** — en esta plantilla todos los componentes se resuelven por espejo [R53].

### 4.3 Plantilla de asiento (código del producto)

El motor identifica `tipoTransaccion = nota_credito_gasto` y aplica la plantilla inversa a la causación:

| Rol | Naturaleza | Alimentado por | Grupo PUC esperado | Descripción |
|-----|-----------|----------------|--------------------|-------------|
| GASTO | **Crédito** | Líneas con `tipoComponente = concepto_devuelto` — espeja GASTO | `["51","52","53"]` | Inverso: acredita la cuenta de gasto |
| IMPUESTO | **Crédito** | Líneas con `tipoComponente = iva` — espeja IMPUESTO | `iva → ["2408"]` | Inverso: acredita la cuenta de impuesto |
| RETENCION | **Débito** | Líneas con `tipoComponente = retefuente` — espeja RETENCION | `retefuente → ["2365"]` | Inverso: debita la cuenta de retención |
| CONTRAPARTIDA | **Débito** | Línea con `tipoComponente = contrapartida` (valor: motor) — espeja CONTRAPARTIDA | `["2205","2335"]` | Reduce CxP |

### 4.4 Resolución por espejo

La nota crédito debe reversar **exactamente las mismas cuentas** de la causación original — por eso todos sus componentes declaran `resolucionPorEspejo` [R53]. El motor ubica su borrador de la causación del Ejemplo 1 (vía `referenciaHechoRelacionado`) y copia la cuenta del rol homólogo:

```
concepto_devuelto → espejo del rol GASTO original → 5110-05-002
iva               → espejo del rol IMPUESTO original → 2408-01-001
retefuente        → espejo del rol RETENCION original → 2365-05-001
contrapartida     → espejo del rol CONTRAPARTIDA original → 2205-01-001
```

Sin cadena de resolución, sin intervención del usuario y sin alimentar el aprendizaje: la cuenta es determinística. Si la causación original no existiera en Contabilidad (ej. saldos migrados de un sistema anterior), el borrador nacería pendiente para que el contador asigne.

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

> **Nota sobre el carácter de este inventario:** Las 42 plantillas listadas en esta sección son un **planteamiento teórico inicial** que sirvió para dimensionar el alcance. No todas se implementarán necesariamente, ni en este número exacto: el conjunto real de cada sub-dominio se determina al modelarlo y refinarlo. La **especificación de verdad** de cada plantilla (roles, componentes y `grupoPucEsperado`) vive en `datos-precargados/plantillas-de-asiento.*` y se va alineando con cada refinamiento. Ejemplo ya alineado: **OXP** — al cruzar con su mapeo canónico real, emite **4** `tipoTransaccion` (`causacion_gasto`, `anticipo_a_proveedor`, `nota_credito_gasto`, `reversa_anticipo`), no las 6 estimadas aquí: lo que el inventario listaba como "nota débito a proveedor", "aplicación de anticipo" y "diferencia en cambio" no son plantillas propias (las dos últimas viajan como componentes de `causacion_gasto`), y faltaba "reversa de anticipo". Donde el catálogo precargado y este inventario difieran, **manda el catálogo precargado**.

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
| **OXP** | 6 *(teórico; 6 reales — ver `datos-precargados/plantillas-de-asiento.*`)* | Causación de obligación, nota crédito proveedor, nota débito proveedor, anticipo a proveedor, aplicación de anticipo, diferencia en cambio |
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
| Resolución de la cuenta auxiliar por rol | Espejo del hecho relacionado (componentes marcados) o la cadena A → C → B sobre la clasificación de la línea | Sí — la cadena se enriquece con cada interacción del usuario; el espejo es determinístico |
| Clasificación de la línea | Cada sub-dominio emisor la compone mecánicamente desde sus catálogos (recetas por componente en el modelo del emisor) | No — no la digita un usuario |
| Plan de cuentas | El cliente (importado) | Sí — cada empresa tiene el suyo |
| Reglas manuales (nivel A) | El analista contable | Sí — excepciones explícitas |

---

## 7. Ejemplo 4 — Extracto de cruce puro (origen del tercero de la contrapartida)

> Se ubica aquí, después del resumen, por ser un ejemplo añadido en un refinamiento posterior (issue #28); conceptualmente acompaña a los ejemplos 1–3.

Caso: el extracto de la tarjeta de **Bancolombia (890.903.938)** del mes cruza dos compras **ya causadas** —proveedor **A (901.090.486)** por 600.000 y proveedor **B (860.533.413)** por 400.000— **sin cargos financieros, diferencia en cambio ni ajustes por tolerancia**. OXP emite solo dos líneas `cruce_obligacion` (`[D29]` de OXP). Ilustra de dónde sale el tercero de la contrapartida.

### 7.1 lineasParaTraduccion() — el banco viaja en la línea de contrapartida

**A nivel del hecho económico:** `tipoTransaccion = causacion_gasto`, `terceroPrincipal = 890903938` (Bancolombia — el `InformacionTercero` raíz del extracto; **informativo** desde [D15]: el tercero de la contrapartida ya viaja en su propia línea).

| # | tipoComponente | clasificacion | tercero (de la línea) | referenciaHechoRelacionado | unidadOrg | valor | moneda |
|---|---------------|---------------|-----------------------|----------------------------|-----------|-------|--------|
| 1 | cruce_obligacion | "cruce de obligación · factura FV-2201 · Prov A" | 901090486 (Prov A) | causación OxpComercio A | — | 600.000 | COP |
| 2 | cruce_obligacion | "cruce de obligación · factura FV-0917 · Prov B" | 860533413 (Prov B) | causación OxpComercio B | — | 400.000 | COP |
| 3 | contrapartida | "tarjeta de crédito Bancolombia · extracto enero" | 890903938 (Bancolombia) | — | — | — (motor) | COP |

El banco viaja como tercero de la **línea de contrapartida** [R54]. (La unidad organizacional de la CxP se rinde según `[I33]`; en este ejemplo, consolidada sin unidad.)

### 7.2 Plantilla y resolución

El motor aplica `causacion_gasto`. Las líneas `cruce_obligacion` alimentan el rol **CRUCE_OBLIGACION** y se resuelven **por espejo** [R53]: cada una copia la cuenta de la CONTRAPARTIDA (CxP del proveedor) del borrador de su causación cruzada — así el débito salda exactamente la cuenta donde nació la deuda. La **CONTRAPARTIDA** (Cr CxP del banco) toma tercero y clasificación de la línea 3; su cuenta se resuelve por la cadena y su valor lo calcula el motor (paso 4 del `ServicioDeTraduccion`).

### 7.3 Asiento contable resultante

| Partida | Cuenta | Tercero | Débito | Crédito |
|---------|--------|---------|--------|---------|
| 1 | 2205-… CxP proveedor | 901090486 (Prov A) | 600.000 | |
| 2 | 2205-… CxP proveedor | 860533413 (Prov B) | 400.000 | |
| 3 | 2205-… CxP banco/emisor | 890903938 (Bancolombia) | | 1.000.000 |
| | | **Totales** | **1.000.000** | **1.000.000** |

La línea de contrapartida resuelve el problema que originó este ejemplo (issue #28): las líneas de cruce traen varios proveedores y el banco no viajaba en ninguna. Hoy el banco llega como tercero de la línea `contrapartida` [R54]; `terceroPrincipal` se conserva a nivel del hecho como dato informativo. Y el espejo [R53] garantiza que cada cruce debite exactamente la CxP donde nació la deuda de la compra cruzada — sin depender de que la cadena "adivine" la misma cuenta.

---

## 8. Ejemplo 5 — Ciclo de la partida en disputa (cuenta transitoria de partidas por aclarar)

> Añadido en el issue #90; conceptualmente extiende el Ejemplo 4. Ilustra los roles `PARTIDA_POR_ACLARAR`/`PARTIDA_ACLARADA` de `causacion_gasto` y la plantilla `reclasificacion_partida` (`[D36]` de OXP). Cuentas ilustrativas — grupos ⚠️ `porValidar` (ítems 11 y 12 de la revisión pendiente del catálogo).

Caso: el extracto de **enero** de la tarjeta de **Bancolombia (890.903.938)** trae tres partidas: dos compras ya causadas —proveedor **A (901.090.486)** por 600.000 y proveedor **B (860.533.413)** por 400.000— y una partida de **500.000 que nadie reconoce** (posible fraude). El usuario la marca **en disputa** (`PartidaEnDisputaMarcada`), lo que permite conciliar al 100% (`R06` de OXP) sin generar anticipos. El banco cobrará el total: **1.500.000**.

### 8.1 Momento 1 — Causación del extracto de enero (`causacion_gasto`)

**A nivel del hecho:** `tipoTransaccion = causacion_gasto`, `terceroPrincipal = 890903938` (Bancolombia, informativo).

| # | tipoComponente | clasificacion | tercero (de la línea) | valor |
|---|---------------|---------------|-----------------------|-------|
| 1 | cruce_obligacion | "cruce de obligación · factura FV-2201 · Prov A" | 901090486 (Prov A) | 600.000 |
| 2 | cruce_obligacion | "cruce de obligación · factura FV-0917 · Prov B" | 860533413 (Prov B) | 400.000 |
| 3 | partida_por_aclarar | "compra no reconocida por 500.000 · posible fraude" | 890903938 (Bancolombia) | 500.000 |
| 4 | contrapartida | "tarjeta de crédito Bancolombia · extracto enero" | 890903938 (Bancolombia) | — (motor) |

Los cruces (líneas 1-2) viajan con `referenciaHechoRelacionado` a su causación y se resuelven **por espejo** [R53]; la partida por aclarar (línea 3) se resuelve por la cadena con su clasificación; la contrapartida (línea 4) trae el tercero del banco [R54].

**Asiento resultante:**

| Partida | Cuenta | Tercero | Débito | Crédito |
|---------|--------|---------|--------|---------|
| 1 | 2205-… CxP proveedor *(espejo de la causación A)* | 901090486 (Prov A) | 600.000 | |
| 2 | 2205-… CxP proveedor *(espejo de la causación B)* | 860533413 (Prov B) | 400.000 | |
| 3 | 1360-… Reclamaciones (partidas por aclarar) | 890903938 (Bancolombia) | 500.000 | |
| 4 | 2205-… CxP banco/emisor *(línea contrapartida; valor del motor)* | 890903938 (Bancolombia) | | 1.500.000 |
| | | **Totales** | **1.500.000** | **1.500.000** |

Sin la línea 3, la CxP del banco quedaría en 1.000.000 — **subvalorada** frente a los 1.500.000 que el banco cobra. Con ella, el pasivo refleja la deuda real y el derecho de la reclamación queda visible en el activo, **por tercero** (se sabe cuánto se le reclama a cada banco).

### 8.2 Momento 2a — Resolución por descarte: el banco reversa en el extracto de marzo

La línea de "Reverso Bancario" (−500.000) llega en el extracto de **marzo** y el usuario la vincula a la disputa de enero (conciliación trans-mensual, `R10c` de OXP → `PartidaEnDisputaDescartada`). La cancelación viaja **dentro de la causación del extracto de marzo** (supóngase un solo cruce adicional de 300.000):

| # | tipoComponente | clasificacion | tercero (de la línea) | valor |
|---|---------------|---------------|-----------------------|-------|
| 1 | cruce_obligacion | "cruce de obligación · factura FV-3304 · Prov A" | 901090486 (Prov A) | 300.000 |
| 2 | partida_aclarada | "reverso bancario · compra no reconocida por 500.000 · posible fraude" | 890903938 (Bancolombia) | 500.000 |
| 3 | contrapartida | "tarjeta de crédito Bancolombia · extracto marzo" | 890903938 (Bancolombia) | — (motor) |

La línea `partida_aclarada` viaja con `referenciaHechoRelacionado` a la **causación del extracto de enero** y se resuelve **por espejo** del rol PARTIDA_POR_ACLARAR [R53]: cancela exactamente la misma cuenta 1360 que abrió la disputa, con naturaleza contraria (Cr).

| Partida | Cuenta | Tercero | Débito | Crédito |
|---------|--------|---------|--------|---------|
| 1 | 2205-… CxP proveedor *(espejo de la causación A)* | 901090486 (Prov A) | 300.000 | |
| 2 | 1360-… Reclamaciones *(espejo de la partida por aclarar de enero)* | 890903938 (Bancolombia) | | 500.000 |
| 3 | 2205-… CxP banco/emisor *(línea contrapartida; valor del motor, ya neto del reverso)* | 890903938 (Bancolombia) | 200.000 | |
| | | **Totales** | **500.000** | **500.000** |

La 1360 queda **en cero** para esa reclamación — abrió y cerró contra el mismo tercero (Bancolombia). *(Nota: si el extracto de marzo trae más movimientos que el reverso, la contrapartida del motor resulta acreedora como de costumbre; el ejemplo se redujo para que se vea el mecanismo.)*

### 8.3 Momento 2b — Resolución por reclasificación: se identifica el gasto real (`reclasificacion_partida`)

Camino alterno: se descubre que los 500.000 corresponden a una compra legítima del proveedor **C (830.037.248)** que nadie había radicado. Se radica y causa la OxpComercio **por el flujo normal** (Db gasto 420.168 + Db IVA 79.832 · Cr CxP proveedor C 500.000 — plantilla `causacion_gasto`, sin novedades). Luego `PartidaEnDisputaReclasificada` emite el hecho propio:

**A nivel del hecho:** `tipoTransaccion = reclasificacion_partida`, `terceroPrincipal = 890903938` (Bancolombia — el hecho pertenece al ciclo del extracto en disputa; informativo: esta plantilla no tiene rol de contrapartida, sus dos líneas viajan con valor).

| # | tipoComponente | clasificacion | tercero (de la línea) | valor |
|---|---------------|---------------|-----------------------|-------|
| 1 | cruce_obligacion | "cruce de obligación · factura FV-8812 · Prov C" | 830037248 (Prov C) | 500.000 |
| 2 | partida_aclarada | "reclasificación · compra no reconocida por 500.000 identificada" | 890903938 (Bancolombia) | 500.000 |

Ambas líneas se resuelven **por espejo** [R53]: el cruce copia la CxP de la causación de la OxpComercio del proveedor C (su `referenciaHechoRelacionado`); la partida aclarada copia la 1360 de la causación del extracto de enero.

| Partida | Cuenta | Tercero | Débito | Crédito |
|---------|--------|---------|--------|---------|
| 1 | 2205-… CxP proveedor *(espejo de la causación C)* | 830037248 (Prov C) | 500.000 | |
| 2 | 1360-… Reclamaciones *(espejo de la partida por aclarar de enero)* | 890903938 (Bancolombia) | | 500.000 |
| | | **Totales** | **500.000** | **500.000** |

La CxP del proveedor C nace en su causación y se salda aquí (su compra ya fue pagada vía tarjeta — es el "cruce diferido" que en un extracto normal habría viajado en el Momento 1); la 1360 queda en cero. En **cualquiera de los dos caminos** la transitoria abre y cierra, auditable por partida y por tercero.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: patrón universal, contrato LineaTraduccion, 3 ejemplos OXP (causación, anticipo, nota crédito), cadena de resolución A → C → B, inventario de plantillas por sub-dominio emisor (8 sub-dominios, 42 plantillas). |
| 1.1 | Mayo 2026 | Grupo del PUC esperado (issue #7). Nota explicativa de `grupoPucEsperado` en la Sección 1.3 y nueva columna "Grupo PUC esperado" en las tablas de plantilla de los 3 ejemplos (causación, anticipo, nota crédito). Alinea con `modelo-dominio.md` v1.3 [D12] y `definicion-alcance.md` v1.3 [R47]. Llenado del inventario completo (Sección 5) y confirmación del grupo del `inc` pendientes de revisión por consultor contable. |
| 1.2 | Junio 2026 | Encuadre del inventario teórico (issue #7). Nota en la Sección 5 que aclara que las 42 plantillas son un planteamiento inicial de dimensionamiento — no todas se implementarán y el número real se determina al modelar cada sub-dominio; la fuente de verdad es `datos-precargados/plantillas-de-asiento.*`. Fila de OXP del resumen (5.3) marcada como "6 teórico; 4 reales". Acompaña la creación del catálogo precargado `datos-precargados/plantillas-de-asiento.md`/`.json` con las 4 plantillas reales de OXP. |
| 1.3 | Junio 2026 | Nuevo **Ejemplo 4 — Extracto de cruce puro** (Sección 7, issue #28): ilustra de dónde sale el tercero de la contrapartida cuando las líneas traen varios proveedores y el banco/emisor no viaja en ninguna. Muestra el uso de `terceroPrincipal` (tercero del documento a nivel del hecho económico) para la contrapartida (CxP del banco) y el tercero por línea para los cruces. Se ubica como Sección 7 (tras el resumen) para no renumerar las secciones existentes ni romper referencias cruzadas. Alinea con `modelo-dominio.md` v1.8 (`InformacionTransaccion` con `terceroPrincipal`, paso 4 del `ServicioDeTraduccion`). |
| 1.4 | Julio 2026 | Nuevo **Ejemplo 5 — Ciclo de la partida en disputa** (Sección 8, issue #90): los tres momentos del ciclo por cuenta transitoria de partidas por aclarar — causación del extracto con la línea `partida_por_aclarar` (la CxP del banco refleja el total real), resolución por descarte (`partida_aclarada` dentro de la causación del extracto donde llega el reverso, conciliación trans-mensual) y resolución por reclasificación (plantilla nueva `reclasificacion_partida`, sin contrapartida del motor). Muestra el tercero de cada línea (la transitoria siempre contra el banco/emisor; las CxP contra su proveedor). Fila de OXP del resumen (5.3) actualizada a "6 reales" y encabezado del anexo corregido (decía v1.2 pese al historial). Alinea con el catálogo precargado v1.7 y `[D37]` de OXP. |
| 1.5 | Julio 2026 | **Clasificación semántica, contrapartida como línea y resolución por espejo (issue #104).** Contrato §1.2: la fila `clasificacion` pasa de "códigos de referencia" a **texto semántico compuesto mecánicamente por el emisor** (obligatorio en toda línea, [R52]); `referenciaHechoRelacionado` documenta su segundo uso — habilitar el **espejo** [R53]; principios reescritos (contrapartida como línea sin valor ni unidad, [R54]; `terceroPrincipal` informativo). Cadena §1.4 con el paso **0 — Espejo del hecho relacionado** y los Niveles A/C redefinidos como partición estable exacta + emparejamiento por similitud. Los 5 ejemplos actualizados: clasificaciones con textos reales (recetas de OXP), línea `contrapartida` en cada hecho con rol de contrapartida, Ejemplo 3 resuelto íntegramente por espejo (la nota crédito reversa exactamente las cuentas de la causación original), Ejemplo 4 reescrito (el banco viaja en la línea de contrapartida; los cruces espejan la CxP de su causación) y Ejemplo 5 con el espejo de la 1360 en ambas resoluciones. Resumen del patrón (§6) con las filas de resolución y clasificación actualizadas. Alinea con el modelo v1.12 ([D15]), el alcance v1.11 ([R52]-[R54]), el catálogo precargado v1.9 y el modelo de OXP v4.8. |
