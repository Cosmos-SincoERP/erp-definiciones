# Anexo — Ejemplo de almacenamiento: RegistroTributario

## Propósito

Este anexo muestra cómo se almacena un `RegistroTributario` como stream de eventos (ES), incluyendo: gravamen al confirmar, intervención manual, y desgravamen por devolución. Su objetivo es reducir ambigüedades de interpretación durante la implementación.

> **Nota didáctica sobre códigos de jurisdicción:** En los ejemplos de este anexo se usa `subnacional: "BOG"` como abreviatura legible. En **implementación real**, el campo `subnacional` referencia `JurisdiccionFiscal.codigo` del catálogo del sub-dominio (Sección 3.7 del modelo) — para Bogotá D.C. el código es `11001` (DIVIPOLA). La invariante `[I13]` garantiza la integridad referencial de los códigos de jurisdicción enviados por el consumidor y persistidos en el registro.

---

## 1. Escenario

Una empresa (NIT 900.123) registra una obligación por pagar (OXP Comercio) con dos conceptos de gasto a un proveedor (NIT 800.456) en Bogotá:

| Concepto | Descripción | Valor | Clasificación |
|---|---|---|---|
| GASTO-001 | Servicios de consultoría | $1.000.000 | Gravado 19% |
| GASTO-002 | Papelería | $400.000 | Gravado 19% (excluido de ICA) |

**Perfiles al momento del cálculo:**
- Emisora: régimen Ordinario, gran contribuyente.
- Contraparte: régimen Ordinario, no autorretenedora, actividad económica CIIU 4711.

**Flujo previo a la confirmación:**
1. Durante la edición de la OXP, el consumidor solicita N simulaciones al motor (stateless — nada se persiste en Impuestos).
2. El usuario revisa el desglose propuesto.
3. El consumidor confirma la transacción y envía el comando de confirmación a Impuestos con el contexto completo y el desglose definitivo.
4. Impuestos re-ejecuta el motor internamente, compara con el desglose del consumidor, y crea el RegistroTributario.

---

## 2. Registro original — sin intervención manual

El usuario no realiza ajustes manuales. El desglose confirmado coincide con el cálculo del motor.

### Stream: registro-tributario-{guid-A}

```
Evento 1: RegistroTributarioCreado
  contextoTransaccional: {
    subDominio: "OXP",
    transaccionId: "oxp-123",
    direccionFiscal: "gasto",
    efectoFiscal: "gravamen"
  }
  entidadFiscalEmisora: {
    identificacion: { NIT: "900.123.456-7", pais: "CO" },
    perfil: { regimenTributario: "Ordinario", esGranContribuyente: true }
  }
  entidadFiscalContraparte: {
    identificacion: { NIT: "800.456.789-0", pais: "CO" },
    perfil: { regimenTributario: "Ordinario", esAutorretenedora: false,
              actividadEconomica: "4711" }
  }
  jurisdiccion: { pais: "CO", subnacional: "BOG" }
  intervencionManual: { huboIntervencion: false }
  lineasDeDesglose: [
    { tributo: "IVA", naturaleza: "aditivo",
      baseGravable: 1000000, tarifa: 0.19, tipoTarifa: "porcentaje",
      valor: 190000, factorUtilizado: "GRAV_19",
      conceptoOrigen: "GASTO-001" },
    { tributo: "RETEFUENTE", naturaleza: "sustractivo",
      baseGravable: 1000000, tarifa: 0.06, tipoTarifa: "porcentaje",
      valor: 60000, factorUtilizado: "Consultoría",
      conceptoOrigen: "GASTO-001" },
    { tributo: "ICA", naturaleza: "sustractivo",
      baseGravable: 1000000, tarifa: 0.01104, tipoTarifa: "porcentaje",
      valor: 11040, factorUtilizado: "4711",
      conceptoOrigen: "GASTO-001" },
    { tributo: "IVA", naturaleza: "aditivo",
      baseGravable: 400000, tarifa: 0.19, tipoTarifa: "porcentaje",
      valor: 76000, factorUtilizado: "GRAV_19",
      conceptoOrigen: "GASTO-002" },
    { tributo: "RETEFUENTE", naturaleza: "sustractivo",
      baseGravable: 400000, tarifa: 0.025, tipoTarifa: "porcentaje",
      valor: 10000, factorUtilizado: "Compras generales",
      conceptoOrigen: "GASTO-002" }
  ]
  // lineasDesgloseMotor: no presente (coincide con lineasDeDesglose)
  timestamp: 2026-03-11T10:30:00
```

