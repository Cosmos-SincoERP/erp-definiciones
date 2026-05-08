Realiza una revisión de correctitud, diseño e idiomatismo del código. Busca bugs, smells, oportunidades de patrón, anemia de dominio y problemas de diseño razonando sobre el código — no verificando convenciones de estilo (eso lo hace `/lint-standards`).

**Scope:** si se pasa un argumento (ruta de archivo o carpeta), analiza solo ese scope. Si no hay argumento, descubrir la solución desde `*.sln` y analizar todos los proyectos (excepto los excluidos abajo).

Excluir siempre: `bin/`, `obj/`, `*.g.cs`, archivos de tests (`*.Tests/`, `AcceptanceTests/`).

> Este comando analiza la **calidad técnica** del código desde principios universales. Para detectar ausencias funcionales usar `/find-falencias`. Para detectar divergencias con el modelo documentado usar `/revision-conformidad-modelo`.

**Marco conceptual:** la revisión se ancla en:
- **SOLID** — Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion. Principios base de diseño OO.
- **DDD tactical** — coherencia de agregados, VOs, domain events, repositorios. Este proyecto se declara DDD-first; es el marco dominante.
- **Connascence** (Meilir Page-Jones) — los tipos de acoplamiento ordenados por "fuerza". Refactor mejora acoplamiento "fuerte" (position, meaning, algorithm) hacia "débil" (name, type).
- **CUPID** (Dan North) — propiedades deseables (Composable, Unix philosophy, Predictable, Idiomatic, Domain-based).
- **Fowler code smells** y **Object Calisthenics** — catálogo de smells y reglas de diseño OO.
- **GoF patterns** — oportunidades de patrón cuando el código las "pide".

---

## Restricción crítica de arquitectura — Marten + polimorfismo en eventos

Al proponer hallazgos (especialmente T — Discriminated Union Disguised, o S — Strategy/polimorfismo), tener presente:

**PROHIBIDO proponer** colocar tipos abstractos, interfaces o jerarquías polimórficas como **campos** dentro de records de evento de Marten.

Los eventos son historia inmutable en JSONB. Un campo abstracto embebe un discriminador `$type` que:
- Queda permanentemente en los streams históricos de PostgreSQL.
- No puede arreglarse con `EventUpcaster<TOld, TNew>` (opera al nivel del evento, no de sub-campos).
- Falla silenciosamente con JSONB si `AllowOutOfOrderMetadataProperties = true` no está configurado — y **no lo está** en este proyecto.
- Acopla nombres de clases C# a historia inmutable.

**Regla para hallazgos T (Discriminated Union Disguised):** cuando el smell existe en un campo que también es parte de un evento, la corrección correcta es solo en la capa de dominio:

```csharp
// ✅ Evento plano (no cambiar)
public record EstadoDefinido(EstadoEntidad Estado, string? Configuracion) : EntidadEvents;

// ✅ DU solo en la entidad de dominio
public record Entidad(..., EstadoEntidadDU EstadoRico, ...);
// Apply() convierte primitivos → DU
```

Cuando se detecte un T-Discriminated Union Disguised donde el campo correlacionado también vive en eventos, reportarlo con esta nota explícita en el hallazgo y severidad ajustada (la corrección completa end-to-end no es viable sin migración de Event Store).

---

## Paso 0 — Leer historial de revisiones previas (memoria factual no autoritativa)

Antes de iniciar cualquier rastreo de la Pasada 1 en adelante:

1. **Localizar el archivo de historial.** Path canónico: `AIResume/docs/revision-codigo/historial.md`.
   - Si **no existe**, omitir este paso y continuar con Paso 1.
   - Si existe, **leerlo completo** y mantener su contenido en contexto durante toda la revisión.

2. **Tratar el historial como contexto factual no autoritativo.** El archivo tiene dos secciones:
   - **Hallazgos Resueltos (`RC-NNN`)** — hallazgos previos cerrados, con la condición original que el código actual NO debería tener.
   - **Decisiones Tomadas (`DC-NNN`)** — decisiones de diseño con alternativas descartadas y condiciones para reabrirlas.

   **El historial NO certifica que el código actual esté correcto.** Cada revisión revalida desde cero.

3. **Reglas estrictas de uso (todas obligatorias):**

   a. **Revalidar siempre desde el código actual.** No asumir que algo está correcto por aparecer como resuelto en el historial.

   b. **No re-reportar hallazgos resueltos** cuya condición ya no exista en el código.

   c. **Reportar como regresión** si la condición de un `RC-NNN` resuelto vuelve a estar presente:
      - Marcador en el título: `(regresión de RC-NNN)`.
      - Severidad **+1 sobre la original**: Minor → 🟡 Major; Major → 🔴 Critical; Critical → 🔴 Critical (clamped).

   d. **No volver a proponer alternativas descartadas en `DC-NNN`** salvo que exista nueva evidencia objetiva (siguiente regla).

   e. **"Nueva evidencia objetiva" se define exclusivamente como uno de estos seis casos:**
      1. Cambio documentado de requisitos del producto.
      2. Cambio de volumen real medido en producción o documentado.
      3. Mediciones de rendimiento reproducibles (no especulativas).
      4. Bug reproducible atribuible a la decisión vigente.
      5. Cambio arquitectónico del proyecto que vuelve insuficiente la decisión.
      6. Nueva documentación oficial de proveedor (Marten, .NET, librerías) que altera las condiciones técnicas.

   f. **Priorizar el código actual ante contradicciones.** Si el historial dice una cosa y el código actual otra, prima el código actual y se registra la contradicción en "Discrepancias con historial" del reporte.

