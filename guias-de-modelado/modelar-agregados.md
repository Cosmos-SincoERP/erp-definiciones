# Guía: Cómo decidir los agregados de un sub-dominio

## Propósito

Guía de decisión para determinar cuántos y cuáles agregados debe tener un bounded context. Aplica a todos los sub-dominios del ERP.

---

## 1. Principio rector: invariantes definen fronteras

> **DDD (Vaughn Vernon):** "Design small aggregates. Aggregate boundaries exist to protect invariants, not to group related data."

La pregunta que define si dos conceptos pertenecen al mismo agregado no es "¿comparten atributos?" sino:

> **¿Puede una operación sobre A violar una invariante de B dentro de la misma transacción?**

Si la respuesta es no, A y B son agregados separados. Los atributos compartidos se resuelven con **Value Objects reutilizables**, no con un agregado compartido.

---

## 2. Método de análisis

Para cada par de conceptos candidatos a compartir un agregado, evaluar:

### Checklist de separación

| # | Pregunta | Si la respuesta es NO → separar |
|---|----------|-------------------------------|
| 1 | ¿Alguna invariante de A depende del estado de B para validarse **transaccionalmente**? | No hay dependencia transaccional cruzada |
| 2 | ¿Comparten transiciones de estado (misma FSM)? | Ciclos de vida independientes → agregados separados |
| 3 | ¿Se crean y eliminan juntos? | Ciclos de vida independientes → agregados separados |
| 4 | ¿Una operación sobre A necesita bloquear a B para mantener consistencia? | Sin contención → agregados separados |
| 5 | ¿Un cambio en la FSM de A requiere cambiar la FSM de B? | No se afectan mutuamente → agregados separados |

### Checklist de unificación

| # | Pregunta | Si la respuesta es SÍ → considerar unificar |
|---|----------|---------------------------------------------|
| 1 | ¿La invariante principal cruza ambos conceptos dentro de la misma transacción? | Deben estar juntos |
| 2 | ¿Extraer B dejaría a A sin capacidad de evaluar su invariante principal? | B es entidad interna de A |
| 3 | ¿B no tiene sentido de negocio fuera de A? | B es entidad interna o value object |

---

## 3. Señales de alerta

### Señales de que hay que separar
- Un agregado tiene más de 20 eventos → evaluar extracción.
- Dos subconjuntos de entidades internas nunca interactúan entre sí.
- Un concepto tiene su propia FSM interna que no depende del estado del padre.

### Señales de que hay que unificar
- Un "agregado" tiene 1-2 eventos y ninguna invariante propia → es una entidad interna.
- Dos agregados siempre se crean, transicionan y cierran juntos → son uno solo.
- La consistencia eventual entre ellos genera problemas de negocio inaceptables.

---

## 4. Atributos compartidos: Value Objects, no agregado compartido

Compartir atributos no justifica un agregado único — justifica tipos compartidos:

> **DDD:** Atributos compartidos → Value Objects compartidos. Agregado compartido → comportamiento y transiciones compartidas.

Si dos agregados usan `InformacionTercero { nit, razonSocial }`, no necesitan estar juntos. Solo necesitan un VO reutilizable dentro del bounded context.

---

## 5. Coordinación inter-agregado: domain services

Cuando una operación afecta a más de un agregado, se coordina mediante un **domain service** que emite eventos a múltiples streams con consistencia eventual.

Cada servicio:
1. Carga los agregados involucrados (streams independientes)
2. Valida precondiciones sobre cada agregado
3. Emite eventos a cada stream
4. Consistencia eventual — no transacción distribuida

---

## 6. Guía de implementación: polimorfismo sobre discriminadores

Entidades con atributos condicionales por tipo deben resolverse con **polimorfismo** (sealed types), no con campos opcionales gobernados por un discriminador.

> **POO (Open/Closed Principle):** Los atributos condicionales gobernados por un campo `tipo` son un code smell. Cada tipo debe ser una clase con su propia estructura, compartiendo un contrato. Agregar un nuevo tipo no debería modificar el código existente.

