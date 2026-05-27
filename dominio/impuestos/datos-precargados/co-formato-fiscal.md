# Catálogo Formatos Fiscales — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `FormatoFiscal` (Sección 3.11 — fase F2)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`co-formato-fiscal.json`](co-formato-fiscal.json)

---

## 1. Propósito

Precarga los formatos fiscales que el ERP debe generar al cierre de cada periodo: reportes de información exógena DIAN, reportes municipales de retenciones de ICA y certificados tributarios.

Cada `FormatoFiscal` declara el contrato del entregable (estructura, periodicidad, autoridad destinataria, formato de salida) y referencia una `HomologacionFiscal` cuando requiere traducir valores internos del ERP a códigos de la autoridad.

---

## 2. Fuente normativa

- **Información exógena DIAN:** Estatuto Tributario art. 631 + Resoluciones anuales (la última vigente: 000233 de 2026).
- **Certificado de retención:** Estatuto Tributario art. 381.
- **Reportes ICA municipales:** Estatutos tributarios municipales (cada ciudad define formato y plazos).

---

## 3. Cobertura

**Total: 10 formatos.**

| Categoría | Cantidad |
|---|:---:|
| Reportes DIAN (información exógena) | 8 |
| Reportes municipales (ICA) | 1 placeholder genérico |
| Certificados tributarios | 1 |

---

## 4. Formatos DIAN

| Código | Nombre | Periodicidad |
|---|---|---|
| `F-1001` | Pagos o abonos en cuenta y retenciones practicadas | Anual (corte 31-03 año siguiente) |
| `F-1003` | Retenciones en la fuente practicadas | Anual |
| `F-1005` | IVA descontable | Anual |
| `F-1006` | IVA generado | Anual |
| `F-1007` | Ingresos recibidos | Anual |
| `F-1647` | Ingresos recibidos para terceros | Anual |
| `F-2276` | Información de rentas de trabajo y pensiones | Anual |
| `F-2856` | Activos digitales (Resolución 000233/2026) | Anual |

Todos en formato XML + Excel prevalidador.

## 5. Formatos municipales

| Código | Nombre | Periodicidad |
|---|---|---|
| `REPORTE-ICA-MUNICIPAL` | Reporte de retenciones de ICA practicadas | Variable (mensual/bimestral/cuatrimestral según municipio) |

Cada una de las 12 ciudades cubiertas en `co-jurisdiccion-fiscal.json` define su propio formato y plazo. El `FormatoFiscal` precargado es un **placeholder genérico** — la instancia específica por municipio se genera en runtime con `origen: personalizado` cuando el cliente activa el módulo de ese municipio.

## 6. Certificados

| Código | Nombre | Tipo |
|---|---|---|
| `FORM-220` | Certificado de Retención en la Fuente — Año Gravable | PDF anual |

---

## 7. Notas operativas

### 7.1. Reportes anuales — fecha de corte 31-03 año siguiente

Todos los reportes DIAN se entregan en marzo del año siguiente al año gravable. Ejemplo: año gravable 2025 → reportes entregados antes del 31 de marzo de 2026.

### 7.2. Resolución 000233 de 2026

La nueva resolución vigente (marzo 2026) introduce:
- **F-2856** — activos digitales (criptomonedas, NFTs, otros).
- **F-2854** — ingresos del exterior (pendiente de modelar en este catálogo).
- **Modificaciones a F-1001 y F-1647** — nuevas columnas y validaciones (verificar con consultores).

### 7.3. Reportes ICA — fragmentación por municipio

Cada uno de los ~12 municipios principales cubiertos en F1 tiene su propio formato de reporte de retención de ICA. El placeholder `REPORTE-ICA-MUNICIPAL` se instancia con `origen: personalizado` por cada municipio activo del cliente. En implementación, se construye un sub-catálogo `REPORTE-ICA-{codigo-municipio}` con sus particularidades (formato, periodicidad, plazos).

### 7.4. FormatoSalida

Los reportes DIAN soportan XML (envío directo al portal DIAN) y Excel via "prevalidador" (herramienta DIAN que valida y convierte a XML). En implementación, el motor debe generar AMBOS para que el cliente escoja.

---

## 8. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 10 formatos (8 DIAN + 1 ICA placeholder + 1 Cert. 220). |

---

## 9. Revisión pendiente

1. **¿F-2854 (ingresos del exterior) debe entrar al catálogo F1?** Introducido por Res. 000233/2026.
2. **Reportes ICA específicos:** ¿Modelamos los formatos por municipio en el catálogo estándar (Bogotá, Medellín, Cali, etc.) o se quedan como `personalizado`?
3. **Estructura de secciones (Secciones del FormatoFiscal):** ¿Modelamos las secciones del formato (cabecera, detalles, sumarias) en el JSON o se difieren a implementación?
4. **Plazos por modificaciones recientes:** Resolución 000233/2026 cambió plazos en algunos formatos — verificar.
5. **Periodicidad — soporte mensual y bimestral:** ¿La declaración de IVA mensual genera reportes mensuales o solo anuales? Verificar.
