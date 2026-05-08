# CLAUDE.md

Este archivo provee instrucciones al agente AI (Claude Code) que trabaja en este repositorio.
Las instrucciones son **obligatorias** — el agente debe seguirlas incluso cuando no se soliciten explícitamente.

---

## Configuración del proyecto

> ⚙️ Esta sección es **específica de cada proyecto**. Actualizarla al iniciar un dominio nuevo.

```bash
# Build y tests
dotnet build [Proyecto].sln
dotnet test [Proyecto].sln
dotnet test [Proyecto].Dominio.Tests/[Proyecto].Dominio.Tests.csproj
dotnet test --filter FullyQualifiedName~Si_NombreDelTest

# Levantar infraestructura
docker-compose up -d --build
```

**Puertos estándar:** Commands API `8080` · Queries API `8090` · Commands MCP `7080` · Queries MCP `7090` · Aspire Dashboard `18888`

### Agregados del dominio

> Mantener este inventario actualizado. Es la fuente de verdad del estado de implementación.

| Aggregate | Carpeta | Estado |
|---|---|---|
| `[NombreAgregado]` | `Dominio/[Carpeta]/` | ⬜ Pendiente / 🔄 En progreso / ✅ Completo |

### Domain Services

| Service | Carpeta | Estado |
|---|---|---|
| `[NombreServicio]` | `Dominio/Compartidos/Services/[Carpeta]/` | ⬜ / 🔄 / ✅ |

### Enums y VOs compartidos

> Documentar aquí los enums y VOs que aplican transversalmente a todo el dominio.

| Tipo | Valores / Contenido | Descripción |
|---|---|---|
| `[NombreEnum]` | `ValorA`, `ValorB`, `ValorC` | [qué representa en el dominio] |
| `[NombreVO]` | [campos] | [qué concepto encapsula] |

---

## Arquitectura

Solución .NET 10 que implementa **DDD + Event Sourcing + CQRS** sobre **CritterStack**
(Marten/PostgreSQL como event store, Wolverine como mediator/bus).
RabbitMQ Outbox/Inbox para mensajería entre servicios.

### Stack tecnológico

| Capa | Tecnología |
|---|---|
| Event Store | Marten sobre PostgreSQL |
| Mediator / Bus | Wolverine |
| Messaging | RabbitMQ (Outbox/Inbox) |
| API | ASP.NET Core Minimal API con Carter |
| Integration | gRPC (Grpc.Net) |
| AI Integration | Model Context Protocol (MCP) |
| Tests | xUnit v3 + AwesomeAssertions + TestContainers |
| Observabilidad | Serilog + OpenTelemetry + Aspire Dashboard |

### Project Layout

| Proyecto | Rol |
|---|---|
| `*.Dominio` | Agregados, comandos, eventos, handlers, VOs, excepciones. **Zero dependencias de infra.** |
| `*.Dominio.Tests` | Tests unitarios. Given/When/Then vía `CommandHandlerAsyncTest<T>`. |
| `*.Consultas` | Proyecciones Marten + query handlers. |
| `*.Consultas.Tests` | Tests de proyección con Marten daemon + PostgreSQL TestContainers. |
| `*.Comandos.API` | ASP.NET Core Minimal API (Carter) para escrituras. |
| `*.Consultas.API` | ASP.NET Core Minimal API (Carter) para lecturas. |
| `*.Comandos.Grpc` / `*.Consultas.Grpc` | Servicios gRPC para integración con otros bounded contexts. |
| `*.Comandos.MCP.Server` / `*.Consultas.MCP.Server` | Model Context Protocol servers para integración con agentes AI. |
| `*.Contratos` | Assembly marker (`IContratosAssemblyMarker`) para descubrimiento de mensajes Wolverine. |
| `*.Infraestructura` | Serilog, OpenTelemetry, health checks. |
| `*.AcceptanceTests` | E2E con `WebApplicationFactory` + TestContainers. |
| `*.Seed` | Console app para precarga de configuración inicial del dominio. |
| `*.Seed.Tests` | Tests de integración del seed con PostgreSQL TestContainers. |

---

## Restricción crítica — Marten + polimorfismo en eventos

**PROHIBIDO** colocar tipos abstractos, interfaces o jerarquías polimórficas como **campos** dentro de records de evento.

Los eventos de Marten son historia inmutable en JSONB. Un tipo abstracto como campo genera un discriminador `$type` que:
- Queda **permanentemente bakeado** en streams históricos de PostgreSQL.
- **No puede rescatarse** con `EventUpcaster<TOld, TNew>` — ese mecanismo opera al nivel del evento completo, no de sub-campos.
- Falla silenciosamente si JSONB reordena propiedades (`AllowOutOfOrderMetadataProperties` no está configurado en este stack).
- Acopla nombres de clases C# a historia inmutable — renombrar rompe todos los streams históricos.

**Regla:** los campos de un evento deben ser primitivos (`Guid`, `string`, `decimal`, `DateTime`, `bool`), enums, o value objects **planos** sin herencia.

```csharp
// ✅ Evento plano — Apply() convierte primitivos → tipo rico en dominio
public record EstadoActualizado(EstadoEntidad Estado, DateTime FechaActualizacion) : EntidadEvents;

// ✅ Discriminated Union solo en la entidad de dominio, no en el evento
public record Condicion(..., EfectoAplicacion Efecto, ...); // EfectoAplicacion es la DU

// ❌ Tipo abstracto como campo de evento
public record EstadoActualizado(IEstado Estado) : EntidadEvents;
public record CondicionAgregada(EfectoAplicacion Efecto) : EntidadEvents; // EfectoAplicacion abstracto
```

El patrón `abstract record XxxEvents { private XxxEvents(){} public record Concreto : XxxEvents; }` es correcto
— Marten solo serializa los concretos y no introduce discriminadores.

---

## Principios de desarrollo

### Razonar el propósito antes de ejecutar

