# Anexo: Configuración estándar — República Dominicana (DO)

## Propósito

Contenido fiscal estándar que el producto provee precargado para República Dominicana. Corresponde a los datos iniciales (seeds) que se cargan como streams de eventos de configuración al iniciar operación — los mismos eventos que el modelo define para cada agregado.

El contenido se organiza siguiendo la estructura de los agregados del modelo de dominio:

| Agregado | Qué contiene en este anexo |
|----------|---------------------------|
| CatalogoTributario | Tributos, clasificaciones tributarias, tratamientos, reglas de localización |
| TarifaTributaria | Tarifas conocidas por tributo |
| CondicionDeAplicacion | Reglas que modifican la aplicación según perfiles tributarios |
| CatalogoDeAtributosFiscales | Atributos fiscales requeridos |
| FormatoFiscal | Formatos de entregables por autoridad fiscal |

**Fuentes:** `fuentes/Definiciones de tributos.xlsx`, `fuentes/formatos-y-entrega-reportes-fiscales.md`, DGII República Dominicana.

**Nota sobre tarifas:** Las tarifas específicas son las vigentes al momento de la elaboración de este documento. El contenido estándar del producto se actualiza cuando la normativa cambia.

---

## 1. Tributos — CatalogoTributario

Los datos del catálogo `catalogo-tributario-DO` se migraron a [`datos-precargados/do-catalogo-tributario.json`](datos-precargados/do-catalogo-tributario.json) (v1.0, 2026-05-26). Allí viven las 19 entidades iniciales F1 (5 tributos + 4 clasificaciones + 5 tratamientos + 5 reglas de localización). Narrativo: [`datos-precargados/do-catalogo-tributario.md`](datos-precargados/do-catalogo-tributario.md).

**Contexto de diseño:**
- **5 tributos:** ITBIS (equivalente IVA, 18% general), RITBIS (retención del 30% del ITBIS), ISC (Selectivo al Consumo, varía por producto), CDT (2% telecomunicaciones), PROPINA (10% hoteles/restaurantes).
- **Todos nacionales** — no hay tributos subnacionales operativos.
- **Reglas de localización:** todos resuelven por `sedeEmisora` sin fallback (la sede determina el país).

### Jurisdicciones fiscales — JurisdiccionFiscal

Las 68 jurisdicciones DR (1 nacional + 32 provincias + 35 municipios principales) se migraron a [`datos-precargados/do-jurisdiccion-fiscal.json`](datos-precargados/do-jurisdiccion-fiscal.json). Narrativo: [`datos-precargados/do-jurisdiccion-fiscal.md`](datos-precargados/do-jurisdiccion-fiscal.md).

**Contexto de diseño:** DR no tiene regímenes territoriales fiscales operativos en F1. Las zonas francas se modelan como **régimen empresarial** (Ley 8-90), no como `JurisdiccionFiscal` con `tipoRegimen`. La precarga jurisdiccional sirve principalmente para integridad referencial de `ubicaciones` enviadas por consumidores.

---

## 2. Tarifas — TarifaTributaria

Las tarifas se migraron a [`datos-precargados/do-tarifa-tributaria.json`](datos-precargados/do-tarifa-tributaria.json) (5 streams nacionales con 9 entradas).

**Contexto de diseño:**
- **ITBIS:** 18% (general), 16% (reducida), 0% (exento).
- **RITBIS:** 30% general + 100% personas físicas (NG 02-05).
- **ISC:** Telecomunicaciones 10%, Seguros 16%. Otras categorías ISC (cigarrillos, alcohol, vehículos, combustibles) requieren precarga adicional con consultores.
- **CDT:** 2% sobre servicios de telecomunicaciones.
- **PROPINA:** 10% obligatorio en hoteles y restaurantes.

---

## 3. Condiciones de aplicación — CondicionDeAplicacion

Las 9 condiciones DR se migraron a [`datos-precargados/do-condicion-de-aplicacion.json`](datos-precargados/do-condicion-de-aplicacion.json).

**Contexto de diseño:** Catálogo compacto (vs CO con 32 condiciones) — la operación dominicana es más simple. Las condiciones DR se basan más en **actividad económica CNAE del emisor** (división 61 para CDT, divisiones 55/56 para PROPINA) que en calidades del perfil. La exoneración por zona franca (Ley 8-90) es la condición más compleja.

---

## 4. Atributos fiscales — CatalogoDeAtributosFiscales

Los 6 atributos DR se migraron a [`datos-precargados/do-catalogo-de-atributos-fiscales.json`](datos-precargados/do-catalogo-de-atributos-fiscales.json).

**Contexto de diseño:**
- **3 requeridos:** `tipoContribuyente`, `rnc`, `ncf`.
- **3 opcionales:** `esAgenteRetencionITBIS`, `esGranContribuyente`, `inscripcionParqueZonaFranca` (con `catalogoReferencia`).
- Compacto vs CO (15 atributos) — DR no tiene Régimen Simple, autorretenciones múltiples ni atributos municipales.

---

## 5. Regímenes especiales empresariales — CatalogoDeRegimenesEspeciales

Los regímenes empresariales DR se migraron a [`datos-precargados/do-catalogo-de-regimenes-especiales.json`](datos-precargados/do-catalogo-de-regimenes-especiales.json) (18 parques de zonas francas precargados).

**Contexto de diseño:**
- **Único tipo vigente en F1 DR:** `zona-franca` (administrado por CNZFE bajo Ley 8-90).
- 15 ZFs industriales + 3 ZFs de servicios.
- Cobertura total estimada ~75 parques certificados — la precarga inicial es muestra representativa.
- **Frontera con `JurisdiccionFiscal`:** las ZFs son **empresariales** (dependen de inscripción) — no hay regímenes territoriales fiscales en DR.

---

## 6. Formatos fiscales — FormatoFiscal

Los formatos fiscales se migraron a [`datos-precargados/do-formato-fiscal.json`](datos-precargados/do-formato-fiscal.json) (4 formatos DGII: F-606, F-607, F-608, F-609). La homologación oficial DGII se migra a [`datos-precargados/do-homologacion-fiscal-dgii.json`](datos-precargados/do-homologacion-fiscal-dgii.json) (18 equivalencias).

**Contexto de diseño:**
- **4 formatos DGII** mensuales (día 15 mes siguiente) en formato XML.
- **F-606 y F-609** siempre obligatorios; F-607 y F-608 redundantes si el contribuyente emite 100% e-CF (los e-CF llegan a DGII en tiempo real).
- **Migración NCF → e-CF** progresiva (Normas Generales 06-2018 + 06-2021).
- Homologaciones DGII pendientes de validación con consultores — los códigos precargados son denominaciones internas razonables.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: 5 tributos, 4 clasificaciones, condiciones simples, 3 atributos fiscales, formatos DGII (606-609). |
| 1.1 | Mayo 2026 | Cambio 3 — Sub-cambio 3.4: nueva Sección 5 con régimen empresarial de zonas francas precargado (~75 parques CNZFE, Ley 8-90). Atributo fiscal nuevo en Sección 4 (`inscripcionParqueZonaFranca`) con `catalogoReferencia`. Renumeración de Sección 5 (Formatos) a Sección 6. `[D13]` `[I16]`. |
