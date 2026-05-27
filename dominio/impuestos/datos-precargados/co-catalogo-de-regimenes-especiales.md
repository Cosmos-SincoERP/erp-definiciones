# Catálogo de Regímenes Especiales — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `CatalogoDeRegimenesEspeciales` (Sección 3.8 de `modelo-dominio.md`)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`co-catalogo-de-regimenes-especiales.json`](co-catalogo-de-regimenes-especiales.json)

---

## 1. Propósito

Catálogo de **regímenes empresariales** colombianos a los que una entidad puede estar inscrita para acceder a tratamiento fiscal diferenciado. Cada inscripción se materializa como un `AtributoFiscal` en el `PerfilTributario` cuya `DefinicionAtributo` tiene `catalogoReferencia` apuntando a este catálogo (decisión `[D13]`).

Tres tipos vigentes en F1 para CO:
- `zona-franca` — zonas francas permanentes (ZFP) y permanentes especiales (ZFPE) habilitadas por DIAN.
- `monopolio-sectorial` — monopolios departamentales sobre licores destilados (Ley 1816/2016) y juegos de azar (Constitución art. 336).
- `puerto-libre-empresa` — componente empresarial del Régimen Puerto Libre San Andrés (complementario al régimen territorial modelado en `JurisdiccionFiscal`).

Cierra parcialmente el pendiente `[PD10]` para Colombia.

---

## 2. Fuente normativa

### 2.1. Zonas francas
- **Marco legal:** Ley 1004 de 2005 + Decretos reglamentarios DIAN (Decreto 2147 de 2016, Decreto 1165 de 2019, Decreto 47 de 2024).
- **Listado oficial:** DIAN (resoluciones de habilitación de cada zona franca).
- **Tipos:**
  - **Permanentes (ZFP):** parques industriales delimitados que albergan a múltiples usuarios. 42 habilitadas (corte mayo 2024).
  - **Permanentes Especiales (ZFPE):** dedicadas a un único usuario industrial. 77 habilitadas (corte mayo 2024).
  - **Transitorias:** asociadas a ferias o eventos.
