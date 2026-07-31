# Catálogo Tributario — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `CatalogoTributario` (Sección 3.2 de `modelo-dominio.md`)
**Versión:** 1.2
**Fecha de actualización:** 2026-07-31
**Archivo de datos:** [`co-catalogo-tributario.json`](co-catalogo-tributario.json)

---

## 1. Propósito

Este catálogo precarga la configuración estándar del agregado `CatalogoTributario` para Colombia: el universo de **tributos**, **clasificaciones tributarias**, **tratamientos** (matriz tributo × clasificación) y **reglas de localización**. Es el ancla referencial para el resto de los catálogos del producto:

- `TarifaTributaria` referencia tributos y clasificaciones para construir sus streams (ej: `tarifa-CO-IVA`, `tarifa-CO-11001-ICA`).
- `CondicionDeAplicacion` modifica el tratamiento por perfil tributario.
- Los consumidores (OXP, CXC) clasifican sus conceptos contra el catálogo de `ClasificacionTributaria`.
- `RegistroTributario` captura el código del tributo y de la clasificación como snapshot inmutable.

---

## 2. Fuente normativa

- **Tributos directos:**
  - IVA, INC: Libro Tercero del Estatuto Tributario (arts. 420 a 513).
  - RETEFUENTE: Estatuto Tributario (arts. 365 a 419) + Decreto Único Reglamentario 1625 de 2016.
  - ICA, RICA, SOBRETASA_BOMBERIL: Ley 14 de 1983 + estatutos tributarios municipales.
- **Autorretenciones:**
  - AUTO_RENTA: Decreto 2201 de 2016.
  - AUTO_RETEFUENTE: Estatuto Tributario art. 9 y normas relacionadas.
  - AUTO_RICA: Acuerdos municipales que designen autorretenedores.
- **IVA por importación de servicios:** Art. 437-2 numeral 3 del Estatuto Tributario — el adquiriente autoliquida el IVA al contratar servicios gravados con proveedores sin residencia ni domicilio en el país; la retención es del 100% del impuesto (art. 437-1).
- **Sobretasa bomberil:** Ley 1575 de 2012 (Ley General de Bomberos).

---

## 3. Cobertura del catálogo

| Categoría | Cantidad |
|---|---|
| Tributos directos | 7 |
| Autorretenciones | 3 |
| Tributos autoliquidados | 1 |
| Clasificaciones tributarias | 6 |
| Tratamientos explícitos `aplica: true` | 22 |
| Reglas de localización | 11 |
| **Total entidades** | **50** |

---

## 4. Tributos

### 4.1. Tributos directos (7)

| Código | Nombre | Naturaleza | Carácter retención | Nivel | Factor de tarifa | Dirección fiscal | Tributo padre |
|---|---|:---:|:---:|:---:|---|:---:|:---:|
| `IVA` | Impuesto al Valor Agregado | aditivo | — | nacional | `clasificacion` | ambas | — |
| `INC` | Impuesto Nacional al Consumo | aditivo | — | nacional | `clasificacion` | ambas | — |
| `ICA` | Impuesto de Industria y Comercio | aditivo | — | municipal | `actividadEconomica` | ingreso | — |
| `RETEFUENTE` | Retención en la Fuente | sustractivo | anticipado | nacional | `conceptoPago` | ambas | — |
| `RIVA` | Retención sobre el IVA | sustractivo | anticipado | nacional | `porcentajeDePadre` | ambas | `IVA` |
| `RICA` | Retención sobre el ICA | sustractivo | anticipado | municipal | `actividadEconomica` | ambas | — |
| `SOBRETASA_BOMBERIL` | Sobretasa Bomberil | sustractivo | definitivo | municipal | `porcentajeDePadre` | ambas | `RICA` |

> **Nota:** `ICA` aplica solo en `ingreso` — el sujeto pasivo del ICA es **quien genera el ingreso**; en dirección gasto el comprador solo practica la retención (`RICA`), no autoliquida ICA (coherente con `R61` del alcance). `RICA` permanece en `ambas` (retención: en gasto la empresa retiene al proveedor; en ingreso el cliente le retiene a la empresa).

### 4.2. Tributos de provisión (4): autorretenciones y autoliquidados

| Código | Nombre | Naturaleza | Carácter retención | Nivel | Factor de tarifa | Dirección fiscal | Tributo padre |
|---|---|:---:|:---:|:---:|---|:---:|:---:|
| `AUTO_RENTA` | Autorretención de Renta | sustractivo | anticipado | nacional | `fija` | ingreso | — |
| `AUTO_RETEFUENTE` | Autorretención en la Fuente | sustractivo | anticipado | nacional | `conceptoPago` | ingreso | — |
| `IVA_IMPORTACION_SERVICIOS` | IVA por importación de servicios | sustractivo | anticipado | nacional | `porcentajeDePadre` | gasto | `IVA` |
| `AUTO_RICA` | Autorretención de ICA | sustractivo | anticipado | municipal | `actividadEconomica` | ingreso | — |

