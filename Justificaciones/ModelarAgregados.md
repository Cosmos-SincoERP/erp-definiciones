# Justificación: Modelar OXP como dos agregados separados

## Contexto de la decisión

Se evaluó si el dominio OXP debe modelarse como un **agregado único Oxp con variantes** (Comercio y Extracto) o como **dos agregados raíz separados** (OxpComercio y OxpExtracto). El documento guía implementación técnica con arquitectura de event sourcing.

---

## Argumento inicial: atributos compartidos

Es válido que Comercio y Extracto compartan atributos. Pero hay una distinción clave en DDD:

> **Atributos compartidos → Value Objects compartidos**, no agregado compartido.
>
> **Agregado compartido → comportamiento y transiciones compartidas.**

Ejemplo concreto de lo que comparten:

```
InformacionTercero   { nit, razonSocial }
MedioDePago          { tipo, numero, entidad }
ValorMonetario       { monto, moneda, trm, montoFuncional }
Concepto             { tipo, valor, cuentaContable, centroCosto, naturaleza }
```

Estos se modelan como **Value Objects reutilizables** por ambos agregados. No se necesita un solo agregado para evitar duplicación — se necesitan tipos compartidos dentro del bounded context.

---

## Prueba desde event sourcing

En event sourcing, el agregado raíz es el dueño del stream de eventos. Ejercicio comparativo:

### Con agregado único Oxp

```
Stream: oxp-{id}

// Instancia Comercio (stream oxp-abc123):
OxpComercioRadicada        → apply() → variante=Comercio, estado=Pendiente
OxpComercioConfirmada      → apply() → estado=Confirmada
OxpComercioCausada         → apply() → estado=Causada
OxpComercioCompensada      → apply() → estado=Compensada

// Instancia Extracto (stream oxp-def456):
ExtractoRadicado           → apply() → variante=Extracto, estado=Pendiente
ConciliacionIniciada       → apply() → estado=ParcialmenteConciliado
ExtractoConciliado         → apply() → estado=Conciliado
ExtractoCausado            → apply() → estado=Causado
ExtractoPagado             → apply() → estado=Pagado
```

**El problema:** la función `apply()` del agregado Oxp necesita manejar **28 eventos** con un switch por variante. El replay reconstituye un objeto Oxp que internamente es una cosa **o** la otra, nunca ambas. Se paga el costo de un solo tipo pero sin obtener beneficio de polimorfismo real porque:

- Ningún evento es emitido por ambas variantes
- Ninguna transición es compartida
- El `apply()` de un evento de Comercio nunca toca campos de Extracto

### Con dos agregados

```
Stream: oxp-comercio-{id}
OxpComercioRadicada        → apply() → estado=Pendiente
OxpComercioConfirmada      → apply() → estado=Confirmada
...

Stream: oxp-extracto-{id}
ExtractoRadicado           → apply() → estado=Pendiente
ConciliacionIniciada       → apply() → estado=ParcialmenteConciliado
...
```

Cada agregado:

- Tiene su propio stream tipado
- Su función `apply()` solo conoce sus eventos (14 y 12 respectivamente)
- Su replay es directo, sin condicionales de variante
- Comparten Value Objects (`InformacionTercero`, `ValorMonetario`, `Concepto`, etc.)

---

## La vinculación entre ambos

En event sourcing, `VinculacionRealizada` (el punto de convergencia) se modela así:

```
// Domain Service: ServicioDeConciliacion

1. Carga OxpComercio (stream oxp-comercio-abc)
2. Carga OxpExtracto  (stream oxp-extracto-def)
3. Valida precondiciones sobre ambos
4. Emite OxpComercioCompensada    → stream oxp-comercio-abc
5. Emite VinculacionRealizada     → stream oxp-extracto-def
```

Dos streams, consistencia eventual, coordinados por un domain service. Esto es un **patrón estándar** en event sourcing — no es una excepción ni un workaround.

---

## Resumen de consecuencias

| Aspecto | Agregado único | Dos agregados |
|---|---|---|
| **Duplicación de atributos** | No hay | No hay (Value Objects compartidos) |
| **`apply()` del agregado** | 28 eventos, switch por variante | 14 y 12 eventos, sin switch |
| **Stream de eventos** | Mixto (solo Comercio O solo Extracto por instancia) | Tipado y limpio |
| **Projections / Read models** | Filtrar por variante | Proyección directa por tipo |
| **VinculacionRealizada** | Ambigua ("Variante: Ambas") | Explícita (domain service + dos eventos) |
| **Evolución** | Cambio en Extracto toca el agregado de Comercio | Independiente |

---

## Decisión

**Dos agregados separados: OxpComercio y OxpExtracto.**

La preocupación por duplicación de atributos se resuelve con **Value Objects compartidos**, no con un agregado compartido. Desde event sourcing, el agregado único genera complejidad en el `apply()`, en los streams y en las projections sin dar beneficio real — porque las dos variantes nunca comparten comportamiento, solo datos.
