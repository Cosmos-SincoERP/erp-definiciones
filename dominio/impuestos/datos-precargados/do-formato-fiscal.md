# Catálogo Formatos Fiscales — República Dominicana

**País:** República Dominicana (`DO`)
**Catálogo del modelo:** `FormatoFiscal` (Sección 3.11 — fase F2)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`do-formato-fiscal.json`](do-formato-fiscal.json)

---

## 1. Propósito

Formatos fiscales que el ERP debe generar mensualmente para reportar a la DGII. DR usa un esquema más sencillo que CO (4 formatos vs 10) pero con periodicidad **mensual** en lugar de anual.

---

## 2. Cobertura

**Total: 4 formatos DGII.**

| Código | Nombre | Periodicidad | Obligatorio si 100% e-CF |
|---|---|:---:|:---:|
| `F-606` | Compras y gastos de proveedores | Mensual | Sí |
| `F-607` | Ventas e ingresos operacionales | Mensual | **No** (e-CF reemplaza) |
| `F-608` | Comprobantes fiscales cancelados (NCF anulados) | Mensual | **No** (e-CF reemplaza) |
| `F-609` | Pagos al exterior sin NCF | Mensual | Sí |

---

## 3. Notas operativas

### 3.1. Migración NCF → e-CF

DR está migrando del sistema de Números de Comprobante Fiscal (NCF) físicos al sistema de Comprobante Fiscal Electrónico (e-CF). Los e-CF llegan a DGII en tiempo real al momento de la emisión, por lo que algunos reportes mensuales pierden obligatoriedad cuando el contribuyente emite 100% e-CF:

- **F-606 (compras):** sigue siendo obligatorio porque DGII no recibe automáticamente las compras del contribuyente.
- **F-607 (ventas):** redundante si todas las ventas son e-CF.
- **F-608 (anulados):** redundante si todas las anulaciones son e-CF.
- **F-609 (pagos exterior):** siempre obligatorio (no se factura con NCF al exterior).

### 3.2. Plazo de entrega

Día 20 del mes siguiente al periodo reportado.

### 3.3. Formato XML

Todos los formatos se entregan en XML siguiendo el esquema definido por DGII en sus normas generales. No hay versión Excel "prevalidador" como en Colombia.

---

## 4. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 4 formatos DGII (606, 607, 608, 609). |

---

## 5. Revisión pendiente

1. **¿Faltan formatos relevantes?** Casos posibles:
   - **IT-1:** Declaración mensual del ITBIS.
   - **IR-2:** Declaración anual del ISR.
   - **IR-13:** Declaración Jurada de Operaciones.
   - **Reporte de Retenciones de ISR:** ¿Es un reporte mensual aparte?
2. **¿Existe un certificado tributario equivalente al Formulario 220 de Colombia?**
3. **e-CF — Estructura del archivo:** ¿Conviene modelar el e-CF como un `FormatoFiscal` aparte (sería formato transaccional, no de cierre)?
