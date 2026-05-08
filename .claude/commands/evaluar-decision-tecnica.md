Evalúa críticamente un argumento técnico sobre una implementación ya hecha o por hacer. Determina si las decisiones descritas son acertadas, sensatas, proporcionales y alineadas con el código actual, los estándares del proyecto, la arquitectura documentada, la documentación oficial de tecnologías relevantes y las restricciones de dominio y mantenibilidad.

El argumento puede venir en `$ARGUMENTS` (si es una referencia corta o ID) o en el cuerpo del mensaje con este formato:

```
Argumento: [decisión o conclusión técnica a evaluar]
Contexto: [implementado / por implementar / por refactorizar]
Archivos relevantes: [opcional — lista de rutas]
```

Si no se provee argumento, usar `AskUserQuestion` para solicitarlo antes de continuar.

---

## Restricción crítica de arquitectura — Marten + polimorfismo en eventos

**PROHIBIDO** colocar tipos abstractos, interfaces o jerarquías polimórficas como **campos** dentro de records de evento de Marten.

Los eventos son historia inmutable en JSONB. Un campo abstracto embebe un discriminador `$type` que:
- Queda permanentemente en los streams históricos de PostgreSQL.
- No puede arreglarse con `EventUpcaster<TOld, TNew>` (opera al nivel del evento, no de sub-campos).
- Falla silenciosamente con JSONB si `AllowOutOfOrderMetadataProperties = true` no está configurado — y **no lo está** en este proyecto.
- Acopla nombres de clases C# a historia inmutable.

Si el argumento a evaluar involucra eventos de Marten, aplicar esta restricción al evaluar viabilidad. Los campos de evento deben ser primitivos (`Guid`, `string`, `decimal`, `DateTime`, `bool`), enums, o value objects **planos** sin herencia.

---

## Paso 1 — Leer el argumento y reunir contexto del proyecto

Lee el argumento, el contexto y los archivos relevantes indicados.

### Documentación de estándares del proyecto

Leer siempre, independientemente del dominio:
- `CLAUDE.md` (o `AGENTS.md` si existe) — principios, convenciones y restricciones del proyecto.
- Cualquier archivo de instrucciones en `.claude/` relevante para el argumento.

### Documentación de dominio

El argumento puede pertenecer a cualquier dominio o sub-dominio. Identificar de qué dominio trata y buscar su documentación:

1. Si el argumento menciona rutas de documentación explícitamente, leerlas.
2. Si no, buscar documentación relevante usando patrones comunes: `Definiciones/`, `docs/`, `Documentation/`, `*.md` en la raíz del módulo afectado.
3. Leer las secciones pertinentes — no toda la documentación disponible, sino la que se relaciona directamente con el argumento (glosario, reglas de negocio, decisiones de diseño, modelo de dominio).

### Historial de decisiones

Si existen, leer como contexto histórico no autoritativo:
- `AIResume/DiagnosticoYPlanDominio.md`
- `AIResume/RevisionCodigo.md`
- Cualquier archivo `DecisionTecnica_*.md` en `AIResume/` relacionado con el tema.

Priorizar siempre evidencia actual del código y documentación sobre memoria histórica.

---

## Paso 2 — Separar componentes del argumento

Antes de evaluar, clasificar explícitamente cada afirmación del argumento:

| Tipo | Descripción |
|---|---|
| **Hecho verificable** | Afirmación sobre el código o documentación que puede confirmarse leyendo el archivo. |
| **Supuesto** | Premisa que el argumento da por válida sin demostrar. |
| **Opinión** | Juicio de valor sobre calidad, preferencia o estilo. |
| **Conclusión** | Decisión o recomendación que se deriva de los anteriores. |

Producir una tabla con esta clasificación. No evaluar aún — solo separar.

---

## Paso 3 — Recopilar evidencia

### Si la implementación ya existe

Leer los archivos relevantes indicados. Si no se indicaron archivos, inferir las rutas a partir del argumento y leerlos.

Para cada hecho verificable del Paso 2, confirmar si el código lo respalda o lo contradice. Anotar líneas específicas como evidencia.

### Si la implementación aún no existe

Buscar patrones similares en el repositorio:
- ¿Hay agregados, VOs, eventos o servicios que resuelvan un problema análogo?
- ¿La solución propuesta sigue o rompe el patrón establecido en el proyecto?

### Si el argumento depende de tecnología externa

Si el argumento hace afirmaciones sobre Marten, Wolverine, gRPC, PostgreSQL/JSONB u otra tecnología verificable con documentación oficial, usar `WebSearch` o `WebFetch` para confirmar. No asumir que una afirmación técnica es correcta por estar bien redactada.

---

## Paso 4 — Evaluar

Evaluar el argumento contra cada dimensión. Ser directo: indicar qué está bien respaldado, qué es débil y qué está demostrado como incorrecto.

### 4.1 Correctitud factual
¿Los hechos verificables del argumento coinciden con lo que dice el código y la documentación? Listar coincidencias y divergencias con referencia a archivo:línea o documento:sección.

### 4.2 Validez de supuestos
¿Los supuestos son razonables dado el contexto del proyecto? ¿Alguno puede demostrarse falso?

