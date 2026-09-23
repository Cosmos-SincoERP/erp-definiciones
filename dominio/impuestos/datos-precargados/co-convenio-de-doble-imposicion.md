# Catálogo de Convenios para Evitar la Doble Imposición — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `ConvenioDeDobleImposicion` (Sección 3.9 de `modelo-dominio.md`)
**Versión:** 1.0
**Fecha de actualización:** 2026-09-22
**Archivo de datos:** [`co-convenio-de-doble-imposicion.json`](co-convenio-de-doble-imposicion.json)

---

## 1. Propósito

Precarga los **convenios para evitar la doble imposición** vigentes entre Colombia y otros países, con la tarifa de retención que sustituye a la general cuando el beneficiario del pago reside fiscalmente en un país con convenio. Es la fuente que consulta la condición `RTF-EXT-02` (operador `con-convenio-vigente`, efecto `cambiarTarifa` por convenio) para reducir la tarifa de la retención a beneficiarios del exterior (`RETEFUENTE_EXTERIOR`, 20 % general → tarifa convenida).

Es **contenido fiscal del producto** (`origen: estandar`): la lista la publica la DIAN y cambia con vigencia propia (entrada en vigor de cada convenio, protocolos, denuncias), independiente de las tarifas del tributo y de los regímenes especiales. Por eso vive en un agregado propio y no dentro de `TarifaTributaria` ni de `CatalogoDeRegimenesEspeciales`.

---

## 2. Fuente normativa

- **Lista de convenios vigentes:** DIAN — Normativa — Convenios tributarios (convenios para evitar la doble imposición y prevenir la evasión fiscal), y las leyes aprobatorias de cada convenio citadas en la tabla.
- **Tarifa general que sustituyen:** Estatuto Tributario arts. 406 a 408 (retención a beneficiarios del exterior, 20 % general).
- **Regla de negocio precargada:** tarifa general del convenio del **10 %** para todos los conceptos, acordada con la consultoría fiscal de la empresa (sep-2026). **Por validar** convenio por convenio: varios convenios eximen los beneficios empresariales sin establecimiento permanente (tarifa cero) o limitan el 10 % a regalías y servicios técnicos.

---

## 3. Cobertura

**Total: 7 convenios vigentes precargados**, todos marcados `porValidar` (lista, fechas de aplicación y tarifas por concepto deben confirmarse contra la publicación oficial de la DIAN).

No se precargan: convenios firmados sin entrada en vigor confirmada (Países Bajos, Luxemburgo, Brasil); la Decisión 578 de la Comunidad Andina (Bolivia, Ecuador, Perú), que es un régimen de tributación exclusiva en la fuente y no una tarifa reducida — pendiente de definición con la consultoría (§7).

---

## 4. Entradas

| País | Convenio | Instrumento | Aplica desde | Tarifas convenidas | |
|---|---|---|:---:|---|:---:|
| `ES` | Convenio Colombia – España | Ley 1082 de 2006 | 2008-10-23 | `*` → 10 % | ⚠️ |
| `CL` | Convenio Colombia – Chile | Ley 1261 de 2008 | 2009-12-22 | `*` → 10 % | ⚠️ |
| `CH` | Convenio Colombia – Suiza | Ley 1344 de 2009 | 2012-01-01 | `*` → 10 % | ⚠️ |
| `CA` | Convenio Colombia – Canadá | Ley 1459 de 2011 | 2012-06-12 | `*` → 10 % | ⚠️ |
| `MX` | Convenio Colombia – México | Ley 1568 de 2012 | 2014-01-01 | `*` → 10 % | ⚠️ |
| `KR` | Convenio Colombia – Corea del Sur | Ley 1667 de 2013 | 2014-07-03 | `*` → 10 % | ⚠️ |
| `IN` | Convenio Colombia – India | Ley 1668 de 2013 | 2014-07-07 | `*` → 10 % | ⚠️ |
| `PT` | Convenio Colombia – Portugal | Ley 1692 de 2013 | 2015-01-30 | `*` → 10 % | ⚠️ |
| `CZ` | Convenio Colombia – República Checa | Ley 1690 de 2013 | 2015-05-06 | `*` → 10 % | ⚠️ |
| `GB` | Convenio Colombia – Reino Unido | Ley 1939 de 2018 | 2019-12-13 | `*` → 10 % | ⚠️ |
| `IT` | Convenio Colombia – Italia | Ley 2004 de 2019 | 2021-10-07 | `*` → 10 % | ⚠️ |
| `FR` | Convenio Colombia – Francia | Ley 2061 de 2020 | 2022-01-01 | `*` → 10 % | ⚠️ |
| `JP` | Convenio Colombia – Japón | Ley 2095 de 2021 | 2022-09-04 | `*` → 10 % | ⚠️ |
| `AE` | Convenio Colombia – Emiratos Árabes Unidos | Ley 2141 de 2021 | 2024-01-01 | `*` → 10 % | ⚠️ |
| `UY` | Convenio Colombia – Uruguay | Ley 2225 de 2022 | 2024-01-01 | `*` → 10 % | ⚠️ |

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
| 1.0 | 2026-09-22 | Carga inicial (issue #143): 7 convenios vigentes con tarifa general del 10 % por regla de negocio, todos `porValidar`. Catálogo nuevo del agregado `ConvenioDeDobleImposicion` (modelo v2.1.0). |

---

## 7. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales**:

1. **Lista y fechas:** confirmar los 7 convenios y su fecha de aplicación contra la publicación de la DIAN; confirmar la entrada en vigor de Emiratos Árabes Unidos y Uruguay; indicar si Países Bajos, Luxemburgo o Brasil ya rigen.
2. **Tarifa por concepto:** el 10 % general es una simplificación. Para cada convenio, ¿qué tarifa aplica a beneficios empresariales sin establecimiento permanente (¿cero?), regalías, servicios técnicos, asistencia técnica, consultoría, intereses y dividendos? La estructura admite una tarifa por concepto de pago y una general.
3. **Comunidad Andina (Decisión 578):** ¿se modela como convenio con tarifa cero para rentas gravadas exclusivamente en el país de la fuente del beneficiario, o requiere un tratamiento distinto?
4. **Certificado de residencia fiscal:** ¿la aplicación de la tarifa de convenio exige certificado vigente del proveedor? Ver pregunta 6 del catálogo de atributos fiscales.
