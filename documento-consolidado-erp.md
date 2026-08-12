# Documento Consolidado — ERP Cosmos

> Visión general del ERP para equipos de desarrollo y diseño.
> Cada sub-dominio tiene documentación detallada en su carpeta — este documento es el mapa, no el territorio.

---

## 1. Visión general

### Qué es
Un ERP multi-país (Colombia, República Dominicana, Panamá) diseñado como un conjunto de microservicios independientes. Cada sub-dominio es un bounded context autónomo que se comunica con los demás mediante eventos.

### Arquitectura
- **Event-Driven Architecture (EDA):** Los sub-dominios se comunican publicando y consumiendo eventos de dominio. No hay llamadas directas entre módulos salvo consultas de lectura a servicios transversales.
- **Event Sourcing (ES):** Los agregados transaccionales persisten su estado como secuencia de eventos. Permite reconstrucción histórica y auditoría natural.
- **Consistencia:** Transaccional dentro de cada agregado, eventual entre agregados y entre sub-dominios.
- **Multi-moneda:** Soporte nativo para operaciones en moneda extranjera con tasa de cambio de referencia.

### Mapa de sub-dominios

```
┌─────────────────────────────────────────────────────────────┐
│                  SERVICIOS TRANSVERSALES                     │
│                                                             │
│   Datos de Referencia     Nuggets         Terceros          │
│   (catálogos base)        (VOs/validación) (bodega           │
│                                            consolidadora)   │
│   Estructura Organizacional                                 │
│   (unidades — dueño único, copia local en consumidores)    │
└──────┬──────────────────┬──────────────────┬────────────────┘
       │ lectura          │ eventos          │ eventos
       ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────┐
│                   SUB-DOMINIOS TRANSACCIONALES               │
│                                                             │
│   ┌───────────┐    ┌──────────────┐    ┌───────────────┐   │
│   │    OXP    │◄──►│  Impuestos   │    │ Contabilidad  │   │
│   │ (gastos)  │───►│  (tributos)  │    │  (asientos)   │   │
│   │           │───────────────────────►│               │   │
│   │           │◄───────────────────────│               │   │
│   └───────────┘    └──────────────┘    └───────────────┘   │
│                                                             │
│   ┌───────────┐    ┌──────────────┐    ┌───────────────┐   │
│   │    CXC    │    │  Tesorería   │    │   Emisión     │   │
│   │ (cobros)  │    │  (pagos)     │    │ Electrónica   │   │
│   │  FUTURO   │    │   FUTURO     │    │   FUTURO      │   │
│   └───────────┘    └──────────────┘    └───────────────┘   │
│                                                             │
│   ┌───────────────────────────┐                             │
│   │  Recepción Electrónica    │                             │
│   │        FUTURO             │                             │
│   └───────────────────────────┘                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Sub-dominios

### 2.1 Obligaciones por Pagar (OXP)

**Propósito:** Gestionar el ciclo de vida completo de las obligaciones de egreso de la empresa: desde que se recibe una factura o extracto hasta que se paga y se contabiliza.

| Agregados | Eventos | Domain Services | Invariantes | Decisiones | Premisas | Pendientes | Sugerencias |
|:---------:|:-------:|:---------------:|:-----------:|:----------:|:--------:|:----------:|:-----------:|
| 6 (+2 config) | 57 | 3 | 28 | 40 (D1-D40) | 3 | 4 | 10 |

**Componentes:**

| Agregado | Descripción |
|----------|-------------|
| OxpComercio | Obligación individual por compra (tarjeta, crédito, contado). Ciclo: radicación → confirmación → causación → pago. |
| OxpExtracto | Obligación consolidada por extracto bancario. Incluye conciliación automática contra OxpComercio. |
| Anticipo | Fondo adelantado al proveedor. Dos dimensiones: saldo de pago + saldo de regularización. |
| Devolucion | Nota crédito o devolución. 3 tipos: comercio, extracto, anticipo. Aplicación automática de crédito. |
| Proveedor | Rol del tercero en OXP (`[D30]`, #38): identidad propia = referenciaOrigen, estado Activo/Inactivo. `AsegurarProveedor` crea-o-reutiliza; publica el evento estándar de rol hacia la bodega de Terceros. |
| ProductoFinanciero | Registro de las tarjetas de la empresa (`[D39]`, #106): número (clave natural), emisor (tercero entidad financiera), configuración de cargos y alerta. Fuente del emisor y el medio de pago del extracto (copia al radicar); atribución con bandeja de pendientes; **la conciliación es por tarjeta** — el extracto consolidado quedó diferido como evolución. |
| CatalogoGastoDirecto (config) | Configuración de tipos de gasto que OXP puede crear directamente (sin módulo de gestión detrás). |
| CatalogoReglasDistribucion (config) | Reglas de distribución por unidad organizacional (`[D35]`, #51): por proveedor/tipo/lugar de ejecución, la más específica gana. Nivel A de la cadena de asignación de unidad. |

**Capacidades F1:** Radicación multi-canal, registro de proveedores (rol del tercero hacia la bodega de Terceros), registro de productos financieros (emisor y configuración del extracto), clasificación inteligente del origen, integración con Impuestos (cálculo tributario), conciliación automática de extractos, ciclo de anticipos, devoluciones con aplicación de crédito, causación hacia Contabilidad, monitoreo de pagos, control de doble pago, alertas.

**Capacidades F2:** Caja menor, viáticos/gastos de viaje, obligaciones recurrentes.

**3 domain services:** ServicioDeConciliacion (vinculación extracto ↔ comercio), ServicioDeRegularizacion (cruce anticipos ↔ comercio), ServicioDeAplicacionDevolucion (3 ramas por tipo de devolución).

**Integración con Contabilidad (cerrada):** causación del Anticipo (D25), generalización terminológica hacia "sistema contable" (D26), mapeo `tipoTransaccion` evento → plantilla de asiento (D27), canonización de `tipoComponente` con código fiscal específico (`iva`/`inc`/`retefuente`/`reteiva`/`reteica`) 1:1 con el catálogo de Contabilidad, y manejo de rechazos del sistema contable vía outbox del consumidor (D28). De jul-2026: ciclo contable de la **partida en disputa** vía cuenta transitoria de partidas por aclarar (`[D36]`, #90), **retenciones asumidas** por pago con tarjeta (`[D37]`, #94 — la retención se practica siempre, doctrina de concurrencia; con tarjeta no se descuenta del pagable, la asume la empresa) y **clasificación semántica del contrato de traducción** (`[D38]`, #104 — la clasificación de cada línea es texto compuesto mecánicamente desde los catálogos de OXP; la contrapartida viaja como línea; los componentes que saldan un hecho anterior se resuelven por espejo con `referenciaHechoRelacionado`). El mapeo canónico quedó en **18 `tipoComponente` y 6 plantillas**.

**Replanteamiento (#31/#45) y refinamientos recientes:** `Proveedor` como rol del tercero hacia la bodega de Terceros (#38); la **unidad organizacional se consume como copia local** por eventos de Estructura Organizacional —no agregado— y la causación **se difiere** cuando la unidad no existe (`[D34]`, `[SI8]`, #48); control de doble pago vía constancia humana (`[R38]`, `[D33]`, #30); **registro de productos financieros** — el emisor del extracto proviene del registro de tarjetas definido por el usuario, con atribución y bandeja de pendientes; conciliación por tarjeta, extracto consolidado diferido como evolución (`[D39]`, #106 — reemplaza la inferencia por histórico del #57); **asignación de la unidad por cadena de niveles** (Nivel A reglas configurables + Nivel B aprendizaje, `[D35]`, #51); **medio de pago canónico** — cadena de resolución con origen rastreado (la tarjeta referenciada al registro de productos financieros), retención en la confirmación de las resoluciones débiles y coherencia con el extracto garantizada por el control de saldos y la diferencia visible en el cruce — sin evento ni regla propios (`[D40]`, #96); **lote del equipo de desarrollo #126/#127** — el extracto no lleva medio de pago (se identifica por su producto financiero y copia el tipo de tarjeta) y la **cuenta consolidadora como dato agrupable** (diagnóstico de la salvaguarda de consolidado + advertencia al registrar un número que coincide con una cuenta anotada).

**Estado:** Alcance v1.20, modelo v4.13 (agosto 2026). En refinamiento continuo (Fase 2). Integración OXP ↔ Contabilidad **cerrada**; integración con Estructura Organizacional documentada (**copia local + diferir por consistencia eventual** — la señal de demanda se retiró en el #72: la copia es para validación, la UI lee a EO en vivo).

> Detalle: [`dominio/obligaciones-por-pagar/modelo-dominio.md`](dominio/obligaciones-por-pagar/modelo-dominio.md)

---

### 2.2 Impuestos

**Propósito:** Centralizar la configuración fiscal, el cálculo de tributos y el registro de hechos fiscales confirmados. Es la fuente de verdad tributaria del ERP.

| Agregados | Eventos | Domain Services | Invariantes | Decisiones | Premisas | Pendientes | Sugerencias |
|:---------:|:-------:|:---------------:|:-----------:|:----------:|:--------:|:----------:|:-----------:|
| 12 | 57 | 2 + flujo orquestado + read model | 28 | 16 | 6 | 12 | 3 |

> El BC tiene 16 elementos: 12 agregados con eventos (9 de configuración + 3 transaccionales) + 2 servicios de dominio (`MotorDeCalculo`, `CargaAsistida`) + 1 flujo orquestado (`ConfirmacionTributaria`) + 1 read model (`CatalogoJurisdiccional`).

**Componentes:**

| Agregado | Fase | Descripción |
|----------|------|-------------|
| CatalogoTributario | F1 | Raíz por país. Tributos, clasificaciones tributarias, matriz de tratamiento. |
| TarifaTributaria | F1 | Tarifas por jurisdicción + tributo, con vigencia temporal. |
| CondicionDeAplicacion | F1 | Modificadores según perfil de entidades fiscales (exenciones, tarifas especiales, reverse charge). Soporta roles de jurisdicción como entidad evaluada. |
| CatalogoDeAtributosFiscales | F1 | Esquema de qué atributos fiscales existen por país (régimen, autorretenedor, etc.). Soporta enums extensos vía `catalogoReferencia`. |
| PerfilTributario | F1 | Datos fiscales de cada entidad (empresa o tercero) por país. Incluye entidad interna `ActividadEconomicaRegistrada` con multiplicidad por jurisdicción + clasificación (resuelve CIIU correcto para ICA/RICA/autorretenciones). |
| JurisdiccionFiscal | F1 | Catálogo de jurisdicciones con regímenes territoriales. 4 tipos: `territorial-administrativa`, `regimen-especial-territorial` (precarga F1: Puerto Libre San Andrés), `distrito-fiscal-especial`, `soberania-tributaria` (F2 para US/CA). |
| CatalogoDeRegimenesEspeciales | F1 | Regímenes empresariales: zonas francas (DIAN, CNZFE), monopolios departamentales CO (Ley 1816/2016), ZEEs panameñas (ZLC/AEEPP/Ciudad del Saber), Puerto Libre empresarial. 5 tipos certificados en F1. |
| RegistroTributario | F1 | Hecho fiscal inmutable. Se crea al confirmar una transacción. Snapshot completo. |
| HomologacionFiscal | F2 | Mapeo de valores internos a códigos de autoridades (DIAN, DGII). |
| FormatoFiscal | F2 | Plantillas de reportes y certificados. |
| EntregableFiscal | F2 | Ciclo: Borrador → Generado → Presentado. Por período + formato + empresa. |
| CertificadoTributario | F2 | Generación y entrega de certificados de retención. |

**Tres responsabilidades:**
1. **Configuración fiscal** — Catálogo de tributos, tarifas, reglas, jurisdicciones y regímenes especiales. Preconfigurado para CO (11 tributos), DO (5), PA (4).
2. **Motor de cálculo** — Stateless. Recibe contexto transaccional, resuelve tributos aplicables. Retorna desglose propuesto + tributos descartados con motivo. Paso 2.c evalúa regímenes empresariales; paso 2.d resuelve CIIU según rol del sujeto pasivo + dirección fiscal.
3. **Cumplimiento fiscal** — Reportes (exógena, DGII, municipales), certificados tributarios. Consume registros propios.

**Cobertura por fase:**
- **F1 — LatAm completo:** CO/DO/PA con regímenes territoriales y empresariales precargados.
- **F2 — Apertura US/CA:** activación de tipos `distrito-fiscal-especial` y `soberania-tributaria`, resolución de jurisdicción por dirección/geocoding, decisión arquitectónica proveedor fiscal externo vs catálogo propio (ver `[PD11]`).

**Catálogos fiscales precargados (F1):** 27 pares de archivos (`.md` + `.json`), 983 entradas que cubren los 3 países F1 (CO: 514, DO: 260, PA: 209). Son **parte del producto** (`origen: estándar`) — pendiente refinamiento por consultores fiscales (cada `.md` lleva sección "Revisión pendiente"). Catálogo tributario CO **v1.4**: `ICA` solo en dirección `ingreso` (#93, validado con la consultoría fiscal), renombre `AUTO_RIVA` → `IVA_IMPORTACION_SERVICIOS` (#110 — la autorretención de IVA no existe como figura; el tributo es la autoliquidación en importación de servicios, con disparador por contraparte sin domicilio fiscal en el país) y retención de la sobretasa bomberil como `anticipado` (#108 — se descuenta de la sobretasa liquidada, no del ICA; validado con las dos consultoras).

**Replanteamiento (#31, #39):** el alta del `PerfilTributario` ya no depende de un registro centralizado de terceros — el comando `AsegurarPerfilTributario` crea-o-reutiliza por identificación × país (`[D16]`), validando la identidad con la pieza del paquete `IdentificacionLegal`; los eventos del perfil se publican hacia la bodega de Terceros (Impuestos es fuente, no consumidor).

**Estado:** Alcance v1.5, modelo v2.0.8 (julio 2026). Modelo completo + catálogos F1 entregados — refinamiento por consultores fiscales en curso (es el sub-dominio más avanzado en el hito de refinamiento).

> Detalle: [`dominio/impuestos/modelo-dominio.md`](dominio/impuestos/modelo-dominio.md), [`dominio/impuestos/datos-precargados/`](dominio/impuestos/datos-precargados/)

---

### 2.3 Contabilidad

**Propósito:** Traducir los hechos económicos de todos los sub-dominios a asientos contables. Dos niveles: N1 (motor de traducción) y N2 (sistema contable propio).

| Agregados | Eventos | Domain Services | Invariantes | Decisiones | Premisas | Pendientes | Sugerencias | Permisos |
|:---------:|:-------:|:---------------:|:-----------:|:----------:|:--------:|:----------:|:-----------:|:--------:|
| 13 | 59 | 3 | 33 (22 L + 11 E) | 14 (D1-D14) | 7 | 3 | 7 | 17 |

**Componentes:**

| Agregado | Nivel | Descripción |
|----------|-------|-------------|
| BorradorContable | N1 | Resultado de traducir un hecho económico. Estados: PENDIENTE → RESUELTO → ENTREGADO. |
| Aprendizaje | N1 | Registra resoluciones del contador para inferencia futura. |
| PlanDeCuentas | N1 | Catálogo jerárquico de cuentas (maestras y auxiliares). Atributo `marcoContable` como referencia inmutable al código del marco que rige el PUC. |
| MarcoContable | N1 (configuración) | Define el marco normativo bajo el cual se construye un PUC (ej: NIIF, US-GAAP, marco local). Habilita coexistencia controlada y arquitectura PUC único + libros paralelos. |
| ReglaDeDerivacion | N1 | Mapeo manual: dimensiones de transacción → cuenta auxiliar (Nivel A de resolución). |
| PlantillaDeAsiento | N1 | Estructura de débitos/créditos por tipo de transacción. Catálogo precargado **v1.9** con **6 plantillas OXP** (causacion_gasto, anticipo_a_proveedor, nota_credito_gasto, reversa_anticipo, amortizacion_anticipo, reclasificacion_partida). |
| EntregaContable | N1 | Entrega individual a sistema contable destino. Resultado: aceptado/rechazado. |
| SistemaContableDestino | N1 | Configuración del destino (SincoA&F en F1, adaptable a Siigo/Alegra). |
| AsientoContable | N2 | Registro inmutable del hecho contable. |
| PeriodoContable | N2 | Apertura y cierre de períodos. |
| LibroContable | N2 | Tipo como texto libre con predeterminados sugeridos: **Principal** y **Fiscal**. Libros adicionales bajo demanda (Gerencial, etc.). |
| NumeracionContable | N2 | Secuencias por tipo de comprobante. |
| EquivalenciaPuc | N2 | Mapeo cuenta a cuenta entre libros. Uso excepcional bajo arquitectura predeterminada moderna (D11). Se congela al registrar. |

**Arquitectura predeterminada moderna (D11):** un único PUC NIIF + libros paralelos (Principal y Fiscal por defecto) sobre el mismo PUC. Las diferencias contables se modelan como asientos específicos del libro Fiscal, no como PUCs paralelos. `EquivalenciaPuc` queda como excepción para clientes que decidan operar con PUCs distintos.

**Cadena de resolución de cuentas (3 niveles, evaluados A → C → B; insumo común: la clasificación semántica de la línea, `[D15]`/#104):**
- **Nivel A:** Regla manual del analista contable (ReglaDeDerivacion — partición estable exacta + texto ancla emparejado por similitud)
- **Nivel C:** Aprendizaje acumulado del sistema (resoluciones previas del contador, emparejadas por similitud dentro de la partición)
- **Nivel B:** Inferencia comparando la clasificación contra el plan de cuentas (acotada por el grupo del PUC esperado, `[R47]`)

> Los componentes que saldan un hecho anterior (cruces, aclaraciones, amortizaciones/reversas de anticipo, nota crédito) **no pasan por la cadena**: se resuelven por **espejo del hecho relacionado** (`[R53]`) — copian la cuenta del rol homólogo del borrador original. La **contrapartida** viaja como línea del consumidor (tercero + clasificación; el motor balancea el valor, `[R54]`).

> El nivel que resolvió cada partida queda registrado (`[R36]`). Esta cadena (configurable + aprendizaje) es el molde que OXP adoptó para la asignación de la unidad organizacional (`[D36]`, #51).

**Tres domain services:** `ServicioDeTraduccion` (N1, motor sin estado con validación contractual y motivos estructurados de rechazo), `ServicioDeAnulacion` (N2), `ServicioDeContabilizacion` (N2).

**Consola de contabilización:** Vista consolidada del estado de cada hecho económico (PENDIENTE / RESUELTO / ENTREGADO / RECHAZADO). Permite resolver cuentas, reintentar entregas, descartar borradores manuales. Los rechazos contractuales del motor (referencia de origen duplicada, tipo de transacción sin plantilla, línea sin rol en plantilla, línea sin clasificación, contrapartida faltante) no aparecen en la consola — se canalizan por mensajería (DLQ + logs + métricas) y la durabilidad la garantiza el outbox del consumidor emisor.

**17 permisos atómicos** definidos con convención `accion_recurso`.

**Refinamientos aplicados (integración con OXP):** grupo del PUC esperado por componente para acotar la inferencia Nivel B (D12), narración del borrador — descripción general + descripción de concepto por partida (D13), herencia del `rol` de la partida desde la plantilla y propagación a la entrega (D14), rol `CRUCE_OBLIGACION` en `causacion_gasto` (#18), plantilla `nota_credito_gasto` completada (#20), `terceroPrincipal` como fuente del tercero de la contrapartida (#28 — desde #104 es informativo: el tercero viaja en la línea `contrapartida`). De jul-2026: roles `PARTIDA_POR_ACLARAR`/`PARTIDA_ACLARADA` y plantilla `reclasificacion_partida` para el ciclo de la partida en disputa (#90), rol `IMPUESTO_ASUMIDO` para las retenciones asumidas por pago con tarjeta (#94) — cuentas `porValidar` por consultor contable —, y **clasificación semántica + contrapartida como línea + resolución por espejo** (#104: `[D15]`, `[R52]`-`[R54]`, `resolucionPorEspejo` en el catálogo de plantillas v1.9). De ago-2026: roles `IMPUESTO_AUTOLIQUIDADO`/`AUTOLIQUIDADO_POR_PAGAR` para los tributos de provisión — el par de líneas del autoliquidado (#128, `[D41]` de OXP; catálogo v1.10).

**Replanteamiento (#45, #47):** Estructura Organizacional deja de ser "fuente de verdad que se consulta" — N1 valida terceros y unidades contra **copia local por suscripción**, sin consulta en caliente (`R07`, `I7b`); la reestructuración de unidades es un hecho de negocio que la capa de reportería aplica al leer (no regla nueva). Contabilidad también realiza transacciones propias de ajuste (N2): no todo proviene de dominios externos.

**Estado:** Alcance v1.11, modelo v1.12 (julio 2026); catálogo de plantillas v1.10, anexo de ejemplos v1.5 (5 ejemplos). N1 listo para desarrollo F1 (N2 en F2) — refinamiento con el equipo de desarrollo en curso.

> Detalle: [`dominio/contabilidad/modelo-dominio.md`](dominio/contabilidad/modelo-dominio.md), [`dominio/contabilidad/anexo-marco-contable-y-arquitectura-puc.md`](dominio/contabilidad/anexo-marco-contable-y-arquitectura-puc.md)

---

### 2.4 Terceros

**Propósito:** **Bodega consolidadora** de los terceros del ERP (replanteamiento #31/#33). Cada dominio captura al tercero **en su rol** y la bodega **consolida** por clave natural, **concilia** duplicados y divergencias, y **señala** un estado global Activo/Inactivo. Nunca es prerrequisito para operar; publica decisiones, no datos.

| Agregados raíz | Domain Services | Value Objects | Eventos | Invariantes | Decisiones | Premisas | Pendientes | SIs | Permisos |
|:--------------:|:---------------:|:-------------:|:-------:|:-----------:|:----------:|:--------:|:----------:|:---:|:--------:|
| 2 | 1 | 7 | 17 (+contrato de entrada) | 15 (10 L + 5 E) | 12 | 4 | 4 | 9 | 11 |

**Componentes:**

| Elemento | Descripción |
|----------|-------------|
| Tercero (agregado raíz) | Identidad consolidada por clave natural (`IdentificacionLegal`). Contiene la entidad interna `Rol` con identidad por (dominio, referenciaOrigen) — los roles homólogos coexisten tras la fusión. Estado terminal `Fusionado`. |
| Conciliacion (agregado raíz) | Caso de duplicado o divergencia con resolución humana. FSM `Abierta → EnCorreccion → Cerrada`. |
| ServicioDeConsolidacion | Proceso de consolidación/fusión en 5 pasos, sin compensaciones. |
| Value Objects | 5 del paquete transversal (Nuggets: `IdentificacionLegal`, `Contacto`, etc.) + `Candidato` y `VersionDeDato`. |

**Modelo de comunicación:** cada dominio dueño de un rol emite el **contrato de entrada estándar** (estado completo del rol + secuencia, `[D5]`); la bodega consolida y, ante divergencias o señal global, **publica decisiones** que cada dominio aplica por su cuenta (injerencia por mensajes, nunca consulta en caliente). La captura es no bloqueante con degradación controlada.

**Prevención de duplicados:** unicidad por clave natural (`IdentificacionLegal`); detección de candidatos a conciliar; el canónico hereda la señal global más restrictiva (`[I14]`); anti-duplicación de casos (`[I15]` + `VersionAgregada`).

**Primera fuente real:** OXP (rol `Proveedor`, #38). Impuestos publica el `PerfilTributario` hacia la bodega (#39).

**11 permisos atómicos.** La v1.0 (autoridad de registro, estados EnRegistro/Abortado, dependencia de Direcciones) vive en el historial de git.

**Estado:** Alcance v2.0, modelo v2.0.2 (junio 2026). 2 auditorías completas aplicadas (28 + 6 hallazgos). Modelo cerrado para desarrollo (F1).

> Detalle: [`dominio/terceros/definicion-alcance.md`](dominio/terceros/definicion-alcance.md), [`dominio/terceros/modelo-dominio.md`](dominio/terceros/modelo-dominio.md)

---

### 2.5 Estructura Organizacional

**Propósito:** Estructura centralizada de las unidades de la empresa a las que se imputan transacciones para control de gestión (centros de costo, proyectos, sucursales, departamentos). Fuente de verdad de la pregunta "¿a qué unidad de la organización pertenece esta transacción?".

| Agregados raíz | Value Objects | Eventos | Invariantes | Decisiones | Premisas | Pendientes | Sugerencias | Permisos |
|:--------------:|:-------------:|:-------:|:-----------:|:----------:|:--------:|:----------:|:-----------:|:--------:|
| 3 | 5 | 18 | 15 (I01-I15) | 14 (D01-D14, +4 heredadas) | 5 | 3 | 10 (SI01-SI10) | 22 |

**Tres agregados raíz:**
- **GrupoOrganizacional:** Agrupador para presentación en reportes. No recibe transacciones. FSM de 2 estados. La estructura admite **varios grupos de primer nivel** (grupos sin padre — condición derivada `esDePrimerNivel()`), cada uno con su propio árbol; la consolidación "total compañía" la da la frontera del tenant (#85 — se retiró el grupo raíz único obligatorio, que nació sin justificación registrada; homologa con el ERP actual: los CC maestros son varios por empresa).
- **UnidadOrganizacional:** Nivel de detalle donde se imputan transacciones. FSM de 5 estados. Referencia su tipo por `tipoUnidadId`.
- **TipoUnidad:** Catálogo de tipos con ámbito del tenant (#86 — dejó de ser entidad interna del grupo raíz): id estable, nombre único por tenant y **renombrable**; el catálogo es la proyección de los tipos del tenant.

**FSM de la unidad (5 estados, 7 transiciones):** `Borrador` (preparación del administrador, antes de operar) o `Activa` → opera → `Suspendida` (pausada) → reactivable → `Inactiva` (reabrible) → reabrir, o `Descartada` (único terminal estricto, antes de operar). `GrupoOrganizacional` admite `GrupoModificado` en estado `Inactivo`; la unidad no, porque participa en historial transaccional.

**Gestiona:** Creación, jerarquía versionada por fecha efectiva, tipos de unidad del tenant, ciclo de vida (descartar un borrador es siempre decisión del administrador o cascada del grupo — el descarte automático por inactividad se retiró, #87), reestructuración (fusión, división, traslado como eventos de primera clase con respaldo IFRS 8).

**Cuatro decisiones arquitectónicas (anexo dedicado):**
1. **Codificación plana + jerarquía versionada aparte** — código plano de **texto libre** (sin embeber jerarquía; longitud mín/máx configurable por tenant, por defecto 2-12; unicidad sin distinguir mayúsculas — #89). Rompe con el patrón posicional de SincoA&F que tenía techo combinatorio y bloqueaba reestructuraciones.
2. **Ciclo de vida con 4 estados** (Borrador, Activa, Suspendida, Inactiva) en lugar de la dupla activo/inactivo.
3. **Fusión, División y Traslado modelados como eventos de dominio de primera clase**, no como mutaciones silenciosas.
4. **Modelo multi-dimensional desde el diseño**, aunque en F1 solo se exponga una dimensión.

**Relación con los consumidores — copia local + diferir (`[D13]`, replanteamientos #45/#72):** EO es el **dueño único** de las unidades; no hay creación bloqueante desde consumidores. (1) Los consumidores (OXP, Contabilidad) mantienen **copia local** por eventos — una proyección **para validación del dominio, no una API de lectura para la UI**: la UI lee a EO en vivo (principio de capas, guía `datos-entre-dominios.md` §2.1). (2) Como la unidad se elige de la fuente de verdad, una unidad referenciada siempre existe en EO; si su evento aún no llegó a la copia, el consumidor **difiere** solo lo que la requiere — consistencia eventual, sin bloquear ni aproximar. (3) La **señal de demanda se retiró** (#72): con la asignación resuelta contra la fuente de verdad, quedó sin disparador. Se eliminó el patrón viejo de creación desde consumidor (comando `SolicitarCreacionDeUnidad`, `origenSolicitud`, cancelación en cascada) y el anexo de orquestación.

**Patrón EDA:** publica el ciclo de vida de la unidad (`UnidadCreada`, `UnidadActivada`, `UnidadModificada`, `UnidadSuspendida`, `UnidadReactivada`, `UnidadReabierta`, `UnidadInactivada`, `UnidadDescartada`, `UnidadFusionada`, `UnidadDividida`, `UnidadTrasladada`) → los consumidores actualizan su copia local. La reestructuración es un hecho de negocio que la capa de reportería de cada consumidor aplica al leer. La validación de fecha efectiva se replanteó (`[R25]`/`[I08]`, #56): EO valida localmente contra la jerarquía vigente y la coherencia con la actividad transaccional es responsabilidad del administrador (la proyección de última imputación se retiró, #56).

**Estado:** Alcance v1.12, modelo v2.5, anexo de decisiones v1.6 (julio 2026). Auditoría completa (101 hallazgos) + replanteamientos #45/#72 + lote de refinamiento #85-#89 (PR #91) aplicados; numeración continua sin saltos (R1-R28, D01-D14, I01-I15, SI01-SI10 — la decisión de copia local es `[D13]`, antes D15). Listo para desarrollo F1.

> Detalle: [`dominio/estructura-organizacional/definicion-alcance.md`](dominio/estructura-organizacional/definicion-alcance.md), [`dominio/estructura-organizacional/anexo-decisiones-arquitectonicas.md`](dominio/estructura-organizacional/anexo-decisiones-arquitectonicas.md)

---

### 2.6 Datos de Referencia (servicio compartido)

**Propósito:** Servicio de infraestructura de datos. Gestiona los catálogos de referencia transversales que todos los sub-dominios consultan pero ninguno posee como parte de su dominio de negocio.

**No es sub-dominio DDD** — no tiene reglas de negocio propias, comportamiento, ciclos de vida complejos ni procesos orquestados. Es infraestructura compartida.

**Catálogos:**

| Catálogo | Naturaleza | Registros precargados |
|----------|------------|----------------------|
| Países | Estático | 195 países (ISO 3166-1) con moneda funcional e indicativo telefónico E.164 |
| Divisiones territoriales | Estático | 1.188 CO (DIVIPOLA), 221 DO, 108 PA |
| Monedas | Estático | 154 monedas (ISO 4217) |
| Tipos de documento de identidad | Estático | 45 tipos (CO, DO, PA, MX, CL, PE, EC, AR, BR + internacionales) |
| Tasas de cambio | Actualización diaria | Sin precarga — alimentado por sincronización o carga manual |

**Consumidores principales:** los Nuggets `Pais`/`Moneda`/`DivisionTerritorial` (fuente única dentro del paquete), Terceros, Impuestos (jurisdicción fiscal, divisiones para ICA/RICA), OXP (monedas, tasas de cambio), Contabilidad (monedas).

**Estrategia Seed + Sync + Extend:** carga inicial desde JSON precargados, sincronización periódica desde fuentes oficiales (Banco de la República CO, Banco Central RD), extensible por administrador.

**Replanteamiento (#31):** con la eliminación del servicio de Direcciones y la introducción de los Nuggets, el alcance pasó a v2.0 — el servicio se centra en **producción de catálogos + tasas de cambio** (la estructura de direcciones la empaqueta el Nugget `DireccionFisica`). Los catálogos de dirección (perfiles por país, tipos de vía/complemento, códigos postales CO) se produjeron y publicaron en el #77.

**Estado:** Alcance v2.0 (la especificación de servicio independiente se absorbió en el replanteamiento — el alcance v2.0 y los catálogos publicados son los artefactos vigentes). Listo para desarrollo.

> Detalle: [`compartido/datos-referencia/definicion-alcance.md`](compartido/datos-referencia/definicion-alcance.md)

---

### 2.7 Nuggets (servicio compartido)

**Propósito:** Catálogo + gobernanza de los **value objects transversales empaquetados** del ERP. Cada Nugget agrupa la estructura de un dato y sus validaciones (formato, reglas por país) para que todos los dominios lo capturen igual — y la bodega de Terceros pueda consolidar por la misma clave. La transversalidad se resuelve por **distribución** (validación empaquetada + información por eventos), no por un servicio en el camino crítico.

**8 Nuggets aceptados (en especificación):** `IdentificacionLegal` (clave natural universal), `DireccionFisica` (reemplaza al servicio de Direcciones eliminado), `Telefono`, `CorreoElectronico`, `Pais`, `Moneda`, `DivisionTerritorial` (fuente única de la jerarquía territorial; Impuestos resuelve jurisdicción fiscal directamente), `Contacto`. Diferidos: `ValorMonetario`, `Vigencia`. Candidato rechazado: `InformacionTercero` (resuelto como composición local, #38).

**Gobernanza:** filtros de admisión, proceso con custodio, versionado (catálogo y gobernanza en `compartido/nuggets/`).

**Estado:** Gobernanza + catálogo, 8 nuggets aceptados en borrador (replanteamiento #31). `DireccionFisica` es el más avanzado: datos embebidos producidos (#77) y especificación **v0.3** con la gramática formal de la línea de dirección, resuelta con el equipo de desarrollo (#100).

> Detalle: [`compartido/nuggets/catalogo-nuggets.md`](compartido/nuggets/catalogo-nuggets.md), [`compartido/nuggets/gobernanza-nuggets.md`](compartido/nuggets/gobernanza-nuggets.md)
>
> ⚫ **Servicio de Direcciones eliminado (junio 2026):** lo reemplaza el Nugget `DireccionFisica` (validación local empaquetada, sin servicio en ejecución). Documentos y catálogos en el historial de git.

---

### 2.8 Asistente de Onboarding (servicio compartido)

**Propósito:** Capacidad transversal del producto que guía la **carga inicial de configuración** de una empresa con sugerencias automáticas, aprendizaje acumulado e historial auditable. El **caso PUC** (cargue del plan de cuentas, consumido por Contabilidad N1) es el primero del patrón; otros casos futuros (terceros, unidades organizacionales, saldos iniciales) seguirán el mismo modelo.

**Estado:** Alcance v1.0, Modelo v1.0, Caso PUC v1.0 — listo para desarrollo (F1).

> Detalle: [`compartido/asistente-onboarding/`](compartido/asistente-onboarding/)

---

## 3. Mapa de integraciones

### Ciclo transaccional F1

```
  ┌─────────┐   solicitud cálculo (síncrona)   ┌──────────────┐
  │         │ ─────────────────────────────────► │              │
  │   OXP   │   desglose propuesto (síncrona)   │  Impuestos   │
  │         │ ◄───────────────────────────────── │              │
  │         │   confirmación (asíncrona)         │              │
  │         │ ─────────────────────────────────► │              │
  └────┬────┘                                    └──────────────┘
       │
       │ líneas de traducción (evento)
       ▼
  ┌───────────────┐
  │               │   borrador → entrega a SincoA&F
  │ Contabilidad  │ ─────────────────────────────────► SincoA&F
  │               │   EntregaAceptada (evento)
  │               │ ─────────────────────────────────► OXP
  └───────────────┘
