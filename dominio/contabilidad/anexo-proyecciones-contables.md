# Anexo — Proyecciones contables: auxiliar y saldos

> **Fecha:** Marzo 2026
> **Propósito:** Documentar por qué el sub-dominio de Contabilidad requiere dos proyecciones principales y cómo cada reporte se alimenta de una u otra según su naturaleza.
> **Versión:** 1.0

---

## 1. El problema: detalle vs. agregación

Los reportes contables tienen dos naturalezas fundamentalmente diferentes:

| Naturaleza | Qué necesita | Ejemplo de reporte |
|------------|-------------|-------------------|
| **Detalle** | Cada movimiento individual: fecha, comprobante, cuenta, tercero, unidad organizacional, débito/crédito | Auxiliar por cuenta, auxiliar por tercero, libro diario |
| **Saldos** | Totales acumulados por combinación de dimensiones en un periodo | Balance de prueba, estados financieros, balance por unidad organizacional |

Una sola fuente de datos no puede servir eficientemente a ambas naturalezas:

- Si solo se persiste el **detalle**, los reportes de saldos requieren agregar potencialmente millones de filas en cada consulta. Con volumen alto, esto degrada el rendimiento de forma impredecible.
- Si solo se persisten los **saldos**, los reportes de detalle no pueden construirse — se perdió la granularidad de cada movimiento.

Por esta razón, el sistema requiere **dos proyecciones principales** que se alimentan del mismo origen (los eventos de los asientos contabilizados) pero sirven a propósitos diferentes.

---

## 2. Proyección 1 — Auxiliar contable (detalle)

Registra cada partida individual de cada asiento contabilizado, con la equivalencia de PUC ya resuelta y congelada al momento de la contabilización.

### Estructura

| Campo | Descripción |
|-------|-------------|
| `comprobante` | Identificador del comprobante contable (ej: CP-202603-0047) |
| `fecha` | Fecha del hecho económico |
| `libroOrigen` | Libro en el que se registró el asiento (Principal, Fiscal u otros tipos custom de la empresa) |
| `libroPresentacion` | Libro para el cual se materializa esta entrada. Un asiento del libro Principal genera entradas para todos los libros cuyos PUCs tengan equivalencia configurada. |
| `cuenta` | Cuenta auxiliar resuelta para el PUC del libro de presentación. Congelada al momento de contabilizar — cambios futuros de equivalencia no afectan entradas históricas. |
| `tercero` | Identificación del tercero (tipo y número) |
| `unidadOrganizacional` | Código del destino de negocio |
| `debito` | Valor al débito (si aplica) |
| `credito` | Valor al crédito (si aplica) |
| `referenciaOrigen` | Referencia técnica al hecho económico del sub-dominio consumidor que originó el asiento |
| `documentoFuente` | Identificador del documento que origina el asiento (número de factura, número de obligación, número de pago, etc.). Es lo que el usuario ve en el auxiliar como columna de referencia. |

### Ejemplo

```
Auxiliar contable (extracto — libro de presentación: NIIF, marzo 2026)
┌──────────┬────────┬──────────┬────────┬──────────────┬────────┬───────────┬─────────┬─────────┬─────────┐
│Comprobante│ Fecha │Libro orig│Lib pres│ Doc. fuente   │ Cuenta │ Tercero   │ Und.Org │ Débito  │ Crédito │
├──────────┼────────┼──────────┼────────┼──────────────┼────────┼───────────┼─────────┼─────────┼─────────┤
│ CP-047   │ 15-mar │Principal │ NIIF   │ OXP-COM-5678 │ 510105 │ 900123456 │ VTA-001 │ 600.000 │         │
│ CP-047   │ 15-mar │Principal │ NIIF   │ OXP-COM-5678 │ 510105 │ 900123456 │ ADM-001 │ 400.000 │         │
│ CP-047   │ 15-mar │Principal │ NIIF   │ OXP-COM-5678 │ 240801 │ 900123456 │ VTA-001 │ 114.000 │         │
│ CP-047   │ 15-mar │Principal │ NIIF   │ OXP-COM-5678 │ 236505 │ 900123456 │ VTA-001 │         │  66.000 │
│ CP-047   │ 15-mar │Principal │ NIIF   │ OXP-COM-5678 │ 220501 │ 900123456 │ —       │         │1.080.000│
│ CD-012   │ 18-mar │ NIIF     │ NIIF   │ —            │ 160501 │ 800555444 │ ADM-001 │5.000.000│         │
└──────────┴────────┴──────────┴────────┴──────────────┴────────┴───────────┴─────────┴─────────┴─────────┘
```

