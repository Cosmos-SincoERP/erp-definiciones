# Implementar Hallazgo

Implementa el hallazgo `$ARGUMENTS` de `AIResume/RevisionCodigo.md`.

**Marco de referencia:** toda implementación se guía por los principios del proyecto (CLAUDE.md) + SOLID + DDD tactical (rich aggregates, domain services delgados) + Connascence (bajar acoplamiento) + CUPID (composable, predictable, idiomatic) + Fowler refactoring catalog + GoF.

---

## Restricción crítica de arquitectura — Marten + polimorfismo en eventos

**PROHIBIDO:** colocar tipos abstractos, interfaces o jerarquías polimórficas como **campos** dentro de records de evento.

Los eventos de Marten son historia inmutable en JSONB. Un campo abstracto embebe un discriminador `$type` que queda bakeado en streams históricos, no puede rescatarse con `EventUpcaster`, falla silenciosamente con JSONB si `AllowOutOfOrderMetadataProperties` no está configurado, y acopla nombres de clases C# a historia inmutable.

**Regla:** los campos de un evento deben ser primitivos (`Guid`, `string`, `decimal`, `DateTime`, `bool`), enums, o value objects **planos** sin herencia.

**Patrón correcto cuando el dominio necesita un DU:** implementarlo en la entidad/VO. El `Apply()` convierte los campos planos del evento al tipo rico:

```csharp
// ✅ CORRECTO — evento plano; Apply() convierte
public record EstadoDefinido(EstadoEntidad Estado, string? Configuracion) : EntidadEvents;
_estado = EstadoEntidadDU.Desde(evento.Estado, evento.Configuracion);

// ❌ PROHIBIDO — tipo abstracto como campo de evento
public record EstadoDefinido(IEstadoEntidad Estado) : EntidadEvents;
```

Si la propuesta implica colocar un tipo polimórfico dentro de un evento, **detener y consultar** antes de implementar.

---

## Paso 0 — Pre-flight check

1. Lee `AIResume/RevisionCodigo.md` completo.
2. Localizar el hallazgo `$ARGUMENTS`. Si no existe o el argumento está vacío, usar `AskUserQuestion`.
3. Si el hallazgo está marcado **✅ RESUELTO**, notificar y detener.
4. **Verificar vigencia:** leer las líneas citadas y confirmar que el problema aún existe. Si el código cambió y ya no aplica, marcar ✅ RESUELTO (cambios previos) y detener.
5. **Detectar dependencias:** verificar si hay hallazgos relacionados en el mismo archivo o categoría adyacente. Si `$ARGUMENTS` podría resolver otros como side-effect, anotarlos para el cierre.
6. **Detectar hallazgos bloqueantes:** si otro hallazgo del mismo componente está en curso (existe `AIResume/hallazgo_*.md`) o si `$ARGUMENTS` depende conceptualmente de otro no resuelto, confirmar con `AskUserQuestion`.
7. Determinar el tipo de hallazgo:
   - **Tier 1 correctitud (A, B, C, D, I, N, Q, R)** — bugs: **TDD** (test en rojo → fix).
   - **Tier 2 diseño (E, F, G, H, J, K, L, M, O)** — refactors: verificar cobertura → refactor → verde.
     - **F, G**: pueden requerir decisión de diseño previa.
     - **L**: si introduce VO nuevo con impacto en callers externos → `AskUserQuestion`.
     - **O (Connascence)**: el refactor debe bajar el tipo de connascence.
   - **Tier 3 dominio/patrón:**
     - **T (Anemia)** — mover lógica al agregado. TDD mixto: test unitario del método nuevo en el agregado + tests del servicio como red de seguridad.
     - **S (Patrón)** — confirmar con `AskUserQuestion` qué patrón aplicar si hay múltiples opciones.
8. Si la categoría implica cambios estructurales no triviales, usar `AskUserQuestion` para confirmar el enfoque.

