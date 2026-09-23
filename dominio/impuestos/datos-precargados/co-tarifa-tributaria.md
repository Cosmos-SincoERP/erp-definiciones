# Catálogo Tarifas Tributarias — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `TarifaTributaria` (Sección 3.3 de `modelo-dominio.md`) — agregado con múltiples streams (uno por jurisdicción × tributo).
**Versión:** 1.4
**Fecha de actualización:** 2026-09-22
**Archivo de datos:** [`co-tarifa-tributaria.json`](co-tarifa-tributaria.json)

---

## 1. Propósito

Precarga todas las **tarifas tributarias** de Colombia organizadas por stream del agregado `TarifaTributaria`. Cada stream identifica una tabla de tarifas de un tributo específico en una jurisdicción específica. El motor de cálculo busca la tarifa aplicable usando el `factorDeTarifa` declarado en el `CatalogoTributario`:

- IVA, INC: factor = clasificación (`GRAV_19`, `GRAV_5`, `EXENTO`).
- RETEFUENTE, RETEFUENTE_EXTERIOR, AUTO_RETEFUENTE: factor = concepto de pago (`COMPRAS_GENERALES_DECLARANTES`, `HONORARIOS_PERSONA_JURIDICA`, etc.). Los tres comparten los códigos de concepto: el tributo que aplica lo deciden las condiciones (proveedor nacional → RETEFUENTE; proveedor sin domicilio fiscal en el país → RETEFUENTE_EXTERIOR), no el concepto.
- ICA, RICA, AUTO_RICA: factor = código CIIU de actividad económica (`4711`, `6201`, etc.).
- RIVA, SOBRETASA_BOMBERIL: porcentaje sobre el tributo padre (sin factor).
- IVA_IMPORTACION_SERVICIOS: factor = clasificación de servicios (`SERVICIOS_GRAV_19`, `SERVICIOS_GRAV_5`) — tarifa propia sobre la base, espejo de la tarifa de IVA del servicio.
- AUTO_RENTA: tarifa fija sin factor.

---

## 2. Fuente normativa

- **IVA, INC:** Estatuto Tributario Nacional (Libro Tercero, arts. 420 a 513) + Reformas Tributarias 2016, 2018, 2022.
- **RETEFUENTE:** Decreto Único Reglamentario 1625 de 2016 (compilación): art. 1.2.4.3.1 (honorarios y comisiones — Decreto 260/2001 art. 1; software — Decreto 2521/2011 para licenciamiento y Decreto 2499/2012 para desarrollo, diseño web y consultoría informática), art. 1.2.4.4.1 (cuantía mínima de servicios), art. 1.2.4.4.6 (transporte aéreo y marítimo de pasajeros — Decreto 399/1987), art. 1.2.4.4.12 (servicios integrales de salud por IPS — Decreto 2271/2009), art. 1.2.4.9.1 (cuantía mínima de compras y construcción — Decreto 1512/1985), art. 1.2.4.10.3 (consultoría en ingeniería — Decreto 1141/2010); Estatuto Tributario arts. 392, 398 y 399. **Cuantías mínimas:** Decreto 572 de 2025 (arts. 2-8) rige desde 2025-06-01; suspendido provisionalmente por el Consejo de Estado con efectos del 2026-05-08 al 2026-06-30 (volvieron las cuantías previas); reactivado desde 2026-07-01 — ver §6.3. Fuente secundaria de contraste: tabla de retención en la fuente 2026 de Actualícese (`fuentes/VA26-Tabla-en-Excel-de-retencion-en-la-fuente-2026 (1).xlsm`, hoja "Retenciones y normatividad", 2026-07-14).
- **RETEFUENTE_EXTERIOR:** Estatuto Tributario arts. 406 a 408 (tarifa general del 20 % sobre pagos a beneficiarios sin residencia ni domicilio fiscal en el país, Ley 2010 de 2019) y 592 num. 2; convenios para evitar la doble imposición vigentes para la tarifa reducida (`co-convenio-de-doble-imposicion`).
- **RIVA:** Estatuto Tributario art. 437-1 + Decreto 522 de 2003 y modificatorios.
- **AUTO_RENTA:** Decreto 2201 de 2016 (tarifas sectoriales 0.40%–1.60%).
- **AUTO_RETEFUENTE:** Aplica tarifas equivalentes a RETEFUENTE cuando la empresa es autorretenedora.
- **IVA_IMPORTACION_SERVICIOS:** IVA asumido por el adquiriente con tarifa espejo de la del IVA del servicio, sobre la base (arts. 420 par. 3 y 437-2 num. 3 del Estatuto Tributario). El efecto económico equivale al 100% del IVA que el proveedor no facturó.
- **ICA, RICA, SOBRETASA_BOMBERIL, AUTO_RICA:** Estatutos tributarios municipales de cada ciudad (acuerdos del Concejo Municipal/Distrital).

