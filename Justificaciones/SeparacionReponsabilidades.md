El problema

  ConceptoDeGasto (dominio OXP)
    └── DestinoContable { centroCosto, cuentaContable, porcentaje }
                           │               │
                           │               └── Esto es del dominio contable
                           └── ¿Es de negocio o contable?

  OXP está asumiendo responsabilidad de saber a qué cuenta va cada gasto, cuando eso debería ser deducido por reglas al momento de traducir la información para SincoA&F.

  Lo que OXP realmente sabe

  OXP sabe qué se compró, cuánto costó y cómo se distribuye el costo en términos de negocio. No sabe (ni debería saber) cómo se contabiliza.

  OxpComercio (dominio OXP)
  │
  ├── ConceptoDeGasto #1
  │    codigo: A1
  │    descripcion: "Suministros oficina"
  │    cantidad: 1
  │    valor: 1.000.000
  │    distribucion:
  │     ○ { destino: "Ventas",        porcentaje: 60% }
  │     ○ { destino: "Administración", porcentaje: 40% }
  │
  └── ConceptoDeGasto #2
       codigo: B3
       descripcion: "Viáticos"
       cantidad: 1
       valor: 500.000
       distribucion:
        ○ { destino: "Comercial", porcentaje: 100% }

  Servicio de Traducción Contable (frontera OXP → SincoA&F)
  │
  │  Recibe: ConceptoDeGasto + distribucion
  │  Aplica: Reglas/patrones de traducción
  │  Produce: Líneas contables
  │
  │  ConceptoDeGasto "Suministros" + destino "Ventas" 60%
  │    → cuenta: 6101, CC: CC-VTA-001, naturaleza: débito, valor: 600.000
  │
  │  ConceptoDeGasto "Suministros" + destino "Administración" 40%
  │    → cuenta: 6102, CC: CC-ADM-001, naturaleza: débito, valor: 400.000

  La separación de responsabilidades
  ┌─────────┬──────────────────────────────────────────────────────────────────────┬─────────────────────────────────────────────────────┐
  │   Qué   │                             Dominio OXP                              │                 Traducción Contable                 │
  ├─────────┼──────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
  │ Sabe    │ Qué se compró, a quién, por cuánto, cómo se distribuye en el negocio │ Cómo traducir esa información a asientos contables  │
  ├─────────┼──────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
  │ No sabe │ Cuentas contables, naturalezas débito/crédito                        │ Reglas de negocio de la OXP (estados, conciliación) │
  ├─────────┼──────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
  │ Ejemplo │ "60% para Ventas, 40% para Admin"                                    │ "Ventas → cuenta 6101, CC-VTA-001, débito"          │
  └─────────┴──────────────────────────────────────────────────────────────────────┴─────────────────────────────────────────────────────┘
  Estructura corregida

  OxpComercio (Agregado)
  │
  ├── Entidades internas:
  │   ├── ConceptoDeGasto
  │   │    └── distribuciones: List<DestinoDeNegocio> (VO)
  │   │         Invariante I2: suma = 100%
  │   │
  │   ├── ConceptoDeImpuesto
  │   │    (sin distribución)
  │   │
  │   └── ConceptoDeRetencion
  │        (sin distribución)
  │
  └── Value Objects:
      ├── InformacionTercero
      ├── MedioDePago
      ├── ValorMonetario
      ├── SoporteDocumental
      └── DestinoDeNegocio { destino, porcentaje }

  DestinoContable → DestinoDeNegocio. Sin cuenta contable, sin naturaleza, sin centro de costo contable. Solo el destino organizacional y el porcentaje. La
  traducción a lenguaje contable ocurre en la frontera, por reglas que el dominio OXP no conoce.