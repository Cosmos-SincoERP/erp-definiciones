# Modelo de Dominio — Contabilidad

## Tabla de contenido

1. [Propósito y relación con otros documentos](#1-propósito)
2. [Convenciones del documento](#2-convenciones)
3. [Bounded Context y Agregados](#3-bounded-context)
4. [Máquinas de estado](#4-máquinas-de-estado)
5. [Catálogo de eventos](#5-catálogo-de-eventos)
6. [Tipos de concepto](#6-tipos-de-concepto)
7. [Invariantes del dominio](#7-invariantes)
8. [Qué NO contiene este documento](#8-exclusiones)
9. [Decisiones de arquitectura y diseño](#9-decisiones)
10. [Premisas de negocio](#10-premisas)
11. [Pendientes por definir](#11-pendientes)
12. [Catálogo de permisos atómicos](#12-permisos)

---

## 1. Propósito y relación con otros documentos

| Documento | Rol | Descripción |
|-----------|-----|-------------|
| `definicion-alcance.md` | QUÉ hace el sistema | Fuente de verdad para glosario, actores, flujos y reglas (`[R##]`). No se duplica aquí. |
| **Este documento** | CÓMO se comporta el dominio | Agregados, eventos, transiciones, precondiciones, invariantes. Organizado en dos niveles: N1 (Motor de Traducción y Servicio de Entrega — obligatorio) y N2 (Sistema contable — opcional). |
| EventCatalog | Catalogación técnica | Consumirá este documento como especificación de entrada. |

Las reglas de negocio se referencian como `[R##]` y su texto completo vive en `definicion-alcance.md`, Sección 6. Las decisiones de diseño previas están documentadas en `anexo-decisiones-de-diseno.md` y se referencian como `[DD##]`.

---

## 2. Convenciones del documento

### 2.1. Nomenclatura
- **Eventos:** PascalCase en español, sin tildes ni caracteres especiales (compatibilidad con código fuente).
- **Referencias:** `[R##]` reglas de negocio (viven en `definicion-alcance.md`), `[DD##]` decisiones de diseño previas (viven en `anexo-decisiones-de-diseno.md`), `[D##]` decisiones de este documento, `[P##]` premisas, `[I##]` invariantes, `[SI##]` sugerencias de implementación, `[PD#]` pendientes.
- **Agregados:** Nombres en PascalCase; corresponden a los términos del glosario canónico (`definicion-alcance.md`, Sección 2).
- **Alcance del glosario canónico:** Los domain services, entidades internas y value objects son artefactos del modelo de dominio — no requieren entrada en el glosario canónico.
- **Niveles:** N1 — Motor de Traducción y Servicio de Entrega (obligatorio). N2 — Sistema contable (opcional). Cada agregado y domain service indica su nivel.
- **Fases:** F1 — N1 con SincoA&F como sistema contable de destino. F2 — N2 (sistema contable propio). F2+ — Mejoras futuras (cierre definitivo, adaptadores adicionales).

### 2.2. Template de evento

Cada evento se documenta con esta estructura:

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Qué ocurrió en términos de negocio. |
| **Causalidad** | Tipo: directa, derivado por transición, derivado por configuración, efecto inter-agregado, compensatorio. |
| **Agregado** | Agregado que emite el evento. |
| **Nivel** | N1 o N2. |
| **Estado previo** | Estado requerido del agregado antes del evento. |
| **Estado resultante** | Estado del agregado después del evento (o "sin cambio" si es evento de progreso). |
| **Precondiciones** | Condiciones que deben cumplirse. Referencias a `[R##]`. |
| **Información capturada** | Datos que el evento registra (payload). |
| **Efectos** | Consecuencias: entidades creadas, saldos modificados, eventos derivados. |

### 2.3. Diagramas
- FSM en ASCII. Estados terminales marcados con `■`.
- Estados transitorios marcados con `(transitorio)`: estado que el sistema abandona automáticamente mediante efecto inter-agregado, sin intervención del usuario.
- Eventos de progreso (sin cambio de estado) se listan dentro del recuadro del estado.
- Eventos de transición se muestran en las flechas entre estados.

### 2.4. Causalidad entre eventos

| Tipo | Descripción | Consistencia |
|------|-------------|-------------|
| Directa | Comando del usuario o del consumidor. | Transaccional |
| Derivado por transición | Mismo agregado, mismo append atómico. | Transaccional |
| Derivado por configuración | Mismo agregado, condicional a configuración. | Transaccional |
| Efecto inter-agregado | Domain service coordina entre agregados. | Eventual |
| Compensatorio | Revierte un efecto previo por fallo. | Eventual |

### 2.5. Precisiones terminológicas

| Término | Precisión |
|---------|-----------|
| **Borrador contable** | Resultado de la traducción en N1. Tiene tres estados: PENDIENTE, RESUELTO, DESCARTADO. No es un asiento contable — es el insumo para que el destino cree uno. |
| **Asiento contable** | Registro inmutable que solo existe en N2. Lo crea N2 a partir de un borrador resuelto. En destinos externos (SincoA&F, Siigo), el equivalente lo crea el sistema externo. |
| **Motor de traducción / ServicioDeTraduccion** | El glosario lo llama "Motor de traducción". En el modelo se implementa como domain service `ServicioDeTraduccion`. Son el mismo concepto. |
| **Servicio de Entrega / EntregaContable** | El glosario lo llama "Servicio de Entrega". En el modelo se implementa como agregado `EntregaContable` (con stream propio). Son el mismo concepto — el agregado registra cada entrega individual. |
| **Sistema contable de destino** | El sistema que recibe los borradores resueltos. Puede ser N2 o un sistema externo. Solo uno activo por empresa. |
| **Combinación de dimensiones** | Conjunto de dimensiones con el que ReglaDeDerivacion y Aprendizaje resuelven una cuenta. Tiene dos partes: las **dimensiones estables** (tipoTransaccion, tipoComponente, empresa) — códigos canónicos que se comparan por igualdad exacta y delimitan la partición donde se busca — y la **clasificación** — texto semántico que viaja en la línea de traducción y se empareja **por similitud** contra el texto ancla de las reglas o el texto de las resoluciones aprendidas dentro de esa partición [D15]. No confundir con "dimensiones de segmentación" de NumeracionContable (empresa, tipoComprobante, periodo, sucursal) que son un concepto diferente. |
| **Clasificación** | Texto semántico de emparejamiento que el consumidor compone mecánicamente por componente a partir de datos de sus catálogos (no lo digita un usuario, no es un código ni una llave). Obligatorio en toda línea de traducción. Las tres capas de la cadena lo comparan por similitud: Niveles A y C contra textos ancla/aprendidos; Nivel B contra las descripciones de las cuentas del PUC [D15]. |

---

## 3. Bounded Context y Agregados

### 3.1. Contabilidad como Bounded Context

**Clasificación de capacidades:**

| Nivel | Agregado | Tipo | Fase |
|-------|----------|------|:----:|
| N1 — Motor de Traducción | BorradorContable | Transaccional (ES) | F1 |
| N1 — Motor de Traducción | Aprendizaje | Transaccional (ES) | F1 |
| N1 — Motor de Traducción | PlanDeCuentas | Configuración | F1 |
| N1 — Motor de Traducción | MarcoContable | Configuración | F1 |
| N1 — Motor de Traducción | ReglaDeDerivacion | Configuración | F1 |
| N1 — Motor de Traducción | PlantillaDeAsiento | Configuración | F1 |
| N1 — Servicio de Entrega | EntregaContable | Transaccional (ES) | F1 |
| N1 — Servicio de Entrega | SistemaContableDestino | Configuración | F1 |
| N2 — Sistema contable | AsientoContable | Transaccional (ES) | F2 |
| N2 — Sistema contable | PeriodoContable | Transaccional (ES) | F2 |
| N2 — Sistema contable | LibroContable | Configuración | F2 |
| N2 — Sistema contable | NumeracionContable | Configuración | F2 |
| N2 — Sistema contable | EquivalenciaPuc | Configuración | F2 |

**Diagrama del Bounded Context:**

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                    Bounded Context: Contabilidad                                  │
│                                                                                   │
│  ┌─ N1 — Motor de Traducción y Servicio de Entrega ────────────────────────────┐ │
│  │                                                                              │ │
│  │  ┌────────────────┐    ┌──────────────────┐    ┌─────────────────────┐      │ │
│  │  │ PlanDeCuentas  │    │ReglaDeDerivacion │    │ PlantillaDeAsiento  │      │ │
│  │  │ (config)       │    │ (config)         │    │ (config)            │      │ │
│  │  └───────┬────────┘    └────────┬─────────┘    └──────────┬──────────┘      │ │
│  │          │                      │                          │                 │ │
│  │          └──────────┬───────────┘──────────────────────────┘                 │ │
│  │                     ▼                                                        │ │
│  │  ┌──────────────────────────────┐         ┌───────────────┐                 │ │
│  │  │     BorradorContable         │────────▶│  Aprendizaje  │                 │ │
│  │  │     (transaccional)          │◀────────│ (transaccional)│                 │ │
│  │  └──────────────┬───────────────┘         └───────────────┘                 │ │
│  │                 │                                                            │ │
│  │                 │ [ServicioDeEntrega]                                        │ │
│  │                 ▼                                                            │ │
│  │        ┌─────────────────┐                                                  │ │
│  │        │ Sistema contable│                                                  │ │
│  │        │   de destino    │─ ─ ─ ─ ─ ─ ─ ─ ▶ SincoA&F / Siigo / N2        │ │
│  │        └─────────────────┘                                                  │ │
│  └──────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                   │
│  ┌─ N2 — Sistema contable (opcional) ──────────────────────────────────────────┐ │
│  │                                                                              │ │
│  │  ┌────────────────┐  ┌──────────────────┐  ┌───────────────┐               │ │
│  │  │ LibroContable  │  │NumeracionContable│  │EquivalenciaPuc│               │ │
│  │  │ (config)       │  │ (config)         │  │ (config)      │               │ │
│  │  └───────┬────────┘  └────────┬─────────┘  └──────┬────────┘               │ │
│  │          │                    │                     │                        │ │
│  │          └────────┬───────────┘─────────────────────┘                       │ │
│  │                   ▼                                                          │ │
│  │  ┌────────────────────────┐         ┌──────────────────┐                   │ │
│  │  │   AsientoContable      │────────▶│  PeriodoContable │                   │ │
│  │  │   (transaccional)      │         │  (transaccional) │                   │ │
│  │  └────────────────────────┘         └──────────────────┘                   │ │
│  └──────────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

### 3.2. Agregado: BorradorContable (N1)

**Descripción:** Resultado de la traducción de un hecho económico. Es el objeto central de N1 — todo hecho económico que llega de un consumidor produce un borrador. El borrador recorre su ciclo de vida hasta que se entrega al Servicio de Entrega o se descarta.

**Raíz:** BorradorContable

**Ciclo de vida:** Transaccional (ES) — 3 estados: PENDIENTE, RESUELTO, DESCARTADO.

**Stream de eventos:** `borrador-contable-{id}`

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| — | Raíz | Atributos de la raíz BorradorContable | id (identificador del stream), estado (PENDIENTE/RESUELTO/DESCARTADO — derivado del stream), descripcion (texto general del hecho económico, opcional — la envía el consumidor; si no la envía, queda vacía, ver [D13]) |
| PartidaBorrador | Entidad | Línea individual del borrador con débito o crédito | cuenta (auxiliar, puede ser null si no resuelta), tercero, unidadOrganizacional, debito, credito, nivelResolucion (espejo/A/C/B/manual/null), rol (código del rol heredado del RolPartida de la plantilla: GASTO/IMPUESTO/RETENCION/CONTRAPARTIDA — conjunto cerrado, ver [D14]), esContrapartida (boolean, heredado del RolPartida — equivale a rol == CONTRAPARTIDA), clasificacion (texto semántico de la línea de traducción que la originó — insumo de la resolución por similitud y del aprendizaje, ver [D15]), descripcionConcepto (narración del movimiento, opcional — solo en partidas cuyo componente la lleva, ver [D13]) |
| ReferenciaOrigen | VO | Identificación única del hecho económico que originó el borrador | referenciaOrigen, subDominioOrigen, documentoFuente, referenciaHechoRelacionado (opcional — vincula devoluciones/notas crédito con el hecho original) |
| InformacionTransaccion | VO | Contexto de la transacción | tipoTransaccion, empresa, moneda, fecha, terceroPrincipal (el tercero del documento que envía el emisor: proveedor en causación de gasto/anticipo/nota crédito, banco o emisor en extracto, etc. — corresponde al `InformacionTercero` de la raíz del agregado emisor; **informativo** desde [D15]: el tercero de la contrapartida ya no se toma de aquí sino de la línea `contrapartida` que envía el consumidor, ver paso 4) |
| MotivoRechazo | VO | Motivo del rechazo del destino (si aplica) | motivo, fechaRechazo, entregaId, destino |

**Comportamiento calculado (no almacenado):**

| Método | Fórmula | Usado por |
|--------|---------|-----------|
| `balancea()` | suma(débitos) == suma(créditos) | [R01] Validación de balance |
| `cuentasResueltas()` | todas las partidas tienen cuenta != null | Determina si pasa a RESUELTO |
| `esManual()` | referenciaOrigen.subDominioOrigen == "Contabilidad" | [R09][R10] Determina si es descartable |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  BorradorContable (Agregado)                                         │
│                                                                      │
│  estado: PENDIENTE (derivado del stream)                             │
│  descripcion: "Honorarios auditoría externa" (opcional)             │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ ReferenciaOrigen (VO)                                          │  │
│  │  referenciaOrigen: oxp-comercio-{id}/OxpComercioCausada        │  │
│  │  subDominioOrigen: OXP · documentoFuente: OXP-COM-5678        │  │
│  │  referenciaHechoRelacionado: null (o ref al hecho original)   │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ InformacionTransaccion (VO)                                    │  │
│  │  tipoTransaccion: causacion_gasto · empresa: COSMOS-SAS       │  │
│  │  moneda: COP · fecha: 2026-03-15                              │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ PartidaBorrador #1 (Entidad)                                   │  │
│  │  rol: GASTO · esContrapartida: false                           │  │
│  │  cuenta: 5110-05-002 · tercero: 900123456 · undOrg: VTA-001    │  │
│  │  debito: 600.000 · credito: 0 · nivelResolucion: C             │  │
│  │  clasificacion: "Servicios de auditoría externa ·              │  │
│  │                  honorarios · servicios profesionales"         │  │
│  │  descripcionConcepto: "Honorarios auditoría externa"           │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ PartidaBorrador #2 (Entidad) — retención (no lleva concepto)   │  │
│  │  rol: RETENCION · esContrapartida: false                       │  │
│  │  cuenta: null · tercero: 900123456 · undOrg: VTA-001           │  │
│  │  debito: 0 · credito: 66.000 · nivelResolucion: null           │  │
│  │  clasificacion: "honorarios · retención en la fuente 11%"      │  │
│  │  descripcionConcepto: — (su componente no lleva descripción)   │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ MotivoRechazo (VO) — solo si fue rechazado por el destino     │  │
│  │  motivo: "periodo cerrado" · fechaRechazo: 2026-04-01         │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

**Eventos:** 13 eventos propios (ver Sección 5).

---

### 3.3. Agregado: Aprendizaje (N1)

**Descripción:** Registra las resoluciones de cuentas que el contador ha tomado al completar borradores pendientes. Cada resolución asocia las dimensiones estables (tipoTransaccion, tipoComponente, empresa) y el texto de clasificación de la línea resuelta con la cuenta auxiliar que el contador eligió. El motor de traducción consulta estos registros como Nivel C de la cadena de resolución: selecciona los aprendizajes de la partición estable y empareja la clasificación de la línea nueva **por similitud** contra los textos aprendidos [D15] [SI8].

**Raíz:** Aprendizaje

**Ciclo de vida:** Transaccional (ES) — sin FSM. Eventos de registro y promoción. **Nota:** El Aprendizaje es un receptor pasivo — sus eventos son efectos de acciones en otros agregados (CuentaResuelta → ResolucionAprendida). Es un patrón válido para un agregado que acumula conocimiento progresivamente. No tiene evento de creación propio [SI4].

**Stream de eventos:** `aprendizaje-{id}`

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| ResolucionAprendida | Entidad | Una resolución de cuenta aprendida del contador | dimensiones estables (tipoTransaccion, tipoComponente, empresa), clasificacion (texto semántico de la línea que el contador resolvió), cuentaAuxiliar, fechaAprendizaje |

**Comportamiento calculado (no almacenado):**

| Método | Fórmula | Usado por |
|--------|---------|-----------|
| `resolver(dimensionesEstables, clasificacion)` | Selecciona las ResolucionAprendida cuya partición estable coincide exactamente y empareja por similitud la clasificación recibida contra los textos aprendidos; resuelve con la de mayor similitud si supera el umbral [SI8]. Una clasificación idéntica (compra repetida) da similitud máxima. | Cadena de resolución, Nivel C [DD2] [D15] |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  Aprendizaje (Agregado)                                              │
│                                                                      │
│  empresa: COSMOS-SAS                                                 │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ ResolucionAprendida #1 (Entidad)                               │  │
│  │  tipoTransaccion: causacion_gasto · tipoComponente: gasto      │  │
│  │  clasificacion: "Servicios de auditoría externa ·              │  │
│  │                  honorarios · servicios profesionales"         │  │
│  │  empresa: COSMOS-SAS · cuentaAuxiliar: 5110-05-002             │  │
│  │  fechaAprendizaje: 2026-03-15                                  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ ResolucionAprendida #2 (Entidad)                               │  │
│  │  tipoTransaccion: causacion_gasto · tipoComponente: iva        │  │
│  │  clasificacion: "honorarios · iva 19%"                         │  │
│  │  empresa: COSMOS-SAS · cuentaAuxiliar: 2408-01-001             │  │
│  │  fechaAprendizaje: 2026-03-15                                  │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

**Eventos:** 3 eventos propios (ver Sección 5).

**Gobernanza:** El aprendizaje es generado por el contador al resolver cuentas (ResolucionAprendida). El analista contable supervisa los aprendizajes acumulados: puede promoverlos a regla formal (AprendizajePromovidoARegla) o invalidarlos si son erróneos (AprendizajeInvalidado) [R35].


---

### 3.4. Agregado: PlanDeCuentas (N1, configuración)

**Descripción:** Catálogo jerárquico de cuentas contables de una empresa. Necesario para que el motor de traducción resuelva cuentas durante la traducción. Cada PlanDeCuentas referencia un MarcoContable que identifica formalmente el esquema bajo el cual se diseña (NIIF, marcos locales, gerencial, consolidación, etc.).

**Raíz:** PlanDeCuentas

**Atributos de la raíz:** id, empresa, nombre (texto descriptivo), marcoContable (referencia al código del agregado MarcoContable, inmutable tras creación [I32]).

**Ciclo de vida:** Configuración — sin FSM transaccional. El PUC se crea una vez y las cuentas se agregan, modifican o inactivan.

**Stream de eventos:** `plan-de-cuentas-{id}`

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| CuentaContable | Entidad | Cuenta individual dentro del PUC | codigo, nombre, tipo (gasto/costo/ingreso/activo/pasivo/patrimonio/banco), nivel (maestra/auxiliar), estado (activa/inactiva), obligatoriedadTercero, obligatoriedadUnidadOrganizacional |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  PlanDeCuentas (Agregado)                                            │
│                                                                      │
│  empresa: COSMOS-SAS · nombre: PUC NIIF · marcoContable: NIIF       │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ CuentaContable #1 (Entidad)                                    │  │
│  │  codigo: 5110-05-002 · nombre: Honorarios                      │  │
│  │  tipo: gasto (ref. TipoDeCuenta) · nivel: auxiliar              │  │
│  │  estado: activa                                                │  │
│  │  obligatoriedadTercero: null (hereda del tipo)                 │  │
│  │  obligatoriedadUndOrg: null (hereda del tipo)                  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ CuentaContable #2 (Entidad)                                    │  │
│  │  codigo: 5110 · nombre: Gastos de personal                     │  │
│  │  tipo: gasto (ref. TipoDeCuenta) · nivel: maestra              │  │
│  │  estado: activa                                                │  │
│  │  (maestra — no posteable, solo agrupa)                         │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ CuentaContable #3 (Entidad)                                    │  │
│  │  codigo: 1524-01-001 · nombre: Maquinaria                      │  │
│  │  tipo: activo (ref. TipoDeCuenta) · nivel: auxiliar             │  │
│  │  estado: activa                                                │  │
│  │  obligatoriedadTercero: obligatorio (sobreescribe tipo)        │  │
│  │  obligatoriedadUndOrg: null (hereda del tipo)                  │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘

Catálogo interno: TipoDeCuenta (no es agregado — diccionario preconfigurado del producto)
┌──────────┬──────────────────────┬──────────────────────────┐
│ Tipo     │ Tercero (default)    │ Und. Org. (default)      │
├──────────┼──────────────────────┼──────────────────────────┤
│ gasto    │ obligatorio          │ obligatorio              │
│ costo    │ obligatorio          │ obligatorio              │
│ ingreso  │ obligatorio          │ obligatorio              │
│ CxP/CxC  │ obligatorio          │ obligatorio              │
│ activo   │ opcional             │ opcional                 │
│ banco    │ opcional             │ opcional                 │
│ patrimonio│ opcional            │ opcional                 │
└──────────┴──────────────────────┴──────────────────────────┘
```

**Eventos:** Patrón uniforme de configuración (ver Sección 5).

---

### 3.5. Agregado: MarcoContable (N1, configuración)

**Descripción:** Catálogo de marcos contables disponibles para una empresa. Cada marco identifica formalmente un esquema bajo el cual se diseña un PlanDeCuentas (NIIF, marcos locales, gerencial, consolidación, sectoriales, etc.). Vive por empresa. El producto precarga el marco NIIF al onboardear la empresa; un usuario con permiso especial puede crear marcos adicionales según las necesidades operativas.

**Raíz:** MarcoContable

**Atributos de la raíz:** codigo (string, único por empresa, estable, inmutable [I28][I29]), nombre (string descriptivo presentable al usuario), descripcion (string opcional con contexto del marco), estado (activo/inactivo).

**Ciclo de vida:** Configuración — sin FSM transaccional. El marco se crea una vez y se modifica, desactiva o reactiva.

**Stream de eventos:** `marco-contable-{empresa}-{codigo}`

**Composición:** Sin entidades internas. La raíz contiene todos los atributos.

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  MarcoContable (Agregado)                                            │
│                                                                      │
│  empresa: COSMOS-SAS                                                 │
│                                                                      │
│  codigo: NIIF                                                        │
│  nombre: Normas Internacionales de Información Financiera           │
│  descripcion: Marco contable principal de la empresa, donde se      │
│                registra toda la operación bajo NIIF                  │
│  estado: activo                                                      │
└──────────────────────────────────────────────────────────────────────┘

Marcos custom (creados por usuario con permiso especial cuando aplique):
┌──────────────────────────────────────────────────────────────────────┐
│  codigo: CONSOLIDACION_GRUPO  ·  nombre: Consolidación Grupo X     │
│  codigo: FISCAL_ALTERNO       ·  nombre: PUC Fiscal Alterno         │
│  codigo: SFC                  ·  nombre: PUC Superintendencia       │
│                                    Financiera de Colombia           │
└──────────────────────────────────────────────────────────────────────┘
```

**Política de catálogo:**

- **Predeterminado:** el marco `NIIF` se crea automáticamente al onboardear la empresa, junto con su PUC NIIF y los libros Principal y Fiscal predeterminados.
- **Custom:** un usuario con permiso especial puede crear marcos adicionales para casos como consolidación de grupo, fiscal alterno o sectores regulados (SFC, Supersalud, Supersolidaria).
- **Desactivación:** desactivar un MarcoContable previene crear nuevos PUCs sobre ese marco. **No hay cascada** sobre los PUCs existentes — siguen operando normalmente [coherente con R07].

**Eventos:** 4 eventos siguiendo el patrón uniforme de configuración (ver Sección 5).

**Justificación detallada:** ver `anexo-marco-contable-y-arquitectura-puc.md`.

---

### 3.6. Agregado: ReglaDeDerivacion (N1, configuración)

**Descripción:** Configuración que determina qué cuenta auxiliar corresponde a una combinación de dimensiones del hecho económico. Las reglas del Nivel A de la cadena de resolución. Cada regla ancla sus dimensiones estables (tipoTransaccion, tipoComponente, empresa) por igualdad exacta y un **texto ancla** que se empareja por similitud contra la clasificación de la línea [D15]. La regla se distingue del aprendizaje por gobernanza, no por mecánica: es explícita, inmutable y prevalece (al promover un aprendizaje a regla, el texto aprendido se copia como texto ancla).

**Raíz:** ReglaDeDerivacion

**Ciclo de vida:** Configuración — sin FSM transaccional.

**Stream de eventos:** `regla-de-derivacion-{id}`

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| Regla | Entidad | Una regla de derivación individual | dimensiones estables (tipoTransaccion, tipoComponente, empresa), textoAncla (texto de clasificación contra el que se empareja por similitud la línea entrante — copiado del aprendizaje al promover, o registrado por el analista contable), cuentaAuxiliar, estado (activa/inactiva) |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  ReglaDeDerivacion (Agregado)                                        │
│                                                                      │
│  empresa: COSMOS-SAS                                                 │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ Regla #1 (Entidad)                                             │  │
│  │  tipoTransaccion: causacion_gasto · tipoComponente: gasto      │  │
│  │  textoAncla: "Servicios de auditoría externa ·                 │  │
│  │               honorarios · servicios profesionales"            │  │
│  │  empresa: COSMOS-SAS · cuentaAuxiliar: 5110-05-002             │  │
│  │  estado: activa                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ Regla #2 (Entidad)                                             │  │
│  │  tipoTransaccion: causacion_gasto · tipoComponente: iva        │  │
│  │  textoAncla: "honorarios · iva 19%"                            │  │
│  │  empresa: COSMOS-SAS · cuentaAuxiliar: 2408-01-001             │  │
│  │  estado: activa                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ Regla #2 (Entidad)                                             │  │
│  │  tipoComponente: iva · clasificacion: IVA-19                   │  │
│  │  empresa: COSMOS-SAS · tipoTransaccion: causacion_gasto        │  │
│  │  cuentaAuxiliar: 2408-01-001 · estado: activa                 │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

**Eventos:** Patrón uniforme de configuración (ver Sección 5).

---

### 3.7. Agregado: PlantillaDeAsiento (N1, configuración)

**Descripción:** Estructura universal de roles (débitos/créditos) por tipo de transacción contable. Define qué partidas genera el borrador y con qué naturaleza. Contenido incluido en el producto.

**Raíz:** PlantillaDeAsiento

**Ciclo de vida:** Configuración — sin FSM transaccional.

**Stream de eventos:** `plantilla-de-asiento-{id}`

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| — | Raíz | Atributo de la raíz PlantillaDeAsiento | tipoTransaccion (clave natural de la plantilla) |
| RolPartida | Entidad | Un rol dentro de la plantilla | rol (código del rol — conjunto cerrado: GASTO, IMPUESTO, RETENCION, CONTRAPARTIDA, etc.; es la clave natural del rol dentro de la plantilla), naturaleza (debito/credito), esContrapartida (boolean) |
| ComponenteDelRol | VO | Cada tipo de componente que alimenta un rol, con su acotación de cuenta. Un rol alimentado por líneas tiene uno o más. | tipoComponente, grupoPucEsperado (lista de prefijos PUC de longitud variable), llevaDescripcionConcepto (boolean — si true, la partida resultante recibe la descripcionConcepto que envía el consumidor; ver [D13]), resolucionPorEspejo (opcional — rol del hecho relacionado cuya cuenta se copia; cuando está presente, el componente no se resuelve por la cadena sino por **espejo del hecho relacionado**, ver [D15] y paso 3 del ServicioDeTraduccion) |
| ConfiguracionPlantilla | VO | Configuración adicional de la plantilla | documentoFuenteObligatorio (boolean) |

**Sobre `grupoPucEsperado` y `ComponenteDelRol`:** Cada componente que alimenta un rol declara su `grupoPucEsperado` — los grupos del PUC (prefijos de código de cuenta, de longitud variable: clase, grupo o cuenta) a los que debe pertenecer la cuenta resuelta. El grupo vive en el componente, no en el rol, porque un mismo rol agrupa varios `tipoComponente` que caen en grupos distintos (ej: el rol RETENCION cubre `retefuente`→`2365` y `reteiva`→`2367`). Desde [D15] la contrapartida ya no es excepción: viaja como línea con `tipoComponente = contrapartida`, por lo que el rol CONTRAPARTIDA declara su `ComponenteDelRol` como cualquier otro. Detalle del concepto en [D12].

**Sobre `llevaDescripcionConcepto`:** Cada `ComponenteDelRol` declara si la partida que genera debe recibir la `descripcionConcepto` del hecho económico. Es `true` solo en los componentes que portan concepto de negocio (`gasto`, `concepto_devuelto`, `anticipo`) y `false` en impuestos y retenciones, cuya cuenta ya es autodescriptiva (el nombre de la cuenta basta). Evita repetir el mismo texto en partidas donde no aporta. Detalle en [D13].

**Sobre `resolucionPorEspejo`:** Los componentes que representan la contraparte contable de un hecho económico anterior deben aterrizar en la **misma cuenta** que usó ese hecho — de lo contrario el cruce contable no salda. Para ellos el `ComponenteDelRol` declara `resolucionPorEspejo` con el rol a espejar, y el motor copia la cuenta de la partida de ese rol en el borrador del hecho relacionado (la línea trae `referenciaHechoRelacionado`; la naturaleza Db/Cr la da esta plantilla, normalmente inversa a la original). Es conocimiento del producto, como la naturaleza. Componentes con espejo en el catálogo de OXP: `cruce_obligacion` → CONTRAPARTIDA de la causación cruzada; `partida_aclarada` → PARTIDA_POR_ACLARAR de la causación del extracto con la disputa; `amortizacion_anticipo` y `reversa_anticipo` → ANTICIPO del anticipo original; y en `nota_credito_gasto` **todos** los componentes espejan el rol homólogo de la causación devuelta (GASTO, IMPUESTO, RETENCION, CONTRAPARTIDA). El espejo precede a la cadena de resolución y no alimenta el aprendizaje. Detalle en [D15].

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  PlantillaDeAsiento (Agregado) — ejemplo: causacion_gasto            │
│                                                                      │
│  tipoTransaccion: causacion_gasto                                    │
│  (Inventario completo de 42 plantillas en                            │
│   anexo-ejemplo-plantilla-de-asiento.md, Sección 5.2)               │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ ConfiguracionPlantilla (VO)                                    │  │
│  │  documentoFuenteObligatorio: false                             │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ RolPartida #1 (Entidad)                                        │  │
│  │  rol: GASTO · naturaleza: debito · esContrapartida: false      │  │
│  │  ComponenteDelRol:                                             │  │
│  │   { tipoComponente: gasto · grupoPucEsperado:["51","52","53"]  │  │
│  │     · llevaDescripcionConcepto: true }                         │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ RolPartida #2 (Entidad)                                        │  │
│  │  rol: IMPUESTO · naturaleza: debito · esContrapartida: false   │  │
│  │  ComponenteDelRol:                                             │  │
│  │   { tipoComponente: iva · grupoPucEsperado: ["2408"]           │  │
│  │     · llevaDescripcionConcepto: false }                        │  │
│  │   { tipoComponente: inc · grupoPucEsperado: [...] (a validar)  │  │
│  │     · llevaDescripcionConcepto: false }                        │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ RolPartida #3 (Entidad)                                        │  │
│  │  rol: RETENCION · naturaleza: credito · esContrapartida: false │  │
│  │  ComponenteDelRol:                                             │  │
│  │   { tipoComponente: retefuente · grupoPucEsperado: ["2365"]    │  │
│  │     · llevaDescripcionConcepto: false }                        │  │
│  │   { tipoComponente: reteiva    · grupoPucEsperado: ["2367"]    │  │
│  │     · llevaDescripcionConcepto: false }                        │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ RolPartida #4 (Entidad)                                        │  │
│  │  rol: CONTRAPARTIDA · naturaleza: credito                      │  │
│  │  esContrapartida: true                                         │  │
│  │  ComponenteDelRol:                                             │  │
│  │   { tipoComponente: contrapartida                              │  │
│  │     · grupoPucEsperado: ["2205","2335"]                        │  │
│  │     · llevaDescripcionConcepto: false }                        │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

**Eventos:** Patrón uniforme de configuración (ver Sección 5).

---

**Nota sobre agregados N2 (3.7–3.11):** Los agregados de N2 representan la visión arquitectónica del sistema contable propio. Su especificación actual es suficiente para entender la integración con N1, pero se completa y refina cuando se inicie la construcción de F2. Varios puntos de operación avanzada (procesos automáticos de cierre, reclasificación) permanecen como pendientes (PD2, PD3).

### 3.8. Agregado: AsientoContable (N2)

**Descripción:** Registro contable inmutable generado a partir de un borrador resuelto. Solo existe cuando N2 está activo como destino. Es la fuente de verdad contable de la empresa.

**Raíz:** AsientoContable

**Ciclo de vida:** Transaccional (ES) — inmutable desde que nace. Sin FSM de transiciones — puede recibir el evento AsientoMarcadoComoAnulado como hecho posterior sin modificar sus partidas.

**Stream de eventos:** `asiento-contable-{id}`

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| PartidaContable | Entidad | Línea individual del asiento | cuenta (auxiliar), tercero, unidadOrganizacional, debito, credito |
| EncabezadoAsiento | VO | Datos del encabezado | comprobante, fecha, libro, periodo, tipoTransaccion, referenciaOrigen, documentoFuente, esAjusteDeCierre (boolean), referenciaHechoRelacionado (opcional — propagado desde el borrador para vincular devoluciones/notas crédito con el asiento original) |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  AsientoContable (Agregado)                                          │
│                                                                      │
│  condicion: VIGENTE (derivada del stream, no persistida — ver D5)     │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ EncabezadoAsiento (VO)                                         │  │
│  │  comprobante: CP-202603-0047 · fecha: 2026-03-15               │  │
│  │  libro: Principal · periodo: 2026-03                           │  │
│  │  tipoTransaccion: causacion_gasto                              │  │
│  │  referenciaOrigen: oxp-comercio-{id}/OxpComercioCausada        │  │
│  │  documentoFuente: OXP-COM-5678 · esAjusteDeCierre: false              │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ PartidaContable #1 (Entidad)                                   │  │
│  │  cuenta: 5110-05-002 · tercero: 900123456 · undOrg: VTA-001   │  │
│  │  debito: 600.000 · credito: 0                                 │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ PartidaContable #2 (Entidad)                                   │  │
│  │  cuenta: 5110-05-002 · tercero: 900123456 · undOrg: ADM-001   │  │
│  │  debito: 400.000 · credito: 0                                 │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ PartidaContable #3 (Entidad)                                   │  │
│  │  cuenta: 2205-01-001 · tercero: 900123456 · undOrg: —         │  │
│  │  debito: 0 · credito: 1.080.000                               │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

> **Nota — unidad organizacional de la contrapartida:** el `undOrg: —` de la partida de cuenta por pagar refleja **uno** de los modos configurables por empresa (consolidada sin unidad organizacional). Según `[I33]`, la misma contrapartida podría ir **distribuida** (replicando la distribución del gasto: dos partidas de CxP, VTA-001 y ADM-001) o **consolidada en una unidad general**. El comportamiento lo define la preferencia de la empresa, no es un valor fijo.

**Eventos:** 2 eventos propios (ver Sección 5).

**Nota sobre validaciones:** Las precondiciones de AsientoContabilizado (I15, I16, I26) y AsientoMarcadoComoAnulado (I18) se validan dentro de los domain services (ServicioDeContabilizacion, ServicioDeAnulacion) que operan sobre el stream del agregado.

---

### 3.9. Agregado: PeriodoContable (N2)

**Descripción:** Intervalo de tiempo (mensual) en el que se agrupan los asientos contables. Tiene ciclo de vida con apertura, cierre por niveles y cierre definitivo.

**Raíz:** PeriodoContable

**Ciclo de vida:** Transaccional (ES) — 3 estados: ABIERTO, CERRADO, CERRADO_DEFINITIVO (fase futura).

**Stream de eventos:** `periodo-contable-{id}`

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| EstadoPorComprobante | Entidad | Excepción de estado por tipo de comprobante | tipoComprobante, estado (abierto/cerrado) |

**Comportamiento calculado (no almacenado):**

| Método | Fórmula | Usado por |
|--------|---------|-----------|
| `estaAbiertoPara(tipoComprobante)` | Si hay excepción para ese tipo, usa el estado de la excepción. Si no, usa el estado general. | [R28] Cierre por niveles |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  PeriodoContable (Agregado)                                          │
│                                                                      │
│  empresa: COSMOS-SAS · periodo: 2026-03 · estado: ABIERTO           │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ EstadoPorComprobante #1 (Entidad) — excepción                  │  │
│  │  tipoComprobante: CP · estado: cerrado                         │  │
│  │  (CP cerrado aunque el periodo general está abierto)           │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ EstadoPorComprobante #2 (Entidad) — excepción                  │  │
│  │  tipoComprobante: CI · estado: cerrado                         │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  (CD no tiene excepción → hereda del estado general: ABIERTO)       │
└──────────────────────────────────────────────────────────────────────┘
```

**Eventos:** 6 eventos propios (ver Sección 5).

---

### 3.10. Agregado: LibroContable (N2, configuración)

**Descripción:** Configuración que define un conjunto de registros contables. Cada libro tiene un PUC asociado y un tipo que indica su rol operativo dentro de la empresa (Principal, Fiscal, Gerencial, Consolidación, etc.). El producto provee dos libros predeterminados al onboardear la empresa: **Principal** (donde se registra toda la operación bajo el PUC NIIF) y **Fiscal** (donde se registran los ajustes específicos para reportes fiscales). El analista contable puede configurar libros adicionales según las necesidades.

**Raíz:** LibroContable

**Ciclo de vida:** Configuración — sin FSM transaccional.

**Stream de eventos:** `libro-contable-{id}`

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| ConfiguracionLibro | VO | Datos del libro | tipo (texto descriptivo del rol del libro — predeterminados sugeridos por el producto: `Principal` y `Fiscal`; el analista contable puede definir tipos adicionales como `Gerencial`, `Consolidacion`, `Sectorial`, etc.), pucAsociado (referencia por id al agregado PlanDeCuentas), estado (activo/inactivo) |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  LibroContable (Agregado) — ejemplo: libro Principal                 │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ ConfiguracionLibro (VO)                                        │  │
│  │  tipo: Principal · pucAsociado: PUC NIIF                       │  │
│  │  estado: activo                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│  LibroContable (Agregado) — ejemplo: libro Fiscal                    │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ ConfiguracionLibro (VO)                                        │  │
│  │  tipo: Fiscal · pucAsociado: PUC NIIF                          │  │
│  │  estado: activo                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

En la arquitectura predeterminada moderna, ambos libros (Principal y Fiscal) apuntan al mismo PlanDeCuentas (PUC NIIF). Las diferencias entre tratamientos (NIIF vs ajustes fiscales) se modelan como asientos específicos del libro fiscal [R34], no como PUCs paralelos. Justificación detallada en `anexo-marco-contable-y-arquitectura-puc.md`.

**Eventos:** Patrón uniforme de configuración (ver Sección 5).

---

### 3.11. Agregado: NumeracionContable (N2, configuración)

**Descripción:** Secuencias de numeración por tipo de comprobante con dimensiones de segmentación configurables.

**Raíz:** NumeracionContable

**Ciclo de vida:** Configuración — sin FSM transaccional.

**Stream de eventos:** `numeracion-contable-{id}`

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| Secuencia | Entidad | Una secuencia de numeración | tipoComprobante, empresa, periodo, sucursal (dimensiones de segmentación), ultimoConsecutivo |

**Comportamiento calculado (no almacenado):**

| Método | Fórmula | Usado por |
|--------|---------|-----------|
| `siguienteConsecutivo(dimensiones)` | ultimoConsecutivo + 1 para la combinación de dimensiones | AsientoContabilizado [I16] [SI1] |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  NumeracionContable (Agregado)                                       │
│                                                                      │
│  empresa: COSMOS-SAS                                                 │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ Secuencia #1 (Entidad)                                         │  │
│  │  tipoComprobante: CP · empresa: COSMOS-SAS                     │  │
│  │  periodo: 2026-03 · sucursal: — · ultimoConsecutivo: 47        │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ Secuencia #2 (Entidad)                                         │  │
│  │  tipoComprobante: CI · empresa: COSMOS-SAS                     │  │
│  │  periodo: 2026-03 · sucursal: — · ultimoConsecutivo: 12        │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ Secuencia #3 (Entidad)                                         │  │
│  │  tipoComprobante: CD · empresa: COSMOS-SAS                     │  │
│  │  periodo: 2026-03 · sucursal: — · ultimoConsecutivo: 5         │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

**Eventos:** Patrón uniforme de configuración (ver Sección 5).

---

### 3.12. Agregado: EquivalenciaPuc (N2, configuración)

**Descripción:** Mapeo cuenta a cuenta entre dos planes de cuentas diferentes. Permite que los reportes contables reflejen un asiento registrado en un PUC con las cuentas equivalentes de otro PUC. La equivalencia se congela al momento de registrar las entradas en los reportes [R31].

**Nota sobre uso (arquitectura moderna predeterminada):** En la arquitectura predeterminada de una empresa moderna (un único PlanDeCuentas con MarcoContable NIIF compartido por los libros Principal y Fiscal), `EquivalenciaPuc` no se requiere — los libros usan las mismas cuentas. Este agregado se utiliza únicamente en casos excepcionales: empresas en transición de PUC local a NIIF, sectores regulados con PUC sectorial obligatorio, grupos empresariales con consolidación entre PUCs distintos, o empresas con PUC fiscal alterno. Como `LibroContable` y `EquivalenciaPuc` son capacidades de F2 — no de F1 — la necesidad efectiva de este agregado se evaluará al construir F2 con base en los casos reales que surjan. Justificación detallada en `anexo-marco-contable-y-arquitectura-puc.md`.

**Raíz:** EquivalenciaPuc

**Ciclo de vida:** Configuración — sin FSM transaccional.

**Stream de eventos:** `equivalencia-puc-{id}`

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| MapeoCuenta | Entidad | Equivalencia individual entre dos cuentas | cuentaOrigen, cuentaDestino |
| ConfiguracionEquivalencia | VO | Datos de la equivalencia | pucOrigen, pucDestino |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  EquivalenciaPuc (Agregado)                                          │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ ConfiguracionEquivalencia (VO)                                 │  │
│  │  pucOrigen: PUC Colombia · pucDestino: PUC NIIF               │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ MapeoCuenta #1 (Entidad)                                       │  │
│  │  cuentaOrigen: 5110-05-002 · cuentaDestino: 510105            │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ MapeoCuenta #2 (Entidad)                                       │  │
│  │  cuentaOrigen: 2408-01-001 · cuentaDestino: 240801            │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ MapeoCuenta #3 (Entidad)                                       │  │
│  │  cuentaOrigen: 2365-05-001 · cuentaDestino: 236505            │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

**Eventos:** Patrón uniforme de configuración (ver Sección 5).

---

### 3.13. Domain service: ServicioDeTraduccion (N1)

**Trigger:** Llegada de líneas de traducción de un sub-dominio consumidor.

**Flujo principal:**

| Paso | Acción | Evento emitido | Stream destino |
|------|--------|---------------|----------------|
| 1 | Validar unicidad de referenciaOrigen [R16]. Si ya existe un borrador con la misma referencia y está en PENDIENTE, se aplica R14 (reemplazo). Si ya no está en PENDIENTE, el motor rechaza el hecho económico con motivo estructurado y notifica al consumidor. | — (si reemplazo: BorradorReemplazado; si rechazo: notificación al consumidor con motivo, sin evento de dominio) | — |
| 2 | Identificar tipoTransaccion → seleccionar PlantillaDeAsiento. Validar que cada tipoComponente recibido en las líneas tenga al menos un RolPartida que lo cubra en la plantilla [I27], que **toda línea traiga clasificación no vacía** [I34] y que, si la plantilla tiene rol CONTRAPARTIDA, venga **exactamente una línea `contrapartida`** [I35]. | — (si no existe plantilla, hay líneas sin rol, líneas sin clasificación o falta la línea de contrapartida, no se crea borrador y se notifica al consumidor con motivo estructurado) | — |
| 3 | Para cada rol de la plantilla, resolver cuenta auxiliar. **Primero, espejo del hecho relacionado:** si el `ComponenteDelRol` declara `resolucionPorEspejo`, el motor busca su propio borrador del hecho relacionado (por `referenciaOrigen` = `referenciaHechoRelacionado` de la línea) y **copia la cuenta** de la partida del rol espejado — garantiza que el cruce contable salde en la misma cuenta [D15]. Si el borrador relacionado no existe (ej. saldos migrados de un sistema anterior) o tiene varias partidas de ese rol con cuentas distintas, la partida queda con cuenta null (borrador PENDIENTE) — nunca se resuelve por similitud, porque una cuenta distinta rompe el cruce; el espejo tampoco alimenta el aprendizaje. **Para los demás componentes, cadena:** Nivel A (ReglaDeDerivacion — partición estable exacta + similitud contra el textoAncla) → Nivel C (Aprendizaje — partición estable exacta + similitud contra los textos aprendidos, umbral [SI8]) → Nivel B (inferencia sobre PlanDeCuentas comparando la clasificación contra la descripción de las cuentas) [DD2] [D15]. El **Nivel B se acota al `grupoPucEsperado`** del componente que alimenta el rol: solo considera cuentas auxiliares cuyo código inicia por alguno de los prefijos declarados [D12]. En cada nivel, si la cuenta resuelta no está activa en el PUC, se descarta y se continúa al siguiente nivel [I24]. | — | — |
| 4 | Completar la partida de contrapartida a partir de la línea `contrapartida` que envía el consumidor: la línea trae **tercero** propio (proveedor en la causación de gasto, banco/emisor en el extracto — antes esto lo resolvía `terceroPrincipal`, que queda informativo) y **clasificación** (texto compuesto por el consumidor, ej. medio de pago + observación general), pero viaja **sin valor y sin unidad organizacional**: el **valor** lo calcula el motor como la diferencia entre débitos y créditos (es la única línea cuyo valor no viene del consumidor — garantiza el balance [R01]) y la **unidad organizacional** se asigna según la preferencia de la empresa [I33] (distribuida replicando las partidas de origen, consolidada en una unidad general, o sin unidad organizacional — respetando la obligatoriedad del PUC). La cuenta se resuelve en el paso 3 como cualquier otro componente (por cadena, o por espejo en `nota_credito_gasto`). Las partidas alimentadas por las demás líneas conservan el tercero de su propia línea. | — | — |
| 4b | Asignar narración: a cada partida cuyo componente tiene `llevaDescripcionConcepto = true`, se le copia la `descripcionConcepto` de la línea de traducción que la originó; las demás partidas quedan sin `descripcionConcepto`. La `descripcion` general del hecho económico (si el consumidor la envió) se traslada al encabezado del borrador; si no vino, el borrador queda sin `descripcion` [D13]. | — | — |
| 5 | Crear borrador con partidas resueltas o pendientes. Cada partida hereda el `rol` del `RolPartida` de la plantilla que la originó (GASTO/IMPUESTO/RETENCION/CONTRAPARTIDA) — queda registrado en la partida y se propaga a la entrega para que el destino (ej. SincoA&F) identifique las partidas tributarias [D14]. | BorradorCreado | borrador-contable-{id} |
| 6 | Si todas las cuentas resueltas y balancea → BorradorResuelto (derivado por transición) | BorradorResuelto | borrador-contable-{id} |

**Rechazos previos al borrador (motivos estructurados):**

Cuando el motor rechaza un hecho económico antes de crear el borrador (pasos 1 y 2), notifica al consumidor con un motivo estructurado. Estos rechazos no son eventos de dominio, no se persisten en stream propio del motor y no aparecen en la consola de contabilización — corresponden a fallos del contrato de entrada o a defectos de configuración del producto que se resuelven fuera del ciclo contable.

| Código | Cuándo ocurre | Información en la notificación | Quién resuelve |
|--------|---------------|---------------------------------|------------------|
| `REFERENCIA_ORIGEN_DUPLICADA_NO_REEMPLAZABLE` | Paso 1 — ya existe un borrador con la misma `referenciaOrigen` que no está en PENDIENTE [R14][R16]. | `referenciaOrigen`, estado actual del borrador existente, `referenciaDestino` si ya fue contabilizado. | Consumidor (idempotencia: reconoce que el hecho ya fue procesado). |
| `TIPO_TRANSACCION_SIN_PLANTILLA` | Paso 2 — no existe `PlantillaDeAsiento` para el `tipoTransaccion` recibido. | `referenciaOrigen`, `tipoTransaccion` recibido. | Equipo de producto (corregir catálogo de plantillas). |
| `LINEA_SIN_ROL_EN_PLANTILLA` | Paso 2 — al menos una línea trae un `tipoComponente` no cubierto por ningún `ComponenteDelRol` de la plantilla [I27]. | `referenciaOrigen`, `tipoTransaccion`, lista de `tipoComponente` no cubiertos. | Equipo de producto (ampliar plantilla) o consumidor (corregir contrato si envió un `tipoComponente` erróneo). |
| `LINEA_SIN_CLASIFICACION` | Paso 2 — al menos una línea llegó sin texto de clasificación [I34]. | `referenciaOrigen`, `tipoTransaccion`, lista de `tipoComponente` de las líneas sin clasificación. | Consumidor (corregir la composición de la clasificación, ver [D15]). |
| `LINEA_CONTRAPARTIDA_FALTANTE` | Paso 2 — la plantilla del `tipoTransaccion` tiene rol CONTRAPARTIDA pero el hecho económico no trae exactamente una línea `contrapartida` [I35]. | `referenciaOrigen`, `tipoTransaccion`. | Consumidor (emitir la línea de contrapartida del contrato, ver [D15]). |

La durabilidad del hecho económico mientras se resuelve la causa del rechazo es responsabilidad del consumidor emisor, que conserva el hecho en su propia bandeja de eventos hasta confirmar que fue procesado. El detalle del mecanismo técnico de notificación y reproceso está en [SI7].

**Nota sobre compensación:** El ServicioDeTraduccion opera en un solo stream (`borrador-contable-{id}`). Los pasos 1-4 son consultas sin efecto lateral. Los pasos 5-6 son un solo append atómico. No requiere tabla de compensación porque no coordina escrituras en múltiples streams. Los rechazos de los pasos 1 y 2 no crean borrador — se notifican al consumidor con uno de los motivos estructurados de la tabla anterior.

**Nota sobre la línea de traducción:** La línea de traducción es el contrato de entrada al motor — viene de fuera del bounded context. Su estructura está documentada en `anexo-ejemplo-plantilla-de-asiento.md`. No se modela como artefacto interno porque el ServicioDeTraduccion la consume y la transforma en un BorradorContable.

**Nota sobre por qué no es agregado:** A diferencia de EntregaContable (que tiene ciclo de vida propio con 3 estados y coordina dos streams), el ServicioDeTraduccion no tiene estado propio ni ciclo de vida. Sus pasos son consultas sobre agregados existentes y la única escritura va al stream del BorradorContable. Todo el comportamiento del ServicioDeTraduccion se materializa en el borrador que produce — no necesita identidad ni stream propio.

**CorrelationId:** `referenciaOrigen` — vincula la solicitud del consumidor con el borrador creado.

---

### 3.14. Agregado: SistemaContableDestino (N1, configuración)

**Descripción:** Registra qué sistema contable de destino está activo para una empresa. Cada cambio de destino queda como evento para trazabilidad. El Servicio de Entrega consulta este agregado para saber a dónde enviar los borradores resueltos. EntregaContable toma un snapshot de este agregado al momento de enviar.

**Raíz:** SistemaContableDestino

**Ciclo de vida:** Configuración — sin FSM transaccional.

**Stream de eventos:** `sistema-contable-destino-{id}`

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| ConfiguracionActiva | VO | Destino activo actualmente (se deriva del último evento SistemaContableDestinoConfigurado del stream) | destino (N2/SincoA&F/Siigo/Alegra), adaptador, fechaConfiguracion |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  SistemaContableDestino (Agregado)                                    │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ ConfiguracionActiva (VO) — se deriva del último evento         │  │
│  │  destino: SincoA&F · adaptador: AdaptadorSincoAF               │  │
│  │  fechaConfiguracion: 2026-01-01                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

**Eventos:** 1 evento — `SistemaContableDestinoConfigurado` (patrón uniforme de configuración, ver Sección 5).

---

### 3.15. Agregado: EntregaContable (N1)

**Descripción:** Registra el proceso de entrega de un borrador resuelto al sistema contable de destino. Segunda capacidad de N1. Gestiona la comunicación con el destino, registra el resultado (aceptación o rechazo) y alimenta la consola de contabilización. Cada borrador resuelto genera una EntregaContable.

**Raíz:** EntregaContable

**Ciclo de vida:** Transaccional (ES) — 3 estados: ENVIADO, ACEPTADO, RECHAZADO.

**Stream de eventos:** `entrega-contable-{id}`

Cada entrega tiene su propio stream que registra el ciclo completo: envío, resultado del destino y efecto sobre el borrador y los consumidores.

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| InformacionBorrador | VO | Referencia al borrador que se entrega | borradorId, referenciaOrigen, empresa, referenciaHechoRelacionado (opcional — se propaga al destino) |
| ConfiguracionDestino | VO | Sistema contable de destino configurado | destino (N2/SincoA&F/Siigo/Alegra), adaptador |
| ResultadoEntrega | VO | Resultado de la comunicación con el destino (si ya se recibió) | tipo (aceptado/rechazado), referenciaDestino (si aceptado), motivo (si rechazado), fechaResultado |

**Diagrama de composición:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  EntregaContable (Agregado)                                          │
│                                                                      │
│  estado: ACEPTADO                                                    │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ InformacionBorrador (VO)                                       │  │
│  │  borradorId: BRD-001                                           │  │
│  │  referenciaOrigen: oxp-comercio-{id}/OxpComercioCausada        │  │
│  │  empresa: COSMOS-SAS                                           │  │
│  │  referenciaHechoRelacionado: null (o ref al hecho original)   │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ ConfiguracionDestino (VO) — snapshot al momento de enviar      │  │
│  │  destino: SincoA&F · adaptador: AdaptadorSincoAF               │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ ResultadoEntrega (VO)                                          │  │
│  │  tipo: aceptado · referenciaDestino: CP-2022010265             │  │
│  │  fechaResultado: 2026-03-15                                    │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

**Trigger:** Evento BorradorResuelto.

**Flujo principal:**

| Paso | Acción | Evento emitido | Stream destino |
|------|--------|---------------|----------------|
| 1 | Escuchar BorradorResuelto | EntregaIniciada | entrega-contable-{id} |
| 2 | Seleccionar adaptador según destino configurado [R13] | — | — |
| 3 | Entregar borrador al destino en el formato que espera. El payload incluye, por cada partida, su `rol` (GASTO/IMPUESTO/RETENCION/CONTRAPARTIDA) además de cuenta, tercero, unidad organizacional y débito/crédito — permite que el destino (ej. SincoA&F) identifique las partidas tributarias [D14]. El mapeo del `rol` al formato específico de cada destino es responsabilidad del adaptador [PD1]. | — | — |
| 4a | Si destino acepta → registrar referencia del destino | EntregaAceptada | entrega-contable-{id} |
| 4b | Si destino rechaza → registrar motivo | EntregaRechazada | entrega-contable-{id} |
| 5a | Si aceptado → informar a consumidores | — | (consumidores escuchan EntregaAceptada) |
| 5b | Si rechazado → borrador vuelve a PENDIENTE | BorradorRechazadoPorDestino | borrador-contable-{id} |

**Tabla de compensación:**

| Paso | Evento | Stream | Si falla | Estrategia |
|------|--------|--------|----------|------------|
| 3 | (envío al destino) | — | Error de conectividad | Reintento automático con backoff. Si persiste → EntregaRechazada con motivo "error de comunicación". |
| 4a | EntregaAceptada | entrega-contable-{id} | Fallo al persistir | Reintento. El evento es idempotente. |
| 5b | BorradorRechazadoPorDestino | borrador-contable-{id} | Fallo al emitir | Reintento. El borrador permanece en RESUELTO hasta que se emita. |

**CorrelationId:** `borradorId` — vincula el borrador resuelto con su proceso de entrega.

**Consola de contabilización:** Se construye escuchando los eventos de `borrador-contable-{id}` y `entrega-contable-{id}`. La consola presenta dos niveles: estado del borrador (principal) y resultado de entrega (contexto). El estado del borrador se determina por el último evento de su stream. Los eventos de entrega aportan contexto complementario pero no reemplazan el estado del borrador.

| Evento fuente | Campos que aporta | Estado del borrador | Resultado de entrega |
|---------------|-------------------|---------------------|----------------------|
| BorradorCreado | referenciaOrigen, subDominioOrigen, documentoFuente, fecha | Pendiente | Sin entrega |
| BorradorResuelto | — | Resuelto | — |
| BorradorDescartado | — | Descartado | — |
| BorradorReemplazado | datos nuevos del consumidor | Pendiente | — (se reinicia) |
| EntregaIniciada | destino | — | Enviado |
| EntregaAceptada | referenciaDestino (consecutivo/comprobante) | — (sigue Resuelto) | Aceptado |
| EntregaRechazada | motivo del rechazo | — | Rechazado |
| BorradorRechazadoPorDestino | motivo, entregaId | Pendiente | — (contexto: último rechazo visible) |

---

### 3.16. Domain service: ServicioDeAnulacion (N2)

**Trigger:** El contador solicita anular un asiento contable.

**Flujo principal:**

| Paso | Acción | Evento emitido | Stream destino |
|------|--------|---------------|----------------|
| 1 | Validar que el asiento original está en condición VIGENTE | — | — |
| 2 | Crear asiento inverso (partidas invertidas, referencia al original) | AsientoContabilizado | asiento-contable-{id-inverso} |
| 3 | Marcar el asiento original como anulado | AsientoMarcadoComoAnulado | asiento-contable-{id-original} |

**Tabla de compensación:**

| Paso | Evento | Stream | Si falla | Estrategia |
|------|--------|--------|----------|------------|
| 2 | AsientoContabilizado (inverso) | asiento-contable-{id-inverso} | Fallo al persistir | Reintento. No se creó nada. |
| 3 | AsientoMarcadoComoAnulado | asiento-contable-{id-original} | Fallo después de crear el inverso | Reintento. El inverso existe pero el original no está marcado. Se reintenta hasta completar. |

**CorrelationId:** `asientoOriginalId` — vincula la solicitud de anulación con el asiento original y el asiento inverso generado.

**IdempotencyKey:** Un asiento solo puede tener un asiento inverso. Si ya existe un AsientoContabilizado con referenciaOrigen apuntando al asiento original como inverso, la solicitud se ignora.

---

### 3.17. Domain service: ServicioDeContabilizacion (N2)

**Trigger:** EntregaAceptada cuando el destino es N2.

**Flujo principal:**

| Paso | Acción | Evento emitido | Stream destino |
|------|--------|---------------|----------------|
| 1 | Validar periodo abierto para el tipo de comprobante [I15] | — (si cerrado → rechaza vía R28) | — |
| 2 | Asignar siguiente consecutivo | ConsecutivoAsignado | numeracion-contable-{id} |
| 3 | Crear asiento contable con consecutivo asignado | AsientoContabilizado | asiento-contable-{id} |

**Tabla de compensación:**

| Paso | Evento | Stream | Si falla | Estrategia |
|------|--------|--------|----------|------------|
| 2 | ConsecutivoAsignado | numeracion-contable-{id} | Fallo al persistir | Reintento. No se consumió consecutivo. |
| 3 | AsientoContabilizado | asiento-contable-{id} | Fallo después de asignar consecutivo | Reintento. Si persiste, el consecutivo queda consumido sin asiento — gap aceptable en la secuencia. Precondición: no existe asiento con la misma referenciaOrigen [I26]. |

**CorrelationId:** `entregaId` — vincula la aceptación de entrega con la asignación de consecutivo y creación de asiento.

**IdempotencyKey:** `entregaId` en ConsecutivoAsignado — si ya existe un consecutivo asignado para el mismo entregaId, la solicitud se ignora [SI1].

---

### 3.18. Sugerencias de implementación

#### [SI1] Concurrencia en asignación de consecutivos

La asignación de consecutivo (`siguienteConsecutivo` en NumeracionContable) es una operación crítica en concurrencia y replay. Cada comando de asignación de consecutivo incluye la versión esperada del stream de `numeracion-contable-{id}`; si la versión cambió (otro proceso asignó primero), el comando se rechaza con error de concurrencia [I16]. Adicionalmente, se recomienda idempotency key basada en `entregaId`: si ya existe un consecutivo asignado para el mismo entregaId, se ignora la solicitud (protección contra replay del ServicioDeContabilizacion) [I26].

#### [SI2] Concurrencia en resolución del borrador

La resolución de un borrador (CuentaResuelta, TerceroModificado, etc.) es una operación donde múltiples contadores podrían intentar intervenir el mismo borrador simultáneamente [R40]. Se recomienda optimistic concurrency sobre la versión del stream: cada comando de modificación incluye la versión esperada del stream; si la versión cambió (otro usuario escribió primero), el comando se rechaza con un error de concurrencia. Este mecanismo también garantiza que no coexista una edición con un intento de entrega [R41].

#### [SI3] Payload mínimo en BorradorResuelto

El Servicio de Entrega y otros consumidores escuchan BorradorResuelto para reaccionar. Se sugiere incluir `borradorId` y `referenciaOrigen` como payload para que los consumidores puedan reaccionar sin consultar el estado completo del borrador.

#### [SI4] Creación implícita del stream de Aprendizaje

El stream `aprendizaje-{id}` se crea implícitamente con el primer ResolucionAprendida — no existe un evento AprendizajeCreado. Se sugiere evaluar si esta convención es suficiente o si conviene agregar un evento de creación por consistencia con el patrón del resto de agregados.

#### [SI5] Manejo de fallos persistentes en procesos multi-agregado

Los domain services que coordinan escrituras en múltiples streams (ServicioDeContabilizacion, ServicioDeAnulacion, EntregaContable) tienen tablas de compensación con estrategia de reintento. Se sugiere evaluar una estrategia para fallos que persistan después de los reintentos: qué mecanismo detecta que un proceso quedó incompleto, cómo se notifica al equipo de operaciones, y cómo se resuelve manualmente si es necesario.

#### [SI6] Optimistic concurrency en EntregaContable

Se recomienda optimistic concurrency sobre la versión del stream de `entrega-contable-{id}` para garantizar que EntregaAceptada y EntregaRechazada no se apliquen sobre un stream cuya versión cambió.

#### [SI7] Tratamiento técnico de rechazos previos al borrador

Los rechazos del ServicioDeTraduccion en pasos 1 y 2 (motivos estructurados `REFERENCIA_ORIGEN_DUPLICADA_NO_REEMPLAZABLE`, `TIPO_TRANSACCION_SIN_PLANTILLA`, `LINEA_SIN_ROL_EN_PLANTILLA`, `LINEA_SIN_CLASIFICACION`, `LINEA_CONTRAPARTIDA_FALTANTE`) no son eventos de dominio. Se sugiere materializarlos sobre la infraestructura de mensajería con tres elementos:

1. **Respuesta negativa al bus (NACK) con dead-letter queue (DLQ):** el mensaje rechazado se redirige automáticamente a una cola de mensajes no procesados, con política de retención auditable (al menos 30 días sugeridos). Permite reproceso manual reinyectando los mensajes una vez corregido el defecto (plantilla ampliada, regla creada, etc.).
2. **Logs estructurados:** cada rechazo deja un registro con `referenciaOrigen`, `motivoCodigo`, `motivoDetalle`, `tipoTransaccion`, `subDominioOrigen`, `empresa`, `fechaRecepcion` y payload completo recibido. Soporta investigación forense fuera de la ventana del bus.
3. **Métricas y alertas:** la tasa de rechazos por motivo se publica como métrica operacional. Un spike de `LINEA_SIN_ROL_EN_PLANTILLA` o `TIPO_TRANSACCION_SIN_PLANTILLA` debe disparar alerta al equipo de producto, ya que corresponde a defectos de configuración del catálogo de plantillas.

La durabilidad del hecho económico mientras se resuelve el rechazo es responsabilidad del consumidor emisor mediante outbox pattern: cada sub-dominio consumidor conserva sus hechos económicos hasta confirmar procesamiento exitoso por el motor. La combinación outbox del consumidor + DLQ del bus garantiza que ningún hecho económico se pierda — sin necesidad de stream propio del motor para rechazos pre-borrador.

#### [SI8] Emparejamiento por similitud de la clasificación (Niveles A y C)

Los Niveles A y C resuelven en dos pasos: filtro exacto por las dimensiones estables (tipoTransaccion, tipoComponente, empresa) y emparejamiento **por similitud semántica** de la clasificación de la línea contra los textos ancla (reglas) o aprendidos (resoluciones) de esa partición [D15]. El equipo de desarrollo elige la técnica (representaciones vectoriales, búsqueda semántica u otras — coherente con la elegida para el Nivel B, [D6]) considerando:

1. **Umbral de resolución automática:** solo se resuelve si la mejor similitud supera un umbral de confianza; por debajo, se continúa al siguiente nivel de la cadena (y del Nivel B en adelante, borrador PENDIENTE). Definir el umbral con medición sobre datos reales, no a priori.
2. **Coincidencia exacta = similitud máxima:** como las clasificaciones se componen mecánicamente desde catálogos, la repetición de una compra produce el mismo texto — el emparejamiento debe garantizar que el texto idéntico siempre resuelve (conserva el comportamiento de [R12]).
3. **Desempate:** a igualdad de similitud entre candidatos con cuentas distintas, prevalece el más reciente (coherente con [I9]); si la ambigüedad persiste, no se resuelve automáticamente.
4. **El índice es por empresa y por partición** — nunca se compara contra textos de otra empresa, otro tipoComponente u otro tipoTransaccion.

---

### 3.19. Relaciones entre agregados

```
N1:
BorradorContable ──consulta──▶ PlanDeCuentas
BorradorContable ──consulta──▶ ReglaDeDerivacion (Nivel A)
BorradorContable ──consulta──▶ Aprendizaje (Nivel C)
BorradorContable ──consulta──▶ PlantillaDeAsiento
BorradorContable ──alimenta──▶ Aprendizaje (cuando el contador resuelve una cuenta)
EntregaContable ──consulta──▶ SistemaContableDestino (seleccionar adaptador)
EntregaContable ──snapshot──▶ ConfiguracionDestino (VO dentro de EntregaContable)

N2:
AsientoContable ──consulta──▶ PeriodoContable (validar periodo abierto)
AsientoContable ──consulta──▶ NumeracionContable (asignar consecutivo)
AsientoContable ──consulta──▶ LibroContable (libro asociado)
Reportes ──consulta──▶ EquivalenciaPuc (resolver equivalencia por libro de presentación)
```

---

## 4. Máquinas de estado

### 4.1. BorradorContable FSM

```
                         BorradorCreado
                              │
                    ┌─────────┴──────────┐
                    │                    │
              (faltan cuentas)    (todas resueltas + balancea)
                    │                    │ BorradorResuelto (derivado)
                    ▼                    ▼
                    ┌──────────────────────────────────────────┐
                    │ PENDIENTE                                 │
                    │                                           │
                    │  Eventos de progreso:                     │
                    │    · CuentaResuelta                       │
                    │    · TerceroModificado                    │
                    │    · UnidadOrganizacionalModificada       │
                    │    · FechaBorradorModificada              │
                    │    · MonedaBorradorModificada             │
                    │    · ValorPartidaModificado                │
                    │    · PartidaAgregada                      │
                    │    · PartidaEliminada                     │
                    │    · BorradorReemplazado                  │
                    │    · BorradorRechazadoPorDestino          │
                    │                                           │
                    ├───────────────────┬───────────────────────┤
                    │                   │                       │
                    │  BorradorResuelto │   BorradorDescartado  │
                    │                   │   (solo manuales)     │
                    ▼                   ▼                       │
          ┌──────────────┐    ┌──────────────────┐             │
          │  RESUELTO    │    │  DESCARTADO  ■   │             │
          │  (transitorio)│    └──────────────────┘             │
          └──────┬───────┘                                      │
                 │                                              │
                 ├── Se entrega al Servicio de Entrega          │
                 │   (resultado en EntregaContable)             │
                 │                                              │
                 └── Si destino rechaza ──▶ vuelve a PENDIENTE  │
                     (BorradorRechazadoPorDestino)              │
                                                                │
                    ◀───────────────────────────────────────────┘
```

**Notas:**
- **PENDIENTE** es el estado donde el contador interactúa. El borrador llega aquí por dos razones: (1) la traducción no resolvió todas las cuentas, o (2) el destino rechazó un borrador previamente resuelto.
- **RESUELTO** es transitorio — el borrador se entrega inmediatamente al Servicio de Entrega. El usuario casi nunca lo ve en este estado. El resultado de la entrega (aceptación o rechazo) se gestiona en EntregaContable — el borrador no tiene más estados después de RESUELTO. Si el destino rechaza, BorradorRechazadoPorDestino lo regresa a PENDIENTE.
- **DESCARTADO** es terminal. Solo aplica a borradores manuales (`esManual() == true`) desde estado PENDIENTE [R09][R10].
- Los eventos de progreso dentro de PENDIENTE representan las modificaciones granulares que el contador realiza sobre el borrador: resolución de cuentas, cambio de tercero, unidad organizacional, fecha o moneda. Cada modificación queda registrada individualmente para trazabilidad y auditoría.
- `BorradorRechazadoPorDestino` es un evento de progreso que registra el motivo del rechazo. El borrador ya estaba en PENDIENTE (volvió del rechazo).
- `BorradorReemplazado` registra la re-emisión del consumidor con la misma referenciaOrigen. Las partidas anteriores se eliminan y se sustituyen por las nuevas [R14][R15].
- `CuentaResuelta` alimenta el agregado de Aprendizaje (Nivel C de la cadena de resolución) [DD2].

### 4.2. PeriodoContable FSM

```
                    PeriodoCreado
                         │
                         ▼
          ┌──────────────────────────────────────┐
          │ CERRADO                               │
          │                                       │
          │  Eventos de progreso:                  │
          │    · ComprobanteAbierto                │
          │    · ComprobanteCerrado                │
          │                                       │
          └──────┬────────────────────┬───────────┘
                 │ PeriodoAbierto     │ PeriodoCerradoDefinitivamente
                 ▼                    ▼ (fase futura)
          ┌──────────────────────────────────────┐
          │ ABIERTO                               │
          │                                       │    ┌──────────────────────┐
          │  Eventos de progreso:                  │    │ CERRADO_DEFINITIVO ■ │
          │    · ComprobanteAbierto                │    └──────────────────────┘
          │    · ComprobanteCerrado                │
          │                                       │
          └───────────────┬───────────────────────┘
                          │ PeriodoCerrado
                          ▼
                    ┌────────────┐
                    │  CERRADO   │ (vuelve arriba)
                    └────────────┘
```

**Notas:**
- **ABIERTO** acepta nuevos asientos. Las excepciones por tipo de comprobante (`ComprobanteAbierto`, `ComprobanteCerrado`) son eventos de progreso — el periodo sigue abierto a nivel general pero puede tener tipos de comprobante cerrados [R28].
- **CERRADO** no acepta nuevos asientos (excepto en tipos de comprobante con excepción abierta). Se puede reabrir con `PeriodoAbierto` [D4]. Al cerrar, si hay borradores pendientes el sistema advierte [R29].
- **CERRADO_DEFINITIVO** es terminal e irreversible. Fase futura [R31].
- Los eventos `ComprobanteAbierto` y `ComprobanteCerrado` pueden ocurrir tanto en ABIERTO como en CERRADO — son las excepciones por tipo de comprobante que prevalecen sobre el estado general [R28].

### 4.3. AsientoContable — ciclo de vida

**Nota:** El AsientoContable no tiene un estado como atributo persistido — es inmutable desde que nace. Sin embargo, tiene un ciclo de vida de dos eventos que representan los momentos significativos de su existencia. Se documenta como FSM para hacer explícito un concepto que el negocio reconoce: un asiento contable puede estar vigente o haber sido anulado.

```
          ┌──────────────┐                    ┌──────────────────────┐
          │  VIGENTE     │── AsientoMarcado──▶│  ANULADO  ■          │
          │              │   ComoAnulado      │                      │
          └──────────────┘                    └──────────────────────┘
```

**Notas:**
- **VIGENTE** es la condición inicial del asiento. Nace con el evento `AsientoContabilizado`. Sus partidas, comprobante y datos son inmutables [R23].
- **ANULADO** indica que otro asiento contable (con partidas invertidas) contrarrestó este asiento. El evento `AsientoMarcadoComoAnulado` registra la referencia al asiento inverso. Las partidas del asiento original no cambian — solo se registra el hecho de que fue anulado.
- La condición vigente/anulado no es un campo del asiento — se deriva de la presencia o ausencia del evento `AsientoMarcadoComoAnulado` en su stream.

### 4.4. EntregaContable FSM

```
                EntregaIniciada
                      │
                      ▼
          ┌──────────────┐
          │  ENVIADO     │
          │              │
          └──────┬───────┘
            ┌────┴────┐
            │         │
  EntregaAceptada  EntregaRechazada
            │         │
            ▼         ▼
  ┌──────────────┐  ┌──────────────┐
  │  ACEPTADO ■  │  │ RECHAZADO ■  │
  └──────────────┘  └──────────────┘
```

**Notas:**
- **ENVIADO** es el estado inicial. Nace con `EntregaIniciada` cuando el Servicio de Entrega toma un borrador resuelto y lo envía al destino.
- **ACEPTADO** es terminal. El destino aceptó el borrador y retornó su referencia. Los consumidores se enteran del resultado.
- **RECHAZADO** es terminal. El destino rechazó el borrador. El borrador vuelve a PENDIENTE en N1 para que el contador decida la acción correctiva.
- Si hay error de conectividad (no rechazo de negocio), el Servicio de Entrega reintenta automáticamente antes de marcar como rechazado.

---

## 5. Catálogo de eventos

El bounded context de Contabilidad emite **55 eventos** distribuidos en 12 agregados y 3 domain services. Los 6 agregados de configuración siguen un patrón uniforme (crear agregado, agregar/modificar/inactivar/reactivar entidades internas) y se documentan en formato compacto. Los 3 agregados transaccionales y el servicio de entrega usan el template completo (Sección 2.2) porque tienen FSM, causalidad derivada y precondiciones complejas.

### 5.1. Resumen por agregado

| Agregado | Nivel | Tipo | Eventos |
|----------|:-----:|------|:-------:|
| BorradorContable | N1 | Transaccional | 13 |
| Aprendizaje | N1 | Transaccional | 3 |
| EntregaContable | N1 | Transaccional (ES) | 3 |
| AsientoContable | N2 | Transaccional | 2 |
| PeriodoContable | N2 | Transaccional | 6 |
| PlanDeCuentas | N1 | Configuración | 5 |
| MarcoContable | N1 | Configuración | 4 |
| ReglaDeDerivacion | N1 | Configuración | 5 |
| PlantillaDeAsiento | N1 | Configuración | 4 |
| LibroContable | N2 | Configuración | 4 |
| NumeracionContable | N2 | Configuración | 5 |
| SistemaContableDestino | N1 | Configuración | 1 |
| EquivalenciaPuc | N2 | Configuración | 4 |
| | | **Total** | **59** |

---

### 5.2. Eventos transaccionales

#### BorradorCreado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se creó un borrador contable a partir de las líneas de traducción de un consumidor o por creación manual del contador. |
| **Causalidad** | Directa (consumidor emite líneas o contador crea manualmente). |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | — (creación) |
| **Estado resultante** | PENDIENTE (si faltan cuentas por resolver) o RESUELTO (si la cadena resolvió todo y balancea — en este caso se emite BorradorResuelto como derivado por transición). |
| **Precondiciones** | Referencia de origen única [R16]. Datos maestros activos [R07]. Documento fuente según tipo de transacción [R08]. |
| **Información capturada** | Partidas (cuenta o null, tercero, unidadOrganizacional, debito, credito, nivelResolucion, rol, esContrapartida, clasificacion, descripcionConcepto), referenciaOrigen, subDominioOrigen, documentoFuente, descripcion, tipoTransaccion, empresa, moneda, fecha, terceroPrincipal (informativo). |
| **Efectos** | Si nace RESUELTO → emite BorradorResuelto (derivado por transición). Si nace de un consumidor → el borrador no es descartable [R09]. |

#### CuentaResuelta

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador asignó o cambió la cuenta auxiliar de una partida del borrador. |
| **Causalidad** | Directa (contador selecciona cuenta del PUC). |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE |
| **Estado resultante** | PENDIENTE (sin cambio de estado). |
| **Precondiciones** | Borrador en estado PENDIENTE. Cuenta debe ser auxiliar [R02] y activa [R07]. |
| **Información capturada** | partidaId, cuenta asignada, nivelResolucion (manual). |
| **Efectos** | Alimenta el agregado de Aprendizaje — emite ResolucionAprendida como efecto inter-agregado [DD2]. |

#### TerceroModificado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador cambió el tercero de una partida del borrador. |
| **Causalidad** | Directa (contador selecciona tercero). |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE |
| **Estado resultante** | PENDIENTE (sin cambio de estado). |
| **Precondiciones** | Borrador en estado PENDIENTE. Tercero debe estar activo [R07]. Obligatoriedad según tipo de cuenta [R04]. |
| **Información capturada** | partidaId, tercero nuevo. |
| **Efectos** | Ninguno adicional. |

#### UnidadOrganizacionalModificada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador cambió la unidad organizacional de una partida del borrador. |
| **Causalidad** | Directa (contador selecciona unidad organizacional). |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE |
| **Estado resultante** | PENDIENTE (sin cambio de estado). |
| **Precondiciones** | Borrador en estado PENDIENTE. Unidad organizacional debe estar activa [R07]. Obligatoriedad según tipo de cuenta [R04]. |
| **Información capturada** | partidaId, unidad organizacional nueva. |
| **Efectos** | Ninguno adicional. |

#### FechaBorradorModificada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador cambió la fecha del borrador. |
| **Causalidad** | Directa (contador modifica fecha). |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE |
| **Estado resultante** | PENDIENTE (sin cambio de estado). |
| **Precondiciones** | Borrador en estado PENDIENTE. |
| **Información capturada** | Fecha anterior, fecha nueva. |
| **Efectos** | Ninguno adicional. |

#### MonedaBorradorModificada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador cambió la moneda del borrador. |
| **Causalidad** | Directa (contador modifica moneda). |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE |
| **Estado resultante** | PENDIENTE (sin cambio de estado). |
| **Precondiciones** | Borrador en estado PENDIENTE. Moneda única [R03]. |
| **Información capturada** | Moneda anterior, moneda nueva. |
| **Efectos** | Ninguno adicional. |

#### ValorPartidaModificado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador modificó el valor (débito/crédito) de una partida del borrador. |
| **Causalidad** | Directa (contador modifica valor). |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE |
| **Estado resultante** | PENDIENTE (sin cambio de estado). |
| **Precondiciones** | Borrador en estado PENDIENTE. La partida resultante debe tener valor mayor a cero en débito o crédito, no en ambos [R06]. |
| **Información capturada** | partidaId, debitoAnterior, creditoAnterior, debitoNuevo, creditoNuevo. |
| **Efectos** | Ninguno adicional. |

#### PartidaAgregada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador agregó una nueva partida al borrador. |
| **Causalidad** | Directa (contador agrega partida). |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE |
| **Estado resultante** | PENDIENTE (sin cambio de estado). |
| **Precondiciones** | Borrador en estado PENDIENTE. Cuenta (si se asigna) debe ser auxiliar [R02] y activa [R07]. |
| **Información capturada** | Cuenta (o null), tercero, unidadOrganizacional, debito, credito, esContrapartida. |
| **Efectos** | Si se asigna cuenta, emite CuentaResuelta como evento derivado por transición, que a su vez alimenta el Aprendizaje [D2]. |

#### PartidaEliminada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador eliminó una partida del borrador. |
| **Causalidad** | Directa (contador elimina partida). |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE |
| **Estado resultante** | PENDIENTE (sin cambio de estado). |
| **Precondiciones** | Borrador en estado PENDIENTE. El borrador debe mantener al menos dos partidas después de la eliminación [R05]. |
| **Información capturada** | partidaId. |
| **Efectos** | Ninguno adicional. |

#### BorradorResuelto

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El borrador tiene todas las cuentas resueltas y balancea. Se entrega inmediatamente al Servicio de Entrega. |
| **Causalidad** | Derivado por transición (todas las cuentas resueltas + balance OK) o derivado de BorradorCreado si nació completo. |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE (o — si nació resuelto). |
| **Estado resultante** | RESUELTO (transitorio — se entrega inmediatamente). |
| **Precondiciones** | Todas las partidas tienen cuenta auxiliar asignada. Balance obligatorio [R01]. Mínimo dos partidas [R05]. Valor mayor a cero [R06]. |
| **Información capturada** | borradorId, referenciaOrigen [SI3]. |
| **Efectos** | El Servicio de Entrega escucha este evento y entrega al sistema contable de destino [DD6]. |

#### BorradorDescartado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador descartó un borrador manual. |
| **Causalidad** | Directa (contador descarta). |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE |
| **Estado resultante** | DESCARTADO (terminal). |
| **Precondiciones** | Borrador en estado PENDIENTE. Solo borradores manuales (`esManual() == true`) [R09][R10]. |
| **Información capturada** | Motivo del descarte (opcional). |
| **Efectos** | Visible en la consola de contabilización como descartado [R18]. |

#### BorradorRechazadoPorDestino

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable de destino rechazó el borrador. El borrador vuelve a PENDIENTE para que el contador decida la acción correctiva. |
| **Causalidad** | Efecto inter-agregado (Servicio de Entrega informa el rechazo al escuchar EntregaRechazada) [DD6]. |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE (nota: la transición RESUELTO→PENDIENTE ocurre de forma implícita al recibir el rechazo del destino. Este evento se emite ya en PENDIENTE y registra el motivo del rechazo). |
| **Estado resultante** | PENDIENTE (sin cambio de estado — registra el motivo del rechazo). |
| **Precondiciones** | El Servicio de Entrega informó un rechazo del destino [R19]. |
| **Información capturada** | entregaId (referencia a la EntregaContable), motivo del rechazo, fecha del rechazo, destino que rechazó. |
| **Efectos** | Visible en la consola de contabilización con el motivo [R19]. El contador puede corregir y reintentar [R20]. |

#### BorradorReemplazado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El consumidor re-emitió un hecho económico con la misma referencia de origen mientras el borrador estaba PENDIENTE. Toda la información del borrador se reemplaza con los nuevos datos: partidas, datos de transacción, documento fuente, tercero, moneda, fecha. Las resoluciones de cuentas realizadas por el contador sobre las partidas anteriores se pierden. |
| **Causalidad** | Directa (consumidor re-emite con misma referenciaOrigen). |
| **Agregado** | BorradorContable |
| **Nivel** | N1 |
| **Estado previo** | PENDIENTE |
| **Estado resultante** | PENDIENTE (sin cambio de estado — toda la información se reemplaza). |
| **Precondiciones** | Borrador en estado PENDIENTE [R14]. La referenciaOrigen coincide con la del borrador existente. Si el borrador ya no está en PENDIENTE, la re-emisión se rechaza. |
| **Información capturada** | Datos nuevos completos del consumidor: partidas (cuenta o null, tercero, unidadOrganizacional, debito, credito, nivelResolucion, rol, esContrapartida, clasificacion, descripcionConcepto), descripcion, tipoTransaccion, empresa, moneda, fecha, terceroPrincipal (informativo), documentoFuente. El estado anterior no se captura — se reconstruye del stream (ES). |
| **Efectos** | Las resoluciones de cuentas anteriores se pierden [R15]. El borrador vuelve a pasar por la cadena de resolución con los nuevos datos. |

---

#### ResolucionAprendida

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema registró la resolución de cuenta que el contador eligió para una combinación de dimensiones. En adelante, cuando llegue una línea de la misma partición estable con clasificación idéntica o suficientemente similar, el sistema resolverá automáticamente [SI8]. Las partidas resueltas por espejo del hecho relacionado no alimentan el aprendizaje [D15]. |
| **Causalidad** | Efecto inter-agregado (derivado de CuentaResuelta en BorradorContable) [DD2]. |
| **Agregado** | Aprendizaje |
| **Nivel** | N1 |
| **Estado previo** | — (sin FSM). |
| **Estado resultante** | — (sin FSM). |
| **Precondiciones** | Se emitió CuentaResuelta en un borrador. |
| **Información capturada** | dimensiones estables (tipoTransaccion, tipoComponente, empresa), clasificacion (texto semántico de la partida resuelta), cuentaAuxiliar, fechaAprendizaje. |
| **Efectos** | El Nivel C de la cadena de resolución usará esta resolución en futuros borradores, emparejando por similitud [R12] [SI8]. |

#### AprendizajePromovidoARegla

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El analista contable promovió una resolución aprendida a regla formal de derivación (Nivel A). |
| **Causalidad** | Directa (analista contable decide promover). |
| **Agregado** | Aprendizaje |
| **Nivel** | N1 |
| **Estado previo** | — (sin FSM). |
| **Estado resultante** | — (sin FSM). |
| **Precondiciones** | Existe una ResolucionAprendida para la partición estable y el texto aprendido. |
| **Información capturada** | dimensiones estables, clasificacion (texto aprendido que se copia como textoAncla de la regla), cuentaAuxiliar, reglaDeDerivacionCreada (referencia). |
| **Efectos** | Se crea una ReglaAgregada en el agregado ReglaDeDerivacion como efecto inter-agregado eventual [R12]. Si la creación de la regla falla, se reintenta — el evento AprendizajePromovidoARegla es idempotente (la misma partición estable con el mismo texto ancla produce la misma regla [I14]). |

#### AprendizajeInvalidado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El analista contable invalidó un aprendizaje erróneo. El aprendizaje invalidado no se aplica a futuros borradores. Los borradores ya resueltos con ese aprendizaje no se afectan. |
| **Causalidad** | Directa (analista contable decide invalidar). |
| **Agregado** | Aprendizaje |
| **Nivel** | N1 |
| **Estado previo** | — (sin FSM). |
| **Estado resultante** | — (sin FSM). |
| **Precondiciones** | Existe una ResolucionAprendida activa para la combinación de dimensiones. |
| **Información capturada** | combinacionDimensiones, cuentaAuxiliar invalidada, motivo (opcional), fechaInvalidacion. |
| **Efectos** | El Nivel C de la cadena de resolución deja de usar esta resolución en futuros borradores [R35]. |

---

#### EntregaIniciada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El Servicio de Entrega tomó un borrador resuelto y lo envió al sistema contable de destino configurado. |
| **Causalidad** | Efecto inter-agregado (derivado de BorradorResuelto) [DD6]. |
| **Agregado** | EntregaContable |
| **Nivel** | N1 |
| **Estado previo** | — (creación). |
| **Estado resultante** | ENVIADO. |
| **Precondiciones** | BorradorResuelto emitido. Destino configurado para la empresa [R13]. |
| **Información capturada** | borradorId, referenciaOrigen, empresa, destino, adaptador, referenciaHechoRelacionado (si aplica). |
| **Efectos** | Consola de contabilización se actualiza (borrador en tránsito hacia el destino). |

#### EntregaAceptada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable de destino aceptó el borrador y retornó su referencia. |
| **Causalidad** | Directa (respuesta del destino). |
| **Agregado** | EntregaContable |
| **Nivel** | N1 |
| **Estado previo** | ENVIADO |
| **Estado resultante** | ACEPTADO (terminal). |
| **Precondiciones** | BorradorResuelto fue entregado al destino. El destino respondió con aceptación. |
| **Información capturada** | referenciaOrigen, referenciaDestino (consecutivo, comprobante u otro identificador del destino), destino (nombre del sistema). |
| **Efectos** | Consumidores actualizan su referencia al asiento [R16][R17]. Consola de contabilización se actualiza [R18]. Cuando el destino es N2, además se emite AsientoContabilizado en el stream del AsientoContable. |

#### EntregaRechazada

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El sistema contable de destino rechazó el borrador. |
| **Causalidad** | Directa (respuesta del destino). |
| **Agregado** | EntregaContable |
| **Nivel** | N1 |
| **Estado previo** | ENVIADO |
| **Estado resultante** | RECHAZADO (terminal). |
| **Precondiciones** | BorradorResuelto fue entregado al destino. El destino respondió con rechazo. |
| **Información capturada** | referenciaOrigen, motivo del rechazo, destino (nombre del sistema). |
| **Efectos** | BorradorRechazadoPorDestino se emite en el stream del borrador (efecto inter-agregado). Consola de contabilización se actualiza [R19]. |

---

#### AsientoContabilizado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se creó un asiento contable inmutable en N2 a partir de un borrador resuelto. |
| **Causalidad** | Efecto inter-agregado (derivado de EntregaAceptada cuando el destino es N2). |
| **Agregado** | AsientoContable |
| **Nivel** | N2 |
| **Estado previo** | — (creación). |
| **Estado resultante** | VIGENTE. |
| **Precondiciones** | N2 activo como destino. Periodo abierto para el tipo de comprobante [R30]. Numeración disponible [R24]. |
| **Información capturada** | Comprobante, fecha, libro, periodo, tipoTransaccion, referenciaOrigen, documentoFuente, esAjusteDeCierre, referenciaHechoRelacionado (si aplica), partidas (cuenta, tercero, unidadOrganizacional, debito, credito). |
| **Efectos** | Reportes contables se actualizan (auxiliar contable y saldos contables). Equivalencia de PUC se congela al registrar en los reportes [R31]. |

#### AsientoMarcadoComoAnulado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Otro asiento contable (con partidas invertidas) contrarrestó este asiento. Se registra el hecho de la anulación sin modificar las partidas originales. |
| **Causalidad** | Efecto inter-agregado (derivado de la contabilización del asiento inverso). |
| **Agregado** | AsientoContable |
| **Nivel** | N2 |
| **Estado previo** | VIGENTE. |
| **Estado resultante** | ANULADO. |
| **Precondiciones** | El asiento está en condición VIGENTE. Existe un asiento inverso contabilizado que referencia a este asiento [R23]. |
| **Información capturada** | Referencia al asiento inverso (comprobante), fecha de anulación. |
| **Efectos** | El asiento pasa a condición ANULADO. Las partidas no cambian. |

---

#### PeriodoCreado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Se creó un periodo contable. Nace en estado CERRADO. |
| **Causalidad** | Directa (sistema crea automáticamente al inicio de operación [R26] o al confirmar creación de nuevos periodos). |
| **Agregado** | PeriodoContable |
| **Nivel** | N2 |
| **Estado previo** | — (creación). |
| **Estado resultante** | CERRADO. |
| **Precondiciones** | — |
| **Información capturada** | Empresa, periodo (año-mes). |
| **Efectos** | Ninguno adicional. El periodo queda cerrado hasta que el analista lo abra. |

#### PeriodoAbierto

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El analista contable abrió un periodo cerrado para operación. Cubre tanto la primera apertura como la reapertura después de un cierre. |
| **Causalidad** | Directa (analista contable abre). |
| **Agregado** | PeriodoContable |
| **Nivel** | N2 |
| **Estado previo** | CERRADO. |
| **Estado resultante** | ABIERTO. |
| **Precondiciones** | Periodo en estado CERRADO (no CERRADO_DEFINITIVO) [R31]. |
| **Información capturada** | — |
| **Efectos** | El periodo acepta nuevos asientos. |

#### PeriodoCerrado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador cerró el periodo. |
| **Causalidad** | Directa (contador cierra). |
| **Agregado** | PeriodoContable |
| **Nivel** | N2 |
| **Estado previo** | ABIERTO. |
| **Estado resultante** | CERRADO. |
| **Precondiciones** | Periodo en estado ABIERTO. Si hay borradores pendientes, el sistema advierte [R29]. |
| **Información capturada** | — |
| **Efectos** | El periodo no acepta nuevos asientos (excepto en tipos de comprobante con excepción abierta [R26]). |

#### PeriodoCerradoDefinitivamente

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El periodo se cerró de forma irreversible. Fase futura. |
| **Causalidad** | Directa (fase futura). |
| **Agregado** | PeriodoContable |
| **Nivel** | N2 |
| **Estado previo** | CERRADO. |
| **Estado resultante** | CERRADO_DEFINITIVO (terminal). |
| **Precondiciones** | Periodo en estado CERRADO [R31]. |
| **Información capturada** | Motivo del cierre definitivo. |
| **Efectos** | El periodo no puede reabrirse bajo ninguna circunstancia. |

#### ComprobanteAbierto

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador abrió una excepción para un tipo de comprobante específico dentro del periodo. |
| **Causalidad** | Directa (contador abre excepción). |
| **Agregado** | PeriodoContable |
| **Nivel** | N2 |
| **Estado previo** | ABIERTO o CERRADO. |
| **Estado resultante** | Sin cambio de estado general. |
| **Precondiciones** | Periodo no está en CERRADO_DEFINITIVO. |
| **Información capturada** | Tipo de comprobante. |
| **Efectos** | El tipo de comprobante acepta nuevos asientos independientemente del estado general del periodo [R26]. |

#### ComprobanteCerrado

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | El contador cerró un tipo de comprobante específico dentro del periodo. |
| **Causalidad** | Directa (contador cierra excepción). |
| **Agregado** | PeriodoContable |
| **Nivel** | N2 |
| **Estado previo** | ABIERTO o CERRADO. |
| **Estado resultante** | Sin cambio de estado general. |
| **Precondiciones** | Periodo no está en CERRADO_DEFINITIVO. |
| **Información capturada** | Tipo de comprobante. |
| **Efectos** | El tipo de comprobante no acepta nuevos asientos independientemente del estado general del periodo [R26]. |

---

### 5.3. Eventos de configuración

Los eventos de configuración siguen un patrón uniforme: el agregado se crea una vez y las entidades internas se agregan, modifican, inactivan o reactivan. No hay FSM transaccional — todos los eventos aplican desde cualquier punto del ciclo de vida del agregado.

#### 5.3.1. PlanDeCuentas — 5 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `PlanDeCuentasCreado` | Se creó el PUC para una empresa, asociado a un MarcoContable. | Empresa, nombre, marcoContable (referencia al código del MarcoContable). | [I31] [I32] |
| 2 | `CuentaAgregada` | Se registró una nueva cuenta en el PUC. | Codigo, nombre, tipo (gasto/costo/ingreso/activo/pasivo/patrimonio/banco), nivel (maestra/auxiliar), obligatoriedadTercero, obligatoriedadUnidadOrganizacional. | [R02] [R04] |
| 3 | `CuentaModificada` | Se actualizaron atributos de una cuenta existente. | Codigo (identifica), campos modificados. | — |
| 4 | `CuentaInactivada` | Una cuenta dejó de estar disponible para nuevos registros. Se conserva para trazabilidad histórica. | Codigo, motivo. | [R07] |
| 5 | `CuentaReactivada` | Una cuenta previamente inactivada volvió a estar disponible. | Codigo. | — |

#### 5.3.2. MarcoContable — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `MarcoContableCreado` | Se creó un marco contable para una empresa. Nace activo por defecto. El marco NIIF se crea automáticamente al onboardear la empresa; otros marcos los crea un usuario con permiso especial. | empresa, codigo, nombre, descripcion. | [I28] [I29] |
| 2 | `MarcoContableModificado` | Se actualizaron atributos descriptivos del marco (nombre, descripción). El código no cambia. | codigo (identifica), campos modificados. | [I29] |
| 3 | `MarcoContableDesactivado` | El marco dejó de estar disponible para crear nuevos PlanDeCuentas. Los PUCs existentes que lo referencian no se afectan. | codigo, motivo. | — |
| 4 | `MarcoContableReactivado` | El marco previamente desactivado volvió a estar disponible. | codigo. | — |

#### 5.3.3. ReglaDeDerivacion — 5 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `ReglaDeDerivacionCreada` | Se creó el conjunto de reglas de derivación. | Empresa. | — |
| 2 | `ReglaAgregada` | Se registró una nueva regla de derivación (Nivel A). Nace activa por defecto. | dimensiones estables (tipoTransaccion, tipoComponente, empresa), textoAncla, cuentaAuxiliar. | [DD2] [D15] |
| 3 | `ReglaModificada` | Se actualizó una regla existente. | combinacionDimensiones (identifica), cuentaAuxiliar nueva. | — |
| 4 | `ReglaInactivada` | Una regla dejó de aplicarse. Se conserva para trazabilidad. | combinacionDimensiones, motivo. | — |
| 5 | `ReglaReactivada` | Una regla previamente inactivada volvió a aplicarse. | combinacionDimensiones. | — |

#### 5.3.4. PlantillaDeAsiento — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `PlantillaDeAsientoCreada` | Se creó una plantilla para un tipo de transacción contable. | TipoTransaccion, documentoFuenteObligatorio. | [R08] |
| 2 | `RolPartidaAgregado` | Se registró un nuevo rol dentro de la plantilla. | rol (código), naturaleza (debito/credito), esContrapartida, componentes (lista de `{ tipoComponente, grupoPucEsperado, llevaDescripcionConcepto }`), grupoPucEsperado (a nivel de rol — solo cuando esContrapartida). | — |
| 3 | `RolPartidaModificado` | Se actualizaron atributos de un rol existente. | rol (identifica), campos modificados. | — |
| 4 | `RolPartidaEliminado` | Se eliminó un rol de la plantilla. | rol. | — |

#### 5.3.5. LibroContable — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `LibroContableCreado` | Se creó un libro contable. | Tipo (texto descriptivo del rol del libro — predeterminados sugeridos: `Principal`, `Fiscal`), pucAsociado (referencia por id al PlanDeCuentas). | [R32] [R46] |
| 2 | `LibroModificado` | Se actualizaron atributos del libro. | Tipo (identifica), campos modificados. | — |
| 3 | `LibroInactivado` | Un libro dejó de estar disponible. Se conserva para trazabilidad. | Tipo, motivo. | — |
| 4 | `LibroReactivado` | Un libro previamente inactivado volvió a estar disponible. | Tipo. | — |

#### 5.3.6. NumeracionContable — 5 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `NumeracionCreada` | Se creó la configuración de numeración. | Empresa. | — |
| 2 | `SecuenciaAgregada` | Se registró una nueva secuencia de numeración. | TipoComprobante, empresa, periodo, sucursal (dimensiones), formatoConsecutivo. | [R24] |
| 3 | `SecuenciaModificada` | Se actualizaron atributos de una secuencia existente. | Dimensiones (identifican), campos modificados. | — |
| 4 | `SecuenciaInactivada` | Una secuencia dejó de generar consecutivos. | Dimensiones, motivo. | — |
| 5 | `ConsecutivoAsignado` | Se asignó el siguiente consecutivo a un asiento contable. Efecto inter-agregado derivado de AsientoContabilizado. | Dimensiones, consecutivoAsignado, asientoContableId. | [I15] |

#### 5.3.7. SistemaContableDestino — 1 evento

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `SistemaContableDestinoConfigurado` | Se configuró o cambió el sistema contable de destino para una empresa. El destino activo es el último evento del stream. Precondición: no existen EntregaContable en estado ENVIADO ni borradores en PENDIENTE con rechazo previo del destino actual para la empresa [I25]. | Empresa, destino (N2/SincoA&F/Siigo/Alegra), adaptador, fechaConfiguracion. | [R13] [R42] [I25] |

#### 5.3.8. EquivalenciaPuc — 4 eventos

| # | Evento | Descripción | Información capturada | Reglas |
|:---:|---|---|---|---|
| 1 | `EquivalenciaPucCreada` | Se creó el mapeo de equivalencia entre dos PUCs. | pucOrigen, pucDestino. | [R32] |
| 2 | `MapeoCuentaAgregado` | Se registró una equivalencia entre dos cuentas. | CuentaOrigen, cuentaDestino. | — |
| 3 | `MapeoCuentaModificado` | Se actualizó la cuenta destino de un mapeo existente. | CuentaOrigen (identifica), cuentaDestino nueva. | — |
| 4 | `MapeoCuentaEliminado` | Se eliminó un mapeo de equivalencia. | CuentaOrigen. | — |

---

## 6. Tipos de transacción y plantillas

Los tipos de transacción contable determinan qué plantilla de asiento aplica y con qué estructura de roles (débitos/créditos). Cada sub-dominio consumidor emite líneas de traducción con un tipo de transacción que el motor usa para seleccionar la plantilla correspondiente.

El inventario completo de tipos de transacción y sus plantillas está documentado en `anexo-ejemplo-plantilla-de-asiento.md`, Sección 5.2, con 42 plantillas en 8 sub-dominios emisores. La **especificación detallada por plantilla** (roles, componentes y `grupoPucEsperado`, lista para precarga) vive en `datos-precargados/plantillas-de-asiento.*` — a la fecha cubre las 6 plantillas de OXP; las demás se precargan al modelar cada sub-dominio emisor.

| Sub-dominio emisor | Plantillas | Ejemplos principales |
|---------------------|:----------:|---------------------|
| OXP | 6 | Causación de obligación, nota crédito proveedor, anticipo a proveedor |
| CXC | 7 | Causación de ingreso, nota crédito/débito cliente, anticipo de cliente |
| Tesorería | 8 | Pago a proveedor, cobro de cliente, transferencia, cargo bancario |
| Inventarios | 6 | Entrada de mercancía, salida por venta (CMV), ajuste de inventario |
| Activos Fijos | 6 | Depreciación, baja/retiro, revaluación, capitalización |
| Nómina | 3 | Causación de nómina, aportes patronales, provisión prestaciones |
| Arrendamientos (NIIF 16) | 3 | Reconocimiento inicial ROU, depreciación ROU, interés sobre pasivo |
| Contabilidad (GL) | 3 | Asiento manual, cierre de periodo, apertura |

Cada plantilla define roles con naturaleza fija (débito/crédito) que son conocimiento universal del producto — no los configura el usuario. Lo que varía por empresa es a qué cuenta auxiliar específica va cada rol, que se resuelve mediante la cadena de resolución [DD2] — o por **espejo del hecho relacionado** en los componentes que la plantilla marca con `resolucionPorEspejo` [D15].

---

## 7. Invariantes del dominio

| # | Invariante | Agregado | Clasificación | Referencia |
|---|-----------|----------|---------------|------------|
| I1 | La suma de débitos debe ser igual a la suma de créditos en todo borrador resuelto. | BorradorContable | Local | [R01] |
| I2 | Las partidas solo pueden asignarse a cuentas auxiliares, nunca a cuentas maestras. | BorradorContable | Local | [R02] |
| I3 | Todas las partidas de un borrador deben estar en una única moneda. | BorradorContable | Local | [R03] |
| I4 | La obligatoriedad de tercero y unidad organizacional se valida según el tipo de cuenta, con posibilidad de sobreescritura por cuenta auxiliar. Se valida mediante consulta a PlanDeCuentas. | BorradorContable, PlanDeCuentas | Eventual | [R04] |
| I5 | Todo borrador debe tener al menos dos partidas — un débito y un crédito. | BorradorContable | Local | [R05] |
| I6 | Toda partida debe tener un valor mayor a cero en débito o crédito. | BorradorContable | Local | [R06] |
| I7a | Un borrador solo puede usar cuentas auxiliares en estado activo. | BorradorContable | Local | [R07] |
| I7b | Un borrador solo puede usar terceros y unidades organizacionales en estado activo. El estado de estos datos maestros se valida contra la **copia local** de la señal que cada sub-dominio dueño publica por suscripción (Terceros: señal global; Estructura Organizacional: eventos de ciclo de vida de unidades) — **sin consulta en caliente** (replanteamiento jun-2026). La validación se aplica al momento de asignar o modificar — si el dato se inactiva después, el borrador conserva la referencia existente. | BorradorContable | Eventual | [R07] |
| I8 | La referencia de origen es única en N1. No pueden existir dos borradores con la misma referencia. Si el consumidor re-emite con la misma referencia y el borrador está PENDIENTE, N1 reemplaza las partidas (BorradorReemplazado) — no crea un segundo borrador. Si el borrador ya no está PENDIENTE, la re-emisión se rechaza. | BorradorContable | Local | [R14] [R15] [R16] |
| I9 | Para una misma partición de dimensiones estables con un texto de clasificación idéntico, la resolución más reciente del Aprendizaje prevalece. No se eliminan las anteriores — se acumulan y la última es la vigente. Textos distintos dentro de la misma partición coexisten como candidatos del emparejamiento por similitud [SI8]. | Aprendizaje | Local | [R12] [D15] |
| I10 | Un borrador generado desde un consumidor no puede descartarse. | BorradorContable | Local | [R09] |
| I11 | Un borrador manual solo puede descartarse desde estado PENDIENTE. | BorradorContable | Local | [R10] |
| I12 | Si la plantilla de asiento define el documento fuente como obligatorio, el borrador debe tener documento fuente no vacío para poder resolverse. | BorradorContable | Local | [R08] |
| I13 | Para un mismo borrador solo puede existir una entrega en curso (ENVIADO). Un borrador puede acumular múltiples entregas a lo largo de su vida (ej: primera entrega rechazada, segunda aceptada). Se valida mediante consulta (read model o proyección) sobre EntregaContable filtrando por borradorId y estado ENVIADO. Las entregas finalizadas (ACEPTADO, RECHAZADO) no bloquean nuevas entregas. | EntregaContable, BorradorContable | Eventual | [DD6] |
| I14 | No pueden existir dos reglas activas con la misma partición de dimensiones estables y el mismo textoAncla en ReglaDeDerivacion. Es el mecanismo de idempotencia para AprendizajePromovidoARegla y el control del repetido de reglas. | ReglaDeDerivacion | Local | [R12] [DD2] [D15] |
| I15 | N2 no acepta asientos en periodos cerrados para el tipo de comprobante correspondiente. La acción ante periodo cerrado (rechazo o redirección al mes siguiente) depende de la configuración de la empresa. El enforcement ocurre en ServicioDeContabilizacion que consulta PeriodoContable antes de escribir en AsientoContable. | PeriodoContable, AsientoContable | Eventual | [R28] [R30] |
| I16 | El consecutivo de un asiento contable no puede repetirse dentro de la misma combinación de dimensiones de segmentación. El enforcement ocurre en ServicioDeContabilizacion que consulta NumeracionContable (paso 2) antes de crear AsientoContable (paso 3). | NumeracionContable, AsientoContable | Eventual | [R24] [SI1] |
| I17 | Los asientos generados como ajuste de cierre deben tener esAjusteDeCierre = true. El tipo de transacción contable determina si un asiento es de ajuste de cierre. | AsientoContable | Local | [R25] |
| I18 | Un asiento contable nunca se modifica ni se elimina. | AsientoContable | Local | [R23] |
| I19 | El estado a nivel de tipo de comprobante prevalece sobre el estado general del periodo. | PeriodoContable | Local | [R28] |
| I20 | Un periodo en estado CERRADO_DEFINITIVO no puede reabrirse. | PeriodoContable | Local | [R31] |
| I21 | Cada libro contable tiene exactamente un PUC asociado. | LibroContable | Local | [R32] |
| I22 | La equivalencia entre PUCs se congela al momento de registrar las entradas en los reportes. Los cambios posteriores no afectan entradas ya registradas. Esta invariante se garantiza al construir la entrada del reporte — el proceso que escucha AsientoContabilizado resuelve la equivalencia y la persiste como dato inmutable de la entrada. | EquivalenciaPuc | Eventual | [R31] |
| I23 | Los cambios en configuración (reglas, PUC, plantillas) aplican solo a borradores nuevos. Los borradores existentes conservan la resolución con la que fueron traducidos. Se garantiza por diseño: el ServicioDeTraduccion consume la configuración vigente al momento de traducir — no hay re-evaluación retroactiva. | ReglaDeDerivacion, PlanDeCuentas, PlantillaDeAsiento | Eventual | [R37] [D8] |
| I24 | Si una cuenta se inactiva en el PUC, los aprendizajes y reglas que la referencian permanecen registrados pero no se aplican a nuevos borradores. Los borradores ya resueltos con esa cuenta no se afectan. | Aprendizaje, ReglaDeDerivacion | Eventual | [R39] [R07] |
| I25 | El sistema contable de destino no puede cambiarse mientras existan entregas en estado ENVIADO o borradores en PENDIENTE que fueron rechazados por el destino actual. Se valida como precondición al modificar SistemaContableDestino mediante consulta a EntregaContable y BorradorContable. | SistemaContableDestino, EntregaContable, BorradorContable | Eventual | [R42] |
| I26 | No pueden existir dos asientos contables con la misma referenciaOrigen en N2 (excepto el par original/inverso por anulación). Se valida como precondición en el ServicioDeContabilizacion paso 3 antes de emitir AsientoContabilizado. | AsientoContable | Eventual | [R24] |
| I27 | Toda línea de traducción recibida debe corresponder a al menos un RolPartida en la plantilla del tipoTransaccion. Si una o más líneas traen un tipoComponente no cubierto por ningún rol, el motor rechaza el hecho económico completo antes de crear el borrador y notifica al consumidor con motivo estructurado. La validación se aplica en el paso 2 del ServicioDeTraduccion. | ServicioDeTraduccion, PlantillaDeAsiento | Local | [DD2] [R45] |
| I28 | El código de un MarcoContable es único por empresa. No pueden coexistir dos marcos con el mismo código dentro del catálogo de una empresa. | MarcoContable | Local | [R46] |
| I29 | El código de un MarcoContable es inmutable tras la creación del marco. Solo nombre y descripción admiten modificación posterior. | MarcoContable | Local | [R46] |
| I30 | Una empresa no puede tener dos PlanDeCuentas referenciando el mismo MarcoContable. Cada marco activo en la empresa puede asociarse a lo sumo a un PUC. | PlanDeCuentas, MarcoContable | Eventual | [R46] |
| I31 | El MarcoContable referenciado por un PlanDeCuentas debe estar activo al momento de crear el PUC. La creación de un PUC sobre un marco desactivado se rechaza. | PlanDeCuentas, MarcoContable | Eventual | [R46] |
| I32 | El MarcoContable referenciado por un PlanDeCuentas es inmutable tras la creación del PUC. Cambiar el marco semánticamente cambia la naturaleza del PUC; en ese caso se crea un PUC nuevo. | PlanDeCuentas | Local | [R46] |
| I33 | **(configurable por empresa)** La asignación de la unidad organizacional en la partida de contrapartida tipo cuenta por pagar depende de la configuración de la empresa. Tres comportamientos posibles: (a) **distribuida** — replica la distribución de las partidas con unidad organizacional que originan la obligación (ej. el gasto en la causación); (b) **consolidada** en una unidad organizacional general; o (c) **sin unidad organizacional**. Aplica a toda contrapartida tipo cuenta por pagar — por ejemplo, en `causacion_gasto`, `nota_credito_gasto` y `anticipo_a_proveedor` (la línea `contrapartida` viaja sin unidad organizacional por esta razón, ver paso 4). Cuando la cuenta exige unidad organizacional en el PUC (`obligatoriedadUnidadOrganizacional`), el modo "sin unidad organizacional" no aplica. El mismo criterio aplica a las partidas tipo cuenta por pagar **alimentadas por línea con valor** (ej. el cruce `cruce_obligacion` del extracto, `[D29 de OXP]`), que deben rendir su unidad organizacional con la misma política para espejar la CxP de la causación original y netear. La forma de almacenar y resolver esta preferencia es decisión de implementación. | ServicioDeTraduccion, PlanDeCuentas | Eventual | [R50] |
| I34 | Toda línea de traducción debe traer clasificación no vacía — el texto semántico es insumo obligatorio de la resolución confiable de cuentas (el grupo del PUC admite muchas cuentas válidas; sin texto no hay confiabilidad). Si una o más líneas llegan sin clasificación, el motor rechaza el hecho económico completo antes de crear el borrador y notifica al consumidor con motivo estructurado. La validación se aplica en el paso 2 del ServicioDeTraduccion. | ServicioDeTraduccion | Local | [D15] |
| I35 | Si la plantilla del tipoTransaccion tiene rol CONTRAPARTIDA, el hecho económico debe traer exactamente una línea `contrapartida` (con tercero y clasificación, sin valor ni unidad organizacional). Si falta o llegan varias, el motor rechaza el hecho económico completo antes de crear el borrador y notifica al consumidor con motivo estructurado. La validación se aplica en el paso 2 del ServicioDeTraduccion. | ServicioDeTraduccion, PlantillaDeAsiento | Local | [D15] |

**Clasificación:**
- **Local:** Se valida dentro de un solo agregado, en la misma transacción.
- **Eventual:** Cruza fronteras de agregado o depende de datos externos (terceros, unidades organizacionales). Se garantiza mediante consultas o procesos asíncronos.

---

## 8. Qué NO contiene este documento

| Concepto | Razón | Referencia |
|----------|-------|------------|
| Gestión de terceros | Sub-dominio independiente. N1 recibe la referencia del tercero en las líneas de traducción. | Sub-dominio de Terceros |
| Gestión de unidades organizacionales | Sub-dominio independiente. N1 recibe la unidad organizacional en las líneas de traducción. | Sub-dominio de Estructura Organizacional |
| Cálculo tributario | Sub-dominio independiente. Los tributos llegan como componentes dentro de las líneas de traducción. | Sub-dominio de Impuestos |
| Distribución por unidad organizacional | Responsabilidad del consumidor. N1 recibe los valores ya distribuidos. | `definicion-alcance.md`, Sección 7 |
| Numeración fiscal por resolución | Responsabilidad del sub-dominio emisor del documento (CXC, Facturación). | `definicion-alcance.md`, Sección 7 |
| Procesamiento de pagos | Responsabilidad de Tesorería / SincoA&F. | `definicion-alcance.md`, Sección 7 |
| Reportes de información fiscal | Responsabilidad del sub-dominio de Impuestos. | `definicion-alcance.md`, Sección 7 |
| Datos base del ERP | Catálogos de países, monedas, tipos de documento. Responsabilidad del servicio de datos de referencia (`compartido/datos-referencia/`). | `definicion-alcance.md`, Sección 7 |
| Detalle de adaptadores por destino | Los adaptadores (SincoA&F, Siigo, Alegra) se documentan como implementaciones del Servicio de Entrega, no como modelo de dominio. | [DD5] |
| Estructura de los reportes contables | El auxiliar contable y saldos contables son proyecciones documentadas en su propio anexo. | `anexo-proyecciones-contables.md` |

---

## 9. Decisiones de arquitectura y diseño

Las decisiones previas (DD1-DD11) están documentadas en `anexo-decisiones-de-diseno.md` y se referencian como `[DD##]` en este documento. A continuación las decisiones que emergieron durante el modelado:

| # | Decisión | Justificación | Referencia |
|---|----------|---------------|------------|
| D1 | EquivalenciaPuc es un agregado propio de N2, no vive dentro de PlanDeCuentas (N1). | La equivalencia solo se usa en N2 (reportes multi-libro). Mantener el PUC limpio en N1 sin datos de N2. | [DD9] [R30] [R31] |
| D2 | Los eventos de modificación del borrador son granulares (CuentaResuelta, TerceroModificado, etc.) en vez de un evento genérico PartidaModificada. | Aprovecha la auditoría natural de ES. El volumen no es problema — solo el 5-10% de los borradores pasa por PENDIENTE. | Sección 5.2 |
| D3 | EntregaAceptada y AsientoContabilizado son eventos diferentes emitidos por emisores diferentes. | EntregaAceptada (Servicio de Entrega) siempre existe independiente del destino. AsientoContabilizado (AsientoContable) solo existe cuando N2 está activo. Evita confusión y duplicidad. | [DD6] |
| D4 | PeriodoAbierto cubre tanto la primera apertura como la reapertura. No hay evento PeriodoReabierto separado. | La transición es la misma (CERRADO → ABIERTO). El contexto se diferencia por el estado previo. | Sección 4.2 |
| D5 | El AsientoContable no tiene estado como atributo persistido. La condición vigente/anulado se deriva de la presencia del evento AsientoMarcadoComoAnulado. | Respeta la inmutabilidad del asiento. El ciclo de vida se documenta como FSM para hacer explícito un concepto que el negocio reconoce. | [R23] Sección 4.3 |
| D6 | La inferencia inteligente (Nivel B de la cadena de resolución) debe aprovechar capacidades de IA (modelos de lenguaje, RAG, similitud semántica u otras técnicas) para sugerir la cuenta más probable. El índice se construye por empresa al cargar el PUC — cada empresa tiene su propio espacio de búsqueda. La técnica menos esperada es representar esto con CRUDs de reglas anidadas — el valor del producto es la categorización contable automática con IA, eliminando los catálogos manuales de miles de reglas que existen hoy. El equipo de desarrollo investiga y propone el enfoque que mejor se adapte, considerando: precisión mínima esperada en las cuentas más frecuentes por empresa, capacidad de mejorar con el uso y velocidad de respuesta aceptable para la operación. Si el Nivel B no encuentra cuenta con confianza suficiente, la partida queda con cuenta null y el borrador nace PENDIENTE. La sugerencia del Nivel B no alimenta automáticamente el Aprendizaje — solo la confirmación explícita del contador lo hace. | [DD2] [R12] |
| D7 | La re-emisión del hecho económico por el consumidor solo se permite cuando el borrador está en estado PENDIENTE. N1 reemplaza toda la información del borrador existente con los nuevos datos (partidas, datos de transacción, documento fuente, tercero, moneda, fecha — toda la información que el consumidor envía en las líneas de traducción). Si el borrador ya fue resuelto, entregado o descartado, la re-emisión se rechaza. La corrección post-entrega se resuelve con un nuevo hecho económico del consumidor (devolución, nota crédito) — N1 no soporta reemplazo de borradores ya entregados. | [R14] [R15] [I8] |
| D8 | Los cambios en configuración (reglas de derivación, plan de cuentas, plantillas de asiento, aprendizajes) aplican solo a borradores nuevos — nunca retroactivamente. Los borradores existentes conservan la resolución con la que fueron traducidos. La alternativa descartada (re-evaluar borradores pendientes con la nueva configuración) se rechazó porque: (1) un borrador que el contador ya intervino perdería sus resoluciones manuales, (2) el volumen de re-evaluación sería impredecible, y (3) cambiar silenciosamente un borrador pendiente viola la confianza del operador. Si una cuenta se inactiva, los aprendizajes y reglas que la referencian permanecen registrados pero no se aplican a nuevos borradores — los borradores ya resueltos no se afectan. | [R37] [R38] [R39] [I23] [I24] |
| D9 | N1 tiene autoridad plena sobre el borrador — el contador puede modificar cualquier campo en estado PENDIENTE sin bloqueo ni matriz de restricción. Los campos se categorizan según su impacto [R43]: corrección contable natural (cuenta, tercero, unidad organizacional) y campos que afectan el hecho económico (valor, moneda, documento fuente, partidas). Cuando el contador modifica campos que afectan el hecho económico, el sistema advierte que la mejor práctica es solicitar al consumidor la re-emisión o un nuevo hecho [R44] — la advertencia no bloquea. Esta postura reconoce que el consumidor puede haber finalizado su ciclo de vida y no puede re-emitir, por lo que crear un bloqueo generaría un cuello de botella en la contabilización. La protección se garantiza por: (1) el borrador de consumidor no puede descartarse [R09], (2) re-emisión disponible en PENDIENTE [R14], (3) advertencia del sistema [R44], (4) cada modificación queda como evento individual (ES) con trazabilidad completa. Alternativa descartada: matriz de campos editables por sub-estado o bloqueo de campos sensibles. | [R09] [R14] [R43] [R44] [D2] |
| D10 | Los agregados de N2 (Secciones 3.8–3.12) se especifican a nivel suficiente para entender la integración con N1. Su refinamiento completo se ejecuta al iniciar la construcción de F2. Varios puntos de operación avanzada (procesos automáticos de cierre, reclasificación) permanecen como pendientes (PD2, PD3). | Sección 8 del alcance |
| D11 | Arquitectura PUC único + libros paralelos como caso típico moderno. Una empresa típica al onboardear opera con un PlanDeCuentas (PUC NIIF) y dos libros predeterminados (Principal y Fiscal) sobre el mismo PUC. Las diferencias entre tratamientos (NIIF vs ajustes fiscales) se modelan como asientos específicos del libro [R34], no como PUCs paralelos. El agregado MarcoContable identifica formalmente el PUC mediante un código estructurado (NIIF predeterminado + custom bajo demanda). El atributo `tipo` del LibroContable es texto libre con predeterminados Principal/Fiscal — el analista contable puede crear libros con tipos adicionales (Gerencial, Consolidación, sectoriales) según necesite la empresa. EquivalenciaPuc permanece para casos excepcionales (transición a NIIF, sectores regulados con PUC sectorial obligatorio, grupos empresariales con consolidación). Decisión basada en investigación de seis ERPs modernos (SAP S/4HANA, Oracle Fusion, Dynamics 365, NetSuite, Workday, Sage) que convergen hacia "Chart of Accounts único + ledgers paralelos sobre el mismo COA". | [R34] [R46] `anexo-marco-contable-y-arquitectura-puc.md` |
| D12 | **Grupo del PUC esperado para acotar la inferencia.** Cada componente que alimenta un rol de la plantilla (`ComponenteDelRol`) declara `grupoPucEsperado`: una lista de uno o más prefijos del código PUC, de **longitud variable** (clase = 1 dígito, grupo = 2, cuenta = 4). El **Nivel B** (inferencia) solo considera cuentas auxiliares cuyo código inicia por alguno de los prefijos. La profundidad refleja qué tan determinístico es el componente: estables a 4 dígitos (`iva`→`["2408"]`, `retefuente`→`["2365"]`, `reteiva`→`["2367"]`); variables según la clasificación a grupos de 2 dígitos (`gasto`→`["51","52","53"]`). El grupo vive en el componente —no en el rol— porque un rol agrupa varios `tipoComponente` que caen en grupos distintos. Desde [D15] la contrapartida dejó de ser excepción: viaja como línea con `tipoComponente = contrapartida` y su `ComponenteDelRol` declara el grupo como cualquier otro (`["2205","2335"]` en `causacion_gasto`; originalmente se declaraba a nivel del rol porque el motor la generaba sin `tipoComponente`). `grupoPucEsperado` **no reemplaza la cadena de resolución**: el mapeo fino a la cuenta exacta lo siguen haciendo el Nivel A (reglas) y el Nivel C (aprendizaje) mediante el emparejamiento por similitud de la clasificación [D15]; el grupo solo orienta el Nivel B. Es obligatorio en todos los roles. Alternativa descartada: grupo por (rol × clasificación) — se rechazó porque la clasificación no vive en la plantilla (llega en cada línea como texto semántico) y duplicaría la función de la cadena de resolución. | [D6] [DD2] [R47] [D15] |
| D13 | **Narración del borrador: descripción general + descripción de concepto por partida.** El borrador admite dos textos de narración, ambos **enviados por el consumidor** (no compuestos por el motor en esta fase): (1) `BorradorContable.descripcion` — descripción general del hecho económico, a nivel de encabezado; opcional, si el consumidor no la envía queda vacía. (2) `PartidaBorrador.descripcionConcepto` — narración del movimiento individual, que el motor asigna **solo** a las partidas cuyo `ComponenteDelRol` tiene `llevaDescripcionConcepto = true`. Este flag se declara en la plantilla y es `true` únicamente en los componentes que portan concepto de negocio (`gasto`, `concepto_devuelto`, `anticipo`) y `false` en impuestos y retenciones, cuya cuenta ya es autodescriptiva — evita repetir el mismo texto donde no aporta. La asignación ocurre en el paso 4b del ServicioDeTraduccion. Alternativa diferida (no en esta fase): que el motor **componga** la `descripcion` general a partir de las descripciones de concepto cuando el consumidor no la envíe. | [R48] |
| D14 | **La partida del borrador hereda el `rol` de la plantilla y lo propaga a la entrega.** Cada `PartidaBorrador` registra el `rol` (código de conjunto cerrado: GASTO/IMPUESTO/RETENCION/CONTRAPARTIDA) del `RolPartida` de la plantilla que la originó, asignado en el paso 5 del ServicioDeTraduccion. Antes, la partida solo heredaba `esContrapartida` (booleano), perdiendo la distinción entre impuesto, retención y gasto; como las cuentas se resuelven dinámicamente, el `rol` es la marca confiable del tipo de partida. El `rol` se propaga en el payload de `EntregaContable` para que el sistema contable de destino (caso **SincoA&F**) identifique las partidas tributarias y les dé tratamiento fiscal. El `rol` es un **código** (no texto descriptivo); por consistencia, el atributo del rol en `RolPartida` se renombró de `nombre` a `rol`, y `esContrapartida` se conserva como atajo equivalente a `rol == CONTRAPARTIDA`. El requisito específico de SincoA&F sobre los impuestos se confirma al implementar su adaptador [PD1]. No se propaga a N2 en esta fase (F2). | [R49] [PD1] |
| D15 | **Clasificación como texto semántico de emparejamiento, contrapartida como línea y resolución por espejo del hecho relacionado (issue #104).** Cierra la zona gris sobre el contenido de `clasificacion` en el contrato de la línea de traducción. **(1) Clasificación = texto semántico, no código ni llave:** cada consumidor la compone **mecánicamente** por componente a partir de datos de sus catálogos (OXP: p. ej. `gasto` = descripción del concepto + concepto de pago + clasificación tributaria; las recetas por componente viven en el modelo del emisor). No la digita un usuario. Es **obligatoria en toda línea** [I34]: el grupo del PUC admite muchas cuentas válidas y sin texto no hay resolución confiable. Los Niveles A y C la emparejan **por similitud** dentro de la partición exacta de dimensiones estables (tipoTransaccion, tipoComponente, empresa) [SI8]; el Nivel B la compara contra las descripciones de las cuentas del PUC [D6]. El control del repetido de reglas y aprendizajes se ancla en las dimensiones estables + el texto [I9] [I14]. Alternativa descartada: código de catálogo normalizado — inútil para comparar contra la descripción de la cuenta contable y rompería la simetría entre las tres capas, que resuelven sobre el mismo dato recibido. **(2) La contrapartida viaja como línea** (`tipoComponente = contrapartida`, canónico): trae tercero propio y clasificación compuesta por el consumidor (p. ej. medio de pago + observación general), sin valor ni unidad organizacional — el motor calcula el valor como balance de débitos/créditos y rinde la unidad según [I33]. Obligatoria cuando la plantilla tiene el rol [I35]. Así Contabilidad no conoce los campos internos de cada consumidor y cada consumidor incorpora los atributos que maneje para componer su clasificación. `terceroPrincipal` (issue #28) se **conserva como informativo** del hecho económico — deja de ser la fuente del tercero de la contrapartida. **(3) Resolución por espejo del hecho relacionado:** los componentes que representan la contraparte contable de un hecho anterior (`cruce_obligacion`, `partida_aclarada`, `amortizacion_anticipo`, `reversa_anticipo` y todos los componentes de `nota_credito_gasto`) deben aterrizar en la **misma cuenta** que usó ese hecho para que el cruce salde. Su `ComponenteDelRol` declara `resolucionPorEspejo` (rol a espejar) y la línea trae `referenciaHechoRelacionado`; el motor copia la cuenta de la partida del rol espejado en su propio borrador del hecho relacionado. El espejo precede a la cadena, no alimenta el aprendizaje y ante ausencia o ambigüedad del hecho relacionado deja la partida sin cuenta (borrador PENDIENTE) — nunca adivina por similitud. | [I34] [I35] [SI8] [D6] [D12] [I33] Sección 3.13 |

---

## 10. Premisas de negocio

| # | Premisa | Impacto en el modelo |
|---|---------|---------------------|
| P1 | El consumidor emite un hecho económico una sola vez. Excepcionalmente puede re-emitir con la misma referencia si el borrador aún está PENDIENTE — N1 reemplaza toda la información del borrador [R14][R15]. Si el borrador ya no está PENDIENTE, la re-emisión se rechaza [D7]. | El borrador recorre el flujo completo sin intervención del consumidor salvo re-emisión en PENDIENTE. Si el destino rechaza, la corrección es responsabilidad de N1 (contador). Post-entrega, el consumidor corrige con un nuevo hecho económico (devolución, nota crédito). |
| P2 | Solo un sistema contable de destino activo por empresa. | El Servicio de Entrega envía a un solo destino. No hay entrega simultánea a múltiples destinos. [DD5] |
| P3 | El Motor de Traducción no valida periodos del sistema contable de destino. | N1 siempre traduce. El destino decide si acepta o rechaza. [DD7] |
| P4 | La elección del sistema contable de destino es una decisión de administración del sistema, no de operación contable. | No es responsabilidad del analista contable ni del contador. No aparece en los flujos de configuración contable. |
| P5 | Los libros contables Principal y Fiscal vienen preestablecidos en el producto al onboardear la empresa, ambos asociados al PlanDeCuentas con MarcoContable NIIF predeterminado. | El analista contable puede agregar libros adicionales (Gerencial, Consolidación, sectoriales u otros tipos custom) según las necesidades de la empresa. Justificación detallada en `anexo-marco-contable-y-arquitectura-puc.md`. |
| P6 | Las plantillas de asiento y reglas de derivación vienen preestablecidas en el producto. | El analista contable solo agrega excepciones, no construye desde cero. |
| P7 | El histórico contable no debe distorsionarse por cambios posteriores en la configuración de equivalencia entre PUCs. | La invariante I22 garantiza esta premisa congelando la equivalencia al momento de registrar. [R31] |

---

## 11. Pendientes por definir

| # | Pendiente | Contexto | Trigger de activación |
|---|-----------|----------|----------------------|
| PD1 | Detalle de los adaptadores por destino (SincoA&F, Siigo, Alegra). | El Servicio de Entrega necesita un adaptador por cada destino. El formato y protocolo de cada uno se define al implementar. | Inicio de F1 (adaptador SincoA&F). |
| PD2 | Cierre definitivo de periodos (CERRADO_DEFINITIVO). | Definido como fase futura (F2+). Requiere definir quién puede ejecutarlo y qué controles tiene. | Inicio de F2+. |
| PD3 | Asientos de reclasificación automática al cierre de periodo. | Mencionado en el Flujo 6 paso 3 del alcance. La plantilla "cierre de periodo" (Sección 6, inventario de 42 plantillas) define la estructura de roles del asiento. Lo pendiente es la lógica del proceso automático: cuándo se genera, qué cuentas de resultado se trasladan al resultado del ejercicio y cómo se coordina con el cierre del periodo (R26). | Inicio de F2. |

---

## 12. Catálogo de permisos atómicos del dominio

Cada bounded context declara los recursos que protege y las acciones que expone como permisos atómicos. La plataforma de seguridad del ERP consume este catálogo para integrarlo a su modelo de autorización (roles, políticas, relaciones).

**Lo que define este catálogo:**
- **Recursos protegidos** — agregados del dominio que requieren control de acceso.
- **Acciones por recurso** — operaciones de negocio que se pueden proteger, identificadas con la convención `accion_recurso`.
- **Restricciones de contexto** — dimensiones que limitan el acceso más allá de la acción.

**Lo que NO define este catálogo:**
- Roles (responsabilidad de la plataforma de seguridad).
- Asignación de usuarios a permisos.
- Mecanismo de autenticación o enforcement.

**Convención de naming:** `accion_recurso` en snake_case. Compatible con OAuth scopes, policy engines (OPA, Cedar) y motores ReBAC (SpiceDB, OpenFGA).

| Recurso | Acción | Nivel | Identificador |
|---------|--------|:-----:|---------------|
| BorradorContable | Resolver cuenta | N1 | `resolver_borrador` |
| BorradorContable | Modificar campos | N1 | `modificar_borrador` |
| BorradorContable | Descartar | N1 | `descartar_borrador` |
| BorradorContable | Reintentar entrega | N1 | `reintentar_borrador` |
| BorradorContable | Consultar | N1 | `consultar_borrador` |
| ReglaDeDerivacion | Configurar | N1 | `configurar_regla` |
| PlanDeCuentas | Configurar | N1 | `configurar_puc` |
| Aprendizaje | Promover a regla | N1 | `promover_aprendizaje` |
| Aprendizaje | Invalidar | N1 | `invalidar_aprendizaje` |
| AsientoContable | Registrar manual | N2 | `registrar_asiento` |
| AsientoContable | Anular | N2 | `anular_asiento` |
| AsientoContable | Consultar | N2 | `consultar_asiento` |
| PeriodoContable | Abrir | N2 | `abrir_periodo` |
| PeriodoContable | Cerrar | N2 | `cerrar_periodo` |
| LibroContable | Configurar | N2 | `configurar_libro` |
| NumeracionContable | Configurar | N2 | `configurar_numeracion` |
| SistemaContableDestino | Configurar | N1 | `configurar_destino` |

**Restricción de contexto:** el acceso a todos los recursos se restringe por empresa. La plataforma de seguridad evalúa la empresa del usuario contra la empresa del recurso.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Abril 2026 | Versión inicial. 12 secciones. 12 agregados (7 N1 + 5 N2), 55 eventos, 26 invariantes (18 Local + 8 Eventual), 10 decisiones (D1-D10), 7 premisas (P1-P7), 3 pendientes (PD1-PD3), 6 sugerencias de implementación (SI1-SI6), 17 permisos atómicos, 3 domain services. Resultado de: construcción iterativa del modelo, 3 auditorías (V1: 36 hallazgos, V2: 31 hallazgos, V3: 84 hallazgos — 49 aplicados, 35 descartados), 3 rondas de revisión. Reporte consolidado de auditoría en `auditoria/contabilidad-actual.md`. |
| 1.1 | Mayo 2026 | Validación contractual del motor de traducción y formalización de rechazos previos al borrador. Cambios: (1) Paso 1 del ServicioDeTraduccion ajustado: el rechazo por referenciaOrigen no reemplazable se notifica al consumidor con motivo estructurado (sin evento de dominio). (2) Paso 2 del ServicioDeTraduccion ampliado: se valida que cada tipoComponente recibido tenga al menos un RolPartida en la plantilla. (3) Nueva invariante I27 (Local) — total ahora 27 invariantes (19 Local + 8 Eventual). (4) Tabla de motivos estructurados de rechazo previo al borrador (`REFERENCIA_ORIGEN_DUPLICADA_NO_REEMPLAZABLE`, `TIPO_TRANSACCION_SIN_PLANTILLA`, `LINEA_SIN_ROL_EN_PLANTILLA`) documentada en el ServicioDeTraduccion. (5) Nueva sugerencia de implementación SI7 — total ahora 7 sugerencias. (6) D5 sin cambios — el motor permanece como servicio sin estado; los rechazos pre-borrador se canalizan por mensajería (DLQ + logs + métricas) y la durabilidad la garantiza el outbox del consumidor emisor. Acompaña actualización de `definicion-alcance.md` v1.1 con la nueva regla R45. |
| 1.2 | Mayo 2026 | Arquitectura PUC + Libro + Marco contable y replanteamiento de libros predeterminados. Cambios: (1) Nuevo agregado `MarcoContable` (N1, configuración) en sección 3.5 — total ahora 13 agregados (8 N1 + 5 N2). (2) Atributo `marcoContable` agregado a `PlanDeCuentas` como referencia inmutable al código del marco; payload de `PlanDeCuentasCreado` actualizado. (3) Nuevos eventos del MarcoContable (`MarcoContableCreado`, `MarcoContableModificado`, `MarcoContableDesactivado`, `MarcoContableReactivado`) — total ahora 59 eventos. (4) Atributo `tipo` del `LibroContable` cambia de enum cerrado (Principal/Local/NIIF/Gerencial) a texto libre con predeterminados sugeridos `Principal` y `Fiscal`. Payload de `LibroContableCreado` actualizado. (5) Nuevas invariantes I28-I32 sobre MarcoContable y la asociación PlanDeCuentas → MarcoContable — total ahora 32 invariantes (22 Local + 10 Eventual). (6) Nueva decisión D11 — arquitectura PUC único + libros paralelos como caso típico moderno. (7) Premisa P5 actualizada — libros predeterminados Principal y Fiscal sobre PUC NIIF. (8) Renumeración de las secciones 3.5-3.18 (3.6-3.19) por la inserción de MarcoContable como sección 3.5. (9) `EquivalenciaPuc` sin cambios estructurales — se agrega nota informativa sobre uso excepcional. (10) Nuevo anexo `anexo-marco-contable-y-arquitectura-puc.md` con investigación de seis ERPs modernos y justificación de la arquitectura. Acompaña actualización de `definicion-alcance.md` v1.2 con nueva regla R46, glosario actualizado y nuevo término "Marco contable". |
| 1.3 | Mayo 2026 | Grupo del PUC esperado en la plantilla de asiento para acotar la inferencia (Nivel B) — issue #7. Cambios: (1) Nuevo VO `ComponenteDelRol` (`{ tipoComponente, grupoPucEsperado }`) en la composición de `PlantillaDeAsiento`: reemplaza el atributo plano `tipoComponenteAsociado` de `RolPartida` por una colección de componentes, cada uno con su grupo del PUC esperado. (2) Atributo `grupoPucEsperado` (lista de prefijos PUC de longitud variable) en `ComponenteDelRol` y, para la contrapartida, a nivel de `RolPartida`. (3) Paso 3 del `ServicioDeTraduccion` ampliado: el Nivel B se acota a las cuentas cuyo código inicia por algún prefijo de `grupoPucEsperado`; Niveles A y C no se acotan. (4) Paso 4 ampliado: la contrapartida acota su Nivel B con el grupo declarado a nivel de rol. (5) Diagrama de composición de `causacion_gasto` actualizado con los componentes y grupos (GASTO→`["51","52","53"]`, IMPUESTO iva→`["2408"]`/inc→a validar, RETENCION retefuente→`["2365"]`/reteiva→`["2367"]`, CONTRAPARTIDA→`["2205","2335"]`). (6) Payload de `RolPartidaAgregado` actualizado. (7) Nueva decisión D12 — total ahora 12 decisiones (D1-D12). Sin cambios en conteo de agregados (13), eventos (59) ni invariantes (32). Acompaña actualización de `definicion-alcance.md` v1.4 con nueva regla R47 y término de glosario "Grupo del PUC esperado" (entrada 27), y de `anexo-ejemplo-plantilla-de-asiento.md` v1.1 con los grupos esperados en los 3 ejemplos. Llenado completo del inventario de 42 plantillas queda pendiente de revisión por consultor contable (incluye confirmar el grupo del `inc`). |
| 1.4 | Junio 2026 | Narración del borrador: descripción general y descripción de concepto por partida — issue #8. Cambios: (1) Nuevo atributo `descripcion` (opcional) en la raíz de `BorradorContable` — descripción general del hecho económico que envía el consumidor; si no la envía, queda vacía. (2) Nuevo atributo `descripcionConcepto` (opcional) en `PartidaBorrador` — narración del movimiento, asignada solo a partidas cuyo componente la lleva. (3) Nuevo atributo `llevaDescripcionConcepto` (boolean) en `ComponenteDelRol` — declara qué componentes portan descripción (`true` en gasto/concepto_devuelto/anticipo; `false` en impuestos y retenciones). (4) Nuevo paso 4b del `ServicioDeTraduccion`: asigna `descripcionConcepto` por partida según el flag y traslada la `descripcion` general al encabezado. (5) Payloads de `BorradorCreado` y `BorradorReemplazado` actualizados (`descripcion` + `descripcionConcepto` por partida); payload de `RolPartidaAgregado` actualizado (`llevaDescripcionConcepto`). (6) Diagramas de composición de `BorradorContable` y de la plantilla `causacion_gasto` actualizados. (7) Nueva decisión D13 — total ahora 13 decisiones (D1-D13). Sin cambios en conteo de agregados (13), eventos (59) ni invariantes (32). Depende de que OXP envíe `descripcion`/`descripcionConcepto` en `LineaTraduccion` (issue #10). Acompaña actualización de `definicion-alcance.md` v1.5 (nueva regla R48 + términos de glosario) y del catálogo `datos-precargados/plantillas-de-asiento.*` (flag `llevaDescripcionConcepto` por componente). |
| 1.5 | Junio 2026 | Herencia del rol de la partida y propagación a la entrega — issue #9. Cambios: (1) Atributo del rol en `RolPartida` **renombrado de `nombre` a `rol`** (código de conjunto cerrado) — actualizado en composición, diagramas, payloads de `RolPartidaAgregado`/`RolPartidaModificado`/`RolPartidaEliminado`. (2) Nuevo atributo `rol` en `PartidaBorrador`, heredado del `RolPartida` de la plantilla; `esContrapartida` se conserva como atajo (rol == CONTRAPARTIDA). (3) Paso 5 del `ServicioDeTraduccion`: cada partida hereda el `rol`. (4) Paso 3 del flujo de `EntregaContable`: el payload al destino incluye el `rol` por partida (para que SincoA&F identifique partidas tributarias). (5) Payloads de `BorradorCreado` y `BorradorReemplazado` actualizados (`rol` por partida). (6) Diagramas de `BorradorContable` y plantilla `causacion_gasto` actualizados. (7) Nueva decisión D14 — total ahora 14 decisiones (D1-D14). Sin cambios en conteo de agregados (13), eventos (59) ni invariantes (32). Requisito específico de SincoA&F sobre impuestos se confirma al implementar el adaptador [PD1]; no se propaga a N2 (F2). Acompaña actualización de `definicion-alcance.md` v1.6 (nueva regla R49 + término de glosario "Rol de la partida") y del catálogo `datos-precargados/plantillas-de-asiento.*` (atributo `rol`). |
| 1.6 | Junio 2026 | Política configurable por empresa para la unidad organizacional de la contrapartida tipo cuenta por pagar — issue #17. Surgió al modelar el cruce de la OXP del extracto (issue #18) y resuelve una contradicción interna del modelo (el paso 4 del ServicioDeTraduccion afirmaba que la contrapartida tomaba la unidad organizacional del contexto de la transacción, mientras el asiento de ejemplo la mostraba consolidada en `—`). Cambios: (1) Nueva invariante **I33 (Eventual, configurable por empresa)** — la unidad organizacional de la contrapartida tipo cuenta por pagar se asigna según la preferencia de la empresa: distribuida (replicando las partidas de origen), consolidada en una unidad general, o sin unidad organizacional; respeta la obligatoriedad del PUC; aplica a toda contrapartida CxP (`causacion_gasto`, `nota_credito_gasto`, `anticipo_a_proveedor`). El mecanismo de almacenamiento/resolución de la preferencia queda a criterio de implementación. Total ahora **33 invariantes (22 Local + 11 Eventual)**. (2) Paso 4 del ServicioDeTraduccion ajustado: el tercero de la contrapartida se asigna del contexto de la transacción, la unidad organizacional sigue I33. (3) Nota aclaratoria en el asiento de ejemplo (el `undOrg: —` es uno de los modos, no la regla única). Sin cambios en conteo de agregados (13), eventos (59) ni decisiones (14). Acompaña actualización de `definicion-alcance.md` (nueva regla R50). |
| 1.7 | Junio 2026 | Rol `CRUCE_OBLIGACION` en la plantilla `causacion_gasto` por ajuste cruzado con OXP — issue #18. Cambios: (1) Nuevo rol `CRUCE_OBLIGACION` (Débito, `tipoComponente` `cruce_obligacion`, `grupoPucEsperado` `["2205","2335"]`) en el catálogo precargado `datos-precargados/plantillas-de-asiento.{md,json}` (v1.4) — lo alimenta solo `ExtractoCausado` (una línea por `Vinculacion`); salda la cuenta por pagar del proveedor de la compra cruzada, reclasificando la deuda hacia el banco/emisor. (2) Invariante **I33** ampliada: el criterio de la unidad organizacional aplica también a las partidas tipo cuenta por pagar alimentadas por línea (el cruce), no solo a la contrapartida generada por el motor; deben espejar la CxP de la causación original para netear. El diagrama ilustrativo de `causacion_gasto` en este documento no se modifica (no enumera el inventario completo de roles; la plantilla completa vive en el catálogo precargado). Sin cambios en conteo de agregados (13), eventos (59), invariantes (33) ni decisiones (14). Alinea con `modelo-dominio.md` de OXP v3.5 [D29]. |
| 1.8 | Junio 2026 | Fuente del tercero de la contrapartida — issue #28. Surgió al analizar el #18: el paso 4 del `ServicioDeTraduccion` decía que el tercero de la contrapartida se tomaba "del contexto de la transacción", pero `InformacionTransaccion` no contenía un tercero. En el extracto, las líneas `cruce_obligacion` traen varios proveedores y la contrapartida debe ir a la CxP del banco/emisor, que no viaja en ninguna línea. Cambios: (1) `InformacionTransaccion` ampliada con **`terceroPrincipal`** — el tercero del documento que envía el emisor (proveedor en causación de gasto/anticipo/nota crédito, banco/emisor en extracto), correspondiente al `InformacionTercero` de la raíz del agregado emisor. (2) Paso 4 del `ServicioDeTraduccion`: el tercero de la contrapartida es el `terceroPrincipal` (no las líneas, que pueden traer varios terceros); las partidas por línea conservan el tercero de su línea. (3) Payloads de `BorradorCreado` y `BorradorReemplazado` incluyen `terceroPrincipal`. (4) Nuevo ejemplo de extracto de cruce puro en `anexo-ejemplo-plantilla-de-asiento.md`. Sin cambios en conteo de agregados (13), eventos (59), invariantes (33) ni decisiones (14). Coordinación cruzada: OXP puebla `terceroPrincipal` a nivel de hecho económico. |
| 1.9 | Junio 2026 | Validación de datos maestros contra copia local — replanteamiento #45, issue #47. Precisada la invariante **`I7b`**: el estado de terceros y unidades organizacionales se valida contra la **copia local** de la señal que cada sub-dominio dueño publica por suscripción (Terceros: señal global; Estructura Organizacional: eventos de ciclo de vida de unidades), **sin consulta en caliente**. Antes decía "se valida contra sub-dominios externos", lenguaje que sugería consulta al maestro — desactualizado frente al patrón de copia local; de paso cierra el cabo que el #37 dejó al actualizar R07 en el alcance pero no esta invariante. Sin cambios de conteo (13 agregados, 59 eventos, 33 invariantes, 14 decisiones). Acompaña `definicion-alcance.md` v1.9 (actor y dependencia de Estructura Organizacional redefinidos; R07 ampliada a unidades + nota de reestructuración). |
| 1.10 | Julio 2026 | Ciclo contable de la partida en disputa del extracto — ajuste cruzado con OXP (issue #90). Cambios en el catálogo precargado `datos-precargados/plantillas-de-asiento.{md,json}` (v1.7): dos roles nuevos en `causacion_gasto` (`PARTIDA_POR_ACLARAR` Db / `PARTIDA_ACLARADA` Cr, transitoria de partidas por aclarar, tentativo `["1360","1380"]`) y **nueva plantilla `reclasificacion_partida`** (Db `CRUCE_OBLIGACION` · Cr `PARTIDA_ACLARADA`, **sin contrapartida del motor** — sus dos líneas viajan explícitas desde OXP con tercero propio). Nuevo **Ejemplo 5** en el anexo (v1.4) con los tres momentos del ciclo. El conjunto cerrado de roles (`[D14]`) incorpora los dos códigos nuevos. Cobertura del catálogo: 5 → 6 plantillas (corregida de paso la mención "4 plantillas" de la Sección de inventario, desactualizada desde v1.6 del catálogo). Sin cambios en agregados (13), eventos (59), invariantes (33) ni decisiones (14) — la mecánica del motor no cambia; la plantilla sin rol CONTRAPARTIDA ya está admitida por el modelo (la contrapartida es un rol opcional de la plantilla, no un paso obligatorio del ServicioDeTraduccion). Alinea con `modelo-dominio.md` de OXP v4.6 (`[D37]`). |
| 1.11 | Julio 2026 | Rol `IMPUESTO_ASUMIDO` en la plantilla `causacion_gasto` del catálogo precargado (v1.8) por ajuste cruzado con OXP — issue #94. Retención asumida cuando el pago ya salió completo (medio de pago tarjeta, `[D38]` de OXP): la retención viaja idéntica (Cr, certificable) y la línea Db espejo `retencion_asumida` (tentativo `["5315"]`) la registra como gasto propio; la contrapartida queda por el total. Sin cambios en el modelo (agregados, eventos, invariantes, decisiones) — la mecánica del motor no cambia. |
| 1.12 | Julio 2026 | **Clasificación semántica, contrapartida como línea y resolución por espejo — issue #104.** Cierra la confusión del equipo de desarrollo sobre el campo `clasificacion` de la línea de traducción (¿texto abierto o código normalizado?). **Nueva decisión [D15]** con tres piezas: **(1) Clasificación = texto semántico de emparejamiento** — el consumidor la compone mecánicamente por componente desde sus catálogos (recetas por `tipoComponente` en el modelo de OXP v4.8); obligatoria en toda línea (**[I34]** nueva); los Niveles A y C pasan de llave exacta a **partición estable exacta (tipoTransaccion, tipoComponente, empresa) + emparejamiento por similitud** (**[SI8]** nueva: umbral, coincidencia exacta = similitud máxima, desempate por recencia, índice por empresa y partición); el Nivel B la compara contra las descripciones de las cuentas del PUC. Glosario ("Combinación de dimensiones" redefinida + término "Clasificación"), `Regla` (ahora con `textoAncla`), `ResolucionAprendida`, `resolver()`, payloads de `ReglaAgregada`/`ResolucionAprendida`/`AprendizajePromovidoARegla`, e invariantes [I9]/[I14] reescritas en esos términos. `PartidaBorrador` gana `clasificacion` (payloads de `BorradorCreado`/`BorradorReemplazado`). **(2) La contrapartida viaja como línea** (`tipoComponente = contrapartida`): tercero y clasificación los envía el consumidor; el motor solo calcula el valor (balance) y rinde la unidad organizacional según [I33]; obligatoria cuando la plantilla tiene el rol (**[I35]** nueva + motivo `LINEA_CONTRAPARTIDA_FALTANTE`; línea sin texto → `LINEA_SIN_CLASIFICACION`); `RolPartida` pierde el `grupoPucEsperado` a nivel de rol (la excepción de [D12] se disuelve); **`terceroPrincipal` se conserva como informativo** (deja de ser la fuente del tercero de la contrapartida — ajuste sobre el #28). **(3) Resolución por espejo del hecho relacionado:** `ComponenteDelRol` gana `resolucionPorEspejo`; `cruce_obligacion`, `partida_aclarada`, `amortizacion_anticipo`, `reversa_anticipo` y todos los componentes de `nota_credito_gasto` copian la cuenta de la partida del rol espejado en el borrador del hecho relacionado (vía `referenciaHechoRelacionado`) — garantiza que el cruce contable salde en la misma cuenta; precede a la cadena, no alimenta el aprendizaje, y ante ausencia/ambigüedad deja el borrador PENDIENTE. Pasos 2, 3 y 4 del `ServicioDeTraduccion` reescritos; `nivelResolucion` admite `espejo`. Conteos: invariantes 33 → **35** ([I34], [I35]); decisiones 14 → **15** ([D15]); sugerencias 7 → **8** ([SI8]); sin cambios en agregados (13) ni eventos (59). **Coordinación cruzada:** alcance v1.11, anexo de ejemplos v1.5, catálogo de plantillas v1.9, modelo de OXP v4.8 (recetas de composición + línea `contrapartida`, catálogo canónico 17 → 18). |
