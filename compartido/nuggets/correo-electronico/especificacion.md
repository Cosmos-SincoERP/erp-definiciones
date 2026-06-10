# Nugget `CorreoElectronico` — Especificación

| | |
|---|---|
| **Estado** | En especificación — borrador para revisión |
| **Versión** | 0.1 |
| **Gobernanza** | [gobernanza-nuggets.md](../gobernanza-nuggets.md) · **Catálogo:** [catalogo-nuggets.md](../catalogo-nuggets.md) |

## Sección 1: Concepto

El **Correo Electrónico** es una dirección de correo válida en formato, con la que una persona o empresa puede ser contactada o notificada (incluida la entrega de documentos electrónicos: facturas, certificados).

**Origen del concepto:** VO `CorreoElectronico` del modelo de Terceros v1.0 (sección 3.3.3). El atributo `preferido` **no se hereda** — es la relación con el contacto que lo posee, vive en el consumidor (mismo criterio que en `Telefono`).

## Sección 2: Atributos

| Atributo | Tipo | Obligatorio | Descripción |
|----------|------|:-----------:|-------------|
| `valor` | string (≤254) | Sí | La dirección de correo, almacenada en minúsculas. |

El Nugget es inmutable. **Igualdad:** por `valor` — como la construcción normaliza a minúsculas, la comparación resulta insensible a mayúsculas (práctica universal de los proveedores de correo; evita duplicados por capitalización en la bodega).

## Sección 3: Reglas de validación

| Regla | Descripción |
|-------|-------------|
| `[V01]` | **Normalización:** sin espacios al inicio/fin; conversión a minúsculas. Espacios internos invalidan (`[V02]`). |
| `[V02]` | **Formato:** cumple RFC 5322 (expresión robusta), sin espacios, con exactamente una `@`. |
| `[V03]` | **Longitud:** máximo 254 caracteres (límite del estándar). |
| `[V04]` | **Dominio:** la parte posterior a la `@` contiene al menos un punto (ej: `usuario@dominio.com`). |

## Sección 4: Datos embebidos

Ninguno — el concepto valida por reglas puras, sin catálogos.

## Sección 5: Ejemplos

| Entrada | Resultado | Razón |
|---------|-----------|-------|
| `Compras@Proveedor.com.co` | ✅ `compras@proveedor.com.co` | Normalizado a minúsculas. |
| `compras@proveedor` | ❌ | `[V04]`: dominio sin punto. |
| `compras proveedor@dominio.com` | ❌ | `[V02]`: espacio interno. |

## Sección 6: Fuera de responsabilidad

| Fuera del Nugget | Responsable |
|------------------|-------------|
| Marca de **preferido** dentro de una colección. | El consumidor (invariante de colección del Contacto en Terceros). |
| **Verificación del dominio** (registro MX) — implica salir del proceso (filtro 3). | Capacidad externa no bloqueante (ya estaba así en Terceros: validación opcional documentada fuera del VO). |
| **Verificación de existencia del buzón** / confirmación por enlace. | Proceso del consumidor que necesite la garantía (ej: Emisión Electrónica antes de enviar facturas). |

## Sección 7: Consumidores

Previstos según la [matriz del catálogo](../catalogo-nuggets.md#matriz-de-consumidores): Terceros (correos del contacto), CXC/Facturación; candidato natural adicional: Emisión Electrónica (correo de entrega de documentos).

## Sección 8: Revisión pendiente

Ninguna — el Nugget queda listo para promoción a `Publicado` v1.0 con la revisión editorial del custodio.

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.1 | Junio 2026 | Borrador inicial desde el VO de Terceros v1.0. `preferido` sale del VO. Normalización a minúsculas para igualdad insensible a capitalización. 4 reglas, sin datos embebidos, sin pendientes. |
