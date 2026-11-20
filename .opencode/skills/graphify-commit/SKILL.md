---
name: graphify-commit
description: "Use before creating any git commit in this repository. Updates the code knowledge graph with graphify and stages graphify-out/ so every commit keeps the graph current."
---

# Graphify en cada commit

Este repositorio versiona el grafo de graphify: `graphify-out/graph.json` y
`graphify-out/GRAPH_REPORT.md`. Antes de crear un commit:

1. Ejecuta `python -m graphify update .` (o `graphify update .` si está en PATH).
2. Añade los cambios del grafo: `git add -A graphify-out`.
3. Comprueba que el commit incluye `graphify-out/graph.json` y `GRAPH_REPORT.md`.
4. Si la actualización falla, no commitees un grafo obsoleto: corrige el error o avisa al usuario.

El hook `.githooks/pre-commit` ejecuta los pasos 1 y 2 automáticamente. Actívalo una vez
por clon:

```powershell
git config core.hooksPath .githooks
```

Si el hook no está activo, haz el flujo a mano antes de cada commit.

No incluyas en el commit estos artefactos regenerables (están en `.gitignore`):
`graphify-out/graph.html`, `graphify-out/cache/`, `manifest.json`,
`.graphify_analysis.json`, `.graphify_partition_cache.json` y `.graphify_build.json`.
