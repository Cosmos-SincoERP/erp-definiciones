# Anexo — Marco contable y arquitectura del PUC

> **Fecha:** Mayo 2026
> **Propósito:** Documentar la decisión sobre cómo se modelan los planes de cuentas (PUC), los libros contables y los marcos contables, con base en la convergencia de la industria hacia "Chart of Accounts único + libros paralelos sobre el mismo COA". Este anexo respalda la introducción del agregado `MarcoContable` en el modelo y el replanteamiento de los libros predeterminados.
> **Versión:** 1.0

---

## 1. Propósito y alcance

Este anexo cubre tres preguntas que surgieron al revisar el modelo de Contabilidad con el equipo de desarrollo y diseño:

1. ¿Cómo se identifica formalmente un plan de cuentas? ¿Es por nombre libre o existe un atributo estructurado que indique a qué marco normativo o esquema contable pertenece?
2. ¿Qué relación existe entre el plan de cuentas y el libro contable? ¿Por qué los tipos del libro (Principal, Local, NIIF, Gerencial) parecían ser tipos del plan de cuentas?
3. ¿Cómo se modela una empresa moderna que opera bajo NIIF y utiliza un solo plan de cuentas con varios libros (Principal, Fiscal) sobre el mismo PUC?

El anexo documenta la investigación de la industria que justifica la decisión adoptada y deja explícita la separación entre los tres conceptos: `PlanDeCuentas`, `LibroContable` y `MarcoContable`.

**Este anexo no cubre:**
- La estructura interna del PUC (cuentas maestras y auxiliares, obligatoriedad de tercero y unidad organizacional, naturaleza inversa). Esos temas están en otros anexos del sub-dominio.
- El detalle del agregado `EquivalenciaPuc` (sección 3.12 del modelo).
- La construcción de F2 (sistema contable propio). El anexo describe la decisión arquitectónica, no la implementación de F2.

---

## 2. El problema

Tres síntomas concretos motivaron este replanteamiento:

### Confusión conceptual entre PUC, Libro y Marco

El sistema legacy (SincoA&F y similares) fusiona en un solo concepto "PUC" lo que el modelo nuevo separa en agregados independientes:

```
Sistema legacy:                   Modelo nuevo:
┌──────────────────┐              ┌─────────────────┐  ┌──────────────┐  ┌──────────────────┐
│  PUC: Principal  │   se separa  │ PlanDeCuentas   │ +│ LibroContable│ +│ MarcoContable    │
│  (selector único)│   ────────▶  │ (las cuentas)   │  │ (libro+rol)  │  │ (clasificación)  │
│  + cuentas       │              │                 │  │              │  │                  │
└──────────────────┘              └─────────────────┘  └──────────────┘  └──────────────────┘
```

El equipo de desarrollo, al revisar el modelo nuevo, asumió que "Principal/Local/NIIF/Gerencial" eran tipos del PUC — porque así lo modela el legacy. La realidad del modelo nuevo es que esos valores corresponden al atributo `tipo` del agregado `LibroContable`, no del `PlanDeCuentas`.

### Identificación del PUC por texto libre

En el modelo previo, el `PlanDeCuentas` se identificaba por `empresa + nombre` (texto libre). Esto generaba ambigüedades:

- "PUC Colombia", "PUC COL", "Plan Colombia" — son la misma cosa pero el sistema no lo sabe.
- No existía atributo formal que indicara qué marco normativo seguía cada PUC.
- Las referencias entre agregados (`LibroContable.pucAsociado`, `EquivalenciaPuc.pucOrigen`) eran ambiguas: ¿son por id o por nombre?

### Modelo de transición a NIIF heredado

La práctica colombiana de los años 2010, durante la transición a NIIF, modelaba tres PUCs paralelos por empresa:

- **Local** — solo para reportes fiscales bajo PCGA local.
- **Principal** — homologación entre Local y NIIF.
- **NIIF** — solo para ajustes específicos NIIF.

Las equivalencias cuenta a cuenta entre los tres permitían generar reportes en cualquier marco. Era un modelo de transición. Las empresas modernas ya no operan así.

---

## 3. Cómo se modelaba antes (legacy)

### Arquitectura típica de la transición

