# Catálogo Condiciones de Aplicación — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `CondicionDeAplicacion` (Sección 3.4 de `modelo-dominio.md`)
**Versión:** 1.2
**Fecha de actualización:** 2026-07-31
**Archivo de datos:** [`co-condicion-de-aplicacion.json`](co-condicion-de-aplicacion.json)

---

## 1. Propósito

Catálogo de **reglas declarativas** que el motor de cálculo evalúa contra `PerfilTributario` y `ubicaciones` de cada transacción para ajustar el tratamiento tributario. Cada condición:

- Identifica un **tributo afectado** (IVA, RETEFUENTE, RIVA, RICA, IVA territorial, etc.).
- Declara la **entidad evaluada** (`emisora`, `contraparte`, ambas, o ninguna para reglas territoriales).
- Lista uno o más **criterios** (atributos del perfil y valores esperados).
- Define un **efecto** (`aplicar`, `noAplicar`, `aplicar-default`, `reverseCharge`, `aplicar-sin-calidades`).
- Acota su **dirección fiscal aplicable** (`ingreso`, `gasto`, o `ambas`).

Las condiciones se evalúan después de que el motor resuelve qué tributos aplican por clasificación (vía `Tratamiento`) — son el **segundo filtro**: ajustan por perfil del sujeto.

---

## 2. Fuente normativa

- **RETEFUENTE — reglas por calidad del agente:** Estatuto Tributario arts. 365 a 419, Decreto Único 1625/2016.
- **RIVA — reglas de agente retenedor IVA:** Estatuto Tributario art. 437-2.
- **RICA — calidades ICA municipal:** Estatutos tributarios municipales + Ley 14/1983.
- **Régimen Simple:** Ley 1943/2018 modificada por Ley 2010/2019.
- **IVA_IMPORTACION_SERVICIOS — autoliquidación del IVA:** Estatuto Tributario art. 437-2 numeral 3 (contratación de servicios gravados con proveedores sin residencia ni domicilio en el país) + art. 437-1 (retención del 100%).
- **Régimen Puerto Libre — IVA territorial:** Constitución art. 310 + Ley 47/1993 art. 22.

---

## 3. Cobertura del catálogo

**Total: 31 condiciones.** Distribución por tributo:

| Tributo | Condiciones | Notas |
|---|:---:|---|
| RETEFUENTE | 15 | 14 condiciones específicas + 1 default. Patrón asimétrico: cada caso real se desdobla en perspectivas `gasto` (sub-código `a`) e `ingreso` (sub-código `b`). |
| RIVA | 4 | Casos de agente retenedor IVA. Con proveedor del exterior RIVA simplemente no aplica (el proveedor no pertenece al régimen ni factura IVA) — no requiere condición propia. |
| RICA | 5 | Casos por calidades ICA + gran contribuyente Bogotá. |
| IVA | 3 | 2 casos de pertenencia régimen IVA + 1 caso territorial Puerto Libre. |
| AUTO_RETEFUENTE | 1 | Activación por autorretenedora. |
| IVA_IMPORTACION_SERVICIOS | 1 | Activación por proveedor sin domicilio fiscal en el país. |
| AUTO_RICA | 1 | Activación por autorretenedora ICA. |
| AUTO_RENTA | 1 | Activación por autorretenedora de renta. |

---

## 4. Modelo de condiciones — Patrón asimétrico

Las reglas tributarias con **perspectiva asimétrica** (que solo tienen sentido normativo en una dirección) se modelan como **dos condiciones independientes**: una evaluando `emisora` con dirección fija, otra evaluando `contraparte` con la dirección opuesta. Las reglas **bilaterales** (Régimen Simple) se mantienen como una sola condición con `direccionFiscalAplicable: ambas`.

Ejemplo:

| Código | Entidad evaluada | Atributo | Efecto | Dirección |
|---|---|---|---|---|
| `RTF-02a` | emisora | esExentoRetefuente=true | noAplicar | ingreso |
| `RTF-02b` | contraparte | esExentoRetefuente=true | noAplicar | gasto |