Una solicitud del usuario es una **hipótesis de solución**, no una orden literal. Antes de escribir código:

1. Identificar el **objetivo de negocio**: qué problema resuelve, qué comportamiento observable debe existir al final.
2. Distinguir **hechos** (bug, regla, comportamiento esperado) de **forma propuesta** (archivos, métodos, tipos sugeridos).
3. Contrastar la forma literal contra los estándares de este archivo + memorias del proyecto.
4. Cuando la forma literal viola un estándar: **proponer la forma idiomática**. Usar `AskUserQuestion` si hay tradeoff genuino; aplicar directamente si el estándar es categórico. Nunca ejecutar la violación silenciosamente.

El comando `/implementar` formaliza este flujo. Para tareas no triviales, preferirlo sobre la ejecución directa.

---

### SOLID

Principios aplicados concretamente al stack DDD + ES:

**S — Single Responsibility Principle**
Una clase tiene una razón para cambiar. Un agregado protege sus invariantes; un handler orquesta la persistencia; un endpoint traduce HTTP. No mezclar.

```csharp
// ❌ handler que además valida reglas de dominio
public async Task HandleAsync(AgregarProducto command, CancellationToken ct)
{
    if (command.Precio < 0) throw new Exception("Precio inválido"); // ← regla de dominio en el handler
    var catalogo = await eventStore.LoadAsync<CatalogoDProductos>(command.CatalogoId, ct);
    catalogo.AgregarProducto(command.Codigo, command.Precio);
    await eventStore.SaveChangesAsync(ct);
}

// ✅ la regla vive en el agregado
public async Task HandleAsync(AgregarProducto command, CancellationToken ct)
{
    var catalogo = await eventStore.LoadAsync<CatalogoDProductos>(command.CatalogoId, ct)
        ?? throw new AgregarProductoException(DomainExceptionType.NotFound, $"No existe catálogo '{command.CatalogoId}'.");
    catalogo.AgregarProducto(command.Codigo, command.Precio); // validación dentro del método
    await eventStore.SaveChangesAsync(ct);
}
```

**O — Open/Closed Principle**
Agregar un nuevo tipo de negocio no debe requerir modificar handlers existentes. Si agregar un nuevo `FactorDeTarifa` o un nuevo `Efecto` obliga a editar handlers en tres lugares → el diseño no es extensible. Candidato a polimorfismo o tabla de decisión.

**L — Liskov Substitution Principle**
Los subtipos deben ser sustituibles por su tipo base sin alterar el comportamiento. En este stack:
- Todos los agregados heredan `AggregateRoot` y deben comportarse igual respecto al ciclo de vida (uncommitted events, Apply).
- Los eventos concretos `XxxEvents.Concreto` son sustituibles por el tipo base abstracto `XxxEvents`.
- Un `Apply()` en un subtipo no debe lanzar donde el base no lanza.

```csharp
// ❌ LSP violado — el subtipo lanza donde el base no lanza; rompe replay de streams válidos
public class ProductoDigital : Producto
{
    public override void Apply(ProductoEvents.PrecioModificado evento)
    {
        if (evento.NuevoPrecio > 999)
            throw new InvalidOperationException("precio fuera de rango");
    }
}

// ✅ el polimorfismo de estado va como campo interno — los agregados no tienen subclases
public sealed class Producto : AggregateRoot
{
    private TipoProducto _tipo; // enum interno, no jerarquía de clases
}
```

**I — Interface Segregation Principle**
Las interfaces deben ser pequeñas y específicas. Los ports en dominio (p.ej. `IProveedorDeConfiguracion`, `IVerificadorDeIdempotencia`) deben exponer exactamente lo que el dominio necesita — no un contrato genérico de repositorio con 15 métodos de los cuales solo se usan 2.

```csharp
// ❌ interfaz genérica en dominio
public interface IRepositorio<T>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct);
    Task SaveAsync(T entity, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    // ... 10 métodos más
}

// ✅ port específico para lo que el dominio necesita
public interface IVerificadorDeIdempotencia
{
    Task<bool> ExisteAsync(string transaccionId, CancellationToken ct);
}
```

**D — Dependency Inversion Principle**
El dominio depende de abstracciones (ports); la infraestructura implementa esas abstracciones (adapters). Nunca al revés.
- El dominio define `IProveedorDeX` en su capa.
- `Dominio.Store` / `Infraestructura` implementa `ProveedorDeX`.
- El dominio no importa namespaces de infra (ver sección *Pureza de capa de dominio*).

---

### DRY — Don't Repeat Yourself

Buscar si ya existe algo equivalente **antes** de escribir lógica nueva:

- Validaciones de entidad → extraer como método estático en el record para compartirlo entre agregado e inicializador.
- Predicados repetidos en varios `Apply` → extraer helper privado.
- Helpers reutilizables entre comandos → patrón `ObtenerXxxOLanzar` con `Func<DomainExceptionType, string, DomainException>`.
- Construcción de objetos esperados repetida en tests → helpers en la clase base abstracta.

---

### CUPID Predictable

El comportamiento de un método debe corresponder a su nombre y firma:
- Un getter no muta.
- Un `Apply` no valida.
- Una query no escribe.
- Si un método puede lanzar, el nombre debe anticiparlo — separar en `ValidarXxx` + acción pura.

---

### Tell, Don't Ask / Ley de Demeter

Los objetos deben actuar, no exponer estado para que el llamador decida. Solo hablar con vecinos inmediatos:

```csharp
// ❌ el servicio decide por el agregado
if (pedido.Lineas.Any(linea => linea.Estado == EstadoLinea.Pendiente))
    pedido.Confirmar();

// ✅ el agregado decide
pedido.ConfirmarSiTienePendientes(); // el método interno verifica y actúa

// ❌ cadena de 3 niveles
var moneda = pedido.Emisor.Cuenta.MonedaBase;

// ✅ encapsular en el nivel correcto
var moneda = pedido.MonedaBase(); // el pedido lo sabe
```

