Entrégame la relación de los cambios realizados, con título, relación de cambios y su correspondiente justificación. Es para colocar en el mensaje del commit.

**Alcance:** Si se especifica un archivo (`$ARGUMENTS`), analiza únicamente ese archivo. Si no se especifica, analiza todos los archivos con cambios pendientes en el proyecto (usa `git diff` y `git status`).

1. **Título:** una frase descriptiva y concisa del alcance de los cambios (sin tecnicismos arrogantes).
2. **Relación de cambios:** Lista (#, Cambio, Justificación) — cada fila describe un cambio concreto realizado y por qué se hizo.

Para construir la relación, compara el estado actual contra lo descrito en la última entrada del control de versiones (la versión inmediatamente anterior) de cada archivo. Detecta todas las diferencias: entidades, eventos, invariantes, decisiones, convenciones, diagramas, secciones nuevas/reescritas, y cualquier otro cambio. No omitas nada.
