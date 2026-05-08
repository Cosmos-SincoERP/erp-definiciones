# Diagnóstico y Plan del Dominio — Datos de Referencia

**Fecha:** 2026-05-07
**Rama:** master
**Documentación analizada:** `Definiciones/compartido/datos-referencia/`
**Solución:** `Cosmos.DatosReferencia.sln`

---

## Resumen ejecutivo

| Estado | Cantidad |
|---|---|
| ✅ Implementados y verificados | 0 |
| 🔄 Parcialmente implementados | 0 |
| ⬜ Pendientes | 26 |
| 🔁 Regresiones detectadas | 0 |
| ❓ Gold-plating (sin respaldo en documentación) | 1 |
| **Total ítems del plan** | **26** |

> **Estado global del dominio:** 0% implementado. La solución contiene únicamente la estructura de proyectos del stack (skeletons con `Program.cs`, `*AssemblyMarker.cs`, `ApiFactory`, `HostTestFactory`). No hay un solo agregado, comando, evento, proyección, query handler, endpoint, seed ni test del dominio Datos de Referencia.

> **Bloqueante crítico:** la documentación disponible (`definicion-alcance.md`, `especificacion-servicio.md`, `anexo-estrategia-datos-referencia.md`) define el *qué* (estructura de datos, validaciones, operaciones de consulta, estrategia Seed+Sync+Extend) pero **no existe `modelo-dominio.md`** para este servicio. La definición misma dice explícitamente *"no tiene reglas de negocio propias / no tiene comportamiento propio / no publica eventos de dominio"* (sección 1.2 del alcance), lo que contradice el stack DDD+ES+CQRS del proyecto. Antes de poder implementar agregados, eventos y comandos hay que cerrar 6 decisiones de diseño previas (Sección 3) y 4 especificaciones faltantes (Sección 4).

---

## Sección 1 — Inventarios y diff

### 1.1 Catálogos documentados (Lista A)

| Catálogo | Identidad | Naturaleza | Atributos |
|---|---|---|---|
| **Países** | `codigo` (ISO 3166-1 α-2) | Estática (precarga 195) | codigo, nombre, monedaPrincipal→Moneda, indicativoTelefonico (E.164), activo |
| **Monedas** | `codigo` (ISO 4217) | Estática (precarga 154) | codigo, nombre, decimales, activo |
| **Divisiones territoriales** | `codigo` único dentro del país | Estática (precarga CO 1.188 / DO 221 / PA 108) | codigo, nombre, paisCodigo→País, nivel, codigoSuperior→DivisiónTerritorial, activo |
| **Tipos documento identidad** | `codigo` + `paisCodigo` | Estática (precarga 45) | codigo, descripcion, paisCodigo→País (nullable internacionales), aplicaA, activo |
| **Tasas de cambio** | `monedaOrigen` + `monedaDestino` + `fechaVigencia` | Dinámica (sync diaria) | monedaOrigen→Moneda, monedaDestino→Moneda, valor, fechaVigencia, fuente |

### 1.2 Agregados — diff

| Agregado | Comandos A/B | Eventos A/B | Behaviors A/B | Tests | Estado |
|---|---|---|---|---|---|
| `Pais` | n/d → 0 | n/d → 0 | n/d → 0 | 0 | ⬜ |
| `Moneda` | n/d → 0 | n/d → 0 | n/d → 0 | 0 | ⬜ |
| `DivisionTerritorial` | n/d → 0 | n/d → 0 | n/d → 0 | 0 | ⬜ |
| `TipoDocumentoIdentidad` | n/d → 0 | n/d → 0 | n/d → 0 | 0 | ⬜ |
| `TasaCambio` | n/d → 0 | n/d → 0 | n/d → 0 | 0 | ⬜ |

> *n/d* = la documentación no enumera comandos/eventos. Solo lista atributos, validaciones y operaciones de consulta. Los comandos/eventos deben derivarse en una decisión de diseño (Sección 3).

### 1.3 Domain Services — diff

| Service | Pipeline A/B | Puertos | Tests | Estado |
|---|---|---|---|---|
| `SincronizadorTasasCambio` (PD1) | n/d → 0 | ⬜ | 0 | ⬜ |

### 1.4 Proyecciones — diff

| Proyección | Read model | Query handler | Endpoint | Tests | Estado |
|---|---|---|---|---|---|
| `PaisesActivos` (listar países activos) | ⬜ | ⬜ | ⬜ | 0 | ⬜ |
| `PaisPorCodigo` | ⬜ | ⬜ | ⬜ | 0 | ⬜ |
| `MonedasActivas` | ⬜ | ⬜ | ⬜ | 0 | ⬜ |
| `MonedaPorCodigo` | ⬜ | ⬜ | ⬜ | 0 | ⬜ |
| `DivisionesPorPais` (con filtros nivel, codigoSuperior) | ⬜ | ⬜ | ⬜ | 0 | ⬜ |
| `DivisionPorCodigo` | ⬜ | ⬜ | ⬜ | 0 | ⬜ |
| `TiposDocumentoPorPais` (con filtro aplicaA) | ⬜ | ⬜ | ⬜ | 0 | ⬜ |
| `TipoDocumentoPorCodigoYPais` | ⬜ | ⬜ | ⬜ | 0 | ⬜ |
| `TasaVigenteEnFecha` (par monedas, fecha) | ⬜ | ⬜ | ⬜ | 0 | ⬜ |

### 1.5 Validaciones documentadas — diff

| ID | Catálogo | Validación | Tipo | Implementada |
|---|---|---|---|---|
| V1 | Países | Código ISO 3166-1 alpha-2 (2 letras mayúsculas, inmutable) | Formato | ⬜ |
| V2 | Países | Moneda principal debe existir en catálogo Monedas | Referencial | ⬜ |
| V3 | Divisiones | País referenciado existe y está activo | Referencial | ⬜ |
| V4 | Divisiones | `codigoSuperior` existe y pertenece al mismo país | Referencial | ⬜ |
| V5 | Monedas | Código ISO 4217 (3 letras mayúsculas) | Formato | ⬜ |
| V6 | Tipos doc | País existe y está activo (excepto internacionales) | Referencial | ⬜ |
| V7 | Tipos doc | Unicidad `codigo` + `paisCodigo` | Unicidad | ⬜ |
| V8 | Tasas cambio | Monedas origen y destino existen | Referencial | ⬜ |
| V9 | Tasas cambio | Unicidad `monedaOrigen` + `monedaDestino` + `fechaVigencia` | Unicidad | ⬜ |
| V10 | Todos | Registro referenciado por otro servicio no se elimina, solo inactiva | Protección | ⬜ |

