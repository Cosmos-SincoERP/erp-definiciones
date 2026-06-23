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

## 5. Cómputo y mensajería: dos decisiones distintas

"Separación física" suele mezclar **dos preguntas distintas** que conviene desenredar:

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

### Cómo se combinan

Los dos ejes se combinan libremente: separación lógica + de despliegue (un contenedor y datos propios por servicio, cualquiera de A–D) sobre **un solo bus compartido** (opción 1 o 2). El bus sigue siendo uno solo corra el contenedor en cluster compartido (A/B) o en máquina dedicada (C). El diagrama del estado actual está en la **sección 9**.

### Cómo está montado (verificado contra el Terraform real)

Validado contra el Terraform de los repos `*.Infraestructura` y los ADR del repo `architecture` (`ADR-001` BC aislados por VNet/RG/VM-Swarm; `ADR-002` Service Bus único cross-BC). **La unidad de aislamiento físico es el bounded context, no el servicio:**

- **Cómputo (C a nivel BC + A/B a nivel servicio):** cada BC = **1 VM dedicada con Docker Swarm**; dentro, sus varios servicios corren como contenedores que comparten esa VM-Swarm. "VM por servicio" se descartó por costo (ADR-001). En prod la VM-Swarm pasa de single-node a 3 nodos (HA).
- **Mensajería inter-BC (opción 1, administrado):** **un solo** Azure Service Bus (SKU Standard) en el application-plane, un tópico por bounded context (`<contexto>.events`), con MassTransit. El "bus federado por BC" se descartó; llamadas síncronas cross-BC prohibidas.
- **Mensajería intra-BC (autooperado):** cada host corre su propio RabbitMQ + Redis en red overlay privada (`<host>-internal`), solo para sus servicios; no sale del host. **No es** el anti-patrón de la sección 8 — no hace integración cross-BC.
- **Datos:** **1 PostgreSQL Flexible por host** (sin BD compartida — ADR-001), **una base por servicio/ocupante** dentro del server. Nadie toca la base de otro BC.
- **Auth del bus:** hoy SAS; el plan es migrar a Managed Identity (ADR-004).

Es decir, **dos niveles de mensajería deliberados**: inter-BC por el Service Bus único; intra-BC por el RabbitMQ/Redis de cada host. Ninguno es el anti-patrón de la sección 8.

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

## 9. Estado actual del ERP (cómo está montado hoy)

**La unidad de separación es el bounded context, no el servicio** — cada BC empaca varios servicios (OXP, por ejemplo: Entradas, Radicación, Reconocimiento, Conciliación Inteligente, Notificaciones), todos en una VM con Docker Swarm. Cada unidad de cómputo es autónoma en recursos (VNet, RG, VM-Swarm, ACR, Key Vault y Postgres — ADR-001) y se comunica con las demás **solo** por el Service Bus compartido (ADR-002). La operación/equipo propios se gradúan BC por BC con los criterios de la sección 3.

Hoy hay **cinco unidades de cómputo** desplegadas:

- **OXP, Impuestos, Contabilidad y Asistente** — cada uno en su **propia** VM-Swarm + Postgres.
- **Host compartido de soporte de datos** (repo `Cosmos.Terceros.Infraestructura`, prefijo `terc`) — **una sola** VM-Swarm + **un solo** Postgres que hospeda **tres ocupantes ya desplegados**: **Terceros** (`tercerosdb`), **Datos de Referencia** (servicio de catálogos, repo `Cosmos.DatosReferencia`, `datosreferenciadb`) y **Estructura Organizacional / EO** (`estructuraorganizacionaldb`).

