# Guía: Topología de despliegue, equipos y backbone de eventos

## Propósito

Cómo decidir **hasta dónde separar físicamente** cada sub-dominio del ERP y cómo se comunican entre sí, sin confundir la frontera lógica (el bounded context) con la topología de despliegue ni con el empaque comercial. Aplica a todos los sub-dominios del ERP.

Surge de una observación recurrente: "separar" se trata como una sola decisión binaria cuando en realidad son varias decisiones independientes, cada una con un costo distinto. Mezclarlas lleva a pagar de más (separar todo desde el día uno) o a perder independencia (agrupar lo que debía evolucionar por separado).

Esta guía es la cara de **despliegue y equipos** del mismo problema que [`datos-entre-dominios.md`](datos-entre-dominios.md) trata en lo lógico: aquella define *cómo* un dominio consume datos de otro sin acoplarse; esta, *cómo* se empaquetan y operan esos dominios.

---

## 1. Tres decisiones que parecen una sola

El error común es tratar la separación como un interruptor único. Son tres decisiones **ortogonales** — ninguna se deriva de la otra:

| Decisión | Pregunta | No depende de… |
|---|---|---|
| **Frontera lógica vs. topología física** | ¿El bounded context obliga a un despliegue separado? | No. La independencia ya está en el diseño lógico (eventos + copia local), no la da la separación física. |
| **Topología vs. intermediario de mensajería** | ¿Separar servicios obliga a un broker específico? | No. Se pueden tener servicios totalmente separados sobre cualquier broker. El broker se elige por sus propios méritos. |
| **Empaque comercial vs. fronteras de servicio** | ¿Vender dos cosas juntas obliga a fusionarlas? | No. Que dos servicios siempre se vendan juntos no es razón para unirlos; que uno se venda solo no es, por sí mismo, razón para separarlo. |

> Tener fronteras lógicas duras **no** requiere segmentación física total: se pueden tener bounded contexts con esquema, código y contrato propios dentro de pocos despliegues. La frontera lógica es innegociable; la topología física es una decisión reversible de costo y operación.

---

## 2. La separación tiene capas, y cada una cuesta distinto

En vez de "separar sí o no", la pregunta correcta es **hasta qué capa invertir hoy**. Son cuatro, de la más barata a la más cara:

| Capa | Qué es | Costo |
|---|---|---|
| **1. Lógica** | Esquema, código y contrato propios por bounded context | Bajo (vive en el diseño) |
| **2. Unidad de despliegue** | Contenedor independiente por servicio | Bajo |
| **3. Operativa** | Pipeline, escalado, on-call y **equipo** propios por servicio | Alto |
| **4. Aislamiento de infra** | Cluster, broker o datos físicamente aislados | Muy alto |

> Las capas 1 y 2 van completas para todos los servicios desde el día uno (son baratas y habilitan la independencia). La capa 3 se enciende **servicio por servicio**, cuando cada uno cruza el umbral de "es producto con equipo real". La capa 4 solo cuando un requisito concreto la pida (cumplimiento, aislamiento de fallo, datos).

Lo firme desde el día uno:

1. **Un contenedor por servicio.**
2. **Esquema y datos propios por servicio.** Nadie toca la base del otro: solo eventos + copia local (ver [`datos-entre-dominios.md`](datos-entre-dominios.md)).
3. **Un único backbone de eventos compartido** (ver sección 4).

---

## 3. Criterios para graduar un servicio a operación independiente

Cuando dudes "¿a este servicio le doy operación/equipo/infra propios ya, o espero?", córrelo contra estos ocho criterios. Si varios dan "sí", separa; si casi todos dan "no", todavía no:

| # | Aspecto | Qué resuelve |
|---|---|---|
| 1 | **¿Producto vendible por sí solo?** | Si es un SKU propio → es producto y merece autonomía. |
| 2 | **Equipo (Conway)** | ¿Tiene o tendrá equipo dedicado que necesita autonomía? |
| 3 | **Escalado** | ¿Perfil de carga distinto al resto? |
| 4 | **Aislamiento de fallo** | ¿Que se caiga no debe tumbar a los demás? |
| 5 | **Cadencia de despliegue** | ¿Libera a su propio ritmo? |
| 6 | **Cumplimiento / datos** | ¿Requisito legal o de datos que obligue a aislar? |
| 7 | **Costo vs. beneficio hoy** | ¿Ya justifica pagar operación independiente? |
| 8 | **Cadencia de localización** | ¿Varía por país/jurisdicción en su propio reloj regulatorio? |

Los criterios 1–6 y 8 dicen *si en algún momento debe separarse*; el 7 dice *si es ahora o después*.

---

## 4. El backbone de eventos

La comunicación entre sub-dominios ya está resuelta por el diseño: es **asíncrona, por eventos** (Event-Carried State Transfer y copia local — ver [`datos-entre-dominios.md`](datos-entre-dominios.md)), **no por llamadas síncronas en caliente**. La consecuencia para infraestructura:

> El intermediario de mensajería es infraestructura **compartida y neutral** — es la columna vertebral que permite que los servicios sean independientes. **Uno solo, no uno por dominio.** Si cada dominio montara su propio broker, se reintroduciría el problema de integración (habría que puentear brokers entre sí). El patrón es: un backbone de eventos, un tópico por publicador, suscripciones por consumidor.

La elección del intermediario es una decisión **aparte de la topología**: se pueden tener los servicios totalmente separados comunicándose por cualquier broker. Se decide por sus propios méritos:

| | **Administrado** (ej. Azure Service Bus) | **Autooperado** (ej. RabbitMQ) |
|---|---|---|
| Quién lo opera | El proveedor | Un equipo propio (plataforma) |
| Carga operativa sobre los microequipos | Menor | Mayor (alguien sostiene la mensajería) |
| Cuándo conviene | Equipos pequeños, no se quiere operar mensajería, ya se está en la nube del proveedor | Necesidad de enrutamiento muy fino, on-prem, o control/costo a gran escala con experiencia propia |

> Un intermediario administrado encaja mejor cuando el gobierno de DevOps recae en los equipos de producto y no se quiere que cada microequipo cargue con operar infraestructura de mensajería.

---

## 5. Alternativas concretas de estructura (cómputo y mensajería)

"Separación física" suele mezclar **dos preguntas distintas** que conviene desenredar antes de decidir:

- **Cómputo:** ¿dónde corre cada servicio? (máquina / cluster / contenedor)
- **Mensajería:** ¿cómo viajan los eventos entre servicios? (el broker / bus)

"Cada servicio en su máquina" es una decisión de **cómputo**; "su propio broker" es una de **mensajería**. Una **no** implica la otra: se puede tener cada servicio en su propia máquina y, aun así, **un solo bus compartido**.

> Un **bus compartido no contradice** la separación física: es justamente lo que permite que servicios separados se hablen. No es una concesión ni un parche; es el patrón correcto. Lo problemático sería un **broker por servicio** (ver anti-patrones).

### Eje 1 — Cómputo (dónde corre cada servicio)

De más compartido/barato a más aislado/caro:

| Opción | Cómo se ve | Costo | Aislamiento |
|---|---|---|---|
| **A. Cluster compartido, un contenedor por servicio** | Un orquestador (tipo Kubernetes / Azure Container Apps) corre los contenedores; comparten el cómputo del cluster | Bajo | Lógico (contenedor), comparten máquina |
| **B. Cluster compartido, recursos reservados por servicio** | Mismo cluster, cada servicio con su cuota/nodo asignado | Medio | Mayor: un servicio pesado no ahoga a los otros |
| **C. Una máquina (VM) dedicada por servicio** | Cada servicio en su propia máquina | Alto | Físico por servicio |
| **D. Una suscripción/cuenta cloud por servicio** | Cada servicio en su propio compartimento de la nube | Muy alto | Máximo (facturación, accesos, todo separado) |

