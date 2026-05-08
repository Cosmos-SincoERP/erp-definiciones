Ejecuta un análisis mecánico del dominio especificado en `$ARGUMENTS` identificando datos, comportamientos e implementaciones que deberían tener impacto funcional pero no lo tienen. Genera `AIResume/PlanFalencias.md` con cada falencia en formato autosuficiente para que pueda ejecutarse en una sesión independiente.

**Uso:** `/find-falencias Definiciones/mi-dominio/`

Si no se provee `$ARGUMENTS`, usar `AskUserQuestion` para solicitar la ruta antes de continuar.

> Este comando analiza lo que **está implementado pero no funciona correctamente o está incompleto**. Para detectar qué falta construir desde cero, usar `/analisis-plan-dominio`.

---

## Paso 0 — Orientación

```bash
ls *.sln
ls -d */
```

Identificar:
- **Nombre de la solución** y proyectos presentes (patrones estándar: `*.Dominio/`, `*.Dominio.Tests/`, `*.Consultas/`, `*.Comandos.API/`, `*.Consultas.API/`, `*.Seed/`, etc.)
- **Archivos de documentación** en `$ARGUMENTS`
- **Estado previo**: verificar si existe `AIResume/PlanFalencias.md`

---

## Paso 1 — Leer la documentación del dominio

Leer **todos** los archivos `.md` en `$ARGUMENTS`. El objetivo es construir los inventarios del Paso 2 — no "leer y recordar".

Extraer y retener explícitamente:

- Todos los agregados con sus **entidades internas y atributos** (composición completa)
- Todos los **comportamientos calculados** de cada agregado (métodos públicos documentados)
- Todos los **domain services** con sus pasos de pipeline
- Todos los **eventos** con su **payload completo** campo por campo
- Todos los **enums** con sus valores (especialmente: `MotivoExclusion`, `Efecto`, estados de FSM, etc.)
- Todas las **invariantes** `[I*]` con su clasificación (local / eventual)
- Todas las **proyecciones** documentadas con sus campos
- Los **pendientes por definir** `[PD*]` — una falencia bloqueada por un PD* no es implementable hasta que se resuelva ese pendiente

---

## Paso 2 — Construir los inventarios mecánicos

**Este paso es el núcleo del comando.** Las falencias silenciosas (datos persistidos sin consumo, métodos documentados sin implementación) no se detectan con lectura superficial — requieren recorrer mecánicamente el inventario completo.

### Lista A — Propiedades de entidades y value objects

Por cada entidad interna y VO de cada agregado documentado, listar **todas** sus propiedades. Incluir las que aparecen en los diagramas de composición y en las tablas de atributos.

```
Agregado [Nombre]:
  Entidad [Nombre]:
    - prop1: tipo
    - prop2: tipo
    - ...
  VO [Nombre]:
    - prop1: tipo
    - ...
```

### Lista B — Comportamientos calculados

Por cada agregado y domain service, listar los métodos descritos en sus tablas de "Comportamiento calculado":

```
Agregado [Nombre]:
  - nombreMetodo(params) → tipo retorno
  - ...
Service [Nombre]:
  - paso1: descripción
  - paso2: descripción
  - ...
```

### Lista C — Valores de enums, motivos y efectos

Por cada enum del dominio (estados de FSM, `MotivoExclusion`, `Efecto`, `FactorDeTarifa`, etc.), listar todos sus valores:

```
Enum [Nombre]: Valor1, Valor2, Valor3, ...
```

### Lista D — Invariantes

Por cada invariante `[I*]`, registrar:

```
I[N]: [texto]
  Clasificación: local / eventual
  Debe enforcearse en: [lugar según clasificación]
```

### Lista E — Proyecciones y sus campos

Por cada proyección documentada:

```
Proyección [Nombre]:
  Campos del read model: campo1, campo2, ...
  Fuente de eventos: [agregados]
```

---

## Paso 3 — Identificar falencias mediante verificación mecánica

Los siete criterios no pueden evaluarse "a vista". Para cada uno existe un procedimiento explícito.

### Criterio 1 — Datos almacenados sin uso funcional

**Procedimiento** (sub-paso 3.2): Para **cada propiedad** de la Lista A, grep en el código y clasificar cada coincidencia encontrada:

- **Escritura**: asignación en `Apply`, factory, construcción de entidad o evento.
- **Propagación**: lectura en handler para pasar al evento, lectura en mapper para pasar a request/response DTO, lectura en proyección para pasar al read model.
- **Consumo de decisión**: lectura para tomar una decisión — comparación (`==`, `!=`, `<`, `>`), branch condicional (`if`, `switch`, `?:`), filtro (`.Where`, `.Any`, `.FirstOrDefault`), validación que puede lanzar excepción, o cálculo que afecta un valor de salida.

> **Distinción crítica**: una propiedad que solo tiene escritura + propagación (se persiste y se copia) pero **ningún consumo de decisión** es una falencia. El dato existe en el sistema pero no influye en ningún comportamiento observable.

Si solo hay escritura y/o propagación → **Falencia Criterio 1**.

### Criterio 2 — Comportamientos especificados no implementados

**Procedimiento** (sub-paso 3.3): Para **cada método** de la Lista B, verificar en el código del componente correspondiente:

- ¿Existe un método público con esa firma o equivalente semántico?
- Si no existe, ¿algún componente externo (service, handler) cumple esa función?
- Si nadie la cumple → **Falencia Criterio 2**.

> La búsqueda no es por nombre literal — un método documentado como `configuracionVigenteA(fecha)` puede estar implementado con otro nombre que cumpla la misma función. Verificar semánticamente.

### Criterio 3 — Pipeline incompleto

**Procedimiento**: Para **cada domain service** de la Lista B con pasos de pipeline documentados:

- Mapear los pasos existentes en el código.
- Identificar pasos documentados que no tienen implementación.
- Verificar que el orden de los pasos respeta las dependencias documentadas.
- Identificar pasos que existen pero producen un resultado diferente al documentado.

Si falta un paso o el resultado es incorrecto → **Falencia Criterio 3**.

### Criterio 4 — Proyecciones faltantes o incompletas

**Procedimiento**: Para **cada proyección** de la Lista E:

- ¿Existe la clase de proyección en `*.Consultas/`?
- ¿El read model tiene **todos** los campos documentados? (comparar Lista E contra propiedades del record)
- ¿Está registrada con `ProjectionLifecycle.Async`?
- ¿Tiene query handler? ¿Tiene endpoint?
- ¿El query handler usa el campo correcto para la búsqueda documentada?

Si falta cualquiera de estos → **Falencia Criterio 4**.

### Criterio 5 — Invariantes no enforced

**Procedimiento**: Para **cada invariante** de la Lista D:

- Buscar en el código el mecanismo de enforcement correspondiente a su clasificación:
  - **Local** → debe estar en un `throw` dentro del agregado o en el property initializer del VO (no en el handler, no en el endpoint).
  - **Eventual** → debe estar en una validación al escribir + proyección de detección tardía.
- ¿El lugar de enforcement coincide con la clasificación documentada?
- ¿La condición de la invariante es exactamente la que describe el documento?

Si no está enforced o está en el lugar incorrecto → **Falencia Criterio 5**.

### Criterio 6 — Datos redundantes

**Procedimiento**: Buscar estructuras que duplican información ya disponible en otro lugar sin agregar valor. Tres patrones concretos a revisar:

**6a — T-Identity Surrogate**: VO/record auxiliar que solo encapsula campos de identidad de otro agregado, sin lógica propia.
```bash
# Buscar records con pocos campos usados como clave de búsqueda
grep -rn "new.*Localizador\|new.*Key\|new.*Clave\|new.*Referencia" --include="*.cs" *.Dominio/ *.Dominio.Store/
```
Para cada resultado, verificar si el tipo auxiliar tiene métodos propios o solo wrappea campos de otro objeto.

**6b — Campos calculables**: propiedades almacenadas que son derivadas de otros campos del mismo objeto y podrían ser propiedades calculadas.
```bash
# Campos que parecen derivados (nombres como Total, Suma, Resultado, etc.)
grep -rn "public.*Total\b\|public.*Suma\b\|public.*Resultado\b" --include="*.cs" *.Dominio/
```
Verificar si el valor se puede obtener a partir de otros campos del mismo objeto — si es así, no necesita almacenarse.

**6c — Proyecciones redundantes**: proyecciones que replican el estado del agregado sin agregar ninguna vista nueva (mismo campo por campo).
Comparar los campos del read model contra los del agregado — si son idénticos y no hay filtros ni proyecciones adicionales, es redundante.

