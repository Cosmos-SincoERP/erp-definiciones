# Visión de Producto — «Cosmos» (ecosistema agéntico empresarial)

> **Qué es este documento:** el **norte** del producto. Responde *qué* estamos construyendo y *por qué*, para que todo el equipo —diseño, desarrollo y negocio— comparta una misma imagen del producto antes de bajar a identidad de marca o a especificación técnica.
> **Qué NO es:** no es el brief de marca (cómo se ve/comunica → `brief-marca.md`) ni la especificación de dominio (cómo se implementa → `dominio/`, `documento-consolidado-erp.md`).
> **Estado:** Borrador v0.2 — visión para alinear al equipo. «Cosmos» es nombre provisional.
> **Fecha:** Junio 2026

---

## 0. Cómo leer este documento

| Marca | Significa |
|---|---|
| ✅ **Dado** | Hecho del producto/negocio, ya definido o construido. |
| 💡 **Visión** | Hacia dónde vamos; propuesta a madurar con el equipo. |
| ❓ **Abierto** | Decisión pendiente. |

### Marco metodológico

Este documento sigue el **kernel de una buena estrategia** (Richard Rumelt, *Good Strategy Bad Strategy*, 2011): una estrategia no es una lista de deseos sino un argumento razonado con tres partes — **diagnóstico** (qué está pasando y cuál es el obstáculo crítico), **política guía** (el enfoque que lo ataca) y **acciones coherentes** (los pasos que se refuerzan entre sí). Así se mapean las secciones:

| Parte del kernel | Secciones de este documento |
|---|---|
| **Diagnóstico** — el obstáculo crítico | §2 (el problema que resolvemos) + §3 (de dónde venimos) |
| **Política guía** — el enfoque que lo ataca | §4 (la visión) + §5 (los ejes diferenciales) |
| **Acciones coherentes** — pasos que se refuerzan entre sí | §6 (ruta de madurez) + §7 (modelo de negocio) + §8 (relación con el dominio) |

Dos controles complementan el marco: la **cascada de decisiones estratégicas** (*Playing to Win*, Lafley & Martin) como resumen auditable de las decisiones (§11), y el **checklist del Product Vision Board** (Pichler) como control de calidad del enunciado central (§4). Las fuentes completas, en §10.

---

## 1. El mapa de documentos (dónde encaja esta visión)

Este proyecto nació como **especificación de dominio** (los modelos de cada sub-dominio). En las conversaciones con diseño quedó claro que faltaba un nivel por encima: **la visión del producto**. Así se ordenan las capas:

```
POR QUÉ / QUÉ   →  📄 Visión de Producto   (este documento — para todo el equipo)
CÓMO SE SIENTE  →  📄 Brief de Marca        (brief-marca.md — para diseño)
CÓMO SE VALIDA  →  📄 Validación de Marca   (validacion-marca.md — proceso y evidencia para decidir el naming)
QUÉ HACE        →  📄 Especificación de dominio (dominio/, documento-consolidado-erp.md — para desarrollo)
MIRADA EXTERNA  →  📄 Benchmark de Mercado  (benchmark-mercado.md — mercado y competencia)
DE DÓNDE VENIMOS→  Producto actual SincoERP/Bitákora (insumo — ver §3)
```

Cada capa se apoya en la de arriba. Esta visión es la raíz.

---

## 2. El problema que resolvemos (✅ / 💡)

Hoy, operar una empresa con software empresarial significa, para el usuario:

1. **Digitar todo a mano.** La información ya existe (en una factura, un RUT, un correo, un extracto), pero alguien tiene que volver a teclearla.
2. **Pelear con formularios.** Procesos que obligan a llenar campos uno por uno, sin entender qué está pasando ni por qué.
3. **Saltar entre herramientas dispersas** que no se hablan entre sí — el dato sale de un sistema y entra manualmente a otro.
4. **Saber de antemano qué hacer.** El sistema espera órdenes; no orienta. Si el usuario no sabe qué sigue, el sistema no le ayuda.

El resultado: operación lenta, propensa a errores, dependiente de expertos y de mucho trabajo manual que no agrega valor.

> **La oportunidad:** un entorno donde el dato **entra solo**, los procesos se **completan sin formularios tradicionales**, los sistemas **se hablan entre sí** y el software **le dice al usuario qué hacer** — devolviéndole el control y el tiempo.

---

## 3. De dónde venimos: el producto actual (✅ dado)

**SincoSoft** ya tiene una suite madura, **SincoERP**, con ~17 soluciones para los sectores de **construcción, infraestructura, inmobiliario y concesiones viales**. No es "solo un ERP contable" — cubre la operación completa:

