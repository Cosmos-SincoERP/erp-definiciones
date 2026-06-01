# Brief de Marca — ERP (nombre provisional: «Cosmos»)

> **Estado:** Borrador inicial v0.1 — definiciones de partida para maduración con el líder de diseño.
> **Propósito de este documento:** dar un punto de partida estratégico (no un manual cerrado) para que diseño ayude a madurar la identidad de marca. Lo que aquí se afirma como producto es real (viene de los modelos de dominio); lo que se propone como marca es **hipótesis a validar**.
> **Fecha:** Junio 2026

---

## 0. Cómo leer este brief

| Marca | Significa |
|---|---|
| ✅ **Dado** | Hecho del producto, ya definido en los modelos de dominio. No se discute aquí. |
| 💡 **Hipótesis** | Propuesta de marca/negocio para validar y madurar con diseño. |
| ❓ **Abierto** | Decisión pendiente que necesita la mirada del líder de diseño. |

---

## 1. Qué es el producto (✅ dado)

Un **ERP multi-país y multi-tenant** (Colombia, República Dominicana, Panamá en F1; apertura a US/CA en F2), construido como un conjunto de microservicios independientes que se comunican por eventos. Cubre la operación financiera y contable de una empresa: obligaciones por pagar, impuestos, contabilidad, terceros, estructura organizacional y servicios transversales.

**Diferenciador central (✅ dado, del modelo de Contabilidad):** la **categorización contable automática con IA**. El motor de traducción convierte hechos económicos en asientos contables sugiriendo la cuenta más probable y aprendiendo de cada decisión del contador — eliminando los catálogos manuales de miles de reglas que existen hoy en los ERPs tradicionales. Esta es la pieza que más distingue al producto y debería anclar la promesa de marca.

**Otros rasgos del producto (✅ dado):**
- Multi-país real: catálogos fiscales certificados por país, no un solo set "colombiano" adaptado.
- Arquitectura event-driven con trazabilidad y auditoría naturales (cada hecho económico es reconstruible).
- Pensado para minimizar "zonas grises": el producto se diseña con contratos de dominio explícitos.

---

## 2. Naming (❓ abierto)

**«Cosmos» es un nombre provisional** — aparece hoy en la documentación y rutas del proyecto, pero **no está confirmado como marca comercial**. El naming es una de las primeras decisiones que necesitamos del líder de diseño.

Preguntas a resolver:
- ¿«Cosmos» se mantiene, evoluciona o se reemplaza?
- ¿La marca del producto es independiente o se ata a una marca paraguas existente (p. ej. el ecosistema SincoERP / SincoA&F que aparece como sistema legacy)?
- Disponibilidad: dominio, redes, registro marcario en los tres países F1.

💡 *Hipótesis para discutir:* si «Cosmos» evoca amplitud/orden de un universo complejo, conecta bien con la idea de "poner orden en la complejidad financiera multi-país". Pero es un nombre genérico y muy usado — validar diferenciación.

---

## 3. Público objetivo (💡 hipótesis)

| Segmento | Quién | Qué le importa |
|---|---|---|
| **Usuario experto** | Contadores, analistas contables, auxiliares | Que el sistema les quite el trabajo repetitivo (clasificar cuentas) sin quitarles el control. Confianza y trazabilidad. |
| **Decisor / comprador** | Gerentes financieros, dueños de PYME, CFOs | Cumplimiento fiscal multi-país sin dolor, menos errores, menos dependencia de expertos para configurar. |
| **Implementador** | Consultores funcionales, equipo de onboarding | Que la puesta en marcha (carga de PUC, catálogos) sea guiada y rápida. |

❓ *Abierto:* ¿el foco de mercado es PYME, empresa mediana, o grupos empresariales multi-país? El producto soporta consolidación de grupo, pero el segmento prioritario define todo el tono de marca.

---

## 4. Propuesta de valor (💡 hipótesis)

**Promesa central propuesta:**
> *"Contabilidad y cumplimiento fiscal multi-país que se configuran solos y aprenden de ti."*

Tres pilares de valor:
1. **Inteligencia que elimina trabajo manual** — la IA categoriza, sugiere y aprende; el humano confirma. (Ancla en el diferenciador real.)
2. **Multi-país sin reconfigurar** — un solo sistema que ya entiende los tributos de CO, RD y PA.
3. **Confianza por diseño** — cada número es trazable a su origen; auditoría natural.

❓ *Abierto:* ¿el eje principal de la marca es la **IA** (innovación), el **multi-país** (alcance) o la **confianza/cumplimiento** (tranquilidad)? Los tres son ciertos; el orden de prioridad define el posicionamiento.

---

## 5. Atributos y personalidad de marca (💡 hipótesis)

**Atributos propuestos** (qué debe transmitir la marca):
- **Inteligente** — sin ser fría ni intimidante.
- **Confiable** — rigor financiero, precisión, cumplimiento.
- **Clara** — combate la complejidad; hace simple lo difícil.
- **Cercana** — habla el idioma del contador latinoamericano, no el de un manual técnico.

**Personalidad (arquetipo a explorar):** entre **el Sabio** (conocimiento, guía experta) y **el Mago** (transforma lo tedioso en automático). ❓ a validar con diseño.

**Tono de voz propuesto:** profesional pero humano; español Colombia natural, sin anglicismos ni jerga técnica innecesaria (consistente con la convención del proyecto). Explica, no impone.

---

## 6. Posicionamiento (💡 hipótesis)

**Contra quién competimos (✅ contexto del producto):** ERPs y sistemas contables tradicionales del mercado (SincoA&F legacy, Siigo, Alegra como destinos/competidores mencionados en el modelo) y suites globales (SAP, Oracle, NetSuite) en el segmento alto.

**Frase de posicionamiento propuesta:**
> *Para empresas que operan en varios países de Latinoamérica y están cansadas de configurar reglas contables a mano, «Cosmos» es el ERP financiero que automatiza la contabilidad con IA y entiende los impuestos de cada país — a diferencia de los ERP tradicionales que exigen catálogos manuales interminables y un experto para cada cambio.*

❓ *Abierto:* ¿competimos por **precio/simplicidad** (vs Siigo/Alegra) o por **potencia/inteligencia** (vs suites globales)? Define el rango de mercado y la estética.

---

## 7. Lo que necesitamos del líder de diseño

Este brief busca arrancar la conversación. Pedimos ayuda para madurar:

1. **Naming** — confirmar/evolucionar/reemplazar «Cosmos» (sección 2).
2. **Priorización del eje de marca** — IA vs multi-país vs confianza (sección 4).
3. **Segmento prioritario** — que aterrice el tono y la estética (sección 3).
4. **Arquetipo y personalidad** — validar Sabio/Mago u otro (sección 5).
5. **Territorio visual** — primera dirección de identidad (logo, paleta, tipografía, mood) una vez cerrado lo anterior.

---

## 8. Insumos disponibles para diseño

- `documento-consolidado-erp.md` — visión técnica y mapa de sub-dominios del producto.
- `README.md` — enfoque y filosofía del proyecto.
- `dominio/` — modelos de cada sub-dominio (el "qué hace" real del producto).
- Diferenciador de IA: ver `dominio/contabilidad/modelo-dominio.md` (decisión D6 — categorización contable con IA).

---

## Control de versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 0.1 | Junio 2026 | Borrador inicial. Definiciones de partida de marca y negocio para maduración con el líder de diseño. Naming «Cosmos» marcado como provisional. Mayoría de secciones son hipótesis a validar. |
