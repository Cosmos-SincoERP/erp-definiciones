Analiza el dominio especificado en `$ARGUMENTS` comparando la documentación contra el código implementado, construye inventarios explícitos de ambos lados, diferea para detectar brechas, y genera `AIResume/DiagnosticoYPlanDominio.md` con el diagnóstico del estado actual y un plan de implementación ordenado e incremental donde cada ítem es autosuficiente.

**Uso:** `/analisis-plan-dominio Definiciones/mi-dominio/`

Si no se provee `$ARGUMENTS`, usar `AskUserQuestion` para solicitar la ruta antes de continuar.

---

## Paso 0 — Orientación

Ejecutar en paralelo antes de leer cualquier archivo:

```bash
# Descubrir solución y estructura del proyecto
ls *.sln
ls -d */
```

Identificar y retener:

- **Nombre de la solución** (`*.sln`) — todos los paths de proyectos se derivan de aquí.
- **Proyectos presentes** — buscar carpetas con los patrones estándar:
  `*.Dominio/`, `*.Dominio.Tests/`, `*.Consultas/`, `*.Consultas.Tests/`,
  `*.Comandos.API/`, `*.Consultas.API/`, `*.Comandos.Grpc/`, `*.Consultas.Grpc/`,
  `*.Seed/`, `*.AcceptanceTests/`
- **Archivos de documentación** — listar todos los `*.md` en `$ARGUMENTS` y subdirectorios.
- **Estado previo** — verificar si existe `AIResume/DiagnosticoYPlanDominio.md`.

---

## Paso 1 — Leer documentación y construir el inventario del modelo

Leer **todos** los archivos `.md` en `$ARGUMENTS`. El objetivo no es "leer y recordar" — es construir listas explícitas que se van a usar como fuente del diff en el Paso 3.

### 1.1 Inventario de agregados (Lista A-Agregados)

Por cada agregado documentado, construir la siguiente ficha:

```
Agregado: [Nombre]
  Fase: F1 / F2
  Tipo: configuración (sin FSM) / transaccional (con FSM)
  Depende de: [otros agregados o services que necesita para existir]
  Comandos: [lista completa de comandos con su payload]
  Eventos: [lista completa de eventos con su payload]
  Comportamientos calculados: [métodos públicos documentados: nombre, input, output]
  Invariantes aplicables: [IDs de invariantes I* que este agregado debe enforcer]
  Proyecciones documentadas: [read models que se nutren de sus eventos]
  Endpoint REST: [sí/no, ruta]
  Contrato gRPC: [sí/no, operación]
```

### 1.2 Inventario de domain services (Lista A-Services)

Por cada domain service documentado:

```
Service: [Nombre]
  Fase: F1 / F2
  Tipo: stateless (no persiste) / con estado
  Pipeline: [pasos del pipeline en orden, con lo que lee y escribe]
  Puertos requeridos: [interfaces que el dominio debe definir]
  Depende de: [agregados que consulta]
```

### 1.3 Inventario de proyecciones y read models (Lista A-Proyecciones)

Por cada proyección documentada:

```
Proyección: [Nombre]
  Tipo: SingleStream / MultiStream
  Read model: [campos con sus tipos]
  Fuentes de eventos: [agregados cuyos eventos alimentan esta proyección]
  Query handler: [nombre, query de entrada, resultado]
  Endpoint: [ruta, verbo]
```

### 1.4 Inventario de invariantes (Lista A-Invariantes)

Por cada invariante `[I*]` documentada:

```
I[N]: [texto de la invariante]
  Clasificación: local (un agregado) / eventual (cruce de agregados)
  Debe enforcearse en: [lugar correcto según clasificación]
```

### 1.5 Inventario de reglas con aplicación en código (Lista A-Reglas)

Solo las reglas `[R*]` que se traducen a validaciones, condiciones o comportamientos en el código (no reglas de producto que no tienen impacto técnico):

```
R[N]: [texto de la regla]
  Aplica en: [agregado / service / endpoint]
```