4. **Mantener en contexto explícito durante toda la revisión:**
   - Lista de IDs `RC-NNN` resueltos con su condición original.
   - Lista de IDs `DC-NNN` con la decisión vigente y las alternativas descartadas.

---

## Paso 1 — Leer el código en scope

Lee todos los archivos `.cs` relevantes. Incluir explícitamente clases utilitarias pequeñas (factories, helpers estáticos, clases selladas de menos de 60 líneas) — son fáciles de omitir pero frecuentemente contienen bugs o reglas de dominio mal ubicadas.

Para cada componente con lógica, retener en contexto:

- Qué estado mantiene (campos privados, colecciones)
- Qué métodos tiene, sus firmas, qué reciben y qué retornan
- Qué muta y en qué condiciones
- De quién depende y cómo recibe sus dependencias
- Qué casos especiales maneja (nulls, colecciones vacías, condiciones de borde)

### Rastreos activos obligatorios

**Cada rastreo debe producir una tabla / lista concreta.** No saltarse un rastreo con "no aplica" — llenarlo explícitamente permite detectar patrones multi-archivo que se pierden en un scan lineal.

#### Pasada 1 — Llamadas en loops (hallazgos G/P)
Para cada llamada dentro de `foreach`/`for`, seguir la llamada y verificar si el resultado es idempotente respecto al input de la iteración. Si lo es → G (cómputo repetido). Si hace IO (DB, HTTP) → P (N+1).

#### Pasada 2 — Campos de tipos de resultado / CQS (A/B/E)
Para cada campo en records de resultado/DTOs/líneas de desglose, verificar que afecte al menos un cálculo o rama condicional. Campo propagado sin uso → A o E.

Para cada método con retorno en el scope: verificar que no mute estado simultáneamente (CQS). Un método que retorna un valor Y produce un efecto observable en el estado → B.

#### Pasada 3 — Inventario de salidas silenciosas (C/N)
Para cada método con lógica, listar TODOS sus puntos de salida temprana (`if (x is null) return`, `return null`, `?? default`). Para cada una, verificar si hay acción observable (excepción, log, registro en colector, evento). Faltante → hallazgo.

#### Pasada 4 — Trace de Connascence (O)
Para cada cross-class call, clasificar el tipo de connascence (ver Categoría O). Las "fuertes" (Meaning, Position, Algorithm, Identity) son candidatas a elevar.

#### Pasada 5 — Trace de invariantes cruzados (I)
Para cada invariante de agregado o VO (unicidad, no-solapamiento, orden), verificar si se preserva cuando se combinan instancias distintas aguas abajo.

#### Pasada 6 — Trace de complejidad (K/M)
Para cada método > 25 líneas, calcular ciclomática manual (cada `if`/`for`/`case`/`&&`/`||`/`?:` suma 1). > 10 = candidato K. > 15 = Critical.

#### Pasada 7 — Inventario de comportamiento por agregado (T) ⭐

**La pasada más importante para detectar anemia.** Construir una tabla por cada aggregate root o entidad de dominio referenciada en el scope:

| Agregado / Entidad | Propiedades leídas por el scope | Métodos llamados | Ratio R:M | Anemia |
|---|---|---|---|---|
| [AggRoot1] | lista de props | lista de métodos | p:m | ✓ rico / ⚠️ borderline / 🔴 anémico |

**Criterios:**
- Ratio R:M ≤ 1:1 con métodos que expresan decisiones → rico.
- Ratio R:M 2:1 a 3:1 → **borderline**: flaggear cada property read donde el scope **decide algo** basado en él.
- Ratio R:M > 3:1 o métodos = 0 → 🔴 **anémico**. Listar en Categoría T cada regla del servicio que debería ser método del agregado.

Para cada read flaggeado: enunciar qué método **debería existir** en el agregado (ej. `Pedido.EstaConfirmado()` en lugar de `pedido.Estado == EstadoPedido.Confirmado` evaluado externamente).

**Filtro obligatorio antes de reportar un hallazgo T:** el método propuesto debe usar **al menos un campo propio** del agregado/entidad (`this.Xxx`). Si todos los inputs llegarían como parámetros externos y el cuerpo del método no toca ningún campo del objeto, es una función estática disfrazada — **no es anemia, no reportar**. Benchmark válido: `entrada.AplicarSobre(baseGravable)` usa `this.TipoTarifa` y `this.Tarifa` (campos propios del VO). Contraejemplo inválido: `static bool SuperaUmbral(decimal monto, decimal umbral) => monto >= umbral` (cero campos propios — solo renombra un operador).

**Check adicional — LSP en agregados event-sourced:**
Verificar si algún agregado hereda de otro agregado (no de `AggregateRoot`). Una jerarquía `ClaseHija : ClaseBase` donde ambas heredan de `AggregateRoot` viola LSP en el contexto ES: si `ClaseHija` sobrescribe un `Apply()` y lanza donde `ClaseBase` no lanza, rompe el replay de streams históricos. Reportar como hallazgo T (🔴 Critical si el override introduce `throw`; 🟡 Major si modifica comportamiento). La corrección es modelar las variantes como campo interno (`enum TipoAgregado`) — no como jerarquía de clases.

