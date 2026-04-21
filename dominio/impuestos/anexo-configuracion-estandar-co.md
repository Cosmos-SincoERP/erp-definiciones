# Anexo: Configuración estándar — Colombia (CO)

## Propósito

Contenido fiscal estándar que el producto provee precargado para Colombia. Corresponde a los datos iniciales (seeds) que se cargan como streams de eventos de configuración al iniciar operación — los mismos eventos que el modelo define para cada agregado.

El contenido se organiza siguiendo la estructura de los agregados del modelo de dominio:

| Agregado | Qué contiene en este anexo |
|----------|---------------------------|
| CatalogoTributario | Tributos, clasificaciones tributarias, tratamientos, reglas de localización |
| TarifaTributaria | Tarifas conocidas por tributo y jurisdicción |
| CondicionDeAplicacion | Reglas que modifican la aplicación según perfiles tributarios |
| CatalogoDeAtributosFiscales | Atributos fiscales requeridos |
| FormatoFiscal | Formatos de entregables por autoridad fiscal |

**Fuentes:** `fuentes/Definiciones de tributos.xlsx`, `fuentes/formatos-y-entrega-reportes-fiscales.md`, Estatuto Tributario colombiano.

**Nota sobre tarifas:** Las tarifas específicas (porcentajes, cuantías mínimas en UVT) son las vigentes al momento de la elaboración de este documento. El contenido estándar del producto se actualiza cuando la normativa cambia — las tarifas aquí documentadas son una referencia inicial, no una fuente normativa.

---

## 1. Tributos — CatalogoTributario

### Tributos directos

| Código | Nombre | Naturaleza | Base | Nivel jurisdiccional | Factor de tarifa | Tributo padre | Definición |
|--------|--------|:----------:|------|:--------------------:|-----------------|:-------------:|------------|
| IVA | Impuesto al Valor Agregado | Aditivo | Ingreso | Nacional | `clasificacion` | — | Impuesto indirecto nacional sobre la prestación de servicios y venta e importación de bienes. |
| INC | Impuesto Nacional al Consumo | Aditivo | Ingreso | Nacional | `clasificacion` | — | Impuesto indirecto que grava los sectores de vehículos, telecomunicaciones, comidas y bebidas. |
| ICA | Impuesto de Industria y Comercio | Aditivo | Ingreso | Municipal | `actividadEconomica` | — | Impuesto generado a toda actividad comercial, de servicios o industrial, realizada de forma ocasional o permanente. Tarifa a nivel municipal. |
| RETEFUENTE | Retención en la Fuente | Sustractivo | Ingreso | Nacional | `conceptoPago` | — | Mecanismo anticipado de recaudación del impuesto a la renta y complementarios, con el fin de garantizar y agilizar el pago. |
| RIVA | Retención sobre el IVA | Sustractivo | IVA (padre) | Nacional | `porcentajeDePadre` | IVA | Recaudación anticipada del impuesto a las ventas efectuada por un agente retenedor. |
| RICA | Retención sobre el ICA | Sustractivo | Ingreso | Municipal | `actividadEconomica` | — | Retención a título del impuesto de industria y comercio. Tarifa a nivel municipal. |
| SOBRETASA_BOMBERIL | Retención Sobretasa Bomberil | Sustractivo | RICA (padre) | Municipal | `porcentajeDePadre` | RICA | Tributo municipal asignado por cada municipio. Requisito: el tercero debe ser responsable del ICA. |

### Tributos de provisión (autoretenciones)

| Código | Nombre | Naturaleza | Base | Nivel jurisdiccional | Factor de tarifa | Tributo padre | Definición |
|--------|--------|:----------:|------|:--------------------:|-----------------|:-------------:|------------|
| AUTO_RENTA | Autorretención de Renta | Sustractivo | Ingreso | Nacional | `fija` | — | Autorretención a título de renta. Aplica cuando la empresa es autorretenedora. |
| AUTO_RETEFUENTE | Autorretención en la Fuente | Sustractivo | Ingreso | Nacional | `conceptoPago` | — | Autorretención en la fuente. Aplica cuando la empresa es autorretenedora de renta. |
| AUTO_RIVA | Autorretención de IVA | Sustractivo | IVA (padre) | Nacional | `porcentajeDePadre` | IVA | Autorretención de IVA. Aplica cuando la empresa es agente retenedor de IVA. |
| AUTO_RICA | Autorretención de ICA | Sustractivo | Ingreso | Municipal | `actividadEconomica` | — | Autorretención de ICA. Aplica cuando la empresa es autorretenedora de ICA. |