---

## 3. Cobertura del catálogo

| Categoría | Streams | Total tarifas |
|---|:---:|:---:|
| Nacionales (IVA, INC, RETEFUENTE, RETEFUENTE_EXTERIOR, RIVA, AUTO_RENTA, AUTO_RETEFUENTE, IVA_IMPORTACION_SERVICIOS) | 8 | 86 |
| Municipales ICA (12 ciudades principales) | 12 | 64 |
| SOBRETASA_BOMBERIL (Bogotá ejemplo) | 1 | 1 |
| RICA y AUTO_RICA (placeholder, replican ICA municipal) | 2 | 0 |
| **Total** | **23** | **151** |

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

**Total: 45 conceptos (factores) precargados en 63 entradas** — 6 conceptos llevan 4 tramos de vigencia por la cuantía mínima (§6.3); los otros 39 tienen una sola entrada. Los pagos a beneficiarios del exterior ya no son conceptos de este stream: tienen tributo y stream propios (§4.4).

Categorías cubiertas:

- **Compras (9 conceptos):** generales declarantes/no declarantes, agropecuarias, café pergamino, combustibles, vehículos, bienes raíces vivienda/otros, activos fijos adquiridos a personas naturales (1%).
- **Servicios (21 conceptos):** generales (decl/no decl), transporte de carga, transporte terrestre de pasajeros (3,5%), transporte aéreo y marítimo de pasajeros (1%), transporte internacional, aseo y vigilancia, servicios temporales, hoteles/restaurantes/hospedaje (3,5%), construcción o urbanización, servicios integrales de salud por IPS (2%), educación, servicios públicos, impresión/publicidad, desarrollo de software (decl 3,5% / PN no decl 10%), licenciamiento de software (decl 3,5% / PN no decl 10%), consultoría en ingeniería de infraestructura y edificaciones (decl 6% / PN no decl 10%), servicios financieros.
- **Honorarios y comisiones (4 conceptos):** honorarios a persona jurídica (11%) y a persona natural (10%), comisiones de intermediación, comisiones del sector financiero.
- **Arrendamientos (2 conceptos):** muebles, inmuebles.
- **Rendimientos y premios (3 conceptos):** rendimientos financieros, loterías/rifas, premios.
- **Otros (6 conceptos):** otros ingresos (decl/no decl), indemnizaciones laborales, comercialización animales vivos, seguros (primas), honorarios de personal de servicios temporales.

Las tarifas van desde 0.1% (combustibles) hasta 20% (loterías, premios e indemnizaciones laborales).

### 4.4. RETEFUENTE_EXTERIOR — `tarifa-CO-RETEFUENTE_EXTERIOR`

**Total: 12 conceptos en 12 entradas, todos al 20 %** — tarifa general del art. 408 ET para pagos o abonos en cuenta a beneficiarios sin residencia ni domicilio fiscal en el país. Sin cuantía mínima.

