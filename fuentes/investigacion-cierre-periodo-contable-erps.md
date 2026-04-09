# Investigacion: Cierre de Periodo Contable en ERPs Lideres

> **Fecha:** 2026-03-24
> **Proposito:** Analisis comparativo de como los ERPs lideres manejan (1) borradores pendientes al cierre de periodo y (2) transacciones que llegan despues del cierre. Incluye contexto normativo (NIIF/NIC 10, Colombia).
> **Version:** 1.0

---

## Tabla de contenido

1. [Escenario 1: Borradores/journals pendientes al cierre](#1-escenario-1-borradoresjournals-pendientes-al-cierre)
2. [Escenario 2: Transacciones que llegan despues del cierre](#2-escenario-2-transacciones-que-llegan-despues-del-cierre)
3. [Estados de periodo por ERP](#3-estados-de-periodo-por-erp)
4. [Periodos de ajuste](#4-periodos-de-ajuste)
5. [Reapertura de periodos cerrados](#5-reapertura-de-periodos-cerrados)
6. [Contexto normativo](#6-contexto-normativo)
7. [Matriz comparativa](#7-matriz-comparativa)
8. [Conclusiones y patrones identificados](#8-conclusiones-y-patrones-identificados)
9. [Fuentes](#9-fuentes)

---

## 1. Escenario 1: Borradores/journals pendientes al cierre

### 1.1 SAP S/4HANA

**Parked documents (borradores) y cierre de periodo:**

- SAP distingue entre **parked documents** (documentos estacionados, no tienen impacto en GL) y **posted documents** (documentos contabilizados).
- Los parked documents **no impiden el cierre del periodo**. No tienen efecto contable; son solo "borradores" guardados en el sistema.
- Al cerrar el periodo via OB52 (o la app "Manage Posting Periods" en S/4HANA 1809+), el sistema **no valida** si hay parked documents pendientes para ese periodo.
- Sin embargo, si un parked document tiene una fecha de posteo en un periodo ahora cerrado, al intentar postearlo (finalizarlo), el sistema **rechaza el posteo** con error M8022 ("Posting period is not open"). El usuario debe cambiar la fecha de posteo a un periodo abierto, o se debe reabrir el periodo.
- **SAP no bloquea el cierre por borradores pendientes. Los borradores quedan "huerfanos" y deben ser gestionados manualmente.**

### 1.2 Oracle Fusion

**Unposted journals y cierre de periodo:**

- Oracle Fusion **permite cerrar un periodo GL aunque existan journals sin postear** (unposted journals) en ese periodo.
- Sin embargo, el proceso de cierre **emite advertencias** (warning R_CLPR0002: "Unposted journals exist in period"). El sistema advierte pero no bloquea.
- El cierre **si puede bloquearse** si hay excepciones configuradas en los subledgers: periodos de subledger aun abiertos, transacciones sin transferir al GL, excepciones intercompany, o registros pendientes en la tabla de interfaz de GL.
- Oracle permite configurar validaciones opcionales al cierre: por defecto verifica Payables, Receivables, Projects Foundation y Revenue Management. Se pueden excluir subledgers de la validacion.
- **Oracle no bloquea por journals sin postear en GL, pero si advierte. Los journals sin postear permanecen y se pueden postear si el periodo se reabre.**

### 1.3 Microsoft Dynamics 365 Finance

**Vouchers en estado draft y cierre de periodo:**

- Dynamics 365 maneja el cierre a traves del **Fiscal Calendar** y el estado del periodo (Open, On Hold, Permanently Closed).
- El sistema **no impide** cerrar (poner en "On Hold") un periodo si hay vouchers en estado draft. No existe una validacion automatica de borradores pendientes.
- Los vouchers draft en un periodo "On Hold" **no se pueden postear** hasta que el periodo se reabra.
- El **Financial Period Close Workspace** proporciona una lista de tareas (checklist) para que el equipo contable revise manualmente lo que queda pendiente antes de cerrar, pero esto es un control operativo, no un bloqueo del sistema.
- **Dynamics 365 no bloquea el cierre por borradores. El control es operativo (checklist), no sistematico.**

### 1.4 NetSuite

**Transacciones pendientes y cierre de periodo:**

- NetSuite tiene un proceso de cierre por fases con un **Period Close Checklist** que exige completar pasos en secuencia:
  1. Lock A/P (bloquea posteos a cuentas de AP)
  2. Lock A/R (bloquea posteos a cuentas de AR)
  3. Lock Payroll (si aplica)
  4. Lock All (bloquea todos los posteos)
  5. Close Period
- **Transacciones pendientes de aprobacion** no bloquean el cierre per se, pero las transacciones que requieren aprobacion y cuyo periodo original ya esta cerrado al momento de aprobarse, se redirigen automaticamente segun la preferencia "Default Posting Period When Transaction Date in Closed Period" (primer periodo abierto o periodo actual).
- NetSuite **no valida borradores pendientes antes del cierre**. Las transacciones en draft simplemente no se han posteado y no afectan el GL.
- **NetSuite no bloquea el cierre por transacciones pendientes. Ofrece un checklist de cierre progresivo (lock por tipo de cuenta).**

### 1.5 Odoo

**Draft entries y cierre fiscal:**

- Odoo no tiene un concepto formal de "cierre de periodo" con estados. En su lugar, usa **Lock Dates** (fechas de bloqueo):
  - **Lock Date for Non-Advisors:** Bloquea a usuarios regulares.
  - **Lock Date for All Users:** Bloquea a todos.
  - **Tax Lock Date:** Bloquea modificaciones que afecten reportes de impuestos.
  - **Hard Lock Date:** Irreversible, para cumplir con regulaciones de inalterabilidad de datos.
- Los **draft entries pueden existir cuando se establece un lock date**. Si un draft entry se confirma despues del lock date, Odoo **ajusta automaticamente la fecha contable al primer dia despues del lock date**.
- Odoo **no obliga a postear o eliminar borradores antes de establecer el lock date**. Los borradores quedan pendientes y se manejan individualmente.
- **Odoo no bloquea el cierre por borradores. Los borradores con fecha anterior al lock date se re-fechan automaticamente al confirmarlos.**

### 1.6 Workday

**Journals pendientes y cierre de periodo:**

- Workday tiene un proceso de cierre de periodo con pasos definidos que incluyen posteo de journals de ajuste, depreciacion y accruals.
- Los journals tienen un flag **"Closed Period"** que indica si pertenecen a un periodo cerrado.
- Workday requiere que los journals pasen por un **flujo de aprobacion** antes de postearse. Los journals pendientes de aprobacion no bloquean el cierre del periodo.
- Al cerrar un periodo, los journals operativos que aun no se han posteado **se redirigen al siguiente periodo abierto** automaticamente.
- **Workday no bloquea el cierre por journals pendientes. Los journals operativos se redirigen al siguiente periodo abierto.**

---

## 2. Escenario 2: Transacciones que llegan despues del cierre

### 2.1 SAP S/4HANA

**Transacciones con fecha en periodo cerrado:**

- Si un modulo (MM, SD, FI) intenta postear un documento con fecha en un periodo cerrado, el sistema **rechaza la operacion** con error M8022.
- El usuario tiene tres opciones:
  1. **Cambiar la fecha de posteo** a un periodo abierto.
  2. **Reabrir el periodo** cerrado via OB52 / Manage Posting Periods.
  3. **Postear en un periodo especial** (13-16) si esta configurado y abierto.
- SAP soporta **hasta 4 periodos especiales** (13, 14, 15, 16) por ano fiscal. El periodo especial 13 es el mas comun y se usa para ajustes de auditoria y cierre de ano fiscal.
- Los periodos especiales tienen **restriccion por usuario/autorizacion**: se pueden abrir solo para usuarios especificos (ej: solo el equipo de contabilidad), usando el campo "Interval 2" (Adjustment Period Interval) en OB52 con grupo de autorizacion.
- La fecha de posteo de un documento en periodo especial debe caer dentro del **ultimo periodo regular** del ano fiscal (ej: diciembre), pero el periodo contable sera 13.
- **Para MM (materiales):** La app "Close Periods" de MM permite mantener abiertos el periodo actual y el anterior para goods receipts, y hasta 3 periodos para facturas de proveedores. Los periodos MM son independientes de los periodos FI.
- **SAP rechaza el posteo en periodo cerrado. Opciones: re-fechar, reabrir periodo, o usar periodos especiales (13-16).**

### 2.2 Oracle Fusion

**Subledger journals para periodo cerrado:**

- Si el proceso "Create Accounting" genera journals de subledger para un periodo cerrado en GL, los journals **se crean como unposted** en el periodo cerrado. Quedan en un estado pendiente.
- Para postearse, el periodo GL debe reabrirse (cambiar estado de "Closed" a "Open").
- Oracle tiene un mecanismo de **"sweep"** (barrido): las transacciones que no pueden contabilizarse en su periodo original se pueden "barrer" al siguiente periodo abierto.
- Oracle Fusion soporta **Adjustment Periods**: periodos marcados con el flag `ADJUSTMENT_PERIOD_FLAG` en la tabla GL_PERIOD_STATUSES. Estos periodos se usan para ajustes de cierre de ano sin mezclar con operaciones normales.
- Los **estados de periodo** en Oracle GL son:
  - **Never Opened** (N): Nunca abierto.
  - **Future Enterable** (F): Se pueden crear entries pero no postear.
  - **Open** (O): Se pueden crear entries y postear.
  - **Closed** (C): No se permiten entries ni posteo. Se puede reabrir.
  - **Permanently Closed** (P): No se puede reabrir jamas.
- Los subledgers (AP, AR, Projects) tienen sus propios estados de periodo, independientes del GL. Un subledger puede estar abierto mientras el GL esta cerrado.
- **Oracle rechaza posteo en periodo cerrado. Opciones: reabrir periodo, sweep al siguiente periodo abierto, o usar adjustment periods.**

### 2.3 Microsoft Dynamics 365 Finance

**Late transactions en periodo cerrado:**

- Dynamics 365 tiene tres estados de periodo:
  - **Open:** Permite posteo.
  - **On Hold:** No permite posteo, pero se puede reabrir. Usado durante el proceso de cierre para permitir ajustes controlados.
  - **Permanently Closed:** No se puede reabrir.
- Si se intenta postear a un periodo "On Hold" o "Permanently Closed", el sistema **rechaza la operacion**.
- Dynamics 365 tiene **Periodo 13** (closing period): un periodo especial creado automaticamente por el sistema para cada ano fiscal, dedicado exclusivamente a ajustes de cierre de ano. Los periodos 0 (apertura) y 13 (cierre) estan permanentemente cerrados por defecto para evitar posteo manual; se abren solo via el proceso de year-end close.
- Se puede **configurar por usuario** quien puede postear a un periodo: via "Ledger" > "Period access" se definen roles/usuarios que pueden postear a periodos especificos, incluso cuando el periodo esta "On Hold" para otros.
- Cuando se identifican ajustes post-cierre, los periodos "On Hold" **se pueden reabrir** para postear ajustes y luego cerrarse de nuevo.
- **Dynamics 365 rechaza posteo en periodo cerrado/on-hold. Opciones: reabrir periodo On Hold, o usar Periodo 13 para ajustes de cierre anual.**

### 2.4 NetSuite

**Posteo a periodos cerrados:**

- NetSuite tiene la preferencia **"Default Posting Period When Transaction Date in Closed Period"** que determina automaticamente que pasa:
  - **First Open Period:** La transaccion se redirige al primer periodo abierto.
  - **Current Period:** La transaccion se postea en el periodo actual.
- Usuarios con el permiso **"Override Period Restrictions"** pueden postear directamente a periodos cerrados/bloqueados, saltando las restricciones.
- El proceso de bloqueo es **gradual**: primero se bloquea A/P, luego A/R, luego Payroll, luego All. Cada paso permite que ciertas transacciones aun se procesen.
- Cuando una transaccion pendiente de aprobacion se aprueba y su periodo original ya esta cerrado, el sistema aplica la preferencia de redireccion automaticamente.
- **NetSuite redirige automaticamente las transacciones al periodo abierto segun configuracion. Usuarios con permisos especiales pueden saltarse la restriccion.**

### 2.5 Odoo

**Facturas de proveedores que llegan tarde (periodo cerrado):**

- Si un usuario intenta crear o confirmar una factura con fecha contable anterior o igual al lock date, Odoo **bloquea la operacion** para usuarios regulares.
- La solucion es:
  1. **Cambiar la fecha contable** a una fecha posterior al lock date (Odoo hace esto automaticamente si el draft se confirma despues del lock date).
  2. Un usuario **administrador** puede crear excepciones al lock date si es necesario.
  3. El **Tax Lock Date** opera de forma independiente: si el periodo de impuestos esta cerrado (porque ya se genero el reporte de IVA/VAT), las correcciones a facturas de cliente o proveedores **deben registrarse en el periodo siguiente**.
- Para casos de auditoria, la practica recomendada por Odoo es:
  1. Establecer **Lock Everything** al ultimo dia del ano fiscal anterior.
  2. Hacer ajustes de auditoria en el nuevo ano antes de establecer el hard lock.
  3. El **Hard Lock Date** es irreversible y garantiza inalterabilidad de datos (cumplimiento regulatorio).
- **Odoo bloquea por lock date. Opciones: re-fechar la transaccion, excepcion de administrador, o hard lock (irreversible).**

### 2.6 Workday

**Late journal entries:**

- Workday maneja los periodos contables con un flag de "Closed Period" en los journals.
- Cuando se intenta crear un journal operativo con fecha en un periodo cerrado, Workday **redirige automaticamente** al siguiente periodo abierto.
- Si no hay periodos futuros abiertos, el sistema **genera un error** y la transaccion no se procesa.
- Workday soporta **Adjustment Journals**: journals marcados explicitamente como "adjustment" que pueden postearse a periodos cerrados bajo controles especificos de seguridad.
- El proceso de cierre de ano fiscal incluye la generacion automatica de journals de reversa (accrual reversals) en el siguiente periodo.
- **Workday redirige journals operativos al siguiente periodo abierto. Soporta adjustment journals para periodos cerrados con controles de seguridad.**

---

## 3. Estados de periodo por ERP

| ERP | Estados de periodo | Notas |
|-----|-------------------|-------|
| **SAP S/4HANA** | Open, Closed (por tipo de cuenta y grupo de autorizacion) | No hay estado explicito; se controla via OB52 con intervalos por tipo de cuenta. Los periodos especiales (13-16) son un concepto separado. |
| **Oracle Fusion** | Never Opened, Future Enterable, Open, Closed, Permanently Closed | 5 estados explicitos. "Future Enterable" permite crear entries sin postear. "Closed" es reabrirle; "Permanently Closed" no. Flag de Adjustment Period separado. |
| **Dynamics 365** | Open, On Hold, Permanently Closed | 3 estados. "On Hold" es el estado intermedio: cerrado para operacion pero reabrirle. Periodo 13 automatico para ajustes de cierre. |
| **NetSuite** | Open, Locked (A/P, A/R, Payroll, All), Closed | Cierre gradual por tipo de cuenta antes del cierre total. Permiso "Override Period Restrictions" para excepciones. |
| **Odoo** | (Sin estados formales) Lock Dates por nivel | No hay estados de periodo. Se usan fechas de bloqueo con 4 niveles: non-advisors, all users, tax, hard lock (irreversible). |
| **Workday** | Open, Closed | Binario. Adjustment Journals como mecanismo separado para postear a periodos cerrados. |

---

## 4. Periodos de ajuste

### 4.1 Quienes los tienen

| ERP | Periodo de ajuste | Detalle |
|-----|-------------------|---------|
| **SAP S/4HANA** | Si — Periodos especiales 13-16 | Hasta 4 periodos especiales por ano fiscal. Se abren selectivamente por grupo de autorizacion. La fecha de posteo debe estar en el ultimo periodo regular. |
| **Oracle Fusion** | Si — Adjustment Period Flag | Periodos marcados como "adjustment" en GL_PERIOD_STATUSES. Separados de periodos operativos. |
| **Dynamics 365** | Si — Periodo 13 | Creado automaticamente por el sistema para cada ano fiscal. Exclusivo para ajustes de cierre. |
| **NetSuite** | No tiene concepto explicito | Los ajustes se hacen en periodos regulares o mediante journals manuales. |
| **Odoo** | No tiene concepto explicito | Los ajustes se manejan como entries regulares. El lock date controla cuando se puede postear. |
| **Workday** | Si — Adjustment Journal flag | No es un periodo separado, sino un **tipo de journal** marcado como "adjustment" que puede postearse a periodos cerrados. |

### 4.2 Patron comun

Los ERPs de gama alta (SAP, Oracle, Dynamics) implementan periodos de ajuste como **periodos separados** dedicados exclusivamente a ajustes post-cierre. Esto permite:
- Separar transacciones operativas de ajustes de auditoria/cierre.
- Controlar acceso: solo usuarios autorizados pueden postear a periodos de ajuste.
- Generar estados financieros con y sin ajustes de cierre (util para comparabilidad).

---

## 5. Reapertura de periodos cerrados

### 5.1 Practica por ERP

| ERP | Reapertura posible | Controles |
|-----|-------------------|-----------|
| **SAP S/4HANA** | Si (siempre) | Via OB52 / Manage Posting Periods. No hay "Permanently Closed" en FI. Cualquier periodo puede reabrirse por un usuario con autorizacion. El control es por grupo de autorizacion. |
| **Oracle Fusion** | Si (Closed), No (Permanently Closed) | Solo periodos con estado "Closed" se pueden reabrir. "Permanently Closed" es irreversible y se usa como control de auditoria definitivo. |
| **Dynamics 365** | Si (On Hold), No (Permanently Closed) | Periodos "On Hold" se reabren facilmente. "Permanently Closed" es irreversible. |
| **NetSuite** | Si | Se puede reabrir cualquier periodo cerrado. Requiere permiso "Override Period Restrictions". El sistema registra justificacion y audit trail (system notes). Se debe re-ejecutar el checklist de cierre al volver a cerrar. |
| **Odoo** | Si (Lock Dates regulares), No (Hard Lock) | Las lock dates regulares se pueden cambiar. El **Hard Lock Date es irreversible** — ni siquiera un administrador puede revertirlo. Disenado para cumplimiento regulatorio de inalterabilidad. |
| **Workday** | Si | Periodos cerrados se pueden reabrir. Los operational journals que se redirigieron al periodo siguiente se mantienen ahi (no se devuelven). |

### 5.2 Es practica comun reabrir periodos?

**Si, es comun pero debe estar controlado.** Los escenarios tipicos son:
- Ajustes de auditoria externa detectados despues del cierre.
- Correcciones de errores materiales descubiertos post-cierre.
- Transacciones intercompany que no se procesaron a tiempo.

Los riesgos de reabrir periodos incluyen:
- **Alteracion de estados financieros ya reportados.** Si los reportes ya se emitieron, los cambios pueden crear inconsistencias.
- **Trazabilidad.** Transacciones posteadas en periodos reabiertos pueden ser dificiles de rastrear.
- **Segregacion de funciones.** En la prisa por cerrar, se pueden omitir aprobaciones.

Los controles recomendados son:
- Requerir **justificacion documentada** para la reapertura (NetSuite lo exige).
- Limitar la reapertura a **roles especificos** con autorizacion explicita.
- Mantener **audit trail** completo de toda actividad en periodos reabiertos.
- Usar **"Permanently Closed"** (Oracle, Dynamics) o **Hard Lock** (Odoo) cuando el periodo no debe reabrirse bajo ninguna circunstancia.

---

## 6. Contexto normativo

### 6.1 NIC 10 / IAS 10 — Hechos posteriores al periodo sobre el que se informa

La NIC 10 distingue dos tipos de eventos posteriores al cierre:

**Tipo 1 — Eventos que implican ajuste:**
- Son hechos que proporcionan **evidencia de condiciones que ya existian** al final del periodo sobre el que se informa.
- **Requieren ajuste** de los estados financieros.
- Ejemplo: Resolucion de un litigio judicial que confirma una obligacion que ya existia al cierre. Quiebra de un cliente que ya tenia problemas de pago al cierre.
- En terminos de ERP: estos eventos justifican postear ajustes al periodo cerrado (o al periodo de ajuste).

**Tipo 2 — Eventos que NO implican ajuste:**
- Son hechos que indican condiciones que **surgieron despues** del cierre del periodo.
- **No requieren ajuste** de los estados financieros, pero si son materiales, requieren **revelacion** en notas.
- Ejemplo: Caida significativa del mercado despues del cierre. Adquisicion de una subsidiaria despues del cierre.
- En terminos de ERP: estos eventos se registran en el periodo en que ocurren, no en el cerrado.

**Ventana de tiempo:**
- Los eventos posteriores cubren desde la fecha de cierre hasta la **fecha de autorizacion de emision** de los estados financieros.
- La entidad debe revelar la fecha en que se autorizo la emision y quien la autorizo.

### 6.2 Colombia — Marco normativo vigente

**Decreto 2649 de 1993** (derogado parcialmente desde enero 2020 por Decreto 2270 de 2019):
- **Articulo 59** — Ajustes: "La informacion conocida despues de la fecha de cierre y antes de la emision de estados financieros, que proporcione evidencia adicional sobre condiciones existentes antes de la fecha de cierre, debe reconocerse en el periodo que se informa."
- Este tratamiento es consistente con la NIC 10 Tipo 1.

**Marco actual — NIIF plenas (Grupo 1) y NIIF para PYMES (Grupo 2):**
- Colombia adopto NIIF plenas mediante Decreto 2420 de 2015 (y sus modificaciones, incluyendo Decreto 2270 de 2019).
- La NIC 10 aplica directamente para entidades del Grupo 1.
- Para el Grupo 2 (NIIF para PYMES), la Seccion 32 "Hechos Ocurridos despues del Periodo sobre el que se Informa" tiene el mismo tratamiento dual (ajuste vs. revelacion).
- El Decreto 2649 fue **derogado** en lo que respecta a normas contables desde el 1 de enero de 2020. Las NIIF son el marco vigente.

**Implicacion practica para el ERP:**
- El ERP debe permitir ajustes post-cierre para eventos Tipo 1 (NIC 10), ya sea mediante periodos de ajuste o reapertura controlada.
- El ERP no debe facilitar la alteracion indiscriminada de periodos cerrados.
- Los ajustes de periodos anteriores (NIC 8 — Politicas Contables, Cambios en las Estimaciones Contables y Errores) se registran de forma **retroactiva**, lo cual puede requerir re-expresar comparativos. Esto es un requerimiento de presentacion, no necesariamente de reabrir periodos.

### 6.3 Practicas generales de auditoria

- **Es aceptable contabilizar en un periodo diferente al del hecho economico?** Si, pero con condiciones:
  - Para **errores de periodos anteriores** (NIC 8): se corrigen retroactivamente re-expresando comparativos. No se reabre el periodo; se postea un ajuste en el periodo actual que afecta retroactivamente la presentacion.
  - Para **eventos posteriores Tipo 1** (NIC 10): se ajusta el periodo cerrado (via periodo de ajuste o reapertura).
  - Para **transacciones operativas tardias** (ej: factura de proveedor que llega tarde): la practica comun es registrar en el periodo en que se recibe, con revelacion si el impacto es material. Si el periodo aun permite ajustes (esta en "adjustment period"), se puede postear ahi.
- Los **auditores** revisan la actividad en periodos reabiertos con especial atencion. Cualquier posteo post-cierre debe tener justificacion documentada.

---

## 7. Matriz comparativa

### 7.1 Escenario 1 — Borradores pendientes al cierre

| Dimension | SAP S/4HANA | Oracle Fusion | Dynamics 365 | NetSuite | Odoo | Workday |
|-----------|-------------|---------------|--------------|----------|------|---------|
| **Bloquea cierre por borradores?** | No | No (advierte) | No | No | N/A (lock dates) | No |
| **Validacion automatica?** | No | Si (warning) | No (checklist manual) | No (checklist) | No | No |
| **Que pasa con borradores?** | Quedan huerfanos; deben re-fecharse para postear | Quedan como unposted; se pueden postear si se reabre | Quedan sin postear; requieren periodo abierto | Se redirigen segun preferencia al aprobarse | Se re-fechan automaticamente al confirmar | Se redirigen al siguiente periodo abierto |

### 7.2 Escenario 2 — Transacciones tardias (periodo cerrado)

| Dimension | SAP S/4HANA | Oracle Fusion | Dynamics 365 | NetSuite | Odoo | Workday |
|-----------|-------------|---------------|--------------|----------|------|---------|
| **Comportamiento por defecto** | Rechazo (error) | Rechazo (no postea) | Rechazo (error) | Redireccion automatica | Bloqueo por lock date | Redireccion automatica |
| **Redireccion automatica?** | No | Si (sweep) | No | Si (configurable) | Si (al confirmar drafts) | Si |
| **Periodo de ajuste?** | Si (13-16) | Si (adjustment flag) | Si (Periodo 13) | No | No | Si (adjustment journal) |
| **Reapertura posible?** | Si (siempre) | Si (Closed) / No (Permanently) | Si (On Hold) / No (Permanently) | Si (con permiso) | Si (lock) / No (hard lock) | Si |
| **Control de acceso para excepciones?** | Grupo de autorizacion | Roles/seguridad | Roles/Period access | Override Period Restrictions | Admin/Hard Lock | Security roles |

---

## 8. Conclusiones y patrones identificados

### 8.1 Patron universal: ningun ERP bloquea el cierre por borradores

Ningun ERP de los analizados **bloquea** el cierre de periodo por la existencia de borradores/drafts/parked documents. El tratamiento varia entre:
- **Advertencia sin bloqueo** (Oracle: warning R_CLPR0002).
- **Checklist operativo** (Dynamics 365, NetSuite: lista de tareas manuales).
- **Silencio** (SAP, Odoo, Workday: no advierten).

Esto tiene sentido porque los borradores **no tienen impacto contable** — no afectan saldos ni estados financieros. Son "intenciones" que aun no se materializaron.

### 8.2 Dos estrategias para transacciones tardias

Los ERPs se dividen en dos filosofias:

**A. Rechazo explicito** (SAP, Oracle, Dynamics):
- El sistema rechaza el posteo y obliga al usuario a tomar accion (re-fechar, reabrir periodo, o usar periodo de ajuste).
- Mas control pero mas friccion operativa.

**B. Redireccion automatica** (NetSuite, Odoo, Workday):
- El sistema redirige la transaccion al periodo abierto automaticamente (o al confirmar el draft).
- Menos friccion pero menor precision en la asignacion temporal.

### 8.3 El periodo de ajuste es practica estandar en ERPs de gama alta

SAP (periodos 13-16), Oracle (adjustment period flag) y Dynamics 365 (Periodo 13) implementan periodos de ajuste como concepto separado. Workday lo implementa como tipo de journal (adjustment journal) en lugar de como periodo separado.

Este concepto es clave para conciliar dos necesidades en tension:
- **Necesidad operativa:** Cerrar el periodo para evitar posteos no autorizados.
- **Necesidad contable:** Permitir ajustes de auditoria y cierre de ano sin "contaminar" los periodos regulares.

### 8.4 "Permanently Closed" / "Hard Lock" como control definitivo

Oracle, Dynamics y Odoo implementan un estado **irreversible** de cierre:
- Oracle: "Permanently Closed" — no se puede reabrir.
- Dynamics: "Permanently Closed" — no se puede reabrir.
- Odoo: "Hard Lock Date" — irreversible, ni siquiera administradores pueden revertir.

SAP y Workday **no tienen** un estado de cierre irreversible. En SAP, cualquier periodo puede reabrirse con la autorizacion adecuada.

### 8.5 Implicacion para el diseno de Contabilidad (ERP Cosmos)

Basado en esta investigacion, el sub-dominio de Contabilidad deberia considerar:

1. **Estados de periodo:** Al menos 3 estados: Open, Closed (reabrirle), Permanently Closed (irreversible). Inspirado en Oracle/Dynamics.
2. **Periodo de ajuste:** Soportar un mecanismo de ajuste post-cierre, ya sea como periodo separado (estilo SAP/Oracle) o como tipo de asiento (estilo Workday).
3. **Comportamiento ante transacciones tardias:** Definir si el sistema rechaza (estilo SAP) o redirige (estilo NetSuite). Esto es una decision de diseno que depende de la filosofia del producto.
4. **Borradores y cierre:** No bloquear el cierre por borradores pendientes (consistente con todos los ERPs). Opcionalmente advertir (estilo Oracle).
5. **Lock irreversible:** Considerar un mecanismo de cierre definitivo para cumplimiento regulatorio (estilo Odoo Hard Lock).

---

## 9. Fuentes

### SAP S/4HANA
- [Managing Posting Periods — SAP Learning](https://learning.sap.com/courses/customizing-core-settings-in-financial-accounting-in-sap-s4hana/managing-posting-periods)
- [Define Open and Close Posting Periods in SAP S/4HANA](https://www.saponlinetutorials.com/define-open-and-close-posting-periods-in-sap-hana/)
- [Manage Posting Periods — SAP Help](https://help.sap.com/docs/SAP_S4HANA_ON-PREMISE/651d8af3ea974ad1a4d74449122c620e/98bd1b5825b0a107e10000000a174cb4.html)
- [Special Periods (Adjustment Periods) — SAP Help](https://help.sap.com/docs/SAP_S4HANA_CLOUD/0fa84c9d9c634132b7c4abb9ffdd8f06/e45f5a3a62cc4e3ebcc7d18fa7eeb3a3.html)
- [Differences between Normal and Adjustment Posting Periods — SAP Community](https://community.sap.com/t5/enterprise-resource-planning-q-a/differences-between-normal-and-adjustment-posting-periods-in-s-4hana-cloud/qaq-p/14208043)
- [Posting of Parked Documents — SAP Help](https://help.sap.com/docs/SAP_S4HANA_ON-PREMISE/3cb1182b4a184bdd93f8d62e3f1f0741/0d7ad253913e4608e10000000a174cb4.html)
- [SAP KBA 2072626 — Posting parked documents in closed periods](https://userapps.support.sap.com/sap/support/knowledge/en/2072626)
- [Close Periods app — SAP Community](https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-sap/how-works-the-close-periods-app-in-sap-s-4hana-cloud/ba-p/13549042)

### Oracle Fusion
- [GL Period Can Be Closed Although There Are Unposted Journal Entries — Oracle Support 1457225.1](https://support.oracle.com/knowledge/Oracle%20Fusion%20Applications/1457225_1.html)
- [Period Close warning R_CLPR0002 — Oracle Support 2440768.1](https://support.oracle.com/knowledge/Oracle%20Cloud/2440768_1.html)
- [Subledger Journals Created As Unposted In Closed Periods — Oracle Support 2346888.1](https://support.oracle.com/knowledge/Oracle%20Cloud/2346888_1.html)
- [Period Closing in Oracle Fusion — Cloudare Blogs](https://blogs.cloudare.in/2025/07/period-closing-in-oracle-fusion/)
- [GL Period Close Process in Oracle Fusion — My Techno Journal](https://mytechnojournal.com/gl-period-close-process-in-oracle-fusion/)
- [GL_PERIOD_STATUSES — Oracle Documentation](https://docs.oracle.com/en/cloud/saas/financials/24b/oedmf/glperiodstatuses-27421.html)

### Microsoft Dynamics 365 Finance
- [Year-end close — Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/finance/general-ledger/year-end-close)
- [Mass financial period close — Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/finance/general-ledger/tasks/mass-financial-period-close)
- [Close a period for posting — Cittros](https://www.cittros.com/insights/close-a-period-for-posting-in-d365-finance)
- [Financial Period Close Workspace — Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/finance/general-ledger/financial-period-close-workspace)
- [Period 13 Posting for Year-End Closing Adjustments — ERP Software Blog](https://erpsoftwareblog.com/2025/11/period-13-posting-for-year-end-closing-adjustments-in-microsoft-d365-finance/)
- [How to specify which users can post to a period — Arctic IT](https://arcticit.com/how-to-specify-which-users-can-post-to-a-period-in-dynamics-365-finance/)
- [Month-end Period on hold — Dynamics Community](https://community.dynamics.com/forums/thread/details/?threadid=b4c97a6b-d112-499b-85ee-4462761b7c9c)

### NetSuite
- [Locking and Unlocking Accounting Periods — Oracle NetSuite Help](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_N1451780.html)
- [Accounting Period Close — Oracle NetSuite Help](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_N1452509.html)
- [Unlocking Period Transactions — Oracle NetSuite Help](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_N1457300.html)
- [Using the Period Close Checklist — Oracle NetSuite Help](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_N1455781.html)
- [Process Transactions for Locked Posting Period — Oracle NetSuite Help](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_164201094029.html)
- [Mastering the NetSuite Period Close Process — Salto](https://www.salto.io/blog-posts/netsuite-period-close-process)

### Odoo
- [Year-end closing — Odoo 18.0 Documentation](https://www.odoo.com/documentation/18.0/applications/finance/accounting/reporting/year_end.html)
- [Year-end closing — Odoo 19.0 Documentation](https://www.odoo.com/documentation/19.0/applications/finance/accounting/reporting/year_end.html)
- [Odoo 18 Lock Dates FAQ — Odoo Forum](https://www.odoo.com/forum/help-1/odoo-18-lock-dates-frequently-asked-questions-263564)
- [How does the Tax Lock Date work? — Odoo Forum](https://www.odoo.com/forum/help-1/how-does-the-tax-lock-date-work-what-if-i-need-to-create-an-invoice-after-the-period-is-closed-181738)

### Workday
- [Financial Consolidation and Close Process Guide — Workday](https://www.workday.com/en-us/perspectives/finance/2025/07/financial-consolidation-close-process-guide.html)
- [Accelerating Financial Close with Automated Journal Entries — SAMAWDS](https://samawds.com/insightblog/accelerating-financial-close-processes-with-workday-financials-automated-journal-entries/)
- [Financial Accounting Period Close — WashU Workday](https://workday.wustl.edu/items/financial-accounting-period-close/)

### Normativo
- [NIC 10 — Hechos Ocurridos Despues del Periodo — Deloitte](https://www2.deloitte.com/content/dam/Deloitte/cr/Documents/audit/documentos/niif-2019/NIC%2010%20-%20Hechos%20Ocurridos%20Despu%C3%A9s%20del%20Periodo%20sobre%20el%20que%20se%20Informa.pdf)
- [Resumen NIC 10 — NIIF Go](https://niif-go.com/resumen-nic-10/)
- [Decreto 2649 de 1993 — Funcion Publica](https://www.funcionpublica.gov.co/eva/gestornormativo/norma.php?i=9863)
- [Decreto 2420 de 2015 — Funcion Publica](https://www.funcionpublica.gov.co/eva/gestornormativo/norma.php?i=76745)
- [Decreto 2270 de 2019 — SUIN Juriscol](https://www.suin-juriscol.gov.co/viewDocument.asp?id=30038628)

### Mejores practicas generales
- [Closing and Opening Accounts: Best Practices, Risks, and Variations — ClefinCode](https://clefincode.com/blog/global-digital-vibes/en/closing-and-opening-accounts-in-accounting-best-practices-risks-and-variations)
- [Overview of Close Financial Periods — Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/guidance/business-processes/record-to-report-close-financial-periods)
