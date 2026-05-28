# Anexo — Decisión de diseño: Orquestación de la creación de unidades organizacionales

> **Fecha:** 2026-04-24
> **Versión:** 1.0
> **Propósito:** Documentar cómo se orquesta la creación de una unidad organizacional cuando la solicitud se origina desde un sub-dominio consumidor (OXP, Contabilidad, etc.) en lugar de desde el propio módulo de Estructura Organizacional.

---

## 1. Problema

Una unidad organizacional puede crearse por dos caminos:

| Camino | Quién inicia | Cuándo ocurre |
|--------|--------------|---------------|
| **Creación directa** | Administrador de estructura organizacional | La empresa decide abrir una sucursal, lanzar un proyecto, crear un centro de costo. El administrador entra al módulo de Estructura Organizacional y lo registra. |
| **Solicitud desde consumidor** | Usuario operativo de OXP, Contabilidad u otro consumidor | Durante la operación de un sub-dominio aparece la necesidad de una unidad que no existe (ej: llega una factura asociada a un proyecto recién aprobado pero aún no registrado; o Contabilidad detecta líneas de traducción que referencian una unidad faltante). |

El primer camino es trivial: el administrador usa la UI del propio módulo. El segundo es el que requiere decisión arquitectónica, porque en una arquitectura de microservicios con EDA hay que decidir **cómo un sub-dominio consumidor origina la creación sin acoplar su modelo al de Estructura Organizacional**.

En SincoA&F esto no es un problema porque el "CRUD de centros de costo" vive dentro del mismo sistema contable. En la nueva arquitectura de microservicios, ¿cómo se mantiene una experiencia unificada para el usuario operativo sin que OXP conozca detalles internos de Estructura Organizacional?

---

## 2. Alternativas evaluadas

### Alternativa A — El frontend llama a cada servicio directamente

La UI de OXP hace una llamada a Estructura Organizacional para crear la unidad, luego continúa con su propio flujo.

| Aspecto | Evaluación |
|---------|-----------|
| Experiencia del usuario | Regular — si la creación falla, el flujo de OXP queda a medias y el usuario debe reintentar manualmente. |
| Complejidad en el frontend | Alta — cada módulo debe manejar la secuencia, los errores parciales y la consistencia entre su propio flujo y la creación de la unidad. |
| Acoplamiento | El frontend de cada consumidor conoce el contrato de creación de Estructura Organizacional. Si cambia, todos los frontends se tocan. |

**Descartada.** Mueve la complejidad de orquestación al frontend y genera fragilidad.

### Alternativa B — El consumidor (OXP) crea en su propio modelo y luego notifica

OXP guarda la unidad en su propio modelo interno y eventualmente notifica a Estructura Organizacional.

| Aspecto | Evaluación |
|---------|-----------|
| Acoplamiento | Muy alto — OXP contamina su modelo con lógica de estructura organizacional. |
| Gobierno | Se pierde — la estructura queda distribuida entre múltiples sub-dominios. |
| Fuente de verdad | Se rompe — Estructura Organizacional deja de ser dueña del dato. |

**Descartada.** Contradice el principio de que Estructura Organizacional es el owner de las unidades y su ciclo de vida.

### Alternativa C — BFF (Backend for Frontend) + estado `Borrador` en Estructura Organizacional ✅

Una capa BFF compone la experiencia del usuario operativo. La solicitud de creación llega al sub-dominio de Estructura Organizacional, que valida sus reglas propias y crea la unidad en estado `Borrador` (no transaccional). El administrador, o una regla del sistema inteligente cuando el contexto sea suficientemente claro, la activa posteriormente.

| Aspecto | Evaluación |
|---------|-----------|
| Experiencia del usuario | Buena — el usuario operativo continúa su flujo sin conocer detalles internos de Estructura Organizacional. |
| Acoplamiento | Bajo — OXP emite un comando genérico y recibe eventos; no conoce el modelo interno. |
| Responsabilidad | Respetada — Estructura Organizacional valida y gestiona el ciclo de vida. OXP solo referencia. |
| Gobierno | Preservado — el administrador retiene control sobre qué entra a la estructura como `Activa`. |

**Seleccionada.** Es el mismo patrón usado por el sub-dominio de Terceros (ver `../terceros/anexo-decision-orquestacion-registro.md`) y por los ERPs de referencia que separan solicitud de confirmación (Workday mediante su modelo de `Proposed` → `Active`).

---

## 3. Flujos soportados

### Flujo 1 — Creación directa por el administrador

El administrador de estructura organizacional entra al módulo, crea la unidad con todos sus datos (código, nombre, tipo, padre en la jerarquía) y la deja en estado `Borrador` o la activa inmediatamente según corresponda. No hay orquestación multi-servicio — es una interacción directa con el módulo dueño del dato.

