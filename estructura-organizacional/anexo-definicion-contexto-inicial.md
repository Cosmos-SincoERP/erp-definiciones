# Sub-dominio de Estructura Organizacional — Definición inicial

> **Fecha:** Marzo 2026
> **Propósito:** Definir el sub-dominio de Estructura Organizacional como concepto transversal del ERP, su responsabilidad, su patrón de integración con los demás sub-dominios y la decisión sobre su nomenclatura.
> **Versión:** 1.0

---

## 1. Definición

El sub-dominio de Estructura Organizacional es el registro centralizado de la estructura de unidades de la empresa a las que se asignan transacciones para efectos de control de gestión. Gestiona centros de costo, proyectos, sucursales, inmuebles, departamentos y cualquier otra unidad de la organización. Es la fuente de verdad de la pregunta "¿a qué unidad de la organización pertenece esta transacción?".

La estructura se organiza en dos niveles jerárquicos:
- **Grupo organizacional:** Agrupa unidades para organización y presentación en informes. No recibe transacciones directas.
- **Unidad organizacional:** Nivel de detalle donde se imputan las transacciones.

La jerarquía sigue una estructura de árbol con codificación que permite la navegación por niveles, similar a como funciona un plan de cuentas. Esta estructura es la base para los reportes de gestión agrupados por área, proyecto, sucursal, etc.

---

## 2. Decisión sobre el nombre

### Nombre del sub-dominio

| Término | Evaluación |
|---------|-----------|
| **Centro de costos** | Término contable/financiero. Es limitante — una unidad puede ser un proyecto, una sucursal o un inmueble, no solo un centro de costos. Genera confusión en dominios no contables (ABR, Nómina) donde el concepto se percibe como ajeno. |
| **Destino de negocio** | Término usado en OXP como Shared Kernel (DestinoDeNegocio). Descriptivo pero abstracto — no comunica que es una estructura con jerarquía y ciclo de vida. |
| **Estructura Organizacional** | Describe la responsabilidad completa: no es solo un catálogo sino una estructura con jerarquía, tipos y reestructuración. Usado por SAP (Enterprise Structure) y Oracle (Organization Structure). |

**Decisión:** El sub-dominio se nombra **Estructura Organizacional**.

### Nombres de las entidades dentro del sub-dominio

Se evaluó la nomenclatura de los ERPs líderes para los dos niveles jerárquicos:

| ERP | Agrupadora | Detalle |
|-----|-----------|---------|
| SAP | Cost Center Group | Cost Center |
| Oracle | Parent / Summary | Detail |
| Dynamics | Category | Operating Unit |
| Odoo | Analytic Group (Grupo analítico) | Analytic Account (Cuenta analítica) |

**Decisión:** Se adopta el patrón de Odoo adaptado al lenguaje de nuestro ERP:
- **Grupo organizacional** — agrupador. Organiza unidades para presentación en informes. No recibe transacciones.
- **Unidad organizacional** — detalle. Donde se imputan las transacciones.

La forma en que se modelan internamente (un solo modelo con atributo, modelos padre-hijo, o estructura de códigos con jerarquía de árbol) se define en el modelo de dominio, no en el alcance.

### Término en los sub-dominios consumidores

Todos los sub-dominios consumidores usan el término "unidad organizacional" en sus documentos de dominio para referirse al nivel de detalle donde se imputan transacciones.

| Sub-dominio | Término anterior | Término adoptado |
|-------------|-----------------|------------------|
| OXP | DestinoDeNegocio (Shared Kernel) | Unidad organizacional |
| Contabilidad | Unidad organizacional | Sin cambio |
| ABR | Centro de costos | Unidad organizacional |
| Nómina | Departamento / Centro de costos | Unidad organizacional |

**Nota:** El cambio de DestinoDeNegocio en OXP a la referencia al sub-dominio de Estructura Organizacional se debe evaluar cuando se actualice el modelo de OXP. No se aplica retroactivamente en este momento.

---

## 3. Responsabilidad del sub-dominio

| Responsabilidad | Descripción |
|----------------|-------------|
| **Creación** | Crear grupos organizacionales y unidades organizacionales, validando unicidad de código y posición en la jerarquía. Los sub-dominios consumidores pueden solicitar la creación desde sus propios flujos. |
| **Jerarquía** | Gestionar la estructura de árbol: grupos organizacionales que contienen unidades organizacionales, con codificación que permite navegación por niveles. |
| **Tipos de unidad** | Clasificación de la unidad según su naturaleza: centro de costo, proyecto, sucursal, inmueble, departamento, entre otros. El tipo permite que cada sub-dominio interprete la unidad según su contexto. |
| **Estado activo / inactivo** | Una unidad inactiva no puede usarse en nuevas transacciones. Los registros históricos que la referencian se conservan intactos. |
| **Reestructuración** | Fusión, división o traslado de unidades organizacionales. Proceso que impacta a todos los sub-dominios que referencian las unidades afectadas. |

---

## 4. Patrón de integración (EDA)

### Eventos que publica el sub-dominio

| Evento | Cuándo se publica | Datos principales |
|--------|-------------------|-------------------|
| **UnidadCreada** | Al crear una nueva unidad organizacional | Código, nombre, tipo, posición en jerarquía |
| **UnidadActualizada** | Al modificar datos de la unidad (nombre, tipo) | Código, campos modificados |
| **UnidadInactivada** | Al inactivar una unidad | Código |
| **UnidadReactivada** | Al reactivar una unidad previamente inactiva | Código |
| **UnidadReestructurada** | Al fusionar, dividir o trasladar una unidad | Código origen, código destino, tipo de reestructuración |

### Servicio de creación desde consumidores

Los sub-dominios consumidores pueden solicitar la creación de una unidad organizacional cuando lo necesiten desde sus propios flujos (ej: ABR necesita registrar un nuevo inmueble como unidad organizacional). El sub-dominio de Estructura Organizacional valida las reglas propias (unicidad de código, tipo válido, posición en jerarquía) y crea el registro.

### Consumo por sub-dominios

Cada sub-dominio que necesite unidades organizacionales:
1. Escucha los eventos relevantes
2. Almacena una referencia local (código de la unidad)
3. Valida estado activo al momento de crear transacciones

### Reestructuración como proceso orquestado

Cuando se reestructura una unidad organizacional (fusión, división, traslado), el sub-dominio publica el evento y cada sub-dominio consumidor reacciona según sus propias reglas:
- **Contabilidad:** Reclasificación contable
- **ABR:** Reasignación de inmuebles
- **Nómina:** Reasignación de empleados
- **OXP:** Actualización de la unidad en obligaciones vigentes

---

## 5. Estado del sub-dominio

Este sub-dominio **no tiene alcance ni modelo de dominio definido aún**. Este documento registra la definición inicial y el patrón de integración acordado durante la construcción del alcance de Contabilidad. La construcción formal del sub-dominio (definicion-alcance.md, modelo-dominio.md) queda como trabajo futuro.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: definición del sub-dominio, decisión de nomenclatura (grupo organizacional / unidad organizacional), responsabilidades, tipos, patrón de integración EDA, reestructuración como proceso orquestado. |
