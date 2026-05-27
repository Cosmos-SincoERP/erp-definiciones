# Catálogo CIIU Rev. 4 A.C. — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** Catálogo de referencia para `PerfilTributario.ActividadEconomicaRegistrada.ciiu`
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`co-ciiu.json`](co-ciiu.json)

---

## 1. Propósito

Este catálogo precarga la Clasificación Industrial Internacional Uniforme **Rev. 4 adaptada para Colombia (CIIU Rev. 4 A.C.)**, vigente desde el 01-12-2012 para cámaras de comercio, DIAN y supervigilancia. Es el catálogo de referencia para:

- El atributo `ciiu` de `ActividadEconomicaRegistrada` dentro del `PerfilTributario` (cada entidad puede tener múltiples actividades económicas registradas con vigencias).
- El factor de búsqueda en `TarifaTributaria` cuando un tributo tiene `factorDeTarifa: actividadEconomica` (ICA, RICA, AUTO_RICA municipales).
- Reportes fiscales que requieren clasificación de actividad económica (información exógena DIAN, declaraciones ICA municipales).

Cierra parcialmente el pendiente `[PD9]` para Colombia.

---

## 2. Fuente normativa

- **Fuente principal:** DANE — Clasificación Industrial Internacional Uniforme Rev. 4 adaptada para Colombia (CIIU Rev. 4 A.C.).
- **URL principal:** [Sistema de clasificaciones DANE](https://clasificaciones.dane.gov.co/ciiu4-0/ciiu4_dispone)
- **Documento maestro:** [CIIU Rev. 4 A.C. (PDF DANE)](https://www.dane.gov.co/files/sen/nomenclatura/ciiu/CIIU_Rev_4_AC2022.pdf)
- **Estructura detallada (Excel):** [EstructuraDetalladaCIIU_4AC.xls](https://www.dane.gov.co/files/sen/nomenclatura/ciiu/EstructuraDetalladaCIIU_4AC.xls)
- **Adopción legal:** Resoluciones DIAN que adoptan la clasificación para uso fiscal.

**Vigencia:** Desde el 01-12-2012. La clasificación se actualiza periódicamente; la versión más reciente del DANE consultada es de 2022.

---

## 3. Estructura jerárquica

CIIU Rev. 4 A.C. tiene cuatro niveles:

| Nivel | Identificador | Cantidad total | Precargado en F1 |
|---|---|---|---|
| Sección | 1 letra (A-U) | 21 | **Sí (21)** |
| División | 2 dígitos | 88 | **Sí (88)** |
| Grupo | 3 dígitos | 246 | No (carga vía Excel DANE) |
| Clase | 4 dígitos | 503 | No (carga vía Excel DANE) |

**Total precargado:** 109 entradas (secciones + divisiones).

**Decisión de alcance:** Las **clases (4 dígitos)** y **grupos (3 dígitos)** NO se precargan en este JSON. Se cargan vía importación del archivo Excel oficial DANE (`EstructuraDetalladaCIIU_4AC.xls`) durante la implementación. Razón: el archivo Excel cambia con las actualizaciones del DANE, y mantener 503 clases sincronizadas manualmente en JSON sería frágil. El catálogo precargado aquí da la estructura macro; las clases entran por carga de datos en runtime.

---

## 4. Cobertura — Secciones (21)

| Sección | Nombre | Divisiones (rango) |
|---|---|---|
| A | Agricultura, ganadería, caza, silvicultura y pesca | 01–03 |
| B | Explotación de minas y canteras | 05–09 |
| C | Industrias manufactureras | 10–33 |
| D | Suministro de electricidad, gas, vapor y aire acondicionado | 35 |
| E | Distribución de agua; evacuación y tratamiento de aguas residuales, gestión de desechos y actividades de saneamiento ambiental | 36–39 |
| F | Construcción | 41–43 |
| G | Comercio al por mayor y al por menor; reparación de vehículos automotores y motocicletas | 45–47 |
| H | Transporte y almacenamiento | 49–53 |
| I | Alojamiento y servicios de comida | 55–56 |
| J | Información y comunicaciones | 58–63 |
| K | Actividades financieras y de seguros | 64–66 |
| L | Actividades inmobiliarias | 68 |
| M | Actividades profesionales, científicas y técnicas | 69–75 |
| N | Actividades de servicios administrativos y de apoyo | 77–82 |
| O | Administración pública y defensa; planes de seguridad social de afiliación obligatoria | 84 |
| P | Educación | 85 |
| Q | Atención de la salud humana y de asistencia social | 86–88 |
| R | Actividades artísticas, de entretenimiento y recreación | 90–93 |
| S | Otras actividades de servicios | 94–96 |
| T | Actividades de los hogares individuales como empleadores o productores para uso propio | 97–98 |
| U | Actividades de organizaciones y entidades extraterritoriales | 99 |

---

## 5. Adaptaciones colombianas relevantes

La adaptación CIIU Rev. 4 A.C. introduce algunas modificaciones respecto a la versión UN estándar:

- **Sección E ampliada:** "Distribución de agua; evacuación y tratamiento de aguas residuales, gestión de desechos y actividades de saneamiento ambiental" — reagrupación de actividades antes dispersas.
- **División 33:** "Instalación, mantenimiento y reparación especializada de maquinaria y equipo" — separada explícitamente de la fabricación.
- **Sección J:** "Información y comunicaciones" — mayor relevancia jerárquica para actividades editoriales (división 58) y de software (división 62) por relevancia económica del sector en Colombia.

---

## 6. Notas operativas

### 6.1. Códigos numéricos vs alfanuméricos

- Las secciones se identifican con **una letra** (A-U). En el JSON, el campo `codigo` lleva la letra como string.
- Las divisiones se identifican con **dos dígitos**. En el JSON, `codigo` lleva un string de 2 caracteres con cero a la izquierda cuando aplica (ej: `"01"`, `"05"`, `"99"`).
- Las clases se identifican con **cuatro dígitos** sin cero a la izquierda (ej: `"4711"` para comercio al por menor en establecimientos no especializados).

### 6.2. Uso desde el modelo

El atributo `ActividadEconomicaRegistrada.ciiu` del `PerfilTributario` lleva un código de **4 dígitos** (nivel clase). El motor de cálculo busca tarifas en `TarifaTributaria` usando ese código como factor — ejemplo: `tarifa-CO-11001-ICA` → factor `4711` → tarifa 4.14×1000 (Bogotá, comercio).

Los niveles superiores (sección, división, grupo) se usan principalmente para reportes agregados y validación referencial — no para el motor de cálculo transaccional.

### 6.3. Sincronización con catálogo de tarifas municipales

Las tarifas ICA/RICA municipales del archivo `co-tarifa-tributaria.json` referencian códigos CIIU como `factor`. Antes de cargar tarifas, el catálogo CIIU completo (las 503 clases) debe estar disponible vía el Excel DANE. El catálogo precargado aquí (109 entradas estructurales) no cubre todas las tarifas — es complemento de la carga masiva.

### 6.4. Actualizaciones futuras

Cuando DANE publique una actualización (ej: Rev. 5 en algún año futuro), el catálogo se reemplaza completo vía cierre de las entradas vigentes (`JurisdiccionCerrada` equivalente para CIIU) y precarga del nuevo Excel. Las `ActividadEconomicaRegistrada` históricas conservan su código en snapshot — la traducción a la nueva clasificación se haría vía `HomologacionFiscal` (F2).

---

## 7. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 21 secciones + 88 divisiones de CIIU Rev. 4 A.C. (DANE). Grupos y clases excluidos del JSON — carga vía Excel DANE en implementación. |

---

## 8. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales**:

1. **¿Precargamos también las 503 clases en este JSON, o aceptamos la decisión de cargarlas vía Excel DANE en implementación?** La decisión actual es la segunda. Riesgo: el equipo de desarrollo necesita un proceso de importación robusto para el Excel.
2. **¿Hay clases específicas que el ERP deba precargar en F1 (ej: las top 50 más usadas en operaciones de los clientes piloto)?** Si sí, agregarlas a este JSON como subset operativo.
3. **¿La estructura jerárquica `seccion → division → grupo → clase` es la que se debe persistir en el sistema, o solo necesitamos las clases (códigos de 4 dígitos) sin la jerarquía superior?** El modelo dice que `ActividadEconomicaRegistrada.ciiu` es un código de 4 dígitos, pero los reportes agregados pueden requerir agrupar por sección/división.
4. **Versionado del DANE:** ¿Trabajamos con la versión 2022 o esperamos a alguna actualización posterior?
5. **División 99 y Sección U:** ¿Aplican realmente a entidades en Colombia o son solo de uso estadístico? Si no aplican, no se precargan en `TarifaTributaria`.
