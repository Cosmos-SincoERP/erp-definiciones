# Plan de Trabajo — Junio 2026

## Contexto

El ERP tiene 3 sub-dominios transaccionales con modelo completo (OXP v2.9, Impuestos v1.3, Contabilidad v1.0), pero no pueden liberarse a desarrollo porque dependen de 3 sub-dominios base que solo tienen definición inicial (Terceros, Estructura Organizacional, Plataforma). Además hay refinamientos pendientes en OXP y necesidad de documentación transversal para el equipo.

**Estrategia:** Ejecución secuencial — un ítem a la vez, cerrar y pasar al siguiente. El documento consolidado se crea al inicio con lo que ya existe y se actualiza a medida que avancemos.

**Actualización 2026-05-29:** Los 4 bloqueantes principales (Documento consolidado, Plataforma/Datos de Referencia, Terceros, Estructura Organizacional) están cerrados. La integración OXP ↔ Contabilidad (ítem 8) también quedó cerrada en el PR #5. Quedan abiertos los ítems 5, 6, 7, 9 y 10.

---

## Estado de los sub-dominios

| Sub-dominio | Alcance | Modelo | Auditoría | Listo para dev |
|-------------|---------|--------|-----------|----------------|
| Obligaciones por Pagar (OXP) | ✅ v1.7 | ✅ v3.3 | ✅ múltiples rondas | ❌ Pendiente refinamientos (ítems 5–7) |
| Impuestos | ✅ v1.4 | ✅ v2.0.4 | ✅ 2 rondas + multi-país F1 LatAm | ✅ |
| Contabilidad | ✅ v1.3 | ✅ v1.2 + caso PUC servicio | ✅ 3 rondas + validación contractual | ✅ (F1) |
| Terceros | ✅ v1.0 | ✅ v1.0 | ✅ 10 skills + 4 rondas PO | ✅ |
| Estructura Organizacional | ✅ v1.2 | ✅ v1.4 + 2 anexos arquitectónicos | ✅ | ✅ |
| Datos de Referencia (compartido) | ✅ v1.0 | ✅ v1.0 (especificación) | — | ✅ |
| Direcciones (compartido) | ✅ v1.0 | ✅ v1.0 | — | ✅ |
| Asistente de Onboarding (compartido) | ✅ v1.0 | ✅ v1.0 + caso PUC | — | ✅ |

---

## Cadena de bloqueo

```
Plataforma (países, monedas, tipos documento)            ✅
       ↓ lectura
Terceros (identidad, roles, perfil tributario)           ✅
       ↓ eventos
Estructura Organizacional (unidades de imputación)       ✅
       ↓ eventos
┌─────────────────────────────────────────────────┐
│  OXP  ←──→  Impuestos  ──→  Contabilidad  ──→  OXP  │   ✅ (ciclo transaccional F1)
│            (ciclo transaccional F1)             │
└─────────────────────────────────────────────────┘
```

**Estado al 2026-05-29:** la cadena de bloqueo quedó desbloqueada. Los 3 sub-dominios base están cerrados y los 3 transaccionales (OXP, Impuestos, Contabilidad) están listos para desarrollo F1.

---

## Orden de ejecución

### 1. Documento consolidado del ERP (v1 inicial)
Crear documento con los sub-dominios ya definidos (OXP, Impuestos, Contabilidad), sus componentes, relaciones de integración, dependencias y fases. Sirve como mapa para el equipo de desarrollo y diseño. Se actualizará al completar cada ítem siguiente.

- **Artefacto:** Nuevo documento en raíz del proyecto
- **Insumos:** Los 3 modelos de dominio existentes, integraciones/, anexos transversales
- **Estado:** ✅ Completado (2026-04-09)

---

### 2. Plataforma — Datos base
Definir catálogos estáticos: países, monedas, divisiones territoriales, tipos de documento, tasas de cambio. Es infraestructura pura, el más rápido de definir.

- **Artefacto:** `compartido/datos-referencia/definicion-alcance.md` o especificación de servicio
- **Bloquea a:** Todos los sub-dominios transaccionales
- **Actualizar:** Documento consolidado (ítem 1)
- **Estado:** ✅ Completado (2026-04-21) — alcance v1.0, especificación de servicio v1.0, anexo de estrategia Seed+Sync+Extend, catálogos precargados (países, monedas, tipos de documento, divisiones territoriales CO/DO/PA)

---

### 3. Terceros
Alcance + modelo de dominio completo. Registro unificado con roles, ciclo de vida, eventos EDA. Es el bloqueante más crítico (OXP, Impuestos y Contabilidad lo necesitan).

