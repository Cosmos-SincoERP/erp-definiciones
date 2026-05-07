# Guía: Arquitectura dirigida por eventos (EDA)

## Propósito

Guía para entender los patrones de comunicación en una arquitectura dirigida por eventos. Aplica a todos los sub-dominios del ERP.

---

## 1. Principio rector: los eventos son ciudadanos de primera clase

> **EDA no significa "todo es asíncrono".** Significa que los eventos de dominio son el mecanismo principal de comunicación entre bounded contexts, y que el estado del sistema se deriva de hechos que ocurrieron.

Un evento de dominio es un hecho inmutable que ya ocurrió: `RegistroTributarioCreado`, `OxpComercioConfirmada`. No es una solicitud ni una instrucción — es una declaración de lo que pasó.

---

## 2. Tipos de evento

| Tipo | Alcance | Consumidor | Acoplamiento | Ejemplo |
|------|---------|------------|-------------|---------|
| **Evento de dominio** | Interno al bounded context | Proyecciones, read models, procesos internos | Bajo — solo el BC lo conoce | `ReglaDeAplicacionCreada` |
| **Evento de integración** | Cruza fronteras de bounded context | Otros bounded contexts | Medio — contrato público | `DesgloseConfirmado` (Impuestos → OXP) |

### Regla de diseño

- Los eventos de dominio son **internos** — su estructura puede cambiar sin afectar otros bounded contexts.
- Los eventos de integración son **contratos públicos** — su estructura es estable y versionada. Cambios requieren compatibilidad hacia atrás.
- **Nunca** exponer un evento de dominio directamente como evento de integración. Si un hecho interno tiene relevancia para otros, se publica un evento de integración separado con la información necesaria (y solo esa).

---

## 3. Patrones de comunicación

EDA soporta múltiples patrones de comunicación. La elección depende de si el emisor necesita una respuesta inmediata.

### 3.1. Request/Reply síncrono

```
Consumidor ──solicitud──► Servicio
Consumidor ◄──respuesta──  Servicio
```

**Cuándo usarlo:** El consumidor necesita el resultado para continuar su flujo. No puede avanzar sin la respuesta.

**Características:**
- El consumidor espera la respuesta antes de continuar.
- No genera estado ni eventos (o genera solo si la operación es exitosa).
- Tiempo de respuesta predecible.

**Ejemplo ERP:** OXP solicita cálculo tributario al Motor → recibe desglose propuesto → lo muestra al usuario. Sin el desglose, la pantalla no puede renderizar.

**Implementación típica:** REST, gRPC, o request/reply sobre mensajería (con correlation ID y timeout).

### 3.2. Comando asíncrono (fire-and-forget con garantía)

```
Emisor ──comando──► Broker ──entrega──► Consumidor
                                            │
                                            ▼
                                       Procesa y emite
                                       evento de dominio
```

**Cuándo usarlo:** El emisor notifica un hecho y no necesita confirmación inmediata. El consumidor procesará cuando pueda.

**Características:**
- El emisor continúa sin esperar.
- Garantía de entrega (at-least-once).
- El consumidor debe ser **idempotente** — puede recibir el mismo mensaje más de una vez.
- Desacopla temporalmente al emisor del consumidor.

**Ejemplo ERP:** OXP confirma transacción → publica comando `ConfirmarRegistroTributario` → Impuestos lo procesa y crea `RegistroTributarioCreado`. OXP ya siguió su flujo.

**Implementación típica:** Message broker (Kafka, RabbitMQ, SQS) con consumer groups.

### 3.3. Evento de dominio (publicación interna)

```
Agregado ──emite evento──► Event Store
                               │
                     ┌─────────┼─────────┐
                     ▼         ▼         ▼
                 Proyección  Read     Proceso
                 interna     Model    interno
```

**Cuándo usarlo:** Un hecho ocurrió dentro del bounded context y otras partes internas necesitan reaccionar.

**Características:**
- El evento se persiste en el event store como parte del stream del agregado.
- Las proyecciones lo consumen para construir read models.
- Los procesos internos (sagas) lo consumen para coordinar flujos.

**Ejemplo ERP:** `RegistroTributarioCreado` → proyección interna actualiza el read model de registros por período → queda disponible para consulta de entregables.

### 3.4. Evento de integración (publicación entre bounded contexts)

