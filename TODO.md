# TODO — Obligaciones por Pagar (OXP)

## Estructura y Datos de Agregados

- [x] Tener calculado el valor total de la OXP en alguna propiedad del agregado o definir si queda encapsulado.
- [x] Especificar el saldo tanto para la OXP de Comercio como para la OXP de Extracto.
- [ ] Re-definir los datos del agregado OXP Comercio y OXP Extracto.
- [ ] Transformar o mejorar el concepto que la OXP de Comercio es mucho mas amplia que solo el medio de pago con tarjeta de credito, en esta entran las obligaciones por pagar a los comercios/proveedores con otras formas de pago también.
- [ ] Incluir las obligaciones por pagar de menor cuantia o cajas menores como se les dice en Colombia, estas son las relacionadas a dos

## Conceptos y Naturaleza de la OXP

- [x] Plantear cómo quedaría la devolución (OXP de naturaleza diferente → OXP de Devolución).
- [x] Incluir el concepto de descuento dentro del concepto de gasto y a nivel global.
- [ ] Incluir el concepto de retegarantía (seguro de garantía del trabajo realizado por unos meses).
- [x] Revisar y establecer los conceptos claros de cuándo una OXP es de anticipo.
- [x] Los abonos y devoluciones en el extracto.

## Tributos e Impuestos

- [ ] Algunos tributos pueden tener dependencia de existencia de otros tributos (ej: retención de IVA no puede existir sin IVA).
- [x] Validar el comportamiento completo de la OXP del Extracto si debe aplicar impuestos a los conceptos de cargos financieros.

## Moneda y TRM

- [x] La información de la moneda y TRM va a nivel del concepto, ya que una OXP puede tener conceptos con diferentes monedas (frecuente en Extracto).

## Estados y Eventos

- [ ] Revisar eventos `OXPComercioPagada` y `OXPExtractoPagada`: ¿es una confirmación del sistema contable externo o se resuelve vía EDA con un broker?
- [ ] Reconsiderar el estado de Causada/Causado en todos los agregados.

## Contabilidad e Integración

- [ ] Revisar cómo generar una sección independiente para la especificación de la traducción contable.
- [ ] No está documentado el servicio de entrada.

## Terceros
- [ ] Definir gestor de terceros
- [ ] Mientras el gestor de terceros existe, como se va a entregar el perfil tributario, se debe tomar desde SincoA&F. 
 
---

## Priorización — Ítems globales de trabajo

| # | Ítem | Tipo | Dependencia | Estado |
|---|------|------|-------------|--------|
| 1 | Sistema de Impuestos | Modelo | — | Pendiente |
| 2 | `lineasParaTraduccion()` | Modelo | Requiere #1 | Pendiente |
| 3 | Servicio de Traducción Contable | Modelo | Requiere #2 | Pendiente |
| 4 | Definición de Payloads | Modelo | Requiere #1–#3 estables | Pendiente |
| 5 | EventCatalog | Artefacto | Requiere #4 | Pendiente |
| 6 | Cajas Menores | Modelo | Independiente, se beneficia de #1–#4 | Pendiente |
| 7 | Reportes y Paneles de trabajo | Alcance → Modelo | Requiere #4 | Pendiente |

### Cadena de dependencias

```
[1] Sistema de Impuestos
        ↓ alimenta
[2] lineasParaTraduccion()
        ↓ consume
[3] Servicio de Traducción Contable
        ↓ estabiliza
[4] Definición de Payloads ──→ [5] EventCatalog

[6] Cajas Menores → independiente (pero se beneficia de #1–#4)

[7] Reportes / Paneles → perspectiva de lectura (read model sobre eventos)
```

### Análisis de priorización

**#1 — Sistema de Impuestos.** Es el cimiento de la cadena. El modelo ya tiene el VO `DesgloseFiscal` con `List<Tributo>` en OxpComercio y Devolución, pero no está definido *cómo* el sistema solicita, calcula y resuelve los tributos. Hasta que esto no esté claro, tanto las líneas de traducción como el servicio contable trabajarían sobre una base incompleta. El pendiente sobre dependencias entre tributos (retención de IVA requiere IVA) confirma que hay reglas de negocio sin especificar.

**#2 — `lineasParaTraduccion()`.** Ya existe un boceto conceptual en el modelo (método calculado que produce `List<LineaTraduccion>` por combinación componente × destino), pero le falta la definición completa. Con el sistema de impuestos resuelto, ya se sabría exactamente qué componentes fiscales entran en cada línea.

**#3 — Servicio de Traducción Contable.** Es el consumidor final de las líneas. Definir si está dentro o fuera del bounded context OXP es una decisión arquitectónica importante, pero que se toma mejor cuando ya se sabe qué le llega como input. La decisión D19 ya da una pista ("la traducción contable interpreta Devolución como nota crédito"), así que hay una base sobre la cual construir.

**#4 — Definición de Payloads.** El modelo tiene 47 eventos documentados pero sus datos internos no están completamente especificados. Este es un refinamiento natural *después* de estabilizar la cadena fiscal-contable, porque los payloads de eventos como `OxpComercioConceptoAgregado` o `DevolucionConfirmada` necesitan reflejar el desglose fiscal ya definido. Es el bloqueante directo del EventCatalog.

**#5 — EventCatalog.** Es el tercer artefacto del sub-dominio (Fase 3). Necesita los payloads definidos para ser útil — sin ellos sería solo un diagrama de nombres sin sustancia. Con los payloads listos, este artefacto se puede generar de forma bastante directa.

**#6 — Cajas Menores.** Es completamente independiente de la cadena fiscal-contable. No bloquea ni es bloqueado por los otros ítems. Tiene el beneficio adicional de ir después: cuando se defina, ya se tendrá el sistema de impuestos, la traducción contable y los payloads estabilizados. El nuevo agregado nace completo desde el primer día.

**#7 — Reportes y Paneles de trabajo.** Son la perspectiva de lectura (read model / proyecciones), mientras que todo lo anterior es la perspectiva de escritura (write model). En un enfoque ES/EDA, los paneles se construyen como proyecciones sobre los eventos ya definidos. Tiene sentido definirlos cuando ya se sabe qué eventos existen y qué datos llevan. Si hay paneles críticos para validar el flujo de negocio, podrían documentarse antes como requisitos funcionales en el alcance (`definicion-alcance.md`) sin necesidad de modelarlos técnicamente todavía.
