# Catálogo Tributario — Panamá

**País:** Panamá (`PA`)
**Catálogo del modelo:** `CatalogoTributario` (Sección 3.2)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`pa-catalogo-tributario.json`](pa-catalogo-tributario.json)

---

## 1. Propósito

Catálogo de tributos vigentes en Panamá administrados por la DGI (Dirección General de Ingresos). Cubre el universo transaccional: ITBMS (equivalente IVA), RITBMS (retención), ISC (selectivo) e ISR (retención sobre la renta).

---

## 2. Fuente normativa

- **Código Fiscal de Panamá:** Ley 8 de 1956 y sus modificatorias.
- **ITBMS:** Ley 8 de 2010 — reforma fiscal que introdujo el ITBMS al 7% (subió desde el 5% anterior).
- **ISC:** Código Fiscal Título XX.
- **ISR:** Código Fiscal arts. 694 y siguientes (Régimen de Renta sobre Fuente Panameña).
- **DGI:** Decretos Ejecutivos y Resoluciones reglamentarias.

---

## 3. Cobertura

| Categoría | Cantidad |
|---|:---:|
| Tributos | 4 (ITBMS, RITBMS, ISC, ISR) |
| Clasificaciones | 5 |
| Tratamientos `aplica: true` | 7 |
| Reglas de localización | 4 |
| **Total** | **20 entidades** |

---

## 4. Tributos

| Código | Nombre | Naturaleza | Factor de tarifa | Tributo padre |
|---|---|:---:|---|:---:|
| `ITBMS` | Impuesto sobre la Transferencia de Bienes y Servicios | aditivo | clasificacion | — |
| `RITBMS` | Retención del ITBMS | sustractivo | clasificacion | `ITBMS` |
| `ISC` | Impuesto Selectivo al Consumo | aditivo | clasificacion | — |
| `ISR` | Impuesto sobre la Renta (retenciones) | sustractivo | conceptoPago | — |

### 4.1. Diferencias respecto a otros países

- **ITBMS vs IVA/ITBIS:** Panamá usa una tarifa general muy baja (7%) comparada con CO (19%) o DR (18%). Hay tarifas especiales para alcohol/hospedaje (10%) y tabaco (15%).
- **RITBMS con `factorDeTarifa: clasificacion`** (no `porcentajeDePadre` como en RIVA CO o RITBIS DR). Razón: en PA la retención no es un porcentaje fijo del impuesto generado — la tarifa de retención puede variar.
- **ISR como tributo transaccional:** Las retenciones del ISR (honorarios, dividendos, pagos al exterior) se modelan como tributo. En CO se llama RETEFUENTE; en DR estaba pendiente de modelar.

---

## 5. Clasificaciones

| Código | Nombre | Tributos |
|---|---|---|
| `GRAV_ITBMS_7` | Gravados ITBMS 7% | ITBMS, RITBMS |
| `GRAV_ITBMS_10` | Gravados ITBMS 10% (alcohol, hospedaje) | ITBMS, RITBMS |
| `GRAV_ITBMS_15` | Gravados ITBMS 15% (cigarrillos) | ITBMS, RITBMS |
| `EXENTO_ITBMS` | Exentos de ITBMS | — |
| `ISC_APLICABLE` | Sujeto a ISC | ISC |

ISR no tiene clasificación — su factor es `conceptoPago` (honorarios, dividendos, intereses, alquileres, etc.).

---

## 6. Reglas de localización

| Tributo | Rol que manda |
|---|---|
| ITBMS, RITBMS, ISC, ISR | `sedeEmisora` (sin fallback) |

Todos los tributos son nacionales — la sede determina el país.

---

## 7. Notas operativas

### 7.1. ITBMS — tres tarifas

A diferencia de IVA CO (19%, 5%, 0%) o ITBIS DR (18%, 16%, 0%), ITBMS PA tiene tres tarifas progresivas: 7% general, 10% para alcohol/hospedaje, 15% para tabaco. La 15% incluye sobretasa específica adicional para cigarrillos.

### 7.2. ISR como tributo transaccional

En Panamá las retenciones del ISR se aplican a:
- **Honorarios profesionales:** tarifa según contribuyente (residente/no residente).
- **Dividendos:** 10% (declaratoria de utilidades).
- **Intereses bancarios:** 5%.
- **Alquileres a personas físicas:** 12.5%.
- **Pagos al exterior:** tarifa según concepto y CDI vigente.

Estas tarifas se modelan como `EntradaDeTarifa` en `pa-tarifa-tributaria.json` con factor = código de concepto de pago.

### 7.3. Sin tributos municipales

A diferencia de CO (ICA, RICA), PA no tiene tributos municipales sobre actividades comerciales. Los municipios cobran tributos sobre licencias comerciales y operacionales, pero no entran al ERP transaccional.

### 7.4. Régimen Territorial de Renta

Panamá sigue el **principio territorial de renta**: solo se grava la renta de fuente panameña. Pagos al exterior por servicios prestados desde el extranjero por un beneficiario no residente generalmente NO están sujetos a ISR. Las condiciones precisas se evalúan en `pa-condicion-de-aplicacion.json`.

---

## 8. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 4 tributos + 5 clasificaciones + 7 tratamientos + 4 reglas de localización. |

---

## 9. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales PA**:

1. **¿Faltan tributos en F1?** Casos posibles:
   - **Aviso de Operación:** licencia comercial nacional.
   - **Impuesto al Inmueble:** anual, no transaccional.
   - **Tasas de licencias municipales:** ¿Entran al ERP?
2. **RITBMS con `factorDeTarifa: clasificacion`:** ¿Es correcto, o debe ser `porcentajeDePadre`? Si la retención es siempre un porcentaje del ITBMS facturado, debería ser `porcentajeDePadre`.
3. **ISR — tarifas específicas:** ¿Cuáles conceptos de retención debemos precargar en F1? Los más comunes: honorarios profesionales, dividendos, intereses, alquileres, pagos al exterior.
4. **Tarifa ISC específica:** ¿Cuáles productos tienen ISC y a qué tarifa? Casos conocidos: vehículos, alcohol, tabaco, joyas, telecomunicaciones móviles.
5. **Clasificaciones ITBMS exentas con sub-categorías:** ¿Conviene distinguir `EXENTO_ITBMS_CANASTA_BASICA`, `EXENTO_ITBMS_MEDICAMENTOS`, etc., o mantener una única `EXENTO_ITBMS`?
