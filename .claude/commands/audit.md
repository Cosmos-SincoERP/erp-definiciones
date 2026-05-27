Ejecuta una auditoría sobre el modelo de dominio especificado en `$ARGUMENTS`.

**Formato de `$ARGUMENTS`:** `<archivo> [skill]`

- Si solo se pasa el archivo (ej: `dominio/obligaciones-por-pagar/modelo-dominio.md`), ejecutar la auditoría completa (`/audit-full`): las 11 skills en secuencia lógica según lo definido en la skill `audit-full`.
- Si se pasa archivo + nombre de skill (ej: `dominio/obligaciones-por-pagar/modelo-dominio.md glossary`), ejecutar solo esa skill individual. Los nombres válidos de skill son:
  - `glossary` → `/audit-structure-glossary`
  - `composition` → `/audit-structure-composition`
  - `state-machines` → `/audit-structure-state-machines`
  - `invariants` → `/audit-structure-invariants`
  - `responsibilities` → `/audit-behavior-responsibilities`
  - `event-semantics` → `/audit-behavior-event-semantics`
  - `contract-vs-internals` → `/audit-behavior-contract-vs-internals`
  - `idempotency` → `/audit-behavior-idempotency`
  - `sagas` → `/audit-process-sagas`
  - `open-decisions` → `/audit-quality-open-decisions`
  - `sanity-check` → `/audit-quality-sanity-check`

**Procedimiento:**

1. Leer el archivo completo del modelo de dominio indicado.
2. Ejecutar la(s) skill(s) correspondiente(s) siguiendo EXACTAMENTE el procedimiento, formato de salida y protocolo definido en cada SKILL.md.
3. Si es auditoría completa, consolidar el reporte unificado según el formato de `/audit-full`.
