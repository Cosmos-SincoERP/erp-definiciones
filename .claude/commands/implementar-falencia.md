# Implementar Falencia

Implementa la falencia `$ARGUMENTS` de `AIResume/PlanFalencias.md` usando TDD estricto.

**Uso:** `/implementar-falencia F-001`

---

## Restricción crítica de arquitectura — Marten + polimorfismo en eventos

**PROHIBIDO:** colocar tipos abstractos, interfaces o jerarquías polimórficas como **campos** dentro de records de evento.

Los eventos de Marten son historia inmutable serializada en JSONB. Embeber un tipo abstracto como campo genera un discriminador `$type` en el JSON que:

1. Queda bakeado en los streams históricos de PostgreSQL de forma permanente.
2. **No puede rescatarse** con `EventUpcaster<TOld, TNew>` — ese mecanismo opera al nivel del evento completo, no de sub-campos internos.
3. Falla silenciosamente si JSONB reordena propiedades (`AllowOutOfOrderMetadataProperties` no está configurado en este stack).
4. Acopla nombres de clases C# a historia inmutable — renombrar la clase rompe todos los streams históricos.

**Regla:** los campos de un evento deben ser primitivos (`Guid`, `string`, `decimal`, `DateTime`, `bool`), enums, o value objects **planos** sin herencia.

**Patrón correcto cuando el dominio necesita un Discriminated Union:** implementarlo en la entidad/VO del dominio. El `Apply()` convierte los campos planos del evento al tipo rico:

```csharp
// ✅ CORRECTO — evento plano; Apply() convierte primitivos → tipo rico
public record EstadoDefinido(EstadoEntidad Estado, string? Configuracion) : EntidadEvents;
// Apply() construye el DU internamente:
_estado = EstadoEntidad.Desde(evento.Estado, evento.Configuracion);

// ❌ PROHIBIDO — tipo abstracto como campo de evento
public record EstadoDefinido(IEstadoEntidad Estado) : EntidadEvents;
```

Si la falencia implica modelar un concepto polimórfico que actualmente vive en un evento, **detener y consultar** antes de implementar.

---

## Paso 0 — Leer el plan y preparar el tracking

1. Lee `AIResume/PlanFalencias.md` completo.
2. Localiza la sección `$ARGUMENTS` (formato `F-NNN`). Si no existe o el argumento está vacío, usar `AskUserQuestion` para pedirlo.
3. Si la falencia está marcada **✅ RESUELTA**, notificar y detener.
4. Si es falencia **con decisión de diseño pendiente**, usar `AskUserQuestion` para resolver cada decisión listada antes de continuar.
5. Si es falencia **que requiere especificación**, usar `AskUserQuestion` para obtener la especificación antes de continuar.
6. Leer las cuatro partes del ítem — **Explicación**, **Respaldo en la documentación**, **Ejemplo** y **Qué hay que implementar** — para tener el contexto completo antes de entrar en plan mode. Estas secciones son el contexto autosuficiente de la falencia; no es necesario releer toda la documentación del dominio.
7. Crear `AIResume/$ARGUMENTS_[NombreDescriptivo].md`:

```
> ⚠️ ELIMINAR este archivo cuando todos los checkboxes estén marcados.

# $ARGUMENTS — [Nombre de la falencia]

**Rama:** [git branch --show-current]
**Iniciado:** [fecha actual]
```

Copiar el plan de fases completo de `PlanFalencias.md` para esta falencia (Fase 1, Fase 2, Fase 3 con todos sus checkboxes).

---

## Paso 1 — Entrar en plan mode

Usar `EnterPlanMode` y presentar al usuario:
- Los archivos a crear o modificar
- El orden de implementación
- Cualquier dependencia o riesgo detectado

Esperar confirmación antes de salir de plan mode.

---

## Paso 2 — Fase 1: Tests en rojo

Para cada test listado en el archivo de tracking:

1. Escribir el test siguiendo las convenciones de `CLAUDE.md`:
   - Naming: `Si_X_Debe_Y` en español, PascalCase dentro de X e Y
   - Cobertura obligatoria: happy path + límite exacto del operador + caso que no activa + alcance (solo los afectados)
   - `TestContext.Current.CancellationToken` en todos los calls async
   - `[Fact]` en lugar de `[Theory]` con un único `[InlineData]`
2. Ejecutar `dotnet test --filter FullyQualifiedName~[NombreDelTest]` — verificar que está en **rojo**.
3. Marcar el checkbox en el tracking.

No pasar a Fase 2 hasta que todos los tests de Fase 1 estén en rojo y marcados.

---

## Paso 3 — Fase 2: Implementación

Para cada paso de implementación listado:

1. Implementar el cambio mínimo necesario para hacer pasar los tests.
2. Aplicar las convenciones de `CLAUDE.md`: DRY check antes de escribir lógica nueva, naming de helpers (`ObtenerXxxOLanzar` retorna, `LanzarExcepcionSiXxx` es void), sin comentarios salvo WHY no obvio, sin dead code.
3. Ejecutar `dotnet test --filter FullyQualifiedName~[ClaseDeTest]` — verificar verde.
4. **Gate de heurísticas** — con los tests en verde, recorrer cada bloque de código nuevo. Si alguna fila falla, corregir antes de avanzar a Fase 3. Corregir el gate no es expandir el scope — es parte de implementar correctamente. Las falencias son funcionalidad nueva y el momento de mayor riesgo de introducir smells.

   **Correctitud y contratos (Tier 1):**

   | Heurística | Control |
   |---|---|
   | **Fail Fast** | ¿Guards al inicio del método, no al final? ¿Algún `catch` que swallow sin rethrow / conversión a excepción de dominio con contexto? ¿`?? defaultValue` que oculta un error de negocio? |
   | **Null semantic** | Si un retorno es `T?`, ¿null representa "no aplica" legítimo o un error encubierto? Si es error → método con sufijo `OLanzar`. ¿Hay `FirstOrDefault()` cuyo resultado se usa sin null-check? |
   | **CQS** | ¿Algún método retorna valor Y muta estado simultáneamente? Separar en command + query. |
   | **Inmutabilidad** | ¿Se expone una colección mutable (`List<T>` público, setter público en write side)? Usar `IReadOnlyList<T>`. |
   | **Dead code** | ¿Imports, helpers privados, parámetros o comentarios `// removed` sin uso tras el cambio? |

   **Acoplamiento (Tier 2):**

   | Heurística | Control |
   |---|---|
   | **Tell, Don't Ask** | ¿El código extrae estado de un objeto para decidir por él? La regla va dentro del agregado, no en el servicio que lo consulta. |
   | **Ley de Demeter** | ¿Cadenas `a.B.C.Method()` (≥ 2 niveles, fuera de LINQ / fluent builders)? Introducir método en el objeto navegado. |
   | **Feature Envy** | ¿Método nuevo usa 3+ miembros de otra clase y 0–1 de la propia? El método está en el lugar equivocado. |
   | **Connascence** | ¿El acoplamiento introducido es Position / Meaning / Identity (fuerte)? Evaluar si elevarlo a Name / Type vía record con nombre. |
   | **DRY** | ¿La lógica nueva duplica algo ya existente? Grep antes de asumir. ¿Algún VO o helper estático ya cubre el caso? |

   **Cognitive load (Tier 2):**

   | Heurística | Control |
   |---|---|
   | **Tamaño** | Método público en agregado > 15 líneas. Método privado > 20. Clase > 200. Extraer fases nombradas. |
   | **Parámetros** | Método > 4 parámetros no encapsulados → record con nombre de dominio. |
   | **Ciclomática** | `if` / `for` / `case` / `&&` / `\|\|` / `?:` > 10 → extraer. |
   | **Condicional** | Anidado > 2 niveles → guard clauses. Boolean parameter flag → dos métodos. |
   | **Primitive obsession** | `string` / `decimal` / `bool` con validaciones repetidas → VO. ¿3+ primitivos que viajan juntos sin record? ¿`string idDeX` cuando el tipo `X` ya existe? |

   **Dominio DDD (Tier 3):**

   | Heurística | Control |
   |---|---|
   | **Agregado enriquecido** | Si se agregó un método de negocio: ¿el agregado expone el método (no es un servicio que lee propiedades del agregado para decidir)? El método nuevo, ¿aumenta `M` sin aumentar reads externos `R`? |
   | **Vocabulario de dominio** | ¿Las clases/métodos nuevos tienen nombres de dominio? Sin `Resolver*`, `Procesador*`, `Manager*`, `Handler*`, `Helper*`, `Util*`, `Service*` sin calificar. |
   | **Invariante explícita** | Las reglas nuevas, ¿están enforced en property initializer del VO o en método público del agregado? No como "se confía en que el caller hace X". |
   | **Estado propio usado** | Cada método nuevo en agregado/VO, ¿accede a al menos un campo del objeto (`this.Xxx`)? Si el cuerpo opera solo sobre sus parámetros, es función estática disfrazada — no es encapsulación real. |

   **CUPID (cualitativo):**

   | Propiedad | Pregunta |
   |---|---|
   | **Composable** | ¿Se combina sin arrastrar dependencias pesadas? |
   | **Predictable** | ¿La firma corresponde al comportamiento? ¿Sin sorpresas? |
   | **Idiomatic** | ¿Estilo del lenguaje y del proyecto (CLAUDE.md)? |
   | **Domain-based** | ¿Nombres de dominio, no genéricos? |

   **Rastreos estructurales:**

   | Heurística | Control |
   |---|---|
   | **Apply hygiene** | Si el cambio toca un `Apply(*Event)`: ¿libre de `throw`, helpers que lanzan (`Lanzar*`, `Validar*`, `*OLanzar`), IO, `await`, logging, fuentes no deterministas (`DateTime.Now/UtcNow`, `Guid.NewGuid`, `Guid.CreateVersion7`, `Random`), estado de runtime (`Environment.*`)? Las fuentes no deterministas se generan en el método de negocio y viajan dentro del evento. |
   | **Layer purity** | Si el cambio toca `*.Dominio`: ¿sin `using` prohibido — web/HTTP, ORM concreto, serialización, mensajería concreta, DI container, logging concreto en agregados/VOs, ni capas superiores, ni read side desde write side? |
   | **Identity Surrogate** | Si se introduce un VO/record auxiliar nuevo: ¿tiene huella en eventos / comandos / queries / proyecciones / puertos? Si no, y todos sus call sites lo construyen con campos del mismo agregado → no introducir; el agregado expone un predicado. |
   | **Synonym drift** | Si se nombra una clase/interfaz/record nuevo: ¿coexiste su raíz lingüística con otra forma morfológica en el scope? Si sí, alinearse con la forma vigente. |
   | **Estructura de lookup** | Si se introduce `Dictionary<K,V>` / `HashSet<T>`: ¿volumetría > ~50 documentada? Si no, preferir `IReadOnlyList<V>` + predicado. |
   | **Validación cross-layer** | Si se introduce una validación nueva (mensaje literal, regex, constante numérica): ¿no queda duplicada en otra capa (Dominio + API/DTO/Infra)? Centralizar en VO o agregado. |

   **Whack-a-mole check.** Para cada smell evitado, grep en el resto del scope tocado para confirmar que no se reprodujo en otro archivo (típico en copy-paste al implementar varios handlers en serie).

   **Test relevance check.** Por cada bloque de código nuevo: ¿hay al menos un test en Fase 1 que lo cubre? Si una rama no está cubierta, agregar el test antes de avanzar a Fase 3.

