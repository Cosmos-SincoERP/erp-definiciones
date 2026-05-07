# Definición de Alcance — Datos de Referencia

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

Datos de Referencia es el servicio de infraestructura de datos del ERP. Gestiona los catálogos de referencia que todos los sub-dominios necesitan pero que ninguno posee como parte de su dominio de negocio: países, divisiones territoriales, monedas, tipos de documento de identidad, tipos de empresa y tasas de cambio.

Opera como un servicio de consulta — los sub-dominios lo consumen para validar, clasificar y contextualizar sus transacciones.

### Por qué no es un sub-dominio

| Criterio DDD | Datos de Referencia | Terceros (comparación) |
|--------------|:----------:|:----------------------:|
| ¿Reglas de negocio propias? | No | Sí (unicidad, roles) |
| ¿Comportamiento propio? | No | Sí (creación, inactivación) |
| ¿Procesos de negocio? | No | Sí (creación desde consumidores) |
| ¿Publica eventos de dominio? | No | Sí (TerceroCreado, etc.) |

Los datos de este servicio son de referencia: se crean, se consultan y eventualmente se inactivan. No desencadenan acciones en otros dominios.

### Contexto actual

Los catálogos de países, monedas y tipos de documento están embebidos en cada módulo de SincoERP. Las tasas de cambio se cargan manualmente desde el Banco de la República. No hay un servicio centralizado — cada módulo mantiene su propia copia, lo que genera inconsistencias.

### Problema actual

1. **Duplicación:** El mismo catálogo de países/monedas está replicado en múltiples módulos con variantes.
2. **Corrupción de entidades:** Cuando un módulo necesita atributos adicionales sobre una entidad común (ej: un país con configuración fiscal, una moneda con decimales específicos), extiende la entidad directamente, corrompiendo la definición original y creando versiones divergentes del mismo concepto.
3. **Tasas de cambio manuales:** La carga diaria de TRM depende de intervención humana. Si no se carga, las operaciones en moneda extranjera se bloquean o usan datos desactualizados.
4. **Extensibilidad limitada:** Agregar un nuevo país o tipo de documento requiere modificar código en cada módulo.
5. **Sin jerarquía territorial:** No existe un catálogo estructurado de divisiones territoriales. Los tributos municipales (ICA, RICA) dependen de que cada módulo conozca las jurisdicciones por su cuenta.

---

## Sección 2: Glosario de términos

| # | Término | Definición |
|---|---------|-----------|
| 1 | **Catálogo** | Conjunto de registros de referencia que describe un concepto transversal del ERP (ej: países, monedas). No tiene comportamiento de negocio — solo se consulta. |
| 2 | **Dato base** | Registro individual dentro de un catálogo (ej: Colombia es un dato base del catálogo de países). |
| 3 | **País** | Entidad político-administrativa soberana, identificada por código ISO 3166-1. Raíz de la configuración fiscal y de las divisiones territoriales. |
| 4 | **División territorial** | Subdivisión político-administrativa de un país (departamento, provincia, municipio, distrito). Estructura jerárquica. |
| 5 | **Moneda** | Unidad monetaria identificada por código ISO 4217. Usada para expresar valores en transacciones del ERP. |
| 6 | **Tipo de documento de identidad** | Clasificación del documento que identifica a una persona o empresa (NIT, CC, RNC, RUC, etc.). Varía por país. |
| 7 | **Tasa de cambio (TRM)** | Valor de conversión entre dos monedas en una fecha determinada. Fuente oficial por país (ej: Banco de la República para Colombia). |
| 8 | **Moneda funcional** | Moneda principal de operación de un país (ej: COP para Colombia, DOP para República Dominicana). |
| 9 | **Dato preconfigurado** | Registro que viene cargado con el sistema para los países donde opera el ERP (CO, DO, PA). No requiere intervención del administrador. |

---

## Sección 3: Catálogos y recursos

| # | Catálogo | Descripción | Naturaleza |
|---|----------|-------------|------------|
| 1 | **Países** | Catálogo de países con código ISO 3166-1, moneda funcional e indicativo telefónico internacional (E.164). | Estático |
| 2 | **Divisiones territoriales** | Estructura jerárquica de subdivisiones por país (departamento → municipio). Necesario para resolución de jurisdicción fiscal. | Estático |
| 3 | **Monedas** | Catálogo de monedas con código ISO 4217 y cantidad de decimales. | Estático |
| 4 | **Tipos de documento de identidad** | Tipos de documento para identificación de personas y empresas, por país. | Estático |
| 5 | **Tasas de cambio** | Histórico de tasas de cambio entre pares de monedas, con fecha de vigencia y fuente oficial. | Actualización diaria |

