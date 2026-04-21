# Especificación del Servicio — [Nombre del Servicio]

## Tabla de contenido

1. [Propósito y relación con otros documentos](#sección-1-propósito)
2. [Estructura de datos](#sección-2-estructura-de-datos)
3. [API de consulta](#sección-3-api-de-consulta)
4. [Estrategia de caché](#sección-4-estrategia-de-caché)
5. [Carga y actualización de datos](#sección-5-carga-y-actualización-de-datos)
6. [Validaciones](#sección-6-validaciones)
7. [Permisos atómicos](#sección-7-permisos-atómicos) *(opcional)*
8. [Consideraciones de implementación](#sección-8-consideraciones-de-implementación)

---

## Sección 1: Propósito

### Propósito de este documento
Especificar el diseño técnico del servicio: estructura de datos, API, estrategias de caché, carga de datos y consideraciones de implementación.

### Relación con otros documentos
- **Definición de alcance:** `definicion-alcance.md` — el *qué* y el *por qué*.
- **Este documento:** el *cómo*.

---

## Sección 2: Estructura de datos

Para cada catálogo definido en el alcance, especificar su estructura completa.

### 2.1 [Nombre del catálogo]

**Identidad:** Qué campo o combinación de campos identifica de forma única un registro.

| Atributo | Tipo | Obligatorio | Descripción | Restricciones |
|----------|------|:-----------:|-------------|---------------|
| ... | string / integer / decimal / boolean / date / enum / ref | Sí / No | ... | Único, FK a [catálogo], valores: [...] |

**Relaciones:**
- Relaciones con otros catálogos del servicio (ej: División territorial → País).

**Ejemplos:**
```
(Ejemplo de 2-3 registros reales para ilustrar los datos)
```

*(Repetir subsección 2.N por cada catálogo)*

---

## Sección 3: API de consulta

### 3.1 Endpoints

| Método | Ruta | Descripción | Parámetros | Respuesta |
|--------|------|-------------|------------|-----------|
| GET | ... | ... | ... | ... |

### 3.2 Filtros y paginación

Convenciones de filtrado, ordenamiento y paginación aplicables a todos los endpoints.

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| ... | ... | ... |

### 3.3 Formato de respuesta

Estructura estándar de respuesta (envelope, errores, paginación).

---

## Sección 4: Recomendaciones de caché para consumidores

Recomendaciones del servicio hacia los sub-dominios que lo consumen. Cada consumidor decide si aplica caché o no según su contexto.

| Catálogo | Cacheable | TTL sugerido | Justificación |
|----------|:---------:|:------------:|---------------|
| ... | Sí / No | ... | ... |

---

## Sección 5: Carga y actualización de datos

### 5.1 Datos precargados
Qué datos vienen con el sistema y cómo se inicializan (scripts de seed, migraciones).

### 5.2 Carga automática
Si aplica: fuentes externas, frecuencia, mecanismo (API, scraping, archivo), manejo de errores y fallback.

| Catálogo | Fuente | Frecuencia | Mecanismo | Fallback |
|----------|--------|------------|-----------|----------|
| ... | ... | ... | ... | ... |

### 5.3 Carga manual
Operaciones que requieren intervención del administrador. Formatos de entrada soportados (UI, CSV, API).

---

## Sección 6: Validaciones

Validaciones que el servicio aplica al crear o modificar registros.

| # | Catálogo | Validación | Tipo |
|---|----------|------------|------|
| V1 | ... | ... | Unicidad / Referencial / Formato / Rango |

---

## Sección 7: Permisos atómicos *(opcional)*

Permisos que el servicio expone a la plataforma de seguridad. Solo incluir si el servicio tiene operaciones que requieran control de acceso diferenciado. Convención: `accion_recurso`.

| Permiso | Descripción |
|---------|-------------|
| ... | ... |

> La plataforma de seguridad consume estos permisos y gestiona roles/asignaciones. El servicio no gestiona roles.

---

## Sección 8: Consideraciones de implementación

### Sugerencias de implementación
Recomendaciones técnicas no prescriptivas (patrones, tecnologías, trade-offs).

### Pendientes de diseño
Decisiones que quedan abiertas para el equipo de desarrollo.

| # | Pendiente | Contexto |
|---|-----------|----------|
| PD1 | ... | ... |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | ... | Versión inicial. |
