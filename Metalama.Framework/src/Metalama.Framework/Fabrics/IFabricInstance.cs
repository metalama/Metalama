// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// Represents an instance of a <see cref="Fabrics.Fabric"/> type including its <see cref="IAspectPredecessor.TargetDeclaration"/>.
    /// </summary>
    [CompileTime]
    public interface IFabricInstance : IAspectPredecessor
    {
        /// <summary>
        /// Gets the <see cref="Fabrics.Fabric"/> instance.
        /// </summary>
        Fabric Fabric { get; }
    }
}