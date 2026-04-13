# Linker Documentation

Architecture and design documents for the linker are in `Metalama.Framework/docs/`:

- **[linker-architecture.md](../../../docs/linker-architecture.md)** — Transformation pipeline: indexing, two-dictionary pattern (source vs introduced constructors), `MemberLevelTransformations.Sort()` dedup, constructor rewriting flow
- **[linker-inlining.md](../../../docs/linker-inlining.md)** — Inlining system: aspect references, reachability analysis, inlining algorithms
- **[linker-callsite.md](../../../docs/linker-callsite.md)** — Call-site advice (`OnInitialized`): cross-project propagation, closure check, walker/substitution pipeline

Also:
- **[linker-overview.md](../../../docs/linker-overview.md)** — Overview of the 3-step pipeline (injection → analysis → linking), aspect reference resolution, intermediate symbol semantics
