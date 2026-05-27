# Catálogo de Jurisdicciones Fiscales — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `JurisdiccionFiscal` (Sección 3.7 de `modelo-dominio.md`)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`co-jurisdiccion-fiscal.json`](co-jurisdiccion-fiscal.json)

---

## 1. Propósito

Este catálogo precarga las jurisdicciones fiscales de Colombia utilizadas por el motor de cálculo del sub-dominio Impuestos. Sirve para:

- Resolver el `subnacional` que el contrato del motor recibe en `ubicaciones.{rol}.subnacional` y que `RegistroTributario.Jurisdiccion.subnacional` persiste.
- Servir de dimensión en el stream key de `TarifaTributaria` para tributos subnacionales (ICA, RICA, sobretasa bomberil, etc.) — ej: `tarifa-CO-11001-ICA` (Bogotá D.C.).
- Marcar regiones con régimen fiscal especial territorial (Puerto Libre de San Andrés, Providencia y Santa Catalina) para que las condiciones de aplicación puedan exceptuar o modificar tributos según `tipo` y `tipoRegimen`.
- Asociar opcionalmente cada jurisdicción a su división administrativa equivalente del catálogo de Datos de Referencia (`divisiones-territoriales-co.json`) vía `divisionTerritorialRef`.

Cierra el pendiente `[PD8]` para Colombia.

---

## 2. Fuente normativa y autoridad

### 2.1. Fuente principal — Códigos territoriales

