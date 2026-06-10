# Definición de Alcance — Direcciones

> ⚠️ **Superado (junio 2026).** El replanteamiento arquitectónico convirtió este servicio en el Nugget [`DireccionFisica`](../nuggets/direccion-fisica/especificacion.md), que hereda su estructura, perfiles por país y catálogos (ver Sección 8 de esa especificación para el paralelo completo y una corrección normativa verificada). Este documento se conserva como referencia histórica y fuente del diseño heredado.

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

Direcciones es un servicio compartido del ERP que gestiona la estructura, configuración y validación de direcciones. Proporciona un modelo unificado que se adapta a las reglas de cada país (campos obligatorios, tipos de vía, complementos, formato de código postal), de forma que los módulos que necesitan direcciones no tienen que conocer las particularidades de cada país.

### Por qué es un servicio independiente

Una dirección no es un campo simple — cada país tiene reglas diferentes sobre cómo se estructura, qué campos son obligatorios y qué catálogos aplican. Por ejemplo, Colombia exige tipos de vía codificados (Calle, Carrera, Diagonal) mientras que República Dominicana permite texto libre. Este servicio centraliza esa complejidad para que los módulos del ERP no la dupliquen.

| Necesidad | ¿Se resuelve con un campo de texto? | ¿Se resuelve con este servicio? |
|-----------|:---:|:---:|
| Estructura diferente por país | ❌ | ✅ |
| Validación de campos obligatorios por país | ❌ | ✅ |
| Catálogos de tipos de vía por país | ❌ | ✅ |
| Tipos de dirección (fiscal, comercial, correspondencia) | ❌ | ✅ |
| Múltiples módulos necesitan direcciones | — | ✅ |

### Contexto actual

En SincoERP las direcciones se manejan como campos de texto libre. No hay estructura estandarizada, lo que dificulta la generación de facturación electrónica y la consistencia entre módulos.

### Problema actual

1. **Sin estructura:** La dirección es un campo de texto libre. No se puede extraer ciudad, departamento o tipo de vía de forma automática.
2. **Incompatible con facturación electrónica:** La DIAN exige tipos de vía codificados (CL, CR, DG, TV) y códigos postales de su catálogo oficial. Hoy se arma manualmente.
3. **Sin soporte para otros países:** Si un tercero tiene dirección en México o Estados Unidos, no hay forma de validar que la estructura sea correcta para ese país.
4. **Duplicación:** Cada módulo que necesita una dirección implementa su propia validación (o no la implementa).
5. **Complementos sin estandarizar:** Algunos módulos manejan información complementaria de la dirección (apartamento, torre, piso, oficina, local, bodega) pero no hay un estándar compartido. Cada módulo que lo necesita lo implementa a su manera, generando inconsistencias.

---

## Sección 2: Glosario de términos

| # | Término | Definición |
|---|---------|-----------|
| 1 | **Dirección** | Ubicación física de una persona, empresa o unidad organizacional. Compuesta por componentes que varían según el país. |
| 2 | **Tipo de dirección** | Clasificación del propósito de una dirección: fiscal, comercial, correspondencia, entrega, sucursal. Un tercero puede tener múltiples direcciones con tipos diferentes. |
| 3 | **Componente de dirección** | Cada parte que conforma una dirección: tipo de vía, número, complemento, ciudad, departamento, código postal, país. |
| 4 | **Tipo de vía** | Clasificación de la vía principal de una dirección. Varía por país. En Colombia: Calle (CL), Carrera (CR), Diagonal (DG), Transversal (TV), Avenida Calle (AC), Avenida Carrera (AK), entre otros. |
| 5 | **Complemento** | Información adicional que precisa la ubicación dentro de una dirección: apartamento, torre, piso, oficina, local, bodega, bloque, interior. |
| 6 | **Código postal** | Código numérico o alfanumérico que identifica una zona geográfica dentro de un país. Exigido por algunas autoridades fiscales (DIAN en Colombia, SAT en México). |
| 7 | **Formato de dirección** | Configuración que define para cada país qué componentes son obligatorios, en qué orden se presentan y qué catálogos aplican. |
| 8 | **Dirección estructurada** | Dirección descompuesta en componentes individuales (tipo de vía, número, complemento, ciudad, etc.) en lugar de un campo de texto libre. |

---

## Sección 3: Catálogos y recursos