> **Nota:** `IVA_IMPORTACION_SERVICIOS` **no es una autorretención** — es la autoliquidación del IVA por el adquiriente al contratar servicios con proveedores sin residencia ni domicilio en el país. El proveedor no factura IVA y recibe el total de la factura; la empresa asume el impuesto (100% del IVA teórico), lo declara y lo paga a la DIAN. Por eso su dirección es `gasto` (solo existe en compras) mientras las tres autorretenciones son `ingreso` (la empresa se retiene sobre sus propias ventas). La antigua figura "autorretención de IVA en ventas" del sistema de facturación no tiene sustento en el Estatuto y fue removida — no existe autorretenedor de rete-IVA.

---

## 5. Clasificaciones tributarias

| Código | Nombre | Tributos que aplican (resumen) |
|---|---|---|
| `GRAV_19` | Gravados 19% | IVA, RETEFUENTE, AUTO_RETEFUENTE, RIVA, ICA, RICA |
| `GRAV_5` | Gravados 5% | IVA, RETEFUENTE, AUTO_RETEFUENTE, RIVA, ICA, RICA |
| `EXCLUIDO` | Excluidos de IVA | RETEFUENTE, AUTO_RETEFUENTE, ICA, RICA |
| `EXENTO` | Exentos de IVA (tarifa 0%) | IVA, RETEFUENTE, AUTO_RETEFUENTE, ICA, RICA |
| `INC_8` | Gravados INC 8% | INC |
| `NO_GRAVADO` | No sujeto a impuestos | — |

---

## 6. Tratamientos (matriz clasificación × tributo)

Cada entrada declara que el tributo aplica cuando un concepto es clasificado con esa clasificación. Los tratamientos NO listados implican que el tributo no aplica por default; pueden agregarse excepciones vía `origen: personalizado`.

**Total: 22 tratamientos `aplica: true`.** Distribución:

- GRAV_19: 6 tributos (IVA, RETEFUENTE, AUTO_RETEFUENTE, RIVA, ICA, RICA).
- GRAV_5: 6 tributos (mismos que GRAV_19, distintas tarifas en `TarifaTributaria`).
- EXCLUIDO: 4 tributos (RETEFUENTE, AUTO_RETEFUENTE, ICA, RICA).
- EXENTO: 5 tributos (IVA con tarifa 0%, RETEFUENTE, AUTO_RETEFUENTE, ICA, RICA).
- INC_8: 1 tributo (INC).
- NO_GRAVADO: ninguno.

`AUTO_RETEFUENTE` **sí participa de la matriz** (las cuatro clasificaciones principales): la matriz declara su candidatura por clasificación, y su activación sigue dependiendo de la calidad de autorretenedora vía `CondicionDeAplicacion` — igual que en la configuración estándar de la implementación. `AUTO_RENTA` y `AUTO_RICA` NO se modelan como tratamientos por clasificación — su aplicación depende solo de atributos del `PerfilTributario` y de la dirección fiscal. La matriz de `IVA_IMPORTACION_SERVICIOS` está **pendiente de definición** con los consultores fiscales (pregunta 6 de la revisión pendiente).

---

## 7. Reglas de localización

Cada regla declara qué rol de ubicación de la transacción determina la jurisdicción fiscalmente relevante para resolver el tributo.

| Tributo | Rol que manda | Fallback |
|---|---|---|
| `IVA` | sedeEmisora | — |
| `INC` | sedeEmisora | — |
| `RETEFUENTE` | sedeEmisora | — |
| `ICA` | lugarEjecucion | sedeEmisora |
| `RIVA` | sedeEmisora | — |
| `RICA` | lugarEjecucion | sedeEmisora |
| `SOBRETASA_BOMBERIL` | lugarEjecucion | sedeEmisora |
| `AUTO_RENTA` | sedeEmisora | — |
| `AUTO_RETEFUENTE` | sedeEmisora | — |
| `IVA_IMPORTACION_SERVICIOS` | sedeEmisora | — |
| `AUTO_RICA` | lugarEjecucion | sedeEmisora |

**Patrón:** Tributos nacionales resuelven por `sedeEmisora` (la sede de la empresa determina el país). Tributos municipales (ICA, RICA, SOBRETASA_BOMBERIL, AUTO_RICA) resuelven por `lugarEjecucion` (donde se presta el servicio o entrega el bien) con `sedeEmisora` como fallback.

---

## 8. Notas operativas

### 8.1. `codigo` semántico inmutable

`Tributo.codigo` y `ClasificacionTributaria.codigo` son **inmutables** durante todo el ciclo de vida (decisión del modelo `[I26]` + `[PD12]`). Cambiar un código rompe la trazabilidad histórica de `RegistroTributario` (que captura el código como snapshot). Si una normativa requiere un tributo distinto, se desactiva el actual y se agrega uno nuevo con código y vigencia propios. La política de corrección de errores la define el equipo fiscal del producto (pendiente `[PD12]`).

