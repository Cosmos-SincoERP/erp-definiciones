# Implementar

Implementa la solicitud `$ARGUMENTS` razonando el **propósito** detrás del prompt, no su literalidad. La instrucción del usuario es una hipótesis de solución; el trabajo es verificarla contra los estándares del proyecto antes de ejecutar.

**Marco de referencia obligatorio:**
- `CLAUDE.md` del proyecto (principios, convenciones, restricciones).
- `MEMORY.md` y memorias relacionadas en `~/.claude/projects/[proyecto]/memory/`.
- DDD tactical (rich aggregates, Tell-Don't-Ask, Ley de Demeter, Fail Fast, SOLID).
- Connascence (bajar acoplamiento), CUPID (Composable, Unix philosophy, Predictable, Idiomatic, Domain-based), Fowler, GoF.
- Restricción crítica Marten + polimorfismo en eventos (ver CLAUDE.md).

---

## Anti-patrón a evitar (motivo de este comando)

Ejecutar la solicitud al pie de la letra sin contrastarla con los estándares produce código que pasa los tests pero introduce smells corregibles. Ejemplos:

- Usuario pide "validar dentro de `ProcesarPedido`". Implementación literal: añade el `throw` dentro del método. Mejor: separar `ValidarPedido` + `ProcesarPedido` (SRP, CUPID Predictable).
- Usuario pide "agregar un diccionario de entidades por ID". Implementación literal: `Dictionary<Guid, Entidad>`. Mejor: `List<Entidad>` + `FirstOrDefault` si N ≤ ~50, o predicado en el agregado si la clave es identidad del propio agregado (T-Identity Surrogate).
- Usuario pide "función estática que reciba el agregado y el ID de la entidad". Implementación literal: método estático con esos parámetros. Mejor: método de instancia del agregado si los parámetros son campos suyos.

**La regla operativa:** cuando la forma literal del prompt entra en conflicto con un estándar del proyecto, no ejecutar silenciosamente. Plantear la opción al usuario.

### Anti-anclaje en la firma existente

**Lecciones concretas — codificadas para que no se repitan:**

1. **La firma actual no es restricción.** Que el prompt asuma una firma `static` o con ciertos parámetros no convierte esa firma en parte del problema fijo — es parte de lo que se evalúa.
2. **`static` con todos los args del mismo agregado en write side es categórico.** No se pregunta y no se archiva — se aplica TDA o se dispara `AskUserQuestion` con la firma como decisión central.
3. **"Fuera de scope" / "próxima iteración" no es un cierre válido para un eje del checklist.** Si el eje detecta divergencia, se resuelve en este `/implementar` (aplicar idiomático, o preguntar al usuario). Archivar es el modo de fallo.
4. **Priorizar un eje y descartar otro sin base es prohibido.** Si Predictable (CUPID) y TDA detectan smells distintos en el mismo método, ambos se atacan o ambos se preguntan — nunca aplicar uno y silenciar el otro.

---

## Fases

### Fase 0 — Plan Mode obligatorio

Este comando **siempre opera en Plan Mode**. No editar archivos de producción hasta que el usuario apruebe vía `ExitPlanMode`. Único archivo editable durante la planificación: el archivo de plan asignado por el harness.

### Fase 1 — Leer prompt + cargar contexto

1. **Leer la solicitud completa.** Identificar:
   - **Objetivo de negocio**: qué problema resuelve, qué comportamiento observable debe existir al final.
   - **Forma propuesta**: qué cambios concretos sugiere el usuario (archivos, métodos, tipos).
   - **Hechos vs hipótesis**: el bug descrito y la regla de negocio son hechos; "modificar `X` agregando `Y`" suele ser hipótesis de solución.

2. **Si el prompt referencia un ítem de `AIResume/DiagnosticoYPlanDominio.md`:**
   Leer el ítem completo y usar sus secciones directamente:
   - **Explicación**, **Respaldo en la documentación** y **Ejemplo** — contexto del comportamiento. No es necesario releer toda la documentación del dominio.
   - **Test que define este comportamiento** — si está presente, es el test a escribir en Fase 1. No derivar qué testear: el test ya está especificado con su nombre, qué verifica, por qué falla en rojo y los casos borde obligatorios.
   - **Lo mínimo para que el test pase** — si está presente, define el alcance exacto de la implementación. No implementar nada que no esté en esta sección, aunque parezca útil o relacionado. Ese código pertenece a un ítem posterior.
   - **Activaciones pendientes (de ítems previos)** — si la sub-sección existe, **las tareas listadas forman parte del scope obligatorio** del ítem actual. Son trabajo dejado por ítems anteriores que solo puede completarse al implementar éste (típicamente tests `[Fact(Skip = "...")]`, helpers temporales o código marcado como `// TODO(#<N>)` que apunta a este ítem). Activarlas y completarlas como parte del cierre.

3. **Revisar deuda hacia atrás en el código** (paso obligatorio antes de planear):
   - Si el ítem es N: ejecutar `grep -r "TODO(#N)"` en el codebase. Cada marcador encontrado es deuda que se activa con este ítem.
   - Si el ítem implementa un nuevo evento/comando `XxxYyy`: ejecutar `grep -r "XxxYyy"` filtrando tests con `[Fact(Skip` para detectar tests que están esperando esa pieza.
   - Los hallazgos entran al scope del plan, incluso si no están listados en la sub-sección "Activaciones pendientes" del ítem (la sub-sección puede no estar actualizada).

4. **Cargar contexto del proyecto** (en orden):
   - `CLAUDE.md` del proyecto (siempre).
   - `MEMORY.md` y memorias relevantes al área tocada.
   - `AIResume/docs/revision-codigo/historial.md` si existe (para no re-introducir condiciones ya resueltas).

5. **Explorar el código en scope** (máx 3 agentes Explore en paralelo) si el área toca > 1 archivo. Identificar:
   - Convenciones locales del agregado / módulo afectado.
   - Helpers existentes que puedan reusarse (DRY proactivo).
   - Tests existentes que documenten el contrato actual.

### Fase 2 — Razonar el prompt contra los estándares

#### 2.0 Análisis de firma y call sites (paso previo obligatorio)

Antes de la checklist, para **cada método/función mencionada en el prompt** (sea existente o propuesta nueva):

1. **Mapear sus call sites actuales** (grep del nombre en el repo).
2. **Para cada parámetro**, anotar de dónde sale en cada call site: campo de un agregado (`agg.X`), variable local del handler, parámetro del comando, etc.
3. **Detectar el patrón "static con args del mismo agregado":**
   - Si el método es `static` y **todos** los argumentos de un grupo provienen del mismo agregado en **todos** los call sites, la firma misma es un Tell-Don't-Ask — no solo el cuerpo.
   - **Categórico en write side**: aplicar la conversión a método de instancia o disparar `AskUserQuestion` con la firma como decisión central. Nunca archivar.
4. **Anclaje prohibido:** que el prompt presuponga la firma actual no la convierte en restricción. La firma es parte de lo que se evalúa.

#### 2.1 Checklist de divergencias

Para cada eje, registrar el resultado con uno de estos cuatro estados — **ningún otro estado válido**:

- ✅ **No diverge** — la forma literal cumple el estándar.
- 🚫 **Categórico, aplicar sin preguntar** — Apply impuro, capa rota, DU en evento, static con args del mismo agregado en write side.
- 🔧 **Idiomático aplicable** — corrección clara sin tradeoff: aplicar directamente.
- ❓ **Tradeoff genuino** — disparar `AskUserQuestion` en Fase 3.

**Estados prohibidos:** "fuera de scope", "próxima iteración", "refactor mayor". Si un eje detecta divergencia, se resuelve aquí — nunca se archiva.

| Eje del estándar | Pregunta a responder | Si diverge → |
|---|---|---|
| **DRY** | ¿Duplicaría lógica que ya existe en el agregado / VO? | proponer reuso del helper existente |
| **SRP** | ¿Mezcla dos responsabilidades en el mismo método (validación + cálculo, IO + lógica)? | proponer separación |
| **Tell-Don't-Ask (firma)** | ¿Todos los args de un grupo vienen del mismo agregado en los call sites? (ver 2.0) | método de instancia — **🚫 categórico en write side** |
| **Tell-Don't-Ask (cuerpo)** | ¿Lee propiedades del agregado para decidir en lugar de pedirle que decida? | mover decisión al agregado |
| **Ley de Demeter** | ¿El plan hace que el llamador atraviese ≥ 2 niveles para tomar decisiones? | proponer encapsular |
| **Fail Fast** | ¿La validación está al borde correcto? ¿Hay null guards faltantes? | corregir borde |
| **Predictable (CUPID)** | ¿El nombre propuesto describe todo lo que el método hace (incluido lanzar excepciones)? | renombrar o separar |
| **CQS** | ¿El método retorna un valor Y muta estado simultáneamente? Un método es comando (retorna void, muta) o query (retorna valor, no muta). | separar en command + query |
| **Inmutabilidad** | ¿Se expone colección mutable (`List<T>` pública) o setter público en el write side? | `IReadOnlyList<T>` en lugar de `List<T>` pública |
| **Apply hygiene (Marten)** | Si el cambio toca un `Apply`, ¿introduce `throw`, IO, o fuente no determinista? | **🚫 categórico** — mover validación al método de negocio |
| **Pureza de capa de dominio** | ¿El cambio agrega un `using` prohibido en proyecto de dominio? | **🚫 categórico** — abstraer detrás de port |
| **DU en eventos** | Si se agrega un campo a un evento, ¿es tipo abstracto o jerarquía polimórfica? | **🚫 categórico** — primitivos / enum, DU solo en la entidad de dominio |
| **Volumetría** | Si se introduce `Dictionary`/`HashSet`, ¿está documentada N > ~50? | preferir `List` + predicado salvo justificación explícita |
| **Naming** | ¿Hay sinónimos coexistentes con clases existentes del scope? | unificar al término vigente |
| **Memorias del usuario** | ¿Alguna memoria específica aplica al área tocada? | aplicar antes de proponer |

**Regla clave:** los catorce ejes se recorren cada vez, **explícitamente** y por escrito en el plan (sub-sección "Análisis de divergencias", ver Fase 4). Saltar la checklist mentalmente o resolverla en notas está prohibido — el registro escrito es lo que evita archivar ejes silenciosamente.

### Fase 3 — `AskUserQuestion` cuando hay divergencia

Si la Fase 2 detecta ≥ 1 divergencia entre la forma literal y la idiomática, **antes de escribir el plan final** lanzar `AskUserQuestion`. Estructurar las opciones:

- **Opción A — Forma literal del prompt**: descripción concreta + tradeoff conocido.
- **Opción B — Forma idiomática del proyecto** (recomendada cuando hay divergencia clara): descripción concreta + por qué cumple el estándar.
- **Opción C — Híbrida**: si existe un punto medio razonable.

**Cuándo NO preguntar (ejecutar la forma idiomática directamente):**
- La divergencia es trivial (renombrar un parámetro local, ordenar miembros).
- El estándar es categórico (ej. `throw` dentro de `Apply` — corregir y avisar, no preguntar).
- El usuario dejó instrucción durable previa (memoria, CLAUDE.md) que ya resuelve el caso.

**Cuándo SÍ preguntar siempre:**
- El cambio idiomático requiere mover código a otra clase / cambiar firma pública.
- Hay tradeoff genuino (introducir VO nuevo vs mantener primitivos; método de instancia vs estático cuando ambos son defendibles).
- El alcance que el usuario quiere atacar es ambiguo.

**Una sola ronda de `AskUserQuestion` cubre múltiples decisiones (hasta 4 preguntas).** No fragmentar en varias rondas si las decisiones son independientes.

### Fase 4 — Escribir el plan

Estructura mínima del plan (en el archivo asignado por el harness):

```markdown
## Contexto
[Por qué este cambio. Problema concreto, regla de negocio que falta, bug observable.
Si viene de DiagnosticoYPlanDominio.md, referenciar el ítem y su número.]

## Interpretación del prompt
[1-3 líneas: qué pidió el usuario literalmente vs qué resuelve el problema.
Si el plan se aparta de la literalidad, explicitar por qué y referenciar el estándar.]

## Análisis de divergencias (obligatorio)

### Call sites de los métodos tocados
[Por cada método del prompt, listar call sites y origen de cada arg.
Marcar si todos los args de un grupo vienen del mismo agregado.]

### Checklist (los 14 ejes, sin omisiones)
| Eje | Estado | Nota / acción |
|---|---|---|
| DRY | ✅ / 🚫 / 🔧 / ❓ | … |
| SRP | | |
| TDA-firma | | |
| TDA-cuerpo | | |
| Demeter | | |
| Fail Fast | | |
| Predictable | | |
| CQS | | |
| Inmutabilidad | | |
| Apply hygiene | | |
| Pureza dominio | | |
| DU eventos | | |
| Volumetría | | |
| Naming | | |
| Memorias | | |

**Restricción:** ningún eje puede quedar como "fuera de scope" o "próxima iteración".
Si un eje detecta divergencia, debe resolverse aquí — nunca archivarse.

## Cambios
[Enumerados, con archivos y líneas. Cada cambio referencia el estándar que lo justifica si no es obvio.]

## Archivos a crear / modificar
[Lista de paths.]

## Verificación
[Comandos de test exactos.]
```

**No incluir** alternativas descartadas en el plan final. El plan es la decisión aprobada, no el debate.

**El "Análisis de divergencias" es la salvaguarda principal contra anclar en la firma existente y archivar ejes silenciosamente.** Si un eje aparece en blanco o con texto vago, el plan está incompleto y debe revisarse antes de `ExitPlanMode`.

### Fase 5 — `ExitPlanMode`

Llamar `ExitPlanMode` para solicitar aprobación. No usar `AskUserQuestion` para preguntar "¿está bien el plan?" — ese es exactamente el rol de `ExitPlanMode`.

### Fase 6 — Implementación

Tras aprobación:

1. **TDD obligatorio.** Para cambios de dominio: tests en rojo primero, luego implementación. Verificar con `dotnet test --filter FullyQualifiedName~[NombreDelTest]` que el test está en rojo antes de implementar.

2. **Gate de calidad sobre el código escrito** — no solo sobre el plan. Al terminar cada bloque de código nuevo, recorrer cada tabla. Si alguna fila falla, corregir antes de continuar.

   **Correctitud y contratos:**

   | Heurística | Control |
   |---|---|
   | **Fail Fast** | Guards al inicio, no al final. Sin `catch` que swallow sin rethrow. Sin `?? default` que oculta un error de negocio. |
   | **Null semantic** | Si `T?` es retorno, ¿null es "no aplica" legítimo o error encubierto? Si error → `OLanzar`. Sin `FirstOrDefault()` sin null-check posterior. |
   | **CQS** | ¿Algún método retorna valor Y muta estado simultáneamente? Separar en command + query. |
   | **Inmutabilidad** | ¿Se expone `List<T>` pública o setter público en write side? Usar `IReadOnlyList<T>`. |
   | **Dead code** | Sin imports, helpers privados, comentarios `// removed` o parámetros sin uso. |

   **Acoplamiento:**

   | Heurística | Control |
   |---|---|
   | **Tell, Don't Ask** | ¿El código extrae estado de un objeto para decidir por él? Mover la lógica al objeto. |
   | **Ley de Demeter** | ¿Cadenas `a.B.C.Method()` nuevas (fuera de LINQ/fluent)? Introducir método en el objeto navegado. |
   | **Feature Envy** | ¿Método nuevo usa 3+ miembros de otra clase y 0–1 de la propia? El método está en el lugar equivocado. |
   | **Connascence** | ¿El acoplamiento introducido es Position / Meaning / Identity (fuerte)? Evaluar si elevarlo a Name / Type. |
   | **DRY** | ¿La lógica nueva duplica algo ya existente? Grep antes de asumir. |

   **Cognitive load:**

   | Heurística | Control |
   |---|---|
   | **Tamaño** | Método público en agregado > 15 líneas. Método privado > 20. Clase > 200. Extraer. |
   | **Parámetros** | > 4 params no encapsulados → record con nombre de dominio. |
   | **Ciclomática** | `if`/`for`/`case`/`&&`/`||`/`?:` > 10 → extraer. |
   | **Condicional** | Anidado > 2 niveles → guard clauses. Boolean flag → dos métodos. |
   | **Primitive obsession** | `string`/`decimal`/`bool` con validaciones repetidas → VO. |

   **Dominio DDD:**

   | Heurística | Control |
   |---|---|
   | **Agregado enriquecido** | Si se agregó un método de negocio: ¿el agregado expone el método en lugar de que el servicio lea propiedades? |
   | **Vocabulario** | ¿Clases/métodos nuevos tienen nombres de dominio? Sin `Resolver*`, `Procesador*`, `Manager*`, `Handler*`, `Helper*`. |
   | **Invariante explícita** | Las reglas nuevas, ¿están enforced en property initializer del VO o en método del agregado? |
   | **Estado propio usado** | El método nuevo, ¿accede a al menos un `this.Xxx`? Si solo opera sobre parámetros → función estática disfrazada. |

   **Rastreos estructurales:**

   | Heurística | Control |
   |---|---|
   | **Apply hygiene** | Si se tocó un `Apply`: libre de `throw`, IO, `await`, logging, fuentes no deterministas (`DateTime.Now`, `Guid.NewGuid`). |
   | **Layer purity** | Sin `using` prohibido en `*.Dominio` (web/HTTP, ORM, serialización, mensajería, capas superiores). |
   | **Identity Surrogate** | Si se introduce VO/record nuevo: ¿tiene huella en eventos/comandos/queries/proyecciones/puertos? Si no, verificar que no todos los call sites lo construyen con campos del mismo agregado. |
   | **Synonym drift** | Si se nombra clase/interfaz/record nuevo: ¿coexiste su raíz con otra forma morfológica en el scope? Alinearse con la forma vigente. |
   | **Lookup sobredimensionado** | Si se introduce `Dictionary<K,V>`: ¿volumetría > ~50 documentada? Si no, `IReadOnlyList<V>` + predicado. |
   | **Validación cross-layer** | Si se introduce validación nueva: ¿no queda duplicada en otra capa? Centralizar en VO o agregado. |

   **Whack-a-mole check:** grep por el smell evitado en el resto del scope — confirmar que no se reprodujo en otro archivo cercano.

   **Test relevance check:** por cada bloque de código nuevo, ¿hay al menos un test que lo cubre? Si una rama no está cubierta, agregar el test antes de continuar.

   **Gate estructural de tests** (recorrer cada test recién escrito, antes de ejecutar la suite — NO confiar en que las reglas de CLAUDE.md se respeten automáticamente, verificarlas test por test):

   | Heurística | Control |
   |---|---|
   | **Then requiere And (estado del agregado)** | Cada test que NO espera excepción debe verificar el estado del SUT tras la acción, no solo el evento emitido. En este proyecto: `Then(...)` siempre seguido de `And<Agregado, T>(agg => agg.X, esperado)`. Tests de excepción están exentos. Regla en CLAUDE.md sección "Testing Pattern". |
   | **Excepción correcta y específica** | Tests de error: `.ThrowAsync<TipoEspecífico>()` + `.Where(ex => ex.Type == DomainExceptionType.X)` + `.WithMessage(...)`. No `Exception` genérica; no solo el tipo sin matcher de mensaje. |
   | **Sin `[Theory]` con un solo `[InlineData]`** | Convertir a `[Fact]` con el valor hardcodeado. |
   | **Sin tests triviales** | No probar primary constructors, herencia o literales hardcoded por sí solos (ver memoria `feedback_no_tests_triviales` si existe). El test vale por el comportamiento observable. |
   | **Sin asserts de colección por índice/Count** | `.Count.Should().Be(N)` y `lista[0].Should()...` están prohibidos. Usar `BeEquivalentTo` o helpers tipados con la lista esperada completa. |
   | **Mensajes de excepción con wildcards, no exactos** | `.WithMessage("*'{id}'*regla*")` en vez de strings completos — evita over-specify. |
   | **Naming con sujeto explícito** | Si dos entidades del scope pueden ser sujeto del `Si_X` o `Debe_Y`, agregar el nombre de la entidad (`EstadoDelTercero…`, `ContactoTieneSoloCorreo…`). Regla en memoria `feedback_test_naming_sujeto_explicito` si existe. |
   | **Tests con Skip llevan marcador TODO(#N)** | El skip debe incluir `TODO(#<N>)` apuntando al ítem que lo desbloquea, y debe haber un bullet en la sub-sección "Activaciones pendientes" del ítem N en el plan refinado. |

   Si alguna fila falla en algún test, corregir antes de ejecutar la suite. CLAUDE.md describe estas reglas pero **el comando es responsable de verificarlas test por test** — no asumir que se cumplieron por inercia.

3. **No introducir cambios fuera del plan aprobado.** Si durante la implementación se detecta un smell adyacente:
   - Si es trivial y está en el mismo archivo: corregirlo y mencionarlo en el resumen.
   - Si requiere cambio de API o de otro archivo: detenerse y proponerlo al usuario.

### Fase 7 — Verificación y cierre

```bash
# Build primero — si falla, no continuar
dotnet build [Proyecto].sln 2>&1 | tail -5

# Suite completa — verificar que ningún proyecto reporta Failed: > 0
dotnet test [Proyecto].sln --no-build 2>&1 | grep -E "Passed!|Failed!"
```

Si hay fallos: investigar y corregir antes de declarar el cierre. No reportar éxito con tests rojos.

### Captura de deuda hacia adelante (paso obligatorio antes de cerrar)

Durante la implementación pudiste detectar que una regla, test o pieza de código del ítem **A** que estás implementando solo puede completarse cuando se implemente un ítem **B** futuro (típicamente: una regla del ítem A menciona un estado/comportamiento que aún no existe porque vive en B). En ese caso, registra la deuda en **dos lugares** antes de cerrar:

1. **En el código**: marcador `// TODO(#<N>): <descripción concreta>` donde `N` es el número del ítem que desbloqueará la deuda. Para tests, usar `[Fact(Skip = "TODO(#<N>): <descripción>")]`. El marcador debe ser **corto** — los detalles van en el plan.
2. **En el plan**: en el ítem **B** de `AIResume/DiagnosticoYPlanDominio.md`, agregar una sub-sección **"Activaciones pendientes (de ítems previos)"** con un bullet por cada deuda: ubicación del marcador (`archivo:nombre_test` o `archivo:linea`), qué reemplazar, y referencia al ítem origen. Si la sub-sección ya existe, agregar el bullet allí.

Esta captura es **obligatoria**: la sub-sección del ítem B se lee como scope obligatorio cuando ese ítem se implemente (ver Fase 1.2), cerrando el círculo.

**Resumen de cierre:**
- Listar archivos tocados.
- Confirmar tests verdes con números: `"N dominio + M acceptance"`.
- Mencionar cualquier estándar aplicado proactivamente más allá del literal del prompt.
- Si quedaron smells adyacentes detectados pero fuera de scope: listarlos como observaciones sin tocar.
- Si quedó **deuda hacia adelante** capturada (TODO(#N) + Activaciones pendientes en el ítem N): mencionarla en el resumen para que el usuario la vea explícitamente.

---

## Garantía operativa

- El propósito del prompt **siempre** se cumple. Apartarse de la literalidad nunca implica ignorar el bug, regla o feature solicitada.
- El usuario **mantiene la decisión final** vía `AskUserQuestion` cuando hay divergencia significativa.
- El código resultante **cumple los estándares del proyecto** (CLAUDE.md + memorias) por construcción, no por revisión posterior.

Si en cualquier fase aparece una contradicción entre el prompt y un estándar **categórico** (Apply impuro, polimorfismo en eventos, dependencia de capa rota), aplicar el estándar y avisar al usuario en el plan — no ejecutar el prompt literal.
