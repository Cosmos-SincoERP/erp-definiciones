## Audit Full — Reporte de Auditoría Completa — Terceros

**Fecha:** 2026-04-20
**Modelo auditado:** `dominio/terceros/modelo-dominio.md` v1.0 (candidata)

---

### 1. Glosario y Lenguaje Ubicuo

#### Términos con Hallazgo

| Término canónico | Variantes encontradas | Secciones donde aparece | Tipo de problema |
|-----------------|----------------------|------------------------|-----------------|
| `ContactoRegistrado` (evento) | "agregar contacto" (`TerceroContactoAgregado`), "agregar medio" (`TerceroContactoMedioAgregado`) | § 3.4 `[SI10]` L430 vs § 5.5 catálogo | Sinónimo no controlado (eventos fantasma) |
| `MedioDeComunicacion` | usado en diagrama (L123), pero los VOs declarados son `CorreoElectronico` y `Telefono` | Diagrama 3.1 vs composición 3.3 | Sinónimo / término no formalizado |
| `HistorialIdentificacion` | aparece como "VO colección N" en diagrama | Diagrama 3.1 L116 vs D3 (derivado del stream, no persistido) | Variante contradictoria / término eliminado presente |
| `contextoOrigen` — valores | `DesdeConsumidor` (en enum) vs "desde un consumidor" (en prosa) | § 5.1 L574 vs § 3.2 L259 | Variante menor estable |
| `TipoUsoDireccion` | enum declarado (§8 L784) pero nunca se nombra explícitamente como tipo en § 3.3.2 | § 6 excluidos vs § 3.3.2 | Término declarado como enum pero sin nombre canónico en composición |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Alta | L430: "emitir `TerceroContactoAgregado`" … "`TerceroContactoMedioAgregado`" | En `[SI10]` se mencionan dos eventos (`TerceroContactoAgregado`, `TerceroContactoMedioAgregado`) que no existen en el catálogo § 5. El catálogo define `ContactoRegistrado` y `ContactoActualizado`. Inconsistencia terminológica que bloquea implementación. | Reemplazar en L430 `TerceroContactoAgregado` por `ContactoRegistrado` y `TerceroContactoMedioAgregado` por `ContactoActualizado`. |
| 2 | Media | L116 (diagrama): "HistorialIdentificacion (VO, colección N)" | El diagrama de la Sección 3.1 muestra `HistorialIdentificacion` como VO colección, pero `[D3]` decide explícitamente que el historial NO se persiste como dato del agregado. Contradicción visual. | Eliminar la línea `HistorialIdentificacion (VO, colección N)` del diagrama ASCII. |
| 3 | Media | L123: "MedioDeComunicacion (VO, 1..N)" | El diagrama introduce el término `MedioDeComunicacion` como VO, pero en la composición real (§ 3.3) los VOs son `CorreoElectronico` y `Telefono`. No existe ningún VO llamado `MedioDeComunicacion`. | Reemplazar en el diagrama `MedioDeComunicacion (VO, 1..N)` por `CorreoElectronico (VO, 0..N) · Telefono (VO, 0..N)`. |
| 4 | Baja | L784: "`TipoUsoDireccion`, estados Activo/Inactivo, `origen` del registro" | Se declara "`TipoUsoDireccion`" como enum inline excluido del catálogo, pero en composición (L293) ese enum se nombra solo "`tipoUso`". Inconsistencia menor de naming. | Usar el mismo término `tipoUso` en § 6 o documentar `TipoUsoDireccion` como nombre del enum en § 3.3.2. |

#### Resumen
- Alta: 1 | Media: 2 | Baja: 1 | Total: 4

---

### 2. Composición de Agregados

#### Inventario por Agregado

```
### Composición: Tercero

Entidades internas: Contacto
Value Objects: Identificacion, ReferenciaDireccion, CorreoElectronico, Telefono
VO compartidos: (ninguno entre agregados — solo existe Tercero)
Atributos raíz: terceroId, digitoVerificacion, tipoPersona, razonSocial, roles (set), estado
Atributos de Contacto: contactoId, nombre (opcional), rolContacto, correos, telefonos, esPrincipal, estado
Comportamientos calculados: contactoPrincipalActivo(), estaActivo(), tieneRol(), direccionesPorTipoUso(), direccionPreferidaPorTipoUso(), contactosActivos(), contactosPorRol(), identificacionVigente()
```