```
                          Usuarios del ERP (HTTPS)
                                      │
                           ┌──────────▼──────────┐
                           │   Azure Front Door  │ edge (application-plane)
                           └──────────┬──────────┘
                                      │  /api/*
                           ┌──────────▼──────────┐
                           │   VM Gateway YARP   │ Docker Swarm (application-plane)
                           └──────────┬──────────┘
                                      │  enruta al BC que corresponde
          ┌─────────────┬─────────────┴─────────────┬─────────────┐
          ▼             ▼             ▼             ▼             ▼
    ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐
    │   BC OXP  │ │ Impuestos │ │Contabilid.│ │ Asistente │ │ Host terc │ ◄ HOST COMPARTIDO (repo Cosmos.Terceros.Infraestructura, prefijo terc)
    │ VM propia │ │ VM propia │ │ VM propia │ │ VM propia │ │ 1 VM Swarm│   ocupantes: Terceros · Datos de Referencia · EO  (ya desplegados)
    │           │ │           │ │           │ │           │ │           │   bases: tercerosdb · datosreferenciadb · estructuraorganizacionaldb
    │ servicios │ │ servicios │ │ servicios │ │ servicios │ │ servicios │   contenedores · redes · topics SEPARADOS  → no acoplados
    │   del BC  │ │   del BC  │ │   del BC  │ │  + OpenAI │ │  de los 3 │   (renombrar a Cosmos.SoporteDeDatos = decisión de naming pendiente)
    │ ········· │ │ ········· │ │ ········· │ │ ········· │ │ ········· │
    │  RabbitMQ │ │  RabbitMQ │ │  RabbitMQ │ │  RabbitMQ │ │  RabbitMQ │
    │  + Redis  │ │  + Redis  │ │  + Redis  │ │  + Redis  │ │  + Redis  │
    │ (interno) │ │ (interno) │ │ (interno) │ │ (interno) │ │ (interno) │
    │ ········· │ │ ········· │ │ ········· │ │ ········· │ │ ········· │
    │  Postgres │ │  Postgres │ │  Postgres │ │  Postgres │ │ 1 Postgres│
    │ (4 bases) │ │  (1 base) │ │  (1 base) │ │  (1 base) │ │  3 bases  │
    └─────┬─────┘ └─────┬─────┘ └─────┬─────┘ └─────┬─────┘ └─────┬─────┘
          │             │             │             │             │
          │   publican / se suscriben a eventos de dominio
          ▼             ▼             ▼             ▼             ▼
    ┌─────┬─────────────┬─────────────┬─────────────┬─────────────┬─────┐
    │ Azure Service Bus — único, compartido (application-plane)         │
    │ topics: oxp · impuestos · contabilidad · asistente ·              │
    │ terceros · datosreferencia · estructuraorganizacional  (.events)  │
    │ ⚠ tópicos diseñados (ADR-002), aún NO creados en Azure todavía    │
    └─────┴─────────────┴─────────────┴─────────────┴─────────────┴─────┘
```

### El host compartido de soporte de datos

