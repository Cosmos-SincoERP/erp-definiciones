# Definición de Alcance — [Nombre del Servicio]

## Tabla de contenido

1. [Definición y justificación](#sección-1-definición-y-justificación)
2. [Glosario de términos](#sección-2-glosario-de-términos)
3. [Catálogos y recursos](#sección-3-catálogos-y-recursos)
4. [Consumidores](#sección-4-consumidores)
5. [Datos preconfigurados](#sección-5-datos-preconfigurados)
6. [Administración](#sección-6-administración)
7. [Qué está dentro y fuera del alcance](#sección-7-qué-está-dentro-y-fuera-del-alcance)
8. [Beneficios esperados](#sección-8-beneficios-esperados)

---

## Sección 1: Definición y justificación

### Definición
Qué es este servicio, cuál es su responsabilidad dentro del ERP y qué tipo de datos gestiona.

### Por qué no es un sub-dominio
Justificación de por qué este servicio no se modela como un bounded context DDD. Criterios: ¿tiene reglas de negocio propias? ¿tiene comportamiento propio? ¿tiene procesos de negocio?

### Contexto actual
Cómo se gestionan estos datos hoy (hojas de cálculo, hardcoded, otro sistema).

### Problema actual
Dolores o limitaciones que justifican construir este servicio.

---

## Sección 2: Glosario de términos

Definiciones de los conceptos clave del servicio. Fuente de verdad terminológica.

| # | Término | Definición |
|---|---------|-----------|
| 1 | ... | ... |

---

## Sección 3: Catálogos y recursos

Lista de cada catálogo o recurso que el servicio gestiona, con descripción breve y naturaleza del dato.

| # | Catálogo | Descripción | Naturaleza |
|---|----------|-------------|------------|
| 1 | ... | ... | Estático / Cambio lento / Actualización periódica |

*(Cada catálogo se detalla en la especificación del servicio con su estructura de datos completa)*

---

## Sección 4: Consumidores

### Matriz de consumo

Qué sub-dominio consume qué catálogo, para qué lo usa y con qué criticidad.

| Catálogo | Sub-dominio | Uso | Criticidad |
|----------|-------------|-----|:----------:|
| ... | ... | ... | Alta / Media / Baja |

### Notas de consumo
Aclaraciones sobre patrones especiales de consumo (ej: un sub-dominio necesita histórico, otro solo dato vigente).

---

## Sección 5: Datos preconfigurados

### Cobertura geográfica
Países para los cuales el servicio viene con datos precargados.

### Datos por catálogo

Para cada catálogo, qué datos vienen precargados y cuáles debe agregar el administrador.

| Catálogo | Datos precargados | Extensible por administrador |
|----------|-------------------|:----------------------------:|
| ... | ... | Sí / No |

---

## Sección 6: Administración

### Actores

| Actor | Descripción | Responsabilidades |
|-------|-------------|-------------------|
| ... | ... | ... |

### Operaciones de administración

| Operación | Quién | Frecuencia | Observaciones |
|-----------|-------|------------|---------------|
| ... | ... | ... | ... |

### Carga automática de datos
Si aplica, describir qué datos se cargan automáticamente, desde qué fuentes, con qué frecuencia y qué mecanismo de fallback existe.

---

## Sección 7: Qué está dentro y fuera del alcance

### Dentro del alcance
Lista de responsabilidades que este servicio asume.

### Fuera del alcance
Lista de responsabilidades que NO pertenecen a este servicio y quién las asume.

### Dependencias externas
Servicios o fuentes externas que este servicio necesita para operar.

---

## Sección 8: Beneficios esperados

### Beneficios operativos
Mejoras en eficiencia y gestión de datos.

### Beneficios de consistencia
Mejoras en calidad de datos, unicidad y eliminación de duplicados.

### Beneficios de escalabilidad
Facilidad para agregar nuevos países, monedas, catálogos.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | ... | Versión inicial. |
