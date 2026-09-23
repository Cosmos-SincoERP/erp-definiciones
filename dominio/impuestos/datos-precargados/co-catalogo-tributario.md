# Catálogo Tributario — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `CatalogoTributario` (Sección 3.2 de `modelo-dominio.md`)
**Versión:** 1.5
**Fecha de actualización:** 2026-09-22
**Archivo de datos:** [`co-catalogo-tributario.json`](co-catalogo-tributario.json)

---

## 1. Propósito

Este catálogo precarga la configuración estándar del agregado `CatalogoTributario` para Colombia: el universo de **tributos**, **clasificaciones tributarias**, **tratamientos** (matriz tributo × clasificación) y **reglas de localización**. Es el ancla referencial para el resto de los catálogos del producto:

- `TarifaTributaria` referencia tributos y clasificaciones para construir sus streams (ej: `tarifa-CO-IVA`, `tarifa-CO-11001-ICA`).
- `CondicionDeAplicacion` modifica el tratamiento por perfil tributario.
- Los consumidores (OXP, CXC) clasifican sus conceptos contra el catálogo de `ClasificacionTributaria`.
- `RegistroTributario` captura el código del tributo y de la clasificación como snapshot inmutable.

---

## 2. Fuente normativa

- **Tributos directos:**
  - IVA, INC: Libro Tercero del Estatuto Tributario (arts. 420 a 513).
  - RETEFUENTE: Estatuto Tributario (arts. 365 a 419) + Decreto Único Reglamentario 1625 de 2016.
  - ICA, RICA, SOBRETASA_BOMBERIL: Ley 14 de 1983 + estatutos tributarios municipales.
- **Autorretenciones:**
  - AUTO_RENTA: Decreto 2201 de 2016.
  - AUTO_RETEFUENTE: Estatuto Tributario art. 9 y normas relacionadas.
  - AUTO_RICA: Acuerdos municipales que designen autorretenedores.
- **Retención a beneficiarios del exterior:** Estatuto Tributario arts. 406 a 408 (tarifa general del 20 % sobre rentas de fuente nacional pagadas a no residentes) y 592 numeral 2 (retención como impuesto definitivo del beneficiario); convenios para evitar la doble imposición vigentes (tarifas reducidas, ver `co-convenio-de-doble-imposicion`).
- **IVA por importación de servicios:** Art. 437-2 numeral 3 del Estatuto Tributario — el adquiriente autoliquida el IVA al contratar servicios gravados con proveedores sin residencia ni domicilio en el país; la retención es del 100% del impuesto (art. 437-1).
- **Sobretasa bomberil:** Ley 1575 de 2012 (Ley General de Bomberos).

---

## 3. Cobertura del catálogo

| Categoría | Cantidad |
|---|---|
| Tributos directos | 8 |
| Autorretenciones | 3 |
| Tributos autoliquidados | 1 |
| Clasificaciones tributarias | 8 |
| Tratamientos explícitos `aplica: true` | 40 |
| Reglas de localización | 12 |
| **Total entidades** | **72** |

---

## 4. Tributos

### 4.1. Tributos directos (8)

| Código | Nombre | Naturaleza | Carácter retención | Nivel | Factor de tarifa | Dirección fiscal | Tributo padre |
|---|---|:---:|:---:|:---:|---|:---:|:---:|
| `IVA` | Impuesto al Valor Agregado | aditivo | — | nacional | `clasificacion` | ambas | — |
| `INC` | Impuesto Nacional al Consumo | aditivo | — | nacional | `clasificacion` | ambas | — |
| `ICA` | Impuesto de Industria y Comercio | aditivo | — | municipal | `actividadEconomica` | ingreso | — |
| `RETEFUENTE` | Retención en la Fuente | sustractivo | anticipado | nacional | `conceptoPago` | ambas | — |
| `RETEFUENTE_EXTERIOR` | Retención en la fuente a beneficiarios del exterior | sustractivo | definitivo | nacional | `conceptoPago` | gasto | — |
| `RIVA` | Retención sobre el IVA | sustractivo | anticipado | nacional | `porcentajeDePadre` | ambas | `IVA` |
| `RICA` | Retención sobre el ICA | sustractivo | anticipado | municipal | `actividadEconomica` | ambas | — |
| `SOBRETASA_BOMBERIL` | Sobretasa Bomberil | sustractivo | anticipado | municipal | `porcentajeDePadre` | ambas | `RICA` |