### 8.2. `caracterRetencion` y compensación

- **anticipado:** el tributo retenido se compensa con la declaración tributaria correspondiente (renta, IVA, ICA).
- **definitivo:** el tributo no se compensa, va directo al fondo destinado (caso SOBRETASA_BOMBERIL).
- **null:** aplica a tributos aditivos que no son retenciones (IVA, INC, ICA).

### 8.3. Tributos sin clasificación específica

INC tiene solo una clasificación de soporte (`INC_8`); los demás conceptos no generan INC. El catálogo se mantiene minimalista — si normativa amplía las tarifas INC, se agregan clasificaciones nuevas (`INC_4`, `INC_16`).

### 8.4. Heredan jurisdicción del padre

- `RIVA` hereda jurisdicción de `IVA` (nacional).
- `SOBRETASA_BOMBERIL` hereda de `RICA` (municipal).
- `IVA_IMPORTACION_SERVICIOS` hereda de `IVA` (nacional).

Esto se modela vía `factorDeTarifa: porcentajeDePadre` — el motor primero calcula el padre, luego aplica la tarifa sobre el resultado en la misma jurisdicción.

### 8.5. Frontera con `CondicionDeAplicacion`

Este catálogo declara **qué tributos existen y a qué clasificaciones aplican por default**. Las condiciones (`CondicionDeAplicacion`) ajustan el comportamiento por **perfil tributario** del sujeto: por ejemplo, si una empresa es Régimen Simple, RETEFUENTE puede quedar exonerado para ciertos conceptos. Esas reglas viven en `co-condicion-de-aplicacion.json`.

---

## 9. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 11 tributos (7 directos + 4 autorretenciones), 6 clasificaciones, 18 tratamientos, 11 reglas de localización. Fuente Estatuto Tributario + Decretos DIAN + leyes municipales. |
| 1.1 | 2026-07-10 | `ICA.direccionFiscalAplicable` pasa de `ambas` a `ingreso` (issue #93): el sujeto pasivo del ICA es quien genera el ingreso; en gasto el comprador solo practica RICA. Alineado con la implementación (`Cosmos.Impuestos#116`). |
| 1.2 | 2026-07-31 | **Renombre `AUTO_RIVA` → `IVA_IMPORTACION_SERVICIOS`** (issue #110, resolución con consultoría fiscal jul-2026): la "autorretención de IVA en ventas" del sistema legado no tiene sustento normativo y se remueve como concepto; el tributo queda definido como la autoliquidación del IVA en importación de servicios (art. 437-2 num. 3 ET). Nombre legible "IVA por importación de servicios"; el disparador pasa de `esAgenteRetenedorIVA` (residuo legado, chocaba con `RIVA-01b`) al nuevo atributo `tieneDomicilioFiscalEnElPais` de la contraparte — ver catálogo de atributos v1.1 y condiciones v1.1. Se cierra la antigua pregunta 4 (dirección `gasto` confirmada) y entran las preguntas 6-8. **Tratamientos 18 → 22** (issue #111): `AUTO_RETEFUENTE` × GRAV_19/GRAV_5/EXCLUIDO/EXENTO (`aplica: true`), alineando la matriz con la configuración estándar de la implementación. Total entidades 46 → 50. |

---

## 10. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales**:

1. **`caracterRetencion` para tributos aditivos:** ¿Es correcto dejarlo `null` para IVA, INC, ICA, o conviene usar un valor explícito (ej: `no-aplica`)?
2. **SOBRETASA_BOMBERIL como `definitivo`:** ¿La sobretasa va directamente al fondo bomberil sin compensación, o tiene algún mecanismo de descuento que la haga `anticipado`?
3. **¿Faltan tributos en F1?** Casos conocidos no incluidos por simplicidad inicial: GMF (4×1000), Impuesto al Patrimonio, Impuesto al Consumo de Licores y Cervezas departamental, Estampillas municipales/departamentales. ¿Cuáles deben entrar en F1?
4. **Tratamientos para INC:** ¿Existen clasificaciones adicionales de INC (`INC_4`, `INC_16`)? Si sí, ¿cómo se diferencian (sectores)?
5. **Tratamientos para clasificaciones GRAV_*:** ¿Faltan tarifas intermedias (GRAV_8, GRAV_12, etc.)?
6. **Matriz de tratamientos de `IVA_IMPORTACION_SERVICIOS`:** el art. 437-2 num. 3 no alcanza todos los conceptos — ¿qué clasificaciones (o servicios) quedan cubiertos por la autoliquidación? Hoy el tributo no tiene tratamientos declarados; definir la matriz con los consultores.
7. **¿La autoliquidación se restringe a emisoras responsables de IVA** (`perteneceRegimenIVA = true`), o aplica a cualquier contratante de servicios del exterior?
8. **Perfil tributario mínimo del proveedor del exterior:** ¿qué datos se exigen para operar con una contraparte sin domicilio fiscal en el país? (El motor rechaza la transacción si la contraparte no tiene perfil.)