Del mismo modo, domain services con ramas que no comparten lógica significativa se implementan con **Strategy pattern** o servicios especializados por tipo, unificados por un dispatcher.

---

## 7. Ejemplo aplicado: Obligaciones por Pagar (OXP)

El bounded context OXP tiene **4 agregados raíz**: OxpComercio, OxpExtracto, Anticipo y Devolucion. Estado actual: 47 eventos, 17 invariantes.

### 7.1. Invariantes independientes por agregado

Cada agregado protege un conjunto de invariantes que solo dependen de su propio estado interno. Ninguna invariante requiere consistencia transaccional entre dos agregados.

**OxpComercio:**

| Invariante | Qué protege |
|---|---|
| I1 | Unicidad NIT + soporte en ventana de 24 meses |
| I5 | Consistencia de moneda (origen + funcional) |
| I10 (parcial) | Distribución coherente: ConceptoDeGasto → DesgloseFiscal → InstruccionDistribucion |
| I13 | `saldoPorPagar()` ≥ 0 (sum PagoAplicado ≤ valorNeto) |
| I15 (parcial) | Causada ↔ saldoPorPagar > 0; Pagada ↔ saldoPorPagar = 0 |

**OxpExtracto:**

| Invariante | Qué protege |
|---|---|
| I3 | Completitud de conciliación (100% partidas resueltas → Conciliado) |
| I14 | `saldoPorPagar()` ≥ 0 (sum CrucePagoExtractoAplicado ≤ valorTotalExtracto) |
| I15 (parcial) | Causado ↔ saldoPorPagar > 0; Pagado ↔ saldoPorPagar = 0 |

**Anticipo:**

| Invariante | Qué protege |
|---|---|
| I8 | Causalidad: cruces solo en estados no terminales, saldos suficientes, tipo reversa exclusivo |
| I11 | `saldoPorPagar()` ≥ 0 y `saldoPorRegularizar()` ≥ 0 |
| I12 | Consistencia estado ↔ saldos (Vigente/Pagado/Regularizado/Cerrado/Reversado) |

**Devolucion:**

| Invariante | Qué protege |
|---|---|
| I17 | Consistencia de devolución: valorNeto vs. agregado OXP origen, acumulado por OXP |

### 7.2. Invariantes transversales (eventual)

| Invariante | Agregados involucrados | Cómo se resuelve |
|---|---|---|
| I2 | Todos (distribución suma 100%) | Cada agregado valida su propia InstruccionDistribucion |
| I4 | Todos (progresión de estados) | Cada agregado controla su propia FSM |
| I6 | OxpComercio, OxpExtracto | Segregación de funciones — validada individualmente |
| I7 | Inter-agregado | Vinculación coherente: vía domain service (consistencia eventual) |
| I9 | OxpComercio, OxpExtracto, Devolucion | Confirmación externa — cada agregado valida su propia transición |
| I16 | OxpComercio, OxpExtracto | Causalidad de pago — cada agregado valida su propio estado |

**Conclusión:** Ninguna invariante requiere consistencia transaccional entre dos agregados. Las operaciones inter-agregado se coordinan mediante domain services con consistencia eventual.

### 7.3. Ciclos de vida independientes

| Agregado | Estados | Eventos | Stream |
|---|---|---|---|
| OxpComercio | Pendiente → Confirmada → Causada → Pagada (+ Devuelta) | 12 | `oxp-comercio-{id}` |
| OxpExtracto | Pendiente → Parc. Conciliado → Conciliado → Confirmado → Causado → Pagado | 19 | `oxp-extracto-{id}` |
| Anticipo | Vigente → Pagado / Regularizado → Cerrado (o Reversado) | 9 | `anticipo-{id}` |
| Devolucion | Pendiente → Confirmada → Causada | 3 | `devolucion-{id}` |

Ningún evento es emitido por dos agregados. Ninguna transición es compartida.

### 7.4. Value Objects compartidos

```
InformacionTercero   { nit, razonSocial }              → 4 agregados
MedioDePago          { tipo, origen, tarjeta? }         → 2 agregados (OxpComercio, Anticipo)
ValorMonetario       { monto, moneda, trm, funcional }  → 4 agregados
SoporteDocumental    { tipo, referencia, datos }        → 4 agregados
DestinoDeNegocio     { unidadOrganizacional, % }        → 4 agregados (vía InstruccionDistribucion)
```

