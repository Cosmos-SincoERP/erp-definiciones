# Modelo de Dominio — [Nombre del Sub-dominio]

## Tabla de contenido

1. [Propósito y relación con otros documentos](#1-propósito)
2. [Convenciones del documento](#2-convenciones)
3. [Bounded Context y Agregados](#3-bounded-context)
4. [Máquinas de estado](#4-máquinas-de-estado)
5. [Catálogo de eventos](#5-catálogo-de-eventos)
6. [Catálogos del dominio](#6-catálogos-del-dominio)
7. [Invariantes del dominio](#7-invariantes)
8. [Qué NO contiene este documento](#8-exclusiones)
9. [Decisiones de arquitectura y diseño](#9-decisiones)
10. [Premisas de negocio](#10-premisas)
11. [Pendientes por definir](#11-pendientes)
12. [Catálogo de permisos atómicos](#12-permisos)

---

## 1. Propósito y relación con otros documentos

| Documento | Rol | Descripción |
|-----------|-----|-------------|
| `definicion-alcance.md` | QUÉ hace el sistema | Fuente de verdad para glosario, actores, flujos y reglas (`[R##]`). No se duplica aquí. |
| **Este documento** | CÓMO se comporta el dominio | Eventos, transiciones, precondiciones, invariantes, tipos de concepto. |
| EventCatalog | Catalogación técnica | Consumirá este documento como especificación de entrada. |

Las reglas de negocio se referencian como `[R##]` y su texto completo vive en `definicion-alcance.md`, Sección 6.

---

## 2. Convenciones del documento

### 2.1. Nomenclatura
- **Eventos:** PascalCase en español, sin tildes ni caracteres especiales (compatibilidad con código fuente).
- **Referencias:** `[R##]` reglas de negocio, `[P##]` premisas, `[D##]` decisiones, `[I##]` invariantes, `[SI##]` sugerencias de implementación, `[PD#]` pendientes.
- **Agregados:** Nombres en PascalCase; corresponden a los términos del glosario canónico (`definicion-alcance.md`, Sección 2).
- **Alcance del glosario canónico:** Los domain services, entidades internas y value objects son artefactos del modelo de dominio — no requieren entrada en el glosario canónico.

### 2.2. Template de evento

Cada evento se documenta con esta estructura:

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | Qué ocurrió en términos de negocio. |
| **Causalidad** | Tipo: directa, derivado por transición, derivado por configuración, efecto inter-agregado, compensatorio. |
| **Agregado** | Agregado que emite el evento. |
| **Estado previo** | Estado requerido del agregado antes del evento. |
| **Estado resultante** | Estado del agregado después del evento (o "sin cambio" si es evento de progreso). |
| **Precondiciones** | Condiciones que deben cumplirse. Referencias a `[R##]`. |
| **Información capturada** | Datos que el evento registra (payload). |
| **Efectos** | Consecuencias: entidades creadas, saldos modificados, eventos derivados. |

### 2.3. Diagramas
- FSM en ASCII. Estados terminales marcados con `■`.
- Eventos de progreso (sin cambio de estado) se listan dentro del recuadro del estado.
- Eventos de transición se muestran en las flechas entre estados.

### 2.4. Causalidad entre eventos

| Tipo | Descripción | Consistencia |
|------|-------------|-------------|
| Derivado por transición | Mismo agregado, mismo append atómico. | Transaccional |
| Derivado por configuración | Mismo agregado, condicional a configuración. | Transaccional |
| Efecto inter-agregado | Domain service coordina entre agregados. | Eventual |
| Compensatorio | Revierte un efecto previo por fallo de saga. | Eventual |

### 2.5. Tipos de cruce (si aplica)

Documentar la semántica de los tipos de cruce del dominio: operaciones de negocio vs. reversiones técnicas por fallo de saga.

### 2.6. Precisiones terminológicas

Desambiguación de términos polisémicos del dominio (ej: un término que designa simultáneamente un proceso y un estado).

---

## 3. Bounded Context y Agregados

### 3.1. [Nombre] como Bounded Context

Diagrama ASCII del contexto con agregados y domain services como flechas de conexión.

```
┌──────────────────────────────────────────────────┐
│              Bounded Context: [Nombre]            │
│                                                   │
│   ┌────────────┐          ┌────────────┐         │
│   │ Agregado1  │◄──[Svc]─►│ Agregado2  │         │
│   └────────────┘          └────────────┘         │
└──────────────────────────────────────────────────┘
```

### 3.2. Agregado: [Nombre]

**Descripción:** Responsabilidad principal del agregado.

**Composición:**

| Componente | Tipo | Descripción | Atributos clave |
|------------|------|-------------|-----------------|
| ... | Entidad / VO | ... | ... |

**Comportamiento calculado (no almacenado):**

| Método | Fórmula | Usado por |
|--------|---------|-----------|
| ... | ... | ... |

**Eventos:** N eventos propios.

*(Repetir para cada agregado)*

### 3.N. Value Objects compartidos

VOs reutilizados entre agregados.

| VO | Usado por | Descripción |
|----|-----------|-------------|
| ... | ... | ... |

### 3.N+1. Sugerencias de implementación `[SI##]`

Recomendaciones técnicas que complementan las definiciones de dominio.

### 3.N+2. Servicio de dominio: [Nombre]

Para cada domain service:

**Trigger:** Qué inicia el proceso.
**Flujo principal:** Pasos numerados con evento emitido y stream destino.
**Tabla de compensación:**

| Paso | Evento | Stream | Si falla | Estrategia |
|------|--------|--------|----------|------------|
| ... | ... | ... | ... | ... |

**Protocolo de proceso:** correlationId, stream propio, persistencia de estado.

### 3.N+3. Relaciones entre agregados

Diagrama de cardinalidad (N:1, 1:1, 1:N) y referencias entre agregados.

### 3.N+4. Patrón: entidades espejo (si aplica)

Tabla de correspondencia entre entidades que representan el mismo hecho desde dos agregados distintos.

---

## 4. Máquinas de estado

### 4.1. [Agregado] FSM

Diagrama ASCII con:
- Estados como recuadros
- Eventos de transición en las flechas
- Eventos de progreso dentro del recuadro del estado
- Estados terminales marcados con `■`

```
┌──────────┐                ┌──────────────────────────┐
│ Estado1  │──EventoX──────►│ Estado2                  │
└──────────┘                │                          │
                            │  Eventos de progreso:    │
                            │    · EventoY             │
                            └────────────┬─────────────┘
                                         │ EventoZ
                                         ▼
                                  ┌──────────┐
                                  │ Estado3  │ ■
                                  └──────────┘
```

**Notas:** Explicación estado-por-estado (simétrica entre agregados):
- `Estado1` es el estado inicial para...
- `Estado2` recibe eventos de progreso...
- `Estado3`: evento de transición cuando...

*(Repetir para cada agregado)*

---

## 5. Catálogo de eventos

N eventos totales: Agregado1 X + Agregado2 Y + ...

Organizados por tema funcional (no por agregado).

### 5.1. [Tema funcional 1]

#### [NombreDelEvento]

| Aspecto | Detalle |
|---------|---------|
| **Descripción** | ... |
| **Causalidad** | ... |
| **Agregado** | ... |
| **Estado previo** | ... |
| **Estado resultante** | ... |
| **Precondiciones** | ... |
| **Información capturada** | ... |
| **Efectos** | ... |

*(Repetir para cada evento)*

---

## 6. Catálogos del dominio

Catálogos propios del sub-dominio que tipifican o clasifican elementos del modelo. Excluye catálogos externos (gestionados por el servicio de Datos de Referencia) y atributos dinámicos que pertenecen a los agregados.

Cada sub-dominio adapta la estructura según la naturaleza de sus catálogos (enumeraciones, tipos con comportamiento, clasificaciones con reglas asociadas, etc.).

*(Subsección por cada catálogo, con sus valores y reglas aplicables.)*

---

## 7. Invariantes del dominio

Restricciones estructurales que deben ser verdaderas en todo momento. Clasificación: **local** (un solo agregado, transaccional) o **eventual** (cruza fronteras, enforceada por proyección).

| # | Invariante | Agregado | Referencia |
|---|-----------|----------|------------|
| I1 | ... | ... | ... |

---

## 8. Qué NO contiene este documento

Lista explícita de lo que está fuera del alcance del modelo de dominio.

| Concepto | Razón | Referencia |
|----------|-------|------------|
| ... | ... | ... |

---

## 9. Decisiones de arquitectura y diseño

| # | Decisión | Justificación | Referencia |
|---|----------|---------------|------------|
| D1 | ... | ... | ... |

---

## 10. Premisas de negocio

| # | Premisa | Impacto en el modelo |
|---|---------|---------------------|
| P1 | ... | ... |

---

## 11. Pendientes por definir

| # | Pendiente | Contexto | Trigger de activación |
|---|-----------|----------|----------------------|
| PD1 | ... | ... | ... |

---

## 12. Catálogo de permisos atómicos del dominio

Cada bounded context declara los recursos que protege y las acciones que expone como permisos atómicos. La plataforma de seguridad del ERP consume este catálogo para integrarlo a su modelo de autorización (roles, políticas, relaciones).

**Lo que define este catálogo:**
- **Recursos protegidos** — agregados o entidades del dominio que requieren control de acceso.
- **Acciones por recurso** — operaciones de negocio que se pueden proteger, identificadas con la convención `accion_recurso`.
- **Restricciones de contexto** — dimensiones que limitan el acceso más allá de la acción (ej: empresa, unidad organizacional).

**Lo que NO define este catálogo:**
- Roles (responsabilidad de la plataforma de seguridad).
- Asignación de usuarios a permisos.
- Mecanismo de autenticación o enforcement.

**Convención de naming:** `accion_recurso` en snake_case (ej: `resolver_borrador`, `cerrar_periodo`). Compatible con OAuth scopes, policy engines (OPA, Cedar) y motores ReBAC (SpiceDB, OpenFGA).

| Recurso | Acción | Identificador |
|---------|--------|---------------|
| ... | ... | `accion_recurso` |

**Restricción de contexto:** [Describir las dimensiones que limitan el acceso — ej: empresa, unidad organizacional, tipo de documento].

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | ... | Versión inicial. |
