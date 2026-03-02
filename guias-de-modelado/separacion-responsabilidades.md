# Guía: Separación de responsabilidades entre sub-dominios

## Propósito

Guía para identificar y corregir fugas de responsabilidad entre sub-dominios. Aplica a todos los sub-dominios del ERP.

---

## 1. Principio: cada sub-dominio solo conoce su vocabulario de negocio

Un sub-dominio modela **su** área de conocimiento. Si un agregado contiene atributos que pertenecen al vocabulario de otro dominio, hay una fuga de responsabilidad.

Preguntas de validación:
- ¿Este atributo es algo que el usuario de **mi** dominio entiende y manipula?
- ¿Si elimino el otro dominio del sistema, este atributo pierde sentido?
- ¿Este atributo cambia por reglas que **mi** dominio no conoce?

Si alguna respuesta indica que el atributo pertenece a otro dominio, debe extraerse.

---

## 2. Señal de alerta: atributos de otro dominio dentro de un agregado

Síntomas típicos:
- Campos como `cuentaContable`, `centroCosto`, `naturalezaDebito` en un agregado que no es del dominio Contabilidad.
- Campos como `codigoImpuesto`, `tarifaRetencion` en un agregado que no es del dominio Impuestos.
- Campos que solo tienen sentido cuando se "traduce" la información a otro sistema.

**Regla:** Si el valor del campo es determinado por reglas que no pertenecen a tu dominio, ese campo no es tuyo.

---

## 3. Patrón de solución: frontera de traducción

El sub-dominio captura la información en **su** vocabulario. La traducción a vocabulario de otro dominio ocurre en la **frontera**, mediante un servicio de traducción que aplica reglas configurables.

```
┌─────────────────────┐         ┌──────────────────────────┐
│   Sub-dominio A     │         │  Servicio de Traducción  │
│                     │         │  (frontera A → B)        │
│  Agregado           │────────►│                          │────────► Sub-dominio B
│   └── Dato negocio  │         │  Recibe: dato negocio    │
│       (vocabulario  │         │  Aplica: reglas de mapeo  │
│        propio)      │         │  Produce: dato traducido  │
└─────────────────────┘         └──────────────────────────┘
```

**Beneficios:**
- El sub-dominio A no se acopla al vocabulario de B.
- Las reglas de traducción pueden cambiar sin modificar el modelo de A.
- El servicio de traducción es el único punto de acoplamiento.

---

## 4. Ejemplo aplicado: OXP → Contabilidad

### El problema detectado

En una versión temprana del modelo OXP, `ConceptoDeGasto` contenía `DestinoContable { centroCosto, cuentaContable, porcentaje }`. Esto mezclaba vocabulario contable dentro de un agregado de obligaciones por pagar.

```
ConceptoDeGasto (dominio OXP)
  └── DestinoContable { centroCosto, cuentaContable, porcentaje }
                         │               │
                         │               └── Esto es del dominio Contabilidad
                         └── ¿Es de negocio o contable?
```

OXP estaba asumiendo responsabilidad de saber a qué cuenta va cada gasto, cuando eso debería ser deducido por reglas al momento de traducir la información para el sistema contable.

### Lo que OXP realmente sabe

OXP sabe **qué se compró, cuánto costó y cómo se distribuye el costo en términos de negocio**. No sabe (ni debería saber) cómo se contabiliza.

```
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
```

### El servicio de traducción en la frontera

```
Servicio de Traducción Contable (frontera OXP → Contabilidad)
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
```

### La separación de responsabilidades aplicada

| Aspecto | Dominio OXP | Traducción Contable |
|---------|------------|---------------------|
| Sabe | Qué se compró, a quién, por cuánto, cómo se distribuye en el negocio | Cómo traducir esa información a asientos contables |
| No sabe | Cuentas contables, naturalezas débito/crédito | Reglas de negocio de la OXP (estados, conciliación) |
| Ejemplo | "60% para Ventas, 40% para Admin" | "Ventas → cuenta 6101, CC-VTA-001, débito" |

### Corrección aplicada

`DestinoContable` fue reemplazado por `DestinoDeNegocio { destino, porcentaje }` — sin cuenta contable, sin naturaleza, sin centro de costo contable. Solo el destino organizacional y el porcentaje. La traducción a lenguaje contable ocurre en la frontera, por reglas que el dominio OXP no conoce.

---

## Historial

| Versión | Cambio |
|---|---|
| v1 | Documento original: problema específico de ConceptoDeGasto vs DestinoContable en OXP. |
| v2 | Generalización: guía universal de separación de responsabilidades. Contenido OXP movido a sección de ejemplo. |