> **Nota:** `ICA` aplica solo en `ingreso` — el sujeto pasivo del ICA es **quien genera el ingreso**; en dirección gasto el comprador solo practica la retención (`RICA`), no autoliquida ICA (coherente con `R61` del alcance). `RICA` permanece en `ambas` (retención: en gasto la empresa retiene al proveedor; en ingreso el cliente le retiene a la empresa).

> **Nota — `RETEFUENTE_EXTERIOR`:** es la retención que la empresa practica a un proveedor **sin residencia ni domicilio fiscal en el país** (arts. 406 a 408 ET). Es un **tributo autónomo**, no un concepto de RETEFUENTE: se activa por condición propia (`RTF-EXT-01`) y en ese mismo caso la retención doméstica se excluye (`RTF-09`) — nunca concurren. Comparte los **códigos de concepto de pago** con RETEFUENTE, de modo que los consumidores no cambian la asignación de sus conceptos: el mismo concepto "honorarios" cae en la tarifa doméstica con un proveedor nacional y en la del exterior (20 %) con un proveedor extranjero. Solo existe en dirección `gasto` (la empresa es la residente). Su carácter es **definitivo**: el beneficiario no declara en el país y la retención es su impuesto (art. 592 num. 2). La tarifa se reduce cuando el país de residencia fiscal del beneficiario tiene un convenio para evitar la doble imposición vigente (`RTF-EXT-02`, catálogo `co-convenio-de-doble-imposicion`). Si la empresa **asume** la retención (el proveedor recibe el total) o se la **descuenta** no lo decide este catálogo: lo decide el sub-dominio consumidor (OXP), que conoce el acuerdo con el proveedor.

### 4.2. Tributos de provisión (4): autorretenciones y autoliquidados

| Código | Nombre | Naturaleza | Carácter retención | Nivel | Factor de tarifa | Dirección fiscal | Tributo padre |
|---|---|:---:|:---:|:---:|---|:---:|:---:|
| `AUTO_RENTA` | Autorretención de Renta | provision | anticipado | nacional | `fija` | ingreso | — |
| `AUTO_RETEFUENTE` | Autorretención en la Fuente | provision | anticipado | nacional | `conceptoPago` | ingreso | — |
| `IVA_IMPORTACION_SERVICIOS` | IVA por importación de servicios | provision | anticipado | nacional | `clasificacion` | gasto | — |
| `AUTO_RICA` | Autorretención de ICA | provision | anticipado | municipal | `actividadEconomica` | ingreso | — |

> **Nota:** los cuatro tienen naturaleza **`provision`** — se reconocen sobre la transacción sin afectar el valor a pagar/cobrar ni los saldos; generan únicamente el reconocimiento contable (un débito y un crédito). `IVA_IMPORTACION_SERVICIOS` **no es una autorretención** — es un **tributo autónomo**: el IVA que el adquiriente asume al contratar servicios gravados con proveedores sin residencia ni domicilio fiscal en el país. El proveedor no factura IVA y recibe el total de la factura; la empresa reconoce el impuesto con **tarifa propia sobre la base** (espejo de la tarifa de IVA de la clasificación del servicio), lo declara y lo paga a la DIAN. No tiene tributo padre ni hijos: sus reglas no se mezclan con las del IVA facturado (no se le retiene RIVA, no arrastra las condiciones del IVA, y no depende de que otro tributo se cause). Por eso su dirección es `gasto` (solo existe en compras) mientras las tres autorretenciones son `ingreso` (la empresa se retiene sobre sus propias ventas). La antigua figura "autorretención de IVA en ventas" del sistema de facturación no tiene sustento en el Estatuto y fue removida — no existe autorretenedor de rete-IVA.

---

## 5. Clasificaciones tributarias

