---
name: issues-crear
description: "Crea issues en GitHub para los sub-dominios del ERP siguiendo la convención de etiquetas del proyecto (subdominio: <nombre> + tipo: <clase>) y una estructura de cuerpo estándar. Úsalo cuando el usuario pida crear uno o varios issues, registrar pendientes o tareas de un sub-dominio, o mencione etiquetas, milestones o gestión de issues en GitHub."
disable-model-invocation: false
user-invocable: true
allowed-tools: Bash, Read
---

# Crear Issues de Sub-dominio

Crea issues en GitHub para los sub-dominios del ERP de forma consistente: aplica la convención de etiquetas del proyecto y una estructura de cuerpo estándar, para que los pendientes queden clasificables y filtrables por sub-dominio y por tipo de trabajo.

## Convención de etiquetas

Las etiquetas usan **prefijo con dos puntos** para simular categorías dentro de la lista plana de etiquetas de GitHub. El prefijo agrupa las etiquetas al ordenar alfabéticamente, hace el autocompletado de filtros predecible (`label:"subdominio:` muestra solo sub-dominios) y evita choques de nombres entre ejes.

Hay dos ejes obligatorios por issue:

| Eje | Formato | Color | Ejemplo |
|-----|---------|-------|---------|
| **Sub-dominio** | `subdominio: <nombre>` | `1d76db` (azul) | `subdominio: contabilidad`, `subdominio: impuestos`, `subdominio: oxp` |
| **Tipo de trabajo** | `tipo: <clase>` | `fbca04` (amarillo) | `tipo: refinamiento` |

Notas:
- Se usa **`subdominio`**, no `dominio`, por coherencia con el glosario del proyecto (CLAUDE.md): el *dominio* es el ERP completo; cada módulo (OXP, Contabilidad, Impuestos, etc.) es un *sub-dominio*.
- El nombre del sub-dominio va en minúscula y kebab-case, alineado con la carpeta en `dominio/` o `compartido/` (ej: `estructura-organizacional`, `datos-referencia`).
- `tipo: refinamiento` cubre ajustes de alcance o de modelo de dominio. Si surge otra clase de trabajo, crear una nueva etiqueta con el mismo patrón (`tipo: <clase>`) en lugar de reusar las etiquetas por defecto de GitHub.

## Estructura de cuerpo estándar

Todo issue se crea con este cuerpo mínimo; las secciones se completan al refinar:

```markdown
## Contexto
Sub-dominio: <Nombre> (`dominio/<carpeta>/` o `compartido/<carpeta>/`)

## Qué se pide
<descripción del título, ampliada en 1-2 líneas>

## Artefactos afectados (por confirmar)
- [ ] definicion-alcance.md
- [ ] modelo-dominio.md
- [ ] datos-precargados / plantilla

## Criterio de aceptación
_A definir al refinar._
```

## Procedimiento

1. **Verificar el repositorio y las etiquetas existentes:**
   ```bash
   gh repo view --json nameWithOwner
   gh label list --limit 100
   ```
2. **Crear las etiquetas que falten** (idempotente — si ya existen, `gh` devuelve error y se ignora):
   ```bash
   gh label create "subdominio: <nombre>" --color "1d76db" --description "Issues del sub-dominio <Nombre>"
   gh label create "tipo: refinamiento"   --color "fbca04" --description "Refinamiento de alcance o modelo de dominio"
   ```
3. **Preparar los borradores** (título + cuerpo estándar) y **presentarlos al usuario para aprobación antes de crear** (regla del proyecto: nunca aplicar cambios sin confirmación).
4. **Crear cada issue** con sus dos etiquetas:
   ```bash
   gh issue create \
     --title "<título>" \
     --label "subdominio: <nombre>" --label "tipo: <clase>" \
     --body "<cuerpo estándar>"
   ```
5. **Reportar** los números y URLs de los issues creados en una tabla.

## Reglas

- **Idioma:** español Colombia, en títulos y cuerpos.
- **Confirmación:** presentar los borradores y esperar aprobación antes de crear issues o etiquetas.
- **Un issue por tema:** cada pendiente es un issue independiente, aunque compartan contexto de sub-dominio.
- **Escalabilidad:** al trabajar un nuevo sub-dominio, replicar el patrón creando su etiqueta `subdominio: <nombre>`.
- **Opcional — milestone:** si el usuario quiere agrupar varios issues bajo un hito, crear un milestone (ej: "Refinamiento Contabilidad") con `gh api repos/{owner}/{repo}/milestones -f title="..."` y asignarlo con `--milestone` al crear cada issue.