### 7.5. Coordinación vía domain services

| Domain Service | Agregados coordinados | Eventos emitidos |
|---|---|---|
| ServicioDeConciliacion | OxpComercio + OxpExtracto | `PagoOxpComercioViaExtractoAplicado` → stream comercio, `VinculacionRealizada` → stream extracto |
| ServicioDeRegularizacion | Anticipo + OxpComercio | `AnticipoRegularizado` → stream anticipo, `PagoOxpComercioViaAnticipoAplicado` → stream comercio |
| ServicioDeAplicacionDevolucion | Devolucion + (OxpComercio / OxpExtracto / Anticipo) | `DevolucionConfirmada` → stream devolucion + evento(s) sobre agregado OXP origen |

### 7.6. Tamaño de los agregados

| Agregado | Entidades internas | Evaluación |
|---|---|---|
| OxpComercio | 2 (ConceptoDeGasto, PagoAplicado) | Adecuado |
| OxpExtracto | 8 (PartidaExtracto, CargoFinanciero, AjustePorDiferenciaCambio, AjustePorTolerancia, Vinculacion, CoberturaAnticipo, CoberturaDevolucion, CrucePagoExtractoAplicado) | Grande — justificado por I3 |
| Anticipo | 2 (CrucePagoAplicado, CruceRegularizacionAplicada) | Adecuado |
| Devolucion | 1 (ConceptoDeDevolucion) | Adecuado |

OxpExtracto es el más grande (8 entidades, 19 eventos). Se justifica porque todas participan en I3 (completitud de conciliación): extraer cualquiera rompería la capacidad del agregado de evaluar I3 transaccionalmente.

### 7.7. Polimorfismo en OXP

| Entidad del modelo | Tipos | Implementación recomendada |
|---|---|---|
| ConceptoDeDevolucion | Comercio / Extracto / Anticipo | Sealed interface con 3 implementaciones |
| PagoAplicado | extracto / anticipo / pago_directo / devolucion | Sealed interface con 4 implementaciones |
| CrucePagoExtractoAplicado | pago_sincoa / devolucion | Sealed interface con 2 implementaciones |
| CrucePagoAplicado (Anticipo) | extracto / pago_directo / devolucion / reversa | Sealed interface con 4 implementaciones |
| CruceRegularizacionAplicada (Anticipo) | regularizacion / reversa | Sealed interface con 2 implementaciones |

### 7.8. Prueba complementaria desde event sourcing

La separación se valida también desde la mecánica de event sourcing:

- **Un agregado = un stream.** Si se unificaran los 4 agregados, un solo stream mezclaría 47 eventos con un switch cuádruple por variante.
- **Replay directo.** Cada agregado reconstruye solo su propio estado.
- **Projections tipadas.** Read models proyectan directamente por tipo de stream.

Este argumento refuerza la decisión, pero no la fundamenta. La razón principal son las invariantes (Sección 7.1).

### 7.9. Decisión

**Cuatro agregados raíz: OxpComercio, OxpExtracto, Anticipo y Devolucion.**

1. **Invariantes independientes:** cada agregado protege sus propias invariantes sin consistencia transaccional cruzada.
2. **Ciclos de vida independientes:** estados, transiciones y eventos disjuntos.
3. **Atributos compartidos resueltos con Value Objects.**
4. **Coordinación vía domain services** con consistencia eventual.
5. **Entidades con discriminadores de tipo** se implementan con polimorfismo (sealed types).

---

## Historial

| Versión | Cambio |
|---|---|
| v1 (v1.2 del modelo OXP) | Documento original: justificación de OxpComercio y OxpExtracto como dos agregados separados. |
| v2 (v2.3 del modelo OXP) | Reescritura: extendido a 4 agregados. Argumento reorientado a invariantes y límites de consistencia transaccional. |
| v3 | Generalización: guía universal de decisión de agregados. Contenido OXP movido a sección de ejemplo. |
