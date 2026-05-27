# Catálogo de Atributos Fiscales — República Dominicana

**País:** República Dominicana (`DO`)
**Catálogo del modelo:** `CatalogoDeAtributosFiscales` (Sección 3.5)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`do-catalogo-de-atributos-fiscales.json`](do-catalogo-de-atributos-fiscales.json)

---

## 1. Propósito

Define los atributos del `PerfilTributario` que el motor de cálculo evalúa para resolver tratamiento tributario en DR. Comparado con CO (15 atributos), el catálogo DR es **mucho más compacto** (6 atributos) porque la operación fiscal dominicana es más simple — no hay tantos regímenes especiales por calidad del contribuyente.

---

## 2. Fuente normativa

- **RNC y Registro:** Código Tributario art. 50 y siguientes.
- **NCF y e-CF:** Normas Generales DGII 06-2018 (NCF) y 06-2021 (e-CF).
- **Agente de Retención ITBIS:** Norma General 02-05.
- **Gran Contribuyente:** Resolución DGII anual.
- **Zonas Francas:** Ley 8-90 administrada por CNZFE.

---

## 3. Cobertura

**6 atributos precargados (3 requeridos + 3 opcionales).**

| Nombre | Tipo | Requerido | Notas |
|---|:---:|:---:|---|
| `tipoContribuyente` | enum | Sí | PersonaFisica / PersonaJuridica |
| `rnc` | string | Sí | Registro Nacional del Contribuyente (9 dígitos para PJ, cédula para PF) |
| `ncf` | boolean | Sí | Autorizado a emitir NCF/e-CF |
| `esAgenteRetencionITBIS` | boolean | No | Designado por DGII (NG 02-05) |
| `esGranContribuyente` | boolean | No | Calificación DGII anual |
| `inscripcionParqueZonaFranca` | enum + catalogoReferencia | No | Habilita beneficios Ley 8-90 |

---

## 4. Notas operativas

### 4.1. `rnc` como atributo del perfil fiscal vs identidad de Terceros

El RNC es el identificador fiscal único en DR. Por **convención del proyecto**, las identificaciones de personas (cédula, RNC, pasaporte) viven en el sub-dominio Terceros como `IdentificacionFiscal`. Aquí lo declaramos como atributo del perfil para que el motor pueda consultarlo en condiciones (validar formato, verificar contra listas DGII). En implementación, el perfil tributario lee el RNC desde Terceros vía integración.

### 4.2. `ncf` y migración a e-CF

DR está migrando del sistema de NCF físico al sistema de e-CF electrónico (norma 06-2021). El atributo booleano indica si el contribuyente está autorizado, sin distinguir entre formatos. Si en el futuro se necesita distinguir, se agregaría un atributo enum (`tipoComprobante: NCF | eCF | ambos`).

### 4.3. `esAgenteRetencionITBIS`

Esta calidad la designa DGII por resolución. Activa la condición `RITBIS-AGENTE` que retiene el 30% del ITBIS facturado por el proveedor.

### 4.4. `inscripcionParqueZonaFranca`

Es el único atributo con `catalogoReferencia` en DR. El valor debe coincidir con un código del catálogo `CatalogoDeRegimenesEspeciales` filtrado por `tipo: zona-franca`. Las empresas inscritas en ZFs acceden a:
- Exoneración del ISR.
- Exoneración del ITBIS sobre ventas a contribuyentes locales (bajo condiciones).
- Exoneración de aranceles de importación.
- Régimen especial de divisas.

### 4.5. Atributos que NO incluí (vs CO)

A diferencia de CO, en DR no se modelan en F1:
- `perteneceRegimenSimple` — DR no tiene equivalente del Régimen Simple colombiano.
- `esExentoRetefuente` — no hay RETEFUENTE equivalente en transacciones DR.
- `esAgenteRetenedorICA` y similares — DR no tiene tributos municipales por actividad.

---

## 5. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 6 atributos (3 requeridos + 3 opcionales). |

---

## 6. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales DR**:

1. **¿Faltan atributos relevantes?** Casos conocidos:
   - **`esRetenedorISR`** (para retenciones de ISR sobre honorarios profesionales, alquileres, etc.).
   - **`esExportador`** (impacta ITBIS sobre exportaciones).
   - **`tipoZonaFranca`** (industrial, servicios, comercial).
2. **`rnc` como atributo fiscal vs en Terceros:** ¿Confirmamos que el RNC vive en Terceros y aquí solo lo proyectamos, o el sub-dominio Impuestos lo necesita como atributo propio?
3. **`ncf` boolean vs enum:** ¿La migración NCF → e-CF requiere ya distinguir, o el booleano es suficiente por ahora?
4. **¿Existen calidades de retención específicas que un agente puede tener?** (ej: retenedor de honorarios profesionales pero no de servicios técnicos).
