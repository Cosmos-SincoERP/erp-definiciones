# Catálogo Tarifas Tributarias — República Dominicana

**País:** República Dominicana (`DO`)
**Catálogo del modelo:** `TarifaTributaria` (Sección 3.3)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`do-tarifa-tributaria.json`](do-tarifa-tributaria.json)

---

## 1. Propósito

Tarifas tributarias vigentes de República Dominicana, organizadas por stream. A diferencia de Colombia (22 streams con tarifas municipales), DR tiene un catálogo **mucho más compacto** — solo 5 streams nacionales.

---

## 2. Fuente normativa

- **ITBIS, RITBIS, ISC:** Código Tributario (Ley 11-92) Títulos III y IV.
- **CDT:** Ley 153-98 (Telecomunicaciones).
- **PROPINA:** Ley 158-01.
- **Reglamentos DGII** vigentes.

---

## 3. Cobertura

| Stream | Tributo | Entradas |
|---|---|:---:|
| `tarifa-DO-ITBIS` | ITBIS | 3 (18%, 16%, 0% exento) |
| `tarifa-DO-RITBIS` | RITBIS | 2 (30% general, 100% personas físicas) |
| `tarifa-DO-ISC` | ISC | 2 (10% telecomunicaciones, 16% seguros) |
| `tarifa-DO-CDT` | CDT | 1 (2%) |
| `tarifa-DO-PROPINA` | PROPINA | 1 (10%) |
| **Total** | | **9 entradas en 5 streams** |

---

## 4. Tarifas

### 4.1. ITBIS (`tarifa-DO-ITBIS`)

| Factor | Tarifa | Notas |
|---|:---:|---|
| `GRAV_ITBIS_18` | 18% | Tarifa general. |
| `GRAV_ITBIS_16` | 16% | Tarifa reducida — yogur, café, azúcar, chocolate, manteca (reforma 2012). |
| `EXENTO_ITBIS` | 0% | Exportaciones, medicamentos, educación, salud, alquiler de vivienda. |

### 4.2. RITBIS (`tarifa-DO-RITBIS`)

| Factor | Tarifa | Notas |
|---|:---:|---|
| `null` (general) | 30% del ITBIS | Retención por agente de retención designado. |
| `RETENCION_PERSONAS_FISICAS` | 100% del ITBIS | Servicios profesionales prestados por personas físicas (NG 02-05). |

### 4.3. ISC (`tarifa-DO-ISC`)

| Factor | Tarifa | Notas |
|---|:---:|---|
| `ISC_TELECOMUNICACIONES` | 10% | Servicios de telecomunicaciones (excluye llamadas internacionales entrantes). |
| `ISC_SEGUROS` | 16% | Primas de seguros (Ley 253-12 reforma fiscal). |

**Nota:** ISC tiene **muchas otras tarifas** específicas por producto (cigarrillos, alcohol, bebidas azucaradas, vehículos, combustibles) que NO se precargan en F1 — su precarga requiere validación detallada con el equipo fiscal.

### 4.4. CDT (`tarifa-DO-CDT`)

| Tarifa | Notas |
|:---:|---|
| 2% | Contribución sobre servicios de telecomunicaciones. |

### 4.5. PROPINA (`tarifa-DO-PROPINA`)

| Tarifa | Notas |
|:---:|---|
| 10% | Propina Legal obligatoria en hoteles, restaurantes y similares. |

---

## 5. Notas operativas

### 5.1. ITBIS y EXENTO_ITBIS

La exención del ITBIS opera de dos formas:
- **Tarifa 0% con derecho a descontar** (exportaciones, productos de la canasta básica). Se modela como `EXENTO_ITBIS` con tarifa 0.
- **Sin sujeción al impuesto** (medicamentos, educación, salud). En este caso ITBIS no aplica como tributo (vía `Tratamiento.aplica: false`), no como tarifa 0.

La precarga actual usa `EXENTO_ITBIS` como tarifa 0 para ambos casos. La distinción se hace en las condiciones de aplicación si el equipo fiscal lo requiere.

### 5.2. ISC — precarga limitada

Solo 2 categorías ISC se precargan (telecomunicaciones y seguros). Otras categorías mayores que requieren modelado:
- **Cigarrillos:** ad valorem + específico por cajetilla.
- **Alcohol:** específico por litro de alcohol absoluto.
- **Bebidas azucaradas:** específico por litro.
- **Vehículos:** ad valorem según cilindrada / valor.
- **Combustibles:** específico por galón.

Estas tarifas tienen estructura compleja y se precargarán con consultores fiscales.

### 5.3. RITBIS — Tarifas diferenciadas

La Norma General 02-05 define varios escenarios de retención con tarifas distintas:
- **30%** — Norma general.
- **75%** — Algunas operaciones específicas (verificar con consultores).
- **100%** — Servicios prestados por personas físicas (incluido en este JSON).

### 5.4. CDT activado solo por sector telecomunicaciones

El motor debe condicionar la aplicación de CDT a operaciones donde el emisor tenga actividad económica CNAE de telecomunicaciones (División 61). Esto se modela en `do-condicion-de-aplicacion.json`.

### 5.5. PROPINA activada solo por sector hotelería/restaurantes

Similar a CDT, PROPINA solo aplica a operaciones de hoteles (División 55) y restaurantes (División 56).

---

## 6. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 5 streams nacionales con 9 entradas (ITBIS 3 + RITBIS 2 + ISC 2 + CDT 1 + PROPINA 1). |

---

## 7. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales DR**:

1. **ISC completo:** ¿Precargamos las tarifas ISC de cigarrillos, alcohol, bebidas azucaradas, vehículos y combustibles, o las dejamos como `personalizado` por sector?
2. **RITBIS — Tarifa 75%:** ¿Cuáles operaciones específicas tienen retención del 75% y cómo se identifican?
3. **CDT — Telecomunicaciones:** ¿Aplica solo a B2C o también B2B?
4. **PROPINA — Casinos y otros:** ¿Aplica solo a hoteles y restaurantes o también a establecimientos de juego, spa, etc.?
5. **Retenciones de ISR:** ¿Las retenciones del ISR sobre honorarios profesionales (15%), alquileres (10%), dividendos (10%), etc., entran a F1 como nuevo tributo o se difieren?
6. **Tarifas EXENTO vs No sujeto:** ¿Conviene distinguir las dos formas de exención en el catálogo, o se mantiene la simplificación con tarifa 0?
