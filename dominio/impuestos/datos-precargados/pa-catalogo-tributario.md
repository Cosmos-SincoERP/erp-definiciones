# Catálogo Tributario — Panamá

**País:** Panamá (`PA`)
**Catálogo del modelo:** `CatalogoTributario` (Sección 3.2)
**Versión:** 1.2
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
| Tratamientos `aplica: true` | 8 |
| Reglas de localización | 4 |
| **Total** | **21 entidades** |

---

## 4. Tributos

| Código | Nombre | Naturaleza | Factor de tarifa | Tributo padre |
|---|---|:---:|---|:---:|
| `ITBMS` | Impuesto sobre la Transferencia de Bienes y Servicios | aditivo | clasificacion | — |
| `RITBMS` | Retención del ITBMS | sustractivo | porcentajeDePadre | `ITBMS` |
| `ISC` | Impuesto Selectivo al Consumo | aditivo | clasificacion | — |
| `ISR` | Impuesto sobre la Renta (retenciones) | sustractivo | conceptoPago | — |

### 4.1. Diferencias respecto a otros países

- **ITBMS vs IVA/ITBIS:** Panamá usa una tarifa general muy baja (7%) comparada con CO (19%) o DR (18%). Hay tarifas especiales para alcohol/hospedaje (10%) y tabaco (15%).
- **RITBMS con `factorDeTarifa: porcentajeDePadre`** — mismo patrón de RIVA (CO) y RITBIS (RD): la retención es siempre un **porcentaje del ITBMS causado** (Decreto Ejecutivo 470 de 2015 — 50% norma general), por lo que hereda su ciclo de vida: si el ITBMS se descarta (por ejemplo, por cuantía mínima), la retención se descarta con él. Los porcentajes distintos que existen (100% en servicios profesionales al Estado y en pagos a no residentes) varían por **calidad del agente o del proveedor, no por clasificación** — se modelarán como condiciones/variantes (pregunta 2 de la revisión pendiente de tarifas). Nota: el caso de pagos a no residentes es el análogo panameño del `IVA_IMPORTACION_SERVICIOS` colombiano.
- **ISR como tributo transaccional:** Las retenciones del ISR (honorarios, dividendos, pagos al exterior) se modelan como tributo. En CO se llama RETEFUENTE; en DR estaba pendiente de modelar.

---

## 5. Clasificaciones

| Código | Nombre | Tributos |
|---|---|---|
| `GRAV_ITBMS_7` | Gravados ITBMS 7% | ITBMS, RITBMS |
| `GRAV_ITBMS_10` | Gravados ITBMS 10% (alcohol, hospedaje) | ITBMS, RITBMS |
| `GRAV_ITBMS_15` | Gravados ITBMS 15% (cigarrillos) | ITBMS, RITBMS |
| `EXENTO_ITBMS` | Exentos de ITBMS | ISR |
| `ISC_APLICABLE` | Sujeto a ISC | ISC |

La tarifa del ISR no depende de la clasificación — su factor es `conceptoPago` (honorarios, dividendos, intereses, alquileres, etc.). Sí participa de la matriz de tratamientos: `ISR` × `EXENTO_ITBMS` declara que la retención de renta puede aplicar a un concepto exento de ITBMS (son tributos independientes: la exención del ITBMS no exime la retención de renta). Coherente con la configuración estándar de la implementación; confirmación con consultores pendiente (pregunta 5).

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
| 1.1 | 2026-07-31 | Nuevo tratamiento `ISR` × `EXENTO_ITBMS` (`aplica: true`) — issue #111: la matriz va atrás de la configuración estándar de la implementación; la retención de renta puede aplicar a conceptos exentos de ITBMS (tributos independientes). Confirmación con consultores pendiente (pregunta 6). Tratamientos 7 → 8, total 20 → 21 entidades. |
| 1.2 | 2026-07-31 | **`RITBMS.factorDeTarifa`: `clasificacion` → `porcentajeDePadre`** (issue #109, decisión por investigación normativa — el sistema legado aún no opera en firme estos tributos). El precargado era internamente inconsistente: declaraba `tributoPadre: ITBMS` con un factor que no lo usa, y **su propio stream de tarifas ya estaba en `porcentajeDePadre`** ("50% del ITBMS facturado"). El Decreto Ejecutivo 470 de 2015 confirma: la retención es siempre porcentaje del ITBMS causado (50% general; 100% en servicios profesionales al Estado y pagos a no residentes — variantes por calidad, no por clasificación, pendientes de modelar). Con el factor correcto, la retención **hereda el ciclo de vida del padre**: ITBMS descartado → RITBMS descartado (cierra el caso de retención sin impuesto causado, patrón RITBIS de RD). Nota §4.1 reescrita (justificaba el factor errado); pregunta 2 cerrada, restantes renumeradas 3-6 → 2-5. `ISR` se ratifica **sin** padre (retención de renta autónoma, análogo de RETEFUENTE CO). ⚠️ La implementación (`ConfiguracionEstandarPa`) no declara el padre — corrección de comportamiento con impacto en cifras. |

---

## 9. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales PA**:

1. **¿Faltan tributos en F1?** Casos posibles:
   - **Aviso de Operación:** licencia comercial nacional.
   - **Impuesto al Inmueble:** anual, no transaccional.
   - **Tasas de licencias municipales:** ¿Entran al ERP?
2. **ISR — tarifas específicas:** ¿Cuáles conceptos de retención debemos precargar en F1? Los más comunes: honorarios profesionales, dividendos, intereses, alquileres, pagos al exterior.
3. **Tarifa ISC específica:** ¿Cuáles productos tienen ISC y a qué tarifa? Casos conocidos: vehículos, alcohol, tabaco, joyas, telecomunicaciones móviles.
4. **Clasificaciones ITBMS exentas con sub-categorías:** ¿Conviene distinguir `EXENTO_ITBMS_CANASTA_BASICA`, `EXENTO_ITBMS_MEDICAMENTOS`, etc., o mantener una única `EXENTO_ITBMS`?
5. **Tratamiento `ISR` × `EXENTO_ITBMS`:** ¿Se confirma que la retención de renta aplica a conceptos exentos de ITBMS? Es coherente (son tributos independientes) y así corre en la configuración estándar, pero conviene la ratificación normativa.