**Total:** 11 tributos (7 directos + 4 de provisión).

### Clasificaciones tributarias

| Código | Nombre | Tributos que aplican | Notas |
|--------|--------|---------------------|-------|
| GRAV_19 | Gravados 19% | IVA, RETEFUENTE, RIVA, ICA, RICA | Tarifa general — bienes y servicios gravados al 19%. |
| GRAV_5 | Gravados 5% | IVA, RETEFUENTE, RIVA, ICA, RICA | Tarifa reducida — bienes y servicios de la canasta básica y otros. |
| EXCLUIDO | Excluidos de IVA | RETEFUENTE, ICA, RICA | No generan IVA. Pueden tener retenciones. |
| EXENTO | Exentos de IVA | IVA (tarifa 0%), RETEFUENTE, ICA, RICA | IVA con tarifa 0% — derecho a descontar IVA pagado. |
| INC_8 | Gravados INC 8% | INC | Telecomunicaciones, comidas y bebidas. |
| NO_GRAVADO | No sujeto a impuestos | — | Conceptos que no generan ningún tributo. |

### Reglas de localización

| Tributo | Clasificación | Rol fiscalmente relevante | Fallback | Notas |
|---------|--------------|--------------------------|----------|-------|
| IVA | * (todas) | `sedeEmisora` | — | Siempre nacional. La sede de la empresa determina el país. |
| INC | * (todas) | `sedeEmisora` | — | Siempre nacional. |
| RETEFUENTE | * (todas) | `sedeEmisora` | — | Siempre nacional. |
| ICA | * (todas) | `lugarEjecucion` | `sedeEmisora` | Municipal. Donde se presta el servicio o se entrega el bien. |
| RIVA | * (todas) | `sedeEmisora` | — | Nacional. Hereda del padre (IVA). |
| RICA | * (todas) | `lugarEjecucion` | `sedeEmisora` | Municipal. Misma resolución que ICA. |
| SOBRETASA_BOMBERIL | * (todas) | `lugarEjecucion` | `sedeEmisora` | Municipal. Hereda del padre (RICA). |
| AUTO_RENTA | * (todas) | `sedeEmisora` | — | Nacional. |
| AUTO_RETEFUENTE | * (todas) | `sedeEmisora` | — | Nacional. |
| AUTO_RIVA | * (todas) | `sedeEmisora` | — | Nacional. |
| AUTO_RICA | * (todas) | `lugarEjecucion` | `sedeEmisora` | Municipal. |

---

## 2. Tarifas — TarifaTributaria

### IVA (nacional) — `tarifa-CO-IVA`

| Factor (clasificación) | Tarifa | Tipo | Cuantía mínima | Vigencia desde |
|------------------------|:------:|:----:|:--------------:|:--------------:|
| GRAV_19 | 19% | Porcentaje | — | 2017-01-01 |
| GRAV_5 | 5% | Porcentaje | — | 2017-01-01 |
| EXENTO | 0% | Porcentaje | — | 2017-01-01 |

### INC (nacional) — `tarifa-CO-INC`

| Factor (clasificación) | Tarifa | Tipo | Cuantía mínima | Vigencia desde |
|------------------------|:------:|:----:|:--------------:|:--------------:|
| INC_8 | 8% | Porcentaje | — | 2017-01-01 |

### RETEFUENTE (nacional) — `tarifa-CO-RETEFUENTE`

| Factor (concepto de pago) | Tarifa | Tipo | Cuantía mínima | Vigencia desde |
|--------------------------|:------:|:----:|:--------------:|:--------------:|
| Compras generales | 2.5% | Porcentaje | 27 UVT | 2017-01-01 |
| Servicios generales | 4% | Porcentaje | 4 UVT | 2017-01-01 |
| Servicios generales declarantes | 2% | Porcentaje | 4 UVT | 2017-01-01 |
| Honorarios | 11% | Porcentaje | — | 2017-01-01 |
| Honorarios declarantes | 10% | Porcentaje | — | 2017-01-01 |
| Arrendamientos bienes inmuebles | 3.5% | Porcentaje | 27 UVT | 2017-01-01 |
| Arrendamientos bienes muebles | 4% | Porcentaje | — | 2017-01-01 |