```

### Consolidación de Terceros (bodega, #31)

Cada dominio captura al tercero **en su rol** y emite el contrato de entrada estándar (estado del rol + secuencia). La bodega consolida y publica decisiones; nadie la consulta en caliente.

```
  ┌─────────┐  rol Proveedor   ┌───────────┐
  │   OXP   │ ───────────────► │           │
  └─────────┘                  │ Terceros  │
  ┌─────────┐  PerfilTribut.   │ (bodega)  │
  │Impuestos│ ───────────────► │           │
  └─────────┘                  └─────┬─────┘
                                     │ señal global
                                     │ (decisión)
                                     ▼
                          cada dominio la aplica
                          por su cuenta
```

### Unidad organizacional — copia local + diferir (`[D13]`, #45/#72)

EO es el **dueño único**; los consumidores no lo consultan en el camino crítico. La copia local es **para validación del dominio** — la UI lee a EO en vivo (principio de capas, guía §2.1).

```
  ┌──────────┐  ciclo de vida   ┌──────────────┐
  │ Estruct. │ ───────────────► │  copia local │
  │ Organiz. │  (eventos)       │ (OXP/Contab.)│
  └──────────┘                  └──────┬───────┘
                                       │ ¿el evento aún
                                       │  no llega?
                                       ▼
                                difiere solo lo que la
                                requiere — consistencia
                                eventual, sin bloquear
                                ni aproximar
