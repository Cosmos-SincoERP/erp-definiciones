# Sub-dominio de Terceros — Definición transversal

> **Fecha:** Marzo 2026
> **Propósito:** Definir el sub-dominio de Terceros como concepto transversal del ERP, su responsabilidad, su patrón de integración con los demás sub-dominios y la decisión sobre su nomenclatura.
> **Versión:** 1.0

---

## 1. Definición

El sub-dominio de Terceros es el registro centralizado de todas las personas y empresas con las que la organización tiene relación: proveedores, clientes, empleados, entidades financieras, socios comerciales y cualquier otra parte. Es la fuente de verdad de la pregunta "¿quién es este tercero?" y gobierna la creación, actualización e inactivación de estos registros.

Sigue el modelo unificado: un mismo tercero que es proveedor y cliente tiene un solo registro con múltiples roles. Cada sub-dominio consumidor enriquece al tercero con sus propios datos (datos comerciales en OXP, perfil tributario en Impuestos, datos laborales en Nómina) sin duplicar la identidad base.

---

## 2. Decisión sobre el nombre

Se evaluaron los siguientes términos:

| Término | Quién lo usa | Evaluación |
|---------|-------------|-----------|
| **Contactos** | Alegra, Odoo, Holded, Xero | Término dominante en ERPs modernos cloud-native. Descartado porque en nuestro modelo un contacto es un componente **dentro** del tercero (personas de contacto con rol, email, celular). Usar "Contactos" para el sub-dominio confunde el todo con una parte. Además suena más a CRM/ventas que a ERP contable. |
| **Business Partner** | SAP S/4HANA | Término estándar internacional en inglés. Sin traducción natural al español que funcione bien. |
| **Party** | Oracle Fusion (TCA), Dynamics 365 (GAB) | Término técnico. "Partes" en español suena jurídico. |
| **Terceros** | Siigo, Helisa, World Office, ERPs colombianos, DIAN, PUC | Término estándar en contabilidad colombiana y latinoamericana. Universalmente entendido en el contexto fiscal y contable de la región. |
| **Entidades** | DGII (RD), algunos ERPs | Puede confundirse con "entidad legal" (la empresa misma). |
| **Directorio** | — | Neutro pero abstracto. No dice "de quién". |

**Decisión:** El sub-dominio se nombra **Terceros**. Es el término estándar en el contexto contable y fiscal de los países donde el producto opera (Colombia, República Dominicana, Panamá). La DIAN, el PUC y los ERPs locales lo usan consistentemente. La presentación en la interfaz de usuario (menú, navegación) se define por separado — el nombre del sub-dominio en la documentación técnica no tiene que ser igual al nombre en la UI.

---

## 3. Responsabilidad del sub-dominio

| Responsabilidad | Descripción |
|----------------|-------------|
| **Creación de terceros** | Validar unicidad (tipo de documento + número), crear el registro base. Los sub-dominios consumidores pueden solicitar la creación desde sus propios flujos. |
| **Datos de identificación** | Tipo de documento, número de identificación, razón social / nombre, dirección principal. |
| **Contactos del tercero** | Personas de contacto dentro del tercero, con rol (comercial, tesorero, representante legal), email y teléfono. |
| **Roles del tercero** | Un tercero puede tener múltiples roles: proveedor, cliente, empleado, entidad financiera, otro. Los roles determinan en qué sub-dominios se habilita. |
| **Estado activo / inactivo** | Un tercero inactivo no puede usarse en nuevas transacciones. Los registros históricos que lo referencian se conservan intactos. |

---

## 4. Datos que NO gestiona este sub-dominio

Cada sub-dominio consumidor enriquece al tercero con datos propios de su contexto:

| Sub-dominio | Datos propios | Dónde viven |
|-------------|--------------|-------------|
| **Impuestos** | Perfil tributario (régimen, autorretenedor, gran contribuyente, agente de retención) | Sub-dominio de Impuestos |
| **OXP** | Condiciones comerciales como proveedor (plazos de pago, moneda, etc.) | Sub-dominio de OXP |
| **CXC** | Condiciones comerciales como cliente (límite de crédito, plazos, etc.) | Sub-dominio de CXC |
| **Tesorería** | Cuentas bancarias del tercero | Sub-dominio de Tesorería |
| **Nómina** | Datos laborales (cargo, salario, tipo de contrato) | Sub-dominio de Nómina |
| **Contabilidad** | No almacena datos propios del tercero — recibe la referencia en las líneas de traducción | Sub-dominio de Contabilidad |

---

## 5. Patrón de integración (EDA)

### Eventos que publica el sub-dominio de Terceros

| Evento | Cuándo se publica | Datos principales |
|--------|-------------------|-------------------|
| **TerceroCreado** | Al crear un nuevo tercero | Tipo documento, número, razón social, dirección, roles asignados |
| **TerceroActualizado** | Al modificar datos de identificación (ej: cambio de razón social) | Número de identificación, campos modificados |
| **TerceroInactivado** | Al inactivar un tercero | Número de identificación |
| **TerceroReactivado** | Al reactivar un tercero previamente inactivo | Número de identificación |

### Servicio de creación desde consumidores

Los sub-dominios consumidores pueden solicitar la creación de un tercero cuando lo necesiten desde sus propios flujos (ej: OXP necesita radicar una obligación con un proveedor que no existe). El sub-dominio de Terceros valida las reglas propias (unicidad, datos mínimos) y crea el registro. El consumidor recibe la confirmación y puede continuar su flujo.

### Consumo por sub-dominios

Cada sub-dominio que necesite terceros:
1. Escucha los eventos relevantes (TerceroCreado, TerceroActualizado, TerceroInactivado)
2. Almacena una referencia local (tipo documento + número de identificación)
3. Enriquece con sus datos propios
4. Valida estado activo al momento de crear transacciones

---

## 6. Estado del sub-dominio

Este sub-dominio **no tiene alcance ni modelo de dominio definido aún**. Este documento registra la definición transversal y el patrón de integración acordado durante la construcción del alcance de Contabilidad. La construcción formal del sub-dominio (definicion-alcance.md, modelo-dominio.md) queda como trabajo futuro.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: definición del sub-dominio, decisión de nomenclatura, responsabilidades, datos propios vs consumidores, patrón de integración EDA. |