| # | Catálogo | Descripción | Naturaleza |
|---|----------|-------------|------------|
| 1 | **Tipos de dirección** | Clasificación del propósito de la dirección: fiscal, comercial, correspondencia, entrega, sucursal. Predefinido y extensible por el administrador. | Estático |
| 2 | **Formatos de dirección por país** | Configuración que define para cada país qué componentes son obligatorios, opcionales o no aplican, en qué orden se presentan y qué validaciones tienen. | Estático |
| 3 | **Tipos de vía por país** | Nomenclatura oficial de tipos de vía. Aplica solo en países que lo exigen (Colombia: Calle, Carrera, Diagonal, Transversal, Avenida — catálogo DIAN). Países sin catálogo oficial usan texto libre. | Estático |
| 4 | **Tipos de complemento** | Clasificación de la información complementaria: apartamento, torre, piso, oficina, local, bodega, bloque, interior, casa. Catálogo global, no varía por país. | Estático |
| 5 | **Códigos postales por país** | Catálogo de códigos postales oficiales, precargados por país. Fuentes: DIAN/4-72 para Colombia, SAT/SEPOMEX para México, USPS para Estados Unidos, GeoNames para otros países. | Estático |

### Consideraciones

- **Tipos de dirección extensibles:** Los tipos base (fiscal, comercial, correspondencia, entrega, sucursal) cubren los usos conocidos. El administrador puede agregar tipos adicionales si el negocio lo requiere (ej: "punto de venta", "bodega de despacho").

- **Formatos de dirección por país:** Siguen el patrón de la industria (ISO 19160, UPU S42): una estructura base común con reglas configurables por país. Ver [`anexo-decision-modelo-direcciones.md`](anexo-decision-modelo-direcciones.md) para la decisión documentada.

---

## Sección 4: Consumidores

### Matriz de consumo

| Catálogo | Módulo | Uso | Criticidad |
|----------|--------|-----|:----------:|
| Tipos de dirección | Terceros | Clasificar las direcciones de un tercero (fiscal, comercial, correspondencia) | Alta |
| Tipos de dirección | Estructura Organizacional | Dirección de sucursales y unidades | Alta |
| Tipos de dirección | Emisión Electrónica | Dirección fiscal obligatoria en factura electrónica | Alta |
| Formatos por país | Todos los módulos | Determinar qué campos son obligatorios al registrar una dirección según el país | Alta |
| Tipos de vía | Terceros, Emisión Electrónica | Nomenclatura codificada exigida por la DIAN para facturación electrónica en Colombia | Alta (CO) |
| Tipos de complemento | Terceros, Estructura Organizacional | Precisar la ubicación dentro de una dirección (apartamento, torre, piso) | Media |
| Códigos postales | Terceros, Impuestos, Emisión Electrónica | Validación de código postal. Exigido por DIAN (CO) y SAT (MX) en facturación electrónica | Alta (CO, MX) |

---

## Sección 5: Datos preconfigurados

### Cobertura

El servicio viene con datos precargados para los países de operación (Colombia, República Dominicana, Panamá) y los países más comunes en operaciones comerciales de LatAm (México, Estados Unidos). Extensible por el administrador para cualquier otro país.

### Datos por catálogo

| Catálogo | Cobertura precargada | Extensible |
|----------|---------------------|:----------:|
| Tipos de dirección | fiscal, comercial, correspondencia, entrega, sucursal | Sí |
| Formatos de dirección | CO, DO, PA, MX, US | Sí |
| Tipos de vía — Colombia | Calle, Carrera, Diagonal, Transversal, Avenida Calle, Avenida Carrera, Circular, Circunvalar y otros (~15 códigos DIAN) | Sí |
| Tipos de complemento | Apartamento, torre, piso, oficina, local, bodega, bloque, interior, casa | Sí |
| Códigos postales — Colombia | 3.685 códigos (fuente: DIAN/4-72) | Sí |
| Códigos postales — México | ~145.000 códigos (fuente: SAT/SEPOMEX) | Sí |
| Códigos postales — USA | ~41.000 códigos (fuente: USPS) | Sí |
| Códigos postales — Otros países | Disponibles vía GeoNames para ~120 países | Sí |

---

## Sección 6: Administración

### Responsabilidades del sistema (producto)

