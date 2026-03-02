# ERP Definiciones

## Propósito

Definición de los sub-dominios de un ERP mediante documentos de diseño conversacional con IA. El objetivo es producir artefactos de dominio lo suficientemente refinados para minimizar zonas grises en la interpretación del equipo de desarrollo. Dominio = ERP (el problema completo). Sub-dominio = cada módulo (OXP, CXC, etc.).

## Sub-dominios

| Carpeta | Sub-dominio | Estado |
|---------|-------------|--------|
| `obligaciones-por-pagar/` | Obligaciones por Pagar | En refinamiento (Fase 2) |
| *(pendiente)* | Facturación | No iniciado |
| *(pendiente)* | Contabilidad | No iniciado |
| *(pendiente)* | Tesorería | No iniciado |
| *(pendiente)* | Emisión y Recepción Electrónica | No iniciado |
| *(pendiente)* | Impuestos | No iniciado |

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
| `obligaciones-por-pagar/` | Sub-dominio OXP: alcance y modelo de dominio. |
| `plantillas/` | Plantillas base para crear nuevos sub-dominios. Actualizar al incorporar nuevas secciones. |
| `guias-de-modelado/` | Criterios generales de modelado (aplican a todos los sub-dominios). |
| `integraciones/` | Contratos de eventos entre sub-dominios (Fase 3). |
| `fuentes/` | Material de referencia externo (PDFs, papers). |
| `auditoria/` | Reportes generados por las skills de auditoría. |

## Convenciones

- **Idioma:** Español Colombia, tanto en documentos como en conversación.
- **Naming:** kebab-case para carpetas y archivos (ej: `obligaciones-por-pagar/modelo-dominio.md`).
- **Estructura:** Una carpeta por sub-dominio con sus artefactos dentro.
- **Plantillas:** Al crear un nuevo sub-dominio, usar como base las plantillas en `plantillas/`.
- **Edición:** Nunca aplicar cambios sin confirmación del usuario. Presentar el cambio, esperar aprobación, luego aplicar.
- **Skills y commands:** Seguir EXACTAMENTE las especificaciones de `.claude/commands/` y `.claude/skills/`.
- **Auditoría:** Usar `/audit <archivo>` para auditoría completa o `/audit <archivo> <skill>` para skill individual.
