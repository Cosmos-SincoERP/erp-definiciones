# Definición de Alcance — Terceros

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

Terceros es el registro centralizado de todas las personas y empresas con las que la organización tiene relación: proveedores, clientes, empleados, entidades financieras y cualquier otra parte. Gobierna la identidad base del tercero (tipo de documento, número de identificación, razón social, tipo de empresa) y sus roles dentro del ERP.

Sigue un modelo unificado: un mismo tercero que es proveedor y cliente tiene un solo registro con múltiples roles. Cada sub-dominio consumidor enriquece al tercero con datos de su propio contexto (perfil tributario en Impuestos, condiciones comerciales en OXP) sin duplicar ni modificar la identidad base.

### Contexto actual

En SincoERP existe un módulo base de terceros, pero cada módulo que lo consume (A&F, CXP, CXC) extiende los datos del tercero con campos propios en la misma tabla. Esto corrompe la definición original de la entidad y genera versiones divergentes del mismo concepto entre módulos.

### Problema actual

1. **Corrupción de la entidad:** Cada módulo agrega atributos propios a la tabla de terceros (datos tributarios, condiciones comerciales, datos laborales). La entidad base pierde su definición clara y se convierte en una tabla monolítica con campos de múltiples contextos.
2. **Sin modelo de roles:** No hay distinción formal entre proveedor, cliente o empleado. Un tercero que es proveedor y cliente al mismo tiempo no tiene esa relación expresada de forma explícita.
3. **Sin eventos de cambio:** Cuando un tercero se inactiva o cambia de razón social, no hay un mecanismo para notificar a los módulos que lo referencian. Cada módulo descubre el cambio cuando falla una operación.
4. **Creación dispersa:** Cada módulo puede crear terceros desde su propio flujo sin pasar por un proceso centralizado de validación de unicidad y datos mínimos.
5. **Contactos sin estandarizar:** Las personas de contacto del tercero (representante legal, tesorero, comercial) se manejan de forma diferente en cada módulo.

### Implementación inicial

El piloto opera con las empresas y terceros de SincoERP en Colombia. Volúmenes estimados: ~70.000 terceros activos, ~5-10 roles por tercero, ~2-3 contactos promedio por tercero.

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
| 1 | **Tercero** | Persona natural o jurídica con la que la organización tiene relación comercial, laboral, financiera o de otro tipo. Tiene un solo registro en el sistema independientemente de cuántos roles tenga. |
| 2 | **Identificación** | Combinación de tipo de documento + número de identificación + país que identifica de forma única a un tercero en el sistema. |
| 3 | **Tipo de documento** | Clasificación del documento de identidad según el país (NIT, CC, CE en Colombia; RNC en República Dominicana; RFC en México). Proviene del catálogo de Datos de Referencia. |
| 4 | **Razón social** | Nombre legal registrado del tercero. Para personas naturales: nombres y apellidos. Para personas jurídicas: nombre de la empresa. |
| 5 | **Tipo de persona** | Clasificación base del tercero: persona (individuo) u organización (entidad constituida). Es un dato de identidad, no tributario. La clasificación tributaria detallada por país (Natural/Física/Moral, ESAL, subtipos) es responsabilidad del perfil tributario en Impuestos. |
| 6 | **Rol** | Función que cumple el tercero dentro del ERP: proveedor, cliente, empleado, entidad financiera, otro. Un tercero puede tener múltiples roles simultáneamente. Es un concepto universal (i18n), no varía por país. |
| 7 | **Contacto** | Persona asociada a un tercero con un rol específico dentro de la relación (representante legal, tesorero, comercial, contacto técnico). Tiene su propio ciclo de vida (creación, inactivación). Es un concepto universal (i18n), no varía por país. |
| 8 | **Estado del tercero** | Condición del ciclo de vida del tercero. Cuatro estados: **En Registro** (identidad registrada, pendiente de confirmación de la dirección fiscal — no operable), **Activo** (operable para nuevas transacciones), **Inactivo** (no operable, pero los registros históricos se conservan) y **Abortado** (registro nunca completó por fallo permanente; terminal, no reactivable). |
| 9 | **Dirección del tercero** | Ubicación física asociada al tercero (fiscal, comercial, correspondencia). Gestionada por el servicio de Direcciones — Terceros referencia por identificador. |
| 10 | **DV (Dígito de Verificación)** | Carácter adicional al número de documento que permite verificar la integridad del número mediante un algoritmo. En Colombia, el NIT incluye DV; otros países pueden usar verificadores análogos con distintos algoritmos (ej: cédula de identidad en Panamá, RNC en República Dominicana). Cuando aplica según el país, el DV es parte de la identificación del tercero y se valida según las reglas publicadas por el catálogo de tipos de documento (Datos de Referencia). |
| 11 | **Contacto Principal** | Contacto del tercero designado como el canal oficial de comunicación. Es obligatorio, único por tercero, y debe tener correo electrónico y teléfono registrados. La designación es ortogonal al rol del contacto (un contacto con rol "representante legal" puede además ser el principal). |

---

## Sección 3: Actores del sistema

### Actores internos (usuarios del sistema)

| Actor | Descripción | Responsabilidades |
|-------|-------------|-------------------|
| Administrador de terceros | Usuario encargado de gestionar el registro centralizado de terceros | Crear, actualizar e inactivar terceros. Gestionar roles y contactos. Resolver duplicados. |
| Usuario operativo | Usuario de cualquier módulo que necesita terceros en su flujo de trabajo | Solicitar creación de un tercero desde su módulo (ej: radicar una obligación en OXP con un proveedor nuevo). Consultar datos del tercero. |

### Actores externos (sistemas integrados)