### 1.6 Inventario de pendientes por definir (Lista A-Pendientes)

Registrar todos los `[PD*]` del modelo — son los ítems que irán directamente a la Sección 4 del output ("Requiere especificación") y bloquean la implementación hasta que el equipo los resuelva:

```
PD[N]: [texto del pendiente]
  Condición de activación documentada: [cuándo debe resolverse]
  Componentes bloqueados: [qué ítems del plan dependen de esta definición]
```

### 1.7 Inventario de seed / contenido estándar (Lista A-Seed)

Por cada conjunto de datos estándar documentado (por jurisdicción, tenant, contexto):

```
Contexto: [nombre]
  Entidades: [lista de agregados con cantidad de instancias esperadas]
  Identificadores fijos: [GUIDs deterministas si están documentados]
```

### 1.8 Inventario de integraciones (Lista A-Integraciones)

```
Canal gRPC:
  Operaciones documentadas: [lista con contrato]

Canal REST:
  Endpoints documentados: [lista con verbo, ruta, payload]
```

---

## Paso 2 — Explorar el código y construir el inventario de la implementación

El objetivo es el mismo: listas explícitas, no "leer y recordar". Explorar en paralelo por tipo de proyecto.

### 2.1 `*.Dominio/` → Lista B-Dominio

Por cada carpeta de agregado encontrada:

```
Agregado: [Nombre] (carpeta: [ruta])
  Comandos implementados: [lista — archivo Commands/*.cs]
  Eventos implementados: [lista — archivo Events/*.cs]
  Handlers implementados: [lista — carpeta CommandHandlers/]
  Entidades internas: [lista — carpeta Entities/]
  Value Objects: [lista — carpeta ValueObjects/]
  Excepciones: [lista — carpeta Exceptions/]
```

Por cada domain service encontrado en `Compartidos/Services/` o equivalente:

```
Service: [Nombre]
  Interfaces/ports definidos: [sí/no]
  Implementación: [sí/no]
  Pipeline implementado: [pasos que existen]
```

Shared: listar enums (`Compartidos/Enums/`), VOs compartidos, interfaces/ports, extensions.

### 2.2 `*.Dominio.Tests/` → Lista B-Tests

```
Por agregado:
  [NombreAgregado]: N [Fact] en [N] clases de test
Por service:
  [NombreService]: N [Fact]
```

### 2.3 `*.Consultas/` → Lista B-Proyecciones

Por cada proyección encontrada:

```
Proyección: [Nombre]
  Tipo: SingleStream / MultiStream
  Read model: [campos con sus tipos — leer el record]
  Query handler: [existe / no existe]
```

### 2.4 `*.Consultas.API/` y `*.Comandos.API/` → Lista B-Endpoints

```
Endpoints expuestos:
  [verbo] [ruta] → [handler] (Commands/Queries API)
Request DTOs: [lista de archivos Requests/]
```

### 2.5 `*.Comandos.Grpc/` y `*.Consultas.Grpc/` → Lista B-gRPC

```
Operaciones gRPC expuestas: [lista]
Contratos .proto: [existe / no existe]
```

### 2.6 `*.Seed/` → Lista B-Seed

```
Por contexto sembrado:
  [Nombre]: [entidades y cantidad de instancias sembradas]
```

### 2.7 `*.Seed.Tests/` → Lista B-SeedTests

```
Por contexto con tests:
  [Nombre]: N [Fact] — qué comportamiento del seed está cubierto
```

Los tests del seed revelan qué comportamiento se considera correcto y cuál es el estado esperado tras la carga inicial. Si hay tests en rojo o ausentes para un contexto documentado, es una brecha adicional.

---

## Paso 3 — Comparar inventarios y construir el diff

Con las listas A y B construidas, realizar el diff sistemático. Para cada elemento de las listas A:

### 3.1 Diff de agregados

