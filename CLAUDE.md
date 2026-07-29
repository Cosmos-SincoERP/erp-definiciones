# ERP Definiciones

## Propósito

Definición de los sub-dominios de un ERP mediante documentos de diseño conversacional con IA. El objetivo es producir artefactos de dominio lo suficientemente refinados para minimizar zonas grises en la interpretación del equipo de desarrollo. Dominio = ERP (el problema completo). Sub-dominio = cada módulo (OXP, CXC, etc.).

## Sub-dominios

| Carpeta | Sub-dominio | Estado |
|---------|-------------|--------|
| `dominio/obligaciones-por-pagar/` | Obligaciones por Pagar (OXP) | Alcance v1.15, Modelo v4.3 — Fase 2 (refinamiento continuo) |
| `dominio/impuestos/` | Impuestos | Alcance v1.5, Modelo v2.0.6 — Listo para desarrollo (F1); multi-país CO/DO/PA + catálogos precargados |
| `dominio/contabilidad/` | Contabilidad (Nivel 1: Motor de Traducción + Nivel 2: Sistema contable) | Alcance v1.10, Modelo v1.9 — N1 listo para desarrollo (F1); N2 (F2) |
| `dominio/terceros/` | Terceros (bodega consolidadora — replanteamiento #31) | Alcance v2.0, Modelo v2.0.2 — listo para desarrollo (F1) |
| `dominio/estructura-organizacional/` | Estructura Organizacional | Alcance v1.4, Modelo v1.6 — replanteamiento #45 (copia local + diferir + señal), listo para desarrollo (F1) |
| `compartido/datos-referencia/` | Datos de Referencia (catálogos base del ERP) | Alcance v2.0 — replanteamiento jun-2026 (catálogos como Nuggets + tasas de cambio) |
| `compartido/nuggets/` | Nuggets (value objects transversales empaquetados: identificación legal, dirección, contacto, país, moneda, etc.) | Catálogo + gobernanza — 8 nuggets aceptados (replanteamiento #31) |
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

### Refinamiento — issue-driven (post-auditoría)
Una vez el modelo queda cerrado y auditado (fin de la Fase 2), el sub-dominio entra en **refinamiento**: ajustes y pendientes postergados que surgen de consultas del equipo de desarrollo o de cruces entre sub-dominios. **El punto de corte es el fin de la Auditoría:** antes se edita el `.md` en conversación; después cada cambio se maneja como **issue** (`subdominio: <nombre>` + `tipo: refinamiento`) → rama → PR que cierra el issue. Usar la skill `issues-crear`. Un cambio cruzado vive en el sub-dominio que lo origina; su PR puede tocar más de uno.

### Fase 3 — EventCatalog (pendiente)
Skills especializadas para generar EventCatalog desde el modelo de dominio. No iniciado.

## Estructura de directorios

| Directorio | Propósito |
|------------|-----------|
| `dominio/` | Bounded contexts de negocio. Cada sub-dominio con su alcance y modelo. |
| `compartido/` | Servicios compartidos del application plane que no son dominio de negocio: `datos-referencia/`, `nuggets/` (value objects transversales empaquetados), `asistente-onboarding/`. |
| `integraciones/entre-dominios/` | Contratos de integración entre sub-dominios propios. |
| `integraciones/externas/` | Conectores y contratos con sistemas de terceros. |
| `plataforma-saas/` | Control plane (futuro): tenant management, identity, billing, admin. |
| `plantillas/` | Plantillas base para crear nuevos sub-dominios y servicios. |
| `guias-de-modelado/` | Criterios generales de modelado (aplican a todos los sub-dominios): `arquitectura-eda.md`, `modelar-agregados.md`, `separacion-responsabilidades.md`, `datos-entre-dominios.md` (dato con dueño único consumido por copia local entre dominios), `topologia-equipos-despliegue.md`. |
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
