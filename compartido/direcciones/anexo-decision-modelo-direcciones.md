# Anexo — Decisión de diseño: Modelo de direcciones

> ⚠️ **Superado (junio 2026).** La Alternativa D (servicio con persistencia centralizada) quedó reemplazada por el Nugget [`DireccionFisica`](../nuggets/direccion-fisica/especificacion.md) — en esencia, la Alternativa B reevaluada: sus dos razones de descarte ya no aplican bajo la gobernanza de Nuggets (la validación se empaqueta una sola vez, y el replanteamiento eliminó deliberadamente la fuente central). La decisión 4 (estructura genérica + configuración por país) **sigue vigente** y es la base del Nugget. Nota: la justificación para descartar la Alternativa A ("la DIAN exige tipos de vía codificados") resultó incorrecta al verificarla contra el Anexo Técnico FE v1.9 — ver Sección 8 de la especificación del Nugget.

> **Fecha:** Abril 2026
> **Propósito:** Documentar las decisiones de diseño del servicio de Direcciones, las alternativas evaluadas, la justificación de cada decisión y el flujo de sincronización entre módulos.

---

## 1. Problema a resolver

Múltiples módulos del ERP necesitan trabajar con direcciones: Terceros (dirección del proveedor/cliente), Impuestos (ubicaciones fiscales para resolución de jurisdicción), Estructura Organizacional (dirección de sucursales), OXP (dirección del proveedor), Emisión Electrónica (dirección fiscal en factura). 

Cada país tiene reglas diferentes sobre cómo se estructura una dirección, qué campos son obligatorios y qué catálogos aplican. Colombia exige tipos de vía codificados (Calle, Carrera, Diagonal) y códigos postales del catálogo DIAN. República Dominicana permite texto libre. México exige catálogos de estado, municipio y colonia del SAT.

Se necesita una solución que unifique la estructura, valide según el país y sea consumida por todos los módulos sin duplicar lógica.

---

## 2. Alternativas evaluadas

### Alternativa A — Campo de texto libre

Cada módulo almacena la dirección como un campo de texto sin estructura.

| Aspecto | Evaluación |
|---------|-----------|
| Simplicidad | Alta — no requiere catálogos ni validación |
| Facturación electrónica | No compatible — la DIAN exige tipos de vía codificados y códigos postales de catálogo oficial |
| Soporte multi-país | No — sin estructura no se puede validar por país |
| Consistencia | Baja — cada módulo interpreta la dirección a su manera |

**Descartada.** No cumple requisitos de facturación electrónica ni soporte internacional.

### Alternativa B — Value Object compartido (sin persistencia)

Una estructura de datos compartida que cada módulo importa y persiste internamente. Sin servicio central — solo un contrato de datos.

| Aspecto | Evaluación |
|---------|-----------|
| Estructura | Sí — todos usan los mismos campos |
| Validación por país | Cada módulo la implementa por su cuenta |
| Persistencia | Distribuida — cada módulo guarda su propia copia |
| Cambio de dirección | Cada módulo debe ser notificado y actualizar independientemente, sin coordinación central |

**Descartada.** La validación por país se duplica en cada módulo. No hay fuente de verdad para la creación y edición.

### Alternativa C — Sub-dominio independiente (bounded context DDD)

Las direcciones como un bounded context completo con modelo de dominio, agregados, eventos de dominio y toda la estructura DDD.

| Aspecto | Evaluación |
|---------|-----------|
| Estructura | Completa — modelo de dominio formal |
| Comportamiento de negocio | No tiene — una dirección no aprueba, no transiciona, no decide |
| Máquinas de estado | No aplica |
| Complejidad | Sobredimensionada para lo que se necesita |

**Descartada.** Las direcciones no tienen comportamiento de negocio propio que justifique un bounded context. No hay FSM, no hay domain services, no hay invariantes de negocio complejas.

### Alternativa D — Servicio compartido con persistencia centralizada ✅

Un servicio compartido que gestiona la estructura, configuración, validación y persistencia de direcciones. Los módulos las crean y editan a través del servicio, y mantienen una referencia local sincronizada por eventos.

| Aspecto | Evaluación |
|---------|-----------|
| Estructura | Sí — modelo unificado configurable por país |
| Validación por país | Centralizada — se implementa una sola vez |
| Persistencia | Centralizada — el servicio es la fuente de verdad |
| Cambio de dirección | Se edita en un solo lugar y se propaga de forma controlada |
| Complejidad | Adecuada — más que un catálogo, menos que un sub-dominio |

**Seleccionada.** Centraliza la complejidad sin sobredimensionar.

---

## 3. ¿Por qué servicio compartido y no sub-dominio?

La persistencia centralizada no convierte automáticamente un componente en sub-dominio. La persistencia es una decisión técnica, no de dominio.

