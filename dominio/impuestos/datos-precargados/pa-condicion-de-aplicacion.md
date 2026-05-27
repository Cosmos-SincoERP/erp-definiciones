# Catálogo Condiciones de Aplicación — Panamá

**País:** Panamá (`PA`)
**Catálogo del modelo:** `CondicionDeAplicacion` (Sección 3.4)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`pa-condicion-de-aplicacion.json`](pa-condicion-de-aplicacion.json)

---

## 1. Propósito

Reglas declarativas para ajustar el tratamiento tributario en PA según el perfil del sujeto, las áreas económicas especiales y el régimen territorial de renta. 11 condiciones precargadas — más complejo que DR (9) por el énfasis en regímenes especiales y CDIs internacionales.

---

## 2. Cobertura

**Total: 11 condiciones.**

| Tributo | Condiciones |
|---|:---:|
| ITBMS | 4 (2 régimen + 2 exoneraciones por área especial) |
| RITBMS | 3 (1 activación agente + 2 exclusiones) |
| ISC | 1 (default) |
| ISR | 3 (1 activación + 1 territorial + 1 CDIs) |

---

## 3. Condiciones destacadas

### 3.1. ITBMS — Exoneración por áreas especiales

Los códigos `ITBMS-02-zlc` y `ITBMS-03-aeepp` aplican exoneración cuando la empresa está inscrita en ZLC o AEEPP. El alcance exacto requiere validación con consultores — la exoneración puede ser total (todas las operaciones), parcial (solo exportaciones) o por tipo de operación.

**Falta condición para Ciudad del Saber** porque su exoneración está más enfocada en ISR e importación de equipos que en ITBMS. Pendiente confirmación.

### 3.2. ISR — Territorialidad

La condición `ISR-02-territorial` materializa el **principio territorial de renta** panameño: pagos al exterior por servicios prestados desde el extranjero (fuente extranjera) no están sujetos a ISR. Esta es una de las particularidades fiscales más distintivas de PA.

**Criterio `fuente = extranjera`:** el motor debe poder determinar si el pago es de fuente panameña o extranjera. Esto se evalúa caso por caso considerando el lugar de prestación del servicio y la residencia del beneficiario. La determinación es **subjetiva** y suele requerir intervención manual o reglas configurables por concepto.

### 3.3. ISR — Convenios para Evitar Doble Imposición (CDIs)

La condición `ISR-03-cdi` reconoce que los CDIs vigentes reducen la tarifa de retención. Panamá tiene CDIs con países como España, México, Italia, Holanda, Singapur, entre otros. El motor consulta una **tabla de CDIs** (pendiente de modelar) para resolver tarifa aplicable.

**Propuesta:** modelar una `TablaCDI` con tarifas reducidas por (país, concepto). No está en el catálogo F1 actual — pendiente con consultores.

---

## 4. Notas operativas

### 4.1. RITBMS — Lógica de activación

Para que aplique RITBMS necesita coincidir:
1. El comprador es agente de retención designado.
2. El proveedor es contribuyente ITBMS.
3. Hay ITBMS facturado.

Si alguna de las tres condiciones falla, RITBMS no aplica. Esto se modela con tres condiciones (`RITBMS-01` aplicar, `RITBMS-02` exclusión por no-agente, `RITBMS-03` exclusión por no-contribuyente).

### 4.2. ITBMS — Exoneraciones territoriales pendientes

Las condiciones `ITBMS-02-zlc` e `ITBMS-03-aeepp` son simplificaciones. En la realidad:
- **ZLC:** la exoneración aplica para re-exportación; las ventas locales sí están gravadas.
- **AEEPP:** la exoneración varía por tipo de actividad certificada.

Estas reglas requieren refinamiento con consultores.

---

## 5. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 11 condiciones (4 ITBMS + 3 RITBMS + 1 ISC + 3 ISR). |

---

## 6. Revisión pendiente

1. **ITBMS — Ciudad del Saber:** ¿Debe haber condición específica de exoneración ITBMS, o las exoneraciones de CDS son solo ISR/importación?
2. **ITBMS — ZLC ventas locales:** ¿Cómo se distinguen las ventas al exterior (exentas) de las ventas locales (gravadas) dentro de la ZLC?
3. **ITBMS — AEEPP por tipo de operación:** ¿Cuáles son las sub-condiciones según tipo de actividad certificada?
4. **ISR — Determinación de fuente:** ¿Hay reglas determinísticas que el motor pueda aplicar para determinar fuente panameña vs extranjera, o siempre requiere análisis manual?
5. **ISR — Tabla CDIs:** ¿Modelamos como nuevo agregado (`TablaCDI`) o como sub-catálogo dentro de `TarifaTributaria`?
6. **¿Falta condición específica para SEM** (Sede de Empresas Multinacionales)? Ley 41/2007 tiene tratamiento ISR distinto.
