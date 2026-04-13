# CodeModel Documentation

Architecture documentation for the CompilationModel and code model system is in `Metalama.Framework/docs/`:

- **[compilation-model.md](../../../docs/compilation-model.md)** — CompilationModel architecture: incremental immutable versioning, declaration types (source vs introduced), DeclarationBuilder vs DeclarationBuilderData freeze pattern, reference system (ISymbolRef vs IIntroducedRef), updatable collections, transformation application
- **[pipeline.md](../../../docs/pipeline.md)** — Aspect pipeline: stages, aspect layers, declaration depth ordering, parallel type processing, mutable compilation lifecycle
