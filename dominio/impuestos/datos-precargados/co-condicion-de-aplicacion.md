# Catálogo Condiciones de Aplicación — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `CondicionDeAplicacion` (Sección 3.4 de `modelo-dominio.md`)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
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
- **AUTO_RIVA — reverse charge:** Estatuto Tributario art. 437-2 numeral 3 (importación de servicios).
- **Régimen Puerto Libre — IVA territorial:** Constitución art. 310 + Ley 47/1993 art. 22.

---

## 3. Cobertura del catálogo

**Total: 32 condiciones.** Distribución por tributo:

| Tributo | Condiciones | Notas |
|---|:---:|---|
| RETEFUENTE | 15 | 14 condiciones específicas + 1 default. Patrón asimétrico: cada caso real se desdobla en perspectivas `gasto` (sub-código `a`) e `ingreso` (sub-código `b`). |
| RIVA | 5 | Casos de agente retenedor IVA + reverse charge para AUTO_RIVA. |
| RICA | 5 | Casos por calidades ICA + gran contribuyente Bogotá. |
| IVA | 3 | 2 casos de pertenencia régimen IVA + 1 caso territorial Puerto Libre. |
| AUTO_RETEFUENTE | 1 | Activación por autorretenedora. |
| AUTO_RIVA | 1 | Activación por reverse charge. |
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
| `RIVA-03` | Reverse charge en gasto | `emisora.esAgenteRetenedorIVA = true` → activa AUTO_RIVA | `reverseCharge` | gasto |

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
| `AUTO-RIVA-01` | `esAgenteRetenedorIVA = true` (gasto, reverse charge) | AUTO_RIVA |
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

### 10.5. `efecto: reverseCharge`

La condición `RIVA-03` tiene efecto `reverseCharge`: RIVA no aplica, pero AUTO_RIVA sí. Esto se modela con el campo `tributoSubstituto: "AUTO_RIVA"` que indica al motor que active el tributo sustituto en lugar del original.

### 10.6. Cuantía mínima no es condición

La validación "Base mínima superada" NO es una `CondicionDeAplicacion`. Es atributo de la `EntradaDeTarifa` (`cuantiaMinima`). El motor la evalúa después de resolver la tarifa.

---

## 11. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 32 condiciones (15 RETEFUENTE + 5 RIVA + 5 RICA + 3 IVA + 4 autorretenciones). Patrón asimétrico aplicado. |

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
6. **`RIVA-03` reverse charge solo importación de servicios:** ¿Hay otros casos de reverse charge (ej: ventas a no responsables especiales)?
7. **AUTO_RENTA — tarifas sectoriales en condiciones:** ¿Deberían modelarse condiciones que activen distintas tarifas según el sector de la empresa autorretenedora?
8. **Régimen Simple vs Régimen Ordinario:** ¿Las condiciones `RTF-01a/b` cubren todos los casos de exclusión por régimen simple, o hay matices (servicios calificados que sí retienen, etc.)?