### Consideraciones

- **Estándares ISO:** Los catálogos de países y monedas adoptan los estándares internacionales ISO 3166-1 (códigos de país de 2 letras: CO, US, MX) e ISO 4217 (códigos de moneda de 3 letras: COP, USD, EUR). Estos estándares son utilizados globalmente por bancos, pasarelas de pago, ERPs y APIs. Se adoptan para garantizar interoperabilidad con sistemas externos y eliminar ambigüedades en operaciones internacionales.

- **Tipos de empresa excluido:** La clasificación de tipo de empresa (persona natural, jurídica, ESAL) no se incluye como catálogo de referencia. Ningún dominio distinto a Terceros lo consulta directamente para operar — los demás dominios lo reciben indirectamente a través del tercero. La responsabilidad de administrar estos tipos es de Terceros.

- **Estrategia de gestión de datos:** Los catálogos se gestionan con un modelo híbrido Seed + Sync + Extend, alineado con la práctica de la industria. Los archivos JSON preconstruidos en `catalogos/` son la fuente de verdad para la carga inicial. Ver [`anexo-estrategia-datos-referencia.md`](anexo-estrategia-datos-referencia.md) para la decisión completa, fuentes de la industria y consideraciones para el equipo de desarrollo.

> La estructura de datos detallada de cada catálogo (atributos, tipos, restricciones) se define en la especificación del servicio (`especificacion-servicio.md`).

---

## Sección 4: Consumidores

### Matriz de consumo

| Catálogo | Sub-dominio | Uso | Criticidad |
|----------|-------------|-----|:----------:|
| Países | Impuestos | Raíz de configuración fiscal (CatalogoTributario por país) | Alta |
| Países | Terceros | País de emisión del documento de identidad; indicativo telefónico del VO `Telefono` | Alta |
| Países | OXP | Identificación de compras del exterior | Media |
| Divisiones territoriales | Impuestos | Resolución de jurisdicción fiscal. Tributos municipales (ICA, RICA) requieren nivel de municipio | Alta |
| Divisiones territoriales | Terceros | Dirección principal del tercero | Media |
| Países | Direcciones | País de la dirección. Determina qué formato y validaciones aplican. | Alta |
| Divisiones territoriales | Direcciones | Departamento y ciudad de la dirección. Referencia en la estructura de dirección. | Alta |
| Monedas | OXP | Moneda de la obligación. Operaciones en moneda extranjera | Alta |
| Monedas | Contabilidad | Moneda única del borrador contable | Alta |
| Monedas | Impuestos | Conversión de cuantía mínima cuando moneda transacción ≠ moneda jurisdicción | Media |
| Tipos de documento | Terceros | Unicidad del tercero: tipoDocumento + numeroId + país | Alta |
| Tipos de documento | Impuestos | Identificación de entidad fiscal | Alta |
| Tasas de cambio | OXP | TRM en fecha de radicación vs. extracto. Ajustes por diferencia en cambio | Alta |
| Tasas de cambio | Impuestos | Conversión de cuantía mínima | Media |

---

## Sección 5: Datos preconfigurados

### Cobertura geográfica

El sistema viene con datos precargados para los 3 países de operación (Colombia, República Dominicana, Panamá) más catálogos globales (todos los países y monedas del mundo). Extensible por el administrador.

### Catálogos precargados