| Concepto (factor) | Tarifa | Nota |
|---|:---:|---|
| `SERVICIOS_GENERALES_DECLARANTES` | 20% | Servicios en general prestados por beneficiarios del exterior. |
| `SERVICIOS_TECNICOS` | 20% | Concepto propio del exterior (sin gemelo doméstico). |
| `ASISTENCIA_TECNICA` | 20% | Concepto propio del exterior. |
| `SERVICIOS_CONSULTORIA_OBRA_CIVIL_DECLARANTES` | 20% | Consultoría. |
| `HONORARIOS_PERSONA_JURIDICA` | 20% | Honorarios y comisiones — persona jurídica. |
| `HONORARIOS_PERSONA_NATURAL` | 20% | Honorarios y comisiones — persona natural. |
| `COMISIONES_INTERMEDIACION` | 20% | Comisiones. |
| `REGALIAS` | 20% | Regalías y explotación de intangibles. Concepto propio del exterior. |
| `SERVICIOS_SOFTWARE_LICENCIAMIENTO_DECLARANTES` | 20% | Licenciamiento de software — base por validar (§8). |
| `SERVICIOS_SOFTWARE_DESARROLLO_DECLARANTES` | 20% | Desarrollo de software y consultoría informática. |
| `ARRENDAMIENTO_MUEBLES` | 20% | Arrendamiento de bienes muebles — tarifa especial de leasing por validar. |
| `RENDIMIENTOS_FINANCIEROS` | 20% | Intereses — tarifa reducida de créditos a más de un año por validar. |

**Cómo se usa:** el stream comparte los códigos de concepto de pago con RETEFUENTE. Un consumidor que envía `HONORARIOS_PERSONA_JURIDICA` con un proveedor nacional cae en el 11 % de RETEFUENTE; con un proveedor sin domicilio fiscal en el país, la condición `RTF-09` excluye RETEFUENTE y `RTF-EXT-01` activa este tributo, que resuelve el 20 % con el mismo código. Los tres conceptos propios del exterior (`SERVICIOS_TECNICOS`, `ASISTENCIA_TECNICA`, `REGALIAS`) los asigna el consumidor cuando el gasto corresponde a esas figuras.

**Qué no tiene entrada:** compras de bienes (`COMPRAS_*`), transporte, servicios públicos y demás conceptos que no constituyen renta de fuente nacional del beneficiario del exterior. Para esos conceptos el tributo se descarta con motivo `tarifa_no_configurada`. Reemplaza los 8 conceptos `EXTERIOR_*` (15 %/33 %, norma anterior a la Ley 2010 de 2019) que hasta la v1.3 vivían dentro de RETEFUENTE sin disparador.

**Tarifa por convenio:** cuando el país de residencia fiscal del beneficiario tiene un convenio para evitar la doble imposición vigente, la condición `RTF-EXT-02` reemplaza la tarifa de este stream por la tarifa convenida (catálogo `co-convenio-de-doble-imposicion`; 10 % general en la precarga, por validar).

### 4.5. RIVA — `tarifa-CO-RIVA`

| Tarifa | Tipo |
|---|---|
| 15% del IVA generado | `porcentajeDePadre` |

### 4.6. Autorretenciones y autoliquidados

| Stream | Tarifa | Notas |
|---|---|---|
| `tarifa-CO-AUTO_RENTA` | 0.55% fija | Tarifa base — existen tarifas sectoriales distintas (0.40%, 1.60%) que pueden requerir agregar entradas. |
| `tarifa-CO-AUTO_RETEFUENTE` | Replica tarifas RETEFUENTE | Solo 3 entradas precargadas como muestra (compras grales, servicios grales, honorarios persona jurídica). |
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

La unidad UVT (Unidad de Valor Tributario) es indexada por la DIAN anualmente. El campo `cuantiaMinima.valor` lleva el monto en UVT; el monto en pesos se calcula dinámicamente como `valor × UVT_vigente`. Para 2026, 1 UVT = $52.374.

**Tramos de vigencia por el Decreto 572 de 2025.** El decreto rebajó las cuantías mínimas de RETEFUENTE (servicios 4 → 2 UVT; compras, arrendamiento de inmuebles, transporte terrestre de pasajeros y construcción 27 → 10 UVT). Su vigencia tuvo tres cortes, y el catálogo los representa con **varias entradas del mismo factor**, misma tarifa, distinta cuantía y vigencias consecutivas sin solape (`[I25]`, `[R08]`):

