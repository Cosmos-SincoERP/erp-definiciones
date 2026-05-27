# Caso de onboarding — Plan Único de Cuentas (PUC)

> **Versión:** 1.0
> **Fecha:** Mayo 2026
> **Servicio padre:** `compartido/asistente-onboarding/`
> **Estado:** Caso modelado en v1.0 del Asistente de Onboarding

---

## 1. Propósito y alcance

Este documento describe el caso específico **onboarding del Plan Único de Cuentas (PUC)** dentro del servicio compartido Asistente de Onboarding. Aterriza las reglas heurísticas, las estructuras de referencia, los casos típicos por línea de negocio y el detalle del flujo iterativo UX para el caso PUC.

Es complemento de:

- `compartido/asistente-onboarding/definicion-alcance.md` — el qué del servicio.
- `compartido/asistente-onboarding/modelo-dominio.md` — el cómo del comportamiento (agregados, eventos, FSM).

**Audiencia:**

- Consultor especializado en contabilidad — usuario principal del flujo.
- Analista contable del cliente — aprueba el PUC final.
- Equipo de producto — mantiene reglas y referencias.
- Equipo de UX — diseña la experiencia de las seis fases.
- Equipo de desarrollo — implementa el motor y la UI.

---

## 2. El problema en el contexto del PUC

Los PUCs heredados del sistema anterior (típicamente SincoA&F u otros ERPs locales) presentan patrones recurrentes que la consultora identifica y corrige caso por caso:

- **Cuentas duplicadas por tercero:** una cuenta de cartera (CxP/CxC) replicada N veces, una por cada cliente o proveedor relevante.
- **Cuentas duplicadas por ciudad:** una cuenta de ingreso o gasto separada por ciudad, cuando la ciudad pertenece a la dimensión de unidad organizacional.
- **Cuentas por cada activo fijo:** una cuenta de activo fijo creada por cada bien específico (vehículo X, equipo Y), en vez de una cuenta agrupadora con la dimensión del activo en el detalle de partida.
- **Atributos fiscales mezclados en cuentas:** porcentaje de tarifa, base mínima y código de ciudad para ICA pegados como atributos de la cuenta del PUC, cuando esa información vive en el sub-dominio Impuestos.
- **Estructura inconsistente:** niveles maestra/auxiliar mal aplicados (cuentas que se asientan estando como maestras, cuentas auxiliares sin maestra padre), longitudes de código atípicas.
- **Sin uniformidad entre clientes:** dos empresas del mismo sector terminan con PUCs estructuralmente diferentes porque los consultores aplicaron criterios distintos.

El **costo operativo** es alto: cada onboarding nuevo requiere a un consultor senior haciendo el mismo análisis manual, decisión por decisión, sin que las decisiones acumuladas mejoren las siguientes implementaciones.

---

## 3. Filosofía del modelo nuevo

El modelo nuevo aplica un principio simple: **cuenta limpia + información en dimensiones**. Lo que en el sistema anterior se replicaba como múltiples cuentas (por tercero, por ciudad, por activo), en el modelo nuevo es una sola cuenta con la dimensión en el detalle de la partida contable.

Coherencia con decisiones ya adoptadas en el sub-dominio Contabilidad:

- **[D11] del modelo de Contabilidad** — arquitectura "PUC único + libros paralelos". Una empresa típica opera con un PUC NIIF y libros (Principal y Fiscal) sobre el mismo PUC. Las diferencias entre tratamientos contables se modelan como asientos específicos del libro, no como PUCs paralelos.
- **R34 del alcance de Contabilidad** — asientos específicos de un libro para casos como ajustes NIIF.
- **`anexo-marco-contable-y-arquitectura-puc.md`** del sub-dominio Contabilidad — la separación formal entre PlanDeCuentas, LibroContable y MarcoContable.

**Sobre los atributos fiscales:** los porcentajes de tarifa, bases mínimas y códigos de jurisdicción que aparecen en las cuentas del PUC legacy **no se reubican automáticamente al sub-dominio Impuestos**. Esa información ya está en Impuestos por carga independiente (los tributos y sus tarifas se gestionan ahí). El asistente identifica esos atributos en el PUC legacy y los **descarta** del PUC final.

---

## 4. Criterios de revisión (los 12)

Cada criterio se aplica como una `ReglaDeRevisionPUC` del catálogo del producto. Las 12 reglas iniciales se precargan con el producto y pueden crecer en el tiempo a partir de aprendizajes promovidos por el equipo de producto.

### 4.1 Tabla detallada de criterios

