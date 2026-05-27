# Catálogo de Jurisdicciones Fiscales — Panamá

**País:** Panamá (`PA`)
**Catálogo del modelo:** `JurisdiccionFiscal` (Sección 3.7)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`pa-jurisdiccion-fiscal.json`](pa-jurisdiccion-fiscal.json)

---

## 1. Propósito

Catálogo de jurisdicciones fiscales de Panamá. Igual que DR, **PA no tiene tributos subnacionales operativos en F1** — ITBMS, ISR e ISC son nacionales. Las jurisdicciones se precargan para:

- Integridad referencial de `ubicaciones.{rol}.subnacional`.
- Servir como referencia para regímenes empresariales (ZLC ubicada en Colón, AEEPP en Panamá Oeste, Ciudad del Saber en Clayton).
- Preparar el modelo ante futuras necesidades.

---

## 2. Fuente normativa

- **Constitución Política de Panamá** (1972, reformas 1978, 1983, 1994, 2004).
- **INEC — Instituto Nacional de Estadística y Censo:** División Político Administrativa.
- **Ley 1 de 1982:** Comarca Kuna Yala (renombrada Guna Yala en 2010).
- **Ley 22 de 1983:** Comarca Emberá-Wounaan.
- **Ley 10 de 1997:** Comarca Ngäbe-Buglé.
- **Ley 119 de 2013:** Creación de la Provincia de Panamá Oeste (escisión de Panamá).

---

## 3. Cobertura

**Total: 25 entradas.**

| Nivel | Cantidad |
|---|:---:|
| Nacional | 1 |
| Provincial | 13 (10 provincias + 3 comarcas indígenas con rango provincial) |
| Distrital | 11 (cabeceras de cada provincia/comarca + Clayton — sede de Ciudad del Saber) |
| Régimen especial territorial | 0 (no aplican a PA en F1) |

---

## 4. Provincias y comarcas (13)

| Código | Nombre | Cabecera |
|---|---|---|
| `01` | Bocas del Toro | Bocas del Toro |
| `02` | Coclé | Penonomé |
| `03` | Colón | Colón |
| `04` | Chiriquí | David |
| `05` | Darién | La Palma |
| `06` | Herrera | Chitré |
| `07` | Los Santos | Las Tablas |
| `08` | Panamá | Panamá (capital nacional) |
| `09` | Veraguas | Santiago |
| `10` | Panamá Oeste | La Chorrera |
| `11` | Comarca Kuna Yala (Guna Yala) | El Porvenir |
| `12` | Comarca Emberá-Wounaan | Unión Chocó |
| `13` | Comarca Ngäbe-Buglé | Llano Tugrí |

Las **tres comarcas indígenas** tienen rango provincial bajo la Constitución panameña — su división administrativa interna se rige por leyes orgánicas específicas, no por la división municipal estándar.

---

## 5. Distritos precargados (11)

Cabeceras de provincia/comarca + Clayton (sede de Ciudad del Saber dentro del distrito de Panamá) + Arraiján (sede del Área Económica Especial Panamá-Pacífico):

| Código | Nombre | Provincia |
|---|---|---|
| `0101` | Bocas del Toro | Bocas del Toro |
| `0201` | Penonomé | Coclé |
| `0301` | Colón | Colón |
| `0401` | David | Chiriquí |
| `0501` | La Palma | Darién |
| `0601` | Chitré | Herrera |
| `0701` | Las Tablas | Los Santos |
| `0801` | Panamá | Panamá |
| `0808` | Clayton (corregimiento) | Panamá (sede Ciudad del Saber) |
| `0901` | Santiago | Veraguas |
| `1001` | La Chorrera | Panamá Oeste |
| `1002` | Arraiján | Panamá Oeste (sede AEEPP) |

---

## 6. Notas operativas

### 6.1. Sin regímenes territoriales fiscales

A diferencia de CO (Puerto Libre), PA NO tiene jurisdicciones con `tipo: regimen-especial-territorial`. Sus áreas económicas especiales (ZLC, AEEPP, Ciudad del Saber) están **físicamente ubicadas** en territorio panameño pero NO constituyen regímenes territoriales fiscales — las empresas dentro y fuera de estas zonas pagan ITBMS según las mismas reglas. El beneficio fiscal depende de la **inscripción empresarial**, no de la ubicación. Por eso `JurisdiccionFiscal` no tiene entradas con `tipoRegimen`.

### 6.2. Códigos compuestos para distritos

Los códigos distritales de PA son **compuestos de 4 dígitos** (provincia `0X` + distrito `XX`). Ejemplo: `0801` es el distrito de Panamá dentro de la provincia 08. INEC también tiene códigos de corregimientos de 6 dígitos (`080800` para Clayton dentro del distrito de Panamá); en el modelo se precargan solo distritos para alinear con CO/DR.

### 6.3. Clayton — distrito de referencia para Ciudad del Saber

El catálogo precarga `0808` como código didáctico para identificar a Clayton (corregimiento donde se ubica la Fundación Ciudad del Saber). El código real INEC podría diferir — pendiente de validación con consultores.

### 6.4. Arraiján — distrito de referencia para AEEPP

`1002` es Arraiján, distrito de Panamá Oeste donde se ubica el Área Económica Especial Panamá-Pacífico (antiguo predio militar de Howard).

### 6.5. Panamá Oeste — creación reciente (2014)

La provincia se creó en 2014 por la Ley 119/2013 al desprenderse del antiguo distrito Capira-La Chorrera de la provincia de Panamá. Sus dos distritos principales son La Chorrera (cabecera) y Arraiján.

---

## 7. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 1 nacional + 10 provincias + 3 comarcas indígenas + 11 distritos cabecera y referencias para regímenes = 25 entradas. |

---

## 8. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales PA**:

1. **¿Cuál es el código INEC oficial para Clayton (Ciudad del Saber)?** El precargado es `0808` como aproximación.
2. **¿Necesitamos modelar corregimientos (6 dígitos) además de distritos?** INEC tiene esa subdivisión adicional.
3. **¿Las comarcas tienen subdivisiones internas que el ERP deba modelar?** En la práctica son territorios de gobierno indígena con dinámica particular.
4. **¿Falta algún distrito secundario relevante para operación ERP?**
5. **Vigencia desde de provincias preexistentes:** ¿Las fechas (1903-11-03 para las creadas con la independencia) son correctas, o conviene rastrear las leyes orgánicas específicas?