| Área | Soluciones (ejemplos reales) |
|---|---|
| **Gestión del negocio** | ADPRO (proyectos/presupuestos y control), SGP (proyectos), CBR (comercialización de bienes raíces), ABR (administración de bienes raíces), CRM, SRM (proveedores), M&E (maquinaria y equipos), ADC (cobros) |
| **Gestión financiera** | A&F (administrativo y financiero), F&C (facturación y cartera), FE (facturación electrónica), RE (recepción electrónica) |
| **Apoyo empresarial** | BITÁKORA (nómina), SGD (documental), SGC (calidad), SST (seguridad y salud), CAPTA (solicitudes) |
| **Infraestructura** | Hosting especializado |

> **Señal de que la visión ya germina:** **Bitákora** (nómina) ya incorpora IA propia — su *"Modo Crucero"* convierte voz, texto y documentos en operaciones de nómina, reduciendo la digitación. La dirección agéntica no parte de cero.

**El reto:** estas soluciones son potentes pero funcionan como módulos relativamente independientes. La siguiente generación las convierte en un **ecosistema que conversa**.

### 3.1. El alcance ya es multi-sector (✅ dado — dato clave para el segmento)

El producto se compone de **dos capas con naturalezas distintas**, y esto define a quién le sirve:

- **Núcleo transversal (horizontal):** lo financiero, contable, facturación, recaudo y **cobros** no depende del sector. **Ya hoy** atiende sectores muy distintos a la construcción. Ejemplos reales:
  - **SincoADC (cobros):** empresas temporales de nómina, **servicios públicos**, **suscripciones de software** (el propio SincoSoft que arrienda su software; Camacol cobrando suscripciones a constructoras), **gestión aeroportuaria** de operación regulada.
  - **SincoM&E:** el negocio de **transporte**.
- **Verticales especializados:** gestión de obra, inmobiliario, infraestructura, concesiones — donde están los **25 años de expertise** y el reconocimiento de marca.

**El dato más importante:** todas esas soluciones —de cualquier sector— **ya entregan su información al núcleo de facturación y contabilidad.** La integración por eventos del ecosistema **no es teórica: ya ocurre**. Es la base real sobre la que se construye Cosmos.

> **Implicación para el segmento:** Cosmos apalanca el **reconocimiento vertical** (construcción/inmobiliario) como ancla de credibilidad, mientras el **núcleo transversal** —ya probado en servicios públicos, transporte, aeroportuario y suscripciones— habilita la **expansión a nuevos sectores**. No es "el software de constructores": es un ecosistema cuyo núcleo sirve a cualquier sector y cuyos verticales aportan profundidad donde se necesita.

---

## 4. La visión: el ecosistema agéntico «Cosmos» (💡)

**Cosmos es la nueva generación de SincoSoft: un ecosistema agéntico donde las soluciones empresariales conviven en un mismo entorno, cada una con inteligencia propia, comunicándose entre sí.**

> ✔️ *Control de calidad del enunciado (checklist del Product Vision Board, Pichler):* **conciso** (una frase), **ambicioso y perdurable** (describe el destino del ecosistema, no una versión del producto), **inspirador desde el usuario** (soluciones que conviven y se entienden entre sí, no una lista de tecnologías) y **sin supuestos de solución** más allá de la apuesta de categoría («agéntico» es la apuesta estratégica, no un detalle de implementación). Queda ❓ **compartido**: la alineación formal con dirección y equipo está pendiente (§9).

```
SincoSoft  (la empresa)
    │
    └── SincoERP  (la suite actual — soluciones en producción)
            │
            └── Cosmos  (la nueva generación — ecosistema agéntico que
                         evoluciona y engloba las soluciones en un entorno común)
```

**El principio de experiencia:** cada empresa **activa las soluciones que necesita** según su operación. El usuario no compra "un ERP"; entra a un entorno y enciende lo que le sirve — y todo lo que enciende se entiende entre sí.

### 4.1. Cada producto tiene identidad propia y puede ser multi-actor (💡)

Pertenecer al ecosistema **no significa que todos los productos se vean y se sientan igual.** Cada solución tiene **identidad, lenguaje y experiencia propios**, ajustados a sus usuarios — porque los públicos son radicalmente distintos. Y muchas soluciones son **multi-actor**: sirven a varios tipos de usuario a la vez, no solo al "empleado de la empresa cliente".

> **Ejemplo — plataforma de reparaciones locativas:** conviven tres actores con necesidades distintas: el **arrendatario o propietario** (reporta y sigue la reparación), el **agente de la inmobiliaria** (coordina y autoriza) y el **proveedor o contratista** (ejecuta y cobra). Esta plataforma vive **dentro del ecosistema** (alimenta finanzas, comparte la inteligencia común), pero tiene **su propia identidad y enfoque** — el contratista nunca verá "un ERP"; verá una herramienta que le resuelve su trabajo.

