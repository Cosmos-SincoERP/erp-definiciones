# Anexo: Configuración estándar — Colombia (CO)

## Propósito

Contenido fiscal estándar que el producto provee precargado para Colombia. Corresponde a los datos iniciales (seeds) que se cargan como streams de eventos de configuración al iniciar operación — los mismos eventos que el modelo define para cada agregado.

El contenido se organiza siguiendo la estructura de los agregados del modelo de dominio:

| Agregado | Qué contiene en este anexo |
|----------|---------------------------|
| CatalogoTributario | Tributos, clasificaciones tributarias, tratamientos, reglas de localización |
| TarifaTributaria | Tarifas conocidas por tributo y jurisdicción |
| CondicionDeAplicacion | Reglas que modifican la aplicación según perfiles tributarios |
| CatalogoDeAtributosFiscales | Atributos fiscales requeridos |
| FormatoFiscal | Formatos de entregables por autoridad fiscal |

**Fuentes:** `fuentes/Definiciones de tributos.xlsx`, `fuentes/formatos-y-entrega-reportes-fiscales.md`, Estatuto Tributario colombiano.

**Nota sobre tarifas:** Las tarifas específicas (porcentajes, cuantías mínimas en UVT) son las vigentes al momento de la elaboración de este documento. El contenido estándar del producto se actualiza cuando la normativa cambia — las tarifas aquí documentadas son una referencia inicial, no una fuente normativa.

---

## 1. Tributos — CatalogoTributario

Los datos del catálogo `catalogo-tributario-CO` (tributos, clasificaciones, tratamientos y reglas de localización) se migraron a [`datos-precargados/co-catalogo-tributario.json`](datos-precargados/co-catalogo-tributario.json) (v1.5, 2026-09-22). Allí viven las 72 entidades F1 (12 tributos + 8 clasificaciones + 40 tratamientos + 12 reglas de localización). El narrativo de revisión para consultores está en [`datos-precargados/co-catalogo-tributario.md`](datos-precargados/co-catalogo-tributario.md).

En esta sección queda únicamente el **contexto de diseño**:

- **Tributos directos:** 8 (IVA, INC, ICA, RETEFUENTE, RETEFUENTE_EXTERIOR, RIVA, RICA, SOBRETASA_BOMBERIL). IVA, INC, ICA son `aditivo`; los demás `sustractivo`. Dependencias padre-hijo: RIVA → IVA, SOBRETASA_BOMBERIL → RICA (RICA es independiente). **RETEFUENTE_EXTERIOR** (`[D17]`) es la retención a beneficiarios sin domicilio fiscal en el país (arts. 406 a 408 ET): tributo autónomo, carácter `definitivo`, dirección `gasto`, factor `conceptoPago` compartido con RETEFUENTE; sustituye a la retención doméstica por condición (`RTF-09` / `RTF-EXT-01`) y toma la tarifa del convenio de doble imposición cuando aplica (`RTF-EXT-02`). Participa solo de las clasificaciones de servicios y de las excluidas/exentas — no de bienes gravados.
- **Tributos de provisión:** 4, todos con naturaleza **`provision`** (se reconocen sin afectar el valor a pagar/cobrar ni los saldos) — tres autorretenciones (AUTO_RENTA, AUTO_RETEFUENTE, AUTO_RICA), que aplican cuando la empresa es autorretenedora (atributo del `PerfilTributario`) con direccionalidad `ingreso`; y un autoliquidado **autónomo** (IVA_IMPORTACION_SERVICIOS, sin padre ni hijos, tarifa propia sobre la base) con direccionalidad `gasto` — el adquiriente asume el IVA cuando el proveedor de servicios no tiene residencia ni domicilio fiscal en el país (art. 437-2 num. 3).
- **`codigo` semántico inmutable:** Los códigos de Tributo y Clasificación son inmutables por referencias históricas desde `RegistroTributario` (`[I26]`). Política de modificabilidad de otros atributos diferida a `[PD12]`.
- **Clasificaciones:** 8 categorías que agrupan bienes y servicios por tratamiento (GRAV_19, GRAV_5, SERVICIOS_GRAV_19, SERVICIOS_GRAV_5, EXCLUIDO, EXENTO, INC_8, NO_GRAVADO). Las `SERVICIOS_*` distinguen la naturaleza de servicio del concepto — el eje que el autoliquidado necesita (solo alcanza servicios; los bienes importados liquidan su IVA en aduana). Las tarifas específicas viven en `TarifaTributaria` — el catálogo solo define qué clasificaciones existen.