### Eje 2 — Mensajería (cómo viajan los eventos)

| Opción | Cómo se ve | Veredicto |
|---|---|---|
| **1. Un solo bus compartido** | Un broker (ej. un namespace de Azure Service Bus); cada servicio publica en **su** tópico y se suscribe a los que le interesan | ✅ Recomendado |
| **2. Bus compartido con aislamiento lógico** | Mismo broker, con tópicos/colas separados y permisos por dominio | ✅ Válido, más control de accesos |
| **3. Un broker por servicio + puentes** | Cada servicio con su propio broker, conectados entre sí | ❌ Anti-patrón: reintroduce el problema de integración |

### Cómo se combinan — la estructura recomendada

Los dos ejes se combinan libremente. La recomendación: separación lógica + de despliegue (un contenedor y datos propios por servicio, cualquiera de A–D) sobre **un solo bus compartido** (opción 1 o 2). El bus sigue siendo uno solo corra el contenedor en cluster compartido (A/B) o en máquina dedicada (C).

```
        ┌──────────── BUS DE EVENTOS COMPARTIDO (uno solo) ────────────┐
        │   tópico OXP · tópico Contab · tópico Impuestos · ...         │
        └───▲────────────▲──────────────▲──────────────▲───────────▲───┘
            │            │              │              │           │
        ┌───┴───┐   ┌────┴────┐   ┌─────┴────┐   ┌─────┴────┐  ┌───┴───┐
        │  OXP  │   │ Contab. │   │ Impuestos│   │ Terceros │  │  EO   │
        └───────┘   └─────────┘   └──────────┘   └──────────┘  └───────┘
        contenedor   contenedor    contenedor     contenedor   contenedor
        + datos      + datos       + datos        + datos      + datos
        propios      propios       propios        propios      propios
```

### Decisión verificada contra la infraestructura real (jun-2026)

Las cinco preguntas que esta sección dejaba abiertas ya tienen respuesta. Se validó el Terraform real de los repos `*.Infraestructura` (`ApplicationPlane`, `ObligacionesPorPagar`, `Cosmos.Impuestos`, `Cosmos.Contabilidad`, `Cosmos.Terceros`, `Cosmos.Asistente`) y los ADR del repo `architecture` —sobre todo `ADR-001` (BC aislados por VNet/RG/VM-Swarm) y `ADR-002` (Service Bus único cross-BC). **La clave es que la unidad de aislamiento físico es el bounded context, no el servicio.**

| # | Pregunta | Respuesta verificada |
|---|---|---|
| 1 | ¿VM dedicada (C) o cluster compartido (A/B)? | **Híbrido por nivel.** Cada BC = **1 VM dedicada con Docker Swarm** (opción C a nivel BC); dentro, los varios servicios del BC corren como contenedores que comparten esa VM-Swarm (opción A/B a nivel servicio). "VM por servicio" se descartó por costo (ADR-001). En prod la VM-Swarm pasa de single-node a 3 nodos (HA). |
| 2 | ¿Un bus compartido o uno por servicio? | **Un solo bus compartido para lo inter-BC** (ADR-002). El "bus federado por BC" se descartó explícitamente. |
| 3 | Si Azure SB: ¿un namespace con tópico por servicio? | **Un namespace, tópico por bounded context** (`<contexto>.events`), no por servicio. ⚠️ Esos tópicos están diseñados pero **aún no creados** en Azure/TF (solo existe el tópico de *provisioning de suscripciones*). |
| 4 | ¿Administrado o autooperado? | **Inter-BC: administrado** (Azure Service Bus, SKU Standard). **Intra-BC: autooperado** (RabbitMQ + Redis en la VM-Swarm de cada BC). Auth hoy por SAS; el plan es migrar a Managed Identity (ADR-004). |
| 5 | ¿BD separada por servicio? | **1 PostgreSQL Flexible por BC** (sin BD compartida — ADR-001) y **una base por servicio dentro del server** (ej. OXP: `entradasdb`, `radicaciondb`, `radicaciondb_vectorial`, `reconocimientodb`). Nadie toca la base de otro BC. |