9. **Leer historial de revisiones:** `AIResume/docs/revision-codigo/historial.md`.
   - Si no existe, omitir este punto.
   - Si existe:

     a. **Detección de regresión:** comparar la condición de `$ARGUMENTS` contra cada `RC-NNN`. Si coincide en archivo + categoría + forma del problema → marcar como **regresión de RC-NNN** y subir severidad **+1** (Minor → Major; Major → Critical; Critical clamped).

     b. **Verificación contra decisiones vigentes:** si la solución planeada coincide con una alternativa marcada como descartada en alguna `DC-NNN` → detener y consultar con `AskUserQuestion`.

     c. **"Nueva evidencia objetiva"** (único caso en que se puede reabrir una DC-NNN): cambio de requisitos, cambio de volumen documentado, mediciones reproducibles, bug atribuible, cambio arquitectónico, nueva documentación oficial de proveedor.

     d. Si ninguna aplica → alinear con la decisión vigente antes de entrar en plan mode.

     e. El historial es contexto no autoritativo. Verificar contra el código actual antes de aplicarlo.

### Crear archivo de tracking

`AIResume/hallazgo_$ARGUMENTS_[NombreDescriptivo].md`:

```markdown
> ⚠️ ELIMINAR este archivo cuando todos los checkboxes estén marcados.

# Hallazgo $ARGUMENTS — [Título]

**Categoría:** [letra y nombre]
**Tier:** [1 correctitud / 2 diseño / 3 dominio-patrón]
**Severidad:** [🔴 Critical / 🟡 Major / 🔵 Minor]
**Archivo(s) afectado(s):** [lista]
**Rama:** [git branch --show-current]
**Iniciado:** [fecha]
**Hallazgos relacionados (side-effects posibles):** [IDs si los hay]

## Plan de resolución

### Fase 1 — [Tests en rojo / Verificar cobertura existente]
- [ ] [item 1]

### Fase 2 — Implementación
- [ ] [cambio 1]

### Fase 3 — Verificación y gate de calidad
- [ ] Suite completa verde
- [ ] Gate de heurísticas pasado
- [ ] Whack-a-mole check (smell no reapareció en otros lugares)
- [ ] Tests del código NUEVO existen
- [ ] Hallazgo marcado como resuelto
```

---

## Paso 1 — Entrar en plan mode

Usar `EnterPlanMode` y presentar:
- Archivos a crear/modificar.
- Enfoque (TDD / refactor / movimiento a agregado / patrón).
- Hallazgos relacionados que podrían resolverse.
- Riesgos: cambios de contrato público, tests que pueden requerir actualización, connascence entre módulos.

**Para hallazgos T (anemia)** el plan debe nombrar explícitamente:
- Qué método **nuevo** tendrá el agregado (firma y qué campos propios usa).
- Qué invariante protege.
- Qué tests unitarios del agregado lo cubrirán.
- Qué tests del servicio simplifican (el servicio ahora delega).
- Qué callers de la propiedad/getter removido deben actualizarse.

**Check obligatorio antes de planificar un T:** el método propuesto debe usar **al menos un campo propio** (`this.Xxx`). Si el cuerpo opera solo sobre parámetros → falso positivo. Marcar ✅ RESUELTO (falso positivo) y detener.

Esperar confirmación.

---

## Paso 2 — Fase 1: Tests o verificación de cobertura

### Para bugs (Tier 1: A, B, C, D, I, N, Q, R)

1. El test rojo debe probar el bug, no solo fallar al compilar. El mensaje de falla debe describir expected vs actual.
2. Naming per CLAUDE.md: `Si_X_Debe_Y`, PascalCase. X = estado exacto; Y = outcome observable.
3. **Cobertura del rojo:**
   - Caso que activa el bug.
   - Valor exacto del límite (off-by-one).
   - Caso adyacente que NO activa el bug.
   - `TestContext.Current.CancellationToken` en async.
   - `Then()` siempre con `And<>()`.
4. Ejecutar `dotnet test --filter FullyQualifiedName~[Nombre]` — verificar **rojo**.
5. Marcar checkbox.

### Para refactors (Tier 2: E, F, G, H, J, K, L, M, O)

1. Ejecutar tests existentes: `dotnet test --filter FullyQualifiedName~[ComponenteAfectado]`.
2. Confirmar **verde**. Si falla alguno → no es refactor, es bug oculto; reportar.
3. Documentar qué tests protegen el comportamiento.
4. Marcar checkbox.

### Para anemia de dominio (Tier 3: T)

1. Identificar el método nuevo del agregado. Verificar que usa al menos un `this.Xxx`.
2. Escribir test unitario del agregado en **rojo** para el método nuevo.
3. Verificar rojo.
4. Ejecutar tests existentes del servicio (serán red de seguridad al mover la lógica). Confirmar **verde**.
5. Documentar: método a crear, tests del servicio que simplificarán, callers a actualizar (grep).

