# Catálogo de Nuggets

Índice vivo de los Nuggets del ERP. Es la **fuente única** para saber qué conceptos transversales existen, en qué estado están y quién los consume. Toda propuesta nueva se compara contra este catálogo antes de cualquier otro trabajo (filtro 6 de la [gobernanza](gobernanza-nuggets.md)).

**Mantenimiento:** el custodio del catálogo actualiza este documento en cada veredicto de revisión, publicación, adopción o cambio de versión.

---

## Estados de un Nugget

| Estado | Significado |
|--------|-------------|
| `Propuesto` | Existe un issue `tipo: nugget` pendiente de revisión por el custodio. |
| `Aceptado — en especificación` | Pasó los seis filtros; su `especificacion.md` está en redacción. |
| `Publicado` | Especificación completa y incorporado al paquete distribuible. Adoptable por los sub-dominios. |
| `Obsoleto` | Ya no debe adoptarse. El catálogo indica su reemplazo y los consumidores pendientes de migrar. |

---

## Catálogo

| Nugget | Concepto | Estado | Versión | Datos embebidos | Origen previo |
|--------|----------|--------|---------|-----------------|---------------|
| [`IdentificacionLegal`](identificacion-legal/especificacion.md) | Identidad documental de una persona o empresa, **emitida o reconocida por una autoridad**: tipo de documento + número + país (+ dígito de verificación cuando aplica). Es la **clave natural universal** con la que la bodega de Terceros consolida. | Aceptado — en especificación | 0.3 (fuentes oficiales aplicadas; renombrado desde `Identificacion`; pendientes P2–P4) | Tipos de documento de identidad (46), países (195) | Terceros (VO `Identificacion`) + Datos de Referencia (reglas de formato y DV) |
| [`DireccionFisica`](direccion-fisica/especificacion.md) | Dirección de un lugar en el territorio de un país: división territorial estructurada (vía Nugget `DivisionTerritorial`) + línea de dirección canónica (+ captura estructurada opcional donde hay nomenclatura codificada). Cubre los usos fiscal, comercial, de sedes y de correspondencia. | Aceptado — en especificación | 0.1 (borrador; hereda el servicio de Direcciones; 4 pendientes — corregimientos PA transferido a `DivisionTerritorial`) | Perfiles de 5 países, 21 tipos de vía CO, 16 complementos, 5 tipos de uso, 248 códigos postales CO (divisiones vía `DivisionTerritorial`) | Servicio de Direcciones v1.0 (reemplazado por este Nugget — paralelo en su Sección 8) |
| [`Telefono`](telefono/especificacion.md) | Número telefónico con indicativo internacional, validado en formato E.164. Nombre ratificado en la evaluación de nomenclatura (jun-2026): el indicativo obligatorio lo distingue por estructura de numeraciones internas. | Aceptado — en especificación | 0.1 (borrador) | Indicativos telefónicos por país | Terceros (VO `Telefono`) + Datos de Referencia (`indicativoTelefonico` en países) |
| [`CorreoElectronico`](correo-electronico/especificacion.md) | Dirección de correo electrónico con validación de formato (RFC 5322, ≤254, normalizada a minúsculas). | Aceptado — en especificación | 0.1 (borrador; sin pendientes) | — | Terceros (VO `CorreoElectronico`) |
| [`Pais`](pais/especificacion.md) | Referencia a un país por código ISO 3166-1 alfa-2, con nombre, indicativo telefónico y moneda principal consultables. **Fuente única de datos de país dentro del paquete**: `IdentificacionLegal`, `DireccionFisica` y `Telefono` componen sobre este Nugget. | Aceptado — en especificación | 0.1 (borrador; sin pendientes) | Países (195, con indicativo y moneda principal) | Datos de Referencia (`paises.json`) |
| [`Moneda`](moneda/especificacion.md) | Referencia a una moneda por código ISO 4217, con nombre y decimales consultables (`validarEscala()` para los montos de los consumidores). | Aceptado — en especificación | 0.1 (borrador; sin pendientes) | Monedas (154, con decimales) | Datos de Referencia (`monedas.json`) |
| [`DivisionTerritorial`](division-territorial/especificacion.md) | Subdivisión político-administrativa de un país dentro de su jerarquía oficial (`perteneceA()`, `superior()`). **Fuente única de la jerarquía territorial en el paquete**: `DireccionFisica` compone sobre él e Impuestos resuelve jurisdicción fiscal directamente. | Aceptado — en especificación | 0.1 (borrador; 1 pendiente: corregimientos PA) | Divisiones territoriales CO (1.188), DO (221), PA (108) | Datos de Referencia (3 archivos por país) |
| [`Contacto`](contacto/especificacion.md) | La persona a través de la cual se mantiene la relación con un tercero: nombre opcional + rol de contacto (vocabulario embebido) + medios por composición (`CorreoElectronico`, `Telefono`). **Solo la estructura como dato**: el ciclo de vida, la marca de principal y la unicidad en la colección son del consumidor. La estructura con la que todos los dominios capturan contactos igual, para que la bodega de Terceros consolide. | Aceptado — en especificación | 0.1 (borrador; sin pendientes) | Vocabulario de roles de contacto (7, producido) | Terceros v1.0 (entidad `Contacto`) — propuesto en el issue #35 (Terceros v2.0, #33) |

