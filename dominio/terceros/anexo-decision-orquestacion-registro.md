# Anexo — Decisión de diseño: Orquestación del registro de terceros

> **Fecha:** Abril 2026
> **Propósito:** Documentar cómo se orquesta la creación de un tercero cuando la información requerida está distribuida en múltiples servicios.

---

## 1. Problema

Registrar un tercero completo (proveedor, cliente, etc.) requiere información que vive en múltiples servicios:

| Servicio | Datos que gestiona | Estado |
|----------|-------------------|:------:|
| **Terceros** | Identidad base: tipo persona, tipo documento, número, razón social, roles, contactos | ✅ Definido |
| **Direcciones** | Dirección fiscal, comercial, correspondencia — validada según país | ✅ Definido |
| **Impuestos** | Perfil tributario: régimen, tipo contribuyente localizado, atributos fiscales por país | ✅ Definido |
| **Tesorería** | Cuentas bancarias del tercero | ⬜ Pendiente de definir |
| **Condiciones comerciales** | Plazos de pago, moneda, categoría, límite de crédito, etc. | ⬜ Pendiente — los candidatos más propensos son OXP (proveedor) y CXC (cliente), pero su composición no se ha definido aún |

En SincoERP todo esto se resuelve en una sola tabla y un solo formulario. En la nueva arquitectura de microservicios, ¿cómo se mantiene esa experiencia unificada sin acoplar los servicios?

---

## 2. Alternativas evaluadas

### Alternativa A — El frontend llama a cada servicio directamente

La UI hace múltiples llamadas independientes a cada servicio.

| Aspecto | Evaluación |
|---------|-----------|
| Experiencia del usuario | Mala — el usuario podría ver errores parciales (se creó el tercero pero falló la dirección) |
| Complejidad en el frontend | Alta — el frontend debe manejar secuencia, errores parciales, reintentos |
| Acoplamiento | El frontend conoce todos los servicios |

**Descartada.** Mueve la complejidad de orquestación al frontend.

### Alternativa B — Terceros orquesta todo

El servicio de Terceros recibe toda la información y llama internamente a Direcciones, Impuestos y los demás.

| Aspecto | Evaluación |
|---------|-----------|
| Experiencia del usuario | Buena — un solo punto de entrada |
| Acoplamiento | Alto — Terceros conoce y depende de todos los otros servicios |
| Responsabilidad | Terceros se convierte en orquestador de negocio — viola su responsabilidad de solo gestionar identidad |

**Descartada.** Terceros no debería saber nada sobre perfiles tributarios, cuentas bancarias ni condiciones comerciales.

### Alternativa C — BFF (Backend for Frontend) ✅

Una capa intermedia dedicada a la experiencia de usuario que compone las llamadas a los servicios.

| Aspecto | Evaluación |
|---------|-----------|
| Experiencia del usuario | Buena — un formulario, un envío, una respuesta |
| Acoplamiento | Bajo — cada servicio solo conoce su responsabilidad. El BFF orquesta sin lógica de negocio. |
| Responsabilidad | Cada servicio valida lo suyo. El BFF solo compone y coordina. |

**Seleccionada.** Es el patrón estándar de la industria para esta situación (SAP simula esto siendo monolítico; Shopify y Stripe lo implementan implícitamente en su API pública).

---

## 3. Flujo de registro orquestado

### Desde la perspectiva del usuario

El usuario ve un solo formulario con secciones. Llena toda la información y presiona guardar una vez.

```
┌──────────────────────────────────────────────┐
│  Registro de proveedor                        │
│                                              │
│  ┌─────────────┐  ┌──────────────────────┐   │
│  │ Datos básicos│  │ Dirección            │   │
│  │ Nombre       │  │ Tipo: Fiscal         │   │
│  │ Tipo doc     │  │ País: CO             │   │
│  │ Número       │  │ Calle 10 #43A-27     │   │
│  │ Tipo persona │  │ Bogotá, Cundinamarca │   │
│  │ Rol: Proveedor│ │ CP: 110111           │   │
│  └─────────────┘  └──────────────────────┘   │
│  ┌─────────────┐  ┌──────────────────────┐   │
│  │ Info fiscal  │  │ Cuenta bancaria      │   │
│  │ Régimen      │  │ Banco: Bancolombia   │   │
│  │ Autorretenedor│ │ Tipo: Ahorros        │   │
│  │ Gran contrib.│  │ Número: ************ │   │
│  └─────────────┘  └──────────────────────┘   │
│                                              │
│              [ Guardar ]                      │
└──────────────────────────────────────────────┘
```

### Desde la perspectiva técnica

