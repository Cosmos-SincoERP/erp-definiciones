# Nugget `IdentificacionLegal` — Especificación

| | |
|---|---|
| **Estado** | En especificación — investigación de fuentes oficiales aplicada, pendiente cierre de la Sección 10 |
| **Versión** | 0.3 |
| **Gobernanza** | [gobernanza-nuggets.md](../gobernanza-nuggets.md) |
| **Catálogo** | [catalogo-nuggets.md](../catalogo-nuggets.md) |

## Tabla de contenido

1. [Concepto](#sección-1-concepto)
2. [Atributos](#sección-2-atributos)
3. [Igualdad y clave canónica](#sección-3-igualdad-y-clave-canónica)
4. [Reglas de validación](#sección-4-reglas-de-validación)
5. [Operaciones](#sección-5-operaciones)
6. [Datos embebidos](#sección-6-datos-embebidos)
7. [Ejemplos por país](#sección-7-ejemplos-por-país)
8. [Fuera de responsabilidad](#sección-8-fuera-de-responsabilidad)
9. [Consumidores](#sección-9-consumidores)
10. [Revisión pendiente](#sección-10-revisión-pendiente)

---

## Sección 1: Concepto

La **Identificación Legal** es la identidad documental de una persona o empresa: la combinación de tipo de documento + número + país emisor que la identifica de forma única en el ERP, acompañada del dígito de verificación cuando el tipo de documento lo contempla.

**Por qué "legal":** lo que distingue a este concepto de cualquier otro identificador del ERP (números de factura, identificadores de registros, códigos internos) es que **la emite o la reconoce una autoridad** — la Registraduría o la DIAN en Colombia, la JCE o la DGII en República Dominicana, el Tribunal Electoral o la DGI en Panamá. De esa cualidad se deriva su alcance: identifica personas y empresas ante el Estado, y por eso sirve como clave natural entre sub-dominios.

Es la **clave natural universal** del ERP: todos los sub-dominios que registran o referencian personas y empresas (proveedores en OXP, perfiles tributarios en Impuestos, clientes en facturación, terceros en la bodega) construyen y comparan identificaciones con este Nugget, y la bodega de Terceros **consolida por esta clave**. Que la validación sea idéntica en todos los bordes de captura es lo que garantiza que el mismo documento llegue a la bodega con la misma clave, sin importar desde qué sub-dominio se registró.

**Origen del concepto:** VO `Identificacion` del modelo de Terceros v1.0 (sección 3.3.1), generalizado como Nugget por el replanteamiento de junio 2026 y **renombrado `IdentificacionLegal`** al formalizarse (el nombre original solo era inequívoco dentro del agregado Tercero; como pieza transversal necesitaba el calificador — Sección 4 de la gobernanza). Hereda sus decisiones estructurales — en particular `[D6]` (el dígito de verificación está fuera de la clave de igualdad) y el comportamiento de `[SI2]` (validar DV capturado o calcularlo cuando no se provee).

---

## Sección 2: Atributos

| Atributo | Tipo | Obligatorio | Descripción |
|----------|------|:-----------:|-------------|
| `tipoDocumento` | string (código del catálogo embebido) | Sí | Tipo de documento (ej: `NIT`, `CC`, `RNC`, `CIP`). Código semántico e inmutable. |
| `numero` | string | Sí | Número del documento, normalizado según `[V03]`. En los tipos con **DV separado** (`dvEmbebido = false`, ej: NIT) nunca lo contiene `[V05]`; en los tipos con **DV embebido** (`dvEmbebido = true`, ej: cédula y RNC dominicanos) el verificador **es el último dígito del número** — así lo trata la autoridad emisora. |
| `pais` | string (ISO 3166-1 alfa-2) | Sí | País emisor del documento (ej: `CO`, `DO`, `PA`). Para los tipos globales del catálogo (`PASAPORTE`, `TIN`), es el país que emitió el documento. |
| `digitoVerificacion` | string | Solo si `tieneDv = true` **y** `dvEmbebido = false` | Dígito(s) de verificación cuando viajan **separados** del número: 1 dígito en el NIT colombiano, 2 dígitos en el RUC/NT panameño. **No participa en la igualdad** (Sección 3). Los tipos con DV embebido no usan este atributo. |

El Nugget es **inmutable**: cualquier cambio implica construir una nueva instancia. Las reglas de la Sección 4 aplican al construir.

---

## Sección 3: Igualdad y clave canónica

**Igualdad:** dos `IdentificacionLegal` son iguales si y solo si coinciden `(tipoDocumento, numero, pais)`. El `digitoVerificacion` **no forma parte** de la comparación — es un dato derivado o capturado que acompaña a la clave, no la define (decisión heredada de Terceros `[D6]`: un DV mal capturado en datos legados no debe producir dos identidades distintas). En los tipos con DV embebido el verificador sí integra el número — y por tanto la clave — porque así define el documento la autoridad emisora.

**Clave canónica:** el Nugget expone una representación textual única de la identidad, destinada a correlación entre sub-dominios y a la consolidación en la bodega:

```
{pais}:{tipoDocumento}:{numero}        →   CO:NIT:900123456
```

Todo evento de integración que viaje hacia la bodega de Terceros u otro sub-dominio identifica a la persona o empresa por esta clave (o por los tres atributos que la componen), nunca por identificadores internos del sub-dominio emisor.

---

## Sección 4: Reglas de validación

Todas las reglas se evalúan al construir la instancia, en el orden indicado, **sin salir del proceso** (filtro 3 de la gobernanza): solo consultan los datos embebidos de la Sección 6.

| Regla | Descripción |
|-------|-------------|
| `[V01]` | **País válido:** `pais` debe existir en el catálogo embebido de países (ISO 3166-1 alfa-2, en mayúsculas). |
| `[V02]` | **Tipo de documento válido para el país:** `tipoDocumento` debe existir en el catálogo embebido de tipos de documento, estar `activo`, y su `paisCodigo` debe coincidir con `pais` **o** ser global (`paisCodigo` nulo: `PASAPORTE`, `TIN`). Un tipo `activo = false` (ej: `PEP`, vencido desde feb-2023) rechaza capturas nuevas; los valores históricos almacenados no se revalidan (regla de evolución 3 de la gobernanza). |
| `[V03]` | **Normalización del número:** antes de validar, el número se normaliza: se eliminan espacios al inicio y al final, se convierte a mayúsculas, y se eliminan los separadores no significativos (puntos, espacios intermedios). Los guiones se eliminan **salvo** en los tipos cuyo formato los declara como parte del número (`separadorSignificativo = true` — tipos de Panamá, donde `8-123-4567` y `81234567` son números distintos). Los ceros iniciales **se conservan** (significativos en la cédula dominicana: serie `001`). El valor almacenado es siempre el normalizado. |
| `[V04]` | **Formato por tipo de documento:** el número normalizado debe cumplir el conjunto de caracteres y el rango de longitud que el catálogo embebido publica para el `tipoDocumento` (`formatoNumero`, `longitudMin`, `longitudMax`). Ej: `NIT` solo dígitos, 1–13; `CIE-DO` exactamente 11 dígitos; `CIP` dígitos, letras de prefijo y guiones. |
| `[V05]` | **El número no embebe un DV separado:** en los tipos con `dvEmbebido = false` y `tieneDv = true`, `numero` no contiene el DV — viaja en `digitoVerificacion`. Para `NIT` (CO): la DIAN define el DV como dato que "no se considera como número integrante del NIT" (IN-CAC-0237); si la entrada llega con el DV concatenado, la construcción la rechaza indicando que debe separarse (no lo separa automáticamente: asumir que el último dígito es un DV corrompería números legados). |
| `[V06]` | **Dígito de verificación.** Según los campos del catálogo para el tipo: **(a) algoritmo calculable + DV separado capturado** → se valida contra el cálculo; si no coincide: con `politicaDv = rechazo` la construcción falla (NIT — la DIAN calcula el DV con el mismo algoritmo); con `politicaDv = advertencia` la instancia se construye marcada con advertencia (existen documentos reales emitidos que no cumplen el algoritmo). **(b) algoritmo calculable + DV separado no provisto** → se calcula automáticamente y la instancia nace con él (comportamiento heredado de `[SI2]` de Terceros). **(c) DV embebido** (`dvEmbebido = true`: cédula y RNC dominicanos) → el verificador es el último dígito de `numero` y se valida en sitio aplicando la `politicaDv` del tipo; no hay nada que calcular por aparte. **(d) `algoritmoDv = capturado`** (RUC/NT Panamá) → el DV es **obligatorio**, se acepta sin validar y no es calculable (la DGI lo asigna y puede cambiarlo al inscribirse el contribuyente; la verificación autoritativa es su servicio externo — Sección 8). |
| `[V07]` | **DV sin contexto:** si se provee `digitoVerificacion` para un tipo que no lo contempla (`tieneDv = false`) o que lo lleva embebido (`dvEmbebido = true`), la construcción falla — un DV donde no corresponde es señal de captura errónea, no un dato que ignorar en silencio. |

---

## Sección 5: Operaciones

| Operación | Descripción |
|-----------|-------------|
| `esIgualA(otra)` | Igualdad por `(tipoDocumento, numero, pais)` — Sección 3. |
| `claveCanonica()` | Retorna `{pais}:{tipoDocumento}:{numero}` — Sección 3. |
| `calcularDigitoVerificacion()` | Retorna el DV según el algoritmo del catálogo para el tipo. Falla de forma explícita si el tipo no tiene DV, lo lleva embebido, o su algoritmo es `capturado`. |
| `tieneAdvertenciaDv()` | `true` cuando la instancia se construyó bajo `politicaDv = advertencia` con un verificador que no cumple el algoritmo (documento real emitido fuera de la regla). Permite al consumidor decidir si exige verificación externa antes de usos fiscales. |
| `aplicaA()` | Retorna a quién aplica el tipo de documento según el catálogo: `personaNatural`, `personaJuridica` o `ambos`. El Nugget **expone** el dato; la coherencia con el tipo de persona del registro la verifica el consumidor (Sección 8). |
| `presentacion()` | Representación para mostrar al usuario: `numero` con el DV separado sufijado cuando existe (`900123456-8`, `155720753-2-2022 DV 39`). Los formatos de presentación con separadores locales (puntuación de miles, agrupación `XXX-XXXXXXX-X` de la cédula dominicana) son decisión de cada interfaz, no del Nugget. |

---

## Sección 6: Datos embebidos

Los datos viajan dentro del paquete (carpeta [`datos/`](datos/)) y los produce **Datos de Referencia** en su rol de taller del custodio. Se congelan en cada versión del paquete; actualizarlos es una versión **menor** (Sección 7 de la gobernanza).

| Archivo | Contenido | Estado |
|---------|-----------|--------|
| [`datos/tipos-documento-identidad.json`](datos/tipos-documento-identidad.json) | 46 entradas (10 CO, 4 DO, 5 PA, 25 de otros países, 2 globales) **extendidas con el contrato de validación** de esta sección. Los 19 tipos F1 + 2 globales con valores investigados en fuentes oficiales; los 25 tipos de países no F1 con validación genérica hasta la habilitación productiva del país (alineado con `[D7]` de Impuestos). | ✅ Producido (jun-2026) |
| Catálogo de países | `[V01]` valida contra el catálogo del Nugget [`Pais`](../pais/especificacion.md) (195) — fuente única de datos de país dentro del paquete; la copia local `datos/paises.json` producida en 0.2 quedó sustituida (jun-2026). | ✅ Vía Nugget `Pais` |
| [`datos/algoritmos-dv.md`](datos/algoritmos-dv.md) | Definición normativa de los 4 valores de `algoritmoDv`, con fuentes oficiales y **casos de prueba verificados aritméticamente** contra identificaciones reales (NITs de DIAN/Ecopetrol/Bancolombia; RNC de DGII/JCE/Banreservas). | ✅ Producido (jun-2026) |

### Contrato de datos — campos de validación del catálogo de tipos de documento

Extensión sobre los campos base del catálogo de Datos de Referencia (`codigo`, `descripcion`, `paisCodigo`, `aplicaA`, `activo`):

| Campo | Tipo | Descripción |
|-------------|------|-------------|
| `formatoNumero` | enum | Conjunto de caracteres del número normalizado: `numerico`, `alfanumerico`, `alfanumerico-con-guiones`. |
| `longitudMin` / `longitudMax` | entero | Rango de longitud del número normalizado (incluye guiones cuando son significativos). |
| `separadorSignificativo` | booleano | `true` cuando los guiones son parte del número y no se eliminan en `[V03]` (tipos de Panamá). |
| `tieneDv` | booleano | El tipo contempla dígito de verificación. |
| `dvEmbebido` | booleano | `true` cuando el verificador es el último dígito de `numero` (cédula y RNC dominicanos); `false` cuando viaja separado (NIT, RUC). |
| `algoritmoDv` | enum | Solo si `tieneDv`: `modulo11-dian`, `luhn-cedula-do`, `modulo11-rnc`, `capturado`. Definiciones y casos de prueba en [`datos/algoritmos-dv.md`](datos/algoritmos-dv.md). |
| `politicaDv` | enum | Solo si el algoritmo es calculable: `rechazo` (DV inválido = error de captura — NIT) o `advertencia` (existen documentos reales emitidos fuera del algoritmo: ~800 cédulas y ~20 RNC dominicanos — la instancia se construye marcada, ver `tieneAdvertenciaDv()`). |
| `validacionGenerica` | booleano | Marca los tipos de países no F1 que aún no tienen reglas investigadas (alfanumérico 1–30). Se retira al habilitar el país. |
| `notas` | string | Contexto normativo de la entrada (fuentes, rangos, advertencias operativas). |

> **Correcciones aplicadas con respaldo oficial** (propagadas también al catálogo fuente de Datos de Referencia, alcance v1.1): **(1)** `NIT.aplicaA = ambos` — la DIAN asigna NIT también a personas naturales vía RUT (Estatuto Tributario art. 555-1; doc. DIAN/OCDE); **(2)** `RNC.aplicaA = ambos` — el RNC con prefijo `5` se asigna a personas físicas extranjeras (DGII CA1009); **(3)** `PEP.activo = false` — vencido desde el 28-feb-2023, la DIAN no lo acepta en el RUT (IN-CAC-0237 §8.10). |

---

## Sección 7: Ejemplos por país

### Colombia (CO)

| Entrada | Resultado | Razón |
|---------|-----------|-------|
| `NIT` `800.197.268` DV no provisto | ✅ `CO:NIT:800197268`, DV calculado = `4` | NIT real de la DIAN. `[V03]` elimina puntos; `[V06b]` calcula con `modulo11-dian`. |
| `NIT` `899999068` DV `1` | ✅ | NIT real de Ecopetrol — caso borde residuo 1 → DV = 1. |
| `NIT` `900123456` DV `5` | ❌ | `[V06a]` + `politicaDv = rechazo`: DV no coincide (el correcto es `8`). |
| `NIT` `900123456-8` (todo en el número) | ❌ | `[V05]`: el DV no integra el NIT (IN-CAC-0237) — debe capturarse separado. |
| `CC` `79456123` | ✅ `CO:CC:79456123`, sin DV | Cédula antigua (rango 1–99.999.999). |
| `CC` `79456123` DV `4` | ❌ | `[V07]`: DV provisto para un tipo que no lo contempla. |
| `PEP` `123456789012345` (captura nueva) | ❌ | `[V02]`: tipo inactivo desde feb-2023. Los PEP históricos almacenados no se revalidan. |
| `RNC` `131234567` con `pais = CO` | ❌ | `[V02]`: `RNC` pertenece a `DO`. |

### República Dominicana (DO)

| Entrada | Resultado | Razón |
|---------|-----------|-------|
| `CIE-DO` `001-1391820-5` | ✅ `DO:CIE-DO:00113918205` | `[V03]` elimina guiones (no significativos en DO) y **conserva los ceros iniciales**; `[V06c]`: el verificador (último dígito) cumple Luhn. La cédula son los 11 dígitos completos — DV embebido, sin atributo separado. |
| `CIE-DO` `00000021249` | ⚠️ válida con advertencia | Cédula real emitida por la JCE que no cumple Luhn — `politicaDv = advertencia` construye y marca (`tieneAdvertenciaDv() = true`). |
| `CIE-DO` `00113918205` DV `5` | ❌ | `[V07]`: el verificador va embebido — proveerlo aparte es captura errónea. |
| `RNC` `401-50625-4` | ✅ `DO:RNC:401506254` | RNC real de la DGII; 9º dígito verificador embebido cumple `modulo11-rnc`. |
| `RNC` `101581601` | ⚠️ válida con advertencia | RNC real que no cumple el algoritmo (excepción documentada). |

### Panamá (PA)

| Entrada | Resultado | Razón |
|---------|-----------|-------|
| `CIP` `8-926-1601` | ✅ `PA:CIP:8-926-1601` | Ejemplo oficial del Tribunal Electoral (provincia-tomo-asiento). `separadorSignificativo = true`: los guiones se conservan. |
| `CIP` `81234567` | ✅ pero es **otra** identificación distinta de `8-123-4567` | Consecuencia del separador significativo — la captura en Panamá debe forzar la estructura con guiones. |
| `CIP` `PE-5-614` / `N-17-371` / `8AV-1-196` | ✅ | Prefijos oficiales: nacido en el extranjero, naturalizado, inscripciones previas a la vigencia. |
| `RUC-PA` `155586106-2-2014` DV `05` | ✅ sin validación local del DV | Formato jurídico SIR folio-2-año (Nota DG-SG-027-2015 del Registro Público). `[V06d]`: DV asignado por la DGI, capturado. |
| `RUC-PA` `155586106-2-2014` sin DV | ❌ | `[V06d]`: con algoritmo `capturado` el DV no es calculable — es obligatorio. |
| `NT-PA` `8-NT-1-24` DV `33` | ✅ | Número Tributario para extranjeros sin cédula (formato del documento oficial del algoritmo DGI). |

### Globales

| Entrada | Resultado | Razón |
|---------|-----------|-------|
| `PASAPORTE` `AV123456` con `pais = ES` | ✅ `ES:PASAPORTE:AV123456` | `[V02]`: tipo global válido para cualquier país del catálogo. |
| `RFC` (México) `GODE561231GR8` | ✅ con validación genérica | País no F1: alfanumérico 1–30 (`validacionGenerica = true`) hasta habilitar MX. |

---

## Sección 8: Fuera de responsabilidad

Lo que este Nugget **no** hace — y dónde vive esa responsabilidad:

| Fuera del Nugget | Responsable |
|------------------|-------------|
| **Verificación contra registros oficiales**: comprobar que el documento exista y pertenezca a quien dice. Servicios identificados: consulta RUT (DIAN, CO), consulta de contribuyentes en el padrón ([DGII](https://dgii.gov.do/herramientas/consultas/Paginas/RNC.aspx), DO), `feConsRucDV.svc` / `feConsLoteRucDV.svc` (DGI, PA — además, único validador autoritativo del DV panameño, que puede cambiar al inscribirse el contribuyente). | Capacidad externa futura, **no bloqueante** — enriquece después, nunca condiciona un registro. Recomendada antes de usos fiscales cuando `tieneAdvertenciaDv() = true`. |
| **Unicidad**: que no existan dos registros con la misma identificación dentro de un sub-dominio. | Cada sub-dominio consumidor (su invariante local, ej: `[I1]` de Terceros). |
| **Detección de duplicados y homonimia** entre registros con identificaciones distintas (la antigua `[I11]` de Terceros). | Proceso de conciliación de la bodega de Terceros, con resolución humana. |
| **Coherencia con el tipo de persona** del registro (un `RNC` prefijo `1` en una persona natural, una `CC` en una empresa). El Nugget expone `aplicaA()`; la verificación usa datos que el Nugget no conoce. | El sub-dominio consumidor al construir su registro. |
| **Homologación a códigos de autoridades** (ej: códigos DIAN de factura electrónica: `13` CC, `31` NIT, `48` PPT…). El Nugget usa códigos semánticos propios del ERP. | `HomologacionFiscal` de Impuestos / Emisión Electrónica. |
| **Razón social / nombre** de la persona o empresa. No es parte de la identidad documental. | Candidato `InformacionTercero` del catálogo (en evaluación) o composición local de cada consumidor. |
| **Vigencia o estado del documento** (documento vencido, cancelado por la registraduría). | Fuera del alcance del ERP en F1. |

---

## Sección 9: Consumidores

Adopción prevista según la [matriz del catálogo](../catalogo-nuggets.md#matriz-de-consumidores): Terceros (bodega — clave de consolidación), OXP (agregado `Proveedor`), Impuestos (`PerfilTributario`), Contabilidad (tercero de las partidas), CXC/Facturación (`Cliente`), Tesorería. Cada adopción confirmada se registra en la matriz del catálogo, no aquí.

---

## Sección 10: Revisión pendiente

### Cerrado por la investigación de fuentes oficiales (junio 2026)

| # | Pregunta original | Resolución |
|---|----------|------------|
| 1 | Formatos y longitudes por tipo (CO) | **Cerrado con matiz.** No existe tabla oficial DIAN de longitudes por tipo (el "CC: 3–10" que circula no es DIAN). Cotas oficiales adoptadas: CC 1–10 (antiguas 1–8; NUIP 10), NIT 1–13 (jurídicas exactamente 9 desde 800 millones), NUIP exactamente 10; exógena admite hasta 20 caracteres y factura electrónica 3–30 sin ceros a la izquierda. TI/RC/CE/PPT quedaron con rangos flexibles documentados en `notas` por ausencia de especificación oficial. |
| 2 | ¿NIT para personas naturales? | **Confirmado: sí** (Estatuto Tributario art. 555-1 + doc. DIAN/OCDE). `aplicaA = ambos` aplicado a los datos del Nugget. |
| 3 | Cédula dominicana: ¿verificador embebido o separado? | **Confirmado: embebido** — la cédula son 11 dígitos incluido el verificador Luhn; ceros iniciales se conservan. Introdujo el campo `dvEmbebido` al contrato. Además: las personas físicas nacionales usan la cédula como identificación tributaria (DGII CA979) — no existe RNC de 9 dígitos para nacionales. |
| 4 | Algoritmo RNC | **Confirmado y verificado** contra los RNC reales de DGII, JCE y Banreservas: módulo 11, pesos `7,9,8,6,5,4,3,2`, residuo 0 → 2, residuo 1 → 1, otro → 11−residuo. Prefijos: `1` jurídica lucrativa, `4` no lucrativa/estatal, `5` física extranjera (oficial DGII). |
| 5 | RUC Panamá | **Confirmado.** DV = 2 dígitos asignados por la DGI, **puede cambiar al inscribirse el contribuyente**; existe algoritmo oficial (doc. DGI v201805) pero la decisión F1 es `capturado` + verificación vía `feConsRucDV.svc` (no bloqueante). RUC polimórfico documentado (cédula / folio-2-año / folio-3-año / ficha-rollo-imagen / NT). DV obligatorio al capturar. |
| 6 | Tipos de los otros países | **Decidido y aplicado:** validación genérica (`alfanumerico` 1–30, `validacionGenerica = true`) hasta la habilitación productiva de cada país, alineado con `[D7]` de Impuestos. |
| 7 | Producir `datos/` | **Hecho:** `tipos-documento-identidad.json` (46 entradas extendidas), `paises.json` (195), `algoritmos-dv.md` (4 algoritmos con casos de prueba verificados). |

### Pendientes restantes para `Publicado` v1.0

| # | Pendiente | Owner | Criterio de cierre |
|---|----------|-------|--------------------|
| ~~P1~~ | ✅ **Cerrado (jun-2026):** las 3 correcciones (NIT `ambos`, RNC `ambos`, PEP `inactivo`) se propagaron por edición directa al catálogo fuente de Datos de Referencia (alcance v1.1) — el replanteamiento ya había reabierto ese servicio. | — | Catálogo fuente y datos del Nugget alineados. ✓ |
| P2 | **Afinar rangos flexibles sin fuente oficial** (CE, TI, RC, PPT en CO; CRM en DO): los rangos adoptados son seguros pero amplios. Prioridad baja — el costo de un rango amplio es aceptar capturas raras, no rechazar válidas. | Consultor fiscal CO/DO | Rangos confirmados o ratificados como flexibles. |
| P3 | **Validar los vectores de prueba del RUC panameño** contra el servicio oficial `feConsRucDV.svc` antes de fijarlos en la suite de pruebas (los DV provienen de la implementación comunitaria que reproduce el doc. oficial DGI). | Equipo técnico | Vectores ratificados por el servicio de la DGI. |
| P4 | **Ratificación del consultor fiscal** de las políticas `advertencia` (DO) y `capturado` (PA) como tratamiento aceptable para usos fiscales (emisión de comprobantes). | Consultor fiscal DO/PA | Visto bueno o ajuste de política. |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.3 | Junio 2026 | **Renombrado `Identificacion` → `IdentificacionLegal`** (decisión del usuario): el nombre original era ambiguo fuera del agregado Tercero — en un ERP todo tiene identificación. El calificador "legal" captura la cualidad distintiva: la emite o reconoce una autoridad. Carpeta renombrada a `identificacion-legal/`; atributos, reglas y clave canónica sin cambios. |
| 0.2 | Junio 2026 | **Investigación de fuentes oficiales aplicada** (DIAN/OCDE, Registraduría, DGII, JCE, Tribunal Electoral, DGI/Registro Público de Panamá). Algoritmos confirmados y verificados aritméticamente contra identificaciones reales. Contrato extendido con `dvEmbebido` y `politicaDv` (cédula/RNC dominicanos llevan el verificador embebido; existen documentos reales fuera del algoritmo → advertencia, no rechazo). `[V06]` reescrita en 4 casos; nueva operación `tieneAdvertenciaDv()`. `datos/` producido: 46 tipos extendidos + 195 países + documento de algoritmos. Correcciones con respaldo oficial: NIT y RNC `aplicaA = ambos`, PEP inactivo. Sección 10 reorganizada: 7 preguntas cerradas, 4 pendientes restantes (P1–P4). |
| 0.1 | Junio 2026 | Borrador inicial en especificación. 4 atributos (DV fuera de la igualdad, heredando `[D6]` de Terceros), clave canónica `{pais}:{tipoDocumento}:{numero}`, 7 reglas de validación `[V01]`–`[V07]`, 5 operaciones, contrato de extensión del catálogo de tipos de documento, algoritmo `modulo11-dian` documentado, ejemplos CO/DO/PA, 6 exclusiones de responsabilidad y 7 puntos de revisión pendiente. |