| # | Criterio | Categoría | Problema en legacy | Comportamiento esperado en modelo nuevo | Cómo el asistente lo detecta y la sugerencia que ofrece |
|---|----------|-----------|----------------------|-------------------------------------------|----------------------------------------------------------|
| 1 | **Cuentas separadas por tercero** | Consolidar | Una cuenta de cartera replicada por cada tercero relevante (proveedor, cliente). Códigos del tipo `1305-01-001`, `1305-01-002`, etc., donde el sufijo identifica al tercero. | Una sola cuenta auxiliar; el tercero vive como dimensión obligatoria de la partida contable. | Patrón: cuentas auxiliares hijas del mismo padre con secuencia incremental en el sufijo, todas del mismo tipo CxP o CxC, sin diferenciación contable real. Sugerencia: consolidar en la cuenta auxiliar única; informar al consultor que el detalle por tercero se preserva en la dimensión `tercero` de cada partida. |
| 2 | **Distintos PUCs por línea de negocio** | Conservar (con marcos) | Empresas con líneas mixtas (construcción + inmobiliaria + concesiones) que mantienen PUCs paralelos por línea, dificultando la consolidación. | Un solo PUC por marco contable. Las líneas de negocio se diferencian por unidad organizacional, no por PUC paralelo. Cuando hay un sector regulado con PUC sectorial obligatorio (SFC, Supersalud), sí aplica un PUC adicional con su MarcoContable propio (uso excepcional). | Patrón: varios PUCs legacy cargados o detección en el archivo de un sub-conjunto de cuentas con estructura claramente sectorial (códigos típicos de un PUC sectorial regulado). Sugerencia: si la empresa NO es sectorial regulada, consolidar en un solo PUC NIIF. Si SÍ es sectorial regulada, mantener el PUC sectorial bajo un MarcoContable adicional (escenario excepcional documentado en el modelo de Contabilidad). |
| 3 | **Variabilidad en grupos 6-7 (Costo) y 14 (Inventario)** | Foco | Los grupos 6 (Costos por proceso de producción), 7 (Costo de producción o de operación) y 14 (Inventario para la venta o transformación) son los que más varían entre empresas. Tienen cuentas muy específicas por proyecto, producto o sub-proceso. | El consultor debe prestar atención especial a estos grupos. El asistente no propone consolidación automática agresiva — propone revisión cuidadosa y resalta diferencias contra el PUC de referencia. | Patrón: grupos contables que tienen alta dispersión de auxiliares respecto al PUC de referencia y respecto a la mediana de otros grupos. Sugerencia: marcar el grupo como "Foco — requiere análisis cuidadoso". Mostrar al consultor las cuentas que difieren del PUC de referencia con detalle y permitirle decidir caso por caso. |
| 4 | **PUC genérico para empresas administrativas** | Caso fácil | Empresas con operación administrativa (servicios profesionales, comercio general sin manufactura) tienen PUCs relativamente simples y similares entre clientes. | El PUC de referencia administrativo aplica casi tal cual; pocas cuentas adicionales o consolidaciones. | Patrón: contexto seleccionado como "Administrativa" + bajo nivel de discrepancia entre PUC legacy y PUC de referencia. Sugerencia: usar el PUC de referencia con ajustes mínimos. El proceso se completa rápido. |
| 5 | **Cuentas contables por ciudad** | Consolidar | Una cuenta de ingreso o gasto replicada por ciudad (ej. una cuenta de "Servicios públicos Bogotá", otra "Servicios públicos Medellín"). | Una sola cuenta auxiliar; la ciudad vive como atributo de la unidad organizacional referenciada en la partida. | Patrón: cuentas auxiliares hijas del mismo padre con nombres que mencionan ciudades colombianas (o cualquier dimensión geográfica). Sugerencia: consolidar en la cuenta auxiliar única; recomendar al consultor que la unidad organizacional asociada a cada partida lleve el detalle de la ciudad. |
| 6 | **Estructura limpia para movimientos** | Filosofía | Sistemas legacy como SincoA&F sí tienen estructura útil para registrar los movimientos (partidas con dimensiones), pero contaminan el PUC con esas dimensiones replicadas como cuentas. | El PUC del modelo nuevo es limpio: solo refleja la naturaleza contable de las cuentas. La dimensionalización vive en cada partida (tercero, unidad organizacional, libro). | Este criterio es la filosofía general, no una regla concreta de detección. Se materializa a través de las reglas #1, #5, #9 y #10. |
| 7 | **Cuentas de banco por entidad y cuenta bancaria** | Conservar | Una cuenta de banco separada para cada entidad financiera y para cada cuenta bancaria específica (ej. `111005-001 BANCOLOMBIA CORRIENTE 123`, `111005-002 BANCOLOMBIA AHORROS 456`). | **Se mantiene tal cual.** Por exigencias de revelación NIIF y por trazabilidad operativa, cada cuenta bancaria debe ser una cuenta auxiliar distinta. | Patrón: cuentas del grupo 11 (Disponible — Bancos) con detalle por entidad y número de cuenta. Sugerencia: conservar todas. Marcar la regla como excepción legítima al principio de consolidación. |
| 8 | **Cuentas con base y porcentaje para impuestos** | Reubicar | Cuentas del PUC legacy con atributos como `tarifaIva: 19%`, `baseMinima: 1.500.000` o `ciudadIca: BOG`. Estos atributos contaminan el PUC. | El PUC final no lleva atributos fiscales. La información de tarifas, bases mínimas y jurisdicciones para impuestos vive en el sub-dominio Impuestos (Tributo, TarifaTributaria, etc.) por carga independiente. | Patrón: columnas en el archivo del PUC legacy con encabezados como "tarifa", "base", "porcentaje", "ciudad ICA" asociadas a las cuentas. Sugerencia: descartar esos atributos del PUC final. Confirmar al consultor que las tarifas y bases ya deben estar (o se deben cargar) en el sub-dominio Impuestos por flujo independiente. |
| 9 | **Cuentas por cada activo fijo** | Consolidar | Una cuenta de activo fijo creada por cada bien identificable (ej. `1524-01-001 Vehículo MAZDA placas ABC`, `1524-01-002 Vehículo CHEVROLET placas XYZ`). | Una sola cuenta auxiliar por categoría de activo fijo; el activo individual vive como dimensión de la partida (referencia al sub-dominio Activos Fijos) o como sub-cuenta en módulos especializados. | Patrón: cuentas auxiliares del grupo 15 (Propiedad, planta y equipo) con detalle individual de activos identificados por placas, números de serie o descripciones específicas. Sugerencia: consolidar en la cuenta auxiliar de la categoría; informar al consultor que el detalle del activo se preserva en el sub-dominio de Activos Fijos cuando esté disponible. |
| 10 | **Cuentas de impuestos con bases y porcentajes** | Reubicar | Cuentas específicas para impuestos (IVA descontable, ReteFuente practicada, etc.) que en el legacy llevan los porcentajes y bases. | La cuenta de impuesto se mantiene en el PUC (es contablemente legítima), pero **sin** los atributos de tarifa o base. La información fiscal vive en Impuestos. | Patrón: cuentas auxiliares del grupo 13 (Cuentas por Cobrar — Impuestos) o 24 (Impuestos, gravámenes y tasas) con columnas adicionales de porcentaje, base mínima, dependencia padre, etc. Sugerencia: conservar la cuenta; descartar las columnas fiscales. Validar contra el catálogo de tributos del sub-dominio Impuestos. |
| 11 | **Revisión de niveles maestra/auxiliar** | Validar | Inconsistencias estructurales: cuentas auxiliares cuyo padre no es maestra; cuentas marcadas como maestras pero que tienen movimientos; saltos de jerarquía en los códigos. | Estructura jerárquica coherente: cada cuenta auxiliar tiene una cuenta maestra padre; cuentas maestras nunca se asientan directamente; los códigos siguen un patrón consistente de longitud. | Patrón: validación estructural del PUC legacy contra reglas de integridad. Sugerencia: marcar las inconsistencias y proponer la corrección (cambiar tipo, crear la maestra faltante, renombrar). Categoría Validar — el consultor debe aprobar cada corrección. |
| 12 | **Longitud y estructura del código contable** | Validar | Códigos del PUC legacy con longitudes inconsistentes (algunas cuentas de 4 dígitos, otras de 12 sin patrón) o con caracteres no estándar. | Códigos del PUC final con longitud consistente y estructura jerárquica clara (grupo - cuenta - sub-cuenta - auxiliar). | Patrón: análisis estadístico de longitudes y formatos de código del PUC legacy. Detecta outliers y patrones rotos. Sugerencia: marcar cuentas con códigos atípicos; sugerir normalización si el patrón general lo permite. Categoría Validar — decisión del consultor sobre si normalizar o mantener. |