Un sub-dominio se justifica cuando tiene reglas de negocio propias, comportamiento propio y procesos de negocio. Las direcciones no tienen nada de esto — son datos configurables que otros módulos consumen.

**Referencia de la industria:**
- SAP clasifica su servicio de direcciones como **BC-SRV-ADR** (Business Cross-Services), no como módulo funcional.
- Dynamics 365 lo maneja como **Global Address Book**, infraestructura transversal.
- Odoo lo embebe en **res.partner**, infraestructura compartida.

Ningún ERP lo trata como módulo de negocio independiente.

---

## 4. ¿Por qué persistencia centralizada y no distribuida?

### Cómo lo hace la industria

| ERP | Persistencia de direcciones | ¿Cada módulo copia? |
|-----|---------------------------|:-------------------:|
| SAP | Tabla única centralizada ADRC. Módulos referencian vía ADDRNUMBER. | No — referencia |
| Odoo | Tabla única res.partner. Módulos apuntan al mismo registro. | No — referencia |
| Dynamics 365 | Global Address Book centralizado. Cambios se propagan automáticamente. | No — referencia |

### Beneficios de centralizar

1. **Creación y edición en un solo lugar.** La dirección se crea y modifica en el servicio de Direcciones con toda la validación por país. Los módulos no implementan esa lógica.

2. **Validación centralizada.** Tipos de vía, códigos postales, formatos por país se validan una sola vez al crear o editar, no en cada módulo.

3. **Propagación controlada de cambios.** Cuando cambia una dirección, el servicio lo notifica de forma controlada. Sin servicio central, no hay responsable de coordinar la propagación.

4. **Consistencia del vocabulario.** Los tipos de dirección, vía y complemento se definen una vez y todos los módulos usan el mismo catálogo.

5. **Cumplimiento regulatorio.** La facturación electrónica (DIAN, SAT, DGII) exige datos de dirección en formatos específicos. Centralizar garantiza que la dirección cumple con el formato requerido desde su creación.

---

## 5. Modelo de referencia: cómo los módulos consumen las direcciones

En una arquitectura de microservicios, cada módulo tiene su propia base de datos. Los módulos no consultan al servicio de Direcciones en cada operación — mantienen una **copia local sincronizada por eventos**.

### Principio

- El servicio de Direcciones es la **fuente de verdad** para creación, edición y validación.
- Cada módulo mantiene una **referencia local** con los datos de la dirección que necesita.
- Los cambios se propagan mediante **eventos de integración** a través de un broker de mensajes.

### Flujo: creación de una dirección

```
1. SOLICITUD
   Un módulo (ej: Terceros) solicita crear una dirección
   para un proveedor a través del servicio de Direcciones.

2. VALIDACIÓN
   El servicio valida la dirección según las reglas del país:
   campos obligatorios, tipo de vía, formato de código postal.
   Si no cumple, rechaza con el detalle del error.

3. PERSISTENCIA + REGISTRO DEL CAMBIO
   El servicio guarda la dirección y registra el evento
   de creación en la misma operación. Si una falla,
   ambas se revierten. Esto garantiza que no se pueda
   guardar una dirección sin que el cambio quede registrado.

4. PUBLICACIÓN
   Un proceso automático toma los eventos pendientes
   y los publica al broker de mensajes.

5. DISTRIBUCIÓN
   El broker almacena el evento y lo entrega a todos
   los módulos suscritos.

6. ACTUALIZACIÓN EN CADA MÓDULO
   Cada módulo recibe el evento y actualiza su referencia
   local. Si ya lo procesó antes, lo descarta.
```

### Flujo: actualización de una dirección

```
1. SOLICITUD DE CAMBIO
   Un módulo solicita actualizar una dirección existente
   a través del servicio de Direcciones.

2. VALIDACIÓN
   El servicio valida los nuevos datos según las reglas
   del país.

3. PERSISTENCIA + REGISTRO DEL CAMBIO
   El servicio actualiza la dirección y registra el evento
   en la misma operación.

4. PROPAGACIÓN
   El evento se publica y todos los módulos que tienen
   referencia a esa dirección actualizan su copia local.
```

### Diagrama del flujo

```
                   ┌─────────────────────┐
                   │    Servicio de       │
   Crear/Editar ──►│    Direcciones       │
                   │                     │
                   │ • Valida por país   │
                   │ • Persiste          │
                   │ • Registra evento   │
                   └────────┬────────────┘
                            │
                            │ evento
                            ▼
                   ┌─────────────────────┐
                   │  Broker de mensajes  │
                   │  (Kafka / RabbitMQ)  │
                   └──┬──────┬───────┬───┘
                      │      │       │
                      ▼      ▼       ▼
                ┌────────┐ ┌─────┐ ┌──────────┐
                │Terceros│ │ OXP │ │Impuestos │
                │        │ │     │ │          │
                │Actualiza│ │Act. │ │Actualiza │
                │ref.local│ │ref. │ │ref.local │
                └────────┘ └─────┘ └──────────┘
```

