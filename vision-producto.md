# Visión de Producto — «Cosmos» (ecosistema agéntico empresarial)

> **Qué es este documento:** el **norte** del producto. Responde *qué* estamos construyendo y *por qué*, para que todo el equipo —diseño, desarrollo y negocio— comparta una misma imagen del producto antes de bajar a identidad de marca o a especificación técnica.
> **Qué NO es:** no es el brief de marca (cómo se ve/comunica → `brief-marca.md`) ni la especificación de dominio (cómo se implementa → `dominio/`, `documento-consolidado-erp.md`).
> **Estado:** Borrador v0.1 — visión para alinear al equipo. «Cosmos» es nombre provisional.
> **Fecha:** Junio 2026

---

## 0. Cómo leer este documento

| Marca | Significa |
|---|---|
| ✅ **Dado** | Hecho del producto/negocio, ya definido o construido. |
| 💡 **Visión** | Hacia dónde vamos; propuesta a madurar con el equipo. |
| ❓ **Abierto** | Decisión pendiente. |

---

## 1. El mapa de documentos (dónde encaja esta visión)

Este proyecto nació como **especificación de dominio** (los modelos de cada sub-dominio). En las conversaciones con diseño quedó claro que faltaba un nivel por encima: **la visión del producto**. Así se ordenan las capas:

```
POR QUÉ / QUÉ   →  📄 Visión de Producto   (este documento — para todo el equipo)
CÓMO SE SIENTE  →  📄 Brief de Marca        (brief-marca.md — para diseño)
QUÉ HACE        →  📄 Especificación de dominio (dominio/, documento-consolidado-erp.md — para desarrollo)
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

---

## 4. La visión: el ecosistema agéntico «Cosmos» (💡)

**Cosmos es la nueva generación de SincoSoft: un ecosistema agéntico donde las soluciones empresariales conviven en un mismo entorno, cada una con inteligencia propia, comunicándose entre sí.**

```
SincoSoft  (la empresa)
    │
    └── SincoERP  (la suite actual — soluciones en producción)
            │
            └── Cosmos  (la nueva generación — ecosistema agéntico que
                         evoluciona y engloba las soluciones en un entorno común)