### 4.2 Resumen por categoría

| Categoría | Criterios |
|-----------|-----------|
| Consolidar | #1 (por tercero), #5 (por ciudad), #9 (por activo fijo) |
| Conservar | #2 (multi-PUC sectorial), #7 (bancos por entidad/cuenta) |
| Reubicar | #8 (atributos fiscales en cuentas), #10 (cuentas de impuestos con porcentajes) |
| Foco | #3 (grupos 6-7-14) |
| Validar | #11 (niveles maestra/auxiliar), #12 (longitud y estructura del código) |
| Filosofía | #6 (estructura limpia — se materializa a través de los demás) |
| Caso fácil | #4 (administrativas — el PUC de referencia aplica casi tal cual) |

---

## 5. Las cinco categorías de tratamiento

### Consolidar

Cuentas duplicadas por una dimensión que en el modelo nuevo vive como atributo de la partida (tercero, unidad organizacional, activo). El asistente propone unificar las cuentas duplicadas en una sola cuenta auxiliar.

**Consecuencia de aceptar:** el PUC queda con menos cuentas. La información detallada por la dimensión se sigue obteniendo en los reportes filtrando por esa dimensión.

**Consecuencia de rechazar:** el PUC mantiene la duplicación. Los reportes seguirán mostrando cuentas múltiples para el mismo concepto contable. Es válido en casos específicos (decisión del consultor).