**Por qué importa:** el valor del ecosistema (que los sistemas se comuniquen y compartan inteligencia) **convive** con la libertad de que cada producto le hable bien a su público. La forma exacta de equilibrar ecosistema e identidad propia es una decisión de **arquitectura de marca** → se trabaja en `brief-marca.md`.

💡 *Por qué el nombre encaja:* «Cosmos» = un universo **ordenado** donde muchos cuerpos coexisten en un sistema. Conecta con "muchas soluciones, un solo entorno con orden". (Naming sigue ❓ — la pregunta vive en el brief de marca y se decide con el proceso de validación de `validacion-marca.md`.)

---

## 5. Los ejes diferenciales (el corazón de la visión)

Cuatro ejes definen qué hace distinto a Cosmos. Son la materia prima tanto del producto como de la marca.

### 5.1. Sin formularios: eliminar la digitación (💡, ya en construcción)
El principio rector es **automatizar y facilitar**, atacando dos cosas que agotan al usuario:
- **La digitación manual** — que el dato entre solo, desde donde ya existe.
- **El formulario tradicional** — completar procesos sin llenar campos uno a uno, sin perder control ni visibilidad.

Para lograrlo, el ecosistema incorpora **toda tecnología que sirva a ese fin** (lista abierta, crece con el tiempo). Ejemplos:

| Tecnología | Cómo reduce digitación / formularios |
|---|---|
| **OCR / lectura de documentos** | Extrae datos de PDF, imágenes y XML (ej. perfil tributario desde el RUT, factura desde su PDF). |
| **Canales conversacionales (WhatsApp, chat, voz)** | El usuario actúa desde donde ya está, sin entrar a un formulario. |
| **Lenguaje natural** | El usuario describe lo que necesita; el sistema arma el proceso. |
| **MCP** | Interconecta agentes y herramientas para que los procesos crucen sistemas. |
| **Recuperación agéntica (evolución del RAG)** | Ancla respuestas y orientación en el conocimiento real del sistema y la empresa. |
| *(futuras)* | Cualquier tecnología que siga reduciendo la operatividad manual se suma. |

> El norte no es "tener IA": es **que el usuario haga menos y entienda más**.

### 5.2. IA en cada solución (💡 visión — con evidencia inicial)
La visión es que **cada solución tenga su propia inteligencia**, apalancada en IA y en el aprendizaje, para orientar al usuario y reducir su carga operativa — **inteligencia distribuida, una por dominio**, no una IA central única.

Hoy esto es **evidencia inicial, no estado generalizado:** ya existe en la categorización contable automática del módulo de **Contabilidad** y en el *"Modo Crucero"* de **Bitákora**. El resto de soluciones lo incorpora progresivamente — es parte de lo que Cosmos lleva a todo el ecosistema.

### 5.3. El asistente transversal de IA (✅ dado — validado en el producto)
Un asistente de IA cruza todo el ecosistema. Tres comportamientos ya observados en lo construido:

**a) Orienta — le dice al usuario qué hacer.** El inicio recibe al usuario por su nombre ("Hola, Ana") y le presenta sus **tareas priorizadas y agrupadas por módulo** con su urgencia ("1 vence hoy", "lleva 4 días sin corregir"). No espera que el usuario sepa qué sigue: se lo propone.

**b) Captura intenciones — predeterminadas y por lenguaje natural.** Conoce los procesos típicos de cada módulo (ej. *"Registrar compra"*) y guía sus campos; y una barra transversal **"Describe lo que necesitas…"** permite pedir en lenguaje natural, interpretando la intención y **validando los datos en vivo**.

**c) Extrae datos de documentos.** Lee documentos reales y **puebla los procesos**: del PDF del RUT de la DIAN completa el perfil tributario (NIT, régimen, país, actividad CIIU, atributos fiscales), marcando cada dato como *"Extraído con IA"* para que el usuario solo confirme.

> Estas tres capacidades (orientar, capturar, extraer) son el **lenguaje de interacción** central del ecosistema — y el punto de contacto más distintivo con el usuario.

### 5.4. Agéntico y orientado a eventos (✅ dado + 💡 visión)
Cada solución es (o tiende a ser) un **agente** que percibe, decide y actúa, comunicándose con las demás de forma **eventual** (event-driven) — sin llamadas manuales ni pasos que el usuario deba orquestar.

