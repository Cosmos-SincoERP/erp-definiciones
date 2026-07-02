# Nugget `DireccionFisica` — Especificación

| | |
|---|---|
| **Estado** | En especificación — borrador para revisión |
| **Versión** | 0.2 |
| **Gobernanza** | [gobernanza-nuggets.md](../gobernanza-nuggets.md) |
| **Catálogo** | [catalogo-nuggets.md](../catalogo-nuggets.md) |
| **Hereda de** | Servicio de Direcciones v1.0 (eliminado en el replanteamiento de jun-2026; conservado en el historial del repositorio) — ver Sección 8 |

## Tabla de contenido

1. [Concepto](#sección-1-concepto)
2. [Atributos](#sección-2-atributos)
3. [Igualdad y normalización](#sección-3-igualdad-y-normalización)
4. [Reglas de validación](#sección-4-reglas-de-validación)
5. [Operaciones](#sección-5-operaciones)
6. [Datos embebidos](#sección-6-datos-embebidos)
7. [Ejemplos por país](#sección-7-ejemplos-por-país)
8. [Herencia del servicio de Direcciones](#sección-8-herencia-del-servicio-de-direcciones)
9. [Fuera de responsabilidad](#sección-9-fuera-de-responsabilidad)
10. [Consumidores](#sección-10-consumidores)
11. [Revisión pendiente](#sección-11-revisión-pendiente)

---

## Sección 1: Concepto

La **Dirección Física** es la dirección de un lugar en el territorio de un país: la división territorial estructurada (departamento/provincia → municipio/distrito) más la descripción de la ubicación dentro de ella (la línea de dirección), complementada con el código postal cuando el país lo usa.

**Por qué "física":** distingue la dirección de un lugar frente a la electrónica (correo), usando el calificador que el lenguaje natural de los países de operación ya emplea (criterio 3 de nomenclatura de la gobernanza). Cubre todos los usos del ERP: fiscal, comercial, de sedes, de entrega y de correspondencia — el *uso* es una relación del consumidor con la dirección, no parte de la dirección (Sección 9).

**El corte estructurado/libre que gobierna el diseño:**

- **La división territorial va estructurada** (validada contra el catálogo embebido) porque tiene consecuencias de negocio: Impuestos resuelve la jurisdicción del ICA por municipio, y la facturación electrónica la exige contra tablas oficiales (DIAN: departamento y municipio según DANE, reglas FAJ11/FAJ12 del Anexo Técnico v1.9; DGI Panamá: provincia y distrito).
- **La descripción urbana va en una línea de texto** porque así la reciben las autoridades (DIAN FE, regla FAJ14: texto libre de 1–300 caracteres, "en lugar de utilizar elementos estructurados") y los humanos. En los países con nomenclatura codificada (Colombia), el Nugget ofrece además una **captura estructurada opcional** (tipo de vía + números + complementos) que compone la línea canónica — hereda los catálogos del servicio de Direcciones sin convertir la estructura en requisito.

**Origen del concepto:** servicio de Direcciones v1.0 (abril 2026), convertido en Nugget por el replanteamiento de junio 2026. La estructura de datos, los perfiles por país y los catálogos se heredan; la persistencia centralizada, la identidad y los eventos de sincronización desaparecen (Sección 8).

---

## Sección 2: Atributos

| Atributo | Tipo | Obligatorio | Descripción |
|----------|------|:-----------:|-------------|
| `pais` | string (ISO 3166-1 alfa-2) | Sí | País de la dirección. |
| `divisionTerritorial` | objeto `{ nivel: codigo }` | Según perfil del país | Códigos de las divisiones territoriales, validados contra el catálogo embebido. Los niveles y su obligatoriedad los define el perfil del país: CO `{ departamento, municipio }`, DO `{ provincia, municipio }`, PA `{ provincia, distrito }`. En países sin perfil (modo genérico) admite `{ region: texto, ciudad: texto }` sin validación de catálogo. |
| `lineaDireccion` | string (1–300) | Sí | Descripción de la ubicación dentro de la división territorial, **sin ciudad ni departamento** (alineado con la regla FAJ14 de la DIAN). Es el **valor canónico** de la dirección urbana. |
| `lineaDireccion2` | string (0–300) | No | Información adicional (torre, piso, referencia de entrega). |
| `codigoPostal` | string | Según perfil del país | Código postal con el formato del país (CO `######`, DO `#####`, PA `####`, MX/US `#####`). |
| `capturaEstructurada` | objeto, opcional | No | Solo en países cuyo perfil la habilita (CO): `{ tipoVia, numeroVia, numeroPredio, complementos[] }` validados contra los catálogos embebidos (21 tipos de vía DIAN, 16 tipos de complemento). Es un **detalle de captura** que compone `lineaDireccion` — se conserva para reedición y presentación uniforme, pero la línea compuesta es el valor canónico. |

El Nugget es **inmutable**: cualquier cambio implica construir una nueva instancia. Cada dominio consumidor embebe sus direcciones como valores propios — no hay identificador de dirección ni referencia a un registro central.

---

## Sección 3: Igualdad y normalización

**Igualdad:** dos `DireccionFisica` son iguales si coinciden `(pais, divisionTerritorial, lineaDireccion normalizada, codigoPostal)`. La `capturaEstructurada` y `lineaDireccion2` **no participan** — son detalle de captura y anotación, no identidad del lugar (mismo patrón que el DV en `IdentificacionLegal`: el dato auxiliar no define el valor).

**Normalización de la línea (para comparación):** mayúsculas, espacios múltiples colapsados, sin espacios en extremos, puntuación no significativa (`.`, `,`) removida. El valor almacenado conserva la forma capturada; la normalización aplica solo al comparar — la bodega de Terceros usa esta comparación al consolidar direcciones del mismo tercero llegadas desde dominios distintos.

---

## Sección 4: Reglas de validación

Se evalúan al construir, en orden, sin salir del proceso (filtro 3 de la gobernanza): solo consultan los datos embebidos de la Sección 6.

| Regla | Descripción |
|-------|-------------|
| `[V01]` | **País válido:** `pais` existe en el catálogo embebido de países. |
| `[V02]` | **División territorial válida:** cada código se valida con el Nugget [`DivisionTerritorial`](../division-territorial/especificacion.md) (existencia y jerarquía vía `perteneceA()` — el municipio pertenece al departamento), y están presentes los niveles que el perfil del país marca como obligatorios. En modo genérico (país sin perfil), se admite texto libre en `region`/`ciudad`. |
| `[V03]` | **Línea de dirección obligatoria:** 1–300 caracteres (límite alineado con FAJ14 de la DIAN), no vacía tras normalizar. |
| `[V04]` | **Código postal por formato:** si se provee, cumple el formato del perfil del país. Si el perfil lo marca obligatorio, debe venir. **Política de existencia: advertencia** — si el catálogo embebido de códigos postales del país está disponible y el código no aparece, la instancia se construye marcada con advertencia, no se rechaza (mismo nivel "notificación" que aplica la DIAN en FAJ73; ver `tieneAdvertenciaCp()`). |
| `[V05]` | **Captura estructurada solo donde aplica:** `capturaEstructurada` solo se admite en países cuyo perfil la habilita; `tipoVia` debe existir en el catálogo de tipos de vía del país y cada complemento en el catálogo de tipos de complemento. Proveerla en un país sin nomenclatura codificada falla la construcción. |
| `[V06]` | **Coherencia línea ↔ captura estructurada:** cuando hay `capturaEstructurada`, `lineaDireccion` debe ser la línea compuesta por el Nugget a partir de ella (operación `componerLinea()`). Evita que el detalle y el valor canónico diverjan. |

---

## Sección 5: Operaciones

| Operación | Descripción |
|-----------|-------------|
| `esIgualA(otra)` | Igualdad por valor canónico — Sección 3. |
| `componerLinea()` | Compone la línea canónica desde `capturaEstructurada` (ej: `CL 10 # 43A-27, EDF Torre Norte, PIS 12, OFC 1205` → "Calle 10 # 43A-27, Edificio Torre Norte, Piso 12, Oficina 1205"). Falla si no hay captura estructurada. |
| `tieneAdvertenciaCp()` | `true` cuando el código postal cumplió el formato pero no aparece en el catálogo embebido (`[V04]`). El consumidor decide si exige corrección antes de usos fiscales. |
| `nivelesTerritoriales()` | Retorna los niveles del perfil del país con sus códigos y nombres resueltos desde el catálogo embebido (para presentación y para consumidores que resuelven jurisdicción — Impuestos). |
| `presentacion()` | Dirección en una sola cadena con el orden del perfil del país: línea + división territorial + código postal + país. Los formatos de impresión específicos (factura, certificado, sobre) son de cada interfaz. |

---

## Sección 6: Datos embebidos

**Producidos por el custodio (Datos de Referencia)** a partir de las fuentes heredadas del servicio de Direcciones v1.0 (rescatadas del historial del repositorio, commit del replanteamiento de jun-2026). Publicados en `compartido/datos-referencia/catalogos/` (fuente del custodio) y embebidos en `compartido/nuggets/direccion-fisica/datos/`:

| Archivo | Contenido | Fuente | Ajuste requerido |
|---------|-----------|--------|------------------|
| `perfiles-direccion.json` | Perfil por país: niveles territoriales y su obligatoriedad, formato y obligatoriedad del código postal, habilitación de captura estructurada, etiquetas de presentación. 5 países: CO, DO, PA, MX, US. | `formatos-direccion.json` (5 países) | ✅ **Aplicado**: reestructurado al modelo del Nugget (`niveles`/`codigoPostal`/`capturaEstructurada`/`presentacion`). Perfil CO corregido — captura estructurada y código postal **opcionales** (Anexo FE v1.9, Sección 8). MX/US en **modo genérico** hasta habilitación productiva (P4). |
| Divisiones territoriales | `[V02]` valida contra el catálogo del Nugget [`DivisionTerritorial`](../division-territorial/especificacion.md) (CO 1.188 / DO 221 / PA 108) — fuente única de la jerarquía territorial dentro del paquete (jun-2026). | Vía Nugget `DivisionTerritorial`. Corregimientos PA: pendiente transferido a ese Nugget. |
| `tipos-via-co.json` | 21 tipos de vía (catálogo DIAN: CL, CR, DG, TV, AC, AK…) | `configuracion/` del servicio eliminado (historial del repositorio) | Sin cambios — pasa a servir la captura estructurada opcional. |
| `tipos-complemento.json` | 16 tipos (APT, TRR, PIS, OFC, LOC, BDG, BLQ, INT, CSA, LTE, ETP, CNJ, URB, BRR, EDF, UND) | ídem | Sin cambios. |
| `tipos-direccion.json` | 5 tipos de uso (FSC, COM, COR, ENT, SUC) — **vocabulario compartido para los consumidores**, no atributo del VO (Sección 9). | ídem | Sin cambios. |
| `codigos-postales-co.json` | 248 códigos de las 10 ciudades principales (de 3.685 totales DIAN/4-72) | ídem | Alimenta la política `advertencia` de `[V04]`. El catálogo completo y los de otros países son datos vivos — fuera del paquete (Sección 9). |

---

## Sección 7: Ejemplos por país

### Colombia (CO) — con captura estructurada

```json
{
  "pais": "CO",
  "divisionTerritorial": { "departamento": "05", "municipio": "05001" },
  "capturaEstructurada": {
    "tipoVia": "CL", "numeroVia": "10", "numeroPredio": "43A-27",
    "complementos": [ { "tipo": "EDF", "valor": "Torre Norte" }, { "tipo": "OFC", "valor": "1205" } ]
  },
  "lineaDireccion": "Calle 10 # 43A-27, Edificio Torre Norte, Oficina 1205",
  "codigoPostal": "050021"
}
```
✅ Válida. La línea fue compuesta por `componerLinea()` (`[V06]`); división validada contra DIVIPOLA; código postal con formato `######` y presente en el catálogo embebido.

### Colombia (CO) — sin estructura

La misma dirección capturada como texto (`lineaDireccion` directa, sin `capturaEstructurada`, sin código postal) también es ✅ **válida**: la estructura es un modo de captura, no un requisito — la DIAN acepta la línea libre (FAJ14) y el código postal es opcional (FAJ73).

### República Dominicana (DO)

```json
{
  "pais": "DO",
  "divisionTerritorial": { "provincia": "01", "municipio": "10100" },
  "lineaDireccion": "Av. Winston Churchill esq. Calle Luis F. Thomen",
  "lineaDireccion2": "Torre Empresarial, Piso 8"
}
```
✅ Válida. DO no tiene nomenclatura codificada; `capturaEstructurada` aquí fallaría `[V05]`.

### Panamá (PA)

```json
{
  "pais": "PA",
  "divisionTerritorial": { "provincia": "8", "distrito": "8-1" },
  "lineaDireccion": "Calle 50, Edificio Global Bank, Piso 7"
}
```
✅ Válida con el catálogo actual (provincia + distrito). El corregimiento que exige la factura electrónica panameña está pendiente de incorporarse al catálogo (Sección 11).

### Estados Unidos (US) — perfil con catálogo no embebido

```json
{
  "pais": "US",
  "divisionTerritorial": { "region": "DC", "ciudad": "Washington" },
  "lineaDireccion": "1600 Pennsylvania Ave NW",
  "codigoPostal": "20500"
}
```
✅ Válida en modo genérico: división como texto (sin divisiones embebidas para US), código postal validado solo por formato `#####`.

---

## Sección 8: Herencia del servicio de Direcciones

El servicio de Direcciones v1.0 resolvió este problema en abril 2026 con un diseño que este Nugget hereda casi completo. Sus documentos y catálogos fueron eliminados del repositorio en el replanteamiento (jun-2026) para evitar confusión con el modelo vigente; se conservan en el historial de git. El paralelo:

| Componente del servicio | Destino en el Nugget |
|---|---|
| Estructura genérica + configuración por país (decisión 4 del anexo, patrón ISO 19160/UPU S42) | **Se hereda intacta** — es la base del Nugget. |
| Campos de la entidad Dirección (división, vía, predio, complementos, líneas, CP) | **Se heredan** reorganizados: división estructurada, línea canónica, captura estructurada opcional. |
| Catálogos: 21 tipos de vía, 16 complementos, 5 tipos de dirección, perfiles de 5 países, 248 códigos postales | **Se heredan como datos embebidos** (Sección 6). |
| Validaciones V1–V8 | **Se heredan** como `[V01]`–`[V06]` (V8 — existencia del CP — baja a advertencia; V9 — protección de borrado — desaparece con la entidad). |
| Identidad (`id`), persistencia centralizada, API CRUD | **Desaparecen** — el Nugget es un VO embebido en cada dominio. |
| Eventos `DireccionCreada/Actualizada/Inactivada` + sincronización por broker (sección 5 del anexo) | **Desaparecen** — no hay copias que sincronizar; la bodega de Terceros consolida las direcciones del tercero como consolida el resto de sus datos. |
| Pendientes PD1 (validación externa) y PD2 (autocompletado UI) | **Se conservan** como capacidades externas no bloqueantes (Sección 9). |
| `tipoDireccion` como atributo de la entidad | **Sale del VO** — es la relación del consumidor con la dirección; el catálogo de 5 tipos viaja como vocabulario compartido. |

### Corrección verificada contra la fuente normativa

La Alternativa A (texto libre) se descartó en el anexo del servicio con el argumento *"la DIAN exige tipos de vía codificados y códigos postales de catálogo oficial"*. Verificado contra el **Anexo Técnico de Factura Electrónica v1.9 (Resolución DIAN 000165 de 2023)**:

- **FAJ14 (`cac:AddressLine/cbc:Line`)**: la dirección es un *"elemento de texto libre, que el emisor puede elegir utilizar para poner toda la información de su dirección, **en lugar de utilizar elementos estructurados**"* — alfanumérico 1–300, "sin ciudad ni departamento". **La DIAN no exige tipos de vía codificados en FE.**
- **FAJ73 (`cbc:PostalZone`)**: código postal **opcional (0..1)**, con validación de nivel *notificación* contra la tabla oficial — advierte, no rechaza.
- Lo que sí exige estructurado: **departamento (FAJ11) y municipio (FAJ12), obligatorios contra las tablas DANE** — por eso la división territorial es la parte estructurada del Nugget.

Consecuencia: la captura estructurada colombiana pasa de exigencia a **modo de captura opcional** (valioso para presentación uniforme y consolidación, no para cumplimiento), y el código postal CO pasa a opcional con política de advertencia. La conclusión de fondo del servicio (estructura genérica + configuración por país) queda **ratificada** — solo cambia qué parte es la estructurada por obligación.

---

## Sección 9: Fuera de responsabilidad

| Fuera del Nugget | Responsable |
|------------------|-------------|
| **El uso de la dirección** (fiscal, comercial, correspondencia, entrega, sucursal): la misma dirección física es el mismo valor sea cual sea su propósito. El catálogo de 5 tipos viaja con los datos como vocabulario compartido. | La asociación del consumidor (como `tipoUso` en el Tercero, o la sede en Estructura Organizacional). |
| **Existencia del código postal contra el catálogo completo** (3.685 CO; ~145.000 MX; ~41.000 US, actualizados anualmente): volumen y frecuencia los hacen datos vivos, no embebibles. | Datos de Referencia (capacidad Sync) como verificación **no bloqueante**; el Nugget valida formato y advierte (`[V04]`). |
| **Validación/normalización externa** (Google Address Validation, Loqate — PD1 del servicio) y **autocompletado en captura** (Google Places — PD2). | Capacidades externas no bloqueantes, fuera del alcance F1. |
| **Geocodificación** (coordenadas). | Fuera del alcance del ERP. |
| **Propagación de cambios de dirección entre dominios.** Cada dominio es dueño de sus copias; la divergencia se detecta y concilia en la bodega de Terceros. | Bodega de Terceros (consolidación). |
| **Resolución de jurisdicción fiscal** a partir del municipio. | Impuestos (`JurisdiccionFiscal`), consumiendo `nivelesTerritoriales()`. |

---

## Sección 10: Consumidores

Adopción prevista según la [matriz del catálogo](../catalogo-nuggets.md#matriz-de-consumidores): Terceros (direcciones del tercero — disuelve `[D4]`/`[D13]` del modelo v1.0), CXC/Facturación (dirección del cliente en factura), Estructura Organizacional (sedes/sucursales). Candidatos adicionales a confirmar al intervenir los modelos: OXP (dirección del proveedor si la llega a necesitar) y Emisión Electrónica (dirección fiscal en los documentos).

---

## Sección 11: Revisión pendiente

| # | Pendiente | Owner | Criterio de cierre |
|---|----------|-------|--------------------|
| ~~P2~~ | ➡️ **Transferido (jun-2026)** al Nugget `DivisionTerritorial` (P1 de esa especificación): los corregimientos de Panamá pertenecen a la jerarquía territorial, no a la dirección. | — | — |
| P3 | **Origen normativo del catálogo de tipos de vía:** la nomenclatura DIAN (CL, CR, DG…) no es exigida en FE (verificado); confirmar su fuente real (formulario RUT / estándar de captura) para citarla en los datos, o reclasificar el catálogo como convención del producto. | Custodio | Fuente citada en `tipos-via-co.json`. |
| P4 | **Perfiles MX y US:** se heredan como referencia (las notas del SAT/CFDI 4.0 son valiosas), pero quedan en modo genérico hasta la habilitación productiva de cada país (alineado con `[D7]` de Impuestos). Ratificar al abrir F2. | Custodio | Perfiles ratificados o ajustados al habilitar el país. |
| P5 | **Ratificación del consultor fiscal** de los perfiles DO y PA (obligatoriedad de provincia/municipio/distrito en e-CF y FE panameña — las notas heredadas del servicio lo afirman pero sin cita normativa). | Consultor fiscal DO/PA | Citas normativas en el perfil o ajuste. |

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.2 | Julio 2026 | **Datos producidos (cierra P1, issue #77).** Los 5 catálogos embebidos se rescataron del historial del repositorio (servicio de Direcciones v1.0) y se publicaron en `compartido/datos-referencia/catalogos/` (fuente del custodio) y `compartido/nuggets/direccion-fisica/datos/` (embebidos): `tipos-via-co` (21), `tipos-complemento` (16), `tipos-direccion` (5), `codigos-postales-co` (248, 10 ciudades) y `perfiles-direccion` (5 países). El perfil se **reestructuró al modelo del Nugget** (`niveles`/`codigoPostal`/`capturaEstructurada`/`presentacion`), aplicando la corrección del perfil CO (captura estructurada y CP opcionales, Anexo FE v1.9) y dejando MX/US en modo genérico (P4). |
| 0.1 | Junio 2026 | Borrador inicial. Hereda del servicio de Direcciones v1.0 la estructura genérica + perfil por país, los catálogos (21 tipos de vía, 16 complementos, 5 tipos de uso, perfiles de 5 países, 248 códigos postales) y las validaciones; elimina identidad, persistencia centralizada y eventos de sincronización. **Corrección verificada contra el Anexo Técnico FE v1.9 (Res. DIAN 000165/2023):** la dirección en FE es texto libre 1–300 (FAJ14) y el código postal es opcional con validación de notificación (FAJ73) — la captura estructurada CO pasa a modo opcional que compone la línea canónica, y lo estructurado por obligación es la división territorial (FAJ11/FAJ12, tablas DANE). 6 reglas `[V01]`–`[V06]`, igualdad por valor canónico (línea normalizada), política de advertencia para código postal, 5 pendientes P1–P5. |