- **Fuente:** DIVIPOLA — División Político-Administrativa de Colombia.
- **Autoridad emisora:** DANE (Departamento Administrativo Nacional de Estadística).
- **URL:** [https://geoportal.dane.gov.co/geovisores/territorio/consulta-divipola-division-politico-administrativa-de-colombia/](https://geoportal.dane.gov.co/geovisores/territorio/consulta-divipola-division-politico-administrativa-de-colombia/)
- **Datos abiertos:** [https://www.datos.gov.co/Mapas-Nacionales/DIVIPOLA-C-digos-municipios/gdxc-w37w](https://www.datos.gov.co/Mapas-Nacionales/DIVIPOLA-C-digos-municipios/gdxc-w37w)

### 2.2. Régimen territorial especial — San Andrés Puerto Libre

- **Norma fundadora:** Ley 47 de 1993, "Por la cual se dictan normas especiales para la organización y el funcionamiento del Departamento Archipiélago de San Andrés, Providencia y Santa Catalina."
- **Norma complementaria:** Ley 915 de 2004 (Estatuto Fronterizo) y Constitución Política, art. 310.
- **Efecto fiscal:** Exclusión del IVA para las ventas y servicios destinados al territorio del Archipiélago (art. 22 Ley 47/1993).

### 2.3. Régimen de Distritos Especiales

- **Norma general:** Ley 1617 de 2013 — "Régimen para los Distritos Especiales."
- **Constitución Política:** art. 322 (Bogotá D.C.) y art. 328 (demás distritos).
- **Distritos creados por norma específica:**
  - Bogotá D.C.: Distrito Capital (CN 1991, art. 322).
  - Cartagena de Indias: Distrito Turístico y Cultural (CN 1991, art. 328).
  - Santa Marta: Distrito Turístico, Cultural e Histórico (CN 1991, art. 328).
  - Barranquilla: Distrito Especial, Industrial y Portuario (Acto Legislativo 01 de 1993).
  - Buenaventura: Distrito Especial, Industrial, Portuario, Biodiverso y Ecoturístico (Acto Legislativo 02 de 2007).
  - San Andrés de Tumaco: Distrito Especial, Industrial, Portuario, Biodiverso y Ecoturístico (Acto Legislativo 02 de 2007, art. 328 — sobrevivió a la sentencia C-033/2009).
  - Riohacha: Distrito Especial, Turístico y Cultural (Ley 1766 de 2015).
  - Santa Cruz de Mompox: Distrito Especial, Turístico, Cultural e Histórico (Ley 1875 de 2017).
  - Turbo: Distrito Portuario, Logístico, Industrial, Turístico y Comercial (Ley 1883 de 2018).
  - Medellín: Distrito Especial de Ciencia, Tecnología e Innovación (Ley 2106 de 2021).
  - Cali: Distrito Especial Deportivo, Cultural, Turístico, Empresarial y de Servicios (Ley 2286 de 2023).
  - Barrancabermeja: Distrito Especial Portuario, Biodiverso, Industrial y Turístico (Ley 2384 de 2024).

---

## 3. Cobertura del catálogo

**Total: 84 entradas.**

Por nivel jurisdiccional:

| Nivel | Cantidad | Detalle |
|---|---|---|
| nacional | 1 | Colombia como soberanía tributaria. |
| departamental | 33 | 32 departamentos + Distrito Capital de Bogotá tratado como entidad de nivel departamental. |
| distrital | 12 | Distritos especiales reconocidos por ley. |
| municipal | 38 | Capitales departamentales no distritales + ciudades grandes con recaudo ICA significativo + municipios del Archipiélago. |

Por tipo de jurisdicción:

| Tipo | Cantidad | Detalle |
|---|---|---|
| territorial-administrativa | 81 | Jurisdicciones ordinarias. |
| regimen-especial-territorial | 3 | Departamento Archipiélago (`88`), San Andrés (`88001`), Providencia (`88564`) — todas con `tipoRegimen: puerto-libre`. |

> Esta es la **carga inicial F1**. Municipios menores (sin recaudo ICA significativo) se agregan en F2 vía administración con `origen: personalizado`.

---

## 4. Tipos y niveles utilizados

### `nivelJurisdiccional`

| Valor | Uso en este catálogo |
|---|---|
| `nacional` | Colombia. |
| `departamental` | Departamentos (incluido el Distrito Capital de Bogotá como ámbito departamental). |
| `distrital` | Distritos especiales reconocidos por ley (12 entradas). |
| `municipal` | Demás municipios con relevancia fiscal. |
| `estatal`, `provincial` | No aplican a Colombia. Reservados para otros países. |

### `tipo`

| Valor | Uso en este catálogo |
|---|---|
| `territorial-administrativa` | Departamentos, distritos y municipios ordinarios. |
| `regimen-especial-territorial` | Departamento Archipiélago + sus municipios (régimen Puerto Libre). |
| `distrito-fiscal-especial` | No aplica en F1 CO. Reservado F2 (US/CA). |
| `soberania-tributaria` | No aplica en F1 CO. Reservado F2 (reservas indígenas US, First Nations CA). |

### `tipoRegimen`

| Valor | Uso en este catálogo |
|---|---|
| `puerto-libre` | Archipiélago de San Andrés, Providencia y Santa Catalina (3 entradas: dpto 88, San Andrés 88001, Providencia 88564). |

---

## 5. Entradas

### 5.1. Nivel nacional

| Código | Nombre | Tipo | Vigencia desde |
|---|---|---|---|
| `CO` | Colombia | territorial-administrativa | 1991-07-04 |

### 5.2. Departamentos (33)

| Código | Nombre | Tipo | tipoRegimen | Vigencia desde |
|---|---|---|---|---|
| `05` | Antioquia | territorial-administrativa | — | 1991-07-04 |
| `08` | Atlántico | territorial-administrativa | — | 1991-07-04 |
| `11` | Bogotá D.C. | territorial-administrativa | — | 1991-07-04 |
| `13` | Bolívar | territorial-administrativa | — | 1991-07-04 |
| `15` | Boyacá | territorial-administrativa | — | 1991-07-04 |
| `17` | Caldas | territorial-administrativa | — | 1991-07-04 |
| `18` | Caquetá | territorial-administrativa | — | 1991-07-04 |
| `19` | Cauca | territorial-administrativa | — | 1991-07-04 |
| `20` | Cesar | territorial-administrativa | — | 1991-07-04 |
| `23` | Córdoba | territorial-administrativa | — | 1991-07-04 |
| `25` | Cundinamarca | territorial-administrativa | — | 1991-07-04 |
| `27` | Chocó | territorial-administrativa | — | 1991-07-04 |
| `41` | Huila | territorial-administrativa | — | 1991-07-04 |
| `44` | La Guajira | territorial-administrativa | — | 1991-07-04 |
| `47` | Magdalena | territorial-administrativa | — | 1991-07-04 |
| `50` | Meta | territorial-administrativa | — | 1991-07-04 |
| `52` | Nariño | territorial-administrativa | — | 1991-07-04 |
| `54` | Norte de Santander | territorial-administrativa | — | 1991-07-04 |
| `63` | Quindío | territorial-administrativa | — | 1991-07-04 |
| `66` | Risaralda | territorial-administrativa | — | 1991-07-04 |
| `68` | Santander | territorial-administrativa | — | 1991-07-04 |
| `70` | Sucre | territorial-administrativa | — | 1991-07-04 |
| `73` | Tolima | territorial-administrativa | — | 1991-07-04 |
| `76` | Valle del Cauca | territorial-administrativa | — | 1991-07-04 |
| `81` | Arauca | territorial-administrativa | — | 1991-07-04 |
| `85` | Casanare | territorial-administrativa | — | 1991-07-04 |
| `86` | Putumayo | territorial-administrativa | — | 1991-07-04 |
| `88` | Archipiélago de San Andrés, Providencia y Santa Catalina | **regimen-especial-territorial** | `puerto-libre` | 1991-07-04 |
| `91` | Amazonas | territorial-administrativa | — | 1991-07-04 |
| `94` | Guainía | territorial-administrativa | — | 1991-07-04 |
| `95` | Guaviare | territorial-administrativa | — | 1991-07-04 |
| `97` | Vaupés | territorial-administrativa | — | 1991-07-04 |
| `99` | Vichada | territorial-administrativa | — | 1991-07-04 |

### 5.3. Distritos especiales (12)

| Código | Nombre | Departamento | Norma de creación | Vigencia desde |
|---|---|---|---|---|
| `05001` | Medellín | Antioquia | Ley 2106 de 2021 | 2021-07-14 |
| `05837` | Turbo | Antioquia | Ley 1883 de 2018 | 2018-01-24 |
| `08001` | Barranquilla | Atlántico | Acto Legislativo 01 de 1993 | 1993-12-29 |
| `11001` | Bogotá D.C. | — (Distrito Capital) | CN 1991, art. 322 | 1991-07-04 |
| `13001` | Cartagena de Indias | Bolívar | CN 1991, art. 328 | 1991-07-04 |
| `13468` | Santa Cruz de Mompox | Bolívar | Ley 1875 de 2017 | 2017-12-27 |
| `44001` | Riohacha | La Guajira | Ley 1766 de 2015 | 2015-07-15 |
| `47001` | Santa Marta | Magdalena | CN 1991, art. 328 | 1991-07-04 |
| `52835` | San Andrés de Tumaco | Nariño | Acto Legislativo 02 de 2007 | 2007-07-17 |
| `68081` | Barrancabermeja | Santander | Ley 2384 de 2024 | 2024-01-25 |
| `76001` | Cali | Valle del Cauca | Ley 2286 de 2023 | 2023-07-25 |
| `76109` | Buenaventura | Valle del Cauca | Acto Legislativo 02 de 2007 | 2007-07-06 |

### 5.4. Municipios capitales no distritales (24)

Capitales departamentales que aún no tienen la categoría de Distrito Especial. Cundinamarca no tiene capital propia: su capital funcional es Bogotá D.C., que es Distrito Capital (entrada `11001`).

| Código | Nombre | Departamento |
|---|---|---|
| `15001` | Tunja | Boyacá |
| `17001` | Manizales | Caldas |
| `18001` | Florencia | Caquetá |
| `19001` | Popayán | Cauca |
| `20001` | Valledupar | Cesar |
| `23001` | Montería | Córdoba |
| `27001` | Quibdó | Chocó |
| `41001` | Neiva | Huila |
| `50001` | Villavicencio | Meta |
| `52001` | Pasto | Nariño |
| `54001` | Cúcuta | Norte de Santander |
| `63001` | Armenia | Quindío |
| `66001` | Pereira | Risaralda |
| `68001` | Bucaramanga | Santander |
| `70001` | Sincelejo | Sucre |
| `73001` | Ibagué | Tolima |
| `81001` | Arauca | Arauca |
| `85001` | Yopal | Casanare |
| `86001` | Mocoa | Putumayo |
| `91001` | Leticia | Amazonas |
| `94001` | Inírida | Guainía |
| `95001` | San José del Guaviare | Guaviare |
| `97001` | Mitú | Vaupés |
| `99001` | Puerto Carreño | Vichada |

### 5.5. Municipios no capitales con relevancia fiscal (ICA significativo)

| Código | Nombre | Departamento | Motivo de inclusión |
|---|---|---|---|
| `05088` | Bello | Antioquia | Cuarta ciudad con mayor recaudo ICA del Valle de Aburrá. |
| `05266` | Envigado | Antioquia | Alto recaudo ICA per cápita. |
| `05360` | Itagüí | Antioquia | Polo industrial del Valle de Aburrá. |
| `05631` | Sabaneta | Antioquia | Alto recaudo ICA per cápita. |
| `08758` | Soledad | Atlántico | Conurbación con Barranquilla. |
| `25754` | Soacha | Cundinamarca | Conurbación con Bogotá, alto recaudo. |
| `68276` | Floridablanca | Santander | Conurbación con Bucaramanga. |
| `68307` | Girón | Santander | Conurbación con Bucaramanga. |
| `68547` | Piedecuesta | Santander | Conurbación con Bucaramanga. |
| `76520` | Palmira | Valle del Cauca | Polo agroindustrial y aeroportuario. |
| `76834` | Tuluá | Valle del Cauca | Centro comercial del Valle del Cauca. |
| `76892` | Yumbo | Valle del Cauca | Polo industrial principal de Colombia. |

### 5.6. Régimen Puerto Libre — Archipiélago (entradas con `tipoRegimen: puerto-libre`)

| Código | Nombre | Nivel | Vigencia desde |
|---|---|---|---|
| `88` | Departamento Archipiélago de San Andrés, Providencia y Santa Catalina | departamental | 1991-07-04 |
| `88001` | San Andrés (municipio cabecera) | municipal | 1993-02-19 |
| `88564` | Providencia | municipal | 1993-02-19 |

---

## 6. Notas operativas

### 6.1. Bogotá D.C. — dualidad departamento/distrito

Bogotá D.C. aparece dos veces en el catálogo:

- `11` como entidad **departamental** (Distrito Capital tratado como nivel departamental para tributos departamentales aplicables y consistencia con DIVIPOLA).
- `11001` como entidad **distrital** (municipio único del Distrito Capital, usado para resolución de ICA y demás tributos municipales).

El motor de cálculo usa `11001` para resolver tributos municipales/distritales en transacciones que ocurren en Bogotá. El código `11` se mantiene por consistencia con DIVIPOLA y para tributos de nivel departamental que pudieran aplicar. Las condiciones de aplicación deben definir explícitamente cuál código evalúan según el tributo.

### 6.2. Régimen Puerto Libre — granularidad

La exención de IVA por el régimen Puerto Libre aplica a todo el territorio del Archipiélago (Ley 47/1993, art. 22). Por eso las tres entradas (departamento `88`, San Andrés `88001`, Providencia `88564`) llevan `tipo: regimen-especial-territorial` y `tipoRegimen: puerto-libre`.

Cuando el motor evalúa una condición tipo "exceptuar IVA si la jurisdicción del rol tiene `tipoRegimen = puerto-libre`", cualquiera de los tres códigos satisface el match. El consumidor (OXP, CXC) debe pasar el código más específico disponible: prefiere `88001` o `88564` sobre `88` cuando se conoce el municipio exacto.

### 6.3. `divisionTerritorialRef` — relación con Datos de Referencia

Todas las jurisdicciones territorial-administrativas tienen `divisionTerritorialRef` poblado con su código DIVIPOLA equivalente del catálogo `divisiones-territoriales-co.json` del sub-dominio Datos de Referencia. Las jurisdicciones de tipo `regimen-especial-territorial` también lo pueblan cuando coinciden con una división administrativa (San Andrés 88001 coincide con el municipio de San Andrés).

Para futuras entradas en F2 (US/CA) de tipo `distrito-fiscal-especial` (transit districts, fire districts) o `soberania-tributaria` (reservas indígenas), `divisionTerritorialRef` queda `null` porque no existe equivalente administrativo.

### 6.4. Capitales departamentales como distritos

Cuando una capital departamental se eleva a Distrito Especial (Medellín 2021, Cali 2023, Barrancabermeja 2024), su `nivelJurisdiccional` cambia de `municipal` a `distrital` y su `vigencia.fechaDesde` corresponde a la fecha de sanción de la ley de creación del distrito. La entidad sigue existiendo con el mismo `codigo` DIVIPOLA — no se crea un nuevo código; se modifica la entrada vía el evento `JurisdiccionModificada` del agregado.

### 6.5. Distritos especiales — sentencia C-033/2009

El Acto Legislativo 02 de 2007 inicialmente otorgó la categoría de Distrito Especial a Tunja, Cúcuta, Popayán, Buenaventura, Turbo y Tumaco. La Corte Constitucional declaró inexequible parcialmente el acto (Sentencia C-033 de enero 28 de 2009), por lo que **Tunja, Cúcuta y Popayán perdieron la categoría de Distrito**. Buenaventura y Tumaco la conservan porque la inexequibilidad parcial respetó las modificaciones al art. 328 que los nombraban directamente. Turbo fue elevado nuevamente vía Ley 1883 de 2018.

En consecuencia, las entradas de **Tunja (`15001`), Cúcuta (`54001`) y Popayán (`19001`)** tienen `nivelJurisdiccional: municipal`, no `distrital`.

### 6.6. Carga vía `JurisdiccionAgregada`

El catálogo se materializa en el agregado `JurisdiccionFiscal` con stream `jurisdiccion-fiscal-CO` mediante una secuencia de eventos:

1. `JurisdiccionFiscalCreada(pais: "CO", origen: estandar)` — un único evento de inicialización.
2. Por cada entrada del JSON: `JurisdiccionAgregada(codigo, nombre, nivelJurisdiccional, divisionTerritorialRef, tipo, tipoRegimen, vigencia, origen: estandar)`.

La carga se ejecuta una sola vez como parte del seeding inicial del producto. Los administradores no pueden modificar entradas con `origen: estandar`; pueden agregar entradas adicionales con `origen: personalizado` (por ejemplo, municipios menores con recaudo ICA que el cliente quiera configurar).

### 6.7. Municipios faltantes

Este catálogo NO incluye los ~1.103 municipios del país. Solo precarga los necesarios para F1:

- 33 departamentos.
- 32 capitales (incluido Bogotá D.C. como distrital).
- 12 distritos especiales (incluye varias capitales y 4 no capitales: Turbo, Mompox, Tumaco, Barrancabermeja).
- 12 municipios no capitales con ICA significativo.
- 3 entradas del Archipiélago.

Para municipios no precargados:

- Si un cliente del ERP necesita tarifa ICA de un municipio no listado, el administrador lo agrega como `origen: personalizado`.
- El catálogo `divisiones-territoriales-co.json` de Datos de Referencia sí mantiene los ~1.103 municipios para autocompletar direcciones — pero no todos son jurisdicciones fiscales activas en el motor.

---

## 7. Histórico de cambios

| Versión | Fecha | Cambio | Autor |
|---|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 1 nacional + 33 departamentos + 12 distritos especiales + 36 municipios + 3 entradas del régimen Puerto Libre. Total 85 entradas. Fuente DIVIPOLA-DANE + leyes de creación de distritos + Ley 47 de 1993. | Equipo de Producto + Equipo Fiscal (pendiente de revisión por consultores). |

---

## 8. Revisión pendiente

Este catálogo requiere validación del **equipo de consultores fiscales** sobre:

1. **Completitud de distritos especiales**: ¿Falta algún distrito creado por ley posterior a 2024-01-25 (Barrancabermeja)?
2. **Lista de municipios no capitales con ICA significativo**: ¿Hay otros municipios que el equipo recomiende incluir en F1 (ej: Rionegro 05615, Sopó 25758, Cajicá 25126, Madrid 25430, Funza 25286, Mosquera 25473, Chía 25175, Cota 25214)? Estos quedaron fuera por criterio conservador, esperando confirmación.
3. **Granularidad del régimen Puerto Libre**: ¿Conviene modelar también el departamento (`88`) con régimen, o limitar a los municipios (`88001` y `88564`) para evitar dobles matches?
4. **Vigencia desde de departamentos preexistentes a la CN 91**: ¿Es correcto usar `1991-07-04` para todos, o conviene rastrear la fecha real de creación (ej: Antioquia 1830)?

Estas preguntas se resuelven en la revisión con el equipo y se reflejan en `v1.1`.