### Para patrones (Tier 3: S)

1. Identificar si el patrón cambia contratos públicos.
2. Si cambia: tests de integración antes/después.
3. Si no: verificar cobertura existente.

---

## Paso 3 — Fase 2: Implementación

Aplicar corrección mínima sin expandir scope.

**Convenciones (CLAUDE.md):**
- DRY check antes de escribir lógica nueva.
- Naming: `ObtenerXxxOLanzar` (retorna no-null), `LanzarExcepcionSiXxx` (void).
- Sin comentarios salvo WHY no obvio.
- Sin dead code — eliminar usos huérfanos.

**Para hallazgos T — orden de operaciones:**
1. Agregar método al agregado/VO.
2. Hacer pasar el test unitario del agregado (rojo → verde).
3. Migrar call-sites del servicio al método nuevo.
4. Considerar si la propiedad original debe quitarse. Si solo el servicio la usaba: quitarla.
5. Ejecutar tests del servicio — deben pasar sin cambios semánticos.
6. Si el servicio tenía tests específicos de la regla movida: evaluar si borrarlos (duplicación con el test del agregado) o mantenerlos como contrato end-to-end.

**Ejecutar tests del componente tras cada cambio significativo:**
```bash
dotnet test --filter FullyQualifiedName~[ComponenteAfectado]
```

**Escalation triggers — detener y consultar si:**
- Test inesperado rojo.
- Fix requiere tocar > 5 archivos fuera del scope del hallazgo. (Excepción válida para T: hasta 3 callers legítimos; si pasa de 5 → stop.)
- Cambio de contrato público no anticipado.
- Build warnings nuevos (`CS0618`, `CS8618`).
- Más ocurrencias del mismo problema descubiertas.
- Para T: mover la lógica requiere que el agregado exponga algo que no debería → probablemente el movimiento es a un domain service, no al agregado.

### Gate de heurísticas — obligatorio antes de Fase 3

Recorrer cada bloque de código nuevo o modificado. Si alguna heurística falla, corregir antes de continuar.

**Correctitud y contratos (Tier 1):**

| Heurística | Control |
|---|---|
| **Fail Fast** | ¿Guards al inicio? ¿`catch` que swallow sin rethrow? ¿`?? defaultValue` que oculta error? |
| **Null semantic** | Si `T?` es retorno, ¿null es "no aplica" legítimo o error encubierto? Si error → `OLanzar`. |
| **CQS** | ¿Algún método retorna valor Y muta estado? Separar en command + query. |
| **Inmutabilidad** | ¿Se expone colección mutable o setter público en write side? Usar `IReadOnlyList<T>`. |
| **Dead code** | ¿Imports, helpers, comentarios `// removed`, o miembros sin uso tras el cambio? |

**Acoplamiento (Tier 2):**

| Heurística | Control |
|---|---|
| **Connascence** | ¿El cambio bajó o elevó el tipo? Position→Name es mejora; Identity nueva es regresión. |
| **Tell, Don't Ask** | ¿El código extrae estado para decidir por el objeto? Mover la lógica al objeto. |
| **Ley de Demeter** | ¿Cadenas `a.B.C.Method()` nuevas (fuera de LINQ/fluent)? |
| **Feature Envy** | ¿Método nuevo usa 3+ miembros de otra clase y 0–1 de la propia? |
| **DRY** | ¿Duplica algo existente? Grep antes de asumir. |

**Cognitive load (Tier 2):**

| Heurística | Control |
|---|---|
| **Tamaño** | Método público en agregado > 15 líneas. Método privado > 20. Clase > 200. Extraer. |
| **Parámetros** | > 4 params no encapsulados → record con nombre de dominio. |
| **Ciclomática** | `if`/`for`/`case`/`&&`/`||`/`?:` > 10 → extraer. |
| **Condicional** | Anidado > 2 niveles → guard clauses. Boolean flag → dos métodos. |
| **Primitive obsession** | `string`/`decimal`/`bool` con validaciones repetidas → VO. |

**Dominio (Tier 3 — T):**

