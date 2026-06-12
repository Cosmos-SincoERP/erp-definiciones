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
│   Datos de Referencia     Direcciones     Terceros          │
│   (catálogos base)        (servicio)      (identidad)       │
│                                                             │
│   Estructura Organizacional                                 │
│   (unidades)                                                │
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
| 5 | 53 | 3 | 18 | 28 (D1-D28) | 3 | 4 | 6 |

**Componentes:**

| Agregado | Descripción |
|----------|-------------|
| OxpComercio | Obligación individual por compra (tarjeta, crédito, contado). Ciclo: radicación → confirmación → causación → pago. |
| OxpExtracto | Obligación consolidada por extracto bancario. Incluye conciliación automática contra OxpComercio. |
| Anticipo | Fondo adelantado al proveedor. Dos dimensiones: saldo de pago + saldo de regularización. |
| Devolucion | Nota crédito o devolución. 3 tipos: comercio, extracto, anticipo. Aplicación automática de crédito. |
| CatalogoGastoDirecto | Configuración de tipos de gasto que OXP puede crear directamente (sin módulo de gestión detrás). |

**Capacidades F1:** Radicación multi-canal, clasificación inteligente del origen, integración con Impuestos (cálculo tributario), conciliación automática de extractos, ciclo de anticipos, devoluciones con aplicación de crédito, causación hacia Contabilidad, monitoreo de pagos, alertas.

**Capacidades F2:** Caja menor, viáticos/gastos de viaje, obligaciones recurrentes.

**3 domain services:** ServicioDeConciliacion (vinculación extracto ↔ comercio), ServicioDeRegularizacion (cruce anticipos ↔ comercio), ServicioDeAplicacionDevolucion (3 ramas por tipo de devolución).

**Integración con Contabilidad (cerrada):** causación del Anticipo (D25), generalización terminológica hacia "sistema contable" (D26), mapeo `tipoTransaccion` evento → plantilla de asiento (D27), canonización de `tipoComponente` con código fiscal específico (`iva`/`inc`/`retefuente`/`reteiva`/`reteica`) 1:1 con el catálogo de Contabilidad, y manejo de rechazos del sistema contable vía outbox del consumidor (D28).

**Estado:** Alcance v1.7, modelo v3.4, 3 rondas de auditoría. Integración OXP ↔ Contabilidad **cerrada**. Pendiente: refinamientos de conceptos (catálogo de gasto), soportes documentales y esquema de ubicaciones hacia Impuestos.

> Detalle: [`dominio/obligaciones-por-pagar/modelo-dominio.md`](dominio/obligaciones-por-pagar/modelo-dominio.md)

---

### 2.2 Impuestos

**Propósito:** Centralizar la configuración fiscal, el cálculo de tributos y el registro de hechos fiscales confirmados. Es la fuente de verdad tributaria del ERP.

| Agregados | Eventos | Domain Services | Invariantes | Decisiones | Premisas | Pendientes | Sugerencias |
|:---------:|:-------:|:---------------:|:-----------:|:----------:|:--------:|:----------:|:-----------:|
| 12 | 57 | 2 + flujo orquestado + read model | 28 | 15 | 6 | 12 | 3 |

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

**Catálogos fiscales precargados (F1):** 27 pares de archivos (`.md` + `.json`), 962 entradas que cubren los 3 países F1 (CO: 493, DO: 260, PA: 209). Son **parte del producto** (`origen: estándar`) — pendiente refinamiento por consultores fiscales (cada `.md` lleva sección "Revisión pendiente").

**Estado:** Alcance v1.4, modelo v2.0.4 (junio 2026). Modelo completo + catálogos F1 entregados — refinamiento por consultores fiscales en curso (es el sub-dominio más avanzado en el hito de refinamiento).

> Detalle: [`dominio/impuestos/modelo-dominio.md`](dominio/impuestos/modelo-dominio.md), [`dominio/impuestos/datos-precargados/`](dominio/impuestos/datos-precargados/)

---

### 2.3 Contabilidad

**Propósito:** Traducir los hechos económicos de todos los sub-dominios a asientos contables. Dos niveles: N1 (motor de traducción) y N2 (sistema contable propio).

| Agregados | Eventos | Domain Services | Invariantes | Decisiones | Premisas | Pendientes | Sugerencias | Permisos |
|:---------:|:-------:|:---------------:|:-----------:|:----------:|:--------:|:----------:|:-----------:|:--------:|
| 13 | 59 | 3 | 32 (22 L + 10 E) | 14 (D1-D14) | 7 | 3 | 7 | 17 |

**Componentes:**

