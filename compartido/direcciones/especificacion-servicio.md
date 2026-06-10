# Especificación del Servicio — Direcciones

> ⚠️ **Superado (junio 2026).** El replanteamiento arquitectónico convirtió este servicio en el Nugget [`DireccionFisica`](../nuggets/direccion-fisica/especificacion.md). La estructura de datos, los perfiles por país y los catálogos de `configuracion/` se heredan como datos embebidos del Nugget; la persistencia centralizada, la API y los eventos de sincronización desaparecen. Se conserva como referencia histórica.

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

Especificar el diseño del servicio de Direcciones: estructura de datos, operaciones disponibles, validaciones, estrategia de carga de datos y consideraciones para la implementación.

### Relación con otros documentos

| Documento | Relación |
|-----------|---------|
| [`definicion-alcance.md`](definicion-alcance.md) | El *qué* y el *por qué* — catálogos, consumidores, administración, dentro/fuera del alcance |
| [`anexo-decision-modelo-direcciones.md`](anexo-decision-modelo-direcciones.md) | Decisiones de diseño: por qué servicio compartido, persistencia centralizada, flujo de sincronización entre módulos |
| [`../anexo-decision-i18n-l10n.md`](../anexo-decision-i18n-l10n.md) | Decisión transversal: convención de codificación, clasificación i18n/l10n, prioridad de resolución por locale |

---

## Sección 2: Estructura de datos

### 2.1 Dirección (entidad principal)

**Identidad:** `id` único generado por el sistema.

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| id | string (UUID) | Sí | Identificador único de la dirección | Generado por el sistema |
| tipoDireccion | string | Sí | Propósito de la dirección | Ref a catálogo Tipos de dirección (`FSC`, `COM`, `COR`, `ENT`, `SUC`) |
| paisCodigo | string | Sí | País de la dirección | Ref a catálogo de Países (Datos de Referencia), ISO 3166-1 |
| departamentoCodigo | string | Condicional | Departamento, provincia o estado | Ref a catálogo de Divisiones territoriales (Datos de Referencia). Obligatoriedad según formato del país |
| ciudadCodigo | string | Condicional | Ciudad o municipio | Ref a catálogo de Divisiones territoriales (Datos de Referencia). Obligatoriedad según formato del país |
| tipoVia | string | Condicional | Tipo de vía principal | Ref a catálogo Tipos de vía por país. Obligatorio solo en países que lo exigen (CO) |
| numeroVia | string | Condicional | Número de la vía principal | Obligatoriedad según formato del país |
| numeroPredio | string | Condicional | Número del predio o edificación | Obligatoriedad según formato del país |
| complementos | array | No | Lista de complementos de la dirección | Cada elemento: `{ tipo: ref Tipos de complemento, valor: string }` |
| codigoPostal | string | Condicional | Código postal | Ref a catálogo de Códigos postales por país. Obligatoriedad y formato según país |
| direccionLinea1 | string | Condicional | Línea de dirección libre | Para países sin estructura obligatoria (DO, PA). Obligatoriedad según formato del país |
| direccionLinea2 | string | No | Segunda línea de dirección libre | Información adicional |
| activo | boolean | Sí | Si la dirección está activa | Por defecto `true` |

**Relaciones:**
- `paisCodigo`, `departamentoCodigo`, `ciudadCodigo` → catálogos de Datos de Referencia
- `tipoDireccion` → catálogo interno Tipos de dirección
- `tipoVia` → catálogo interno Tipos de vía (por país)
- `complementos[].tipo` → catálogo interno Tipos de complemento
- `codigoPostal` → catálogo interno Códigos postales (por país)

**Ejemplo — Dirección en Colombia:**
```json
{
  "id": "dir-001",
  "tipoDireccion": "FSC",
  "paisCodigo": "CO",
  "departamentoCodigo": "05",
  "ciudadCodigo": "05001",
  "tipoVia": "CL",
  "numeroVia": "10",
  "numeroPredio": "43A-27",
  "complementos": [
    { "tipo": "EDF", "valor": "Torre Norte" },
    { "tipo": "PIS", "valor": "12" },
    { "tipo": "OFC", "valor": "1205" }
  ],
  "codigoPostal": "050021",
  "direccionLinea1": null,
  "direccionLinea2": null,
  "activo": true
}
```