```
Empresa COSMOS-SAS (sistema legacy 2010-2015)

  PUC Local         PUC Principal       PUC NIIF
  (Decreto 2650)    (homologador)       (NIC/NIIF)
       │                  │                  │
       ▼                  ▼                  ▼
  Libro Local       Libro Principal     Libro NIIF
  (reportes         (reportes           (ajustes
   fiscales)         consolidados)       NIIF-only)

  + Equivalencias cuenta a cuenta entre los tres PUCs
```

**Características del modelo legacy:**

- Cada PUC duplicaba (en distinta granularidad) el catálogo de cuentas.
- Las equivalencias requerían mantenimiento manual y sincronización.
- El cierre de mes implicaba reconciliar tres ledgers contra los movimientos.
- El sistema no podía garantizar consistencia entre los tres PUCs sin intervención humana.

**Por qué se hacía así:**

- Durante la transición a NIIF (2010-2015 en Colombia), las empresas tenían que reportar bajo dos marcos simultáneamente: el local (Decreto 2650) y NIIF.
- Los ERPs locales no soportaban el patrón moderno de "ledgers paralelos sobre un solo COA". La solución pragmática fue duplicar PUCs.
- Una vez completada la transición a NIIF, las empresas siguieron operando con la arquitectura heredada por inercia.

---

## 4. Cómo lo hacen los ERPs modernos

Investigación de seis plataformas líderes de la industria. Todas convergen hacia el mismo patrón: **un Chart of Accounts único + libros/ledgers/layers/books paralelos sobre el mismo COA**.

### Tabla comparativa

| ERP | Concepto principal | Cómo modela los marcos paralelos | Tendencia oficial |
|-----|---------------------|------------------------------------|-------------------|
| **SAP S/4HANA** | Universal Journal (`ACDOCA`) — una sola tabla unificada para todos los ledgers | Un solo Chart of Accounts global. Múltiples Ledgers paralelos: Leading Ledger (típicamente IFRS), Non-Leading Ledgers (Local GAAP, Tax, Group). Cada ledger se asocia a un Accounting Principle. Document Splitting automático: una transacción se postea simultáneamente en todos los ledgers, con ajustes específicos donde la norma lo exige. | Eliminar la noción de "varios planes de cuentas" como práctica recomendada. Un COA, ledgers paralelos. |
| **Oracle Fusion ERP Cloud** | Subledger Accounting (SLA) + Primary y Secondary Ledgers | Recomendación oficial: usar el mismo Chart of Accounts entre Primary y Secondary Ledgers, diferenciando solo el accounting method (IFRS vs Tax vs Local GAAP). Reporting Currencies para perspectivas adicionales sin necesidad de ledgers separados. | "Minimize the number of secondary ledgers; prefer reporting currencies and adjustment ledgers over duplicate COAs." |
| **Microsoft Dynamics 365 Finance** | Posting Layers | Capas independientes (Current, Operations, Tax, Fiscal) sobre el mismo plan de cuentas. Una transacción puede postearse en múltiples capas simultáneamente con valores distintos. Reportes regulatorios y fiscales se generan filtrando por capa, no consultando otro COA. | Las "capas" reemplazan completamente la idea de PUCs paralelos. |
| **NetSuite** | Multi-Book Accounting | Primary Book y Secondary Books. En la práctica común actual, los Secondary Books usan el mismo Chart of Accounts que el Primary, con reglas de ajuste para diferencias. Adjustment-Only Books son cada vez más populares: solo registran las diferencias entre tratamientos, no duplican el saldo completo. | Adjustment-only books como patrón de modernización. |
| **Workday Financials** | Single ledger architecture | Un solo libro mayor con dimensiones múltiples. "Ledger" no es una entidad separada — es una dimensión sobre la transacción. Reportes multi-marco se generan por filtros y reglas, no por estructuras paralelas. | Arquitectura "single ledger" explícita. |
| **Sage Intacct / Acumatica / Odoo** | COA único con dimensiones / etiquetas / journals | Convergencia similar: COA único con etiquetas, dimensiones o journals para diferenciar tratamientos. Catálogos paralelos de cuentas son legacy, no práctica recomendada. | Coherente con SAP/Oracle/Dynamics. |

