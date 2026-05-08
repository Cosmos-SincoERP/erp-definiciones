# Revisión de Conformidad con el Modelo Documentado

Detecta implementaciones que existen y funcionan, pero divergen de lo que la documentación de dominio especifica como correcto. Genera `AIResume/docs/conformidad-modelo/reporte.md`.

**Uso:** `/revision-conformidad-modelo Definiciones/mi-dominio/`

Si no se provee `$ARGUMENTS`, usar `AskUserQuestion` para solicitar la ruta antes de continuar.

---

## Propósito y distinción con otros comandos

Existen tres ejes ortogonales de revisión:

| Eje | Pregunta | Comando |
|---|---|---|
| **Ausencias** | ¿Qué no está construido? | `/find-falencias` |
| **Calidad técnica** | ¿Qué está construido pero el código puede mejorarse? | `/revision-codigo` |
| **Conformidad** | ¿Qué está construido y funciona pero diverge del modelo documentado? | **Este comando** |

Los hallazgos de este comando son una clase distinta: el código puede pasar revisión técnica y no estar marcado como ausente, pero ser semánticamente incorrecto respecto al modelo. Una implementación puede ser limpia técnicamente y aun así implementar el patrón equivocado.

Ejemplos concretos que **ninguno de los otros dos detecta:**
- Un `Apply` puro y determinista que pone `Activa = false` cuando la convención documentada de cierre exige acotar `Vigencia.Hasta`.
- Un comando `Modificar` cuyo payload permite cambiar la `Vigencia`, contradiciendo el patrón documentado donde la vigencia es identificadora del hecho.
- Un evento de cierre con payload insuficiente para producir el efecto documentado (no lleva la fecha de cierre necesaria para acotar la vigencia).
- Dos entidades análogas del modelo que implementan el cierre de formas diferentes sin justificación documentada.

---

## Paso 0 — Orientación

```bash
ls *.sln
ls -d */
```

Identificar proyectos presentes (patrones estándar: `*.Dominio/`, `*.Consultas/`, `*.Comandos.API/`, etc.) y archivos de documentación en `$ARGUMENTS`.

---

## Paso 1 — Leer la documentación del dominio

Leer **todos** los archivos `.md` en `$ARGUMENTS`. Extraer y retener explícitamente:

**Convenciones documentadas:**
- Convenciones de nomenclatura de verbos en comandos y eventos (`Cerrado` vs `Desactivado` vs `Eliminado` — cuándo usa cada uno)
- Convenciones de payload de eventos (qué campos debe llevar cada tipo de evento)
- Convenciones de comandos (qué permite modificar, qué es inmutable)

**Patrones documentados:**
- Cómo se modela el cierre temporal (¿acotar `Vigencia.Hasta`? ¿flag `Activo`? ¿borrado lógico?)
- Cómo se modela una modificación (¿qué campos son modificables? ¿qué es identificador del hecho y no puede cambiar?)
- Cómo se modela una agregación (¿qué datos lleva el momento de inicio vs el evento de cierre?)
- Cómo se filtran datos históricos en consultas (¿los cerrados siguen siendo válidos dentro de su rango?)

**Decisiones de diseño** `[D*]` — cada decisión describe un comportamiento esperado.

**Reglas de negocio** `[R*]` — especialmente las transversales que aplican a múltiples componentes.

**Invariantes** `[I*]` — con su clasificación (local / eventual) y el lugar correcto de enforcement.

**Premisas** `[P*]` — principios que condicionan múltiples componentes.

**Patrones cruzados** — cuando dos o más entidades comparten una característica (ej. ambas tienen `Vigencia` VO, ambas son configuración con dos orígenes), deben implementar las operaciones análogas de forma consistente.

---

## Paso 2 — Construir el mapa de conformidad

Para cada componente del modelo (agregado, entidad, VO, comando, evento, domain service, proyección) construir:

| Componente | Convenciones aplicables | Patrones aplicables | Decisiones aplicables | Reglas/Invariantes/Premisas | Análogos en el modelo |
|---|---|---|---|---|---|
| `[Nombre]` | [lista citada] | [lista citada] | [IDs D*] | [IDs R*, I*, P*] | [componentes con misma característica] |

Este mapa es la base de la comparación. Sin él, las divergencias se detectan ad-hoc y se pierden las cruzadas.

---

## Paso 3 — Leer el estado actual del código

Para cada componente del mapa, leer los archivos relevantes. No limitarse al archivo de la entidad principal — seguir las dependencias hasta donde el comportamiento se completa:

`comando → handler → método del agregado → evento → Apply → filtro de lectura → query handler`

Para cada elemento leer:
- **Comando**: payload, qué permite cambiar
- **Evento**: payload, nombre, semántica
- **Apply**: qué muta y cómo
- **Entidad/VO**: estructura, propiedades, semántica de cierre/modificación
- **Handler**: qué orquesta
- **Domain service**: pipeline, qué invoca
- **Proyección**: qué traduce, filtros de consulta

---

## Paso 4 — Detectar divergencias

Para cada componente del mapa, contrastar la implementación actual contra:

### 4.1 Conformidad de convenciones

- ¿El **verbo del comando** coincide con la convención documentada según las características de la entidad? (Ej: entidad con `Vigencia` VO debe usar `Cerrar`, no `Desactivar`)
- ¿El **nombre del evento** sigue la convención del verbo del comando?
- ¿El **payload del evento** es suficiente para producir el efecto documentado?

### 4.2 Conformidad de patrones

- ¿La **semántica del cierre** coincide? Si la convención dice "acotar el rango temporal", ¿el `Apply` acota `Vigencia.Hasta` o usa un mecanismo paralelo (flag, borrado lógico)?
- ¿La **semántica de la modificación** respeta la inmutabilidad de los identificadores temporales? Si el modelo trata `Vigencia` como identificadora del hecho, ¿el comando `Modificar` permite cambiarla?
- ¿La **agregación** recibe solo los datos del momento de inicio, dejando el cierre para un evento dedicado?
- ¿El **filtro de consulta histórica** respeta que datos cerrados siguen siendo válidos dentro de su rango?

### 4.3 Conformidad cruzada entre entidades análogas

Cuando dos o más entidades comparten una característica documentada (ej. ambas tienen `Vigencia` VO, ambas son configuración estándar/personalizado), sus operaciones análogas deben implementarse del mismo modo.

Para cada par de análogos verificar:
- ¿El cierre se implementa igual?
- ¿La modificación tiene el mismo alcance de payload?
- ¿La agregación tiene la misma forma?
- ¿El filtro histórico se comporta igual?
- ¿El verbo y el nombre del evento siguen la misma convención?

Si una entidad lo implementa de una forma y la análoga de otra → divergencia. Reportar ambas: la incorrecta y la referencia.

### 4.4 Conformidad con decisiones de diseño `[D*]`

Para cada decisión `[D*]`:
- ¿La implementación la refleja?
- ¿Hay implementaciones que la contradicen aunque sea parcialmente?
- ¿La decisión aplica transversalmente y la implementación solo la honra en algunos componentes?

### 4.5 Conformidad con reglas `[R*]` y premisas `[P*]`

Para cada regla o premisa con aplicación transversal:
- ¿La implementación la respeta en todos los componentes que deberían respetarla?
- ¿Hay un componente que la viola aunque no sea su propietario directo?

### 4.6 Conformidad de invariantes `[I*]`

Para cada invariante:
- ¿Está enforced en el código?
- ¿Está enforced en el **lugar correcto** según su clasificación?
  - **Local** → property initializer del VO o método público del agregado
  - **Eventual** → validación al escribir + proyección de detección tardía
- ¿Hay invariantes implícitas en la documentación que no están enunciadas como `[I*]` pero deben respetarse?

---

## Paso 5 — Documentar cada divergencia

Cada divergencia sigue este formato de cuatro partes obligatorio:

### [N]. [Nombre corto]

**Categoría:** Convención | Patrón | Payload | Decisión | Regla/Premisa | Invariante | Consistencia cruzada
**Componente:** [nombre del componente afectado]
**Archivos afectados:** [lista con líneas si aplica]
**Impacto observable:** [escenario real donde el comportamiento es incorrecto, o "cosmético" si no hay impacto]

**Explicación**
Qué hace el código actualmente y por qué eso no coincide con lo que el modelo dice que debe hacer. Quién se ve afectado y qué no puede funcionar correctamente. La explicación debe ser autosuficiente — alguien que no haya visto la documentación ni el código debe entender qué está mal y por qué importa.

**Respaldo en la documentación**
> "[cita textual del documento]"
> — `[nombre-archivo].md`, Sección [N.N]

> "[segunda cita si hay decisión o invariante relevante]"
> — `[nombre-archivo].md`, `[D*]` / `[R*]` / `[I*]`

Sin cita textual no hay divergencia válida — la cita es la prueba de que el problema es contra el modelo y no contra preferencia personal.