- **Artefactos:** `dominio/terceros/definicion-alcance.md`, `dominio/terceros/modelo-dominio.md`
- **Base existente:** `dominio/terceros/anexo-definicion-contexto-inicial.md`
- **Bloquea a:** OXP, Impuestos, Contabilidad
- **Actualizar:** Documento consolidado (ítem 1)
- **Estado:** ✅ Completado (2026-04-21) — alcance v1.0 (9 secciones, 11 términos, 26 reglas, 14 capacidades), modelo v1.0 (1 agregado + 1 entidad interna, 4 VOs, 18 eventos, 11 invariantes, 13 decisiones, FSM 4 estados). Auditoría de 10 skills (50 hallazgos) + 4 rondas de comité de POs aplicadas.

---

### 4. Estructura Organizacional
Alcance + modelo de dominio completo. Grupos, unidades, jerarquías, reestructuración. Segundo bloqueante (OXP y Contabilidad lo necesitan).

- **Artefactos:** `dominio/estructura-organizacional/definicion-alcance.md`, `dominio/estructura-organizacional/modelo-dominio.md`
- **Base existente:** `dominio/estructura-organizacional/anexo-definicion-contexto-inicial.md`
- **Bloquea a:** OXP, Contabilidad
- **Actualizar:** Documento consolidado (ítem 1)
- **Estado:** ✅ Completado (2026-05-28, commit `ca14031`) — alcance v1.2 (4 familias de flujos, 30 reglas, 23 permisos atómicos, multi-país y multi-moneda), modelo v1.4 (2 agregados raíz: GrupoOrganizacional y UnidadOrganizacional con FSMs de 2 y 5 estados; herencia dinámica del catálogo de tipos desde el grupo raíz; reestructuración como eventos de dominio de primera clase con respaldo IFRS 8), 14 decisiones + 4 heredadas, 16 invariantes, 18 eventos, 10 SIs, 3 domain services. Dos anexos arquitectónicos nuevos: `anexo-decisiones-arquitectonicas.md` (4 decisiones de arquitectura con benchmarks de ERPs líderes) y `anexo-orquestacion-creacion.md` (patrón BFF + estado Borrador).

---

### 5. Catálogo de conceptos OXP — Redefinición
Repensar cómo se definen los conceptos/productos/servicios que la empresa compra. Salir del CRUD tradicional, evaluar clasificación inteligente, aprendizaje, etc.

- **Archivo:** `dominio/obligaciones-por-pagar/modelo-dominio.md`
- **Actualizar:** Documento consolidado (ítem 1)
- **Estado:** ⚠️ Parcialmente abordado — el agregado `CatalogoGastoDirecto` (`[D21]`) y la clasificación inteligente de origen (`[D23]`, `[R36]`) están definidos en el modelo de OXP, con el patrón "cada dominio dueño de su catálogo + Impuestos como fuente fiscal" (`[D21]`). Pendiente revisar si el alcance original del ítem está cubierto o si requiere profundización adicional sobre patrones de aprendizaje y desambiguación.

---

### 6. Soportes de hechos económicos
Evaluar pros/contras de: servicio independiente de gestión documental vs. modelado dentro de OXP vs. híbrido. Decisión de arquitectura.

- **Artefacto:** Decisión de arquitectura + diseño según resultado
- **Actualizar:** Documento consolidado (ítem 1)
- **Estado:** ⬜ Pendiente

---

### 7. Esquema de ubicaciones OXP → Impuestos
Formalizar la estructura de ubicaciones que OXP envía a Impuestos para resolución de jurisdicción (Place of Supply) en el contrato D22/D9.

- **Archivos:** `dominio/obligaciones-por-pagar/modelo-dominio.md`, `dominio/impuestos/modelo-dominio.md`
- **Actualizar:** Documento consolidado (ítem 1)
- **Estado:** ⚠️ Parcialmente abordado — `direccionFiscal` formalizada en el contrato OXP → Impuestos (D22) y el anexo `dominio/impuestos/anexo-ejemplo-direccion-fiscal.md` documenta un ejemplo del esquema. Pendiente confirmar si el esquema cubre los tres países F1 (CO/DO/PA) y si hay zonas grises por país pendientes.

---

### 8. Integración OXP ↔ Contabilidad
Formalizar contrato bidireccional: líneas de traducción OXP → Contabilidad, respuesta EntregaAceptada → OXP. Con Terceros y Estructura Org ya definidos, el contrato queda completo.