```

### Tabla de integraciones

> **Nota:** Este mapa es parcial. Las integraciones formalizadas corresponden a los sub-dominios con modelo completo (OXP, Impuestos, Contabilidad, Terceros, Estructura Organizacional, Datos de Referencia). Las marcadas como "Futuro" son integraciones esperadas según referencias en los modelos existentes, pero sus contratos aún no están definidos.
>
> **Sub-dominios sin integraciones definidas aún:** Tesorería, Activos Fijos, Arrendamientos. Sus integraciones se formalizarán cuando se construyan sus definiciones de alcance y modelo de dominio.

| Origen | Destino | Tipo | Contrato | Estado |
|--------|---------|------|----------|--------|
| OXP | Impuestos | Síncrono + Asíncrono | D22/D9: solicitud de cálculo + confirmación | Formalizado |
| OXP | Contabilidad | Evento | LineaTraduccion: tipoComponente, clasificacion, valor, tercero, undOrg | Formalizado |
| Contabilidad | OXP | Evento | EntregaAceptada: consecutivo del asiento en destino | Formalizado |
| OXP, Impuestos (fuentes de rol) | Terceros | Evento | Contrato de entrada estándar del rol (estado completo + secuencia, `[D5]`): OXP rol `Proveedor` (#38), Impuestos `PerfilTributario` (#39) | Formalizado v2.0 |
| Terceros (bodega) | OXP, Impuestos, Contabilidad, CXC, RRHH | Evento | Publica **decisiones**: señal global Activo/Inactivo + correcciones de identidad por conciliación; cada dominio las aplica por su cuenta (sin consulta en caliente) | Formalizado v2.0 |
| Estructura Org | OXP, Contabilidad | Eventos EDA | Ciclo de vida de la unidad y del tipo de unidad → copia local del consumidor (para validación; la UI lee a EO en vivo); el consumidor difiere por consistencia eventual (`[D13]`, #45/#72) | Formalizado — modelo v2.5 |
| Datos de Referencia | Todos (vía Nuggets) | Lectura | Catálogos (países, divisiones, monedas, tipos doc., tasas de cambio); los Nuggets `Pais`/`Moneda`/`DivisionTerritorial` son la fuente única dentro del paquete | Formalizado v2.0 |
| Recepción Electrónica | OXP | Evento | Documento validado → radicación automática | Futuro |
| Emisión Electrónica | CXC | Evento | Hecho de ingreso → emisión de factura electrónica | Futuro |
| Emisión Electrónica | OXP | Evento | Compra a no obligado → emisión doc. soporte | Futuro |
| Sistema externo de Nómina del cliente | Emisión Electrónica | API de importación | Datos de nómina del cliente para emisión electrónica (MX y otros países) | Futuro |
| Emisión Electrónica | Contabilidad | Evento | Asientos/balanza → emisión contabilidad electrónica (MX) | Futuro |
| CXC | Impuestos | Síncrono + Asíncrono | Mismo contrato D9 (dirección ingreso) | Futuro |
| CXC | Contabilidad | Evento | Mismo contrato LineaTraduccion | Futuro |
| Tesorería | Contabilidad | Evento | Mismo contrato LineaTraduccion | Futuro |

### Modelo federado de catálogos de conceptos

Cada sub-dominio de gestión es dueño de su catálogo de conceptos (productos, servicios, tipos de gasto). No existe catálogo centralizado. Todos referencian las clasificaciones tributarias y conceptos de pago de Impuestos (fuente de verdad fiscal).

> Detalle: [`integraciones/entre-dominios/catalogo-conceptos-por-dominio.md`](integraciones/entre-dominios/catalogo-conceptos-por-dominio.md)

---

## 4. Orden de construcción y prioridad

> **Tras el replanteamiento #31/#45 ya no hay "cadena de bloqueo" en ejecución:** ningún dominio depende de otro en el camino crítico. La transversalidad se resuelve por **distribución** — validaciones empaquetadas (Nuggets), información por eventos (copia local) y decisiones publicadas que cada dominio aplica por su cuenta. El orden de abajo es de **completitud de diseño** y de seed de catálogos, no de dependencia en tiempo de ejecución.

### Orden de diseño

```
Datos de Referencia ──► Nuggets ──► (terceros y unidades
       │                    │         se capturan en cada dominio
       │ seed                │ validación   y se distribuyen por eventos)
       ▼                    ▼