> **Nota:** La tabla anterior es un extracto representativo. El catálogo completo de conceptos de pago para RETEFUENTE incluye ~50 conceptos definidos por la DIAN. Se carga como parte del contenido estándar del producto.

### RIVA (nacional) — `tarifa-CO-RIVA`

| Factor | Tarifa | Tipo | Notas |
|--------|:------:|:----:|-------|
| — (porcentaje del padre) | 15% | Porcentaje del IVA | Tarifa general de retención de IVA. |

### ICA y RICA (municipal) — ejemplo Bogotá — `tarifa-CO-BOG-ICA`

| Factor (actividad económica CIIU) | Tarifa | Tipo | Cuantía mínima | Vigencia desde |
|-----------------------------------|:------:|:----:|:--------------:|:--------------:|
| 4711 – Comercio al por menor | 11.04‰ | Por mil | Sí (varía por municipio) | 2020-01-01 |
| 6201 – Desarrollo de software | 9.66‰ | Por mil | Sí (varía por municipio) | 2020-01-01 |
| 7010 – Actividades de sedes principales | 11.04‰ | Por mil | Sí (varía por municipio) | 2020-01-01 |

> **Nota:** Cada municipio tiene su propia tabla de tarifas de ICA/RICA por actividad económica. El contenido estándar incluye las principales ciudades donde opera el cliente. La tarifa de RICA es la misma tarifa del ICA aplicada como retención.

### SOBRETASA_BOMBERIL — `tarifa-CO-{ciudad}-SOBRETASA_BOMBERIL`

| Factor | Tarifa | Tipo | Notas |
|--------|:------:|:----:|-------|
| — (porcentaje del padre) | Varía por municipio | Porcentaje del RICA | Cada municipio define si aplica y con qué porcentaje. |

### AUTO_RENTA (nacional) — `tarifa-CO-AUTO_RENTA`

| Factor | Tarifa | Tipo | Cuantía mínima | Vigencia desde |
|--------|:------:|:----:|:--------------:|:--------------:|
| — (fija) | 0.55% | Porcentaje | — | 2017-01-01 |

---

## 3. Condiciones de aplicación — CondicionDeAplicacion

Las condiciones traducen las reglas del Excel fuente (`CO - Aplicacion`) al modelo del dominio. Cada caso del Excel se convierte en una o más condiciones evaluables por el motor.

### RETEFUENTE — 8 casos

| # | Entidad evaluada | Atributo evaluado | Valor esperado | Tributo afectado | Efecto | Notas |
|---|:----------------:|-------------------|:--------------:|:----------------:|:------:|-------|
| 1 | Emisora | perteneceRegimenSimple | `true` | RETEFUENTE | `noAplicar` | Régimen simple no practica retención. |
| 2 | Emisora | esExentoRetefuente | `true` | RETEFUENTE | `noAplicar` | Entidad exenta de retención. |
| 3 | Emisora | esAutorretenedora | `true` | RETEFUENTE | `noAplicar` → aplica AUTO_RETEFUENTE | Cuando el emisor es autorretenedor, no se practica retención sino autorretención. |
| 4 | Contraparte | perteneceRegimenIVA | `false` | RETEFUENTE | `noAplicar` | Caso 2 del Excel: contraparte no pertenece al régimen de IVA. |
| 5 | Emisora + Contraparte | esGranContribuyente (E) + esGranContribuyente (C) + esAutorretenedora (C) | `true` + `true` + `true` | RETEFUENTE | `aplicar` | Caso 5: ambos gran contribuyente, contraparte autorretenedora. |
| 6 | Emisora + Contraparte | esGranContribuyente (E) + esGranContribuyente (C) + esAutorretenedora (C) | `true` + `true` + `false` | RETEFUENTE | `aplicar` | Caso 6: ambos gran contribuyente, contraparte NO autorretenedora. |
| 7 | Emisora + Contraparte | esGranContribuyente (E) + esGranContribuyente (C) | `true` + `false` | RETEFUENTE | `aplicar` | Caso 7: emisor gran contribuyente, contraparte no. |
| 8 | — | — | — | RETEFUENTE | `aplicar` (default) | Si ninguna condición de exclusión se cumple y la base supera la cuantía mínima. |

