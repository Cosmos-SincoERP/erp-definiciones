# Audit Full — Reporte de Auditoría Completa

**Fecha:** 2026-03-16
**Modelo auditado:** `impuestos/modelo-dominio.md` v1.3
**Alcance cruzado:** `impuestos/definicion-alcance.md` v1.1 (glosario, 22 términos)
**Nota:** Los agregados de cumplimiento fiscal (HomologacionFiscal, FormatoFiscal, EntregableFiscal, CertificadoTributario) se auditan estructuralmente pero los hallazgos funcionales se anotan como diferidos a segunda fase, conforme a la decisión del usuario.

---

## Plan de trabajo

| # | Hallazgo | Severidad | Estado |
|---|----------|-----------|--------|
| G1 | Remanente "Contenido estándar del producto" en L170 | Baja | **Aplicado** |
| INV1 | I10 requiere LineaDescartada[] en desgravámenes con intervención, pero LineaDescartada es exclusiva del motor | Media | **Aplicado** |
| E1 | CertificadoTributarioReenviado — payload condicional de Destinatario | Baja | **Aplicado** |
| SG1 | Precondición de existencia del RegistroTributario origen no documentada en desgravámenes | Media | **Aplicado** |
| OD1 | Restricción 1:1 desgravamen-a-origen implícita en D9 | Baja | **Aplicado** |
| SC1 | D11 existe en el modelo pero ningún changelog registra su adición | Baja | **Descartado** — el changelog es snapshot de cada versión, no se modifica retroactivamente |
| SC2 | Desglose de eventos en L1232: "33 config + 11 transaccionales" — conteo invertido | Baja | **Descartado** — el changelog es snapshot de cada versión, no se modifica retroactivamente |

---

### 1. Glosario y Lenguaje Ubicuo

**Fecha:** 2026-03-16

#### Cruce con glosario canónico (22 términos)

| # Alcance | Término canónico | Representación en modelo | Alineado |
|:---------:|-----------------|-------------------------|:--------:|
| 1 | Tributo | `Tributo` (entidad en CatalogoTributario) | ✓ |
| 2 | Impuesto | `naturaleza: aditivo` | ✓ |
| 3 | Retención | `naturaleza: sustractivo` | ✓ |
| 4 | Autorretención | Tributos AUTO_* en anexos | ✓ |
| 5 | Dirección fiscal | `direccionFiscal` en D9 y ContextoTransaccional | ✓ |
| 6 | Base gravable | `baseGravable` (atributo en LineaDeDesglose, LineaDesgloseMotor, LineaDescartada) | ✓ |
| 7 | Tarifa | `tarifa` (atributo), `EntradaDeTarifa` (entidad) | ✓ |
| 8 | Cuantía mínima | `CuantiaMínima` (VO en TarifaTributaria) | ✓ |
| 9 | Tributo padre | `tributoPadre` (atributo de Tributo) | ✓ |
| 10 | Perfil tributario | `PerfilTributario` (agregado) | ✓ |
| 11 | Agente de retención | Implícito en CondicionDeAplicacion | ✓ |
| 12 | Registro tributario | `RegistroTributario` (agregado, ES) | ✓ |
| 13 | Desglose fiscal | `LineaDeDesglose[]`, `desgloseConfirmado[]` en D9 | ✓ |
| 14 | Régimen tributario | Ejemplo en PerfilTributario (`regimenTributario`) | ✓ |
| 15 | Jurisdicción | `Jurisdiccion` (VO en RegistroTributario) | ✓ |
| 16 | Certificado tributario | `CertificadoTributario` (agregado, ES) | ✓ |
| 17 | Declaración tributaria | Diferida (PD4) | ✓ |
| 18 | Reporte de información fiscal | `EntregableFiscal` (agregado, ES) | ✓ |
| 19 | Clasificación tributaria | `ClasificacionTributaria` (entidad en CatalogoTributario) | ✓ |
| 20 | Entidad fiscal emisora | `EntidadFiscalEmisora` (VO en RegistroTributario) | ✓ |
| 21 | Entidad fiscal contraparte | `EntidadFiscalContraparte` (VO en RegistroTributario) | ✓ |
| 22 | Contenido fiscal | Prosa en 3.16, D6 | ✓ |

#### Términos con Hallazgo

