# Catálogo de Atributos Fiscales — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `CatalogoDeAtributosFiscales` (Sección 3.5 de `modelo-dominio.md`)
**Versión:** 1.0
**Fecha de actualización:** 2026-05-26
**Archivo de datos:** [`co-catalogo-de-atributos-fiscales.json`](co-catalogo-de-atributos-fiscales.json)

---

## 1. Propósito

Define las **definiciones de atributos** que el `PerfilTributario` de cada entidad fiscal puede llevar. Cada `DefinicionAtributo` declara su nombre, tipo, valores válidos y si es requerido. El `PerfilTributario` instancia esas definiciones con valores específicos por entidad (vía `AtributoFiscal`), y la `CondicionDeAplicacion` los consulta para modificar el tratamiento tributario.

Cierra el contrato entre:
- `PerfilTributario` (consumidor: instancia atributos con valores).
- `CondicionDeAplicacion` (consumidor: evalúa expresiones contra atributos).
- `CatalogoDeRegimenesEspeciales` (referenciado: cuando un atributo tiene `catalogoReferencia`).

---

## 2. Fuente normativa

- **Estatuto Tributario Nacional** (Decreto 624 de 1989) — base para los atributos de RETEFUENTE, IVA, RIVA.
- **Régimen Simple de Tributación:** Ley 1943 de 2018 modificada por Ley 2010 de 2019.
- **Calificación DIAN de contribuyentes:** Resoluciones anuales.
- **Régimen Puerto Libre:** Constitución art. 310 + Ley 47 de 1993.
- **Monopolios departamentales:** Ley 1816 de 2016 (licores) + Constitución art. 336 (juegos de azar).

---

## 3. Cobertura del catálogo

| Categoría | Cantidad |
|---|---|
| Atributos booleanos (calificaciones DIAN/municipales) | 9 |
| Atributos enum simples | 2 (`regimenTributario`, `tipoPersona`) |
| Atributos enum con `catalogoReferencia` | 3 (zona franca, monopolio, puerto libre empresarial) |
| Atributos string libres | 0 |
| **Total** | **15** atributos en la precarga F1 |

---

## 4. Entradas

| Nombre | Tipo | Requerido | Valores válidos / Catálogo referenciado | Vigencia desde |
|---|:---:|:---:|---|:---:|
| `regimenTributario` | enum | Sí | Ordinario, Simple, Especial, NoResponsable | 2023-01-01 |
| `perteneceRegimenIVA` | boolean | Sí | — | 2017-01-01 |
| `esGranContribuyente` | boolean | Sí | — | 2017-01-01 |
| `esAutorretenedora` | boolean | Sí | — | 2017-01-01 |
| `esAgenteRetenedorIVA` | boolean | Sí | — | 2017-01-01 |
| `esExentoRetefuente` | boolean | Sí | — | 2017-01-01 |
| `perteneceRegimenSimple` | boolean | Sí | — | 2019-01-01 |
| `esAutorretenedorRenta` | boolean | Sí | — | 2017-01-01 |
| `esAgenteRetenedorICA` | boolean | No | — | 2017-01-01 |
| `esAutorretenedorICA` | boolean | No | — | 2017-01-01 |
| `esGranContribuyenteICA` | boolean | No | — | 2017-01-01 |
| `tipoPersona` | enum | Sí | Natural, Juridica | 2017-01-01 |
| `inscripcionZonaFranca` | enum | No | `CatalogoDeRegimenesEspeciales` filtro `zona-franca` | 2018-01-01 |
| `inscripcionMonopolio` | enum | No | `CatalogoDeRegimenesEspeciales` filtro `monopolio-sectorial` | 2016-12-19 |
| `inscripcionPuertoLibre` | enum | No | `CatalogoDeRegimenesEspeciales` filtro `puerto-libre-empresa` | 1991-07-04 |

---

## 5. Notas operativas

### 5.1. `requerido: true` significa que el PerfilTributario debe declararlo

Los 8 atributos requeridos (`regimenTributario`, `perteneceRegimenIVA`, `esGranContribuyente`, `esAutorretenedora`, `esAgenteRetenedorIVA`, `esExentoRetefuente`, `perteneceRegimenSimple`, `esAutorretenedorRenta`, `tipoPersona`) deben estar presentes en el `PerfilTributario` de cualquier entidad colombiana. Los 7 opcionales solo aplican cuando son relevantes para la entidad.

### 5.2. Diferencia entre `valoresValidos` y `catalogoReferencia`

- **`valoresValidos`** (enum corto): lista cerrada de valores embebida en la definición. Útil para enums pequeños y estables (`Ordinario`, `Simple`, etc.).
- **`catalogoReferencia`** (enum largo): el valor del atributo se valida contra otro agregado (típicamente `CatalogoDeRegimenesEspeciales`) filtrado por una categoría (`tipo`). Útil cuando los valores válidos son muchos y cambian (121 zonas francas, 33 monopolios, etc.).

### 5.3. `actividadEconomica` retirada como atributo

En versiones anteriores del modelo, la actividad económica era un atributo simple. Tras la decisión `[D14]`, pasa a modelarse como **entidad propia** `ActividadEconomicaRegistrada` dentro del `PerfilTributario`, con multiplicidad por jurisdicción y/o clasificación tributaria. NO se lista aquí porque ya no es un atributo del catálogo.

### 5.4. Atributos municipales (ICA)

Los tres atributos opcionales `esAgenteRetenedorICA`, `esAutorretenedorICA`, `esGranContribuyenteICA` se evalúan contextualmente. Una empresa puede ser autorretenedora de ICA en Bogotá pero no en Medellín. El modelo actual los declara como atributos simples del perfil — su contextualización por jurisdicción podría requerir refinamiento futuro (sugerencia: matriz `(municipio, atributo) → valor`).

### 5.5. Vigencia desde

La vigencia desde corresponde a la entrada en vigor de la norma que crea o regula el atributo. Algunos atributos coinciden con la entrada de IVA moderno (2017-01-01); otros tienen fechas específicas por marco normativo.

---

## 6. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 15 atributos (9 booleanos + 2 enum simples + 3 enum con catálogo referenciado + 1 enum tipo persona). |

---

## 7. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales**:

1. **¿Faltan atributos relevantes para F1?** Casos conocidos no incluidos: `esResponsableINC`, `pertenecesistemaPrecios`, `esExportador`, `esImportadorHabitual`. ¿Cuáles deben entrar en F1?
2. **Atributos municipales por jurisdicción:** ¿Cómo modelar que una empresa sea autorretenedora de ICA en Bogotá pero no en Medellín? ¿Mantenemos atributo simple booleano + condiciones por municipio, o introducimos estructura matricial?
3. **`regimenTributario` con valor `NoResponsable`:** ¿Es correcto este enum, o conviene `NoObligado` u otra denominación más alineada con DIAN?
4. **Vigencia desde 2017-01-01 para tantos atributos:** ¿Es la fecha correcta, o conviene rastrear el marco normativo específico de cada uno?
5. **¿`tipoPersona` debería ser atributo de Terceros (sub-dominio) en vez de atributo fiscal?** El sub-dominio Terceros ya modela tipo de persona. Aquí lo declaramos para que el motor lo pueda evaluar; podría leerse de Terceros vía integración.
