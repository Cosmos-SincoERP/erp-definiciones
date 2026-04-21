# Anexo — Decisiones de diseño para el modelo de dominio

> **Fecha:** Marzo 2026
> **Propósito:** Registrar las decisiones de diseño conversadas durante la definición de alcance del sub-dominio de Contabilidad. Estas decisiones no son parte del alcance (el *qué*) sino insumos para el modelo de dominio (el *cómo*). Se documentan aquí para no perder el contexto cuando se inicie la Fase 2.
> **Versión:** 4.0

---

## Nivel 1 — Motor de Traducción (obligatorio)

### DD1 — Borrador contable como resultado de la traducción

El resultado inmediato de la traducción es siempre un borrador contable. Tiene tres estados: PENDIENTE (cuentas sin resolver, el contador puede editarlo), RESUELTO (completo y balanceado, se entrega inmediatamente al Servicio de Entrega) y DESCARTADO (solo para borradores manuales creados por el contador, desde estado pendiente). El borrador no conoce el destino ni el resultado de la entrega — su responsabilidad termina cuando está resuelto.

**Contexto:** Se evaluaron 5 opciones arquitectónicas:
- (A) Borrador → Asiento → Libro Mayor como proyección
- (B) Borrador → Libro Mayor como concepto de primer nivel → Proyecciones
- (C) Borrador → Asiento confirmado → Libro Mayor como concepto → Proyecciones (3 fases)
- (D) Sin borrador persistente → Libro Mayor → Proyecciones
- (E) Libro Mayor absorbe todo (sin borrador separado)

Se evaluó también extender el borrador con estados de entrega (ENVIADO, CONTABILIZADO, RECHAZADO) pero se descartó porque convertía al borrador en un "mini asiento contable" acoplado al destino.

**Decisión:** Borrador con tres estados (PENDIENTE / RESUELTO / DESCARTADO). El borrador es responsabilidad del Motor de Traducción. La entrega y su resultado son responsabilidad del Servicio de Entrega. El asiento contable es responsabilidad del destino (N2 propio u otro sistema). Un borrador rechazado por el destino vuelve a PENDIENTE.

**Razones:**
- Semántica honesta: un borrador es trabajo en progreso, un asiento es un hecho contable.
- El borrador no se acopla al destino — su única responsabilidad es la traducción.
- RESUELTO es transitorio: se envía inmediatamente al Servicio de Entrega.
- PENDIENTE es el único estado donde el contador interactúa.
- DESCARTADO solo aplica a borradores manuales desde estado pendiente.

---

### DD2 — Cadena de resolución de cuentas: A → C → B

La resolución de la cuenta auxiliar para cada rol del asiento sigue tres niveles en orden de precedencia:
- **Nivel A** — Regla manual del analista contable (excepción explícita).
- **Nivel C** — Aprendizaje del sistema (confirmaciones y correcciones previas del usuario).
- **Nivel B** — Inferencia inteligente (análisis del plan de cuentas: nombre, código, jerarquía).

Cada intervención del usuario alimenta el Nivel C. El analista puede promover un aprendizaje a regla formal (Nivel A).

**Contexto:** Detallado con ejemplos en `anexo-ejemplo-plantilla-de-asiento.md`, secciones 1.4 y 2.4.

---

### DD3 — Referencia contable como trazabilidad bidireccional, sin estado

Cada sub-dominio consumidor persiste una referencia al asiento contable como campo de trazabilidad. No es un estado, no afecta la FSM del consumidor, no introduce conceptos contables en el dominio transaccional. Es una referencia única que se popula cuando el Servicio de Entrega informa el resultado de la contabilización.

**Contexto:** Se evaluaron tres alternativas:

1. *El consumidor guarda estado contable completo (descartada):* Problema: contamina el dominio del consumidor con conceptos contables.

2. *El consumidor no guarda nada (descartada):* Problema: la pregunta más básica de un sistema financiero ("¿dónde está el asiento de esta transacción?") requiere una consulta cruzada a otro dominio.

3. *El consumidor guarda solo la referencia al asiento (adoptada):* Un campo, sin estado, sin conceptos contables. Se popula una vez cuando el Servicio de Entrega informa el resultado.

**Decisión:** Todo sub-dominio que emita líneas de traducción persiste un campo con la referencia al asiento contable en el destino. El campo es vacío mientras no se haya contabilizado. No hay estados intermedios, no hay referencia al borrador, no hay manejo de descartes.

**Razones:**
- Trazabilidad bidireccional: N1 apunta al consumidor (referencia de origen), el consumidor apunta al destino (referencia al asiento).
- Requisito de auditoría: transacción → asiento → reporte financiero.
- Costo mínimo: un campo, cero estados. Aplica por igual a todos los consumidores.

---

### DD4 — Contrato de integración bidireccional: consumidor ↔ Motor de Traducción

La integración entre cualquier sub-dominio transaccional y el Motor de Traducción sigue un contrato estandarizado bidireccional. Este contrato es transversal — aplica por igual a OXP, CXC, Tesorería, Nómina, Activos Fijos, Arrendamientos y cualquier consumidor futuro.