```
  Administrador en el módulo de Estructura Organizacional
       │
       │  Crear unidad (código, nombre, tipo, padre)
       ▼
  ┌────────────────────────┐
  │ Estructura Organizacional│ → Valida código único, tipo válido,
  │                        │   padre existente, jerarquía coherente.
  │                        │   Crea la unidad en estado Borrador
  │                        │   (o Activa si el administrador
  │                        │    decide activarla en el mismo paso).
  └────────────────────────┘
       │
       ▼
  Evento UnidadCreada (+ UnidadActivada si aplica)
       │
       ▼
  Consumidores (OXP, Contabilidad, ...) reciben y replican referencia local.
```

### Flujo 2 — Solicitud originada desde un sub-dominio consumidor

```
  Usuario operativo en OXP (radicando una obligación)
       │
       │  El sistema inteligente detecta que el documento
       │  referencia una unidad que no existe.
       │  Sugiere primero unidades similares existentes.
       │  Si ninguna aplica, ofrece "solicitar creación".
       ▼
  UI muestra formulario de solicitud con campos pre-llenados
  (tipo sugerido, padre probable, nombre canónico, etc.)
       │
       │  Usuario confirma
       ▼
  ┌─────────────────┐
  │       BFF       │
  │ (Backend for    │
  │  Frontend)      │
  └────────┬────────┘
           │
           │  Comando SolicitarCreacionDeUnidad
           ▼
  ┌──────────────────────────┐
  │ Estructura Organizacional│ → Valida código único, tipo válido,
  │                          │   padre existente, jerarquía coherente.
  │                          │   Crea la unidad en estado Borrador.
  └────────────┬─────────────┘
               │
               ▼
  Evento UnidadCreada (estado Borrador)
               │
               ▼
  OXP continúa con su flujo: referencia la unidad
  pero el sistema advierte que está en Borrador y
  no es operable todavía — la obligación queda en
  un estado acorde (p.ej. "pendiente por unidad").
               │
               ▼
  Administrador revisa la solicitud en su bandeja,
  completa datos faltantes y activa la unidad
  (o rechaza con justificación).
               │
               ▼
  Evento UnidadActivada
               │
               ▼
  OXP reacciona al evento y la obligación puede
  ahora imputarse normalmente.
```

### Secuencia detallada del Flujo 2

| Paso | Componente | Acción |
|:----:|------------|--------|
| 1 | UI del consumidor (OXP, etc.) | Detecta la necesidad de una unidad inexistente. El sistema inteligente sugiere similares existentes; si no aplican, ofrece el formulario de solicitud pre-llenado. |
| 1.5 | BFF | **(Verificación previa, best-effort)** consume el endpoint `verificarDisponibilidadCodigo(codigo)` del servicio. Si el código ya está en uso, el sistema inteligente sugiere alternativa y el flujo regresa al paso 1 sin enviar el comando. |
| 2 | BFF | Recibe la solicitud del frontend y la convierte en un comando `SolicitarCreacionDeUnidad` hacia Estructura Organizacional. |
| 3 | Estructura Organizacional | Valida sus reglas propias (unicidad del código en el tenant, tipo válido, padre existente, coherencia jerárquica) — el servicio es la autoridad de unicidad, no confía en la verificación del BFF. Si pasa, crea la unidad en estado `Borrador` y emite `UnidadCreada`. Si falla con `codigo-no-disponible` (race concurrente), retorna el error al BFF que vuelve al paso 1 con sugerencia. |
| 4 | Consumidor (OXP) | Escucha `UnidadCreada`, replica la referencia local y marca que la unidad está en `Borrador`. La operación del consumidor (radicar obligación, generar borrador contable, etc.) queda en un estado que refleja esa dependencia. |
| 5 | Administrador | Revisa la solicitud pendiente (unidades en `Borrador`), completa datos faltantes si los hay, y activa o rechaza. |
| 6 | Estructura Organizacional | Al activar, emite `UnidadActivada`. Los consumidores reaccionan y destraban sus operaciones dependientes. |

#### Verificación de disponibilidad de código en BFF

Para optimizar la experiencia del usuario, el sub-dominio de Estructura Organizacional expone un endpoint de solo lectura del tipo:

```
GET /unidades/verificar-disponibilidad-codigo?codigo=<codigo>&tenantId=<tenantId>
→ { disponible: boolean, sugerencia?: codigo }
```

El BFF lo consume en el paso 1.5 antes de enviar `SolicitarCreacionDeUnidad`. Características:

