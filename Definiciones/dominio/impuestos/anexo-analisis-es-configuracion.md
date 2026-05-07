# Anexo: Análisis — Event Sourcing para agregados de configuración fiscal

## Contexto

El bounded context de Impuestos tiene 9 agregados: 2 transaccionales (RegistroTributario, EntregableFiscal) y 7 de configuración (CatalogoTributario, TarifaTributaria, CondicionDeAplicacion, CatalogoDeAtributosFiscales, PerfilTributario, HomologacionFiscal, FormatoFiscal). Los transaccionales usan Event Sourcing (ES) como fuente de verdad. Este análisis evalúa si los agregados de configuración deben usar el mismo patrón o persistir como CRUD con eventos de auditoría.

**Referencia:** Decisión `[D10]` en `modelo-dominio.md`, Sección 8.

---

## Las dos posturas

### Postura A: ES completo — el stream es la fuente de verdad

El agregado de configuración no tiene tabla relacional. Su estado se reconstruye reproduciendo el stream de eventos.

**Ejemplo: TarifaTributaria para IVA Colombia**

```
Stream: tarifa-CO-IVA

Evento 1: TarifaTributariaCreada
  { tributo: IVA, jurisdiccion: CO, filas: [
    { factor: "porcentaje", valor: 19, vigenciaDesde: 2023-01-01 },
    { factor: "porcentaje", valor: 5, vigenciaDesde: 2023-01-01 },
    { factor: "porcentaje", valor: 0, vigenciaDesde: 2023-01-01 }
  ]}

Evento 2: FilaDeTarifaAgregada
  { factor: "porcentaje", valor: 15, vigenciaDesde: 2026-07-01 }

Evento 3: FilaDeTarifaDesactivada
  { factor: "porcentaje", valor: 19, vigenciaHasta: 2026-06-30 }
```

Para saber el estado actual → se reproducen los 3 eventos → se obtienen las tarifas vigentes.
Para saber el estado al 2025-06-15 → se reproduce hasta el evento 1 → tarifas originales.

### Postura B: CRUD con eventos de auditoría — la tabla es la fuente de verdad

El agregado de configuración vive en tablas relacionales. Cuando ocurre un cambio, se publica un evento como notificación, pero la tabla es la fuente de verdad.

**Ejemplo: TarifaTributaria para IVA Colombia**

```
Tabla: tarifas_tributarias
┌────────┬──────────────┬───────┬────────────────┬────────────────┐
│ tributo│ jurisdiccion │ valor │ vigencia_desde │ vigencia_hasta │
├────────┼──────────────┼───────┼────────────────┼────────────────┤
│ IVA    │ CO           │ 19%   │ 2023-01-01     │ 2026-06-30     │
│ IVA    │ CO           │ 15%   │ 2026-07-01     │ NULL           │
│ IVA    │ CO           │ 5%    │ 2023-01-01     │ NULL           │
│ IVA    │ CO           │ 0%    │ 2023-01-01     │ NULL           │
└────────┴──────────────┴───────┴────────────────┴────────────────┘

Evento publicado (notificación): TarifaTributariaActualizada
  { tributo: IVA, jurisdiccion: CO, cambio: "nueva tarifa 15%" }
```

Para saber el estado actual → `SELECT WHERE vigencia_hasta IS NULL`.
Para saber el estado al 2025-06-15 → `SELECT WHERE vigencia_desde <= fecha AND (vigencia_hasta IS NULL OR vigencia_hasta >= fecha)`.

---

## Criterios de evaluación

### 1. Rendimiento del camino crítico — lectura del motor

El MotorDeCalculo es el camino más caliente del bounded context: cada vez que un consumidor solicita un cálculo, el motor necesita leer configuración (catálogo, tarifas, condiciones, perfil tributario). Esto ocurre potencialmente cientos de veces por minuto en operación normal.

**Con ES:** El motor no lee del stream — lee de un read model (proyección). Esa proyección se actualiza cuando llega un evento de configuración. Hay una ventana de consistencia eventual entre el momento en que se publica el evento y el momento en que la proyección se actualiza.

**¿Qué tan relevante es esa ventana?** Muy poco. Los cambios de configuración fiscal son infrecuentes (reforma tributaria: 1-2 al año; ajuste de tarifa municipal: tal vez mensual). Además, los cambios de configuración fiscal tienen vigencia futura — se configuran hoy para que apliquen desde una fecha futura. Incluso si la proyección tarda segundos en actualizarse, el cambio no entra en efecto hasta la vigencia.

**Conclusión:** El rendimiento de lectura del motor es idéntico en ambos enfoques porque en ambos casos el motor debería leer de una estructura optimizada para consulta. La fuente de verdad no afecta el rendimiento de lectura si el read model está bien diseñado.

**Matiz operativo:** Si la proyección de configuración se corrompe o tiene un bug, con ES se reconstruye desde cero (replay del stream). Con CRUD, la tabla ES la proyección — si se corrompe, no hay de dónde reconstruir.