**Contrato:**

Consumidor → Motor:
- Emite líneas de traducción con una referencia de origen que identifica unívocamente el hecho económico.
- La referencia de origen es **única en el Motor**. El Motor valida esta unicidad como invariante de idempotencia.

Servicio de Entrega → Consumidor:
- Informa el resultado de la contabilización con la referencia de origen + la referencia al asiento contable del destino + el comprobante.
- El consumidor persiste la referencia al asiento como trazabilidad (DD3).
- Una sola información por referencia de origen. No hay estados intermedios.

**Invariantes:**
- La referencia de origen es única en el Motor (validada al crear el borrador).
- La referencia al asiento es única por transacción del consumidor.
- El consumidor no recibe información de descartes ni de borradores pendientes.

---

### DD5 — N1 como servicio con dos capacidades y destino configurable

N1 está compuesto por dos capacidades: (1) el motor de traducción (líneas → borradores) y (2) el Servicio de Entrega (borradores resueltos → sistema contable de destino). El destino es configurable por empresa.

**Contexto:** Se identificó que el acoplamiento entre los módulos de negocio (OXP, ABR, CXC) y el sistema contable impedía la comercialización independiente de los módulos. Un cliente con Siigo o Alegra no podía usar los módulos de negocio sin adoptar todo el sistema contable.

**Decisión:** N1 opera en dos modos según el destino configurado:
- **Adaptador:** Traduce y el Servicio de Entrega envía a un sistema contable externo (SincoA&F, Siigo, Alegra, otro). No persiste asientos propios.
- **Sistema contable propio:** Traduce y el Servicio de Entrega envía a N2. N2 persiste los asientos.

La elección del destino es una decisión de administración del sistema, no de operación contable. Solo un destino activo por empresa.

**Razones:**
- Los módulos de negocio se pueden comercializar sin obligar al cliente a adoptar un sistema contable específico.
- El contrato de líneas de traducción es el mismo independientemente del destino.
- El destino es configuración, no código.

---

### DD6 — Coreografía de eventos para la coordinación del flujo

El flujo de contabilización se coordina mediante eventos desacoplados. Ningún servicio conoce el flujo completo — la coordinación emerge de la cadena de eventos.

**Flujo de eventos:**
1. El Motor de Traducción emite: `BorradorResuelto` (cuando la traducción está completa y balanceada).
2. El Servicio de Entrega escucha `BorradorResuelto`, entrega al destino y emite: `EntregaAceptada` (si el destino acepta, con la referencia del destino) o `EntregaRechazada` (si el destino rechaza, con motivo). Cuando el destino es N2, además se emite `AsientoContabilizado` en el stream del AsientoContable [D3].
3. El consumidor (OXP, CXC) escucha `EntregaAceptada` y actualiza su referencia al asiento.
4. N1 escucha `EntregaAceptada` y `EntregaRechazada` para construir la vista de estado de contabilización. Cuando escucha `EntregaRechazada`, el borrador vuelve a PENDIENTE.

**Diagrama del flujo de eventos:**

```
CONSUMIDOR (OXP)        N1 - MOTOR              SERV. ENTREGA         DESTINO
────────────────        ──────────              ───────────────        ───────

① HechoEconomicoEmitido
        │
        ▼
  ┌─────────────┐
  │   MOTOR     │
  │             │
  │ PENDIENTE   │ ← contador resuelve cuentas
  │     ↓       │
  │ RESUELTO    │
  └──────┬──────┘
         │
  ② BorradorResuelto
         │
         ▼
  ┌─────────────┐
  │  SERVICIO   │
  │  ENTREGA    │         ┌──────────────┐
  │  adaptador ─┼────────▶│   DESTINO    │
  │             │◀────────┤  (SincoA&F,  │
  │             │         │   Siigo, N2) │
  └──────┬──────┘         └──────────────┘
         │
    ┌────┴────┐
    │         │
③ Aceptado  ④ Rechazado
    │         │
EntregaAceptada    EntregaRechazada
 { referencia }     { motivo }
    │         │
    ▼         ▼
OXP escucha  Motor escucha
guarda ref   borrador → PENDIENTE
             contador actúa
```

**Contexto:** Se evaluaron tres alternativas:
1. *Borrador con estados de entrega (descartada):* El borrador acumulaba estados ENVIADO/CONTABILIZADO/RECHAZADO, convirtiéndolo en un mini asiento contable acoplado al destino.
2. *Saga con orquestador central (descartada):* Un coordinador central gestionaba todo el flujo. Más complejo y con riesgo de acumular lógica de negocio.
3. *Coreografía de eventos (adoptada):* Cada servicio hace lo suyo y emite un evento. La coordinación es emergente.

**Justificación:** Patrón inspirado en Oracle SLA → Transfer to GL, donde el motor de traducción (SLA) y el proceso de transferencia (Transfer to GL) son procesos separados que se coordinan por lotes.

---