```
Bounded Context A                    Bounded Context B
       │                                    │
  Evento de dominio                         │
       │                                    │
  Publicador ──evento integración──► Broker ──► Consumidor B
  (transforma                               │
   dominio → integración)                   ▼
                                       Reacciona
```

**Cuándo usarlo:** Un hecho de un bounded context tiene relevancia para otro bounded context.

**Características:**
- Contrato público y versionado.
- Payload mínimo: solo lo que el consumidor necesita.
- El consumidor decide qué hacer con el evento — el emisor no lo sabe ni le importa.

**Ejemplo ERP:** Impuestos publica `PerfilTributarioActualizado` → Gestión de Terceros lo consume para mostrar en la ficha del tercero que sus datos fiscales fueron actualizados, sin duplicar el perfil.

---

## 4. Patrón mixto: la realidad de un bounded context

Un bounded context típico combina los cuatro patrones según la naturaleza de cada interacción:

```
┌──────────────────────────────────────────────────────────┐
│  Sub-dominio consumidor (OXP)                            │
│                                                          │
│  1. Solicita cálculo ──[síncrono]──► Motor de Cálculo    │
│     ◄── Desglose propuesto ◄────────                     │
│                                                          │
│  2. Usuario trabaja con el desglose                      │
│                                                          │
│  3. Confirma transacción ──[comando async]──► Impuestos  │
│                                                          │
│                          Impuestos (bounded context)     │
│                          ┌──────────────────────┐        │
│                          │ RegistroTributario    │        │
│                          │ Creado                │        │
│                          │   │ [evento dominio]  │        │
│                          │   ├──► Proyección     │        │
│                          │   │    interna        │        │
│                          │   │                   │        │
│                          │   └──► [evento        │        │
│                          │        integración]   │        │
│                          │        RegistroFiscal │        │
│                          │        Confirmado ──► │──► OXP │
│                          └──────────────────────┘        │
└──────────────────────────────────────────────────────────┘
```

---

## 5. Idempotencia: requisito no negociable en asíncrono

Toda operación que reciba mensajes asíncronos **debe ser idempotente**: procesar el mismo mensaje dos veces debe producir el mismo resultado que procesarlo una vez.

| Mecanismo | Descripción |
|-----------|-------------|
| **Idempotency key** | El mensaje incluye un identificador único. El consumidor registra los IDs procesados y rechaza duplicados. |
| **Detección por estado** | El consumidor verifica el estado actual antes de aplicar. Si ya está en el estado esperado, descarta. |
| **Optimistic concurrency** | El event store rechaza escrituras si la versión del stream no coincide con la esperada. |

---

## 6. Consistencia eventual: el trade-off fundamental

> **Consistencia fuerte (transaccional):** garantía inmediata de que todos ven el mismo estado. Solo dentro de un agregado.
>
> **Consistencia eventual:** garantía de que todos **llegarán** al mismo estado, pero puede haber una ventana donde no coinciden. Entre agregados y entre bounded contexts.

| Situación | Tipo de consistencia | Justificación |
|-----------|---------------------|---------------|
| Crear registro tributario con contenido mínimo (R24) | Fuerte — dentro del agregado RegistroTributario | La invariante es local |
| OXP confirma → Impuestos crea registro | Eventual — entre bounded contexts | Cada uno opera autónomo; si Impuestos está caído, OXP no se bloquea |
| Entregable consulta registros para generar reporte | Eventual — lectura de proyección | El read model puede estar milisegundos atrás |

**Regla de diseño:** La consistencia fuerte solo se justifica dentro de un agregado para proteger invariantes locales. Todo lo demás es eventual.

---

## 7. Cuándo NO usar eventos

| Situación | Mejor alternativa | Razón |
|-----------|-------------------|-------|
| Consulta de datos de referencia (catálogos, jurisdicciones) | Query síncrona / Read model | No hay hecho de negocio — es una lectura |
| Validación que necesita respuesta inmediata (perfil tributario existe) | Query síncrona | El comando no puede proceder sin saber el resultado |
| Operación dentro del mismo agregado | Lógica interna del agregado | No necesita coordinación externa |

---

## Historial

| Versión | Cambio |
|---------|--------|
| v1 | Versión inicial: patrones de comunicación EDA, tipos de evento, idempotencia, consistencia eventual. |
