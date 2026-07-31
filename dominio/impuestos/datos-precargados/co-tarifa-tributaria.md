# Catálogo Tarifas Tributarias — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `TarifaTributaria` (Sección 3.3 de `modelo-dominio.md`) — agregado con múltiples streams (uno por jurisdicción × tributo).
**Versión:** 1.2
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`co-tarifa-tributaria.json`](co-tarifa-tributaria.json)

---

## 1. Propósito

Precarga todas las **tarifas tributarias** de Colombia organizadas por stream del agregado `TarifaTributaria`. Cada stream identifica una tabla de tarifas de un tributo específico en una jurisdicción específica. El motor de cálculo busca la tarifa aplicable usando el `factorDeTarifa` declarado en el `CatalogoTributario`:

- IVA, INC: factor = clasificación (`GRAV_19`, `GRAV_5`, `EXENTO`).
- RETEFUENTE, AUTO_RETEFUENTE: factor = concepto de pago (`COMPRAS_GENERALES_DECLARANTES`, `HONORARIOS_DECLARANTES`, etc.).
- ICA, RICA, AUTO_RICA: factor = código CIIU de actividad económica (`4711`, `6201`, etc.).
- RIVA, SOBRETASA_BOMBERIL: porcentaje sobre el tributo padre (sin factor).
- IVA_IMPORTACION_SERVICIOS: factor = clasificación de servicios (`SERVICIOS_GRAV_19`, `SERVICIOS_GRAV_5`) — tarifa propia sobre la base, espejo de la tarifa de IVA del servicio.
- AUTO_RENTA: tarifa fija sin factor.

---

## 2. Fuente normativa

- **IVA, INC:** Estatuto Tributario Nacional (Libro Tercero, arts. 420 a 513) + Reformas Tributarias 2016, 2018, 2022.
- **RETEFUENTE:** Decreto Único Reglamentario 1625 de 2016 (compilación) + actualizaciones DIAN sectoriales anuales.
- **RIVA:** Estatuto Tributario art. 437-1 + Decreto 522 de 2003 y modificatorios.
- **AUTO_RENTA:** Decreto 2201 de 2016 (tarifas sectoriales 0.40%–1.60%).
- **AUTO_RETEFUENTE:** Aplica tarifas equivalentes a RETEFUENTE cuando la empresa es autorretenedora.
- **IVA_IMPORTACION_SERVICIOS:** IVA asumido por el adquiriente con tarifa espejo de la del IVA del servicio, sobre la base (arts. 420 par. 3 y 437-2 num. 3 del Estatuto Tributario). El efecto económico equivale al 100% del IVA que el proveedor no facturó.
- **ICA, RICA, SOBRETASA_BOMBERIL, AUTO_RICA:** Estatutos tributarios municipales de cada ciudad (acuerdos del Concejo Municipal/Distrital).

---

## 3. Cobertura del catálogo

| Categoría | Streams | Total tarifas |
|---|:---:|:---:|
| Nacionales (IVA, INC, RETEFUENTE, RIVA, AUTO_RENTA, AUTO_RETEFUENTE, IVA_IMPORTACION_SERVICIOS) | 7 | 60 |
| Municipales ICA (12 ciudades principales) | 12 | 64 |
| SOBRETASA_BOMBERIL (Bogotá ejemplo) | 1 | 1 |
| RICA y AUTO_RICA (placeholder, replican ICA municipal) | 2 | 0 |
| **Total** | **22** | **125** |

**Ciudades cubiertas en ICA (12):** Bogotá D.C. (`11001`), Medellín (`05001`), Cali (`76001`), Barranquilla (`08001`), Bucaramanga (`68001`), Cartagena (`13001`), Pereira (`66001`), Manizales (`17001`), Cúcuta (`54001`), Ibagué (`73001`), Santa Marta (`47001`), Villavicencio (`50001`).

---

## 4. Tarifas nacionales

### 4.1. IVA — `tarifa-CO-IVA`