La lógica que reproduce una condición que el objeto ya conoce está en el lugar equivocado.
En el **read side**, exponer propiedades es correcto. En el **write side**, el comportamiento va dentro del agregado.
En LINQ y fluent builders, la cadena es intencional y no es violación.

---

### Fail Fast

Validar en el borde, fallar inmediatamente, nunca acumular estado inválido:

- Guard clauses **al inicio** del método, no al final.
- Nunca swallow exceptions: si se captura, se relanza o se convierte con contexto.
- `?? defaultValue` solo cuando null es un estado válido de negocio.
- `FirstOrDefault()` seguido de uso sin null-check es una bomba silenciosa → usar `ObtenerXxxOLanzar`.

```csharp
// ❌ el null explota más tarde
var entrada = _entradas.FirstOrDefault(e => e.Id == id);
entrada.Cerrar(fecha); // NullReferenceException en producción

// ✅ falla inmediato con contexto
var entrada = ObtenerEntradaOLanzar(id, (tipo, msg) => new CerrarEntradaException(tipo, msg));
entrada.Cerrar(fecha);
```

---

### Primitive Obsession

El mismo primitivo con las mismas validaciones en ≥3 lugares es un Value Object sin nombre:

- `string codigo` con validación de no-nulo + longitud → VO con nombre de dominio.
- Parámetros que siempre viajan juntos → record con nombre de dominio.
- `bool activo` con más de dos transiciones posibles → enum o tipo con semántica.

---

### Command Query Separation (CQS)

Un método es un **comando** (muta estado, retorna void) o una **query** (retorna valor, no muta). Nunca ambas cosas.

```csharp
// ❌ retorna el ID y muta estado
public Guid AgregarProducto(string codigo, decimal precio)
{
    var id = Guid.CreateVersion7();
    RegistrarEvento(new ProductoAgregado(id, codigo, precio));
    return id; // ← mezcla query + command
}

// ✅ separados: el ID se genera fuera (en el handler o endpoint)
public void AgregarProducto(Guid id, string codigo, decimal precio)
    => RegistrarEvento(new ProductoAgregado(id, codigo, precio));
```

El ID siempre se genera en el **handler** o en el **endpoint**, no en el agregado.

---

### Inmutabilidad por defecto

- Los `record` son inmutables por construcción — preferirlos sobre `class` para VOs y entidades.
- El estado de los agregados muta **solo** dentro de `Apply()`, a partir de eventos ya ocurridos.
- Los eventos son historia inmutable — nunca se modifican, nunca se eliminan.
- Las colecciones expuestas desde agregados son `IReadOnlyList<T>` — nunca `List<T>` mutable pública.

---

### Sin código ruido

El código ruido no aporta información y actúa como interferencia:

| Patrón | Regla |
|---|---|
| Código comentado | **Prohibido.** Usar git para recuperar historial, no comentarios. |
| `// TODO:` sin owner ni fecha | **Prohibido.** Si es deuda real, registrarla en el backlog. |
| Variables `temp`, `aux`, `data`, `result`, `obj` | **Prohibido.** Nombrar por su rol de negocio. |
| Números o strings mágicos inline | **Prohibido.** Extraer a constante con nombre de dominio. |
| `using` importado pero no usado | **Prohibido.** El compilador avisa; resolverlo. |
| Comentarios que explican QUÉ hace el código | **Prohibido.** El código bien nombrado ya lo dice. Solo comentar el POR QUÉ cuando no es obvio. |
| Métodos privados llamados exactamente 0 veces | **Prohibido.** Dead code. Eliminar. |

---

### Tamaños máximos orientativos

| Unidad | Límite |
|---|---|
| Método público de negocio en un agregado | ≤ 15 líneas |
| Método privado helper | ≤ 20 líneas |
| Clase / agregado completo | ≤ 200 líneas |
| Parámetros por método | ≤ 4 (si son más → record con nombre) |

---

## Logging

Structured logging siempre: `logger.LogInformation("Msg {Param}", value)` — **nunca** interpolación de string.

| Nivel | Cuándo |
|---|---|
| `Information` | Inicio de command endpoints (con params clave) + segundo log en `Crear` con el ID generado. |
| `Debug` | Query endpoints y query handlers. |
| `LogWarning` | `DomainException` esperada (regla de negocio rechazada). |
| `LogError` | Excepción inesperada del sistema. |

**Inyección:**
- Endpoints Carter: `ILogger<T>` como parámetro del lambda — **no** en el constructor de la clase módulo.
- Query handlers: `ILogger<T>` en primary constructor.
- Domain command handlers: **sin logging** (`*.Dominio` = zero infra deps).

**Prohibido:** PII, contraseñas, tokens. Nunca loggear en loops — loggear antes/después con el conteo.

---

## Code Conventions

### Lenguaje
Todos los términos de dominio en **español** (commands, events, exceptions, test names, properties, lambda params).
El código de infraestructura que wrap tecnología externa (gRPC mappers, Carter modules) puede usar inglés si el término es propio de la tecnología.

### Formato y sintaxis
- **String interpolation**: siempre `$"..."`. Nunca concatenar con `+`.
- **File-scoped namespaces** y `ImplicitUsings` habilitados en todos los proyectos.
- **`Guid.CreateVersion7()`** — nunca `Guid.NewGuid()`. Version 7 es monotónico y sortable.
- **Primary constructors** para inyección de dependencias en handlers y servicios.
- **Collection expressions** (`[..coleccion, elemento]`) sobre `.Add()` / `.Concat()`.
- **Extension methods** con la sintaxis C# 14: `extension(IReadOnlyList<T> lista)` — no el patrón `static this`.

