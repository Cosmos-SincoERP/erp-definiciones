# Anexo — Diseño dimensional del catálogo tributario

## Propósito

Este anexo documenta la decisión de diseño para la configuración fiscal del sub-dominio de Impuestos. Explica por qué se adoptó un modelo dimensional (dimensiones independientes que el motor cruza en tiempo de cálculo) en lugar de un modelo combinatorio (reglas pre-armadas con todas las combinaciones).

---

## 1. Problema del modelo combinatorio

En un modelo combinatorio, cada regla de aplicación es una combinación completa:

```
Regla #4.827:
  clasificacion: "Servicios gravados 19%"
  + direccion: Gasto
  + jurisdiccion: CO-BOG
  + tributo: ICA
  + actividad: "Comercio al por menor"
  → tarifa: 11.04 ‰
```

Esto genera una explosión de registros por la multiplicación de factores (clasificación × dirección × jurisdicción × tributo × actividad). Para Colombia se estimaron ~286.000 reglas, donde el 90% es la misma estructura repetida con solo la tarifa diferente.

**Problemas:**
- Difícil de administrar — actualizar una tarifa de ICA requiere encontrar y modificar todas las combinaciones de ese municipio.
- Difícil de auditar — el volumen dificulta identificar qué cambió y cuándo.
- Diseño localizado — la estructura refleja la complejidad de Colombia (ICA municipal por actividad) pero no se adapta bien a países sin tributos subnacionales (República Dominicana) o con miles de jurisdicciones por clasificación de producto (EEUU).

---

## 2. Modelo dimensional

Se separa la configuración fiscal en dimensiones independientes. Cada dimensión responde una pregunta distinta y tiene su propio ciclo de vida:

| Dimensión | Pregunta que responde | Agregado | Registros típicos |
|-----------|----------------------|----------|:------------------:|
| **Tributos + Clasificaciones + Tratamientos** | ¿Qué tributos existen y a qué clasificaciones aplican? | CatalogoTributario | ~50 por país |
| **Tarifas por tributo y jurisdicción** | ¿Cuánto se cobra? | TarifaTributaria | ~500 por stream (máximo) |
| **Condiciones por perfil** | ¿Quién está exento o tiene tratamiento especial? | CondicionDeAplicacion | ~20 por país |

El motor cruza las tres dimensiones en tiempo de cálculo mediante un pipeline de pasos simples.

### Pipeline del motor

```
Solicitud:
  clasificacion: "Bienes gravados 19%"
  direccion: Gasto
  emisora: { granContribuyente: true }
  contraparte: { regimen: "Ordinario", autorretenedor: false }
  jurisdiccion: Bogotá
  actividadEconomica: "Comercio al por menor"

Paso 1 — ¿Qué tributos existen para esta jurisdicción?
  Nacional: IVA, RETEFUENTE, RIVA, INC
  Municipal Bogotá: ICA, RICA
  → Candidatos: [IVA, RETEFUENTE, RIVA, INC, ICA, RICA]

Paso 2 — ¿Cómo trata esta clasificación a cada tributo?
  "Bienes gravados 19%" + IVA → gravado ✓
  "Bienes gravados 19%" + RETEFUENTE → aplica ✓
  "Bienes gravados 19%" + RIVA → aplica (si hay IVA) ✓
  "Bienes gravados 19%" + INC → no aplica ✗
  "Bienes gravados 19%" + ICA → aplica ✓
  "Bienes gravados 19%" + RICA → aplica (si hay ICA) ✓
  → Filtrados: [IVA, RETEFUENTE, RIVA, ICA, RICA]

Paso 3 — ¿Algún perfil modifica la aplicación?
  contraparte no es autorretenedor → RETEFUENTE se mantiene
  emisora es gran contribuyente → RIVA aplica
  → Sin cambios: [IVA, RETEFUENTE, RIVA, ICA, RICA]

Paso 4 — ¿Cuál es la tarifa de cada uno?
  IVA → stream tarifa-CO-IVA → 19%
  RETEFUENTE → stream tarifa-CO-RETEFUENTE, concepto "Compras generales" → 2.5%
  RIVA → stream tarifa-CO-RIVA → 15% del IVA
  ICA → stream tarifa-CO-BOG-ICA, actividad "Comercio al por menor" → 11.04 ‰
  RICA → stream tarifa-CO-BOG-RICA → 10% del ICA

Paso 5 — Calcular (R16: valor = base × tarifa)
```

