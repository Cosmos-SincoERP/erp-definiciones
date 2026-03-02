# ERP Definiciones

Definición de los sub-dominios de un ERP mediante documentos de diseño conversacional con IA.

## Dominio y sub-dominios

El **dominio** es el espacio completo del problema de negocio que el software resuelve: gestionar las operaciones financieras de la empresa — eso es el ERP.

Cada **sub-dominio** es un área de conocimiento especializado dentro de ese problema, con vocabulario propio, reglas propias y ciclo de vida independiente:

- **Obligaciones por Pagar (OXP)** — obligaciones, conciliación, extractos
- **Facturación (CXC)** — facturación, cartera, recaudo
- **Contabilidad** — asientos, cuentas, períodos
- **Tesorería** — flujos de caja, pagos, bancos
- **Emisión y Recepción Electrónica** — documentos de gasto y costo
- **Impuestos** — tributos, retenciones, declaraciones

Son sub-dominios (no dominios independientes) porque sirven al mismo negocio, comparten conceptos transversales y dependen entre sí para entregar valor.

## Artefactos por sub-dominio

Cada sub-dominio se define con 3 artefactos:

1. **definicion-alcance.md** — El *qué*: alcance funcional, glosario canónico, reglas de negocio.
2. **modelo-dominio.md** — El *cómo*: agregados, eventos, invariantes, FSM, domain services (DDD/ES/EDA).
3. **EventCatalog** — Representación visual del modelo de dominio.

## Estructura del repositorio

```
obligaciones-por-pagar/   Sub-dominio OXP: alcance y modelo de dominio
plantillas/               Plantillas base para crear nuevos sub-dominios
guias-de-modelado/        Criterios generales de modelado (aplican a todos los sub-dominios)
integraciones/            Contratos de eventos entre sub-dominios (Fase 3)
fuentes/                  Material de referencia externo
auditoria/                Reportes generados por las skills de auditoría
```