#### Pasada 8 — Discriminantes + switches distribuidos (K/S/T)
Para cada `enum` y cada `Kind`/`Tipo`/discriminante en el scope, construir:

| Enum | Valores | Archivos con `switch`/`if` sobre él | Record con campo nullable correlacionado | Candidato polimorfismo |
|---|---|---|---|---|
| [EnumName] | [vals] | [archivos] | [campo?] | ✓/— |

**Reglas:**
- Switch sobre el mismo enum en ≥3 archivos distintos → candidato S (polimorfismo).
- Record con campo `T? X` que solo es no-null cuando otro campo `Kind == ValorEspecifico` → **Discriminated Union Disguised**. Candidato a jerarquía polimórfica.
- Validación "si Kind == X entonces campo Y debe..." en property initializer + switch externo → confirmación del patrón.

#### Pasada 9 — Probe de costo de extensión (S/T/M)

Simular **tres extensiones concretas y plausibles** para el dominio del scope. Para identificar las extensiones plausibles, usar como criterio: qué tipos de cambio harían los desarrolladores del próximo sprint según la arquitectura del dominio observada. Ejemplos típicos:

- Agregar un nuevo valor al enum de estados del agregado principal (ej. nuevo estado de ciclo de vida).
- Agregar un nuevo tipo de entidad/VO dentro de un agregado existente.
- Agregar un nuevo comando a un agregado ya implementado (nuevo comportamiento de negocio).

| Extensión hipotética (concreta para este dominio) | Archivos a modificar | Clasificación |
|---|---|---|
| [extensión 1] | [lista] | 🔴 Shotgun (>5) / 🟡 (3-5) / 🔵 (1-2) |
| [extensión 2] | [lista] | |
| [extensión 3] | [lista] | |

**Regla:** > 5 archivos en una extensión razonable del negocio = Shotgun Surgery concreta, no especulativa. Reportar en E (Fowler smell) Y en T (si la causa es anemia / falta de polimorfismo).

#### Pasada 10 — Onboarding cost
Partir del entry point público principal del scope y trazar el call graph a profundidad ≤ 3. Contar:
- Archivos distintos tocados.
- Agregados/VOs referenciados.

Reportar la métrica. > 10 archivos para un único use case → flag: la lógica podría delegarse a agregados para bajar el conteo sin perder semántica.

#### Pasada 11 — Auditoría de vocabulario (CUPID Domain-based)
Listar cada clase, método público, y VO del scope. Clasificar:

| Tipo | Nombre | ¿Verbo genérico / técnico? | ¿Sinónimo coexistente? | Término de dominio sugerido |
|---|---|---|---|---|
| Clase | `ResolvedorDeX` | ✓ (Resolver) | — | [propuesta] |
| Clase | `ResolutorDeY` | ✓ (Resolver) | ✓ coexiste con `ResolvedorDeX` | unificar a `Resolvedor` (o ambos a término de dominio) |
| Clase | `ProcesadorDeZ` | ✓ (Procesar) | — | [propuesta] |

**Verbos/sufijos a flaggear como técnicos:** Resolver, Convertir, Cache, Collect/Colector, Factory/Fabrica, Process/Procesador, Manage/Gestor, Handle/Handler, Helper, Util, Service (sin calificar), Engine/Motor sin contexto de dominio claro.

**Regla técnica:** > 50% de nombres técnicos en un scope declarado como dominio → patrón sistémico: "vocabulario técnico desplaza al lenguaje ubicuo".

**Sub-rastreo: Synonym drift (extiende Pasada 11)**

1. Tras enumerar todos los nombres, agruparlos por **raíz lingüística**:
   - Eliminar prefijo `I` si es interfaz.
   - Eliminar sufijos de dominio: `DeXxx`, `DelXxx`, `DeLaXxx`.
   - Tomar los primeros 4-6 caracteres del lema resultante.

2. Agrupar por raíz. Si hay ≥ 2 nombres distintos con la misma raíz y mismo rol conceptual → sinónimo coexistente.

3. Pares típicos a buscar:
   - `Resolutor` / `Resolvedor` (raíz `Resol`)
   - `Determinador` / `Decididor`
   - `Convertidor` / `Conversor` (raíz `Conver`)
   - `Validador` / `Verificador` / `Comprobador`
   - `Manejador` / `Gestor` / `Administrador`

4. Reportar como **T-Synonym Drift** si hay ≥ 1 par morfológico. Severidad: 🟡 Major si afecta nombres públicos, 🔵 Minor si solo locales/privados.

#### Pasada 12 — Auditoría de VOs / types accesorios (T-Identity Surrogate)

Para cada VO, record auxiliar o tipo de soporte declarado en el scope **excluyendo** eventos, comandos, queries, request/response DTOs y read models de proyección:

| Tipo | Campos | ¿En eventos? | ¿En comandos / queries? | ¿En proyecciones / read models? | ¿En puertos / contratos externos? | ¿Algún call site 100% derivado de un único agregado? | Veredicto |
|---|---|---|---|---|---|---|---|
| `[Tipo]` | [campos] | ✓/❌ | ✓/❌ | ✓/❌ | ✓/❌ | ✓/❌ | ✓ legítimo / 🟡 sin huella / 🔴 Identity Surrogate |

**"Call site 100% derivado de un único agregado"**: existe al menos un call site con la forma `new TipoAuxiliar(otro.X, otro.Y[, ...])` donde `otro` es la misma instancia en todos los argumentos y todos son propiedades públicas de `otro`.