### Conservar

La duplicación o segmentación tiene fundamento legítimo (regulatorio, operativo, de revelación NIIF). El asistente reconoce el patrón y propone mantener la estructura.

**Consecuencia de aceptar:** la estructura se conserva como está.

**Consecuencia de rechazar (consolidar contra recomendación):** el consultor debe justificar por qué consolida algo que el modelo recomienda conservar. Generalmente no se rechaza.

### Reubicar

Atributos o conceptos que pertenecen a otro sub-dominio. En el caso PUC, se trata de atributos fiscales (porcentajes, bases) que viven en Impuestos.

**Consecuencia de aceptar:** los atributos se descartan del PUC. El consultor confirma que la información correspondiente ya está (o se cargará) en el sub-dominio correspondiente.

**Consecuencia de rechazar:** los atributos permanecerían en el PUC, pero el modelo nuevo no los soporta. Una decisión de rechazo aquí requiere reportarse al equipo de producto como caso no cubierto.

### Foco

Áreas del PUC donde la variabilidad real entre empresas justifica análisis cuidadoso del consultor. El asistente no propone consolidación agresiva — proporciona contexto y permite decidir caso por caso.

**Consecuencia de aceptar (las sugerencias dentro del foco):** decisiones tomadas con criterio del consultor.

**Consecuencia de rechazar:** decisiones del consultor que se respetan. El sistema aprende qué patrones son legítimos en ese grupo.

### Validar

Inconsistencias estructurales del PUC legacy: niveles mal asignados, códigos atípicos, jerarquía rota. El asistente propone corrección; el consultor decide si normalizar o conservar.

**Consecuencia de aceptar:** la estructura del PUC final queda consistente.

**Consecuencia de rechazar:** se mantiene la inconsistencia. En algunos casos puede impedir operación posterior (cuentas que se asientan estando marcadas como maestras producirían errores). El asistente advierte estos casos como severidad Crítica.

---

## 6. Casos típicos por línea de negocio

Cada caso tiene su `PUCdeReferencia` precargado en el producto, con cuentas específicas del sector y `obligatoriedadTercero` / `obligatoriedadUnidadOrganizacional` configuradas según la práctica común.

### 6.1 Construcción

**Particularidades del PUC esperado:**

- Grupos 6 (Costos) y 7 (Costo de operación) detallados por etapa de obra (excavación, estructura, mampostería, acabados).
- Grupo 14 (Inventarios) con sub-cuentas por bodega de obra y por tipo de material.
- Cuentas 1440 (Anticipos a proveedores) y 2705 (Anticipos recibidos de clientes) con detalle por proyecto a través de la dimensión `unidadOrganizacional` = proyecto.
- Cuentas de fideicomiso inmobiliario (1715, 2790) si la operación las usa.

**Reglas que más se aplican:**

- #3 Foco — el grupo 6-7 requiere revisión cuidadosa.
- #5 Consolidar — frecuente consolidación de cuentas por proyecto en una sola con dimensión.
- #9 Consolidar — cuentas por activo fijo (maquinaria de obra) en cuenta agrupadora.

**PUCdeReferencia asociado:** `CONSTRUCCION_CO` (jurisdicción CO, marco NIIF).

### 6.2 Inmobiliaria (arrendamiento + venta)

**Particularidades del PUC esperado:**

- Cuentas de ingresos por arrendamiento (4220) separadas de ingresos por venta de inmuebles (415x).
- Cuentas 2705 (Anticipos recibidos de clientes) con detalle por unidad inmobiliaria a través de la dimensión.
- Cuentas de fideicomiso para esquemas de preventa.
- Cuentas de inversión en propiedades (1504 — Propiedades de inversión bajo NIIF 40).
- Cuentas de pasivos por arrendamiento (NIIF 16, agregadas en el sub-dominio Arrendamientos).

**Reglas que más se aplican:**

- #5 Consolidar — cuentas por proyecto inmobiliario o por unidad vendida.
- #2 Conservar — frecuente que una empresa mixta tenga PUC separado para venta vs arrendamiento; se evalúa caso por caso.

**PUCdeReferencia asociado:** `INMOBILIARIA_CO` (jurisdicción CO, marco NIIF).

### 6.3 Concesiones viales

**Particularidades del PUC esperado:**

- Activo intangible — derecho de concesión (1605 — Crédito mercantil; 1610 — Marcas) y cuentas asociadas a NIIF 12.
- Ingresos por peaje (415x) con detalle por estación.
- Cuentas de mantenimiento mayor (5165) y provisiones asociadas.

**Reglas que más se aplican:**

- #3 Foco — el grupo 16 (Intangibles) requiere análisis cuidadoso por las reglas NIIF 12.
- #5 Consolidar — cuentas por estación de peaje en una sola con dimensión.

**PUCdeReferencia asociado:** `CONCESIONES_VIALES_CO` (jurisdicción CO, marco NIIF).