**Ejemplo**
Escenario concreto y reproducible:
- Datos de entrada: [valores concretos]
- Qué hace el sistema hoy: [comportamiento actual]
- Qué debería hacer según la documentación: [comportamiento esperado]
- Por qué el resultado actual es incorrecto: [consecuencia observable]

**Qué hay que cambiar en la implementación**
Por cada componente afectado:

`[Archivo.cs]` (líneas si aplica)
Qué hace hoy → qué debe hacer → por qué el cambio cierra la divergencia respecto a lo documentado.

Si hay análogos del modelo que ya implementan correctamente el patrón, citarlos como referencia.

---

## Paso 6 — Clasificar las divergencias

| Categoría | Descripción |
|---|---|
| **Convención** | El código usa nombres, verbos o estructuras que contradicen una convención documentada |
| **Patrón** | El código implementa un patrón documentado pero con semántica desviada |
| **Payload** | El evento o comando tiene payload insuficiente o demasiado permisivo |
| **Decisión** | La implementación contradice una decisión `[D*]` documentada |
| **Regla/Premisa transversal** | La implementación viola una regla `[R*]` o premisa `[P*]` que aplica a múltiples componentes |
| **Invariante** | Una invariante `[I*]` no está enforced o lo está en el lugar incorrecto |
| **Consistencia cruzada** | Dos componentes análogos se implementan de forma divergente sin justificación |

---

## Paso 7 — Verificar contra reportes anteriores

Si existe `AIResume/docs/conformidad-modelo/reporte.md`:

- Marcar `✅ RESUELTA` cualquier divergencia anterior que la implementación actual ya corrigió.
- Marcar `🔁 REGRESIÓN` cualquier divergencia que vuelve a aparecer después de haber sido resuelta.
- Identificar divergencias nuevas que no estaban antes.

---

## Paso 8 — Generar `AIResume/docs/conformidad-modelo/reporte.md`

```markdown
# Reporte de Conformidad con el Modelo — [Nombre del dominio]

**Fecha:** [git log -1 --format=%ci]
**Rama:** [git branch --show-current]
**Documentación analizada:** `$ARGUMENTS`

---

## Resumen

| Categoría | Nuevas | Resueltas | Regresiones |
|---|---|---|---|
| Convención | N | N | N |
| Patrón | N | N | N |
| Payload | N | N | N |
| Decisión | N | N | N |
| Regla/Premisa | N | N | N |
| Invariante | N | N | N |
| Consistencia cruzada | N | N | N |
| **Total** | **N** | **N** | **N** |

---

## Sección 1 — Mapa de conformidad

| Componente | Convenciones | Patrones | Decisiones | Reglas/Inv./Prem. | Análogos |
|---|---|---|---|---|---|
| `[Nombre]` | | | | | |

---

## Sección 2 — Divergencias detectadas

### [N]. [Nombre]

[formato del Paso 5]

---

## Sección 3 — Divergencias resueltas

### ✅ [Nombre]
[descripción de cómo se resolvió y en qué fecha]

---

## Sección 4 — Regresiones

### 🔁 [Nombre]
[descripción de la regresión y cuándo se había resuelto previamente]
```

---

## Paso 9 — Mostrar resumen al usuario

1. **Tabla resumen** por categoría (nuevas / resueltas / regresiones).
2. **Regresiones** (si las hay) — prioridad máxima, requieren atención inmediata.
3. **Top 3 divergencias de mayor impacto** — las que afectan comportamiento observable, no cosmética.
4. **Divergencias cosméticas** (naming, convención sin impacto funcional) — listadas pero sin urgencia.
5. **Próximo paso sugerido**: cada divergencia se implementa con `/implementar "[nombre de la divergencia]"`.

---

## Consideraciones

- **Cada divergencia debe estar respaldada por una cita textual.** Sin cita no es divergencia válida — puede ser una opinión de diseño que pertenece a `/revision-codigo`.
- **No es una ausencia.** Si algo no está hecho del todo, va en `/find-falencias`. Aquí solo entran cosas hechas que divergen.
- **No es calidad técnica.** Si el código podría estar mejor pero no hay nada en la documentación que lo respalde, va en `/revision-codigo`.
- **Verificar análogos cruzados activamente.** El modelo está diseñado con consistencia transversal — entidades con la misma característica deben implementar las mismas operaciones análogamente.
- **No inferir convenciones no documentadas.** Si la documentación no establece una convención, no es divergencia que la implementación elija un camino. Solo se reporta lo que choca con algo escrito.