| Heurística | Control |
|---|---|
| **Agregado enriquecido** | ¿El ratio R:M mejoró? Listar métodos nuevos + propiedades encapsuladas. |
| **Vocabulario** | ¿Nombres de dominio (no `Resolver*`, `Procesador*`, `Manager*`, `Handler*`, `Helper*`)? |
| **Invariante explícita** | La regla movida al agregado, ¿queda enforced en property initializer o método público? |
| **Cohesión del servicio** | Post-movimiento, ¿el servicio es más delgado o solo delegó la llamada sin reducirse? |
| **Estado propio usado** | El método nuevo, ¿accede a al menos un `this.Xxx`? Si solo parámetros → función estática disfrazada. Revertir y marcar como falso positivo. |

**Rastreos estructurales:**

| Heurística | Control |
|---|---|
| **Apply hygiene** | Si se toca un `Apply(*Event)`: libre de `throw`, helpers que lanzan, IO, `await`, logging, fuentes no deterministas (`DateTime.Now/UtcNow`, `Guid.NewGuid`, `Guid.CreateVersion7`, `Random`), estado de runtime. Las fuentes no deterministas van en el método de negocio y viajan dentro del evento. |
| **Layer purity** | Si se toca `*.Dominio`: sin `using` prohibido — web/HTTP, ORM concreto, serialización, mensajería, DI container, logging concreto en agregados/VOs, capas superiores, read side desde write side. |
| **Identity Surrogate** | Si se introduce VO/record nuevo: ¿tiene huella en eventos/comandos/queries/proyecciones/puertos? Si no y todos los call sites lo construyen con campos del mismo agregado → no introducir; el agregado expone un predicado. |
| **Synonym drift** | Si se nombra clase/interfaz/record nuevo: ¿coexiste su raíz lingüística con otra forma en el scope? Alinearse con la forma vigente. |
| **Estructura de lookup** | Si se introduce `Dictionary<K,V>`: ¿volumetría > ~50 documentada? Si no, `IReadOnlyList<V>` + predicado. |
| **Validación cross-layer** | Si se introduce validación nueva: ¿no queda duplicada en otra capa? Centralizar en VO o agregado. |

**CUPID (cualitativo):**

| Propiedad | Pregunta |
|---|---|
| **Composable** | ¿Se combina sin arrastrar dependencias pesadas? |
| **Predictable** | ¿La firma corresponde al comportamiento? |
| **Idiomatic** | ¿Estilo del lenguaje y del proyecto? |
| **Domain-based** | ¿Nombres de dominio, no genéricos? |

**Whack-a-mole check:** grep por el smell/patrón original en otros lugares del scope. Para T: grep por la propiedad/getter removido — todos los callers deben estar migrados.

**Test relevance check:**
- Para bugs: el test rojo ahora pasa.
- Para refactors: al menos 1 test existente cubre el código nuevo.
- Para T: el agregado tiene test unitario del método nuevo; los tests del servicio siguen pasando sin cambios semánticos.

**Cross-finding detection:** si al resolver `$ARGUMENTS` se resolvió otro hallazgo como side-effect, anotarlo en el tracking para marcarlo en el cierre.

---

## Paso 4 — Fase 3: Verificación

### Build

```bash
dotnet build [Proyecto].sln 2>&1 | tail -5
```

0 errores. Warnings nuevos → investigar.

### Suite completa

```bash
dotnet test [Proyecto].sln --no-build 2>&1 | tee /tmp/cosmos-hallazgo-results.txt
echo ""
echo "=== RESUMEN POR PROYECTO ==="
grep -E "Passed!|Failed!" /tmp/cosmos-hallazgo-results.txt
```

Todos los proyectos con `Failed: 0`. Regresiones → corregir.

### Métricas del cambio

Según la categoría, documentar en el tracking:

- **M / K:** `wc -l [archivo]` antes y después. Confirmar que bajó.
- **O (Connascence):** "Position → Name", "Meaning → Type", etc.
- **T (Anemia):** ratio R:M del agregado antes y después (ej. "3:0 → 1:2"). Método(s) nuevo(s). Lógica eliminada del servicio (líneas removidas).
- **S (Patrón):** patrón introducido, antipatrón eliminado.

---

## Paso 5 — Cierre