### 6.4 Administrativa (servicios, comercio general)

**Particularidades del PUC esperado:**

- PUC relativamente estándar y simple.
- Cuentas de servicios (5135), gastos administrativos (515x), gastos de ventas (52xx) sin gran detalle por categoría.
- Pocas particularidades sectoriales.

**Reglas que más se aplican:**

- #4 Caso fácil — el PUC de referencia aplica casi tal cual.
- #1, #5, #9 — consolidaciones estándar de cuentas duplicadas.

**PUCdeReferencia asociado:** `ADMINISTRATIVA_GENERICA_CO` (jurisdicción CO, marco NIIF).

### 6.5 Otros casos sectoriales

Existen sectores con PUCs sectoriales obligatorios (financiero — SFC, salud — Supersalud, solidario — Supersolidaria). Para estos casos:

- El asistente reconoce el patrón de un PUC sectorial regulado y propone mantenerlo bajo un `MarcoContable` adicional al NIIF (escenario excepcional documentado en el modelo de Contabilidad).
- La regla #2 (Conservar — distintos PUCs por línea de negocio) se aplica.
- El equipo de producto puede agregar `PUCdeReferencia` específicos para estos sectores cuando haya demanda.

---

## 7. Diseño funcional del flujo iterativo

El flujo del asistente para el caso PUC consta de seis fases secuenciales con pausabilidad y reanudación en la fase 4.

### 7.1 Fase 1 — Carga del PUC del sistema anterior

```
┌────────────────────────────────────────────────────────────────────┐
│  Onboarding del PUC                              Paso 1 de 6        │
│  Cargar el PUC del sistema anterior                                 │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  Empresa: COSMOS-SAS                                                │
│                                                                    │
│  Origen del PUC:                                                    │
│   ◉ Archivo Excel (.xlsx)                                          │
│   ◯ Archivo CSV (.csv)                                             │
│                                                                    │
│  [ Seleccionar archivo ]   PUC-COSMOS-v2.xlsx (847 KB)             │
│                                                                    │
│  Validación de formato:                                             │
│   ✓ Estructura de columnas reconocida                              │
│   ✓ 1.847 cuentas detectadas                                       │
│   ⚠ 47 cuentas tienen columnas adicionales (porcentaje, base)      │
│     — se analizarán en el siguiente paso.                           │
│                                                                    │
│  ¿Continuar con este archivo?                                       │
│                                                                    │
│  [Cancelar]                                            [Continuar]  │
└────────────────────────────────────────────────────────────────────┘
```

**Output:** evento `PUCLegacyImportado` registrado. El asistente queda con el contenido del archivo en memoria, listo para selección de contexto.

### 7.2 Fase 2 — Contexto de la empresa

```
┌────────────────────────────────────────────────────────────────────┐
│  Onboarding del PUC                              Paso 2 de 6        │
│  Contexto de la empresa                                             │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  El asistente compara el PUC cargado contra una estructura de      │
│  referencia adecuada al sector y modelo de negocio de la empresa.   │
│                                                                    │
│  Jurisdicción:                                                      │
│   ◉ Colombia (CO)    ◯ República Dominicana (DO)                  │
│   ◯ Panamá (PA)                                                    │
│                                                                    │
│  Sector económico:                                                  │
│   ◉ Construcción     ◯ Servicios                                   │
│   ◯ Inmobiliaria     ◯ Comercio                                    │
│   ◯ Concesiones      ◯ Manufactura                                 │
│   ◯ Otro                                                            │
│                                                                    │
│  Modelo de negocio:                                                 │
│   ◉ Construcción de obra civil                                     │
│   ◯ Construcción de vivienda                                        │
│   ◯ Mantenimiento de infraestructura                                │
│                                                                    │
│  ☐ Sector regulado (Superintendencia Financiera, Supersalud,        │
│     Supersolidaria) — el PUC sectorial obligatorio se cargará      │
│     adicional al NIIF.                                              │
│                                                                    │
│  Marco contable destino: NIIF (predeterminado del producto)         │
│                                                                    │
│  Estructura de referencia sugerida: CONSTRUCCION_CO                 │
│  [ Ver otras estructuras disponibles ]                              │
│                                                                    │
│  [Atrás]                                              [Continuar]   │
└────────────────────────────────────────────────────────────────────┘
```

**Output:** evento `PUCdeReferenciaSeleccionado`. Proceso transita a `EN_ANALISIS`.

### 7.3 Fase 3 — Resumen pre-análisis