**Dos niveles de mensajería, deliberados** (y ninguno es el anti-patrón de la sección 8):

- **Intra-BC** — entre los servicios de un mismo BC: RabbitMQ + Redis propios del BC, en su red overlay privada (`<bc>-internal`); no se exponen fuera del bounded context.
- **Inter-BC** — entre bounded contexts: **el único** Service Bus compartido, topic por BC, con MassTransit. Llamadas síncronas cross-BC prohibidas (ADR-001/ADR-002).

El RabbitMQ-por-BC **no** contradice el "un solo bus" de la sección 4: solo hace plomería interna del BC, no integración cross-BC.

### Cómo quedó montado (estado real verificado)

```
                    Usuarios del ERP (HTTPS)
                                │
                     ┌──────────▼──────────┐
                     │   Azure Front Door  │ edge (application-plane)
                     └──────────┬──────────┘
                                │  /api/*
                     ┌──────────▼──────────┐
                     │   VM Gateway YARP   │ Docker Swarm
                     │  (application-plane)│
                     └──────────┬──────────┘
                                │  enruta al BC que corresponde
          ┌──────────────┬──────┴───────┬──────────────┐
          ▼              ▼              ▼              ▼
    ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐
    │   BC OXP  │  │ Impuestos │  │Contabilid.│  │  Terceros │
    │  VM+Swarm │  │  VM+Swarm │  │  VM+Swarm │  │  VM+Swarm │
    │           │  │           │  │           │  │           │
    │ servicios │  │ servicios │  │ servicios │  │ servicios │
    │   del BC  │  │   del BC  │  │   del BC  │  │   del BC  │
    │ ········· │  │ ········· │  │ ········· │  │ ········· │
    │  RabbitMQ │  │  RabbitMQ │  │  RabbitMQ │  │  RabbitMQ │
    │  + Redis  │  │  + Redis  │  │  + Redis  │  │  + Redis  │
    │ (interno) │  │ (interno) │  │ (interno) │  │ (interno) │
    │ ········· │  │ ········· │  │ ········· │  │ ········· │
    │  Postgres │  │  Postgres │  │  Postgres │  │  Postgres │
    │ (N bases) │  │  (1 base) │  │  (1 base) │  │ (2 bases) │
    └─────┬─────┘  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘
          │              │              │              │
          │   publican / se suscriben a eventos de dominio
          ▼              ▼              ▼              ▼
    ┌─────┬──────────────┬──────────────┬──────────────┬─────┐
    │ Azure Service Bus — único, compartido (app-plane)      │
    │ administrado · Standard · topic por BC                 │
    │ <contexto>.events: diseñados (ADR-002), aún no creados │
    └─────┴──────────────┴──────────────┴──────────────┴─────┘

   Cada BC = 1 VNet + 1 RG + 1 VM-Swarm + 1 ACR + 1 Key Vault + 1 Postgres propios.
   Intra-BC: RabbitMQ/Redis en la red overlay privada del BC (no sale del BC).
   Inter-BC: solo por el Service Bus compartido (sin llamadas síncronas).
   (Asistente tiene su propia VM-Swarm con el mismo molde; EO aún no tiene infra.)
```

> **Pendiente real de implementación:** el backbone inter-BC está diseñado (ADR-002) pero los tópicos `<contexto>.events` todavía no existen en Azure ni en el Terraform activo (verificado 2026-05-14, vista `03-messaging-flow` del repo `architecture`). Hoy la infraestructura de cada BC está viva, pero los eventos de dominio entre BC aún no fluyen. Es el siguiente paso para que la integración cross-BC sea real.

---

## 6. Gobierno de DevOps

