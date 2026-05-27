# Catálogo Tarifas Tributarias — Panamá

**País:** Panamá (`PA`)
**Catálogo del modelo:** `TarifaTributaria` (Sección 3.3)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`pa-tarifa-tributaria.json`](pa-tarifa-tributaria.json)

---

## 1. Propósito

Tarifas tributarias vigentes de Panamá. 4 streams nacionales con 13 entradas.

---

## 2. Cobertura

| Stream | Tributo | Entradas |
|---|---|:---:|
| `tarifa-PA-ITBMS` | ITBMS | 4 (7%, 10%, 15%, 0% exento) |
| `tarifa-PA-RITBMS` | RITBMS | 2 (50% sobre ITBMS 7%, 50% sobre ITBMS 10%) |
| `tarifa-PA-ISC` | ISC | 2 (telecomunicaciones móvil 5%, joyas/armas 5%) |
| `tarifa-PA-ISR` | ISR | 5 (honorarios 15%, dividendos 10%, intereses 5%, alquileres 12.5%, exterior 12.5%) |
| **Total** | | **13 entradas** |

---

## 3. Notas operativas

### 3.1. ITBMS — tres tarifas progresivas

| Tarifa | Aplicación |
|:---:|---|
| 7% | Tarifa general. |
| 10% | Bebidas alcohólicas, servicios de hospedaje (hoteles). |
| 15% | Cigarrillos y productos del tabaco. |

### 3.2. RITBMS — 50% sobre ITBMS

La retención del ITBMS por agentes de retención designados es **el 50% del ITBMS facturado**. El proveedor recupera el 50% restante en su declaración mensual. La tarifa es la misma para todas las clasificaciones ITBMS.

### 3.3. ISC — Tarifas conocidas

Las tarifas precargadas son las más comunes. ISC tiene **muchas otras tarifas** específicas por producto (vehículos importados según cilindrada, alcohol según graduación, combustibles, etc.) que requieren modelado adicional con consultores.

### 3.4. ISR — Retenciones por concepto

Las tarifas precargadas son retenciones a fuente. Las tarifas reales para liquidación anual del ISR difieren (escala progresiva para personas naturales, 25% para personas jurídicas con régimen ordinario).

### 3.5. ISR — Régimen territorial

Panamá usa el **principio territorial de renta**. Los pagos al exterior por servicios prestados **desde el extranjero** por beneficiarios no residentes **generalmente NO están sujetos a ISR** en Panamá. La retención precargada para `PAGOS_EXTERIOR_SERVICIOS` aplica solo cuando el servicio se considera de fuente panameña — la determinación caso por caso requiere análisis del lugar de prestación.

---

## 4. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 4 streams nacionales con 13 entradas. |

---

## 5. Revisión pendiente

1. **ISC completo:** Vehículos, alcohol, combustibles requieren precarga adicional.
2. **RITBMS 50%:** ¿Hay casos con tarifas distintas (75%, 100%)?
3. **ISR honorarios — escala:** ¿La tarifa del 15% es para personas físicas residentes? ¿Cómo se modela la escala progresiva?
4. **ISR exterior — tarifas por CDI:** ¿Cómo manejamos las tarifas reducidas por Convenios para Evitar la Doble Imposición (CDIs)?
5. **Tarifas EXENTO_ITBMS vs No sujeto:** Igual que DR, ¿conviene distinguir las dos formas de exención?