**Qué significa "agéntico" aquí** (no es un chatbot): sigue el paradigma clásico de agentes inteligentes **percibir → decidir → actuar**, cerrado en un bucle con el entorno.
- **Percibe** hechos del negocio vengan de donde vengan (correo, documento, WhatsApp, evento de otro módulo).
- **Decide** qué corresponde, con su inteligencia de dominio y el aprendizaje acumulado.
- **Actúa** ejecutando o proponiendo el siguiente paso, y le cuenta al usuario qué hizo o qué revisar.
- **Colabora** con otros agentes: un hecho en un módulo dispara la cadena correcta en los demás.

**Cómo se construye técnicamente (no es una idea superficial):**

El comportamiento agéntico se apoya en piezas de arquitectura concretas y reconocidas en la industria:

| Capacidad | Cómo se resuelve técnicamente | Respaldo |
|---|---|---|
| **Que los módulos reaccionen sin intervención** | **Arquitectura orientada a eventos (EDA)**: cada hecho del negocio es un evento publicado en un bus; los módulos interesados lo consumen y reaccionan. Es la misma arquitectura sobre la que ya se modela el dominio (Event Sourcing + EDA). | Patrón establecido de sistemas distribuidos; ya operando en `dominio/` bajo guías propias del ecosistema (`guias-de-modelado/`: arquitectura EDA, datos entre dominios, topología de equipos y despliegue). |
| **Que ningún módulo dependa de otro para operar** | **Distribución en lugar de dependencia:** el dato con dueño único viaja por eventos y cada módulo mantiene su copia local para validar; las reglas transversales van empaquetadas dentro de cada módulo (Nuggets). Si un módulo no está disponible, los demás siguen operando. | Principio vigente del ecosistema — `guias-de-modelado/datos-entre-dominios.md`. |
| **Que el agente decida y use herramientas** | Combinación de **workflows** (rutas predefinidas para procesos deterministas — ej. "causar una factura") y **agents** (el modelo dirige dinámicamente los pasos cuando el caso es abierto). Patrones: *routing* (clasificar y derivar), *orchestrator-workers* (un coordinador delega en agentes especializados). Es el mismo reparto que el ecosistema ya practica: los procesos deterministas del dominio son workflows (ej. traducir una obligación a asiento contable vía plantillas), y los casos abiertos combinan reglas con aprendizaje (ej. proponer el PUC limpio en el onboarding). | Anthropic, *Building Effective Agents* (2024) — fundacional; operacionalizado en el **Claude Agent SDK** (2025) y demostrado en producción en su sistema multi-agente de investigación (2025). |
| **Que entienda documentos y lenguaje natural** | **LLMs** con *tool use* + **OCR** para documentos; captura de intención por lenguaje natural. | Estándar actual de IA aplicada. |
| **Que responda anclado en datos reales (sin inventar)** | **Recuperación agéntica de conocimiento:** el agente decide cuándo y dónde buscar, recuperando el contexto bajo demanda como uso de herramientas — evolución del RAG clásico de pipeline fijo. Reduce alucinaciones y da trazabilidad. | Anthropic, *context engineering* (2025); Singh et al., *Agentic RAG Survey* (2025); origen del concepto: Lewis et al. (2020). |
| **Que los agentes se conecten a herramientas y entre sí** | **MCP (Model Context Protocol)** para conectar agentes con datos y herramientas de forma uniforme, y **A2A (Agent2Agent)** para la comunicación agente↔agente entre proveedores. | MCP: creado por Anthropic (2024), hoy **estándar abierto bajo la Linux Foundation** (Agentic AI Foundation, dic-2025), adoptado por OpenAI, Google y Microsoft. A2A: Linux Foundation (2025). |
| **Que la autonomía sea segura** | **Human-in-the-loop**: puntos de control donde el agente pausa para confirmación humana, y condiciones de parada. La autonomía crece por proceso según su riesgo (ver §6). | Anthropic, *Building Effective Agents* (2024); gobernanza formalizable con **NIST AI RMF**, **ISO/IEC 42001** y el **OWASP Top 10 para aplicaciones agénticas** (2025) — relevante para un ecosistema con responsabilidad legal financiera (§6). |
| **Que el agente demuestre que funciona** | **Evaluación de agentes (evals):** cada capacidad agéntica se mide por su trayectoria y su resultado, con confiabilidad repetida — no con demostraciones puntuales. Las evaluaciones acompañan el desarrollo del agente como las pruebas acompañan al código. | Anthropic, *Demystifying evals for AI agents* (2026); τ²-bench y la métrica de confiabilidad repetida (Sierra Research, 2025). |