### Filtros principales

```
Auxiliar contable — Filtros disponibles:

  "Todo el libro NIIF"
    → WHERE libro_presentacion = 'NIIF'
    → Todas las entradas: proyectadas desde Principal + propias de NIIF

  "Solo lo registrado en NIIF"
    → WHERE libro_presentacion = 'NIIF' AND libro_origen = 'NIIF'
    → Solo entradas de asientos registrados directamente en el libro NIIF

  "Solo lo proyectado al NIIF"
    → WHERE libro_presentacion = 'NIIF' AND libro_origen != 'NIIF'
    → Solo entradas proyectadas desde otros libros

  "Movimientos de una cuenta específica en NIIF"
    → WHERE libro_presentacion = 'NIIF' AND cuenta = '510105'

  "Movimientos de un tercero en Principal"
    → WHERE libro_presentacion = 'Principal' AND tercero = '900123456'
```

### Comportamiento

- Se alimenta escuchando los eventos de contabilización de asientos.
- Por cada asiento contabilizado, materializa una entrada por partida por libro de presentación.
- La equivalencia de PUC se resuelve al escribir y queda congelada. Los reportes históricos siempre reflejan la equivalencia vigente al momento de contabilizar.
- Las anulaciones generan nuevas entradas (con los valores invertidos del asiento inverso). No se modifican ni eliminan entradas existentes.

---

## 3. Proyección 2 — Saldos contables (agregada por dimensiones)

Acumula los totales de débitos, créditos y saldo neto por cada combinación de dimensiones relevantes en un periodo. Se actualiza incrementalmente con cada nuevo asiento — no requiere recalcular desde el detalle.

### Estructura

| Campo | Descripción |
|-------|-------------|
| `libroOrigen` | Libro en el que se registró el asiento. Permite filtrar entre saldos de asientos propios del libro vs saldos proyectados desde otro libro. |
| `libroPresentacion` | Libro para el cual se acumula el saldo |
| `cuenta` | Cuenta auxiliar (resuelta para el PUC del libro de presentación) |
| `tercero` | Identificación del tercero |
| `unidadOrganizacional` | Código del destino de negocio |
| `periodo` | Periodo contable (ej: 2026-03) |
| `debitos` | Suma acumulada de débitos en el periodo |
| `creditos` | Suma acumulada de créditos en el periodo |
| `saldo` | Débitos - Créditos del periodo |

### Ejemplo

```
Saldos contables (extracto — libro de presentación: NIIF, marzo 2026)
┌──────────┬────────────┬─────────┬───────────┬─────────┬─────────┬────────────┬────────────┬───────────┐
│Libro orig│Lib pres.   │ Cuenta  │ Tercero   │ Und.Org │ Periodo │ Débitos    │ Créditos   │ Saldo     │
├──────────┼────────────┼─────────┼───────────┼─────────┼─────────┼────────────┼────────────┼───────────┤
│Principal │ NIIF       │ 510105  │ 900123456 │ VTA-001 │ 2026-03 │  1.450.000 │    300.000 │ 1.150.000 │
│Principal │ NIIF       │ 510105  │ 900123456 │ ADM-001 │ 2026-03 │    800.000 │          0 │   800.000 │
│Principal │ NIIF       │ 240801  │ 900123456 │ VTA-001 │ 2026-03 │    456.000 │     57.000 │   399.000 │
│Principal │ NIIF       │ 236505  │ 900123456 │ VTA-001 │ 2026-03 │     33.000 │    264.000 │  -231.000 │
│Principal │ NIIF       │ 220501  │ 900123456 │ —       │ 2026-03 │    324.000 │  3.240.000 │-2.916.000 │
│ NIIF     │ NIIF       │ 160501  │ 800555444 │ ADM-001 │ 2026-03 │  5.000.000 │          0 │ 5.000.000 │
└──────────┴────────────┴─────────┴───────────┴─────────┴─────────┴────────────┴────────────┴───────────┘
```