> **Nota sobre la cuantía mínima:** La validación `Base mínima > al configurado` no es una condición de aplicación sino una regla de la `EntradaDeTarifa` (atributo `cuantíaMínima`). El motor la evalúa después de resolver la tarifa.

### RIVA — 3 casos

| # | Entidad evaluada | Atributo evaluado | Valor esperado | Tributo afectado | Efecto | Notas |
|---|:----------------:|-------------------|:--------------:|:----------------:|:------:|-------|
| 1 | Emisora + Contraparte | perteneceRegimenIVA (E) + esAgenteRetenedorIVA (C) | `true` + `true` | RIVA | `aplicar` | Contraparte es agente retenedor de IVA. |
| 2 | Contraparte | esAgenteRetenedorIVA | `false` | RIVA | `noAplicar` | Contraparte NO es agente retenedor. |
| 3 | Emisora | esAgenteRetenedorIVA | `true` | RIVA → AUTO_RIVA | `reverseCharge` | Emisora es agente retenedor → se autoliquida como AUTO_RIVA. |

### RICA — 3 casos

| # | Entidad evaluada | Atributo evaluado | Valor esperado | Tributo afectado | Efecto | Notas |
|---|:----------------:|-------------------|:--------------:|:----------------:|:------:|-------|
| 1 | Emisora + Contraparte | perteneceRegimenSimple (E) + (perteneceRegimenIVA OR esAgenteRetenedorICA OR esGranContribuyenteICA OR esAutorretenedorICA) (C) | `false` + `true` (cualquiera) | RICA | `aplicar` | Caso estándar por calidades tributarias. Requiere ciudad. |
| 2 | — | — | — | RICA | `aplicar` (sin calidades) | Caso 2: check "no calcular por calidades tributarias" habilitado. Solo valida ciudad del centro de costo. |
| 3 | Emisora | esGranContribuyenteBogota | `true` | RICA | `noAplicar` | Si el emisor es gran contribuyente de Bogotá y la negociación es en Bogotá, no aplica RICA. |

### IVA — 1 caso

| # | Entidad evaluada | Atributo evaluado | Valor esperado | Tributo afectado | Efecto |
|---|:----------------:|-------------------|:--------------:|:----------------:|:------:|
| 1 | Emisora | perteneceRegimenIVA | `true` | IVA | `aplicar` |

> IVA aplica si el emisor pertenece al régimen de IVA. No depende de calidades del adquiriente.

### INC, ICA — sin condiciones por perfil

INC e ICA no evalúan calidades tributarias del emisor ni del adquiriente. Su aplicación depende únicamente de la clasificación tributaria (INC) y de la ciudad + actividad económica (ICA).

### SOBRETASA_BOMBERIL — 1 caso

| # | Entidad evaluada | Atributo evaluado | Valor esperado | Tributo afectado | Efecto | Notas |
|---|:----------------:|-------------------|:--------------:|:----------------:|:------:|-------|
| 1 | — | — | — | SOBRETASA_BOMBERIL | `aplicar` | Aplica si existe RICA y si el adquiriente está configurado en el concepto de sobretasa bomberil (configuración por tercero específico). |

### Autoretenciones

| Tributo | Entidad evaluada | Atributo evaluado | Valor esperado | Efecto | Notas |
|---------|:----------------:|-------------------|:--------------:|:------:|-------|
| AUTO_RETEFUENTE | Emisora | esAutorretenedora | `true` | `aplicar` | Se activa en lugar de RETEFUENTE cuando el emisor es autorretenedor. |
| AUTO_RIVA | Emisora | esAgenteRetenedorIVA | `true` | `aplicar` | Se activa en lugar de RIVA cuando el emisor es agente retenedor de IVA. |
| AUTO_RICA | Emisora | esAutorretenedorICA | `true` | `aplicar` | Se activa en lugar de RICA. Requiere ciudad. |
| AUTO_RENTA | Emisora | esAutorretenedorRenta | `true` | `aplicar` | Autorretención a título de renta. |

---

## 4. Atributos fiscales — CatalogoDeAtributosFiscales

