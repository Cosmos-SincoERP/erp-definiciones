# Homologación Fiscal DGII — República Dominicana

**País:** República Dominicana (`DO`)
**Autoridad:** DGII (Dirección General de Impuestos Internos)
**Catálogo del modelo:** `HomologacionFiscal` (Sección 3.10 — fase F2)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`do-homologacion-fiscal-dgii.json`](do-homologacion-fiscal-dgii.json)

---

## 1. Propósito

Traduce los valores internos del ERP DR a códigos oficiales DGII para los formatos 606-609 y las declaraciones IT-1/IR-2.

---

## 2. Cobertura

**Total: 18 equivalencias.**

| Categoría | Cantidad |
|---|:---:|
| ITBIS (clasificaciones + operaciones) | 6 |
| RITBIS (tarifas de retención) | 3 |
| ISR (conceptos de retención) | 6 |
| ISC | 1 |
| Operación (local/exterior) | 2 |

---

## 3. Códigos ITBIS

| Valor interno | Código DGII | Significado |
|---|---|---|
| `GRAV_ITBIS_18` | `G` | Gravado ITBIS 18% (tarifa general) |
| `GRAV_ITBIS_16` | `G2` | Gravado ITBIS 16% (tarifa reducida) |
| `EXENTO_ITBIS` | `E` | Exento de ITBIS |
| `ITBIS_FACTURADO` | `ITBIS-F` | ITBIS facturado en ventas |
| `ITBIS_DEDUCIDO` | `ITBIS-D` | ITBIS deducido en compras |
| `ITBIS_EXPORTACION` | `ITBIS-X` | ITBIS en operaciones de exportación |

---

## 4. Códigos RITBIS

| Valor interno | Código DGII | Significado |
|---|---|---|
| `RITBIS-30` | `R-30` | Retención ITBIS 30% (norma general) |
| `RITBIS-100-PF` | `R-100` | Retención ITBIS 100% — persona física profesional |
| `RITBIS-75` | `R-75` | Retención ITBIS 75% — casos específicos |

---

## 5. Códigos ISR (retenciones)

| Valor interno | Código DGII | Concepto |
|---|---|---|
| `ISR-HONORARIOS` | `ISR-01` | Honorarios profesionales |
| `ISR-ALQUILERES` | `ISR-02` | Alquileres |
| `ISR-DIVIDENDOS` | `ISR-03` | Dividendos |
| `ISR-INTERESES` | `ISR-04` | Intereses |
| `ISR-EXTERIOR` | `ISR-EXT` | Pagos al exterior |
| `ISR-PREMIOS` | `ISR-PR` | Premios y ganancias fortuitas |

---

## 6. Códigos de operación (F-606, F-607)

| Valor interno | Código DGII | Significado |
|---|---|---|
| `OPERACION_LOCAL` | `01` | Operación local (dentro de DR) |
| `OPERACION_EXTERIOR` | `02` | Operación con el exterior |

---

## 7. Notas operativas

### 7.1. Códigos alfanuméricos

A diferencia de DIAN CO (donde los códigos son numéricos), DGII DR usa códigos **alfanuméricos cortos** (`G`, `G2`, `E`, `R-30`). Esto refleja la convención de los archivos XML de los formatos 606-609.

### 7.2. Códigos NO totalmente verificados

Los códigos `R-30`, `R-100`, `R-75`, `ISR-01`, etc. son **denominaciones internas del catálogo**. Los códigos exactos publicados por DGII en sus anexos técnicos pueden diferir — requiere validación con consultores fiscales DR.

### 7.3. ITBIS vs ITBIS-F vs ITBIS-D

DGII distingue en los formatos:
- **`G`** — clasificación del bien/servicio (en F-606/F-607).
- **`ITBIS-F`** — valor del ITBIS facturado (en F-607).
- **`ITBIS-D`** — valor del ITBIS deducible (en F-606).

Por eso aparecen tres equivalencias relacionadas con ITBIS — cada una con propósito distinto en los formatos.

---

## 8. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 18 equivalencias (6 ITBIS + 3 RITBIS + 6 ISR + 1 ISC + 2 operación). |

---

## 9. Revisión pendiente

1. **Códigos exactos DGII:** Los códigos precargados son denominaciones internas. **Es prioritario validar con consultores cuáles son los códigos oficiales** que DGII espera en los XML de los formatos 606-609.
2. **¿Faltan códigos ISR?** Casos posibles: regalías al exterior, comisiones, servicios técnicos del exterior.
3. **¿Hay códigos especiales para zonas francas?** El reporte de operaciones desde ZFs puede requerir códigos distintos.
4. **¿Códigos para e-CF vs NCF físico?** ¿Se distinguen en los XML?
5. **Códigos para retenciones de IR-2 (declaración anual ISR):** ¿Existen códigos adicionales no incluidos para reportes anuales?