> En síntesis: **EDA** da el sistema nervioso (los eventos), la **distribución** evita que un módulo dependa de otro, los **patrones de agentes** dan la toma de decisiones, la **recuperación agéntica + MCP/A2A** dan conocimiento y conexión, las **evaluaciones** dan confianza medida, y el **human-in-the-loop** — gobernado con marcos formales — mantiene el control. No es "ponerle IA a un ERP": es una arquitectura agéntica de extremo a extremo.

**Caso ilustrativo (con la tecnología que lo resuelve en cada paso):**
> Llega una **factura de compra** por el correo empresarial — o por **WhatsApp**, o el usuario tiene el **PDF/XML**. → *(EDA)* la llegada es un **evento**. → *(OCR + LLM)* el agente **extrae** sus datos. → *(recuperación agéntica)* los **contrasta** con el proveedor, el plan de cuentas y reglas de la empresa. → *(workflow de causación)* la **registra** en el módulo contable. → *(EDA)* publica el hecho, que otros módulos (tesorería, impuestos) consumen. → *(human-in-the-loop)* avisa al responsable **solo si necesita una decisión**. Nadie la digitó; un hecho del mundo real disparó toda la cadena.

### 5.5. Onboarding guiado: dar de alta cada aplicación sin fricción (✅ dado + 💡 visión)

El mismo principio de "sin formularios, con IA que orienta" aplica al **momento más crítico y costoso** del ciclo de vida: la **puesta en marcha**. Hoy, dar de alta una empresa en un ERP exige a un consultor senior repetir, cliente por cliente, el mismo análisis manual — y es donde más implementaciones fracasan (ver `benchmark-mercado.md`).

La visión: que el **consultor o el propio usuario** pueda dar de alta la mayoría de las aplicaciones de forma **práctica y guiada**, con un **asistente de onboarding** que compara contra estructuras de referencia, aplica reglas heurísticas, **aprende de cada implementación** para mejorar la siguiente, y presenta sugerencias iterativas que el usuario solo confirma.

> **Ya especificado (✅):** el **Asistente de Onboarding** es un sub-dominio formal del ecosistema (`compartido/asistente-onboarding/` — alcance, modelo de dominio y caso PUC en v1.0, en refinamiento), diseñado como pieza **transversal**: el caso PUC es el primero, y los siguientes (terceros, estructura organizacional, saldos iniciales, configuración fiscal…) reutilizan el mismo asistente. En el caso PUC: toma el plan de cuentas heredado del sistema anterior, detecta sus problemas típicos (cuentas duplicadas por tercero/ciudad, atributos fiscales mezclados), propone un PUC limpio y aprende de las decisiones del consultor.

**Por qué es estratégico:** el onboarding guiado **baja la barrera de entrada** (más clientes activados, más rápido, con menos consultoría senior), ataca directamente la causa #1 de fracaso de ERPs, y es **multiplicador del ecosistema**: cada nueva aplicación que se suma se vuelve fácil de adoptar.

---

## 6. La ruta de madurez hacia la autonomía (💡)

**El destino de la visión es un ecosistema autónomo** — agentes que ejecutan el trabajo y mantienen al humano en el centro de las decisiones que importan (etapa 4). "Autónomo" no significa "sin humano": en finanzas y contabilidad hay responsabilidad legal, por lo que el **human-in-the-loop** es parte del diseño, no una limitación temporal.

Las cuatro etapas **no son opciones de "hasta dónde llegar"** — son la **ruta de madurez** hacia ese mismo destino. El cambio de fondo es un **desplazamiento del rol del usuario**: de operar a supervisar.

| Etapa | El sistema… | El usuario… |
|---|---|---|
| **1. Asiste** *(donde está hoy)* | Orienta (muestra tareas y prioridades), extrae datos de documentos y entiende lo que el usuario quiere hacer. | **Ejecuta cada paso.** El sistema acompaña, no actúa por él. |
| **2. Propone** | Prepara el trabajo hecho (la factura ya registrada, la conciliación armada, la nómina calculada). | **Solo confirma.** El formulario desaparece detrás de una propuesta lista. |
| **3. Actúa con autonomía supervisada** | Ejecuta de punta a punta los procesos rutinarios y de bajo riesgo, bajo políticas definidas; **escala solo las excepciones**. | **Supervisa y decide** las excepciones. |
| **4. Agentes que colaboran** *(destino)* | Los agentes de los módulos se coordinan entre sí (compras ↔ contabilidad ↔ tesorería ↔ nómina). | Queda en el **centro de las decisiones que importan**; la operación fluye como una conversación entre agentes. |

> El eje es el desplazamiento del usuario: de *digitar y navegar formularios* → a *confirmar propuestas* → a *supervisar agentes*. Es el criterio con el que se diseña cada funcionalidad nueva.

---

