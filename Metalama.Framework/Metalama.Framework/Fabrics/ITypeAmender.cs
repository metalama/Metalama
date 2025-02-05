// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// Argument of <see cref="TypeFabric.AmendType"/>. Allows reporting diagnostics and adding aspects to the target declaration of the fabric. 
    /// </summary>
    public interface ITypeAmender : IAmender<INamedType>
    {
        /// <summary>
        /// Gets the target type of the current fabric (i.e. the declaring type of the nested type).
        /// </summary>
        INamedType Type { get; }

        /// <summary>
        /// Gets an object that allows creating advice, e.g. overriding members, introducing members, or implementing new interfaces.
        /// </summary>
        IAdviceFactory Advice { get; }
    }
}