### Jurisdicciones fiscales — JurisdiccionFiscal

Los datos del catálogo `jurisdiccion-fiscal-CO` se migraron a [`datos-precargados/co-jurisdiccion-fiscal.json`](datos-precargados/co-jurisdiccion-fiscal.json) (v1.0, 2026-05-26). Allí vive la lista completa de las 84 entradas iniciales F1 (1 nacional + 33 departamentos + 12 distritos especiales + 38 municipios) con sus códigos DIVIPOLA, vigencias y referencias normativas. El narrativo de revisión para consultores está en [`datos-precargados/co-jurisdiccion-fiscal.md`](datos-precargados/co-jurisdiccion-fiscal.md).

En esta sección queda únicamente el **contexto de diseño** de la configuración estándar:

- **Códigos:** Se usan los códigos DIVIPOLA del DANE como identificador de cada jurisdicción. La codificación es estable, oficial y compartida con el catálogo `divisiones-territoriales-co.json` de Datos de Referencia vía `divisionTerritorialRef`.
- **Regímenes territoriales especiales:** Se modelan explícitamente con `tipo: regimen-especial-territorial` y `tipoRegimen` categórico. En Colombia aplica únicamente el régimen `puerto-libre` para el Archipiélago de San Andrés, Providencia y Santa Catalina (Constitución art. 310 + Ley 47/1993). Las demás jurisdicciones son `territorial-administrativa`.
- **Cobertura inicial F1:** La carga inicial F1 incluye nacional + departamentos + distritos especiales + capitales departamentales + ~12 municipios no capitales con recaudo ICA significativo. Los municipios menores se agregan progresivamente como `origen: personalizado` cuando el cliente los necesita.

> **Nota sobre tipoRegimen:** El régimen `puerto-libre` agrupa las jurisdicciones del Archipiélago de San Andrés, Providencia y Santa Catalina, que tienen tributación especial conforme a la Constitución (art. 310) y Ley 47/1993. El IVA nacional NO aplica a transacciones donde el lugar de ejecución pertenece a este régimen — esta regla se modela como `CondicionDeAplicacion` que evalúa `lugarEjecucion.jurisdiccion.tipoRegimen = "puerto-libre"`.

### Reglas de localización

Las 12 reglas de localización se migraron junto con el catálogo tributario a [`datos-precargados/co-catalogo-tributario.json`](datos-precargados/co-catalogo-tributario.json) (sección `reglasLocalizacion`).

**Patrón de diseño:** Tributos nacionales (IVA, INC, RETEFUENTE, RETEFUENTE_EXTERIOR, RIVA, AUTO_RENTA, AUTO_RETEFUENTE, IVA_IMPORTACION_SERVICIOS) resuelven por `sedeEmisora` sin fallback — la sede de la empresa determina el país. Tributos municipales (ICA, RICA, SOBRETASA_BOMBERIL, AUTO_RICA) resuelven por `lugarEjecucion` con fallback a `sedeEmisora` — donde se presta el servicio o entrega el bien, y si no hay dato del lugar de ejecución, se usa la sede.

---

## 2. Tarifas — TarifaTributaria

Las tarifas tributarias se migraron a [`datos-precargados/co-tarifa-tributaria.json`](datos-precargados/co-tarifa-tributaria.json) (v1.3, 2026-09-21). Allí viven 22 streams con 147 entradas de tarifa (7 streams nacionales + 12 streams ICA municipales + sobretasa Bogotá + placeholders RICA/AUTO_RICA). El narrativo de revisión está en [`datos-precargados/co-tarifa-tributaria.md`](datos-precargados/co-tarifa-tributaria.md).

En esta sección queda únicamente el **contexto de diseño**:

- **IVA, INC:** 3 + 1 = 4 entradas nacionales por clasificación tributaria.
- **RETEFUENTE:** 45 conceptos de pago en 63 entradas (Decreto Único 1625/2016 + Decreto 572/2025; 6 conceptos con 4 tramos de cuantía mínima) — compras, servicios, honorarios (persona jurídica 11% / persona natural 10%), arrendamientos, etc. Tarifas de 0.1% a 20%. Los pagos al exterior salieron de este stream en v1.4.
- **RETEFUENTE_EXTERIOR:** 12 conceptos al 20 % (art. 408 ET) en el stream propio `tarifa-CO-RETEFUENTE_EXTERIOR`, con los mismos códigos de concepto de RETEFUENTE más `SERVICIOS_TECNICOS`, `ASISTENCIA_TECNICA` y `REGALIAS`; sin entradas para compras de bienes (descarte `tarifa_no_configurada`). Tarifa reducida por convenio de doble imposición: catálogo `co-convenio-de-doble-imposicion` (10 % general, por validar). La elección entre pares `_DECLARANTES`/`_NO_DECLARANTES` la hace el consumidor al escoger el concepto de pago.
- **RIVA:** 15% del IVA generado (porcentajeDePadre).
- **AUTO_RENTA:** Tarifa base 0.55%. Tarifas sectoriales (0.40% industria, 0.80% comercio, 1.60% otros) pendientes de validación.
- **AUTO_RETEFUENTE:** Replica tarifas RETEFUENTE; precarga inicial de 3 conceptos.
- **IVA_IMPORTACION_SERVICIOS:** tarifa espejo de la del IVA del servicio, sobre la base (19%/5% por clasificación de servicios) — efecto económico equivalente al 100% del IVA que el proveedor no facturó.
- **ICA municipal:** Streams por código DIVIPOLA — `tarifa-CO-11001-ICA` (Bogotá), `tarifa-CO-05001-ICA` (Medellín), etc. Cobertura F1 en 12 ciudades principales. Bogotá tiene 13 entradas con tarifas oficiales del Acuerdo 65/2002; las otras 11 ciudades tienen 3–8 entradas con valores razonables pendientes de validación.
- **RICA y AUTO_RICA:** Replican la tarifa ICA del municipio correspondiente.
- **SOBRETASA_BOMBERIL:** Solo Bogotá precargada (8% del RICA). Otros municipios pendientes.

Los stream keys usan códigos DIVIPOLA del catálogo `JurisdiccionFiscal` (`tarifa-CO-11001-ICA` para Bogotá, no abreviaturas didácticas).

---

## 3. Condiciones de aplicación — CondicionDeAplicacion

Las condiciones de aplicación se migraron a [`datos-precargados/co-condicion-de-aplicacion.json`](datos-precargados/co-condicion-de-aplicacion.json) (v1.3, 2026-09-22). Allí viven las 34 condiciones precargadas (16 RETEFUENTE + 2 RETEFUENTE_EXTERIOR + 4 RIVA + 5 RICA + 3 IVA + 4 autorretenciones y autoliquidado). El narrativo de revisión está en [`datos-precargados/co-condicion-de-aplicacion.md`](datos-precargados/co-condicion-de-aplicacion.md).

En esta sección queda únicamente el **contexto de diseño**:

- **Patrón asimétrico:** Las reglas con perspectiva asimétrica (que solo tienen sentido normativo en una dirección) se modelan como dos condiciones independientes — una evaluando `emisora` con dirección fija, otra evaluando `contraparte` con la dirección opuesta. Las reglas bilaterales (Régimen Simple) se mantienen como una sola condición con `direccionFiscalAplicable: ambas`.
- **Lenguaje fiscal del dominio:** `emisora` y `contraparte` como roles posicionales — no `vendedor`/`comprador` (esos roles se proyectan según dirección).
- **Distribución por tributo:** RETEFUENTE 16 (9 exclusiones — incluida `RTF-09` al proveedor sin domicilio fiscal — + 6 casos granC compuestos + 1 default), RETEFUENTE_EXTERIOR 2 (`RTF-EXT-01` activación por proveedor sin domicilio fiscal; `RTF-EXT-02` tarifa por convenio con el operador `con-convenio-vigente` sobre `paisDeResidenciaFiscal`), RIVA 4, RICA 5, IVA 3 (2 régimen IVA + 1 territorial Puerto Libre), autorretenciones y autoliquidado 4 (una por tributo).
- **Exclusión mutua doméstica/exterior:** el mismo criterio (`contraparte.tieneDomicilioFiscalEnElPais = false`) excluye RETEFUENTE y activa RETEFUENTE_EXTERIOR — un proveedor recibe una de las dos retenciones, nunca ambas (`[D17]`).
- **Frontera con `Tratamiento`:** El `Tratamiento` (en `CatalogoTributario`) declara qué tributos aplican por clasificación; la `CondicionDeAplicacion` ajusta por perfil tributario del sujeto. El motor primero filtra por tratamiento y luego evalúa condiciones.
- **Condición territorial:** `IVA-02-territorial` evalúa `lugarEjecucion.jurisdiccion.tipoRegimen = "puerto-libre"` — opera sobre la jurisdicción resuelta, no sobre atributos del perfil. Materializa la decisión `[D12]` y la invariante `[I15]`. Si en el futuro se incorporan más jurisdicciones con `tipoRegimen: puerto-libre`, esta condición las cubre automáticamente.
- **Cuantía mínima:** NO es condición. Es atributo de `EntradaDeTarifa` (`cuantiaMinima`). El motor la evalúa después de resolver la tarifa.
- **Provisiones:** Su `direccionFiscalAplicable` declarada en el `Tributo` (ingreso para las autorretenciones AUTO_RENTA/RETEFUENTE/RICA; gasto para el autoliquidado IVA_IMPORTACION_SERVICIOS) actúa como prefiltro estructural. Las condiciones activan las autorretenciones cuando la empresa tiene la calidad correspondiente, y el autoliquidado cuando la contraparte no tiene domicilio fiscal en el país (`tieneDomicilioFiscalEnElPais = false`).

### INC, ICA — sin condiciones por perfil

INC e ICA no evalúan calidades tributarias del emisor ni del adquiriente. Su aplicación depende únicamente de la clasificación tributaria (INC) y de la ciudad + actividad económica (ICA). El ICA tiene además **direccionalidad inherente** (`direccionFiscalAplicable: ingreso`): solo se liquida cuando la empresa genera el ingreso — en las compras el comprador practica la retención (RICA), no autoliquida ICA.

---

## 4. Atributos fiscales — CatalogoDeAtributosFiscales

Las 17 definiciones de atributos fiscales se migraron a [`datos-precargados/co-catalogo-de-atributos-fiscales.json`](datos-precargados/co-catalogo-de-atributos-fiscales.json) (v1.3, 2026-09-22). Allí viven los atributos requeridos/opcionales con sus tipos, valoresValidos o catalogoReferencia, vigencias y normas asociadas. El narrativo de revisión está en [`datos-precargados/co-catalogo-de-atributos-fiscales.md`](datos-precargados/co-catalogo-de-atributos-fiscales.md).

En esta sección queda únicamente el **contexto de diseño**:

- **10 atributos requeridos** clasifican el régimen tributario y las calificaciones DIAN principales (gran contribuyente, autorretenedora, agente retenedor IVA, exento retefuente, régimen simple, autorretenedor renta, perteneceRegimenIVA, tieneDomicilioFiscalEnElPais, tipoPersona, regimenTributario).
- **`paisDeResidenciaFiscal`** (enum contra el catálogo de países de Datos de Referencia): declarado, obligatorio cuando la contraparte no tiene domicilio fiscal en el país (`[I29]`). Con `tipoPersona` y `tieneDomicilioFiscalEnElPais = false` conforma el perfil mínimo del proveedor del exterior. Lo evalúa `RTF-EXT-02` para la tarifa por convenio.
- **3 atributos opcionales municipales** (`esAgenteRetenedorICA`, `esAutorretenedorICA`, `esGranContribuyenteICA`) cubren calificaciones por jurisdicción municipal. Su contextualización por municipio (una empresa puede ser autorretenedora de ICA en Bogotá pero no en Medellín) es pendiente de refinamiento por consultores.
- **3 atributos opcionales con `catalogoReferencia`** (`inscripcionZonaFranca`, `inscripcionMonopolio`, `inscripcionPuertoLibre`) referencian al `CatalogoDeRegimenesEspeciales` filtrado por `tipo`. Soportan el patrón D13 (regímenes empresariales por inscripción).
- **Retirado:** `actividadEconomica` ya no es atributo del catálogo. Tras `[D14]`, se modela como entidad `ActividadEconomicaRegistrada` dentro del `PerfilTributario`, con multiplicidad por jurisdicción/clasificación. La precarga de códigos CIIU vive en `co-ciiu.json`.

