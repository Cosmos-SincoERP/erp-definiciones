# Anexo: Configuración estándar — Panamá (PA)

## Propósito

Contenido fiscal estándar que el producto provee precargado para Panamá. Corresponde a los datos iniciales (seeds) que se cargan como streams de eventos de configuración al iniciar operación — los mismos eventos que el modelo define para cada agregado.

El contenido se organiza siguiendo la estructura de los agregados del modelo de dominio:

| Agregado | Qué contiene en este anexo |
|----------|---------------------------|
| CatalogoTributario | Tributos, clasificaciones tributarias, tratamientos, reglas de localización |
| TarifaTributaria | Tarifas conocidas por tributo |
| CondicionDeAplicacion | Reglas que modifican la aplicación según perfiles tributarios |
| CatalogoDeAtributosFiscales | Atributos fiscales requeridos |
| FormatoFiscal | Formatos de entregables por autoridad fiscal |

**Fuentes:** `fuentes/Definiciones de tributos.xlsx`, DGI Panamá.

**Nota sobre tarifas:** Las tarifas específicas son las vigentes al momento de la elaboración de este documento. El contenido estándar del producto se actualiza cuando la normativa cambia.

---

## 1. Tributos — CatalogoTributario

Los datos del catálogo `catalogo-tributario-PA` se migraron a [`datos-precargados/pa-catalogo-tributario.json`](datos-precargados/pa-catalogo-tributario.json) (v1.2, 2026-07-31). Allí viven las 21 entidades F1 (4 tributos + 5 clasificaciones + 8 tratamientos + 4 reglas de localización). Narrativo: [`datos-precargados/pa-catalogo-tributario.md`](datos-precargados/pa-catalogo-tributario.md).

**Contexto de diseño:**
- **4 tributos:** ITBMS (tarifa progresiva 7/10/15%), RITBMS (50% del ITBMS), ISC (varía por producto), ISR (retenciones por concepto).
- **Todos nacionales** — sin tributos subnacionales operativos en F1.
- **ISR como tributo transaccional** (retenciones) — Panamá modela las retenciones del Impuesto sobre la Renta como tributo del catálogo, no solo como provisión anual.

### Jurisdicciones fiscales — JurisdiccionFiscal

Las 25 jurisdicciones PA (1 nacional + 10 provincias + 3 comarcas + 11 distritos) se migraron a [`datos-precargados/pa-jurisdiccion-fiscal.json`](datos-precargados/pa-jurisdiccion-fiscal.json). Narrativo: [`datos-precargados/pa-jurisdiccion-fiscal.md`](datos-precargados/pa-jurisdiccion-fiscal.md).

**Contexto de diseño:** Las áreas económicas especiales panameñas (ZLC, AEEPP, Ciudad del Saber) son **regímenes empresariales** (dependen de inscripción), NO territoriales — `JurisdiccionFiscal` no tiene entradas con `tipoRegimen` para PA.

---

## 2. Tarifas — TarifaTributaria

Las tarifas se migraron a [`datos-precargados/pa-tarifa-tributaria.json`](datos-precargados/pa-tarifa-tributaria.json) (v1.1, 2026-07-31 — 4 streams nacionales con 12 entradas).

**Contexto de diseño:**
- **ITBMS:** 3 tarifas progresivas (7% general, 10% alcohol/hospedaje, 15% tabaco) + 0% exento.
- **RITBMS:** 50% del ITBMS causado (norma general, Decreto Ejecutivo 470 de 2015); `porcentajeDePadre` con padre ITBMS — hereda su ciclo de vida. Variantes del 100% (Estado-servicios, no residentes) pendientes de modelar.
- **ISC:** Tarifas por categoría (telecomunicaciones móvil 5%, joyas/armas 5%) — otras categorías ISC (vehículos, alcohol, combustibles) pendientes.
- **ISR:** 5 conceptos precargados (honorarios 15%, dividendos 10%, intereses 5%, alquileres 12.5%, pagos exterior 12.5%).

---

## 3. Condiciones de aplicación — CondicionDeAplicacion

Las 11 condiciones PA se migraron a [`datos-precargados/pa-condicion-de-aplicacion.json`](datos-precargados/pa-condicion-de-aplicacion.json).