5. Marcar el checkbox en el tracking.

---

## Paso 4 — Fase 3: Verificación

1. Ejecutar el comando de verificación del plan (`dotnet test --filter FullyQualifiedName~[Agregado]`).
2. Verificar que todos los tests nuevos pasan y ningún test existente rompió.
3. Si hay regresiones, investigar y corregir antes de continuar.
4. **Suite completa — ejecutar directamente con Bash, NO vía Agent** (los agentes pueden truncar output y reportar falsos positivos):

   ```bash
   dotnet build [Proyecto].sln 2>&1 | tail -5
   ```

   Si el build falla, corregir antes de continuar.

   ```bash
   dotnet test [Proyecto].sln --no-build 2>&1 | tee /tmp/falencia-test-results.txt; echo ""; echo "=== RESUMEN POR PROYECTO ==="; grep -E "Passed!|Failed!" /tmp/falencia-test-results.txt
   ```

   Verificar que **todos** los proyectos muestren `Failed: 0`. Si hay fallos (los cambios de mapeo pueden requerir actualizar assertions de aceptación para incluir nuevos campos y eliminar `Excluding()` obsoletos): corregir y volver a ejecutar.

5. Marcar cada checkbox de verificación en el tracking.

---

## Paso 5 — Cierre

1. Verificar que **todos** los checkboxes del tracking están marcados `[x]`.
2. Eliminar `AIResume/$ARGUMENTS_[NombreDescriptivo].md`.
3. Marcar la falencia en `AIResume/PlanFalencias.md` como **✅ RESUELTA** con fecha.

