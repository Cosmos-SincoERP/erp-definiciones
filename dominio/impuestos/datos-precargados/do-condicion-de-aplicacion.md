# Catálogo Condiciones de Aplicación — República Dominicana

**País:** República Dominicana (`DO`)
**Catálogo del modelo:** `CondicionDeAplicacion` (Sección 3.4)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`do-condicion-de-aplicacion.json`](do-condicion-de-aplicacion.json)

---

## 1. Propósito

Reglas declarativas que ajustan el tratamiento tributario según el perfil del sujeto, la actividad económica del emisor o la inscripción en regímenes especiales. Es **mucho más compacto** que CO (32 vs 9) porque la operación fiscal dominicana es más simple.

---

## 2. Fuente normativa

- **NCF/e-CF (autorización):** Normas Generales DGII 06-2018 y 06-2021.
- **RITBIS:** Norma General 02-05.
- **CDT — Telecomunicaciones:** Ley 153-98.
- **PROPINA:** Ley 158-01.
- **Zonas Francas — Exoneración:** Ley 8-90.

---

## 3. Cobertura

**Total: 9 condiciones.**

| Tributo | Condiciones |
|---|:---:|
| ITBIS | 3 (2 régimen NCF + 1 exoneración zona franca) |
| RITBIS | 3 (1 general agente retención + 1 personas físicas + 1 exclusión) |
| CDT | 1 (sector telecomunicaciones) |
| PROPINA | 1 (sector hoteles/restaurantes) |
| ISC | 1 (default por clasificación) |

---

## 4. Condiciones

### 4.1. ITBIS (3)

| Código | Caso | Atributo | Efecto | Dirección |
|---|---|---|:---:|:---:|
| `ITBIS-01a` | Empresa vendedora autorizada NCF/e-CF | `emisora.ncf = true` | `aplicar` | ingreso |
| `ITBIS-01b` | Proveedor autorizado NCF/e-CF | `contraparte.ncf = true` | `aplicar` | gasto |
| `ITBIS-02-zona-franca` | Empresa inscrita en zona franca | `inscripcionParqueZonaFranca` no nulo (cualquiera) | `noAplicar` | ambas |

### 4.2. RITBIS (3)

| Código | Caso | Efecto | Dirección |
|---|---|:---:|:---:|
| `RITBIS-01` | Empresa adquiriente agente retención + proveedor NCF → retiene 30% | `aplicar` | gasto |
| `RITBIS-02-personas-fisicas` | Persona física proveedora de servicios profesionales → retiene 100% | `aplicar` (tarifa 100%) | gasto |
| `RITBIS-03` | Empresa adquiriente NO agente retención → no retiene | `noAplicar` | gasto |

### 4.3. CDT (1)

| Código | Caso | Atributo | Efecto |
|---|---|---|:---:|
| `CDT-01` | Empresa con CNAE División 61 (telecomunicaciones) | `actividadEconomica.division = "61"` | `aplicar` |

### 4.4. PROPINA (1)

| Código | Caso | Atributo | Efecto |
|---|---|---|:---:|
| `PROPINA-01` | Hoteles (División 55) o restaurantes (División 56) | `actividadEconomica.division ∈ {"55", "56"}` | `aplicar` |

### 4.5. ISC (1)

| Código | Caso | Efecto |
|---|---|:---:|
| `ISC-01-clasificacion` | Concepto con clasificación `ISC_APLICABLE` y tarifa específica | `aplicar-default` |

---

## 5. Notas operativas

### 5.1. Condicionamiento por actividad económica

A diferencia de CO (donde las condiciones se basan principalmente en atributos del perfil tributario como `esGranContribuyente`, `esExentoRetefuente`), DR usa fuertemente la **actividad económica CNAE** como criterio:
- CDT solo aplica a División 61 (telecomunicaciones).
- PROPINA solo aplica a Divisiones 55 y 56 (hoteles, restaurantes).

Esto requiere que el motor resuelva la actividad económica del emisor (`emisora.actividadEconomica`) antes de evaluar estas condiciones. El campo `actividadEconomica.division` se proyecta desde el código CIIU/CNAE de 4 dígitos tomando los 2 primeros.

### 5.2. ITBIS-02-zona-franca

La exoneración del ITBIS para empresas inscritas en zonas francas es la condición más compleja del catálogo DR. **El alcance exacto requiere verificación con consultores:**
- ¿Aplica solo a ventas de exportación, o también a ventas locales bajo ciertas condiciones?
- ¿Aplica a las compras de insumos y maquinaria de la ZF, exonerándolas?
- ¿Cómo se distingue el régimen ZF total (todas las operaciones exoneradas) del régimen ZF parcial (solo exportaciones)?

La precarga inicial usa una regla simple (`inscripcionParqueZonaFranca` no nulo → noAplicar). En producción, esta condición probablemente se refine con sub-condiciones.

### 5.3. RITBIS sin condiciones por régimen simple

DR no tiene Régimen Simple equivalente al colombiano. La condición principal de exclusión es no ser agente de retención designado (`RITBIS-03`).

### 5.4. PROPINA como tributo opcional del cliente

La PROPINA es un **tributo facturado** que el establecimiento agrega al consumo, pero el cliente puede solicitar su exoneración en algunos casos (consumo para llevar, propina ya incluida en menú). El modelado precargado activa PROPINA por sector emisor; la exoneración voluntaria se maneja vía condiciones `personalizado` si el cliente así lo requiere.

---

## 6. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 9 condiciones (3 ITBIS + 3 RITBIS + 1 CDT + 1 PROPINA + 1 ISC). |

---

## 7. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales DR**:

1. **ITBIS-02-zona-franca — alcance exacto:** ¿La regla `inscripcionParqueZonaFranca` no nulo → noAplicar es correcta para todos los escenarios, o necesita sub-condiciones por tipo de operación (venta local/exportación/insumo)?
2. **PROPINA — sectores aplicables:** ¿Las Divisiones CNAE 55 y 56 cubren todos los establecimientos obligados? ¿Falta incluir alguno (ej: salones de eventos, food trucks, dark kitchens)?
3. **CDT — telecomunicaciones B2B vs B2C:** ¿Aplica en todas las ventas o solo a clientes finales? ¿Las llamadas internacionales entrantes están exoneradas?
4. **RITBIS — Otras tarifas:** ¿Existen escenarios de retención del 75% o de otras tarifas que requieran condición específica?
5. **¿Faltan condiciones específicas para:**
   - Contribuyentes acogidos al **Régimen Simplificado de Tributación (RST)** — ¿están exonerados de retener?
   - Operaciones de **exportación** — ¿requieren condición específica para ITBIS exento?
   - Operaciones **inter-zonas francas** — ¿qué tratamiento llevan?
6. **`actividadEconomica.division` como criterio:** ¿La proyección de los primeros 2 dígitos del código CNAE para obtener la división es correcta, o debemos modelar la división explícitamente en `ActividadEconomicaRegistrada`?