| Responsabilidad | Descripción |
|----------------|-------------|
| Proveer catálogos completos y listos para usar | El sistema viene predeterminado con formatos de dirección, tipos de vía, complementos y códigos postales para los países soportados. El cliente no necesita configurar nada para empezar a operar. |
| Definir reglas por país | Los campos obligatorios, tipos de vía válidos y formato de código postal de cada país están definidos por el producto. No dependen de configuración del usuario. |
| Proteger datos inmutables | Códigos ISO de países y monedas no son editables por ningún usuario. |
| Proteger registros en uso | Un registro que fue usado en una dirección no se puede eliminar — solo inactivar. |
| Registrar cambios | Toda modificación a los catálogos queda registrada con fecha y usuario. |
| Mantener códigos postales actualizados | Sincronización periódica con fuentes oficiales (DIAN/4-72, SAT/SEPOMEX, USPS). |

### Intervención del administrador (excepcional)

En casos excepcionales, el administrador puede extender los catálogos que el producto provee:

| Caso | Ejemplo |
|------|---------|
| País no soportado por el producto | Una empresa necesita operar en un país que aún no tiene formato de dirección predeterminado. |
| Tipo de dirección específico del negocio | Una empresa necesita un tipo que no está en los predefinidos (ej: "punto de venta"). |
| Inactivar un registro obsoleto | Un tipo de complemento que el negocio ya no usa. |

---

## Sección 7: Qué está dentro y fuera del alcance

### Dentro del alcance

- Estructura de dirección configurable por país con componentes estandarizados.
- Persistencia centralizada de direcciones. El servicio es la fuente de verdad — los módulos referencian direcciones y mantienen una copia local sincronizada por eventos.
- Catálogos de tipos de dirección, tipos de vía, tipos de complemento y códigos postales.
- Formatos de dirección por país que definen campos obligatorios, opcionales y validaciones.
- Catálogos predeterminados por el producto para los países soportados (CO, DO, PA, MX, US).
- Propagación controlada de cambios a los módulos consumidores.
- Extensibilidad para nuevos países y tipos por parte del administrador.
- Reglas de protección: no eliminar registros en uso, datos ISO inmutables, cambios auditables.

### Fuera del alcance

- Geocodificación (convertir dirección en coordenadas). Es un servicio externo opcional (Google Maps, Mapbox).
- Validación externa de direcciones (Google Address Validation, Loqate, SmartyStreets). Puede integrarse como capacidad futura.
- Autocompletado de direcciones en la interfaz de usuario. Es una funcionalidad de la capa de presentación, no del servicio.

### Dependencias

- **Datos de Referencia** — El servicio de Direcciones consume los catálogos de países y divisiones territoriales de Datos de Referencia para los campos de país, departamento y ciudad.

> Para la justificación de estas decisiones, alternativas evaluadas y flujo de sincronización entre módulos, ver [`anexo-decision-modelo-direcciones.md`](anexo-decision-modelo-direcciones.md).

---

## Sección 8: Beneficios esperados

### Beneficios operativos
- Cualquier módulo del ERP que necesite una dirección la registra con la misma estructura y validación, sin implementar su propia lógica.
- La dirección se registra correcta desde el primer momento — no se descubren errores después cuando otro módulo la necesita.
- Los complementos (apartamento, torre, piso, oficina) quedan estandarizados. No hay interpretaciones diferentes entre módulos.

### Beneficios de consistencia
- Una sola fuente de verdad. Cuando un tercero cambia de dirección, todos los módulos que la referencian se actualizan de forma controlada.
- Vocabulario unificado. Todos los módulos usan los mismos tipos de dirección (fiscal, comercial, correspondencia), tipos de vía y complementos.
- Se elimina el problema de SincoERP donde cada módulo extiende la entidad de dirección con atributos propios, corrompiendo la definición original.

### Beneficios de escalabilidad
- Agregar un nuevo país no requiere modificar ningún módulo — solo agregar la configuración del país en el servicio.
- Los catálogos vienen predeterminados por el producto. El cliente opera desde el primer día sin configurar formatos ni catálogos.
- Un tercero puede tener dirección de cualquier país del mundo. La estructura se adapta automáticamente según las reglas del país.

### Beneficios de cumplimiento
- Las autoridades fiscales de cada país exigen formatos específicos de dirección en documentos electrónicos. Centralizar la estructura garantiza cumplimiento desde la captura del dato, no como corrección posterior.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Abril 2026 | Versión inicial. 8 secciones: definición y justificación, glosario (8 términos), 5 catálogos, matriz de consumidores, datos preconfigurados (CO, DO, PA, MX, US), administración (sistema + excepcional), dentro/fuera del alcance con persistencia centralizada, beneficios. Anexo de decisión con 8 decisiones documentadas. |
