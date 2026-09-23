# Catálogo de Convenios para Evitar la Doble Imposición — Panamá

**País:** Panamá (`PA`)
**Catálogo del modelo:** `ConvenioDeDobleImposicion` (Sección 3.9 de `modelo-dominio.md`)
**Versión:** 1.0
**Fecha de actualización:** 2026-09-22
**Archivo de datos:** [`pa-convenio-de-doble-imposicion.json`](pa-convenio-de-doble-imposicion.json)

---

## 1. Propósito

Precarga los **convenios para evitar la doble imposición** vigentes entre Panamá y otros países. Es la fuente que consulta la condición `ISR-03-cdi` (operador `con-convenio-vigente`, efecto `cambiarTarifa` por convenio) para reducir la tarifa del ISR sobre pagos a beneficiarios del exterior. Resuelve la pregunta abierta desde la carga inicial de Panamá (tabla de convenios "pendiente de modelar"): la condición ya puede sembrarse con un operador y un efecto definidos en el modelo.

---

## 2. Fuente normativa

- **Lista de convenios vigentes:** Dirección General de Ingresos (DGI) de Panamá — convenios para evitar la doble tributación, y sus leyes aprobatorias.
- **Tarifas convenidas:** **no precargadas**. Las tarifas reducidas varían por convenio y por concepto (dividendos, intereses, regalías, servicios); sembrar un valor aproximado sería inventar dato fiscal. Mientras el convenio no tenga tarifas convenidas, la condición `ISR-03-cdi` **no modifica la tarifa** del ISR.

---

## 3. Cobertura

**Total: 7 convenios vigentes precargados**, todos marcados `porValidar` y sin tarifas convenidas. Fechas de vigencia a nivel de año, por confirmar con la DGI.

---

## 4. Entradas

| País | Convenio | Instrumento | Aplica desde | Tarifas convenidas | |
|---|---|---|:---:|---|:---:|
| `MX` | Convenio Panamá – México | Ley aprobatoria de la República de Panamá | 2011-01-01 | — (por validar) | ⚠️ |
| `BB` | Convenio Panamá – Barbados | Ley aprobatoria de la República de Panamá | 2011-01-01 | — (por validar) | ⚠️ |
| `QA` | Convenio Panamá – Catar | Ley aprobatoria de la República de Panamá | 2011-01-01 | — (por validar) | ⚠️ |
| `ES` | Convenio Panamá – España | Ley aprobatoria de la República de Panamá | 2011-01-01 | — (por validar) | ⚠️ |
| `NL` | Convenio Panamá – Países Bajos | Ley aprobatoria de la República de Panamá | 2011-01-01 | — (por validar) | ⚠️ |
| `SG` | Convenio Panamá – Singapur | Ley aprobatoria de la República de Panamá | 2011-01-01 | — (por validar) | ⚠️ |
| `FR` | Convenio Panamá – Francia | Ley aprobatoria de la República de Panamá | 2012-01-01 | — (por validar) | ⚠️ |
| `KR` | Convenio Panamá – Corea del Sur | Ley aprobatoria de la República de Panamá | 2012-01-01 | — (por validar) | ⚠️ |
| `PT` | Convenio Panamá – Portugal | Ley aprobatoria de la República de Panamá | 2012-01-01 | — (por validar) | ⚠️ |
| `IE` | Convenio Panamá – Irlanda | Ley aprobatoria de la República de Panamá | 2012-01-01 | — (por validar) | ⚠️ |
| `LU` | Convenio Panamá – Luxemburgo | Ley aprobatoria de la República de Panamá | 2013-01-01 | — (por validar) | ⚠️ |
| `CZ` | Convenio Panamá – República Checa | Ley aprobatoria de la República de Panamá | 2013-01-01 | — (por validar) | ⚠️ |
| `AE` | Convenio Panamá – Emiratos Árabes Unidos | Ley aprobatoria de la República de Panamá | 2013-01-01 | — (por validar) | ⚠️ |
| `GB` | Convenio Panamá – Reino Unido | Ley aprobatoria de la República de Panamá | 2013-01-01 | — (por validar) | ⚠️ |
| `IL` | Convenio Panamá – Israel | Ley aprobatoria de la República de Panamá | 2014-01-01 | — (por validar) | ⚠️ |
| `IT` | Convenio Panamá – Italia | Ley aprobatoria de la República de Panamá | 2017-01-01 | — (por validar) | ⚠️ |
| `VN` | Convenio Panamá – Vietnam | Ley aprobatoria de la República de Panamá | 2017-01-01 | — (por validar) | ⚠️ |

