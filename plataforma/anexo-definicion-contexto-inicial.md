# Plataforma — Datos base del ERP

> **Fecha:** Marzo 2026
> **Propósito:** Definir los catálogos y datos base que son transversales a todos los sub-dominios del ERP. Estos datos no pertenecen a ningún sub-dominio de negocio — son infraestructura compartida sobre la cual operan todos los demás.
> **Versión:** 1.0

---

## 1. Definición

La plataforma contiene los catálogos y datos base que todos los sub-dominios del ERP necesitan pero que ninguno posee como parte de su dominio de negocio. Son datos relativamente estáticos, sin procesos de negocio complejos ni ciclos de vida propios más allá de la creación y el mantenimiento básico.

La diferencia con los sub-dominios transversales (Terceros, Estructura Organizacional) es que estos datos **no tienen comportamiento de negocio**: no se reestructuran, no tienen procesos orquestados, no publican eventos que desencadenen acciones en otros dominios. Son catálogos de referencia.

---

## 2. Catálogos identificados

| Catálogo | Descripción | Consumidores principales |
|----------|-------------|-------------------------|
| **Países** | Catálogo de países con código ISO 3166-1. | Terceros (dirección), Impuestos (jurisdicción), OXP (compras del exterior) |
| **Divisiones territoriales** | Departamentos, estados, provincias y ciudades por país. | Terceros (dirección), Impuestos (jurisdicción municipal) |
| **Monedas** | Catálogo de monedas con código ISO 4217. | OXP (moneda de operación), CXC, Contabilidad (moneda del asiento), Tesorería |
| **Tipos de documento de identidad** | NIT, CC, CE, Pasaporte, RNC, RUC, etc. Por país. | Terceros (identificación), Impuestos (entidad fiscal) |
| **Tipos de empresa** | Persona natural, persona jurídica, entidad sin ánimo de lucro, etc. | Terceros, Impuestos |
| **Tasas de cambio** | Tasa de cambio entre monedas, con fecha de vigencia. Fuente: Banco de la República, Banco Central de RD, etc. | OXP (conversión de moneda extranjera), Contabilidad (moneda del asiento), CXC |

---

## 3. Características comunes

| Característica | Descripción |
|---------------|-------------|
| **Estáticos o de cambio lento** | Estos catálogos cambian con muy poca frecuencia. Un país nuevo, una moneda nueva o un tipo de documento nuevo son eventos excepcionales. La excepción es tasas de cambio que se actualizan diariamente. |
| **Sin procesos de negocio** | No tienen flujos, estados ni ciclos de vida complejos. Son datos de referencia que se consultan. |
| **Preconfigurados por el producto** | El sistema viene con los catálogos precargados para los países donde opera (Colombia, República Dominicana, Panamá). El administrador puede extenderlos si necesita cubrir otros países. |
| **Consumidos por todos los sub-dominios** | Cualquier sub-dominio puede consultar estos catálogos. No hay restricción de acceso. |

---

## 4. ¿Por qué no es un sub-dominio?

Un sub-dominio en DDD justifica su existencia cuando tiene:
- Reglas de negocio propias → estos catálogos no las tienen
- Comportamiento propio → no tienen comportamiento, solo datos
- Ciclo de vida complejo → se crean y eventualmente se inactivan, nada más
- Procesos de negocio → no desencadenan procesos en otros dominios

Comparación:

| Concepto | ¿Reglas propias? | ¿Comportamiento? | ¿Procesos? | Clasificación |
|----------|:---:|:---:|:---:|---|
| **Terceros** | Sí (unicidad, roles, contactos) | Sí (creación, inactivación) | Sí (creación desde consumidores) | Sub-dominio |
| **Estructura Organizacional** | Sí (jerarquía, codificación, tipos) | Sí (reestructuración) | Sí (reestructuración orquestada) | Sub-dominio |
| **Países, monedas, tipos de doc.** | No | No | No | Plataforma (datos base) |

---

## 5. Estado

Este documento registra la definición inicial de los datos base del ERP. No requiere un proceso formal de alcance y modelo de dominio — es infraestructura de soporte. Los catálogos se irán completando a medida que los sub-dominios los necesiten.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: definición de plataforma, 6 catálogos identificados, justificación de por qué no es un sub-dominio. |
