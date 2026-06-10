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

*(En construcción)*

---

## Sección 4: Flujo principal

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