### 1.6 Reglas e invariantes derivadas

| Regla derivada | Origen | Tipo | Componente |
|---|---|---|---|
| Códigos ISO de países y monedas son inmutables | Alcance §6 | Local | Pais / Moneda |
| Datos referenciados solo se inactivan, no se eliminan | Alcance §6, V10 | Eventual (cross-domain) | Todos los catálogos |
| Toda modificación queda registrada con fecha y usuario | Alcance §6 | Eventual (auditoría) | Todos |
| Divisiones territoriales siguen estructura jerárquica auto-referencial | Espec §2.2 | Local | DivisionTerritorial |
| Tasas de cambio se consultan por fecha — no "última tasa" | Espec §8 | N/A (query) | Proyección TasaVigenteEnFecha |

### 1.7 Pendientes documentados (PD)

| ID | Pendiente | Componentes bloqueados |
|---|---|---|
| PD1 | Mecanismo de sincronización automática de tasas de cambio (Banco República CO, Banco Central RD) | Domain service `SincronizadorTasasCambio` y todo lo derivado |

### 1.8 Seed / contenido estándar

| Contexto | Entidades A | Entidades B | Estado |
|---|---|---|---|
| Países globales | 195 (paises.json) | 0 | ⬜ |
| Monedas globales | 154 (monedas.json) | 0 | ⬜ |
| Tipos documento (CO+DO+PA+MX+CL+PE+EC+AR+BR + intl) | 45 (tipos-documento-identidad.json) | 0 | ⬜ |
| Divisiones territoriales CO (DIVIPOLA) | 1.188 (divisiones-territoriales-co.json) | 0 | ⬜ |
| Divisiones territoriales DO | 221 (divisiones-territoriales-do.json) | 0 | ⬜ |
| Divisiones territoriales PA | 108 (divisiones-territoriales-pa.json) | 0 | ⬜ |
| Tasas de cambio | Sin precarga (sync diaria) | 0 | n/a |

> **Brecha de proyecto:** No existen los proyectos `Cosmos.DatosReferencia.Seed` ni `Cosmos.DatosReferencia.Seed.Tests`. La estrategia Seed+Sync+Extend documentada exige idempotencia y JSON-as-source-of-truth, lo que implica un proyecto consola dedicado.

### 1.9 Integraciones — diff

| Operación | Documentada | Implementada | Contrato coincide |
|---|---|---|---|
| GET listar países activos | ✅ (Espec §3.1) | ⬜ | n/a |
| GET país por código | ✅ | ⬜ | n/a |
| GET divisiones por país | ✅ | ⬜ | n/a |
| GET divisiones por nivel | ✅ | ⬜ | n/a |
| GET división por código | ✅ | ⬜ | n/a |
| GET listar monedas activas | ✅ | ⬜ | n/a |
| GET moneda por código | ✅ | ⬜ | n/a |
| GET tipos doc por país | ✅ | ⬜ | n/a |
| GET tipo doc por código + país | ✅ | ⬜ | n/a |
| GET tasa vigente (par, fecha) | ✅ | ⬜ | n/a |
| Canal gRPC | No documentado | ⬜ | n/a |
| Canal MCP | No documentado | Existen los proyectos `*.MCP.Server` (skeleton) | n/a |

### 1.10 Gold-plating detectado

| Item | Origen | Decisión sugerida |
|---|---|---|
| `Cosmos.DatosReferencia.Contratos/Example/ProductCreated.cs` | Plantilla copiada del template original | Eliminar — no aplica al dominio |

---

## Sección 2 — Plan de implementación

> Cada ítem es un comportamiento concreto con su ciclo TDD completo.
> El orden está dictado por la dependencia de dominio: `Moneda` y `País` son los conceptos raíz; `DivisionTerritorial`, `TipoDocumentoIdentidad` y `TasaCambio` dependen de ellos.
> **Todos los ítems están condicionados a las decisiones de diseño de la Sección 3.** Hasta que esas decisiones se cierren, ningún test puede escribirse — la forma del agregado, los comandos y los eventos depende directamente de las respuestas. Por eso los ítems se marcan `[Con decisión]` y describen el comportamiento esperado, no el código exacto.
> Implementar cada ítem con `/implementar "[nombre del ítem]"` una vez decididas las preguntas de la Sección 3.

### 1. Eliminar contrato de ejemplo del template `[F1]` `[Directamente implementable]`

**Explicación**
`Cosmos.DatosReferencia.Contratos/Example/ProductCreated.cs` es código residual del template original. No representa ningún concepto del dominio Datos de Referencia y debe eliminarse antes de añadir contratos reales para evitar confundir el espacio de mensajes públicos.

**Respaldo en la documentación**
> "Tipos de empresa excluido: La clasificación de tipo de empresa (persona natural, jurídica, ESAL) no se incluye como catálogo de referencia. […]"
> — `definicion-alcance.md`, §3 (consideraciones)

No hay ningún `Producto` ni `ProductCreated` documentado en el dominio.

**Ejemplo**
- Comportamiento actual: la solución expone un evento público `ProductCreated` que no pertenece al dominio.
- Comportamiento esperado: el assembly `Contratos` queda con su `IContratosAssemblyMarker` y se llena solo cuando aparezca un evento público real.

