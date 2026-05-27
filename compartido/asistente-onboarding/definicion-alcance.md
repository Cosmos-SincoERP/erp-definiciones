# Asistente de Onboarding — Definición de alcance

> **Versión:** 1.0
> **Fecha:** Mayo 2026
> **Estado:** Listo para desarrollo (F1 — caso PUC)

---

## Tabla de contenido

1. Definición, contexto y problema
2. Glosario de términos
3. Actores del sistema
4. Flujo principal del onboarding
5. Integraciones con otros sub-dominios
6. Reglas de negocio
7. Qué está dentro y fuera del alcance
8. Estrategia de implementación por fases
9. Beneficios esperados

---

## Sección 1: Definición, contexto y problema

### Definición

El **Asistente de Onboarding** es un servicio compartido del ERP que guía al consultor especializado y al analista contable durante la carga inicial de datos esenciales de una empresa. Aplica reglas de revisión formalizadas, compara los datos del sistema anterior contra estructuras de referencia precargadas, aprende de las decisiones del usuario y persiste todo el proceso como historial auditable.

El asistente es **transversal por diseño** — el mismo patrón opera para distintos casos de onboarding. En la versión inicial v1.0, el caso modelado es el **Plan Único de Cuentas (PUC)**. Casos futuros previstos: terceros, unidades organizacionales y saldos iniciales contables. Cada caso aporta sus reglas y referencias específicas; el patrón del proceso (cargar → analizar → revisar → decidir → generar → auditar) es el mismo.

### Contexto actual

Al implementar un cliente nuevo en el ERP, la consultora dedica tiempo significativo a analizar los datos del sistema anterior — particularmente el PUC — para construir las estructuras del modelo nuevo. El conocimiento sobre qué transformar (cuentas duplicadas por dimensión, atributos fiscales mal ubicados, niveles maestra/auxiliar inconsistentes) vive como conocimiento tácito de unos pocos consultores especializados.

### Problemas a resolver

- **Conocimiento tácito de la consultora:** las decisiones de revisión no están formalizadas; cada consultor aplica criterios similares pero no idénticos. Los nuevos consultores requieren tiempo de aprendizaje significativo.
- **Repetición sin aprendizaje:** las mismas decisiones se toman cliente tras cliente. El sistema no captura ni reutiliza ese conocimiento.
- **Sin historial persistido:** los intentos previos de carga no quedan registrados. Si el cliente carga varias versiones del PUC durante la revisión, no hay trazabilidad de qué se intentó y por qué se descartó.
- **Sin trazabilidad de decisiones:** una vez generado el PUC final, no es posible saber qué cuentas se consolidaron, qué atributos se descartaron o por qué un consultor rechazó una sugerencia.
- **Sin gobernanza de criterios:** las reglas de revisión no son visibles ni actualizables. El equipo de producto no puede mejorar las heurísticas con base en lo que aprende la consultora en campo.

### Resultado esperado

El asistente entrega cuatro capacidades clave:

1. **Revisión guiada e iterativa:** el consultor revisa los datos del sistema anterior agrupados por familia (en el caso PUC: por grupo contable), con sugerencias automáticas presentadas con justificación y consecuencias visibles antes de decidir.
2. **Historial persistido auditable:** cada intento de onboarding queda registrado completo. El consultor puede consultar procesos anteriores (incluso los abandonados) y entender qué se decidió y por qué.
3. **Aprendizaje progresivo:** las decisiones aceptadas alimentan un sistema de aprendizaje por empresa. Decisiones repetidas se detectan y sugieren automáticamente; el equipo de producto puede promover patrones consistentes a reglas formales del producto.
4. **Gobernanza centralizada:** las reglas de revisión y las estructuras de referencia son contenido del producto, mantenidas por el equipo de producto con permisos especiales. La consultora en campo no inventa reglas — las aplica o las solicita.

---

## Sección 2: Glosario de términos