### Comparación con el modelo combinatorio

| Aspecto | Combinatorio | Dimensional |
|---------|:------------:|:-----------:|
| Registros totales Colombia | ~286.000 | ~150.000 |
| Tamaño del registro más grande | Stream único con 300K | ~500 (ICA Bogotá) |
| Actualizar tarifa ICA de un municipio | Buscar todas las combinaciones | Una fila en un stream |
| Crear nuevo concepto de retención | N reglas (clasificación × dirección) | Una fila en tarifa-CO-RETEFUENTE |
| Complejidad del motor | Buscar regla pre-armada | Pipeline de cruces simples |
| Adaptabilidad multi-país | Estructura localizada | Misma estructura, diferentes datos |

---

## 3. Estructura de los agregados

```
CatalogoTributario (agregado)           → stream: catalogo-tributario-{paisId}
  ├── Tributo (entidad)                   Qué tributos existen
  ├── ClasificacionTributaria (entidad)   Cómo se categorizan bienes/servicios
  └── Tratamiento (entidad)               Qué tributo aplica a qué clasificación

TarifaTributaria (agregado)             → stream: tarifa-{jurisdiccionId}-{tributoId}
  ├── EntradaDeTarifa (entidad)           Factor → tarifa (tipoTarifa: porcentaje | específica)
  └── Vigencia (VO)                       Rango temporal

CondicionDeAplicacion (agregado)        → stream: condicion-{paisId}
  ├── Condicion (entidad)                 Perfil → efecto sobre tributo
  └── Vigencia (VO)                       Rango temporal
```

### Patrón `origen` (estándar/personalizado)

Cada entrada tiene un atributo `origen` que indica si viene precargada con el sistema o fue configurada por el usuario:

- **origen: estándar** — Contenido fiscal precargado. Se actualiza con el producto cuando la normativa cambia.
- **origen: personalizado** — Excepción o ajuste del usuario. Tiene precedencia sobre el estándar.

El algoritmo de resolución es único: si existen dos entradas para el mismo factor (una estándar y otra personalizada), aplica la personalizada mientras esté vigente. Cuando la personalizada vence, la estándar vuelve a aplicar automáticamente.

---

## 4. Ejemplo completo — Colombia

### CatalogoTributario (stream: catalogo-tributario-CO)

**Tributos:**

| Código | Nombre | Naturaleza | Nivel jurisdiccional | Factor de tarifa | Padre |
|--------|--------|------------|---------------------|-----------------|-------|
| IVA | Impuesto sobre las Ventas | Aditivo | Nacional | Clasificación | — |
| RETEFUENTE | Retención en la fuente | Sustractivo | Nacional | Concepto de pago | — |
| RIVA | Retención de IVA | Sustractivo | Nacional | Porcentaje de padre | IVA |
| INC | Impuesto Nacional al Consumo | Aditivo | Nacional | Clasificación | — |
| AUTORETE_RENTA | Autorretención especial renta | Sustractivo | Nacional | Actividad económica | — |
| TIMBRE | Impuesto de timbre | Sustractivo | Nacional | Fija | — |
| GMF | Gravamen Movimientos Financieros | Sustractivo | Nacional | Fija | — |
| ICA | Impuesto de Industria y Comercio | Sustractivo | Municipal | Actividad económica | — |
| RICA | Retención de ICA | Sustractivo | Municipal | Porcentaje de padre | ICA |

**Clasificaciones tributarias:**