> **Nota:** El stream tiene un solo evento. El RegistroTributario nace como hecho fiscal confirmado — no hay estados intermedios. Cuando no hubo intervención manual, `lineasDesgloseMotor` no se incluye porque coincide con `lineasDeDesglose`.

### Estado reconstruido por replay

| Campo | Valor |
|---|---|
| Contexto | OXP oxp-123, gasto, gravamen |
| Emisora | NIT 900.123, Gran Contribuyente, Ordinario |
| Contraparte | NIT 800.456, Ordinario, no autorretenedora, CIIU 4711 |
| Jurisdicción | CO, Bogotá |
| Intervención | No |

| # | Concepto | Tributo | Naturaleza | Base | Tarifa | Valor |
|---|---|---|---|---|---|---|
| 1 | GASTO-001 | IVA | Aditivo | $1.000k | 19% | $190k |
| 2 | GASTO-001 | RETEFUENTE | Sustractivo | $1.000k | 6% | $60k |
| 3 | GASTO-001 | ICA | Sustractivo | $1.000k | 11.04‰ | $11k |
| 4 | GASTO-002 | IVA | Aditivo | $400k | 19% | $76k |
| 5 | GASTO-002 | RETEFUENTE | Sustractivo | $400k | 2.5% | $10k |

**Calculado (no almacenado):**
- `totalImpuestos()` = $190k + $76k = **$266k**
- `totalRetenciones()` = $60k + $11k + $10k = **$81k**
- `valorNeto($1.400k)` = $1.400k + $266k - $81k = **$1.585k**

---

## 3. Registro con intervención manual

Mismo escenario, pero el usuario excluye manualmente el tributo ICA del concepto GASTO-001 (tiene certificado de exención pendiente de registro). El desglose confirmado tiene 4 líneas en vez de 5. Al confirmar, Impuestos re-ejecuta el motor (que sí calcula ICA), detecta la divergencia, y crea el registro con ambos desgloses.

### Stream: registro-tributario-{guid-D}

