// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Fabrics
{
    /// <summary>
    /// Enumerates the kinds of fabrics.
    /// </summary>
    internal enum FabricKind
    {
        // The order is significant because it becomes the execution order.

        Compilation,

        // Transitive dependencies are intentionally running after compilation dependencies, so compilation dependencies have a chance
        // to configure the transitive dependencies before they run.
        Transitive,
        Namespace,
        Type
    }
}