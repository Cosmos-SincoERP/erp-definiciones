# Definición de Alcance — Datos de Referencia

> ℹ️ **v2.0 — Transformado por el replanteamiento de junio 2026.** El servicio de consulta descrito en la v1.x quedó superado: sus 4 catálogos estáticos se distribuyen ahora como **Nuggets** (`compartido/nuggets/`) y nadie los consulta en tiempo de ejecución. Lo que permanece son dos capacidades de naturaleza distinta: la **producción de catálogos** (producir, verificar y versionar los datos embebidos en los Nuggets — a cargo del custodio) y el **servicio de tasas de cambio** (el único dato vivo). El mapa completo de disposición v1 → v2 está en la Sección 2.

## Tabla de contenido

1. [Definición y justificación](#sección-1-definición-y-justificación)
2. [Mapa de disposición v1 → v2](#sección-2-mapa-de-disposición-v1--v2)
3. [Capacidad 1: Producción de catálogos](#sección-3-capacidad-1--producción-de-catálogos)
4. [Capacidad 2: Tasas de cambio](#sección-4-capacidad-2-tasas-de-cambio)
5. [Habilitación por inquilino](#sección-5-habilitación-por-inquilino)
6. [Qué está dentro y fuera del alcance](#sección-6-qué-está-dentro-y-fuera-del-alcance)

---

## Sección 1: Definición y justificación

### Definición

Datos de Referencia es el componente del ERP responsable de los datos de referencia transversales, con dos capacidades:

1. **Producción de catálogos** *(sin runtime)*: produce, verifica y versiona los catálogos estables que los Nuggets embeben — países, monedas, divisiones territoriales, tipos de documento de identidad, perfiles de dirección, tipos de vía/complemento, códigos postales. El dueño de este componente ejerce el rol de **custodio del catálogo de Nuggets** definido en la [gobernanza](../nuggets/gobernanza-nuggets.md).
2. **Servicio de tasas de cambio** *(runtime)*: gestiona el único dato de referencia vivo — las tasas de cambio diarias — sincronizándolas desde fuentes oficiales y distribuyéndolas a los dominios.

### Por qué cambió (v1 → v2)

La v1.x definía un servicio de consulta: los sub-dominios le preguntaban en tiempo de ejecución para validar países, monedas, tipos de documento y divisiones. El equipo técnico detectó en implementación que ese patrón creaba acoplamiento de disponibilidad (crear un tercero desde cualquier sub-dominio exigía consultar este servicio). El replanteamiento de junio 2026 resolvió la transversalidad con **distribución en lugar de dependencia**: los catálogos estables viajan empaquetados dentro de los Nuggets y se validan localmente en cada dominio. Las tasas de cambio no caben en ese modelo (cambian a diario — filtros 3 y 4 de la gobernanza) y permanecen como capacidad de servicio.

---

## Sección 2: Mapa de disposición v1 → v2

Inventario completo de lo definido en v1.x y su destino — nada se perdió:

| Elemento v1.x | Destino v2 |
|---|---|
| Catálogo de **países** (195) | Nugget [`Pais`](../nuggets/pais/especificacion.md) — fuente única de datos de país en el paquete |
| Catálogo de **monedas** (154, con decimales) | Nugget [`Moneda`](../nuggets/moneda/especificacion.md) |
| Catálogo de **tipos de documento** (46) | Nugget [`IdentificacionLegal`](../nuggets/identificacion-legal/especificacion.md) — extendido con reglas de formato y DV verificadas en fuentes oficiales |
| Catálogo de **divisiones territoriales** (CO 1.188 / DO 221 / PA 108) | Nugget [`DivisionTerritorial`](../nuggets/division-territorial/especificacion.md) — consumido por `DireccionFisica` y por la jurisdicción fiscal de Impuestos |
| **Tasas de cambio** (diarias, par + fecha) | **Permanece aquí** — Capacidad 2 (Sección 4) |
| Validaciones de formato ISO (V1, V5) | Reglas `[V01]`/`[V02]` de los Nuggets `Pais` y `Moneda` |
| Validaciones referenciales entre catálogos (V2, V3, V4, V6, V7) | **Verificación de construcción del paquete** (Sección 3): el custodio las ejecuta al producir cada versión — un dato roto no publica, en lugar de fallar en producción |
| V10 (registros en uso no se eliminan) + auditoría de cambios | Por diseño de los Nuggets: datos inmutables por versión, `activo: false` en versión nueva, históricos no se revalidan (regla de evolución 3 de la gobernanza), changelog por catálogo |
| API de consulta + recomendaciones de caché | Innecesarias para los catálogos estáticos — el dato ya vive en el proceso de cada dominio. Para tasas de cambio, ver Sección 4 |
| **Seed** (carga inicial desde JSON) | Los JSON pasan a ser insumo del paquete de Nuggets, no de seeds por servicio |
| **Sync** (actualización desde fuentes oficiales) | Tasas de cambio: permanece (Sección 4). Divisiones territoriales (DIVIPOLA anual): pasa a ser insumo de la producción de catálogos — produce versión menor del paquete |
| **Extend** (administrador agrega país/tipos nuevos) | Cambia de actor: agregar un país es **versión menor del paquete publicada por el custodio** (producto), no tarea del administrador del cliente — alineado con la gobernanza ("un Nugget no es configurable por inquilino"). El escape para capturas urgentes de países no perfilados es el **modo genérico** de los Nuggets (`validacionGenerica`) |
| Activación de catálogos según necesidad del cliente | **Habilitación por inquilino** — concepto distinto del `activo` global; traspasado al control plane (Sección 5) |
| Tipos de empresa | Ya estaba fuera del alcance (responsabilidad de Terceros) — se ratifica |
| Catálogos del servicio de Direcciones (tipos de vía, complementos, formatos, códigos postales) | Datos embebidos del Nugget [`DireccionFisica`](../nuggets/direccion-fisica/especificacion.md) — producidos por esta capacidad |

---

## Sección 3: Capacidad 1 — Producción de catálogos

La producción de catálogos **no es un servicio en ejecución**: es la responsabilidad de producir los datos que el paquete de Nuggets embebe. La ejerce el **custodio** definido en la gobernanza. Sus funciones:

| Función | Descripción |
|---------|-------------|
| **Producir** | Generar y mantener los archivos de datos de cada Nugget desde las fuentes oficiales (ISO, DANE/DIVIPOLA, DIAN, JCE/DGII, Tribunal Electoral/DGI, 4-72). |
| **Verificar** | Ejecutar las validaciones referenciales al construir cada versión del paquete: moneda principal de cada país existe en monedas; jerarquía territorial coherente (cada división con superior válido del mismo país); tipos de documento sin duplicados por país; perfiles de dirección referencian catálogos existentes. **Una versión con datos rotos no se publica.** |
| **Versionar** | Publicar las versiones del paquete según las reglas de la gobernanza (datos nuevos = menor; cambio de regla = mayor) y mantener el changelog por catálogo. |
| **Custodiar** | Ejercer el rol de custodio del catálogo de Nuggets: filtros de admisión, nomenclatura, matriz de consumidores (ver [gobernanza](../nuggets/gobernanza-nuggets.md), Sección 9). |

Los archivos fuente permanecen en este directorio (`catalogos/`) como material de trabajo del custodio; los datos publicados viven en `compartido/nuggets/*/datos/` y en el paquete distribuible.

---

## Sección 4: Capacidad 2 — Tasas de cambio

El único catálogo dinámico de la v1.x. Estructura del dato (heredada sin cambios de la especificación v1.0):

| Atributo | Descripción |
|----------|-------------|
| `monedaOrigen` / `monedaDestino` | Par de monedas (códigos del Nugget `Moneda`). |
| `valor` | Tasa de conversión. |
| `fechaVigencia` | Fecha desde la cual aplica. |
| `fuente` | Fuente oficial (Banco de la República CO, Banco Central RD, etc.). |

**Identidad:** `(monedaOrigen, monedaDestino, fechaVigencia)` — no pueden existir dos tasas para el mismo par y fecha (V9 de la v1.x).

### Distribución: por evento, no por consulta

Para no recrear el acoplamiento de disponibilidad que motivó el replanteamiento, los dominios **no consultan este servicio en caliente**:

1. El servicio sincroniza la tasa diaria desde la fuente oficial de cada país (o recibe carga manual de contingencia cuando la sincronización falla o la moneda no tiene fuente automatizada).
2. Publica el evento de integración **`TasaDeCambioPublicada`** (par, valor, fecha de vigencia, fuente).
3. Cada dominio consumidor (OXP para la TRM de radicación y de extracto; Impuestos para conversión de cuantía mínima) mantiene su **copia local** del histórico de tasas y resuelve sus consultas por fecha contra ella.

Si este servicio está caído, los dominios siguen operando con las tasas ya recibidas; lo único que se degrada es la llegada de la tasa del día — misma filosofía del replanteamiento: distribución, no dependencia. Los consumidores consultan **la tasa de una fecha específica**, nunca "la última" (OXP necesita la TRM de la fecha de radicación y la del extracto, que pueden diferir).

### Pendiente heredado

| # | Pendiente | Contexto |
|---|-----------|----------|
| PD1 | Mecanismo de sincronización automática | Definir cómo se obtiene la TRM diaria del Banco de la República y del Banco Central RD (heredado de la especificación v1.0). |

---

## Sección 5: Habilitación por inquilino

La v1.x tenía un único `activo` **global del producto** en cada catálogo. Existe una necesidad distinta, que se hace explícita aquí para no perderla: **cada inquilino (tenant) habilita los países y monedas que necesita para operar** — que sus listas de captura ofrezcan 3 monedas y no 154.

| Aspecto | Definición |
|---------|------------|
| **Qué es** | Un filtro de configuración por inquilino sobre los catálogos globales de los Nuggets (países de operación, monedas habilitadas). |
| **Qué NO es** | No es parte de los Nuggets (la gobernanza prohíbe la configuración por inquilino en ellos) ni un dato de estos catálogos (el `activo` del catálogo es del producto). |
| **Dónde vive** | **Configuración del tenant — control plane (`plataforma-saas/`, tenant management)**. Se diseña cuando ese plano se aborde. |
| **Cómo opera** | El BFF y las interfaces filtran la oferta de captura con la preferencia del tenant. **Los dominios validan contra el catálogo completo del Nugget**: un dato histórico con una moneda luego deshabilitada sigue siendo válido — la habilitación restringe la captura nueva, no la validez. |
| **Granularidad** | Por tenant en principio; si un tenant multi-empresa necesita preferencias por empresa (ej: monedas distintas por empresa), esa granularidad se decide al diseñar tenant management — se anota como pregunta abierta de ese diseño. |

---

## Sección 6: Qué está dentro y fuera del alcance

### Dentro del alcance (v2)

- Producción, verificación y versionado de los datos embebidos de los Nuggets (producción de catálogos, a cargo del custodio).
- Servicio de tasas de cambio: sincronización diaria, carga manual de contingencia, publicación por evento, histórico por par y fecha.

### Fuera del alcance

- Consulta en runtime de catálogos estáticos (eliminada — los Nuggets la reemplazan).
- Habilitación por inquilino de países/monedas (control plane — Sección 5).
- Tipos de empresa (Terceros), configuración fiscal por país (Impuestos), direcciones (Nugget `DireccionFisica`), gestión de terceros (Terceros).

### Dependencias

- La capacidad de tasas de cambio referencia los códigos del Nugget `Moneda`.
- Fuentes externas: Banco de la República (CO), Banco Central RD (DO), DIVIPOLA/DANE, fuentes ISO.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 2.0 | Junio 2026 | **Transformación por el replanteamiento de Nuggets.** El servicio de consulta desaparece: los 4 catálogos estáticos se distribuyen como Nuggets (`Pais`, `Moneda`, `IdentificacionLegal`, `DivisionTerritorial`, más los datos de `DireccionFisica`). Quedan dos capacidades: producción de catálogos (producción/verificación/versionado de datos embebidos a cargo del custodio, validaciones referenciales como verificación de construcción del paquete) y servicio de tasas de cambio (distribución por evento `TasaDeCambioPublicada` + copia local de los consumidores — sin consulta en caliente). Extend pasa de administrador del cliente a versión del paquete; la habilitación por inquilino de países/monedas se hace explícita y se traspasa al control plane (tenant management). Mapa de disposición completo en la Sección 2 — ningún elemento de la v1.x quedó sin destino. |
| 1.1 | Junio 2026 | Tres correcciones al catálogo `tipos-documento-identidad.json` con respaldo en fuentes oficiales, surgidas de la investigación del Nugget `IdentificacionLegal`: NIT `aplicaA = ambos` (ET art. 555-1; DIAN/OCDE); RNC `aplicaA = ambos` (DGII CA1009); PEP `activo = false` (vencido 28-feb-2023, Res. 971/2021). |
| 1.0 | Abril 2026 | Versión inicial. 8 secciones: definición y justificación, glosario (9 términos), 5 catálogos, matriz de consumidores (6 sub-dominios/servicios), datos preconfigurados (6 archivos JSON), administración (sistema + excepcional), dentro/fuera del alcance, beneficios. Anexo de estrategia de datos (Seed + Sync + Extend). |