Esto preserva el **lenguaje fiscal del dominio** (`emisora`/`contraparte` como roles posicionales) y permite que el motor evalúe la condición sólo en la dirección correcta sin requerir lógica inversa.

---

## 5. Condiciones — RETEFUENTE (15)

### 5.1. Exenciones generales (8)

| Código | Caso | Atributo evaluado | Efecto | Dirección |
|---|---|---|:---:|:---:|
| `RTF-01a` | Empresa en régimen simple | `emisora.perteneceRegimenSimple = true` | `noAplicar` | ambas |
| `RTF-01b` | Contraparte en régimen simple | `contraparte.perteneceRegimenSimple = true` | `noAplicar` | ambas |
| `RTF-02a` | Empresa vendedora exenta | `emisora.esExentoRetefuente = true` | `noAplicar` | ingreso |
| `RTF-02b` | Proveedor exento | `contraparte.esExentoRetefuente = true` | `noAplicar` | gasto |
| `RTF-03a` | Empresa autorretenedora | `emisora.esAutorretenedora = true` → activa AUTO_RETEFUENTE | `noAplicar` | ingreso |
| `RTF-03b` | Proveedor autorretenedor | `contraparte.esAutorretenedora = true` | `noAplicar` | gasto |
| `RTF-04a` | Proveedor fuera régimen IVA | `contraparte.perteneceRegimenIVA = false` | `noAplicar` | gasto |
| `RTF-04b` | Empresa fuera régimen IVA | `emisora.perteneceRegimenIVA = false` | `noAplicar` | ingreso |

### 5.2. Casos compuestos por calidad granC (6)

Combinan calidades de Gran Contribuyente (`esGranContribuyente`) y Autorretenedora (`esAutorretenedora`) en ambas entidades. Cada caso `5a–7a` (perspectiva `gasto`) tiene su contraparte `5b–7b` (perspectiva `ingreso`).

| Código | Lógica resumida | Efecto |
|---|---|:---:|
| `RTF-05a/b` | granC × granC × autorretenedor → aplica |
| `RTF-06a/b` | granC × granC × NO autorretenedor → aplica |
| `RTF-07a/b` | granC × NO granC → aplica |

### 5.3. Default (1)

`RTF-08` — si ninguna condición de exclusión se cumple y la base supera la cuantía mínima, RETEFUENTE aplica.

---

## 6. Condiciones — RIVA (5)

| Código | Caso | Atributo | Efecto | Dirección |
|---|---|---|:---:|:---:|
| `RIVA-01a` | Empresa régimen IVA + cliente agente retenedor | `emisora.perteneceRegimenIVA = true ∧ contraparte.esAgenteRetenedorIVA = true` | `aplicar` | ingreso |
| `RIVA-01b` | Proveedor régimen IVA + empresa agente retenedor | `contraparte.perteneceRegimenIVA = true ∧ emisora.esAgenteRetenedorIVA = true` | `aplicar` | gasto |
| `RIVA-02a` | Cliente no agente retenedor | `contraparte.esAgenteRetenedorIVA = false` | `noAplicar` | ingreso |
| `RIVA-02b` | Empresa no agente retenedor | `emisora.esAgenteRetenedorIVA = false` | `noAplicar` | gasto |

---

## 7. Condiciones — RICA (5)

| Código | Caso | Resumen | Dirección |
|---|---|---|:---:|
| `RICA-01a` | Empresa no régimen simple + proveedor con calidades ICA | `aplicar` (requiere ciudad) | gasto |
| `RICA-01b` | Cliente agente retenedor ICA + empresa con calidades ICA | `aplicar` (requiere ciudad) | ingreso |
| `RICA-02` | Sin calidades — solo valida ciudad | `aplicar-sin-calidades` | ambas |
| `RICA-03a` | Empresa Gran Contribuyente Bogotá | `noAplicar` | ingreso |
| `RICA-03b` | Proveedor Gran Contribuyente Bogotá | `noAplicar` | gasto |