#### Inconsistencias

| Agregado | Componente | Declarado en composición | Referenciado en eventos | Tipo de inconsistencia |
|----------|-----------|-------------------------|------------------------|----------------------|
| Tercero | `HistorialIdentificacion` (VO colección) | Sí, en diagrama L116 | No referenciado por ningún evento | Declarado pero huérfano (además contradice `[D3]`) |
| Tercero | `MedioDeComunicacion` (VO 1..N) | Sí, en diagrama L123 | No; los eventos usan `CorreoElectronico` y `Telefono` | Declarado en diagrama pero ausente de § 3.3 |
| Tercero | `TerceroContactoAgregado`, `TerceroContactoMedioAgregado` (eventos referenciados en `[SI10]` L430) | No (no existen en catálogo § 5) | Referenciados en `[SI10]` | Evento referenciado en sugerencia pero ausente en catálogo |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Alta | L430: "emitir `TerceroContactoAgregado`" | `[SI10]` referencia dos eventos (`TerceroContactoAgregado`, `TerceroContactoMedioAgregado`) que no existen en el catálogo § 5. Los nombres correctos son `ContactoRegistrado` y `ContactoActualizado`. | Reemplazar los nombres en L430 por los eventos reales del catálogo. |
| 2 | Media | L116: "HistorialIdentificacion (VO, colección N)" | Componente declarado en el diagrama de Sección 3.1 pero no figura como VO en § 3.3 ni es capturado por ningún evento; contradice `[D3]`. | Eliminar la línea del diagrama. |
| 3 | Media | L123: "MedioDeComunicacion (VO, 1..N)" | VO mencionado en el diagrama pero ausente en § 3.3; los medios de comunicación se modelan con los VOs `CorreoElectronico` y `Telefono`. | Sustituir en diagrama por los VOs concretos. |
| 4 | Baja | L174: "roles (set)...`Proveedor`, `Cliente`, `Empleado`, `EntidadFinanciera`, `Otro`" | El enum `roles` está repetido en la composición (L174) y en el catálogo § 6.1 (L791). No es incorrecto pero el modelo no declara cuál es fuente canónica. | Dejar referencia única al catálogo § 6.1 en la composición (o viceversa). |
| 5 | Baja | L176: "`Contactos` \| Componente `Contacto` (colección N)" | En la tabla raíz se usa `Contactos` (plural) como nombre; más abajo se habla de la sub-tabla "Componente interno `Contacto`". Naming plural/singular podría generar confusión. | Usar el singular `Contacto` (colección N) para alinearse con el diagrama y la FSM 4.2. |

#### Resumen
- Alta: 1 | Media: 2 | Baja: 2 | Total: 5

---

### 3. Máquinas de Estado (FSM)

#### FSM por Agregado

```
### FSM: Tercero
Estados: Activo, Inactivo
Terminales: ninguno
Transiciones:
  TerceroRegistrado: ∅ → Activo
  TerceroInactivado: Activo → Inactivo
  TerceroReactivado:  Inactivo → Activo
Eventos de progreso (en Activo):
  TerceroIdentificacionActualizada, TerceroRazonSocialActualizada, TerceroTipoPersonaActualizado,
  TerceroRolAsignado, TerceroRolRemovido,
  TerceroDireccionReferenciada, TerceroDireccionDesreferenciada, TerceroDireccionPreferidaDesignada
```