| Sistema | Descripción | Relación con el dominio |
|---------|-------------|------------------------|
| Datos de Referencia | Catálogos de tipos de documento y países | Terceros consume tipos de documento y países para validar la identificación. |
| Direcciones | Servicio de persistencia de direcciones | Terceros referencia las direcciones del tercero (fiscal, comercial, correspondencia) por identificador. |
| Impuestos | Motor de cálculo tributario y perfiles | Impuestos escucha eventos de Terceros para gestionar el perfil tributario por país. |
| OXP | Gestión de obligaciones por pagar | OXP consume datos del tercero (identificación, razón social) y puede solicitar creación de terceros desde su flujo. |
| Contabilidad | Motor de traducción contable | Contabilidad valida que el tercero esté activo al crear borradores contables. |

> La orquestación del registro completo de un tercero (identidad + dirección + perfil tributario) se documenta en [`anexo-decision-orquestacion-registro.md`](anexo-decision-orquestacion-registro.md).

---

## Sección 4: Flujo principal

El sub-dominio de Terceros opera en seis flujos:

### Flujo 1 — Creación de un tercero (Administrador de terceros / Usuario operativo)

1. El **Administrador de terceros** o un **usuario operativo desde otro módulo** (ej: OXP al radicar una obligación con un proveedor que no existe) inicia el registro de un nuevo tercero. En ambos casos el formulario captura la información base: tipo de persona, tipo de documento, número, país, razón social y los roles que cumplirá el tercero.

2. La **capa BFF / API Composition** recibe la solicitud y distribuye los datos a cada dominio dueño: Terceros recibe la identidad base, los roles y el contacto principal; Direcciones recibe los datos de la dirección fiscal; Impuestos, Tesorería y los dominios consumidores del rol reciben lo que les corresponde una vez el tercero esté activo. Cada dominio procesa su propia porción de forma autónoma. **Terceros no orquesta a los demás dominios** — la coordinación vive fuera del sub-dominio. El detalle está en [`anexo-decision-orquestacion-registro.md`](anexo-decision-orquestacion-registro.md).

3. Terceros valida la **unicidad de la identificación** según R01 — la combinación tipo de documento + número + país debe ser única en el sistema. Si ya existe un tercero con esa identificación exacta, el registro se rechaza. Adicionalmente, si el número coincide con otro tercero registrado con distinta combinación de tipo de documento o país y la razón social es equivalente, el sistema detecta un **posible duplicado** (R01b): las vías automáticas de registro rechazan la creación y escalan al operador; un operador humano autorizado puede forzar el registro justificando la homonimia legítima.

4. Superadas las validaciones, Terceros crea el registro base y lo deja en estado **En Registro** — la identidad queda registrada pero el tercero **aún no es operable**. Los roles solicitados se asignan en este mismo momento. La notificación a los dominios consumidores del rol (OXP para proveedor, CXC para cliente, RRHH para empleado, etc.) **no se emite todavía**: ocurre solo cuando el tercero transicione a **Activo** (ver Flujo 3 y paso siguiente).

5. El tercero permanece en **En Registro** hasta que el servicio de **Direcciones** confirme asincrónicamente la creación de la dirección fiscal. Al recibir la confirmación, Terceros transiciona a **Activo** y **solo entonces** notifica a los dominios consumidores del rol. Si Direcciones falla de forma permanente tras los reintentos automáticos, el tercero pasa al estado terminal **Abortado** y la identificación queda disponible para un nuevo intento de registro (ver R16). Los datos complementarios de otros dominios (perfil tributario en Impuestos, cuentas bancarias en Tesorería, condiciones comerciales en OXP/CXC) son **enriquecimiento posterior** y no bloquean la activación del tercero — pueden fallar individualmente y completarse después sin afectar su operatividad (ver Flujo 6).

### Flujo 2 — Actualización de datos de identificación (Administrador de terceros)

1. El administrador modifica uno o más datos de identidad del tercero: razón social, tipo de documento, número, tipo de persona. Los cambios de identificación son sensibles y pueden requerir documentación de respaldo (ej: acta de cambio de razón social).

2. Terceros valida que la nueva identificación siga cumpliendo las reglas de unicidad. Si cambia tipo o número de documento, la nueva combinación no puede existir en otro tercero.

3. Terceros aplica el cambio y notifica a los dominios consumidores. Cada dominio actualiza su vista local del tercero y, cuando aplique, revalida datos dependientes (ej: Impuestos puede revalidar el perfil tributario si cambió el tipo de persona).

### Flujo 3 — Gestión de roles (Administrador de terceros / Usuario operativo)

1. Un rol puede asignarse al crear el tercero (Flujo 1) o posteriormente cuando empieza a cumplir una nueva función (ej: un cliente que pasa a ser también proveedor).

2. Terceros registra el rol en el agregado del tercero. La notificación al **dominio consumidor del rol** (OXP como proveedor, CXC como cliente, RRHH como empleado, etc.) depende del estado del tercero:

   - Si el rol se asigna durante la creación del tercero (tercero en **En Registro**), la notificación y la apertura del registro en el dominio consumidor ocurren **cuando el tercero pase a Activo** (tras la confirmación de la dirección fiscal por el servicio de Direcciones).
   - Si el rol se asigna a un tercero **ya Activo**, la notificación ocurre de inmediato y el consumidor abre su registro en ese momento.

   Cada dominio consumidor es dueño del ciclo de vida de ese registro, de los datos específicos que necesita y de determinar cuándo considera al tercero "listo para operar" en su contexto (ver Flujo 6).

3. La remoción de un rol no elimina los registros históricos asociados. Si el tercero fue proveedor y tuvo obligaciones radicadas, esos registros se conservan; solo se impide la creación de **nuevas** operaciones bajo ese rol.

### Flujo 4 — Gestión de contactos (Administrador de terceros)

1. El administrador crea, actualiza o inactiva personas de contacto asociadas al tercero (representante legal, tesorero, comercial, contacto técnico). Los contactos tienen su propio ciclo de vida — inactivar uno no afecta a los demás contactos del mismo tercero.