| Factor | Tarifa | Notas |
|---|:---:|---|
| `GRAV_19` | 19% | Tarifa general. |
| `GRAV_5` | 5% | Tarifa reducida — canasta básica y otros. |
| `EXENTO` | 0% | Bienes exentos. |

### 4.2. INC — `tarifa-CO-INC`

| Factor | Tarifa |
|---|:---:|
| `INC_8` | 8% |

### 4.3. RETEFUENTE — `tarifa-CO-RETEFUENTE`

**Total: 49 conceptos precargados.**

Categorías cubiertas:

- **Compras (8 conceptos):** generales declarantes/no declarantes, agropecuarias, café pergamino, combustibles, vehículos, bienes raíces vivienda/otros.
- **Servicios (15 conceptos):** generales (decl/no decl), transporte carga/pasajeros/internacional, aseo y vigilancia, servicios temporales, restaurante/hotel, construcción, obra civil, salud (decl/no decl), educación, servicios públicos, impresión/publicidad, software, consultoría obra civil, servicios financieros.
- **Honorarios y comisiones (4 conceptos):** honorarios decl/no decl, comisiones intermediación, comisiones sector financiero.
- **Arrendamientos (2 conceptos):** muebles, inmuebles.
- **Rendimientos y premios (3 conceptos):** rendimientos financieros, loterías/rifas, premios.
- **Pagos al exterior (9 conceptos):** servicios técnicos, asistencia técnica, regalías, software, intereses, dividendos, consultoría, comisiones extranjeras, transporte internacional.
- **Otros (8 conceptos):** otros ingresos (decl/no decl), indemnizaciones laborales, comercialización animales vivos, seguros (primas), servicios temporales empleo, etc.

Las tarifas van desde 0.1% (combustibles) hasta 33% (pagos al exterior por software/comisiones extranjeras).

### 4.4. RIVA — `tarifa-CO-RIVA`

| Tarifa | Tipo |
|---|---|
| 15% del IVA generado | `porcentajeDePadre` |

### 4.5. Autorretenciones

| Stream | Tarifa | Notas |
|---|---|---|
| `tarifa-CO-AUTO_RENTA` | 0.55% fija | Tarifa base — existen tarifas sectoriales distintas (0.40%, 1.60%) que pueden requerir agregar entradas. |
| `tarifa-CO-AUTO_RETEFUENTE` | Replica tarifas RETEFUENTE | Solo 3 entradas precargadas como muestra (compras grales, servicios grales, honorarios). |
| `tarifa-CO-IVA_IMPORTACION_SERVICIOS` | 19% / 5% sobre la base | IVA asumido en importación de servicios (proveedor sin domicilio fiscal en el país) — espejo de la tarifa de IVA por clasificación de servicio. |

---

## 5. Tarifas municipales — ICA

Las tarifas ICA se expresan en **‰ (por mil)**. Cada ciudad tiene su tabla por código CIIU.

### 5.1. Bogotá D.C. (`tarifa-CO-11001-ICA`)

13 entradas con tarifas Acuerdo 65/2002 (modif. Acuerdo 469/2011, Acuerdo 648/2016). Rango: 4.14‰ (comercio, industria liviana) a 13.8‰ (espectáculos artísticos).

| Actividad CIIU | Tarifa |
|---|:---:|
| 4711 Comercio menor establecimientos no especializados | 4.14‰ |
| 4719 Otro comercio menor no especializado | 4.14‰ |
| 4631 Comercio mayor productos alimenticios | 4.14‰ |
| 1011 Procesamiento de carne | 4.14‰ |
| 5611 Restaurantes con servicio de mesa | 9.66‰ |
| 6201 Desarrollo de sistemas informáticos | 9.66‰ |
| 6810 Actividades inmobiliarias | 6.9‰ |
| 7010 Administración empresarial (sedes principales) | 11.04‰ |
| 7110 Arquitectura e ingeniería | 9.66‰ |
| 4321 Instalaciones eléctricas | 6.9‰ |
| 4111 Construcción de edificios residenciales | 6.9‰ |
| 8511 Educación primera infancia y primaria privada | 4.14‰ |
| 9001 Creación literaria, musical, artística | 13.8‰ |