```
  Usuario presiona "Guardar"
       │
       │  UN solo request con toda la información
       ▼
  ┌─────────────────┐
  │       BFF       │
  │ (Backend for    │
  │  Frontend)      │
  └────────┬────────┘
           │
           │  Paso 1: Crear tercero base (obtener id)
           ▼
      ┌─────────┐
      │Terceros │ → Valida unicidad, crea registro, retorna id
      └────┬────┘
           │
           │  Paso 2: Con el id del tercero, crear en paralelo
           ├──────────────────┬──────────────────┐
           ▼                  ▼                  ▼
      ┌──────────┐      ┌──────────┐      ┌──────────┐
      │Direcciones│     │Impuestos │      │Tesorería │
      │          │      │(perfil)  │      │(cuentas) │
      │Valida    │      │Valida    │      │Registra  │
      │formato CO│      │atributos │      │cuenta    │
      │Crea dir. │      │Crea perfil│     │bancaria  │
      └──────────┘      └──────────┘      └──────────┘
           │                  │                  │
           └──────────────────┴──────────────────┘
                              │
                              ▼
                    BFF compone respuesta
                              │
                              ▼
                    Usuario ve: "Proveedor creado"
```

### Secuencia detallada

| Paso | Servicio | Acción | Depende de | Estado |
|:----:|----------|--------|:----------:|:------:|
| 1 | Terceros | Crear tercero base (tipo persona, tipo doc, número, razón social, roles, contacto principal). El tercero queda en estado **En Registro** — identidad registrada, aún no operable. Retorna `terceroId`. | — | ✅ |
| 2 | Direcciones | Crear la dirección fiscal referenciando al tercero. Al completar, Direcciones emite un evento de confirmación que Terceros consume para emitir `TerceroActivado`, transicionando el tercero a estado **Activo**. | Paso 1 | ✅ |
| 3 | Impuestos | Crear perfil tributario para el tercero en el país correspondiente | Paso 1 (tercero Activo) | ✅ |
| 4 | Tesorería | Registrar cuentas bancarias del tercero | Paso 1 (tercero Activo) | ⬜ |
| 5 | OXP / CXC | Registrar condiciones comerciales como proveedor o cliente | Paso 1 (tercero Activo) | ⬜ |

Los pasos 2 y los pasos 3-5 se ejecutan en paralelo desde el punto de vista del BFF. La diferencia semántica es importante: **el paso 2 determina si el tercero llega a Activo**, mientras que los pasos 3-5 son enriquecimiento posterior que no bloquea la activación.

---

## 4. Manejo de errores

### ¿Qué pasa si un paso falla?

| Escenario | Qué pasa | Cómo se resuelve |
|-----------|----------|------------------|
| Paso 1 falla (Terceros) | No se crea nada | El BFF retorna error inmediato. El usuario corrige y reintenta. |
| Paso 2 falla (Direcciones) — transitorio | El tercero queda en **En Registro**. La plataforma de mensajería reintenta automáticamente según su política de retries. | Al reintentar con éxito, Direcciones emite la confirmación y Terceros transiciona a **Activo** vía `TerceroActivado`. Para el usuario es transparente (puede ver un indicador "procesando" si el retraso es perceptible). |
| Paso 2 falla (Direcciones) — permanente | Tras agotar los reintentos de la plataforma, Terceros emite `TerceroRegistroAbortado` y el tercero queda en estado terminal **Abortado**. No es reactivable. | El operador investiga la causa (dirección inválida, servicio caído, rechazo regulatorio). Si corresponde, registra un nuevo tercero con la misma identificación — el índice de unicidad excluye los Abortados, por lo que la identificación queda disponible para un nuevo intento con otro `terceroId`. |
| Paso 3 falla (Impuestos) | El tercero ya está **Activo** (su activación dependía solo de la dirección fiscal del paso 2). El perfil tributario queda pendiente. | Impuestos no bloquea hasta que se necesite calcular tributos. El perfil se completa después sin afectar la operatividad del tercero. |
| Paso 4 falla (Tesorería) | Tercero **Activo** sin cuenta bancaria. | Se completa después. No bloquea la existencia ni la operatividad del tercero. |
| Paso 5 falla (OXP/CXC) | Tercero **Activo** sin condiciones comerciales. | Se completan después. |

### Principio de diseño

El registro del tercero **es todo-o-nada para su núcleo esencial**: identidad base (paso 1) + dirección fiscal (paso 2). Si cualquiera de los dos falla, el tercero no queda operable — permanece en **En Registro** mientras la plataforma reintenta el paso 2, y pasa a **Abortado** si el fallo es permanente. Nunca existe un tercero **Activo** sin dirección fiscal.

Los demás datos (perfil tributario en Impuestos, cuentas bancarias en Tesorería, condiciones comerciales en OXP/CXC) son **enriquecimiento posterior**: el tercero **Activo** puede existir sin ellos. Cada dominio consumidor decide cuándo los necesita para operar, y un fallo en esos pasos no afecta el ciclo de vida del tercero.