| Agregado | Nivel | Descripción |
|----------|-------|-------------|
| BorradorContable | N1 | Resultado de traducir un hecho económico. Estados: PENDIENTE → RESUELTO → ENTREGADO. |
| Aprendizaje | N1 | Registra resoluciones del contador para inferencia futura. |
| PlanDeCuentas | N1 | Catálogo jerárquico de cuentas (maestras y auxiliares). Atributo `marcoContable` como referencia inmutable al código del marco que rige el PUC. |
| MarcoContable | N1 (configuración) | Define el marco normativo bajo el cual se construye un PUC (ej: NIIF, US-GAAP, marco local). Habilita coexistencia controlada y arquitectura PUC único + libros paralelos. |
| ReglaDeDerivacion | N1 | Mapeo manual: dimensiones de transacción → cuenta auxiliar (Nivel A de resolución). |
| PlantillaDeAsiento | N1 | Estructura de débitos/créditos por tipo de transacción. 42 plantillas preconfiguradas. |
| EntregaContable | N1 | Entrega individual a sistema contable destino. Resultado: aceptado/rechazado. |
| SistemaContableDestino | N1 | Configuración del destino (SincoA&F en F1, adaptable a Siigo/Alegra). |
| AsientoContable | N2 | Registro inmutable del hecho contable. |
| PeriodoContable | N2 | Apertura y cierre de períodos. |
| LibroContable | N2 | Tipo como texto libre con predeterminados sugeridos: **Principal** y **Fiscal**. Libros adicionales bajo demanda (Gerencial, etc.). |
| NumeracionContable | N2 | Secuencias por tipo de comprobante. |
| EquivalenciaPuc | N2 | Mapeo cuenta a cuenta entre libros. Uso excepcional bajo arquitectura predeterminada moderna (D11). Se congela al registrar. |

**Arquitectura predeterminada moderna (D11):** un único PUC NIIF + libros paralelos (Principal y Fiscal por defecto) sobre el mismo PUC. Las diferencias contables se modelan como asientos específicos del libro Fiscal, no como PUCs paralelos. `EquivalenciaPuc` queda como excepción para clientes que decidan operar con PUCs distintos.

**Cadena de resolución de cuentas (3 niveles):**
- **Nivel A:** Reglas manuales (ReglaDeDerivacion)
- **Nivel B:** Inferencia inteligente (IA, RAG, similitud semántica por empresa)
- **Nivel C:** Aprendizaje del contador (resoluciones previas)

**Tres domain services:** `ServicioDeTraduccion` (N1, motor sin estado con validación contractual y motivos estructurados de rechazo), `ServicioDeAnulacion` (N2), `ServicioDeContabilizacion` (N2).

**Consola de contabilización:** Vista consolidada del estado de cada hecho económico (PENDIENTE / RESUELTO / ENTREGADO / RECHAZADO). Permite resolver cuentas, reintentar entregas, descartar borradores manuales. Los rechazos contractuales del motor (referencia de origen duplicada, tipo de transacción sin plantilla, línea sin rol en plantilla) no aparecen en la consola — se canalizan por mensajería (DLQ + logs + métricas) y la durabilidad la garantiza el outbox del consumidor emisor.

**17 permisos atómicos** definidos con convención `accion_recurso`.

**Refinamientos aplicados (integración con OXP):** grupo del PUC esperado por componente para acotar la inferencia Nivel B (D12), narración del borrador — descripción general + descripción de concepto por partida (D13), herencia del `rol` de la partida desde la plantilla y propagación a la entrega (D14).

**Estado:** Alcance v1.6, modelo v1.5 (junio 2026). Listo para desarrollo F1 — refinamiento con el equipo de desarrollo en curso.

> Detalle: [`dominio/contabilidad/modelo-dominio.md`](dominio/contabilidad/modelo-dominio.md), [`dominio/contabilidad/anexo-marco-contable-y-arquitectura-puc.md`](dominio/contabilidad/anexo-marco-contable-y-arquitectura-puc.md)

---

### 2.4 Terceros

**Propósito:** Registro centralizado de personas y empresas con las que la organización tiene relación. Fuente de verdad de identidad.

| Agregados | Entidades internas | Value Objects | Eventos | Invariantes | Decisiones | Premisas | Pendientes | SIs | Permisos |
|:---------:|:------------------:|:-------------:|:-------:|:-----------:|:----------:|:--------:|:----------:|:---:|:--------:|
| 1 | 1 | 4 | 18 | 11 | 13 | 6 | 3 | 11 | 19 |

**Componentes:**

| Elemento | Descripción |
|----------|-------------|
| Tercero (agregado raíz) | Identidad base: tipo documento + número + país, razón social, roles, referencias a direcciones, estado del ciclo de vida. FSM de 4 estados. |
| Contacto (entidad interna) | Personas de contacto del tercero con ciclo de vida propio. Exactamente un principal con correo y teléfono. |
| Value Objects | Identificacion, ReferenciaDireccion, CorreoElectronico, Telefono. |

**Gestiona:** Identidad (tipo documento, número, razón social, tipo persona), roles universales (proveedor, cliente, empleado, entidad financiera), contactos con medios de comunicación, referencias a direcciones, estado del ciclo de vida.

**No gestiona (lo enriquece cada consumidor):** Contenido de direcciones (Direcciones), perfil tributario (Impuestos), condiciones comerciales (OXP/CXC), cuentas bancarias (Tesorería), datos laborales (RRHH / sistema externo de nómina del cliente).

**Ciclo de vida — FSM de 4 estados (D13):**
- **EnRegistro** → identidad registrada, pendiente confirmación asíncrona de dirección fiscal por el servicio de Direcciones. No operable.
- **Activo** → tras `TerceroActivado`. Operable. Dominios consumidores abren sus registros de rol.
- **Inactivo** → tras `TerceroInactivado`. Reactivable.
- **Abortado** → tras `TerceroRegistroAbortado` si Direcciones falla permanentemente. Terminal.

**Patrón de registro en dos fases (D13):** resuelve la tensión entre dirección fiscal obligatoria para tercero operable (R25/I6), Direcciones como servicio único, y arquitectura event-driven asíncrona.

