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

---

## 1. Propósito y relación con otros documentos

Este documento especifica el comportamiento interno del dominio OXP mediante eventos, transiciones de estado, precondiciones, invariantes y la información de negocio que cada evento captura. Su objetivo es servir como puente entre la definición funcional y la implementación técnica.

| Documento | Alcance | Relación |
|-----------|---------|----------|
| `Definicion_Alcance.md` | QUÉ hace el sistema | Fuente de verdad para glosario, actores, flujos y reglas (R01–R27). No se duplica aquí. |
| **Este documento** | CÓMO se comporta el dominio | Eventos, transiciones, precondiciones, invariantes, tipos de concepto. |
| `Justificaciones/ModelarAgregados.md` | POR QUÉ dos agregados | Análisis comparativo de agregado único vs. dos agregados desde event sourcing. |
| EventCatalog (fase 2) | Catalogación técnica | Consumirá este documento como especificación de entrada durante la implementación. |

Las reglas de negocio se referencian como `[R##]` y su texto completo vive en `Definicion_Alcance.md`, Sección 6.

---

## 2. Convenciones del documento

### Nomenclatura

- **Eventos:** PascalCase en español. Ej: `OxpComercioRadicada`, `ExtractoConciliado`.
- **Referencias a reglas:** `[R##]` remite a `Definicion_Alcance.md`, Sección 6.
- **Agregados:** OxpComercio, OxpExtracto, Anticipo.
- **Estados OxpComercio:** Pendiente, Confirmada, Causada, Compensada, Pagada, Devuelta.
- **Estados OxpExtracto:** Pendiente, Parcialmente Conciliado, Conciliado, Confirmado, Causado, Pagado.
- **Estados Anticipo:** Vigente, Amortizado.

### Template de evento