### Agregados
- Heredan `AggregateRoot`.
- Métodos de negocio **validan** y luego **pushean** a `_uncommittedEvents` vía `RegistrarEvento`.
- `Apply()` **actualiza estado únicamente** — sin validación, sin side effects.
- **Orden de miembros obligatorio**:
  1. Campos privados (`_lista`, `_uncommittedEvents`)
  2. Propiedades públicas
  3. Constructores
  4. Métodos públicos de negocio
  5. Métodos `internal` (si aplica)
  6. Métodos privados (ObtenerXxx, LanzarExcepcionSi, ValidarXxx, RegistrarEvento, ActualizarOAgregar)
  7. Métodos `public void Apply(Event)`

### Commands y Events
Nested `record` types dentro de un `abstract record` con constructor privado sin parámetros.

```csharp
public abstract record ProductoCommands
{
    private ProductoCommands() { }
    public record Crear(Guid Id, string Codigo, decimal Precio) : ProductoCommands;
    public record ModificarPrecio(Guid Id, decimal NuevoPrecio) : ProductoCommands;
}

public abstract record ProductoEvents
{
    private ProductoEvents() { }
    public record ProductoCreado(Guid Id, string Codigo, decimal Precio) : ProductoEvents;
    public record PrecioModificado(Guid Id, decimal NuevoPrecio, DateTime FechaCambio) : ProductoEvents;
}
```

### Exceptions
Una excepción por comando. Recibe `(DomainExceptionType tipo, string mensaje)`.

```csharp
public class CrearProductoException(DomainExceptionType tipo, string mensaje)
    : DomainException(tipo, mensaje);
```

Tipos disponibles: `BusinessRule | NotFound | InvalidData`.

**Mensajes de excepción:** campo + contexto de entidad + valor (interpolado si disponible) + constraint.

```csharp
// ✅
$"El precio ({precio}) no puede ser negativo."
$"No se encontró el producto con código '{codigo}'."
$"La vigencia de la entrada (hasta: {hasta:yyyy-MM-dd}) no puede ser anterior al inicio ({desde:yyyy-MM-dd})."

// ❌
"Precio inválido."
"No se encontró."
"Fecha incorrecta."
```

Los mensajes de `BusinessRule` deben responder "¿por qué es necesario este dato?" no solo "¿qué falta?".
En handlers siempre incluir el ID buscado interpolado en mensajes `NotFound`.

### Handlers
Primary constructor injection de `IEventStore`. Implementan `ICommandHandlerAsync<T>`. Flujo estándar: Load → call method → `SaveChangesAsync`.

```csharp
public class ModificarPrecioHandler(IEventStore eventStore)
    : ICommandHandlerAsync<ProductoCommands.ModificarPrecio>
{
    public async Task HandleAsync(ProductoCommands.ModificarPrecio command, CancellationToken ct)
    {
        var producto = await eventStore.LoadAsync<Producto>(command.Id, ct)
            ?? throw new ModificarPrecioException(DomainExceptionType.NotFound,
                $"No se encontró el producto '{command.Id}'.");
        producto.ModificarPrecio(command.NuevoPrecio);
        await eventStore.SaveChangesAsync(ct);
    }
}
```

### Entities y Value Objects
`record` posicional. Validación en property initializers — ninguna instancia inválida puede existir.
La validación de entidad usa `XxxException`; el agregado llama `ValidarXxx(params, factory)` estático (DRY) — el mismo método sirve tanto al agregado como a los tests que ejercen el inicializador directamente.

```csharp
public record CodigoProducto(string Valor)
{
    public string Valor { get; } = string.IsNullOrWhiteSpace(Valor)
        ? throw new ProductoException(DomainExceptionType.InvalidData,
            "El código del producto no puede ser nulo o vacío.")
        : Valor.Trim().ToUpperInvariant();
}
```

Los tests de VO ejercen los property initializers **directamente**, no solo a través del command handler:

```csharp
[Fact]
public void Si_CodigoEsNulo_Debe_LanzarExcepcion()
{
    var caller = () => new CodigoProducto(null!);
    caller.Should().Throw<ProductoException>()
        .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData);
}
```

### Parámetros de lambdas
Sustantivos en español que reflejen el tipo o rol — **nunca** una sola letra.

```csharp
// ✅
_entradas.Where(entrada => entrada.EstaVigente(fecha))
_productos.FirstOrDefault(producto => producto.Codigo == codigo)
_condiciones.Select(condicion => condicion.Efecto)

// ❌
_entradas.Where(e => e.EstaVigente(fecha))
_productos.FirstOrDefault(p => p.Codigo == codigo)
```

Excepciones admitidas: exception factory `(tipo, mensaje) =>`, assertion exclusions `miembro =>`.

---

### Vocabulario de dominio

Las clases, métodos públicos y VOs del dominio usan **lenguaje ubicuo del negocio**.

**Sufijos/verbos prohibidos en nombres de clases de dominio:**

| Prohibido | Alternativa |
|---|---|
| `Resolver*`, `Resolvedor*` | Nombre del concepto que resuelve: `ProveedorDeX` |
| `Convertidor*`, `Conversor*` | `TraductorDeX`, `NormalizadorDeX` |
| `Procesador*`, `Tramitador*` | Verbo de dominio: `EvaluadorDeX`, `CalculadorDeX` |
| `Gestor*`, `Manager*` | Nombre del aggregate que gestiona: `CatalogoDeX` |
| `Helper*`, `Util*` | Extraer a método del objeto dueño o a extension method nombrado |
| `Service*` sin calificar | `MotorDeCalculo`, `EvaluadorDeCondiciones` (nombre del concepto) |
| `Handler*` en dominio | Solo en `CommandHandlers/` donde es estructural; nunca como nombre de clase de negocio |

**Synonym drift prohibido.** No deben coexistir dos morfologías del mismo concepto en el scope
(ej. `Resolutor` vs `Resolvedor` — elegir **una** forma y mantenerla).
Si > 50% de los nombres del scope son técnicos en un proyecto DDD → refactor sistemático.

---