---

## 5. Regímenes especiales empresariales — CatalogoDeRegimenesEspeciales

Los datos del catálogo `catalogo-regimenes-CO` se migraron a [`datos-precargados/co-catalogo-de-regimenes-especiales.json`](datos-precargados/co-catalogo-de-regimenes-especiales.json) (v1.0, 2026-05-26). Allí viven las 38 entradas precargadas de la muestra significativa F1 (21 zonas francas + 16 monopolios departamentales + 1 Puerto Libre empresarial). El narrativo de revisión está en [`datos-precargados/co-catalogo-de-regimenes-especiales.md`](datos-precargados/co-catalogo-de-regimenes-especiales.md). Origen normativo y metodología completa: ver `anexo-catalogo-regimenes-especiales.md`.

En esta sección queda únicamente el **contexto de diseño**:

- **Tres tipos vigentes en F1 para CO:** `zona-franca`, `monopolio-sectorial`, `puerto-libre-empresa`. Los tipos `zona-economica-especial` y `regimen-especial-decreto` están en el enum (`[D13]`) pero no aplican a CO (se reservan para entradas `personalizado`).
- **Cobertura F1:** Muestra significativa de 38 entradas. La cobertura total estimada (~155 entradas: 121 zonas francas + ~33 monopolios + 1 Puerto Libre) se completa con consultores fiscales tras revisión del catálogo. Las entradas faltantes entran vía `RegimenEspecialAgregado` con `origen: estandar`.
- **Frontera con `JurisdiccionFiscal`:** Los regímenes aquí son **empresariales** — aplican porque la empresa está inscrita. Los regímenes **territoriales** (Puerto Libre como hecho económico) viven en `JurisdiccionFiscal` con `tipoRegimen`. Una empresa inscrita en `PL-EMP-SAI` físicamente ubicada en San Andrés activa AMBOS tratamientos; una empresa de Bogotá vendiendo a San Andrés activa solo el territorial.
- **Códigos internos:** Los códigos `ZF-BAQ`, `ZFPE-ECOPETROL-RBC`, `MON-LIC-ANT`, etc. son códigos internos del catálogo asignados para esta precarga. Si los consultores indican usar los códigos oficiales DIAN, se renombran.

---

## 5 bis. Convenios para evitar la doble imposición — ConvenioDeDobleImposicion

Los convenios vigentes de Colombia se precargan en [`datos-precargados/co-convenio-de-doble-imposicion.json`](datos-precargados/co-convenio-de-doble-imposicion.json) (v1.0, 2026-09-22): 15 convenios (España, Chile, Suiza, Canadá, México, Corea del Sur, India, Portugal, República Checa, Reino Unido, Italia, Francia, Japón, Emiratos Árabes Unidos, Uruguay) con tarifa general del 10 % por regla de negocio acordada con la consultoría fiscal — todos `porValidar` (lista, fechas y tarifas por concepto). El narrativo está en [`datos-precargados/co-convenio-de-doble-imposicion.md`](datos-precargados/co-convenio-de-doble-imposicion.md).

**Contexto de diseño:** agregado propio por país (`[D17]`, Sección 3.9 del modelo), consultado por el motor cuando la condición `RTF-EXT-02` evalúa `paisDeResidenciaFiscal` con `con-convenio-vigente` y resuelve la `TarifaAlternativa` de forma convenio (tarifa convenida por concepto → general → sin cambio; cero exime). No se precargan convenios firmados sin entrada en vigor confirmada; la Decisión 578 de la Comunidad Andina queda pendiente con la consultoría (`[PD13]`).

---

## 6. Formatos fiscales — FormatoFiscal

Los formatos fiscales se migraron a [`datos-precargados/co-formato-fiscal.json`](datos-precargados/co-formato-fiscal.json) (10 formatos: 8 DIAN + 1 municipal + 1 certificado). La homologación oficial DIAN se migra a [`datos-precargados/co-homologacion-fiscal-dian.json`](datos-precargados/co-homologacion-fiscal-dian.json) (35 equivalencias).

