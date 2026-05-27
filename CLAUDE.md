# ERP Definiciones

## Propósito

Definición de los sub-dominios de un ERP mediante documentos de diseño conversacional con IA. El objetivo es producir artefactos de dominio lo suficientemente refinados para minimizar zonas grises en la interpretación del equipo de desarrollo. Dominio = ERP (el problema completo). Sub-dominio = cada módulo (OXP, CXC, etc.).

## Sub-dominios

| Carpeta | Sub-dominio | Estado |
|---------|-------------|--------|
| `dominio/obligaciones-por-pagar/` | Obligaciones por Pagar | En refinamiento (Fase 2) |
| `dominio/impuestos/` | Impuestos | Modelo v1.3 completo |
| `dominio/contabilidad/` | Contabilidad (Nivel 1: Motor de Traducción + Nivel 2: Sistema contable) | Alcance v1.0, Modelo v1.0 — Listo para desarrollo (F1) |
| `dominio/terceros/` | Terceros | Alcance v1.0, Modelo v1.0 — Listo para desarrollo |
| `dominio/estructura-organizacional/` | Estructura Organizacional | Definición inicial |
| `compartido/datos-referencia/` | Datos de Referencia (catálogos base del ERP) | Alcance v1.0, Especificación v1.0 — Listo para desarrollo |
| `compartido/asistente-onboarding/` | Asistente de Onboarding (caso PUC en v1.0; transversal a otros casos futuros) | Alcance v1.0, Modelo v1.0, Caso PUC v1.0 — Listo para desarrollo (F1) |
| *(pendiente)* | Tesorería | No iniciado |
| *(pendiente)* | Emisión Electrónica | No iniciado |
| *(pendiente)* | Recepción Electrónica | No iniciado |

## Artefactos por sub-dominio

Cada sub-dominio produce 3 artefactos:

1. **definicion-alcance.md** — El *qué*: alcance funcional, glosario canónico, reglas de negocio.
2. **modelo-dominio.md** — El *cómo*: agregados, eventos, invariantes, FSM, domain services (DDD/ES/EDA).
3. **EventCatalog** — Representación visual del modelo de dominio (pendiente, Fase 3).

## Flujo de trabajo

### Fase 1 — Alcance (conversacional)
Conversación con IA para construir `definicion-alcance.md`: necesidades del negocio, glosario, reglas, premisas.

### Fase 2 — Modelo de dominio (conversacional + auditoría)
Conversación con IA para construir `modelo-dominio.md` a partir del alcance. Proceso iterativo: cada cambio se confirma manualmente. Al llegar a un punto maduro, ejecutar `/audit <archivo>` para auditoría completa (10 skills). Los hallazgos se revisan uno a uno y se aplican o descartan con el usuario.

### Fase 3 — EventCatalog (pendiente)
Skills especializadas para generar EventCatalog desde el modelo de dominio. No iniciado.

## Estructura de directorios

| Directorio | Propósito |
|------------|-----------|
| `dominio/` | Bounded contexts de negocio. Cada sub-dominio con su alcance y modelo. |
| `compartido/` | Servicios compartidos del application plane que no son dominio de negocio. |
| `integraciones/entre-dominios/` | Contratos de integración entre sub-dominios propios. |
| `integraciones/externas/` | Conectores y contratos con sistemas de terceros. |
| `plataforma-saas/` | Control plane (futuro): tenant management, identity, billing, admin. |
| `plantillas/` | Plantillas base para crear nuevos sub-dominios y servicios. |
| `guias-de-modelado/` | Criterios generales de modelado (aplican a todos los sub-dominios). |
| `fuentes/` | Material de referencia externo (PDFs, papers). |
| `auditoria/` | Reportes generados por las skills de auditoría. |

## Convenciones

- **Idioma:** Español Colombia, tanto en documentos como en conversación.
- **Naming:** kebab-case para carpetas y archivos (ej: `dominio/obligaciones-por-pagar/modelo-dominio.md`).
- **Estructura:** Una carpeta por sub-dominio dentro de `dominio/` con sus artefactos dentro.
- **Plantillas:** Al crear un nuevo sub-dominio, usar como base las plantillas en `plantillas/`.
- **Edición:** Nunca aplicar cambios sin confirmación del usuario. Presentar el cambio, esperar aprobación, luego aplicar.
- **Skills y commands:** Seguir EXACTAMENTE las especificaciones de `.claude/commands/` y `.claude/skills/`.
- **Auditoría:** Usar `/audit <archivo>` para auditoría completa o `/audit <archivo> <skill>` para skill individual.