| Agregado | Comandos A | Comandos B | Eventos A | Eventos B | Behaviors A | Behaviors B | Tests | Estado |
|---|---|---|---|---|---|---|---|---|
| [Nombre] | N | N | N | N | N | N | N [Fact] | ✅/🔄/⬜ |

Anotar qué falta exactamente: ¿un comando específico? ¿un evento con payload incompleto? ¿un comportamiento calculado sin implementar?

### 3.2 Diff de domain services

| Service | Pipeline A (pasos) | Pipeline B (pasos) | Puertos definidos | Tests | Estado |
|---|---|---|---|---|---|
| [Nombre] | N | N | ✅/⬜ | N [Fact] | ✅/🔄/⬜ |

### 3.3 Diff de proyecciones

| Proyección | Read model completo | Query handler | Endpoint | Tests | Estado |
|---|---|---|---|---|---|
| [Nombre] | ✅/🔄/⬜ | ✅/⬜ | ✅/⬜ | N [Fact] | ✅/🔄/⬜ |

### 3.4 Diff de invariantes

| Invariante | Clasificación | ¿Enforced? | ¿En el lugar correcto? |
|---|---|---|---|
| I[N] | local/eventual | ✅/⬜ | ✅/⬜ |

### 3.5 Diff de reglas

| Regla | Componente | ¿Implementada? |
|---|---|---|
| R[N] | [agregado/service] | ✅/⬜ |

### 3.6 Diff de seed

| Contexto | Entidades A | Entidades B | Estado |
|---|---|---|---|
| [Nombre] | N | N | ✅/🔄/⬜ |

### 3.7 Diff de integraciones (REST + gRPC)

| Operación | Documentada | Implementada | Contrato coincide |
|---|---|---|---|
| [verbo/ruta u operación] | ✅ | ✅/⬜ | ✅/⬜ |

### 3.8 Gold-plating

Listar todo lo que está en las listas B pero **no** en las listas A. Puede ser válido (decisión de implementación) o deuda invisible.

---

## Paso 4 — Revisar diagnóstico previo (si existe)

Si existe `AIResume/DiagnosticoYPlanDominio.md`:

1. Leerlo completo.
2. **Para cada ítem marcado ✅**: verificar contra el código actual que sigue siendo correcto — no asumir que el estado no regresionó.
3. Clasificar cada ítem previo:
   - **Sigue ✅**: verificado en el Paso 3.
   - **Regresionó**: estaba ✅, el diff del Paso 3 muestra que ya no.
   - **Sigue pendiente**: sin cambio respecto al diagnóstico anterior.
   - **Ya no aplica**: la documentación cambió o el scope se redujo.
   - **Nuevo**: no estaba en el diagnóstico anterior.

---

## Paso 5 — Construir el plan de implementación

### 5.0 Descomponer en comportamientos individuales

El diff del Paso 3 opera a nivel de componente (un agregado, un service, una proyección). El plan opera a nivel de comportamiento concreto. Este paso hace la transición explícita.

**Para cada componente faltante o parcial del diff**, listar cada uno de sus comportamientos como un candidato de ítem separado:

- Por cada comando faltante → un candidato de ítem
- Por cada comportamiento calculado no implementado → un candidato de ítem
- Por cada paso de pipeline de un domain service faltante → un candidato de ítem
- Por cada proyección o query handler faltante → un candidato de ítem

El resultado de este paso es una **lista plana de candidatos** — todos los comportamientos individuales pendientes de implementación, sin agrupar ni ordenar todavía. Esta lista es la entrada del paso 5.1 y 5.2.

> No omitir comportamientos por parecer pequeños o triviales. El tamaño del comportamiento no determina si merece ítem propio — su posición en el grafo de dependencias sí.

### 5.1 Clasificar cada brecha

Para cada candidato de la lista del paso 5.0, asignar una de estas categorías:

**Directamente implementable** — la documentación provee todo lo necesario. Se pueden escribir los tests en rojo ahora mismo sin hacer preguntas adicionales.