## 7. Modelo de negocio: cómo se cobra (💡 patrón a adoptar por todos los productos)

El ecosistema necesita un **patrón de monetización común** para que cada producto lo aplique de forma coherente. El principio: **el precio escala con el valor que el cliente obtiene**, medido por la **unidad de crecimiento propia de cada negocio**.

**Patrón de precios (tres componentes):**

| Componente | Qué es | Ejemplo |
|---|---|---|
| **1. Precio base** | Un piso de entrada por activar la solución. Da acceso al producto y a la infraestructura común del ecosistema. | Tarifa mensual mínima por tener la app de arriendos activa. |
| **2. Unidad de crecimiento del negocio** | La métrica que crece junto con el valor que el cliente recibe. **Cada producto define la suya.** A más unidad, más paga — alineando precio y valor. | Arriendos: **# de inmuebles administrados**. Cobros (ADC): # de obligaciones gestionadas. Nómina (Bitákora): # de empleados liquidados. |
| **3. Consumo de capacidades avanzadas** *(opcional)* | Uso intensivo de capacidades que tienen costo marginal real (ej. procesamiento de documentos con IA, canales como WhatsApp). | Volumen de documentos procesados con OCR/IA por encima de un cupo. |

**Principios del patrón:**
- **Alineación valor-precio:** el cliente paga más solo cuando su negocio crece (más inmuebles, más empleados, más transacciones). No hay penalización por adoptar; hay acompañamiento al crecimiento.
- **Entrada baja, expansión natural:** el precio base permite empezar pequeño; el ecosistema crece en facturación a medida que el cliente activa más soluciones y crece su operación (modelo *land and expand*).
- **Cada producto declara su unidad de crecimiento** explícitamente, pero todos siguen la misma estructura de tres componentes — para que el cliente entienda el ecosistema como un todo coherente y no como tarifas dispares.

> **Para definir (❓ — dueño: liderazgo Cosmos, dirección comercial y gerencia; momento: antes del lanzamiento comercial de la primera solución del ecosistema):** valores concretos, cupos y si el ecosistema ofrece un esquema de "suscripción al ecosistema" con descuento por activar varias soluciones. El **patrón** queda fijo en esta visión; los **números** los define negocio.

---

## 8. Relación con la especificación de dominio (✅ dado)

Esta visión se materializa, pieza por pieza, en los **modelos de dominio** que ya construye el proyecto (DDD / Event Sourcing / EDA). La arquitectura **orientada a eventos** de esos modelos es, precisamente, lo que habilita el comportamiento agéntico descrito en §5.4.

- Mapa técnico del producto: `documento-consolidado-erp.md`.
- Modelos por sub-dominio: `dominio/` (ej. la categorización contable con IA está en `dominio/contabilidad/modelo-dominio.md` [D6]).
- Principios operativos del ecosistema: `guias-de-modelado/` (arquitectura EDA, datos entre dominios con dueño único, separación de responsabilidades, topología de equipos y despliegue) — las reglas con las que los módulos colaboran sin depender uno del otro.
- Filosofía y flujo del proyecto: `README.md`.

> La visión dice *qué y por qué*; el dominio dice *cómo*. Deben mantenerse coherentes: si la visión evoluciona, los modelos la siguen, y viceversa.

---

## 9. Preguntas abiertas para el equipo (❓)

| # | Pregunta | Dueño | Momento de cierre |
|---|----------|-------|-------------------|
| 1 | **Alcance de la primera materialización** — ¿qué soluciones inauguran el ecosistema agéntico y en qué orden? *(Pregunta de roadmap, no de visión: el destino —autonomía, §6— no está en discusión; se prioriza el camino.)* | Liderazgo Cosmos + dirección | Al definir el plan de desarrollo de la primera fase |
| 2 | **Relación con SincoERP actual** — ¿Cosmos convive con SincoERP, lo reemplaza progresivamente, o es una capa nueva sobre lo existente? | Dirección ejecutiva + I+D | Antes de comunicar la nueva generación al mercado (condiciona la arquitectura de marca — brief §8) |
| 3 | **Nuevos sectores objetivo** — ¿a qué sectores nuevos apunta la expansión? Incluye la decisión gemela que exige la cascada (§11.2): **dónde NO jugar**. | Dirección + negocio | Al completar "dónde jugar" en la cascada (§11) |
| 4 | **Naming y marca** — la pregunta vive en el brief de marca (§9.1) y se decide con el proceso de validación de `validacion-marca.md`. | Diseño + dirección | Según el proceso de validación (fases 0–3) |

---

## 10. Fundamento y referencias

### Marcos de estrategia (el método de este documento)

