# Catálogo de Jurisdicciones Fiscales — República Dominicana

**País:** República Dominicana (`DO`)
**Catálogo del modelo:** `JurisdiccionFiscal` (Sección 3.7 de `modelo-dominio.md`)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`do-jurisdiccion-fiscal.json`](do-jurisdiccion-fiscal.json)

---

## 1. Propósito

Catálogo de las jurisdicciones fiscales de República Dominicana. A diferencia de Colombia, **DO no tiene tributos subnacionales operativos en F1** — ITBIS, ISC, CDT, PROPINA y RITBIS son nacionales. Las jurisdicciones se precargan principalmente para:

- Integridad referencial de `ubicaciones.{rol}.subnacional` enviado por consumidores (OXP, CXC).
- Servir de referencia administrativa para regímenes empresariales (parques de zonas francas vinculados a una provincia).
- Preparar el modelo ante futuras necesidades subnacionales si DGII introduce tributos provinciales.

---

## 2. Fuente normativa

- **Constitución de la República Dominicana (2010).**
- **Ley 5220 de 1959** y modificatorias — División Territorial de la República Dominicana.
- **ONE — Oficina Nacional de Estadística:** códigos territoriales oficiales.
- **Ley 163-01:** creación de la Provincia Santo Domingo a partir del Distrito Nacional (octubre 2001).
- **Ley 312-01:** elevación del Distrito Nacional a la categoría de provincia distinta.

---

## 3. Cobertura

**Total: 68 entradas.**

| Nivel | Cantidad |
|---|:---:|
| Nacional | 1 |
| Provincial | 32 (31 provincias + Distrito Nacional) |
| Municipal | 35 (cabeceras provinciales + municipios de Santo Domingo Este/Norte/Oeste y Boca Chica) |
| Régimen especial territorial | 0 (no aplican a DO en F1) |

---

## 4. Provincias (32)

| Código | Nombre | Capital | Fecha de creación |
|---|---|---|---|
| `01` | Distrito Nacional | Santo Domingo de Guzmán | 1844 |
| `02` | Azua | Azua de Compostela | 1844 |
| `03` | Bahoruco | Neiba | 1943 |
| `04` | Barahona | Santa Cruz de Barahona | 1881 |
| `05` | Dajabón | Dajabón | 1938 |
| `06` | Duarte | San Francisco de Macorís | 1896 |
| `07` | Elías Piña | Comendador | 1942 |
| `08` | El Seibo | Santa Cruz de El Seibo | 1844 |
| `09` | Espaillat | Moca | 1885 |
| `10` | Independencia | Jimaní | 1948 |
| `11` | La Altagracia | Higüey | 1961 |
| `12` | La Romana | La Romana | 1944 |
| `13` | La Vega | Concepción de La Vega | 1844 |
| `14` | María Trinidad Sánchez | Nagua | 1959 |
| `15` | Monte Cristi | San Fernando de Monte Cristi | 1879 |
| `16` | Pedernales | Pedernales | 1957 |
| `17` | Peravia | Baní | 1944 |
| `18` | Puerto Plata | San Felipe de Puerto Plata | 1872 |
| `19` | Hermanas Mirabal | Salcedo | 1952 |
| `20` | Samaná | Samaná | 1865 |
| `21` | San Cristóbal | San Cristóbal | 1932 |
| `22` | San Juan | San Juan de la Maguana | 1938 |
| `23` | San Pedro de Macorís | San Pedro de Macorís | 1908 |
| `24` | Sánchez Ramírez | Cotuí | 1952 |
| `25` | Santiago | Santiago de los Caballeros | 1844 |
| `26` | Santiago Rodríguez | Sabaneta | 1948 |
| `27` | Valverde | Mao | 1959 |
| `28` | Monseñor Nouel | Bonao | 1982 |
| `29` | Monte Plata | Monte Plata | 1982 |
| `30` | Hato Mayor | Hato Mayor del Rey | 1984 |
| `31` | San José de Ocoa | San José de Ocoa | 2000 |
| `32` | Santo Domingo | Santo Domingo Este | 2001 |

---

## 5. Notas operativas

### 5.1. Provincia 19 — Hermanas Mirabal

En 2007 (Ley 137-07), la provincia "Salcedo" fue renombrada como "Hermanas Mirabal" en honor a las hermanas Mirabal (símbolos de la lucha contra la dictadura). El municipio cabecera mantiene el nombre histórico "Salcedo".

### 5.2. Provincia 32 — Santo Domingo

Creada en 2001 al separar del Distrito Nacional (Ley 163-01). Tiene cuatro municipios principales precargados: Santo Domingo Este (`3201`, cabecera), Norte (`3202`), Oeste (`3203`) y Boca Chica (`3204`). Es la provincia más poblada del país.

### 5.3. Sin regímenes territoriales

DO no tiene jurisdicciones con `tipo: regimen-especial-territorial` ni `tipoRegimen` poblado en F1. Las zonas francas son **régimen empresarial** (Ley 8-90), no territorial — viven en `CatalogoDeRegimenesEspeciales` (`do-catalogo-de-regimenes-especiales.json`).

### 5.4. Vigencia desde

Para provincias preexistentes a 1844, la fecha de creación se aproxima al 27 de febrero de 1844 (independencia nacional). Para provincias creadas posteriormente, se usa la fecha de la ley de creación.

---

## 6. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 1 nacional + 32 provincias + 35 municipios cabecera y principales = 68 entradas. |

---

## 7. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales DR**:

1. **¿Se necesitan más municipios?** Solo precargué cabeceras provinciales + 4 municipios de Santo Domingo. ¿Hay municipios secundarios relevantes para operación ERP?
2. **Códigos ONE vs DGII:** ¿Los códigos territoriales que usa DGII coinciden con los códigos ONE precargados, o tiene su propio sistema?
3. **¿Existen distritos municipales (subdivisiones de municipios) que el ERP deba modelar?**
4. **Provincia Santo Domingo:** ¿Es correcto precargar 4 municipios (Este/Norte/Oeste/Boca Chica) o necesitamos más detalle (San Antonio de Guerra, Pedro Brand, Los Alcarrizos, etc.)?
5. **Vigencia desde:** ¿Las fechas de creación de provincias son correctas? Use estimaciones basadas en leyes históricas DR.