```
### FSM: Contacto
Estados: Activo, Inactivo
Terminales: ninguno
Transiciones:
  ContactoRegistrado: ∅ → Activo
  ContactoInactivado: Activo → Inactivo
  ContactoReactivado: Inactivo → Activo
Eventos de progreso (en Activo):
  ContactoActualizado, ContactoPrincipalDesignado
```

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Media | L499-L508 (diagrama FSM Tercero) | La FSM del Tercero no representa visualmente los eventos del componente Contacto; solo los menciona en nota. El lector puede creer que el Tercero no emite eventos de Contacto. | Añadir línea ASCII bajo `Activo` del tipo `· (Componente Contacto — ver 4.2)`. |
| 2 | Media | L549: "Precondición para `ContactoInactivado`: si el contacto es el principal, debe haberse designado previamente otro contacto activo como principal" | La FSM del Contacto muestra `ContactoInactivado: Activo → Inactivo` sin reflejar visualmente la precondición cruzada con `esPrincipal`. | Anotar en la flecha del diagrama la condición [R15] o poner guard visible. |
| 3 | Baja | L250: "`ContactoPrincipalDesignado` cambia `esPrincipal` en el nuevo y en el anterior simultáneamente" | Evento clasificado como "progreso" pero cambia propiedades en dos instancias. No aclarado en el diagrama. | Añadir nota en diagrama 4.2: "afecta a dos contactos: designa nuevo y desmarca anterior". |

#### Resumen
- Alta: 0 | Media: 2 | Baja: 1 | Total: 3

---

### 4. Invariantes

#### Clasificación de Invariantes

| ID | Tipo | Enforcement documentado | Gap |
|----|------|------------------------|-----|
| I1 | Eventual | `[SI1]` índice eventualmente consistente | Falta documentar compensación ante colisión concurrente detectada post-append |
| I2 | Local | Precondición eventos + `[SI2]` | OK |
| I3 | Local | Guards `TerceroRegistrado`, `TerceroRolRemovido` | OK |
| I4 | Local | Guards en varios eventos | OK |
| I5 | Local | Precondición `ContactoRegistrado`, `ContactoActualizado` | OK |
| I6 | Local | Precondición `TerceroRegistrado`, `TerceroDireccionDesreferenciada` | Gap: no existe guard explícito para `TerceroReactivado` |
| I7 | Local | `TerceroDireccionPreferidaDesignada` | OK |
| I8 | Local | Precondición `TerceroDireccionReferenciada` | OK |
| I9 | Local | Declarado en VOs | Gap: eventos no explicitan validación al reemplazar colección |
| I10 | Local (estructural ES) | Stream append-only | OK |
| I11 | Eventual | `[SI10]` + `[SI11]` | Gap: no documenta compensación post-detección concurrente |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Alta | L834: "`I1` … Eventual" y L351: "`[SI1]` … proyección" | I1 está clasificada como eventual, pero no se documenta estrategia de compensación si dos comandos concurrentes pasan el guard y el índice detecta colisión después. Gap alto porque `[R01]` es regla dura. | Añadir nota en `[SI1]` o D## que describa qué sucede ante colisión detectada a posteriori. |
| 2 | Alta | L844: "`I11` … excepción `RegistrarTerceroForzado`" | Similar a I1: no hay mecanismo de compensación documentado si dos registros concurrentes pasan los guards y quedan creados. | Añadir en `[SI10]` o `[D10]` la política de reconciliación post-detección. |
| 3 | Media | L839: "`I6` Dirección fiscal obligatoria" + L196: `TerceroReactivado` | El evento `TerceroReactivado` no documenta como precondición que el tercero tenga dirección fiscal. | Añadir precondición en `TerceroReactivado`: "el tercero conserva al menos una `ReferenciaDireccion` con `tipoUso=Fiscal`". |
| 4 | Media | L842: "`I9` … como mucho un preferido" | I9 es local pero ni `ContactoActualizado` ni `ContactoRegistrado` explicitan la verificación al recibir colecciones con 2+ preferidos. | Añadir precondición explícita "≤1 correo con `preferido=true` y ≤1 teléfono con `preferido=true`" en ambos eventos. |
| 5 | Media | L837: "`I4` Contacto principal único y obligatorio" | `ContactoInactivado` + `ContactoPrincipalDesignado` pueden observarse como dos appends separados con estado intermedio sin principal. | Aclarar que ambos eventos se appendean en el mismo commit atómico al inactivar al principal. |
| 6 | Baja | L851: "`I11` … Unicidad reforzada" | La fila de I11 no clasifica explícitamente su enforcement (guard vs eventual) como sí lo hace I1. | Añadir en la fila de I11 la columna de enforcement "guard del comando + índice eventualmente consistente". |