| # | Término | Definición |
|---|---------|-----------|
| 1 | **Proceso de onboarding** | Conjunto completo de pasos que sigue una empresa para cargar inicialmente un tipo de dato esencial (en v1.0: el PUC). Tiene ciclo de vida con varios estados, registra todas las decisiones del consultor y produce el resultado final (en v1.0: el Plan Único de Cuentas operativo). |
| 2 | **Intento** | Cada ejecución de un proceso de onboarding para una empresa y un tipo de dato. Una empresa puede tener varios intentos a lo largo del tiempo (por ejemplo, si la primera carga del PUC fue incompleta y se reintenta con un archivo corregido). Todos los intentos quedan persistidos; solo uno termina como definitivo. |
| 3 | **Datos del sistema anterior** | Archivo o registro proveniente del ERP previo del cliente (típicamente Excel o CSV) que contiene las cuentas, terceros o estructuras a migrar. En el caso PUC, contiene el plan de cuentas legacy. |
| 4 | **Estructura de referencia** | Plantilla precargada en el producto que sirve como modelo comparativo. En el caso PUC, es un PUC base por sector o modelo de negocio (construcción, inmobiliaria, concesiones, administrativa). |
| 5 | **Regla de revisión** | Heurística formalizada que el asistente aplica para detectar patrones en los datos del sistema anterior y proponer una transformación al modelo nuevo. Cada regla pertenece a una categoría de tratamiento. |
| 6 | **Categoría de tratamiento** | Clasificación funcional de las acciones que el asistente puede sugerir. Cinco categorías: **Consolidar** (unificar registros duplicados por dimensión), **Conservar** (mantener tal cual — caso legítimo), **Reubicar** (la información pertenece a otro sub-dominio), **Foco** (área de mayor variabilidad que requiere análisis cuidadoso), **Validar** (verificación de integridad estructural). |
| 7 | **Sugerencia** | Recomendación específica generada por el motor de análisis. Cada sugerencia indica un registro o grupo de registros afectados, la regla que la origina, la transformación propuesta y las consecuencias de aceptarla o rechazarla. |
| 8 | **Decisión del consultor** | Respuesta del usuario ante una sugerencia. Cuatro acciones posibles: **Aceptar** (aplica la sugerencia tal cual), **Modificar** (ajusta los detalles de la sugerencia antes de aplicarla), **Rechazar** (descarta la sugerencia con justificación), **Aplazar** (pospone la decisión para resolverla más adelante en el proceso). |
| 9 | **Revisión por grupo** | Patrón de revisión en el que las sugerencias se presentan agrupadas por una dimensión natural del dato (en el caso PUC: por grupo contable 1-9, 11, 13, 14, etc.). Permite que el consultor pause entre grupos y reanude el proceso. |
| 10 | **Aprendizaje del asistente** | Conocimiento acumulado por empresa que el asistente construye a partir de las decisiones aceptadas del consultor. Patrones repetidos se detectan y se reutilizan en sugerencias futuras. Es específico de la empresa en v1.0. |
| 11 | **Generación final** | Acción que cierra el proceso de onboarding produciendo los datos definitivos en el sub-dominio correspondiente. En el caso PUC, genera el `PlanDeCuentas` con sus cuentas en el sub-dominio Contabilidad. Sólo puede ejecutarse una vez por proceso. |
| 12 | **Caso de onboarding** | Familia específica de datos que el asistente sabe procesar. v1.0 entrega el caso **PUC**. Casos futuros: terceros, unidades organizacionales, saldos iniciales. |

---

## Sección 3: Actores del sistema

### Actores internos (usuarios del sistema)

| Actor | Descripción | Responsabilidades en el asistente |
|-------|-------------|-----------------------------------|
| **Consultor especializado** | Profesional de la consultora con conocimiento profundo del dominio (contabilidad, terceros, etc.). Acompaña al cliente durante el onboarding. | Iniciar el proceso, cargar los datos del sistema anterior, seleccionar la estructura de referencia, revisar y decidir cada sugerencia, generar el resultado final. |
| **Analista contable** | Profesional del cliente que entiende su operación y aprueba el resultado del onboarding. | Validar las decisiones del consultor, aprobar el PUC final antes de la generación, consultar el historial de procesos para auditoría. |
| **Equipo de producto** | Equipo interno del fabricante del ERP que mantiene el catálogo de reglas y estructuras de referencia. | Crear y modificar reglas formales, publicar estructuras de referencia por sector o modelo de negocio, supervisar el aprendizaje agregado y promover patrones a reglas, atender solicitudes de la consultora cuando se identifican casos no cubiertos. |