**Prevención de duplicados:**
- R01 — Unicidad exacta `(tipoDocumento, numero, pais)`.
- R01b — Detección de posibles duplicados por número + razón social canónica (rechazo automático en consumidores; override humano vía `RegistrarTerceroForzado`).

**Eventos principales:** `TerceroRegistrado`, `TerceroActivado`, `TerceroRegistroAbortado`, `TerceroInactivado`, `TerceroReactivado`, `TerceroIdentificacionActualizada`, `TerceroRazonSocialActualizada`, `TerceroTipoPersonaActualizado`, `TerceroRolAsignado`, `TerceroRolRemovido`, `TerceroDireccionReferenciada`, `TerceroDireccionDesreferenciada`, `TerceroDireccionPreferidaDesignada`, `ContactoRegistrado`, `ContactoActualizado`, `ContactoInactivado`, `ContactoReactivado`, `ContactoPrincipalDesignado`.

**Orquestación del registro:** vive fuera del sub-dominio en una capa BFF / API Composition. Terceros no orquesta a otros dominios.

**19 permisos atómicos** todos N1 (Terceros es dominio de una sola capacidad).

**Estado:** Alcance v1.0, modelo v1.0. Auditoría de 10 skills + rondas de refinamiento aplicadas. Listo para desarrollo F1.