```
┌────────────────────────────────────────────────────────────────────┐
│  Onboarding del PUC                              Paso 3 de 6        │
│  Resumen del análisis automático                                    │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  PUC cargado:           1.847 cuentas                              │
│  Estructura referencia: CONSTRUCCION_CO (Construcción - Colombia)   │
│  Marco contable:        NIIF                                        │
│                                                                    │
│  Sugerencias generadas: 359 en total                                │
│                                                                    │
│   • Consolidar: 312 cuentas (cuentas duplicadas por tercero,        │
│     ciudad o activo fijo)                                           │
│   • Reubicar:    47 cuentas con atributos fiscales que se           │
│     descartarán del PUC                                             │
│   • Validar:      8 cuentas con inconsistencias estructurales       │
│     (niveles, códigos)                                              │
│   • Foco:       220 cuentas de los grupos 6-7-14 marcadas para      │
│     revisión cuidadosa                                              │
│   • Conservar: 1.480 cuentas que se mantienen tal cual              │
│                                                                    │
│  Resultado estimado del PUC final: 1.488 cuentas (-359, 19% más    │
│  limpio que el original)                                            │
│                                                                    │
│  Tiempo estimado de revisión: 3-4 horas                             │
│                                                                    │
│  [Atrás]              [Iniciar revisión iterativa por grupo →]      │
└────────────────────────────────────────────────────────────────────┘
```

**Output:** evento `AnalisisAutomaticoEjecutado` con el resultado y las sugerencias listas. Proceso transita a `EN_REVISION`.

### 7.4 Fase 4 — Revisión iterativa por grupo

Esta es la fase más extensa. El consultor revisa cada sugerencia individualmente. El sistema agrupa por grupo contable y permite pausa y reanudación.

**Vista de la sugerencia (ejemplo):**

```
┌──────────────────────────────────────────────────────────────────────┐
│  Onboarding del PUC                                Paso 4 de 6        │
│  Grupo 14 — Inventarios                          Sugerencia 1 de 21   │
│  Grupo 3 de 13 grupos totales              Progreso total: 28/359     │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Sugerencia: Consolidar                            Severidad: Reco.   │
│  Regla: CONSOLIDAR_CUENTAS_POR_PROYECTO_INVENTARIO                    │
│                                                                      │
│  Detectadas 47 cuentas con patrón similar:                            │
│   • 1430-01-001 hasta 1430-01-047 (Inventario obra Proyecto X-N)     │
│                                                                      │
│  ⚠ Análisis del asistente:                                           │
│    Las 47 cuentas representan inventarios diferenciados por          │
│    proyecto, no por naturaleza contable. La dimensión "proyecto"     │
│    se modela como unidadOrganizacional en cada partida.              │
│                                                                      │
│  ✓ Sugerencia: consolidar las 47 cuentas en una sola cuenta          │
│    auxiliar 1430-01-001 (Inventario de obra). La unidad              │
│    organizacional asociada a cada partida llevará el proyecto         │
│    específico.                                                        │
│                                                                      │
│  Consecuencias de aceptar:                                            │
│   • Reportes de inventario por proyecto: siguen funcionando (los     │
│     filtran por unidadOrganizacional).                                │
│   • Auxiliar contable: 47 filas pasan a 1 fila por proyecto cuando   │
│     se consulta filtrando por unidad.                                 │
│   • Saldo total: idéntico, sin pérdida de información.                │
│                                                                      │
│  Consecuencias de rechazar:                                           │
│   • Se mantienen las 47 cuentas duplicadas.                          │
│   • El PUC tendrá complejidad operativa innecesaria.                  │
│   • Crear un nuevo proyecto requerirá crear una nueva cuenta.         │
│                                                                      │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐      │
│  │ ✓ Aceptar    │ ✎ Modificar  │ ✗ Rechazar   │ ⏸ Aplazar    │      │
│  └──────────────┴──────────────┴──────────────┴──────────────┘      │
│                                                                      │
│  Progreso del grupo: ████░░░░░░░░░░░░░░ 1 de 21                     │
│  Progreso total:     █████░░░░░░░░░░░░░ 28 de 359                   │
│                                                                      │
│  [⏸ Pausar y guardar para continuar más tarde]                       │
└──────────────────────────────────────────────────────────────────────┘
```

**Acciones disponibles por sugerencia:**

- **Aceptar:** aplica la sugerencia tal cual. Evento `SugerenciaAceptada`.
- **Modificar:** abre un diálogo para ajustar el detalle (por ejemplo: aceptar la consolidación pero usar otro código de cuenta destino). Justificación obligatoria. Evento `SugerenciaModificada` con el delta del ajuste.
- **Rechazar:** descarta la sugerencia. Justificación obligatoria. Evento `SugerenciaRechazada`.
- **Aplazar:** pospone la decisión. La sugerencia vuelve al final del proceso. Evento `SugerenciaAplazada`.

**Navegación entre grupos:** el sistema avanza automáticamente de un grupo a otro cuando todas las sugerencias del grupo están decididas (excepto las aplazadas). El consultor puede saltar entre grupos manualmente desde un menú lateral.

**Pausabilidad:** el consultor puede salir en cualquier momento. Al regresar, el sistema reconstruye el estado exacto desde el stream del `ProcesoOnboardingPUC`: qué sugerencia estaba viendo, qué grupos completó, qué sugerencias quedan aplazadas.

