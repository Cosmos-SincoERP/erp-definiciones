# Especificación del Servicio — Datos de Referencia

## Tabla de contenido

1. [Propósito y relación con otros documentos](#sección-1-propósito)
2. [Estructura de datos](#sección-2-estructura-de-datos)
3. [API de consulta](#sección-3-api-de-consulta)
4. [Recomendaciones de caché para consumidores](#sección-4-recomendaciones-de-caché-para-consumidores)
5. [Carga y actualización de datos](#sección-5-carga-y-actualización-de-datos)
6. [Validaciones](#sección-6-validaciones)
7. [Permisos atómicos](#sección-7-permisos-atómicos) *(opcional)*
8. [Consideraciones de implementación](#sección-8-consideraciones-de-implementación)

---

## Sección 1: Propósito

### Propósito de este documento

Especificar el diseño del servicio de Datos de Referencia: estructura de datos de cada catálogo, operaciones disponibles, validaciones, estrategia de carga y consideraciones para la implementación.

### Relación con otros documentos

| Documento | Relación |
|-----------|---------|
| [`definicion-alcance.md`](definicion-alcance.md) | El *qué* y el *por qué* — 5 catálogos, consumidores, administración, dentro/fuera del alcance |
| [`anexo-estrategia-datos-referencia.md`](anexo-estrategia-datos-referencia.md) | Estrategia Seed + Sync + Extend, fuentes oficiales, consideraciones para desarrollo |
| [`../anexo-decision-i18n-l10n.md`](../anexo-decision-i18n-l10n.md) | Clasificación i18n/l10n de catálogos, convención de codificación |

---

## Sección 2: Estructura de datos

### 2.1 Países

**Identidad:** `codigo` (ISO 3166-1 alpha-2, inmutable)

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| codigo | string | Sí | Código ISO 3166-1 alpha-2 | 2 letras mayúsculas. Inmutable. |
| nombre | string | Sí | Nombre de referencia en español | Traducción final es responsabilidad del frontend (i18n) |
| monedaPrincipal | string | Sí | Moneda funcional del país | Ref a catálogo de Monedas (ISO 4217) |
| indicativoTelefonico | string | Sí | Código internacional de marcación en formato E.164 | Prefijo `+` seguido de 1 a 3 dígitos (ej: `+57`, `+1`, `+507`). Consumido por Terceros para el VO `Telefono`. |
| activo | boolean | Sí | Si el país está habilitado | Por defecto `true` |

**Datos precargados:** [`catalogos/paises.json`](catalogos/paises.json) — 195 países

### 2.2 Divisiones territoriales

**Identidad:** `codigo` (único dentro del país, código oficial: DIVIPOLA para CO)

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| codigo | string | Sí | Código oficial de la división | Formato según país (numérico DIVIPOLA para CO) |
| nombre | string | Sí | Nombre de la división | Nombre geográfico oficial (no se traduce) |
| paisCodigo | string | Sí | País al que pertenece | Ref a catálogo de Países |
| nivel | string | Sí | Nivel jerárquico | departamento, municipio, provincia, distrito, corregimiento |
| codigoSuperior | string | No | División padre en la jerarquía | Ref a otra División territorial. Null para el nivel más alto. |
| activo | boolean | Sí | Si la división está habilitada | Por defecto `true` |

**Relaciones:**
- `paisCodigo` → Países
- `codigoSuperior` → Divisiones territoriales (auto-referencia jerárquica)

**Datos precargados:** Archivos separados por país:
- [`catalogos/divisiones-territoriales-co.json`](catalogos/divisiones-territoriales-co.json) — 1.188 registros
- [`catalogos/divisiones-territoriales-do.json`](catalogos/divisiones-territoriales-do.json) — 221 registros
- [`catalogos/divisiones-territoriales-pa.json`](catalogos/divisiones-territoriales-pa.json) — 108 registros

### 2.3 Monedas

**Identidad:** `codigo` (ISO 4217, inmutable)

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| codigo | string | Sí | Código ISO 4217 | 3 letras mayúsculas. Inmutable. |
| nombre | string | Sí | Nombre de referencia en español | Traducción final es responsabilidad del frontend (i18n) |
| decimales | integer | Sí | Cantidad de decimales | 0 para JPY/CLP, 2 para la mayoría, 3 para BHD |
| activo | boolean | Sí | Si la moneda está habilitada | Por defecto `true` |

**Datos precargados:** [`catalogos/monedas.json`](catalogos/monedas.json) — 154 monedas

### 2.4 Tipos de documento de identidad

**Identidad:** `codigo` + `paisCodigo`

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| codigo | string | Sí | Código del tipo de documento | Código oficial del país (NIT, CC, RNC, RFC, etc.) |
| descripcion | string | Sí | Nombre completo del tipo | Término del país de origen (l10n, no se traduce) |
| paisCodigo | string | Sí | País donde aplica | Ref a catálogo de Países. Null para documentos internacionales. |
| aplicaA | string | Sí | A quién aplica | personaNatural, personaJuridica, ambos |
| activo | boolean | Sí | Si el tipo está habilitado | Por defecto `true` |

**Datos precargados:** [`catalogos/tipos-documento-identidad.json`](catalogos/tipos-documento-identidad.json) — 45 tipos (CO, DO, PA, MX, CL, PE, EC, AR, BR + internacionales)

### 2.5 Tasas de cambio

**Identidad:** `monedaOrigen` + `monedaDestino` + `fechaVigencia`

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| monedaOrigen | string | Sí | Moneda que se convierte | Ref a catálogo de Monedas (ISO 4217) |
| monedaDestino | string | Sí | Moneda a la que se convierte | Ref a catálogo de Monedas (ISO 4217) |
| valor | decimal | Sí | Tasa de cambio | Precisión según monedas involucradas |
| fechaVigencia | date | Sí | Fecha desde la cual aplica | — |
| fuente | string | No | Fuente oficial del dato | Ej: "Banco de la República", "Banco Central RD" |

**Datos precargados:** Sin precarga. Se alimenta diariamente por sincronización o carga manual.

**Ejemplo:**
```json
{
  "monedaOrigen": "USD",
  "monedaDestino": "COP",
  "valor": 4150.25,
  "fechaVigencia": "2026-04-15",
  "fuente": "Banco de la República"
}
```

---

## Sección 3: API de consulta

### 3.1 Operaciones por catálogo

| Catálogo | Operaciones | Parámetros principales |
|----------|------------|----------------------|
| Países | Listar activos, consultar por código | — / codigo |
| Divisiones territoriales | Listar por país, listar por nivel, consultar por código | paisCodigo, nivel / codigo |
| Monedas | Listar activas, consultar por código | — / codigo |
| Tipos de documento | Listar por país, consultar por código + país | paisCodigo / codigo + paisCodigo |
| Tasas de cambio | Obtener tasa vigente para un par de monedas en una fecha | monedaOrigen, monedaDestino, fecha |

### 3.2 Consultas comunes

| Operación | Descripción | Ejemplo de uso |
|-----------|-------------|---------------|
| Divisiones por jerarquía | Obtener los municipios de un departamento | paisCodigo=CO, codigoSuperior=05 → todos los municipios de Antioquia |
| Tasa vigente | Obtener la TRM más reciente para un par de monedas en una fecha determinada | monedaOrigen=USD, monedaDestino=COP, fecha=2026-04-15 → 4150.25 |
| Tipos de documento por aplicabilidad | Filtrar por país y tipo de persona | paisCodigo=CO, aplicaA=personaJuridica → NIT |

---

## Sección 4: Recomendaciones de caché para consumidores

| Catálogo | Cacheable | Justificación |
|----------|:---------:|---------------|
| Países | Sí | Prácticamente inmutable. Cambios excepcionales. |
| Divisiones territoriales | Sí | Cambian de forma excepcional (nuevo municipio, reestructuración). |
| Monedas | Sí | Prácticamente inmutable. |
| Tipos de documento | Sí | Cambian de forma excepcional (nuevo país de operación). |
| Tasas de cambio | Con precaución | Se actualizan diariamente. El consumidor debe asegurar que consulta la tasa de la fecha correcta, no una versión cacheada de otro día. |

---

## Sección 5: Carga y actualización de datos

### Datos precargados

El servicio se inicializa con los archivos ubicados en `catalogos/`. Estos archivos son la fuente de verdad para la carga inicial en cualquier ambiente.

| Archivo | Contenido |
|---------|-----------|
| `paises.json` | 195 países |
| `monedas.json` | 154 monedas |
| `tipos-documento-identidad.json` | 45 tipos de documento (9 países + internacionales) |
| `divisiones-territoriales-co.json` | 1.188 divisiones de Colombia |
| `divisiones-territoriales-do.json` | 221 divisiones de República Dominicana |
| `divisiones-territoriales-pa.json` | 108 divisiones de Panamá |

### Actualización periódica

| Catálogo | Fuente | Frecuencia estimada |
|----------|--------|---------------------|
| Tasas de cambio CO | Banco de la República | Diaria |
| Tasas de cambio DO | Banco Central RD | Diaria |
| Divisiones territoriales | DIVIPOLA (DANE) para CO | Anual o cuando haya actualización |

> La estrategia de carga sigue el patrón Seed + Sync + Extend documentado en [`anexo-estrategia-datos-referencia.md`](anexo-estrategia-datos-referencia.md).

---

## Sección 6: Validaciones

| # | Catálogo | Validación | Tipo |
|---|----------|------------|------|
| V1 | Países | El código debe ser ISO 3166-1 alpha-2 válido (2 letras mayúsculas) | Formato |
| V2 | Países | La moneda principal debe existir en el catálogo de Monedas | Referencial |
| V3 | Divisiones territoriales | El país referenciado debe existir y estar activo | Referencial |
| V4 | Divisiones territoriales | Si tiene codigoSuperior, la división padre debe existir y pertenecer al mismo país | Referencial |
| V5 | Monedas | El código debe ser ISO 4217 válido (3 letras mayúsculas) | Formato |
| V6 | Tipos de documento | El país referenciado debe existir y estar activo (excepto documentos internacionales con paisCodigo null) | Referencial |
| V7 | Tipos de documento | No pueden existir dos tipos con el mismo código para el mismo país | Unicidad |
| V8 | Tasas de cambio | Moneda origen y moneda destino deben existir en el catálogo de Monedas | Referencial |
| V9 | Tasas de cambio | No pueden existir dos tasas con el mismo par de monedas en la misma fecha | Unicidad |
| V10 | Todos | Un registro referenciado por otro servicio o dominio no se puede eliminar — solo inactivar | Protección |

---

## Sección 7: Permisos atómicos

No aplica para este servicio. Datos de Referencia es un servicio de consulta que otros módulos consumen, no un módulo al que un usuario accede directamente. La administración excepcional de catálogos es responsabilidad de la plataforma de seguridad a nivel de administrador del sistema.

---

## Sección 8: Consideraciones de implementación

### Sugerencias de implementación

- **Catálogos como datos inmutables en origen:** Los códigos ISO (países, monedas) vienen precargados y no son editables. El sistema los provee, no el usuario. La capa de administración solo permite agregar registros nuevos (divisiones territoriales de un nuevo país, tipos de documento de un nuevo mercado) o inactivar existentes.

- **Tasas de cambio como único catálogo dinámico:** A diferencia de los otros 4 catálogos que son estáticos, las tasas de cambio se actualizan diariamente desde fuentes oficiales de cada país.

- **Consultas por fecha para tasas de cambio:** Los consumidores deben consultar la tasa vigente para una fecha específica, no "la última tasa". OXP necesita la TRM de la fecha de radicación y la de la fecha del extracto — pueden ser diferentes.

### Pendientes de diseño

| # | Pendiente | Contexto |
|---|-----------|----------|
| PD1 | Sincronización automática de tasas de cambio | Definir el mecanismo para obtener la TRM diaria del Banco de la República y Banco Central RD. |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Abril 2026 | Versión inicial. 8 secciones: estructura de datos (5 catálogos), operaciones y consultas, recomendaciones de caché, carga de datos (Seed + Sync + Extend), 10 validaciones, permisos no aplica (decisión documentada), 1 pendiente de diseño. |