- **Richard Rumelt — *Good Strategy Bad Strategy* (2011).** El kernel diagnóstico → política guía → acciones coherentes que estructura este documento (§0).
- **A.G. Lafley & Roger Martin — *Playing to Win* (2013).** La cascada de cinco decisiones estratégicas del anexo (§11).
- **Roman Pichler — Product Vision Board.** El checklist de calidad aplicado al enunciado central (§4).

### Fundamento técnico del comportamiento agéntico (§5.4)

Referencias vigentes a 2026, del material fundacional a su operacionalización:

- **Anthropic — *Building Effective Agents* (dic-2024).** Distingue *workflows* (rutas predefinidas) de *agents* (el modelo dirige sus pasos); define los patrones de coordinación y el rol del human-in-the-loop. Sigue siendo el texto fundacional del campo.
- **Anthropic — serie de ingeniería 2025-2026.** Lo operacionaliza: *How we built our multi-agent research system* (2025 — orchestrator-workers en producción), *Effective context engineering for AI agents* (2025 — el contexto como recurso finito, recuperación bajo demanda), *Building agents with the Claude Agent SDK* (2025 — el bucle recopilar contexto → actuar → verificar) y *Demystifying evals for AI agents* (2026 — evaluación por trayectoria y resultado).
- **MCP y A2A — estándares abiertos de interconexión.** MCP (conexión agente↔herramientas/datos): creado por Anthropic (2024), donado a la Linux Foundation (Agentic AI Foundation, dic-2025), adoptado por OpenAI, Google y Microsoft. A2A (conexión agente↔agente entre proveedores): Linux Foundation (2025).
- **Recuperación agéntica de conocimiento.** El agente recupera contexto bajo demanda como uso de herramientas (Anthropic, 2025; Singh et al., *Agentic RAG Survey*, 2025) — evolución del RAG clásico introducido por Lewis et al. (2020).
- **Sistemas multi-agente orientados a eventos.** La tesis de este documento ya es categoría de industria: patrones evento-agente de Confluent (2025) y patrones de orquestación de agentes de Microsoft (Azure Architecture Center, 2025).
- **Evaluación de agentes.** τ²-bench y la confiabilidad repetida como métrica (Sierra Research, 2025): un agente se mide por acertar consistentemente, no por una demo.
- **Gobernanza de la autonomía.** NIST AI RMF (con su perfil de IA generativa), ISO/IEC 42001 (gestión de IA certificable) y OWASP Top 10 para aplicaciones agénticas (2025) — el human-in-the-loop de la ruta de madurez (§6), formalizado con marcos de riesgo reconocidos.
- **Paradigma percibir–decidir–actuar.** Base conceptual de los agentes; sus formulaciones vigentes: recopilar contexto → actuar → verificar → repetir (Agent SDK) y percibir eventos → razonar → actuar → aprender (Confluent).

> Estas referencias dan respaldo a las decisiones de producto y permiten que el equipo de desarrollo aterrice la visión sobre tecnología concreta y probada.

---

## 11. Anexo — Cascada de decisiones estratégicas (Playing to Win)

Resumen del documento en las **cinco decisiones anidadas** del marco *Playing to Win* (Lafley & Martin): la estrategia queda auditable — cada casilla remite a la sección narrativa que la sustenta, y las decisiones aún abiertas quedan ❓ con su dueño.

### 1. Aspiración ganadora (✅ — este documento)

Ser la nueva generación del software empresarial de SincoSoft: el ecosistema agéntico donde operar una empresa deja de ser digitar y navegar formularios, y el usuario pasa de operar a supervisar (§4, §6).

### 2. Dónde jugar (✅ parcial / ❓ por completar)

- ✅ **Geografía:** Colombia, República Dominicana y Panamá como países de la primera fase (el dominio ya se especifica multi-país); Norteamérica como apertura posterior.
- ✅ **Dos capas de segmento (§3.1):** verticales de expertise (construcción, inmobiliario, infraestructura, concesiones — el ancla de credibilidad) + núcleo transversal agnóstico de sector (la puerta de expansión, ya probada en servicios públicos, transporte, aeroportuario y suscripciones).
- ✅ **Expansión oportunista:** nichos que la consolidación del mercado deja huérfanos (`benchmark-mercado.md` §4).
- ❓ **Por decidir (dueño: dirección — §9):** la prioridad entre base instalada y clientes nuevos; los sectores nuevos objetivo; y —la decisión que este marco exige y hoy no está escrita— **dónde NO jugar** (sectores, tamaños de empresa o geografías que se declinan).

### 3. Cómo ganar (✅ — §5 y benchmark §3)