**Con decisión de diseño** — la documentación describe la necesidad pero hay al menos un aspecto de implementación que requiere una decisión no documentada. Identificar la pregunta exacta.

**Requiere especificación** — el comportamiento esperado no está suficientemente definido. No se pueden escribir los tests sin obtener más información.

### 5.2 Ordenar por dependencia de dominio

El orden lo determina el conocimiento del dominio adquirido en los Pasos 1 y 2. **No es un algoritmo de capas técnicas — es razonamiento sobre dependencias de existencia**: un concepto del dominio no puede ser implementado ni probado antes que los conceptos de los que depende estén presentes.

**Principio rector:** el ítem N solo puede implementarse una vez que los ítems 1..N-1 están completos. Esto se verifica porque el test del ítem N necesita que el código de los ítems anteriores exista para poder compilar y fallar en rojo.

**Cómo determinar el orden:**

1. **Identificar las dependencias de existencia del dominio** — qué conceptos deben estar implementados para que otros puedan ser creados o verificados. Por ejemplo, en un dominio de pedidos `Cliente` y `Producto` son independientes entre sí; `LineaDePedido` depende de ambos; `Pedido` depende de `LineaDePedido`. En cualquier dominio existe una estructura análoga — el grafo de estas dependencias determina el orden del plan.

2. **Respetar el orden de dependencia** — si B requiere que A exista para poder ser creado o probado, A precede a B. Este orden surge de comprender el dominio, no de clasificar los artefactos por tipo técnico.

3. **Dentro de un mismo componente, los comportamientos que lo establecen preceden a los que operan sobre él:**
   - `Crear` antes que `Modificar` o `Eliminar` — operar sobre algo inexistente carece de sentido en el dominio
   - `AgregarElemento` antes que `ModificarElemento` — la modificación presupone la existencia del elemento
   - `Crear` antes que cualquier consulta — no hay estado que consultar si el componente no fue creado

4. **Aplicar el conocimiento del dominio** — si la documentación establece que el componente A es prerequisito del componente B (porque B valida contra A, o porque B referencia entidades que solo A produce), ese orden debe reflejarse en el plan con independencia de la categoría técnica de cada uno.

### 5.3 Granularidad de ítems

**Un ítem = un comportamiento concreto con su test.** No un agregado completo.

Para un agregado cualquiera del dominio, la secuencia típica de ítems sería:
- `Crear[Entidad]` — primer ítem; establece la entidad en el sistema
- `Agregar[ElementoAsociado]` — ítem posterior; requiere que la entidad exista
- `Modificar[ElementoAsociado]` — ítem posterior; requiere que el elemento esté agregado
- `Cerrar[Entidad]` — ítem posterior; requiere entidades en estado activo

**Agrupación permitida** — varios comportamientos pueden ir en un mismo ítem solo cuando:
- Sus tests no tienen dependencia entre sí (ninguno requiere que el otro esté implementado)
- El conjunto es suficientemente pequeño para ser implementado en una sola sesión

**No aceptable:**
- Un ítem que abarca todos los comportamientos de un componente
- Un ítem que abarca varios componentes sin relación entre sí

**Criterio de separación:** si el test del comportamiento B requiere que el comportamiento A esté implementado para poder compilar o ejecutarse, A y B son ítems separados.

### 5.4 Verificar autosuficiencia TDD de cada ítem

Antes de escribir el ítem en el plan, verificar que cumple **todos** los criterios:

- [ ] El test descrito puede escribirse en **rojo** ahora mismo, asumiendo que todos los ítems anteriores están implementados.
- [ ] El test puede **compilar** con los ítems anteriores implementados — no faltan types, comandos ni eventos de ítems previos.
- [ ] El test falla por **comportamiento** (la lógica no existe), no por tipos faltantes (eso indicaría una dependencia no declarada).
- [ ] La sección "Lo mínimo para que el test pase" no incluye nada que el test de este ítem no necesite. Sin excepciones especulativas, sin VOs no requeridos, sin helpers no usados.
- [ ] Un developer puede abrir este ítem, escribir el test, implementar, y tener todo verde sin necesitar contexto adicional.