**Reglas de veredicto:**

- **🔴 Identity Surrogate** — cuatro `❌` + al menos un call site 100% derivado de un único agregado. El agregado debería exponer un predicado `EsParaXxx(parámetros)` que use sus campos propios. El tipo auxiliar se elimina.

  > **Por qué los lookups con inputs externos no salvan el veredicto:** confirman que el VO se usa como llave de búsqueda sobre el mismo agregado que ya conoce su identidad.

- **🟡 VO sin huella semántica** — cuatro `❌` sin call site 100% derivado. Revisar caso a caso. Mencionar en el reporte si N ≥ 3 en el scope.

- **✓ legítimo** — al menos una `✓` en columnas de huella. Ignorar.

**Filtro anti-falso-positivo:** el método propuesto para reemplazar el Identity Surrogate debe usar al menos un campo propio del agregado origen.

**Caveat para event sourcing:** si el VO aparece en eventos, eliminar retroactivamente puede no ser viable. La sustitución se mantiene como propuesta pero la severidad baja a Major y el reporte debe explicitar la migración requerida.

#### Pasada 13 — Apply hygiene en agregados event-sourced (F-Apply impuro) ⭐

Los métodos `Apply(...)` participan en tres escenarios donde su comportamiento debe ser idéntico: append normal, live aggregation, y rebuild de daemon. Un `throw` dentro rompe la rehidratación de streams válidos.

**Forma esperada:**

```csharp
public void Modificar(/* params */)
{
    ValidarReglasDeNegocio(/* params */);
    RegistrarEvento(new EntidadModificada(/* ... */));
}

public void Apply(EntidadModificada evento)
{
    // Solo mutar estado.
    _entradas[idx] = _entradas[idx] with { Tarifa = evento.Tarifa };
}
```

**Procedimiento:**

1. **Identificar clases candidatas** cuya declaración matchee:
   - `: AggregateRoot`
   - `: SingleStreamProjection<`
   - `: MultiStreamProjection<`
   - `: CustomProjection<` o `: EventProjection`

2. **Localizar métodos `Apply` / `Create`** con firmas:
   - `public void Apply(<EventType> @event)`
   - `public <ReadModelType> Apply(<EventType> @event, <ReadModelType> readModel)`
   - `public <ReadModelType> Apply(IEvent<<EventType>> @event, <ReadModelType> readModel)`
   - `public <ReadModelType> Create(IEvent<<EventType>> @event)`
   - `public <ReadModelType> Create(<EventType> @event)`

3. **Escanear el cuerpo** de cada método identificado:

   | # | Patrón | Detección | Severidad | Razón |
   |---|---|---|---|---|
   | 1 | `throw` directo | match `throw` + espacio/paréntesis | 🔴 Critical | rompe replay |
   | 2 | Helper que lanza: `Lanzar*`, `Validar*`, `Verificar*`, `*OLanzar` | regex en invocaciones | 🔴 Critical | excepción indirecta |
   | 3 | Logging: `Console.`, `Trace.`, `Debug.`, invocación sobre `ILogger` | match string | 🟡 Major | side effect en replay |
   | 4 | `await` en el cuerpo | match `await` | 🟡 Major | Apply debe ser síncrono |
   | 5 | IO: `HttpClient`, `IDocumentSession`, `IQuerySession`, `DbContext`, `IBus`, `Stream`, `File.` | match identificadores | 🟡 Major | no debe tocar mundo externo |
   | 6 | No-determinismo: `DateTime.Now`, `DateTime.UtcNow`, `Guid.NewGuid(`, `Guid.CreateVersion7(`, `Random` | match string | 🟡 Major | la fecha/ID viaja dentro del evento |
   | 7 | Runtime: `Environment.`, `Process.`, `Thread.CurrentThread`, `CultureInfo.CurrentCulture` | match string | 🟡 Major | replay depende del entorno |
   | 8 | `.First()` / `.Single()` sin null-check sobre colección mutada por otro `Apply` | inspección manual | 🔵 Minor | preferir `FirstOrDefault` |

4. **Excepción permitida:** `throw` defensivo por corrupción imposible, solo con comentario inline explícito que lo declare. Sin ese comentario → 🔴 Critical.

**Tabla de evidencia:**

| Archivo | Clase | Método | Patrón # | Severidad |
|---|---|---|---|---|
| `[archivo]` | `[ClassName]` | `Apply([EventName])` | [1-8] | 🔴/🟡/🔵 |

#### Pasada 14 — Pureza de capa de dominio (F-Dependencia de capa rota)

1. **Identificar proyectos de dominio:**
   - `.csproj` con nombre `*.Dominio.csproj` o `*.Dominio.<sufijo>.csproj`.
   - Excluir `*.Dominio.Tests.csproj`.

2. **Para cada archivo `.cs` del dominio**, listar directivas `using`.