- **Fuentes de listado público:** [ProColombia Directorio de Zonas Francas](https://procolombia.co/system/files/2024-05/directorio_zonas_francas_espanol.pdf), [DANE Estadísticas Zonas Francas](https://www.dane.gov.co/index.php/estadisticas-por-tema/comercio-internacional/zonas-francas), [Actualícese — Anexo Zonas Francas 2024](https://actualicese.com/rutas/books/impuesto-de-renta-2025-claves-para-su-planeacion-tributaria-eficiente-en-colombia/page/anexo-2-zonas-francas-permanentes-zfp-habilitadas-en-colombia-a-mayo-de-2024).

### 2.2. Monopolios departamentales
- **Licores destilados:** Ley 1816 de 2016 — "Por la cual se fija el régimen propio del monopolio rentístico de licores destilados". Cada departamento decide por iniciativa del gobernador y aprobación de la Asamblea entre régimen de monopolio (operación directa por industria licorera oficial) o régimen de impuesto al consumo. Los dos regímenes son excluyentes.
- **Juegos de azar:** Constitución art. 336 + Ley 643 de 2001 (sistema general de loterías). Cada departamento opera mediante su lotería oficial (o vincula juegos electrónicos a través del sistema departamental).

### 2.3. Puerto Libre San Andrés
- Constitución Política, art. 310.
- Ley 47 de 1993 — "Por la cual se dictan normas especiales para la organización y el funcionamiento del Departamento Archipiélago de San Andrés, Providencia y Santa Catalina".
- Componente empresarial: la empresa se inscribe ante la Cámara de Comercio de San Andrés para acceder a exenciones de impuesto a la renta y régimen aduanero especial.

---

## 3. Cobertura del catálogo

**Total precargado: 38 entradas.**

| Tipo | Subtipo | Cantidad precargada | Cantidad oficial (corte 2024) | Notas |
|---|---|:---:|:---:|---|
| `zona-franca` | permanente | 15 | ~42 | Muestra representativa: las más grandes y verificables. |
| `zona-franca` | permanente-especial | 6 | ~77 | Muestra de ZFPE más conocidas. |
| `monopolio-sectorial` | licores | 11 | ~13–15 | Departamentos con industria licorera activa. |
| `monopolio-sectorial` | juegos-azar | 5 | ~15–20 | Loterías departamentales más conocidas. |
| `puerto-libre-empresa` | — | 1 | 1 | Inscripción única para San Andrés. |
| **Total** | | **38** | **~155** | |

**Decisión de alcance:** Esta versión 1.0 es una **muestra significativa**, no el catálogo completo. Las 121 zonas francas DIAN y los ~33 monopolios departamentales se completan tras validación con el equipo de consultores fiscales con base en las fuentes oficiales DIAN, ProColombia y la Federación Nacional de Departamentos.

---

## 4. Entradas — Zonas francas (21)

### 4.1. Zonas Francas Permanentes (ZFP) — 15

| Código | Nombre | Ubicación |
|---|---|---|
| `ZF-BAQ` | Zona Franca Permanente de Barranquilla | Barranquilla (`08001`) |
| `ZF-CTG` | Zona Franca Permanente de Cartagena | Cartagena (`13001`) |
| `ZF-BOG-FNT` | Zona Franca de Bogotá (Fontibón) | Bogotá D.C. (`11001`) |
| `ZF-PAL` | Zona Franca del Pacífico (Palmaseca) | Palmira (`76520`) |
| `ZF-RIO` | Zona Franca de Rionegro | Rionegro (`05615`) |
| `ZF-CCT` | Zona Franca de Cúcuta | Cúcuta (`54001`) |
| `ZF-SMR` | Zona Franca de Santa Marta | Santa Marta (`47001`) |
| `ZF-LBR` | Zona Franca de La Cayena (Barranquilla) | Barranquilla (`08001`) |
| `ZF-TAYRONA` | Zona Franca Tayrona | Santa Marta (`47001`) |
| `ZF-INTEXZONA` | Zona Franca Intexzona | Bello (`05088`) |
| `ZF-LATINOAMERICANA` | Zona Franca Latinoamericana | Soacha (`25754`) |
| `ZF-METROPOLITANA` | Zona Franca Metropolitana | Funza (`25286`) |
| `ZF-OCCIDENTE` | Zona Franca de Occidente | Funza (`25286`) |
| `ZF-TOCANCIPA` | Zona Franca de Tocancipá | Tocancipá (`25817`) |
| `ZF-BAYPORT` | Zona Franca BayPort de las Américas | Cartagena (`13001`) |

### 4.2. Zonas Francas Permanentes Especiales (ZFPE) — 6

| Código | Nombre | Ubicación |
|---|---|---|
| `ZFPE-ECOPETROL-RBC` | Refinería de Cartagena (Reficar) | Cartagena (`13001`) |
| `ZFPE-ARGOS` | Cementos Argos | Cartagena (`13001`) |
| `ZFPE-PRODECO` | Prodeco | Ciénaga (`47189`) |
| `ZFPE-DRUMMOND` | Drummond | Ciénaga (`47189`) |
| `ZFPE-PUERTO-BAHIA` | Puerto Bahía | Cartagena (`13001`) |
| `ZFPE-PACIFIC-RUBIALES` | Pacific Rubiales | Villavicencio (`50001`) |

---

## 5. Entradas — Monopolios departamentales (16)

### 5.1. Monopolio de licores destilados — 11

| Código | Departamento | Industria licorera oficial |
|---|---|---|
| `MON-LIC-ANT` | Antioquia (`05`) | Fábrica de Licores y Alcoholes de Antioquia (FLA) |
| `MON-LIC-CUN` | Cundinamarca (`25`) | Empresa de Licores de Cundinamarca (ELC) |
| `MON-LIC-VAL` | Valle del Cauca (`76`) | Industria de Licores del Valle (ILV) |
| `MON-LIC-CAL` | Caldas (`17`) | Industria Licorera de Caldas (ILC) |
| `MON-LIC-TOL` | Tolima (`73`) | Industria Licorera del Tolima (ILT) |
| `MON-LIC-BOY` | Boyacá (`15`) | Industria Licorera de Boyacá (ILB) |
| `MON-LIC-CAU` | Cauca (`19`) | Industria Licorera del Cauca |
| `MON-LIC-NAR` | Nariño (`52`) | Empresa Licorera de Nariño |
| `MON-LIC-ATL` | Atlántico (`08`) | Industria de Licores del Atlántico |
| `MON-LIC-QUI` | Quindío (`63`) | Industria Licorera del Quindío |
| `MON-LIC-BOL` | Bolívar (`13`) | Industria Licorera de Bolívar |

### 5.2. Monopolio de juegos de azar — 5

| Código | Departamento | Lotería |
|---|---|---|
| `MON-JUE-CUN` | Cundinamarca (`25`) | Lotería de Cundinamarca |
| `MON-JUE-BOG` | Bogotá D.C. (`11`) | Lotería de Bogotá |
| `MON-JUE-BOY` | Boyacá (`15`) | Lotería de Boyacá |
| `MON-JUE-ANT` | Antioquia (`05`) | Lotería de Medellín |
| `MON-JUE-VAL` | Valle del Cauca (`76`) | Lotería del Valle |

---

## 6. Entradas — Puerto Libre empresarial (1)

| Código | Nombre | Autoridad | Ubicación |
|---|---|---|---|
| `PL-EMP-SAI` | Inscripción empresarial Régimen Puerto Libre San Andrés | DIAN — Cámara de Comercio de San Andrés | San Andrés (`88001`) |

---

## 7. Notas operativas

### 7.1. Frontera con `JurisdiccionFiscal`

Los regímenes de este catálogo son **empresariales** — aplican porque la empresa está inscrita en un registro oficial. Los regímenes **territoriales** (Puerto Libre San Andrés en sentido amplio) viven en `JurisdiccionFiscal` con `tipoRegimen`. Una empresa **inscrita en `PL-EMP-SAI`** con sede física en San Andrés (jurisdicción `88001` con `tipoRegimen: puerto-libre`) activa **ambos** tratamientos — el territorial (sin IVA por el hecho económico) y el empresarial (sin renta sobre las utilidades).

### 7.2. Monopolios licores vs. impuesto al consumo

La Ley 1816/2016 permite a cada departamento elegir, vía decisión de la Asamblea, entre:

1. **Régimen de monopolio:** el departamento opera directamente la producción/comercialización de licores destilados a través de su industria licorera oficial; recibe una **participación** en lugar del impuesto al consumo.
2. **Régimen de impuesto al consumo:** los productores privados pagan el impuesto al consumo al departamento.

Los dos son excluyentes. Las empresas privadas que comercializan licores destilados en un departamento bajo régimen de monopolio deben estar **inscritas** ante la industria licorera del departamento o contar con autorización.

### 7.3. Códigos con prefijo semántico

- `ZF-{abreviación}`: zonas francas permanentes (ej: `ZF-BAQ` para Barranquilla).
- `ZFPE-{empresa}`: zonas francas permanentes especiales (ej: `ZFPE-ECOPETROL-RBC` para refinería Reficar).
- `MON-LIC-{dpto3}`: monopolios de licores por departamento.
- `MON-JUE-{dpto3}`: monopolios de juegos por departamento.
- `PL-EMP-{sigla}`: inscripciones empresariales en regímenes territoriales (Puerto Libre).

Los códigos no son los oficiales DIAN — son **códigos internos del catálogo** asignados para esta precarga. Si los consultores indican que deben usarse los códigos oficiales DIAN, se renombran.

### 7.4. Tipos `zona-economica-especial` y `regimen-especial-decreto` no aplican a CO

Estos tipos del enum (`[D13]`) están definidos pero NO tienen entradas precargadas en F1 para Colombia. Se reservan para entradas personalizadas (origen `personalizado`) cuando el cliente ERP requiera modelar regímenes empresariales otorgados por decreto individual no incluidos en la precarga estándar.

---

## 8. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 21 zonas francas (15 ZFP + 6 ZFPE) + 16 monopolios departamentales (11 licores + 5 juegos) + 1 Puerto Libre empresarial. Total 38 entradas (muestra significativa de ~155 oficiales). |

---

## 9. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales**:

1. **Lista completa de 121 zonas francas DIAN:** ¿Solicitamos al equipo el listado oficial actualizado o usamos la muestra de 21 como suficiente para piloto F1?
2. **Códigos internos vs. códigos oficiales DIAN:** ¿Los códigos `ZF-BAQ`, `ZFPE-ECOPETROL-RBC` son adecuados, o debemos usar los códigos oficiales asignados por DIAN en sus resoluciones de habilitación?
3. **Monopolios departamentales — completitud:** ¿Cuáles son los 33 monopolios (licores + juegos) actualmente vigentes? La precarga incluye 16; faltan al menos 17 por confirmar.
4. **Lotería de Bogotá vs Lotería de Cundinamarca:** ambas operan parcialmente en territorio compartido. ¿Cómo distinguirlas para inscripción empresarial?
5. **¿El Puerto Libre tiene un código oficial DIAN para la inscripción empresarial,** o el código `PL-EMP-SAI` es adecuado?
6. **Zonas Francas Transitorias:** ¿Deben precargarse en F1 o solo permanentes/permanentes especiales?
7. **`subtipo` adicional:** ¿Necesitamos un `subtipo` adicional como `agroindustrial`, `servicios`, `salud` para clasificar internamente las zonas francas? El modelo permite agregarlo si el equipo fiscal lo considera útil.