**Output al cerrar:** evento `RevisionDeGrupoCompletada` cada vez que se termina un grupo. Cuando todas las sugerencias están resueltas (ninguna aplazada), evento `ProcesoListoParaGenerar` y transición a `LISTO_PARA_GENERAR`.

### 7.5 Fase 5 — Confirmación del resumen

```
┌────────────────────────────────────────────────────────────────────┐
│  Onboarding del PUC                              Paso 5 de 6        │
│  Confirmar y generar PUC final                                      │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  Resumen de decisiones:                                             │
│                                                                    │
│   ✓ Aceptadas:     287 sugerencias                                  │
│   ✎ Modificadas:    34 sugerencias                                  │
│   ✗ Rechazadas:     31 sugerencias                                  │
│   ⏸ Aplazadas:       0 sugerencias  ✓ todas resueltas               │
│                                                                    │
│  PUC resultante: 1.512 cuentas                                      │
│   • 359 cuentas consolidadas                                        │
│   • 47 atributos fiscales descartados                              │
│   • Estructura validada — niveles y códigos consistentes            │
│                                                                    │
│  Reporte de migración:                                              │
│   [Vista previa]    [Descargar Excel]    [Descargar PDF]            │
│                                                                    │
│  Aprobación del analista contable:                                  │
│   ☐ Yo, ____________, apruebo el PUC final para la empresa         │
│      COSMOS-SAS                                                     │
│                                                                    │
│  Una vez generado, el proceso queda cerrado y no se puede modificar.│
│  Si necesitas cambios después, deberás iniciar un proceso nuevo.    │
│                                                                    │
│  [Volver a revisar decisiones]              [✓ Confirmar y generar]│
└────────────────────────────────────────────────────────────────────┘
```

**Output:** el consultor puede regresar a la fase 4 para modificar decisiones específicas, o confirmar la generación.

### 7.6 Fase 6 — Generación

Al confirmar, el `ServicioDeGeneracionPUC` coordina la creación en el sub-dominio Contabilidad:

1. Si el marco contable es custom, se crea `MarcoContable`.
2. Se crea `PlanDeCuentas` con la referencia al marco.
3. Se agregan todas las `CuentaContable` resultantes.
4. Se genera el reporte de migración descargable.
5. Se emite `PUCFinalGenerado`. Proceso transita a `GENERADO` (terminal).

El consultor ve la confirmación final con el resumen y el enlace al PUC creado en el sub-dominio Contabilidad.

---

## 8. Salida del proceso — relación consistente persistida

El usuario lo planteó como necesidad clave: la salida del proceso no es solo un reporte plano descargable, sino una **relación consistente y consultable indefinidamente** de todo lo que ocurrió.

### 8.1 Qué queda persistido tras cada proceso

Cada `ProcesoOnboardingPUC` es un agregado Event Sourcing con stream propio (`proceso-onboarding-puc-{id}`). En el stream quedan **todos** los eventos en orden:

- El inicio (`ProcesoOnboardingPUCIniciado`).
- La carga del archivo (`PUCLegacyImportado` con la huella del contenido).
- La selección de la referencia (`PUCdeReferenciaSeleccionado`).
- El resultado del análisis automático (`AnalisisAutomaticoEjecutado` con las 359 sugerencias generadas).
- Cada decisión del consultor (`SugerenciaAceptada`, `SugerenciaModificada`, `SugerenciaRechazada`, `SugerenciaAplazada`) con su marca de tiempo, usuario responsable y justificación.
- Cada cierre de grupo (`RevisionDeGrupoCompletada`).
- El cierre del proceso (`ProcesoListoParaGenerar` y luego `PUCFinalGenerado` o `ProcesoAbandonado`).

### 8.2 Capacidad de re-consultar procesos anteriores

Cualquier consultor, analista contable o auditor con permiso puede:

- **Listar todos los procesos** de una empresa (intentos 1, 2, 3, ...) con su estado terminal (`GENERADO`, `ABANDONADO`).
- **Abrir un proceso específico** y reconstruir su flujo completo: qué archivo se cargó, qué sugerencias se generaron, qué decisiones tomó el consultor, qué quedó como definitivo y por qué.
- **Comparar dos procesos** de la misma empresa para entender qué cambió entre el intento 1 abandonado y el intento 2 generado.

### 8.3 Trazabilidad cuenta por cuenta

Para cada cuenta del PUC final generado, es posible rastrear hacia atrás:

- De qué cuenta(s) del PUC legacy proviene.
- Qué regla de revisión se aplicó (si aplicó alguna).
- Qué decisión tomó el consultor y con qué justificación.
- En qué fecha y por qué consultor.

Esta trazabilidad es valiosa para auditorías regulatorias (revisoría fiscal, auditoría externa) y para auditorías internas del cliente.

### 8.4 Reporte de migración