3. **Comparar contra la lista negra:**

   | Categoría | Namespaces prohibidos | Razón |
   |---|---|---|
   | Web / HTTP | `Microsoft.AspNetCore.*`, `System.Net.Http.*` | dominio no conoce transporte |
   | Persistencia / ORM | `Microsoft.EntityFrameworkCore.*`, `Marten.*` (excl. abstracciones), `Dapper.*` | dominio depende de abstracciones |
   | Serialización | `System.Text.Json.*`, `Newtonsoft.Json.*`, `MessagePack.*` | pertenece a la capa de transporte |
   | Logging concreto | `Serilog.*`, `NLog.*`. `Microsoft.Extensions.Logging.*` solo en handlers/services con `ILogger<>` inyectado | delegar a capa de aplicación |
   | DI containers | `Microsoft.Extensions.DependencyInjection.*` (solo en `*Module.cs` / `ServiceCollectionExtensions.cs`) | configuración DI no pertenece al dominio |
   | Mensajería | `Wolverine.*` (excl. `Wolverine.Attributes`), `MassTransit.*`, `RabbitMQ.Client.*` | dominio depende de abstracción de bus |
   | Capas superiores | `.API`, `.Application`, `.Infraestructura`, `.Grpc`, `.MCP`, `.Web` | inversión de dependencias rota |
   | Read side | `*.Consultas.*`, `*.Queries.*` desde write side | CQRS roto |

4. **Excepciones declaradas (no generan hallazgo):**
   - Sub-namespaces de abstracción: `Cosmos.EventSourcing.Abstractions`, `Marten.Schema.Identity`, `Wolverine.Attributes`.
   - Archivos en `Abstractions/`, `Interfaces/`, `Ports/` que solo declaran contratos.

5. **Reportar** como F-Dependencia de capa rota 🟡 Major.

**Tabla de evidencia:**

| Archivo de dominio | `using` prohibido | Categoría | Ruta sugerida |
|---|---|---|---|
| `[Dominio/.../X.cs]` | `using [Namespace];` | Web / Persistencia / etc. | abstraer detrás de interfaz |

---

## Paso 2 — Analizar por categorías

Los hallazgos se agrupan en 3 **tiers**:

- **Tier 1 (correctitud):** A, B, C, D, I, N, Q, R.
- **Tier 2 (diseño):** E, F, G, H, J, K, L, M, O.
- **Tier 3 (idiomatismo / dominio / patrón):** T, S.

Para cada hallazgo documentar: archivo, línea(s), severidad, descripción, impacto, corrección sugerida, y **referencia** (Connascence type, CUPID property, GoF pattern, DDD pattern, Fowler smell).

### Tier 1 — Correctitud

#### Categoría A — Bugs de lógica
- Variable incorrecta, cálculo que ignora datos, acumulación sin consumo completo, doble emisión, condición de salida prematura.

#### Categoría B — Efectos secundarios ocultos / CQS
- Métodos `Resolver*`/`Obtener*`/`Calcular*`/`Evaluar*` que mutan parámetros.
- **Command Query Separation (CQS):** método que retorna un valor Y muta estado simultáneamente. Un método es comando (muta, retorna void) o query (retorna valor, no muta) — nunca ambos.

#### Categoría C — Pérdida silenciosa de datos
- `return null` sin log/descartado/excepción.
- `FirstOrDefault` en colección multi-elemento.
- `?. / ?? null` que oculta un caso de negocio.
- Múltiples salidas silenciosas en el mismo método.

#### Categoría D — Estado mutable compartido / Inmutabilidad
- `List<T>` pública + `Add` externo.
- Múltiples componentes escribiendo al mismo objeto en pipeline.
- Acoplamiento temporal implícito.
- **Inmutabilidad violada en write side:** propiedad con setter público en un agregado o entidad, colección expuesta como `List<T>` en lugar de `IReadOnlyList<T>`, o `record` con `{ get; set; }` donde debería ser `{ get; init; }`. En el write side el estado muta solo a través de `Apply()`.

#### Categoría I — Contratos implícitos productor/consumidor
- Precondición no verificada (homogeneidad, unicidad, orden).
- Clasificación por heurística (`Any`) consumida como invariante.
- "Last writer wins" sin garantía de orden.
- Validación intra-componente que debería ser inter-componente.

#### Categoría N — Fail Fast / contratos explícitos
- Guard clause ausente al inicio.
- `catch (Exception)` sin rethrow/conversión.
- Precondición asumida sin verificar.
- `?? defaultValue` donde null debería ser error.
- Null-forgiving operator (`!`) sin invariante documentada.

#### Categoría Q — Seguridad
- Input no validado cruzando un borde.
- Injection surface, PII en logs, authz faltante, deserialización insegura, timing attacks.

#### Categoría R — Concurrencia y thread-safety
- Estado mutable estático sin lock.
- TOCTOU, async void, ConfigureAwait inconsistente, Task.Run en código async sin justificación.

### Tier 2 — Diseño

#### Categoría E — Code smells (Fowler catalog)
- Class/record que solo reempaqueta.
- Paso de transformación innecesario.
- Parámetros sin responsabilidad.
- `TODO`/`HACK` sin resolver.
- Método privado llamado exactamente una vez.
- Método de entrada sin orquestación delegada.
- **Middle Man**, **Divergent Change**, **Shotgun Surgery**.
- **E-Validación duplicada cross-layer:** la misma validación (mensaje literal, regex, constante numérica) aparece en ≥ 2 archivos de capas distintas.

  **Procedimiento:**
  1. Construir índices de mensajes de excepción literales, patrones regex, constantes numéricas de validación.
  2. Para cada literal/regex/constante, contar archivos distintos.
  3. Mapear cada archivo a su capa:
     - `*.Dominio.*` → **Dominio**
     - `*.API.*`, `*.Comandos.API.*`, `*.Consultas.API.*` → **API**
     - `*.Application.*` → **Application**
     - `*.Infraestructura.*`, `*.Infrastructure.*` → **Infra**
     - `*.Tests.*`, `*.AcceptanceTests.*` → **Tests**
     - `*.Requests.*`, `*.Contratos.*`, `*.DTOs.*` → **DTO/Contracts**
  4. Marcar como hallazgo si el mismo elemento aparece en ≥ 2 capas distintas.

  **Severidad:**
  - 🔵 Minor — Dominio y Tests (acoplamiento aceptable: tests verifican el contrato).
  - 🟡 Major — Dominio y API/Infra/Application/DTO sin delegación.

  | Literal / Regex / Constante | Archivos | Capas | Sugerencia |
  |---|---|---|---|
  | `"El código no puede estar vacío."` | `[X.cs]`, `[Y.cs]` | Dominio + API | mover a VO; las demás capas delegan |