Las condiciones RICA-01a/b usan operador `alguno-de` para evaluar si la entidad tiene **al menos una** de las calidades ICA (`perteneceRegimenIVA`, `esAgenteRetenedorICA`, `esGranContribuyenteICA`, `esAutorretenedorICA`).

---

## 8. Condiciones — IVA (3)

| Código | Caso | Atributo | Efecto | Dirección |
|---|---|---|:---:|:---:|
| `IVA-01a` | Empresa vendedora en régimen IVA | `emisora.perteneceRegimenIVA = true` | `aplicar` | ingreso |
| `IVA-01b` | Proveedor en régimen IVA | `contraparte.perteneceRegimenIVA = true` | `aplicar` | gasto |
| `IVA-02-territorial` | Hecho económico en Puerto Libre | `lugarEjecucion.jurisdiccion.tipoRegimen = "puerto-libre"` | `noAplicar` | ambas |

La condición `IVA-02-territorial` es **independiente de calidades del perfil**: opera sobre la **jurisdicción** resuelta por el motor. El IVA nacional no aplica si el lugar de ejecución es San Andrés (Constitución art. 310, Ley 47/1993 art. 22). Esta es la materialización de la decisión `[D12]` y la invariante `[I15]`.

---

## 9. Condiciones — Autorretenciones (4)

Cada autorretención tiene una condición de activación cuando la empresa vendedora tiene la calidad correspondiente:

| Código | Atributo | Tributo |
|---|---|---|
| `AUTO-RTF-01` | `esAutorretenedora = true` | AUTO_RETEFUENTE |
| `IVA-IMPORTACION-SERVICIOS-01` | `contraparte.tieneDomicilioFiscalEnElPais = false` (gasto) | IVA_IMPORTACION_SERVICIOS |
| `AUTO-RICA-01` | `esAutorretenedorICA = true` (requiere ciudad) | AUTO_RICA |
| `AUTO-RENTA-01` | `esAutorretenedorRenta = true` | AUTO_RENTA |

---

## 10. Notas operativas

### 10.1. Operadores de criterio

- **`igual`**: el atributo del perfil debe coincidir exactamente con el valor.
- **`alguno-de`**: al menos uno de los atributos listados debe ser true (usado en RICA-01a/b).
- **`igual` con valor `*`**: comodín (no usado actualmente).

### 10.2. Atributo `_ciudad` en RICA-03a/b

El criterio `_ciudad` representa la jurisdicción municipal resuelta por el motor (la del lugar de ejecución), no un atributo del perfil. La condición RICA-03 aplica solo cuando la ciudad resuelta es Bogotá (`11001`).

### 10.3. Frontera con `Tratamiento`

Las condiciones complementan los `Tratamientos` declarados en `CatalogoTributario`:

- **`Tratamiento`** dice: "este tributo aplica a esta clasificación" (filtro estructural por clasificación del concepto).
- **`CondicionDeAplicacion`** dice: "este tributo no aplica al perfil X" o "este tributo se sustituye por AUTO_X cuando el perfil cumple Y" (filtro contextual por perfil).

El motor primero aplica `tributosAplicablesA(clasificacion)` y luego evalúa las condiciones del catálogo de aplicación.

### 10.4. `efecto: aplicar-default`

La condición `RTF-08` tiene efecto `aplicar-default`. Es la condición catch-all: si **ninguna otra condición** del tributo se cumple, RETEFUENTE aplica con su tarifa estándar. El motor evalúa las condiciones específicas en orden y solo cae al default si ninguna las excluye o las activa explícitamente.

### 10.5. Activación autónoma del autoliquidado (sin sustitución)

`IVA_IMPORTACION_SERVICIOS` se activa por su **propia** condición (`IVA-IMPORTACION-SERVICIOS-01`: contraparte sin domicilio fiscal, dirección gasto), no por sustitución de RIVA: es un tributo autónomo de naturaleza `provision`, con tarifa propia sobre la base. RIVA no necesita condición de exclusión para el caso — con proveedor del exterior sus propias condiciones no disparan (el proveedor no pertenece al régimen de IVA ni hay IVA facturado que retener). El efecto `reverseCharge` del modelo permanece disponible como mecanismo, pero la precarga de Colombia ya no lo usa.