### Patrón común identificado

| Elemento | Forma común en ERPs modernos |
|----------|--------------------------------|
| Chart of Accounts | Único por compañía |
| Múltiples marcos contables | Modelados como ledgers/books/layers paralelos sobre el mismo COA |
| Diferencias entre marcos | Asientos específicos del ledger (no cuentas distintas) |
| Reportes multi-marco | Filtros sobre dimensiones, no joins entre estructuras paralelas |
| Reconciliación | Trivial (mismo COA → diferencias = suma de ajustes) |

---

## 5. Razones de la convergencia

Cinco causas explican por qué la industria moderna abandonó el modelo de PUCs paralelos:

| # | Razón | Explicación |
|---|-------|-------------|
| 1 | **Convergencia normativa hacia NIIF** | NIIF es el marco contable de referencia global. Las jurisdicciones locales convergen, y las diferencias se reducen a ajustes puntuales (depreciación tributaria, ingresos diferidos fiscales, etc.). No tiene sentido modelar "todo el universo" de cuentas dos veces. |
| 2 | **Reducción de complejidad de mantenimiento** | Mantener N PUCs requiere sincronizarlos, lo que es mecánicamente complejo y propenso a errores. Un COA con ledgers paralelos elimina ese problema. |
| 3 | **Trazabilidad inmediata** | Una transacción afecta varios ledgers desde un solo evento → la trazabilidad emisor → contabilización → reporte regulatorio es directa, sin duplicación. |
| 4 | **Reportes simplificados** | Filtros sobre dimensiones (ledger, posting layer, accounting principle) en lugar de joins entre estructuras paralelas. |
| 5 | **Reconciliación automática** | Si todos los ledgers usan el mismo COA, la diferencia entre Principal y Fiscal es siempre la suma de los ajustes registrados — trivial de reconciliar. |

---

## 6. Realidad operativa colombiana actual

Consultas con consultores especializados en sistemas contables coinciden con la convergencia global:

- **Las empresas modernas operan con un solo PUC** estructurado bajo NIIF (NIIF Plenas o NIIF para Pymes según el grupo de la empresa).
- **Las mismas cuentas se afectan en todos los libros.** No hay duplicación de catálogos.
- **Las diferencias fiscales se modelan como ajustes específicos en el libro fiscal**, no como cuentas separadas. Por ejemplo, una empresa registra la depreciación contable bajo NIIF en su libro Principal y registra la depreciación tributaria adicional como un ajuste en el libro Fiscal — ambos sobre las mismas cuentas del PUC NIIF.
- **Los libros Principal, Fiscal, Gerencial apuntan al mismo PUC.** Lo que cambia entre ellos son las reglas de afectación que aplica cada uno.

Esta práctica es exactamente lo que SAP llama "Multiple Ledgers, Single Chart of Accounts" y Dynamics llama "Posting Layers". La práctica colombiana evolucionó en paralelo con la industria global.

---

## 7. Decisión adoptada

Cuatro elementos componen la decisión:

### 7.1 Tres agregados independientes

El modelo de Contabilidad mantiene tres agregados separados, cada uno con su responsabilidad clara:

| Agregado | Responsabilidad | Nivel |
|----------|------------------|-------|
| `PlanDeCuentas` | Catálogo jerárquico de cuentas (maestras y auxiliares). Estructura del COA. | N1 |
| `LibroContable` | Configuración de un conjunto de registros contables con su rol operativo (Principal, Fiscal, Gerencial, etc.) y referencia al PUC asociado. | N2 |
| **`MarcoContable`** *(nuevo)* | Identificación formal del esquema bajo el cual se diseña un PUC (NIIF, marcos locales, gerencial, consolidación, etc.). | N1 |

### 7.2 Caso típico moderno

Una empresa típica al onboardear opera con:

```
Empresa COSMOS-SAS

  MarcoContable:    NIIF
                     │
                     ▼
  PlanDeCuentas:    PUC NIIF (referencia el marco NIIF)
                     │
        ┌────────────┴─────────────┐
        ▼                          ▼
  LibroContable:  Principal   LibroContable:  Fiscal
  (asienta la operación        (asienta los ajustes
   bajo NIIF)                   específicos para reportes
                                 fiscales)
```