### 4.3 Proporcionalidad
¿La complejidad introducida es proporcional al problema real? Señalar si la solución es excesiva para el volumen o contexto actual, o si es insuficiente.

### 4.4 Alineación con DDD / ES / CQRS / SOLID
- ¿Respeta las fronteras de agregado?
- ¿Preserva invariantes existentes?
- ¿Los eventos propuestos cumplen la restricción de Marten (sección inicial)?
- ¿Hay riesgos de replay, idempotencia o compatibilidad de streams?
- ¿Respeta SRP, OCP, LSP, ISP, DIP según aplique?
- ¿Mantiene CQS (command/query separation)?

### 4.5 Alineación con convenciones del repositorio
- ¿Sigue los patrones de naming del proyecto (CLAUDE.md)?
- ¿Es coherente con la estructura de carpetas y capas existente?
- ¿Introduce infraestructura accidental, duplicación conceptual o acoplamiento innecesario?

### 4.6 Riesgos de regresión
- ¿Cambia contratos públicos (API, gRPC, eventos)?
- ¿Afecta streams históricos en el Event Store?
- ¿Puede romper tests existentes sin que sea intencional?
- ¿Hay ventanas de inconsistencia eventual no documentadas?

### 4.7 Alternativas y tradeoffs
Si el argumento descarta alternativas, evaluar si el descarte está justificado. No reabrir decisiones históricas ya cerradas salvo que exista nueva evidencia objetiva.

---

## Paso 5 — Respuesta

```markdown
## Veredicto

[Una de: **Aceptar** / **Aceptar con ajustes** / **Rechazar** / **Inconcluso por falta de evidencia**]

## Confianza

[**Alta** / **Media** / **Baja**]
[Una oración que justifica el nivel de confianza — qué evidencia tuvo / qué faltó.]

## Evaluación

### Qué es correcto
[Lista de afirmaciones del argumento respaldadas por evidencia concreta. Citar archivo:línea o documento:sección.]

### Qué es débil, incompleto o incorrecto
[Lista de afirmaciones que son supuestos no demostrados, contradicen el código, o tienen razonamiento insuficiente.]

### Supuestos no demostrados
[Lista de premisas que el argumento asume sin verificar. Indicar si pueden verificarse y cómo.]

### Evidencia que respalda el juicio
[Lista de referencias concretas — archivo:línea, documento:sección — que fundamentan el veredicto.]

## Alineación

| Dimensión | Estado | Notas |
|---|---|---|
| Arquitectura del proyecto | ✅ / ⚠️ / ❌ | |
| Lenguaje de dominio (ubiquitous language) | ✅ / ⚠️ / ❌ | |
| SOLID | ✅ / ⚠️ / ❌ | |
| DDD táctico | ✅ / ⚠️ / ❌ | |
| Event Sourcing | ✅ / ⚠️ / ❌ | |
| CQRS | ✅ / ⚠️ / ❌ | |
| Marten / Wolverine | ✅ / ⚠️ / ❌ | |
| Testing esperado (TDD, cobertura de invariantes) | ✅ / ⚠️ / ❌ | |
| Simplicidad y mantenibilidad | ✅ / ⚠️ / ❌ | |

## Riesgos

[Lista de riesgos concretos y verificables — no teóricos. Para cada uno: qué puede pasar, bajo qué condición, con qué probabilidad estimada (alta/media/baja).]

## Recomendación

[Decisión recomendada en una oración. Si el veredicto es "Aceptar con ajustes": los ajustes mínimos necesarios. Si es "Rechazar": qué alternativa es preferible y por qué. Si es "Inconcluso": qué información falta y cómo obtenerla.]
```

---

## Paso 6 — Guardar el resultado

Guardar en `AIResume/DecisionTecnica_[NombreDescriptivo].md` con encabezado:

```markdown
# Evaluación de Decisión Técnica — [Nombre descriptivo de la decisión]

**Fecha:** [fecha]
**Rama:** [git branch --show-current]
**Veredicto:** [veredicto]
**Confianza:** [nivel]
```

Seguido del contenido completo del Paso 5.

Mostrar al usuario:
- Veredicto y confianza.
- Los 2-3 puntos más importantes de la evaluación (uno por cada sección con hallazgos relevantes).
- Path del archivo guardado.
- Si el veredicto es "Aceptar con ajustes" o "Rechazar", indicar si los cambios recomendados encajan como hallazgo implementable con `/implementar-hallazgo` o como falencia con `/implementar-falencia`.

---

## Reglas de evaluación

- Ser directo y técnico. No dar aprobaciones genéricas.
- No inventar documentación. Si no se puede verificar, declararlo explícitamente.
- No sobreoptimizar: si una decisión es válida pero excesiva para el volumen o contexto actual, decirlo.
- No rechazar por preferencia personal si la decisión es técnicamente válida.
- Si una decisión es simple pero rompe una regla arquitectónica, decirlo aunque sea simple.
- No reabrir decisiones históricas cerradas salvo nueva evidencia objetiva.
- Si falta evidencia para decidir, listar exactamente qué información falta — no emitir veredicto con baja confianza sin explicitar el porqué.
- Priorizar siempre evidencia actual del código y documentación sobre memoria histórica.