4. **Actualizar `AIResume/DiagnosticoYPlanDominio.md`** si existe (cierre del ciclo):

   El diagnóstico queda desactualizado entre re-ejecuciones de `/analisis-plan-dominio`. Cuando una falencia se cierra y corresponde a ítems del diagnóstico, este paso registra el avance retroactivamente.

   **Procedimiento:**

   a. Si no existe `AIResume/DiagnosticoYPlanDominio.md`, omitir este paso.

   b. Si existe, leerlo completo. Buscar ítems cuyo ámbito intersecte con la falencia recién cerrada:
      - Coincidencia por nombre de agregado / domain service / proyección.
      - Coincidencia por capacidad funcional citada (`[F1]`, `[F2]`, `[R*]`, `[I*]`, `[D*]` mencionados en el plan de la falencia y en el ítem del diagnóstico).
      - Coincidencia por archivo afectado.

   c. Por cada ítem candidato encontrado, usar `AskUserQuestion` para confirmar:
      > "La falencia `$ARGUMENTS` cerró: [resumen del fix, 1-2 líneas]. ¿Esto cierra **completamente** el ítem '[nombre del ítem]', lo cierra **parcialmente**, o no está relacionado?"

      - **Cierre completo** → marcar como `✅ COMPLETADO (cerrado por falencia $ARGUMENTS, [fecha])`.
      - **Cierre parcial** → agregar nota: `> Avance parcial — falencia $ARGUMENTS cubrió [aspecto]. Faltante: [aspecto pendiente].`
      - **No relacionado** → no modificar.

   d. Si la falencia introdujo capacidades no documentadas como ítem, agregar en el Changelog del diagnóstico:
      `[fecha] — Falencia $ARGUMENTS cerrada. Cambios: [resumen]. Próxima ejecución de /analisis-plan-dominio revalidará el estado completo.`

   e. **No re-ejecutar `/analisis-plan-dominio` automáticamente.** Este paso solo registra el avance para mantener trazabilidad.

5. Reportar al usuario:
   - Tests creados (con nombres).
   - Archivos modificados.
   - Falencia cerrada en `PlanFalencias.md`.
   - Ítems actualizados en `DiagnosticoYPlanDominio.md` (completados / parciales / sin tocar).