┌──────────────────────────────────────────────────────────────┐
│           OXP ◄──► Impuestos ──► Contabilidad                │
└──────────────────────────────────────────────────────────────┘
```

| Sub-dominio | Provee a | Se apoya en (diseño) | Estado |
|-------------|----------|----------------------|--------|
| Datos de Referencia | Todos (catálogos) | Ninguno | **v2.0 — listo para desarrollo** |
| Nuggets | Todos (validación empaquetada) | Datos de Referencia | **8 nuggets aceptados (borrador)** |
| Terceros (bodega) | Consolida roles; publica señal global | Nuggets (clave natural) | **v2.0.2 — cerrado para desarrollo F1** |
| Estructura Org | OXP, Contabilidad (copia local de unidades) | Datos de Referencia | **v2.5 — listo para desarrollo F1** |
| Impuestos | OXP (cálculo tributario) | Nuggets, Datos de Referencia | **v2.0.8 — modelo completo + catálogos F1** |
| Contabilidad | OXP (confirmación de asiento) | Terceros, Estructura Org (copia local) | **v1.12 — N1 listo para desarrollo F1** |
| OXP | Terceros (rol Proveedor) | Impuestos, Contabilidad, Estructura Org (copia local) | **v4.13 — Fase 2 (refinamiento continuo)** |

### Estado actual de construcción

- ✅ **Datos de Referencia** — v2.0 listo (producción de catálogos + tasas de cambio).
- 🟡 **Nuggets** — 8 nuggets aceptados en borrador (gobernanza + catálogo).
- ✅ **Terceros** — v2.0.2 cerrado para desarrollo F1 (bodega consolidadora, 2 auditorías).
- ✅ **Impuestos** — modelo v2.0.8 completo + catálogos F1 (LatAm CO/DO/PA, apertura US/CA F2).
- ✅ **Estructura Organizacional** — modelo v2.5 listo F1 (copia local + diferir; lote #85-#89 aplicado: varios grupos de primer nivel, `TipoUnidad` agregado propio, código de texto libre).
- ✅ **Contabilidad** — v1.12, N1 listo F1 (MarcoContable + arquitectura PUC + grupo PUC esperado + copia local de datos maestros; catálogo de plantillas v1.10 con 6 plantillas).
- 🔄 **OXP** — v4.13, Fase 2. Integración con Contabilidad **cerrada** y con Estructura Organizacional documentada; refinamiento continuo (últimos del equipo de desarrollo: #126/#127 — extracto sin medio de pago, cuenta consolidadora agrupable — y #128 — par de líneas del tributo de provisión, `[D41]`).

### Siguiente paso

Los sub-dominios base (Terceros, Estructura Org, Datos de Referencia, Nuggets) y los 3 transaccionales (OXP, Impuestos, Contabilidad) tienen modelo completo, y el replanteamiento #31/#45 ya eliminó los acoplamientos de ejecución entre ellos. El frente restante son el **refinamiento continuo de OXP**, el **refinamiento con el equipo de desarrollo** del resto de sub-dominios y los **transversales del ERP** (EventCatalog, infraestructura, UX). Ver el plan de trabajo activo para el detalle priorizado.

---

## 5. Fases por sub-dominio

| Sub-dominio | F1 | F2 |
|-------------|----|----|
| **OXP** | Radicación, clasificación inteligente, conciliación, anticipos, devoluciones, causación, integración Impuestos y Contabilidad | Caja menor, viáticos, obligaciones recurrentes |
| **Impuestos** | Configuración fiscal multi-país LatAm (CO/DO/PA, 11+5+4 tributos preconfigurados), motor de cálculo, perfiles tributarios con actividad económica por jurisdicción, carga asistida, registro tributario, gestión de jurisdicciones fiscales (incluido Puerto Libre San Andrés), regímenes especiales empresariales (zonas francas, monopolios departamentales CO, ZEEs panameñas) | Reportes de información (exógena, DGII), certificados tributarios, homologación fiscal, apertura multi-país a US/CA (distritos fiscales especiales, soberanías tributarias, resolución por dirección/geocoding) |
| **Contabilidad** | N1: Motor de traducción + entrega a SincoA&F. Cadena de resolución 3 niveles. Consola de contabilización. Aprendizaje. **MarcoContable** + arquitectura PUC único + libros paralelos (Principal, Fiscal). Validación contractual del motor (rechazos pre-borrador). | N2: Sistema contable propio (asientos, períodos, libros, numeración). Libros adicionales bajo demanda. Adaptadores adicionales (Siigo, Alegra). |
| **Terceros** | **Bodega consolidadora:** consolidación de roles por clave natural, conciliación de duplicados y divergencias con resolución humana, señal global Activo/Inactivo, asistencia de captura no bloqueante, vista consolidada de lectura. | Resolución de duplicados tardíos adicional, recepción electrónica |
| **Estructura Org** | Grupos, unidades con codificación plana + jerarquía versionada, FSM de 5 estados, copia local en los consumidores + diferir por consistencia eventual (`[D13]`), fusión/división/traslado como eventos de primera clase, eventos EDA | Multi-dimensionalidad expuesta (más allá de la dimensión inicial) |
| **Datos de Referencia** | 5 catálogos base (países, divisiones, monedas, tipos de documento, tasas de cambio), estrategia Seed + Sync + Extend | Extensiones por país, validación avanzada |
| **Nuggets** | 8 value objects transversales empaquetados (identificación legal, dirección física, teléfono, correo, país, moneda, división territorial, contacto) + gobernanza con custodio | Nuggets diferidos (`ValorMonetario`, `Vigencia`) |

---

## 6. Sub-dominios futuros

| Sub-dominio | Descripción | Dependencias conocidas |
|-------------|-------------|------------------------|
| **CXC (Cuentas por Cobrar)** | Gestión de obligaciones de ingreso. Mismo patrón que OXP pero dirección fiscal invertida (empresa es emisora). | Impuestos (mismo contrato D9), Contabilidad (LineaTraduccion), Terceros (clientes) |
| **Tesorería** | Gestión de pagos, cobros, transferencias, consignaciones, conciliación bancaria. | Contabilidad (LineaTraduccion), Terceros (cuentas bancarias) |
| **Emisión Electrónica** | Emisión de documentos electrónicos ante autoridades fiscales. Capacidades activables por el cliente: facturas + notas crédito/débito (ingreso), documentos soporte de compra + notas (gasto a no obligados a facturar), nómina electrónica (México y otros), contabilidad electrónica (México: catálogo de cuentas, balanza, pólizas al SAT). | Impuestos, Terceros, Nugget `DireccionFisica`. Cada capacidad se conecta con su fuente: facturas → CXC, documentos soporte → OXP, nómina → sistema externo de nómina del cliente, contabilidad → Contabilidad |
| **Recepción Electrónica** | Recepción, validación y gestión del ciclo de vida de documentos electrónicos de proveedores (facturas, notas). Alimenta el proceso de radicación de obligaciones de gasto. | Terceros. Se conecta con OXP para radicación automática |
| **Activos Fijos** | Control de activos, depreciación, valorización. | Contabilidad (LineaTraduccion) |
| **Arrendamientos** | Contratos de arrendamiento, comisiones, inmuebles. | OXP (gasto), CXC (ingreso), Contabilidad |

---

## 7. Composición comercial

La arquitectura de microservicios permite comercializar capacidades de forma modular. Cada cliente adquiere el producto que resuelve su necesidad y puede escalar a medida que crece.

### 7.1 Infraestructura base

Incluida en todo producto. No se vende por separado — es el cimiento que habilita cualquier capacidad transaccional.

| Componente | Qué aporta al cliente |
|------------|----------------------|
| **Datos de Referencia** | Catálogos multi-país (CO, DO, PA): países, monedas, tasas de cambio, tipos de documento, divisiones territoriales. Operación multi-moneda desde el día 1. |
| **Nuggets** | Value objects transversales empaquetados con validación local: identificación legal, dirección física (con soporte para facturación electrónica DIAN), teléfono, correo, contacto. Cada dominio captura los mismos datos igual. |
| **Terceros (bodega)** | Consolidación de proveedores, clientes, empleados y entidades financieras capturados por cada dominio en su rol. Conciliación de duplicados y señal global Activo/Inactivo. |
| **Estructura Organizacional** | Centros de costo, proyectos, sucursales, departamentos. Control de gestión y distribución de gastos/ingresos por unidad de negocio. |

### 7.2 Productos

#### Cosmos Contabilidad

Para empresas que necesitan un sistema contable moderno con traducción automática de hechos económicos.

| Sub-dominio | Capacidades incluidas |
|-------------|----------------------|
| Contabilidad (N1) | Motor de traducción automática con validación contractual, cadena de resolución de cuentas en 3 niveles (reglas → IA → aprendizaje), consola de contabilización, entrega a sistema destino, trazabilidad bidireccional. `MarcoContable` como agregado de configuración. |
| Contabilidad (N2) | Sistema contable propio: asientos inmutables, períodos contables, libros paralelos (Principal y Fiscal predeterminados sobre PUC NIIF único; libros adicionales bajo demanda), numeración configurable, auxiliares y saldos contables. |
| Infraestructura base | Datos de Referencia + Nuggets + Terceros (bodega) + Estructura Organizacional. |

**Para quién:** Empresa que quiere reemplazar su sistema contable actual o que no tiene uno y necesita arrancar de cero. Puede recibir hechos económicos de cualquier origen (manual o desde otros módulos Cosmos).

---

#### Cosmos Gastos

Para empresas que necesitan gestionar el ciclo completo de sus obligaciones de egreso: desde la factura hasta el asiento contable.

| Sub-dominio | Capacidades incluidas |
|-------------|----------------------|
| OXP | Radicación multi-canal (XML, PDF, manual), clasificación inteligente del origen, conciliación automática de extractos bancarios, ciclo de anticipos, devoluciones con aplicación de crédito, monitoreo de pagos, alertas. |
| Impuestos (cálculo) | Configuración fiscal preconfigurada (CO, DO, PA), motor de cálculo automático de tributos, perfiles tributarios por entidad, carga asistida desde fuentes oficiales (DIAN, DGII). |
| Contabilidad (N1) | Motor de traducción automática de cada obligación causada a borrador contable. Entrega al sistema contable del cliente (SincoA&F, Siigo, o Cosmos Contabilidad N2). |
| Infraestructura base | Datos de Referencia + Nuggets + Terceros (bodega) + Estructura Organizacional. |

**Para quién:** Empresa que procesa facturas de proveedores, extractos de tarjeta corporativa, anticipos a terceros. Tiene o no tiene sistema contable propio — N1 se adapta al destino.

---

#### Cosmos Impuestos

Para empresas o partners con ERP existente que necesitan un motor fiscal multi-país para LatAm.

| Sub-dominio | Capacidades incluidas |
|-------------|----------------------|
| Impuestos (completo) | Configuración fiscal multi-país LatAm (CO, DO, PA), motor de cálculo stateless vía API, perfiles tributarios con actividad económica por jurisdicción + clasificación, gestión de jurisdicciones fiscales (incluido Puerto Libre San Andrés), regímenes especiales empresariales (zonas francas DIAN/CNZFE, monopolios departamentales CO, ZEEs panameñas), carga asistida, registro tributario inmutable, cumplimiento fiscal (reportes exógena, DGII, municipales, certificados de retención). F2 abre US/CA. |
| Infraestructura base | Datos de Referencia + Terceros (para perfiles tributarios). |

**Para quién:** Partner tecnológico que tiene su propio ERP y necesita un motor fiscal confiable para LatAm. Empresa grande con ERP legacy (SAP, Oracle, Dynamics, SincoA&F) que necesita modernizar su capa fiscal sin migrar todo el sistema. Se integra vía contrato semántico (API de solicitud de cálculo + confirmación).

---

#### Cosmos CXC *(futuro)*

Para empresas que necesitan gestionar el ciclo de obligaciones de ingreso: facturación, cobro, cartera.

| Sub-dominio | Capacidades incluidas |
|-------------|----------------------|
| CXC | Gestión de cuentas por cobrar, seguimiento de cartera, provisiones, notas crédito/débito de cliente. |
| Impuestos (cálculo) | Mismo motor, dirección fiscal invertida (empresa es emisora). |
| Contabilidad (N1) | Traducción automática de cada hecho de ingreso. |
| Infraestructura base | Datos de Referencia + Nuggets + Terceros (bodega) + Estructura Organizacional. |

**Para quién:** Empresa que necesita controlar su cartera de clientes y automatizar el ciclo de cobro.

---

#### Cosmos Tesorería *(futuro)*

Para empresas que necesitan gestionar pagos, cobros, transferencias y conciliación bancaria.

| Sub-dominio | Capacidades incluidas |
|-------------|----------------------|
| Tesorería | Pagos a proveedores, cobros de clientes, transferencias entre cuentas, consignaciones, conciliación bancaria, caja menor. |
| Contabilidad (N1) | Traducción automática de cada movimiento de tesorería. |
| Infraestructura base | Datos de Referencia + Terceros (con cuentas bancarias) + Estructura Organizacional. |

**Para quién:** Empresa que necesita centralizar y automatizar sus operaciones bancarias.

---

#### Cosmos Emisión Electrónica *(futuro)*

Para empresas que necesitan emitir documentos electrónicos ante autoridades fiscales. El cliente activa solo las capacidades que necesita.

| Capacidad activable | Descripción | Se conecta con |
|---------------------|-------------|----------------|
| Facturas electrónicas + notas crédito/débito | Documentos de ingreso ante DIAN, DGII, SAT | CXC (si tiene) |
| Documentos soporte de compra + notas | Compras a personas/empresas no obligadas a facturar electrónicamente | OXP (si tiene) |
| Nómina electrónica | Emisión de nómina electrónica (México y otros países) | Sistema externo de nómina del cliente (Cosmos no construye nómina) |
| Contabilidad electrónica | Catálogo de cuentas, balanza de comprobación, pólizas al SAT (México) | Contabilidad (si tiene) |

Incluye infraestructura base (Datos de Referencia + Nuggets + Terceros).

**Para quién:** Empresa que necesita cumplir con obligaciones de emisión electrónica. Puede ser independiente (el cliente solo quiere emitir) o complementar otros productos Cosmos. Cada capacidad activada ajusta el costo.

---

#### Cosmos Recepción Electrónica *(futuro)*

Para empresas que necesitan recibir, validar y gestionar documentos electrónicos de proveedores.

| Sub-dominio | Capacidades incluidas |
|-------------|----------------------|
| Recepción Electrónica | Recepción de facturas electrónicas de proveedores, validación de autenticidad ante autoridad fiscal, gestión del ciclo de aceptación/rechazo, extracción automática de datos del documento. |
| Infraestructura base | Datos de Referencia + Terceros. |

**Para quién:** Empresa que recibe facturas electrónicas de proveedores y necesita validarlas y gestionarlas. Se conecta con OXP para radicación automática de obligaciones si el cliente tiene Cosmos Gastos.

---

#### Cosmos ERP *(full)*

Todos los sub-dominios integrados. La solución completa.

| Sub-dominio | Capacidades incluidas |
|-------------|----------------------|
| Todos | OXP + CXC + Tesorería + Impuestos + Contabilidad (N1+N2) + Emisión Electrónica + Recepción Electrónica + infraestructura base. |

**Para quién:** Empresa que quiere reemplazar su ERP actual con una solución moderna, integrada y multi-país.

---

### 7.3 Matriz de capacidades por producto

| Capacidad | Contabilidad | Gastos | Impuestos | Emisión | Recepción | CXC | Tesorería | ERP |
|-----------|:----:|:-----:|:---------:|:------:|:---------:|:---:|:---------:|:---:|
| **Gestión de terceros** | ● | ● | ● | ● | ● | ● | ● | ● |
| **Estructura organizacional** | ● | ● | ○ | — | — | ● | ● | ● |
| **Catálogos multi-país** | ● | ● | ● | ● | ● | ● | ● | ● |
| **Direcciones estructuradas (Nugget)** | ● | ● | ● | ● | ● | ● | ● | ● |
| **Radicación de obligaciones** | — | ● | — | — | — | — | — | ● |
| **Clasificación inteligente** | — | ● | — | — | — | — | — | ● |
| **Conciliación de extractos** | — | ● | — | — | — | — | — | ● |
| **Ciclo de anticipos** | — | ● | — | — | — | — | — | ● |
| **Devoluciones y créditos** | — | ● | — | — | — | ● | — | ● |
| **Configuración fiscal multi-país** | — | ● | ● | — | — | ● | — | ● |
| **Motor de cálculo tributario** | — | ● | ● | — | — | ● | — | ● |
| **Perfiles tributarios** | — | ● | ● | — | — | ● | — | ● |
| **Registro tributario inmutable** | — | ● | ● | — | — | ● | — | ● |
| **Reportes fiscales (exógena, DGII)** | — | — | ● | — | — | — | — | ● |
| **Certificados de retención** | — | — | ● | — | — | — | — | ● |
| **Traducción contable automática** | ● | ● | — | — | — | ● | ● | ● |
| **Cadena de resolución (reglas + aprendizaje + inferencia)** | ● | ● | — | — | — | ● | ● | ● |
| **Consola de contabilización** | ● | ● | — | — | — | ● | ● | ● |
| **Asientos contables (N2)** | ● | — | — | — | — | — | — | ● |
| **Multi-libro (Principal, Fiscal)** | ● | — | — | — | — | — | — | ● |
| **Períodos contables** | ● | — | — | — | — | — | — | ● |
| **Emisión de facturas electrónicas** | — | — | — | ◆ | — | — | — | ● |
| **Emisión doc. soporte de compra** | — | — | — | ◆ | — | — | — | ● |
| **Emisión nómina electrónica** | — | — | — | ◆ | — | — | — | ● |
| **Emisión contabilidad electrónica** | — | — | — | ◆ | — | — | — | ● |
| **Recepción facturas de proveedores** | — | — | — | — | ● | — | — | ● |
| **Validación ante autoridad fiscal** | — | — | — | — | ● | — | — | ● |
| **Gestión de cartera** | — | — | — | — | — | ● | — | ● |
| **Conciliación bancaria** | — | — | — | — | — | — | ● | ● |
| **Pagos y cobros centralizados** | — | — | — | — | — | — | ● | ● |

● incluido — ○ parcial — ◆ activable (el cliente elige cuáles capacidades activa)

---

### 7.4 Escenarios de composición

**Escenario 1 — Empresa mediana en Colombia con SincoA&F**
> "Tengo SincoA&F para contabilidad pero proceso facturas de proveedores y extractos de tarjeta en Excel."
>
> **Producto:** Cosmos Gastos. OXP gestiona las obligaciones, Impuestos calcula tributos automáticamente, N1 traduce y entrega los asientos a SincoA&F. El cliente no cambia de sistema contable.

**Escenario 2 — Empresa multi-país que necesita motor fiscal**
> "Tengo mi propio ERP pero necesito un motor de impuestos confiable para Colombia, República Dominicana y Panamá."
>
> **Producto:** Cosmos Impuestos. Se integra vía API (solicitud de cálculo + confirmación). El cliente consume el motor desde su ERP existente. Incluye reportes de cumplimiento fiscal.

**Escenario 3 — Empresa que solo necesita contabilidad moderna**
> "Quiero reemplazar mi sistema contable actual por algo con IA que aprenda las cuentas de mis transacciones."
>
> **Producto:** Cosmos Contabilidad. N1 traduce hechos económicos con cadena de resolución inteligente (reglas → aprendizaje → inferencia por PUC). N2 es el sistema contable con multi-libro y períodos.

**Escenario 4 — Empresa que necesita cumplir con emisión electrónica**
> "Tengo mi ERP pero necesito emitir facturas electrónicas ante la DIAN y recibir las de mis proveedores."
>
> **Productos:** Cosmos Emisión Electrónica (activa solo facturas + notas) + Cosmos Recepción Electrónica. Funcionan de forma independiente. Si el cliente luego adquiere Cosmos Gastos, la recepción alimenta automáticamente la radicación de OXP.

**Escenario 5 — Empresa en México que necesita contabilidad electrónica**
> "Necesito enviar mi catálogo de cuentas, balanza y pólizas al SAT cada mes."
>
> **Productos:** Cosmos Contabilidad + Cosmos Emisión Electrónica (activa solo contabilidad electrónica). N2 gestiona los asientos y Emisión genera los XML para el Buzón Tributario.

**Escenario 6 — Empresa que quiere el ERP completo**
> "Quiero reemplazar todo: gastos, cobros, tesorería, contabilidad, impuestos. Una sola plataforma."
>
> **Producto:** Cosmos ERP. Todos los módulos integrados, un solo ecosistema de eventos.

---

### 7.5 Modelo de crecimiento

Un cliente puede empezar con un producto y escalar sin fricción. Cada módulo se conecta al ecosistema existente porque comparten la misma infraestructura base y el mismo bus de eventos.

```
                         Cosmos ERP (full)
                              ▲
            ┌─────────────────┼─────────────────┐
            │                 │                 │
      + Tesorería         + CXC        + Contabilidad N2
            ▲                 ▲                 ▲
            │                 │                 │
            └─────────────────┼─────────────────┘
                              │
                        Cosmos Gastos
                   (punto de entrada más común)
                              ▲
                              │
                    Cosmos Contabilidad
                   (punto de entrada alternativo)

    ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─
    Productos complementarios (se agregan a cualquier
    producto en cualquier momento):

      Cosmos Emisión Electrónica (capacidades activables)
      Cosmos Recepción Electrónica
      Cosmos Impuestos (para partners)
