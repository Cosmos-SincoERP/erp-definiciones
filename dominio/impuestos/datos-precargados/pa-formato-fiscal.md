# Catálogo Formatos Fiscales — Panamá

**País:** Panamá (`PA`)
**Catálogo del modelo:** `FormatoFiscal` (Sección 3.11 — fase F2)
**Versión:** 0.1-placeholder
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`pa-formato-fiscal.json`](pa-formato-fiscal.json)

> **AVISO DE ESTADO:** Esta versión es una **propuesta placeholder**. Los formatos fiscales exigidos por la DGI Panamá no estaban documentados en las fuentes disponibles al momento de la elaboración del modelo (ver anexo PA original — sección 6 marcada como "pendiente"). Los 5 formatos precargados son estimaciones basadas en obligaciones declarativas conocidas del régimen fiscal panameño. **Requiere validación detallada con consultores fiscales PA antes de implementar.**

---

## 1. Propósito

Documentar las obligaciones de reporte fiscal del ERP en Panamá. Cubre tres tipos de entregables:

- **Declaraciones mensuales:** ITBMS, retenciones del ISR.
- **Declaraciones anuales:** ISR persona jurídica, ISR persona natural.
- **Certificados:** Retenciones anuales a terceros.

---

## 2. Cobertura propuesta

**Total: 5 formatos (todos en estado `propuesta`).**

| Código | Nombre | Periodicidad |
|---|---|---|
| `DECL-ITBMS` | Declaración Mensual del ITBMS | Mensual (día 15) |
| `DECL-ISR-PJ` | Declaración Anual del ISR — Persona Jurídica | Anual (31-03 año siguiente) |
| `DECL-ISR-PN` | Declaración Anual del ISR — Persona Natural | Anual (15-03 año siguiente) |
| `DECL-RETENCIONES` | Declaración de Retenciones del ISR | Mensual (día 15) |
| `CERT-RET-ANUAL` | Certificado Anual de Retenciones | Anual (31-03 año siguiente) |

---

## 3. Estado de la precarga

Todos los formatos llevan `estado: propuesta` para señalar que no son la lista definitiva validada. La versión 1.0 (no-placeholder) se publicará cuando los consultores confirmen:

1. **Cuáles son los formatos oficiales de DGI Panamá.**
2. **Códigos oficiales de cada formato.**
3. **Estructura exacta** (secciones, campos, validaciones).
4. **Formato de salida** (XML, Excel, PDF — qué espera DGI).
5. **Plazos exactos** y excepciones por tipo de contribuyente.
6. **Obligación según régimen** (¿zonas francas tienen formatos distintos? ¿personas naturales sin actividad comercial?).

---

## 4. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1-placeholder | 2026-05-26 | Propuesta inicial con 5 formatos estimados. Requiere validación con consultores fiscales PA. |

---

## 5. Revisión pendiente — CRÍTICA

Preguntas **bloqueantes** para los consultores fiscales PA:

1. **¿Cuál es el listado oficial de formatos fiscales DGI Panamá?** URL de referencia.
2. **¿La Declaración mensual del ITBMS tiene un código oficial específico?** (Similar a F-606 en DR o F-1001 en CO).
3. **¿Las declaraciones se entregan vía portal web o por XML automatizado?** Esto define el `formatoSalida`.
4. **¿Existe un formato de información exógena similar a la del DIAN colombiano?** (Reporte detallado de operaciones con terceros).
5. **¿Las empresas inscritas en regímenes especiales (ZLC, AEEPP, Ciudad del Saber) tienen formatos adicionales o exenciones de presentación?**
6. **¿Hay reporte específico para CDIs (Convenios para Evitar Doble Imposición) cuando se aplican tarifas reducidas?**
7. **Aviso de Operación:** ¿La renovación anual del Aviso de Operación es un formato fiscal o tributario aparte que debamos modelar?
8. **¿Qué pasa con las facturas electrónicas?** ¿Existe un esquema de factura electrónica obligatoria en PA con reporte propio?