### Filtros principales

```
Saldos contables — Filtros disponibles:

  "Todo el libro NIIF"
    → WHERE libro_presentacion = 'NIIF'
    → 6 filas (todos los saldos: proyectados + propios)

  "Solo registrado en NIIF"
    → WHERE libro_presentacion = 'NIIF' AND libro_origen = 'NIIF'
    → 1 fila (solo cuenta 160501 — ajuste NIIF-only)

  "Solo proyectado al NIIF"
    → WHERE libro_presentacion = 'NIIF' AND libro_origen != 'NIIF'
    → 5 filas (saldos de asientos del Principal con equivalencia)

  "Balance de prueba de un periodo"
    → WHERE libro_presentacion = 'Principal' AND periodo = '2026-03'

  "Saldo de una cuenta específica"
    → WHERE libro_presentacion = 'NIIF' AND cuenta = '510105' AND periodo = '2026-03'

  "Saldo por unidad organizacional"
    → WHERE libro_presentacion = 'Principal' AND unidad_organizacional = 'VTA-001'
```

### Comportamiento

- Se alimenta escuchando los mismos eventos de contabilización que el auxiliar contable.
- Por cada partida del asiento, **suma incrementalmente** al registro de saldo correspondiente (misma combinación de libro origen + libro presentación + cuenta + tercero + unidad organizacional + periodo). Si no existe, lo crea.
- Las anulaciones restan del saldo (las partidas inversas se suman con sus valores invertidos).
- No almacena detalle de movimientos individuales — solo totales por combinación de dimensiones.

---

## 4. ¿Por qué `libroPresentacion` y no `pucDestino`?

Se evaluaron dos diseños para identificar a qué libro pertenece cada entrada en las proyecciones:

### Diseño evaluado — `pucDestino`

Cada entrada se identifica por el PUC al que pertenece la cuenta. Como múltiples libros pueden compartir el mismo PUC, la relación libro → PUC se resuelve al momento de consultar.

```
Ejemplo: Principal y Fiscal comparten PUC NIIF.

  Entrada: { puc_destino: 'PUC NIIF', cuenta: 5110-05-002, ... }

  Consulta "libro Principal" → buscar qué PUC usa Principal
                              → PUC NIIF
                              → WHERE puc_destino = 'PUC NIIF'
```

**Problema:** La proyección no está lista para consumir. Cada consulta requiere un paso previo de interpretación (resolver libro → PUC) antes de poder filtrar. En ES/CQRS, una proyección se optimiza para la lectura — debe responder directamente sin transformaciones intermedias. Si la proyección necesita un intérprete, no está cumpliendo su propósito.

### Diseño adoptado — `libroPresentacion`

Cada entrada se identifica directamente por el libro para el cual fue materializada. Si dos libros comparten el mismo PUC, se generan entradas para ambos (con las mismas cuentas y montos).

```
Ejemplo: Principal y Fiscal comparten PUC NIIF.

  Entrada 1: { libro_presentacion: 'Principal', cuenta: 5110-05-002, ... }
  Entrada 2: { libro_presentacion: 'Fiscal',    cuenta: 5110-05-002, ... }

  Consulta "libro Principal" → WHERE libro_presentacion = 'Principal'
  Consulta "libro Fiscal"    → WHERE libro_presentacion = 'Fiscal'
```

**Ventaja:** Consulta directa. Sin lookups, sin intérpretes. El sistema resolvió la lógica de equivalencia y asignación de libros al momento de escribir la proyección, no al momento de leerla.

### Comparación

