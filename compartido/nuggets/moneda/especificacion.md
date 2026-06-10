# Nugget `Moneda` — Especificación

| | |
|---|---|
| **Estado** | En especificación — borrador para revisión |
| **Versión** | 0.1 |
| **Gobernanza** | [gobernanza-nuggets.md](../gobernanza-nuggets.md) · **Catálogo:** [catalogo-nuggets.md](../catalogo-nuggets.md) |

## Sección 1: Concepto

La **Moneda** es la referencia a una moneda mediante su código ISO 4217, con sus datos estables: nombre y número de decimales (la precisión con que la moneda representa montos).

**Paso por los filtros de admisión:** transversal (todo dominio que capture o muestre dinero); sin identidad ni ciclo de vida (código semántico inmutable); autocontenida y estable (154 monedas, cambian por versión del producto); mínima (un concepto); su fuente es el catálogo de Datos de Referencia, producido por el taller del custodio.

**Relación con un futuro `ValorMonetario`:** este Nugget aporta la moneda y su precisión; el concepto compuesto monto + moneda quedó **diferido** del catálogo (se re-propondrá al intervenir OXP/Contabilidad — ver diferidos del catálogo). Mientras tanto, los dominios que manejan montos usan `Moneda` para validar la divisa y su escala.

**Origen:** catálogo `monedas.json` de Datos de Referencia v1.0 (154 monedas con `decimales`).

## Sección 2: Atributos

| Atributo | Tipo | Obligatorio | Descripción |
|----------|------|:-----------:|-------------|
| `codigo` | string (ISO 4217) | Sí | Código de la moneda en mayúsculas (ej: `COP`, `USD`, `JPY`). |

El Nugget es inmutable. **Igualdad:** por `codigo`. Nombre y decimales se consultan del catálogo embebido mediante las operaciones.

## Sección 3: Reglas de validación

| Regla | Descripción |
|-------|-------------|
| `[V01]` | **Normalización:** mayúsculas, sin espacios. |
| `[V02]` | **Código válido:** existe en el catálogo embebido y está activo. |

## Sección 4: Operaciones

| Operación | Descripción |
|-----------|-------------|
| `nombre()` | Nombre de la moneda en español. |
| `decimales()` | Precisión de la moneda (`COP`/`USD`: 2; `JPY`/`CLP`: 0; `BHD`: 3). Es el dato con que los consumidores validan la escala de sus montos y redondean sus prorrateos. |
| `validarEscala(monto)` | `true` si los decimales del monto no exceden los de la moneda. Utilidad para los consumidores que manejan montos sin un VO compuesto. |

## Sección 5: Datos embebidos

| Archivo | Contenido | Fuente |
|---------|-----------|--------|
| `monedas.json` | 154 monedas: `codigo`, `nombre`, `decimales`, `activo`. | `compartido/datos-referencia/catalogos/monedas.json` — completo, sin extensión requerida. |

## Sección 6: Ejemplos

| Entrada | Resultado |
|---------|-----------|
| `cop` | ✅ `COP` — `decimales()` = 2. |
| `validarEscala(100.5)` sobre `JPY` | `false` — JPY tiene 0 decimales. |
| `XYZ` | ❌ `[V02]`: no existe en ISO 4217. |

## Sección 7: Fuera de responsabilidad

| Fuera del Nugget | Responsable |
|------------------|-------------|
| **Tasas de cambio / conversión** entre monedas. | Datos de Referencia (dato vivo, Sync) + el consumidor con la TRM del contexto. |
| **Moneda funcional de la empresa.** | Estructura Organizacional / configuración de la empresa. |
| **El monto** y su aritmética. | El consumidor (o el futuro `ValorMonetario` diferido). |

## Sección 8: Consumidores

Previstos: todo dominio que capture montos o divisas — OXP, Contabilidad, CXC/Facturación, Tesorería, Datos de Referencia (tasas de cambio referencian monedas). `Pais.monedaPrincipal()` referencia códigos de este catálogo.

## Sección 9: Revisión pendiente

Ninguna — el catálogo fuente está completo. Listo para promoción con la revisión editorial del custodio.

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.1 | Junio 2026 | Borrador inicial. VO por código ISO 4217 + catálogo embebido completo (154 con `decimales`). Hereda de `ValorMonetario` (diferido) la validación de escala como utilidad. Sin pendientes. |