| Nombre | Tipo | Valores válidos | Requerido | Vigencia definición | Notas |
|--------|:----:|----------------|:---------:|:-------------------:|-------|
| regimenTributario | Enum | Ordinario, Simple, Especial, No responsable | Sí | 2023-01-01 → ∞ | Determina obligaciones fiscales generales. |
| perteneceRegimenIVA | Boolean | — | Sí | 2017-01-01 → ∞ | Determina si genera IVA y si se le practica retención. |
| esGranContribuyente | Boolean | — | Sí | 2017-01-01 → ∞ | Calificación DIAN. Afecta RETEFUENTE. |
| esAutorretenedora | Boolean | — | Sí | 2017-01-01 → ∞ | Calificación DIAN. Activa AUTO_RETEFUENTE en lugar de RETEFUENTE. |
| esAgenteRetenedorIVA | Boolean | — | Sí | 2017-01-01 → ∞ | Activa RIVA o AUTO_RIVA. |
| esExentoRetefuente | Boolean | — | Sí | 2017-01-01 → ∞ | Entidad exenta de retención en la fuente. |
| perteneceRegimenSimple | Boolean | — | Sí | 2019-01-01 → ∞ | Régimen Simple de Tributación. No practica retenciones. |
| esAutorretenedorRenta | Boolean | — | Sí | 2017-01-01 → ∞ | Activa AUTO_RENTA. |
| esAgenteRetenedorICA | Boolean | — | No | 2017-01-01 → ∞ | Agente retenedor de ICA en algún municipio. |
| esAutorretenedorICA | Boolean | — | No | 2017-01-01 → ∞ | Autorretenedor de ICA. Activa AUTO_RICA. |
| esGranContribuyenteICA | Boolean | — | No | 2017-01-01 → ∞ | Gran contribuyente de ICA (Bogotá). |
| actividadEconomica | String | Código CIIU | Sí | 2017-01-01 → ∞ | Código de actividad económica. Usado como factor de tarifa para ICA/RICA. |
| tipoPersona | Enum | Natural, Jurídica | Sí | 2017-01-01 → ∞ | Afecta cuentas contables de autoretenciones. |

---

## 5. Formatos fiscales — FormatoFiscal

### Reportes de información fiscal (autoridad: DIAN)

| Código formato | Nombre | Tipo entregable | Periodicidad | Formato salida | Notas |
|---------------|--------|:---------------:|:------------:|:--------------:|-------|
| F-1001 | Pagos o abonos en cuenta y retenciones practicadas | Reporte | Anual | XML, Excel (prevalidador) | Exógena — principal formato de retenciones. |
| F-1003 | Retenciones en la fuente practicadas | Reporte | Anual | XML, Excel (prevalidador) | Exógena — detalle de retenciones. |
| F-1005 | IVA descontable | Reporte | Anual | XML, Excel (prevalidador) | Exógena — IVA en compras. |
| F-1006 | IVA generado | Reporte | Anual | XML, Excel (prevalidador) | Exógena — IVA en ventas. |
| F-1007 | Ingresos recibidos | Reporte | Anual | XML, Excel (prevalidador) | Exógena — ingresos por tercero. |
| F-1647 | Ingresos recibidos para terceros | Reporte | Anual | XML, Excel (prevalidador) | Exógena — ingresos en nombre de terceros. |
| F-2276 | Información de rentas de trabajo y pensiones | Reporte | Anual | XML, Excel (prevalidador) | Exógena — certificados de renta. |

> **Nota:** La resolución 000233 de 2026 introduce nuevos formatos (F-2856 activos digitales, F-2854 ingresos del exterior) y cambios en formatos 1001 y 1647.

### Reportes municipales

| Formato | Nombre | Tipo entregable | Periodicidad | Formato salida | Notas |
|---------|--------|:---------------:|:------------:|:--------------:|-------|
| Reporte ICA | Retenciones de ICA practicadas por municipio | Reporte | Bimestral/Mensual | Excel/PDF | Varía por municipio. ~17 ciudades principales. |

### Certificados tributarios

| Formato | Nombre | Tipo entregable | Periodicidad | Formato salida | Notas |
|---------|--------|:---------------:|:------------:|:--------------:|-------|
| Formulario 220 | Certificado de retención en la fuente | Certificado | Anual | PDF | Plazo: antes del 31 de marzo del año siguiente. Entrega individual y masiva. |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: 11 tributos, 6 clasificaciones, condiciones de aplicación completas, 13 atributos fiscales, formatos DIAN y municipales. |
