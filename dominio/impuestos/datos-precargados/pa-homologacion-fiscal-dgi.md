# Homologación Fiscal DGI — Panamá

**País:** Panamá (`PA`)
**Autoridad:** DGI (Dirección General de Ingresos)
**Catálogo del modelo:** `HomologacionFiscal` (Sección 3.10 — fase F2)
**Versión:** 0.1-placeholder
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`pa-homologacion-fiscal-dgi.json`](pa-homologacion-fiscal-dgi.json)

> **AVISO DE ESTADO:** Esta versión es una **propuesta placeholder**. Los códigos exactos que DGI Panamá espera en sus formatos fiscales no estaban documentados en las fuentes disponibles. Las 14 equivalencias precargadas son propuestas razonables basadas en el modelo fiscal panameño. **Requiere validación con consultores fiscales PA.**

---

## 1. Propósito

Traduce los valores internos del ERP PA a códigos oficiales DGI para los formatos fiscales.

---

## 2. Cobertura propuesta

**Total: 14 equivalencias.**

| Categoría | Cantidad |
|---|:---:|
| ITBMS (clasificaciones) | 4 |
| RITBMS (tarifa de retención) | 1 |
| ISR (conceptos de retención) | 5 |
| ISC | 2 |
| Operación (local/exterior) | 2 |

---

## 3. Códigos propuestos

### 3.1. ITBMS

| Valor interno | Código DGI propuesto |
|---|---|
| `GRAV_ITBMS_7` | `07` |
| `GRAV_ITBMS_10` | `10` |
| `GRAV_ITBMS_15` | `15` |
| `EXENTO_ITBMS` | `EX` |

### 3.2. RITBMS

| Valor interno | Código DGI propuesto |
|---|---|
| `RITBMS-50` | `R50` |

### 3.3. ISR (Retenciones)

| Valor interno | Código DGI propuesto |
|---|---|
| `ISR-HONORARIOS` | `ISR-HON` |
| `ISR-DIVIDENDOS` | `ISR-DIV` |
| `ISR-INTERESES` | `ISR-INT` |
| `ISR-ALQUILERES` | `ISR-ALQ` |
| `ISR-EXTERIOR` | `ISR-EXT` |

### 3.4. ISC

| Valor interno | Código DGI propuesto |
|---|---|
| `ISC_TELECOMUNICACIONES_MOVIL` | `ISC-TM` |
| `ISC_JOYAS_ARMAS` | `ISC-JA` |

### 3.5. Operación

| Valor interno | Código DGI propuesto |
|---|---|
| `OPERACION_LOCAL` | `L` |
| `OPERACION_EXTERIOR` | `E` |

---

## 4. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1-placeholder | 2026-05-26 | Propuesta inicial con 14 equivalencias. Códigos pendientes de validación con DGI/consultores. |

---

## 5. Revisión pendiente — CRÍTICA

Preguntas **bloqueantes** para consultores fiscales PA:

1. **Códigos oficiales DGI:** ¿Cuáles son los códigos exactos que DGI espera en los XML/archivos de sus formatos? La precarga es estimación.
2. **¿Existe documentación técnica pública de DGI con la tabla de códigos?** URL/referencia.
3. **¿Los códigos varían entre formatos?** Ej: el mismo concepto puede tener un código en la Declaración mensual de ITBMS y otro en la Declaración anual de ISR.
4. **Códigos para CDIs:** ¿Cómo se identifican en los formatos las retenciones reducidas por CDI vigente?
5. **Códigos por régimen especial:** ¿Las operaciones de empresas ZLC/AEEPP/Ciudad del Saber tienen códigos distintos en los reportes?
6. **¿La factura electrónica obligatoria (si existe) cambia la homologación requerida?**