```
Evento 1: RegistroTributarioCreado
  contextoTransaccional: {
    subDominio: "OXP",
    transaccionId: "oxp-456",
    direccionFiscal: "gasto",
    efectoFiscal: "gravamen"
  }
  entidadFiscalEmisora: {
    identificacion: { NIT: "900.123.456-7", pais: "CO" },
    perfil: { regimenTributario: "Ordinario", esGranContribuyente: true }
  }
  entidadFiscalContraparte: {
    identificacion: { NIT: "800.456.789-0", pais: "CO" },
    perfil: { regimenTributario: "Ordinario", esAutorretenedora: false,
              actividadEconomica: "4711" }
  }
  jurisdiccion: { pais: "CO", subnacional: "BOG" }
  intervencionManual: { huboIntervencion: true }
  lineasDeDesglose: [                          // ← desglose CONFIRMADO
    { tributo: "IVA", naturaleza: "aditivo",
      baseGravable: 1000000, tarifa: 0.19, tipoTarifa: "porcentaje",
      valor: 190000, factorUtilizado: "GRAV_19",
      conceptoOrigen: "GASTO-001" },
    { tributo: "RETEFUENTE", naturaleza: "sustractivo",
      baseGravable: 1000000, tarifa: 0.06, tipoTarifa: "porcentaje",
      valor: 60000, factorUtilizado: "Consultoría",
      conceptoOrigen: "GASTO-001" },
    // ← ICA EXCLUIDO por intervención manual
    { tributo: "IVA", naturaleza: "aditivo",
      baseGravable: 400000, tarifa: 0.19, tipoTarifa: "porcentaje",
      valor: 76000, factorUtilizado: "GRAV_19",
      conceptoOrigen: "GASTO-002" },
    { tributo: "RETEFUENTE", naturaleza: "sustractivo",
      baseGravable: 400000, tarifa: 0.025, tipoTarifa: "porcentaje",
      valor: 10000, factorUtilizado: "Compras generales",
      conceptoOrigen: "GASTO-002" }
  ]
  lineasDesgloseMotor: [                       // ← cálculo ORIGINAL del motor
    { tributo: "IVA", naturaleza: "aditivo",
      baseGravable: 1000000, tarifa: 0.19, tipoTarifa: "porcentaje",
      valor: 190000, factorUtilizado: "GRAV_19",
      conceptoOrigen: "GASTO-001" },
    { tributo: "RETEFUENTE", naturaleza: "sustractivo",
      baseGravable: 1000000, tarifa: 0.06, tipoTarifa: "porcentaje",
      valor: 60000, factorUtilizado: "Consultoría",
      conceptoOrigen: "GASTO-001" },
    { tributo: "ICA", naturaleza: "sustractivo",      // ← el motor SÍ lo calculó
      baseGravable: 1000000, tarifa: 0.01104, tipoTarifa: "porcentaje",
      valor: 11040, factorUtilizado: "4711",
      conceptoOrigen: "GASTO-001" },
    { tributo: "IVA", naturaleza: "aditivo",
      baseGravable: 400000, tarifa: 0.19, tipoTarifa: "porcentaje",
      valor: 76000, factorUtilizado: "GRAV_19",
      conceptoOrigen: "GASTO-002" },
    { tributo: "RETEFUENTE", naturaleza: "sustractivo",
      baseGravable: 400000, tarifa: 0.025, tipoTarifa: "porcentaje",
      valor: 10000, factorUtilizado: "Compras generales",
      conceptoOrigen: "GASTO-002" }
  ]
  lineasDescartadas: [                         // ← tributos evaluados pero EXCLUIDOS por el motor
    { tributo: "RICA", naturaleza: "sustractivo",
      baseGravable: 1000000, tarifa: 0.01104, tipoTarifa: "porcentaje",
      valor: 11040, factorUtilizado: "4711",
      conceptoOrigen: "GASTO-001",
      motivoExclusion: "perfil_no_aplica" }    // ← emisora no es agente de retención de ICA en BOG
  ]
  timestamp: 2026-03-11T11:45:00
```

> **Nota:** Cuando `huboIntervencion = true`, el evento incluye tres conjuntos: `lineasDeDesglose` (lo que el usuario confirmó), `lineasDesgloseMotor` (tributos que el motor aplicó) y `lineasDescartadas` (tributos que el motor evaluó pero excluyó, con motivo). La divergencia entre `lineasDesgloseMotor` y `lineasDeDesglose` refleja la intervención del usuario. Las `lineasDescartadas` muestran qué más evaluó el motor y por qué lo descartó. Para reportes fiscales, se usan las `lineasDeDesglose` (desglose confirmado).

---

## 4. Desgravamen — devolución total

El proveedor devuelve toda la mercancía. OXP crea una Devolución tipo Comercio (dev-456) como transacción independiente. OXP envía la confirmación con `efectoFiscal: desgravamen` y `transaccionOrigenId: "oxp-123"`. Impuestos resuelve el RegistroTributario del gravamen original, prorratea su desglose confirmado a los montos del desgravamen (en este caso 100% → espejo exacto), y lo usa como referencia. El usuario no interviene → `huboIntervencion: false`.

**El stream original (registro-tributario-{guid-A}) NO se modifica.**

### Stream: registro-tributario-{guid-B} (NUEVO)