Si hay duplicación sin valor adicional → **Falencia Criterio 6**.

### Criterio 7 — Dead code derivado

**Procedimiento** (sub-paso 3.4): Para **cada valor de enum** de la Lista C:

- ¿Existe al menos un sitio en el código que lo **emita** (asigne, construya, retorne)?
- Un valor que nunca se asigna → la lógica que lo produciría está ausente.
- Especial atención a: motivos de exclusión, efectos, estados de FSM — un valor de FSM nunca alcanzable es dead code aunque exista en el enum.

Si un valor nunca se emite → **Falencia Criterio 7**.

---

## Paso 4 — Documentar cada falencia

Para cada falencia identificada:

**4.1** Verificar si está bloqueada por un `[PD*]` del modelo. Si sí, marcar como "Requiere especificación — bloqueada por PD[N]" y no incluir en la sección de directamente implementables.

**4.2** Clasificar en una de tres categorías:

**Directamente implementable** — la documentación es suficiente. Se pueden escribir los tests en rojo ahora mismo sin hacer preguntas adicionales.

**Con decisión de diseño** — la documentación describe la necesidad pero hay al menos un aspecto de implementación que requiere una decisión no documentada. Identificar la pregunta exacta.

**Requiere especificación** — el comportamiento esperado no está definido. No se pueden escribir los tests porque no se sabe qué debe hacer el sistema (incluye falencias bloqueadas por `[PD*]`).

**4.3** Asignar ID en formato `F-NNN` (comenzar desde `F-001`, incrementar por cada nueva falencia).

---

## Paso 5 — Comparar con plan previo (si existe)

Si existe `AIResume/PlanFalencias.md`:

**Importante — evitar el sesgo de continuidad:** el plan previo NO es autoritativo. Si declara "0 falencias abiertas", no significa que no las haya — significa que en la ejecución anterior no se encontraron. Los sub-pasos mecánicos del Paso 3 **siempre** se ejecutan desde cero antes de consultar el plan previo.

Al comparar:
- Falencias del plan previo que siguen abiertas → conservar con su ID original.
- Falencias del plan previo ya resueltas → marcarlas `✅ RESUELTA` con fecha.
- Falencias nuevas no encontradas antes → asignar nuevo ID `F-NNN`.
- Falencias del plan previo que ya no aplican → marcarlas como `❌ DESCARTADA` con razón.

---

## Paso 6 — Generar `AIResume/PlanFalencias.md`

Crear o sobreescribir con la siguiente estructura:

```markdown
# Plan de Falencias — [Nombre del dominio]

**Fecha:** [git log -1 --format=%ci]
**Rama:** [git branch --show-current]
**Documentación analizada:** `$ARGUMENTS`

---

## Resumen

| Categoría | Cantidad |
|---|---|
| Directamente implementables | N |
| Con decisión de diseño | N |
| Requieren especificación | N |
| Resueltas desde versión anterior | N |
| **Total falencias abiertas** | **N** |

---

## Sección 1 — Contexto del análisis

### Inventarios construidos

**Lista A — Propiedades analizadas:** N propiedades en N entidades/VOs
**Lista B — Comportamientos verificados:** N métodos en N componentes
**Lista C — Valores de enum verificados:** N valores en N enums
**Lista D — Invariantes verificadas:** N invariantes
**Lista E — Proyecciones verificadas:** N proyecciones

### Hallazgos por criterio

| Criterio | Falencias encontradas |
|---|---|
| 1. Datos sin uso funcional | N |
| 2. Comportamientos no implementados | N |
| 3. Pipeline incompleto | N |
| 4. Proyecciones faltantes/incompletas | N |
| 5. Invariantes no enforced | N |
| 6. Datos redundantes | N |
| 7. Dead code derivado | N |

---

## Sección 2 — Falencias directamente implementables

### F-NNN — [Nombre de la falencia]

**Criterio:** [1–7]
**Archivos afectados:** [lista con rutas]

**Explicación**
Qué hace el sistema actualmente y por qué es incorrecto o insuficiente.
Qué no puede funcionar como consecuencia de esta falencia.

**Respaldo en la documentación**
> "[cita textual que especifica el comportamiento esperado]"
> — `[archivo].md`, Sección [N.N]

**Ejemplo**
- Estado actual: [qué sucede hoy con datos concretos]
- Estado esperado: [qué debería suceder según la documentación]
- Cómo detectarlo: [test o verificación que confirma la falencia]

**Qué hay que implementar**
- `[Proyecto/Carpeta/Archivo.cs]`: [qué crear o modificar]
- `[Proyecto/Carpeta/Archivo.cs]`: [qué crear o modificar]

---

**Plan de implementación** *(consumido por `/implementar-falencia F-NNN`)*

**Fase 1 — Tests en rojo:**
- [ ] `Si_[Estado]_Debe_[Outcome]` — verifica [comportamiento principal]
- [ ] `Si_[LímiteExacto]_Debe_[Outcome]` — mata mutante `<` → `<=`
- [ ] `Si_[CasoNegativo]_NoDebe_[Outcome]` — caso que no activa

**Fase 2 — Implementación:**
- [ ] [paso de implementación 1]
- [ ] [paso de implementación 2]

**Fase 3 — Verificación:**
- [ ] `dotnet test --filter FullyQualifiedName~[ClaseDeTest]` — todos verdes
- [ ] [verificación adicional si aplica]

---

## Sección 3 — Falencias con decisión de diseño pendiente

### F-NNN — [Nombre]

**Criterio:** [1–7]
**Archivos afectados:** [lista]

**Explicación**
[...]

**Respaldo en la documentación**
[...]

**Ejemplo**
[...]

**Decisión requerida**
[Pregunta concreta que debe responderse antes de implementar]

**Opciones:**
- Opción A: [descripción + tradeoff]
- Opción B: [descripción + tradeoff]

**Una vez decidido, implementar:**
- [ ] [Fase 1 — Tests]
- [ ] [Fase 2 — Implementación]
- [ ] [Fase 3 — Verificación]

---

## Sección 4 — Falencias que requieren especificación

### F-NNN — [Nombre]

**Criterio:** [1–7]
**Bloqueada por:** `[PD-N]` si aplica

**Por qué la documentación es insuficiente:** [explicación]
**Preguntas que deben responderse:**
1. [pregunta]
2. [pregunta]

---

## Sección 5 — Falencias resueltas

| ID | Nombre | Fecha de cierre |
|---|---|---|
| F-NNN | [Nombre] | [fecha] |

---

## Changelog

| Versión | Fecha | Descripción |
|---|---|---|
| 1.0 | [fecha] | Análisis inicial: N falencias encontradas. |
| 1.1 | [fecha] | [qué cambió] |
```

---

## Paso 7 — Mostrar resumen al usuario

Presentar:

1. **Tabla de hallazgos por criterio** — cuántas falencias por tipo.
2. **Top 3 falencias de mayor impacto** — las que bloquean más comportamiento funcional.
3. **Falencias bloqueadas por PD*** — si las hay, son señal de que el modelo tiene decisiones pendientes que el equipo debe resolver antes de continuar.
4. **Falencias con decisión de diseño** — cuántas y cuáles son las preguntas pendientes.
5. **Próximo paso sugerido**: `/implementar-falencia F-001`.

---

## Consideraciones

- **No inventar:** toda falencia debe estar respaldada por evidencia en el código o en la documentación. Si algo parece un gap pero no hay evidencia clara, no incluirlo.
- **No asumir fases:** verificar explícitamente en la documentación la clasificación `[F1]`/`[F2]` de cada capacidad. Las capacidades de fase futura no son falencias activas.
- **Distinción gap vs falencia:** un gap es algo que falta construir (pertenece a `DiagnosticoYPlanDominio.md`). Una falencia es algo construido que no tiene impacto funcional correcto o completo.
- **Los sub-pasos mecánicos son obligatorios:** evaluar los criterios "a vista" pierde las falencias silenciosas. Si los sub-pasos 3.2, 3.3 y 3.4 no se ejecutaron sobre los inventarios de la Lista A, B y C, el análisis está incompleto.
- **Cada falencia en PlanFalencias.md debe ser autosuficiente:** `/implementar-falencia F-NNN` se ejecuta en una sesión independiente que no tiene el contexto de esta sesión. La cita, el ejemplo y el plan de fases deben ser suficientes sin releer la documentación completa.