### 10.6. Cuantía mínima no es condición

La validación "Base mínima superada" NO es una `CondicionDeAplicacion`. Es atributo de la `EntradaDeTarifa` (`cuantiaMinima`). El motor la evalúa después de resolver la tarifa.

---

## 11. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 32 condiciones (15 RETEFUENTE + 5 RIVA + 5 RICA + 3 IVA + 4 autorretenciones). Patrón asimétrico aplicado. |
| 1.2 | 2026-07-31 | **Se retira `RIVA-03` — el autoliquidado se activa solo (issues #117/#118).** La sustitución `reverseCharge` era innecesaria y sostenía el modelado padre-hijo que descartaba al autoliquidado por `[R14]`: como tributo autónomo, `IVA_IMPORTACION_SERVICIOS` se activa por su propia condición (`IVA-IMPORTACION-SERVICIOS-01`, sin cambios) y RIVA no dispara con proveedor del exterior por sus propias condiciones. §10.5 reescrita. Total 32 → 31 condiciones (RIVA 5 → 4). |
| 1.1 | 2026-07-31 | **Disparador de la autoliquidación del IVA corregido (issue #110, resolución con consultoría fiscal):** `RIVA-03` y `AUTO-RIVA-01` (ahora `IVA-IMPORTACION-SERVICIOS-01`) evaluaban `emisora.esAgenteRetenedorIVA = true` — residuo de la definición legada del sistema de facturación, que disparaba el autoliquidado en cualquier compra doméstica de una empresa agente retenedora y chocaba con `RIVA-01b`. Ambas pasan a evaluar `contraparte.tieneDomicilioFiscalEnElPais = false` (art. 437-2 num. 3: proveedor sin residencia ni domicilio en el país), usando el atributo nuevo del catálogo de atributos v1.1. Renombre `AUTO_RIVA` → `IVA_IMPORTACION_SERVICIOS` aplicado en códigos y descripciones. |

---

## 12. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales**:

1. **Casos compuestos `RTF-05/06/07`:** ¿La lógica de granC × granC × autorretenedor es correcta? Convendría tener ejemplos concretos para validar.
2. **`RICA-03` solo Bogotá:** ¿La exclusión de Gran Contribuyente ICA aplica también en Medellín, Cali, Barranquilla? Si sí, ¿es la misma lógica o varía por ciudad?
3. **`alguno-de` en RICA-01:** ¿Es correcto que basta con UNA calidad ICA en la contraparte, o se requieren combinaciones específicas?
4. **`IVA-02-territorial`:** ¿La regla se evalúa solo por `lugarEjecucion` o también requiere considerar `sedeContraparte` (cuando el destinatario está en San Andrés)? Ley 47/93 art. 22 menciona "servicios destinados a"...
5. **Condiciones faltantes:** ¿Falta modelar casos típicos como:
   - Pagos a no contribuyentes (universidades, gobierno, ESALES)
   - Servicios prestados desde el exterior
   - Operaciones con régimen ZESE (Zonas Económicas y Sociales Especiales)?
6. **Otros casos de autoliquidación:** además de la importación de servicios (`IVA_IMPORTACION_SERVICIOS`), ¿existen otros casos normativos donde la empresa deba asumir un impuesto que la contraparte no factura? Si aparecen, siguen el mismo patrón: tributo autónomo de provisión con condición de activación propia.
7. **AUTO_RENTA — tarifas sectoriales en condiciones:** ¿Deberían modelarse condiciones que activen distintas tarifas según el sector de la empresa autorretenedora?
8. **Régimen Simple vs Régimen Ordinario:** ¿Las condiciones `RTF-01a/b` cubren todos los casos de exclusión por régimen simple, o hay matices (servicios calificados que sí retienen, etc.)?
