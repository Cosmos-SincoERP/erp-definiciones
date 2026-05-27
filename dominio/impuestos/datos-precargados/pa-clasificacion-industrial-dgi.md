# Clasificación Industrial DGI — Panamá

**País:** Panamá (`PA`)
**Catálogo del modelo:** Catálogo de referencia para `PerfilTributario.ActividadEconomicaRegistrada.ciiu`.
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`pa-clasificacion-industrial-dgi.json`](pa-clasificacion-industrial-dgi.json)

---

## 1. Propósito

Precarga la clasificación de actividades económicas usada por la DGI Panamá para el Aviso de Operación (la licencia obligatoria para operar comercialmente en Panamá). La clasificación se basa en CIIU Rev. 4 de Naciones Unidas, adaptada por DGI con notas locales en algunas clases.

---

## 2. Fuente normativa

- **DGI Panamá** — Clasificación de Actividades Económicas para Aviso de Operación.
- **Base internacional:** ISIC Rev. 4 (2008) de Naciones Unidas.
- **Ley 5 de 2007** y modificatorias — Ley del Aviso de Operación.

---

## 3. Cobertura

**Total: 109 entradas** (21 secciones + 88 divisiones).

Las clases (4 dígitos) se cargan vía importación del catálogo DGI en implementación.

---

## 4. Notas operativas

### 4.1. Uso desde el modelo

El campo `ActividadEconomicaRegistrada.ciiu` del PerfilTributario lleva el código de 4 dígitos. En PA cumple varios roles:

- Identifica la actividad principal de la empresa para el Aviso de Operación.
- Es el factor de tarifa para condiciones (ej: telecomunicaciones → división 61 → ITBMS reducido + ISC móvil).
- Soporta los reportes de la DGI.

### 4.2. Sin tarifas por actividad

A diferencia de CO (donde ICA municipal depende del CIIU), en PA **no hay tarifas tributarias que dependan de la actividad económica**. La clasificación se usa principalmente para reportes y validaciones de condiciones (sectores específicos como telecomunicaciones, hospedaje, tabaco).

---

## 5. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 21 secciones + 88 divisiones (estándar ISIC Rev. 4 adaptado DGI). |

---

## 6. Revisión pendiente

1. **¿Existe un catálogo oficial DGI con las clases panameñas?** URL y formato.
2. **¿Hay clases con notas locales que no aparezcan en el CIIU estándar?**
3. **¿La clasificación se actualiza periódicamente o se mantiene desde 2012?**