- **Archivo:** `dominio/obligaciones-por-pagar/modelo-dominio.md`
- **Actualizar:** Documento consolidado (ítem 1)
- **Estado:** ✅ Completado (2026-05-29, commit `6cc7395` — PR #5 mergeado) — cierra los cuatro frentes de la integración: (1) aclaración del canal de amortización (D26 — la amortización viaja junto con la causación de la OXP de Comercio que la regulariza), (2) alineación terminológica SincoA&F → sistema contable (sub-dominio Contabilidad como gateway único, SincoA&F como destino físico legacy configurable), (3) mapeo OXP → tipoTransaccion contable (D27 — etiqueta semántica del hecho económico que viaja con cada causación; mapeo canónico evento → plantilla; identifica plantilla nueva `reversa_anticipo` #7 como coordinación cruzada con Contabilidad), (4) manejo de rechazos del sistema contable (D28 — OXP no modela el rechazo como evento de dominio; outbox pattern del consumidor en SI6 para durabilidad). Modelo v3.0 → v3.3, alcance v1.4 → v1.7. 28 decisiones, 6 SIs, sin cambios estructurales en agregados/FSMs/eventos.

---

### 9. Dependencias de infraestructura
Lista de dependencias transversales: autenticación, autorización, observabilidad, despliegue. Puede ser sección del documento consolidado o documento independiente.

- **Actualizar:** Documento consolidado (ítem 1)
- **Estado:** ⬜ Pendiente

---

### 10. Diseño UX por capas / responsabilidad
Estructura de navegación y responsabilidad: Home, Sub-dominios, Usuarios, Permisos.

- **Actualizar:** Documento consolidado (ítem 1)
- **Estado:** ⬜ Pendiente

---

## Lógica del orden

- **Documento consolidado primero** porque el equipo necesita contexto general ya, y se va enriqueciendo con cada ítem.
- **Plataforma antes de Terceros/Estructura Org** porque es rápido y ambos consumen datos base (países, tipos de documento, monedas).
- **Terceros antes de Estructura Org** porque Terceros bloquea a 3 sub-dominios (OXP, Impuestos, Contabilidad) mientras Estructura Org bloquea a 2 (OXP, Contabilidad).
- **Conceptos → Soportes → Ubicaciones → Integración** secuencial porque conceptos alimenta soportes (asociados al concepto), ubicaciones (del concepto) y líneas de traducción (que incluyen conceptos).
- **Infraestructura y UX al final** porque son transversales que no bloquean la definición de dominio, y se benefician de tener todo lo anterior resuelto.

---

## Verificación

Al completar cada ítem:
1. Confirmar con el usuario que el artefacto está completo
2. Actualizar el documento consolidado (ítem 1)
3. Actualizar la memoria del proyecto
4. Commit con la relación de cambios
5. Marcar como completado en este archivo (⬜ → ✅ + fecha)

---

## Logros adicionales no planeados originalmente

A lo largo de la ejecución surgieron y se cerraron piezas no contempladas en el plan original. Se listan aquí para mantener trazabilidad:

- **Direcciones (compartido)** ✅ — nuevo sub-dominio compartido con alcance v1.0 y modelo v1.0 (commit `309d325`, 2026-04-21). Integrado como dependencia base usada por Terceros e Impuestos.
- **Asistente de Onboarding (compartido)** ✅ — nuevo sub-dominio compartido con alcance v1.0 y modelo v1.0 + caso PUC v1.0 (commits `215ab19`, `326e7a2`, 2026-05-26). Transversal al ERP, primer caso de uso es el PUC contable.
- **Impuestos — multi-país F1 LatAm** ✅ — alcance v1.4 + modelo v2.0.4 con catálogos precargados, 2 auditorías y 2 rondas de refinamiento (commit `6b14d71`, 2026-05-26).
- **Contabilidad — Validación contractual del motor de traducción** ✅ — alcance v1.1 + modelo v1.1 (commit `d4b7e9c`, 2026-05-06).
- **Contabilidad — MarcoContable y arquitectura PUC** ✅ — alcance v1.2 + modelo v1.2 + nuevo anexo (commit `09328b4`, 2026-05-08).
- **Contabilidad — Caso PUC como servicio compartido** ✅ — alcance v1.2 → v1.3 + nuevo servicio compartido v1.0 (commit `215ab19`, 2026-05-26).
- **OXP — Causación contable del Anticipo** ✅ — alcance v1.3 → v1.4 + modelo v2.9 → v3.0 (commit `4769c33`, 2026-05-27). Cierre del hueco contable del Anticipo: D25 nueva, 5 → 7 estados, AnticipoConfirmado y AnticipoCausado eventos.
- **Plataforma — nueva skill `audit-contract-vs-internals`** ✅ — meta-auditor para validar coherencia entre contratos externos y flujos internos en cualquier sub-dominio (commit `326e7a2`, 2026-05-26).

---

## Resumen ejecutivo al 2026-05-29

- **Cerrados:** 5 de 10 ítems del plan original (1, 2, 3, 4, 8) + 8 logros adicionales no planeados.
- **Parcialmente abordados:** 2 ítems (5 catálogo de conceptos OXP, 7 esquema de ubicaciones OXP → Impuestos) que requieren confirmación de cierre o profundización.
- **Pendientes:** 3 ítems (6 soportes de hechos económicos, 9 dependencias de infraestructura, 10 diseño UX por capas).
- **Cadena de bloqueo:** desbloqueada. Los 3 sub-dominios transaccionales F1 (OXP, Impuestos, Contabilidad) están listos para desarrollo.
