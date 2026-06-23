# Guía: Datos entre dominios

## Propósito

Cómo un sub-dominio usa datos que pertenecen a otro: quién los gobierna, cómo se consumen sin acoplar la disponibilidad, cómo se sincronizan y qué hacer cuando un consumidor necesita un dato que el dueño todavía no tiene. Aplica a todos los sub-dominios del ERP.

Surge de una observación recurrente: en una arquitectura de microservicios asíncrona, el problema más común no es de modelado interno sino de **acoplamiento entre dominios** — un sub-dominio que no puede operar porque otro está caído, o que termina gestionando datos que no le pertenecen.

---

## 1. Principio rector: un dato, un dueño

> Cada dato tiene un único **dueño** que lo gobierna: lo crea, lo valida y decide su ciclo de vida. Los demás sub-dominios son **consumidores**: guardan una copia para operar, pero ninguno termina gestionando el dato como si fuera suyo. Si un consumidor empieza a crear o modificar el dato por su cuenta, el gobierno se rompe.

En el vocabulario de DDD (*context mapping*), el dueño es el sub-dominio **río arriba** (*upstream*) y el consumidor el **río abajo** (*downstream*).

Todo diseño que cruce datos entre dominios debe responder dos preguntas antes de nada:

1. **¿Quién es el dueño?** — uno solo.
2. **¿Cómo lo consumen los demás sin depender de él en tiempo de ejecución?**

---

## 2. Cómo consume el downstream: réplica local

El consumidor **no consulta al dueño cada vez que necesita el dato**. Mantiene una **copia local** y opera contra ella. El patrón canónico es **Event-Carried State Transfer** (transferencia de estado por eventos): el dueño publica eventos cuando su estado cambia —creado, actualizado, dado de baja— llevando los datos suficientes; el consumidor se suscribe y mantiene su copia al día.

> **Regla de oro: el consumidor nunca consulta al dueño en el camino crítico de su operación.** Opera contra su copia local. Si el dueño está caído, el consumidor sigue funcionando.

Por qué no consultar en caliente:

- **Acopla la disponibilidad:** si el dueño cae, el consumidor no opera — es justo el acoplamiento que esta arquitectura busca eliminar.
- **Acopla el rendimiento:** cada operación del consumidor quedaría atada a la latencia del dueño.

La copia local es una **proyección** (un *read model*, en CQRS): una vista materializada, **derivada y reconstruible**. Si se corrompe o se pierde, se rehace desde el dueño. No es una segunda fuente de verdad — la fuente de verdad siempre es el dueño.

### 2.1. El plano de la UI vs el plano del dominio

El desacople de un bounded context es una propiedad de su **backend/runtime**, no de su interfaz. La **UI / BFF es una capa de composición**: puede consumir componentes de varios dominios y leer al **dueño en vivo** (fuente de verdad) para mostrar, seleccionar o parametrizar. Si un dominio compuesto no está disponible, la UI **degrada** apoyándose en la capacidad del dominio principal de operar y diferir.

Dos consecuencias que conviene tener explícitas:

- **La UI lee al dueño, no a la copia local del consumidor.** La copia es una proyección eventualmente consistente y de propósito interno (validación e integridad del dominio); usarla para pintar datos en pantalla expondría datos *stale* y acoplaría la UI a un detalle interno del consumidor. Para mostrar y seleccionar, la UI va a la fuente de verdad.
- **La degradación de la UI se apoya en el dominio.** Que la UI permita completar una acción cuando un dominio compuesto cae solo es posible porque el dominio principal admite **operar y diferir** (sección 4). El desacople del backend es lo que habilita esa degradación — no son mecanismos independientes.

> **Regla práctica:** la **UI consume al dueño (en vivo)**; el **dominio del consumidor valida contra su copia local**. Es el mismo dato leído en dos planos distintos, por propósito y momento distintos (mostrar/seleccionar vs validar/imputar) — no es duplicación ni contradicción.

En la práctica, sobre el tamaño y el disparador de la copia: si la operación del consumidor **siempre elige el dato de la fuente de verdad** (vía la UI o reglas parametrizadas contra el dueño), el dato referenciado siempre existe en el dueño, y la copia local sirve sobre todo para **validar** (incluida la detección de cambios de estado posteriores, p. ej. una baja) y para **diferir** ante el desfase de propagación. No hace falta, en ese caso, un canal por el que el consumidor le "pida" al dueño crear el dato: ese canal solo se justifica si el consumidor puede referenciar algo que el dueño aún no tiene.