2. Terceros valida los datos del contacto y notifica los cambios a los dominios consumidores que los necesiten (ej: Emisión Electrónica requiere el representante legal para firmar documentos).

### Flujo 5 — Inactivación y reactivación de un tercero (Administrador de terceros)

1. El administrador solicita la inactivación cuando cesa la relación comercial o laboral con el tercero. Terceros cambia el estado a **inactivo** y lo notifica.

2. A partir de la inactivación, ningún dominio consumidor puede usar al tercero en **nuevas** transacciones. Los registros históricos se conservan intactos y los reportes siguen incluyéndolo. La validación de estado activo es responsabilidad del consumidor al iniciar una nueva operación.

3. La reactivación es posible si la relación se retoma: el administrador reactiva el tercero, se notifica el cambio y los consumidores vuelven a permitir su uso.

### Flujo 6 — Consulta del estado de completitud (Administrador de terceros / Usuario operativo)

1. El usuario abre la ficha del tercero y necesita ver si está listo para operar en un contexto específico (ej: "¿este tercero está listo como proveedor para radicar obligaciones?").

2. El sistema compone la vista en tiempo real consultando cada dominio involucrado: Terceros entrega identidad, estado del ciclo de vida (**En Registro**, **Activo**, **Inactivo** o **Abortado**), roles y referencias a direcciones; Direcciones, Impuestos, Tesorería y los dominios consumidores (OXP, CXC, etc.) entregan el estado de sus propios datos. Cada dominio consumidor es **dueño del significado de "completo"** para su caso de uso.

3. La ficha presenta una vista consolidada tipo checklist. El estado del tercero en Terceros (**En Registro**, **Activo**, **Inactivo** o **Abortado**) encabeza la información: un tercero **Activo** ya tiene identidad y dirección fiscal confirmadas por diseño (R25 + D13 del modelo). Los items que sí pueden estar pendientes aparecen como ✓ o ✗ según cada dominio consumidor: perfil tributario CO, cuenta bancaria, condiciones comerciales como proveedor, etc. El usuario puede navegar a cada servicio para completar lo faltante.

4. La vista de completitud es **informativa** — Terceros no guarda este estado. La validación de "tercero listo para operar" ocurre en cada dominio consumidor al iniciar la transacción, no en esta consulta.

---

## Sección 5: Integraciones

### Integraciones de entrada

| Origen | Dato | Propósito |
|--------|------|-----------|
| **Datos de Referencia** | Catálogo de tipos de documento por país | Validar la identificación del tercero |
| **Datos de Referencia** | Catálogo de países | Validar la identificación y país de origen |
| **Direcciones** | Confirmación asíncrona de creación de la dirección fiscal del tercero | Activar el tercero: pasar de **En Registro** a **Activo** cuando la dirección fiscal exista en Direcciones. Si la confirmación falla permanentemente tras los reintentos automáticos, el tercero se marca como **Abortado**. |
| **Sub-dominios consumidores** (OXP, CXC, RRHH, etc.) | Solicitud de creación de tercero desde sus propios flujos | Permitir que un usuario operativo cree el tercero sin salir del módulo donde trabaja |
| **Sub-dominios consumidores** | Consulta por identificación o por identificador del tercero | Resolver la identidad y obtener los datos del tercero para operar |

### Integraciones de salida

| Destino | Dato | Propósito |
|---------|------|-----------|
| **Sub-dominios consumidores** (OXP, CXC, RRHH, Impuestos, Contabilidad, Emisión Electrónica, etc.) | Notificación de tercero **activado** (no solo registrado), actualizado, inactivado o reactivado | Que cada dominio actualice su propia vista del tercero. Los consumidores se suscriben a **Tercero Activado**, no a *Tercero Registrado*, para garantizar que solo abren sus registros para terceros operables (no para terceros en **En Registro** pendientes de confirmar su dirección fiscal). |
| **Sub-dominio consumidor del rol** (OXP para proveedor, CXC para cliente, RRHH para empleado, etc.) | Notificación de rol asignado o removido (solo en terceros **Activos**) | Que el dominio consumidor abra o cierre su propio registro del tercero en ese contexto |
| **Sub-dominios consumidores** (Emisión Electrónica, OXP, CXC, etc.) | Notificación de contacto creado, actualizado o inactivado | Que cada dominio actualice su propia vista de contactos |

### Datos propios del sub-dominio

El sub-dominio de Terceros gestiona como datos propios:

- La **identidad base** del tercero: tipo de persona, tipo y número de documento, país, razón social.
- El **catálogo de roles** que cumple el tercero (proveedor, cliente, empleado, entidad financiera, otro).
- Las **personas de contacto** asociadas al tercero y su ciclo de vida.
- El **estado** del tercero en su ciclo de vida (En Registro, Activo, Inactivo, Abortado).
- Las **referencias a direcciones** gestionadas por el servicio de Direcciones (Terceros guarda el identificador, no los datos).

### Datos que NO son responsabilidad de Terceros

Para evitar la corrupción de la entidad descrita en Sección 1, estos datos viven en otros dominios:

| Dato | Responsable |
|------|-------------|
| Contenido de la dirección (calle, ciudad, código postal) | **Direcciones** |
| Perfil tributario (régimen, condición de autorretenedor, etc.) | **Impuestos** |
| Cuentas bancarias del tercero | **Tesorería** (pendiente de definir) |
| Condiciones comerciales (plazos de pago, límite de crédito, moneda) | **OXP** (como proveedor) y **CXC** (como cliente) — pendientes de definir |

### Nota sobre completitud del tercero

Terceros no gestiona un estado de completitud global. Como se definió en el Flujo 6 (Sección 4), cada dominio consumidor determina el significado de "completo" para su propio caso de uso, y el sistema compone la vista consolidada en tiempo real consultando a cada dominio involucrado.

---

## Sección 6: Reglas de negocio

Las reglas se organizan en seis frentes funcionales:

| Frente | Alcance | Reglas |
|--------|---------|:------:|
| 6.1 Identidad del tercero | Unicidad, validación y cambios de los datos de identificación | 7 |
| 6.2 Roles | Asignación, remoción, múltiples roles simultáneos | 4 |
| 6.3 Contactos | Asociación al tercero, ciclo de vida, datos mínimos, medios de comunicación, contacto principal | 5 |
| 6.4 Estado del tercero | Activación, inactivación, reactivación, validación por el consumidor | 4 |
| 6.5 Separación de responsabilidades | Frontera de qué gestiona Terceros y qué no, notificación a consumidores | 5 |
| 6.6 Direcciones | Referencias a direcciones y requisitos mínimos | 1 |

### 6.1 Identidad del tercero

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R01** | **Unicidad de la identificación:** La combinación tipo de documento + número de documento + país debe ser única en todo el sistema. No pueden existir dos terceros con la misma identificación. | No |
| **R01b** | **Detección de posibles duplicados no exactos:** Además de la unicidad exacta (R01), el sistema detecta **posibles duplicados** cuando el número de identificación coincide con el de otro tercero registrado con combinación distinta de tipo de documento o país, y la razón social es equivalente (ignorando diferencias menores de capitalización, tildes, puntuación o espacios). Caso canónico: un mismo tercero en Colombia registrado por error como CC y NIT, o con país de emisión erróneo. Ante un posible duplicado, las vías automáticas de registro (integraciones, importaciones, registro desde módulos consumidores) **rechazan la creación** y escalan a un operador humano con la información del tercero candidato. Un operador humano autorizado puede forzar el registro en casos legítimos de homonimia real (ej: personas con doble nacionalidad y documentos del mismo número en países distintos), dejando registrado el motivo del registro forzado. | No |
| **R02** | **Un solo registro por tercero:** Un mismo tercero que cumple múltiples roles (ej: proveedor y cliente) tiene un solo registro en el sistema. No se duplica por rol. | No |
| **R03** | **Validación del documento por país:** El tipo de documento informado debe existir en el catálogo de tipos de documento del país informado (ej: NIT y CC aplican en Colombia; RNC aplica en República Dominicana). El sistema rechaza identificaciones con tipos no válidos para el país. | No |
| **R04** | **Atributos y validaciones del documento definidos por el catálogo:** El catálogo de tipos de documento (Datos de Referencia) define para cada tipo: el formato del número (longitud, caracteres permitidos), los atributos adicionales que requiere (ej: **dígito de verificación DV** para el NIT en Colombia; otros verificadores según cada país como la cédula de identidad en Panamá o el RNC en República Dominicana) y las reglas para validarlos. Terceros almacena estos atributos asociados al tercero y los valida junto con la identificación, aplicando las reglas publicadas por el catálogo. El **dígito de verificación no forma parte de la clave de unicidad del tercero** — la unicidad se define por la tupla (tipo de documento + número + país) según R01. El DV es un valor derivado del número según el algoritmo del catálogo y se almacena aparte. | No |
| **R05** | **Tipo de persona como dato de identidad:** La clasificación del tercero como persona (individuo) u organización (entidad constituida) es un dato de identidad. La clasificación tributaria detallada (Natural/Física/Moral, ESAL, subtipos) es responsabilidad del perfil tributario en Impuestos, no de Terceros. | No |
| **R06** | **Cambios de identificación con trazabilidad:** Los cambios de razón social, tipo o número de documento se registran con la fecha del cambio. El historial de identificaciones anteriores se conserva para que los registros históricos en otros dominios puedan seguir haciendo referencia al tercero con la identificación vigente al momento de la transacción. | No |

### 6.2 Roles

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R07** | **Múltiples roles simultáneos:** Un tercero puede tener varios roles al mismo tiempo (proveedor y cliente, proveedor y empleado, etc.). No hay exclusión entre roles. | No |
| **R08** | **Roles universales:** El catálogo de roles (proveedor, cliente, empleado, entidad financiera, otro) es universal y no varía por país. | No |
| **R09** | **Asignación de rol dispara apertura en el dominio consumidor:** Al asignar un rol, el dominio consumidor correspondiente (OXP para proveedor, CXC para cliente, RRHH para empleado, etc.) abre su propio registro del tercero en ese contexto. La apertura ocurre cuando el tercero está **Activo**: si el rol se asigna durante la creación (tercero en **En Registro**), la apertura se difiere hasta la activación; si el rol se asigna a un tercero ya Activo, la apertura ocurre de inmediato. | No |
| **R10** | **Remoción de rol no elimina historial:** Al remover un rol de un tercero, los registros históricos asociados a ese rol en los dominios consumidores se conservan intactos. Solo se impide la creación de nuevas operaciones bajo ese rol. | No |

**Nota operativa — remover rol vs inactivar tercero:**

- **Remover un rol** aplica cuando el tercero deja de operar en un contexto específico pero sigue vigente en otros (ej: una empresa deja de ser proveedor pero sigue siendo cliente). Solo se cierra la operación en el dominio consumidor del rol removido.
- **Inactivar el tercero** aplica cuando la relación global con el tercero cesa dentro del ERP (ej: cierre definitivo de la relación comercial y laboral). Afecta a todos los dominios consumidores y bloquea nuevas operaciones en todos los contextos.

