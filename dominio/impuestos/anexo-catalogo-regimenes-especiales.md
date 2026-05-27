# Anexo — Catálogo de regímenes especiales empresariales

> **Propósito:** Documentar el origen, las fuentes normativas y la metodología de construcción del catálogo de tipos del agregado `CatalogoDeRegimenesEspeciales` (Sección 3.8 del modelo). Diferenciar los tipos certificados para F1 (cargados con datos operativos de CO, DO, PA) de los tipos identificados como candidatos futuros para F2 (estructura conceptual lista, precarga pendiente al abordar otros países).
>
> **Aplica a:** Modelo de dominio `dominio/impuestos/modelo-dominio.md`, decisión `[D13]`.
> **Referencias:** `anexo-configuracion-estandar-co.md`, `anexo-configuracion-estandar-do.md`, `anexo-configuracion-estandar-pa.md`, Cambio 5 (Carga de catálogos certificados para producción).
> **Audiencia:** Equipo de desarrollo, administradores fiscales, equipo de producto al expandir a nuevos países.

---

## 1. Origen de la investigación

El catálogo de tipos surgió a partir de una investigación documentada en sesión iterativa sobre **jurisdicciones fiscales no-administrativas y regímenes especiales empresariales** realizada el 2026-05-04. La investigación se motivó tras detectar que los regímenes especiales (zonas francas, polos económicos, áreas de libre comercio, etc.) no estaban contemplados en el modelo inicial y representaban casos reales con volumen significativo:

- **Colombia:** 121 zonas francas activas (DIAN, corte 31-mar-2022).
- **República Dominicana:** 75 parques de zona franca, ~714 empresas inscritas (CNZFE).
- **Panamá:** Zona Libre de Colón (~2.000 empresas), AEEPP, Ciudad del Saber.
- **México:** 43 municipios de Frontera Norte + 24 de Frontera Sur en padrones SAT.
- **Brasil:** Zona Franca de Manaus + 7 Áreas de Livre Comércio (SUFRAMA).
- **Estados Unidos:** 574 tribus federalmente reconocidas con soberanía tributaria.

La investigación reveló que estos regímenes tienen **dos naturalezas distintas** que requieren modelado separado:

1. **Régimen TERRITORIAL** — una región completa con tributación diferenciada (modelado como `JurisdiccionFiscal` con `tipo: regimen-especial-territorial` y `tipoRegimen` categórico — Cambio 2).
2. **Régimen EMPRESARIAL** — una empresa específica inscrita en un registro oficial para acceder a beneficios fiscales (modelado como `RegimenEspecial` en `CatalogoDeRegimenesEspeciales` referenciado desde el `PerfilTributario` — este Cambio 3).

Algunos casos requieren **AMBOS** simultáneamente. Ejemplo: en México, una empresa goza del IVA 8% si (a) opera en un municipio fronterizo (territorial) Y (b) está inscrita en el padrón SAT (empresarial).

---

## 2. Metodología de derivación de tipos

A partir de los casos identificados, se sintetizó una **taxonomía** que agrupa regímenes por mecanismo fiscal común. Cada `tipo` cubre múltiples regímenes específicos de distintos países que comparten naturaleza fiscal similar:

- **No es un estándar internacional preexistente.** Es una clasificación propia derivada del análisis de los casos reales.
- **No es exhaustiva.** Cubre los casos identificados en la investigación; nuevos regímenes en otros países pueden requerir ampliar el enum.
- **Es evolutiva.** Cuando se aborde un país nuevo o aparezca un régimen no contemplado, se agrega el tipo correspondiente al enum del modelo (extensión de bajo costo — solo amplía valores válidos sin cambios estructurales).

---

## 3. Catálogo de tipos — Estado F1 (certificado en el modelo)

Los siguientes tipos están **declarados en el enum del modelo F1** (Sección 3.8) y respaldados por fuentes normativas para los países del alcance (CO, DO, PA). La precarga de instancias concretas (datos operativos) se gestiona en **Cambio 5 — Carga de catálogos certificados para producción**.

