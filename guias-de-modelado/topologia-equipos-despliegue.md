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

## 5. Gobierno de DevOps

El gobierno de DevOps recae en el **equipo de producto** ("tú lo construyes, tú lo operas"), con el apoyo de un **equipo de plataforma** que diseña el mecanismo de aprovisionamiento (autoservicio, plantillas, guardarraíles) y lo entrega a los microequipos, de forma que cada equipo entienda la responsabilidad sobre cada componente que necesite aprovisionar.

La intención de la separación a futuro es que **microequipos administren los servicios**: un mismo equipo puede administrar varios (por ejemplo OXP y Contabilidad), o un equipo por servicio, según la carga. La separación lógica y de despliegue (capas 1 y 2) habilita ese reparto sin rehacer arquitectura; el reparto operativo (capa 3) se da cuando los equipos maduran.

---

## 6. Empaque comercial: el paquete mínimo vendible

El empaque comercial **no** define fronteras de servicio: define qué debe poder correr y licenciarse junto. Se resuelve por **licenciamiento / activación por configuración**, no por topología. El mismo conjunto de servicios separados se empaqueta como un SKU u otro prendiendo o apagando piezas.

- Un servicio que **no se vende solo** define que el paquete mínimo vendible lo incluye junto a otros — es un hecho de licenciamiento, no un mandato de fusionar servicios.
- Un servicio que **se vende solo** es producto por derecho propio (sube su prioridad para graduarse a operación independiente, criterio #1).

---

## 7. Anti-patrones

| Anti-patrón | Por qué es malo |
|---|---|
| **Creer que la frontera lógica obliga a separación física total** | Se paga operación e infra independientes antes de que un disparador real las justifique. |
| **Acoplar la elección del broker a la topología** | Falsa disyuntiva: la separación de servicios no exige un broker concreto; el broker se elige por sus méritos. |
| **Un broker por dominio** | Reintroduce el problema de integración (puentear brokers entre sí). El backbone debe ser uno solo. |
| **Fusionar servicios porque siempre se venden juntos** | Confunde empaque comercial con frontera de servicio; se pierde la independencia de equipo y cadencia. |
| **"Compartido" entendido como "acoplado"** | Dos servicios pueden co-ubicarse, pero cada uno conserva su esquema y su contrato; no se mezclan tablas. |
| **Encender la capa operativa antes de tiempo** | Pipeline/on-call/equipo dedicado por servicio cuesta; se paga cuando el servicio es producto, no antes. |

---

## 8. Caso aplicado: estado actual del ERP

Los cinco servicios están en **contenedores independientes** (OXP, Contabilidad, Impuestos, Terceros, EO) sobre **un único bus de eventos compartido**. La separación arquitectónica de los cinco va completa desde el día uno; la operación/equipo propios se gradúan servicio por servicio con los criterios de la sección 3.

Hechos comerciales que alimentan el análisis:

- **OXP no se vende solo** → el paquete mínimo vendible lo incluye junto a Contabilidad, Impuestos, Terceros y EO. El nicho principal controla gasto/costo (OXP); CXC con frecuencia no se activa porque no venden.
- **OXP + Contabilidad** es el paquete que normalmente se compra (núcleo comercial).
- **Contabilidad se puede comercializar sola** a futuro (sistema de consolidación de información) → es producto por derecho propio.
- **Impuestos** es el que más localización necesita al llevar el sistema a otros países → evoluciona en el reloj regulatorio de cada jurisdicción.

Mapa por servicio:

| Servicio | Lógica + contenedor + bus | ¿Producto propio? | Operación / equipo propio |
|---|:---:|:---:|---|
| **OXP** | ✅ | No se vende solo (núcleo) | **Sí** — producto vivo, su equipo |
| **Contabilidad** | ✅ | **Sí** (consolidación) | **Temprano** — se gradúa con el núcleo |
| **Impuestos** | ✅ | **Sí** (en paquetes) | **Temprano + equipo propio** — el reloj regulatorio lo empuja primero |
| **Terceros** | ✅ | No (soporte de datos) | Tardío — pero listo para la 1.ª venta |
| **EO** | ✅ | No (soporte de datos) | Tardío — pero listo para la 1.ª venta |

Matiz sobre "qué está vivo": como OXP no se vende sin los demás, los cinco están en la **ruta crítica de la primera venta** — todos deben estar listos para facturar. Lo que difiere no es *si* deben funcionar, sino *cuándo cada uno gana equipo y operación propios*: OXP ya; Impuestos y Contabilidad temprano por ser productos (Impuestos con equipo propio por el reloj regulatorio); Terceros y EO al final por ser soporte de datos.

---

## 9. Relación con otras guías

- **[`datos-entre-dominios.md`](datos-entre-dominios.md)** — la cara lógica del mismo problema: cómo un consumidor usa datos de otro dominio sin acoplar disponibilidad (dueño único, réplica local, eventos). Esta guía asume ese diseño y trata cómo se empaquetan y operan los servicios.
- **[`arquitectura-eda.md`](arquitectura-eda.md)** — los mecanismos de mensajería que el backbone de eventos usa.

---

## Historial

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Junio 2026 | Versión inicial. Surge del análisis de infraestructura de los sub-dominios del ERP. Consolida: las tres decisiones ortogonales (frontera lógica vs. topología, topología vs. broker, empaque vs. fronteras de servicio), las cuatro capas de separación con su costo, los ocho criterios para graduar un servicio a operación independiente, el backbone de eventos único y la elección administrado vs. autooperado, el gobierno de DevOps (producto + plataforma), el paquete mínimo vendible, anti-patrones y el caso aplicado del estado actual del ERP (cinco servicios en contenedores sobre un bus compartido). |