| Tramo | Vigencia | Cuantías |
|---|---|---|
| 1 | 2017-01-01 → 2025-05-31 | previas |
| 2 | 2025-06-01 → 2026-05-07 | Decreto 572 |
| 3 | 2026-05-08 → 2026-06-30 | previas (suspensión provisional del Consejo de Estado) |
| 4 | 2026-07-01 → ∞ | Decreto 572 (suspensión revocada) |

Convención de `entradaId`: la entrada inicial conserva el identificador base; las siguientes llevan el sufijo `-{fechaDesde}` (ej. `rtf-servicios-restaurante-hotel-2025-06-01`).

Conceptos con tramos en esta versión (6): hoteles/restaurantes/hospedaje, servicios integrales de salud IPS y transporte aéreo/marítimo de pasajeros (4 → 2 UVT); transporte terrestre de pasajeros, construcción o urbanización y activos fijos adquiridos a personas naturales (27 → 10 UVT). **Los demás conceptos con cuantía conservan el valor previo al Decreto 572** (compras generales 27, agropecuarias 92, café 160, arrendamiento de inmuebles 27, otros ingresos 27, servicios generales 4, transporte de carga 4, aseo y vigilancia 4, servicios temporales 4, educación 4, impresión 4, financieros 4, loterías y premios 48, animales vivos 92) hasta la actualización masiva de cuantías, que se trabaja junto con la cuantía mínima como política de empresa (pregunta 7).

### 6.4. Pares de conceptos por condición del beneficiario

Varios conceptos de RETEFUENTE existen en pares según una condición del beneficiario, expresada en el sufijo del factor. Hay dos ejes:

- `_DECLARANTES` / `_NO_DECLARANTES` (obligado o no a declarar renta): compras generales, servicios generales, otros ingresos, desarrollo y licenciamiento de software, consultoría en ingeniería.
- `_PERSONA_JURIDICA` / `_PERSONA_NATURAL` (tipo de persona): honorarios y comisiones (Decreto 260/2001 art. 1).

El sufijo solo existe cuando hay gemelo; un factor sin sufijo aplica a cualquier beneficiario.

**La elección entre los dos miembros del par la hace el consumidor (OXP) al escoger el concepto de pago de la transacción.** El motor no la deriva del perfil tributario: el atributo `regimenTributario` (Ordinario, Simple, Especial, NoResponsable) no expresa la condición de declarante de renta y ninguna condición precargada lo evalúa; tampoco existe hoy un atributo de declarante de renta en el perfil (pregunta 13).