| Código | Nombre |
|--------|--------|
| GRAV_19 | Bienes y servicios gravados 19% |
| GRAV_05 | Bienes y servicios gravados 5% |
| EXENTO | Bienes y servicios exentos (0% con devolución) |
| EXCLUIDO | Bienes y servicios excluidos de IVA |
| INC_04 | Servicios gravados con INC 4% |
| INC_08 | Servicios gravados con INC 8% |

**Tratamientos (clasificación × tributo → aplica/no aplica):**

| Clasificación | IVA | RETEFUENTE | RIVA | INC | ICA | RICA |
|---------------|:---:|:----------:|:----:|:---:|:---:|:----:|
| GRAV_19 | ✓ gravado | ✓ | ✓ | ✗ | ✓ | ✓ |
| GRAV_05 | ✓ gravado | ✓ | ✓ | ✗ | ✓ | ✓ |
| EXENTO | ✓ exento | ✓ | ✗ | ✗ | ✓ | ✓ |
| EXCLUIDO | ✗ | ✓ | ✗ | ✗ | ✓ | ✓ |
| INC_04 | ✗ | ✓ | ✗ | ✓ | ✓ | ✓ |
| INC_08 | ✗ | ✓ | ✗ | ✓ | ✓ | ✓ |

**Total catálogo Colombia: ~51 registros** (9 tributos + 6 clasificaciones + 36 tratamientos)

### TarifaTributaria — streams nacionales

**stream: tarifa-CO-IVA** (4 entradas)

| Factor (clasificación) | Tarifa | Vigencia |
|------------------------|:------:|----------|
| GRAV_19 | 19% | 2017-01-01 → ∞ |
| GRAV_05 | 5% | 2017-01-01 → ∞ |
| EXENTO | 0% | 2017-01-01 → ∞ |
| INC_08 | 0% | 2017-01-01 → ∞ |

**stream: tarifa-CO-RETEFUENTE** (~35 entradas)

| Factor (concepto de pago) | Tarifa | Cuantía mínima | Vigencia |
|--------------------------|:------:|:--------------:|----------|
| Compras generales | 2.5% | 27 UVT | 2026-01-01 → ∞ |
| Servicios generales | 4% | 4 UVT | 2026-01-01 → ∞ |
| Servicios de consultoría | 6% | 4 UVT | 2026-01-01 → ∞ |
| Honorarios PN declarante | 10% | 0 | 2026-01-01 → ∞ |
| Honorarios PN no declarante | 11% | 0 | 2026-01-01 → ∞ |
| Arrendamiento muebles | 4% | 0 | 2026-01-01 → ∞ |
| Arrendamiento inmuebles | 3.5% | 27 UVT | 2026-01-01 → ∞ |
| Contratos de construcción | 2% | 27 UVT | 2026-01-01 → ∞ |
| Compras combustibles | 0.1% | 0 | 2026-01-01 → ∞ |
| Pagos al exterior | 15% | 0 | 2026-01-01 → ∞ |
| *(... ~25 conceptos más)* | | | |

**stream: tarifa-CO-RIVA** (2 entradas)

| Factor | Tarifa | Vigencia |
|--------|:------:|----------|
| General | 15% del IVA | 2026-01-01 → ∞ |
| Agentes designados | 100% del IVA | 2026-01-01 → ∞ |

**stream: tarifa-CO-INC** (3 entradas)

| Factor (categoría de consumo) | Tarifa | Vigencia |
|-------------------------------|:------:|----------|
| Telefonía celular y datos | 4% | 2026-01-01 → ∞ |
| Restaurantes y bares | 8% | 2026-01-01 → ∞ |
| Vehículos >USD 30K | 16% | 2026-01-01 → ∞ |

**stream: tarifa-CO-AUTORETE_RENTA** (~20 entradas)

| Factor (grupo actividad CIIU) | Tarifa | Vigencia |
|-------------------------------|:------:|----------|
| Comercio | 0.40% | 2026-01-01 → ∞ |
| Servicios | 0.80% | 2026-01-01 → ∞ |
| Industrial | 0.40% | 2026-01-01 → ∞ |
| Financiero | 1.60% | 2026-01-01 → ∞ |
| *(... ~15 grupos más)* | | |