**Test que define este comportamiento**
- No requiere test — eliminación de archivo. Verificación: `dotnet build` sigue verde.

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Contratos/Example/ProductCreated.cs`: eliminar.
- `Cosmos.DatosReferencia.Contratos/Example/`: eliminar carpeta vacía resultante.

**Habilita:** todos los ítems siguientes (limpia el espacio de Contratos).
**Depende de:** ninguno.

---

### 2. Crear `Moneda` con código ISO 4217 válido y decimales `[F1]` `[Con decisión]`

**Explicación**
No existe el agregado `Moneda`. Sin él no se puede crear `Pais` (su `monedaPrincipal` referencia a Monedas, V2), ni `TasaCambio` (V8). Es el primer concepto raíz del grafo de dependencias del dominio.

**Respaldo en la documentación**
> "Identidad: `codigo` (ISO 4217, inmutable) — codigo: 3 letras mayúsculas, inmutable; nombre: español de referencia; decimales: 0 para JPY/CLP, 2 para la mayoría, 3 para BHD; activo: por defecto true."
> — `especificacion-servicio.md`, §2.3

> "V5 — Monedas: El código debe ser ISO 4217 válido (3 letras mayúsculas)."
> — `especificacion-servicio.md`, §6

**Ejemplo**
- Acción: crear la moneda `COP` con 2 decimales.
- Comportamiento esperado: el sistema emite el evento de creación; consultas posteriores retornan la moneda. Crear con código `cop`, `COPP`, `12C` o vacío falla con `InvalidData`.

**Test que define este comportamiento**
- Nombre: `Si_DatosDeMonedaSonValidos_Debe_EmitirMonedaCreada`
- Casos borde obligatorios:
  - `Si_CodigoEstaEnMinusculas_Debe_LanzarExcepcionInvalidData`
  - `Si_CodigoTieneDosLetras_Debe_LanzarExcepcionInvalidData`
  - `Si_DecimalesEsNegativo_Debe_LanzarExcepcionInvalidData`
  - `Si_NombreEsNuloOVacio_Debe_LanzarExcepcionInvalidData`

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Dominio/Monedas/Commands/MonedaCommands.cs`: comando `Crear`.
- `Cosmos.DatosReferencia.Dominio/Monedas/Events/MonedaEvents.cs`: evento `MonedaCreada`.
- `Cosmos.DatosReferencia.Dominio/Monedas/Moneda.cs`: agregado.
- `Cosmos.DatosReferencia.Dominio/Monedas/ValueObjects/CodigoMoneda.cs`: VO con validación ISO 4217.
- `Cosmos.DatosReferencia.Dominio/Monedas/Exceptions/CrearMonedaException.cs`.
- `Cosmos.DatosReferencia.Dominio/Monedas/CommandHandlers/CrearMonedaHandler.cs`.
- `Cosmos.DatosReferencia.Dominio.Tests/Monedas/Comandos/CrearMonedaTests.cs` + base abstracta `MonedaCommandHandlerAsyncTest<T>`.

**Habilita:** ítem 3 (modificar moneda), ítem 4 (inactivar moneda), ítem 5 (crear país), ítem 14 (crear tasa de cambio).
**Depende de:** decisiones D1, D2, D3 (Sección 3).

---

### 3. Modificar nombre o decimales de una `Moneda` existente `[F1]` `[Con decisión]`

**Explicación**
Aún sin `ModificarMoneda`, una corrección de tipografía en el nombre o un ajuste de decimales (caso `JPY` → `0` decimales) requeriría manipular Marten directamente. La administración del catálogo lo exige (`Alcance §6: Toda modificación a los catálogos queda registrada con fecha y usuario`).

**Respaldo en la documentación**
> "Registrar cambios — Toda modificación a los catálogos queda registrada con fecha y usuario."
> — `definicion-alcance.md`, §6

> "Códigos ISO de países (3166-1) y monedas (4217) no son editables. Son estándares internacionales."
> — `definicion-alcance.md`, §6

**Test**
- Nombre: `Si_MonedaExisteYDatosSonValidos_Debe_EmitirMonedaModificada`
- Casos borde: `Si_MonedaNoExiste_Debe_LanzarExcepcionNotFound`, `Si_NuevoCodigoEsDistintoDelOriginal_Debe_LanzarExcepcionBusinessRule` (códigos ISO inmutables).

**Lo mínimo para que el test pase**
- `MonedaCommands.cs`: comando `Modificar` con nombre y decimales (no código).
- `MonedaEvents.cs`: evento `MonedaModificada`.
- `Moneda.cs`: método `Modificar(...)` + `Apply`.
- `Exceptions/ModificarMonedaException.cs`.
- `CommandHandlers/ModificarMonedaHandler.cs`.
- `*.Dominio.Tests/Monedas/Comandos/ModificarMonedaTests.cs`.

**Habilita:** ítem 4.
**Depende de:** ítem 2.

---

### 4. Inactivar una `Moneda` existente `[F1]` `[Con decisión]`

**Explicación**
La protección V10 exige que un registro referenciado solo pueda inactivarse, no eliminarse. La inactivación debe ser explícita y reversible (reactivación es ítem futuro si el negocio la pide).

**Respaldo en la documentación**
> "V10 — Todos: Un registro referenciado por otro servicio o dominio no se puede eliminar — solo inactivar."
> — `especificacion-servicio.md`, §6

**Test**
- `Si_MonedaActivaExiste_Debe_EmitirMonedaInactivada`
- `Si_MonedaYaInactiva_Debe_LanzarExcepcionBusinessRule`

**Lo mínimo para que el test pase**
- `MonedaCommands.cs`: `Inactivar`.
- `MonedaEvents.cs`: `MonedaInactivada`.
- `Moneda.cs`: método `Inactivar()` + `Apply`.
- `Exceptions/InactivarMonedaException.cs`.
- `CommandHandlers/InactivarMonedaHandler.cs`.
- `*.Dominio.Tests/Monedas/Comandos/InactivarMonedaTests.cs`.

**Habilita:** invariante V10 para Monedas.
**Depende de:** ítem 2.

---

### 5. Crear `Pais` con moneda principal existente `[F1]` `[Con decisión]`

**Explicación**
`Pais` es el segundo concepto raíz. Su creación valida V1 (formato ISO 3166-1) y V2 (referencia a Moneda existente). Sin él no se pueden crear `DivisionTerritorial` (V3) ni `TipoDocumentoIdentidad` (V6).

**Respaldo en la documentación**
> "Identidad: codigo (ISO 3166-1 alpha-2, inmutable). codigo: 2 letras mayúsculas inmutable; nombre: español de referencia; monedaPrincipal: Ref a catálogo de Monedas; indicativoTelefonico: prefijo + seguido de 1 a 3 dígitos (E.164); activo por defecto true."
> — `especificacion-servicio.md`, §2.1

> "V1 — código ISO 3166-1 alpha-2 (2 letras mayúsculas)."
> "V2 — La moneda principal debe existir en el catálogo de Monedas."
> — `especificacion-servicio.md`, §6