---

## 6. Situaciones especiales

### Un módulo está caído cuando se publica el cambio

El broker de mensajes conserva el evento. Cuando el módulo vuelve a estar disponible, recibe todos los eventos que no procesó y se sincroniza automáticamente. No se pierde ningún cambio.

### Un módulo nuevo se despliega por primera vez

El módulo solicita una copia inicial de todas las direcciones que necesita al servicio de Direcciones. A partir de ese momento, escucha los cambios incrementales por eventos.

### El mismo evento llega dos veces a un módulo

Cada evento tiene un identificador único. El módulo verifica si ya lo procesó antes de aplicarlo. Si es duplicado, lo descarta sin efecto.

### Consistencia eventual

Los módulos no ven el cambio al mismo instante. Hay una ventana de milisegundos a segundos en la que un módulo puede tener la versión anterior. Esto es aceptable porque:

- Las direcciones cambian con muy poca frecuencia.
- No hay operaciones críticas que dependan de que todos los módulos vean el cambio simultáneamente.
- Es el mismo patrón que usan los ERPs cloud-native modernos.

---

## 7. Decisión sobre la estructura de la dirección

### Patrón de la industria: estructura genérica + configuración por país

La industria (ISO 19160, UPU S42, Shopify, SAP) resuelve la diversidad de formatos con un modelo de dos niveles:

**Nivel 1 — Estructura base común a cualquier país:**
Campos genéricos que toda dirección necesita: país, región/departamento, ciudad, línea de dirección, código postal, complemento.

**Nivel 2 — Configuración específica por país:**
Reglas que definen para cada país qué campos son obligatorios, qué tipos de vía aplican, qué formato tiene el código postal y en qué orden se presentan los campos.

### Por qué no un esquema diferente por país

Un esquema diferente por país (tabla separada para Colombia vs México) genera:
- Duplicación de lógica de consulta
- Complejidad al agregar un nuevo país
- Imposibilidad de tener una dirección de un país en un módulo que "no lo conoce"

Con estructura genérica + configuración, agregar un nuevo país es agregar su configuración, no modificar el modelo de datos.

### Fuentes de la decisión

| Fuente | Patrón |
|--------|--------|
| ISO 19160-4 / UPU S42 | Componentes estándar + templates por país |
| Shopify | Formato de dirección dinámico por país (campos obligatorios, orden) |
| SAP | Tabla única ADRC + formato de código postal configurable por país |
| DIAN (Colombia) | Catálogo oficial de tipos de vía (CL, CR, DG, TV, AC, AK) |
| SAT (México) | Catálogos de estado, municipio, código postal |
| DGII (Rep. Dominicana) | Formato permisivo (línea1, línea2, código postal, ciudad) |

---

## 8. Resumen de decisiones

| # | Decisión | Justificación |
|---|----------|---------------|
| 1 | Servicio compartido, no sub-dominio | No tiene comportamiento de negocio propio. SAP, Odoo, Dynamics lo clasifican como infraestructura transversal, no como módulo funcional. |
| 2 | Persistencia centralizada | Los módulos referencian, no copian. Creación, edición y validación en un solo lugar. Patrón de SAP (ADRC), Odoo (res.partner), Dynamics (GAB). |
| 3 | Sincronización por eventos | En arquitectura de microservicios, cada módulo mantiene referencia local actualizada por eventos. Consistencia eventual aceptable para datos que cambian con poca frecuencia. |
| 4 | Estructura genérica + configuración por país | Patrón ISO 19160 / UPU S42 / Shopify. Un modelo base para cualquier país, con reglas configurables por país. |
| 5 | Tipos de vía y códigos postales como catálogos del servicio | No son catálogos de Datos de Referencia porque solo tienen sentido en el contexto de una dirección. Cada país tiene sus propios catálogos (DIAN para CO, SAT para MX). |
| 6 | Tipos de dirección como catálogo centralizado | Fiscal, comercial, correspondencia, entrega, sucursal. Predefinidos por el producto, extensibles por el administrador. Patrón de SAP, Odoo, Dynamics. |
| 7 | Catálogos predeterminados por el producto | El sistema viene listo para usar. El cliente no configura formatos ni catálogos para empezar a operar. Intervención del administrador solo en casos excepcionales. |
| 8 | Códigos postales precargados | Colombia tiene 3.685 códigos (DIAN/4-72), completamente viable como precarga. GeoNames ofrece ~120 países de forma gratuita. |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Abril 2026 | Versión inicial. 8 decisiones documentadas, flujo de sincronización, alternativas evaluadas. |