1. Verificar todos los checkboxes `[x]`.
2. Eliminar `AIResume/hallazgo_$ARGUMENTS_*.md`.
3. En `AIResume/RevisionCodigo.md`:
   - Marcar `$ARGUMENTS` como **✅ RESUELTO (fecha)**.
   - Side-effects → marcar otros IDs **✅ RESUELTO (fecha, side-effect de $ARGUMENTS)**.
   - Si surgió decisión de diseño no obvia, agregar nota con enfoque y razón.

4. **Actualizar historial** en `AIResume/docs/revision-codigo/historial.md` — paso **obligatorio**.

   **4.1. Si el archivo no existe**, crearlo:

   ```
   # Historial De Revision De Codigo

   Este archivo es memoria factual para revisiones futuras. No certifica que el codigo actual este correcto.
   Cada entrada se agrega exclusivamente desde /implementar-hallazgo al cierre. No editar a mano.

   ---

   ## Hallazgos Resueltos

   ## Decisiones Tomadas
   ```

   **4.2. Agregar entrada en "Hallazgos Resueltos":**

   Numeración: leer entradas `### RC-(\d+):` existentes, tomar `max + 1` con padding a 3 dígitos. Si no hay entradas previas, empezar en `RC-001`.

   ```
   ### RC-NNN: [Titulo corto, ≤ 60 chars]
   Estado: resuelto
   Fecha: [YYYY-MM-DD]
   Archivos:
   - [ruta1]

   Condicion Original:
   [1-3 lineas describiendo el problema en presente histórico, sin referencias a IDs internos]

   Solucion:
   [1-3 lineas describiendo el fix aplicado, en pasado]

   Verificacion:
   - [dotnet test --filter FullyQualifiedName~[ClaseDeTest]]

   Instruccion Para Revisores:
   No reportar este hallazgo si [condicion verificable que el código actual ya cumple]. Si vuelve, reportar como regresion.
   ```

   **Para regresiones:** no crear nuevo RC-NNN — actualizar el RC-NNN original con `Estado: regresion-resuelta` y una sub-nota fechada.

   **4.3. Si se descartaron alternativas**, agregar en "Decisiones Tomadas":

   Numeración: análoga a `RC-NNN`, tomar `DC-NNN = max + 1`.

   ```
   ### DC-NNN: [Titulo corto, ≤ 60 chars]
   Estado: decidida
   Fecha: [YYYY-MM-DD]

   Decision:
   [Resumen de la eleccion final]

   Alternativas Descartadas:
   - [alternativa 1]
   - [alternativa 2]

   Razon:
   [Por que se eligio esta opcion — evidencia objetiva: volumetria, restriccion de plataforma, dominio]

   Condiciones Para Reabrir:
   - [condicion objetiva verificable]

   Instruccion Para Revisores:
   No volver a proponer [alternativas] salvo que exista condicion objetiva para reabrir.
   ```

   **4.4. Reglas de calidad obligatorias:**
   - Entradas inmutables tras agregarse (excepción: actualizar RC-NNN original ante regresión).
   - Textos en español sin tildes.
   - "Condicion Original" e "Instruccion Para Revisores" en términos verificables por inspección del código.
   - "Razon" cita evidencia objetiva — nunca opiniones.

5. **Actualizar memoria si aplica:**
   - Decisión de diseño nueva → feedback memory + MEMORY.md.
   - Convención no documentada aplicada → mismo flujo.

6. Reportar al usuario:
   - Archivos modificados.
   - Tests creados/actualizados.
   - Hallazgo cerrado (+ side-effects).
   - **Para T:** método nuevo del agregado + métricas R:M antes/después.
   - Entradas agregadas al historial: `RC-NNN` y, si aplicó, `DC-NNN`.
   - Memoria guardada si aplicó.

### Estado final del reporte

Leer `AIResume/RevisionCodigo.md` y determinar:

- **Si quedan hallazgos no resueltos:** presentar lista (ID + severidad + título). Sugerir siguiente por prioridad:
  1. 🔴 Critical primero.
  2. Mismo tier/severidad: bugs (A, B, C, Q, R) > contratos (I, N) > T Major (anemia) > diseño > tamaño (M) > resto.
  3. T Major tiene prioridad sobre diseño Minor — es deuda estructural del dominio.
- **Si todos resueltos:** notificar cierre completo. Sugerir `/revision-codigo` nuevo ciclo para detectar hallazgos emergentes tras los cambios (especialmente importante si se resolvieron T — el nuevo estado de agregados puede revelar más oportunidades).