Con la combinación que hoy nadie ofrece junta en LatAm: **expertise sectorial profundo + ecosistema agéntico integrado por eventos + onboarding guiado**. Materializada en los cuatro ejes diferenciales (§5). Explícitamente NO se compite por amplitud funcional (terreno de SAP) ni por precio (terreno de Siigo) — `benchmark-mercado.md` §3.

### 4. Capacidades imprescindibles (✅ — prueba can't/won't)

Lo que los competidores **no pueden o no querrían** imitar:

| Capacidad | Por qué pasa la prueba |
|---|---|
| 25 años de conocimiento contable, fiscal y operativo LatAm (CO/RD/PA) | Los globales no querrán profundizar en mercados que consideran pequeños; los locales no pueden replicar la profundidad. |
| Base instalada con ~17 soluciones que **ya** entregan su información al núcleo financiero (§3.1) | La integración por eventos no es teórica: ya ocurre. Un entrante parte de cero. |
| Especificación de dominio DDD/ES/EDA con principios propios (§8, `guias-de-modelado/`) | Es el sistema nervioso del comportamiento agéntico — años de trabajo de dominio, no una capa de IA superpuesta. |
| Catálogos fiscales precargados multi-país + asistente de onboarding en especificación | Atacan la causa #1 de fracaso de los ERP (la puesta en marcha) con conocimiento acumulado. |
| Evidencia de IA ya en producción (Modo Crucero, categorización contable, asistente transversal §5.3) | La dirección agéntica no parte de una promesa. |

### 5. Sistemas de gestión (✅ parcial / ❓ por completar)

- ✅ **La ruta de madurez (§6)** como criterio de diseño de cada funcionalidad nueva.
- ✅ **El patrón de monetización común (§7)** que cada producto aplica — con ❓ los números concretos (dueño: negocio).
- ✅ **El flujo de especificación** que mantiene visión y dominio coherentes (§8).
- ❓ **Métricas de progreso (dueño: liderazgo Cosmos):** cada etapa de la ruta de madurez tendrá un criterio de salida medible; las métricas se definen cuando haya producto operando (compromiso tomado del marco North Star, Amplitude — no se inventan métricas decorativas antes de tener datos).

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.2 | Julio 2026 | **Refinamiento #80 (milestone "Refinamiento visión de producto").** (1) **Marcos de estrategia adoptados:** kernel de Rumelt declarado como estructura del documento (§0 — diagnóstico → política guía → acciones coherentes), anexo §11 con la cascada de decisiones estratégicas de *Playing to Win* (incluida la decisión que faltaba: **dónde NO jugar**, ❓ con dueño) y checklist del Product Vision Board aplicado al enunciado central (§4). (2) **Fundamento agéntico actualizado a 2026 (§5.4/§10):** serie de ingeniería de Anthropic 2025-2026, MCP como estándar de la Linux Foundation + A2A, recuperación agéntica (evolución del RAG clásico), sistemas multi-agente orientados a eventos (Confluent/Microsoft), fila nueva de evaluación de agentes y gobernanza de la autonomía (NIST AI RMF, ISO/IEC 42001, OWASP agéntico); patrones aterrizados a lo que el ecosistema ya practica. (3) **Actualización al estado real:** Asistente de Onboarding como sub-dominio formal en refinamiento (§5.5), principio de distribución/copia local como fila propia de §5.4, guías de modelado en §8, mapa de documentos con `validacion-marca.md` (§1) y naming apuntando al proceso de validación (§4.1). (4) **§9 en tabla con dueño y momento de cierre por pregunta; §7 con responsable de los números** (liderazgo Cosmos, dirección comercial y gerencia). |
| 0.1 | Junio 2026 | Primera versión de la visión de producto. Separa el "qué/por qué" del ecosistema agéntico (antes mezclado en el brief de marca): problema que resuelve, producto actual SincoERP/Bitákora, modelo de segmento de dos capas (núcleo transversal multi-sector ya probado en servicios públicos/transporte/aeroportuario/suscripciones + verticales de expertise + expansión), identidad propia por producto y multi-actor (ej. reparaciones locativas con tres actores), los cuatro ejes diferenciales, la ruta de madurez hacia la autonomía y la relación con la especificación de dominio. Cada idea agéntica se respalda con la tecnología que la resuelve (EDA, patrones de agentes de Anthropic, RAG, MCP, human-in-the-loop) y referencias técnicas. Incluye el eje de **onboarding guiado** (con el caso PUC como patrón materializado) y el **modelo de negocio** (patrón de cobro: precio base + unidad de crecimiento del negocio + consumo de capacidades). Se acompaña del análisis de mercado en `benchmark-mercado.md`. «Cosmos» como nombre provisional. |
