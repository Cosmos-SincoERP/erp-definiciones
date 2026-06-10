# Gobernanza de Nuggets

## Tabla de contenido

1. [Definición y justificación](#sección-1-definición-y-justificación)
2. [Definición canónica](#sección-2-definición-canónica)
3. [Criterios de admisión](#sección-3-criterios-de-admisión)
4. [Nomenclatura](#sección-4-nomenclatura)
5. [Especificación de un Nugget](#sección-5-especificación-de-un-nugget)
6. [Proceso de gobernanza](#sección-6-proceso-de-gobernanza)
7. [Versionado y evolución](#sección-7-versionado-y-evolución)
8. [Distribución](#sección-8-distribución)
9. [Roles y responsabilidades](#sección-9-roles-y-responsabilidades)
10. [Relación con los servicios y sub-dominios](#sección-10-relación-con-los-servicios-y-sub-dominios)

---

## Sección 1: Definición y justificación

### El problema que los Nuggets resuelven

La arquitectura del ERP es de microservicios que se comunican de forma asíncrona y eventual. Bajo esa filosofía, el diseño inicial concentró las reglas y los datos transversales en servicios centrales de consulta: Datos de Referencia publicaba las reglas de identificación por país, Direcciones resolvía las direcciones postales, y Terceros actuaba como autoridad de registro de toda persona o empresa. El resultado fue un acoplamiento en tiempo de ejecución contrario a la propia arquitectura: si Terceros no está disponible, OXP no puede operar; crear un tercero desde cualquier sub-dominio exigía consultar Datos de Referencia; y el registro debía esperar la confirmación asíncrona de Direcciones para activarse.

El equipo técnico identificó este patrón durante la implementación: **la transversalidad se estaba resolviendo con dependencia, cuando debía resolverse con distribución.**

### La solución

Un **Nugget** materializa la transversalidad sin dependencia: la estructura, las reglas de validación y los datos de referencia estables de un concepto compartido se empaquetan y se versionan como una pieza que cada sub-dominio incorpora dentro de su propio proceso. La validación es local y síncrona; no hay red, no hay espera, no hay servicio que pueda estar caído.

Este documento define qué es un Nugget, qué requisitos debe cumplir un concepto para serlo, cómo se nombra, cómo se especifica, quién lo aprueba y cómo evoluciona. Su objetivo es doble:

1. **Evitar la proliferación**: que no se creen Nuggets nuevos para conceptos que ya existen en el catálogo.
2. **Mantener la calidad**: que cada Nugget sea muy pequeño, muy bien nombrado por el contexto al que corresponde, y especificado con el mismo rigor que un modelo de dominio.

---

## Sección 2: Definición canónica

> Un **Nugget** es un Value Object transversal del ERP: una pieza mínima de estructura + reglas de validación + datos de referencia estables, empaquetada y versionada, que cualquier sub-dominio incorpora sin dependencia en tiempo de ejecución. Un Nugget no tiene identidad, ni ciclo de vida, ni estado, ni emite eventos. Es un concepto del lenguaje ubicuo del ERP, no de un sub-dominio particular.

### Qué contiene un Nugget

| Componente | Descripción | Ejemplo (`IdentificacionLegal`) |
|------------|-------------|---------------------------|
| **Estructura** | Los atributos que componen el concepto y sus tipos. | `tipoDocumento`, `numero`, `pais`, `digitoVerificacion` |
| **Reglas de validación** | Las reglas que determinan si un valor es válido, ejecutables sin salir del proceso. | Formato del número según tipo y país; algoritmo del dígito de verificación |
| **Datos de referencia embebidos** | Los catálogos estables que las reglas necesitan, congelados en la versión del paquete. | Tipos de documento por país, lista de países |

### Qué NO es un Nugget

- **No es un agregado**: no tiene identidad ni ciclo de vida. Si el concepto necesita estados, historia o eventos, pertenece a un sub-dominio.
- **No es un servicio**: no se consulta por red. Si la validación necesita un dato vivo (una tasa de cambio del día, una verificación contra un sistema externo), esa parte queda fuera del Nugget.
- **No es una librería utilitaria**: no agrupa funciones técnicas sueltas. Cada Nugget es un único concepto de negocio con nombre propio en el glosario del ERP.
- **No es configurable por inquilino**: sus reglas y datos son parte del producto. Lo que un administrador pueda extender en operación no pertenece al Nugget.

---

## Sección 3: Criterios de admisión

Una propuesta entra al catálogo solo si pasa **los seis filtros**. El filtro 6 es la regla anti-proliferación: se evalúa contra el catálogo completo antes que cualquier otro trabajo.

| # | Filtro | Pregunta de control | Si falla |
|---|--------|---------------------|----------|
| 1 | **Transversal** | ¿Lo necesitan 2 o más sub-dominios? | Es un VO local del sub-dominio que lo necesita. |
| 2 | **Sin identidad** | ¿Se compara por valor, sin identificador ni ciclo de vida? | Es un agregado o entidad de algún sub-dominio. |
| 3 | **Autocontenido** | ¿Valida sin salir del proceso — sin red, sin base de datos, sin reloj externo? | La parte no autocontenida se separa: queda en un servicio de datos vivos. |
| 4 | **Estable** | ¿Sus reglas y datos cambian por versión del producto, no por operación diaria? | Es un dato vivo: pertenece a Datos de Referencia (capacidad Sync) o al sub-dominio dueño. |
| 5 | **Mínimo** | ¿Representa un solo concepto del lenguaje ubicuo? | Se divide en los conceptos que realmente contiene. |
| 6 | **No duplicado** | ¿Ningún Nugget existente cubre el concepto ni puede extenderse para cubrirlo? | Se extiende el Nugget existente o se rechaza la propuesta con razón documentada. |

### Prueba ácida servicio vs. Nugget

**¿El concepto necesita ciclo de vida, estado propio o datos vivos?**

- **No** → estructura + reglas + datos estables → **Nugget**. Ejemplo: la dirección postal (estructura por país y divisiones territoriales estables).
- **Sí** → capacidad de un servicio o sub-dominio. Ejemplo: las tasas de cambio (dato diario, capacidad Sync de Datos de Referencia).

---

## Sección 4: Nomenclatura

1. **El nombre es el término del glosario canónico del ERP**, en español, por concepto de negocio. Nunca por dominio consumidor ni por tecnología: `IdentificacionLegal`, no `TerceroIdentificacion` ni `DocumentoUtil`. El nombre debe ser inequívoco **fuera** de cualquier agregado: `Identificacion` a secas era claro dentro del agregado Tercero, pero como pieza transversal necesita el calificador que dice qué la distingue (la emite una autoridad).
2. **El contexto califica el nombre cuando el concepto tiene variantes.** Cada variante produce un Nugget distinto y pequeño, en lugar de un Nugget grande con banderas internas: `DireccionFisica` (y, si algún día existe, `DireccionElectronica` sería otro Nugget).
3. **El calificador se toma del lenguaje natural de los países de operación, no de convenciones traducidas.** "Dirección física" es como la gente distingue la dirección de un lugar frente a la electrónica; "dirección postal" es un calco de *postal address* que en Colombia desvía el significado hacia el correo físico. Ante la duda, la palabra que el negocio ya usa gana sobre la convención internacional.
4. **Convención de escritura**: carpeta en kebab-case (`direccion-fisica/`), Value Object en PascalCase (`DireccionFisica`) — igual que el resto del proyecto.
5. **Sin abreviaturas ni siglas** salvo las consagradas en el glosario del ERP.
6. **El nombre se valida en la revisión** (Sección 6): un Nugget mal nombrado se corrige antes de publicarse, porque después del primer consumidor el nombre queda fijado.

---

## Sección 5: Especificación de un Nugget

Cada Nugget vive en su carpeta dentro de `compartido/nuggets/` con esta estructura:

```
compartido/nuggets/<nombre-nugget>/
├── especificacion.md      ← el contrato del Nugget
└── datos/                 ← JSON embebidos, si aplica (producidos por Datos de Referencia)
```

La `especificacion.md` debe contener, como mínimo:

| Sección | Contenido |
|---------|-----------|
| **Concepto** | Qué representa, en una definición de negocio. Referencia al término del glosario del ERP. |
| **Atributos** | Tabla de atributos: nombre, tipo, obligatoriedad, descripción. |
| **Reglas de validación** | Cada regla numerada (`[V01]`, `[V02]`…), con su comportamiento por país cuando aplique. |
| **Datos embebidos** | Qué catálogos incluye, su origen (Datos de Referencia), conteo y fecha de corte. |
| **Ejemplos por país** | Valores válidos e inválidos para cada país de la Fase 1 (CO, DO, PA). |
| **Fuera de responsabilidad** | Qué NO hace el Nugget — tan importante como lo que sí hace. Aquí se documenta la parte del concepto que quedó en servicios (ej: verificación postal externa). |
| **Consumidores** | Sub-dominios que lo incorporan (se mantiene sincronizada con la matriz del catálogo). |
| **Control de versiones** | Historial de cambios del Nugget, alineado con el versionado del paquete (Sección 7). |

---

## Sección 6: Proceso de gobernanza

El ciclo de vida de un Nugget tiene cinco pasos. La gobernanza es deliberadamente liviana — un rol custodio, una plantilla con filtros y un catálogo índice — porque el riesgo real (la proliferación de gemelos) se controla en el filtro de entrada, no con burocracia posterior.

```
 Propuesta ──► Revisión ──► Especificación ──► Publicación ──► Evolución
 (issue       (custodio     (especificacion    (entra al        (versionado
  tipo:        aplica los    .md con rigor      catálogo y al    semántico,
  nugget)      6 filtros)    de modelo)         paquete)         Sección 7)
```

### Paso 1 — Propuesta

Cualquier sub-dominio o el equipo técnico propone un Nugget mediante un **issue con etiqueta `tipo: nugget`**. La propuesta obliga a:

- Responder los seis filtros de la Sección 3.
- Declarar contra qué Nuggets existentes del catálogo se comparó (filtro 6).
- Nombrar los sub-dominios que lo necesitan (filtro 1).

### Paso 2 — Revisión

El **custodio del catálogo** (Sección 9) valida los filtros — en especial el 6 — y el nombre propuesto (Sección 4). El veredicto es uno de tres:

- **Aceptar**: el concepto entra al catálogo en estado `Aceptado — en especificación`.
- **Extender**: el concepto ya está cubierto parcialmente; se amplía el Nugget existente (sigue el versionado de la Sección 7).
- **Rechazar**: con la razón documentada en el issue. Las razones de rechazo quedan como memoria del catálogo para no re-evaluar lo mismo dos veces.

### Paso 3 — Especificación

Se redacta la `especificacion.md` según la Sección 5, con el mismo rigor de un modelo de dominio. Si el Nugget embebe datos, Datos de Referencia produce los JSON correspondientes con su fecha de corte.

### Paso 4 — Publicación

El Nugget entra al `catalogo-nuggets.md` en estado `Publicado` con su versión inicial, y se incorpora al paquete distribuible (Sección 8). A partir de este momento los sub-dominios pueden adoptarlo; cada adopción se registra en la matriz de consumidores del catálogo.

### Paso 5 — Evolución

Todo cambio posterior sigue las reglas de versionado de la Sección 7 y se gestiona como issue (`tipo: nugget`). Antes de cambiar un Nugget se consulta la matriz de consumidores para conocer el impacto.

---

## Sección 7: Versionado y evolución

El conjunto de Nuggets se versiona de forma **semántica y unificada** a nivel del paquete (Sección 8), con un historial por Nugget en su especificación.

| Tipo de cambio | Versión | Regla de adopción |
|----------------|---------|-------------------|
| Agregar datos embebidos (nuevo país, nueva entrada de catálogo) | **Menor** | Los sub-dominios actualizan cuando quieran. Los valores ya almacenados no se ven afectados. |
| Agregar un atributo opcional o una regla que solo amplía lo válido | **Menor** | Igual que el anterior. |
| Cambiar una regla de validación, un atributo existente o restringir lo válido | **Mayor** | Requiere plan de adopción coordinado entre los consumidores, anotado en el catálogo antes de publicar. |
| Nuevo Nugget en el catálogo | **Menor** | No afecta a quien no lo consume. |

### Reglas de evolución

1. **Nada se elimina.** Un Nugget que deja de tener sentido se marca `Obsoleto` en el catálogo, con su reemplazo indicado. Los consumidores migran a su ritmo; el catálogo registra quiénes faltan.
2. **Los códigos embebidos son semánticos e inmutables** (misma política de los catálogos fiscales del producto): un tipo de documento `NIT` o un país `CO` nunca cambia de código, porque los valores históricos almacenados en los sub-dominios los referencian.
3. **Los valores históricos no se revalidan.** Cuando una regla cambia (versión mayor), aplica a los valores que se capturen de ahí en adelante. Lo almacenado bajo reglas anteriores conserva su validez histórica — el estado se reconstruye del stream de eventos tal como ocurrió.
4. **Un cambio mayor exige razón normativa o de producto documentada** en el issue que lo origina (ej: un país cambió el algoritmo del dígito de verificación).

---

## Sección 8: Distribución

Los Nuggets se distribuyen como **un único paquete versionado** para todo el ERP, no como un paquete por Nugget.

| Decisión | Justificación |
|----------|---------------|
| Paquete único | Todos los sub-dominios compilan contra el mismo conjunto coherente de conceptos transversales. Evita matrices de compatibilidad entre Nuggets y simplifica la gestión de dependencias. |
| Versionado a nivel de paquete | Una sola versión que adoptar por servicio; el historial por Nugget vive en cada especificación. |
| Datos embebidos en el paquete | La validación es autocontenida (filtro 3). Actualizar los datos = adoptar una versión menor del paquete. |

> **Sugerencia de implementación:** en el stack .NET del proyecto, el paquete se materializa como un paquete NuGet (ej: `Cosmos.Nuggets`). La definición de cadencia de publicación y del repositorio de paquetes es del equipo técnico — este documento gobierna el contenido, no el mecanismo.

---

## Sección 9: Roles y responsabilidades

| Rol | Quién | Responsabilidades |
|-----|-------|-------------------|
| **Custodio del catálogo** | El dueño natural es quien gobierna Datos de Referencia. Es un rol, no un comité. | Aplicar los seis filtros a cada propuesta; velar por la nomenclatura; mantener `catalogo-nuggets.md` al día; coordinar los planes de adopción de cambios mayores; producir y versionar los datos embebidos. |
| **Proponente** | Cualquier sub-dominio o el equipo técnico. | Presentar la propuesta completa (issue `tipo: nugget` con los seis filtros respondidos y la comparación contra el catálogo). |
| **Consumidor** | Cada sub-dominio que adopta un Nugget. | Registrar su adopción en la matriz del catálogo; participar en los planes de adopción de cambios mayores que lo afecten; no duplicar localmente conceptos ya cubiertos por el catálogo. |

---

## Sección 10: Relación con los servicios y sub-dominios

### Datos de Referencia

Deja de ser un servicio de consulta de reglas y catálogos estables, y su capacidad principal pasa a ser la **producción de catálogos**: producir, verificar y versionar los datos que los Nuggets embeben, ejercida por el custodio. Conserva como capacidad propia de servicio únicamente los **datos vivos** (tasas de cambio vía Sync), que por el filtro 4 no caben en un Nugget.

### Direcciones

Desaparece como servicio. Su concepto se materializa en el Nugget `DireccionFisica` (estructura por país + divisiones territoriales embebidas). La verificación o normalización contra servicios externos — si algún día se necesita — será una capacidad aparte y **no bloqueante**: enriquece después, nunca condiciona un registro.

### Terceros

Deja de ser autoridad de registro y pasa a ser **bodega consolidadora**: cada sub-dominio garantiza localmente el "manual de creación" de una persona o empresa usando los Nuggets, y la bodega consolida por la clave natural (el Nugget `IdentificacionLegal`), detecta duplicados y divergencias, y gestiona su conciliación con resolución humana. La bodega nunca es prerrequisito para operar.

### Sub-dominios operativos (OXP, Impuestos, CXC, …)

Incorporan los Nuggets para validar localmente, sin dependencia en tiempo de ejecución. Cada uno es dueño de su representación del tercero con el fin que le corresponde (proveedor en OXP, perfil tributario en Impuestos, cliente en facturación) y alimenta a la bodega con sus eventos de registro y actualización.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.2 | Junio 2026 | Nomenclatura: nuevo criterio 3 — el calificador se toma del lenguaje natural de los países de operación, no de convenciones traducidas (surgido del renombre `DireccionPostal` → `DireccionFisica`: "postal" es calco del inglés). Ejemplos actualizados. |
| 1.1 | Junio 2026 | Nomenclatura reforzada (Sección 4): el nombre debe ser inequívoco **fuera** de cualquier agregado — criterio surgido del renombre `Identificacion` → `IdentificacionLegal` (primer caso real de aplicación de la gobernanza). Ejemplos actualizados al nuevo nombre. |
| 1.0 | Junio 2026 | Versión inicial. 10 secciones: definición y justificación, definición canónica, 6 criterios de admisión, nomenclatura, especificación mínima, proceso de gobernanza en 5 pasos (rol custodio + issue `tipo: nugget`), versionado semántico con 4 reglas de evolución, distribución como paquete único, 3 roles, y relación con Datos de Referencia / Direcciones / Terceros / sub-dominios operativos. Surge del replanteamiento arquitectónico de junio 2026 (acoplamiento en tiempo de ejecución detectado por el equipo técnico durante la implementación). |