### Actores externos (sistemas)

| Sistema | Descripción | Integración con el asistente |
|---------|-------------|------------------------------|
| **Sub-dominio Contabilidad** | Bounded context donde vive el agregado `PlanDeCuentas` y el agregado `MarcoContable`. | Receptor de la generación final del caso PUC: cuando el proceso alcanza el estado `GENERADO`, se desencadena la creación del PUC en Contabilidad. |
| **Servicio de Datos de Referencia** | Servicio compartido que provee jurisdicciones, monedas y catálogos transversales. | Consumido por el asistente para validar selecciones de contexto (jurisdicción de la empresa, monedas asociadas). |
| **Sub-dominio Impuestos** | Bounded context que gestiona tributos, tarifas y bases gravables. | Receptor conceptual — el asistente identifica atributos fiscales en los datos del sistema anterior y los descarta del PUC final (no se reubican automáticamente; las tarifas y bases ya viven en Impuestos por carga independiente). |

---

## Sección 4: Flujo principal del onboarding

El asistente opera en seis fases secuenciales con posibilidad de pausa y reanudación entre ellas. El diagrama general:

```
┌────────────────────────────────────────────────────────────────────────┐
│  Fase 1     Fase 2     Fase 3     Fase 4     Fase 5     Fase 6        │
│                                                                        │
│  Carga  →  Contexto → Análisis → Revisión → Confirmar → Generar       │
│  legacy    empresa   automático iterativa   resumen     resultado     │
│                                                                        │
│                                  (pausable                             │
│                                   y reanu-                             │
│                                   dable)                               │
└────────────────────────────────────────────────────────────────────────┘
```

### Fase 1 — Carga de los datos del sistema anterior

El consultor inicia un nuevo proceso de onboarding para una empresa y selecciona el tipo de dato a cargar (en v1.0: PUC). El sistema le permite cargar el archivo desde Excel o CSV, valida el formato (estructura mínima de columnas y tipos), y registra el archivo como punto de partida del intento.

Si la empresa tiene procesos previos abiertos para el mismo tipo de dato, el sistema le pregunta si desea continuar el proceso anterior o iniciar uno nuevo. Si elige iniciar uno nuevo, el proceso anterior pasa a estado `ABANDONADO` con historial completo conservado.

### Fase 2 — Contexto de la empresa

El consultor confirma o define los atributos de contexto que el asistente usa para seleccionar la estructura de referencia adecuada. En el caso PUC: sector económico (servicios, comercio, manufactura, etc.), modelo de negocio (construcción, inmobiliaria, concesiones, administrativa, otra) y jurisdicción de operación.

Con base en el contexto, el sistema sugiere la estructura de referencia más apropiada. El consultor puede aceptar la sugerencia o seleccionar manualmente otra estructura de referencia disponible.

### Fase 3 — Análisis automático

El asistente ejecuta el análisis comparando los datos del sistema anterior contra la estructura de referencia seleccionada y aplicando las reglas de revisión. La cadena de aplicación tiene tres niveles, en orden de precedencia:

1. **Reglas formales del catálogo del producto** — las heurísticas curadas por el equipo de producto.
2. **Aprendizaje de la empresa** — patrones que el asistente aprendió de procesos anteriores de la misma empresa.
3. **Comparación con la estructura de referencia y validaciones estructurales** — diferencias entre los datos cargados y la referencia, más validaciones de integridad (niveles, longitud de código, jerarquía).

El resultado es un conjunto de sugerencias clasificadas por categoría de tratamiento (Consolidar, Conservar, Reubicar, Foco, Validar) y agrupadas por la dimensión natural del dato (en el caso PUC: por grupo contable). El consultor recibe un resumen pre-revisión: cuántas sugerencias hay por categoría y por grupo.

### Fase 4 — Revisión iterativa por grupo

Esta es la fase más extensa del proceso y la más crítica para el consultor. El sistema presenta las sugerencias agrupadas, una por una, en una vista que incluye:

- El registro o grupo de registros afectados, con su contexto en los datos originales.
- La regla que originó la sugerencia y la categoría de tratamiento.
- La transformación propuesta (qué quedaría en el modelo nuevo).
- Las **consecuencias de aceptar** la sugerencia (qué cambia en los reportes, integraciones, operación).
- Las **consecuencias de rechazar** la sugerencia (qué problemas operativos podrían surgir si no se aplica).

El consultor decide entre cuatro acciones: **Aceptar**, **Modificar** (con detalle del ajuste), **Rechazar** (con justificación) o **Aplazar** (vuelve a presentarse al final del proceso). El sistema registra cada decisión.

El consultor puede pausar el proceso en cualquier momento. Cuando regresa, el sistema reconstruye el estado exacto: qué grupo estaba revisando, cuáles sugerencias ya decidió, cuáles quedan pendientes.

### Fase 5 — Confirmación del resumen

Cuando todas las sugerencias han sido decididas (incluyendo resolver las aplazadas), el sistema presenta un resumen del resultado:

- Cuántas sugerencias se aceptaron, modificaron, rechazaron.
- Cuántos registros quedaron en el resultado final vs. el original.
- Qué reglas fueron las más aplicadas.
- Lista de las decisiones con justificación.

El consultor puede volver atrás y modificar decisiones específicas, o continuar a la generación. El analista contable puede revisar este resumen y dar la aprobación final.

### Fase 6 — Generación del resultado

Cuando se confirma la generación, el sistema produce los datos definitivos en el sub-dominio correspondiente. En el caso PUC: se crea el `MarcoContable` (si es uno nuevo o custom), el `PlanDeCuentas`, y cada `CuentaContable` del PUC final. El proceso pasa al estado `GENERADO`, terminal, con todo el historial inmutable.

El consultor recibe un reporte de migración que describe en detalle qué se hizo, qué se consolidó, qué se descartó, y qué decisiones tomó cada paso. El analista contable puede descargar este reporte para auditoría interna del cliente.

---

## Sección 5: Integraciones con otros sub-dominios

### Sub-dominio Contabilidad — generación del resultado del caso PUC

Cuando el caso PUC alcanza el estado `GENERADO`, el asistente genera los siguientes elementos en el sub-dominio Contabilidad:

- Si el marco contable del proceso es custom (no es el marco NIIF predeterminado), se crea el agregado `MarcoContable` con su código, nombre y descripción.
- Se crea el agregado `PlanDeCuentas` asociado al marco, con el nombre seleccionado por el consultor.
- Se agregan todas las `CuentaContable` resultantes con sus atributos (código, nombre, tipo, nivel, obligatoriedad de tercero y unidad organizacional).

La generación es una coordinación inter-agregado garantizada por un domain service del asistente. El sub-dominio Contabilidad recibe los eventos como cualquier otra creación de PUC.

### Sub-dominio Impuestos — atributos fiscales descartados

Los datos del sistema anterior (PUC legacy de SincoA&F u otros ERPs locales) frecuentemente incluyen atributos fiscales en las cuentas: porcentajes de tarifa, bases mínimas, marcas de ciudad para ICA. **Estos atributos no pertenecen al PUC en el modelo nuevo** — viven en el sub-dominio Impuestos como `Tributo`, `TarifaTributaria`, `Jurisdiccion`.

El asistente detecta estos atributos en los datos del sistema anterior y aplica reglas de la categoría **Reubicar**. Las sugerencias para este caso indican al consultor que la información de tarifas y bases ya está disponible en Impuestos por carga independiente; el atributo se descarta del PUC limpio. No hay reubicación automática al sub-dominio Impuestos — solo se filtra del PUC.

### Servicio de Datos de Referencia — contexto de la empresa

El asistente consume el servicio de Datos de Referencia para:

- Validar la jurisdicción seleccionada en la fase de contexto (códigos ISO de país y división territorial).
- Validar la moneda funcional de la empresa si forma parte del contexto del onboarding.

---

## Sección 6: Reglas de negocio

Las reglas se agrupan por frente lógico. Los códigos `R##` son identificadores estables: los saltos en la numeración son consecuencia natural de la evolución del documento.

### Frente 1: Integridad del proceso

