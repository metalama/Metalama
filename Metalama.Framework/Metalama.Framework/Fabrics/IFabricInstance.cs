// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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