## Apply hygiene en agregados event-sourced

`Apply(...)` participa en tres escenarios donde su comportamiento debe ser **idéntico**:

1. **Append normal**: tras un command handler, el agregado emite eventos y los `Apply` sincronizan el estado en memoria antes de persistir.
2. **Live aggregation**: Marten reproduce todos los eventos del stream para reconstruir el estado actual. Cada `Apply` se ejecuta contra historia ya emitida.
3. **Rebuild de daemon**: rehidrata proyecciones y snapshots. Una excepción aquí pausa o mata el daemon.

**Patrones prohibidos en `Apply` / `Create`:**

| # | Patrón | Severidad | Razón |
|---|---|---|---|
| 1 | `throw` directo | 🔴 Critical | rompe replay de streams válidos |
| 2 | Helper que lanza: `Lanzar*`, `Validar*`, `Verificar*`, `*OLanzar` | 🔴 Critical | excepción indirecta en replay |
| 3 | Logging / trazas | 🟡 Major | side effect; duplicado en cada replay |
| 4 | `await` | 🟡 Major | `Apply` debe ser síncrono |
| 5 | IO: `HttpClient`, `IDocumentSession`, `IBus`, `File.` | 🟡 Major | no debe tocar mundo externo |
| 6 | Fuentes no deterministas: `DateTime.Now/UtcNow`, `Guid.NewGuid()`, `Guid.CreateVersion7()`, `Random` | 🟡 Major | la fecha/id viajan dentro del evento |
| 7 | Estado de runtime: `Environment.*`, `Thread.CurrentThread` | 🟡 Major | replay depende del entorno |

```csharp
// ✅ forma correcta
public void ModificarPrecio(decimal nuevoPrecio)
{
    if (nuevoPrecio < 0)
        throw new ModificarPrecioException(DomainExceptionType.InvalidData,
            $"El precio ({nuevoPrecio}) no puede ser negativo.");  // ← validación aquí
    RegistrarEvento(new ProductoEvents.PrecioModificado(Id, nuevoPrecio, DateTime.UtcNow));
}

public void Apply(ProductoEvents.PrecioModificado evento)
    => _precio = evento.NuevoPrecio;  // solo mutar estado, sin validar ni IO
```

**Excepción única:** `throw` defensivo por corrupción imposible del Event Store, con comentario inline que lo declare explícitamente. Sin ese comentario → 🔴 Critical sin atenuante.

---

## Pureza de capa de dominio

Los proyectos `*.Dominio.csproj` **no importan** namespaces de:

| Categoría | Namespaces prohibidos |
|---|---|
| Web / HTTP | `Microsoft.AspNetCore.*`, `System.Net.Http.*` |
| Persistencia / ORM | `Microsoft.EntityFrameworkCore.*`, `Marten.*` (excl. abstracciones puras), `Dapper.*` |
| Serialización | `System.Text.Json.*`, `Newtonsoft.Json.*`, `MessagePack.*` |
| Logging concreto | `Serilog.*`, `NLog.*`. `Microsoft.Extensions.Logging.*` **solo** en handlers/services con `ILogger<>` inyectado — prohibido en agregados, entidades y VOs |
| DI containers | `Microsoft.Extensions.DependencyInjection.*` (solo en `*Module.cs` / `ServiceCollectionExtensions.cs`) |
| Mensajería | `Wolverine.*` (excl. `Wolverine.Attributes`), `MassTransit.*`, `RabbitMQ.Client.*` |
| Capas superiores | `.API`, `.Infraestructura`, `.Grpc`, `.MCP`, `.Web` y sus sub-namespaces |
| Read side desde write side | `*.Consultas.*`, `*.Queries.*` |

**Excepciones aceptadas:** `Cosmos.EventSourcing.Abstractions`, `Marten.Schema.Identity`, `Wolverine.Attributes`.

**Patrón correcto:** definir interfaz/port en dominio, implementación concreta en infraestructura.

---

## Anti-patrones DDD — glosario compartido

| ID | Nombre | Detección rápida | Corrección |
|---|---|---|---|
| **T-Propiedad decisora** | servicio lee propiedad del agregado para decidir | `if (agg.Cosas.Any(...))` afuera del agregado | método semántico: `agg.TieneCosasPendientes()` |
| **T-Colección interna consultada** | LINQ externo sobre colección expuesta | `agg.Items.Where(...).Any(...)` | `agg.ItemsVigentesEn(fecha)` |
| **T-Regla reimplementada** | servicio compone lógica que el agregado ya conoce | método estático con `agg.X`, `agg.Y` | método de instancia en el agregado |
| **T-Discriminated Union Disguised** | `Kind: Enum` + nullable correlacionado | `record(Kind, T? X)` con `X != null` solo si `Kind == Val` | jerarquía polimórfica en dominio (no en evento) |
| **T-Identity Surrogate** | VO que encapsula la tupla identidad de otro agregado | `new VO(otro.X, otro.Y)` con todos los args del mismo origen | predicado `agg.EsParaXxx(...)` con campos propios |
| **T-Repository camuflado** | `IResolutor*` / `IObtenedor*` que carga agregados por criterio | nombre técnico con responsabilidad de repositorio | renombrar a `IXxxRepositorio` |
| **T-Transaction Script disfrazado** | domain service como secuencia lineal sobre bag de estado | mutaciones imperativas sobre parámetro | delegar a métodos de agregados |
| **T-God Parameter Object** | `Contexto/Request/Scope` pasado a N colaboradores con <50% overlap | parameter bag sin concepto de dominio | cada colaborador recibe solo lo que usa |
| **T-Synonym Drift** | dos morfologías del mismo concepto coexisten en el scope | `Resolutor` y `Resolvedor` en el mismo módulo | unificar a una forma |
| **T-Vocabulario técnico** | clase de dominio con verbo genérico | `Procesador*`, `Gestor*`, `Helper*` | término del lenguaje ubicuo |
| **F-Apply impuro** | `Apply` con `throw`/IO/no determinismo | ver tabla Apply hygiene | mover validación al método de negocio |
| **F-Dependencia de capa rota** | `using` prohibido en dominio | ver tabla Pureza de capa | port en dominio, adapter en infra |
| **G-Estructura de lookup sobredimensionada** | `Dictionary`/`HashSet` con N pequeño sin documentar | volumetría ≤ ~50, sin doc | `List` + `FirstOrDefault(predicado)` |
| **E-Validación duplicada cross-layer** | mismo mensaje/regex/constante en ≥2 capas | doc + API + dominio reimplementan la misma regla | single source of truth en dominio |