### 5.2. Las otras 11 ciudades — Resumen

| Ciudad | Código | Tarifas | Cobertura actividades |
|---|---|:---:|---|
| Medellín | `05001` | 8 | Comercio, restaurantes, software, inmobiliario, admin, industria, construcción |
| Cali | `76001` | 7 | Comercio, restaurantes, software, admin, industria, construcción |
| Barranquilla | `08001` | 6 | Comercio, restaurantes, software, admin, industria |
| Bucaramanga | `68001` | 5 | Comercio, restaurantes, software, admin |
| Cartagena | `13001` | 5 | Comercio, restaurantes, software, admin, hoteles |
| Pereira | `66001` | 4 | Comercio, restaurantes, software, admin |
| Manizales | `17001` | 4 | Comercio, restaurantes, software, admin |
| Cúcuta | `54001` | 3 | Comercio, restaurantes, software |
| Ibagué | `73001` | 3 | Comercio, restaurantes, software |
| Santa Marta | `47001` | 3 | Comercio, restaurantes, hoteles |
| Villavicencio | `50001` | 3 | Comercio, restaurantes, software |

**Las tarifas para ciudades distintas de Bogotá son estimaciones razonables basadas en rangos típicos.** Las tarifas exactas por actividad CIIU varían trimestralmente y requieren validación contra el estatuto tributario vigente de cada municipio.

---

## 6. Notas operativas

### 6.1. RICA y AUTO_RICA replican ICA

`RICA` (retención de ICA) y `AUTO_RICA` (autorretención de ICA) **replican la tarifa del ICA municipal correspondiente**. El JSON incluye dos streams placeholder (`tarifa-CO-RICA-municipal-replica` y `tarifa-CO-municipios-AUTO_RICA`) que documentan este patrón sin duplicar datos. En implementación, el motor lee la tarifa ICA del municipio cuando calcula RICA o AUTO_RICA.

Sin embargo, algunos municipios definen porcentajes retenidos **distintos** del causado (típicamente 100% del ICA, pero pueden ser otros). Esto requiere verificación caso por caso con consultores.

### 6.2. SOBRETASA_BOMBERIL

Cada municipio define si aplica sobretasa bomberil y con qué porcentaje sobre RICA. Solo se precarga Bogotá D.C. (8% del RICA) como ejemplo. Los demás municipios se agregan a medida que se confirmen con el equipo fiscal.

### 6.3. Cuantías mínimas en UVT

La unidad UVT (Unidad de Valor Tributario) es indexada por la DIAN anualmente. El campo `cuantiaMinima.valor` lleva el monto en UVT; el monto en pesos se calcula dinámicamente como `valor × UVT_vigente`. Para 2024, 1 UVT = $47.065.

### 6.4. Tarifas declarantes vs no declarantes

Para RETEFUENTE, los pagos a beneficiarios **no declarantes** del impuesto sobre la renta tienen tarifas más altas (típicamente +1pp). El motor debe consultar el atributo `regimenTributario` del tercero para escoger entre los conceptos `_DECLARANTES` y `_NO_DECLARANTES`.

### 6.5. AUTO_RENTA — tarifas sectoriales

El Decreto 2201/2016 define tarifas distintas según el sector de la empresa autorretenedora:
- **0.40%** — Industria manufacturera.
- **0.55%** — General (tarifa precargada).
- **0.80%** — Comercio.
- **1.60%** — Sectores específicos (energía, banca, etc.).

La precarga solo incluye 0.55%. Las tarifas sectoriales se agregan cuando se valide con consultores qué sectores aplican.

### 6.6. Stream key y códigos didácticos