```
Evento 1: RegistroTributarioCreado
  contextoTransaccional: {
    subDominio: "OXP",
    transaccionId: "dev-456",
    direccionFiscal: "gasto",
    efectoFiscal: "desgravamen",
    transaccionOrigenId: "oxp-123"       // ← Impuestos resuelve el registro origen
  }
  entidadFiscalEmisora: {
    identificacion: { NIT: "900.123.456-7", pais: "CO" },
    perfil: { regimenTributario: "Ordinario", esGranContribuyente: true }
  }
  entidadFiscalContraparte: {
    identificacion: { NIT: "800.456.789-0", pais: "CO" },
    perfil: { regimenTributario: "Ordinario", esAutorretenedora: false,
              actividadEconomica: "4711" }
  }
  jurisdiccion: { pais: "CO", subnacional: "BOG" }
  intervencionManual: { huboIntervencion: false }
  lineasDeDesglose: [
    { tributo: "IVA", naturaleza: "aditivo",
      baseGravable: 1000000, tarifa: 0.19, tipoTarifa: "porcentaje",
      valor: 190000, factorUtilizado: "GRAV_19",
      conceptoOrigen: "GASTO-001" },
    { tributo: "RETEFUENTE", naturaleza: "sustractivo",
      baseGravable: 1000000, tarifa: 0.06, tipoTarifa: "porcentaje",
      valor: 60000, factorUtilizado: "Consultoría",
      conceptoOrigen: "GASTO-001" },
    { tributo: "ICA", naturaleza: "sustractivo",
      baseGravable: 1000000, tarifa: 0.01104, tipoTarifa: "porcentaje",
      valor: 11040, factorUtilizado: "4711",
      conceptoOrigen: "GASTO-001" },
    { tributo: "IVA", naturaleza: "aditivo",
      baseGravable: 400000, tarifa: 0.19, tipoTarifa: "porcentaje",
      valor: 76000, factorUtilizado: "GRAV_19",
      conceptoOrigen: "GASTO-002" },
    { tributo: "RETEFUENTE", naturaleza: "sustractivo",
      baseGravable: 400000, tarifa: 0.025, tipoTarifa: "porcentaje",
      valor: 10000, factorUtilizado: "Compras generales",
      conceptoOrigen: "GASTO-002" }
  ]
  // Montos POSITIVOS — el efectoFiscal "desgravamen" indica que reduce la obligación
  timestamp: 2026-03-15T14:20:00
```

---

## 5. Desgravamen — devolución parcial

Si solo se devuelve el concepto GASTO-001 (servicios de consultoría), el prorrateo toma las líneas del registro origen correspondientes a ese concepto (100% del monto de GASTO-001). Adicionalmente, se ilustra una devolución parcial por monto: devolver $500.000 de los $1.000.000 de GASTO-001 prorratea cada tributo al 50%.

### Stream: registro-tributario-{guid-C} (NUEVO)

**5a. Devolución de concepto completo (GASTO-001 al 100%):**

```
Evento 1: RegistroTributarioCreado
  contextoTransaccional: {
    subDominio: "OXP",
    transaccionId: "dev-789",
    direccionFiscal: "gasto",
    efectoFiscal: "desgravamen",
    transaccionOrigenId: "oxp-123"
  }
  // ... misma emisora, contraparte, jurisdicción ...
  intervencionManual: { huboIntervencion: false }
  lineasDeDesglose: [
    { tributo: "IVA", naturaleza: "aditivo",
      baseGravable: 1000000, tarifa: 0.19, tipoTarifa: "porcentaje",
      valor: 190000, factorUtilizado: "GRAV_19",
      conceptoOrigen: "GASTO-001" },
    { tributo: "RETEFUENTE", naturaleza: "sustractivo",
      baseGravable: 1000000, tarifa: 0.06, tipoTarifa: "porcentaje",
      valor: 60000, factorUtilizado: "Consultoría",
      conceptoOrigen: "GASTO-001" },
    { tributo: "ICA", naturaleza: "sustractivo",
      baseGravable: 1000000, tarifa: 0.01104, tipoTarifa: "porcentaje",
      valor: 11040, factorUtilizado: "4711",
      conceptoOrigen: "GASTO-001" }
  ]
  // Prorrateo 100% de GASTO-001 — GASTO-002 sigue vigente en el registro original
  timestamp: 2026-03-15T14:20:00
```

**5b. Devolución parcial por monto ($500.000 de los $1.000.000 de GASTO-001 → prorrateo 50%):**

