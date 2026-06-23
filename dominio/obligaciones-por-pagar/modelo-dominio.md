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
11. [Pendientes por definir](#11-pendientes-por-definir)

---

## 1. Propósito y relación con otros documentos

Este documento especifica el comportamiento interno del dominio OXP mediante eventos, transiciones de estado, precondiciones, invariantes y la información de negocio que cada evento captura. Su objetivo es servir como puente entre la definición funcional y la implementación técnica.

| Documento | Alcance | Relación |
|-----------|---------|----------|
| `definicion-alcance.md` | QUÉ hace el sistema | Fuente de verdad para glosario, actores, flujos y reglas (R01–R37). No se duplica aquí. |
| **Este documento** | CÓMO se comporta el dominio | Eventos, transiciones, precondiciones, invariantes, tipos de concepto. |
| `guias-de-modelado/modelar-agregados.md` | POR QUÉ múltiples agregados | Análisis comparativo de agregado único vs. múltiples agregados desde event sourcing. |
| EventCatalog (fase 2) | Catalogación técnica | Consumirá este documento como especificación de entrada durante la implementación. |

Las reglas de negocio se referencian como `[R##]` y su texto completo vive en `definicion-alcance.md`, Sección 6.

---

## 2. Convenciones del documento

### Nomenclatura

- **Eventos:** PascalCase en español. Ej: `OxpComercioRadicada`, `ExtractoConciliado`.
- **Referencias a reglas:** `[R##]` remite a `definicion-alcance.md`, Sección 6.
- **Premisas de negocio:** `[P##]` remite a Sección 10 de este documento.
- **Sugerencias de implementación:** `[SI##]` — recomendaciones técnicas que complementan y clarifican una definición del dominio, orientando cómo llevarla a código. No son restricciones del modelo de dominio ni decisiones de arquitectura.
- **Fase de implementación:** `[F1]` Comercio + Extracto (implementación inmediata). `[F2]` Ampliación de tipos (fase futura). Definido en `[D24]`.
- **Agregados:** OxpComercio, OxpExtracto, Anticipo, Devolucion. Nombres en PascalCase sin tildes por compatibilidad con código fuente; corresponden a los términos del glosario canónico (`definicion-alcance.md`, Sección 2) — ej: `OxpComercio` = "OXP de Comercio".
- **Alcance del glosario canónico:** Los domain services, entidades internas y value objects son artefactos del modelo de dominio — no requieren entrada en el glosario canónico (`definicion-alcance.md`). Ej: `ServicioDeRegularizacion`, `InstruccionDistribucion`.
- **Estados OxpComercio:** Pendiente, Confirmada, Causada, Pagada, Devuelta.
- **Estados Devolucion:** Pendiente, Confirmada, Causada.
- **Estados OxpExtracto:** Pendiente, Parcialmente Conciliada, Conciliada, Confirmada, Causada, Pagada.
- **Género de estados:** Los agregados OXP (OxpComercio, OxpExtracto) usan femenino porque representan "la obligación por pagar". Devolucion usa femenino ("la devolución"). Anticipo usa masculino ("el anticipo").
- **Estados Anticipo:** Vigente, Confirmada, Causada, Pagado, Regularizado, Cerrado, Reversado.
- **Referencias cruzadas a otros sub-dominios:** `[D##-Xxx]` refiere a una decisión del sub-dominio indicado. Ej: `[D9-Imp]` refiere a la decisión D9 del modelo de Impuestos (`dominio/impuestos/modelo-dominio.md`).

### Template de evento

Cada evento del catálogo (Sección 5) se documenta con la siguiente estructura:

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Qué ocurrió en términos de negocio. |
| **Causalidad** | _(Solo si no es directa.)_ Derivado por transición / Derivado por configuración / Efecto inter-agregado / Evento compensatorio. Ver Causalidad entre eventos. |
| **Agregado** | OxpComercio / OxpExtracto / Anticipo / Devolucion. |
| **Estado previo** | Estado(s) desde los que puede emitirse. |
| **Estado resultante** | Estado al que transiciona la entidad. |
| **Precondiciones** | Condiciones requeridas. Ref. a reglas: [R##]. |
| **Información capturada** | Datos de negocio que el evento registra (no campos de BD). |
| **Efectos** | Integraciones salientes, alertas u otros eventos derivados. |

### Diagramas

Las máquinas de estado usan notación ASCII. Los estados terminales se marcan con `■`. Las transiciones se etiquetan con el nombre del evento entre paréntesis.

### Causalidad entre eventos

Cuando un evento produce otro evento, se distinguen cuatro tipos de causalidad:

| Tipo | Alcance | Mecanismo | Ejemplo |
|---|---|---|---|
| **Evento derivado por transición** | Mismo agregado, mismo append | El agregado evalúa una condición de estado y emite un segundo evento en la misma operación. | `PagoOxpComercioViaExtractoAplicado` reduce `saldoPorPagar()`; si llega a 0, emite `OxpComercioPagada`. `VinculacionRealizada` resuelve una partida; si 100% resueltas, emite `ExtractoConciliado`. |
| **Evento derivado por configuración** | Mismo agregado, condicional | El agregado emite un segundo evento solo si una regla de negocio configurable lo habilita. | `OxpComercioRadicada` emite `OxpComercioConfirmada` si `[R02]` está configurada como automática. `OxpComercioConfirmada` emite `OxpComercioCausada` si `[R12]` está configurada como automática. |
| **Efecto inter-agregado** | Múltiples streams, consistencia eventual | Un domain service coordina la emisión de eventos en streams de agregados diferentes. | `ServicioDeConciliacion` emite `VinculacionRealizada` (OxpExtracto) + `PagoOxpComercioViaExtractoAplicado` (OxpComercio). `ServicioDeAplicacionDevolucion` emite `DevolucionConfirmada` (Devolucion) + evento de efecto en el agregado OXP origen. |
| **Evento compensatorio** | Múltiples streams, reversión por fallo | Un domain service emite un evento que revierte el efecto de un paso anterior cuando un paso posterior falla. Solo se dispara por fallo del proceso — nunca por operación de negocio directa. Cada domain service documenta su tabla de compensación (ver Sección 3). | Si `PagoOxpComercioViaExtractoAplicado` (paso 5) falla permanentemente después de `VinculacionRealizada` (paso 4), el proceso emite `VinculacionRevertida` → stream OxpExtracto. |

### Tipos de cruce: `reversa` vs `revertido`

Las entidades de cruce parcial (`PagoAplicado`, `CrucePagoAplicado`, `CrucePagoExtractoAplicado`, `CruceRegularizacionAplicada`) usan dos tipos con semántica diferente para contrarrestar un cruce anterior:

| Tipo | Origen | Semántica | Reglas de negocio | Ejemplo |
|------|--------|-----------|-------------------|---------|
| `reversa` | Operación de negocio | Hecho de dominio: una devolución reversa totalmente un anticipo. Decisión del negocio. | Sí — solo desde Vigente o Confirmada (estados pre-causación), sin cruces previos, reversa total exclusivamente. | `AnticipoReversado` crea `CrucePagoAplicado` tipo reversa y `CruceRegularizacionAplicada` tipo reversa. |
| `revertido` | Fallo de saga `[SI3]` | Hecho técnico-operativo: un paso del domain service falló permanentemente y se deshace el efecto del paso anterior. No es una decisión de negocio. | No — es mecánico, solo contrarresta lo que se hizo. | `RegularizacionRevertida` crea `CruceRegularizacionAplicada` tipo revertido. |

**Mapeo de eventos → tipo `revertido`:**

| Entidad | Agregado | Evento que lo crea |
|---------|----------|--------------------|
| `PagoAplicado` tipo `revertido` | OxpComercio | `PagoOxpComercioViaAnticipoRevertido`, `PagoOxpComercioViaDevolucionRevertido` |
| `CrucePagoExtractoAplicado` tipo `revertido` | OxpExtracto | `PagoExtractoViaDevolucionRevertido` |
| `CruceRegularizacionAplicada` tipo `revertido` | Anticipo | `RegularizacionRevertida` |

**Mapeo de eventos → tipo `reversa`:**

| Entidad | Agregado | Evento que lo crea |
|---------|----------|--------------------|
| `CrucePagoAplicado` tipo `reversa` | Anticipo | `AnticipoReversado` |
| `CruceRegularizacionAplicada` tipo `reversa` | Anticipo | `AnticipoReversado` |

Nota: OxpComercio y OxpExtracto no tienen cruces tipo `reversa` actualmente — ver `[PD2]`.

**Nota sobre el naming `pago_sincoa`:** El tipo `pago_sincoa` en `CrucePagoExtractoAplicado` mantiene su nombre por trazabilidad histórica con el destino legacy SincoA&F. Semánticamente representa cualquier pago confirmado por el sistema contable, independientemente del destino físico configurado. Cambiar el nombre tendría costo alto y bajo beneficio funcional.

Ambos tipos preservan la inmutabilidad del cruce original — no se modifica el registro anterior, se agrega un nuevo registro que lo contrarresta.

### Precisiones terminológicas

| Término | Contexto | Significado en este documento |
|---------|----------|-------------------------------|
| Conciliación | Proceso | Operación coordinada por `ServicioDeConciliacion` que vincula partidas del extracto con OxpComercio e incluye ajustes por tolerancia y diferencia de cambio. |
| Conciliada | Estado OxpExtracto | 100% de partidas resueltas — invariante I3. |
| Parcialmente Conciliada | Estado OxpExtracto | Progreso parcial durante el proceso de conciliación; al menos una partida resuelta pero no todas. |

---

## 3. Bounded Context y Agregados

### OXP como Bounded Context

OXP (Obligaciones por Pagar) es un **bounded context** — no un agregado. Contiene múltiples agregados coordinados que en conjunto gestionan el ciclo de vida de las obligaciones originadas en medios de pago corporativos, junto con el **registro propio del Proveedor** — el rol del tercero de OXP en el modelo de bodega (replanteamiento #31, issue #38).

### Clasificación de capacidades

El bounded context de OXP agrupa capacidades con distinto nivel de madurez. Esta clasificación no implica separación en bounded contexts — todas conviven dentro del mismo BC — pero establece prioridad de implementación: las capacidades F2 requieren el núcleo F1 operativo. `[D24]`

| Nivel | Capacidades | Agregados / Servicios | Fase |
|---|---|---|---|
| **Núcleo transaccional** | Obligaciones individuales, obligaciones consolidadas, anticipos, devoluciones, conciliación, regularización, aplicación de devoluciones | OxpComercio, OxpExtracto, Anticipo, Devolucion, ServicioDeConciliacion, ServicioDeRegularizacion, ServicioDeAplicacionDevolucion | `[F1]` |
| **Registro del tercero** | Registro de proveedores con validación empaquetada, emisión del evento estándar de rol hacia la bodega de Terceros, aplicación automática de sus decisiones (señal global, correcciones) | Proveedor | `[F1]` |
| **Configuración** | Catálogo de gasto directo, clasificación inteligente de origen | CatalogoGastoDirecto | `[F1]` |
| **Ampliación** | Obligaciones de caja menor (fondo fijo, rendición, reembolso) | OxpCajaMenor *(por especificar)* | `[F2]` |

```
┌───────────┐   ┌───────────┐   ┌───────────┐
│  SincoRE  │   │ Servicio  │   │  Carga    │
│   (XML)   │   │ extracción│   │  manual   │
│           │   │ (PDF/img) │   │           │
└─────┬─────┘   └─────┬─────┘   └─────┬─────┘
      │               │               │
      └───────────────┼───────────────┘
                      │ datos extraídos
                      ▼
              ┌──────────────────┐
              │  Clasificación   │
              │  inteligente     │
              │  [D23] [R36]     │
              └────────┬─────────┘
                       │
                                    ┌──────────────────┐
                                    │    Impuestos     │
                                    │  (sub-dominio    │
                                    │   transversal)   │
                                    └────────┬─────────┘
                              solicitud de cálculo (sínc.)
                              confirmación (asínc.) [D22]
                                             │
┌────────────────────────────────────────────┼─────────────────────────────────┐
│                   Bounded Context: OXP     │                                 │
│                                            ▼                                 │
│  ┌──────────────┐                                  ┌──────────────────┐     │
│  │  OxpComercio │◄──[ServicioDeConciliacion]──────►│   OxpExtracto    │     │
│  └──────────────┘                                  └──────────────────┘     │
│        ▲     ▲                                          ▲          ▲        │
│        │     │                                          │ cubre    │        │
│        │     └───[ServicioDeRegularizacion]───►┌────────┴───────┐  │        │
│        │                                      │    Anticipo    │  │        │
│        │                                      └────────┬───────┘  │        │
│        │                                               ▲          │        │
│        │                                    crea (excedente)      │ ajuste │
│        │                                    / reversa             │ sobre  │
│        │                                               │          │        │
│        └───[ServicioDeAplicacionDevolucion]──►┌─────────┴──────┐  │        │
│             espejo de                         │   Devolucion   │──┘        │
│                                               └────────────────┘           │
└──────────────────────────────────────────────────────────────────────────────┘
                       │ causaciones (líneas de traducción)
                       │ confirmaciones de pago entrantes
                       ▼
              ┌──────────────────────┐
              │  Sistema Contable    │
              │  (sub-dominio        │
              │   Contabilidad)      │
              └──────────────────────┘
```

**Nota sobre el destino contable:** El sub-dominio Contabilidad actúa como punto único de entrega de las causaciones. El destino físico donde quedan registrados los asientos es configurable por empresa (sistema contable propio del ERP, SincoA&F como sistema legacy del ecosistema SincoERP, u otros sistemas externos).

### Flujos de integración con Impuestos

OxpComercio interactúa con el sub-dominio de Impuestos mediante dos operaciones formalizadas en `[D22]`: solicitud de cálculo (síncrona) y confirmación (asíncrona). Los siguientes diagramas muestran el recorrido completo desde el catálogo de conceptos hasta el registro tributario inmutable, para los dos escenarios que puede tener OXP.

**Flujo A — Gasto directo (originado en OXP):**

```
  Usuario                  OXP                        Impuestos
    │                       │                            │
    │ 1. Selecciona tipo    │                            │
    │    de gasto del       │                            │
    │    CatalogoGasto-     │                            │
    │    Directo de OXP     │                            │
    │    + tercero + monto  │                            │
    │ ─────────────────────>│                            │
    │                       │                            │
    │                       │  2. Resuelve desde         │
    │                       │     catálogo propio:       │
    │                       │     clasificTrib + concPago│
    │                       │                            │
    │                       │  3. Crea OxpComercio       │
    │                       │     subDominioOrigen: "OXP"│
    │                       │     ConceptoDeGasto con    │
    │                       │     referenciaOrigen:      │
    │                       │     "LIC-SW"               │
    │                       │                            │
    │                       │  4. Solicita cálculo       │
    │                       │     (síncrono)             │
    │                       │ ──────────────────────────>│
    │                       │                            │
    │                       │  5. DesgloseFiscal         │
    │                       │     propuesto              │
    │                       │ <──────────────────────────│
    │                       │                            │
    │  6. Muestra desglose  │                            │
    │ <─────────────────────│                            │
    │                       │                            │
    │  7. Confirma OXP      │                            │
    │ ─────────────────────>│                            │
    │                       │                            │
    │                       │  8. Comando confirmación   │
    │                       │     efectoFiscal: gravamen │
    │                       │     (asíncrono)            │
    │                       │ ──────────────────────────>│
    │                       │                            │
    │                       │                            │ 9. Crea Registro
    │                       │                            │    Tributario
    │                       │                            │    inmutable
```

**Flujo B — Desde módulo de gestión (ej: Compras):**

```
  Compras              OXP                        Impuestos
    │                   │                            │
    │ 1. Confirma       │                            │
    │    factura.       │                            │
    │    Envía conceptos│                            │
    │    con clasifTrib │                            │
    │    y concPago ya  │                            │
    │    resueltos desde│                            │
    │    catálogo de    │                            │
    │    Compras        │                            │
    │ ─────────────────>│                            │
    │                   │                            │
    │                   │  2. Crea OxpComercio       │
    │                   │     subDominioOrigen:      │
    │                   │     "Compras" [SI5]        │
    │                   │     ConceptoDeGasto con    │
    │                   │     referenciaOrigen:      │
    │                   │     "MAT-HC-042"           │
    │                   │                            │
    │                   │  3. Solicita cálculo       │
    │                   │     (síncrono)             │
    │                   │ ──────────────────────────>│
    │                   │                            │
    │                   │  4. DesgloseFiscal         │
    │                   │     propuesto              │
    │                   │ <──────────────────────────│
    │                   │                            │
    │                   │  (usuario revisa/confirma) │
    │                   │                            │
    │                   │  5. Comando confirmación   │
    │                   │     efectoFiscal: gravamen │
    │                   │     (asíncrono)            │
    │                   │ ──────────────────────────>│
    │                   │                            │
    │                   │                            │ 6. Crea Registro
    │                   │                            │    Tributario
    │                   │                            │    inmutable
```

**Comparativa entre flujos:**

| Aspecto | Flujo A (gasto directo) | Flujo B (desde gestión) |
|---|---|---|
| Origen del concepto | Catálogo de gasto directo de OXP | Catálogo del módulo de gestión |
| Quién resuelve clasif. tributaria | OXP (desde su catálogo) | Módulo de gestión (desde su catálogo) |
| subDominioOrigen | "OXP" | "Compras", "Arrendamiento", etc. |
| Solicitud de cálculo | Idéntica | Idéntica |
| Confirmación a Impuestos | Idéntica | Idéntica |
| ConceptoDeGasto resultante | Misma estructura | Misma estructura |

### Integración con sub-dominio Contabilidad

OXP entrega los hechos económicos al sub-dominio Contabilidad mediante el contrato estandarizado de **líneas de traducción** (ver glosario del sub-dominio Contabilidad). Cada vez que un agregado de OXP causa contablemente (estado Causada), emite un evento cuyo efecto incluye las líneas de traducción del agregado más una etiqueta de **tipo de transacción contable** (`tipoTransaccion`) que permite al sistema contable seleccionar la plantilla de asiento adecuada. La etiqueta es metadato semántico del hecho económico — no implica que OXP conozca cuentas, naturalezas ni centros de costo (D8 sigue vigente). Ver `[D27]`.

**Mapeo canónico de eventos OXP → tipoTransaccion contable:**

| Evento OXP | tipoTransaccion emitido | Plantilla del inventario de Contabilidad |
|---|---|---|
| `OxpComercioCausada` | `causacion_gasto` | #1 Causación de obligación |
| `ExtractoCausado` | `causacion_gasto` | #1 Causación de obligación |
| `AnticipoCausado` | `anticipo_a_proveedor` | #4 Anticipo a proveedor |
| `DevolucionCausada` (tipo Comercio) | `nota_credito_gasto` | #2 Nota crédito de proveedor |
| `DevolucionCausada` (tipo Extracto) | `nota_credito_gasto` | #2 Nota crédito de proveedor |
| `DevolucionCausada` (tipo Anticipo) | `reversa_anticipo` | #7 Reversa de anticipo (plantilla nueva — requiere registro en el inventario de Contabilidad) |
| `PagoOxpComercioViaAnticipoAplicado` (solo cuando la OXP ya está Causada — Caso B de `[D26]`) | `amortizacion_anticipo` | #8 Amortización de anticipo (plantilla nueva) |

**Notas sobre componentes que no son `tipoTransaccion` separados:**

- **Amortización del anticipo (doble naturaleza según el momento del cruce, ver `[D26]`):** **Caso A — cruce antes o durante la causación:** la amortización viaja como **tipo de componente** (`amortizacion_anticipo`) dentro de las líneas de la misma causación; OXP emite `tipoTransaccion = causacion_gasto` para `OxpComercioCausada`, regularice anticipo o no, y el motor de Contabilidad reclasifica internamente la cuenta de anticipos a proveedores. **Caso B — cruce después de causar la OXP:** la amortización se emite como **`tipoTransaccion` independiente** (`amortizacion_anticipo`) desde `PagoOxpComercioViaAnticipoAplicado`, porque la causación ya salió y no hay dónde embeberla. Así, `amortizacion_anticipo` es **tipo de componente** (Caso A) **y** `tipoTransaccion` propio (Caso B).
- **Diferencia en cambio:** viaja como **tipo de componente** dentro de las líneas del documento que la produjo — `OxpComercioCausada` si se generó al radicar en moneda extranjera, o `ExtractoCausado` si se generó en conciliación. No es `tipoTransaccion` separado. Esto preserva la trazabilidad al documento origen, necesaria para ajustes o notas crédito posteriores.
- **`AnticipoAmortizado`:** evento entrante (confirmación del sistema contable sobre la reclasificación que viajó dentro de `OxpComercioCausada`). No emite líneas de traducción ni `tipoTransaccion`.

**Catálogo canónico de `tipoComponente` emitidos por OXP:**

Cada línea de traducción que OXP emite lleva un `tipoComponente` cuyo nombre es **canónico** (snake_case) y coincide 1:1 con los componentes declarados en el catálogo de plantillas del sub-dominio Contabilidad (`dominio/contabilidad/datos-precargados/plantillas-de-asiento.*`). Para los tributos, el `tipoComponente` es el **código del tributo** tomado del desglose fiscal del sub-dominio Impuestos (no un genérico "impuesto"/"retención") — así Contabilidad puede acotar la cuenta por grupo del PUC `[D12 Contabilidad]`. La dirección contable (débito/crédito) no se codifica en el nombre: la determina la plantilla según el `tipoTransaccion`, por lo que los componentes de una nota crédito reutilizan el mismo nombre que en la causación (sin sufijo `_devuelto`).

| `tipoComponente` | Origen en OXP | Emitido en (`tipoTransaccion`) | Agregado |
|---|---|---|---|
| `gasto` | `ConceptoDeGasto` | `causacion_gasto` | OxpComercio |
| `iva` | `Tributo` IVA del `DesgloseFiscal` | `causacion_gasto`, `nota_credito_gasto` | OxpComercio, Devolucion (Comercio) |
| `inc` | `Tributo` INC del `DesgloseFiscal` | `causacion_gasto`, `nota_credito_gasto` | OxpComercio, Devolucion (Comercio) |
| `retefuente` | `Tributo` ReteFuente del `DesgloseFiscal` | `causacion_gasto`, `nota_credito_gasto` | OxpComercio, Devolucion (Comercio) |
| `reteiva` | `Tributo` ReteIVA del `DesgloseFiscal` | `causacion_gasto`, `nota_credito_gasto` | OxpComercio, Devolucion (Comercio) |
| `reteica` | `Tributo` ReteICA del `DesgloseFiscal` | `causacion_gasto`, `nota_credito_gasto` | OxpComercio, Devolucion (Comercio) |
| `concepto_devuelto` | `ConceptoDevuelto` | `nota_credito_gasto` | Devolucion (Comercio) |
| `cargo_financiero` | `CargoFinanciero` / `CargoFinancieroDevuelto` | `causacion_gasto`, `nota_credito_gasto` | OxpExtracto, Devolucion (Extracto) |
| `diferencia_en_cambio` | `AjustePorDiferenciaCambio` | `causacion_gasto` | OxpComercio, OxpExtracto |
| `ajuste_tolerancia` | `AjustePorTolerancia` | `causacion_gasto` | OxpExtracto |
| `cruce_obligacion` | `Vinculacion` (valor de la obligación cruzada, a TRM de radicación) | `causacion_gasto` | OxpExtracto |
| `amortizacion_anticipo` | regularización de anticipo `[D26]` | `causacion_gasto` (Caso A — componente) · `amortizacion_anticipo` (Caso B — `tipoTransaccion` propio) | OxpComercio |
| `anticipo` | valor del `Anticipo` | `anticipo_a_proveedor` | Anticipo |
| `reversa_anticipo` | `ReversaTotal` | `reversa_anticipo` | Devolucion (Anticipo) |

> **Nota de coordinación cruzada:** `amortizacion_anticipo` y `ajuste_tolerancia` se incorporaron como roles propios en el catálogo de plantillas de Contabilidad (`causacion_gasto`, v1.3) en el mismo cambio que esta canonización, preservando la coincidencia 1:1. Sus grupos del PUC quedan `porValidar` por consultor contable (ítems 9 y 10 de la revisión pendiente del catálogo). `cruce_obligacion` se incorporó como rol propio `CRUCE_OBLIGACION` (débito, grupo del PUC `["2205","2335"]`) en la misma plantilla `causacion_gasto` (issue #18), preservando la coincidencia 1:1.

**Campos de narración que OXP puebla en el contrato `LineaTraduccion`:**

El contrato `LineaTraduccion` se define en el sub-dominio Contabilidad. OXP, como consumidor, puebla dos campos de narración además del `tipoComponente` y el valor (ver `[R48]`/`[D13]` de Contabilidad):

- **`descripcionConcepto`** (por línea): narración del movimiento. OXP la incluye **solo** en las líneas de concepto de negocio — `gasto` (de `ConceptoDeGasto.descripcion`), `concepto_devuelto` (de `ConceptoDevuelto.descripcion`) y `anticipo`. Las líneas de tributo (`iva`, `retefuente`, ...), `cargo_financiero`, `diferencia_en_cambio`, `ajuste_tolerancia` y `amortizacion_anticipo` no la llevan (su cuenta es autodescriptiva — el catálogo de Contabilidad las marca `llevaDescripcionConcepto: false`).
- **`descripcion`** (a nivel del hecho económico): descripción general que OXP envía una vez por causación, si el documento la tiene; mapea al encabezado del borrador (`BorradorContable.descripcion`). Es opcional — si el documento no la tiene, OXP no la envía y el borrador queda sin descripción general.

Además, OXP puebla **`terceroPrincipal`** a nivel del hecho económico (no por línea): es el `InformacionTercero` de la raíz del agregado emisor — **el proveedor** en `OxpComercioCausada`, `AnticipoCausado` y `DevolucionCausada`; **el banco/emisor** en `ExtractoCausado`. El motor de Contabilidad lo usa como tercero de la **contrapartida** (ver paso 4 del `ServicioDeTraduccion` e issue #28). Esto resuelve el caso del extracto, cuyas líneas `cruce_obligacion` traen varios proveedores pero cuya contrapartida (CxP del banco/emisor, `[D29]`) no aparece en ninguna línea.

**Flujo bidireccional con el sub-dominio Contabilidad:**

```
  OXP                              Contabilidad                        Destino físico
   │                                    │                              (SincoA&F, propio, etc)
   │ 1. Emite *Causada con              │                                    │
   │    líneas de traducción +          │                                    │
   │    tipoTransaccion [D27]           │                                    │
   │ ──────────────────────────────────>│                                    │
   │                                    │                                    │
   │                                    │ 2. Motor de Traducción              │
   │                                    │    valida contrato + crea           │
   │                                    │    BorradorContable                 │
   │                                    │                                    │
   │                                    │ 3. Borrador resuelto (auto o        │
   │                                    │    asistido por el contador)        │
   │                                    │                                    │
   │                                    │ 4. EntregaContable envía            │
   │                                    │    al destino físico                │
   │                                    │ ──────────────────────────────────>│
   │                                    │                                    │
   │                                    │ 5a. Destino acepta:                 │
   │                                    │     EntregaAceptada con             │
   │                                    │     referenciaDestino               │
   │                                    │ <──────────────────────────────────│
   │                                    │                                    │
   │ 6a. OXP escucha EntregaAceptada    │                                    │
   │     y persiste referenciaDestino   │                                    │
   │     como número de asiento         │                                    │
   │ <──────────────────────────────────│                                    │
   │                                    │                                    │
   │                                    │ 5b. Destino rechaza:                │
   │                                    │     EntregaRechazada +              │
   │                                    │     BorradorRechazadoPorDestino     │
   │                                    │     (borrador vuelve a PENDIENTE)   │
   │                                    │                                    │
   │                                    │ 6b. Contador en Contabilidad        │
   │                                    │     corrige y reintenta             │
   │                                    │     (OXP NO cambia estado)          │
```

**Manejo de rechazos — responsabilidad:**

| Tipo de rechazo | Cuándo ocurre | Quién resuelve | Estado del documento OXP |
|---|---|---|---|
| Rechazo pre-borrador (`TIPO_TRANSACCION_SIN_PLANTILLA`, `LINEA_SIN_ROL_EN_PLANTILLA`, `REFERENCIA_ORIGEN_DUPLICADA_NO_REEMPLAZABLE`) | El motor de traducción no puede crear el borrador. No es evento de dominio en Contabilidad. | Equipo de producto (catálogo de plantillas) o consumidor (corregir contrato/idempotencia). Se materializa por infraestructura del bus (NACK + DLQ) según `[SI7]` del modelo de Contabilidad. | Causada (sin cambio). OXP conserva el hecho económico vía outbox (ver `[SI6]`) hasta confirmar procesamiento. |
| Rechazo post-borrador (`EntregaRechazada` + `BorradorRechazadoPorDestino`) | El destino físico rechaza la entrega. Sí es evento de dominio en Contabilidad. | El contador en el sub-dominio Contabilidad reabre el caso, corrige y reintenta. | Causada (sin cambio). OXP no es responsable de reaccionar — la corrección vive dentro del ciclo de Contabilidad. Ver `[D28]`. |

**Confirmación entrante (`EntregaAceptada`):** OXP escucha el evento y persiste la `referenciaDestino` (número de asiento contable externo) como información complementaria del documento causado. Es la confirmación de procesamiento exitoso y permite a OXP liberar el hecho económico del outbox local.

### Integración con Estructura Organizacional

Las distribuciones de OXP (`InstruccionDistribucion` → `DestinoDeNegocio`) imputan a **unidades organizacionales**, cuyo **dueño único es el sub-dominio de Estructura Organizacional (EO)**. OXP es **consumidor**: no crea, modifica ni gobierna el ciclo de vida de las unidades — solo las usa para distribuir e imputar. Por eso la unidad **no es un agregado de OXP** (a diferencia del `Proveedor` `[D30]`, que OXP sí co-gobierna): es un dato gobernado externamente que OXP mantiene como **copia local**. Esta copia es una proyección para **validar e imputar en el dominio** (`I24`/`[D34]`), **no una API de lectura para la UI**: la interfaz lee unidades directamente de EO (fuente de verdad). Ver `[D34]`, la decisión `[D15]` del modelo de EO y la guía `guias-de-modelado/datos-entre-dominios.md`.

**Copia local por eventos (`[SI8]`):** OXP mantiene una vista de lectura (read model) de las unidades del tenant con su estado vigente. **Precisión sobre su naturaleza:** no es una proyección del *event store* de OXP (como `[SI4]`/`[SI7]`, que se reconstruyen reproduciendo streams propios), sino un read model alimentado por un **consumidor de los eventos de integración que EO publica** (Event-Carried State Transfer) — los eventos viven en EO, OXP solo guarda la réplica. OXP **opera y valida siempre contra esa copia, nunca consulta a EO en caliente**: si EO está caído, OXP sigue operando.

Cómo se mantiene (incremental y reactivo, no un proceso programado):

- **Carga inicial (bootstrap):** al desplegar OXP o incorporar un tenant nuevo, la copia se llena pidiendo a EO el estado vigente (una foto) o reproduciendo su histórico de eventos.
- **Operación normal:** cada evento de ciclo de vida que EO publica y el bus entrega actualiza la tabla local al instante (push, evento por evento) — sin *polling* ni *cron*.
- **Reparación de respaldo (Capa 2 de la guía):** de fondo y fuera del camino crítico, OXP reconcilia su copia contra el punto de resincronización de EO (`[SI12]` de EO) ante un evento perdido o un desfase tras estar caído.

Garantías (provistas por la plataforma, `[D20]` — Marten/Wolverine *inbox*): **entrega al-menos-una-vez** + consumidor **seguro de repetir** (reprocesar un evento no duplica ni corrompe la copia); **orden por unidad** (un evento más viejo que el último aplicado a esa unidad se descarta, por versión/secuencia); **consistencia eventual** — la copia está al día con el retraso de propagación del bus, y es justo ese retraso el que motiva el *diferir* (`[D34]`): si el evento de creación aún no llegó, la unidad no está en la copia y el componente cae en destino pendiente hasta que aterrice.

**Eventos entrantes (EO → OXP), alimentan la copia local:**

| Evento de EO | Efecto en la copia local de OXP |
|---|---|
| `UnidadCreada` / `UnidadActivada` | Alta o activación de la unidad — queda disponible para distribuir |
| `UnidadSuspendida` / `UnidadInactivada` | La unidad deja de admitir distribuciones **nuevas** (`I24`); el histórico no se toca |
| `UnidadReactivada` / `UnidadReabierta` | Vuelve a admitir distribuciones nuevas (reactivación desde `Suspendida`; reapertura desde `Inactiva`) |
| `UnidadFusionada` / `UnidadDividida` / `UnidadTrasladada` | Reestructuración — OXP reasigna las referencias **futuras** según el evento; las distribuciones históricas conservan su unidad de entonces (comparabilidad) |

**Validación local — `I24`:** toda distribución nueva referencia una unidad que existe y está **Activa** en la copia local (gemela conceptual de `I22` con el Proveedor). El histórico y los documentos en curso no se afectan cuando una unidad se suspende, inactiva o reestructura — la restricción impide operaciones **nuevas**, no reescribe el pasado.

**Diferir por consistencia eventual (alineado con `[D15]` de EO):** la unidad se elige siempre de la fuente de verdad (la UI la lee de EO en vivo; las reglas de distribución se parametrizan contra EO), así que una unidad resuelta por la cadena (`I10`/`D6`/`D7`) **existe en EO**; lo único que puede faltar es que su evento de ciclo de vida aún no haya llegado a la copia local (desfase de propagación). En ese caso —o si la unidad está inactiva en la copia— ese componente cae en **destino único pendiente**: OXP **registra la obligación pero no causa esa parte** hasta que la unidad exista y esté activa en la copia. La resolución es automática: cuando llega `UnidadCreada`/`UnidadActivada` a la copia local, el pendiente se libera y la causación procede (consistencia eventual, sin espera humana ni consulta en caliente). OXP **nunca aproxima** con una unidad provisional o de tránsito: la unidad debe coincidir exacto con la contabilidad, y aproximarla desconciliaría operación y contabilidad (anti-patrón de la guía). Esto reusa el mecanismo de *destino pendiente* que OXP ya tiene — es la misma "incompletitud que bloquea la causación", ahora también disparada por un evento de unidad que aún no llega.

**Eventos salientes hacia EO:** **ninguno.** OXP solo consume los eventos de ciclo de vida de EO para mantener su copia local; no le devuelve nada. *(La señal de demanda `DemandaDeUnidadSenalada` que existió entre #48 y #56 se retiró en el #72: una vez la unidad se elige de la fuente de verdad, referenciar una unidad inexistente no ocurre en operación y la señal/bandeja quedaron sin disparador. Ver `[D34]`.)*

> El diagrama del flujo (copia local + diferir por consistencia eventual) y el **principio de capas** —la UI lee EO en vivo; la copia local es para validación del dominio, no una API de lectura para la UI— viven en el modelo de EO, sub-sección 3.8, y en la guía `datos-entre-dominios.md`; no se duplican aquí.

### Agregado: OxpComercio [F1]

- **Raíz:** Una obligación individual originada por compra con tarjeta corporativa (crédito o débito prepago).
- **Ciclo de vida:** Radicación → Confirmación → Causación → Pago(s) → Pagada.
- **Estado terminal:** Pagada (`saldoPorPagar() = 0`).
- **Stream de eventos:** `oxp-comercio-{id}`
- **Eventos propios:** 13.
- **subDominioOrigen:** Identifica el sub-dominio que originó la obligación (Compras, Arrendamiento, OXP, etc.). Deducido de la identidad del consumidor del comando `[SI5]` — no enviado por el consumidor. Inmutable.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `ConceptoDeGasto` | Gasto o costo de la obligación. Pueden existir múltiples conceptos idénticos como registros independientes. Invariante: mínimo 1 por OxpComercio. | Código, descripción, cantidad, valor, clasificacionTributaria (ref. catálogo Impuestos), conceptoPago (ref. catálogo Impuestos), referenciaOrigen (código del concepto en el catálogo del sub-dominio origen). Desglose fiscal: `DesgloseFiscal` (VO). |
| `PagoAplicado` | Cada registro representa un cruce parcial contra el valorNeto de la obligación. Inmutable una vez creado. Tipo: `extracto` (ref. a OxpExtracto + PartidaExtracto, valor cubierto; creado por `PagoOxpComercioViaExtractoAplicado`), `anticipo` (ref. a Anticipo, monto cubierto; creado por `PagoOxpComercioViaAnticipoAplicado`), `pago_directo` (ref. a pago confirmado por el sistema contable, valor pagado; creado por `PagoOxpComercioDirectoAplicado`), `devolucion` (ref. a Devolucion, monto cubierto; creado por `PagoOxpComercioViaDevolucionAplicado`), o `revertido` (ref. al PagoAplicado original, mismo valor; creado por evento de reversa de saga `[SI3]` ante fallo permanente — contrarresta el cruce original sin modificarlo). Los tipos extracto, anticipo, pago_directo y devolucion pueden coexistir (pagos mixtos). El tipo revertido preserva la inmutabilidad del registro original. | Tipo, referencia (varía por tipo), valor, fecha. |
| `ConstanciaAnticipoNoAplicable` | Registro del juicio humano "este anticipo no corresponde a esta OXP" (`[R38]`, `[I23]`). Inmutable una vez creado. Una constancia por anticipo dentro de la misma OXP. Se registra vía `RegistrarConstanciaAnticipoNoAplicable` en Pendiente, Confirmada o Causada mientras `saldoPorPagar()` > 0 — cubre también el anticipo que aparece después de la causación (`[D33]`). | anticipoId, motivo (obligatorio), usuarioId, fecha. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `InformacionTercero` | NIT, razón social — **copiado del `Proveedor` al radicar** (referencia `proveedorId`, `[D31]`) |
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
│  ○ SoporteDocumental     subDominioOrigen: "Compras" [SI5]   │
│                                                              │
│  Invariante: mínimo 1 ConceptoDeGasto                        │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ConceptoDeGasto #1 (Entidad)                           │  │
│  │  codigo · descripcion · cantidad · valor               │  │
│  │  clasificacionTributaria · conceptoPago                │  │
│  │  referenciaOrigen: "MAT-HC-042"                        │  │
│  │                                                        │  │
│  │  desgloseFiscal: (VO)                                  │  │
│  │   ○ Tributo { IVA, base: 600k, 19%, $114k }           │  │
│  │   ○ Tributo { ReteFte, base: 600k, 2.5%, $15k }       │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ConceptoDeGasto #2 (Entidad)                           │  │
│  │  codigo · descripcion · cantidad · valor               │  │
│  │  clasificacionTributaria · conceptoPago                │  │
│  │  referenciaOrigen: "LIC-SW"                            │  │
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

Al consultar la distribución efectiva de cualquier componente, el agregado aplica la siguiente cadena (en orden de prioridad; ver `[D36]`):

1. **Instrucción explícita** → Si el componente tiene instrucción propia (puesta por el usuario, o una sugerencia que el usuario confirmó), se usa.
2. **Herencia del gasto padre** → Si un Tributo no tiene instrucción propia, hereda la del ConceptoDeGasto al que pertenece.
3. **Reglas de preferencia de distribución (Nivel A — determinístico, `[D36]`)** → Conjunto de reglas configuradas en `CatalogoReglasDistribucion`, evaluadas por **especificidad** sobre los criterios `proveedor`, `tipo de gasto/clasificación` y `lugarEjecucion`: gana la regla que casa más criterios; si dos empatan en especificidad, desempata el orden de prioridad de criterios (proveedor > tipo de gasto > lugarEjecucion). La regla **sin criterios** (preferencia general de la empresa) es el default más general — preserva el comportamiento anterior. Resuelve **automáticamente** (no requiere confirmación: el usuario configuró la regla). Cubre el arranque en frío.
4. **Sugerencia por aprendizaje (Nivel B — no vinculante, `[D36]`/`[SI10]`)** → Si ninguna regla aplica, el sistema **pre-llena** con la unidad más frecuente del patrón histórico para esa combinación (`proveedor + tipo + lugarEjecucion`); el usuario **confirma o corrige** → pasa a instrucción explícita (nivel 1). No resuelve sola.
5. **Destino único pendiente** → Si ninguna regla resolvió y no se confirmó sugerencia, destino único al 100% pendiente de asignación por el usuario. **También** cae aquí si la unidad resuelta por la cadena no existe o no está Activa en la copia local de Estructura Organizacional (`I24`, `[D34]`): el componente queda pendiente y la causación de esa parte se difiere hasta que llegue el evento `UnidadActivada` — sin aproximar con una unidad provisional.

Los niveles 1–3 y 5 son **determinísticos y propios del agregado**; el nivel 4 es **asistencia de la capa de aplicación** (no vinculante, `[D23]`/`[D36]`). Cada distribución resuelta registra **con qué nivel** se asignó (trazabilidad). La sugerencia (nivel 4) y las reglas (nivel 3) operan siempre sobre la **copia local de unidades activas** (`[SI8]`); nunca proponen una unidad inexistente o inactiva.

**Comportamiento calculado del agregado:**

Los valores totales y las líneas de traducción no se almacenan — se derivan de los componentes. Esto garantiza una única fuente de verdad.

| Comportamiento | Descripción |
|---|---|
| `valorBruto()` | Suma del valor de todos los `ConceptoDeGasto`. |
| `totalImpuestos()` | Suma del valor de todos los `Tributo` de tipo impuesto dentro de los desgloses fiscales. |
| `totalRetenciones()` | Suma del valor de todos los `Tributo` de tipo retención dentro de los desgloses fiscales. |
| `valorNeto()` | `valorBruto()` + `totalImpuestos()` - `totalRetenciones()`. |
| `saldoPorPagar()` | `valorNeto()` - sum(`PagoAplicado`.valor). Derivado desde radicación (evoluciona con cambios en conceptos/tributos). Pagos solo se aplican desde estado Causada. Cuando `saldoPorPagar()` = 0 → transición a Pagada. |
| `lineasParaTraduccion()` | Pre-computa una lista plana de líneas, una por cada combinación (componente × destino de negocio), con el valor ya distribuido (valor × porcentaje). Cada línea incluye: `tipoComponente` canónico (`gasto`; los tributos con su código específico del desglose fiscal: `iva`, `inc`, `retefuente`, `reteiva`, `reteica`; `diferencia_en_cambio` y `amortizacion_anticipo` cuando apliquen — ver catálogo canónico de `tipoComponente`), identificador de unidad organizacional, valor distribuido y, en las líneas de concepto (`gasto`), la `descripcionConcepto` tomada de `ConceptoDeGasto.descripcion`. El servicio de Traducción Contable recibe estas líneas y solo necesita mapear `(tipoComponente + unidad organizacional) → cuenta contable`. No necesita entender distribuciones, herencias ni cadenas de resolución. |

### Agregado: OxpExtracto [F1]

- **Raíz:** Una obligación consolidada del período, originada por extracto bancario.
- **Ciclo de vida:** Radicación → Conciliación → Confirmación → Causación → Pago.
- **Estado terminal:** Pagada (`saldoPorPagar()` = 0, financiero — confirmado por el sistema contable).
- **Stream de eventos:** `oxp-extracto-{id}`
- **Eventos propios:** 20.

**Entidades internas:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `PartidaExtracto` | Línea individual del extracto bancario. Identidad: posición (índice) en el extracto — permite trazabilidad directa al documento fuente. | Posición, descripción, valor (en moneda funcional), monedaOriginal, valorOriginal, TRM (si aplica; solo para partidas en moneda extranjera `[R05d]`), fecha, estado (pendiente/vinculada/disputa/anticipo/devolucion/descartada). |
| `CargoFinanciero` | Cargo adicional del extracto (no corresponde a compra). | Subtipo (4x1000, cuota de manejo, intereses), valor, período. |
| `AjustePorDiferenciaCambio` | Ajuste generado al vincular OxpComercio en moneda extranjera. | OxpComercio origen, TRM radicación, TRM extracto, valor, clasificación (gasto/ingreso financiero). |
| `AjustePorTolerancia` | Ajuste generado al vincular con diferencia dentro de tolerancia. Inmutable una vez creado. Identidad: referencia trazable al par OxpComercio-partida que lo originó. Participa individualmente en `InstruccionDistribucion` y `lineasParaTraduccion()`. | OxpComercio origen, valor diferencia, dirección (extracto mayor/menor). |
| `Vinculacion` | Referencia que conecta una partida del extracto con una o más OxpComercio. | Ref. a OxpComercio, partida, tipo (1:1/N:1), origen (automática/manual), valorCruzado (valor de la obligación que esta vinculación salda, en moneda funcional a TRM de radicación de la OxpComercio — alimenta la línea `cruce_obligacion`), distribucionOrigen (la distribución por unidad organizacional del gasto de la OxpComercio cruzada, leída del agregado OxpComercio — insumo para que Contabilidad rinda la unidad organizacional del cruce según `[I33]` de Contabilidad). |
| `CoberturaAnticipo` | Referencia que conecta una partida del extracto con un Anticipo. Vínculo permanente. Contraparte en el agregado Anticipo: `CrucePagoAplicado` tipo extracto. Cada agregado mantiene su propia entidad (consistencia eventual). | Ref. a Anticipo, partida. |
| `CoberturaDevolucion` | Referencia que conecta una partida del extracto con una Devolucion. Vínculo permanente. Permite cubrir partidas que representan retorno de dinero durante la conciliación. | Ref. a Devolucion, partida. |
| `CrucePagoExtractoAplicado` | Cada registro representa un pago parcial contra el valor total del extracto. Inmutable una vez creado. Tipo: `pago_sincoa` (ref. de pago confirmado por el sistema contable, valor, fecha; creado por `PagoExtractoAplicado`), `devolucion` (ref. a Devolucion, monto cubierto, fecha; creado por `PagoExtractoViaDevolucionAplicado`), o `revertido` (ref. al CrucePagoExtractoAplicado original, mismo valor; creado por evento de reversa de saga `[SI3]` ante fallo permanente — contrarresta el cruce original sin modificarlo). Los tipos pago_sincoa y devolucion pueden coexistir. El tipo revertido preserva la inmutabilidad del registro original. (Ver Sección 2 — convenciones de tipos de cruce — para nota sobre el naming `pago_sincoa`.) | Tipo, referencia (varía por tipo), valor, fecha. |

**Value Objects:**

| Value Object | Contenido |
|---|---|
| `InformacionTercero` | NIT, razón social — identifica al **emisor/banco** del producto financiero; no es un `Proveedor` de OXP (entidad financiera, `[D31]`). Lleva el **origen** del dato (`del_archivo \| inferido_historico \| capturado_usuario`): cuando el archivo no lo trae, se resuelve por inferencia desde el extracto más reciente con el mismo número de tarjeta o por captura del usuario (`[D35]`, `[SI9]`) |
| `MedioDePago` | Tipo (crédito/débito prepago), número, entidad bancaria |
| `ValorMonetario` | Monto, moneda, TRM (si aplica), monto en moneda funcional |
| `InstruccionDistribucion` | Distribución por unidad organizacional. Indica cómo distribuir un valor entre unidades organizacionales. Se aplica al valor total del extracto y a cada componente individual (CargoFinanciero, AjustePorDiferenciaCambio o AjustePorTolerancia). Cada referencia tiene su propia distribución independiente. `List<DestinoDeNegocio>` (invariante I2: suma = 100%). |
| `DestinoDeNegocio` | Identificador de unidad organizacional (Shared Kernel con el contexto contable), porcentaje. Ej: `{ unidadOrganizacional: "FIN-001", porcentaje: 100 }`. Usado dentro de `InstruccionDistribucion`. |
| `SoporteDocumental` | Tipo (PDF, imagen, XML), referencia, datos extraídos. Soporte del extracto bancario y documentación de disputas. |

**Comportamiento calculado del agregado:**

Los valores totales del extracto no se almacenan — se derivan de los componentes. Esto garantiza una única fuente de verdad.

| Comportamiento | Descripción |
|---|---|
| `valorTotalExtracto()` | **Opera en la moneda del extracto** (moneda única del extracto si es homogéneo; moneda funcional si el extracto tiene partidas en monedas mixtas `[R05d]`). Suma del valor de todas las `PartidaExtracto` + suma de todos los `CargoFinanciero`. En extractos con monedas mixtas, las partidas en moneda extranjera ya fueron convertidas a moneda funcional durante la radicación. Excluye `AjustePorDiferenciaCambio` y `AjustePorTolerancia` porque estos se generan durante la conciliación como cálculos internos de OXP — no representan montos cobrados por el banco. El extracto bancario define qué se debe (partidas + cargos); los ajustes son correcciones contables que se causan junto con el extracto pero no modifican el monto por pagar. |
| `saldoPorPagar()` | **Opera en la moneda del extracto** (misma moneda que `valorTotalExtracto()`). `valorTotalExtracto()` - sum(`CrucePagoExtractoAplicado`.valor). Derivado desde radicación (evoluciona con partidas y cargos). Pagos de origen externo (confirmados por el sistema contable) solo se aplican desde estado Causada; pagos de origen interno (devolución) se aplican desde Confirmada. Cuando `saldoPorPagar()` = 0 → transición a Pagada. |
| `lineasParaTraduccion()` | Pre-computa una lista plana de líneas, una por cada combinación (componente × destino de negocio), con el valor ya distribuido (valor × porcentaje). `tipoComponente` canónico por componente: `CargoFinanciero` → `cargo_financiero`, `AjustePorDiferenciaCambio` → `diferencia_en_cambio`, `AjustePorTolerancia` → `ajuste_tolerancia` (ver catálogo canónico de `tipoComponente`). Estos componentes no llevan `descripcionConcepto` (su cuenta es autodescriptiva). Además, emite una línea **`cruce_obligacion`** por cada `Vinculacion`, con el **tercero** del proveedor de la `OxpComercio` cruzada, el **valor** de la obligación saldada (`Vinculacion.valorCruzado`, a TRM de radicación — la diferencia con el valor de la partida del extracto ya viaja como `diferencia_en_cambio`/`ajuste_tolerancia`) y la **distribución de origen** de la compra (`Vinculacion.distribucionOrigen`). El cruce no lleva `descripcionConcepto`. La unidad organizacional del cruce **la rinde Contabilidad según `[I33]`** (distribuida con la distribución de origen, consolidada en una unidad general, o sin unidad) — espeja cómo se registró la CxP de la causación original, garantizando el neteo. El servicio de Traducción Contable recibe estas líneas y solo necesita mapear `(tipoComponente + unidad organizacional) → cuenta contable`. |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  OxpExtracto (Agregado)                                      │
│                                                              │
│  ○ InformacionTercero    ○ MedioDePago    ○ ValorMonetario   │
│  ○ SoporteDocumental                                        │
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
│  │  descripcion · valor: 4.100.000 · fecha · estado: ant  │  │
│  │  monedaOrig: USD · valorOrig: 1.000 · TRM: 4.100      │  │
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
│  │  tipo: pago_sincoa · ref pago del sistema contable  │     │
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
│  │   Cambio, AjustePorTolerancia + una línea              │  │
│  │   cruce_obligacion por Vinculacion (tercero del        │  │
│  │   proveedor, valor a radicación, distrib. de origen).  │  │
│  │   El traductor solo mapea, no distribuye.              │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ● = Entidad (tiene identidad)   ○ = Value Object (sin ID)  │
└──────────────────────────────────────────────────────────────┘
```

### Agregado: Anticipo [F1]

- **Raíz:** Pago adelantado al tercero. Puede o no contar con soporte documental (cuando tiene soporte, típicamente es una cuenta de cobro). Puede ya haberse pagado (en cuyo caso la partida aparece en el extracto) o estar pendiente de pago (se debe vincular el pago).
- **Ciclo de vida:** Registro → Confirmación → Causación → Pago(s) y/o Regularización(es) → Pagado y/o Regularizado → Cerrado. Alternativa: Reversado desde estados previos a Causada (reversión total vía `ServicioDeAplicacionDevolucion`).
- **Estado inicial:** Vigente (registrado, pendiente de confirmación).
- **Estados de progreso contable:** Confirmada (aprobado para causación), Causada (asiento contable generado en el sistema contable).
- **Estados intermedios:** Pagado (saldoPorPagar = 0), Regularizado (saldoPorRegularizar = 0).
- **Estados terminales:** Cerrado (Pagado + Regularizado), Reversado (desde Vigente o Confirmada, sin cruces previos).
- **Stream de eventos:** `anticipo-{id}`
- **Eventos propios:** 12.

El anticipo tiene **dos comportamientos** según su relación con el extracto, y **dos dimensiones de valor** que se resuelven por caminos diferentes:

**Comportamiento 1 — Vinculado a extracto:** El pago ya se realizó y la partida aparece en el extracto. El valor total se **compensa** contra partida(s) del extracto. La regularización aplica como control posterior.

**Comportamiento 2 — No vinculado a extracto:** El pago está pendiente y se debe vincular. El valor total se resuelve validando el **pago**. La regularización aplica como control posterior.

En ambos casos, la **regularización** siempre ocurre vía OxpComercio que aporta el soporte formal definitivo (factura), independiente de si el anticipo tenía documentación preliminar (ej: cuenta de cobro). El estado terminal (Cerrado) requiere **ambos valores resueltos**: pago del valor total (Pagado) + regularización completa (Regularizado).

**Escenarios de regularización:**

| # | Escenario | Estado del Anticipo | OxpComercio requerida en |
|---|-----------|---------------------|--------------------------|
| R1 | Anticipo independiente, pago pendiente (externo) | Vigente | Confirmada o posterior |
| R2 | Anticipo vinculado a extracto (pago ya cubierto por partida) | Pagado | Confirmada o posterior |
| R3 | Anticipo nacido de devolución sobre OxpComercio pagada (C3, C4) — pago cubierto al crearse | Pagado | Confirmada o posterior |
| R4 | Regularización parcial contra múltiples OxpComercio | Vigente o Pagado | Confirmada o posterior (cada una) |

- La regularización afecta **ambos agregados** en una sola operación coordinada por `ServicioDeRegularizacion`: reduce `saldoPorRegularizar()` del Anticipo y reduce `saldoPorPagar()` de la OxpComercio como si fuera un pago (crea `PagoAplicado` tipo anticipo).
- La OxpComercio debe estar en estado **Confirmada o posterior** — Confirmada es el estado más temprano donde `valorNeto()` es estable (la FSM no permite correcciones después de Confirmada).
- La dimensión de pago del Anticipo (`saldoPorPagar()`) es independiente de la regularización: la resuelven sistemas o procesos externos (partida de extracto, pago directo vía el sistema contable) o el origen del anticipo (devolución).
- La dimensión de regularización (`saldoPorRegularizar()`) es controlada internamente por el bounded context OXP vía la(s) OxpComercio que aportan soporte formal definitivo (factura).
- **R3 — Devolución que crea anticipo:** Una devolución sobre OxpComercio ya pagada (`saldoPorPagar` = 0, escenarios C3/C4 en Devolucion) crea un nuevo Anticipo que nace en estado Pagado (`saldoPorPagar()` = 0, cubierto por `CrucePagoAplicado` tipo devolucion) con `saldoPorRegularizar()` = `valorNeto(devolucion)`. Este anticipo necesita regularización contra una nueva OxpComercio que aporte soporte formal definitivo. Al regularizarse completamente, transiciona directamente a Cerrado (ya estaba Pagado).
- Un anticipo puede tener pagos mixtos (extracto + pago directo) y regularizaciones parciales contra múltiples OxpComercio simultáneamente.

**Dimensiones de valor:**

1. **Valor anticipo** (`ValorMonetario`) — monto adelantado. Se resuelve por **regularización** (OxpComercio aporta soporte formal definitivo).
2. **Valor total** — cargo bancario real (comportamiento 1) o monto a pagar (comportamiento 2). Puede diferir del valor anticipo. Se resuelve por **compensación** contra partida(s) del extracto o por **pago directo**.

**Estructura:**

| Componente | Tipo | Contenido |
|---|---|---|
| `InformacionTercero` | VO | NIT, razón social — **copiado del `Proveedor` al radicar** (referencia `proveedorId`, `[D31]`) |
| `ValorMonetario` | VO | Valor del anticipo: monto adelantado. Monto, moneda, TRM si aplica, monto en moneda funcional. Valor global sin desglose fiscal `[P1]`. |
| `MedioDePago` | VO | Tipo (crédito/débito prepago), número, entidad bancaria |
| `SoporteDocumental` | VO (opcional) | Soporte preliminar del anticipo (ej: cuenta de cobro). Opcional — el anticipo puede registrarse sin soporte. El soporte formal definitivo (factura) llega vía OxpComercio durante la regularización. |
| `valorTotal` | Valor preestablecido | Inicialmente igual al valor anticipo. Puede diferir si el cargo bancario real (extracto) o el monto a pagar (pago directo) resulta diferente. El saldo se deriva de los cruces (ver comportamiento calculado). |
| `justificacion` | Texto (condicional) | Motivo de ausencia de soporte documental. Solo aplica cuando el anticipo se registra sin soporte. |
| `CrucePagoAplicado` | Entidad (1:N) | Cada registro representa un cruce parcial contra el valor total. Inmutable una vez creado. Tipo: `extracto` (ref. a OxpExtracto + PartidaExtracto, valor cubierto; creado por `AnticipoVinculadoAPartida`), `pago_directo` (ref. a pago confirmado por el sistema contable, valor pagado; creado por `PagoAnticipoAplicado`), `devolucion` (ref. a Devolucion que originó el anticipo; creado por `ServicioDeAplicacionDevolucion` al crear el anticipo por excedente), o `reversa` (ref. a Devolucion tipo Anticipo, valor = valorTotal; creado por `ServicioDeAplicacionDevolucion` Rama Anticipo al reversar). Los tipos extracto, pago_directo y devolucion pueden coexistir. El tipo reversa es exclusivo (solo en Vigente o Confirmada sin cruces previos). |
| `CruceRegularizacionAplicada` | Entidad (1:N) | Cada registro representa un cruce parcial contra el valor anticipo. Inmutable una vez creado. Tipo: `regularizacion` (ref. a OxpComercio, monto regularizado, fecha; creado por `AnticipoRegularizado`), `reversa` (ref. a Devolucion tipo Anticipo, valor = valorAnticipo; creado por `ServicioDeAplicacionDevolucion` Rama Anticipo al reversar), o `revertido` (ref. al CruceRegularizacionAplicada original, mismo valor; creado por `RegularizacionRevertida` de saga `[SI3]` ante fallo permanente — contrarresta el cruce original sin modificarlo). El tipo reversa es exclusivo (solo en Vigente o Confirmada sin cruces previos). |
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
│  │                                                            │  │
│  │  lineasParaTraduccion() → List<LineaTraduccion>            │  │
│  │   Línea única: valor anticipo × distribución               │  │
│  │   (destino de negocio). tipoComponente = anticipo.         │  │
│  │   Lleva descripcionConcepto (del anticipo). Sin desglose   │  │
│  │   fiscal [P1]. El traductor mapea como anticipo a          │  │
│  │   proveedor (Db Anticipos · Cr CxP — cuenta puente).       │  │
│  │   Disparado por AnticipoCausado (integración saliente).    │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ● = Entidad (tiene identidad)   ○ = Value Object (sin ID)      │
└──────────────────────────────────────────────────────────────────┘
```

### Agregado: Devolucion [F1]

- **Raíz:** Crédito (nota crédito) que reversa total o parcialmente una obligación. Puede referenciar OxpComercio, OxpExtracto o Anticipo según el tipo de OXP.
- **Ciclo de vida:** Radicación → Confirmación (+ aplicación del crédito) → Causación.
- **Estado terminal:** Causada (nota crédito registrada en el sistema contable).
- **Stream de eventos:** `devolucion-{id}`
- **Eventos propios:** 4.

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
| C6 | Devolución con excedente, OXP parcialmente pagada | > 0 | > saldoPorPagar | saldoPorPagar → 0 (Pagada) | Crea Anticipo por excedente (`valorNeto(devolucion) - saldoPorPagar`) |

- Cuando `saldoPorPagar > 0` y `valorNeto(devolucion) ≤ saldoPorPagar`: el crédito reduce el saldo directamente (C1, C2, C5).
- Cuando `saldoPorPagar > 0` y `valorNeto(devolucion) > saldoPorPagar`: **bifurcación** — la devolución se divide en crédito por `saldoPorPagar` (reduce saldo a 0, emite `OxpComercioPagada`) + crea Anticipo por el excedente (`valorNeto(devolucion) - saldoPorPagar`), estado Pagado, pendiente regularización. Rama Comercio-C en `ServicioDeAplicacionDevolucion` (C6).
- Cuando `saldoPorPagar = 0`: la devolución completa se convierte en Anticipo (dimensión pago resuelta, regularización pendiente) (C3, C4).
- ⚠️ **Pendiente:** ver `[PD1]` — reembolso de anticipo / integración con CXC.

**Tipo Extracto:**

| # | Escenario | Origen de la Devolucion | Efecto en OxpExtracto |
|---|-----------|------------------------|----------------------|
| E1 | Partida de retorno en extracto (reembolso de compra) | OxpComercio | Siempre es Devolucion tipo Comercio. Partida vinculada a Devolucion durante conciliación (`PartidaCubiertaPorDevolucion`). Cuenta como resuelta para I3. |
| E2 | Cargo financiero devuelto (ej: cuota de manejo, 4x1000 cobrado de más) | OxpExtracto anterior (donde se cobró) | Devolucion tipo Extracto radicada contra el extracto anterior. El extracto actual (donde llega el crédito) vincula su partida a la Devolucion durante conciliación. Reduce `saldoPorPagar()` del extracto origen. |

- Devoluciones sobre extracto solo aplican cuando `saldoPorPagar > 0` (desde Confirmada o Causada — pago interno coordinado por `ServicioDeAplicacionDevolucion`, ver `[I16]`). No se crea Anticipo por excedente en extracto.

**Tipo Anticipo:**

| # | Escenario | Efecto |
|---|-----------|--------|
| A1 | Reversa total por error (proveedor incorrecto o valor incorrecto) | Solo si Vigente o Confirmada sin cruces (`saldoPorPagar` = valorTotal, `saldoPorRegularizar` = valorAnticipo). Anticipo reversado (estado terminal). Si el anticipo ya estaba en Causada, la reversa requiere asiento contrario en SincoA&F (ver `[PD2]`). |
| A2 | Proveedor devuelve dinero | ⚠️ Diferido — ver `[PD1]`. |

**Entidades internas — tres entidades polimórficas con contrato común (`descripcion`, `valor: ValorMonetario`). Valores positivos (magnitud del crédito, D19):**

| Entidad | Tipo OXP | Cardinalidad | Descripción | Atributos propios |
|---|---|---|---|---|
| `ConceptoDevuelto` | Comercio | 1..N | Espejo parcial o total de los conceptos de la OxpComercio origen. | codigo, cantidad, `DesgloseFiscal` (VO). |
| `CargoFinancieroDevuelto` | Extracto | 1..N | Cargo financiero del OxpExtracto anterior que fue devuelto. Espejo de `CargoFinanciero`. | referenciaCargoFinanciero (ref. al `CargoFinanciero` del OxpExtracto origen). |
| `ReversaTotal` | Anticipo | Exactamente 1 | Reversa completa del anticipo. Siempre cubre el 100% del valor. | motivoReversa (proveedor incorrecto \| valor incorrecto). |

**Value Objects:**

| Value Object | Contenido | Aplica a tipo |
|---|---|---|
| `InformacionTercero` | NIT, razón social. Coincide con el del agregado OXP origen — misma referencia `proveedorId` (`[D31]`). | Todos |
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
| `lineasParaTraduccion()` | Pre-cómputo de líneas planas (concepto × destino) con valor distribuido. `tipoComponente`: `concepto_devuelto` para cada `ConceptoDevuelto`, más los tributos devueltos con su código (`iva`, `retefuente`, `reteiva`, ...). Las líneas de `concepto_devuelto` llevan `descripcionConcepto` (de `ConceptoDevuelto.descripcion`). `tipoTransaccion = nota_credito_gasto` `[D27]`. | Línea por cada `CargoFinancieroDevuelto`, `tipoComponente = cargo_financiero`. `tipoTransaccion = nota_credito_gasto`. | Línea única de la `ReversaTotal`, `tipoComponente = reversa_anticipo`. `tipoTransaccion = reversa_anticipo`. |

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
│  Invariante: mismo Proveedor que OXP origen                    │
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

### Agregado: CatalogoGastoDirecto [F1]

- **Raíz:** Catálogo de conceptos de gasto para obligaciones que se originan directamente en OXP, sin módulo de gestión detrás `[D21]`.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `catalogo-gasto-directo-{id}`
- **Eventos propios:** 4 — ver Sección 5.7.

**Entidad interna:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `ConceptoGastoDirecto` | Concepto de gasto disponible para obligaciones directas. El usuario lo selecciona al crear una OxpComercio directa; OXP resuelve las referencias fiscales desde este catálogo. Invariante: unicidad de código dentro del catálogo. | Código, descripción, clasificacionTributaria (ref. catálogo Impuestos), conceptoPago (ref. catálogo Impuestos), activo. |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  CatalogoGastoDirecto (Agregado)                             │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ConceptoGastoDirecto #1 (Entidad)                      │  │
│  │  codigo: "LIC-SW" · descripcion: Licencia de software  │  │
│  │  clasificacionTributaria: "GRAV_19"                     │  │
│  │  conceptoPago: "Servicios" · activo: true               │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ConceptoGastoDirecto #2 (Entidad)                      │  │
│  │  codigo: "ASEO" · descripcion: Servicios de aseo       │  │
│  │  clasificacionTributaria: "GRAV_19"                     │  │
│  │  conceptoPago: "Servicios" · activo: true               │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

### Agregado: CatalogoReglasDistribucion [F1]

- **Raíz:** Catálogo de reglas de preferencia de distribución por unidad organizacional, configuradas por la empresa. Materializa el **Nivel A** de la cadena de resolución de la unidad (`[D36]`) — determinístico: resuelve la distribución automáticamente cuando una regla casa, sin requerir confirmación del usuario.
- **Ciclo de vida:** Configuración — sin FSM transaccional.
- **Stream de eventos:** `catalogo-reglas-distribucion-{id}`
- **Eventos propios:** 4 — ver Sección 5.9.

**Entidad interna:**

| Entidad | Descripción | Atributos |
|---|---|---|
| `ReglaDeDistribucion` | Regla que asigna una distribución por unidad organizacional cuando una transacción casa sus criterios. Los criterios son **opcionales y combinables**; los no especificados actúan como comodín. La **especificidad** de la regla es el número de criterios definidos. La regla **sin criterios** es la preferencia general de la empresa (default más general — preserva el comportamiento previo a `[D36]`). La `distribucion` es una `InstruccionDistribucion` cuyos `DestinoDeNegocio` suman 100% (`I25`). | `criterioProveedor` (ref. `Proveedor`, opcional), `criterioTipoGasto` (clasificación/concepto, opcional), `criterioLugarEjecucion` (ubicación, opcional), `distribucion` (lista de `DestinoDeNegocio`: unidad organizacional + porcentaje), `activo`. |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────┐
│  CatalogoReglasDistribucion (Agregado)                       │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ReglaDeDistribucion #1 (Entidad) · especificidad: 2    │  │
│  │  criterioProveedor: "Aseo Total"                       │  │
│  │  criterioLugarEjecucion: "Bogotá"                      │  │
│  │  distribucion: 100% → UO "Sucursal-Bogota" · activo    │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ReglaDeDistribucion #2 (Entidad) · especificidad: 0    │  │
│  │  (sin criterios = preferencia general de la empresa)   │  │
│  │  distribucion: 100% → UO "Administracion" · activo     │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

> **Cómo se evalúa (Nivel A de `[D36]`):** ante una transacción, se eligen las reglas activas cuyos criterios definidos coinciden; gana la de **mayor especificidad**; si empatan, desempata el orden de prioridad de criterios (proveedor > tipo de gasto > lugarEjecucion). La unidad resultante se valida contra la copia local de unidades activas (`[SI8]`); si no existe/activa, el componente cae en destino pendiente (`I24`/`[D34]`). El **aprendizaje** (Nivel B, `[SI10]`) solo entra si **ninguna** regla casa.

### Agregado: Proveedor [F1]

El registro propio de OXP del tercero con quien se contraen las obligaciones — su **rol del tercero** en el modelo de bodega (replanteamiento #31, issue #38). OXP lo captura con las validaciones empaquetadas del producto, lo gobierna y **informa cada cambio a la bodega de Terceros** con el evento estándar de rol. La bodega nunca es prerrequisito: el Proveedor nace y opera con validación local.

**Identidad:** `proveedorId` propio — es la `referenciaOrigen` que viaja a la bodega: la correlación exacta para que las resoluciones de conciliación lleguen a este registro y para navegar transacción → registro → ficha consolidada. La empresa no es atributo del agregado (convención del BC: el contexto de empresa está resuelto en la lógica de los eventos) — el campo `empresa` del evento estándar sale de ese contexto, igual que en las causaciones.

**Estructura:**

| Componente | Tipo | Contenido |
|---|---|---|
| `identificacionLegal` | Pieza del paquete | Tipo de documento + número + país (+ DV cuando aplica). **Clave natural** del registro; validación local al capturar (formato, DV, tipo válido para el país). |
| `razonSocial` | Texto | Dato de identidad compartido — sujeto a conciliación en la bodega. |
| `tipoPersona` | `persona` \| `organizacion` | Dato de identidad compartido. |
| `direcciones` | Colección { `DireccionFisica`, tipoUso } | Piezas del paquete; el tipo de uso es atributo de la relación. |
| `contactos` | Colección { contacto: `Contacto`, esPrincipal } | Pieza del paquete (primera adopción del `Contacto`); la marca de principal es de la relación. |
| `estado` | `Activo` \| `Inactivo` | Mapeo natural al `estadoEnOrigen` del contrato con la bodega — sin traducción. |
| `motivoInactivacion` | { origen: `local` \| `senal_global`, codigo, descripcion } | El origen distingue la decisión comercial de OXP del veto global de la bodega — y gobierna quién puede reactivar (I21). |
| `secuencia` | Número | Contador de emisión del contrato: incrementa con cada evento estándar de rol informado a la bodega. |

**Comandos:**

| Comando | Descripción |
|---|---|
| `AsegurarProveedor` | **Única vía de creación** (I20). Idempotente: si no existe Proveedor con la clave natural, lo crea (`ProveedorRegistrado`); si existe, **lo reutiliza sin modificarlo** — las diferencias entre lo digitado y el registro se presentan al usuario (asistencia de captura), nunca sobrescriben en silencio: la actualización es siempre explícita. Invocado desde la radicación (la radicación nunca se bloquea por proveedor inexistente) o desde la gestión directa. |
| `ActualizarProveedor` | Actualización explícita de datos (razón social, tipo de persona, identificación, direcciones, contactos). Si cambia un dato de identidad compartido, el cambio viaja a la bodega y puede abrir divergencia allá — comportamiento correcto y deseado. |
| `InactivarProveedor` / `ReactivarProveedor` | Decisión comercial **local** de OXP, con motivo. La reactivación local solo procede si la inactivación vigente es de origen local (I21). |

> Las decisiones de la bodega (señal global, resoluciones de conciliación) **no son comandos**: OXP las aplica automáticamente al consumir los avisos — emiten `ProveedorInactivado`/`ProveedorReactivado` con origen `senal_global` y `CorreccionDeIdentidadAplicada`.

**Relación con las transacciones:** los 4 agregados transaccionales siguen embebiendo `InformacionTercero` en sus eventos (el hecho económico queda completo e inmutable; contrato con Contabilidad intacto) — pero el dato **se copia del Proveedor al radicar**, y la radicación lleva además la referencia `proveedorId` (ver Sección 5.1 y `[D31]`).

### Value Objects compartidos

`InformacionTercero` y `ValorMonetario` son Value Objects reutilizados por los cuatro agregados transaccionales. Desde `[D31]`, en OxpComercio, Anticipo y Devolucion el `InformacionTercero` **se copia del agregado `Proveedor` al radicar** (referencia `proveedorId`) — una sola fuente del dato dentro de OXP; en OxpExtracto identifica al emisor/banco, que no es un Proveedor. `MedioDePago` aplica a OxpComercio, OxpExtracto y Anticipo (Devolucion no lo requiere — hereda implícitamente el medio de pago del agregado OXP origen). Cada agregado los incluye en su composición pero la definición es la misma — evita duplicación de estructuras de datos sin acoplar los agregados.

### [SI1] Entidades internas con discriminador de tipo → sealed interfaces

Algunas entidades internas usan un discriminador de tipo para distinguir variantes que comparten la misma estructura de datos: `PagoAplicado` (extracto/anticipo/pago_directo/devolucion/revertido), `CrucePagoAplicado` (extracto/pago_directo/devolucion/reversa), `CrucePagoExtractoAplicado` (pago_sincoa/devolucion/revertido), `CruceRegularizacionAplicada` (regularizacion/reversa/revertido). Se sugiere considerar sealed interfaces (o mecanismo equivalente) para garantizar que todas las variantes se manejen explícitamente.

| Aspecto | Sin sealed (discriminador string) | Con sealed interface |
|---|---|---|
| Variante olvidada | Falla en runtime | Error de compilación |
| Variantes externas no previstas | Cualquiera agrega un string nuevo | Solo las definidas en el módulo |
| Typos en el discriminador | Posibles, fallan silenciosamente | Imposibles — no hay strings |
| Atributos propios por variante | No (mismo data class) | Sí (cada variante es un tipo) |

Ver `guias-de-modelado/modelar-agregados.md`, Sección 7.

### Servicio de dominio: ServicioDeConciliacion [F1]

La conciliación es la operación que vincula OxpComercio y OxpExtracto. No pertenece a ninguno de los dos — es un **domain service** que coordina efectos en ambos streams. **Refuerzo de doble pago (`[R38]` `[I23]`):** la vinculación de una partida con una OxpComercio cuyo Proveedor tiene anticipos abiertos sin resolver **no procede** — como la conciliación la inicia un usuario, hay humano en el lazo para resolver en el acto: aplica la regularización o deja constancia (`RegistrarConstanciaAnticipoNoAplicable`) y continúa.

**Flujo principal (vinculación de compras):**

1. Carga la instancia de `OxpComercio` (stream `oxp-comercio-{id}`)
2. Carga la instancia de `OxpExtracto` (stream `oxp-extracto-{id}`)
3. Valida precondiciones sobre ambos agregados
4. Emite `VinculacionRealizada` → stream de OxpExtracto (hecho de negocio: partida vinculada)
5. Emite `PagoOxpComercioViaExtractoAplicado` → stream de OxpComercio (efecto financiero: crea `PagoAplicado` tipo extracto, reduce `saldoPorPagar()`)

**Flujo de partidas de retorno (devoluciones):**

Si la partida es un crédito (retorno de dinero), el servicio busca Devoluciones existentes (tipo Comercio) del mismo Proveedor (referencia `proveedorId`) o permite crear una nueva Devolucion (tipo Extracto):

1. Carga la instancia de `OxpExtracto` (stream `oxp-extracto-{id}`)
2. Identifica partida de retorno (crédito)
3. Busca Devolucion existente (tipo Comercio) del mismo Proveedor (referencia `proveedorId`), o permite radicar nueva Devolucion (tipo Extracto)
4. Emite `PartidaCubiertaPorDevolucion` → stream de OxpExtracto (crea `CoberturaDevolucion`, partida transiciona a estado `devolucion`)

Sin tabla de compensación: operación de un solo paso sobre un solo agregado (OxpExtracto) — reintentable `[D20]`, sin riesgo de inconsistencia inter-agregado.

**Flujo de cobertura de anticipo:**

Si una partida no tiene OxpComercio asociada pero existe un Anticipo vigente del mismo Proveedor (referencia `proveedorId`), el servicio permite cubrir la partida con el anticipo:

1. Carga la instancia de `OxpExtracto` (stream `oxp-extracto-{id}`)
2. Identifica partida pendiente sin OxpComercio
3. Carga la instancia de `Anticipo` (stream `anticipo-{id}`) — del mismo Proveedor (referencia `proveedorId`), con `saldoPorPagar()` > 0
4. Emite `PartidaCubiertaPorAnticipo` → stream de OxpExtracto (crea `CoberturaAnticipo`, partida transiciona a estado `anticipo`)
5. Emite `AnticipoVinculadoAPartida` → stream de Anticipo (crea `CrucePagoAplicado` tipo extracto, reduce `saldoPorPagar()`)

**Compensación cobertura anticipo `[SI3]`:**

| Paso | Evento emitido | Stream | Si falla paso posterior → Compensación |
|------|---------------|--------|---------------------------------------|
| 4 | `PartidaCubiertaPorAnticipo` | OxpExtracto | Si paso 5 falla permanentemente: evento compensatorio por definir → stream OxpExtracto |
| 5 | `AnticipoVinculadoAPartida` | Anticipo | (último paso — reintentable `[D20]`) |

Los tres flujos coexisten en el mismo servicio porque la identificación de partidas (de retorno y de anticipo) ocurre durante la conciliación del extracto y comparte el contexto de carga. Separarlos duplicaría la carga del extracto y la clasificación de partidas.

Dos streams, consistencia eventual, coordinados por el domain service.

**Compensación `[SI3]`:**

| Paso | Evento (hecho de negocio primero) | Stream | Si falla paso posterior → Estrategia |
|------|----------------------------------|--------|--------------------------------------|
| 4 | `VinculacionRealizada` | OxpExtracto | Si paso 5 falla permanentemente: `VinculacionRevertida` → stream OxpExtracto |
| 5 | `PagoOxpComercioViaExtractoAplicado` | OxpComercio | (último paso — reintentable `[D20]`. No requiere evento de reversa: si falla, el `PagoAplicado` no se creó; si fue exitoso, no hay paso posterior que requiera compensación. La reversión de conciliación por razones de negocio está pendiente por definir `[PD2]`.) |

**Protocolo de proceso:**
- **correlationId:** UUID generado al inicio de cada ejecución de conciliación. Incluido en `VinculacionRealizada` y `PagoOxpComercioViaExtractoAplicado`.
- **Persistencia:** Stream propio `conciliacion-{correlationId}` con estado del proceso (pasos completados, referencias a streams afectados). No duplica eventos de dominio.

### Servicio de dominio: ServicioDeRegularizacion [F1]

La regularización es la operación que vincula un Anticipo con una OxpComercio, aportando el soporte documental formal (factura) que justifica el anticipo. Es un **domain service** que coordina efectos en ambos streams:

**Trigger:** El usuario selecciona una OxpComercio en estado Confirmada o posterior y un Anticipo del mismo Proveedor (referencia `proveedorId`) con `saldoPorRegularizar()` > 0. El sistema permite seleccionar el monto a regularizar (default: `min(saldoPorRegularizar(), saldoPorPagar(OxpComercio))`).

**Flujo principal:**

1. Recibe comando con: `anticipoId`, `oxpComercioId`, `montoARegularizar`
2. Carga la instancia de `Anticipo` (stream `anticipo-{id}`)
3. Carga la instancia de `OxpComercio` (stream `oxp-comercio-{id}`)
4. Valida precondiciones:
   - Anticipo en estado no terminal (ni Cerrado ni Reversado), causado contablemente (estado Causada o posterior)
   - Mismo tercero
   - `saldoPorRegularizar()` ≥ `montoARegularizar`
   - OxpComercio en estado Confirmada o posterior
   - `saldoPorPagar()` ≥ `montoARegularizar` en OxpComercio
5. Emite `AnticipoRegularizado` → stream del Anticipo (crea `CruceRegularizacionAplicada`, reduce `saldoPorRegularizar()`)
6. Emite `PagoOxpComercioViaAnticipoAplicado` → stream de OxpComercio (crea `PagoAplicado` tipo anticipo, reduce `saldoPorPagar()`)

Dos streams, consistencia eventual, coordinados por el domain service.

**Escenario 1:N (R4):** Un anticipo puede regularizarse contra múltiples OxpComercio. Cada ejecución del servicio opera sobre un par (anticipo, OxpComercio) con un `montoARegularizar` específico. La concurrencia entre múltiples ejecuciones simultáneas sobre el mismo anticipo se controla por `[D20]` contra el stream del Anticipo — la segunda ejecución falla con conflicto de versión en el paso 5 (`AnticipoRegularizado`) y reintenta con el saldo actualizado.

**Compensación `[SI3]`:**

| Paso | Evento emitido | Stream | Si falla paso posterior → Compensación |
|------|---------------|--------|---------------------------------------|
| 5 | `AnticipoRegularizado` | Anticipo | `RegularizacionRevertida` → stream Anticipo |
| 6 | `PagoOxpComercioViaAnticipoAplicado` | OxpComercio | `PagoOxpComercioViaAnticipoRevertido` → stream OxpComercio |

**Protocolo de proceso:**
- **correlationId:** UUID generado al inicio de cada ejecución de regularización. Incluido en `AnticipoRegularizado` y `PagoOxpComercioViaAnticipoAplicado`.
- **Persistencia:** Stream propio `regularizacion-{correlationId}` con estado del proceso (pasos completados, referencias a streams afectados). No duplica eventos de dominio.

**Momento de la regularización:** La OxpComercio debe estar en estado **Confirmada o posterior**. Confirmada es el estado más temprano donde `valorNeto()` es estable — la FSM no permite correcciones después de Confirmada (no hay transición Confirmada → Devuelta), por lo que los cruces inmutables en ambos agregados reflejan un `valorNeto()` definitivo. La reserva de saldo del anticipo cuando múltiples OxpComercio lo referencian se controla por control de concurrencia `[D20]`. Si el anticipo cubre 100% de la OxpComercio en Confirmada, al causarse se emite `OxpComercioPagada` como derivado por transición.

### Servicio de dominio: ServicioDeAplicacionDevolucion [F1]

La aplicación de devolución es la operación que aplica el crédito de una Devolucion contra el agregado OXP origen. Es un **domain service** que coordina efectos en múltiples streams. Se ejecuta como parte de la confirmación de la devolución:

1. Carga Devolucion (stream `devolucion-{id}`) — debe estar en estado Pendiente
2. Según `tipo` de la referencia a OXP origen:
   - **Comercio:** Carga OxpComercio → ejecuta Rama Comercio
   - **Extracto:** Carga OxpExtracto → ejecuta Rama Extracto
   - **Anticipo:** Carga Anticipo → ejecuta Rama Anticipo

**Rama Comercio** (escenarios C1–C5):

3c. Carga OxpComercio referenciada (stream `oxp-comercio-{id}`) — debe estar en estado Confirmada o posterior
4c. Evalúa según `saldoPorPagar(OXP)`:

  **Rama Comercio-A — saldoPorPagar > 0 y valorNeto(devolucion) ≤ saldoPorPagar** (C1, C2, C5):

  5ca. Emite `DevolucionConfirmada` → stream Devolucion
  6ca. Emite `PagoOxpComercioViaDevolucionAplicado` → stream OxpComercio (crea `PagoAplicado` tipo devolucion por `valorNeto(devolucion)`, reduce `saldoPorPagar()`)

  **Rama Comercio-B — saldoPorPagar = 0** (C3, C4):

  5cb. Emite `DevolucionConfirmada` → stream Devolucion
  6cb. Emite `AnticipoRegistrado` + `AnticipoConfirmado` + `AnticipoCausado` + `AnticipoPagado` (mismo append) → nuevo stream `anticipo-{id}`
    - Anticipo nace con `valorTotal = valorNeto(devolucion)`, `valorAnticipo = valorNeto(devolucion)`
    - Confirmación y causación automáticas heredadas del flujo de devolución (sin confirmador manual). El asiento contable entregado es Db Anticipos · Cr CxC proveedor (sin cuenta puente porque el dinero ya está reconocido como crédito contra el proveedor, no involucra banco).
    - Incluye `CrucePagoAplicado` (tipo devolucion) que referencia la Devolucion que lo originó → `saldoPorPagar() = 0` → estado Pagado (derivado por transición)
    - `saldoPorRegularizar() = valorNeto(devolucion)` → pendiente de regularización contra nueva OxpComercio o reembolso

  **Rama Comercio-C — saldoPorPagar > 0 y valorNeto(devolucion) > saldoPorPagar** (C6):

  5cc. `montoCredito = saldoPorPagar(OXP)`, `montoExcedente = valorNeto(devolucion) - saldoPorPagar(OXP)`
  6cc. Emite `DevolucionConfirmada` → stream Devolucion
  7cc. Emite `PagoOxpComercioViaDevolucionAplicado` → stream OxpComercio (crea `PagoAplicado` tipo devolucion por `montoCredito`, reduce `saldoPorPagar()` a 0, emite `OxpComercioPagada`)
  8cc. Emite `AnticipoRegistrado` + `AnticipoConfirmado` + `AnticipoCausado` + `AnticipoPagado` (mismo append) → nuevo stream `anticipo-{id}`
    - Anticipo nace con `valorTotal = montoExcedente`, `valorAnticipo = montoExcedente`
    - Confirmación y causación automáticas heredadas del flujo de devolución. Asiento contable: Db Anticipos · Cr CxC proveedor.
    - Incluye `CrucePagoAplicado` (tipo devolucion) → `saldoPorPagar() = 0` → estado Pagado (derivado por transición)
    - `saldoPorRegularizar() = montoExcedente` → pendiente de regularización

**Rama Extracto** (escenario E2 — cargo financiero devuelto):

3e. Carga OxpExtracto referenciado (stream `oxp-extracto-{id}`) — debe estar en estado Confirmada o posterior (`saldoPorPagar > 0`)
4e. Valida: `valorNeto(devolucion) ≤ saldoPorPagar(OxpExtracto)`
5e. Emite `DevolucionConfirmada` → stream Devolucion
6e. Emite `PagoExtractoViaDevolucionAplicado` → stream OxpExtracto (crea `CrucePagoExtractoAplicado` tipo devolucion, reduce `saldoPorPagar()`)

Devoluciones sobre extracto solo aplican cuando hay saldo pendiente (`saldoPorPagar > 0`). No aplica rama `saldoPorPagar = 0` para extracto (a diferencia de Comercio). El extracto debe estar en estado Confirmada o posterior.

**Rama Anticipo** (escenario A1):

3a. Carga Anticipo referenciado (stream `anticipo-{id}`) — debe estar en estado Vigente o Confirmada (estados pre-causación)
4a. Valida: sin `CrucePagoAplicado`, sin `CruceRegularizacionAplicada`
5a. Valida: `valorNeto(devolucion) = valorTotal` del anticipo (reversa total)
6a. Emite `DevolucionConfirmada` → stream Devolucion
7a. Emite `AnticipoReversado` → stream Anticipo (nuevo evento — estado terminal). Si el anticipo ya estaba Causada, la reversa requiere asiento contrario en SincoA&F (ver `[PD2]`).

Hasta 3 streams por rama, consistencia eventual, coordinados por el domain service.

**Compensación `[SI3]`:**

En todas las ramas, `DevolucionConfirmada` se emite primero (hecho de negocio). Los efectos en otros agregados son pasos posteriores, idempotentes y reintentables `[D20]`.

| Rama | Paso | Evento efecto (último) | Si falla → Estrategia |
|------|------|----------------------|----------------------|
| Comercio-A | 6ca | `PagoOxpComercioViaDevolucionAplicado` | Reintentable (idempotente) `[D20]`. Si fallo permanente: `PagoOxpComercioViaDevolucionRevertido` → stream OxpComercio + `DevolucionRevertida` → stream Devolucion. |
| Comercio-B | 6cb | Crea Anticipo | Reintentable (idempotente) `[D20]`. Fallo permanente improbable (stream nuevo, sin conflicto de precondiciones). Si fallo permanente: `DevolucionRevertida` → stream Devolucion. Si el stream del Anticipo se creó parcialmente (stream huérfano): identificable por `correlationId` del proceso fallido — intervención operativa para marcar como Reversado o eliminar el stream incompleto. |
| Comercio-C | 7cc | `PagoOxpComercioViaDevolucionAplicado` | Reintentable `[D20]`. Si fallo permanente: `PagoOxpComercioViaDevolucionRevertido` → stream OxpComercio + `DevolucionRevertida` → stream Devolucion. |
| Comercio-C | 8cc | Crea Anticipo (excedente) | Reintentable `[D20]`. Fallo permanente improbable (stream nuevo, sin conflicto de precondiciones). Si fallo permanente: compensar 7cc (`PagoOxpComercioViaDevolucionRevertido` → stream OxpComercio) + `DevolucionRevertida` → stream Devolucion. Si el stream del Anticipo se creó parcialmente (stream huérfano): identificable por `correlationId` del proceso fallido — intervención operativa para marcar como Reversado o eliminar el stream incompleto. |
| Extracto | 6e | `PagoExtractoViaDevolucionAplicado` | Reintentable (idempotente) `[D20]`. Si fallo permanente: `PagoExtractoViaDevolucionRevertido` → stream OxpExtracto + `DevolucionRevertida` → stream Devolucion. |
| Anticipo | 7a | `AnticipoReversado` | Reintentable (idempotente y terminal) `[D20]`. Fallo permanente improbable (validación completa en pasos 4a-5a). |

**Protocolo de proceso:**
- **correlationId:** UUID generado al inicio de cada ejecución. Incluido en `DevolucionConfirmada` y en el evento efecto correspondiente a la rama ejecutada.
- **Persistencia:** Stream propio `aplicacion-devolucion-{correlationId}` con estado del proceso (rama seleccionada, pasos completados, referencias a streams afectados). No duplica eventos de dominio.

⚠️ **Pendientes:** ver `[PD1]` — reembolso de anticipo / integración con CXC.

### [SI2] ServicioDeAplicacionDevolucion con 3 ramas → Strategy pattern

El servicio tiene 3 ramas con lógica diferenciada por tipo de OXP. Se sugiere considerar Strategy pattern o servicios especializados por tipo (ej: `EstrategiaDevolucionComercio`, `EstrategiaDevolucionExtracto`, `EstrategiaDevolucionAnticipo`) despachados por un coordinador.

| Aspecto | Sin Strategy (condicionales) | Con Strategy (servicios especializados) |
|---|---|---|
| Agregar nuevo tipo de OXP | Modificar el método existente | Crear una clase nueva, registrarla |
| Testear una rama individual | Instanciar todo el servicio | Instanciar solo la estrategia específica |
| Responsabilidad | Una clase conoce toda la lógica | Cada clase conoce solo su rama |
| Riesgo al modificar | Puede afectar otras ramas | Cada rama está aislada |
| Tabla de compensación | Una tabla global del servicio | Cada Strategy encapsula su propia tabla de compensación (Comercio: bilateral, Extracto: simple, Anticipo: sin compensación — terminal) |

### [SI3] Domain services multi-agregado con compensación → Wolverine Saga

Los domain services que coordinan eventos en múltiples streams y documentan eventos compensatorios se implementan como clases `Saga` de Wolverine `[D20]`. Wolverine persiste el estado del proceso en Marten, gestiona retries/timeouts y ejecuta los handlers de compensación. Cada paso del domain service corresponde a un handler de la saga; cada evento compensatorio corresponde a un compensation handler.

| Domain service | Saga sugerida | Agregados |
|---|---|---|
| `ServicioDeConciliacion` | `ConciliacionSaga` | OxpComercio, OxpExtracto |
| `ServicioDeRegularizacion` | `RegularizacionSaga` | Anticipo, OxpComercio |
| `ServicioDeAplicacionDevolucion` | `AplicacionDevolucionSaga` | Devolucion + OxpComercio / OxpExtracto / Anticipo (según rama `[SI2]`) |

**Nota para implementación:** Cuando los reintentos automáticos de Wolverine se agotan (tanto para pasos principales como para eventos compensatorios), la implementación debe definir una política de fallo de compensación: dead letter queue, alertas operativas, intervención manual, etc. Esta política se debe especificar al momento de la implementación y no forma parte del modelo de dominio.

### [SI4] Unicidad de obligación (I1) → proyección con constraint compuesto

La invariante I1 (unicidad NIT + número de soporte en ventana de 24 meses) cruza agregados — un agregado individual no puede validarla por sí solo. Se sugiere implementar vía proyección (read model) con constraint de unicidad compuesto sobre la combinación de los campos. Validación eventual, ventana de inconsistencia mínima.

### [SI5] `subDominioOrigen` deducido de identidad del consumidor

El campo `subDominioOrigen` de `OxpComercio` no viaja en el comando del consumidor — se resuelve en la capa de aplicación de OXP a partir de la identidad del consumidor del comando (autenticación del sub-dominio). Esto garantiza que ningún consumidor puede hacerse pasar por otro y que el dato es confiable para auditoría y trazabilidad. La validación opcional de `referenciaOrigen` (código del concepto en el catálogo del sub-dominio origen, presente en cada `ConceptoDeGasto`) depende de la disponibilidad de un query al catálogo del consumidor. Si no está disponible, se acepta la referencia como dato informativo sin validación cruzada.

### [SI6] Outbox pattern del consumidor para integración contable y eventos hacia la bodega

OXP es responsable de conservar los hechos económicos causados (eventos `*Causada`) hasta confirmar su procesamiento exitoso por el sub-dominio Contabilidad vía `EntregaAceptada`. Esto materializa la responsabilidad del consumidor declarada en `[SI7]` del modelo de Contabilidad y la decisión `[D28]` de OXP. Se sugiere implementarlo como **outbox pattern** sobre la infraestructura de persistencia y mensajería (Marten + Wolverine en el stack actual, según `[D20]`):

1. **Persistencia local del hecho económico:** al emitir un evento `*Causada`, la causación se anota en una tabla outbox local del bounded context OXP con `referenciaOrigen` única, payload completo y estado inicial "pendiente". El append del evento y la escritura en outbox ocurren en la misma transacción.
2. **Confirmación al recibir `EntregaAceptada`:** cuando OXP recibe la confirmación de Contabilidad con la `referenciaDestino` (número de asiento contable externo), la entrada del outbox correspondiente a esa `referenciaOrigen` se marca como "procesada". La `referenciaDestino` se persiste como información complementaria del documento causado.
3. **Reintento ante NACK del bus:** si Contabilidad rechaza el hecho económico antes de crear el borrador (motivos pre-borrador del `[SI7]` de Contabilidad: `TIPO_TRANSACCION_SIN_PLANTILLA`, `LINEA_SIN_ROL_EN_PLANTILLA`, `REFERENCIA_ORIGEN_DUPLICADA_NO_REEMPLAZABLE`), el mensaje cae en dead-letter queue del bus. La entrada del outbox permanece "pendiente" hasta que la causa se resuelva (catálogo de plantillas ampliado o contrato corregido) y se reinyecte manualmente.
4. **Métricas operativas:** se sugiere exponer métricas de la cola outbox (cantidad de causaciones pendientes, antigüedad máxima de una entrada sin confirmar, tasa de reintento) como indicadores de salud de la integración con Contabilidad.

La combinación outbox del consumidor + DLQ del bus + idempotencia del motor de Contabilidad garantiza que ningún hecho económico se pierda y que cada causación se procese exactamente una vez, sin requerir modelado defensivo en OXP (`[D28]`).

### [SI7] Unicidad de Proveedor (I19) → proyección con constraint único

Mismo patrón de `[SI4]`: proyección con constraint único sobre la clave natural (tipo de documento, número, país) del Proveedor. Es además el árbitro de la creación concurrente: el perdedor de la carrera reintenta `AsegurarProveedor` y reutiliza el registro ganador — alineado con el `[SI1]` de la bodega de Terceros.

### [SI9] Resolución del emisor del extracto → proyección "último emisor por número de tarjeta"

Materializa la inferencia de `[D35]`. Una proyección de lectura indexa, por **número de tarjeta**, el `InformacionTercero` del emisor visto en el `OxpExtracto` **más reciente** que lo registró (alimentada por `ExtractoRadicado`). Al radicar un extracto sin emisor en el archivo, la radicación consulta esta proyección por el número de tarjeta y propone el emisor encontrado como **sugerencia revisable** (no lo fija en silencio); si no hay coincidencia, pide la captura del usuario. No es una entidad ni un registro maestro de tarjetas — es un índice derivado de eventos que ya existen (`OxpExtracto`), reconstruible reproduciendo los streams. El día que exista el dueño del dato "tarjeta" (Tesorería o servicio global, `[D35]`), esta proyección se sustituye por la copia local del registro de tarjetas (patrón `[SI8]`).

### [SI10] Aprendizaje de la unidad organizacional → proyección "unidad frecuente por combinación"

Materializa el **Nivel B** de la cadena de resolución de la unidad (`[D36]`) — la sugerencia por aprendizaje.

- **Proyección de lectura** que acumula, por combinación `(criterioProveedor, criterioTipoGasto, criterioLugarEjecucion)`, la **unidad organizacional más frecuente** que el usuario terminó asignando (alimentada por las distribuciones efectivamente confirmadas/causadas). No es una entidad ni configuración — es un índice derivado de eventos que ya existen, reconstruible reproduciendo los streams.
- **No vinculante:** cuando ninguna regla del Nivel A casa, la radicación consulta esta proyección y **pre-llena** la distribución como **sugerencia**; el usuario confirma o corrige. Al confirmar, pasa a instrucción explícita (nivel 1). Nunca causa sola.
- **Promovible a regla:** el usuario puede convertir una sugerencia recurrente en una `ReglaDeDistribucion` formal del Nivel A (`CatalogoReglasDistribucion`).
- **Invalidable:** una sugerencia aprendida errónea puede invalidarse para que no se vuelva a proponer. No afecta distribuciones ya resueltas.
- Siempre sobre la **copia local de unidades activas** (`[SI8]`): nunca propone una unidad inexistente o inactiva; si la sugerida no existe aún, aplica el diferir (`[D34]`).

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
- **Tipo Comercio:** N Devolucion referencian 1 OxpComercio (relación N:1). Si `saldoPorPagar(OXP) = 0` al confirmar: se crea Anticipo por el valor completo (Rama B). Si `saldoPorPagar(OXP) > 0` y `valorNeto(devolucion) > saldoPorPagar`: bifurcación — crédito + Anticipo por excedente (Rama C).
- **Tipo Extracto:** N Devolucion referencian 1 OxpExtracto (relación N:1). Solo aplica cuando `saldoPorPagar > 0`.
- **Tipo Anticipo:** 1 Devolucion referencia 1 Anticipo (relación 1:1). Solo reversa total. Anticipo transiciona a Reversado (estado terminal).
- 1 Anticipo puede ser regularizado por N OxpComercio (regularización parcial).
- 1 Anticipo **puede** cubrir N partidas de uno o más extractos (vínculo permanente). Los cruces tipo extracto y tipo pago directo pueden coexistir `[R08]`.
- N OxpComercio se vinculan a 1 OxpExtracto (conciliación).
- Una OxpComercio solo puede vincularse a un único OxpExtracto (invariante I7).
- Un OxpExtracto puede recibir N vinculaciones.
- Una OxpComercio puede recibir pagos tipo extracto (conciliación), anticipo (regularización), pago directo (confirmado por el sistema contable), o devolucion — los cuatro tipos de `PagoAplicado` pueden coexistir (pagos mixtos). La vinculación con extracto sigue siendo opcional.
- La vinculación es por referencia (ID), no por composición. Cada agregado mantiene su propio stream de eventos independiente.

**Proveedor:** OxpComercio, Anticipo y Devolucion lo referencian (N:1) vía `proveedorId` — es la fuente del `InformacionTercero` que embeben al radicar (`[D31]`). La comparación "mismo Proveedor" (regularización, aplicación de devoluciones, conciliación contra anticipos) es por esta referencia, no por igualdad de textos. OxpExtracto **no** lo referencia: su tercero es el emisor/banco (entidad financiera — fuera del rol que OXP informa a la bodega).

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
┌─────────────────────────────────────────────┐
│            Confirmada                        │
│                                             │
│  Eventos de progreso (reducen saldoPorPagar,  │
│  sin cambio de estado):                      │
│    · PagoOxpComercioViaAnticipoAplicado      │
│    · PagoOxpComercioViaDevolucionAplicado    │
└────────────────┬────────────────────────────┘
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
- `Confirmada` recibe eventos de progreso de origen interno (domain services): `PagoOxpComercioViaAnticipoAplicado` (regularización, coordinado por `ServicioDeRegularizacion`) y `PagoOxpComercioViaDevolucionAplicado` (devolución, coordinado por `ServicioDeAplicacionDevolucion`). Confirmada es el estado más temprano donde `valorNeto()` es estable.
- `Causada` recibe **eventos de progreso** que reducen `saldoPorPagar()` sin cambiar de estado. Cuatro vías de pago: extracto (conciliación, coordinado por `ServicioDeConciliacion`), anticipo (regularización, coordinado por `ServicioDeRegularizacion`), pago directo, devolución (coordinado por `ServicioDeAplicacionDevolucion`). Los cuatro tipos pueden coexistir (pagos mixtos).
- `Pagada`: **evento de transición** cuando `saldoPorPagar()` = 0. Cuando pagos internos (anticipo y/o devolución) cubren 100% del saldo en Confirmada, la secuencia `Confirmada → Causada → Pagada` ocurre en un solo append: `OxpComercioCausada` + `OxpComercioPagada` (derivado por transición). Único estado terminal financiero, independiente de la(s) fuente(s) de pago.
- La transición `Devuelta → Pendiente` ocurre vía `OxpComercioCorregida`.
- Si `[R02]` está configurada como automática, `OxpComercioRadicada` puede emitir `OxpComercioConfirmada` inmediatamente.

> **Eventos de progreso del control de doble pago (`[R38]`):** `ConstanciaAnticipoNoAplicableRegistrada` aplica en Pendiente, Confirmada y Causada (con `saldoPorPagar()` > 0); `AlertaDoblePagoPotencial` aplica en Causada. Ninguno cambia el estado.

### 4.2. OxpExtracto

```
┌──────────┐  ConciliacionIniciada  ┌───────────────────┐
│Pendiente │───────────────────────►│Parcialmente       │
└──────────┘                        │Conciliada         │
                                    └─────────┬─────────┘
                                              │ ExtractoConciliado [R06]
                                              ▼
                                    ┌───────────────────┐
                                    │Conciliada (100%)  │
                                    └─────────┬─────────┘
                                              │ ExtractoConfirmado
                                              ▼
                                    ┌──────────────────────────────────────────────┐
                                    │  Confirmada                                  │
                                    │                                              │
                                    │  Evento de progreso (reduce saldoPorPagar,   │
                                    │  sin cambio de estado):                      │
                                    │    · PagoExtractoViaDevolucionAplicado       │
                                    └──────────────────────┬───────────────────────┘
                                                           │ ExtractoCausado
                                                           ▼
                                    ┌──────────────────────────────────────────────┐
                                    │  Causada                                     │
                                    │                                              │
                                    │  Eventos de progreso (reducen saldoPorPagar, │
                                    │  sin cambio de estado):                      │
                                    │    · PagoExtractoAplicado                    │
                                    │    · PagoExtractoViaDevolucionAplicado       │
                                    └──────────────────────┬───────────────────────┘
                                                           │ ExtractoPagado [R18]
                                                           │ (saldoPorPagar() = 0)
                                                           ▼
                                    ┌───────────────────┐
                                    │Pagada             │ ■
                                    └───────────────────┘
```

**Notas:**
- `Pendiente` es el estado inicial para todo extracto importado.
- `Parcialmente Conciliada` recibe eventos de vinculación, anticipo, devolución, disputa y ajustes. La transición a Conciliada requiere 100% de partidas resueltas `[R06]` — las partidas en disputa, anticipo y devolución cuentan como resueltas para este umbral.
- `Confirmada` recibe `PagoExtractoViaDevolucionAplicado` como evento de progreso de origen interno — la devolución de cargo financiero reduce `saldoPorPagar()` desde el estado más temprano donde el extracto está conciliado y confirmado (coordinado por `ServicioDeAplicacionDevolucion`).
- `Causada` recibe **eventos de progreso** que reducen `saldoPorPagar()` sin cambiar de estado. Dos vías de pago: el sistema contable confirma pagos parciales (`PagoExtractoAplicado`, crea `CrucePagoExtractoAplicado` tipo pago_sincoa) y devolución (`PagoExtractoViaDevolucionAplicado`, crea `CrucePagoExtractoAplicado` tipo devolucion). Ambos tipos pueden coexistir.
- `Pagada`: **evento de transición** (`ExtractoPagado`) cuando `saldoPorPagar()` = 0. Si la devolución cubrió 100% en Confirmada, `ExtractoPagado` se emite como derivado por transición al causarse. Único estado terminal financiero.

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
                  (PartidaEnDisputaDescartada)  (PartidaEnDisputaReclasificada)│
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
| T5 | disputa | descartada | `PartidaEnDisputaDescartada` |
| T6 | disputa | vinculada | `PartidaEnDisputaReclasificada` |

**Estados terminales (■):** vinculada, anticipo, devolucion, descartada. Todos cuentan como "resueltos" para I3.
**Estado intermedio:** disputa — cuenta como resuelta para I3 pero admite transiciones posteriores (T5, T6).

### 4.3. Anticipo

```
┌──────────┐  AnticipoConfirmado  ┌────────────┐  AnticipoCausado  ┌──────────────────────────────────────────────────────────┐
│          │ ────────────────────►│            │ ─────────────────►│                         Causada                          │
│ Vigente  │                      │ Confirmada │                   │                                                          │
│          │                      │            │                   │  Eventos de progreso (reducen saldo,                     │
└────┬─────┘                      └─────┬──────┘                   │  sin cambio de estado):                                  │
     │                                  │                          │    · AnticipoVinculadoAPartida                           │
     │                                  │                          │    · PagoAnticipoAplicado                                │
     │ AnticipoReversado                │ AnticipoReversado        │    · AnticipoRegularizado                                │
     │ (sin cruces)                     │ (sin cruces)             └────┬─────────────────────┬──────────────────────┬────────┘
     │                                  │                               │                     │                      │
     │                                  │               (AnticipoPagado)    (RegularizacionCompletada)    (AnticipoReversado)
     │                                  │                               │                     │                      │
     ▼                                  ▼                               ▼                     ▼                      ▼
┌───────────┐                      ┌───────────┐         ┌──────────────────────────┐  ┌──────────────────────────┐  ┌───────────┐
│ Reversado │ ■                    │ Reversado │ ■       │         Pagado            │  │       Regularizado        │  │ Reversado │ ■
└───────────┘                      └───────────┘         │                          │  │                          │  └───────────┘
                                                         │  Progreso:               │  │  Progreso:               │
                                                         │   · AnticipoRegularizado │  │   · AnticipoVinculado    │
                                                         │                          │  │     APartida             │
                                                         │                          │  │   · PagoAnticipoAplicado │
                                                         └────────────┬─────────────┘  └────────────┬─────────────┘
                                                                      │                              │
                                                       (Regularizacion│                              │(AnticipoPagado)
                                                       Completada)    └──────────────┬───────────────┘
                                                                                     ▼
                                                                              ┌──────────┐
                                                                              │ Cerrado  │ ■
                                                                              └──────────┘

(AnticipoRegistrado nacido de devolución, Ramas B/C — confirmación y causación automáticas heredadas)
    │
    ▼
 Causada + Pagado (en mismo append)
```

**Notas:**
- `Vigente` es el estado desde el registro (manual). El anticipo aún no ha sido confirmado para causación contable.
- `Confirmada`: el anticipo ha sido validado y aprobado para causación contable. Estado intermedio entre el registro y la entrega a contabilidad.
- `Causada`: el sistema contable ha confirmado el registro exitoso del asiento contable del anticipo (Db Anticipos a proveedores · Cr CxP por anticipos). Desde este estado se pueden recibir pagos externos (extracto, pago directo) — regulado por `[I16]`.
- **Eventos de progreso en Causada** (reducen saldos, sin cambio de estado): `AnticipoVinculadoAPartida` (crea `CrucePagoAplicado` tipo extracto; en Causada o Regularizado), `PagoAnticipoAplicado` (crea `CrucePagoAplicado` tipo pago_directo; en Causada o Regularizado), `AnticipoRegularizado` (crea `CruceRegularizacionAplicada`; en Causada o Pagado).
- **Eventos de transición** (cambian estado cuando un saldo llega a 0): `AnticipoPagado` (saldoPorPagar = 0), `RegularizacionDeAnticipoCompletada` (saldoPorRegularizar = 0), `AnticipoReversado` (ambos saldos = 0 vía cruces tipo reversa, desde Vigente o Confirmada sin cruces previos).
- Tres condiciones independientes para flujo normal (todas desde Causada):
  - **Pagado:** `saldoPorPagar()` = 0 — el valor total fue cubierto mediante partida(s) de un extracto (TC), o fue pagado y confirmado por el sistema contable cuando la forma de pago es diferente a TC; OXP monitorea y vincula el pago hasta que se cumple.
  - **Regularizado:** `saldoPorRegularizar()` = 0 — el valor anticipo fue justificado mediante OxpComercio con soporte documental formal (factura).
  - **Cerrado ■:** estado terminal = Pagado **+** Regularizado.
- **Reversado ■:** estado terminal alternativo. Solo desde Vigente o Confirmada, sin cruces previos. Si el anticipo ya fue causado contablemente, la reversa requiere un asiento contrario en SincoA&F (ver `[PD2]`). El `ServicioDeAplicacionDevolucion` (Rama Anticipo) crea `CrucePagoAplicado` tipo reversa y `CruceRegularizacionAplicada` tipo reversa, llevando ambos saldos a 0. La Devolucion tipo Anticipo es el documento que evidencia la reversión.
- `Pagado` y `Regularizado` son estados intermedios. En estado Pagado aún se pueden recibir regularizaciones; en estado Regularizado aún se pueden recibir pagos.
- `AnticipoAmortizado` es confirmación externa del sistema contable (reclasificación contable). Sin cambio de estado — ocurre después de la regularización completa (estado Regularizado o Cerrado). Canal independiente de `AnticipoCausado`.
- **Entrada directa a Causada+Pagado:** Anticipos nacidos de devolución (`ServicioDeAplicacionDevolucion`, Ramas B/C) ingresan vía `AnticipoRegistrado` con confirmación y causación automáticas heredadas del flujo de devolución — en el mismo append se emiten `AnticipoRegistrado` + `AnticipoConfirmado` + `AnticipoCausado` (entrega contable: Db Anticipos · Cr CxC proveedor, sin cuenta puente porque no involucra banco) + `AnticipoPagado` (porque nacen con `CrucePagoAplicado` tipo devolucion que cubre 100% del `valorTotal`). Solo requieren regularización para alcanzar Cerrado.
- `AlertaPlazoAnticipoVencido` es evento informativo sin cambio de estado `[R04b]`. Aplica en estados Causada o Pagado (`saldoPorRegularizar()` > 0).

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
- `Causada ■`: nota crédito registrada en el sistema contable. Estado terminal. La causación informa al sistema contable lo que ya ocurrió en la confirmación.
- No aplica estado `Devuelta` (no se devuelve una devolución).
- La máquina de estados es la misma independiente del tipo de OXP. Lo que cambia son los efectos de la confirmación.

---

### 4.5. Proveedor

Nace `Activo` (vía `AsegurarProveedor`). La inactivación tiene dos orígenes — la decisión comercial local y la señal global de la bodega — y cada origen gobierna su reversa (I21). Los datos se siguen actualizando en cualquier estado (las correcciones de la bodega aplican también sobre un Proveedor inactivo).

```
                 ProveedorRegistrado
                        │
                        ▼
                 ┌─────────────┐   ProveedorInactivado     ┌─────────────┐
                 │   ACTIVO    │ ────────────────────────► │  INACTIVO   │
                 │             │ ◄──────────────────────── │             │
                 │ · ProveedorActualizado ProveedorReactivado · ProveedorActualizado
                 │ · CorreccionDeIdentidadAplicada          │ · CorreccionDeIdentidadAplicada
                 └─────────────┘                            └─────────────┘

  Reglas de reversa (I21):
   · origen local        → reactiva el comando local o la señal de la bodega
   · origen senal_global → reactiva SOLO la señal de reactivación de la bodega
```

## 5. Catálogo de eventos

### 5.1. Radicación

#### OxpComercioRadicada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una obligación individual (compra) realizada con tarjeta corporativa ha sido registrada en el sistema con sus soportes documentales. |
| **Agregado** | OxpComercio |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Pendiente. Si `[R02]` está configurada como automática: Confirmada. |
| **Precondiciones** | Soporte documental adjunto (PDF, imagen o XML). Si es XML, datos extraídos de SincoRE. Validación de unicidad superada `[R26]`. Proveedor asegurado (`AsegurarProveedor`, I20) y Activo (I22). |
| **Información capturada** | `proveedorId` (referencia al Proveedor `[D31]`); tercero (NIT, razón social — copiado del Proveedor), fecha de transacción, valor en moneda original, moneda, TRM del día si aplica `[R05b]`, valor en moneda funcional, número de soporte/factura, medio de pago (tarjeta), conceptos (gasto/costo + impuestos + retenciones) con clasificacionTributaria y conceptoPago por cada concepto (resueltos desde el catálogo del sub-dominio origen o del catálogo de gasto directo de OXP), subDominioOrigen `[SI5]`, distribución de costos si aplica `[R05c]`, soportes documentales adjuntos. |
| **Efectos** | Solicitud de cálculo al sub-dominio de Impuestos con el contexto transaccional completo (conceptos con clasificacionTributaria y conceptoPago, entidades fiscales, ubicaciones, fecha, moneda, direccionFiscal = gasto). El DesgloseFiscal propuesto se asigna a cada ConceptoDeGasto. Si el soporte trae tributos del proveedor, se validan contra el cálculo de Impuestos `[R37]` — las discrepancias se presentan al usuario para decisión. Si XML: extracción automática de datos desde SincoRE. Si requiere formalización: notificación a SincoADPRO `[R20]`. Si supera monto máximo: alerta informativa `[R05]`. Si compra del exterior o sujeto no obligado a facturar: alerta de plazo DIAN para Documento Soporte en Adquisiciones (6 días hábiles) `[R01]` — el documento lo emite SincoFE; OXP controla que haya sido emitido. Si `[R02]` automática: emite `OxpComercioConfirmada`. |

#### ExtractoRadicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El extracto bancario del período ha sido cargado al sistema y sus partidas han sido extraídas. |
| **Agregado** | OxpExtracto |
| **Estado previo** | (nuevo) — no existía previamente. |
| **Estado resultante** | Pendiente. |
| **Precondiciones** | Archivo de extracto válido (PDF o CSV). |
| **Información capturada** | Entidad bancaria emisora (`InformacionTercero`) con su **origen** (`del_archivo \| inferido_historico \| capturado_usuario`, `[D35]`) — si el archivo no la trae, se resuelve por inferencia del `OxpExtracto` más reciente con el mismo **número de tarjeta** (`[SI9]`, sugerencia revisable) o por captura del usuario; tarjeta (número — llave de la inferencia), período, moneda del extracto `[R05d]`, partidas del extracto (descripción, valor en moneda del extracto, moneda original, valor original, TRM si aplica `[R05d]`, fecha por cada una), cargos adicionales detectados según configuración por tarjeta `[R06]` `[R19]`. Distribución de costos: se establece por componente individual (`CargoFinanciero`, `AjustePorDiferenciaCambio`, `AjustePorTolerancia`) usando preferencia de empresa o instrucción explícita — sin herencia entre componentes (ver I10). Los ajustes se distribuyen cuando se generan durante la conciliación. |
| **Efectos** | Emite `CargosAdicionalesExtraidos` si se detectan cargos configurados. Extracto disponible para conciliación. |

#### CargosAdicionalesExtraidos

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Los cargos financieros del extracto (4x1000, cuota de manejo, intereses) han sido detectados y registrados como conceptos de la OXP. |
| **Causalidad** | Derivado por transición de `ExtractoRadicado`. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Co-emisión atómica con `ExtractoRadicado` (mismo append). No tiene estado previo independiente — ocurre como parte de la radicación del extracto. |
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
| **Estado resultante** | Vigente. Excepción: anticipos nacidos de devolución (`ServicioDeAplicacionDevolucion`, Ramas B/C) — en el mismo append se emiten `AnticipoRegistrado` + `AnticipoConfirmado` + `AnticipoCausado` + `AnticipoPagado` (confirmación y causación automáticas heredadas; nacen con `CrucePagoAplicado` tipo devolucion que cubre 100% del `valorTotal`, por lo que `saldoPorPagar()` = 0). Estado resultante neto: Causada + Pagado. |
| **Precondiciones** | **Registro manual:** Usuario con perfil habilitado para generar anticipos `[R22]`. Si no hay soporte: justificación obligatoria `[R03]`. **Nacido de devolución:** Emitido por `ServicioDeAplicacionDevolucion` (Ramas B/C) — precondiciones validadas por el domain service. |
| **Información capturada** | `proveedorId` (referencia al Proveedor `[D31]`); tercero (NIT, razón social — copiado del Proveedor), valor del anticipo, valorTotal (inicialmente igual al valor anticipo), medio de pago, fecha de transacción. Si hay soporte: soporte documental (ej: cuenta de cobro). Si no hay soporte: justificación de ausencia. Distribución de costos: instrucción única sobre el valor global (sin desglose fiscal `[P1]`) — preferencia de empresa o destino único pendiente (ver I10). **Nacido de devolución (Ramas B/C):** adicionalmente incluye `CrucePagoAplicado` tipo devolucion (ref. a Devolucion que originó el anticipo, valor = valorTotal), referencia a la OxpComercio origen de la devolución. |
| **Efectos** | **Registro manual:** Inicia conteo de plazo para regularización `[R04b]` (default 30 días). Anticipo queda en estado Vigente pendiente de confirmación. Si `[R12]` está configurada como automática: emite `AnticipoConfirmado` (y eventualmente `AnticipoCausado`) en el mismo append. **Nacido de devolución:** Anticipo pasa por Confirmada y Causada en el mismo append (confirmación y causación automáticas heredadas del flujo de devolución) y nace en estado Pagado (`saldoPorPagar()` = 0). `saldoPorRegularizar()` = valorNeto(devolucion) — pendiente de regularización contra nueva OxpComercio. Inicia conteo de plazo para regularización `[R04b]`. |

#### AnticipoConfirmado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El anticipo ha sido validado y aprobado para causación contable. |
| **Causalidad** | Directa (confirmación manual) o Derivado por configuración de `AnticipoRegistrado` `[R02]`. En Ramas B/C del `ServicioDeAplicacionDevolucion`: emisión automática en el mismo append (confirmación heredada del flujo de devolución). |
| **Agregado** | Anticipo |
| **Estado previo** | Vigente. |
| **Estado resultante** | Confirmada. |
| **Precondiciones** | Usuario con rol de Confirmador `[R23]`. Confirmador diferente al Radicador `[R25]` (excepto Ramas B/C — confirmación automática). Anticipo en estado Vigente. |
| **Información capturada** | Usuario confirmador, fecha y hora de confirmación. En Ramas B/C: referencia al evento de devolución que originó la confirmación automática. |
| **Efectos** | Habilita la transición hacia causación. Si `[R12]` está configurada como automática: emite `AnticipoCausado`. |

#### AnticipoCausado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable ha confirmado el registro exitoso de la causación del anticipo. Reconoce el activo "anticipos a proveedores" contra una cuenta por pagar puente (Db Anticipos · Cr CxP por anticipos). En Ramas B/C, el asiento es Db Anticipos · Cr CxC proveedor (sin cuenta puente — no involucra banco). |
| **Causalidad** | Directa (confirmación del sistema contable) o Derivado por configuración de `AnticipoConfirmado` `[R12]`. |
| **Agregado** | Anticipo |
| **Estado previo** | Confirmada. |
| **Estado resultante** | Causada. |
| **Precondiciones** | Anticipo confirmado. El sistema contable confirma registro exitoso de la causación enviada `[R14b]`. |
| **Información capturada** | Fecha de causación. La `referenciaDestino` (número de asiento contable externo) se persiste de manera asíncrona al recibir `EntregaAceptada` del sub-dominio Contabilidad como información complementaria del documento causado (ver `[D28]` y `[SI6]`). |
| **Efectos** | Integración saliente: causación individual enviada al sistema contable (JSON) con las líneas de `lineasParaTraduccion()` del agregado y `tipoTransaccion = anticipo_a_proveedor` `[D27]`. Espera `EntregaAceptada` del sub-dominio Contabilidad para registrar la `referenciaDestino`. Habilita pagos externos del anticipo (vinculación con partida de extracto, pago directo notificado por el sistema contable) `[I16]`. En Ramas B/C: emite `AnticipoPagado` como derivado por transición en el mismo append (porque nace con `CrucePagoAplicado` tipo devolucion que cubre 100% del valorTotal). |

#### AnticipoRegularizado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una OxpComercio vinculada aporta el **soporte formal definitivo** (factura), reduciendo el saldo por regularizar del anticipo. |
| **Causalidad** | Efecto inter-agregado — `ServicioDeRegularizacion`. |
| **Agregado** | Anticipo |
| **Estado previo** | Causada o Pagado. |
| **Estado resultante** | Causada o Pagado (reduce `saldoPorRegularizar()`). Si `saldoPorRegularizar()` = 0: transiciona a Regularizado (o Cerrado si ya estaba Pagado). |
| **Precondiciones** | Anticipo en estado no terminal (ni Cerrado ni Reversado), causado contablemente (estado Causada o posterior). OxpComercio del mismo Proveedor (referencia `proveedorId`), en estado Confirmada o posterior. `saldoPorRegularizar()` suficiente para el monto a regularizar. Coordinado por `ServicioDeRegularizacion`. |
| **Información capturada** | Referencia a OxpComercio vinculada, monto regularizado, fecha. |
| **Efectos** | Crea entidad `CruceRegularizacionAplicada` en el agregado Anticipo. Reduce `saldoPorRegularizar()`. Genera información estructurada para amortización contable `[R15]`. Si `saldoPorRegularizar()` = 0: emite `RegularizacionDeAnticipoCompletada`. |

#### RegularizacionRevertida

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Evento compensatorio de `AnticipoRegularizado`. Revierte la `CruceRegularizacionAplicada` creada por una regularización cuyo paso posterior (`PagoOxpComercioViaAnticipoAplicado`) falló permanentemente. Restaura `saldoPorRegularizar()`. Solo emitido por compensación del `ServicioDeRegularizacion` `[SI3]` — nunca por operación de negocio directa. |
| **Causalidad** | Evento compensatorio de `AnticipoRegularizado` — `ServicioDeRegularizacion` `[SI3]`. |
| **Agregado** | Anticipo |
| **Estado previo** | Causada o Pagado. |
| **Estado resultante** | Causada o Pagado (restaura `saldoPorRegularizar()` al valor previo a la regularización fallida). |
| **Precondiciones** | Existe `CruceRegularizacionAplicada` correspondiente al `correlationId` del proceso fallido `[D20]`. |
| **Información capturada** | Referencia a la `CruceRegularizacionAplicada` revertida, `correlationId` del proceso, monto restaurado, motivo del fallo. |
| **Efectos** | Elimina la `CruceRegularizacionAplicada` del agregado. Restaura `saldoPorRegularizar()`. |

#### AnticipoAmortizado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable ha confirmado la reclasificación contable del saldo del anticipo a cuentas de gasto o costo definitivas. La reclasificación se realiza dentro del mismo registro contable de la causación de la OXP de Comercio que regulariza el anticipo — no corresponde a un registro contable independiente. Este evento es la confirmación de que el sistema contable procesó esa reclasificación; es independiente de la causación inicial del anticipo (`AnticipoCausado`), que reconoce el activo cuando se entrega el dinero. |
| **Agregado** | Anticipo |
| **Estado previo** | Cerrado o Regularizado (sin cambio de estado — es confirmación de efecto contable externo posterior a la regularización completa). |
| **Estado resultante** | (sin cambio de estado). |
| **Precondiciones** | Anticipo con regularización completa (`saldoPorRegularizar()` = 0). El sistema contable ha procesado la reclasificación contable a partir de la información entregada por OXP. |
| **Información capturada** | Número de asiento de amortización, fecha de amortización. |
| **Efectos** | Cierra el ciclo contable del anticipo. |

#### AnticipoVinculadoAPartida

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una partida del extracto ha sido cubierta por este anticipo. Contraparte del evento `PartidaCubiertaPorAnticipo` emitido sobre el stream del OxpExtracto. Registra el lado Anticipo de la operación de cobertura. Puede coexistir con cruces tipo pago directo sobre el mismo anticipo. |
| **Causalidad** | Efecto inter-agregado — `ServicioDeConciliacion` (flujo de cobertura de anticipo). Contraparte de `PartidaCubiertaPorAnticipo` (OxpExtracto). |
| **Agregado** | Anticipo |
| **Estado previo** | Causada o Regularizado. |
| **Estado resultante** | Causada o Regularizado (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: transiciona a Pagado (o Cerrado si ya estaba Regularizado). |
| **Precondiciones** | Anticipo en estado no terminal (ni Cerrado ni Reversado), causado contablemente (estado Causada o posterior) `[I16]`. Mismo tercero. Partida del extracto en estado pendiente `[R08]`. `saldoPorPagar()` suficiente para el valor cubierto. |
| **Información capturada** | Referencia a OxpExtracto, referencia a PartidaExtracto, valor cubierto. |
| **Efectos** | Crea entidad `CrucePagoAplicado` (tipo: extracto) en el agregado Anticipo. Reduce `saldoPorPagar()`. Si `saldoPorPagar()` = 0: emite `AnticipoPagado`. |

#### PagoAnticipoAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable ha confirmado un pago parcial o total del valor total del anticipo por vía diferente a tarjeta de crédito. Puede coexistir con cruces tipo extracto sobre el mismo anticipo. |
| **Agregado** | Anticipo |
| **Estado previo** | Causada o Regularizado. |
| **Estado resultante** | Causada o Regularizado (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: transiciona a Pagado (o Cerrado si ya estaba Regularizado). |
| **Precondiciones** | Anticipo en estado no terminal (ni Cerrado ni Reversado), causado contablemente (estado Causada o posterior) `[I16]`. El sistema contable confirma el pago. `saldoPorPagar()` suficiente para el monto pagado. |
| **Información capturada** | Referencia de pago del sistema contable (incluye identificador del destino físico que originó el pago, ej: número de transacción SincoA&F), valor pagado, fecha. |
| **Efectos** | Crea entidad `CrucePagoAplicado` (tipo: pago_directo) en el agregado Anticipo. Reduce `saldoPorPagar()`. Si `saldoPorPagar()` = 0: emite `AnticipoPagado`. |

#### AnticipoPagado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El valor total del anticipo ha sido completamente cubierto. El pago pudo realizarse mediante partida(s) de extracto (TC), pago directo confirmado por el sistema contable (forma de pago diferente a TC), devolución (anticipo nacido de `ServicioDeAplicacionDevolucion`), o una combinación de estos. |
| **Causalidad** | Derivado por transición — emitido cuando `saldoPorPagar()` = 0. |
| **Agregado** | Anticipo |
| **Estado previo** | Causada o Regularizado. |
| **Estado resultante** | Pagado. Si ya estaba Regularizado: Cerrado (estado terminal). |
| **Precondiciones** | `saldoPorPagar()` = 0. |
| **Información capturada** | Total de cruces de pago aplicados (cantidad, suma de valores, detalle por tipo extracto/pago_directo/devolucion), fecha de cierre de la dimensión de pago. |
| **Efectos** | Transiciona a Pagado. Si el anticipo ya estaba Regularizado (`saldoPorRegularizar()` = 0): transiciona directamente a Cerrado. |

#### RegularizacionDeAnticipoCompletada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El valor anticipo ha sido completamente regularizado mediante OxpComercio con soporte documental formal (factura). |
| **Causalidad** | Derivado por transición — emitido cuando `saldoPorRegularizar()` = 0. |
| **Agregado** | Anticipo |
| **Estado previo** | Causada o Pagado. |
| **Estado resultante** | Regularizado. Si ya estaba Pagado: Cerrado (estado terminal). |
| **Precondiciones** | `saldoPorRegularizar()` = 0. |
| **Información capturada** | Total de cruces de regularización aplicados (cantidad, suma de montos regularizados, referencias a OxpComercio), fecha de cierre de la dimensión de regularización. |
| **Efectos** | Transiciona a Regularizado. Si el anticipo ya estaba Pagado (`saldoPorPagar()` = 0): transiciona directamente a Cerrado. |

#### AnticipoReversado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El anticipo ha sido completamente reversado por error (proveedor incorrecto o valor incorrecto). Ambos saldos llevados a cero vía cruces tipo reversa. |
| **Causalidad** | Efecto inter-agregado — `ServicioDeAplicacionDevolucion` (Rama Anticipo). |
| **Agregado** | Anticipo |
| **Estado previo** | Vigente o Confirmada (sin cruces previos). Si el anticipo ya estaba Causada, la reversa requiere asiento contrario en SincoA&F (ver `[PD2]`). |
| **Estado resultante** | Reversado (estado terminal). |
| **Precondiciones** | Anticipo en estado Vigente o Confirmada. Sin `CrucePagoAplicado` previos (`saldoPorPagar` = valorTotal). Sin `CruceRegularizacionAplicada` previos (`saldoPorRegularizar` = valorAnticipo). Coordinado por `ServicioDeAplicacionDevolucion` (Rama Anticipo). |
| **Información capturada** | Referencia a Devolucion tipo Anticipo que origina la reversión, `CrucePagoAplicado` tipo reversa (valor = valorTotal), `CruceRegularizacionAplicada` tipo reversa (valor = valorAnticipo), motivo, fecha. |
| **Efectos** | Crea `CrucePagoAplicado` tipo reversa y `CruceRegularizacionAplicada` tipo reversa. `saldoPorPagar()` = 0, `saldoPorRegularizar()` = 0. Transiciona a Reversado. Estado terminal — no se pueden recibir más cruces ni regularizaciones. |

#### AlertaPlazoAnticipoVencido

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El anticipo no ha sido regularizado dentro del plazo configurado. |
| **Agregado** | Anticipo |
| **Estado previo** | Causada o Pagado. |
| **Estado resultante** | (sin cambio de estado; es evento informativo). |
| **Precondiciones** | Anticipo con `saldoPorRegularizar()` > 0. Plazo configurado excedido `[R04b]` (default 30 días, configurable por empresa). |
| **Información capturada** | Días de retraso, saldo pendiente de regularización, fecha límite original. |
| **Efectos** | Evento de dominio (se persiste en el stream). Consumidor: read model de alertas y panel de trabajo. Resolución implícita: el read model marca la alerta como resuelta cuando `saldoPorRegularizar()` = 0 (`AnticipoRegularizado` o `AnticipoCerrado`). |

---

### 5.3. Conciliación

#### ConciliacionIniciada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El proceso de conciliación entre las partidas del extracto y las OxpComercio registradas ha comenzado. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Pendiente. |
| **Estado resultante** | Parcialmente Conciliada. |
| **Precondiciones** | Extracto radicado. Existen instancias de OxpComercio disponibles para vinculación. |
| **Información capturada** | Fecha de inicio de conciliación, partidas que el sistema propone automáticamente (basado en patrones aprendidos `[R09]` y criterios de comercio, valor y fecha). |
| **Efectos** | Inicia conteo de plazo de conciliación `[R07]`. Aplica conciliación automática con patrones persistidos `[R09]`. Cada match exitoso dispara `ServicioDeConciliacion` que emite `VinculacionRealizada` + `PagoOxpComercioViaExtractoAplicado`. |

#### VinculacionRealizada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una partida del extracto ha sido vinculada con una o más OxpComercio. Este evento registra el lado Extracto de la operación de conciliación coordinada por `ServicioDeConciliacion`. |
| **Causalidad** | Efecto inter-agregado — `ServicioDeConciliacion`. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliada. |
| **Estado resultante** | Parcialmente Conciliada. Si el 100% de partidas quedan resueltas, el agregado emite `ExtractoConciliado` automáticamente. |
| **Precondiciones** | OxpComercio causada(s). Tipo de vinculación válido: 1:1 o N:1. Si N:1: suma de OxpComercio dentro de tolerancia `[R10]`. Coordinado por `ServicioDeConciliacion`. |
| **Información capturada** | Tipo de vinculación (1:1 o N:1), referencia(s) a OxpComercio vinculada(s), partida del extracto, valor de diferencia (si existe), origen (automática o manual). |
| **Efectos** | `ServicioDeConciliacion` emite simultáneamente `PagoOxpComercioViaExtractoAplicado` sobre cada OxpComercio vinculada (crea `PagoAplicado` tipo extracto, reduce `saldoPorPagar()`). Si diferencia dentro de tolerancia: emite `AjustePorToleranciaGenerado` `[R10]`. Si OxpComercio en moneda extranjera con diferencia de TRM: emite `AjustePorDiferenciaEnCambioRegistrado` `[R10b]`. Persiste asociación de patrón comercio-descripción `[R09]`. |

#### VinculacionRevertida

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Evento compensatorio de `VinculacionRealizada`. Revierte la vinculación de una partida cuando el paso posterior (`PagoOxpComercioViaExtractoAplicado`) falló permanentemente. Solo emitido por compensación del `ServicioDeConciliacion` `[SI3]` — nunca por operación de negocio directa. |
| **Causalidad** | Evento compensatorio de `VinculacionRealizada` — `ServicioDeConciliacion` `[SI3]`. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliada o Conciliada. |
| **Estado resultante** | Parcialmente Conciliada (partida revertida a pendiente de vinculación). Si estaba Conciliada por efecto derivado de esta vinculación, retorna a Parcialmente Conciliada. |
| **Precondiciones** | Existe vinculación correspondiente al `correlationId` del proceso fallido `[D20]`. Extracto no ha avanzado más allá de Conciliada (no Confirmada ni Causada). |
| **Información capturada** | Referencia a partida desvinculada, referencia(s) a OxpComercio desvinculadas, `correlationId` del proceso, motivo del fallo. |
| **Efectos** | Revierte vinculación de la partida. Partida disponible para nueva conciliación. |

#### AjustePorDiferenciaEnCambioRegistrado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La TRM de radicación difiere de la TRM del extracto para una OxpComercio en moneda extranjera — se registra la diferencia y se crea la entidad `AjustePorDiferenciaCambio` en un solo hecho atómico. |
| **Causalidad** | Derivado por transición de `VinculacionRealizada` / `PagoOxpComercioViaExtractoAplicado`. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliada. |
| **Estado resultante** | (sin cambio de estado). |
| **Precondiciones** | `VinculacionRealizada` previa con OxpComercio en moneda extranjera. Diferencia entre valor radicado (TRM transacción) y valor en extracto (TRM corte) `[R10b]`. |
| **Información capturada** | OxpComercio de origen, TRM de radicación, TRM del extracto, valor de la diferencia, clasificación (gasto financiero si TRM subió, ingreso financiero si TRM bajó). |
| **Efectos** | Crea entidad `AjustePorDiferenciaCambio`. Concepto incluido en el OxpExtracto. Se causa junto con el extracto. Un ajuste por cada OxpComercio en moneda extranjera con diferencia `[R10b]`. |

#### AjustePorToleranciaGenerado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se ha generado un ajuste por tolerancia sobre el OxpExtracto, registrando la diferencia menor entre el valor de la partida del extracto y la OxpComercio vinculada. |
| **Causalidad** | Derivado por transición de `VinculacionRealizada`. |
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
| **Causalidad** | Efecto inter-agregado — `ServicioDeConciliacion` (flujo de cobertura de anticipo). |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliada. |
| **Estado resultante** | Parcialmente Conciliada. Si el 100% de partidas quedan resueltas, el agregado emite `ExtractoConciliado` automáticamente. |
| **Precondiciones** | Anticipo vigente para el mismo Proveedor (referencia `proveedorId`). Partida en estado pendiente `[R08]`. |
| **Información capturada** | Referencia al Anticipo, partida del extracto cubierta. |
| **Efectos** | Partida transiciona a estado `anticipo`. Se crea entidad `CoberturaAnticipo` en el agregado. Cuenta como resuelta para invariante I3 (completitud de conciliación). El vínculo anticipo-partida es permanente. |

#### PartidaCubiertaPorDevolucion

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una partida del extracto que representa un retorno de dinero ha sido cubierta por una Devolucion. Permite avanzar en la conciliación. |
| **Causalidad** | Efecto inter-agregado — `ServicioDeConciliacion` (flujo de partidas de retorno). |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliada. |
| **Estado resultante** | Parcialmente Conciliada. Si el 100% de partidas quedan resueltas, el agregado emite `ExtractoConciliado` automáticamente. |
| **Precondiciones** | Devolucion existente (tipo Comercio) o nueva (tipo Extracto) para el mismo Proveedor (referencia `proveedorId`). Partida en estado pendiente. Coordinado por `ServicioDeConciliacion`. |
| **Información capturada** | Referencia a Devolucion, partida del extracto cubierta. |
| **Efectos** | Partida transiciona a estado `devolucion`. Se crea entidad `CoberturaDevolucion` en el agregado. Cuenta como resuelta para invariante I3 (completitud de conciliación). El vínculo devolucion-partida es permanente. |

#### ExtractoConciliado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El 100% de las partidas del extracto han sido vinculadas a OxpComercio, cubiertas por anticipo, cubiertas por devolución, descartadas, clasificadas como cargos adicionales, o marcadas como partida en disputa. |
| **Causalidad** | Derivado por transición — emitido cuando 100% de partidas resueltas. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliada. |
| **Estado resultante** | Conciliada. |
| **Precondiciones** | 100% de partidas resueltas `[R06]`. |
| **Información capturada** | Resumen de conciliación: total de partidas, partidas automáticas vs. manuales, partidas en disputa, partidas cubiertas por anticipo, partidas cubiertas por devolución, conceptos de ajuste generados. |
| **Efectos** | Habilita la transición hacia confirmación. |

#### PartidaEnDisputaMarcada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una partida del extracto ha sido marcada como disputa por no poder conciliarse debido a errores bancarios, fraudes potenciales o transacciones no reconocidas. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Parcialmente Conciliada. |
| **Estado resultante** | (sin cambio de estado del extracto; partida marcada internamente). |
| **Precondiciones** | Partida del extracto sin OxpComercio asociada. Decisión del usuario o del Autorizador. |
| **Información capturada** | Partida del extracto afectada, motivo de la disputa (error bancario, fraude potencial, no reconocida), usuario que marca, fecha. |
| **Efectos** | La partida cuenta como conciliada para alcanzar el 100% `[R06]`. Permite avanzar sin generar anticipos. Resolución posterior vía `PartidaEnDisputaDescartada` o `PartidaEnDisputaReclasificada`. |

#### PartidaEnDisputaDescartada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La partida en disputa ha sido descartada porque el banco reversó la transacción. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Partida marcada como disputa. |
| **Estado resultante** | Partida transiciona a estado `descartada` (compensada contra reverso bancario). |
| **Precondiciones** | Línea de "Reverso Bancario" identificada en un extracto (puede ser de un período futuro) `[R06b]` `[R10c]`. |
| **Información capturada** | Referencia al extracto y línea de reverso bancario, fecha de resolución. |
| **Efectos** | Cierra el ciclo de la partida en disputa. |

#### PartidaEnDisputaReclasificada

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
| **Estado previo** | Parcialmente Conciliada. |
| **Estado resultante** | (sin cambio de estado; es evento informativo). |
| **Precondiciones** | Plazo configurado excedido `[R07]` (default 3 días previos a fecha de pago, configurable por tarjeta). |
| **Información capturada** | Días de retraso, partidas pendientes de conciliar, fecha límite original. |
| **Efectos** | Evento de dominio (se persiste en el stream). Consumidor: read model de alertas y panel de trabajo. Resolución implícita: el read model marca la alerta como resuelta cuando todas las partidas están resueltas (`ExtractoConciliado`). |

---

### 5.4. Confirmación y Causación

#### OxpComercioConfirmada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La OXP ha sido validada y aprobada para causación contable. |
| **Causalidad** | Directa (confirmación manual) o Derivado por configuración de `OxpComercioRadicada` `[R02]`. |
| **Agregado** | OxpComercio |
| **Estado previo** | Pendiente. |
| **Estado resultante** | Confirmada. |
| **Precondiciones** | Usuario con rol de Confirmador `[R23]`. Confirmador diferente al Radicador `[R25]`. OXP en estado Pendiente con soportes completos. **Verificación temprana de doble pago (`[R38]` `[I23]`):** si el Proveedor tiene anticipos con `saldoPorRegularizar()` > 0 no resueltos para esta OXP (sin regularización aplicada ni constancia registrada), el sistema los destaca y exige la decisión según el modo configurado por empresa — bloqueo (no confirma sin resolver) o decisión obligatoria (aplicar la regularización, que lleva a la amortización embebida `[D26]`, o dejar constancia). |
| **Información capturada** | Usuario confirmador, fecha y hora de confirmación. |
| **Efectos** | Comando asíncrono de confirmación al sub-dominio de Impuestos con: transaccionId, efectoFiscal = gravamen, contexto transaccional completo, desglose confirmado `[R37]`. Impuestos crea el registro tributario inmutable `[D9-Imp]`. Habilita la transición hacia causación. Si `[R12]` está configurada como automática: emite `OxpComercioCausada`. |

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
| **Descripción** | El sistema contable ha confirmado el registro exitoso de la causación. |
| **Causalidad** | Directa (confirmación del sistema contable) o Derivado por configuración de `OxpComercioConfirmada` `[R12]`. |
| **Agregado** | OxpComercio |
| **Estado previo** | Confirmada. |
| **Estado resultante** | Causada. |
| **Precondiciones** | OXP confirmada. El sistema contable confirma registro exitoso de la causación enviada `[R13]`. |
| **Información capturada** | Fecha de causación (fecha del soporte/factura, principio de devengo). La `referenciaDestino` (número de asiento contable externo) se persiste de manera asíncrona al recibir `EntregaAceptada` del sub-dominio Contabilidad como información complementaria del documento causado (ver `[D28]` y `[SI6]`). |
| **Efectos** | Integración saliente: causación individual enviada al sistema contable (JSON) con `tipoTransaccion = causacion_gasto` `[D27]`. Si la OxpComercio ya tenía un anticipo regularizado al momento de causarse (Caso A de `[D26]`): la información de amortización se incluye en las mismas líneas de traducción como tipo de componente (no como `tipoTransaccion` separado) `[R15]` `[D26]`. Si el cruce con el anticipo ocurre **después** de esta causación (Caso B), la amortización se emite por separado vía `PagoOxpComercioViaAnticipoAplicado`. Espera `EntregaAceptada` del sub-dominio Contabilidad para registrar la `referenciaDestino`. Si `saldoPorPagar()` > 0: OxpComercio disponible para recibir pagos vía extracto (conciliación), anticipo (regularización), pago directo (notificado por el sistema contable), o devolución (`ServicioDeAplicacionDevolucion`). Si `saldoPorPagar()` = 0 (pagos internos — anticipo y/o devolución — cubrieron 100% en Confirmada): emite `OxpComercioPagada` como derivado por transición. |

#### ExtractoConfirmado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El extracto conciliado ha sido validado y aprobado para causación contable. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Conciliada. |
| **Estado resultante** | Confirmada. |
| **Precondiciones** | Extracto en estado Conciliada (100%) `[R11]`. Usuario con rol de Confirmador `[R23]`. |
| **Información capturada** | Usuario confirmador, fecha y hora de confirmación. |
| **Efectos** | Habilita la transición hacia causación. Si `[R12]` está configurada como automática: emite `ExtractoCausado`. |

#### ExtractoCausado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable ha confirmado el registro exitoso de la causación del extracto. |
| **Causalidad** | Directa (confirmación del sistema contable) o Derivado por configuración de `ExtractoConfirmado` `[R12]`. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Confirmada. |
| **Estado resultante** | Causada. |
| **Precondiciones** | Extracto en estado Confirmada. El sistema contable confirma registro exitoso `[R14]`. |
| **Información capturada** | Fecha de causación (fecha de compensación), conceptos causados (incluye cargos adicionales y ajustes). La `referenciaDestino` (número de asiento contable externo) se persiste de manera asíncrona al recibir `EntregaAceptada` del sub-dominio Contabilidad como información complementaria del documento causado (ver `[D28]` y `[SI6]`). |
| **Efectos** | Integración saliente: causación de OxpExtracto enviada al sistema contable (JSON) con `tipoTransaccion = causacion_gasto` `[D27]`. Registra el total del extracto reclasificando la deuda hacia la entidad bancaria/emisor: por cada `Vinculacion` viaja una línea `cruce_obligacion` (saldo de la cuenta por pagar del proveedor de la compra cruzada), y los cargos adicionales y ajustes (cargos financieros, diferencia en cambio y ajustes por tolerancia) viajan como tipos de componente dentro de las mismas líneas; la contrapartida (deuda con el banco/emisor) la genera el motor de Contabilidad `[D29]`. Espera `EntregaAceptada` del sub-dominio Contabilidad para registrar la `referenciaDestino`. Si `saldoPorPagar()` > 0: extracto disponible para recibir pagos (notificados por el sistema contable y/o devolución). Si `saldoPorPagar()` = 0 (devolución cubrió 100% en Confirmada): emite `ExtractoPagado` como derivado por transición. |

---

### 5.5. Pago

#### PagoOxpComercioViaExtractoAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La OxpComercio ha recibido un pago parcial o total vía vinculación con una partida del extracto durante la conciliación. Evento de progreso — reduce `saldoPorPagar()` sin cambiar de estado. Emitido por `ServicioDeConciliacion` como contraparte de `VinculacionRealizada`. Puede coexistir con pagos tipo anticipo y pago directo sobre la misma OxpComercio. |
| **Causalidad** | Efecto inter-agregado — `ServicioDeConciliacion`. |
| **Agregado** | OxpComercio |
| **Estado previo** | Causada. |
| **Estado resultante** | Causada (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: emite `OxpComercioPagada`. |
| **Precondiciones** | OxpComercio causada. Coordinado por `ServicioDeConciliacion`. Vinculación existente con partida de extracto (1:1 o N:1). `saldoPorPagar()` suficiente para el valor cubierto. |
| **Información capturada** | Referencia a OxpExtracto, partida del extracto vinculada, tipo de vinculación (1:1 o N:1), valor cubierto. |
| **Efectos** | Crea entidad `PagoAplicado` (tipo: extracto). Reduce `saldoPorPagar()`. El agregado genera `lineasParaTraduccion()` como insumo que el servicio de Traducción Contable transforma y entrega al sistema contable `[R17]`. Si la OxpComercio es en moneda extranjera y existe diferencia de TRM: emite `AjustePorDiferenciaEnCambioRegistrado` sobre el OxpExtracto `[R10b]`. Si la diferencia está dentro de tolerancia `[R10]`: emite `AjustePorToleranciaGenerado` sobre el OxpExtracto. |

#### PagoOxpComercioViaAnticipoAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El anticipo que regulariza esta OxpComercio cubre parcial o totalmente el valor por pagar. Evento de progreso — reduce `saldoPorPagar()` sin cambiar de estado. Emitido por `ServicioDeRegularizacion`. Puede coexistir con pagos tipo extracto y pago directo sobre la misma OxpComercio. |
| **Causalidad** | Efecto inter-agregado — `ServicioDeRegularizacion`. |
| **Agregado** | OxpComercio |
| **Estado previo** | Confirmada o Causada. |
| **Estado resultante** | Confirmada o Causada (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0 en Causada: emite `OxpComercioPagada`. Si `saldoPorPagar()` = 0 en Confirmada: `OxpComercioPagada` se emitirá como derivado por transición al causarse. |
| **Precondiciones** | OxpComercio en estado Confirmada o posterior. Anticipo en estado no terminal (ni Cerrado ni Reversado), vinculado vía regularización. `saldoPorPagar()` suficiente para el monto cubierto. Coordinado por `ServicioDeRegularizacion`. |
| **Información capturada** | Referencia a Anticipo, monto cubierto por anticipo, fecha. |
| **Efectos** | Crea entidad `PagoAplicado` (tipo: anticipo). Reduce `saldoPorPagar()`. **Amortización contable según el momento del cruce `[D26]`:** si la OxpComercio está en **Confirmada** (aún no causada — Caso A), no emite causación: la amortización viajará embebida como tipo de componente en la futura `OxpComercioCausada`. Si la OxpComercio ya está en **Causada** (Caso B), emite una **causación de amortización independiente** al sistema contable con `tipoTransaccion = amortizacion_anticipo` (Db CxP proveedor · Cr Anticipos a proveedores) `[R15]` `[D26]`, ya que la causación del gasto ya salió. En ambos casos, al completarse la regularización el Anticipo recibe `AnticipoAmortizado` para cerrar su ciclo. |

#### PagoOxpComercioViaAnticipoRevertido

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Evento compensatorio de `PagoOxpComercioViaAnticipoAplicado`. Revierte el `PagoAplicado` (tipo: anticipo) cuando el proceso de regularización falló permanentemente. Restaura `saldoPorPagar()`. Solo emitido por compensación del `ServicioDeRegularizacion` `[SI3]` — nunca por operación de negocio directa. Se emite junto con `RegularizacionRevertida` → stream Anticipo. |
| **Causalidad** | Evento compensatorio de `PagoOxpComercioViaAnticipoAplicado` — `ServicioDeRegularizacion` `[SI3]`. |
| **Agregado** | OxpComercio |
| **Estado previo** | Confirmada, Causada o Pagada. |
| **Estado resultante** | Confirmada o Causada (restaura `saldoPorPagar()`). Si estaba Pagada por efecto de este pago: retorna a Confirmada o Causada. |
| **Precondiciones** | Existe `PagoAplicado` (tipo: anticipo) correspondiente al `correlationId` del proceso fallido `[D20]`. |
| **Información capturada** | Referencia a Anticipo, monto restaurado, `correlationId` del proceso, motivo del fallo. |
| **Efectos** | Elimina `PagoAplicado` (tipo: anticipo). Restaura `saldoPorPagar()`. |

#### PagoOxpComercioDirectoAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable ha confirmado un pago parcial o total del valor de la OxpComercio por vía directa (sin pasar por extracto ni anticipo). Evento de progreso — reduce `saldoPorPagar()` sin cambiar de estado. Puede coexistir con pagos tipo extracto y anticipo sobre la misma OxpComercio. |
| **Agregado** | OxpComercio |
| **Estado previo** | Causada. |
| **Estado resultante** | Causada (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: emite `OxpComercioPagada`. |
| **Precondiciones** | OxpComercio causada. El sistema contable confirma el pago. `saldoPorPagar()` suficiente para el monto pagado. |
| **Información capturada** | Referencia de pago del sistema contable (incluye identificador del destino físico que originó el pago, ej: número de transacción SincoA&F), valor pagado, fecha. |
| **Efectos** | Crea entidad `PagoAplicado` (tipo: pago_directo). Reduce `saldoPorPagar()`. **Detección de doble pago (`[R38]` `[I23]`):** el pago directo es un hecho consumado (el dinero ya salió del sistema contable) — el control no puede prevenir en este canal: si el Proveedor tiene anticipos abiertos sin resolver, el pago se aplica igual y se emite `AlertaDoblePagoPotencial` (derivado por configuración). La prevención de este canal vive en la verificación temprana de la confirmación. |

#### ConstanciaAnticipoNoAplicableRegistrada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Un usuario dejó constancia explícita y justificada de que un anticipo abierto del Proveedor **no corresponde** a esta OXP (`[R38]`). Es el único mecanismo válido cuando el anticipo no aplica: la pertenencia anticipo↔OXP nunca es calculable por el sistema (`[D33]`) — siempre es juicio humano. |
| **Causalidad** | Directa (comando `RegistrarConstanciaAnticipoNoAplicable`). |
| **Agregado** | OxpComercio |
| **Estado previo** | Pendiente, Confirmada o Causada, con `saldoPorPagar()` > 0. |
| **Estado resultante** | Sin cambio (progreso). |
| **Precondiciones** | El anticipo es del mismo Proveedor (referencia `proveedorId`) y tiene `saldoPorRegularizar()` > 0. Motivo obligatorio. Sin constancia previa para el mismo anticipo en esta OXP. |
| **Información capturada** | anticipoId, motivo, usuarioId, fecha. |
| **Efectos** | Crea la entidad `ConstanciaAnticipoNoAplicable`. El anticipo queda **resuelto para esta OXP** a efectos de `[I23]`: la confirmación y la vinculación con extracto proceden, y la alerta de pago directo no se emite por ese anticipo. No afecta al anticipo (sigue abierto para otras OXP). Si existía `AlertaDoblePagoPotencial` por ese anticipo, el read model de alertas la marca resuelta. |

#### AlertaDoblePagoPotencial

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Un pago directo se aplicó sobre una OXP cuyo Proveedor tenía anticipos abiertos sin resolver — posible doble pago (anticipo entregado + factura pagada). El canal automático no puede prevenir (el dinero ya salió): detecta y alerta (`[R38]`). |
| **Causalidad** | Derivado por configuración de `PagoOxpComercioDirectoAplicado` (solo cuando `[I23]` está habilitada por empresa y existe anticipo abierto sin resolver). |
| **Agregado** | OxpComercio |
| **Estado previo** | Causada. |
| **Estado resultante** | Sin cambio (progreso — evento informativo, mismo patrón de `AlertaPlazoAnticipoVencido`). |
| **Precondiciones** | Pago directo aplicado con anticipo(s) del Proveedor con `saldoPorRegularizar()` > 0, sin regularización aplicada a esta OXP ni constancia registrada. |
| **Información capturada** | anticipoId(s) abiertos sin resolver, valor pagado, referencia de pago del sistema contable. |
| **Efectos** | Evento de dominio (se persiste en el stream). Consumidor: read model de alertas y panel de trabajo. Resolución implícita: la alerta se marca resuelta cuando el anticipo se regulariza (contra esta u otra OXP), se cierra, se reversa, o se registra `ConstanciaAnticipoNoAplicableRegistrada` para él en esta OXP. |

#### OxpComercioPagada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La obligación ha sido liquidada financieramente. `saldoPorPagar()` = 0. El pago pudo realizarse mediante extracto (conciliación), anticipo (regularización), pago directo (confirmado por el sistema contable), devolución (`ServicioDeAplicacionDevolucion`), o una combinación de estos. |
| **Causalidad** | Derivado por transición — emitido cuando `saldoPorPagar()` = 0. |
| **Agregado** | OxpComercio |
| **Estado previo** | Causada. Si pagos internos cubrieron 100% en Confirmada, se emite como derivado por transición de `OxpComercioCausada`. |
| **Estado resultante** | Pagada (estado terminal). |
| **Precondiciones** | `saldoPorPagar()` = 0. |
| **Información capturada** | Total de pagos aplicados (cantidad, suma de valores, detalle por tipo extracto/anticipo/pago_directo/devolucion), fecha de cierre. |
| **Efectos** | Transiciona a Pagada. Cierre operativo de la OxpComercio. |

#### PagoExtractoAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable ha confirmado un pago parcial o total contra el extracto. Evento de progreso — reduce `saldoPorPagar()` del OxpExtracto sin cambiar de estado. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Causada. |
| **Estado resultante** | Causada (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0: emite `ExtractoPagado`. |
| **Precondiciones** | Extracto causado. El sistema contable confirma pago. `saldoPorPagar()` suficiente para el monto pagado. |
| **Información capturada** | Referencia de pago del sistema contable (incluye identificador del destino físico que originó el pago, ej: número de transacción SincoA&F), monto pagado, fecha. |
| **Efectos** | Crea entidad `CrucePagoExtractoAplicado` (tipo: pago_sincoa). Reduce `saldoPorPagar()`. |

#### PagoExtractoViaDevolucionAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una devolución confirmada (tipo Extracto) cubre parcial o totalmente el valor por pagar del extracto. Evento de progreso — reduce `saldoPorPagar()` del OxpExtracto sin cambiar de estado. Emitido por `ServicioDeAplicacionDevolucion`. Puede coexistir con pagos tipo pago_sincoa sobre el mismo extracto. |
| **Causalidad** | Efecto inter-agregado — `ServicioDeAplicacionDevolucion` (Rama Extracto). |
| **Agregado** | OxpExtracto |
| **Estado previo** | Confirmada o Causada. |
| **Estado resultante** | Confirmada o Causada (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0 en Causada: emite `ExtractoPagado`. Si `saldoPorPagar()` = 0 en Confirmada: `ExtractoPagado` se emitirá como derivado por transición al causarse. |
| **Precondiciones** | OxpExtracto en estado Confirmada o posterior. Devolucion tipo Extracto en confirmación. `saldoPorPagar()` suficiente para el monto cubierto. Coordinado por `ServicioDeAplicacionDevolucion`. |
| **Información capturada** | Referencia a Devolucion, monto cubierto, fecha. |
| **Efectos** | Crea entidad `CrucePagoExtractoAplicado` (tipo: devolucion). Reduce `saldoPorPagar()`. |

#### PagoExtractoViaDevolucionRevertido

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Evento compensatorio de `PagoExtractoViaDevolucionAplicado`. Revierte el `CrucePagoExtractoAplicado` (tipo: devolucion) cuando el proceso de aplicación de devolución falló permanentemente. Restaura `saldoPorPagar()`. Solo emitido por compensación del `ServicioDeAplicacionDevolucion` `[SI3]` — nunca por operación de negocio directa. Se emite junto con `DevolucionRevertida` → stream Devolucion. |
| **Causalidad** | Evento compensatorio de `PagoExtractoViaDevolucionAplicado` — `ServicioDeAplicacionDevolucion` `[SI3]`. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Confirmada o Causada. |
| **Estado resultante** | Confirmada o Causada (restaura `saldoPorPagar()`). |
| **Precondiciones** | Existe `CrucePagoExtractoAplicado` (tipo: devolucion) correspondiente al `correlationId` del proceso fallido `[D20]`. |
| **Información capturada** | Referencia a Devolucion, monto restaurado, `correlationId` del proceso, motivo del fallo. |
| **Efectos** | Elimina `CrucePagoExtractoAplicado` (tipo: devolucion). Restaura `saldoPorPagar()`. |

#### PagoOxpComercioViaDevolucionAplicado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Una devolución confirmada cubre parcial o totalmente el valor por pagar de la OxpComercio. Evento de progreso — reduce `saldoPorPagar()` sin cambiar de estado. Emitido por `ServicioDeAplicacionDevolucion` como parte de la confirmación de la devolución. Puede coexistir con pagos tipo extracto, anticipo y pago directo sobre la misma OxpComercio. |
| **Causalidad** | Efecto inter-agregado — `ServicioDeAplicacionDevolucion` (Rama Comercio-A). |
| **Agregado** | OxpComercio |
| **Estado previo** | Confirmada o Causada. |
| **Estado resultante** | Confirmada o Causada (reduce `saldoPorPagar()`). Si `saldoPorPagar()` = 0 en Causada: emite `OxpComercioPagada`. Si `saldoPorPagar()` = 0 en Confirmada: `OxpComercioPagada` se emitirá como derivado por transición al causarse. |
| **Precondiciones** | OxpComercio en estado Confirmada o posterior. Devolucion en confirmación referenciando esta OxpComercio. `saldoPorPagar()` suficiente para el monto cubierto. Coordinado por `ServicioDeAplicacionDevolucion`. |
| **Información capturada** | Referencia a Devolucion, monto cubierto por devolución, fecha. |
| **Efectos** | Crea entidad `PagoAplicado` (tipo: devolucion). Reduce `saldoPorPagar()`. |

#### PagoOxpComercioViaDevolucionRevertido

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Evento compensatorio de `PagoOxpComercioViaDevolucionAplicado`. Revierte el `PagoAplicado` (tipo: devolucion) cuando el proceso de aplicación de devolución falló permanentemente. Restaura `saldoPorPagar()`. Solo emitido por compensación del `ServicioDeAplicacionDevolucion` `[SI3]` — nunca por operación de negocio directa. Se emite junto con `DevolucionRevertida` → stream Devolucion. |
| **Causalidad** | Evento compensatorio de `PagoOxpComercioViaDevolucionAplicado` — `ServicioDeAplicacionDevolucion` `[SI3]`. |
| **Agregado** | OxpComercio |
| **Estado previo** | Confirmada, Causada o Pagada (si `OxpComercioPagada` fue derivado de este pago). |
| **Estado resultante** | Confirmada o Causada (restaura `saldoPorPagar()`). Si estaba en Pagada por efecto derivado de este pago, retorna al estado previo a Pagada. |
| **Precondiciones** | Existe `PagoAplicado` (tipo: devolucion) correspondiente al `correlationId` del proceso fallido `[D20]`. |
| **Información capturada** | Referencia a Devolucion, monto restaurado, `correlationId` del proceso, motivo del fallo. |
| **Efectos** | Elimina `PagoAplicado` (tipo: devolucion). Restaura `saldoPorPagar()`. |

#### ExtractoPagado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El extracto ha sido completamente pagado. `saldoPorPagar()` = 0. |
| **Causalidad** | Derivado por transición — emitido cuando `saldoPorPagar()` = 0. |
| **Agregado** | OxpExtracto |
| **Estado previo** | Causada. Si devolución cubrió 100% en Confirmada, se emite como derivado por transición de `ExtractoCausado`. |
| **Estado resultante** | Pagada (estado terminal). |
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
| **Precondiciones** | **Comercio:** OxpComercio existe. Mismo Proveedor (referencia `proveedorId` del origen). `valorNeto(devolucion)` ≤ `valorNeto(OxpComercio)`. Acumulado I17. OxpComercio en Confirmada o posterior. Soporte documental adjunto (nota crédito `[R28]`). **Extracto:** OxpExtracto existe. `valorNeto(devolucion)` ≤ saldoPorPagar(OxpExtracto). OxpExtracto en estado Confirmada o posterior. **Anticipo:** Anticipo existe. Anticipo en estado Vigente o Confirmada (estados pre-causación). `saldoPorPagar()` = valorTotal (sin cruces de pago). `saldoPorRegularizar()` = valorAnticipo (sin cruces de regularización). `valorNeto(devolucion)` = valorTotal del anticipo (solo reversa total). Mismo Proveedor. |
| **Información capturada** | Referencia a OXP origen (tipo + ID, obligatoria, inmutable), `proveedorId` (heredado del agregado OXP origen `[D31]`), tercero (NIT, razón social — coincide con el del origen), entidades internas según tipo de OXP: `ConceptoDevuelto`(s) para Comercio, `CargoFinancieroDevuelto`(s) para Extracto, `ReversaTotal` para Anticipo. Soportes documentales adjuntos. **Comercio:** adicionalmente moneda, TRM, distribución de costos. |
| **Efectos** | Devolución disponible para confirmación. |

#### DevolucionConfirmada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | La devolución ha sido validada y el crédito ha sido aplicado contra el agregado OXP origen. Coordinado por `ServicioDeAplicacionDevolucion`. Los efectos dependen del tipo de OXP. |
| **Causalidad** | Efecto inter-agregado — `ServicioDeAplicacionDevolucion`. |
| **Agregado** | Devolucion |
| **Estado previo** | Pendiente. |
| **Estado resultante** | Confirmada. |
| **Precondiciones** | Devolucion en estado Pendiente. Agregado OXP origen existe y cumple precondiciones según tipo (ver `DevolucionRadicada`). |
| **Información capturada** | Resultado de la aplicación según tipo de OXP: monto aplicado, referencia a Anticipo creado (si aplica, solo Comercio), fecha de confirmación. |
| **Efectos** | **Comercio — Rama A** (`saldoPorPagar > 0`, `devolucion ≤ saldoPorPagar`): emite `PagoOxpComercioViaDevolucionAplicado` → stream OxpComercio. **Comercio — Rama B** (`saldoPorPagar = 0`): crea nuevo Anticipo (valorTotal = valorNeto(devolucion), dimensión pago resuelta → estado Pagado, `saldoPorRegularizar()` pendiente). **Comercio — Rama C** (`saldoPorPagar > 0`, `devolucion > saldoPorPagar`): emite `PagoOxpComercioViaDevolucionAplicado` por `saldoPorPagar` → stream OxpComercio + crea nuevo Anticipo por excedente (`valorNeto(devolucion) - saldoPorPagar`), estado Pagado, `saldoPorRegularizar()` pendiente. **Extracto:** Emite `PagoExtractoViaDevolucionAplicado` → stream OxpExtracto. **Anticipo:** Emite `AnticipoReversado` → stream Anticipo (crea `CrucePagoAplicado` tipo reversa y `CruceRegularizacionAplicada` tipo reversa, saldos → 0, estado → Reversado). |

#### DevolucionRevertida

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Evento compensatorio de `DevolucionConfirmada`. Revierte la confirmación de la devolución cuando el efecto posterior en el agregado OXP origen falló permanentemente. Solo emitido por compensación del `ServicioDeAplicacionDevolucion` `[SI3]` — nunca por operación de negocio directa. Se emite junto con el evento compensatorio del efecto fallido (`PagoOxpComercioViaDevolucionRevertido` o `PagoExtractoViaDevolucionRevertido`). |
| **Causalidad** | Evento compensatorio de `DevolucionConfirmada` — `ServicioDeAplicacionDevolucion` `[SI3]`. |
| **Agregado** | Devolucion |
| **Estado previo** | Confirmada. |
| **Estado resultante** | Pendiente (devolución disponible para nuevo intento de confirmación). |
| **Precondiciones** | Devolucion en estado Confirmada. Proceso de aplicación fallido permanentemente (`correlationId` `[D20]`). |
| **Información capturada** | `correlationId` del proceso fallido, motivo del fallo, fecha de reversión. |
| **Efectos** | Devolución retorna a Pendiente. Disponible para nuevo intento de confirmación por `ServicioDeAplicacionDevolucion`. |

#### DevolucionCausada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable ha confirmado el registro exitoso de la nota crédito correspondiente a esta devolución. Estado terminal. |
| **Agregado** | Devolucion |
| **Estado previo** | Confirmada. |
| **Estado resultante** | Causada (estado terminal). |
| **Precondiciones** | Devolucion confirmada. El sistema contable confirma registro exitoso de la nota crédito `[R16]`. |
| **Información capturada** | Fecha de causación. La `referenciaDestino` (número de asiento contable externo) se persiste de manera asíncrona al recibir `EntregaAceptada` del sub-dominio Contabilidad como información complementaria del documento causado (ver `[D28]` y `[SI6]`). |
| **Efectos** | Integración saliente: causación de la devolución enviada al sistema contable (JSON) `[R16]` con `lineasParaTraduccion()` y `tipoTransaccion` según el tipo de la devolución `[D27]`: `nota_credito_gasto` para devoluciones tipo Comercio y tipo Extracto; `reversa_anticipo` para devoluciones tipo Anticipo (esta última requiere plantilla nueva #7 en el inventario del sub-dominio Contabilidad). Espera `EntregaAceptada` del sub-dominio Contabilidad para registrar la `referenciaDestino`. |

---

### 5.7. CatalogoGastoDirecto (configuración)

Eventos de configuración del catálogo de gasto directo `[D21]`. Patrón uniforme: el agregado se crea una vez y los conceptos se agregan, modifican o desactivan. No hay FSM transaccional — todos los eventos aplican desde cualquier punto del ciclo de vida del agregado.

| # | Evento | Descripción | Información capturada | Precondiciones |
|:---:|---|---|---|---|
| 1 | `CatalogoGastoDirectoCreado` | Se creó el catálogo de gasto directo. | empresaId, fecha de creación. | — |
| 2 | `ConceptoGastoDirectoAgregado` | Se registró un nuevo concepto de gasto disponible para obligaciones directas. | Código, descripción, clasificacionTributaria (ref. Impuestos), conceptoPago (ref. Impuestos), activo. | Código único dentro del catálogo `[I18]`. clasificacionTributaria y conceptoPago deben ser referencias válidas al catálogo de Impuestos `[D22]`. |
| 3 | `ConceptoGastoDirectoModificado` | Se actualizaron atributos de un concepto existente. | Código (identifica), descripción, clasificacionTributaria, conceptoPago (campos modificados). | Concepto existe y está activo. clasificacionTributaria y conceptoPago deben ser referencias válidas al catálogo de Impuestos `[D22]`. |
| 4 | `ConceptoGastoDirectoDesactivado` | Un concepto dejó de estar disponible para nuevas obligaciones. Se conserva por trazabilidad — las OxpComercio existentes que lo referencian no se afectan. | Código, motivo. | Concepto existe y está activo. |

---

### 5.8. Proveedor

#### ProveedorRegistrado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se registró un proveedor nuevo en OXP — el registro propio del tercero con quien se contraen obligaciones. |
| **Causalidad** | Directa (`AsegurarProveedor`, cuando no existe la clave natural). |
| **Agregado** | Proveedor |
| **Estado previo** | (nuevo) — no existía. |
| **Estado resultante** | Activo. |
| **Precondiciones** | Identificación legal válida (validación empaquetada: tipo para el país, formato, DV según política); clave natural sin Proveedor existente (I19). La asistencia de captura de la bodega pudo advertir duplicados — el usuario decidió (no bloqueante). |
| **Información capturada** | `proveedorId`; `identificacionLegal` { tipoDocumento, numero, pais, digitoVerificacion? }; `razonSocial`; `tipoPersona`; `direcciones` [ { DireccionFisica, tipoUso } ]; `contactos` [ { contacto, esPrincipal } ]. |
| **Efectos** | Emite el evento estándar de rol hacia la bodega (derivado por transición, `secuencia` = 1) — OXP informa, la bodega consolida. |

#### ProveedorActualizado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se actualizaron datos del proveedor por decisión explícita de un usuario de OXP. |
| **Causalidad** | Directa (`ActualizarProveedor`). |
| **Agregado** | Proveedor |
| **Estado previo / resultante** | Activo o Inactivo / sin cambio (progreso). |
| **Precondiciones** | El Proveedor existe. Si cambia la identificación legal: validación empaquetada de la nueva. |
| **Información capturada** | Campos modificados (delta): de `razonSocial`, `tipoPersona`, `identificacionLegal`, `direcciones`, `contactos` — solo los que cambiaron, con identificadores. |
| **Efectos** | Emite el evento estándar de rol (estado completo, `secuencia` incrementada). Si cambió un dato de identidad compartido y otra fuente de la bodega lo tiene distinto, la bodega abrirá divergencia — comportamiento esperado. |

#### ProveedorInactivado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El proveedor dejó de estar disponible para nuevas operaciones: por decisión comercial local de OXP o por aplicación automática de la señal global de la bodega. |
| **Causalidad** | Directa (`InactivarProveedor`, origen `local`) o efecto de integración (aviso `TerceroInactivado` de la bodega, origen `senal_global` — aplicado automáticamente, sin intervención). |
| **Agregado** | Proveedor |
| **Estado previo** | Activo. |
| **Estado resultante** | Inactivo. |
| **Precondiciones** | Origen `local`: motivo obligatorio y permiso del usuario. Origen `senal_global`: la clave natural del aviso corresponde a este registro. |
| **Información capturada** | `motivoInactivacion` { origen, codigo, descripcion }; `usuarioId` (solo origen local). |
| **Efectos** | Las radicaciones nuevas con este proveedor quedan impedidas (I22); el historial no se toca. Emite el evento estándar de rol (`estadoEnOrigen` = inactivo). |

#### ProveedorReactivado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El proveedor vuelve a estar disponible para nuevas operaciones. |
| **Causalidad** | Directa (`ReactivarProveedor`, solo si la inactivación vigente es de origen `local`) o efecto de integración (aviso `TerceroReactivado` de la bodega). |
| **Agregado** | Proveedor |
| **Estado previo / resultante** | Inactivo / Activo. |
| **Precondiciones** | **I21:** si la inactivación vigente tiene origen `senal_global`, solo la señal de reactivación de la bodega procede — el veto global no se levanta localmente. |
| **Información capturada** | `motivo` { origen, codigo, descripcion }; `usuarioId` (solo origen local). |
| **Efectos** | Emite el evento estándar de rol (`estadoEnOrigen` = activo). |

#### CorreccionDeIdentidadAplicada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | OXP aplicó automáticamente una resolución de conciliación de la bodega: el dato de identidad de este registro estaba errado y quedó corregido — injerencia por mensajes, nunca escritura remota. |
| **Causalidad** | Efecto de integración (aviso `DatoDeIdentidadCorregido` cuyo `registrosACorregir` incluye este `proveedorId`). |
| **Agregado** | Proveedor |
| **Estado previo / resultante** | Activo o Inactivo / sin cambio (progreso). |
| **Precondiciones** | El aviso señala este registro por (`dominio` = OXP, `referenciaOrigen` = `proveedorId`); el valor actual difiere del corregido. |
| **Información capturada** | `dato` (RazonSocial / TipoPersona / IdentificacionLegal); `valorAnterior`; `valorNuevo`; `conciliacionId` (trazabilidad a la decisión de la bodega). |
| **Efectos** | Emite el evento estándar de rol con el dato corregido — la corrección **regresa a la bodega por el flujo normal** y permite el cierre por convergencia de la divergencia. Las transacciones históricas no se modifican (sus `InformacionTercero` embebidos son el hecho económico de su momento); las radicaciones futuras copian el dato corregido. |

#### Evento estándar de rol (integración → bodega de Terceros)

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | OXP informa a la bodega el estado completo de su Proveedor — el contrato de entrada del modelo de Terceros (Sección 5.2 de ese documento). Es el evento que convierte a OXP en fuente de la bodega. |
| **Causalidad** | Derivado por transición de cada evento de dominio del Proveedor (mismo append; la entrega usa el patrón de outbox `[SI6]`). |
| **Agregado** | Proveedor |
| **Estado previo / resultante** | El del evento que lo deriva / sin cambio. |
| **Precondiciones** | — (acompaña siempre al evento de dominio). |
| **Información capturada** | El contrato completo: `identificacionLegal`, `razonSocial`, `tipoPersona`; `rol` = `proveedor`, `dominio` = OXP, `empresa` (del contexto), `referenciaOrigen` = `proveedorId`, `estadoEnOrigen` (activo/inactivo — mapeo directo del estado), `secuencia`; `direcciones`; `contactos` [ { contacto, esPrincipal } ]; `fechaDelHecho`. **Estado completo, no delta** — la bodega tolera pérdida y desorden (contrato `[D5]` de Terceros). |
| **Efectos** | La bodega consolida (crea o actualiza el rol del tercero), evalúa señales de duplicado/divergencia y actualiza la ficha. Si la bodega no está disponible, el evento espera en la cola — la operación de OXP no se entera. |

### 5.9. CatalogoReglasDistribucion (configuración)

Eventos del agregado de configuración que materializa el **Nivel A** de la cadena de resolución de la unidad organizacional (`[D36]`). Ciclo de vida CRUD, sin FSM.

| # | Evento | Descripción | Información capturada | Precondiciones |
|---|--------|-------------|----------------------|----------------|
| 1 | `CatalogoReglasDistribucionCreado` | Se creó el catálogo de reglas de distribución de la empresa. | `empresaId`, fecha de creación. | — |
| 2 | `ReglaDeDistribucionAgregada` | Se registró una nueva regla de preferencia de distribución. | `reglaId`, criterios (`criterioProveedor`, `criterioTipoGasto`, `criterioLugarEjecucion` — opcionales), `distribucion` (lista de `DestinoDeNegocio`), `activo`. | La `distribucion` suma 100% (`I25`); no existe otra regla **activa** con la misma combinación exacta de criterios (evita empate irresoluble); las unidades referenciadas existen en la copia local (`[SI8]`). |
| 3 | `ReglaDeDistribucionModificada` | Se actualizaron criterios o la distribución de una regla existente. | `reglaId` (identifica), campos modificados (criterios y/o `distribucion`). | Regla existe y está activa; la `distribucion` resultante suma 100% (`I25`); no genera combinación de criterios duplicada con otra activa. |
| 4 | `ReglaDeDistribucionDesactivada` | Una regla dejó de aplicarse a nuevas transacciones. Se conserva por trazabilidad — las transacciones ya resueltas con ella no se afectan. | `reglaId`, motivo. | Regla existe y está activa. |

> Una regla **promovida desde el aprendizaje** (Nivel B → Nivel A, `[D36]`/`[SI10]`) entra por `ReglaDeDistribucionAgregada` como cualquier otra regla. La invalidación de un aprendizaje vive en la proyección de aprendizaje (`[SI10]`), no aquí.

## 6. Tipos de concepto

El agregado `OxpComercio` tiene una única entidad interna (`ConceptoDeGasto`) que contiene su desglose fiscal como Value Objects (`DesgloseFiscal` → `Tributo`). El agregado `Devolucion` tiene tres entidades polimórficas con contrato común (`descripcion`, `valor`): `ConceptoDevuelto` (Comercio), `CargoFinancieroDevuelto` (Extracto) y `ReversaTotal` (Anticipo). La distribución de costos se gestiona mediante instrucciones separadas a nivel del agregado (ver Sección 3, reglas de consistencia). OXP captura la información de negocio; la traducción a lenguaje contable es responsabilidad del servicio de **Traducción Contable** en la frontera OXP → sistema contable.

### ConceptoDeGasto

| Aspecto | Detalle |
|---------|---------|
| **Componente** | Entidad interna de OxpComercio |
| **Clasificación** | Entidad (tiene identidad — puede haber duplicados con mismos atributos). |
| **Aparece en** | Radicación. |
| **Información (dominio OXP)** | Código, descripción, cantidad, valor, clasificacionTributaria (ref. catálogo Impuestos `[D9-Imp]`), conceptoPago (ref. catálogo Impuestos `[D9-Imp]`), referenciaOrigen (código del concepto en el catálogo del sub-dominio origen). Contiene `DesgloseFiscal` con los tributos derivados del sub-dominio de Impuestos. |
| **Distribución** | Gestionada por `InstruccionDistribucion` a nivel del agregado `[R05c]`. No vive dentro del concepto. |
| **Traducción contable (frontera)** | El servicio de Traducción Contable deduce cuenta de gasto o costo, centro de costo y naturaleza (débito) a partir del destino de negocio y reglas configuradas. |

### Tributo (Impuesto)

| Aspecto | Detalle |
|---------|---------|
| **Componente** | Value Object dentro de `DesgloseFiscal` de un `ConceptoDeGasto` |
| **Clasificación** | Value Object (inmutable — se reemplaza al recalcular). |
| **Aparece en** | Radicación. Derivado del `ConceptoDeGasto` al que pertenece. |
| **Información (dominio OXP)** | Tipo de impuesto (IVA, ICA, etc.), base gravable, tarifa, valor calculado. Determinado por el sub-dominio de Impuestos mediante solicitud de cálculo con `clasificacionTributaria` y `conceptoPago` del `ConceptoDeGasto` padre. |
| **Distribución** | Gestionada por `InstruccionDistribucion`. Por defecto hereda del `ConceptoDeGasto` padre; puede sobrescribirse. |
| **Traducción contable (frontera)** | El servicio de Traducción Contable deduce cuenta de impuesto descontable o gasto según normativa. |

### Tributo (Retención)

| Aspecto | Detalle |
|---------|---------|
| **Componente** | Value Object dentro de `DesgloseFiscal` de un `ConceptoDeGasto` |
| **Clasificación** | Value Object (inmutable — se reemplaza al recalcular). |
| **Aparece en** | Radicación. Derivado del `ConceptoDeGasto` al que pertenece. |
| **Información (dominio OXP)** | Tipo de retención (ReteFuente, ReteIVA, ReteICA), base, tarifa, valor retenido. Determinado por el sub-dominio de Impuestos. En dirección de gasto, las retenciones se practican al confirmar la transacción — no al pagar `[P3]`. |
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
| **Información (dominio OXP)** | descripcion, valor (`ValorMonetario`), codigo, cantidad, clasificacionTributaria, conceptoPago, referenciaOrigen. `DesgloseFiscal` (VO) — derivado por prorrateo proporcional del desglose del gravamen original (el motor de cálculo no participa). Al confirmar la devolución, OXP envía comando de confirmación a Impuestos con `efectoFiscal = desgravamen` y `transaccionOrigenId` = la OxpComercio original. |
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

Las invariantes son restricciones estructurales que deben ser verdaderas en todo momento del ciclo de vida del dominio. A diferencia de las reglas de negocio (R01–R37), que pueden ser configurables y tener excepciones, las invariantes son absolutas — excepto aquellas condicionadas por configuración de empresa, que aplican solo cuando están habilitadas (ej: I6). Clasificación: **local** (enforceada por un solo agregado, transaccional) o **eventual** (cruza fronteras de agregado, enforceada por proyección con ventana de inconsistencia mínima `[SI4]`). Se indica entre paréntesis cuando es eventual.

| # | Invariante | Agregado | Referencia |
|---|-----------|----------|------------|
| I1 | **Unicidad de obligación (eventual):** No pueden existir dos OxpComercio con el mismo NIT + número de soporte dentro de la ventana de 24 meses. | OxpComercio | `[R26]` |
| I2 | **Integridad de distribución:** Para cualquier `InstruccionDistribucion` de los agregados OxpComercio, OxpExtracto, Anticipo o Devolucion, la suma de sus `DestinoDeNegocio` es exactamente 100%. Se valida por cada instrucción individual, ya sea referenciando un `ConceptoDeGasto`, un `ConceptoDevuelto` (Devolucion tipo Comercio), un `Tributo`, un `CargoFinanciero`, un ajuste (diferencia cambio o tolerancia), o el valor global de un anticipo (sin desglose fiscal `[P1]`). | OxpComercio, OxpExtracto, Anticipo, Devolucion | `[R05c]` |
| I3 | **Completitud de conciliación:** Un OxpExtracto en estado Conciliada tiene el 100% de sus `PartidaExtracto` resueltas (vinculadas a OxpComercio, cubiertas por anticipo, cubiertas por devolución, descartadas o marcadas como disputa). Los `CargoFinanciero` no participan en el conteo de completitud — se consideran conciliados automáticamente como componentes propios del extracto `[R06]`. | OxpExtracto | `[R06]` |
| I4a | **Progresión de estados — OxpComercio:** Solo puede avanzar en su máquina de estados. Excepciones: (1) Devuelta → Pendiente (corrección), (2) cruce tipo `revertido` por saga `[SI3]` ante fallo permanente — contrarresta el cruce que provocó la transición y retorna al estado previo. Estado terminal: Pagada. | OxpComercio | — |
| I4b | **Progresión de estados — OxpExtracto:** Solo puede avanzar en su máquina de estados. Excepción: cruce tipo `revertido` por saga `[SI3]` ante fallo permanente — contrarresta el cruce que provocó la transición y retorna al estado previo. Estado terminal: Pagada. | OxpExtracto | — |
| I4c | **Progresión de estados — Anticipo:** Solo puede avanzar en su máquina de estados (Vigente → Confirmada → Causada → Pagado/Regularizado → Cerrado). Excepción: cruce tipo `revertido` en `CruceRegularizacionAplicada` por saga `[SI3]` ante fallo permanente — contrarresta el cruce de regularización que provocó la transición y retorna al estado previo. Estados terminales: Cerrado (`saldoPorPagar()` = 0 y `saldoPorRegularizar()` = 0) y Reversado (transición terminal desde Vigente o Confirmada por reversión total vía `ServicioDeAplicacionDevolucion` — no es retroceso). | Anticipo | — |
| I4d | **Progresión de estados — Devolucion:** Solo puede avanzar en su máquina de estados. Excepción: `DevolucionRevertida` por saga `[SI3]` ante fallo permanente — retorna de Confirmada a Pendiente para nuevo intento de confirmación. Estado terminal: Causada. | Devolucion | — |
| I5 | **Consistencia de moneda:** (a) Toda OxpComercio en moneda extranjera almacena tanto el valor en moneda de origen como el valor en moneda funcional `[R05b]`. (b) Toda `PartidaExtracto` en moneda extranjera almacena valor original, moneda original y TRM, además del valor en la moneda del extracto `[R05d]`. (c) Un OxpExtracto opera en una sola moneda: la moneda homogénea de sus partidas, o moneda funcional si las partidas tienen monedas mixtas. | OxpComercio, OxpExtracto | `[R05b]` `[R05d]` |
| I6 | **Segregación de funciones:** El usuario que confirma una OXP no puede ser el mismo que la radicó (cuando está habilitada por empresa). Nota: esta restricción es configurable por empresa `[R25]` — aplica como invariante solo cuando está habilitada. En empresas donde no está habilitada, no se valida. | OxpComercio, OxpExtracto | `[R25]` |
| I7 | **Vinculación coherente (eventual):** Una OxpComercio solo puede estar vinculada a un único OxpExtracto. Un OxpExtracto puede tener N vinculaciones. La vinculación (conciliación) genera un `PagoAplicado` tipo extracto, pero no implica pago total — la OxpComercio puede tener pagos adicionales de otras fuentes. Enforcement: validación en `ServicioDeConciliacion` (precondición de vinculación — verifica que la OxpComercio no tenga vinculación previa) + proyección eventual `[SI4]` para detección tardía. | Inter-agregado | — |
| I8 | **Causalidad de anticipo:** Un anticipo solo puede recibir cruces parciales si no ha alcanzado un estado terminal (Cerrado o Reversado). Cruces de pago externos (`CrucePagoAplicado` tipo extracto o pago_directo): permitidos desde estado Causada o Regularizado `[I16]`, `saldoPorPagar()` suficiente, mismo Proveedor (referencia `proveedorId`) para cobertura de partida. Cruces de pago tipo devolucion: aplicados como efecto del registro inicial en Ramas B/C del `ServicioDeAplicacionDevolucion` (mismo append que `AnticipoRegistrado` + `AnticipoConfirmado` + `AnticipoCausado`) — no se aplican como operación posterior. Cruces de regularización (`CruceRegularizacionAplicada`): permitidos desde estado Causada o Pagado, `saldoPorRegularizar()` suficiente. Cruces tipo `reversa`: exclusivos desde Vigente o Confirmada sin cruces previos — emitidos únicamente por `AnticipoReversado` como parte de la reversión total. | Anticipo | — |
| I9 | **Confirmación externa de causación:** Un documento del dominio OXP solo transiciona a estado Causada cuando el sistema contable confirma el registro. El dominio OXP no auto-declara la causación. | OxpComercio, OxpExtracto, Anticipo, Devolucion | — |
| I10 | **Consistencia de distribución:** Toda `InstruccionDistribucion` referencia un componente existente del agregado. **OxpComercio:** `ConceptoDeGasto` o `Tributo`; cadena de resolución (`[D36]`): instrucción explícita → herencia del gasto padre → reglas de preferencia de distribución (Nivel A, determinístico) → sugerencia por aprendizaje (Nivel B, no vinculante) → destino único pendiente. **Devolucion tipo Comercio:** `ConceptoDevuelto` o `Tributo`; misma cadena de resolución. **OxpExtracto:** `CargoFinanciero`, `AjustePorDiferenciaCambio` o `AjustePorTolerancia`; sin herencia (cada componente tiene instrucción propia o resuelve por reglas de preferencia `[D36]`). **Anticipo:** instrucción única sobre el valor global (sin desglose `[P1]`); reglas de preferencia `[D36]` o destino único pendiente. Al agregar, eliminar o recalcular componentes, el agregado mantiene la coherencia. **Validez contra la copia local de unidades (`I24`, `[D34]`):** la unidad resuelta por la cadena debe existir y estar Activa en la copia local de Estructura Organizacional; si la unidad aún no existe o no está activa, el componente cae en **destino único pendiente** y la causación de esa parte se difiere hasta que llegue su `UnidadActivada` (sin aproximar con unidad provisional). | OxpComercio, OxpExtracto, Anticipo, Devolucion | `[R05c]` |
| I11 | **Saldos no negativos del Anticipo:** `saldoPorPagar()` ≥ 0 y `saldoPorRegularizar()` ≥ 0. La suma de los `CrucePagoAplicado` no puede superar el valorTotal. La suma de los `CruceRegularizacionAplicada` no puede superar el valor anticipo. | Anticipo | — |
| I12 | **Consistencia de estado del Anticipo:** | Anticipo | — |

| Estado | saldoPorPagar() | saldoPorRegularizar() | Nota |
|---|---|---|---|
| Vigente | > 0 | > 0 | — |
| Pagado | = 0 | > 0 | Anticipo creado por `ServicioDeAplicacionDevolucion` nace en este estado |
| Regularizado | > 0 | = 0 | — |
| Cerrado | = 0 | = 0 | — |
| Reversado | = 0 | = 0 | Cruces tipo `reversa`. Terminal — no admite más cruces ni regularizaciones |
| I13 | **Saldos no negativos de OxpComercio:** `saldoPorPagar()` ≥ 0. La suma de los `PagoAplicado`.valor no puede superar `valorNeto()`. | OxpComercio | — |
| I14 | **Saldos no negativos de OxpExtracto:** `saldoPorPagar()` ≥ 0. La suma de los `CrucePagoExtractoAplicado`.valor no puede superar `valorTotalExtracto()`. | OxpExtracto | — |
| I15 | **Consistencia de estado de pago:** OxpComercio en estado Confirmada tiene `saldoPorPagar()` ≥ 0 (puede reducirse por regularización de anticipo). OxpComercio en estado Causada tiene `saldoPorPagar()` ≥ 0 — si al causarse `saldoPorPagar()` = 0 (anticipo cubrió 100% en Confirmada), `OxpComercioPagada` se emite como derivado por transición. OxpComercio en estado Pagada tiene `saldoPorPagar()` = 0. OxpExtracto en estado Confirmada tiene `saldoPorPagar()` ≥ 0 (puede reducirse por devolución). OxpExtracto en estado Causada tiene `saldoPorPagar()` ≥ 0 — si al causarse `saldoPorPagar()` = 0 (devolución cubrió 100% en Confirmada), `ExtractoPagado` se emite como derivado por transición. OxpExtracto en estado Pagada tiene `saldoPorPagar()` = 0. | OxpComercio, OxpExtracto | — |
| I16 | **Origen del pago determina estado mínimo.** Pagos de origen interno — coordinados por domain services (`ServicioDeRegularizacion`, `ServicioDeAplicacionDevolucion`) — se aplican desde estado **Confirmada**: `PagoOxpComercioViaAnticipoAplicado`, `PagoOxpComercioViaDevolucionAplicado` (OxpComercio) y `PagoExtractoViaDevolucionAplicado` (OxpExtracto). Confirmada es el estado más temprano donde `valorNeto()` es estable — la FSM no permite correcciones posteriores. Pagos de origen externo — confirmados por el sistema contable — se aplican desde estado **Causada** (OxpComercio: `PagoAplicado` tipo pago_directo, pago_extracto; OxpExtracto: `CrucePagoExtractoAplicado` tipo pago_sincoa; Anticipo: `AnticipoVinculadoAPartida` tipo extracto, `PagoAnticipoAplicado` tipo pago_directo). Los pagos externos requieren causación porque dependen de la integración contable. **Excepción Anticipo nacido de devolución (Ramas B/C):** el `CrucePagoAplicado` tipo devolucion se aplica en el mismo append que `AnticipoRegistrado` + `AnticipoConfirmado` + `AnticipoCausado` — la confirmación y causación son automáticas (heredadas del flujo de devolución), por lo que el cruce queda registrado al alcanzar el estado Causada en ese mismo append. Ver `[PD3]` para evolución futura de esta invariante. | OxpComercio, OxpExtracto, Anticipo | — |
| I17 | **Consistencia de devolución (eventual):** Restricciones por tipo de OXP. **Comercio:** `valorNeto(Devolucion)` ≤ `valorNeto(OxpComercio)`. La suma de todas las devoluciones sobre una misma OxpComercio no puede superar el `valorNeto()` original. Cuando `saldoPorPagar(OXP) > 0` y `valorNeto(devolucion) ≤ saldoPorPagar`: crédito directo (Rama A). Cuando `saldoPorPagar(OXP) > 0` y `valorNeto(devolucion) > saldoPorPagar`: bifurcación — crédito por `saldoPorPagar` + Anticipo por excedente (Rama C). **Extracto:** `valorNeto(devolucion)` ≤ `saldoPorPagar(OxpExtracto)` cuando saldo > 0. **Anticipo:** solo reversa total (`valorNeto(devolucion)` = valorTotal del anticipo). Anticipo en estado Vigente o Confirmada (estados pre-causación) sin cruces de pago ni regularización. Mismo tercero obligatorio en todos los tipos. Enforcement: validación en `ServicioDeAplicacionDevolucion` (precondición con lectura de acumulado de devoluciones por OxpComercio) + proyección eventual `[SI4]` de suma de devoluciones por OxpComercio para detección tardía. | Devolucion, OxpComercio, OxpExtracto, Anticipo | — |
| I18 | **Unicidad de código en CatalogoGastoDirecto:** No pueden existir dos `ConceptoGastoDirecto` con el mismo código dentro del mismo catálogo (empresa). | CatalogoGastoDirecto | — |
| I19 | **Unicidad de Proveedor por clave natural** (eventual): no pueden existir dos Proveedores con la misma identificación legal (tipo + número + país, sin DV). Enforcement: proyección con constraint único `[SI7]`; ante colisión concurrente, `AsegurarProveedor` reintenta y reutiliza el existente. | Proveedor | — |
| I20 | **`AsegurarProveedor` es la única vía de creación del Proveedor.** No existe registro directo: toda creación pasa por la vía idempotente (crear o reutilizar) — la radicación nunca se bloquea por proveedor inexistente. | Proveedor | — |
| I21 | **El origen de la inactivación gobierna la reversa:** toda inactivación lleva motivo y origen (`local` \| `senal_global`); la de origen `senal_global` solo la levanta la señal de reactivación de la bodega — el veto global no se esquiva localmente. | Proveedor | — |
| I22 | **Ninguna radicación nueva con Proveedor Inactivo** (eventual): OxpComercio, Anticipo y Devolucion verifican al radicar que el Proveedor referenciado esté Activo. El historial y los documentos en curso no se afectan — la inactivación impide operaciones **nuevas**. | Proveedor + OxpComercio, Anticipo, Devolucion | — |
| I23 | **Control de doble pago con anticipos abiertos** — condicionada por configuración de empresa (como I6; modos: bloqueo / decisión obligatoria): **(a) al confirmar** una OxpComercio cuyo Proveedor tiene anticipos con `saldoPorRegularizar()` > 0 **no resueltos** para esta OXP, el sistema exige la decisión según el modo; **(b) al pagar:** la vinculación con extracto (iniciada por usuario) no procede mientras exista anticipo abierto sin resolver; el pago directo (hecho consumado, automático) se aplica siempre y emite `AlertaDoblePagoPotencial`. **"Resuelto"** = regularización aplicada a esta OXP **o** `ConstanciaAnticipoNoAplicable` registrada para ese anticipo en esta OXP. | OxpComercio + Anticipo (lectura) | `[R38]` `[D33]` |
| I24 | **Ninguna distribución nueva con unidad organizacional inexistente o inactiva** (eventual): toda `InstruccionDistribucion`/`DestinoDeNegocio` nueva referencia una unidad que existe y está **Activa** en la copia local de Estructura Organizacional (`[SI8]`, `[D34]`). El historial y los documentos en curso no se afectan cuando una unidad se suspende, inactiva o reestructura — la restricción impide imputaciones **nuevas**, no reescribe el pasado. Si la unidad resuelta aún no existe/activa, el componente cae en destino pendiente y la causación de esa parte se difiere (`I10`). Gemela conceptual de `I22` con el Proveedor. | OxpComercio, OxpExtracto, Anticipo, Devolucion | `[D34]` |
| I25 | **Coherencia de las reglas de distribución:** En `CatalogoReglasDistribucion`, la `distribucion` de toda `ReglaDeDistribucion` suma exactamente 100% entre sus `DestinoDeNegocio` (gemela de `I2` a nivel de regla), y no existen dos reglas **activas** con la misma combinación exacta de criterios (`criterioProveedor`, `criterioTipoGasto`, `criterioLugarEjecucion`) — así el desempate por especificidad/prioridad de `[D36]` siempre tiene una ganadora única. | CatalogoReglasDistribucion | `[D36]` |

---

## 8. Qué NO contiene este documento

| Excluido | Razón | Dónde vive |
|----------|-------|------------|
| Glosario de términos | Ya definido | `definicion-alcance.md`, Sección 2 |
| Actores y permisos | Ya definidos | `definicion-alcance.md`, Sección 3 |
| Reglas de negocio completas | Ya definidas (R01–R37) | `definicion-alcance.md`, Sección 6 |
| Modelo de datos / esquema de BD | Pertenece a implementación | Documentación técnica (fase 2) |
| Endpoints de API / contratos | Pertenece a implementación | Documentación técnica (fase 2) |
| Diseño de interfaz de usuario | Pertenece a UX | Especificaciones de UX |
| Configuración de EventCatalog | Herramienta de fase 2 | Se derivará de este documento |
| Justificación de decisiones de modelado | Documento separado | `guias-de-modelado/modelar-agregados.md` |

---

## 9. Decisiones de arquitectura y diseño

Registro de las decisiones tomadas durante la definición del modelo de dominio. Cada decisión incluye su justificación y el principio de diseño que la sustenta.

| # | Decisión | Justificación | Principio |
|---|---|---|---|
| D1 | **OXP es un bounded context**, no un agregado. | Contiene múltiples agregados coordinados (OxpComercio, OxpExtracto, Anticipo, Devolucion) con ciclos de vida independientes. | DDD: bounded context como límite lingüístico y de responsabilidad. |
| D2 | **Agregados raíz:** cuatro transaccionales (OxpComercio, OxpExtracto, Anticipo, Devolucion), el rol `Proveedor` (`[D30]`, #38) y dos de configuración: `CatalogoGastoDirecto` (`[D21]`) y `CatalogoReglasDistribucion` (`[D36]`, #51). | Los 4 transaccionales no comparten estados, eventos ni transiciones. Solo comparten Value Objects. Streams de eventos independientes. Cada agregado tiene su propio ciclo de vida y máquina de estados. Los agregados de configuración tienen ciclo de vida CRUD sin FSM. | DDD: el agregado define un límite de consistencia transaccional. Análisis detallado en `guias-de-modelado/modelar-agregados.md`. |
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
| D15 | **Anticipo con dos comportamientos, dos dimensiones de valor y estados intermedios independientes (Pagado, Regularizado) hacia estado terminal (Cerrado).** | Comportamiento 1: vinculado a extracto (TC, pago ya realizado, partida visible). Comportamiento 2: no vinculado a extracto (forma de pago diferente a TC, pago pendiente confirmado por el sistema contable). Ambos pueden tener o no soporte preliminar (ej: cuenta de cobro). Los dos tipos de cruce (extracto y pago directo) pueden coexistir sobre el mismo anticipo — los comportamientos no son mutuamente excluyentes. Valor anticipo (monto adelantado, se resuelve por regularización vía OxpComercio con soporte formal) y valor total (se resuelve por pago vía extracto o pago directo). Los valores pueden diferir. Cada dimensión se resuelve mediante cruces parciales rastreados por entidades internas (`CrucePagoAplicado`, `CruceRegularizacionAplicada`). Saldos derivados, no almacenados. Estado terminal (Cerrado) requiere ambos resueltos. | Cada dimensión de valor tiene su propio ciclo de resolución independiente. La regularización es un control transversal. Los estados Pagado y Regularizado son intermedios — el verdadero terminal es Cerrado. |
| D16 | **Cruces parciales como entidades internas del Anticipo.** | Cada cruce parcial (pago o regularización) es un registro individual (`CrucePagoAplicado` o `CruceRegularizacionAplicada`). El saldo es un valor derivado (valor preestablecido − suma de cruces). Dos componentes separados porque rastrean valores preestablecidos distintos (valorTotal vs valorAnticipo) y referencian entidades externas diferentes (PartidaExtracto / pago confirmado por el sistema contable vs OxpComercio). | POO: saldo derivado evita redundancia (D10). DDD: entidades internas con identidad para trazabilidad de cada cruce individual. Event sourcing: replay reconstruye los cruces, los saldos se derivan. |
| D17 | **`saldoPorPagar` como comportamiento calculado en OxpComercio y OxpExtracto.** | Sigue el patrón de saldos derivados del Anticipo (D10, D16). Valor derivado, no almacenado. Se reduce mediante pagos parciales rastreados por entidades internas (`PagoAplicado` en OxpComercio, `CrucePagoExtractoAplicado` en OxpExtracto). Evento de transición a estado terminal cuando saldo = 0. | POO: saldo derivado evita redundancia. Event sourcing: replay reconstruye entidades de pago, los saldos se derivan. Consistencia con el patrón del Anticipo. |
| D18 | **Compensada eliminada como estado de OxpComercio.** | La vinculación con extracto (conciliación) es un pago aplicado que reduce `saldoPorPagar()`, no un cambio de estado. Pagada es el único estado terminal financiero. Permite pagos mixtos (extracto + anticipo + pago directo) sobre la misma OxpComercio. Reemplaza el modelo anterior donde Compensada era un estado terminal. | DDD: el estado debe reflejar la realidad del negocio. La obligación no "se compensa" — recibe pagos parciales hasta liquidarse. |
| D19 | **Devolucion con valores positivos (magnitud del crédito).** La naturaleza contable (nota crédito vs factura) no se representa con el signo de los valores — la determina el tipo del agregado. La traducción contable interpreta Devolucion como nota crédito. | Consistente con D8 (OXP no conoce el dominio contable). Evita el problema de `valorNeto()` negativo que invalidó D12 en v2.1. Cada agregado tiene valores positivos; la semántica contable la resuelve la frontera. |
| D20 | **Control de concurrencia, idempotencia y trazabilidad delegados a la plataforma (Marten + Wolverine).** `expectedVersion` (control de concurrencia): garantizada por Marten a nivel del event store. `idempotencyKey` (deduplicación de mensajes): garantizada por Wolverine vía inbox/outbox pattern. `correlationId` (trazabilidad de procesos): propagado automáticamente por Wolverine en la cadena de mensajes. Este documento no especifica estos mecanismos por evento ni por comando — son garantías transversales de la plataforma de persistencia y mensajería. Si la plataforma cambia, revalidar que el nuevo stack provea estas tres garantías. **Nota sobre pagos externos:** Los pagos confirmados por el sistema contable (`PagoOxpComercioDirectoAplicado`, `PagoAnticipoAplicado`, `CrucePagoExtractoAplicado` tipo pago_sincoa) deben incluir un identificador de negocio del pago (ej: número de transacción del destino físico que originó el pago, como SincoA&F) como referencia de origen. Si bien la deduplicación técnica la resuelve `idempotencyKey` de Wolverine, la referencia de origen permite detección de duplicados a nivel de dominio y trazabilidad del pago externo. | Estos mecanismos son patrones de infraestructura (optimistic concurrency control, idempotent consumer, correlation identifier), no comportamiento de dominio. Especificarlos por evento duplicaría lo que la plataforma ya resuelve y contaminaría el modelo con concerns de infraestructura. | Event sourcing (Marten): concurrencia a nivel de stream. EDA (Wolverine): at-least-once delivery con deduplicación automática. |
| D21 | **Catálogo de gasto directo como agregado de configuración dentro de OXP.** OXP administra un catálogo propio de tipos de gasto para obligaciones que se originan directamente en OXP (sin módulo de gestión detrás). Cada tipo de gasto configura: código, descripción, clasificacionTributaria (ref. Impuestos), conceptoPago (ref. Impuestos), activo. Cuando la obligación viene de un módulo de gestión (Compras, Arrendamiento, etc.), los conceptos ya llegan con las referencias fiscales resueltas desde el catálogo del módulo origen — OXP no usa su catálogo propio. Modelo federado: cada dominio de gestión es dueño de su catálogo con atributos particulares + referencias fiscales de Impuestos. No hay catálogo centralizado transversal. Ver `integraciones/catalogo-conceptos-por-dominio.md`. | Autonomía por dominio. Gobierno fiscal centralizado en Impuestos (fuente de verdad), no en un catálogo intermedio. Evita el anti-patrón del maestro centralizado que se degrada al atraer responsabilidades ajenas. | DDD: cada bounded context protege su modelo. Shared Kernel solo para vocabulario compartido (IDs de clasificación tributaria). |
| D22 | **Contrato de integración OXP → Impuestos en dos operaciones.** (1) **Solicitud de cálculo (síncrona):** OXP envía contexto transaccional (conceptos con clasificacionTributaria y conceptoPago, entidades fiscales, ubicaciones, fecha, moneda, direccionFiscal = gasto) y recibe desglose fiscal propuesto. Se invoca al radicar y al recalcular (cambio de monto, tercero, clasificación). (2) **Confirmación (asíncrona):** al confirmar OxpComercio, OXP envía comando con transaccionId, efectoFiscal = gravamen, contexto completo + desglose confirmado. Impuestos crea el registro tributario inmutable. Para devoluciones tipo Comercio: efectoFiscal = desgravamen + transaccionOrigenId = OxpComercio original — Impuestos prorratea del gravamen, no invoca al motor. El contrato semántico mínimo del consumidor está definido en `[D9]` del modelo de Impuestos. | OXP necesita formalizar cómo interactúa con Impuestos: qué datos envía, en qué momento, y cómo se vinculan confirmaciones y desgravámenes. | DDD: integración entre bounded contexts mediante contratos explícitos. |
| D23 | **Canales de entrada agnósticos con clasificación inteligente `[R36]`.** Los canales de entrada (SincoRE, servicio de extracción de datos, carga manual) son agnósticos al origen — entregan datos extraídos sin clasificar. La clasificación (directa vs. sub-dominio de gestión) y la resolución de referencias fiscales (clasificacionTributaria, conceptoPago) son responsabilidad de OXP en la capa de aplicación `[R36]`. La clasificación no se implementa con tablas configurables estáticas ni flujos de enrutamiento rígidos — se espera que opere con mecanismos inteligentes y adaptativos (ej: coincidencia con documentos pendientes de sub-dominios de gestión, aprendizaje por repetición, asistencia por IA). El usuario siempre puede corregir la sugerencia. Cuando el soporte trae tributos del proveedor, se validan contra el cálculo de Impuestos `[R37]`. | OXP recibe datos ya extraídos por servicios de infraestructura transversal (SincoRE, servicio de extracción). Los canales son agnósticos al origen; OXP decide. | DDD: la clasificación es lógica de dominio de OXP (capa de aplicación). La extracción es infraestructura compartida. |
| D24 | **Clasificación de capacidades por fase de implementación.** Las capacidades del bounded context OXP se clasifican en dos fases: **`[F1]` — Comercio + Extracto:** Todos los agregados y domain services necesarios para gestionar el ciclo de vida completo de obligaciones individuales (OxpComercio), consolidadas (OxpExtracto), anticipos, devoluciones y su configuración de gasto directo. Incluye integración con Impuestos y clasificación inteligente de origen. **`[F2]` — Ampliación de tipos:** Nuevos agregados con ciclo de vida propio que extienden el BC sin redefinir el núcleo. Primer candidato: OxpCajaMenor (fondo fijo, rendición, reembolso). Las fases reflejan dependencia funcional, no cronograma. | El núcleo transaccional (F1) debe estar operativo antes de incorporar nuevos tipos de obligaciones (F2). Alineado con la Sección 8 de `definicion-alcance.md`. | DDD: priorizar el core domain antes de extender con nuevos agregados. |
| D25 | **El Anticipo se causa al confirmarse, replicando el patrón de OxpComercio.** Ciclo: Vigente → Confirmada → Causada → (Pagado / Regularizado) → Cerrado / Reversado. La causación reconoce el activo "anticipos a proveedores" contra una cuenta por pagar puente (Db Anticipos · Cr CxP por anticipos), análogo al patrón Down Payment de SAP (Special G/L Indicator) y Prepayment Invoice de Oracle Cloud Payables. Para anticipos nacidos de devolución (Ramas B/C), la causación es automática heredada del flujo de devolución y el asiento es Db Anticipos · Cr CxC proveedor (sin cuenta puente porque no involucra banco). La amortización (`AnticipoAmortizado`) es la confirmación de un efecto contable distinto, ver `[D26]`. | Antes de v3.0 el Anticipo no tenía entrega contable propia: el efecto se asumía embebido en la causación de la OxpComercio que lo regularizaba. Esto generaba dos problemas: (1) un hueco contable cuando el anticipo se pagaba pero aún no se regularizaba, resuelto manualmente en SincoA&F; (2) restricción operativa de SincoA&F que paga solo sobre asiento — sin causación del anticipo la integración se bloquea. | DDD: simetría con OxpComercio. NIIF: cuenta puente refleja la obligación real al confirmarse. Compatibilidad con `[PD3]` (Tesorería desacoplada futura). Alineación con ERPs maduros (SAP, Oracle, NetSuite, Dynamics). |
| D26 | **La amortización del anticipo se entrega al sistema contable embebida en la causación cuando el cruce ocurre antes o durante la causación de la OXP, y como asiento independiente cuando ocurre después.** **Caso A (cruce pre/durante causación):** la información de la amortización viaja dentro de la misma causación de la OXP de Comercio (`causacion_gasto`); un único registro que reconoce el gasto y reclasifica el saldo del anticipo (Db Gasto · Cr Anticipos a proveedores, sin acreditar CxP). **Caso B (cruce post-causación, permitido por R30/R4 y §1307):** la causación ya se emitió (Db Gasto · Cr CxP proveedor) y la amortización se entrega como **causación independiente** con `tipoTransaccion = amortizacion_anticipo` (Db CxP proveedor · Cr Anticipos a proveedores), emitida por `PagoOxpComercioViaAnticipoAplicado` cuando la OXP ya está en estado Causada. El Anticipo recibe la confirmación vía `AnticipoAmortizado` para cerrar su ciclo en ambos casos. | La amortización (Db CxP proveedor · Cr Anticipos) y la causación del gasto (Db Gasto · Cr CxP proveedor) son el mismo hecho económico **solo cuando coinciden en el tiempo**: si el cruce ocurre antes/durante la causación, se consolidan en un registro (más simple, sin descoordinación). Pero R30/R4 y §1307 permiten regularizar contra una OXP ya Causada — ahí la causación ya salió, y forzar "un único registro" dejaría la amortización **sin reflejo contable** (la CxP y el activo Anticipos quedan vivos pese a que el dominio ya redujo el saldo). El asiento independiente cierra ese hueco. Es exactamente el patrón SAP F-54 (Down Payment Clearing): una transacción separada y posterior a la factura. | DDD: en el Caso A el evento `OxpComercioCausada` es el punto de notificación (la amortización viaja como tipo de componente). En el Caso B el punto de notificación es `PagoOxpComercioViaAnticipoAplicado` sobre una OXP ya Causada, que emite su propia causación de amortización. La guarda contra doble pago es intrínseca: la regularización solo reduce `saldoPorPagar` si hay saldo (control reforzado en el issue #26). El Anticipo recibe el eco vía `AnticipoAmortizado` para cerrar su propio ciclo. |
| D27 | **OXP etiqueta sus eventos de causación con un tipo de transacción contable (`tipoTransaccion`) como metadato de integración con el sub-dominio Contabilidad.** Cinco eventos causales de OXP (`OxpComercioCausada`, `ExtractoCausado`, `AnticipoCausado`, `DevolucionCausada` en sus tres variantes) emiten líneas de traducción acompañadas de una etiqueta que permite al sistema contable seleccionar la plantilla de asiento aplicable. La etiqueta es semántica del hecho económico — no es cuenta, naturaleza ni centro de costo, por lo que no contradice `[D8]`. El mapeo canónico (evento → tipoTransaccion → plantilla) se documenta en la sección "Integración con sub-dominio Contabilidad" del Bounded Context. La amortización y la diferencia en cambio viajan como tipos de componente dentro de las líneas, no como `tipoTransaccion` separados. La devolución tipo Anticipo requiere una plantilla nueva (`reversa_anticipo`) en el inventario del sub-dominio Contabilidad — punto de coordinación cruzada. | El sub-dominio Contabilidad requiere que cada línea de traducción incluya `tipoTransaccion` para seleccionar plantilla (contrato del anexo de plantillas de Contabilidad). Sin documentar el mapeo en OXP, la integración queda implícita y abre zona gris en implementación. La separación `causacion_gasto` vs `reversa_anticipo` vs `nota_credito_gasto` preserva la claridad semántica para el contador en la consola de contabilización y permite reglas de derivación independientes en el motor de Contabilidad. | DDD: contrato explícito entre bounded contexts. La etiqueta es vocabulario compartido (shared kernel mínimo) sin acoplar el modelo de OXP al modelo contable. Funcionalmente análoga al "tipo de componente" que OXP ya emite por línea, pero a nivel del hecho económico completo. |
| D28 | **OXP no cambia el estado de sus documentos causados cuando el sistema contable rechaza una entrega.** Los rechazos del sistema contable se resuelven dentro del ciclo del sub-dominio Contabilidad: los rechazos post-borrador (destino físico) los reabre y corrige el contador en la consola de contabilización; los rechazos pre-borrador (defectos de catálogo o de contrato) los atiende el equipo de producto o el consumidor. En ambos casos el documento OXP permanece en estado Causada — no hay nuevos eventos `*RechazadaPorContabilidad`, ni transiciones FSM hacia atrás, ni invariantes de "trazabilidad de rechazos" duplicando lo que ya vive en el sub-dominio Contabilidad. OXP es responsable de conservar el hecho económico hasta confirmar procesamiento exitoso vía `EntregaAceptada` mediante outbox pattern (ver `[SI6]`). | El contrato del sub-dominio Contabilidad establece explícitamente que el consumidor no es responsable de reaccionar ante `EntregaRechazada` — el flujo de corrección vive dentro del sub-dominio Contabilidad. Modelar el rechazo en OXP duplicaría responsabilidad. La durabilidad del hecho económico ante rechazos pre-borrador (NACK del bus) sí es responsabilidad del consumidor, materializada por outbox pattern como infraestructura, no como comportamiento de dominio. | DDD: cada bounded context resuelve sus problemas dentro de su propio ciclo. OXP delega la consistencia eventual al outbox + idempotencia del motor de Contabilidad, sin requerir modelado defensivo. |
| D29 | **El cruce de la partida del extracto contra cada compra viaja en las líneas de traducción como `cruce_obligacion`.** Al causar el extracto, cada `Vinculacion` (partida ↔ OxpComercio) emite una línea `cruce_obligacion` que salda la cuenta por pagar del proveedor de esa compra. Lectura contable: el extracto **reclasifica** la deuda del proveedor hacia el banco/emisor (medio de pago crédito/prepago) — Db CxP proveedor (el cruce) · Cr CxP banco/emisor (contrapartida que genera el motor de Contabilidad); luego el pago del extracto al banco salda esa CxP (flujo ya existente). La línea lleva el tercero del proveedor, el valor de la obligación saldada (a TRM de radicación; la diferencia de cambio del momento viaja aparte como `diferencia_en_cambio`) y la distribución de origen de la compra; la **unidad organizacional del cruce la rinde Contabilidad según `[I33 de Contabilidad]`** (distribuida/general/sin unidad), espejando cómo se registró la CxP de la causación original para garantizar el neteo. | Antes del issue #18, `lineasParaTraduccion()` del extracto solo emitía `cargo_financiero`, `diferencia_en_cambio` y `ajuste_tolerancia`: la cuenta por pagar acreditada al causar cada OxpComercio nunca se debitaba al pagar vía extracto (`PagoOxpComercioViaExtractoAplicado` solo reduce `saldoPorPagar()`, sin asiento). El cruce restaura esa pata de la partida doble. La lectura de reclasificación es coherente con el ciclo de dos pasos que el extracto ya tiene (Causada → Pagada) y con la naturaleza de una tarjeta/cupo (la deuda pasa al emisor hasta pagar el extracto). | DDD: contrato explícito entre bounded contexts; OXP emite el hecho (tercero, valor, distribución de origen) y Contabilidad aplica su política `[I33]`. Coordinación cruzada: nuevo rol `CRUCE_OBLIGACION` en la plantilla `causacion_gasto` de Contabilidad. |
| D30 | **Agregado `Proveedor` — el rol del tercero de OXP** (replanteamiento #31, issue #38). OXP captura y gobierna su registro del proveedor con las validaciones empaquetadas del producto, lo informa a la bodega de Terceros con el evento estándar de rol (estado completo + `secuencia`, entrega por outbox `[SI6]`) y aplica automáticamente las decisiones que la bodega publica (señal global → `ProveedorInactivado/Reactivado` con origen `senal_global`; correcciones → `CorreccionDeIdentidadAplicada`). `AsegurarProveedor` idempotente es la única vía de creación (I20) — reutiliza sin sobrescribir; la radicación nunca se bloquea. Sin `empresa` como atributo (convención del BC: contexto resuelto en la lógica de los eventos; el campo del contrato sale de ese contexto). Estado `Activo \| Inactivo` — mapeo natural al `estadoEnOrigen` del contrato, sin traducción. | La bodega nunca es prerrequisito (alcance de Terceros v2.0): la transversalidad se resuelve con distribución — validación local empaquetada + información por eventos — en lugar de consulta. El veto global no se esquiva localmente (I21). | DDD: el agregado de rol vive en el dominio consumidor (decisión D1 de Terceros v1.0, ratificada por el modelo de bodega); EDA: injerencia por mensajes, nunca escritura remota. |
| D31 | **Las transacciones embeben `InformacionTercero` copiado del `Proveedor` y llevan la referencia `proveedorId`.** El contrato con Contabilidad queda intacto (el hecho económico viaja completo e inmutable, incluido `terceroPrincipal` `[D27]`/v3.7); lo que cambia es la **fuente**: el dato ya no se digita suelto en cada radicación — se copia del registro. "Mismo tercero" (regularización, devoluciones, anticipos) pasa de comparación de textos a **referencia exacta**. Las correcciones de la bodega aterrizan en el registro y las radicaciones futuras copian el dato corregido; las transacciones históricas no se reescriben. El emisor/banco del extracto **no** es un Proveedor (entidad financiera — su rol pertenece a Tesorería a futuro): su `InformacionTercero` se captura en el extracto como hasta ahora. | Sin registro, cada radicación re-captura el NIT y el error renace; sin referencia, "mismo tercero" depende de igualdad de textos digitados. | Una sola fuente del dato dentro del BC + trazabilidad transacción → registro → ficha consolidada de la bodega. |
| D32 | **El candidato `InformacionTercero` del catálogo de Nuggets se resuelve como composición local — no será Nugget.** Es (identificación legal + razón social): la identificación ya es pieza del paquete con todas las reglas; la razón social es texto sin validación propia. Empaquetar la pareja no aportaría reglas ni datos — solo un envoltorio (fallaría el filtro 5/6 de la gobernanza frente a `IdentificacionLegal`). Cada consumidor la compone localmente: OXP la copia de su `Proveedor`; el contrato con Contabilidad ya la trata así desde antes del replanteamiento (fue el precedente del patrón). | Registrado en el catálogo de Nuggets (memoria de propuestas, para no re-evaluar). | Cierra el pendiente que el catálogo difería "al intervenir los modelos de OXP y Terceros". |
| D33 | **El control de doble pago (`[R38]`) se materializa con constancia humana y comportamiento por canal** (issue #30, descubierto al implementar y validar el #26 con tests E2E). (1) **La pertenencia anticipo↔OXP nunca es calculable por el sistema**: el registro del anticipo no referencia factura, orden ni compra esperada — solo Proveedor, valor, medio de pago y fecha; la regularización es el único vínculo y su único filtro de sistema es el mismo Proveedor (el emparejamiento lo decide el humano, `[R29]` 1:N). Por eso la **constancia** ("este anticipo no corresponde a esta OXP") es el único mecanismo válido cuando R38 dispara, y forzar "aplicar" sería peligroso (aplicar el anticipo equivocado es trivial y el sistema no puede impedirlo por datos). (2) **La constancia se puede registrar en cualquier estado antes de saldarse** (Pendiente, Confirmada, Causada con saldo) — sin esto, el anticipo que aparece después de causar dejaba a la OXP bloqueada de forma permanente al pagar, sin salida. (3) **El refuerzo al pagar es por canal**: la conciliación vía extracto la inicia un usuario — hay humano para resolver en el acto y el cruce se bloquea hasta resolver; el pago directo es automático y registra un pago que **ya ocurrió** en el sistema contable (`[R35]`, `PagoAplicado` tipo pago_directo) — rechazarlo dejaría a OXP negando la realidad, así que el canal **detecta y alerta** (`AlertaDoblePagoPotencial`), y su prevención real vive en la verificación temprana de la confirmación. | El #26 entregó la regla en el alcance pero el modelo nunca recibió el diseño (0 eventos / 0 invariantes); la implementación lo construyó sin respaldo y los tests E2E destaparon el escenario sin salida y la asimetría de canales. | DDD: el juicio humano se modela como hecho explícito e inmutable (la constancia), no como bandera; EDA: los hechos consumados no se rechazan — se registran y se compensa operativamente. |
| D34 | **La unidad organizacional es un dato gobernado por Estructura Organizacional (dueño único); OXP la consume como copia local, no como agregado** (replanteamiento #45, issue #48). OXP no crea, modifica ni gobierna unidades — solo las usa para distribuir e imputar. Mantiene una **copia local** (read model) por suscripción a los eventos de ciclo de vida de EO (`[SI8]`), opera y valida siempre contra ella (`I24`) y **nunca consulta a EO en caliente**. La copia es para **validación del dominio, no una API de lectura para la UI** (la interfaz lee unidades directamente de EO, fuente de verdad). Como la unidad se elige de la fuente de verdad, una unidad resuelta por la cadena (`I10`) existe en EO; si su evento aún no llegó a la copia (desfase de propagación) o está inactiva, OXP **difiere** la causación de esa parte vía *destino pendiente* hasta que llegue `UnidadCreada`/`UnidadActivada` — sin aproximar con unidad provisional ni de tránsito (desconciliaría con contabilidad). Reacciona a las reestructuraciones de EO (fusión/división/traslado) reasignando referencias futuras y conservando el histórico. **OXP no emite eventos salientes hacia EO** (la señal de demanda `DemandaDeUnidadSenalada` se retiró en el #72: con la unidad elegida de la fuente de verdad, referenciar una inexistente no ocurre en operación). **Contraste con el `Proveedor` (`[D30]`):** el Proveedor es un rol que OXP co-gobierna (agregado, `AsegurarProveedor`); la unidad tiene dueño único externo → solo copia local. **Nota:** la *asignación automática* de la unidad (qué unidad le corresponde a cada gasto) se resuelve con la cadena de niveles de `[D36]` (instrucción → herencia → reglas configurables → sugerencia por aprendizaje → pendiente). | Sin copia local, OXP dependería de la disponibilidad de EO en cada distribución (acoplamiento de ejecución que el replanteamiento #45 elimina) o re-gobernaría un dato ajeno (el consumidor volviéndose dueño). El diferir preserva la consistencia exacta operación↔contabilidad sin bloquear la radicación. | DDD: un dato con dueño único externo se consume por réplica local (read model), no se modela como agregado. EDA: copia local por eventos, nunca consulta ni escritura remota. Fundamento en `guias-de-modelado/datos-entre-dominios.md` y `[D15]` de Estructura Organizacional. |
| D35 | **Resolución del emisor/banco del extracto cuando el archivo no lo trae — inferencia por número de tarjeta desde el histórico, sin agregado de tarjetas** (issue #57). **Regla provisional mientras se define el dueño del dato "tarjeta"** (ver al final de esta celda). Algunos extractos no traen identificada la entidad financiera emisora (razón social, NIT), pero **siempre traen el número de tarjeta**. El emisor se resuelve en cadena: **(1)** del archivo si viene; **(2)** si no, se **infiere del `OxpExtracto` más reciente con el mismo número de tarjeta** y se reutiliza su `InformacionTercero` — **sugerencia revisable**, nunca silenciosa; **(3)** si no hay histórico, el usuario lo **captura/selecciona** en la radicación (queda disponible para los siguientes). La llave de coincidencia es el **número de tarjeta**, no el VO `MedioDePago` completo (que incluye la entidad bancaria — justo el dato ausente). Se **rastrea el origen** del dato del emisor (`del_archivo \| inferido_historico \| capturado_usuario`) para que un error de la primera captura no se propague en silencio — espeja el `origen` de `Vinculacion` (automática/manual) y de `Proveedor` (local/senal_global). El emisor sigue siendo un **Tercero** (rol entidad financiera), no una entidad de OXP: **no se crea agregado, catálogo ni registro de tarjetas** (anti-patrón que corrigieron `#31`/`#45` — un consumidor apropiándose de un dato transversal). La inferencia es del mismo tipo que la asignación automática `[D23]`; se materializa con la proyección `[SI9]`. **Dueño futuro del dato "tarjeta" (provisionalidad):** en el planteamiento inicial puede pertenecer a **Tesorería** (`[D31]` lo anticipa), pero está **por analizar si debe vivir en un contexto global** (servicio compartido transversal a varios dominios). En cualquiera de los dos casos, cuando ese dueño exista esta inferencia se reemplaza por **copia local** del registro de tarjetas por eventos (mismo patrón `[D34]`/`[SI8]` que las unidades); mientras tanto, el histórico de extractos hace de fuente sin crear un dueño provisional. | Sin la cadena, un extracto sin emisor no se puede causar (la contrapartida CxP del banco/emisor `[D29]` / `terceroPrincipal` queda sin tercero) o se re-digita el NIT cada período y el error renace; crear un agregado de tarjetas haría al consumidor dueño de un dato ajeno. | DDD: un dato con dueño (futuro) externo no se modela como agregado del consumidor; se resuelve por regla de asignación sobre datos que ya existen. ES: el histórico de `OxpExtracto` ya contiene el par (número, emisor) — la verdad se deriva del stream, no de una entidad nueva. Fundamento en `guias-de-modelado/datos-entre-dominios.md`. |
| D36 | **Asignación de la unidad organizacional por una cadena de resolución de niveles** (issue #51). La distribución por unidad deja de resolverse solo con una preferencia global de empresa (gruesa) y pasa a una cadena con dos niveles nuevos: **Nivel A — reglas de preferencia de distribución** (configurables en `CatalogoReglasDistribucion`, **determinísticas**): criterios combinables `proveedor`/`tipo de gasto`/`lugarEjecucion`, resueltas por **especificidad** (más criterios gana) con desempate por prioridad (proveedor > tipo > lugarEjecucion); la regla sin criterios es la preferencia general (preserva el comportamiento anterior). Resuelven **automáticamente** sin confirmación — el usuario las configuró — y **cubren el arranque en frío** (un tenant nuevo configura reglas desde el día 1, sin esperar histórico). **Nivel B — sugerencia por aprendizaje** (`[SI10]`, **no vinculante**): cuando ninguna regla casa, se pre-llena con la unidad más frecuente del histórico para esa combinación; el usuario confirma → pasa a instrucción explícita; promovible a regla e invalidable. La cadena completa: instrucción explícita → herencia del gasto padre → Nivel A → Nivel B (sugerencia) → destino pendiente. Cada distribución registra **con qué nivel** se resolvió (trazabilidad). Todo opera sobre la **copia local de unidades activas** (`[SI8]`, `[D34]`): nunca se propone una unidad inexistente/inactiva; si la resuelta no existe aún, se difiere. **Límite (lo determinístico vs lo inferido):** lo vinculante/automático es solo lo determinístico (explícita, herencia, reglas configuradas); lo inferido por aprendizaje **siempre** pasa por confirmación humana — una inferencia no pisa una regla que el negocio configuró. | El aprendizaje por sí solo no resuelve el arranque en frío (sin histórico no sugiere) y es inferencia (riesgoso para un dato que debe coincidir exacto con la contabilidad); las reglas determinísticas dan certeza y resolución desde el día 1. La estructura de dos niveles (determinístico + aprendizaje) sigue el mismo patrón de la cadena de resolución de cuentas del sub-dominio Contabilidad. | DDD: la asignación es lógica de OXP (capa de aplicación para la sugerencia, agregado de configuración para las reglas). Extiende `[D23]` (clasificación inteligente) a la dimensión unidad. |

---

## 10. Premisas de negocio

Premisas que provienen del negocio, la regulación o la fiscalidad y que condicionan el diseño del modelo. No son decisiones arquitectónicas (D##) ni invariantes estructurales (I##) — son verdades externas al modelo que se toman como base.

| # | Premisa | Justificación | Aplica a |
|---|---|---|---|
| P1 | **El anticipo registra únicamente el valor global; no aplica desglose fiscal.** | Fiscalmente, el IVA solo puede tomarse como descontable cuando existe factura o documento válido. El anticipo es un pago adelantado sin factura — el soporte formal (factura) llega durante la regularización vía OxpComercio. Por esta razón, el Anticipo no contiene `ConceptoDeGasto`, `DesgloseFiscal` ni `Tributo`. | Anticipo |
| P2 | **Moneda operativa del extracto.** Un OxpExtracto opera en una sola moneda. Si todas las partidas están en la misma moneda, el extracto opera en esa moneda (con `ValorMonetario` incluyendo TRM y equivalente en moneda funcional si es moneda extranjera). Cuando el extracto contiene partidas en monedas mixtas, las partidas en moneda extranjera se convierten a moneda funcional usando la TRM del extracto, y el extracto opera en moneda funcional. En ambos casos, cada partida conserva su valor original, moneda de origen y TRM para trazabilidad. La diferencia de cambio entre la TRM de radicación del extracto y la TRM al momento del desembolso es responsabilidad del dominio de Tesorería — OXP no ejecuta pagos, solo registra y monitorea su estado `[R18]`. Validación: estándar universal confirmado por SAP, NetSuite, Dynamics 365, Odoo, Peppol BIS 3.0 y NF-e Brasil (ver `fuentes/investigacion-moneda-unica-por-factura.md`). | OxpExtracto |
| P3 | **En dirección de gasto, las retenciones se practican al reconocer la obligación, no al pagar.** El DesgloseFiscal incluye impuestos y retenciones desde la radicación. El valorNeto() ya es neto de retenciones. El pago es puramente financiero — reduce saldoPorPagar() sin componente fiscal adicional. Validación: estándar en Colombia y consistente con SAP (Extended Withholding Tax "at invoice"), Oracle Fusion y Dynamics 365 para la dirección de gasto. | OxpComercio, Devolucion (tipo Comercio) |

---

## 11. Pendientes por definir

Aspectos del modelo que requieren definición futura. Los pendientes específicos de un componente se documentan junto a ese componente (ej: ⚠️ Pendientes en domain services). Esta sección consolida los pendientes de alcance general.

| # | Pendiente | Contexto | Condición de activación |
|---|-----------|----------|------------------------|
| PD1 | **Reembolso de anticipo — integración con CXC.** Cuando un anticipo generado por devolución (Rama B o C del `ServicioDeAplicacionDevolucion`) no tiene OxpComercio futura para regularizar, el reembolso al proveedor requiere integración con el dominio de Cuentas por Cobrar (CXC). | Anticipo A2 — proveedor devuelve dinero. Pendiente desde v2.2. | Cuando se implemente el bounded context CXC. |
| PD2 | **Cruce tipo `reversa` (negocio) para OxpComercio y OxpExtracto.** Actualmente `PagoAplicado` (OxpComercio) y `CrucePagoExtractoAplicado` (OxpExtracto) solo tienen tipo `revertido` (saga). El escenario de negocio donde se reverse un pago por razones de dominio (ej: devolución bancaria, recall de pago) no está definido. Solo el Anticipo tiene `reversa` de negocio hoy. | Los agregados OxpComercio y OxpExtracto no tienen un mecanismo de dominio para reversar pagos — solo la reversión técnica por fallo de saga. | Cuando el negocio identifique un escenario real de reversión de pago por razón de dominio. |
| PD3 | **Redefinición de I16 con sistema de Tesorería independiente.** Actualmente I16 distingue pagos internos (desde Confirmada, vía domain services) de pagos externos (desde Causada, confirmados por el sistema contable). Esto aplica a los tres agregados con dimensión de pago: OxpComercio, OxpExtracto y Anticipo (este último desde v3.0). Con un futuro sistema de Tesorería desacoplado de la causación contable, los pagos externos podrían aplicarse desde Confirmada, unificando el estado mínimo para todos los tipos de pago en los tres agregados. I16 requeriría redefinición. | I16 actual es correcta para el sistema actual donde el módulo de pagos del destino contable (ej: SincoA&F como destino legacy) paga sobre asiento. | Cuando se implemente sistema de Tesorería independiente. |
| PD4 | **Prorrateo de desgravamen para devoluciones parciales.** D22 establece que el desgravamen se prorratea del gravamen original. Para devoluciones parciales (subconjunto de conceptos), el mecanismo de prorrateo exacto es responsabilidad del sub-dominio de Impuestos — OXP envía los ConceptoDevuelto con sus valores y la referencia a la OxpComercio original (transaccionOrigenId). | Devolución tipo Comercio parcial. | Cuando se especifique el diseño detallado de desgravamen en el modelo de Impuestos. |

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
| 2.2 | Febrero 2026 | **Devolucion como agregado independiente** (D12 resuelta). Devolución extraída de OxpComercio como cuarto agregado raíz (D2 actualizada). Espejo parcial o completo de la OxpComercio que reversa, con valores positivos representando magnitud del crédito (D19 nueva). `TipoOxpComercio` eliminado de OxpComercio. `DevolucionAsociada` eliminado — funcionalidad trasladada a `DevolucionRadicada`. Nuevo `ServicioDeAplicacionDevolucion` (domain service): coordina Devolucion, OxpComercio y opcionalmente Anticipo. Dos ramas: si `saldoPorPagar(OXP) > 0` reduce saldo vía `PagoOxpComercioViaDevolucionAplicado`; si `saldoPorPagar(OXP) = 0` crea Anticipo (estado Pagado, pendiente regularización). Crédito aplicado en la confirmación (no en causación). `PagoAplicado` ahora tiene 4 tipos: extracto, anticipo, pago_directo, devolucion. Estados Devolucion: Pendiente → Confirmada → Causada ■. 3 eventos nuevos: `DevolucionRadicada`, `DevolucionConfirmada`, `DevolucionCausada`. 1 evento nuevo en OxpComercio: `PagoOxpComercioViaDevolucionAplicado`. I13/I15 ⚠️ de devolución resueltos. Nueva invariante I17 (consistencia de devolución). 40 eventos (OxpComercio: 12, OxpExtracto: 17, Anticipo: 8, Devolucion: 3). 17 invariantes. **Correcciones post-inspección:** `OxpComercioPagada` y `AnticipoPagado` actualizados con devolución como fuente de pago. `CrucePagoAplicado` del Anticipo: nuevo tipo `devolucion` para anticipo nacido de devolución. `DevolucionRadicada`: precondición reforzada con acumulado I17. `DevolucionConfirmada` y `ServicioDeAplicacionDevolucion`: precondición explícita de OxpComercio en Causada o Pagada. Template de evento, I2, I4, I8, I9, I12 actualizados con Devolucion. `OxpComercioDevuelta`: eliminada referencia obsoleta a "tipo = Compra". D1: 4 agregados. Value Objects compartidos: `MedioDePago` aplica a 3 de 4 agregados. Referencia a `guias-de-modelado/modelar-agregados.md`: "múltiples agregados". **⚠️ Pendientes:** (1) Escenario donde devolución > saldoPorPagar en OXP parcialmente pagada (nota débito no definida). (2) Reembolso del excedente de devolución (posible integración CXC). |
| 2.3 | Febrero 2026 | **Devolucion extendida a OxpExtracto y Anticipo.** Devolucion ya no referencia únicamente OxpComercio — ahora referencia uno de tres tipos de OXP (Comercio, Extracto, Anticipo) vía referencia a OXP origen (tipo + ID). Nueva entidad interna `ConceptoDeDevolucion` con atributos condicionales por tipo: Comercio (código, cantidad, DesgloseFiscal), Extracto (tipoComponente, referenciaComponente), Anticipo (motivoReversa). `ServicioDeAplicacionDevolucion` extendido con 3 ramas: Comercio (sin cambios), Extracto (reduce saldoPorPagar vía `PagoExtractoViaDevolucionAplicado`), Anticipo (reversa total vía `AnticipoReversado`). **OxpExtracto:** 2 eventos nuevos (`PartidaCubiertaPorDevolucion`, `PagoExtractoViaDevolucionAplicado`). Nueva entidad `CoberturaDevolucion`. `CrucePagoExtractoAplicado` con tipos `pago_sincoa` y `devolucion`. Estado de partida `devolucion`. `ServicioDeConciliacion` extendido con flujo de partidas de retorno. **Anticipo:** Nuevo estado terminal Reversado (desde Vigente, sin cruces previos). Nuevo evento `AnticipoReversado`. Cruces tipo `reversa` en `CrucePagoAplicado` y `CruceRegularizacionAplicada` — llevan ambos saldos a 0 (patrón consistente: estado terminal = saldos resueltos). Diagrama BC actualizado: Devolucion referencia OxpComercio (espejo de), OxpExtracto (ajuste sobre) y Anticipo (reversa). I3, I4, I8, I12, I17 actualizadas. 43 eventos (OxpComercio: 12, OxpExtracto: 19, Anticipo: 9, Devolucion: 3). 17 invariantes. **⚠️ Pendientes:** (1) Momento de regularización de anticipos. (2) Anticipo A2 — proveedor devuelve dinero (requiere dominio CXC). (3) `lineasParaTraduccion()` para Devolucion tipo Anticipo — hereda distribución del anticipo, capturada en radicación. |
| 2.4 | Febrero 2026 | **Refinamiento del modelo de dominio — 8 hallazgos aplicados (H1–H8).** **H1 — Entidades polimórficas en Devolucion:** `ConceptoDeDevolucion` (entidad única con atributos condicionales) reemplazado por tres entidades polimórficas con contrato común (`descripcion`, `valor: ValorMonetario`): `ConceptoDevuelto` (Comercio — código, cantidad, DesgloseFiscal), `CargoFinancieroDevuelto` (Extracto — referenciaCargoFinanciero) y `ReversaTotal` (Anticipo — motivoReversa). Tabla de comportamiento calculado reestructurada con columnas por tipo (Comercio, Extracto, Anticipo). `lineasParaTraduccion()` con descripción específica por tipo. 3 diagramas de composición independientes (1 reescrito, 2 nuevos). Sección 6 reescrita con 3 subsecciones: `ConceptoDevuelto`, `CargoFinancieroDevuelto`, `ReversaTotal`. Escenario E1b eliminado — partida de retorno siempre es Devolucion tipo Comercio (E1). E2 redefinido: cargo financiero devuelto contra extracto anterior. `DevolucionRadicada` actualizado con entidades específicas por tipo. D12 reescrita: tres entidades polimórficas con contrato común. I2, I10 actualizadas. **H2 — Strategy en ServicioDeAplicacionDevolucion:** `[SI2]` — sugerencia de implementación con tabla beneficio/costo para las 3 ramas del servicio. **H3 — InstruccionDistribucion como lista paralela:** D6 expandida con análisis beneficio/costo: la separación habilita cadena de resolución pero requiere sincronización encapsulada por el agregado. **H4 — FSM de PartidaExtracto:** nueva sub-sección 4.2.1 con máquina de estados interna (6 estados, 6 transiciones, diagrama ASCII). **H5 — OxpComercioExteriorRadicada eliminado:** consolidado como efecto condicional de `OxpComercioRadicada` (compra del exterior o sujeto no obligado a facturar → alerta DIAN para Documento Soporte en Adquisiciones). **H6 — Causalidad entre eventos:** nueva convención en Sección 2 con tres tipos: evento derivado por transición (mismo agregado, mismo append), derivado por configuración (mismo agregado, condicional), efecto inter-agregado (domain service, consistencia eventual). Tabla con ejemplos. **H7 — Exclusión de ajustes en valorTotalExtracto():** justificación añadida — `AjustePorDiferenciaCambio` y `AjustePorTolerancia` son cálculos internos de conciliación, no montos cobrados por el banco. **H8 — Entidades espejo:** patrón formalizado con tabla (`Vinculacion`↔`PagoAplicado`, `CoberturaAnticipo`↔`CrucePagoAplicado`, `CoberturaDevolucion`↔ref en Devolucion) y convención: cada agregado es dueño de su entidad espejo, ninguno consulta la del otro. **Nuevas convenciones:** `[SI##]` (sugerencias de implementación — recomendaciones técnicas que complementan definiciones del dominio), causalidad entre eventos, entidades espejo. `[SI1]` — sealed interfaces para entidades internas con discriminador de tipo (`PagoAplicado`, `CrucePagoAplicado`, `CrucePagoExtractoAplicado`, `CruceRegularizacionAplicada`). 42 eventos (OxpComercio: 11, OxpExtracto: 19, Anticipo: 9, Devolucion: 3). 17 invariantes. **⚠️ Pendientes:** (1) Momento de regularización de anticipos. (2) Anticipo A2 — proveedor devuelve dinero (requiere dominio CXC). |
| 2.5 | Febrero 2026 | **Fase 1 de auditoría — 18 hallazgos de severidad Alta resueltos (9 bloques).** **D20 — Control de concurrencia, idempotencia y trazabilidad delegados a la plataforma (Marten + Wolverine):** `expectedVersion` (control de concurrencia), `idempotencyKey` (deduplicación de mensajes), `correlationId` (trazabilidad de procesos) — garantías transversales de infraestructura, no especificadas por evento ni por comando. **Protocolos de proceso** en los 3 domain services: `correlationId`, compensación por paso con evento compensatorio, persistencia en stream propio. **ServicioDeRegularizacion completamente especificado:** trigger, comando, flujo principal, precondiciones, escenario 1:N, tabla de compensación bilateral, protocolo de proceso, momento de la regularización (Confirmada o posterior). **I15 actualizada:** OxpExtracto en Confirmada con `saldoPorPagar()` ≥ 0 (reducible por devolución); si devolución cubre 100% en Confirmada, `ExtractoPagado` se emite como derivado por transición al causarse. **I16 reescrita:** principio de origen del pago — pagos internos (domain services) desde Confirmada, pagos externos (SincoA&F) desde Causada/Causado. Nota sobre futuro sistema de Tesorería. **Excedente devolución resuelto (I17):** bifurcación Rama Comercio-C — crédito por saldoPorPagar + Anticipo por excedente. **6 eventos compensatorios nuevos:** `PagoOxpComercioViaAnticipoRevertido`, `PagoOxpComercioViaDevolucionRevertido` (OxpComercio), `PagoExtractoViaDevolucionRevertido` (OxpExtracto), `RegularizacionRevertida` (Anticipo), `DevolucionRevertida` (Devolucion). **Composición:** `lineasParaTraduccion()` en Anticipo, `SoporteDocumental` en OxpExtracto. **Consistencia:** estado Confirmado → Confirmada en OxpExtracto (género unificado con los demás agregados). Convención single emitter: domain services operan vía comandos a agregados, no escriben directamente en streams ajenos. definicion-alcance.md actualizado: Compensada marcada como eliminada (D18), devolución con valor positivo (D19). 48 eventos (OxpComercio: 13, OxpExtracto: 21, Anticipo: 10, Devolucion: 4). 17 invariantes. Decisión D20 nueva. **⚠️ Pendientes:** (1) Anticipo A2 — proveedor devuelve dinero (requiere dominio CXC). |
| 2.6 | Marzo 2026 | **Fase 2-3 de auditoría — 44 hallazgos de severidad Media/Baja resueltos + consolidación estructural.** **Convenciones nuevas (Sección 2):** género de estados (femenino para obligaciones, masculino para anticipo); nombres de agregados con justificación PascalCase y referencia al glosario canónico; alcance del glosario canónico (artefactos del modelo no requieren entrada); subsección *tipos de cruce: `reversa` vs `revertido`* con tabla semántica y mapeo de eventos; precisiones terminológicas (Conciliación proceso vs Conciliada/Parcialmente Conciliada estados); evento compensatorio como cuarto tipo de causalidad. **Corrección de género sistémica en OxpExtracto:** Conciliado→Conciliada, Causado→Causada, Pagado→Pagada (~30 ocurrencias en FSM, catálogo de eventos, invariantes y notas). **Diagrama de Bounded Context** completamente redibujado con domain services como etiquetas en las flechas (`[ServicioDeConciliacion]`, `[ServicioDeRegularizacion]`, `[ServicioDeAplicacionDevolucion]`). **Composición:** tipo `revertido` documentado en `PagoAplicado` (OxpComercio), `CrucePagoExtractoAplicado` (OxpExtracto) y `CruceRegularizacionAplicada` (Anticipo) — cruce espejo creado por compensación de saga `[SI3]`; `PartidaExtracto` con identidad explícita (posición/índice); `AjustePorTolerancia` enriquecido (inmutabilidad, identidad trazable, participación individual en distribución); conteos de eventos actualizados en composición. **FSM:** "(futuro)" eliminado de pago directo en OxpComercio (el evento ya existe en el catálogo); nota de Pagada reescrita (secuencia derivada en un solo append); OxpExtracto con estado Causada expandido mostrando eventos de progreso; notas consolidadas; diagrama de Anticipo mejorado; rename `PartidaDisputaDescartada`/`PartidaDisputaReclasificada` → `PartidaEnDisputaDescartada`/`PartidaEnDisputaReclasificada` (consistencia con preposición "En"). **Catálogo de eventos:** fusión de `DiferenciaEnCambioDetectada` + `ConceptoAjusteDiferenciaEnCambioGenerado` → `AjustePorDiferenciaEnCambioRegistrado` (hecho atómico indivisible); payloads enriquecidos en `ExtractoRadicado` y `AnticipoRegistrado` (distribución de costos por componente, ref I10); `CargosAdicionalesExtraidos` corregido como co-emisión atómica; `AnticipoVencido` y `ConciliacionVencida` con read model de alertas y resolución implícita; `DevolucionRadicada` con precondiciones ampliadas de Causada a Confirmada o posterior (alineación con I16); `OxpComercioPagada` y `ExtractoPagado` con mecánica de derivado por transición cuando pagos internos cubren 100%. **Invariantes:** preámbulo reescrito con clasificación **local** vs **eventual** y referencia a `[SI4]`; I1 marcada como eventual; I3 corregida (`PartidaExtracto` explícito, `CargoFinanciero` excluido del conteo [R06]); I4 descompuesta en I4a (OxpComercio), I4b (OxpExtracto), I4c (Anticipo), I4d (Devolucion) con excepciones específicas por agregado (Devuelta→Pendiente, revertido por saga, DevolucionRevertida); I6 con nota de configurabilidad [R25]; I7 marcada como eventual con enforcement dual (precondición en ServicioDeConciliacion + proyección [SI4]); I12 reestructurada como tabla de 5 filas por estado; I17 marcada como eventual con enforcement dual (precondición en ServicioDeAplicacionDevolucion + proyección [SI4]); I16 con referencia a [PD3]. **Domain services:** ServicioDeConciliacion con flujo de partidas de retorno documentado (sin tabla de compensación, justificación); paso 5 ampliado con nota sobre inexistencia de evento de reversa y [PD2]; ServicioDeRegularizacion con mecánica explícita de conflicto de versión en escenario 1:N; ServicioDeAplicacionDevolucion Ramas Comercio-B y C con compensación de stream huérfano documentada (intervención operativa); 3 domain services con nota "no duplica eventos de dominio". **Sugerencias de implementación:** nueva `[SI4]` (unicidad I1 → proyección con constraint compuesto); `[SI1]` actualizado con tipo `revertido` en 3 entidades; `[SI2]` con fila de tabla de compensación por Strategy; `[SI3]` con política de fallo de compensación (dead letter queue, alertas, intervención manual). **D20:** nota de portabilidad agregada. **Nueva Sección 11 — Pendientes por definir:** sistema formal `[PD#]` con PD1 (reembolso anticipo → CXC), PD2 (cruce tipo reversa para OxpComercio/OxpExtracto), PD3 (I16 con sistema de Tesorería); pendientes inline consolidados como referencias a `[PD#]` (~5 ubicaciones). **Referencias cruzadas:** rutas kebab-case (`definicion-alcance.md`, `guias-de-modelado/modelar-agregados.md`); rango de reglas R01–R27 → R01–R35; rutas en changelog v2.2 y v2.5 corregidas. 47 eventos (OxpComercio: 13, OxpExtracto: 20, Anticipo: 10, Devolucion: 4) — neto -1 por fusión de 2 eventos en 1. 17 invariantes (I4 descompuesta en I4a–I4d). **⚠️ Pendientes:** ver Sección 11 (`[PD1]`, `[PD2]`, `[PD3]`). |
| 2.7 | Marzo 2026 | **Moneda operativa del extracto y partidas en moneda extranjera.** Nueva regla R05d (moneda operativa del extracto) y premisa P2 en `definicion-alcance.md` y `modelo-dominio.md`. Enfoque: un OxpExtracto opera en una sola moneda — si las partidas son homogéneas, opera en esa moneda; si son mixtas (ej: tarjetas con facturación segmentada), las partidas en moneda extranjera se convierten a moneda funcional y el extracto opera en moneda funcional. **Composición:** `PartidaExtracto` ampliada con atributos de moneda original (monedaOriginal, valorOriginal, TRM). **Comportamiento calculado:** `valorTotalExtracto()` y `saldoPorPagar()` operan en la moneda del extracto. **Diagrama:** partida de ejemplo con moneda extranjera. **Evento:** `ExtractoRadicado` enriquecido con moneda del extracto y atributos de moneda por partida. **Invariante:** I5 extendida a OxpExtracto (consistencia de moneda en partidas y moneda operativa del extracto). **Glosario:** nuevos términos (Moneda Funcional), ampliaciones (Extracto Bancario, Diferencia en Cambio con dos momentos: conciliación y desembolso). **Variante Radicación:** OXP de Extracto con moneda extranjera. **R10b:** nota aclaratoria sobre partidas ya convertidas. **Frontera OXP-Tesorería:** la diferencia de cambio al momento del desembolso es responsabilidad del dominio de Tesorería. Validación con ERPs internacionales documentada en `fuentes/investigacion-moneda-unica-por-factura.md`. 47 eventos (sin cambios). 17 invariantes (I5 ampliada). Premisa P2 nueva. |
| 2.8 | Marzo 2026 | **Integración OXP → Impuestos, catálogo de gasto directo y clasificación por fases.** `ConceptoDeGasto` enriquecido con `clasificacionTributaria`, `conceptoPago` (refs. catálogo Impuestos) y `referenciaOrigen` (código del concepto en el catálogo del sub-dominio origen). `subDominioOrigen` como atributo de `OxpComercio` (deducido de identidad del consumidor `[SI5]`). Contrato de integración con Impuestos formalizado en dos operaciones (D22): solicitud de cálculo (síncrona al radicar) y confirmación (asíncrona al confirmar). Desgravamen para devoluciones tipo Comercio (prorrateo, no motor). Diagramas de flujo de integración: Flujo A (gasto directo) y Flujo B (desde módulo de gestión) con tabla comparativa. Diagrama de bounded context con integración Impuestos. `ConceptoDevuelto` actualizado con semántica de desgravamen. Nueva premisa P3 (retenciones al reconocer en dirección de gasto). Nuevas decisiones D21 (catálogo de gasto directo — modelo federado), D22 (contrato de integración OXP → Impuestos). [SI5] (subDominioOrigen deducido de identidad del consumidor). PD4 (definición detallada del agregado CatalogoGastoDirecto). Eventos OxpComercioRadicada y OxpComercioConfirmada actualizados con integración Impuestos. Tributo (Impuesto) y Tributo (Retención) actualizados con referencia al sub-dominio de Impuestos. Nuevo anexo: `integraciones/catalogo-conceptos-por-dominio.md` — decisión arquitectónica de catálogos federados con directriz para nuevos sub-dominios. Nuevo agregado de configuración `CatalogoGastoDirecto` con entidad interna `ConceptoGastoDirecto` y 4 eventos (Sección 5.7) — PD4 resuelto. D2 actualizada (5 agregados: 4 transaccionales + 1 configuración). Nueva decisión D23 (canales de entrada agnósticos con clasificación inteligente — la clasificación no usa tablas estáticas ni flujos rígidos). Diagrama de bounded context actualizado con canales de entrada y clasificación inteligente [D23]. 51 eventos (47 transaccionales + 4 configuración). 17 invariantes (sin cambios). Premisa P3 nueva. Decisiones D21, D22, D23 nuevas. `[SI5]` nuevo. Nueva convención `[F1]`/`[F2]` en Sección 2. Tabla de clasificación de capacidades en Sección 3 (Núcleo transaccional F1, Configuración F1, Ampliación F2). Todos los agregados y domain services marcados con `[F1]`. OxpCajaMenor mapeada como `[F2]` (por especificar). Nueva decisión D24 (clasificación por fases — dependencia funcional, no cronograma). |
| 2.9 | Marzo 2026 | **Auditoría v2.8 — 17 hallazgos (1 Alta, 9 Media, 7 Baja), 1 descartado (C2).** `AnticipoRegistrado` enriquecido para anticipos nacidos de devolución — documenta `CrucePagoAplicado` tipo devolucion y entrada directa a estado Pagado (ES1 Alta). Rango de reglas corregido R01–R35 → R01–R37 (G1/SC1). Convención `[D##-Xxx]` para referencias cruzadas a otros sub-dominios (SC2). `ServicioDeConciliacion` con tercer flujo: cobertura de anticipo con tabla de compensación bilateral (C1/SG1). Coordinador nombrado en `PartidaCubiertaPorAnticipo` y `AnticipoVinculadoAPartida` (G2). `CatalogoGastoDirecto`: validación de referencias fiscales en precondiciones (RS1), nueva invariante I18 de unicidad de código (INV1). C2 (scope singleton) descartado — la segmentación por empresa es implícita en todos los agregados, se definirá en implementación. FSM Anticipo con entrada directa a Pagado desde devolución (FSM1). `CatalogoGastoDirectoCreado` con payload (ES2). `DevolucionRadicada` con ref a `[R28]` (ES3). `[R36]` referenciada en D23 y diagrama BC (SC3). Nota de idempotencia de pagos externos en D20 (ID1). Nuevo PD4: prorrateo desgravamen parcial (OD1). `[R37]` operacionalizada en OxpComercioRadicada y OxpComercioConfirmada (OD2). 51 eventos (sin cambios — `AnticipoRegistrado` existente enriquecido para cubrir registro manual y nacimiento por devolución). 18 invariantes (+1 — I18). |
| 3.0 | Mayo 2026 | **Causación contable del Anticipo — cierre del hueco contable inicial.** El Anticipo se causa al confirmarse, replicando el patrón de OxpComercio. Antes de v3.0 el efecto contable inicial del anticipo se asumía embebido en la causación de la OxpComercio que lo regularizaba, lo cual generaba un hueco real (anticipo pagado pero no regularizado sin reflejo contable) y bloqueaba la integración con SincoA&F (que paga sobre asiento). **Nuevos estados Anticipo:** Confirmada y Causada (5 → 7 estados; ciclo: Vigente → Confirmada → Causada → Pagado / Regularizado → Cerrado / Reversado). **Nuevos eventos:** `AnticipoConfirmado` (Vigente → Confirmada; manual o automático por `[R12]`) y `AnticipoCausado` (Confirmada → Causada; integración saliente Db Anticipos · Cr CxP por anticipos vía `lineasParaTraduccion()` del agregado). Eventos existentes actualizados con nuevos estados previos: `AnticipoRegularizado`, `RegularizacionRevertida`, `AnticipoVinculadoAPartida`, `PagoAnticipoAplicado`, `AnticipoPagado`, `RegularizacionDeAnticipoCompletada`, `AnticipoReversado`, `AlertaPlazoAnticipoVencido`. FSM del Anticipo redibujado con Confirmada y Causada como pasos previos a las dimensiones de pago/regularización. **Invariantes:** I16 extendida para cubrir Anticipo (pagos externos solo desde Causada). I4c actualizada con secuencia Vigente → Confirmada → Causada y Reversado alcanzable desde Vigente o Confirmada. I8 reescrita (cruces de pago externos desde Causada, regularización desde Causada/Pagado, reversa desde Vigente/Confirmada). I17 actualizada (reversa de Anticipo desde Vigente o Confirmada). **Decisiones:** D25 nueva (causación del Anticipo replica patrón OxpComercio, cuenta puente, alineación con SAP/Oracle/NetSuite/Dynamics). D26 nueva (la amortización del anticipo se entrega junto con la causación de la OxpComercio que lo regulariza, en un único registro contable). **Pendientes:** PD3 extendido para incluir Anticipo (Tesorería desacoplada). **Domain services:** `ServicioDeRegularizacion` con precondición actualizada (Anticipo en Causada o posterior). `ServicioDeAplicacionDevolucion` Ramas Comercio-B/C — el Anticipo nacido de devolución pasa por Confirmada+Causada+Pagado en el mismo append (confirmación y causación automáticas heredadas; asiento Db Anticipos · Cr CxC proveedor, sin cuenta puente). Rama Anticipo opera desde Vigente o Confirmada. **Alcance v1.4** alineado: glosario "Anticipo" y "Causación" actualizados; Etapa 3 cubre los tres tipos de documento; tabla de estados por etapa con Confirmación y Causación de Anticipo; nueva regla R14b; nueva integración saliente "Causación de Anticipo" a SincoA&F; R15 y Etapa 5 Variante Amortización aclaradas (amortización viaja junto con la causación de la OXP que regulariza, no como registro independiente); tabla de integraciones salientes aclarada en la fila de amortización; descripción de `AnticipoAmortizado` aclarada (es la confirmación del sistema contable sobre la reclasificación que se realizó dentro de la causación de la OxpComercio). **Conteos:** 53 eventos (51 + `AnticipoConfirmado` + `AnticipoCausado`). 18 invariantes (sin cambios — I16, I4c, I8, I17 extendidas/reescritas). 26 decisiones (D24 + D25 + D26 nuevas). 4 pendientes (PD3 extendido). Estados del Anticipo: 5 → 7. **Lo que NO incluye:** integración con el sub-dominio Contabilidad (Paso 5 pendiente — hoy todo apunta a SincoA&F). |
| 3.1 | Mayo 2026 | **Alineación con el sub-dominio Contabilidad — generalización terminológica.** Se reemplaza la referencia fija al destino contable "SincoA&F" por el destino conceptual "sistema contable" (el sub-dominio Contabilidad del ERP, que actúa como gateway único). SincoA&F queda mencionado solo como ejemplo de destino físico legacy configurable, no como destino único. No hay cambios estructurales — el contrato funcional (causaciones, confirmaciones de pago, líneas de traducción) sigue siendo el mismo. **Cambios aplicados:** descripciones de eventos `OxpComercioCausada`, `ExtractoCausado`, `AnticipoCausado`, `DevolucionCausada`, `AnticipoAmortizado`; descripciones de eventos de pago `PagoOxpComercioDirectoAplicado`, `PagoExtractoAplicado`, `PagoAnticipoAplicado` (incluyen ahora "referencia de pago del sistema contable" con SincoA&F como ejemplo del destino físico); entidades de cruce `PagoAplicado` (tipo pago_directo), `CrucePagoExtractoAplicado` (tipo pago_sincoa con nota de trazabilidad), `CrucePagoAplicado` (Anticipo, tipo pago_directo); estado terminal de OxpExtracto y Devolucion; FSM del Anticipo (notas de Causada, AnticipoAmortizado y Pagado); composición y diagramas de OxpExtracto (cruce con referencia genérica al sistema contable); invariantes I9 (extendida a los cuatro agregados causables) e I16 (referencia genérica al sistema contable, manteniendo nombres técnicos pago_sincoa, pago_directo, pago_extracto); decisiones D15, D16, D20 con referencia generalizada (D20 mantiene SincoA&F como ejemplo del destino físico); pendiente PD3 (sistema actual donde el módulo de pagos del destino contable paga sobre asiento, con SincoA&F como destino legacy); diagrama de Bounded Context con nuevo sub-dominio vecino "Sistema Contable (sub-dominio Contabilidad)" como destino de causaciones. **No se tocaron:** tipo técnico `pago_sincoa` (nombre de tipo establecido, mantiene trazabilidad con destino legacy); menciones a SincoA&F en notas sobre `[PD2]` (asiento contrario si Anticipo ya estaba Causada — ejemplo legítimo); ejemplos de "número de transacción SincoA&F" en información capturada de eventos de pago externos; comparaciones con ERPs maduros en D25 (SAP, Oracle, NetSuite, Dynamics — referencias externas); changelog histórico. **Alcance v1.5** alineado: nueva entrada de glosario "Sistema Contable" (sub-dominio interno con destinos configurables); entradas "Pagada (OXP de Extracto)", "Amortización del Anticipo", "Pago Directo" generalizadas; actor "Sistema Contable" en sección 3 ampliado; módulo SincoA&F del ecosistema SincoERP redefinido como destino físico legacy; tabla de integraciones salientes con destino "Sistema Contable" y nota al pie aclarando configurabilidad; Etapas 3, 4 y 5 generalizadas; reglas R17, R18, R35 generalizadas; tabla de "fuera del alcance" y diagramas ASCII (sección 5 y arquitectura SincoERP) actualizados. **Conteos sin cambios:** 53 eventos, 18 invariantes, 26 decisiones, 4 pendientes, 7 estados del Anticipo. **Lo que NO incluye:** Gap 1 del Pendiente B (mapeo OXP → tipo de transacción contable que Contabilidad espera en cada línea de traducción) — diferido como discusión específica sobre si introducir el concepto "tipo de transacción contable" en OXP sin tensar D8. Gap 2 del Pendiente B (reacción de OXP ante rechazo del sistema contable, evento `EntregaRechazada` que Contabilidad emite) — diferido como decisión de negocio sobre comportamiento ante rechazo. Migración técnica de SincoA&F al sub-dominio Contabilidad — es trabajo de implementación, no de modelo. |
| 3.2 | Mayo 2026 | **Mapeo OXP → tipo de transacción contable (Gap 1 del Pendiente B).** Se documenta cómo OXP etiqueta sus eventos de causación con el `tipoTransaccion` que el sub-dominio Contabilidad espera para seleccionar la plantilla de asiento. La etiqueta es semántica del hecho económico, no es cuenta ni naturaleza — no contradice `[D8]`. **Cambios aplicados:** **D27 nueva** formaliza la decisión y referencia la sección de mapeo. **Nueva subsección "Integración con sub-dominio Contabilidad"** en Sección 3 (análoga a "Flujos de integración con Impuestos"): incluye la tabla canónica evento → tipoTransaccion → plantilla del inventario de Contabilidad, y notas sobre componentes que viajan dentro de las líneas (amortización, diferencia en cambio) y sobre el ack entrante `AnticipoAmortizado`. **Mapeo canónico:** `OxpComercioCausada` → `causacion_gasto`; `ExtractoCausado` → `causacion_gasto`; `AnticipoCausado` → `anticipo_a_proveedor`; `DevolucionCausada` (tipo Comercio o Extracto) → `nota_credito_proveedor`; `DevolucionCausada` (tipo Anticipo) → `reversa_anticipo` (plantilla nueva #7 — requiere registro en el inventario del sub-dominio Contabilidad como punto de coordinación cruzada). **Descripciones de eventos enriquecidas:** `OxpComercioCausada`, `ExtractoCausado`, `AnticipoCausado` y `DevolucionCausada` ahora mencionan el `tipoTransaccion` que emiten. **Notas operativas:** la amortización viaja como tipo de componente dentro de las líneas de `OxpComercioCausada` (no como `tipoTransaccion` separado, alineado con `[D26]`); la diferencia en cambio viaja como tipo de componente dentro del documento que la produjo (preserva la trazabilidad necesaria para ajustes y notas crédito posteriores); `AnticipoAmortizado` no emite `tipoTransaccion` (es ack entrante). **Alcance v1.6** alineado: nueva entrada de glosario "Tipo de Transacción Contable"; R14b ampliada para incluir la cláusula de etiquetado. **Conteos:** 27 decisiones (D26 + D27). Sin cambios en eventos (53), invariantes (18), pendientes (4) ni estados (7). **Lo que NO incluye:** Gap 2 del Pendiente B (reacción de OXP ante `EntregaRechazada` emitido por Contabilidad) — sigue diferido como decisión de negocio. |
| 3.3 | Mayo 2026 | **Manejo de rechazos del sistema contable y outbox del consumidor (Gap 2 del Pendiente B).** Se formaliza que OXP no cambia el estado de sus documentos causados ante rechazo del sistema contable — los rechazos se resuelven dentro del ciclo del sub-dominio Contabilidad (el contador para destino físico, el equipo de producto para defectos de catálogo). La durabilidad del hecho económico ante rechazos pre-borrador es responsabilidad del consumidor mediante outbox pattern. **Cambios aplicados:** **D28 nueva** formaliza el principio (OXP no modela el rechazo como evento de dominio; lo delega al ciclo del sub-dominio Contabilidad; el outbox es responsabilidad técnica del consumidor). **[SI6] nueva** documenta el outbox pattern del consumidor para integración contable (persistencia local, confirmación al recibir `EntregaAceptada`, reintento ante NACK del bus, métricas operativas). **Subsección "Integración con sub-dominio Contabilidad"** ampliada con diagrama del flujo bidireccional (causación saliente, `EntregaAceptada` entrante, manejo asíncrono de la `referenciaDestino`) y tabla de responsabilidades por tipo de rechazo (pre-borrador vs post-borrador). **Información capturada de los 4 eventos `*Causada`** (`OxpComercioCausada`, `ExtractoCausado`, `AnticipoCausado`, `DevolucionCausada`) actualizada: el "número de asiento contable externo" pasa a denominarse `referenciaDestino` y se persiste de manera asíncrona al recibir `EntregaAceptada`. Los efectos de los eventos mencionan explícitamente la espera de la confirmación entrante. **Alcance v1.7** alineado: nueva regla R14d (el documento OXP permanece en Causada ante rechazo; resolución a cargo del sistema contable; OXP conserva la causación hasta confirmar procesamiento exitoso). **Conteos:** sin cambios en eventos (53), invariantes (18), decisiones (27 — D28 nueva), pendientes (4), estados (7). 6 sugerencias de implementación (SI1-SI6). **Lo que NO incluye:** modelado del rechazo como evento de dominio en OXP (D28 explícitamente lo descarta); migración técnica SincoA&F → sub-dominio Contabilidad (sigue siendo trabajo de implementación). |
| 3.4 | Junio 2026 | **Canonización de `tipoComponente` y campos de narración hacia Contabilidad (issue #10).** Cierra la zona gris entre OXP y el catálogo de plantillas de Contabilidad. **Cambios aplicados:** **(1)** Renombre `nota_credito_proveedor` → `nota_credito_gasto` en el mapeo canónico, en los efectos de `DevolucionCausada` y en `[D27]` (nombre canónico acordado en Contabilidad #7). **(2)** Nueva tabla **"Catálogo canónico de `tipoComponente` emitidos por OXP"** en la subsección de integración: fija los nombres literales (snake_case) que viajan en `lineasParaTraduccion()`, coincidiendo 1:1 con el catálogo de Contabilidad. Para tributos, el `tipoComponente` es el **código específico** del desglose fiscal (`iva`, `inc`, `retefuente`, `reteiva`, `reteica`) en lugar de los genéricos "impuesto"/"retención" — habilita el acotado por grupo del PUC en Contabilidad. Los componentes devueltos reutilizan el mismo nombre que en la causación (sin sufijo `_devuelto`; la dirección la da la plantilla). Componentes adicionales canonizados: `cargo_financiero`, `diferencia_en_cambio`, `ajuste_tolerancia`, `amortizacion_anticipo`, `concepto_devuelto`, `anticipo`, `reversa_anticipo`. **(3)** Descripciones de `lineasParaTraduccion()` de OxpComercio, OxpExtracto, Anticipo y Devolucion actualizadas con los nombres canónicos. **(4)** Nueva nota **"Campos de narración que OXP puebla en `LineaTraduccion`"**: OXP incluye `descripcionConcepto` por línea (solo en `gasto`/`concepto_devuelto`/`anticipo`, desde `ConceptoDeGasto.descripcion`/`ConceptoDevuelto.descripcion`) y `descripcion` a nivel del hecho económico (opcional). Alinea con Contabilidad `[D13]`/`[R48]`. **Ajuste cruzado entre dominios:** en este mismo cambio se agregaron `amortizacion_anticipo` y `ajuste_tolerancia` como roles propios al catálogo de plantillas de Contabilidad (`causacion_gasto`, v1.3), preservando la coincidencia 1:1 (sus grupos del PUC quedan `porValidar`). **Conteos:** sin cambios en eventos (53), invariantes (18), decisiones (28), pendientes (4), estados (7). No requiere cambios en `definicion-alcance.md` — la canonización es detalle del contrato técnico (modelo), no del alcance funcional, que sigue describiendo los conceptos en lenguaje de negocio ("Tipo de Concepto": Gasto, Impuesto, Retención, etc.). |
| 3.5 | Junio 2026 | **Cruce de la partida del extracto en las líneas de traducción — componente `cruce_obligacion` (issue #18).** Cierra un hueco de la partida doble: el `lineasParaTraduccion()` del `OxpExtracto` solo emitía `cargo_financiero`, `diferencia_en_cambio` y `ajuste_tolerancia`, así que la cuenta por pagar acreditada al causar cada OxpComercio nunca se debitaba al pagar vía extracto. **Cambios aplicados:** **(1)** Nuevo `tipoComponente` `cruce_obligacion` en el catálogo canónico (origen `Vinculacion`; emitido en `causacion_gasto`; agregado `OxpExtracto`). **(2)** `Vinculacion` ampliada con `valorCruzado` (valor de la obligación saldada, a TRM de radicación) y `distribucionOrigen` (distribución por unidad organizacional del gasto de la compra, leída del agregado OxpComercio). **(3)** `lineasParaTraduccion()` del extracto emite una línea `cruce_obligacion` por `Vinculacion` (tercero del proveedor, valor a radicación, distribución de origen; sin `descripcionConcepto`); la unidad organizacional la rinde Contabilidad según `[I33 de Contabilidad]`, espejando la CxP de la causación original. **(4)** Efectos de `ExtractoCausado` actualizados (lectura de reclasificación: Db CxP proveedor vía cruce · Cr CxP banco/emisor vía contrapartida del motor). **(5)** Diagrama de composición del extracto actualizado. **(6)** Nueva decisión **D29**. **Ajuste cruzado entre dominios:** nuevo rol `CRUCE_OBLIGACION` (débito, grupo del PUC `["2205","2335"]`) en la plantilla `causacion_gasto` de Contabilidad, preservando la coincidencia 1:1. **Conteos:** sin cambios en eventos (53), invariantes (18), pendientes (4), estados (7); decisiones 28 → **29** (D29 nueva). No requiere cambios en `definicion-alcance.md` — es detalle del contrato técnico (modelo). |
| 3.6 | Junio 2026 | **Amortización del anticipo post-causación (Caso B) — issue #25.** Cierra el hueco contable cuando el cruce anticipo↔OXP ocurre después de causar la OXP (permitido por R30/R4 y §1307). **Cambios:** **(1)** `[D26]` reescrita: la amortización viaja **embebida** en la causación si el cruce es pre/durante (Caso A), y como **causación independiente** (`tipoTransaccion = amortizacion_anticipo`, Db CxP proveedor · Cr Anticipos) si es post-causación (Caso B, patrón SAP F-54). **(2)** Mapeo canónico: nueva fila `PagoOxpComercioViaAnticipoAplicado` (OXP ya Causada) → `amortizacion_anticipo`. **(3)** Nota de componentes: `amortizacion_anticipo` ahora es tipo de componente (Caso A) **y** `tipoTransaccion` propio (Caso B); fila del catálogo actualizada. **(4)** Efectos de `PagoOxpComercioViaAnticipoAplicado`: emite la causación de amortización separada cuando la OXP ya está Causada. **(5)** Efectos de `OxpComercioCausada` aclarados (Caso A vs B). **Ajuste cruzado:** nueva plantilla `amortizacion_anticipo` en Contabilidad. **Conteos:** sin cambios — eventos (53), invariantes (18), decisiones (29, D26 reescrita), pendientes (4), estados (7). Acompaña reescritura de R15/§311 en `definicion-alcance.md` y la regla de control de doble pago del issue #26. |
| 3.7 | Junio 2026 | **`terceroPrincipal` a nivel de hecho económico (issue #28).** OXP puebla `terceroPrincipal` (el `InformacionTercero` de la raíz del agregado emisor: proveedor en `OxpComercioCausada`/`AnticipoCausado`/`DevolucionCausada`, banco/emisor en `ExtractoCausado`), que el motor de Contabilidad usa como tercero de la contrapartida. Resuelve el caso del extracto, cuyas líneas `cruce_obligacion` traen varios proveedores pero cuya contrapartida (CxP del banco/emisor) no viaja en ninguna línea. **Cambios:** nota "Campos que OXP puebla en `LineaTraduccion`" ampliada con `terceroPrincipal`. Sin cambios de conteo. Acompaña `modelo-dominio.md` de Contabilidad v1.8 (`InformacionTransaccion` con `terceroPrincipal`, paso 4 del `ServicioDeTraduccion`). |
| 3.8 | Junio 2026 | **Agregado `Proveedor` — el rol del tercero de OXP en el modelo de bodega (replanteamiento #31, issue #38).** Quinto agregado del BC: registro propio del proveedor con validación empaquetada (identificación legal, direcciones, contactos — primera adopción del `Contacto` del paquete como { contacto, esPrincipal }), estado `Activo \| Inactivo` con `motivoInactivacion` cuyo origen (`local \| senal_global`) gobierna la reversa, `AsegurarProveedor` idempotente como única vía de creación, FSM 4.5 y 6 fichas de evento en 5.8 (5 de dominio + el **evento estándar de rol** hacia la bodega — estado completo + `secuencia`, derivado por transición, entrega por outbox `[SI6]`). Cableado: las radicaciones de Comercio, Anticipo y Devolución llevan `proveedorId` y copian el `InformacionTercero` del registro (el contrato con Contabilidad queda intacto); "mismo tercero" pasa a referencia exacta en regularización, devoluciones y conciliación contra anticipos; el emisor/banco del extracto no es Proveedor. Nuevas invariantes I19-I22, `[SI7]` (constraint único de clave natural), decisiones D30-D32 (D32 resuelve el candidato `InformacionTercero` del catálogo de Nuggets como composición local). Acompaña al alcance v1.10. |
| 3.9 | Junio 2026 | **Control de doble pago modelado — issue #30 (cierra lo que el #26 dejó abierto).** La regla se renumera **R36 → R38** en el alcance (ID duplicado con la clasificación inteligente de la v1.3). Elementos nuevos: entidad `ConstanciaAnticipoNoAplicable` en OxpComercio (juicio humano inmutable, una por anticipo); comando `RegistrarConstanciaAnticipoNoAplicable` válido en Pendiente, Confirmada y Causada con saldo — cierra el escenario sin salida del anticipo post-causación; eventos `ConstanciaAnticipoNoAplicableRegistrada` y `AlertaDoblePagoPotencial` (patrón de alertas existente, derivado por configuración); invariante **I23** condicionada por empresa con los dos puntos de aplicación (verificación temprana al confirmar; refuerzo al pagar **por canal**: extracto previene con humano en el lazo, pago directo detecta y alerta porque el dinero ya salió); decisión **D33** con el fundamento (la pertenencia anticipo↔OXP nunca es calculable — la constancia es el único mecanismo válido). Toques en `OxpComercioConfirmada`, `PagoOxpComercioDirectoAplicado`, `ServicioDeConciliacion` y FSM 4.1. Dos eventos nuevos en el catálogo (+1 entidad, +1 invariante, +1 decisión). Acompaña al alcance v1.11. |
| 4.0 | Junio 2026 | **Integración con Estructura Organizacional — copia local de unidades, no agregado (replanteamiento #45, issue #48).** OXP usa unidades organizacionales en todas sus distribuciones pero el modelo no documentaba su origen. Se formaliza la relación según `[D15]` de EO y la guía `datos-entre-dominios.md`: la unidad tiene **dueño único = EO**; OXP la consume como **copia local** (read model), no como agregado (contraste explícito con el `Proveedor` `[D30]`, que OXP sí co-gobierna). **Cambios:** **D34 nueva** (dato gobernado externo → copia local; valida y opera contra la copia, sin consulta en caliente; difiere vía destino pendiente cuando la unidad no existe/activa, sin aproximar; reacciona a reestructuraciones; publica señal de demanda + imputación hacia EO). **I24 nueva** (eventual — ninguna distribución nueva con unidad inexistente o inactiva en la copia local; gemela de `I22`; no reescribe el histórico). **`[SI8]` nueva** (copia local de unidades como proyección por eventos, idempotente, reconciliación de respaldo análoga a `[SI12]` de EO). **Nueva subsección "Integración con Estructura Organizacional"** en Sección 3 (análoga a las de Contabilidad e Impuestos): eventos entrantes (8 de ciclo de vida) y salientes (señal de demanda → `[SI11]` de EO; imputación → `[SI10]` de EO); el diagrama del flujo no se duplica (vive en EO §3.8). **`I10` y la cadena de resolución** ampliadas: si la unidad resuelta no existe/activa en la copia local, el componente cae en **destino pendiente** y se difiere hasta `UnidadActivada`. La *asignación inteligente* de la unidad (reglas finas/aprendizaje) se difiere a issue aparte (#51, extensión de `[D23]`). **Conteos:** sin cambios en eventos (53); invariantes 23 → **24** (I24 nueva); decisiones 33 → **34** (D34 nueva); sugerencias 7 → **8** (`[SI8]` nueva); pendientes (4), estados (7) sin cambios. Acompaña al alcance v1.12. |
| 4.1 | Junio 2026 | **Consistencia del modelo de comunicación con EO (issue #56).** Se **retira el aviso de imputación saliente** hacia EO: al replantear EO la validación de fecha efectiva (`[SI10]` retirada), OXP ya no publica imputación — su único aviso a EO es la **señal de demanda** (`DemandaDeUnidadSenalada`, nombre del contrato unificado). Se **añade `UnidadReactivada`** a la copia local (eventos entrantes): sin ella, una unidad reactivada desde `Suspendida` quedaba como suspendida en la copia y `I24` habría rechazado distribuciones nuevas indebidamente. `[D34]` ajustada (un solo aviso saliente). Sin cambios de conteo (53 eventos, 24 invariantes, 34 decisiones, 8 SIs). Acompaña al alcance v1.13 y a EO (R25/I08 replanteadas, SI10 retirada). |
| 4.2 | Junio 2026 | **Resolución del emisor/banco del extracto cuando el archivo no lo trae (issue #57).** Algunos extractos no identifican la entidad financiera emisora (razón social/NIT) pero siempre traen el número de tarjeta. Se define la **cadena de resolución del emisor** (regla **provisional** mientras se define el dueño del dato "tarjeta"): del archivo → inferido del `OxpExtracto` más reciente con el mismo número de tarjeta (sugerencia revisable) → captura del usuario; el emisor sigue siendo un **Tercero** (entidad financiera), **sin crear agregado, catálogo ni registro de tarjetas** (anti-patrón de `#31`/`#45`). **Cambios:** **D35 nueva** (regla de resolución + origen rastreado `del_archivo \| inferido_historico \| capturado_usuario`; llave = número de tarjeta; dueño futuro del dato "tarjeta" en Tesorería o un servicio global, **por analizar**, migrable a copia local `[D34]`/`[SI8]`). **`[SI9]` nueva** (proyección "último emisor por número de tarjeta", índice derivado de `ExtractoRadicado`, no entidad). Ficha **`ExtractoRadicado`** y VO **`InformacionTercero`** de OxpExtracto ampliados con el origen del emisor y la cadena de resolución. **Conteos:** sin cambios en eventos (53), invariantes (24), estados (7), pendientes (4); decisiones 34 → **35** (D35 nueva); sugerencias 8 → **9** (`[SI9]` nueva). Acompaña al alcance v1.14 (R39 nueva). |
| 4.3 | Junio 2026 | **Asignación de la unidad organizacional por cadena de niveles, inspirada en Contabilidad (issue #51).** **D36 nueva**: la distribución por unidad se resuelve con Nivel A (reglas configurables, determinísticas, por especificidad sobre proveedor/tipo/lugarEjecucion) + Nivel B (sugerencia por aprendizaje, no vinculante, promovible a regla e invalidable); estructura de niveles inspirada en la cadena de resolución de cuentas de Contabilidad. **Nuevo agregado de configuración `CatalogoReglasDistribucion`** (entidad `ReglaDeDistribucion`, 4 eventos en §5.9) — D2 actualizada (incluye además el `Proveedor` que faltaba listar). **I25 nueva** (distribución de cada regla suma 100%; sin dos reglas activas con criterios idénticos). **`[SI10]` nueva** (proyección de aprendizaje: unidad frecuente por combinación). Cadena de resolución (§3) e `I10` actualizadas: el nivel "preferencia de empresa" se reemplaza por Nivel A + Nivel B; trazabilidad del nivel. Todo sobre la copia local de unidades activas (`[SI8]`/`[D34]`). Conteos: invariantes 24 → **25**; decisiones 35 → **36**; SIs 9 → **10**; +1 agregado de configuración. Acompaña al alcance v1.15 (R40). |
| 4.4 | Junio 2026 | **Retiro de la emisión de la señal de demanda; la copia local es para validación, no para la UI (issue #72/#74).** Una vez la unidad se elige de la fuente de verdad (la UI lee EO en vivo; las reglas de distribución `[D36]` se parametrizan contra EO), referenciar una unidad inexistente no ocurre en operación → la señal `DemandaDeUnidadSenalada` (única saliente hacia EO) queda sin disparador y **se retira**: OXP ya no emite eventos salientes hacia EO, solo consume su ciclo de vida. La subsección de integración y `[D34]` se reescriben: se elimina la fila de evento saliente; el `diferir` se reencuadra a **consistencia eventual** (la unidad existe en EO, solo puede faltar la propagación del evento a la copia, o estar inactiva); se aclara que la **copia local es una proyección para validación del dominio, no una API de lectura para la UI** (la interfaz lee EO directamente). Se actualiza la nota de `[D34]` sobre la asignación automática para apuntar a `[D36]` (ya implementada). Sin cambios de conteo (la señal no era evento del catálogo de OXP; `[D34]` se reformula, no se elimina): 53 eventos, 25 invariantes, 36 decisiones, 10 SIs. Acompaña al alcance v1.16 y al retiro del aparato de señal/bandeja en EO (modelo v1.7) y la guía `datos-entre-dominios.md`. |