**Ejemplo — Dirección en República Dominicana:**
```json
{
  "id": "dir-002",
  "tipoDireccion": "COM",
  "paisCodigo": "DO",
  "departamentoCodigo": "01",
  "ciudadCodigo": "10100",
  "tipoVia": null,
  "numeroVia": null,
  "numeroPredio": null,
  "complementos": [],
  "codigoPostal": "10100",
  "direccionLinea1": "Av. Winston Churchill esq. Calle Luis F. Thomen",
  "direccionLinea2": "Torre Empresarial, Piso 8",
  "activo": true
}
```

### 2.2 Tipos de dirección

**Identidad:** `codigo` (3 letras mayúsculas, convención propia — ver [anexo i18n/l10n](../anexo-decision-i18n-l10n.md))

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| codigo | string | Sí | Código único del tipo | 3 letras mayúsculas. Convención propia. |
| nombre | string | Sí | Nombre de referencia en español | Traducción final es responsabilidad del frontend (i18n) |
| activo | boolean | Sí | Si el tipo está activo | Por defecto `true` |

**Datos precargados:** [`configuracion/tipos-direccion.json`](configuracion/tipos-direccion.json) — 5 tipos (FSC, COM, COR, ENT, SUC)

### 2.3 Tipos de complemento

**Identidad:** `codigo` (3 letras mayúsculas, convención propia)

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| codigo | string | Sí | Código único del complemento | 3 letras mayúsculas. Convención propia. |
| nombre | string | Sí | Nombre de referencia en español | Traducción final es responsabilidad del frontend (i18n) |
| activo | boolean | Sí | Si el complemento está activo | Por defecto `true` |

**Datos precargados:** [`configuracion/tipos-complemento.json`](configuracion/tipos-complemento.json) — 16 tipos (APT, TRR, PIS, OFC, LOC, BDG, BLQ, INT, CSA, LTE, ETP, CNJ, URB, BRR, EDF, UND)

### 2.4 Tipos de vía (por país)

**Identidad:** `codigo` + `paisCodigo`

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| codigo | string | Sí | Código oficial del tipo de vía | Estándar del país (DIAN para CO). Longitud variable. |
| nombre | string | Sí | Nombre de referencia en español | Traducción final es responsabilidad del frontend (i18n) |
| paisCodigo | string | Sí | País al que pertenece | Ref a catálogo de Países (Datos de Referencia) |
| activo | boolean | Sí | Si el tipo está activo | Por defecto `true` |

**Datos precargados:** [`configuracion/tipos-via-co.json`](configuracion/tipos-via-co.json) — 21 tipos para Colombia (catálogo DIAN)

**Nota:** Solo se crean catálogos de tipos de vía para países que lo exigen en su regulación. Países sin catálogo oficial (DO, PA) usan el campo `direccionLinea1` para texto libre.

### 2.5 Formatos de dirección (por país)

**Identidad:** `paisCodigo`

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| paisCodigo | string | Sí | País al que aplica el formato | Ref a catálogo de Países (Datos de Referencia) |
| componentes | objeto | Sí | Definición de cada componente de dirección con su obligatoriedad | Cada componente: `{ obligatorio: boolean, catalogo?: string, formato?: string }` |
| notas | string | No | Aclaraciones sobre el formato del país | — |

**Datos precargados:** [`configuracion/formatos-direccion.json`](configuracion/formatos-direccion.json) — 5 países (CO, DO, PA, MX, US)

### 2.6 Códigos postales (por país)

**Identidad:** `codigo` (único dentro del archivo de cada país)

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| codigo | string | Sí | Código postal oficial | Formato según país (6 dígitos CO, 5 dígitos MX/US) |
| municipioCodigo | string | Sí | Municipio o ciudad al que pertenece | Ref a catálogo de Divisiones territoriales (Datos de Referencia) |
| nombre | string | No | Zona o sector que cubre el código | Referencia descriptiva |
| activo | boolean | Sí | Si el código está activo | Por defecto `true` |