**Contexto de diseño:**
- **Régimen territorial de renta:** condición `ISR-02-territorial` materializa que pagos al exterior por servicios prestados desde el extranjero NO están sujetos a ISR (principio territorial panameño).
- **CDIs:** condición `ISR-03-cdi` reconoce tarifas reducidas por Convenios para Evitar Doble Imposición. Tabla CDI pendiente de modelar.
- **Exoneraciones por áreas económicas:** condiciones `ITBMS-02-zlc` (ZLC) y `ITBMS-03-aeepp` (AEEPP) — alcance específico pendiente de refinamiento con consultores.

---

## 4. Atributos fiscales — CatalogoDeAtributosFiscales

Los 8 atributos PA se migraron a [`datos-precargados/pa-catalogo-de-atributos-fiscales.json`](datos-precargados/pa-catalogo-de-atributos-fiscales.json).

**Contexto de diseño:**
- **3 requeridos:** `tipoContribuyente`, `ruc`, `esContribuyenteITBMS`.
- **5 opcionales:** `esAgenteRetencionITBMS`, `esAgenteRetencionISR`, + 3 con `catalogoReferencia` (uno por cada área económica especial).
- **Tres atributos para tres regímenes** — modelado explícito para distinguir ZLC, AEEPP y Ciudad del Saber en condiciones.

---

## 5. Regímenes especiales empresariales — CatalogoDeRegimenesEspeciales

Los 3 regímenes empresariales PA se migraron a [`datos-precargados/pa-catalogo-de-regimenes-especiales.json`](datos-precargados/pa-catalogo-de-regimenes-especiales.json).

**Contexto de diseño:**
- **Único tipo vigente en F1 PA:** `zona-economica-especial`.
- **3 entradas atómicas:** ZLC (Colón, Decreto Ley 18/1948), AEEPP (Arraiján, Ley 41/2004), Ciudad del Saber (Clayton, Decreto Ley 6/1998).
- **Subtipos distintos** (`zona-libre-colon`, `panama-pacifico`, `ciudad-del-saber`) — permiten distinguir en condiciones de aplicación.
- **Cardinalidad atómica:** una entrada por régimen (no múltiples parques como CO/DR).
- **Frontera con `JurisdiccionFiscal`:** las áreas están físicamente en PA pero NO son regímenes territoriales — el beneficio depende de inscripción empresarial.

---

## 6. Formatos fiscales — FormatoFiscal

Propuesta inicial migrada a [`datos-precargados/pa-formato-fiscal.json`](datos-precargados/pa-formato-fiscal.json) (v0.1-placeholder, 5 formatos propuestos). Homologación DGI placeholder en [`datos-precargados/pa-homologacion-fiscal-dgi.json`](datos-precargados/pa-homologacion-fiscal-dgi.json) (14 equivalencias propuestas).

**Estado: pendiente de validación con consultores fiscales PA.** Los formatos exactos que DGI Panamá espera no estaban documentados en las fuentes disponibles. La precarga incluye obligaciones declarativas razonablemente esperables (Declaración mensual ITBMS, Declaración anual ISR PJ/PN, Declaración mensual retenciones, Certificado anual de retenciones) pero requiere validación detallada antes de implementar. Ver el `.md` con las preguntas bloqueantes específicas.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: 4 tributos, 5 clasificaciones, condiciones simples, 2 atributos fiscales. Formatos DGI pendientes. |
| 1.1 | Mayo 2026 | Cambio 3 — Sub-cambio 3.4: nueva Sección 5 con regímenes empresariales precargados (Zona Libre de Colón, AEEPP Panamá-Pacífico, Ciudad del Saber). 3 atributos fiscales nuevos en Sección 4 (`inscripcionZonaLibreColon`, `inscripcionAEEPP`, `inscripcionCiudadDelSaber`) con `catalogoReferencia`. Renumeración de Sección 5 (Formatos) a Sección 6. `[D13]` `[I16]`. |
| 1.2 | Julio 2026 | Contexto de RITBMS alineado a la resolución del issue #109: 50% del **ITBMS causado** (Decreto Ejecutivo 470 de 2015), `porcentajeDePadre` con padre ITBMS — hereda su ciclo de vida; variantes del 100% (Estado-servicios, no residentes) anotadas como pendientes. Referencias a catálogo PA v1.2 y tarifas v1.1 (12 entradas tras consolidar el stream RITBMS). |