El gobierno de DevOps recae en el **equipo de producto** ("tú lo construyes, tú lo operas"), con el apoyo de un **equipo de plataforma** que diseña el mecanismo de aprovisionamiento (autoservicio, plantillas, guardarraíles) y lo entrega a los microequipos, de forma que cada equipo entienda la responsabilidad sobre cada componente que necesite aprovisionar.

La intención de la separación a futuro es que **microequipos administren los servicios**: un mismo equipo puede administrar varios (por ejemplo OXP y Contabilidad), o un equipo por servicio, según la carga. La separación lógica y de despliegue (capas 1 y 2) habilita ese reparto sin rehacer arquitectura; el reparto operativo (capa 3) se da cuando los equipos maduran.

---

## 7. Empaque comercial: el paquete mínimo vendible

El empaque comercial **no** define fronteras de servicio: define qué debe poder correr y licenciarse junto. Se resuelve por **licenciamiento / activación por configuración**, no por topología. El mismo conjunto de servicios separados se empaqueta como un SKU u otro prendiendo o apagando piezas.

- Un servicio que **no se vende solo** define que el paquete mínimo vendible lo incluye junto a otros — es un hecho de licenciamiento, no un mandato de fusionar servicios.
- Un servicio que **se vende solo** es producto por derecho propio (sube su prioridad para graduarse a operación independiente, criterio #1).

---

## 8. Anti-patrones

| Anti-patrón | Por qué es malo |
|---|---|
| **Creer que la frontera lógica obliga a separación física total** | Se paga operación e infra independientes antes de que un disparador real las justifique. |
| **Acoplar la elección del broker a la topología** | Falsa disyuntiva: la separación de servicios no exige un broker concreto; el broker se elige por sus méritos. |
| **Un broker por dominio** | Reintroduce el problema de integración (puentear brokers entre sí). El backbone debe ser uno solo. |
| **Fusionar servicios porque siempre se venden juntos** | Confunde empaque comercial con frontera de servicio; se pierde la independencia de equipo y cadencia. |
| **"Compartido" entendido como "acoplado"** | Dos servicios pueden co-ubicarse, pero cada uno conserva su esquema y su contrato; no se mezclan tablas. |
| **Encender la capa operativa antes de tiempo** | Pipeline/on-call/equipo dedicado por servicio cuesta; se paga cuando el servicio es producto, no antes. |

---

## 9. Caso aplicado: estado actual del ERP

**La unidad de separación es el bounded context, no el servicio** — cada BC empaca varios servicios (OXP, por ejemplo: Entradas, Radicación, Reconocimiento, Conciliación Inteligente, Notificaciones), todos en su propia VM con Docker Swarm. Hoy tienen infraestructura aprovisionada (repo `*.Infraestructura` + VM-Swarm propia) **OXP, Impuestos, Contabilidad, Terceros y Asistente**; **Estructura Organizacional (EO) todavía no tiene repo de infraestructura**. Cada BC es autónomo en recursos (VNet, RG, VM-Swarm, ACR, Key Vault y Postgres propios — ADR-001) y se comunica con los demás **solo** por el Service Bus compartido (ADR-002). La operación/equipo propios se gradúan BC por BC con los criterios de la sección 3. El montaje real verificado y las respuestas a las cinco preguntas de aprovisionamiento están en la sección 5.

Hechos comerciales que alimentan el análisis:

- **OXP no se vende solo** → el paquete mínimo vendible lo incluye junto a Contabilidad, Impuestos, Terceros y EO. El nicho principal controla gasto/costo (OXP); CXC con frecuencia no se activa porque no venden.
- **OXP + Contabilidad** es el paquete que normalmente se compra (núcleo comercial).
- **Contabilidad se puede comercializar sola** a futuro (sistema de consolidación de información) → es producto por derecho propio.
- **Impuestos** es el que más localización necesita al llevar el sistema a otros países → evoluciona en el reloj regulatorio de cada jurisdicción.

Mapa por servicio:

| Bounded context | Infra propia (repo + VM-Swarm + bus) | ¿Producto propio? | Operación / equipo propio |
|---|:---:|:---:|---|
| **OXP** | ✅ | No se vende solo (núcleo) | **Sí** — producto vivo, su equipo |
| **Contabilidad** | ✅ | **Sí** (consolidación) | **Temprano** — se gradúa con el núcleo |
| **Impuestos** | ✅ | **Sí** (en paquetes) | **Temprano + equipo propio** — el reloj regulatorio lo empuja primero |
| **Terceros** | ✅ | No (soporte de datos) | Tardío — pero listo para la 1.ª venta |
| **Asistente** | ✅ | No (transversal) | Según carga — no estaba en el set original de la guía |
| **EO** | ⏳ **pendiente** (sin repo `.Infraestructura` aún) | No (soporte de datos) | Tardío — pero en la ruta crítica de la 1.ª venta |

Matiz sobre "qué está vivo": como OXP no se vende sin los demás, los BC del núcleo están en la **ruta crítica de la primera venta** — todos deben estar listos para facturar. Lo que difiere no es *si* deben funcionar, sino *cuándo cada uno gana equipo y operación propios*: OXP ya; Impuestos y Contabilidad temprano por ser productos (Impuestos con equipo propio por el reloj regulatorio); Terceros y EO al final por ser soporte de datos. **EO es hoy la brecha concreta:** todavía le falta su repo `*.Infraestructura` para estar en línea con los demás.

---

## 10. Relación con otras guías

- **[`datos-entre-dominios.md`](datos-entre-dominios.md)** — la cara lógica del mismo problema: cómo un consumidor usa datos de otro dominio sin acoplar disponibilidad (dueño único, réplica local, eventos). Esta guía asume ese diseño y trata cómo se empaquetan y operan los servicios.
- **[`arquitectura-eda.md`](arquitectura-eda.md)** — los mecanismos de mensajería que el backbone de eventos usa.

---

## Historial

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Junio 2026 | Versión inicial. Surge del análisis de infraestructura de los sub-dominios del ERP. Consolida: las tres decisiones ortogonales (frontera lógica vs. topología, topología vs. broker, empaque vs. fronteras de servicio), las cuatro capas de separación con su costo, los ocho criterios para graduar un servicio a operación independiente, el backbone de eventos único y la elección administrado vs. autooperado, el gobierno de DevOps (producto + plataforma), el paquete mínimo vendible, anti-patrones y el caso aplicado del estado actual del ERP (cinco servicios en contenedores sobre un bus compartido). |
| 1.1 | Junio 2026 | Nueva sección 5 "Alternativas concretas de estructura (cómputo y mensajería)" para analizar con el equipo de plataforma: desenreda cómputo vs. mensajería, menú del eje de cómputo (A–D), menú del eje de mensajería (1–3), diagrama de la estructura recomendada (bus compartido único + contenedor y datos propios por servicio) y cinco preguntas para definir con plataforma a partir del aprovisionamiento real. Renumeradas las secciones siguientes. |
| 1.2 | Junio 2026 | Cierre de las cinco preguntas con la **infraestructura real verificada** (repos `*.Infraestructura` + ADR-001/ADR-002 del repo `architecture`). En la sección 5: las preguntas pasan a respuestas, se documentan los dos niveles de mensajería (intra-BC RabbitMQ/Redis autooperado, inter-BC Service Bus único administrado), nueva subsección "Cómo quedó montado" con el diagrama ASCII del estado real, y la advertencia de que los tópicos `<contexto>.events` están diseñados pero aún no creados. Sección 9 actualizada: la unidad de aislamiento es el **bounded context (con N servicios)**, no el servicio; set real de BC con infra (OXP, Impuestos, Contabilidad, Terceros, Asistente) y EO marcado como pendiente de repo `.Infraestructura`. |
