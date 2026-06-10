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

Terceros es la **bodega consolidadora** de las personas y empresas con las que la organización tiene relación: proveedores, clientes, empleados, entidades financieras y cualquier otra parte. Construye y mantiene la **vista unificada** de cada tercero a partir de lo que los sub-dominios operativos informan, la consolida por su **clave natural** (la identificación legal: tipo de documento + número + país) y detecta duplicados y divergencias para resolverlos por conciliación.

El modelo invierte la premisa de la versión anterior: **Terceros no es la autoridad que registra y autoriza** — es el consolidador que escucha. Cada sub-dominio crea y opera al tercero **en su propio rol** (el Proveedor en OXP, el Cliente en CXC, el Empleado en RRHH) garantizando la calidad de la captura con las **validaciones empaquetadas** del producto (identificación legal, dirección física, teléfono, correo — validan localmente, sin consultar servicios). Al operar, cada dominio publica sus eventos y la bodega consolida: un mismo tercero que es proveedor y cliente aparece como **una sola entidad consolidada** con ambos roles.

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

El piloto opera con las empresas y terceros de SincoERP en Colombia. Volúmenes estimados: ~70.000 terceros consolidados, ~5-10 roles por tercero entre dominios y empresas, ~2-3 contactos promedio por tercero.

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
| 3 | **Rol** | Función que el tercero ejerce en un dominio operativo: proveedor en OXP, cliente en CXC, empleado en RRHH. Cada rol lo **captura y lo gobierna su dominio dueño** — allí es un registro propio, con sus datos y su ciclo de vida (el Proveedor de OXP) — y la bodega lo registra como parte de la vista consolidada. Los roles no se asignan en Terceros. |
| 4 | **Clave natural** | La identificación legal con la que la bodega agrupa los roles informados en un tercero consolidado: tipo de documento + número + país. |
| 5 | **Identificación legal** | Identidad documental emitida o reconocida por una autoridad (tipo de documento + número + país, con DV cuando aplica). Se valida localmente en cada dominio al capturarla, con la validación empaquetada. |
| 6 | **Validación empaquetada** | Pieza del producto que viaja incluida en cada dominio, con la estructura, las reglas y los datos estables para validar un dato transversal al capturarlo (identificación legal, dirección física, teléfono, correo) — sin consultar servicios. *En la arquitectura este empaque se llama "Nugget" y se gobierna en `compartido/nuggets/`; este documento usa el nombre funcional.* |
| 7 | **Consolidación** | Proceso por el cual la bodega agrupa los roles que comparten clave natural en un tercero consolidado y compone su vista unificada. |
| 8 | **Duplicado** | Dos terceros consolidados que corresponden a la misma entidad del mundo real pese a tener claves naturales distintas (ej: la misma persona con CC y con NIT). La bodega lo detecta; se resuelve por conciliación. |
| 9 | **Divergencia** | Desacuerdo entre los dominios que informan al mismo tercero, en un dato de identidad compartido (ej: razones sociales distintas entre OXP y CXC para el mismo NIT). La bodega la detecta; se resuelve por conciliación. |
| 10 | **Conciliación** | Proceso de resolución humana sobre duplicados y divergencias detectados: fusionar, marcar homonimia legítima o corregir el dato en el dominio de origen. Nunca bloquea la operación de los dominios. |
| 11 | **Asistencia de captura** | Consulta no bloqueante a la bodega al capturar un tercero en cualquier dominio: advierte que la identificación ya existe o se parece a una existente. El usuario decide; el dominio nunca queda impedido. |
| 12 | **Estado del tercero** | Señal global del tercero consolidado: **Activo** o **Inactivo**. La administra la bodega (único lugar donde el tercero existe completo) ante el cese global de la relación (fraude, listas restrictivas, cierre definitivo); se publica por evento y **cada dominio la aplica localmente** — la bodega no autoriza ni bloquea operación por operación. |
| 13 | **Razón social** | Nombre legal registrado del tercero. Personas naturales: nombres y apellidos; jurídicas: nombre de la empresa. |
| 14 | **Tipo de persona** | Clasificación base: persona (individuo) u organización (entidad constituida). Dato de identidad; la clasificación tributaria detallada es responsabilidad del perfil tributario en Impuestos. |
| 15 | **Contacto** | Persona asociada al tercero en una relación, con **rol de contacto** (representante legal, tesorero, comercial, técnico, facturación, notificaciones). La captura cada dominio junto con su rol del tercero — con estructura empaquetada propuesta como validación del producto (issue #35) — y la bodega la consolida. El ciclo de vida y la designación de principal viven donde se captura y se consolidan en la bodega. |
| 16 | **DV (Dígito de Verificación)** | Carácter que verifica la integridad del número de documento. Las reglas y algoritmos por país viven en la validación empaquetada de la identificación, con políticas de rechazo o advertencia según el tipo de documento. |

> **Sobre la palabra "rol":** el documento la usa en dos sentidos que siempre se califican: el **rol del tercero** (término 3: proveedor, cliente, empleado) y el **rol del contacto** (término 15: representante legal, tesorero, comercial). Nunca aparece "rol" sin contexto que indique cuál de los dos es.

---

## Sección 3: Actores del sistema

### Actores internos (usuarios del sistema)

| Actor | Descripción | Responsabilidades |
|-------|-------------|-------------------|
| **Administrador de terceros** | Usuario encargado de operar la bodega consolidadora. **Ya no crea terceros** — la captura vive en los dominios operativos. | Resolver la conciliación: decidir sobre duplicados (fusionar o marcar homonimia legítima) y divergencias (indicar el dato correcto y el dominio que debe corregir). Administrar la señal global del tercero (inactivar ante cese de relación, fraude o listas restrictivas; reactivar). Supervisar la calidad de la consolidación. |
| **Usuario operativo** | Usuario de cualquier dominio que trabaja con terceros desde su propio módulo. **No captura nada en Terceros** — captura al tercero en su rol, dentro de su dominio. | Consultar la vista consolidada del tercero (la ficha completa: roles, empresas, contactos, estado). Al capturar en su dominio, recibir y decidir sobre la asistencia de captura (advertencia no bloqueante de posibles duplicados). |

### Actores externos (sistemas integrados)

La bodega tiene dos relaciones de naturaleza distinta con los dominios: **fuentes** que la alimentan y **consumidores** que escuchan lo que ella publica. Un mismo dominio suele ser ambas cosas.

**Como fuentes (alimentan la bodega):**

| Sistema | Rol / dato que aporta | Relación |
|---------|------------------------|----------|
| **OXP** | Rol **Proveedor** (registro propio del dominio, definido en el replanteamiento) | Publica los eventos de creación, actualización e inactivación de su Proveedor, con la identificación legal y los datos validados localmente al capturar. |
| **CXC** *(futuro)* | Rol **Cliente** | Mismo patrón. |
| **RRHH** *(futuro)* | Rol **Empleado** | Mismo patrón. |
| **Impuestos** | **Perfil tributario** por identificación legal | Publica los eventos del perfil — enriquecen la vista consolidada del tercero. |

**Como consumidores (escuchan la bodega):**

| Sistema | Dato que consume | Relación |
|---------|------------------|----------|
| **Todos los dominios con roles del tercero** | Señal global de estado (Activo/Inactivo) y **resoluciones de conciliación** (correcciones de datos compartidos) | Cada dominio las aplica localmente y de forma automática: bloquea nuevas operaciones según su propia regla y corrige su registro del tercero al recibir una resolución. Injerencia por mensajes — desacoplada, nunca escritura remota. |
| **Interfaces de captura de los dominios** | Asistencia de captura | Consulta no bloqueante al capturar una identificación: ¿ya existe?, ¿se parece a una existente? El usuario decide. |
| **Contabilidad** | Señal global de estado + **resultados de conciliación** (mapa identificación → tercero canónico) | Mantiene copia local de la señal para evaluar sus reglas de datos maestros al crear borradores, y aplica el mapa canónico **en sus vistas y reportes por tercero** (auxiliares, información exógena, certificados de retención) — los asientos permanecen inmutables, la vista por tercero se presenta por el canónico al leer. Sin consulta en caliente. |
| **Emisión Electrónica** | Contactos consolidados (ej: representante legal para firma) | Consulta la vista consolidada. |

### Quiénes dejan de ser actores (cambio frente a v1.0)

| Actor v1.0 | Por qué sale |
|------------|--------------|
| **Datos de Referencia** | La validación de tipos de documento y países ya no se consulta en ejecución — viaja empaquetada con el producto en cada dominio. |
| **Direcciones** | El servicio desapareció en el replanteamiento; las direcciones se capturan en cada dominio con la validación empaquetada de direcciones. |

> **Nota — Contabilidad cambió de naturaleza, no salió:** en la v1.0 era un consumidor que "validaba el tercero activo" contra Terceros como fuente de verdad. En la v2.0 su certificación es **eventual y por suscripción**: la calidad de la captura la garantizan las validaciones empaquetadas en el origen, la vigencia la da la señal global (copia local), y la canonicidad llega por los resultados de conciliación aplicados en sus vistas y reportes por tercero — donde el dato fiscal realmente se reporta. Requiere un ajuste cruzado en los documentos de Contabilidad (issue al cerrar este alcance).

### Formatos de entrada soportados

| Formato | Origen | Contenido |
|---------|--------|-----------|
| Eventos de integración | Dominios operativos (OXP, CXC, RRHH, Impuestos) | Roles del tercero y sus cambios: identificación legal, razón social, tipo de persona, direcciones, contactos, estado del rol en su dominio. |
| Comandos de conciliación | UI de la bodega (administrador de terceros) | Resoluciones de duplicados y divergencias; administración de la señal global. |

---

## Sección 4: Flujo principal

El sub-dominio de Terceros opera en seis flujos:

| # | Flujo | Naturaleza |
|---|-------|-----------|
| 1 | Consolidación de un rol del tercero | Entrada — los dominios alimentan la bodega |
| 2 | Asistencia de captura | Servicio no bloqueante a los dominios |
| 3 | Detección y conciliación de duplicados | Conciliación — resolución humana |
| 4 | Detección y conciliación de divergencias | Conciliación — resolución humana |
| 5 | Administración de la señal global | Decisión global — aplicada localmente por cada dominio |
| 6 | Consulta de la vista consolidada | Salida — lectura de la bodega |

> Frente a la v1.0: el Flujo de creación se invierte (ya no hay registro en Terceros — Flujo 1); la validación de duplicados pasa de rechazo a advertencia (Flujo 2) y de prevención a conciliación (Flujos 3 y 4); la asignación de roles en Terceros desaparece (cada rol nace en el dominio que lo ejerce) y los contactos entran por la consolidación (se capturan en los dominios).

### Flujo 1 — Consolidación de un rol del tercero (dominios operativos → bodega)

1. Un usuario operativo crea o modifica al tercero en su rol, dentro de su dominio (ej: el Proveedor en OXP), con la captura validada localmente por las validaciones empaquetadas (identificación legal, dirección física, teléfono, correo).
2. El dominio publica el evento de integración del rol (creado, actualizado, inactivado) con la identificación legal, los datos de captura y los contactos.
3. La bodega recibe el evento y extrae la **clave natural** (tipo de documento + número + país).
4. Si no existe un tercero consolidado con esa clave, la bodega **lo crea** con ese primer rol. Si ya existe, **suma o actualiza el rol** en el consolidado.
5. La bodega evalúa señales de duplicado (¿otra clave natural parece ser la misma entidad?) y de divergencia (¿los dominios informan distinto un dato compartido?). Si hay señal, **abre un caso de conciliación** (Flujos 3 y 4) — sin afectar la operación del dominio.
6. La vista consolidada queda actualizada.

```
DOMINIO OPERATIVO (ej: OXP)                BODEGA CONSOLIDADORA (Terceros)
┌─────────────────────────┐
│ 1. Captura del          │
│    Proveedor            │
│    (validación local    │
│    empaquetada — sin    │
│    consultar a nadie)   │
└───────────┬─────────────┘
            │ 2. Evento: rol creado/actualizado
            ▼
   ┌──────────────────────────────────────────────┐
   │ 3. Extrae clave natural (ej: NIT 900123456)  │
   │ 4. ¿Existe tercero consolidado?              │
   │    ├─ No → crea el consolidado con el rol    │
   │    └─ Sí → suma/actualiza el rol             │
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
5. En ambos caminos, el dominio registra al tercero en su rol con su validación local y la operación sigue su curso normal.
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
   │        no existe      → captura sigue                │    al tercero en su rol
   │                                                      │    (validación local)
   └─ NO ──► 4. "asistencia no disponible"                │    La operación continúa
             captura continúa SIN advertencias ───────────┘
                                      │
                                      ▼
             6. La consolidación posterior (F1) detecta
                lo que la asistencia no advirtió → F3
```

### Flujo 3 — Detección y conciliación de duplicados

1. Al consolidar (Flujo 1, paso 5), la bodega detecta una **señal de duplicado**: dos terceros consolidados con claves naturales distintas parecen ser la misma entidad. Criterios heredados de la v1.0 (R01b): mismo número de documento con tipo o país distinto, y razón social equivalente en su forma canónica (ignorando mayúsculas, tildes, puntuación).
2. La bodega **abre un caso de conciliación** de tipo duplicado, con los candidatos y la evidencia. Ningún dominio se entera todavía: ambos consolidados siguen operando y visibles, marcados "en conciliación".
3. El **administrador de terceros** revisa el caso: los roles de cada candidato, sus dominios, sus datos.
4. Decide una de dos:
   - **Fusionar:** designa el tercero canónico. La bodega fusiona las vistas y **publica el resultado de conciliación con el mapa canónico** (identificación → tercero canónico). Los interesados lo aplican en sus vistas y reportes por tercero (Contabilidad presenta auxiliares y exógena por el canónico). Si el duplicado nació de un error de captura (se registró CC donde era NIT), la resolución incluye la **corrección del dato**, que los dominios con el dato errado aplican automáticamente — misma mecánica de resolución publicada del Flujo 4.
   - **Homonimia legítima:** marca que son entidades distintas. La marca queda como **memoria de conciliación**: la señal no se reabre por los mismos criterios.
5. El caso cierra con trazabilidad completa: quién decidió, cuándo y con qué motivo.

```
F1 (consolidación)
     │ señal: NIT 900123456 ≈ CC 900123456
     │        + razón social canónica equivalente
     ▼
┌─────────────────────────┐     ┌──────────────────────────────────┐
│ 2. Caso de conciliación │ ──► │ 3. Administrador revisa evidencia │
│    (tipo: duplicado)    │     └────────────┬─────────────────────┘
└─────────────────────────┘                  │ 4. decide
                         ┌───────────────────┴───────────────────┐
                         ▼                                       ▼
              FUSIONAR                              HOMONIMIA LEGÍTIMA
              · designa canónico                    · entidades distintas
              · publica mapa canónico ──► reportes  · memoria de conciliación
              · publica corrección del dato           (no se reabre)
                si hubo error de captura (F4)
```

### Flujo 4 — Detección y conciliación de divergencias

> Aplica solo a los **datos de identidad compartidos** (identificación legal, razón social, tipo de persona). Los datos propios de cada relación (direcciones de uso, contactos, condiciones) **pueden diferir legítimamente entre roles** y no son divergencias.

1. Al consolidar, la bodega detecta que un dominio informa un **dato compartido distinto** al de la vista consolidada (ej: OXP dice "Suministros XYZ S.A.S." y CXC dice "Suministros XYZ Ltda" para el mismo NIT).
2. La vista consolidada **muestra el valor más reciente con marca visible de divergencia** — no se oculta el desacuerdo, pero tampoco se bloquea nada.
3. La bodega abre el **caso de conciliación** de tipo divergencia, con las versiones y su fuente (qué dominio informó qué y cuándo).
4. El administrador determina el **dato correcto** y la bodega **publica la resolución de la conciliación** como aviso: clave natural, dato en disputa y valor correcto.
5. Cada dominio con el dato errado **aplica la corrección automáticamente en su registro del tercero** al recibir la resolución — de forma desacoplada y distribuida: la bodega nunca escribe sobre los registros de los dominios ni exige respuesta inmediata; publica, y cada dominio aplica con sus propios medios. El dato corregido regresa por el flujo normal (Flujo 1) y, cuando los dominios convergen, la bodega **cierra el caso automáticamente**.

```
OXP: "XYZ S.A.S." ──┐                       (mismo NIT)
CXC: "XYZ Ltda"  ───┤ F1 detecta divergencia en dato compartido
                    ▼
       ┌──────────────────────────┐
       │ 2. Vista: valor reciente │
       │    + marca de divergencia│
       │ 3. Caso de conciliación  │
       └───────────┬──────────────┘
                   ▼
       4. Administrador define el dato correcto
          └─► la bodega PUBLICA la resolución (aviso)
                   │
                   ▼
       5. Dominio(s) con el dato errado la aplican
          automáticamente en su registro (desacoplado)
                   └─► F1 ──► los datos convergen
                            └─► caso cierra solo
```

> **Principio de injerencia por mensajes:** la bodega sí tiene injerencia sobre los dominios operativos (correcciones de conciliación, señal global), pero siempre **publicando avisos que cada dominio aplica de forma autónoma** — nunca escritura directa sobre los registros de los dominios, nunca dependencia en línea.

### Flujo 5 — Administración de la señal global

1. El **administrador de terceros** decide el cese global de la relación con un tercero consolidado (fraude, listas restrictivas, cierre definitivo de la relación comercial y laboral) — o su reactivación si la relación se retoma.
2. La bodega cambia el estado del consolidado (**Activo → Inactivo**, o el inverso), registrando **motivo y trazabilidad** (quién, cuándo, por qué).
3. La bodega **publica la señal global** como aviso.
4. Cada dominio donde el tercero tiene un rol la **aplica localmente y de forma automática**: impide nuevas operaciones con ese tercero según su propia regla. El historial queda intacto — los registros y reportes existentes no se tocan.
5. La vista consolidada muestra el estado global y su motivo.

> **La señal global no reemplaza la inactivación por rol:** un dominio puede inactivar el rol que gobierna sin tocar los demás (el proveedor deja de serlo en OXP; el cliente sigue activo en CXC). La señal global es para el cese de la relación **completa** — y es la única decisión de alcance global, por eso vive en la bodega: el único lugar donde el tercero existe completo.

```
ADMINISTRADOR                  BODEGA                       DOMINIOS
┌──────────────────┐   ┌─────────────────────┐
│ 1. Cese global   │──►│ 2. Activo→Inactivo  │
│   (fraude, lista │   │    + motivo + traza │
│    restrictiva)  │   │ 3. Publica señal ───┼──► OXP: bloquea nuevas ops
└──────────────────┘   └─────────────────────┘    CXC: bloquea nuevas ops
                                                  RRHH: según su regla
                                                  (histórico intacto)
```

### Flujo 6 — Consulta de la vista consolidada

1. Un usuario (operativo o administrador) abre la **ficha del tercero** — desde la interfaz de la bodega o navegando desde cualquier dominio.
2. La bodega entrega la **vista consolidada completa**: identidad compartida (identificación legal, razón social, tipo de persona), estado global con su motivo, **roles por dominio y empresa** con el estado que cada dominio informó, contactos consolidados, perfil tributario (informado por Impuestos) y casos de conciliación abiertos o resueltos.
3. La vista es **de solo lectura**: para actuar sobre un rol, el usuario navega al dominio dueño. Para actuar sobre la conciliación o la señal global, el administrador opera en la bodega.
4. A diferencia de la v1.0, la ficha **no se compone consultando en vivo a cada dominio**: la bodega ya tiene los datos, consolidados por los eventos recibidos. Si la bodega no está disponible, la ficha no se puede consultar — pero **ninguna operación se afecta** (la vista es informativa; los dominios operan con sus propios datos).

```
            ┌─── FICHA DEL TERCERO (NIT 900123456) ───────────┐
            │ Identidad: Suministros XYZ S.A.S. · organización │
            │ Estado global: ACTIVO                            │
            │ ─────────────────────────────────────────────── │
            │ Roles:    Proveedor (OXP)  · activo · Empresa A  │
            │           Cliente   (CXC)  · activo · Empresa B  │
            │ Perfil tributario (Impuestos): CO completo       │
            │ Contactos: María Pérez (rep. legal) ✉ ☎          │
            │ Conciliación: sin casos abiertos                 │
            └──────────────────────────────────────────────────┘
              ▲ lectura local a la bodega (sin consultas en vivo
                a los dominios — los datos ya están consolidados)
```

---

## Sección 5: Integraciones

### Principio de responsabilidad

La bodega no produce datos de negocio: **consolida lo que los dominios informan** y produce únicamente las decisiones de alcance global (resoluciones de conciliación, señal global, mapa canónico). Todo intercambio ocurre mediante **avisos que cada parte recibe y aplica por su cuenta, sin esperar a la otra**; el único contacto directo es la asistencia de captura, que ayuda cuando está disponible y nunca bloquea (Flujo 2).

### Integraciones de entrada

| Origen | Dato | Propósito |
|--------|------|-----------|
| **OXP** (hoy) · **CXC, RRHH** (futuros) | Eventos del rol del tercero: creado, actualizado, inactivado — con identificación legal, razón social, tipo de persona, direcciones, contactos y empresa | Alimentar la consolidación (Flujo 1) |
| **Impuestos** | Eventos del perfil tributario, por identificación legal | Enriquecer la vista consolidada |

> **Las validaciones empaquetadas no son una integración de entrada:** viajan incluidas en cada dominio (también en la bodega, que valida con las mismas reglas al consolidar). Nadie consulta nada al capturar.

### Integraciones de salida

| Destino | Dato | Cómo llega | Propósito |
|---------|------|------------|-----------|
| Todos los dominios con roles del tercero | **Señal global** (Activo/Inactivo + motivo) | Aviso que cada dominio recibe y aplica por su cuenta | Cada dominio impide nuevas operaciones según su regla (Flujo 5) |
| Dominios con el dato errado | **Resolución de conciliación** (dato compartido corregido) | Aviso que el dominio aplica automáticamente en su registro del tercero | Corrección en el origen (Flujo 4) |
| **Contabilidad** y demás interesados en reportes por tercero | **Mapa canónico** (resultado de fusiones: identificación → tercero canónico) | Aviso; cada interesado lo aplica en sus vistas y reportes | Que los auxiliares, la información exógena y los certificados se presenten por el tercero canónico (Flujo 3) |
| Interfaces de captura de los dominios | **Asistencia de captura** | Consulta en línea con tiempo de espera corto; si la bodega no responde, la captura continúa | Advertir duplicados al capturar (Flujo 2) |
| Usuarios y dominios lectores (Emisión Electrónica) | **Vista consolidada** (ficha del tercero, contactos) | Consulta de lectura | Ficha completa del tercero (Flujo 6) |

### Datos propios de la bodega

- El **tercero consolidado**: la vista unificada, su estado global con motivo, sus marcas de conciliación.
- Los **casos de conciliación**: evidencia, decisiones, trazabilidad.
- La **memoria de conciliación**: homonimias legítimas marcadas (las señales no se reabren).
- El **mapa canónico**: resultado acumulado de las fusiones.

### Datos que NO son responsabilidad de la bodega

| Dato | Responsable |
|------|-------------|
| Los roles del tercero y todos sus datos de captura (el original) | El dominio dueño — la bodega guarda la copia consolidada |
| Perfil tributario | Impuestos |
| Cuentas bancarias | Tesorería (pendiente de definir) |
| Condiciones comerciales | OXP / CXC |
| Datos laborales | RRHH |
| Reglas de validación de identificación, dirección, teléfono, correo | Validaciones empaquetadas del producto (custodiadas por Datos de Referencia) |

### Diagrama de integraciones

```
  OXP            CXC           RRHH         IMPUESTOS
(Proveedor)   (Cliente)     (Empleado)      (Perfil)
    │ eventos      │ eventos     │ eventos      │ eventos
    └──────────────┴─────┬───────┴──────────────┘
                         ▼
              ┌─────────────────────┐
              │  BODEGA (Terceros)  │◄──── asistencia de captura
              │  consolida·concilia │      (consulta en línea desde los
              └──────────┬──────────┘       formularios; si no responde,
                         │                  la captura continúa)
                         │ avisos que cada dominio aplica por su cuenta
         ┌───────────────┼────────────────────┐
         ▼               ▼                    ▼
   señal global    resoluciones de      mapa canónico
   (todos los      conciliación         (Contabilidad: reportes
   dominios        (dominios corrigen   por tercero presentados
   aplican)        su registro)         por el canónico)

   Lectura: ficha consolidada → usuarios, Emisión Electrónica
```

### Notas de la primera fase

- La única fuente disponible al arranque es **OXP** (rol Proveedor, registro definido en el replanteamiento). CXC y RRHH se integran cuando esos sub-dominios se construyan — la bodega no requiere cambios para recibir nuevas fuentes que informen la misma información estándar del rol.
- La **carga histórica** (los ~70.000 terceros de SincoERP) entra por los dominios — cada uno carga sus terceros y la bodega consolida. El detalle se trata en las Secciones 7 y 8.

### Visión a futuro

- Nuevas fuentes con la misma información estándar: Tesorería (cuentas bancarias por tercero), otros dominios con roles propios.
- **Verificación de identidades contra registros oficiales** (tipo RUES/DIAN): capacidad aparte y no bloqueante, con el mismo principio — enriquece la conciliación, nunca condiciona la operación.

---

## Sección 6: Reglas de negocio

Las reglas se organizan en cinco frentes funcionales:

| Frente | Alcance | Reglas |
|--------|---------|:------:|
| 6.1 Identidad y consolidación | Clave natural, agrupación de roles, historial | 8 |
| 6.2 Conciliación y asistencia de captura | Duplicados, divergencias, resolución humana, asistencia | 7 |
| 6.3 Señal global y estado | Cese global, aplicación local, reactivación | 5 |
| 6.4 Contactos | Captura en los dominios, consolidación, contacto principal | 5 |
| 6.5 Separación de responsabilidades | Frontera bodega ↔ dominios | 5 |

### 6.1 Identidad y consolidación

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R01** | **Clave natural de consolidación:** La combinación tipo de documento + número + país agrupa los roles del tercero en un solo consolidado. Dos registros con la misma clave natural son el mismo tercero, sin importar el dominio o la empresa donde se capturaron. | No |
| **R02** | **Un solo consolidado por clave natural:** En la bodega no pueden existir dos terceros consolidados con la misma clave natural. *(La unicidad de la v1.0, que era un rechazo al usuario, ahora es una garantía interna de la bodega.)* | No |
| **R03** | **Validación en el origen:** Cada dominio valida la identificación legal al capturarla, con la validación empaquetada: tipo de documento válido para el país, formato del número y DV según la política del tipo (rechazo o advertencia). La calidad de la captura se garantiza donde se captura. | No |
| **R04** | **La bodega nunca rechaza:** Todo rol recibido se consolida. Si la bodega encuentra una anomalía al verificar (con las mismas reglas empaquetadas), la convierte en caso de conciliación — nunca en rechazo ni bloqueo del dominio. | No |
| **R05** | **El DV no es parte de la clave natural:** El dígito de verificación se almacena y valida, pero la consolidación agrupa por tipo + número + país. | No |
| **R06** | **Historial de identidad conservado:** Los cambios de identificación o razón social quedan en el historial del consolidado, para que los registros históricos de los dominios sigan siendo interpretables con la identificación vigente al momento de cada transacción. | No |
| **R07** | **Tipo de persona como dato de identidad:** Persona (individuo) u organización (entidad constituida). La clasificación tributaria detallada (Natural/Física/Moral, ESAL, subtipos por país) es responsabilidad del perfil tributario en Impuestos. | No |
| **R08** | **Un solo tercero, múltiples roles:** El tercero consolidado es único aunque ejerza varios roles. Cada rol lo informa el dominio que lo gobierna — la bodega no asigna roles. | No |

### 6.2 Conciliación y asistencia de captura

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R09** | **Detección de posibles duplicados:** Cuando el número de identificación coincide con el de otro consolidado de clave natural distinta (otro tipo de documento u otro país) y la razón social es equivalente en su forma canónica (ignorando mayúsculas, tildes, puntuación, espacios), la bodega abre un caso de conciliación. Nunca rechaza ni impide la operación. | No |
| **R10** | **La conciliación la decide un humano:** Solo el administrador de terceros resuelve fusiones, homonimias legítimas y datos correctos. La bodega detecta, reúne evidencia y propone — no decide sola. | No |
| **R11** | **Memoria de conciliación:** Una homonimia legítima marcada no vuelve a generar caso por los mismos criterios. Las decisiones de conciliación quedan con trazabilidad completa (quién, cuándo, motivo). | No |
| **R12** | **Toda fusión publica el mapa canónico:** Al fusionar duplicados, la bodega publica la correspondencia identificación → tercero canónico. Los interesados en reportes por tercero (Contabilidad: auxiliares, exógena, certificados) la aplican en sus vistas. Los registros históricos de los dominios **no se reescriben**. | No |
| **R13** | **El dato se corrige en el origen:** El dato compartido errado se corrige en el dominio que lo capturó, mediante la resolución que la bodega publica y el dominio aplica automáticamente en su registro del tercero. La bodega nunca modifica los registros de los dominios directamente. | No |
| **R14** | **Divergencia solo sobre datos de identidad compartidos:** Identificación legal, razón social y tipo de persona. Los datos propios de cada relación (direcciones de uso, contactos, condiciones) pueden diferir legítimamente entre roles y no constituyen divergencia. | No |
| **R15** | **La asistencia de captura nunca bloquea:** Ayuda cuando está disponible (advierte existentes y similares, ofrece precargar). Si no responde a tiempo, la captura continúa sin advertencias y el respaldo es la conciliación posterior. | No |

### 6.3 Señal global y estado

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R16** | **El consolidado nace Activo:** Todo tercero consolidado se crea a partir de roles que ya operan — no existe un estado intermedio de registro. Los estados del consolidado son **Activo** e **Inactivo**. | No |
| **R17** | **Inactivación global con motivo:** La señal Inactivo es decisión del administrador de terceros ante el cese global de la relación (fraude, listas restrictivas, cierre definitivo), siempre con motivo obligatorio y trazabilidad. | No |
| **R18** | **Aplicación local de la señal:** Cada dominio recibe la señal global y la aplica según su propia regla, impidiendo nuevas operaciones con el tercero en su dominio. La bodega no autoriza ni bloquea operación por operación. | No |
| **R19** | **El historial queda intacto:** La inactivación global no modifica registros históricos ni reportes existentes en ningún dominio. | No |
| **R20** | **Reactivación permitida — y la señal no reemplaza la inactivación por rol:** El administrador puede reactivar un tercero inactivo (se publica y aplica igual). Cada dominio sigue gobernando el estado del rol que le pertenece: dejar de ser proveedor en OXP no toca al cliente en CXC. La señal global existe solo para el cese de la relación **completa**. | No |

### 6.4 Contactos

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R21** | **Contactos capturados en el origen:** Cada dominio captura los contactos junto con su rol del tercero, con la estructura empaquetada del producto (propuesta en el issue #35). La bodega los consolida en la vista del tercero. | No |
| **R22** | **Rol del contacto como dato de la relación:** El rol del contacto (representante legal, tesorero, comercial, técnico, facturación, notificaciones, otro) califica la relación del contacto con el rol del tercero, y se captura en el dominio usando el vocabulario compartido del producto. | No |
| **R23** | **Datos mínimos del contacto:** Todo contacto debe tener rol de contacto y al menos un medio de comunicación (correo o teléfono). El nombre es opcional al capturar y se recomienda completarlo. *(Hereda la v1.0; la exige la estructura empaquetada en el origen.)* | No |
| **R24** | **Ciclo de vida del contacto en el origen:** Crear, actualizar o inactivar un contacto es del dominio que lo capturó; la bodega refleja el cambio en la vista consolidada, indicando el rol y dominio de origen de cada contacto. | No |
| **R25** | **Contacto principal por rol del tercero:** Cada dominio designa el contacto principal de su rol según sus necesidades (con correo y teléfono obligatorios). La bodega muestra los principales de cada rol — **no impone un principal global del tercero**. | No |

### 6.5 Separación de responsabilidades

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R26** | **Alcance de la bodega:** Consolidación, conciliación, señal global y vista unificada — nada más. Los datos de negocio del tercero viven en sus dominios dueños (perfil tributario en Impuestos, condiciones comerciales en OXP/CXC, cuentas bancarias en Tesorería, datos laborales en RRHH). | No |
| **R27** | **Los roles son soberanos — injerencia solo por mensajes:** Cada dominio es dueño del ciclo de vida y los datos del rol que gobierna. La bodega influye únicamente publicando resoluciones de conciliación y la señal global, que los dominios aplican automáticamente y de forma autónoma. Nunca escritura directa, nunca dependencia en línea. | No |
| **R28** | **La bodega nunca es prerrequisito:** Ningún dominio necesita a la bodega para operar. Su indisponibilidad solo degrada la asistencia de captura y la consulta de la ficha — jamás la operación. | No |
| **R29** | **Cada dominio define "listo para operar":** La bodega no certifica completitud ni autoriza operaciones. Cada dominio decide qué necesita para operar con el tercero y lo valida con sus propios datos. | No |
| **R30** | **Todo cambio relevante se publica:** Los dominios publican los cambios de sus roles del tercero; la bodega publica sus decisiones (señal global, resoluciones, mapa canónico). Las únicas consultas en línea son las de lectura no bloqueante (asistencia y ficha). | No |

---

## Sección 7: Qué está dentro y fuera del alcance

La bodega consolidadora mantiene la vista unificada de los terceros a partir de lo que los dominios operativos informan, concilia duplicados y divergencias, y administra la única decisión de alcance global (la señal Activo/Inactivo). Todo lo demás — capturar, validar, operar, autorizar — vive en los dominios.

### Dentro del alcance

> Las fases F1 y F2 se definen en la Sección 8.

| Área | Descripción | Fase |
|------|-------------|:----:|
| **Consolidación de roles** | Recepción de los eventos de roles del tercero de los dominios, agrupación por clave natural, creación y actualización del tercero consolidado. | F1 |
| **Detección de duplicados y divergencias** | Evaluación de señales al consolidar: claves naturales distintas que parecen la misma entidad; datos de identidad compartidos que difieren entre dominios. | F1 |
| **Conciliación con resolución humana** | Casos de conciliación con evidencia; fusión con publicación del mapa canónico; homonimia legítima con memoria; resolución de divergencias con corrección publicada al dominio de origen. | F1 |
| **Señal global del tercero** | Inactivación y reactivación del consolidado con motivo y trazabilidad; publicación para aplicación local en cada dominio. | F1 |
| **Asistencia de captura** | Consulta en línea desde los formularios de los dominios: existentes, similares, datos para precargar. Con tiempo de espera corto y sin bloquear jamás. | F1 |
| **Vista consolidada del tercero** | La ficha completa: identidad, estado global, roles por dominio y empresa, contactos, perfil tributario, casos de conciliación. Solo lectura, local a la bodega. | F1 |
| **Consolidación de contactos** | Contactos informados con cada rol, presentados en la vista con su origen y sus principales. | F1 |
| **Historial de identidad** | Conservación de los cambios de identificación y razón social del consolidado, para interpretar registros históricos con la identificación vigente al momento de cada transacción. | F1 |
| **Arranque y carga histórica** | La bodega arranca vacía y se llena consolidando los terceros que los dominios cargan en su propia migración (los ~70.000 terceros de SincoERP entran por OXP y los demás dominios, no por la bodega). Ver la nota de carga histórica. | F1 |
| **Detección ampliada de duplicados** | Criterios adicionales de similitud más allá del número + razón social canónica (ej: coincidencia de contactos o direcciones entre consolidados distintos). | F2 |
| **Verificación contra registros oficiales** | Consulta de registros públicos (tipo RUES/DIAN) para enriquecer la conciliación con evidencia externa. No bloqueante. | F2 |

> **Nota — carga histórica:** al migrar, la conciliación detectará de golpe los duplicados que ya existen en SincoERP (años de registros con CC/NIT cruzados, razones sociales divergentes entre módulos). Ese volumen inicial de casos es **esperado y deseable** — es la deuda de calidad de datos saliendo a la luz — pero exige prever capacidad operativa del administrador y herramientas de resolución por lotes. El detalle operativo se trata en la Sección 8 y en el modelo de dominio.

### Fuera del alcance del sub-dominio de Terceros

| Área | Descripción | Observación |
|------|-------------|-------------|
| **Captura y creación de terceros** | El alta de proveedores, clientes, empleados y sus datos. | Vive en cada dominio operativo, con el rol que gobierna. La bodega solo consolida. |
| **Datos de negocio del tercero** | Perfil tributario, condiciones comerciales, cuentas bancarias, datos laborales. | Impuestos, OXP/CXC, Tesorería, RRHH — cada uno es dueño en su contexto. |
| **Reglas de validación de captura** | Formato, DV, tipos de documento por país, estructura de direcciones, teléfonos y correos. | Validaciones empaquetadas del producto, custodiadas por Datos de Referencia. |
| **Autorización para operar y completitud** | Decidir si un tercero puede usarse en una operación o si está "completo". | Cada dominio valida con sus propios datos (R28, R29). |
| **Corrección de transacciones históricas** | Reescribir registros de los dominios tras una fusión o corrección. | Los registros no se reescriben; los reportes por tercero aplican el mapa canónico al leer (R12). |
| **Experiencia de captura** | Los formularios y pantallas donde se capturan los roles del tercero y sus contactos. | De cada dominio; la bodega solo expone la consulta de asistencia. |

### Dependencias externas

| Dependencia | Descripción | Impacto |
|-------------|-------------|---------|
| **OXP — rol Proveedor** | Primera fuente de la bodega (registro definido en el replanteamiento). | Sin roles publicados no hay qué consolidar: el arranque funcional de la bodega depende de esta integración. |
| **Validaciones empaquetadas** | Paquete del producto con las reglas de identificación, dirección, teléfono y correo (custodio: Datos de Referencia). | La bodega verifica con las mismas reglas con que los dominios capturan. Incluye la estructura de contacto propuesta (issue #35). |
| **Contratos de los eventos del rol** | La información estándar que toda fuente publica. | Se formalizan con el modelo de dominio y el EventCatalog (Fase 3 del proyecto). |

> Frente a la v1.0 salen de esta sección: el servicio de Direcciones (desapareció), la capa de composición para la vista (la ficha se lee de la bodega) y la "resolución de duplicados" como capacidad diferida a F2 (la conciliación es ahora el corazón del sub-dominio en F1).

---

## Sección 8: Estrategia de implementación por fases

La Fase 1 constituye el alcance funcional objetivo del sub-dominio. Se organiza en dos bloques:

- **Núcleo de la bodega:** capacidades que el equipo del BC Terceros construye solo, contra la información estándar del rol — sin esperar a ningún otro equipo.
- **Habilitadores con dependencias externas:** capacidades que requieren que otros equipos publiquen, apliquen o integren. Maduran progresivamente sin bloquear la salida técnica del núcleo.

### Fase 1 — Núcleo de la bodega

| Capacidad | Descripción |
|-----------|-------------|
| Consolidación de roles | Recepción de eventos del rol, agrupación por clave natural, creación y actualización del consolidado (R01-R08). |
| Detección de señales | Duplicados por número + razón social canónica (R09); divergencias en datos de identidad compartidos (R14). |
| Casos de conciliación | Apertura con evidencia, resolución humana (fusión / homonimia / dato correcto), memoria de conciliación, trazabilidad (R10-R13). |
| Publicación de decisiones | Mapa canónico, resoluciones de corrección, señal global — avisos que los dominios aplican por su cuenta (R27, R30). |
| Señal global | Inactivación/reactivación del consolidado con motivo (R16-R20). |
| Vista consolidada | Ficha del tercero de solo lectura, local a la bodega (Flujo 6). |
| Historial de identidad | Cambios de identificación y razón social del consolidado (R06). |

### Fase 1 — Habilitadores con dependencias externas

| Capacidad | Dependencia | Descripción |
|-----------|-------------|-------------|
| Primera fuente real | OXP (rol Proveedor) | OXP publica su rol con la información estándar; la bodega lo consolida. |
| Asistencia de captura en formularios | Interfaces de los dominios | La consulta la expone la bodega; la experiencia (advertir, precargar, continuar si no responde) la construye cada dominio en su formulario. |
| Aplicación automática de decisiones | Cada dominio fuente | Los dominios aplican la señal global y las resoluciones de conciliación en sus registros del tercero, de forma autónoma. |
| Perfil tributario en la vista | Impuestos | Impuestos publica el perfil por identificación legal y la bodega lo refleja en la ficha. |
| Reportes por tercero canónico | Contabilidad | Contabilidad consume el mapa canónico y presenta auxiliares, exógena y certificados por el tercero canónico. |
| Carga histórica | Migración por dominios | Los dominios cargan sus terceros desde SincoERP; la bodega consolida y atiende el volumen inicial de casos con **resolución por lotes** (herramienta del administrador). |
| Contratos de eventos formalizados | EventCatalog (Fase 3 del proyecto) | La información estándar del rol y los avisos de la bodega quedan como contratos formales. |

### Fase 2 — Capacidades de extensión

| Capacidad | Descripción |
|-----------|-------------|
| Detección ampliada de duplicados | Criterios adicionales de similitud: coincidencia de contactos, direcciones u otros datos entre consolidados distintos. |
| Verificación contra registros oficiales | Consulta de registros públicos (tipo RUES/DIAN) como evidencia externa para la conciliación. No bloqueante. |

> **Las nuevas fuentes no son una fase:** cuando CXC, RRHH o Tesorería se construyan, se integran publicando la misma información estándar del rol — la bodega no requiere cambios. Es crecimiento continuo, no extensión del alcance.

### Criterio de éxito de la Fase 1

**Nivel A — Salida técnica del núcleo**

1. La bodega consolida los roles recibidos, creando y actualizando consolidados por clave natural.
2. Detecta duplicados y divergencias según los criterios definidos y abre casos con su evidencia.
3. El administrador resuelve casos: la fusión publica el mapa canónico; la homonimia queda en memoria; la divergencia publica la corrección.
4. La señal global se administra con motivo y trazabilidad y se publica.
5. La ficha consolidada se consulta con identidad, roles, contactos y estado.

**Nivel B — Experiencia funcional completa de F1**

6. OXP publica el rol Proveedor y la bodega lo consolida — primera fuente real de extremo a extremo.
7. La asistencia de captura opera desde el formulario de OXP: advierte, precarga, y **si la bodega no responde la captura continúa**.
8. OXP aplica automáticamente la señal global y las resoluciones de conciliación en sus registros del tercero.
9. Impuestos enriquece la ficha con el perfil tributario.
10. Contabilidad presenta sus reportes por tercero usando el mapa canónico.
11. La carga histórica de OXP queda consolidada y el volumen inicial de casos se gestiona con la resolución por lotes.

El Nivel A puede alcanzarse sin esperar al Nivel B; el Nivel B madura progresivamente sin recrear el desarrollo del núcleo.

---

## Sección 9: Beneficios esperados

| # | Beneficio | Problema que resuelve |
|---|-----------|----------------------|
| 1 | **Identidad del tercero sin corromper:** cada dato vive en su dominio dueño (perfil tributario en Impuestos, condiciones en OXP/CXC, datos laborales en RRHH) y la bodega solo consolida — ningún módulo vuelve a extender la entidad de otro. | Corrupción de la entidad (Problema 1) |
| 2 | **Vista unificada del tercero:** por primera vez existe un lugar donde el tercero se ve completo — qué roles ejerce, en qué dominios y empresas opera, con qué contactos, con qué perfil tributario y en qué estado. | Sin vista unificada (Problema 2) |
| 3 | **Duplicados y divergencias con dueño y proceso:** la deuda de calidad de datos deja de estar oculta — la bodega la detecta, la presenta como casos con evidencia y la conciliación la resuelve con trazabilidad y memoria. | Sin detección ni conciliación (Problema 3) |
| 4 | **Los dominios se enteran solos:** la señal global y las resoluciones llegan publicadas y cada dominio las aplica automáticamente — nadie descubre un cambio cuando una operación falla. | Sin eventos de cambio (Problema 4) |
| 5 | **Contactos estandarizados desde el origen:** la estructura empaquetada y el vocabulario compartido de roles de contacto hacen que todos los dominios capturen contactos de la misma forma, y la bodega los consolide por tercero. | Contactos sin estandarizar (Problema 5) |
| 6 | **La operación nunca se encadena:** ningún dominio necesita a la bodega para operar — el acoplamiento de disponibilidad que motivó el replanteamiento desaparece por diseño. | Acoplamiento de disponibilidad (Problema 6) |
| 7 | **Reportes fiscales por tercero canónico:** los auxiliares contables, la información exógena y los certificados de retención se presentan por el tercero canónico — sin saldos fragmentados entre duplicados ni sanciones por identificaciones cruzadas. | *Integridad fiscal* |
| 8 | **Captura asistida y más rápida:** al digitar una identificación, el formulario advierte duplicados y ofrece precargar los datos consolidados — menos digitación, menos errores, menos duplicados nuevos. | *Reducción de captura manual* |
| 9 | **Historial de identidad preservado:** un cambio de razón social o de documento no rompe los registros históricos — cada transacción se interpreta con la identificación vigente en su momento. | *Trazabilidad* |
| 10 | **La calidad de datos como operación continua:** la conciliación convierte la limpieza de terceros en un proceso permanente con herramientas y responsable — no en migraciones de choque cada pocos años. | *Sostenibilidad estratégica* |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 2.0 | Junio 2026 | **Reescritura por el replanteamiento arquitectónico (#31, #33):** Terceros pasa de autoridad de registro a bodega consolidadora. En construcción. |
| 1.0 | Abril 2026 | Versión inicial (autoridad de registro). Conservada en `definicion-alcance_bk.md`. |
