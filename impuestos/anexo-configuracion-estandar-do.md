# Anexo: Configuración estándar — República Dominicana (DO)

## Propósito

Contenido fiscal estándar que el producto provee precargado para República Dominicana. Corresponde a los datos iniciales (seeds) que se cargan como streams de eventos de configuración al iniciar operación — los mismos eventos que el modelo define para cada agregado.

El contenido se organiza siguiendo la estructura de los agregados del modelo de dominio:

| Agregado | Qué contiene en este anexo |
|----------|---------------------------|
| CatalogoTributario | Tributos, clasificaciones tributarias, tratamientos, reglas de localización |
| TarifaTributaria | Tarifas conocidas por tributo |
| CondicionDeAplicacion | Reglas que modifican la aplicación según perfiles tributarios |
| CatalogoDeAtributosFiscales | Atributos fiscales requeridos |
| FormatoFiscal | Formatos de entregables por autoridad fiscal |

**Fuentes:** `fuentes/Definiciones de tributos.xlsx`, `fuentes/formatos-y-entrega-reportes-fiscales.md`, DGII República Dominicana.

**Nota sobre tarifas:** Las tarifas específicas son las vigentes al momento de la elaboración de este documento. El contenido estándar del producto se actualiza cuando la normativa cambia.

---

## 1. Tributos — CatalogoTributario

| Código | Nombre | Naturaleza | Base | Nivel jurisdiccional | Factor de tarifa | Tributo padre | Definición |
|--------|--------|:----------:|------|:--------------------:|-----------------|:-------------:|------------|
| ITBIS | Impuesto a la Transferencia de Bienes Industrializados y Servicios | Aditivo | Ingreso | Nacional | `clasificacion` | — | Equivalente al IVA. Grava la transferencia de bienes industrializados, importación y prestación de servicios. |
| RITBIS | Retención del ITBIS | Sustractivo | ITBIS (padre) | Nacional | `porcentajeDePadre` | ITBIS | Retención anticipada del ITBIS. |
| ISC | Impuesto Selectivo al Consumo | Aditivo | Ingreso | Nacional | `clasificacion` | — | Impuesto que se aplica a ciertos productos como forma de desincentivar su consumo. |
| CDT | Contribución al Desarrollo de las Telecomunicaciones | Aditivo | Ingreso | Nacional | `fija` | — | Impuesto que ayuda al desarrollo de las telecomunicaciones. |
| PROPINA | Propina Legal | Aditivo | Ingreso | Nacional | `fija` | — | Impuesto de propina legal obligatorio en establecimientos de servicio. |

**Total:** 5 tributos (4 impuestos + 1 retención).

### Clasificaciones tributarias

| Código | Nombre | Tributos que aplican | Notas |
|--------|--------|---------------------|-------|
| GRAV_ITBIS_18 | Gravados ITBIS 18% | ITBIS, RITBIS | Tarifa general. |
| GRAV_ITBIS_16 | Gravados ITBIS 16% | ITBIS, RITBIS | Tarifa reducida. |
| EXENTO_ITBIS | Exentos de ITBIS | — | Bienes y servicios exentos. |
| ISC_APLICABLE | Sujeto a ISC | ISC | Bienes gravados con selectivo al consumo. |

### Reglas de localización

| Tributo | Rol fiscalmente relevante | Fallback | Notas |
|---------|--------------------------|----------|-------|
| ITBIS | `sedeEmisora` | — | Nacional. |
| RITBIS | `sedeEmisora` | — | Nacional. |
| ISC | `sedeEmisora` | — | Nacional. |
| CDT | `sedeEmisora` | — | Nacional. |
| PROPINA | `sedeEmisora` | — | Nacional. |

---

## 2. Tarifas — TarifaTributaria

### ITBIS — `tarifa-DO-ITBIS`

| Factor (clasificación) | Tarifa | Tipo | Vigencia desde |
|------------------------|:------:|:----:|:--------------:|
| GRAV_ITBIS_18 | 18% | Porcentaje | 2017-01-01 |
| GRAV_ITBIS_16 | 16% | Porcentaje | 2017-01-01 |

### RITBIS — `tarifa-DO-RITBIS`

| Factor | Tarifa | Tipo | Notas |
|--------|:------:|:----:|-------|
| — (porcentaje del padre) | 30% | Porcentaje del ITBIS | Retención del 30% del ITBIS facturado. Norma general. |

### CDT — `tarifa-DO-CDT`

| Factor | Tarifa | Tipo | Vigencia desde |
|--------|:------:|:----:|:--------------:|
| — (fija) | 2% | Porcentaje | 2017-01-01 |

### PROPINA — `tarifa-DO-PROPINA`

| Factor | Tarifa | Tipo | Vigencia desde |
|--------|:------:|:----:|:--------------:|
| — (fija) | 10% | Porcentaje | 2017-01-01 |

---

## 3. Condiciones de aplicación — CondicionDeAplicacion

| Tributo | Entidad evaluada | Atributo evaluado | Valor esperado | Efecto | Notas |
|---------|:----------------:|-------------------|:--------------:|:------:|-------|
| ITBIS | — | — | — | `aplicar` (default) | No depende de calidades tributarias del emisor ni adquiriente. |
| RITBIS | — | — | — | `aplicar` si existe ITBIS | Requiere que ITBIS se haya calculado. No depende de calidades tributarias. |

> **Nota:** Las condiciones de aplicación en República Dominicana son significativamente más simples que en Colombia. No hay evaluación de calidades tributarias — la aplicación depende de la clasificación del bien/servicio.

---

## 4. Atributos fiscales — CatalogoDeAtributosFiscales

| Nombre | Tipo | Valores válidos | Requerido | Vigencia definición | Notas |
|--------|:----:|----------------|:---------:|:-------------------:|-------|
| tipoContribuyente | Enum | Persona Física, Persona Jurídica | Sí | 2017-01-01 → ∞ | Clasificación del contribuyente. |
| rnc | String | — | Sí | 2017-01-01 → ∞ | Registro Nacional del Contribuyente. |
| ncf | Boolean | — | Sí | 2017-01-01 → ∞ | Autorizado a emitir Comprobantes Fiscales (NCF/e-CF). |

---

## 5. Formatos fiscales — FormatoFiscal

### Reportes de información fiscal (autoridad: DGII)

| Código formato | Nombre | Tipo entregable | Periodicidad | Formato salida | Notas |
|---------------|--------|:---------------:|:------------:|:--------------:|-------|
| F-606 | Compras y gastos de proveedores | Reporte | Mensual | XML | Formato DGII — compras con NCF. |
| F-607 | Ventas e ingresos operacionales | Reporte | Mensual | XML | Formato DGII — ventas. Si 100% e-CF, no es obligatorio. |
| F-608 | Comprobantes cancelados (NCF) | Reporte | Mensual | XML | Formato DGII — NCF anulados. Si 100% e-CF, no es obligatorio. |
| F-609 | Pagos al exterior sin NCF | Reporte | Mensual | XML | Formato DGII — pagos a proveedores del exterior. |

> **Nota:** Si el 100% de las facturas son electrónicas (e-CF), no es obligatorio enviar formatos 607 y 608 — solo 606 y 609.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: 5 tributos, 4 clasificaciones, condiciones simples, 3 atributos fiscales, formatos DGII (606-609). |