#### Categoría F — Diseño arquitectónico
- IO + lógica pura en el mismo método.
- Dominio con dependencias de infra.
- Abstracción sin justificación.
- Contrato demasiado amplio.
- Puertos/adaptadores mal ubicados.
- **Layer mixing**: un método hace simultáneamente application service + domain service + data mapping.
- CQRS/ES mal aplicado.
- **F-Apply impuro:** detectado por Pasada 13. Severidad: 🔴 Critical para `throw` y validaciones; 🟡 Major para side effects, IO y fuentes no deterministas.
- **F-Dependencia de capa rota:** detectado por Pasada 14. Severidad: 🟡 Major.

#### Categoría G — Diseño redundante o sobredimensionado
- Dos niveles de indirección para lo mismo.
- Cómputo idempotente repetido N veces.
- Enum/constante que nunca toma más de un valor.
- Premature abstraction.
- **G-Estructura de lookup sobredimensionada:** uso de `Dictionary<K,V>`, `HashSet<T>` cuando la volumetría ≤ ~50 instancias.

  **Procedimiento:**
  1. Localizar construcciones de lookup: `new Dictionary<`, `.ToDictionary(`, `new HashSet<`, `.ToLookup(`.
  2. Identificar `V` (tipo del valor).
  3. Buscar volumetría documentada con greps:

     ```bash
     grep -rni --include="*.md" --include="*.cs" -E "(~|aprox\.?|approximately|cerca de|alrededor de) ?[0-9]{1,4} (streams?|instancias?|registros?|entries|items|filas|rows|elementos)" .
     grep -rni --include="*.md" --include="*.cs" -E "(<=?|≤|máximo|maximo|max\.?|hasta) ?[0-9]{1,4} (streams?|instancias?|registros?|entries|items|elementos|por (pa[ií]s|jurisdicci[oó]n|tenant|cliente))" .
     grep -rni --include="*.md" --include="*.cs" -E "volumen (esperado|estimado|t[ií]pico|de referencia)[: ]" .
     # Buscar el nombre del tipo V en documentación de dominio:
     grep -rni --include="*.md" -E "\b[0-9]{1,4}\b.*(instancias?|elementos?|entidades?|registros?)" .
     grep -rni --include="*.md" -E "\b<V>\b.*([0-9]{1,4})" .   # reemplazar <V> con el nombre concreto del tipo
     ```

  4. Clasificar:

     | Volumetría documentada | Pasada 12 sobre `K` | Veredicto |
     |---|---|---|
     | ≤ ~50 (encontrada) | cualquier | 🟡 Major — Dictionary sobredimensionado |
     | No documentada | 🔴 Identity Surrogate sobre `K` | 🟡 Major — la corrección de Identity Surrogate elimina el Dictionary |
     | No documentada | 🟡 sin huella sobre `K` | 🔵 Minor — observación |
     | No documentada | ✓ legítimo, `V` es agregado ES | 🔵 Minor — "volumetría no documentada; verificar antes de aceptar" |
     | > ~50 (encontrada) | cualquier | no es hallazgo |

  **Regla anti-falso-negativo:** si greps devuelven 0 matches y `V` es un agregado, registrar 🔵 Minor obligatorio.

  | Estructura | Tipo de valor | Volumetría documentada | Sugerencia |
  |---|---|---|---|
  | `Dictionary<ClaveX, AgregadoY>` | `AgregadoY` | "~25 instancias" en `[doc]` | reemplazar por `IReadOnlyList<AgregadoY>` + predicado |

#### Categoría H — Valores mágicos
- Strings literales como identificadores.
- Números mágicos.
- Rutas/nombres repetidos.

#### Categoría J — Tell, Don't Ask / Ley de Demeter

Patrones concretos greppeables:
- **Getter chain para decidir:** `objeto.Prop.Method()` a 2+ niveles para evaluar condición.
- **Propiedad leída solo para lanzar** → guard debería ser método del objeto.
- **Servicio que reproduce lógica de un agregado** (ver Categoría T).
- **LINQ sobre colección interna del agregado:** `agregado.Coleccion.Where(...)` donde la expresión codifica una regla → método semántico del agregado.
- **Switch sobre propiedad enum del agregado en el servicio** → polimorfismo refused (ver T).
- **Cadena `a.B.C.Method()`** (excluye LINQ/fluent): dos capas de indirección.
- **Feature Envy:** método usa 3+ miembros de otra clase y 0–1 de la propia.

#### Categoría K — Complejidad condicional
- Condicionales anidados > 2 niveles.
- `if/else if` > 5 ramas sobre el mismo discriminante.
- Boolean parameter flag.
- Negaciones anidadas.
- Condición repetida en 3+ lugares.
- Ciclomática > 10 (Minor) / > 15 (Major).