**Test**
- `Si_DatosDePaisSonValidosYMonedaExiste_Debe_EmitirPaisCreado`
- `Si_CodigoNoEsDosLetrasMayusculas_Debe_LanzarExcepcionInvalidData`
- `Si_IndicativoNoCumpleE164_Debe_LanzarExcepcionInvalidData` (sin `+`, más de 3 dígitos, no numérico)
- `Si_MonedaPrincipalNoExisteOEstaInactiva_Debe_LanzarExcepcionBusinessRule`

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Dominio/Paises/Commands/PaisCommands.cs`: `Crear`.
- `Cosmos.DatosReferencia.Dominio/Paises/Events/PaisEvents.cs`: `PaisCreado`.
- `Cosmos.DatosReferencia.Dominio/Paises/Pais.cs`.
- `ValueObjects/CodigoPais.cs`, `ValueObjects/IndicativoTelefonico.cs`.
- `Exceptions/CrearPaisException.cs`.
- Port `IVerificadorDeMonedaActiva` en `Compartidos/Ports/` (V2 cruza agregados → port en dominio + adapter sobre proyección `MonedasActivas`).
- `CommandHandlers/CrearPaisHandler.cs`.
- `*.Dominio.Tests/Paises/Comandos/CrearPaisTests.cs`.

**Habilita:** ítems 6, 7, 9, 11.
**Depende de:** ítem 2 (necesita una moneda activa para validar V2).

---

### 6. Modificar nombre, moneda principal o indicativo de un `Pais` `[F1]` `[Con decisión]`

**Explicación**
Permite corregir el nombre de referencia o cambiar la moneda funcional sin alterar el código ISO (inmutable).

**Respaldo en la documentación**
> "codigo: Inmutable. […] Toda modificación a los catálogos queda registrada con fecha y usuario."
> — `especificacion-servicio.md` §2.1, `definicion-alcance.md` §6

**Test**
- `Si_PaisExisteYNuevaMonedaEsValida_Debe_EmitirPaisModificado`
- `Si_PaisNoExiste_Debe_LanzarExcepcionNotFound`
- `Si_NuevaMonedaPrincipalNoExiste_Debe_LanzarExcepcionBusinessRule`

**Lo mínimo para que el test pase**
- `PaisCommands.cs`: `Modificar`.
- `PaisEvents.cs`: `PaisModificado`.
- `Pais.cs`: método `Modificar(...)` + `Apply`.
- `Exceptions/ModificarPaisException.cs`.
- `CommandHandlers/ModificarPaisHandler.cs`.
- Test correspondiente.

**Habilita:** ítem 7.
**Depende de:** ítem 5.

---

### 7. Inactivar un `Pais` `[F1]` `[Con decisión]`

**Explicación**
V10 — un país referenciado (por divisiones, tipos doc, terceros) no se elimina, solo se inactiva. La inactivación de país tiene implicaciones cruzadas: V3 y V6 exigen que las divisiones y tipos de documento referencien a un país **activo** — esto es una invariante eventual que se debe documentar.

**Respaldo en la documentación**
> "V3 — El país referenciado debe existir y estar activo. V6 — El país referenciado debe existir y estar activo (excepto documentos internacionales con paisCodigo null). V10 — Un registro referenciado no se puede eliminar — solo inactivar."
> — `especificacion-servicio.md`, §6

**Test**
- `Si_PaisActivoExiste_Debe_EmitirPaisInactivado`
- `Si_PaisYaInactivo_Debe_LanzarExcepcionBusinessRule`

**Lo mínimo para que el test pase**
- `PaisCommands.cs`: `Inactivar`.
- `PaisEvents.cs`: `PaisInactivado`.
- `Pais.cs`: método `Inactivar()` + `Apply`.
- `Exceptions/InactivarPaisException.cs`.
- `CommandHandlers/InactivarPaisHandler.cs`.
- Test correspondiente.

**Habilita:** invariante V10 para `Pais`.
**Depende de:** ítem 5.

---

### 8. Proyección + query `MonedasActivas` y `MonedaPorCodigo` `[F1]` `[Con decisión]`

**Explicación**
Sin proyección no hay endpoint de consulta. El read model alimenta tanto a consumidores externos como al port `IVerificadorDeMonedaActiva` usado internamente por `CrearPais` (V2) y `CrearTasaCambio` (V8).

**Respaldo en la documentación**
> "Monedas — Listar activas, consultar por código."
> — `especificacion-servicio.md`, §3.1

> "Monedas — Cacheable: prácticamente inmutable."
> — `especificacion-servicio.md`, §4

**Test**
- `Si_HayMonedasActivasEInactivas_Debe_RetornarSoloLasActivas`
- `Si_NoExisteMonedaConCodigo_Debe_RetornarNull`

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Consultas/Monedas/Proyecciones/MonedaReadModel.cs`: read model con `Codigo`, `Nombre`, `Decimales`, `Activo`.
- `Cosmos.DatosReferencia.Consultas/Monedas/Proyecciones/MonedaProjection.cs`: `SingleStreamProjection<MonedaReadModel, string>`.
- Query handlers: `ListarMonedasActivasHandler`, `ObtenerMonedaPorCodigoHandler`.
- Registrar en `Consultas.API/ProyeccionesRegister.cs`.
- `*.Consultas.Tests/Monedas/...` con `IAsyncLifetime`.

**Habilita:** endpoint REST de monedas (ítem 17), validación V2 desde `CrearPais`.
**Depende de:** ítem 2.

---

### 9. Crear `DivisionTerritorial` con país y división superior válidos `[F1]` `[Con decisión]`

**Explicación**
Es el primer comportamiento que requiere validación de jerarquía auto-referencial: V4 exige que `codigoSuperior`, si está presente, exista y pertenezca al mismo país.

**Respaldo en la documentación**
> "Identidad: codigo (único dentro del país, código oficial: DIVIPOLA para CO). codigo formato según país; paisCodigo Ref Países; nivel: departamento, municipio, provincia, distrito, corregimiento; codigoSuperior nullable Ref otra DivisiónTerritorial."
> — `especificacion-servicio.md`, §2.2

> "V3 — País existe y está activo. V4 — Si tiene codigoSuperior, la división padre debe existir y pertenecer al mismo país."
> — `especificacion-servicio.md`, §6

