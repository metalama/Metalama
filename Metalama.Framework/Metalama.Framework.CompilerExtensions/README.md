This project contains facades for all entry points called by Roslyn at build- or design-time.

Assembly dependencies are embedded as managed resources. The entry points can contain no strongly-typed reference to
embedded assemblies, therefore facades instantiate their implementation using type names, and the implementations are
instantiated from the assemblies extracted from managed resources.