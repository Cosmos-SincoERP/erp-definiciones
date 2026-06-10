# Algoritmos de dígito de verificación — Nugget `IdentificacionLegal`

Definición normativa de los valores del campo `algoritmoDv` del catálogo embebido (`tipos-documento-identidad.json`). Todos los casos de prueba fueron **verificados aritméticamente** contra identificaciones reales de fuentes oficiales (junio 2026).

---

## `modulo11-dian` — NIT (Colombia)

**Fuente normativa:** Orden Administrativa 4 de 1989 de la DIAN, citada como origen del algoritmo en el [documento oficial DIAN/OCDE sobre el TIN colombiano](https://www.oecd.org/content/dam/oecd/en/topics/policy-issue-focus/aeoi/colombia-tin.pdf). El DV es un **dato separado del número** (no integra el NIT — instructivo DIAN IN-CAC-0237, casilla 6) y en factura electrónica viaja en el atributo `@schemeID`, aparte del `CompanyID`.

**Algoritmo:**

1. A cada dígito del número (sin DV), de **derecha a izquierda**, se le asigna el peso correspondiente de la serie de primos: `3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71` (soporta hasta 15 dígitos).
2. `suma` = Σ (dígito × peso); `residuo = suma mod 11`.
3. **Si el residuo es 0 o 1 → DV = residuo.** En caso contrario → `DV = 11 − residuo`. El DV siempre es un solo dígito 0–9.

**Política:** `rechazo` — el DV lo calcula la DIAN con este mismo algoritmo; un DV capturado que no coincida es un error de captura.

**Casos de prueba verificados** (NITs públicos reales):

| Entidad | Número | DV esperado | Verificación |
|---|---|:---:|---|
| DIAN | `800197268` | `4` | ✓ residuo 7 → 11−7 = 4 |
| Ecopetrol S.A. | `899999068` | `1` | ✓ residuo 1 → DV = 1 (caso borde residuo 1) |
| Bancolombia S.A. | `890903938` | `8` | ✓ residuo 3 → 11−3 = 8 |
| Ejemplo de la especificación | `900123456` | `8` | ✓ suma 586, residuo 3 → 8 |

---

## `luhn-cedula-do` — Cédula de Identidad y Electoral (República Dominicana)

**Fuente:** estructura oficial de 11 dígitos confirmada por la DGII (que trata "9 dígitos para RNC y 11 dígitos para cédulas"); el algoritmo Luhn **no está publicado por la JCE** — es convergencia total de la comunidad (python-stdnum, librerías dominicanas) verificada aritméticamente. El verificador es el **dígito 11, embebido en el número** (`dvEmbebido = true`): la cédula nunca se maneja sin él.

**Algoritmo (Luhn / módulo 10 sobre los 10 primeros dígitos):**

1. De **derecha a izquierda** sobre los 10 primeros dígitos, duplicar los dígitos en posiciones impares (1ª, 3ª, 5ª…); si el resultado es > 9, restarle 9.
2. `suma` = Σ de todos los valores; `verificador = (10 − suma mod 10) mod 10`.
3. El verificador debe coincidir con el dígito 11 de la cédula.

**Política:** `advertencia` — la JCE emitió **~800 cédulas reales que no cumplen Luhn** (ej. documentadas: `00000021249`, `00100012146`, `03100001162`). La validación advierte y permite continuar; la verificación fuerte es contra el padrón (consulta DGII/JCE, capacidad externa no bloqueante).

**Casos de prueba:**

| Cédula | Resultado esperado |
|---|---|
| `00113918205` (001-1391820-5) | ✓ válida (suma Luhn ≡ 0 mod 10) |
| `00113918204` | ✗ inválida (verificador incorrecto) |
| `00000021249` | ✗ por algoritmo pero **emitida y real** → caso de la política `advertencia` |

---

## `modulo11-rnc` — RNC (República Dominicana)

**Fuente:** estructura oficial confirmada por la DGII ([CA979](https://ayuda.dgii.gov.do/conversations/definiciones/ca979-qu-es-el-registro-nacional-de-contribuyentes-rnc/5f3c17668cd858ce879ba489), [CA1009](https://ayuda.dgii.gov.do/conversations/registro-nacional-de-contribuyentes-rnc/ca1009-cul-es-la-estructura-del-cdigo-del-rnc/5f3c17608cd858ce879a5814)): 9 dígitos = tipo (1) + secuencia (7) + verificador (1). Primer dígito: `1` jurídica lucrativa, `4` no lucrativa/estatal, `5` persona física extranjera. El algoritmo no está publicado oficialmente — convergencia de comunidad verificada contra RNC institucionales reales. Verificador **embebido** (`dvEmbebido = true`).

**Algoritmo (módulo 11 sobre los 8 primeros dígitos):**

1. Multiplicar los 8 primeros dígitos, de izquierda a derecha, por los pesos `7, 9, 8, 6, 5, 4, 3, 2`.
2. `suma` = Σ productos; `residuo = suma mod 11`.
3. **residuo 0 → verificador = 2; residuo 1 → verificador = 1; otro → verificador = 11 − residuo.** (Siempre 1–9, nunca 0.)

**Política:** `advertencia` — existen ~20 RNC reales que no cumplen el algoritmo (ej. documentado: `101581601`).

**Casos de prueba verificados** (instituciones reales, fuentes oficiales):

| Institución | RNC | Verificación |
|---|---|---|
| DGII | `401506254` (401-50625-4) | ✓ suma 106, residuo 7 → 11−7 = 4 |
| Junta Central Electoral | `401007541` (401-00754-1) | ✓ suma 87, residuo 10 → 11−10 = 1 |
| Banreservas | `401010062` (4-01-01006-2) | ✓ suma 53, residuo 9 → 11−9 = 2 |
| Excepción real | `101581601` | ✗ por algoritmo (calcula 3, real 1) → caso de la política `advertencia` |

---

## `capturado` — RUC y NT (Panamá)

**Fuente:** la DGI confirma que el DV son **2 dígitos (00–99) asignados automáticamente por el sistema** a cada RUC ([dgi.mef.gob.pa/DV](https://dgi.mef.gob.pa/DV)), y advierte que **el DV puede cambiar cuando el contribuyente se inscribe** — por eso no es un dato derivable de forma estable.

**Sí existe algoritmo oficial:** "Cálculo Dígito Verificador RUC — Versión 201805" (DGI, 24 páginas, anunciado por [@DGIpma](https://x.com/DGIpma/status/1009479743975182337)): módulo 11 ponderado en dos pasadas (DV1 y DV2) sobre un campo de 20 posiciones, con rutinas especiales para RUC jurídico antiguo (referencia cruzada), sustitución de letras (E, N, PE → 5) y truncamiento de asientos de 7 posiciones. El PDF ya no está enlazado en el sitio de la DGI; sobreviven [copias verificables](https://github.com/juancorradine/Panama-RUC-DV-Calculator/blob/master/dgi/Calculo_Digito_Verificador_RUC.pdf).

**Decisión F1:** `capturado` — el DV se captura siempre (obligatorio) y **no se valida ni calcula localmente**, por tres razones: la complejidad y casos especiales del algoritmo 201805, el hecho de que la DGI puede reasignar el DV al inscribirse el contribuyente, y la existencia de servicios oficiales de verificación. La verificación autoritativa es el servicio web de la DGI definido en la Ficha Técnica de Factura Electrónica (Resolución 201-5784 de 2018): `feConsRucDV.svc` / `feConsLoteRucDV.svc` en `dgi-fepws.mef.gob.pa` — **capacidad externa no bloqueante**. Implementar el algoritmo 201805 como pre-validación local queda como opción F2.

**Vectores de referencia** (ejemplos resueltos del documento oficial DGI; DV según la implementación comunitaria que lo reproduce — validar contra `feConsRucDV` antes de fijarlos como pruebas):

| RUC | Tipo | DV |
|---|---|:---:|
| `10102-64-103462` | Jurídico formato viejo (Ejemplo #1 del doc DGI) | 30 |
| `8-769-1080` | Natural (cédula) | 56 |
| `155720753-2-2022` | Jurídico SIR (folio-2-año) | 39 |
| `8-NT-1-24` | NT natural | 33 |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Junio 2026 | Versión inicial. 4 valores de `algoritmoDv` documentados con fuentes y casos de prueba verificados aritméticamente (3 NITs públicos CO, 3 RNC institucionales DO, cédulas de prueba DO, vectores de referencia PA). |