#### Resumen
- Alta: 2 | Media: 3 | Baja: 1 | Total: 6

---

### 5. Responsabilidades de Agregados

#### Mapa de Responsabilidades

```
### Responsabilidades: Tercero

Razón de cambio dominante: identidad + roles + referencias + contactos de un tercero
Comandos: 15+ (incluyendo AsegurarTerceroDesdeConsumidor, RegistrarTerceroForzado)
Eventos propios: 16
Invariantes protegidas: I1..I11
Domain services que lo coordinan: Ninguno dentro del BC (L156)
Diagnóstico: Saludable — agregado único, comportamiento rico y cohesivo.
```

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Media | L415-L456 (`[SI10]` completo) | `[SI10]` describe lógica de decisión compleja (lookup, canónica, rechazo, enriquecer, crear) que no está encapsulada en el agregado — vive en la SI. Riesgo de que se implemente como servicio fuera del agregado. | Aclarar que el cuerpo de decisión reside en el application service que carga el agregado; no es un domain service del BC. |
| 2 | Media | L455: "El consumidor automático, ante un posible duplicado, siempre rechaza y escala." | La política "nunca bypass `[I11]` para consumidores" no está formalizada como decisión/premisa. | Documentar como D## o P##: "`AsegurarTerceroDesdeConsumidor` nunca bypassa `[I11]` — eso es privilegio exclusivo de `RegistrarTerceroForzado`". |
| 3 | Baja | L217: "`contactoPrincipalActivo()`… invariante del agregado" | El comportamiento calculado referencia un invariante sin citar `[I4]`. | Añadir referencia `[I4]`. |
| 4 | Baja | L219-L226 (métodos calculados) | `direccionPreferidaPorTipoUso` retorna "`null` si no existe" pero para `tipoUso=Fiscal` nunca sería null por `[I6]`. | Añadir nota "salvo `tipoUso=Fiscal`, siempre existe mientras el tercero esté activo (`[I6]`)". |

#### Resumen
- Alta: 0 | Media: 2 | Baja: 2 | Total: 4

---

### 6. Semántica de Eventos

#### Inventario Semántico

```
### Eventos: Tercero

De transición (3): TerceroRegistrado, TerceroInactivado, TerceroReactivado
De progreso (8): TerceroIdentificacionActualizada, TerceroRazonSocialActualizada, TerceroTipoPersonaActualizado, TerceroRolAsignado, TerceroRolRemovido, TerceroDireccionReferenciada, TerceroDireccionDesreferenciada, TerceroDireccionPreferidaDesignada
De transición Contacto (3): ContactoRegistrado, ContactoInactivado, ContactoReactivado
De progreso Contacto (2): ContactoActualizado, ContactoPrincipalDesignado

Naming consistente: Sí
```

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Alta | L430: "`TerceroContactoAgregado`" / "`TerceroContactoMedioAgregado`" | `[SI10]` menciona dos eventos que no existen en el catálogo: rompe auto-contención y hace ambigua la implementación. | Usar los nombres correctos: `ContactoRegistrado` y `ContactoActualizado`. |
| 2 | Media | L574 (`TerceroRegistrado.contactoPrincipalInicial`) vs L725 (`ContactoRegistrado`) | Ambigüedad sobre si `TerceroRegistrado` crea el primer contacto sin emitir `ContactoRegistrado` adicional. | Añadir nota: "este evento crea el contacto principal inicial sin emitir un `ContactoRegistrado` adicional; el `contactoId` viene en el payload." |
| 3 | Media | L710: "`TerceroDireccionPreferidaDesignada` … `direccionIdAnterior` (opcional; caso raro)" | "Caso raro" sin explicar cómo ocurre. | Documentar el escenario exacto: "cuando el único con `esPreferida=true` de ese `tipoUso` fue desreferenciado". |
| 4 | Media | L574: `origen` en `TerceroRegistrado` vs eventos de actualización | Solo `TerceroRegistrado` carga `origen`/`contextoOrigen`. Eventos de actualización pueden venir de consumidor pero no capturan ese contexto. | Decidir explícitamente: "eventos de actualización no capturan `origen`; la trazabilidad queda en `motivo` + `usuarioId`" o añadir `origen`. |
| 5 | Baja | L720: `ContactoRegistrado` + esPrincipal=false | No obvio qué pasa al intentar registrar un contacto cuando I4 no está satisfecha aún (reparación). | Añadir nota: "`ContactoRegistrado` nunca viola I4 porque `esPrincipal` nace siempre en false; I4 se satisface por secuencia `ContactoRegistrado` + `ContactoPrincipalDesignado`". |
| 6 | Baja | L775: `contactoIdAnterior` | No se contempla `contactoIdAnterior = null` como ocurrió en `TerceroDireccionPreferidaDesignada`. | Alinear al patrón hermano: aceptar `contactoIdAnterior` opcional y documentar el escenario. |