| Tipo | Descripción | Países (F1) | Autoridad / Catálogo oficial | Fuente normativa |
|---|---|---|---|---|
| `zona-franca` | Empresa autorizada para operar en zona franca con régimen tributario diferenciado (renta reducida, exención IVA en bienes/servicios intra-zona, exención aranceles importación de insumos) | **CO** | DIAN — código de zona franca asignado | Decreto 2147/2016 (CO). Ley 8-90 (DO). |
| | | **DO** | CNZFE — código de parque + código de empresa | |
| `puerto-libre-empresa` | Empresa inscrita en régimen empresarial archipelágico (caso empresarial del régimen territorial Puerto Libre, cuando aplique condición empresarial específica además de la ubicación territorial) | **CO** | DIAN — registro empresarial archipiélago | Constitución art. 310. Ley 47/1993. |
| `monopolio-sectorial` | Empresa con monopolio departamental de comercialización de bienes sujetos a régimen rentístico departamental (licores destilados, juegos de azar, loterías) | **CO** | Asambleas departamentales — concesión/autorización | Ley 1816/2016 (licores). Régimen departamental de juegos. |
| `zona-economica-especial` | Empresa autorizada para operar en zona económica especial / área económica especial / centro internacional de negocios con régimen tributario propio | **PA** | Autoridad de la zona (ZLC, AEEPP, Fundación Ciudad del Saber) | Ley 18/1948 ZLC (PA). Ley 41/2004 AEEPP (PA). Decreto Ciudad del Saber. |
| `regimen-especial-decreto` | Empresa con régimen tributario propio otorgado por decreto/resolución individual (no genérico). Catch-all para casos no cubiertos por los tipos anteriores. | **Genérico** | Varía por jurisdicción y caso | Decretos individuales por país. |

**Total: 5 tipos en el enum F1.**

---

## 4. Catálogo de tipos — Candidatos futuros (F2, fuera del enum del modelo F1)

Los siguientes tipos están **identificados conceptualmente** pero NO en el enum del modelo F1. Cuando se aborde el país correspondiente, se agrega el tipo al enum (cambio quirúrgico) y se precarga el catálogo en una fase de Cambio 5 extendida.

| Tipo candidato | Descripción | Países objetivo | Autoridad / Catálogo | Fuente normativa |
|---|---|---|---|---|
| `polo-economico` | Empresa autorizada para operar en polo de desarrollo económico con beneficios fiscales (créditos ISR, exenciones IVA temporales, deducción inmediata) | **MX** | SAT — constancia PODEBI | Decreto Polos Istmo de Tehuantepec 2023, ref. 2026. |
| | | **BR** | Estado — incentivos por polo industrial | Marco de incentivos por estado. |
| `inscripcion-region-fronteriza` | Empresa inscrita en padrón/registro oficial de región fronteriza para acceder a tasas reducidas (IVA, ISR) | **MX** | SAT — padrón frontera norte / padrón frontera sur | Decreto Frontera Norte 2019 (43 municipios). Decreto Frontera Sur 2020 (24 municipios). Renovación anual. |
| `area-libre-comercio` | Empresa inscrita en área de libre comercio con exención de IPI/PIS/Cofins | **BR** | SUFRAMA — cadastro | Marco regulatório ZFM/ALCs SUFRAMA. |
| `regimen-archipielago-empresa` | Empresa en régimen archipelágico empresarial (cuando hay registro empresarial específico además de la ubicación territorial) | **EC** y otros | Consejo de Gobierno respectivo | LOREG (Galápagos). Regímenes similares. |
| `status-indigena` | Status tributario asociado a comunidades/individuos indígenas con soberanía tributaria. Aplicable a transacciones en reservas o entre miembros tribales. | **US** | Tribu federalmente reconocida (574 tribus) | Indian Act sección 87 (CA). Tribal sales tax codes (US). |
| | | **CA** | First Nation (~630 First Nations) | First Nations Goods and Services Tax (FNGST). |

**Total: 5 tipos candidatos futuros.**

> **Política de extensión:** cuando se decida abordar un país de la columna "Países objetivo", el equipo de modelado realiza:
> 1. Validación normativa actualizada de la fuente.
> 2. Adición del tipo al enum de `RegimenEspecial.tipo` en el modelo.
> 3. Adición de la fila correspondiente a la tabla F1 de este anexo (movimiento de candidato a certificado).
> 4. Precarga del catálogo en el sub-dominio (Cambio 5 ampliado).
> 5. Verificación de invariantes I16 e I17 contra la nueva precarga.

---

## 5. Frontera con otros modelos del dominio

### 5.1. `JurisdiccionFiscal` (Cambio 2) — Régimen TERRITORIAL

Las regiones territoriales con régimen fiscal propio (San Andrés Puerto Libre, Galápagos LOREG, Frontera Norte/Sur MX, ALCs Brasil) **NO se modelan aquí**. Se modelan en `JurisdiccionFiscal` con `tipo: regimen-especial-territorial` y `tipoRegimen` categórico.

**Caso de cruce (algunos países requieren AMBOS):** en México, una transacción goza de IVA reducido del 8% si la jurisdicción `lugarEjecucion` tiene `tipoRegimen: frontera-iva-reducido` (territorial — Cambio 2) Y la entidad vendedora tiene atributo `inscripcionRegionFronteriza` apuntando a un `RegimenEspecial` vigente del catálogo (empresarial — Cambio 3). La condición fiscal cruzaría ambas dimensiones.

