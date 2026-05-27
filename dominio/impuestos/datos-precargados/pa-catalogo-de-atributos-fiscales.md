# Catálogo de Atributos Fiscales — Panamá

**País:** Panamá (`PA`)
**Catálogo del modelo:** `CatalogoDeAtributosFiscales` (Sección 3.5)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`pa-catalogo-de-atributos-fiscales.json`](pa-catalogo-de-atributos-fiscales.json)

---

## 1. Propósito

Atributos del `PerfilTributario` que el motor evalúa para resolver tratamiento tributario en PA. Compacto como DR (8 atributos vs 15 de CO), con énfasis en las tres áreas económicas especiales panameñas.

---

## 2. Cobertura

**8 atributos precargados.**

| Nombre | Tipo | Requerido | Notas |
|---|:---:|:---:|---|
| `tipoContribuyente` | enum | Sí | Natural / Juridica |
| `ruc` | string | Sí | Registro Único DGI |
| `esContribuyenteITBMS` | boolean | Sí | Registrado como contribuyente ITBMS |
| `esAgenteRetencionITBMS` | boolean | No | Designado por DGI |
| `esAgenteRetencionISR` | boolean | No | Designado por DGI |
| `inscripcionZonaLibreColon` | enum + catalogoReferencia | No | Filtro `zona-economica-especial` + subtipo `zona-libre-colon` |
| `inscripcionAEEPP` | enum + catalogoReferencia | No | Filtro `zona-economica-especial` + subtipo `panama-pacifico` |
| `inscripcionCiudadDelSaber` | enum + catalogoReferencia | No | Filtro `zona-economica-especial` + subtipo `ciudad-del-saber` |

---

## 3. Notas operativas

### 3.1. Tres atributos para tres regímenes especiales

PA modela cada régimen especial como un atributo distinto (`inscripcionZonaLibreColon`, `inscripcionAEEPP`, `inscripcionCiudadDelSaber`) en lugar de un único atributo genérico. Razón: cada régimen tiene **autoridad distinta** y **alcance fiscal distinto**, por lo que las condiciones de aplicación necesitan distinguirlos.

Alternativa considerada: un único atributo `regimenEspecialAplicable` con catálogo genérico. Se descartó porque complicaría la lectura de las condiciones (requeriría verificar el tipo además del valor).

### 3.2. `esContribuyenteITBMS`

A diferencia de CO o DR, en PA no todas las empresas están automáticamente registradas como contribuyentes del ITBMS. Las personas naturales con ingresos menores a USD 36,000 anuales no son contribuyentes obligatorios. El motor debe verificar este atributo antes de aplicar ITBMS.

### 3.3. `ruc` como atributo del perfil vs identidad de Terceros

Igual que en CO/DR, el RUC vive en el sub-dominio Terceros como `IdentificacionFiscal`. Aquí lo proyectamos para que el motor pueda consultarlo en condiciones (validar formato, verificar contra listas DGI).

### 3.4. Atributos NO incluidos (vs CO)

- `perteneceRegimenSimple` — PA no tiene equivalente directo.
- `esGranContribuyente` — la designación de Gran Contribuyente DGI no afecta tarifas transaccionales (solo plazos de pago y declaraciones).

---

## 4. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 8 atributos (3 requeridos + 5 opcionales, 3 de los cuales con `catalogoReferencia`). |

---

## 5. Revisión pendiente

1. **`esContribuyenteITBMS` — umbral:** ¿El umbral de USD 36,000 anuales es vigente? Pendiente de validación.
2. **¿Faltan atributos relevantes?** Casos posibles: `esSEM` (Sede de Empresas Multinacionales, Ley 41/2007), `regimenFiscalEspecial` (genérico para decretos individuales).
3. **`tipoContribuyente`:** ¿Existen más categorías además de Natural/Juridica? (ej: Persona Jurídica Extranjera, Trust).
4. **¿La designación como agente de retención puede ser parcial?** (ej: agente solo de ITBMS o solo de ISR pero no ambos).