**Las diferencias entre Principal y Fiscal se modelan como asientos específicos del libro fiscal (regla R34 del alcance), no como PUCs paralelos.**

### 7.3 Predeterminado del producto al onboardear empresa

El producto precarga **un solo marco contable: NIIF**. Este marco se crea automáticamente al onboardear la empresa, junto con un PUC NIIF y los dos libros predeterminados (Principal y Fiscal) asociados a ese PUC.

### 7.4 Marcos custom por usuario con permiso especial

Cuando una empresa requiere un marco contable adicional (consolidación de grupo, fiscal alterno, sectorial), un usuario con permiso especial puede crearlo. La política de creación de marcos custom es controlada — no es auto-servicio del cliente.

---

## 8. Estructura del agregado `MarcoContable`

### Atributos

| Atributo | Tipo | Detalle |
|----------|------|---------|
| `codigo` | string | Único por empresa, estable, inmutable. Ejemplo: `NIIF`. |
| `nombre` | string | Texto descriptivo presentable, en idioma del país de la empresa. |
| `descripcion` | string opcional | Texto largo con contexto del marco. |
| `estado` | activo / inactivo | Permite desactivar marcos obsoletos sin borrar datos. |

### Eventos (4)

| # | Evento | Información capturada |
|---|--------|------------------------|
| 1 | `MarcoContableCreado` | empresa, codigo, nombre, descripcion (estado nace activo) |
| 2 | `MarcoContableModificado` | codigo (identifica), nombre y/o descripcion modificados |
| 3 | `MarcoContableDesactivado` | codigo, motivo |
| 4 | `MarcoContableReactivado` | codigo |

### Invariantes

| ID | Invariante | Tipo |
|----|------------|------|
| I28 | El código de un MarcoContable es único por empresa. | Local |
| I29 | El código de un MarcoContable es inmutable tras creación. | Local |
| I30 | Una empresa no puede tener dos PlanDeCuentas referenciando el mismo MarcoContable. | Eventual |
| I31 | El MarcoContable referenciado por un PlanDeCuentas debe estar activo al momento de crear el PUC. | Eventual |
| I32 | El MarcoContable referenciado por un PlanDeCuentas es inmutable tras la creación del PUC. | Local |

### Política de catálogo

- **Predeterminado:** el marco `NIIF` se crea automáticamente al onboardear la empresa, junto con su PUC NIIF y los libros Principal y Fiscal.
- **Custom:** un usuario con permiso especial puede crear marcos adicionales según las necesidades de la empresa.
- **Desactivación:** desactivar un MarcoContable previene crear nuevos PUCs sobre ese marco. **No hay cascada** sobre los PUCs existentes — siguen operando normalmente.

---

## 9. Casos excepcionales

*Esta sección es informativa. Documenta situaciones reales donde una empresa puede requerir más de un marco contable o más de un PUC, sin dictar política operativa específica.*

### 9.1 Empresas en transición a NIIF

Empresas que aún operan bajo el modelo legacy de tres PUCs paralelos (Local, Principal, NIIF) durante una transición. Son situaciones temporales que tienden a converger al modelo de un solo PUC NIIF al cerrar la transición.

### 9.2 Sectores regulados con PUC sectorial

Algunas empresas pertenecen a sectores donde la autoridad fiscal o regulatoria impone un PUC distinto al estándar comercial. Ejemplos en Colombia:

- **Sector financiero:** "Plan Único de Cuentas para Entidades Vigiladas por la Superintendencia Financiera de Colombia (SFC)".
- **Sector salud:** "Plan Único de Cuentas para Entidades del Sector Salud" — Supersalud.
- **Sector solidario:** "Plan Único de Cuentas para Entidades del Sector Solidario" — Supersolidaria.

Cada uno de estos es un marco normativo distinto al PUC comercial general; las empresas reguladas deben aplicarlo. En el modelo nuevo se modelarían como marcos custom adicionales al NIIF predeterminado.

### 9.3 Grupos empresariales con consolidación

Grupos que consolidan estados financieros entre subsidiarias requieren un marco específico para la consolidación, separado del marco operativo de cada subsidiaria. En el modelo nuevo se modela como un marco custom (`CONSOLIDACION_GRUPO_X`).