### 5.5 Formato de cada ítem

```markdown
### [N]. [Nombre del comportamiento concreto] `[F1/F2]` `[Directamente implementable / Con decisión / Requiere spec]`

**Explicación**
Qué hace el sistema actualmente. Si es ausencia total, qué no puede funcionar
como consecuencia. Si es parcial, qué falta exactamente.

**Respaldo en la documentación**
> "[cita textual — suficiente para entender el comportamiento sin releer el doc completo]"
> — `[nombre-archivo].md`, Sección [N.N]

> "[cita de invariante o regla relevante si aplica]"
> — `[nombre-archivo].md`, `[I*]` / `[R*]` / `[D*]`

**Ejemplo**
- Acción: [qué comando se ejecuta / qué hace el consumidor]
- Comportamiento actual: [qué pasa hoy]
- Comportamiento esperado: [qué debería pasar según la documentación]

**Test que define este comportamiento**
El test a escribir en rojo antes de implementar:
- Nombre: `Si_[EntidadCompletaYEstadoExacto]_Debe_[OutcomeObservable]`
  - Convención obligatoria: `Si`, `Debe` y `NoDebe` son los **únicos** separadores snake_case. X e Y son PascalCase sin underscores internos.
  - X debe nombrar la entidad completa y el estado exacto: `EntidadPrincipalConDatosValidos`, no `Entidad` ni `DatosValidos`.
  - Y debe describir el outcome observable y concreto: `EmitirEntidadCreada`, no `Funcionar` ni `EstarBien`.
- Qué verifica: [comportamiento observable que debe existir]
- Por qué falla (rojo): [qué no existe todavía que hace fallar el test]
- Casos borde obligatorios: [mínimo que el dominio exige — límites, invariantes]

**Lo mínimo para que el test pase**
Solo lo que el test necesita para compilar y pasar — nada más:
- `[Proyecto]/[Carpeta]/[Archivo].cs`: [crear / modificar — exactamente qué y por qué lo necesita el test]
- `[Proyecto]/[Carpeta]/[Archivo].cs`: [crear / modificar]

> Si hay un tipo (excepción, VO, helper) que podría crearse aquí pero ningún test
> de este ítem lo necesita todavía → no va en este ítem.

**Habilita:** [qué ítems posteriores pueden escribir su test una vez que este esté verde]
**Depende de:** ítem(s) [N] — [por qué los necesita]
```

---

## Paso 6 — Generar `AIResume/DiagnosticoYPlanDominio.md`

Crear o sobreescribir con la siguiente estructura:

```markdown
# Diagnóstico y Plan del Dominio — [Nombre del dominio]

**Fecha:** [fecha actual — git log -1 --format=%ci para precisión]
**Rama:** [git branch --show-current]
**Documentación analizada:** `$ARGUMENTS`
**Solución:** `[Nombre].sln`

---

## Resumen ejecutivo

| Estado | Cantidad |
|---|---|
| ✅ Implementados y verificados | N |
| 🔄 Parcialmente implementados | N |
| ⬜ Pendientes | N |
| 🔁 Regresiones detectadas | N |
| ❓ Gold-plating (sin respaldo en documentación) | N |
| **Total ítems del plan** | **N** |

---

## Sección 1 — Inventarios y diff

### Agregados

| Agregado | Comandos A/B | Eventos A/B | Behaviors A/B | Tests | Estado |
|---|---|---|---|---|---|
| `[Nombre]` | N/N | N/N | N/N | N [Fact] | ✅/🔄/⬜ |

### Domain Services

| Service | Pipeline A/B | Puertos | Tests | Estado |
|---|---|---|---|---|
| `[Nombre]` | N/N pasos | ✅/⬜ | N [Fact] | ✅/🔄/⬜ |

### Proyecciones

| Proyección | Read model | Query handler | Endpoint | Tests | Estado |
|---|---|---|---|---|---|
| `[Nombre]` | ✅/🔄/⬜ | ✅/⬜ | ✅/⬜ | N [Fact] | ✅/🔄/⬜ |

### Invariantes

| ID | ¿Enforced? | ¿En el lugar correcto? |
|---|---|---|
| I[N] | ✅/⬜ | ✅/⬜ |

### Seed

| Contexto | Entidades A/B | Estado |
|---|---|---|
| [Nombre] | N/N | ✅/🔄/⬜ |

### Gold-plating detectado

[Lista de implementaciones sin respaldo en la documentación — con archivo y descripción breve.]

---

## Sección 2 — Plan de implementación

> Cada ítem es un comportamiento concreto con su ciclo TDD completo.
> El orden es por dependencia de dominio: cada ítem puede escribirse en rojo
> solo cuando todos los ítems anteriores están implementados.
> Implementar cada ítem con `/implementar "[nombre del ítem]"`.

### [1]. [Nombre del comportamiento] `[F1]` `[Directamente implementable]`

[formato del Paso 5.5]

---

### [2]. [Nombre del comportamiento] `[F1]` `[Directamente implementable]`

[formato del Paso 5.5]

---

[... continúa en orden de dependencia de dominio, sin agrupar por capas técnicas ...]

---

## Sección 3 — Ítems con decisión de diseño pendiente

### [N]. [Nombre]
**Decisión requerida:** [pregunta concreta — no puede implementarse sin responderla]
**Opciones identificadas:** [alternativas si las hay]
**Una vez decidido, implementar:** [descripción de los pasos]

---

## Sección 4 — Ítems que requieren especificación

### [N]. [Nombre]
**Por qué la documentación es insuficiente:** [explicación]
**Preguntas que deben responderse:** [lista numerada]

---

## Sección 5 — Regresiones detectadas

### [N]. [Nombre] 🔁
**Estaba marcado ✅ en el diagnóstico anterior.**
**Qué regresionó:** [descripción concreta de lo que dejó de cumplirse]
**Verificado en:** [archivo y línea donde se observó la regresión]

---

## Changelog

| Versión | Fecha | Descripción |
|---|---|---|
| 1.0 | [fecha] | Diagnóstico inicial: N agregados, N services, N proyecciones. N ítems en el plan. |
| 1.1 | [fecha] | [qué cambió] |
```

---

## Paso 7 — Mostrar resumen al usuario

Presentar en este orden:

1. **Tabla resumen** — ítems por estado (✅ / 🔄 / ⬜ / 🔁 / ❓).
2. **Regresiones** (si las hay) — mención explícita con prioridad, requieren atención inmediata antes de avanzar en el plan.
3. **Gold-plating** (si hay) — para que el usuario decida si es intencional o deuda.
4. **Top 3 ítems del plan** — los primeros de Capa 1 y 2 que desbloquean todo lo que sigue.
5. **Ítems bloqueados** — cuántos requieren decisión o especificación, con las preguntas concretas.
6. **Próximo paso**: `implementar "[nombre del ítem 1]"` — escribir el test en rojo primero, luego implementar lo mínimo para que pase.

---

## Consideraciones

- **No inventar**: toda brecha debe estar respaldada por evidencia en el código o en la documentación. Si algo parece un gap pero no hay evidencia clara, no incluirlo.
- **No asumir fases**: verificar explícitamente en la documentación la clasificación `[F1]`/`[F2]` de cada capacidad antes de incluirla.
- **Distinguir gap de decisión pendiente**: un gap es algo que debería estar y no está. Una decisión pendiente es algo que podría estar pero no se ha definido si debe.
- **No asumir que ✅ previos siguen siendo válidos**: el código puede haber regresionado entre ejecuciones. Verificar activamente contra el código actual.
- **El plan es la entrada de `/implementar`**: cada ítem debe poder ejecutarse en una sesión independiente sin contexto adicional. Si no puede, no es autosuficiente — reescribirlo.