```
Evento 1: RegistroTributarioCreado
  contextoTransaccional: {
    subDominio: "OXP",
    transaccionId: "dev-790",
    direccionFiscal: "gasto",
    efectoFiscal: "desgravamen",
    transaccionOrigenId: "oxp-123"
  }
  // ... misma emisora, contraparte, jurisdicción ...
  intervencionManual: { huboIntervencion: false }
  lineasDeDesglose: [
    { tributo: "IVA", naturaleza: "aditivo",
      baseGravable: 500000, tarifa: 0.19, tipoTarifa: "porcentaje",
      valor: 95000, factorUtilizado: "GRAV_19",
      conceptoOrigen: "GASTO-001" },
    { tributo: "RETEFUENTE", naturaleza: "sustractivo",
      baseGravable: 500000, tarifa: 0.06, tipoTarifa: "porcentaje",
      valor: 30000, factorUtilizado: "Consultoría",
      conceptoOrigen: "GASTO-001" },
    { tributo: "ICA", naturaleza: "sustractivo",
      baseGravable: 500000, tarifa: 0.01104, tipoTarifa: "porcentaje",
      valor: 5520, factorUtilizado: "4711",
      conceptoOrigen: "GASTO-001" }
  ]
  // Prorrateo 50%: mismas tarifas del origen, bases y valores proporcionados al monto devuelto
  timestamp: 2026-03-15T15:00:00
```

> **Nota:** El prorrateo conserva las tarifas del registro origen. La base se reduce al monto devuelto y el valor se recalcula proporcionalmente. Un tributo que aplicó al gravamen original siempre aplica al desgravamen en la misma proporción — incluso si la cuantía mínima del tributo excede el monto devuelto.

---

## 6. Efecto fiscal neto (para reportes)

Las proyecciones (read models) para reportes fiscales interpretan el `efectoFiscal` de cada registro para determinar el signo al sumar. Ejemplo con devolución total:

| Concepto | Tributo | Gravamen (oxp-123) | Desgravamen (dev-456) | **Neto** |
|---|---|---|---|---|
| GASTO-001 | IVA | +$190k | -$190k | **$0** |
| GASTO-001 | RETEFUENTE | +$60k | -$60k | **$0** |
| GASTO-001 | ICA | +$11k | -$11k | **$0** |
| GASTO-002 | IVA | +$76k | -$76k | **$0** |
| GASTO-002 | RETEFUENTE | +$10k | -$10k | **$0** |
| | | **Total: $347k** | **Total: -$347k** | **$0** |

Ejemplo con devolución parcial (solo GASTO-001):

| Concepto | Tributo | Gravamen (oxp-123) | Desgravamen (dev-789) | **Neto** |
|---|---|---|---|---|
| GASTO-001 | IVA | +$190k | -$190k | **$0** |
| GASTO-001 | RETEFUENTE | +$60k | -$60k | **$0** |
| GASTO-001 | ICA | +$11k | -$11k | **$0** |
| GASTO-002 | IVA | +$76k | — | **+$76k** |
| GASTO-002 | RETEFUENTE | +$10k | — | **+$10k** |

**Principio:** Ambos registros son hechos fiscales independientes con montos positivos. El `efectoFiscal` determina cómo las proyecciones interpretan cada registro: `gravamen` suma, `desgravamen` resta. El consumidor envía `transaccionOrigenId` para que Impuestos resuelva el RegistroTributario del gravamen original y prorratea su desglose como referencia. La relación entre transacciones se establece en el `ContextoTransaccional` del desgravamen.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: ejemplo de cálculo con dos conceptos, confirmación, reversa total, reversa parcial, efecto neto para reportes. |
| 1.1 | Marzo 2026 | Alineación con D4/D5: RegistroTributario nace como hecho fiscal confirmado (un solo evento RegistroTributarioCreado). Eliminados eventos Propuesto/Confirmado. Agregada sección de intervención manual con ejemplo de exclusión de tributo. |
| 1.2 | Marzo 2026 | Modelo simplificado de efecto fiscal: eliminados `tipoTransaccion` y valores negativos. Reemplazados por `efectoFiscal` (gravamen/desgravamen) con montos siempre positivos. Desgravámenes son transacciones independientes del consumidor. |
| 1.3 | Marzo 2026 | Modelo de desgravamen por prorrateo del registro origen: `transaccionOrigenId` reintroducido en el ContextoTransaccional del desgravamen. Impuestos resuelve el registro origen y prorratea su desglose como referencia (el motor no participa). Ejemplo de devolución parcial por monto (50%) agregado en sección 5b. Sección 6 actualizada: relación entre transacciones establecida en ContextoTransaccional. |