- **Best-effort:** la verificación NO bloquea la unicidad. El servicio sigue validando al recibir el comando (paso 3) vía `[SI01]`.
- **Sin garantía transaccional:** si dos clientes verifican simultáneamente y obtienen "disponible", solo uno logra crear; el otro recibe `codigo-no-disponible` al hacer submit. El BFF maneja ese error con la misma lógica de sugerencia del paso 1.5.
- **Autoridad única:** el servicio sigue siendo la fuente de verdad de unicidad. El BFF NO mantiene proyección local del dominio — solo consume el endpoint.
- **UX:** el sistema inteligente puede sugerir código alternativo inmediatamente sin esperar el round-trip completo del submit.

Esta decisión cierra el gap detectado en la auditoría sobre el contrato BFF → Domain Service.

---

## 4. Manejo de errores

| Escenario | Qué pasa | Cómo se resuelve |
|-----------|----------|------------------|
| Paso 3 falla — validación rechaza | La unidad no se crea. El BFF retorna el error al frontend. | El sistema inteligente sugiere corrección (ej: "el código ya existe, ¿te referías a X?"). El usuario ajusta y reintenta. |
| Paso 5 nunca ocurre — administrador no activa | La unidad queda en `Borrador` indefinidamente. La operación del consumidor queda pendiente. | El sub-dominio de Estructura Organizacional puede emitir alertas de antigüedad (ej: "borradores con más de N días sin revisar"). Eventualmente el administrador actúa o rechaza. |
| Paso 5 es rechazo del administrador | La unidad pasa a `Inactiva` (o se elimina del modelo si aún no tuvo ningún evento relevante — decisión que se tomará en el modelo de dominio). El consumidor reacciona al evento `UnidadRechazada` (o al evento de inactivación) y cancela su operación dependiente. | El usuario operativo es notificado con la razón del rechazo y puede elegir otra unidad o escalar. |
| Solicitudes duplicadas (dos usuarios solicitan la misma unidad casi simultáneamente) | La validación de unicidad del código en el paso 3 rechaza la segunda solicitud. | El BFF del segundo usuario devuelve un error específico y el sistema inteligente sugiere usar la unidad ya en `Borrador` en lugar de crear una nueva. |

### Principio de diseño

La creación desde un consumidor **nunca activa la unidad automáticamente**. El estado `Borrador` (Decisión 2 del anexo `anexo-decisiones-arquitectonicas.md`) es el vehículo explícito de la consistencia eventual entre la solicitud y la activación. Esto protege el gobierno de la estructura: ningún flujo operativo puede introducir unidades `Activas` sin pasar por la revisión del administrador (o por una regla explícita del sistema inteligente que asuma la activación cuando el contexto sea suficientemente claro).

El consumidor que originó la solicitud sabe que la referencia está en `Borrador` y refleja esa dependencia en su propia operación — por ejemplo, OXP puede admitir la referencia pero marcar la obligación como "pendiente por unidad organizacional" hasta que llegue `UnidadActivada`.

---

## 5. Rol del sistema inteligente

El sistema inteligente es una pieza clave del flujo (no un componente opcional). Sus responsabilidades en el contexto de creación son:

| Momento | Qué hace el sistema inteligente |
|---------|--------------------------------|
| Antes de solicitar | Busca unidades existentes similares al contexto del documento (por nombre fonético, código, tipo, padre probable, tercero asociado). Sugiere opciones antes de permitir crear una nueva. Evita proliferación de unidades ad-hoc equivalentes. |
| Al solicitar | Pre-llena los campos del formulario con base en el contexto (tipo inferido del documento, padre más probable, nombre canónico sugerido). Reduce el esfuerzo del usuario operativo. |
| Durante la revisión del administrador | Agrupa solicitudes relacionadas, detecta posibles duplicados entre solicitudes y existentes, sugiere reutilización. |
| Para activación automática (futuro) | En casos donde el contexto es inequívoco (ej: proyecto aprobado en el sistema de presupuesto con todos los datos necesarios), puede activar la unidad sin intervención humana. En F1 la activación es siempre humana; esta capacidad se habilita en fases posteriores. |

---

## 6. ¿Qué es y qué no es el BFF?

### Es

- Una capa intermedia entre el frontend del consumidor y los microservicios.
- Específica para la experiencia de usuario de cada sub-dominio consumidor (el BFF de OXP es distinto al de Contabilidad, aunque compartan patrones).
- Responsable de componer llamadas y coordinar la secuencia cuando un flujo de usuario toca varios sub-dominios.
- El lugar donde se maneja el error y se decide qué mostrar al usuario.

### No es