| ID | Regla | Configurable |
|----|-------|--------------|
| R01 | **Un proceso activo por empresa y caso:** una empresa puede tener varios procesos de onboarding del mismo caso a lo largo del tiempo, pero a lo sumo uno activo (no terminal) simultáneamente. Iniciar un proceso nuevo cuando ya existe uno activo requiere abandonar el anterior. | No |
| R02 | **Procesos terminales son inmutables:** un proceso en estado `ABANDONADO` o `GENERADO` no puede modificarse ni reanudarse. Su historial queda persistido para auditoría. | No |
| R03 | **Reanudación dentro de un proceso activo:** un consultor puede pausar y reanudar un proceso activo en cualquier momento de la fase de revisión iterativa. El sistema reconstruye el estado exacto. | No |
| R04 | **Cargar nuevos datos inicia un nuevo proceso:** si el consultor decide cargar un archivo distinto, el proceso actual pasa a `ABANDONADO` y se inicia uno nuevo. No se permite reemplazar el archivo dentro del mismo proceso. | No |

### Frente 2: Multi-intento y trazabilidad

| ID | Regla | Configurable |
|----|-------|--------------|
| R05 | **Todos los intentos se conservan:** cada proceso de onboarding queda persistido como agregado completo con todos sus eventos. Los procesos abandonados son consultables después de cerrados — no se eliminan. | No |
| R06 | **Solo un intento definitivo:** una empresa puede tener varios procesos en estado `ABANDONADO`, pero a lo sumo uno en estado `GENERADO`. La generación final cierra el camino del caso para esa empresa hasta que se inicie un nuevo proceso (lo cual abandonaría el anterior). | No |
| R07 | **Trazabilidad registro a registro:** el resultado final del proceso incluye, para cada registro generado, la referencia al origen del que proviene, las reglas que se aplicaron y las decisiones que tomó el consultor. | No |

### Frente 3: Decisiones del consultor

| ID | Regla | Configurable |
|----|-------|--------------|
| R08 | **Cuatro acciones disponibles por sugerencia:** Aceptar, Modificar, Rechazar, Aplazar. La acción aplicada queda registrada con marca de tiempo y usuario responsable. | No |
| R09 | **Modificar y Rechazar requieren justificación:** cuando el consultor modifica o rechaza una sugerencia, debe indicar la razón. La justificación queda persistida con la decisión. | No |
| R10 | **Aplazar es temporal:** una decisión aplazada vuelve a presentarse al consultor antes del cierre del proceso. No se puede generar el resultado final con sugerencias en estado `APLAZADA`. | No |
| R11 | **Aceptar puede ser explícito o sugerido por aprendizaje:** las decisiones pueden venir directamente del consultor o ser propuestas automáticamente por el aprendizaje del asistente. En ambos casos, el consultor confirma o ajusta antes de avanzar. | No |

### Frente 4: Aprendizaje del asistente

| ID | Regla | Configurable |
|----|-------|--------------|
| R12 | **Solo decisiones aceptadas alimentan el aprendizaje:** rechazos y aplazamientos no se aprenden. Las modificaciones se aprenden con el detalle del ajuste. | No |
| R13 | **Aprendizaje por empresa:** el conocimiento acumulado en el aprendizaje es específico de la empresa y no se comparte automáticamente entre empresas. | No |
| R14 | **Promoción a regla formal:** el equipo de producto puede revisar patrones de aprendizaje repetidos y promoverlos a reglas formales del catálogo del producto. La promoción es una acción explícita. | No |
| R15 | **Invalidación de aprendizaje:** el equipo de producto puede invalidar un aprendizaje específico. Procesos futuros no aplican el aprendizaje invalidado; procesos ya generados no se afectan. | No |

### Frente 5: Reglas y estructuras de referencia

| ID | Regla | Configurable |
|----|-------|--------------|
| R16 | **Reglas formales son contenido del producto:** las reglas de revisión vienen precargadas con el producto. El equipo de producto puede agregar, modificar o desactivar reglas con permisos especiales. El consultor en campo no crea reglas. | No |
| R17 | **Estructuras de referencia son contenido del producto:** las estructuras de referencia (en el caso PUC: los PUCs base por sector) vienen precargadas. El equipo de producto las mantiene. | No |
| R18 | **Inmutabilidad del código de regla y de referencia:** los códigos identificadores de reglas y estructuras de referencia son inmutables tras su publicación. Solo nombre, descripción y estado admiten modificación posterior. | No |