#### Resumen
- Alta: 1 | Media: 3 | Baja: 2 | Total: 6

---

### 7. Idempotencia y Concurrencia

#### Matriz de Idempotencia

| Comando | IdempotencyKey | Guard anti-duplicado | expectedVersion | Riesgo |
|---------|---------------|---------------------|-----------------|--------|
| `RegistrarTercero` | No | `[I1]`, `[I11]` vía `[SI1]` | No doc | Medio |
| `RegistrarTerceroForzado` | No | Solo `[I1]` | No doc | Medio |
| `AsegurarTerceroDesdeConsumidor` | No explícito | Dedupe por rol/contacto/tipoUso | No doc | Bajo-Medio |
| `ActualizarIdentificacion` | No | Parcial (unicidad) | No doc | Medio |
| `DesignarContactoPrincipal` | No | Guards I4 | No doc | Medio |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Alta | L442: "idempotente — reintentos producen 0 eventos nuevos" | `[SI10]` declara idempotencia sin mecanismo concreto. Dos llamadas concurrentes pueden ambas pasar guards y emitir eventos duplicados. | Añadir en `[SI10]`: "cada invocación lleva `idempotencyKey` única del consumidor" + optimistic concurrency. |
| 2 | Alta | L351: "índice … eventualmente consistente" | No se documenta resolución de ventana de carrera entre dos `RegistrarTercero` simultáneos con misma identificación. | Documentar: (a) unique-constraint DB transaccional antes del append, o (b) evento `TerceroDuplicadoDetectado` + reconciliación. |
| 3 | Media | Toda § 5 | Ningún evento documenta `expectedVersion` del stream como precondición (patrón estándar ES optimistic concurrency). | Añadir en § 2 o § 3.2: "todos los comandos verifican `expectedVersion` del stream". |
| 4 | Media | L777: `DesignarContactoPrincipal` | Dos `DesignarContactoPrincipal` simultáneos podrían ambos pasar el guard leyendo el mismo estado. | Igual que #3: documentar expectedVersion. |
| 5 | Media | L415 (`[SI10]`) | Mezcla creación con enriquecimiento; si retry parcial falla entre eventos, no es idempotente. | Aclarar: "todos los eventos van en un solo commit atómico; si falla, nada se persiste". |
| 6 | Baja | L442 | No documenta cómo un consumidor con timeout reintenta safe. | Añadir nota: "reintentos safe por construcción — si el primer llegó al stream, el segundo hace no-op". |

#### Resumen
- Alta: 2 | Media: 3 | Baja: 1 | Total: 6

---

### 8. Sagas y Procesos Multi-Agregado

#### Mapa de Procesos

```
### Proceso: (ninguno dentro del BC)

El BC declara explícitamente que NO tiene domain services ni sagas internas:
- L156: "Terceros no tiene domain services."
- L459: "No hay relaciones entre agregados internos del BC."

La orquestación multi-dominio (BFF / API Composition) vive EXTERNAMENTE, documentada en `anexo-decision-orquestacion-registro.md`.
```

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Media | L415 `[SI10]` | Aunque no hay saga, `AsegurarTerceroDesdeConsumidor` ejecuta secuencia compleja que emite N eventos. Si precondiciones fallan parcialmente, no se documenta rollback/compensación. | Añadir en `[SI10]`: "todos los eventos en un único append atómico; si falla, nada se emite. No hay compensación porque no hay pasos multi-agregado." |
| 2 | Media | L9 tabla contenidos y anexo externo | El modelo reconoce proceso externo pero no declara la responsabilidad de compensación como no-suya explícitamente. | Añadir en § 8: "Compensación ante fallos en orquestación multi-dominio: responsabilidad del BFF/API Composition según anexo." |
| 3 | Baja | Sección 5.3 `TerceroRolAsignado` L657 | Se notifica a consumidor para que "abra su registro" — proceso multi-agregado eventual sin `correlationId` ni política de retry documentados. | Añadir en § 8 o `[PD1]`: "contrato con consumidores define `correlationId`, retry, DLQ en Fase 3 EventCatalog". |

