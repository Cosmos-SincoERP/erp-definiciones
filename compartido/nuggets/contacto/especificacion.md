# Nugget `Contacto` — Especificación

| | |
|---|---|
| **Estado** | En especificación — borrador para revisión |
| **Versión** | 0.1 |
| **Gobernanza** | [gobernanza-nuggets.md](../gobernanza-nuggets.md) |
| **Catálogo** | [catalogo-nuggets.md](../catalogo-nuggets.md) |

## Tabla de contenido

1. [Concepto](#sección-1-concepto)
2. [Atributos](#sección-2-atributos)
3. [Igualdad y normalización](#sección-3-igualdad-y-normalización)
4. [Reglas de validación](#sección-4-reglas-de-validación)
5. [Operaciones](#sección-5-operaciones)
6. [Datos embebidos](#sección-6-datos-embebidos)
7. [Ejemplos](#sección-7-ejemplos)
8. [Fuera de responsabilidad](#sección-8-fuera-de-responsabilidad)
9. [Consumidores](#sección-9-consumidores)
10. [Revisión pendiente](#sección-10-revisión-pendiente)

---

## Sección 1: Concepto

El **Contacto** es la persona a través de la cual se mantiene la relación con un tercero: su nombre, el rol que cumple en esa relación (representante legal, tesorero, comercial…) y sus medios de comunicación. Es la estructura con la que **todos los sub-dominios capturan contactos de la misma forma** — y esa uniformidad es lo que permite que la bodega de Terceros los consolide por tercero sin traducciones por fuente.

**Por qué es un Nugget y no una entidad:** el contacto *como dato* es comparable por valor y no necesita identificador ni ciclo de vida propio dentro del paquete. Lo que sí tiene ciclo de vida — crearlo, actualizarlo, inactivarlo, marcarlo como principal, garantizar que no se repita en una colección — pertenece **al consumidor que lo captura**, igual que el tipo de uso pertenece al consumidor de una `DireccionFisica`. Es el mismo criterio que resolvió el filtro 2 (sin identidad) para las direcciones.

**Evaluación de nombre (criterios de la gobernanza):** `Contacto` se mantiene sin calificador. En el ERP la palabra tiene un solo significado — la persona de contacto. El sub-dominio que pudo llamarse "Contactos" se llama Terceros precisamente para no confundir el todo con la parte.

**Origen del concepto:** entidad `Contacto` del modelo de Terceros v1.0, generalizada como pieza del paquete por la reescritura de Terceros v2.0 (propuesta y aceptación en el issue #35). De ese origen hereda dos criterios que esta especificación hace propios: el nombre es opcional al capturar (Sección 2) y un contacto sin medios de comunicación no es un contacto (`[V01]`).

---

## Sección 2: Atributos

| Atributo | Tipo | Obligatorio | Descripción |
|----------|------|:-----------:|-------------|
| `nombre` | string | No | Nombre de la persona. **Opcional al capturar**: en la operación real el medio suele conocerse antes que el nombre (ej: el correo de pagos del proveedor); se permite registrar sin nombre y completarlo después. Normalizado según la Sección 3. |
| `rolContacto` | string (código del vocabulario embebido) | Sí | El rol de la persona **en la relación con el tercero**: código del vocabulario de la Sección 6. No es un cargo laboral (eso lo sabría RRHH del empleado del proveedor, no este ERP) — es para qué sirve este contacto en la relación. |
| `correos` | colección de [`CorreoElectronico`](../correo-electronico/especificacion.md) | 0..N | Correos de la persona. Cada uno valida con las reglas de su propio Nugget. |
| `telefonos` | colección de [`Telefono`](../telefono/especificacion.md) | 0..N | Teléfonos de la persona. Cada uno valida con las reglas de su propio Nugget. |

El Nugget es **inmutable**: cualquier cambio implica construir una nueva instancia. Las reglas de la Sección 4 aplican al construir.

> **El contacto es universal:** a diferencia de la identificación o las direcciones, su estructura no varía por país — un representante legal panameño y uno colombiano se capturan igual. Por eso esta especificación no tiene perfiles por país.

---

## Sección 3: Igualdad y normalización

**Normalización del nombre** (al construir): espacios eliminados al inicio y al final; espacios intermedios repetidos colapsados a uno. No se altera el uso de mayúsculas y minúsculas — el nombre se almacena como la persona lo escribe.

**Igualdad:** dos `Contacto` son iguales si y solo si coinciden **todos** sus atributos:

- `nombre` normalizado, comparado **sin distinguir mayúsculas, minúsculas ni tildes** ("María Pérez" = "maria perez"); la ausencia de nombre solo es igual a otra ausencia.
- `rolContacto` (código exacto).
- Los **conjuntos** de `correos` y de `telefonos` — cada elemento compara con la igualdad de su propio Nugget; el orden de la colección no importa.

> La igualdad sirve para detectar repeticiones dentro de una colección y para que la consolidación de la bodega reconozca el mismo contacto informado dos veces. **No pretende identificar personas**: dos contactos con igual correo pero distinto rol son instancias distintas (la misma persona puede ser comercial y contacto de facturación — son dos relaciones).

---

## Sección 4: Reglas de validación

Todas las reglas se evalúan al construir la instancia, en el orden indicado, **sin salir del proceso** (filtro 3 de la gobernanza): solo consultan el vocabulario embebido de la Sección 6.

| Regla | Descripción |
|-------|-------------|
| `[V01]` | **Al menos un medio de comunicación:** la suma de elementos de `correos` y `telefonos` debe ser ≥ 1. Un contacto sin forma de contactarlo no es un contacto — el dato existe para comunicarse. |
| `[V02]` | **Rol de contacto válido:** `rolContacto` debe existir en el vocabulario embebido y estar `activo`. Un código fuera del vocabulario rechaza la construcción (si la relación no encaja en ningún código, existe `otro`). |
| `[V03]` | **Medios válidos por composición:** cada elemento de `correos` se construye con el Nugget `CorreoElectronico` y cada elemento de `telefonos` con el Nugget `Telefono` — sus reglas aplican completas. Este Nugget **no re-valida formatos**: compone. |
| `[V04]` | **Nombre no vacío cuando se informa:** si `nombre` viene, tras la normalización de la Sección 3 no puede quedar vacío. Una cadena de espacios es señal de captura errónea, no un nombre. |
| `[V05]` | **Sin medios repetidos:** dentro de `correos` no puede haber dos elementos iguales (igualdad del Nugget `CorreoElectronico`); ídem en `telefonos`. La repetición dentro de la misma instancia es ruido de captura. |

---

## Sección 5: Operaciones

| Operación | Descripción |
|-----------|-------------|
| `esIgualA(otro)` | Igualdad por valor según la Sección 3. |
| `tieneNombre()` | `true` cuando el nombre fue informado. Permite al consumidor implementar la recomendación de completarlo (ej: advertencia de captura no bloqueante). |
| `medios()` | La lista combinada de correos y teléfonos, para presentación o verificación de `[V01]` por el consumidor. |
| `presentacion()` | `"María Pérez · Representante legal · ✉ pagos@xyz.com · ☎ +57 300 1234567"` — nombre (o `"(sin nombre)"`), el rol del vocabulario y los medios con la presentación de sus propios Nuggets. Los formatos finales de pantalla son de cada interfaz. |

---

## Sección 6: Datos embebidos

Los datos viajan dentro del paquete (carpeta `datos/`) y los produce **Datos de Referencia** en su capacidad de producción de catálogos (rol custodio de la gobernanza). Se congelan en cada versión del paquete; actualizarlos es una versión **menor**.

| Archivo | Contenido | Estado |
|---------|-----------|--------|
| [`datos/roles-contacto.json`](datos/roles-contacto.json) | **Vocabulario de roles de contacto** — 7 códigos (tabla abajo), con nombre de presentación y marca `activo`. Es **un solo vocabulario para todo el ERP**: el que los dominios usan al capturar y el que la bodega de Terceros recibe en el evento estándar de rol. Extensible por versión del producto, no por configuración del cliente. | ✅ Producido (jun-2026, 7 entradas) |

| Código | Rol del contacto |
|--------|------------------|
| `representante_legal` | Representante legal |
| `tesorero` | Tesorero |
| `comercial` | Comercial |
| `tecnico` | Técnico |
| `facturacion` | Contacto de facturación |
| `notificaciones` | Contacto de notificaciones |
| `otro` | Otro |

---

## Sección 7: Ejemplos

El contacto no varía por país (Sección 2) — los ejemplos son universales:

| Entrada | Resultado | Razón |
|---------|-----------|-------|
| (sin nombre) / `tesorero` / correo `pagos@xyz.com` | ✅ | El nombre es opcional; hay un medio (`[V01]`). |
| `María  Pérez ` / `representante_legal` / correo + teléfono | ✅ `"María Pérez"` | Normalización de espacios (Sección 3); contacto completo. |
| `Juan Gómez` / `gerente` / teléfono | ❌ | `[V02]`: `gerente` no existe en el vocabulario — la relación se captura con `otro` si ningún código encaja. |
| `Ana Ruiz` / `comercial` / (sin medios) | ❌ | `[V01]`: ningún medio de comunicación. |
| `"   "` / `tecnico` / correo | ❌ | `[V04]`: nombre informado pero vacío tras normalizar. |
| `Luis Mora` / `facturacion` / correos: `a@x.com`, `A@X.com` | ❌ | `[V05]`: correo repetido (la igualdad de `CorreoElectronico` normaliza a minúsculas). |
| Misma persona: `comercial` con ✉ y `facturacion` con el mismo ✉ | ✅ (dos instancias) | Son dos relaciones distintas — la igualdad no identifica personas (Sección 3). |

---

## Sección 8: Fuera de responsabilidad

| Fuera del Nugget | Responsable |
|------------------|-------------|
| **Marca de principal** dentro de una colección de contactos. | El consumidor — es un atributo de la relación, no del contacto (criterio transversal del catálogo: igual que `preferido` en teléfonos y `tipoUso` en direcciones). |
| **Ciclo de vida** del contacto (crear, actualizar, inactivar). | El dominio que lo captura, en su propio registro del tercero. |
| **Unicidad dentro de una colección** (no repetir el mismo contacto en un registro). | Invariante del consumidor — `[V05]` solo cubre medios repetidos dentro de una instancia. |
| **Reglas de "datos mínimos" más exigentes** (ej: el principal debe tener correo **y** teléfono). | El consumidor que designa al principal define qué le exige. |
| **Verificación de que la persona exista o siga en el cargo.** | Capacidad externa no bloqueante, fuera del alcance F1. |
| **Consolidación de contactos por tercero** y su presentación en la ficha. | La bodega de Terceros (los recibe en el evento estándar de rol). |

---

## Sección 9: Consumidores

Previstos según la [matriz del catálogo](../catalogo-nuggets.md#matriz-de-consumidores):

| Consumidor | Uso |
|------------|-----|
| **OXP** (primera adopción — issue #38) | Captura los contactos de su Proveedor y los emite en el evento estándar de rol. |
| **Bodega de Terceros** | Recibe los contactos en el evento de rol (como `{ contacto, esPrincipal }`), los consolida y los muestra en la ficha. |
| **CXC / RRHH** *(futuros)* | Mismo patrón con sus registros del tercero. |
| **Emisión Electrónica** | Lee el representante legal desde la ficha consolidada (no captura contactos). |

---

## Sección 10: Revisión pendiente

*Ninguno.* El vocabulario de roles de contacto se produjo con los 7 códigos vigentes (decisión de la revisión del PR #40); cualquier ajuste futuro entra como versión menor del paquete, por el proceso normal de la gobernanza.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.1 | Junio 2026 | Borrador inicial — veredicto de aceptación del issue #35 (surge de Terceros v2.0, #33). Especificación autocontenida: estructura pura del contacto (nombre opcional + rol de contacto + medios por composición de `CorreoElectronico` y `Telefono`), igualdad por valor sin identificar personas, 5 reglas, 4 operaciones, **vocabulario de 7 roles de contacto producido** (`datos/roles-contacto.json`, decisión de la revisión del PR #40), universalidad declarada (sin perfiles por país), sin pendientes. El ciclo de vida, la marca de principal y la unicidad en colecciones quedan en el consumidor. |
