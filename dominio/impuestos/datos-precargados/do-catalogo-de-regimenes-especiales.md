# Catálogo de Regímenes Especiales — República Dominicana

**País:** República Dominicana (`DO`)
**Catálogo del modelo:** `CatalogoDeRegimenesEspeciales` (Sección 3.8)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`do-catalogo-de-regimenes-especiales.json`](do-catalogo-de-regimenes-especiales.json)

---

## 1. Propósito

Catálogo de **regímenes empresariales** en República Dominicana al que una empresa puede estar inscrita para acceder a tratamiento fiscal diferenciado. En F1 DR aplica únicamente el tipo **`zona-franca`** — el sistema de Zonas Francas de Exportación administrado por el Consejo Nacional de Zonas Francas de Exportación (CNZFE).

A diferencia de Colombia (que tiene zonas francas + monopolios departamentales + Puerto Libre), DR concentra sus regímenes empresariales en un único tipo. Otros casos (regímenes turísticos como CONFOTUR Ley 158-01, regímenes especiales por decretos individuales) se reservan para entradas `personalizado` o expansiones posteriores.

Cierra parcialmente el pendiente `[PD10]` para DR.

---

## 2. Fuente normativa

- **Ley 8-90 de 1990:** Régimen de Zonas Francas de Exportación de la República Dominicana.
- **Reglamentos CNZFE:** procedimientos de habilitación y operación.
- **Beneficios fiscales:**
  - Exoneración del Impuesto sobre la Renta (ISR) por 15 años (renovable según ubicación geográfica).
  - Exoneración del ITBIS sobre maquinaria, equipos e insumos.
  - Exoneración de aranceles de importación.
  - Régimen aduanero especial.

---

## 3. Cobertura

**Total precargado: 18 parques.**

| Subtipo | Cantidad | Notas |
|---|:---:|---|
| `industrial` | 15 | Parques industriales principales (maquila textil, manufactura, electrónica). |
| `servicios` | 3 | Zonas francas de servicios (call centers, BPO, turismo). |

**Cobertura total estimada:** ~75 parques CNZFE certificados al cierre 2025. Esta versión precarga una muestra representativa. La precarga completa se valida con consultores fiscales.

---

## 4. Parques de zonas francas precargados

### 4.1. Industriales (15)

| Código | Nombre | Provincia |
|---|---|---|
| `ZF-DO-LAS-AMERICAS` | Zona Franca Industrial Las Américas | Santo Domingo Este (`3201`) |
| `ZF-DO-SAN-PEDRO` | Zona Franca Industrial San Pedro de Macorís | San Pedro de Macorís (`23`) |
| `ZF-DO-SANTIAGO` | Zona Franca Industrial Santiago (ITABO) | Santiago (`25`) |
| `ZF-DO-LA-ROMANA` | Zona Franca Industrial La Romana | La Romana (`12`) |
| `ZF-DO-PUERTO-PLATA` | Zona Franca Industrial Puerto Plata | Puerto Plata (`18`) |
| `ZF-DO-SANTIAGO-NORTE` | Parque Industrial Santiago Norte | Santiago (`25`) |
| `ZF-DO-SAN-ISIDRO` | Parque Industrial San Isidro | Santo Domingo (`32`) |
| `ZF-DO-LAS-CAYAS` | Parque Zona Franca Las Cayas | Duarte (`06`) |
| `ZF-DO-CONSUELO` | Parque Industrial Consuelo | San Pedro de Macorís (`23`) |
| `ZF-DO-HAINA` | Parque Industrial Haina | San Cristóbal (`21`) |
| `ZF-DO-MOCA` | Parque Industrial Moca | Espaillat (`09`) |
| `ZF-DO-BONAO` | Parque Industrial Bonao | Monseñor Nouel (`28`) |
| `ZF-DO-SAN-CRISTOBAL` | Parque Industrial San Cristóbal | San Cristóbal (`21`) |
| `ZF-DO-VALVERDE` | Parque Industrial Valverde | Valverde (`27`) |
| `ZF-DO-BARAHONA` | Parque Industrial Barahona | Barahona (`04`) |
| `ZF-DO-AZUA` | Parque Industrial Azua | Azua (`02`) |

