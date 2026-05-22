# Diagnóstico y Plan del Dominio — Datos de Referencia

**Fecha:** 2026-05-20
**Rama:** master
**Documentación analizada:** `Definiciones/compartido/datos-referencia/`
**Solución:** `Cosmos.DatosReferencia.sln`

---

## Resumen ejecutivo

| Estado | Cantidad |
|---|---|
| ✅ Implementados y verificados | Entidad `Moneda` + VO `CodigoMoneda` + tests · ítem #1 F1 (decisiones archivadas) · ítem #2 (Agregar Moneda con port `IDomainStore` y `TestDomainStore`) · ítem #3 (Consultar Moneda por código con Marten Id por convención; sin DTO) · ítem #4 (Listar monedas activas con `WhereAsync` en el port) |
| 🔄 Parcialmente implementados | `Moneda` con persistencia operativa pero sin queries (#3, #4), endpoints REST (#5, #6) ni comandos de Modificar/Inactivar/Activar (#7, #8, #9) |
| ⬜ Pendientes | 4 catálogos (Pais, DivisionTerritorial, TipoDocumentoIdentidad, TasaDeCambio) + seed + endpoints + sync TRM |
| 🔁 Regresiones detectadas | N/A (instrucción explícita: ignorar plan previo) |
| ❓ Gold-plating sin respaldo en documentación | 2 (MCP servers — decisión F1 los mantiene vacíos como rail futuro; método `Moneda.Modificar` sin consumidor) |
| **Total ítems del plan** | **30** |

### Naturaleza crítica del bounded context

Este servicio **no es Event Sourcing**. La sección 1.2 del alcance declara explícitamente:
"No reglas de negocio propias, no comportamiento propio, no procesos de negocio, no publica eventos de dominio".
La memoria de proyecto `feedback_modelado_crud` lo confirma: **CRUD sobre Marten**.

El modelo actual de `Moneda` ya respeta esto (record plano, no hereda `AggregateRoot`).
El wiring de `Comandos.API` está orientado a ES (`UsarWolverineParaComandos`, `IEventStore`, Outbox + RabbitMQ).
**Decisión F1 (2026-05-21):** ese wiring se mantiene **inerte** — los handlers CRUD usarán `IDocumentSession` por la sesión que Marten expone igual; el stack ES queda cableado para una eventual migración futura. Ver `feedback_wiring_comandos_api.md` y el ítem #1 abajo para el detalle de las 5 sub-decisiones.

---

## Sección 1 — Inventarios y diff

### Catálogos (entidades del dominio)

| Catálogo | Atributos modelo (A) | Atributos código (B) | Tests | Estado |
|---|---|---|---|---|
| `Pais` | codigo (ISO 3166-1 a2), nombre, monedaPrincipal, indicativoTelefonico (E.164), activo | — | 0 | ⬜ |
| `DivisionTerritorial` | codigo, nombre, paisCodigo, nivel, codigoSuperior, activo | — | 0 | ⬜ |
| `Moneda` | codigo (ISO 4217), nombre, decimales, activo | codigo, nombre, decimales, activo + método `Modificar` | 7 + 5 = 12 [Fact] | 🔄 |
| `TipoDocumentoIdentidad` | codigo, descripcion, paisCodigo (nullable), aplicaA, activo | — | 0 | ⬜ |
| `TasaDeCambio` | monedaOrigen, monedaDestino, valor, fechaVigencia, fuente | — | 0 | ⬜ |

### Validaciones

| ID | Validación | ¿Implementada? |
|---|---|---|
| V1 | País ISO 3166-1 alpha-2 | ⬜ |
| V2 | Pais.monedaPrincipal existe en Monedas (cross-catalog) | ⬜ |
| V3 | DivTerr.paisCodigo existe y activo | ⬜ |
| V4 | DivTerr.codigoSuperior existe y pertenece al mismo país | ⬜ |
| V5 | Moneda ISO 4217 | ✅ (en `CodigoMoneda`) |
| V6 | TipoDoc.paisCodigo existe y activo (excepto null) | ⬜ |
| V7 | TipoDoc unicidad (codigo, paisCodigo) | ⬜ |
| V8 | Tasa monedas existen | ⬜ |
| V9 | Tasa unicidad (origen, destino, fecha) | ⬜ |
| V10 | Registros referenciados no se eliminan — solo inactivar | ⬜ (necesita decisión de diseño D) |

### Operaciones de consulta (API)

| Operación | ¿Implementada? |
|---|---|
| Países: `GET /paises`, `GET /paises/{codigo}` | ⬜ |
| Divisiones: por país, por nivel, por código, por jerarquía | ⬜ |
| Monedas: `GET /monedas`, `GET /monedas/{codigo}` | ⬜ |
| Tipos doc: por país, por código+país, por aplicaA | ⬜ |
| Tasas: tasa vigente para (origen, destino, fecha) | ⬜ |

### Operaciones de administración (escritura — implícitas en Sección 6 alcance)

| Operación | ¿Implementada? |
|---|---|
| Agregar / Modificar / Inactivar / Activar de cada catálogo | ⬜ |
| Carga manual de TRM | ⬜ |

### Seed

| Contexto | Archivo JSON | Registros | Proyecto `Seed` | Cargado |
|---|---|---|---|---|
| Países (global) | `paises.json` | 195 | ⬜ no existe | ⬜ |
| Monedas (global) | `monedas.json` | 154 | ⬜ | ⬜ |
| Tipos doc (multi-país) | `tipos-documento-identidad.json` | 45 | ⬜ | ⬜ |
| DivTerr CO | `divisiones-territoriales-co.json` | 1.188 | ⬜ | ⬜ |
| DivTerr DO | `divisiones-territoriales-do.json` | 221 | ⬜ | ⬜ |
| DivTerr PA | `divisiones-territoriales-pa.json` | 108 | ⬜ | ⬜ |

### Sync (sincronización TRM)

| Mecanismo | Estado |
|---|---|
| Sync diaria Banco de la República (CO) | ⬜ — bloqueado por **PD1** |
| Sync diaria Banco Central RD | ⬜ — bloqueado por **PD1** |

### Integraciones

| Canal | Documentado | Implementado |
|---|---|---|
| REST consultas | ✅ | ⬜ |
| REST administración | ✅ implícito (Sección 6 alcance) | ⬜ |
| gRPC | no mencionado | no existe |
| MCP | no mencionado | proyectos vacíos (gold-plating ❓) |

### Gold-plating detectado

1. **`Cosmos.DatosReferencia.Comandos.MCP.Server` y `Cosmos.DatosReferencia.Consultas.MCP.Server`** — proyectos en la solución pero sin código de dominio. La documentación no menciona MCP. Decidir si se mantiene como rail futuro o se elimina.
2. **Wiring de Event Sourcing en `Comandos.API/Program.cs`** — `UsarWolverineParaComandos`, `AgregarMartenEventStore`, Outbox/Inbox/RabbitMQ. La memoria del proyecto (`feedback_modelado_crud`) dice CRUD sobre Marten. Tratado en ítem #1.
3. **Método `Moneda.Modificar`** sin handler/endpoint que lo invoque. Respaldado parcialmente por Sección 6 del alcance ("Toda modificación a los catálogos queda registrada con fecha y usuario"), pero no hay caller en el código actual — funcionalidad muerta hasta que se implementen los endpoints.

---

## Sección 2 — Plan de implementación

> Cada ítem es un comportamiento concreto con su ciclo TDD completo.
> Implementar cada ítem con `/implementar "[nombre del ítem]"`.
> El orden es por dependencia de dominio: ningún ítem puede escribirse en rojo hasta que todos los anteriores estén implementados.

---

### 1. Decidir wiring CRUD y refactorizar `Comandos.API` `[F1]` `[Con decisión de diseño]` ✅

**Explicación**
El wiring actual de `Cosmos.DatosReferencia.Comandos.API/Program.cs` está orientado a Event Sourcing (`UsarWolverineParaComandos`, `AgregarMartenEventStore`, Outbox/Inbox, RabbitMQ). La memoria de proyecto (`feedback_modelado_crud`) y el alcance (Sección 1.2) establecen que este bounded context es **CRUD sobre Marten**, no ES. El modelo actual de `Moneda` ya respeta esto (record plano, no hereda `AggregateRoot`). Sin resolver este mismatch, no se puede inyectar `IDocumentSession` en los command handlers ni en los query handlers.

**Respaldo en la documentación**
> "¿Reglas de negocio propias? No. ¿Comportamiento propio? No. ¿Procesos de negocio? No. ¿Publica eventos de dominio? No."
> — `definicion-alcance.md`, Sección 1.2

> "Datos de Referencia se modela CRUD sobre Marten, no Event Sourcing — D1=B; nada de agregados/eventos/handlers ES en este bounded context."
> — Memoria del proyecto `feedback_modelado_crud`

**Decisión aplicada (2026-05-21)**

Cinco decisiones tomadas en `AskUserQuestion` de la fase de plan. Quedan archivadas en la memoria del proyecto `feedback_wiring_comandos_api.md` como fuente de verdad para los ítems #2 al #30.

| # | Decisión | Resultado |
|---|---|---|
| Q1 | Wiring base de `Program.cs` | **Inerte como está.** Idéntico a `Cosmos.Terceros` (`UsarWolverineParaComandos` + RabbitMQ + Outbox + Inbox + Marten event store + Wolverine command router). Sin cambios. |
| Q2 | Pipeline de comandos | **Carter → `ICommandRouter` Wolverine → handler.** Misma forma que el `RegistrarTerceroEndpoint` de Cosmos.Terceros. |
| Q3 | Ubicación de los handlers | **`Cosmos.DatosReferencia.Dominio/[Catalogo]/CommandHandlers/`.** Refinado en #2 con patrón **port + adapter** (réplica de `ObligacionesPorPagar.Radicacion`): handlers dependen del port `IDomainStore` (en Dominio); el adapter `DomainStore` con `IDocumentSession` vive en proyecto separado `Cosmos.DatosReferencia.Dominio.Store/`. **La divergencia con CLAUDE.md desaparece** — el Dominio queda 100% puro. |
| Q4 | Proyectos MCP.Server | **Mantener vacíos** como rail futuro. |
| Q5 | Persistencia | **Documento Marten** (`session.Store(entidad)` + `SaveChangesAsync`). Las entidades son `record` planos sin `AggregateRoot`, sin eventos, sin `Apply()`. Cargar con `session.LoadAsync<T>(id, ct)`. |

**Diferencia operativa concreta vs Cosmos.Terceros (ES puro):**

| Aspecto | Cosmos.Terceros | Cosmos.DatosReferencia |
|---|---|---|
| Entidad raíz | `class Tercero : AggregateRoot` | `record Moneda(...)` plano |
| Persistir creación | `eventStore.StartStream(tercero)` | `session.Store(moneda)` |
| Persistir mutación | método del agregado emite evento | `session.Store(moneda with { ... })` |
| Cargar | `eventStore.LoadAsync<Tercero>(id, ct)` | `session.LoadAsync<Moneda>(codigo, ct)` |
| Eventos | `TerceroEvents.*` records | no existen |
| Inyección | `IEventStore` | `IDomainStore` (port en Dominio; adapter en `Dominio.Store/` delega a `IDocumentSession`) |

**Archivos modificados en este ítem:** ninguno en el repo. F1 es decisión registrada en memoria, no cambio de código.

- `Program.cs` queda inerte (Q1=A).
- `Cosmos.DatosReferencia.Dominio.csproj` agregará la referencia a Marten cuando el ítem #2 introduzca el primer handler.
- `ApiFactory.cs` mantiene `ResetAllData()` a propósito — funciona para CRUD documental y deja la puerta abierta a una migración a ES en el futuro.
- Proyectos MCP.Server intactos.

**Activaciones pendientes (para el ítem #2):** ✅ **resueltas en #2**
- ~~Agregar Marten a `Cosmos.DatosReferencia.Dominio.csproj`.~~ → No fue necesario. El patrón port + adapter de Radicacion mantiene Marten **fuera** del Dominio.
- ~~Decidir scaffolding de tests del dominio.~~ → `TestDomainStore` fake in-memory (réplica de Radicacion). Sin clase base por ahora.

**Habilita:** todos los ítems siguientes (2 al 30).
**Depende de:** nada.

---

### 2. `Moneda` — Persistir documento Marten al agregar `[F1]` `[Directamente implementable]` ✅

**Explicación**
La entidad `Moneda` existe y valida sus invariantes en el dominio. No hay handler/comando que la persista en Marten. Sin esto, ningún endpoint puede crear monedas y ningún test de integración puede usarlas como prerequisito.

**Respaldo en la documentación**
> "Identidad: codigo (ISO 4217, inmutable) | codigo string | nombre string | decimales integer | activo boolean (por defecto true)"
> — `especificacion-servicio.md`, Sección 2.3

> "Toda modificación a los catálogos queda registrada con fecha y usuario."
> — `definicion-alcance.md`, Sección 6 (Responsabilidades del sistema)

**Ejemplo**
- Acción: ejecutar `MonedaCommands.Agregar("USD", "Dólar estadounidense", 2)` contra un handler.
- Comportamiento actual: no existe el handler, el endpoint, ni el request DTO.
- Comportamiento esperado: la moneda queda persistida como documento Marten identificado por `Codigo.Valor`; consultable después.

**Test que define este comportamiento**
- Nombre: `Si_MonedaNoExisteYDatosSonValidos_Debe_PersistirMoneda`
- Qué verifica: tras invocar el handler, una `IDocumentSession.LoadAsync<Moneda>("USD")` retorna el documento con los valores enviados.
- Por qué falla (rojo): no existe `MonedaCommands.Agregar`, ni `AgregarMonedaHandler`, ni el `IDocumentSession.Store` wiring.
- Casos borde obligatorios:
  - `Si_MonedaYaExisteConElMismoCodigo_Debe_LanzarExcepcionBusinessRule` (idempotencia/unicidad por identidad ISO).
  - `Si_CodigoEsInvalidoISO4217_Debe_LanzarExcepcionInvalidData` (delega al VO existente).

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Dominio/Monedas/Commands/MonedaCommands.cs`: nested record `Agregar(string Codigo, string Nombre, int Decimales)` dentro de `abstract record MonedaCommands`.
- `Cosmos.DatosReferencia.Dominio/Monedas/CommandHandlers/AgregarMonedaHandler.cs`: handler que recibe `IDocumentSession`, valida unicidad con `session.LoadAsync<Moneda>(comando.Codigo)`, construye `new Moneda(new CodigoMoneda(...), nombre, decimales)` y hace `session.Store(moneda); await session.SaveChangesAsync(ct);`.
- `Cosmos.DatosReferencia.Dominio/Monedas/Exceptions/AgregarMonedaException.cs`: `AgregarMonedaException(DomainExceptionType tipo, string mensaje)`.

**Habilita:** #3, #4, #5, #6, #7 (todas las operaciones que requieren leer/escribir monedas), #13 (Pais necesita validar V2 contra monedas persistidas), #29 (TasaDeCambio V8 igual).
**Depende de:** ítem 1.

**Implementación aplicada (2026-05-21)**

Patrón port + adapter replicado de `ObligacionesPorPagar.Radicacion.Dominio.Store`:

- **Port en Dominio:** `Cosmos.DatosReferencia.Dominio/Compartidos/Store/IDomainStore.cs` con tres métodos (`AnyAsync`, `FirstOrDefaultAsync`, `SaveAsync`). Cero dependencia Marten.
- **Adapter en proyecto nuevo:** `Cosmos.DatosReferencia.Dominio.Store/` (Marten 8.23.0). Implementa `IDomainStore` delegando a `IDocumentSession`.
- **Wiring DI:** `services.AgregarDomainStore()` en `Comandos.API/Program.cs`. AcceptanceTests lo hereda transitivamente vía Comandos.API.
- **Handler:** `Dominio/Monedas/CommandHandlers/AgregarMonedaHandler.cs` recibe `IDomainStore`. Orden Fail Fast: construir `CodigoMoneda` (valida ISO) → `AnyAsync` (unicidad) → `new Moneda(...)` (valida nombre/decimales) → `SaveAsync`.
- **Comando:** `Dominio/Monedas/Commands/MonedaCommands.cs` con `abstract record MonedaCommands` + nested `Agregar(string Codigo, string Nombre, int Decimales)`.
- **Excepción:** `Dominio/Monedas/Exceptions/AgregarMonedaException.cs`.
- **Tests:** `Dominio.Tests/Monedas/Comandos/AgregarMonedaTests.cs` con los 3 casos del diagnóstico, usando `TestDomainStore` fake in-memory (`Dominio.Tests/Compartidos/Imitaciones/TestDomainStore.cs`).

**Refinamiento de F1:** la decisión Q3 de F1 había aceptado divergir con CLAUDE.md (Marten en Dominio). El patrón port + adapter de Radicacion **restauró** la pureza de capa — el Dominio quedó sin Marten. Memoria `feedback_wiring_comandos_api.md` actualizada con el refinamiento.

**Tests:** 15 dominio (12 previos + 3 nuevos) + 0 acceptance verdes.

---

### 3. `Moneda` — Query: consultar por código `[F1]` `[Directamente implementable]` ✅

**Explicación**
La documentación define la consulta "consultar por código" como operación expuesta. Hoy no hay query handler ni endpoint.

**Respaldo en la documentación**
> "Monedas | Listar activas, consultar por código | — / codigo"
> — `especificacion-servicio.md`, Sección 3.1

**Test que define este comportamiento**
- Nombre: `Si_MonedaExiste_Debe_RetornarMonedaConSusAtributos`
- Qué verifica: handler `ConsultarMonedaPorCodigoHandler` invocado con `"USD"` retorna `MonedaReadModel` con los campos esperados.
- Por qué falla (rojo): no existe el query handler ni la query.
- Casos borde obligatorios:
  - `Si_MonedaNoExiste_Debe_LanzarExcepcionNotFound`.

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Consultas/Monedas/Queries/ConsultarMonedaPorCodigo.cs`: `record ConsultarMonedaPorCodigo(string Codigo)`.
- `Cosmos.DatosReferencia.Consultas/Monedas/QueryHandlers/ConsultarMonedaPorCodigoHandler.cs`: handler `IQueryHandler<ConsultarMonedaPorCodigo, MonedaReadModel>` que usa `IQuerySession.LoadAsync<Moneda>(query.Codigo)`.
- `Cosmos.DatosReferencia.Consultas/Monedas/ReadModels/MonedaReadModel.cs`: `record MonedaReadModel { string Codigo; string Nombre; int Decimales; bool Activo; }`.

**Habilita:** #4 (listado), #6, #7 (validan existencia por código), endpoints REST GET de moneda.
**Depende de:** ítem 2.

**Implementación aplicada (2026-05-21)**

- Handler en `Cosmos.DatosReferencia.Consultas/Monedas/QueryHandlers/ConsultarMonedaPorCodigoHandler.cs` con `IQueryHandler<MonedaQueries.ConsultarPorCodigo, Moneda>`. Inyecta `IDomainStore` (no `IQuerySession` como decía el diagnóstico literal) — reusando el port del write side por simetría con #2. Retorna la entidad de Dominio directamente (sin `MonedaReadModel` DTO — descartado tras revisión: en CRUD doc la entidad ES el modelo de lectura, no hay proyección que justifique un tipo intermedio; el control de wire format JSON irá a la capa API).
- Query record en `Consultas/Monedas/Queries/MonedaQueries.cs`. Una exception por query en `Exceptions/ConsultarMonedaPorCodigoException.cs`.
- Tests en `Dominio.Tests/Monedas/Consultas/ConsultarMonedaPorCodigoTests.cs` con `TestDomainStore` fake (no TestContainer). 2 casos: happy path y NotFound. Total suite: **17 dominio**.
- **`Moneda` ahora declara `public string Id { get; init; } = Codigo.Valor;`** — patrón heredado de Localizadores de Cosmos.Impuestos para que Marten descubra el Id por convención. Sin `StoreConfiguration`, sin import Marten en Dominio.
- Wiring DI: `Consultas.API/Program.cs` ahora invoca `services.AgregarDomainStore()` con `ProjectReference` a `Dominio.Store`. `Dominio.Tests` agregó `ProjectReference` a `Consultas` para importar los tipos del read side.

---

### 4. `Moneda` — Query: listar activas `[F1]` `[Directamente implementable]` ✅

**Explicación**
La doc indica `Listar activas` como operación de consulta. Sin esto, los consumidores no pueden poblar dropdowns ni catálogos.

**Respaldo en la documentación**
> "Monedas | Listar activas, consultar por código"
> — `especificacion-servicio.md`, Sección 3.1

**Test que define este comportamiento**
- Nombre: `Si_HayMonedasActivasEInactivas_Debe_RetornarSoloActivas`
- Qué verifica: con 3 monedas persistidas (2 activas, 1 inactiva), el handler retorna 2 elementos.
- Por qué falla (rojo): no existe el query handler.
- Casos borde obligatorios:
  - `Si_NoHayMonedas_Debe_RetornarListaVacia`.

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Consultas/Monedas/Queries/ListarMonedasActivas.cs`: `record ListarMonedasActivas()`.
- `Cosmos.DatosReferencia.Consultas/Monedas/QueryHandlers/ListarMonedasActivasHandler.cs`: usa `session.Query<Moneda>().Where(m => m.Activo)`.

**Habilita:** endpoint REST GET de listado.
**Depende de:** ítem 2.

**Implementación aplicada (2026-05-21)**

- Handler en `Cosmos.DatosReferencia.Consultas/Monedas/QueryHandlers/ListarMonedasActivasHandler.cs` con `IQueryHandler<MonedaQueries.ListarActivas, IReadOnlyList<Moneda>>`. Una sola expresión-lambda: `=> domainStore.WhereAsync<Moneda>(moneda => moneda.Activo, ct)`. Retorna la entidad de dominio directamente (sin DTO — decisión revisada en #3).
- Query nested en `MonedaQueries.cs`: `public record ListarActivas() : MonedaQueries;` (consistencia con `ConsultarPorCodigo`).
- Cierra la activación pendiente desde #3: `IDomainStore.WhereAsync<T>(predicate)` agregado al port, al adapter `DomainStore` (delega a `session.Query<T>().Where(...).ToListAsync(ct)`), y al fake `TestDomainStore`. El port pasa de 3 a 4 métodos.
- Tests en `Dominio.Tests/Monedas/Consultas/ListarMonedasActivasTests.cs` con `TestDomainStore`: happy path (mix activas/inactivas → solo activas) + edge (sin monedas → lista vacía). Aserciones `BeEquivalentTo`/`BeEmpty` sin acceso por índice ni `.Count`.
- Sin nueva exception (lista vacía es resultado válido, no error).
- Sin nuevos `ProjectReference` ni wiring DI — todo se apoya en lo ya cableado en #2 y #3.

---

### 5. `Moneda` — Endpoints REST `GET /monedas` y `GET /monedas/{codigo}` `[F1]` `[Directamente implementable]`

**Explicación**
Los query handlers existen pero no están expuestos por HTTP. Sin endpoint, los consumidores externos no pueden invocarlos.

**Respaldo en la documentación**
> Sección 3 entera de `especificacion-servicio.md` define la API de consulta.

**Test que define este comportamiento (acceptance)**
- Nombre: `Si_MonedaExiste_Debe_Retornar200ConMonedaSerializada`
- Qué verifica: `await client.GetAsync("/monedas/USD")` retorna 200 + JSON con los campos del read model.
- Por qué falla (rojo): el módulo Carter no está mapeado.
- Casos borde obligatorios:
  - `Si_MonedaNoExiste_Debe_Retornar404`.
  - `Si_HayMonedasActivasEInactivas_Debe_ListadoRetornarSoloActivas`.

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Consultas.API/Endpoints/Monedas/ListarMonedasEndpoint.cs`: Carter module con `app.MapGet("/monedas", …)`.
- `Cosmos.DatosReferencia.Consultas.API/Endpoints/Monedas/ConsultarMonedaPorCodigoEndpoint.cs`: `app.MapGet("/monedas/{codigo}", …)`.
- `Cosmos.DatosReferencia.AcceptanceTests/Monedas/Queries/ConsultarMonedaSpecifications.cs`: specifications con `ApiFactory`.

**Habilita:** consumo desde otros bounded contexts.
**Depende de:** ítems 2, 3, 4.

---

### 6. `Moneda` — Endpoint REST `POST /monedas` `[F1]` `[Directamente implementable]`

**Explicación**
El handler de agregar existe (ítem 2) pero sin endpoint el administrador no puede invocarlo.

**Respaldo en la documentación**
> "Agregar monedas no estándar si el negocio lo requiere."
> — `anexo-estrategia-datos-referencia.md`, Sección 3 (Extend)

**Test que define este comportamiento (acceptance)**
- Nombre: `Si_DatosDeMonedaSonValidos_Debe_Retornar201ConLocation`
- Qué verifica: `POST /monedas` con `{ "codigo":"BTC", "nombre":"Bitcoin", "decimales":8 }` retorna 201 y `Location: /monedas/BTC`.
- Por qué falla (rojo): no existe el endpoint.
- Casos borde obligatorios:
  - `Si_MonedaYaExiste_Debe_Retornar422ConProblemDetails` (BusinessRule).
  - `Si_CodigoNoEsISO4217_Debe_Retornar400ConProblemDetails` (InvalidData).

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Comandos.API/Requests/MonedaRequest.cs`: `abstract record MonedaRequest` con nested `Crear(string Codigo, string Nombre, int Decimales)`.
- `Cosmos.DatosReferencia.Comandos.API/Endpoints/Monedas/AgregarMonedaEndpoint.cs`: `app.MapPost("/monedas", …)` que invoca el handler del ítem 2 y retorna `Results.Created($"/monedas/{codigo}", null)`.
- `Cosmos.DatosReferencia.AcceptanceTests/Monedas/Commands/AgregarMonedaSpecifications.cs`.

**Habilita:** carga manual y onboarding antes de tener seed (#11).
**Depende de:** ítems 1, 2.

---

### 7. `Moneda` — Comando + endpoint: Modificar `[F1]` `[Directamente implementable]`

**Explicación**
El método `Moneda.Modificar(nuevoNombre, nuevosDecimales)` existe en el dominio pero no hay handler/endpoint que lo invoque. El código del catálogo es inmutable (ISO 4217), solo nombre y decimales son editables.

**Respaldo en la documentación**
> "Toda modificación a los catálogos queda registrada con fecha y usuario."
> — `definicion-alcance.md`, Sección 6

> "El código debe ser ISO 4217 válido (3 letras mayúsculas). Inmutable."
> — `especificacion-servicio.md`, Sección 2.3

**Test que define este comportamiento**
- Nombre: `Si_MonedaExisteYNuevosDatosSonValidos_Debe_PersistirMonedaConValoresActualizados`
- Qué verifica: tras invocar el handler de modificar, el documento Marten tiene nombre/decimales nuevos y conserva el código y el estado `Activo`.
- Por qué falla (rojo): no existe el handler ni el comando.
- Casos borde obligatorios:
  - `Si_MonedaNoExiste_Debe_LanzarExcepcionNotFound`.
  - `Si_NuevoNombreEsNuloOVacio_Debe_LanzarExcepcionInvalidData`.

**Lo mínimo para que el test pase**
- `MonedaCommands.cs`: agregar nested record `Modificar(string Codigo, string Nombre, int Decimales)`.
- `Cosmos.DatosReferencia.Dominio/Monedas/CommandHandlers/ModificarMonedaHandler.cs`: carga, llama `moneda.Modificar(...)`, store + save.
- `Cosmos.DatosReferencia.Dominio/Monedas/Exceptions/ModificarMonedaException.cs`.
- `Cosmos.DatosReferencia.Comandos.API/Endpoints/Monedas/ModificarMonedaEndpoint.cs`: `app.MapPut("/monedas/{codigo}", …)`.

**Habilita:** UX de administración.
**Depende de:** ítems 1, 2, 3.

---

### 8. `Moneda` — Comando + endpoint: Inactivar `[F1]` `[Directamente implementable]`

**Explicación**
V10 obliga a inactivar en lugar de eliminar. El dominio de Moneda no tiene método `Inactivar()` todavía. Sin este, no se puede retirar una moneda obsoleta.

**Respaldo en la documentación**
> "Un registro que fue referenciado en una transacción no se puede eliminar — solo inactivar."
> — `definicion-alcance.md`, Sección 6 / `especificacion-servicio.md` V10

**Test que define este comportamiento**
- Nombre: `Si_MonedaActivaExiste_Debe_DejarlaInactiva`
- Qué verifica: tras invocar `InactivarMonedaHandler`, el documento Marten tiene `Activo = false`.
- Por qué falla (rojo): no existe el método `Moneda.Inactivar()` ni el handler.
- Casos borde obligatorios:
  - `Si_MonedaYaEstaInactiva_Debe_LanzarExcepcionBusinessRule`.
  - `Si_MonedaNoExiste_Debe_LanzarExcepcionNotFound`.

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Dominio/Monedas/Moneda.cs`: agregar método `public Moneda Inactivar()` con guard `if (!Activo) throw …`.
- `MonedaCommands.Inactivar(string Codigo)`.
- `InactivarMonedaHandler`.
- `Cosmos.DatosReferencia.Comandos.API/Endpoints/Monedas/InactivarMonedaEndpoint.cs`: `app.MapPatch("/monedas/{codigo}/inactivar", …)`.

**Habilita:** higiene del catálogo y respeto de V10.
**Depende de:** ítems 1, 2.

---

### 9. `Moneda` — Comando + endpoint: Activar `[F1]` `[Directamente implementable]`

**Explicación**
Contraparte de #8. Sin método `Activar()`, una moneda inactivada por error queda atrapada.

**Test que define este comportamiento**
- Nombre: `Si_MonedaInactivaExiste_Debe_DejarlaActiva`
- Casos borde:
  - `Si_MonedaYaEstaActiva_Debe_LanzarExcepcionBusinessRule`.
  - `Si_MonedaNoExiste_Debe_LanzarExcepcionNotFound`.

**Lo mínimo para que el test pase**
- `Moneda.Activar()`.
- `MonedaCommands.Activar`, `ActivarMonedaHandler`, `ActivarMonedaEndpoint` (`PATCH /monedas/{codigo}/activar`).

**Depende de:** ítems 1, 2, 8.

---

### 10. `Moneda` — Seed idempotente desde `monedas.json` `[F1]` `[Directamente implementable]`

**Explicación**
La estrategia Seed+Sync+Extend exige cargar los 154 registros de `monedas.json` al inicializar cualquier ambiente. Hoy no existe el proyecto `Cosmos.DatosReferencia.Seed`. Sin seed, el catálogo está vacío en producción y los consumidores no pueden validar nada.

**Respaldo en la documentación**
> "Al momento de la implementación, los scripts de seed del framework (...) consumen estos JSON directamente como input, sin reinterpretación manual."
> — `anexo-estrategia-datos-referencia.md`, Sección 1 (Seed)

> "Los scripts de seed deben ser idempotentes. Ejecutar el seed dos veces no debe duplicar datos ni fallar."
> — `anexo-estrategia-datos-referencia.md`, Consideraciones para el equipo

**Test que define este comportamiento (Seed.Tests)**
- Nombre: `Si_SeEjecutaSeedDeMonedas_Debe_PersistirLas154MonedasDelJson`
- Qué verifica: tras ejecutar el seed con un Marten limpio, `session.Query<Moneda>().Count()` es 154 y existen `"COP"`, `"USD"`, `"EUR"`, `"JPY"` (verificar 4 muestras representativas).
- Por qué falla (rojo): no existe el proyecto Seed.
- Casos borde obligatorios:
  - `Si_SeEjecutaSeedDosVeces_Debe_NoDuplicarMonedas` (mismo count tras segunda corrida — idempotencia).

**Lo mínimo para que el test pase**
- Crear proyecto `Cosmos.DatosReferencia.Seed` (console app) y `Cosmos.DatosReferencia.Seed.Tests` (xUnit + Marten TestContainer).
- `Cosmos.DatosReferencia.Seed/Cargadores/CargadorDeMonedas.cs`: lee `monedas.json`, hace upsert con `session.Store<Moneda>`.
- Embedded resource o path conocido para `monedas.json`.

**Habilita:** todos los demás seeds (estructura del proyecto) y ítems que necesitan monedas para validación referencial (#13, #29).
**Depende de:** ítems 1, 2.

---

### 11. `Pais` — Entidad + VOs `CodigoPais` y `IndicativoTelefonico` `[F1]` `[Directamente implementable]`

**Explicación**
Pais es la raíz de la jerarquía territorial y de los tipos de documento por país. Sin la entidad y sus VOs, ningún otro catálogo dependiente puede compilar sus tests.

**Respaldo en la documentación**
> "Identidad: codigo (ISO 3166-1 alpha-2, inmutable). codigo: 2 letras mayúsculas. indicativoTelefonico: Prefijo `+` seguido de 1 a 3 dígitos (ej: `+57`, `+1`, `+507`)."
> — `especificacion-servicio.md`, Sección 2.1

> V1: "El código debe ser ISO 3166-1 alpha-2 válido (2 letras mayúsculas)."
> — `especificacion-servicio.md`, Sección 6

**Test que define este comportamiento**
- Nombre: `Si_DatosDePaisSonValidos_Debe_ConstruirPais`
- Qué verifica: `new Pais(new CodigoPais("CO"), "Colombia", new CodigoMoneda("COP"), new IndicativoTelefonico("+57"))` queda construido con `Activo = true`.
- Por qué falla (rojo): no existen los tipos.
- Casos borde obligatorios:
  - `Si_CodigoEsMinusculas_Debe_LanzarExcepcionInvalidData` (CodigoPais).
  - `Si_CodigoTieneTresLetras_Debe_LanzarExcepcionInvalidData` (CodigoPais).
  - `Si_IndicativoNoEmpiezaConMas_Debe_LanzarExcepcionInvalidData`.
  - `Si_IndicativoTieneCuatroDigitos_Debe_LanzarExcepcionInvalidData`.
  - `Si_NombreEsNuloOVacio_Debe_LanzarExcepcionInvalidData`.

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Dominio/Paises/ValueObjects/CodigoPais.cs`: regex `^[A-Z]{2}$`.
- `Cosmos.DatosReferencia.Dominio/Paises/ValueObjects/IndicativoTelefonico.cs`: regex `^\+\d{1,3}$`.
- `Cosmos.DatosReferencia.Dominio/Paises/Exceptions/PaisException.cs`.
- `Cosmos.DatosReferencia.Dominio/Paises/Pais.cs`: `record Pais(CodigoPais Codigo, string Nombre, CodigoMoneda MonedaPrincipal, IndicativoTelefonico IndicativoTelefonico) { bool Activo {get;init;} = true; }`.
- Tests en `Cosmos.DatosReferencia.Dominio.Tests/Paises/`.

**Habilita:** #12 al #16 y todos los catálogos que referencian país.
**Depende de:** ítems 1 (wiring); el VO `CodigoMoneda` ya existe del trabajo previo.

---

### 12. `Pais` — Comando + endpoint: Agregar con validación V2 cross-catalog `[F1]` `[Directamente implementable]`

**Explicación**
Al agregar un país, la moneda principal debe existir en el catálogo de Monedas (V2). Sin esta validación, se persisten países con monedas inválidas.

**Respaldo en la documentación**
> V2: "La moneda principal debe existir en el catálogo de Monedas"
> — `especificacion-servicio.md`, Sección 6 (Validaciones)

**Test que define este comportamiento**
- Nombre: `Si_MonedaPrincipalExisteYDatosSonValidos_Debe_PersistirPais`
- Qué verifica: con `Moneda("COP")` ya persistida, `AgregarPaisHandler({"CO","Colombia","COP","+57"})` deja el país en Marten.
- Por qué falla (rojo): no existe el handler.
- Casos borde obligatorios:
  - `Si_MonedaPrincipalNoExisteEnElCatalogo_Debe_LanzarExcepcionBusinessRule` (V2).
  - `Si_PaisYaExisteConElMismoCodigo_Debe_LanzarExcepcionBusinessRule`.

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Dominio/Paises/Commands/PaisCommands.cs`: `abstract record PaisCommands { record Agregar(...) : PaisCommands; }`.
- `Cosmos.DatosReferencia.Dominio/Paises/CommandHandlers/AgregarPaisHandler.cs`: valida unicidad + V2 cargando `session.LoadAsync<Moneda>(comando.MonedaPrincipal)`.
- `AgregarPaisException`.
- `Cosmos.DatosReferencia.Comandos.API/Requests/PaisRequest.cs` y `Endpoints/Paises/AgregarPaisEndpoint.cs` (`POST /paises`).

**Habilita:** #13, #14, #15, #16, #17 (DivTerr y TipoDoc requieren país persistido para V3/V6).
**Depende de:** ítems 1, 2, 11.

---

### 13. `Pais` — Queries: por código + listar activos + endpoints REST `[F1]` `[Directamente implementable]`

**Explicación**
Misma estructura que #3-#5 para Moneda. Sin queries no se pueden validar V3/V6 ni los consumidores leer el catálogo.

**Respaldo en la documentación**
> "Países | Listar activos, consultar por código | — / codigo"
> — `especificacion-servicio.md`, Sección 3.1

**Test que define este comportamiento (acceptance)**
- Nombre: `Si_PaisExiste_Debe_Retornar200ConPaisSerializado`
- Casos borde: `Si_PaisNoExiste_Debe_Retornar404`, `Si_HayPaisesActivosEInactivos_Debe_ListadoRetornarSoloActivos`.

**Lo mínimo para que el test pase**
- `ConsultarPaisPorCodigo` + handler + `PaisReadModel` en Consultas.
- `ListarPaisesActivos` + handler.
- `GET /paises` y `GET /paises/{codigo}` en Consultas.API.

**Depende de:** ítems 11, 12.

---

### 14. `Pais` — Comando + endpoint: Modificar `[F1]` `[Directamente implementable]`

**Explicación**
Equivalente a #7 para Pais. Permite cambiar nombre, monedaPrincipal e indicativo manteniendo el código ISO inmutable.

**Test que define este comportamiento**
- Nombre: `Si_PaisExisteYNuevosDatosSonValidos_Debe_PersistirPaisConValoresActualizados`
- Casos borde:
  - `Si_NuevaMonedaPrincipalNoExiste_Debe_LanzarExcepcionBusinessRule` (V2 también en modificación).
  - `Si_PaisNoExiste_Debe_LanzarExcepcionNotFound`.

**Lo mínimo para que el test pase**
- `Pais.Modificar(string nombre, CodigoMoneda monedaPrincipal, IndicativoTelefonico indicativo)`.
- `PaisCommands.Modificar`, `ModificarPaisHandler`, `ModificarPaisException`, endpoint `PUT /paises/{codigo}`.

**Depende de:** ítems 11, 12, 13.

---

### 15. `Pais` — Comandos + endpoints: Inactivar y Activar `[F1]` `[Directamente implementable]`

**Explicación**
Análogo a #8/#9 para Pais. V10 también aplica a países.

**Test que define este comportamiento**
- Nombre: `Si_PaisActivoExiste_Debe_DejarloInactivo` y simétrico.
- Casos borde: ya inactivo / ya activo / no existe.

**Lo mínimo para que el test pase**
- `Pais.Inactivar()`, `Pais.Activar()`.
- `PaisCommands.Inactivar`, `PaisCommands.Activar` y sus handlers/endpoints (`PATCH /paises/{codigo}/inactivar|activar`).

**Depende de:** ítems 11, 12.

---

### 16. `Pais` — Seed idempotente desde `paises.json` `[F1]` `[Directamente implementable]`

**Explicación**
195 países en el archivo. Cada país referencia una moneda principal que debe existir antes (V2). Por eso el seed de monedas (#10) precede al de países.

**Respaldo en la documentación**
> "Países | paises.json | 195 | Todos los países del mundo (ISO 3166-1)"
> — `definicion-alcance.md`, Sección 5

**Test que define este comportamiento (Seed.Tests)**
- Nombre: `Si_SeEjecutaSeedDePaisesConMonedasPresentes_Debe_Persistir195Paises`
- Casos borde: `Si_SeEjecutaSeedDosVeces_Debe_NoDuplicarPaises`.

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Seed/Cargadores/CargadorDePaises.cs`.

**Depende de:** ítems 10, 11, 12.

---

### 17. `DivisionTerritorial` — Entidad + VO `CodigoDivision` + enum `NivelDivision` `[F1]` `[Directamente implementable]`

**Explicación**
Las divisiones territoriales son jerárquicas (departamento → municipio en CO, provincia → distrito en PA). Necesitan VO de código, enum de nivel, y soporte para `codigoSuperior` opcional.

**Respaldo en la documentación**
> "codigo: Código oficial de la división (formato según país, numérico DIVIPOLA para CO). nivel: departamento, municipio, provincia, distrito, corregimiento. codigoSuperior: Ref a otra División territorial. Null para el nivel más alto."
> — `especificacion-servicio.md`, Sección 2.2

**Test que define este comportamiento**
- Nombre: `Si_DatosDeDivisionSonValidos_Debe_ConstruirDivision`
- Casos borde:
  - `Si_NivelNoEsValido_Debe_LanzarExcepcionInvalidData` (enum se valida en parser).
  - `Si_NombreEsNuloOVacio_Debe_LanzarExcepcionInvalidData`.
  - `Si_CodigoSuperiorEsNuloYNivelEsDepartamento_Debe_ConstruirDivision`.

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Dominio/DivisionesTerritoriales/ValueObjects/CodigoDivision.cs`.
- `Cosmos.DatosReferencia.Dominio/DivisionesTerritoriales/NivelDivision.cs`: `enum { Departamento, Municipio, Provincia, Distrito, Corregimiento }`.
- `Cosmos.DatosReferencia.Dominio/DivisionesTerritoriales/DivisionTerritorial.cs`: record con `CodigoSuperior` como `CodigoDivision?`.
- `DivisionTerritorialException`.

**Habilita:** #18 al #21.
**Depende de:** ítem 11 (referencia a `CodigoPais`).

---

### 18. `DivisionTerritorial` — Comando + endpoint: Agregar con validaciones V3 y V4 `[F1]` `[Directamente implementable]`

**Explicación**
V3 exige que el país exista y esté activo. V4 exige que `codigoSuperior` exista y pertenezca al mismo país. Estas son validaciones cross-document que requieren cargar otros documentos en el handler.

**Respaldo en la documentación**
> V3: "El país referenciado debe existir y estar activo"
> V4: "Si tiene codigoSuperior, la división padre debe existir y pertenecer al mismo país"
> — `especificacion-servicio.md`, Sección 6

**Test que define este comportamiento**
- Nombre: `Si_PaisExisteYActivoYDatosSonValidos_Debe_PersistirDivision`
- Casos borde:
  - `Si_PaisNoExiste_Debe_LanzarExcepcionBusinessRule` (V3).
  - `Si_PaisEstaInactivo_Debe_LanzarExcepcionBusinessRule` (V3).
  - `Si_CodigoSuperiorExisteEnOtroPais_Debe_LanzarExcepcionBusinessRule` (V4).
  - `Si_CodigoSuperiorNoExiste_Debe_LanzarExcepcionBusinessRule` (V4).
  - `Si_DivisionYaExiste_Debe_LanzarExcepcionBusinessRule` (unicidad por código + país; ver decisión D-IdComp).

**Lo mínimo para que el test pase**
- `DivisionTerritorialCommands.Agregar` + `AgregarDivisionTerritorialHandler` + `AgregarDivisionTerritorialException`.
- Endpoint `POST /divisiones`.
- Decisión `D-IdComp`: cómo identificar el documento Marten dado que la identidad es `codigo+paisCodigo` (clave compuesta). Ver Sección 3.

**Depende de:** ítems 1, 11, 12, 17.

---

### 19. `DivisionTerritorial` — Queries y endpoints: por país, por nivel, por código, por jerarquía `[F1]` `[Directamente implementable]`

**Explicación**
La doc define 4 modos de consulta. Tributos municipales (ICA, RICA) los necesitan para resolver jurisdicción.

**Respaldo en la documentación**
> "Divisiones territoriales | Listar por país, listar por nivel, consultar por código | paisCodigo, nivel / codigo"
> — `especificacion-servicio.md`, Sección 3.1
> "Divisiones por jerarquía: paisCodigo=CO, codigoSuperior=05 → todos los municipios de Antioquia"
> — `especificacion-servicio.md`, Sección 3.2

**Test que define este comportamiento**
- `Si_HayDivisionesEnVariosPaises_Debe_RetornarSoloDelPaisSolicitado`.
- `Si_HayDivisionesDeVariosNiveles_Debe_RetornarSoloDelNivelSolicitado`.
- `Si_HayDivisionesDeMultiplesSuperiores_Debe_RetornarSoloHijasDelSuperiorSolicitado`.
- `Si_DivisionNoExiste_Debe_Retornar404`.

**Lo mínimo para que el test pase**
- 4 query handlers + read model + 4 endpoints en `Consultas.API` (`GET /paises/{paisCodigo}/divisiones`, `?nivel=`, `?codigoSuperior=`, `GET /divisiones/{codigo}?paisCodigo=`).

**Depende de:** ítems 17, 18.

---

### 20. `DivisionTerritorial` — Comandos + endpoints: Modificar / Inactivar / Activar `[F1]` `[Directamente implementable]`

**Explicación**
Misma estructura que #14/#15 para divisiones. Una observación: si una división padre es inactivada, ¿qué pasa con sus hijos? El modelo no lo aclara — ver decisión `D-CascadaInactivacion` en Sección 3.

**Test que define este comportamiento**
- `Si_DivisionExisteYNuevosDatosSonValidos_Debe_PersistirActualizada`.
- `Si_DivisionActivaExiste_Debe_DejarlaInactiva` y simétrico.

**Lo mínimo para que el test pase**
- `DivisionTerritorial.Modificar`, `.Inactivar()`, `.Activar()`.
- Comandos + handlers + endpoints (`PUT /divisiones/{codigo}`, `PATCH .../inactivar|activar`).

**Depende de:** ítems 17, 18.

---

### 21. `DivisionTerritorial` — Seed CO/DO/PA en orden jerárquico `[F1]` `[Directamente implementable]`

**Explicación**
1517 divisiones totales en 3 archivos. El seed debe respetar la jerarquía: los departamentos/provincias antes que los municipios/distritos (V4 exige que `codigoSuperior` exista).

**Respaldo en la documentación**
> "Divisiones territoriales se separan por país porque cada país tiene estructura jerárquica diferente"
> — `anexo-estrategia-datos-referencia.md`, Consideraciones

**Test que define este comportamiento (Seed.Tests)**
- Nombre: `Si_SeEjecutaSeedDeDivisionesCO_Debe_Persistir1188DivisionesConJerarquiaIntacta`
- Casos borde:
  - `Si_SeEjecutaSeedDosVeces_Debe_NoDuplicarDivisiones`.
  - `Si_PaisCONoEstaSembrado_Debe_LanzarExcepcionBusinessRule` (V3).

**Lo mínimo para que el test pase**
- `CargadorDeDivisionesTerritoriales` parametrizado por país (CO, DO, PA).
- Orden de inserción: por nivel ascendente (departamento → municipio).

**Depende de:** ítems 16 (Pais seed), 17, 18.

---

### 22. `TipoDocumentoIdentidad` — Entidad + VO `CodigoTipoDocumento` + enum `AplicaA` `[F1]` `[Directamente implementable]`

**Explicación**
Identidad compuesta `codigo + paisCodigo` (V7). `paisCodigo` puede ser null para documentos internacionales (V6). `aplicaA` es enum con 3 valores: persona natural, jurídica, ambos.

**Respaldo en la documentación**
> "Identidad: codigo + paisCodigo. paisCodigo: Ref a catálogo de Países. Null para documentos internacionales. aplicaA: personaNatural, personaJuridica, ambos"
> — `especificacion-servicio.md`, Sección 2.4

**Test que define este comportamiento**
- Nombre: `Si_DatosDeTipoDocumentoSonValidos_Debe_ConstruirTipoDocumento`
- Casos borde:
  - `Si_PaisCodigoEsNuloYDocumentoEsInternacional_Debe_ConstruirTipoDocumento`.
  - `Si_DescripcionEsNulaOVacia_Debe_LanzarExcepcionInvalidData`.
  - `Si_AplicaAEsValorInvalido_Debe_LanzarExcepcionInvalidData`.

**Lo mínimo para que el test pase**
- `CodigoTipoDocumento` VO, `AplicaA` enum, `TipoDocumentoIdentidad` record con `CodigoPais? PaisCodigo`.
- `TipoDocumentoIdentidadException`.

**Habilita:** #23 al #26.
**Depende de:** ítem 11.

---

### 23. `TipoDocumentoIdentidad` — Comando + endpoint: Agregar con V6 y V7 `[F1]` `[Directamente implementable]`

**Explicación**
V6: si `paisCodigo` no es null, debe existir y estar activo. V7: unicidad de `(codigo, paisCodigo)`. Identidad compuesta — ver decisión `D-IdComp`.

**Test que define este comportamiento**
- `Si_PaisExisteYActivoYDatosSonValidos_Debe_PersistirTipoDocumento`.
- Casos borde:
  - `Si_PaisCodigoEsNulo_Debe_PersistirTipoDocumentoInternacional` (V6 excepción para nulos).
  - `Si_PaisNoExiste_Debe_LanzarExcepcionBusinessRule`.
  - `Si_PaisInactivo_Debe_LanzarExcepcionBusinessRule`.
  - `Si_TipoYaExisteConMismoCodigoYPais_Debe_LanzarExcepcionBusinessRule` (V7).

**Lo mínimo para que el test pase**
- Comando, handler, exception, endpoint `POST /tipos-documento`.

**Depende de:** ítems 1, 11, 12, 22.

---

### 24. `TipoDocumentoIdentidad` — Queries y endpoints: por país, por código+país, filtrar por aplicaA `[F1]` `[Directamente implementable]`

**Test que define este comportamiento**
- `Si_HayTiposEnVariosPaises_Debe_RetornarSoloDelPaisSolicitado`.
- `Si_HayTiposParaPersonaNaturalYJuridica_Debe_RetornarSoloDelTipoSolicitado`.
- `Si_TipoNoExiste_Debe_Retornar404`.

**Lo mínimo para que el test pase**
- 3 query handlers + read model + endpoints (`GET /paises/{pais}/tipos-documento`, `?aplicaA=`, `GET /tipos-documento/{codigo}?paisCodigo=`).

**Depende de:** ítems 22, 23.

---

### 25. `TipoDocumentoIdentidad` — Modificar / Inactivar / Activar `[F1]` `[Directamente implementable]`

**Test que define este comportamiento**
- `Si_TipoExisteYNuevosDatosSonValidos_Debe_PersistirActualizado`.
- `Si_TipoActivoExiste_Debe_DejarloInactivo` y simétrico.

**Lo mínimo para que el test pase**
- Métodos en la entidad + comandos + handlers + endpoints (`PUT /tipos-documento/...`, `PATCH .../inactivar|activar`).

**Depende de:** ítems 22, 23.

---

### 26. `TipoDocumentoIdentidad` — Seed desde `tipos-documento-identidad.json` `[F1]` `[Directamente implementable]`

**Explicación**
45 tipos en el JSON, mezclando documentos por país e internacionales. El seed de países debe estar listo antes (V6).

**Test que define este comportamiento (Seed.Tests)**
- `Si_SeEjecutaSeedDeTiposDocumento_Debe_Persistir45TiposConPaisesValidos`.
- `Si_SeEjecutaSeedDosVeces_Debe_NoDuplicarTipos`.

**Lo mínimo para que el test pase**
- `CargadorDeTiposDocumento`.

**Depende de:** ítems 16, 22, 23.

---

### 27. `TasaDeCambio` — Entidad + VO `ParDeMonedas` `[F1]` `[Directamente implementable]`

**Explicación**
Identidad compuesta `monedaOrigen + monedaDestino + fechaVigencia` (V9). El VO `ParDeMonedas` agrupa origen y destino para simplificar firmas y queries.

**Respaldo en la documentación**
> "Identidad: monedaOrigen + monedaDestino + fechaVigencia. valor: Precisión según monedas involucradas."
> — `especificacion-servicio.md`, Sección 2.5

**Test que define este comportamiento**
- `Si_DatosDeTasaSonValidos_Debe_ConstruirTasa`.
- Casos borde:
  - `Si_ValorEsNegativo_Debe_LanzarExcepcionInvalidData` (regla implícita — valor de tasa no puede ser negativo).
  - `Si_MonedaOrigenIgualADestino_Debe_LanzarExcepcionInvalidData` (regla implícita — par válido excluye tasa 1.0 entre la misma moneda; ver decisión `D-MismaMoneda`).

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Dominio/TasasDeCambio/ValueObjects/ParDeMonedas.cs`.
- `Cosmos.DatosReferencia.Dominio/TasasDeCambio/TasaDeCambio.cs`.
- `TasaDeCambioException`.

**Habilita:** #28, #29, #30.
**Depende de:** ítem 2 (`CodigoMoneda` ya existe).

---

### 28. `TasaDeCambio` — Query: tasa vigente para (origen, destino, fecha) + endpoint `[F1]` `[Directamente implementable]`

**Explicación**
La doc define explícitamente: la TRM más reciente con `fechaVigencia <= fechaSolicitada`. No es "la última" — depende de la fecha pedida (OXP compara fecha de radicación vs fecha de extracto).

**Respaldo en la documentación**
> "Obtener la tasa vigente para un par de monedas en una fecha. Tasa vigente: monedaOrigen=USD, monedaDestino=COP, fecha=2026-04-15 → 4150.25."
> — `especificacion-servicio.md`, Secciones 3.1 y 3.2

> "Los consumidores deben consultar la tasa vigente para una fecha específica, no 'la última tasa'."
> — `especificacion-servicio.md`, Sección 8

**Test que define este comportamiento**
- `Si_HayTasaParaParEnFechaExacta_Debe_RetornarEsaTasa`.
- `Si_HayTasaAnteriorYNingunaPosterior_Debe_RetornarLaMasRecienteAnteriorOIgualALaFecha`.
- Casos borde:
  - `Si_NoHayTasaParaParEnNingunaFecha_Debe_Retornar404`.
  - `Si_SoloHayTasasPosterioresALaFechaSolicitada_Debe_Retornar404`.

**Lo mínimo para que el test pase**
- `ObtenerTasaVigente` query + handler que ordena por `FechaVigencia` desc y toma primera con `FechaVigencia <= fecha`.
- Endpoint `GET /tasas?origen=USD&destino=COP&fecha=2026-04-15`.

**Depende de:** ítems 27, 29 (al menos una tasa en Marten para que el test no quede vacío — alternativa: el ítem persiste el dato dentro de su Arrange).

---

### 29. `TasaDeCambio` — Comando + endpoint: Agregar con V8 y V9 `[F1]` `[Directamente implementable]`

**Explicación**
V8: monedas deben existir. V9: unicidad por par + fecha.

**Test que define este comportamiento**
- `Si_MonedasExistenYDatosSonValidos_Debe_PersistirTasa`.
- Casos borde:
  - `Si_MonedaOrigenNoExiste_Debe_LanzarExcepcionBusinessRule` (V8).
  - `Si_MonedaDestinoNoExiste_Debe_LanzarExcepcionBusinessRule` (V8).
  - `Si_TasaYaExisteParaParYFecha_Debe_LanzarExcepcionBusinessRule` (V9).

**Lo mínimo para que el test pase**
- Comando, handler, exception.
- Endpoint `POST /tasas` o `POST /tasas/{origen}/{destino}`.

**Depende de:** ítems 1, 2, 10, 27.

---

### 30. `TasaDeCambio` — Modificar / Eliminar `[F2]` `[Requiere especificación]`

**Explicación**
A diferencia de los otros catálogos, las tasas son hechos históricos. La documentación no aclara si pueden modificarse, si se eliminan o si solo se corrige por sustitución (crear una nueva tasa para la misma fecha sobrescribe). Tampoco hay V10 explícito para tasas (sí aplica a "todos" pero las tasas no son "referenciadas en transacciones" del mismo modo que las monedas).

**Por qué la documentación es insuficiente**
Sección 6 alcance dice "Cargar tasas de cambio manualmente" como caso de administración pero no especifica si es solo alta o también edición. Sección 8 dice que las tasas se cargan diariamente — implica reemplazo, no edición.

**Preguntas que deben responderse**
1. ¿Una tasa cargada por error puede modificarse, o el flujo es eliminar + recrear?
2. ¿Aplica V10 (no eliminar — solo inactivar) a tasas? Si sí, ¿qué significa "inactivar" para un hecho histórico?
3. ¿Hay auditoría para cambios de tasas (la doc menciona "fecha y usuario" para modificaciones)?

**Depende de:** ítem 29 (al menos la creación debe existir).

---

## Sección 3 — Ítems con decisión de diseño pendiente

### 1. Wiring CRUD vs ES en `Comandos.API`
**Decisión requerida:** ¿Se refactoriza el wiring de `Comandos.API/Program.cs` para usar `IDocumentSession` (CRUD sobre Marten) en lugar de `IEventStore`?
**Opciones identificadas:**
- A) Refactor completo: reemplazar `UsarWolverineParaComandos` y `AgregarMartenEventStore` por wiring CRUD (`AddMarten` clásico + Wolverine como mediator de comandos CRUD).
- B) Mantener wiring ES como infra inerte y usar la `IDocumentSession` que Marten ya expone — los handlers ignoran el Event Store y trabajan con documentos.
- C) Reemplazar Wolverine por Carter directo + handler simple, eliminando la indirección del mediator.
- D) Eliminar también RabbitMQ + Outbox + Inbox + Contratos (no hay eventos de dominio que publicar).
**Una vez decidido, implementar:** ítem #1 del plan.

### 2. Identidad compuesta de documentos Marten
**Decisión requerida:** ¿Cómo se modela en Marten una entidad con identidad compuesta?
- `DivisionTerritorial`: identidad `codigo` (único dentro del país) — pero la doc dice "único dentro del país", sugiriendo que el identificador del documento debería ser `paisCodigo + codigo`.
- `TipoDocumentoIdentidad`: identidad `codigo + paisCodigo`.
- `TasaDeCambio`: identidad `monedaOrigen + monedaDestino + fechaVigencia`.
**Opciones identificadas:**
- A) String concatenado como Id (ej: `"CO-05001"`).
- B) Guid v7 sintético como Id + índice único en los campos naturales.
- C) `CompoundKey` document mapping de Marten.
**Una vez decidido, implementar:** afecta ítems 17, 18, 22, 23, 27, 29.

### 3. Cascada de inactivación de divisiones territoriales (`D-CascadaInactivacion`)
**Decisión requerida:** Si una división padre se inactiva, ¿qué pasa con sus hijos?
**Opciones identificadas:**
- A) Inactivación en cascada (todos los hijos se inactivan).
- B) Bloqueo: no se puede inactivar mientras tenga hijos activos.
- C) Inactivación lógica del padre, los hijos quedan huérfanos activos (V4 ya no se respeta para inactivos).
**Una vez decidido, implementar:** afecta ítem 20.

### 4. Identidad de moneda contra ella misma en `TasaDeCambio` (`D-MismaMoneda`)
**Decisión requerida:** ¿Se permite crear una tasa con `monedaOrigen == monedaDestino`?
**Opciones identificadas:**
- A) Prohibido (rechazo en validación) — ítem 27 incluye este test.
- B) Permitido pero ignorado en queries (siempre retornar `1.0` cuando origen == destino sin necesidad de persistir).
**Una vez decidido, implementar:** afecta ítem 27.

### 5. Auditoría de cambios
**Decisión requerida:** "Toda modificación a los catálogos queda registrada con fecha y usuario" (Sección 6 alcance). ¿Cómo se implementa?
**Opciones identificadas:**
- A) Tabla `AuditLog` paralela escrita por interceptor de Marten.
- B) Eventos de auditoría persistidos en stream Marten (irónico dado D=B CRUD).
- C) Aprovechar `IDocumentSessionListener` + Serilog estructurado.
- D) Postergar — primera iteración sin auditoría (DEUDA documentada).
**Una vez decidido, implementar:** afecta todos los comandos de Modificar/Inactivar/Activar (ítems 7, 8, 9, 14, 15, 20, 25, 30).

### 6. Aplicación de V10 (protección de registros en uso)
**Decisión requerida:** V10 ("Un registro referenciado por otro servicio o dominio no se puede eliminar — solo inactivar") es una invariante de integración (cross-dominio).
**Opciones identificadas:**
- A) Endpoint expuesto: "¿estás usando este registro?" — pull síncrono por dominio.
- B) Eventos de integración (`MonedaInactivada`, etc.) — push asíncrono.
- C) Política blanda: este servicio nunca expone delete, solo inactivar; cada dominio respeta su propia protección.
**Una vez decidido, implementar:** afecta los comandos Inactivar de todos los catálogos.

### 7. Proyectos MCP Servers
**Decisión requerida:** Los proyectos `Cosmos.DatosReferencia.Comandos.MCP.Server` y `Cosmos.DatosReferencia.Consultas.MCP.Server` existen pero la doc no los menciona.
**Opciones identificadas:**
- A) Mantener vacíos como rail futuro.
- B) Eliminar.
- C) Implementar (no hay documentación que diga qué expondrían).
**Una vez decidido, implementar:** ortogonal al plan, ítem propio fuera del catálogo.

---

## Sección 4 — Ítems que requieren especificación

### PD1. Sincronización automática de tasas de cambio
**Por qué la documentación es insuficiente:** Está marcado explícitamente como pendiente de diseño en `especificacion-servicio.md`, Sección 8.
**Preguntas que deben responderse:**
1. ¿API directa del Banco de la República o scraping de archivo plano?
2. ¿Frecuencia exacta (00:00, cada hora, on-demand)?
3. ¿Qué hacer si la sincronización falla (alertar, reintentar, abrir ticket)?
4. ¿Estructura del módulo (BackgroundService, Hangfire, Quartz)?
5. ¿Misma estrategia para Banco Central RD?
6. ¿Otras fuentes (Open Exchange Rates, Fixer.io) son fallback documentados?

**Bloquea:** ítem 31 (no incluido en el plan numerado porque no hay forma de escribir un test rojo sin especificación).

### Decimales tope superior (PD informal)
**Por qué la documentación es insuficiente:** Sección 2.3 dice "0 para JPY/CLP, 2 para la mayoría, 3 para BHD" — implica un rango razonable pero no explicita el límite superior. El código actual permite cualquier `int >= 0`.
**Preguntas que deben responderse:**
1. ¿Hay un tope superior (4? 8? `int.MaxValue`)?
2. ¿La validación es responsabilidad del catálogo o del consumidor (tope práctico por precisión decimal)?

**Bloquea:** validación adicional en `Moneda`. Si se mantiene `Decimales >= 0` actual, esto no bloquea.

---

## Sección 5 — Regresiones detectadas

N/A. La instrucción explícita del usuario fue **ignorar el plan previo** para construir este plan desde cero. No se realizó comparación con `AIResume/DiagnosticoYPlanDominio.md` (si existiera).

---

## Changelog

| Versión | Fecha | Descripción |
|---|---|---|
| 1.0 | 2026-05-20 | Diagnóstico inicial: 5 catálogos (1 parcial, 4 pendientes), 10 validaciones (1 implementada), 6 archivos seed (ninguno cargado), 1 PD bloqueante (sync TRM), 30 ítems en el plan, 7 decisiones de diseño pendientes. |