---

## 3. Sincronización: dos capas

**Capa 1 — Tiempo real (el camino normal).** El dueño publica eventos de cambio; el consumidor los proyecta en su copia local. Requisitos (ver [`arquitectura-eda.md`](arquitectura-eda.md)):

- Entrega *al menos una vez* + **consumidor idempotente**: reprocesar un evento repetido no cambia el resultado.
- Orden por versión o secuencia del dato: el consumidor descarta un evento más viejo que el último que ya aplicó.

**Capa 2 — Reconciliación de respaldo (defensa en profundidad).** Un proceso de fondo, periódico o bajo demanda, que **repara la copia local contra el dueño** cuando se desfasó (un consumidor que estuvo caído mucho tiempo, un evento perdido por un error). Puede ser un reproceso de eventos desde un punto, o una foto del estado actual del dueño.

> La reconciliación **sí** consulta al dueño, pero **de fondo, no bloqueante y fuera del camino de operar**: tolera que el dueño esté caído y reintenta después. No es el acoplamiento que se evita en la Capa 1.

La fuente de verdad siempre es el dueño; ambas capas solo mantienen la copia fiel.

---

## 4. Crear un dato que el consumidor necesita y el dueño no tiene

El consumidor detecta que necesita un dato que en el dueño todavía no existe. El principio no cambia: **solo el dueño crea el dato.** Lo que el consumidor hace depende de la **naturaleza del dato**. Tres estrategias:

| Estrategia | En qué consiste | Cuándo usarla |
|---|---|---|
| **Solicitar al dueño (asíncrono)** | El consumidor publica una intención; el dueño la procesa con sus reglas, crea el dato y emite el evento; el consumidor actualiza su copia y procede. | Cuando la operación del consumidor puede completarse sin ese dato en el instante y la creación puede esperar la propagación. |
| **Operar con un valor de respaldo y corregir después** | El consumidor usa un valor por defecto o provisional y reconcilia cuando el real llega. | **Solo si el dato tolera aproximación temporal** — es accesorio y no debe coincidir exacto entre sistemas. |
| **Diferir la parte que necesita el dato** | El consumidor registra todo lo demás y deja pendiente solo lo que requiere el dato; se resuelve cuando el evento de creación llega a su copia local. | Cuando el dato es **parte de la integridad** del hecho del consumidor y debe ser **exacto** — aproximarlo rompería la consistencia entre sistemas. |

**El criterio que decide:**

- ¿El dato debe **coincidir exacto** entre el sistema del consumidor y el del dueño (o un tercero, como el sistema contable)? Si sí, la estrategia del valor de respaldo queda **descartada** (desconcilia) → se difiere.
- ¿Es **accesorio o informativo**? Entonces el valor de respaldo sirve.

> **Diferir no es bloquear.** El consumidor no se detiene esperando una acción humana en el dueño ni una consulta en caliente: registra lo que puede y la parte pendiente se resuelve **sola** cuando el evento de creación llega a su copia local. Es consistencia eventual, no acoplamiento.

Y en ningún caso el consumidor **inventa** el dato en el dueño ni lo **aproxima** cuando debe ser exacto.

---

## 5. Caso especial: dato autovalidable y universal — empaquetar en vez de replicar

Hay datos que **no necesitan un dueño en tiempo de ejecución** porque son universales y se validan localmente con reglas compartidas: una identificación legal (formato + dígito de verificación por país), una dirección, un correo. Para estos, en vez de replicar desde un dueño, se **empaqueta** la estructura + las reglas + los datos estables, y viaja incluida en cada sub-dominio (en este ERP, los **Nuggets** — ver `compartido/nuggets/`).

La distinción:

| | Dato **gobernado** | Dato **autovalidable y universal** |
|---|---|---|
| Ejemplos | Unidad organizacional, perfil tributario | Identificación legal, dirección, correo, teléfono |
| Cómo se valida | Requiere conocer el estado de otros datos (catálogo del tenant, jerarquía) | Con reglas universales, sin consultar nada |
| Mecanismo | **Réplica local por eventos** (secciones 2-4) | **Empaquetar** (Nugget); la consolidación, si hace falta, es posterior |