### 9.4 Empresas con PUC fiscal alterno

Algunas empresas mantienen un PUC paralelo solo para reportes a la autoridad fiscal (información exógena, declaraciones), con detalle distinto al PUC operativo. En el modelo nuevo se modelaría como un marco custom (`FISCAL_ALTERNO`).

---

## 10. Implicaciones operativas

### 10.1 Identificación de PUCs por id, no por nombre

Las referencias entre agregados se hacen por **id estable del agregado**, no por nombre libre:

- `LibroContable.pucAsociado` → referencia el id del `PlanDeCuentas`.
- `EquivalenciaPuc.pucOrigen` y `EquivalenciaPuc.pucDestino` → referencias por id.

El nombre del PUC es texto libre presentable al usuario, pero las relaciones del modelo se basan en identificadores estables. Esto cierra la zona gris que existía antes (los ejemplos del modelo mostraban "PUC Colombia" como referencia, lo cual era ambiguo).

### 10.2 `BorradorContable` y `AsientoContable` no persisten `marcoContable`

El marco contable es derivable a través de la cadena `BorradorContable → libro destino → PlanDeCuentas → MarcoContable`. Por eso no se persiste como atributo en el borrador ni en el asiento.

Las **proyecciones de reportes** (`auxiliar contable`, `saldos contables`) son una decisión separada: si surge la necesidad de filtrar reportes por marco contable, se evaluará en su momento si conviene denormalizar el atributo en las proyecciones para optimizar la lectura. Hoy no se hace.

### 10.3 `EquivalenciaPuc` y F2

El agregado `EquivalenciaPuc` permanece en el modelo. La arquitectura predeterminada moderna (un PUC NIIF compartido por todos los libros) **no requiere equivalencias** — los libros usan las mismas cuentas. `EquivalenciaPuc` sería necesario únicamente en los casos excepcionales descritos en la sección 9.

Como `LibroContable` y `EquivalenciaPuc` son **capacidades de F2** — no de F1 — la necesidad efectiva de `EquivalenciaPuc` se evaluará cuando se aborde la construcción de F2, con base en los casos reales que surjan en producción. F1 no se ve afectado por esta decisión.

### 10.4 Sin cascada al desactivar marcos

Cuando se desactiva un `MarcoContable`, los `PlanDeCuentas` que lo referencian no se afectan: siguen operativos, los borradores y asientos siguen su flujo normal. La desactivación solo previene crear nuevos PUCs sobre ese marco. Esta política preserva el histórico contable, alineada con la regla R07 (datos maestros activos) del alcance.

### 10.5 Atributo `tipo` del `LibroContable` como texto libre

El atributo `tipo` del `LibroContable` deja de ser un enum cerrado (Principal/Local/NIIF/Gerencial) y pasa a ser texto libre con predeterminados sugeridos por el producto: `Principal` y `Fiscal`. El analista contable puede crear libros adicionales con tipos distintos (Gerencial, Consolidación, sectoriales) según las necesidades de la empresa.

Esta apertura del enum es coherente con la decisión de modelar marcos custom: ambos catálogos (marcos y tipos de libro) son extensibles por empresa, no cerrados al producto.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Mayo 2026 | Versión inicial. Investigación de seis ERPs modernos (SAP S/4HANA, Oracle Fusion, Dynamics 365, NetSuite, Workday, Sage). Decisión sobre arquitectura PUC + Libro + MarcoContable. Predeterminado NIIF como único marco precargado al onboardear empresa. Libros predeterminados: Principal y Fiscal sobre el mismo PUC NIIF. `EquivalenciaPuc` permanece para casos excepcionales (transición, sectores regulados, consolidación) — su necesidad se evaluará al construir F2. Atributo `tipo` del `LibroContable` pasa de enum cerrado a texto libre. Acompaña actualización de `modelo-dominio.md` v1.2 (nuevo agregado `MarcoContable`, invariantes I28-I32, decisión D11, premisa P5 actualizada) y `definicion-alcance.md` v1.2 (glosario actualizado, nueva regla R46, capacidad F2 reformulada). |