| Código | Nombre | Tributos que aplican (resumen) |
|---|---|---|
| `GRAV_19` | Gravados 19% | IVA, RETEFUENTE, AUTO_RETEFUENTE, RIVA, ICA, RICA |
| `GRAV_5` | Gravados 5% | IVA, RETEFUENTE, AUTO_RETEFUENTE, RIVA, ICA, RICA |
| `SERVICIOS_GRAV_19` | Servicios gravados 19% | IVA, RETEFUENTE, RETEFUENTE_EXTERIOR, AUTO_RETEFUENTE, RIVA, ICA, RICA, IVA_IMPORTACION_SERVICIOS |
| `SERVICIOS_GRAV_5` | Servicios gravados 5% | IVA, RETEFUENTE, RETEFUENTE_EXTERIOR, AUTO_RETEFUENTE, RIVA, ICA, RICA, IVA_IMPORTACION_SERVICIOS |
| `EXCLUIDO` | Excluidos de IVA | RETEFUENTE, RETEFUENTE_EXTERIOR, AUTO_RETEFUENTE, ICA, RICA |
| `EXENTO` | Exentos de IVA (tarifa 0%) | IVA, RETEFUENTE, RETEFUENTE_EXTERIOR, AUTO_RETEFUENTE, ICA, RICA |
| `INC_8` | Gravados INC 8% | INC |
| `NO_GRAVADO` | No sujeto a impuestos | — |

> **Nota (eje de naturaleza del concepto):** las clasificaciones `SERVICIOS_*` distinguen que el concepto es un **servicio** — el eje que los tributos autoliquidados necesitan, porque el art. 437-2 num. 3 solo alcanza servicios (el IVA de los bienes importados lo recauda la aduana). El consumidor clasifica sus conceptos de servicio con ellas **siempre**, sea el proveedor local o del exterior: la clasificación depende de qué es el concepto, no de quién lo vende. Cuál tributo sobrevive en cada compra lo deciden las condiciones por calidades (el IVA del proveedor en régimen, o el autoliquidado del proveedor sin domicilio fiscal).

---

## 6. Tratamientos (matriz clasificación × tributo)

Cada entrada declara que el tributo aplica cuando un concepto es clasificado con esa clasificación. Los tratamientos NO listados implican que el tributo no aplica por default; pueden agregarse excepciones vía `origen: personalizado`.

**Total: 40 tratamientos `aplica: true`.** Distribución:

- GRAV_19: 6 tributos (IVA, RETEFUENTE, AUTO_RETEFUENTE, RIVA, ICA, RICA).
- GRAV_5: 6 tributos (mismos que GRAV_19, distintas tarifas en `TarifaTributaria`).
- SERVICIOS_GRAV_19: 8 tributos (los 6 de GRAV_19 + RETEFUENTE_EXTERIOR + IVA_IMPORTACION_SERVICIOS).
- SERVICIOS_GRAV_5: 8 tributos (los 6 de GRAV_5 + RETEFUENTE_EXTERIOR + IVA_IMPORTACION_SERVICIOS).
- EXCLUIDO: 5 tributos (RETEFUENTE, RETEFUENTE_EXTERIOR, AUTO_RETEFUENTE, ICA, RICA).
- EXENTO: 6 tributos (IVA con tarifa 0%, RETEFUENTE, RETEFUENTE_EXTERIOR, AUTO_RETEFUENTE, ICA, RICA).

`RETEFUENTE_EXTERIOR` participa **solo** de las clasificaciones de servicios y de las excluidas/exentas, **no** de `GRAV_19`/`GRAV_5` (bienes gravados): la compra de bienes a un proveedor del exterior es una importación y no está sometida a esta retención. Es el filtro estructural que evita que el tributo llegue a evaluarse sobre mercancías; para los conceptos de bienes que caigan en `EXCLUIDO`/`EXENTO`, el tributo se descarta porque su stream de tarifas no tiene entrada para esos conceptos (motivo `tarifa_no_configurada`).
- INC_8: 1 tributo (INC).
- NO_GRAVADO: ninguno.

`AUTO_RETEFUENTE` **sí participa de la matriz** (las cuatro clasificaciones principales y las dos de servicios): la matriz declara su candidatura por clasificación, y su activación sigue dependiendo de la calidad de autorretenedora vía `CondicionDeAplicacion` — igual que en la configuración estándar de la implementación. `AUTO_RENTA` y `AUTO_RICA` NO se modelan como tratamientos por clasificación — su aplicación depende solo de atributos del `PerfilTributario` y de la dirección fiscal. `IVA_IMPORTACION_SERVICIOS` participa **solo en las clasificaciones de servicios** — un bien nunca es candidato al autoliquidado. La validación fina de la matriz (exclusiones del art. 476) sigue con los consultores fiscales (pregunta 5 de la revisión pendiente).

---

## 7. Reglas de localización

Cada regla declara qué rol de ubicación de la transacción determina la jurisdicción fiscalmente relevante para resolver el tributo.

