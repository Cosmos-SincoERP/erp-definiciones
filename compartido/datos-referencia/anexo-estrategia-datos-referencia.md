# Anexo — Estrategia de datos de referencia

> **Fecha:** Abril 2026
> **Propósito:** Documentar la decisión sobre cómo se gestionan los catálogos de datos de referencia del ERP: carga inicial, sincronización, extensión y validación.

---

## Decisión

Los catálogos de datos de referencia se gestionan con una estrategia **híbrida (Seed + Sync + Extend)**, alineada con la práctica de la industria de ERPs y plataformas SaaS.

---

## Estrategia: Seed + Sync + Extend

### 1. Seed — Carga inicial desde archivos JSON

Los catálogos se preconstruyen como archivos JSON dentro del repositorio (`compartido/datos-referencia/catalogos/`). Estos archivos son la **fuente de verdad** para la carga inicial de datos en cualquier ambiente (desarrollo, staging, producción).

Al momento de la implementación, los scripts de seed del framework (Entity Framework, Prisma, Sequelize, etc.) consumen estos JSON directamente como input, sin reinterpretación manual.

**Archivos disponibles:**

| Archivo | Registros | Fuente original |
|---------|:---------:|-----------------|
| `paises.json` | 195 | ISO 3166-1 |
| `monedas.json` | 154 | ISO 4217 |
| `tipos-documento-identidad.json` | 45 | Legislación por país (CO, DO, PA, MX, CL, PE, EC, AR, BR) |
| `divisiones-territoriales-co.json` | 1.188 | DIVIPOLA — DANE Colombia |
| `divisiones-territoriales-do.json` | 221 | División territorial oficial RD |
| `divisiones-territoriales-pa.json` | 108 | División territorial oficial PA |

**Beneficios del seed desde JSON:**
- El desarrollador no decide qué datos cargar — los toma del archivo.
- Los atributos ya reflejan las necesidades de los dominios consumidores.
- Los archivos son versionables en git — cualquier cambio queda en el historial.
- Son agnósticos al framework — cualquier ORM puede consumirlos.

### 2. Sync — Sincronización periódica con fuentes externas

Algunos catálogos requieren actualización periódica desde fuentes oficiales.

| Catálogo | Fuente | Frecuencia | Mecanismo |
|----------|--------|------------|-----------|
| Tasas de cambio CO | Banco de la República | Diaria | API o scraping programado |
| Tasas de cambio DO | Banco Central RD | Diaria | API o scraping programado |
| Divisiones territoriales CO | DIVIPOLA (DANE) | Anual o cuando haya actualización | Descarga manual + actualización del JSON |

**Fallback:** Si la sincronización automática falla, el administrador puede cargar los datos manualmente.

**Nota:** Las tasas de cambio no se preconstruyen en JSON porque son datos que cambian diariamente. Se cargan exclusivamente por sincronización o carga manual.

### 3. Extend — Extensión por el administrador

Todos los catálogos son extensibles. El administrador puede:
- Agregar un nuevo país de operación (requiere crear divisiones territoriales y tipos de documento asociados).
- Agregar divisiones territoriales para un nuevo municipio (ej: habilitar ICA/RICA en una nueva ciudad).
- Agregar tipos de documento para un nuevo país.
- Agregar monedas no estándar si el negocio lo requiere.

Los datos agregados por el administrador coexisten con los datos precargados sin conflicto.

---

## Referencia de la industria

### Cómo lo hacen otros

| Plataforma | Estrategia |
|------------|-----------|
| **SAP / Odoo** | Datos locales persistidos + sincronización de tasas de cambio desde APIs externas |
| **Stripe / Shopify** | API-first con caché local para performance |
| **ERPs LatAm (Siigo, Alegra)** | Datos precargados por país, extensibles por admin |

### Fuentes públicas utilizadas

| Fuente | Qué ofrece | Cómo la usamos |
|--------|-----------|----------------|
| **ISO 3166-1** | Códigos de países (2 letras) | Estándar para `paises.json` |
| **ISO 4217** | Códigos de monedas (3 letras) | Estándar para `monedas.json` |
| **DIVIPOLA (DANE)** | Códigos de departamentos y municipios de Colombia | Fuente para `divisiones-territoriales-co.json` |
| **REST Countries API** | Datos completos de países (gratuito, sin autenticación) | Referencia para validar datos |
| **GeoNames** | 25M+ ubicaciones con divisiones administrativas | Referencia para divisiones territoriales |
| **CLDR (Unicode)** | Formatos locales (números, fechas, monedas) | Referencia para formatos de presentación |
| **Banco de la República** | TRM diaria Colombia | Fuente para sincronización de tasas de cambio |
| **Banco Central RD** | Tasas de cambio República Dominicana | Fuente para sincronización de tasas de cambio |
| **Open Exchange Rates / Fixer.io** | Tasas de cambio globales (API de pago) | Alternativa para monedas sin fuente oficial directa |

---

## Consideraciones para el equipo de desarrollo

1. **Los JSON son el contrato de datos.** La estructura de atributos (nombres, tipos, relaciones) que definen estos archivos es la que debe reflejar el modelo de datos en la base de datos.
2. **No modificar los JSON manualmente sin actualizar el documento de alcance.** Cualquier cambio en estructura (nuevo atributo, cambio de tipo) debe reflejarse en `definicion-alcance.md`.
3. **Los scripts de seed deben ser idempotentes.** Ejecutar el seed dos veces no debe duplicar datos ni fallar.
4. **Divisiones territoriales se separan por país** porque cada país tiene estructura jerárquica diferente (Colombia usa DIVIPOLA, RD usa códigos de provincia, Panamá usa distritos).

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Abril 2026 | Versión inicial. Estrategia Seed + Sync + Extend. 6 archivos JSON preconstruidos. |