Cada evento del catálogo (Sección 5) se documenta con la siguiente estructura:

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Qué ocurrió en términos de negocio. |
| **Agregado** | OxpComercio / OxpExtracto / Anticipo. |
| **Estado previo** | Estado(s) desde los que puede emitirse. |
| **Estado resultante** | Estado al que transiciona la entidad. |
| **Precondiciones** | Condiciones requeridas. Ref. a reglas: [R##]. |
| **Información capturada** | Datos de negocio que el evento registra (no campos de BD). |
| **Efectos** | Integraciones salientes, alertas u otros eventos derivados. |

### Diagramas

Las máquinas de estado usan notación ASCII. Los estados terminales se marcan con `■`. Las transiciones se etiquetan con el nombre del evento entre paréntesis.

---

## 3. Bounded Context y Agregados

### OXP como Bounded Context

OXP (Obligaciones por Pagar) es un **bounded context** — no un agregado. Contiene múltiples agregados coordinados que en conjunto gestionan el ciclo de vida de las obligaciones originadas en medios de pago corporativos.

```
┌──────────────────────────────────────────────────────────────────────────┐
│                     Bounded Context: OXP                                  │
│                                                                           │
│  ┌──────────────┐                               ┌──────────────────┐    │
│  │   Anticipo   │                                │   OxpExtracto    │    │
│  │  (Agregado)  │                                │   (Agregado)     │    │
│  └──────┬───────┘                                └────────┬─────────┘    │
│         │ ServicioDe                                      │              │
│         │ Regularizacion                    ServicioDe    │              │
│         │ (Domain Service)                  Conciliacion  │              │
│         ▼                                   (Domain Svc)  │              │
│  ┌──────────────────┐◄───────────────────────────────────┘              │
│  │   OxpComercio    │                                                    │
│  │   (Agregado)     │                                                    │
│  └──────────────────┘                                                    │
│                                                                           │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │              Value Objects compartidos                              │  │
│  │  InformacionTercero · MedioDePago · ValorMonetario                 │  │
│  │  SoporteDocumental                                                 │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
```

### Agregado: OxpComercio

- **Raíz:** Una obligación individual originada por compra o devolución con tarjeta corporativa (crédito o débito prepago).
- **Ciclo de vida:** Radicación → Confirmación → Causación → Compensación o Pago.
- **Estados terminales:** Compensada (vinculada a extracto) o Pagada (regulariza anticipo / pago directo futuro).
- **Stream de eventos:** `oxp-comercio-{id}`
- **Eventos propios:** 10.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `ConceptoDeGasto` | Gasto o costo de la obligación. Pueden existir múltiples conceptos idénticos como registros independientes. Invariante: mínimo 1 por OxpComercio. | Código, descripción, cantidad, valor. Desglose fiscal: `DesgloseFiscal` (VO). |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `TipoOxpComercio` | Compra o Devolución. Determina variantes en radicación (valor positivo/negativo, referencia a OXP original) y causación (factura/nota crédito). Inmutable una vez radicada. |
| `InformacionTercero` | NIT, razón social |
| `MedioDePago` | Tipo (crédito/débito prepago), número, entidad bancaria |
| `ValorMonetario` | Monto, moneda, TRM (si aplica), monto en moneda funcional |
| `SoporteDocumental` | Tipo (PDF, imagen, XML), referencia, datos extraídos |
| `DesgloseFiscal` | Agrupa los cálculos fiscales derivados de un `ConceptoDeGasto`. Inmutable — se reemplaza completo al recalcular. Contiene: `List<Tributo>` de impuestos y `List<Tributo>` de retenciones. |
| `Tributo` | Cálculo fiscal individual (impuesto o retención). Tipo, base, tarifa, valor. Inmutable — es el resultado de aplicar reglas fiscales al gasto. |
| `InstruccionDistribucion` | Instrucción que indica cómo distribuir un componente del agregado. Referencia al componente (ConceptoDeGasto o Tributo específico), `List<DestinoDeNegocio>` (invariante I2: suma = 100%). |
| `DestinoDeNegocio` | Identificador de unidad organizacional (Shared Kernel con el contexto contable), porcentaje. Ej: `{ unidadOrganizacional: "VTA-001", porcentaje: 60 }`. Usado dentro de `InstruccionDistribucion`. |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  OxpComercio (Agregado)                                      │
│                                                              │
│  ○ TipoOxpComercio (Compra | Devolución)                    │
│  ○ InformacionTercero    ○ MedioDePago    ○ ValorMonetario   │
│  ○ SoporteDocumental                                        │
│  ○ Ref. a OxpComercio original (solo si tipo = Devolución)   │
│  ○ Ref. a Anticipo(s) regularizado(s) (opcional)             │
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
│  │ Instrucciones de Distribución (VO)                     │  │
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
| `lineasParaTraduccion()` | Pre-computa una lista plana de líneas, una por cada combinación (componente × destino de negocio), con el valor ya distribuido (valor × porcentaje). Cada línea incluye: tipo de componente (gasto, impuesto, retención), identificador de unidad organizacional, y valor distribuido. El servicio de Traducción Contable recibe estas líneas y solo necesita mapear `(tipo componente + unidad organizacional) → cuenta contable`. No necesita entender distribuciones, herencias ni cadenas de resolución. |

### Agregado: OxpExtracto

- **Raíz:** Una obligación consolidada del período, originada por extracto bancario.
- **Ciclo de vida:** Radicación → Conciliación → Confirmación → Causación → Pago.
- **Estado terminal:** Pagado (financiero — confirmado por SincoA&F).
- **Stream de eventos:** `oxp-extracto-{id}`
- **Eventos propios:** 16.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `PartidaExtracto` | Línea individual del extracto bancario. | Descripción, valor, fecha, estado (pendiente/vinculada/disputa/anticipo). |
| `CargoFinanciero` | Cargo adicional del extracto (no corresponde a compra). | Subtipo (4x1000, cuota de manejo, intereses), valor, período. |
| `AjustePorDiferenciaCambio` | Ajuste generado al vincular OxpComercio en moneda extranjera. | OxpComercio origen, TRM radicación, TRM extracto, valor, clasificación (gasto/ingreso financiero). |
| `AjustePorTolerancia` | Ajuste generado al vincular con diferencia dentro de tolerancia. | OxpComercio origen, valor diferencia, dirección (extracto mayor/menor). |
| `Vinculacion` | Referencia que conecta una partida del extracto con una o más OxpComercio. | Ref. a OxpComercio, partida, tipo (1:1/N:1), origen (automática/manual). |
| `CoberturaAnticipo` | Referencia que conecta una partida del extracto con un Anticipo. Vínculo permanente. | Ref. a Anticipo, partida. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `InformacionTercero` | NIT, razón social |
| `MedioDePago` | Tipo (crédito/débito prepago), número, entidad bancaria |
| `ValorMonetario` | Monto, moneda, TRM (si aplica), monto en moneda funcional |

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
│                                                              │
│  ● = Entidad (tiene identidad)   ○ = Value Object (sin ID)  │
└──────────────────────────────────────────────────────────────┘
```

### Agregado: Anticipo

- **Raíz:** Pago adelantado sin soporte documental. Representa dinero que ya fue desembolsado al tercero pero sin evidencia económica (factura/soporte).
- **Ciclo de vida:** Registro → Regularización(es) → Amortización.
- **Estado terminal:** Amortizado (saldo regularizado al 100% y confirmado por SincoA&F).
- **Stream de eventos:** `anticipo-{id}`
- **Eventos propios:** 4.

**Estructura:**

| Componente | Tipo | Contenido |
|---|---|---|
| `InformacionTercero` | VO | NIT, razón social |
| `ValorMonetario` | VO | Monto, moneda, TRM si aplica, monto en moneda funcional |
| `MedioDePago` | VO | Tipo (crédito/débito prepago), número, entidad bancaria |
| `saldoPendiente` | Valor | Monto aún no regularizado (inicia igual al valor total) |
| `justificacion` | Texto | Motivo de ausencia de soporte documental |
| `InstruccionDistribucion` | VO | Instrucción que indica cómo distribuir el valor del anticipo. `List<DestinoDeNegocio>` (invariante I2: suma = 100%). |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  Anticipo (Agregado)                                          │
│                                                              │
│  ○ InformacionTercero    ○ MedioDePago    ○ ValorMonetario   │
│  ○ justificacion         ○ saldoPendiente                    │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Instrucciones de Distribución (VO)                     │  │
│  │                                                        │  │
│  │  ○ Valor anticipo → { VTA-001: 60%, ADM-001: 40% }    │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ○ = Value Object (sin ID)                                   │
└──────────────────────────────────────────────────────────────┘
```

### Value Objects compartidos

`InformacionTercero`, `MedioDePago` y `ValorMonetario` son Value Objects reutilizados por los tres agregados. Cada agregado los incluye en su composición pero la definición es la misma — evita duplicación de estructuras de datos sin acoplar los agregados.

### Servicio de dominio: ServicioDeConciliacion

La conciliación es la operación que vincula OxpComercio y OxpExtracto. No pertenece a ninguno de los dos — es un **domain service** que coordina la transición de estado en ambos streams:

1. Carga la instancia de `OxpComercio` (stream `oxp-comercio-{id}`)
2. Carga la instancia de `OxpExtracto` (stream `oxp-extracto-{id}`)
3. Valida precondiciones sobre ambos agregados
4. Emite `OxpComercioCompensada` → stream de OxpComercio
5. Emite `VinculacionRealizada` → stream de OxpExtracto

Dos streams, consistencia eventual, coordinados por el domain service.

### Servicio de dominio: ServicioDeRegularizacion

La regularización es la operación que vincula un Anticipo con una OxpComercio entrante. Es un **domain service** que coordina la transición de estado entre ambos streams:

1. Detecta que la OxpComercio entrante tiene un tercero con anticipo vigente
2. Carga la instancia de `Anticipo` (stream `anticipo-{id}`)
3. Carga la instancia de `OxpComercio` (stream `oxp-comercio-{id}`)
4. Valida precondiciones (anticipo vigente, mismo tercero, saldo suficiente)
5. Emite `AnticipoRegularizado` → stream del Anticipo (reduce saldo)
6. La OxpComercio queda con referencia al anticipo para que `lineasParaTraduccion()` incluya la información de amortización

Dos streams, consistencia eventual, coordinados por el domain service.

### Relaciones entre agregados

```
Anticipo ──────(1:N)──────► OxpComercio ──────(N:1)──────► OxpExtracto
   │                                                            │
   └──────────(1:1)────────► PartidaExtracto (del OxpExtracto) ─┘
```

- 1 Anticipo puede ser regularizado por N OxpComercio (regularización parcial).
- 1 Anticipo cubre 1 partida del extracto (vínculo permanente) `[R08]`.
- N OxpComercio se vinculan a 1 OxpExtracto (conciliación).
- Una OxpComercio solo puede vincularse a un único OxpExtracto (invariante I7).
- Un OxpExtracto puede recibir N vinculaciones.
- Una OxpComercio que regulariza anticipo **no** se vincula al OxpExtracto (va a Pagada, no a Compensada).
- La vinculación es por referencia (ID), no por composición. Cada agregado mantiene su propio stream de eventos independiente.

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
┌──────────┐
│ Causada  │
└────┬─────┘
     │
     ├──(OxpComercioCompensada)──► ┌──────────┐
     │                              │Compensada│ ■
     │                              └──────────┘
     │
     └──(OxpComercioPagada)──────► ┌──────────┐
                                    │ Pagada   │ ■
                                    └──────────┘
```

**Notas:**
- `Pendiente` es el estado inicial para toda radicación (tanto tipo Compra como Devolución).
- `Compensada`: la OxpComercio fue vinculada a un extracto durante la conciliación (flujo tarjeta crédito). Coordinada por `ServicioDeConciliacion`.
- `Pagada`: la obligación fue liquidada financieramente por otra vía — actualmente por regularización de anticipo (el anticipo ya cubrió el desembolso); a futuro por medios de pago directos.
- La transición `Devuelta → Pendiente` ocurre vía `OxpComercioCorregida`.
- Si `[R02]` está configurada como automática, `OxpComercioRadicada` puede emitir `OxpComercioConfirmada` inmediatamente.
- **Tipo Devolución:** Sigue el camino Pendiente → Confirmada → Causada → Compensada. No aplica Devuelta.

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
- La transición a `Conciliado` requiere 100% de partidas vinculadas, cubiertas por anticipo, marcadas como disputa, o clasificadas como cargos adicionales `[R06]`.
- Las partidas en disputa y las cubiertas por anticipo cuentan como resueltas para este umbral.
- `ExtractoPagado` es confirmado por SincoA&F; OXP no ejecuta pagos `[R18]`.

### 4.3. Anticipo

```
┌──────────┐  AnticipoRegularizado (parcial)  ┌──────────┐
│          │ ─────────────────────────────────► │          │
│ Vigente  │                                    │ Vigente  │ (saldo reducido)
│          │ ◄─────────────────────────────────  │          │
└────┬─────┘                                    └──────────┘
     │
     │ AnticipoRegularizado (saldo = 0)
     ▼
┌───────────┐
│Amortizado │ ■
└───────────┘
```

**Notas:**
- `Vigente` es el estado desde el registro hasta que el saldo pendiente llega a 0.
- Cada `AnticipoRegularizado` reduce el saldo. Cuando saldo = 0: transiciona a Amortizado.
- `AnticipoAmortizado` es confirmación externa de SincoA&F (reclasificación contable). Sin cambio de estado — similar a la confirmación de causación en OxpComercio.
- `AlertaPlazoAnticipoVencido` es evento informativo sin cambio de estado `[R04b]`.

---

## 5. Catálogo de eventos

### 5.1. Radicación

#### OxpComercioRadicada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una obligación individual (compra o devolución) realizada con tarjeta corporativa ha sido registrada en el sistema con sus soportes documentales. |
| **Agregado** | OxpComercio |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Pendiente. Si `[R02]` está configurada como automática: Confirmada. |
| **Precondiciones** | Soporte documental adjunto (PDF, imagen o XML). Si es XML, datos extraídos de SincoRE. Validación de unicidad superada `[R26]`. |
| **Información capturada** | Tipo de OXP (Compra o Devolución), tercero (NIT, razón social), fecha de transacción, valor en moneda original (positivo para compra, negativo para devolución), moneda, TRM del día si aplica `[R05b]`, valor en moneda funcional, número de soporte/factura, medio de pago (tarjeta), conceptos (gasto/costo + impuestos + retenciones), distribución de costos si aplica `[R05c]`, soportes documentales adjuntos. Si tipo = Devolución: referencia a OxpComercio original (si disponible). |
| **Efectos** | Si XML: extracción automática de datos desde SincoRE. Si requiere formalización: notificación a SincoADPRO `[R20]`. Si supera monto máximo: alerta informativa `[R05]`. Si compra del exterior: alerta de plazo DIAN `[R01]`. Si `[R02]` automática: emite `OxpComercioConfirmada`. Si tipo = Devolución: inicia búsqueda de OxpComercio original para asociación posterior (`DevolucionAsociada`). |

#### OxpComercioExteriorRadicada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una compra del exterior ha sido radicada con valoración en moneda extranjera. Especialización de `OxpComercioRadicada`. |
| **Agregado** | OxpComercio |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Pendiente. |
| **Precondiciones** | Transacción en moneda diferente a la moneda funcional. TRM del día disponible `[R05b]`. |
| **Información capturada** | Moneda de origen, valor en moneda de origen, TRM del día de la transacción, valor convertido a moneda funcional. Además de toda la información de `OxpComercioRadicada`. |
| **Efectos** | Alerta de plazo DIAN para documento soporte electrónico (6 días hábiles) `[R01]`. |

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

#### DevolucionAsociada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una OxpComercio de tipo Devolución ha sido vinculada con la OxpComercio original de tipo Compra. |
| **Agregado** | OxpComercio |
| **Estado previo** | Pendiente (tipo = Devolución). |
| **Estado resultante** | (sin cambio de estado; es vinculación de referencia). |
| **Precondiciones** | OxpComercio original (tipo = Compra) existe en el sistema `[R04]`. |
| **Información capturada** | Referencia a OxpComercio original, fecha de asociación. |
| **Efectos** | Devolución queda vinculada para trazabilidad. La referencia se preserva para la conciliación y la traducción contable. |

---

### 5.2. Anticipo

#### AnticipoRegistrado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se ha registrado un pago adelantado sin soporte documental. El dinero ya fue desembolsado o comprometido por el medio de pago, pero no se cuenta con la evidencia económica (factura/soporte). |
| **Agregado** | Anticipo |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Vigente. |
| **Precondiciones** | Ausencia de soporte documental `[R03]`. Usuario con perfil habilitado para generar anticipos `[R22]`. |
| **Información capturada** | Tercero (NIT, razón social), valor, medio de pago, fecha de transacción, justificación de ausencia de soporte. |
| **Efectos** | Inicia conteo de plazo para regularización `[R04b]` (default 30 días). Anticipo disponible para vinculación con partida de extracto `[R08]` y para regularización futura con OxpComercio. |

#### AnticipoRegularizado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El anticipo ha sido vinculado a una OxpComercio entrante que aporta los soportes documentales correspondientes, reduciendo el saldo pendiente del anticipo. |
| **Agregado** | Anticipo |
| **Estado previo** | Vigente. |
| **Estado resultante** | Vigente (si saldo > 0) o Amortizado (si saldo = 0). |
| **Precondiciones** | OxpComercio radicada para el mismo tercero. Saldo pendiente suficiente para el monto a regularizar. Coordinado por `ServicioDeRegularizacion`. |
| **Información capturada** | Referencia a OxpComercio vinculada, monto regularizado, saldo restante. |
| **Efectos** | Genera información estructurada para amortización contable `[R15]`. Si saldo = 0: anticipo cerrado. La OxpComercio vinculada transicionará a Pagada una vez alcance estado Causada y SincoA&F confirme la amortización. |

#### AnticipoAmortizado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable externo (SincoA&F) ha confirmado la reclasificación contable del saldo del anticipo a cuentas de gasto o costo definitivas. |
| **Agregado** | Anticipo |
| **Estado previo** | Amortizado (sin cambio de estado — es confirmación de efecto contable externo). |
| **Estado resultante** | (sin cambio de estado). |
| **Precondiciones** | SincoA&F ha procesado la reclasificación contable a partir de la información entregada por OXP. |
| **Información capturada** | Número de asiento de amortización, fecha de amortización. |
| **Efectos** | Emite `OxpComercioPagada` sobre la(s) OxpComercio que regularizaron este anticipo (si están en estado Causada). Cierra el ciclo contable del anticipo. |

#### AlertaPlazoAnticipoVencido

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El anticipo no ha sido regularizado dentro del plazo configurado. |
| **Agregado** | Anticipo |
| **Estado previo** | Vigente. |
| **Estado resultante** | (sin cambio de estado; es evento informativo). |
| **Precondiciones** | Plazo configurado excedido `[R04b]` (default 30 días, configurable por empresa). |
| **Información capturada** | Días de retraso, saldo pendiente, fecha límite original. |
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
| **Efectos** | Inicia conteo de plazo de conciliación `[R07]`. Aplica conciliación automática con patrones persistidos `[R09]`. Cada match exitoso dispara `ServicioDeConciliacion` que emite `VinculacionRealizada` + `OxpComercioCompensada`. |

#### VinculacionRealizada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una partida del extracto ha sido vinculada con una o más OxpComercio. Este evento registra el lado Extracto de la operación de conciliación coordinada por `ServicioDeConciliacion`. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliado. |
| **Estado resultante** | Parcialmente Conciliado. Si el 100% de partidas quedan resueltas, el agregado emite `ExtractoConciliado` automáticamente. |
| **Precondiciones** | OxpComercio causada(s). Tipo de vinculación válido: 1:1 o N:1. Si N:1: suma de OxpComercio dentro de tolerancia `[R10]`. Coordinado por `ServicioDeConciliacion`. |
| **Información capturada** | Tipo de vinculación (1:1 o N:1), referencia(s) a OxpComercio vinculada(s), partida del extracto, valor de diferencia (si existe), origen (automática o manual). |
| **Efectos** | `ServicioDeConciliacion` emite simultáneamente `OxpComercioCompensada` sobre cada OxpComercio vinculada. Si diferencia dentro de tolerancia: emite `AjustePorToleranciaGenerado` `[R10]`. Si OxpComercio en moneda extranjera con diferencia de TRM: emite `DiferenciaEnCambioDetectada` `[R10b]`. Persiste asociación de patrón comercio-descripción `[R09]`. |

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

#### ExtractoConciliado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El 100% de las partidas del extracto han sido vinculadas a OxpComercio, cubiertas por anticipo, clasificadas como cargos adicionales, o marcadas como partida en disputa. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliado. |
| **Estado resultante** | Conciliado. |
| **Precondiciones** | 100% de partidas resueltas `[R06]`. |
| **Información capturada** | Resumen de conciliación: total de partidas, partidas automáticas vs. manuales, partidas en disputa, partidas cubiertas por anticipo, conceptos de ajuste generados. |
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
| **Estado resultante** | (partida cerrada; compensada contra reverso bancario). |
| **Precondiciones** | Línea de "Reverso Bancario" identificada en un extracto (puede ser de un período futuro) `[R06b]` `[R10c]`. |
| **Información capturada** | Referencia al extracto y línea de reverso bancario, fecha de resolución. |
| **Efectos** | Cierra el ciclo de la partida en disputa. |

#### PartidaDisputaReclasificada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se ha identificado el gasto real detrás de la partida en disputa y se ha vinculado con una OxpComercio radicada, mediante reclasificación contable. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Partida marcada como disputa. |
| **Estado resultante** | (partida cerrada; vinculada a OxpComercio). |
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
| **Precondiciones** | Usuario con rol de Confirmador `[R11b]`. Solo aplica para tipo = Compra. |
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
| **Efectos** | Integración saliente: causación individual enviada a SincoA&F (JSON). Si tipo = Devolución: el documento generado por SincoA&F es una nota crédito `[R16]`. Si la OxpComercio regulariza un anticipo: la información de amortización se incluye en la integración saliente `[R15]`. OxpComercio disponible para vinculación con extracto (si no regulariza anticipo) o para transición a Pagada (si regulariza anticipo). |

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

### 5.5. Compensación y Pago

#### OxpComercioCompensada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La OxpComercio ha sido vinculada a un OxpExtracto durante la conciliación. Estado operativo, no representa pago financiero. Emitido por `ServicioDeConciliacion` como contraparte de `VinculacionRealizada`. |
| **Agregado** | OxpComercio |
| **Estado previo** | Causada. |
| **Estado resultante** | Compensada (estado terminal). |
| **Precondiciones** | OxpComercio causada. Coordinado por `ServicioDeConciliacion`. Vinculación existente con partida de extracto (1:1 o N:1). |
| **Información capturada** | Referencia a OxpExtracto, partida del extracto vinculada, tipo de vinculación (1:1 o N:1). |
| **Efectos** | El agregado genera `lineasParaTraduccion()` como insumo que el servicio de Traducción Contable transforma y entrega a SincoA&F `[R17]`. Si la OxpComercio es en moneda extranjera y existe diferencia de TRM: emite `DiferenciaEnCambioDetectada` sobre el OxpExtracto `[R10b]`. Si la diferencia está dentro de tolerancia `[R10]`: emite `AjustePorToleranciaGenerado` sobre el OxpExtracto. |

#### OxpComercioPagada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La obligación ha sido liquidada financieramente a través de un anticipo que ya cubrió el desembolso. La OxpComercio aportó los soportes documentales para regularizar el anticipo; no necesita pasar por conciliación porque la partida del extracto ya fue cubierta por el anticipo. |
| **Agregado** | OxpComercio |
| **Estado previo** | Causada. |
| **Estado resultante** | Pagada (estado terminal). |
| **Precondiciones** | OxpComercio causada. Anticipo vinculado amortizado (SincoA&F confirma reclasificación contable). No aplica conciliación con extracto. |
| **Información capturada** | Referencia al Anticipo regularizado, fecha de confirmación de amortización. |
| **Efectos** | Cierre operativo de la OxpComercio. La obligación fue liquidada financieramente a través del anticipo. |

#### ExtractoPagado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | SincoA&F ha confirmado la ejecución del pago financiero correspondiente al extracto. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Causado. |
| **Estado resultante** | Pagado (estado terminal). |
| **Precondiciones** | SincoA&F confirma ejecución del pago `[R18]`. OXP no ejecuta pagos; solo registra el estado. |
| **Información capturada** | Fecha de pago, referencia de pago de SincoA&F. |
| **Efectos** | Cierre operativo del ciclo del OxpExtracto. |

---

## 6. Tipos de concepto

El agregado `OxpComercio` tiene una única entidad interna (`ConceptoDeGasto`) que contiene su desglose fiscal como Value Objects (`DesgloseFiscal` → `Tributo`). La distribución de costos se gestiona mediante instrucciones separadas a nivel del agregado (ver Sección 3, reglas de consistencia). OXP captura la información de negocio; la traducción a lenguaje contable es responsabilidad del servicio de **Traducción Contable** en la frontera OXP → SincoA&F.

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
| **Traducción contable (frontera)** | El servicio de Traducción Contable deduce gasto financiero por diferencia en cambio (si TRM subió) o ingreso financiero (si TRM bajó). |

### AjustePorTolerancia

| Aspecto | Detalle |
|---------|---------|
| **Entidad del agregado** | OxpExtracto |
| **Tipo** | Entidad interna (una por cada vinculación con diferencia dentro de tolerancia). |
| **Aparece en** | Conciliación (al vincular con diferencia dentro de tolerancia). |
| **Información (dominio OXP)** | OxpComercio de origen, valor de la diferencia, dirección (extracto mayor o menor que OxpComercio) `[R10]`. |
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
| **Traducción contable (frontera)** | El servicio de Traducción Contable deduce cuenta de gasto financiero según subtipo. |

---

## 7. Invariantes del dominio

Las invariantes son restricciones estructurales que deben ser verdaderas en todo momento del ciclo de vida del dominio. A diferencia de las reglas de negocio (R01–R27), que pueden ser configurables y tener excepciones, las invariantes son absolutas.

| # | Invariante | Agregado | Referencia |
|---|-----------|----------|------------|
| I1 | **Unicidad de obligación:** No pueden existir dos OxpComercio con el mismo NIT + número de soporte dentro de la ventana de 24 meses. | OxpComercio | `[R26]` |
| I2 | **Integridad de distribución:** Para cualquier `InstruccionDistribucion` del agregado OxpComercio, la suma de sus `DestinoDeNegocio` es exactamente 100%. Se valida por cada instrucción individual (ya sea de un `ConceptoDeGasto` o de un `Tributo`). | OxpComercio | `[R05c]` |
| I3 | **Completitud de conciliación:** Un OxpExtracto en estado Conciliado tiene el 100% de sus partidas vinculadas a OxpComercio, cubiertas por anticipo, marcadas como disputa, o clasificadas como cargos adicionales. | OxpExtracto | `[R06]` |
| I4 | **Progresión de estados:** Cada agregado solo puede avanzar en su máquina de estados; no puede retroceder excepto por la transición explícita Devuelta → Pendiente en OxpComercio (vía corrección). | Ambos | — |
| I5 | **Consistencia de moneda:** Toda OxpComercio en moneda extranjera almacena tanto el valor en moneda de origen como el valor en moneda funcional. | OxpComercio | `[R05b]` |
| I6 | **Segregación de funciones:** El usuario que confirma una OXP no puede ser el mismo que la radicó (cuando está habilitada por empresa). | OxpComercio, OxpExtracto | `[R25]` |
| I7 | **Vinculación coherente:** Una OxpComercio solo puede estar vinculada a un único OxpExtracto. Un OxpExtracto puede tener N vinculaciones. | Inter-agregado | — |
| I8 | **Causalidad de anticipo:** Un anticipo solo puede regularizarse si está en estado Vigente y su saldo pendiente es suficiente para el monto a regularizar. | Anticipo | — |
| I9 | **Confirmación externa de causación:** Una OXP solo transiciona a estado Causada cuando el sistema externo (SincoA&F) confirma el registro. OXP no auto-declara la causación. | OxpComercio, OxpExtracto | — |
| I10 | **Consistencia de distribución:** Toda `InstruccionDistribucion` referencia un componente existente del agregado (`ConceptoDeGasto` o `Tributo`). Al agregar, eliminar o recalcular componentes, el agregado OxpComercio mantiene la coherencia aplicando la cadena de resolución: instrucción explícita → herencia del gasto padre → preferencia de empresa → destino único pendiente. | OxpComercio | `[R05c]` |
| I11 | **Saldo de anticipo no negativo:** El saldo pendiente de un Anticipo nunca puede ser negativo. La suma de todas las regularizaciones no puede superar el valor original del anticipo. | Anticipo | — |

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
| D1 | **OXP es un bounded context**, no un agregado. | Contiene múltiples agregados coordinados (OxpComercio, OxpExtracto, Anticipo) con ciclos de vida independientes. | DDD: bounded context como límite lingüístico y de responsabilidad. |
| D2 | **Dos agregados raíz:** OxpComercio y OxpExtracto. | No comparten estados, eventos ni transiciones. Solo comparten Value Objects. Streams de eventos independientes. | DDD: el agregado define un límite de consistencia transaccional. Análisis detallado en `Justificaciones/ModelarAgregados.md`. |
| D3 | **ServicioDeConciliacion** como domain service. | La conciliación modifica ambos agregados (OxpComercio → Compensada, OxpExtracto → VinculacionRealizada). No pertenece a ninguno — es coordinación. | DDD: domain service para operaciones que no pertenecen a un agregado. Event sourcing: consistencia eventual entre streams. |
| D4 | **ConceptoDeGasto** como única entidad interna de OxpComercio. | Impuestos y retenciones son cálculos derivados del gasto (no tienen vida propia, se reemplazan al recalcular). El gasto es la causa; los tributos son el efecto. | DDD: entidades para cosas con identidad y ciclo de vida; Value Objects para cálculos derivados e inmutables. |
| D5 | **DesgloseFiscal y Tributo** como Value Objects. | Se reemplazan completos al recalcular. No necesitan identidad. Un IVA sobre el mismo gasto con los mismos datos es el mismo cálculo. | POO: inmutabilidad para derivados. Evita desincronización entre datos almacenados y calculados. |
| D6 | **Distribución separada del concepto** (InstruccionDistribucion). | Cada componente (gasto o tributo) puede tener distribución diferente. Mezclar distribución con concepto cruza tres responsabilidades: qué se compró, qué tributos aplican, hacia dónde va. | Separación de responsabilidades. Cada objeto tiene una razón de cambio. |
| D7 | **Cadena de resolución** de distribución. | Las instrucciones dependen de los componentes que las originan. La cadena (explícita → herencia → empresa → pendiente) garantiza coherencia sin datos huérfanos. | Invariante I10. Consistencia del agregado. |
| D8 | **OXP no conoce el dominio contable.** | Sin cuentas contables, sin centros de costo, sin naturalezas débito/crédito. La traducción ocurre en la frontera (servicio de Traducción Contable). | DDD: anti-corruption layer. Cada contexto protege su lenguaje. |
| D9 | **DestinoDeNegocio con identificador estandarizado** (Shared Kernel). | `unidadOrganizacional` usa un código del catálogo organizacional que ambos contextos (OXP y Contabilidad) reconocen. OXP no sabe qué cuenta es; el traductor sí. | DDD: Shared Kernel para vocabulario compartido entre bounded contexts. |
| D10 | **Valores totales como comportamiento calculado**, no como dato almacenado. | `valorBruto()`, `totalImpuestos()`, `totalRetenciones()`, `valorNeto()` se derivan de los componentes. Una sola fuente de verdad. | POO: evitar redundancia. Event sourcing: el replay reconstruye componentes, los totales se derivan. Para consultas rápidas: read model / projection. |
| D11 | **lineasParaTraduccion()** como comportamiento del agregado. | Pre-computa líneas planas (componente × destino) con valor distribuido. El servicio de Traducción Contable recibe líneas listas y solo mapea, sin entender distribuciones ni herencias. | Separación de responsabilidades. OXP prepara; el traductor traduce. |
| D12 | **Devolución como variante de OxpComercio** (`TipoOxpComercio`), no como agregado separado. | Comparte 70%+ del comportamiento (misma estructura, misma máquina de estados, misma conciliación). Solo 1 evento exclusivo (`DevolucionAsociada`). Separar duplicaría Confirmada, Causada y Compensada sin beneficio. | DDD: mismo límite de consistencia. La naturaleza contable (factura vs nota crédito) no es responsabilidad de OXP (D8). |
| D13 | **Anticipo como agregado independiente**, no como estado de OxpComercio. | El anticipo tiene un ciclo de vida simple e independiente (quién + cuánto + medio de pago), diferente al de OxpComercio (soporte, conceptos, desglose fiscal, distribución). La regularización es una operación inter-agregado coordinada por `ServicioDeRegularizacion`. La partida del extracto se vincula al anticipo de forma permanente `[R08]`; la OxpComercio que regulariza no pasa por conciliación. | DDD: agregados diferentes para ciclos de vida diferentes. El anticipo no "se convierte" en OxpComercio — son dos objetos que se vinculan. |
| D14 | **Pagada como segundo estado terminal de OxpComercio.** | Actualmente se alcanza cuando una OxpComercio regulariza un anticipo (el pago ya ocurrió vía anticipo). Diseñado para soportar futuros medios de pago donde la OxpComercio se pague directamente sin pasar por extracto. | Extensibilidad. El estado terminal refleja la realidad del negocio: la obligación fue liquidada financieramente. |

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