**Filtro anti-falso-positivo para T:** el método propuesto debe usar al menos un campo propio (`this.X`). Si todo llega como parámetro externo y el cuerpo no toca ningún campo del objeto, es una función estática disfrazada — no reportar como anemia.

---

## Testing Pattern

Cada comando tiene su clase de test en `*.Dominio.Tests/[Agregado]/Comandos/`.

```csharp
// Clase base abstracta por agregado
public abstract class ProductoCommandHandlerAsyncTest<T> : CommandHandlerAsyncTest<T>
    where T : ProductoCommands
{
    protected Guid ProductoId => GuidAggregateId;

    // Helpers de estado previo con defaults representativos
    protected CatalogoEvents.ProductoAgregado ProductoAgregado(
        string codigo = "PROD-X",
        decimal precio = 100m)
        => new(ProductoId, codigo, precio);
}

// Clase de test por comando
public class ModificarPrecioTests : ProductoCommandHandlerAsyncTest<ProductoCommands.ModificarPrecio>
{
    protected override ICommandHandlerAsync<ProductoCommands.ModificarPrecio> Handler =>
        new ModificarPrecioHandler(EventStore);

    [Fact]
    public async Task Si_ProductoExisteYPrecioEsPositivo_Debe_EmitirPrecioModificado()
    {
        Given(ProductoAgregado());
        await WhenAsync(new ProductoCommands.ModificarPrecio(ProductoId, 200m),
            TestContext.Current.CancellationToken);
        Then(new ProductoEvents.PrecioModificado(ProductoId, 200m, /* fecha */));
        And<Producto, decimal>(producto => producto.Precio, 200m);
    }

    [Fact]
    public async Task Si_ProductoNoExiste_Debe_LanzarExcepcionNotFound()
    {
        var caller = () => WhenAsync(new ProductoCommands.ModificarPrecio(ProductoId, 200m),
            TestContext.Current.CancellationToken);
        await caller.Should().ThrowAsync<ModificarPrecioException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.NotFound)
            .WithMessage($"*'{ProductoId}'*");
    }
}
```

### Reglas de naming

- **Separadores snake_case**: `Si`, `Debe`, `NoDebe` son los **únicos**. X e Y son PascalCase sin underscores.
- **X**: entidad completa + estado exacto. `CatalogoDeProductosActivo`, no `Catalogo`.
- **Y**: outcome observable. `EmitirPrecioModificado`, no `Funcionar`.
- **Lenguaje del dominio en X y Y, no técnico.** El nombre debe expresar la regla/comportamiento del dominio que el test protege, no la mecánica del SUT:
  - **Y usa verbos del dominio**, no del lenguaje: `Debe_VerificadorReportarComoExistente` mejor que `Debe_RetornarTrue`; `Debe_LanzarExcepcionBusinessRule` mejor que `Debe_Lanzar`. Incluir siempre el `DomainExceptionType` (`InvalidData`/`BusinessRule`/`NotFound`) cuando Y es lanzar.
  - **X refleja precondiciones del dominio**, no implementación: `Si_HayTerceroOperableConLaIdentificacion` mejor que `Si_ExisteTerceroConLaIdentificacion` (incluir matices del contrato como "operable" excluye Abortado); `Si_HayTerceroRegistradoConLaIdentificacion` mejor que `Si_HashSetContieneIdentificacion`.
  - **Identificar el SUT cuando hay ambigüedad**: si en el archivo conviven tests del agregado y de un puerto, incluir el actor en Y (`VerificadorReportarComo...`) ayuda al lector a saber qué se está probando.
  - **Simetría positivo/negativo**: usar la misma estructura X/Y intercambiando solo la condición. Ej. `Si_HayTerceroOperableConLaIdentificacion_Debe_VerificadorReportarComoExistente` ↔ `Si_NoHayTerceroOperableConLaIdentificacion_Debe_VerificadorReportarComoNoExistente`.

```
✅ Si_ProductoExisteYPrecioEsPositivo_Debe_EmitirPrecioModificado
✅ Si_ProductoNoExiste_Debe_LanzarExcepcionNotFound
✅ Si_PrecioEsCeroExacto_Debe_EmitirPrecioModificado      ← test del límite exacto
✅ Si_PrecioEsNegativo_Debe_LanzarExcepcionInvalidData
✅ Si_HayTerceroOperableConLaIdentificacion_Debe_VerificadorReportarComoExistente   ← X y Y en lenguaje del dominio

❌ Si_Precio_Es_Positivo_Debe_Funcionar                   ← underscores en X, Y genérico
❌ Si_DatosValidos_Debe_EmitirEvento                      ← X ambiguo
❌ Si_ProductoNoExiste_Debe_Lanzar                        ← Y sin tipo de excepción
❌ Si_ExisteTerceroConIdentificacion_Debe_RetornarTrue    ← Y técnico (RetornarTrue), X sin matices del contrato (Operable)
```

### Reglas de testing