---

### 2. Evolución de esquema — el costo a largo plazo

La configuración fiscal va a evolucionar estructuralmente. Cuando se agreguen Panamá o República Dominicana, pueden aparecer nuevos tipos de reglas, nuevos atributos en el catálogo, nuevas dimensiones en las condiciones de aplicación. Esto no es especulación — está en el alcance.

**Con ES:** Cada cambio estructural requiere decidir cómo manejar eventos históricos:

- **Upcasting:** Transformar eventos viejos al formato nuevo al leerlos. Es transparente pero agrega una capa de mapeo que crece con cada versión. Es manejable si los cambios son aditivos (agregar campos con defaults). Se complica si los cambios son destructivos (renombrar, eliminar, reestructurar).
- **Versionado de eventos:** `TarifaTributariaCreada_v1`, `TarifaTributariaCreada_v2`. Explícito pero verboso.

**Con CRUD:** Un `ALTER TABLE ADD COLUMN` con default resuelve el caso aditivo. Una migración resuelve el caso destructivo. Es un patrón universal.

**¿Qué tan frecuente es el cambio estructural?** En la práctica, el esquema fiscal de un país es estable por años. Los cambios son en datos (nueva tarifa, nuevo tributo), no en estructura (nueva dimensión del modelo). Cuando ocurre un cambio estructural (agregar ReglaDeLocalizacion, por ejemplo), es un evento poco frecuente que justifica el esfuerzo de upcasting.

**Conclusión:** El costo de evolución de esquema es real pero manejable. Para un equipo maduro en ES, el upcasting es una práctica conocida. El riesgo es bajo porque los cambios estructurales de configuración fiscal son infrecuentes.

---

### 3. Reconstrucción temporal regulatoria — el argumento más fuerte

**Escenario real:** La DIAN audita el año 2025. Pregunta: "¿Con qué tarifa de retención en la fuente calcularon la retención de la factura #X del 15 de marzo de 2025?"

**Con ES:** Se reproduce el stream de TarifaTributaria hasta marzo 2025 → se obtiene el estado exacto de la configuración que el motor usó en ese momento. Se puede demostrar que la configuración era correcta en esa fecha. Si alguien la modificó después, los eventos posteriores lo evidencian.

**Con CRUD + vigencias:** Se consulta `WHERE vigencia_desde <= '2025-03-15' AND (vigencia_hasta IS NULL OR vigencia_hasta >= '2025-03-15')`. Se obtienen las tarifas que aplicaban. Pero no se puede demostrar que esas filas no fueron modificadas después. Si alguien corrigió un error en la tarifa (UPDATE directo), la fila actual no refleja lo que existía en marzo 2025 — refleja la corrección.

**Con CRUD + tabla de auditoría:** Se puede consultar la tabla de auditoría para ver el historial de cambios. Pero estas tablas tienen problemas reales conocidos por el equipo:

- Crecen significativamente y hay presión operativa para purgarlas.
- El diferencial (qué cambió exactamente) requiere comparar snapshots o capturar deltas — ambos propensos a imprecisión.
- Si se purgaron registros de auditoría anteriores a 2025, la demostración regulatoria es imposible.

**Con ES:** El stream es inmutable por diseño. No hay purga posible sin decisión explícita (y los streams de configuración son pequeños — no hay presión de espacio). La demostración regulatoria es nativa.

**Conclusión:** Para un dominio fiscal donde la configuración tiene relevancia regulatoria y puede ser auditada años después, la inmutabilidad nativa de ES es una ventaja estructural que CRUD + auditoría no puede igualar sin construir esencialmente un event store paralelo.

---

### 4. Escalabilidad — tamaño de streams y multi-tenancy

**Tamaño de streams de configuración:** Un CatalogoTributario de Colombia podría acumular, en 10 años, entre 200-500 eventos (creación de tributos, modificaciones de clasificaciones, ajustes de matriz). Una TarifaTributaria quizás 50-100 eventos. Estos números son triviales para cualquier event store — no requieren snapshots ni estrategias de compactación.

**Comparación con streams transaccionales:** Un RegistroTributario tiene 1-3 eventos por instancia pero miles de instancias. Un EntregableFiscal tiene 3-5 eventos por instancia. El volumen total de streams transaccionales es órdenes de magnitud mayor que el de configuración.

**Multi-tenancy (múltiples empresas):** Cada empresa tiene su propia configuración fiscal. Con ES, cada empresa genera sus propios streams de configuración. Con 100 empresas × 7 agregados de configuración × ~5 streams promedio por agregado = ~3,500 streams de configuración. Todos pequeños. No es un problema de escalabilidad en ningún enfoque.

**Conclusión:** La escalabilidad no es un diferenciador. Los streams de configuración son inherentemente pequeños y acotados.

---

### 5. Operaciones masivas — reformas tributarias

**Escenario:** Colombia cambia la tarifa general de IVA del 19% al 15%. Implica actualizar TarifaTributaria, posiblemente ajustar CondicionDeAplicacion y agregar nuevas entradas en CatalogoTributario.