| Tributo | Rol que manda | Fallback |
|---|---|---|
| `IVA` | sedeEmisora | — |
| `INC` | sedeEmisora | — |
| `RETEFUENTE` | sedeEmisora | — |
| `RETEFUENTE_EXTERIOR` | sedeEmisora | — |
| `ICA` | lugarEjecucion | sedeEmisora |
| `RIVA` | sedeEmisora | — |
| `RICA` | lugarEjecucion | sedeEmisora |
| `SOBRETASA_BOMBERIL` | lugarEjecucion | sedeEmisora |
| `AUTO_RENTA` | sedeEmisora | — |
| `AUTO_RETEFUENTE` | sedeEmisora | — |
| `IVA_IMPORTACION_SERVICIOS` | sedeEmisora | — |
| `AUTO_RICA` | lugarEjecucion | sedeEmisora |

**Patrón:** Tributos nacionales resuelven por `sedeEmisora` (la sede de la empresa determina el país). Tributos municipales (ICA, RICA, SOBRETASA_BOMBERIL, AUTO_RICA) resuelven por `lugarEjecucion` (donde se presta el servicio o entrega el bien) con `sedeEmisora` como fallback.

---

## 8. Notas operativas

### 8.1. `codigo` semántico inmutable

`Tributo.codigo` y `ClasificacionTributaria.codigo` son **inmutables** durante todo el ciclo de vida (decisión del modelo `[I26]` + `[PD12]`). Cambiar un código rompe la trazabilidad histórica de `RegistroTributario` (que captura el código como snapshot). Si una normativa requiere un tributo distinto, se desactiva el actual y se agrega uno nuevo con código y vigencia propios. La política de corrección de errores la define el equipo fiscal del producto (pendiente `[PD12]`).

### 8.2. `caracterRetencion` y compensación

- **anticipado:** la retención se compensa en la declaración del tributo correspondiente del retenido — es un abono a su propio impuesto (renta, IVA, ICA, o la sobretasa bomberil contra la sobretasa liquidada). Contablemente el retenido la registra como saldo a favor (activo, grupo 17/13), no como gasto.
- **definitivo:** la retención no es compensable en ninguna declaración del retenido — es su pago final del tributo (caso típico: retenciones a beneficiarios que no declaran en el país). El destino del recaudo (un fondo específico, por ejemplo) **no** determina el carácter: lo determina si el retenido puede descontarla. En Colombia lo usa `RETEFUENTE_EXTERIOR`: el beneficiario del exterior sin establecimiento permanente no declara renta en el país (art. 592 num. 2) y la retención es su impuesto. Salvedad: el no residente con establecimiento permanente o que opta por declarar sí la acredita — ese caso se gestiona con el perfil del proveedor, no cambia el carácter del tributo.
- **null:** aplica a tributos aditivos que no son retenciones (IVA, INC, ICA).

> **Nota (validado con consultoría fiscal, jul-2026):** la retención de `SOBRETASA_BOMBERIL` es **anticipado** — se descuenta de la sobretasa liquidada en la declaración del retenido (que acompaña la del ICA en los municipios que la adoptaron, ej. Cali e Ibagué; no se descuenta del ICA, contra el que solo aplica `RICA`). Salvedad: la sobretasa es normativa municipal, no nacional — si algún municipio la definiera como cobro no compensable, ese caso se manejaría como excepción (hoy no se ha evidenciado ninguno).

### 8.3. Tributos sin clasificación específica

INC tiene solo una clasificación de soporte (`INC_8`); los demás conceptos no generan INC. El catálogo se mantiene minimalista — si normativa amplía las tarifas INC, se agregan clasificaciones nuevas (`INC_4`, `INC_16`).

### 8.4. Heredan jurisdicción del padre

- `RIVA` hereda jurisdicción de `IVA` (nacional).
- `SOBRETASA_BOMBERIL` hereda de `RICA` (municipal).

Esto se modela vía `factorDeTarifa: porcentajeDePadre` — el motor primero calcula el padre, luego aplica la tarifa sobre el resultado en la misma jurisdicción.

### 8.5. Frontera con `CondicionDeAplicacion`

Este catálogo declara **qué tributos existen y a qué clasificaciones aplican por default**. Las condiciones (`CondicionDeAplicacion`) ajustan el comportamiento por **perfil tributario** del sujeto: por ejemplo, si una empresa es Régimen Simple, RETEFUENTE puede quedar exonerado para ciertos conceptos. Esas reglas viven en `co-condicion-de-aplicacion.json`.

