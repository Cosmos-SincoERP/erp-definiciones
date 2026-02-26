# Modelo de Dominio OXP — Eventos y Transiciones

## Tabla de contenido

1. [Propósito y relación con otros documentos](#1-propósito-y-relación-con-otros-documentos)
2. [Convenciones del documento](#2-convenciones-del-documento)
3. [Bounded Context y Agregados](#3-bounded-context-y-agregados)
4. [Máquinas de estado](#4-máquinas-de-estado)
5. [Catálogo de eventos](#5-catálogo-de-eventos)
6. [Tipos de concepto](#6-tipos-de-concepto)
7. [Invariantes del dominio](#7-invariantes-del-dominio)
8. [Qué NO contiene este documento](#8-qué-no-contiene-este-documento)
9. [Decisiones de arquitectura y diseño](#9-decisiones-de-arquitectura-y-diseño)
10. [Premisas de negocio](#10-premisas-de-negocio)

---

## 1. Propósito y relación con otros documentos

Este documento especifica el comportamiento interno del dominio OXP mediante eventos, transiciones de estado, precondiciones, invariantes y la información de negocio que cada evento captura. Su objetivo es servir como puente entre la definición funcional y la implementación técnica.

| Documento | Alcance | Relación |
|-----------|---------|----------|
| `Definicion_Alcance.md` | QUÉ hace el sistema | Fuente de verdad para glosario, actores, flujos y reglas (R01–R27). No se duplica aquí. |
| **Este documento** | CÓMO se comporta el dominio | Eventos, transiciones, precondiciones, invariantes, tipos de concepto. |
| `Justificaciones/ModelarAgregados.md` | POR QUÉ múltiples agregados | Análisis comparativo de agregado único vs. múltiples agregados desde event sourcing. |
| EventCatalog (fase 2) | Catalogación técnica | Consumirá este documento como especificación de entrada durante la implementación. |

Las reglas de negocio se referencian como `[R##]` y su texto completo vive en `Definicion_Alcance.md`, Sección 6.

---

## 2. Convenciones del documento

### Nomenclatura

- **Eventos:** PascalCase en español. Ej: `OxpComercioRadicada`, `ExtractoConciliado`.
- **Referencias a reglas:** `[R##]` remite a `Definicion_Alcance.md`, Sección 6.
- **Premisas de negocio:** `[P##]` remite a Sección 10 de este documento.
- **Sugerencias de implementación:** `[SI##]` — recomendaciones técnicas que complementan y clarifican una definición del dominio, orientando cómo llevarla a código. No son restricciones del modelo de dominio ni decisiones de arquitectura.
- **Agregados:** OxpComercio, OxpExtracto, Anticipo, Devolucion.
- **Estados OxpComercio:** Pendiente, Confirmada, Causada, Pagada, Devuelta.
- **Estados Devolucion:** Pendiente, Confirmada, Causada.
- **Estados OxpExtracto:** Pendiente, Parcialmente Conciliado, Conciliado, Confirmado, Causado, Pagado.
- **Estados Anticipo:** Vigente, Pagado, Regularizado, Cerrado, Reversado.

### Template de evento

Cada evento del catálogo (Sección 5) se documenta con la siguiente estructura:

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Qué ocurrió en términos de negocio. |
| **Agregado** | OxpComercio / OxpExtracto / Anticipo / Devolucion. |
| **Estado previo** | Estado(s) desde los que puede emitirse. |
| **Estado resultante** | Estado al que transiciona la entidad. |
| **Precondiciones** | Condiciones requeridas. Ref. a reglas: [R##]. |
| **Información capturada** | Datos de negocio que el evento registra (no campos de BD). |
| **Efectos** | Integraciones salientes, alertas u otros eventos derivados. |

### Diagramas

Las máquinas de estado usan notación ASCII. Los estados terminales se marcan con `■`. Las transiciones se etiquetan con el nombre del evento entre paréntesis.

### Causalidad entre eventos

Cuando un evento produce otro evento, se distinguen tres tipos de causalidad:

| Tipo | Alcance | Mecanismo | Ejemplo |
|---|---|---|---|
| **Evento derivado por transición** | Mismo agregado, mismo append | El agregado evalúa una condición de estado y emite un segundo evento en la misma operación. | `PagoOxpComercioViaExtractoAplicado` reduce `saldoPorPagar()`; si llega a 0, emite `OxpComercioPagada`. `VinculacionRealizada` resuelve una partida; si 100% resueltas, emite `ExtractoConciliado`. |
| **Evento derivado por configuración** | Mismo agregado, condicional | El agregado emite un segundo evento solo si una regla de negocio configurable lo habilita. | `OxpComercioRadicada` emite `OxpComercioConfirmada` si `[R02]` está configurada como automática. `OxpComercioConfirmada` emite `OxpComercioCausada` si `[R12]` está configurada como automática. |
| **Efecto inter-agregado** | Múltiples streams, consistencia eventual | Un domain service coordina la emisión de eventos en streams de agregados diferentes. | `ServicioDeConciliacion` emite `VinculacionRealizada` (OxpExtracto) + `PagoOxpComercioViaExtractoAplicado` (OxpComercio). `ServicioDeAplicacionDevolucion` emite `DevolucionConfirmada` (Devolucion) + evento de efecto en el agregado OXP origen. |

---

## 3. Bounded Context y Agregados

### OXP como Bounded Context

OXP (Obligaciones por Pagar) es un **bounded context** — no un agregado. Contiene múltiples agregados coordinados que en conjunto gestionan el ciclo de vida de las obligaciones originadas en medios de pago corporativos.

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                        Bounded Context: OXP                                    │
│                                                                                │
│  ┌──────────────┐      cubre partida(s) (TC)      ┌──────────────────┐        │
│  │   Anticipo   │ ─────────────────────────────► │   OxpExtracto    │        │
│  │  (Agregado)  │                                │   (Agregado)     │        │
│  └──────┬───────┘                                └────────┬─────────┘        │
│         ▲ crea (excedente)                                │         ▲        │
│         ▲ reversa                                         │         │        │
│         │                                                 │  ajuste │        │
│  ┌──────┴──────────┐  ServicioDe                          │  sobre  │        │
│  │  Devolucion     │  AplicacionDevolucion  ServicioDe    │         │        │
│  │  (Agregado)     │  (Domain Service)      Conciliacion  │         │        │
│  └──┬──────────────┘                        (Domain Svc)  │         │        │
│     │ espejo de              ServicioDe                    │         │        │
│     │                        Regularizacion                │         │        │
│     │                        (Domain Service)              │         │        │
│     ▼                                                      │         │        │
│  ┌──────────────────┐◄─────────────────────────────────────┘         │        │
│  │   OxpComercio    │                                                │        │
│  │   (Agregado)     │            Devolucion ─────────────────────────┘        │
│  └──────────────────┘                                                         │
│                                                                                │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │              Value Objects compartidos                                  │  │
│  │  InformacionTercero · MedioDePago · ValorMonetario · SoporteDocumental │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Agregado: OxpComercio

- **Raíz:** Una obligación individual originada por compra con tarjeta corporativa (crédito o débito prepago).
- **Ciclo de vida:** Radicación → Confirmación → Causación → Pago(s) → Pagada.
- **Estado terminal:** Pagada (`saldoPorPagar() = 0`).
- **Stream de eventos:** `oxp-comercio-{id}`
- **Eventos propios:** 11 (DevolucionAsociada eliminado en v2.2, PagoOxpComercioViaDevolucionAplicado agregado, OxpComercioExteriorRadicada eliminado — consolidado como efecto condicional de OxpComercioRadicada).

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `ConceptoDeGasto` | Gasto o costo de la obligación. Pueden existir múltiples conceptos idénticos como registros independientes. Invariante: mínimo 1 por OxpComercio. | Código, descripción, cantidad, valor. Desglose fiscal: `DesgloseFiscal` (VO). |
| `PagoAplicado` | Cada registro representa un cruce parcial contra el valorNeto de la obligación. Inmutable una vez creado. Tipo: `extracto` (ref. a OxpExtracto + PartidaExtracto, valor cubierto; creado por `PagoOxpComercioViaExtractoAplicado`), `anticipo` (ref. a Anticipo, monto cubierto; creado por `PagoOxpComercioViaAnticipoAplicado`), `pago_directo` (ref. a pago SincoA&F, valor pagado; creado por `PagoOxpComercioDirectoAplicado`), o `devolucion` (ref. a Devolucion, monto cubierto; creado por `PagoOxpComercioViaDevolucionAplicado`). Los cuatro tipos pueden coexistir (pagos mixtos). | Tipo, referencia (varía por tipo), valor, fecha. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `InformacionTercero` | NIT, razón social |
| `MedioDePago` | Tipo (crédito/débito prepago), número, entidad bancaria |
| `ValorMonetario` | Monto, moneda, TRM (si aplica), monto en moneda funcional |
| `SoporteDocumental` | Tipo (PDF, imagen, XML), referencia, datos extraídos |
| `DesgloseFiscal` | Agrupa los cálculos fiscales derivados de un `ConceptoDeGasto`. Inmutable — se reemplaza completo al recalcular. Contiene: `List<Tributo>` de impuestos y `List<Tributo>` de retenciones. |
| `Tributo` | Cálculo fiscal individual (impuesto o retención). Tipo, base, tarifa, valor. Inmutable — es el resultado de aplicar reglas fiscales al gasto. |
| `InstruccionDistribucion` | Distribución por unidad organizacional. Indica cómo distribuir un valor entre unidades organizacionales. Se aplica al valor total de la obligación y a cada componente individual (ConceptoDeGasto o Tributo). Cada referencia tiene su propia distribución independiente. `List<DestinoDeNegocio>` (invariante I2: suma = 100%). |
| `DestinoDeNegocio` | Identificador de unidad organizacional (Shared Kernel con el contexto contable), porcentaje. Ej: `{ unidadOrganizacional: "VTA-001", porcentaje: 60 }`. Usado dentro de `InstruccionDistribucion`. |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  OxpComercio (Agregado)                                      │
│                                                              │
│  ○ InformacionTercero    ○ MedioDePago    ○ ValorMonetario   │
│  ○ SoporteDocumental                                        │
│                                                              │
│  Invariante: mínimo 1 ConceptoDeGasto                        │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ConceptoDeGasto #1 (Entidad)                           │  │
│  │  codigo · descripcion · cantidad · valor               │  │
│  │                                                        │  │
│  │  desgloseFiscal: (VO)                                  │  │
│  │   ○ Tributo { IVA, base: 600k, 19%, $114k }           │  │
│  │   ○ Tributo { ReteFte, base: 600k, 2.5%, $15k }       │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ConceptoDeGasto #2 (Entidad)                           │  │
│  │  codigo · descripcion · cantidad · valor               │  │
│  │                                                        │  │
│  │  desgloseFiscal: (VO)                                  │  │
│  │   ○ Tributo { ReteFte, base: 400k, 2.5%, $10k }       │  │
│  │   (IVA no aplica para este tipo de gasto)              │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Componente: Pagos aplicados (resuelve valorNeto)       │  │
│  │                                                        │  │
│  │ PagoAplicado #1 (Entidad)                              │  │
│  │  tipo: extracto · ref OxpExtracto · ref Partida        │  │
│  │  valor cubierto · fecha                                │  │
│  │                                                        │  │
│  │ PagoAplicado #2 (Entidad)                              │  │
│  │  tipo: anticipo · ref Anticipo                         │  │
│  │  valor cubierto · fecha                                │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ InstruccionDistribucion — unidad organizacional (VO)   │  │
│  │                                                        │  │
│  │  ○ Total obligación → { FIN-001: 100% }                │  │
│  │                                                        │  │
│  │  ○ Gasto #1       → { VTA-001: 60%, ADM-001: 40% }    │  │
│  │  ○ IVA de #1      → { FIN-001: 100% }                 │  │
│  │  ○ ReteFte de #1  → hereda de Gasto #1                 │  │
│  │  ○ Gasto #2       → { COM-001: 100% }                 │  │
│  │  ○ ReteFte de #2  → hereda de Gasto #2                 │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  valorBruto()       → sum(gastos.valor)     = 1.000k  │  │
│  │  totalImpuestos()   → sum(tributos imp.)    =   114k  │  │
│  │  totalRetenciones() → sum(tributos ret.)    =    25k  │  │
│  │  valorNeto()        → bruto + imp. - ret.   = 1.089k  │  │
│  │                                                        │  │
│  │  saldoPorPagar()    → valorNeto()                      │  │
│  │                        - sum(pagos aplicados)           │  │
│  │                                                        │  │
│  │  lineasParaTraduccion() → List<LineaTraduccion>        │  │
│  │   Pre-computa líneas planas por combinación            │  │
│  │   (componente × destino) con valor distribuido.        │  │
│  │   El traductor solo mapea, no distribuye.              │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ● = Entidad (tiene identidad)   ○ = Value Object (sin ID)  │
└──────────────────────────────────────────────────────────────┘
```

**Reglas de consistencia del agregado:**

Las instrucciones de distribución no viven de forma independiente — dependen de los componentes que las originan. El agregado `OxpComercio` es responsable de mantener la coherencia entre los conceptos, su desglose fiscal y las instrucciones de distribución.

| Operación sobre componentes | Efecto sobre instrucciones de distribución |
|---|---|
| **Se agrega un ConceptoDeGasto** | Se crea instrucción por defecto según cadena de resolución (ver abajo). |
| **Se elimina un ConceptoDeGasto** | Se eliminan todas las instrucciones asociadas al gasto y a cada uno de sus tributos. |
| **Se recalcula desgloseFiscal** (nuevo tributo aparece) | El nuevo tributo **hereda** la distribución del ConceptoDeGasto padre por defecto. Se puede sobrescribir después. |
| **Se recalcula desgloseFiscal** (un tributo desaparece) | Se elimina la instrucción de distribución de ese tributo. |
| **Se modifica una instrucción de distribución** | Solo afecta al componente referenciado. No propaga a otros. |

**Cadena de resolución de distribución:**

Al consultar la distribución efectiva de cualquier componente, el agregado aplica la siguiente cadena (en orden de prioridad):

1. **Instrucción explícita** → Si el componente tiene instrucción propia, se usa.
2. **Herencia del gasto padre** → Si un Tributo no tiene instrucción propia, hereda la del ConceptoDeGasto al que pertenece.
3. **Preferencia de empresa** → Si el gasto tampoco tiene instrucción explícita, se aplica la configuración por defecto de la empresa.
4. **Destino único pendiente** → Si no hay preferencia configurada, destino único al 100% pendiente de asignación por el usuario.

**Comportamiento calculado del agregado:**

Los valores totales y las líneas de traducción no se almacenan — se derivan de los componentes. Esto garantiza una única fuente de verdad.

| Comportamiento | Descripción |
|---|---|
| `valorBruto()` | Suma del valor de todos los `ConceptoDeGasto`. |
| `totalImpuestos()` | Suma del valor de todos los `Tributo` de tipo impuesto dentro de los desgloses fiscales. |
| `totalRetenciones()` | Suma del valor de todos los `Tributo` de tipo retención dentro de los desgloses fiscales. |
| `valorNeto()` | `valorBruto()` + `totalImpuestos()` - `totalRetenciones()`. |
| `saldoPorPagar()` | `valorNeto()` - sum(`PagoAplicado`.valor). Derivado desde radicación (evoluciona con cambios en conceptos/tributos). Pagos solo se aplican desde estado Causada. Cuando `saldoPorPagar()` = 0 → transición a Pagada. |
| `lineasParaTraduccion()` | Pre-computa una lista plana de líneas, una por cada combinación (componente × destino de negocio), con el valor ya distribuido (valor × porcentaje). Cada línea incluye: tipo de componente (gasto, impuesto, retención), identificador de unidad organizacional, y valor distribuido. El servicio de Traducción Contable recibe estas líneas y solo necesita mapear `(tipo componente + unidad organizacional) → cuenta contable`. No necesita entender distribuciones, herencias ni cadenas de resolución. |

### Agregado: OxpExtracto

- **Raíz:** Una obligación consolidada del período, originada por extracto bancario.
- **Ciclo de vida:** Radicación → Conciliación → Confirmación → Causación → Pago.
- **Estado terminal:** Pagado (`saldoPorPagar()` = 0, financiero — confirmado por SincoA&F).
- **Stream de eventos:** `oxp-extracto-{id}`
- **Eventos propios:** 19.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `PartidaExtracto` | Línea individual del extracto bancario. | Descripción, valor, fecha, estado (pendiente/vinculada/disputa/anticipo/devolucion/descartada). |
| `CargoFinanciero` | Cargo adicional del extracto (no corresponde a compra). | Subtipo (4x1000, cuota de manejo, intereses), valor, período. |
| `AjustePorDiferenciaCambio` | Ajuste generado al vincular OxpComercio en moneda extranjera. | OxpComercio origen, TRM radicación, TRM extracto, valor, clasificación (gasto/ingreso financiero). |
| `AjustePorTolerancia` | Ajuste generado al vincular con diferencia dentro de tolerancia. | OxpComercio origen, valor diferencia, dirección (extracto mayor/menor). |
| `Vinculacion` | Referencia que conecta una partida del extracto con una o más OxpComercio. | Ref. a OxpComercio, partida, tipo (1:1/N:1), origen (automática/manual). |
| `CoberturaAnticipo` | Referencia que conecta una partida del extracto con un Anticipo. Vínculo permanente. Contraparte en el agregado Anticipo: `CrucePagoAplicado` tipo extracto. Cada agregado mantiene su propia entidad (consistencia eventual). | Ref. a Anticipo, partida. |
| `CoberturaDevolucion` | Referencia que conecta una partida del extracto con una Devolucion. Vínculo permanente. Permite cubrir partidas que representan retorno de dinero durante la conciliación. | Ref. a Devolucion, partida. |
| `CrucePagoExtractoAplicado` | Cada registro representa un pago parcial contra el valor total del extracto. Inmutable una vez creado. Tipo: `pago_sincoa` (ref. de pago de SincoA&F, valor, fecha; creado por `PagoExtractoAplicado`) o `devolucion` (ref. a Devolucion, monto cubierto, fecha; creado por `PagoExtractoViaDevolucionAplicado`). Ambos tipos pueden coexistir. | Tipo, referencia (varía por tipo), valor, fecha. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `InformacionTercero` | NIT, razón social |
| `MedioDePago` | Tipo (crédito/débito prepago), número, entidad bancaria |
| `ValorMonetario` | Monto, moneda, TRM (si aplica), monto en moneda funcional |
| `InstruccionDistribucion` | Distribución por unidad organizacional. Indica cómo distribuir un valor entre unidades organizacionales. Se aplica al valor total del extracto y a cada componente individual (CargoFinanciero, AjustePorDiferenciaCambio o AjustePorTolerancia). Cada referencia tiene su propia distribución independiente. `List<DestinoDeNegocio>` (invariante I2: suma = 100%). |
| `DestinoDeNegocio` | Identificador de unidad organizacional (Shared Kernel con el contexto contable), porcentaje. Ej: `{ unidadOrganizacional: "FIN-001", porcentaje: 100 }`. Usado dentro de `InstruccionDistribucion`. |

**Comportamiento calculado del agregado:**

Los valores totales del extracto no se almacenan — se derivan de los componentes. Esto garantiza una única fuente de verdad.

| Comportamiento | Descripción |
|---|---|
| `valorTotalExtracto()` | Suma del valor de todas las `PartidaExtracto` + suma de todos los `CargoFinanciero`. Excluye `AjustePorDiferenciaCambio` y `AjustePorTolerancia` porque estos se generan durante la conciliación como cálculos internos de OXP — no representan montos cobrados por el banco. El extracto bancario define qué se debe (partidas + cargos); los ajustes son correcciones contables que se causan junto con el extracto pero no modifican el monto por pagar. |
| `saldoPorPagar()` | `valorTotalExtracto()` - sum(`CrucePagoExtractoAplicado`.valor). Derivado desde radicación (evoluciona con partidas y cargos). Pagos solo se aplican desde estado Causado. Cuando `saldoPorPagar()` = 0 → transición a Pagado. |
| `lineasParaTraduccion()` | Pre-computa una lista plana de líneas, una por cada combinación (componente × destino de negocio), con el valor ya distribuido (valor × porcentaje). Componentes: CargoFinanciero, AjustePorDiferenciaCambio, AjustePorTolerancia. El servicio de Traducción Contable recibe estas líneas y solo necesita mapear `(tipo componente + unidad organizacional) → cuenta contable`. |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  OxpExtracto (Agregado)                                      │
│                                                              │
│  ○ InformacionTercero    ○ MedioDePago    ○ ValorMonetario   │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ PartidaExtracto #1 (Entidad)                           │  │
│  │  descripcion · valor · fecha · estado: vinculada       │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ PartidaExtracto #2 (Entidad)                           │  │
│  │  descripcion · valor · fecha · estado: disputa         │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ PartidaExtracto #3 (Entidad)                           │  │
│  │  descripcion · valor · fecha · estado: anticipo        │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ PartidaExtracto #4 (Entidad)                           │  │
│  │  descripcion · valor · fecha · estado: devolucion      │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌─────────────────────────────────┐                         │
│  │ CargoFinanciero (Entidad)       │                         │
│  │  subtipo: 4x1000 · valor · per │                         │
│  └─────────────────────────────────┘                         │
│  ┌─────────────────────────────────┐                         │
│  │ CargoFinanciero (Entidad)       │                         │
│  │  subtipo: cuota manejo · valor  │                         │
│  └─────────────────────────────────┘                         │
│  ┌─────────────────────────────────────────────────────┐     │
│  │ AjustePorDiferenciaCambio (Entidad)                 │     │
│  │  oxpComercio origen · TRM rad · TRM ext · valor     │     │
│  └─────────────────────────────────────────────────────┘     │
│  ┌─────────────────────────────────────────────────────┐     │
│  │ AjustePorTolerancia (Entidad)                       │     │
│  │  oxpComercio origen · valor dif · dirección         │     │
│  └─────────────────────────────────────────────────────┘     │
│  ┌─────────────────────────────────────────────────────┐     │
│  │ Vinculacion (Entidad)                               │     │
│  │  ref OxpComercio · partida · tipo 1:1 · auto       │     │
│  └─────────────────────────────────────────────────────┘     │
│  ┌─────────────────────────────────────────────────────┐     │
│  │ CoberturaAnticipo (Entidad)                         │     │
│  │  ref Anticipo · partida                             │     │
│  └─────────────────────────────────────────────────────┘     │
│  ┌─────────────────────────────────────────────────────┐     │
│  │ CoberturaDevolucion (Entidad)                       │     │
│  │  ref Devolucion · partida                           │     │
│  └─────────────────────────────────────────────────────┘     │
│  ┌─────────────────────────────────────────────────────┐     │
│  │ CrucePagoExtractoAplicado (Entidad)                 │     │
│  │  tipo: pago_sincoa · ref pago SincoA&F · valor      │     │
│  │  tipo: devolucion  · ref Devolucion · monto · fecha │     │
│  └─────────────────────────────────────────────────────┘     │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ InstruccionDistribucion — unidad organizacional (VO)        │  │
│  │                                                        │  │
│  │  ○ Total extracto      → { ADM-001: 100% }             │  │
│  │                                                        │  │
│  │  ○ CargoFin. 4x1000   → { FIN-001: 100% }             │  │
│  │  ○ CargoFin. cuota    → { FIN-001: 100% }             │  │
│  │  ○ AjusteDifCambio #1 → { VTA-001: 60%, ADM-001: 40%} │  │
│  │  ○ AjusteTolerancia #1→ { VTA-001: 100% }             │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  valorTotalExtracto() → Monto                         │  │
│  │   Suma PartidaExtracto + CargoFinanciero.              │  │
│  │                                                        │  │
│  │  saldoPorPagar() → Monto                              │  │
│  │   valorTotalExtracto() - sum(CrucePago...valor).       │  │
│  │   Cuando = 0 → ExtractoPagado.                        │  │
│  │                                                        │  │
│  │  lineasParaTraduccion() → List<LineaTraduccion>        │  │
│  │   Pre-computa líneas planas por combinación            │  │
│  │   (componente × destino) con valor distribuido.        │  │
│  │   Componentes: CargoFinanciero, AjustePorDiferencia    │  │
│  │   Cambio, AjustePorTolerancia.                         │  │
│  │   El traductor solo mapea, no distribuye.              │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ● = Entidad (tiene identidad)   ○ = Value Object (sin ID)  │
└──────────────────────────────────────────────────────────────┘
```

### Agregado: Anticipo

- **Raíz:** Pago adelantado al tercero. Puede o no contar con soporte documental (cuando tiene soporte, típicamente es una cuenta de cobro). Puede ya haberse pagado (en cuyo caso la partida aparece en el extracto) o estar pendiente de pago (se debe vincular el pago).
- **Ciclo de vida:** Registro → Pago(s) y/o Regularización(es) → Pagado y/o Regularizado → Cerrado. Alternativa: Reversado desde Vigente (reversión total vía `ServicioDeAplicacionDevolucion`).
- **Estados intermedios:** Pagado (saldoPorPagar = 0), Regularizado (saldoPorRegularizar = 0).
- **Estados terminales:** Cerrado (Pagado + Regularizado), Reversado (desde Vigente, sin cruces previos).
- **Stream de eventos:** `anticipo-{id}`
- **Eventos propios:** 9.

El anticipo tiene **dos comportamientos** según su relación con el extracto, y **dos dimensiones de valor** que se resuelven por caminos diferentes:

**Comportamiento 1 — Vinculado a extracto:** El pago ya se realizó y la partida aparece en el extracto. El valor total se **compensa** contra partida(s) del extracto. La regularización aplica como control posterior.

**Comportamiento 2 — No vinculado a extracto:** El pago está pendiente y se debe vincular. El valor total se resuelve validando el **pago**. La regularización aplica como control posterior.

En ambos casos, la **regularización** siempre ocurre vía OxpComercio que aporta el soporte formal definitivo (factura), independiente de si el anticipo tenía documentación preliminar (ej: cuenta de cobro). El estado terminal (Cerrado) requiere **ambos valores resueltos**: pago del valor total (Pagado) + regularización completa (Regularizado).

**Dimensiones de valor:**

1. **Valor anticipo** (`ValorMonetario`) — monto adelantado. Se resuelve por **regularización** (OxpComercio aporta soporte formal definitivo).
2. **Valor total** — cargo bancario real (comportamiento 1) o monto a pagar (comportamiento 2). Puede diferir del valor anticipo. Se resuelve por **compensación** contra partida(s) del extracto o por **pago directo**.

**Estructura:**

| Componente | Tipo | Contenido |
|---|---|---|
| `InformacionTercero` | VO | NIT, razón social |
| `ValorMonetario` | VO | Valor del anticipo: monto adelantado. Monto, moneda, TRM si aplica, monto en moneda funcional. Valor global sin desglose fiscal `[P1]`. |
| `MedioDePago` | VO | Tipo (crédito/débito prepago), número, entidad bancaria |
| `SoporteDocumental` | VO (opcional) | Soporte preliminar del anticipo (ej: cuenta de cobro). Opcional — el anticipo puede registrarse sin soporte. El soporte formal definitivo (factura) llega vía OxpComercio durante la regularización. |
| `valorTotal` | Valor preestablecido | Inicialmente igual al valor anticipo. Puede diferir si el cargo bancario real (extracto) o el monto a pagar (pago directo) resulta diferente. El saldo se deriva de los cruces (ver comportamiento calculado). |
| `justificacion` | Texto (condicional) | Motivo de ausencia de soporte documental. Solo aplica cuando el anticipo se registra sin soporte. |
| `CrucePagoAplicado` | Entidad (1:N) | Cada registro representa un cruce parcial contra el valor total. Inmutable una vez creado. Tipo: `extracto` (ref. a OxpExtracto + PartidaExtracto, valor cubierto; creado por `AnticipoVinculadoAPartida`), `pago_directo` (ref. a pago confirmado por SincoA&F, valor pagado; creado por `PagoAnticipoAplicado`), `devolucion` (ref. a Devolucion que originó el anticipo; creado por `ServicioDeAplicacionDevolucion` al crear el anticipo por excedente), o `reversa` (ref. a Devolucion tipo Anticipo, valor = valorTotal; creado por `ServicioDeAplicacionDevolucion` Rama Anticipo al reversar). Los tipos extracto, pago_directo y devolucion pueden coexistir. El tipo reversa es exclusivo (solo en Vigente sin cruces previos). |
| `CruceRegularizacionAplicada` | Entidad (1:N) | Cada registro representa un cruce parcial contra el valor anticipo. Inmutable una vez creado. Tipo: `regularizacion` (ref. a OxpComercio, monto regularizado, fecha; creado por `AnticipoRegularizado`) o `reversa` (ref. a Devolucion tipo Anticipo, valor = valorAnticipo; creado por `ServicioDeAplicacionDevolucion` Rama Anticipo al reversar). El tipo reversa es exclusivo (solo en Vigente sin cruces previos). |
| `InstruccionDistribucion` | VO | Distribución por unidad organizacional. Una sola instrucción aplica proporcionalmente tanto al valor anticipo como al valor total (los porcentajes son los mismos para ambas dimensiones). `List<DestinoDeNegocio>` (invariante I2: suma = 100%). |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────┐
│  Anticipo (Agregado)                                              │
│                                                                  │
│  ○ InformacionTercero    ○ MedioDePago    ○ ValorMonetario       │
│  ○ SoporteDocumental (opcional — ej: cuenta de cobro)            │
│  ○ justificacion (si no hay soporte)   ○ valorTotal              │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ Componente 1: Cruces de Compensación (resuelve valorTotal)│  │
│  │                                                            │  │
│  │ CrucePagoAplicado #1 (Entidad)                             │  │
│  │  tipo: extracto · ref OxpExtracto · ref PartidaExtracto   │  │
│  │  valor cubierto · fecha                                    │  │
│  │                                                            │  │
│  │ CrucePagoAplicado #2 (Entidad)                             │  │
│  │  tipo: extracto · ref OxpExtracto · ref PartidaExtracto   │  │
│  │  valor cubierto · fecha                                    │  │
│  └────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ Componente 2: Cruces de Regularización (resuelve valor    │  │
│  │ anticipo)                                                  │  │
│  │                                                            │  │
│  │ CruceRegularizacionAplicada #1 (Entidad)                           │  │
│  │  ref OxpComercio · monto regularizado · fecha              │  │
│  │                                                            │  │
│  │ CruceRegularizacionAplicada #2 (Entidad)                           │  │
│  │  ref OxpComercio · monto regularizado · fecha              │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ InstruccionDistribucion — unidad organizacional (VO)       │  │
│  │                                                            │  │
│  │  ○ Valor anticipo → { VTA-001: 60%, ADM-001: 40% }        │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  Comportamiento calculado (no almacenado):                       │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  saldoPorPagar()    → valorTotal                       │  │
│  │                           - sum(cruces compensación)       │  │
│  │  saldoPorRegularizar()  → valorAnticipo                    │  │
│  │                           - sum(cruces regularización)     │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ● = Entidad (tiene identidad)   ○ = Value Object (sin ID)      │
└──────────────────────────────────────────────────────────────────┘
```

### Agregado: Devolucion

- **Raíz:** Crédito (nota crédito) que reversa total o parcialmente una obligación. Puede referenciar OxpComercio, OxpExtracto o Anticipo según el tipo de OXP.
- **Ciclo de vida:** Radicación → Confirmación (+ aplicación del crédito) → Causación.
- **Estado terminal:** Causada (nota crédito registrada en SincoA&F).
- **Stream de eventos:** `devolucion-{id}`
- **Eventos propios:** 3.

La devolución es un documento independiente que referencia exactamente **un** agregado origen (OxpComercio, OxpExtracto o Anticipo). La estructura interna y el comportamiento calculado varían según el tipo de OXP.

**Referencia a OXP origen (obligatoria, inmutable):**

| Atributo | Detalle |
|----------|---------|
| `tipo` | Comercio \| Extracto \| Anticipo |
| `referencia` | ID del agregado OXP origen |

**Restricciones por tipo:**

- **Comercio:** `ConceptoDevuelto` con código, cantidad y DesgloseFiscal. `valorNeto()` = bruto + impuestos - retenciones. Puede ser parcial (subconjunto de conceptos o montos proporcionales) o total (espejo completo). Una OxpComercio puede tener N devoluciones (relación N:1). InstruccionDistribucion propia.
- **Extracto:** `CargoFinancieroDevuelto` con referenciaCargoFinanciero. `valorNeto()` = sum(cargos.valor). Sin DesgloseFiscal ni InstruccionDistribucion propia — hereda del extracto origen. Aplica exclusivamente a cargos financieros cobrados en un extracto anterior; los reembolsos de compra son Devolucion tipo Comercio (E1a).
- **Anticipo:** `ReversaTotal` con motivoReversa. Sin DesgloseFiscal `[P1]` ni InstruccionDistribucion propia — hereda del anticipo. `valorNeto()` = valor total del anticipo. Solo reversa total (exactamente 1 concepto).

**Escenarios de negocio por tipo de OXP:**

**Tipo Comercio:**

| # | Escenario | saldoPorPagar OXP | valorNeto devolución | Efecto en OxpComercio | Efecto Anticipo |
|---|-----------|-------------------|----------------------|----------------------|-----------------|
| C1 | Devolución total, OXP sin pagos | > 0 | = valorNeto OXP | saldoPorPagar → 0 (Pagada) | — |
| C2 | Devolución parcial, OXP sin pagos | > 0 | < saldoPorPagar | saldoPorPagar disminuye | — |
| C3 | Devolución total, OXP ya pagada | = 0 | = valorNeto OXP | — | Crea Anticipo (Pagado, pendiente regularización) |
| C4 | Devolución parcial, OXP ya pagada | = 0 | < valorNeto OXP | — | Crea Anticipo (Pagado, pendiente regularización) |
| C5 | Devolución parcial = saldo restante | > 0 | = saldoPorPagar | saldoPorPagar → 0 (Pagada) | — |

- Cuando `saldoPorPagar > 0`: `valorNeto(devolucion) ≤ saldoPorPagar(OXP)`. El crédito reduce el saldo directamente.
- Cuando `saldoPorPagar = 0`: la devolución completa se convierte en Anticipo (dimensión pago resuelta, regularización pendiente).
- ⚠️ **Pendiente:** escenario donde `valorNeto(devolucion) > saldoPorPagar` en OXP parcialmente pagada. Puede involucrar nota débito u otro mecanismo contable no definido en este dominio.
- ⚠️ **Pendiente:** reembolso cuando no existe OxpComercio futura para regularizar el Anticipo generado — puede requerir integración con CXC u otro dominio.

**Tipo Extracto:**

| # | Escenario | Origen de la Devolucion | Efecto en OxpExtracto |
|---|-----------|------------------------|----------------------|
| E1 | Partida de retorno en extracto (reembolso de compra) | OxpComercio | Siempre es Devolucion tipo Comercio. Partida vinculada a Devolucion durante conciliación (`PartidaCubiertaPorDevolucion`). Cuenta como resuelta para I3. |
| E2 | Cargo financiero devuelto (ej: cuota de manejo, 4x1000 cobrado de más) | OxpExtracto anterior (donde se cobró) | Devolucion tipo Extracto radicada contra el extracto anterior. El extracto actual (donde llega el crédito) vincula su partida a la Devolucion durante conciliación. Reduce `saldoPorPagar()` del extracto origen. |

- Devoluciones sobre extracto solo aplican cuando `saldoPorPagar > 0` (estado Causado). No se crea Anticipo por excedente en extracto.

**Tipo Anticipo:**

| # | Escenario | Efecto |
|---|-----------|--------|
| A1 | Reversa total por error (proveedor incorrecto o valor incorrecto) | Solo si Vigente sin cruces (`saldoPorPagar` = valorTotal, `saldoPorRegularizar` = valorAnticipo). Anticipo reversado (estado terminal). |
| A2 | Proveedor devuelve dinero | ⚠️ Diferido — requiere dominio CXC. |

**Entidades internas — tres entidades polimórficas con contrato común (`descripcion`, `valor: ValorMonetario`). Valores positivos (magnitud del crédito, D19):**

| Entidad | Tipo OXP | Cardinalidad | Descripción | Atributos propios |
|---|---|---|---|---|
| `ConceptoDevuelto` | Comercio | 1..N | Espejo parcial o total de los conceptos de la OxpComercio origen. | codigo, cantidad, `DesgloseFiscal` (VO). |
| `CargoFinancieroDevuelto` | Extracto | 1..N | Cargo financiero del OxpExtracto anterior que fue devuelto. Espejo de `CargoFinanciero`. | referenciaCargoFinanciero (ref. al `CargoFinanciero` del OxpExtracto origen). |
| `ReversaTotal` | Anticipo | Exactamente 1 | Reversa completa del anticipo. Siempre cubre el 100% del valor. | motivoReversa (proveedor incorrecto \| valor incorrecto). |

**Value Objects:**

| Value Object | Contenido | Aplica a tipo |
|---|---|---|
| `InformacionTercero` | NIT, razón social. Debe coincidir con el tercero del agregado OXP origen. | Todos |
| `ValorMonetario` | Monto, moneda, TRM (si aplica), monto en moneda funcional. | Todos |
| `SoporteDocumental` | Tipo (PDF, imagen, XML), referencia, datos extraídos. | Todos |
| `DesgloseFiscal` | Agrupa los cálculos fiscales derivados de un `ConceptoDevuelto`. Inmutable — se reemplaza completo al recalcular. Contiene: `List<Tributo>` de impuestos y `List<Tributo>` de retenciones. | Comercio |
| `Tributo` | Cálculo fiscal individual (impuesto o retención). Tipo, base, tarifa, valor. Inmutable. | Comercio |
| `InstruccionDistribucion` | Distribución por unidad organizacional. `List<DestinoDeNegocio>` (invariante I2: suma = 100%). Aplica a `ConceptoDevuelto`. Tipos Extracto y Anticipo no tienen distribución propia — heredan del agregado OXP origen. | Comercio |
| `DestinoDeNegocio` | Identificador de unidad organizacional (Shared Kernel), porcentaje. | Comercio |

**Comportamiento calculado del agregado:**

| Método | Tipo Comercio | Tipo Extracto | Tipo Anticipo |
|--------|--------------|---------------|---------------|
| `valorBruto()` | sum(`ConceptoDevuelto`.valor). | N/A | N/A |
| `totalImpuestos()` | sum(impuestos de cada `DesgloseFiscal`). | N/A | N/A |
| `totalRetenciones()` | sum(retenciones de cada `DesgloseFiscal`). | N/A | N/A |
| `valorNeto()` | `valorBruto()` + `totalImpuestos()` - `totalRetenciones()`. Siempre positivo — magnitud del crédito (D19). | sum(`CargoFinancieroDevuelto`.valor). | `ReversaTotal`.valor. |
| `lineasParaTraduccion()` | Pre-cómputo de líneas planas (concepto × destino) con valor distribuido. El servicio de Traducción Contable interpreta como nota crédito (D8). | Línea por cada `CargoFinancieroDevuelto` (el traductor mapea como devolución de cargo financiero). | Línea única de la `ReversaTotal` (el traductor mapea como reversión de anticipo). |

**Diagrama de composición — Devolucion tipo Comercio:**

```
┌──────────────────────────────────────────────────────────────┐
│  Devolucion (Agregado)                                       │
│                                                              │
│  ○ Ref. a OXP origen: tipo + ID (obligatoria, inmutable)     │
│  ○ InformacionTercero    ○ ValorMonetario                    │
│  ○ SoporteDocumental                                        │
│                                                              │
│  Invariante: mínimo 1 entidad interna                        │
│  Invariante: mismo tercero que OXP origen                    │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ConceptoDevuelto #1 (Entidad)                          │  │
│  │  descripcion · valor (contrato común)                  │  │
│  │  codigo · cantidad                                     │  │
│  │  desgloseFiscal: (VO)                                  │  │
│  │   ○ Tributo { IVA, base: 300k, 19%, $57k }            │  │
│  │   ○ Tributo { ReteFte, base: 300k, 2.5%, $7.5k }      │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ InstruccionDistribucion (VO) — solo tipo Comercio      │  │
│  │  ConceptoDevuelto #1 → { VTA-001: 60%, ADM: 40% }     │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  valorBruto()       → sum(conceptos.valor)   = 300k   │  │
│  │  totalImpuestos()   → sum(impuestos)         = 57k    │  │
│  │  totalRetenciones() → sum(retenciones)       = 7.5k   │  │
│  │  valorNeto()        → bruto + imp. - ret.    = 349.5k │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ● = Entidad (tiene identidad)   ○ = Value Object (sin ID)  │
└──────────────────────────────────────────────────────────────┘
```

**Diagrama de composición — Devolucion tipo Extracto:**

```
┌──────────────────────────────────────────────────────────────┐
│  Devolucion (Agregado)                                       │
│                                                              │
│  ○ Ref. a OXP origen: tipo + ID (obligatoria, inmutable)     │
│  ○ InformacionTercero    ○ ValorMonetario                    │
│  ○ SoporteDocumental                                        │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ CargoFinancieroDevuelto #1 (Entidad)                   │  │
│  │  descripcion · valor (contrato común)                  │  │
│  │  referenciaCargoFinanciero                             │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  valorNeto() → sum(cargos.valor)                       │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

**Diagrama de composición — Devolucion tipo Anticipo:**

```
┌──────────────────────────────────────────────────────────────┐
│  Devolucion (Agregado)                                       │
│                                                              │
│  ○ Ref. a OXP origen: tipo + ID (obligatoria, inmutable)     │
│  ○ InformacionTercero    ○ ValorMonetario                    │
│  ○ SoporteDocumental                                        │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ReversaTotal (Entidad) — exactamente 1                 │  │
│  │  descripcion · valor (contrato común)                  │  │
│  │  motivoReversa                                         │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Comportamiento calculado (no almacenado):                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  valorNeto() → reversaTotal.valor                      │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

### Value Objects compartidos

`InformacionTercero` y `ValorMonetario` son Value Objects reutilizados por los cuatro agregados. `MedioDePago` aplica a OxpComercio, OxpExtracto y Anticipo (Devolucion no lo requiere — hereda implícitamente el medio de pago del agregado OXP origen). Cada agregado los incluye en su composición pero la definición es la misma — evita duplicación de estructuras de datos sin acoplar los agregados.

### [SI1] Entidades internas con discriminador de tipo → sealed interfaces

Algunas entidades internas usan un discriminador de tipo para distinguir variantes que comparten la misma estructura de datos: `PagoAplicado` (extracto/anticipo/pago_directo/devolucion), `CrucePagoAplicado` (extracto/pago_directo/devolucion/reversa), `CrucePagoExtractoAplicado` (pago_sincoa/devolucion), `CruceRegularizacionAplicada` (regularizacion/reversa). Se sugiere considerar sealed interfaces (o mecanismo equivalente) para garantizar que todas las variantes se manejen explícitamente.

| Aspecto | Sin sealed (discriminador string) | Con sealed interface |
|---|---|---|
| Variante olvidada | Falla en runtime | Error de compilación |
| Variantes externas no previstas | Cualquiera agrega un string nuevo | Solo las definidas en el módulo |
| Typos en el discriminador | Posibles, fallan silenciosamente | Imposibles — no hay strings |
| Atributos propios por variante | No (mismo data class) | Sí (cada variante es un tipo) |

Ver `Justificaciones/ModelarAgregados.md`, Sección 7.

### Servicio de dominio: ServicioDeConciliacion

La conciliación es la operación que vincula OxpComercio y OxpExtracto. No pertenece a ninguno de los dos — es un **domain service** que coordina efectos en ambos streams:

**Flujo principal (vinculación de compras):**

1. Carga la instancia de `OxpComercio` (stream `oxp-comercio-{id}`)
2. Carga la instancia de `OxpExtracto` (stream `oxp-extracto-{id}`)
3. Valida precondiciones sobre ambos agregados
4. Emite `PagoOxpComercioViaExtractoAplicado` → stream de OxpComercio (crea `PagoAplicado` tipo extracto, reduce `saldoPorPagar()`)
5. Emite `VinculacionRealizada` → stream de OxpExtracto

**Flujo de partidas de retorno (devoluciones):**

Si la partida es un crédito (retorno de dinero), el servicio busca Devoluciones existentes (tipo Comercio) del mismo tercero o permite crear una nueva Devolucion (tipo Extracto):

1. Carga la instancia de `OxpExtracto` (stream `oxp-extracto-{id}`)
2. Identifica partida de retorno (crédito)
3. Busca Devolucion existente (tipo Comercio) del mismo tercero, o permite radicar nueva Devolucion (tipo Extracto)
4. Emite `PartidaCubiertaPorDevolucion` → stream de OxpExtracto (crea `CoberturaDevolucion`, partida transiciona a estado `devolucion`)

Dos streams, consistencia eventual, coordinados por el domain service.

### Servicio de dominio: ServicioDeRegularizacion

La regularización es la operación que vincula un Anticipo con una OxpComercio. Es un **domain service** que coordina efectos en ambos streams:

1. Detecta que la OxpComercio tiene un tercero con anticipo vigente
2. Carga la instancia de `Anticipo` (stream `anticipo-{id}`)
3. Carga la instancia de `OxpComercio` (stream `oxp-comercio-{id}`)
4. Valida precondiciones (anticipo en estado no terminal — ni Cerrado ni Reversado, mismo tercero, `saldoPorRegularizar()` suficiente, `saldoPorPagar()` suficiente en OxpComercio)
5. Emite `AnticipoRegularizado` → stream del Anticipo (crea `CruceRegularizacionAplicada`, reduce saldo por regularizar)
6. Emite `PagoOxpComercioViaAnticipoAplicado` → stream de OxpComercio (crea `PagoAplicado` tipo anticipo, reduce `saldoPorPagar()`)

Dos streams, consistencia eventual, coordinados por el domain service.

⚠️ **Pendiente de precisar:** el momento exacto de la regularización y la coordinación temporal entre ambos agregados. La regularización debe ocurrir en Pendiente/Confirmada (negocio), pero: (1) si `valorNeto()` de la OxpComercio cambia después de regularizar, los cruces inmutables en ambos agregados quedan desfasados; (2) múltiples OxpComercio pueden referenciar el mismo anticipo y el saldo debe reservarse correctamente. Además, I16 actualmente restringe pagos a Causada (integración SincoA&F), pero con el futuro sistema de Tesorería el pago podría desacoplarse de la causación. Requiere diseño detallado.

### Servicio de dominio: ServicioDeAplicacionDevolucion

La aplicación de devolución es la operación que aplica el crédito de una Devolucion contra el agregado OXP origen. Es un **domain service** que coordina efectos en múltiples streams. Se ejecuta como parte de la confirmación de la devolución:

1. Carga Devolucion (stream `devolucion-{id}`) — debe estar en estado Pendiente
2. Según `tipo` de la referencia a OXP origen:
   - **Comercio:** Carga OxpComercio → ejecuta Rama Comercio
   - **Extracto:** Carga OxpExtracto → ejecuta Rama Extracto
   - **Anticipo:** Carga Anticipo → ejecuta Rama Anticipo

**Rama Comercio** (escenarios C1–C5):

3c. Carga OxpComercio referenciada (stream `oxp-comercio-{id}`) — debe estar en estado Causada o Pagada
4c. Evalúa según `saldoPorPagar(OXP)`:

  **Rama Comercio-A — saldoPorPagar > 0** (C1, C2, C5):

  5ca. Valida: `valorNeto(devolucion) ≤ saldoPorPagar(OXP)`
  6ca. Emite `PagoOxpComercioViaDevolucionAplicado` → stream OxpComercio (crea `PagoAplicado` tipo devolucion, reduce `saldoPorPagar()`)
  7ca. Emite `DevolucionConfirmada` → stream Devolucion

  **Rama Comercio-B — saldoPorPagar = 0** (C3, C4):

  5cb. Crea nuevo Anticipo → stream `anticipo-{id}`
    - Anticipo nace con `valorTotal = valorNeto(devolucion)`, `valorAnticipo = valorNeto(devolucion)`
    - Inmediatamente tiene un `CrucePagoAplicado` (tipo devolucion) que referencia la Devolucion que lo originó → `saldoPorPagar() = 0` → estado Pagado
    - `saldoPorRegularizar() = valorNeto(devolucion)` → pendiente de regularización contra nueva OxpComercio o reembolso
  6cb. Emite `DevolucionConfirmada` → stream Devolucion

**Rama Extracto** (escenario E2 — cargo financiero devuelto):

3e. Carga OxpExtracto referenciado (stream `oxp-extracto-{id}`) — debe estar en estado Causado (`saldoPorPagar > 0`)
4e. Valida: `valorNeto(devolucion) ≤ saldoPorPagar(OxpExtracto)`
5e. Emite `PagoExtractoViaDevolucionAplicado` → stream OxpExtracto (crea `CrucePagoExtractoAplicado` tipo devolucion, reduce `saldoPorPagar()`)
6e. Emite `DevolucionConfirmada` → stream Devolucion

Devoluciones sobre extracto solo aplican cuando hay saldo pendiente. No aplica rama `saldoPorPagar = 0` para extracto (a diferencia de Comercio).

**Rama Anticipo** (escenario A1):

3a. Carga Anticipo referenciado (stream `anticipo-{id}`) — debe estar en estado Vigente
4a. Valida: sin `CrucePagoAplicado`, sin `CruceRegularizacionAplicada`
5a. Valida: `valorNeto(devolucion) = valorTotal` del anticipo (reversa total)
6a. Emite `AnticipoReversado` → stream Anticipo (nuevo evento — estado terminal)
7a. Emite `DevolucionConfirmada` → stream Devolucion

Hasta 3 streams por rama, consistencia eventual, coordinados por el domain service.

⚠️ **Pendientes:**
- **Excedente Comercio (devolución > saldoPorPagar cuando saldo > 0):** no se modela todavía. Puede involucrar nota débito u otro mecanismo contable no definido en este dominio.
- **Reembolso:** cuando no existe OxpComercio futura para regularizar el Anticipo generado — puede requerir integración con CXC u otro dominio.
- **Anticipo A2 (proveedor devuelve dinero):** diferido — requiere dominio CXC.

### [SI2] ServicioDeAplicacionDevolucion con 3 ramas → Strategy pattern

El servicio tiene 3 ramas con lógica diferenciada por tipo de OXP. Se sugiere considerar Strategy pattern o servicios especializados por tipo (ej: `EstrategiaDevolucionComercio`, `EstrategiaDevolucionExtracto`, `EstrategiaDevolucionAnticipo`) despachados por un coordinador.

| Aspecto | Sin Strategy (condicionales) | Con Strategy (servicios especializados) |
|---|---|---|
| Agregar nuevo tipo de OXP | Modificar el método existente | Crear una clase nueva, registrarla |
| Testear una rama individual | Instanciar todo el servicio | Instanciar solo la estrategia específica |
| Responsabilidad | Una clase conoce toda la lógica | Cada clase conoce solo su rama |
| Riesgo al modificar | Puede afectar otras ramas | Cada rama está aislada |

### Relaciones entre agregados

```
                    ┌──(N:1)──► OxpComercio ──(N:1)──► OxpExtracto
                    │                ▲                        ▲
                    │(crea excedente)│(regularización)        │
Devolucion ─────────┤                │                        │
                    ├──(N:1)──► OxpExtracto ◄────────────────┘
                    │                                  (conciliación)
                    └──(1:1)──► Anticipo
                                   │
                    Anticipo ──────(1:N)──► OxpComercio (regularización)
                       │
                       └──────(1:N)──► PartidaExtracto (del OxpExtracto)
```

- 1 Devolucion referencia exactamente 1 agregado OXP origen (relación 1:1 desde devolución).
- **Tipo Comercio:** N Devolucion referencian 1 OxpComercio (relación N:1). Si `saldoPorPagar(OXP) = 0` al confirmar: se crea Anticipo por el valor completo de la devolución (dimensión pago resuelta, regularización pendiente).
- **Tipo Extracto:** N Devolucion referencian 1 OxpExtracto (relación N:1). Solo aplica cuando `saldoPorPagar > 0`.
- **Tipo Anticipo:** 1 Devolucion referencia 1 Anticipo (relación 1:1). Solo reversa total. Anticipo transiciona a Reversado (estado terminal).
- 1 Anticipo puede ser regularizado por N OxpComercio (regularización parcial).
- 1 Anticipo **puede** cubrir N partidas de uno o más extractos (vínculo permanente). Los cruces tipo extracto y tipo pago directo pueden coexistir `[R08]`.
- N OxpComercio se vinculan a 1 OxpExtracto (conciliación).
- Una OxpComercio solo puede vincularse a un único OxpExtracto (invariante I7).
- Un OxpExtracto puede recibir N vinculaciones.
- Una OxpComercio puede recibir pagos tipo extracto (conciliación), anticipo (regularización), pago directo (SincoA&F), o devolucion — los cuatro tipos de `PagoAplicado` pueden coexistir (pagos mixtos). La vinculación con extracto sigue siendo opcional.
- La vinculación es por referencia (ID), no por composición. Cada agregado mantiene su propio stream de eventos independiente.

### Patrón: entidades espejo con consistencia eventual

Cuando una operación inter-agregado crea una relación entre dos agregados, cada uno mantiene su propia entidad interna que registra su lado de la relación. Estas entidades se crean en la misma operación del domain service, pero viven en streams independientes — la consistencia es eventual.

| Entidad (OxpExtracto) | Contraparte (otro agregado) | Domain service que coordina |
|---|---|---|
| `Vinculacion` | `PagoAplicado` tipo extracto (OxpComercio) | `ServicioDeConciliacion` |
| `CoberturaAnticipo` | `CrucePagoAplicado` tipo extracto (Anticipo) | `ServicioDeConciliacion` |
| `CoberturaDevolucion` | Referencia al OXP origen en Devolucion | `ServicioDeConciliacion` |

**Convención:** cada agregado es dueño de su entidad espejo y la usa para sus propias invariantes y cálculos (ej: `CoberturaAnticipo` cuenta como resuelta para I3 en OxpExtracto; `CrucePagoAplicado` reduce `saldoPorPagar()` en Anticipo). Ningún agregado consulta la entidad del otro — ambos registran el mismo hecho desde su propia perspectiva.

---

## 4. Máquinas de estado

### 4.1. OxpComercio

```
┌──────────┐  OxpComercioDevuelta  ┌──────────┐
│          │ ─────────────────────► │          │
│Pendiente │                        │ Devuelta │
│          │ ◄───────────────────── │          │
└────┬─────┘  OxpComercioCorregida  └──────────┘
     │
     │ OxpComercioConfirmada
     ▼
┌──────────┐
│Confirmada│
└────┬─────┘
     │ OxpComercioCausada
     ▼
┌─────────────────────────────────────────────┐
│             Causada                          │
│                                             │
│  Eventos de progreso (reducen saldoPorPagar, │
│  sin cambio de estado):                      │
│    · PagoOxpComercioViaExtractoAplicado      │
│    · PagoOxpComercioViaAnticipoAplicado      │
│    · PagoOxpComercioDirectoAplicado          │
│    · PagoOxpComercioViaDevolucionAplicado    │
└────────────────┬────────────────────────────┘
                 │
                 │ OxpComercioPagada
                 │ (saldoPorPagar() = 0)
                 ▼
          ┌──────────┐
          │ Pagada   │ ■
          └──────────┘
```

**Notas:**
- `Pendiente` es el estado inicial para toda radicación.
- `Causada` recibe **eventos de progreso** que reducen `saldoPorPagar()` sin cambiar de estado. Cuatro vías de pago: extracto (conciliación, coordinado por `ServicioDeConciliacion`), anticipo (regularización, coordinado por `ServicioDeRegularizacion`), pago directo (futuro), devolución (coordinado por `ServicioDeAplicacionDevolucion`). Los cuatro tipos pueden coexistir (pagos mixtos).
- `Pagada`: **evento de transición** cuando `saldoPorPagar()` = 0. Único estado terminal financiero, independiente de la(s) fuente(s) de pago.
- La transición `Devuelta → Pendiente` ocurre vía `OxpComercioCorregida`.
- Si `[R02]` está configurada como automática, `OxpComercioRadicada` puede emitir `OxpComercioConfirmada` inmediatamente.

### 4.2. OxpExtracto

```
┌──────────┐  ConciliacionIniciada  ┌───────────────────┐
│Pendiente │───────────────────────►│Parcialmente       │
└──────────┘                        │Conciliado         │
                                    └─────────┬─────────┘
                                              │ ExtractoConciliado [R06]
                                              ▼
                                    ┌───────────────────┐
                                    │Conciliado (100%)  │
                                    └─────────┬─────────┘
                                              │ ExtractoConfirmado
                                              ▼
                                    ┌───────────────────┐
                                    │Confirmado         │
                                    └─────────┬─────────┘
                                              │ ExtractoCausado
                                              ▼
                                    ┌───────────────────┐
                                    │Causado            │
                                    └─────────┬─────────┘
                                              │ ExtractoPagado [R18]
                                              ▼
                                    ┌───────────────────┐
                                    │Pagado             │ ■
                                    └───────────────────┘
```

**Notas:**
- La transición a `Conciliado` requiere 100% de partidas vinculadas a OxpComercio, cubiertas por anticipo, cubiertas por devolución, descartadas, marcadas como disputa, o clasificadas como cargos adicionales `[R06]`.
- Las partidas en disputa, cubiertas por anticipo y cubiertas por devolución cuentan como resueltas para este umbral.
- `Causado` recibe **eventos de progreso** que reducen `saldoPorPagar()` sin cambiar de estado. Dos vías de pago: SincoA&F confirma pagos parciales (`PagoExtractoAplicado`, crea `CrucePagoExtractoAplicado` tipo pago_sincoa) y devolución (`PagoExtractoViaDevolucionAplicado`, crea `CrucePagoExtractoAplicado` tipo devolucion). Ambos tipos pueden coexistir.
- `Pagado`: **evento de transición** (`ExtractoPagado`) cuando `saldoPorPagar()` = 0. Único estado terminal financiero.

#### 4.2.1. PartidaExtracto — máquina de estados interna

Cada `PartidaExtracto` tiene su propia máquina de estados, independiente de la del agregado OxpExtracto. El estado de la partida determina si cuenta como "resuelta" para la invariante I3 (completitud de conciliación).

```
                        ┌─────────────────────────────────────────────────┐
                        │                                                 │
                        │  (VinculacionRealizada)              vinculada  │ ■
                        ├────────────────────────────────────►            │
                        │                                                 │
                        │  (PartidaCubiertaPorAnticipo)        anticipo   │ ■
          ┌──────────┐  ├────────────────────────────────────►            │
          │pendiente │──┤                                                 │
          └──────────┘  │  (PartidaCubiertaPorDevolucion)      devolucion │ ■
                        ├────────────────────────────────────►            │
                        │                                                 │
                        │  (PartidaEnDisputaMarcada)                      │
                        └──────────────────────┐                          │
                                               ▼                          │
                                     ┌──────────────┐                    │
                                     │   disputa     │                    │
                                     └───┬──────┬────┘                    │
                                         │      │                         │
                  (PartidaDisputaDescartada)  (PartidaDisputaReclasificada)│
                                         │      │                         │
                                         ▼      └───────────► vinculada  │ ■
                                    descartada ■                          │
                                                                          │
                                                                          │
```

**6 estados:** pendiente, vinculada, anticipo, devolucion, disputa, descartada.
**6 transiciones:**

| # | Desde | Hacia | Evento |
|---|---|---|---|
| T1 | pendiente | vinculada | `VinculacionRealizada` |
| T2 | pendiente | anticipo | `PartidaCubiertaPorAnticipo` |
| T3 | pendiente | devolucion | `PartidaCubiertaPorDevolucion` |
| T4 | pendiente | disputa | `PartidaEnDisputaMarcada` |
| T5 | disputa | descartada | `PartidaDisputaDescartada` |
| T6 | disputa | vinculada | `PartidaDisputaReclasificada` |

**Estados terminales (■):** vinculada, anticipo, devolucion, descartada. Todos cuentan como "resueltos" para I3.
**Estado intermedio:** disputa — cuenta como resuelta para I3 pero admite transiciones posteriores (T5, T6).

### 4.3. Anticipo

```
                     ┌─────────────────────────────────────────┐
                     │                Vigente                    │
                     │                                         │
                     │  Eventos de progreso (reducen saldo,     │
                     │  sin cambio de estado):                  │
                     │    · AnticipoVinculadoAPartida           │
                     │    · PagoAnticipoAplicado                │
                     │    · AnticipoRegularizado                │
                     └────────┬──────────────┬──────────┬──────┘
                              │              │          │
               (AnticipoPagado)  (Regulariz.)  (AnticipoReversado)
                              │              │          │
                              ▼              ▼          ▼
  ┌──────────────────────────┐  ┌──────────────────────────┐  ┌───────────┐
  │         Pagado            │  │       Regularizado        │  │ Reversado │ ■
  │                          │  │                          │  └───────────┘
  │  Progreso:               │  │  Progreso:               │
  │   · AnticipoRegularizado │  │   · AnticipoVinculado    │
  │                          │  │     APartida             │
  │                          │  │   · PagoAnticipoAplicado │
  └────────────┬─────────────┘  └────────────┬─────────────┘
               │                              │
(Regularizacion│                              │(AnticipoPagado)
DeAnticipo     │                              │
Completada)    └──────────────┬───────────────┘
                              ▼
                       ┌──────────┐
                       │ Cerrado  │ ■
                       └──────────┘
```

**Notas:**
- `Vigente` es el estado desde el registro. Puede recibir pagos (partidas de extracto o pagos directos) y regularizaciones (OxpComercio) en cualquier orden. Los dos tipos de cruce (extracto y pago directo) pueden coexistir.
- **Eventos de progreso** (reducen saldos, sin cambio de estado): `AnticipoVinculadoAPartida` (crea `CrucePagoAplicado` tipo extracto; en Vigente o Regularizado), `PagoAnticipoAplicado` (crea `CrucePagoAplicado` tipo pago_directo; en Vigente o Regularizado), `AnticipoRegularizado` (crea `CruceRegularizacionAplicada`; en Vigente o Pagado).
- **Eventos de transición** (cambian estado cuando un saldo llega a 0): `AnticipoPagado` (saldoPorPagar = 0), `RegularizacionDeAnticipoCompletada` (saldoPorRegularizar = 0), `AnticipoReversado` (ambos saldos = 0 vía cruces tipo reversa, desde Vigente sin cruces previos).
- Tres condiciones independientes para flujo normal:
  - **Pagado:** `saldoPorPagar()` = 0 — el valor total fue cubierto mediante partida(s) de un extracto (TC), o fue pagado por el sistema externo SincoA&F cuando la forma de pago es diferente a TC; OXP monitorea y vincula el pago hasta que se cumple.
  - **Regularizado:** `saldoPorRegularizar()` = 0 — el valor anticipo fue justificado mediante OxpComercio con soporte documental formal (factura).
  - **Cerrado ■:** estado terminal = Pagado **+** Regularizado.
- **Reversado ■:** estado terminal alternativo. Solo desde Vigente, sin cruces previos. El `ServicioDeAplicacionDevolucion` (Rama Anticipo) crea `CrucePagoAplicado` tipo reversa y `CruceRegularizacionAplicada` tipo reversa, llevando ambos saldos a 0. El anticipo fue reversado por error (proveedor incorrecto o valor incorrecto). La Devolucion tipo Anticipo es el documento que evidencia la reversión.
- `Pagado` y `Regularizado` son estados intermedios. En estado Pagado aún se pueden recibir regularizaciones; en estado Regularizado aún se pueden recibir pagos.
- `AnticipoAmortizado` es confirmación externa de SincoA&F (reclasificación contable). Sin cambio de estado — ocurre después de la regularización completa (estado Regularizado o Cerrado).
- `AlertaPlazoAnticipoVencido` es evento informativo sin cambio de estado `[R04b]`. Aplica en Vigente o Pagado (`saldoPorRegularizar()` > 0).

### 4.4. Devolucion

```
┌──────────┐                      ┌──────────┐                    ┌──────────┐
│          │  DevolucionConfirmada │          │  DevolucionCausada │          │
│Pendiente │─────────────────────►│Confirmada│───────────────────►│ Causada  │ ■
│          │                      │          │                    │          │
└──────────┘                      └──────────┘                    └──────────┘
```

**Notas:**
- `Pendiente` es el estado inicial para toda devolución radicada.
- `Confirmada`: la devolución ha sido validada. En este momento el `ServicioDeAplicacionDevolucion` coordina la aplicación del crédito contra el agregado OXP origen. Los efectos dependen del tipo de OXP (ver ServicioDeAplicacionDevolucion, Sección 3).
- `Causada ■`: nota crédito registrada en SincoA&F. Estado terminal. La causación informa al sistema contable lo que ya ocurrió en la confirmación.
- No aplica estado `Devuelta` (no se devuelve una devolución).
- La máquina de estados es la misma independiente del tipo de OXP. Lo que cambia son los efectos de la confirmación.

---

## 5. Catálogo de eventos

### 5.1. Radicación

#### OxpComercioRadicada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una obligación individual (compra) realizada con tarjeta corporativa ha sido registrada en el sistema con sus soportes documentales. |
| **Agregado** | OxpComercio |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Pendiente. Si `[R02]` está configurada como automática: Confirmada. |
| **Precondiciones** | Soporte documental adjunto (PDF, imagen o XML). Si es XML, datos extraídos de SincoRE. Validación de unicidad superada `[R26]`. |
| **Información capturada** | Tercero (NIT, razón social), fecha de transacción, valor en moneda original, moneda, TRM del día si aplica `[R05b]`, valor en moneda funcional, número de soporte/factura, medio de pago (tarjeta), conceptos (gasto/costo + impuestos + retenciones), distribución de costos si aplica `[R05c]`, soportes documentales adjuntos. |
| **Efectos** | Si XML: extracción automática de datos desde SincoRE. Si requiere formalización: notificación a SincoADPRO `[R20]`. Si supera monto máximo: alerta informativa `[R05]`. Si compra del exterior o sujeto no obligado a facturar: alerta de plazo DIAN para Documento Soporte en Adquisiciones (6 días hábiles) `[R01]` — el documento lo emite SincoFE; OXP controla que haya sido emitido. Si `[R02]` automática: emite `OxpComercioConfirmada`. |

#### ExtractoRadicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El extracto bancario del período ha sido cargado al sistema y sus partidas han sido extraídas. |
| **Agregado** | OxpExtracto |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Pendiente. |
| **Precondiciones** | Archivo de extracto válido (PDF o CSV). |
| **Información capturada** | Entidad bancaria, tarjeta, período, partidas del extracto (descripción, valor, fecha por cada una), cargos adicionales detectados según configuración por tarjeta `[R06]` `[R19]`. |
| **Efectos** | Emite `CargosAdicionalesExtraidos` si se detectan cargos configurados. Extracto disponible para conciliación. |

#### CargosAdicionalesExtraidos

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Los cargos financieros del extracto (4x1000, cuota de manejo, intereses) han sido detectados y registrados como conceptos de la OXP. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Radicación del extracto en curso. |
| **Estado resultante** | (sin cambio de estado; conceptos agregados al extracto). |
| **Precondiciones** | Configuración por tarjeta define cuáles cargos adicionales maneja `[R06]` `[R19]`. |
| **Información capturada** | Tipo de cargo (4x1000, cuota de manejo, intereses), valor, período. Intereses aplican solo para tarjeta de crédito. |
| **Efectos** | Cargos se incluyen en la OXP para que el valor a pagar coincida exactamente con el extracto. No requieren OxpComercio. No generan anticipos. Se consideran conciliados automáticamente `[R06]`. |

#### DistribucionDeCostosConfigurada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Los componentes de una OxpComercio han sido distribuidos entre múltiples destinos de negocio. |
| **Agregado** | OxpComercio |
| **Estado previo** | Radicación en curso. |
| **Estado resultante** | (sin cambio de estado; modifica estructura interna de instrucciones de distribución). |
| **Precondiciones** | Suma de cada instrucción de distribución = 100% `[R05c]` (Invariante I2). |
| **Información capturada** | N destinos de negocio (`DestinoDeNegocio`), porcentaje por destino, unidad organizacional por cada distribución. Componente referenciado (`ConceptoDeGasto` o `Tributo` específico). |
| **Efectos** | Las instrucciones de distribución se incorporan al agregado según cadena de resolución (Sección 3). `lineasParaTraduccion()` generará N líneas por componente distribuido. |

---

### 5.2. Anticipo

#### AnticipoRegistrado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se ha registrado un pago adelantado al tercero. Puede contar con soporte documental preliminar (ej: cuenta de cobro) o no. Puede ya haberse pagado (partida visible en extracto) o estar pendiente de pago. |
| **Agregado** | Anticipo |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Vigente. |
| **Precondiciones** | Usuario con perfil habilitado para generar anticipos `[R22]`. Si no hay soporte: justificación obligatoria `[R03]`. |
| **Información capturada** | Tercero (NIT, razón social), valor del anticipo, valorTotal (inicialmente igual al valor anticipo), medio de pago, fecha de transacción. Si hay soporte: soporte documental (ej: cuenta de cobro). Si no hay soporte: justificación de ausencia. |
| **Efectos** | Inicia conteo de plazo para regularización `[R04b]` (default 30 días). Anticipo disponible para vinculación con partida de extracto `[R08]` o para vinculación de pago directo. Regularización futura con OxpComercio en ambos casos. Los dos tipos de cruce (extracto y pago directo) pueden coexistir sobre el mismo anticipo. |

#### AnticipoRegularizado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una OxpComercio vinculada aporta el **soporte formal definitivo** (factura), reduciendo el saldo por regularizar del anticipo. |
| **Agregado** | Anticipo |
| **Estado previo** | Vigente o Pagado. |
| **Estado resultante** | Vigente o Pagado (reduce `saldoPorRegularizar()`). Si `saldoPorRegularizar()` = 0: transiciona a Regularizado (o Cerrado si ya estaba Pagado). |
| **Precondiciones** | Anticipo en estado no terminal (ni Cerrado ni Reversado). OxpComercio del mismo tercero ⚠️ (ver ServicioDeRegularizacion — momento exacto pendiente de precisar). `saldoPorRegularizar()` suficiente para el monto a regularizar. Coordinado por `ServicioDeRegularizacion`. |
| **Información capturada** | Referencia a OxpComercio vinculada, monto regularizado, fecha. |
| **Efectos** | Crea entidad `CruceRegularizacionAplicada` en el agregado Anticipo. Reduce `saldoPorRegularizar()`. Genera información estructurada para amortización contable `[R15]`. Si `saldoPorRegularizar()` = 0: emite `RegularizacionDeAnticipoCompletada`. |

#### AnticipoAmortizado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable externo (SincoA&F) ha confirmado la reclasificación contable del saldo del anticipo a cuentas de gasto o costo definitivas. |
| **Agregado** | Anticipo |
| **Estado previo** | Cerrado o Regularizado (sin cambio de estado — es confirmación de efecto contable externo posterior a la regularización completa). |
| **Estado resultante** | (sin cambio de estado). |
| **Precondiciones** | Anticipo con regularización completa (`saldoPorRegularizar()` = 0). SincoA&F ha procesado la reclasificación contable a partir de la información entregada por OXP. |
| **Información capturada** | Número de asiento de amortización, fecha de amortización. |
| **Efectos** | Cierra el ciclo contable del anticipo. |

#### AnticipoVinculadoAPartida

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una partida del extracto ha sido cubierta por este anticipo. Contraparte del evento `PartidaCubiertaPorAnticipo` emitido sobre el stream del OxpExtracto. Registra el lado Anticipo de la operación de cobertura. Puede coexistir con cruces tipo pago directo sobre el mismo anticipo. |
| **Agregado** | Anticipo |
| **Estado previo** | Vigente o Regularizado. |
| **Estado resultante** | Vigente o Regularizado (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: transiciona a Pagado (o Cerrado si ya estaba Regularizado). |
| **Precondiciones** | Anticipo en estado no terminal (ni Cerrado ni Reversado). Mismo tercero. Partida del extracto en estado pendiente `[R08]`. `saldoPorPagar()` suficiente para el valor cubierto. |
| **Información capturada** | Referencia a OxpExtracto, referencia a PartidaExtracto, valor cubierto. |
| **Efectos** | Crea entidad `CrucePagoAplicado` (tipo: extracto) en el agregado Anticipo. Reduce `saldoPorPagar()`. Si `saldoPorPagar()` = 0: emite `AnticipoPagado`. |

#### PagoAnticipoAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | SincoA&F ha confirmado un pago parcial o total del valor total del anticipo por vía diferente a tarjeta de crédito. Puede coexistir con cruces tipo extracto sobre el mismo anticipo. |
| **Agregado** | Anticipo |
| **Estado previo** | Vigente o Regularizado. |
| **Estado resultante** | Vigente o Regularizado (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: transiciona a Pagado (o Cerrado si ya estaba Regularizado). |
| **Precondiciones** | Anticipo en estado no terminal (ni Cerrado ni Reversado). SincoA&F confirma el pago. `saldoPorPagar()` suficiente para el monto pagado. |
| **Información capturada** | Referencia de pago de SincoA&F, valor pagado, fecha. |
| **Efectos** | Crea entidad `CrucePagoAplicado` (tipo: pago_directo) en el agregado Anticipo. Reduce `saldoPorPagar()`. Si `saldoPorPagar()` = 0: emite `AnticipoPagado`. |

#### AnticipoPagado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El valor total del anticipo ha sido completamente cubierto. El pago pudo realizarse mediante partida(s) de extracto (TC), pago directo confirmado por SincoA&F (forma de pago diferente a TC), devolución (anticipo nacido de `ServicioDeAplicacionDevolucion`), o una combinación de estos. |
| **Agregado** | Anticipo |
| **Estado previo** | Vigente o Regularizado. |
| **Estado resultante** | Pagado. Si ya estaba Regularizado: Cerrado (estado terminal). |
| **Precondiciones** | `saldoPorPagar()` = 0. |
| **Información capturada** | Total de cruces de pago aplicados (cantidad, suma de valores, detalle por tipo extracto/pago_directo/devolucion), fecha de cierre de la dimensión de pago. |
| **Efectos** | Transiciona a Pagado. Si el anticipo ya estaba Regularizado (`saldoPorRegularizar()` = 0): transiciona directamente a Cerrado. |

#### RegularizacionDeAnticipoCompletada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El valor anticipo ha sido completamente regularizado mediante OxpComercio con soporte documental formal (factura). |
| **Agregado** | Anticipo |
| **Estado previo** | Vigente o Pagado. |
| **Estado resultante** | Regularizado. Si ya estaba Pagado: Cerrado (estado terminal). |
| **Precondiciones** | `saldoPorRegularizar()` = 0. |
| **Información capturada** | Total de cruces de regularización aplicados (cantidad, suma de montos regularizados, referencias a OxpComercio), fecha de cierre de la dimensión de regularización. |
| **Efectos** | Transiciona a Regularizado. Si el anticipo ya estaba Pagado (`saldoPorPagar()` = 0): transiciona directamente a Cerrado. |

#### AnticipoReversado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El anticipo ha sido completamente reversado por error (proveedor incorrecto o valor incorrecto). Ambos saldos llevados a cero vía cruces tipo reversa. |
| **Agregado** | Anticipo |
| **Estado previo** | Vigente. |
| **Estado resultante** | Reversado (estado terminal). |
| **Precondiciones** | Anticipo en estado Vigente. Sin `CrucePagoAplicado` previos (`saldoPorPagar` = valorTotal). Sin `CruceRegularizacionAplicada` previos (`saldoPorRegularizar` = valorAnticipo). Coordinado por `ServicioDeAplicacionDevolucion` (Rama Anticipo). |
| **Información capturada** | Referencia a Devolucion tipo Anticipo que origina la reversión, `CrucePagoAplicado` tipo reversa (valor = valorTotal), `CruceRegularizacionAplicada` tipo reversa (valor = valorAnticipo), motivo, fecha. |
| **Efectos** | Crea `CrucePagoAplicado` tipo reversa y `CruceRegularizacionAplicada` tipo reversa. `saldoPorPagar()` = 0, `saldoPorRegularizar()` = 0. Transiciona a Reversado. Estado terminal — no se pueden recibir más cruces ni regularizaciones. |

#### AlertaPlazoAnticipoVencido

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El anticipo no ha sido regularizado dentro del plazo configurado. |
| **Agregado** | Anticipo |
| **Estado previo** | Vigente o Pagado. |
| **Estado resultante** | (sin cambio de estado; es evento informativo). |
| **Precondiciones** | Anticipo con `saldoPorRegularizar()` > 0. Plazo configurado excedido `[R04b]` (default 30 días, configurable por empresa). |
| **Información capturada** | Días de retraso, saldo pendiente de regularización, fecha límite original. |
| **Efectos** | Notificación a usuarios responsables. |

---

### 5.3. Conciliación

#### ConciliacionIniciada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El proceso de conciliación entre las partidas del extracto y las OxpComercio registradas ha comenzado. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Pendiente. |
| **Estado resultante** | Parcialmente Conciliado. |
| **Precondiciones** | Extracto radicado. Existen instancias de OxpComercio disponibles para vinculación. |
| **Información capturada** | Fecha de inicio de conciliación, partidas que el sistema propone automáticamente (basado en patrones aprendidos `[R09]` y criterios de comercio, valor y fecha). |
| **Efectos** | Inicia conteo de plazo de conciliación `[R07]`. Aplica conciliación automática con patrones persistidos `[R09]`. Cada match exitoso dispara `ServicioDeConciliacion` que emite `VinculacionRealizada` + `PagoOxpComercioViaExtractoAplicado`. |

#### VinculacionRealizada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una partida del extracto ha sido vinculada con una o más OxpComercio. Este evento registra el lado Extracto de la operación de conciliación coordinada por `ServicioDeConciliacion`. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliado. |
| **Estado resultante** | Parcialmente Conciliado. Si el 100% de partidas quedan resueltas, el agregado emite `ExtractoConciliado` automáticamente. |
| **Precondiciones** | OxpComercio causada(s). Tipo de vinculación válido: 1:1 o N:1. Si N:1: suma de OxpComercio dentro de tolerancia `[R10]`. Coordinado por `ServicioDeConciliacion`. |
| **Información capturada** | Tipo de vinculación (1:1 o N:1), referencia(s) a OxpComercio vinculada(s), partida del extracto, valor de diferencia (si existe), origen (automática o manual). |
| **Efectos** | `ServicioDeConciliacion` emite simultáneamente `PagoOxpComercioViaExtractoAplicado` sobre cada OxpComercio vinculada (crea `PagoAplicado` tipo extracto, reduce `saldoPorPagar()`). Si diferencia dentro de tolerancia: emite `AjustePorToleranciaGenerado` `[R10]`. Si OxpComercio en moneda extranjera con diferencia de TRM: emite `DiferenciaEnCambioDetectada` `[R10b]`. Persiste asociación de patrón comercio-descripción `[R09]`. |

#### DiferenciaEnCambioDetectada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Durante la conciliación, se ha detectado que la TRM de radicación difiere de la TRM del extracto para una OxpComercio en moneda extranjera. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Conciliación en curso, OxpComercio en moneda extranjera siendo vinculada. |
| **Estado resultante** | (sin cambio de estado; genera concepto de ajuste). |
| **Precondiciones** | Diferencia entre valor radicado (TRM transacción) y valor en extracto (TRM corte) `[R10b]`. |
| **Información capturada** | OxpComercio de origen, TRM de radicación, TRM del extracto, valor de la diferencia, clasificación (gasto o ingreso financiero). |
| **Efectos** | Emite `ConceptoAjusteDiferenciaEnCambioGenerado`. |

#### ConceptoAjusteDiferenciaEnCambioGenerado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se ha generado un concepto de ajuste por diferencia en cambio sobre el OxpExtracto, permitiendo el cruce exacto sin crear una nueva OXP. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Conciliación en curso. |
| **Estado resultante** | (sin cambio de estado; concepto agregado al extracto). |
| **Precondiciones** | `DiferenciaEnCambioDetectada` emitida previamente. |
| **Información capturada** | Referencia a OxpComercio origen, valor del ajuste, tipo (gasto financiero si TRM subió, ingreso financiero si TRM bajó). |
| **Efectos** | Concepto incluido en el OxpExtracto. Se causa junto con el extracto. Un concepto por cada OxpComercio en moneda extranjera con diferencia `[R10b]`. |

#### AjustePorToleranciaGenerado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se ha generado un ajuste por tolerancia sobre el OxpExtracto, registrando la diferencia menor entre el valor de la partida del extracto y la OxpComercio vinculada. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Conciliación en curso. |
| **Estado resultante** | (sin cambio de estado; ajuste agregado al extracto). |
| **Precondiciones** | Diferencia entre valor de partida del extracto y OxpComercio vinculada dentro de tolerancia configurada `[R10]`. |
| **Información capturada** | Referencia a OxpComercio origen, valor de la diferencia, dirección (extracto mayor o menor que OxpComercio). |
| **Efectos** | Ajuste incluido en el OxpExtracto. Se causa junto con el extracto. Dirección determina clasificación: gasto bancario (extracto > OxpComercio) o aprovechamiento bancario (extracto < OxpComercio) `[R10]`. |

#### PartidaCubiertaPorAnticipo

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una partida del extracto sin OxpComercio asociada ha sido cubierta por un anticipo, permitiendo avanzar en la conciliación sin generar una nueva OxpComercio. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliado. |
| **Estado resultante** | Parcialmente Conciliado. Si el 100% de partidas quedan resueltas, el agregado emite `ExtractoConciliado` automáticamente. |
| **Precondiciones** | Anticipo vigente para el mismo tercero. Partida en estado pendiente `[R08]`. |
| **Información capturada** | Referencia al Anticipo, partida del extracto cubierta. |
| **Efectos** | Partida transiciona a estado `anticipo`. Se crea entidad `CoberturaAnticipo` en el agregado. Cuenta como resuelta para invariante I3 (completitud de conciliación). El vínculo anticipo-partida es permanente. |

#### PartidaCubiertaPorDevolucion

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una partida del extracto que representa un retorno de dinero ha sido cubierta por una Devolucion. Permite avanzar en la conciliación. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliado. |
| **Estado resultante** | Parcialmente Conciliado. Si el 100% de partidas quedan resueltas, el agregado emite `ExtractoConciliado` automáticamente. |
| **Precondiciones** | Devolucion existente (tipo Comercio) o nueva (tipo Extracto) para el mismo tercero. Partida en estado pendiente. Coordinado por `ServicioDeConciliacion`. |
| **Información capturada** | Referencia a Devolucion, partida del extracto cubierta. |
| **Efectos** | Partida transiciona a estado `devolucion`. Se crea entidad `CoberturaDevolucion` en el agregado. Cuenta como resuelta para invariante I3 (completitud de conciliación). El vínculo devolucion-partida es permanente. |

#### ExtractoConciliado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El 100% de las partidas del extracto han sido vinculadas a OxpComercio, cubiertas por anticipo, cubiertas por devolución, descartadas, clasificadas como cargos adicionales, o marcadas como partida en disputa. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliado. |
| **Estado resultante** | Conciliado. |
| **Precondiciones** | 100% de partidas resueltas `[R06]`. |
| **Información capturada** | Resumen de conciliación: total de partidas, partidas automáticas vs. manuales, partidas en disputa, partidas cubiertas por anticipo, partidas cubiertas por devolución, conceptos de ajuste generados. |
| **Efectos** | Habilita la transición hacia confirmación. |

#### PartidaEnDisputaMarcada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una partida del extracto ha sido marcada como disputa por no poder conciliarse debido a errores bancarios, fraudes potenciales o transacciones no reconocidas. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliado. |
| **Estado resultante** | (sin cambio de estado del extracto; partida marcada internamente). |
| **Precondiciones** | Partida del extracto sin OxpComercio asociada. Decisión del usuario o del Autorizador. |
| **Información capturada** | Partida del extracto afectada, motivo de la disputa (error bancario, fraude potencial, no reconocida), usuario que marca, fecha. |
| **Efectos** | La partida cuenta como conciliada para alcanzar el 100% `[R06]`. Permite avanzar sin generar anticipos. Resolución posterior vía `PartidaDisputaDescartada` o `PartidaDisputaReclasificada`. |

#### PartidaDisputaDescartada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La partida en disputa ha sido descartada porque el banco reversó la transacción. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Partida marcada como disputa. |
| **Estado resultante** | Partida transiciona a estado `descartada` (compensada contra reverso bancario). |
| **Precondiciones** | Línea de "Reverso Bancario" identificada en un extracto (puede ser de un período futuro) `[R06b]` `[R10c]`. |
| **Información capturada** | Referencia al extracto y línea de reverso bancario, fecha de resolución. |
| **Efectos** | Cierra el ciclo de la partida en disputa. |

#### PartidaDisputaReclasificada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se ha identificado el gasto real detrás de la partida en disputa y se ha vinculado con una OxpComercio radicada, mediante reclasificación contable. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Partida marcada como disputa. |
| **Estado resultante** | Partida transiciona a estado `vinculada` (reclasificada a OxpComercio). |
| **Precondiciones** | OxpComercio correspondiente radicada y disponible `[R06b]`. |
| **Información capturada** | Referencia a la nueva OxpComercio, reclasificación contable aplicada. |
| **Efectos** | Sistema vincula la partida del extracto original con la nueva OxpComercio. Sin generar documentos duplicados ni nueva deuda `[R06b]`. |

#### AlertaConciliacionPlazoVencido

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La conciliación no ha sido completada dentro del plazo configurado previo a la fecha de pago. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliado. |
| **Estado resultante** | (sin cambio de estado; es evento informativo). |
| **Precondiciones** | Plazo configurado excedido `[R07]` (default 3 días previos a fecha de pago, configurable por tarjeta). |
| **Información capturada** | Días de retraso, partidas pendientes de conciliar, fecha límite original. |
| **Efectos** | Notificación a usuarios responsables. |

---

### 5.4. Confirmación y Causación

#### OxpComercioConfirmada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La OXP ha sido validada y aprobada para causación contable. |
| **Agregado** | OxpComercio |
| **Estado previo** | Pendiente. |
| **Estado resultante** | Confirmada. |
| **Precondiciones** | Usuario con rol de Confirmador `[R23]`. Confirmador diferente al Radicador `[R25]`. OXP en estado Pendiente con soportes completos. |
| **Información capturada** | Usuario confirmador, fecha y hora de confirmación. |
| **Efectos** | Habilita la transición hacia causación. Si `[R12]` está configurada como automática: emite `OxpComercioCausada`. |

#### OxpComercioDevuelta

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El confirmador ha rechazado la OXP y la devuelve al radicador para corrección. |
| **Agregado** | OxpComercio |
| **Estado previo** | Pendiente. |
| **Estado resultante** | Devuelta. |
| **Precondiciones** | Usuario con rol de Confirmador `[R11b]`. |
| **Información capturada** | Motivo de rechazo (obligatorio), usuario confirmador, fecha de rechazo. |
| **Efectos** | OXP retorna a la bandeja del radicador. Radicador puede corregir (emite `OxpComercioCorregida`) o descartar. |

#### OxpComercioCorregida

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El radicador ha corregido la OXP previamente rechazada y la reenvía a confirmación. |
| **Agregado** | OxpComercio |
| **Estado previo** | Devuelta. |
| **Estado resultante** | Pendiente. |
| **Precondiciones** | OXP en estado Devuelta. Radicador ha modificado los datos según el motivo de rechazo. |
| **Información capturada** | Datos corregidos, referencia al rechazo previo (trazabilidad). |
| **Efectos** | OXP vuelve a flujo de confirmación. |

#### OxpComercioCausada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable externo (SincoA&F) ha confirmado el registro exitoso de la causación. |
| **Agregado** | OxpComercio |
| **Estado previo** | Confirmada. |
| **Estado resultante** | Causada. |
| **Precondiciones** | OXP confirmada. SincoA&F confirma registro exitoso de la causación enviada `[R13]`. |
| **Información capturada** | Número de asiento contable externo, fecha de causación (fecha del soporte/factura, principio de devengo). |
| **Efectos** | Integración saliente: causación individual enviada a SincoA&F (JSON). Si la OxpComercio regulariza un anticipo: la información de amortización se incluye en la integración saliente `[R15]`. OxpComercio disponible para recibir pagos (`saldoPorPagar()` > 0): vía extracto (conciliación), vía anticipo (regularización), pago directo (SincoA&F), o devolución (`ServicioDeAplicacionDevolucion`). |

#### ExtractoConfirmado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El extracto conciliado ha sido validado y aprobado para causación contable. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Conciliado. |
| **Estado resultante** | Confirmado. |
| **Precondiciones** | Extracto en estado Conciliado (100%) `[R11]`. Usuario con rol de Confirmador `[R23]`. |
| **Información capturada** | Usuario confirmador, fecha y hora de confirmación. |
| **Efectos** | Habilita la transición hacia causación. Si `[R12]` está configurada como automática: emite `ExtractoCausado`. |

#### ExtractoCausado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable externo (SincoA&F) ha confirmado el registro exitoso de la causación del extracto. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Confirmado. |
| **Estado resultante** | Causado. |
| **Precondiciones** | Extracto confirmado. SincoA&F confirma registro exitoso `[R14]`. |
| **Información capturada** | Número de asiento contable externo, fecha de causación (fecha de compensación), conceptos causados (incluye cargos adicionales y ajustes). |
| **Efectos** | Integración saliente: causación de OxpExtracto enviada a SincoA&F (JSON). Registra el total del extracto contra la entidad bancaria, incluyendo cargos adicionales. |

---

### 5.5. Pago

#### PagoOxpComercioViaExtractoAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La OxpComercio ha recibido un pago parcial o total vía vinculación con una partida del extracto durante la conciliación. Evento de progreso — reduce `saldoPorPagar()` sin cambiar de estado. Emitido por `ServicioDeConciliacion` como contraparte de `VinculacionRealizada`. Puede coexistir con pagos tipo anticipo y pago directo sobre la misma OxpComercio. |
| **Agregado** | OxpComercio |
| **Estado previo** | Causada. |
| **Estado resultante** | Causada (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: emite `OxpComercioPagada`. |
| **Precondiciones** | OxpComercio causada. Coordinado por `ServicioDeConciliacion`. Vinculación existente con partida de extracto (1:1 o N:1). `saldoPorPagar()` suficiente para el valor cubierto. |
| **Información capturada** | Referencia a OxpExtracto, partida del extracto vinculada, tipo de vinculación (1:1 o N:1), valor cubierto. |
| **Efectos** | Crea entidad `PagoAplicado` (tipo: extracto). Reduce `saldoPorPagar()`. El agregado genera `lineasParaTraduccion()` como insumo que el servicio de Traducción Contable transforma y entrega a SincoA&F `[R17]`. Si la OxpComercio es en moneda extranjera y existe diferencia de TRM: emite `DiferenciaEnCambioDetectada` sobre el OxpExtracto `[R10b]`. Si la diferencia está dentro de tolerancia `[R10]`: emite `AjustePorToleranciaGenerado` sobre el OxpExtracto. |

#### PagoOxpComercioViaAnticipoAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El anticipo que regulariza esta OxpComercio cubre parcial o totalmente el valor por pagar. Evento de progreso — reduce `saldoPorPagar()` sin cambiar de estado. Emitido por `ServicioDeRegularizacion`. Puede coexistir con pagos tipo extracto y pago directo sobre la misma OxpComercio. |
| **Agregado** | OxpComercio |
| **Estado previo** | Causada. |
| **Estado resultante** | Causada (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: emite `OxpComercioPagada`. |
| **Precondiciones** | OxpComercio causada ⚠️ (ver I16). Anticipo en estado no terminal (ni Cerrado ni Reversado), vinculado vía regularización. `saldoPorPagar()` suficiente para el monto cubierto. Coordinado por `ServicioDeRegularizacion`. |
| **Información capturada** | Referencia a Anticipo, monto cubierto por anticipo, fecha. |
| **Efectos** | Crea entidad `PagoAplicado` (tipo: anticipo). Reduce `saldoPorPagar()`. |

#### PagoOxpComercioDirectoAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | SincoA&F ha confirmado un pago parcial o total del valor de la OxpComercio por vía directa (sin pasar por extracto ni anticipo). Evento de progreso — reduce `saldoPorPagar()` sin cambiar de estado. Puede coexistir con pagos tipo extracto y anticipo sobre la misma OxpComercio. |
| **Agregado** | OxpComercio |
| **Estado previo** | Causada. |
| **Estado resultante** | Causada (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: emite `OxpComercioPagada`. |
| **Precondiciones** | OxpComercio causada. SincoA&F confirma el pago. `saldoPorPagar()` suficiente para el monto pagado. |
| **Información capturada** | Referencia de pago de SincoA&F, valor pagado, fecha. |
| **Efectos** | Crea entidad `PagoAplicado` (tipo: pago_directo). Reduce `saldoPorPagar()`. |

#### OxpComercioPagada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La obligación ha sido liquidada financieramente. `saldoPorPagar()` = 0. El pago pudo realizarse mediante extracto (conciliación), anticipo (regularización), pago directo (SincoA&F), devolución (`ServicioDeAplicacionDevolucion`), o una combinación de estos. |
| **Agregado** | OxpComercio |
| **Estado previo** | Causada. |
| **Estado resultante** | Pagada (estado terminal). |
| **Precondiciones** | `saldoPorPagar()` = 0. |
| **Información capturada** | Total de pagos aplicados (cantidad, suma de valores, detalle por tipo extracto/anticipo/pago_directo/devolucion), fecha de cierre. |
| **Efectos** | Transiciona a Pagada. Cierre operativo de la OxpComercio. |

#### PagoExtractoAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | SincoA&F ha confirmado un pago parcial o total contra el extracto. Evento de progreso — reduce `saldoPorPagar()` del OxpExtracto sin cambiar de estado. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Causado. |
| **Estado resultante** | Causado (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: emite `ExtractoPagado`. |
| **Precondiciones** | Extracto causado. SincoA&F confirma pago. `saldoPorPagar()` suficiente para el monto pagado. |
| **Información capturada** | Referencia de pago de SincoA&F, monto pagado, fecha. |
| **Efectos** | Crea entidad `CrucePagoExtractoAplicado` (tipo: pago_sincoa). Reduce `saldoPorPagar()`. |

#### PagoExtractoViaDevolucionAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una devolución confirmada (tipo Extracto) cubre parcial o totalmente el valor por pagar del extracto. Evento de progreso — reduce `saldoPorPagar()` del OxpExtracto sin cambiar de estado. Emitido por `ServicioDeAplicacionDevolucion`. Puede coexistir con pagos tipo pago_sincoa sobre el mismo extracto. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Causado. |
| **Estado resultante** | Causado (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: emite `ExtractoPagado`. |
| **Precondiciones** | Devolucion tipo Extracto en confirmación. `saldoPorPagar()` suficiente para el monto cubierto. Coordinado por `ServicioDeAplicacionDevolucion`. |
| **Información capturada** | Referencia a Devolucion, monto cubierto, fecha. |
| **Efectos** | Crea entidad `CrucePagoExtractoAplicado` (tipo: devolucion). Reduce `saldoPorPagar()`. |

#### PagoOxpComercioViaDevolucionAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una devolución confirmada cubre parcial o totalmente el valor por pagar de la OxpComercio. Evento de progreso — reduce `saldoPorPagar()` sin cambiar de estado. Emitido por `ServicioDeAplicacionDevolucion` como parte de la confirmación de la devolución. Puede coexistir con pagos tipo extracto, anticipo y pago directo sobre la misma OxpComercio. |
| **Agregado** | OxpComercio |
| **Estado previo** | Causada. |
| **Estado resultante** | Causada (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: emite `OxpComercioPagada`. |
| **Precondiciones** | Devolucion en confirmación referenciando esta OxpComercio. `saldoPorPagar()` suficiente para el monto cubierto. Coordinado por `ServicioDeAplicacionDevolucion`. |
| **Información capturada** | Referencia a Devolucion, monto cubierto por devolución, fecha. |
| **Efectos** | Crea entidad `PagoAplicado` (tipo: devolucion). Reduce `saldoPorPagar()`. |

#### ExtractoPagado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El extracto ha sido completamente pagado. `saldoPorPagar()` = 0. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Causado. |
| **Estado resultante** | Pagado (estado terminal). |
| **Precondiciones** | `saldoPorPagar()` = 0. |
| **Información capturada** | Total de pagos confirmados (cantidad, suma de valores), fecha de cierre. |
| **Efectos** | Cierre operativo del ciclo del OxpExtracto. |

### 5.6. Devolucion

#### DevolucionRadicada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una devolución ha sido registrada en el sistema contra un agregado OXP origen (OxpComercio, OxpExtracto o Anticipo). Valores positivos representando la magnitud del crédito (D19). |
| **Agregado** | Devolucion |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Pendiente. |
| **Precondiciones** | **Comercio:** OxpComercio existe. Mismo tercero (NIT). `valorNeto(devolucion)` ≤ `valorNeto(OxpComercio)`. Acumulado I17. OxpComercio en Causada o Pagada. Soporte documental adjunto. **Extracto:** OxpExtracto existe. `valorNeto(devolucion)` ≤ saldoPorPagar(OxpExtracto). OxpExtracto en estado Causado. **Anticipo:** Anticipo existe. Anticipo en estado Vigente. `saldoPorPagar()` = valorTotal (sin cruces de pago). `saldoPorRegularizar()` = valorAnticipo (sin cruces de regularización). `valorNeto(devolucion)` = valorTotal del anticipo (solo reversa total). Mismo tercero. |
| **Información capturada** | Referencia a OXP origen (tipo + ID, obligatoria, inmutable), tercero (NIT, razón social), entidades internas según tipo de OXP: `ConceptoDevuelto`(s) para Comercio, `CargoFinancieroDevuelto`(s) para Extracto, `ReversaTotal` para Anticipo. Soportes documentales adjuntos. **Comercio:** adicionalmente moneda, TRM, distribución de costos. |
| **Efectos** | Devolución disponible para confirmación. |

#### DevolucionConfirmada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La devolución ha sido validada y el crédito ha sido aplicado contra el agregado OXP origen. Coordinado por `ServicioDeAplicacionDevolucion`. Los efectos dependen del tipo de OXP. |
| **Agregado** | Devolucion |
| **Estado previo** | Pendiente. |
| **Estado resultante** | Confirmada. |
| **Precondiciones** | Devolucion en estado Pendiente. Agregado OXP origen existe y cumple precondiciones según tipo (ver `DevolucionRadicada`). |
| **Información capturada** | Resultado de la aplicación según tipo de OXP: monto aplicado, referencia a Anticipo creado (si aplica, solo Comercio), fecha de confirmación. |
| **Efectos** | **Comercio:** Si `saldoPorPagar(OXP) > 0`: emite `PagoOxpComercioViaDevolucionAplicado` → stream OxpComercio. Si `saldoPorPagar(OXP) = 0`: crea nuevo Anticipo (valorTotal = valorNeto(devolucion), dimensión pago resuelta → estado Pagado, `saldoPorRegularizar()` pendiente). **Extracto:** Emite `PagoExtractoViaDevolucionAplicado` → stream OxpExtracto. **Anticipo:** Emite `AnticipoReversado` → stream Anticipo (crea `CrucePagoAplicado` tipo reversa y `CruceRegularizacionAplicada` tipo reversa, saldos → 0, estado → Reversado). |

#### DevolucionCausada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable externo (SincoA&F) ha confirmado el registro exitoso de la nota crédito correspondiente a esta devolución. Estado terminal. |
| **Agregado** | Devolucion |
| **Estado previo** | Confirmada. |
| **Estado resultante** | Causada (estado terminal). |
| **Precondiciones** | Devolucion confirmada. SincoA&F confirma registro exitoso de la nota crédito `[R16]`. |
| **Información capturada** | Número de asiento contable externo, fecha de causación. |
| **Efectos** | Integración saliente: nota crédito enviada a SincoA&F (JSON) `[R16]`. Incluye `lineasParaTraduccion()` con información de la aplicación realizada en la confirmación. |

---

## 6. Tipos de concepto

El agregado `OxpComercio` tiene una única entidad interna (`ConceptoDeGasto`) que contiene su desglose fiscal como Value Objects (`DesgloseFiscal` → `Tributo`). El agregado `Devolucion` tiene tres entidades polimórficas con contrato común (`descripcion`, `valor`): `ConceptoDevuelto` (Comercio), `CargoFinancieroDevuelto` (Extracto) y `ReversaTotal` (Anticipo). La distribución de costos se gestiona mediante instrucciones separadas a nivel del agregado (ver Sección 3, reglas de consistencia). OXP captura la información de negocio; la traducción a lenguaje contable es responsabilidad del servicio de **Traducción Contable** en la frontera OXP → SincoA&F.

### ConceptoDeGasto

| Aspecto | Detalle |
|---------|---------|
| **Componente** | Entidad interna de OxpComercio |
| **Clasificación** | Entidad (tiene identidad — puede haber duplicados con mismos atributos). |
| **Aparece en** | Radicación. |
| **Información (dominio OXP)** | Código, descripción, cantidad, valor. Contiene `DesgloseFiscal` con los tributos derivados. |
| **Distribución** | Gestionada por `InstruccionDistribucion` a nivel del agregado `[R05c]`. No vive dentro del concepto. |
| **Traducción contable (frontera)** | El servicio de Traducción Contable deduce cuenta de gasto o costo, centro de costo y naturaleza (débito) a partir del destino de negocio y reglas configuradas. |

### Tributo (Impuesto)

| Aspecto | Detalle |
|---------|---------|
| **Componente** | Value Object dentro de `DesgloseFiscal` de un `ConceptoDeGasto` |
| **Clasificación** | Value Object (inmutable — se reemplaza al recalcular). |
| **Aparece en** | Radicación. Derivado del `ConceptoDeGasto` al que pertenece. |
| **Información (dominio OXP)** | Tipo de impuesto (IVA, ICA, etc.), base gravable, tarifa, valor calculado. Determinado por el servicio transversal de cálculo de impuestos. |
| **Distribución** | Gestionada por `InstruccionDistribucion`. Por defecto hereda del `ConceptoDeGasto` padre; puede sobrescribirse. |
| **Traducción contable (frontera)** | El servicio de Traducción Contable deduce cuenta de impuesto descontable o gasto según normativa. |

### Tributo (Retención)

| Aspecto | Detalle |
|---------|---------|
| **Componente** | Value Object dentro de `DesgloseFiscal` de un `ConceptoDeGasto` |
| **Clasificación** | Value Object (inmutable — se reemplaza al recalcular). |
| **Aparece en** | Radicación. Derivado del `ConceptoDeGasto` al que pertenece. |
| **Información (dominio OXP)** | Tipo de retención (ReteFuente, ReteIVA, ReteICA), base, tarifa, valor retenido. |
| **Distribución** | Gestionada por `InstruccionDistribucion`. Por defecto hereda del `ConceptoDeGasto` padre; puede sobrescribirse. |
| **Traducción contable (frontera)** | El servicio de Traducción Contable deduce cuenta de retención por pagar. |

### AjustePorDiferenciaCambio

| Aspecto | Detalle |
|---------|---------|
| **Entidad del agregado** | OxpExtracto |
| **Tipo** | Entidad interna (una por cada OxpComercio en moneda extranjera con diferencia). |
| **Aparece en** | Conciliación (al vincular OxpComercio en moneda extranjera). |
| **Información (dominio OXP)** | OxpComercio de origen, TRM de radicación, TRM del extracto, valor de la diferencia, clasificación (gasto o ingreso financiero) `[R10b]`. |
| **Distribución** | Gestionada por `InstruccionDistribucion` a nivel del agregado OxpExtracto. |
| **Traducción contable (frontera)** | El servicio de Traducción Contable deduce gasto financiero por diferencia en cambio (si TRM subió) o ingreso financiero (si TRM bajó). |

### AjustePorTolerancia

| Aspecto | Detalle |
|---------|---------|
| **Entidad del agregado** | OxpExtracto |
| **Tipo** | Entidad interna (una por cada vinculación con diferencia dentro de tolerancia). |
| **Aparece en** | Conciliación (al vincular con diferencia dentro de tolerancia). |
| **Información (dominio OXP)** | OxpComercio de origen, valor de la diferencia, dirección (extracto mayor o menor que OxpComercio) `[R10]`. |
| **Distribución** | Gestionada por `InstruccionDistribucion` a nivel del agregado OxpExtracto. |
| **Evento creador** | `AjustePorToleranciaGenerado` (Sección 5.3). |
| **Traducción contable (frontera)** | El servicio de Traducción Contable deduce gastos bancarios (si extracto > OxpComercio) o aprovechamientos bancarios (si extracto < OxpComercio). |

### CargoFinanciero

| Aspecto | Detalle |
|---------|---------|
| **Entidad del agregado** | OxpExtracto |
| **Tipo** | Entidad interna. |
| **Aparece en** | Radicación del extracto. |
| **Subtipos** | 4x1000 (GMF): aplica ambos medios de pago. Cuota de manejo: aplica ambos medios de pago. Intereses: aplica únicamente tarjeta de crédito. |
| **Información (dominio OXP)** | Tipo de cargo, valor, período. Configurado por tarjeta `[R06]` `[R19]`. |
| **Distribución** | Gestionada por `InstruccionDistribucion` a nivel del agregado OxpExtracto. |
| **Traducción contable (frontera)** | El servicio de Traducción Contable deduce cuenta de gasto financiero según subtipo. |

### ConceptoDevuelto

| Aspecto | Detalle |
|---------|---------|
| **Componente** | Entidad interna de Devolucion (tipo Comercio) |
| **Clasificación** | Entidad (tiene identidad). 1..N por Devolucion. Contrato común: `descripcion`, `valor` (`ValorMonetario`). Valores positivos — magnitud del crédito (D19). |
| **Aparece en** | Radicación de devolución contra OxpComercio. |
| **Información (dominio OXP)** | descripcion, valor (`ValorMonetario`), codigo, cantidad, `DesgloseFiscal` (VO). |
| **Distribución** | Gestionada por `InstruccionDistribucion` a nivel del agregado `[R05c]`. |
| **Traducción contable (frontera)** | El servicio de Traducción Contable interpreta como nota crédito (D8). |

### CargoFinancieroDevuelto

| Aspecto | Detalle |
|---------|---------|
| **Componente** | Entidad interna de Devolucion (tipo Extracto) |
| **Clasificación** | Entidad (tiene identidad). 1..N por Devolucion. Contrato común: `descripcion`, `valor` (`ValorMonetario`). Valores positivos — magnitud del crédito (D19). Espejo de `CargoFinanciero`. |
| **Aparece en** | Radicación de devolución contra OxpExtracto (exclusivamente cargos financieros cobrados en un extracto anterior). |
| **Información (dominio OXP)** | descripcion, valor (`ValorMonetario`), referenciaCargoFinanciero (ref. al `CargoFinanciero` del OxpExtracto origen). |
| **Distribución** | Sin distribución propia — hereda del agregado OXP origen. |
| **Traducción contable (frontera)** | El servicio de Traducción Contable interpreta como devolución de cargo financiero (D8). |

### ReversaTotal

| Aspecto | Detalle |
|---------|---------|
| **Componente** | Entidad interna de Devolucion (tipo Anticipo) |
| **Clasificación** | Entidad (tiene identidad). Exactamente 1 por Devolucion. Contrato común: `descripcion`, `valor` (`ValorMonetario`). Valores positivos — magnitud del crédito (D19). |
| **Aparece en** | Radicación de devolución (reversa) contra Anticipo. |
| **Información (dominio OXP)** | descripcion, valor (`ValorMonetario`), motivoReversa (proveedor incorrecto \| valor incorrecto). |
| **Distribución** | Sin distribución propia — hereda del agregado OXP origen. |
| **Traducción contable (frontera)** | El servicio de Traducción Contable interpreta como reversión de anticipo (D8). |

---

## 7. Invariantes del dominio

Las invariantes son restricciones estructurales que deben ser verdaderas en todo momento del ciclo de vida del dominio. A diferencia de las reglas de negocio (R01–R27), que pueden ser configurables y tener excepciones, las invariantes son absolutas.

| # | Invariante | Agregado | Referencia |
|---|-----------|----------|------------|
| I1 | **Unicidad de obligación:** No pueden existir dos OxpComercio con el mismo NIT + número de soporte dentro de la ventana de 24 meses. | OxpComercio | `[R26]` |
| I2 | **Integridad de distribución:** Para cualquier `InstruccionDistribucion` de los agregados OxpComercio, OxpExtracto, Anticipo o Devolucion, la suma de sus `DestinoDeNegocio` es exactamente 100%. Se valida por cada instrucción individual, ya sea referenciando un `ConceptoDeGasto`, un `ConceptoDevuelto` (Devolucion tipo Comercio), un `Tributo`, un `CargoFinanciero`, un ajuste (diferencia cambio o tolerancia), o el valor global de un anticipo (sin desglose fiscal `[P1]`). | OxpComercio, OxpExtracto, Anticipo, Devolucion | `[R05c]` |
| I3 | **Completitud de conciliación:** Un OxpExtracto en estado Conciliado tiene el 100% de sus partidas vinculadas a OxpComercio, cubiertas por anticipo, cubiertas por devolución, descartadas, marcadas como disputa, o clasificadas como cargos adicionales. | OxpExtracto | `[R06]` |
| I4 | **Progresión de estados:** Cada agregado solo puede avanzar en su máquina de estados; no puede retroceder excepto por la transición explícita Devuelta → Pendiente en OxpComercio (vía corrección). Estados terminales válidos: Pagada (OxpComercio), Pagado (OxpExtracto), Cerrado y Reversado (Anticipo), Causada (Devolucion). Reversado no es retroceso — es una transición terminal desde Vigente por reversión total vía `ServicioDeAplicacionDevolucion`. | OxpComercio, OxpExtracto, Anticipo, Devolucion | — |
| I5 | **Consistencia de moneda:** Toda OxpComercio en moneda extranjera almacena tanto el valor en moneda de origen como el valor en moneda funcional. | OxpComercio | `[R05b]` |
| I6 | **Segregación de funciones:** El usuario que confirma una OXP no puede ser el mismo que la radicó (cuando está habilitada por empresa). | OxpComercio, OxpExtracto | `[R25]` |
| I7 | **Vinculación coherente:** Una OxpComercio solo puede estar vinculada a un único OxpExtracto. Un OxpExtracto puede tener N vinculaciones. La vinculación (conciliación) genera un `PagoAplicado` tipo extracto, pero no implica pago total — la OxpComercio puede tener pagos adicionales de otras fuentes. | Inter-agregado | — |
| I8 | **Causalidad de anticipo:** Un anticipo solo puede recibir cruces parciales si no ha alcanzado un estado terminal (Cerrado o Reversado). Cruces de pago (`CrucePagoAplicado`): permitidos en estado Vigente o Regularizado, `saldoPorPagar()` suficiente, mismo tercero para cobertura de partida. Cruces de regularización (`CruceRegularizacionAplicada`): permitidos en estado Vigente o Pagado, `saldoPorRegularizar()` suficiente. Cruces tipo `reversa`: exclusivos desde Vigente sin cruces previos — emitidos únicamente por `AnticipoReversado` como parte de la reversión total. Nota: el anticipo creado por `ServicioDeAplicacionDevolucion` recibe su `CrucePagoAplicado` (tipo devolucion) como efecto del registro inicial (no como operación posterior). | Anticipo | — |
| I9 | **Confirmación externa de causación:** Una OXP solo transiciona a estado Causada cuando el sistema externo (SincoA&F) confirma el registro. OXP no auto-declara la causación. | OxpComercio, OxpExtracto, Devolucion | — |
| I10 | **Consistencia de distribución:** Toda `InstruccionDistribucion` referencia un componente existente del agregado. **OxpComercio:** `ConceptoDeGasto` o `Tributo`; cadena de resolución: instrucción explícita → herencia del gasto padre → preferencia de empresa → destino único pendiente. **Devolucion tipo Comercio:** `ConceptoDevuelto` o `Tributo`; misma cadena de resolución. **OxpExtracto:** `CargoFinanciero`, `AjustePorDiferenciaCambio` o `AjustePorTolerancia`; sin herencia (cada componente tiene instrucción propia o preferencia de empresa). **Anticipo:** instrucción única sobre el valor global (sin desglose `[P1]`); preferencia de empresa o destino único pendiente. Al agregar, eliminar o recalcular componentes, el agregado mantiene la coherencia. | OxpComercio, OxpExtracto, Anticipo, Devolucion | `[R05c]` |
| I11 | **Saldos no negativos del Anticipo:** `saldoPorPagar()` ≥ 0 y `saldoPorRegularizar()` ≥ 0. La suma de los `CrucePagoAplicado` no puede superar el valorTotal. La suma de los `CruceRegularizacionAplicada` no puede superar el valor anticipo. | Anticipo | — |
| I12 | **Consistencia de estado del Anticipo:** Un anticipo en estado Vigente tiene `saldoPorPagar()` > 0 y `saldoPorRegularizar()` > 0. Un anticipo en estado Pagado tiene `saldoPorPagar()` = 0 y `saldoPorRegularizar()` > 0. Un anticipo en estado Regularizado tiene `saldoPorRegularizar()` = 0 y `saldoPorPagar()` > 0. Un anticipo en estado Cerrado tiene `saldoPorPagar()` = 0 y `saldoPorRegularizar()` = 0. Un anticipo en estado Reversado tiene `saldoPorPagar()` = 0 y `saldoPorRegularizar()` = 0, resueltos vía cruces tipo `reversa` (`CrucePagoAplicado` tipo reversa + `CruceRegularizacionAplicada` tipo reversa). Estado terminal por reversión total — no admite más cruces ni regularizaciones. Nota: un anticipo creado por `ServicioDeAplicacionDevolucion` nace con `CrucePagoAplicado` (tipo devolucion) como parte de su registro inicial, transicionando inmediatamente a Pagado. | Anticipo | — |
| I13 | **Saldos no negativos de OxpComercio:** `saldoPorPagar()` ≥ 0. La suma de los `PagoAplicado`.valor no puede superar `valorNeto()`. | OxpComercio | — |
| I14 | **Saldos no negativos de OxpExtracto:** `saldoPorPagar()` ≥ 0. La suma de los `CrucePagoExtractoAplicado`.valor no puede superar `valorTotalExtracto()`. | OxpExtracto | — |
| I15 | **Consistencia de estado de pago:** OxpComercio en estado Causada tiene `saldoPorPagar()` > 0. OxpComercio en estado Pagada tiene `saldoPorPagar()` = 0. OxpExtracto en estado Causado tiene `saldoPorPagar()` > 0. OxpExtracto en estado Pagado tiene `saldoPorPagar()` = 0. ⚠️ Pendiente: escenario donde anticipo cubre 100% antes de causación. | OxpComercio, OxpExtracto | — |
| I16 | **Causalidad de pago:** Pagos (`PagoAplicado`) solo se aplican a OxpComercio en estado Causada. Pagos (`CrucePagoExtractoAplicado`) solo se aplican a OxpExtracto en estado Causado. ⚠️ **Provisional (restricción SincoA&F):** esta regla refleja la integración actual con SincoA&F. Con el futuro sistema de Tesorería, el pago podría desacoplarse de la causación (Tesorería recibe OXPs confirmadas y confirma pagos independientemente de la causación contable). En particular, `PagoAplicado` tipo anticipo podría aplicarse antes de Causada (la regularización ocurre en Pendiente/Confirmada), pero requiere resolver: (1) desincronización si `valorNeto()` cambia entre regularización y causación, (2) reserva de saldo en el anticipo cuando múltiples OxpComercio referencian el mismo anticipo. | OxpComercio, OxpExtracto | — |
| I17 | **Consistencia de devolución:** Restricciones por tipo de OXP. **Comercio:** `valorNeto(Devolucion)` ≤ `valorNeto(OxpComercio)`. La suma de todas las devoluciones sobre una misma OxpComercio no puede superar el `valorNeto()` original. Cuando `saldoPorPagar(OXP) > 0`: `valorNeto(devolucion) ≤ saldoPorPagar(OXP)`. ⚠️ Pendiente: escenario donde devolución > saldoPorPagar (nota débito no definida). **Extracto:** `valorNeto(devolucion)` ≤ `saldoPorPagar(OxpExtracto)` cuando saldo > 0. **Anticipo:** solo reversa total (`valorNeto(devolucion)` = valorTotal del anticipo). Anticipo en estado Vigente sin cruces de pago ni regularización. Mismo tercero obligatorio en todos los tipos. | Devolucion, OxpComercio, OxpExtracto, Anticipo | — |

---

## 8. Qué NO contiene este documento

| Excluido | Razón | Dónde vive |
|----------|-------|------------|
| Glosario de términos | Ya definido | `Definicion_Alcance.md`, Sección 2 |
| Actores y permisos | Ya definidos | `Definicion_Alcance.md`, Sección 3 |
| Reglas de negocio completas | Ya definidas (R01–R27) | `Definicion_Alcance.md`, Sección 6 |
| Modelo de datos / esquema de BD | Pertenece a implementación | Documentación técnica (fase 2) |
| Endpoints de API / contratos | Pertenece a implementación | Documentación técnica (fase 2) |
| Diseño de interfaz de usuario | Pertenece a UX | Especificaciones de UX |
| Configuración de EventCatalog | Herramienta de fase 2 | Se derivará de este documento |
| Justificación de decisiones de modelado | Documento separado | `Justificaciones/ModelarAgregados.md` |

---

## 9. Decisiones de arquitectura y diseño

Registro de las decisiones tomadas durante la definición del modelo de dominio. Cada decisión incluye su justificación y el principio de diseño que la sustenta.

| # | Decisión | Justificación | Principio |
|---|---|---|---|
| D1 | **OXP es un bounded context**, no un agregado. | Contiene múltiples agregados coordinados (OxpComercio, OxpExtracto, Anticipo, Devolucion) con ciclos de vida independientes. | DDD: bounded context como límite lingüístico y de responsabilidad. |
| D2 | **Cuatro agregados raíz:** OxpComercio, OxpExtracto, Anticipo y Devolucion. | No comparten estados, eventos ni transiciones. Solo comparten Value Objects. Streams de eventos independientes. Cada agregado tiene su propio ciclo de vida y máquina de estados. | DDD: el agregado define un límite de consistencia transaccional. Análisis detallado en `Justificaciones/ModelarAgregados.md`. |
| D3 | **ServicioDeConciliacion** como domain service. | La conciliación coordina efectos en ambos agregados (OxpComercio → PagoOxpComercioViaExtractoAplicado, OxpExtracto → VinculacionRealizada). No pertenece a ninguno — es coordinación. | DDD: domain service para operaciones que no pertenecen a un agregado. Event sourcing: consistencia eventual entre streams. |
| D4 | **ConceptoDeGasto** como única entidad interna de OxpComercio. | Impuestos y retenciones son cálculos derivados del gasto (no tienen vida propia, se reemplazan al recalcular). El gasto es la causa; los tributos son el efecto. | DDD: entidades para cosas con identidad y ciclo de vida; Value Objects para cálculos derivados e inmutables. |
| D5 | **DesgloseFiscal y Tributo** como Value Objects. | Se reemplazan completos al recalcular. No necesitan identidad. Un IVA sobre el mismo gasto con los mismos datos es el mismo cálculo. | POO: inmutabilidad para derivados. Evita desincronización entre datos almacenados y calculados. |
| D6 | **Distribución separada del concepto** (InstruccionDistribucion). | Cada componente (gasto o tributo) puede tener distribución diferente. Mezclar distribución con concepto cruza tres responsabilidades: qué se compró, qué tributos aplican, hacia dónde va. **Beneficio:** la separación habilita la cadena de resolución (instrucción explícita → herencia del componente padre → preferencia de empresa → destino único pendiente, ver D7/I10) y permite distribución independiente por componente. **Costo:** al ser una lista paralela a los componentes, el agregado debe encapsular la sincronización — al agregar, eliminar o recalcular componentes, las instrucciones correspondientes deben mantenerse coherentes para evitar datos huérfanos. Esta sincronización es responsabilidad interna del agregado, no del consumidor externo. | Separación de responsabilidades. Cada objeto tiene una razón de cambio. |
| D7 | **Cadena de resolución** de distribución. | Las instrucciones dependen de los componentes que las originan. La cadena (explícita → herencia → empresa → pendiente) garantiza coherencia sin datos huérfanos. | Invariante I10. Consistencia del agregado. |
| D8 | **OXP no conoce el dominio contable.** | Sin cuentas contables, sin centros de costo, sin naturalezas débito/crédito. La traducción ocurre en la frontera (servicio de Traducción Contable). | DDD: anti-corruption layer. Cada contexto protege su lenguaje. |
| D9 | **DestinoDeNegocio con identificador estandarizado** (Shared Kernel). | `unidadOrganizacional` usa un código del catálogo organizacional que ambos contextos (OXP y Contabilidad) reconocen. OXP no sabe qué cuenta es; el traductor sí. | DDD: Shared Kernel para vocabulario compartido entre bounded contexts. |
| D10 | **Valores totales como comportamiento calculado**, no como dato almacenado. | `valorBruto()`, `totalImpuestos()`, `totalRetenciones()`, `valorNeto()` se derivan de los componentes. Una sola fuente de verdad. | POO: evitar redundancia. Event sourcing: el replay reconstruye componentes, los totales se derivan. Para consultas rápidas: read model / projection. |
| D11 | **lineasParaTraduccion()** como comportamiento del agregado. | Pre-computa líneas planas (componente × destino) con valor distribuido. El servicio de Traducción Contable recibe líneas listas y solo mapea, sin entender distribuciones ni herencias. | Separación de responsabilidades. OXP prepara; el traductor traduce. |
| D12 | **Devolución como agregado independiente** (`Devolucion`). Puede referenciar OxpComercio, OxpExtracto o Anticipo según el tipo de OXP. Tres entidades internas polimórficas con contrato común (`descripcion`, `valor: ValorMonetario`): `ConceptoDevuelto` (Comercio, 1..N), `CargoFinancieroDevuelto` (Extracto, 1..N), `ReversaTotal` (Anticipo, exactamente 1). Valores positivos representando magnitud del crédito (D19). Comportamiento propio: no recibe pagos, sino que genera un crédito que se aplica vía `ServicioDeAplicacionDevolucion`. Los efectos varían por tipo de OXP (ver Sección 3, ServicioDeAplicacionDevolucion). | La justificación original de v1.5 (70%+ solapamiento estructural) fue invalidada por v2.1: `saldoPorPagar()` con `valorNeto()` negativo violaba I13/I15. La devolución tiene comportamiento financiero fundamentalmente distinto (crédito vs débito). Agregado independiente con ciclo de vida propio. Tres entidades polimórficas reemplazan la entidad unificada con atributos condicionales — cada una con nombre de dominio propio y atributos específicos, compartiendo un contrato común. |
| D13 | **Anticipo como agregado independiente**, no como estado de OxpComercio. | El anticipo tiene un ciclo de vida propio e independiente (quién + cuánto + medio de pago + soporte opcional), diferente al de OxpComercio (conceptos, desglose fiscal, distribución compleja). Dos comportamientos: vinculado a extracto (compensación) o pendiente de pago. La regularización siempre es vía OxpComercio con soporte formal. La partida del extracto se vincula al anticipo de forma permanente `[R08]`. La OxpComercio que regulariza un anticipo puede opcionalmente vincularse a un extracto (pagos mixtos, v2.1) o pagarse completamente vía anticipo. | DDD: agregados diferentes para ciclos de vida diferentes. El anticipo no "se convierte" en OxpComercio — son dos objetos que se vinculan. |
| D14 | **Pagada como único estado terminal financiero de OxpComercio.** | Se alcanza cuando `saldoPorPagar()` = 0, independiente de la(s) fuente(s) de pago (extracto, anticipo, pago directo, o combinación). Compensada eliminada como estado (ver D18). | Extensibilidad. El estado terminal refleja la realidad del negocio: la obligación fue liquidada financieramente. |
| D15 | **Anticipo con dos comportamientos, dos dimensiones de valor y estados intermedios independientes (Pagado, Regularizado) hacia estado terminal (Cerrado).** | Comportamiento 1: vinculado a extracto (TC, pago ya realizado, partida visible). Comportamiento 2: no vinculado a extracto (forma de pago diferente a TC, pago pendiente confirmado por SincoA&F). Ambos pueden tener o no soporte preliminar (ej: cuenta de cobro). Los dos tipos de cruce (extracto y pago directo) pueden coexistir sobre el mismo anticipo — los comportamientos no son mutuamente excluyentes. Valor anticipo (monto adelantado, se resuelve por regularización vía OxpComercio con soporte formal) y valor total (se resuelve por pago vía extracto o pago directo). Los valores pueden diferir. Cada dimensión se resuelve mediante cruces parciales rastreados por entidades internas (`CrucePagoAplicado`, `CruceRegularizacionAplicada`). Saldos derivados, no almacenados. Estado terminal (Cerrado) requiere ambos resueltos. | Cada dimensión de valor tiene su propio ciclo de resolución independiente. La regularización es un control transversal. Los estados Pagado y Regularizado son intermedios — el verdadero terminal es Cerrado. |
| D16 | **Cruces parciales como entidades internas del Anticipo.** | Cada cruce parcial (pago o regularización) es un registro individual (`CrucePagoAplicado` o `CruceRegularizacionAplicada`). El saldo es un valor derivado (valor preestablecido − suma de cruces). Dos componentes separados porque rastrean valores preestablecidos distintos (valorTotal vs valorAnticipo) y referencian entidades externas diferentes (PartidaExtracto/SincoA&F vs OxpComercio). | POO: saldo derivado evita redundancia (D10). DDD: entidades internas con identidad para trazabilidad de cada cruce individual. Event sourcing: replay reconstruye los cruces, los saldos se derivan. |
| D17 | **`saldoPorPagar` como comportamiento calculado en OxpComercio y OxpExtracto.** | Sigue el patrón de saldos derivados del Anticipo (D10, D16). Valor derivado, no almacenado. Se reduce mediante pagos parciales rastreados por entidades internas (`PagoAplicado` en OxpComercio, `CrucePagoExtractoAplicado` en OxpExtracto). Evento de transición a estado terminal cuando saldo = 0. | POO: saldo derivado evita redundancia. Event sourcing: replay reconstruye entidades de pago, los saldos se derivan. Consistencia con el patrón del Anticipo. |
| D18 | **Compensada eliminada como estado de OxpComercio.** | La vinculación con extracto (conciliación) es un pago aplicado que reduce `saldoPorPagar()`, no un cambio de estado. Pagada es el único estado terminal financiero. Permite pagos mixtos (extracto + anticipo + pago directo) sobre la misma OxpComercio. Reemplaza el modelo anterior donde Compensada era un estado terminal. | DDD: el estado debe reflejar la realidad del negocio. La obligación no "se compensa" — recibe pagos parciales hasta liquidarse. |
| D19 | **Devolucion con valores positivos (magnitud del crédito).** La naturaleza contable (nota crédito vs factura) no se representa con el signo de los valores — la determina el tipo del agregado. La traducción contable interpreta Devolucion como nota crédito. | Consistente con D8 (OXP no conoce el dominio contable). Evita el problema de `valorNeto()` negativo que invalidó D12 en v2.1. Cada agregado tiene valores positivos; la semántica contable la resuelve la frontera. |

---

## 10. Premisas de negocio

Premisas que provienen del negocio, la regulación o la fiscalidad y que condicionan el diseño del modelo. No son decisiones arquitectónicas (D##) ni invariantes estructurales (I##) — son verdades externas al modelo que se toman como base.

| # | Premisa | Justificación | Aplica a |
|---|---|---|---|
| P1 | **El anticipo registra únicamente el valor global; no aplica desglose fiscal.** | Fiscalmente, el IVA solo puede tomarse como descontable cuando existe factura o documento válido. El anticipo es un pago adelantado sin factura — el soporte formal (factura) llega durante la regularización vía OxpComercio. Por esta razón, el Anticipo no contiene `ConceptoDeGasto`, `DesgloseFiscal` ni `Tributo`. | Anticipo |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Febrero 2026 | Versión inicial: 28 eventos, 2 máquinas de estado, 6 tipos de concepto, 9 invariantes. |
| 1.1 | Febrero 2026 | Reestructuración: agregado único Oxp con variantes Comercio y Extracto. Catálogo reorganizado por fase del ciclo de vida. |
| 1.2 | Febrero 2026 | OXP como bounded context con dos agregados raíz (OxpComercio, OxpExtracto). Value Objects compartidos. ServicioDeConciliacion como domain service. VinculacionRealizada y OxpComercioCompensada como eventos coordinados. |
| 1.3 | Febrero 2026 | Reestructuración de OxpComercio: ConceptoDeGasto como única entidad con DesgloseFiscal (Tributo VO). Distribución separada como InstruccionDistribucion con cadena de resolución. Nueva invariante I10 (consistencia de distribución). 10 invariantes. |
| 1.4 | Febrero 2026 | DestinoDeNegocio con identificador estandarizado (Shared Kernel). Comportamiento calculado del agregado (valorBruto, valorNeto, lineasParaTraduccion). Sección 9: Decisiones de arquitectura y diseño (D1–D11). |
| 1.5 | Febrero 2026 | Devolución absorbida como variante de OxpComercio (`TipoOxpComercio`). Corrección de 7 inconsistencias entre agregados, máquinas de estado y catálogo de eventos. Nuevo evento `AjustePorToleranciaGenerado`. 27 eventos (OxpComercio: 12, OxpExtracto: 15). Decisión D12. |
| 1.6 | Febrero 2026 | Anticipo extraído como agregado independiente con 4 eventos propios. Nuevo domain service `ServicioDeRegularizacion`. Nuevo estado terminal Pagada en OxpComercio. Nuevo evento `PartidaCubiertaPorAnticipo` en OxpExtracto. Nuevo evento `OxpComercioPagada` en OxpComercio. Nueva invariante I11 (saldo no negativo). 30 eventos (OxpComercio: 10, OxpExtracto: 16, Anticipo: 4). Decisiones D13, D14. |
| 1.7 | Febrero 2026 | Distribución unificada con dos niveles: (1) agregado — valor total de la obligación (Shared Kernel), (2) componente — detalle por concepto/cargo/ajuste. `InstruccionDistribucion` aplicado a los tres agregados. OxpExtracto: distribución en CargoFinanciero, AjustePorDiferenciaCambio y AjustePorTolerancia con `lineasParaTraduccion()`. Anticipo: distribución sobre el valor del anticipo. Invariante I2 extendida a OxpComercio, OxpExtracto y Anticipo. |
| 1.8 | Febrero 2026 | Reestructuración del Anticipo: dos comportamientos (vinculado a extracto / pendiente de pago), dos dimensiones de valor (valor anticipo y valor total), dos estados terminales (Compensado y Pagado). Anticipo puede tener o no soporte documental preliminar (ej: cuenta de cobro); la regularización siempre es vía OxpComercio con soporte formal. Nuevo: 3 eventos (`AnticipoVinculadoAPartida`, `AnticipoCompensado`, `AnticipoPagado`), entidad `CoberturaPartida`, `SoporteDocumental` opcional, `saldoPendienteRegularizacion`. Relación Anticipo → PartidaExtracto cambia de 1:1 a 1:N. Conexión Anticipo → OxpExtracto en diagrama BC. Nueva invariante I12 (consistencia de cobertura). 33 eventos (OxpComercio: 10, OxpExtracto: 16, Anticipo: 7). Decisión D15. 12 invariantes. |
| 1.9 | Febrero 2026 | Cruces parciales y nuevo modelo de estados del Anticipo. Entidades `CoberturaPartida` y `saldoPendienteRegularizacion` reemplazadas por dos componentes de cruce parcial: `CrucePagoAplicado` (resuelve valorTotal, tipo extracto o pago_directo, inmutable, coexistentes) y `CruceRegularizacionAplicada` (resuelve valorAnticipo). Saldos derivados: `saldoPorPagar()` y `saldoPorRegularizar()`. `valorTotal` inicialmente igual al valor anticipo. Nuevo modelo de 4 estados con dos dimensiones independientes: Vigente → Pagado / Regularizado → Cerrado ■. Distinción entre eventos de progreso (reducen saldos) y eventos de transición (cambian estado). Evento `AnticipoCompensado` eliminado; nuevos eventos: `PagoAnticipoAplicado`, `RegularizacionDeAnticipoCompletada`. Los dos tipos de cruce (extracto y pago directo) pueden coexistir sobre el mismo anticipo. `AnticipoAmortizado` puede ocurrir desde Regularizado o Cerrado. `OxpComercioPagada` marcada pendiente de redefinición (futuro `saldoPorPagar` de OxpComercio). Eliminado acoplamiento `AnticipoAmortizado` → `OxpComercioPagada`. Distribución única aplica proporcionalmente a ambas dimensiones de valor. Corrección de inconsistencias: D2 (3 agregados), I4 (todos los agregados), I8/I11/I12 actualizadas, dualidad `CoberturaAnticipo`/`CrucePagoAplicado` documentada. 34 eventos (OxpComercio: 10, OxpExtracto: 16, Anticipo: 8). Decisiones D15 actualizada, D16 nueva. 12 invariantes. |
| 2.0 | Febrero 2026 | Nueva Sección 10: Premisas de Negocio (`P##`). Nomenclatura `[P##]` agregada a convenciones (Sección 2) y tabla de contenido. P1: el anticipo registra únicamente el valor global sin desglose fiscal — el IVA solo es descontable con factura o documento válido, que llega vía OxpComercio durante la regularización. Referencia `[P1]` en estructura del Anticipo (`ValorMonetario`). I2 actualizada con referencia explícita a `[P1]` para distribución del anticipo. |
| 2.1 | Febrero 2026 | `saldoPorPagar()` como comportamiento calculado en OxpComercio y OxpExtracto (D17). **Compensada eliminada como estado** de OxpComercio (D18) — la vinculación con extracto es un pago aplicado, no un cambio de estado. Pagada como único estado terminal financiero (D14 actualizada). Nuevas entidades internas: `PagoAplicado` (OxpComercio, tipo extracto/anticipo/pago_directo) y `CrucePagoExtractoAplicado` (OxpExtracto). Comportamiento calculado `valorTotalExtracto()` en OxpExtracto. Eventos: `OxpComercioCompensada` eliminado; nuevos `PagoOxpComercioViaExtractoAplicado`, `PagoOxpComercioViaAnticipoAplicado`, `PagoOxpComercioDirectoAplicado`, `PagoExtractoAplicado`. `OxpComercioPagada` redefinido como evento de transición (saldoPorPagar = 0). `ExtractoPagado` redefinido como evento de transición. `ServicioDeConciliacion` ahora emite `PagoOxpComercioViaExtractoAplicado` (no Compensada). `ServicioDeRegularizacion` ahora coordina efecto en OxpComercio (`PagoOxpComercioViaAnticipoAplicado`). Relaciones actualizadas: pagos mixtos documentados. Nuevas invariantes I13–I16 (saldos no negativos, consistencia de estado de pago, causalidad de pago). 37 eventos (OxpComercio: 12, OxpExtracto: 17, Anticipo: 8). 16 invariantes. Decisiones D17, D18 nuevas. **Correcciones post-inspección:** D13 corregida (pagos mixtos compatibles con conciliación). Precondición de `PagoOxpComercioViaAnticipoAplicado` clarificada ("Anticipo no cerrado" en vez de "vigente"). `AnticipoRegularizado`: descripción corregida ("OxpComercio vinculada" en vez de "entrante"), precondición alineada con ⚠️ pendiente (eliminada referencia a "radicada"). ServicioDeRegularizacion paso 4: agregada precondición `saldoPorPagar()` suficiente en OxpComercio (M2). **⚠️ Pendientes por definir:** (1) **Devoluciones (D12):** `saldoPorPagar()` no se sostiene con `valorNeto()` negativo — I13/I15 se violan. La devolución tiene comportamiento financiero fundamentalmente distinto (crédito vs débito). Posible extracción como agregado independiente; la devolución es un espejo parcial o completo de la obligación original. (2) **Momento de regularización (ServicioDeRegularizacion):** el negocio regulariza en Pendiente/Confirmada, pero esto genera dos problemas: desincronización si `valorNeto()` cambia después de regularizar (cruces inmutables desfasados en ambos agregados), y overbooking cuando múltiples OxpComercio referencian el mismo anticipo sin reservar saldo. (3) **I16 — provisional (restricción SincoA&F):** "pagos solo desde Causada" no es verdad de dominio. Con el futuro sistema de Tesorería, el pago se desacopla de la causación (Tesorería recibe OXPs confirmadas y confirma pagos independientemente del control contable). I16 requiere redefinición cuando se implemente Tesorería. (4) **I13/I15 para tipo Devolución:** requieren redefinición una vez se resuelva el diseño de devoluciones. |
| 2.2 | Febrero 2026 | **Devolucion como agregado independiente** (D12 resuelta). Devolución extraída de OxpComercio como cuarto agregado raíz (D2 actualizada). Espejo parcial o completo de la OxpComercio que reversa, con valores positivos representando magnitud del crédito (D19 nueva). `TipoOxpComercio` eliminado de OxpComercio. `DevolucionAsociada` eliminado — funcionalidad trasladada a `DevolucionRadicada`. Nuevo `ServicioDeAplicacionDevolucion` (domain service): coordina Devolucion, OxpComercio y opcionalmente Anticipo. Dos ramas: si `saldoPorPagar(OXP) > 0` reduce saldo vía `PagoOxpComercioViaDevolucionAplicado`; si `saldoPorPagar(OXP) = 0` crea Anticipo (estado Pagado, pendiente regularización). Crédito aplicado en la confirmación (no en causación). `PagoAplicado` ahora tiene 4 tipos: extracto, anticipo, pago_directo, devolucion. Estados Devolucion: Pendiente → Confirmada → Causada ■. 3 eventos nuevos: `DevolucionRadicada`, `DevolucionConfirmada`, `DevolucionCausada`. 1 evento nuevo en OxpComercio: `PagoOxpComercioViaDevolucionAplicado`. I13/I15 ⚠️ de devolución resueltos. Nueva invariante I17 (consistencia de devolución). 40 eventos (OxpComercio: 12, OxpExtracto: 17, Anticipo: 8, Devolucion: 3). 17 invariantes. **Correcciones post-inspección:** `OxpComercioPagada` y `AnticipoPagado` actualizados con devolución como fuente de pago. `CrucePagoAplicado` del Anticipo: nuevo tipo `devolucion` para anticipo nacido de devolución. `DevolucionRadicada`: precondición reforzada con acumulado I17. `DevolucionConfirmada` y `ServicioDeAplicacionDevolucion`: precondición explícita de OxpComercio en Causada o Pagada. Template de evento, I2, I4, I8, I9, I12 actualizados con Devolucion. `OxpComercioDevuelta`: eliminada referencia obsoleta a "tipo = Compra". D1: 4 agregados. Value Objects compartidos: `MedioDePago` aplica a 3 de 4 agregados. Referencia a `Justificaciones/ModelarAgregados.md`: "múltiples agregados". **⚠️ Pendientes:** (1) Escenario donde devolución > saldoPorPagar en OXP parcialmente pagada (nota débito no definida). (2) Reembolso del excedente de devolución (posible integración CXC). |
| 2.3 | Febrero 2026 | **Devolucion extendida a OxpExtracto y Anticipo.** Devolucion ya no referencia únicamente OxpComercio — ahora referencia uno de tres tipos de OXP (Comercio, Extracto, Anticipo) vía referencia a OXP origen (tipo + ID). Nueva entidad interna `ConceptoDeDevolucion` con atributos condicionales por tipo: Comercio (código, cantidad, DesgloseFiscal), Extracto (tipoComponente, referenciaComponente), Anticipo (motivoReversa). `ServicioDeAplicacionDevolucion` extendido con 3 ramas: Comercio (sin cambios), Extracto (reduce saldoPorPagar vía `PagoExtractoViaDevolucionAplicado`), Anticipo (reversa total vía `AnticipoReversado`). **OxpExtracto:** 2 eventos nuevos (`PartidaCubiertaPorDevolucion`, `PagoExtractoViaDevolucionAplicado`). Nueva entidad `CoberturaDevolucion`. `CrucePagoExtractoAplicado` con tipos `pago_sincoa` y `devolucion`. Estado de partida `devolucion`. `ServicioDeConciliacion` extendido con flujo de partidas de retorno. **Anticipo:** Nuevo estado terminal Reversado (desde Vigente, sin cruces previos). Nuevo evento `AnticipoReversado`. Cruces tipo `reversa` en `CrucePagoAplicado` y `CruceRegularizacionAplicada` — llevan ambos saldos a 0 (patrón consistente: estado terminal = saldos resueltos). Diagrama BC actualizado: Devolucion referencia OxpComercio (espejo de), OxpExtracto (ajuste sobre) y Anticipo (reversa). I3, I4, I8, I12, I17 actualizadas. 43 eventos (OxpComercio: 12, OxpExtracto: 19, Anticipo: 9, Devolucion: 3). 17 invariantes. **⚠️ Pendientes:** (1) Momento de regularización de anticipos. (2) Anticipo A2 — proveedor devuelve dinero (requiere dominio CXC). (3) `lineasParaTraduccion()` para Devolucion tipo Anticipo — hereda distribución del anticipo, capturada en radicación. |
| 2.4 | Febrero 2026 | **Refinamiento del modelo de dominio — 8 hallazgos aplicados (H1–H8).** **H1 — Entidades polimórficas en Devolucion:** `ConceptoDeDevolucion` (entidad única con atributos condicionales) reemplazado por tres entidades polimórficas con contrato común (`descripcion`, `valor: ValorMonetario`): `ConceptoDevuelto` (Comercio — código, cantidad, DesgloseFiscal), `CargoFinancieroDevuelto` (Extracto — referenciaCargoFinanciero) y `ReversaTotal` (Anticipo — motivoReversa). Tabla de comportamiento calculado reestructurada con columnas por tipo (Comercio, Extracto, Anticipo). `lineasParaTraduccion()` con descripción específica por tipo. 3 diagramas de composición independientes (1 reescrito, 2 nuevos). Sección 6 reescrita con 3 subsecciones: `ConceptoDevuelto`, `CargoFinancieroDevuelto`, `ReversaTotal`. Escenario E1b eliminado — partida de retorno siempre es Devolucion tipo Comercio (E1). E2 redefinido: cargo financiero devuelto contra extracto anterior. `DevolucionRadicada` actualizado con entidades específicas por tipo. D12 reescrita: tres entidades polimórficas con contrato común. I2, I10 actualizadas. **H2 — Strategy en ServicioDeAplicacionDevolucion:** `[SI2]` — sugerencia de implementación con tabla beneficio/costo para las 3 ramas del servicio. **H3 — InstruccionDistribucion como lista paralela:** D6 expandida con análisis beneficio/costo: la separación habilita cadena de resolución pero requiere sincronización encapsulada por el agregado. **H4 — FSM de PartidaExtracto:** nueva sub-sección 4.2.1 con máquina de estados interna (6 estados, 6 transiciones, diagrama ASCII). **H5 — OxpComercioExteriorRadicada eliminado:** consolidado como efecto condicional de `OxpComercioRadicada` (compra del exterior o sujeto no obligado a facturar → alerta DIAN para Documento Soporte en Adquisiciones). **H6 — Causalidad entre eventos:** nueva convención en Sección 2 con tres tipos: evento derivado por transición (mismo agregado, mismo append), derivado por configuración (mismo agregado, condicional), efecto inter-agregado (domain service, consistencia eventual). Tabla con ejemplos. **H7 — Exclusión de ajustes en valorTotalExtracto():** justificación añadida — `AjustePorDiferenciaCambio` y `AjustePorTolerancia` son cálculos internos de conciliación, no montos cobrados por el banco. **H8 — Entidades espejo:** patrón formalizado con tabla (`Vinculacion`↔`PagoAplicado`, `CoberturaAnticipo`↔`CrucePagoAplicado`, `CoberturaDevolucion`↔ref en Devolucion) y convención: cada agregado es dueño de su entidad espejo, ninguno consulta la del otro. **Nuevas convenciones:** `[SI##]` (sugerencias de implementación — recomendaciones técnicas que complementan definiciones del dominio), causalidad entre eventos, entidades espejo. `[SI1]` — sealed interfaces para entidades internas con discriminador de tipo (`PagoAplicado`, `CrucePagoAplicado`, `CrucePagoExtractoAplicado`, `CruceRegularizacionAplicada`). 42 eventos (OxpComercio: 11, OxpExtracto: 19, Anticipo: 9, Devolucion: 3). 17 invariantes. **⚠️ Pendientes:** (1) Momento de regularización de anticipos. (2) Anticipo A2 — proveedor devuelve dinero (requiere dominio CXC). |
