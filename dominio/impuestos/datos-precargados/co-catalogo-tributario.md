# Catálogo Tributario — Colombia

**País:** Colombia (`CO`)
**Catálogo del modelo:** `CatalogoTributario` (Sección 3.2 de `modelo-dominio.md`)
**Versión:** 1.1
**Fecha de actualización:** 2026-07-10
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
  - AUTO_RIVA: Art. 437-2 del Estatuto Tributario (reverse charge).
  - AUTO_RICA: Acuerdos municipales que designen autorretenedores.
- **Sobretasa bomberil:** Ley 1575 de 2012 (Ley General de Bomberos).

---

## 3. Cobertura del catálogo

| Categoría | Cantidad |
|---|---|
| Tributos directos | 7 |
| Tributos de provisión (autorretenciones) | 4 |
| Clasificaciones tributarias | 6 |
| Tratamientos explícitos `aplica: true` | 18 |
| Reglas de localización | 11 |
| **Total entidades** | **46** |

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

### 4.2. Tributos de provisión / autorretenciones (4)

| Código | Nombre | Naturaleza | Carácter retención | Nivel | Factor de tarifa | Dirección fiscal | Tributo padre |
|---|---|:---:|:---:|:---:|---|:---:|:---:|
| `AUTO_RENTA` | Autorretención de Renta | sustractivo | anticipado | nacional | `fija` | ingreso | — |
| `AUTO_RETEFUENTE` | Autorretención en la Fuente | sustractivo | anticipado | nacional | `conceptoPago` | ingreso | — |
| `AUTO_RIVA` | Autorretención de IVA | sustractivo | anticipado | nacional | `porcentajeDePadre` | gasto | `IVA` |
| `AUTO_RICA` | Autorretención de ICA | sustractivo | anticipado | municipal | `actividadEconomica` | ingreso | — |

---

## 5. Clasificaciones tributarias

| Código | Nombre | Tributos que aplican (resumen) |
|---|---|---|
| `GRAV_19` | Gravados 19% | IVA, RETEFUENTE, RIVA, ICA, RICA |
| `GRAV_5` | Gravados 5% | IVA, RETEFUENTE, RIVA, ICA, RICA |
| `EXCLUIDO` | Excluidos de IVA | RETEFUENTE, ICA, RICA |
| `EXENTO` | Exentos de IVA (tarifa 0%) | IVA, RETEFUENTE, ICA, RICA |
| `INC_8` | Gravados INC 8% | INC |
| `NO_GRAVADO` | No sujeto a impuestos | — |

---

## 6. Tratamientos (matriz clasificación × tributo)

Cada entrada declara que el tributo aplica cuando un concepto es clasificado con esa clasificación. Los tratamientos NO listados implican que el tributo no aplica por default; pueden agregarse excepciones vía `origen: personalizado`.

**Total: 18 tratamientos `aplica: true`.** Distribución:

- GRAV_19: 5 tributos (IVA, RETEFUENTE, RIVA, ICA, RICA).
- GRAV_5: 5 tributos (mismos que GRAV_19, distintas tarifas en `TarifaTributaria`).
- EXCLUIDO: 3 tributos (RETEFUENTE, ICA, RICA).
- EXENTO: 4 tributos (IVA con tarifa 0%, RETEFUENTE, ICA, RICA).
- INC_8: 1 tributo (INC).
- NO_GRAVADO: ninguno.

Las **autorretenciones** (`AUTO_*`) NO se modelan como tratamientos por clasificación — su aplicación depende de atributos del `PerfilTributario` (si la empresa es autorretenedora) y de la dirección fiscal. Se controlan vía `CondicionDeAplicacion`.

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
| `AUTO_RIVA` | sedeEmisora | — |
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
- `AUTO_RIVA` hereda de `IVA` (nacional).

Esto se modela vía `factorDeTarifa: porcentajeDePadre` — el motor primero calcula el padre, luego aplica la tarifa sobre el resultado en la misma jurisdicción.

### 8.5. Frontera con `CondicionDeAplicacion`

Este catálogo declara **qué tributos existen y a qué clasificaciones aplican por default**. Las condiciones (`CondicionDeAplicacion`) ajustan el comportamiento por **perfil tributario** del sujeto: por ejemplo, si una empresa es Régimen Simple, RETEFUENTE puede quedar exonerado para ciertos conceptos. Esas reglas viven en `co-condicion-de-aplicacion.json`.

---

## 9. Histórico de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-05-26 | Carga inicial F1: 11 tributos (7 directos + 4 autorretenciones), 6 clasificaciones, 18 tratamientos, 11 reglas de localización. Fuente Estatuto Tributario + Decretos DIAN + leyes municipales. |

---

## 10. Revisión pendiente

Preguntas para validación del **equipo de consultores fiscales**:

1. **`caracterRetencion` para tributos aditivos:** ¿Es correcto dejarlo `null` para IVA, INC, ICA, o conviene usar un valor explícito (ej: `no-aplica`)?
2. **SOBRETASA_BOMBERIL como `definitivo`:** ¿La sobretasa va directamente al fondo bomberil sin compensación, o tiene algún mecanismo de descuento que la haga `anticipado`?
3. **¿Faltan tributos en F1?** Casos conocidos no incluidos por simplicidad inicial: GMF (4×1000), Impuesto al Patrimonio, Impuesto al Consumo de Licores y Cervezas departamental, Estampillas municipales/departamentales. ¿Cuáles deben entrar en F1?
4. **`AUTO_RIVA` con `direccionFiscalAplicable: gasto`:** ¿Es correcto modelarlo así (la empresa compradora autoliquida IVA por reverse charge), o conviene otra dirección?
5. **Tratamientos para INC:** ¿Existen clasificaciones adicionales de INC (`INC_4`, `INC_16`)? Si sí, ¿cómo se diferencian (sectores)?
6. **Tratamientos para clasificaciones GRAV_*:** ¿Faltan tarifas intermedias (GRAV_8, GRAV_12, etc.)?
