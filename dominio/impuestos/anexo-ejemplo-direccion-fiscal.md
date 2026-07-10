# Anexo — Ejemplo de comportamiento del motor de cálculo según la dirección fiscal

> **Propósito:** Ilustrar con un caso simétrico cómo el `MotorDeCalculo` evalúa las `CondicionDeAplicacion` ante las dos direcciones fiscales (`gasto` e `ingreso`), evidenciando cómo la dirección fiscal se materializa **explícitamente** en el modelo mediante `direccionFiscalAplicable` en Tributo y Condicion, manteniendo el lenguaje fiscal del dominio (`emisora`/`contraparte` como roles posicionales).
>
> **Aplica a:** Modelo de dominio `dominio/impuestos/modelo-dominio.md` v1.3 (con el refinamiento del Cambio 1 — direccionFiscalAplicable explícito).
> **Referencias:** R02, R09, R11, R30, P2, [D2] refinada, [D9], MotorDeCalculo (Sección 3.12), CondicionDeAplicacion (Sección 3.4), `anexo-configuracion-estandar-co.md`.
>
> **Audiencia:** Equipo de desarrollo, integradores de OXP/CXC, administradores fiscales.

---

## 1. Marco conceptual

La `direccionFiscal` (gasto/ingreso) es un campo **obligatorio** del contrato de entrada del motor de cálculo (`[D9]`, R30). Determina el **sentido del hecho económico** y, con ello, los roles comerciales de las partes:

| Rol posicional fiscal | En `gasto` (OXP/CXP) — rol comercial | En `ingreso` (CXC) — rol comercial |
|---|---|---|
| **emisora** | La empresa operadora (adquiriente, agente retenedor) | La empresa operadora (facturadora, sujeto de retención) |
| **contraparte** | Proveedor (vendedor) | Cliente (comprador) |

**Decisión de diseño clave (`[D2]` refinada):** la dirección fiscal se materializa **explícitamente** en el modelo mediante dos mecanismos complementarios:

1. **`Tributo.direccionFiscalAplicable`** declara las direcciones donde el tributo existe normativamente (invariante del agregado). Ejemplos: AUTO_RETEFUENTE solo en `ingreso`; AUTO_RIVA solo en `gasto` (reverseCharge). La mayoría de tributos directos (IVA, RETEFUENTE, RIVA, RICA) tienen `direccionFiscalAplicable: ambas`; ICA es `ingreso` — el sujeto pasivo es quien genera el ingreso (issue #93).
2. **`Condicion.direccionFiscalAplicable`** declara las direcciones donde la condición se evalúa. Permite modelar reglas con perspectiva fiscal específica (ej: "si el proveedor es exento de retefuente no le retengo" solo aplica en `gasto` evaluando `contraparte`; la regla simétrica en `ingreso` evalúa `emisora`).

Los roles `emisora`/`contraparte` se mantienen como **roles posicionales fiscales** (lenguaje del dominio, alineado con SAP Legal Entity, Oracle First/Third Party). El motor filtra tributos y condiciones por `direccionFiscalAplicable` antes de evaluar — la dirección entra explícitamente a la lógica del motor, no implícitamente.

> **Nota — relación con `JurisdiccionFiscal` (Cambio 2):** Además de filtrar por dirección fiscal, el motor resuelve las jurisdicciones de las ubicaciones (`sedeEmisora.jurisdiccion`, `sedeContraparte.jurisdiccion`, `lugarEjecucion.jurisdiccion`) contra el catálogo `JurisdiccionFiscal` (Sección 3.7 del modelo). Esto permite que las condiciones evalúen atributos de la jurisdicción (`tipo`, `tipoRegimen`, `codigo`) para modelar regímenes territoriales (Puerto Libre San Andrés, Frontera Norte MX, etc.). Este ejemplo se enfoca en la dirección fiscal y omite el cruce con jurisdicciones territoriales especiales para mantener legibilidad — el comportamiento de las condiciones que evalúan jurisdicciones se documentará en un anexo dedicado posteriormente.

---

## 2. Contexto del ejemplo

Se modela la **misma transacción** vista desde las dos direcciones:

- **Concepto:** Servicios de consultoría
- **Monto:** $1.000.000 COP
- **Clasificación tributaria:** `GRAV_19` (gravado al 19%)
- **Concepto de pago:** "Servicios de consultoría"
- **Fecha de transacción:** 2026-05-04
- **Jurisdicción:** Colombia (omitimos tributos municipales ICA/RICA para mantener el ejemplo legible)

### 2.1. Entidades fiscales

| Entidad | Atributos del perfil tributario |
|---|---|
| **Sinco SAS** *(la empresa que usa el ERP)* | `perteneceRegimenIVA=true`, `esGranContribuyente=false`, `esAutorretenedora=false`, `esAgenteRetenedorIVA=false`, `perteneceRegimenSimple=false`, `esExentoRetefuente=false` |
| **ConsultorPro SAS** *(proveedor — Caso A)* | `perteneceRegimenIVA=true`, `esAgenteRetenedorIVA=false`, `esAutorretenedora=false` |
| **MegaCorp SAS** *(cliente — Caso B)* | `perteneceRegimenIVA=true`, `esAgenteRetenedorIVA=true`, `esGranContribuyente=true` |

### 2.2. Configuración fiscal aplicable (extracto Colombia)

**Tributos candidatos para `GRAV_19`** (de `CatalogoTributario.tributosAplicablesA(GRAV_19)`): `IVA`, `RETEFUENTE`, `RIVA`, `ICA`, `RICA`.

**Condiciones relevantes** (de `CondicionDeAplicacion`, ver `anexo-configuracion-estandar-co.md`):

| # | Tributo | Entidad evaluada | Atributo | Valor esperado | Efecto | direccionFiscalAplicable |
|---|---|---|---|---|---|---|
| IVA-1a | IVA | emisora | `perteneceRegimenIVA` | `true` | `aplicar` | `ingreso` |
| IVA-1b | IVA | contraparte | `perteneceRegimenIVA` | `true` | `aplicar` | `gasto` |
| RTF-1a | RETEFUENTE | emisora | `perteneceRegimenSimple` | `true` | `noAplicar` | `ambas` |
| RTF-1b | RETEFUENTE | contraparte | `perteneceRegimenSimple` | `true` | `noAplicar` | `ambas` |
| RTF-2a | RETEFUENTE | emisora | `esExentoRetefuente` | `true` | `noAplicar` | `ingreso` |
| RTF-2b | RETEFUENTE | contraparte | `esExentoRetefuente` | `true` | `noAplicar` | `gasto` |
| RTF-3a | RETEFUENTE | emisora | `esAutorretenedora` | `true` | `noAplicar` (activa AUTO_RETEFUENTE) | `ingreso` |
| RTF-3b | RETEFUENTE | contraparte | `esAutorretenedora` | `true` | `noAplicar` | `gasto` |
| RTF-4a | RETEFUENTE | contraparte | `perteneceRegimenIVA` | `false` | `noAplicar` | `gasto` |
| RTF-4b | RETEFUENTE | emisora | `perteneceRegimenIVA` | `false` | `noAplicar` | `ingreso` |
| RIVA-1a | RIVA | emisora + contraparte | `perteneceRegimenIVA` (E) + `esAgenteRetenedorIVA` (C) | `true` + `true` | `aplicar` | `ingreso` |
| RIVA-1b | RIVA | contraparte + emisora | `perteneceRegimenIVA` (C) + `esAgenteRetenedorIVA` (E) | `true` + `true` | `aplicar` | `gasto` |
| RIVA-2a | RIVA | contraparte | `esAgenteRetenedorIVA` | `false` | `noAplicar` | `ingreso` |
| RIVA-2b | RIVA | emisora | `esAgenteRetenedorIVA` | `false` | `noAplicar` | `gasto` |
| RIVA-3 | RIVA | emisora | `esAgenteRetenedorIVA` | `true` | `reverseCharge` (activa AUTO_RIVA) | `gasto` |

**Tributos con direccionalidad inherente** (`Tributo.direccionFiscalAplicable`):
- ICA, AUTO_RETEFUENTE, AUTO_RICA, AUTO_RENTA → `ingreso`
- AUTO_RIVA → `gasto` (reverseCharge)
- IVA, INC, RETEFUENTE, RIVA, RICA, SOBRETASA_BOMBERIL → `ambas`

**Tarifas vigentes a 2026-05-04** (de `TarifaTributaria`):

- IVA, factor `clasificacion=GRAV_19` → 19%
- RETEFUENTE, factor `conceptoPago="Servicios de consultoría"` → 4%
- RIVA, factor `porcentajeDePadre` → 15% del IVA padre

---

## 3. Caso A — Dirección `gasto` (Sinco compra a ConsultorPro vía OXP)

### 3.1. Solicitud al motor

```
direccionFiscal:           gasto
entidadFiscalEmisora:      Sinco SAS         ← rol: adquiriente
entidadFiscalContraparte:  ConsultorPro SAS  ← rol: proveedor
fechaTransaccion:          2026-05-04
moneda:                    COP
conceptos[0]: {
  id:                      GASTO-001
  clasificacionTributaria: GRAV_19
  conceptoPago:            "Servicios de consultoría"
  monto:                   1.000.000
}
```

### 3.2. Flujo del motor

**Paso 1 — Resolver perfiles tributarios:**
`PerfilTributario.perfilCompletoA(2026-05-04)` para Sinco y ConsultorPro.

**Paso 2 — Determinar tributos candidatos y filtrar por direccionFiscalAplicable:**
`CatalogoTributario.tributosAplicablesA(GRAV_19)` → `[IVA, RETEFUENTE, RIVA, ICA, RICA]`. **ICA se descarta por dirección** (`direccionFiscalAplicable: ingreso` no incluye `gasto` — el comprador no autoliquida ICA; solo practicaría la retención RICA, issue #93); IVA, RETEFUENTE, RIVA y RICA tienen `ambas` y siguen. Las autorretenciones no están en la matriz para GRAV_19.

**Paso 3 — Filtrar condiciones por direccionFiscalAplicable contra `gasto`:**
Solo se evalúan las condiciones con `direccionFiscalAplicable ∈ {gasto, ambas}`. Las que tienen `direccionFiscalAplicable: ingreso` se descartan (no se evalúan en este caso).

**Paso 4 — Evaluar condiciones filtradas por tributo:**

| Tributo | Condición (solo las aplicables en gasto) | Evaluación con perfiles | Resultado |
|---|---|---|---|
| **IVA** | IVA-1b: `contraparte.perteneceRegimenIVA == true` (gasto) | ConsultorPro=true ✓ | **aplica** |
| **RETEFUENTE** | RTF-1a: `emisora.perteneceRegimenSimple == true` (ambas) | Sinco=false ✗ | sigue |
| | RTF-1b: `contraparte.perteneceRegimenSimple == true` (ambas) | ConsultorPro=false ✗ | sigue |
| | RTF-2b: `contraparte.esExentoRetefuente == true` (gasto) | ConsultorPro=false ✗ | sigue |
| | RTF-3b: `contraparte.esAutorretenedora == true` (gasto) | ConsultorPro=false ✗ | sigue |
| | RTF-4a: `contraparte.perteneceRegimenIVA == false` (gasto) | ConsultorPro=true ✗ | sigue |
| | (caso 5a-7a granC requieren `emisora.esGranContribuyente`) | Sinco=false ✗ | sigue |
| | (default — ninguna excluyó) | | **aplica** |
| **RIVA** | RIVA-1b: `contraparte.regimenIVA && emisora.esAgenteRetenedorIVA` (gasto) | ConsultorPro=true, Sinco=false ✗ | sigue |
| | RIVA-2b: `emisora.esAgenteRetenedorIVA == false` (gasto) | Sinco=false ✓ | **`noAplicar`** |

Nota: las condiciones IVA-1a, RTF-2a, RTF-3a, RTF-4b, RIVA-1a, RIVA-2a, RIVA-3 tienen `direccionFiscalAplicable: ingreso` y por tanto **no se evalúan** en este caso (gasto).

**Paso 5 — Calcular base × tarifa:**

- IVA: `1.000.000 × 19% = 190.000`
- RETEFUENTE: `1.000.000 × 4% = 40.000`

### 3.3. Resultado del motor

```
ResultadoCalculo (CASO A — gasto)
├── aplicados[]:
│     • IVA          base: 1.000.000  tarifa: 19%      valor: 190.000
│     • RETEFUENTE   base: 1.000.000  tarifa:  4%      valor:  40.000
└── descartados[]:
      • RIVA         motivoExclusion: perfil_no_aplica
```

### 3.4. Lectura contable (la deriva OXP, no el motor)

Con `direccionFiscal=gasto`:

- **IVA = soportado** (Sinco se lo paga al proveedor).
- **RETEFUENTE = practicada** (Sinco se la descuenta al proveedor y la consigna a la DIAN).

**Liquidación al proveedor:** `1.000.000 + 190.000 (IVA) − 40.000 (RETEFUENTE) = 1.150.000`

---

## 4. Caso B — Dirección `ingreso` (Sinco vende a MegaCorp vía CXC)

### 4.1. Solicitud al motor

```
direccionFiscal:           ingreso
entidadFiscalEmisora:      Sinco SAS    ← rol: facturadora
entidadFiscalContraparte:  MegaCorp SAS ← rol: cliente
fechaTransaccion:          2026-05-04
moneda:                    COP
conceptos[0]: {
  id:                      ING-001
  clasificacionTributaria: GRAV_19
  conceptoPago:            "Servicios de consultoría"
  monto:                   1.000.000
}
```

### 4.2. Flujo del motor

**Paso 1 — Resolver perfiles:** Sinco y MegaCorp a 2026-05-04.

**Paso 2 — Determinar tributos candidatos y filtrar por direccionFiscalAplicable:**
`CatalogoTributario.tributosAplicablesA(GRAV_19)` → `[IVA, RETEFUENTE, RIVA, ICA, RICA]`. ICA (`ingreso`) **sí pasa el filtro** en esta dirección; los demás tienen `ambas` — ninguno se descarta.

**Paso 3 — Filtrar condiciones por direccionFiscalAplicable contra `ingreso`:**
Solo se evalúan las condiciones con `direccionFiscalAplicable ∈ {ingreso, ambas}`. Las condiciones que aplicaban en gasto (Caso A) ahora se descartan; las simétricas de ingreso se evalúan.

**Paso 4 — Evaluar condiciones filtradas por tributo:**

| Tributo | Condición (solo las aplicables en ingreso) | Evaluación con perfiles | Resultado |
|---|---|---|---|
| **IVA** | IVA-1a: `emisora.perteneceRegimenIVA == true` (ingreso) | Sinco=true ✓ | **aplica** |
| **RETEFUENTE** | RTF-1a: `emisora.perteneceRegimenSimple == true` (ambas) | Sinco=false ✗ | sigue |
| | RTF-1b: `contraparte.perteneceRegimenSimple == true` (ambas) | MegaCorp=false ✗ | sigue |
| | RTF-2a: `emisora.esExentoRetefuente == true` (ingreso) | Sinco=false ✗ | sigue |
| | RTF-3a: `emisora.esAutorretenedora == true` (ingreso) | Sinco=false ✗ | sigue |
| | RTF-4b: `emisora.perteneceRegimenIVA == false` (ingreso) | Sinco=true ✗ | sigue |
| | (default) | | **aplica** |
| **RIVA** | RIVA-1a: `emisora.regimenIVA && contraparte.esAgenteRetenedorIVA` (ingreso) | Sinco=true ✓, MegaCorp=true ✓ | **aplica** |

Nota: las condiciones IVA-1b, RTF-2b, RTF-3b, RTF-4a, RIVA-1b, RIVA-2b, RIVA-3 tienen `direccionFiscalAplicable: gasto` y por tanto **no se evalúan** en este caso (ingreso). Esto demuestra que el motor evalúa **conjuntos diferentes de condiciones según la dirección**.

**Paso 5 — Calcular:**

- IVA: `1.000.000 × 19% = 190.000`
- RETEFUENTE: `1.000.000 × 4% = 40.000`
- RIVA: factor `porcentajeDePadre` → `190.000 × 15% = 28.500`

### 4.3. Resultado del motor

```
ResultadoCalculo (CASO B — ingreso)
└── aplicados[]:
      • IVA          base: 1.000.000  tarifa: 19%        valor: 190.000
      • RETEFUENTE   base: 1.000.000  tarifa:  4%        valor:  40.000
      • RIVA         base:   190.000  tarifa: 15%(padre) valor:  28.500
```

### 4.4. Lectura contable (la deriva CXC)

Con `direccionFiscal=ingreso`:

- **IVA = generado** (Sinco se lo cobra al cliente, queda como obligación con la DIAN).
- **RETEFUENTE = sufrida** (MegaCorp se la retiene a Sinco y la consigna por su cuenta).
- **RIVA = sufrida** (MegaCorp le retiene IVA a Sinco).

**Cobro al cliente:** `1.000.000 + 190.000 (IVA) − 40.000 (RETEFUENTE sufrida) − 28.500 (RIVA sufrida) = 1.121.500`

---

## 5. Comparativa lado a lado

```
Misma transacción ($1M consultoría, GRAV_19, 2026-05-04)
─ Mismas condiciones del catálogo
─ Mismos roles semánticos (emisora/contraparte)
─ Diferentes perfiles ocupando los roles → diferente resultado

                ┌─────────────────────────┬─────────────────────────┐
                │  gasto (OXP)            │  ingreso (CXC)          │
                │  emisora    = Sinco     │  emisora    = Sinco     │
                │  contraparte= ConsultorP│  contraparte= MegaCorp  │
─────────────────┼─────────────────────────┼─────────────────────────┤
 IVA            │  190.000  soportado     │  190.000  generado      │
 RETEFUENTE     │   40.000  practicada    │   40.000  sufrida       │
 RIVA           │  descartado             │   28.500  sufrida       │
─────────────────┴─────────────────────────┴─────────────────────────┘
 Liquidación    │  1.150.000 al proveedor │  1.121.500 del cliente  │
─────────────────┴─────────────────────────┴─────────────────────────┘
```

---

## 6. Conclusiones para el equipo de implementación

### 6.1. El motor evalúa conjuntos diferentes de condiciones según la dirección fiscal

A diferencia de un modelo "implícito" donde las condiciones serían universales y solo cambian los actores, el motor **filtra explícitamente** los tributos y condiciones por `direccionFiscalAplicable` antes de evaluarlas. Esto se ve en los pasos 2 y 3 del flujo:

- **Paso 2 (filtro de tributos):** descarta tributos cuya `Tributo.direccionFiscalAplicable` no incluya la dirección actual (ej: AUTO_RETEFUENTE no se evalúa en gasto).
- **Paso 3 (filtro de condiciones):** solo se evalúan las condiciones cuya `Condicion.direccionFiscalAplicable` sea compatible con la dirección actual.

Los conjuntos de condiciones evaluadas en `gasto` e `ingreso` son **distintos** — cada conjunto modela la perspectiva fiscal del agente correspondiente (retenedor en gasto, sujeto de retención en ingreso).

### 6.2. La diferencia numérica entre A y B la producen los perfiles + las condiciones direccionales

En el ejemplo, `RIVA` aparece en B y no en A por dos razones combinadas:
1. **Perfil:** ConsultorPro (gasto) y MegaCorp (ingreso) difieren en `esAgenteRetenedorIVA`.
2. **Condiciones direccionales:** la regla "aplica RIVA cuando el comprador es agente retenedor" se modela como dos condiciones (RIVA-1a para ingreso, RIVA-1b para gasto) — cada una se evalúa solo en su dirección.

La dirección entra al motor de manera **explícita** vía el filtrado, no implícita vía la reasignación de roles.

### 6.3. La interpretación contable la hace el consumidor, no el motor

El motor produce un desglose contablemente neutro: un conjunto de tributos con su base, tarifa y valor. La etiqueta semántica (**soportado vs generado**, **practicada vs sufrida**) la deriva el consumidor leyendo `direccionFiscal` del `RegistroTributario`. Por eso el `ContextoTransaccional` la persiste como dato inmutable (R24).

### 6.4. Roles `emisora`/`contraparte` como lenguaje fiscal del dominio

Los roles `emisora` y `contraparte` son **posicionales fiscales** (alineados con SAP Legal Entity, Oracle First/Third Party). `emisora` siempre representa a la entidad operadora del ERP (típicamente la empresa cliente; en facturación a nombre de terceros, el tercero gestionado). `contraparte` siempre es la otra parte.

Lo que cambia con la dirección NO es quién ocupa cada rol fiscal, sino el **rol comercial** que la emisora juega (adquiriente en gasto, facturadora en ingreso). Las condiciones del catálogo evalúan los roles posicionales, no los roles comerciales — el modelado de la asimetría comercial se hace mediante el filtrado por `direccionFiscalAplicable`.

Las reglas con semántica genuinamente bilateral (ej: régimen simple — RTF-1a y RTF-1b) tienen `direccionFiscalAplicable: ambas` y se modelan como condiciones individuales sobre `emisora` o `contraparte`. Las reglas con asimetría comercial se modelan como **pares duplicados** (una con `direccion=gasto` evaluando `contraparte`, otra con `direccion=ingreso` evaluando `emisora`).

### 6.5. ReverseCharge y direccionalidad inherente del tributo

Las autorretenciones, el reverseCharge y el ICA tienen **direccionalidad inherente** declarada explícitamente como invariante del agregado en `Tributo.direccionFiscalAplicable`:

| Tributo | `direccionFiscalAplicable` | Caso de uso |
|---|---|---|
| AUTO_RIVA | `gasto` | Reverse charge — importación de servicios |
| AUTO_RETEFUENTE | `ingreso` | Autoretención sobre ingresos propios |
| AUTO_RICA | `ingreso` | Autoretención de ICA |
| AUTO_RENTA | `ingreso` | Autoretención de renta |
| ICA | `ingreso` | Impuesto del ingreso: el sujeto pasivo es quien lo genera — en gasto el comprador solo practica RICA (issue #93) |

Ejemplo del caso reverseCharge (condición RIVA-3):

```
Tributo:                    RIVA  (Tributo.direccionFiscalAplicable: ambas)
AmbitoEvaluado:             emisora
Atributo:                   esAgenteRetenedorIVA
ValorEsperado:              true
Efecto:                     reverseCharge → activa AUTO_RIVA
direccionFiscalAplicable:   gasto
```

Si en el Caso A el perfil de Sinco fuera `esAgenteRetenedorIVA=true`, el motor descartaría `RIVA` y activaría `AUTO_RIVA` (caso típico: importación de servicios). **AUTO_RIVA tiene `direccionFiscalAplicable: gasto`** — por su invariante del agregado, no se calcula en ingreso, incluso si la condición RIVA-3 intentara dispararla.

**Semántica condicional de reverseCharge:** si el tributo alternativo (`AUTO_RIVA`) no es aplicable en la dirección actual (filtrado por su `direccionFiscalAplicable`), el tributo original (`RIVA`) continúa su evaluación normal. Esto garantiza coherencia: la condición RIVA-3 solo se evalúa en gasto, y el reemplazo `RIVA → AUTO_RIVA` ocurre solo cuando ambos son aplicables.

### 6.6. Persistencia obligatoria en el RegistroTributario

El `RegistroTributario` debe conservar `direccionFiscal` dentro de su `ContextoTransaccional` (Sección 3.11 del modelo). Sin ese dato, los reportes de exógena, los certificados de retención y la clasificación contable de IVA (descontable vs generado) son irreproducibles.

---

## 7. Trazabilidad con el modelo

| Concepto del ejemplo | Referencia en el modelo |
|---|---|
| Contrato de entrada del motor | `MotorDeCalculo`, contrato `[D9]`, Sección 3.12 |
| `direccionFiscal` campo obligatorio | R30 (alcance) |
| Dirección fiscal explícita en Tributo y Condicion | `[D2]` refinada, Sección "Decisiones de diseño" |
| `Tributo.direccionFiscalAplicable` (invariante del agregado) | `CatalogoTributario`, Sección 3.2 — entidad `Tributo` |
| `Condicion.direccionFiscalAplicable` (direccionalidad de la regla) | `CondicionDeAplicacion`, Sección 3.4 — entidad `Condicion` |
| Filtrado de tributos por dirección antes de evaluar | Paso 2.a del Motor, Sección 3.12 |
| Filtrado de condiciones por dirección antes de evaluar | Paso 2.c del Motor, Sección 3.12 |
| `tributosAplicablesA(clasificacion)` | `CatalogoTributario`, Sección 3.2 |
| `tarifaVigenteA(factor, fecha)` | `TarifaTributaria`, Sección 3.3 |
| `perfilCompletoA(fecha)` | `PerfilTributario`, Sección 3.6 |
| Persistencia de `direccionFiscal` en el registro | `RegistroTributario.ContextoTransaccional`, Sección 3.11, R24 |
| Comportamiento del tributo según dirección | R11, P2 (alcance) |
| Semántica condicional de `reverseCharge` | VO `Efecto` en `CondicionDeAplicacion`, Sección 3.4 |
| `reverseCharge` y autorretenciones | RIVA-3 + autorretenciones, `anexo-configuracion-estandar-co.md` |

---

## 8. Limitaciones del ejemplo

- **Tributos municipales (ICA/RICA) omitidos** para mantener legibilidad. En un cálculo real, la `ReglaDeLocalizacion` resuelve el `lugarEjecucion` y se evalúan condiciones específicas por municipio (ver `anexo-configuracion-estandar-co.md`, sección RICA).
- **No se ilustra el flujo de desgravamen.** Para notas crédito y devoluciones, el motor **no recalcula** — se prorratea el desglose del `RegistroTributario` origen (R39, F1). La dirección fiscal se hereda del registro origen.
- **No se ilustran condiciones personalizadas.** El ejemplo asume que solo existen condiciones de `origen=estándar`. Si el cliente registra una condición personalizada para la misma combinación (atributo + tributo), aplica la personalizada por precedencia (R35).
