// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// Argument of <see cref="NamespaceFabric.AmendNamespace"/>. Allows reporting diagnostics and adding aspects to the target declaration of the fabric. 
    /// </summary>
    public interface INamespaceAmender : IAmender<INamespace>
    {
        /// <summary>
        /// Gets the full name of the namespace on which the current fabric is applied.
        /// </summary>
        string Namespace { get; }
    }
}