# Catálogos de conceptos por dominio — Decisión arquitectónica

## Problema

Los sub-dominios consumidores del ERP (OXP, CXC, Nómina, etc.) necesitan
catálogos de productos/servicios/conceptos para clasificar sus transacciones.
Cada catálogo debe vincular referencias fiscales (clasificación tributaria,
concepto de pago) del sub-dominio de Impuestos para habilitar el cálculo
tributario.

¿Debe existir un catálogo centralizado o cada dominio gestiona el suyo?

## Decisión

**Modelo federado: cada dominio de gestión es dueño de su catálogo.**

- Cada catálogo tiene atributos particulares del dominio + referencias fiscales
  obligatorias a los catálogos de Impuestos.
- Impuestos es la fuente de verdad fiscal — publica los catálogos de
  clasificaciones tributarias y conceptos de pago. Nadie los duplica.
- No existe un catálogo centralizado de conceptos transversal.

## Justificación

Se evaluaron tres alternativas:

| Alternativa | Ventaja principal | Riesgo principal |
|---|---|---|
| Centralizado único | Cero duplicación, gobierno simple | Se degrada con el tiempo: atrae atributos de todos los dominios, se vuelve God Module, cuello de botella organizacional |
| Centralizado segmentado | Cero duplicación con filtros | Mismo riesgo de degradación — los segmentos terminan compartiendo atributos |
| **Federado (elegido)** | Autonomía total, cada dominio evoluciona a su ritmo | Requiere disciplina en las referencias fiscales |

La experiencia con el ERP actual demostró que el modelo centralizado se
degradó al cargarle responsabilidades propias de cada dominio sin respetar
su esencia.

## Gobierno fiscal

El gobierno no requiere un catálogo centralizado — requiere que Impuestos
sea la fuente de verdad:

```
┌─────────────────────────────────────────────┐
│          Impuestos (centralizado)             │
│                                               │
│  Catálogo clasificaciones tributarias         │
│  Catálogo conceptos de pago                   │
│  (contenido fiscal por jurisdicción)          │
└──────────────────┬────────────────────────────┘
                   │ referencia (no duplicación)
     ┌─────────────┼─────────────┬──────────────┐
     ▼             ▼             ▼              ▼
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│ Catálogo │ │ Catálogo │ │ Catálogo │ │ Catálogo │
│ Compras  │ │ OXP      │ │ Arrend.  │ │ Nómina   │
│ Constr.  │ │ (gasto   │ │          │ │          │
│          │ │ directo) │ │          │ │          │
└──────────┘ └──────────┘ └──────────┘ └──────────┘
```

Si Impuestos desactiva una clasificación tributaria, emite evento
(`ClasificacionTributariaDesactivada`). Cada dominio que la use reacciona
según su criterio (alerta, bloqueo, etc.).

## Catálogos identificados

| Dominio dueño | Catálogo | Atributos propios (ejemplos) | Consumidores transaccionales |
|---|---|---|---|
| Compras Construcción | Ítems de construcción | unidadMedida, codigoUNSPC, categoriaAprovisionamiento | Compras → OXP |
| OXP | Tipos de gasto directo | (mínimo — solo código y descripción) | OXP (modo directo) |
| Arrendamiento | Conceptos de arrendamiento | tipoInmueble, periodicidad, porcentajeComision | Arrendamiento → OXP (gasto) y → CXC (ingreso) |
| Nómina | Conceptos laborales | tipoNovedad, baseLiquidacion, topeLegal | Nómina → su módulo transaccional |
| Facturación / Ventas | Productos y servicios vendidos | (por definir) | Facturación → CXC |

## Estructura común obligatoria

Independiente de los atributos particulares, todo catálogo debe incluir:

| Campo | Tipo | Obligatorio | Fuente |
|---|---|---|---|
| código | string | Sí | Propio del dominio |
| descripción | string | Sí | Propio del dominio |
| clasificacionTributaria | string (ref.) | Sí | Catálogo de Impuestos |
| conceptoPago | string (ref.) | Sí | Catálogo de Impuestos |
| activo | boolean | Sí | Propio del dominio |

## Contrato de envío a módulos transaccionales

Cuando un módulo de gestión envía conceptos a un módulo transaccional
(OXP, CXC), usa un contrato estandarizado:

```
ConceptoParaTransaccional {
  codigo:                   string
  descripcion:              string
  cantidad:                 decimal
  valor:                    ValorMonetario
  clasificacionTributaria:  string (ref. Impuestos)
  conceptoPago:             string (ref. Impuestos)
}
```

