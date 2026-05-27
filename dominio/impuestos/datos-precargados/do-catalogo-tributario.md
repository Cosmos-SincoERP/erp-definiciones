# Catálogo Tributario — República Dominicana

**País:** República Dominicana (`DO`)
**Catálogo del modelo:** `CatalogoTributario` (Sección 3.2 de `modelo-dominio.md`)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`do-catalogo-tributario.json`](do-catalogo-tributario.json)

---

## 1. Propósito

Catálogo de tributos vigentes en República Dominicana para la operación del producto. Cubre el universo de tributos directos sobre transacciones (ITBIS, ISC, CDT, PROPINA) más la retención de ITBIS (RITBIS). Otros tributos como Impuesto sobre la Renta (ISR), Patentes Comerciales o Tributos Sucesorales no están en F1 porque no se calculan transaccionalmente.

---

## 2. Fuente normativa

- **Código Tributario:** Ley 11-92 (Título I, II, IV).
- **ITBIS:** Ley 11-92 Título III (arts. 335 y ss.).
- **ISC:** Ley 11-92 Título IV.
- **CDT:** Ley 153-98 (Telecomunicaciones).
- **PROPINA:** Ley 158-01 (Propina Legal).
- **Reglamentos DGII** vigentes (varios años).

---

## 3. Cobertura

| Categoría | Cantidad |
|---|:---:|
| Tributos directos | 4 (ITBIS, ISC, CDT, PROPINA) |
| Tributos de provisión / retenciones | 1 (RITBIS) |
| Clasificaciones tributarias | 4 |
| Tratamientos explícitos `aplica: true` | 5 |
| Reglas de localización | 5 |
| **Total** | **19 entidades** |

---

## 4. Tributos

| Código | Nombre | Naturaleza | Nivel | Factor de tarifa | Tributo padre |
|---|---|:---:|:---:|---|:---:|
| `ITBIS` | Impuesto a la Transferencia de Bienes Industrializados y Servicios | aditivo | nacional | clasificacion | — |
| `RITBIS` | Retención del ITBIS | sustractivo | nacional | porcentajeDePadre | `ITBIS` |
| `ISC` | Impuesto Selectivo al Consumo | aditivo | nacional | clasificacion | — |
| `CDT` | Contribución al Desarrollo de las Telecomunicaciones | aditivo | nacional | fija | — |
| `PROPINA` | Propina Legal | aditivo | nacional | fija | — |

---

## 5. Clasificaciones

| Código | Nombre | Tributos que aplican |
|---|---|---|
| `GRAV_ITBIS_18` | Gravados ITBIS 18% | ITBIS, RITBIS |
| `GRAV_ITBIS_16` | Gravados ITBIS 16% | ITBIS, RITBIS |
| `EXENTO_ITBIS` | Exentos de ITBIS | — |
| `ISC_APLICABLE` | Sujeto a ISC | ISC |

CDT y PROPINA no requieren clasificación: aplican por **dominio del concepto** (servicios de telecomunicaciones para CDT, hospedaje/restaurante para PROPINA) — el motor los activa por la naturaleza del bien/servicio según condiciones de aplicación.

---

## 6. Reglas de localización

| Tributo | Rol que manda | Fallback |
|---|---|---|
| ITBIS, RITBIS, ISC, CDT, PROPINA | `sedeEmisora` | — |

Todos los tributos son nacionales — la sede del emisor determina el país, y no hay tributos subnacionales en F1.

---

## 7. Notas operativas

### 7.1. ITBIS — Tarifa reducida 16%

La tarifa reducida del 16% aplica a un grupo específico de bienes (yogur natural, café, azúcar, chocolate sin azúcar, etc.) que en la reforma tributaria 2012 fueron movidos del exento al gravado pero con tarifa reducida. La lista completa la mantiene DGII vía resoluciones.

### 7.2. ISC — Diversidad de productos

El ISC tiene **tarifas distintas por producto**:
- Tabaco y bebidas alcohólicas: tarifas especificas (Ad valorem + específicos por volumen).
- Vehículos: tarifa según cilindrada / valor.
- Telecomunicaciones: 10%.
- Bebidas azucaradas: tasa específica por litro.

La precarga inicial usa `clasificacion: ISC_APLICABLE` como ancla; las tarifas específicas por producto se modelarán como entradas distintas en `do-tarifa-tributaria.json` con sub-clasificaciones.

### 7.3. RITBIS — Tarifa general 30%

La retención general del ITBIS por agentes de retención designados es del 30% del ITBIS facturado. Existen casos especiales (servicios profesionales de personas físicas: 100%; servicios prestados por entidades del exterior: tarifas distintas) que requieren modelado adicional.

### 7.4. PROPINA — Obligación de establecimientos de servicio

La Propina Legal aplica únicamente a establecimientos gastronómicos y hoteleros (Ley 158-01). El motor debe condicionar su aplicación a la actividad económica del emisor (sector CNAE de hoteles, restaurantes).

### 7.5. `caracterRetencion` para tributos aditivos

ITBIS, ISC, CDT y PROPINA llevan `caracterRetencion: null` porque no son retenciones. RITBIS lleva `anticipado` porque se compensa con la declaración mensual de ITBIS del retenido.

---

## 8. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 5 tributos + 4 clasificaciones + 5 tratamientos + 5 reglas de localización. |

---

## 9. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales DR**:

1. **¿Faltan tributos en F1?** Casos conocidos:
   - **ISR — Impuesto sobre la Renta:** se gestiona declarativamente, no transaccionalmente. ¿Debe entrar al ERP?
   - **Retenciones de ISR** (Honorarios profesionales, alquileres, dividendos): ¿son tributos transaccionales para F1?
   - **Patente Municipal:** anual, no transaccional.
   - **Impuesto al Cheque (Norma General 02-05):** retención 0.0015 sobre cheques.
2. **Clasificaciones GRAV_ITBIS_16:** ¿Cuáles son exactamente los productos aplicables? ¿Conviene crear sub-clasificaciones (GRAV_ITBIS_16_LACTEOS, GRAV_ITBIS_16_AZUCAR)?
3. **PROPINA:** ¿Aplica solo a hoteles y restaurantes, o también a casinos y otros establecimientos de servicio?
4. **RITBIS Tarifa general 30%:** ¿Existen tarifas distintas según el tipo de servicio o de retenedor? Casos conocidos: 100% para servicios profesionales prestados por personas físicas.
5. **¿Tratamientos faltantes?** Por ejemplo, ¿ITBIS aplica también con tarifa 0% a exportaciones (similar a EXENTO_ITBIS pero con derecho a descontar)?