#### Resumen
- Alta: 0 | Media: 2 | Baja: 1 | Total: 3

---

### 9. Decisiones Abiertas

#### Inventario de Pendientes

| # | Ubicación | Texto | Tipo | Riesgo |
|---|-----------|-------|------|--------|
| 1 | L908 `[PD1]` | Contratos de eventos de integración | Fase 3 EventCatalog | Medio |
| 2 | L909 `[PD2]` | Estrategia técnica del índice de unicidad | Implementación | **Alto** |
| 3 | L910 `[PD3]` | Canales externos adicionales | Futuro Producto | Bajo |
| 4 | L395 `[SI7]` | Verificación MX de correos | Implícita opcional | Bajo |
| 5 | L399 `[SI8]` | Advertencia UX contactos sin nombre | Implícita UX | Bajo |
| 6 | L341 | Validación por país delegada al catálogo | Implícita | Bajo |
| 7 | L710 | `direccionIdAnterior=null` "caso raro" | Implícita semántica | Medio |
| 8 | L777 | `contactoIdAnterior=null` "caso raro" | Implícita semántica | Medio |
| 9 | L965 | Changelog desactualizado | TODO | Bajo |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Alta | L909: "`PD2` queda fuera del modelo por ser decisión de implementación" | El mecanismo del índice determina si `[I1]` e `[I11]` se cumplen. Diferirlo a implementación sin definir comportamiento ante colisión concurrente deja una regla de negocio dura abierta. | Reclasificar PD2 como "pendiente del modelo" o añadir sub-pendiente "resolución de colisiones concurrentes post-detección" con criterio de cierre. |
| 2 | Media | L965 (Control de versiones) | Changelog afirma que solo 1-2 están redactadas, pero todo 3-12 está escrito. | Actualizar entrada v1.0. |
| 3 | Media | L710, L777 ("caso raro") | Ambiguedad sobre cuándo se acepta `null` en `direccionIdAnterior` / `contactoIdAnterior`. | Documentar escenario exacto. |
| 4 | Baja | L395 `[SI7]` "opcionalmente el sistema puede verificar" | "Opcionalmente" sin criterio de activación. | Aclarar: "feature opcional habilitable por configuración de tenant". |
| 5 | Baja | L341 | Validación condicionada a evento futuro del catálogo sin ownership. | Mover a `[PD#]` formal o fijar plazo de cierre. |

#### Resumen
- Alta: 1 | Media: 2 | Baja: 2 | Total: 5

---

### 10. Sanity Check (Coherencia Cruzada)

#### Coherencia