Terceros, Datos de Referencia y EO son todos **soporte de datos** (baja carga, graduación tardía — criterio #7): no se justifica una VM dedicada por cada uno. Comparten el **host físico** (1 VM-Swarm + 1 Postgres) **sin quedar acoplados**, porque cada ocupante conserva:

- **Su base de datos** en el mismo servidor — nadie toca las tablas del otro.
- **Su contenedor y su red overlay** dentro del Swarm; cada repo de aplicación (`Cosmos.Terceros*`, `Cosmos.DatosReferencia`, `Cosmos.EstructuraOrganizacional*`) despliega sus servicios con el patrón `onboard-dotnet-repo`.
- **Su tópico en el bus** (`terceros.events`, `datosreferencia.events`, `estructuraorganizacional.events`) — se hablan solo por eventos, nunca en proceso (ADR-002), aunque compartan host.

Compartir hierro **≠** ser el mismo bounded context: EO y Datos de Referencia **no** son "parte de Terceros"; son BC/servicios propios que co-habitan el host.

**Consideraciones evaluadas:**

- **Trade-offs del host compartido:** se comparte *blast radius* (si la VM cae, caen los tres) y ventana de parcheo; y el host lo opera **un solo equipo**. Aceptable para ocupantes de soporte de datos de baja carga. Es una **excepción deliberada al ADR-001** ("1 VM por BC"), justificada por costo (criterio #7) y reversibilidad — el equipo decide si enmienda el ADR o abre un ADR nuevo para la excepción.
- **Reversible:** mover cualquier ocupante a su propia VM más adelante (más carga, equipo propio, aislamiento de fallo) es un cambio de **despliegue**, no de diseño ni de código. Solo se relaja la *capa 4* (aislamiento de infra) de la sección 2.
- **Naming pendiente (decisión de plataforma):** el repo se llama `Cosmos.Terceros.Infraestructura` pero hospeda tres ocupantes — el nombre se quedó corto. Renombrarlo a `Cosmos.SoporteDeDatos.Infraestructura` (familia de *host*, como `ApplicationPlane.Infraestructura`) sería más honesto, pero el prefijo CAF `terc` está incrustado en todos los recursos (RG, VM, Postgres, ACR, redes, runner) y los nombres en Azure son **inmutables**, así que re-prefijar implica **re-provisionar** (barato en DEV, no trivial en prod). Es una mejora **cosmética**, no funcional: el host compartido ya está montado. Lo que **sí** conviene corregir es la etiqueta interna que llama al front de EO "segundo front del BC Terceros" — para no acoplar conceptualmente a EO con Terceros.

Hechos comerciales que alimentan el análisis:

- **OXP no se vende solo** → el paquete mínimo vendible lo incluye junto a Contabilidad, Impuestos, Terceros y EO. El nicho principal controla gasto/costo (OXP); CXC con frecuencia no se activa porque no venden.
- **OXP + Contabilidad** es el paquete que normalmente se compra (núcleo comercial).
- **Contabilidad se puede comercializar sola** a futuro (sistema de consolidación de información) → es producto por derecho propio.
- **Impuestos** es el que más localización necesita al llevar el sistema a otros países → evoluciona en el reloj regulatorio de cada jurisdicción.

Mapa por bounded context:

| Bounded context | Cómputo | ¿Producto propio? | Operación / equipo propio |
|---|:---:|:---:|---|
| **OXP** | VM propia | No se vende solo (núcleo) | **Sí** — producto vivo, su equipo |
| **Contabilidad** | VM propia | **Sí** (consolidación) | **Temprano** — se gradúa con el núcleo |
| **Impuestos** | VM propia | **Sí** (en paquetes) | **Temprano + equipo propio** — el reloj regulatorio lo empuja primero |
| **Asistente** | VM propia | No (transversal) | Según carga — no estaba en el set original de la guía |
| **Terceros** | host compartido `terc` | No (soporte de datos) | Tardío — comparte host con DatosRef y EO |
| **Datos de Referencia** | host compartido `terc` | No (servicio de catálogos) | Tardío — servicio compartido, no BC de dominio |
| **EO** | host compartido `terc` | No (soporte de datos) | Tardío — ya desplegado sobre el host compartido |

> **Pendiente real de implementación:** el backbone inter-BC está diseñado (ADR-002) pero los tópicos `<contexto>.events` todavía **no existen** en Azure ni en el Terraform activo (solo el tópico de *provisioning de suscripciones*; verificado contra la vista `03-messaging-flow` del repo `architecture`). La infraestructura de cada host está viva, pero los eventos de dominio entre BC aún no fluyen. Es el siguiente paso para que la integración cross-BC sea real.

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
| 1.3 | Junio 2026 | Nueva subsección en la sección 9: **propuesta para incorporar EO compartiendo host con Terceros** (repo `Cosmos.Terceros.Infraestructura` → `Cosmos.SoporteDeDatos.Infraestructura`; 1 VM + 1 Postgres compartidos, bases/contenedores/redes/topics separados; desacople por eventos y reversibilidad; excepción deliberada al ADR-001). Incluye un diagrama completo (el de la sección 5 con la 4.ª columna como host compartido). **El diagrama de la sección 5 no se modificó.** |
| 2.0 | Junio 2026 | **Refinamiento integral al estado actual.** Se confirmó con plataforma que EO ya está desplegado sobre el host compartido de Terceros (swarm `terceros`, `estructuraorganizacionaldb`, RabbitMQ/KV de `terc`) — el host compartido ya es realidad, no propuesta. Se eliminó la estructura de "preguntas para plataforma" y la "propuesta"; la guía ahora presenta **cómo está montado hoy**. Sección 5 sin diagramas (solo explicación + "cómo está montado" en afirmaciones). Sección 9 = estado actual con **un único diagrama** (5 unidades de cómputo: OXP, Impuestos, Contabilidad, Asistente + host compartido Terceros/DatosRef/EO); se eliminaron los otros dos diagramas. EO corregido de "brecha" a tercer ocupante ya desplegado; renombre a `SoporteDeDatos` como consideración de naming pendiente (no como hecho); aclaración conceptual EO ≠ front de Terceros. |