```

**Camino típico:**
1. El cliente empieza con **Cosmos Gastos** (su dolor inmediato: facturas, extractos, anticipos)
2. Agrega **Cosmos Recepción Electrónica** para recibir facturas de proveedores y alimentar OXP automáticamente
3. Agrega **Cosmos Contabilidad N2** cuando quiere reemplazar SincoA&F con sistema contable propio
4. Agrega **Cosmos CXC** + **Cosmos Emisión Electrónica** (facturas) cuando necesita gestionar cartera e interactuar con la DIAN/DGII
5. Agrega **Cosmos Tesorería** cuando quiere centralizar pagos y conciliación bancaria
6. Resultado: **Cosmos ERP** completo, construido de forma incremental

**Camino alternativo:**
1. El cliente empieza con **Cosmos Contabilidad** (quiere sistema contable moderno)
2. Agrega **Cosmos Gastos** cuando necesita automatizar obligaciones de egreso
3. Escala con CXC, Tesorería, Emisión y Recepción según necesidad

**Camino electrónico:**
1. El cliente empieza con **Cosmos Emisión Electrónica** y/o **Cosmos Recepción Electrónica** (cumplimiento regulatorio)
2. Cuando quiere gestionar las obligaciones detrás de esos documentos, agrega **Cosmos Gastos** o **Cosmos CXC**
3. Escala al ERP completo

**Partner:**
1. El partner integra **Cosmos Impuestos** vía API en su ERP existente
2. Puede agregar **Cosmos Emisión Electrónica** para cubrir obligaciones regulatorias de sus clientes
3. Si decide migrar más funcionalidad, escala a Cosmos Gastos o Cosmos ERP

---

## Referencias

| Archivo | Contenido |
|---------|-----------|
| [`dominio/obligaciones-por-pagar/definicion-alcance.md`](dominio/obligaciones-por-pagar/definicion-alcance.md) | Alcance OXP: glosario, reglas, fases |
| [`dominio/obligaciones-por-pagar/modelo-dominio.md`](dominio/obligaciones-por-pagar/modelo-dominio.md) | Modelo OXP: agregados, eventos, invariantes, FSM |
| [`dominio/impuestos/definicion-alcance.md`](dominio/impuestos/definicion-alcance.md) | Alcance Impuestos: glosario, reglas, fases |
| [`dominio/impuestos/modelo-dominio.md`](dominio/impuestos/modelo-dominio.md) | Modelo Impuestos: agregados, eventos, invariantes |
| [`dominio/contabilidad/definicion-alcance.md`](dominio/contabilidad/definicion-alcance.md) | Alcance Contabilidad: glosario, reglas, fases |
| [`dominio/contabilidad/modelo-dominio.md`](dominio/contabilidad/modelo-dominio.md) | Modelo Contabilidad: agregados, eventos, invariantes, permisos |
| [`dominio/contabilidad/anexo-marco-contable-y-arquitectura-puc.md`](dominio/contabilidad/anexo-marco-contable-y-arquitectura-puc.md) | Investigación de seis ERPs modernos. Justificación de la arquitectura PUC único + libros paralelos (Principal/Fiscal) y `MarcoContable` como agregado de configuración |
| [`dominio/impuestos/anexo-catalogo-regimenes-especiales.md`](dominio/impuestos/anexo-catalogo-regimenes-especiales.md) | Regímenes empresariales: zonas francas (DIAN, CNZFE), monopolios departamentales CO, ZEEs panameñas, Puerto Libre. Fuentes normativas. |
| [`dominio/terceros/definicion-alcance.md`](dominio/terceros/definicion-alcance.md) | Alcance Terceros: glosario, reglas, flujos, fases |
| [`dominio/terceros/modelo-dominio.md`](dominio/terceros/modelo-dominio.md) | Modelo Terceros v2.0: bodega consolidadora — agregados Tercero y Conciliacion, ServicioDeConsolidacion |
| [`dominio/estructura-organizacional/definicion-alcance.md`](dominio/estructura-organizacional/definicion-alcance.md) | Alcance Estructura Organizacional (v1.4): glosario, actores, flujos, reglas |
| [`dominio/estructura-organizacional/anexo-definicion-contexto-inicial.md`](dominio/estructura-organizacional/anexo-definicion-contexto-inicial.md) | Definición inicial de contexto (preexistente al alcance formal) |
| [`dominio/estructura-organizacional/anexo-decisiones-arquitectonicas.md`](dominio/estructura-organizacional/anexo-decisiones-arquitectonicas.md) | Cuatro decisiones arquitectónicas: codificación plana + jerarquía versionada, FSM 4 estados, fusión/división/traslado como eventos, multi-dimensionalidad |
| [`compartido/datos-referencia/definicion-alcance.md`](compartido/datos-referencia/definicion-alcance.md) | Alcance Datos de Referencia (v2.0 — producción de catálogos + tasas de cambio) |
| [`compartido/nuggets/gobernanza-nuggets.md`](compartido/nuggets/gobernanza-nuggets.md) | Gobernanza de Nuggets: filtros de admisión, proceso con custodio, versionado |
| [`compartido/nuggets/catalogo-nuggets.md`](compartido/nuggets/catalogo-nuggets.md) | Catálogo de Nuggets (8 aceptados en borrador) |
| [`compartido/asistente-onboarding/`](compartido/asistente-onboarding/) | Asistente de Onboarding (caso PUC v1.0): patrón transversal de carga inicial |
| [`compartido/anexo-decision-i18n-l10n.md`](compartido/anexo-decision-i18n-l10n.md) | Decisión transversal de internacionalización/localización |
| [`guias-de-modelado/datos-entre-dominios.md`](guias-de-modelado/datos-entre-dominios.md) | Criterio transversal: dueño único + réplica local por eventos + diferir; principio de capas §2.1 — la copia es para validación, la UI lee al dueño en vivo (fundamento de los replanteamientos #31/#45/#72) |
| [`guias-de-modelado/topologia-equipos-despliegue.md`](guias-de-modelado/topologia-equipos-despliegue.md) | Topología de equipos y despliegue |
| [`integraciones/entre-dominios/catalogo-conceptos-por-dominio.md`](integraciones/entre-dominios/catalogo-conceptos-por-dominio.md) | Modelo federado de catálogos, contratos entre dominios |
| [`plan-trabajo-junio.md`](plan-trabajo-junio.md) | Plan de ejecución con orden de prioridad |

---

## 8. Avance por sub-dominio

> Snapshot al **10 de julio de 2026**. Refleja completitud de los artefactos de diseño que habilitan el inicio de desarrollo, no el avance de la implementación en código.

### Metodología

El porcentaje de avance combina cinco hitos. Cada hito tiene un peso fijo y se evalúa como ✅ (completo), 🟡 (parcial — se cuenta el % alcanzado del hito) o ⬜ (pendiente). El símbolo — indica que el hito no aplica para ese sub-dominio.

**Esquema para sub-dominios de negocio** (OXP, Impuestos, Contabilidad, Terceros, Estructura Organizacional):

| Hito | Peso | Criterio de cierre |
|------|:----:|--------------------|
| **Alcance** | 20% | `definicion-alcance.md` aprobado + anexos de decisiones arquitectónicas formalizadas |
| **Modelo** | 25% | `modelo-dominio.md` v1.0+ con agregados, eventos, invariantes, FSM, domain services definidos |
| **Auditoría** | 15% | 11 skills de auditoría ejecutadas + hallazgos resueltos o descartados con justificación |
| **Refinamiento** | 30% | Consultas del equipo de diseño y del equipo de desarrollo resueltas y aplicadas al modelo. Sello de validación cruzada antes de pasar a desarrollo. |
| **Listo para F1** | 10% | Decisiones cerradas, pendientes documentados sin bloqueos, integraciones contractadas con consumidores |

**Esquema para servicios de infraestructura** (Datos de Referencia, Nuggets) — sin auditoría formal de dominio porque no son bounded contexts DDD:

| Hito | Peso | Criterio de cierre |
|------|:----:|--------------------|
| **Alcance** | 30% | `definicion-alcance.md` aprobado |
| **Especificación** | 30% | `especificacion-servicio.md` aprobada (catálogos, contratos, datos precargados) |
| **Refinamiento** | 30% | Consultas del equipo de diseño y del equipo de desarrollo resueltas y aplicadas. |
| **Listo para F1** | 10% | Catálogos precargados disponibles, contratos con consumidores cerrados |

> **Nota — peso del Refinamiento (30%):** Refleja que ningún artefacto puede considerarse listo para desarrollo hasta que sea validado por el equipo de desarrollo que lo va a usar. La auditoría asegura coherencia interna del modelo; el refinamiento asegura coherencia con la realidad operativa del diseño y la viabilidad técnica. Hoy **OXP, Impuestos y Contabilidad** tienen refinamiento en progreso (issues del equipo de desarrollo aplicados — ej: #18/#25/#28/#30/#38/#51/#90/#94 en OXP, #39/#93 en Impuestos, #17/#18/#20/#28/#47/#90/#94 en Contabilidad), **Estructura Organizacional** cerró además el lote #85-#89 del refinamiento (jul-2026) sobre los replanteamientos #31/#45/#72, y **Terceros** absorbió el replanteamiento #31 (bodega consolidadora) además de sus auditorías.

### Tabla de avance

| Sub-dominio | Alcance | Modelo / Especificación | Auditoría | Refinamiento | Listo F1 | **Avance** |
|-------------|:-------:|:-----------------------:|:---------:|:------------:|:--------:|:----------:|
| Datos de Referencia | ✅ | ✅ | — | 🟡 (60%) | ⬜ | **78%** |
| Nuggets | ✅ | 🟡 (45%) | — | 🟡 (10%) | ⬜ | **47%** |
| Terceros | ✅ | ✅ | ✅ | 🟡 (50%) | ⬜ | **75%** |
| Estructura Organizacional | ✅ | ✅ | ✅ | 🟡 (70%) | ⬜ | **81%** |
| OXP | ✅ | ✅ | ✅ | 🟡 (75%) | ⬜ | **82%** |
| Contabilidad | ✅ | ✅ | ✅ | 🟡 (75%) | ⬜ | **82%** |
| Impuestos | ✅ | ✅ | ✅ | 🟡 (85%) | ⬜ | **85%** |

> Los porcentajes son estimaciones del snapshot bajo la metodología de arriba; el factor que falta para el 100% en todos es la validación final con el equipo de desarrollo y el cierre formal de "Listo F1".

**Lectura del cuadro:**
- **Impuestos lidera** con 85% — refinamiento por consultores fiscales más avanzado (activo: #93 resuelto con la consultoría), catálogos F1 entregados.
- **OXP y Contabilidad (82%)** tienen refinamiento del equipo de desarrollo activo (últimos: partida en disputa #90, retenciones asumidas #94 y clasificación semántica del contrato de traducción #104, registro de productos financieros #106 y medio de pago canónico #96); OXP está en Fase 2 (refinamiento continuo) con el modelo maduro (v4.13) y la integración con Contabilidad y Estructura Organizacional cerrada.
- **Estructura Organizacional (81%)** cerró el lote #85-#89 (varios grupos de primer nivel, `TipoUnidad` agregado propio, retiro del descarte automático, código de texto libre, numeración continua) sobre los replanteamientos #31/#45/#72; resta la validación final con el equipo de desarrollo.
- **Terceros (75%)** absorbió el replanteamiento #31 (bodega consolidadora) además de sus auditorías; resta la validación final con el equipo de desarrollo.
- **Datos de Referencia (78%)** completó alcance v2.0 y publicó los catálogos de dirección (#77); **Nuggets (47%)** tiene gobernanza y catálogo, con `DireccionFisica` v0.3 ya consultada por el equipo de desarrollo (#100) y las demás especificaciones en borrador.

### Detalle de los parciales

**Impuestos — 85%**
- ✅ Alcance v1.5, Modelo v2.0.8, Auditoría aplicada (2 rondas). Catálogo tributario CO v1.4.
- 🟡 Refinamiento en progreso (~85%) — catálogos fiscales F1 entregados (983 entradas CO/DO/PA); #39 (perfil sin registro centralizado), #93 (ICA solo ingreso), #110 (IVA_IMPORTACION_SERVICIOS, resolución con la consultoría fiscal), #111 (matriz de tratamientos alineada a la implementación), #108 (sobretasa bomberil `anticipado`, validado con las dos consultoras), #109 (RITBMS de Panamá como `porcentajeDePadre` — hereda el ciclo de vida del ITBMS) y #117/#118 (autoliquidado autónomo de naturaleza `provision` + clasificaciones de servicios) aplicados; resta el refinamiento por consultores sobre las secciones "Revisión pendiente" (abierto: #97 cuantía mínima como política).
- ⬜ Listo F1 — depende del cierre del refinamiento.
- **Cálculo:** 20 + 25 + 15 + (30 × 0.85) + 0 = 85.5 ≈ **85%**.

**OXP — 82%**
- ✅ Alcance v1.17, Modelo v4.8, Auditoría: 3 rondas aplicadas. Modelo maduro: integración con Contabilidad y con Estructura Organizacional **cerradas**; ubicaciones hacia Impuestos resueltas (`lugarEjecucion`).
- 🟡 Refinamiento en progreso (~75%, Fase 2 continuo) — #18/#25/#26/#28/#30/#38/#48/#51/#57/#72/#90/#94 aplicados (abierto: #96, canonización del medio de pago).
- ⬜ Listo F1 — depende del cierre del refinamiento.
- **Cálculo:** 20 + 25 + 15 + (30 × 0.75) + 0 = 82.5 ≈ **82%**.

**Contabilidad — 82%**
- ✅ Alcance v1.11, Modelo v1.12, Auditoría aplicada. Catálogo de plantillas v1.10 (6 plantillas), anexo de ejemplos v1.5.
- 🟡 Refinamiento en progreso (~75%) — #7/#8/#9 (grupo PUC, narración, herencia del rol), #17 (unidad de la contrapartida), #18 (rol CRUCE_OBLIGACION), #20 (nota_credito_gasto), #28 (terceroPrincipal), #47 (copia local de datos maestros), #90 (partida en disputa), #94 (retenciones asumidas) y #104 (clasificación semántica, contrapartida como línea y espejo) aplicados (abierto: #98, IVA descontable vs mayor valor).
- ⬜ Listo F1 — depende del cierre del refinamiento.
- **Cálculo:** 20 + 25 + 15 + (30 × 0.75) + 0 = 82.5 ≈ **82%**.

**Terceros — 75%**
- ✅ Alcance v2.0, Modelo v2.0.2, Auditoría aplicada (2 rondas: 28 + 6 hallazgos).
- 🟡 Refinamiento en progreso (~50%) — reescritura completa a bodega consolidadora (#31/#33) absorbida; modelo cerrado para desarrollo; resta la validación final con el equipo de desarrollo.
- ⬜ Listo F1 — depende del cierre del refinamiento.
- **Cálculo:** 20 + 25 + 15 + (30 × 0.50) + 0 = 75 ≈ **75%**.

**Estructura Organizacional — 81%**
- ✅ Alcance v1.12, Modelo v2.5 (anexo de decisiones v1.6), Auditoría completa (101 hallazgos).
- 🟡 Refinamiento en progreso (~70%) — replanteamientos #45/#72 (copia local + diferir, `[D13]`) y el lote #85-#89 (varios grupos de primer nivel, `TipoUnidad` agregado propio, retiro del descarte automático, código de texto libre, numeración continua sin saltos) aplicados de punta a punta.
- ⬜ Listo F1 — depende del cierre del refinamiento.
- **Cálculo:** 20 + 25 + 15 + (30 × 0.70) + 0 = 81 ≈ **81%**.

### Camino crítico restante

Para que el paquete F1 transversal del ERP llegue al 100% hacen falta tres frentes:

1. **Ronda de validación final con el equipo de desarrollo** y cierre formal de "Listo F1" para todos los sub-dominios — es el factor común que falta. Es el frente con mayor retorno por tiempo invertido.
2. **Cerrar el refinamiento continuo de OXP** (Fase 2) y la revisión por consultores fiscales de los catálogos de Impuestos.
3. **Transversales del ERP**: EventCatalog (Fase 3), dependencias de infraestructura, diseño UX por capas.

Una vez completados, los sub-dominios futuros (CXC, Tesorería, Emisión Electrónica, Recepción Electrónica) heredarán el patrón ya validado en F1.
