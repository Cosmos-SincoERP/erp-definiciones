# Anexo — Decisión transversal: Internacionalización (i18n) y Localización (l10n)

> **Fecha:** Abril 2026
> **Alcance:** Aplica a todos los catálogos de todos los servicios y dominios del ERP.
> **Propósito:** Establecer las reglas transversales para el manejo de traducción de textos (i18n), datos por país (l10n) y convenciones de codificación en catálogos.

---

## 1. Definiciones

### ¿Qué son i18n y l10n?

Las abreviaturas **i18n** y **l10n** son convenciones de la industria del software para referirse a internacionalización y localización. Se escriben así porque:

- **i18n** = **i** + 18 letras + **n** (internationalizatio**n**) — hay 18 letras entre la "i" y la "n"
- **l10n** = **l** + 10 letras + **n** (localizatio**n**) — hay 10 letras entre la "l" y la "n"

Estos términos fueron popularizados por empresas como IBM, Sun Microsystems y el W3C (World Wide Web Consortium) en los años 90, y hoy son estándar en toda la industria del software. La distinción entre ambos conceptos es fundamental para cualquier sistema que opere en múltiples países o idiomas.

**Fuentes de referencia:**
- [W3C — Localization vs Internationalization](https://www.w3.org/International/questions/qa-i18n)
- [Mozilla L10N — i18n vs l10n: What's the Diff?](https://blog.mozilla.org/l10n/2011/12/14/i18n-vs-l10n-whats-the-diff/)
- [Unicode CLDR — Common Locale Data Repository](https://cldr.unicode.org/)

### Internacionalización (i18n)
Capacidad del sistema para presentar la misma información en diferentes idiomas. El dato es el mismo, solo cambia cómo se muestra al usuario.

**Ejemplo:** El código `COP` se muestra como "Peso Colombiano" en español o "Colombian Peso" en inglés. El dato no cambia — cambia la presentación.

### Localización (l10n)
Adaptación del sistema a un país o región específica mediante datos completamente diferentes. No es traducción — son registros distintos con validaciones, formatos y reglas propias.

**Ejemplo:** Colombia tiene NIT como tipo de documento de identidad para empresas. México tiene RFC. No es que NIT se traduzca a RFC — son conceptos distintos con formatos y validaciones diferentes.

---

## 2. Clasificación de catálogos

Cada catálogo del ERP se clasifica según su naturaleza:

### Catálogos de traducción (i18n)

Son catálogos donde el dato es universal — el código es el mismo en cualquier país. Solo cambia el nombre visible según el idioma del usuario.

| Catálogo | Código (universal) | Traducción (responsabilidad del frontend) |
|----------|-------------------|------------------------------------------|
| Países | `CO` | "Colombia" (es), "Colombia" (en), "Colômbia" (pt) |
| Monedas | `COP` | "Peso Colombiano" (es), "Colombian Peso" (en) |
| Tipos de dirección | `FSC` | "Fiscal" (es), "Tax Address" (en) |
| Tipos de complemento | `APT` | "Apartamento" (es_CO), "Departamento" (es_MX), "Apartment" (en) |

**Regla:** El backend almacena el código. El frontend traduce según el idioma y locale del usuario. Los catálogos incluyen un nombre por defecto en español como referencia, pero la presentación final es responsabilidad de la capa de presentación.

### Catálogos de localización (l10n)

Son catálogos donde cada país tiene datos completamente diferentes. No es traducción — son registros distintos.

| Catálogo | Dato por país | Ejemplo |
|----------|--------------|---------|
| Tipos de documento de identidad | Cada país tiene sus propios tipos | CO: NIT, CC, CE. MX: RFC, CURP. DO: RNC, CIE |
| Divisiones territoriales | Cada país tiene su propia estructura jerárquica | CO: departamentos + municipios (DIVIPOLA). DO: provincias + municipios |
| Tipos de vía | Cada país que lo exige tiene su propio catálogo oficial | CO: CL, CR, DG, TV (catálogo DIAN). Otros países pueden no tener |
| Códigos postales | Cada país tiene su propio catálogo | CO: 3.685 códigos (DIAN/4-72). MX: ~145.000 (SAT/SEPOMEX) |
| Formatos de dirección | Cada país define qué campos son obligatorios | CO: tipo de vía obligatorio. DO: texto libre. MX: colonia obligatoria |

**Regla:** Cada país tiene sus propios registros en el catálogo. Se almacenan separados (por archivo o por campo `paisCodigo`). No se traducen — son datos diferentes.

### Catálogos mixtos

Algunos catálogos tienen datos que varían por país Y además necesitan traducción.

| Catálogo | Componente l10n | Componente i18n |
|----------|----------------|-----------------|
| Tipos de complemento | Algunos complementos solo existen en ciertos países (ej: "Conjunto" solo en Colombia) | El nombre del complemento se traduce según idioma (ej: "Apartamento" / "Apartment") |

**Regla:** Se maneja como l10n (el catálogo indica en qué países aplica cada registro) + i18n (el nombre se traduce en el frontend).

---

## 3. Dónde vive cada responsabilidad

| Responsabilidad | Dónde vive | Ejemplo |
|----------------|-----------|---------|
| Códigos de catálogo | Backend (base de datos) | `CO`, `COP`, `FSC`, `APT` |
| Datos por país (l10n) | Backend (base de datos, separados por país) | NIT solo existe para `paisCodigo: CO` |
| Traducción de nombres (i18n) | Frontend (capa de presentación) | `APT` → "Apartamento" (es_CO) / "Departamento" (es_MX) |
| Nombre por defecto | Backend (campo de referencia en el catálogo) | Nombre en español como referencia para desarrollo y administración |

---

## 4. Convención de codificación de catálogos

### Cuando existe estándar internacional

Se adopta el estándar sin modificación.

| Estándar | Aplica a | Formato | Ejemplo |
|----------|---------|---------|---------|
| ISO 3166-1 alpha-2 | Países | 2 letras mayúsculas | `CO`, `MX`, `US` |
| ISO 4217 | Monedas | 3 letras mayúsculas | `COP`, `USD`, `EUR` |
| DIAN nomenclatura | Tipos de vía Colombia | 2-3 letras mayúsculas | `CL`, `CR`, `DG`, `TV`, `AC`, `AK` |
| DIVIPOLA | Divisiones territoriales Colombia | Numérico | `05`, `05001` |

### Cuando no existe estándar internacional

Se define una convención propia con las siguientes reglas:

| Regla | Descripción |
|-------|-------------|
| **Longitud** | 3 letras mayúsculas |
| **Idioma base** | Español (región LatAm) — mnemónico del término en español |
| **Formato** | Solo letras, sin números, sin caracteres especiales |
| **Unicidad** | El código debe ser único dentro de su catálogo |
| **Mnemónico** | El código debe ser reconocible a partir del término que representa |

**Ejemplos aplicados:**

| Catálogo | Código | Término |
|----------|--------|---------|
| Tipos de dirección | `FSC` | Fiscal |
| Tipos de dirección | `COM` | Comercial |
| Tipos de dirección | `COR` | Correspondencia |
| Tipos de dirección | `ENT` | Entrega |
| Tipos de dirección | `SUC` | Sucursal |
| Tipos de complemento | `APT` | Apartamento |
| Tipos de complemento | `TRR` | Torre |
| Tipos de complemento | `PIS` | Piso |
| Tipos de complemento | `OFC` | Oficina |
| Tipos de complemento | `LOC` | Local |
| Tipos de complemento | `BDG` | Bodega |

### Referencia cruzada con estándares internacionales

Cuando un código propio tiene equivalente en un estándar internacional (ej: USPS), se documenta la referencia para interoperabilidad.

| Código propio | Término | Equivalente USPS | Equivalente vCard |
|--------------|---------|:-----------------:|:-----------------:|
| `APT` | Apartamento | APT (Apartment) | — |
| `PIS` | Piso | FL (Floor) | — |
| `OFC` | Oficina | OFC (Office) | — |
| `EDF` | Edificio | BLDG (Building) | — |
| `UND` | Unidad | UNIT (Unit) | — |
| `FSC` | Fiscal | — | — |
| `COM` | Comercial | — | WORK |

---

## 5. Traducción por locale

Para catálogos i18n, la traducción se resuelve por **locale** (país + idioma), no solo por idioma. Esto es necesario porque en LatAm todos hablan español pero usan términos diferentes.

### Prioridad de resolución

1. Locale específico (`es_MX`) — si existe traducción para ese locale, se usa
2. Idioma general (`es`) — si no existe locale específico, se usa el idioma base
3. Nombre por defecto — si no existe traducción, se usa el nombre por defecto del catálogo

### Ejemplo

Para el código `APT` (Apartamento):

| Locale | Nombre mostrado |
|--------|----------------|
| `es_CO` | Apartamento |
| `es_MX` | Departamento |
| `es_AR` | Departamento |
| `en_US` | Apartment |
| `pt_BR` | Apartamento |
| `es` (genérico) | Apartamento |

**Responsabilidad:** El mecanismo de traducción (archivos de traducción, servicio i18n, CLDR) es decisión del equipo de desarrollo. Este documento solo establece que la traducción se resuelve por locale y que el backend no es responsable de almacenar traducciones.

---

## 6. Aplicación a catálogos existentes

### Catálogos que no necesitan cambio

| Catálogo | Razón |
|----------|-------|
| Divisiones territoriales (CO, DO, PA) | l10n puro — datos por país, nombres geográficos no se traducen |
| Tipos de documento de identidad | l10n puro — datos por país, ya separados por `paisCodigo` |
| Tipos de vía Colombia | l10n puro — catálogo DIAN específico de Colombia |

### Catálogos que incluyen nombre por defecto como referencia

| Catálogo | Campo de código | Campo de referencia | Traducción final |
|----------|----------------|--------------------|-----------------| 
| Países | `codigo` (ISO) | `nombre` (español, referencia) | Frontend por locale |
| Monedas | `codigo` (ISO) | `nombre` (español, referencia) | Frontend por locale |
| Tipos de dirección | `codigo` (convención propia) | `nombre` (español, referencia) | Frontend por locale |
| Tipos de complemento | `codigo` (convención propia) | `nombre` (español, referencia) | Frontend por locale |

El campo `nombre` en estos catálogos es una referencia para desarrollo y administración, no la versión final que ve el usuario.

---

## 7. Referencia de la industria

| ERP/Estándar | Enfoque i18n | Enfoque l10n |
|-------------|-------------|-------------|
| **SAP** | Tabla de textos separada por idioma (SE63) | Country packages con datos y procesos por país |
| **Odoo** | Archivos .po (gettext) por locale | Módulos l10n_XX con Chart of Accounts, impuestos, reportes por país |
| **Dynamics 365** | Metadatos de traducción por idioma | Regulatory features + Competitive features por país |
| **CLDR (Unicode)** | Estándar global de traducciones por locale | Formatos de dirección, postal, números por país |
| **USPS** | — | Códigos de complemento (APT, STE, FL) específicos de USA |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Abril 2026 | Versión inicial. Clasificación i18n/l10n, convención de codificación (3 letras mayúsculas mnemónicas en español), prioridad de resolución por locale, aplicación a catálogos existentes. |