**Contexto de diseño:**
- **8 formatos DIAN** de información exógena (F-1001, F-1003, F-1005, F-1006, F-1007, F-1647, F-2276, F-2856) — todos anuales con corte 31-03 año siguiente. Resolución 000233 de 2026 vigente.
- **Reporte ICA municipal** como placeholder genérico — cada municipio define su propio formato vía `origen: personalizado` cuando se activa.
- **Formulario 220** — certificado anual de retención en la fuente entregado a terceros.
- **35 equivalencias DIAN** cubren conceptos RETEFUENTE (bloque 5XXX), códigos IVA (`01`, `02`, etc.), rentas laborales (bloque 53XX) y códigos ICA.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.5 | Septiembre 2026 | **Retención a beneficiarios del exterior y convenios de doble imposición (issue #143, `[D17]`).** Nuevo tributo directo `RETEFUENTE_EXTERIOR` (12 tributos, 40 tratamientos, 12 reglas — catálogo v1.5); stream propio de 12 conceptos al 20 % y retiro de los `EXTERIOR_*` de RETEFUENTE (tarifas v1.4); condiciones `RTF-09`, `RTF-EXT-01/02` (34 condiciones — v1.3); atributo `paisDeResidenciaFiscal` (17 atributos — v1.3); nueva Sección 5 bis con el catálogo de convenios (v1.0). Cifras de secciones 1, 3 y 4 actualizadas (traían conteos de mayo). |
| 1.0 | Marzo 2026 | Versión inicial: 11 tributos, 6 clasificaciones, condiciones de aplicación completas, 13 atributos fiscales, formatos DIAN y municipales. |
| 1.1 | Mayo 2026 | Cambio 3 — Sub-cambio 3.4: nueva Sección 5 con regímenes empresariales precargados (zonas francas, monopolios departamentales, Puerto Libre empresarial). 3 atributos fiscales nuevos en Sección 4 (`inscripcionZonaFranca`, `inscripcionMonopolio`, `inscripcionPuertoLibre`) con `catalogoReferencia`. Renumeración de Sección 5 (Formatos) a Sección 6. `[D13]` `[I16]`. |
| 1.4 | Julio 2026 | **Autoliquidado autónomo + naturaleza `provision` (issues #117/#118).** IVA_IMPORTACION_SERVICIOS deja de ser hijo del IVA (el modelado padre-hijo lo descartaba por `[R14]` cuando el proveedor no facturaba IVA): tributo autónomo con tarifa propia sobre la base (espejo 19%/5%). Los 4 tributos de provisión pasan a naturaleza `provision` (modelo v2.0.8). Clasificaciones nuevas `SERVICIOS_GRAV_19`/`SERVICIOS_GRAV_5` con matriz completa (22 → 36 tratamientos, 66 entidades). `RIVA-03` (sustitución) retirada de condiciones — cada tributo se activa por sus propias condiciones. |
| 1.3 | Julio 2026 | **Renombre `AUTO_RIVA` → `IVA_IMPORTACION_SERVICIOS` y corrección del disparador (issue #110, resolución con consultoría fiscal).** La "autorretención de IVA" no existe como figura normativa: el tributo queda definido como la autoliquidación del IVA en importación de servicios (art. 437-2 num. 3), con disparador `contraparte.tieneDomicilioFiscalEnElPais = false` (atributo nuevo del catálogo v1.1) en lugar del residuo legado `emisora.esAgenteRetenedorIVA`. Prosa de provisiones reescrita distinguiendo autorretenciones (ingreso, por calidad propia) del autoliquidado (gasto, por contraparte del exterior). Conteos alineados al catálogo v1.2 (22 tratamientos, 50 entidades — issue #111). |
| 1.2 | Julio 2026 | **ICA con direccionalidad inherente `ingreso` (issue #93).** El catálogo declaraba `ICA: ambas`, lo que en dirección gasto hacía liquidar un ICA aditivo del que el comprador no es sujeto pasivo (además de la RICA, con misma base y tarifa) — contradiciendo `R61`. `ICA.direccionFiscalAplicable` pasa a `ingreso` (patrón de `AUTO_RICA`); `RICA` permanece en `ambas` (es retención). Sección "INC, ICA — sin condiciones por perfil" anotada. Alinea la definición fuente con la implementación ya verificada (`Cosmos.Impuestos#116`): catálogo `co-catalogo-tributario.{md,json}` v1.1 y `anexo-ejemplo-direccion-fiscal.md` (el paso de filtro por dirección del Caso A ahora sí descarta un tributo — ICA). |