⚠️ = `porValidar: true`.

---

## 5. Notas operativas

### 5.1. Cómo lo usa el motor

Una `Condicion` con operador **`con-convenio-vigente`** sobre el atributo `paisDeResidenciaFiscal` de la contraparte se cumple cuando este catálogo tiene un convenio con ese país vigente a la fecha de la transacción (`convenioVigenteA`). Su efecto `cambiarTarifa` con `tarifaAlternativa: { tipo: convenio }` reemplaza la tarifa del stream del tributo por la **tarifa convenida**, resuelta en este orden: (1) la tarifa convenida para el **concepto de pago** del concepto evaluado; (2) si no existe, la tarifa **general** del convenio (`conceptoPago: "*"`); (3) si el convenio no define ninguna, la condición **no modifica la tarifa** y rige la de `TarifaTributaria`. Una tarifa convenida de **cero** significa que el convenio reserva la potestad de gravar al país del beneficiario: el tributo se **excluye** del desglose con motivo `convenio_exime`.

### 5.2. Identidad y vigencia

Cada convenio se identifica por `(paisContraparte + origen + vigencia.fechaDesde)`. No pueden coexistir dos convenios vigentes con el mismo país y origen (invariante `[I28]`). Un protocolo que cambia una tarifa se registra con `ConvenioModificado` (tarifasConvenidas es modificable); la denuncia o terminación cierra la vigencia (`ConvenioCerrado`) y el convenio sigue valiendo para transacciones anteriores a la fecha de cierre.

### 5.3. Origen y precedencia

El contenido estándar lo provee el producto (`origen: estandar`). El cliente puede agregar o corregir con `origen: personalizado`; para el mismo país prevalece el personalizado (`[P3]`).

### 5.4. Frontera con otros catálogos

No es un régimen especial (`CatalogoDeRegimenesEspeciales` registra inscripciones de **empresas** ante una autoridad; aquí no hay empresa inscrita) ni una tarifa del tributo (`TarifaTributaria` indexa por un factor del tributo; aquí la tarifa depende del **país** de la contraparte). El país de residencia fiscal de la contraparte es un atributo **declarado** del `PerfilTributario` (`paisDeResidenciaFiscal`), no se deriva del país de la identificación.

---

## 6. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-09-22 | Carga inicial (issue #143; cierra el #138): 7 convenios vigentes según la DGI, sin tarifas convenidas (`porValidar`). Catálogo nuevo del agregado `ConvenioDeDobleImposicion` (modelo v2.1.0); `ISR-03-cdi` migrada al mecanismo en `pa-condicion-de-aplicacion` v1.1. |

---

## 7. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales**:

1. **Lista y fechas:** confirmar los 7 convenios y su fecha exacta de aplicación contra la publicación de la DGI.
2. **Tarifas convenidas por concepto:** para cada convenio, tarifa aplicable a dividendos, intereses, regalías y servicios (honorarios, servicios técnicos), y si los beneficios empresariales sin establecimiento permanente quedan exentos (tarifa cero → `convenio_exime`).
3. **Conceptos de pago del ISR:** las tarifas convenidas se indexan por los códigos de concepto de `tarifa-PA-ISR` (`HONORARIOS_PROFESIONALES`, `DIVIDENDOS`, `INTERESES_BANCARIOS`, `PAGOS_EXTERIOR_SERVICIOS`, …). ¿Ese conjunto de conceptos es suficiente para expresar las cláusulas de los convenios?