**Datos precargados:** [`configuracion/codigos-postales-co.json`](configuracion/codigos-postales-co.json) — 248 códigos para Colombia (10 ciudades principales, fuente DIAN/4-72)

**Nota:** El catálogo completo de Colombia tiene 3.685 códigos. El archivo precargado cubre las ciudades principales. El resto se carga por sincronización con la fuente oficial.

---

## Sección 3: API de consulta

### 3.1 Operaciones sobre direcciones

| Operación | Descripción | Entrada | Salida |
|-----------|-------------|---------|--------|
| Crear dirección | Registra una nueva dirección validada según el formato del país | Dirección completa con tipo, país y componentes | Dirección creada con id asignado |
| Consultar dirección | Obtiene una dirección por su identificador | id de la dirección | Dirección completa |
| Actualizar dirección | Modifica los datos de una dirección existente | id + campos a modificar | Dirección actualizada |
| Inactivar dirección | Marca una dirección como inactiva (no se elimina) | id de la dirección | Confirmación |

### 3.2 Consultas de catálogos

| Operación | Descripción | Parámetros |
|-----------|-------------|------------|
| Listar tipos de dirección | Todos los tipos activos | — |
| Listar tipos de complemento | Todos los complementos activos | — |
| Listar tipos de vía por país | Tipos de vía activos para un país | paisCodigo |
| Obtener formato de dirección | Formato con campos obligatorios de un país | paisCodigo |
| Listar códigos postales | Códigos postales activos de un país | paisCodigo, filtro opcional por municipio |

### 3.3 Eventos de integración

Eventos que el servicio publica cuando una dirección cambia. Los módulos consumidores los escuchan para actualizar su referencia local.

| Evento | Se emite cuando | Contenido |
|--------|----------------|-----------|
| DireccionCreada | Se registra una nueva dirección | Dirección completa |
| DireccionActualizada | Se modifican los datos de una dirección existente | Dirección actualizada + campos que cambiaron |
| DireccionInactivada | Se inactiva una dirección | id + fecha de inactivación |

> El flujo de publicación y consumo de estos eventos está documentado en [`anexo-decision-modelo-direcciones.md`](anexo-decision-modelo-direcciones.md), sección 5.

---

## Sección 4: Recomendaciones de caché para consumidores

| Dato | Cacheable | Justificación |
|------|:---------:|---------------|
| Catálogos (tipos de dirección, complemento, vía, formatos) | Sí | Cambian de forma excepcional. El consumidor puede cachear con la frecuencia que considere adecuada. |
| Códigos postales | Sí | Se actualizan anualmente desde fuentes oficiales. |
| Direcciones específicas | No recomendado | Los cambios se propagan por eventos de integración. Cachear con TTL puede generar inconsistencias si una dirección se actualiza antes de que expire el caché. |

---

## Sección 5: Carga y actualización de datos

### Datos precargados

El servicio se inicializa con los archivos de configuración ubicados en `configuracion/`. Estos archivos son la fuente de verdad para la carga inicial en cualquier ambiente.

| Archivo | Contenido |
|---------|-----------|
| `tipos-direccion.json` | 5 tipos de dirección |
| `tipos-complemento.json` | 16 tipos de complemento |
| `tipos-via-co.json` | 21 tipos de vía para Colombia |
| `formatos-direccion.json` | Formatos para 5 países (CO, DO, PA, MX, US) |
| `codigos-postales-co.json` | 248 códigos postales de Colombia (10 ciudades principales) |

### Actualización de códigos postales

Los códigos postales se actualizan desde fuentes oficiales cuando estas publican nuevas versiones.

| País | Fuente | Frecuencia estimada |
|------|--------|---------------------|
| Colombia | DIAN / 4-72 | Anual |
| México | SAT / SEPOMEX | Anual |
| Estados Unidos | USPS | Anual |
| Otros países | GeoNames | Variable |