Los stream keys usan códigos DIVIPOLA de las jurisdicciones (`tarifa-CO-11001-ICA` para Bogotá, `tarifa-CO-05001-ICA` para Medellín). Es coherente con el catálogo `JurisdiccionFiscal`.

---

## 7. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 22 streams (7 nacionales + 12 ICA municipales + 1 SOBRETASA Bogotá + 2 placeholders RICA/AUTO_RICA) con 124 entradas de tarifa. 49 conceptos RETEFUENTE precargados. |
| 1.2 | 2026-07-31 | **Tarifa propia del autoliquidado (issues #117/#118):** el stream `tarifa-CO-IVA_IMPORTACION_SERVICIOS` pasa de una entrada "100% del padre" a **dos entradas espejo de la tarifa de IVA sobre la base** (`SERVICIOS_GRAV_19` → 19%, `SERVICIOS_GRAV_5` → 5%, `tipoTarifa: porcentaje`): el modelado padre-hijo descartaba el tributo cuando el proveedor no facturaba IVA (`[R14]`), y el adjetivo "teórico" no tenía contraparte en el vocabulario del modelo. 124 → 125 entradas. |
| 1.1 | 2026-07-31 | Renombre `AUTO_RIVA` → `IVA_IMPORTACION_SERVICIOS` (issue #110): stream `tarifa-CO-IVA_IMPORTACION_SERVICIOS`, entrada `iva-importacion-servicios-general`. Se corrige la fuente normativa §2, que describía el tributo como autorretención ("cuando la empresa es autorretenedora" — residuo de la definición legada): la tarifa del 100% corresponde a la autoliquidación del art. 437-2 num. 3 + art. 437-1. Se cierra la antigua pregunta 8 (¿siempre 100%?): el art. 437-1 fija la retención en el 100% del impuesto para este caso. |

---

## 8. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales**:

1. **Conceptos RETEFUENTE:** ¿Los 49 conceptos precargados son los relevantes para F1, o faltan/sobran? Casos conocidos no incluidos: dividendos a personas jurídicas, ganancias ocasionales, premios deportivos.
2. **Tarifas ICA por ciudad:** Los valores para las 11 ciudades distintas de Bogotá son **estimaciones**. ¿Se valida contra los estatutos tributarios vigentes de cada municipio?
3. **Cobertura de actividades CIIU por ciudad:** Solo precargué entre 3 y 13 actividades por ciudad. ¿La precarga completa (todas las actividades del estatuto municipal) se hace en F1 o se difiere a implementación?
4. **RICA — tarifa retención distinta del ICA causado:** ¿En qué municipios la tarifa de RICA es distinta de la tarifa ICA? Esos casos requieren stream propio.
5. **AUTO_RENTA — tarifas sectoriales:** ¿Cuáles sectores deben precargarse en F1 (industria 0.40%, comercio 0.80%, energía 1.60%) y cómo se identifica el sector aplicable?
6. **SOBRETASA_BOMBERIL:** Solo Bogotá precargado (8%). ¿Qué municipios aplican sobretasa y con qué porcentaje? Lista pendiente. **Datos nuevos (consultoría fiscal jul-2026):** Cali e Ibagué la manejan en la declaración; Bogotá **no** la maneja en la declaración de ICA según la consultoría — aunque el Acuerdo 927 de 2024 creó una sobretasa bomberil distrital (1% del ICA liquidado, para ingresos altos). El precargado "Bogotá 8%" luce doblemente sospechoso (¿municipio equivocado? ¿tarifa equivocada?): validar la lista real de municipios con retención de sobretasa y sus porcentajes.
7. **Cuantías mínimas:** ¿Las cuantías mínimas en UVT precargadas son las vigentes 2024–2025? Caso típico: arrendamiento inmuebles 27 UVT, servicios generales 4 UVT.
8. **Stream `tarifa-CO-AUTO_RETEFUENTE` incompleto:** ¿Replicamos todos los 49 conceptos como AUTO_RETEFUENTE o solo los aplicables a autorretenedoras?