**Caso del 11% a persona natural:** la tarifa de honorarios a persona natural es 10%, y sube a 11% cuando el contrato o los pagos acumulados del año superan 3.300 UVT, o cuando el beneficiario vincula 2 o más trabajadores (Decreto 260/2001 art. 1, literales a y b). El catálogo lleva 10%; el caso queda documentado y no se automatiza (pregunta 10).

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
| 1.4 | 2026-09-22 | **Retención a beneficiarios del exterior con stream propio (issue #143).** Nuevo stream `tarifa-CO-RETEFUENTE_EXTERIOR` (12 entradas al 20 %, art. 408 ET) indexado por los mismos códigos de concepto de pago de RETEFUENTE más tres conceptos propios del exterior (`SERVICIOS_TECNICOS`, `ASISTENCIA_TECNICA`, `REGALIAS`). Se **retiran** de `tarifa-CO-RETEFUENTE` los 8 conceptos `EXTERIOR_*` (15 %/33 %, desactualizados y sin disparador): 53 → 45 conceptos, 71 → 63 entradas. Streams 22 → 23 (nacionales 8), entradas 147 → 151. Sección 4.4 nueva; 4.4-4.5 anteriores → 4.5-4.6. Preguntas 16-17 nuevas. |
| 1.3 | 2026-09-21 | **Conceptos RETEFUENTE contrastados con la tabla de retención 2026 de Actualícese (issue #133):** (a) **Honorarios** por tipo de persona, no por declarante: `HONORARIOS_DECLARANTES` (10%) → `HONORARIOS_PERSONA_JURIDICA` **11%** y `HONORARIOS_NO_DECLARANTES` (11%) → `HONORARIOS_PERSONA_NATURAL` **10%** (11% si contrato/pagos > 3.300 UVT o ≥ 2 trabajadores — documentado, no automatizado); `auto-rtf-honorarios` y equivalencias DIAN 5020/5021 alineadas. (b) **Software:** `SERVICIOS_SOFTWARE_DESARROLLO` (4%, 4 UVT, tratado como servicio general) → `SERVICIOS_SOFTWARE_DESARROLLO_DECLARANTES` **3,5% sin cuantía** (Decreto 2499/2012) + gemelo `_NO_DECLARANTES` 10%; nuevo par `SERVICIOS_SOFTWARE_LICENCIAMIENTO_DECLARANTES` 3,5% / `_NO_DECLARANTES` 10% (Decreto 2521/2011). (c) **Consultoría en ingeniería:** `SERVICIOS_CONSULTORIA_OBRA_CIVIL` (6%, 27 UVT) → `_DECLARANTES` 6% **sin cuantía** + gemelo `_NO_DECLARANTES` 10% (Decreto 1141/2010). (d) **Transporte de pasajeros:** `SERVICIOS_TRANSPORTE_PASAJEROS` → `SERVICIOS_TRANSPORTE_TERRESTRE_PASAJEROS` (3,5%); nuevo `SERVICIOS_TRANSPORTE_AEREO_MARITIMO_PASAJEROS` 1% (Decreto 399/1987). (e) Nuevo `COMPRAS_ACTIVOS_FIJOS_PERSONA_NATURAL` 1% (arts. 398-399 ET). (f) **Retirados** `SERVICIOS_SALUD_NO_DECLARANTES` (3%, sin sustento) y `SERVICIOS_OBRA_CIVIL` (duplicaba `SERVICIOS_CONSTRUCCION`); `SERVICIOS_SALUD_DECLARANTES` → `SERVICIOS_SALUD_IPS` (servicios integrales de salud por IPS, Decreto 2271/2009). (g) **Cuantías por tramos de vigencia** del Decreto 572/2025 (vigente 2025-06-01, suspendido 2026-05-08 a 2026-06-30, reactivado 2026-07-01) **solo en los 6 conceptos tocados**: hoteles/restaurantes, salud IPS y transporte aéreo/marítimo 4 → 2 UVT; transporte terrestre, construcción y activos fijos PN 27 → 10 UVT; convención `entradaId-{fechaDesde}` (§6.3). El resto del stream conserva cuantías previas (actualización masiva en issue posterior). (h) §6.4 corregido: la elección entre pares la hace el consumidor al escoger el concepto de pago — el motor no consulta `regimenTributario`. (i) Conteos: 49 → **53** conceptos, 49 → **71** entradas RETEFUENTE, 125 → **147** entradas totales (la cabecera del JSON decía 124 desde v1.0; los conteos por categoría de §4.3 se recalcularon desde el JSON). Preguntas 9-15 nuevas en §8. Homologación DIAN v1.1 y anexo de configuración estándar alineados. |
| 1.0 | 2026-05-26 | Carga inicial F1: 22 streams (7 nacionales + 12 ICA municipales + 1 SOBRETASA Bogotá + 2 placeholders RICA/AUTO_RICA) con 124 entradas de tarifa. 49 conceptos RETEFUENTE precargados. |
| 1.2 | 2026-07-31 | **Tarifa propia del autoliquidado (issues #117/#118):** el stream `tarifa-CO-IVA_IMPORTACION_SERVICIOS` pasa de una entrada "100% del padre" a **dos entradas espejo de la tarifa de IVA sobre la base** (`SERVICIOS_GRAV_19` → 19%, `SERVICIOS_GRAV_5` → 5%, `tipoTarifa: porcentaje`): el modelado padre-hijo descartaba el tributo cuando el proveedor no facturaba IVA (`[R14]`), y el adjetivo "teórico" no tenía contraparte en el vocabulario del modelo. 124 → 125 entradas. |
| 1.1 | 2026-07-31 | Renombre `AUTO_RIVA` → `IVA_IMPORTACION_SERVICIOS` (issue #110): stream `tarifa-CO-IVA_IMPORTACION_SERVICIOS`, entrada `iva-importacion-servicios-general`. Se corrige la fuente normativa §2, que describía el tributo como autorretención ("cuando la empresa es autorretenedora" — residuo de la definición legada): la tarifa del 100% corresponde a la autoliquidación del art. 437-2 num. 3 + art. 437-1. Se cierra la antigua pregunta 8 (¿siempre 100%?): el art. 437-1 fija la retención en el 100% del impuesto para este caso. |

---

## 8. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales**:

1. **Conceptos RETEFUENTE:** ¿Los 53 conceptos precargados son los relevantes para F1, o faltan/sobran? Casos conocidos no incluidos: dividendos a personas jurídicas, ganancias ocasionales, premios deportivos (ver también la pregunta 15).
2. **Tarifas ICA por ciudad:** Los valores para las 11 ciudades distintas de Bogotá son **estimaciones**. ¿Se valida contra los estatutos tributarios vigentes de cada municipio?
3. **Cobertura de actividades CIIU por ciudad:** Solo precargué entre 3 y 13 actividades por ciudad. ¿La precarga completa (todas las actividades del estatuto municipal) se hace en F1 o se difiere a implementación?
4. **RICA — tarifa retención distinta del ICA causado:** ¿En qué municipios la tarifa de RICA es distinta de la tarifa ICA? Esos casos requieren stream propio.
5. **AUTO_RENTA — tarifas sectoriales:** ¿Cuáles sectores deben precargarse en F1 (industria 0.40%, comercio 0.80%, energía 1.60%) y cómo se identifica el sector aplicable?
6. **SOBRETASA_BOMBERIL:** Solo Bogotá precargado (8%). ¿Qué municipios aplican sobretasa y con qué porcentaje? Lista pendiente. **Datos nuevos (consultoría fiscal jul-2026):** Cali e Ibagué la manejan en la declaración; Bogotá **no** la maneja en la declaración de ICA según la consultoría — aunque el Acuerdo 927 de 2024 creó una sobretasa bomberil distrital (1% del ICA liquidado, para ingresos altos). El precargado "Bogotá 8%" luce doblemente sospechoso (¿municipio equivocado? ¿tarifa equivocada?): validar la lista real de municipios con retención de sobretasa y sus porcentajes.
7. **Cuantías mínimas:** los 6 conceptos revisados en v1.3 siguen la línea de tiempo del Decreto 572/2025 (§6.3); el resto conserva las cuantías previas. ¿Confirman la línea de tiempo (vigencia 2025-06-01, suspensión 2026-05-08 a 2026-06-30, reactivación 2026-07-01) y que la suspensión cubrió todos los artículos 2 a 8? La actualización del resto del stream se trabaja junto con la cuantía mínima como política de empresa.
8. **Stream `tarifa-CO-AUTO_RETEFUENTE` incompleto:** ¿Replicamos todos los 53 conceptos como AUTO_RETEFUENTE o solo los aplicables a autorretenedoras?
9. **Alcance del 3,5% de software frente a honorarios:** el Decreto 2499/2012 enumera análisis, diseño, desarrollo, implementación, mantenimiento, pruebas y documentación de software, diseño de páginas web y consultoría informática. ¿Un contrato de "consultoría informática" facturado por persona jurídica va al 3,5% (`SERVICIOS_SOFTWARE_DESARROLLO_DECLARANTES`) o al 11% de honorarios? ¿El soporte y mantenimiento vendidos junto con una licencia siguen la licencia (3,5%) o son servicio? ¿Qué criterio operativo le damos al usuario que clasifica el pago?
10. **Seguimiento del 11% a persona natural:** `HONORARIOS_PERSONA_NATURAL` lleva 10%. ¿Cómo se controla en la práctica el salto al 11% (contrato > 3.300 UVT, pagos acumulados del año, 2 o más trabajadores)? ¿Se acepta que sea decisión del usuario al escoger el concepto, o requiere acumulados por tercero (fuera de F1)?
11. **"4% o 6%" de servicios a persona natural no declarante:** el precargado lleva `SERVICIOS_GENERALES_NO_DECLARANTES` al 6%. La tabla 2026 cita un 4% especial (inciso 6 del art. 392 ET). ¿En qué casos aplica y cómo debería expresarse en el catálogo?
12. **Hoteles, restaurantes y hospedaje a no declarantes:** la norma fija el 3,5% "para obligados a declarar renta". ¿Qué tarifa aplica a un prestador persona natural no declarante: 3,5% igual, o el 6% de servicios generales?
13. **Atributo de declarante de renta en el perfil tributario:** hoy no existe y el motor no lo necesita porque el consumidor escoge el concepto (§6.4). ¿Conviene tenerlo para validar o sugerir el concepto? De ser así, requiere issue propio sobre el catálogo de atributos fiscales y una regla de poblado en la migración de terceros.
14. **Conceptos sin respaldo en la tabla 2026:** `SERVICIOS_EDUCACION` (2%), `SERVICIOS_IMPRESION_PUBLICIDAD` (4%), `SERVICIOS_FINANCIEROS` (4%), `SERVICIOS_TRANSPORTE_INTERNACIONAL` (3%), `INDEMNIZACIONES_LABORALES` (20%), `COMERCIALIZACION_ANIMALES_VIVOS` (1,5%), `SEGUROS_PRIMAS` (2,5%), `COMISIONES_INTERMEDIACION` (11%), `HONORARIOS_SERVICIOS_TEMPORALES` (1%). ¿Se sostienen con norma propia, se reconducen a servicios generales u honorarios, o se retiran? Impresión/publicidad y financieros son en la práctica servicios generales al 4%.
15. **Conceptos de la tabla 2026 deliberadamente no precargados** por estar fuera del gasto o requerir mecanismos distintos de una tarifa: compras con tarjeta débito/crédito (1,5%, la practica la entidad financiera al comercio), exportaciones de hidrocarburos/carbón y minerales (autorretención), rentas de trabajo con la tabla del art. 383 ET (incluye personas naturales con honorarios que declaran bajo juramento no usar costos), dividendos (tablas por tipo de socio), rendimientos de títulos de renta fija/CDAT (4%), oro de comercializadoras internacionales, comisiones en bolsa (3%), sísmica de hidrocarburos, emolumentos eclesiásticos, colocación de juegos de azar, artistas extranjeros (8%). ¿Alguno debe entrar en F1?
16. **`RETEFUENTE_EXTERIOR` — conceptos con tarifa especial:** el stream precarga la tarifa general del 20 % (art. 408 ET) para 12 conceptos. ¿Qué conceptos tienen tarifa distinta y con qué base: intereses de créditos a más de un año (15 %), arrendamiento financiero de equipos (leasing), transporte internacional, explotación de software, dividendos a no residentes? ¿Cuáles de los conceptos precargados no constituyen renta de fuente nacional y deberían retirarse?
17. **`RETEFUENTE_EXTERIOR` — jurisdicciones no cooperantes y retención asumida:** ¿aplica la tarifa general de renta (35 %) a beneficiarios en jurisdicciones no cooperantes o de baja imposición (art. 408 parágrafo)? Requeriría una segunda lista de países y una condición propia. Y cuando la empresa asume la retención (el proveedor recibe el total), ¿la base se reajusta como mayor valor del pago (100 pagados → base 125) y el gasto de la retención asumida es no deducible? Hoy el motor liquida sobre la base facturada.