#### Categoría L — Obsesión por primitivos
- `string` código/identificador con validaciones repetidas → VO.
- `decimal` sin semántica de unidad.
- Data Clumps (2-3 campos que viajan juntos).
- `bool` que codifica ciclo de vida.
- Parámetro primitivo donde existe un VO.

#### Categoría M — Métodos y clases grandes
- Método > 25 líneas (público en agregado > 15).
- Clase > 200 líneas.
- Método con > 4 parámetros no encapsulados.
- Divergent Change / Shotgun Surgery.

#### Categoría O — Connascence
Clasificar cada acoplamiento cross-class. Formas fuertes → elevar a débiles.

**Estáticas:** CoN (Name), CoT (Type), CoM (Meaning/Convention), CoP (Position), CoA (Algorithm).
**Dinámicas:** CoE (Execution), CoT (Timing), CoV (Values), CoI (Identity).

**Cross-check obligatorio con Pasada 12** para CoA o CoM entre construcción y lookup de un VO accesorio:

| Veredicto Pasada 12 | Corrección preferida | Antipatrón a NO proponer |
|---|---|---|
| ✓ legítimo | Centralizar construcción: `VO.Para(...)` | — |
| 🟡 sin huella | Eliminar VO vía predicado o tuple inline | Factory estático sobre el VO |
| 🔴 Identity Surrogate | Eliminar VO vía `agregado.EsParaXxx(...)` con campos propios | `VO.Para(...)`, `agregado.Identidad` que devuelva el tuple |

### Tier 3 — Dominio y patrón

#### Categoría T — Anemia de dominio (DDD) ⭐

Patrones concretos:

- **T-Propiedad decisora:** el servicio lee una propiedad del agregado para tomar una decisión que debería ser método del agregado. Ejemplo: `if (pedido.Lineas.Any(linea => linea.Estado == EstadoLinea.Pendiente))` → `if (pedido.TienePendientes())`.
- **T-Colección interna consultada:** agregado expone colección consultable en lugar de consultas semánticas. Ejemplo: `servicio.Entidades.Where(e => e.Vigencia.Contiene(fecha))` → `agregado.VigentesEn(fecha)`.
- **T-Regla reimplementada:** servicio compone lógica que el agregado ya conoce. Si el método resultante usaría campos propios del agregado, la regla debe estar en el agregado.
- **T-Invariante multi-hijo en servicio:** regla que coordina hijos de un agregado raíz, enforced desde fuera en vez de dentro del raíz.
- **T-Discriminated Union Disguised:** record con campo `Kind: Enum` + nullable correlacionado. Candidato a jerarquía polimórfica.
- **T-Identity Surrogate:** detectado por Pasada 12.
- **T-Vocabulario técnico en clases de dominio:** `Resolvedor*`, `Convertidor*`, `Procesador*`, `Gestor*` en nombres de dominio.
- **T-Synonym Drift:** detectado por Pasada 11.
- **T-Repository camuflado:** puerto que carga agregados por criterio sin "Repositorio" en el nombre.
- **T-Transaction Script disfrazado:** domain service como secuencia lineal de mutaciones a un bag de estado.
- **T-God Parameter Object:** record "Contexto/Request/Scope" pasado a N colaboradores con < 50% de campos usados por cada uno.

Para cada hallazgo T, **nombrar el método que debería existir** en el agregado/VO.

**Antes de reportar:** verificar que el método propuesto use al menos un campo propio. Si todo llega como parámetro externo y el cuerpo no toca ningún `this.Xxx` → función estática disfrazada, no anemia.

#### Categoría S — Oportunidades de patrón (GoF / arquitectónicos)

**Creacionales:** Builder, Factory Method, Abstract Factory, Prototype.
**Estructurales:** Adapter, Bridge, Composite + Visitor, Decorator, Facade, Flyweight, Proxy.
**Comportamentales:** Chain of Responsibility, Command, Iterator, Mediator, Memento, Observer, State, Strategy, Template Method, Visitor.

**Arquitectónicos:**
- Ports/adapters mal invertidos.
- CQRS: read model sincronizado con escritura.
- Event Sourcing: proyección no idempotente o mal reintento.

**Antipatrones:**
- Singleton con estado mutable compartido.
- Decorator que rompe el contrato que envuelve.
- Factory trivial. Strategy con 1 sola estrategia real.

---

## Paso 3 — Evaluación CUPID (cualitativa, global)

Evaluar el scope contra las 5 propiedades:

- **Composable** — ¿las clases se combinan bien sin arrastrar contexto pesado?
- **Unix philosophy** — ¿cada clase hace una cosa bien?
- **Predictable** — ¿el comportamiento corresponde a la firma? ¿sin sorpresas?
- **Idiomatic** — ¿sigue el estilo del lenguaje y del proyecto?
- **Domain-based** — referencia la tabla de la Pasada 11. Ratio dominio/técnico, lista de clases a reconsiderar.

---

## Paso 4 — Preguntar ante ambigüedad

Usar `AskUserQuestion` si:
- El código parece incorrecto pero podría ser decisión intencional documentada.
- La extracción de patrón (S) o el movimiento de lógica a agregado (T) cambiaría API pública.
- La severidad no es clara.

**Stop-and-ask específico para servicios de dominio:** si el scope es un domain service que orquesta múltiples agregados, antes de finalizar el reporte preguntar:

> "Las reglas aplicadas por este servicio podrían vivir en los agregados que coordina. ¿Las clasifico como hallazgos T Major (deuda estructural) o T Minor (oportunidad diferida)?"

Sin esta pregunta, los hallazgos DDD quedan subreportados como "oportunidades" cuando son deuda concreta.

---

## Paso 5 — Formato del reporte

```markdown
## Resumen

| Tier | Categoría | 🔴 Critical | 🟡 Major | 🔵 Minor | Total |
|---|---|---|---|---|---|
| 1 | A. Bugs de lógica | 0 | 1 | 0 | 1 |
| 1 | B. CQS / Efectos secundarios | | | | |
| 1 | D. Inmutabilidad / Estado mutable | | | | |
| ... | ... | | | | |
| 3 | T. Anemia de dominio | | | | |
| | **TOTAL** | **N** | **N** | **N** | **N** |

---

## Cross-check con historial (si existe AIResume/docs/revision-codigo/historial.md)

### Hallazgos resueltos verificados contra el código actual

| ID histórico | Condición original | Estado en código actual | Acción |
|---|---|---|---|
| RC-NNN | [descripción] | ✅ no presente / 🔴 REGRESIÓN | omitir / reportar con severidad +1 |

### Decisiones vigentes respetadas

| ID histórico | Decisión vigente | Alternativas descartadas | ¿Nueva evidencia objetiva? |
|---|---|---|---|
| DC-NNN | [resumen] | [alternativas] | ❌ no / ✓ sí |

### Discrepancias con historial

[Contradicciones entre lo que el historial dice y lo que se observa en el código actual.]

---

## Auditoría DDD táctica (tabla de Pasada 7)

| Agregado / Entidad | Propiedades leídas | Métodos llamados | R:M | Anemia |
|---|---|---|---|---|

## Discriminantes (tabla de Pasada 8)

| Enum | Archivos con switch | Record con nullable correlacionado | Polimorfismo candidato |
|---|---|---|---|

## Costo de extensión (Pasada 9)

| Extensión | Archivos | Clasificación |
|---|---|---|

## Onboarding cost (Pasada 10)
[métrica: N archivos distintos, N agregados/VOs referenciados para el use case principal]

## Auditoría de vocabulario (Pasada 11)

| Tipo | Nombre | Técnico? | Término sugerido |
|---|---|---|---|

Ratio dominio/técnico: X/Y

## Auditoría de VOs accesorios (tabla de Pasada 12)

| Tipo | Campos | Eventos? | Comandos/queries? | Proyecciones? | Puertos? | Call site derivado? | Veredicto |
|---|---|---|---|---|---|---|---|

## Apply hygiene (tabla de Pasada 13)

| Archivo | Clase | Método | Patrón # | Severidad |
|---|---|---|---|---|

## Pureza de capa de dominio (tabla de Pasada 14)

| Archivo de dominio | `using` prohibido | Categoría | Ruta sugerida |
|---|---|---|---|

---

## [Letra]. [Nombre de categoría]

### [Letra.n] [Título] [🔴 Critical / 🟡 Major / 🔵 Minor]

**Archivo:** `ruta:línea`
**Problema:** [qué hace vs qué debería]
**Impacto:** [consecuencia observable]
**Corrección sugerida:** [qué cambiar]
**Referencia:** [Connascence type / CUPID / SOLID / DDD / GoF / Fowler]
```

**Severidad:**
- **🔴 Critical** — datos incorrectos, pérdida silenciosa, seguridad, race condition, Apply impuro con throw.
- **🟡 Major** — diseño problemático que bloquea mantenibilidad/extensión.
- **🔵 Minor** — smell de estilo/legibilidad.

**Patrones sistémicos** al final si hay repetición multi-componente.

**Evaluación CUPID global** narrativa al final.

IDs `[Letra].[N]` para referencia por `/implementar-hallazgo`.

---

## Paso 6 — Guardar el reporte

`AIResume/RevisionCodigo.md` con encabezado:

```markdown
# Revisión de Código — [proyecto]

**Fecha:** [git log -1 --format=%ci]
**Rama:** [git branch --show-current]
**Scope:** [scope analizado]
**Marco:** SOLID + DDD tactical + Fowler smells + Connascence + CUPID + GoF patterns
```

---

## Paso 7 — Mostrar resumen al usuario

1. **Tabla ejecutiva** con conteo por tier y categoría.
2. **Tablas de evidencia** de Pasadas 7–14 (DDD táctica, discriminantes, vocabulario, VOs, Apply, pureza).
3. **Clasificación de hallazgos por tipo de acción:**
   - 🔴 **Corregibles con TDD** — A, C, Q, R con comportamiento incorrecto.
   - 🔧 **Refactors puros** — B, D, E, J, K, L, M, O, T — backed por tests existentes.
   - 🎨 **Decisión de diseño previa** — F, G con VO nuevo, S con patrón nuevo, T con cambio de API de agregado.
4. **Top 3 hallazgos** por severidad con su ID.
5. **Diagnóstico DDD:** si hay hallazgos T Major, resaltar como bloque:
   > "El scope tiene [N] reglas que deberían vivir en agregados. Esta es la brecha principal entre la declaración DDD-first del proyecto y su código."
6. **Próximo paso sugerido:** el hallazgo más crítico con su acción: `/implementar-hallazgo [Letra].[N]`.
