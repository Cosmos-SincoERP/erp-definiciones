# Definición de Alcance — Terceros

> ℹ️ **v2.0 — En construcción (junio 2026).** Reescritura por el replanteamiento arquitectónico (issue #31): Terceros pasa de autoridad de registro a **bodega consolidadora**. La v1.0 se conserva como referencia en [`definicion-alcance_bk.md`](definicion-alcance_bk.md) mientras dura la construcción.

## Tabla de contenido

1. [Definición, Contexto actual y problema a resolver](#sección-1-definición-contexto-actual-y-problema-a-resolver)
2. [Glosario de términos](#sección-2-glosario-de-términos)
3. [Actores del sistema](#sección-3-actores-del-sistema)
4. [Flujo principal](#sección-4-flujo-principal)
5. [Integraciones](#sección-5-integraciones)
6. [Reglas de negocio](#sección-6-reglas-de-negocio)
7. [Qué está dentro y fuera del alcance](#sección-7-qué-está-dentro-y-fuera-del-alcance)
8. [Estrategia de implementación por fases](#sección-8-estrategia-de-implementación-por-fases)
9. [Beneficios esperados](#sección-9-beneficios-esperados)

---

## Sección 1: Definición, Contexto actual y problema a resolver

### Definición

Terceros es la **bodega consolidadora** de las personas y empresas con las que la organización tiene relación: proveedores, clientes, empleados, entidades financieras y cualquier otra parte. Construye y mantiene la **vista unificada** de cada tercero a partir de lo que los sub-dominios operativos informan, la consolida por su **clave natural** (la identificación legal: tipo de documento + número + país, validada por el Nugget `IdentificacionLegal`) y detecta duplicados y divergencias para resolverlos por conciliación.

El modelo invierte la premisa de la versión anterior: **Terceros no es la autoridad que registra y autoriza** — es el consolidador que escucha. Cada sub-dominio crea y opera sus propias figuras (el Proveedor en OXP, el Cliente en CXC, el Empleado en RRHH) garantizando la calidad de la captura con los **Nuggets** (identificación legal, dirección física, teléfono, correo — validación local empaquetada, sin consultar servicios). Al operar, cada dominio publica sus eventos y la bodega consolida: un mismo tercero que es proveedor y cliente aparece como **una sola entidad consolidada** con presencia en ambos contextos.

**Terceros nunca es prerrequisito para operar.** Si la bodega no está disponible, los dominios siguen creando proveedores, clientes y empleados; la consolidación se pone al día cuando la bodega procesa los eventos pendientes. La asistencia de duplicados al capturar es **no bloqueante**: advierte, no impide.

### Contexto actual

En SincoERP existe un módulo base de terceros, pero cada módulo que lo consume (A&F, CXP, CXC) extiende los datos del tercero con campos propios en la misma tabla, corrompiendo la entidad y generando versiones divergentes del mismo concepto.

Adicionalmente, el diseño v1.0 de este sub-dominio (registro centralizado como autoridad) reveló en implementación un **acoplamiento de disponibilidad** contrario a la arquitectura de microservicios asíncronos: si Terceros no estaba disponible, OXP no podía operar; crear un tercero exigía consultar Datos de Referencia y esperar la confirmación asíncrona de Direcciones. El replanteamiento de junio 2026 (issue #31) resolvió la transversalidad con **distribución en lugar de dependencia** — y este documento es la consecuencia de ese giro.

### Problema actual

1. **Corrupción de la entidad** *(heredado de SincoERP)*: cada módulo agrega atributos propios a la tabla de terceros; la entidad pierde su definición.
2. **Sin vista unificada:** no existe un lugar donde ver al tercero completo — qué roles cumple, en qué empresas opera, con qué datos lo conoce cada módulo.
3. **Sin detección de duplicados ni divergencias:** el mismo tercero registrado dos veces (CC y NIT, o con razones sociales distintas entre módulos) no se detecta ni se concilia.
4. **Sin eventos de cambio:** cuando la identidad de un tercero cambia en un módulo, los demás lo descubren cuando una operación falla.
5. **Contactos sin estandarizar** *(heredado)*: las personas de contacto se manejan distinto en cada módulo.
6. **Acoplamiento de disponibilidad** *(del diseño v1.0)*: hacer del registro un prerrequisito centralizado encadena la operación de todos los dominios a la disponibilidad de uno.

### Implementación inicial

El piloto opera con las empresas y terceros de SincoERP en Colombia. Volúmenes estimados: ~70.000 terceros consolidados, ~5-10 presencias por tercero entre dominios y empresas, ~2-3 contactos promedio por tercero.

### Nomenclatura del sub-dominio

Se evaluaron varios términos antes de adoptar **Terceros** como nombre oficial:

| Término | Quién lo usa | Evaluación |
|---------|--------------|-----------|
| **Contactos** | Alegra, Odoo, Holded, Xero | Dominante en ERPs cloud-native. Descartado porque en este modelo un contacto es un componente **dentro** del tercero (persona con rol, correo, teléfono). Confunde el todo con la parte. |
| **Business Partner** | SAP S/4HANA | Estándar internacional en inglés. Sin traducción natural al español. |
| **Party** | Oracle Fusion (TCA), Dynamics 365 | Técnico; "partes" en español suena jurídico. |
| **Terceros** | Siigo, Helisa, World Office, DIAN, PUC | Estándar en contabilidad colombiana y latinoamericana. Universalmente entendido en el contexto fiscal y contable de Colombia, República Dominicana y Panamá. |
| **Entidades** | DGII (RD), algunos ERPs | Se confunde con "entidad legal" (la empresa misma). |
| **Directorio** | — | Neutro pero abstracto; no dice "de quién". |

**Decisión:** el sub-dominio se nombra **Terceros**. La presentación en la UI (menú, navegación) se define por separado — el nombre técnico no tiene que coincidir con el de presentación.

---

## Sección 2: Glosario de términos

| # | Término | Definición |
|---|---------|-----------|
| 1 | **Tercero** | Persona natural o jurídica con la que la organización tiene relación. En la bodega es la **entidad consolidada**: una sola, sin importar cuántos dominios o empresas la conozcan. |
| 2 | **Bodega consolidadora** | Naturaleza del sub-dominio: recibe los eventos de los dominios operativos, agrupa por clave natural y mantiene la vista unificada del tercero. Nunca es prerrequisito para que un dominio opere. |
| 3 | **Figura** | La materialización del tercero en un dominio operativo: el Proveedor en OXP, el Cliente en CXC, el Empleado en RRHH. Cada figura la crea y la gobierna su dominio dueño; la bodega la registra como parte de la vista consolidada. |
| 4 | **Clave natural** | La identificación legal con la que la bodega agrupa figuras en un tercero consolidado: tipo de documento + número + país. |
| 5 | **Identificación legal** | Identidad documental emitida o reconocida por una autoridad (tipo de documento + número + país, con DV cuando aplica). Validada localmente en cada dominio por el Nugget `IdentificacionLegal`. |
| 6 | **Nugget** | Pieza transversal empaquetada y versionada (estructura + reglas + datos estables embebidos) con la que cada dominio garantiza la calidad de la captura sin consultar servicios en ejecución. Gobernados en `compartido/nuggets/`. |
| 7 | **Consolidación** | Proceso por el cual la bodega agrupa las figuras que comparten clave natural en un tercero consolidado y compone su vista unificada. |
| 8 | **Duplicado** | Dos terceros consolidados que corresponden a la misma entidad del mundo real pese a tener claves naturales distintas (ej: la misma persona con CC y con NIT). La bodega lo detecta; se resuelve por conciliación. |
| 9 | **Divergencia** | Desacuerdo entre figuras del mismo tercero en un dato compartido (ej: razones sociales distintas entre OXP y CXC para el mismo NIT). La bodega la detecta; se resuelve por conciliación. |
| 10 | **Conciliación** | Proceso de resolución humana sobre duplicados y divergencias detectados: fusionar, marcar homonimia legítima o corregir el dato en el dominio de origen. Nunca bloquea la operación de los dominios. |
| 11 | **Asistencia de captura** | Consulta no bloqueante a la bodega al capturar un tercero en cualquier dominio: advierte que la identificación ya existe o se parece a una existente. El usuario decide; el dominio nunca queda impedido. |
| 12 | **Estado del tercero** | Señal global del tercero consolidado: **Activo** o **Inactivo**. La administra la bodega (único lugar donde el tercero existe completo) ante el cese global de la relación (fraude, listas restrictivas, cierre definitivo); se publica por evento y **cada dominio la aplica localmente** — la bodega no autoriza ni bloquea operación por operación. |
| 13 | **Razón social** | Nombre legal registrado del tercero. Personas naturales: nombres y apellidos; jurídicas: nombre de la empresa. |
| 14 | **Tipo de persona** | Clasificación base: persona (individuo) u organización (entidad constituida). Dato de identidad; la clasificación tributaria detallada es responsabilidad del perfil tributario en Impuestos. |
| 15 | **Rol** | Función que el tercero cumple dentro del ERP (proveedor, cliente, empleado, entidad financiera). **Se deriva de las figuras**: un tercero es proveedor porque tiene figura de Proveedor en OXP. No se asigna en Terceros. |
| 16 | **Contacto** | Persona asociada al tercero en una relación, con rol de contacto (representante legal, tesorero, comercial, técnico, facturación, notificaciones). La captura cada dominio junto con su figura — con estructura empaquetada (Nugget `Contacto`, propuesto en el issue #35) — y la bodega la consolida. El ciclo de vida y la designación de principal viven donde se captura y se consolidan en la bodega. |
| 17 | **DV (Dígito de Verificación)** | Carácter que verifica la integridad del número de documento. Las reglas y algoritmos por país viven en el Nugget `IdentificacionLegal`, con políticas de rechazo o advertencia según el tipo de documento. |

---

## Sección 3: Actores del sistema

### Actores internos (usuarios del sistema)

| Actor | Descripción | Responsabilidades |
|-------|-------------|-------------------|
| **Administrador de terceros** | Usuario encargado de operar la bodega consolidadora. **Ya no crea terceros** — la captura vive en los dominios operativos. | Resolver la conciliación: decidir sobre duplicados (fusionar o marcar homonimia legítima) y divergencias (indicar el dato correcto y el dominio que debe corregir). Administrar la señal global del tercero (inactivar ante cese de relación, fraude o listas restrictivas; reactivar). Supervisar la calidad de la consolidación. |
| **Usuario operativo** | Usuario de cualquier dominio que trabaja con terceros desde su propio módulo. **No captura nada en Terceros** — captura su figura en su dominio. | Consultar la vista consolidada del tercero (la ficha completa: figuras, empresas, contactos, estado). Al capturar en su dominio, recibir y decidir sobre la asistencia de captura (advertencia no bloqueante de posibles duplicados). |

### Actores externos (sistemas integrados)

La bodega tiene dos relaciones de naturaleza distinta con los dominios: **fuentes** que la alimentan y **consumidores** que escuchan lo que ella publica. Un mismo dominio suele ser ambas cosas.

**Como fuentes (alimentan la bodega):**

| Sistema | Figura / dato que aporta | Relación |
|---------|--------------------------|----------|
| **OXP** | Figura **Proveedor** (agregado propio del dominio, definido en el replanteamiento) | Publica los eventos de creación, actualización e inactivación de su figura, con la identificación legal y los datos de captura validados localmente por los Nuggets. |
| **CXC** *(futuro)* | Figura **Cliente** | Mismo patrón. |
| **RRHH** *(futuro)* | Figura **Empleado** | Mismo patrón. |
| **Impuestos** | **Perfil tributario** por identificación legal | Publica los eventos del perfil — enriquecen la vista consolidada del tercero. |

**Como consumidores (escuchan la bodega):**

| Sistema | Dato que consume | Relación |
|---------|------------------|----------|
| **Todos los dominios con figuras** | Señal global de estado (Activo/Inactivo) | Cada dominio la aplica localmente: bloquea nuevas operaciones con su figura según su propia regla. |
| **Interfaces de captura de los dominios** | Asistencia de captura | Consulta no bloqueante al capturar una identificación: ¿ya existe?, ¿se parece a una existente? El usuario decide. |
| **Contabilidad** | Señal global de estado + **resultados de conciliación** (mapa identificación → tercero canónico) | Mantiene copia local de la señal para evaluar sus reglas de datos maestros al crear borradores, y aplica el mapa canónico **en sus proyecciones por tercero** (auxiliares, información exógena, certificados de retención) — los asientos permanecen inmutables, la vista por tercero se canoniza al leer. Sin consulta en caliente. |
| **Emisión Electrónica** | Contactos consolidados (ej: representante legal para firma) | Consulta la vista consolidada. |

### Quiénes dejan de ser actores (cambio frente a v1.0)

| Actor v1.0 | Por qué sale |
|------------|--------------|
| **Datos de Referencia** | La validación de tipos de documento y países ya no se consulta en ejecución — viaja empaquetada en los Nuggets (`IdentificacionLegal`, `Pais`). |
| **Direcciones** | El servicio desapareció en el replanteamiento; las direcciones se capturan en cada dominio con el Nugget `DireccionFisica`. |

> **Nota — Contabilidad cambió de naturaleza, no salió:** en la v1.0 era un consumidor que "validaba el tercero activo" contra Terceros como fuente de verdad. En la v2.0 su certificación es **eventual y por suscripción**: la calidad de la captura la garantizan los Nuggets en el origen, la vigencia la da la señal global (copia local), y la canonicidad llega por los resultados de conciliación aplicados en sus proyecciones — donde el dato fiscal realmente se reporta. Requiere un ajuste cruzado en los documentos de Contabilidad (issue al cerrar este alcance).

### Formatos de entrada soportados

| Formato | Origen | Contenido |
|---------|--------|-----------|
| Eventos de integración | Dominios operativos (OXP, CXC, RRHH, Impuestos) | Figuras del tercero y sus cambios: identificación legal, razón social, tipo de persona, direcciones, contactos, estado de la figura. |
| Comandos de conciliación | UI de la bodega (administrador de terceros) | Resoluciones de duplicados y divergencias; administración de la señal global. |

---

## Sección 4: Flujo principal

El sub-dominio de Terceros opera en seis flujos:

| # | Flujo | Naturaleza |
|---|-------|-----------|
| 1 | Consolidación de una figura | Entrada — los dominios alimentan la bodega |
| 2 | Asistencia de captura | Servicio no bloqueante a los dominios |
| 3 | Detección y conciliación de duplicados | Conciliación — resolución humana |
| 4 | Detección y conciliación de divergencias | Conciliación — resolución humana |
| 5 | Administración de la señal global | Decisión global — aplicada localmente por cada dominio |
| 6 | Consulta de la vista consolidada | Salida — lectura de la bodega |

> Frente a la v1.0: el Flujo de creación se invierte (ya no hay registro en Terceros — Flujo 1); la validación de duplicados pasa de rechazo a advertencia (Flujo 2) y de prevención a conciliación (Flujos 3 y 4); la gestión de roles desaparece (el rol se deriva de las figuras) y la de contactos entra por la consolidación (se capturan en los dominios).

### Flujo 1 — Consolidación de una figura (dominios operativos → bodega)

1. Un usuario operativo crea o modifica su figura en su dominio (ej: el Proveedor en OXP), con la captura validada localmente por los Nuggets (identificación legal, dirección física, teléfono, correo).
2. El dominio publica el evento de integración de su figura (creada, actualizada, inactivada) con la identificación legal, los datos de captura y los contactos.
3. La bodega recibe el evento y extrae la **clave natural** (tipo de documento + número + país).
4. Si no existe un tercero consolidado con esa clave, la bodega **lo crea** con esa primera figura. Si ya existe, **suma o actualiza la figura** en el consolidado.
5. La bodega evalúa señales de duplicado (¿otra clave natural parece ser la misma entidad?) y de divergencia (¿las figuras informan distinto un dato compartido?). Si hay señal, **abre un caso de conciliación** (Flujos 3 y 4) — sin afectar la operación del dominio.
6. La vista consolidada queda actualizada.

```
DOMINIO OPERATIVO (ej: OXP)                BODEGA CONSOLIDADORA (Terceros)
┌─────────────────────────┐
│ 1. Captura de la figura │
│    Proveedor            │
│    (Nuggets validan     │
│    localmente, sin red) │
└───────────┬─────────────┘
            │ 2. Evento: figura creada/actualizada
            ▼
   ┌──────────────────────────────────────────────┐
   │ 3. Extrae clave natural (ej: NIT 900123456)  │
   │ 4. ¿Existe tercero consolidado?              │
   │    ├─ No → crea el consolidado con la figura │
   │    └─ Sí → suma/actualiza la figura          │
   │ 5. ¿Señales de duplicado o divergencia?      │
   │    └─ Sí → abre caso de conciliación (F3/F4) │
   │ 6. Vista consolidada actualizada             │
   └──────────────────────────────────────────────┘
```

> **Si la bodega no está disponible**, los eventos de los dominios quedan pendientes de entrega y se procesan cuando vuelva. La operación de los dominios nunca se entera.

### Flujo 2 — Asistencia de captura (no bloqueante, con degradación controlada)

1. El usuario digita la identificación de un tercero en el formulario de **su dominio** (ej: nuevo proveedor en OXP).
2. La interfaz consulta la asistencia de la bodega con un **tiempo de espera corto**.
3. **Camino normal — la bodega responde:**
   - **Existe exacto:** la interfaz muestra el tercero conocido y puede ofrecer **precargar** los datos consolidados (razón social, contactos), reduciendo la captura.
   - **Existe similar** (posible duplicado): advertencia con los candidatos; el usuario decide continuar con su captura o usar el existente.
   - **No existe:** la captura sigue sin más.
4. **Camino degradado — la bodega no responde a tiempo o no está disponible:** la interfaz lo indica discretamente ("asistencia no disponible") y **permite continuar la captura sin advertencias**. La captura nunca se bloquea.
5. En ambos caminos, el dominio registra su figura con la validación local de los Nuggets y la operación sigue su curso normal.
6. **Red de seguridad:** al consolidar (Flujo 1), la bodega detecta el duplicado que la asistencia no alcanzó a advertir y abre el caso de conciliación (Flujo 3).

```
FORMULARIO DEL DOMINIO            BODEGA
┌────────────────────┐
│ 1. Digita NIT      │
└───────┬────────────┘
        │ 2. Consulta (espera corta)
        ▼
   ¿Responde la bodega?
   ├─ SÍ ──► existe exacto  → muestra/precarga datos      ┐
   │        existe similar → advierte, usuario decide     │ 5. El dominio registra
   │        no existe      → captura sigue                │    su figura (Nuggets)
   └─ NO ──► 4. "asistencia no disponible"                │    La operación continúa
             captura continúa SIN advertencias ───────────┘
                                      │
                                      ▼
             6. La consolidación posterior (F1) detecta
                lo que la asistencia no advirtió → F3
```

### Flujo 3 — Detección y conciliación de duplicados

*(En construcción)*

### Flujo 4 — Detección y conciliación de divergencias

*(En construcción)*

### Flujo 5 — Administración de la señal global

*(En construcción)*

### Flujo 6 — Consulta de la vista consolidada

*(En construcción)*

---

## Sección 5: Integraciones

*(En construcción)*

---

## Sección 6: Reglas de negocio

*(En construcción)*

---

## Sección 7: Qué está dentro y fuera del alcance

*(En construcción)*

---

## Sección 8: Estrategia de implementación por fases

*(En construcción)*

---

## Sección 9: Beneficios esperados

*(En construcción)*

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 2.0 | Junio 2026 | **Reescritura por el replanteamiento arquitectónico (#31, #33):** Terceros pasa de autoridad de registro a bodega consolidadora. En construcción. |
| 1.0 | Abril 2026 | Versión inicial (autoridad de registro). Conservada en `definicion-alcance_bk.md`. |