**stream: tarifa-CO-TIMBRE** (1 entrada)

| Factor | Tarifa | Cuantía mínima | Vigencia |
|--------|:------:|:--------------:|----------|
| Instrumentos públicos/privados | 1% | 6.000 UVT | 2026-01-01 → ∞ |

**stream: tarifa-CO-GMF** (1 entrada)

| Factor | Tarifa | Vigencia |
|--------|:------:|----------|
| Movimientos financieros | 0.4% (4×1000) | 2026-01-01 → ∞ |

### TarifaTributaria — streams municipales

**stream: tarifa-CO-BOG-ICA** (~500 entradas)

| Factor (actividad económica) | Tarifa (‰) | Vigencia |
|------------------------------|:----------:|----------|
| Comercio al por menor | 11.04 | 2026-01-01 → ∞ |
| Servicios financieros | 11.04 | 2026-01-01 → ∞ |
| Industrial textil | 4.14 | 2026-01-01 → ∞ |
| Servicios profesionales | 9.66 | 2026-01-01 → ∞ |
| Transporte | 4.14 | 2026-01-01 → ∞ |
| *(... ~495 actividades más)* | | |

**stream: tarifa-CO-BOG-RICA** (1 entrada)

| Factor | Tarifa | Vigencia |
|--------|:------:|----------|
| General | 10% del ICA | 2026-01-01 → ∞ |

**stream: tarifa-CO-MED-ICA** (~300 entradas)

| Factor (actividad económica) | Tarifa (‰) | Vigencia |
|------------------------------|:----------:|----------|
| Comercio al por menor | 7.0 | 2026-01-01 → ∞ |
| Servicios financieros | 10.0 | 2026-01-01 → ∞ |
| Industrial textil | 3.0 | 2026-01-01 → ∞ |
| *(... ~297 actividades más)* | | |

### CondicionDeAplicacion (stream: condicion-CO)

| Condición | Tributo afectado | Efecto | Vigencia |
|-----------|-----------------|--------|----------|
| Contraparte es autorretenedora | RETEFUENTE | No practicar retención | 2026-01-01 → ∞ |
| Contraparte es no responsable IVA | IVA | No cobrar IVA | 2026-01-01 → ∞ |
| Contraparte es régimen simple | RETEFUENTE | No practicar retención | 2026-01-01 → ∞ |
| Emisora es gran contribuyente | RIVA | Aplica tarifa "Agentes designados" | 2026-01-01 → ∞ |
| Contraparte es autorretenedora ICA | ICA | No practicar RICA | 2026-01-01 → ∞ |
| Base gravable < cuantía mínima | *(cualquiera)* | No aplicar tributo | 2026-01-01 → ∞ |
| Contraparte es persona del exterior | RETEFUENTE | Aplicar tarifa "Pagos al exterior" | 2026-01-01 → ∞ |
| Transacción es importación de servicios | IVA | Reverse charge (emisora autoliquida) | 2026-01-01 → ∞ |
| *(... ~12 condiciones más)* | | | |

### Resumen de volumen Colombia

| Agregado | Streams | Entradas por stream | Total entradas |
|----------|:-------:|:-------------------:|:--------------:|
| CatalogoTributario | 1 | ~51 | ~51 |
| TarifaTributaria (nacionales) | 7 | 1-35 | ~66 |
| TarifaTributaria (municipales) | ~2.244 | 1-500 | ~150.000 |
| CondicionDeAplicacion | 1 | ~20 | ~20 |

---

## 5. Ejemplo completo — República Dominicana

### CatalogoTributario (stream: catalogo-tributario-DO)

**Tributos:**