```

**El principio de experiencia:** cada empresa **activa las soluciones que necesita** según su operación. El usuario no compra "un ERP"; entra a un entorno y enciende lo que le sirve — y todo lo que enciende se entiende entre sí.

💡 *Por qué el nombre encaja:* «Cosmos» = un universo **ordenado** donde muchos cuerpos coexisten en un sistema. Conecta con "muchas soluciones, un solo entorno con orden". (Naming sigue ❓ — se decide en el brief de marca.)

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
| **RAG** | Ancla respuestas y orientación en el conocimiento real del sistema y la empresa. |
| *(futuras)* | Cualquier tecnología que siga reduciendo la operatividad manual se suma. |

> El norte no es "tener IA": es **que el usuario haga menos y entienda más**.

### 5.2. IA en cada solución (✅ dado)
Cada solución tiene **su propia inteligencia**, desarrollada desde el aprendizaje y apalancada en IA, para orientar al usuario y reducir su carga operativa. No es una IA central única: es **inteligencia distribuida, una por dominio** (ej. la categorización contable automática del módulo de Contabilidad; el "Modo Crucero" de Bitákora).

### 5.3. El asistente transversal de IA (✅ dado — validado en el producto)
Un asistente de IA cruza todo el ecosistema. Tres comportamientos ya observados en lo construido:

**a) Orienta — le dice al usuario qué hacer.** El inicio recibe al usuario por su nombre ("Hola, Ana") y le presenta sus **tareas priorizadas y agrupadas por módulo** con su urgencia ("1 vence hoy", "lleva 4 días sin corregir"). No espera que el usuario sepa qué sigue: se lo propone.

**b) Captura intenciones — predeterminadas y por lenguaje natural.** Conoce los procesos típicos de cada módulo (ej. *"Registrar compra"*) y guía sus campos; y una barra transversal **"Describe lo que necesitas…"** permite pedir en lenguaje natural, interpretando la intención y **validando los datos en vivo**.

**c) Extrae datos de documentos.** Lee documentos reales y **puebla los procesos**: del PDF del RUT de la DIAN completa el perfil tributario (NIT, régimen, país, actividad CIIU, atributos fiscales), marcando cada dato como *"Extraído con IA"* para que el usuario solo confirme.

> Estas tres capacidades (orientar, capturar, extraer) son el **lenguaje de interacción** central del ecosistema — y el punto de contacto más distintivo con el usuario.

### 5.4. Agéntico y orientado a eventos (✅ dado + 💡 visión)
Cada solución es (o tiende a ser) un **agente** que percibe, decide y actúa, comunicándose con las demás de forma **eventual** (event-driven) — sin llamadas manuales ni pasos que el usuario deba orquestar.

**Qué significa "agéntico" aquí** (no es un chatbot):
- **Percibe** hechos del negocio vengan de donde vengan (correo, documento, WhatsApp, evento de otro módulo).
- **Decide** qué corresponde, con su inteligencia de dominio y el aprendizaje acumulado.
- **Actúa** ejecutando o proponiendo el siguiente paso, y le cuenta al usuario qué hizo o qué revisar.
- **Colabora** con otros agentes: un hecho en un módulo dispara la cadena correcta en los demás.

**Caso ilustrativo:**
> Llega una **factura de compra** por el correo empresarial — o por **WhatsApp**, o el usuario tiene el **PDF/XML**. El sistema **la reconoce de inmediato**, extrae sus datos, la registra en el módulo contable y avisa al responsable solo si necesita una decisión. Nadie la digitó; un hecho del mundo real disparó, por eventos, toda la cadena.

---

## 6. La evolución de la idea (💡 hipótesis para alinear al equipo)

El cambio de fondo es un **desplazamiento del rol del usuario**: de operar a supervisar.

| Etapa | El sistema… | El usuario… |
|---|---|---|
| **1. Hoy — Asiste** | Orienta (muestra tareas y prioridades), extrae datos de documentos y entiende lo que el usuario quiere hacer. | **Ejecuta cada paso.** El sistema acompaña, no actúa por él. |
| **2. Propone** | Prepara el trabajo hecho (la factura ya registrada, la conciliación armada, la nómina calculada). | **Solo confirma.** El formulario desaparece detrás de una propuesta lista. |
| **3. Actúa con autonomía supervisada** | Ejecuta de punta a punta los procesos rutinarios y de bajo riesgo, bajo políticas definidas; **escala solo las excepciones**. | **Supervisa y decide** las excepciones. |
| **4. Agentes que colaboran** | Los agentes de los módulos se coordinan entre sí (compras ↔ contabilidad ↔ tesorería ↔ nómina). | Queda en el **centro de las decisiones que importan**; la operación fluye como una conversación entre agentes. |

> De *digitar y navegar formularios* → a *confirmar propuestas* → a *supervisar agentes*. Ese desplazamiento es la historia más poderosa que el producto puede contar — y el criterio de diseño de cada funcionalidad nueva.

---

## 7. Relación con la especificación de dominio (✅ dado)

Esta visión se materializa, pieza por pieza, en los **modelos de dominio** que ya construye el proyecto (DDD / Event Sourcing / EDA). La arquitectura **orientada a eventos** de esos modelos es, precisamente, lo que habilita el comportamiento agéntico descrito en §5.4.

- Mapa técnico del producto: `documento-consolidado-erp.md`.
- Modelos por sub-dominio: `dominio/` (ej. la categorización contable con IA está en `dominio/contabilidad/modelo-dominio.md` [D6]).
- Filosofía y flujo del proyecto: `README.md`.

> La visión dice *qué y por qué*; el dominio dice *cómo*. Deben mantenerse coherentes: si la visión evoluciona, los modelos la siguen, y viceversa.

---

## 8. Preguntas abiertas para el equipo (❓)

1. **Alcance de la primera materialización** — ¿qué módulos inauguran el ecosistema agéntico y en qué orden?
2. **Relación con SincoERP actual** — ¿Cosmos convive con SincoERP, lo reemplaza progresivamente, o es una capa nueva sobre lo existente?
3. **Hasta qué etapa de la evolución (§6) apuntamos en el corto plazo** — ¿nos quedamos en "Asiste/Propone" o ya pilotamos "Actúa con autonomía supervisada" en algún proceso?
4. **Naming y marca** — se trabaja en `brief-marca.md`.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.1 | Junio 2026 | Primera versión de la visión de producto. Separa el "qué/por qué" del ecosistema agéntico (antes mezclado en el brief de marca): problema que resuelve, producto actual SincoERP/Bitákora como punto de partida, visión del ecosistema, los cuatro ejes diferenciales (sin formularios, IA por solución, asistente transversal, agéntico/eventual), la evolución en 4 etapas y la relación con la especificación de dominio. «Cosmos» como nombre provisional. |