> Los enlaces a `especificacion.md` quedarán activos a medida que se redacte cada especificación (paso 3 de la gobernanza).

---

## Candidatos en evaluación

Conceptos identificados que aún no tienen veredicto. No se adoptan ni se especifican hasta que el custodio resuelva su issue.

| Candidato | Concepto | Estado de la evaluación |
|-----------|----------|-------------------------|
| *(ninguno)* | — | — |

---

## Nuggets diferidos

Conceptos que pasaron los filtros y llegaron a borrador, pero quedaron fuera del alcance del frente actual (Terceros / Direcciones / Datos de Referencia). Se re-proponen al intervenir sus sub-dominios consumidores — el conocimiento del borrador se conserva aquí para no rehacerlo.

| Nugget | Diferido | Re-proponer al intervenir | Decisiones del borrador a conservar |
|--------|----------|---------------------------|-------------------------------------|
| `ValorMonetario` | Jun-2026 | OXP / Contabilidad | Núcleo `(monto, moneda)` — la TRM y el monto funcional son composición local del consumidor (la TRM es dato vivo); aritmética solo entre la misma moneda con falla explícita; escala validada contra los decimales de la moneda (capacidad que hoy presta `Moneda.validarEscala()`); negativos permitidos, el signo lo gobierna el consumidor. |
| `Vigencia` | Jun-2026 | Impuestos | Límites **inclusivos** (pendiente de ratificar con Impuestos, que nunca lo declaró), granularidad día, operaciones `estaVigenteA(fecha)` y `solapaCon(otra)`; el no-solape es invariante del agregado dueño, no del VO. |

---

## Propuestas rechazadas

Memoria del catálogo para no re-evaluar lo mismo dos veces.