| Código | Nombre | Naturaleza | Nivel | Factor de tarifa | Padre |
|--------|--------|------------|-------|-----------------|-------|
| ITBIS | Impuesto a Transferencias de Bienes y Servicios | Aditivo | Nacional | Clasificación | — |
| RITBIS | Retención de ITBIS | Sustractivo | Nacional | Porcentaje de padre | ITBIS |
| ISC | Impuesto Selectivo al Consumo | Aditivo | Nacional | Clasificación | — |
| RET_ISR | Retención Impuesto Sobre la Renta | Sustractivo | Nacional | Concepto de pago | — |
| CDT | Contribución Transitoria sobre Intereses | Sustractivo | Nacional | Fija | — |
| PROPINA | Propina Legal | Aditivo | Nacional | Fija | — |

**Clasificaciones tributarias:**

| Código | Nombre |
|--------|--------|
| GRAV_18 | Bienes y servicios gravados ITBIS 18% |
| GRAV_16 | Bienes gravados ITBIS 16% |
| EXENTO_ITBIS | Bienes y servicios exentos de ITBIS |
| ISC_ALCOHOL | Productos gravados con ISC (alcohol) |
| ISC_TABACO | Productos gravados con ISC (tabaco) |

**Tratamientos:**

| Clasificación | ITBIS | RITBIS | ISC | RET_ISR | PROPINA |
|---------------|:-----:|:------:|:---:|:-------:|:-------:|
| GRAV_18 | ✓ | ✓ | ✗ | ✓ | ✗ |
| GRAV_16 | ✓ | ✓ | ✗ | ✓ | ✗ |
| EXENTO_ITBIS | ✗ | ✗ | ✗ | ✓ | ✗ |
| ISC_ALCOHOL | ✓ | ✓ | ✓ | ✓ | ✗ |
| ISC_TABACO | ✓ | ✓ | ✓ | ✓ | ✗ |

### TarifaTributaria — streams RD

**stream: tarifa-DO-ITBIS** (2 entradas)

| Factor | Tarifa | Vigencia |
|--------|:------:|----------|
| GRAV_18 | 18% | 2026-01-01 → ∞ |
| GRAV_16 | 16% | 2026-01-01 → ∞ |

**stream: tarifa-DO-RITBIS** (1 entrada)

| Factor | Tarifa | Vigencia |
|--------|:------:|----------|
| General | 30% del ITBIS | 2026-01-01 → ∞ |

**stream: tarifa-DO-RET_ISR** (4 entradas)

| Factor (concepto) | Tarifa | Vigencia |
|-------------------|:------:|----------|
| Alquiler | 10% | 2026-01-01 → ∞ |
| Honorarios profesionales | 10% | 2026-01-01 → ∞ |
| Pagos al exterior | 27% | 2026-01-01 → ∞ |
| Intereses | 10% | 2026-01-01 → ∞ |

**stream: tarifa-DO-ISC** (2 entradas)

| Factor (categoría) | Tarifa | Vigencia |
|--------------------|:------:|----------|
| Alcohol | Específica por producto | 2026-01-01 → ∞ |
| Tabaco | Específica por producto | 2026-01-01 → ∞ |

### CondicionDeAplicacion (stream: condicion-DO)

| Condición | Tributo | Efecto | Vigencia |
|-----------|---------|--------|----------|
| Contraparte es agente retención ITBIS | RITBIS | Aplica retención | 2026-01-01 → ∞ |
| Contraparte es persona física | RET_ISR | Aplica retención | 2026-01-01 → ∞ |
| Contraparte exenta por resolución DGII | ITBIS | No cobrar ITBIS | 2026-01-01 → ∞ |

### Resumen de volumen República Dominicana

| Agregado | Streams | Entradas por stream | Total entradas |
|----------|:-------:|:-------------------:|:--------------:|
| CatalogoTributario | 1 | ~30 | ~30 |
| TarifaTributaria | 5 | 1-4 | ~11 |
| CondicionDeAplicacion | 1 | ~10 | ~10 |

**Nota:** República Dominicana no tiene tributos subnacionales. Cero streams municipales.

---