### 4.2. Servicios (3)

| Código | Nombre | Provincia |
|---|---|---|
| `ZF-DO-CABO-ENGAÑO` | Zona Franca Punta Cana / Cabo Engaño | La Altagracia (`11`) |
| `ZF-DO-BAVARO` | Zona Franca Bávaro | La Altagracia (`11`) |
| `ZF-DO-LAS-CAYAS` | (Mixta) | Duarte (`06`) |

---

## 5. Notas operativas

### 5.1. Frontera con `JurisdiccionFiscal`

A diferencia de CO (donde existe Puerto Libre como régimen territorial), DR no tiene **regímenes territoriales fiscales** operativos. El ITBIS y el ISR aplican uniformemente a todo el territorio nacional con la misma tarifa.

Las zonas francas son **régimen empresarial** — el beneficio depende de la **inscripción de la empresa** en el parque certificado por CNZFE, no de la ubicación geográfica de la transacción. Una empresa de Santo Domingo que vende a un cliente en una zona franca no obtiene exoneración solo por el lugar de entrega; el cliente debe ser una empresa inscrita en una ZF.

Por eso `JurisdiccionFiscal` DR no tiene entradas con `tipoRegimen` y este catálogo concentra el modelado fiscal especial.

### 5.2. Subtipo `industrial` vs `servicios`

CNZFE distingue dos tipos de zonas francas:
- **Industriales:** maquila textil, manufactura, electrónica, ensamblaje.
- **Servicios:** call centers, BPO, software, contenidos digitales, salud, turismo.

Algunas ZFs son **mixtas** y albergan ambos tipos de operación. El subtipo precargado refleja el uso principal del parque.

### 5.3. Códigos internos

Los códigos `ZF-DO-LAS-AMERICAS`, `ZF-DO-SAN-PEDRO`, etc. son **códigos internos del catálogo** asignados para esta precarga. CNZFE asigna códigos oficiales en sus resoluciones de habilitación — si los consultores indican usarlos, se renombran.

### 5.4. Tipos del enum NO precargados en F1 para DR

Los tipos `monopolio-sectorial`, `puerto-libre-empresa`, `zona-economica-especial` y `regimen-especial-decreto` están en el enum (`[D13]`) pero NO aplican a DR. Se reservan para entradas `personalizado` cuando el cliente requiera modelar regímenes específicos (ej: incentivos turísticos CONFOTUR Ley 158-01, exenciones por decreto presidencial individual).

---

## 6. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 18 parques de zonas francas (15 industriales + 3 servicios). Marco normativo Ley 8-90 administrado por CNZFE. |

---

## 7. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales DR**:

1. **Listado completo de los 75 parques CNZFE:** ¿El equipo proporciona el listado oficial actualizado para completar el catálogo?
2. **Códigos oficiales CNZFE:** ¿Existen códigos numéricos oficiales (ej: `ZF-001`, `ZF-002`) que debamos usar en lugar de los códigos semánticos actuales?
3. **CONFOTUR (Ley 158-01):** ¿El régimen turístico debe modelarse como un nuevo tipo de régimen empresarial (`regimen-turistico-confotur`) o como entradas `personalizado` por proyecto turístico?
4. **PROINDUSTRIA (Ley 392-07):** ¿Régimen de incentivos a la industria manufacturera — debe entrar al catálogo?
5. **Subtipo de zonas francas:** ¿La distinción industrial/servicios es la que usa CNZFE, o tienen otra taxonomía (ej: manufactura/agroindustria/servicios)?
6. **¿Hay ZFs ya derogadas o canceladas?** Para marcar `vigencia.fechaHasta` correctamente.