| Catálogo | Archivo | Registros | Cobertura |
|----------|---------|:---------:|-----------|
| Países | [`catalogos/paises.json`](catalogos/paises.json) | 195 | Todos los países del mundo (ISO 3166-1) |
| Monedas | [`catalogos/monedas.json`](catalogos/monedas.json) | 154 | Todas las monedas activas del mundo (ISO 4217) |
| Tipos de documento de identidad | [`catalogos/tipos-documento-identidad.json`](catalogos/tipos-documento-identidad.json) | 45 | CO, DO, PA completos + MX, CL, PE, EC, AR, BR + internacionales |
| Divisiones territoriales — Colombia | [`catalogos/divisiones-territoriales-co.json`](catalogos/divisiones-territoriales-co.json) | 1.188 | 33 departamentos + todos los municipios con códigos DIVIPOLA |
| Divisiones territoriales — Rep. Dominicana | [`catalogos/divisiones-territoriales-do.json`](catalogos/divisiones-territoriales-do.json) | 221 | 32 provincias + todos los municipios |
| Divisiones territoriales — Panamá | [`catalogos/divisiones-territoriales-pa.json`](catalogos/divisiones-territoriales-pa.json) | 108 | 10 provincias + 3 comarcas + todos los distritos |
| Tasas de cambio | — | — | Sin precarga. Se alimenta diariamente desde fuentes oficiales o carga manual. |

### Extensibilidad

Todos los catálogos son extensibles por el administrador. Al agregar un nuevo país de operación, se deben crear las divisiones territoriales y tipos de documento correspondientes.

---

## Sección 6: Administración

### Responsabilidades del sistema (producto)

| Responsabilidad | Descripción |
|----------------|-------------|
| Proveer catálogos completos y listos para usar | El sistema viene con todos los países del mundo (195), todas las monedas activas (154), divisiones territoriales de los países de operación y tipos de documento de identidad de LatAm. El cliente no necesita configurar nada para empezar a operar. |
| Proteger datos de estándares internacionales | Los códigos ISO de países (3166-1) y monedas (4217) no son editables. Son estándares internacionales. |
| Proteger registros en uso | Un registro que fue referenciado en una transacción no se puede eliminar — solo inactivar. |
| Registrar cambios | Toda modificación a los catálogos queda registrada con fecha y usuario. |
| Mantener tasas de cambio actualizadas | Sincronización diaria con fuentes oficiales (Banco de la República para CO, Banco Central RD para DO). |

### Intervención del administrador (excepcional)

| Caso | Ejemplo |
|------|---------|
| Agregar divisiones territoriales de un nuevo país | Una empresa necesita operar en un país que no tiene divisiones precargadas. |
| Agregar tipos de documento de un nuevo país | Un nuevo mercado con tipos de documento no cubiertos. |
| Cargar tasas de cambio manualmente | Cuando la sincronización automática falla o para monedas sin fuente automatizada. |

---

## Sección 7: Qué está dentro y fuera del alcance

### Dentro del alcance

- Los 5 catálogos definidos: países, divisiones territoriales, monedas, tipos de documento de identidad, tasas de cambio.
- Catálogos precargados por el producto para operación inmediata.
- Sincronización diaria de tasas de cambio desde fuentes oficiales.
- Extensibilidad para nuevos países y tipos de documento.
- Reglas de protección: datos ISO inmutables, registros en uso no eliminables, cambios auditables.

### Fuera del alcance

- Tipos de empresa (responsabilidad de Terceros).
- Direcciones (responsabilidad del servicio de Direcciones en `compartido/direcciones/`).
- Configuración fiscal por país (responsabilidad de Impuestos).
- Gestión de terceros (responsabilidad del dominio de Terceros).

### Dependencias

- Ninguna. Este servicio no depende de otros servicios — es la base que todos consumen.

---

## Sección 8: Beneficios esperados

### Beneficios operativos
- Cualquier módulo del ERP consulta países, monedas o divisiones territoriales desde una sola fuente. No hay copias divergentes.
- Las tasas de cambio se actualizan automáticamente. Las operaciones en moneda extranjera no dependen de carga manual.

### Beneficios de consistencia
- Un país, una moneda, un tipo de documento tienen una sola definición en todo el ERP. Se elimina el problema de SincoERP donde cada módulo extendía las entidades con atributos propios.
- Los códigos ISO garantizan interoperabilidad con sistemas externos (bancos, pasarelas de pago, autoridades fiscales).

### Beneficios de escalabilidad
- Agregar un nuevo país de operación no requiere modificar código en ningún módulo. Se agregan las divisiones territoriales y tipos de documento del país, y todos los módulos lo ven automáticamente.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Abril 2026 | Versión inicial. 8 secciones: definición y justificación, glosario (9 términos), 5 catálogos, matriz de consumidores (6 sub-dominios/servicios), datos preconfigurados (6 archivos JSON), administración (sistema + excepcional), dentro/fuera del alcance, beneficios. Anexo de estrategia de datos (Seed + Sync + Extend). |