## 6. Ejemplo completo — EEUU (Texas)

### CatalogoTributario (stream: catalogo-tributario-US)

**Tributos:**

| Código | Nombre | Naturaleza | Nivel | Factor de tarifa | Padre |
|--------|--------|------------|-------|-----------------|-------|
| SALES_TAX | Sales and Use Tax | Aditivo | Estatal | Clasificación | — |
| LOCAL_SALES_TAX | Local Sales Tax | Aditivo | Municipal/Condado | Clasificación | — |

**Clasificaciones tributarias:**

| Código | Nombre |
|--------|--------|
| TANGIBLE | Tangible personal property |
| FOOD_GROCERY | Food and grocery (unprepared) |
| FOOD_PREPARED | Prepared food |
| CLOTHING | Clothing and footwear |
| DIGITAL | Digital goods |
| EXEMPT_US | Exempt items |

**Tratamientos (para Texas):**

| Clasificación | SALES_TAX | LOCAL_SALES_TAX |
|---------------|:---------:|:---------------:|
| TANGIBLE | ✓ | ✓ |
| FOOD_GROCERY | ✗ | ✗ |
| FOOD_PREPARED | ✓ | ✓ |
| CLOTHING | ✓ | ✓ |
| DIGITAL | ✓ | ✓ |
| EXEMPT_US | ✗ | ✗ |

### TarifaTributaria — streams EEUU

**stream: tarifa-US-TX-SALES_TAX** (4 entradas)

| Factor | Tarifa | Vigencia |
|--------|:------:|----------|
| TANGIBLE | 6.25% | 2026-01-01 → ∞ |
| FOOD_PREPARED | 6.25% | 2026-01-01 → ∞ |
| CLOTHING | 6.25% | 2026-01-01 → ∞ |
| DIGITAL | 6.25% | 2026-01-01 → ∞ |

**stream: tarifa-US-TX-AUS-LOCAL_SALES_TAX** (Austin, TX — 4 entradas)

| Factor | Tarifa | Vigencia |
|--------|:------:|----------|
| TANGIBLE | 2.0% | 2026-01-01 → ∞ |
| FOOD_PREPARED | 2.0% | 2026-01-01 → ∞ |
| CLOTHING | 2.0% | 2026-01-01 → ∞ |
| DIGITAL | 2.0% | 2026-01-01 → ∞ |

**stream: tarifa-US-TX-DAL-LOCAL_SALES_TAX** (Dallas, TX — 4 entradas)

| Factor | Tarifa | Vigencia |
|--------|:------:|----------|
| TANGIBLE | 2.0% | 2026-01-01 → ∞ |
| FOOD_PREPARED | 2.0% | 2026-01-01 → ∞ |
| CLOTHING | 2.0% | 2026-01-01 → ∞ |
| DIGITAL | 2.0% | 2026-01-01 → ∞ |

### CondicionDeAplicacion (stream: condicion-US)

| Condición | Tributo | Efecto | Vigencia |
|-----------|---------|--------|----------|
| Comprador tiene certificado de exención | SALES_TAX | No cobrar | 2026-01-01 → ∞ |
| Comprador es entidad gubernamental | SALES_TAX | Exento | 2026-01-01 → ∞ |
| Comprador es organización sin ánimo de lucro | SALES_TAX | Exento (con certificado) | 2026-01-01 → ∞ |

### Resumen de volumen EEUU (solo Texas)

| Agregado | Streams | Entradas por stream | Total entradas |
|----------|:-------:|:-------------------:|:--------------:|
| CatalogoTributario | 1 | ~15 | ~15 |
| TarifaTributaria (estatal) | 1 | ~4 | ~4 |
| TarifaTributaria (locales) | ~1.200 | ~4 | ~4.800 |
| CondicionDeAplicacion | 1 | ~10 | ~10 |

---

## 7. Cómo se almacenan los streams (Event Sourcing)

Cada stream es una secuencia de eventos inmutables que, al reproducirse (replay), reconstruyen el estado actual del agregado.