```
Referencias del alcance USADAS: R01, R03, R04, R05, R06, R07, R08, R09, R10, R11, R12, R13, R15, R16, R17, R18, R19, R21, R23, R24, R25, R02 (en P3)
Referencias del alcance NO usadas: R14, R20, R22

Invariantes: I1..I11 todas definidas y usadas → OK
Decisiones: D1..D10 todas definidas y usadas → OK
Premisas: P1..P6 todas definidas → OK
Pendientes: PD1..PD3 todos declarados → OK
SIs: SI1..SI11 todas declaradas y referenciadas → OK

Conteos:
  16 eventos ✓, 19 permisos ✓, 11 invariantes ✓, 10 decisiones ✓, 6 premisas ✓, 3 pendientes ✓, 11 SIs ✓

Conceptos fantasma:
  - HistorialIdentificacion en diagrama pero D3 lo elimina
  - TerceroContactoAgregado / TerceroContactoMedioAgregado en [SI10] no existen como eventos
```

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| 1 | Alta | L430 | Dos eventos referenciados en `[SI10]` no existen. Contradicción cruzada. | Renombrar a `ContactoRegistrado` y `ContactoActualizado`. |
| 2 | Alta | L116 vs L876 `[D3]` | Contradicción: diagrama muestra `HistorialIdentificacion` como VO pero D3 lo elimina. | Eliminar la línea del diagrama. |
| 3 | Media | L123 vs § 3.3 | `MedioDeComunicacion` en diagrama no existe en composición. | Reemplazar por `CorreoElectronico`, `Telefono`. |
| 4 | Media | L965 | Control de versiones dice "Secciones 3-12 en construcción" pero están completas. | Actualizar al cerrar v1.0. |
| 5 | Media | R23 en § 8 | R23 solo se cita en exclusión; podría referenciarse en comportamiento `estaActivo()`. | Añadir `[R23]` junto a `estaActivo()` L220. |
| 6 | Media | R20, R22 sin usar | R20 (alcance) y R22 (completitud) están en alcance pero no se referencian. | Añadir `[R20]` en § 3.1 (L84) y `[R22]` junto a `[D5]` (L878). |
| 7 | Media | R14 sin usar | R14 (medios exclusivos del contacto) no está referenciada. | Añadir `[R14]` en L176 o como nota en § 3.2. |
| 8 | Baja | L165/L171/D6 | DV no-clave-unicidad mencionado en 3 lugares. | Dejar una sola fuente canónica y referencias. |

#### Resumen
- Alta: 2 | Media: 5 | Baja: 1 | Total: 8

---

### Resumen Ejecutivo

| Skill | Alta | Media | Baja | Total |
|-------|------|-------|------|-------|
| Glosario | 1 | 2 | 1 | 4 |
| Composición | 1 | 2 | 2 | 5 |
| FSM | 0 | 2 | 1 | 3 |
| Invariantes | 2 | 3 | 1 | 6 |
| Responsabilidades | 0 | 2 | 2 | 4 |
| Semántica Eventos | 1 | 3 | 2 | 6 |
| Idempotencia | 2 | 3 | 1 | 6 |
| Sagas | 0 | 2 | 1 | 3 |
| Decisiones Abiertas | 1 | 2 | 2 | 5 |
| Sanity Check | 2 | 5 | 1 | 8 |
| **TOTAL** | **10** | **26** | **14** | **50** |

### Top 5 Hallazgos Críticos

| # | Skill origen | Severidad | Problema | Corrección mínima |
|---|-------------|-----------|----------|-------------------|
| 1 | Glosario / Composición / Semántica Eventos / Sanity Check | Alta | `[SI10]` (L430) referencia dos eventos (`TerceroContactoAgregado`, `TerceroContactoMedioAgregado`) que no existen en el catálogo de eventos § 5. Replicado en 4 auditorías distintas. | Reemplazar ambos nombres en L430 por `ContactoRegistrado` y `ContactoActualizado`. |
| 2 | Idempotencia | Alta | El índice de unicidad de `[I1]` es eventualmente consistente (`[SI1]`) y no se documenta resolución de colisiones concurrentes. Riesgo: dos terceros con misma identificación violando `[R01]`. | Añadir a `[SI1]` estrategia concreta: unique-constraint transaccional previo al append, o evento `TerceroDuplicadoDetectado` + política. |
| 3 | Idempotencia | Alta | `AsegurarTerceroDesdeConsumidor` se declara idempotente sin mecanismo explícito. Dos llamadas concurrentes pueden emitir eventos duplicados. | Añadir en `[SI10]`: `idempotencyKey` única del consumidor + optimistic concurrency por versión del stream. |
| 4 | Sanity Check / Composición | Alta | El diagrama § 3.1 (L116) muestra `HistorialIdentificacion (VO, colección N)` contradiciendo `[D3]`. | Eliminar la línea del diagrama ASCII. |
| 5 | Invariantes / Decisiones Abiertas | Alta | `[I11]` no documenta qué ocurre si la inconsistencia se detecta post-creación por dos registros concurrentes. `[PD2]` difiere este vacío a "implementación". | Documentar en `[SI10]`/`[D10]` la política de reconciliación post-detección o reclasificar `[PD2]`. |