### 6.3 Contactos

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R11** | **Contacto asociado a un único tercero:** Cada contacto pertenece a un solo tercero. Una misma persona puede existir como contacto en múltiples terceros, pero cada relación es independiente. | No |
| **R12** | **Ciclo de vida independiente por contacto:** Los contactos tienen estado propio (activo/inactivo). Inactivar un contacto no afecta a los demás contactos del mismo tercero ni al estado del tercero. | No |
| **R13** | **Datos mínimos del contacto:** Todo contacto debe tener como mínimo: rol del contacto (representante legal, tesorero, comercial, técnico, contacto de facturación, contacto de notificaciones, otro) y al menos un medio de comunicación (correo electrónico o teléfono). El nombre del contacto es opcional al momento del registro y se recomienda completarlo posteriormente. | No |
| **R14** | **Medios de comunicación exclusivos del contacto:** Los medios de comunicación (correo electrónico, teléfono y similares) pertenecen a los contactos del tercero, no al tercero directamente. Para comunicaciones corporativas (facturación, notificaciones oficiales) se modela un contacto con el rol correspondiente. | No |
| **R15** | **Contacto principal obligatorio:** Todo tercero activo debe tener exactamente un contacto designado como **principal**, con correo electrónico y teléfono obligatorios (ambos). El contacto principal se designa al momento del registro y puede reasignarse posteriormente a otro contacto del tercero, pero siempre debe existir uno activo. La marca de "principal" es ortogonal al rol del contacto — puede ser representante legal, comercial u otro. | No |

### 6.4 Estado del tercero

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R16** | **Ciclo de vida en dos fases:** Todo tercero creado nace en estado **En Registro** (identidad registrada, pendiente de confirmación de dirección fiscal por el servicio de Direcciones — no operable aún). Transiciona a **Activo** cuando la dirección fiscal se confirma. Si la confirmación falla permanentemente tras los reintentos automáticos, el tercero queda en estado terminal **Abortado** (no operable, no reactivable). La identificación de un tercero Abortado queda disponible para un nuevo intento con otro registro. | No |
| **R17** | **Inactivación conserva el historial:** La inactivación de un tercero no modifica ni elimina los registros históricos que lo referencian en otros dominios. Los reportes históricos siguen incluyéndolo. | No |
| **R18** | **Solo terceros activos son operables:** Un tercero solo puede usarse en nuevas operaciones cuando su estado es **Activo**. Los estados **En Registro**, **Inactivo** y **Abortado** no son operables. La validación del estado es responsabilidad del dominio consumidor al iniciar cada nueva transacción. | No |
| **R19** | **Reactivación permitida:** Un tercero inactivado puede reactivarse si la relación comercial o laboral se retoma. Tras la reactivación, vuelve a poder usarse en nuevas operaciones. La reactivación solo aplica al estado **Inactivo** — un tercero **Abortado** no es reactivable. | No |

### 6.5 Separación de responsabilidades

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R20** | **Alcance del sub-dominio de Terceros:** Terceros se encarga únicamente de la identidad del tercero, sus roles, sus contactos, su estado del ciclo de vida (En Registro, Activo, Inactivo, Abortado) y las referencias a sus direcciones. Los demás datos del tercero viven en otros dominios: la dirección en **Direcciones**, la información tributaria en **Impuestos**, las cuentas bancarias en **Tesorería**, las condiciones comerciales en **OXP** y **CXC**. | No |
| **R21** | **Dirección del tercero gestionada por el servicio de Direcciones:** El contenido de las direcciones (calle, ciudad, código postal, etc.) lo gestiona el servicio de Direcciones. Terceros no almacena los datos de la dirección — solo conserva una referencia para consultarlos cuando se necesite. | No |
| **R22** | **Cada dominio define qué considera "tercero completo":** Terceros no guarda un estado único de completitud del tercero. Cada dominio consumidor (OXP, CXC, RRHH, Impuestos, etc.) decide qué información necesita para considerar al tercero listo para operar en su contexto y la valida contra sus propios datos. | No |
| **R23** | **Autorización para operar es responsabilidad del dominio consumidor:** Cuando un dominio consumidor inicia una operación con un tercero (radicar una obligación, emitir una factura, pagar un salario, etc.), ese dominio es el que evalúa si el tercero tiene la información necesaria. Terceros no autoriza ni bloquea operaciones de otros dominios. | No |
| **R24** | **Notificación de cambios a los dominios consumidores:** Todo cambio relevante en Terceros (creación, actualización, inactivación, reactivación, asignación o remoción de rol, gestión de contactos) se notifica a los dominios consumidores que conocen al tercero para que actualicen su información del tercero. | No |

### 6.6 Direcciones

| ID | Regla | Configurable |
|----|-------|:------------:|
| **R25** | **Dirección fiscal obligatoria en tercero Activo:** Todo tercero **Activo** debe tener al menos una referencia a dirección con tipo de uso **Fiscal**. Esta condición se exige para que el tercero pueda transicionar de **En Registro** a **Activo** mediante la confirmación asíncrona del servicio de Direcciones (ver R16 y el modelo de dominio), y debe mantenerse mientras el tercero permanezca activo. En estado **En Registro** la dirección fiscal aún puede estar pendiente de confirmación por el servicio de Direcciones. Para desreferenciar la única dirección fiscal de un tercero Activo, primero debe referenciarse otra con el mismo tipo de uso. | No |

---

## Sección 7: Qué está dentro y fuera del alcance

El sub-dominio de Terceros mantiene el registro centralizado de la identidad base de personas y empresas con las que la organización tiene relación. Su alcance cubre identidad, roles, contactos, estado y referencias a direcciones — no los datos que por definición de frontera pertenecen a otros dominios (contenido de direcciones, perfil tributario, cuentas bancarias, condiciones comerciales, datos laborales).

Las tablas a continuación detallan qué está dentro y fuera del alcance del sub-dominio.

### Dentro del alcance

> Las fases F1 y F2 se definen en la Sección 8. La pertenencia al sub-dominio no cambia — la columna indica el objetivo de implementación.