- xUnit v3 + AwesomeAssertions.
- **`TestContext.Current.CancellationToken`** en todos los calls async. Nunca omitir, nunca usar `default`.
- **TDD obligatorio**: tests en rojo antes de implementar. Verificar `dotnet test --filter` está rojo antes de implementar.
- **`[Theory]` con un solo `[InlineData]`**: prohibido. Usar `[Fact]` con el valor hardcodeado.
- **`Then()` siempre requiere `And<>()`** para verificar estado del agregado. Tests de excepción (sin `Then()`) están exentos.
- **Sin dead code**: todo campo/comportamiento ejercido por al menos un test. `Apply()` es la única excepción (framework lo invoca por reflexión).
- **Datos agnósticos**: constantes abstractas (`"PROD-X"`, `"CAT-A"`) en lugar de nombres de dominio reales. Valores esperados derivados de constantes.
- **Cobertura de mutantes**: activador de la regla + valor exacto del límite (mata `<` → `<=`) + caso que no activa + solo los afectados se ven impactados.
- **Assertions de colección**: `And<Agregado, IReadOnlyList<T>>(agg => agg.Lista, [elem1, elem2])`. Nunca acceso por índice ni `.Count.Should().Be(N)`.
- **Mensajes de excepción**: verificar con `.WithMessage($"*'{id}'*")` usando wildcards para no over-specify.

---

## Acceptance Testing Pattern

E2E en `*.AcceptanceTests/` contra TestContainers (PostgreSQL + RabbitMQ).

### Estructura de archivos

```
AcceptanceTests/
└── [Agregado]/
    ├── Commands/
    │   └── [Entidad][Accion]Specifications.cs   ← un archivo por comando
    └── Helpers/
        ├── [Entidad]RequestBuilder.cs            ← fluent builder del request DTO
        └── [Entidad]RequestManager.cs            ← helpers de setup multi-paso
Compartidos/
└── HttpClientTestExtensions.cs                   ← ReadProblemDetailsAsync()
```

### ApiFactory y aislamiento

```csharp
public class ApiFactory : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:4-management").Build();
    // ...
    public async ValueTask InitializeAsync() { /* start containers, seed base data */ }
    public async Task ResetDataAsync() { /* reset + re-seed entre tests */ }
}

[CollectionDefinition("AcceptanceTests")]
public class AcceptanceTestsCollection : ICollectionFixture<ApiFactory>;

[Collection("AcceptanceTests")]
public class AcceptanceTest(ApiFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetDataAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

### Clase de test

```csharp
[Collection("AcceptanceTests")]
public class CrearProductoSpecifications(ApiFactory apiFactory) : AcceptanceTest(apiFactory)
{
    private readonly HttpClient _client = apiFactory.CommandsFactory.CreateClient();

