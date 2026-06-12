# Nugget `Contacto` — Especificación

| | |
|---|---|
| **Estado** | En especificación — borrador para revisión |
| **Versión** | 0.1 |
| **Gobernanza** | [gobernanza-nuggets.md](../gobernanza-nuggets.md) · **Catálogo:** [catalogo-nuggets.md](../catalogo-nuggets.md) |

## Sección 1: Concepto

El **Contacto** es la persona a través de la cual se mantiene la relación con un tercero: su nombre, el rol que cumple en la relación (representante legal, tesorero, comercial…) y sus medios de comunicación. Es la estructura con la que **todos los dominios capturan contactos de la misma forma**, para que la bodega de Terceros pueda consolidarlos por tercero.

**Evaluación de nombre (criterios de la gobernanza):** `Contacto` se mantiene sin calificador. En el ERP la palabra tiene un solo significado — la persona de contacto; el sub-dominio que pudo llamarse "Contactos" se llama Terceros precisamente para no confundir el todo con la parte (evaluación de nomenclatura del alcance de Terceros, Sección 1).

**Origen del concepto:** entidad `Contacto` del modelo de Terceros v1.0 (componente interno del agregado, con FSM propia). En el Nugget queda **solo la estructura como dato** — el ciclo de vida (activo/inactivo), la marca de principal y la unicidad dentro de una colección son del consumidor que lo captura. Propuesto y aceptado en el issue #35 (reescritura de Terceros v2.0, #33).

## Sección 2: Atributos

| Atributo | Tipo | Obligatorio | Descripción |
|----------|------|:-----------:|-------------|
| `nombre` | string | No | Nombre de la persona. Opcional al capturar (criterio R23 del alcance de Terceros: se permite registrar el medio sin el nombre y se recomienda completarlo después). |
| `rolContacto` | string (código del vocabulario) | Sí | El rol de la persona en la relación: código del vocabulario embebido (Sección 5). |
| `correos` | colección de [`CorreoElectronico`](../correo-electronico/especificacion.md) | 0..N | Correos de la persona. |
| `telefonos` | colección de [`Telefono`](../telefono/especificacion.md) | 0..N | Teléfonos de la persona. |

El Nugget es inmutable. **Igualdad:** dos `Contacto` son iguales si coinciden todos sus atributos (nombre normalizado, rol de contacto y los conjuntos de correos y teléfonos).

## Sección 3: Reglas de validación

| Regla | Descripción |
|-------|-------------|
| `[V01]` | **Al menos un medio de comunicación:** la suma de correos y teléfonos debe ser ≥ 1. Un contacto sin forma de contactarlo no es un contacto (hereda el criterio de la v1.0 de Terceros). |
| `[V02]` | **Rol de contacto válido:** `rolContacto` existe en el vocabulario embebido (Sección 5). |
| `[V03]` | **Medios válidos por composición:** cada correo valida con las reglas del Nugget `CorreoElectronico` y cada teléfono con las del Nugget `Telefono`. Este Nugget no re-valida formatos — compone. |
| `[V04]` | **Nombre no vacío cuando viene:** si se informa, no puede ser cadena vacía; los espacios se normalizan (extremos e intermedios repetidos). |

## Sección 4: Operaciones

| Operación | Descripción |
|-----------|-------------|
| `esIgualA(otro)` | Igualdad por valor de todos los atributos. |
| `presentacion()` | `"María Pérez · Representante legal · ✉ ☎"` — nombre (o "(sin nombre)"), rol y los medios disponibles. Los formatos de cada medio son de sus Nuggets. |

## Sección 5: Datos embebidos

**Vocabulario de roles de contacto** (`datos/roles-contacto.json`, por producir por la producción de catálogos de Datos de Referencia). Es el mismo vocabulario del catálogo 6.2 del modelo de Terceros v2.0 — un solo vocabulario para todos los dominios (`[R22]` del alcance de Terceros). Extensible por versión del producto, no por configuración del cliente.

| Código | Rol del contacto |
|--------|------------------|
| `representante_legal` | Representante legal |
| `tesorero` | Tesorero |
| `comercial` | Comercial |
| `tecnico` | Técnico |
| `facturacion` | Contacto de facturación |
| `notificaciones` | Contacto de notificaciones |
| `otro` | Otro |

## Sección 6: Ejemplos

| Entrada | Resultado | Razón |
|---------|-----------|-------|
| (sin nombre) / `tesorero` / correo `pagos@xyz.com` | ✅ | El nombre es opcional (`[V04]` no aplica); hay un medio (`[V01]`). |
| `María Pérez` / `representante_legal` / correo + teléfono | ✅ | Contacto completo. |
| `Juan Gómez` / `gerente` / teléfono | ❌ | `[V02]`: `gerente` no existe en el vocabulario (usar `otro` si no hay código). |
| `Ana Ruiz` / `comercial` / (sin medios) | ❌ | `[V01]`: ningún medio de comunicación. |
| `"  "` / `tecnico` / correo | ❌ | `[V04]`: nombre informado pero vacío tras normalizar. |

## Sección 7: Fuera de responsabilidad

| Fuera del Nugget | Responsable |
|------------------|-------------|
| **Marca de principal** (`esPrincipal`) dentro de una colección de contactos. | El consumidor — es un atributo de la relación, no del contacto (criterio transversal del catálogo; `[R25]` del alcance de Terceros: principal por rol del tercero). |
| **Ciclo de vida** (activo/inactivo, crear/actualizar/eliminar). | El dominio que captura (`[R24]` del alcance de Terceros). |
| **Unicidad dentro de la colección** (no repetir el mismo contacto en un rol del tercero). | Invariante del consumidor. |
| **Verificación de que la persona exista o siga en el cargo.** | Capacidad externa no bloqueante, fuera del alcance F1. |
| **Consolidación de contactos por tercero.** | La bodega de Terceros (modelo v2.0, eventos de rol). |

## Sección 8: Consumidores

Previstos según la [matriz del catálogo](../catalogo-nuggets.md): **OXP** (contactos del Proveedor — primera adopción, issue #38), CXC y RRHH (futuros, con sus roles del tercero), **bodega de Terceros** (los recibe en el evento estándar de rol y los consolida en la ficha), **Emisión Electrónica** (lee el representante legal desde la ficha consolidada).

## Sección 9: Revisión pendiente

| # | Pendiente | Owner | Criterio de cierre |
|---|----------|-------|--------------------|
| P1 | Ratificar el vocabulario de roles de contacto con el comité de producto (es el heredado de Terceros v1.0 — probablemente pasa sin cambios). | Producto | Vocabulario ratificado antes de producir `datos/roles-contacto.json`. |

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.1 | Junio 2026 | Borrador inicial — veredicto de aceptación del issue #35 (surge de Terceros v2.0, #33). Estructura pura del contacto: nombre opcional + rol de contacto + medios por composición (`CorreoElectronico`, `Telefono`). El ciclo de vida, la marca de principal y la unicidad quedan en el consumidor. 4 reglas, vocabulario de 7 roles de contacto embebido (por producir), 1 pendiente. |