> La estrategia de carga sigue el patrón Seed + Sync + Extend documentado en [`../datos-referencia/anexo-estrategia-datos-referencia.md`](../datos-referencia/anexo-estrategia-datos-referencia.md).

---

## Sección 6: Validaciones

| # | Contexto | Validación | Tipo |
|---|----------|------------|------|
| V1 | Crear/actualizar dirección | El país debe existir y estar activo en el catálogo de Datos de Referencia | Referencial |
| V2 | Crear/actualizar dirección | El departamento y la ciudad deben pertenecer al país indicado | Referencial |
| V3 | Crear/actualizar dirección | Los campos marcados como obligatorios en el formato del país deben estar presentes | Formato |
| V4 | Crear/actualizar dirección (CO) | El tipo de vía debe existir en el catálogo de tipos de vía del país | Referencial |
| V5 | Crear/actualizar dirección | El tipo de dirección debe existir y estar activo | Referencial |
| V6 | Crear/actualizar dirección | Cada complemento debe referenciar un tipo de complemento activo | Referencial |
| V7 | Crear/actualizar dirección | El código postal debe cumplir con el formato del país (ej: 6 dígitos para CO, 5 para MX/US) | Formato |
| V8 | Crear/actualizar dirección | Si existe catálogo de códigos postales para el país, el código debe existir en el catálogo | Referencial |
| V9 | Inactivar dirección | Un registro en uso por otro módulo no se puede eliminar — solo inactivar | Protección |

---

## Sección 7: Permisos atómicos

No aplica para este servicio. Las direcciones no son un módulo al que un usuario accede directamente — son un servicio que otros módulos consumen. Un usuario crea una dirección cuando registra un tercero, configura una sucursal o emite una factura. Los permisos los controla el módulo que consume el servicio, no el servicio de Direcciones.

La administración excepcional de catálogos (agregar tipos de dirección, vía o complemento) es responsabilidad de la plataforma de seguridad a nivel de administrador del sistema.

---

## Sección 8: Consideraciones de implementación

### Sugerencias de implementación

- **Propagación de cambios:** El flujo de sincronización entre módulos (creación, actualización, inactivación de direcciones) se describe en detalle en el [`anexo-decision-modelo-direcciones.md`](anexo-decision-modelo-direcciones.md), sección 5. Los patrones sugeridos (outbox, idempotencia, snapshot para primera carga) son orientaciones — el equipo de desarrollo elige la implementación que mejor se adapte a su stack.

- **Validación condicional por país:** La lógica de validación debe leer el formato del país (`formatos-direccion.json`) para determinar qué campos son obligatorios. Esto permite agregar soporte para nuevos países sin modificar código — solo se agrega la configuración.

- **Convivencia de direcciones estructuradas y libres:** Algunos países exigen estructura (CO: tipo de vía + número + predio) y otros permiten texto libre (DO: línea 1 + línea 2). La entidad Dirección soporta ambos modelos. El formato del país determina cuál aplica.

### Pendientes de diseño

| # | Pendiente | Contexto |
|---|-----------|----------|
| PD1 | Adaptador de validación externa | Integración futura con Google Address Validation, Loqate o SmartyStreets para validar direcciones en tiempo real. No está en el alcance actual. |
| PD2 | Autocompletado en UI | Funcionalidad de la capa de presentación que sugiere direcciones mientras el usuario escribe. Depende de servicios externos (Google Places, Mapbox). |
| PD3 | Códigos postales completos | El catálogo precargado de Colombia cubre 248 de 3.685 códigos. El resto se completa por sincronización con DIAN/4-72. Los catálogos de MX y US no están precargados por volumen (~145.000 y ~41.000 respectivamente). |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Abril 2026 | Versión inicial. 8 secciones: estructura de datos (6 subsecciones: Dirección + 5 catálogos), operaciones y eventos de integración, recomendaciones de caché, carga de datos, 9 validaciones, permisos no aplica (decisión documentada), 3 pendientes de diseño. |