    [Fact]
    public async Task Si_DatosDeCreacionSonValidos_Debe_Retornar201ConLocation()
    {
        var response = await _client.PostAsJsonAsync("/productos",
            new ProductoRequestBuilder().Build(), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Si_CodigoEsNulo_Debe_Retornar400ConDetalle()
    {
        var response = await _client.PostAsJsonAsync("/productos",
            new ProductoRequestBuilder().ConCodigo(null!).Build(), TestContext.Current.CancellationToken);
        var problem = await response.ReadProblemDetailsAsync(TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problem.Detail.Should().Contain("código");
    }
}
```

### Carter endpoint pattern

Un `ICarterModule` por endpoint. El endpoint genera el ID; el request DTO no lo lleva.

```csharp
app.MapPost("/productos", async (ICommandRouter router, ProductoRequest.Crear request, CancellationToken ct) =>
{
    var id = Guid.CreateVersion7();
    await router.InvokeAsync(new ProductoCommands.Crear(id, request.Codigo, request.Precio), ct);
    return Results.Created($"/productos/{id}", id);
}).DisableAntiforgery();
```

- **`Crear`** → `Results.Created($"ruta/{id}", id)`.
- **Mutaciones** → `Results.NoContent()`.
- **`.DisableAntiforgery()`** en todos los endpoints.

### Request DTOs (capa API)

Abstract record sellado en `*.Comandos.API/Requests/`. Nested records para cada comando. Capa separada de los comandos de dominio — los request DTOs no son comandos y no deben heredar de ellos.

```csharp
public abstract record ProductoRequest
{
    private ProductoRequest() { }
    public record Crear(string Codigo, decimal Precio) : ProductoRequest;
    public record ModificarPrecio(decimal NuevoPrecio) : ProductoRequest;
}
```

### Request Builders y Managers

**Builder**: un builder por entidad raíz con defaults representativos y métodos `Con*(val)` fluent que retornan `this`. Cada builder en su propio archivo.

```csharp
public class ProductoRequestBuilder
{
    private string _codigo = "PROD-X";
    private decimal _precio = 100m;

    public ProductoRequestBuilder ConCodigo(string codigo) { _codigo = codigo; return this; }
    public ProductoRequestBuilder ConPrecio(decimal precio) { _precio = precio; return this; }
    public ProductoRequest.Crear Build() => new(_codigo, _precio);
}
```

**Manager**: métodos estáticos que encapsulan secuencias HTTP repetidas entre tests (crear prerequisito antes de probar un comando dependiente).

### Error handling — ProblemDetails (RFC 7807)

| `DomainExceptionType` | HTTP Status |
|---|---|
| `NotFound` | `404 Not Found` |
| `BusinessRule` | `422 Unprocessable Entity` |
| `InvalidData` | `400 Bad Request` |
| Cualquier otra | `500 Internal Server Error` |

---

## Refactoring Standards

### Flujo de método público en agregado (SRP)

```csharp
public void ModificarDescripcion(string nuevaDescripcion)
{
    var producto = ObtenerProductoOLanzar(Id,
        (tipo, msg) => new ModificarDescripcionException(tipo, msg));
    LanzarExcepcionSiProductoInactivo(producto,
        (tipo, msg) => new ModificarDescripcionException(tipo, msg));
    RegistrarEvento(new ProductoEvents.DescripcionModificada(Id, nuevaDescripcion));
}
```

### Convención de helpers privados

| Propósito | Patrón | Retorno |
|---|---|---|
| Buscar o lanzar NotFound | `ObtenerXxxOLanzar(id, factory)` | entidad (nunca null) |
| Validar existencia sin usar retorno | `LanzarExcepcionSiXxxNoExiste(id, factory)` | `void` |
| Validar estado inactivo | `LanzarExcepcionSiXxxInactivo/Inactiva(entidad, factory)` | `void` |
| Validación condicional compleja | `ValidarXxxSiAplica(params, factory)` | `void` |
| Persistir evento | `RegistrarEvento(@event)` | `void` — siempre este nombre |
| Upsert en lista interna | `ActualizarOAgregar<T>(lista, predicate, elemento)` | `void` |

> `ObtenerXxxOLanzar` solo cuando el retorno se usa. Si solo se valida existencia → `LanzarExcepcionSiXxxNoExiste` (`void`). Nunca descartar el retorno de `ObtenerXxxOLanzar`.

### Exception factory pattern

```csharp
private Producto ObtenerProductoOLanzar(Guid id,
    Func<DomainExceptionType, string, DomainException> crearExcepcion)
    => _productos.FirstOrDefault(p => p.Id == id)
       ?? throw crearExcepcion(DomainExceptionType.NotFound,
           $"No se encontró el producto '{id}'.");

private static void LanzarExcepcionSiProductoInactivo(Producto producto,
    Func<DomainExceptionType, string, DomainException> crearExcepcion)
{
    if (!producto.Activo)
        throw crearExcepcion(DomainExceptionType.BusinessRule,
            $"El producto '{producto.Codigo}' está inactivo y no puede modificarse.");
}
```

### Estructuras de lookup — volumetría justifica la elección

`Dictionary<K,V>` / `HashSet<T>` se justifican cuando:
1. **N esperado > ~50** documentado explícitamente, **o**
2. La clave `K` es un concepto de dominio con peso propio, no un surrogate de identidad.

**Por defecto:** `IReadOnlyList<V>` + `FirstOrDefault(predicado)`.

Si la clave `K` se construye con todos los campos del mismo agregado origen, es **T-Identity Surrogate** — eliminar `K` y exponer predicado en el agregado (`agg.EsParaXxx(...)`).

Si la volumetría no está documentada: registrar como 🔵 Minor — *"volumetría no documentada; verificar antes de aceptar el `Dictionary`"* — y no asumir que el índice está justificado.

### Validación cross-layer — single source of truth

El mismo mensaje/regex/constante **no debe aparecer en ≥2 capas**. La validación vive en el property initializer del VO o en el método del agregado. Las capas externas consumen el resultado vía middleware → ProblemDetails.
Tests pueden replicar mensajes para verificar el contrato (Minor — acoplamiento aceptable).

### Tests — convenciones de refactoring

- Helpers de estado previo viven en la clase abstracta base con parámetros opcionales y defaults representativos.
- Helpers de construcción de eventos/entidades esperadas también en la base.
- Nunca repetir construcción inline en múltiples tests: extraer helper.
- Assertions de colección: siempre `BeEquivalentTo` contra lista tipada — nunca índices ni `.Count.Should().Be(N)`.

---

## Read Models — Proyecciones (Consultas)

Los streams usan `StreamIdentity.AsString` → IDs son `string` en toda la capa de consultas.

| Elemento | Tipo correcto |
|---|---|
| Read model `Id` | `string` (Guid.ToString()) |
| Projection 1 stream → 1 doc | `SingleStreamProjection<TReadModel, string>` |
| Projection N streams → 1 doc | `MultiStreamProjection<TReadModel, string>` |
| Primer evento | `Create(IEvent<TCreatedEvent>)` — retorna read model |
| Eventos posteriores | `Apply(TEvent, TReadModel)` — retorna `readModel with { ... }` |
| Apply con metadata del stream | `Apply(IEvent<TEvent>, TReadModel)` — cuando se necesita `@event.StreamKey` |
| Query handler | `IQueryHandler<TQuery, TResult>` (no Async) |
| DI en query handler | `IProjectionStore projectionStore` |
| Lifecycle | `ProjectionLifecycle.Async` en `ProyeccionesRegister` |

**Read model:** `record` con `{ get; init; }` — **no** posicional. Defaults para colecciones (`= []`) y strings (`= string.Empty`).

**MultiStreamProjection:** usa `IAsyncLifetime` con `ResetAllMartenDataAsync()` en `InitializeAsync()` de tests. **No** `ResetAllData()` — rompe el daemon.

**Custom grouper:** implementa `IJasperFxAggregateGrouper<string, IQuerySession>` (namespace `JasperFx.Events.Grouping`). En `Group()`: filtrar eventos sin key, cargar el stream vía `session.Events.FetchStreamAsync(streamKey)`, enrutar con `grouping.AddEvent(key, evento)`.

---

## Anatomy of a new feature (checklist)

**Dominio — TDD primero, tests en rojo antes de implementar:**

1. `*.Dominio.Tests/[Agregado]/Comandos/NuevoComandoTests.cs` — mínimo 3 tests (happy path, NotFound, BusinessRule).
2. `Exceptions/NuevoComandoException.cs`
3. `Commands/XxxCommands.cs` — agregar nested record al abstract record existente.
4. `Events/XxxEvents.cs` — agregar nested event record.
5. `[Agregado].cs` — método de negocio + `Apply(NuevoEvento)`.
6. `CommandHandlers/NuevoComandoHandler.cs`

**API — TDD primero, acceptance test en rojo antes del endpoint:**

7. `*.AcceptanceTests/[Agregado]/Commands/[Entidad][Accion]Specifications.cs` — happy path + errores esperados.
8. `*.Comandos.API/Requests/[Agregado]Request.cs` — agregar nested record al abstract record del request DTO.
9. `*.Comandos.API/Endpoints/[Entidad][Accion]Endpoint.cs` — Carter module.

---