Esta distinción (núcleo obligatorio vs enriquecimiento opcional) resuelve la tensión entre:
- **Regla fuerte del dominio:** todo tercero Activo debe tener dirección fiscal (R25 / I6).
- **Servicio único de Direcciones:** las direcciones las gestiona un único servicio, sin duplicación.
- **Arquitectura event-driven asíncrona:** sin llamadas síncronas ni transacciones distribuidas.

El estado intermedio **En Registro** reconoce explícitamente la ventana de consistencia eventual del registro multi-servicio. Ver decisión `[D13]` en el modelo de dominio de Terceros.

---

## 5. Creación desde otro módulo

Cuando un usuario de OXP necesita radicar una obligación con un proveedor que no existe, el flujo es el mismo pero iniciado desde OXP:

```
  Usuario en OXP radica obligación
       │
       │  "Este proveedor no existe"
       ▼
  UI muestra formulario de registro rápido
  (puede ser modal o paso dentro del flujo de OXP)
       │
       │  Misma orquestación BFF
       ▼
  BFF → Terceros → Direcciones → Impuestos → Tesorería
       │
       ▼
  Proveedor creado, OXP continúa con la radicación
```

El BFF es el mismo independientemente de desde qué módulo se inicie el registro. La UI puede adaptar el formulario (ej: desde OXP se pre-selecciona el rol "proveedor"), pero la orquestación es la misma.

---

## 6. ¿Qué es y qué no es el BFF?

### Es

- Una capa intermedia entre el frontend y los microservicios.
- Específica para un tipo de experiencia de usuario (no es un API Gateway genérico).
- Responsable de componer llamadas y coordinar la secuencia.
- El lugar donde se maneja el error parcial y se decide qué mostrar al usuario.

### No es

- Un servicio de negocio — no tiene reglas de negocio propias.
- Un reemplazo de los servicios — cada servicio sigue validando lo suyo.
- Parte de ningún dominio — es infraestructura de aplicación.
- Necesario para comunicación entre servicios — solo existe para la experiencia de usuario.

---

## 7. Referencia de la industria

| Sistema | Cómo resuelve el registro multi-servicio |
|---------|----------------------------------------|
| **SAP** | Monolítico: una transacción ACID, un formulario, una tabla central (BUT000) |
| **Odoo** | Monolítico: un modelo (res.partner) con campos de localización en la misma tabla |
| **Shopify** | BFF implícito: una llamada API crea customer + dirección. Payment method es llamada aparte. |
| **Stripe** | BFF implícito: crear customer es una llamada, attachar payment method es otra. Secuencial e idempotente. |
| **Microservicios (patrón)** | BFF + API Composition: capa dedicada que compone llamadas a servicios independientes |

---

## 8. Impacto en los documentos de alcance

Esta decisión no cambia la responsabilidad de ningún servicio:

| Servicio | Sigue siendo responsable de |
|----------|-----------------------------|
| Terceros | Identidad base, unicidad, roles, contactos, estado |
| Direcciones | Estructura, validación por país, persistencia centralizada |
| Impuestos | Perfil tributario, atributos fiscales, motor de cálculo |

Lo que se agrega es la conciencia de que **existe una capa de orquestación (BFF)** que coordina la experiencia de usuario sin que los servicios se conozcan entre sí.

---

## 9. Pendientes

| # | Pendiente | Contexto |
|---|-----------|----------|
| PD1 | Tesorería — cuentas bancarias | Definir el sub-dominio de Tesorería y cómo gestiona las cuentas bancarias del tercero. Cuando se defina, se formaliza como paso 4 del flujo. |
| PD2 | Condiciones comerciales del tercero | Definir dónde viven las condiciones comerciales (plazos de pago, moneda, límite de crédito). Los candidatos más propensos son OXP (para proveedores) y CXC (para clientes). Cuando se defina, se agrega como paso 5 del flujo. |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Abril 2026 | Versión inicial. Decisión BFF + API Composition para orquestación de registro multi-servicio. Flujo con 3 servicios confirmados (Terceros, Direcciones, Impuestos) y 2 pendientes (Tesorería, Condiciones comerciales). |
| 1.1 | Abril 2026 | **Registro en dos fases.** Alineado con `[D13]` del modelo de dominio de Terceros. El tercero nace en **En Registro** (no operable) y pasa a **Activo** solo cuando Direcciones confirma asincrónicamente la creación de la dirección fiscal. Si la confirmación falla permanentemente, el tercero queda en estado terminal **Abortado**. El "principio de diseño" se reformula: el núcleo (identidad + dirección fiscal) es todo-o-nada; los demás datos (perfil tributario, cuentas bancarias, condiciones comerciales) sí son enriquecimiento posterior. Resuelve la tensión entre R25/I6 (dirección fiscal obligatoria en tercero Activo), servicio único de Direcciones, y arquitectura event-driven asíncrona. |
