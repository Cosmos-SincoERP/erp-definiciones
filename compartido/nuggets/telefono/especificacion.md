# Nugget `Telefono` — Especificación

| | |
|---|---|
| **Estado** | En especificación — borrador para revisión |
| **Versión** | 0.1 |
| **Gobernanza** | [gobernanza-nuggets.md](../gobernanza-nuggets.md) · **Catálogo:** [catalogo-nuggets.md](../catalogo-nuggets.md) |

## Sección 1: Concepto

El **Teléfono** es un número telefónico con su indicativo internacional de marcación, en el estándar E.164: la forma en que una persona o empresa puede ser contactada por voz o mensajería desde cualquier país.

**Evaluación de nombre (criterios 1 y 3 de la gobernanza):** `Telefono` se mantiene sin calificador. A diferencia de "identificación" (todo registro tiene una) o "dirección" (también significa cargo/área), en el ERP no hay otro concepto que dispute la palabra "teléfono"; y la estructura misma del VO (indicativo de país obligatorio) lo distingue de numeraciones internas como extensiones, que no son `Telefono`.

**Origen del concepto:** VO `Telefono` del modelo de Terceros v1.0 (sección 3.3.4). El atributo `preferido` **no se hereda**: marca la relación del teléfono con el contacto que lo posee (una preferencia dentro de una colección), no una cualidad del número — vive en el consumidor, igual que el `tipoUso` de las direcciones.

## Sección 2: Atributos

| Atributo | Tipo | Obligatorio | Descripción |
|----------|------|:-----------:|-------------|
| `indicativoPais` | string | Sí | Código internacional de marcación en formato E.164: `+` seguido de 1–3 dígitos (ej: `+57`, `+1`, `+507`). |
| `numero` | string | Sí | Número telefónico, solo dígitos, sin separadores. |

El Nugget es inmutable. **Igualdad:** dos `Telefono` son iguales si coinciden `(indicativoPais, numero)`.

## Sección 3: Reglas de validación

| Regla | Descripción |
|-------|-------------|
| `[V01]` | **Indicativo válido:** inicia con `+` seguido de 1–3 dígitos, y existe en el catálogo embebido de indicativos por país. Nota: un indicativo puede pertenecer a varios países (`+1` cubre EE.UU., Canadá y el Caribe) — el Nugget valida que el indicativo exista, no infiere el país. |
| `[V02]` | **Número:** solo dígitos, no vacío. La normalización elimina espacios, guiones y paréntesis de la captura antes de validar. |
| `[V03]` | **Longitud E.164:** dígitos del indicativo (sin `+`) más dígitos del número, entre 8 y 15 en total. |

**Validación por país (heredada como pendiente de Terceros):** las longitudes válidas por país no están en el catálogo — mientras no existan, aplica la validación E.164 genérica de `[V03]`. *Sugerencia de implementación:* los metadatos públicos de numeración por país (estilo libphonenumber) pueden servir de fuente si el custodio decide cerrar este pendiente; la regla del Nugget seguiría siendo la misma, con datos más finos.

## Sección 4: Operaciones

| Operación | Descripción |
|-----------|-------------|
| `esIgualA(otro)` | Igualdad por `(indicativoPais, numero)`. |
| `presentacion()` | `+57 300 1234567` — agrupación simple para lectura; los formatos locales de marcación son de cada interfaz. |

## Sección 5: Datos embebidos

Ninguno propio — los indicativos se consultan del catálogo del Nugget [`Pais`](../pais/especificacion.md) (`indicativoTelefonico`), la fuente única de datos de país dentro del paquete.

## Sección 6: Ejemplos

| Entrada | Resultado | Razón |
|---------|-----------|-------|
| `+57` / `300 123-4567` | ✅ `(+57, 3001234567)` | Normalización elimina separadores; 12 dígitos totales. |
| `+1` / `8095551234` | ✅ | `+1` válido (EE.UU./Canadá/Caribe); el Nugget no infiere el país. |
| `57` / `3001234567` | ❌ | `[V01]`: el indicativo debe iniciar con `+`. |
| `+57` / `12345` | ❌ | `[V03]`: 7 dígitos totales, fuera del rango E.164 (8–15). |
| `+999` / `3001234567` | ❌ | `[V01]`: indicativo inexistente en el catálogo. |

## Sección 7: Fuera de responsabilidad

| Fuera del Nugget | Responsable |
|------------------|-------------|
| Marca de **preferido** dentro de una colección de teléfonos. | El consumidor (invariante de colección del Contacto en Terceros). |
| **Verificación de existencia/propiedad** del número (SMS, llamada). | Capacidad externa no bloqueante, fuera del alcance F1. |
| **Tipo de teléfono** (móvil/fijo/fax) si algún consumidor lo llega a necesitar. | La asociación del consumidor. |
| Extensiones internas de conmutador. | No son `Telefono` — numeración interna del consumidor. |

## Sección 8: Consumidores

Previstos según la [matriz del catálogo](../catalogo-nuggets.md#matriz-de-consumidores): Terceros (teléfonos del contacto), CXC/Facturación.

## Sección 9: Revisión pendiente

| # | Pendiente | Owner | Criterio de cierre |
|---|----------|-------|--------------------|
| ~~P1~~ | ✅ **Cerrado (jun-2026):** los indicativos se consultan del catálogo del Nugget `Pais` — sin archivo propio. | — | — |
| P2 | Longitudes de numeración por país (pendiente heredado de Terceros, sin ownership allá): decidir si se cierra con metadatos públicos de numeración o se ratifica la validación E.164 genérica. | Custodio | Decisión documentada en los datos. |

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.1 | Junio 2026 | Borrador inicial desde el VO de Terceros v1.0. Evaluación de nombre: se mantiene `Telefono`. `preferido` sale del VO (relación del consumidor). 3 reglas, igualdad por (indicativo, número), indicativos embebidos por producir, 2 pendientes. |
