# Nugget `DivisionTerritorial` — Especificación

| | |
|---|---|
| **Estado** | En especificación — borrador para revisión |
| **Versión** | 0.1 |
| **Gobernanza** | [gobernanza-nuggets.md](../gobernanza-nuggets.md) · **Catálogo:** [catalogo-nuggets.md](../catalogo-nuggets.md) |

## Sección 1: Concepto

La **División Territorial** es la referencia a una subdivisión político-administrativa de un país (departamento, provincia, comarca, municipio, distrito) dentro de su jerarquía oficial.

Es la **fuente única de la jerarquía territorial dentro del paquete**, con dos consumidores de naturaleza distinta: `DireccionFisica` la compone para estructurar direcciones, e **Impuestos la consume directamente** para resolver jurisdicción fiscal (los tributos municipales — ICA, RICA — se resuelven por municipio, criticidad alta). Separarla de `DireccionFisica` evita que la jurisdicción fiscal dependa del Nugget de direcciones — el mismo patrón por el que `Pais` se separó de sus consumidores.

**Paso por los filtros:** transversal (direcciones, jurisdicción fiscal, reportes por territorio); sin identidad ni ciclo de vida (códigos oficiales inmutables: DIVIPOLA/DANE para CO); autocontenida y estable (cambios excepcionales — nuevo municipio, reestructuración — por versión del producto); mínima; su fuente es el catálogo de Datos de Referencia, producido por el custodio (producción de catálogos).

**Origen:** catálogo de divisiones territoriales de Datos de Referencia v1.0 (3 archivos por país).

## Sección 2: Atributos

| Atributo | Tipo | Obligatorio | Descripción |
|----------|------|:-----------:|-------------|
| `pais` | string (ISO 3166-1 alfa-2) | Sí | País de la división (Nugget `Pais`). |
| `codigo` | string | Sí | Código oficial de la división, único dentro del país (numérico DIVIPOLA para CO). |

El Nugget es inmutable. **Igualdad:** por `(pais, codigo)`. Nombre, nivel y división superior se consultan del catálogo embebido mediante las operaciones.

## Sección 3: Reglas de validación

| Regla | Descripción |
|-------|-------------|
| `[V01]` | **País válido:** según el Nugget `Pais`. |
| `[V02]` | **Código válido:** existe en el catálogo embebido de divisiones del país y está activo. En países sin divisiones embebidas, la construcción falla — el consumidor usa su modo genérico (texto libre), no este Nugget. |

## Sección 4: Operaciones

| Operación | Descripción |
|-----------|-------------|
| `nombre()` | Nombre geográfico oficial (no se traduce — convención l10n del proyecto). |
| `nivel()` | Nivel jerárquico: `departamento`, `provincia`, `comarca`, `municipio`, `distrito` (y `corregimiento` cuando se incorpore — pendiente P1). |
| `superior()` | La `DivisionTerritorial` padre en la jerarquía, o ninguna si es el nivel más alto. |
| `perteneceA(otra)` | `true` si `otra` está en la cadena de superiores (un municipio pertenece a su departamento). Es la operación con que `DireccionFisica` valida la coherencia de niveles (`[V02]` de esa especificación) y con que los reportes agrupan por territorio. |
| `divisionesDe(pais, nivel)` / `hijasDe(division)` | Consultas de catálogo para las listas de captura (los municipios de Antioquia). |

## Sección 5: Datos embebidos

| Archivo | Contenido | Fuente |
|---------|-----------|--------|
| `divisiones-territoriales-co.json` | 1.188 — 33 departamentos + municipios (códigos DIVIPOLA/DANE). | `compartido/datos-referencia/catalogos/` — completo. |
| `divisiones-territoriales-do.json` | 221 — provincias + municipios. | ídem |
| `divisiones-territoriales-pa.json` | 108 — 10 provincias + 3 comarcas + 81 distritos. | ídem — ver pendiente P1 (corregimientos). |

Países sin archivo: sin divisiones embebidas — los consumidores aplican su modo genérico hasta la habilitación productiva del país (alineado con `[D7]` de Impuestos).

## Sección 6: Ejemplos

| Entrada | Resultado |
|---------|-----------|
| `(CO, 05001)` | ✅ — `nombre()` = "Medellín", `nivel()` = municipio, `superior()` = `(CO, 05)` Antioquia. |
| `(CO, 05001).perteneceA((CO, 05))` | `true` — insumo de `[V02]` de `DireccionFisica` y de la resolución de jurisdicción. |
| `(CO, 99999)` | ❌ `[V02]`: código inexistente en DIVIPOLA. |
| `(US, NY)` | ❌ `[V02]`: US sin divisiones embebidas — el consumidor usa modo genérico. |

## Sección 7: Fuera de responsabilidad

| Fuera del Nugget | Responsable |
|------------------|-------------|
| **La dirección** que usa la división. | Nugget `DireccionFisica` (compone sobre este). |
| **La jurisdicción fiscal** y sus atributos tributarios. | Impuestos (`JurisdiccionFiscal` referencia divisiones de este catálogo). |
| **Códigos postales** asociados al territorio. | Datos de `DireccionFisica` (existencia = dato vivo no bloqueante). |

## Sección 8: Consumidores

Previstos: `DireccionFisica` (composición), Impuestos (resolución de jurisdicción — ICA/RICA por municipio), Terceros y CXC vía direcciones, Estructura Organizacional (ubicación de sedes), reportes por territorio.

## Sección 9: Revisión pendiente

| # | Pendiente | Owner | Criterio de cierre |
|---|----------|-------|--------------------|
| P1 | **Corregimientos de Panamá** (transferido desde `DireccionFisica` P2): la factura electrónica panameña exige el corregimiento y el catálogo llega a distrito. Incorporar el nivel (~700 entradas) o documentar el diferimiento a la integración de Emisión Electrónica PA. | Custodio + consultor fiscal PA | Nivel resuelto o decisión documentada. |

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.1 | Junio 2026 | Borrador inicial. Separado de `DireccionFisica` como fuente única de la jerarquía territorial (dos consumidores de naturaleza distinta: direcciones y jurisdicción fiscal). VO `(pais, codigo)` + jerarquía consultable (`perteneceA`, `superior`). Catálogos CO/DO/PA embebidos completos; corregimientos PA pendientes (P1, transferido). |