**Con ES:** Cada cambio es un comando independiente que produce un evento. Una reforma tributaria podría generar 10-30 comandos. Cada uno se procesa individualmente, respetando las invariantes del agregado. Si uno falla, los demás siguen intactos. El historial muestra exactamente qué se cambió como parte de la reforma.

**Con CRUD:** Podría hacerse en una transacción batch. Pero en DDD, cada agregado protege sus propias invariantes, así que en la práctica también se procesan individualmente.

**Conclusión:** No hay diferencia significativa. Las reformas tributarias son operaciones infrecuentes (1-2 al año) que involucran pocos cambios.

---

### 6. Mantenimiento y carga cognitiva

**Un solo patrón vs. dos patrones:**

Si los 7 agregados de configuración son CRUD, el bounded context tiene dos modelos mentales de persistencia:

- "¿Este agregado es ES o CRUD?" → hay que recordarlo para cada uno.
- Los repositories, los tests, las migraciones, los patrones de lectura — todos son diferentes según el tipo de agregado.
- Onboarding de nuevos desarrolladores: "este agregado se lee así, pero este otro se lee de otra forma".

Si todo es ES, el bounded context tiene un solo modelo mental:

- Todo agregado tiene un stream, se reconstruye desde eventos, se proyecta a read models.
- Un solo tipo de repository, un solo patrón de testing, una sola forma de hacer rollback.
- Onboarding: "todo funciona igual, la diferencia es que los de configuración tienen streams más cortos y cambios menos frecuentes".

**Para un equipo que ya domina ES**, la carga cognitiva de mantener dos patrones es mayor que la de aplicar ES a agregados simples de configuración. ES en un agregado de configuración no es complejo — es un stream corto con eventos sencillos (TarifaCreada, FilaAgregada, FilaDesactivada). La complejidad de ES está en los flujos transaccionales con sagas, compensación y concurrencia — no en un CRUD que emite 3-5 tipos de eventos.

---

### 7. Resiliencia operativa — reconstrucción y disaster recovery

**Con ES:** Si una proyección se corrompe (bug en el projector, despliegue fallido), se reconstruye desde cero reproduciendo todos los streams. Para configuración, esto toma segundos (streams pequeños). El sistema se auto-repara.

**Con CRUD:** Si la tabla se corrompe o un bug introduce datos incorrectos, la recuperación depende de backups. Si el backup más reciente es de hace 6 horas, se pierden 6 horas de cambios. Para configuración fiscal, donde un cambio incorrecto puede afectar miles de cálculos, esto es un riesgo real.

**Conclusión:** ES ofrece una ventaja clara en resiliencia operativa. La capacidad de reconstruir proyecciones desde la fuente de verdad (el stream) es un seguro que CRUD no ofrece nativamente.

---

## Matriz de evaluación consolidada

| Criterio | Peso para Impuestos | ES completo (A) | CRUD + eventos (B) |
|----------|:---:|:---:|:---:|
| Rendimiento camino crítico (motor) | Alto | Igual (ambos usan read model) | Igual |
| Reconstrucción temporal regulatoria | **Muy alto** | **Nativo e inmutable** | Depende de auditoría paralela (frágil) |
| Evolución de esquema | Medio | Upcasting (manejable, infrecuente) | ALTER TABLE (más simple) |
| Escalabilidad | Bajo | Sin problema (streams acotados) | Sin problema |
| Operaciones masivas | Bajo | Sin diferencia práctica | Sin diferencia práctica |
| Mantenimiento (un patrón vs. dos) | **Alto** | **Un solo modelo mental** | Dos patrones en el mismo BC |
| Resiliencia operativa | Alto | **Reconstrucción nativa** | Depende de backups |
| Carga cognitiva equipo | Alto (equipo maduro ES) | **Menor** — aplica lo que ya sabe | Mayor — introduce patrón diferente |

---

## Conclusión

Los criterios que realmente diferencian son tres: **reconstrucción temporal regulatoria**, **uniformidad de patrón** y **resiliencia operativa**. En los demás criterios, las dos posturas son equivalentes.

Para un bounded context fiscal donde la configuración tiene relevancia regulatoria, el equipo ya domina ES, y la alternativa (auditoría paralela) tiene problemas operativos conocidos (crecimiento, purga, imprecisión de diferenciales), ES completo es la decisión más sólida. No porque ES sea inherentemente superior, sino porque el costo de aplicarlo a agregados simples de configuración es bajo (streams cortos, eventos sencillos, sin sagas ni compensación), y los beneficios (inmutabilidad regulatoria, un solo patrón, reconstrucción nativa) son estructurales.

El único contra real es el costo de evolución de esquema (upcasting), pero en configuración fiscal los cambios estructurales son tan infrecuentes que no justifica introducir un segundo modelo de persistencia.

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | Marzo 2026 | Versión inicial: análisis de 7 criterios, matriz de evaluación, conclusión a favor de ES completo. |