### 5.2. `PerfilTributario` (Cambio 1, ampliado en Cambio 2)

El régimen empresarial se materializa como **atributo del perfil**: una `DefinicionAtributo` con `catalogoReferencia: "CatalogoDeRegimenesEspeciales"` define el atributo (ej: `regimenZonaFranca`, `inscripcionRegionFronteriza`). El `AtributoFiscal.valor` referencia `RegimenEspecial.codigo` (validado por invariante I16).

El motor consulta el perfil para evaluar condiciones que dependen del régimen empresarial vigente.

---

## 6. Fuentes consultadas

### Colombia
- DIAN — **Caracterización del régimen de zonas francas en Colombia**, Cuaderno de Trabajo, marzo 2022.
- **Decreto 2147 de 2016** (Régimen de zonas francas en Colombia) — Normograma DIAN.
- **Ley 47 de 1993** (Régimen especial del Archipiélago de San Andrés, Providencia y Santa Catalina).
- **Constitución Política de Colombia, Art. 310** (Régimen especial archipiélago).
- **Ley 1816 de 2016** (Monopolio rentístico departamental de licores).

### República Dominicana
- CNZFE — **Consejo Nacional de Zonas Francas de Exportación**, FAQs y registro oficial.
- **Ley 8-90** (Régimen de zonas francas industriales de RD), DGII.

### Panamá
- **Zona Libre de Colón** — Sitio oficial ZOLICOL y normativa: Ley 18 de 1948, Ley 8 de 2016, Ley 412 de 2023.
- **Área Económica Especial Panamá-Pacífico** — Ley 41 de 2004.
- **Ciudad del Saber** — Fundación Ciudad del Saber, marco normativo.

### México (F2)
- **PRODECON** — Decreto de estímulos fiscales Región Fronteriza Norte (2019, prorrogado 2024 y 2026).
- **VTZ.mx** — Estímulos Fiscales Frontera Norte y Sur (resumen normativo).
- **PWC México** — Decreto beneficios fiscales actividades Istmo de Tehuantepec (PODEBI).

### Brasil (F2)
- **SUFRAMA** — Marco Regulatório de Incentivos Fiscais ZFM ao ALCs.

### Estados Unidos (F2)
- **Congress.gov R47414** — 574 Federally Recognized Tribes.
- **Avalara** — Native Americans and Sales Taxes; Special Tax Jurisdictions.

### Canadá (F2)
- **Canada.ca** — GST/HST and First Nations; First Nations Goods Services Tax (FNGST).

---

## 7. Coordinación con Cambio 5 (Carga de catálogos certificados)

Los **datos operativos** (instancias concretas de `RegimenEspecial` con sus códigos oficiales) se cargan en el sub-dominio mediante el **Cambio 5 — Carga de catálogos certificados para producción**. Este anexo documenta la estructura conceptual y las fuentes; el Cambio 5 documenta los datos por país certificados por el equipo de negocio:

| País (F1) | Precarga F1 (Cambio 5) | Aproximado |
|---|---|---|
| Colombia | 121 zonas francas (códigos DIAN) + monopolios departamentales (~33 licores) + casos San Andrés empresarial | ~150 instancias |
| Rep. Dominicana | 75 parques de zona franca (códigos CNZFE) | ~75 instancias |
| Panamá | ZLC + AEEPP + Ciudad del Saber + zonas adicionales | ~10 instancias |

---

## 8. Política de evolución del catálogo

1. **Agregar un tipo:** se valida con investigación documentada (fuente normativa, casos reales, naturaleza fiscal). El tipo se agrega al enum del modelo (Sección 3.8) y a este anexo. Si el tipo cubre un país en F1, se mueve de "candidato futuro" a "certificado F1".

2. **Renombrar un tipo:** evaluar impacto en datos existentes. Si hay instancias precargadas con el nombre anterior, se requiere migración (evento `RegimenEspecialModificado` o reescritura del stream).

3. **Retirar un tipo:** no se elimina del enum si hay instancias históricas. Se marca como deprecado en este anexo y se documenta que no se usa para nuevos registros.

4. **Versionado:** este anexo se versiona junto con el modelo. Los cambios al catálogo se registran en la tabla de control de versiones al final del documento.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---|---|---|
| 1.0 | 2026-05-13 | Versión inicial. 5 tipos certificados para F1 (CO/DO/PA), 5 tipos candidatos futuros para F2 (MX/BR/US/CA). Fuentes normativas documentadas. |
