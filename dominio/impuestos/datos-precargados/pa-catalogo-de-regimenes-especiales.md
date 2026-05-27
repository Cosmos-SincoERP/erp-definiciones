# Catálogo de Regímenes Especiales — Panamá

**País:** Panamá (`PA`)
**Catálogo del modelo:** `CatalogoDeRegimenesEspeciales` (Sección 3.8)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`pa-catalogo-de-regimenes-especiales.json`](pa-catalogo-de-regimenes-especiales.json)

---

## 1. Propósito

Catálogo de regímenes empresariales panameños. Panamá tiene **3 áreas económicas especiales** principales con regímenes diferenciados, todas modeladas con `tipo: zona-economica-especial`.

Cierra el pendiente `[PD10]` para PA.

---

## 2. Fuente normativa

- **Zona Libre de Colón:** Decreto Ley 18 de 1948 y reglamentos.
- **AEEPP — Área Económica Especial Panamá-Pacífico:** Ley 41 de 2004.
- **Ciudad del Saber:** Decreto Ley 6 de 1998 + Estatuto de la Fundación.

---

## 3. Cobertura

**Total: 3 entradas.** Las tres áreas económicas especiales operativas de Panamá. Cada una es **atómica** — una empresa se inscribe en el régimen como un todo, no en sub-categorías.

| Código | Régimen | Subtipo | Ubicación |
|---|---|---|---|
| `ZLC-EMP` | Zona Libre de Colón | `zona-libre-colon` | Colón (`0301`) |
| `AEEPP-EMP` | Área Económica Especial Panamá-Pacífico | `panama-pacifico` | Arraiján (`1002`) |
| `CDS-EMP` | Ciudad del Saber | `ciudad-del-saber` | Clayton (`0808`) |

---

## 4. Notas operativas

### 4.1. Frontera con `JurisdiccionFiscal`

Las áreas económicas especiales panameñas están **físicamente ubicadas** en territorio panameño pero NO constituyen regímenes territoriales fiscales — las empresas dentro y fuera de estas zonas pagan ITBMS según las mismas reglas territoriales. El beneficio fiscal depende de la **inscripción** de la empresa en el régimen ante la autoridad respectiva, no del solo hecho de operar dentro del polígono físico. Por eso `JurisdiccionFiscal` no tiene entradas con `tipoRegimen` para PA.

### 4.2. Subtipos diferenciados

Aunque las tres tienen `tipo: zona-economica-especial`, sus `subtipo` distintos permiten:
- Distinguir el régimen específico en condiciones de aplicación.
- Filtrar el catálogo desde los atributos del perfil tributario (`inscripcionZonaLibreColon`, `inscripcionAEEPP`, `inscripcionCiudadDelSaber`).
- Documentar las particularidades normativas de cada régimen.

### 4.3. Cardinalidad atómica

Cada régimen panameño es **una entrada única** porque no hay sub-divisiones operativas:
- ZLC: una sola zona física en Colón (no múltiples parques).
- AEEPP: un solo polígono en Arraiján.
- Ciudad del Saber: un solo predio en Clayton.

Esto es distinto a CO (121 zonas francas) o DR (75 parques CNZFE), donde el catálogo lista parques individuales.

### 4.4. Otros regímenes panameños NO precargados

Existen otros regímenes empresariales con beneficios fiscales que NO entran en F1:
- **SEM — Sede de Empresas Multinacionales (Ley 41 de 2007):** régimen para multinacionales que establecen su sede regional en Panamá. No es una zona territorial sino un régimen empresarial puro.
- **EMMA — Empresas Multinacionales para la Prestación de Servicios relacionados con la Manufactura (Ley 159 de 2020):** régimen similar a SEM para manufactura.
- **Régimen de licencias especiales:** zonas turísticas (Ley 80 de 2012), agroindustriales, etc.

Estos se reservan para entradas `personalizado` o expansión futura.

---

## 5. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 3 áreas económicas especiales (ZLC, AEEPP, Ciudad del Saber). |

---

## 6. Revisión pendiente

1. **¿Falta algún régimen importante para F1?** SEM y EMMA son regímenes prominentes — ¿deben entrar al catálogo o se quedan como `personalizado`?
2. **Subtipo `panama-pacifico` vs `aeepp`:** ¿Cuál es la nomenclatura preferida?
3. **AEEPP — sectores específicos:** ¿Hay sub-regímenes dentro de AEEPP según el tipo de operación (logística vs manufactura vs servicios)?
4. **Ciudad del Saber — categorías de inscripción:** ¿Hay distintos niveles de afiliación (empresa de innovación vs centro de investigación vs ONG) que requieran sub-entradas?
5. **ZLC — operadores vs usuarios:** ¿Conviene distinguir entre operador de manzana y empresa usuaria de la zona?