Documento descargable (formato Excel y/o PDF — pendiente PD3) que sintetiza el proceso completo:

- Información de cabecera: empresa, fecha del proceso, consultor responsable, analista contable aprobador, PUC de referencia usado.
- Resumen estadístico: total de cuentas legacy, total de cuentas finales, distribución por categoría.
- Detalle de decisiones: por cada cuenta o grupo de cuentas, qué se hizo, qué regla aplicó, qué justificación se dio.
- Cuentas conservadas: las 1.480 que no requirieron transformación.

El reporte queda asociado al proceso y se puede regenerar en cualquier momento desde el historial.

### 8.5 Aprendizaje acumulado

Cada decisión aceptada o modificada alimenta el `AprendizajeOnboardingPUC` de la empresa. En procesos futuros de la misma empresa:

- El motor de análisis aplica primero las reglas formales, luego el aprendizaje, luego la comparación con referencia.
- Si el consultor ya decidió antes que las cuentas de tipo `1430-01-XXX` se consolidan en `1430-01-001`, el siguiente proceso lo propondrá automáticamente.

Esto reduce significativamente el tiempo de revisión en procesos sucesivos de la misma empresa.

---

## 9. Cómo lo hacen otros ERPs

Investigación comparativa de las herramientas de migración y onboarding del COA en los principales ERPs del mercado:

| ERP | Herramienta | Alcance | Limitación frente al asistente del proyecto |
|-----|-------------|---------|----------------------------------------------|
| **SAP S/4HANA** | SAP Migration Cockpit + LTMC | Carga masiva de datos maestros desde plantillas Excel. Validaciones de integridad. Soporte para migración desde SAP ECC. | No incluye heurísticas contables de simplificación. No aprende de decisiones previas. No propone consolidaciones — solo valida formato y unicidad. |
| **Oracle Fusion ERP Cloud** | File-Based Data Import (FBDI) + Configuration Manager + Tax Configuration Wizard | Plantillas con validaciones automáticas. Wizard guiado para configurar Chart of Accounts. | Mismo patrón que SAP: cargar y validar, no proponer transformaciones inteligentes. El consultor debe decidir la estructura antes de cargar. |
| **Microsoft Dynamics 365 Finance** | Data Management Framework + Configuration Migration Tool | Importación de datos maestros con plantillas. Validaciones por entidad. | Sin asistente de simplificación. Sin aprendizaje. La curaduría del COA es manual y previa al cargue. |
| **NetSuite** | SuiteCloud Development Framework (SDF) + CSV Import Assistant | Importación CSV con mapping de campos. Asistentes de configuración inicial. | Sin reglas heurísticas para detectar duplicaciones por dimensión. Sin aprendizaje progresivo. |
| **Workday Financials** | Enterprise Interface Builder (EIB) + Workday Adaptive Planning | Cargas masivas y planeación financiera. Sugerencias de modelado en la planeación. | Las sugerencias de Adaptive aplican a planeación, no a migración del COA. Sin asistente nativo de simplificación contable. |

### 9.1 Diferenciación del Asistente del proyecto

Ningún ERP grande ofrece simultáneamente:

1. **Asistente nativo de simplificación contable** con reglas heurísticas formalizadas sobre las prácticas comunes del PUC heredado (duplicaciones por tercero/ciudad/activo, atributos fiscales mal ubicados, niveles inconsistentes).
2. **Aprendizaje progresivo** por empresa que mejora con cada proceso.
3. **Multi-intento con historial Event Sourcing** completamente auditable.
4. **Revisión iterativa con consecuencias visibles** antes de decidir.
5. **Promoción de aprendizajes a reglas formales** del catálogo del producto, gestionada por el equipo de producto.

Los ERPs grandes ofrecen carga masiva con validaciones — esa es una capacidad necesaria pero diferente. La consultoría especializada en migración (Big 4, integradores) ofrece servicios similares al asistente, pero como consultoría humana, no como capacidad nativa del producto.

**Oportunidad:** el asistente del proyecto incorpora dentro del producto el conocimiento que hoy se contrata como consultoría externa.

---

## 10. Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Mayo 2026 | Versión inicial del caso PUC del Asistente de Onboarding. Documenta los 12 criterios de revisión confirmados, las 5 categorías de tratamiento (Consolidar, Conservar, Reubicar, Foco, Validar), los 4 casos típicos por línea de negocio (Construcción, Inmobiliaria, Concesiones viales, Administrativa), las 6 fases del flujo iterativo UX con diagramas ASCII, y la sección de salida persistida (relación consistente con historial Event Sourcing, trazabilidad cuenta por cuenta, reporte de migración, aprendizaje acumulado). Investigación comparativa con SAP, Oracle, Dynamics, NetSuite y Workday. Acompañado por `compartido/asistente-onboarding/definicion-alcance.md` v1.0 y `compartido/asistente-onboarding/modelo-dominio.md` v1.0. |