| Término canónico | Variantes encontradas | Secciones donde aparece | Tipo de problema |
|-----------------|----------------------|------------------------|-----------------|
| Contenido fiscal (#22) | "Contenido estándar del producto" (L170), "contenido fiscal que viene con el producto" (L1584) | 3.2 (ReglaDeLocalizacion), D6 | Inconsistencia |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| G1 | Baja | L170: `"Contenido estándar del producto."` vs L1584: `"es contenido fiscal que viene con el producto"` | Remanente de la terminología pre-v1.3. La descripción de `ReglaDeLocalizacion` usa "contenido estándar" como sinónimo del término canónico #22 ("contenido fiscal"). La resolución del hallazgo G2 en la auditoría anterior alineó la prosa en múltiples ubicaciones pero omitió L170. | Cambiar L170 a `"Contenido fiscal del producto."` — consistente con el término canónico y con D6 (L1584). No cambiar el VO `Origen` ni su valor `estándar`. |

#### Resumen
- Alta: 0 | Media: 0 | Baja: 1
- Total: 1 hallazgo

---

### 2. Composición de Agregados

**Fecha:** 2026-03-16

#### Inventario por Agregado

**Composición: CatalogoTributario**
- Entidades internas: Tributo, ClasificacionTributaria, Tratamiento, ReglaDeLocalizacion
- Value Objects: Origen
- Comportamientos calculados: tributosAplicablesA(), clasificacionesVigentes(), resolverJurisdiccion()

**Composición: TarifaTributaria**
- Entidades internas: EntradaDeTarifa
- Value Objects: Vigencia, CuantiaMínima, Origen
- Comportamientos calculados: tarifaVigenteA(), validarNoSolapamiento()

**Composición: CondicionDeAplicacion**
- Entidades internas: Condicion
- Value Objects: Vigencia, Efecto, Origen
- Comportamientos calculados: condicionesVigentesA(), evaluar()

**Composición: CatalogoDeAtributosFiscales**
- Entidades internas: DefinicionAtributo
- Value Objects: VigenciaDefinicion, Origen
- Comportamientos calculados: definicionesVigentesA(), validarValor(), atributosRequeridos()

**Composición: PerfilTributario**
- Entidades internas: AtributoFiscal
- Value Objects: Vigencia, FuenteDeAutoridad, IdentificacionFiscal
- Comportamientos calculados: atributoVigenteA(), perfilCompletoA()

**Composición: RegistroTributario (ES)**
- Entidades internas: LineaDeDesglose, LineaDesgloseMotor, LineaDescartada
- Value Objects: ContextoTransaccional, EntidadFiscalEmisora, EntidadFiscalContraparte, Jurisdiccion, IntervencionManual
- Comportamientos calculados: totalImpuestos(), totalRetenciones(), valorNeto(), fueIntervenido(), crear()

**Composición: HomologacionFiscal**
- Entidades internas: Equivalencia
- Value Objects: AutoridadFiscal, Vigencia, Origen
- Comportamientos calculados: homologar(), equivalenciasVigentesA()

**Composición: FormatoFiscal**
- Entidades internas: SeccionFormato
- Value Objects: AutoridadFiscal, Periodicidad, FormatoDeSalida, Vigencia, Origen
- Comportamientos calculados: esVigenteA(), formatosDeSalida()

**Composición: EntregableFiscal (ES)**
- Entidades internas: ContenidoGenerado
- Value Objects: AutoridadFiscal, PeriodoFiscal, ReferenciaFormato, ReferenciaHomologacion, ArchivoGenerado
- Comportamientos calculados: puedeGenerarContenido(), esPresentable()

**Composición: CertificadoTributario (ES)**
- Entidades internas: ContenidoCertificado
- Value Objects: Destinatario, PeriodoFiscal, ReferenciaFormato, ReferenciaHomologacion, ArchivoGenerado, AutoridadFiscal, ResultadoEnvio
- Comportamientos calculados: puedeGenerarContenido(), esEnviable(), esReenviable()

#### Cruce composición ↔ eventos

Verificación completa de 10 agregados × 45 eventos:
- Todas las entidades internas documentadas en composición son referenciadas por al menos un evento. ✓
- Todos los datos capturados en eventos existen en la composición del agregado correspondiente. ✓
- VOs compartidos (`Origen`, `Vigencia`, `AutoridadFiscal`) documentados consistentemente en cada agregado que los usa. ✓
- Comportamientos calculados solo referencian componentes propios. ✓

#### Inconsistencias

Sin inconsistencias detectadas.

#### Hallazgos

Sin hallazgos.

#### Resumen
- Alta: 0 | Media: 0 | Baja: 0
- Total: 0 hallazgos

---

### 3. Máquinas de Estado (FSM)

**Fecha:** 2026-03-16

#### FSM por Agregado

**FSM: EntregableFiscal**
- Estados: Borrador, Generado, Presentado ■
- Terminales: Presentado ■
- Transiciones:
  - EntregableFiscalCreado: (nuevo) → Borrador
  - EntregableFiscalGenerado: Borrador → Generado
  - EntregableFiscalRegenerado: Generado → Borrador
  - EntregableFiscalPresentado: Generado → Presentado ■
- Eventos de progreso: ninguno

**FSM: CertificadoTributario**
- Estados: Borrador, Generado, Entregado ■, Fallido
- Terminales: Entregado ■
- Transiciones:
  - CertificadoTributarioCreado: (nuevo) → Borrador
  - CertificadoTributarioGenerado: Borrador → Generado
  - CertificadoTributarioRegenerado: Generado → Borrador
  - CertificadoTributarioEntregado: Generado → Entregado ■
  - CertificadoTributarioFallido: Generado → Fallido
  - CertificadoTributarioReenviado: Fallido → Generado
- Eventos de progreso: ninguno

**Sin FSM:** RegistroTributario (hecho inmutable, 1 evento), 7 agregados de configuración (ciclo CRUD sin transiciones).

#### Verificación

- Estados huérfanos: 0 (todos los estados tienen al menos una transición de entrada). ✓
- Estados sumidero no intencionados: 0 (Fallido tiene salida vía Reenviado, Presentado y Entregado son terminales explícitos). ✓
- Transiciones imposibles: 0 (precondiciones coherentes con estados origen). ✓
- Eventos sin cobertura en FSM: 0 (todos los eventos de EntregableFiscal y CertificadoTributario están mapeados). ✓

#### Hallazgos

Sin hallazgos.

#### Resumen
- Alta: 0 | Media: 0 | Baja: 0
- Total: 0 hallazgos

---

### 4. Invariantes

**Fecha:** 2026-03-16

#### Clasificación de Invariantes

| ID | Invariante (resumen) | Tipo | Agregado(s) involucrado(s) | Enforcement documentado | Gap |
|----|---------------------|------|---------------------------|------------------------|-----|
| I1 | No solapamiento vigencias por factor/origen | Local | TarifaTributaria | `validarNoSolapamiento()` precondición | — |
| I2 | Dependencia tributo padre debe existir y estar activo | Local | CatalogoTributario | Validación interna | — |
| I3 | Unicidad de tratamiento por (tributo × clasificación × origen) | Local | CatalogoTributario | `tributosAplicablesA()` precedencia | — |
| I4 | Unicidad de equivalencia por (valorInterno + tributo + origen) | Local | HomologacionFiscal | `homologar()` precedencia | — |
| I5 | Atributo fiscal validado contra catálogo | Eventual | PerfilTributario, CatalogoDeAtributosFiscales | Validación en escritura + degradación elegante | — |
| I6 | Condición referencia atributo existente | Eventual | CondicionDeAplicacion, CatalogoDeAtributosFiscales | Validación en escritura + expiración por vigencia | — |
| I7 | Unicidad de catálogo por país | Eventual | CatalogoTributario, CatalogoDeAtributosFiscales, CondicionDeAplicacion | Validación al crear + proyección eventual | — |
| I8 | Homologación completa para generación | Eventual | EntregableFiscal, CertificadoTributario, HomologacionFiscal | Precondición de generación | — |
| I9 | Inmutabilidad del registro tributario | Local | RegistroTributario | Modelo de 1 evento | — |
| I10 | Consistencia de intervención manual | Local | RegistroTributario | Factory method `crear()` | **Gap detectado** |
| I11a | Progresión de estados EntregableFiscal | Local | EntregableFiscal | FSM enforcement | — |
| I11b | Progresión de estados CertificadoTributario | Local | CertificadoTributario | FSM enforcement | — |
| I12 | Unicidad perfil por entidad y país | Eventual | PerfilTributario | Validación al crear + proyección eventual | — |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| INV1 | Media | I10 (L~1496): `"el registro debe contener LineaDesgloseMotor[] y LineaDescartada[]"` vs L596: `"LineaDescartada: Solo presente en el cálculo original del motor."` | I10 exige `LineaDescartada[]` como parte del cálculo de referencia cuando hay intervención. Pero `LineaDescartada` es exclusiva del motor (L596) — en desgravámenes el cálculo de referencia es el prorrateo del desglose origen, que no produce tributos "descartados". Un desgravamen con intervención tendría `LineaDesgloseMotor[]` (el prorrateo) pero no `LineaDescartada[]`. I10 no distingue este caso, generando ambigüedad para la implementación del factory method `crear()` cuando `efectoFiscal = desgravamen`. | Acotar I10: `"...debe contener LineaDesgloseMotor[]. En gravámenes, también LineaDescartada[] (tributos excluidos por el motor). En desgravámenes, LineaDescartada no aplica — el cálculo de referencia es el prorrateo del origen."` Alinear L1350 (RegistroTributarioCreado) con la misma distinción. |

#### Resumen
- Alta: 0 | Media: 1 | Baja: 0
- Total: 1 hallazgo

---

### 5. Responsabilidades de Agregados

**Fecha:** 2026-03-16

#### Mapa de Responsabilidades por Agregado

**Responsabilidades: CatalogoTributario**
- Razón de cambio dominante: Qué tributos existen y cómo se aplican en un país.
- Eventos propios: 9
- Invariantes protegidas: I2, I3, I7
- Domain services que lo coordinan: MotorDeCalculo (lectura)
- Diagnóstico: Saludable

**Responsabilidades: TarifaTributaria**
- Razón de cambio dominante: Cuánto se cobra por tributo × jurisdicción.
- Eventos propios: 4
- Invariantes protegidas: I1
- Domain services que lo coordinan: MotorDeCalculo (lectura)
- Diagnóstico: Saludable

**Responsabilidades: CondicionDeAplicacion**
- Razón de cambio dominante: Excepciones por perfil tributario.
- Eventos propios: 4
- Invariantes protegidas: I6, I7
- Domain services que lo coordinan: MotorDeCalculo (lectura)
- Diagnóstico: Saludable

**Responsabilidades: CatalogoDeAtributosFiscales**
- Razón de cambio dominante: Esquema de atributos fiscales por país.
- Eventos propios: 4
- Invariantes protegidas: I5, I6, I7
- Domain services que lo coordinan: CargaAsistida (lectura)
- Diagnóstico: Saludable

**Responsabilidades: PerfilTributario**
- Razón de cambio dominante: Datos fiscales de una entidad en un país.
- Eventos propios: 4
- Invariantes protegidas: I5, I12
- Domain services que lo coordinan: MotorDeCalculo (lectura), CargaAsistida (escritura indirecta)
- Diagnóstico: Saludable

**Responsabilidades: RegistroTributario (ES)**
- Razón de cambio dominante: Hecho fiscal inmutable confirmado.
- Eventos propios: 1
- Invariantes protegidas: I9, I10
- Domain services que lo coordinan: ConfirmacionTributaria (escritura)
- Diagnóstico: Saludable — no anémico a pesar de 1 evento; tiene factory method complejo (`crear()`) con lógica de comparación y clasificación de intervención, más 4 comportamientos calculados.

**Responsabilidades: HomologacionFiscal**
- Razón de cambio dominante: Traducción de códigos internos a códigos de autoridad.
- Eventos propios: 4
- Invariantes protegidas: I4, I8
- Domain services que lo coordinan: ninguno (lectura directa por EntregableFiscal/CertificadoTributario)
- Diagnóstico: Saludable

**Responsabilidades: FormatoFiscal**
- Razón de cambio dominante: Plantilla de formato para entregables fiscales.
- Eventos propios: 5
- Invariantes protegidas: (ninguna formalizada — ver PD5)
- Domain services que lo coordinan: ninguno (lectura directa por EntregableFiscal/CertificadoTributario)
- Diagnóstico: Saludable — la ausencia de invariantes formales está reconocida en PD5.

**Responsabilidades: EntregableFiscal (ES)**
- Razón de cambio dominante: Ciclo de vida de un reporte fiscal concreto.
- Eventos propios: 4
- Invariantes protegidas: I8, I11a
- Domain services que lo coordinan: ninguno
- Diagnóstico: Saludable

**Responsabilidades: CertificadoTributario (ES)**
- Razón de cambio dominante: Ciclo de vida de un certificado tributario individual.
- Eventos propios: 6
- Invariantes protegidas: I8, I11b
- Domain services que lo coordinan: ninguno
- Diagnóstico: Saludable

#### Hallazgos

Sin hallazgos.

#### Resumen
- Alta: 0 | Media: 0 | Baja: 0
- Total: 0 hallazgos

---

### 6. Semántica de Eventos

**Fecha:** 2026-03-16

#### Inventario Semántico por Agregado

**Eventos: CatalogoTributario (9)**
- De transición: n/a (sin FSM)
- De configuración: Creado, TributoAgregado/Modificado/Desactivado, ClasificacionTributariaAgregada/Modificada/Desactivada, TratamientoDefinido, ReglaDeLocalizacionDefinida
- Naming consistente: Sí — prefijo implícito por agregado, verbos en participio
- Payloads completos: Sí

**Eventos: TarifaTributaria (4)**
- De configuración: Creada, EntradaDeTarifaAgregada/Modificada/Cerrada
- Naming consistente: Sí
- Payloads completos: Sí

**Eventos: CondicionDeAplicacion (4)**
- De configuración: Creada, CondicionAgregada/Modificada/Cerrada
- Naming consistente: Sí
- Payloads completos: Sí

**Eventos: CatalogoDeAtributosFiscales (4)**
- De configuración: Creado, DefinicionAtributoAgregada/Modificada/Cerrada
- Naming consistente: Sí
- Payloads completos: Sí

**Eventos: PerfilTributario (4)**
- De configuración: Creado, AtributoFiscalAgregado/Modificado/Cerrado
- Naming consistente: Sí
- Payloads completos: Sí

**Eventos: HomologacionFiscal (4)**
- De configuración: Creada, EquivalenciaAgregada/Modificada/Cerrada
- Naming consistente: Sí
- Payloads completos: Sí

**Eventos: FormatoFiscal (5)**
- De configuración: Creado, Modificado, SeccionFormatoAgregada/Modificada/Eliminada
- Naming consistente: Sí
- Payloads completos: Sí

**Eventos: RegistroTributario (1)**
- De creación: RegistroTributarioCreado
- Naming consistente: Sí
- Payloads completos: Sí — captura contexto completo, desglose, intervención

**Eventos: EntregableFiscal (4)**
- De transición: Creado (→Borrador), Generado (Borrador→Generado), Regenerado (Generado→Borrador), Presentado (Generado→Presentado■)
- Naming consistente: Sí
- Payloads completos: Sí

**Eventos: CertificadoTributario (6)**
- De transición: Creado (→Borrador), Generado (Borrador→Generado), Regenerado (Generado→Borrador), Entregado (Generado→Entregado■), Fallido (Generado→Fallido), Reenviado (Fallido→Generado)
- Naming consistente: Sí
- Payloads completos: Gap menor en Reenviado (ver hallazgo)

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| E1 | Baja | L~1476: `"Destinatario actualizado (si se corrigieron datos de contacto)"` | El payload de `CertificadoTributarioReenviado` incluye el Destinatario solo condicionalmente ("si se corrigieron"). Para replay seguro en ES, el evento debería documentar si el Destinatario completo siempre se captura (con o sin cambios) o solo el delta. Un consumidor del stream no puede saber si la ausencia de Destinatario significa "no cambió" sin consultar un evento anterior. | Aclarar: `"Destinatario (estado actual — siempre incluido para replay safety)"` o `"Destinatario actualizado (solo si cambió; si ausente, el valor anterior se conserva)"`. La primera opción es más segura para ES. |

#### Resumen
- Alta: 0 | Media: 0 | Baja: 1
- Total: 1 hallazgo

---

### 7. Idempotencia y Concurrencia

**Fecha:** 2026-03-16

#### Contexto

El modelo delega explícitamente los mecanismos de concurrencia, deduplicación y trazabilidad a la plataforma mediante `[D11]` (L~1658):
- **`expectedVersion`**: control de concurrencia optimista por stream (event store).
- **`idempotencyKey`**: deduplicación vía inbox/outbox pattern.
- **`correlationId`**: propagación automática por la plataforma de mensajería.

Esta decisión es legítima y está bien fundamentada: estos son concerns de infraestructura, no de dominio. El modelo no los especifica por evento ni por comando porque la plataforma los resuelve transversalmente.

#### Matriz de Idempotencia

| Operación / Comando | Agregado | IdempotencyKey documentada | Guard anti-duplicado | Optimistic concurrency | Riesgo concurrencia |
|---------------------|----------|---------------------------|---------------------|----------------------|-------------------|
| Confirmación tributaria (gravamen) | RegistroTributario | D11 (plataforma) | D11 (plataforma) | D11 (event store) | Bajo — cada confirmación crea un nuevo stream |
| Confirmación tributaria (desgravamen) | RegistroTributario | D11 (plataforma) | D11 (plataforma) | D11 (event store) | Bajo — nuevo stream, lectura de origen es idempotente |
| Configuración (todos los agregados) | 7 agregados CRUD | D11 (plataforma) | D11 (plataforma) + I7 (unicidad por país) | D11 (event store) | Bajo — streams de configuración con baja concurrencia |
| Generación de entregable | EntregableFiscal | D11 (plataforma) | FSM (solo desde Borrador) | D11 (event store) | Bajo — operación por período + autoridad |
| Generación de certificado | CertificadoTributario | D11 (plataforma) | FSM (solo desde Borrador) | D11 (event store) | Bajo — un stream por tercero × período |
| Entrega de certificado | CertificadoTributario | D11 (plataforma) | FSM (solo desde Generado) | D11 (event store) | Bajo — derivado de infraestructura |

#### Hallazgos

Sin hallazgos. D11 cubre los concerns de idempotencia y concurrencia de forma transversal. Las FSM de los agregados transaccionales agregan guards de estado que previenen doble ejecución a nivel de dominio.

#### Resumen
- Alta: 0 | Media: 0 | Baja: 0
- Total: 0 hallazgos

---

### 8. Sagas y Procesos Multi-Agregado

**Fecha:** 2026-03-16

#### Mapa de Procesos

**Proceso: ConfirmacionTributaria (3.13)**
- Trigger: Comando asíncrono de confirmación desde sub-dominio consumidor
- Agregados involucrados: MotorDeCalculo (lectura, solo gravámenes), RegistroTributario (lectura origen en desgravámenes + escritura)
- Pasos:
  1. Validar consumidor autorizado
  2. Validar estructura del comando [D9]
  3a. (Gravamen) MotorDeCalculo.calcular() → ResultadoCalculo como referencia
  3b. (Desgravamen) Buscar RegistroTributario origen por transaccionOrigenId → Prorratear desglose como referencia
  4. RegistroTributario.crear(contexto, desgloseConfirmado, calculoDeReferencia) → RegistroTributarioCreado
  5. Persistir evento en stream
- Compensación: No requerida — es una operación atómica de escritura en un solo agregado. Si falla cualquier paso previo, no se emite evento.
- CorrelationId: D11 (plataforma)
- IdempotencyKey por paso: D11 (plataforma)
- Persistencia del estado: No aplica — flujo síncrono sin estado intermedio persistido.

**Proceso: CargaAsistida (3.14)**
- Trigger: Datos normalizados desde cualquier canal (API, formulario, OCR)
- Agregados involucrados: CatalogoDeAtributosFiscales (lectura), PerfilTributario (escritura tras aprobación)
- Pasos:
  1. Recibir atributos normalizados
  2. Validar contra CatalogoDeAtributosFiscales
  3. Retornar ResultadoCarga para aprobación
  4-6. (Aplicación) Administrador aprueba → PerfilTributario actualizado
- Compensación: No requerida — la escritura en PerfilTributario es un comando simple post-aprobación.
- CorrelationId: D11
- Persistencia del estado: No aplica — flujo con aprobación humana.

**Procesos implícitos (cumplimiento fiscal — segunda fase):**
- Generación de EntregableFiscal: lectura de RegistroTributario + HomologacionFiscal → escritura en EntregableFiscal. Proceso single-aggregate.
- Generación/entrega de CertificadoTributario: incluye interacción con infraestructura de envío. PD7 reconoce gaps.

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| SG1 | Media | L~1070: `"Resuelve el RegistroTributario origen buscando por transaccionId = transaccionOrigenId con efectoFiscal = gravamen."` | El flujo de desgravamen asume que el RegistroTributario del gravamen original ya existe al momento de procesar la confirmación del desgravamen. No está documentado: (1) como precondición explícita de la confirmación tributaria para desgravámenes, (2) qué ocurre si el origen no se encuentra (¿rechazo? ¿reintento?). En un sistema EDA, el evento de confirmación del desgravamen podría llegar antes que el del gravamen original (eventual consistency). El desarrollador necesita saber qué hacer en ese caso. | Agregar precondición explícita en 3.13: `"3b. (Desgravamen) Precondición: debe existir un RegistroTributario con transaccionId = transaccionOrigenId y efectoFiscal = gravamen. Si no existe → rechazar con error indicando que el registro origen no fue encontrado."` Si se desea soportar ordenamiento eventual, documentar estrategia de retry. |

#### Resumen
- Alta: 0 | Media: 1 | Baja: 0
- Total: 1 hallazgo

---

### 9. Decisiones Abiertas

**Fecha:** 2026-03-16

#### Inventario de Pendientes

| # | Ubicación (L~N) | Texto literal | Tipo | Decisión temporal | Riesgo | Criterio de cierre |
|---|-----------------|--------------|------|-------------------|--------|-------------------|
| PD1 | L~1690 | "Validación final de composición y diseño — agregados de cumplimiento fiscal" | Diferido | Modelo documenta agregados; pendiente validación con datos reales. | Medio — gaps funcionales posibles en el frente de cumplimiento | Implementación del frente de cumplimiento |
| PD2 | L~1691 | "Localizaciones por país — contenido fiscal" | Diferido | Anexos v1.0 por país. Pendiente: ~50 conceptos RETEFUENTE, ICA municipal, homologaciones, Panamá. | Bajo — son datos operativos, no modelo | Carga de contenido fiscal |
| PD3 | L~1692 | "Eventos de integración con otros bounded contexts" | Diferido | D9 define contrato semántico. Pendiente: eventos formales. | Medio — bloqueará fase 3 (EventCatalog) | Construcción del EventCatalog |
| PD4 | L~1693 | "Declaraciones tributarias" | Diferido | FormatoFiscal soporta tipoEntregable extensible. | Bajo — decisión de producto | Decisión de producto |
| PD5 | L~1694 | "Invariantes formales de FormatoFiscal" | Diferido | Ninguna formalizada. | Bajo — FormatoFiscal es de segunda fase | Diseño del frente de cumplimiento |
| PD6 | L~1695 | "Payload de EntregableFiscalPresentado — referencia al contenido" | Diferido | Trazabilidad depende de reconstruir stream. | Bajo — segunda fase | Diseño del frente de cumplimiento |
| PD7 | L~1696 | "Documentación del proceso de generación masiva de certificados" | Diferido | Se menciona como proceso de aplicación sin documentar. | Medio — sin estrategia ante fallo parcial | Implementación de CertificadoTributario |
| — | L~1171 | `"FormatoFiscal | ... | Por definir"` (Panamá) | Dato pendiente | Contenido fiscal de Panamá incompleto. | Bajo — dato operativo | Carga de contenido Panamá |

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| OD1 | Baja | D9 (L~1627): `"transaccionOrigenId | string | Solo si efectoFiscal = desgravamen"` | El contrato D9 define `transaccionOrigenId` como un string único (no array). Esto implica que un desgravamen mapea a exactamente un gravamen original (relación 1:1). Sin embargo, en la práctica pueden existir notas crédito que cubren conceptos de múltiples facturas. Si la restricción 1:1 es por diseño (el consumidor descompone la nota crédito en múltiples confirmaciones, una por factura origen), debería formalizarse como decisión o precondición. Si es una limitación conocida, documentar como pendiente. | Evaluar: si es por diseño → agregar nota en D9: `"Cada confirmación de desgravamen referencia exactamente una transacción origen. Si una operación del consumidor afecta múltiples orígenes, el consumidor descompone en N confirmaciones."` Si es limitación → agregar como PD. |

#### Resumen
- Pendientes formales: 7 (PD1–PD7)
- Datos pendientes: 1 (Panamá)
- Decisiones implícitas: 1 (OD1)
- Alta: 0 | Media: 0 | Baja: 1
- Total: 1 hallazgo

---

### 10. Sanity Check (Coherencia Cruzada)

**Fecha:** 2026-03-16

#### Coherencia Cruzada

**Referencias verificadas:**
- D1–D11: 11 definidas, todas referenciadas (D11 como transversal). ✓
- I1–I12: 13 IDs (I11 split en a/b), todas definidas y referenciadas internamente. ✓
- P1–P5: 5 definidas, referenciadas en composición y notas. ✓
- PD1–PD7: 7 definidas, con cross-references correctas (PD1↔PD5,PD6,PD7). ✓
- R01–R38: referenciadas desde alcance, sin rotas verificables. ✓
- **Referencias rotas: 0** ✓

**Conteos verificados:**
- Total eventos: 45 (tabla L1256 coincide con catálogo detallado). ✓
- Desglose por tipo: **inconsistente** (ver SC2).
- Agregados: 10 en Sección 3 + diagrama. ✓
- Invariantes: 13 IDs vigentes (I1–I12 con I11a/b). ✓
- Decisiones: 11 (D1–D11). Changelog v1.0 dice 10 — **D11 no registrado** (ver SC1).
- Premisas: 5 (P1–P5). ✓
- Pendientes: 7 (PD1–PD7). ✓

**Decisiones vigentes:**
- D1–D10: todas alineadas con el modelo actual. ✓
- D11: alineada con el modelo (delega a plataforma), pero sin registro en changelog. ✓

**Premisas operacionalizadas:**
- P1 → MotorDeCalculo opera por concepto. ✓
- P2 → CondicionDeAplicacion evalúa roles según dirección. ✓
- P3 → Todos los agregados de configuración soportan estándar/personalizado. ✓
- P4 → ES como persistencia (D10). ✓
- P5 → Vigencia temporal en todas las configuraciones. ✓

**Conceptos eliminados/renombrados:**
- `base` → `baseGravable` (v1.3): sin remanentes en entidades/contratos. ✓
- `esRegenerable()` → `puedeGenerarContenido()` (v1.3): sin remanentes. ✓
- `contenido estándar` → `contenido fiscal` (v1.3): remanente en L170 (reportado en G1).

**Contradicciones entre secciones:**
- I10 vs L596: reportada en INV1.

#### Hallazgos

| # | Severidad | Evidencia (L~N, cita textual) | Problema | Corrección mínima |
|---|-----------|-------------------------------|----------|-------------------|
| SC1 | Baja | D11 (L~1658) no tiene entrada en ninguna versión del changelog (L1700–1717). Changelog v1.0 (L~1713): `"10 decisiones (D1–D10)"`. | D11 fue añadida al modelo pero nunca registrada en el control de versiones. No es claro en qué versión se introdujo. | Agregar mención de D11 en la entrada de changelog correspondiente (probablemente v1.0, dado que aparece junto a D10). |
| SC2 | Baja | L1232: `"33 configuración + 11 transaccionales + 1 RegistroTributario"` | El desglose por tipo es incorrecto. Conteo real: 34 configuración (9+4+4+4+4+4+5) + 10 transaccionales (4+6) + 1 RegistroTributario = 45. El total (45) es correcto pero el desglose invierte 1 evento entre las categorías. | Corregir L1232: `"34 configuración + 10 transaccionales + 1 RegistroTributario"`. |

#### Resumen
- Alta: 0 | Media: 0 | Baja: 2
- Total: 2 hallazgos

---

## Resumen Ejecutivo

| Skill | Alta | Media | Baja | Total |
|-------|------|-------|------|-------|
| Glosario | 0 | 0 | 1 | 1 |
| Composición | 0 | 0 | 0 | 0 |
| FSM | 0 | 0 | 0 | 0 |
| Invariantes | 0 | 1 | 0 | 1 |
| Responsabilidades | 0 | 0 | 0 | 0 |
| Semántica Eventos | 0 | 0 | 1 | 1 |
| Idempotencia | 0 | 0 | 0 | 0 |
| Sagas | 0 | 1 | 0 | 1 |
| Decisiones Abiertas | 0 | 0 | 1 | 1 |
| Sanity Check | 0 | 0 | 2 | 2 |
| **TOTAL** | **0** | **2** | **5** | **7** |

### Top 5 Hallazgos Críticos

| # | Skill origen | Severidad | Problema | Corrección mínima |
|---|-------------|-----------|----------|-------------------|
| 1 | Invariantes (INV1) | Media | I10 exige `LineaDescartada[]` en intervención de desgravámenes, pero `LineaDescartada` es exclusiva del motor (gravámenes). Ambigüedad en factory method `crear()` para desgravámenes con intervención. | Acotar I10 distinguiendo gravámenes (motor → LineaDesgloseMotor + LineaDescartada) de desgravámenes (prorrateo → solo LineaDesgloseMotor). |
| 2 | Sagas (SG1) | Media | ConfirmacionTributaria no documenta precondición de existencia del RegistroTributario origen al procesar desgravámenes, ni el comportamiento si no se encuentra. | Agregar precondición explícita: origen debe existir, rechazar si no se encuentra. |
| 3 | Glosario (G1) | Baja | L170 "Contenido estándar del producto" — remanente terminológico post-v1.3. | Cambiar a "Contenido fiscal del producto." |
| 4 | Semántica Eventos (E1) | Baja | `CertificadoTributarioReenviado` — payload condicional del Destinatario, ambiguo para replay ES. | Documentar si Destinatario siempre se captura o solo cuando cambia. |
| 5 | Sanity Check (SC2) | Baja | Desglose de 45 eventos: "33 config + 11 transaccionales" debería ser "34 config + 10 transaccionales". | Corregir conteo en L1232. |