**Test**
- `Si_DivisionDeNivelDepartamentoSinSuperior_Debe_EmitirDivisionCreada`
- `Si_DivisionDeNivelMunicipioConSuperiorDelMismoPais_Debe_EmitirDivisionCreada`
- `Si_PaisNoExisteOEstaInactivo_Debe_LanzarExcepcionBusinessRule`
- `Si_SuperiorPerteneceAOtroPais_Debe_LanzarExcepcionBusinessRule`
- `Si_NivelNoEsDelEnumDocumentado_Debe_LanzarExcepcionInvalidData`

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Dominio/DivisionesTerritoriales/Commands/.../Crear`.
- Eventos `DivisionCreada`.
- VO `NivelDivisionTerritorial` (enum: Departamento, Municipio, Provincia, Distrito, Corregimiento).
- Ports `IVerificadorDePaisActivo`, `IVerificadorDeDivisionEnPais` (V3, V4).
- Handler + tests.

**Habilita:** ítem 10.
**Depende de:** ítem 5 (necesita país activo) y proyección de País (ítem 12).

---

### 10. Inactivar `DivisionTerritorial` `[F1]` `[Con decisión]`

**Explicación**
V10 aplica a divisiones — ICA/RICA referencia divisiones de nivel municipio. La inactivación es soft-delete.

**Respaldo en la documentación**
> "V10 — Todos los catálogos." `definicion-alcance.md` §6 — "Tributos municipales (ICA, RICA) requieren nivel de municipio."

**Test**
- `Si_DivisionActivaExiste_Debe_EmitirDivisionInactivada`
- `Si_DivisionYaInactiva_Debe_LanzarExcepcionBusinessRule`

**Lo mínimo para que el test pase**
- Comando `Inactivar`, evento `DivisionInactivada`, método + Apply, exception, handler, test.

**Habilita:** V10 sobre divisiones territoriales.
**Depende de:** ítem 9.

---

### 11. Crear `TipoDocumentoIdentidad` por país (con soporte de internacionales) `[F1]` `[Con decisión]`

**Explicación**
La identidad compuesta `codigo + paisCodigo` y la posibilidad de `paisCodigo null` para documentos internacionales son condiciones específicas de este catálogo. V7 exige unicidad.

**Respaldo en la documentación**
> "Identidad: codigo + paisCodigo. paisCodigo Ref Países. Null para documentos internacionales. aplicaA: personaNatural, personaJuridica, ambos."
> — `especificacion-servicio.md`, §2.4

> "V6 — País existe y está activo (excepto internacionales con paisCodigo null). V7 — No pueden existir dos tipos con el mismo código para el mismo país."
> — `especificacion-servicio.md`, §6

**Test**
- `Si_TipoDocumentoColombianoConCodigoUnico_Debe_EmitirTipoDocumentoCreado`
- `Si_TipoDocumentoInternacionalSinPais_Debe_EmitirTipoDocumentoCreado`
- `Si_AplicaANoEsDelEnumDocumentado_Debe_LanzarExcepcionInvalidData` (personaNatural/personaJuridica/ambos)
- `Si_PaisNoExisteOInactivoYNoEsInternacional_Debe_LanzarExcepcionBusinessRule`
- `Si_YaExisteOtroTipoConMismoCodigoEnMismoPais_Debe_LanzarExcepcionBusinessRule`

**Lo mínimo para que el test pase**
- Carpeta `TiposDocumentoIdentidad/` en Dominio.
- VO `AplicaA` (enum), VO `CodigoTipoDocumento`.
- Comando `Crear`, evento `TipoDocumentoCreado`.
- Port `IVerificadorDeUnicidadTipoDocumento` (V7) + adapter sobre proyección.
- Handler + tests.

**Habilita:** ítem siguiente sobre tipos doc.
**Depende de:** ítem 5, ítem 12 (proyección de país para V6), proyección propia para V7 (ítem 13 si se decide port-vía-proyección).

---

### 12. Proyección + query `PaisesActivos` y `PaisPorCodigo` `[F1]` `[Con decisión]`

**Explicación**
Read model de país. Alimenta a `IVerificadorDePaisActivo` (V3, V6), endpoint público y consumidores externos.

**Respaldo en la documentación**
> "Países — Listar activos, consultar por código. Cacheable: prácticamente inmutable."
> — `especificacion-servicio.md`, §3.1, §4

**Test**
- `Si_PaisesCreadosYAlgunosInactivados_Debe_ListarSoloActivos`
- `Si_PaisExistePorCodigo_Debe_RetornarReadModelCompleto`

**Lo mínimo para que el test pase**
- `Cosmos.DatosReferencia.Consultas/Paises/...` proyección, read model, query handlers, tests.

**Habilita:** validación V3, V6 para divisiones y tipos doc; endpoint REST de países (ítem 17).
**Depende de:** ítem 5.

---

### 13. Proyección + queries de tipos de documento (por país, por código + país) `[F1]` `[Con decisión]`

**Explicación**
Read model de tipos de documento. Alimenta el port `IVerificadorDeUnicidadTipoDocumento` (V7) y los endpoints de consulta.

**Respaldo en la documentación**
> "Tipos de documento — Listar por país, consultar por código + país. Filtrar por país y tipo de persona."
> — `especificacion-servicio.md`, §3.1, §3.2

**Test**
- `Si_HayTiposDocumentoPorVariosPaises_Debe_FiltrarPorPaisCodigo`
- `Si_HayTiposDocumentoConDistintaAplicabilidad_Debe_FiltrarPorAplicaA`

**Lo mínimo para que el test pase**
- Proyección `TipoDocumentoProjection` + read model + 2 query handlers.

**Habilita:** V7 y endpoints (ítem 17).
**Depende de:** ítem 11.

---

### 14. Crear `TasaCambio` para par de monedas y fecha `[F1]` `[Con decisión]`

**Explicación**
Único catálogo dinámico. La identidad es la tupla `(monedaOrigen, monedaDestino, fechaVigencia)`. V8 valida existencia de monedas; V9 exige unicidad por la tupla.

**Respaldo en la documentación**
> "Identidad: monedaOrigen + monedaDestino + fechaVigencia. valor decimal. fuente nullable."
> — `especificacion-servicio.md`, §2.5

> "V8 — Monedas origen y destino existen. V9 — Unicidad monedaOrigen + monedaDestino + fechaVigencia."
> — `especificacion-servicio.md`, §6

> "Tasas de cambio como único catálogo dinámico. Consultas por fecha — los consumidores deben consultar la tasa vigente para una fecha específica, no 'la última tasa'."
> — `especificacion-servicio.md`, §8

**Test**
- `Si_TasaConParDeMonedasYFechaUnica_Debe_EmitirTasaRegistrada`
- `Si_MonedaOrigenODestinoNoExiste_Debe_LanzarExcepcionBusinessRule`
- `Si_OrigenIgualADestino_Debe_LanzarExcepcionInvalidData`
- `Si_ValorEsCeroONegativo_Debe_LanzarExcepcionInvalidData`
- `Si_YaExisteTasaConMismaTuplaOrigenDestinoFecha_Debe_LanzarExcepcionBusinessRule`

**Lo mínimo para que el test pase**
- Agregado `TasaCambio` (forma exacta depende de D2).
- VO `Valor` (positivo, precisión).
- Comando `Registrar`, evento `TasaRegistrada`.
- Ports `IVerificadorDeMonedaActiva` (reutilizar el del ítem 8) + `IVerificadorDeUnicidadTasa` (V9).
- Handler + tests.

**Habilita:** ítem 15.
**Depende de:** ítems 2, 8.

---

### 15. Proyección `TasaVigenteEnFecha` (consulta por par y fecha) `[F1]` `[Con decisión]`

**Explicación**
La query crítica para OXP e Impuestos: dada una `fecha` y un par `(origen, destino)`, retornar la tasa vigente más reciente con `fechaVigencia <= fecha`.

**Respaldo en la documentación**
> "Tasa vigente — Obtener la TRM más reciente para un par de monedas en una fecha determinada. monedaOrigen=USD, monedaDestino=COP, fecha=2026-04-15 → 4150.25."
> — `especificacion-servicio.md`, §3.2

> "Tasas de cambio — Con precaución (cacheable): se actualizan diariamente."
> — `especificacion-servicio.md`, §4

**Test**
- `Si_HayVariasTasasParaParDeMonedas_Debe_RetornarLaMasRecienteConFechaMenorOIgualALaConsultada`
- `Si_NoExisteTasaParaParEnFechaConsultada_Debe_RetornarNull`

**Lo mínimo para que el test pase**
- `MultiStreamProjection` o read model que indexe por `(origen, destino)` y permita búsqueda por fecha.
- `ConsultarTasaVigenteHandler`.
- Tests.

**Habilita:** endpoint REST de tasa vigente (ítem 17).
**Depende de:** ítem 14.

---

### 16. Proyección + query `DivisionesPorPais` y `DivisionPorCodigo` (con jerarquía y nivel) `[F1]` `[Con decisión]`

**Explicación**
Read model de divisiones territoriales con soporte de filtros: por país, por nivel, por `codigoSuperior` (jerarquía: dado un departamento, listar sus municipios).

**Respaldo en la documentación**
> "Divisiones por jerarquía — Obtener los municipios de un departamento. paisCodigo=CO, codigoSuperior=05 → todos los municipios de Antioquia."
> — `especificacion-servicio.md`, §3.2

**Test**
- `Si_HayDivisionesEnVariosPaises_Debe_FiltrarPorPaisCodigo`
- `Si_HayDivisionesEnVariosNiveles_Debe_FiltrarPorNivel`
- `Si_HayDivisionesConCodigoSuperior_Debe_FiltrarPorJerarquia`

**Lo mínimo para que el test pase**
- Proyección, read model, query handlers, tests.

**Habilita:** endpoint REST de divisiones (ítem 17).
**Depende de:** ítem 9.

---

### 17. Endpoints REST de consulta (Carter) `[F1]` `[Con decisión]`

**Explicación**
Sección 3.1 de la especificación enumera 10 operaciones de consulta — ningún endpoint existe hoy. Se exponen como Minimal API (Carter).

**Respaldo en la documentación**
> Ver tabla §3.1 completa de `especificacion-servicio.md`.

**Test (acceptance)**
Para cada endpoint: status code esperado + estructura mínima de respuesta. Ej.:
- `Si_HayPaisesActivosCargados_GETPaisesDevuelve200ConColeccion`
- `Si_PaisExistePorCodigo_GETPaisesCodigoDevuelve200ConDetalle`
- `Si_PaisNoExiste_GETPaisesCodigoDevuelve404`
- `Si_TasaVigenteExisteParaParYFecha_GETTasasDevuelve200ConValor`

**Lo mínimo para que el test pase**
Un Carter `ICarterModule` por endpoint (10 archivos), DTOs de respuesta, registro en Carter. Las rutas concretas dependen de D5 (Sección 4).

**Habilita:** consumidores externos (Terceros, OXP, Impuestos, Direcciones).
**Depende de:** ítems 8, 12, 13, 15, 16 (necesitan las queries detrás).

---

### 18. Proyecto `Cosmos.DatosReferencia.Seed` con carga idempotente desde JSON `[F1]` `[Con decisión]`

**Explicación**
La estrategia documentada Seed+Sync+Extend exige un consola con carga idempotente desde los 6 archivos JSON. El proyecto no existe.

**Respaldo en la documentación**
> "Seed — Carga inicial desde archivos JSON. Los catálogos se preconstruyen como archivos JSON en `compartido/datos-referencia/catalogos/`. Estos archivos son la fuente de verdad. […] Los scripts de seed deben ser idempotentes."
> — `anexo-estrategia-datos-referencia.md`

**Test (en `Cosmos.DatosReferencia.Seed.Tests`)**
- `Si_BaseVacia_DebeCargarLos195PaisesDelJson`
- `Si_BaseVacia_DebeCargarLas154MonedasDelJson`
- `Si_BaseVacia_DebeCargar45TiposDocumentoDelJson`
- `Si_BaseVacia_DebeCargar1188DivisionesCO`
- `Si_BaseVacia_DebeCargar221DivisionesDO`
- `Si_BaseVacia_DebeCargar108DivisionesPA`
- `Si_SeedSeEjecutaDosVeces_NoDebeDuplicarRegistros`

**Lo mínimo para que el test pase**
- Crear proyectos `Cosmos.DatosReferencia.Seed` (consola) y `Cosmos.DatosReferencia.Seed.Tests`.
- Configurar acceso a JSONs (link a `Definiciones/compartido/datos-referencia/catalogos/`).
- Lector + invocación de comandos `Crear*` idempotentemente (verificar existencia antes de crear o ignorar `BusinessRule` de duplicado).

**Habilita:** datos preconfigurados en cualquier ambiente.
**Depende de:** ítems 2, 5, 8, 9, 11, 12, 13, 16 (todos los Crear y proyecciones de validación).

---

### 19. Sincronización diaria de tasas de cambio (CO + DO) `[F2]` `[Requiere especificación]`

**Explicación**
PD1 de la especificación: el mecanismo no está definido. Bloqueado por la ausencia de spec del trigger (cron interno vs. orquestador externo), de la cobertura (qué pares cargar), del manejo de fines de semana/festivos y del fallback.

**Respaldo en la documentación**
> "PD1 — Sincronización automática de tasas de cambio. Definir el mecanismo para obtener la TRM diaria del Banco de la República y Banco Central RD."
> — `especificacion-servicio.md`, §8

> Pasa a la Sección 4 hasta que se especifique.

---

### 20-26. Consideraciones diferidas

Una vez tomadas las decisiones de la Sección 3 y resuelta la especificación faltante (Sección 4), pueden requerirse:

- **20.** Reactivar registros inactivados (si el negocio lo pide — no documentado).
- **21.** Modificar atributos no-clave de divisiones (si lo pide la administración).
- **22.** Modificar atributos no-clave de tipos de documento.
- **23.** Endpoints de administración (POST/PUT/DELETE) — no documentados; el alcance §6 menciona "intervención del administrador (excepcional)" pero no detalla rutas.
- **24.** Canal gRPC para integración con Terceros, OXP, Impuestos, Direcciones (no documentado, es ❓ gold-plating si se hace antes de pedirlo).
- **25.** Servidores MCP poblados con tools de consulta (los proyectos `*.MCP.Server` están vacíos).
- **26.** Health checks específicos del dominio (más allá de los genéricos del template).

Estos ítems se elaborarán cuando se decida su inclusión en el alcance — no están listos para `/implementar`.

---

## Sección 3 — Ítems con decisión de diseño pendiente

### D1. ¿Event Sourcing o CRUD para Datos de Referencia?

**Decisión requerida:** El alcance §1.2 declara explícitamente que Datos de Referencia *"no tiene reglas de negocio propias / no tiene comportamiento propio / no publica eventos de dominio"* — criterios que descalifican el patrón ES. Sin embargo, el stack técnico del proyecto es DDD+ES+CQRS. ¿Se modela bajo Event Sourcing (consistente con el stack) o como CRUD + read models (consistente con la naturaleza del servicio)?

**Opciones identificadas:**
- **A. Event Sourcing leve** — agregados con eventos `XCreado`, `XModificado`, `XInactivado`. Pros: consistente con CLAUDE.md y el resto de los servicios; auditoría natural ("toda modificación queda registrada con fecha y usuario", §6); permite reactividad si más adelante el servicio publica eventos públicos. Contras: sobre-ingeniería para CRUD; cada cambio de catálogo emite un evento.
- **B. CRUD sobre Marten/PostgreSQL** — documentos Marten directos, sin streams. Pros: alineado con la declaración de la doc; menos ceremonia. Contras: rompe la convención de la solución; introduce un patrón disjunto para auditoría.
- **C. Híbrido** — Países, Monedas, Divisiones, Tipos doc como CRUD (estáticos); Tasas de Cambio como ES (dinámico, alimentado diariamente, requiere historia). Pros: cada catálogo usa el patrón natural a su naturaleza. Contras: dos modelos en el mismo servicio.

**Una vez decidido, implementar:** todos los ítems de la Sección 2 dependen de esta respuesta. El plan está redactado **asumiendo opción A** porque es la única consistente con el stack documentado y con `Apply hygiene en agregados event-sourced` del CLAUDE.md.

---

### D2. Granularidad de los agregados — ¿catálogo-como-agregado o registro-como-agregado?

**Decisión requerida:** ¿`Pais` es un agregado independiente por país (195 streams) o existe un agregado raíz `CatalogoDePaises` que contiene 195 entradas internas? Lo mismo para `Moneda`, `DivisionTerritorial` (¿1 stream por país con todas sus divisiones, o 1 por división?), `TipoDocumentoIdentidad`, `TasaCambio`.

**Opciones identificadas:**
- **A. Stream por registro** — 195 streams para países, 154 para monedas, 1.188 para divisiones-CO, etc. Pros: identidad de dominio coincide con identidad de stream (`StreamIdentity.AsString` con `CO`, `COP`, etc., ya configurado en `HostTestFactory.cs`); cada Apply trivial; commands pequeños. Contras: gran cantidad de streams; protección de unicidad (V7, V9) requiere proyección + port.
- **B. Stream por catálogo** — un agregado `CatalogoDePaises` con 195 entradas internas. Pros: invariantes de unicidad nativas dentro del agregado. Contras: agregado masivo (~1.188 divisiones-CO en un stream); replays costosos; viola "tamaño máximo orientativo" de CLAUDE.md.
- **C. Stream por catálogo y país** (solo para divisiones y tipos doc) — un agregado por (catálogo, país). Pros: balance — tamaño manejable y unicidad nativa dentro del país. Contras: combina dos esquemas.

**Una vez decidido, implementar:** el formato de los archivos `Crear*Handler.cs`, las identidades, y el plan de proyecciones cambian. **El plan asume opción A** (stream por registro) por consistencia con `StreamIdentity = StreamIdentity.AsString` ya configurado y con el patrón general de DDD/ES del CLAUDE.md.

---

### D3. ¿Identidad del stream — `Guid.CreateVersion7()` o código de dominio (string)?

**Decisión requerida:** CLAUDE.md establece "`Guid.CreateVersion7()` — nunca `Guid.NewGuid()`". El `HostTestFactory.cs` configura `StreamIdentity = StreamIdentity.AsString`. La identidad natural de los catálogos es el código (`CO`, `COP`, `05001`, etc.). ¿Se usa el código como `StreamKey` o se mantiene un Guid v7 con el código como atributo?

**Opciones identificadas:**
- **A. `StreamKey = codigo`** — más natural para datos de referencia; alinea identidad de dominio con identidad técnica; permite endpoints `/paises/CO` directos. La regla "Guid v7" del CLAUDE.md aplica al *Id de comando*, no al StreamKey.
- **B. `StreamKey = Guid.CreateVersion7()` y codigo como atributo** — consistente con el patrón general; require índice en proyección para resolver por código.

**Una vez decidido, implementar:** los handlers de `Crear` y la firma de los comandos cambian. **El plan asume opción A** (StreamKey = codigo) porque es consistente con `StreamIdentity.AsString` ya configurado y con la identidad inmutable documentada.

---

### D4. Enforcement de V10 — ¿solo soft-delete o protección activa?

**Decisión requerida:** V10 dice *"un registro referenciado por otro servicio o dominio no se puede eliminar — solo inactivar"*. Pero Datos de Referencia no sabe quién lo referencia. ¿La protección se materializa como (a) eliminación dura prohibida por diseño — solo existe `Inactivar` —, o (b) eliminación dura permitida solo si el dominio puede confirmar no-referencias (vía Inbox de eventos de eliminación entrantes)?

**Opciones identificadas:**
- **A. Sin eliminación dura** — solo `Inactivar*`. La invariante se cumple vacuamente. Es la lectura literal de §6.
- **B. Eliminar permitido si nadie lo referencia** — requiere un domain service `IVerificadorDeReferenciasExternas` que consulte a Terceros, OXP, etc. Sobre-ingeniería para datos estáticos.

**Una vez decidido, implementar:** los ítems 4, 7, 10 (inactivar) cubren la opción A. La opción B agrega N comandos `Eliminar*` y un domain service. **El plan asume opción A.**

---

### D5. Rutas REST y forma de los DTOs de respuesta

**Decisión requerida:** §3.1 lista las operaciones funcionalmente pero no documenta las URLs. ¿`/paises`, `/paises/{codigo}`, `/paises/{codigo}/divisiones?nivel=municipio`? ¿O `/divisiones?paisCodigo=CO&nivel=municipio`? ¿Los DTOs de respuesta exponen `monedaPrincipal` como código (`"COP"`) o como objeto anidado?

**Opciones identificadas:**
- **A. Recursos planos con query string** — `/divisiones?paisCodigo=CO&nivel=municipio`. Más flexible.
- **B. Recursos jerárquicos** — `/paises/CO/divisiones?nivel=municipio`. Más REST-puro.

**Una vez decidido, implementar:** ítems 17 y 18 (Carter modules + acceptance tests).

---

### D6. ¿Modelo de dominio formal (`modelo-dominio.md`) o derivación implícita desde alcance + especificación?

**Decisión requerida:** El resto de los sub-dominios (Obligaciones, Impuestos, Contabilidad, Terceros) tienen `modelo-dominio.md` con agregados, eventos, FSM, invariantes formalmente documentados. Datos de Referencia solo tiene alcance + especificación. ¿Se redacta `modelo-dominio.md` como insumo previo a la implementación, o se deja la forma DDD como decisión técnica del equipo de desarrollo?

**Opciones identificadas:**
- **A. Generar `Definiciones/compartido/datos-referencia/modelo-dominio.md` antes de empezar** — formaliza D1-D4 y elimina ambigüedad. Coherente con el estándar de los demás dominios.
- **B. Implementar derivando de alcance+especificación** — más rápido pero introduce divergencia entre dominios.

**Una vez decidido, implementar:** si A, este es el primer ítem antes que cualquier otro. Si B, las decisiones D1-D5 se documentan dentro de `AIResume/` como decisiones técnicas del equipo.

---

## Sección 4 — Ítems que requieren especificación

### S1. Sincronización automática de tasas de cambio (PD1)

**Por qué la documentación es insuficiente:** §8 declara el pendiente literalmente — "Definir el mecanismo para obtener la TRM diaria del Banco de la República y Banco Central RD". El anexo-estrategia menciona "API o scraping programado" como categoría, sin detalles.

**Preguntas que deben responderse:**
1. ¿API REST oficial, scraping HTML, archivo descargable diario, o servicio de terceros (Open Exchange Rates / Fixer.io)?
2. ¿Trigger interno (cron en el propio servicio vía Wolverine schedule, Quartz.NET, BackgroundService) u orquestador externo (cron de Kubernetes, Hangfire)?
3. ¿Qué pares de monedas se cargan? ¿Solo TRM USD↔COP / USD↔DOP / USD↔PAB? ¿O todos los pares activos?
4. ¿Cómo se manejan fines de semana, festivos y días sin publicación? ¿Se reusa la última, se omite, se marca?
5. ¿Cómo se manejan fallos? ¿Retry exponencial, alerta operativa, fallback a carga manual?
6. ¿Se publica un evento público `TasaSincronizada` o se guarda silencioso?

### S2. Estructura concreta del modelo de dominio (modelo-dominio.md)

**Por qué la documentación es insuficiente:** D6 lo describe — falta el documento que define explícitamente los agregados, sus comandos, eventos, FSM (si los hay), invariantes locales vs. eventuales, y domain services del servicio Datos de Referencia.

**Preguntas que deben responderse:**
1. Lista canónica de agregados.
2. Por cada agregado: comandos, eventos, payload exacto, invariantes locales.
3. Domain services y sus puertos (V2, V3, V6, V7, V8, V9 cruzan agregados).
4. ¿Existe FSM para algún catálogo (ej. `Activo` ↔ `Inactivo` de tasas — ¿hay reactivación?)?
5. ¿Cómo se modela "código inmutable" técnicamente — sin comando `Modificar` que toque el código, o con verificación explícita?

### S3. Contratos REST exactos (rutas, query strings, DTOs, paginación)

**Por qué la documentación es insuficiente:** §3 enumera operaciones pero no contratos HTTP.

**Preguntas que deben responderse:**
1. URLs canónicas (D5).
2. ¿Hay paginación? Catálogo de tipos doc tiene 45 entradas (no requiere); catálogo de divisiones-CO tiene 1.188 (probablemente sí).
3. Estructura del DTO de respuesta — ¿plano o anidado?
4. ¿Filtros por `activo` (¿incluyen inactivos por default?, ¿filtro `?activo=false`?)?
5. ¿Códigos HTTP — 404 cuando no existe, 200 con array vacío cuando no hay resultados?

### S4. Contratos gRPC (si aplica)

**Por qué la documentación es insuficiente:** El alcance no menciona gRPC. La existencia de los proyectos `Cosmos.DatosReferencia.Comandos.MCP.Server` / `Consultas.MCP.Server` (skeletons) sugiere que MCP sí está en mente, pero gRPC no aparece. Otros sub-dominios del ERP (Terceros, OXP, Impuestos) probablemente quieran consumir Datos de Referencia.

**Preguntas que deben responderse:**
1. ¿Datos de Referencia expone canal gRPC para consumidores internos del ERP?
2. Si sí: ¿qué operaciones, qué `.proto`, qué versionado?
3. Si no: ¿REST + caché local en cada consumidor es la única vía?

---

## Sección 5 — Regresiones detectadas

Ninguna. No existe un diagnóstico previo (`AIResume/DiagnosticoYPlanDominio.md`) ni código del dominio anterior — el proyecto está en estado inicial. Esta es la versión 1.0 del diagnóstico.

---

## Changelog

| Versión | Fecha | Descripción |
|---|---|---|
| 1.0 | 2026-05-07 | Diagnóstico inicial: 5 catálogos documentados, 0 implementados. 26 ítems en el plan (1 directo de limpieza, 17 con decisión de diseño, 6 candidatos diferidos, 1 con especificación pendiente, 1 placeholder). 6 decisiones de diseño bloqueantes (D1-D6). 4 especificaciones faltantes (S1-S4). 1 gold-plating (`ProductCreated.cs` de template). |