### Frente 6: Generación final

| ID | Regla | Configurable |
|----|-------|--------------|
| R19 | **Generación solo desde LISTO_PARA_GENERAR:** el proceso debe haber completado la revisión de todas las sugerencias antes de generar el resultado. No es posible saltarse la fase de revisión. | No |
| R20 | **Generación única:** la acción de generar el resultado se ejecuta una sola vez por proceso. Subsecuentes intentos para la misma empresa requieren nuevos procesos. | No |
| R21 | **Reporte de migración:** la generación produce un reporte descargable con el detalle de todas las decisiones tomadas, los registros consolidados, los descartados y los conservados. El reporte queda asociado al proceso y es consultable indefinidamente. | No |

---

## Sección 7: Qué está dentro y fuera del alcance

### Dentro del alcance v1.0

- **Caso PUC:** modelo completo del proceso de onboarding del Plan Único de Cuentas, con reglas de revisión, estructuras de referencia por sector y aprendizaje por empresa.
- **Carga desde Excel o CSV:** formato de entrada para los datos del sistema anterior.
- **Flujo de seis fases con revisión iterativa por grupo:** experiencia completa del consultor con pausabilidad y reanudación.
- **Historial multi-intento auditable:** todos los procesos de una empresa se conservan; solo uno termina como definitivo.
- **Sistema de aprendizaje por empresa:** decisiones aceptadas alimentan el aprendizaje específico de la empresa.
- **Catálogo de reglas formales:** reglas precargadas del producto; el equipo de producto las mantiene.
- **Catálogo de estructuras de referencia:** PUCs base por sector y modelo de negocio (construcción, inmobiliaria, concesiones, administrativa).
- **Integración con Contabilidad:** generación final del PUC en el sub-dominio Contabilidad.
- **Reporte de migración descargable:** documento auditable con el detalle del proceso.

### Fuera del alcance v1.0

- **Casos diferentes al PUC:** terceros, unidades organizacionales, saldos iniciales contables y cualquier otro caso de onboarding se modelarán cuando llegue el momento de cada uno. Cuando se aborde el segundo caso, se evaluará si el patrón se extrae como genérico reutilizable o se mantiene autónomo por caso.
- **Conexión directa a ERPs externos:** SincoA&F, Siigo, Alegra u otros sistemas no se conectan directamente. La entrada en v1.0 es archivo (Excel/CSV).
- **Aprendizaje global del producto:** el conocimiento aprendido por una empresa no se comparte automáticamente con otras. La evolución a aprendizaje global (con anonimización) es posible a futuro si el volumen lo justifica.
- **Vista unificada de onboarding del cliente:** una pantalla que agrupe todos los procesos de onboarding (PUC, terceros, unidades, saldos) de una misma empresa será una proyección de lectura cuando haya más de un caso modelado. No forma parte de v1.0.
- **Reubicación automática a otros sub-dominios:** los atributos fiscales detectados en el PUC legacy no se reubican automáticamente al sub-dominio Impuestos. Las tarifas y bases ya se cargan ahí por flujo independiente. El asistente solo descarta esos atributos del PUC final.

---

## Sección 8: Estrategia de implementación por fases

### Fase 1 (F1) — Asistente operativo del caso PUC

El alcance de F1 entrega el asistente completo para el caso PUC, integrado con el sub-dominio Contabilidad. Capacidades incluidas:

| Capacidad | Descripción |
|-----------|-------------|
| Proceso de onboarding del PUC | Agregado con ciclo de vida completo: Iniciado → En análisis → En revisión → Listo para generar → Generado / Abandonado. Persistencia auditable. |
| Carga de datos del sistema anterior | Importación desde Excel o CSV con validación de formato. |
| Análisis automático | Aplicación de la cadena de tres niveles (reglas formales → aprendizaje de la empresa → comparación con referencia + validaciones). |
| Revisión iterativa por grupo | Experiencia de UI guiada con pausabilidad y reanudación. Cuatro acciones por sugerencia (Aceptar, Modificar, Rechazar, Aplazar). |
| Catálogo de reglas formales | Reglas iniciales precargadas (mínimo las 12 identificadas) con mantenimiento por el equipo de producto. |
| Catálogo de estructuras de referencia | PUCs base por línea de negocio (construcción, inmobiliaria, concesiones, administrativa) precargados. |
| Aprendizaje por empresa | Sistema que aprende de las decisiones aceptadas y propone en procesos futuros de la misma empresa. |
| Promoción de aprendizajes a reglas | Capacidad del equipo de producto para revisar patrones y promoverlos a reglas formales. |
| Generación del PUC en Contabilidad | Domain service que coordina la creación del MarcoContable, el PlanDeCuentas y las CuentaContable en el sub-dominio Contabilidad. |
| Reporte de migración descargable | Documento auditable del proceso. |
| Historial multi-intento | Consulta de procesos abandonados y generados, con detalle completo. |

### Fases futuras (fuera de v1.0)

- **F2 — Caso Terceros:** asistente para onboarding de terceros con sus contextos por sub-dominio.
- **F3 — Caso Unidades Organizacionales:** asistente para onboarding de estructura organizacional.
- **F4 — Caso Saldos Iniciales Contables:** asistente para carga de saldos al activar N2 de Contabilidad.
- **Otros casos futuros:** catálogos de gasto, productos, conceptos de nómina, etc., según evolucione el ERP.

Cuando se aborde la F2, se evaluará si extraer el patrón genérico del proceso (`ProcesoOnboarding` con tipo) o mantener cada caso como agregado independiente. La decisión se basará en el grado de similitud real observado entre los dos casos.

---

## Sección 9: Beneficios esperados

| # | Beneficio | Descripción |
|---|-----------|-------------|
| 1 | **Reducción del tiempo de implementación** | El consultor avanza por un flujo guiado con sugerencias automáticas en lugar de revisar cuenta por cuenta manualmente. |
| 2 | **Estandarización del onboarding** | Todas las implementaciones aplican los mismos criterios formalizados, eliminando la variabilidad por consultor. |
| 3 | **Aprendizaje progresivo** | El asistente mejora con cada proceso; las decisiones repetidas se sugieren automáticamente. |
| 4 | **Auditoría completa** | Todo intento queda registrado; el cliente puede entender en cualquier momento cómo se construyó su PUC. |
| 5 | **Independencia del consultor especializado** | Consultores junior pueden ejecutar onboardings complejos siguiendo el flujo del asistente, con la calidad de un consultor senior. |
| 6 | **Visibilidad de mejora del producto** | El equipo de producto identifica patrones repetidos en los aprendizajes y mejora las reglas formales del catálogo. |
| 7 | **Reducción de errores operativos** | Las consecuencias de cada decisión son visibles antes de aplicarla; el consultor decide informado. |
| 8 | **Patrón reutilizable** | El proceso modelado en v1.0 sirve de base para futuros casos de onboarding (terceros, unidades, saldos), acelerando su entrega. |
| 9 | **Diferenciación competitiva** | Los ERPs grandes ofrecen herramientas de carga masiva con validaciones pero no asistentes nativos con heurísticas contables y aprendizaje progresivo. |
| 10 | **Trazabilidad regulatoria** | El reporte de migración descargable y el historial de intentos son materia prima para auditorías externas del cliente. |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Mayo 2026 | Versión inicial del alcance del servicio compartido `compartido/asistente-onboarding/`. Cubre el caso PUC como primer caso del patrón transversal. 9 secciones, 12 términos en glosario, 3 actores internos, 3 actores externos, 6 fases del flujo, 6 frentes de reglas (21 reglas), capacidades F1 completas, 10 beneficios esperados. Acompañado por `modelo-dominio.md` v1.0 (4 agregados: PUCdeReferencia, ReglaDeRevisionPUC, AprendizajeOnboardingPUC, ProcesoOnboardingPUC con FSM) y `casos/onboarding-puc.md` v1.0 (12 criterios detallados, 5 categorías de tratamiento, casos por línea de negocio, 6 fases UX). Capacidad referenciada desde `dominio/contabilidad/definicion-alcance.md` v1.3 como capacidad F1 del producto. |