### DD7 — El Motor no valida periodos del destino

El Motor de Traducción no valida si el periodo está abierto o cerrado en el sistema contable de destino. La validación de periodos es responsabilidad del destino. Si el destino rechaza por periodo cerrado, el Servicio de Entrega gestiona el rechazo.

**Contexto:** Se definieron originalmente reglas R17/R18 donde el Motor rechazaba hechos económicos de periodos cerrados y el consumidor re-emitía. Se descartaron porque:
- El Motor traduce, no valida reglas del destino.
- Para destinos externos (SincoA&F, Siigo) N1 no puede conocer el estado de los periodos.
- El consumidor emite una sola vez (R14) — la corrección se gestiona dentro de N1, no por re-emisión.

**Decisión:** N1 siempre traduce. El destino decide si acepta o rechaza. Cuando N2 es el destino, N2 aplica la validación de periodo con opción de redirección al mes siguiente (R28).

---

## Nivel 2 — Sistema contable (opcional)

### DD8 — Dos reportes contables principales: auxiliar contable (detalle) y saldos contables (agrupada)

El sistema requiere dos reportes principales que se alimentan del mismo origen (los asientos contabilizados) pero sirven a propósitos diferentes: el auxiliar contable para reportes de detalle (movimientos individuales) y los saldos contables para reportes agrupados (balances, estados financieros).

**Contexto:** Se evaluaron tres alternativas en secuencia:

1. *Libro Mayor como concepto de primer nivel (descartado):* Problemas: (a) en ES, un stream que crece con cada asiento del periodo es inviable para reconstruir estado; (b) los periodos y la numeración son compartidos entre libros (DD10), generando conflictos de propiedad; (c) el PUC se consume durante la traducción pero el Libro Mayor se alimenta después, creando dependencia circular.

2. *Reporte único de detalle (descartado):* Los reportes de saldos requieren agregar potencialmente millones de filas en cada consulta.

3. *Dos reportes: detalle + saldos (adoptado):* Cada uno optimizado para su propósito.

**Decisión:** Dos reportes persistentes — auxiliar contable (detalle por partida) y saldos contables (agrupada por dimensiones). Ambos con `libroOrigen` y `libroPresentacion` como atributos. La equivalencia de PUC se resuelve al escribir (congelada al momento de registrar).

**Detalle:** Estructura, ejemplos, filtros y direccionamiento de reportes documentados en `anexo-proyecciones-contables.md`.

---

### DD9 — Multi-libro: asiento en el libro que corresponda, equivalencia en los reportes

El multi-libro se resuelve mediante el patrón: los asientos automáticos van al libro Principal y los reportes los reflejan en los demás libros aplicando la equivalencia de PUC. Los asientos manuales pueden ir directamente al libro que corresponda sin generar equivalencia inversa.

**Contexto:** Se evaluaron dos planteamientos:

1. *N asientos independientes por libro (descartado):* Introduce control de cruces entre documentos de distintos libros.

2. *Asiento en el libro que corresponda + equivalencia en reportes (adoptado):* Un solo asiento para la operación transaccional. Los libros alternos lo ven mediante los reportes que aplican la equivalencia de PUC al registrar.

**Decisión:** Cada libro contable es configuración (CRUD) que define: tipo, PUC asociado y equivalencia entre PUCs. El asiento contable tiene `libro` como atributo. Los reportes registran entradas con `libroOrigen` y `libroPresentacion` para cada libro que deba ver el asiento.

---

### DD10 — Numeración y periodos compartidos entre libros

La numeración contable y los periodos contables son compartidos entre libros. No se segmentan por libro.

**Contexto:** Solo Oracle Fusion usa el libro como dimensión de numeración. No hay requisito legal que lo exija.

**Decisión:** La numeración se segmenta por empresa + tipo de comprobante + periodo + sucursal. Los periodos son por empresa. Ambos compartidos entre libros.

---

### DD11 — Anulación contable por asiento inverso

Un asiento contable nunca se borra ni se modifica. La anulación se materializa como un nuevo asiento contable con las partidas invertidas, referenciando al asiento original.

**Contexto:** Requisito legal (Decreto 2649, Art. 124). Práctica universal en todos los ERPs investigados.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: 8 decisiones de diseño (DD1-DD8). |
| 2.0 | Marzo 2026 | Reestructuración fallida: separación Motor ↔ Contabilidad en carpetas diferentes. Revertida. |
| 3.0 | Marzo 2026 | Reunificación: un solo archivo organizado por niveles. |
| 4.0 | Marzo 2026 | Actualización post-coreografía de eventos. DD1: borrador con 3 estados (PENDIENTE/RESUELTO/DESCARTADO), no conoce el destino. DD4: Servicio de Entrega informa al consumidor. DD5: dos capacidades de N1, solo un destino por empresa. DD6 nueva: coreografía de eventos (BorradorResuelto → AsientoContabilizado / EntregaRechazada). DD7 nueva: Motor no valida periodos del destino. DDs de N2 renumeradas (DD8-DD11). |
