# Anexo: Configuración estándar — Panamá (PA)

## Propósito

Contenido fiscal estándar que el producto provee precargado para Panamá. Corresponde a los datos iniciales (seeds) que se cargan como streams de eventos de configuración al iniciar operación — los mismos eventos que el modelo define para cada agregado.

El contenido se organiza siguiendo la estructura de los agregados del modelo de dominio:

| Agregado | Qué contiene en este anexo |
|----------|---------------------------|
| CatalogoTributario | Tributos, clasificaciones tributarias, tratamientos, reglas de localización |
| TarifaTributaria | Tarifas conocidas por tributo |
| CondicionDeAplicacion | Reglas que modifican la aplicación según perfiles tributarios |
| CatalogoDeAtributosFiscales | Atributos fiscales requeridos |
| FormatoFiscal | Formatos de entregables por autoridad fiscal |

**Fuentes:** `fuentes/Definiciones de tributos.xlsx`, DGI Panamá.

**Nota sobre tarifas:** Las tarifas específicas son las vigentes al momento de la elaboración de este documento. El contenido estándar del producto se actualiza cuando la normativa cambia.

---

## 1. Tributos — CatalogoTributario

| Código | Nombre | Naturaleza | Base | Nivel jurisdiccional | Factor de tarifa | Tributo padre | Definición |
|--------|--------|:----------:|------|:--------------------:|-----------------|:-------------:|------------|
| ITBMS | Impuesto sobre la Transferencia de Bienes Corporales Muebles y la Prestación de Servicios | Aditivo | Ingreso | Nacional | `clasificacion` | — | Similar al IVA. Grava la transferencia de bienes y prestación de servicios. |
| RITBMS | Retención del ITBMS | Sustractivo | Ingreso | Nacional | `clasificacion` | — | Retención a título del ITBMS. |
| ISC | Impuesto Selectivo al Consumo | Aditivo | Ingreso | Nacional | `clasificacion` | — | Impuesto indirecto que se aplica a la importación y venta de ciertos bienes y servicios no esenciales o de lujo. |
| ISR | Impuesto sobre la Renta | Sustractivo | Ingreso | Nacional | `conceptoPago` | — | Tributo que se aplica sobre los ingresos obtenidos por personas naturales y jurídicas. |

**Total:** 4 tributos (2 impuestos + 2 retenciones).

### Clasificaciones tributarias

| Código | Nombre | Tributos que aplican | Notas |
|--------|--------|---------------------|-------|
| GRAV_ITBMS_7 | Gravados ITBMS 7% | ITBMS, RITBMS | Tarifa general. |
| GRAV_ITBMS_10 | Gravados ITBMS 10% | ITBMS, RITBMS | Bebidas alcohólicas, hospedaje. |
| GRAV_ITBMS_15 | Gravados ITBMS 15% | ITBMS, RITBMS | Cigarrillos y productos del tabaco. |
| EXENTO_ITBMS | Exentos de ITBMS | ISR | Bienes y servicios exentos del ITBMS. |
| ISC_APLICABLE | Sujeto a ISC | ISC | Bienes gravados con selectivo al consumo. |

### Reglas de localización

| Tributo | Rol fiscalmente relevante | Fallback | Notas |
|---------|--------------------------|----------|-------|
| ITBMS | `sedeEmisora` | — | Nacional. |
| RITBMS | `sedeEmisora` | — | Nacional. |
| ISC | `sedeEmisora` | — | Nacional. |
| ISR | `sedeEmisora` | — | Nacional. |

---

## 2. Tarifas — TarifaTributaria

### ITBMS — `tarifa-PA-ITBMS`

| Factor (clasificación) | Tarifa | Tipo | Vigencia desde |
|------------------------|:------:|:----:|:--------------:|
| GRAV_ITBMS_7 | 7% | Porcentaje | 2010-01-01 |
| GRAV_ITBMS_10 | 10% | Porcentaje | 2010-01-01 |
| GRAV_ITBMS_15 | 15% | Porcentaje | 2010-01-01 |

---

## 3. Condiciones de aplicación — CondicionDeAplicacion

| Tributo | Entidad evaluada | Atributo evaluado | Valor esperado | Efecto | Notas |
|---------|:----------------:|-------------------|:--------------:|:------:|-------|
| ITBMS | — | — | — | `aplicar` (default) | No depende de calidades tributarias. |
| RITBMS | — | — | — | `aplicar` | Reglas de tipo tercero configuradas previamente. Verifica que el adquiriente esté configurado en el concepto de RITBMS. |

> **Nota:** Al igual que República Dominicana, las condiciones en Panamá son simples. La complejidad de Colombia (8 casos para RETEFUENTE, 3 para RICA) no se replica en este país.

---

## 4. Atributos fiscales — CatalogoDeAtributosFiscales

| Nombre | Tipo | Valores válidos | Requerido | Vigencia definición | Notas |
|--------|:----:|----------------|:---------:|:-------------------:|-------|
| tipoContribuyente | Enum | Natural, Jurídica | Sí | 2017-01-01 → ∞ | Clasificación del contribuyente. |
| ruc | String | — | Sí | 2017-01-01 → ∞ | Registro Único del Contribuyente. |

---

## 5. Formatos fiscales — FormatoFiscal

> **Pendiente:** Los formatos fiscales exigidos por la DGI de Panamá no están documentados en las fuentes disponibles. Se completarán cuando se inicie la localización de Panamá.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: 4 tributos, 5 clasificaciones, condiciones simples, 2 atributos fiscales. Formatos DGI pendientes. |