El módulo transaccional agrega internamente:
- `subDominioOrigen` (a nivel del agregado raíz): deducido de la identidad
  del consumidor del comando (no enviado por el consumidor) `[SI5]`
- `referenciaOrigen` (a nivel del concepto): el código del concepto en el
  catálogo del dominio origen

## Flujo de resolución de referencias fiscales

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│  Módulo de gestión (ej: Compras)                                │
│                                                                  │
│  1. Usuario selecciona ítem del catálogo de Compras              │
│     "Varilla corrugada 1/2" (MAT-HC-042)                        │
│                                                                  │
│  2. El catálogo tiene configurado:                               │
│     clasificacionTributaria: "Bien gravado 19%"                  │
│     conceptoPago: "Compras"                                      │
│     (resueltos cuando se configuró el ítem,                      │
│      seleccionando del catálogo de Impuestos)                    │
│                                                                  │
│  3. Compras envía a OXP el ConceptoParaTransaccional             │
│     con las referencias ya resueltas                             │
│                                                                  │
└──────────────────────────────────┬──────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│  Módulo transaccional (OXP)                                     │
│                                                                  │
│  4. Crea OxpComercio con subDominioOrigen: "Compras" [SI5]      │
│     Crea ConceptoDeGasto con clasificacionTributaria,            │
│     conceptoPago y referenciaOrigen: "MAT-HC-042"               │
│                                                                  │
│  5. Solicita cálculo a Impuestos con esos datos                  │
│                                                                  │
└──────────────────────────────────┬──────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│  Impuestos                                                       │
│                                                                  │
│  6. Recibe clasificacionTributaria + conceptoPago                │
│     Resuelve qué tributos aplican, calcula, retorna desglose    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Directriz para nuevos sub-dominios

Al definir un nuevo sub-dominio que genere transacciones con impacto fiscal:

1. Definir el catálogo propio del dominio con los atributos de negocio
   que necesita.
2. Incluir los campos obligatorios: clasificacionTributaria y conceptoPago
   (refs. a catálogos de Impuestos).
3. Modelar el catálogo como agregado con eventos para trazabilidad.
4. Al enviar transacciones a módulos transaccionales (OXP, CXC), usar el
   contrato estandarizado ConceptoParaTransaccional.
5. No crear catálogos intermedios ni compartidos — cada dominio es autónomo.

## Canales de entrada agnósticos — Directriz transversal

Los sub-dominios transaccionales del ERP (OXP, CXC, etc.) reciben documentos
por múltiples canales de entrada. La postura arquitectónica es:

### Principio

Los canales de entrada (SincoRE, servicio de extracción de datos, carga manual)
son **agnósticos al origen** — entregan datos extraídos sin clasificar. La
clasificación y enrutamiento es responsabilidad del sub-dominio receptor.

### Separación de responsabilidades

| Capa | Responsabilidad | Ejemplo |
|---|---|---|
| **Infraestructura (transversal)** | Extracción de datos del documento | SincoRE (XML), servicio de extracción (PDF, imágenes), parsing (CSV) |
| **Aplicación (del sub-dominio)** | Clasificación del origen y resolución de referencias fiscales | OXP clasifica si es directa o de sub-dominio de gestión, resuelve clasificacionTributaria |
| **Dominio** | Registro y ciclo de vida de la obligación | OxpComercio con subDominioOrigen y ConceptoDeGasto |

### Clasificación inteligente

La clasificación NO se implementa con tablas configurables estáticas ni flujos
de enrutamiento rígidos. Se espera que opere con mecanismos inteligentes y
adaptativos. Ejemplos de mecanismos posibles:

1. **Coincidencia con documentos pendientes** — buscar en sub-dominios de
   gestión integrados (órdenes de compra, contratos) por tercero + monto
   compatible.
2. **Aprendizaje por repetición** — mismo tercero + patrón similar →
   misma clasificación anterior.
3. **Asistencia por IA** — cuando no hay historial ni documentos pendientes.
4. **Confirmación del usuario** — siempre puede corregir la sugerencia.

### Directriz para nuevos sub-dominios

Al definir un sub-dominio que reciba documentos externos:

1. No crear lógica de extracción propia — consumir el servicio transversal
   (SincoRE para XML, servicio de extracción para PDF/imágenes).
2. Implementar la clasificación y enrutamiento como capability de la capa
   de aplicación del sub-dominio, no como domain service.
3. No depender de tablas configurables estáticas — preferir mecanismos
   inteligentes y adaptativos.
4. Cuando el documento trae información fiscal del emisor, validarla contra
   el cálculo propio del sub-dominio de Impuestos.