| Criterio | `pucDestino` | `libroPresentacion` |
|----------|:---:|:---:|
| Consulta directa por libro | No — requiere lookup libro → PUC | **Sí** |
| Duplicación cuando libros comparten PUC | No | Sí (entradas idénticas con distinto libro) |
| Lógica de resolución | En cada lectura | **Una sola vez al escribir** |
| Independencia ante cambios de configuración | Si Principal y Fiscal se separan a PUCs diferentes, las consultas cambian | **Las entradas históricas quedan correctas** — cada una ya tiene su libro asignado |
| Principio ES/CQRS | Viola: proyección que necesita intérprete | **Cumple: proyección lista para consumir** |

### Decisión

Se adopta `libroPresentacion` como atributo de las proyecciones. La duplicación de entradas entre libros que comparten PUC es el costo aceptable de tener proyecciones que responden directamente. La fuente de verdad son los eventos de los asientos — las proyecciones se pueden reconstruir en cualquier momento.

---

## 5. Direccionamiento de reportes

Cada reporte del sistema se alimenta de una de las dos proyecciones según su naturaleza:

### Reportes de detalle → Auxiliar contable

| Reporte | Filtros principales | Fuente |
|---------|-------------------|--------|
| **Auxiliar por cuenta** | libro_presentacion + cuenta + rango de fechas | Auxiliar contable |
| **Auxiliar por tercero** | libro_presentacion + tercero + rango de fechas | Auxiliar contable |
| **Libro Diario** | libro_presentacion + rango de fechas (ordenado cronológicamente) | Auxiliar contable |
| **Consulta de comprobante** | comprobante | Auxiliar contable |
| **Movimientos por unidad organizacional** | libro_presentacion + unidad_organizacional + rango de fechas | Auxiliar contable |

### Reportes de saldos → Saldos contables

| Reporte | Filtros principales | Fuente |
|---------|-------------------|--------|
| **Balance de prueba** | libro_presentacion + periodo | Saldos contables |
| **Balance general** | libro_presentacion + periodo + cuentas de naturaleza activo/pasivo/patrimonio | Saldos contables |
| **Estado de resultados** | libro_presentacion + periodo + cuentas de naturaleza ingreso/gasto/costo | Saldos contables |
| **Balance por unidad organizacional** | libro_presentacion + unidad_organizacional + periodo | Saldos contables |
| **Balance por tercero** | libro_presentacion + tercero + periodo | Saldos contables |
| **Comparativo entre periodos** | libro_presentacion + cuenta + varios periodos | Saldos contables |

### Reportes mixtos → Ambas fuentes

| Reporte | Cómo combina | Fuente |
|---------|-------------|--------|
| **Libro Mayor** (movimientos + saldo acumulado) | Saldo de apertura del periodo anterior (de saldos contables) + movimientos del periodo (de auxiliar contable) | Ambas |

---

## 6. ¿Por qué no una sola proyección?

| Alternativa evaluada | Problema |
|---------------------|----------|
| **Solo auxiliar contable** | Los reportes de saldos (balance de prueba, estados financieros) requieren agregar miles o millones de filas en cada consulta. Con volumen alto (>10.000 asientos/mes), el rendimiento se degrada de forma impredecible. Los reportes de saldos son los más consultados por contadores y gerencia. |
| **Solo saldos contables** | Los reportes de detalle (auxiliar por cuenta, libro diario, consulta de comprobante) no pueden construirse — se perdió la granularidad de cada movimiento individual. |
| **Una proyección con ambos niveles** | Mezclar detalle y saldos en una sola estructura fuerza compromisos: o se agregan campos de saldo a cada fila de detalle (redundancia masiva) o se usan dos tipos de filas en la misma tabla (complejidad en consultas). |

La separación en dos proyecciones permite que cada una se optimice para su propósito: el auxiliar contable para búsquedas por movimiento y el saldo contable para consultas agregadas.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: justificación de las dos proyecciones (auxiliar contable y saldos contables), estructura, ejemplos, filtros, direccionamiento de reportes y análisis de la decisión `libroPresentacion` vs `pucDestino`. |
