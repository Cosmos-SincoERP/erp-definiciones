# Nugget `Pais` — Especificación

| | |
|---|---|
| **Estado** | En especificación — borrador para revisión |
| **Versión** | 0.1 |
| **Gobernanza** | [gobernanza-nuggets.md](../gobernanza-nuggets.md) · **Catálogo:** [catalogo-nuggets.md](../catalogo-nuggets.md) |

## Sección 1: Concepto

El **País** es la referencia a un país del mundo mediante su código ISO 3166-1 alfa-2, con sus datos estables asociados: nombre, indicativo telefónico y moneda principal.

Es la **fuente única de datos de país dentro del paquete de Nuggets**: `IdentificacionLegal` (`[V01]`), `DireccionFisica` (`[V01]`) y `Telefono` (indicativos en `[V01]`) validan contra el catálogo que este Nugget embebe — un solo archivo de países en el paquete, no una copia por Nugget.

**Paso por los filtros de admisión:** transversal (lo consumen todos los sub-dominios y tres Nuggets); sin identidad ni ciclo de vida (el código es un valor semántico inmutable — política ya establecida para los catálogos del producto); autocontenido y estable (195 países cambian por versión del producto, no por operación); mínimo (un concepto); el catálogo de Datos de Referencia es su fuente, no su duplicado — Datos de Referencia lo produce en su capacidad de producción de catálogos.

**Origen:** catálogo `paises.json` de Datos de Referencia v1.0 (195 países, ISO 3166-1, con `indicativoTelefonico` y `monedaPrincipal`).

## Sección 2: Atributos

| Atributo | Tipo | Obligatorio | Descripción |
|----------|------|:-----------:|-------------|
| `codigo` | string (ISO 3166-1 alfa-2) | Sí | Código del país en mayúsculas (ej: `CO`, `DO`, `PA`, `US`). |

El Nugget es inmutable. **Igualdad:** por `codigo`. Los demás datos (nombre, indicativo, moneda principal) no son atributos del valor — se **consultan** del catálogo embebido mediante las operaciones.

## Sección 3: Reglas de validación

| Regla | Descripción |
|-------|-------------|
| `[V01]` | **Normalización:** mayúsculas, sin espacios. |
| `[V02]` | **Código válido:** existe en el catálogo embebido y está activo. |

## Sección 4: Operaciones

| Operación | Descripción |
|-----------|-------------|
| `nombre()` | Nombre del país en español (la traducción a otros idiomas es del frontend — convención i18n del proyecto). |
| `indicativoTelefonico()` | Indicativo de marcación (ej: `+57`). Consumido por `Telefono`. |
| `monedaPrincipal()` | Código ISO 4217 de la moneda principal (ej: `COP`). Valor por defecto para capturas; no restringe — un tercero colombiano puede operar en USD. |

## Sección 5: Datos embebidos

| Archivo | Contenido | Fuente |
|---------|-----------|--------|
| `paises.json` | 195 países: `codigo`, `nombre`, `indicativoTelefonico`, `monedaPrincipal`, `activo`. | `compartido/datos-referencia/catalogos/paises.json` — completo, sin extensión requerida. Reemplaza las copias parciales previstas en `IdentificacionLegal` (`datos/paises.json`) y `Telefono` (`indicativos-telefonicos.json`): dentro del paquete hay **un solo** catálogo de países. |

## Sección 6: Ejemplos

| Entrada | Resultado |
|---------|-----------|
| `co` | ✅ `CO` (normalizado) — `nombre()` = "Colombia", `indicativoTelefonico()` = `+57`, `monedaPrincipal()` = `COP`. |
| `XX` | ❌ `[V02]`: no existe en ISO 3166-1. |

## Sección 7: Fuera de responsabilidad

| Fuera del Nugget | Responsable |
|------------------|-------------|
| **Divisiones territoriales** del país. | Nugget `DireccionFisica` (sus datos embebidos). |
| **Habilitación productiva** del país en el ERP (qué países están operativos). | Configuración del producto / `[D7]` de Impuestos. |
| **Tasas de cambio** de la moneda principal. | Datos de Referencia (dato vivo, Sync). |

## Sección 8: Consumidores

Todos los sub-dominios, directa o indirectamente: tres Nuggets componen sobre este (`IdentificacionLegal`, `DireccionFisica`, `Telefono`), y cualquier dominio que capture un país lo usa directo.

## Sección 9: Revisión pendiente

Ninguna — el catálogo fuente está completo. Listo para promoción con la revisión editorial del custodio.

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.1 | Junio 2026 | Borrador inicial. VO por código ISO 3166-1 alfa-2 + catálogo embebido completo (195). Fuente única de datos de país dentro del paquete — los demás Nuggets componen sobre este. Sin pendientes. |
