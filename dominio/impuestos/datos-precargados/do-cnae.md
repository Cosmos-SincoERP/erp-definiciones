# Catálogo CNAE — República Dominicana

**País:** República Dominicana (`DO`)
**Catálogo del modelo:** Catálogo de referencia para `PerfilTributario.ActividadEconomicaRegistrada.ciiu` (adaptación DR).
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`do-cnae.json`](do-cnae.json)

---

## 1. Propósito

Precarga la **Clasificación Nacional de Actividades Económicas (CNAE)** de la DGII República Dominicana, basada en CIIU Rev. 4 de Naciones Unidas. Sirve para:

- Clasificar la actividad económica de cada contribuyente al registrarse en DGII (RNC).
- Soportar las declaraciones del ISR y los reportes de información (F-606, F-607, F-608).
- Validar el código de actividad declarado por el contribuyente al emitir Comprobantes Fiscales Electrónicos (e-CF).

Cierra parcialmente el pendiente `[PD9]` para Dominicana.

---

## 2. Fuente normativa

- **DGII República Dominicana:** Clasificación adaptada de CIIU Rev. 4.
- **Base internacional:** ONU — International Standard Industrial Classification (ISIC) Rev. 4 (2008).
- **Vigencia DR:** desde 2012 con la adopción del ISIC Rev. 4 por la DGII para el registro de contribuyentes y los reportes electrónicos.

---

## 3. Estructura jerárquica

CNAE DR sigue la jerarquía estándar ISIC Rev. 4:

| Nivel | Identificador | Cantidad | Precargado en F1 |
|---|---|---|---|
| Sección | 1 letra (A-U) | 21 | **Sí** |
| División | 2 dígitos | 88 | **Sí** |
| Grupo | 3 dígitos | ~238 | No (carga vía catálogo DGII) |
| Clase | 4 dígitos | ~419 | No (carga vía catálogo DGII) |

**Total precargado:** 109 entradas estructurales.

---

## 4. Diferencias con CIIU CO

La CNAE DR es **equivalente estructuralmente** al CIIU CO Rev. 4 A.C. — ambas se basan en el ISIC Rev. 4 estándar de la ONU. Las únicas diferencias son:

- **Adaptaciones particulares en clases (4 dígitos):** cada país agrega notas explicativas locales o subdivisiones específicas. Para CO existe la "Adaptación Colombia" (`A.C.`); para DO existen notas DGII que no afectan los códigos estructurales pero sí algunas clases.
- **Sección E:** la denominación en DR es "Suministro de agua; evacuación de aguas residuales, gestión de desechos y descontaminación" (similar a CO pero con leve cambio en redacción).
- **Sección P:** DR usa "Enseñanza"; CO usa "Educación".
- **Sección Q:** DR usa "Servicios sociales y relacionados con la salud humana"; CO usa "Atención de la salud humana y de asistencia social".

Estas diferencias son **cosméticas** — no afectan la interoperabilidad ni la integridad referencial entre las dos clasificaciones.

---

## 5. Notas operativas

### 5.1. RNC y clasificación de actividad

Al crear el Registro Nacional del Contribuyente (RNC), DGII solicita el código CNAE principal y, opcionalmente, códigos secundarios. El ERP debe:

- Almacenar el código CNAE principal en `ActividadEconomicaRegistrada.ciiu` con `clasificacionAplicable: null` (catch-all).
- Permitir múltiples `ActividadEconomicaRegistrada` por entidad si tiene varias líneas de negocio.

### 5.2. Frontera con tarifas

A diferencia de Colombia donde `ICA` y `RICA` dependen del CIIU para resolver la tarifa, **DR no tiene tributos subnacionales por actividad económica en F1**. El CNAE no se usa como factor de tarifa — se usa principalmente para reportes y para condiciones de aplicación (por ejemplo, PROPINA aplica solo a hoteles y restaurantes).

### 5.3. Decisión de alcance: solo estructura macro

Las 419 clases y 238 grupos NO se precargan en este JSON. Se cargan vía importación del catálogo oficial DGII durante la implementación. Razón: igual que con CIIU CO, mantener cientos de clases manualmente en JSON es frágil. La estructura macro (secciones + divisiones) es suficiente para validación referencial y reportes agregados.

---

## 6. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 21 secciones + 88 divisiones de CNAE DR (basada en ISIC Rev. 4). Grupos y clases excluidos del JSON — carga vía catálogo DGII en implementación. |

---

## 7. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales DR**:

1. **¿Existe un catálogo oficial DGII actualizado con todas las clases?** Confirmar URL y formato.
2. **¿Las clases se renombran respecto a CIIU CO?** Si DGII tiene clases con denominación distinta, las identificamos para no asumir interoperabilidad total.
3. **PROPINA por CNAE:** ¿Qué códigos exactos de Sección I (55, 56) activan el cobro obligatorio de PROPINA?
4. **¿Hay grupos/clases con tarifas especiales de ITBIS o ISC?** Caso conocido: bebidas alcohólicas (clases 1101-1102) con ISC específico.
5. **CNAE vs CIIU CO:** ¿Mantenemos catálogos separados (`do-cnae` vs `co-ciiu`) o conviene un catálogo unificado de actividades económicas con sufijo país?