**La prueba para distinguir:** *¿se puede validar este dato sin conocer el estado de otros datos?* Si sí → empaquetable. Si no → es gobernado y necesita dueño.

---

## 6. Anti-patrones

| Anti-patrón | Por qué es malo |
|---|---|
| **Consultar al dueño en el camino crítico** | Acopla la disponibilidad: el consumidor no opera si el dueño cae. |
| **Escribir en la base de datos del dueño** | Viola el encapsulamiento; el dueño deja de controlar su propio dato. |
| **El consumidor se vuelve dueño** (crea o gestiona el dato en su propio modelo) | El gobierno se distribuye y se pierde; el dato diverge entre sub-dominios. |
| **Aproximar un dato que debe ser exacto** | Desconcilia los sistemas — ej: imputar a un valor provisional que luego no coincide con la corrección del dueño. |

---

## 7. Ejemplos aplicados

**Terceros — dato autovalidable → empaquetar + consolidar.** La identificación legal es universal y autovalidable → se empaqueta (Nugget `IdentificacionLegal`); cada sub-dominio (OXP, Impuestos…) produce su tercero correctamente sin consultar a nadie; la bodega de Terceros **consolida** lo informado y concilia duplicados después. No hay réplica ni consulta en caliente.

**Estructura Organizacional — dato gobernado → réplica + diferir.** La unidad organizacional necesita dueño (unicidad en el tenant, jerarquía) → solo Estructura Organizacional la crea; OXP y Contabilidad mantienen **copia local por eventos** y operan contra ella. Cuando llega un gasto cuya unidad aún no existe y la unidad es obligatoria para el asiento (debe coincidir exacto entre operación y contabilidad), se **difiere** la causación de esa parte hasta que la unidad exista — nunca se aproxima con un valor provisional, porque desconciliaría.

**OXP → Contabilidad — frontera de traducción.** El tercero y la unidad viajan **embebidos** en las líneas de traducción (el hecho económico es completo e inmutable); Contabilidad valida contra sus copias locales, no consulta en caliente. (Ver [`separacion-responsabilidades.md`](separacion-responsabilidades.md).)

---

## 8. Relación con otras guías

- **[`arquitectura-eda.md`](arquitectura-eda.md)** — los mecanismos de mensajería que esta guía usa: tipos de evento, idempotencia, consistencia eventual, entrega. Esta guía no los repite; los aplica al problema de propiedad y réplica de datos.
- **[`separacion-responsabilidades.md`](separacion-responsabilidades.md)** — qué vocabulario conoce cada dominio (fugas de responsabilidad). Complementaria: aquella trata *qué* sabe cada dominio; esta, *cómo* consume lo que es de otro.
- **[`modelar-agregados.md`](modelar-agregados.md)** — decisiones de fronteras **dentro** de un bounded context.

---

## Historial

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Junio 2026 | Versión inicial. Surge del análisis de diseño del replanteamiento de Estructura Organizacional (eliminar acoplamientos de ejecución y proceso entre dominios, issue #45/#46). Consolida: principio de dueño único, réplica local por Event-Carried State Transfer, sincronización en dos capas (tiempo real + reconciliación de respaldo), las tres estrategias para datos faltantes con su criterio de elección, el caso del dato autovalidable (Nugget), anti-patrones y ejemplos aplicados (Terceros, Estructura Organizacional, OXP→Contabilidad). |
| 1.1 | Junio 2026 | **Principio de capas UI vs dominio (issue #72/#75).** Nueva sub-sección 2.1: el desacople es del backend/runtime, no de la UI; la UI compone y lee al dueño en vivo (fuente de verdad), no a la copia local del consumidor (que es para validación del dominio); la degradación de la UI se apoya en la capacidad del dominio de operar y diferir. En consecuencia: si el consumidor siempre elige el dato de la fuente de verdad, la copia sirve para validar y diferir, y no se requiere un canal de "demanda de creación" hacia el dueño. Surge del análisis con el equipo de desarrollo que motivó retirar el aparato de señal/bandeja de Estructura Organizacional. |