| Área | Descripción | Fase |
|------|-------------|:----:|
| **Registro centralizado de identidad** | Alta, actualización, inactivación y reactivación de terceros con identificación única (tipo de documento + número + país). Un solo registro por tercero, independientemente de los roles que cumpla. | F1 |
| **Validación por catálogo de tipos de documento** | Aplicación de las reglas de formato, DV y atributos adicionales publicadas por el catálogo de Datos de Referencia (ej: NIT con DV en Colombia, RNC en República Dominicana, cédula de identidad en Panamá). | F1 |
| **Tipo de persona como dato de identidad** | Clasificación base persona (individuo) u organización (entidad constituida), independiente de la clasificación tributaria detallada — que es responsabilidad de Impuestos. | F1 |
| **Gestión de roles** | Asignación y remoción de roles universales (proveedor, cliente, empleado, entidad financiera, otro). Múltiples roles simultáneos. La apertura del registro del tercero en el dominio consumidor ocurre cuando el tercero está **Activo**; si el rol se asignó en **En Registro**, se difiere hasta la activación. | F1 |
| **Gestión de contactos** | Alta, actualización e inactivación de contactos del tercero con rol (representante legal, tesorero, comercial, técnico, contacto de facturación, contacto de notificaciones, otro). Ciclo de vida independiente por contacto. | F1 |
| **Medios de comunicación del contacto** | Correo electrónico, teléfono y demás medios de comunicación almacenados a nivel de contacto. El tercero no tiene medios de comunicación propios — las comunicaciones corporativas se modelan con contactos por rol. | F1 |
| **Referencias a direcciones** | Conservación de los identificadores de direcciones del tercero (fiscal, comercial, correspondencia). El contenido lo gestiona el servicio de Direcciones. | F1 |
| **Historial de identificaciones** | Conservación de identificaciones anteriores tras un cambio de razón social, tipo o número de documento, para que los registros históricos de otros dominios sigan siendo válidos bajo la identificación vigente al momento de la transacción. | F1 |
| **Notificación de cambios a dominios consumidores** | Aviso a cada dominio consumidor ante creación, actualización, inactivación, reactivación, asignación o remoción de rol y gestión de contactos, para que actualicen su vista local del tercero. | F1 |
| **Solicitud de creación desde dominios consumidores** | Soporte para que un usuario operativo inicie el registro de un tercero desde el flujo de OXP, CXC u otro dominio sin salir del módulo. La coordinación del registro complementario (dirección, perfil tributario, condiciones comerciales) vive externamente al sub-dominio (ver [`anexo-decision-orquestacion-registro.md`](anexo-decision-orquestacion-registro.md)). | F1 |
| **Vista consolidada de completitud** | Consulta compuesta en tiempo real que integra identidad (Terceros), dirección (Direcciones), perfil tributario (Impuestos), cuentas bancarias (Tesorería) y condiciones comerciales (OXP, CXC) desde cada dominio dueño. Es informativa — Terceros no persiste este estado. | F1 |
| **Aprovechamiento de datos desde documentos de soporte** | Cuando el tercero aporta un documento de soporte (ej: RUT en Colombia, equivalentes según el país), el sistema puede extraer automáticamente los atributos disponibles en el documento y proponerlos al usuario, evitando la captura manual. El administrador valida los datos extraídos antes de confirmarlos. La viabilidad se evalúa por país según el estándar documental de cada uno. | F1 |
| **Importación masiva de terceros desde archivos** | Carga por lotes de identidad y roles desde archivos (Excel, CSV, etc.), con validación y reporte de resultados. El proceso de carga reparte los datos a los dominios correspondientes. | F1 |
| **Registro automático desde SincoRE** | SincoRE procesa archivos electrónicos de factura; su proceso de recepción puede registrar al tercero emisor cuando aún no existe, usando los datos confiables del archivo y repartiéndolos a cada dominio correspondiente (identidad → Terceros; perfil tributario → Impuestos; dirección fiscal → Direcciones). | F1 |
| **Registro automático desde otros procesos de recepción electrónica** | Ampliación del mismo patrón a otros sistemas de recepción presentes y futuros (recepción externa, procesos por país). | F2 |
| **Resolución de duplicados** | Capacidad para consolidar dos terceros que, habiendo sido creados como registros distintos y habiendo superado las validaciones de registro (R01 y R01b), se identifican posteriormente como la misma entidad por señales operativas (ej: razones sociales escritas muy distinto, números de documento distintos pertenecientes a la misma persona, evidencia externa). Comprende la fusión de la identidad en un tercero canónico y la **corrección de las transacciones históricas** que los referencian en los dominios consumidores (OXP, CXC, Impuestos, Contabilidad, etc.). Diseño dedicado pendiente — se aborda al iniciar F2. | F2 |

> **Principio de recepción:** Cuando una fuente externa (documento de soporte, archivo de importación, recepción electrónica) aporta datos que pertenecen a varios dominios, **el proceso de recepción es el que reparte** a cada dominio su porción correspondiente. Terceros recibe únicamente los datos de identidad y roles. El perfil tributario se entrega a Impuestos, la dirección a Direcciones, las condiciones comerciales a OXP/CXC. Terceros no acopla ni coordina a los otros dominios.

---

### Fuera del alcance del sub-dominio de Terceros

| Área | Descripción | Observación |
|------|-------------|-------------|
| **Contenido de direcciones** | Calle, ciudad, país, código postal, coordenadas. | Responsabilidad del servicio de Direcciones. Terceros solo referencia por identificador. |
| **Perfil tributario** | Régimen tributario, condición de autorretenedor, gran contribuyente, agente de retención, clasificación tributaria detallada (Natural/Física/Moral, ESAL, subtipos por país). | Responsabilidad de Impuestos. |
| **Cuentas bancarias del tercero** | Alta y gestión de cuentas bancarias para pagos o cobros. | Responsabilidad de Tesorería (sub-dominio pendiente). |
| **Condiciones comerciales** | Plazos de pago, límite de crédito, moneda de operación, descuentos. | Responsabilidad de OXP (como proveedor) y CXC (como cliente). |
| **Datos laborales** | Salario, cargo, fecha de ingreso, historial laboral, afiliaciones. | Responsabilidad de RRHH. |
| **Estado de completitud persistido** | Terceros no guarda un estado global de "tercero completo". | La vista consolidada se compone en tiempo real consultando a cada dominio dueño. Cada dominio consumidor define qué significa "completo" para su contexto. |
| **Autorización para operar** | Decidir si un tercero tiene la información necesaria para ejecutar una operación específica. | Cada dominio consumidor valida según sus propias reglas al iniciar la transacción. Terceros no autoriza ni bloquea operaciones de otros dominios. |
| **Orquestación del registro completo del tercero** | Coordinar el alta simultánea en Terceros + Direcciones + Impuestos + OXP/CXC como un único flujo visible al usuario. | Coordinada externamente al sub-dominio (ver [`anexo-decision-orquestacion-registro.md`](anexo-decision-orquestacion-registro.md)). |