### Ejemplo: stream tarifa-CO-IVA

```
Stream: tarifa-CO-IVA
Versión: 5 eventos

Evento 1: TarifaTributariaCreada
  agregadoId: tarifa-CO-IVA
  origen: estándar
  timestamp: 2026-01-01

Evento 2: EntradaDeTarifaAgregada
  factor: "GRAV_19"
  tarifa: 19%
  cuantiaMínima: null
  vigencia: { desde: 2017-01-01, hasta: null }
  origen: estándar

Evento 3: EntradaDeTarifaAgregada
  factor: "GRAV_05"
  tarifa: 5%
  cuantiaMínima: null
  vigencia: { desde: 2017-01-01, hasta: null }
  origen: estándar

Evento 4: EntradaDeTarifaAgregada
  factor: "EXENTO"
  tarifa: 0%
  cuantiaMínima: null
  vigencia: { desde: 2017-01-01, hasta: null }
  origen: estándar

Evento 5: EntradaDeTarifaAgregada
  factor: "GRAV_05"
  tarifa: 8%
  cuantiaMínima: null
  vigencia: { desde: 2028-01-01, hasta: null }
  origen: estándar
  // Hipotético: el gobierno sube la tarifa diferencial de 5% a 8%.
  // La entrada anterior (evento 3) no se modifica.
  // R08 se valida: vigencia anterior se cierra automáticamente
  // al 2027-12-31 para no solapar.
```

**Estado reconstruido por replay:**

| Factor | Tarifa | Vigencia | Origen |
|--------|:------:|----------|--------|
| GRAV_19 | 19% | 2017-01-01 → ∞ | estándar |
| GRAV_05 | 5% | 2017-01-01 → 2027-12-31 | estándar |
| GRAV_05 | 8% | 2028-01-01 → ∞ | estándar |
| EXENTO | 0% | 2017-01-01 → ∞ | estándar |

**Consulta del motor:**
- `tarifaVigenteA("GRAV_05", 2026-06-15)` → 5%
- `tarifaVigenteA("GRAV_05", 2028-03-01)` → 8%

### Ejemplo: stream tarifa-CO-RETEFUENTE

```
Stream: tarifa-CO-RETEFUENTE
Versión: 38 eventos (1 creación + 35 entradas + 2 actualizaciones)

Evento 1: TarifaTributariaCreada
  agregadoId: tarifa-CO-RETEFUENTE
  origen: estándar
  timestamp: 2026-01-01

Evento 2: EntradaDeTarifaAgregada
  factor: "Compras generales"
  tarifa: 2.5%
  cuantiaMínima: { valor: 27, unidad: "UVT" }
  vigencia: { desde: 2026-01-01, hasta: null }
  origen: estándar

Evento 3: EntradaDeTarifaAgregada
  factor: "Servicios generales"
  tarifa: 4%
  cuantiaMínima: { valor: 4, unidad: "UVT" }
  vigencia: { desde: 2026-01-01, hasta: null }
  origen: estándar

Evento 4: EntradaDeTarifaAgregada
  factor: "Honorarios PN declarante"
  tarifa: 10%
  cuantiaMínima: { valor: 0, unidad: "UVT" }
  vigencia: { desde: 2026-01-01, hasta: null }
  origen: estándar

Evento 5: EntradaDeTarifaAgregada
  factor: "Honorarios PN no declarante"
  tarifa: 11%
  cuantiaMínima: { valor: 0, unidad: "UVT" }
  vigencia: { desde: 2026-01-01, hasta: null }
  origen: estándar

Evento 6: EntradaDeTarifaAgregada
  factor: "Arrendamiento inmuebles"
  tarifa: 3.5%
  cuantiaMínima: { valor: 27, unidad: "UVT" }
  vigencia: { desde: 2026-01-01, hasta: null }
  origen: estándar

Evento 7: EntradaDeTarifaAgregada
  factor: "Arrendamiento muebles"
  tarifa: 4%
  cuantiaMínima: { valor: 0, unidad: "UVT" }
  vigencia: { desde: 2026-01-01, hasta: null }
  origen: estándar

Evento 8: EntradaDeTarifaAgregada
  factor: "Contratos de construcción"
  tarifa: 2%
  cuantiaMínima: { valor: 27, unidad: "UVT" }
  vigencia: { desde: 2026-01-01, hasta: null }
  origen: estándar

... (eventos 9-36: los demás conceptos)

Evento 37: EntradaDeTarifaActualizada
  factor: "Servicios generales"
  tarifa: 6%
  cuantiaMínima: { valor: 4, unidad: "UVT" }
  vigencia: { desde: 2027-01-01, hasta: null }
  origen: estándar
  // Gobierno subió tarifa de servicios generales de 4% a 6%.

Evento 38: EntradaDeTarifaAgregada
  factor: "Servicios de consultoría especializada"
  tarifa: 3%
  cuantiaMínima: { valor: 4, unidad: "UVT" }
  vigencia: { desde: 2027-06-01, hasta: null }
  origen: personalizado
  // El usuario agrega una excepción: tiene una resolución
  // de la DIAN que le autoriza una tarifa reducida para
  // este concepto específico. Origen: personalizado.
```