- Un servicio de negocio — no tiene reglas de negocio propias ni gestiona ciclo de vida.
- Un reemplazo de los servicios — Estructura Organizacional sigue siendo quien valida y crea.
- Parte de ningún dominio — es infraestructura de aplicación.
- Necesario para la comunicación entre servicios — solo existe para la experiencia de usuario. La comunicación servicio-a-servicio sigue siendo vía eventos (EDA).

---

## 7. Referencia de la industria

| Sistema | Cómo resuelve la creación de unidades desde flujos consumidores |
|---------|----------------------------------------------------------------|
| **SAP S/4HANA** | Monolítico: el maestro de cost centers se crea desde transacciones estándar (KS01). No hay orquestación multi-servicio porque todo vive en el mismo sistema. |
| **Oracle Fusion** | Similar a SAP: los segmentos del Chart of Accounts se crean desde la aplicación de configuración. |
| **Workday** | Soporta `Proposed` → `Active` nativamente. Una unidad puede entrar como propuesta desde un workflow de `Staffing` o `Organization Change` y ser aprobada formalmente después. Este es el referente conceptual más cercano. |
| **Dynamics 365 Finance** | Valores de dimensión se crean desde la aplicación; no hay un flujo nativo para crearlos desde consumidores, aunque sí existe configuración de workflows de aprobación. |
| **Microservicios (patrón)** | BFF + API Composition + estado intermedio en el owner. El consumidor origina la solicitud; el owner la crea con estado no operable; un actor posterior (humano o automático) la activa. |

---

## 8. Impacto en los documentos de alcance

Esta decisión no cambia la responsabilidad del sub-dominio:

| Servicio | Sigue siendo responsable de |
|----------|-----------------------------|
| Estructura Organizacional | Crear, validar, gestionar el ciclo de vida (`Borrador`, `Activa`, `Suspendida`, `Inactiva`), gestionar la jerarquía, ejecutar reestructuraciones. |
| OXP, Contabilidad y demás consumidores | Solo referencian unidades existentes y reaccionan a sus eventos. Pueden originar solicitudes de creación sin conocer el modelo interno del owner. |

Lo que se agrega es la conciencia de que **existe una capa BFF** que compone la experiencia del usuario operativo cuando la solicitud se origina desde un flujo consumidor, y que el estado `Borrador` es el vehículo formal de la ventana de consistencia eventual entre la solicitud y la activación.

---

## 9. Pendientes

| # | Pendiente | Contexto |
|---|-----------|----------|
| PD01 | Política de activación por el sistema inteligente | Definir en qué condiciones el sistema inteligente puede activar una unidad solicitada sin intervención humana (ej: integración con sistema de presupuesto, aprobación gerencial documentada en el documento origen). En F1 la activación es siempre humana. **Estado: abierto.** Continúa como `[PD01]` del `modelo-dominio.md` v1.2. |
| PD02 | Política de expiración de `Borrador` | Definir si una unidad en `Borrador` por más de N días se marca como `Abandonada` automáticamente, y si esto dispara alguna notificación o acción. **Estado: CERRADO** por `[D09]` + `[SI05]` del `modelo-dominio.md`: proceso programado del sub-dominio emite `UnidadDescartada` con `motivoBaja: "abandono_por_inactividad"` tras umbral configurable por tenant (default sugerido 30 días). |
| PD03 | Comando explícito de rechazo | Decidir si existe un evento `UnidadRechazada` explícito o si el rechazo se modela como transición directa a `Inactiva` con razón documentada. **Estado: CERRADO** por `[D08]` del `modelo-dominio.md`: un solo evento `UnidadDescartada` cubre rechazo manual y abandono automático, diferenciados por el atributo `motivoBaja`. |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-24 | Versión inicial. Decisión BFF + API Composition con estado `Borrador` como vehículo de la consistencia eventual entre solicitud y activación. Dos flujos soportados (creación directa por administrador; solicitud desde sub-dominio consumidor). Referencia al sistema inteligente como pieza clave del flujo. |
| 1.1 | 2026-05-27 | **Aplicado bloque Media de la auditoría del modelo (Tema M10 y M11).** Naming estandarizado: `PD1/PD2/PD3` → `PD01/PD02/PD03`. Estado de pendientes actualizado: `PD02` y `PD03` marcados como CERRADOS por las decisiones `[D09]` y `[D08]` del `modelo-dominio.md` v1.2; `PD01` sigue abierto. Flujo F2 ampliado con paso 1.5 — verificación de disponibilidad de código en BFF mediante el endpoint `verificarDisponibilidadCodigo(codigo)` del servicio (best-effort; el servicio sigue siendo la autoridad única de unicidad; el BFF NO mantiene proyección local del dominio). Decisión M11 cerrada con Opción C (consulta a API del servicio). |