---

### Dependencias externas

| Dependencia | Descripción | Impacto en el sub-dominio de Terceros |
|-------------|-------------|---------------------------------------|
| **Datos de Referencia** | Catálogo de tipos de documento y países. | Terceros valida la identificación contra las reglas publicadas por el catálogo (formato, DV, atributos requeridos). |
| **Direcciones** | Servicio de persistencia de direcciones. | Terceros guarda el identificador; las consultas de contenido van al servicio de Direcciones. |
| **Sub-dominios consumidores** (Impuestos, OXP, CXC, RRHH, Tesorería, Emisión Electrónica) | Dueños de los datos específicos del tercero en su contexto. | Alimentan la vista consolidada de completitud y son notificados de cambios relevantes en la identidad del tercero. |
| **Procesos de recepción y carga** (SincoRE, recepción electrónica, importación masiva, lectura de documentos de soporte) | Coordinan la recepción de datos externos y su reparto a los dominios dueños. | No pertenecen a Terceros — viven externamente al sub-dominio. Terceros recibe únicamente la porción de identidad y roles. |

---

## Sección 8: Estrategia de implementación por fases

El sub-dominio de Terceros conserva una visión integral de largo plazo que abarca todas las capacidades descritas en este documento. Su implementación se organiza en dos fases alineadas con la naturaleza de cada capacidad.

La Fase 1 constituye el **alcance funcional objetivo** del sub-dominio consumible por el resto del ERP. Internamente se organiza en dos bloques para claridad operativa:

- **Núcleo del sub-dominio:** capacidades cuyo desarrollo depende únicamente del equipo del BC Terceros. **No requieren coordinación con otros dominios ni con la capa BFF para construirse**. Son los prerrequisitos técnicos para la salida inicial del sub-dominio como servicio.
- **Habilitadores con dependencias externas:** capacidades que pertenecen a F1 funcional pero cuya ejecución completa **requiere coordinación externa** (servicios de otros dominios, BFF / API Composition, contratos de integración aún en definición). Su maduración puede ser progresiva y **no bloquea la salida técnica del núcleo** del BC.

### Fase 1 — Núcleo del sub-dominio

Capacidades construibles directamente por el equipo del BC Terceros, sin dependencias críticas en servicios o dominios externos más allá del consumo estándar de catálogos.

| Capacidad | Descripción |
|-----------|-------------|
| Registro centralizado de identidad | Alta del tercero en estado **En Registro**, actualización (en Activo), inactivación y reactivación con identificación única. Prevención de duplicados en origen (R01, R01b). |
| Validación por catálogo de tipos de documento | Aplicación de las reglas de formato, DV y atributos por país. Consumo estándar del catálogo de Datos de Referencia. |
| Gestión de roles | Asignación y remoción de roles universales en el agregado. |
| Gestión de contactos | Alta, actualización e inactivación con roles y medios de comunicación. |
| Historial de identificaciones | Preservación de la identificación vigente al momento de cada transacción histórica. |

### Fase 1 — Habilitadores con dependencias externas

Capacidades que pertenecen al alcance funcional de F1, pero cuya ejecución completa requiere coordinación externa: otros dominios, BFF / API Composition, contratos de integración o procesos aún no formalizados.

| Capacidad | Dependencia | Descripción |
|-----------|-------------|-------------|
| Activación del tercero y referencias a direcciones | Servicio de Direcciones | Transición de **En Registro** a **Activo** tras confirmación asíncrona de la dirección fiscal, y conservación de los identificadores de dirección en el agregado (ver D13 del modelo de dominio). |
| Notificación de cambios a dominios consumidores | Contratos formales de eventos (Fase 3 — EventCatalog del proyecto) | Aviso a consumidores (OXP, CXC, RRHH, Impuestos, etc.) ante activación, actualización, inactivación, reactivación, asignación y remoción de rol, y gestión de contactos. |
| Solicitud de creación desde dominios consumidores | Capa BFF / API Composition | Integración con el flujo operativo de OXP, CXC y otros dominios; ver [`anexo-decision-orquestacion-registro.md`](anexo-decision-orquestacion-registro.md). |
| Vista consolidada de completitud | BFF + dominios dueños (Impuestos, Direcciones, Tesorería, OXP, CXC) | Consulta compuesta en tiempo real integrando a los dominios dueños. |
| Aprovechamiento de datos desde documentos de soporte | Servicio de extracción por país (RUT y equivalentes) | Extracción automática de atributos desde documentos de respaldo con validación del administrador. |
| Importación masiva desde archivos | Proceso de ingesta externo al sub-dominio | Carga por lotes con validación y reparto a los dominios dueños. |
| Registro automático desde SincoRE | Integración con SincoRE | Proceso de recepción que reparte datos del archivo electrónico a los dominios correspondientes. |

### Fase 2 — Capacidades de extensión

Capacidades que amplían la operación del registro pero no son esenciales para el ciclo operativo básico:

| Capacidad | Descripción |
|-----------|-------------|
| Registro automático desde otros procesos de recepción electrónica | Extensión del patrón de SincoRE a otros sistemas presentes y futuros. |
| Resolución de duplicados | Consolidación de dos terceros detectados posteriormente a su registro como la misma entidad (fusión tardía). Los mecanismos de prevención R01 y R01b operan al momento del registro; esta capacidad atiende los casos que esas reglas no pueden detectar — típicamente por divergencia fuerte en la razón social o por números de documento distintos pertenecientes a la misma persona. Requiere diseño dedicado (no incluido en este documento): definición de eventos de fusión, consolidación de streams del agregado Tercero, coordinación con los dominios consumidores para corregir transacciones históricas y UI operativa. Su diseño se difiere al momento de abordar F2. |

### Criterio de éxito de la Fase 1

La Fase 1 se considera completa en dos niveles de maduración, alineados con la partición del alcance:

**Nivel A — Salida técnica del núcleo del BC Terceros**

El equipo del BC puede liberar el sub-dominio como servicio autónomo cuando:

1. Un administrador de terceros puede registrar (en estado **En Registro**), actualizar, inactivar y reactivar un tercero con identidad validada por el catálogo de tipos de documento.
2. La prevención de duplicados (R01 y R01b) opera correctamente sobre el registro normal.
3. La gestión de roles y contactos del agregado funciona conforme al modelo.
4. Los cambios de identificación conservan historial suficiente para que los registros históricos de otros dominios sigan siendo válidos.

**Nivel B — Experiencia funcional completa de F1**

La Fase 1 queda operativa de extremo a extremo cuando, además del Nivel A, se cierran las dependencias externas:

5. El servicio de Direcciones confirma la creación de la dirección fiscal y Terceros transiciona correctamente a **Activo** (o a **Abortado** ante fallo permanente).
6. Un usuario operativo desde un dominio consumidor (OXP, CXC) puede originar la creación de un tercero; la capa BFF / API Composition coordina el registro complementario hacia los dominios dueños.
7. Terceros notifica los cambios de identidad, roles y contactos (tras la activación) y los dominios consumidores actualizan su vista local mediante los contratos de integración formalizados.
8. La vista consolidada de completitud integra en tiempo real a todos los dominios dueños.
9. El proceso de recepción de SincoRE puede registrar al tercero emisor de un archivo electrónico recibido, repartiendo datos a los dominios correspondientes.
10. El administrador puede iniciar un alta a partir de un documento de soporte (RUT) o importar múltiples terceros por archivo, con los datos repartidos automáticamente a los dominios dueños.

El Nivel A puede alcanzarse independientemente del Nivel B — la salida técnica del núcleo no está bloqueada por la maduración de las dependencias externas. El Nivel B puede madurar progresivamente sin recrear el ciclo de desarrollo del núcleo.

> **Nota:** Esta sección es una decisión de alcance funcional y de implementación, no un cronograma ni plan de proyecto.

---

## Sección 9: Beneficios esperados

| # | Beneficio | Problema que resuelve |
|---|-----------|----------------------|
| 1 | **Identidad del tercero sin corromper:** La identidad base (tipo de persona, identificación, razón social, roles, contactos) es lo único que gestiona Terceros. Los datos específicos viven en el dominio dueño: perfil tributario en Impuestos, cuentas bancarias en Tesorería, condiciones comerciales en OXP y CXC. | Corrupción de la entidad (Problema 1) |
| 2 | **Modelo de roles explícito:** Un mismo tercero puede ser proveedor y cliente simultáneamente con un solo registro. Los roles se expresan de forma explícita y cada dominio consumidor abre su propio registro del tercero en ese contexto. | Sin modelo de roles (Problema 2) |
| 3 | **Notificación automática de cambios:** Los dominios consumidores se enteran de inmediato cuando un tercero cambia de razón social, se inactiva o cambia de rol, y actualizan su vista local sin que una operación falle por desconocerlo. | Sin eventos de cambio (Problema 3) |
| 4 | **Creación centralizada pero iniciada desde cualquier flujo:** El administrador de terceros y los usuarios operativos de otros módulos pueden iniciar el registro del tercero sin duplicar procesos. La validación de unicidad y los datos mínimos se aplican siempre, independientemente del origen de la solicitud. | Creación dispersa (Problema 4) |
| 5 | **Contactos estandarizados con ciclo de vida propio:** Los contactos del tercero (representante legal, tesorero, comercial, técnico, contacto de facturación, contacto de notificaciones) tienen un modelo uniforme, sus propios medios de comunicación y pueden activarse o inactivarse independientemente. | Contactos sin estandarizar (Problema 5) |
| 6 | **Historial de identificaciones preservado:** Un cambio de razón social o número de documento no rompe los registros históricos. Cada registro histórico sigue siendo válido bajo la identificación vigente al momento de la transacción. | *Trazabilidad* |
| 7 | **Aprovechamiento de datos desde documentos de soporte:** Cuando el tercero aporta un documento de respaldo (RUT en Colombia, equivalentes por país), el sistema extrae automáticamente los atributos disponibles y los propone al usuario, reduciendo la captura manual y los errores de digitación. | *Reducción de captura manual* |
| 8 | **Importación masiva con reparto automático:** La carga de terceros por lotes desde archivos se reparte automáticamente a los dominios dueños (identidad a Terceros, perfil tributario a Impuestos, dirección a Direcciones), facilitando la migración desde sistemas previos y el alta inicial a escala. | *Escalabilidad y migración* |
| 9 | **Registro automático desde recepción electrónica:** Un tercero nuevo detectado en un archivo electrónico recibido se registra automáticamente, con los datos confiables repartidos a cada dominio correspondiente. | *Automatización desde operación real* |
| 10 | **Vista consolidada de completitud sin acoplamiento:** El usuario ve en un solo lugar si el tercero está listo para operar en cada contexto (proveedor, cliente, empleado). La vista se compone en tiempo real consultando a cada dominio dueño — Terceros no se contamina con responsabilidades ajenas. | *Experiencia de usuario sin corromper la frontera* |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Abril 2026 | Versión inicial del documento de definición de alcance |