> ⚠️ **Superado (junio 2026):** Terceros pasó a v2.0 — **bodega consolidadora** (replanteamiento #31, issue #33): la captura vive en los dominios con sus roles; la bodega consolida por clave natural, concilia duplicados y divergencias, y administra la señal global. Esta sección describe la v1.0 y se actualizará con el consolidado.
>
> Detalle: [`dominio/terceros/definicion-alcance.md`](dominio/terceros/definicion-alcance.md), [`dominio/terceros/modelo-dominio.md`](dominio/terceros/modelo-dominio.md)

---

### 2.5 Estructura Organizacional

**Propósito:** Estructura centralizada de las unidades de la empresa a las que se imputan transacciones para control de gestión (centros de costo, proyectos, sucursales, departamentos). Fuente de verdad de la pregunta "¿a qué unidad de la organización pertenece esta transacción?".

| Agregados raíz | Entidad interna | Value Objects | Eventos | Invariantes | Decisiones | Premisas | Pendientes | Sugerencias | Permisos |
|:--------------:|:---------------:|:-------------:|:-------:|:-----------:|:----------:|:--------:|:----------:|:-----------:|:--------:|
| 2 | 1 | 5 | 18 | 16 | 14 (+4 heredadas) | 5 | 3 | 10 | 23 |

**Dos niveles (dos agregados raíz):**
- **GrupoOrganizacional:** Agrupador para presentación en reportes. No recibe transacciones. FSM de 2 estados.
- **UnidadOrganizacional:** Nivel de detalle donde se imputan transacciones. FSM de 5 estados.

**FSM de la unidad (5 estados, 7 transiciones):** `Borrador` (creada desde consumidor, pendiente de aprobación) o `Activa` (según flujo de creación) → opera → `Suspendida` (pausada) → reactivable → `Inactiva` (reabrible) → reabrir, o `Descartada` (único terminal estricto, antes de operar). `GrupoOrganizacional` admite `GrupoModificado` en estado `Inactivo`; la unidad no, porque participa en historial transaccional.

**Gestiona:** Creación, jerarquía versionada por fecha efectiva, tipos de unidad heredados del grupo raíz, ciclo de vida, reestructuración (fusión, división, traslado como eventos de primera clase con respaldo IFRS 8).

**Cuatro decisiones arquitectónicas (anexo dedicado):**
1. **Codificación plana + jerarquía versionada aparte** — código alfanumérico plano (sin embeber jerarquía en el código). Rompe con el patrón posicional de SincoA&F que tenía techo combinatorio y bloqueaba reestructuraciones.
2. **Ciclo de vida con 4 estados** (Borrador, Activa, Suspendida, Inactiva) en lugar de la dupla activo/inactivo.
3. **Fusión, División y Traslado modelados como eventos de dominio de primera clase**, no como mutaciones silenciosas.
4. **Modelo multi-dimensional desde el diseño**, aunque en F1 solo se exponga una dimensión.

**Patrón de creación desde consumidores (BFF + Borrador):** cuando un usuario operativo de OXP o Contabilidad necesita una unidad que no existe, la UI del consumidor invoca al BFF que crea la unidad en estado `Borrador` en Estructura Organizacional. El administrador la aprueba (transición a `Activa`) en una segunda fase. Evita acoplar el modelo del consumidor con el de Estructura Organizacional manteniendo experiencia unificada para el usuario.

**Patrón EDA:** publica eventos `UnidadCreada`, `UnidadActualizada`, `UnidadSuspendida`, `UnidadReactivada`, `UnidadInactivada`, `UnidadFusionada`, `UnidadDividida`, `UnidadTrasladada`. Reestructuración dispara reacciones en consumidores (Contabilidad reclasifica, etc.).

**Estado:** Alcance v1.2, modelo v1.4 (junio 2026). Auditoría completa (101 hallazgos) + rondas de refinamiento aplicadas. Listo para desarrollo F1.

> Detalle: [`dominio/estructura-organizacional/definicion-alcance.md`](dominio/estructura-organizacional/definicion-alcance.md), [`dominio/estructura-organizacional/anexo-decisiones-arquitectonicas.md`](dominio/estructura-organizacional/anexo-decisiones-arquitectonicas.md), [`dominio/estructura-organizacional/anexo-orquestacion-creacion.md`](dominio/estructura-organizacional/anexo-orquestacion-creacion.md)

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

**Consumidores principales:** Terceros (país del documento, indicativo telefónico), Direcciones (país + divisiones), Impuestos (jurisdicción fiscal, divisiones municipales para ICA/RICA), OXP (monedas, tasas de cambio), Contabilidad (monedas).

**Estrategia Seed + Sync + Extend:** carga inicial desde JSON precargados, sincronización periódica desde fuentes oficiales (Banco de la República CO, Banco Central RD), extensible por administrador.

**Estado:** Alcance v1.0, especificación de servicio v1.0. Listo para desarrollo.

> Detalle: [`compartido/datos-referencia/definicion-alcance.md`](compartido/datos-referencia/definicion-alcance.md)
>
> ⚠️ **Superado parcialmente (junio 2026):** el alcance pasó a v2.0 con el replanteamiento de Nuggets — producción de catálogos + tasas de cambio por evento. Esta sección describe la v1.0 y se actualizará con el consolidado.

---

### 2.7 Direcciones (servicio compartido)

> ⚠️ **Superado (junio 2026):** el servicio fue eliminado en el replanteamiento arquitectónico — lo reemplaza el Nugget [`DireccionFisica`](compartido/nuggets/direccion-fisica/especificacion.md) (validación local empaquetada, sin servicio en ejecución). Esta sección describe la v1.0 y se actualizará con el consolidado.

**Propósito:** Servicio compartido que gestiona estructura, configuración y validación de direcciones, con reglas adaptables por país. Centraliza la complejidad de validación de direcciones para que los módulos del ERP no la dupliquen.

**Por qué es un servicio independiente:** cada país tiene reglas diferentes sobre estructura, campos obligatorios y catálogos aplicables. Colombia exige tipos de vía codificados (Calle, Carrera, Diagonal — exigidos por DIAN para facturación electrónica); República Dominicana permite texto libre. Múltiples módulos (Terceros, Emisión Electrónica, Estructura Organizacional) necesitan direcciones estructuradas.

**Catálogos:**

| Catálogo | Descripción |
|----------|-------------|
| Tipos de dirección | Fiscal, comercial, correspondencia, entrega, sucursal. Extensible. |
| Formatos de dirección por país | Qué componentes son obligatorios/opcionales, orden de presentación, validaciones. |
| Tipos de vía por país | Nomenclatura oficial — en CO: Calle (CL), Carrera (CR), Diagonal (DG), Transversal (TV), Avenida (AV). |
| Tipos de complemento | Apartamento, torre, piso, oficina, local, bodega, bloque, interior. Global. |
| Códigos postales por país | Catálogos oficiales (DIAN/4-72 para CO, SAT/SEPOMEX para MX). |

**Consumidores:** Terceros (dirección fiscal obligatoria + comerciales + correspondencia), Estructura Organizacional (sucursales), Emisión Electrónica (dirección fiscal en facturas), Impuestos (jurisdicción), todos los módulos que registren direcciones.

**Relación con Terceros (D13):** el servicio de Direcciones emite la confirmación asíncrona de creación de la dirección fiscal, que dispara la transición de Tercero en `EnRegistro` a `Activo`. Ver anexo de orquestación en Terceros.

**Estado:** Eliminado (junio 2026). Documentos y catálogos conservados en el historial del repositorio.

> Detalle: [`compartido/nuggets/direccion-fisica/especificacion.md`](compartido/nuggets/direccion-fisica/especificacion.md)

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

### Registro de Tercero (patrón de dos fases — D13)

```
  Capa BFF / API Composition
       │
       │ (1) RegistrarTercero (identidad + roles + contacto)
       ▼
  ┌──────────┐                         ┌────────────┐
  │ Terceros │                         │ Direcciones│
  │          │   TerceroRegistrado     │            │
  │ EnRegis- │─── evento ───────────►  │   crea     │
  │ tro      │                         │  dirección │
  │          │                         │   fiscal   │
  │          │                         └─────┬──────┘
  │          │                               │
  │          │◄── DireccionFiscalCreada ─────┘
  │          │    (evento de confirmación)
  │          │
  │ Activo   │─── TerceroActivado ───► OXP, CXC, RRHH, ...
  └──────────┘    (abren sus registros de rol)
```

### Tabla de integraciones

> **Nota:** Este mapa es parcial. Las integraciones formalizadas corresponden a los sub-dominios con modelo completo (OXP, Impuestos, Contabilidad, Terceros, Datos de Referencia, Direcciones). Las marcadas como "Futuro" son integraciones esperadas según referencias en los modelos existentes, pero sus contratos aún no están definidos.
>
> **Sub-dominios sin integraciones definidas aún:** Tesorería, Activos Fijos, Arrendamientos. Sus integraciones se formalizarán cuando se construyan sus definiciones de alcance y modelo de dominio.

| Origen | Destino | Tipo | Contrato | Estado |
|--------|---------|------|----------|--------|
| OXP | Impuestos | Síncrono + Asíncrono | D22/D9: solicitud de cálculo + confirmación | Formalizado |
| OXP | Contabilidad | Evento | LineaTraduccion: tipoComponente, clasificacion, valor, tercero, undOrg | Formalizado |
| Contabilidad | OXP | Evento | EntregaAceptada: consecutivo del asiento en destino | Formalizado |
| Terceros | OXP, Impuestos, Contabilidad, CXC, RRHH | Eventos EDA | `TerceroActivado`, `TerceroInactivado`, `TerceroReactivado`, `TerceroRolAsignado`, `TerceroRolRemovido`, actualizaciones de identidad. Consumidores se suscriben a `TerceroActivado` para apertura de registros de rol (D13) | Formalizado v1.0 |
| Direcciones | Terceros | Evento | Confirmación asíncrona de creación de dirección fiscal → dispara `TerceroActivado` (D13) | Formalizado v1.0 |
| Estructura Org | OXP, Contabilidad | Eventos EDA | UnidadCreada/Activada/Suspendida/Reactivada/Inactivada + Fusionada/Dividida/Trasladada. Patrón BFF + estado `Borrador` para creación desde consumidores. | Formalizado — modelo v1.4 |
| Datos de Referencia | Todos | Lectura | Catálogos estáticos (países, divisiones, monedas, tipos doc., tasas de cambio) | Formalizado v1.0 |
| Direcciones | Terceros, Estructura Org, Emisión Electrónica | Lectura + eventos | Validación de direcciones por país + eventos de creación/modificación | Formalizado v1.0 |
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

## 4. Cadena de bloqueo y prioridad de construcción

### Dependencias

```
Datos de Referencia ──► Direcciones ──► Terceros ──► Estructura Org
       │                    │              │                │
       │ lectura             │ eventos       │ eventos          │ eventos
       ▼                    ▼              ▼                ▼
┌──────────────────────────────────────────────────────────────┐
│           OXP ◄──► Impuestos ──► Contabilidad                │
└──────────────────────────────────────────────────────────────┘
```

| Sub-dominio | Bloquea a | Bloqueado por | Estado |
|-------------|-----------|---------------|--------|
| Datos de Referencia | Todos | Ninguno | **v1.0 — listo para desarrollo** |
| Direcciones | Terceros, Estructura Org, Emisión Electrónica | Datos de Referencia | **v1.0 — listo para desarrollo** |
| Terceros | OXP, Impuestos, Contabilidad, CXC, RRHH | Datos de Referencia, Direcciones | **v1.0 — listo para desarrollo** |
| Estructura Org | OXP, Contabilidad | Datos de Referencia | **v1.4 — listo para desarrollo F1** |
| Impuestos | OXP (cálculo tributario) | Terceros, Datos de Referencia | **v2.0.4 — modelo completo + catálogos F1** |
| Contabilidad | OXP (confirmación de asiento) | Terceros, Estructura Org, Datos de Referencia | **v1.5 — listo para desarrollo F1** |
| OXP | — | Impuestos, Contabilidad, Terceros, Estructura Org, Datos de Referencia | v3.4 — integración Contabilidad cerrada; refinamientos OXP pendientes |

### Estado actual de construcción

- ✅ **Datos de Referencia** — v1.0 listo.
- ✅ **Direcciones** — v1.0 listo.
- ✅ **Terceros** — v1.0 listo (auditoría + rondas de refinamiento).
- ✅ **Impuestos** — modelo v2.0.4 completo + catálogos F1 (LatAm CO/DO/PA, apertura US/CA F2).
- ✅ **Estructura Organizacional** — modelo v1.4 listo F1 (2 agregados raíz, reestructuración como eventos de primera clase).
- ✅ **Contabilidad** — v1.5 listo F1 (MarcoContable + arquitectura PUC + grupo PUC esperado + narración del borrador).
- 🔄 **OXP** — v3.4. Integración con Contabilidad **cerrada**; quedan refinamientos del modelo (catálogo de conceptos, soportes documentales, esquema de ubicaciones).

### Siguiente paso

La cadena de bloqueo está **desbloqueada**: los 3 sub-dominios base (Terceros, Estructura Org, Datos de Referencia/Direcciones) y los 3 transaccionales (OXP, Impuestos, Contabilidad) tienen modelo completo. El frente restante son los **refinamientos de OXP** (conceptos, soportes, ubicaciones), el **refinamiento con el equipo de desarrollo** del resto de sub-dominios y los **transversales del ERP** (EventCatalog, infraestructura, UX). Ver el plan de trabajo activo para el detalle priorizado.

---

## 5. Fases por sub-dominio

| Sub-dominio | F1 | F2 |
|-------------|----|----|
| **OXP** | Radicación, clasificación inteligente, conciliación, anticipos, devoluciones, causación, integración Impuestos y Contabilidad | Caja menor, viáticos, obligaciones recurrentes |
| **Impuestos** | Configuración fiscal multi-país LatAm (CO/DO/PA, 11+5+4 tributos preconfigurados), motor de cálculo, perfiles tributarios con actividad económica por jurisdicción, carga asistida, registro tributario, gestión de jurisdicciones fiscales (incluido Puerto Libre San Andrés), regímenes especiales empresariales (zonas francas, monopolios departamentales CO, ZEEs panameñas) | Reportes de información (exógena, DGII), certificados tributarios, homologación fiscal, apertura multi-país a US/CA (distritos fiscales especiales, soberanías tributarias, resolución por dirección/geocoding) |
| **Contabilidad** | N1: Motor de traducción + entrega a SincoA&F. Cadena de resolución 3 niveles. Consola de contabilización. Aprendizaje. **MarcoContable** + arquitectura PUC único + libros paralelos (Principal, Fiscal). Validación contractual del motor (rechazos pre-borrador). | N2: Sistema contable propio (asientos, períodos, libros, numeración). Libros adicionales bajo demanda. Adaptadores adicionales (Siigo, Alegra). |
| **Terceros** | **Núcleo del BC:** registro de identidad, gestión de roles, contactos, historial. **Habilitadores con dependencias:** activación del tercero (requiere Direcciones), notificación a consumidores, solicitud desde consumidores (BFF), vista consolidada de completitud, aprovechamiento de documentos de soporte, importación masiva, registro automático desde SincoRE. | Resolución de duplicados tardíos (fusión), recepción electrónica adicional |
| **Estructura Org** | Grupos, unidades con codificación plana + jerarquía versionada, FSM de 5 estados de la unidad, creación desde consumidores con patrón BFF + Borrador, fusión/división/traslado como eventos de primera clase, eventos EDA | Multi-dimensionalidad expuesta (más allá de la dimensión inicial) |
| **Datos de Referencia** | 5 catálogos base (países, divisiones, monedas, tipos de documento, tasas de cambio), estrategia Seed + Sync + Extend | Extensiones por país, validación avanzada |
| **Direcciones** | Catálogos de tipos, formatos por país, códigos postales, validación estructurada | Validación externa (Google Address, Loqate, SmartyStreets) |

---

## 6. Sub-dominios futuros

| Sub-dominio | Descripción | Dependencias conocidas |
|-------------|-------------|------------------------|
| **CXC (Cuentas por Cobrar)** | Gestión de obligaciones de ingreso. Mismo patrón que OXP pero dirección fiscal invertida (empresa es emisora). | Impuestos (mismo contrato D9), Contabilidad (LineaTraduccion), Terceros (clientes) |
| **Tesorería** | Gestión de pagos, cobros, transferencias, consignaciones, conciliación bancaria. | Contabilidad (LineaTraduccion), Terceros (cuentas bancarias) |
| **Emisión Electrónica** | Emisión de documentos electrónicos ante autoridades fiscales. Capacidades activables por el cliente: facturas + notas crédito/débito (ingreso), documentos soporte de compra + notas (gasto a no obligados a facturar), nómina electrónica (México y otros), contabilidad electrónica (México: catálogo de cuentas, balanza, pólizas al SAT). | Impuestos, Terceros, Direcciones. Cada capacidad se conecta con su fuente: facturas → CXC, documentos soporte → OXP, nómina → sistema externo de nómina del cliente, contabilidad → Contabilidad |
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
| **Direcciones** | Gestión estructurada de direcciones con validación por país. Soporte para facturación electrónica DIAN (tipos de vía codificados) y equivalentes por país. |
| **Terceros** | Registro centralizado de proveedores, clientes, empleados, entidades financieras. Un solo lugar para gestionar todas las relaciones comerciales. Prevención de duplicados en origen. |
| **Estructura Organizacional** | Centros de costo, proyectos, sucursales, departamentos. Control de gestión y distribución de gastos/ingresos por unidad de negocio. |

### 7.2 Productos

#### Cosmos Contabilidad

Para empresas que necesitan un sistema contable moderno con traducción automática de hechos económicos.

| Sub-dominio | Capacidades incluidas |
|-------------|----------------------|
| Contabilidad (N1) | Motor de traducción automática con validación contractual, cadena de resolución de cuentas en 3 niveles (reglas → IA → aprendizaje), consola de contabilización, entrega a sistema destino, trazabilidad bidireccional. `MarcoContable` como agregado de configuración. |
| Contabilidad (N2) | Sistema contable propio: asientos inmutables, períodos contables, libros paralelos (Principal y Fiscal predeterminados sobre PUC NIIF único; libros adicionales bajo demanda), numeración configurable, auxiliares y saldos contables. |
| Infraestructura base | Datos de Referencia + Direcciones + Terceros + Estructura Organizacional. |

**Para quién:** Empresa que quiere reemplazar su sistema contable actual o que no tiene uno y necesita arrancar de cero. Puede recibir hechos económicos de cualquier origen (manual o desde otros módulos Cosmos).

---

#### Cosmos Gastos

Para empresas que necesitan gestionar el ciclo completo de sus obligaciones de egreso: desde la factura hasta el asiento contable.

| Sub-dominio | Capacidades incluidas |
|-------------|----------------------|
| OXP | Radicación multi-canal (XML, PDF, manual), clasificación inteligente del origen, conciliación automática de extractos bancarios, ciclo de anticipos, devoluciones con aplicación de crédito, monitoreo de pagos, alertas. |
| Impuestos (cálculo) | Configuración fiscal preconfigurada (CO, DO, PA), motor de cálculo automático de tributos, perfiles tributarios por entidad, carga asistida desde fuentes oficiales (DIAN, DGII). |
| Contabilidad (N1) | Motor de traducción automática de cada obligación causada a borrador contable. Entrega al sistema contable del cliente (SincoA&F, Siigo, o Cosmos Contabilidad N2). |
| Infraestructura base | Datos de Referencia + Direcciones + Terceros + Estructura Organizacional. |

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
| Infraestructura base | Datos de Referencia + Direcciones + Terceros + Estructura Organizacional. |

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

Incluye infraestructura base (Datos de Referencia + Direcciones + Terceros).

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
| **Direcciones estructuradas** | ● | ● | ● | ● | ● | ● | ● | ● |
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
| **Cadena de resolución (reglas + IA)** | ● | ● | — | — | — | ● | ● | ● |
| **Consola de contabilización** | ● | ● | — | — | — | ● | ● | ● |
| **Asientos contables (N2)** | ● | — | — | — | — | — | — | ● |
| **Multi-libro (Principal, NIIF)** | ● | — | — | — | — | — | — | ● |
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
> **Producto:** Cosmos Contabilidad. N1 traduce hechos económicos con cadena de resolución inteligente (reglas → IA → aprendizaje). N2 es el sistema contable con multi-libro y períodos.

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
| [`dominio/estructura-organizacional/definicion-alcance.md`](dominio/estructura-organizacional/definicion-alcance.md) | Alcance Estructura Organizacional (v1.2): glosario, actores, flujos, reglas |
| [`dominio/estructura-organizacional/anexo-definicion-contexto-inicial.md`](dominio/estructura-organizacional/anexo-definicion-contexto-inicial.md) | Definición inicial de contexto (preexistente al alcance formal) |
| [`dominio/estructura-organizacional/anexo-decisiones-arquitectonicas.md`](dominio/estructura-organizacional/anexo-decisiones-arquitectonicas.md) | Cuatro decisiones arquitectónicas: codificación plana + jerarquía versionada, FSM 4 estados, fusión/división/traslado como eventos, multi-dimensionalidad |
| [`dominio/estructura-organizacional/anexo-orquestacion-creacion.md`](dominio/estructura-organizacional/anexo-orquestacion-creacion.md) | Patrón BFF + estado `Borrador` para creación de unidades desde sub-dominios consumidores |
| [`compartido/datos-referencia/definicion-alcance.md`](compartido/datos-referencia/definicion-alcance.md) | Alcance Datos de Referencia (v2.0 — producción de catálogos + tasas de cambio) |
| [`compartido/nuggets/gobernanza-nuggets.md`](compartido/nuggets/gobernanza-nuggets.md) | Gobernanza de Nuggets: filtros de admisión, proceso con custodio, versionado |
| [`compartido/nuggets/catalogo-nuggets.md`](compartido/nuggets/catalogo-nuggets.md) | Catálogo de Nuggets (7 aceptados en borrador) |
| [`compartido/anexo-decision-i18n-l10n.md`](compartido/anexo-decision-i18n-l10n.md) | Decisión transversal de internacionalización/localización |
| [`integraciones/entre-dominios/catalogo-conceptos-por-dominio.md`](integraciones/entre-dominios/catalogo-conceptos-por-dominio.md) | Modelo federado de catálogos, contratos entre dominios |
| [`plan-trabajo-abril.md`](plan-trabajo-abril.md) | Plan de ejecución con orden de prioridad |

---

## 8. Avance por sub-dominio

> Snapshot al **3 de junio de 2026**. Refleja completitud de los artefactos de diseño que habilitan el inicio de desarrollo, no el avance de la implementación en código.

### Metodología

El porcentaje de avance combina cinco hitos. Cada hito tiene un peso fijo y se evalúa como ✅ (completo), 🟡 (parcial — se cuenta el % alcanzado del hito) o ⬜ (pendiente). El símbolo — indica que el hito no aplica para ese sub-dominio.

**Esquema para sub-dominios de negocio** (OXP, Impuestos, Contabilidad, Terceros, Estructura Organizacional):

| Hito | Peso | Criterio de cierre |
|------|:----:|--------------------|
| **Alcance** | 20% | `definicion-alcance.md` aprobado + anexos de decisiones arquitectónicas formalizadas |
| **Modelo** | 25% | `modelo-dominio.md` v1.0+ con agregados, eventos, invariantes, FSM, domain services definidos |
| **Auditoría** | 15% | 10 skills de auditoría ejecutadas + hallazgos resueltos o descartados con justificación |
| **Refinamiento** | 30% | Consultas del equipo de diseño y del equipo de desarrollo resueltas y aplicadas al modelo. Sello de validación cruzada antes de pasar a desarrollo. |
| **Listo para F1** | 10% | Decisiones cerradas, pendientes documentados sin bloqueos, integraciones contractadas con consumidores |

**Esquema para servicios de infraestructura** (Datos de Referencia, Direcciones) — sin auditoría formal de dominio porque no son bounded contexts DDD:

| Hito | Peso | Criterio de cierre |
|------|:----:|--------------------|
| **Alcance** | 30% | `definicion-alcance.md` aprobado |
| **Especificación** | 30% | `especificacion-servicio.md` aprobada (catálogos, contratos, datos precargados) |
| **Refinamiento** | 30% | Consultas del equipo de diseño y del equipo de desarrollo resueltas y aplicadas. |
| **Listo para F1** | 10% | Catálogos precargados disponibles, contratos con consumidores cerrados |

> **Nota — peso del Refinamiento (30%):** Refleja que ningún artefacto puede considerarse listo para desarrollo hasta que sea validado por el equipo de desarrollo que lo va a usar. La auditoría asegura coherencia interna del modelo; el refinamiento asegura coherencia con la realidad operativa del diseño y la viabilidad técnica. Hoy **Impuestos, Contabilidad y OXP** tienen refinamiento en progreso (consultas del equipo de desarrollo aplicadas — ej: issues #7/#8/#9/#10); los demás completaron Alcance + Modelo + Auditoría pero aún no han pasado por la ronda con el equipo de desarrollo.

### Tabla de avance

| Sub-dominio | Alcance | Modelo / Especificación | Auditoría | Refinamiento | Listo F1 | **Avance** |
|-------------|:-------:|:-----------------------:|:---------:|:------------:|:--------:|:----------:|
| Datos de Referencia | ✅ | ✅ | — | ⬜ | ⬜ | **60%** |
| Direcciones | ✅ | ✅ | — | ⬜ | ⬜ | **60%** |
| Terceros | ✅ | ✅ | ✅ | ⬜ | ⬜ | **60%** |
| Estructura Organizacional | ✅ | ✅ | ✅ | ⬜ | ⬜ | **60%** |
| OXP | ✅ | 🟡 (90%) | ✅ | 🟡 (30%) | ⬜ | **67%** |
| Contabilidad | ✅ | ✅ | ✅ | 🟡 (50%) | ⬜ | **75%** |
| Impuestos | ✅ | ✅ | ✅ | 🟡 (80%) | ⬜ | **84%** |

**Lectura del cuadro:**
- **Impuestos lidera** con 84% — refinamiento por consultores fiscales más avanzado, catálogos F1 entregados.
- **Contabilidad (75%) y OXP (67%)** ya tienen refinamiento del equipo de desarrollo en progreso (issues #7/#8/#9 en Contabilidad, #10 en OXP). OXP además debe cerrar pendientes del modelo (conceptos, soportes, ubicaciones).
- **Cuatro sub-dominios en 60%** — Datos de Referencia, Direcciones, Terceros y Estructura Organizacional completaron Alcance + Modelo/Especificación (+ Auditoría los dos últimos) pero esperan la ronda de refinamiento con el equipo de desarrollo. Es el grupo más cercano a desbloquearse.
- **Estructura Organizacional saltó de 13% a 60%** — cerró alcance (v1.2), modelo (v1.4) y auditoría (101 hallazgos) desde el snapshot anterior.

### Detalle de los parciales

**Impuestos — 84%**
- ✅ Alcance v1.4, Modelo v2.0.4, Auditoría aplicada (2 rondas).
- 🟡 Refinamiento en progreso (~80%) — catálogos fiscales F1 entregados (962 entradas CO/DO/PA); resta el refinamiento por consultores fiscales sobre las secciones "Revisión pendiente".
- ⬜ Listo F1 — depende del cierre del refinamiento.
- **Cálculo:** 20 + 25 + 15 + (30 × 0.80) + 0 = 84 ≈ **84%**.

**Contabilidad — 75%**
- ✅ Alcance v1.6, Modelo v1.5, Auditoría aplicada.
- 🟡 Refinamiento en progreso (~50%) — issues #7/#8/#9 del equipo de desarrollo aplicados (grupo PUC esperado, narración del borrador, herencia del rol). Restan más consultas del equipo de desarrollo.
- ⬜ Listo F1 — depende del cierre del refinamiento.
- **Cálculo:** 20 + 25 + 15 + (30 × 0.50) + 0 = 75 ≈ **75%**.

**OXP — 67%**
- ✅ Alcance v1.7, Auditoría: 3 rondas aplicadas.
- 🟡 Modelo v3.4 (~90%): integración OXP ↔ Contabilidad **cerrada**; quedan tres frentes del modelo — (1) catálogo de conceptos, (2) soportes documentales, (3) esquema de ubicaciones hacia Impuestos.
- 🟡 Refinamiento en progreso (~30%) — issue #10 (canonización de `tipoComponente`) aplicado.
- ⬜ Listo F1 — bloqueado por el cierre del modelo y el refinamiento.
- **Cálculo:** 20 + (25 × 0.90) + 15 + (30 × 0.30) + 0 = 66.5 ≈ **67%**.

**Estructura Organizacional — 60%**
- ✅ Alcance v1.2, Modelo v1.4, Auditoría completa (101 hallazgos).
- ⬜ Refinamiento con el equipo de desarrollo — pendiente.
- ⬜ Listo F1 — depende del refinamiento.
- **Cálculo:** 20 + 25 + 15 + 0 + 0 = **60%**.

### Camino crítico restante

Para que el paquete F1 transversal del ERP llegue al 100% hacen falta tres frentes:

1. **Cerrar los refinamientos del modelo de OXP** (catálogo de conceptos, soportes documentales, esquema de ubicaciones hacia Impuestos): es el único sub-dominio transaccional con frentes del modelo abiertos. Auditar el cierre.
2. **Ronda de refinamiento con el equipo de desarrollo** para los sub-dominios que aún no la tienen (Datos de Referencia, Direcciones, Terceros, Estructura Organizacional) y completar la de los que están en curso (Impuestos, Contabilidad, OXP). Es el frente con mayor retorno por tiempo invertido — sube cuatro sub-dominios de 60% y cierra el hito en los otros tres.
3. **Transversales del ERP**: EventCatalog (Fase 3), dependencias de infraestructura, diseño UX por capas.

Una vez completados, los sub-dominios futuros (CXC, Tesorería, Emisión Electrónica, Recepción Electrónica) heredarán el patrón ya validado en F1.