**Estado reconstruido por replay:**

| Factor | Tarifa | Cuantía mín. | Vigencia | Origen |
|--------|:------:|:------------:|----------|--------|
| Compras generales | 2.5% | 27 UVT | 2026-01-01 → ∞ | estándar |
| Servicios generales | 4% | 4 UVT | 2026-01-01 → 2026-12-31 | estándar |
| Servicios generales | 6% | 4 UVT | 2027-01-01 → ∞ | estándar |
| Honorarios PN declarante | 10% | 0 | 2026-01-01 → ∞ | estándar |
| Honorarios PN no declarante | 11% | 0 | 2026-01-01 → ∞ | estándar |
| Arrendamiento inmuebles | 3.5% | 27 UVT | 2026-01-01 → ∞ | estándar |
| Arrendamiento muebles | 4% | 0 | 2026-01-01 → ∞ | estándar |
| Contratos de construcción | 2% | 27 UVT | 2026-01-01 → ∞ | estándar |
| *(... ~27 conceptos más)* | | | | estándar |
| Servicios de consultoría especializada | 3% | 4 UVT | 2027-06-01 → ∞ | **personalizado** |

**Consulta del motor:**

```
Solicitud: tarifa de RETEFUENTE para concepto "Servicios generales",
           fecha transacción 2027-03-15, base gravable $5.000.000

Paso 1: tarifaVigenteA("Servicios generales", 2027-03-15)
  → Candidatas: 6% (producto, vigente ✓), 4% (producto, venció ✗)
  → Resultado: 6%

Paso 2: ¿Cuantía mínima?
  → 4 UVT × $49.799 (UVT 2027) = $199.196
  → $5.000.000 > $199.196 → Aplica ✓

Paso 3: Cálculo
  → $5.000.000 × 6% = $300.000 retención
```

---

## 8. Volumen comparado por país

| País | CatalogoTributario | Streams TarifaTributaria | Entradas de tarifa (aprox.) | CondicionDeAplicacion |
|------|:------------------:|:------------------------:|:---------------------------:|:---------------------:|
| **Colombia** | ~51 | ~2.251 (7 nacionales + 2.244 municipales) | ~150.000 | ~20 |
| **Rep. Dominicana** | ~30 | 5 | ~11 | ~10 |
| **EEUU (solo Texas)** | ~15 | ~1.201 (1 estatal + 1.200 locales) | ~4.800 | ~10 |
| **Panamá** | ~20 | 4 | ~40 | ~8 |

**Nota:** Ningún stream individual supera ~500 entradas. El volumen total se distribuye en miles de streams pequeños e independientes.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: justificación del diseño dimensional, ejemplos por país, almacenamiento de streams. |