| Propuesta | Fecha | Razón del rechazo |
|-----------|-------|-------------------|
| `InformacionTercero` | Jun-2026 | **Resuelto como composición local al intervenir OXP** (issue #38, `[D32]` de su modelo): es (identificación legal + razón social) — la identificación ya es pieza del paquete con todas las reglas; la razón social es texto sin validación propia. Empaquetar la pareja no aportaría reglas ni datos (filtros 5/6 frente a `IdentificacionLegal`). Cada consumidor la compone localmente: OXP la copia de su agregado `Proveedor`; el contrato OXP→Contabilidad ya la trataba así (precedente del patrón). |

---

## Matriz de consumidores

Adopción **prevista** según los modelos de dominio vigentes; cada celda se confirma (✓) cuando el sub-dominio adopta el Nugget en implementación. Antes de publicar un cambio mayor, el custodio consulta esta matriz para conocer el impacto.

| Nugget | Terceros (bodega) | OXP | Impuestos | Contabilidad | CXC / Facturación | Estructura Org. | Tesorería |
|--------|:-----------------:|:---:|:---------:|:------------:|:------------------:|:---------------:|:---------:|
| `IdentificacionLegal` | prevista | prevista | prevista | prevista | prevista | — | prevista |
| `DireccionFisica` | prevista | — | — | — | prevista | prevista | — |
| `Telefono` | prevista | — | — | — | prevista | — | — |
| `CorreoElectronico` | prevista | — | — | — | prevista | — | — |
| `Pais` | prevista | prevista | prevista | prevista | prevista | prevista | prevista |
| `Moneda` | — | prevista | prevista | prevista | prevista | — | prevista |
| `DivisionTerritorial` | prevista | — | **prevista (jurisdicción fiscal)** | — | prevista | prevista | — |
| `Contacto` | prevista (consolida y muestra en la ficha) | **prevista (contactos del Proveedor, #38)** | — | — | prevista | — | — |

**Convención:** `prevista` = el modelo de dominio del consumidor referencia el concepto; `✓` = adopción confirmada en implementación; `—` = sin uso identificado. La matriz se ajusta a medida que los modelos se intervengan con el replanteamiento.

---

## Cómo proponer un Nugget

1. Verificar contra este catálogo (incluidos candidatos y rechazados) que el concepto no esté cubierto.
2. Crear un issue con etiqueta `tipo: nugget` respondiendo los **seis filtros** de la [gobernanza](gobernanza-nuggets.md#sección-3-criterios-de-admisión) y declarando contra qué Nuggets existentes se comparó.
3. Esperar el veredicto del custodio: aceptar, extender un Nugget existente o rechazar.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.7 | Junio 2026 | **Candidato `InformacionTercero` resuelto: composición local, no Nugget** (intervención de OXP, issue #38, `[D32]` de su modelo). Pasa a la memoria de propuestas rechazadas con su razón — la identificación legal ya empaqueta todas las reglas; la razón social no aporta validación propia. La lista de candidatos queda vacía. |
| 1.6 | Junio 2026 | **Entra `Contacto`** (0.1) — veredicto de aceptación del issue #35 (surgido de la reescritura de Terceros v2.0, #33: los contactos los captura cada dominio con su rol del tercero y la bodega los consolida). Estructura pura como dato (nombre opcional + rol de contacto + medios por composición); el ciclo de vida, la marca de principal y la unicidad quedan en el consumidor — mismo criterio que salvó a `DireccionFisica` en el filtro 2. Vocabulario de 7 roles de contacto **producido** (`datos/roles-contacto.json` — decisión de la revisión del PR #40: se crea con los códigos vigentes; los ajustes futuros entran como versión menor). Primera adopción prevista: OXP (#38). Catálogo queda con **8 Nuggets aceptados** + 1 candidato (`InformacionTercero`, se resuelve en #38) + 2 diferidos. |
| 1.5 | Junio 2026 | **Entra `DivisionTerritorial`** (0.1): separado de `DireccionFisica` como fuente única de la jerarquía territorial — tiene dos consumidores de naturaleza distinta (direcciones y la jurisdicción fiscal de Impuestos, que no debe depender del Nugget de direcciones). `DireccionFisica` ahora compone sobre él; el pendiente de corregimientos PA se transfirió. Surge de la validación de eliminación total de Datos de Referencia (su alcance pasa a v2.0: producción de catálogos + tasas de cambio). |
| 1.4 | Junio 2026 | **Ajuste de alcance del catálogo** (decisión del usuario): `ValorMonetario` y `Vigencia` pasan a **diferidos** (sirven a OXP/Contabilidad/Impuestos, fuera del frente actual Terceros/Direcciones/Datos de Referencia; sus decisiones de borrador quedan conservadas en la sección de diferidos). Entran **`Pais`** y **`Moneda`** (0.1, sin pendientes): pasan los 6 filtros y formalizan la fuente única de datos de país/moneda dentro del paquete — `IdentificacionLegal`, `DireccionFisica` y `Telefono` componen sobre `Pais`. |
| 1.3 | Junio 2026 | **Las 6 especificaciones en borrador 0.1+.** Se completaron `DireccionFisica` (hereda el servicio de Direcciones + corrección normativa FE), `Telefono` (nombre ratificado; `preferido` sale del VO), `CorreoElectronico` (sin pendientes), `ValorMonetario` (núcleo monto+moneda; TRM/funcional = composición local del consumidor) y `Vigencia` (límites inclusivos, pendiente de ratificar con Impuestos). Criterio transversal aplicado: los atributos de **relación** (preferido, tipo de uso) viven en el consumidor, no en el VO. |
| 1.2 | Junio 2026 | **Renombre `DireccionPostal` → `DireccionFisica`** (evaluación de nomenclatura con el usuario: "postal" es calco del inglés y en Colombia desvía el significado hacia el correo físico; "física" es el calificador que el lenguaje natural ya usa para distinguirla de la electrónica y cubre los usos fiscal/comercial/sedes/correspondencia). |
| 1.1 | Junio 2026 | **Renombre `Identificacion` → `IdentificacionLegal`** (evaluación de nomenclatura con el usuario: el nombre original era ambiguo fuera del agregado Tercero; "legal" = emitida o reconocida por una autoridad). Especificación del Nugget avanzada a 0.3 con investigación de fuentes oficiales y `datos/` producidos; conteo de tipos de documento corregido a 46. |
| 1.0 | Junio 2026 | Versión inicial. 6 Nuggets en estado `Aceptado — en especificación` (`Identificacion`, `DireccionPostal`, `Telefono`, `CorreoElectronico`, `ValorMonetario`, `Vigencia`), 1 candidato en evaluación (`InformacionTercero`), matriz de consumidores prevista para 7 sub-dominios/servicios. |
