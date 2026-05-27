# Homologación Fiscal DIAN — Colombia

**País:** Colombia (`CO`)
**Autoridad:** DIAN (Dirección de Impuestos y Aduanas Nacionales)
**Catálogo del modelo:** `HomologacionFiscal` (Sección 3.10 — fase F2)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`co-homologacion-fiscal-dian.json`](co-homologacion-fiscal-dian.json)

---

## 1. Propósito

Traduce los **valores internos** del ERP (códigos de conceptos, clasificaciones tributarias, tipos de operación) a los **códigos oficiales DIAN** requeridos en los reportes de información exógena y los certificados tributarios.

Cada `Equivalencia` es un mapeo `(valorInterno, tributo) → codigoAutoridad` con su denominación oficial. El motor de cumplimiento (`FormatoFiscal` + `EntregableFiscal`) consulta esta tabla al generar los formatos XML para sustituir los códigos internos por los aceptados por DIAN.

---

## 2. Fuente normativa

- **Resoluciones DIAN de información exógena** (anuales) — Anexos técnicos:
  - Resolución 162 de 2023 (año gravable 2024).
  - Resolución 000233 de 2026 (año gravable 2025 — vigente).
- **Especificaciones de formatos:** F-1001, F-1003, F-1005, F-1006, F-1007, F-1647, F-2276, F-2856.

---

## 3. Cobertura

**Total: 35 equivalencias.**

| Tributo | Equivalencias |
|---|:---:|
| RETEFUENTE | 18 (conceptos de pago — bloque 5XXX) |
| IVA | 7 (clasificaciones IVA + RIVA + bienes/servicios) |
| INC | 1 |
| ICA / RICA / AUTO_RICA / SOBRETASA_BOMBERIL | 4 (códigos ICA-0X para reportes ICA municipales) |
| RETEFUENTE (rentas laborales para F-2276) | 5 (bloque 53XX) |

---

## 4. Códigos de conceptos RETEFUENTE (bloque 5XXX)

Los códigos `5XXX` son los conceptos oficiales DIAN para el formato F-1001 (Pagos y retenciones). Cubren las 18 categorías principales de RETEFUENTE precargadas en `co-tarifa-tributaria.json`. Ejemplos:

| Código DIAN | Valor interno | Concepto |
|---|---|---|
| `5001` | `COMPRAS_GENERALES_DECLARANTES` | Compras generales — declarantes |
| `5002` | `COMPRAS_GENERALES_NO_DECLARANTES` | Compras generales — no declarantes |
| `5010` | `SERVICIOS_GENERALES_DECLARANTES` | Servicios generales — declarantes |
| `5020` | `HONORARIOS_DECLARANTES` | Honorarios y comisiones — declarantes |
| `5031` | `ARRENDAMIENTO_INMUEBLES` | Arrendamiento de bienes inmuebles |
| `5050` | `EXTERIOR_SERVICIOS_TECNICOS` | Pagos al exterior — servicios técnicos |

---

## 5. Códigos de IVA

| Código DIAN | Valor interno | Significado |
|---|---|---|
| `01` | `GRAV_19` | IVA al 19% |
| `01-B` | `GRAV_19_BIENES` | IVA 19% sobre bienes |
| `01-S` | `GRAV_19_SERVICIOS` | IVA 19% sobre servicios |
| `02` | `GRAV_5` | IVA al 5% |
| `03` | `EXENTO` | IVA exento |
| `04` | `EXCLUIDO` | IVA excluido |
| `08` | `INC_8` | INC 8% |
| `10` | `IVA-RETENCION-15` (RIVA) | Retención IVA 15% |

---

## 6. Códigos ICA municipales

| Código DIAN | Tributo | Significado |
|---|---|---|
| `ICA-01` | ICA | ICA causado en el periodo |
| `ICA-02` | RICA | ICA retenido — Reteica |
| `ICA-03` | AUTO_RICA | ICA autorretenido |
| `ICA-04` | SOBRETASA_BOMBERIL | Sobretasa bomberil |

Estos códigos son **internos del ERP** — los reportes ICA municipales no usan códigos DIAN sino códigos del estatuto tributario de cada municipio. La homologación oficial por municipio se agrega al introducir cada ciudad operativa.

---

## 7. Códigos rentas laborales (F-2276, bloque 53XX)

| Código DIAN | Valor interno | Concepto |
|---|---|---|
| `5300` | `SALARIOS` | Pagos laborales y prestaciones sociales |
| `5301` | `PENSIONES` | Pensiones |
| `5302` | `APORTES_SS` | Aportes al sistema de seguridad social del trabajador |
| `5303` | `AFC` | Aportes voluntarios pensiones y AFC |
| `5304` | `MEDICINA_PREPAGADA` | Medicina prepagada deducible |

---

## 8. Notas operativas

### 8.1. Códigos como string

Aunque los códigos DIAN son numéricos (`5001`, `5002`), se almacenan como **string** en el JSON porque:
- Algunos códigos tienen sufijos (`01-B`, `01-S`).
- Pueden tener ceros a la izquierda significativos (`01` vs `1`).
- Los reportes XML los entregan como string.

### 8.2. `valorInterno` debe coincidir con códigos del ERP

El `valorInterno` debe coincidir exactamente con los códigos usados en otros catálogos:
- Códigos de `EntradaDeTarifa.factor` en `co-tarifa-tributaria.json`.
- Códigos de `ClasificacionTributaria` en `co-catalogo-tributario.json`.
- Códigos de `RegistroTributario.LineaDeDesglose`.

Esta consistencia es la razón por la que el atributo se llama `valorInterno` — el motor lo busca con clave exacta.

### 8.3. Sin equivalencias por jurisdicción

Las equivalencias DIAN son nacionales — no varían por municipio. Para reportes ICA municipales, se requiere una `HomologacionFiscal` por municipio (no incluida en F1; se genera `personalizado` al activar cada municipio).

### 8.4. Vigencia desde 2017

La mayoría de equivalencias tienen `fechaDesde: 2017-01-01` porque corresponden a la reforma tributaria estructural (Ley 1819/2016). Los códigos previos (anteriores a 2017) se omiten porque no son aplicables a transacciones modernas.

---

## 9. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 35 equivalencias (18 RETEFUENTE + 7 IVA + 1 INC + 4 ICA + 5 rentas laborales). |

---

## 10. Revisión pendiente

1. **Códigos 5XXX completos:** Los 18 conceptos RETEFUENTE precargados son los más comunes. ¿Existen otros conceptos DIAN del bloque 5XXX que debamos incluir? (DIAN tiene ~50 conceptos en total).
2. **Códigos por formato:** ¿Necesitamos campo adicional `formatoAplica` para identificar en qué formato usa cada código? (Ej: `5001` se usa en F-1001 y F-1003, pero no en F-2276).
3. **Códigos por año gravable:** ¿Algunos códigos han cambiado entre años gravables y requieren modelado de vigencia más fino?
4. **Códigos para activos digitales (F-2856):** ¿Cuáles son los códigos específicos para criptomonedas, NFTs, otros activos digitales?
5. **Códigos para sectores especializados:** ¿Existen códigos específicos para sectores como salud, educación, ESALES?