---

## 9. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.5 | 2026-09-22 | **Retención a beneficiarios del exterior como tributo autónomo `RETEFUENTE_EXTERIOR`** (issue #143; revisión con el contador de la empresa y las consultoras fiscales). Nuevo tributo directo: sustractivo, carácter **definitivo** (primer uso de este valor en CO), nacional, factor `conceptoPago` compartido con RETEFUENTE, dirección `gasto`, sin padre. Tratamientos +4 (`SERVICIOS_GRAV_19`, `SERVICIOS_GRAV_5`, `EXCLUIDO`, `EXENTO`; no en bienes gravados) → 40; regla de localización `sedeEmisora` → 12; total de entidades 66 → 72. La exclusión mutua con RETEFUENTE y la tarifa por convenio viven en `co-condicion-de-aplicacion` v1.3 (`RTF-09`, `RTF-EXT-01/02`); las tarifas en `co-tarifa-tributaria` v1.4 (stream propio, conceptos `EXTERIOR_*` retirados de RETEFUENTE); la lista de convenios en el catálogo nuevo `co-convenio-de-doble-imposicion` v1.0. Nota 8.2 actualizada. Pregunta 7 de la revisión pendiente resuelta (perfil mínimo del exterior); preguntas 8-10 nuevas. |
| 1.0 | 2026-05-26 | Carga inicial F1: 11 tributos (7 directos + 4 autorretenciones), 6 clasificaciones, 18 tratamientos, 11 reglas de localización. Fuente Estatuto Tributario + Decretos DIAN + leyes municipales. |
| 1.1 | 2026-07-10 | `ICA.direccionFiscalAplicable` pasa de `ambas` a `ingreso` (issue #93): el sujeto pasivo del ICA es quien genera el ingreso; en gasto el comprador solo practica RICA. Alineado con la implementación (`Cosmos.Impuestos#116`). |
| 1.4 | 2026-07-31 | **Autoliquidado autónomo + naturaleza `provision` (issues #117/#118, sobre la resolución del #110).** `IVA_IMPORTACION_SERVICIOS` deja de ser hijo del IVA: `factorDeTarifa` pasa de `porcentajeDePadre` a `clasificacion`, `tributoPadre` a vacío, y su tarifa es propia sobre la base (espejo de la del IVA del servicio) — el modelado padre-hijo lo descartaba por `[R14]` justo cuando el proveedor no factura IVA (el único escenario que lo justifica), y además mezclaba las reglas e hijos del IVA real con las del asumido. **Naturaleza `provision`** (modelo v2.0.8) para los 4 tributos de provisión — las autorretenciones pasan de `sustractivo` a `provision`: se reconocen sin afectar el valor a pagar/cobrar. **Clasificaciones nuevas `SERVICIOS_GRAV_19` y `SERVICIOS_GRAV_5`** (el eje de naturaleza del concepto que la norma exige: el autoliquidado solo alcanza servicios; los bienes importados liquidan su IVA en aduana) con matriz completa (22 → 36 tratamientos; 50 → 66 entidades). Pregunta 5 reencuadrada a las exclusiones del art. 476. |
| 1.3 | 2026-07-31 | **Carácter de la retención de la sobretasa bomberil: `definitivo` → `anticipado`** (issue #108, validado con las dos consultoras fiscales jul-31): la retención se descuenta de la **sobretasa liquidada** en la declaración del retenido — no del ICA, contra el que solo aplica RICA — y el retenido la registra como saldo a favor (grupo 17). El valor `definitivo` de la carga inicial se justificaba por el destino del recaudo ("va directo al fondo bomberil"), que no determina el carácter: lo determina si el retenido puede descontarla. Nota 8.2 reescrita con las definiciones correctas de cada carácter + salvedad municipal (la sobretasa es norma de cada municipio — ej. Cali e Ibagué la tienen, Bogotá no la maneja en la declaración de ICA; un municipio podría definirla no compensable y se manejaría como excepción). Se cierra la antigua pregunta 2 y se renumeran las restantes (3-8 → 2-7). ⚠️ La implementación (`ConfiguracionEstandarCo`) ya decía `anticipado` — ahora validado, deja de ser default sospechoso. |
| 1.2 | 2026-07-31 | **Renombre `AUTO_RIVA` → `IVA_IMPORTACION_SERVICIOS`** (issue #110, resolución con consultoría fiscal jul-2026): la "autorretención de IVA en ventas" del sistema legado no tiene sustento normativo y se remueve como concepto; el tributo queda definido como la autoliquidación del IVA en importación de servicios (art. 437-2 num. 3 ET). Nombre legible "IVA por importación de servicios"; el disparador pasa de `esAgenteRetenedorIVA` (residuo legado, chocaba con `RIVA-01b`) al nuevo atributo `tieneDomicilioFiscalEnElPais` de la contraparte — ver catálogo de atributos v1.1 y condiciones v1.1. Se cierra la antigua pregunta 4 (dirección `gasto` confirmada) y entran las preguntas 6-8. **Tratamientos 18 → 22** (issue #111): `AUTO_RETEFUENTE` × GRAV_19/GRAV_5/EXCLUIDO/EXENTO (`aplica: true`), alineando la matriz con la configuración estándar de la implementación. Total entidades 46 → 50. |

---

## 10. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales**:

1. **`caracterRetencion` para tributos aditivos:** ¿Es correcto dejarlo `null` para IVA, INC, ICA, o conviene usar un valor explícito (ej: `no-aplica`)?
2. **¿Faltan tributos en F1?** Casos conocidos no incluidos por simplicidad inicial: GMF (4×1000), Impuesto al Patrimonio, Impuesto al Consumo de Licores y Cervezas departamental, Estampillas municipales/departamentales. ¿Cuáles deben entrar en F1?
3. **Tratamientos para INC:** ¿Existen clasificaciones adicionales de INC (`INC_4`, `INC_16`)? Si sí, ¿cómo se diferencian (sectores)?
4. **Tratamientos para clasificaciones GRAV_*:** ¿Faltan tarifas intermedias (GRAV_8, GRAV_12, etc.)?
5. **Exclusiones del autoliquidado (art. 476):** la matriz ya declara `IVA_IMPORTACION_SERVICIOS` sobre las clasificaciones de servicios (`SERVICIOS_GRAV_19`/`SERVICIOS_GRAV_5`) — la regla del art. 420 parágrafo 3 es de destino (todo servicio del exterior con usuario en el país está gravado, salvo los excluidos del art. 476). ¿Los servicios excluidos quedan suficientemente cubiertos clasificándolos como `EXCLUIDO`, o hay casos del 476 que requieran clasificación o tratamiento propio?
6. **¿La autoliquidación se restringe a emisoras responsables de IVA** (`perteneceRegimenIVA = true`), o aplica a cualquier contratante de servicios del exterior?
7. **Perfil tributario mínimo del proveedor del exterior — resuelto (v1.5):** tipo de persona, `tieneDomicilioFiscalEnElPais = false` y `paisDeResidenciaFiscal` (obligatorio en ese caso, ver `co-catalogo-de-atributos-fiscales` v1.3). Los demás atributos requeridos se declaran con su valor por defecto (no responsable, no gran contribuyente, etc.). Queda por confirmar con los consultores si el certificado de residencia fiscal debe exigirse como soporte para aplicar la tarifa de convenio.
8. **`RETEFUENTE_EXTERIOR` — conceptos con tarifa distinta del 20 %:** el stream precarga la tarifa general del art. 408 ET para todos los conceptos aplicables. ¿Qué conceptos tienen tarifa especial (intereses de créditos a más de un año, arrendamiento financiero, transporte internacional, software) y cuál es su base? ¿Aplica el 35 % a beneficiarios en jurisdicciones no cooperantes o de baja imposición (art. 408 parágrafo)? Si sí, requeriría una segunda lista de países y una condición propia.
9. **`RETEFUENTE_EXTERIOR` — retención asumida:** cuando la empresa asume la retención (el proveedor recibe el total), ¿la base se reajusta tratando la retención asumida como mayor valor del pago (100 pagados → base 125, retención 25) y el gasto de la retención asumida es no deducible? Hoy el motor liquida sobre la base facturada; la asunción la decide OXP.
10. **Concurrencia con `IVA_IMPORTACION_SERVICIOS`:** confirmar que sobre un mismo servicio del exterior aplican a la vez la retención a beneficiarios del exterior (sustractiva) y el IVA autoliquidado (provisión).
